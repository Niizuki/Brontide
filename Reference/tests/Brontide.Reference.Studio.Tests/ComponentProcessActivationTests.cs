using Brontide.Reference.Experimental.Binding.Portable;
using System.Diagnostics;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed record Cbi30Observation(
        bool Active,
        string Code,
        string? Realization,
        string? AnsweringProvider,
        bool Released,
        bool Retired,
        bool ProviderExited);

    private static Process StartCbi30Provider(string provider)
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

        return Process.Start(new ProcessStartInfo
        {
            FileName = path!,
            Arguments = "--portable",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException("The CBI30 provider process did not start.");
    }

    private static PortableProcessConversation Cbi30Conversation(Process process) =>
        new(
            new PortableStreamDuplex(
                process.StandardOutput.BaseStream,
                process.StandardInput.BaseStream,
                PortableLimits.Declared,
                ownsStreams: false),
            PortableLimits.Declared);

    private static async Task<bool> WaitForCbi30ExitAsync(Process process)
    {
        if (process.HasExited)
        {
            return true;
        }

        var exited = process.WaitForExitAsync();
        return await Task.WhenAny(exited, Task.Delay(TimeSpan.FromSeconds(5))) == exited;
    }

    private static async Task StopCbi30ProviderAsync(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }

        await process.WaitForExitAsync();
    }

    private static async Task<Cbi30Observation> Cbi30RunAsync(
        string provider,
        bool interruptBeforeInterconnection)
    {
        using var process = StartCbi30Provider(provider);
        await using var conversation = Cbi30Conversation(process);
        try
        {
            if (interruptBeforeInterconnection)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            var (resolution, selection, occurrence) = LifecycleInput();
            var result = await ComponentBindingLifecycle.ActivateAsync(
                resolution,
                selection,
                RuntimeRequest(Plan(occurrence)),
                conversation);
            var member = result.Member;
            var realization = member?.Plan?.Realization.Token();
            var answeringProvider = member?.AnsweringProvider?.ToString();
            var released = member?.IsReleased == true;
            var active = result.IsActive;
            var retired = false;

            if (active)
            {
                var retirement = await member!.RetireAsync("CBI30 process activation completed.");
                retired = member.Stage == PortableCompositionStage.Retired && retirement.ReplacementPermitted;
            }

            if (member is not null)
            {
                await member.DisposeAsync();
            }

            return new(
                active,
                result.Failure?.Code ?? "active",
                realization,
                answeringProvider,
                released,
                retired,
                await WaitForCbi30ExitAsync(process));
        }
        finally
        {
            await StopCbi30ProviderAsync(process);
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Shared_cbi30_vectors_activate_through_real_provider_processes()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi30-process-activation-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var observation = await Cbi30RunAsync(
                vector.GetProperty("provider").GetString()!,
                vector.GetProperty("interruptBeforeInterconnection").GetBoolean());
            var expectedRealization = vector.GetProperty("expectedRealization");

            Assert.Multiple(() =>
            {
                Assert.That(observation.Active, Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()), scenario);
                Assert.That(observation.Code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()), scenario);
                Assert.That(
                    observation.Realization,
                    Is.EqualTo(expectedRealization.ValueKind == JsonValueKind.Null ? null : expectedRealization.GetString()),
                    scenario);
                Assert.That(observation.Released, Is.EqualTo(vector.GetProperty("expectedReleased").GetBoolean()), scenario);
                Assert.That(observation.Retired, Is.EqualTo(vector.GetProperty("expectedRetired").GetBoolean()), scenario);
                Assert.That(observation.ProviderExited, Is.EqualTo(vector.GetProperty("expectedProviderExited").GetBoolean()), scenario);
                Assert.That(observation.Active, Is.EqualTo(observation.Released), scenario);
            });
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi30_C1_activation_crosses_a_real_process_boundary()
    {
        var observation = await Cbi30RunAsync("reference", interruptBeforeInterconnection: false);

        Assert.Multiple(() =>
        {
            Assert.That(observation.Active, Is.True);
            Assert.That(observation.Released, Is.True);
            Assert.That(observation.Code, Is.EqualTo("active"));
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi30_C2_either_stack_provider_is_substitutable_at_the_process_seam()
    {
        var reference = await Cbi30RunAsync("reference", interruptBeforeInterconnection: false);
        var minimal = await Cbi30RunAsync("minimal", interruptBeforeInterconnection: false);

        Assert.Multiple(() =>
        {
            Assert.That(reference with { ProviderExited = false }, Is.EqualTo(minimal with { ProviderExited = false }));
            Assert.That(reference.ProviderExited, Is.True);
            Assert.That(minimal.ProviderExited, Is.True);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi30_C3_the_negotiated_realization_and_answering_provider_are_observable()
    {
        var observation = await Cbi30RunAsync("minimal", interruptBeforeInterconnection: false);

        Assert.Multiple(() =>
        {
            Assert.That(observation.Realization, Is.EqualTo("negotiated-process"));
            Assert.That(observation.AnsweringProvider, Is.EqualTo(CoolingPortableFixture.Provider.ToString()));
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi30_C4_process_loss_is_an_explicit_pre_release_refusal()
    {
        var observation = await Cbi30RunAsync("reference", interruptBeforeInterconnection: true);

        Assert.Multiple(() =>
        {
            Assert.That(observation.Code, Is.EqualTo("portable-process-interrupted"));
            Assert.That(observation.Active, Is.False);
            Assert.That(observation.Released, Is.False);
            Assert.That(observation.Realization, Is.Null);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi30_C5_retirement_closes_the_process_lifecycle()
    {
        var observation = await Cbi30RunAsync("reference", interruptBeforeInterconnection: false);

        Assert.Multiple(() =>
        {
            Assert.That(observation.Retired, Is.True);
            Assert.That(observation.ProviderExited, Is.True);
        });
    }
}
