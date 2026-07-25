namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>A canonical name inside the portable profile.</summary>
/// <remarks>
/// The portable profile narrows the Brontide canonical-name rule to ASCII letters, digits,
/// underscore, and hyphen. A Unicode-letter name is a valid Brontide canonical name but is not
/// portable in binding 0.1, so the narrowing is enforced here rather than left implicit at a codec.
/// </remarks>
public readonly record struct PortableName : IComparable<PortableName>
{
    public const int MaxBytes = 256;

    private PortableName(string value) => Value = value;

    public string Value { get; }

    public static PortableName Parse(string? value) =>
        TryParse(value, out var name)
            ? name
            : throw PortableFaultException.Malformed(
                "name-profile",
                $"'{value}' is outside the portable canonical-name profile.");

    public static bool TryParse(string? value, out PortableName name)
    {
        name = default;
        if (string.IsNullOrEmpty(value) || value.Length > MaxBytes)
        {
            return false;
        }

        var colon = value.IndexOf(':', StringComparison.Ordinal);
        if (colon != value.LastIndexOf(':'))
        {
            return false;
        }

        if (colon >= 0)
        {
            if (!IsPath(value[..colon]) || !IsPath(value[(colon + 1)..]))
            {
                return false;
            }
        }
        else if (!IsPath(value))
        {
            return false;
        }

        name = new PortableName(value);
        return true;
    }

    private static bool IsPath(string path)
    {
        if (path.Length == 0)
        {
            return false;
        }

        foreach (var segment in path.Split('.'))
        {
            if (segment.Length == 0 || !segment.All(IsSegmentCharacter))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSegmentCharacter(char character) =>
        character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '-';

    public int CompareTo(PortableName other) => StringComparer.Ordinal.Compare(Value, other.Value);

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>
/// A member token: a record field name, a choice alternative name, or a lifecycle feature name.
/// </summary>
public readonly record struct PortableMemberToken
{
    private PortableMemberToken(string value) => Value = value;

    public string Value { get; }

    public static PortableMemberToken Parse(string? value) =>
        TryParse(value, out var token)
            ? token
            : throw PortableFaultException.Malformed(
                "member-token",
                $"'{value}' is outside the portable member-token profile.");

    public static bool TryParse(string? value, out PortableMemberToken token)
    {
        token = default;
        if (string.IsNullOrEmpty(value) || value.Length > PortableName.MaxBytes)
        {
            return false;
        }

        foreach (var character in value)
        {
            var permitted = character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or
                (>= '0' and <= '9') or '_' or '-';
            if (!permitted)
            {
                return false;
            }
        }

        token = new PortableMemberToken(value);
        return true;
    }

    public override string ToString() => Value ?? string.Empty;
}

internal static class PortableReferenceSyntax
{
    public static int RequireVersion(int version) =>
        version >= 1
            ? version
            : throw PortableFaultException.Malformed("reference-version", "A canonical reference version must be at least 1.");

    /// <summary>Splits the text rendering on the last '@', which is the only permitted separator.</summary>
    public static bool TrySplit(string? text, out PortableName name, out int version)
    {
        name = default;
        version = 0;
        var separator = text?.LastIndexOf('@') ?? -1;
        return separator > 0 &&
            int.TryParse(text![(separator + 1)..], out version) &&
            version >= 1 &&
            PortableName.TryParse(text[..separator], out name);
    }
}

/// <summary>The Component whose contract is established.</summary>
public readonly record struct PortableComponentReference
{
    public PortableComponentReference(PortableName name, int version)
    {
        Name = name;
        Version = PortableReferenceSyntax.RequireVersion(version);
    }

    public PortableName Name { get; }
    public int Version { get; }

    public static PortableComponentReference Parse(string name, int version) => new(PortableName.Parse(name), version);

    public override string ToString() => $"{Name}@{Version}";
}

/// <summary>The endpoint offering the Component.</summary>
public readonly record struct PortableProviderReference
{
    public PortableProviderReference(PortableName name, int version)
    {
        Name = name;
        Version = PortableReferenceSyntax.RequireVersion(version);
    }

    public PortableName Name { get; }
    public int Version { get; }

    public static PortableProviderReference Parse(string name, int version) => new(PortableName.Parse(name), version);

    public override string ToString() => $"{Name}@{Version}";
}

/// <summary>A negotiated Operation.</summary>
public readonly record struct PortableOperationReference
{
    public PortableOperationReference(PortableName name, int version)
    {
        Name = name;
        Version = PortableReferenceSyntax.RequireVersion(version);
    }

    public PortableName Name { get; }
    public int Version { get; }

    public static PortableOperationReference Parse(string name, int version) => new(PortableName.Parse(name), version);

    public override string ToString() => $"{Name}@{Version}";
}

/// <summary>An input, result, or detail Shape.</summary>
public readonly record struct PortableShapeReference : IComparable<PortableShapeReference>
{
    public PortableShapeReference(PortableName name, int version)
    {
        Name = name;
        Version = PortableReferenceSyntax.RequireVersion(version);
    }

    public PortableName Name { get; }
    public int Version { get; }

    public static PortableShapeReference Parse(string name, int version) => new(PortableName.Parse(name), version);

    public int CompareTo(PortableShapeReference other)
    {
        var byName = Name.CompareTo(other.Name);
        return byName != 0 ? byName : Version.CompareTo(other.Version);
    }

    public override string ToString() => $"{Name}@{Version}";
}

/// <summary>A declared Fragment.</summary>
public readonly record struct PortableFragmentReference : IComparable<PortableFragmentReference>
{
    public PortableFragmentReference(PortableName name, int version)
    {
        Name = name;
        Version = PortableReferenceSyntax.RequireVersion(version);
    }

    public PortableName Name { get; }
    public int Version { get; }

    public static PortableFragmentReference Parse(string name, int version) => new(PortableName.Parse(name), version);

    /// <summary>
    /// The text rendering used where a single string key is structurally required, which is the
    /// fragment map of a record value. It is a rendering of the structured form, never a distinct
    /// identity.
    /// </summary>
    public string CanonicalText => $"{Name}@{Version}";

    public static PortableFragmentReference ParseText(string? text) =>
        PortableReferenceSyntax.TrySplit(text, out var name, out var version)
            ? new PortableFragmentReference(name, version)
            : throw PortableFaultException.Malformed(
                "fragment-key",
                $"'{text}' is not a canonical '<name>@<version>' Fragment key.");

    public int CompareTo(PortableFragmentReference other)
    {
        var byName = Name.CompareTo(other.Name);
        return byName != 0 ? byName : Version.CompareTo(other.Version);
    }

    public override string ToString() => CanonicalText;
}

/// <summary>A declared profile, binding, feature, or resource-flavor dependency.</summary>
public readonly record struct PortableDependencyReference
{
    public PortableDependencyReference(PortableName name, int version)
    {
        Name = name;
        Version = PortableReferenceSyntax.RequireVersion(version);
    }

    public PortableName Name { get; }
    public int Version { get; }

    public static PortableDependencyReference Parse(string name, int version) => new(PortableName.Parse(name), version);

    public override string ToString() => $"{Name}@{Version}";
}

/// <summary>Identifies one binding scope. It is opaque and is never persisted as identity.</summary>
public readonly record struct PortablePlanId(string Value)
{
    public static PortablePlanId New() => new($"plan-{Guid.NewGuid():n}");

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>Channel correlation for one established binding.</summary>
public readonly record struct PortableChannelId(string Value)
{
    public static PortableChannelId New() => new($"ch-{Guid.NewGuid():n}");

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>Channel correlation for one request within one binding; also the replay identity.</summary>
public readonly record struct PortableChannelRequestId(string Value)
{
    public static PortableChannelRequestId New() => new($"rq-{Guid.NewGuid():n}");

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>Optional Channel correlation carried only when the peer echoes it.</summary>
public readonly record struct PortableChannelExecutionId(string Value)
{
    public static PortableChannelExecutionId New() => new($"ex-{Guid.NewGuid():n}");

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>
/// The host's own Execution identity. The Channel identity rule requires it to stay distinct from
/// every Channel correlation identity, so it is a separate type rather than a reused string.
/// </summary>
public readonly record struct PortableHostExecutionId(string Value)
{
    public static PortableHostExecutionId New() => new($"host-exec-{Guid.NewGuid():n}");

    public override string ToString() => Value ?? string.Empty;
}

public enum PortableIdentitySpace
{
    Operation,
    Shape,
    Fragment
}

/// <summary>
/// A binding-scoped compact identifier. It is assigned only after canonical negotiation succeeds,
/// is meaningless outside its binding, and never appears in a plan fact or observation as identity.
/// </summary>
public readonly record struct PortableCompactIdentifier
{
    public const int MaxValue = 65_535;

    public PortableCompactIdentifier(int value)
    {
        if (value is < 0 or > MaxValue)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "compact-identifier-range",
                "A compact identifier occupies the unsigned range 0..65535.");
        }

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>One compact-identifier assignment recorded as plan data.</summary>
public sealed record PortableCompactAssignment(
    PortableIdentitySpace Space,
    string Reference,
    PortableCompactIdentifier CompactId);
