using System.Collections.Immutable;
using System.Diagnostics;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>
/// The host's decision about one request before anything leaves the host.
/// </summary>
/// <remarks>
/// It exists as a separate observation because a denial decided here is frameless: there is no
/// later stage at which to report it, and reporting it as a rejected frame would misdescribe what
/// happened.
/// </remarks>
public sealed record PortableRequestDecision(
    PortableFrameDecision FrameDecision,
    PortableResultClass ResultClass,
    PortableAdmission Admission,
    ImmutableArray<string> MappingObligations,
    PortableCorrelationMapping Correlation,
    PortableObservation? DenialObservation)
{
    public bool MayEmit => FrameDecision == PortableFrameDecision.Emit;
}

/// <summary>
/// The host side of one established portable binding.
/// </summary>
/// <remarks>
/// The host evaluates its local authority boundary before any frame is emitted, so a denial starts
/// no provider and produces no frame in either realization. Everything after that point is the
/// realization's business, which is why the same host drives both.
/// </remarks>
public sealed class PortableBindingHost : IAsyncDisposable
{
    private readonly IPortableProviderConversation _conversation;
    private readonly PortableAuthorityGate _gate;
    private readonly PortableLifecycle _lifecycle;
    private readonly PortableObservationBuilder _observations;
    private readonly Stopwatch _clock;
    private readonly long _establishedAtElapsedMilliseconds;

    private PortableBindingHost(
        PortableBindingPlan plan,
        IPortableProviderConversation conversation,
        PortableChannelId channel,
        PortableLifecycle lifecycle,
        Stopwatch clock)
    {
        Plan = plan;
        ChannelId = channel;
        _conversation = conversation;
        _gate = new PortableAuthorityGate(plan.Contract.Authority);
        _lifecycle = lifecycle;
        _observations = new PortableObservationBuilder(plan);
        _clock = clock;
        _establishedAtElapsedMilliseconds = clock.ElapsedMilliseconds;
    }

    public PortableBindingPlan Plan { get; }

    public PortableChannelId ChannelId { get; }

    public PortableLifecycleState State => _lifecycle.State;

    /// <summary>
    /// Establishes one binding scope: negotiate, freeze the plan, and wait for readiness. No
    /// provider effect may occur before the readiness signal arrives.
    /// </summary>
    public static async ValueTask<PortableBindingHost> EstablishAsync(
        PortableContractDocument required,
        IPortableProviderConversation conversation,
        string hostEndpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(required);
        ArgumentNullException.ThrowIfNull(conversation);
        var clock = Stopwatch.StartNew();
        var channel = PortableChannelId.New();
        var lifecycle = new PortableLifecycle(required.Lifecycle.ReplayProtectionDeclared);

        lifecycle.Apply(PortableEnvelopeKind.Establish);
        var offered = await conversation
            .EstablishAsync(required, hostEndpoint, channel, cancellationToken)
            .ConfigureAwait(false);
        var plan = PortableNegotiation.Negotiate(
            required,
            offered,
            conversation.Realization,
            hostEndpoint,
            offered.Provider.ToString(),
            "the only endpoint offering the required contract; version 0.1 applies no selection policy");
        lifecycle.Apply(PortableEnvelopeKind.EstablishAccepted);

        await conversation.AwaitReadyAsync(channel, cancellationToken).ConfigureAwait(false);
        lifecycle.Apply(PortableEnvelopeKind.Ready);
        return new PortableBindingHost(plan, conversation, channel, lifecycle, clock);
    }

    /// <summary>Evaluates the local authority boundary and the mapping obligations of one request.</summary>
    public PortableRequestDecision PrepareRequest(
        PortableOperationReference operation,
        PortableShapeReference inputShape,
        PortableConstraint authority)
    {
        ArgumentNullException.ThrowIfNull(authority);
        var correlation = new PortableCorrelationMapping(
            ChannelId,
            PortableChannelRequestId.New(),
            PortableHostExecutionId.New());
        var admission = _gate.Evaluate(authority);
        if (!admission.MayProceed)
        {
            return new PortableRequestDecision(
                PortableFrameDecision.None,
                PortableResultClass.Denial,
                admission,
                [],
                correlation,
                _observations.Denial(correlation, admission.Decision, Timing(0), admission.Reason));
        }

        var declared = Plan.Operation(operation).InputShape;
        var obligations = inputShape == declared
            ? ImmutableArray<string>.Empty
            : [$"projected:{inputShape}->{declared}"];
        return new PortableRequestDecision(
            PortableFrameDecision.Emit,
            PortableResultClass.Request,
            admission,
            obligations,
            correlation,
            null);
    }

    /// <summary>Runs one complete interaction and reports its category-level result.</summary>
    public async ValueTask<PortableInteractionResult> InvokeAsync(
        PortableOperationReference operation,
        PortableShapeReference inputShape,
        PortableValue input,
        PortableConstraint authority,
        IReadOnlyList<PortableResource>? resources = null,
        PortableChannelExecutionId? execution = null,
        PortableRequestDecision? prepared = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(authority);
        var presented = resources ?? [];
        var decision = prepared ?? PrepareRequest(operation, inputShape, authority);
        var started = _clock.ElapsedMilliseconds;

        if (!decision.MayEmit)
        {
            return new PortableInteractionResult(
                PortableFrameDecision.None,
                PortableResultClass.Denial,
                null,
                null,
                null,
                decision.DenialObservation!);
        }

        var resourceObservations = presented
            .Select(resource => PortableResourceCodec.Observe(resource, _conversation.Realization))
            .ToImmutableArray();
        var copies = resourceObservations.Sum(resource => resource.Copies);

        try
        {
            _lifecycle.Apply(PortableEnvelopeKind.Request);
            _lifecycle.RecordRequest(decision.Correlation.RequestId);
            var receipt = await _conversation.RequestAsync(
                Plan,
                ChannelId,
                decision.Correlation.RequestId,
                execution,
                operation,
                null,
                inputShape,
                input,
                presented,
                cancellationToken).ConfigureAwait(false);
            _lifecycle.Apply(PortableEnvelopeKind.Outcome);

            var declaration = Plan.Operation(operation);
            var expected = receipt.Status == PortableOutcomeStatus.Succeeded
                ? declaration.ResultShape
                : declaration.DetailShape;
            if (receipt.ValueShape != expected)
            {
                throw PortableFaultException.InvalidPayload(
                    "outcome-shape",
                    $"The Outcome declares Shape {receipt.ValueShape} where {expected} was negotiated.");
            }

            PortableValueCodec.Validate(Plan.Catalog, expected, receipt.Value, declaration.RequiredFragments);

            var succeeded = receipt.Status == PortableOutcomeStatus.Succeeded;
            var observation = _observations.Build(
                succeeded ? PortableTerminalStatus.Succeeded : PortableTerminalStatus.Failed,
                PortableAuthorityDecision.Permitted,
                Plan.AuthorityDecisionPoint,
                decision.Correlation,
                succeeded ? null : PortableFailureDomain.RemoteProvider,
                receipt.ProviderEffectCount,
                copies,
                resourceObservations,
                decision.MappingObligations,
                false,
                Timing(_clock.ElapsedMilliseconds - started),
                null,
                null);
            return new PortableInteractionResult(
                PortableFrameDecision.Accept,
                succeeded ? PortableResultClass.OutcomeSucceeded : PortableResultClass.OutcomeFailed,
                null,
                null,
                receipt.Value,
                observation);
        }
        catch (PortableFaultException fault)
        {
            _lifecycle.Fail();
            return Rejected(fault, decision, resourceObservations, started);
        }
        catch (PortableProcessFailureException failure)
        {
            _lifecycle.Fail();
            return Lost(failure, decision, resourceObservations, started);
        }
    }

    public async ValueTask WithdrawAsync(CancellationToken cancellationToken = default)
    {
        _lifecycle.Apply(PortableEnvelopeKind.Withdraw);
        await _conversation.WithdrawAsync(ChannelId, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask TerminateAsync(CancellationToken cancellationToken = default)
    {
        _lifecycle.Apply(PortableEnvelopeKind.Terminate);
        await _conversation.TerminateAsync(ChannelId, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => _conversation.DisposeAsync();

    private PortableInteractionResult Rejected(
        PortableFaultException fault,
        PortableRequestDecision decision,
        ImmutableArray<PortableResourceObservation> resources,
        long started) =>
        new(
            PortableFrameDecision.Reject,
            PortableResultClass.ProtocolError,
            fault.Category,
            null,
            null,
            _observations.Build(
                PortableTerminalStatus.ProtocolError,
                PortableAuthorityDecision.Permitted,
                Plan.AuthorityDecisionPoint,
                decision.Correlation,
                fault.Domain,
                // A rejection never produces a partial provider effect.
                0,
                0,
                resources,
                decision.MappingObligations,
                false,
                Timing(_clock.ElapsedMilliseconds - started),
                fault.LocalCode,
                fault.Message));

    private PortableInteractionResult Lost(
        PortableProcessFailureException failure,
        PortableRequestDecision decision,
        ImmutableArray<PortableResourceObservation> resources,
        long started) =>
        new(
            PortableFrameDecision.None,
            PortableResultClass.ProcessFailure,
            null,
            failure.Category,
            null,
            _observations.Build(
                PortableTerminalStatus.ProcessFailure,
                PortableAuthorityDecision.Permitted,
                Plan.AuthorityDecisionPoint,
                decision.Correlation,
                failure.Domain,
                0,
                0,
                resources,
                decision.MappingObligations,
                failure.Category == PortableProcessCategory.TransportInterrupted,
                Timing(_clock.ElapsedMilliseconds - started),
                failure.Category.Token(),
                failure.Message));

    private PortableTiming Timing(long requestElapsed) =>
        new(_establishedAtElapsedMilliseconds, requestElapsed);
}
