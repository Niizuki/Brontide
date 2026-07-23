using System.Collections.Immutable;

namespace Brontide.Reference.Core;

/// <summary>The single authority and effect gate for one hosted Brontide authority domain.</summary>
public sealed class AuthorityDomain
{
    private readonly object _gate = new();
    private readonly Dictionary<OperationReference, OperationDefinition> _operations = [];
    private readonly Dictionary<EventReference, EventDefinition> _events = [];
    private readonly Dictionary<CanonicalName, ConstraintDefinition> _constraints = [];
    private readonly List<ActorReference> _actors = [];
    private readonly List<Capability> _capabilities = [];
    private readonly List<LivenessLease> _leases = [];
    private readonly List<GenesisRecord> _genesis = [];
    private readonly List<ProvenanceEntry> _provenance = [];
    private readonly Dictionary<ActivityReference, ActorReference> _activities = [];
    private readonly HashSet<ActivityReference> _terminalActivities = [];
    private long _sequence;
    private bool _genesisOccurrenceActive;

    private AuthorityDomain(string name, TimeProvider? timeProvider)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("An authority-domain name is required.", nameof(name))
            : name;
        TimeProvider = timeProvider;
        Id = Guid.NewGuid();
        Shapes = ShapeRegistry.CreateWithBuiltIns();
        RegisterStandardConstraints();
    }

    internal Guid Id { get; }
    public string Name { get; }
    public TimeProvider? TimeProvider { get; }
    public ShapeRegistry Shapes { get; }

    public IReadOnlyList<ActorReference> Actors
    {
        get { lock (_gate) { return _actors.ToArray(); } }
    }

    public IReadOnlyList<Capability> Capabilities
    {
        get { lock (_gate) { return _capabilities.ToArray(); } }
    }

    public IReadOnlyList<GenesisRecord> GenesisOccurrences
    {
        get { lock (_gate) { return _genesis.ToArray(); } }
    }

    public IReadOnlyList<ProvenanceEntry> Provenance
    {
        get { lock (_gate) { return _provenance.ToArray(); } }
    }

    public static AuthorityDomain Create(string name, Action<GenesisContext> primordial) =>
        Create(name, null, primordial);

    public static AuthorityDomain Create(
        string name,
        TimeProvider? timeProvider,
        Action<GenesisContext> primordial)
    {
        ArgumentNullException.ThrowIfNull(primordial);
        var domain = new AuthorityDomain(name, timeProvider);
        var context = new GenesisContext(domain);
        try
        {
            primordial(context);
        }
        finally
        {
            context.Deactivate();
        }

        return domain;
    }

    public async ValueTask<ExecutionResult> ExecuteAsync(
        ActorReference actor,
        OperationReference operation,
        Capability capability,
        ShapeValue input,
        OriginClass origin = OriginClass.Unverified)
    {
        EnsureRuntimeAccessAllowed();
        return await ExecuteInternalAsync(
            actor,
            null,
            operation,
            AuthorityPresentation.Direct(capability),
            input,
            origin).ConfigureAwait(false);
    }

    public async ValueTask<ExecutionResult> ExecuteOnBehalfAsync(
        ActorReference deputy,
        ActorReference requester,
        OperationReference operation,
        AuthorityPresentation authorityPresentation,
        ShapeValue input,
        OriginClass origin = OriginClass.Unverified)
    {
        EnsureRuntimeAccessAllowed();
        if (authorityPresentation.Kind == AuthorityPresentationKind.Direct)
        {
            throw new ArgumentException(
                "A deputy must forward request authority or deliberately present its own authority.",
                nameof(authorityPresentation));
        }

        return await ExecuteInternalAsync(
            deputy,
            requester,
            operation,
            authorityPresentation,
            input,
            origin).ConfigureAwait(false);
    }

    public DomainEvent EmitEvent(
        ActorReference emitter,
        EventReference kind,
        ShapeValue assertion,
        OriginClass origin = OriginClass.Unverified,
        Capability? originAuthority = null,
        OccurrenceReference? causation = null,
        TemporalMark? occurredAt = null) =>
        EmitEventCore(
            emitter,
            kind,
            assertion,
            origin,
            originAuthority,
            causation,
            occurredAt,
            authorityAlreadyEvaluated: false);

    internal DomainEvent EmitEventFromAuthorizedExecution(
        ExecutionRecord execution,
        ActorReference emitter,
        EventReference kind,
        ShapeValue assertion,
        OriginClass origin,
        Capability? originAuthority,
        TemporalMark? occurredAt)
    {
        ArgumentNullException.ThrowIfNull(execution);
        if (origin != OriginClass.Unverified &&
            (execution.Interaction.Origin != origin ||
             !ReferenceEquals(execution.Initiator, emitter) ||
             !ReferenceEquals(execution.AuthorityPresentation.Capability, originAuthority)))
        {
            throw new BrontideDenialException(
                "An Event may inherit asserted origin only from its already-authorised initiating Execution.");
        }

        return EmitEventCore(
            emitter,
            kind,
            assertion,
            origin,
            originAuthority,
            new OccurrenceReference(execution.Id.Value),
            occurredAt,
            authorityAlreadyEvaluated: true);
    }

    private DomainEvent EmitEventCore(
        ActorReference emitter,
        EventReference kind,
        ShapeValue assertion,
        OriginClass origin,
        Capability? originAuthority,
        OccurrenceReference? causation,
        TemporalMark? occurredAt,
        bool authorityAlreadyEvaluated)
    {
        EnsureRuntimeAccessAllowed();
        EnsureActor(emitter);
        EventDefinition definition;
        lock (_gate)
        {
            if (!_events.TryGetValue(kind, out definition!))
            {
                throw new BrontideDenialException($"Event {kind} is not recognised by domain {Name}.");
            }
        }

        var shape = Shapes.Project(assertion, definition.Assertion);
        if (!shape.IsValid)
        {
            throw new BrontideDenialException($"Event assertion denied: {shape.Message}");
        }

        EnsureOriginAuthority(emitter, origin, originAuthority, authorityAlreadyEvaluated);
        var emitted = new DomainEvent(
            new InteractionContext(
                emitter,
                OccurrenceReference.New(),
                Causation: causation,
                Origin: origin,
                EmittedAt: TrustedTemporalMark()),
            kind,
            emitter,
            shape.Value!,
            occurredAt);
        Append(ProvenanceKind.Event, @event: emitted);
        return emitted;
    }

    public Outcome EmitActivityOutcome(
        ActorReference responsible,
        ActivityReference activity,
        OutcomeStatus status = OutcomeStatus.Completed,
        ShapeContract? detailsShape = null,
        ShapeValue? details = null,
        string message = "activity completed")
    {
        EnsureRuntimeAccessAllowed();
        EnsureActor(responsible);
        lock (_gate)
        {
            if (!_activities.TryGetValue(activity, out var owner) || !ReferenceEquals(owner, responsible))
            {
                throw new InvalidOperationException("The Actor does not own the named activity.");
            }

            if (!_terminalActivities.Add(activity))
            {
                throw new InvalidOperationException("An activity may have only one terminal Outcome.");
            }
        }

        ShapeValue? projectedDetails = null;
        if (detailsShape is not null || details is not null)
        {
            if (detailsShape is null || details is null)
            {
                throw new InvalidOperationException("Activity failure details require both a Shape and value.");
            }

            var projection = Shapes.Project(details, detailsShape);
            if (!projection.IsValid)
            {
                throw new InvalidOperationException($"Activity details are invalid: {projection.Message}");
            }

            projectedDetails = projection.Value;
        }

        var outcome = new Outcome(
            new InteractionContext(
                responsible,
                OccurrenceReference.New(),
                Origin: OriginClass.Derived,
                EmittedAt: TrustedTemporalMark()),
            TerminalReference.For(activity),
            status,
            null,
            detailsShape,
            projectedDetails,
            message);
        Append(ProvenanceKind.Outcome, outcome: outcome);
        return outcome;
    }

    public GenesisRecord OccurGenesis(
        ActorReference policyActor,
        string kind,
        string reason,
        Action<GenesisContext> occurrence)
    {
        EnsureActor(policyActor);
        if (string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Genesis occurrences require a kind and attributable reason.");
        }

        ArgumentNullException.ThrowIfNull(occurrence);
        lock (_gate)
        {
            if (_genesisOccurrenceActive)
            {
                throw new InvalidOperationException("Genesis occurrences cannot be nested.");
            }

            var actorStart = _actors.Count;
            var capabilityStart = _capabilities.Count;
            var leaseStart = _leases.Count;
            var existingLeaseStates = _leases
                .Select(lease => (Lease: lease, State: lease.CaptureState()))
                .ToArray();
            var operationKeys = _operations.Keys.ToHashSet();
            var eventKeys = _events.Keys.ToHashSet();
            var constraintKeys = _constraints.Keys.ToHashSet();
            using var shapeTransaction = Shapes.BeginRegistrationTransaction();
            var context = new GenesisContext(this);
            _genesisOccurrenceActive = true;
            try
            {
                occurrence(context);

                var record = new GenesisRecord(
                    new InteractionContext(policyActor, OccurrenceReference.New(), EmittedAt: TrustedTemporalMark()),
                    kind,
                    reason,
                    _actors.Skip(actorStart).ToImmutableArray(),
                    _capabilities.Skip(capabilityStart).ToImmutableArray());
                _genesis.Add(record);
                Append(ProvenanceKind.Genesis, genesis: record);
                shapeTransaction.Commit();
                return record;
            }
            catch
            {
                _actors.RemoveRange(actorStart, _actors.Count - actorStart);
                _capabilities.RemoveRange(capabilityStart, _capabilities.Count - capabilityStart);
                _leases.RemoveRange(leaseStart, _leases.Count - leaseStart);
                foreach (var (lease, state) in existingLeaseStates)
                {
                    lease.RestoreState(state);
                }

                foreach (var key in _operations.Keys.Except(operationKeys).ToArray())
                {
                    _operations.Remove(key);
                }

                foreach (var key in _events.Keys.Except(eventKeys).ToArray())
                {
                    _events.Remove(key);
                }

                foreach (var key in _constraints.Keys.Except(constraintKeys).ToArray())
                {
                    _constraints.Remove(key);
                }

                throw;
            }
            finally
            {
                context.Deactivate();
                _genesisOccurrenceActive = false;
            }
        }
    }

    private async ValueTask<ExecutionResult> ExecuteInternalAsync(
        ActorReference initiator,
        ActorReference? requester,
        OperationReference operation,
        AuthorityPresentation presentation,
        ShapeValue input,
        OriginClass origin)
    {
        ArgumentNullException.ThrowIfNull(initiator);
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(input);

        var capability = presentation.Capability;
        var authorityActor = presentation.Kind == AuthorityPresentationKind.Forwarded
            ? requester ?? throw new InvalidOperationException("Forwarded authority requires a requester.")
            : initiator;
        var target = capability.Target;
        var id = ExecutionId.New();
        var execution = new ExecutionRecord(
            id,
            new InteractionContext(
                initiator,
                new OccurrenceReference(id.Value),
                Origin: origin,
                EmittedAt: TrustedTemporalMark()),
            initiator,
            requester,
            authorityActor,
            operation,
            target,
            presentation,
            input);
        var decisions = ImmutableArray.CreateBuilder<ConstraintDecision>();

        if (!ActorBelongs(initiator) || (requester is not null && !ActorBelongs(requester)))
        {
            return Reject(execution, decisions, "denied: Actor reference is not issued by this authority domain");
        }

        OperationDefinition definition;
        lock (_gate)
        {
            if (!_operations.TryGetValue(operation, out definition!))
            {
                return Reject(execution, decisions, $"denied: Operation {operation} is unrecognised by target");
            }
        }

        if (!CapabilityBelongs(capability))
        {
            return Reject(execution, decisions, "denied: Capability is not registered by this authority domain");
        }

        if (!ReferenceEquals(capability.Holder, authorityActor))
        {
            return Reject(execution, decisions, "denied: Capability does not designate the attributed Actor");
        }

        if (!ReferenceEquals(capability.Target, definition.Target))
        {
            return Reject(execution, decisions, "denied: Capability target differs from the Operation target");
        }

        if (!capability.RootOperations.Contains(operation))
        {
            return Reject(execution, decisions, $"denied: Capability does not permit Operation {operation}");
        }

        var projectedInput = Shapes.Project(input, definition.Input);
        if (!projectedInput.IsValid)
        {
            return Reject(execution, decisions, $"denied: invalid input Shape; {projectedInput.Message}");
        }

        var trustedNow = TimeProvider?.GetUtcNow();
        var evaluationContext = new ConstraintEvaluationContext(
            operation,
            initiator,
            requester,
            definition.Target,
            capability,
            projectedInput.Value!,
            origin,
            trustedNow);
        var originGrantSeen = false;

        foreach (var expression in capability.EffectiveConstraintExpressions())
        {
            var evaluation = ConstraintExpressionEvaluator.Evaluate(
                expression,
                constraint => EvaluateConstraintAtom(constraint, evaluationContext));
            originGrantSeen |= evaluation.SatisfiedConstraints.Contains(StandardConstraintNames.OriginGrant);
            var decision = ConstraintDecision.FromExpression(expression, evaluation);
            decisions.Add(decision);
            if (!decision.Allowed)
            {
                return Reject(execution, decisions, $"denied: {decision.Reason}");
            }
        }

        if (origin != OriginClass.Unverified && !originGrantSeen)
        {
            var decision = ConstraintDecision.Deny(
                StandardConstraintNames.OriginGrant,
                $"origin {origin} was asserted without an origin grant");
            decisions.Add(decision);
            return Reject(execution, decisions, $"denied: {decision.Reason}");
        }

        Append(
            ProvenanceKind.Execution,
            execution: execution,
            decisions: decisions,
            authorized: true,
            message: "authorised; effect dispatch began");

        var context = new ExecutionContext(this, execution, projectedInput.Value!, input);
        OperationEffect effect;
        try
        {
            effect = await definition.Handler(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            effect = InternalFailure($"Operation handler failed: {exception.Message}");
        }
        finally
        {
            context.Deactivate();
        }

        Outcome outcome;
        if (effect.Succeeded)
        {
            var result = Shapes.Project(effect.Result ?? ShapeValue.Unit, definition.Output);
            outcome = result.IsValid
                ? CreateOutcome(execution, definition.Target, OutcomeStatus.Succeeded, result.Value, null, null, effect.Message)
                : CreateInternalFailureOutcome(execution, definition.Target,
                    $"Operation returned an incompatible output Shape: {result.Message}");
        }
        else if (effect.DetailsShape is null || effect.Details is null)
        {
            outcome = CreateInternalFailureOutcome(execution, definition.Target, "Failure did not declare shaped details.");
        }
        else
        {
            var details = Shapes.Project(effect.Details, effect.DetailsShape);
            outcome = details.IsValid
                ? CreateOutcome(execution, definition.Target, OutcomeStatus.Failed, null,
                    effect.DetailsShape, details.Value, effect.Message)
                : CreateInternalFailureOutcome(execution, definition.Target,
                    $"Failure details are incompatible with their Shape: {details.Message}");
        }

        Append(ProvenanceKind.Outcome, outcome: outcome);
        return new ExecutionResult(execution, outcome, decisions.ToImmutable(), context.EmittedEvents.ToImmutableArray());
    }

    private ConstraintAtomEvaluation EvaluateConstraintAtom(
        Constraint constraint,
        ConstraintEvaluationContext evaluationContext)
    {
        ConstraintDefinition constraintDefinition;
        lock (_gate)
        {
            if (!_constraints.TryGetValue(constraint.Name, out constraintDefinition!))
            {
                return ConstraintAtomEvaluation.Unsupported(constraint.Name);
            }
        }

        var constraintShape = Shapes.Project(constraint.Value, constraintDefinition.ValueShape);
        if (!constraintShape.IsValid)
        {
            return ConstraintAtomEvaluation.InvalidValue();
        }

        try
        {
            var decision = constraintDefinition.Evaluator(constraint, evaluationContext);
            return decision.Allowed
                ? ConstraintAtomEvaluation.Satisfied(decision.Reason)
                : ConstraintAtomEvaluation.Unsatisfied(decision.Reason);
        }
        catch
        {
            return ConstraintAtomEvaluation.EvaluatorFailed();
        }
    }

    private ExecutionResult Reject(
        ExecutionRecord execution,
        ImmutableArray<ConstraintDecision>.Builder decisions,
        string reason)
    {
        Append(
            ProvenanceKind.Execution,
            execution: execution,
            decisions: decisions,
            authorized: false,
            message: reason);
        var details = ShapeValue.Record(BuiltInShapes.Details, ("message", ShapeValue.Text(reason)));
        var outcome = CreateOutcome(
            execution,
            execution.Target,
            OutcomeStatus.Rejected,
            null,
            ShapeContract.For(BuiltInShapes.Details),
            details,
            reason);
        Append(ProvenanceKind.Outcome, outcome: outcome);
        return new ExecutionResult(execution, outcome, decisions.ToImmutable(), []);
    }

    private static OperationEffect InternalFailure(string message) => OperationEffect.Failure(
        ShapeContract.For(BuiltInShapes.Details),
        ShapeValue.Record(BuiltInShapes.Details, ("message", ShapeValue.Text(message))),
        message);

    private Outcome CreateInternalFailureOutcome(
        ExecutionRecord execution,
        ActorReference target,
        string message)
    {
        var details = ShapeValue.Record(BuiltInShapes.Details, ("message", ShapeValue.Text(message)));
        return CreateOutcome(
            execution,
            target,
            OutcomeStatus.Failed,
            null,
            ShapeContract.For(BuiltInShapes.Details),
            details,
            message);
    }

    private Outcome CreateOutcome(
        ExecutionRecord execution,
        ActorReference responsible,
        OutcomeStatus status,
        ShapeValue? result,
        ShapeContract? detailsShape,
        ShapeValue? details,
        string message) =>
        new(
            new InteractionContext(
                responsible,
                OccurrenceReference.New(),
                Causation: new OccurrenceReference(execution.Id.Value),
                Origin: OriginClass.Derived,
                EmittedAt: TrustedTemporalMark()),
            TerminalReference.For(execution.Id),
            status,
            result,
            detailsShape,
            details,
            message);

    private void RegisterStandardConstraints()
    {
        _constraints.Add(
            StandardConstraintNames.PermittedOperations,
            new ConstraintDefinition(
                StandardConstraintNames.PermittedOperations,
                ShapeContract.For(BuiltInShapes.OperationSet),
                (constraint, context) => constraint is PermittedOperationsConstraint permitted
                    ? permitted.AllowedOperations.Contains(context.Operation)
                        ? ConstraintDecision.Allow(constraint.Name, $"Operation {context.Operation} is permitted")
                        : ConstraintDecision.Deny(constraint.Name, $"Operation {context.Operation} is outside the delegated set")
                    : ConstraintDecision.Deny(constraint.Name, "invalid permitted-operations Constraint representation")));

        _constraints.Add(
            StandardConstraintNames.WallClockValidity,
            new ConstraintDefinition(
                StandardConstraintNames.WallClockValidity,
                ShapeContract.For(BuiltInShapes.TimeWindow),
                (constraint, context) =>
                {
                    if (constraint is not WallClockValidityConstraint window)
                    {
                        return ConstraintDecision.Deny(constraint.Name, "invalid wall-clock Constraint representation");
                    }

                    if (context.TrustedNow is null)
                    {
                        return ConstraintDecision.Deny(constraint.Name, "target has no trusted clock; fail-closed");
                    }

                    return (window.NotBefore is null || context.TrustedNow >= window.NotBefore) &&
                           (window.NotAfter is null || context.TrustedNow < window.NotAfter)
                        ? ConstraintDecision.Allow(constraint.Name, "trusted time is within the validity window")
                        : ConstraintDecision.Deny(constraint.Name, "trusted time is outside the validity window");
                }));

        _constraints.Add(
            StandardConstraintNames.LivenessLease,
            new ConstraintDefinition(
                StandardConstraintNames.LivenessLease,
                ShapeContract.For(BuiltInShapes.Lease),
                (constraint, context) =>
                {
                    if (constraint is not LivenessLeaseConstraint lease)
                    {
                        return ConstraintDecision.Deny(constraint.Name, "invalid lease Constraint representation");
                    }

                    return context.TrustedNow is not null && lease.Lease.IsAlive(context.TrustedNow.Value)
                        ? ConstraintDecision.Allow(constraint.Name, "liveness lease is active")
                        : ConstraintDecision.Deny(constraint.Name, "liveness lease is expired or no trusted clock exists");
                }));

        _constraints.Add(
            StandardConstraintNames.OriginGrant,
            new ConstraintDefinition(
                StandardConstraintNames.OriginGrant,
                ShapeContract.For(BuiltInShapes.OriginClass),
                (constraint, context) =>
                {
                    if (constraint is not OriginGrantConstraint grant)
                    {
                        return ConstraintDecision.Deny(constraint.Name, "invalid origin-grant representation");
                    }

                    if (context.RequestedOrigin == OriginClass.Unverified)
                    {
                        return ConstraintDecision.Allow(constraint.Name, "no origin class was asserted");
                    }

                    if (context.Capability.IsPrimordial && context.RequestedOrigin == grant.GrantedClass)
                    {
                        return ConstraintDecision.Allow(constraint.Name, $"primordial grant vouches {grant.GrantedClass}");
                    }

                    if (!context.Capability.IsPrimordial && context.RequestedOrigin == OriginClass.Derived)
                    {
                        return ConstraintDecision.Allow(constraint.Name, "delegated origin is capped at Derived");
                    }

                    return ConstraintDecision.Deny(
                        constraint.Name,
                        $"origin {context.RequestedOrigin} exceeds the Capability's origin ceiling");
                }));
    }

    internal ActivityReference CreateActivity(ActorReference owner, CanonicalName kind)
    {
        EnsureRuntimeAccessAllowed();
        EnsureActor(owner);
        var activity = ActivityReference.New(kind);
        lock (_gate)
        {
            _activities.Add(activity, owner);
        }

        return activity;
    }

    private void EnsureOriginAuthority(
        ActorReference emitter,
        OriginClass origin,
        Capability? originAuthority,
        bool authorityAlreadyEvaluated)
    {
        if (origin == OriginClass.Unverified)
        {
            return;
        }

        if (originAuthority is null || !CapabilityBelongs(originAuthority) ||
            !ReferenceEquals(originAuthority.Holder, emitter))
        {
            throw new BrontideDenialException($"Origin {origin} requires a Capability held by the emitter.");
        }

        var constraints = originAuthority.EffectiveConstraints().ToArray();
        if (constraints.Any(constraint =>
                constraint.Name == StandardConstraintNames.OriginGrant && constraint is not OriginGrantConstraint))
        {
            throw new BrontideDenialException("Origin authority has an invalid origin-grant representation.");
        }

        var grants = constraints.OfType<OriginGrantConstraint>().ToArray();
        var allowed = grants.Length > 0 && grants.All(grant =>
            (originAuthority.IsPrimordial && origin == grant.GrantedClass) ||
            (!originAuthority.IsPrimordial && origin == OriginClass.Derived));
        if (!allowed)
        {
            throw new BrontideDenialException($"Origin {origin} exceeds the Capability's origin ceiling.");
        }

        if (authorityAlreadyEvaluated)
        {
            return;
        }

        var trustedNow = TimeProvider?.GetUtcNow();
        foreach (var constraint in constraints)
        {
            switch (constraint)
            {
                case OriginGrantConstraint:
                    break;
                case WallClockValidityConstraint window when trustedNow is null:
                    throw new BrontideDenialException(
                        "Origin authority denied: target has no trusted clock; fail-closed.");
                case WallClockValidityConstraint window when
                    (window.NotBefore is not null && trustedNow < window.NotBefore) ||
                    (window.NotAfter is not null && trustedNow >= window.NotAfter):
                    throw new BrontideDenialException(
                        "Origin authority denied: trusted time is outside the validity window.");
                case WallClockValidityConstraint:
                    break;
                case LivenessLeaseConstraint lease when
                    trustedNow is null || !lease.Lease.IsAlive(trustedNow.Value):
                    throw new BrontideDenialException(
                        "Origin authority denied: liveness lease is expired or no trusted clock exists.");
                case LivenessLeaseConstraint:
                    break;
                default:
                    throw new BrontideDenialException(
                        $"Origin authority denied: constraint '{constraint.Name}' cannot be evaluated " +
                        "outside its authorised Execution context; fail-closed.");
            }
        }
    }

    private TemporalMark? TrustedTemporalMark()
    {
        if (TimeProvider is null)
        {
            return null;
        }

        return new TemporalMark(
            TimeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
            $"Brontide:{Name}.TrustedClock");
    }

    private void Append(
        ProvenanceKind kind,
        GenesisRecord? genesis = null,
        ExecutionRecord? execution = null,
        DomainEvent? @event = null,
        Outcome? outcome = null,
        IEnumerable<ConstraintDecision>? decisions = null,
        bool? authorized = null,
        string? message = null)
    {
        lock (_gate)
        {
            var sequence = ++_sequence;
            _provenance.Add(kind switch
            {
                ProvenanceKind.Genesis => ProvenanceEntry.ForGenesis(sequence, genesis!),
                ProvenanceKind.Execution => ProvenanceEntry.ForExecution(
                    sequence, execution!, decisions ?? [], authorized ?? false, message ?? string.Empty),
                ProvenanceKind.Event => ProvenanceEntry.ForEvent(sequence, @event!),
                ProvenanceKind.Outcome => ProvenanceEntry.ForOutcome(sequence, outcome!),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            });
        }
    }

    private ActorReference IssueActor(string displayName)
    {
        var actor = new ActorReference(Id, displayName);
        lock (_gate)
        {
            _actors.Add(actor);
        }

        return actor;
    }

    private Capability IssueCapability(
        ActorReference holder,
        ActorReference target,
        IEnumerable<OperationReference> operations,
        IEnumerable<ConstraintExpression> constraints,
        bool delegable)
    {
        EnsureActor(holder);
        EnsureActor(target);
        var operationSet = operations.ToImmutableHashSet();
        var constraintSet = constraints.ToImmutableArray();
        if (operationSet.Count == 0)
        {
            throw new ArgumentException("A primordial Capability must permit at least one Operation.", nameof(operations));
        }

        lock (_gate)
        {
            EnsureLeasesBelong(constraintSet);
            foreach (var operation in operationSet)
            {
                if (!_operations.TryGetValue(operation, out var definition))
                {
                    throw new InvalidOperationException($"Cannot grant unrecognised Operation {operation}.");
                }

                if (!ReferenceEquals(definition.Target, target))
                {
                    throw new InvalidOperationException($"Operation {operation} is registered at a different target.");
                }
            }
        }

        var capability = new Capability(
            Id,
            holder,
            target,
            operationSet,
            null,
            constraintSet,
            delegable,
            RegisterDerivedCapability);
        lock (_gate)
        {
            _capabilities.Add(capability);
        }

        return capability;
    }

    private void RegisterDerivedCapability(Capability capability)
    {
        lock (_gate)
        {
            if (capability.DomainId != Id ||
                capability.Parent is null ||
                !_capabilities.Contains(capability.Parent) ||
                !_actors.Contains(capability.Holder) ||
                !_actors.Contains(capability.Target))
            {
                throw new InvalidOperationException(
                    "A Capability can be delegated only from registered authority to registered Actors.");
            }

            EnsureLeasesBelong(capability.AddedConstraintExpressions);
            _capabilities.Add(capability);
        }
    }

    private LivenessLease IssueLease(ActorReference grantor, TimeSpan duration)
    {
        EnsureActor(grantor);
        var lease = new LivenessLease(grantor, duration, TimeProvider, _gate);
        lock (_gate)
        {
            _leases.Add(lease);
        }

        return lease;
    }

    private void RegisterOperation(
        OperationReference reference,
        ActorReference target,
        ShapeContract input,
        ShapeContract output,
        string semantics,
        OperationHandler handler)
    {
        EnsureActor(target);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(handler);
        if (!Shapes.Recognizes(input) || !Shapes.Recognizes(output))
        {
            throw new InvalidOperationException(
                $"Operation {reference} requires recognised input and output Shapes and Fragments.");
        }

        lock (_gate)
        {
            _operations.Add(reference, new OperationDefinition(reference, target, input, output, semantics, handler));
        }
    }

    private void RegisterEvent(
        EventReference reference,
        ShapeContract assertion,
        string semantics)
    {
        if (!Shapes.Recognizes(assertion))
        {
            throw new InvalidOperationException($"Event {reference} requires a recognised assertion Shape.");
        }

        lock (_gate)
        {
            _events.Add(reference, new EventDefinition(reference, assertion, semantics));
        }
    }

    private void RegisterConstraint(ConstraintDefinition definition)
    {
        if (!Shapes.Recognizes(definition.ValueShape))
        {
            throw new InvalidOperationException($"Constraint {definition.Name} requires a recognised value Shape.");
        }

        lock (_gate)
        {
            _constraints.Add(definition.Name, definition);
        }
    }

    private bool ActorBelongs(ActorReference actor)
    {
        lock (_gate)
        {
            return actor.DomainId == Id && _actors.Contains(actor);
        }
    }

    private bool CapabilityBelongs(Capability capability)
    {
        lock (_gate)
        {
            return capability.DomainId == Id && _capabilities.Contains(capability);
        }
    }

    private void EnsureLeasesBelong(IEnumerable<ConstraintExpression> constraints)
    {
        foreach (var leaseConstraint in constraints
                     .SelectMany(expression => ConstraintExpressionEvaluator.AtomicConstraints(expression))
                     .OfType<LivenessLeaseConstraint>())
        {
            if (!_leases.Contains(leaseConstraint.Lease) || !ActorBelongs(leaseConstraint.Lease.Grantor))
            {
                throw new InvalidOperationException(
                    "A liveness-lease Constraint must use a lease registered by this authority domain.");
            }
        }
    }

    private void EnsureRuntimeAccessAllowed()
    {
        lock (_gate)
        {
            if (_genesisOccurrenceActive)
            {
                throw new InvalidOperationException(
                    "Runtime effects cannot occur inside an active Genesis occurrence.");
            }
        }
    }

    private void EnsureActor(ActorReference actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (!ActorBelongs(actor))
        {
            throw new InvalidOperationException("Actor reference belongs to another authority domain.");
        }
    }

    public sealed class GenesisContext
    {
        private readonly AuthorityDomain _domain;
        private bool _active = true;

        internal GenesisContext(AuthorityDomain domain) => _domain = domain;

        public ActorReference Actor(string displayName)
        {
            EnsureActive();
            return _domain.IssueActor(displayName);
        }

        public void Shape(ShapeDefinition definition)
        {
            EnsureActive();
            _domain.Shapes.Register(definition);
        }

        public void Shape(DeclaredFragmentDefinition definition)
        {
            EnsureActive();
            _domain.Shapes.Register(definition);
        }

        public void Constraint(
            CanonicalName name,
            ShapeContract valueShape,
            ConstraintEvaluator evaluator)
        {
            EnsureActive();
            _domain.RegisterConstraint(new ConstraintDefinition(name, valueShape, evaluator));
        }

        public void Operation(
            OperationReference reference,
            ActorReference target,
            ShapeContract input,
            ShapeContract output,
            string semantics,
            OperationHandler handler)
        {
            EnsureActive();
            _domain.RegisterOperation(reference, target, input, output, semantics, handler);
        }

        public void Event(EventReference reference, ShapeContract assertion, string semantics)
        {
            EnsureActive();
            _domain.RegisterEvent(reference, assertion, semantics);
        }

        public Capability Grant(
            ActorReference holder,
            ActorReference target,
            IEnumerable<OperationReference> operations,
            IEnumerable<Constraint>? constraints = null,
            bool delegable = true)
        {
            EnsureActive();
            return _domain.IssueCapability(holder, target, operations, constraints ?? [], delegable);
        }

        public Capability GrantExpressions(
            ActorReference holder,
            ActorReference target,
            IEnumerable<OperationReference> operations,
            IEnumerable<ConstraintExpression> expressions,
            bool delegable = true)
        {
            EnsureActive();
            ArgumentNullException.ThrowIfNull(expressions);
            return _domain.IssueCapability(holder, target, operations, expressions, delegable);
        }

        public LivenessLease Lease(ActorReference grantor, TimeSpan duration)
        {
            EnsureActive();
            return _domain.IssueLease(grantor, duration);
        }

        internal void Deactivate() => _active = false;

        private void EnsureActive()
        {
            if (!_active)
            {
                throw new InvalidOperationException(
                    "Genesis declarations are valid only during domain construction or an attributable Genesis occurrence.");
            }
        }
    }
}
