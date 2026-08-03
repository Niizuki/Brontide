using System.Security.Cryptography;
using System.Text.Json;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed record Cbi31Observation(
        string Code,
        bool Launched,
        string? Isolation,
        bool Active,
        bool Released,
        bool Retired,
        bool ProviderExited);

    private static string Cbi31ProviderPath(string provider)
    {
        var variable = provider switch
        {
            "reference" => "BRONTIDE_REFERENCE_PROVIDER",
            "minimal" => "BRONTIDE_MINIMAL_PROVIDER",
            _ => throw new ArgumentOutOfRangeException(nameof(provider)),
        };
        var path = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Assert.Ignore($"{variable} does not name a built provider endpoint.");
        }

        return Path.GetFullPath(path!);
    }

    private static string Cbi31Digest(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static async Task<Cbi31Observation> Cbi31RunAsync(JsonElement vector)
    {
        var providerPath = Cbi31ProviderPath(vector.GetProperty("provider").GetString()!);
        var requestedPath = vector.GetProperty("path").GetString() == "missing"
            ? providerPath + ".missing"
            : providerPath;
        var digest = vector.GetProperty("digest").GetString() == "incorrect"
            ? new string('0', 64)
            : Cbi31Digest(providerPath);
        var arguments = vector.GetProperty("arguments").EnumerateArray().Select(value => value.GetString()!).ToArray();
        var allowedArguments = vector.GetProperty("allowedArguments").EnumerateArray().Select(value => value.GetString()!).ToArray();
        var acquisition = LocalProviderArtifactActivator.AcquireAndLaunch(
            new LocalProviderArtifact(vector.GetProperty("id").GetString()!, requestedPath, digest, arguments),
            new LocalProviderLaunchPolicy(Path.GetDirectoryName(providerPath)!, allowedArguments));

        if (!acquisition.IsLaunched)
        {
            return new(acquisition.Failure!.Code, false, null, false, false, false, true);
        }

        await using var owner = acquisition.Owner!;
        var (resolution, selection, occurrence) = LifecycleInput();
        var result = await ComponentBindingLifecycle.ActivateAsync(
            resolution,
            selection,
            RuntimeRequest(Plan(occurrence)),
            owner.Conversation);
        var member = result.Member;
        var active = result.IsActive;
        var released = member?.IsReleased == true;
        var retired = false;
        if (active)
        {
            var retirement = await member!.RetireAsync("CBI31 artifact activation completed.");
            retired = member.Stage == PortableCompositionStage.Retired && retirement.ReplacementPermitted;
        }

        if (member is not null)
        {
            await member.DisposeAsync();
        }

        return new(
            result.Failure?.Code ?? "active",
            true,
            owner.Isolation,
            active,
            released,
            retired,
            await owner.WaitForExitAsync(TimeSpan.FromSeconds(5)));
    }

    private static async Task<JsonElement> Cbi31VectorAsync(string identity)
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi31-local-artifact-activation-vectors.json")));
        return fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(vector => vector.GetProperty("id").GetString() == identity)
            .Clone();
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Shared_cbi31_vectors_verify_policy_and_activate_local_artifacts()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi31-local-artifact-activation-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var observation = await Cbi31RunAsync(vector);
            var expectedIsolation = vector.GetProperty("expectedIsolation");
            Assert.Multiple(() =>
            {
                Assert.That(observation.Code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()), scenario);
                Assert.That(observation.Launched, Is.EqualTo(vector.GetProperty("expectedLaunched").GetBoolean()), scenario);
                Assert.That(
                    observation.Isolation,
                    Is.EqualTo(expectedIsolation.ValueKind == JsonValueKind.Null ? null : expectedIsolation.GetString()),
                    scenario);
                Assert.That(observation.Active, Is.EqualTo(observation.Launched), scenario);
                Assert.That(observation.Released, Is.EqualTo(observation.Launched), scenario);
                Assert.That(observation.Retired, Is.EqualTo(observation.Launched), scenario);
                Assert.That(observation.ProviderExited, Is.True, scenario);
            });
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi31_C1_acquisition_verifies_an_immutable_local_artifact()
    {
        var missing = await Cbi31RunAsync(await Cbi31VectorAsync("cbi31-03-missing-artifact"));
        var changed = await Cbi31RunAsync(await Cbi31VectorAsync("cbi31-04-integrity-refused"));
        Assert.Multiple(() =>
        {
            Assert.That(missing.Code, Is.EqualTo("artifact-unavailable"));
            Assert.That(changed.Code, Is.EqualTo("artifact-integrity-failed"));
            Assert.That(missing.Launched || changed.Launched, Is.False);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi31_C2_launch_policy_is_explicit_and_precedes_execution()
    {
        var arguments = await Cbi31RunAsync(await Cbi31VectorAsync("cbi31-05-arguments-refused"));
        var providerPath = Cbi31ProviderPath("reference");
        var outsideRoot = LocalProviderArtifactActivator.AcquireAndLaunch(
            new LocalProviderArtifact("outside-root", providerPath, Cbi31Digest(providerPath), ["--portable"]),
            new LocalProviderLaunchPolicy(Path.Combine(Path.GetDirectoryName(providerPath)!, "allowed"), ["--portable"]));
        Assert.Multiple(() =>
        {
            Assert.That(arguments.Code, Is.EqualTo("launch-policy-refused"));
            Assert.That(arguments.Launched, Is.False);
            Assert.That(outsideRoot.Failure?.Code, Is.EqualTo("launch-policy-refused"));
            Assert.That(outsideRoot.IsLaunched, Is.False);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi31_C3_launch_isolation_is_observable_and_bounded()
    {
        var providerPath = Cbi31ProviderPath("reference");
        var activation = LocalProviderArtifactActivator.AcquireAndLaunch(
            new LocalProviderArtifact("isolation", providerPath, Cbi31Digest(providerPath), ["--portable"]),
            new LocalProviderLaunchPolicy(Path.GetDirectoryName(providerPath)!, ["--portable"]));
        await using var owner = activation.Owner!;
        Assert.Multiple(() =>
        {
            Assert.That(activation.IsLaunched, Is.True);
            Assert.That(owner.Isolation, Is.EqualTo("dedicated-process"));
            Assert.That(owner.UsesShell, Is.False);
            Assert.That(owner.RedirectsStandardStreams, Is.True);
        });

        var nonExecutable = Path.Combine(Path.GetTempPath(), $"brontide-cbi31-{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllTextAsync(nonExecutable, "not an executable");
            var refused = LocalProviderArtifactActivator.AcquireAndLaunch(
                new LocalProviderArtifact("not-executable", nonExecutable, Cbi31Digest(nonExecutable), ["--portable"]),
                new LocalProviderLaunchPolicy(Path.GetTempPath(), ["--portable"]));
            Assert.That(refused.Failure?.Code, Is.EqualTo("provider-process-start-failed"));
        }
        finally
        {
            File.Delete(nonExecutable);
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi31_C4_owner_composes_with_cbi30_and_owns_retirement_cleanup()
    {
        var observation = await Cbi31RunAsync(await Cbi31VectorAsync("cbi31-01-reference-artifact"));
        Assert.Multiple(() =>
        {
            Assert.That(observation.Active, Is.True);
            Assert.That(observation.Released, Is.True);
            Assert.That(observation.Retired, Is.True);
            Assert.That(observation.ProviderExited, Is.True);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi31_C5_both_roots_agree_on_portable_observations()
    {
        var reference = await Cbi31RunAsync(await Cbi31VectorAsync("cbi31-01-reference-artifact"));
        var minimal = await Cbi31RunAsync(await Cbi31VectorAsync("cbi31-02-minimal-artifact"));
        Assert.That(reference, Is.EqualTo(minimal));
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi31_C4_owner_cleanup_terminates_an_unfinished_process()
    {
        var providerPath = Cbi31ProviderPath("reference");
        var activation = LocalProviderArtifactActivator.AcquireAndLaunch(
            new LocalProviderArtifact("cleanup", providerPath, Cbi31Digest(providerPath), ["--portable"]),
            new LocalProviderLaunchPolicy(Path.GetDirectoryName(providerPath)!, ["--portable"]));
        Assert.That(activation.IsLaunched, Is.True);
        var owner = activation.Owner!;
        await owner.DisposeAsync();
        Assert.That(owner.HasExited, Is.True);
    }
}
