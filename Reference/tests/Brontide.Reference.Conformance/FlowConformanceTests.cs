using Brontide.Reference.Core;
using Brontide.Reference.Extensions.Flow;

namespace Brontide.Reference.Conformance;

public sealed class FlowConformanceTests
{
    [Test]
    [SpecSection("15")]
    public async Task Flow_occurrences_cannot_spoof_device_origin_and_replay_is_derived()
    {
        var scenario = await PointerFlowScenario.RunAsync();

        Assert.Multiple(() =>
        {
            Assert.That(scenario.Open.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(scenario.SpoofedPublication.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(scenario.Published, Has.Length.EqualTo(3));
            Assert.That(scenario.Published.All(item => item.Interaction.Origin == OriginClass.Device), Is.True);
            Assert.That(scenario.Replay.Items.Single().Interaction.Actor,
                Is.SameAs(scenario.Published[1].Interaction.Actor));
            Assert.That(scenario.Replay.Items.Single().Interaction.Origin, Is.EqualTo(OriginClass.Derived));
        });
    }
}
