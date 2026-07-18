using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using Brontide.Reference.Core;

namespace Brontide.Reference.Experimental.Composition;

public static class ImageWorkspaceContracts
{
    public static readonly OperationReference Invert = OperationReference.Parse("Brontide:Image.Invert");
    public static readonly ShapeReference ImageFrame = ShapeReference.Parse("Brontide:Image.Frame", 1);

    internal static void Register(AuthorityDomain.GenesisContext genesis)
    {
        genesis.Shape(ShapeDefinition.Record(
            ImageFrame,
            FragmentPolicy.Closed,
            RecordField.Required("width", BuiltInShapes.Signed64),
            RecordField.Required("height", BuiltInShapes.Signed64),
            RecordField.Required("pixels", BuiltInShapes.Bytes)));
    }
}

public sealed record ImageFrame
{
    public ImageFrame(int width, int height, IEnumerable<byte> pixels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        var materialized = pixels.ToImmutableArray();
        if (materialized.Length != checked(width * height))
        {
            throw new ArgumentException("An image requires exactly width × height grayscale pixels.", nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = materialized;
    }

    public int Width { get; }
    public int Height { get; }
    public ImmutableArray<byte> Pixels { get; }

    internal ShapeValue ToShapeValue() => ShapeValue.Record(
        ImageWorkspaceContracts.ImageFrame,
        ("width", ShapeValue.Signed64(Width)),
        ("height", ShapeValue.Signed64(Height)),
        ("pixels", ShapeValue.Opaque(BuiltInShapes.Bytes, Pixels.AsSpan())));

    internal static ImageFrame FromShapeValue(ShapeValue value)
    {
        var width = checked((int)value.RequireField("width").RequireScalar<long>());
        var height = checked((int)value.RequireField("height").RequireScalar<long>());
        if (value.RequireField("pixels") is not OpaqueShapeValue pixels)
        {
            throw new InvalidOperationException("Image pixels must use the Opaque.Bytes Shape.");
        }

        return new ImageFrame(width, height, pixels.Bytes.ToArray());
    }
}

public sealed record ProviderOperationalCharacteristics(
    ExecutionPlacement Placement,
    string Representation,
    ImmutableArray<string> CrossedBoundaries,
    int BatchSize,
    int Copies,
    string FailureDomain);

public interface IImageTransformProvider
{
    ComponentDescriptor Descriptor { get; }
    ProviderOperationalCharacteristics Characteristics { get; }
    ImageFrame Invert(ImageFrame input);
}

public sealed class CpuImageInvertProvider : IImageTransformProvider
{
    public CpuImageInvertProvider()
    {
        Descriptor = new ComponentDescriptor(
            CanonicalName.Parse("Brontide:Image.Invert.Cpu"),
            CanonicalName.Parse("Brontide:ReferenceStack"),
            [ImageWorkspaceContracts.Invert],
            executionClaims: [ExecutionProperty.Pure, ExecutionProperty.Deterministic]);
    }

    public ComponentDescriptor Descriptor { get; }

    public ProviderOperationalCharacteristics Characteristics { get; } = new(
        ExecutionPlacement.InProcessCpu,
        "direct immutable grayscale buffer",
        [],
        1,
        1,
        "Brontide.Reference Studio process");

    public ImageFrame Invert(ImageFrame input)
    {
        var result = new byte[input.Pixels.Length];
        for (var index = 0; index < input.Pixels.Length; index++)
        {
            result[index] = (byte)(byte.MaxValue - input.Pixels[index]);
        }

        return new ImageFrame(input.Width, input.Height, result);
    }
}

/// <summary>
/// A real System.Numerics vector path used as Brontide.Reference's current specialised-accelerator evidence.
/// It is not presented as a GPU and records the managed/vector boundary explicitly.
/// </summary>
public sealed class VectorImageInvertProvider : IImageTransformProvider
{
    public VectorImageInvertProvider()
    {
        Descriptor = new ComponentDescriptor(
            CanonicalName.Parse("Brontide:Image.Invert.Vector"),
            CanonicalName.Parse("Brontide:ReferenceStack"),
            [ImageWorkspaceContracts.Invert],
            executionClaims:
            [
                ExecutionProperty.Pure,
                ExecutionProperty.Deterministic,
                ExecutionProperty.Batchable,
                ExecutionProperty.Vectorisable,
                ExecutionProperty.AcceleratorCompatible
            ]);
    }

    public ComponentDescriptor Descriptor { get; }

    public ProviderOperationalCharacteristics Characteristics { get; } = new(
        ExecutionPlacement.VectorAccelerator,
        "copied contiguous grayscale buffer",
        ["managed host → System.Numerics vector provider"],
        Vector<byte>.Count,
        1,
        "Brontide.Reference Studio vector provider");

    public ImageFrame Invert(ImageFrame input)
    {
        var source = input.Pixels.AsSpan();
        var result = new byte[source.Length];
        var maximum = new Vector<byte>(byte.MaxValue);
        var index = 0;
        for (; index <= source.Length - Vector<byte>.Count; index += Vector<byte>.Count)
        {
            var values = new Vector<byte>(source.Slice(index, Vector<byte>.Count));
            (maximum - values).CopyTo(result, index);
        }

        for (; index < source.Length; index++)
        {
            result[index] = (byte)(byte.MaxValue - source[index]);
        }

        return new ImageFrame(input.Width, input.Height, result);
    }
}

public sealed record ProviderTransformResult(ImageFrame Output, ProviderExecutionObservation Observation);

public sealed class ImageTransformRouter
{
    private const int AcceleratorThreshold = 256;
    private readonly ImmutableArray<IImageTransformProvider> _providers;

    public ImageTransformRouter(IEnumerable<IImageTransformProvider> providers)
    {
        _providers = providers.ToImmutableArray();
        if (_providers.IsEmpty)
        {
            throw new ArgumentException("At least one transform provider is required.", nameof(providers));
        }

        if (_providers.Any(provider => !provider.Descriptor.Operations.Contains(ImageWorkspaceContracts.Invert)))
        {
            throw new ArgumentException("Every image transform provider must expose the same Image.Invert Operation.", nameof(providers));
        }

        if (_providers.All(provider => provider.Characteristics.Placement != ExecutionPlacement.InProcessCpu))
        {
            throw new ArgumentException("A visible CPU fallback provider is required.", nameof(providers));
        }
    }

    public ProviderTransformResult Invert(ImageFrame input, bool preferAcceleration)
    {
        var cpu = _providers.First(provider =>
            provider.Characteristics.Placement == ExecutionPlacement.InProcessCpu);
        var selected = cpu;
        var reason = "Local CPU provider selected for the small composition.";

        if (preferAcceleration && input.Pixels.Length >= AcceleratorThreshold)
        {
            var accelerated = _providers.FirstOrDefault(provider =>
                provider.Characteristics.Placement != ExecutionPlacement.InProcessCpu &&
                OptimizationEligibility.ForAccelerator(provider.Descriptor).IsEligible);
            if (accelerated is not null)
            {
                selected = accelerated;
                reason =
                    $"{accelerated.Descriptor.Name} selected because it explicitly declares every required accelerator claim and the workload has {input.Pixels.Length} pixels.";
            }
            else
            {
                var missing = _providers
                    .Where(provider => provider.Characteristics.Placement != ExecutionPlacement.InProcessCpu)
                    .SelectMany(provider => OptimizationEligibility.ForAccelerator(provider.Descriptor).MissingClaims)
                    .Distinct()
                    .ToArray();
                reason = missing.Length == 0
                    ? "No accelerator provider is available; the declared CPU fallback remains selected."
                    : $"No accelerator provider declares all required claims ({string.Join(", ", missing)} missing); the CPU fallback remains selected.";
            }
        }

        try
        {
            return Result(selected, selected.Invert(input), reason);
        }
        catch (Exception exception) when (!ReferenceEquals(selected, cpu))
        {
            var fallback = cpu.Invert(input);
            var attempted = selected.Characteristics;
            var recovered = cpu.Characteristics;
            return new ProviderTransformResult(
                fallback,
                new ProviderExecutionObservation(
                    ImageWorkspaceContracts.Invert,
                    cpu.Descriptor,
                    $"{selected.Descriptor.Name} failed with {exception.GetType().Name}; explicit CPU fallback selected.",
                    recovered.Placement,
                    $"failed attempt: {attempted.Representation}; fallback: {recovered.Representation}",
                    attempted.CrossedBoundaries.Add("accelerator failure → declared CPU fallback"),
                    OptimizationEligibility.AcceleratorClaims,
                    attempted.BatchSize,
                    attempted.Copies + recovered.Copies,
                    0,
                    $"{attempted.FailureDomain}; fallback in {recovered.FailureDomain}",
                    selected.Descriptor.Name));
        }
    }

    private static ProviderTransformResult Result(
        IImageTransformProvider provider,
        ImageFrame output,
        string reason,
        CanonicalName? fallbackFrom = null)
    {
        var characteristics = provider.Characteristics;
        var claimsUsed = characteristics.Placement == ExecutionPlacement.InProcessCpu
            ? ImmutableArray<ExecutionProperty>.Empty
            : OptimizationEligibility.AcceleratorClaims;
        return new ProviderTransformResult(
            output,
            new ProviderExecutionObservation(
                ImageWorkspaceContracts.Invert,
                provider.Descriptor,
                reason,
                characteristics.Placement,
                characteristics.Representation,
                characteristics.CrossedBoundaries,
                claimsUsed,
                characteristics.BatchSize,
                characteristics.Copies,
                0,
                characteristics.FailureDomain,
                fallbackFrom));
    }
}

public static class ImageWorkspaceFacilityContracts
{
    public static readonly CanonicalName ExecutionHistory = CanonicalName.Parse("Brontide:ExecutionHistory");
    public static readonly CanonicalName RetainedExecutionHistory = CanonicalName.Parse("Brontide:SessionRetainedExecutionHistory");
    public static readonly CanonicalName SearchableMetadata = CanonicalName.Parse("Brontide:SearchableImageMetadata");
    public static readonly CanonicalName WorkspaceState = CanonicalName.Parse("Brontide:WorkspaceState");
}

public sealed record WorkspaceExecutionSnapshot(
    ExecutionId Execution,
    ImageFrame Input,
    ImageFrame Output,
    StructuredExecutionExplanation Explanation);

public interface IWorkspaceFacility
{
    FacilityDescriptor Descriptor { get; }
    void Observe(WorkspaceExecutionSnapshot execution);
}

public sealed class InMemoryExecutionHistoryFacility : IWorkspaceFacility
{
    private readonly List<WorkspaceExecutionSnapshot> _entries = [];

    public FacilityDescriptor Descriptor { get; } = new(
        CanonicalName.Parse("Brontide:Workspace.ExecutionHistory"),
        CanonicalName.Parse("Brontide:ReferenceStack"),
        ImageWorkspaceFacilityContracts.ExecutionHistory,
        true,
        [ImageWorkspaceFacilityContracts.RetainedExecutionHistory]);

    public IReadOnlyList<WorkspaceExecutionSnapshot> Entries => _entries;

    public void Observe(WorkspaceExecutionSnapshot execution) => _entries.Add(execution);
}

public sealed record ImageMetadata(ExecutionId Execution, int Width, int Height, double AverageLuminance);

public sealed class InMemoryMetadataFacility : IWorkspaceFacility
{
    private readonly List<ImageMetadata> _entries = [];

    public InMemoryMetadataFacility(CanonicalName component, CanonicalName provider, bool isSystemProvided)
    {
        Descriptor = new FacilityDescriptor(
            component,
            provider,
            ImageWorkspaceFacilityContracts.SearchableMetadata,
            isSystemProvided);
    }

    public FacilityDescriptor Descriptor { get; }
    public IReadOnlyList<ImageMetadata> Entries => _entries;

    public IEnumerable<ImageMetadata> SearchByDimensions(int width, int height) =>
        _entries.Where(entry => entry.Width == width && entry.Height == height);

    public void Observe(WorkspaceExecutionSnapshot execution)
    {
        _entries.Add(new ImageMetadata(
            execution.Execution,
            execution.Output.Width,
            execution.Output.Height,
            execution.Output.Pixels.Average(pixel => pixel)));
    }
}

public sealed class InMemoryWorkspaceStateFacility : IWorkspaceFacility
{
    public FacilityDescriptor Descriptor { get; } = new(
        CanonicalName.Parse("Brontide:Workspace.SharedState"),
        CanonicalName.Parse("Brontide:ReferenceStack"),
        ImageWorkspaceFacilityContracts.WorkspaceState,
        true);

    public WorkspaceExecutionSnapshot? Current { get; private set; }

    public void Observe(WorkspaceExecutionSnapshot execution) => Current = execution;
}

public sealed record FacilityAdoption(ComponentDependency Dependency, DependencyResolution Resolution);

public sealed record ProviderSubstitution(
    CanonicalName Contract,
    FacilityDescriptor Previous,
    FacilityDescriptor Replacement,
    ComponentDependency ReplacementDependency,
    string StateHandoff,
    ImmutableArray<string> CrossedBoundaries);

public sealed record WorkspaceExecutionResult(
    ExecutionResult Core,
    ImageFrame Output,
    StructuredExecutionExplanation Explanation);

public sealed class ImageWorkspaceComposition
{
    private readonly ImageTransformRouter _router;
    private readonly AsyncLocal<bool?> _preferAcceleration = new();
    private readonly ConcurrentDictionary<ExecutionId, ProviderExecutionObservation> _observations = new();
    private readonly List<(ComponentDependency Dependency, IWorkspaceFacility Facility)> _facilities = [];
    private readonly ActorReference _editor;
    private readonly Capability _capability;

    public ImageWorkspaceComposition(IEnumerable<IImageTransformProvider>? providers = null)
    {
        _router = new ImageTransformRouter(providers ?? [new CpuImageInvertProvider(), new VectorImageInvertProvider()]);
        ActorReference editor = null!;
        ActorReference target = null!;
        Capability capability = null!;
        Domain = AuthorityDomain.Create("Brontide 0.5 image workspace", genesis =>
        {
            editor = genesis.Actor("ImageEditor");
            target = genesis.Actor("ImageTransformBoundary");
            ImageWorkspaceContracts.Register(genesis);
            genesis.Operation(
                ImageWorkspaceContracts.Invert,
                target,
                ShapeContract.For(ImageWorkspaceContracts.ImageFrame),
                ShapeContract.For(ImageWorkspaceContracts.ImageFrame),
                "invert each grayscale pixel without changing image dimensions",
                context =>
                {
                    var transformed = _router.Invert(
                        ImageFrame.FromShapeValue(context.Input),
                        _preferAcceleration.Value ?? false);
                    _observations[context.Execution.Id] = transformed.Observation;
                    return OperationEffect.SucceededAsync(
                        transformed.Output.ToShapeValue(),
                        "image inverted through the selected provider");
                });
            capability = genesis.Grant(editor, target, [ImageWorkspaceContracts.Invert]);
        });
        _editor = editor;
        _capability = capability;

        TransformationComponent = new ComponentDescriptor(
            CanonicalName.Parse("Brontide:Image.Invert.Module"),
            CanonicalName.Parse("Brontide:ReferenceStack"),
            [ImageWorkspaceContracts.Invert]);
    }

    public AuthorityDomain Domain { get; }
    public ComponentDescriptor TransformationComponent { get; }
    public IReadOnlyList<FacilityAdoption> Adoptions => _facilities
        .Select(entry => new FacilityAdoption(
            entry.Dependency,
            ResolveAgainst(entry.Dependency, entry.Facility.Descriptor)))
        .ToArray();

    public FacilityAdoption Adopt(ComponentDependency dependency, IWorkspaceFacility facility)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(facility);
        if (_facilities.Any(entry => entry.Facility.Descriptor.Component == facility.Descriptor.Component))
        {
            throw new InvalidOperationException($"Facility {facility.Descriptor.Component} is already adopted.");
        }

        var resolution = ResolveAgainst(dependency, facility.Descriptor);
        if (!resolution.IsSatisfied || resolution.Facility is null)
        {
            throw new InvalidOperationException(resolution.Explanation);
        }

        _facilities.Add((dependency, facility));
        return new FacilityAdoption(dependency, resolution);
    }

    public ProviderSubstitution Replace(
        CanonicalName contract,
        ComponentDependency replacementDependency,
        IWorkspaceFacility replacement,
        string stateHandoff,
        params string[] crossedBoundaries)
    {
        var index = _facilities.FindIndex(entry => entry.Facility.Descriptor.Contract == contract);
        if (index < 0)
        {
            throw new InvalidOperationException($"No active facility provides {contract}.");
        }

        if (replacement.Descriptor.Contract != contract)
        {
            throw new InvalidOperationException("A replacement must provide the same declared facility contract.");
        }

        var resolution = ResolveAgainst(replacementDependency, replacement.Descriptor);
        if (!resolution.IsSatisfied || resolution.Facility is null)
        {
            throw new InvalidOperationException(resolution.Explanation);
        }

        var previous = _facilities[index].Facility.Descriptor;
        _facilities[index] = (replacementDependency, replacement);
        return new ProviderSubstitution(
            contract,
            previous,
            replacement.Descriptor,
            replacementDependency,
            stateHandoff,
            crossedBoundaries.ToImmutableArray());
    }

    public async ValueTask<WorkspaceExecutionResult> ExecuteAsync(
        ImageFrame input,
        bool preferAcceleration = false)
    {
        var previousPreference = _preferAcceleration.Value;
        _preferAcceleration.Value = preferAcceleration;
        var stopwatch = Stopwatch.StartNew();
        ExecutionResult core;
        try
        {
            core = await Domain.ExecuteAsync(
                _editor,
                ImageWorkspaceContracts.Invert,
                _capability,
                input.ToShapeValue()).ConfigureAwait(false);
        }
        finally
        {
            _preferAcceleration.Value = previousPreference;
        }

        stopwatch.Stop();
        if (!_observations.TryRemove(core.Execution.Id, out var observation))
        {
            throw new InvalidOperationException("The selected provider did not record an operational observation.");
        }

        if (core.Outcome.Result is null)
        {
            throw new InvalidOperationException($"Image transformation did not return a result: {core.Outcome.Message}");
        }

        var output = ImageFrame.FromShapeValue(core.Outcome.Result);
        var explanation = new StructuredExecutionExplanation(
            core.Execution.Id,
            core.Execution.Operation,
            observation,
            core.Outcome.Status,
            stopwatch.Elapsed,
            core.Events.Length + 1,
            core.Outcome.Message);
        var snapshot = new WorkspaceExecutionSnapshot(core.Execution.Id, input, output, explanation);
        foreach (var facility in _facilities)
        {
            facility.Facility.Observe(snapshot);
        }

        return new WorkspaceExecutionResult(core, output, explanation);
    }

    private static DependencyResolution ResolveAgainst(
        ComponentDependency dependency,
        FacilityDescriptor facility)
    {
        var catalog = new CompositionCatalog();
        catalog.Add(facility);
        return catalog.Resolve(dependency);
    }
}

public sealed record FacilityEvidence(
    int HistoryEntries,
    int ReferenceMetadataEntries,
    int ReplacementMetadataEntries,
    int SearchableMetadataMatches,
    bool WorkspaceStateAvailable);

public sealed record ImageWorkspaceShowcaseResult(
    AuthorityDomain Domain,
    ComponentDescriptor SimpleTransformation,
    WorkspaceExecutionResult StageOne,
    ImmutableArray<FacilityAdoption> StageTwoAdoptions,
    WorkspaceExecutionResult StageTwo,
    FacilityEvidence StageTwoEvidence,
    ProviderSubstitution StageThreeSubstitution,
    WorkspaceExecutionResult StageThree,
    WorkspaceExecutionResult StageFour,
    FacilityEvidence FinalEvidence,
    ComponentDescriptor BoxedApplication,
    ImmutableArray<string> OutstandingCrossStackEvidence,
    ImmutableArray<string> ExperimentalSidelineProjects);

public static class ImageWorkspaceShowcase
{
    public static async ValueTask<ImageWorkspaceShowcaseResult> RunAsync()
    {
        var composition = new ImageWorkspaceComposition();
        var smallImage = new ImageFrame(4, 4, Enumerable.Range(0, 16).Select(value => (byte)(value * 8)));
        var stageOne = await composition.ExecuteAsync(smallImage).ConfigureAwait(false);

        var history = new InMemoryExecutionHistoryFacility();
        var referenceMetadata = new InMemoryMetadataFacility(
            CanonicalName.Parse("Brontide:Workspace.Metadata"),
            CanonicalName.Parse("Brontide:ReferenceStack"),
            true);
        var workspace = new InMemoryWorkspaceStateFacility();
        var adoptions = ImmutableArray.Create(
            composition.Adopt(
                ComponentDependency.RequireProfile(ImageWorkspaceFacilityContracts.RetainedExecutionHistory),
                history),
            composition.Adopt(
                ComponentDependency.RequireContract(ImageWorkspaceFacilityContracts.SearchableMetadata),
                referenceMetadata),
            composition.Adopt(
                ComponentDependency.PreferSystemProvider(ImageWorkspaceFacilityContracts.WorkspaceState),
                workspace));
        var stageTwo = await composition.ExecuteAsync(smallImage).ConfigureAwait(false);
        var stageTwoEvidence = new FacilityEvidence(
            history.Entries.Count,
            referenceMetadata.Entries.Count,
            0,
            referenceMetadata.SearchByDimensions(smallImage.Width, smallImage.Height).Count(),
            workspace.Current is not null);

        var replacementMetadata = new InMemoryMetadataFacility(
            CanonicalName.Parse("Example:Workspace.Metadata"),
            CanonicalName.Parse("Example:MetadataProvider"),
            false);
        var substitution = composition.Replace(
            ImageWorkspaceFacilityContracts.SearchableMetadata,
            ComponentDependency.RequireProvider(
                ImageWorkspaceFacilityContracts.SearchableMetadata,
                CanonicalName.Parse("Example:MetadataProvider")),
            replacementMetadata,
            "No hidden handoff: prior searchable entries remain with the old provider; new executions populate the replacement.",
            "Brontide.Reference composition → Example provider boundary");
        var stageThree = await composition.ExecuteAsync(smallImage).ConfigureAwait(false);

        var largeImage = new ImageFrame(
            64,
            64,
            Enumerable.Range(0, 64 * 64).Select(value => (byte)(value % 256)));
        var stageFour = await composition.ExecuteAsync(largeImage, preferAcceleration: true).ConfigureAwait(false);
        var finalEvidence = new FacilityEvidence(
            history.Entries.Count,
            referenceMetadata.Entries.Count,
            replacementMetadata.Entries.Count,
            replacementMetadata.SearchByDimensions(largeImage.Width, largeImage.Height).Count(),
            workspace.Current is not null);

        var boxed = ComponentDescriptor.Boxed(
            CanonicalName.Parse("Example:LegacyImageEditor"),
            CanonicalName.Parse("Example:ConventionalHost"),
            OperationReference.Parse("Example:ImageEditor.Open"));

        return new ImageWorkspaceShowcaseResult(
            composition.Domain,
            composition.TransformationComponent,
            stageOne,
            adoptions,
            stageTwo,
            stageTwoEvidence,
            substitution,
            stageThree,
            stageFour,
            finalEvidence,
            boxed,
            [
                "real Brontide.Minimal Component interchange",
                "cross-machine or cross-authority-domain binding"
            ],
            [
                "GPU execution with explicit eligibility, compilation, host/device copies, dispatch, failure, and fallback evidence"
            ]);
    }
}
