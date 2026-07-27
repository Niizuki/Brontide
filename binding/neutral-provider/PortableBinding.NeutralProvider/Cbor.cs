using System.Formats.Cbor;

namespace PortableBinding.NeutralProvider;

/// <summary>
/// The subset of CBOR the portable representation declares, as a small immutable tree.
/// </summary>
/// <remarks>
/// Reading and writing go through the base class library's <see cref="CborReader"/> and
/// <see cref="CborWriter"/>. Only the deterministic map ordering is applied here, because the
/// library's canonical mode is CTAP2 length-first ordering and the portable representation declares
/// RFC 8949 section 4.2.1 bytewise ordering on the encoded key.
/// </remarks>
internal abstract record Cbor
{
    public static Cbor Text(string value) => new CborTextValue(value);

    public static Cbor Integer(long value) => new CborIntegerValue(value);

    public static Cbor Boolean(bool value) => new CborBooleanValue(value);

    public static Cbor Bytes(byte[] value) => new CborBytesValue(value);

    public static Cbor Null { get; } = new CborNullValue();

    public static Cbor Array(params Cbor[] items) => new CborArrayValue(items);

    public static Cbor Array(IEnumerable<Cbor> items) => new CborArrayValue([.. items]);

    public static Cbor Map(params (string Key, Cbor Value)[] entries) => new CborMapValue(entries);

    public static Cbor Map(IEnumerable<(string Key, Cbor Value)> entries) => new CborMapValue([.. entries]);

    /// <summary>A canonical reference, the one compound the contract repeats everywhere.</summary>
    public static Cbor Reference(string name, int version) =>
        Map(("name", Text(name)), ("version", Integer(version)));

    public byte[] Encode()
    {
        var writer = new CborWriter(CborConformanceMode.Strict);
        Write(writer);
        return writer.Encode();
    }

    public static Cbor Decode(byte[] frame)
    {
        var reader = new CborReader(frame, CborConformanceMode.Strict);
        var value = Read(reader);
        if (reader.BytesRemaining != 0)
        {
            throw new PortableFault("malformed-message", "trailing-bytes", "A frame carries bytes after its item.");
        }

        return value;
    }

    protected abstract void Write(CborWriter writer);

    private static Cbor Read(CborReader reader) =>
        reader.PeekState() switch
        {
            CborReaderState.UnsignedInteger or CborReaderState.NegativeInteger => Integer(reader.ReadInt64()),
            CborReaderState.TextString => Text(reader.ReadTextString()),
            CborReaderState.ByteString => Bytes(reader.ReadByteString()),
            CborReaderState.Boolean => Boolean(reader.ReadBoolean()),
            CborReaderState.Null => ReadNull(reader),
            CborReaderState.StartArray => ReadArray(reader),
            CborReaderState.StartMap => ReadMap(reader),
            var state => throw new PortableFault(
                "malformed-message",
                "unsupported-major-type",
                $"The portable representation declares no CBOR item of state '{state}'.")
        };

    private static Cbor ReadNull(CborReader reader)
    {
        reader.ReadNull();
        return Null;
    }

    private static Cbor ReadArray(CborReader reader)
    {
        var count = reader.ReadStartArray()
            ?? throw new PortableFault("malformed-message", "indefinite-length", "Definite lengths are required.");
        var items = new List<Cbor>(count);
        for (var index = 0; index < count; index++)
        {
            items.Add(Read(reader));
        }

        reader.ReadEndArray();
        return Array(items);
    }

    private static Cbor ReadMap(CborReader reader)
    {
        var count = reader.ReadStartMap()
            ?? throw new PortableFault("malformed-message", "indefinite-length", "Definite lengths are required.");
        var entries = new List<(string, Cbor)>(count);
        for (var index = 0; index < count; index++)
        {
            if (reader.PeekState() != CborReaderState.TextString)
            {
                throw new PortableFault("malformed-message", "map-key", "Every declared map key is a text string.");
            }

            entries.Add((reader.ReadTextString(), Read(reader)));
        }

        reader.ReadEndMap();
        return Map(entries);
    }

    /// <summary>Bytewise comparison of two encoded map keys, as the representation declares.</summary>
    internal static int CompareEncodedKeys(byte[] left, byte[] right)
    {
        var shared = Math.Min(left.Length, right.Length);
        for (var index = 0; index < shared; index++)
        {
            if (left[index] != right[index])
            {
                return left[index] - right[index];
            }
        }

        return left.Length - right.Length;
    }
}

internal sealed record CborTextValue(string Value) : Cbor
{
    protected override void Write(CborWriter writer) => writer.WriteTextString(Value);
}

internal sealed record CborIntegerValue(long Value) : Cbor
{
    protected override void Write(CborWriter writer) => writer.WriteInt64(Value);
}

internal sealed record CborBooleanValue(bool Value) : Cbor
{
    protected override void Write(CborWriter writer) => writer.WriteBoolean(Value);
}

internal sealed record CborBytesValue(byte[] Value) : Cbor
{
    protected override void Write(CborWriter writer) => writer.WriteByteString(Value);
}

internal sealed record CborNullValue : Cbor
{
    protected override void Write(CborWriter writer) => writer.WriteNull();
}

internal sealed record CborArrayValue(IReadOnlyList<Cbor> Items) : Cbor
{
    protected override void Write(CborWriter writer)
    {
        writer.WriteStartArray(Items.Count);
        foreach (var item in Items)
        {
            var nested = new CborWriter(CborConformanceMode.Strict);
            item.WriteTo(nested);
            writer.WriteEncodedValue(nested.Encode());
        }

        writer.WriteEndArray();
    }
}

internal sealed record CborMapValue(IReadOnlyList<(string Key, Cbor Value)> Entries) : Cbor
{
    protected override void Write(CborWriter writer)
    {
        // Sorting on the complete encoded key, not on the string, is what the representation
        // declares. The two orders disagree as soon as one key is a prefix of another or a
        // multi-byte UTF-8 sequence is involved.
        var ordered = Entries
            .Select(entry =>
            {
                var keyWriter = new CborWriter(CborConformanceMode.Strict);
                keyWriter.WriteTextString(entry.Key);
                return (Encoded: keyWriter.Encode(), entry.Key, entry.Value);
            })
            .Order(Comparer<(byte[] Encoded, string Key, Cbor Value)>.Create(
                (left, right) => CompareEncodedKeys(left.Encoded, right.Encoded)))
            .ToList();

        writer.WriteStartMap(ordered.Count);
        foreach (var entry in ordered)
        {
            writer.WriteEncodedValue(entry.Encoded);
            var nested = new CborWriter(CborConformanceMode.Strict);
            entry.Value.WriteTo(nested);
            writer.WriteEncodedValue(nested.Encode());
        }

        writer.WriteEndMap();
    }
}

internal static class CborWriteExtensions
{
    /// <summary>Lets a nested item write itself through the same protected path.</summary>
    public static void WriteTo(this Cbor item, CborWriter writer)
    {
        var encoded = item.Encode();
        writer.WriteEncodedValue(encoded);
    }
}

/// <summary>Reading helpers that fail with a portable category rather than a parser exception.</summary>
internal static class CborAccess
{
    public static IReadOnlyList<(string Key, Cbor Value)> Entries(this Cbor item, string context) =>
        item is CborMapValue map
            ? map.Entries
            : throw new PortableFault("malformed-message", context, $"'{context}' is not a map.");

    public static bool Has(this Cbor item, string key) =>
        item is CborMapValue map && map.Entries.Any(entry => entry.Key == key);

    public static Cbor Field(this Cbor item, string key, string context)
    {
        foreach (var entry in item.Entries(context))
        {
            if (entry.Key == key)
            {
                return entry.Value;
            }
        }

        throw new PortableFault("malformed-message", context, $"'{context}' declares no field '{key}'.");
    }

    public static string TextField(this Cbor item, string key, string context) =>
        item.Field(key, context) is CborTextValue text
            ? text.Value
            : throw new PortableFault("malformed-message", context, $"Field '{key}' is not text.");

    public static long IntegerField(this Cbor item, string key, string context) =>
        item.Field(key, context) is CborIntegerValue integer
            ? integer.Value
            : throw new PortableFault("malformed-message", context, $"Field '{key}' is not an integer.");

    public static IReadOnlyList<Cbor> Items(this Cbor item, string context) =>
        item is CborArrayValue array
            ? array.Items
            : throw new PortableFault("malformed-message", context, $"'{context}' is not an array.");

    public static (string Name, int Version) Reference(this Cbor item, string context) =>
        (item.TextField("name", context), (int)item.IntegerField("version", context));
}
