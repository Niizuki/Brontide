using System.Diagnostics;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// PB5 / PB-61: this host establishes and invokes against a provider that depends on neither stack.
/// </summary>
/// <remarks>
/// The endpoint under <c>binding/neutral-provider/</c> imports no Brontide assembly. It answers with
/// the contract transcoded from the checked-in neutral declaration rather than one restated in its
/// own source, and it reads and writes the wire with the base class library's CBOR codec rather than
/// with either stack's. Passing the same parity matrix against it is what turns "the contract is
/// implementable without importing either private model" from a claim into evidence.
/// </remarks>
[Category("CrossProcess")]
[Category("NeutralProvider")]
public sealed class PortableNeutralProviderTests
{
    public static IEnumerable<PortableParityScenario> Scenarios => PortableParityMatrix.Scenarios;

    private static Process StartNeutralProvider(params string[] arguments)
    {
        var path = Environment.GetEnvironmentVariable("BRONTIDE_NEUTRAL_PROVIDER");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Assert.Ignore("BRONTIDE_NEUTRAL_PROVIDER does not name a built implementation-neutral provider.");
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
        }) ?? throw new InvalidOperationException("The neutral provider process did not start.");
    }

    private static PortableProcessConversation Conversation(Process process) =>
        new(
            new PortableStreamDuplex(
                process.StandardOutput.BaseStream,
                process.StandardInput.BaseStream,
                PortableLimits.Declared,
                ownsStreams: false),
            PortableLimits.Declared);

    private static async Task StopAsync(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }

        await process.WaitForExitAsync();
    }

    [TestCaseSource(nameof(Scenarios))]
    public async Task A_provider_depending_on_neither_stack_reaches_the_same_observations(
        PortableParityScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var native = await PortableRealizationParityTests.RunAsync(scenario, direct: true);

        using var provider = StartNeutralProvider([.. scenario.ProviderArguments]);
        try
        {
            await using var host = await PortableBindingHost.EstablishAsync(
                scenario.Contract,
                Conversation(provider),
                "reference-hosts-neutral");

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
    /// The neutral endpoint negotiates the contract it was given rather than one it invented: the
    /// established plan carries the same declared facts the stack's own negotiation produces.
    /// </summary>
    [Test]
    public async Task The_negotiated_plan_matches_the_one_this_stack_negotiates_with_itself()
    {
        await using var own = await PortableTestHarness.DirectHostAsync();
        using var provider = StartNeutralProvider("--portable");
        try
        {
            await using var host = await PortableBindingHost.EstablishAsync(
                CoolingPortableFixture.Contract,
                Conversation(provider),
                "reference-hosts-neutral");

            var permitted = new[] { "realization", "framing", "crossedBoundaries", "planId", "hostEndpoint" };
            var differing = own.Plan.Facts
                .Where(fact => host.Plan.Facts[fact.Key] != fact.Value)
                .Select(fact => fact.Key);

            Assert.That(differing, Is.SubsetOf(permitted));
        }
        finally
        {
            await StopAsync(provider);
        }
    }
}
