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

    private static ComponentParticipantRequest[] ParticipantSet(LocalActorReferenceId supervisorActor)
    {
        var policy = SetPolicy(supervisorActor);
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

    private static LocalAuthorityPolicy SetPolicy(LocalActorReferenceId supervisorActor) =>
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
