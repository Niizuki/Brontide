using Brontide.Reference.Experimental.Binding.Portable;
using Brontide.Reference.Experimental.ComponentManagement;
using NUnit.Framework;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

[TestFixture]
public sealed class ComponentBindingIntegrationTests
{
    private static readonly DefinitionId Consumer = DefinitionId.Create("def.test.cooling-consumer");
    private static readonly DefinitionId Provider = DefinitionId.Create("def.test.cooling-provider");
    private static readonly RequirementId Requirement = RequirementId.Create("req.cooling");
    private static readonly ContractId Contract = ContractId.Create("brontide.fake.cooling");
    private static readonly VersionLiteral Version = VersionLiteral.Create("1.0");
    private static readonly ActorId Participant = ActorId.Create("actor.cooling-provider");
    private static readonly ActorId Target = ActorId.Create("actor.cooling-target");
    private static readonly EvidenceId AuthorityEvidence = EvidenceId.Create("evidence.cooling-provider");
    private static readonly IssuerId AuthorityIssuer = IssuerId.Create("issuer.integration-host");
    private static readonly RelationshipRequestId Relationship = RelationshipRequestId.Create("relationship.cooling-provider");
    private static readonly AuthorityRequestId Authority = AuthorityRequestId.Create("authority.cooling-control");
    private static readonly CapabilityId Capability = CapabilityId.Create("capability.cooling-control");
    private static readonly OperationId Operation = OperationId.Create("cooling.set-enabled");
    private static readonly CapabilityScopeId AuthorityScope = CapabilityScopeId.Create("scope.cooling-session");
    private static readonly DateTimeOffset EvaluationTime =
        new(2026, 7, 30, 10, 0, 0, TimeSpan.Zero);
    private static readonly ActorId Supervisor = ActorId.Create("actor.cooling-supervisor");
    private static readonly EvidenceId SupervisorEvidence = EvidenceId.Create("evidence.cooling-supervisor");
    private static readonly RelationshipRequestId SupervisorRelationship =
        RelationshipRequestId.Create("relationship.cooling-supervisor");
    private static readonly AuthorityRequestId ReportAuthority = AuthorityRequestId.Create("authority.cooling-report");
    private static readonly AuthorityRequestId AuditAuthority = AuthorityRequestId.Create("authority.cooling-audit");
    private static readonly CapabilityId ReportCapability = CapabilityId.Create("capability.cooling-report");
    private static readonly CapabilityId AuditCapability = CapabilityId.Create("capability.cooling-audit");
    private static readonly OperationId ReportOperation = OperationId.Create("cooling.read-state");
    private static readonly OperationId AuditOperation = OperationId.Create("cooling.read-log");
    private static readonly LocalActorReferenceId ProviderLocalActor =
        LocalActorReferenceId.Create("local.cooling-provider");
    private static readonly LocalActorReferenceId SupervisorLocalActor =
        LocalActorReferenceId.Create("local.cooling-supervisor");
    private static readonly ActorId Observer = ActorId.Create("actor.cooling-observer");
    private static readonly EvidenceId ObserverEvidence = EvidenceId.Create("evidence.cooling-observer");
    private static readonly RelationshipRequestId ObserverRelationship =
        RelationshipRequestId.Create("relationship.cooling-observer");
    private static readonly AuthorityRequestId ObserveAuthority =
        AuthorityRequestId.Create("authority.cooling-observe");
    private static readonly CapabilityId ObserveCapability = CapabilityId.Create("capability.cooling-observe");
    private static readonly OperationId ObserveOperation = OperationId.Create("cooling.observe");
    private static readonly LocalActorReferenceId ObserverLocalActor =
        LocalActorReferenceId.Create("local.cooling-observer");
    private static readonly ActorId Deputy = ActorId.Create("actor.cooling-deputy");
    private static readonly EvidenceId DeputyEvidence = EvidenceId.Create("evidence.cooling-deputy");
    private static readonly RelationshipRequestId DeputyRelationship =
        RelationshipRequestId.Create("relationship.cooling-deputy");
    private static readonly AuthorityRequestId DeputyAuthority =
        AuthorityRequestId.Create("authority.cooling-deputy-audit");
    private static readonly LocalActorReferenceId DeputyLocalActor =
        LocalActorReferenceId.Create("local.cooling-deputy");
    private static readonly string[] DeclaredAuthority = ["cooling.control", "cooling.audit"];
    private static readonly RequirementId SecondaryRequirement = RequirementId.Create("req.cooling-secondary");
    private static readonly DefinitionId SecondaryProvider = DefinitionId.Create("def.test.cooling-secondary");
    private static readonly ContractId SecondaryContract = ContractId.Create("brontide.fake.cooling-secondary");
    private static readonly RequirementId TertiaryRequirement = RequirementId.Create("req.cooling-tertiary");
    private static readonly DefinitionId TertiaryProvider = DefinitionId.Create("def.test.cooling-tertiary");
    private static readonly ContractId TertiaryContract = ContractId.Create("brontide.fake.cooling-tertiary");

    /// <summary>The independent positions a membership can be drawn from, one provider each.</summary>
    private static readonly (RequirementId Requirement, DefinitionId Provider, ContractId Contract)[]
        PositionCatalog =
        [
            (Requirement, Provider, Contract),
            (SecondaryRequirement, SecondaryProvider, SecondaryContract),
            (TertiaryRequirement, TertiaryProvider, TertiaryContract),
        ];

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

    [Test]
    public async Task Exact_cm5_admission_gates_one_released_active_member()
    {
        var (resolution, selection, occurrence) = LifecycleInput();

        var result = await ComponentAuthorityIntegration.ActivateAsync(
            resolution,
            selection,
            new(occurrence, Participant),
            RuntimeRequest(Plan(occurrence)),
            Admission(),
            DirectCooling(CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsActive, Is.True);
            Assert.That(result.Authority!.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.Admitted));
            Assert.That(result.Authority.Observation.Relationships, Has.Count.EqualTo(1));
            Assert.That(result.Authority.Observation.Grants, Has.Count.EqualTo(1));
            Assert.That(result.Lifecycle!.Member!.Stage, Is.EqualTo(PortableCompositionStage.Released));
            Assert.That(result.Lifecycle.Member.Plan!.NoCapabilityTransfer, Is.True);
        });
    }

    [Test]
    public async Task Authority_mapping_mismatch_stops_before_cm5_and_portable_preflight()
    {
        var (resolution, selection, occurrence) = LifecycleInput();

        var result = await ComponentAuthorityIntegration.ActivateAsync(
            resolution,
            selection,
            new(occurrence, ActorId.Create("actor.other")),
            RuntimeRequest(Plan(occurrence)),
            Admission(),
            DirectCooling(CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentAuthorityIntegrationFailureKind.MappingInvalid));
            Assert.That(result.Authority, Is.Null);
            Assert.That(result.Lifecycle, Is.Null);
        });
    }

    [Test]
    public async Task Revoked_cm5_evidence_prevents_provider_contact()
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        var denied = Admission() with
        {
            Evidence =
            [
                Admission().Evidence.Single() with { State = AdmissionEvidenceState.Revoked },
            ],
        };

        var result = await ComponentAuthorityIntegration.ActivateAsync(
            resolution,
            selection,
            new(occurrence, Participant),
            RuntimeRequest(Plan(occurrence)),
            denied,
            DirectCooling(CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentAuthorityIntegrationFailureKind.AuthorityRefused));
            Assert.That(result.Authority!.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.Denied));
            Assert.That(result.Authority.Observation.Grants, Is.Empty);
            Assert.That(result.Lifecycle, Is.Null);
        });
    }

    [Test]
    public async Task Additional_authority_request_is_refused_before_cm5_evaluation()
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        var admission = Admission();
        var wider = admission with
        {
            Authority =
            [
                admission.Authority.Single(),
                admission.Authority.Single() with
                {
                    Request = AuthorityRequestId.Create("authority.additional"),
                },
            ],
        };

        var result = await ComponentAuthorityIntegration.ActivateAsync(
            resolution,
            selection,
            new(occurrence, Participant),
            RuntimeRequest(Plan(occurrence)),
            wider,
            DirectCooling(CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentAuthorityIntegrationFailureKind.AuthorityShapeUnsupported));
            Assert.That(result.Authority, Is.Null);
            Assert.That(result.Lifecycle, Is.Null);
        });
    }

    [Test]
    public async Task Caller_authored_cm4_binding_authority_is_refused_before_cm5_evaluation()
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        var runtime = RuntimeRequest(Plan(occurrence)) with
        {
            BindingExercises =
            [
                new(
                    BindingExerciseId.Create("exercise.caller"),
                    BindingId.Create("binding.caller"),
                    occurrence,
                    occurrence,
                    SourceId.Create("source.caller"),
                    BindingExposureKind.Distinct,
                    null,
                    RoutingDecisionId.Create("routing.caller"),
                    true,
                    BindingDeliveryResult.Delivered,
                    null),
            ],
        };

        var result = await ComponentAuthorityIntegration.ActivateAsync(
            resolution,
            selection,
            new(occurrence, Participant),
            runtime,
            Admission(),
            DirectCooling(CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentAuthorityIntegrationFailureKind.AuthorityShapeUnsupported));
            Assert.That(result.Authority, Is.Null);
            Assert.That(result.Lifecycle, Is.Null);
        });
    }

    [Test]
    public async Task Structurally_invalid_cm5_request_prevents_provider_contact()
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        var baseline = Admission();
        var invalid = baseline with
        {
            Evidence = [baseline.Evidence.Single(), baseline.Evidence.Single()],
        };

        var result = await ComponentAuthorityIntegration.ActivateAsync(
            resolution,
            selection,
            new(occurrence, Participant),
            RuntimeRequest(Plan(occurrence)),
            invalid,
            DirectCooling(CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentAuthorityIntegrationFailureKind.AuthorityRefused));
            Assert.That(result.Authority!.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.InvalidRequest));
            Assert.That(result.Lifecycle, Is.Null);
        });
    }

    [Test]
    public async Task Portable_failure_remains_inactive_after_cm5_admission()
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        var substituted = CoolingPortableFixture.Contract with
        {
            Provider = PortableProviderReference.Parse("brontide.fake.substituted", 1),
        };

        var result = await ComponentAuthorityIntegration.ActivateAsync(
            resolution,
            selection,
            new(occurrence, Participant),
            RuntimeRequest(Plan(occurrence)),
            Admission(),
            DirectCooling(substituted));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsActive, Is.False);
            Assert.That(result.Authority!.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.Admitted));
            Assert.That(result.Failure!.Kind, Is.EqualTo(ComponentAuthorityIntegrationFailureKind.LifecycleRefused));
            Assert.That(result.Lifecycle!.Member!.Stage, Is.Not.EqualTo(PortableCompositionStage.Released));
        });
    }

    [Test]
    public async Task Shared_cbi4_vectors_pin_the_complete_native_profiles()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi4-integrated-comparison-vectors.json")));
        var actual = new Dictionary<string, (string Profile, string Digest)>(StringComparer.Ordinal);
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var id = vector.GetProperty("id").GetString()!;
            var result = await ComparisonResult(id);
            var profile = ComponentAuthorityComparison.Profile(id, result);
            actual.Add(id, (profile, ComponentAuthorityComparison.Digest(profile)));
        }

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var id = vector.GetProperty("id").GetString()!;
            var expected = vector.GetProperty("expectedProfileSha256").GetString();
            using var profile = JsonDocument.Parse(actual[id].Profile);
            Assert.Multiple(() =>
            {
                Assert.That(actual[id].Digest, Is.EqualTo(expected), id);
                Assert.That(
                    profile.RootElement.GetProperty("active").GetBoolean(),
                    Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()),
                    id);
                var expectedFailure = vector.GetProperty("expectedIntegrationFailure");
                var actualFailure = profile.RootElement.GetProperty("integrationFailure");
                Assert.That(
                    actualFailure.ValueKind == JsonValueKind.Null
                        ? null
                        : actualFailure.GetProperty("kind").GetString(),
                    Is.EqualTo(
                        expectedFailure.ValueKind == JsonValueKind.Null
                            ? null
                            : expectedFailure.GetString()),
                    id);
            });
        }
    }

    [Test]
    public async Task Shared_cbi5_vectors_revalidate_or_close_the_released_member()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi5-authority-withdrawal-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, member, handler) = await RevalidationResult(scenario);
            var expectedKind = vector.GetProperty("expectedKind").GetString();
            PortableInteractionResult? afterWithdrawal = null;
            if (result.Kind != ComponentAuthorityRevalidationKind.Continued)
            {
                afterWithdrawal = await member.InvokeAsync(
                    CoolingPortableFixture.SetEnabled,
                    CoolingPortableFixture.CommandV1,
                    CoolingPortableFixture.Command("primary", enabled: true),
                    PortableConstraint.AllOf(
                        PortableConstraint.Atom(PortableTruth.Satisfied),
                        PortableConstraint.Atom(PortableTruth.Satisfied)));
            }

            Assert.Multiple(() =>
            {
                Assert.That(RevalidationKindToken(result.Kind), Is.EqualTo(expectedKind), scenario);
                Assert.That(
                    result.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                    scenario);
                Assert.That(
                    member.Stage,
                    Is.EqualTo(
                        result.Kind == ComponentAuthorityRevalidationKind.Continued
                            ? PortableCompositionStage.Released
                            : PortableCompositionStage.Retired),
                    scenario);
                Assert.That(
                    result.Replacement,
                    result.Kind == ComponentAuthorityRevalidationKind.Withdrawn
                        ? Is.Not.Null
                        : Is.Null,
                    scenario);
                Assert.That(
                    afterWithdrawal?.Category,
                    result.Kind == ComponentAuthorityRevalidationKind.Continued
                        ? Is.Null
                        : Is.EqualTo(PortableProtocolCategory.StateViolation),
                    scenario);
                Assert.That(handler.ProviderEffectCount, Is.Zero, scenario);
            });
        }
    }

    [Test]
    public async Task Refused_cbi3_result_cannot_be_revalidated_as_active()
    {
        var unavailable = new ComponentAuthorityIntegrationResult(null, null, null);
        var result = await ComponentAuthorityRevalidation.RevalidateAsync(
            unavailable,
            Admission(),
            "authority unavailable");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ComponentAuthorityRevalidationKind.ActivationUnavailable));
            Assert.That(result.CurrentAuthority, Is.Null);
            Assert.That(result.Replacement, Is.Null);
        });
    }

    [Test]
    public async Task Shared_cbi6_vectors_gate_the_participant_set_before_provider_contact()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi6-participant-admission-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, handler) = await ParticipantResult(scenario);
            var expectedFailure = vector.GetProperty("expectedFailureKind");
            var expectedCode = vector.GetProperty("expectedCode");

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsActive,
                    Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()),
                    scenario);
                Assert.That(
                    result.Failure is null ? null : ParticipantFailureToken(result.Failure.Kind),
                    Is.EqualTo(
                        expectedFailure.ValueKind == JsonValueKind.Null ? null : expectedFailure.GetString()),
                    scenario);
                Assert.That(
                    result.Failure?.Code,
                    Is.EqualTo(expectedCode.ValueKind == JsonValueKind.Null ? null : expectedCode.GetString()),
                    scenario);
                Assert.That(
                    result.Admissions,
                    Has.Count.EqualTo(vector.GetProperty("expectedParticipantsEvaluated").GetInt32()),
                    scenario);
                Assert.That(
                    result.Grants,
                    Has.Count.EqualTo(vector.GetProperty("expectedGrants").GetInt32()),
                    scenario);
                Assert.That(
                    result.Lifecycle,
                    result.IsActive ? Is.Not.Null : Is.Null,
                    $"{scenario}: a refused participant set must not reach the provider.");
                Assert.That(handler.ProviderEffectCount, Is.Zero, scenario);
            });
        }
    }

    [Test]
    public async Task Admitted_participant_set_holds_distinct_local_actors_and_every_grant()
    {
        var (result, _) = await ParticipantResult("cbi6-01-two-participants");
        var holders = result.Grants.Select(grant => grant.Holder).Distinct().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsActive, Is.True);
            Assert.That(result.Admissions.Select(item => item.Participant), Is.EqualTo(new[] { Participant, Supervisor }));
            Assert.That(
                result.Grants.Select(grant => grant.Request.Value),
                Is.EqualTo(new[] { Authority.Value, ReportAuthority.Value, AuditAuthority.Value }.OrderBy(item => item, StringComparer.Ordinal)));
            Assert.That(holders, Has.Length.EqualTo(2));
            Assert.That(result.Lifecycle!.Member!.Stage, Is.EqualTo(PortableCompositionStage.Released));
            Assert.That(result.Lifecycle.Member.Plan!.NoCapabilityTransfer, Is.True);
        });
    }

    [Test]
    public async Task Participant_set_size_cannot_change_any_portable_fact()
    {
        var set = ParticipantSet(SupervisorLocalActor);
        var wide = await Activate(set);
        var narrow = await Activate([set[0]]);

        Assert.Multiple(() =>
        {
            Assert.That(wide.IsActive, Is.True);
            Assert.That(narrow.IsActive, Is.True);
            Assert.That(wide.Grants, Has.Count.EqualTo(3));
            Assert.That(narrow.Grants, Has.Count.EqualTo(2));
            Assert.That(PortableFacts(wide), Is.EqualTo(PortableFacts(narrow)));
        });
    }

    [Test]
    public async Task Shared_cbi7_vectors_revalidate_or_retire_the_shared_member()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi7-participant-withdrawal-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, member, handler) = await SetRevalidationResult(scenario);
            PortableInteractionResult? afterWithdrawal = null;
            if (!result.IsActive)
            {
                afterWithdrawal = await member.InvokeAsync(
                    CoolingPortableFixture.SetEnabled,
                    CoolingPortableFixture.CommandV1,
                    CoolingPortableFixture.Command("primary", enabled: true),
                    PortableConstraint.AllOf(
                        PortableConstraint.Atom(PortableTruth.Satisfied),
                        PortableConstraint.Atom(PortableTruth.Satisfied)));
            }

            Assert.Multiple(() =>
            {
                Assert.That(
                    SetRevalidationToken(result.Kind),
                    Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                    scenario);
                Assert.That(
                    result.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                    scenario);
                Assert.That(
                    result.CurrentAuthority,
                    Has.Count.EqualTo(vector.GetProperty("expectedParticipantsEvaluated").GetInt32()),
                    scenario);
                Assert.That(
                    result.Unrenewed,
                    Has.Count.EqualTo(vector.GetProperty("expectedUnrenewed").GetInt32()),
                    scenario);
                Assert.That(
                    member.Stage,
                    Is.EqualTo(
                        result.IsActive
                            ? PortableCompositionStage.Released
                            : PortableCompositionStage.Retired),
                    scenario);
                Assert.That(
                    result.Replacement,
                    result.Kind == ComponentParticipantRevalidationKind.Withdrawn
                        ? Is.Not.Null
                        : Is.Null,
                    scenario);
                Assert.That(
                    afterWithdrawal?.Category,
                    result.IsActive ? Is.Null : Is.EqualTo(PortableProtocolCategory.StateViolation),
                    scenario);
                Assert.That(handler.ProviderEffectCount, Is.Zero, scenario);
            });
        }
    }

    [Test]
    public async Task One_participant_losing_authority_never_narrows_the_set()
    {
        var (result, member, _) = await SetRevalidationResult("cbi7-02-one-revoked");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ComponentParticipantRevalidationKind.Withdrawn));
            Assert.That(result.Unrenewed, Is.EqualTo(new[] { Supervisor }));
            Assert.That(
                result.CurrentAuthority.Single(item => item.Participant == Participant).Authority.Kind,
                Is.EqualTo(AuthorityAdmissionOutcomeKind.Admitted),
                "The unaffected participant is still admitted; that is what makes the retirement a choice.");
            Assert.That(member.Stage, Is.EqualTo(PortableCompositionStage.Retired));
        });
    }

    [Test]
    public async Task Refused_cbi6_set_cannot_be_revalidated_as_active()
    {
        var unavailable = new ComponentParticipantAdmissionResult(
            Array.Empty<ComponentParticipantObservation>(),
            Array.Empty<LocalCapabilityGrant>(),
            null,
            null);

        var result = await ComponentParticipantRevalidation.RevalidateAsync(
            unavailable,
            ParticipantSet(SupervisorLocalActor).Select(item => item.Request).ToArray(),
            "set authority unavailable");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ComponentParticipantRevalidationKind.ActivationUnavailable));
            Assert.That(result.CurrentAuthority, Is.Empty);
            Assert.That(result.Unrenewed, Is.Empty);
            Assert.That(result.Replacement, Is.Null);
        });
    }

    [Test]
    public async Task Shared_cbi8_vectors_extend_or_decline_without_disturbing_the_member()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi8-participant-extension-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, member, handler) = await ExtensionResult(scenario);
            var released = vector.GetProperty("expectedReleased").GetBoolean();

            Assert.Multiple(() =>
            {
                Assert.That(
                    ExtensionToken(result.Kind),
                    Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                    scenario);
                Assert.That(
                    result.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                    scenario);
                Assert.That(
                    result.CurrentAuthority,
                    Has.Count.EqualTo(vector.GetProperty("expectedParticipantsEvaluated").GetInt32()),
                    scenario);
                Assert.That(
                    result.InForce?.Admissions.Count ?? 0,
                    Is.EqualTo(vector.GetProperty("expectedInForceParticipants").GetInt32()),
                    scenario);
                Assert.That(
                    result.InForce?.Grants.Count ?? 0,
                    Is.EqualTo(vector.GetProperty("expectedInForceGrants").GetInt32()),
                    scenario);
                Assert.That(
                    member.Stage,
                    Is.EqualTo(
                        released ? PortableCompositionStage.Released : PortableCompositionStage.Retired),
                    scenario);
                Assert.That(
                    result.InForce,
                    released ? Is.Not.Null : Is.Null,
                    $"{scenario}: a set is in force exactly while the member is released.");
                Assert.That(handler.ProviderEffectCount, Is.Zero, scenario);
            });
        }
    }

    [Test]
    public async Task An_extended_set_is_revalidated_as_one_set()
    {
        var (extension, member, _) = await ExtensionResult("cbi8-01-added");
        var extended = extension.InForce!;
        var requests = extended.Admissions
            .Select(item => ParticipantRequestFor(item.Participant))
            .ToArray();

        var revalidated = await ComponentParticipantRevalidation.RevalidateAsync(
            extended,
            requests,
            "extended set revalidation");

        Assert.Multiple(() =>
        {
            Assert.That(extension.IsExtended, Is.True);
            Assert.That(revalidated.Kind, Is.EqualTo(ComponentParticipantRevalidationKind.Continued));
            Assert.That(revalidated.CurrentAuthority, Has.Count.EqualTo(3));
            Assert.That(member.Stage, Is.EqualTo(PortableCompositionStage.Released));
        });
    }

    [Test]
    public async Task Refused_cbi6_set_cannot_be_extended()
    {
        var unavailable = new ComponentParticipantAdmissionResult(
            Array.Empty<ComponentParticipantObservation>(),
            Array.Empty<LocalCapabilityGrant>(),
            null,
            null);

        var result = await ComponentParticipantExtension.ExtendAsync(
            unavailable,
            ParticipantSet(SupervisorLocalActor).Select(item => item.Request).ToArray(),
            "set extension unavailable");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ComponentParticipantExtensionKind.ActivationUnavailable));
            Assert.That(result.InForce, Is.Null);
            Assert.That(result.CurrentAuthority, Is.Empty);
        });
    }

    [Test]
    public async Task Shared_cbi9_vectors_revise_the_set_only_while_the_declaration_stays_covered()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi9-dependency-revision-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, member, handler) = await RevisionResult(scenario);
            var released = vector.GetProperty("expectedReleased").GetBoolean();

            Assert.Multiple(() =>
            {
                Assert.That(
                    RevisionToken(result.Kind),
                    Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                    scenario);
                Assert.That(
                    result.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                    scenario);
                Assert.That(
                    result.CurrentAuthority,
                    Has.Count.EqualTo(vector.GetProperty("expectedParticipantsEvaluated").GetInt32()),
                    scenario);
                Assert.That(
                    result.InForce?.Admissions.Count ?? 0,
                    Is.EqualTo(vector.GetProperty("expectedInForceParticipants").GetInt32()),
                    scenario);
                Assert.That(
                    result.InForce?.Grants.Count ?? 0,
                    Is.EqualTo(vector.GetProperty("expectedInForceGrants").GetInt32()),
                    scenario);
                Assert.That(
                    member.Stage,
                    Is.EqualTo(
                        released ? PortableCompositionStage.Released : PortableCompositionStage.Retired),
                    scenario);
                Assert.That(
                    result.InForce,
                    released ? Is.Not.Null : Is.Null,
                    $"{scenario}: a set is in force exactly while the member is released.");
                Assert.That(handler.ProviderEffectCount, Is.Zero, scenario);
            });
        }
    }

    [Test]
    public async Task A_substitute_satisfies_the_declaration_a_different_holder_used_to_satisfy()
    {
        var (result, member, _) = await RevisionResult("cbi9-03-substitute-holder");
        var inForce = result.InForce!;
        var auditGrants = inForce.Grants
            .Where(grant => grant.Capability == AuditCapability && grant.Operation == AuditOperation)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsRevised, Is.True);
            Assert.That(
                inForce.Admissions.Select(item => item.Participant),
                Does.Not.Contain(Supervisor),
                "The participant that used to satisfy the declared audit dependency is gone.");
            Assert.That(auditGrants, Has.Length.EqualTo(1));
            Assert.That(
                auditGrants[0].Holder,
                Is.EqualTo(LocalActorReferenceId.Create("local.cooling-deputy")),
                "A different receiving-domain Actor now satisfies it.");
            Assert.That(member.Stage, Is.EqualTo(PortableCompositionStage.Released));
        });
    }

    [Test]
    public async Task Refused_cbi6_set_cannot_be_revised()
    {
        var (resolution, selection, _) = LifecycleInput(DeclaredAuthority);
        var unavailable = new ComponentParticipantAdmissionResult(
            Array.Empty<ComponentParticipantObservation>(),
            Array.Empty<LocalCapabilityGrant>(),
            null,
            null);

        var result = await ComponentParticipantRevision.ReviseAsync(
            resolution,
            selection,
            unavailable,
            Dependency(selection.Definition),
            ParticipantSet(SupervisorLocalActor).Select(item => item.Request).ToArray(),
            "set revision unavailable");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ComponentParticipantRevisionKind.ActivationUnavailable));
            Assert.That(result.InForce, Is.Null);
            Assert.That(result.CurrentAuthority, Is.Empty);
        });
    }

    private static async Task<(
        ComponentParticipantRevisionResult Result,
        PortableCompositionMember Member,
        CoolingPortableHandler Handler)>
        RevisionResult(string scenario)
    {
        var declared = scenario == "cbi9-07-declaration-empty" ? [] : DeclaredAuthority;
        var (resolution, selection, occurrence) = LifecycleInput(declared);
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        IPortableProviderConversation conversation = new PortableDirectConversation(
            new PortableProviderEndpoint(
                CoolingPortableFixture.Contract,
                handler,
                PortableRealization.FixedDirectCall));
        if (scenario == "cbi9-13-retirement-failure")
        {
            conversation = new FailingRetirementConversation(conversation);
        }

        var deputyActor = scenario == "cbi9-11-added-shared-local-actor"
            ? ObserverLocalActor
            : DeputyLocalActor;
        var policy = SetPolicy(SupervisorLocalActor, ObserverLocalActor, deputyActor);
        var participants = ParticipantTrio(occurrence, policy);
        var active = await ComponentParticipantAdmission.ActivateAsync(
            resolution,
            selection,
            participants,
            RuntimeRequest(Plan(occurrence)),
            conversation);
        var provider = participants[0].Request;
        var supervisor = participants[1].Request;
        var observer = participants[2].Request;
        var deputy = DeputyRequest(policy);

        var dependency = scenario switch
        {
            "cbi9-06-declaration-mismatch" => new ComponentGrantDependency(
                selection.Definition,
                [
                    new("cooling.control", Capability, Target, Operation, AuthorityScope),
                    new("cooling.other", AuditCapability, Target, AuditOperation, AuthorityScope),
                ]),
            "cbi9-07-declaration-empty" => new ComponentGrantDependency(selection.Definition, []),
            "cbi9-08-declaration-unsatisfied" => new ComponentGrantDependency(
                selection.Definition,
                [
                    new("cooling.control", CapabilityId.Create("capability.other"), Target, Operation, AuthorityScope),
                    new("cooling.audit", AuditCapability, Target, AuditOperation, AuthorityScope),
                ]),
            _ => Dependency(selection.Definition),
        };

        AuthorityAdmissionRequest[] intended = scenario switch
        {
            "cbi9-01-drop-undepended" => [provider, supervisor],
            "cbi9-02-drop-depended" => [provider, observer],
            "cbi9-03-substitute-holder" or "cbi9-11-added-shared-local-actor" =>
                [provider, observer, deputy],
            "cbi9-04-unchanged" => [provider, supervisor, observer],
            "cbi9-05-empty" => [],
            "cbi9-06-declaration-mismatch" or "cbi9-07-declaration-empty"
                or "cbi9-08-declaration-unsatisfied" => [provider, supervisor],
            "cbi9-09-retained-identity-drift" =>
            [
                provider,
                supervisor with
                {
                    Authority =
                    [
                        supervisor.Authority.Single() with
                        {
                            Capability = CapabilityId.Create("capability.other"),
                        },
                    ],
                },
            ],
            "cbi9-10-added-participant-denied" => [provider, observer, Revoked(deputy)],
            "cbi9-12-retained-participant-revoked" or "cbi9-13-retirement-failure" =>
                [provider, Revoked(supervisor)],
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "unknown CBI9 vector"),
        };

        var result = await ComponentParticipantRevision.ReviseAsync(
            resolution,
            selection,
            active,
            dependency,
            intended,
            $"set revision {scenario}");
        return (result, active.Lifecycle!.Member!, handler);
    }

    [Test]
    public async Task Shared_cbi10_vectors_verify_the_declaration_against_what_the_member_did()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi10-observed-interaction-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (verdict, member, handler) = await VerificationResult(scenario);
            var released = vector.GetProperty("expectedReleased").GetBoolean();
            var expectedRuntime = vector.GetProperty("expectedRuntimeActive");

            Assert.Multiple(() =>
            {
                Assert.That(
                    VerdictToken(verdict.Kind),
                    Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                    scenario);
                Assert.That(
                    verdict.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                    scenario);
                Assert.That(
                    verdict.Exercises,
                    Has.Count.EqualTo(vector.GetProperty("expectedExercises").GetInt32()),
                    scenario);
                Assert.That(
                    verdict.Unexercised,
                    Has.Count.EqualTo(vector.GetProperty("expectedUnexercised").GetInt32()),
                    scenario);
                Assert.That(
                    verdict.Uncovered,
                    Has.Count.EqualTo(vector.GetProperty("expectedUncovered").GetInt32()),
                    scenario);
                Assert.That(
                    verdict.Runtime is null
                        ? (bool?)null
                        : verdict.Runtime.Kind == ActivationRuntimeOutcomeKind.Active,
                    Is.EqualTo(
                        expectedRuntime.ValueKind == JsonValueKind.Null
                            ? (bool?)null
                            : expectedRuntime.GetBoolean()),
                    scenario);
                Assert.That(
                    member.Stage,
                    Is.EqualTo(
                        released ? PortableCompositionStage.Released : PortableCompositionStage.Retired),
                    scenario);
                Assert.That(
                    handler.ProviderEffectCount,
                    Is.EqualTo(vector.GetProperty("expectedProviderEffects").GetInt32()),
                    scenario);

                // The runtime accepts the projection exactly when the verification is consistent.
                if (verdict.Runtime is { } runtime)
                {
                    Assert.That(
                        runtime.Kind == ActivationRuntimeOutcomeKind.Active,
                        Is.EqualTo(verdict.IsConsistent),
                        scenario);
                }
            });
        }
    }

    [Test]
    public async Task Undeclared_use_is_condemned_by_the_runtime_rather_than_by_the_verifier()
    {
        var (verdict, _, _) = await VerificationResult("cbi10-04-undeclared-authority");

        Assert.Multiple(() =>
        {
            Assert.That(verdict.Kind, Is.EqualTo(ComponentInteractionVerdictKind.UndeclaredUse));
            Assert.That(
                verdict.Runtime!.Kind,
                Is.EqualTo(ActivationRuntimeOutcomeKind.BindingObservationConflict),
                "CM4's own rule refuses a delivered exercise the authority check denied.");
            Assert.That(verdict.Exercises.Single().AuthorityAdmitted, Is.False);
            Assert.That(verdict.Exercises.Single().Delivery, Is.EqualTo(BindingDeliveryResult.Delivered));
            Assert.That(verdict.Replacement, Is.Not.Null);
        });
    }

    private static async Task<(
        ComponentInteractionVerdict Verdict,
        PortableCompositionMember Member,
        CoolingPortableHandler Handler)>
        VerificationResult(string scenario)
    {
        var (resolution, selection, occurrence) = LifecycleInput(DeclaredAuthority);
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        IPortableProviderConversation conversation = new PortableDirectConversation(
            new PortableProviderEndpoint(
                CoolingPortableFixture.Contract,
                handler,
                PortableRealization.FixedDirectCall));
        if (scenario == "cbi10-07-retirement-failure")
        {
            conversation = new FailingRetirementConversation(conversation);
        }

        var policy = SetPolicy(SupervisorLocalActor, ObserverLocalActor);
        var participants = ParticipantSet(SupervisorLocalActor)
            .Select(item => item with { Request = item.Request with { Policy = policy } })
            .ToArray();
        var runtimeRequest = RuntimeRequest(Plan(occurrence));
        var active = await ComponentParticipantAdmission.ActivateAsync(
            resolution,
            selection,
            participants,
            runtimeRequest,
            conversation);
        var member = active.Lifecycle!.Member!;

        // The observations are real: the host invokes the released member and records what came back.
        var observations = new List<ComponentObservedInteraction>();
        if (scenario != "cbi10-02-nothing-observed")
        {
            var constraint = scenario == "cbi10-03-denied-before-any-frame"
                ? PortableConstraint.Atom(PortableTruth.Unsatisfied)
                : PortableConstraint.Atom(PortableTruth.Satisfied);
            var result = await member.InvokeAsync(
                CoolingPortableFixture.SetEnabled,
                CoolingPortableFixture.CommandV1,
                CoolingPortableFixture.Command("primary", enabled: true),
                constraint);
            observations.Add(new(CoolingPortableFixture.SetEnabled, result));
        }

        var dependency = scenario == "cbi10-06-ungranted-authority"
            ? new ComponentGrantDependency(
                selection.Definition,
                [
                    new("cooling.control", Capability, Target, Operation, CapabilityScopeId.Create("scope.other")),
                    new("cooling.audit", AuditCapability, Target, AuditOperation, AuthorityScope),
                ])
            : scenario == "cbi10-08-declaration-mismatch"
                ? new ComponentGrantDependency(
                    selection.Definition,
                    [
                        new("cooling.control", Capability, Target, Operation, AuthorityScope),
                        new("cooling.other", AuditCapability, Target, AuditOperation, AuthorityScope),
                    ])
                : Dependency(selection.Definition);

        IReadOnlyList<ComponentOperationAuthorityMapping> attribution = scenario switch
        {
            "cbi10-04-undeclared-authority" or "cbi10-07-retirement-failure" =>
                [new(CoolingPortableFixture.SetEnabled, "cooling.other")],
            "cbi10-05-unmapped-operation" => [],
            "cbi10-09-mapping-not-distinct" =>
            [
                new(CoolingPortableFixture.SetEnabled, "cooling.control"),
                new(CoolingPortableFixture.SetEnabled, "cooling.audit"),
            ],
            _ => [new(CoolingPortableFixture.SetEnabled, "cooling.control")],
        };

        var verdict = await ComponentInteractionVerification.VerifyAsync(
            resolution,
            selection,
            active,
            dependency,
            attribution,
            observations,
            runtimeRequest,
            $"observed interaction {scenario}");
        return (verdict, member, handler);
    }

    [Test]
    public async Task Shared_cbi11_vectors_narrow_only_when_a_successor_declares_less()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi11-declaration-succession-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, member) = await SuccessionResult(scenario);

            Assert.Multiple(() =>
            {
                Assert.That(
                    SuccessionToken(result.Kind),
                    Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                    scenario);
                Assert.That(
                    result.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                    scenario);
                Assert.That(
                    result.Dropped,
                    Has.Count.EqualTo(vector.GetProperty("expectedDropped").GetInt32()),
                    scenario);
                Assert.That(
                    result.Vetoed,
                    Has.Count.EqualTo(vector.GetProperty("expectedVetoed").GetInt32()),
                    scenario);
                Assert.That(
                    result.Declaration!.Entries,
                    Has.Count.EqualTo(vector.GetProperty("expectedDeclaredInForce").GetInt32()),
                    scenario);
                // CBI11 has no retirement path.
                Assert.That(
                    member.Stage,
                    Is.EqualTo(PortableCompositionStage.Released),
                    scenario);
                Assert.That(
                    vector.GetProperty("expectedReleased").GetBoolean(),
                    Is.True,
                    $"{scenario}: every CBI11 outcome leaves the member released.");
            });
        }
    }

    [Test]
    public async Task A_narrowed_declaration_lets_cbi9_release_the_participant_it_kept()
    {
        var (resolution, selection, occurrence) = LifecycleInput(DeclaredAuthority);
        var participants = ParticipantSet(SupervisorLocalActor);
        var active = await ComponentParticipantAdmission.ActivateAsync(
            resolution,
            selection,
            participants,
            RuntimeRequest(Plan(occurrence)),
            DirectCooling(CoolingPortableFixture.Contract));

        var before = await ComponentParticipantRevision.ReviseAsync(
            resolution,
            selection,
            active,
            Dependency(selection.Definition),
            [participants[0].Request],
            "drop the audit holder before succession");

        var successor = new FakeGenerationResolver().Resolve(
            Request(Cardinality.Parse("1..1"), ["cooling.control"]));
        var narrowed = ComponentDeclarationSuccession.Succeed(
            resolution,
            successor,
            selection,
            active,
            Dependency(selection.Definition),
            ControlOnlyDependency(selection.Definition),
            [new(CoolingPortableFixture.SetEnabled, "cooling.control")],
            []);

        var after = await ComponentParticipantRevision.ReviseAsync(
            successor,
            selection,
            active,
            narrowed.Declaration!,
            [participants[0].Request],
            "drop the audit holder after succession");

        Assert.Multiple(() =>
        {
            Assert.That(before.Code, Is.EqualTo("dependency-not-covered"));
            Assert.That(narrowed.IsNarrowed, Is.True);
            Assert.That(narrowed.Dropped, Is.EqualTo(new[] { "cooling.audit" }));
            Assert.That(after.Kind, Is.EqualTo(ComponentParticipantRevisionKind.Revised));
            Assert.That(after.InForce!.Admissions, Has.Count.EqualTo(1));
        });
    }

    private static async Task<(ComponentDeclarationSuccessionResult Result, PortableCompositionMember Member)>
        SuccessionResult(string scenario)
    {
        var (resolution, selection, occurrence) = LifecycleInput(DeclaredAuthority);
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        var conversation = new PortableDirectConversation(
            new PortableProviderEndpoint(
                CoolingPortableFixture.Contract,
                handler,
                PortableRealization.FixedDirectCall));
        var active = await ComponentParticipantAdmission.ActivateAsync(
            resolution,
            selection,
            ParticipantSet(SupervisorLocalActor),
            RuntimeRequest(Plan(occurrence)),
            conversation);
        var member = active.Lifecycle!.Member!;
        var interaction = await member.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableConstraint.Atom(PortableTruth.Satisfied));
        var observations = new ComponentObservedInteraction[]
        {
            new(CoolingPortableFixture.SetEnabled, interaction),
        };

        var successor = scenario == "cbi11-06-position-mismatch"
            ? new FakeGenerationResolver().Resolve(
                Request(Cardinality.Parse("1..1"), ["cooling.control"], BindingScopeId.Create("scope.other")))
            : scenario == "cbi11-07-successor-declares-nothing"
                ? new FakeGenerationResolver().Resolve(Request(Cardinality.Parse("1..1"), []))
                : scenario is "cbi11-03-unchanged" or "cbi11-08-successor-mapping-mismatch"
                    ? new FakeGenerationResolver().Resolve(
                        Request(Cardinality.Parse("1..1"), DeclaredAuthority))
                    : scenario == "cbi11-04-wider"
                        ? new FakeGenerationResolver().Resolve(
                            Request(Cardinality.Parse("1..1"), ["cooling.control", "cooling.audit", "cooling.extra"]))
                        : new FakeGenerationResolver().Resolve(
                            Request(Cardinality.Parse("1..1"), ["cooling.control"]));

        var successorDeclaration = scenario switch
        {
            "cbi11-03-unchanged" => Dependency(selection.Definition),
            "cbi11-04-wider" => new ComponentGrantDependency(
                selection.Definition,
                [
                    .. Dependency(selection.Definition).Entries,
                    new("cooling.extra", ReportCapability, Target, ReportOperation, AuthorityScope),
                ]),
            "cbi11-05-tuple-changed" => new ComponentGrantDependency(
                selection.Definition,
                [new("cooling.control", Capability, Target, Operation, CapabilityScopeId.Create("scope.other"))]),
            "cbi11-07-successor-declares-nothing" => new ComponentGrantDependency(selection.Definition, []),
            "cbi11-08-successor-mapping-mismatch" => new ComponentGrantDependency(
                selection.Definition,
                [new("cooling.control", Capability, Target, Operation, AuthorityScope)]),
            _ => ControlOnlyDependency(selection.Definition),
        };

        IReadOnlyList<ComponentOperationAuthorityMapping> attribution = scenario switch
        {
            "cbi11-02-use-vetoed" => [new(CoolingPortableFixture.SetEnabled, "cooling.audit")],
            "cbi11-09-ambiguous-attribution" =>
            [
                new(CoolingPortableFixture.SetEnabled, "cooling.control"),
                new(CoolingPortableFixture.SetEnabled, "cooling.audit"),
            ],
            _ => [new(CoolingPortableFixture.SetEnabled, "cooling.control")],
        };

        var result = ComponentDeclarationSuccession.Succeed(
            resolution,
            successor,
            selection,
            active,
            Dependency(selection.Definition),
            successorDeclaration,
            attribution,
            observations);
        return (result, member);
    }

    private static ComponentGrantDependency ControlOnlyDependency(DefinitionId definition) =>
        new(definition, [new("cooling.control", Capability, Target, Operation, AuthorityScope)]);

    private static string SuccessionToken(ComponentDeclarationSuccessionKind kind) => kind switch
    {
        ComponentDeclarationSuccessionKind.Narrowed => "narrowed",
        ComponentDeclarationSuccessionKind.Declined => "declined",
        ComponentDeclarationSuccessionKind.ActivationUnavailable => "activation-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string VerdictToken(ComponentInteractionVerdictKind kind) => kind switch
    {
        ComponentInteractionVerdictKind.Consistent => "consistent",
        ComponentInteractionVerdictKind.UndeclaredUse => "undeclared-use",
        ComponentInteractionVerdictKind.UngrantedUse => "ungranted-use",
        ComponentInteractionVerdictKind.RetirementFailed => "retirement-failed",
        ComponentInteractionVerdictKind.Declined => "declined",
        ComponentInteractionVerdictKind.ActivationUnavailable => "activation-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static ComponentGrantDependency Dependency(DefinitionId definition) =>
        new(
            definition,
            [
                new("cooling.control", Capability, Target, Operation, AuthorityScope),
                new("cooling.audit", AuditCapability, Target, AuditOperation, AuthorityScope),
            ]);

    private static ComponentParticipantRequest[] ParticipantTrio(
        OccurrenceId occurrence,
        LocalAuthorityPolicy policy)
    {
        var pair = ParticipantSet(SupervisorLocalActor, ObserverLocalActor)
            .Select(item => item with { Request = item.Request with { Policy = policy } })
            .ToArray();
        return
        [
            pair[0],
            pair[1],
            new(new(occurrence, Observer), ObserverRequest(policy)),
        ];
    }

    private static AuthorityAdmissionRequest DeputyRequest(LocalAuthorityPolicy policy) =>
        new(
            AdmissionRequestId.Create("admission.set-deputy"),
            Deputy,
            EvaluationTime,
            [SetEvidence(DeputyEvidence, Deputy)],
            [new(DeputyRelationship, Deputy, ActorRelationshipKind.ComponentParticipant, [DeputyEvidence])],
            [new(DeputyAuthority, DeputyRelationship, AuditCapability, Target, AuditOperation, AuthorityScope, false)],
            policy);

    private static string RevisionToken(ComponentParticipantRevisionKind kind) => kind switch
    {
        ComponentParticipantRevisionKind.Revised => "revised",
        ComponentParticipantRevisionKind.Declined => "declined",
        ComponentParticipantRevisionKind.Withdrawn => "withdrawn",
        ComponentParticipantRevisionKind.RetirementFailed => "retirement-failed",
        ComponentParticipantRevisionKind.ActivationUnavailable => "activation-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static AuthorityAdmissionRequest ParticipantRequestFor(ActorId participant)
    {
        var policy = SetPolicy(SupervisorLocalActor, ObserverLocalActor);
        if (participant == Observer)
        {
            return ObserverRequest(policy);
        }

        return ParticipantSet(SupervisorLocalActor)
            .Select(item => item.Request)
            .Single(item => item.Participant == participant);
    }

    private static async Task<(
        ComponentParticipantExtensionResult Result,
        PortableCompositionMember Member,
        CoolingPortableHandler Handler)>
        ExtensionResult(string scenario)
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        IPortableProviderConversation conversation = new PortableDirectConversation(
            new PortableProviderEndpoint(
                CoolingPortableFixture.Contract,
                handler,
                PortableRealization.FixedDirectCall));
        if (scenario == "cbi8-11-retirement-failure")
        {
            conversation = new FailingRetirementConversation(conversation);
        }

        var observerActor = scenario == "cbi8-09-added-shared-local-actor"
            ? SupervisorLocalActor
            : ObserverLocalActor;
        var participants = ParticipantSet(SupervisorLocalActor, observerActor);
        var active = await ComponentParticipantAdmission.ActivateAsync(
            resolution,
            selection,
            participants,
            RuntimeRequest(Plan(occurrence)),
            conversation);
        var baseline = participants.Select(item => item.Request).ToArray();
        var provider = baseline[0];
        var supervisor = baseline[1];
        var observer = ObserverRequest(SetPolicy(SupervisorLocalActor, observerActor));

        AuthorityAdmissionRequest[] intended = scenario switch
        {
            "cbi8-01-added" or "cbi8-09-added-shared-local-actor" => [.. baseline, observer],
            "cbi8-02-participant-removed" => [provider],
            "cbi8-03-participant-substituted" => [provider, observer],
            "cbi8-04-unchanged" => baseline,
            "cbi8-05-added-identity-collision" =>
            [
                .. baseline,
                observer with
                {
                    Authority = [observer.Authority.Single() with { Request = Authority }],
                },
            ],
            "cbi8-06-added-unlimited-grant" =>
            [
                .. baseline,
                observer with
                {
                    Authority = [observer.Authority.Single() with { Unlimited = true }],
                },
            ],
            "cbi8-07-retained-identity-drift" =>
            [
                provider,
                supervisor with
                {
                    Authority =
                    [
                        supervisor.Authority.Single() with
                        {
                            Capability = CapabilityId.Create("capability.other"),
                        },
                    ],
                },
                observer,
            ],
            "cbi8-08-added-participant-denied" => [.. baseline, Revoked(observer)],
            "cbi8-10-retained-participant-revoked" or "cbi8-11-retirement-failure" =>
                [provider, Revoked(supervisor), observer],
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "unknown CBI8 vector"),
        };

        var result = await ComponentParticipantExtension.ExtendAsync(
            active,
            intended,
            $"set extension {scenario}");
        return (result, active.Lifecycle!.Member!, handler);
    }

    private static string ExtensionToken(ComponentParticipantExtensionKind kind) => kind switch
    {
        ComponentParticipantExtensionKind.Extended => "extended",
        ComponentParticipantExtensionKind.Declined => "declined",
        ComponentParticipantExtensionKind.Withdrawn => "withdrawn",
        ComponentParticipantExtensionKind.RetirementFailed => "retirement-failed",
        ComponentParticipantExtensionKind.ActivationUnavailable => "activation-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static async Task<(
        ComponentParticipantRevalidationResult Result,
        PortableCompositionMember Member,
        CoolingPortableHandler Handler)>
        SetRevalidationResult(string scenario)
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        IPortableProviderConversation conversation = new PortableDirectConversation(
            new PortableProviderEndpoint(
                CoolingPortableFixture.Contract,
                handler,
                PortableRealization.FixedDirectCall));
        if (scenario == "cbi7-08-retirement-failure")
        {
            conversation = new FailingRetirementConversation(conversation);
        }

        var participants = ParticipantSet(SupervisorLocalActor);
        var active = await ComponentParticipantAdmission.ActivateAsync(
            resolution,
            selection,
            participants,
            RuntimeRequest(Plan(occurrence)),
            conversation);
        var baseline = participants.Select(item => item.Request).ToArray();
        var provider = baseline[0];
        var supervisor = baseline[1];

        AuthorityAdmissionRequest[] fresh = scenario switch
        {
            "cbi7-01-current" => baseline,
            "cbi7-02-one-revoked" => [provider, Revoked(supervisor)],
            "cbi7-03-all-expired" => baseline.Select(Expired).ToArray(),
            "cbi7-04-tuple-mismatch" =>
            [
                provider,
                supervisor with
                {
                    Authority =
                    [
                        supervisor.Authority.Single() with
                        {
                            Capability = CapabilityId.Create("capability.other"),
                        },
                    ],
                },
            ],
            "cbi7-05-grant-dropped" => [provider with { Authority = [provider.Authority[0]] }, supervisor],
            "cbi7-06-participant-removed" => [provider],
            "cbi7-07-participant-added" => [.. baseline, Relabelled(supervisor, Observer)],
            "cbi7-08-retirement-failure" => [Revoked(provider), Revoked(supervisor)],
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "unknown CBI7 vector"),
        };

        var result = await ComponentParticipantRevalidation.RevalidateAsync(
            active,
            fresh,
            $"set authority revalidation {scenario}");
        return (result, active.Lifecycle!.Member!, handler);
    }

    private static AuthorityAdmissionRequest Revoked(AuthorityAdmissionRequest request) =>
        request with
        {
            Evidence = request.Evidence
                .Select(item => item with { State = AdmissionEvidenceState.Revoked })
                .ToArray(),
        };

    private static AuthorityAdmissionRequest Expired(AuthorityAdmissionRequest request) =>
        request with { EvaluationTime = request.Evidence.Max(item => item.ExpiresAt) };

    private static AuthorityAdmissionRequest Relabelled(AuthorityAdmissionRequest request, ActorId actor) =>
        request with
        {
            Request = AdmissionRequestId.Create($"admission.set-{actor.Value}"),
            Participant = actor,
            Evidence = request.Evidence.Select(item => item with { Subject = actor }).ToArray(),
            Relationships = request.Relationships
                .Select(item => item with { ProposedActor = actor })
                .ToArray(),
        };

    private static string SetRevalidationToken(ComponentParticipantRevalidationKind kind) => kind switch
    {
        ComponentParticipantRevalidationKind.Continued => "continued",
        ComponentParticipantRevalidationKind.Withdrawn => "withdrawn",
        ComponentParticipantRevalidationKind.RetirementFailed => "retirement-failed",
        ComponentParticipantRevalidationKind.ActivationUnavailable => "activation-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static SortedDictionary<string, string> PortableFacts(ComponentParticipantAdmissionResult result)
    {
        var member = result.Lifecycle!.Member!;
        var facts = new SortedDictionary<string, string>(member.ResolutionFacts, StringComparer.Ordinal);
        foreach (var fact in member.Plan!.Facts.Where(item => item.Key != "planId"))
        {
            facts[fact.Key] = fact.Value;
        }

        return facts;
    }

    private static async Task<ComponentParticipantAdmissionResult> Activate(
        IReadOnlyList<ComponentParticipantRequest> participants)
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        return await ComponentParticipantAdmission.ActivateAsync(
            resolution,
            selection,
            participants,
            RuntimeRequest(Plan(occurrence)),
            DirectCooling(CoolingPortableFixture.Contract));
    }

    private static async Task<(ComponentParticipantAdmissionResult Result, CoolingPortableHandler Handler)>
        ParticipantResult(string scenario)
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        var conversation = new PortableDirectConversation(
            new PortableProviderEndpoint(
                CoolingPortableFixture.Contract,
                handler,
                PortableRealization.FixedDirectCall));
        var supervisorActor = scenario == "cbi6-08-shared-local-actor"
            ? ProviderLocalActor
            : SupervisorLocalActor;
        var participants = ParticipantSet(supervisorActor);

        participants = scenario switch
        {
            "cbi6-01-two-participants" or "cbi6-08-shared-local-actor" => participants,
            "cbi6-02-second-participant-denied" =>
            [
                participants[0],
                Revoked(participants[1]),
            ],
            "cbi6-03-repeated-participant" => [participants[0], participants[0]],
            "cbi6-04-shared-authority-identity" =>
            [
                participants[0],
                WithAuthority(
                    participants[1],
                    participants[1].Request.Authority.Single() with { Request = Authority }),
            ],
            "cbi6-05-repeated-grant-tuple" =>
            [
                WithAuthority(
                    participants[0],
                    participants[0].Request.Authority[0],
                    participants[0].Request.Authority[0] with { Request = AuthorityRequestId.Create("authority.cooling-control-again") }),
                participants[1],
            ],
            "cbi6-06-unlimited-grant" =>
            [
                participants[0],
                WithAuthority(
                    participants[1],
                    participants[1].Request.Authority.Single() with { Unlimited = true }),
            ],
            "cbi6-07-empty-set" => [],
            "cbi6-09-foreign-occurrence" =>
            [
                participants[0],
                participants[1] with
                {
                    Mapping = participants[1].Mapping with { Occurrence = OccurrenceId.Create("occ.unselected") },
                },
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "unknown CBI6 vector"),
        };

        var result = await ComponentParticipantAdmission.ActivateAsync(
            resolution,
            selection,
            participants,
            RuntimeRequest(Plan(occurrence)),
            conversation);
        return (result, handler);
    }

    private static ComponentParticipantRequest Revoked(ComponentParticipantRequest participant) =>
        participant with
        {
            Request = participant.Request with
            {
                Evidence = participant.Request.Evidence
                    .Select(item => item with { State = AdmissionEvidenceState.Revoked })
                    .ToArray(),
            },
        };

    private static ComponentParticipantRequest WithAuthority(
        ComponentParticipantRequest participant,
        params AuthorityRequest[] authority) =>
        participant with { Request = participant.Request with { Authority = authority } };

    private static ComponentParticipantRequest[] ParticipantSet(LocalActorReferenceId supervisorActor) =>
        ParticipantSet(supervisorActor, ObserverLocalActor);

    private static ComponentParticipantRequest[] ParticipantSet(
        LocalActorReferenceId supervisorActor,
        LocalActorReferenceId observerActor)
    {
        var policy = SetPolicy(supervisorActor, observerActor);
        var (_, _, occurrence) = LifecycleInput();
        var provider = new AuthorityAdmissionRequest(
            AdmissionRequestId.Create("admission.set-provider"),
            Participant,
            EvaluationTime,
            [SetEvidence(AuthorityEvidence, Participant)],
            [new(Relationship, Participant, ActorRelationshipKind.ComponentParticipant, [AuthorityEvidence])],
            [
                new(Authority, Relationship, Capability, Target, Operation, AuthorityScope, false),
                new(ReportAuthority, Relationship, ReportCapability, Target, ReportOperation, AuthorityScope, false),
            ],
            policy);
        var supervisor = new AuthorityAdmissionRequest(
            AdmissionRequestId.Create("admission.set-supervisor"),
            Supervisor,
            EvaluationTime,
            [SetEvidence(SupervisorEvidence, Supervisor)],
            [new(SupervisorRelationship, Supervisor, ActorRelationshipKind.ComponentParticipant, [SupervisorEvidence])],
            [new(AuditAuthority, SupervisorRelationship, AuditCapability, Target, AuditOperation, AuthorityScope, false)],
            policy);
        return
        [
            new(new(occurrence, Participant), provider),
            new(new(occurrence, Supervisor), supervisor),
        ];
    }

    private static AdmissionEvidence SetEvidence(EvidenceId evidence, ActorId subject) =>
        new(
            evidence,
            AuthorityIssuer,
            subject,
            AdmissionEvidenceVerification.Verified,
            EvaluationTime.AddHours(-1),
            EvaluationTime.AddHours(1),
            AdmissionEvidenceState.Current);

    private static AuthorityAdmissionRequest ObserverRequest(LocalAuthorityPolicy policy) =>
        new(
            AdmissionRequestId.Create("admission.set-observer"),
            Observer,
            EvaluationTime,
            [SetEvidence(ObserverEvidence, Observer)],
            [new(ObserverRelationship, Observer, ActorRelationshipKind.ComponentParticipant, [ObserverEvidence])],
            [new(ObserveAuthority, ObserverRelationship, ObserveCapability, Target, ObserveOperation, AuthorityScope, false)],
            policy);

    private static LocalAuthorityPolicy SetPolicy(
        LocalActorReferenceId supervisorActor,
        LocalActorReferenceId observerActor) =>
        SetPolicy(supervisorActor, observerActor, DeputyLocalActor);

    private static LocalAuthorityPolicy SetPolicy(
        LocalActorReferenceId supervisorActor,
        LocalActorReferenceId observerActor,
        LocalActorReferenceId deputyActor) =>
        new(
            AuthorityPolicyId.Create("policy.integration-set"),
            [AuthorityIssuer],
            [
                new(
                    PolicyRuleId.Create("rule.component-participant"),
                    Participant,
                    ActorRelationshipKind.ComponentParticipant,
                    PolicyDisposition.Allow,
                    ProviderLocalActor,
                    [AuthorityEvidence],
                    false,
                    "component participant admitted"),
                new(
                    PolicyRuleId.Create("rule.component-supervisor"),
                    Supervisor,
                    ActorRelationshipKind.ComponentParticipant,
                    PolicyDisposition.Allow,
                    supervisorActor,
                    [SupervisorEvidence],
                    false,
                    "component supervisor admitted"),
                new(
                    PolicyRuleId.Create("rule.component-observer"),
                    Observer,
                    ActorRelationshipKind.ComponentParticipant,
                    PolicyDisposition.Allow,
                    observerActor,
                    [ObserverEvidence],
                    false,
                    "component observer admitted"),
                new(
                    PolicyRuleId.Create("rule.component-deputy"),
                    Deputy,
                    ActorRelationshipKind.ComponentParticipant,
                    PolicyDisposition.Allow,
                    deputyActor,
                    [DeputyEvidence],
                    false,
                    "component deputy admitted"),
            ],
            [
                new(
                    PolicyRuleId.Create("rule.cooling-control"),
                    ActorRelationshipKind.ComponentParticipant,
                    Capability,
                    Target,
                    Operation,
                    AuthorityScope,
                    PolicyDisposition.Allow,
                    false,
                    "narrow cooling control admitted"),
                new(
                    PolicyRuleId.Create("rule.cooling-report"),
                    ActorRelationshipKind.ComponentParticipant,
                    ReportCapability,
                    Target,
                    ReportOperation,
                    AuthorityScope,
                    PolicyDisposition.Allow,
                    false,
                    "narrow cooling reporting admitted"),
                new(
                    PolicyRuleId.Create("rule.cooling-audit"),
                    ActorRelationshipKind.ComponentParticipant,
                    AuditCapability,
                    Target,
                    AuditOperation,
                    AuthorityScope,
                    PolicyDisposition.Allow,
                    false,
                    "narrow cooling audit admitted"),
                new(
                    PolicyRuleId.Create("rule.cooling-observe"),
                    ActorRelationshipKind.ComponentParticipant,
                    ObserveCapability,
                    Target,
                    ObserveOperation,
                    AuthorityScope,
                    PolicyDisposition.Allow,
                    false,
                    "narrow cooling observation admitted"),
            ]);

    private static string ParticipantFailureToken(ComponentParticipantAdmissionFailureKind kind) => kind switch
    {
        ComponentParticipantAdmissionFailureKind.ParticipantSetInvalid => "participant-set-invalid",
        ComponentParticipantAdmissionFailureKind.AuthorityShapeUnsupported => "authority-shape-unsupported",
        ComponentParticipantAdmissionFailureKind.AuthorityRefused => "authority-refused",
        ComponentParticipantAdmissionFailureKind.LocalIdentityConflict => "local-identity-conflict",
        ComponentParticipantAdmissionFailureKind.LifecycleRefused => "lifecycle-refused",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static async Task<(
        ComponentAuthorityRevalidationResult Result,
        PortableCompositionMember Member,
        CoolingPortableHandler Handler)>
        RevalidationResult(string scenario)
    {
        var (resolution, selection, occurrence) = LifecycleInput();
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        IPortableProviderConversation conversation = new PortableDirectConversation(
            new PortableProviderEndpoint(
                CoolingPortableFixture.Contract,
                handler,
                PortableRealization.FixedDirectCall));
        if (scenario == "cbi5-05-retirement-failure")
        {
            conversation = new FailingRetirementConversation(conversation);
        }

        var active = await ComponentAuthorityIntegration.ActivateAsync(
            resolution,
            selection,
            new(occurrence, Participant),
            RuntimeRequest(Plan(occurrence)),
            Admission(),
            conversation);
        var member = active.Lifecycle!.Member!;
        var request = Admission();

        switch (scenario)
        {
            case "cbi5-01-current":
            case "cbi5-05-retirement-failure":
                break;
            case "cbi5-02-revoked":
                request = request with
                {
                    Evidence = request.Evidence
                        .Select(item => item with { State = AdmissionEvidenceState.Revoked })
                        .ToArray(),
                };
                break;
            case "cbi5-03-expired":
                request = request with { EvaluationTime = request.Evidence.Single().ExpiresAt };
                break;
            case "cbi5-04-request-mismatch":
                request = request with
                {
                    Authority =
                    [
                        request.Authority.Single() with
                        {
                            Capability = CapabilityId.Create("capability.other"),
                        },
                    ],
                };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "unknown CBI5 vector");
        }

        if (scenario == "cbi5-05-retirement-failure")
        {
            request = request with
            {
                Evidence = request.Evidence
                    .Select(item => item with { State = AdmissionEvidenceState.Revoked })
                    .ToArray(),
            };
        }

        var result = await ComponentAuthorityRevalidation.RevalidateAsync(
            active,
            request,
            $"authority revalidation {scenario}");
        return (result, member, handler);
    }

    private static string RevalidationKindToken(ComponentAuthorityRevalidationKind kind) => kind switch
    {
        ComponentAuthorityRevalidationKind.Continued => "continued",
        ComponentAuthorityRevalidationKind.Withdrawn => "withdrawn",
        ComponentAuthorityRevalidationKind.RetirementFailed => "retirement-failed",
        ComponentAuthorityRevalidationKind.ActivationUnavailable => "activation-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static async Task<ComponentAuthorityIntegrationResult> ComparisonResult(string scenario)
    {
        var (resolution, originalSelection, occurrence) = LifecycleInput();
        var selection = originalSelection with { HostEndpoint = "component-comparison-host" };
        var mapping = new ComponentAuthorityMapping(occurrence, Participant);
        var authority = Admission();
        var document = CoolingPortableFixture.Contract;

        switch (scenario)
        {
            case "cbi4-02-authority-denied":
                authority = authority with
                {
                    Evidence = authority.Evidence
                        .Select(item => item with { State = AdmissionEvidenceState.Revoked })
                        .ToArray(),
                };
                break;
            case "cbi4-03-authority-shape":
                authority = authority with
                {
                    Authority =
                    [
                        .. authority.Authority,
                        authority.Authority.Single() with
                        {
                            Request = AuthorityRequestId.Create("authority.additional"),
                        },
                    ],
                };
                break;
            case "cbi4-04-mapping":
                mapping = mapping with { Participant = ActorId.Create("actor.other") };
                break;
            case "cbi4-05-lifecycle":
                document = document with
                {
                    Provider = PortableProviderReference.Parse("brontide.fake.substituted", 1),
                };
                break;
            case "cbi4-01-active":
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "unknown CBI4 vector");
        }

        return await ComponentAuthorityIntegration.ActivateAsync(
            resolution,
            selection,
            mapping,
            RuntimeRequest(Plan(occurrence)),
            authority,
            DirectCooling(document));
    }

    private static ResolutionOutcome Resolve(Cardinality cardinality) =>
        new FakeGenerationResolver().Resolve(Request(cardinality));

    private static (ResolutionOutcome Resolution, ComponentBindingSelection Selection, OccurrenceId Occurrence) LifecycleInput() =>
        LifecycleInput([]);

    private static (ResolutionOutcome Resolution, ComponentBindingSelection Selection, OccurrenceId Occurrence)
        LifecycleInput(IReadOnlyList<string> declaredAuthority)
    {
        var resolution = new FakeGenerationResolver().Resolve(
            Request(Cardinality.Parse("1..1"), declaredAuthority));
        var member = resolution.Generation!.ProviderSets.Single().Members.Single();
        return (resolution, Selection(member), member.Occurrence);
    }

    /// <summary>A genuinely cyclic group: one strongly connected component carrying a protocol.</summary>
    /// <summary>
    /// A strongly connected group that declares no protocol: the members interact ordinarily, which
    /// is enough to make one component of the graph and nothing more.
    /// </summary>
    private static ActivationGroupPlan CyclePlan(
        IReadOnlyList<OccurrenceId> cycle,
        params OccurrenceId[] isolated)
    {
        var members = cycle.Concat(isolated)
            .Select(occurrence => new ActivationGroupMember(
                occurrence,
                DefinitionId.Create($"def.{occurrence.Value}"),
                RegionId.Create("region.integration"),
                [new ProvidedContract(Contract, Version)],
                [],
                [],
                []))
            .ToArray();
        var edges = cycle
            .Select((occurrence, index) => new ActivationDependency(
                ActivationEdgeId.Create($"edge.cycle-{index}"),
                occurrence,
                cycle[(index + 1) % cycle.Count],
                ActivationDependencyKind.OrdinaryInteraction,
                Contract,
                Version,
                // Ordinary traffic observed before Release is what CM3 refuses; this only declares
                // that the members interact once both are serving.
                false,
                null,
                null,
                false))
            .ToArray();
        var outcome = new FakeActivationGroupPlanner().Plan(new(
            ActivationGroupRequestId.Create("group.integration"),
            GenerationId.Create("gen.lifecycle"),
            RestartScopeId.Create("restart.lifecycle"),
            members,
            edges,
            [],
            []));
        return outcome.Plan ?? throw new InvalidOperationException(
            $"CM3 refused the ordinary cycle: {outcome.Failure?.Kind} {outcome.Failure?.Reason}");
    }

    private static ActivationGroupPlan ProtocolPlan(IReadOnlyList<OccurrenceId> occurrences)
    {
        var members = occurrences.Select(occurrence => new ActivationGroupMember(
            occurrence,
            DefinitionId.Create($"def.{occurrence.Value}"),
            RegionId.Create("region.integration"),
            new[] { new ProvidedContract(Contract, Version) },
            Array.Empty<LifecycleInputId>(),
            Array.Empty<LifecycleInputId>(),
            Array.Empty<OccurrenceId>())).ToArray();
        var protocol = LifecycleProtocolId.Create("protocol.forward");
        var reverse = LifecycleProtocolId.Create("protocol.backward");
        var forward = ActivationEdgeId.Create("edge.forward");
        var backward = ActivationEdgeId.Create("edge.backward");
        LifecycleProtocolDeclaration Declare(
            LifecycleProtocolId identity,
            ActivationEdgeId edge,
            OccurrenceId from,
            OccurrenceId to) =>
            new(
                identity,
                edge,
                from,
                to,
                LifecycleOperationId.Create("lifecycle.handshake"),
                [CapabilityId.Create("capability.lifecycle-handshake")],
                ShapeId.Create("shape.handshake-in"),
                ShapeId.Create("shape.handshake-out"),
                "ordered",
                1000,
                0,
                true,
                "acknowledged",
                "abort",
                "release");
        var outcome = new FakeActivationGroupPlanner().Plan(new(
            ActivationGroupRequestId.Create("group.integration"),
            GenerationId.Create("gen.lifecycle"),
            RestartScopeId.Create("restart.lifecycle"),
            members,
            new[]
            {
                new ActivationDependency(
                    forward,
                    occurrences[0],
                    occurrences[1],
                    ActivationDependencyKind.RelationalInitialisation,
                    Contract,
                    Version,
                    true,
                    protocol,
                    null,
                    false),
                new ActivationDependency(
                    backward,
                    occurrences[1],
                    occurrences[0],
                    ActivationDependencyKind.RelationalInitialisation,
                    Contract,
                    Version,
                    true,
                    reverse,
                    null,
                    false),
            },
            new[]
            {
                Declare(protocol, forward, occurrences[0], occurrences[1]),
                Declare(reverse, backward, occurrences[1], occurrences[0]),
            },
            Array.Empty<RegionCrossingDeclaration>()));
        return outcome.Plan ?? throw new InvalidOperationException(
            $"CM3 refused the cyclic plan: {outcome.Failure?.Kind} {outcome.Failure?.Reason}");
    }

    private static ActivationGroupPlan Plan(params OccurrenceId[] occurrences) =>
        PlanFor(
            GenerationId.Create("gen.lifecycle"),
            RestartScopeId.Create("restart.lifecycle"),
            occurrences);

    private static ActivationRuntimeRequest RuntimeRequest(ActivationGroupPlan plan) =>
        RuntimeRequestFor(plan, GenerationId.Create("gen.retained"));

    private static ActivationGroupPlan PlanFor(
        GenerationId generation,
        RestartScopeId restartScope,
        params OccurrenceId[] occurrences)
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
            generation,
            restartScope,
            members,
            Array.Empty<ActivationDependency>(),
            Array.Empty<LifecycleProtocolDeclaration>(),
            Array.Empty<RegionCrossingDeclaration>()));
        return outcome.Plan!;
    }

    private static ActivationRuntimeRequest RuntimeRequestFor(
        ActivationGroupPlan plan,
        GenerationId retained)
    {
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

    private static AuthorityAdmissionRequest Admission()
    {
        var relationship = new ActorRelationshipRequest(
            Relationship,
            Participant,
            ActorRelationshipKind.ComponentParticipant,
            new[] { AuthorityEvidence });
        var authority = new AuthorityRequest(
            Authority,
            Relationship,
            Capability,
            Target,
            Operation,
            AuthorityScope,
            false);
        return new(
            AdmissionRequestId.Create("admission.integration"),
            Participant,
            EvaluationTime,
            new[]
            {
                new AdmissionEvidence(
                    AuthorityEvidence,
                    AuthorityIssuer,
                    Participant,
                    AdmissionEvidenceVerification.Verified,
                    EvaluationTime.AddHours(-1),
                    EvaluationTime.AddHours(1),
                    AdmissionEvidenceState.Current),
            },
            new[] { relationship },
            new[] { authority },
            new LocalAuthorityPolicy(
                AuthorityPolicyId.Create("policy.integration"),
                new[] { AuthorityIssuer },
                new[]
                {
                    new RelationshipPolicyRule(
                        PolicyRuleId.Create("rule.component-participant"),
                        Participant,
                        ActorRelationshipKind.ComponentParticipant,
                        PolicyDisposition.Allow,
                        LocalActorReferenceId.Create("local.cooling-provider"),
                        new[] { AuthorityEvidence },
                        false,
                        "component participant admitted"),
                },
                new[]
                {
                    new AuthorityPolicyRule(
                        PolicyRuleId.Create("rule.cooling-control"),
                        ActorRelationshipKind.ComponentParticipant,
                        Capability,
                        Target,
                        Operation,
                        AuthorityScope,
                        PolicyDisposition.Allow,
                        false,
                        "narrow cooling control admitted"),
                }));
    }

    [Test]
    public async Task Shared_cbi12_vectors_open_ordinary_interaction_for_every_member_or_none()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi12-group-activation-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, handlers) = await GroupActivationResult(scenario);
            var expectedFailure = vector.GetProperty("expectedFailureKind");
            var expectedCode = vector.GetProperty("expectedCode");
            var expectedRuntime = vector.GetProperty("expectedRuntimeActive");

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsActive,
                    Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()),
                    scenario);
                Assert.That(
                    result.Failure is null ? null : GroupFailureToken(result.Failure.Kind),
                    Is.EqualTo(
                        expectedFailure.ValueKind == JsonValueKind.Null ? null : expectedFailure.GetString()),
                    scenario);
                Assert.That(
                    result.Failure?.Code,
                    Is.EqualTo(expectedCode.ValueKind == JsonValueKind.Null ? null : expectedCode.GetString()),
                    scenario);
                Assert.That(
                    result.Members,
                    Has.Count.EqualTo(vector.GetProperty("expectedMembers").GetInt32()),
                    scenario);
                Assert.That(
                    result.Members.Count(item => item.Member.IsReleased),
                    Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                    scenario);
                Assert.That(
                    result.Members.Count(item => item.Member.Stage == PortableCompositionStage.Retired),
                    Is.EqualTo(vector.GetProperty("expectedRetired").GetInt32()),
                    scenario);
                Assert.That(
                    result.Runtime is null ? (bool?)null : result.Runtime.IsActive,
                    Is.EqualTo(
                        expectedRuntime.ValueKind == JsonValueKind.Null
                            ? (bool?)null
                            : expectedRuntime.GetBoolean()),
                    scenario);

                // Either every member is released or none is.
                Assert.That(
                    result.Members.All(item => item.Member.IsReleased) ||
                        result.Members.All(item => !item.Member.IsReleased),
                    Is.True,
                    $"{scenario}: the release barrier is the activation, not the member.");
                Assert.That(handlers.Sum(handler => handler.ProviderEffectCount), Is.Zero, scenario);
            });
        }
    }

    [Test]
    public async Task A_failed_member_leaves_no_other_member_reachable()
    {
        var (result, _) = await GroupActivationResult("cbi12-02-second-member-refused");
        var survivor = result.Members[0].Member;

        var attempted = await survivor.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableConstraint.Atom(PortableTruth.Satisfied));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsActive, Is.False);
            Assert.That(result.Failure!.Member, Is.EqualTo(result.Members[1].Occurrence));
            Assert.That(survivor.Stage, Is.EqualTo(PortableCompositionStage.Retired));
            Assert.That(attempted.Category, Is.EqualTo(PortableProtocolCategory.StateViolation));
        });
    }

    private static async Task<(ComponentGroupActivationResult Result, CoolingPortableHandler[] Handlers)>
        GroupActivationResult(string scenario)
    {
        var resolution = new FakeGenerationResolver().Resolve(PairRequest());
        var first = resolution.Generation!.ProviderSets.Single(item => item.Requirement == Requirement);
        var second = resolution.Generation.ProviderSets.Single(item => item.Requirement == SecondaryRequirement);
        var handlers = new[]
        {
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
        };
        var substituted = CoolingPortableFixture.Contract with
        {
            Provider = PortableProviderReference.Parse("brontide.fake.substituted", 1),
        };
        var secondContract = scenario == "cbi12-02-second-member-refused"
            ? substituted
            : CoolingPortableFixture.Contract;
        var members = new ComponentGroupMember[]
        {
            new(
                Selection(first.Members[0]) with { HostEndpoint = "group-host-primary" },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    CoolingPortableFixture.Contract,
                    handlers[0],
                    PortableRealization.FixedDirectCall))),
            new(
                Selection(second.Members[0]) with
                {
                    Requirement = SecondaryRequirement,
                    HostEndpoint = "group-host-secondary",
                },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    secondContract,
                    handlers[1],
                    PortableRealization.FixedDirectCall))),
        };
        if (scenario == "cbi12-03-preparation-refused")
        {
            members[1] = members[1] with
            {
                Selection = members[1].Selection with
                {
                    Requirement = RequirementId.Create("req.absent"),
                },
            };
        }

        var occurrences = members.Select(item => item.Selection.Occurrence).ToArray();
        var plan = scenario switch
        {
            "cbi12-04-unselected-member" => Plan([.. occurrences, OccurrenceId.Create("occ.extra")]),
            "cbi12-05-protocol-group" => ProtocolPlan(occurrences),
            _ => Plan(occurrences),
        };
        var runtimeRequest = RuntimeRequest(plan);
        if (scenario == "cbi12-06-runtime-refused")
        {
            runtimeRequest = runtimeRequest with
            {
                Release = new(ReleaseId.Create("release.integration"), ReleaseFailureMoment.BeforeCutover),
            };
        }

        var result = await ComponentGroupLifecycle.ActivateAsync(resolution, members, runtimeRequest);
        return (result, handlers);
    }

    [Test]
    public async Task Shared_cbi13_vectors_admit_every_member_before_any_provider_is_reached()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi13-group-authority-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, handlers) = await GroupAuthorityResult(scenario);
            var expectedFailure = vector.GetProperty("expectedFailureKind");

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsActive,
                    Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()),
                    scenario);
                Assert.That(
                    result.Failure is null ? null : GroupAuthorityToken(result.Failure.Kind),
                    Is.EqualTo(
                        expectedFailure.ValueKind == JsonValueKind.Null ? null : expectedFailure.GetString()),
                    scenario);
                Assert.That(
                    result.Admissions,
                    Has.Count.EqualTo(vector.GetProperty("expectedMembersAdmitted").GetInt32()),
                    scenario);
                Assert.That(
                    result.Grants,
                    Has.Count.EqualTo(vector.GetProperty("expectedGrants").GetInt32()),
                    scenario);
                Assert.That(
                    result.Lifecycle?.Members.Count(item => item.Member.IsReleased) ?? 0,
                    Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                    scenario);
                Assert.That(
                    handlers.Sum(handler => handler.ProviderEffectCount),
                    Is.EqualTo(vector.GetProperty("expectedProviderEffects").GetInt32()),
                    scenario);

                // The authority barrier is earlier than the release barrier: an authority refusal
                // never reaches a provider at all.
                if (result.Failure is { Kind: not ComponentGroupAuthorityFailureKind.ActivationRefused })
                {
                    Assert.That(result.Lifecycle, Is.Null, scenario);
                }
            });
        }
    }

    [Test]
    public async Task One_party_may_participate_in_two_members_through_one_local_actor()
    {
        var (result, _) = await GroupAuthorityResult("cbi13-02-shared-participant-consistent");
        var holders = result.Grants.Select(grant => grant.Holder).Distinct().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsActive, Is.True);
            Assert.That(
                result.Admissions.SelectMany(item => item.Participants).Select(item => item.Participant).Distinct(),
                Has.Exactly(1).Items,
                "One party participates in both members.");
            Assert.That(holders, Has.Length.EqualTo(1), "It maps onto exactly one receiving-domain Actor.");
            Assert.That(result.Grants, Has.Count.EqualTo(2), "It holds one grant per member.");
        });
    }

    private static async Task<(ComponentGroupAuthorityResult Result, CoolingPortableHandler[] Handlers)>
        GroupAuthorityResult(string scenario)
    {
        var resolution = new FakeGenerationResolver().Resolve(PairRequest());
        var first = resolution.Generation!.ProviderSets.Single(item => item.Requirement == Requirement);
        var second = resolution.Generation.ProviderSets.Single(item => item.Requirement == SecondaryRequirement);
        var handlers = new[]
        {
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
        };
        var secondContract = scenario == "cbi13-07-activation-refused-after-admission"
            ? CoolingPortableFixture.Contract with
            {
                Provider = PortableProviderReference.Parse("brontide.fake.substituted", 1),
            }
            : CoolingPortableFixture.Contract;
        var groupMembers = new[]
        {
            new ComponentGroupMember(
                Selection(first.Members[0]) with { HostEndpoint = "authority-host-primary" },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    CoolingPortableFixture.Contract,
                    handlers[0],
                    PortableRealization.FixedDirectCall))),
            new ComponentGroupMember(
                Selection(second.Members[0]) with
                {
                    Requirement = SecondaryRequirement,
                    HostEndpoint = "authority-host-secondary",
                },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    secondContract,
                    handlers[1],
                    PortableRealization.FixedDirectCall))),
        };

        // The second member's participant differs by default, so each member admits its own party.
        var sharesParticipant = scenario is "cbi13-02-shared-participant-consistent"
            or "cbi13-05-participant-two-local-actors";
        var secondaryActor = sharesParticipant ? Participant : Supervisor;
        var secondaryLocalActor = scenario switch
        {
            "cbi13-05-participant-two-local-actors" => SupervisorLocalActor,
            "cbi13-06-participants-one-local-actor" => ProviderLocalActor,
            _ => sharesParticipant ? ProviderLocalActor : SupervisorLocalActor,
        };
        // Only the second member's policy varies, so the activation-level mapping rules are what the
        // vectors exercise rather than any one member's admission.
        var policy = scenario == "cbi13-05-participant-two-local-actors"
            ? GroupPolicy(SupervisorLocalActor)
            : GroupPolicy(ProviderLocalActor, secondaryLocalActor);
        var firstParticipant = new ComponentParticipantRequest(
            new(groupMembers[0].Selection.Occurrence, Participant),
            ProviderAuthority(GroupPolicy(ProviderLocalActor), Authority));
        var secondaryAuthority = scenario == "cbi13-04-authority-identity-shared"
            ? Authority
            : ReportAuthority;
        var secondParticipant = new ComponentParticipantRequest(
            new(groupMembers[1].Selection.Occurrence, secondaryActor),
            secondaryActor == Participant
                ? ProviderAuthority(policy, secondaryAuthority) with
                {
                    Request = AdmissionRequestId.Create("admission.group-secondary"),
                    Relationships =
                    [
                        new(
                            RelationshipRequestId.Create("relationship.group-secondary"),
                            Participant,
                            ActorRelationshipKind.ComponentParticipant,
                            [AuthorityEvidence]),
                    ],
                    Authority =
                    [
                        new(
                            secondaryAuthority,
                            RelationshipRequestId.Create("relationship.group-secondary"),
                            ReportCapability,
                            Target,
                            ReportOperation,
                            AuthorityScope,
                            false),
                    ],
                }
                : SupervisorAuthority(policy, secondaryAuthority, scenario == "cbi13-03-second-member-denied"));

        var result = await ComponentGroupAuthority.ActivateAsync(
            resolution,
            [
                new(groupMembers[0], [firstParticipant]),
                new(groupMembers[1], [secondParticipant]),
            ],
            RuntimeRequest(Plan(groupMembers.Select(item => item.Selection.Occurrence).ToArray())));
        return (result, handlers);
    }

    /// <summary>The receiving-domain policy, with the participant's own local Actor overridable.</summary>
    private static LocalAuthorityPolicy GroupPolicy(
        LocalActorReferenceId participantActor,
        LocalActorReferenceId? supervisorActor = null,
        LocalActorReferenceId? observerActor = null)
    {
        var policy = SetPolicy(supervisorActor ?? SupervisorLocalActor, observerActor ?? ObserverLocalActor);
        return policy with
        {
            RelationshipRules = policy.RelationshipRules
                .Select(rule => rule.ProposedActor == Participant
                    ? rule with { LocalActor = participantActor }
                    : rule)
                .ToArray(),
        };
    }

    private static AuthorityAdmissionRequest ProviderAuthority(
        LocalAuthorityPolicy policy,
        AuthorityRequestId authority) =>
        new(
            AdmissionRequestId.Create("admission.group-provider"),
            Participant,
            EvaluationTime,
            [SetEvidence(AuthorityEvidence, Participant)],
            [new(Relationship, Participant, ActorRelationshipKind.ComponentParticipant, [AuthorityEvidence])],
            [
                authority == Authority
                    ? new(Authority, Relationship, Capability, Target, Operation, AuthorityScope, false)
                    : new(authority, Relationship, ReportCapability, Target, ReportOperation, AuthorityScope, false),
            ],
            policy);

    private static AuthorityAdmissionRequest SupervisorAuthority(
        LocalAuthorityPolicy policy,
        AuthorityRequestId authority,
        bool revoked)
    {
        var evidence = SetEvidence(SupervisorEvidence, Supervisor);
        return new(
            AdmissionRequestId.Create("admission.group-supervisor"),
            Supervisor,
            EvaluationTime,
            [revoked ? evidence with { State = AdmissionEvidenceState.Revoked } : evidence],
            [new(SupervisorRelationship, Supervisor, ActorRelationshipKind.ComponentParticipant, [SupervisorEvidence])],
            [new(authority, SupervisorRelationship, AuditCapability, Target, AuditOperation, AuthorityScope, false)],
            policy);
    }

    [Test]
    public async Task Shared_cbi14_vectors_retire_every_member_or_none()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi14-group-withdrawal-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, active, handlers) = await GroupRevalidationResult(scenario);
            var released = active.Lifecycle!.Members.Count(item => item.Member.IsReleased);

            Assert.Multiple(() =>
            {
                Assert.That(
                    GroupRevalidationToken(result.Kind),
                    Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                    scenario);
                Assert.That(
                    result.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                    scenario);
                Assert.That(
                    result.Members,
                    Has.Count.EqualTo(vector.GetProperty("expectedMembersEvaluated").GetInt32()),
                    scenario);
                Assert.That(
                    result.Lapsed,
                    Has.Count.EqualTo(vector.GetProperty("expectedLapsed").GetInt32()),
                    scenario);
                Assert.That(
                    released,
                    Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                    scenario);
                Assert.That(
                    result.Replacements,
                    Has.Count.EqualTo(vector.GetProperty("expectedReplacements").GetInt32()),
                    scenario);

                // The activation shares a restart scope, so it shares a fate.
                Assert.That(
                    released == active.Lifecycle.Members.Count || released == 0,
                    Is.True,
                    $"{scenario}: every member is released or none is.");
                Assert.That(handlers.Sum(handler => handler.ProviderEffectCount), Is.Zero, scenario);
            });
        }
    }

    [Test]
    public async Task One_member_losing_authority_retires_the_member_that_kept_it()
    {
        var (result, active, _) = await GroupRevalidationResult("cbi14-02-one-member-lapsed");
        var survivor = active.Lifecycle!.Members[0].Member;

        var attempted = await survivor.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableConstraint.Atom(PortableTruth.Satisfied));

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ComponentGroupRevalidationKind.Withdrawn));
            Assert.That(result.Lapsed, Is.EqualTo(new[] { active.Lifecycle.Members[1].Occurrence }));
            Assert.That(
                result.Members[0].Unrenewed,
                Is.Empty,
                "The member that kept its authority is retired without being named as the cause.");
            Assert.That(survivor.Stage, Is.EqualTo(PortableCompositionStage.Retired));
            Assert.That(attempted.Category, Is.EqualTo(PortableProtocolCategory.StateViolation));
        });
    }

    private static async Task<(
        ComponentGroupRevalidationResult Result,
        ComponentGroupAuthorityResult Active,
        CoolingPortableHandler[] Handlers)>
        GroupRevalidationResult(string scenario)
    {
        var failCleanup = scenario == "cbi14-06-retirement-failure";
        var (active, handlers, participants) = await AdmittedGroup(failCleanup);
        var first = active.Admissions[0].Occurrence;
        var second = active.Admissions[1].Occurrence;
        var lapse = scenario is "cbi14-02-one-member-lapsed" or "cbi14-06-retirement-failure"
            or "cbi14-03-both-members-lapsed";

        var requests = new List<ComponentGroupMemberRequests>
        {
            new(
                first,
                [
                    scenario == "cbi14-03-both-members-lapsed"
                        ? Revoked(participants[0])
                        : participants[0],
                ]),
            new(second, [lapse ? Revoked(participants[1]) : participants[1]]),
        };

        switch (scenario)
        {
            case "cbi14-04-member-set-changed":
                requests.RemoveAt(1);
                break;
            case "cbi14-05-participant-drift":
                requests[1] = new(
                    second,
                    [
                        participants[1] with
                        {
                            Authority =
                            [
                                participants[1].Authority.Single() with
                                {
                                    Capability = CapabilityId.Create("capability.other"),
                                },
                            ],
                        },
                    ]);
                break;
            default:
                break;
        }

        var result = await ComponentGroupRevalidation.RevalidateAsync(
            active,
            requests,
            $"group revalidation {scenario}");
        return (result, active, handlers);
    }

    private static async Task<(
        ComponentGroupAuthorityResult Active,
        CoolingPortableHandler[] Handlers,
        AuthorityAdmissionRequest[] Participants)>
        AdmittedGroup(bool failCleanup)
    {
        var resolution = new FakeGenerationResolver().Resolve(PairRequest());
        var first = resolution.Generation!.ProviderSets.Single(item => item.Requirement == Requirement);
        var second = resolution.Generation.ProviderSets.Single(item => item.Requirement == SecondaryRequirement);
        var handlers = new[]
        {
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
        };
        IPortableProviderConversation SecondConversation()
        {
            var conversation = new PortableDirectConversation(new PortableProviderEndpoint(
                CoolingPortableFixture.Contract,
                handlers[1],
                PortableRealization.FixedDirectCall));
            return failCleanup ? new FailingRetirementConversation(conversation) : conversation;
        }

        var groupMembers = new[]
        {
            new ComponentGroupMember(
                Selection(first.Members[0]) with { HostEndpoint = "withdrawal-host-primary" },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    CoolingPortableFixture.Contract,
                    handlers[0],
                    PortableRealization.FixedDirectCall))),
            new ComponentGroupMember(
                Selection(second.Members[0]) with
                {
                    Requirement = SecondaryRequirement,
                    HostEndpoint = "withdrawal-host-secondary",
                },
                SecondConversation()),
        };
        var participants = new[]
        {
            ProviderAuthority(GroupPolicy(ProviderLocalActor), Authority),
            SupervisorAuthority(GroupPolicy(ProviderLocalActor), ReportAuthority, revoked: false),
        };
        var active = await ComponentGroupAuthority.ActivateAsync(
            resolution,
            [
                new(groupMembers[0], [new(new(groupMembers[0].Selection.Occurrence, Participant), participants[0])]),
                new(groupMembers[1], [new(new(groupMembers[1].Selection.Occurrence, Supervisor), participants[1])]),
            ],
            RuntimeRequest(Plan(groupMembers.Select(item => item.Selection.Occurrence).ToArray())));
        return (active, handlers, participants);
    }

    [Test]
    public async Task Shared_cbi15_vectors_revise_per_member_and_check_the_activation()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi15-group-revision-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, active) = await GroupRevisionResult(scenario);
            var released = active.Lifecycle!.Members.Count(item => item.Member.IsReleased);

            Assert.Multiple(() =>
            {
                Assert.That(
                    GroupRevisionToken(result.Kind),
                    Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                    scenario);
                Assert.That(
                    result.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                    scenario);
                Assert.That(
                    result.CurrentAuthority,
                    Has.Count.EqualTo(vector.GetProperty("expectedEvaluated").GetInt32()),
                    scenario);
                Assert.That(
                    result.InForce?.Admissions.Sum(item => item.Participants.Count) ?? 0,
                    Is.EqualTo(vector.GetProperty("expectedInForceParticipants").GetInt32()),
                    scenario);
                Assert.That(
                    released,
                    Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                    scenario);

                // A declined change is local; only a lapse retires, and then the whole activation.
                Assert.That(
                    result.InForce,
                    result.Kind is ComponentGroupRevisionKind.Withdrawn
                        or ComponentGroupRevisionKind.RetirementFailed
                        ? Is.Null
                        : Is.Not.Null,
                    scenario);
                Assert.That(
                    released == active.Lifecycle.Members.Count || released == 0,
                    Is.True,
                    scenario);
            });
        }
    }

    [Test]
    public async Task A_lapse_in_an_untouched_member_retires_the_activation_being_revised()
    {
        var (result, active) = await GroupRevisionResult("cbi15-02-unchanged-member-lapsed");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ComponentGroupRevisionKind.Withdrawn));
            Assert.That(
                result.Lapsed,
                Is.EqualTo(new[] { active.Admissions[1].Occurrence }),
                "The member that lapsed is not the member being revised.");
            Assert.That(
                active.Lifecycle!.Members.All(item => item.Member.Stage == PortableCompositionStage.Retired),
                Is.True);
        });
    }

    private static async Task<(ComponentGroupRevisionResult Result, ComponentGroupAuthorityResult Active)>
        GroupRevisionResult(string scenario)
    {
        var resolution = new FakeGenerationResolver().Resolve(
            PairRequest(["cooling.control"], ["cooling.audit"]));
        var first = resolution.Generation!.ProviderSets.Single(item => item.Requirement == Requirement);
        var second = resolution.Generation.ProviderSets.Single(item => item.Requirement == SecondaryRequirement);
        var handlers = new[]
        {
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
        };
        var groupMembers = new[]
        {
            new ComponentGroupMember(
                Selection(first.Members[0]) with { HostEndpoint = "revision-host-primary" },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    CoolingPortableFixture.Contract, handlers[0], PortableRealization.FixedDirectCall))),
            new ComponentGroupMember(
                Selection(second.Members[0]) with
                {
                    Requirement = SecondaryRequirement,
                    HostEndpoint = "revision-host-secondary",
                },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    CoolingPortableFixture.Contract, handlers[1], PortableRealization.FixedDirectCall))),
        };
        var admitted = new[]
        {
            ProviderAuthority(GroupPolicy(ProviderLocalActor), Authority),
            SupervisorAuthority(GroupPolicy(ProviderLocalActor), AuditAuthority, revoked: false),
        };
        var active = await ComponentGroupAuthority.ActivateAsync(
            resolution,
            [
                new(groupMembers[0], [new(new(groupMembers[0].Selection.Occurrence, Participant), admitted[0])]),
                new(groupMembers[1], [new(new(groupMembers[1].Selection.Occurrence, Supervisor), admitted[1])]),
            ],
            RuntimeRequest(Plan(groupMembers.Select(item => item.Selection.Occurrence).ToArray())));

        // The first member gains an observer; the second member is restated unchanged.
        var observer = ObserverRequest(GroupPolicy(ProviderLocalActor));
        var firstDependency = new ComponentGrantDependency(
            groupMembers[0].Selection.Definition,
            [new("cooling.control", Capability, Target, Operation, AuthorityScope)]);
        var secondDependency = new ComponentGrantDependency(
            groupMembers[1].Selection.Definition,
            [new("cooling.audit", AuditCapability, Target, AuditOperation, AuthorityScope)]);

        var firstRequests = new List<AuthorityAdmissionRequest> { admitted[0], observer };
        var secondRequests = new List<AuthorityAdmissionRequest> { admitted[1] };
        switch (scenario)
        {
            case "cbi15-02-unchanged-member-lapsed":
                secondRequests[0] = Revoked(admitted[1]);
                break;
            case "cbi15-04-nothing-changed":
                firstRequests.RemoveAt(1);
                break;
            case "cbi15-05-identity-shared-across-members":
                firstRequests[1] = observer with
                {
                    Authority = [observer.Authority.Single() with { Request = AuditAuthority }],
                };
                break;
            case "cbi15-06-local-actor-shared-across-members":
                // The observer is mapped onto the Actor the second member's supervisor already holds.
                firstRequests[1] = ObserverRequest(
                    GroupPolicy(ProviderLocalActor, observerActor: SupervisorLocalActor));
                break;
            case "cbi15-07-dependency-not-covered":
                firstRequests.RemoveAt(0);
                break;
            case "cbi15-08-retained-identity-drift":
                firstRequests[0] = admitted[0] with
                {
                    Authority =
                    [
                        admitted[0].Authority.Single() with
                        {
                            Capability = CapabilityId.Create("capability.other"),
                        },
                    ],
                };
                break;
            default:
                break;
        }

        var revisions = new List<ComponentGroupMemberRevision>
        {
            new(groupMembers[0].Selection.Occurrence, groupMembers[0].Selection, firstDependency, firstRequests),
            new(groupMembers[1].Selection.Occurrence, groupMembers[1].Selection, secondDependency, secondRequests),
        };
        if (scenario == "cbi15-03-member-set-changed")
        {
            revisions.RemoveAt(1);
        }

        var result = await ComponentGroupRevision.ReviseAsync(
            resolution,
            active,
            revisions,
            $"group revision {scenario}");
        return (result, active);
    }

    [Test]
    public async Task Shared_cbi16_vectors_verify_every_member_against_its_own_declaration()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi16-group-verification-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, active, handlers) = await GroupVerificationResult(scenario);
            var released = active.Lifecycle!.Members.Count(item => item.Member.IsReleased);
            var expectedRuntime = vector.GetProperty("expectedRuntimeActive");

            Assert.Multiple(() =>
            {
                Assert.That(
                    GroupVerificationToken(result.Kind),
                    Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                    scenario);
                Assert.That(
                    result.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                    scenario);
                Assert.That(
                    result.Exercises,
                    Has.Count.EqualTo(vector.GetProperty("expectedExercises").GetInt32()),
                    scenario);
                Assert.That(
                    result.Violating,
                    Has.Count.EqualTo(vector.GetProperty("expectedViolating").GetInt32()),
                    scenario);
                Assert.That(
                    result.Members.Sum(item => item.Unexercised.Count),
                    Is.EqualTo(vector.GetProperty("expectedUnexercised").GetInt32()),
                    scenario);
                Assert.That(
                    result.Members.Sum(item => item.Uncovered.Count),
                    Is.EqualTo(vector.GetProperty("expectedUncovered").GetInt32()),
                    scenario);
                Assert.That(
                    result.Runtime is null
                        ? (bool?)null
                        : result.Runtime.Kind == ActivationRuntimeOutcomeKind.Active,
                    Is.EqualTo(
                        expectedRuntime.ValueKind == JsonValueKind.Null
                            ? (bool?)null
                            : expectedRuntime.GetBoolean()),
                    scenario);
                Assert.That(
                    released,
                    Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                    scenario);
                Assert.That(
                    handlers.Sum(handler => handler.ProviderEffectCount),
                    Is.EqualTo(vector.GetProperty("expectedProviderEffects").GetInt32()),
                    scenario);

                // The runtime accepts the one projection exactly when every member is consistent.
                if (result.Runtime is { } runtime)
                {
                    Assert.That(
                        runtime.Kind == ActivationRuntimeOutcomeKind.Active,
                        Is.EqualTo(result.IsConsistent),
                        scenario);
                }

                // A structural refusal evaluates nothing; a violation retires the whole activation.
                Assert.That(
                    released == active.Lifecycle.Members.Count || released == 0,
                    Is.True,
                    scenario);
                Assert.That(
                    result.Violating.Count == 0 || released == 0,
                    Is.True,
                    $"{scenario}: a violation in any member closes every member's gate.");
                Assert.That(
                    result.Members.All(item => item.IsViolating == result.Violating.Contains(item.Occurrence)),
                    Is.True,
                    $"{scenario}: only members with a failed attribution are named as violating.");
            });
        }
    }

    [Test]
    public async Task One_members_undeclared_use_is_condemned_by_the_runtime_for_the_whole_activation()
    {
        var (result, active, _) = await GroupVerificationResult("cbi16-05-one-member-undeclared");
        var survivor = active.Lifecycle!.Members[0].Member;

        var attempted = await survivor.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableConstraint.Atom(PortableTruth.Satisfied));

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ComponentGroupVerificationKind.UndeclaredUse));
            Assert.That(
                result.Runtime!.Kind,
                Is.EqualTo(ActivationRuntimeOutcomeKind.BindingObservationConflict),
                "One request carries every member's exercises, so CM4's own rule refuses all of them.");
            Assert.That(
                result.Violating,
                Is.EqualTo(new[] { active.Admissions[1].Occurrence }),
                "The member that stayed inside its declaration is retired without being named as the cause.");
            Assert.That(result.Members[0].IsViolating, Is.False);
            Assert.That(attempted.Category, Is.EqualTo(PortableProtocolCategory.StateViolation));
            Assert.That(result.Replacements, Has.Count.EqualTo(active.Lifecycle.Members.Count));
        });
    }

    [Test]
    public async Task The_same_operation_is_attributed_separately_in_each_member()
    {
        var (result, _, _) = await GroupVerificationResult("cbi16-02-same-operation-in-both-members");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsConsistent, Is.True);
            Assert.That(
                result.Exercises.Select(item => item.Exercise.Value).Distinct().Count(),
                Is.EqualTo(2),
                "One CM4 request refuses a repeated binding-exercise identity.");
            Assert.That(
                result.Exercises.All(item => item.AuthorityAdmitted),
                Is.True,
                "Each member's admission is derived from its own declaration and its own grants.");
        });
    }

    private static async Task<(
        ComponentGroupVerificationResult Result,
        ComponentGroupAuthorityResult Active,
        CoolingPortableHandler[] Handlers)>
        GroupVerificationResult(string scenario)
    {
        var resolution = new FakeGenerationResolver().Resolve(
            PairRequest(["cooling.control"], ["cooling.audit"]));
        var first = resolution.Generation!.ProviderSets.Single(item => item.Requirement == Requirement);
        var second = resolution.Generation.ProviderSets.Single(item => item.Requirement == SecondaryRequirement);
        var handlers = new[]
        {
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
        };
        IPortableProviderConversation SecondConversation()
        {
            var conversation = new PortableDirectConversation(new PortableProviderEndpoint(
                CoolingPortableFixture.Contract, handlers[1], PortableRealization.FixedDirectCall));
            return scenario == "cbi16-08-retirement-failure"
                ? new FailingRetirementConversation(conversation)
                : conversation;
        }

        var groupMembers = new[]
        {
            new ComponentGroupMember(
                Selection(first.Members[0]) with { HostEndpoint = "verification-host-primary" },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    CoolingPortableFixture.Contract, handlers[0], PortableRealization.FixedDirectCall))),
            new ComponentGroupMember(
                Selection(second.Members[0]) with
                {
                    Requirement = SecondaryRequirement,
                    HostEndpoint = "verification-host-secondary",
                },
                SecondConversation()),
        };
        var runtimeRequest = RuntimeRequest(Plan(groupMembers.Select(item => item.Selection.Occurrence).ToArray()));
        var active = await ComponentGroupAuthority.ActivateAsync(
            resolution,
            [
                new(
                    groupMembers[0],
                    [new(new(groupMembers[0].Selection.Occurrence, Participant), ProviderAuthority(GroupPolicy(ProviderLocalActor), Authority))]),
                new(
                    groupMembers[1],
                    [new(new(groupMembers[1].Selection.Occurrence, Supervisor), SupervisorAuthority(GroupPolicy(ProviderLocalActor), AuditAuthority, revoked: false))]),
            ],
            runtimeRequest);

        // The observations are real: the host invokes each released member and records what came back.
        var silent = scenario is "cbi16-01-one-member-interacted" or "cbi16-03-nothing-observed"
            or "cbi16-04-denied-before-any-frame";
        var observations = new List<ComponentObservedInteraction>[] { [], [] };
        for (var index = 0; index < groupMembers.Length; index++)
        {
            if (scenario == "cbi16-03-nothing-observed" || (index == 1 && silent))
            {
                continue;
            }

            var result = await active.Lifecycle!.Members[index].Member.InvokeAsync(
                CoolingPortableFixture.SetEnabled,
                CoolingPortableFixture.CommandV1,
                CoolingPortableFixture.Command("primary", enabled: true),
                scenario == "cbi16-04-denied-before-any-frame"
                    ? PortableConstraint.Atom(PortableTruth.Unsatisfied)
                    : PortableConstraint.Atom(PortableTruth.Satisfied));
            observations[index].Add(new(CoolingPortableFixture.SetEnabled, result));
        }

        var auditScope = scenario is "cbi16-06-one-member-ungranted" or "cbi16-07-undeclared-outranks-ungranted"
            ? CapabilityScopeId.Create("scope.other")
            : AuthorityScope;
        var interactions = new List<ComponentGroupMemberInteractions>
        {
            new(
                groupMembers[0].Selection,
                new(
                    groupMembers[0].Selection.Definition,
                    [new("cooling.control", Capability, Target, Operation, AuthorityScope)]),
                scenario switch
                {
                    "cbi16-07-undeclared-outranks-ungranted" => [new(CoolingPortableFixture.SetEnabled, "cooling.other")],
                    "cbi16-10-mapping-not-distinct" =>
                    [
                        new(CoolingPortableFixture.SetEnabled, "cooling.control"),
                        new(CoolingPortableFixture.SetEnabled, "cooling.other"),
                    ],
                    _ => [new(CoolingPortableFixture.SetEnabled, "cooling.control")],
                },
                observations[0]),
            new(
                groupMembers[1].Selection,
                new(
                    groupMembers[1].Selection.Definition,
                    [
                        new(
                            scenario == "cbi16-11-declaration-mismatch" ? "cooling.other" : "cooling.audit",
                            AuditCapability,
                            Target,
                            AuditOperation,
                            auditScope),
                    ]),
                scenario is "cbi16-05-one-member-undeclared" or "cbi16-08-retirement-failure"
                    ? [new(CoolingPortableFixture.SetEnabled, "cooling.other")]
                    : [new(CoolingPortableFixture.SetEnabled, "cooling.audit")],
                observations[1]),
        };
        if (scenario == "cbi16-09-member-set-changed")
        {
            interactions.RemoveAt(1);
        }

        var verdict = await ComponentGroupVerification.VerifyAsync(
            resolution,
            active,
            interactions,
            runtimeRequest,
            $"group verification {scenario}");
        return (verdict, active, handlers);
    }

    [Test]
    public async Task Shared_cbi17_vectors_narrow_every_member_or_none()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi17-group-succession-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, active, effects) = await GroupSuccessionResult(scenario);
            var released = active.Lifecycle!.Members.Count(item => item.Member.IsReleased);

            Assert.Multiple(() =>
            {
                Assert.That(
                    GroupSuccessionToken(result.Kind),
                    Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                    scenario);
                Assert.That(
                    result.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                    scenario);
                Assert.That(
                    result.Members.Sum(item => item.Dropped.Count),
                    Is.EqualTo(vector.GetProperty("expectedDropped").GetInt32()),
                    scenario);
                Assert.That(
                    result.Members.Sum(item => item.Vetoed.Count),
                    Is.EqualTo(vector.GetProperty("expectedVetoed").GetInt32()),
                    scenario);
                Assert.That(
                    result.Narrowed,
                    Has.Count.EqualTo(vector.GetProperty("expectedNarrowedMembers").GetInt32()),
                    scenario);
                Assert.That(
                    result.Members.Sum(item => item.Declaration.Entries.Count),
                    Is.EqualTo(vector.GetProperty("expectedDeclaredInForce").GetInt32()),
                    scenario);
                Assert.That(
                    released,
                    Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                    scenario);

                // This slice has no retirement path and reaches no provider.
                Assert.That(released, Is.EqualTo(active.Lifecycle.Members.Count), scenario);
                Assert.That(
                    effects.After,
                    Is.EqualTo(effects.Before),
                    $"{scenario}: succession performs nothing, so no member's provider is reached.");

                // A veto anywhere refuses every member's narrowing, and vice versa.
                Assert.That(
                    result.Vetoing.Count > 0,
                    Is.EqualTo(result.Members.Any(item => item.Vetoed.Count > 0)),
                    scenario);
                Assert.That(
                    result.Narrowed.Count > 0,
                    Is.EqualTo(result.IsNarrowed),
                    $"{scenario}: an applied succession narrows at least one member, and a refused one narrows none.");
            });
        }
    }

    [Test]
    public async Task A_successor_that_narrows_one_member_leaves_the_other_untouched()
    {
        var (result, active, _) = await GroupSuccessionResult("cbi17-02-one-member-unchanged");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsNarrowed, Is.True);
            Assert.That(
                result.Narrowed,
                Is.EqualTo(new[] { active.Admissions[0].Occurrence }),
                "A member the successor does not narrow is untouched rather than refusing the succession.");
            Assert.That(result.Members[1].Dropped, Is.Empty);
            Assert.That(result.Members[1].Declaration.Entries, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task A_member_the_successor_does_not_resolve_blocks_every_other_member()
    {
        var (result, _, _) = await GroupSuccessionResult("cbi17-07-member-position-absent");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ComponentGroupSuccessionKind.Declined));
            Assert.That(result.Code, Is.EqualTo("successor-position-mismatch"));
            Assert.That(
                result.Members.All(item => item.Dropped.Count == 0),
                Is.True,
                "A generation that does not resolve one member's position narrows none of them.");
        });
    }

    [Test]
    public async Task A_veto_in_one_member_refuses_the_narrowing_the_other_had_earned()
    {
        var (result, active, _) = await GroupSuccessionResult("cbi17-03-use-vetoed-in-other-member");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ComponentGroupSuccessionKind.Declined));
            Assert.That(
                result.Vetoing,
                Is.EqualTo(new[] { active.Admissions[1].Occurrence }),
                "The member that vetoed is named; the one whose narrowing it refused is not.");
            Assert.That(result.Members[0].Vetoed, Is.Empty);
            Assert.That(
                result.Members[0].Dropped,
                Is.Empty,
                "One transaction: the member with no veto drops nothing either.");
            Assert.That(result.Members[1].Vetoed, Is.EqualTo(new[] { "cooling.observe" }));
        });
    }

    [Test]
    public async Task A_narrowed_activation_lets_cbi15_release_the_participant_it_kept()
    {
        var (resolution, active, members, handlers) = await SuccessionActivation();
        var observations = await SuccessionObservations(active, handlers);

        // Dropping the supervisor is refused while the declaration in force still needs its grant.
        ComponentGroupMemberRevision Revision(int index, ComponentGrantDependency declaration) =>
            new(
                members[index].Occurrence,
                members[index].Selection,
                declaration,
                index == 0
                    ? [members[0].Participants[0]]
                    : members[1].Participants);
        var before = await ComponentGroupRevision.ReviseAsync(
            resolution,
            active,
            [Revision(0, members[0].Declaration), Revision(1, members[1].Declaration)],
            "drop the supervisor before succession");

        var successor = new FakeGenerationResolver().Resolve(
            PairRequest(["cooling.control"], ["cooling.observe"]));
        var narrowed = ComponentGroupSuccession.Succeed(
            resolution,
            successor,
            active,
            members.Select((member, index) => new ComponentGroupMemberSuccession(
                member.Selection,
                member.Declaration,
                NarrowedDeclaration(member, index),
                member.Attribution,
                observations[index])).ToArray());

        var after = await ComponentGroupRevision.ReviseAsync(
            successor,
            active,
            [
                Revision(0, narrowed.Members[0].Declaration),
                Revision(1, narrowed.Members[1].Declaration),
            ],
            "drop the supervisor after succession");

        Assert.Multiple(() =>
        {
            Assert.That(before.Code, Is.EqualTo("dependency-not-covered"));
            Assert.That(narrowed.IsNarrowed, Is.True);
            Assert.That(narrowed.Members[0].Dropped, Is.EqualTo(new[] { "cooling.audit" }));
            Assert.That(after.Kind, Is.EqualTo(ComponentGroupRevisionKind.Revised));
            Assert.That(
                after.InForce!.Admissions[0].Participants,
                Has.Count.EqualTo(1),
                "Narrowing permits the revision; it does not perform it.");
        });
    }

    private static async Task<(
        ComponentGroupSuccessionResult Result,
        ComponentGroupAuthorityResult Active,
        (long Before, long After) Effects)>
        GroupSuccessionResult(string scenario)
    {
        var (resolution, active, members, handlers) = await SuccessionActivation();
        var observations = await SuccessionObservations(active, handlers);
        var before = handlers.Sum(handler => handler.ProviderEffectCount);

        var successorRequest = scenario switch
        {
            "cbi17-02-one-member-unchanged" => PairRequest(["cooling.control"], ["cooling.observe", "cooling.report"]),
            "cbi17-03-use-vetoed-in-other-member" => PairRequest(["cooling.control"], ["cooling.report"]),
            "cbi17-04-activation-unchanged" =>
                PairRequest(["cooling.control", "cooling.audit"], ["cooling.observe", "cooling.report"]),
            "cbi17-05-wider-in-one-member" =>
                PairRequest(["cooling.control", "cooling.audit", "cooling.observe"], ["cooling.observe"]),
            "cbi17-07-member-position-absent" => RescopedSecondary(),
            "cbi17-08-successor-declares-nothing" => PairRequest(["cooling.control"], []),
            _ => PairRequest(["cooling.control"], ["cooling.observe"]),
        };
        var successor = new FakeGenerationResolver().Resolve(successorRequest);

        var successions = members
            .Select((member, index) => new ComponentGroupMemberSuccession(
                member.Selection,
                member.Declaration,
                scenario switch
                {
                    "cbi17-04-activation-unchanged" => member.Declaration,
                    "cbi17-05-wider-in-one-member" when index == 0 => new(
                        member.Selection.Definition,
                        [
                            .. member.Declaration.Entries,
                            new("cooling.observe", ObserveCapability, Target, ObserveOperation, AuthorityScope),
                        ]),
                    "cbi17-06-tuple-changed" when index == 0 => new(
                        member.Selection.Definition,
                        [
                            new(
                                "cooling.control",
                                Capability,
                                Target,
                                Operation,
                                CapabilityScopeId.Create("scope.other")),
                        ]),
                    "cbi17-08-successor-declares-nothing" when index == 1 => new(
                        member.Selection.Definition,
                        Array.Empty<ComponentGrantDependencyEntry>()),
                    _ => NarrowedDeclaration(member, index, scenario),
                },
                scenario == "cbi17-10-ambiguous-attribution" && index == 0
                    ? [.. member.Attribution, new(CoolingPortableFixture.SetEnabled, "cooling.audit")]
                    : member.Attribution,
                observations[index]))
            .ToList();
        if (scenario == "cbi17-09-member-set-changed")
        {
            successions.RemoveAt(1);
        }

        var result = ComponentGroupSuccession.Succeed(resolution, successor, active, successions);
        return (result, active, (before, handlers.Sum(handler => handler.ProviderEffectCount)));
    }

    /// <summary>The successor a scenario narrows to, in the shape its generation records.</summary>
    private static ComponentGrantDependency NarrowedDeclaration(
        SuccessionMember member,
        int index,
        string scenario = "")
    {
        var kept = index == 0
            ? "cooling.control"
            : scenario == "cbi17-03-use-vetoed-in-other-member" ? "cooling.report" : "cooling.observe";
        var retained = member.Declaration.Entries
            .Where(entry => entry.DeclaredAuthority == kept)
            .ToArray();
        return new(
            member.Selection.Definition,
            scenario == "cbi17-02-one-member-unchanged" && index == 1 ? member.Declaration.Entries : retained);
    }

    /// <summary>A successor that resolves the secondary position under a different binding scope.</summary>
    private static ResolutionRequest RescopedSecondary()
    {
        var request = PairRequest(["cooling.control"], ["cooling.observe"]);
        var consumer = request.Definitions.Single(item => item.Definition == Consumer);
        return request with
        {
            Definitions = request.Definitions
                .Select(item => item.Definition == Consumer
                    ? consumer with
                    {
                        Requirements = consumer.Requirements
                            .Select(requirement => requirement.Requirement == SecondaryRequirement
                                ? requirement with { Scope = BindingScopeId.Create("scope.cooling-successor") }
                                : requirement)
                            .ToArray(),
                    }
                    : item)
                .ToArray(),
        };
    }

    private sealed record SuccessionMember(
        OccurrenceId Occurrence,
        ComponentBindingSelection Selection,
        ComponentGrantDependency Declaration,
        IReadOnlyList<ComponentOperationAuthorityMapping> Attribution,
        IReadOnlyList<AuthorityAdmissionRequest> Participants);

    /// <summary>
    /// Two released members, the first covering its two declared authorities with two participants
    /// so a later CBI15 revision has one to release.
    /// </summary>
    private static async Task<(
        ResolutionOutcome Resolution,
        ComponentGroupAuthorityResult Active,
        SuccessionMember[] Members,
        CoolingPortableHandler[] Handlers)>
        SuccessionActivation()
    {
        var resolution = new FakeGenerationResolver().Resolve(
            PairRequest(["cooling.control", "cooling.audit"], ["cooling.observe", "cooling.report"]));
        var first = resolution.Generation!.ProviderSets.Single(item => item.Requirement == Requirement);
        var second = resolution.Generation.ProviderSets.Single(item => item.Requirement == SecondaryRequirement);
        var handlers = new[]
        {
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
        };
        var groupMembers = new[]
        {
            new ComponentGroupMember(
                Selection(first.Members[0]) with { HostEndpoint = "succession-host-primary" },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    CoolingPortableFixture.Contract, handlers[0], PortableRealization.FixedDirectCall))),
            new ComponentGroupMember(
                Selection(second.Members[0]) with
                {
                    Requirement = SecondaryRequirement,
                    HostEndpoint = "succession-host-secondary",
                },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    CoolingPortableFixture.Contract, handlers[1], PortableRealization.FixedDirectCall))),
        };

        var policy = GroupPolicy(ProviderLocalActor);
        var observer = ObserverRequest(policy) with
        {
            Authority =
            [
                new(ObserveAuthority, ObserverRelationship, ObserveCapability, Target, ObserveOperation, AuthorityScope, false),
                new(ReportAuthority, ObserverRelationship, ReportCapability, Target, ReportOperation, AuthorityScope, false),
            ],
        };
        var members = new[]
        {
            new SuccessionMember(
                groupMembers[0].Selection.Occurrence,
                groupMembers[0].Selection,
                Dependency(groupMembers[0].Selection.Definition),
                [new(CoolingPortableFixture.SetEnabled, "cooling.control")],
                [ProviderAuthority(policy, Authority), SupervisorAuthority(policy, AuditAuthority, revoked: false)]),
            new SuccessionMember(
                groupMembers[1].Selection.Occurrence,
                groupMembers[1].Selection,
                new(
                    groupMembers[1].Selection.Definition,
                    [
                        new("cooling.observe", ObserveCapability, Target, ObserveOperation, AuthorityScope),
                        new("cooling.report", ReportCapability, Target, ReportOperation, AuthorityScope),
                    ]),
                [new(CoolingPortableFixture.SetEnabled, "cooling.observe")],
                [observer]),
        };
        var active = await ComponentGroupAuthority.ActivateAsync(
            resolution,
            [
                new(
                    groupMembers[0],
                    [
                        new(new(members[0].Occurrence, Participant), members[0].Participants[0]),
                        new(new(members[0].Occurrence, Supervisor), members[0].Participants[1]),
                    ]),
                new(groupMembers[1], [new(new(members[1].Occurrence, Observer), members[1].Participants[0])]),
            ],
            RuntimeRequest(Plan(groupMembers.Select(item => item.Selection.Occurrence).ToArray())));
        return (resolution, active, members, handlers);
    }

    /// <summary>Each member interacts once, so each has an exercised authority of its own.</summary>
    private static async Task<List<ComponentObservedInteraction>[]> SuccessionObservations(
        ComponentGroupAuthorityResult active,
        CoolingPortableHandler[] handlers)
    {
        var observations = new List<ComponentObservedInteraction>[] { [], [] };
        for (var index = 0; index < handlers.Length; index++)
        {
            var result = await active.Lifecycle!.Members[index].Member.InvokeAsync(
                CoolingPortableFixture.SetEnabled,
                CoolingPortableFixture.CommandV1,
                CoolingPortableFixture.Command("primary", enabled: true),
                PortableConstraint.Atom(PortableTruth.Satisfied));
            observations[index].Add(new(CoolingPortableFixture.SetEnabled, result));
        }

        return observations;
    }

    [Test]
    public async Task Shared_cbi19_vectors_replace_the_generation_in_one_scope()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi19-scoped-replacement-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, retained) = await ReplacementResult(scenario);
            var retainedMembers = retained.Lifecycle!.Members;
            var successorReleased = result.Successor?.Lifecycle?.Members
                .Count(item => item.Member.IsReleased) ?? 0;

            Assert.Multiple(() =>
            {
                Assert.That(
                    ReplacementToken(result.Kind),
                    Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                    scenario);
                Assert.That(
                    result.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                    scenario);
                Assert.That(
                    result.CutOver,
                    Is.EqualTo(vector.GetProperty("expectedCutover").GetBoolean()),
                    scenario);
                Assert.That(
                    successorReleased,
                    Is.EqualTo(vector.GetProperty("expectedSuccessorReleased").GetInt32()),
                    scenario);
                Assert.That(
                    retainedMembers.Count(item => item.Member.IsReleased),
                    Is.EqualTo(vector.GetProperty("expectedRetainedReleased").GetInt32()),
                    scenario);
                Assert.That(
                    retainedMembers.Count(item => item.Member.Stage == PortableCompositionStage.Retired),
                    Is.EqualTo(vector.GetProperty("expectedRetainedRetired").GetInt32()),
                    scenario);
                Assert.That(
                    result.Successor?.Admissions.Count ?? 0,
                    Is.EqualTo(vector.GetProperty("expectedAdmitted").GetInt32()),
                    scenario);

                // Cutover is the boundary in both directions.
                Assert.That(
                    result.Retired.Count > 0,
                    Is.EqualTo(result.CutOver),
                    $"{scenario}: retained members are retired exactly when the scope cut over.");
                Assert.That(
                    successorReleased == (result.Successor?.Lifecycle?.Members.Count ?? 0) ||
                        successorReleased == 0,
                    Is.True,
                    $"{scenario}: the release barrier arms for the whole successor activation.");
                Assert.That(
                    result.CutOver || retainedMembers.All(item => item.Member.IsReleased),
                    Is.True,
                    $"{scenario}: before cutover the retained activation is untouched.");
            });
        }
    }

    [Test]
    public async Task C1_replacement_needs_a_released_activation_and_a_successor_for_the_same_scope()
    {
        var unavailable = await ComponentGroupReplacement.ReplaceAsync(
            new FakeGenerationResolver().Resolve(PairRequest()),
            new(
                Array.Empty<ComponentGroupMemberAdmission>(),
                Array.Empty<LocalCapabilityGrant>(),
                null,
                null),
            Array.Empty<ComponentGroupParticipant>(),
            RuntimeRequest(Plan()),
            "replacement unavailable");
        var (scope, _) = await ReplacementResult("cbi19-02-scope-mismatch");
        var (sameGeneration, _) = await ReplacementResult("cbi19-03-generation-not-successor");
        var (retainedMismatch, _) = await ReplacementResult("cbi19-04-retained-generation-mismatch");

        Assert.Multiple(() =>
        {
            Assert.That(unavailable.Kind, Is.EqualTo(ComponentGroupReplacementKind.ActivationUnavailable));
            Assert.That(scope.Code, Is.EqualTo("restart-scope-mismatch"));
            Assert.That(sameGeneration.Code, Is.EqualTo("generation-not-successor"));
            Assert.That(retainedMismatch.Code, Is.EqualTo("retained-generation-mismatch"));
            Assert.That(
                new[] { unavailable, scope, sameGeneration, retainedMismatch }.All(item =>
                    item.Successor is null && !item.CutOver && item.Retired.Count == 0),
                Is.True,
                "Every refusal before establishment creates no successor and cuts nothing over.");
        });
    }

    [Test]
    public async Task C2_authority_is_re_established_and_follows_the_occurrence()
    {
        var (changed, changedRetained) = await ReplacementResult("cbi19-05-surviving-occurrence-authority-changed");
        var (replaced, retained) = await ReplacementResult("cbi19-01-surviving-occurrences-replaced");

        Assert.Multiple(() =>
        {
            Assert.That(
                changed.Code,
                Is.EqualTo("authority-revalidation-mismatch"),
                "A surviving occurrence may not be re-admitted for different authority.");
            Assert.That(changed.Successor, Is.Null);
            Assert.That(
                changedRetained.Lifecycle!.Members.All(item => item.Member.IsReleased),
                Is.True);

            // Re-established, not inherited: the successor carries its own admissions from this
            // attempt, over the same durable occurrences.
            Assert.That(replaced.Successor!.Admissions.Select(item => item.Occurrence),
                Is.EqualTo(retained.Admissions.Select(item => item.Occurrence)));
            Assert.That(
                replaced.Successor.Admissions.SelectMany(item => item.Participants).Count(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public async Task C3_the_successor_stands_up_under_cbi13_barriers()
    {
        var (denied, retained) = await ReplacementResult("cbi19-06-successor-authority-denied");

        Assert.Multiple(() =>
        {
            Assert.That(denied.Code, Is.EqualTo("authority-not-admitted"));
            Assert.That(
                denied.Successor!.Lifecycle,
                Is.Null,
                "An admission refusal contacts no successor provider at all.");
            Assert.That(
                retained.Lifecycle!.Members.All(item => item.Member.IsReleased),
                Is.True,
                "And it leaves the retained activation released.");
        });
    }

    [Test]
    public async Task C4_the_release_barrier_re_arms_for_the_whole_successor_activation()
    {
        var (refused, _) = await ReplacementResult("cbi19-07-successor-member-never-ready");
        var (replaced, _) = await ReplacementResult("cbi19-01-surviving-occurrences-replaced");

        Assert.Multiple(() =>
        {
            Assert.That(
                refused.Successor!.Lifecycle!.Members.Count(item => item.Member.IsReleased),
                Is.Zero,
                "One member that never reports Ready releases none of them.");
            Assert.That(
                replaced.Successor!.Lifecycle!.Members.All(item => item.Member.IsReleased),
                Is.True);
            Assert.That(replaced.Successor.Lifecycle.Members, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task C5_before_cutover_the_retained_activation_is_untouched()
    {
        var (refused, retained) = await ReplacementResult("cbi19-08-release-fails-before-cutover");
        var survivor = retained.Lifecycle!.Members[0].Member;
        var attempted = await survivor.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableConstraint.Atom(PortableTruth.Satisfied));

        Assert.Multiple(() =>
        {
            Assert.That(refused.Code, Is.EqualTo("release-failed-before-cutover"));
            Assert.That(refused.CutOver, Is.False);
            Assert.That(
                retained.Lifecycle.Members.All(item => item.Member.IsReleased),
                Is.True,
                "The retained activation was never stood down, so it does not need restoring.");
            Assert.That(
                attempted.FrameDecision,
                Is.Not.EqualTo(PortableFrameDecision.None),
                "It is still serving ordinary interaction.");
        });
    }

    [Test]
    public async Task C6_the_retained_members_are_retired_after_cutover_and_never_before()
    {
        var (replaced, retained) = await ReplacementResult("cbi19-01-surviving-occurrences-replaced");
        var (refused, untouched) = await ReplacementResult("cbi19-07-successor-member-never-ready");

        Assert.Multiple(() =>
        {
            Assert.That(replaced.CutOver, Is.True);
            Assert.That(
                retained.Lifecycle!.Members.All(item => item.Member.Stage == PortableCompositionStage.Retired),
                Is.True,
                "Every retained member is retired once the scope cut over.");
            Assert.That(replaced.Retired, Has.Count.EqualTo(2));

            Assert.That(refused.CutOver, Is.False);
            Assert.That(
                untouched.Lifecycle!.Members.Any(item => item.Member.Stage == PortableCompositionStage.Retired),
                Is.False,
                "And none is retired when cutover did not happen.");
            Assert.That(refused.Retired, Is.Empty);
        });
    }

    [Test]
    public async Task C7_a_cleanup_failure_after_cutover_stays_visible_and_does_not_undo_it()
    {
        var (result, _) = await ReplacementResult("cbi19-09-retained-cleanup-fails-after-cutover");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ComponentGroupReplacementKind.CleanupFailed));
            Assert.That(result.Code, Is.EqualTo("retained-retirement-failed"));
            Assert.That(result.CutOver, Is.True);
            Assert.That(
                result.Successor!.Lifecycle!.Members.All(item => item.Member.IsReleased),
                Is.True,
                "The scope has already cut over, so the successor stays released.");
            Assert.That(result.Reason, Does.Contain("withdraw-refused"));
        });
    }

    [Test]
    public async Task C8_a_replacement_produces_an_activation_the_other_slices_accept()
    {
        var (result, _) = await ReplacementResult("cbi19-01-surviving-occurrences-replaced");
        var continued = await ComponentGroupRevalidation.RevalidateAsync(
            result.Successor!,
            SuccessorRequests(result.Successor!),
            "revalidate the successor activation");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsReplaced, Is.True);
            Assert.That(
                continued.Kind,
                Is.EqualTo(ComponentGroupRevalidationKind.Continued),
                "CBI14 accepts the activation a replacement produced.");
        });
    }

    [Test]
    public async Task C9_the_replacer_adds_no_grant_and_widens_no_scope()
    {
        var (result, retained) = await ReplacementResult("cbi19-01-surviving-occurrences-replaced");
        var observation = result.Successor!.Lifecycle!.Runtime!.Observation;

        Assert.Multiple(() =>
        {
            Assert.That(
                observation.RestartScope,
                Is.EqualTo(retained.Lifecycle!.Runtime!.Observation.RestartScope),
                "The successor occupies the scope the retained activation held; nothing widens.");
            Assert.That(
                observation.RetainedGeneration,
                Is.EqualTo(retained.Lifecycle.Runtime.Observation.TargetGeneration),
                "And it names the generation it replaced.");
            Assert.That(
                result.Successor.Grants.Count,
                Is.EqualTo(retained.Grants.Count),
                "Replacement grants no authority of its own.");
        });
    }

    [Test]
    public async Task C10_a_replacement_migrates_no_state_and_replaces_no_single_member()
    {
        var (result, retained) = await ReplacementResult("cbi19-01-surviving-occurrences-replaced");

        Assert.Multiple(() =>
        {
            Assert.That(
                retained.Lifecycle!.Members.Count,
                Is.EqualTo(result.Successor!.Lifecycle!.Members.Count),
                "The successor resolves the same positions; none is added or removed.");
            Assert.That(
                retained.Lifecycle.Members.Select(item => item.Member)
                    .Intersect(result.Successor.Lifecycle.Members.Select(item => item.Member))
                    .Any(),
                Is.False,
                "No portable member is carried across; the successor's are its own.");
            Assert.That(
                result.Retired,
                Has.Count.EqualTo(retained.Lifecycle.Members.Count),
                "The whole retained generation goes, never one member of it.");
        });
    }

    private static ComponentGroupMemberRequests[] SuccessorRequests(ComponentGroupAuthorityResult active) =>
        active.Admissions
            .Select(item => new ComponentGroupMemberRequests(
                item.Occurrence,
                item.Occurrence == active.Admissions[0].Occurrence
                    ? [ProviderAuthority(GroupPolicy(ProviderLocalActor), Authority)]
                    : [SupervisorAuthority(GroupPolicy(ProviderLocalActor), AuditAuthority, revoked: false)]))
            .ToArray();

    private static async Task<(
        ComponentGroupReplacementResult Result,
        ComponentGroupAuthorityResult Retained)>
        ReplacementResult(string scenario)
    {
        var (retained, retainedHandlers) = await ReplacementRetained(
            scenario == "cbi19-09-retained-cleanup-fails-after-cutover");
        _ = retainedHandlers;

        var successorResolution = new FakeGenerationResolver().Resolve(PairRequest());
        var first = successorResolution.Generation!.ProviderSets.Single(item => item.Requirement == Requirement);
        var second = successorResolution.Generation.ProviderSets.Single(item => item.Requirement == SecondaryRequirement);
        var handlers = new[]
        {
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
        };

        // A provider the required contract does not match never reports Ready.
        var secondDocument = scenario == "cbi19-07-successor-member-never-ready"
            ? CoolingPortableFixture.Contract with
            {
                Provider = PortableProviderReference.Parse("brontide.fake.substituted", 1),
            }
            : CoolingPortableFixture.Contract;
        var successorMembers = new[]
        {
            new ComponentGroupMember(
                Selection(first.Members[0]) with { HostEndpoint = "replacement-host-primary" },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    CoolingPortableFixture.Contract, handlers[0], PortableRealization.FixedDirectCall))),
            new ComponentGroupMember(
                Selection(second.Members[0]) with
                {
                    Requirement = SecondaryRequirement,
                    HostEndpoint = "replacement-host-secondary",
                },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    secondDocument, handlers[1], PortableRealization.FixedDirectCall))),
        };

        var policy = GroupPolicy(ProviderLocalActor);
        var providerRequest = scenario == "cbi19-05-surviving-occurrence-authority-changed"
            ? ProviderAuthority(policy, Authority) with
            {
                Authority =
                [
                    ProviderAuthority(policy, Authority).Authority.Single() with
                    {
                        Capability = CapabilityId.Create("capability.other"),
                    },
                ],
            }
            : ProviderAuthority(policy, Authority);
        var supervisorRequest = SupervisorAuthority(
            policy,
            AuditAuthority,
            revoked: scenario == "cbi19-06-successor-authority-denied");

        var occurrences = successorMembers.Select(item => item.Selection.Occurrence).ToArray();
        var plan = scenario switch
        {
            "cbi19-02-scope-mismatch" => PlanFor(
                GenerationId.Create("gen.successor"),
                RestartScopeId.Create("restart.other"),
                occurrences),
            "cbi19-03-generation-not-successor" => PlanFor(
                GenerationId.Create("gen.lifecycle"),
                RestartScopeId.Create("restart.lifecycle"),
                occurrences),
            _ => PlanFor(
                GenerationId.Create("gen.successor"),
                RestartScopeId.Create("restart.lifecycle"),
                occurrences),
        };
        var runtimeRequest = RuntimeRequestFor(
            plan,
            scenario == "cbi19-04-retained-generation-mismatch"
                ? GenerationId.Create("gen.retained")
                : GenerationId.Create("gen.lifecycle"));
        if (scenario == "cbi19-08-release-fails-before-cutover")
        {
            runtimeRequest = runtimeRequest with
            {
                Release = runtimeRequest.Release with
                {
                    FailureMoment = ReleaseFailureMoment.BeforeCutover,
                },
            };
        }

        var result = await ComponentGroupReplacement.ReplaceAsync(
            successorResolution,
            retained,
            [
                new(successorMembers[0], [new(new(occurrences[0], Participant), providerRequest)]),
                new(successorMembers[1], [new(new(occurrences[1], Supervisor), supervisorRequest)]),
            ],
            runtimeRequest,
            $"scoped replacement {scenario}");
        return (result, retained);
    }

    /// <summary>The activation being replaced: released, and expected to stay so until cutover.</summary>
    private static async Task<(ComponentGroupAuthorityResult Retained, CoolingPortableHandler[] Handlers)>
        ReplacementRetained(bool failCleanup)
    {
        var resolution = new FakeGenerationResolver().Resolve(PairRequest());
        var first = resolution.Generation!.ProviderSets.Single(item => item.Requirement == Requirement);
        var second = resolution.Generation.ProviderSets.Single(item => item.Requirement == SecondaryRequirement);
        var handlers = new[]
        {
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
        };
        IPortableProviderConversation Conversation(int index)
        {
            var conversation = new PortableDirectConversation(new PortableProviderEndpoint(
                CoolingPortableFixture.Contract, handlers[index], PortableRealization.FixedDirectCall));
            return failCleanup ? new FailingRetirementConversation(conversation) : conversation;
        }

        var groupMembers = new[]
        {
            new ComponentGroupMember(
                Selection(first.Members[0]) with { HostEndpoint = "retained-host-primary" },
                Conversation(0)),
            new ComponentGroupMember(
                Selection(second.Members[0]) with
                {
                    Requirement = SecondaryRequirement,
                    HostEndpoint = "retained-host-secondary",
                },
                Conversation(1)),
        };
        var policy = GroupPolicy(ProviderLocalActor);
        var retained = await ComponentGroupAuthority.ActivateAsync(
            resolution,
            [
                new(
                    groupMembers[0],
                    [new(new(groupMembers[0].Selection.Occurrence, Participant), ProviderAuthority(policy, Authority))]),
                new(
                    groupMembers[1],
                    [new(new(groupMembers[1].Selection.Occurrence, Supervisor), SupervisorAuthority(policy, AuditAuthority, revoked: false))]),
            ],
            RuntimeRequest(Plan(groupMembers.Select(item => item.Selection.Occurrence).ToArray())));
        return (retained, handlers);
    }

    /// <summary>
    /// CBI19 claims one entry per successor member and no position added or removed; it checked
    /// neither, so a caller could drop a position the successor generation still resolves.
    /// </summary>
    [Test]
    public async Task Cbi19_refuses_a_membership_the_successor_generation_does_not_resolve()
    {
        var (retained, _) = await ReplacementRetained(false);
        var successor = new FakeGenerationResolver().Resolve(PairRequest());
        var primary = successor.Generation!.ProviderSets.Single(item => item.Requirement == Requirement);
        var selection = Selection(primary.Members[0]) with { HostEndpoint = "partial-host-primary" };
        var result = await ComponentGroupReplacement.ReplaceAsync(
            successor,
            retained,
            [
                new(
                    new(selection, DirectCooling(CoolingPortableFixture.Contract)),
                    [new(new(selection.Occurrence, Participant), ProviderAuthority(GroupPolicy(ProviderLocalActor), Authority))]),
            ],
            RuntimeRequestFor(
                PlanFor(
                    GenerationId.Create("gen.successor"),
                    RestartScopeId.Create("restart.lifecycle"),
                    selection.Occurrence),
                GenerationId.Create("gen.lifecycle")),
            "partial membership");

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("position-not-supplied"));
            Assert.That(result.CutOver, Is.False);
            Assert.That(
                retained.Lifecycle!.Members.All(item => item.Member.IsReleased),
                Is.True,
                "A membership the generation does not resolve stands nothing down.");
        });
    }

    /// <summary>An added or dropped position is CBI20's operation, and CBI19 declines it by name.</summary>
    [Test]
    public async Task Cbi19_refuses_a_changed_membership()
    {
        var (retained, _) = await MembershipRetained(false);
        var successor = new FakeGenerationResolver().Resolve(RequestFor(Requirement, TertiaryRequirement));
        var (members, _) = MembershipMembers(successor, "cbi20-03-position-added-and-dropped");
        var result = await ComponentGroupReplacement.ReplaceAsync(
            successor,
            retained,
            members,
            MembershipRuntimeRequest(members, "cbi20-03-position-added-and-dropped"),
            "changed membership through CBI19");

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("membership-changed"));
            Assert.That(result.CutOver, Is.False);
            Assert.That(retained.Lifecycle!.Members.All(item => item.Member.IsReleased), Is.True);
        });
    }

    private static string ReplacementToken(ComponentGroupReplacementKind kind) => kind switch
    {
        ComponentGroupReplacementKind.Replaced => "replaced",
        ComponentGroupReplacementKind.CleanupFailed => "cleanup-failed",
        ComponentGroupReplacementKind.Declined => "declined",
        ComponentGroupReplacementKind.ActivationUnavailable => "activation-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    [Test]
    public async Task Shared_cbi20_vectors_replace_a_membership_across_one_cutover()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi20-membership-replacement-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, retained) = await MembershipResult(scenario);
            var retainedMembers = retained.Lifecycle!.Members;
            var successorReleased = result.Successor?.Lifecycle?.Members
                .Count(item => item.Member.IsReleased) ?? 0;

            Assert.Multiple(() =>
            {
                Assert.That(
                    MembershipToken(result.Kind),
                    Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                    scenario);
                Assert.That(
                    result.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                    scenario);
                Assert.That(
                    result.CutOver,
                    Is.EqualTo(vector.GetProperty("expectedCutover").GetBoolean()),
                    scenario);
                Assert.That(
                    successorReleased,
                    Is.EqualTo(vector.GetProperty("expectedSuccessorReleased").GetInt32()),
                    scenario);
                Assert.That(
                    retainedMembers.Count(item => item.Member.IsReleased),
                    Is.EqualTo(vector.GetProperty("expectedRetainedReleased").GetInt32()),
                    scenario);
                Assert.That(
                    retainedMembers.Count(item => item.Member.Stage == PortableCompositionStage.Retired),
                    Is.EqualTo(vector.GetProperty("expectedRetainedRetired").GetInt32()),
                    scenario);
                Assert.That(
                    result.Successor?.Admissions.Count ?? 0,
                    Is.EqualTo(vector.GetProperty("expectedAdmitted").GetInt32()),
                    scenario);
                Assert.That(
                    result.Added,
                    Has.Count.EqualTo(vector.GetProperty("expectedAdded").GetInt32()),
                    scenario);
                Assert.That(
                    result.Dropped,
                    Has.Count.EqualTo(vector.GetProperty("expectedDropped").GetInt32()),
                    scenario);

                // C2 over every vector: the three sets partition the two memberships.
                Assert.That(
                    result.Added.Intersect(result.Dropped),
                    Is.Empty,
                    $"{scenario}: nothing is both added and dropped.");
                Assert.That(
                    result.Dropped.Concat(result.Surviving).OrderBy(item => item.Value, StringComparer.Ordinal),
                    Is.EqualTo(retained.Admissions.Select(item => item.Occurrence)
                        .OrderBy(item => item.Value, StringComparer.Ordinal))
                        .Or.Empty,
                    $"{scenario}: dropped and surviving are the retained activation's membership.");

                // C4 and C7: an addition needs the cutover, and the boundary holds in both directions.
                Assert.That(
                    result.CutOver || successorReleased == 0,
                    Is.True,
                    $"{scenario}: no member is released without a cutover.");
                Assert.That(
                    result.Retired.Count > 0,
                    Is.EqualTo(result.CutOver),
                    $"{scenario}: retained members are retired exactly when the scope cut over.");
                Assert.That(
                    result.CutOver || retainedMembers.All(item => item.Member.IsReleased),
                    Is.True,
                    $"{scenario}: before cutover the retained activation is untouched.");
            });
        }
    }

    [Test]
    public async Task C1_the_membership_is_read_from_the_successor_generation()
    {
        var (absent, absentRetained) = await MembershipResult("cbi20-07-resolved-position-not-supplied");
        var (foreign, _) = await MembershipResult("cbi20-08-member-not-resolved");
        var unavailable = await ComponentGroupMembership.ReplaceAsync(
            new FakeGenerationResolver().Resolve(PairRequest()),
            new(
                Array.Empty<ComponentGroupMemberAdmission>(),
                Array.Empty<LocalCapabilityGrant>(),
                null,
                null),
            Array.Empty<ComponentGroupParticipant>(),
            RuntimeRequest(Plan()),
            "membership unavailable");

        Assert.Multiple(() =>
        {
            Assert.That(absent.Code, Is.EqualTo("position-not-supplied"));
            Assert.That(foreign.Code, Is.EqualTo("member-not-resolved"));
            Assert.That(
                unavailable.Kind,
                Is.EqualTo(ComponentGroupMembershipKind.ActivationUnavailable));
            Assert.That(
                new[] { absent, foreign, unavailable }.All(item =>
                    item.Successor is null &&
                    !item.CutOver &&
                    item.Added.Count == 0 &&
                    item.Dropped.Count == 0 &&
                    item.Surviving.Count == 0),
                Is.True,
                "A refusal of the membership itself computes no membership change.");
            Assert.That(
                absentRetained.Lifecycle!.Members.All(item => item.Member.IsReleased),
                Is.True);
        });
    }

    [Test]
    public async Task C2_the_added_and_dropped_sets_are_derived_from_the_generation()
    {
        var (both, retained) = await MembershipResult("cbi20-03-position-added-and-dropped");
        var successor = both.Successor!.Admissions.Select(item => item.Occurrence).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                both.Added.Concat(both.Surviving).OrderBy(item => item.Value, StringComparer.Ordinal),
                Is.EqualTo(successor.OrderBy(item => item.Value, StringComparer.Ordinal)),
                "Added and surviving are exactly the successor's membership.");
            Assert.That(
                both.Dropped.Concat(both.Surviving).OrderBy(item => item.Value, StringComparer.Ordinal),
                Is.EqualTo(retained.Admissions.Select(item => item.Occurrence)
                    .OrderBy(item => item.Value, StringComparer.Ordinal)),
                "Dropped and surviving are exactly the retained activation's.");
            Assert.That(both.Added.Single().Value, Does.Contain("tertiary"));
            Assert.That(both.Dropped.Single().Value, Does.Contain("secondary"));
        });
    }

    [Test]
    public async Task C3_a_dropped_positions_authority_is_not_re_established()
    {
        var (result, retained) = await MembershipResult("cbi20-02-position-dropped");
        var dropped = result.Dropped.Single();
        var priorGrants = retained.Admissions
            .Single(item => item.Occurrence == dropped)
            .Grants
            .Select(item => item.Request)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Successor!.Admissions.Any(item => item.Occurrence == dropped),
                Is.False,
                "The successor admits nothing against a dropped occurrence.");
            Assert.That(priorGrants, Is.Not.Empty, "The dropped occurrence did hold a grant.");
            Assert.That(
                result.Successor.Grants.Select(item => item.Request).Intersect(priorGrants),
                Is.Empty,
                "And no grant of its authority survives into the successor.");
        });
    }

    [Test]
    public async Task C4_an_added_position_joins_only_across_a_cutover()
    {
        var (refused, _) = await MembershipResult("cbi20-13-added-member-never-ready");
        var (denied, _) = await MembershipResult("cbi20-11-added-member-authority-denied");
        var (added, _) = await MembershipResult("cbi20-01-position-added");

        Assert.Multiple(() =>
        {
            Assert.That(
                refused.Successor!.Lifecycle!.Members.Count(item => item.Member.IsReleased),
                Is.Zero,
                "An added member that never reports Ready releases none of them.");
            Assert.That(refused.CutOver, Is.False);
            Assert.That(
                denied.Successor!.Lifecycle,
                Is.Null,
                "An added member whose authority is denied reaches no provider.");
            Assert.That(added.CutOver, Is.True);
            Assert.That(
                added.Successor!.Lifecycle!.Members.All(item => item.Member.IsReleased),
                Is.True,
                "The addition is released with the whole successor activation.");
            Assert.That(added.Successor.Lifecycle.Members, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public async Task C5_an_emptied_membership_is_a_withdrawal_not_a_replacement()
    {
        var (result, retained) = await MembershipResult("cbi20-09-successor-resolves-nothing");

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("membership-empty"));
            Assert.That(result.CutOver, Is.False);
            Assert.That(result.Retired, Is.Empty);
            Assert.That(
                retained.Lifecycle!.Members.All(item => item.Member.IsReleased),
                Is.True,
                "Standing the activation down is CBI14's operation, so this one stands nothing down.");
        });
    }

    [Test]
    public async Task C6_the_successor_stands_up_under_the_earlier_barriers()
    {
        var (changed, _) = await MembershipResult("cbi20-10-surviving-occurrence-authority-changed");
        var (conflated, retained) = await MembershipResult("cbi20-12-surviving-actor-reused-by-added-party");
        var (reused, _) = await MembershipResult("cbi20-05-dropped-actor-reused-by-added-party");

        Assert.Multiple(() =>
        {
            Assert.That(
                changed.Code,
                Is.EqualTo("authority-revalidation-mismatch"),
                "A surviving occurrence may not be re-admitted for different authority.");
            Assert.That(
                conflated.Code,
                Is.EqualTo("local-actor-shared-across-members"),
                "An addition may not take a surviving participant's receiving-domain Actor.");
            Assert.That(
                conflated.Successor!.Lifecycle,
                Is.Null,
                "And it contacts no successor provider.");
            Assert.That(
                reused.IsReplaced,
                Is.True,
                "But it may take the Actor a dropped participant held.");
            Assert.That(
                retained.Lifecycle!.Members.All(item => item.Member.IsReleased),
                Is.True);
        });
    }

    [Test]
    public async Task C7_cutover_is_the_boundary_and_the_retained_membership_goes_as_a_whole()
    {
        var (refused, serving) = await MembershipResult("cbi20-14-release-fails-before-cutover");
        var (replaced, retired) = await MembershipResult("cbi20-03-position-added-and-dropped");
        var survivor = serving.Lifecycle!.Members[1].Member;
        var attempted = await survivor.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableConstraint.Atom(PortableTruth.Satisfied));

        Assert.Multiple(() =>
        {
            Assert.That(refused.Code, Is.EqualTo("release-failed-before-cutover"));
            Assert.That(
                serving.Lifecycle.Members.All(item => item.Member.IsReleased),
                Is.True,
                "A pre-cutover failure leaves the dropped member serving too.");
            Assert.That(
                attempted.FrameDecision,
                Is.Not.EqualTo(PortableFrameDecision.None),
                "The member whose position the successor drops is still interacting.");
            Assert.That(
                retired.Lifecycle!.Members.All(item => item.Member.Stage == PortableCompositionStage.Retired),
                Is.True,
                "After cutover the whole retained membership goes, dropped and surviving alike.");
            Assert.That(replaced.Retired, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task C8_a_membership_replacement_produces_an_activation_the_other_slices_accept()
    {
        var (result, _) = await MembershipResult("cbi20-01-position-added");
        var successor = result.Successor!;
        var policy = GroupPolicy(ProviderLocalActor);
        var continued = await ComponentGroupRevalidation.RevalidateAsync(
            successor,
            [
                new(successor.Admissions[0].Occurrence, [ProviderAuthority(policy, Authority)]),
                new(successor.Admissions[1].Occurrence, [SupervisorAuthority(policy, AuditAuthority, revoked: false)]),
                new(successor.Admissions[2].Occurrence, [ObserverRequest(policy)]),
            ],
            "revalidate the replaced membership");

        Assert.Multiple(() =>
        {
            Assert.That(
                continued.Kind,
                Is.EqualTo(ComponentGroupRevalidationKind.Continued),
                "CBI14 accepts the activation a membership replacement produced.");
            Assert.That(
                continued.Members.Select(item => item.Occurrence),
                Is.EqualTo(successor.Admissions.Select(item => item.Occurrence)),
                "And it names exactly the successor's membership, including the addition.");
        });
    }

    [Test]
    public async Task C9_the_membership_replacer_adds_no_grant_and_widens_no_scope()
    {
        var (result, retained) = await MembershipResult("cbi20-03-position-added-and-dropped");
        var observation = result.Successor!.Lifecycle!.Runtime!.Observation;
        var admitted = result.Successor.Admissions.SelectMany(item => item.Grants).Count();

        Assert.Multiple(() =>
        {
            Assert.That(
                observation.RestartScope,
                Is.EqualTo(retained.Lifecycle!.Runtime!.Observation.RestartScope),
                "The successor occupies the scope the retained activation held; nothing widens.");
            Assert.That(
                observation.RetainedGeneration,
                Is.EqualTo(retained.Lifecycle.Runtime.Observation.TargetGeneration));
            Assert.That(
                result.Successor.Grants,
                Has.Count.EqualTo(admitted),
                "Every grant in force was admitted in this attempt and none besides.");
        });
    }

    [Test]
    public async Task C10_a_membership_replacement_migrates_no_state_and_moves_no_single_member()
    {
        var (result, retained) = await MembershipResult("cbi20-02-position-dropped");

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Successor!.Lifecycle!.Members,
                Has.Count.EqualTo(1),
                "The successor holds the positions its generation resolves, and no others.");
            Assert.That(
                retained.Lifecycle!.Members.Select(item => item.Member)
                    .Intersect(result.Successor.Lifecycle.Members.Select(item => item.Member))
                    .Any(),
                Is.False,
                "No portable member is carried across; the successor's are its own.");
            Assert.That(
                result.Retired,
                Has.Count.EqualTo(retained.Lifecycle.Members.Count),
                "The whole retained generation goes, never one member of it.");
        });
    }

    private static async Task<(
        ComponentGroupMembershipResult Result,
        ComponentGroupAuthorityResult Retained)>
        MembershipResult(string scenario)
    {
        var (retained, _) = await MembershipRetained(
            scenario == "cbi20-06-dropped-member-cleanup-fails-after-cutover");
        var successor = new FakeGenerationResolver().Resolve(RequestFor(MembershipPositions(scenario)));
        var (members, _) = MembershipMembers(successor, scenario);
        var result = await ComponentGroupMembership.ReplaceAsync(
            successor,
            retained,
            members,
            MembershipRuntimeRequest(members, scenario),
            $"membership replacement {scenario}");
        return (result, retained);
    }

    /// <summary>The positions the successor generation resolves, per scenario.</summary>
    private static RequirementId[] MembershipPositions(string scenario) => scenario switch
    {
        "cbi20-02-position-dropped" or
        "cbi20-06-dropped-member-cleanup-fails-after-cutover" or
        "cbi20-08-member-not-resolved" => [Requirement],
        "cbi20-03-position-added-and-dropped" or
        "cbi20-05-dropped-actor-reused-by-added-party" or
        "cbi20-10-surviving-occurrence-authority-changed" or
        "cbi20-14-release-fails-before-cutover" => [Requirement, TertiaryRequirement],
        "cbi20-04-membership-unchanged" => [Requirement, SecondaryRequirement],
        "cbi20-09-successor-resolves-nothing" => [],
        _ => [Requirement, SecondaryRequirement, TertiaryRequirement],
    };

    /// <summary>
    /// The members the caller supplies, which the fixture deliberately lets disagree with the
    /// generation in two scenarios.
    /// </summary>
    private static (ComponentGroupParticipant[] Members, CoolingPortableHandler[] Handlers)
        MembershipMembers(ResolutionOutcome successor, string scenario)
    {
        var supplied = scenario switch
        {
            "cbi20-07-resolved-position-not-supplied" => new[] { Requirement, SecondaryRequirement },
            "cbi20-08-member-not-resolved" => [Requirement, SecondaryRequirement],
            _ => MembershipPositions(scenario),
        };
        var policy = GroupPolicy(
            ProviderLocalActor,
            observerActor: scenario is "cbi20-05-dropped-actor-reused-by-added-party" or
                "cbi20-12-surviving-actor-reused-by-added-party"
                ? SupervisorLocalActor
                : null);
        var handlers = supplied
            .Select(_ => new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()))
            .ToArray();
        var members = new List<ComponentGroupParticipant>();
        for (var index = 0; index < supplied.Length; index++)
        {
            var requirement = supplied[index];
            var position = successor.Generation?.ProviderSets
                .SingleOrDefault(item => item.Requirement == requirement);

            // A member the generation does not resolve still has to be nameable, so it borrows the
            // occurrence the retained activation holds for that position.
            var occurrence = position is null
                ? OccurrenceId.Create($"occ.{PositionCatalog.Single(item => item.Requirement == requirement).Provider.Value}.1")
                : position.Members[0].Occurrence;
            var definition = position is null
                ? PositionCatalog.Single(item => item.Requirement == requirement).Provider
                : position.Members[0].Definition;
            var selection = new ComponentBindingSelection(
                requirement,
                definition,
                occurrence,
                CoolingPortableFixture.Component,
                CoolingPortableFixture.Provider,
                $"membership-host-{index}",
                "cooling-provider",
                CoolingPortableFixture.Contract);

            // A provider the required contract does not match never reports Ready.
            var document = scenario == "cbi20-13-added-member-never-ready" &&
                requirement == TertiaryRequirement
                ? CoolingPortableFixture.Contract with
                {
                    Provider = PortableProviderReference.Parse("brontide.fake.substituted", 1),
                }
                : CoolingPortableFixture.Contract;
            members.Add(new(
                new(
                    selection,
                    new PortableDirectConversation(new PortableProviderEndpoint(
                        document,
                        handlers[index],
                        PortableRealization.FixedDirectCall))),
                [MembershipParticipant(requirement, policy, occurrence, scenario)]));
        }

        return (members.ToArray(), handlers);
    }

    private static ComponentParticipantRequest MembershipParticipant(
        RequirementId requirement,
        LocalAuthorityPolicy policy,
        OccurrenceId occurrence,
        string scenario)
    {
        if (requirement == SecondaryRequirement)
        {
            return new(new(occurrence, Supervisor), SupervisorAuthority(policy, AuditAuthority, revoked: false));
        }

        if (requirement == TertiaryRequirement)
        {
            var observer = ObserverRequest(policy);
            return new(
                new(occurrence, Observer),
                scenario == "cbi20-11-added-member-authority-denied" ? Revoked(observer) : observer);
        }

        var provider = ProviderAuthority(policy, Authority);
        return new(
            new(occurrence, Participant),
            scenario == "cbi20-10-surviving-occurrence-authority-changed"
                ? provider with
                {
                    Authority = [provider.Authority.Single() with { Capability = CapabilityId.Create("capability.other") }],
                }
                : provider);
    }

    private static ActivationRuntimeRequest MembershipRuntimeRequest(
        IReadOnlyList<ComponentGroupParticipant> members,
        string scenario)
    {
        var request = RuntimeRequestFor(
            PlanFor(
                GenerationId.Create("gen.successor"),
                RestartScopeId.Create("restart.lifecycle"),
                members.Select(item => item.Member.Selection.Occurrence).ToArray()),
            GenerationId.Create("gen.lifecycle"));
        return scenario == "cbi20-14-release-fails-before-cutover"
            ? request with
            {
                Release = request.Release with { FailureMoment = ReleaseFailureMoment.BeforeCutover },
            }
            : request;
    }

    /// <summary>
    /// The activation being replaced: two released members, one of which every drop scenario drops.
    /// </summary>
    private static async Task<(ComponentGroupAuthorityResult Retained, CoolingPortableHandler[] Handlers)>
        MembershipRetained(bool failDroppedCleanup)
    {
        var resolution = new FakeGenerationResolver().Resolve(PairRequest());
        var first = resolution.Generation!.ProviderSets.Single(item => item.Requirement == Requirement);
        var second = resolution.Generation.ProviderSets.Single(item => item.Requirement == SecondaryRequirement);
        var handlers = new[]
        {
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
        };
        IPortableProviderConversation Conversation(int index)
        {
            var conversation = new PortableDirectConversation(new PortableProviderEndpoint(
                CoolingPortableFixture.Contract, handlers[index], PortableRealization.FixedDirectCall));

            // Only the member the successor drops refuses withdrawal, so the failure names it.
            return failDroppedCleanup && index == 1
                ? new FailingRetirementConversation(conversation)
                : conversation;
        }

        var groupMembers = new[]
        {
            new ComponentGroupMember(
                Selection(first.Members[0]) with { HostEndpoint = "retained-host-primary" },
                Conversation(0)),
            new ComponentGroupMember(
                Selection(second.Members[0]) with
                {
                    Requirement = SecondaryRequirement,
                    HostEndpoint = "retained-host-secondary",
                },
                Conversation(1)),
        };
        var policy = GroupPolicy(ProviderLocalActor);
        var retained = await ComponentGroupAuthority.ActivateAsync(
            resolution,
            [
                new(
                    groupMembers[0],
                    [new(new(groupMembers[0].Selection.Occurrence, Participant), ProviderAuthority(policy, Authority))]),
                new(
                    groupMembers[1],
                    [new(new(groupMembers[1].Selection.Occurrence, Supervisor), SupervisorAuthority(policy, AuditAuthority, revoked: false))]),
            ],
            RuntimeRequest(Plan(groupMembers.Select(item => item.Selection.Occurrence).ToArray())));
        return (retained, handlers);
    }

    private static string MembershipToken(ComponentGroupMembershipKind kind) => kind switch
    {
        ComponentGroupMembershipKind.Replaced => "replaced",
        ComponentGroupMembershipKind.CleanupFailed => "cleanup-failed",
        ComponentGroupMembershipKind.Declined => "declined",
        ComponentGroupMembershipKind.ActivationUnavailable => "activation-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    [Test]
    public async Task Shared_cbi18_vectors_grow_every_member_set_or_none()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi18-group-extension-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, active, _) = await GroupExtensionResult(scenario);
            var released = active.Lifecycle!.Members.Count(item => item.Member.IsReleased);

            Assert.Multiple(() =>
            {
                Assert.That(
                    GroupExtensionToken(result.Kind),
                    Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                    scenario);
                Assert.That(
                    result.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                    scenario);
                Assert.That(
                    result.CurrentAuthority,
                    Has.Count.EqualTo(vector.GetProperty("expectedEvaluated").GetInt32()),
                    scenario);
                Assert.That(
                    result.Grown,
                    Has.Count.EqualTo(vector.GetProperty("expectedGrownMembers").GetInt32()),
                    scenario);
                Assert.That(
                    result.InForce?.Admissions.Sum(item => item.Participants.Count) ?? 0,
                    Is.EqualTo(vector.GetProperty("expectedInForceParticipants").GetInt32()),
                    scenario);
                Assert.That(
                    result.Lapsed,
                    Has.Count.EqualTo(vector.GetProperty("expectedLapsed").GetInt32()),
                    scenario);
                Assert.That(
                    released,
                    Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                    scenario);

                // Only a lapse retires, and then the whole activation.
                Assert.That(
                    released == active.Lifecycle.Members.Count || released == 0,
                    Is.True,
                    scenario);
                Assert.That(
                    result.InForce is null,
                    Is.EqualTo(result.Kind is ComponentGroupExtensionKind.Withdrawn
                        or ComponentGroupExtensionKind.RetirementFailed),
                    scenario);
                Assert.That(
                    result.Grown.Count > 0,
                    Is.EqualTo(result.IsExtended),
                    $"{scenario}: an applied extension grows at least one member, and a refused one grows none.");
            });
        }
    }

    [Test]
    public async Task C1_extension_needs_a_released_activation_and_the_members_it_admitted()
    {
        var unavailable = await ComponentGroupExtension.ExtendAsync(
            new(
                Array.Empty<ComponentGroupMemberAdmission>(),
                Array.Empty<LocalCapabilityGrant>(),
                null,
                null),
            Array.Empty<ComponentGroupMemberRequests>(),
            "extension unavailable");
        var (wrongMembers, active, _) = await GroupExtensionResult("cbi18-08-member-set-changed");

        Assert.Multiple(() =>
        {
            Assert.That(unavailable.Kind, Is.EqualTo(ComponentGroupExtensionKind.ActivationUnavailable));
            Assert.That(unavailable.CurrentAuthority, Is.Empty);
            Assert.That(unavailable.InForce, Is.Null);
            Assert.That(wrongMembers.Kind, Is.EqualTo(ComponentGroupExtensionKind.Declined));
            Assert.That(wrongMembers.CurrentAuthority, Is.Empty, "A member set the activation did not admit evaluates nothing.");
            Assert.That(active.Lifecycle!.Members.All(item => item.Member.IsReleased), Is.True);
        });
    }

    [Test]
    public async Task C2_every_member_retains_everyone_and_the_activation_gains_someone()
    {
        var (removal, removalActive, _) = await GroupExtensionResult("cbi18-05-removal-declined");
        var (substitution, _, _) = await GroupExtensionResult("cbi18-06-substitution-declined");
        var (unchanged, _, _) = await GroupExtensionResult("cbi18-07-activation-unchanged");

        Assert.Multiple(() =>
        {
            Assert.That(removal.Code, Is.EqualTo("participant-not-retained"));
            Assert.That(substitution.Code, Is.EqualTo("participant-not-retained"), "A substitute is a removal plus an addition, and the removal decides it.");
            Assert.That(unchanged.Code, Is.EqualTo("activation-unchanged"));
            Assert.That(
                new[] { removal, substitution, unchanged }.All(item =>
                    item.CurrentAuthority.Count == 0 && item.Grown.Count == 0),
                Is.True,
                "None of the three evaluates anything or grows anyone.");
            Assert.That(removalActive.Lifecycle!.Members.All(item => item.Member.IsReleased), Is.True);
        });
    }

    [Test]
    public async Task C3_no_declaration_is_consulted_for_any_member()
    {
        var parameters = typeof(ComponentGroupExtension)
            .GetMethod(nameof(ComponentGroupExtension.ExtendAsync))!
            .GetParameters()
            .Select(item => item.ParameterType)
            .ToArray();
        var (result, _, prior) = await GroupExtensionResult("cbi18-01-one-member-grown");

        // Coverage is monotone in the grants held, which is why growth needs no declaration.
        var declared = Dependency(Consumer).Entries
            .Select(entry => $"{entry.Capability.Value}|{entry.Target.Value}|{entry.Operation.Value}|{entry.Scope.Value}")
            .ToArray();
        string[] Tuples(IReadOnlyList<LocalCapabilityGrant> grants) => grants
            .Select(grant => $"{grant.Capability.Value}|{grant.Target.Value}|{grant.Operation.Value}|{grant.Scope.Value}")
            .ToArray();
        var before = Tuples(prior.SelectMany(item => item.Grants).ToArray());
        var after = Tuples(result.InForce!.Grants);

        Assert.Multiple(() =>
        {
            Assert.That(
                parameters,
                Has.None.EqualTo(typeof(ResolutionOutcome)).And.None.EqualTo(typeof(ComponentGrantDependency)),
                "The absent parameter is the contract: growth reads no resolution and no declaration.");
            Assert.That(
                declared.Where(before.Contains).All(after.Contains),
                Is.True,
                "Every tuple covered before the extension is still covered after it.");
            Assert.That(before.All(after.Contains), Is.True, "Growth withdraws no grant at all.");
        });
    }

    [Test]
    public async Task C4_a_declined_extension_changes_nothing_anywhere()
    {
        var (result, active, prior) = await GroupExtensionResult("cbi18-11-addition-denied");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ComponentGroupExtensionKind.Declined));
            Assert.That(result.InForce, Is.Not.Null);
            Assert.That(
                result.InForce!.Admissions.Sum(item => item.Participants.Count),
                Is.EqualTo(prior.Sum(item => item.Participants.Count)),
                "The in-force activation is the one it was given, not the one that was intended.");
            Assert.That(result.Grown, Is.Empty);
            Assert.That(active.Lifecycle!.Members.All(item => item.Member.IsReleased), Is.True);
        });
    }

    [Test]
    public async Task C5_a_malformed_request_decides_nothing_and_evaluated_loss_retires_everything()
    {
        var (drift, driftActive, _) = await GroupExtensionResult("cbi18-12-retained-identity-drift");
        var (lapse, lapseActive, _) = await GroupExtensionResult("cbi18-13-untouched-member-lapsed");

        Assert.Multiple(() =>
        {
            Assert.That(drift.Kind, Is.EqualTo(ComponentGroupExtensionKind.Declined));
            Assert.That(drift.CurrentAuthority, Is.Empty, "Nothing was evaluated, so nothing was learned.");
            Assert.That(driftActive.Lifecycle!.Members.All(item => item.Member.IsReleased), Is.True);

            Assert.That(lapse.Kind, Is.EqualTo(ComponentGroupExtensionKind.Withdrawn));
            Assert.That(lapse.CurrentAuthority, Is.Not.Empty, "No result both retires and reports zero evaluations.");
            Assert.That(
                lapseActive.Lifecycle!.Members.All(item => item.Member.Stage == PortableCompositionStage.Retired),
                Is.True,
                "The lapse was in the member that was not growing, and the whole activation retires.");
        });
    }

    [Test]
    public async Task C6_retained_authority_is_revalidated_before_it_is_extended()
    {
        var (result, active, _) = await GroupExtensionResult("cbi18-15-lapse-outranks-a-denied-addition");

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Kind,
                Is.EqualTo(ComponentGroupExtensionKind.Withdrawn),
                "A lapse outranks any problem with an addition, so a call that would both retire and decline retires.");
            Assert.That(result.Code, Is.EqualTo("authority-not-renewed"));
            Assert.That(result.InForce, Is.Null, "No set is extended on top of authority that has itself lapsed.");
            Assert.That(active.Lifecycle!.Members.Count(item => item.Member.IsReleased), Is.Zero);
        });
    }

    [Test]
    public async Task C7_an_added_participant_is_admitted_on_cbi13_terms()
    {
        var (result, active, _) = await GroupExtensionResult("cbi18-11-addition-denied");

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("authority-not-admitted"));
            Assert.That(
                result.CurrentAuthority,
                Has.Count.EqualTo(3),
                "The addition was evaluated, and refused on the evaluator's own terms.");
            Assert.That(
                active.Lifecycle!.Members.All(item => item.Member.IsReleased),
                Is.True,
                "A refused addition declines the extension rather than retiring the activation.");
        });
    }

    [Test]
    public async Task C8_the_extended_activation_obeys_the_activation_wide_rules()
    {
        var (shared, _, _) = await GroupExtensionResult("cbi18-03-shared-party-added-to-second-member");
        var (secondActor, _, _) = await GroupExtensionResult("cbi18-04-shared-party-mapped-onto-a-second-actor");
        var (sharedActor, _, _) = await GroupExtensionResult("cbi18-10-local-actor-shared-across-members");
        var (identity, _, _) = await GroupExtensionResult("cbi18-09-identity-shared-across-members");

        Assert.Multiple(() =>
        {
            Assert.That(
                shared.IsExtended,
                Is.True,
                "A party already participating in another member may be added to a second, under the local Actor it already holds.");
            Assert.That(
                shared.InForce!.Admissions
                    .SelectMany(item => item.Participants)
                    .Where(item => item.Participant == Participant)
                    .Select(item => item.Authority.Observation.Relationships[0].LocalActor)
                    .Distinct()
                    .Count(),
                Is.EqualTo(1),
                "It arrives at exactly one receiving-domain Actor across the activation.");
            Assert.That(secondActor.Code, Is.EqualTo("participant-actor-not-single"));
            Assert.That(sharedActor.Code, Is.EqualTo("local-actor-shared-across-members"));
            Assert.That(identity.Code, Is.EqualTo("authority-identity-not-distinct"));
        });
    }

    [Test]
    public async Task C9_an_extension_produces_an_activation_the_other_slices_accept()
    {
        var (_, active, admitted, policy, _) = await ExtensionActivation(failCleanup: false);
        var intended = new ComponentGroupMemberRequests[]
        {
            new(active.Admissions[0].Occurrence, [admitted[0], ObserverRequest(policy)]),
            new(active.Admissions[1].Occurrence, [admitted[1], DeputyRequest(policy)]),
        };
        var result = await ComponentGroupExtension.ExtendAsync(active, intended, "extend before revalidating");

        // CBI14 revalidates the extended activation from the same requests that produced it.
        var continued = await ComponentGroupRevalidation.RevalidateAsync(
            result.InForce!,
            intended,
            "revalidate the extended activation");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsExtended, Is.True);
            Assert.That(
                continued.Kind,
                Is.EqualTo(ComponentGroupRevalidationKind.Continued),
                "CBI14 accepts the activation an extension produced.");
            Assert.That(continued.Members.Sum(item => item.CurrentAuthority.Count), Is.EqualTo(4));
        });
    }

    [Test]
    public async Task C10_an_extension_exercises_nothing_and_notifies_no_provider()
    {
        var (resolution, active, admitted, policy, handlers) = await ExtensionActivation(failCleanup: false);
        var before = handlers.Sum(handler => handler.ProviderEffectCount);
        var result = await ComponentGroupExtension.ExtendAsync(
            active,
            [
                new(active.Admissions[0].Occurrence, [admitted[0], ObserverRequest(policy)]),
                new(active.Admissions[1].Occurrence, [admitted[1]]),
            ],
            "extension reaches no provider");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsExtended, Is.True);
            Assert.That(
                handlers.Sum(handler => handler.ProviderEffectCount),
                Is.EqualTo(before),
                "CBI18 exercises no granted Operation.");
            Assert.That(
                active.Lifecycle!.Members.All(item => item.Member.Stage == PortableCompositionStage.Released),
                Is.True,
                "It tells no provider the set changed, so no member's portable stage moves.");
            Assert.That(resolution.Generation, Is.Not.Null);
        });
    }

    private static async Task<(
        ComponentGroupExtensionResult Result,
        ComponentGroupAuthorityResult Active,
        ComponentGroupMemberAdmission[] Prior)>
        GroupExtensionResult(string scenario)
    {
        var failCleanup = scenario == "cbi18-14-retirement-failure";
        var (_, active, admitted, policy, _) = await ExtensionActivation(failCleanup);
        var prior = active.Admissions.ToArray();
        var first = prior[0].Occurrence;
        var second = prior[1].Occurrence;

        // A second request for a party already live in the first member: distinct identities
        // throughout, and the Actor its own policy establishes.
        AuthorityAdmissionRequest SharedParty(LocalAuthorityPolicy actorPolicy)
        {
            var relationship = RelationshipRequestId.Create("relationship.group-provider-second");
            return ProviderAuthority(actorPolicy, Authority) with
            {
                Request = AdmissionRequestId.Create("admission.group-provider-second"),
                Relationships =
                [
                    new(relationship, Participant, ActorRelationshipKind.ComponentParticipant, [AuthorityEvidence]),
                ],
                Authority =
                [
                    new(
                        AuthorityRequestId.Create("authority.cooling-control-second"),
                        relationship,
                        Capability,
                        Target,
                        Operation,
                        AuthorityScope,
                        false),
                ],
            };
        }

        var observer = ObserverRequest(policy);
        var firstRequests = new List<AuthorityAdmissionRequest> { admitted[0], observer };
        var secondRequests = new List<AuthorityAdmissionRequest> { admitted[1] };
        switch (scenario)
        {
            case "cbi18-02-both-members-grown":
                secondRequests.Add(DeputyRequest(policy));
                break;
            case "cbi18-03-shared-party-added-to-second-member":
                firstRequests.RemoveAt(1);
                secondRequests.Add(SharedParty(policy));
                break;
            case "cbi18-04-shared-party-mapped-onto-a-second-actor":
                firstRequests.RemoveAt(1);
                secondRequests.Add(SharedParty(GroupPolicy(DeputyLocalActor)));
                break;
            case "cbi18-05-removal-declined":
                firstRequests.Clear();
                break;
            case "cbi18-06-substitution-declined":
                firstRequests.RemoveAt(0);
                break;
            case "cbi18-07-activation-unchanged":
                firstRequests.RemoveAt(1);
                break;
            case "cbi18-09-identity-shared-across-members":
                firstRequests[1] = observer with
                {
                    Authority = [observer.Authority.Single() with { Request = AuditAuthority }],
                };
                break;
            case "cbi18-10-local-actor-shared-across-members":
                firstRequests[1] = ObserverRequest(
                    GroupPolicy(ProviderLocalActor, observerActor: SupervisorLocalActor));
                break;
            case "cbi18-11-addition-denied":
            case "cbi18-15-lapse-outranks-a-denied-addition":
                firstRequests[1] = Revoked(observer);
                break;
            case "cbi18-12-retained-identity-drift":
                firstRequests[0] = admitted[0] with
                {
                    Authority =
                    [
                        admitted[0].Authority.Single() with { Capability = CapabilityId.Create("capability.other") },
                    ],
                };
                break;
            default:
                break;
        }

        if (scenario is "cbi18-13-untouched-member-lapsed" or "cbi18-14-retirement-failure"
            or "cbi18-15-lapse-outranks-a-denied-addition")
        {
            secondRequests[0] = Revoked(admitted[1]);
        }

        var requests = new List<ComponentGroupMemberRequests>
        {
            new(first, firstRequests),
            new(second, secondRequests),
        };
        if (scenario == "cbi18-08-member-set-changed")
        {
            requests.RemoveAt(1);
        }

        var result = await ComponentGroupExtension.ExtendAsync(
            active,
            requests,
            $"group extension {scenario}");
        return (result, active, prior);
    }

    /// <summary>Two released members holding one participant each, so growth is observable.</summary>
    private static async Task<(
        ResolutionOutcome Resolution,
        ComponentGroupAuthorityResult Active,
        AuthorityAdmissionRequest[] Admitted,
        LocalAuthorityPolicy Policy,
        CoolingPortableHandler[] Handlers)>
        ExtensionActivation(bool failCleanup)
    {
        var resolution = new FakeGenerationResolver().Resolve(PairRequest());
        var first = resolution.Generation!.ProviderSets.Single(item => item.Requirement == Requirement);
        var second = resolution.Generation.ProviderSets.Single(item => item.Requirement == SecondaryRequirement);
        var handlers = new[]
        {
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
        };
        IPortableProviderConversation SecondConversation()
        {
            var conversation = new PortableDirectConversation(new PortableProviderEndpoint(
                CoolingPortableFixture.Contract, handlers[1], PortableRealization.FixedDirectCall));
            return failCleanup ? new FailingRetirementConversation(conversation) : conversation;
        }

        var groupMembers = new[]
        {
            new ComponentGroupMember(
                Selection(first.Members[0]) with { HostEndpoint = "extension-host-primary" },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    CoolingPortableFixture.Contract, handlers[0], PortableRealization.FixedDirectCall))),
            new ComponentGroupMember(
                Selection(second.Members[0]) with
                {
                    Requirement = SecondaryRequirement,
                    HostEndpoint = "extension-host-secondary",
                },
                SecondConversation()),
        };
        var policy = GroupPolicy(ProviderLocalActor);
        var admitted = new[]
        {
            ProviderAuthority(policy, Authority),
            SupervisorAuthority(policy, AuditAuthority, revoked: false),
        };
        var active = await ComponentGroupAuthority.ActivateAsync(
            resolution,
            [
                new(groupMembers[0], [new(new(groupMembers[0].Selection.Occurrence, Participant), admitted[0])]),
                new(groupMembers[1], [new(new(groupMembers[1].Selection.Occurrence, Supervisor), admitted[1])]),
            ],
            RuntimeRequest(Plan(groupMembers.Select(item => item.Selection.Occurrence).ToArray())));
        return (resolution, active, admitted, policy, handlers);
    }

    private static string GroupExtensionToken(ComponentGroupExtensionKind kind) => kind switch
    {
        ComponentGroupExtensionKind.Extended => "extended",
        ComponentGroupExtensionKind.Declined => "declined",
        ComponentGroupExtensionKind.Withdrawn => "withdrawn",
        ComponentGroupExtensionKind.RetirementFailed => "retirement-failed",
        ComponentGroupExtensionKind.ActivationUnavailable => "activation-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string GroupSuccessionToken(ComponentGroupSuccessionKind kind) => kind switch
    {
        ComponentGroupSuccessionKind.Narrowed => "narrowed",
        ComponentGroupSuccessionKind.Declined => "declined",
        ComponentGroupSuccessionKind.ActivationUnavailable => "activation-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string GroupVerificationToken(ComponentGroupVerificationKind kind) => kind switch
    {
        ComponentGroupVerificationKind.Consistent => "consistent",
        ComponentGroupVerificationKind.UndeclaredUse => "undeclared-use",
        ComponentGroupVerificationKind.UngrantedUse => "ungranted-use",
        ComponentGroupVerificationKind.RetirementFailed => "retirement-failed",
        ComponentGroupVerificationKind.Declined => "declined",
        ComponentGroupVerificationKind.ActivationUnavailable => "activation-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string GroupRevisionToken(ComponentGroupRevisionKind kind) => kind switch
    {
        ComponentGroupRevisionKind.Revised => "revised",
        ComponentGroupRevisionKind.Declined => "declined",
        ComponentGroupRevisionKind.Withdrawn => "withdrawn",
        ComponentGroupRevisionKind.RetirementFailed => "retirement-failed",
        ComponentGroupRevisionKind.ActivationUnavailable => "activation-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string GroupRevalidationToken(ComponentGroupRevalidationKind kind) => kind switch
    {
        ComponentGroupRevalidationKind.Continued => "continued",
        ComponentGroupRevalidationKind.Withdrawn => "withdrawn",
        ComponentGroupRevalidationKind.RetirementFailed => "retirement-failed",
        ComponentGroupRevalidationKind.ActivationUnavailable => "activation-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string GroupAuthorityToken(ComponentGroupAuthorityFailureKind kind) => kind switch
    {
        ComponentGroupAuthorityFailureKind.IdentityNotDistinct => "identity-not-distinct",
        ComponentGroupAuthorityFailureKind.MemberAuthorityRefused => "member-authority-refused",
        ComponentGroupAuthorityFailureKind.ActorMappingInconsistent => "actor-mapping-inconsistent",
        ComponentGroupAuthorityFailureKind.ActivationRefused => "activation-refused",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    [Test]
    public async Task Shared_cbi21_vectors_activate_a_strongly_connected_group_without_a_protocol()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi21-strongly-connected-group-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, plan, handlers) = await StronglyConnectedResult(scenario);
            var expectedCode = vector.GetProperty("expectedCode");

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.IsActive,
                    Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()),
                    scenario);
                Assert.That(
                    result.Failure?.Code,
                    Is.EqualTo(expectedCode.ValueKind == JsonValueKind.Null ? null : expectedCode.GetString()),
                    scenario);
                Assert.That(
                    plan.Groups,
                    Has.Count.EqualTo(vector.GetProperty("expectedGroups").GetInt32()),
                    scenario);
                Assert.That(
                    plan.Groups.Sum(group => group.Members.Count),
                    Is.EqualTo(vector.GetProperty("expectedMembers").GetInt32()),
                    $"{scenario}: the plan carries the members the vector names.");
                Assert.That(
                    result.Members,
                    Has.Count.EqualTo(vector.GetProperty("expectedPrepared").GetInt32()),
                    scenario);
                Assert.That(
                    result.Members.Count(item => item.Member.IsReleased),
                    Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                    scenario);

                // Grouping changes which members CM4 expects observations for; it changes no barrier.
                Assert.That(
                    result.Members.All(item => item.Member.IsReleased) ||
                        result.Members.All(item => !item.Member.IsReleased),
                    Is.True,
                    $"{scenario}: the release barrier is the activation's, whatever the grouping.");
                Assert.That(
                    handlers.Sum(handler => handler.ProviderEffectCount),
                    Is.Zero,
                    $"{scenario}: activation exercises nothing of its own.");
            });
        }
    }

    [Test]
    public async Task C1_a_group_is_refused_for_its_protocols_and_not_for_its_members()
    {
        var (cycle, _, _) = await StronglyConnectedResult("cbi21-01-ordinary-cycle-activated");
        var (mixed, mixedPlan, _) = await StronglyConnectedResult("cbi21-02-mixed-grouping-activated");
        var (unplanned, _, _) = await StronglyConnectedResult("cbi21-04-member-not-planned");
        var (unselected, _, _) = await StronglyConnectedResult("cbi21-05-member-not-selected");
        var (repeated, _, _) = await StronglyConnectedResult("cbi21-06-member-not-distinct");

        Assert.Multiple(() =>
        {
            Assert.That(
                cycle.IsActive,
                Is.True,
                "A cyclic group that declares no protocol needs nothing this seam lacks.");
            Assert.That(mixed.IsActive, Is.True);
            Assert.That(
                mixedPlan.Groups.Select(group => group.Members.Count).OrderBy(count => count),
                Is.EqualTo(new[] { 1, 2 }),
                "One plan carrying a singleton group and a cyclic pair activates as one activation.");

            Assert.That(unplanned.Failure!.Code, Is.EqualTo("member-not-planned"));
            Assert.That(unselected.Failure!.Code, Is.EqualTo("member-not-selected"));
            Assert.That(repeated.Failure!.Code, Is.EqualTo("member-not-distinct"));
            Assert.That(
                new[] { unplanned, unselected, repeated }.All(item =>
                    item.Members.Count == 0 && item.Runtime is null),
                Is.True,
                "Every plan refusal happens before a member is prepared.");
        });
    }

    [Test]
    public async Task C2_a_declared_bounded_protocol_is_refused_by_name()
    {
        var (refused, plan, handlers) = await StronglyConnectedResult("cbi21-03-protocol-group-refused");

        Assert.Multiple(() =>
        {
            Assert.That(refused.Failure!.Kind, Is.EqualTo(ComponentGroupActivationFailureKind.PlanUnsupported));
            Assert.That(refused.Failure.Code, Is.EqualTo("relational-initialisation-unsupported"));
            Assert.That(
                refused.Failure.Reason,
                Does.Contain("Relational Initialisation"),
                "The refusal names the stage rather than the group's shape.");
            Assert.That(
                plan.Groups.Single().Protocols,
                Has.Count.EqualTo(2),
                "The plan really does declare bounded protocols.");
            Assert.That(refused.Members, Is.Empty);
            Assert.That(handlers.Sum(handler => handler.ProviderEffectCount), Is.Zero);
        });
    }

    [Test]
    public async Task C3_the_refusal_is_the_seams_and_cm3_and_cm4_both_accept_the_plan()
    {
        var (refused, plan, _) = await StronglyConnectedResult("cbi21-03-protocol-group-refused");
        var runtime = new FakeActivationRuntime().Activate(RuntimeRequest(plan) with
        {
            StageOutcomes = plan.Groups
                .SelectMany(group => group.Members.SelectMany(member => group.Stages.Select(stage =>
                    new MemberStageOutcome(group.Group, member.Occurrence, stage.Stage, true, "supplied"))))
                .ToArray(),
            InteractionAttempts = plan.Groups
                .SelectMany(group => group.Protocols.Select((protocol, index) => new RuntimeInteractionAttempt(
                    RuntimeInteractionId.Create($"interaction.{index}"),
                    group.Group,
                    protocol.From,
                    protocol.To,
                    RuntimeInteractionPhase.RelationalInitialisation,
                    RuntimeInteractionKind.Lifecycle,
                    protocol.Edge,
                    protocol.Operation,
                    protocol.Authority[0],
                    protocol.InputShape)))
                .ToArray(),
        });

        Assert.Multiple(() =>
        {
            Assert.That(refused.IsActive, Is.False, "The integration refuses it.");
            Assert.That(
                plan.Groups.Single().Stages.Select(stage => stage.Stage),
                Does.Contain(ActivationStage.RelationalInitialisation),
                "CM3 planned the stage.");
            Assert.That(
                runtime.Kind,
                Is.EqualTo(ActivationRuntimeOutcomeKind.Active),
                "And CM4 accepts the plan and its declared handshakes, so neither of them is the refusal.");
        });
    }

    [Test]
    public async Task C4_the_seam_leaves_no_window_for_a_relational_stage()
    {
        var resolution = new FakeGenerationResolver().Resolve(RequestFor(Requirement));
        var member = resolution.Generation!.ProviderSets.Single().Members.Single();
        var prepared = ComponentBindingIntegration.Prepare(resolution, Selection(member)).Member!;

        var readyBefore = prepared.IsReady;
        await prepared.InterconnectAsync(DirectCooling(CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(readyBefore, Is.False);
            Assert.That(
                prepared.IsReady,
                Is.True,
                "Interconnection carries establishment and the readiness signal together.");
            Assert.That(
                prepared.Stage,
                Is.EqualTo(PortableCompositionStage.Interconnected),
                "So a member is Ready before anything else the seam offers can be called, and CM4 requires Relational Initialisation to precede Ready.");
        });
    }

    [Test]
    public async Task C5_the_seam_has_no_lifecycle_traffic_verb()
    {
        var resolution = new FakeGenerationResolver().Resolve(RequestFor(Requirement));
        var member = resolution.Generation!.ProviderSets.Single().Members.Single();
        var prepared = ComponentBindingIntegration.Prepare(resolution, Selection(member)).Member!;
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        await prepared.InterconnectAsync(new PortableDirectConversation(
            new PortableProviderEndpoint(CoolingPortableFixture.Contract, handler, PortableRealization.FixedDirectCall)));

        var attempted = await prepared.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableConstraint.Atom(PortableTruth.Satisfied));

        Assert.Multiple(() =>
        {
            Assert.That(prepared.IsReady, Is.True);
            Assert.That(
                attempted.Category,
                Is.EqualTo(PortableProtocolCategory.StateViolation),
                "The one verb a composition can initiate is gated on Release, and the refusal is the portable layer's own.");
            Assert.That(
                attempted.FrameDecision,
                Is.EqualTo(PortableFrameDecision.None),
                "So a declared handshake could not reach a provider even if one were named.");
            Assert.That(handler.ProviderEffectCount, Is.Zero);
        });
    }

    [Test]
    public async Task C6_a_delivered_group_activates_on_cbi12_terms()
    {
        var (cycle, plan, handlers) = await StronglyConnectedResult("cbi21-01-ordinary-cycle-activated");

        Assert.Multiple(() =>
        {
            Assert.That(cycle.IsActive, Is.True);
            Assert.That(plan.Groups.Single().Cyclic, Is.True, "One group, and CM3 calls it cyclic.");
            Assert.That(
                plan.Groups.Single().Stages.Select(stage => stage.Stage),
                Does.Not.Contain(ActivationStage.RelationalInitialisation),
                "And it declares no relational stage, which is why it is deliverable.");
            Assert.That(cycle.Members.All(item => item.Member.IsReleased), Is.True);
            Assert.That(cycle.Runtime!.IsActive, Is.True);
            Assert.That(handlers.Sum(handler => handler.ProviderEffectCount), Is.Zero);
        });
    }

    [Test]
    public async Task C7_a_delivered_group_performs_none_of_its_internal_edges()
    {
        var (cycle, plan, handlers) = await StronglyConnectedResult("cbi21-01-ordinary-cycle-activated");

        Assert.Multiple(() =>
        {
            Assert.That(
                plan.Groups.Single().InternalEdges,
                Has.Count.EqualTo(2),
                "The edges that made the group are declarations.");
            Assert.That(
                cycle.Runtime!.Observation.BindingExercises,
                Is.Empty,
                "Activation produces no binding exercise of its own; that is CBI16's question.");
            Assert.That(cycle.Runtime.Observation.Interactions, Is.Empty);
            Assert.That(handlers.Sum(handler => handler.ProviderEffectCount), Is.Zero);
        });
    }

    private static async Task<(
        ComponentGroupActivationResult Result,
        ActivationGroupPlan Plan,
        CoolingPortableHandler[] Handlers)>
        StronglyConnectedResult(string scenario)
    {
        var requirements = scenario == "cbi21-02-mixed-grouping-activated"
            ? new[] { Requirement, SecondaryRequirement, TertiaryRequirement }
            : [Requirement, SecondaryRequirement];
        var resolution = new FakeGenerationResolver().Resolve(RequestFor(requirements));
        var handlers = requirements.Select(_ =>
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry())).ToArray();
        var members = requirements
            .Select((requirement, index) => new ComponentGroupMember(
                Selection(resolution.Generation!.ProviderSets
                    .Single(item => item.Requirement == requirement).Members[0]) with
                {
                    Requirement = requirement,
                    HostEndpoint = $"cycle-host-{index}",
                },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    CoolingPortableFixture.Contract, handlers[index], PortableRealization.FixedDirectCall))))
            .ToArray();
        var occurrences = members.Select(item => item.Selection.Occurrence).ToArray();

        // Each scenario differs only in the plan it is given and which members it selects.
        var (plan, selected) = scenario switch
        {
            "cbi21-02-mixed-grouping-activated" => (
                CyclePlan([occurrences[0], occurrences[1]], occurrences[2]),
                members),
            "cbi21-03-protocol-group-refused" => (ProtocolPlan(occurrences), members),
            "cbi21-04-member-not-planned" => (CyclePlan([occurrences[0]]), members),
            "cbi21-05-member-not-selected" => (CyclePlan(occurrences), members[..1]),
            "cbi21-06-member-not-distinct" => (CyclePlan([occurrences[0]]), [members[0], members[0]]),
            _ => (CyclePlan(occurrences), members),
        };

        var result = await ComponentGroupLifecycle.ActivateAsync(resolution, selected, RuntimeRequest(plan));
        return (result, plan, handlers);
    }

    [Test]
    public async Task Shared_cbi22_vectors_attach_a_child_to_a_released_parent()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi22-child-port-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, parent, handlers) = await ChildActivationResult(scenario);
            var childReleased = result.Child?.Lifecycle?.Members.Count(item => item.Member.IsReleased) ?? 0;
            var parentReleased = parent.Lifecycle?.Members.Count(item => item.Member.IsReleased) ?? 0;

            Assert.Multiple(() =>
            {
                Assert.That(ChildToken(result.Kind), Is.EqualTo(vector.GetProperty("expectedKind").GetString()), scenario);
                Assert.That(result.Code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()), scenario);
                Assert.That(
                    childReleased,
                    Is.EqualTo(vector.GetProperty("expectedChildReleased").GetInt32()),
                    scenario);
                Assert.That(
                    parentReleased,
                    Is.EqualTo(vector.GetProperty("expectedParentReleased").GetInt32()),
                    scenario);
                Assert.That(
                    result.Child?.Admissions.Count ?? 0,
                    Is.EqualTo(vector.GetProperty("expectedAdmitted").GetInt32()),
                    scenario);

                // A child activation is a second activation, never a replacement of the first.
                Assert.That(
                    vector.GetProperty("expectedParentReleased").GetInt32() == 0 ||
                        !(parent.Lifecycle?.Members.Any(item =>
                            item.Member.Stage == PortableCompositionStage.Retired) ?? false),
                    Is.True,
                    $"{scenario}: nothing in a child activation stands a released parent down.");
                Assert.That(
                    childReleased == (result.Child?.Lifecycle?.Members.Count ?? 0) || childReleased == 0,
                    Is.True,
                    $"{scenario}: the child's release barrier covers the child's members.");
                Assert.That(
                    handlers.Parent.Sum(handler => handler.ProviderEffectCount),
                    Is.Zero,
                    $"{scenario}: no child outcome exercises a parent provider.");
            });
        }
    }

    [Test]
    public async Task C1_a_child_needs_a_released_parent_and_an_attachment_read_from_it()
    {
        var (unavailable, _, _) = await ChildActivationResult("cbi22-03-parent-not-released");
        var (generation, parent, _) = await ChildActivationResult("cbi22-04-parent-generation-mismatch");
        var (scope, _, _) = await ChildActivationResult("cbi22-05-child-scope-is-the-parent-scope");

        Assert.Multiple(() =>
        {
            Assert.That(unavailable.Kind, Is.EqualTo(ComponentChildActivationKind.ParentUnavailable));
            Assert.That(generation.Code, Is.EqualTo("parent-generation-mismatch"));
            Assert.That(
                scope.Code,
                Is.EqualTo("child-scope-not-distinct"),
                "A child Port exists to give its Component a restart boundary.");
            Assert.That(
                new[] { unavailable, generation, scope }.All(item => item.Child is null),
                Is.True,
                "Every refusal before establishment creates no child member.");
            Assert.That(parent.Lifecycle!.Members.All(item => item.Member.IsReleased), Is.True);
        });
    }

    [Test]
    public async Task C2_the_port_is_the_generations_and_not_the_callers()
    {
        var (foreign, _, _) = await ChildActivationResult("cbi22-06-attachment-names-another-port");
        var (loose, _, _) = await ChildActivationResult("cbi22-07-member-not-port-contained");
        var (overstated, _, _) = await ChildActivationResult("cbi22-08-port-lifecycle-overstated");
        var (attached, _, _) = await ChildActivationResult("cbi22-01-child-attached");

        Assert.Multiple(() =>
        {
            Assert.That(foreign.Code, Is.EqualTo("port-not-resolved"));
            Assert.That(loose.Code, Is.EqualTo("member-not-port-contained"));
            Assert.That(
                overstated.Code,
                Is.EqualTo("port-lifecycle-overstated"),
                "The envelope, not the caller, says what the Port permits.");
            Assert.That(
                attached.Port,
                Is.EqualTo(ChildPort),
                "An admitted attachment names the Port its members were resolved into.");
        });
    }

    [Test]
    public async Task C2_members_drawn_from_two_ports_have_no_one_port_to_attach_to()
    {
        var (parent, _) = await ChildParent(fail: false);
        var (resolution, selections) = TwoPortPositions();
        var handlers = selections.Select(_ =>
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry())).ToArray();
        var policy = GroupPolicy(ProviderLocalActor);
        var members = selections
            .Select((selection, index) => new ComponentGroupParticipant(
                new(
                    selection,
                    new PortableDirectConversation(new PortableProviderEndpoint(
                        CoolingPortableFixture.Contract, handlers[index], PortableRealization.FixedDirectCall))),
                [
                    new(
                        new(selection.Occurrence, index == 0 ? Participant : Supervisor),
                        index == 0
                            ? ProviderAuthority(policy, Authority)
                            : SupervisorAuthority(policy, AuditAuthority, revoked: false)),
                ]))
            .ToArray();
        var plan = PlanFor(
            GenerationId.Create("gen.child"),
            ChildScope,
            selections.Select(item => item.Occurrence).ToArray());
        var result = await ComponentChildActivation.AttachAsync(
            resolution,
            parent,
            members,
            RuntimeRequestFor(plan, GenerationId.Create("gen.child-retained")) with
            {
                ActiveScopes =
                [
                    new(ChildScope, GenerationId.Create("gen.child-retained"), RuntimeScopeStatus.Active),
                    new(ParentScope, GenerationId.Create("gen.lifecycle"), RuntimeScopeStatus.Active),
                ],
                Child = new(
                    ParentScope,
                    GenerationId.Create("gen.lifecycle"),
                    ChildPort,
                    RuntimeOpen: true,
                    Occupied: false,
                    ReplacementLifecycleDeclared: false,
                    HostAssisted: false,
                    InternalReleaseSequence: 0,
                    ExportReleaseSequence: 2,
                    OuterHostOwnsAdmission: false),
            });

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Code,
                Is.EqualTo("port-not-resolved"),
                "One attachment names one Port, so members from two have no single Port to attach to.");
            Assert.That(result.Child, Is.Null);
            Assert.That(
                handlers.Sum(handler => handler.ProviderEffectCount),
                Is.Zero);
        });
    }

    [Test]
    public async Task C3_a_port_contained_position_outside_a_child_activation_is_refused()
    {
        var (resolution, selection) = ChildPosition(PortLifecycleMode.RuntimeOpen);
        var member = new ComponentGroupMember(selection, DirectCooling(CoolingPortableFixture.Contract));
        var flattened = await ComponentGroupLifecycle.ActivateAsync(
            resolution,
            [member],
            RuntimeRequest(Plan(selection.Occurrence)));
        var singleton = await ComponentBindingLifecycle.ActivateAsync(
            resolution,
            selection,
            RuntimeRequest(Plan(selection.Occurrence)),
            DirectCooling(CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(
                flattened.Failure!.Code,
                Is.EqualTo("member-port-contained"),
                "The containment is a statement the generation made about where the Component runs.");
            Assert.That(flattened.Members, Is.Empty);
            Assert.That(
                singleton.Failure!.Code,
                Is.EqualTo("member-port-contained"),
                "And the singleton path flattened it too.");
            Assert.That(singleton.Member, Is.Null);
        });
    }

    [Test]
    public async Task C4_an_occupied_port_needs_an_explicit_replacement_lifecycle()
    {
        var (occupied, parent, handlers) = await ChildActivationResult("cbi22-09-occupied-port-without-replacement");

        Assert.Multiple(() =>
        {
            Assert.That(occupied.Code, Is.EqualTo("replacement-lifecycle-required"));
            Assert.That(
                occupied.Child!.Lifecycle!.Runtime!.Kind,
                Is.EqualTo(ActivationRuntimeOutcomeKind.ReplacementLifecycleRequired),
                "The classification is CM4's, reported rather than reformed.");
            Assert.That(
                handlers.Child.Sum(handler => handler.ProviderEffectCount),
                Is.Zero,
                "It reaches no provider.");
            Assert.That(parent.Lifecycle!.Members.All(item => item.Member.IsReleased), Is.True);
        });
    }

    [Test]
    public async Task C5_a_host_assisted_export_follows_the_childs_internal_release()
    {
        var (ordered, _, _) = await ChildActivationResult("cbi22-02-host-assisted-attached");
        var (conflict, _, _) = await ChildActivationResult("cbi22-10-host-assisted-order-conflict");
        var child = ordered.Child!.Lifecycle!.Runtime!.Observation.Child!;

        Assert.Multiple(() =>
        {
            Assert.That(ordered.IsAttached, Is.True);
            Assert.That(child.HostAssisted, Is.True);
            Assert.That(
                child.ExportReleaseSequence,
                Is.GreaterThan(child.InternalReleaseSequence),
                "The exported boundary is released after the child's own Release.");
            Assert.That(conflict.Code, Is.EqualTo("host-assisted-order-conflict"));
        });
    }

    [Test]
    public async Task C6_the_parent_is_untouched_in_every_outcome()
    {
        var (attached, parent, _) = await ChildActivationResult("cbi22-01-child-attached");
        var survivor = parent.Lifecycle!.Members[0].Member;
        var attempted = await survivor.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableConstraint.Atom(PortableTruth.Satisfied));
        var observation = attached.Child!.Lifecycle!.Runtime!.Observation;

        Assert.Multiple(() =>
        {
            Assert.That(attached.IsAttached, Is.True);
            Assert.That(parent.Lifecycle.Members.All(item => item.Member.IsReleased), Is.True);
            Assert.That(
                attempted.FrameDecision,
                Is.Not.EqualTo(PortableFrameDecision.None),
                "The parent is still serving ordinary interaction.");
            Assert.That(
                observation.Scopes.Single(item => item.Scope == ParentScope).Generation,
                Is.EqualTo(GenerationId.Create("gen.lifecycle")),
                "And CM4 reports the parent scope carrying the generation it already had.");
        });
    }

    [Test]
    public async Task C7_the_childs_barriers_are_its_own()
    {
        var (neverReady, parent, _) = await ChildActivationResult("cbi22-11-child-member-never-ready");
        var (attached, attachedParent, _) = await ChildActivationResult("cbi22-01-child-attached");

        Assert.Multiple(() =>
        {
            Assert.That(neverReady.Code, Is.EqualTo("child-establishment-refused"));
            Assert.That(
                neverReady.Child!.Lifecycle!.Members.Count(item => item.Member.IsReleased),
                Is.Zero);
            Assert.That(
                parent.Lifecycle!.Members.Count(item => item.Member.IsReleased),
                Is.EqualTo(2),
                "A child that never comes up leaves the parent exactly as it was.");
            Assert.That(
                attached.Child!.Lifecycle!.Members.All(item => item.Member.IsReleased),
                Is.True);
            Assert.That(
                attachedParent.Lifecycle!.Members.Count(item => item.Member.IsReleased),
                Is.EqualTo(2),
                "And so does one that does.");
        });
    }

    [Test]
    public async Task C8_authority_is_the_childs_own()
    {
        var (denied, _, handlers) = await ChildActivationResult("cbi22-12-child-authority-denied");
        var (attached, parent, _) = await ChildActivationResult("cbi22-01-child-attached");

        Assert.Multiple(() =>
        {
            Assert.That(denied.Code, Is.EqualTo("authority-not-admitted"));
            Assert.That(
                handlers.Child.Sum(handler => handler.ProviderEffectCount),
                Is.Zero,
                "A denied child admission contacts no child provider.");
            Assert.That(attached.Child!.Admissions, Has.Count.EqualTo(1));
            Assert.That(
                attached.Child.Grants.Select(item => item.Grant)
                    .Intersect(parent.Grants.Select(item => item.Grant)),
                Is.Empty,
                "The parent's grants admit nothing for a child member.");
        });
    }

    private static string ChildToken(ComponentChildActivationKind kind) => kind switch
    {
        ComponentChildActivationKind.Attached => "attached",
        ComponentChildActivationKind.Declined => "declined",
        ComponentChildActivationKind.ParentUnavailable => "parent-unavailable",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static readonly RegionId ChildRegion = RegionId.Create("region.child");
    private static readonly PortId ChildPort = PortId.Create("port.child");
    private static readonly RestartScopeId ParentScope = RestartScopeId.Create("restart.lifecycle");
    private static readonly RestartScopeId ChildScope = RestartScopeId.Create("restart.child");

    /// <summary>One position CM2 resolved inside a child Port of the named lifecycle.</summary>
    private static (ResolutionOutcome Resolution, ComponentBindingSelection Selection) ChildPosition(
        PortLifecycleMode lifecycle)
    {
        var single = Request(Cardinality.Parse("1..1"));
        var consumer = single.Definitions.Single(item => item.Definition == Consumer);
        var contained = consumer.Requirements.Single() with
        {
            ContainingRegion = ChildRegion,
            ContainingPort = ChildPort,
            RuntimeAttachment = lifecycle == PortLifecycleMode.RuntimeOpen,
        };
        var resolution = new FakeGenerationResolver().Resolve(single with
        {
            Definitions = [consumer with { Requirements = [contained] }, .. single.Definitions.Skip(1)],
            Ports =
            [
                new PortEnvelope(
                    ChildRegion,
                    ChildPort,
                    lifecycle,
                    [new ProvidedContract(Contract, Version)],
                    Cardinality.Parse("1..1"),
                    [],
                    [],
                    [],
                    [],
                    "isolate",
                    "scope",
                    false),
            ],
        });
        var member = resolution.Generation!.ProviderSets.Single().Members.Single();
        return (resolution, Selection(member) with { HostEndpoint = "child-host" });
    }

    private static async Task<(
        ComponentChildActivationResult Result,
        ComponentGroupAuthorityResult Parent,
        (CoolingPortableHandler[] Parent, CoolingPortableHandler[] Child) Handlers)>
        ChildActivationResult(string scenario)
    {
        var (parent, parentHandlers) = await ChildParent(scenario == "cbi22-03-parent-not-released");
        var (resolution, selection) = scenario == "cbi22-07-member-not-port-contained"
            ? LooseChildPosition()
            : ChildPosition(scenario == "cbi22-08-port-lifecycle-overstated"
                ? PortLifecycleMode.ActivationOpen
                : PortLifecycleMode.RuntimeOpen);
        var childHandlers = new[] { new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()) };
        var document = scenario == "cbi22-11-child-member-never-ready"
            ? CoolingPortableFixture.Contract with
            {
                Provider = PortableProviderReference.Parse("brontide.fake.substituted", 1),
            }
            : CoolingPortableFixture.Contract;
        var member = new ComponentGroupMember(
            selection,
            new PortableDirectConversation(new PortableProviderEndpoint(
                document, childHandlers[0], PortableRealization.FixedDirectCall)));
        var policy = GroupPolicy(ProviderLocalActor);
        // A child member's authority is its own request; reusing the parent's identity would give
        // the two the same grant identity without CM5 having decided anything about the child.
        var childRelationship = RelationshipRequestId.Create("relationship.child");
        var request = ProviderAuthority(policy, Authority) with
        {
            Request = AdmissionRequestId.Create("admission.child"),
            Relationships =
            [
                new(childRelationship, Participant, ActorRelationshipKind.ComponentParticipant, [AuthorityEvidence]),
            ],
            Authority =
            [
                new(
                    AuthorityRequestId.Create("authority.child-control"),
                    childRelationship,
                    Capability,
                    Target,
                    Operation,
                    AuthorityScope,
                    false),
            ],
        };
        if (scenario == "cbi22-12-child-authority-denied")
        {
            request = request with
            {
                Evidence = [SetEvidence(AuthorityEvidence, Participant) with { State = AdmissionEvidenceState.Revoked }],
            };
        }

        var childScope = scenario == "cbi22-05-child-scope-is-the-parent-scope" ? ParentScope : ChildScope;
        var plan = PlanFor(GenerationId.Create("gen.child"), childScope, selection.Occurrence);
        var hostAssisted = scenario is "cbi22-02-host-assisted-attached" or "cbi22-10-host-assisted-order-conflict";
        var runtimeRequest = RuntimeRequestFor(plan, GenerationId.Create("gen.child-retained")) with
        {
            ActiveScopes =
            [
                new ActiveScopeSnapshot(childScope, GenerationId.Create("gen.child-retained"), RuntimeScopeStatus.Active),
                .. childScope == ParentScope
                    ? Array.Empty<ActiveScopeSnapshot>()
                    : [new ActiveScopeSnapshot(ParentScope, GenerationId.Create("gen.lifecycle"), RuntimeScopeStatus.Active)],
            ],
            Child = new ChildActivationDeclaration(
                ParentScope,
                scenario == "cbi22-04-parent-generation-mismatch"
                    ? GenerationId.Create("gen.other")
                    : GenerationId.Create("gen.lifecycle"),
                scenario == "cbi22-06-attachment-names-another-port" ? PortId.Create("port.other") : ChildPort,
                RuntimeOpen: true,
                Occupied: scenario == "cbi22-09-occupied-port-without-replacement",
                ReplacementLifecycleDeclared: false,
                HostAssisted: hostAssisted,
                InternalReleaseSequence: hostAssisted ? 1 : 0,
                ExportReleaseSequence: scenario == "cbi22-10-host-assisted-order-conflict" ? 1 : 2,
                OuterHostOwnsAdmission: false),
        };

        var result = await ComponentChildActivation.AttachAsync(
            resolution,
            parent,
            [new(member, [new(new(selection.Occurrence, Participant), request)])],
            runtimeRequest);
        return (result, parent, (parentHandlers, childHandlers));
    }

    /// <summary>Two positions, each resolved into a Port of its own.</summary>
    private static (ResolutionOutcome Resolution, ComponentBindingSelection[] Selections) TwoPortPositions()
    {
        var secondPort = PortId.Create("port.child-secondary");
        var pair = RequestFor(Requirement, SecondaryRequirement);
        var consumer = pair.Definitions.Single(item => item.Definition == Consumer);
        var contained = consumer.Requirements
            .Select(item => item with
            {
                ContainingRegion = ChildRegion,
                ContainingPort = item.Requirement == Requirement ? ChildPort : secondPort,
                RuntimeAttachment = true,
            })
            .ToArray();
        PortEnvelope Envelope(PortId port, ContractId contract) => new(
            ChildRegion,
            port,
            PortLifecycleMode.RuntimeOpen,
            [new ProvidedContract(contract, Version)],
            Cardinality.Parse("1..1"),
            [],
            [],
            [],
            [],
            "isolate",
            "scope",
            false);
        var resolution = new FakeGenerationResolver().Resolve(pair with
        {
            Definitions = [consumer with { Requirements = contained }, .. pair.Definitions.Skip(1)],
            Ports = [Envelope(ChildPort, Contract), Envelope(secondPort, SecondaryContract)],
        });
        var selections = new[] { Requirement, SecondaryRequirement }
            .Select((requirement, index) => Selection(resolution.Generation!.ProviderSets
                .Single(item => item.Requirement == requirement).Members[0]) with
            {
                Requirement = requirement,
                HostEndpoint = $"two-port-host-{index}",
            })
            .ToArray();
        return (resolution, selections);
    }

    /// <summary>A position resolved outside any Port, for the attachment that has nothing to attach.</summary>
    private static (ResolutionOutcome Resolution, ComponentBindingSelection Selection) LooseChildPosition()
    {
        var resolution = new FakeGenerationResolver().Resolve(Request(Cardinality.Parse("1..1")));
        var member = resolution.Generation!.ProviderSets.Single().Members.Single();
        return (resolution, Selection(member) with { HostEndpoint = "child-host" });
    }

    /// <summary>The parent activation a child attaches to: two members over the parent scope.</summary>
    private static async Task<(ComponentGroupAuthorityResult Parent, CoolingPortableHandler[] Handlers)>
        ChildParent(bool fail)
    {
        var resolution = new FakeGenerationResolver().Resolve(RequestFor(Requirement, SecondaryRequirement));
        var handlers = new[]
        {
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
        };
        var secondDocument = fail
            ? CoolingPortableFixture.Contract with
            {
                Provider = PortableProviderReference.Parse("brontide.fake.substituted", 1),
            }
            : CoolingPortableFixture.Contract;
        var members = new[] { Requirement, SecondaryRequirement }
            .Select((requirement, index) => new ComponentGroupMember(
                Selection(resolution.Generation!.ProviderSets
                    .Single(item => item.Requirement == requirement).Members[0]) with
                {
                    Requirement = requirement,
                    HostEndpoint = $"parent-host-{index}",
                },
                new PortableDirectConversation(new PortableProviderEndpoint(
                    index == 1 ? secondDocument : CoolingPortableFixture.Contract,
                    handlers[index],
                    PortableRealization.FixedDirectCall))))
            .ToArray();
        var policy = GroupPolicy(ProviderLocalActor);
        var parent = await ComponentGroupAuthority.ActivateAsync(
            resolution,
            [
                new(members[0], [new(new(members[0].Selection.Occurrence, Participant), ProviderAuthority(policy, Authority))]),
                new(members[1], [new(new(members[1].Selection.Occurrence, Supervisor), SupervisorAuthority(policy, AuditAuthority, revoked: false))]),
            ],
            RuntimeRequest(Plan(members.Select(item => item.Selection.Occurrence).ToArray())));
        return (parent, handlers);
    }

    [Test]
    public async Task Shared_cbi23_vectors_nest_a_child_beneath_a_child()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi23-nested-child-port-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var tree = await NestedTree(scenario);

            Assert.Multiple(() =>
            {
                Assert.That(ChildToken(tree.Result.Kind), Is.EqualTo(vector.GetProperty("expectedKind").GetString()), scenario);
                Assert.That(tree.Result.Code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()), scenario);
                Assert.That(
                    tree.Depth,
                    Is.EqualTo(vector.GetProperty("expectedDepth").GetInt32()),
                    scenario);
                Assert.That(
                    tree.ReleasedMembers,
                    Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                    scenario);

                // Every level above the one being attached is left exactly as it was.
                Assert.That(
                    tree.Levels.SkipLast(1).All(level =>
                        level.Lifecycle!.Members.All(item => item.Member.IsReleased)),
                    Is.True,
                    $"{scenario}: an attachment disturbs no level above it.");
            });
        }
    }

    [Test]
    public async Task Shared_cbi23_withdrawals_retire_an_attachment_tree_deepest_first()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi23-nested-child-port-vectors.json")));

        foreach (var vector in fixture.RootElement.GetProperty("withdrawals").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var (result, levels) = await WithdrawalResult(scenario);
            var expectedScopes = vector.GetProperty("expectedRetiredScopes")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(WithdrawalToken(result.Kind), Is.EqualTo(vector.GetProperty("expectedKind").GetString()), scenario);
                Assert.That(result.Code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()), scenario);
                Assert.That(
                    result.Retired.Select(item => item.Scope.Value),
                    Is.EqualTo(expectedScopes),
                    $"{scenario}: the cascade order is deepest first.");
                Assert.That(
                    levels.Sum(level => level.Lifecycle!.Members.Count(item => item.Member.IsReleased)),
                    Is.EqualTo(vector.GetProperty("expectedReleasedAfter").GetInt32()),
                    scenario);

                // A refusal of the relation retires nothing at all.
                Assert.That(
                    result.Kind != ComponentAttachmentWithdrawalKind.Declined || result.Retired.Count == 0,
                    Is.True,
                    $"{scenario}: a declined withdrawal retires nothing.");
            });
        }
    }

    [Test]
    public async Task C1_a_child_activation_is_an_ordinary_parent()
    {
        var attached = await NestedTree("cbi23-01-grandchild-attached");
        var overstated = await NestedTree("cbi23-02-grandchild-port-lifecycle-overstated");
        var scope = await NestedTree("cbi23-03-grandchild-scope-is-its-parents");
        var generation = await NestedTree("cbi23-04-grandchild-parent-generation-mismatch");

        Assert.Multiple(() =>
        {
            Assert.That(attached.Result.IsAttached, Is.True);
            Assert.That(
                attached.Result.Port,
                Is.EqualTo(GrandchildPort),
                "The grandchild names the Port its own position was resolved into.");
            Assert.That(
                overstated.Result.Code,
                Is.EqualTo("port-lifecycle-overstated"),
                "CBI22's envelope rule applies at the second level unchanged.");
            Assert.That(scope.Result.Code, Is.EqualTo("child-scope-not-distinct"));
            Assert.That(generation.Result.Code, Is.EqualTo("parent-generation-mismatch"));
            Assert.That(
                new[] { overstated, scope, generation }.All(item =>
                    item.Levels[1].Lifecycle!.Members.All(member => member.Member.IsReleased)),
                Is.True,
                "And a refusal at the second level leaves the first child released.");
        });
    }

    [Test]
    public async Task C2_depth_is_not_bounded_by_this_slice()
    {
        var attached = await NestedTree("cbi23-01-grandchild-attached");
        var greatGrandchild = await AttachLevel(
            attached.Levels[2],
            GrandchildScope,
            GenerationId.Create("gen.grandchild"),
            new AttachSpec(
                PortId.Create("port.great-grandchild"),
                RestartScopeId.Create("restart.great-grandchild"),
                GenerationId.Create("gen.great-grandchild"),
                PortLifecycleMode.RuntimeOpen,
                RuntimeOpen: true,
                Suffix: "great"));

        Assert.Multiple(() =>
        {
            Assert.That(
                greatGrandchild.Result.IsAttached,
                Is.True,
                "A fourth level is admitted on exactly the terms the second was.");
            Assert.That(greatGrandchild.Result.Code, Is.EqualTo("child-attached"));
            Assert.That(
                attached.Levels.All(level => level.Lifecycle!.Members.All(item => item.Member.IsReleased)),
                Is.True);
        });
    }

    [Test]
    public async Task C3_the_attachment_relation_is_derived_and_checked()
    {
        var (duplicate, levels) = await WithdrawalResult("cbi23-09-duplicate-scope");
        var (cascade, _) = await WithdrawalResult("cbi23-06-cascade-deepest-first");

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Code, Is.EqualTo("scope-not-distinct"));
            Assert.That(
                duplicate.Retired,
                Is.Empty,
                "Every refusal of the relation itself retires nothing.");
            Assert.That(
                levels.All(level => level.Lifecycle!.Members.All(item => item.Member.IsReleased)),
                Is.True);

            // The relation is read from each activation rather than declared: the middle level knows
            // its parent because CM4 recorded the attachment, not because the caller said so.
            Assert.That(
                cascade.Retired.Select(item => item.Scope.Value),
                Is.EqualTo(new[] { "restart.grandchild", "restart.child", "restart.lifecycle" }));
        });
    }

    [Test]
    public async Task C4_a_child_is_retired_before_the_parent_whose_port_it_occupies()
    {
        var (cascade, _) = await WithdrawalResult("cbi23-06-cascade-deepest-first");
        var order = cascade.Retired.Select(item => item.Scope.Value).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(cascade.IsWithdrawn, Is.True);
            Assert.That(
                Array.IndexOf(order, "restart.grandchild"),
                Is.LessThan(Array.IndexOf(order, "restart.child")),
                "The grandchild goes before the child whose Port it occupies.");
            Assert.That(
                Array.IndexOf(order, "restart.child"),
                Is.LessThan(Array.IndexOf(order, "restart.lifecycle")),
                "And the child before the parent whose Port it occupies.");
        });
    }

    [Test]
    public async Task C5_the_root_can_only_order_what_it_is_given()
    {
        var (partial, levels) = await WithdrawalResult("cbi23-07-cascade-from-the-middle");

        Assert.Multiple(() =>
        {
            Assert.That(partial.IsWithdrawn, Is.True);
            Assert.That(
                partial.Retired.Select(item => item.Scope.Value),
                Is.EqualTo(new[] { "restart.grandchild", "restart.child" }),
                "The outcome names exactly the scopes it retired.");
            Assert.That(
                levels[0].Lifecycle!.Members.All(item => item.Member.IsReleased),
                Is.True,
                "A parent the caller did not name is left running, which is visible by absence.");
        });
    }

    [Test]
    public async Task C6_an_attachment_beneath_a_retired_parent_is_refused()
    {
        var refused = await NestedTree("cbi23-05-attachment-beneath-a-retired-parent");

        Assert.Multiple(() =>
        {
            Assert.That(refused.Result.Kind, Is.EqualTo(ComponentChildActivationKind.ParentUnavailable));
            Assert.That(refused.Result.Child, Is.Null);
            Assert.That(
                refused.Levels[1].Lifecycle!.Members.All(item =>
                    item.Member.Stage == PortableCompositionStage.Retired),
                Is.True,
                "Its parent is gone, and CBI22's own precondition is what refuses it.");
        });
    }

    [Test]
    public async Task C7_a_cleanup_failure_is_named_and_restores_nothing()
    {
        var (result, levels) = await WithdrawalResult("cbi23-08-cleanup-fails-in-the-child");

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(ComponentAttachmentWithdrawalKind.CleanupFailed));
            Assert.That(result.Reason, Does.Contain("withdraw-refused"));
            Assert.That(
                result.Retired.Select(item => item.Scope.Value),
                Is.EqualTo(new[] { "restart.grandchild", "restart.child", "restart.lifecycle" }),
                "The cascade continues past the failure rather than stopping.");
            Assert.That(
                result.Retired.Count(item => item.Cleanup is not null),
                Is.EqualTo(1),
                "And the failure is reported against the scope it happened in.");
            Assert.That(
                levels.SelectMany(level => level.Lifecycle!.Members).Any(item => item.Member.IsReleased),
                Is.False,
                "Nothing is returned to released.");
        });
    }

    [Test]
    public async Task C8_nesting_adds_no_grant_and_leaves_the_earlier_slices_alone()
    {
        var attached = await NestedTree("cbi23-01-grandchild-attached");
        var grants = attached.Levels.SelectMany(level => level.Grants.Select(item => item.Grant.Value)).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                grants.Distinct().Count(),
                Is.EqualTo(grants.Length),
                "Each level's authority is its own request, so no grant identity is shared.");
            Assert.That(
                attached.Levels[2].Lifecycle!.Runtime!.Observation.Child!.ParentScope,
                Is.EqualTo(ChildScope),
                "The grandchild's attachment names the child's scope, not the root's.");
            Assert.That(
                attached.Levels[0].Lifecycle!.Runtime!.Observation.Child,
                Is.Null,
                "And the root is not itself an attachment.");
        });
    }

    private static string WithdrawalToken(ComponentAttachmentWithdrawalKind kind) => kind switch
    {
        ComponentAttachmentWithdrawalKind.Withdrawn => "withdrawn",
        ComponentAttachmentWithdrawalKind.CleanupFailed => "cleanup-failed",
        ComponentAttachmentWithdrawalKind.Declined => "declined",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static readonly PortId GrandchildPort = PortId.Create("port.grandchild");
    private static readonly RestartScopeId GrandchildScope = RestartScopeId.Create("restart.grandchild");

    private sealed record AttachSpec(
        PortId Port,
        RestartScopeId Scope,
        GenerationId Generation,
        PortLifecycleMode Lifecycle,
        bool RuntimeOpen,
        string Suffix,
        bool FailCleanup = false,
        GenerationId? DeclaredParentGeneration = null);

    private sealed record AttachedLevel(
        ComponentChildActivationResult Result,
        IReadOnlyList<ComponentGroupAuthorityResult> Levels,
        CoolingPortableHandler[] Handlers)
    {
        public int Depth => Levels.Count;

        public int ReleasedMembers =>
            Levels.Sum(level => level.Lifecycle?.Members.Count(item => item.Member.IsReleased) ?? 0);
    }

    /// <summary>One attachment beneath the given parent, with everything it needs derived from the spec.</summary>
    private static async Task<AttachedLevel> AttachLevel(
        ComponentGroupAuthorityResult parent,
        RestartScopeId parentScope,
        GenerationId parentGeneration,
        AttachSpec spec,
        IReadOnlyList<ComponentGroupAuthorityResult>? above = null)
    {
        var (resolution, selection) = PortPosition(spec.Port, spec.Lifecycle, $"{spec.Suffix}-host");
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        var conversation = new PortableDirectConversation(new PortableProviderEndpoint(
            CoolingPortableFixture.Contract, handler, PortableRealization.FixedDirectCall));
        var member = new ComponentGroupMember(
            selection,
            spec.FailCleanup ? new FailingRetirementConversation(conversation) : conversation);
        var relationship = RelationshipRequestId.Create($"relationship.{spec.Suffix}");
        var policy = GroupPolicy(ProviderLocalActor);
        var request = ProviderAuthority(policy, Authority) with
        {
            Request = AdmissionRequestId.Create($"admission.{spec.Suffix}"),
            Relationships =
            [
                new(relationship, Participant, ActorRelationshipKind.ComponentParticipant, [AuthorityEvidence]),
            ],
            Authority =
            [
                new(
                    AuthorityRequestId.Create($"authority.{spec.Suffix}"),
                    relationship,
                    Capability,
                    Target,
                    Operation,
                    AuthorityScope,
                    false),
            ],
        };
        var plan = PlanFor(spec.Generation, spec.Scope, selection.Occurrence);
        var runtimeRequest = RuntimeRequestFor(plan, GenerationId.Create($"{spec.Generation.Value}-retained")) with
        {
            ActiveScopes =
            [
                new(spec.Scope, GenerationId.Create($"{spec.Generation.Value}-retained"), RuntimeScopeStatus.Active),
                .. spec.Scope == parentScope
                    ? Array.Empty<ActiveScopeSnapshot>()
                    : [new ActiveScopeSnapshot(parentScope, parentGeneration, RuntimeScopeStatus.Active)],
            ],
            Child = new ChildActivationDeclaration(
                parentScope,
                spec.DeclaredParentGeneration ?? parentGeneration,
                spec.Port,
                spec.RuntimeOpen,
                Occupied: false,
                ReplacementLifecycleDeclared: false,
                HostAssisted: false,
                InternalReleaseSequence: 0,
                ExportReleaseSequence: 2,
                OuterHostOwnsAdmission: false),
        };

        var result = await ComponentChildActivation.AttachAsync(
            resolution,
            parent,
            [new(member, [new(new(selection.Occurrence, Participant), request)])],
            runtimeRequest);
        var levels = new List<ComponentGroupAuthorityResult>(above ?? []);
        if (levels.Count == 0)
        {
            levels.Add(parent);
        }

        if (result.Child is { } child && result.IsAttached)
        {
            levels.Add(child);
        }

        return new(result, levels, [handler]);
    }

    /// <summary>A parent, a child beneath it, and a grandchild beneath that.</summary>
    private static async Task<AttachedLevel> NestedTree(string scenario)
    {
        var (root, _) = await ChildParent(fail: false);
        var child = await AttachLevel(
            root,
            ParentScope,
            GenerationId.Create("gen.lifecycle"),
            new AttachSpec(
                ChildPort,
                ChildScope,
                GenerationId.Create("gen.child"),
                PortLifecycleMode.RuntimeOpen,
                RuntimeOpen: true,
                Suffix: "child"));
        if (scenario == "cbi23-05-attachment-beneath-a-retired-parent")
        {
            foreach (var outcome in child.Levels[1].Lifecycle!.Members)
            {
                await outcome.Member.RetireAsync("retired before the grandchild attaches");
            }
        }

        var spec = new AttachSpec(
            GrandchildPort,
            scenario == "cbi23-03-grandchild-scope-is-its-parents" ? ChildScope : GrandchildScope,
            GenerationId.Create("gen.grandchild"),
            scenario == "cbi23-02-grandchild-port-lifecycle-overstated"
                ? PortLifecycleMode.ActivationOpen
                : PortLifecycleMode.RuntimeOpen,
            RuntimeOpen: true,
            Suffix: "grandchild",
            DeclaredParentGeneration: scenario == "cbi23-04-grandchild-parent-generation-mismatch"
                ? GenerationId.Create("gen.other")
                : null);
        return await AttachLevel(
            child.Levels[1],
            ChildScope,
            GenerationId.Create("gen.child"),
            spec,
            child.Levels);
    }

    private static async Task<(
        ComponentAttachmentWithdrawalResult Result,
        IReadOnlyList<ComponentGroupAuthorityResult> Levels)>
        WithdrawalResult(string scenario)
    {
        var (root, _) = await ChildParent(fail: false);
        var child = await AttachLevel(
            root,
            ParentScope,
            GenerationId.Create("gen.lifecycle"),
            new AttachSpec(
                ChildPort,
                ChildScope,
                GenerationId.Create("gen.child"),
                PortLifecycleMode.RuntimeOpen,
                RuntimeOpen: true,
                Suffix: "child",
                FailCleanup: scenario == "cbi23-08-cleanup-fails-in-the-child"));
        var grandchild = await AttachLevel(
            child.Levels[1],
            ChildScope,
            GenerationId.Create("gen.child"),
            new AttachSpec(
                GrandchildPort,
                GrandchildScope,
                GenerationId.Create("gen.grandchild"),
                PortLifecycleMode.RuntimeOpen,
                RuntimeOpen: true,
                Suffix: "grandchild"),
            child.Levels);
        var levels = grandchild.Levels;

        var given = scenario switch
        {
            "cbi23-07-cascade-from-the-middle" => new[] { levels[1], levels[2] },
            "cbi23-09-duplicate-scope" => [levels[0], levels[1], levels[2], levels[2]],
            _ => [levels[0], levels[1], levels[2]],
        };
        var result = await ComponentAttachmentWithdrawal.WithdrawAsync(
            given,
            $"attachment withdrawal {scenario}");
        return (result, levels);
    }

    /// <summary>One position CM2 resolved inside the named Port, with the named lifecycle.</summary>
    private static (ResolutionOutcome Resolution, ComponentBindingSelection Selection) PortPosition(
        PortId port,
        PortLifecycleMode lifecycle,
        string endpoint)
    {
        var single = Request(Cardinality.Parse("1..1"));
        var consumer = single.Definitions.Single(item => item.Definition == Consumer);
        var contained = consumer.Requirements.Single() with
        {
            ContainingRegion = ChildRegion,
            ContainingPort = port,
            RuntimeAttachment = lifecycle == PortLifecycleMode.RuntimeOpen,
        };
        var resolution = new FakeGenerationResolver().Resolve(single with
        {
            Definitions = [consumer with { Requirements = [contained] }, .. single.Definitions.Skip(1)],
            Ports =
            [
                new PortEnvelope(
                    ChildRegion,
                    port,
                    lifecycle,
                    [new ProvidedContract(Contract, Version)],
                    Cardinality.Parse("1..1"),
                    [],
                    [],
                    [],
                    [],
                    "isolate",
                    "scope",
                    false),
            ],
        });
        var member = resolution.Generation!.ProviderSets.Single().Members.Single();
        return (resolution, Selection(member) with { HostEndpoint = endpoint });
    }

    private static string GroupFailureToken(ComponentGroupActivationFailureKind kind) => kind switch
    {
        ComponentGroupActivationFailureKind.PlanUnsupported => "plan-unsupported",
        ComponentGroupActivationFailureKind.PreparationUnavailable => "preparation-unavailable",
        ComponentGroupActivationFailureKind.RuntimeRefusedBeforeStart => "runtime-refused-before-start",
        ComponentGroupActivationFailureKind.MemberEstablishmentRefused => "member-establishment-refused",
        ComponentGroupActivationFailureKind.MemberReleaseRefused => "member-release-refused",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static ResolutionRequest Request(Cardinality cardinality) => Request(cardinality, []);

    private static ResolutionRequest Request(Cardinality cardinality, IReadOnlyList<string> declaredAuthority) =>
        Request(cardinality, declaredAuthority, BindingScopeId.Create("scope.cooling"));

    private static ResolutionRequest Request(
        Cardinality cardinality,
        IReadOnlyList<string> declaredAuthority,
        BindingScopeId scope)
    {
        var requirement = new ResolutionRequirement(
            Requirement,
            Contract,
            Version,
            scope,
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
            declaredAuthority.ToArray());
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

    /// <summary>
    /// One independent requirement per named position, so the generation resolves exactly those and
    /// a membership can be drawn from any subset of them.
    /// </summary>
    private static ResolutionRequest RequestFor(params RequirementId[] requirements)
    {
        var single = Request(Cardinality.Parse("1..1"));
        var consumer = single.Definitions.Single(item => item.Definition == Consumer);
        var provider = single.Definitions.Single(item => item.Definition == Provider);
        var template = consumer.Requirements.Single();
        var candidate = single.Candidates.Single();
        var chosen = PositionCatalog.Where(item => requirements.Contains(item.Requirement)).ToArray();
        return single with
        {
            Definitions =
            [
                consumer with
                {
                    Requirements =
                    [
                        .. chosen.Select(item => template with
                        {
                            Requirement = item.Requirement,
                            Contract = item.Contract,
                        }),
                    ],
                },
                .. chosen.Select(item => provider with
                {
                    Definition = item.Provider,
                    Provides = [new ProvidedContract(item.Contract, Version)],
                }),
            ],
            Candidates =
            [
                .. chosen.Select(item => candidate with
                {
                    Definition = item.Provider,
                    Provides = [new ProvidedContract(item.Contract, Version)],
                }),
            ],
        };
    }

    private static ResolutionRequest PairRequest() => PairRequest([], []);

    /// <summary>Two independent requirements, so the generation resolves two distinct occurrences.</summary>
    private static ResolutionRequest PairRequest(
        IReadOnlyList<string> firstAuthority,
        IReadOnlyList<string> secondAuthority)
    {
        var single = Request(Cardinality.Parse("1..1"), firstAuthority);
        var secondaryRequirement = single.Definitions
            .Single(item => item.Definition == Consumer).Requirements.Single() with
        {
            Requirement = SecondaryRequirement,
            Contract = SecondaryContract,
        };
        var consumer = single.Definitions.Single(item => item.Definition == Consumer);
        var provider = single.Definitions.Single(item => item.Definition == Provider);
        var candidate = single.Candidates.Single();
        return single with
        {
            Definitions =
            [
                consumer with { Requirements = [consumer.Requirements.Single(), secondaryRequirement] },
                provider,
                provider with
                {
                    Definition = SecondaryProvider,
                    Provides = [new ProvidedContract(SecondaryContract, Version)],
                    RequestedAuthority = secondAuthority.ToArray(),
                },
            ],
            Candidates =
            [
                candidate,
                candidate with
                {
                    Definition = SecondaryProvider,
                    Provides = [new ProvidedContract(SecondaryContract, Version)],
                },
            ],
        };
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

    private sealed class FailingRetirementConversation(IPortableProviderConversation inner)
        : IPortableProviderConversation
    {
        public PortableRealization Realization => inner.Realization;

        public ValueTask<PortableContractDocument> EstablishAsync(
            PortableContractDocument required,
            string hostEndpoint,
            PortableChannelId channel,
            CancellationToken cancellationToken) =>
            inner.EstablishAsync(required, hostEndpoint, channel, cancellationToken);

        public ValueTask AwaitReadyAsync(
            PortableChannelId channel,
            CancellationToken cancellationToken) =>
            inner.AwaitReadyAsync(channel, cancellationToken);

        public ValueTask<PortableOutcomeReceipt> RequestAsync(
            PortableBindingPlan plan,
            PortableChannelId channel,
            PortableChannelRequestId request,
            PortableChannelExecutionId? execution,
            PortableOperationReference? operation,
            int? compactOperation,
            PortableShapeReference inputShape,
            PortableValue input,
            IReadOnlyList<PortableResource> resources,
            CancellationToken cancellationToken) =>
            inner.RequestAsync(
                plan,
                channel,
                request,
                execution,
                operation,
                compactOperation,
                inputShape,
                input,
                resources,
                cancellationToken);

        public ValueTask WithdrawAsync(
            PortableChannelId channel,
            CancellationToken cancellationToken) =>
            ValueTask.FromException(
                new PortableFaultException(
                    PortableProtocolCategory.StateViolation,
                    "withdraw-refused",
                    "the test peer refused withdrawal"));

        public ValueTask TerminateAsync(
            PortableChannelId channel,
            CancellationToken cancellationToken) =>
            inner.TerminateAsync(channel, cancellationToken);

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
