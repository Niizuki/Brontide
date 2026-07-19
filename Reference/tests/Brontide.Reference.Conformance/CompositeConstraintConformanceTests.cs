using Brontide.Reference.Core;

namespace Brontide.Reference.Conformance;

public sealed class CompositeConstraintConformanceTests
{
    [Test]
    [SpecSection("10.1")]
    [SpecSection("13.5")]
    public async Task Satisfied_positive_origin_branch_preserves_authority_through_nested_logic()
    {
        ActorReference holder = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var operation = OperationReference.Parse("Example:PositiveCompositeOriginEffect");
        var deny = CanonicalName.Parse("Example:DenyOriginSibling");
        var effects = 0;

        var domain = AuthorityDomain.Create("architecture-0.7-positive-composite-origin", genesis =>
        {
            holder = genesis.Actor("Holder");
            target = genesis.Actor("Target");
            genesis.Constraint(
                deny,
                ShapeContract.For(BuiltInShapes.Text),
                (constraint, _) => ConstraintDecision.Deny(constraint.Name, "unrelated atom did not match"));
            genesis.Operation(
                operation,
                target,
                ShapeContract.Unit,
                ShapeContract.Unit,
                "positive composite origin effect",
                _ =>
                {
                    effects++;
                    return OperationEffect.SucceededAsync(ShapeValue.Unit);
                });
            capability = genesis.GrantExpressions(
                holder,
                target,
                [operation],
                [
                    new AnyOfConstraintExpression(
                        new NotConstraintExpression(
                            new NotConstraintExpression(
                                new OriginGrantConstraint(OriginClass.Device))),
                        new ValueConstraint(deny, ShapeValue.Text("miss")))
                ]);
        });

        var result = await domain.ExecuteAsync(
            holder,
            operation,
            capability,
            ShapeValue.Unit,
            OriginClass.Device);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(effects, Is.EqualTo(1));
        });
    }

    [Test]
    [SpecSection("10.1")]
    [SpecSection("13.5")]
    public async Task Satisfied_unrelated_branch_cannot_turn_an_unsatisfied_origin_atom_into_authority()
    {
        ActorReference holder = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var operation = OperationReference.Parse("Example:CompositeOriginEffect");
        var known = CanonicalName.Parse("Example:KnownOriginSibling");
        var effects = 0;

        var domain = AuthorityDomain.Create("architecture-0.7-composite-origin", genesis =>
        {
            holder = genesis.Actor("Holder");
            target = genesis.Actor("Target");
            genesis.Constraint(
                known,
                ShapeContract.For(BuiltInShapes.Text),
                (constraint, _) => ConstraintDecision.Allow(constraint.Name, "unrelated atom matched"));
            genesis.Operation(
                operation,
                target,
                ShapeContract.Unit,
                ShapeContract.Unit,
                "observable origin effect",
                _ =>
                {
                    effects++;
                    return OperationEffect.SucceededAsync(ShapeValue.Unit);
                });
            capability = genesis.GrantExpressions(
                holder,
                target,
                [operation],
                [
                    new AnyOfConstraintExpression(
                        new OriginGrantConstraint(OriginClass.Device),
                        new ValueConstraint(known, ShapeValue.Text("match")))
                ]);
        });

        var result = await domain.ExecuteAsync(
            holder,
            operation,
            capability,
            ShapeValue.Unit,
            OriginClass.Derived);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(result.Outcome.Message, Does.Contain("without an origin grant"));
            Assert.That(effects, Is.Zero);
        });
    }

    [Test]
    [SpecSection("10.1")]
    [SpecSection("13.5")]
    [SpecSection("29.2")]
    public async Task BR_07_CONSTRAINT_002_poisoned_capability_denies_before_effect_with_redacted_diagnostics()
    {
        ActorReference holder = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var operation = OperationReference.Parse("Example:CompositeEffect");
        var known = CanonicalName.Parse("Example:KnownConstraint");
        var unsupported = CanonicalName.Parse("Example:UnsupportedConstraint");
        var effects = 0;

        var domain = AuthorityDomain.Create("architecture-0.7-composite", genesis =>
        {
            holder = genesis.Actor("Holder");
            target = genesis.Actor("Target");
            genesis.Constraint(
                known,
                ShapeContract.For(BuiltInShapes.Text),
                (constraint, _) => ConstraintDecision.Allow(constraint.Name, "known atom matched"));
            genesis.Operation(
                operation,
                target,
                ShapeContract.Unit,
                ShapeContract.Unit,
                "observable effect",
                _ =>
                {
                    effects++;
                    return OperationEffect.SucceededAsync(ShapeValue.Unit);
                });
            capability = genesis.GrantExpressions(
                holder,
                target,
                [operation],
                [
                    new AnyOfConstraintExpression(
                        new ValueConstraint(known, ShapeValue.Text("match")),
                        new NotConstraintExpression(
                            new ValueConstraint(unsupported, ShapeValue.Text("protected-secret"))))
                ]);
        });

        var result = await domain.ExecuteAsync(holder, operation, capability, ShapeValue.Unit);
        var decision = result.Decisions.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(effects, Is.Zero);
            Assert.That(decision.Outcome, Is.EqualTo(ConstraintEvaluationOutcome.Indeterminate));
            Assert.That(decision.DiagnosticCategory, Is.EqualTo(ConstraintDiagnosticCategory.UnsupportedConstraint));
            Assert.That(decision.UnsupportedConstraints, Is.EqualTo(new[] { unsupported }));
            Assert.That(result.Outcome.Message, Does.Contain("UnsupportedConstraint"));
            Assert.That(result.Outcome.Message, Does.Not.Contain("protected-secret"));
            Assert.That(domain.Provenance.Last(entry => entry.Execution is not null).Message,
                Does.Not.Contain("protected-secret"));
        });
    }
}
