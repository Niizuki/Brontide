using Brontide.Reference.Core;
using NUnit.Framework;

namespace Brontide.Reference.Conformance;

[TestFixture]
public sealed class Architecture08D4ConformanceTests
{
    private static readonly OperationReference Execute = new(CanonicalName.Parse("Example:ExecuteD4"));
    private static readonly CanonicalName RemoteLiveness = CanonicalName.Parse("Example:RemoteLiveness");
    private static readonly CanonicalName UnrelatedPolicy = CanonicalName.Parse("Example:UnrelatedPolicy");
    private static readonly CanonicalName PerHolderRate = CanonicalName.Parse("Example:PerHolderRate");

    [Test]
    public async Task D4_C1_BR_08_ADV_C1_001_expired_ancestor_liveness_denies_before_effect()
    {
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var fixture = CreateLivenessFixture(clock);
        clock.Advance(TimeSpan.FromSeconds(6));

        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.ChildHolder, Execute, fixture.Child, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(result.Decisions.Any(decision => decision.ConstraintName == StandardConstraintNames.LivenessLease), Is.True);
            Assert.That(fixture.Effects(), Is.Zero);
        });
    }

    [Test]
    public async Task D4_C2_BR_08_ADV_C1_002_unavailable_liveness_evaluator_denies_with_redacted_category()
    {
        var effects = 0;
        var fixture = CreateCustomFixture(
            Declaration(RemoteLiveness, ConstraintAccountingScope.NotQuantified, "remote liveness scope is active"),
            evaluator: null,
            new ValueConstraint(RemoteLiveness, ShapeValue.Text("sensitive-scope-reference")),
            () => effects++);

        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.Holder, Execute, fixture.Capability, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(result.Outcome.Message, Does.Contain(RemoteLiveness.ToString()));
            Assert.That(result.Outcome.Message, Does.Contain("UnsupportedConstraint"));
            Assert.That(result.Outcome.Message, Does.Not.Contain("sensitive-scope-reference"));
            Assert.That(effects, Is.Zero);
        });
    }

    [Test]
    public async Task D4_C3_BR_08_ADV_C1_003_live_ancestor_authorises_exactly_one_effect()
    {
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var fixture = CreateLivenessFixture(clock);

        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.ChildHolder, Execute, fixture.Child, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(fixture.Effects(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task D4_C4_BR_08_ADV_C5_001_sibling_delegations_share_the_ancestor_occurrence_budget()
    {
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var fixture = CreateRateFixture(clock, maximum: 2);

        var first = await fixture.Domain.ExecuteDraft08Async(fixture.FirstHolder, Execute, fixture.First, ShapeValue.Unit);
        var second = await fixture.Domain.ExecuteDraft08Async(fixture.FirstHolder, Execute, fixture.First, ShapeValue.Unit);
        var sibling = await fixture.Domain.ExecuteDraft08Async(fixture.SecondHolder, Execute, fixture.Second, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(first.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(second.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(sibling.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(fixture.Effects(), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task D4_C5_BR_08_ADV_C5_002_denied_executions_consume_no_budget()
    {
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var unrelatedAllows = false;
        var effects = 0;
        var fixture = CreateCustomFixture(
            Declaration(UnrelatedPolicy, ConstraintAccountingScope.NotQuantified, "unrelated policy is satisfied"),
            (constraint, _) => unrelatedAllows
                ? ConstraintDecision.Allow(constraint.Name, "unrelated policy satisfied")
                : ConstraintDecision.Deny(constraint.Name, "unrelated policy denied"),
            [new ExecutionRateLimitConstraint(1, TimeSpan.FromMinutes(1)), new ValueConstraint(UnrelatedPolicy, ShapeValue.Text("check"))],
            () => effects++,
            clock);

        var denied1 = await fixture.Domain.ExecuteDraft08Async(fixture.Holder, Execute, fixture.Capability, ShapeValue.Unit);
        var denied2 = await fixture.Domain.ExecuteDraft08Async(fixture.Holder, Execute, fixture.Capability, ShapeValue.Unit);
        unrelatedAllows = true;
        var allowed = await fixture.Domain.ExecuteDraft08Async(fixture.Holder, Execute, fixture.Capability, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(denied1.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(denied2.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(allowed.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(effects, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task D4_C6_BR_08_ADV_C5_003_unenforceable_vocabulary_scope_denies_before_evaluator()
    {
        var evaluatorCalls = 0;
        var effects = 0;
        var fixture = CreateCustomFixture(
            Declaration(PerHolderRate, ConstraintAccountingScope.VocabularyDefined, "one execution per holder"),
            (constraint, _) =>
            {
                evaluatorCalls++;
                return ConstraintDecision.Allow(constraint.Name, "would allow");
            },
            new ValueConstraint(PerHolderRate, ShapeValue.Text("1")),
            () => effects++);

        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.Holder, Execute, fixture.Capability, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(fixture.Domain.ConstraintRecognitionSet.Single(item => item.Declaration.Name == PerHolderRate).Decision,
                Is.EqualTo(ConstraintRecognitionDecision.Declined));
            Assert.That(evaluatorCalls, Is.Zero);
            Assert.That(effects, Is.Zero);
        });
    }

    private static LivenessFixture CreateLivenessFixture(ManualTimeProvider clock)
    {
        ActorReference grantor = null!;
        ActorReference childHolder = null!;
        ActorReference target = null!;
        Capability root = null!;
        var effects = 0;
        var domain = AuthorityDomain.Create("Brontide.Reference.Tests:D4.Liveness", clock, genesis =>
        {
            grantor = genesis.Actor("Grantor");
            childHolder = genesis.Actor("ChildHolder");
            target = genesis.Actor("Target");
            genesis.Operation(Execute, target, ShapeContract.Unit, ShapeContract.Unit, "D4 effect", _ =>
            {
                effects++;
                return OperationEffect.SucceededAsync(ShapeValue.Unit);
            });
            var lease = genesis.Lease(grantor, TimeSpan.FromSeconds(5));
            root = genesis.Grant(grantor, target, [Execute], [new LivenessLeaseConstraint(lease)]);
        });
        var child = root.Delegate(childHolder);
        return new(domain, childHolder, child, () => effects);
    }

    private static RateFixture CreateRateFixture(ManualTimeProvider clock, long maximum)
    {
        ActorReference grantor = null!;
        ActorReference firstHolder = null!;
        ActorReference secondHolder = null!;
        ActorReference target = null!;
        Capability root = null!;
        var effects = 0;
        var domain = AuthorityDomain.Create("Brontide.Reference.Tests:D4.Rate", clock, genesis =>
        {
            grantor = genesis.Actor("Grantor");
            firstHolder = genesis.Actor("FirstHolder");
            secondHolder = genesis.Actor("SecondHolder");
            target = genesis.Actor("Target");
            genesis.Operation(Execute, target, ShapeContract.Unit, ShapeContract.Unit, "D4 effect", _ =>
            {
                effects++;
                return OperationEffect.SucceededAsync(ShapeValue.Unit);
            });
            root = genesis.Grant(grantor, target, [Execute],
                [new ExecutionRateLimitConstraint(maximum, TimeSpan.FromMinutes(1))]);
        });
        return new(
            domain,
            firstHolder,
            root.Delegate(firstHolder),
            secondHolder,
            root.Delegate(secondHolder),
            () => effects);
    }

    private static CustomFixture CreateCustomFixture(
        ConstraintDeclaration declaration,
        ConstraintEvaluator? evaluator,
        Constraint constraint,
        Action effect,
        TimeProvider? clock = null) =>
        CreateCustomFixture(declaration, evaluator, [constraint], effect, clock);

    private static CustomFixture CreateCustomFixture(
        ConstraintDeclaration declaration,
        ConstraintEvaluator? evaluator,
        IEnumerable<Constraint> constraints,
        Action effect,
        TimeProvider? clock = null)
    {
        ActorReference holder = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var domain = AuthorityDomain.Create("Brontide.Reference.Tests:D4.Custom", clock, genesis =>
        {
            holder = genesis.Actor("Holder");
            target = genesis.Actor("Target");
            genesis.Constraint(declaration, evaluator);
            genesis.Operation(Execute, target, ShapeContract.Unit, ShapeContract.Unit, "D4 effect", _ =>
            {
                effect();
                return OperationEffect.SucceededAsync(ShapeValue.Unit);
            });
            capability = genesis.Grant(holder, target, [Execute], constraints);
        });
        return new(domain, holder, capability);
    }

    private static ConstraintDeclaration Declaration(
        CanonicalName name,
        ConstraintAccountingScope scope,
        string semantics) =>
        ConstraintDeclaration.Create(name, ShapeContract.For(BuiltInShapes.Text), semantics, accountingScope: scope);

    private sealed record LivenessFixture(
        AuthorityDomain Domain,
        ActorReference ChildHolder,
        Capability Child,
        Func<int> Effects);

    private sealed record RateFixture(
        AuthorityDomain Domain,
        ActorReference FirstHolder,
        Capability First,
        ActorReference SecondHolder,
        Capability Second,
        Func<int> Effects);

    private sealed record CustomFixture(AuthorityDomain Domain, ActorReference Holder, Capability Capability);

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
