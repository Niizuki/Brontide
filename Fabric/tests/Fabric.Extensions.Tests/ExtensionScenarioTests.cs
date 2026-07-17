using Fabric.Core;
using Fabric.Extensions.Events;
using Fabric.Extensions.Flow;

namespace Fabric.Extensions.Tests;

[Category("Experimental")]
public sealed class ExtensionScenarioTests
{
    [Test]
    public void Flow_open_staging_rejects_foreign_participants_and_unregistered_item_shapes()
    {
        ActorReference producer = null!;
        ActorReference consumer = null!;
        ActorReference foreignConsumer = null!;
        var domain = AuthorityDomain.Create("flow-boundary", genesis =>
        {
            producer = genesis.Actor("Producer");
            consumer = genesis.Actor("Consumer");
        });
        _ = AuthorityDomain.Create("foreign-flow-boundary", genesis =>
        {
            foreignConsumer = genesis.Actor("ForeignConsumer");
        });
        var runtime = new FlowRuntime();
        var recovery = FlowRecoveryContract.Recoverable();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => runtime.StageOpen(
                    domain,
                    producer,
                    foreignConsumer,
                    ShapeContract.For(BuiltInShapes.Text),
                    recovery),
                Throws.InvalidOperationException.With.Message.Contains("authority domain"));
            Assert.That(
                () => runtime.StageOpen(
                    domain,
                    producer,
                    consumer,
                    ShapeContract.For(ShapeReference.Parse("Unknown.Flow.Item", 1)),
                    recovery),
                Throws.InvalidOperationException.With.Message.Contains("recognised"));
        });
    }

    [Test]
    public async Task Event_mediator_gates_publish_and_observe_and_preserves_emitters_on_replay()
    {
        var scenario = await EventDistributionScenario.RunAsync();

        Assert.Multiple(() =>
        {
            Assert.That(scenario.ObserveFirst.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(scenario.ObserveSecond.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(scenario.UnauthorizedPublish.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(scenario.Publish.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(scenario.Subscriptions.Length, Is.EqualTo(2));
            Assert.That(scenario.Subscriptions[0].Received.Count, Is.EqualTo(2));
            Assert.That(scenario.Subscriptions[1].Received.Count, Is.EqualTo(1));
            Assert.That(scenario.Replay.Interaction.Actor, Is.SameAs(scenario.Original.Interaction.Actor));
            Assert.That(scenario.Replay.Interaction.Id, Is.EqualTo(scenario.Original.Interaction.Id));
            Assert.That(scenario.Replay.Interaction.Origin, Is.EqualTo(OriginClass.Derived));
            Assert.That(scenario.Replay.IsReplay, Is.True);
        });
    }

    [Test]
    public async Task Pointer_flow_authorises_items_detects_a_gap_and_replays_the_original_source_position()
    {
        var scenario = await PointerFlowScenario.RunAsync();

        Assert.Multiple(() =>
        {
            Assert.That(scenario.Open.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(scenario.SpoofedPublication.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(scenario.Publications.All(publication =>
                publication.Outcome.Status == OutcomeStatus.Succeeded), Is.True);
            Assert.That(scenario.InitialRead.Items.Select(item => item.SourcePosition), Is.EqualTo(new long[] { 1, 3 }));
            Assert.That(scenario.InitialRead.Gaps.Length, Is.EqualTo(1));
            Assert.That(scenario.InitialRead.Gaps[0].FromPosition, Is.EqualTo(2));
            Assert.That(scenario.ReplayExecution.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(scenario.ReplayExecution.Events.Single().Kind, Is.EqualTo(FlowContracts.GapDetected));
            Assert.That(scenario.Replay.Items.Length, Is.EqualTo(1));
            Assert.That(scenario.Replay.Items[0].SourcePosition, Is.EqualTo(2));
            Assert.That(scenario.Replay.Items[0].IsReplay, Is.True);
            Assert.That(scenario.Replay.Items[0].Interaction.Origin, Is.EqualTo(OriginClass.Derived));
            Assert.That(scenario.Replay.Items[0].Interaction.Actor,
                Is.SameAs(scenario.Published[1].Interaction.Actor));
            Assert.That(scenario.Replay.Gaps, Is.Empty);
            Assert.That(scenario.Domain.Provenance.Count(entry => entry.Kind == ProvenanceKind.Execution),
                Is.EqualTo(6));
        });
    }
}
