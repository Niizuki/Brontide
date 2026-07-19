using System.Collections.Immutable;
using Brontide.Reference.Core;

namespace Brontide.Reference.Experimental.Composition;

/// <summary>
/// Experimental vocabulary for the dependency strengths distinguished by Brontide 0.5 section 18.2.
/// These values deliberately do not form part of Brontide.Reference.Core or claim a ratified Brontide descriptor.
/// </summary>
public enum DependencyStrength
{
    RequiredContract,
    RequiredProfile,
    PreferredSystemProvider,
    RequiredAuthoredProvider
}

public sealed record ComponentDependency
{
    private ComponentDependency(
        CanonicalName contract,
        DependencyStrength strength,
        CanonicalName? requiredProvider)
    {
        if (strength == DependencyStrength.RequiredAuthoredProvider && requiredProvider is null)
        {
            throw new ArgumentException("A provider-specific dependency must name its required provider.");
        }

        if (strength != DependencyStrength.RequiredAuthoredProvider && requiredProvider is not null)
        {
            throw new ArgumentException("Only a provider-specific dependency may name a required provider.");
        }

        Contract = contract;
        Strength = strength;
        RequiredProvider = requiredProvider;
    }

    public CanonicalName Contract { get; }
    public DependencyStrength Strength { get; }
    public CanonicalName? RequiredProvider { get; }
    public bool IsRequired => Strength != DependencyStrength.PreferredSystemProvider;

    public static ComponentDependency RequireContract(CanonicalName contract) =>
        new(contract, DependencyStrength.RequiredContract, null);

    public static ComponentDependency RequireProfile(CanonicalName profile) =>
        new(profile, DependencyStrength.RequiredProfile, null);

    public static ComponentDependency PreferSystemProvider(CanonicalName contract) =>
        new(contract, DependencyStrength.PreferredSystemProvider, null);

    public static ComponentDependency RequireProvider(CanonicalName contract, CanonicalName provider) =>
        new(contract, DependencyStrength.RequiredAuthoredProvider, provider);
}

public enum ComponentBoundaryVisibility
{
    Declared,
    OpaqueBox
}

public enum ExecutionProperty
{
    Pure,
    Deterministic,
    ReplaySafe,
    Batchable,
    Vectorisable,
    Relocatable,
    AcceleratorCompatible
}

public sealed class ComponentDescriptor
{
    public ComponentDescriptor(
        CanonicalName name,
        CanonicalName provider,
        IEnumerable<OperationReference> operations,
        IEnumerable<ComponentDependency>? dependencies = null,
        IEnumerable<ExecutionProperty>? executionClaims = null,
        ComponentBoundaryVisibility boundaryVisibility = ComponentBoundaryVisibility.Declared)
    {
        var providedOperations = operations.ToImmutableArray();
        if (providedOperations.IsEmpty && boundaryVisibility != ComponentBoundaryVisibility.OpaqueBox)
        {
            throw new ArgumentException("A Component descriptor must expose at least one Operation.", nameof(operations));
        }

        Name = name;
        Provider = provider;
        Operations = providedOperations;
        Dependencies = (dependencies ?? []).ToImmutableArray();
        ExecutionClaims = (executionClaims ?? []).ToImmutableHashSet();
        BoundaryVisibility = boundaryVisibility;
    }

    public CanonicalName Name { get; }
    public CanonicalName Provider { get; }
    public ImmutableArray<OperationReference> Operations { get; }
    public ImmutableArray<ComponentDependency> Dependencies { get; }
    public ImmutableHashSet<ExecutionProperty> ExecutionClaims { get; }
    public ComponentBoundaryVisibility BoundaryVisibility { get; }

    public static ComponentDescriptor Boxed(
        CanonicalName name,
        CanonicalName provider,
        params OperationReference[] boundaryOperations) =>
        new(
            name,
            provider,
            boundaryOperations,
            boundaryVisibility: ComponentBoundaryVisibility.OpaqueBox);
}

public sealed class FacilityDescriptor
{
    public FacilityDescriptor(
        CanonicalName component,
        CanonicalName provider,
        CanonicalName contract,
        bool isSystemProvided,
        IEnumerable<CanonicalName>? profiles = null)
    {
        Component = component;
        Provider = provider;
        Contract = contract;
        IsSystemProvided = isSystemProvided;
        Profiles = (profiles ?? []).ToImmutableHashSet();
    }

    public CanonicalName Component { get; }
    public CanonicalName Provider { get; }
    public CanonicalName Contract { get; }
    public bool IsSystemProvided { get; }
    public ImmutableHashSet<CanonicalName> Profiles { get; }
}

public sealed record DependencyResolution(
    ComponentDependency Dependency,
    FacilityDescriptor? Facility,
    bool IsSatisfied,
    string Explanation);

public sealed record DefinitionConstraintCandidate<T>(
    CanonicalName Name,
    T Value,
    ConstraintExpression Constraint);

public sealed record DefinitionConstraintRejection(
    CanonicalName Candidate,
    ConstraintDiagnosticCategory DiagnosticCategory,
    ImmutableArray<CanonicalName> UnsupportedConstraints,
    string Reason);

public sealed record DefinitionConstraintSelectionResult<T>(
    ImmutableArray<DefinitionConstraintCandidate<T>> Eligible,
    ImmutableArray<DefinitionConstraintRejection> Rejected);

/// <summary>
/// Experimental Architecture 0.7 selection boundary. An indeterminate Definition Constraint is
/// unsatisfiable for selection and excludes its candidate without exposing atom values.
/// </summary>
public static class DefinitionConstraintSelection
{
    public static DefinitionConstraintSelectionResult<T> Filter<T>(
        IEnumerable<DefinitionConstraintCandidate<T>> candidates,
        Func<Constraint, ConstraintAtomEvaluation> evaluateAtom)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(evaluateAtom);

        var eligible = ImmutableArray.CreateBuilder<DefinitionConstraintCandidate<T>>();
        var rejected = ImmutableArray.CreateBuilder<DefinitionConstraintRejection>();
        foreach (var candidate in candidates)
        {
            var evaluation = ConstraintExpressionEvaluator.Evaluate(candidate.Constraint, evaluateAtom);
            if (evaluation.Outcome == ConstraintEvaluationOutcome.Satisfied)
            {
                eligible.Add(candidate);
                continue;
            }

            rejected.Add(new(
                candidate.Name,
                evaluation.DiagnosticCategory,
                evaluation.UnsupportedConstraints,
                evaluation.Reason));
        }

        return new(eligible.ToImmutable(), rejected.ToImmutable());
    }
}

/// <summary>
/// Small experimental resolver that preserves dependency strength instead of flattening every
/// dependency into an undifferentiated service flag.
/// </summary>
public sealed class CompositionCatalog
{
    private readonly List<FacilityDescriptor> _facilities = [];

    public IReadOnlyList<FacilityDescriptor> Facilities => _facilities;

    public void Add(FacilityDescriptor facility)
    {
        ArgumentNullException.ThrowIfNull(facility);
        _facilities.Add(facility);
    }

    public DependencyResolution Resolve(ComponentDependency dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        FacilityDescriptor? selected = dependency.Strength switch
        {
            DependencyStrength.RequiredContract => _facilities.FirstOrDefault(
                facility => facility.Contract == dependency.Contract),
            DependencyStrength.RequiredProfile => _facilities.FirstOrDefault(
                facility => facility.Profiles.Contains(dependency.Contract)),
            DependencyStrength.PreferredSystemProvider => _facilities.FirstOrDefault(
                facility => facility.Contract == dependency.Contract && facility.IsSystemProvided),
            DependencyStrength.RequiredAuthoredProvider => _facilities.FirstOrDefault(
                facility => facility.Contract == dependency.Contract &&
                    facility.Provider == dependency.RequiredProvider),
            _ => throw new ArgumentOutOfRangeException(nameof(dependency))
        };

        if (selected is not null)
        {
            return new DependencyResolution(
                dependency,
                selected,
                true,
                $"{dependency.Strength} {dependency.Contract} selected {selected.Provider}.");
        }

        if (!dependency.IsRequired)
        {
            return new DependencyResolution(
                dependency,
                null,
                true,
                $"Preferred system facility {dependency.Contract} is absent; the preference remains unsatisfied but optional.");
        }

        var provider = dependency.RequiredProvider is { } requiredProvider
            ? $" from {requiredProvider}"
            : string.Empty;
        return new DependencyResolution(
            dependency,
            null,
            false,
            $"Required {dependency.Strength} {dependency.Contract}{provider} is unavailable.");
    }

    public ImmutableArray<DependencyResolution> Resolve(ComponentDescriptor component) =>
        component.Dependencies.Select(Resolve).ToImmutableArray();
}

public sealed record OptimizationEligibility(
    bool IsEligible,
    ImmutableArray<ExecutionProperty> RequiredClaims,
    ImmutableArray<ExecutionProperty> MissingClaims)
{
    public static ImmutableArray<ExecutionProperty> AcceleratorClaims { get; } =
    [
        ExecutionProperty.Pure,
        ExecutionProperty.Deterministic,
        ExecutionProperty.Batchable,
        ExecutionProperty.AcceleratorCompatible
    ];

    public static OptimizationEligibility ForAccelerator(ComponentDescriptor component)
    {
        var missing = AcceleratorClaims
            .Where(claim => !component.ExecutionClaims.Contains(claim))
            .ToImmutableArray();
        return new OptimizationEligibility(missing.IsEmpty, AcceleratorClaims, missing);
    }
}

public enum ExecutionPlacement
{
    InProcessCpu,
    VectorAccelerator,
    Remote,
    Opaque
}

/// <summary>
/// Experimental structured observation. It is intentionally named as an observation rather than
/// a Binding Plan because Brontide 0.5 has not ratified the Binding Plan representation.
/// </summary>
public sealed record ProviderExecutionObservation(
    OperationReference Operation,
    ComponentDescriptor SelectedComponent,
    string SelectionReason,
    ExecutionPlacement Placement,
    string Representation,
    ImmutableArray<string> CrossedBoundaries,
    ImmutableArray<ExecutionProperty> ClaimsUsed,
    int BatchSize,
    int Copies,
    int Retries,
    string FailureDomain,
    CanonicalName? FallbackFrom = null);

public sealed record StructuredExecutionExplanation(
    ExecutionId Execution,
    OperationReference Operation,
    ProviderExecutionObservation Provider,
    OutcomeStatus Outcome,
    TimeSpan Elapsed,
    int EmittedOccurrences,
    string Message);
