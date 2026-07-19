using Brontide.Reference.Core;

namespace Brontide.Reference.Core.Tests;

public sealed class ConstraintExpressionTests
{
    private static readonly CanonicalName AllowName = CanonicalName.Parse("Example:Allow");
    private static readonly CanonicalName DenyName = CanonicalName.Parse("Example:Deny");
    private static readonly CanonicalName UnknownName = CanonicalName.Parse("Example:Unsupported");

    [Test]
    public void BR_07_CONSTRAINT_001_three_state_recursive_evaluation_is_explicit()
    {
        var satisfied = ConstraintExpressionEvaluator.Evaluate(
            new AllOfConstraintExpression(
                Atom(AllowName),
                new NotConstraintExpression(Atom(DenyName))),
            EvaluateAtom);
        var unsatisfied = ConstraintExpressionEvaluator.Evaluate(
            new AnyOfConstraintExpression(Atom(DenyName), new NotConstraintExpression(Atom(AllowName))),
            EvaluateAtom);
        var indeterminate = ConstraintExpressionEvaluator.Evaluate(
            new NotConstraintExpression(Atom(UnknownName, "protected-secret")),
            EvaluateAtom);

        Assert.Multiple(() =>
        {
            Assert.That(satisfied.Outcome, Is.EqualTo(ConstraintEvaluationOutcome.Satisfied));
            Assert.That(unsatisfied.Outcome, Is.EqualTo(ConstraintEvaluationOutcome.Unsatisfied));
            Assert.That(indeterminate.Outcome, Is.EqualTo(ConstraintEvaluationOutcome.Indeterminate));
            Assert.That(indeterminate.DiagnosticCategory, Is.EqualTo(ConstraintDiagnosticCategory.UnsupportedConstraint));
            Assert.That(indeterminate.Reason, Does.Not.Contain("protected-secret"));
        });
    }

    [Test]
    public void Unknown_atom_poisons_every_composite_position_without_short_circuit_masking()
    {
        ConstraintExpression[] expressions =
        [
            new AllOfConstraintExpression(Atom(UnknownName), Atom(DenyName)),
            new AllOfConstraintExpression(Atom(DenyName), Atom(UnknownName)),
            new AnyOfConstraintExpression(Atom(UnknownName), Atom(AllowName)),
            new AnyOfConstraintExpression(Atom(AllowName), Atom(UnknownName)),
            new NotConstraintExpression(Atom(UnknownName)),
            new AllOfConstraintExpression(
                Atom(AllowName),
                new AnyOfConstraintExpression(Atom(DenyName), new NotConstraintExpression(Atom(UnknownName))))
        ];

        var results = expressions
            .Select(expression => ConstraintExpressionEvaluator.Evaluate(expression, EvaluateAtom))
            .ToArray();

        Assert.That(results.Select(result => result.Outcome),
            Is.All.EqualTo(ConstraintEvaluationOutcome.Indeterminate));
        Assert.That(results.Select(result => result.DiagnosticCategory),
            Is.All.EqualTo(ConstraintDiagnosticCategory.UnsupportedConstraint));
        Assert.That(results, Has.All.Matches<ConstraintExpressionEvaluation>(result =>
            result.UnsupportedConstraints.SequenceEqual([UnknownName])));
    }

    [Test]
    public void Reordered_unknown_atoms_have_the_same_deterministic_explanation()
    {
        var anotherUnknown = CanonicalName.Parse("Example:AnotherUnsupported");
        var left = ConstraintExpressionEvaluator.Evaluate(
            new AnyOfConstraintExpression(Atom(UnknownName), Atom(anotherUnknown), Atom(AllowName)),
            EvaluateAtom);
        var right = ConstraintExpressionEvaluator.Evaluate(
            new AnyOfConstraintExpression(Atom(AllowName), Atom(anotherUnknown), Atom(UnknownName)),
            EvaluateAtom);

        Assert.Multiple(() =>
        {
            Assert.That(right.Outcome, Is.EqualTo(left.Outcome));
            Assert.That(right.DiagnosticCategory, Is.EqualTo(left.DiagnosticCategory));
            Assert.That(right.UnsupportedConstraints, Is.EqualTo(left.UnsupportedConstraints));
            Assert.That(right.Reason, Is.EqualTo(left.Reason));
        });
    }

    [Test]
    public void Unknown_expression_node_fails_closed_instead_of_escaping_evaluation()
    {
        var result = ConstraintExpressionEvaluator.Evaluate(new UnknownExpression(), EvaluateAtom);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(ConstraintEvaluationOutcome.Indeterminate));
            Assert.That(result.DiagnosticCategory, Is.EqualTo(ConstraintDiagnosticCategory.InvalidConstraintExpression));
            Assert.That(result.UnsupportedConstraints, Is.Empty);
        });
    }

    private static ValueConstraint Atom(CanonicalName name, string value = "value") =>
        new(name, ShapeValue.Text(value));

    private static ConstraintAtomEvaluation EvaluateAtom(Constraint constraint) => constraint.Name switch
    {
        var name when name == AllowName => ConstraintAtomEvaluation.Satisfied(),
        var name when name == DenyName => ConstraintAtomEvaluation.Unsatisfied(),
        _ => ConstraintAtomEvaluation.Unsupported(constraint.Name)
    };

    private sealed record UnknownExpression()
        : ConstraintExpression(CanonicalName.Parse("Example:UnknownExpression"));
}
