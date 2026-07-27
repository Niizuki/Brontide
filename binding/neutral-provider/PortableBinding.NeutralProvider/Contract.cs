using System.Text.Json;

namespace PortableBinding.NeutralProvider;

/// <summary>A refusal carrying the portable category and a local, non-normative diagnostic code.</summary>
internal sealed class PortableFault(string category, string localCode, string message)
    : Exception(message)
{
    public string Category { get; } = category;

    public string LocalCode { get; } = localCode;
}

/// <summary>One Operation the served contract declares.</summary>
internal sealed record OperationDeclaration(
    string Name,
    int Version,
    (string Name, int Version) InputShape,
    (string Name, int Version) ResultShape,
    (string Name, int Version) DetailShape,
    IReadOnlyList<(string Name, int Version)> RequiredFragments,
    IReadOnlyList<string> ResourceFlavors);

/// <summary>One declared Shape, in the only detail this endpoint needs to validate a value.</summary>
internal sealed record ShapeDeclaration(
    string Name,
    int Version,
    string Kind,
    string? FragmentPolicy,
    IReadOnlyList<(string Name, (string Name, int Version) Shape, bool Required)> Fields,
    (string Name, int Version)? ItemShape);

/// <summary>
/// The contract this endpoint serves, read from the checked-in neutral declaration.
/// </summary>
/// <remarks>
/// The declaration is transcoded to CBOR rather than restated in source. The neutral JSON and the
/// wire encoding are structurally the same document — the same field names in the same nesting —
/// so a faithful transcode is the whole of "encode the contract". That correspondence is what makes
/// this endpoint evidence: it implements the contract from the published form, having imported no
/// part of either stack's model.
/// </remarks>
internal sealed class ServedContract
{
    private readonly JsonDocument _document;

    private ServedContract(JsonDocument document)
    {
        _document = document;
        var root = document.RootElement;

        Operations =
        [
            .. root.GetProperty("operations").EnumerateArray().Select(operation =>
            {
                var reference = ReadReference(operation.GetProperty("reference"));
                return new OperationDeclaration(
                    reference.Name,
                    reference.Version,
                    ReadReference(operation.GetProperty("inputShape")),
                    ReadReference(operation.GetProperty("resultShape")),
                    ReadReference(operation.GetProperty("detailShape")),
                    [.. operation.GetProperty("requiredFragments").EnumerateArray().Select(ReadReference)],
                    [.. operation.GetProperty("resourceFlavors").EnumerateArray().Select(flavor => flavor.GetString()!)]);
            })
        ];

        Shapes =
        [
            .. root.GetProperty("shapes").EnumerateArray().Select(shape =>
            {
                var reference = ReadReference(shape.GetProperty("reference"));
                var kind = shape.GetProperty("kind").GetString()!;
                return new ShapeDeclaration(
                    reference.Name,
                    reference.Version,
                    kind,
                    shape.TryGetProperty("fragmentPolicy", out var policy) ? policy.GetString() : null,
                    shape.TryGetProperty("fields", out var fields)
                        ?
                        [
                            .. fields.EnumerateArray().Select(field => (
                                field.GetProperty("name").GetString()!,
                                ReadReference(field.GetProperty("shape")),
                                field.GetProperty("required").GetBoolean()))
                        ]
                        : [],
                    shape.TryGetProperty("itemShape", out var itemShape) ? ReadReference(itemShape) : null);
            })
        ];

        var representation = root.GetProperty("representation");
        ResourceFlavors = [.. representation.GetProperty("resourceFlavors").EnumerateArray().Select(flavor => flavor.GetString()!)];
        AcceptedResourceHandles =
        [
            .. representation.GetProperty("acceptedResourceHandles").EnumerateArray().Select(handle => handle.GetString()!)
        ];
        TrustBoundaryCrossed = root.GetProperty("authority").GetProperty("trustBoundaryCrossed").GetBoolean();
        MaxResourceBytes = root.GetProperty("limits").GetProperty("maxResourceBytes").GetInt32();
        _annotationFields =
        [
            .. root.GetProperty("annotationFields").EnumerateArray().Select(field => field.GetString()!)
        ];
    }

    private readonly IReadOnlyList<string> _annotationFields;

    public IReadOnlyList<OperationDeclaration> Operations { get; }

    public IReadOnlyList<ShapeDeclaration> Shapes { get; }

    public IReadOnlyList<string> ResourceFlavors { get; }

    public IReadOnlyList<string> AcceptedResourceHandles { get; }

    public bool TrustBoundaryCrossed { get; }

    public int MaxResourceBytes { get; }

    public static ServedContract Load(string path) =>
        new(JsonDocument.Parse(File.ReadAllText(path)));

    /// <summary>The contract document, in the wire form the neutral schemas declare.</summary>
    public Cbor Encode() => Transcode(_document.RootElement, _annotationFields, root: true);

    public OperationDeclaration Operation(string name, int version) =>
        Operations.FirstOrDefault(operation => operation.Name == name && operation.Version == version)
        ?? throw new PortableFault(
            "unsupported-operation",
            "operation-undeclared",
            $"The established contract declares no Operation '{name}@{version}'.");

    public ShapeDeclaration Shape((string Name, int Version) reference) =>
        Shapes.FirstOrDefault(shape => shape.Name == reference.Name && shape.Version == reference.Version)
        ?? throw new PortableFault(
            "unsupported-contract",
            "shape-undeclared",
            $"The established contract declares no Shape '{reference.Name}@{reference.Version}'.");

    private static (string Name, int Version) ReadReference(JsonElement element) =>
        (element.GetProperty("name").GetString()!, element.GetProperty("version").GetInt32());

    /// <summary>
    /// Transcodes the declaration to CBOR. The two forms carry the same names and nesting, so the
    /// only decisions here are which members belong to the contract document and how JSON scalars
    /// map onto the declared CBOR item types.
    /// </summary>
    /// <remarks>
    /// The fixture files carry documentation alongside the contract — why a Shape version exists,
    /// what an encoding-edge Shape is for. <c>component-contract.json</c> declares exactly which
    /// fields a contract document has and rejects unknown ones, so those members must be dropped.
    /// Which names are documentation is read from the file's own <c>annotationFields</c> rather than
    /// guessed here, so a future annotation has to declare itself instead of silently becoming a
    /// malformed contract.
    /// </remarks>
    private static Cbor Transcode(JsonElement element, IReadOnlyList<string> annotations, bool root = false)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var entries = new List<(string, Cbor)>();
                foreach (var member in element.EnumerateObject())
                {
                    // These describe the artifact, not the contract it declares.
                    if (root && member.Name is "schemaVersion" or "fixture" or "annotationFields" or "annotationFieldsNote")
                    {
                        continue;
                    }

                    if (annotations.Contains(member.Name, StringComparer.Ordinal))
                    {
                        continue;
                    }

                    entries.Add((member.Name, Transcode(member.Value, annotations)));
                }

                return Cbor.Map(entries);
            }
            case JsonValueKind.Array:
                return Cbor.Array(element.EnumerateArray().Select(item => Transcode(item, annotations)));
            case JsonValueKind.String:
                return Cbor.Text(element.GetString()!);
            case JsonValueKind.Number:
                return Cbor.Integer(element.GetInt64());
            case JsonValueKind.True:
            case JsonValueKind.False:
                return Cbor.Boolean(element.GetBoolean());
            case JsonValueKind.Null:
                return Cbor.Null;
            default:
                throw new PortableFault(
                    "malformed-message",
                    "contract-transcode",
                    $"The declaration carries an unsupported JSON value of kind '{element.ValueKind}'.");
        }
    }
}
