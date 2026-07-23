using Brontide.Reference.Core;

namespace Brontide.Reference.Core.Tests;

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
        clock.Advance(TimeSpan.FromSeconds(-2));
        Assert.That((await domain.ExecuteAsync(holder, operation, capability, ShapeValue.Unit)).Outcome.Status,
            Is.EqualTo(OutcomeStatus.Rejected));
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
    public async Task Failed_genesis_occurrence_rolls_back_unrecorded_authority()
    {
        ActorReference policy = null!;
        ActorReference target = null!;
        Capability existingCapability = null!;
        ActorReference escapedActor = null!;
        Capability escapedCapability = null!;
        LivenessLease escapedLease = null!;
        var runtimeAccessBlocked = false;
        var effects = 0;
        var operation = OperationReference.Parse("Example.Existing");
        var domain = AuthorityDomain.Create("genesis-rollback", genesis =>
        {
            policy = genesis.Actor("Policy");
            target = genesis.Actor("Target");
            genesis.Operation(operation, target, ShapeContract.Unit, ShapeContract.Unit, "existing",
                _ =>
                {
                    effects++;
                    return OperationEffect.SucceededAsync(ShapeValue.Unit);
                });
            existingCapability = genesis.Grant(policy, target, [operation]);
        });
        var actorCount = domain.Actors.Count;
        var capabilityCount = domain.Capabilities.Count;
        var provenanceCount = domain.Provenance.Count;
        var shapeCount = domain.Shapes.Shapes.Count;
        var transientShape = ShapeReference.Parse("Example.Transient", 1);

        Assert.That(() => domain.OccurGenesis(policy, "attachment", "test rollback", genesis =>
        {
            escapedActor = genesis.Actor("Attached");
            genesis.Shape(ShapeDefinition.Unit(transientShape));
            escapedLease = genesis.Lease(escapedActor, TimeSpan.FromMinutes(1));
            escapedCapability = genesis.Grant(policy, target, [operation]);
            try
            {
                _ = domain.ExecuteAsync(policy, operation, existingCapability, ShapeValue.Unit)
                    .AsTask().GetAwaiter().GetResult();
            }
            catch (InvalidOperationException)
            {
                runtimeAccessBlocked = true;
            }

            throw new InvalidOperationException("simulated policy failure");
        }), Throws.InvalidOperationException);

        Assert.Multiple(() =>
        {
            Assert.That(domain.Actors, Has.Count.EqualTo(actorCount));
            Assert.That(domain.Capabilities, Has.Count.EqualTo(capabilityCount));
            Assert.That(domain.GenesisOccurrences, Is.Empty);
            Assert.That(domain.Provenance, Has.Count.EqualTo(provenanceCount));
            Assert.That(domain.Shapes.Shapes, Has.Count.EqualTo(shapeCount));
            Assert.That(domain.Shapes.Shapes.Any(shape => shape.Reference == transientShape), Is.False);
            Assert.That(runtimeAccessBlocked, Is.True);
            Assert.That(effects, Is.Zero);
        });

        var escapedActorResult = await domain.ExecuteAsync(
            escapedActor, operation, existingCapability, ShapeValue.Unit);
        var escapedCapabilityResult = await domain.ExecuteAsync(
            policy, operation, escapedCapability, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(escapedActorResult.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(escapedCapabilityResult.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(() => escapedCapability.Delegate(policy), Throws.InvalidOperationException);
            Assert.That(effects, Is.Zero);
        });

        Assert.That(() => domain.OccurGenesis(policy, "attachment", "reject escaped lease", genesis =>
            _ = genesis.Grant(
                policy,
                target,
                [operation],
                [new LivenessLeaseConstraint(escapedLease)])), Throws.InvalidOperationException);
        Assert.That(domain.Capabilities, Has.Count.EqualTo(capabilityCount));
    }

    [Test]
    public async Task Failed_genesis_occurrence_rolls_back_renewal_of_an_existing_lease()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        ActorReference policy = null!;
        ActorReference target = null!;
        Capability capability = null!;
        LivenessLease lease = null!;
        var effects = 0;
        var operation = OperationReference.Parse("Example.ExistingLease");
        var domain = AuthorityDomain.Create("genesis-existing-lease-rollback", clock, genesis =>
        {
            policy = genesis.Actor("Policy");
            target = genesis.Actor("Target");
            genesis.Operation(operation, target, ShapeContract.Unit, ShapeContract.Unit, "existing lease",
                _ =>
                {
                    effects++;
                    return OperationEffect.SucceededAsync(ShapeValue.Unit);
                });
            lease = genesis.Lease(policy, TimeSpan.FromSeconds(5));
            capability = genesis.Grant(
                policy,
                target,
                [operation],
                [new LivenessLeaseConstraint(lease)]);
        });

        var originalExpiry = lease.ExpiresAt;
        clock.Advance(TimeSpan.FromSeconds(4));
        Assert.That(() => domain.OccurGenesis(policy, "attachment", "failed lease renewal", _ =>
        {
            Assert.That(lease.Renew(policy), Is.True);
            throw new InvalidOperationException("simulated policy failure");
        }), Throws.InvalidOperationException);

        clock.Advance(TimeSpan.FromSeconds(2));
        var result = await domain.ExecuteAsync(policy, operation, capability, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(domain.GenesisOccurrences, Is.Empty);
            Assert.That(lease.ExpiresAt, Is.EqualTo(originalExpiry));
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(effects, Is.Zero);
        });
    }

    [Test]
    public async Task Rejected_provenance_excludes_the_protected_input()
    {
        ActorReference holder = null!;
        ActorReference stranger = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var operation = OperationReference.Parse("Example.Protected");
        var domain = AuthorityDomain.Create("protected-audit", genesis =>
        {
            holder = genesis.Actor("Holder");
            stranger = genesis.Actor("Stranger");
            target = genesis.Actor("Target");
            genesis.Operation(operation, target, ShapeContract.For(BuiltInShapes.Text), ShapeContract.Unit,
                "protected", _ => OperationEffect.SucceededAsync(ShapeValue.Unit));
            capability = genesis.Grant(holder, target, [operation]);
        });
        var protectedInput = ShapeValue.Text("do-not-log");

        var result = await domain.ExecuteAsync(stranger, operation, capability, protectedInput);
        var audit = domain.Provenance.Single(entry => entry.Kind == ProvenanceKind.Execution);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(result.Execution.Input, Is.SameAs(protectedInput));
            Assert.That(audit.Execution!.Id, Is.EqualTo(result.Execution.Id));
            Assert.That(audit.Execution.HasInput, Is.False);
            Assert.That(() => _ = audit.Execution.Input, Throws.InvalidOperationException);
        });
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

    [Test]
    public void Scalar_carriers_are_typed_immutable_and_cannot_contain_authority()
    {
        ActorReference holder = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var operation = OperationReference.Parse("Example.Authority");
        _ = AuthorityDomain.Create("scalar-authority", genesis =>
        {
            holder = genesis.Actor("Holder");
            target = genesis.Actor("Target");
            genesis.Operation(operation, target, ShapeContract.Unit, ShapeContract.Unit, "authority",
                _ => OperationEffect.SucceededAsync(ShapeValue.Unit));
            capability = genesis.Grant(holder, target, [operation]);
        });
        var registry = ShapeRegistry.CreateWithBuiltIns();
        var semanticText = ShapeReference.Parse("Example.SemanticText", 1);
        registry.Register(ShapeDefinition.Scalar<string>(semanticText));

        Assert.Multiple(() =>
        {
            Assert.That(() => ShapeDefinition.Scalar<Capability>(ShapeReference.Parse("Example.Capability", 1)),
                Throws.ArgumentException);
            Assert.That(() => ShapeValue.Scalar(BuiltInShapes.Text, capability), Throws.ArgumentException);
            Assert.That(() => ShapeValue.Scalar(semanticText, new HashSet<string>()), Throws.ArgumentException);
            Assert.That(registry.Project(
                ShapeValue.Scalar(semanticText, 42L),
                ShapeContract.For(semanticText)).IsValid, Is.False);
        });
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
