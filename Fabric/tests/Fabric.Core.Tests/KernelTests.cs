using Fabric.Core;

namespace Fabric.Core.Tests;

public sealed class KernelTests
{
    [Test]
    public async Task Liveness_lease_dies_without_renewal_and_cannot_be_resurrected()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        ActorReference grantor = null!;
        ActorReference holder = null!;
        ActorReference target = null!;
        Capability capability = null!;
        LivenessLease lease = null!;
        var operation = OperationReference.Parse("Example.Tick");

        var domain = AuthorityDomain.Create("leases", clock, genesis =>
        {
            grantor = genesis.Actor("Grantor");
            holder = genesis.Actor("Holder");
            target = genesis.Actor("Target");
            genesis.Operation(operation, target, ShapeContract.Unit, ShapeContract.Unit, "tick",
                _ => OperationEffect.SucceededAsync(ShapeValue.Unit));
            lease = genesis.Lease(grantor, TimeSpan.FromSeconds(5));
            capability = genesis.Grant(holder, target, [operation], [new LivenessLeaseConstraint(lease)]);
        });

        Assert.That((await domain.ExecuteAsync(holder, operation, capability, ShapeValue.Unit)).Outcome.Status,
            Is.EqualTo(OutcomeStatus.Succeeded));
        clock.Advance(TimeSpan.FromSeconds(4));
        Assert.That(lease.Renew(grantor), Is.True);
        clock.Advance(TimeSpan.FromSeconds(4));
        Assert.That((await domain.ExecuteAsync(holder, operation, capability, ShapeValue.Unit)).Outcome.Status,
            Is.EqualTo(OutcomeStatus.Succeeded));
        clock.Advance(TimeSpan.FromSeconds(2));
        Assert.That((await domain.ExecuteAsync(holder, operation, capability, ShapeValue.Unit)).Outcome.Status,
            Is.EqualTo(OutcomeStatus.Rejected));
        Assert.That(lease.Renew(grantor), Is.False);
    }

    [Test]
    public async Task Wall_clock_constraint_fails_closed_when_domain_has_no_clock()
    {
        ActorReference holder = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var operation = OperationReference.Parse("Example.ClockBound");
        var domain = AuthorityDomain.Create("clockless", genesis =>
        {
            holder = genesis.Actor("Holder");
            target = genesis.Actor("Target");
            genesis.Operation(operation, target, ShapeContract.Unit, ShapeContract.Unit, "clock-bound",
                _ => OperationEffect.SucceededAsync(ShapeValue.Unit));
            capability = genesis.Grant(holder, target, [operation],
                [new WallClockValidityConstraint(notAfter: DateTimeOffset.MaxValue)]);
        });

        var result = await domain.ExecuteAsync(holder, operation, capability, ShapeValue.Unit);

        Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
        Assert.That(result.Outcome.Message, Does.Contain("no trusted clock").IgnoreCase);
    }

    [Test]
    public void Captured_genesis_context_cannot_mint_authority_later()
    {
        AuthorityDomain.GenesisContext captured = null!;
        _ = AuthorityDomain.Create("genesis", genesis => captured = genesis);

        Assert.That(() => captured.Actor("late actor"), Throws.InvalidOperationException);
    }

    [Test]
    public void Registry_supports_all_base_shape_kinds_without_CLR_assignability()
    {
        var registry = ShapeRegistry.CreateWithBuiltIns();
        var sequence = ShapeReference.Parse("Example.Texts", 1);
        var choice = ShapeReference.Parse("Example.Choice", 1);
        var opaque = ShapeReference.Parse("Example.Opaque", 1);
        registry.Register(ShapeDefinition.Sequence(sequence, BuiltInShapes.Text));
        registry.Register(ShapeDefinition.Choice(choice,
            new KeyValuePair<string, ShapeReference>("text", BuiltInShapes.Text),
            new KeyValuePair<string, ShapeReference>("number", BuiltInShapes.Signed64)));
        registry.Register(ShapeDefinition.Opaque(opaque));

        Assert.That(registry.Project(
            ShapeValue.Sequence(sequence, ShapeValue.Text("a"), ShapeValue.Text("b")),
            ShapeContract.For(sequence)).IsValid, Is.True);
        Assert.That(registry.Project(
            ShapeValue.Choice(choice, "number", ShapeValue.Signed64(3)),
            ShapeContract.For(choice)).IsValid, Is.True);
        Assert.That(registry.Project(
            ShapeValue.Opaque(opaque, [1, 2, 3]),
            ShapeContract.For(opaque)).IsValid, Is.True);
        Assert.That(registry.Project(
            ShapeValue.Scalar(BuiltInShapes.Signed64, 3),
            ShapeContract.For(BuiltInShapes.Text)).IsValid, Is.False);
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
