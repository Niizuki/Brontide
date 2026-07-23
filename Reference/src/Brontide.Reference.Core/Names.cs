using System.Diagnostics.CodeAnalysis;

namespace Brontide.Reference.Core;

/// <summary>A structurally legible, semantically opaque Brontide canonical name.</summary>
public readonly record struct CanonicalName : IComparable<CanonicalName>
{
    private CanonicalName(string value) => Value = value;

    public string Value { get; }

    public static CanonicalName Parse(string value)
    {
        if (!TryParse(value, out var name))
        {
            throw new FormatException($"'{value}' is not an Brontide canonical name.");
        }

        return name;
    }

    public static bool TryParse(string? value, out CanonicalName name)
    {
        name = default;
        if (string.IsNullOrWhiteSpace(value) || value != value.Trim())
        {
            return false;
        }

        var colon = value.IndexOf(':');
        if (colon != value.LastIndexOf(':'))
        {
            return false;
        }

        if (colon >= 0 && (!ValidPath(value[..colon]) || !ValidPath(value[(colon + 1)..])))
        {
            return false;
        }

        if (colon < 0 && !ValidPath(value))
        {
            return false;
        }

        name = new CanonicalName(value);
        return true;
    }

    private static bool ValidPath(string path) =>
        path.Split('.').All(segment => segment.Length > 0 && segment.All(character =>
            char.IsLetterOrDigit(character) || character is '_' or '-'));

    public int CompareTo(CanonicalName other) => StringComparer.Ordinal.Compare(Value, other.Value);

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>
/// An open, validated typed-member category token. Architecture 0.7 records examples such as
/// <c>Store</c> and <c>Parameter</c> but deliberately does not freeze a closed catalogue.
/// </summary>
public readonly record struct MemberKind : IComparable<MemberKind>
{
    private MemberKind(string value) => Value = value;

    public string Value { get; }

    public static MemberKind Parse(string value)
    {
        if (!TryParse(value, out var kind))
        {
            throw new FormatException($"'{value}' is not a Brontide member kind.");
        }

        return kind;
    }

    public static bool TryParse(string? value, out MemberKind kind)
    {
        kind = default;
        if (!MemberTokenSyntax.IsValid(value))
        {
            return false;
        }

        kind = new MemberKind(value!);
        return true;
    }

    public int CompareTo(MemberKind other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value ?? string.Empty;
}

/// <summary>A validated local name within one typed-member identity.</summary>
public readonly record struct MemberName : IComparable<MemberName>
{
    private MemberName(string value) => Value = value;

    public string Value { get; }

    public static MemberName Parse(string value)
    {
        if (!TryParse(value, out var name))
        {
            throw new FormatException($"'{value}' is not a Brontide member name.");
        }

        return name;
    }

    public static bool TryParse(string? value, out MemberName name)
    {
        name = default;
        if (!MemberTokenSyntax.IsValid(value))
        {
            return false;
        }

        name = new MemberName(value!);
        return true;
    }

    public int CompareTo(MemberName other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value ?? string.Empty;
}

/// <summary>
/// A typed member identity in strict
/// <c>[AuthorityPath:]ConceptPath#MemberKind.MemberName</c> form.
/// </summary>
public readonly record struct CanonicalMemberName : IComparable<CanonicalMemberName>
{
    private CanonicalMemberName(CanonicalName owner, MemberKind kind, MemberName name)
    {
        Owner = owner;
        Kind = kind;
        Name = name;
    }

    public CanonicalName Owner { get; }
    public MemberKind Kind { get; }
    public MemberName Name { get; }
    public string Value => $"{Owner}#{Kind}.{Name}";

    public static CanonicalMemberName Parse(string value)
    {
        if (!TryParse(value, out var name))
        {
            throw new FormatException($"'{value}' is not a Brontide typed-member canonical name.");
        }

        return name;
    }

    public static bool TryParse(string? value, out CanonicalMemberName name)
    {
        name = default;
        if (string.IsNullOrWhiteSpace(value) || value != value.Trim())
        {
            return false;
        }

        var memberSeparator = value.IndexOf('#');
        if (memberSeparator <= 0 || memberSeparator != value.LastIndexOf('#'))
        {
            return false;
        }

        var kindSeparator = value.IndexOf('.', memberSeparator + 1);
        if (kindSeparator <= memberSeparator + 1 || kindSeparator != value.LastIndexOf('.'))
        {
            return false;
        }

        if (!CanonicalName.TryParse(value[..memberSeparator], out var owner)
            || !MemberKind.TryParse(value[(memberSeparator + 1)..kindSeparator], out var kind)
            || !MemberName.TryParse(value[(kindSeparator + 1)..], out var memberName))
        {
            return false;
        }

        name = new CanonicalMemberName(owner, kind, memberName);
        return true;
    }

    public int CompareTo(CanonicalMemberName other) =>
        StringComparer.Ordinal.Compare(Value, other.Value);

    public override string ToString() => Value;
}

internal static class MemberTokenSyntax
{
    public static bool IsValid([NotNullWhen(true)] string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value == value.Trim()
        && value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-');
}

public readonly record struct OperationReference(CanonicalName Name)
{
    public static OperationReference Parse(string name) => new(CanonicalName.Parse(name));
    public override string ToString() => Name.ToString();
}

public readonly record struct EventReference(CanonicalName Name)
{
    public static EventReference Parse(string name) => new(CanonicalName.Parse(name));
    public override string ToString() => Name.ToString();
}

public readonly record struct ShapeReference
{
    public ShapeReference(CanonicalName name, int version)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
        Name = name;
        Version = version;
    }

    public CanonicalName Name { get; }
    public int Version { get; }

    public static ShapeReference Parse(string name, int version) => new(CanonicalName.Parse(name), version);
    public override string ToString() => $"{Name} {Version}";
}

public readonly record struct FragmentReference
{
    public FragmentReference(CanonicalName name, int version)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
        Name = name;
        Version = version;
    }

    public CanonicalName Name { get; }
    public int Version { get; }

    public static FragmentReference Parse(string name, int version) => new(CanonicalName.Parse(name), version);
    public override string ToString() => $"{Name} {Version}";
}

public readonly record struct ExecutionId(Guid Value)
{
    public static ExecutionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct OccurrenceReference(Guid Value)
{
    public static OccurrenceReference New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N");
}

public readonly record struct ActivityReference(Guid Value, CanonicalName Kind)
{
    internal static ActivityReference New(CanonicalName kind) => new(Guid.NewGuid(), kind);
    public override string ToString() => $"{Kind}:{Value:N}";
}

public enum TerminalReferenceKind
{
    Execution,
    Activity
}

public readonly record struct TerminalReference(TerminalReferenceKind Kind, Guid Value)
{
    public static TerminalReference For(ExecutionId execution) => new(TerminalReferenceKind.Execution, execution.Value);
    public static TerminalReference For(ActivityReference activity) => new(TerminalReferenceKind.Activity, activity.Value);
    public override string ToString() => $"{Kind}:{Value:N}";
}

/// <summary>
/// Opaque, domain-issued Actor designator. It deliberately retains object-reference equality.
/// </summary>
public sealed class ActorReference
{
    internal ActorReference(Guid domainId, string displayName)
    {
        DomainId = domainId;
        Id = Guid.NewGuid();
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? throw new ArgumentException("An Actor display name is required.", nameof(displayName))
            : displayName;
    }

    internal Guid DomainId { get; }
    internal Guid Id { get; }
    public string DisplayName { get; }

    public override string ToString() => DisplayName;
}

internal static class Guard
{
    public static T NotNull<T>([NotNull] T? value, string parameterName) where T : class =>
        value ?? throw new ArgumentNullException(parameterName);
}
