using System.Collections.Immutable;
using System.Text;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>A value in the deterministic CBOR core the portable representation permits.</summary>
/// <remarks>
/// The model is deliberately narrower than CBOR: there is no float, no indefinite length, no simple
/// value beyond true/false/null, and exactly one tag. A value outside the model cannot be
/// constructed, so the encoder cannot emit something the decoder would have to reject.
/// </remarks>
public abstract record CborItem;

public sealed record CborInteger(long Value) : CborItem;

public sealed record CborText(string Value) : CborItem;

public sealed record CborBytes(ReadOnlyMemory<byte> Value) : CborItem;

public sealed record CborBoolean(bool Value) : CborItem;

public sealed record CborNull : CborItem
{
    public static CborNull Instance { get; } = new();
}

/// <summary>The single allowlisted tag: an exact decimal fraction encoded as tag 4.</summary>
public sealed record CborDecimal(int Exponent, long Mantissa) : CborItem
{
    public const int MinExponent = -28;
    public const int MaxExponent = 28;

    /// <summary>
    /// Normalizes so that one value has exactly one encoding: a mantissa divisible by ten is scaled
    /// up, and a zero mantissa always carries exponent zero.
    /// </summary>
    public static CborDecimal Normalize(int exponent, long mantissa)
    {
        if (mantissa == 0)
        {
            return new CborDecimal(0, 0);
        }

        while (mantissa % 10 == 0 && exponent < MaxExponent)
        {
            mantissa /= 10;
            exponent++;
        }

        if (exponent is < MinExponent or > MaxExponent)
        {
            throw PortableFaultException.Malformed(
                "decimal-exponent",
                "A Decimal exponent outside -28..28 is not representable in the portable core.");
        }

        return new CborDecimal(exponent, mantissa);
    }
}

public sealed record CborArray(ImmutableArray<CborItem> Items) : CborItem
{
    public static CborArray Of(params CborItem[] items) => new([.. items]);
}

public sealed record CborMap(ImmutableArray<CborMapEntry> Entries) : CborItem
{
    public static CborMap Empty { get; } = new([]);

    public static CborMap Of(IEnumerable<(string Key, CborItem Value)> entries) =>
        new([.. entries.Select(entry => new CborMapEntry(entry.Key, entry.Value))]);

    public bool TryGet(string key, out CborItem value)
    {
        foreach (var entry in Entries)
        {
            if (string.Equals(entry.Key, key, StringComparison.Ordinal))
            {
                value = entry.Value;
                return true;
            }
        }

        value = CborNull.Instance;
        return false;
    }

    public bool Contains(string key) => TryGet(key, out _);
}

/// <summary>
/// One map entry. Every map key in this contract is a text string, so the key is modelled as one
/// rather than as a general item.
/// </summary>
public sealed record CborMapEntry(string Key, CborItem Value);

/// <summary>
/// The deterministic CBOR core encoder and decoder (RFC 8949 section 4.2.1), restricted to the
/// subset the portable representation declares.
/// </summary>
/// <remarks>
/// Map keys sort by the bytewise order of their complete encoding, not by ordinal string
/// comparison. The two orders disagree once key lengths cross an encoding-width boundary, so a
/// codec that reuses an ordinal comparer produces bytes this decoder rejects.
/// </remarks>
public static class PortableCbor
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static byte[] Encode(CborItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var buffer = new PortableByteBuffer();
        Write(buffer, item);
        return buffer.ToArray();
    }

    public static CborItem Decode(ReadOnlySpan<byte> body, PortableLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        var position = 0;
        var item = Read(body, ref position, 1, limits);
        if (position != body.Length)
        {
            throw PortableFaultException.Malformed(
                "trailing-bytes",
                "One frame carries exactly one top-level item; trailing bytes are not permitted.");
        }

        return item;
    }

    /// <summary>Orders two complete key encodings by the deterministic bytewise rule.</summary>
    public static int CompareKeyEncodings(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        var shared = Math.Min(left.Length, right.Length);
        for (var index = 0; index < shared; index++)
        {
            if (left[index] != right[index])
            {
                return left[index] < right[index] ? -1 : 1;
            }
        }

        return left.Length.CompareTo(right.Length);
    }

    // ---------------------------------------------------------------------
    // Encoding
    // ---------------------------------------------------------------------

    private static void Write(PortableByteBuffer buffer, CborItem item)
    {
        switch (item)
        {
            case CborInteger integer when integer.Value >= 0:
                WriteHead(buffer, 0, (ulong)integer.Value);
                break;
            case CborInteger integer:
                WriteHead(buffer, 1, unchecked((ulong)(-1 - integer.Value)));
                break;
            case CborBoolean boolean:
                buffer.Write(boolean.Value ? (byte)0xF5 : (byte)0xF4);
                break;
            case CborNull:
                buffer.Write((byte)0xF6);
                break;
            case CborText text:
            {
                var bytes = StrictUtf8.GetBytes(text.Value);
                WriteHead(buffer, 3, (ulong)bytes.Length);
                buffer.Write(bytes);
                break;
            }
            case CborBytes bytes:
                WriteHead(buffer, 2, (ulong)bytes.Value.Length);
                buffer.Write(bytes.Value.Span);
                break;
            case CborDecimal number:
            {
                var normalized = CborDecimal.Normalize(number.Exponent, number.Mantissa);
                WriteHead(buffer, 6, 4);
                WriteHead(buffer, 4, 2);
                Write(buffer, new CborInteger(normalized.Exponent));
                Write(buffer, new CborInteger(normalized.Mantissa));
                break;
            }
            case CborArray array:
                WriteHead(buffer, 4, (ulong)array.Items.Length);
                foreach (var element in array.Items)
                {
                    Write(buffer, element);
                }

                break;
            case CborMap map:
                WriteMap(buffer, map);
                break;
            default:
                throw PortableFaultException.Malformed(
                    "unencodable-item",
                    "The portable core cannot encode this item.");
        }
    }

    private static void WriteMap(PortableByteBuffer buffer, CborMap map)
    {
        var encoded = new List<(byte[] Key, byte[] Value)>(map.Entries.Length);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in map.Entries)
        {
            if (!seen.Add(entry.Key))
            {
                throw PortableFaultException.Malformed(
                    "duplicate-key",
                    $"Map key '{entry.Key}' appears more than once.");
            }

            encoded.Add((Encode(new CborText(entry.Key)), Encode(entry.Value)));
        }

        encoded.Sort(static (left, right) => CompareKeyEncodings(left.Key, right.Key));
        WriteHead(buffer, 5, (ulong)encoded.Count);
        foreach (var entry in encoded)
        {
            buffer.Write(entry.Key);
            buffer.Write(entry.Value);
        }
    }

    private static void WriteHead(PortableByteBuffer buffer, int major, ulong argument)
    {
        var prefix = (byte)(major << 5);
        if (argument < 24)
        {
            buffer.Write((byte)(prefix | (byte)argument));
        }
        else if (argument <= byte.MaxValue)
        {
            buffer.Write((byte)(prefix | 24));
            buffer.Write((byte)argument);
        }
        else if (argument <= ushort.MaxValue)
        {
            buffer.Write((byte)(prefix | 25));
            WriteBigEndian(buffer, argument, 2);
        }
        else if (argument <= uint.MaxValue)
        {
            buffer.Write((byte)(prefix | 26));
            WriteBigEndian(buffer, argument, 4);
        }
        else
        {
            buffer.Write((byte)(prefix | 27));
            WriteBigEndian(buffer, argument, 8);
        }
    }

    private static void WriteBigEndian(PortableByteBuffer buffer, ulong argument, int width)
    {
        for (var shift = (width - 1) * 8; shift >= 0; shift -= 8)
        {
            buffer.Write((byte)((argument >> shift) & 0xFF));
        }
    }

    // ---------------------------------------------------------------------
    // Decoding
    // ---------------------------------------------------------------------

    private static CborItem Read(ReadOnlySpan<byte> body, ref int position, int depth, PortableLimits limits)
    {
        if (depth > limits.MaxNestingDepth)
        {
            throw PortableFaultException.LimitExceeded(
                "nesting-depth",
                $"Decoding stopped at the declared nesting depth of {limits.MaxNestingDepth}.");
        }

        var head = Take(body, ref position, 1)[0];
        var major = head >> 5;
        var additional = head & 0x1F;

        if (major == 7)
        {
            return additional switch
            {
                20 => new CborBoolean(false),
                21 => new CborBoolean(true),
                22 => CborNull.Instance,
                _ => throw PortableFaultException.Malformed(
                    "simple-value",
                    "Only the simple values 20, 21, and 22 belong to the deterministic core; floats are excluded.")
            };
        }

        var argument = ReadArgument(body, ref position, additional);
        switch (major)
        {
            case 0:
                if (argument > long.MaxValue)
                {
                    throw PortableFaultException.Malformed(
                        "integer-domain",
                        "An unsigned integer outside the Integer.Signed64 domain is not portable.");
                }

                return new CborInteger((long)argument);
            case 1:
                if (argument > long.MaxValue)
                {
                    throw PortableFaultException.Malformed(
                        "integer-domain",
                        "A negative integer outside the Integer.Signed64 domain is not portable.");
                }

                return new CborInteger(unchecked(-1L - (long)argument));
            case 2:
            {
                var length = RequireLength(argument, limits.MaxByteStringBytes, "byte-string");
                return new CborBytes(Take(body, ref position, length).ToArray());
            }
            case 3:
            {
                var length = RequireLength(argument, limits.MaxTextBytes, "text-string");
                var bytes = Take(body, ref position, length);
                try
                {
                    return new CborText(StrictUtf8.GetString(bytes));
                }
                catch (DecoderFallbackException)
                {
                    throw PortableFaultException.Malformed("text-encoding", "A text string is not well-formed UTF-8.");
                }
            }
            case 4:
            {
                var count = RequireLength(argument, limits.MaxSequenceItems, "array");
                var items = ImmutableArray.CreateBuilder<CborItem>(count);
                for (var index = 0; index < count; index++)
                {
                    items.Add(Read(body, ref position, depth + 1, limits));
                }

                return new CborArray(items.MoveToImmutable());
            }
            case 5:
                return ReadMap(body, ref position, depth, limits, argument);
            case 6:
                return ReadTag(body, ref position, depth, limits, argument);
            default:
                throw PortableFaultException.Malformed("major-type", "The item uses a major type outside the portable core.");
        }
    }

    private static CborMap ReadMap(
        ReadOnlySpan<byte> body,
        ref int position,
        int depth,
        PortableLimits limits,
        ulong argument)
    {
        var count = RequireLength(argument, limits.MaxRecordFields, "map");
        var entries = ImmutableArray.CreateBuilder<CborMapEntry>(count);
        var previousKeyStart = -1;
        var previousKeyEnd = -1;
        for (var index = 0; index < count; index++)
        {
            var keyStart = position;
            var key = Read(body, ref position, depth + 1, limits) as CborText ??
                throw PortableFaultException.Malformed("map-key-kind", "Every portable map key is a text string.");
            var keyEnd = position;

            if (previousKeyStart >= 0)
            {
                var order = CompareKeyEncodings(
                    body[previousKeyStart..previousKeyEnd],
                    body[keyStart..keyEnd]);
                if (order == 0)
                {
                    throw PortableFaultException.Malformed(
                        "duplicate-key",
                        $"Map key '{key.Value}' appears more than once.");
                }

                if (order > 0)
                {
                    throw PortableFaultException.Malformed(
                        "map-key-order",
                        $"Map key '{key.Value}' breaks ascending deterministic key order.");
                }
            }

            previousKeyStart = keyStart;
            previousKeyEnd = keyEnd;
            entries.Add(new CborMapEntry(key.Value, Read(body, ref position, depth + 1, limits)));
        }

        return new CborMap(entries.MoveToImmutable());
    }

    private static CborDecimal ReadTag(
        ReadOnlySpan<byte> body,
        ref int position,
        int depth,
        PortableLimits limits,
        ulong tag)
    {
        if (tag != 4)
        {
            throw PortableFaultException.Malformed("tag-allowlist", $"Tag {tag} is outside the allowlist, which contains only tag 4.");
        }

        if (Read(body, ref position, depth + 1, limits) is not CborArray array ||
            array.Items.Length != 2 ||
            array.Items[0] is not CborInteger exponent ||
            array.Items[1] is not CborInteger mantissa)
        {
            throw PortableFaultException.Malformed(
                "decimal-body",
                "Tag 4 carries an array of exactly two integers: [exponent, mantissa].");
        }

        if (exponent.Value is < CborDecimal.MinExponent or > CborDecimal.MaxExponent)
        {
            throw PortableFaultException.Malformed("decimal-exponent", "A Decimal exponent must lie in -28..28.");
        }

        if (mantissa.Value == 0)
        {
            if (exponent.Value != 0)
            {
                throw PortableFaultException.Malformed(
                    "decimal-canonical",
                    "A zero mantissa is canonically encoded with exponent 0.");
            }
        }
        else if (mantissa.Value % 10 == 0)
        {
            throw PortableFaultException.Malformed(
                "decimal-canonical",
                "A Decimal mantissa is normalized so that it is not divisible by ten.");
        }

        return new CborDecimal((int)exponent.Value, mantissa.Value);
    }

    private static ulong ReadArgument(ReadOnlySpan<byte> body, ref int position, int additional)
    {
        switch (additional)
        {
            case < 24:
                return (ulong)additional;
            case 24:
            {
                var value = (ulong)Take(body, ref position, 1)[0];
                return RequireShortest(value, 24);
            }
            case 25:
                return RequireShortest(ReadBigEndian(body, ref position, 2), byte.MaxValue + 1UL);
            case 26:
                return RequireShortest(ReadBigEndian(body, ref position, 4), ushort.MaxValue + 1UL);
            case 27:
                return RequireShortest(ReadBigEndian(body, ref position, 8), uint.MaxValue + 1UL);
            case 31:
                throw PortableFaultException.Malformed(
                    "indefinite-length",
                    "Indefinite-length items are excluded from the deterministic core.");
            default:
                throw PortableFaultException.Malformed("reserved-argument", "The argument uses a reserved additional-information value.");
        }
    }

    private static ulong ReadBigEndian(ReadOnlySpan<byte> body, ref int position, int width)
    {
        var bytes = Take(body, ref position, width);
        var value = 0UL;
        foreach (var current in bytes)
        {
            value = (value << 8) | current;
        }

        return value;
    }

    private static ulong RequireShortest(ulong value, ulong minimum) =>
        value >= minimum
            ? value
            : throw PortableFaultException.Malformed(
                "non-shortest-argument",
                "An argument must use the shortest form that represents it exactly.");

    private static int RequireLength(ulong argument, int bound, string what)
    {
        if (argument > (ulong)bound)
        {
            throw PortableFaultException.LimitExceeded(
                $"{what}-bound",
                $"A {what} of {argument} exceeds the declared bound of {bound}.");
        }

        return (int)argument;
    }

    private static ReadOnlySpan<byte> Take(ReadOnlySpan<byte> body, ref int position, int count)
    {
        if (count < 0 || position + count > body.Length)
        {
            throw PortableFaultException.Malformed("truncated-item", "The frame body ends inside an item.");
        }

        var slice = body.Slice(position, count);
        position += count;
        return slice;
    }
}

/// <summary>A minimal growable byte sink; the codec writes forward only and never seeks.</summary>
internal sealed class PortableByteBuffer
{
    private byte[] _buffer = new byte[256];
    private int _count;

    public void Write(byte value)
    {
        EnsureCapacity(1);
        _buffer[_count++] = value;
    }

    public void Write(ReadOnlySpan<byte> values)
    {
        EnsureCapacity(values.Length);
        values.CopyTo(_buffer.AsSpan(_count));
        _count += values.Length;
    }

    public byte[] ToArray() => _buffer.AsSpan(0, _count).ToArray();

    private void EnsureCapacity(int additional)
    {
        if (_count + additional <= _buffer.Length)
        {
            return;
        }

        var capacity = _buffer.Length;
        while (capacity < _count + additional)
        {
            capacity *= 2;
        }

        Array.Resize(ref _buffer, capacity);
    }
}
