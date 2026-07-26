using System.Diagnostics;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// The negotiated process realization across a real operating-system process boundary.
/// </summary>
/// <remarks>
/// The default suite exercises the same realization over a local pipe seam, which proves the
/// encoding, bounds, and copy accounting. This suite proves the remaining claim that seam cannot:
/// that the endpoint needs nothing of the host's process to reach the same observations.
/// </remarks>
[Category("CrossProcess")]
public sealed class PortableCrossProcessTests
{
    private static Process StartProvider(params string[] arguments)
    {
        var path = Environment.GetEnvironmentVariable("BRONTIDE_REFERENCE_PROVIDER");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Assert.Ignore("BRONTIDE_REFERENCE_PROVIDER does not name a built Reference provider endpoint.");
        }

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = path!,
            Arguments = string.Join(' ', arguments),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("The provider process did not start.");
        return process;
    }

    private static PortableProcessConversation Conversation(Process process, PortableLimits limits) =>
        new(
            new PortableStreamDuplex(
                process.StandardOutput.BaseStream,
                process.StandardInput.BaseStream,
                limits,
                ownsStreams: false),
            limits);

    [Test]
    public async Task A_provider_in_its_own_process_establishes_invokes_and_reports_a_success()
    {
        using var process = StartProvider("--portable");
        try
        {
            await using var host = await PortableBindingHost.EstablishAsync(
                CoolingPortableFixture.Contract,
                Conversation(process, PortableLimits.Declared),
                "reference-cross-process");

            var result = await host.InvokeAsync(
                CoolingPortableFixture.SetEnabled,
                CoolingPortableFixture.CommandV1,
                CoolingPortableFixture.Command("primary", enabled: true),
                PortableTestHarness.Permitted(),
                [PortableTestHarness.Blob()]);

            Assert.Multiple(() =>
            {
                Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.OutcomeSucceeded));
                Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo(1));
                Assert.That(result.Observation.CopyCount, Is.EqualTo(1), "A copied blob crosses the process seam once.");
                Assert.That(result.Observation.CrossedBoundaries, Does.Contain("process"));
                Assert.That(host.Plan.Fact("framing"), Is.EqualTo("length-delimited"));
            });

            await host.WithdrawAsync();
            await host.TerminateAsync();
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await process.WaitForExitAsync();
        }
    }

    [Test]
    public async Task A_shaped_failure_crosses_the_process_boundary_without_transporting_an_exception()
    {
        using var process = StartProvider("--portable");
        try
        {
            await using var host = await PortableBindingHost.EstablishAsync(
                CoolingPortableFixture.Contract,
                Conversation(process, PortableLimits.Declared),
                "reference-cross-process");

            var result = await host.InvokeAsync(
                CoolingPortableFixture.SetEnabled,
                CoolingPortableFixture.CommandV1,
                CoolingPortableFixture.Command("primary", enabled: true, failureMode: "requested-failure"),
                PortableTestHarness.Permitted());

            Assert.Multiple(() =>
            {
                Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.OutcomeFailed));
                Assert.That(result.Category, Is.Null);
                Assert.That(((PortableRecordValue)result.Value!).Fields, Does.ContainKey("code"));
                Assert.That(result.Observation.FailureDomain, Is.EqualTo(PortableFailureDomain.RemoteProvider));
            });
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await process.WaitForExitAsync();
        }
    }
}
