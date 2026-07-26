using System.Collections.Immutable;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>What the host received back from one request.</summary>
public sealed record PortableOutcomeReceipt(
    PortableOutcomeStatus Status,
    PortableShapeReference ValueShape,
    PortableValue Value,
    long ProviderEffectCount);

/// <summary>
/// One realization of the seam between a host and a provider endpoint.
/// </summary>
/// <remarks>
/// The interface carries shaped values rather than bytes so that the fixed direct-call realization
/// is a real direct call: it never encodes, so its copy accounting is genuinely zero.
/// </remarks>
public interface IPortableProviderConversation : IAsyncDisposable
{
    PortableRealization Realization { get; }

    ValueTask<PortableContractDocument> EstablishAsync(
        PortableContractDocument required,
        string hostEndpoint,
        PortableChannelId channel,
        CancellationToken cancellationToken);

    ValueTask AwaitReadyAsync(PortableChannelId channel, CancellationToken cancellationToken);

    ValueTask<PortableOutcomeReceipt> RequestAsync(
        PortableBindingPlan plan,
        PortableChannelId channel,
        PortableChannelRequestId request,
        PortableChannelExecutionId? execution,
        PortableOperationReference? operation,
        int? compactOperation,
        PortableShapeReference inputShape,
        PortableValue input,
        IReadOnlyList<PortableResource> resources,
        CancellationToken cancellationToken);

    ValueTask WithdrawAsync(PortableChannelId channel, CancellationToken cancellationToken);

    ValueTask TerminateAsync(PortableChannelId channel, CancellationToken cancellationToken);
}

/// <summary>The fixed direct-call realization: no wire, no encoding, no copy.</summary>
public sealed class PortableDirectConversation(PortableProviderEndpoint endpoint) : IPortableProviderConversation
{
    private readonly PortableProviderEndpoint _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

    public PortableRealization Realization => PortableRealization.FixedDirectCall;

    public ValueTask<PortableContractDocument> EstablishAsync(
        PortableContractDocument required,
        string hostEndpoint,
        PortableChannelId channel,
        CancellationToken cancellationToken)
    {
        var accepted = _endpoint.Establish(required, hostEndpoint);
        return ValueTask.FromResult(accepted.Contract);
    }

    public ValueTask AwaitReadyAsync(PortableChannelId channel, CancellationToken cancellationToken)
    {
        _endpoint.SignalReady();
        return ValueTask.CompletedTask;
    }

    public ValueTask<PortableOutcomeReceipt> RequestAsync(
        PortableBindingPlan plan,
        PortableChannelId channel,
        PortableChannelRequestId request,
        PortableChannelExecutionId? execution,
        PortableOperationReference? operation,
        int? compactOperation,
        PortableShapeReference inputShape,
        PortableValue input,
        IReadOnlyList<PortableResource> resources,
        CancellationToken cancellationToken)
    {
        var outcome = _endpoint.Request(request, operation, compactOperation, inputShape, input, resources);
        return ValueTask.FromResult(new PortableOutcomeReceipt(
            outcome.Status,
            outcome.ValueShape,
            outcome.Value,
            outcome.ProviderEffectCount));
    }

    public ValueTask WithdrawAsync(PortableChannelId channel, CancellationToken cancellationToken)
    {
        _endpoint.Withdraw();
        return ValueTask.CompletedTask;
    }

    public ValueTask TerminateAsync(PortableChannelId channel, CancellationToken cancellationToken)
    {
        _endpoint.Terminate();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>The negotiated process realization over a bounded, length-delimited duplex.</summary>
public sealed class PortableProcessConversation(IPortableDuplex duplex, PortableLimits limits) : IPortableProviderConversation
{
    private readonly IPortableDuplex _duplex = duplex ?? throw new ArgumentNullException(nameof(duplex));
    private readonly PortableLimits _limits = limits ?? throw new ArgumentNullException(nameof(limits));

    public PortableRealization Realization => PortableRealization.NegotiatedProcess;

    public async ValueTask<PortableContractDocument> EstablishAsync(
        PortableContractDocument required,
        string hostEndpoint,
        PortableChannelId channel,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(required);
        await SendAsync(
            PortableEnvelope.Control(PortableEnvelopeKind.Establish, channel, PortableContractCodec.Encode(required)),
            cancellationToken).ConfigureAwait(false);
        var envelope = await ReceiveAsync(cancellationToken).ConfigureAwait(false);
        RequireKind(envelope, PortableEnvelopeKind.EstablishAccepted);
        return PortableEstablishAcceptedBody.Decode(envelope.Body).Contract;
    }

    public async ValueTask AwaitReadyAsync(PortableChannelId channel, CancellationToken cancellationToken)
    {
        var envelope = await ReceiveAsync(cancellationToken).ConfigureAwait(false);
        RequireKind(envelope, PortableEnvelopeKind.Ready);
    }

    public async ValueTask<PortableOutcomeReceipt> RequestAsync(
        PortableBindingPlan plan,
        PortableChannelId channel,
        PortableChannelRequestId request,
        PortableChannelExecutionId? execution,
        PortableOperationReference? operation,
        int? compactOperation,
        PortableShapeReference inputShape,
        PortableValue input,
        IReadOnlyList<PortableResource> resources,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(resources);
        var body = new PortableRequestBody(
            operation,
            compactOperation,
            inputShape,
            PortableValueCodec.Encode(plan.Catalog, inputShape, input),
            [.. resources.Select(PortableResourceCodec.Encode)]);
        await SendAsync(
            new PortableEnvelope(
                PortableContractDocument.SupportedContractVersion,
                PortableEnvelopeKind.Request,
                channel,
                request,
                execution,
                body.Encode()),
            cancellationToken).ConfigureAwait(false);

        var envelope = await ReceiveAsync(cancellationToken).ConfigureAwait(false);
        RequireKind(envelope, PortableEnvelopeKind.Outcome);
        if (envelope.RequestId != request || (execution is not null && envelope.ExecutionId != execution))
        {
            throw new PortableFaultException(
                PortableProtocolCategory.CorrelationMismatch,
                "correlation-mismatch",
                "The claimed Outcome does not correlate with the outstanding request.");
        }

        var outcome = PortableOutcomeBody.Decode(envelope.Body);

        // The Operation's declared Fragments govern the decode, exactly as they govern the direct
        // realization's validation. Decoding with an empty set would refuse a declared Fragment on
        // the wire that a direct call accepts.
        var declared = operation is { } reference
            ? plan.Operation(reference).RequiredFragments
            : ImmutableArray<PortableFragmentReference>.Empty;
        var value = PortableValueCodec.Decode(plan.Catalog, outcome.ValueShape, outcome.Value, declared);
        return new PortableOutcomeReceipt(outcome.Status, outcome.ValueShape, value, outcome.ProviderEffectCount);
    }

    public ValueTask WithdrawAsync(PortableChannelId channel, CancellationToken cancellationToken) =>
        SendAsync(PortableEnvelope.Control(PortableEnvelopeKind.Withdraw, channel), cancellationToken);

    public ValueTask TerminateAsync(PortableChannelId channel, CancellationToken cancellationToken) =>
        SendAsync(PortableEnvelope.Control(PortableEnvelopeKind.Terminate, channel), cancellationToken);

    public ValueTask DisposeAsync() => _duplex.DisposeAsync();

    private ValueTask SendAsync(PortableEnvelope envelope, CancellationToken cancellationToken) =>
        _duplex.SendAsync(PortableEnvelopeCodec.Encode(envelope), cancellationToken);

    private async ValueTask<PortableEnvelope> ReceiveAsync(CancellationToken cancellationToken)
    {
        var frame = await _duplex.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        return PortableEnvelopeCodec.Decode(frame, _limits);
    }

    /// <summary>
    /// Turns a peer's protocol error back into the same portable category on this side, so a local
    /// code never becomes the semantics.
    /// </summary>
    private static void RequireKind(PortableEnvelope envelope, PortableEnvelopeKind expected)
    {
        if (envelope.Kind == expected)
        {
            return;
        }

        if (envelope.Kind == PortableEnvelopeKind.ProtocolError)
        {
            var error = PortableProtocolErrorBody.Decode(envelope.Body);
            throw new PortableFaultException(
                error.Category,
                error.LocalCode,
                "The peer rejected the interaction.",
                PortableFailureDomain.RemoteEndpoint);
        }

        throw new PortableFaultException(
            PortableProtocolCategory.StateViolation,
            "unexpected-kind",
            $"A '{envelope.Kind.Token()}' frame arrived where '{expected.Token()}' was legal.");
    }
}

/// <summary>
/// The provider side of the negotiated process realization: it reads frames, applies the endpoint's
/// decisions, and answers with exactly one envelope per inbound frame.
/// </summary>
public static class PortableProviderProcessLoop
{
    public static async Task RunAsync(
        IPortableDuplex duplex,
        PortableProviderEndpoint endpoint,
        PortableLimits limits,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(duplex);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(limits);

        while (!cancellationToken.IsCancellationRequested)
        {
            PortableEnvelope envelope;
            try
            {
                var frame = await duplex.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                envelope = PortableEnvelopeCodec.Decode(frame, limits);
            }
            catch (PortableProcessFailureException)
            {
                // Process loss is observed, never signalled: the seam is gone, so no frame can go out.
                endpoint.Fail();
                return;
            }
            catch (PortableFaultException fault)
            {
                await ReportAsync(duplex, endpoint, PortableChannelId.New(), null, fault, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            try
            {
                var terminated = await HandleAsync(duplex, endpoint, envelope, limits, cancellationToken)
                    .ConfigureAwait(false);
                if (terminated)
                {
                    return;
                }
            }
            catch (PortableProcessFailureException)
            {
                endpoint.Fail();
                return;
            }
            catch (PortableFaultException fault)
            {
                await ReportAsync(duplex, endpoint, envelope.ChannelId, envelope.RequestId, fault, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }
        }
    }

    private static async ValueTask<bool> HandleAsync(
        IPortableDuplex duplex,
        PortableProviderEndpoint endpoint,
        PortableEnvelope envelope,
        PortableLimits limits,
        CancellationToken cancellationToken)
    {
        switch (envelope.Kind)
        {
            case PortableEnvelopeKind.Establish:
            {
                var required = PortableContractCodec.Decode(envelope.Body);
                var accepted = endpoint.Establish(required, "host");
                await SendAsync(
                    duplex,
                    PortableEnvelope.Control(PortableEnvelopeKind.EstablishAccepted, envelope.ChannelId, accepted.Encode()),
                    cancellationToken).ConfigureAwait(false);
                endpoint.SignalReady();
                await SendAsync(
                    duplex,
                    PortableEnvelope.Control(PortableEnvelopeKind.Ready, envelope.ChannelId),
                    cancellationToken).ConfigureAwait(false);
                return false;
            }
            case PortableEnvelopeKind.Request:
            {
                var outcome = HandleRequest(endpoint, envelope);
                var plan = endpoint.Plan!;
                var body = new PortableOutcomeBody(
                    outcome.Status,
                    outcome.ValueShape,
                    PortableValueCodec.Encode(plan.Catalog, outcome.ValueShape, outcome.Value),
                    outcome.ProviderEffectCount);
                await SendAsync(
                    duplex,
                    new PortableEnvelope(
                        PortableContractDocument.SupportedContractVersion,
                        PortableEnvelopeKind.Outcome,
                        envelope.ChannelId,
                        envelope.RequestId,
                        envelope.ExecutionId,
                        body.Encode()),
                    cancellationToken).ConfigureAwait(false);
                return false;
            }
            case PortableEnvelopeKind.Withdraw:
                endpoint.Withdraw();
                return false;
            case PortableEnvelopeKind.Terminate:
                endpoint.Terminate();
                return true;
            default:
                throw new PortableFaultException(
                    PortableProtocolCategory.StateViolation,
                    "unexpected-kind",
                    $"A '{envelope.Kind.Token()}' frame is not directed at a provider endpoint.");
        }
    }

    private static PortableProviderOutcome HandleRequest(PortableProviderEndpoint endpoint, PortableEnvelope envelope)
    {
        var body = PortableRequestBody.Decode(envelope.Body);
        var plan = endpoint.Plan ?? throw new PortableFaultException(
            PortableProtocolCategory.StateViolation,
            "unestablished",
            "A request arrived before any contract was established.");

        // The authority scan runs before the value is given a Shape, so authority-bearing content is
        // refused as an authority presentation rather than as an undeclared field.
        if (plan.TrustBoundaryCrossed)
        {
            PortableAuthorityVocabulary.RequireNoCapabilityContent(body.Input);
        }

        var declaration = endpoint.ResolveOperation(body.Operation, body.CompactOperation);
        var input = PortableValueCodec.Decode(
            plan.Catalog,
            body.InputShape,
            body.Input,
            declaration.RequiredFragments);
        var resources = body.Resources
            .Select(resource => PortableResourceCodec.Decode(
                resource,
                plan.ResourceFlavors,
                plan.AcceptedResourceHandles,
                plan.Limits))
            .ToImmutableArray();

        return endpoint.Request(
            envelope.RequestId!.Value,
            body.Operation,
            body.CompactOperation,
            body.InputShape,
            input,
            resources);
    }

    private static async ValueTask ReportAsync(
        IPortableDuplex duplex,
        PortableProviderEndpoint endpoint,
        PortableChannelId channel,
        PortableChannelRequestId? request,
        PortableFaultException fault,
        CancellationToken cancellationToken)
    {
        endpoint.Fail();
        var body = new PortableProtocolErrorBody(fault.Category, fault.LocalCode, PortableFailureDomain.RemoteEndpoint);
        try
        {
            await SendAsync(
                duplex,
                new PortableEnvelope(
                    PortableContractDocument.SupportedContractVersion,
                    PortableEnvelopeKind.ProtocolError,
                    channel,
                    request,
                    null,
                    body.Encode()),
                cancellationToken).ConfigureAwait(false);
        }
        catch (PortableProcessFailureException)
        {
            // The seam disappeared while reporting; the local observation already records the fault.
        }
    }

    private static ValueTask SendAsync(IPortableDuplex duplex, PortableEnvelope envelope, CancellationToken cancellationToken) =>
        duplex.SendAsync(PortableEnvelopeCodec.Encode(envelope), cancellationToken);
}
