using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Brontide.Reference.Core;

namespace Brontide.Reference.Experimental.Binding;

public readonly record struct BindingRequestId(Guid Value)
{
    public static BindingRequestId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct BindingExecutionId(Guid Value)
{
    public static BindingExecutionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct BindingOccurrenceId(Guid Value)
{
    public static BindingOccurrenceId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public sealed record WireReference
{
    [JsonConstructor]
    public WireReference(string name, int version)
    {
        Name = name;
        Version = version;
    }

    public WireReference(ShapeReference reference) : this(reference.Name.Value, reference.Version) { }
    public WireReference(FragmentReference reference) : this(reference.Name.Value, reference.Version) { }
    public WireReference(OperationReference reference, int version = 1) : this(reference.Name.Value, version) { }

    public string Name { get; }
    public int Version { get; }

    public void Validate(string description)
    {
        if (!CanonicalName.TryParse(Name, out _) || Version < 1)
        {
            throw new BoundaryProtocolException($"{description} is invalid.");
        }
    }

    public override string ToString() => $"{Name}@{Version}";
}

public sealed record ManifestField(string Name, WireReference Shape, bool Required);
public sealed record ManifestShape(
    WireReference Reference,
    string Kind,
    string FragmentPolicy,
    ImmutableArray<ManifestField> Fields);
public sealed record ManifestFragment(
    WireReference Reference,
    WireReference HostShape,
    ImmutableArray<ManifestField> Fields);
public sealed record AuthorityRequirement(bool HostDecisionRequired, string ConstraintPolicy);
public sealed record ManifestOperation(
    WireReference Reference,
    WireReference InputShape,
    WireReference OutputShape,
    ImmutableArray<WireReference> RequiredFragments,
    AuthorityRequirement Authority);
public sealed record ManifestDependency(
    string Kind,
    WireReference Reference,
    string Strength,
    bool ProviderSpecific);
public sealed record BindingDeclaration(
    ImmutableArray<string> Representations,
    ImmutableArray<string> CrossedBoundaries,
    ImmutableArray<string> Limitations);

public sealed record PortableManifest(
    int ProtocolVersion,
    WireReference Component,
    WireReference Provider,
    ImmutableArray<ManifestOperation> Operations,
    ImmutableArray<ManifestShape> Shapes,
    ImmutableArray<ManifestFragment> Fragments,
    ImmutableArray<ManifestDependency> Dependencies,
    BindingDeclaration Binding)
{
    public void Validate()
    {
        if (ProtocolVersion != PortableProtocol.Version)
        {
            throw new BoundaryProtocolException($"Unsupported protocol version {ProtocolVersion}.");
        }

        Component.Validate("The component reference");
        Provider.Validate("The provider reference");
        if (Operations.IsDefault || Shapes.IsDefault || Fragments.IsDefault || Dependencies.IsDefault ||
            Binding is null)
        {
            throw new BoundaryProtocolException("The manifest is incomplete.");
        }

        foreach (var operation in Operations)
        {
            operation.Reference.Validate("An Operation reference");
            operation.InputShape.Validate("An input Shape reference");
            operation.OutputShape.Validate("An output Shape reference");
            foreach (var fragment in operation.RequiredFragments)
            {
                fragment.Validate("A required Fragment reference");
            }

            if (!operation.Authority.HostDecisionRequired || operation.Authority.ConstraintPolicy != "fail-closed")
            {
                throw new BoundaryProtocolException("The test contract requires a fail-closed host authority decision.");
            }
        }

        foreach (var shape in Shapes)
        {
            shape.Reference.Validate("A Shape reference");
            if (shape.Kind != "record" || shape.FragmentPolicy is not ("open" or "closed"))
            {
                throw new BoundaryProtocolException("The Cooling fixture supports declared record Shapes only.");
            }

            ValidateFields(shape.Fields);
        }

        foreach (var fragment in Fragments)
        {
            fragment.Reference.Validate("A Fragment reference");
            fragment.HostShape.Validate("A Fragment host Shape reference");
            ValidateFields(fragment.Fields);
        }

        foreach (var dependency in Dependencies)
        {
            dependency.Reference.Validate("A dependency reference");
            if (dependency.Strength is not ("required" or "preferred" or "opposed" or "optional"))
            {
                throw new BoundaryProtocolException("A manifest dependency strength is unknown.");
            }
        }

        if (!Binding.Representations.Contains("inline-tagged-json", StringComparer.Ordinal) ||
            !Binding.CrossedBoundaries.Contains("process", StringComparer.Ordinal) ||
            !Binding.Limitations.Contains("no-capability-transfer", StringComparer.Ordinal))
        {
            throw new BoundaryProtocolException("The binding declaration is incompatible with the test protocol.");
        }
    }

    private static void ValidateFields(ImmutableArray<ManifestField> fields)
    {
        if (fields.IsDefault || fields.Any(field => string.IsNullOrWhiteSpace(field.Name)) ||
            fields.Select(field => field.Name).Distinct(StringComparer.Ordinal).Count() != fields.Length)
        {
            throw new BoundaryProtocolException("Manifest fields must be named uniquely.");
        }

        foreach (var field in fields)
        {
            field.Shape.Validate("A field Shape reference");
        }
    }
}

public static class ManifestCodec
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static string Encode(PortableManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        manifest.Validate();
        return JsonSerializer.Serialize(manifest, Options);
    }

    public static PortableManifest Decode(string json)
    {
        using var document = BoundaryJson.Parse(json);
        var manifest = document.RootElement.Deserialize<PortableManifest>(Options) ??
            throw new BoundaryProtocolException("The manifest is null.");
        manifest.Validate();
        return manifest;
    }

    public static void NegotiateExact(PortableManifest required, PortableManifest offered)
    {
        required.Validate();
        offered.Validate();
        if (required.ProtocolVersion != offered.ProtocolVersion)
        {
            throw new BoundaryNegotiationException("The boundary protocol versions are incompatible.");
        }

        if (required.Component != offered.Component)
        {
            throw new BoundaryNegotiationException(
                $"Required component {required.Component} was not offered as the exact contract.");
        }

        RequireReferences(
            "Operation",
            required.Operations.Select(item => item.Reference),
            offered.Operations.Select(item => item.Reference));
        RequireReferences(
            "Shape",
            required.Shapes.Select(item => item.Reference),
            offered.Shapes.Select(item => item.Reference));
        RequireReferences(
            "Fragment",
            required.Fragments.Select(item => item.Reference),
            offered.Fragments.Select(item => item.Reference));

        foreach (var operation in required.Operations)
        {
            var candidate = offered.Operations.Single(item => item.Reference == operation.Reference);
            if (candidate.InputShape != operation.InputShape ||
                candidate.OutputShape != operation.OutputShape ||
                !candidate.RequiredFragments.SequenceEqual(operation.RequiredFragments))
            {
                throw new BoundaryNegotiationException(
                    $"Operation {operation.Reference} has incompatible Shape or Fragment declarations.");
            }
        }

        foreach (var dependency in required.Dependencies.Where(item => item.Strength == "required"))
        {
            if (!offered.Dependencies.Any(candidate =>
                    candidate.Kind == dependency.Kind &&
                    candidate.Reference == dependency.Reference &&
                    candidate.Strength == "required" &&
                    candidate.ProviderSpecific == dependency.ProviderSpecific))
            {
                throw new BoundaryNegotiationException(
                    $"Required dependency {dependency.Reference} was not offered.");
            }
        }
    }

    private static void RequireReferences(
        string kind,
        IEnumerable<WireReference> required,
        IEnumerable<WireReference> offered)
    {
        var offeredSet = offered.ToHashSet();
        var missing = required.FirstOrDefault(reference => !offeredSet.Contains(reference));
        if (missing is not null)
        {
            throw new BoundaryNegotiationException($"Required {kind} {missing} was not negotiated.");
        }
    }
}

internal static class BoundaryJson
{
    private static readonly HashSet<string> ForbiddenProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "$type", "typeName", "exception", "stackTrace", "innerException", "targetSite"
    };

    public static JsonDocument Parse(string json)
    {
        try
        {
            var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 64
            });
            ValidateElement(document.RootElement);
            return document;
        }
        catch (BoundaryProtocolException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            throw new BoundaryProtocolException($"The boundary JSON is invalid: {exception.Message}");
        }
    }

    private static void ValidateElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new BoundaryProtocolException($"Duplicate protected field '{property.Name}' is not permitted.");
                }

                if (ForbiddenProperties.Contains(property.Name))
                {
                    throw new BoundaryProtocolException(
                        $"Private CLR type or exception metadata field '{property.Name}' is not permitted.");
                }

                ValidateElement(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                ValidateElement(item);
            }
        }
    }
}

public sealed class BoundaryProtocolException(string message) : InvalidOperationException(message);
public sealed class BoundaryNegotiationException(string message) : InvalidOperationException(message);
