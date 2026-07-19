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
}
