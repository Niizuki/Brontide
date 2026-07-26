using System.Collections.Immutable;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>The Shape floor's built-in references, which no contract needs to redeclare.</summary>
public static class PortableBuiltInShapes
{
    public static PortableShapeReference Text { get; } = PortableShapeReference.Parse("Text", 1);
    public static PortableShapeReference Boolean { get; } = PortableShapeReference.Parse("Boolean", 1);
    public static PortableShapeReference Signed64 { get; } = PortableShapeReference.Parse("Integer.Signed64", 1);
    public static PortableShapeReference Decimal { get; } = PortableShapeReference.Parse("Decimal", 1);
    public static PortableShapeReference Bytes { get; } = PortableShapeReference.Parse("Bytes", 1);
    public static PortableShapeReference Unit { get; } = PortableShapeReference.Parse("Unit", 1);

    public static ImmutableArray<PortableShapeDeclaration> Declarations { get; } =
    [
        PortableShapeDeclaration.ScalarShape(Text, PortableScalarKind.Text),
        PortableShapeDeclaration.ScalarShape(Boolean, PortableScalarKind.Boolean),
        PortableShapeDeclaration.ScalarShape(Signed64, PortableScalarKind.Signed64),
        PortableShapeDeclaration.ScalarShape(Decimal, PortableScalarKind.Decimal),
        PortableShapeDeclaration.ScalarShape(Bytes, PortableScalarKind.Bytes),
        PortableShapeDeclaration.Unit(Unit)
    ];
}

/// <summary>
/// The Shape graph of one established contract, plus the projection rules that decide when a
/// version difference is additive.
/// </summary>
/// <remarks>
/// Unresolvable references fail as <c>unsupported-contract</c> rather than as a decode error,
/// because an unknown Shape, Fragment, or dependency is a contract fact, not a byte-level one.
/// </remarks>
public sealed class PortableShapeCatalog
{
    private readonly ImmutableDictionary<PortableShapeReference, PortableShapeDeclaration> _shapes;
    private readonly ImmutableDictionary<PortableFragmentReference, PortableFragmentDeclaration> _fragments;

    private PortableShapeCatalog(
        ImmutableDictionary<PortableShapeReference, PortableShapeDeclaration> shapes,
        ImmutableDictionary<PortableFragmentReference, PortableFragmentDeclaration> fragments,
        PortableLimits limits)
    {
        _shapes = shapes;
        _fragments = fragments;
        Limits = limits;
    }

    public PortableLimits Limits { get; }

    public static PortableShapeCatalog FromContract(PortableContractDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var shapes = ImmutableDictionary.CreateBuilder<PortableShapeReference, PortableShapeDeclaration>();
        foreach (var builtIn in PortableBuiltInShapes.Declarations)
        {
            shapes[builtIn.Reference] = builtIn;
        }

        foreach (var shape in document.Shapes)
        {
            if (shapes.ContainsKey(shape.Reference))
            {
                throw PortableFaultException.Malformed(
                    "duplicate-shape",
                    $"Shape {shape.Reference} is declared more than once.");
            }

            RequireUniqueFieldNames(shape.Fields, $"Shape {shape.Reference}");
            shapes[shape.Reference] = shape;
        }

        var fragments = ImmutableDictionary.CreateBuilder<PortableFragmentReference, PortableFragmentDeclaration>();
        foreach (var fragment in document.Fragments)
        {
            if (fragments.ContainsKey(fragment.Reference))
            {
                throw PortableFaultException.Malformed(
                    "duplicate-fragment",
                    $"Fragment {fragment.Reference} is declared more than once.");
            }

            RequireUniqueFieldNames(fragment.Fields, $"Fragment {fragment.Reference}");
            fragments[fragment.Reference] = fragment;
        }

        var catalog = new PortableShapeCatalog(shapes.ToImmutable(), fragments.ToImmutable(), document.Limits);
        catalog.RequireResolvable(document);
        return catalog;
    }

    public PortableShapeDeclaration Shape(PortableShapeReference reference) =>
        _shapes.TryGetValue(reference, out var declaration)
            ? declaration
            : throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "unknown-shape",
                $"Shape {reference} is not part of the established contract.");

    public bool TryShape(PortableShapeReference reference, out PortableShapeDeclaration declaration) =>
        _shapes.TryGetValue(reference, out declaration!);

    public PortableFragmentDeclaration Fragment(PortableFragmentReference reference) =>
        _fragments.TryGetValue(reference, out var declaration)
            ? declaration
            : throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "unknown-fragment",
                $"Fragment {reference} is not part of the established contract.");

    public bool TryFragment(PortableFragmentReference reference, out PortableFragmentDeclaration declaration) =>
        _fragments.TryGetValue(reference, out declaration!);

    /// <summary>
    /// Decides whether the later Shape version differs from the earlier one only additively: new
    /// optional fields. A new required field, a removed field, a changed field Shape, or a changed
    /// cardinality is not additive.
    /// </summary>
    public bool IsAdditiveOver(PortableShapeReference later, PortableShapeReference earlier)
    {
        if (later.Name != earlier.Name || later.Version < earlier.Version)
        {
            return false;
        }

        if (later == earlier)
        {
            return true;
        }

        if (!TryShape(later, out var laterShape) || !TryShape(earlier, out var earlierShape))
        {
            return false;
        }

        if (laterShape.Kind != earlierShape.Kind ||
            laterShape.FragmentPolicy != earlierShape.FragmentPolicy ||
            laterShape.ItemShape != earlierShape.ItemShape ||
            laterShape.Scalar != earlierShape.Scalar ||
            !laterShape.Alternatives.SequenceEqual(earlierShape.Alternatives))
        {
            return false;
        }

        var laterFields = laterShape.Fields.ToImmutableDictionary(field => field.Name, StringComparer.Ordinal);
        foreach (var field in earlierShape.Fields)
        {
            if (!laterFields.TryGetValue(field.Name, out var candidate) || candidate != field)
            {
                return false;
            }
        }

        var earlierNames = earlierShape.Fields.Select(field => field.Name).ToImmutableHashSet(StringComparer.Ordinal);
        return laterShape.Fields.All(field => earlierNames.Contains(field.Name) || !field.Required);
    }

    private void RequireResolvable(PortableContractDocument document)
    {
        foreach (var shape in document.Shapes)
        {
            foreach (var field in shape.Fields)
            {
                _ = Shape(field.Shape);
            }

            foreach (var alternative in shape.Alternatives)
            {
                _ = Shape(alternative.Shape);
            }

            if (shape.ItemShape is { } itemShape)
            {
                _ = Shape(itemShape);
            }
        }

        foreach (var fragment in document.Fragments)
        {
            _ = Shape(fragment.HostShape);
            foreach (var field in fragment.Fields)
            {
                _ = Shape(field.Shape);
            }
        }

        foreach (var operation in document.Operations)
        {
            _ = Shape(operation.InputShape);
            _ = Shape(operation.ResultShape);
            _ = Shape(operation.DetailShape);
            foreach (var fragment in operation.RequiredFragments)
            {
                _ = Fragment(fragment);
            }
        }
    }

    private static void RequireUniqueFieldNames(ImmutableArray<PortableFieldDeclaration> fields, string owner)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            _ = PortableMemberToken.Parse(field.Name);
            if (!seen.Add(field.Name))
            {
                throw PortableFaultException.Malformed(
                    "duplicate-field",
                    $"{owner} declares field '{field.Name}' more than once.");
            }
        }
    }
}
