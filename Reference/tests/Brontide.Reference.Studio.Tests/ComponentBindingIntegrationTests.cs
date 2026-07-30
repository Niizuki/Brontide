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

    /// <summary>Two independent requirements, so the generation resolves two distinct occurrences.</summary>
    private static ResolutionRequest PairRequest()
    {
        var single = Request(Cardinality.Parse("1..1"));
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
