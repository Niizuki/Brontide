using Brontide.Reference.Core;

namespace Brontide.Reference.Conformance;

public sealed class Architecture08D1ConformanceTests
{
    private static readonly CanonicalName UnknownName = CanonicalName.Parse("Example:Draft08.Unknown");

    [Test]
    public async Task BR_08_ADV_C7_001_not_unknown_denies_before_effect()
    {
        var observation = await ExecuteExpressionAsync((_, _) =>
            new NotConstraintExpression(Unknown()));

        AssertDeniedWithUnknown(observation);
    }

    [Test]
    public async Task BR_08_ADV_C7_002_any_true_unknown_authorizes_and_records_unknown()
    {
        var observation = await ExecuteExpressionAsync((allowed, _) =>
            new AnyOfConstraintExpression(new PermittedOperationsConstraint(allowed), Unknown()));

        Assert.Multiple(() =>
        {
            Assert.That(observation.Result.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(observation.Effects, Is.EqualTo(1));
            Assert.That(observation.Result.Decisions.Single().UnsupportedConstraints, Does.Contain(UnknownName));
        });
    }

    [Test]
    public async Task BR_08_ADV_C7_003_all_true_unknown_denies_before_effect()
    {
        var observation = await ExecuteExpressionAsync((allowed, _) =>
            new AllOfConstraintExpression(new PermittedOperationsConstraint(allowed), Unknown()));

        AssertDeniedWithUnknown(observation);
    }

    [Test]
    public async Task BR_08_ADV_C7_004_any_unknown_false_denies_before_effect()
    {
        var observation = await ExecuteExpressionAsync((_, denied) =>
            new AnyOfConstraintExpression(Unknown(), new PermittedOperationsConstraint(denied)));

        AssertDeniedWithUnknown(observation);
    }

    [Test]
    public async Task BR_08_ADV_C7_005_all_false_unknown_is_false_and_denies_before_effect()
    {
        var observation = await ExecuteExpressionAsync((_, denied) =>
            new AllOfConstraintExpression(new PermittedOperationsConstraint(denied), Unknown()));

        Assert.Multiple(() =>
        {
            Assert.That(observation.Result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(observation.Effects, Is.Zero);
            Assert.That(observation.Result.Decisions.Single().Outcome, Is.EqualTo(ConstraintEvaluationOutcome.Unsatisfied));
            Assert.That(observation.Result.Decisions.Single().UnsupportedConstraints, Does.Contain(UnknownName));
        });
    }

    [Test]
    public async Task BR_08_ADV_C7_006_unknown_excluded_middle_remains_unknown()
    {
        var observation = await ExecuteExpressionAsync((_, _) =>
            new AnyOfConstraintExpression(Unknown(), new NotConstraintExpression(Unknown())));

        AssertDeniedWithUnknown(observation);
    }

    [Test]
    public async Task BR_08_ADV_C3_001_expiry_after_effect_start_does_not_retroactively_deny()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        ActorReference holder = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var operation = OperationReference.Parse("Draft08.Instantaneous.Start");
        var notAfter = clock.GetUtcNow().AddSeconds(5);
        var effects = 0;
        var domain = AuthorityDomain.Create("draft-08-instantaneous", clock, genesis =>
        {
            holder = genesis.Actor("Holder");
            target = genesis.Actor("Target");
            genesis.Operation(operation, target, ShapeContract.Unit, ShapeContract.Unit, "cross expiry", _ =>
            {
                effects++;
                clock.Advance(TimeSpan.FromSeconds(10));
                return OperationEffect.SucceededAsync(ShapeValue.Unit);
            });
            capability = genesis.Grant(holder, target, [operation],
                [new WallClockValidityConstraint(notAfter: notAfter)]);
        });

        var result = await domain.ExecuteDraft08Async(holder, operation, capability, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(effects, Is.EqualTo(1));
            Assert.That(clock.GetUtcNow(), Is.GreaterThan(notAfter));
        });
    }

    [Test]
    public async Task BR_08_ADV_C3_002_new_execution_after_expiry_is_denied()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        ActorReference holder = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var operation = OperationReference.Parse("Draft08.Instantaneous.Later");
        var effects = 0;
        var domain = AuthorityDomain.Create("draft-08-later", clock, genesis =>
        {
            holder = genesis.Actor("Holder");
            target = genesis.Actor("Target");
            genesis.Operation(operation, target, ShapeContract.Unit, ShapeContract.Unit, "later execution", _ =>
            {
                effects++;
                return OperationEffect.SucceededAsync(ShapeValue.Unit);
            });
            capability = genesis.Grant(holder, target, [operation],
                [new WallClockValidityConstraint(notAfter: clock.GetUtcNow().AddSeconds(5))]);
        });
        clock.Advance(TimeSpan.FromSeconds(6));

        var result = await domain.ExecuteDraft08Async(holder, operation, capability, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(effects, Is.Zero);
        });
    }

    [Test]
    public async Task BR_08_ADV_C4_001_grandparent_constraint_denies_grandchild()
    {
        ActorReference actorA = null!;
        ActorReference actorB = null!;
        ActorReference actorD = null!;
        ActorReference target = null!;
        Capability root = null!;
        var allowed = OperationReference.Parse("Draft08.Chain.Allowed");
        var denied = OperationReference.Parse("Draft08.Chain.Denied");
        var effects = 0;
        var domain = AuthorityDomain.Create("draft-08-chain", genesis =>
        {
            actorA = genesis.Actor("Actor A");
            actorB = genesis.Actor("Actor B");
            actorD = genesis.Actor("Actor D");
            target = genesis.Actor("Target");
            genesis.Operation(allowed, target, ShapeContract.Unit, ShapeContract.Unit, "allowed operation", _ =>
                OperationEffect.SucceededAsync(ShapeValue.Unit));
            genesis.Operation(denied, target, ShapeContract.Unit, ShapeContract.Unit, "denied operation", _ =>
            {
                effects++;
                return OperationEffect.SucceededAsync(ShapeValue.Unit);
            });
            root = genesis.Grant(actorA, target, [allowed, denied],
                [new PermittedOperationsConstraint(allowed)]);
        });
        var child = root.Delegate(actorB);
        var grandchild = child.Delegate(actorD);

        var result = await domain.ExecuteDraft08Async(actorD, denied, grandchild, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(effects, Is.Zero);
            Assert.That(grandchild.AddedConstraintExpressions, Is.Empty);
        });
    }

    private static ValueConstraint Unknown() => new(UnknownName, ShapeValue.Text("protected-value"));

    private static async Task<AuthorityObservation> ExecuteExpressionAsync(
        Func<OperationReference, OperationReference, ConstraintExpression> expression)
    {
        ActorReference holder = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var allowed = OperationReference.Parse("Draft08.Evaluate.Allowed");
        var denied = OperationReference.Parse("Draft08.Evaluate.Denied");
        var effects = 0;
        var domain = AuthorityDomain.Create("draft-08-expression", genesis =>
        {
            holder = genesis.Actor("Holder");
            target = genesis.Actor("Target");
            genesis.Operation(allowed, target, ShapeContract.Unit, ShapeContract.Unit, "evaluate", _ =>
            {
                effects++;
                return OperationEffect.SucceededAsync(ShapeValue.Unit);
            });
            capability = genesis.GrantExpressions(holder, target, [allowed], [expression(allowed, denied)]);
        });

        var result = await domain.ExecuteDraft08Async(holder, allowed, capability, ShapeValue.Unit);
        return new(result, effects);
    }

    private static void AssertDeniedWithUnknown(AuthorityObservation observation)
    {
        Assert.Multiple(() =>
        {
            Assert.That(observation.Result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(observation.Effects, Is.Zero);
            Assert.That(observation.Result.Decisions.Single().Outcome, Is.EqualTo(ConstraintEvaluationOutcome.Indeterminate));
            Assert.That(observation.Result.Decisions.Single().UnsupportedConstraints, Does.Contain(UnknownName));
            Assert.That(observation.Result.Decisions.Single().Reason, Does.Not.Contain("protected-value"));
        });
    }

    private sealed record AuthorityObservation(ExecutionResult Result, int Effects);

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
