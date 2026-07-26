using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// The Reference codec against the neutral golden encodings.
/// </summary>
/// <remarks>
/// The bytes are read from <c>binding/portable/vectors/golden-encodings.json</c> rather than
/// restated here. That is what makes them evidence: an encoder that drifts from the deterministic
/// rules fails against the same artifact the other stack will be measured against.
/// </remarks>
public sealed class PortableEncodingTests
{
    private static readonly PortableShapeCatalog Catalog =
        PortableShapeCatalog.FromContract(CoolingPortableFixture.Contract);

    [Test]
    public void Every_golden_encoding_is_reproduced_byte_for_byte()
    {
        using var document = PortableTestHarness.ReadNeutral("vectors", "golden-encodings.json");
        var encodings = document.RootElement.GetProperty("encodings").EnumerateArray().ToArray();
        Assert.That(encodings, Is.Not.Empty);

        foreach (var entry in encodings)
        {
            var id = entry.GetProperty("id").GetString()!;
            var shape = PortableTestHarness.ReadShapeReference(entry.GetProperty("shape"));
            var value = PortableTestHarness.ReadDescribedValue(entry.GetProperty("value"));
            var encoded = PortableCbor.Encode(PortableValueCodec.Encode(Catalog, shape, value));
            Assert.That(
                PortableTestHarness.Hex(encoded),
                Is.EqualTo(entry.GetProperty("cbor").GetString()),
                $"Golden encoding '{id}' was not reproduced.");
        }
    }

    [Test]
    public void Every_golden_encoding_decodes_back_to_its_described_value()
    {
        using var document = PortableTestHarness.ReadNeutral("vectors", "golden-encodings.json");
        foreach (var entry in document.RootElement.GetProperty("encodings").EnumerateArray())
        {
            var shape = PortableTestHarness.ReadShapeReference(entry.GetProperty("shape"));
            var expected = PortableTestHarness.ReadDescribedValue(entry.GetProperty("value"));
            var bytes = PortableTestHarness.FromHex(entry.GetProperty("cbor").GetString()!);
            var decoded = PortableValueCodec.Decode(
                Catalog,
                shape,
                PortableCbor.Decode(bytes, PortableLimits.Declared),
                [CoolingPortableFixture.HostContext]);
            Assert.That(
                PortableTestHarness.Hex(PortableCbor.Encode(PortableValueCodec.Encode(Catalog, shape, decoded))),
                Is.EqualTo(PortableTestHarness.Hex(PortableCbor.Encode(PortableValueCodec.Encode(Catalog, shape, expected)))),
                $"Golden encoding '{entry.GetProperty("id").GetString()}' did not round trip.");
        }
    }

    [Test]
    public void Deterministic_key_order_is_not_ordinal_string_order()
    {
        // G1 is the smallest case where the two disagree: 'loop' precedes 'enabled' because the
        // encoded key sorts first, while ordinal string order would reverse them. A codec that
        // reused the retained JSON comparer would produce the other order here.
        var encoded = PortableTestHarness.Hex(PortableCbor.Encode(PortableValueCodec.Encode(
            Catalog,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true, requesterLabel: null))));
        Assert.That(encoded.IndexOf("6c6f6f70", StringComparison.Ordinal), Is.LessThan(
            encoded.IndexOf("656e61626c6564", StringComparison.Ordinal)));
        Assert.That(
            string.CompareOrdinal("loop", "enabled"),
            Is.GreaterThan(0),
            "Ordinal string order puts 'enabled' first, which is exactly the divergence being pinned.");
    }

    [Test]
    public void The_framed_example_carries_a_four_byte_big_endian_prefix()
    {
        using var document = PortableTestHarness.ReadNeutral("vectors", "golden-encodings.json");
        var framed = document.RootElement.GetProperty("framedExample");
        var source = document.RootElement.GetProperty("encodings").EnumerateArray()
            .Single(entry => entry.GetProperty("id").GetString() == framed.GetProperty("of").GetString());
        var body = PortableTestHarness.FromHex(source.GetProperty("cbor").GetString()!);

        Assert.That(body.Length, Is.EqualTo(framed.GetProperty("bodyBytes").GetInt32()));
        var prefix = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(prefix, (uint)body.Length);
        Assert.That(
            PortableTestHarness.Hex(prefix.Concat(body).ToArray()),
            Is.EqualTo(framed.GetProperty("framed").GetString()));
    }

    [Test]
    public void Every_rejected_encoding_fails_with_its_declared_category()
    {
        using var document = PortableTestHarness.ReadNeutral("vectors", "golden-encodings.json");
        foreach (var entry in document.RootElement.GetProperty("rejectedEncodings").EnumerateArray())
        {
            var id = entry.GetProperty("id").GetString()!;
            var bytes = PortableTestHarness.FromHex(entry.GetProperty("cbor").GetString()!);
            var expected = entry.GetProperty("expected").GetProperty("category").GetString();
            var fault = Assert.Throws<PortableFaultException>(
                () => PortableCbor.Decode(bytes, PortableLimits.Declared),
                $"Rejected encoding '{id}' decoded instead of failing.");
            Assert.That(fault!.Category.Token(), Is.EqualTo(expected), $"Rejected encoding '{id}'.");
        }
    }

    [Test]
    public void Integer_widths_use_the_shortest_form_that_represents_the_value()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PortableTestHarness.Hex(PortableCbor.Encode(new CborInteger(23))), Is.EqualTo("17"));
            Assert.That(PortableTestHarness.Hex(PortableCbor.Encode(new CborInteger(24))), Is.EqualTo("1818"));
            Assert.That(PortableTestHarness.Hex(PortableCbor.Encode(new CborInteger(256))), Is.EqualTo("190100"));
            Assert.That(PortableTestHarness.Hex(PortableCbor.Encode(new CborInteger(65536))), Is.EqualTo("1a00010000"));
            Assert.That(
                PortableTestHarness.Hex(PortableCbor.Encode(new CborInteger(4294967296))),
                Is.EqualTo("1b0000000100000000"));
            Assert.That(PortableTestHarness.Hex(PortableCbor.Encode(new CborInteger(-1))), Is.EqualTo("20"));
            Assert.That(PortableTestHarness.Hex(PortableCbor.Encode(new CborInteger(-25))), Is.EqualTo("3818"));
        });
    }

    [Test]
    public void A_decimal_normalizes_so_that_one_value_has_exactly_one_encoding()
    {
        var normalized = CborDecimal.Normalize(-3, 12340);
        Assert.Multiple(() =>
        {
            Assert.That(normalized.Exponent, Is.EqualTo(-2));
            Assert.That(normalized.Mantissa, Is.EqualTo(1234));
            Assert.That(CborDecimal.Normalize(-5, 0), Is.EqualTo(new CborDecimal(0, 0)));
        });
    }
}
