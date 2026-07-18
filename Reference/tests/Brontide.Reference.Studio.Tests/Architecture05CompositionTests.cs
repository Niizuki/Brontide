using Brontide.Reference.Core;
using Brontide.Reference.Experimental.Composition;

namespace Brontide.Reference.Studio.Tests;

[Category("Experimental")]
public sealed class Architecture05CompositionTests
{
    [Test]
    public async Task Image_workspace_keeps_the_module_simple_while_facilities_are_adopted_independently()
    {
        var result = await ImageWorkspaceShowcase.RunAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result.SimpleTransformation.Operations, Has.Length.EqualTo(1));
            Assert.That(result.SimpleTransformation.Dependencies, Is.Empty);
            Assert.That(result.SimpleTransformation.ExecutionClaims, Is.Empty);
            Assert.That(result.StageOne.Core.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(result.StageOne.Explanation.Provider.Placement, Is.EqualTo(ExecutionPlacement.InProcessCpu));
            Assert.That(result.StageTwoAdoptions.Select(adoption => adoption.Dependency.Strength), Is.EqualTo(new[]
            {
                DependencyStrength.RequiredProfile,
                DependencyStrength.RequiredContract,
                DependencyStrength.PreferredSystemProvider
            }));
            Assert.That(result.StageTwoAdoptions.All(adoption => adoption.Resolution.IsSatisfied), Is.True);
            Assert.That(result.StageTwoEvidence.HistoryEntries, Is.EqualTo(1));
            Assert.That(result.StageTwoEvidence.ReferenceMetadataEntries, Is.EqualTo(1));
            Assert.That(result.StageTwoEvidence.SearchableMetadataMatches, Is.EqualTo(1));
            Assert.That(result.StageTwoEvidence.WorkspaceStateAvailable, Is.True);
        });
    }

    [Test]
    public async Task Provider_substitution_and_vector_optimisation_remain_operationally_visible()
    {
        var result = await ImageWorkspaceShowcase.RunAsync();
        var vector = result.StageFour.Explanation.Provider;

        Assert.Multiple(() =>
        {
            Assert.That(result.StageOne.Explanation.Operation, Is.EqualTo(ImageWorkspaceContracts.Invert));
            Assert.That(result.StageFour.Explanation.Operation, Is.EqualTo(ImageWorkspaceContracts.Invert));
            Assert.That(result.StageThreeSubstitution.Replacement.Provider.ToString(),
                Is.EqualTo("Example:MetadataProvider"));
            Assert.That(result.StageThreeSubstitution.ReplacementDependency.Strength,
                Is.EqualTo(DependencyStrength.RequiredAuthoredProvider));
            Assert.That(result.StageThreeSubstitution.StateHandoff, Does.Contain("No hidden handoff"));
            Assert.That(result.StageThreeSubstitution.CrossedBoundaries, Is.Not.Empty);
            Assert.That(vector.Placement, Is.EqualTo(ExecutionPlacement.VectorAccelerator));
            Assert.That(vector.ClaimsUsed, Is.EquivalentTo(OptimizationEligibility.AcceleratorClaims));
            Assert.That(vector.CrossedBoundaries, Is.Not.Empty);
            Assert.That(vector.BatchSize, Is.GreaterThan(1));
            Assert.That(vector.Copies, Is.GreaterThan(0));
            Assert.That(vector.FailureDomain, Is.Not.Empty);
            Assert.That(result.StageOne.Output.Pixels[0], Is.EqualTo(byte.MaxValue));
            Assert.That(result.StageOne.Output.Pixels[1], Is.EqualTo(247));
            Assert.That(result.StageFour.Output.Pixels[0], Is.EqualTo(byte.MaxValue));
            Assert.That(result.StageFour.Output.Pixels[1], Is.EqualTo(254));
            Assert.That(result.FinalEvidence.HistoryEntries, Is.EqualTo(3));
            Assert.That(result.FinalEvidence.ReferenceMetadataEntries, Is.EqualTo(1));
            Assert.That(result.FinalEvidence.ReplacementMetadataEntries, Is.EqualTo(2));
            Assert.That(result.FinalEvidence.SearchableMetadataMatches, Is.EqualTo(1));
            Assert.That(result.BoxedApplication.BoundaryVisibility,
                Is.EqualTo(ComponentBoundaryVisibility.OpaqueBox));
            Assert.That(result.BoxedApplication.Dependencies, Is.Empty);
            Assert.That(result.OutstandingCrossStackEvidence, Has.None.Contains("GPU"));
            Assert.That(result.ExperimentalSidelineProjects, Has.One.Contains("GPU execution"));
        });
    }

    [Test]
    public void A_conventionally_hosted_opaque_box_needs_no_internal_dependencies_or_brontide_operation()
    {
        var boxed = ComponentDescriptor.Boxed(
            CanonicalName.Parse("Example:ConventionalApplication"),
            CanonicalName.Parse("Example:ConventionalHost"));

        Assert.Multiple(() =>
        {
            Assert.That(boxed.BoundaryVisibility, Is.EqualTo(ComponentBoundaryVisibility.OpaqueBox));
            Assert.That(boxed.Operations, Is.Empty);
            Assert.That(boxed.Dependencies, Is.Empty);
        });
    }

    [Test]
    public void Accelerator_eligibility_is_not_inferred_from_operation_or_placement()
    {
        var unclaimedAccelerator = new TestAccelerator(
            [ExecutionProperty.Vectorisable],
            shouldFail: false);
        var router = new ImageTransformRouter([new CpuImageInvertProvider(), unclaimedAccelerator]);
        var image = new ImageFrame(32, 32, Enumerable.Repeat((byte)7, 32 * 32));

        var result = router.Invert(image, preferAcceleration: true);
        var eligibility = OptimizationEligibility.ForAccelerator(unclaimedAccelerator.Descriptor);

        Assert.Multiple(() =>
        {
            Assert.That(eligibility.IsEligible, Is.False);
            Assert.That(eligibility.MissingClaims, Does.Contain(ExecutionProperty.Pure));
            Assert.That(result.Observation.Placement, Is.EqualTo(ExecutionPlacement.InProcessCpu));
            Assert.That(result.Observation.SelectionReason, Does.Contain("missing"));
        });
    }

    [Test]
    public void Accelerator_failure_uses_and_explains_the_declared_cpu_fallback()
    {
        var failingAccelerator = new TestAccelerator(
            OptimizationEligibility.AcceleratorClaims,
            shouldFail: true);
        var router = new ImageTransformRouter([new CpuImageInvertProvider(), failingAccelerator]);
        var image = new ImageFrame(32, 32, Enumerable.Repeat((byte)7, 32 * 32));

        var result = router.Invert(image, preferAcceleration: true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Observation.Placement, Is.EqualTo(ExecutionPlacement.InProcessCpu));
            Assert.That(result.Observation.FallbackFrom, Is.EqualTo(failingAccelerator.Descriptor.Name));
            Assert.That(result.Observation.Retries, Is.Zero);
            Assert.That(result.Observation.CrossedBoundaries, Is.Not.Empty);
            Assert.That(result.Observation.Copies, Is.EqualTo(2));
            Assert.That(result.Observation.SelectionReason, Does.Contain("explicit CPU fallback"));
        });
    }

    [Test]
    public void Dependency_catalog_preserves_optional_profile_and_provider_specific_meaning()
    {
        var history = new InMemoryExecutionHistoryFacility();
        var catalog = new CompositionCatalog();
        catalog.Add(history.Descriptor);

        var generic = catalog.Resolve(
            ComponentDependency.RequireContract(ImageWorkspaceFacilityContracts.ExecutionHistory));
        var profile = catalog.Resolve(
            ComponentDependency.RequireProfile(ImageWorkspaceFacilityContracts.RetainedExecutionHistory));
        var optional = catalog.Resolve(
            ComponentDependency.PreferSystemProvider(ImageWorkspaceFacilityContracts.WorkspaceState));
        var providerSpecific = catalog.Resolve(
            ComponentDependency.RequireProvider(
                ImageWorkspaceFacilityContracts.ExecutionHistory,
                CanonicalName.Parse("Example:WrongProvider")));

        Assert.Multiple(() =>
        {
            Assert.That(generic.IsSatisfied, Is.True);
            Assert.That(profile.IsSatisfied, Is.True);
            Assert.That(optional.IsSatisfied, Is.True);
            Assert.That(optional.Facility, Is.Null);
            Assert.That(providerSpecific.IsSatisfied, Is.False);
        });
    }

    private sealed class TestAccelerator : IImageTransformProvider
    {
        private readonly bool _shouldFail;

        public TestAccelerator(IEnumerable<ExecutionProperty> claims, bool shouldFail)
        {
            _shouldFail = shouldFail;
            Descriptor = new ComponentDescriptor(
                CanonicalName.Parse("Example:TestAccelerator"),
                CanonicalName.Parse("Example:TestProvider"),
                [ImageWorkspaceContracts.Invert],
                executionClaims: claims);
        }

        public ComponentDescriptor Descriptor { get; }

        public ProviderOperationalCharacteristics Characteristics { get; } = new(
            ExecutionPlacement.VectorAccelerator,
            "test buffer",
            ["host → test accelerator"],
            32,
            1,
            "test accelerator");

        public ImageFrame Invert(ImageFrame input) => _shouldFail
            ? throw new InvalidOperationException("test provider failed")
            : new ImageFrame(input.Width, input.Height, input.Pixels);
    }
}
