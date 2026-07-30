using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Brontide.Reference.Experimental.ComponentManagement;
using NUnit.Framework;

namespace Brontide.Reference.ComponentManagement.Tests;

[TestFixture]
public sealed class AuthorityComparisonTests
{
    private const string ReferenceImplementation = "reference-csharp";

    private static IReadOnlyList<AuthorityComparisonScenario> Scenarios()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "component-management",
            "fixtures",
            "cm6-authority-comparison-vectors.json");
        return FakeAuthorityComparisonEndpoint.LoadFixture(File.ReadAllText(path));
    }

    [Test]
    public void Neutral_scenarios_are_complete_data_and_cover_every_cm5_outcome()
    {
        var scenarios = Scenarios();

        Assert.Multiple(() =>
        {
            Assert.That(
                scenarios.Select(item => item.Id),
                Is.EqualTo(new[]
                {
                    "cm6-01-admitted",
                    "cm6-02-no-mapping",
                    "cm6-03-partial",
                    "cm6-04-unlimited",
                    "cm6-05-revoked",
                    "cm6-06-expired",
                    "cm6-07-policy-mistake",
                    "cm6-08-invalid-request",
                }));
            Assert.That(
                scenarios.Select(item => item.ExpectedOutcome).Distinct(),
                Is.EquivalentTo(new[] { "admitted", "partially-admitted", "denied", "invalid-request" }));
            Assert.That(scenarios.All(item => !item.Json.Contains("algorithm", StringComparison.Ordinal)), Is.True);
            Assert.That(scenarios.All(item => !item.Json.Contains("Reference", StringComparison.Ordinal)), Is.True);
            Assert.That(scenarios.All(item => !item.Json.Contains("Minimal", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void Every_native_profile_has_the_declared_outcome_and_complete_cm5_sections()
    {
        foreach (var scenario in Scenarios())
        {
            using var response = JsonDocument.Parse(
                FakeAuthorityComparisonEndpoint.Evaluate(scenario.Json, ReferenceImplementation));
            var root = response.RootElement;
            var profile = root.GetProperty("profile");

            Assert.Multiple(() =>
            {
                Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1), scenario.Id);
                Assert.That(root.GetProperty("implementation").GetString(), Is.EqualTo(ReferenceImplementation), scenario.Id);
                Assert.That(root.GetProperty("scenario").GetString(), Is.EqualTo(scenario.Id), scenario.Id);
                Assert.That(profile.GetProperty("outcome").GetString(), Is.EqualTo(scenario.ExpectedOutcome), scenario.Id);
                Assert.That(profile.TryGetProperty("evidenceDecisions", out _), Is.True, scenario.Id);
                Assert.That(profile.TryGetProperty("relationshipDecisions", out _), Is.True, scenario.Id);
                Assert.That(profile.TryGetProperty("authorityDecisions", out _), Is.True, scenario.Id);
                Assert.That(profile.TryGetProperty("relationships", out _), Is.True, scenario.Id);
                Assert.That(profile.TryGetProperty("grants", out _), Is.True, scenario.Id);
                Assert.That(profile.TryGetProperty("policyMistakes", out _), Is.True, scenario.Id);
                Assert.That(profile.TryGetProperty("decisionLog", out _), Is.True, scenario.Id);
            });
        }
    }

    [Test]
    public async Task Json_lines_is_stateless_ordered_and_flushes_one_response_per_input()
    {
        var scenarios = Scenarios().Take(3).ToArray();
        using var input = new StringReader(string.Join(Environment.NewLine, scenarios.Select(item => item.Json)));
        using var output = new StringWriter();

        await FakeAuthorityComparisonEndpoint.RunAsync(input, output, ReferenceImplementation);

        var lines = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.That(
            lines.Select(line => JsonDocument.Parse(line).RootElement.GetProperty("scenario").GetString()),
            Is.EqualTo(scenarios.Select(item => item.Id)));
    }

    [Test]
    public void Protocol_errors_are_not_cm5_outcomes()
    {
        var unsupported = JsonNode.Parse(Scenarios()[0].Json)!.AsObject();
        unsupported["schemaVersion"] = 2;
        var malformed = FakeAuthorityComparisonEndpoint.Evaluate("{", ReferenceImplementation);
        var schema = FakeAuthorityComparisonEndpoint.Evaluate(unsupported.ToJsonString(), ReferenceImplementation);
        var unknown = JsonNode.Parse(Scenarios()[0].Json)!.AsObject();
        unknown["relationships"]![0]!["kind"] = "invented";
        var token = FakeAuthorityComparisonEndpoint.Evaluate(unknown.ToJsonString(), ReferenceImplementation);

        Assert.Multiple(() =>
        {
            Assert.That(ErrorCode(malformed), Is.EqualTo("malformed-json"));
            Assert.That(ErrorCode(schema), Is.EqualTo("unsupported-schema"));
            Assert.That(ErrorCode(token), Is.EqualTo("unknown-token"));
            Assert.That(malformed, Does.Not.Contain("\"profile\""));
            Assert.That(schema, Does.Not.Contain("\"profile\""));
            Assert.That(token, Does.Not.Contain("\"profile\""));
        });
    }

    [Test]
    public void Oversized_lines_fail_at_the_protocol_boundary()
    {
        var response = FakeAuthorityComparisonEndpoint.Evaluate(
            new string(' ', 1_048_577),
            ReferenceImplementation);

        Assert.Multiple(() =>
        {
            Assert.That(ErrorCode(response), Is.EqualTo("invalid-envelope"));
            Assert.That(response, Does.Not.Contain("\"profile\""));
        });
    }

    [Test]
    public void Repetition_and_semantic_enumeration_permutation_are_byte_stable()
    {
        var scenario = Scenarios().Single(item => item.Id == "cm6-03-partial");
        var first = Profile(FakeAuthorityComparisonEndpoint.Evaluate(scenario.Json, ReferenceImplementation));
        var repeated = Profile(FakeAuthorityComparisonEndpoint.Evaluate(scenario.Json, ReferenceImplementation));
        var node = JsonNode.Parse(scenario.Json)!.AsObject();
        Reverse(node["authority"]!.AsArray());
        Reverse(node["policy"]!["authority"]!.AsArray());
        var permuted = Profile(
            FakeAuthorityComparisonEndpoint.Evaluate(node.ToJsonString(), ReferenceImplementation));

        Assert.Multiple(() =>
        {
            Assert.That(repeated, Is.EqualTo(first));
            Assert.That(permuted, Is.EqualTo(first));
        });
    }

    [Test]
    [Category("CrossProcess")]
    [Category("CrossStack")]
    public async Task Reference_profiles_equal_the_Minimal_process_for_every_scenario()
    {
        using var process = StartForeign("BRONTIDE_MINIMAL_PROVIDER", "Minimal", "--component-management");
        foreach (var scenario in Scenarios())
        {
            await process.StandardInput.WriteLineAsync(scenario.Json);
            await process.StandardInput.FlushAsync();
            var line = await process.StandardOutput.ReadLineAsync();
            Assert.That(line, Is.Not.Null, scenario.Id);
            using var response = JsonDocument.Parse(line!);

            Assert.Multiple(() =>
            {
                Assert.That(response.RootElement.GetProperty("implementation").GetString(), Is.EqualTo("minimal-fsharp"), scenario.Id);
                Assert.That(
                    response.RootElement.GetProperty("profile").GetRawText(),
                    Is.EqualTo(Profile(FakeAuthorityComparisonEndpoint.Evaluate(scenario.Json, ReferenceImplementation))),
                    scenario.Id);
            });
        }

        process.StandardInput.Close();
        await process.WaitForExitAsync();
        Assert.That(process.ExitCode, Is.Zero, await process.StandardError.ReadToEndAsync());
    }

    private static Process StartForeign(string variable, string label, params string[] arguments)
    {
        var path = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Assert.Ignore($"{variable} does not name a built {label} provider endpoint.");
        }

        return Process.Start(new ProcessStartInfo
        {
            FileName = path!,
            Arguments = string.Join(' ', arguments),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException($"The {label} provider process did not start.");
    }

    private static string Profile(string response)
    {
        using var document = JsonDocument.Parse(response);
        return document.RootElement.GetProperty("profile").GetRawText();
    }

    private static string? ErrorCode(string response)
    {
        using var document = JsonDocument.Parse(response);
        return document.RootElement.GetProperty("protocolError").GetProperty("code").GetString();
    }

    private static void Reverse(JsonArray array)
    {
        var values = array.Select(item => item!.DeepClone()).Reverse().ToArray();
        array.Clear();
        foreach (var value in values)
        {
            array.Add(value);
        }
    }
}
