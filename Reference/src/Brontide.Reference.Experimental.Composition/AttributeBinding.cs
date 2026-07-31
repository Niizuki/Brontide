using System.Collections.Immutable;
using Brontide.Reference.Core;

namespace Brontide.Reference.Experimental.Composition;

/// <summary>
/// An Attribute value together with the source that produced it, as Architecture 0.7 §18.1 requires:
/// an Attribute is a value obtained through a specified Operation, never a free-floating label.
/// </summary>
public sealed record AttributeValue(
    CanonicalName Attribute,
    CanonicalName SourceOperation,
    string VocabularyVersion,
    ShapeReference ResultShape,
    string ResultPath,
    ShapeValue Value);

/// <summary>A selection candidate and the Attribute values it reports at the moment it is read.</summary>
public sealed record AttributeCandidate
{
    public AttributeCandidate(CanonicalName provider, params AttributeValue[] attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        if (attributes.Any(attribute => attribute is null))
        {
            throw new ArgumentException("An Attribute candidate cannot report a null value.", nameof(attributes));
        }

        Provider = provider;
        Attributes = [.. attributes];
    }

    public CanonicalName Provider { get; }

    public ImmutableArray<AttributeValue> Attributes { get; }
}

/// <summary>An atomic Definition Constraint requiring one Attribute to hold one value.</summary>
public sealed record AttributeConstraint(CanonicalName Attribute, ShapeValue Expected)
    : Constraint(Attribute, Expected);

public enum AttributeCandidateDisposition
{
    Selected,
    Unsatisfied,
    Unevaluatable,
}

public sealed record AttributeCandidateOutcome(
    CanonicalName Provider,
    AttributeCandidateDisposition Disposition,
    ConstraintDiagnosticCategory DiagnosticCategory,
    ImmutableArray<CanonicalName> UnsupportedConstraints,
    string Reason);

/// <summary>
/// The immutable record of one completed resolution: what was selected, the values that decided it,
/// and the account of every candidate considered.
/// </summary>
/// <remarks>
/// This record holds no reference to the candidate set or Attribute source it was resolved from,
/// which is what makes a later Attribute or candidate change unable to rebind it.
/// </remarks>
public sealed record AttributeBindingRecord(
    CanonicalName Binding,
    CanonicalName SelectedProvider,
    ImmutableArray<AttributeValue> EffectiveValues,
    ImmutableArray<AttributeCandidateOutcome> Provenance);

public sealed record AttributeBindingResolution(
    AttributeBindingRecord? Binding,
    ImmutableArray<AttributeCandidateOutcome> Provenance,
    string Reason)
{
    public bool IsResolved => Binding is not null;
}

/// <summary>
/// Resolves an Attribute-constrained binding exactly once and records what decided it.
/// </summary>
/// <remarks>
/// Architecture 0.7 §18.1: resolution evaluates Definition Constraints against Attribute values
/// obtained at that moment and records effective values and provenance; a later Attribute change
/// never invalidates, rebinds, or migrates an active binding. The tempting implementation is a live
/// query that re-answers on every read, which passes every single-shot test and violates that rule,
/// so the resolved record deliberately captures values rather than sources.
/// </remarks>
public static class AttributeConstrainedBinding
{
    public static AttributeBindingResolution Resolve(
        CanonicalName binding,
        ConstraintExpression constraint,
        IEnumerable<AttributeCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        ArgumentNullException.ThrowIfNull(candidates);

        // One stated total order, so ties resolve identically on every run and in both stacks.
        var ordered = candidates
            .Select(candidate => candidate ?? throw new ArgumentException(
                "A candidate set cannot contain a null candidate.",
                nameof(candidates)))
            .OrderBy(candidate => candidate.Provider)
            .ToImmutableArray();

        var outcomes = ImmutableArray.CreateBuilder<AttributeCandidateOutcome>();
        foreach (var candidate in ordered)
        {
            var evaluation = ConstraintExpressionEvaluator.Evaluate(
                constraint,
                atom => EvaluateAtom(atom, candidate));
            var disposition = evaluation.Outcome switch
            {
                ConstraintEvaluationOutcome.Satisfied => AttributeCandidateDisposition.Selected,
                ConstraintEvaluationOutcome.Unsatisfied => AttributeCandidateDisposition.Unsatisfied,
                _ => AttributeCandidateDisposition.Unevaluatable,
            };
            outcomes.Add(new(
                candidate.Provider,
                disposition,
                evaluation.DiagnosticCategory,
                evaluation.UnsupportedConstraints,
                evaluation.Reason));

            if (disposition != AttributeCandidateDisposition.Selected)
            {
                continue;
            }

            // Selected: capture the values the constraint actually read, then stop. Nothing beyond
            // this point consults the candidate set again.
            var provenance = outcomes.ToImmutable();
            return new(
                new(binding, candidate.Provider, EffectiveValues(constraint, candidate), provenance),
                provenance,
                $"Binding '{binding}' resolved to '{candidate.Provider}' against {provenance.Length} considered candidate(s).");
        }

        var considered = outcomes.ToImmutable();
        return new(
            null,
            considered,
            considered.Length == 0
                ? $"Binding '{binding}' has no candidates to resolve against."
                : $"No candidate satisfies the declared constraints for binding '{binding}'; {considered.Length} were considered and excluded.");
    }

    /// <summary>
    /// Restores a recorded binding from its own recorded evidence, consulting no source.
    /// </summary>
    /// <remarks>
    /// The absent parameter is the contract: restoration takes no candidate set and no Attribute
    /// source, so it cannot silently reselect against one. The recorded effective values must still
    /// satisfy the constraint, which is what makes an incomplete record refusable rather than
    /// restorable.
    /// </remarks>
    public static AttributeBindingResolution Restore(
        ConstraintExpression constraint,
        AttributeBindingRecord record)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        ArgumentNullException.ThrowIfNull(record);

        var recorded = new AttributeCandidate(record.SelectedProvider, [.. record.EffectiveValues]);
        var evaluation = ConstraintExpressionEvaluator.Evaluate(
            constraint,
            atom => EvaluateAtom(atom, recorded));
        if (evaluation.Outcome != ConstraintEvaluationOutcome.Satisfied)
        {
            return new(
                null,
                record.Provenance,
                $"The recorded effective values for '{record.SelectedProvider}' no longer satisfy the declared constraints: {evaluation.Reason}");
        }

        return new(
            record,
            record.Provenance,
            $"Binding '{record.Binding}' restored to '{record.SelectedProvider}' without reselection.");
    }

    /// <summary>
    /// Evaluates one atom against one candidate's reported values.
    /// </summary>
    /// <remarks>
    /// A candidate that cannot answer the atom at all, or answers with a value this evaluator cannot
    /// compare, is Indeterminate rather than Unsatisfied: under the poisoning rule that makes the
    /// whole expression unevaluatable for that candidate, which in selection context is candidate
    /// exclusion.
    /// </remarks>
    private static ConstraintAtomEvaluation EvaluateAtom(Constraint atom, AttributeCandidate candidate)
    {
        if (atom is not AttributeConstraint required)
        {
            return ConstraintAtomEvaluation.Unsupported(atom.Name);
        }

        var reported = candidate.Attributes
            .FirstOrDefault(attribute => attribute.Attribute == required.Attribute);
        if (reported is null)
        {
            return ConstraintAtomEvaluation.Unsupported(required.Attribute);
        }

        if (reported.Value is not ScalarShapeValue actual || required.Expected is not ScalarShapeValue expected)
        {
            return ConstraintAtomEvaluation.InvalidValue();
        }

        return actual.Reference == expected.Reference && Equals(actual.Value, expected.Value)
            ? ConstraintAtomEvaluation.Satisfied(
                $"Attribute '{required.Attribute}' reported by '{candidate.Provider}' from Operation '{reported.SourceOperation}' matches.")
            : ConstraintAtomEvaluation.Unsatisfied(
                $"Attribute '{required.Attribute}' reported by '{candidate.Provider}' does not match the declared value.");
    }

    private static ImmutableArray<AttributeValue> EffectiveValues(
        ConstraintExpression constraint,
        AttributeCandidate candidate)
    {
        var referenced = ConstraintExpressionEvaluator.AtomicConstraints(constraint)
            .OfType<AttributeConstraint>()
            .Select(atom => atom.Attribute)
            .ToImmutableHashSet();
        return
        [
            .. candidate.Attributes
                .Where(attribute => referenced.Contains(attribute.Attribute))
                .OrderBy(attribute => attribute.Attribute),
        ];
    }
}
