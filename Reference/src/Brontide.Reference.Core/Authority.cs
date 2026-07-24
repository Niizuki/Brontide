using System.Collections.Immutable;

namespace Brontide.Reference.Core;

public static class StandardConstraintNames
{
    public static readonly CanonicalName PermittedOperations = CanonicalName.Parse("Brontide:PermittedOperations");
    public static readonly CanonicalName WallClockValidity = CanonicalName.Parse("Brontide:WallClockValidity");
    public static readonly CanonicalName LivenessLease = CanonicalName.Parse("Brontide:LivenessLease");
    public static readonly CanonicalName OriginGrant = CanonicalName.Parse("Brontide:OriginGrant");
    public static readonly CanonicalName AllOf = CanonicalName.Parse("Brontide:Constraint.AllOf");
    public static readonly CanonicalName AnyOf = CanonicalName.Parse("Brontide:Constraint.AnyOf");
    public static readonly CanonicalName Not = CanonicalName.Parse("Brontide:Constraint.Not");
}

public abstract record ConstraintExpression(CanonicalName DiagnosticName);

public abstract record Constraint(CanonicalName Name, ShapeValue Value) : ConstraintExpression(Name);

public sealed record AllOfConstraintExpression : ConstraintExpression
{
    public AllOfConstraintExpression(params ConstraintExpression[] operands)
        : base(StandardConstraintNames.AllOf)
    {
        ArgumentNullException.ThrowIfNull(operands);
        if (operands.Length == 0 || operands.Any(operand => operand is null))
        {
            throw new ArgumentException("AllOf requires one or more non-null operands.", nameof(operands));
        }

        Operands = operands.ToImmutableArray();
    }

    public ImmutableArray<ConstraintExpression> Operands { get; }
}

public sealed record AnyOfConstraintExpression : ConstraintExpression
{
    public AnyOfConstraintExpression(params ConstraintExpression[] operands)
        : base(StandardConstraintNames.AnyOf)
    {
        ArgumentNullException.ThrowIfNull(operands);
        if (operands.Length == 0 || operands.Any(operand => operand is null))
        {
            throw new ArgumentException("AnyOf requires one or more non-null operands.", nameof(operands));
        }

        Operands = operands.ToImmutableArray();
    }

    public ImmutableArray<ConstraintExpression> Operands { get; }
}

public sealed record NotConstraintExpression : ConstraintExpression
{
    public NotConstraintExpression(ConstraintExpression operand)
        : base(StandardConstraintNames.Not) =>
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));

    public ConstraintExpression Operand { get; }
}

public enum ConstraintEvaluationOutcome
{
    Satisfied,
    Unsatisfied,
    Indeterminate
}

public enum ConstraintDiagnosticCategory
{
    Satisfied,
    Unsatisfied,
    UnsupportedConstraint,
    InvalidConstraintValue,
    EvaluatorFailure,
    InvalidConstraintExpression
}

public sealed record ConstraintAtomEvaluation(
    ConstraintEvaluationOutcome Outcome,
    ConstraintDiagnosticCategory DiagnosticCategory,
    ImmutableArray<CanonicalName> UnsupportedConstraints,
    string Reason)
{
    public static ConstraintAtomEvaluation Satisfied(string reason = "constraint atom satisfied") =>
        new(ConstraintEvaluationOutcome.Satisfied, ConstraintDiagnosticCategory.Satisfied, [], reason);

    public static ConstraintAtomEvaluation Unsatisfied(string reason = "constraint atom unsatisfied") =>
        new(ConstraintEvaluationOutcome.Unsatisfied, ConstraintDiagnosticCategory.Unsatisfied, [], reason);

    public static ConstraintAtomEvaluation Unsupported(CanonicalName name) =>
        new(
            ConstraintEvaluationOutcome.Indeterminate,
            ConstraintDiagnosticCategory.UnsupportedConstraint,
            [name],
            $"UnsupportedConstraint: constraint kind '{name}' is unrecognised by target; no evaluator is available.");

    public static ConstraintAtomEvaluation InvalidValue() =>
        new(
            ConstraintEvaluationOutcome.Indeterminate,
            ConstraintDiagnosticCategory.InvalidConstraintValue,
            [],
            "InvalidConstraintValue: the target cannot evaluate the constraint value.");

    public static ConstraintAtomEvaluation EvaluatorFailed() =>
        new(
            ConstraintEvaluationOutcome.Indeterminate,
            ConstraintDiagnosticCategory.EvaluatorFailure,
            [],
            "EvaluatorFailure: the target constraint evaluator failed closed.");
}

public sealed record ConstraintExpressionEvaluation(
    ConstraintEvaluationOutcome Outcome,
    ConstraintDiagnosticCategory DiagnosticCategory,
    ImmutableArray<CanonicalName> UnsupportedConstraints,
    string Reason)
{
    internal ImmutableArray<CanonicalName> SatisfiedConstraints { get; init; } = [];
    internal ImmutableArray<CanonicalName> UnsatisfiedConstraints { get; init; } = [];
}

public static class ConstraintExpressionEvaluator
{
    public static ConstraintExpressionEvaluation Evaluate(
        ConstraintExpression expression,
        Func<Constraint, ConstraintAtomEvaluation> evaluateAtom)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(evaluateAtom);

        return expression switch
        {
            Constraint atom => EvaluateAtom(atom, evaluateAtom),
            AllOfConstraintExpression allOf => EvaluateGroup(allOf.Operands, evaluateAtom, requireAll: true),
            AnyOfConstraintExpression anyOf => EvaluateGroup(anyOf.Operands, evaluateAtom, requireAll: false),
            NotConstraintExpression not => Negate(Evaluate(not.Operand, evaluateAtom)),
            _ => InvalidExpression()
        };
    }

    public static ImmutableArray<Constraint> AtomicConstraints(ConstraintExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return expression switch
        {
            Constraint atom => [atom],
            AllOfConstraintExpression allOf => allOf.Operands.SelectMany(operand => AtomicConstraints(operand)).ToImmutableArray(),
            AnyOfConstraintExpression anyOf => anyOf.Operands.SelectMany(operand => AtomicConstraints(operand)).ToImmutableArray(),
            NotConstraintExpression not => AtomicConstraints(not.Operand),
            _ => []
        };
    }

    private static ConstraintExpressionEvaluation InvalidExpression() =>
        new(
            ConstraintEvaluationOutcome.Indeterminate,
            ConstraintDiagnosticCategory.InvalidConstraintExpression,
            [],
            "InvalidConstraintExpression: the target does not recognise the expression node and failed closed.");

    private static ConstraintExpressionEvaluation EvaluateAtom(
        Constraint atom,
        Func<Constraint, ConstraintAtomEvaluation> evaluateAtom)
    {
        ConstraintAtomEvaluation result;
        try
        {
            result = evaluateAtom(atom) ?? ConstraintAtomEvaluation.EvaluatorFailed();
        }
        catch
        {
            result = ConstraintAtomEvaluation.EvaluatorFailed();
        }

        return new(
            result.Outcome,
            result.DiagnosticCategory,
            NormalizeUnsupported(result.UnsupportedConstraints),
            result.Reason)
        {
            SatisfiedConstraints = result.Outcome == ConstraintEvaluationOutcome.Satisfied ? [atom.Name] : [],
            UnsatisfiedConstraints = result.Outcome == ConstraintEvaluationOutcome.Unsatisfied ? [atom.Name] : []
        };
    }

    private static ConstraintExpressionEvaluation EvaluateGroup(
        ImmutableArray<ConstraintExpression> operands,
        Func<Constraint, ConstraintAtomEvaluation> evaluateAtom,
        bool requireAll)
    {
        var children = operands.Select(operand => Evaluate(operand, evaluateAtom)).ToImmutableArray();
        var indeterminate = children.Where(child => child.Outcome == ConstraintEvaluationOutcome.Indeterminate).ToArray();
        if (indeterminate.Length > 0)
        {
            return Poison(indeterminate);
        }

        var satisfied = requireAll
            ? children.All(child => child.Outcome == ConstraintEvaluationOutcome.Satisfied)
            : children.Any(child => child.Outcome == ConstraintEvaluationOutcome.Satisfied);
        if (satisfied)
        {
            var proof = requireAll
                ? children.SelectMany(child => child.SatisfiedConstraints)
                : children
                    .Where(child => child.Outcome == ConstraintEvaluationOutcome.Satisfied)
                    .SelectMany(child => child.SatisfiedConstraints);
            return new(
                ConstraintEvaluationOutcome.Satisfied,
                ConstraintDiagnosticCategory.Satisfied,
                [],
                "Satisfied: the complete constraint expression matched.")
            {
                SatisfiedConstraints = NormalizeNames(proof)
            };
        }

        var failedProof = requireAll
            ? children
                .Where(child => child.Outcome == ConstraintEvaluationOutcome.Unsatisfied)
                .SelectMany(child => child.UnsatisfiedConstraints)
            : children.SelectMany(child => child.UnsatisfiedConstraints);
        return new(
                ConstraintEvaluationOutcome.Unsatisfied,
                ConstraintDiagnosticCategory.Unsatisfied,
                [],
                "Unsatisfied: the complete constraint expression did not match.")
        {
            UnsatisfiedConstraints = NormalizeNames(failedProof)
        };
    }

    private static ConstraintExpressionEvaluation Negate(ConstraintExpressionEvaluation child) =>
        child.Outcome switch
        {
            ConstraintEvaluationOutcome.Indeterminate => Poison([child]),
            ConstraintEvaluationOutcome.Satisfied => new(
                ConstraintEvaluationOutcome.Unsatisfied,
                ConstraintDiagnosticCategory.Unsatisfied,
                [],
                "Unsatisfied: the complete constraint expression did not match.")
            {
                UnsatisfiedConstraints = child.SatisfiedConstraints
            },
            ConstraintEvaluationOutcome.Unsatisfied => new(
                ConstraintEvaluationOutcome.Satisfied,
                ConstraintDiagnosticCategory.Satisfied,
                [],
                "Satisfied: the complete constraint expression matched.")
            {
                SatisfiedConstraints = child.UnsatisfiedConstraints
            },
            _ => throw new ArgumentOutOfRangeException(nameof(child))
        };

    private static ConstraintExpressionEvaluation Poison(IEnumerable<ConstraintExpressionEvaluation> children)
    {
        var items = children.ToArray();
        var unsupported = NormalizeUnsupported(items.SelectMany(child => child.UnsupportedConstraints));
        var category = unsupported.IsEmpty
            ? items.Select(child => child.DiagnosticCategory).Order().First()
            : ConstraintDiagnosticCategory.UnsupportedConstraint;
        var suffix = unsupported.IsEmpty
            ? string.Empty
            : $" Target has unrecognised constraint kind(s): {string.Join(", ", unsupported)}.";
        return new(
            ConstraintEvaluationOutcome.Indeterminate,
            category,
            unsupported,
            $"{category}: the complete constraint expression is indeterminate.{suffix}");
    }

    private static ImmutableArray<CanonicalName> NormalizeUnsupported(IEnumerable<CanonicalName> names) =>
        NormalizeNames(names);

    private static ImmutableArray<CanonicalName> NormalizeNames(IEnumerable<CanonicalName> names) =>
        names
            .Distinct()
            .OrderBy(name => name.ToString(), StringComparer.Ordinal)
            .ToImmutableArray();
}

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
    internal readonly record struct Snapshot(
        DateTimeOffset? ExpiresAt,
        DateTimeOffset? LatestTrustedTime,
        bool Dead,
        bool Invalidated);

    private readonly object _authorityGate;
    private readonly object _gate = new();
    private readonly TimeProvider? _timeProvider;
    private DateTimeOffset? _expiresAt;
    private DateTimeOffset? _latestTrustedTime;
    private bool _dead;
    private bool _invalidated;

    internal LivenessLease(
        ActorReference grantor,
        TimeSpan duration,
        TimeProvider? timeProvider,
        object authorityGate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
        _authorityGate = authorityGate ?? throw new ArgumentNullException(nameof(authorityGate));
        Grantor = grantor;
        Duration = duration;
        _timeProvider = timeProvider;
        _latestTrustedTime = timeProvider?.GetUtcNow();
        _expiresAt = _latestTrustedTime?.Add(duration);
        Id = Guid.NewGuid().ToString("N");
    }

    public string Id { get; }
    public ActorReference Grantor { get; }
    public TimeSpan Duration { get; }

    public DateTimeOffset? ExpiresAt
    {
        get
        {
            lock (_authorityGate)
            {
                lock (_gate)
                {
                    return _expiresAt;
                }
            }
        }
    }

    public bool Renew(ActorReference by)
    {
        if (!ReferenceEquals(by, Grantor))
        {
            throw new UnauthorizedAccessException("Only the lease grantor may renew it.");
        }

        lock (_authorityGate)
        {
            lock (_gate)
            {
                if (_invalidated || _timeProvider is null)
                {
                    return false;
                }

                var now = ObserveTrustedTime(_timeProvider.GetUtcNow());
                if (_dead || _expiresAt is null || now >= _expiresAt.Value)
                {
                    _dead = true;
                    return false;
                }

                _expiresAt = now.Add(Duration);
                return true;
            }
        }
    }

    internal bool IsAlive(DateTimeOffset trustedNow)
    {
        lock (_authorityGate)
        {
            lock (_gate)
            {
                var observed = ObserveTrustedTime(trustedNow);
                if (_invalidated || _dead || _expiresAt is null || observed >= _expiresAt.Value)
                {
                    _dead = true;
                    return false;
                }

                return true;
            }
        }
    }

    internal Snapshot CaptureState()
    {
        lock (_authorityGate)
        {
            lock (_gate)
            {
                return new Snapshot(_expiresAt, _latestTrustedTime, _dead, _invalidated);
            }
        }
    }

    internal void RestoreState(Snapshot snapshot)
    {
        lock (_authorityGate)
        {
            lock (_gate)
            {
                _expiresAt = snapshot.ExpiresAt;
                _latestTrustedTime = snapshot.LatestTrustedTime;
                _dead = snapshot.Dead;
                _invalidated = snapshot.Invalidated;
            }
        }
    }

    internal void Invalidate()
    {
        lock (_authorityGate)
        {
            lock (_gate)
            {
                _invalidated = true;
                _dead = true;
            }
        }
    }

    private DateTimeOffset ObserveTrustedTime(DateTimeOffset trustedNow)
    {
        if (_latestTrustedTime is null || trustedNow > _latestTrustedTime.Value)
        {
            _latestTrustedTime = trustedNow;
        }

        return _latestTrustedTime.Value;
    }
}

public sealed record ConstraintDecision(bool Allowed, CanonicalName ConstraintName, string Reason)
{
    public ConstraintEvaluationOutcome Outcome { get; init; } =
        Allowed ? ConstraintEvaluationOutcome.Satisfied : ConstraintEvaluationOutcome.Unsatisfied;
    public ConstraintDiagnosticCategory DiagnosticCategory { get; init; } =
        Allowed ? ConstraintDiagnosticCategory.Satisfied : ConstraintDiagnosticCategory.Unsatisfied;
    public ImmutableArray<CanonicalName> UnsupportedConstraints { get; init; } = [];

    public static ConstraintDecision Allow(CanonicalName name, string reason) => new(true, name, reason);
    public static ConstraintDecision Deny(CanonicalName name, string reason) => new(false, name, reason);

    internal static ConstraintDecision FromExpression(
        ConstraintExpression expression,
        ConstraintExpressionEvaluation evaluation) =>
        new(evaluation.Outcome == ConstraintEvaluationOutcome.Satisfied, expression.DiagnosticName, evaluation.Reason)
        {
            Outcome = evaluation.Outcome,
            DiagnosticCategory = evaluation.DiagnosticCategory,
            UnsupportedConstraints = evaluation.UnsupportedConstraints
        };
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
        IEnumerable<ConstraintExpression> addedConstraints,
        bool delegationAllowed,
        Action<Capability> derivedCallback)
    {
        DomainId = domainId;
        Holder = holder;
        Target = target;
        RootOperations = rootOperations;
        Parent = parent;
        AddedConstraintExpressions = addedConstraints.ToImmutableArray();
        AddedConstraints = AddedConstraintExpressions.OfType<Constraint>().ToImmutableArray();
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
    public ImmutableArray<ConstraintExpression> AddedConstraintExpressions { get; }
    public bool DelegationAllowed { get; }
    public bool IsPrimordial => Parent is null;

    public Capability Delegate(ActorReference newHolder, params Constraint[] added)
        => DelegateExpressions(newHolder, added);

    public Capability DelegateExpressions(ActorReference newHolder, params ConstraintExpression[] added)
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

        if (added.SelectMany(expression => ConstraintExpressionEvaluator.AtomicConstraints(expression))
            .Any(constraint => constraint.Name == StandardConstraintNames.OriginGrant))
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

    internal IEnumerable<ConstraintExpression> EffectiveConstraintExpressions() =>
        DerivationChain().SelectMany(capability => capability.AddedConstraintExpressions);

    internal IEnumerable<Constraint> EffectiveConstraints() =>
        EffectiveConstraintExpressions().SelectMany(expression => ConstraintExpressionEvaluator.AtomicConstraints(expression));

    public override string ToString() => $"Capability {Id:N} for {Holder}";
}
