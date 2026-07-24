using System.Collections.Immutable;
using Brontide.Reference.Core;

namespace Brontide.Reference.Experimental.Binding;

public sealed record ExperimentalBindingObservation(
    string Host,
    string SelectedProvider,
    string SelectionReason,
    ImmutableArray<string> RejectedAlternatives,
    WireReference Operation,
    WireReference InputShape,
    WireReference OutputShape,
    string Representation,
    ImmutableArray<string> CrossedBoundaries,
    int Copies,
    ImmutableArray<string> ReferencedResources,
    string HostAuthorityDecision,
    string AuthorityDecisionPoint,
    ImmutableArray<string> MappingObligations,
    ImmutableArray<string> AdapterObligations,
    int RetryCount,
    bool Interrupted,
    bool ProviderProcessFailure,
    string Fallback,
    string FailureDomain,
    string TerminalOutcome,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    BindingRequestId RequestId,
    BindingExecutionId BindingExecutionId,
    BindingOccurrenceId BindingOccurrenceId,
    ExecutionId HostExecutionId,
    string Requester,
    long? ProviderEffectCount);

public sealed record BoundExecutionResult(
    ExecutionResult Execution,
    ExperimentalBindingObservation Observation,
    ShapeValue? ForwardedInput);

public sealed class ReferenceCoolingBindingHost
{
    private readonly SemaphoreSlim _singleInvocation = new(1, 1);
    private readonly ProviderLaunch _launch;
    private readonly TimeProvider _clock;
    private readonly PortableManifest _requiredManifest;
    private InvocationContext? _current;
    private ExchangeFacts? _exchange;

    public ReferenceCoolingBindingHost(
        ProviderLaunch launch,
        TimeProvider clock,
        Func<PortableManifest, PortableManifest>? manifestTransform = null)
    {
        _launch = launch ?? throw new ArgumentNullException(nameof(launch));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _requiredManifest = (manifestTransform ?? (manifest => manifest))(
            InterchangeCoolingContract.CreateManifest("brontide-reference-host-requirement"));

        ActorReference target = null!;
        Domain = AuthorityDomain.Create("Brontide.Reference hosts foreign Cooling", clock, genesis =>
        {
            AuthorizedActor = genesis.Actor("ReferenceInterchangeRequester");
            DeniedActor = genesis.Actor("ReferenceInterchangeStranger");
            UnknownConstraintActor = genesis.Actor("ReferenceUnknownConstraintRequester");
            target = genesis.Actor("ForeignCoolingProxy");
            InterchangeCoolingContract.RegisterShapes(genesis, includeOptionalLocalFragment: true);
            genesis.Operation(
                InterchangeCoolingContract.Operation,
                target,
                ShapeContract.For(InterchangeCoolingContract.CommandShape, InterchangeCoolingContract.HostContext),
                ShapeContract.For(InterchangeCoolingContract.ResultShape),
                "Execute the neutral binary Cooling contract through an independently implemented process binding.",
                InvokeProviderAsync);
            AuthorizedCapability = genesis.Grant(
                AuthorizedActor,
                target,
                [InterchangeCoolingContract.Operation]);
            UnknownConstraintCapability = genesis.Grant(
                UnknownConstraintActor,
                target,
                [InterchangeCoolingContract.Operation],
                [new ValueConstraint(
                    CanonicalName.Parse("interchange.tests.unknown-constraint"),
                    ShapeValue.Text("must fail closed"))]);
        });
    }

    public AuthorityDomain Domain { get; }
    public ActorReference AuthorizedActor { get; private set; } = null!;
    public ActorReference DeniedActor { get; private set; } = null!;
    public ActorReference UnknownConstraintActor { get; private set; } = null!;
    public Capability AuthorizedCapability { get; private set; } = null!;
    public Capability UnknownConstraintCapability { get; private set; } = null!;
    public int ProviderStarts { get; private set; }

    public async ValueTask<BoundExecutionResult> ExecuteAsync(
        ActorReference actor,
        Capability capability,
        ShapeValue input,
        CancellationToken cancellationToken = default)
    {
        await _singleInvocation.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var started = _clock.GetUtcNow();
            var ids = new InvocationContext(
                BindingRequestId.New(),
                BindingExecutionId.New(),
                BindingOccurrenceId.New(),
                cancellationToken);
            _current = ids;
            _exchange = null;
            var execution = await Domain.ExecuteAsync(
                actor,
                InterchangeCoolingContract.Operation,
                capability,
                input).ConfigureAwait(false);
            var finished = _clock.GetUtcNow();
            var exchange = _exchange;
            var authority = execution.IsAuthorized ? "allowed" : "denied";
            var observation = new ExperimentalBindingObservation(
                "brontide-reference",
                exchange?.Provider ?? "not-activated",
                exchange?.SelectionReason ?? "host authority or Shape validation stopped binding before selection",
                [],
                new WireReference(InterchangeCoolingContract.Operation),
                new WireReference(InterchangeCoolingContract.CommandShape),
                new WireReference(InterchangeCoolingContract.ResultShape),
                "inline-tagged-json",
                exchange is null ? [] : ["process"],
                exchange is null ? 0 : 2,
                [],
                authority,
                "Brontide.Reference AuthorityDomain before foreign process activation",
                ["tagged ShapeValue to Brontide.Reference-native ShapeValue"],
                ["binary enabled maps to Brontide.Reference fan speed 100 or Fan.Stop in the Brontide.Reference provider"],
                0,
                exchange?.Interrupted ?? false,
                exchange?.ProviderProcessFailure ?? false,
                "none",
                exchange?.FailureDomain ?? (execution.IsAuthorized ? "none" : "host-authority"),
                execution.Outcome.Status.ToString(),
                started,
                finished,
                ids.Request,
                ids.Execution,
                ids.Occurrence,
                execution.Execution.Id,
                actor.DisplayName,
                exchange?.ProviderEffectCount);
            return new BoundExecutionResult(execution, observation, exchange?.ForwardedInput);
        }
        finally
        {
            _current = null;
            _exchange = null;
            _singleInvocation.Release();
        }
    }

    private async ValueTask<OperationEffect> InvokeProviderAsync(Brontide.Reference.Core.ExecutionContext context)
    {
        var invocation = _current ?? throw new InvalidOperationException("No binding invocation is active.");
        ProviderStarts++;
        try
        {
            var client = new ProcessBindingClient(_launch, TimeSpan.FromSeconds(10));
            var result = await client.InvokeAsync(
                _requiredManifest,
                invocation.Request,
                invocation.Execution,
                invocation.Occurrence,
                context.PresentedInput,
                invocation.CancellationToken).ConfigureAwait(false);
            _exchange = new ExchangeFacts(
                result.Provider,
                "exact component, Operation, Shape, Fragment, dependency, and protocol versions negotiated",
                result.ForwardedInput,
                result.ProviderEffectCount,
                false,
                false,
                "none");
            return result.Succeeded
                ? OperationEffect.Success(result.Value, "foreign Cooling provider succeeded")
                : OperationEffect.Failure(
                    ShapeContract.For(InterchangeCoolingContract.DetailsShape),
                    result.Value,
                    "foreign Cooling provider returned a declared failed Outcome");
        }
        catch (BoundaryNegotiationException exception)
        {
            _exchange = new ExchangeFacts(
                "not-activated",
                exception.Message,
                null,
                null,
                false,
                false,
                "binding-negotiation");
            return Failure("incompatible-manifest", exception.Message);
        }
        catch (BoundaryProtocolException exception)
        {
            _exchange = new ExchangeFacts(
                "unknown",
                "provider selected but returned an invalid protocol message",
                null,
                null,
                true,
                false,
                "binding-protocol");
            return Failure("protocol-failure", exception.Message);
        }
        catch (ProviderProcessException exception)
        {
            _exchange = new ExchangeFacts(
                "unknown",
                "provider process was selected and then failed",
                null,
                null,
                true,
                true,
                $"provider-process:{exception.Stage}");
            return Failure("provider-process-failure", exception.Message);
        }
    }

    private static OperationEffect Failure(string code, string message) =>
        OperationEffect.Failure(
            ShapeContract.For(InterchangeCoolingContract.DetailsShape),
            InterchangeCoolingContract.Details(code, message),
            message);

    private sealed record InvocationContext(
        BindingRequestId Request,
        BindingExecutionId Execution,
        BindingOccurrenceId Occurrence,
        CancellationToken CancellationToken);

    private sealed record ExchangeFacts(
        string Provider,
        string SelectionReason,
        ShapeValue? ForwardedInput,
        long? ProviderEffectCount,
        bool Interrupted,
        bool ProviderProcessFailure,
        string FailureDomain);
}
