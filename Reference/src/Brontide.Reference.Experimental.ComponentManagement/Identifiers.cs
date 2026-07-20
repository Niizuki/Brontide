namespace Brontide.Reference.Experimental.ComponentManagement;

/// <summary>
/// Shared syntax for the fake Component Manager's identifier spaces. Every space is a distinct
/// type even though all are backed by the same lowercase token syntax, so mixing spaces is a
/// compile-time error rather than a silent bug.
/// </summary>
internal static class IdentifierSyntax
{
    internal static string Require(string value, string space)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException($"{space} requires a non-empty value.", nameof(value));
        }

        foreach (var ch in value)
        {
            var valid = ch is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '-';
            if (!valid)
            {
                throw new ArgumentException(
                    $"{space} '{value}' contains invalid character '{ch}'; use lowercase letters, digits, '.', or '-'.",
                    nameof(value));
            }
        }

        return value;
    }
}

public readonly record struct SourceId(string Value)
{
    public static SourceId Create(string value) => new(IdentifierSyntax.Require(value, nameof(SourceId)));
    public override string ToString() => Value;
}

public readonly record struct PublisherId(string Value)
{
    public static PublisherId Create(string value) => new(IdentifierSyntax.Require(value, nameof(PublisherId)));
    public override string ToString() => Value;
}

public readonly record struct PackageId(string Value)
{
    public static PackageId Create(string value) => new(IdentifierSyntax.Require(value, nameof(PackageId)));
    public override string ToString() => Value;
}

public readonly record struct DefinitionId(string Value)
{
    public static DefinitionId Create(string value) => new(IdentifierSyntax.Require(value, nameof(DefinitionId)));
    public override string ToString() => Value;
}

public readonly record struct OccurrenceId(string Value)
{
    public static OccurrenceId Create(string value) => new(IdentifierSyntax.Require(value, nameof(OccurrenceId)));
    public override string ToString() => Value;
}

public readonly record struct ActorId(string Value)
{
    public static ActorId Create(string value) => new(IdentifierSyntax.Require(value, nameof(ActorId)));
    public override string ToString() => Value;
}

public readonly record struct ContractId(string Value)
{
    public static ContractId Create(string value) => new(IdentifierSyntax.Require(value, nameof(ContractId)));
    public override string ToString() => Value;
}

public readonly record struct VersionLiteral(string Value)
{
    public static VersionLiteral Create(string value) => new(IdentifierSyntax.Require(value, nameof(VersionLiteral)));
    public override string ToString() => Value;
}

public readonly record struct BindingScopeId(string Value)
{
    public static BindingScopeId Create(string value) => new(IdentifierSyntax.Require(value, nameof(BindingScopeId)));
    public override string ToString() => Value;
}

public readonly record struct BindingId(string Value)
{
    public static BindingId Create(string value) => new(IdentifierSyntax.Require(value, nameof(BindingId)));
    public override string ToString() => Value;
}

public readonly record struct ArtifactId(string Value)
{
    public static ArtifactId Create(string value) => new(IdentifierSyntax.Require(value, nameof(ArtifactId)));
    public override string ToString() => Value;
}

public readonly record struct EvidenceId(string Value)
{
    public static EvidenceId Create(string value) => new(IdentifierSyntax.Require(value, nameof(EvidenceId)));
    public override string ToString() => Value;
}

public readonly record struct IssuerId(string Value)
{
    public static IssuerId Create(string value) => new(IdentifierSyntax.Require(value, nameof(IssuerId)));
    public override string ToString() => Value;
}

public readonly record struct PreferenceId(string Value)
{
    public static PreferenceId Create(string value) => new(IdentifierSyntax.Require(value, nameof(PreferenceId)));
    public override string ToString() => Value;
}

public readonly record struct GenerationId(string Value)
{
    public static GenerationId Create(string value) => new(IdentifierSyntax.Require(value, nameof(GenerationId)));
    public override string ToString() => Value;
}

public readonly record struct RestartScopeId(string Value)
{
    public static RestartScopeId Create(string value) => new(IdentifierSyntax.Require(value, nameof(RestartScopeId)));
    public override string ToString() => Value;
}

public readonly record struct RegionId(string Value)
{
    public static RegionId Create(string value) => new(IdentifierSyntax.Require(value, nameof(RegionId)));
    public override string ToString() => Value;
}

public readonly record struct PortId(string Value)
{
    public static PortId Create(string value) => new(IdentifierSyntax.Require(value, nameof(PortId)));
    public override string ToString() => Value;
}

public readonly record struct TopologyNodeId(string Value)
{
    public static TopologyNodeId Create(string value) => new(IdentifierSyntax.Require(value, nameof(TopologyNodeId)));
    public override string ToString() => Value;
}

public readonly record struct FunctionId(string Value)
{
    public static FunctionId Create(string value) => new(IdentifierSyntax.Require(value, nameof(FunctionId)));
    public override string ToString() => Value;
}

public readonly record struct ClaimId(string Value)
{
    public static ClaimId Create(string value) => new(IdentifierSyntax.Require(value, nameof(ClaimId)));
    public override string ToString() => Value;
}

public readonly record struct ObserverId(string Value)
{
    public static ObserverId Create(string value) => new(IdentifierSyntax.Require(value, nameof(ObserverId)));
    public override string ToString() => Value;
}

/// <summary>Requirement cardinality such as <c>1..1</c> or <c>0..*</c>.</summary>
public readonly record struct Cardinality(int Minimum, int? Maximum)
{
    public static Cardinality Parse(string value)
    {
        var separator = value.IndexOf("..", StringComparison.Ordinal);
        if (separator <= 0 || separator + 2 >= value.Length + 1)
        {
            throw new ArgumentException($"Cardinality '{value}' must use the form 'min..max'.", nameof(value));
        }

        var minimumText = value[..separator];
        var maximumText = value[(separator + 2)..];
        if (!int.TryParse(minimumText, out var minimum) || minimum < 0)
        {
            throw new ArgumentException($"Cardinality '{value}' has an invalid minimum.", nameof(value));
        }

        int? maximum;
        if (maximumText == "*")
        {
            maximum = null;
        }
        else if (int.TryParse(maximumText, out var bounded) && bounded >= minimum)
        {
            maximum = bounded;
        }
        else
        {
            throw new ArgumentException($"Cardinality '{value}' has an invalid maximum.", nameof(value));
        }

        return new Cardinality(minimum, maximum);
    }

    public override string ToString() => Maximum is int max ? $"{Minimum}..{max}" : $"{Minimum}..*";
}
