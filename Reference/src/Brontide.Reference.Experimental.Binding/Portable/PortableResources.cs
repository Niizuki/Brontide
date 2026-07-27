using System.Collections.Immutable;
using System.Security.Cryptography;

namespace Brontide.Reference.Experimental.Binding.Portable;

public enum PortableResourceFlavor
{
    CopiedImmutableBlob,
    AddressingOnlyHandle
}

/// <summary>The declared referenced-resource flavors and the ones version 0.1 refuses.</summary>
public static class PortableResourceFlavors
{
    public const string CopiedImmutableBlob = "copied-immutable-blob";
    public const string AddressingOnlyHandle = "addressing-only-handle";

    /// <summary>
    /// Borrow intervals and ownership transfer are 0.1 non-goals. Declaring one fails negotiation
    /// closed; there is no fallback and no silent downgrade to the copied flavor.
    /// </summary>
    public static ImmutableArray<string> NonGoals { get; } =
    [
        "borrowed-read-only-region",
        "transferred-ownership"
    ];

    public static string Token(this PortableResourceFlavor flavor) =>
        flavor == PortableResourceFlavor.CopiedImmutableBlob ? CopiedImmutableBlob : AddressingOnlyHandle;

    public static PortableResourceFlavor Parse(string token) => token switch
    {
        CopiedImmutableBlob => PortableResourceFlavor.CopiedImmutableBlob,
        AddressingOnlyHandle => PortableResourceFlavor.AddressingOnlyHandle,
        _ => throw new PortableFaultException(
            PortableProtocolCategory.UnsupportedContract,
            "unsupported-resource-flavor",
            $"Resource flavor '{token}' is not supported in version 0.1; there is no fallback.")
    };

    public static string Ownership(this PortableResourceFlavor flavor) =>
        flavor == PortableResourceFlavor.CopiedImmutableBlob ? "copied" : "provider-retained";
}

/// <summary>A referenced shaped resource carried alongside an inline request payload.</summary>
public abstract record PortableResource(string Name)
{
    public abstract PortableResourceFlavor Flavor { get; }
}

/// <summary>
/// The v0.1 floor: the receiver obtains an independent immutable copy verified by content hash.
/// </summary>
/// <remarks>
/// Because ownership is by copy there is no borrow interval, the sender retains its own copy, and
/// no release or completion signal is defined. A frame carrying one is a state violation rather
/// than a harmless no-op.
/// </remarks>
public sealed record PortableCopiedBlobResource(string Name, ReadOnlyMemory<byte> Content, string Integrity)
    : PortableResource(Name)
{
    public override PortableResourceFlavor Flavor => PortableResourceFlavor.CopiedImmutableBlob;

    public static PortableCopiedBlobResource Create(string name, ReadOnlySpan<byte> content) =>
        new(name, content.ToArray(), HashOf(content));

    public static string HashOf(ReadOnlySpan<byte> content) => Convert.ToHexStringLower(SHA256.HashData(content));
}

/// <summary>
/// The Catalog-derived flavor: a handle conveys addressing and never authority, so possession is
/// not an admission decision.
/// </summary>
public sealed record PortableHandleResource(string Name, string Provider, string Id) : PortableResource(Name)
{
    public override PortableResourceFlavor Flavor => PortableResourceFlavor.AddressingOnlyHandle;

    public string CanonicalText => $"{Provider}/{Id}";
}

/// <summary>What one interaction observed about one referenced resource.</summary>
public sealed record PortableResourceObservation(
    string Flavor,
    string Ownership,
    long Copies,
    bool IntegrityVerified,
    bool Accepted)
{
    /// <summary>
    /// The same resource as observed by an interaction that failed.
    /// </summary>
    /// <remarks>
    /// Acceptance and integrity are facts about a completed admission, not about the flavor. An
    /// interaction that never completed one observed neither, so claiming either would be a false
    /// success in the observation set: a refused blob would report that its content hash verified.
    /// The flavor, ownership, and copy count still hold, because those describe what was presented.
    /// </remarks>
    public PortableResourceObservation AsRefused() =>
        this with { IntegrityVerified = false, Accepted = false };
}

/// <summary>Codec and admission rules for referenced resources.</summary>
public static class PortableResourceCodec
{
    private static readonly string[] DeclaredFields =
    [
        "flavor", "name", "content", "integrity", "provider", "id", "release"
    ];

    public static CborItem Encode(PortableResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return resource switch
        {
            PortableCopiedBlobResource blob => CborMap.Of(
            [
                ("flavor", new CborText(PortableResourceFlavors.CopiedImmutableBlob)),
                ("name", new CborText(blob.Name)),
                ("content", new CborBytes(blob.Content)),
                ("integrity", new CborText(blob.Integrity))
            ]),
            PortableHandleResource handle => CborMap.Of(
            [
                ("flavor", new CborText(PortableResourceFlavors.AddressingOnlyHandle)),
                ("name", new CborText(handle.Name)),
                ("provider", new CborText(handle.Provider)),
                ("id", new CborText(handle.Id))
            ]),
            _ => throw PortableFaultException.InvalidPayload("resource-flavor", "The resource flavor has no portable encoding.")
        };
    }

    /// <summary>
    /// Decodes and admits one declared resource against the frozen plan facts.
    /// </summary>
    /// <remarks>
    /// Every refusal here happens before any provider effect, and none of them is an authority
    /// decision: a handle carries no authority, so refusing one is a payload decision.
    /// </remarks>
    public static PortableResource Decode(
        CborItem item,
        ImmutableArray<string> negotiatedFlavors,
        ImmutableArray<string> acceptedHandles,
        PortableLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        var map = CborAccess.RequireMap(item, "resource");
        map.RequireDeclaredFields("resource", DeclaredFields);
        var flavorToken = map.RequireText("flavor");
        var flavor = PortableResourceFlavors.Parse(flavorToken);
        var name = map.RequireText("name");
        var hasContent = map.Contains("content");
        var hasHandle = map.Contains("provider") || map.Contains("id");

        if (map.Contains("release"))
        {
            throw new PortableFaultException(
                PortableProtocolCategory.StateViolation,
                "release-signal-undefined",
                $"Flavor '{flavorToken}' defines no release signal, so a frame carrying one is illegal.");
        }

        PortableResource resource;
        if (flavor == PortableResourceFlavor.CopiedImmutableBlob)
        {
            if (hasHandle)
            {
                throw PortableFaultException.InvalidPayload(
                    "resource-members",
                    "A copied-immutable-blob resource carries content and integrity, not a handle.");
            }

            if (map.Require("content") is not CborBytes content)
            {
                throw PortableFaultException.InvalidPayload("resource-content", "Resource content must be a byte string.");
            }

            resource = new PortableCopiedBlobResource(name, content.Value, map.RequireText("integrity"));
        }
        else
        {
            if (hasContent)
            {
                // The addressing-only handle permits no copy at all, so producing octets for it is
                // a forbidden implicit copy rather than an accepted convenience.
                throw PortableFaultException.InvalidPayload(
                    "forbidden-implicit-copy",
                    "The addressing-only-handle flavor permits no copy, so resource octets may not accompany it.");
            }

            resource = new PortableHandleResource(name, map.RequireText("provider"), map.RequireText("id"));
        }

        Admit(resource, negotiatedFlavors, acceptedHandles, limits);
        return resource;
    }

    /// <summary>
    /// Applies the admission rules a realization cannot skip: the flavor is negotiated, the bound
    /// holds, the content hash verifies, and a handle sits inside the declared accept list.
    /// </summary>
    /// <remarks>
    /// The fixed direct-call realization never decodes a frame, so it calls this directly and
    /// refuses exactly what the process realization refuses.
    /// </remarks>
    public static void Admit(
        PortableResource resource,
        ImmutableArray<string> negotiatedFlavors,
        ImmutableArray<string> acceptedHandles,
        PortableLimits limits)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(limits);
        if (!negotiatedFlavors.Contains(resource.Flavor.Token(), StringComparer.Ordinal))
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "resource-flavor-unnegotiated",
                $"The Binding Plan did not negotiate resource flavor '{resource.Flavor.Token()}'.");
        }

        switch (resource)
        {
            case PortableCopiedBlobResource blob:
            {
                if (blob.Content.Length > limits.MaxResourceBytes)
                {
                    throw PortableFaultException.LimitExceeded(
                        "resource-bound",
                        $"A resource of {blob.Content.Length} bytes exceeds the declared bound of {limits.MaxResourceBytes}.");
                }

                var wellFormed = blob.Integrity.Length == 64 &&
                    blob.Integrity.All(character => character is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
                if (!wellFormed)
                {
                    throw PortableFaultException.InvalidPayload(
                        "resource-integrity-form",
                        "A content hash is 64 lowercase hexadecimal characters of SHA-256.");
                }

                if (!string.Equals(
                        PortableCopiedBlobResource.HashOf(blob.Content.Span),
                        blob.Integrity,
                        StringComparison.Ordinal))
                {
                    throw PortableFaultException.InvalidPayload(
                        "resource-integrity",
                        "The received resource octets do not hash to the declared content hash.");
                }

                break;
            }
            case PortableHandleResource handle when !acceptedHandles.Contains(handle.CanonicalText, StringComparer.Ordinal):
                // A handle carries no authority, so refusing one is a payload decision rather than
                // an authority decision.
                throw PortableFaultException.InvalidPayload(
                    "resource-refused",
                    $"Handle '{handle.CanonicalText}' is outside the accept list the Binding Plan declares.");
            default:
                break;
        }
    }

    /// <summary>
    /// The copy accounting one realization implies. It is a required observation, so a forbidden
    /// implicit copy is visible rather than inferred.
    /// </summary>
    public static long CopiesFor(PortableResourceFlavor flavor, PortableRealization realization) =>
        flavor == PortableResourceFlavor.CopiedImmutableBlob && realization == PortableRealization.NegotiatedProcess
            ? 1
            : 0;

    public static PortableResourceObservation Observe(PortableResource resource, PortableRealization realization)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return new PortableResourceObservation(
            resource.Flavor.Token(),
            resource.Flavor.Ownership(),
            CopiesFor(resource.Flavor, realization),
            resource.Flavor == PortableResourceFlavor.CopiedImmutableBlob,
            true);
    }
}
