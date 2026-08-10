using Brontide.Reference.Core;
using NUnit.Framework;

namespace Brontide.Reference.Conformance;

[TestFixture]
public sealed class Architecture08D2ConformanceTests
{
    private static readonly OperationReference Operation =
        new(CanonicalName.Parse("Brontide.Reference.Tests:Draft08.D2.Execute"));

    private static AuthorityFixture CreateFixture(params Constraint[] constraints)
    {
        ActorReference actorA = null!;
        ActorReference actorB = null!;
        ActorReference actorC = null!;
        ActorReference target = null!;
        Capability root = null!;
        var effects = 0;

        var domain = AuthorityDomain.Create("Brontide.Reference.Tests:Draft08.D2.Policy", genesis =>
        {
            actorA = genesis.Actor("ActorA");
            actorB = genesis.Actor("ActorB");
            actorC = genesis.Actor("ActorC");
            target = genesis.Actor("Target");
            genesis.Operation(
                Operation,
                target,
                ShapeContract.Unit,
                ShapeContract.Unit,
                "A08-D2 checked effect",
                _ =>
                {
                    effects++;
                    return OperationEffect.SucceededAsync(ShapeValue.Unit);
                });
            root = genesis.Grant(actorA, target, [Operation], constraints);
        });

        return new(domain, actorA, actorB, actorC, target, root, () => effects);
    }

    [Test]
    public async Task D2_C1_BR_08_ADV_C6_001_unadorned_capability_is_delegable_by_default()
    {
        var fixture = CreateFixture();
        var child = fixture.Root.Delegate(fixture.ActorB);

        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.ActorB,
            Operation,
            child,
            ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(fixture.Effects(), Is.EqualTo(1));
            Assert.That(child.Parent, Is.SameAs(fixture.Root));
            Assert.That(child.Target, Is.SameAs(fixture.Root.Target));
            Assert.That(child.RootOperations, Is.EqualTo(fixture.Root.RootOperations));
        });
    }

    [Test]
    public async Task D2_C2_BR_08_ADV_C6_002_delegation_depth_constraint_denies_every_descendant()
    {
        var fixture = CreateFixture(new DelegationDepthConstraint(0));
        var child = fixture.Root.Delegate(fixture.ActorB);
        var grandchild = child.Delegate(fixture.ActorC);

        var rootResult = await fixture.Domain.ExecuteDraft08Async(
            fixture.ActorA,
            Operation,
            fixture.Root,
            ShapeValue.Unit);

        var childResult = await fixture.Domain.ExecuteDraft08Async(
            fixture.ActorB,
            Operation,
            child,
            ShapeValue.Unit);
        var grandchildResult = await fixture.Domain.ExecuteDraft08Async(
            fixture.ActorC,
            Operation,
            grandchild,
            ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(rootResult.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(childResult.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(grandchildResult.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(childResult.Outcome.Message, Does.Contain("delegation depth"));
            Assert.That(grandchildResult.Outcome.Message, Does.Contain("delegation depth"));
            Assert.That(fixture.Effects(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task D2_C3_BR_08_ADV_C2_001_delegated_origin_is_capped_by_implicit_constraint()
    {
        var fixture = CreateFixture(new OriginGrantConstraint(OriginClass.Device));
        var child = fixture.Root.Delegate(fixture.ActorB);
        var grandchild = child.Delegate(fixture.ActorC);

        var spoofed = await fixture.Domain.ExecuteDraft08Async(
            fixture.ActorB,
            Operation,
            child,
            ShapeValue.Unit,
            OriginClass.Device);
        var derived = await fixture.Domain.ExecuteDraft08Async(
            fixture.ActorB,
            Operation,
            child,
            ShapeValue.Unit,
            OriginClass.Derived);
        var unverified = await fixture.Domain.ExecuteDraft08Async(
            fixture.ActorB,
            Operation,
            child,
            ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(spoofed.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(derived.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(derived.Outcome.Interaction.Origin, Is.EqualTo(OriginClass.Derived));
            Assert.That(unverified.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(unverified.Outcome.Interaction.Origin, Is.EqualTo(OriginClass.Unverified));
            Assert.That(child.AddedConstraintExpressions.OfType<OriginCeilingConstraint>().Single().Maximum,
                Is.EqualTo(OriginClass.Derived));
            Assert.That(grandchild.AddedConstraintExpressions.OfType<OriginCeilingConstraint>().Single().Maximum,
                Is.EqualTo(OriginClass.Derived));
            Assert.That(fixture.Effects(), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task D2_C4_BR_08_ADV_C2_002_primordial_origin_grant_remains_vouched()
    {
        var fixture = CreateFixture(new OriginGrantConstraint(OriginClass.Device));

        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.ActorA,
            Operation,
            fixture.Root,
            ShapeValue.Unit,
            OriginClass.Device);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(result.Outcome.Interaction.Origin, Is.EqualTo(OriginClass.Device));
            Assert.That(fixture.Root.AddedConstraintExpressions.OfType<OriginCeilingConstraint>(), Is.Empty);
        });
    }

    [Test]
    public async Task D2_C5_phase_property_denials_are_effect_free_and_boolean_surface_is_removed()
    {
        var fixture = CreateFixture(new DelegationDepthConstraint(0));
        var child = fixture.Root.Delegate(fixture.ActorB);

        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.ActorB,
            Operation,
            child,
            ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(fixture.Effects(), Is.Zero);
            Assert.That(typeof(Capability).GetProperty("DelegationAllowed"), Is.Null);
            Assert.That(typeof(AuthorityDomain.GenesisContext).GetMethods()
                .Where(method => method.Name is "Grant" or "GrantExpressions")
                .SelectMany(method => method.GetParameters())
                .Any(parameter => parameter.Name == "delegable"), Is.False);
        });
    }

    private sealed record AuthorityFixture(
        AuthorityDomain Domain,
        ActorReference ActorA,
        ActorReference ActorB,
        ActorReference ActorC,
        ActorReference Target,
        Capability Root,
        Func<int> Effects);
}
