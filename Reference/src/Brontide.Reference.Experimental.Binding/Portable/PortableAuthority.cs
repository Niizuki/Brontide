using System.Collections.Immutable;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>A three-valued authority atom.</summary>
public enum PortableTruth
{
    Satisfied,
    Unsatisfied,
    Unknown
}

/// <summary>An authority expression over three-valued atoms.</summary>
/// <remarks>
/// Unknown never resolves to permitted. Only a Satisfied expression permits an effect, so an
/// unrecognized Constraint value version denies rather than widening authority by projection.
/// </remarks>
public abstract record PortableConstraint
{
    public static PortableConstraint Atom(PortableTruth truth) => new PortableAtomConstraint(truth);

    public static PortableConstraint AnyOf(params PortableConstraint[] operands) =>
        new PortableAnyOfConstraint([.. operands]);

    public static PortableConstraint AllOf(params PortableConstraint[] operands) =>
        new PortableAllOfConstraint([.. operands]);

    public static PortableConstraint Not(PortableConstraint operand) => new PortableNotConstraint(operand);

    /// <summary>Evaluates the expression under strong Kleene three-valued logic.</summary>
    public PortableTruth Evaluate() => this switch
    {
        PortableAtomConstraint atom => atom.Truth,
        PortableAnyOfConstraint anyOf => EvaluateAnyOf(anyOf.Operands),
        PortableAllOfConstraint allOf => EvaluateAllOf(allOf.Operands),
        PortableNotConstraint not => not.Operand.Evaluate() switch
        {
            PortableTruth.Satisfied => PortableTruth.Unsatisfied,
            PortableTruth.Unsatisfied => PortableTruth.Satisfied,
            _ => PortableTruth.Unknown
        },
        _ => PortableTruth.Unknown
    };

    private static PortableTruth EvaluateAnyOf(ImmutableArray<PortableConstraint> operands)
    {
        var unknown = false;
        foreach (var operand in operands)
        {
            switch (operand.Evaluate())
            {
                case PortableTruth.Satisfied:
                    return PortableTruth.Satisfied;
                case PortableTruth.Unknown:
                    unknown = true;
                    break;
                default:
                    break;
            }
        }

        return unknown ? PortableTruth.Unknown : PortableTruth.Unsatisfied;
    }

    private static PortableTruth EvaluateAllOf(ImmutableArray<PortableConstraint> operands)
    {
        var unknown = false;
        foreach (var operand in operands)
        {
            switch (operand.Evaluate())
            {
                case PortableTruth.Unsatisfied:
                    return PortableTruth.Unsatisfied;
                case PortableTruth.Unknown:
                    unknown = true;
                    break;
                default:
                    break;
            }
        }

        return unknown ? PortableTruth.Unknown : PortableTruth.Satisfied;
    }
}

public sealed record PortableAtomConstraint(PortableTruth Truth) : PortableConstraint;

public sealed record PortableAnyOfConstraint(ImmutableArray<PortableConstraint> Operands) : PortableConstraint;

public sealed record PortableAllOfConstraint(ImmutableArray<PortableConstraint> Operands) : PortableConstraint;

public sealed record PortableNotConstraint(PortableConstraint Operand) : PortableConstraint;

/// <summary>
/// The tokens by which this endpoint recognizes authority-bearing content in a payload position.
/// </summary>
/// <remarks>
/// The neutral contract forbids a Capability, Constraint expression, or derivation chain from
/// crossing a trust boundary but does not enumerate their names, so the recognizer is declared here
/// rather than inferred. The names are local; the effect is not: any of them in a body of a
/// trust-crossing binding is an invalid authority presentation, never a merely undeclared field.
/// </remarks>
public static class PortableAuthorityVocabulary
{
    private static readonly ImmutableHashSet<string> Tokens = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "capability",
        "capabilityChain",
        "constraint",
        "constraintExpression",
        "derivation",
        "derivationChain",
        "authorityToken");

    public static bool IsAuthorityBearing(string name) => Tokens.Contains(name);

    /// <summary>
    /// Rejects authority-bearing content anywhere in a decoded body before the body is given a
    /// Shape. The scan runs first so the rejection names the authority rule rather than the Shape.
    /// </summary>
    public static void RequireNoCapabilityContent(CborItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        switch (item)
        {
            case CborMap map:
                foreach (var entry in map.Entries)
                {
                    if (IsAuthorityBearing(entry.Key))
                    {
                        throw new PortableFaultException(
                            PortableProtocolCategory.InvalidAuthorityPresentation,
                            "capability-in-body",
                            $"Field '{entry.Key}' presents authority across a trust boundary that carries no Capability.");
                    }

                    RequireNoCapabilityContent(entry.Value);
                }

                break;
            case CborArray array:
                foreach (var element in array.Items)
                {
                    RequireNoCapabilityContent(element);
                }

                break;
            default:
                break;
        }
    }

    /// <summary>
    /// The same scan over an in-memory value, which is what the fixed direct-call realization
    /// presents. Both realizations therefore refuse the same content.
    /// </summary>
    public static void RequireNoCapabilityContent(PortableValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        switch (value)
        {
            case PortableRecordValue record:
                foreach (var field in record.Fields)
                {
                    RequireNoAuthorityMember(field.Key);
                    RequireNoCapabilityContent(field.Value);
                }

                foreach (var fragment in record.Fragments)
                {
                    RequireNoAuthorityMember(fragment.Key.Name.Value);
                    foreach (var field in fragment.Value)
                    {
                        RequireNoAuthorityMember(field.Key);
                        RequireNoCapabilityContent(field.Value);
                    }
                }

                break;
            case PortableSequenceValue sequence:
                foreach (var item in sequence.Items)
                {
                    RequireNoCapabilityContent(item);
                }

                break;
            case PortableChoiceValue choice:
                RequireNoAuthorityMember(choice.Alternative);
                RequireNoCapabilityContent(choice.Value);
                break;
            default:
                break;
        }
    }

    private static void RequireNoAuthorityMember(string name)
    {
        if (IsAuthorityBearing(name))
        {
            throw new PortableFaultException(
                PortableProtocolCategory.InvalidAuthorityPresentation,
                "capability-in-body",
                $"Member '{name}' presents authority across a trust boundary that carries no Capability.");
        }
    }
}

/// <summary>The result of a local authority evaluation.</summary>
public sealed record PortableAdmission(
    PortableAuthorityDecision Decision,
    PortableAuthorityDecisionPoint DecisionPoint,
    string Reason)
{
    public bool MayProceed => Decision == PortableAuthorityDecision.Permitted;
}

/// <summary>
/// Evaluates the local authority boundary for one request.
/// </summary>
/// <remarks>
/// A denial or unknown condition decided here starts no provider and emits no Channel frame: local
/// denial is an observation, never an envelope kind.
/// </remarks>
public sealed class PortableAuthorityGate(PortableAuthorityDeclaration declaration)
{
    private readonly PortableAuthorityDeclaration _declaration = declaration ??
        throw new ArgumentNullException(nameof(declaration));

    public PortableAdmission Evaluate(PortableConstraint constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        var point = _declaration.TrustBoundaryCrossed
            ? PortableAuthorityDecisionPoint.HostLocal
            : PortableAuthorityDecisionPoint.TargetBoundary;

        return constraint.Evaluate() switch
        {
            PortableTruth.Satisfied => new PortableAdmission(
                PortableAuthorityDecision.Permitted,
                point,
                "The authority expression evaluated to True."),
            PortableTruth.Unsatisfied => new PortableAdmission(
                PortableAuthorityDecision.Denied,
                point,
                "The authority expression evaluated to False."),
            _ => new PortableAdmission(
                PortableAuthorityDecision.Unknown,
                point,
                "The authority expression evaluated to Unknown, which never resolves to permitted.")
        };
    }

    /// <summary>Validates the declared presentation before any binding is established.</summary>
    public static void RequireValidPresentation(PortableAuthorityDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        if (!string.Equals(
                declaration.ConstraintPolicy,
                PortableAuthorityDeclaration.OnlyPermittedConstraintPolicy,
                StringComparison.Ordinal))
        {
            throw new PortableFaultException(
                PortableProtocolCategory.InvalidAuthorityPresentation,
                "constraint-policy",
                "The only permitted constraint policy is fail-closed.");
        }

        var crossTrust = declaration.PresentationMode == PortableAuthorityMode.CrossTrustNoCapabilityTransfer;
        if (crossTrust != declaration.TrustBoundaryCrossed)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.InvalidAuthorityPresentation,
                "presentation-mode",
                "The declared presentation mode and the trust-boundary declaration disagree.");
        }

        if (declaration.TrustBoundaryCrossed && !declaration.NoCapabilityTransfer)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.InvalidAuthorityPresentation,
                "no-capability-transfer-absent",
                "A trust-crossing binding must declare no-capability-transfer rather than defaulting to a permissive mode.");
        }
    }
}
