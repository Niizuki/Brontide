using System.Collections.Immutable;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>
/// Strict accessors over a decoded control item. Every miss is a portable category rather than a
/// parser exception, so no foreign runtime failure escapes the decode boundary.
/// </summary>
internal static class CborAccess
{
    public static CborMap RequireMap(CborItem item, string what) =>
        item as CborMap ?? throw PortableFaultException.Malformed($"{what}-kind", $"'{what}' must be a map.");

    public static CborArray RequireArray(CborItem item, string what) =>
        item as CborArray ?? throw PortableFaultException.Malformed($"{what}-kind", $"'{what}' must be an array.");

    public static CborItem Require(this CborMap map, string name) =>
        map.TryGet(name, out var value)
            ? value
            : throw PortableFaultException.Malformed($"{name}-absent", $"The required field '{name}' is absent.");

    public static string RequireText(this CborMap map, string name) =>
        map.Require(name) is CborText text
            ? text.Value
            : throw PortableFaultException.Malformed($"{name}-kind", $"'{name}' must be a text string.");

    public static long RequireInteger(this CborMap map, string name) =>
        map.Require(name) is CborInteger integer
            ? integer.Value
            : throw PortableFaultException.Malformed($"{name}-kind", $"'{name}' must be an integer.");

    public static int RequireInt32(this CborMap map, string name)
    {
        var value = map.RequireInteger(name);
        return value is >= int.MinValue and <= int.MaxValue
            ? (int)value
            : throw PortableFaultException.Malformed($"{name}-range", $"'{name}' is outside the 32-bit range.");
    }

    public static bool RequireBoolean(this CborMap map, string name) =>
        map.Require(name) is CborBoolean boolean
            ? boolean.Value
            : throw PortableFaultException.Malformed($"{name}-kind", $"'{name}' must be a boolean.");

    public static CborMap RequireMap(this CborMap map, string name) => RequireMap(map.Require(name), name);

    public static CborArray RequireArray(this CborMap map, string name) => RequireArray(map.Require(name), name);

    public static ImmutableArray<T> RequireArray<T>(this CborMap map, string name, Func<CborItem, T> read) =>
        [.. map.RequireArray(name).Items.Select(read)];

    public static string? OptionalText(this CborMap map, string name)
    {
        if (!map.TryGet(name, out var value))
        {
            return null;
        }

        return value is CborText text
            ? text.Value
            : throw PortableFaultException.Malformed($"{name}-kind", $"'{name}' must be a text string.");
    }

    /// <summary>Rejects any field the caller did not declare, per the reject-unknown-field policy.</summary>
    public static void RequireDeclaredFields(this CborMap map, string what, params string[] declared)
    {
        var permitted = declared.ToImmutableHashSet(StringComparer.Ordinal);
        foreach (var entry in map.Entries)
        {
            if (!permitted.Contains(entry.Key))
            {
                throw PortableFaultException.Malformed(
                    $"{what}-unknown-field",
                    $"'{what}' declares no field named '{entry.Key}'.");
            }
        }
    }

    public static CborMap Reference(string name, int version) =>
        CborMap.Of([("name", new CborText(name)), ("version", new CborInteger(version))]);

    public static (PortableName Name, int Version) ReadReference(CborItem item, string what)
    {
        var map = RequireMap(item, what);
        map.RequireDeclaredFields(what, "name", "version");
        var name = PortableName.Parse(map.RequireText("name"));
        var version = map.RequireInt32("version");
        return (name, PortableReferenceSyntax.RequireVersion(version));
    }
}
