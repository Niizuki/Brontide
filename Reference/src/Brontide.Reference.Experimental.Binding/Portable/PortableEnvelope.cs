using System.Collections.Immutable;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>The Channel envelope kinds.</summary>
/// <remarks>
/// There is deliberately no denial kind. A local authority denial emits no frame at all, so the
/// Cooling experiment's denial message is not carried into the portable envelope set. Process loss
/// is likewise observed locally rather than signalled.
/// </remarks>
public enum PortableEnvelopeKind
{
    Establish,
    EstablishAccepted,
    Ready,
    Request,
    Outcome,
    ProtocolError,
    Withdraw,
    Terminate
}

public static class PortableEnvelopeKinds
{
    public static string Token(this PortableEnvelopeKind kind) => kind switch
    {
        PortableEnvelopeKind.Establish => "establish",
        PortableEnvelopeKind.EstablishAccepted => "establish-accepted",
        PortableEnvelopeKind.Ready => "ready",
        PortableEnvelopeKind.Request => "request",
        PortableEnvelopeKind.Outcome => "outcome",
        PortableEnvelopeKind.ProtocolError => "protocol-error",
        PortableEnvelopeKind.Withdraw => "withdraw",
        PortableEnvelopeKind.Terminate => "terminate",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static PortableEnvelopeKind Parse(string token)
    {
        foreach (var kind in Enum.GetValues<PortableEnvelopeKind>())
        {
            if (string.Equals(kind.Token(), token, StringComparison.Ordinal))
            {
                return kind;
            }
        }

        throw new PortableFaultException(
            PortableProtocolCategory.UnsupportedKind,
            "unknown-kind",
            $"Envelope kind '{token}' is outside the declared set; an unknown kind is never treated as a no-op.");
    }

    /// <summary>A request, an outcome, and a correlated protocol error all require a request identity.</summary>
    public static bool RequiresRequestId(this PortableEnvelopeKind kind) =>
        kind is PortableEnvelopeKind.Request or PortableEnvelopeKind.Outcome;
}

/// <summary>One Channel envelope.</summary>
public sealed record PortableEnvelope(
    int ContractVersion,
    PortableEnvelopeKind Kind,
    PortableChannelId ChannelId,
    PortableChannelRequestId? RequestId,
    PortableChannelExecutionId? ExecutionId,
    CborItem Body)
{
    public static PortableEnvelope Control(
        PortableEnvelopeKind kind,
        PortableChannelId channel,
        CborItem? body = null) =>
        new(
            PortableContractDocument.SupportedContractVersion,
            kind,
            channel,
            null,
            null,
            body ?? CborMap.Empty);
}

/// <summary>Strict envelope codec; every miss is a portable category rather than a parser failure.</summary>
public static class PortableEnvelopeCodec
{
    private static readonly string[] EnvelopeFields =
    [
        "contractVersion", "kind", "channelId", "requestId", "executionId", "body"
    ];

    public static byte[] Encode(PortableEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var entries = new List<(string, CborItem)>
        {
            ("contractVersion", new CborInteger(envelope.ContractVersion)),
            ("kind", new CborText(envelope.Kind.Token())),
            ("channelId", new CborText(envelope.ChannelId.Value)),
            ("body", envelope.Body)
        };

        if (envelope.RequestId is { } request)
        {
            entries.Add(("requestId", new CborText(request.Value)));
        }

        if (envelope.ExecutionId is { } execution)
        {
            entries.Add(("executionId", new CborText(execution.Value)));
        }

        return PortableCbor.Encode(CborMap.Of(entries));
    }

    public static PortableEnvelope Decode(ReadOnlySpan<byte> body, PortableLimits limits)
    {
        var item = PortableCbor.Decode(body, limits);
        var map = CborAccess.RequireMap(item, "envelope");

        // Foreign runtime identity is refused before the field set is checked, so the rejection
        // names the rule that actually applies rather than reporting an unknown field.
        foreach (var entry in map.Entries.Where(entry => !string.Equals(entry.Key, "body", StringComparison.Ordinal)))
        {
            if (PortableForbiddenContent.IsForbidden(entry.Key))
            {
                throw PortableFaultException.Malformed(
                    "foreign-runtime-data",
                    $"Control field '{entry.Key}' carries foreign runtime identity.");
            }

            PortableForbiddenContent.RequireCleanControlItem(entry.Value);
        }

        map.RequireDeclaredFields("envelope", EnvelopeFields);

        var contractVersion = map.RequireInt32("contractVersion");
        if (contractVersion != PortableContractDocument.SupportedContractVersion)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedVersion,
                "envelope-version",
                $"Envelope contract version {contractVersion} is not recognized.");
        }

        var kind = PortableEnvelopeKinds.Parse(map.RequireText("kind"));
        var channelId = new PortableChannelId(map.RequireText("channelId"));
        var requestText = map.OptionalText("requestId");
        var executionText = map.OptionalText("executionId");

        if (kind.RequiresRequestId() && requestText is null)
        {
            throw PortableFaultException.Malformed(
                "correlation-absent",
                $"A '{kind.Token()}' envelope carries a request identity; it is never matched to an outstanding request by position.");
        }

        return new PortableEnvelope(
            contractVersion,
            kind,
            channelId,
            requestText is null ? null : new PortableChannelRequestId(requestText),
            executionText is null ? null : new PortableChannelExecutionId(executionText),
            map.Require("body"));
    }
}

/// <summary>The body of an <c>establish-accepted</c> envelope.</summary>
public sealed record PortableEstablishAcceptedBody(
    PortableContractDocument Contract,
    ImmutableArray<PortableCompactAssignment> CompactIdentifiers)
{
    public CborItem Encode() => CborMap.Of(
    [
        ("contract", PortableContractCodec.Encode(Contract)),
        ("compactIdentifiers", new CborArray(
        [
            .. CompactIdentifiers.Select(assignment => (CborItem)CborMap.Of(
            [
                ("space", new CborText(assignment.Space.ToString().ToLowerInvariant())),
                ("reference", new CborText(assignment.Reference)),
                ("compactId", new CborInteger(assignment.CompactId.Value))
            ]))
        ]))
    ]);

    public static PortableEstablishAcceptedBody Decode(CborItem item)
    {
        var map = CborAccess.RequireMap(item, "establish-accepted");
        map.RequireDeclaredFields("establish-accepted", "contract", "compactIdentifiers");
        return new PortableEstablishAcceptedBody(
            PortableContractCodec.Decode(map.Require("contract")),
            map.RequireArray("compactIdentifiers", element =>
            {
                var entry = CborAccess.RequireMap(element, "compactAssignment");
                entry.RequireDeclaredFields("compactAssignment", "space", "reference", "compactId");
                return new PortableCompactAssignment(
                    ParseSpace(entry.RequireText("space")),
                    entry.RequireText("reference"),
                    new PortableCompactIdentifier(entry.RequireInt32("compactId")));
            }));
    }

    private static PortableIdentitySpace ParseSpace(string token) => token switch
    {
        "operation" => PortableIdentitySpace.Operation,
        "shape" => PortableIdentitySpace.Shape,
        "fragment" => PortableIdentitySpace.Fragment,
        _ => throw PortableFaultException.Malformed("compact-space", $"'{token}' is not a compact-identifier space.")
    };
}

/// <summary>The body of a <c>request</c> envelope.</summary>
/// <remarks>
/// The Operation may be named canonically or by a compact identifier this binding assigned. A
/// compact identifier the binding never assigned resolves to no canonical identity, so it is an
/// unsupported contract rather than an unknown Operation.
/// </remarks>
public sealed record PortableRequestBody(
    PortableOperationReference? Operation,
    int? CompactOperation,
    PortableShapeReference InputShape,
    CborItem Input,
    ImmutableArray<CborItem> Resources)
{
    private static readonly string[] Fields = ["operation", "compactOperation", "inputShape", "input", "resources"];

    public CborItem Encode()
    {
        var entries = new List<(string, CborItem)>
        {
            ("inputShape", CborAccess.Reference(InputShape.Name.Value, InputShape.Version)),
            ("input", Input),
            ("resources", new CborArray(Resources))
        };

        if (Operation is { } operation)
        {
            entries.Add(("operation", CborAccess.Reference(operation.Name.Value, operation.Version)));
        }

        if (CompactOperation is { } compact)
        {
            entries.Add(("compactOperation", new CborInteger(compact)));
        }

        return CborMap.Of(entries);
    }

    public static PortableRequestBody Decode(CborItem item)
    {
        var map = CborAccess.RequireMap(item, "request");
        map.RequireDeclaredFields("request", Fields);
        foreach (var entry in map.Entries.Where(entry => !string.Equals(entry.Key, "input", StringComparison.Ordinal)))
        {
            PortableForbiddenContent.RequireCleanControlItem(entry.Value);
        }

        PortableForbiddenContent.RequireCleanPayloadItem(map.Require("input"));

        var hasOperation = map.Contains("operation");
        var hasCompact = map.Contains("compactOperation");
        if (hasOperation == hasCompact)
        {
            throw PortableFaultException.Malformed(
                "operation-designation",
                "A request names its Operation either canonically or by one compact identifier.");
        }

        PortableOperationReference? operation = null;
        if (hasOperation)
        {
            var reference = CborAccess.ReadReference(map.Require("operation"), "operation");
            operation = new PortableOperationReference(reference.Name, reference.Version);
        }

        return new PortableRequestBody(
            operation,
            hasCompact ? map.RequireInt32("compactOperation") : null,
            PortableContractCodec.ReadShapeReference(map.Require("inputShape"), "inputShape"),
            map.Require("input"),
            [.. map.RequireArray("resources").Items]);
    }
}

public enum PortableOutcomeStatus
{
    Succeeded,
    Failed
}

/// <summary>The body of an <c>outcome</c> envelope.</summary>
public sealed record PortableOutcomeBody(
    PortableOutcomeStatus Status,
    PortableShapeReference ValueShape,
    CborItem Value,
    long ProviderEffectCount)
{
    private static readonly string[] Fields = ["status", "valueShape", "value", "providerEffectCount"];

    public CborItem Encode() => CborMap.Of(
    [
        ("status", new CborText(Status == PortableOutcomeStatus.Succeeded ? "succeeded" : "failed")),
        ("valueShape", CborAccess.Reference(ValueShape.Name.Value, ValueShape.Version)),
        ("value", Value),
        ("providerEffectCount", new CborInteger(ProviderEffectCount))
    ]);

    public static PortableOutcomeBody Decode(CborItem item)
    {
        var map = CborAccess.RequireMap(item, "outcome");
        map.RequireDeclaredFields("outcome", Fields);
        foreach (var entry in map.Entries.Where(entry => !string.Equals(entry.Key, "value", StringComparison.Ordinal)))
        {
            PortableForbiddenContent.RequireCleanControlItem(entry.Value);
        }

        PortableForbiddenContent.RequireCleanPayloadItem(map.Require("value"));

        var status = map.RequireText("status") switch
        {
            "succeeded" => PortableOutcomeStatus.Succeeded,
            "failed" => PortableOutcomeStatus.Failed,
            var other => throw PortableFaultException.Malformed(
                "outcome-status",
                $"'{other}' is not a terminal Outcome status.")
        };

        return new PortableOutcomeBody(
            status,
            PortableContractCodec.ReadShapeReference(map.Require("valueShape"), "valueShape"),
            map.Require("value"),
            map.RequireInteger("providerEffectCount"));
    }
}

/// <summary>The body of a <c>protocol-error</c> envelope.</summary>
/// <remarks>
/// The portable category drives semantics. The local code travels beside it as non-normative data,
/// so two realizations may use different local strings for the same category.
/// </remarks>
public sealed record PortableProtocolErrorBody(
    PortableProtocolCategory Category,
    string LocalCode,
    PortableFailureDomain FailureDomain)
{
    public CborItem Encode() => CborMap.Of(
    [
        ("category", new CborText(Category.Token())),
        ("localCode", new CborText(LocalCode)),
        ("failureDomain", new CborText(FailureDomain.Token()))
    ]);

    public static PortableProtocolErrorBody Decode(CborItem item)
    {
        var map = CborAccess.RequireMap(item, "protocol-error");
        map.RequireDeclaredFields("protocol-error", "category", "localCode", "failureDomain");
        PortableForbiddenContent.RequireCleanControlItem(map);
        return new PortableProtocolErrorBody(
            ParseCategory(map.RequireText("category")),
            map.RequireText("localCode"),
            ParseDomain(map.RequireText("failureDomain")));
    }

    private static PortableProtocolCategory ParseCategory(string token)
    {
        foreach (var category in Enum.GetValues<PortableProtocolCategory>())
        {
            if (string.Equals(category.Token(), token, StringComparison.Ordinal))
            {
                return category;
            }
        }

        throw PortableFaultException.Malformed("protocol-category", $"'{token}' is outside the Channel taxonomy.");
    }

    private static PortableFailureDomain ParseDomain(string token)
    {
        foreach (var domain in Enum.GetValues<PortableFailureDomain>())
        {
            if (string.Equals(domain.Token(), token, StringComparison.Ordinal))
            {
                return domain;
            }
        }

        throw PortableFaultException.Malformed("failure-domain", $"'{token}' is outside the Channel taxonomy.");
    }
}
