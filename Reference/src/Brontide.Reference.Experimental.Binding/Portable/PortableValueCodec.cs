using System.Collections.Immutable;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>The outcome of projecting a presented value onto the version a consumer recognizes.</summary>
public sealed record PortableProjection(PortableValue Value, ImmutableArray<string> MappingObligations);

/// <summary>
/// The schema-guided value codec: the negotiated Shape determines the type of every position, so
/// values carry no kind discriminator on the wire.
/// </summary>
/// <remarks>
/// This is the difference from the retained inline-tagged JSON representation, whose values are
/// self-describing. Because the wire carries no discriminator, a value that does not match its
/// declared Shape is a decode failure, not a coercion.
/// </remarks>
public static class PortableValueCodec
{
    public static CborItem Encode(
        PortableShapeCatalog catalog,
        PortableShapeReference shape,
        PortableValue value)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(value);
        var declaration = catalog.Shape(shape);
        switch (declaration.Kind)
        {
            case PortableShapeKind.Unit:
                return value is PortableUnitValue
                    ? CborNull.Instance
                    : throw Mismatch(shape, "unit");
            case PortableShapeKind.Scalar:
                return EncodeScalar(declaration, shape, value);
            case PortableShapeKind.Sequence:
            {
                var sequence = value as PortableSequenceValue ?? throw Mismatch(shape, "sequence");
                RequireBound(sequence.Items.Length, catalog.Limits.MaxSequenceItems, "sequence");
                return new CborArray(
                    [.. sequence.Items.Select(item => Encode(catalog, declaration.ItemShape!.Value, item))]);
            }
            case PortableShapeKind.Choice:
            {
                var choice = value as PortableChoiceValue ?? throw Mismatch(shape, "choice");
                var alternative = declaration.Alternatives
                    .FirstOrDefault(candidate => string.Equals(candidate.Name, choice.Alternative, StringComparison.Ordinal))
                    ?? throw PortableFaultException.InvalidPayload(
                        "unknown-alternative",
                        $"Shape {shape} declares no alternative '{choice.Alternative}'.");
                return CborArray.Of(
                    new CborText(choice.Alternative),
                    Encode(catalog, alternative.Shape, choice.Value));
            }
            case PortableShapeKind.Record:
                return EncodeRecord(catalog, declaration, shape, value);
            default:
                throw PortableFaultException.InvalidPayload("shape-kind", $"Shape {shape} has an unsupported kind.");
        }
    }

    public static PortableValue Decode(
        PortableShapeCatalog catalog,
        PortableShapeReference shape,
        CborItem item,
        IReadOnlyCollection<PortableFragmentReference>? operationFragments = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(item);
        var declaration = catalog.Shape(shape);
        switch (declaration.Kind)
        {
            case PortableShapeKind.Unit:
                return item is CborNull
                    ? PortableUnitValue.Instance
                    : throw Mismatch(shape, "unit");
            case PortableShapeKind.Scalar:
                return DecodeScalar(declaration, shape, item);
            case PortableShapeKind.Sequence:
            {
                var array = item as CborArray ?? throw Mismatch(shape, "sequence");
                RequireBound(array.Items.Length, catalog.Limits.MaxSequenceItems, "sequence");
                return new PortableSequenceValue(
                    [.. array.Items.Select(element => Decode(catalog, declaration.ItemShape!.Value, element, operationFragments))]);
            }
            case PortableShapeKind.Choice:
            {
                if (item is not CborArray choice || choice.Items.Length != 2 || choice.Items[0] is not CborText name)
                {
                    throw Mismatch(shape, "choice");
                }

                var alternative = declaration.Alternatives
                    .FirstOrDefault(candidate => string.Equals(candidate.Name, name.Value, StringComparison.Ordinal))
                    ?? throw PortableFaultException.InvalidPayload(
                        "unknown-alternative",
                        $"Shape {shape} declares no alternative '{name.Value}'; the envelope kind was recognized, so this is a payload decision.");
                return new PortableChoiceValue(
                    name.Value,
                    Decode(catalog, alternative.Shape, choice.Items[1], operationFragments));
            }
            case PortableShapeKind.Record:
                return DecodeRecord(catalog, declaration, shape, item, operationFragments ?? []);
            default:
                throw PortableFaultException.InvalidPayload("shape-kind", $"Shape {shape} has an unsupported kind.");
        }
    }

    /// <summary>
    /// Validates an in-memory value against its declared Shape without producing bytes.
    /// </summary>
    /// <remarks>
    /// The fixed direct-call realization has no wire, so this is where it applies the same
    /// conformance rules the decoder applies. Encoding and decoding to check a direct call would
    /// manufacture a copy the realization does not make, and copy accounting is a reported fact.
    /// </remarks>
    public static void Validate(
        PortableShapeCatalog catalog,
        PortableShapeReference shape,
        PortableValue value,
        IReadOnlyCollection<PortableFragmentReference>? operationFragments = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(value);
        var declaration = catalog.Shape(shape);
        var fragments = operationFragments ?? [];
        switch (declaration.Kind)
        {
            case PortableShapeKind.Unit:
                if (value is not PortableUnitValue)
                {
                    throw Mismatch(shape, "unit");
                }

                break;
            case PortableShapeKind.Scalar:
                _ = EncodeScalar(declaration, shape, value);
                break;
            case PortableShapeKind.Sequence:
            {
                var sequence = value as PortableSequenceValue ?? throw Mismatch(shape, "sequence");
                RequireBound(sequence.Items.Length, catalog.Limits.MaxSequenceItems, "sequence");
                foreach (var item in sequence.Items)
                {
                    Validate(catalog, declaration.ItemShape!.Value, item, fragments);
                }

                break;
            }
            case PortableShapeKind.Choice:
            {
                var choice = value as PortableChoiceValue ?? throw Mismatch(shape, "choice");
                var alternative = declaration.Alternatives
                    .FirstOrDefault(candidate => string.Equals(candidate.Name, choice.Alternative, StringComparison.Ordinal))
                    ?? throw PortableFaultException.InvalidPayload(
                        "unknown-alternative",
                        $"Shape {shape} declares no alternative '{choice.Alternative}'.");
                Validate(catalog, alternative.Shape, choice.Value, fragments);
                break;
            }
            case PortableShapeKind.Record:
                ValidateRecord(catalog, declaration, shape, value, fragments);
                break;
            default:
                throw PortableFaultException.InvalidPayload("shape-kind", $"Shape {shape} has an unsupported kind.");
        }
    }

    private static void ValidateRecord(
        PortableShapeCatalog catalog,
        PortableShapeDeclaration declaration,
        PortableShapeReference shape,
        PortableValue value,
        IReadOnlyCollection<PortableFragmentReference> operationFragments)
    {
        var record = value as PortableRecordValue ?? throw Mismatch(shape, "record");
        RequireBound(record.Fields.Count, catalog.Limits.MaxRecordFields, "record");
        RequireBound(record.Fragments.Count, catalog.Limits.MaxFragmentsPerRecord, "fragment map");

        var declared = declaration.Fields.ToImmutableDictionary(field => field.Name, StringComparer.Ordinal);
        foreach (var field in record.Fields)
        {
            if (!declared.TryGetValue(field.Key, out var fieldDeclaration))
            {
                throw PortableFaultException.InvalidPayload(
                    "undeclared-field",
                    $"Shape {shape} declares no field '{field.Key}'.");
            }

            Validate(catalog, fieldDeclaration.Shape, field.Value, operationFragments);
        }

        foreach (var field in declaration.Fields.Where(field => field.Required))
        {
            if (!record.Fields.ContainsKey(field.Name))
            {
                throw PortableFaultException.InvalidPayload(
                    "required-field-absent",
                    $"Shape {shape} requires field '{field.Name}'.");
            }
        }

        var permitted = operationFragments.ToImmutableHashSet();
        foreach (var fragment in record.Fragments)
        {
            if (declaration.FragmentPolicy == PortableFragmentPolicy.Closed && !permitted.Contains(fragment.Key))
            {
                throw PortableFaultException.InvalidPayload(
                    "closed-fragment-policy",
                    $"Shape {shape} is closed and rejects Fragment {fragment.Key}, which the negotiated Operation does not declare.");
            }

            if (!catalog.TryFragment(fragment.Key, out var fragmentDeclaration))
            {
                throw PortableFaultException.InvalidPayload(
                    "undeclared-fragment",
                    $"Fragment {fragment.Key} is not declared by the established contract, so its fields have no Shape.");
            }

            if (fragmentDeclaration.HostShape.Name != shape.Name || fragmentDeclaration.HostShape.Version > shape.Version)
            {
                throw PortableFaultException.InvalidPayload(
                    "fragment-host",
                    $"Fragment {fragment.Key} is not attachable to {shape}.");
            }

            var fragmentFields = fragmentDeclaration.Fields.ToImmutableDictionary(field => field.Name, StringComparer.Ordinal);
            foreach (var field in fragment.Value)
            {
                if (!fragmentFields.TryGetValue(field.Key, out var fieldDeclaration))
                {
                    throw PortableFaultException.InvalidPayload(
                        "undeclared-field",
                        $"Fragment {fragment.Key} declares no field '{field.Key}'.");
                }

                Validate(catalog, fieldDeclaration.Shape, field.Value, operationFragments);
            }

            foreach (var field in fragmentDeclaration.Fields.Where(field => field.Required))
            {
                if (!fragment.Value.ContainsKey(field.Name))
                {
                    throw PortableFaultException.InvalidPayload(
                        "required-field-absent",
                        $"Fragment {fragment.Key} requires field '{field.Name}'.");
                }
            }
        }
    }

    /// <summary>
    /// Projects a presented value onto the Shape version the consumer recognizes.
    /// </summary>
    /// <remarks>
    /// Projection discards unrecognized additive structure. It is therefore never applied to an
    /// authority or control position: discarding narrowing semantics from an authority value would
    /// widen authority.
    /// </remarks>
    public static PortableProjection Project(
        PortableShapeCatalog catalog,
        PortableValue value,
        PortableShapeReference presented,
        PortableShapeReference accepted,
        IReadOnlyCollection<PortableFragmentReference> operationFragments)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(operationFragments);

        if (presented == accepted)
        {
            return new PortableProjection(value, []);
        }

        if (!catalog.IsAdditiveOver(presented, accepted))
        {
            throw PortableFaultException.InvalidPayload(
                "non-additive-projection",
                $"Shape {presented} does not differ from {accepted} additively, so it cannot be projected.");
        }

        var acceptedShape = catalog.Shape(accepted);
        if (acceptedShape.Kind != PortableShapeKind.Record || value is not PortableRecordValue record)
        {
            return new PortableProjection(value, [$"projected:{presented}->{accepted}"]);
        }

        var declaredNames = acceptedShape.Fields.Select(field => field.Name).ToImmutableHashSet(StringComparer.Ordinal);
        var retained = record.Fields
            .Where(field => declaredNames.Contains(field.Key))
            .ToImmutableDictionary(field => field.Key, field => field.Value, StringComparer.Ordinal);
        var dropped = record.Fields.Keys
            .Where(name => !declaredNames.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToImmutableArray();

        var obligations = ImmutableArray.CreateBuilder<string>();
        obligations.Add($"projected:{presented}->{accepted}");
        foreach (var name in dropped)
        {
            obligations.Add($"field-projected:{name}");
        }

        return new PortableProjection(record with { Fields = retained }, obligations.ToImmutable());
    }

    // ---------------------------------------------------------------------

    private static CborItem EncodeScalar(
        PortableShapeDeclaration declaration,
        PortableShapeReference shape,
        PortableValue value) => (declaration.Scalar, value) switch
    {
        (PortableScalarKind.Text, PortableTextValue text) => new CborText(text.Value),
        (PortableScalarKind.Boolean, PortableBooleanValue boolean) => new CborBoolean(boolean.Value),
        (PortableScalarKind.Signed64, PortableIntegerValue integer) => new CborInteger(integer.Value),
        (PortableScalarKind.Decimal, PortableDecimalValue number) => CborDecimal.Normalize(number.Exponent, number.Mantissa),
        (PortableScalarKind.Bytes, PortableBytesValue bytes) => new CborBytes(bytes.Value),
        _ => throw Mismatch(shape, declaration.Scalar?.Token() ?? "scalar")
    };

    private static PortableValue DecodeScalar(
        PortableShapeDeclaration declaration,
        PortableShapeReference shape,
        CborItem item) => (declaration.Scalar, item) switch
    {
        (PortableScalarKind.Text, CborText text) => new PortableTextValue(text.Value),
        (PortableScalarKind.Boolean, CborBoolean boolean) => new PortableBooleanValue(boolean.Value),
        (PortableScalarKind.Signed64, CborInteger integer) => new PortableIntegerValue(integer.Value),
        (PortableScalarKind.Decimal, CborDecimal number) => new PortableDecimalValue(number.Exponent, number.Mantissa),
        (PortableScalarKind.Bytes, CborBytes bytes) => new PortableBytesValue(bytes.Value),
        _ => throw Mismatch(shape, declaration.Scalar?.Token() ?? "scalar")
    };

    private static CborItem EncodeRecord(
        PortableShapeCatalog catalog,
        PortableShapeDeclaration declaration,
        PortableShapeReference shape,
        PortableValue value)
    {
        var record = value as PortableRecordValue ?? throw Mismatch(shape, "record");
        RequireBound(record.Fields.Count, catalog.Limits.MaxRecordFields, "record");
        RequireBound(record.Fragments.Count, catalog.Limits.MaxFragmentsPerRecord, "fragment map");

        var fields = new List<(string, CborItem)>();
        foreach (var field in declaration.Fields)
        {
            if (!record.Fields.TryGetValue(field.Name, out var fieldValue))
            {
                if (field.Required)
                {
                    throw PortableFaultException.InvalidPayload(
                        "required-field-absent",
                        $"Shape {shape} requires field '{field.Name}'.");
                }

                // An optional field carrying no value is omitted rather than encoded as null;
                // omission is what makes additive projection possible.
                continue;
            }

            fields.Add((field.Name, Encode(catalog, field.Shape, fieldValue)));
        }

        var declaredNames = declaration.Fields.Select(field => field.Name).ToImmutableHashSet(StringComparer.Ordinal);
        var undeclared = record.Fields.Keys.FirstOrDefault(name => !declaredNames.Contains(name));
        if (undeclared is not null)
        {
            throw PortableFaultException.InvalidPayload(
                "undeclared-field",
                $"Shape {shape} declares no field '{undeclared}'.");
        }

        var fragments = new List<(string, CborItem)>();
        foreach (var fragment in record.Fragments)
        {
            var fragmentDeclaration = catalog.Fragment(fragment.Key);
            var fragmentFields = new List<(string, CborItem)>();
            foreach (var field in fragmentDeclaration.Fields)
            {
                if (!fragment.Value.TryGetValue(field.Name, out var fieldValue))
                {
                    if (field.Required)
                    {
                        throw PortableFaultException.InvalidPayload(
                            "required-field-absent",
                            $"Fragment {fragment.Key} requires field '{field.Name}'.");
                    }

                    continue;
                }

                fragmentFields.Add((field.Name, Encode(catalog, field.Shape, fieldValue)));
            }

            fragments.Add((fragment.Key.CanonicalText, CborMap.Of(fragmentFields)));
        }

        return CborArray.Of(CborMap.Of(fields), CborMap.Of(fragments));
    }

    private static PortableValue DecodeRecord(
        PortableShapeCatalog catalog,
        PortableShapeDeclaration declaration,
        PortableShapeReference shape,
        CborItem item,
        IReadOnlyCollection<PortableFragmentReference> operationFragments)
    {
        if (item is not CborArray array || array.Items.Length != 2 ||
            array.Items[0] is not CborMap fieldMap || array.Items[1] is not CborMap fragmentMap)
        {
            throw Mismatch(shape, "record");
        }

        RequireBound(fieldMap.Entries.Length, catalog.Limits.MaxRecordFields, "record");
        RequireBound(fragmentMap.Entries.Length, catalog.Limits.MaxFragmentsPerRecord, "fragment map");

        var declared = declaration.Fields.ToImmutableDictionary(field => field.Name, StringComparer.Ordinal);
        var fields = ImmutableDictionary.CreateBuilder<string, PortableValue>(StringComparer.Ordinal);
        foreach (var entry in fieldMap.Entries)
        {
            if (!declared.TryGetValue(entry.Key, out var field))
            {
                throw PortableFaultException.InvalidPayload(
                    "undeclared-field",
                    $"Shape {shape} declares no field '{entry.Key}'.");
            }

            fields[entry.Key] = Decode(catalog, field.Shape, entry.Value, operationFragments);
        }

        foreach (var field in declaration.Fields.Where(field => field.Required))
        {
            if (!fields.ContainsKey(field.Name))
            {
                throw PortableFaultException.InvalidPayload(
                    "required-field-absent",
                    $"Shape {shape} requires field '{field.Name}'.");
            }
        }

        var permitted = operationFragments.ToImmutableHashSet();
        var fragments = ImmutableDictionary.CreateBuilder<PortableFragmentReference, ImmutableDictionary<string, PortableValue>>();
        foreach (var entry in fragmentMap.Entries)
        {
            var reference = PortableFragmentReference.ParseText(entry.Key);
            if (declaration.FragmentPolicy == PortableFragmentPolicy.Closed && !permitted.Contains(reference))
            {
                throw PortableFaultException.InvalidPayload(
                    "closed-fragment-policy",
                    $"Shape {shape} is closed and rejects Fragment {reference}, which the negotiated Operation does not declare.");
            }

            if (!catalog.TryFragment(reference, out var fragmentDeclaration))
            {
                throw PortableFaultException.InvalidPayload(
                    "undeclared-fragment",
                    $"Fragment {reference} is not declared by the established contract, so its fields have no Shape.");
            }

            if (fragmentDeclaration.HostShape.Name != shape.Name || fragmentDeclaration.HostShape.Version > shape.Version)
            {
                throw PortableFaultException.InvalidPayload(
                    "fragment-host",
                    $"Fragment {reference} is not attachable to {shape}.");
            }

            fragments[reference] = DecodeFragmentFields(
                catalog,
                fragmentDeclaration,
                CborAccess.RequireMap(entry.Value, "fragment"),
                operationFragments);
        }

        return new PortableRecordValue(fields.ToImmutable(), fragments.ToImmutable());
    }

    private static ImmutableDictionary<string, PortableValue> DecodeFragmentFields(
        PortableShapeCatalog catalog,
        PortableFragmentDeclaration fragment,
        CborMap map,
        IReadOnlyCollection<PortableFragmentReference> operationFragments)
    {
        var declared = fragment.Fields.ToImmutableDictionary(field => field.Name, StringComparer.Ordinal);
        var fields = ImmutableDictionary.CreateBuilder<string, PortableValue>(StringComparer.Ordinal);
        foreach (var entry in map.Entries)
        {
            if (!declared.TryGetValue(entry.Key, out var field))
            {
                throw PortableFaultException.InvalidPayload(
                    "undeclared-field",
                    $"Fragment {fragment.Reference} declares no field '{entry.Key}'.");
            }

            fields[entry.Key] = Decode(catalog, field.Shape, entry.Value, operationFragments);
        }

        foreach (var field in fragment.Fields.Where(field => field.Required))
        {
            if (!fields.ContainsKey(field.Name))
            {
                throw PortableFaultException.InvalidPayload(
                    "required-field-absent",
                    $"Fragment {fragment.Reference} requires field '{field.Name}'.");
            }
        }

        return fields.ToImmutable();
    }

    private static void RequireBound(int count, int bound, string what)
    {
        if (count > bound)
        {
            throw PortableFaultException.LimitExceeded(
                $"{what}-bound",
                $"A {what} of {count} exceeds the declared bound of {bound}.");
        }
    }

    private static PortableFaultException Mismatch(PortableShapeReference shape, string expected) =>
        PortableFaultException.InvalidPayload(
            "shape-mismatch",
            $"Shape {shape} requires a {expected} value.");
}
