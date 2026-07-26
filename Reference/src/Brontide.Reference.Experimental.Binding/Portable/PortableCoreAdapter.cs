using System.Collections.Immutable;
using System.Globalization;
using Brontide.Reference.Core;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>
/// The Reference stack's native adapter between its private Shape model and the neutral positions.
/// </summary>
/// <remarks>
/// The adapter is the boundary that keeps the portable layer fixture-neutral: the Cooling and
/// Catalog experiments express their values in the stack's own <see cref="ShapeValue"/> model and
/// cross into the portable contract here, rather than the portable contract learning either
/// fixture's types.
/// </remarks>
public static class PortableCoreAdapter
{
    public static PortableValue ToPortable(ShapeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        switch (value)
        {
            case UnitShapeValue:
                return PortableUnitValue.Instance;
            case ScalarShapeValue scalar:
                return scalar.Value switch
                {
                    string text => new PortableTextValue(text),
                    bool boolean => new PortableBooleanValue(boolean),
                    decimal number => ToPortableDecimal(number),
                    byte or sbyte or short or ushort or int or uint or long =>
                        new PortableIntegerValue(Convert.ToInt64(scalar.Value, CultureInfo.InvariantCulture)),
                    _ => throw PortableFaultException.InvalidPayload(
                        "unportable-scalar",
                        $"Scalar {value.Reference} has no portable Shape floor member.")
                };
            case OpaqueShapeValue opaque:
                return new PortableBytesValue(opaque.Bytes);
            case SequenceShapeValue sequence:
                return new PortableSequenceValue([.. sequence.Items.Select(ToPortable)]);
            case ChoiceShapeValue choice:
                return new PortableChoiceValue(choice.Alternative, ToPortable(choice.Value));
            case RecordShapeValue record:
            {
                var portable = PortableRecordValue.Empty with
                {
                    Fields = record.Fields.ToImmutableDictionaryOrdinal(field => field.Key, field => ToPortable(field.Value))
                };
                foreach (var fragment in record.Fragments)
                {
                    portable = portable with
                    {
                        Fragments = portable.Fragments.SetItem(
                            ToPortableFragment(fragment.Key),
                            fragment.Value.ToImmutableDictionaryOrdinal(field => field.Key, field => ToPortable(field.Value)))
                    };
                }

                return portable;
            }
            default:
                throw PortableFaultException.InvalidPayload(
                    "unportable-value",
                    $"Value {value.Reference} has no portable Shape floor form.");
        }
    }

    /// <summary>
    /// Projects a portable value back into the stack's model, using the stack's own registry to
    /// resolve each nested position's Shape.
    /// </summary>
    public static ShapeValue ToCore(PortableValue value, ShapeReference reference, ShapeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(registry);
        var definition = registry.Shapes.FirstOrDefault(candidate => candidate.Reference == reference) ??
            throw PortableFaultException.InvalidPayload(
                "unknown-native-shape",
                $"The stack registry does not recognize {reference}.");

        switch (value)
        {
            case PortableUnitValue:
                return ShapeValue.Unit;
            case PortableTextValue text:
                return ShapeValue.Scalar(reference, text.Value);
            case PortableBooleanValue boolean:
                return ShapeValue.Scalar(reference, boolean.Value);
            case PortableIntegerValue integer:
                return ShapeValue.Scalar(reference, integer.Value);
            case PortableDecimalValue number:
                return ShapeValue.Scalar(reference, ToCoreDecimal(number));
            case PortableBytesValue bytes:
                return ShapeValue.Opaque(reference, bytes.Value.Span);
            case PortableSequenceValue sequence:
            {
                var element = definition.ElementShape ?? throw PortableFaultException.InvalidPayload(
                    "native-sequence",
                    $"{reference} is not a sequence in the stack registry.");
                return ShapeValue.Sequence(
                    reference,
                    [.. sequence.Items.Select(item => ToCore(item, element, registry))]);
            }
            case PortableChoiceValue choice:
            {
                if (!definition.Alternatives.TryGetValue(choice.Alternative, out var alternative))
                {
                    throw PortableFaultException.InvalidPayload(
                        "native-alternative",
                        $"{reference} declares no alternative '{choice.Alternative}' in the stack registry.");
                }

                return ShapeValue.Choice(reference, choice.Alternative, ToCore(choice.Value, alternative, registry));
            }
            case PortableRecordValue record:
            {
                var fields = new List<(string Name, ShapeValue Value)>();
                foreach (var field in record.Fields)
                {
                    if (!definition.Fields.TryGetValue(field.Key, out var declaration))
                    {
                        throw PortableFaultException.InvalidPayload(
                            "native-field",
                            $"{reference} declares no field '{field.Key}' in the stack registry.");
                    }

                    fields.Add((field.Key, ToCore(field.Value, declaration.Shape, registry)));
                }

                var fragments = new List<(FragmentReference Fragment, IReadOnlyDictionary<string, ShapeValue> Fields)>();
                foreach (var fragment in record.Fragments)
                {
                    var nativeFragment = ToCoreFragment(fragment.Key);
                    var declaration = registry.Fragments.FirstOrDefault(candidate => candidate.Reference == nativeFragment) ??
                        throw PortableFaultException.InvalidPayload(
                            "native-fragment",
                            $"The stack registry does not recognize Fragment {fragment.Key}.");
                    var fragmentFields = new Dictionary<string, ShapeValue>(StringComparer.Ordinal);
                    foreach (var field in fragment.Value)
                    {
                        if (!declaration.Fields.TryGetValue(field.Key, out var fieldDeclaration))
                        {
                            throw PortableFaultException.InvalidPayload(
                                "native-field",
                                $"Fragment {fragment.Key} declares no field '{field.Key}' in the stack registry.");
                        }

                        fragmentFields[field.Key] = ToCore(field.Value, fieldDeclaration.Shape, registry);
                    }

                    fragments.Add((nativeFragment, fragmentFields));
                }

                return ShapeValue.Record(reference, fields, fragments);
            }
            default:
                throw PortableFaultException.InvalidPayload("unportable-value", "The portable value has no stack form.");
        }
    }

    public static PortableShapeReference ToPortableShape(ShapeReference reference) =>
        PortableShapeReference.Parse(reference.Name.Value, reference.Version);

    public static ShapeReference ToCoreShape(PortableShapeReference reference) =>
        ShapeReference.Parse(reference.Name.Value, reference.Version);

    public static PortableFragmentReference ToPortableFragment(FragmentReference reference) =>
        PortableFragmentReference.Parse(reference.Name.Value, reference.Version);

    public static FragmentReference ToCoreFragment(PortableFragmentReference reference) =>
        FragmentReference.Parse(reference.Name.Value, reference.Version);

    private static PortableDecimalValue ToPortableDecimal(decimal value)
    {
        var parts = decimal.GetBits(value);
        if (parts[2] != 0)
        {
            throw PortableFaultException.InvalidPayload(
                "decimal-range",
                "The portable Decimal mantissa is a signed 64-bit integer.");
        }

        var magnitude = ((ulong)(uint)parts[1] << 32) | (uint)parts[0];
        if (magnitude > long.MaxValue)
        {
            throw PortableFaultException.InvalidPayload(
                "decimal-range",
                "The portable Decimal mantissa is a signed 64-bit integer.");
        }

        var scale = (parts[3] >> 16) & 0xFF;
        var negative = (parts[3] & unchecked((int)0x80000000)) != 0;
        var mantissa = negative ? -(long)magnitude : (long)magnitude;
        var normalized = CborDecimal.Normalize(-scale, mantissa);
        return new PortableDecimalValue(normalized.Exponent, normalized.Mantissa);
    }

    private static decimal ToCoreDecimal(PortableDecimalValue value)
    {
        var result = (decimal)value.Mantissa;
        for (var step = 0; step < Math.Abs(value.Exponent); step++)
        {
            result = value.Exponent < 0 ? result / 10m : result * 10m;
        }

        return result;
    }
}

internal static class OrdinalDictionaryExtensions
{
    public static ImmutableDictionary<string, TValue> ToImmutableDictionaryOrdinal<TSource, TValue>(
        this IEnumerable<TSource> source,
        Func<TSource, string> key,
        Func<TSource, TValue> value) =>
        source.ToImmutableDictionary(key, value, StringComparer.Ordinal);
}
