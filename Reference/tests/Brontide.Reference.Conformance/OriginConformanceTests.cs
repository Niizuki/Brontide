using Brontide.Reference.Core;

namespace Brontide.Reference.Conformance;

public sealed class OriginConformanceTests
{
    [Test]
    [SpecSection("10.1")]
    [SpecSection("10.3")]
    [SpecSection("15")]
    public void Direct_origin_assertion_evaluates_mortality_and_fails_closed()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        ActorReference device = null!;
        ActorReference target = null!;
        Capability originAuthority = null!;
        var changed = EventReference.Parse("Device.Changed");
        var operation = OperationReference.Parse("Device.Read");
        var domain = AuthorityDomain.Create("origin-mortality", clock, genesis =>
        {
            device = genesis.Actor("Device");
            target = genesis.Actor("Target");
            genesis.Event(changed, ShapeContract.For(BuiltInShapes.Text), "device changed");
            genesis.Operation(operation, target, ShapeContract.Unit, ShapeContract.Unit, "read",
                _ => OperationEffect.SucceededAsync(ShapeValue.Unit));
            var lease = genesis.Lease(target, TimeSpan.FromSeconds(5));
            originAuthority = genesis.Grant(
                device,
                target,
                [operation],
                [new LivenessLeaseConstraint(lease), new OriginGrantConstraint(OriginClass.Device)]);
        });

        var accepted = domain.EmitEvent(
            device,
            changed,
            ShapeValue.Text("online"),
            OriginClass.Device,
            originAuthority);
        clock.Advance(TimeSpan.FromSeconds(5));

        Assert.That(accepted.Interaction.Origin, Is.EqualTo(OriginClass.Device));
        Assert.That(
            () => domain.EmitEvent(
                device,
                changed,
                ShapeValue.Text("still online"),
                OriginClass.Device,
                originAuthority),
            Throws.TypeOf<BrontideDenialException>().With.Message.Contains("expired"));
        Assert.That(domain.Provenance.Count(entry => entry.Kind == ProvenanceKind.Event), Is.EqualTo(1));
    }

    [Test]
    [SpecSection("10.1")]
    [SpecSection("15")]
    public void Direct_origin_assertion_denies_constraints_without_event_semantics()
    {
        ActorReference device = null!;
        ActorReference target = null!;
        Capability originAuthority = null!;
        var changed = EventReference.Parse("Device.Changed");
        var operation = OperationReference.Parse("Device.Read");
        var domain = AuthorityDomain.Create("origin-fail-closed", TimeProvider.System, genesis =>
        {
            device = genesis.Actor("Device");
            target = genesis.Actor("Target");
            genesis.Event(changed, ShapeContract.For(BuiltInShapes.Text), "device changed");
            genesis.Operation(operation, target, ShapeContract.Unit, ShapeContract.Unit, "read",
                _ => OperationEffect.SucceededAsync(ShapeValue.Unit));
            originAuthority = genesis.Grant(
                device,
                target,
                [operation],
                [
                    new OriginGrantConstraint(OriginClass.Device),
                    new ValueConstraint(CanonicalName.Parse("Example:Unrecognised"), ShapeValue.Text("x"))
                ]);
        });

        Assert.That(
            () => domain.EmitEvent(
                device,
                changed,
                ShapeValue.Text("online"),
                OriginClass.Device,
                originAuthority),
            Throws.TypeOf<BrontideDenialException>().With.Message.Contains("fail-closed"));
        Assert.That(domain.Provenance, Is.Empty);
    }

    [Test]
    [SpecSection("13.5")]
    [SpecSection("15")]
    public async Task Completed_execution_context_cannot_reuse_an_old_origin_authorisation()
    {
        ActorReference device = null!;
        ActorReference target = null!;
        Capability capability = null!;
        Brontide.Reference.Core.ExecutionContext captured = null!;
        var read = OperationReference.Parse("Device.Read");
        var changed = EventReference.Parse("Device.Changed");
        var domain = AuthorityDomain.Create("origin-context-lifetime", genesis =>
        {
            device = genesis.Actor("Device");
            target = genesis.Actor("Target");
            genesis.Event(changed, ShapeContract.For(BuiltInShapes.Text), "device changed");
            genesis.Operation(read, target, ShapeContract.Unit, ShapeContract.Unit, "read", context =>
            {
                captured = context;
                return OperationEffect.SucceededAsync(ShapeValue.Unit);
            });
            capability = genesis.Grant(
                device,
                target,
                [read],
                [new OriginGrantConstraint(OriginClass.Device)]);
        });

        var execution = await domain.ExecuteAsync(
            device,
            read,
            capability,
            ShapeValue.Unit,
            OriginClass.Device);

        Assert.That(execution.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
        Assert.That(
            () => captured.EmitEventFromInitiator(
                changed,
                ShapeValue.Text("late"),
                OriginClass.Device,
                capability),
            Throws.InvalidOperationException.With.Message.Contains("handler is active"));
        Assert.That(domain.Provenance.Any(entry => entry.Kind == ProvenanceKind.Event), Is.False);
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
