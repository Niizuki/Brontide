using System.Collections.Immutable;

namespace Brontide.Reference.Core;

public enum OriginClass
{
    Unverified,
    Derived,
    Device,
    Human,
    Autonomous
}

public sealed record TemporalMark
{
    public TemporalMark(long milliseconds, string timeDomain, long? uncertaintyMilliseconds = null)
    {
        if (string.IsNullOrWhiteSpace(timeDomain))
        {
            throw new ArgumentException("A time domain is required.", nameof(timeDomain));
        }

        if (uncertaintyMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(uncertaintyMilliseconds));
        }

        Milliseconds = milliseconds;
        TimeDomain = timeDomain;
        UncertaintyMilliseconds = uncertaintyMilliseconds;
    }

    public long Milliseconds { get; }
    public string TimeDomain { get; }
    public long? UncertaintyMilliseconds { get; }
}

public sealed record InteractionContext(
    ActorReference Actor,
    OccurrenceReference Id,
    OccurrenceReference? Correlation = null,
    OccurrenceReference? Causation = null,
    OriginClass Origin = OriginClass.Unverified,
    TemporalMark? EmittedAt = null);

public sealed class ExecutionRecord
{
    private readonly ShapeValue? _input;

    internal ExecutionRecord(
        ExecutionId id,
        InteractionContext interaction,
        ActorReference initiator,
        ActorReference? requester,
        ActorReference authorityActor,
        OperationReference operation,
        ActorReference target,
        AuthorityPresentation authorityPresentation,
        ShapeValue? input)
    {
        Id = id;
        Interaction = interaction;
        Initiator = initiator;
        Requester = requester;
        AuthorityActor = authorityActor;
        Operation = operation;
        Target = target;
        AuthorityPresentation = authorityPresentation;
        _input = input;
    }

    public ExecutionId Id { get; }
    public InteractionContext Interaction { get; }
    public ActorReference Initiator { get; }
    public ActorReference? Requester { get; }
    public ActorReference AuthorityActor { get; }
    public OperationReference Operation { get; }
    public ActorReference Target { get; }
    public AuthorityPresentation AuthorityPresentation { get; }
    public bool HasInput => _input is not null;
    public ShapeValue Input => _input ?? throw new InvalidOperationException(
        "The protected input was not retained in this audit record.");

    internal ExecutionRecord WithoutInput() => new(
        Id,
        Interaction,
        Initiator,
        Requester,
        AuthorityActor,
        Operation,
        Target,
        AuthorityPresentation,
        null);
}

public enum OutcomeStatus
{
    Succeeded,
    Rejected,
    Failed,
    Completed,
    Cancelled,
    AuthorityWithdrawn
}

public sealed record Outcome(
    InteractionContext Interaction,
    TerminalReference TerminalFor,
    OutcomeStatus Status,
    ShapeValue? Result,
    ShapeContract? DetailsShape,
    ShapeValue? Details,
    string Message);

public sealed record DomainEvent(
    InteractionContext Interaction,
    EventReference Kind,
    ActorReference Subject,
    ShapeValue Assertion,
    TemporalMark? OccurredAt = null,
    bool IsReplay = false);

public delegate ValueTask<OperationEffect> OperationHandler(ExecutionContext context);

public sealed class OperationEffect
{
    private OperationEffect(
        bool succeeded,
        ShapeValue? result,
        ShapeContract? detailsShape,
        ShapeValue? details,
        string message)
    {
        Succeeded = succeeded;
        Result = result;
        DetailsShape = detailsShape;
        Details = details;
        Message = message;
    }

    public bool Succeeded { get; }
    public ShapeValue? Result { get; }
    public ShapeContract? DetailsShape { get; }
    public ShapeValue? Details { get; }
    public string Message { get; }

    public static OperationEffect Success(ShapeValue result, string message = "succeeded") =>
        new(true, result ?? throw new ArgumentNullException(nameof(result)), null, null, message);

    public static ValueTask<OperationEffect> SucceededAsync(ShapeValue result, string message = "succeeded") =>
        ValueTask.FromResult(Success(result, message));

    public static OperationEffect Failure(ShapeContract detailsShape, ShapeValue details, string message = "failed") =>
        new(false, null, detailsShape, details, message);

    public static ValueTask<OperationEffect> FailedAsync(
        ShapeContract detailsShape,
        ShapeValue details,
        string message = "failed") =>
        ValueTask.FromResult(Failure(detailsShape, details, message));
}

internal sealed record OperationDefinition(
    OperationReference Reference,
    ActorReference Target,
    ShapeContract Input,
    ShapeContract Output,
    string Semantics,
    OperationHandler Handler);

internal sealed record EventDefinition(
    EventReference Reference,
    ShapeContract Assertion,
    string Semantics);

public sealed class ExecutionContext
{
    private readonly AuthorityDomain _domain;
    private readonly List<DomainEvent> _events = [];
    private bool _active = true;

    internal ExecutionContext(
        AuthorityDomain domain,
        ExecutionRecord execution,
        ShapeValue input,
        ShapeValue presentedInput)
    {
        _domain = domain;
        Execution = execution;
        Input = input;
        PresentedInput = presentedInput;
    }

    public ExecutionRecord Execution { get; }
    public ShapeValue Input { get; }
    public ShapeValue PresentedInput { get; }
    public IReadOnlyList<DomainEvent> EmittedEvents => _events;

    public DomainEvent EmitEvent(
        EventReference kind,
        ShapeValue assertion,
        OriginClass origin = OriginClass.Unverified,
        Capability? originAuthority = null,
        TemporalMark? occurredAt = null)
    {
        EnsureActive();
        var emitted = _domain.EmitEvent(
            Execution.Target,
            kind,
            assertion,
            origin,
            originAuthority,
            new OccurrenceReference(Execution.Id.Value),
            occurredAt);
        _events.Add(emitted);
        return emitted;
    }

    /// <summary>
    /// Used by trusted provider machinery that binds an initiating Actor (for example an attached
    /// device) to the Event it causes. The Core origin gate still validates the presented grant.
    /// </summary>
    public DomainEvent EmitEventFromInitiator(
        EventReference kind,
        ShapeValue assertion,
        OriginClass origin = OriginClass.Unverified,
        Capability? originAuthority = null,
        TemporalMark? occurredAt = null)
    {
        EnsureActive();
        var emitted = _domain.EmitEventFromAuthorizedExecution(
            Execution,
            Execution.Initiator,
            kind,
            assertion,
            origin,
            originAuthority,
            occurredAt);
        _events.Add(emitted);
        return emitted;
    }

    public ActivityReference CreateActivity(CanonicalName kind)
    {
        EnsureActive();
        return _domain.CreateActivity(Execution.Target, kind);
    }

    public Outcome CompleteActivity(
        ActivityReference activity,
        OutcomeStatus status = OutcomeStatus.Completed,
        ShapeContract? detailsShape = null,
        ShapeValue? details = null,
        string message = "activity completed")
    {
        EnsureActive();
        return _domain.EmitActivityOutcome(Execution.Target, activity, status, detailsShape, details, message);
    }

    internal void Deactivate() => _active = false;

    private void EnsureActive()
    {
        if (!_active)
        {
            throw new InvalidOperationException(
                "Execution effects and derived occurrences may be created only while the Operation handler is active.");
        }
    }
}

public sealed record ExecutionResult(
    ExecutionRecord Execution,
    Outcome Outcome,
    ImmutableArray<ConstraintDecision> Decisions,
    ImmutableArray<DomainEvent> Events)
{
    public bool IsAuthorized => Outcome.Status != OutcomeStatus.Rejected;
}

public enum ProvenanceKind
{
    Genesis,
    Execution,
    Event,
    Outcome
}

public sealed class ProvenanceEntry
{
    private ProvenanceEntry(
        long sequence,
        ProvenanceKind kind,
        GenesisRecord? genesis = null,
        ExecutionRecord? execution = null,
        DomainEvent? @event = null,
        Outcome? outcome = null,
        IEnumerable<ConstraintDecision>? decisions = null,
        bool? authorized = null,
        string? message = null)
    {
        Sequence = sequence;
        Kind = kind;
        Genesis = genesis;
        Execution = execution;
        Event = @event;
        Outcome = outcome;
        Decisions = (decisions ?? []).ToImmutableArray();
        Authorized = authorized;
        Message = message;
    }

    public long Sequence { get; }
    public ProvenanceKind Kind { get; }
    public GenesisRecord? Genesis { get; }
    public ExecutionRecord? Execution { get; }
    public DomainEvent? Event { get; }
    public Outcome? Outcome { get; }
    public ImmutableArray<ConstraintDecision> Decisions { get; }
    public bool? Authorized { get; }
    public string? Message { get; }

    internal static ProvenanceEntry ForGenesis(long sequence, GenesisRecord genesis) =>
        new(sequence, ProvenanceKind.Genesis, genesis: genesis);

    internal static ProvenanceEntry ForExecution(
        long sequence,
        ExecutionRecord execution,
        IEnumerable<ConstraintDecision> decisions,
        bool authorized,
        string message) =>
        new(sequence, ProvenanceKind.Execution, execution: authorized ? execution : execution.WithoutInput(), decisions: decisions,
            authorized: authorized, message: message);

    internal static ProvenanceEntry ForEvent(long sequence, DomainEvent @event) =>
        new(sequence, ProvenanceKind.Event, @event: @event);

    internal static ProvenanceEntry ForOutcome(long sequence, Outcome outcome) =>
        new(sequence, ProvenanceKind.Outcome, outcome: outcome);
}

public sealed record GenesisRecord(
    InteractionContext Interaction,
    string Kind,
    string Reason,
    ImmutableArray<ActorReference> ActorsCreated,
    ImmutableArray<Capability> CapabilitiesCreated);

public sealed class BrontideDenialException(string message) : InvalidOperationException(message);
