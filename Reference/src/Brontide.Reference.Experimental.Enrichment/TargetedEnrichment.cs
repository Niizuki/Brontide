using System.Collections.Immutable;
using Brontide.Reference.Core;

namespace Brontide.Reference.Experimental.Enrichment;

/// <summary>
/// A recorded implementation choice, not part of the experimental availability semantics.
/// </summary>
public enum EnrichmentRealizationStrategy
{
    DirectMaterialization,
    ParameterThreading,
    CarrierStructure,
    ContextualStorage,
    AttachedFragment,
    Forwarding
}

public enum EnrichmentTransformKind
{
    Copy,
    Projection,
    DeterministicDerivation
}

public sealed record EnrichmentSourceRequirement
{
    public EnrichmentSourceRequirement(string key, ShapeContract shape)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("An Enrichment source key is required.", nameof(key));
        }

        Key = key;
        Shape = shape ?? throw new ArgumentNullException(nameof(shape));
    }

    public string Key { get; }
    public ShapeContract Shape { get; }
}

public sealed record AvailableValue
{
    public AvailableValue(string key, ShapeValue value, string provenance)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(provenance))
        {
            throw new ArgumentException("Available values require a key and provenance.");
        }

        Key = key;
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Provenance = provenance;
    }

    public string Key { get; }
    public ShapeValue Value { get; }
    public string Provenance { get; }

    public static AvailableValue Direct(string key, ShapeValue value, string provenance) =>
        new(key, value, provenance);

    public static AvailableValue FromExecution(string key, ExecutionResult execution)
    {
        ArgumentNullException.ThrowIfNull(execution);
        if (execution.Outcome.Status != OutcomeStatus.Succeeded || execution.Outcome.Result is null)
        {
            throw new EnrichmentResolutionException(
                $"Execution {execution.Execution.Id} did not produce a successful result for source '{key}'.");
        }

        return new AvailableValue(
            key,
            execution.Outcome.Result,
            $"result of {execution.Execution.Operation} Execution {execution.Execution.Id}");
    }
}

public sealed class EnrichmentInputs
{
    private readonly ImmutableDictionary<string, ShapeValue> _values;

    internal EnrichmentInputs(IEnumerable<KeyValuePair<string, ShapeValue>> values) =>
        _values = values.ToImmutableDictionary(StringComparer.Ordinal);

    public ShapeValue Require(string key) => _values.TryGetValue(key, out var value)
        ? value
        : throw new EnrichmentResolutionException($"Required source '{key}' is unavailable.");
}

public delegate IReadOnlyDictionary<string, ShapeValue> EnrichmentDerivation(EnrichmentInputs inputs);

public sealed class EnrichmentProvider
{
    private EnrichmentProvider(
        CanonicalName id,
        OperationReference target,
        FragmentReference fragment,
        IEnumerable<EnrichmentSourceRequirement> requirements,
        EnrichmentTransformKind transformKind,
        EnrichmentDerivation derive)
    {
        Id = id;
        Target = target;
        Fragment = fragment;
        Requirements = requirements.ToImmutableArray();
        TransformKind = transformKind;
        Derive = derive;
    }

    public CanonicalName Id { get; }
    public OperationReference Target { get; }
    public FragmentReference Fragment { get; }
    public ImmutableArray<EnrichmentSourceRequirement> Requirements { get; }
    public EnrichmentTransformKind TransformKind { get; }
    internal EnrichmentDerivation Derive { get; }

    public static EnrichmentProvider CopyField(
        CanonicalName id,
        OperationReference target,
        FragmentReference fragment,
        EnrichmentSourceRequirement source,
        string sourceField,
        string targetField) =>
        new(
            id,
            target,
            fragment,
            [source],
            EnrichmentTransformKind.Copy,
            inputs => new Dictionary<string, ShapeValue>(StringComparer.Ordinal)
            {
                [targetField] = inputs.Require(source.Key).RequireField(sourceField)
            });

    public static EnrichmentProvider Project(
        CanonicalName id,
        OperationReference target,
        FragmentReference fragment,
        EnrichmentSourceRequirement source,
        string targetField,
        Func<ShapeValue, ShapeValue> projection) =>
        new(
            id,
            target,
            fragment,
            [source],
            EnrichmentTransformKind.Projection,
            inputs => new Dictionary<string, ShapeValue>(StringComparer.Ordinal)
            {
                [targetField] = projection(inputs.Require(source.Key))
            });

    public static EnrichmentProvider DeriveDeterministically(
        CanonicalName id,
        OperationReference target,
        FragmentReference fragment,
        IEnumerable<EnrichmentSourceRequirement> requirements,
        EnrichmentDerivation derivation) =>
        new(
            id,
            target,
            fragment,
            requirements,
            EnrichmentTransformKind.DeterministicDerivation,
            derivation ?? throw new ArgumentNullException(nameof(derivation)));
}

public sealed record EnrichmentTrace(
    string Composition,
    OperationReference Target,
    FragmentReference Fragment,
    CanonicalName Provider,
    EnrichmentTransformKind Transform,
    EnrichmentRealizationStrategy Strategy,
    ImmutableDictionary<string, string> Sources);

public interface IEnrichmentObserver
{
    void Resolved(EnrichmentTrace trace);
}

public sealed record EnrichmentResolution(ShapeValue Input, EnrichmentTrace Trace);

/// <summary>
/// Composition-local targeted availability. It only constructs shaped information, then calls the
/// ordinary Core gate with a Capability explicitly supplied by the caller.
/// </summary>
public sealed class TargetedEnrichmentComposition
{
    private readonly ShapeRegistry _shapes;
    private readonly ImmutableDictionary<(OperationReference Target, FragmentReference Fragment), EnrichmentProvider> _providers;
    private readonly IEnrichmentObserver? _observer;

    public TargetedEnrichmentComposition(
        string name,
        ShapeRegistry shapes,
        IEnumerable<EnrichmentProvider> providers,
        EnrichmentRealizationStrategy strategy = EnrichmentRealizationStrategy.DirectMaterialization,
        IEnrichmentObserver? observer = null)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("A composition name is required.", nameof(name))
            : name;
        _shapes = shapes ?? throw new ArgumentNullException(nameof(shapes));
        Strategy = strategy;
        _observer = observer;

        var builder = ImmutableDictionary.CreateBuilder<
            (OperationReference Target, FragmentReference Fragment), EnrichmentProvider>();
        foreach (var provider in providers ?? throw new ArgumentNullException(nameof(providers)))
        {
            var key = (provider.Target, provider.Fragment);
            if (!builder.TryAdd(key, provider))
            {
                throw new EnrichmentConfigurationException(
                    $"Competing providers target {provider.Fragment} at {provider.Target} in composition '{Name}'.");
            }

            if (!_shapes.Fragments.Any(fragment => fragment.Reference == provider.Fragment))
            {
                throw new EnrichmentConfigurationException(
                    $"Provider {provider.Id} produces unrecognised Fragment {provider.Fragment}.");
            }
        }

        _providers = builder.ToImmutable();
    }

    public string Name { get; }
    public EnrichmentRealizationStrategy Strategy { get; }

    public EnrichmentResolution Resolve(
        OperationReference target,
        FragmentReference requiredFragment,
        ShapeValue baseInput,
        IEnumerable<AvailableValue> availableValues)
    {
        if (baseInput is not RecordShapeValue record)
        {
            throw new EnrichmentResolutionException("Targeted Enrichment currently requires a record input Shape.");
        }

        if (record.Fragments.Keys.Any(fragment => fragment.Name == requiredFragment.Name))
        {
            throw new EnrichmentResolutionException(
                $"Enrichment may only add absent structure; {requiredFragment.Name} is already present.");
        }

        if (!_providers.TryGetValue((target, requiredFragment), out var provider))
        {
            throw new EnrichmentResolutionException(
                $"No provider makes {requiredFragment} available at {target} in composition '{Name}'.");
        }

        var available = new Dictionary<string, AvailableValue>(StringComparer.Ordinal);
        foreach (var value in availableValues ?? throw new ArgumentNullException(nameof(availableValues)))
        {
            if (!available.TryAdd(value.Key, value))
            {
                throw new EnrichmentResolutionException($"Available source key '{value.Key}' is ambiguous.");
            }
        }

        var projectedSources = ImmutableDictionary.CreateBuilder<string, ShapeValue>(StringComparer.Ordinal);
        var provenance = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var requirement in provider.Requirements)
        {
            if (!available.TryGetValue(requirement.Key, out var source))
            {
                throw new EnrichmentResolutionException(
                    $"Provider {provider.Id} requires missing source '{requirement.Key}'.");
            }

            var projected = _shapes.Project(source.Value, requirement.Shape);
            if (!projected.IsValid)
            {
                throw new EnrichmentResolutionException(
                    $"Source '{requirement.Key}' is incompatible: {projected.Message}");
            }

            projectedSources.Add(requirement.Key, projected.Value!);
            provenance.Add(requirement.Key, source.Provenance);
        }

        IReadOnlyDictionary<string, ShapeValue> fields;
        try
        {
            fields = provider.Derive(new EnrichmentInputs(projectedSources));
        }
        catch (EnrichmentResolutionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new EnrichmentResolutionException(
                $"Provider {provider.Id} failed visibly: {exception.Message}", exception);
        }

        var fragments = record.Fragments
            .Select(fragment => (fragment.Key, fragment.Value))
            .Append((requiredFragment, fields))
            .ToArray();
        var enriched = ShapeValue.Record(
            record.Reference,
            record.Fields.Select(field => (field.Key, field.Value)),
            fragments);
        var validation = _shapes.Project(
            enriched,
            ShapeContract.For(record.Reference, requiredFragment));
        if (!validation.IsValid)
        {
            throw new EnrichmentResolutionException(
                $"Provider {provider.Id} produced an invalid Fragment: {validation.Message}");
        }

        var trace = new EnrichmentTrace(
            Name,
            target,
            requiredFragment,
            provider.Id,
            provider.TransformKind,
            Strategy,
            provenance.ToImmutable());
        _observer?.Resolved(trace);
        return new EnrichmentResolution(enriched, trace);
    }

    public async ValueTask<ExecutionResult> ExecuteAsync(
        AuthorityDomain domain,
        ActorReference actor,
        OperationReference target,
        Capability capability,
        ShapeValue baseInput,
        FragmentReference requiredFragment,
        IEnumerable<AvailableValue> availableValues)
    {
        ArgumentNullException.ThrowIfNull(domain);
        var resolution = Resolve(target, requiredFragment, baseInput, availableValues);
        return await domain.ExecuteAsync(actor, target, capability, resolution.Input).ConfigureAwait(false);
    }
}

public sealed class EnrichmentConfigurationException(string message) : InvalidOperationException(message);

public sealed class EnrichmentResolutionException : InvalidOperationException
{
    public EnrichmentResolutionException(string message) : base(message) { }
    public EnrichmentResolutionException(string message, Exception innerException) : base(message, innerException) { }
}
