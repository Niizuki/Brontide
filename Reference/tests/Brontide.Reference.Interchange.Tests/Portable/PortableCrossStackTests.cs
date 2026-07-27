using System.Diagnostics;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// PB5: a Reference host drives a <em>Minimal</em> provider over the portable contract.
/// </summary>
/// <remarks>
/// This is the first evidence that the two stacks can speak to each other over the reusable layer
/// rather than over the retained line-delimited Cooling and Catalog experiments. Nothing is shared
/// but the data: the Minimal endpoint is a separate process built from F# sources that reference no
/// Reference assembly, and the only thing crossing is deterministic CBOR described by the neutral
/// schemas.
///
/// The scenarios are the PB4 parity matrix, unchanged. Reusing it is the point — the claim is not
/// that some request works across the stacks, but that the same category-level observations the
/// Reference host reports when it talks to itself are what it reports when it talks to Minimal.
/// </remarks>
[Category("CrossProcess")]
[Category("CrossStack")]
public sealed class PortableCrossStackTests
{
    public static IEnumerable<PortableParityScenario> Scenarios => PortableParityMatrix.Scenarios;

    private static Process StartMinimalProvider(params string[] arguments)
    {
        var path = Environment.GetEnvironmentVariable("BRONTIDE_MINIMAL_PROVIDER");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Assert.Ignore("BRONTIDE_MINIMAL_PROVIDER does not name a built Minimal provider endpoint.");
        }

        return Process.Start(new ProcessStartInfo
        {
            FileName = path!,
            Arguments = string.Join(' ', arguments),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("The Minimal provider process did not start.");
    }

    private static async Task StopAsync(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }

        await process.WaitForExitAsync();
    }

    [TestCaseSource(nameof(Scenarios))]
    public async Task A_Reference_host_and_a_Minimal_provider_agree_on_every_parity_scenario(
        PortableParityScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        // The baseline is this stack talking to itself, so a difference is attributable to the peer
        // rather than to the scenario.
        var native = await PortableRealizationParityTests.RunAsync(scenario, direct: true);

        using var provider = StartMinimalProvider([.. scenario.ProviderArguments]);
        try
        {
            await using var host = await PortableBindingHost.EstablishAsync(
                scenario.Contract,
                new PortableProcessConversation(
                    new PortableStreamDuplex(
                        provider.StandardOutput.BaseStream,
                        provider.StandardInput.BaseStream,
                        PortableLimits.Declared,
                        ownsStreams: false),
                    PortableLimits.Declared),
                "reference-hosts-minimal");

            var crossed = await host.InvokeAsync(
                scenario.Operation,
                scenario.InputShape,
                scenario.Input,
                scenario.Authority,
                scenario.Resources);

            Assert.Multiple(() =>
            {
                Assert.That(crossed.FrameDecision, Is.EqualTo(scenario.Frame));
                Assert.That(crossed.ResultClass, Is.EqualTo(scenario.Result));
                Assert.That(crossed.Category, Is.EqualTo(scenario.Category));
                Assert.That(crossed.ParityProfile(), Is.EqualTo(native.ParityProfile()));
            });
        }
        finally
        {
            await StopAsync(provider);
        }
    }

    /// <summary>
    /// The negotiated plan names the Minimal provider, so the binding is demonstrably not this
    /// stack's own endpoint under another name.
    /// </summary>
    [Test]
    public async Task The_established_plan_records_the_Minimal_provider_and_a_crossed_process_boundary()
    {
        using var provider = StartMinimalProvider("--portable");
        try
        {
            await using var host = await PortableBindingHost.EstablishAsync(
                CoolingPortableFixture.Contract,
                new PortableProcessConversation(
                    new PortableStreamDuplex(
                        provider.StandardOutput.BaseStream,
                        provider.StandardInput.BaseStream,
                        PortableLimits.Declared,
                        ownsStreams: false),
                    PortableLimits.Declared),
                "reference-hosts-minimal");

            var result = await host.InvokeAsync(
                CoolingPortableFixture.SetEnabled,
                CoolingPortableFixture.CommandV1,
                CoolingPortableFixture.Command("primary", enabled: true),
                PortableTestHarness.Permitted(),
                [PortableTestHarness.Blob()]);

            Assert.Multiple(() =>
            {
                Assert.That(host.Plan.Fact("framing"), Is.EqualTo("length-delimited"));
                Assert.That(host.Plan.Fact("realization"), Is.EqualTo("negotiated-process"));
                Assert.That(result.Observation.CrossedBoundaries, Does.Contain("process"));
                Assert.That(result.Observation.SelectedProvider, Is.EqualTo(CoolingPortableFixture.Provider));
                Assert.That(result.Observation.CopyCount, Is.EqualTo(1));
                Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo(1));
            });

            await host.WithdrawAsync();
            await host.TerminateAsync();
        }
        finally
        {
            await StopAsync(provider);
        }
    }

    /// <summary>
    /// A shaped failed Outcome produced by the Minimal provider's own domain crosses as data, with
    /// no F# exception, runtime type name, or stack trace anywhere in the observation.
    /// </summary>
    [Test]
    public async Task A_Minimal_provider_failure_crosses_as_shaped_data_and_not_as_a_foreign_runtime_value()
    {
        using var provider = StartMinimalProvider("--portable");
        try
        {
            await using var host = await PortableBindingHost.EstablishAsync(
                CoolingPortableFixture.Contract,
                new PortableProcessConversation(
                    new PortableStreamDuplex(
                        provider.StandardOutput.BaseStream,
                        provider.StandardInput.BaseStream,
                        PortableLimits.Declared,
                        ownsStreams: false),
                    PortableLimits.Declared),
                "reference-hosts-minimal");

            var result = await host.InvokeAsync(
                CoolingPortableFixture.SetEnabled,
                CoolingPortableFixture.CommandV1,
                CoolingPortableFixture.Command("primary", enabled: true, failureMode: "requested-failure"),
                PortableTestHarness.Permitted());

            var details = (PortableRecordValue)result.Value!;
            Assert.Multiple(() =>
            {
                Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.OutcomeFailed));
                Assert.That(result.Category, Is.Null, "A domain refusal is not a protocol error.");
                Assert.That(details.Fields, Does.ContainKey("code"));
                Assert.That(result.Observation.FailureDomain, Is.EqualTo(PortableFailureDomain.RemoteProvider));
                Assert.That(result.Observation.LocalMessage ?? string.Empty, Does.Not.Contain("Microsoft.FSharp"));
                Assert.That(result.Observation.LocalMessage ?? string.Empty, Does.Not.Contain("Exception"));
            });
        }
        finally
        {
            await StopAsync(provider);
        }
    }
}
