using System.Collections.Immutable;

namespace Brontide.Reference.Experimental.Binding.Portable;

public enum PortableDependencyKind
{
    Operation,
    Profile,
    Binding,
    ResourceFlavor,
    Feature
}

public enum PortableRequirementStrength
{
    Required,
    Preferred,
    Optional,
    Opposed
}

public enum PortableShapeKind
{
    Record,
    Sequence,
    Choice,
    Scalar,
    Unit
}

public enum PortableFragmentPolicy
{
    Open,
    Closed
}

public enum PortableScalarKind
{
    Text,
    Boolean,
    Signed64,
    Decimal,
    Bytes
}

public enum PortableAuthorityMode
{
    LocalDomainEvaluated,
    CrossTrustNoCapabilityTransfer
}

public enum PortableRealization
{
    FixedDirectCall,
    NegotiatedProcess
}

/// <summary>Maps the neutral tokens of each contract enumeration onto the local enumerations.</summary>
public static class PortableEnums
{
    public static string Token(this PortableDependencyKind kind) => kind switch
    {
        PortableDependencyKind.Operation => "operation",
        PortableDependencyKind.Profile => "profile",
        PortableDependencyKind.Binding => "binding",
        PortableDependencyKind.ResourceFlavor => "resource-flavor",
        PortableDependencyKind.Feature => "feature",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static string Token(this PortableRequirementStrength strength) => strength switch
    {
        PortableRequirementStrength.Required => "required",
        PortableRequirementStrength.Preferred => "preferred",
        PortableRequirementStrength.Optional => "optional",
        PortableRequirementStrength.Opposed => "opposed",
        _ => throw new ArgumentOutOfRangeException(nameof(strength))
    };

    public static string Token(this PortableShapeKind kind) => kind switch
    {
        PortableShapeKind.Record => "record",
        PortableShapeKind.Sequence => "sequence",
        PortableShapeKind.Choice => "choice",
        PortableShapeKind.Scalar => "scalar",
        PortableShapeKind.Unit => "unit",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static string Token(this PortableFragmentPolicy policy) =>
        policy == PortableFragmentPolicy.Open ? "open" : "closed";

    public static string Token(this PortableScalarKind scalar) => scalar switch
    {
        PortableScalarKind.Text => "Text",
        PortableScalarKind.Boolean => "Boolean",
        PortableScalarKind.Signed64 => "Integer.Signed64",
        PortableScalarKind.Decimal => "Decimal",
        PortableScalarKind.Bytes => "Bytes",
        _ => throw new ArgumentOutOfRangeException(nameof(scalar))
    };

    public static string Token(this PortableAuthorityMode mode) =>
        mode == PortableAuthorityMode.LocalDomainEvaluated
            ? "local-domain-evaluated"
            : "cross-trust-no-capability-transfer";

    public static string Token(this PortableRealization realization) =>
        realization == PortableRealization.FixedDirectCall ? "fixed-direct-call" : "negotiated-process";

    /// <summary>
    /// An unknown value in a declared enumeration is 'malformed-message'; it is never defaulted to a
    /// permissive member.
    /// </summary>
    public static T Parse<T>(string token, string what) where T : struct, Enum
    {
        foreach (var candidate in Enum.GetValues<T>())
        {
            if (string.Equals(TokenOf(candidate), token, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        throw PortableFaultException.Malformed($"{what}-enumeration", $"'{token}' is outside the declared '{what}' enumeration.");
    }

    private static string TokenOf<T>(T value) where T : struct, Enum => value switch
    {
        PortableDependencyKind kind => kind.Token(),
        PortableRequirementStrength strength => strength.Token(),
        PortableShapeKind kind => kind.Token(),
        PortableFragmentPolicy policy => policy.Token(),
        PortableScalarKind scalar => scalar.Token(),
        PortableAuthorityMode mode => mode.Token(),
        PortableRealization realization => realization.Token(),
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
}

public sealed record PortableProvision(
    PortableDependencyKind Kind,
    PortableDependencyReference Reference,
    bool ProviderSpecific);

public sealed record PortableRequirement(
    PortableDependencyKind Kind,
    PortableDependencyReference Reference,
    PortableRequirementStrength Strength,
    bool ProviderSpecific);

public sealed record PortableFieldDeclaration(string Name, PortableShapeReference Shape, bool Required);

public sealed record PortableAlternativeDeclaration(string Name, PortableShapeReference Shape);

public sealed record PortableShapeDeclaration(
    PortableShapeReference Reference,
    PortableShapeKind Kind,
    PortableFragmentPolicy? FragmentPolicy,
    ImmutableArray<PortableFieldDeclaration> Fields,
    PortableShapeReference? ItemShape,
    ImmutableArray<PortableAlternativeDeclaration> Alternatives,
    PortableScalarKind? Scalar)
{
    public static PortableShapeDeclaration Record(
        PortableShapeReference reference,
        PortableFragmentPolicy policy,
        params PortableFieldDeclaration[] fields) =>
        new(reference, PortableShapeKind.Record, policy, [.. fields], null, [], null);

    public static PortableShapeDeclaration Sequence(PortableShapeReference reference, PortableShapeReference itemShape) =>
        new(reference, PortableShapeKind.Sequence, null, [], itemShape, [], null);

    public static PortableShapeDeclaration Choice(
        PortableShapeReference reference,
        params PortableAlternativeDeclaration[] alternatives) =>
        new(reference, PortableShapeKind.Choice, null, [], null, [.. alternatives], null);

    public static PortableShapeDeclaration ScalarShape(PortableShapeReference reference, PortableScalarKind scalar) =>
        new(reference, PortableShapeKind.Scalar, null, [], null, [], scalar);

    public static PortableShapeDeclaration Unit(PortableShapeReference reference) =>
        new(reference, PortableShapeKind.Unit, null, [], null, [], null);
}

public sealed record PortableFragmentDeclaration(
    PortableFragmentReference Reference,
    PortableShapeReference HostShape,
    ImmutableArray<PortableFieldDeclaration> Fields);

public sealed record PortableOperationDeclaration(
    PortableOperationReference Reference,
    PortableShapeReference InputShape,
    PortableShapeReference ResultShape,
    PortableShapeReference DetailShape,
    ImmutableArray<PortableFragmentReference> RequiredFragments,
    ImmutableArray<string> ResourceFlavors);

public sealed record PortableAuthorityDeclaration(
    PortableAuthorityMode PresentationMode,
    bool TrustBoundaryCrossed,
    bool NoCapabilityTransfer,
    string ConstraintPolicy)
{
    public const string OnlyPermittedConstraintPolicy = "fail-closed";
}

public sealed record PortableRepresentationDeclaration(
    string Representation,
    string Framing,
    ImmutableArray<string> ResourceFlavors,
    ImmutableArray<string> AcceptedResourceHandles);

public sealed record PortableLifecycleDeclaration(
    bool ReplayProtectionDeclared,
    string ReplayWindow,
    ImmutableSortedDictionary<string, bool> Features);

/// <summary>
/// One versioned Component contract, established before any provider effect.
/// </summary>
/// <remarks>
/// The document replaces the two divergent manifest encodings the baseline inventory found: it
/// names the detail Shape explicitly rather than reaching it only through a failure path, and it
/// uses one structured canonical reference rather than mixing structured and 'name@version' forms.
/// </remarks>
public sealed record PortableContractDocument(
    int ContractVersion,
    PortableComponentReference Component,
    PortableProviderReference Provider,
    ImmutableArray<PortableProvision> Provisions,
    ImmutableArray<PortableRequirement> Requirements,
    ImmutableArray<PortableOperationDeclaration> Operations,
    ImmutableArray<PortableShapeDeclaration> Shapes,
    ImmutableArray<PortableFragmentDeclaration> Fragments,
    PortableAuthorityDeclaration Authority,
    PortableRepresentationDeclaration Representation,
    PortableLimits Limits,
    PortableLifecycleDeclaration Lifecycle)
{
    public const int SupportedContractVersion = 1;
}

/// <summary>Strict codec for the contract document; unknown fields and enumeration values fail closed.</summary>
public static class PortableContractCodec
{
    private static readonly string[] DocumentFields =
    [
        "contractVersion", "component", "provider", "provisions", "requirements", "operations",
        "shapes", "fragments", "authority", "representation", "limits", "lifecycle"
    ];

    private static readonly string[] LimitFields =
    [
        "maxFrameBytes", "maxNestingDepth", "maxRecordFields", "maxFragmentsPerRecord",
        "maxSequenceItems", "maxTextBytes", "maxByteStringBytes", "maxResourceBytes",
        "ioTimeoutMilliseconds", "maxConcurrentRequests"
    ];

    public static CborItem Encode(PortableContractDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return CborMap.Of(
        [
            ("contractVersion", new CborInteger(document.ContractVersion)),
            ("component", CborAccess.Reference(document.Component.Name.Value, document.Component.Version)),
            ("provider", CborAccess.Reference(document.Provider.Name.Value, document.Provider.Version)),
            ("provisions", new CborArray([.. document.Provisions.Select(EncodeProvision)])),
            ("requirements", new CborArray([.. document.Requirements.Select(EncodeRequirement)])),
            ("operations", new CborArray([.. document.Operations.Select(EncodeOperation)])),
            ("shapes", new CborArray([.. document.Shapes.Select(EncodeShape)])),
            ("fragments", new CborArray([.. document.Fragments.Select(EncodeFragment)])),
            ("authority", EncodeAuthority(document.Authority)),
            ("representation", EncodeRepresentation(document.Representation)),
            ("limits", EncodeLimits(document.Limits)),
            ("lifecycle", EncodeLifecycle(document.Lifecycle))
        ]);
    }

    public static PortableContractDocument Decode(CborItem item)
    {
        var map = CborAccess.RequireMap(item, "contract");
        PortableForbiddenContent.RequireCleanControlItem(map);
        map.RequireDeclaredFields("contract", DocumentFields);

        var contractVersion = map.RequireInt32("contractVersion");
        if (contractVersion != PortableContractDocument.SupportedContractVersion)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedVersion,
                "contract-version",
                $"Contract version {contractVersion} is not recognized.");
        }

        var component = CborAccess.ReadReference(map.Require("component"), "component");
        var provider = CborAccess.ReadReference(map.Require("provider"), "provider");

        return new PortableContractDocument(
            contractVersion,
            new PortableComponentReference(component.Name, component.Version),
            new PortableProviderReference(provider.Name, provider.Version),
            map.RequireArray("provisions", DecodeProvision),
            map.RequireArray("requirements", DecodeRequirement),
            map.RequireArray("operations", DecodeOperation),
            map.RequireArray("shapes", DecodeShape),
            map.RequireArray("fragments", DecodeFragment),
            DecodeAuthority(map.RequireMap("authority")),
            DecodeRepresentation(map.RequireMap("representation")),
            DecodeLimits(map.RequireMap("limits")),
            DecodeLifecycle(map.RequireMap("lifecycle")));
    }

    // -- provisions and requirements -------------------------------------

    private static CborItem EncodeProvision(PortableProvision provision) => CborMap.Of(
    [
        ("kind", new CborText(provision.Kind.Token())),
        ("reference", CborAccess.Reference(provision.Reference.Name.Value, provision.Reference.Version)),
        ("providerSpecific", new CborBoolean(provision.ProviderSpecific))
    ]);

    private static PortableProvision DecodeProvision(CborItem item)
    {
        var map = CborAccess.RequireMap(item, "provision");
        map.RequireDeclaredFields("provision", "kind", "reference", "providerSpecific");
        var reference = CborAccess.ReadReference(map.Require("reference"), "provision-reference");
        return new PortableProvision(
            PortableEnums.Parse<PortableDependencyKind>(map.RequireText("kind"), "provision-kind"),
            new PortableDependencyReference(reference.Name, reference.Version),
            map.RequireBoolean("providerSpecific"));
    }

    private static CborItem EncodeRequirement(PortableRequirement requirement) => CborMap.Of(
    [
        ("kind", new CborText(requirement.Kind.Token())),
        ("reference", CborAccess.Reference(requirement.Reference.Name.Value, requirement.Reference.Version)),
        ("strength", new CborText(requirement.Strength.Token())),
        ("providerSpecific", new CborBoolean(requirement.ProviderSpecific))
    ]);

    private static PortableRequirement DecodeRequirement(CborItem item)
    {
        var map = CborAccess.RequireMap(item, "requirement");
        map.RequireDeclaredFields("requirement", "kind", "reference", "strength", "providerSpecific");
        var reference = CborAccess.ReadReference(map.Require("reference"), "requirement-reference");
        return new PortableRequirement(
            PortableEnums.Parse<PortableDependencyKind>(map.RequireText("kind"), "requirement-kind"),
            new PortableDependencyReference(reference.Name, reference.Version),
            PortableEnums.Parse<PortableRequirementStrength>(map.RequireText("strength"), "requirement-strength"),
            map.RequireBoolean("providerSpecific"));
    }

    // -- operations -------------------------------------------------------

    private static CborItem EncodeOperation(PortableOperationDeclaration operation) => CborMap.Of(
    [
        ("reference", CborAccess.Reference(operation.Reference.Name.Value, operation.Reference.Version)),
        ("inputShape", CborAccess.Reference(operation.InputShape.Name.Value, operation.InputShape.Version)),
        ("resultShape", CborAccess.Reference(operation.ResultShape.Name.Value, operation.ResultShape.Version)),
        ("detailShape", CborAccess.Reference(operation.DetailShape.Name.Value, operation.DetailShape.Version)),
        ("requiredFragments", new CborArray(
            [.. operation.RequiredFragments.Select(fragment => CborAccess.Reference(fragment.Name.Value, fragment.Version))])),
        ("resourceFlavors", new CborArray([.. operation.ResourceFlavors.Select(flavor => (CborItem)new CborText(flavor))]))
    ]);

    private static PortableOperationDeclaration DecodeOperation(CborItem item)
    {
        var map = CborAccess.RequireMap(item, "operation");
        map.RequireDeclaredFields(
            "operation",
            "reference",
            "inputShape",
            "resultShape",
            "detailShape",
            "requiredFragments",
            "resourceFlavors");
        var reference = CborAccess.ReadReference(map.Require("reference"), "operation-reference");
        return new PortableOperationDeclaration(
            new PortableOperationReference(reference.Name, reference.Version),
            ReadShapeReference(map.Require("inputShape"), "inputShape"),
            ReadShapeReference(map.Require("resultShape"), "resultShape"),
            ReadShapeReference(map.Require("detailShape"), "detailShape"),
            map.RequireArray("requiredFragments", element => ReadFragmentReference(element, "requiredFragment")),
            map.RequireArray("resourceFlavors", element => RequireText(element, "resourceFlavor")));
    }

    // -- shapes and fragments ---------------------------------------------

    private static CborItem EncodeShape(PortableShapeDeclaration shape)
    {
        var entries = new List<(string, CborItem)>
        {
            ("reference", CborAccess.Reference(shape.Reference.Name.Value, shape.Reference.Version)),
            ("kind", new CborText(shape.Kind.Token()))
        };

        if (shape.FragmentPolicy is { } policy)
        {
            entries.Add(("fragmentPolicy", new CborText(policy.Token())));
        }

        if (shape.Kind == PortableShapeKind.Record)
        {
            entries.Add(("fields", new CborArray([.. shape.Fields.Select(EncodeField)])));
        }

        if (shape.ItemShape is { } itemShape)
        {
            entries.Add(("itemShape", CborAccess.Reference(itemShape.Name.Value, itemShape.Version)));
        }

        if (shape.Kind == PortableShapeKind.Choice)
        {
            entries.Add(("alternatives", new CborArray([.. shape.Alternatives.Select(EncodeAlternative)])));
        }

        if (shape.Scalar is { } scalar)
        {
            entries.Add(("scalar", new CborText(scalar.Token())));
        }

        return CborMap.Of(entries);
    }

    private static PortableShapeDeclaration DecodeShape(CborItem item)
    {
        var map = CborAccess.RequireMap(item, "shape");
        map.RequireDeclaredFields("shape", "reference", "kind", "fragmentPolicy", "fields", "itemShape", "alternatives", "scalar");
        var reference = ReadShapeReference(map.Require("reference"), "shape-reference");
        var kind = PortableEnums.Parse<PortableShapeKind>(map.RequireText("kind"), "shape-kind");

        var policyToken = map.OptionalText("fragmentPolicy");
        PortableFragmentPolicy? policy = policyToken is null
            ? null
            : PortableEnums.Parse<PortableFragmentPolicy>(policyToken, "fragment-policy");
        var scalarToken = map.OptionalText("scalar");
        PortableScalarKind? scalar = scalarToken is null
            ? null
            : PortableEnums.Parse<PortableScalarKind>(scalarToken, "scalar-kind");

        var fields = map.Contains("fields") ? map.RequireArray("fields", DecodeField) : ImmutableArray<PortableFieldDeclaration>.Empty;
        var alternatives = map.Contains("alternatives")
            ? map.RequireArray("alternatives", DecodeAlternative)
            : ImmutableArray<PortableAlternativeDeclaration>.Empty;
        PortableShapeReference? itemShape = map.Contains("itemShape")
            ? ReadShapeReference(map.Require("itemShape"), "itemShape")
            : null;

        var declaration = new PortableShapeDeclaration(reference, kind, policy, fields, itemShape, alternatives, scalar);
        RequireShapeMembers(declaration, map);
        return declaration;
    }

    /// <summary>A member required by one kind and forbidden for the others is enforced both ways.</summary>
    private static void RequireShapeMembers(PortableShapeDeclaration shape, CborMap map)
    {
        void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw PortableFaultException.Malformed("shape-members", message);
            }
        }

        var isRecord = shape.Kind == PortableShapeKind.Record;
        Require(isRecord == shape.FragmentPolicy.HasValue, "fragmentPolicy is required for a record Shape and forbidden otherwise.");
        Require(isRecord == map.Contains("fields"), "fields are required for a record Shape and forbidden otherwise.");
        Require((shape.Kind == PortableShapeKind.Sequence) == shape.ItemShape.HasValue, "itemShape is required for a sequence Shape and forbidden otherwise.");
        Require((shape.Kind == PortableShapeKind.Choice) == map.Contains("alternatives"), "alternatives are required for a choice Shape and forbidden otherwise.");
        Require((shape.Kind == PortableShapeKind.Scalar) == shape.Scalar.HasValue, "scalar is required for a scalar Shape and forbidden otherwise.");
    }

    private static CborItem EncodeField(PortableFieldDeclaration field) => CborMap.Of(
    [
        ("name", new CborText(field.Name)),
        ("shape", CborAccess.Reference(field.Shape.Name.Value, field.Shape.Version)),
        ("required", new CborBoolean(field.Required))
    ]);

    private static PortableFieldDeclaration DecodeField(CborItem item)
    {
        var map = CborAccess.RequireMap(item, "field");
        map.RequireDeclaredFields("field", "name", "shape", "required");
        return new PortableFieldDeclaration(
            PortableMemberToken.Parse(map.RequireText("name")).Value,
            ReadShapeReference(map.Require("shape"), "field-shape"),
            map.RequireBoolean("required"));
    }

    private static CborItem EncodeAlternative(PortableAlternativeDeclaration alternative) => CborMap.Of(
    [
        ("name", new CborText(alternative.Name)),
        ("shape", CborAccess.Reference(alternative.Shape.Name.Value, alternative.Shape.Version))
    ]);

    private static PortableAlternativeDeclaration DecodeAlternative(CborItem item)
    {
        var map = CborAccess.RequireMap(item, "alternative");
        map.RequireDeclaredFields("alternative", "name", "shape");
        return new PortableAlternativeDeclaration(
            PortableMemberToken.Parse(map.RequireText("name")).Value,
            ReadShapeReference(map.Require("shape"), "alternative-shape"));
    }

    private static CborItem EncodeFragment(PortableFragmentDeclaration fragment) => CborMap.Of(
    [
        ("reference", CborAccess.Reference(fragment.Reference.Name.Value, fragment.Reference.Version)),
        ("hostShape", CborAccess.Reference(fragment.HostShape.Name.Value, fragment.HostShape.Version)),
        ("fields", new CborArray([.. fragment.Fields.Select(EncodeField)]))
    ]);

    private static PortableFragmentDeclaration DecodeFragment(CborItem item)
    {
        var map = CborAccess.RequireMap(item, "fragment");
        map.RequireDeclaredFields("fragment", "reference", "hostShape", "fields");
        return new PortableFragmentDeclaration(
            ReadFragmentReference(map.Require("reference"), "fragment-reference"),
            ReadShapeReference(map.Require("hostShape"), "fragment-host"),
            map.RequireArray("fields", DecodeField));
    }

    // -- authority, representation, limits, lifecycle ----------------------

    private static CborItem EncodeAuthority(PortableAuthorityDeclaration authority) => CborMap.Of(
    [
        ("presentationMode", new CborText(authority.PresentationMode.Token())),
        ("trustBoundaryCrossed", new CborBoolean(authority.TrustBoundaryCrossed)),
        ("noCapabilityTransfer", new CborBoolean(authority.NoCapabilityTransfer)),
        ("constraintPolicy", new CborText(authority.ConstraintPolicy))
    ]);

    private static PortableAuthorityDeclaration DecodeAuthority(CborMap map)
    {
        map.RequireDeclaredFields(
            "authority",
            "presentationMode",
            "trustBoundaryCrossed",
            "noCapabilityTransfer",
            "constraintPolicy");
        return new PortableAuthorityDeclaration(
            PortableEnums.Parse<PortableAuthorityMode>(map.RequireText("presentationMode"), "presentation-mode"),
            map.RequireBoolean("trustBoundaryCrossed"),
            map.RequireBoolean("noCapabilityTransfer"),
            map.RequireText("constraintPolicy"));
    }

    private static CborItem EncodeRepresentation(PortableRepresentationDeclaration representation) => CborMap.Of(
    [
        ("representation", new CborText(representation.Representation)),
        ("framing", new CborText(representation.Framing)),
        ("resourceFlavors", new CborArray([.. representation.ResourceFlavors.Select(flavor => (CborItem)new CborText(flavor))])),
        ("acceptedResourceHandles", new CborArray(
            [.. representation.AcceptedResourceHandles.Select(handle => (CborItem)new CborText(handle))]))
    ]);

    private static PortableRepresentationDeclaration DecodeRepresentation(CborMap map)
    {
        map.RequireDeclaredFields("representation", "representation", "framing", "resourceFlavors", "acceptedResourceHandles");
        return new PortableRepresentationDeclaration(
            map.RequireText("representation"),
            map.RequireText("framing"),
            map.RequireArray("resourceFlavors", element => RequireText(element, "resourceFlavor")),
            map.RequireArray("acceptedResourceHandles", element => RequireText(element, "acceptedResourceHandle")));
    }

    private static CborItem EncodeLimits(PortableLimits limits) => CborMap.Of(
    [
        ("maxFrameBytes", new CborInteger(limits.MaxFrameBytes)),
        ("maxNestingDepth", new CborInteger(limits.MaxNestingDepth)),
        ("maxRecordFields", new CborInteger(limits.MaxRecordFields)),
        ("maxFragmentsPerRecord", new CborInteger(limits.MaxFragmentsPerRecord)),
        ("maxSequenceItems", new CborInteger(limits.MaxSequenceItems)),
        ("maxTextBytes", new CborInteger(limits.MaxTextBytes)),
        ("maxByteStringBytes", new CborInteger(limits.MaxByteStringBytes)),
        ("maxResourceBytes", new CborInteger(limits.MaxResourceBytes)),
        ("ioTimeoutMilliseconds", new CborInteger(limits.IoTimeoutMilliseconds)),
        ("maxConcurrentRequests", new CborInteger(limits.MaxConcurrentRequests))
    ]);

    private static PortableLimits DecodeLimits(CborMap map)
    {
        map.RequireDeclaredFields("limits", LimitFields);
        var limits = new PortableLimits(
            map.RequireInt32("maxFrameBytes"),
            map.RequireInt32("maxNestingDepth"),
            map.RequireInt32("maxRecordFields"),
            map.RequireInt32("maxFragmentsPerRecord"),
            map.RequireInt32("maxSequenceItems"),
            map.RequireInt32("maxTextBytes"),
            map.RequireInt32("maxByteStringBytes"),
            map.RequireInt32("maxResourceBytes"),
            map.RequireInt32("ioTimeoutMilliseconds"),
            map.RequireInt32("maxConcurrentRequests"));
        limits.Validate();
        return limits;
    }

    private static CborItem EncodeLifecycle(PortableLifecycleDeclaration lifecycle) => CborMap.Of(
    [
        ("replayProtectionDeclared", new CborBoolean(lifecycle.ReplayProtectionDeclared)),
        ("replayWindow", new CborText(lifecycle.ReplayWindow)),
        ("features", CborMap.Of(
            lifecycle.Features.Select(feature => (feature.Key, (CborItem)new CborBoolean(feature.Value)))))
    ]);

    private static PortableLifecycleDeclaration DecodeLifecycle(CborMap map)
    {
        map.RequireDeclaredFields("lifecycle", "replayProtectionDeclared", "replayWindow", "features");
        var features = map.RequireMap("features");
        var builder = ImmutableSortedDictionary.CreateBuilder<string, bool>(StringComparer.Ordinal);
        foreach (var entry in features.Entries)
        {
            builder[entry.Key] = entry.Value is CborBoolean boolean
                ? boolean.Value
                : throw PortableFaultException.Malformed("feature-kind", $"Lifecycle feature '{entry.Key}' must be a boolean.");
        }

        return new PortableLifecycleDeclaration(
            map.RequireBoolean("replayProtectionDeclared"),
            map.RequireText("replayWindow"),
            builder.ToImmutable());
    }

    // -- shared readers ----------------------------------------------------

    internal static PortableShapeReference ReadShapeReference(CborItem item, string what)
    {
        var reference = CborAccess.ReadReference(item, what);
        return new PortableShapeReference(reference.Name, reference.Version);
    }

    internal static PortableFragmentReference ReadFragmentReference(CborItem item, string what)
    {
        var reference = CborAccess.ReadReference(item, what);
        return new PortableFragmentReference(reference.Name, reference.Version);
    }

    private static string RequireText(CborItem item, string what) =>
        item is CborText text
            ? text.Value
            : throw PortableFaultException.Malformed($"{what}-kind", $"'{what}' must be a text string.");
}
