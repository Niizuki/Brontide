using Fabric.Core;
using Fabric.Vocabularies.Cooling;

namespace Fabric.Conformance;

public sealed class CoolingConformanceTests
{
    [Test]
    [SpecSection("14")]
    [SpecSection("17")]
    public async Task Minimal_cooling_system_runs_only_through_checked_executions()
    {
        var scenario = await CoolingScenario.RunAsync();

        Assert.Multiple(() =>
        {
            Assert.That(scenario.ExecutionCountImmediatelyAfterEvent, Is.Zero,
                "receiving an Event must not initiate an Execution");
            Assert.That(scenario.EffectsImmediatelyAfterEvent, Is.Zero);
            Assert.That(scenario.TemperatureRead.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(scenario.AcceptedSetSpeed.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(scenario.DeniedSetSpeed.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(scenario.EmergencyStop.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(scenario.EmergencyGrant.Parent, Is.Not.Null);
            Assert.That(scenario.FinalFanSpeed, Is.Zero);
            Assert.That(scenario.TotalEffects, Is.EqualTo(2));
            Assert.That(scenario.Transcript.Length, Is.EqualTo(5));
            Assert.That(scenario.Domain.Provenance.Count(entry => entry.Kind == ProvenanceKind.Execution),
                Is.EqualTo(4));
        });
    }
}
