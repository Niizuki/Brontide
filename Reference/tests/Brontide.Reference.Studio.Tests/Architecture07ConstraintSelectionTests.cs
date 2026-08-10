using Brontide.Reference.Core;
using Brontide.Reference.Experimental.Composition;

namespace Brontide.Reference.Studio.Tests;

public sealed class Architecture07ConstraintSelectionTests
{
    [Test]
    public void BR_07_CONSTRAINT_003_poisoned_definition_constraint_excludes_candidate()
    {
        var supported = CanonicalName.Parse("Example:Supported");
        var unsupported = CanonicalName.Parse("Example:Unsupported");
        var poisonedName = CanonicalName.Parse("Example:PoisonedProvider");
        var eligibleName = CanonicalName.Parse("Example:EligibleProvider");
        var candidates = new[]
        {
            new DefinitionConstraintCandidate<string>(
                poisonedName,
                "poisoned",
                new AnyOfConstraintExpression(
                    new ValueConstraint(supported, ShapeValue.Text("yes")),
                    new ValueConstraint(unsupported, ShapeValue.Text("protected-value")))),
            new DefinitionConstraintCandidate<string>(
                eligibleName,
                "eligible",
                new ValueConstraint(supported, ShapeValue.Text("yes")))
        };

        var result = DefinitionConstraintSelection.Filter(
            candidates,
            atom => atom.Name == supported
                ? ConstraintAtomEvaluation.Satisfied()
                : ConstraintAtomEvaluation.Unsupported(atom.Name));

        Assert.Multiple(() =>
        {
            Assert.That(result.Eligible.Select(candidate => candidate.Name), Is.EqualTo(new[] { eligibleName }));
            Assert.That(result.Rejected, Has.Length.EqualTo(1));
            Assert.That(result.Rejected[0].Candidate, Is.EqualTo(poisonedName));
            Assert.That(result.Rejected[0].DiagnosticCategory,
                Is.EqualTo(ConstraintDiagnosticCategory.UnsupportedConstraint));
            Assert.That(result.Rejected[0].UnsupportedConstraints, Is.EqualTo(new[] { unsupported }));
            Assert.That(result.Rejected[0].Reason, Does.Not.Contain("protected-value"));
        });
    }

    [Test]
    public void BR_08_ADV_C7_007_any_unknown_match_retains_candidate_and_records_unknown()
    {
        var supported = CanonicalName.Parse("Example:Draft08.Supported");
        var unknown = CanonicalName.Parse("Example:Draft08.Unknown");
        var candidateName = CanonicalName.Parse("Example:Draft08.Candidate");
        var candidate = new DefinitionConstraintCandidate<string>(
            candidateName,
            "candidate",
            new AnyOfConstraintExpression(Atom(supported), Atom(unknown)));

        var result = DefinitionConstraintSelection.FilterDraft08(
            [candidate],
            constraint => constraint.Name == supported
                ? ConstraintAtomEvaluation.Satisfied()
                : ConstraintAtomEvaluation.Unsupported(unknown));

        Assert.Multiple(() =>
        {
            Assert.That(result.Eligible.Select(item => item.Name), Is.EqualTo(new[] { candidateName }));
            Assert.That(result.Rejected, Is.Empty);
            Assert.That(result.Assessments.Single().UnsupportedConstraints, Does.Contain(unknown));
        });
    }

    [Test]
    public void BR_08_ADV_C7_008_all_match_unknown_excludes_candidate_and_records_unknown()
    {
        var supported = CanonicalName.Parse("Example:Draft08.Supported");
        var unknown = CanonicalName.Parse("Example:Draft08.Unknown");
        var candidateName = CanonicalName.Parse("Example:Draft08.Candidate");
        var candidate = new DefinitionConstraintCandidate<string>(
            candidateName,
            "candidate",
            new AllOfConstraintExpression(Atom(supported), Atom(unknown)));

        var result = DefinitionConstraintSelection.FilterDraft08(
            [candidate],
            constraint => constraint.Name == supported
                ? ConstraintAtomEvaluation.Satisfied()
                : ConstraintAtomEvaluation.Unsupported(unknown));

        Assert.Multiple(() =>
        {
            Assert.That(result.Eligible, Is.Empty);
            Assert.That(result.Rejected.Single().Candidate, Is.EqualTo(candidateName));
            Assert.That(result.Assessments.Single().UnsupportedConstraints, Does.Contain(unknown));
        });
    }

    private static ValueConstraint Atom(CanonicalName name) => new(name, ShapeValue.Text("value"));
}
