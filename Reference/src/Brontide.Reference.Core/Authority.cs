using System.Collections.Immutable;

namespace Brontide.Reference.Core;

public static class StandardConstraintNames
{
    public static readonly CanonicalName PermittedOperations = CanonicalName.Parse("Brontide:PermittedOperations");
    public static readonly CanonicalName WallClockValidity = CanonicalName.Parse("Brontide:WallClockValidity");
    public static readonly CanonicalName LivenessLease = CanonicalName.Parse("Brontide:LivenessLease");
    public static readonly CanonicalName OriginGrant = CanonicalName.Parse("Brontide:OriginGrant");
}

public abstract record Constraint(CanonicalName Name, ShapeValue Value);

public sealed record ValueConstraint : Constraint
{
    public ValueConstraint(CanonicalName name, ShapeValue value)
        : base(name, value ?? throw new ArgumentNullException(nameof(value))) { }
}

public sealed record PermittedOperationsConstraint : Constraint
{
    public PermittedOperationsConstraint(params OperationReference[] allowedOperations)
        : base(
            StandardConstraintNames.PermittedOperations,
            ShapeValue.Sequence(
                BuiltInShapes.OperationSet,
                allowedOperations.Select(operation => ShapeValue.Text(operation.ToString())).ToArray()))
    {
        if (allowedOperations.Length == 0)
        {
            throw new ArgumentException("At least one permitted Operation is required.", nameof(allowedOperations));
        }

        AllowedOperations = allowedOperations.ToImmutableHashSet();
    }

    public ImmutableHashSet<OperationReference> AllowedOperations { get; }
}

public sealed record WallClockValidityConstraint : Constraint
{
    public WallClockValidityConstraint(DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null)
        : base(StandardConstraintNames.WallClockValidity, CreateValue(notBefore, notAfter))
    {
        if (notBefore is null && notAfter is null)
        {
            throw new ArgumentException("A wall-clock validity window must have at least one bound.");
        }

        if (notBefore > notAfter)
        {
            throw new ArgumentException("The not-before bound cannot be after not-after.");
        }

        NotBefore = notBefore;
        NotAfter = notAfter;
    }

    public DateTimeOffset? NotBefore { get; }
    public DateTimeOffset? NotAfter { get; }

    private static ShapeValue CreateValue(DateTimeOffset? notBefore, DateTimeOffset? notAfter)
    {
        var fields = new List<(string Name, ShapeValue Value)>();
        if (notBefore is not null)
        {
            fields.Add(("not-before", ShapeValue.Text(notBefore.Value.ToUniversalTime().ToString("O"))));
        }

        if (notAfter is not null)
        {
            fields.Add(("not-after", ShapeValue.Text(notAfter.Value.ToUniversalTime().ToString("O"))));
        }

        return ShapeValue.Record(BuiltInShapes.TimeWindow, fields.ToArray());
    }
}

public sealed record LivenessLeaseConstraint : Constraint
{
    public LivenessLeaseConstraint(LivenessLease lease)
        : base(
            StandardConstraintNames.LivenessLease,
            ShapeValue.Scalar(BuiltInShapes.Lease, (lease ?? throw new ArgumentNullException(nameof(lease))).Id))
    {
        Lease = lease;
    }

    public LivenessLease Lease { get; }
}

public sealed record OriginGrantConstraint : Constraint
{
    public OriginGrantConstraint(OriginClass grantedClass)
        : base(StandardConstraintNames.OriginGrant, ShapeValue.Scalar(BuiltInShapes.OriginClass, grantedClass.ToString()))
    {
        if (grantedClass is OriginClass.Unverified or OriginClass.Derived)
        {
            throw new ArgumentException("Genesis-grade origin grants must name a vouched source class.", nameof(grantedClass));
        }

        GrantedClass = grantedClass;
    }

    public OriginClass GrantedClass { get; }
}

/// <summary>A renewable, grantor-scoped mortality token evaluated against the domain's trusted clock.</summary>
public sealed class LivenessLease
{
    private readonly object _gate = new();
    private readonly TimeProvider? _timeProvider;
    private DateTimeOffset? _expiresAt;

    internal LivenessLease(ActorReference grantor, TimeSpan duration, TimeProvider? timeProvider)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
        Grantor = grantor;
        Duration = duration;
        _timeProvider = timeProvider;
        _expiresAt = timeProvider?.GetUtcNow().Add(duration);
        Id = Guid.NewGuid().ToString("N");
    }

    public string Id { get; }
    public ActorReference Grantor { get; }
    public TimeSpan Duration { get; }

    public DateTimeOffset? ExpiresAt
    {
        get { lock (_gate) { return _expiresAt; } }
    }

    public bool Renew(ActorReference by)
    {
        if (!ReferenceEquals(by, Grantor))
        {
            throw new UnauthorizedAccessException("Only the lease grantor may renew it.");
        }

        lock (_gate)
        {
            if (_timeProvider is null)
            {
                return false;
            }

            var now = _timeProvider.GetUtcNow();
            if (_expiresAt is null || now >= _expiresAt.Value)
            {
                return false;
            }

            _expiresAt = now.Add(Duration);
            return true;
        }
    }

    internal bool IsAlive(DateTimeOffset trustedNow)
    {
        lock (_gate)
        {
            return _expiresAt is not null && trustedNow < _expiresAt.Value;
        }
    }
}

public sealed record ConstraintDecision(bool Allowed, CanonicalName ConstraintName, string Reason)
{
    public static ConstraintDecision Allow(CanonicalName name, string reason) => new(true, name, reason);
    public static ConstraintDecision Deny(CanonicalName name, string reason) => new(false, name, reason);
}

public delegate ConstraintDecision ConstraintEvaluator(Constraint constraint, ConstraintEvaluationContext context);

public sealed record ConstraintDefinition(
    CanonicalName Name,
    ShapeContract ValueShape,
    ConstraintEvaluator Evaluator);

public sealed class ConstraintEvaluationContext
{
    internal ConstraintEvaluationContext(
        OperationReference operation,
        ActorReference initiator,
        ActorReference? requester,
        ActorReference target,
        Capability capability,
        ShapeValue input,
        OriginClass requestedOrigin,
        DateTimeOffset? trustedNow)
    {
        Operation = operation;
        Initiator = initiator;
        Requester = requester;
        Target = target;
        Capability = capability;
        Input = input;
        RequestedOrigin = requestedOrigin;
        TrustedNow = trustedNow;
    }

    public OperationReference Operation { get; }
    public ActorReference Initiator { get; }
    public ActorReference? Requester { get; }
    public ActorReference Target { get; }
    public Capability Capability { get; }
    public ShapeValue Input { get; }
    public OriginClass RequestedOrigin { get; }
    public DateTimeOffset? TrustedNow { get; }
}

public enum AuthorityPresentationKind
{
    Direct,
    Forwarded,
    OwnAuthority
}

public sealed class AuthorityPresentation
{
    private AuthorityPresentation(Capability capability, AuthorityPresentationKind kind, string? reason)
    {
        Capability = capability;
        Kind = kind;
        Reason = reason;
    }

    public Capability Capability { get; }
    public AuthorityPresentationKind Kind { get; }
    public string? Reason { get; }

    public static AuthorityPresentation Forward(Capability capability) =>
        new(capability ?? throw new ArgumentNullException(nameof(capability)), AuthorityPresentationKind.Forwarded, null);

    internal static AuthorityPresentation Direct(Capability capability) =>
        new(capability, AuthorityPresentationKind.Direct, null);

    internal static AuthorityPresentation Own(Capability capability, string reason) =>
        new(capability, AuthorityPresentationKind.OwnAuthority, reason);
}

/// <summary>An immutable grant. Derivation can only change the holder and append Constraints.</summary>
public sealed class Capability
{
    private readonly Action<Capability> _derivedCallback;

    internal Capability(
        Guid domainId,
        ActorReference holder,
        ActorReference target,
        ImmutableHashSet<OperationReference> rootOperations,
        Capability? parent,
        IEnumerable<Constraint> addedConstraints,
        bool delegationAllowed,
        Action<Capability> derivedCallback)
    {
        DomainId = domainId;
        Holder = holder;
        Target = target;
        RootOperations = rootOperations;
        Parent = parent;
        AddedConstraints = addedConstraints.ToImmutableArray();
        DelegationAllowed = delegationAllowed;
        _derivedCallback = derivedCallback;
        Id = Guid.NewGuid();
    }

    internal Guid DomainId { get; }
    public Guid Id { get; }
    public ActorReference Holder { get; }
    public ActorReference Target { get; }
    public ImmutableHashSet<OperationReference> RootOperations { get; }
    public Capability? Parent { get; }
    public ImmutableArray<Constraint> AddedConstraints { get; }
    public bool DelegationAllowed { get; }
    public bool IsPrimordial => Parent is null;

    public Capability Delegate(ActorReference newHolder, params Constraint[] added)
    {
        ArgumentNullException.ThrowIfNull(newHolder);
        ArgumentNullException.ThrowIfNull(added);
        if (!DelegationAllowed)
        {
            throw new InvalidOperationException("This Capability does not permit further Delegation.");
        }

        if (newHolder.DomainId != DomainId)
        {
            throw new InvalidOperationException("Cross-domain Delegation is not part of Brontide Base.");
        }

        if (added.Any(constraint => constraint.Name == StandardConstraintNames.OriginGrant))
        {
            throw new InvalidOperationException("Origin grants cannot be introduced through Delegation.");
        }

        var derived = new Capability(
            DomainId,
            newHolder,
            Target,
            RootOperations,
            this,
            added,
            DelegationAllowed,
            _derivedCallback);
        _derivedCallback(derived);
        return derived;
    }

    public AuthorityPresentation AsOwnAuthority(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Using deputy authority requires an attributable reason.", nameof(reason));
        }

        return AuthorityPresentation.Own(this, reason);
    }

    public ImmutableArray<Capability> DerivationChain()
    {
        var chain = new Stack<Capability>();
        for (var current = this; current is not null; current = current.Parent)
        {
            chain.Push(current);
        }

        return chain.ToImmutableArray();
    }

    internal IEnumerable<Constraint> EffectiveConstraints() =>
        DerivationChain().SelectMany(capability => capability.AddedConstraints);

    public override string ToString() => $"Capability {Id:N} for {Holder}";
}
