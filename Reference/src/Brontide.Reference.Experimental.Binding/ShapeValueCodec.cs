using System.Text;
using System.Text.Json;
using Brontide.Reference.Core;

namespace Brontide.Reference.Experimental.Binding;

public static class PortableShapeValueCodec
{
    public static string Encode(ShapeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteValue(writer, value);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static ShapeValue Decode(string json, ShapeReference expectedShape)
    {
        using var document = BoundaryJson.Parse(json);
        return DecodeValue(document.RootElement, expectedShape);
    }

    internal static ShapeValue Decode(JsonElement element, ShapeReference expectedShape) =>
        DecodeValue(element, expectedShape);

    internal static void Write(Utf8JsonWriter writer, ShapeValue value) => WriteValue(writer, value);

    private static void WriteValue(Utf8JsonWriter writer, ShapeValue value)
    {
        writer.WriteStartObject();
        switch (value)
        {
            case UnitShapeValue:
                writer.WriteString("kind", "unit");
                break;
            case ScalarShapeValue scalar when scalar.Value is bool boolean:
                writer.WriteString("kind", "boolean");
                writer.WriteBoolean("value", boolean);
                break;
            case ScalarShapeValue scalar when IsInteger(scalar.Value):
                writer.WriteString("kind", "integer");
                writer.WriteNumber("value", Convert.ToInt64(scalar.Value, System.Globalization.CultureInfo.InvariantCulture));
                break;
            case ScalarShapeValue scalar when scalar.Value is decimal number:
                writer.WriteString("kind", "decimal");
                writer.WriteNumber("value", number);
                break;
            case ScalarShapeValue scalar when scalar.Value is string text:
                writer.WriteString("kind", "text");
                writer.WriteString("value", text);
                break;
            case OpaqueShapeValue opaque:
                writer.WriteString("kind", "bytes");
                writer.WriteBase64String("value", opaque.Bytes.Span);
                break;
            case ChoiceShapeValue choice:
                writer.WriteString("kind", "choice");
                writer.WriteString("case", choice.Alternative);
                writer.WritePropertyName("value");
                WriteValue(writer, choice.Value);
                break;
            case SequenceShapeValue sequence:
                writer.WriteString("kind", "sequence");
                writer.WriteStartArray("items");
                foreach (var item in sequence.Items)
                {
                    WriteValue(writer, item);
                }
                writer.WriteEndArray();
                break;
            case RecordShapeValue record:
                writer.WriteString("kind", "record");
                writer.WriteStartObject("fields");
                foreach (var field in record.Fields.OrderBy(item => item.Key, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(field.Key);
                    WriteValue(writer, field.Value);
                }
                writer.WriteEndObject();
                writer.WriteStartObject("fragments");
                foreach (var fragment in record.Fragments
                             .OrderBy(item => item.Key.Name)
                             .ThenBy(item => item.Key.Version))
                {
                    writer.WritePropertyName($"{fragment.Key.Name.Value}@{fragment.Key.Version}");
                    writer.WriteStartObject();
                    writer.WriteString("kind", "record");
                    writer.WriteStartObject("fields");
                    foreach (var field in fragment.Value.OrderBy(item => item.Key, StringComparer.Ordinal))
                    {
                        writer.WritePropertyName(field.Key);
                        WriteValue(writer, field.Value);
                    }
                    writer.WriteEndObject();
                    writer.WriteStartObject("fragments");
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
                break;
            default:
                throw new BoundaryProtocolException($"ShapeValue kind {value.GetType().Name} is not portable here.");
        }
        writer.WriteEndObject();
    }

    private static ShapeValue DecodeValue(JsonElement element, ShapeReference? expectedShape)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty("kind", out var kindElement) ||
            kindElement.ValueKind != JsonValueKind.String)
        {
            throw new BoundaryProtocolException("A tagged ShapeValue must be an object with a string kind.");
        }

        return kindElement.GetString() switch
        {
            "unit" => ShapeValue.Unit,
            "boolean" => ShapeValue.Boolean(Required(element, "value").GetBoolean()),
            "integer" => ShapeValue.Signed64(Required(element, "value").GetInt64()),
            "text" => ShapeValue.Text(Required(element, "value").GetString() ??
                throw new BoundaryProtocolException("A text ShapeValue cannot be null.")),
            "bytes" => ShapeValue.Opaque(
                expectedShape ?? BuiltInShapes.Bytes,
                Required(element, "value").GetBytesFromBase64()),
            "record" => DecodeRecord(element, expectedShape ??
                throw new BoundaryProtocolException("A record ShapeValue requires an expected Shape.")),
            "sequence" => DecodeSequence(element, expectedShape ??
                throw new BoundaryProtocolException("A sequence ShapeValue requires an expected Shape.")),
            "choice" => DecodeChoice(element, expectedShape ??
                throw new BoundaryProtocolException("A choice ShapeValue requires an expected Shape.")),
            "decimal" => throw new BoundaryProtocolException("Decimal values are not used by the Cooling fixture."),
            var kind => throw new BoundaryProtocolException($"Unknown ShapeValue kind '{kind}'.")
        };
    }

    private static ShapeValue DecodeRecord(JsonElement element, ShapeReference expectedShape)
    {
        var fieldsElement = Required(element, "fields");
        var fragmentsElement = Required(element, "fragments");
        if (fieldsElement.ValueKind != JsonValueKind.Object || fragmentsElement.ValueKind != JsonValueKind.Object)
        {
            throw new BoundaryProtocolException("Record fields and fragments must be objects.");
        }

        var fields = fieldsElement.EnumerateObject()
            .Select(property => (property.Name, DecodeValue(property.Value, null)))
            .ToArray();
        var fragments = new List<(FragmentReference Fragment, IReadOnlyDictionary<string, ShapeValue> Fields)>();
        foreach (var property in fragmentsElement.EnumerateObject())
        {
            var reference = ParseFragment(property.Name);
            var payloadFields = Required(property.Value, "fields");
            var payloadFragments = Required(property.Value, "fragments");
            if (payloadFields.ValueKind != JsonValueKind.Object ||
                payloadFragments.ValueKind != JsonValueKind.Object ||
                payloadFragments.EnumerateObject().Any())
            {
                throw new BoundaryProtocolException("A Fragment payload must be a flat tagged record.");
            }

            fragments.Add((reference, payloadFields.EnumerateObject().ToDictionary(
                field => field.Name,
                field => DecodeValue(field.Value, null),
                StringComparer.Ordinal)));
        }

        return ShapeValue.Record(expectedShape, fields, fragments);
    }

    private static ShapeValue DecodeSequence(JsonElement element, ShapeReference expectedShape)
    {
        var items = Required(element, "items");
        if (items.ValueKind != JsonValueKind.Array)
        {
            throw new BoundaryProtocolException("Sequence items must be an array.");
        }

        return ShapeValue.Sequence(expectedShape, items.EnumerateArray().Select(item => DecodeValue(item, null)).ToArray());
    }

    private static ShapeValue DecodeChoice(JsonElement element, ShapeReference expectedShape)
    {
        var alternative = Required(element, "case").GetString();
        if (string.IsNullOrWhiteSpace(alternative))
        {
            throw new BoundaryProtocolException("A choice case is required.");
        }

        return ShapeValue.Choice(expectedShape, alternative, DecodeValue(Required(element, "value"), null));
    }

    private static JsonElement Required(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value)
            ? value
            : throw new BoundaryProtocolException($"The JSON value is missing '{name}'.");

    private static FragmentReference ParseFragment(string text)
    {
        var separator = text.LastIndexOf('@');
        if (separator < 1 || !int.TryParse(text[(separator + 1)..], out var version) || version < 1)
        {
            throw new BoundaryProtocolException("A Fragment reference must end with @version.");
        }

        return FragmentReference.Parse(text[..separator], version);
    }

    private static bool IsInteger(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong;
}
