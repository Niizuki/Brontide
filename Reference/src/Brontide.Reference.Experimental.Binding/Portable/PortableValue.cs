using System.Collections.Immutable;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>A shaped value in the portable Shape floor.</summary>
/// <remarks>
/// The value model carries no kind discriminator on the wire: the negotiated Shape determines the
/// type of every position. The discriminated model exists in memory so that a mismatch between the
/// declared Shape and the presented value is a decode failure rather than a silent coercion.
/// </remarks>
public abstract record PortableValue;

public sealed record PortableUnitValue : PortableValue
{
    public static PortableUnitValue Instance { get; } = new();
}

public sealed record PortableTextValue(string Value) : PortableValue;

public sealed record PortableBooleanValue(bool Value) : PortableValue;

public sealed record PortableIntegerValue(long Value) : PortableValue;

public sealed record PortableDecimalValue(int Exponent, long Mantissa) : PortableValue;

public sealed record PortableBytesValue(ReadOnlyMemory<byte> Value) : PortableValue;

public sealed record PortableSequenceValue(ImmutableArray<PortableValue> Items) : PortableValue;

public sealed record PortableChoiceValue(string Alternative, PortableValue Value) : PortableValue;

public sealed record PortableRecordValue(
    ImmutableDictionary<string, PortableValue> Fields,
    ImmutableDictionary<PortableFragmentReference, ImmutableDictionary<string, PortableValue>> Fragments)
    : PortableValue
{
    public static PortableRecordValue Empty { get; } = new(
        ImmutableDictionary<string, PortableValue>.Empty.WithComparers(StringComparer.Ordinal),
        ImmutableDictionary<PortableFragmentReference, ImmutableDictionary<string, PortableValue>>.Empty);

    public static PortableRecordValue Of(params (string Name, PortableValue Value)[] fields) =>
        Empty with
        {
            Fields = fields.ToImmutableDictionary(field => field.Name, field => field.Value, StringComparer.Ordinal)
        };

    public PortableRecordValue WithField(string name, PortableValue value) =>
        this with { Fields = Fields.SetItem(name, value) };

    public PortableRecordValue WithFragment(
        PortableFragmentReference fragment,
        params (string Name, PortableValue Value)[] fields) =>
        this with
        {
            Fragments = Fragments.SetItem(
                fragment,
                fields.ToImmutableDictionary(field => field.Name, field => field.Value, StringComparer.Ordinal))
        };

    public PortableValue? Field(string name) => Fields.TryGetValue(name, out var value) ? value : null;
}

/// <summary>
/// Field names that carry foreign runtime identity and may never appear in any position.
/// </summary>
/// <remarks>
/// The outcome follows the position: a rejected name in a control position is
/// <c>malformed-message</c>; in a declared payload position it is <c>invalid-payload</c>. Matching is
/// case-insensitive so a renamed casing does not smuggle the same content across.
/// </remarks>
public static class PortableForbiddenContent
{
    private static readonly ImmutableHashSet<string> Names = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "$type", "typeName", "exception", "stackTrace", "innerException", "targetSite");

    public static bool IsForbidden(string name) => Names.Contains(name);

    /// <summary>Walks a decoded control item and rejects foreign runtime identity as malformed.</summary>
    public static void RequireCleanControlItem(CborItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        switch (item)
        {
            case CborMap map:
                foreach (var entry in map.Entries)
                {
                    if (IsForbidden(entry.Key))
                    {
                        throw PortableFaultException.Malformed(
                            "foreign-runtime-data",
                            $"Control field '{entry.Key}' carries foreign runtime identity.");
                    }

                    RequireCleanControlItem(entry.Value);
                }

                break;
            case CborArray array:
                foreach (var element in array.Items)
                {
                    RequireCleanControlItem(element);
                }

                break;
            default:
                break;
        }
    }

    /// <summary>Walks a decoded payload item and rejects foreign runtime identity as invalid payload.</summary>
    public static void RequireCleanPayloadItem(CborItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        switch (item)
        {
            case CborMap map:
                foreach (var entry in map.Entries)
                {
                    if (IsForbidden(entry.Key))
                    {
                        throw PortableFaultException.InvalidPayload(
                            "foreign-runtime-data",
                            $"Payload field '{entry.Key}' carries foreign runtime identity.");
                    }

                    RequireCleanPayloadItem(entry.Value);
                }

                break;
            case CborArray array:
                foreach (var element in array.Items)
                {
                    RequireCleanPayloadItem(element);
                }

                break;
            default:
                break;
        }
    }
}
