using Brontide.Reference.Experimental.Binding.Portable;
using Brontide.Reference.Experimental.ComponentManagement;
using NUnit.Framework;

namespace Brontide.Reference.Studio.Tests;

[TestFixture]
public sealed class ComponentBindingIntegrationTests
{
    private static readonly DefinitionId Consumer = DefinitionId.Create("def.test.cooling-consumer");
    private static readonly DefinitionId Provider = DefinitionId.Create("def.test.cooling-provider");
    private static readonly RequirementId Requirement = RequirementId.Create("req.cooling");
    private static readonly ContractId Contract = ContractId.Create("brontide.fake.cooling");
    private static readonly VersionLiteral Version = VersionLiteral.Create("1.0");

    [Test]
    public void Completed_direct_one_to_one_resolution_enters_portable_preflight()
    {
        var resolution = Resolve(Cardinality.Parse("1..1"));
        var member = resolution.Generation!.ProviderSets.Single().Members.Single();

        var result = ComponentBindingIntegration.Prepare(resolution, Selection(member));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsPrepared, Is.True);
            Assert.That(result.Failure, Is.Null);
            Assert.That(result.Member!.Stage, Is.EqualTo(PortableCompositionStage.LocalInitialisation));
            Assert.That(result.Member.Plan, Is.Null, "Preflight must not invent a negotiated plan.");
            Assert.That(result.Member.Fact("bindingScope"), Is.EqualTo("scope.cooling"));
            Assert.That(result.Member.Fact("selectedProvision"), Is.EqualTo(CoolingPortableFixture.Provider.ToString()));
            Assert.That(resolution.Effects, Is.EqualTo(Cm2EffectObservation.None));
        });
    }

    [Test]
    public void Explicit_mapping_cannot_name_a_different_occurrence()
    {
        var resolution = Resolve(Cardinality.Parse("1..1"));
        var member = resolution.Generation!.ProviderSets.Single().Members.Single();
        var selection = Selection(member) with { Occurrence = OccurrenceId.Create("occ.unselected") };

        var result = ComponentBindingIntegration.Prepare(resolution, selection);

        Assert.Multiple(() =>
        {
            Assert.That(result.Member, Is.Null);
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentBindingIntegrationFailureKind.SelectionMismatch));
            Assert.That(result.Failure.Code, Is.EqualTo("selection-mismatch"));
        });
    }

    [Test]
    public void Wider_provider_set_is_refused_instead_of_narrowed()
    {
        var resolution = Resolve(Cardinality.Parse("1..2"));
        var member = resolution.Generation!.ProviderSets.Single().Members.Single();

        var result = ComponentBindingIntegration.Prepare(resolution, Selection(member));

        Assert.Multiple(() =>
        {
            Assert.That(result.Member, Is.Null);
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentBindingIntegrationFailureKind.CardinalityUnsupported));
            Assert.That(result.Failure.Code, Is.EqualTo("cardinality-unsupported"));
        });
    }

    [Test]
    public void Refused_resolution_never_reaches_portable_preflight()
    {
        var resolution = new FakeGenerationResolver().Resolve(Request(Cardinality.Parse("1..1")) with
        {
            Candidates = Array.Empty<ResolutionCandidate>(),
        });
        var synthetic = new ProviderSetMember(
            Provider,
            OccurrenceId.Create("occ.synthetic"),
            null,
            PublisherId.Create("pub.test"),
            null,
            false,
            Array.Empty<EvidenceId>(),
            Array.Empty<string>(),
            "failure.synthetic",
            null);

        var result = ComponentBindingIntegration.Prepare(resolution, Selection(synthetic));

        Assert.Multiple(() =>
        {
            Assert.That(result.Member, Is.Null);
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentBindingIntegrationFailureKind.ResolutionNotComplete));
            Assert.That(resolution.Effects, Is.EqualTo(Cm2EffectObservation.None));
        });
    }

    [Test]
    public void Missing_endpoint_designation_is_refused_before_portable_preflight()
    {
        var resolution = Resolve(Cardinality.Parse("1..1"));
        var mapping = Selection(resolution.Generation!.ProviderSets.Single().Members.Single()) with
        {
            ProviderEndpoint = string.Empty,
        };

        var result = ComponentBindingIntegration.Prepare(resolution, mapping);

        Assert.Multiple(() =>
        {
            Assert.That(result.Member, Is.Null);
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentBindingIntegrationFailureKind.MappingInvalid));
            Assert.That(result.Failure.Code, Is.EqualTo("endpoint-invalid"));
        });
    }

    [Test]
    public async Task Singleton_lifecycle_derives_cm4_stages_and_releases_only_after_active()
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        var request = RuntimeRequest(Plan(occurrence)) with
        {
            StageOutcomes =
            [
                new(
                    Plan(occurrence).Groups.Single().Group,
                    occurrence,
                    ActivationStage.LocalInitialisation,
                    false,
                    "untrusted caller claim"),
            ],
        };
        var conversation = DirectCooling(CoolingPortableFixture.Contract);

        var result = await ComponentBindingLifecycle.ActivateAsync(
            resolution,
            selection,
            request,
            conversation);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsActive, Is.True);
            Assert.That(result.Runtime!.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.Active));
            Assert.That(result.Runtime.Observation.Effects.Released, Is.True);
            Assert.That(result.Runtime.Observation.Effects.CapabilityGranted, Is.False);
            Assert.That(result.Member!.Stage, Is.EqualTo(PortableCompositionStage.Released));
            Assert.That(result.Member.Plan, Is.Not.Null);
        });
    }

    [Test]
    public async Task Cm4_preflight_refusal_prevents_provider_contact()
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        var request = RuntimeRequest(Plan(occurrence)) with
        {
            Release = new(ReleaseId.Create("release.integration"), ReleaseFailureMoment.BeforeCutover),
        };

        var result = await ComponentBindingLifecycle.ActivateAsync(
            resolution,
            selection,
            request,
            DirectCooling(CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentBindingLifecycleFailureKind.RuntimeRefusedBeforeStart));
            Assert.That(result.Runtime!.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.ReleaseFailedBeforeCutover));
            Assert.That(result.Member!.Stage, Is.EqualTo(PortableCompositionStage.LocalInitialisation));
            Assert.That(result.Member.Plan, Is.Null);
        });
    }

    [Test]
    public async Task Unsupported_activation_group_is_refused_before_provider_contact()
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        var extra = OccurrenceId.Create("occ.extra");

        var result = await ComponentBindingLifecycle.ActivateAsync(
            resolution,
            selection,
            RuntimeRequest(Plan(occurrence, extra)),
            DirectCooling(CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentBindingLifecycleFailureKind.PlanUnsupported));
            Assert.That(result.Runtime, Is.Null);
            Assert.That(result.Member!.Stage, Is.EqualTo(PortableCompositionStage.LocalInitialisation));
        });
    }

    [Test]
    public async Task Activation_plan_cannot_replace_the_cbi1_selected_occurrence()
    {
        var (resolution, selection, _) = LifecycleInput();

        var result = await ComponentBindingLifecycle.ActivateAsync(
            resolution,
            selection,
            RuntimeRequest(Plan(OccurrenceId.Create("occ.replacement"))),
            DirectCooling(CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentBindingLifecycleFailureKind.PlanUnsupported));
            Assert.That(result.Runtime, Is.Null);
            Assert.That(result.Member!.Stage, Is.EqualTo(PortableCompositionStage.LocalInitialisation));
            Assert.That(result.Member.Plan, Is.Null);
        });
    }

    [Test]
    public async Task Portable_interconnection_refusal_is_projected_as_cm4_establishment_failure()
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        var substituted = CoolingPortableFixture.Contract with
        {
            Provider = PortableProviderReference.Parse("brontide.fake.substituted", 1),
        };

        var result = await ComponentBindingLifecycle.ActivateAsync(
            resolution,
            selection,
            RuntimeRequest(Plan(occurrence)),
            DirectCooling(substituted));

        Assert.Multiple(() =>
        {
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentBindingLifecycleFailureKind.PortableInterconnectionRefused));
            Assert.That(result.Runtime!.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.EstablishmentFailed));
            Assert.That(result.Member!.Stage, Is.Not.EqualTo(PortableCompositionStage.Released));
        });
    }

    private static ResolutionOutcome Resolve(Cardinality cardinality) =>
        new FakeGenerationResolver().Resolve(Request(cardinality));

    private static (ResolutionOutcome Resolution, ComponentBindingSelection Selection, OccurrenceId Occurrence) LifecycleInput()
    {
        var resolution = Resolve(Cardinality.Parse("1..1"));
        var member = resolution.Generation!.ProviderSets.Single().Members.Single();
        return (resolution, Selection(member), member.Occurrence);
    }

    private static ActivationGroupPlan Plan(params OccurrenceId[] occurrences)
    {
        var members = occurrences.Select(occurrence => new ActivationGroupMember(
            occurrence,
            DefinitionId.Create($"def.{occurrence.Value}"),
            RegionId.Create("region.integration"),
            new[] { new ProvidedContract(Contract, Version) },
            Array.Empty<LifecycleInputId>(),
            Array.Empty<LifecycleInputId>(),
            Array.Empty<OccurrenceId>())).ToArray();
        var outcome = new FakeActivationGroupPlanner().Plan(new(
            ActivationGroupRequestId.Create("group.integration"),
            GenerationId.Create("gen.lifecycle"),
            RestartScopeId.Create("restart.lifecycle"),
            members,
            Array.Empty<ActivationDependency>(),
            Array.Empty<LifecycleProtocolDeclaration>(),
            Array.Empty<RegionCrossingDeclaration>()));
        return outcome.Plan!;
    }

    private static ActivationRuntimeRequest RuntimeRequest(ActivationGroupPlan plan)
    {
        var retained = GenerationId.Create("gen.retained");
        return new(
            ActivationAttemptId.Create("activation.integration"),
            plan,
            plan.RestartScope,
            retained,
            new[] { new ActiveScopeSnapshot(plan.RestartScope, retained, RuntimeScopeStatus.Active) },
            null,
            Array.Empty<MemberStageOutcome>(),
            Array.Empty<RuntimeInteractionAttempt>(),
            Array.Empty<BindingExerciseDeclaration>(),
            new ReleaseDeclaration(ReleaseId.Create("release.integration"), ReleaseFailureMoment.None),
            RollbackAvailability.Available,
            RetainedGenerationDisposition.TerminateAfterRelease,
            null);
    }

    private static IPortableProviderConversation DirectCooling(PortableContractDocument document) =>
        new PortableDirectConversation(new PortableProviderEndpoint(
            document,
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            PortableRealization.FixedDirectCall));

    private static ResolutionRequest Request(Cardinality cardinality)
    {
        var requirement = new ResolutionRequirement(
            Requirement,
            Contract,
            Version,
            BindingScopeId.Create("scope.cooling"),
            cardinality,
            false,
            ProviderExposure.Distinct,
            null,
            Array.Empty<DefinitionConstraint>(),
            null,
            null,
            false,
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            null,
            Array.Empty<string>(),
            Array.Empty<TopologyRelation>());
        var consumer = new ResolutionDefinition(
            Consumer,
            PublisherId.Create("pub.test"),
            Array.Empty<ProvidedContract>(),
            new[] { requirement },
            Array.Empty<CompositionParameterDeclaration>(),
            Array.Empty<ActivationParameterDeclaration>(),
            Array.Empty<string>());
        var provider = new ResolutionDefinition(
            Provider,
            PublisherId.Create("pub.test"),
            new[] { new ProvidedContract(Contract, Version) },
            Array.Empty<ResolutionRequirement>(),
            Array.Empty<CompositionParameterDeclaration>(),
            Array.Empty<ActivationParameterDeclaration>(),
            Array.Empty<string>());
        var candidate = new ResolutionCandidate(
            Provider,
            SourceId.Create("src.test"),
            PublisherId.Create("pub.test"),
            PackageId.Create("pkg.test"),
            new[] { new ProvidedContract(Contract, Version) },
            false,
            new SharingDeclaration(true, true, true),
            new[]
            {
                new CandidatePolicyObservation(CandidatePolicyDomain.Trust, true, "trusted test candidate"),
            },
            new[] { EvidenceId.Create("ev.test") },
            Array.Empty<string>(),
            "failure.test",
            null);
        return new ResolutionRequest(
            ResolutionRequestId.Create("resolution.integration"),
            GenerationId.Create("gen.integration"),
            null,
            RestartScopeId.Create("restart.integration"),
            new[] { Consumer },
            new[] { consumer, provider },
            new[] { candidate },
            Array.Empty<ActivatedOccurrenceEntry>(),
            Array.Empty<OccupiedBindingEntry>(),
            Array.Empty<PreferenceEntry>(),
            Array.Empty<BindingId>(),
            Array.Empty<CompositionParameterSelection>(),
            Array.Empty<ActivationParameterValue>(),
            Array.Empty<ProviderPreselection>(),
            Array.Empty<PortEnvelope>(),
            Array.Empty<TopologyPolicyInput>());
    }

    private static ComponentBindingSelection Selection(ProviderSetMember member) =>
        new(
            Requirement,
            member.Definition,
            member.Occurrence,
            CoolingPortableFixture.Component,
            CoolingPortableFixture.Provider,
            "reference-component-host",
            "cooling-provider",
            CoolingPortableFixture.Contract);
}
