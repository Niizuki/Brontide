namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Text.Json
open NUnit.Framework
open Brontide.Minimal.Binding.Portable
open Brontide.Minimal.Experimental.ComponentManagement
open Brontide.Minimal.Host

[<TestFixture>]
type ComponentBindingIntegrationTests() =
    let consumer = DefinitionId.create "def.test.cooling-consumer"
    let provider = DefinitionId.create "def.test.cooling-provider"
    let requirementId = RequirementId.create "req.cooling"
    let contractId = ContractId.create "brontide.fake.cooling"
    let version = VersionLiteral.create "1.0"
    let participant = ActorId.create "actor.cooling-provider"
    let authorityTarget = ActorId.create "actor.cooling-target"
    let authorityEvidence = EvidenceId.create "evidence.cooling-provider"
    let authorityIssuer = IssuerId.create "issuer.integration-host"
    let relationshipId = RelationshipRequestId.create "relationship.cooling-provider"
    let authorityId = AuthorityRequestId.create "authority.cooling-control"
    let capability = CapabilityId.create "capability.cooling-control"
    let operation = OperationId.create "cooling.set-enabled"
    let authorityScope = CapabilityScopeId.create "scope.cooling-session"
    let evaluationTime = DateTimeOffset(2026, 7, 30, 10, 0, 0, TimeSpan.Zero)
    let multiple action = Assert.Multiple(Action action)

    let request cardinality =
        let requirement =
            { Requirement = requirementId
              Contract = contractId
              Version = version
              Scope =
                Brontide.Minimal.Experimental.ComponentManagement.BindingScopeId.create
                    "scope.cooling"
              Cardinality = cardinality
              AllowSharing = false
              Exposure = ProviderExposure.Distinct
              Mediation = None
              Constraints = []
              ContainingRegion = None
              ContainingPort = None
              RuntimeAttachment = false
              RequiredImports = []
              RequiredExports = []
              RequiredFailurePolicy = None
              RequiredRollbackBoundary = None
              RequestedAuthority = []
              TopologyRequirements = [] }
        let consumerDefinition =
            { Definition = consumer
              Publisher = PublisherId.create "pub.test"
              Provides = []
              Requirements = [ requirement ]
              CompositionParameters = []
              ActivationParameters = []
              RequestedAuthority = [] }
        let providerDefinition =
            { Definition = provider
              Publisher = PublisherId.create "pub.test"
              Provides = [ { Contract = contractId; Version = version } ]
              Requirements = []
              CompositionParameters = []
              ActivationParameters = []
              RequestedAuthority = [] }
        let candidate =
            { Definition = provider
              Source = SourceId.create "src.test"
              Publisher = PublisherId.create "pub.test"
              Package = PackageId.create "pkg.test"
              Provides = [ { Contract = contractId; Version = version } ]
              Generic = false
              Sharing =
                { IsolationCompatible = true
                  LifecycleCompatible = true
                  AuthorityCompatible = true }
              Policy = [ { Domain = Trust; Accepted = true; Reason = "trusted test candidate" } ]
              Evidence = [ EvidenceId.create "ev.test" ]
              Authority = []
              FailureDomain = "failure.test"
              AttachmentNode = None }
        { Request = ResolutionRequestId.create "resolution.integration"
          Generation = GenerationId.create "gen.integration"
          ActiveGeneration = None
          RestartScope = RestartScopeId.create "restart.integration"
          Roots = [ consumer ]
          Definitions = [ consumerDefinition; providerDefinition ]
          Candidates = [ candidate ]
          ExistingOccurrences = []
          OccupiedBindings = []
          Preferences = []
          AuthorisedReplacements = []
          CompositionParameters = []
          ActivationParameters = []
          PreselectedProviders = []
          Ports = []
          TopologyClaims = [] }

    let resolve cardinality =
        request cardinality |> FakeGenerationResolver.resolve

    let memberOf = function
        | ResolutionOutcome.Resolved(_, generation) ->
            generation.ProviderSets |> List.exactlyOne |> fun set -> set.Members |> List.exactlyOne
        | outcome -> failwithf "Expected a resolved generation, got %A." outcome

    let selection (memberValue: ProviderSetMember) =
        { Requirement = requirementId
          Definition = memberValue.Definition
          Occurrence = memberValue.Occurrence
          Component = CoolingFixture.component'
          Provider = CoolingFixture.provider
          HostEndpoint = "minimal-component-host"
          ProviderEndpoint = "cooling-provider"
          RequiredContract = CoolingFixture.contract }

    let prepared () =
        let resolution = resolve (Cardinality.parse "1..1")
        let memberValue = memberOf resolution
        resolution, selection memberValue, memberValue.Occurrence

    let activationMember occurrence =
        { Occurrence = occurrence
          Definition = DefinitionId.create (sprintf "def.%s" (OccurrenceId.value occurrence))
          Region = RegionId.create "region.integration"
          Provides = [ { Contract = contractId; Version = version } ]
          RequiredReadyInputs = []
          AvailableReadyInputs = []
          WaitsForReadyOf = [] }

    let plan occurrences =
        let groupRequest =
            { Request = ActivationGroupRequestId.create "group.integration"
              Generation = GenerationId.create "gen.lifecycle"
              RestartScope = RestartScopeId.create "restart.lifecycle"
              Members = occurrences |> List.map activationMember
              Edges = []
              Protocols = []
              RegionCrossings = [] }
        match FakeActivationGroupPlanner.plan groupRequest with
        | Planned value -> value
        | outcome -> failwithf "Expected a CM3 plan, got %A." outcome

    let runtimeRequest planValue =
        let retained = GenerationId.create "gen.retained"
        { Attempt = ActivationAttemptId.create "activation.integration"
          Plan = planValue
          RequestedRestartScope = planValue.RestartScope
          RetainedGeneration = retained
          ActiveScopes =
            [ { Scope = planValue.RestartScope
                Generation = retained
                Status = RuntimeScopeStatus.ActiveScope } ]
          Preparation = None
          StageOutcomes = []
          InteractionAttempts = []
          BindingExercises = []
          Release =
            { Release = ReleaseId.create "release.integration"
              FailureMoment = ReleaseFailureMoment.NoReleaseFailure }
          Rollback = RollbackAvailability.Available
          RetainedDisposition = RetainedGenerationDisposition.TerminateAfterRelease
          Child = None }

    let directCooling document =
        PortableDirectConversation(
            PortableProviderEndpoint(document, CoolingHandler(), Realization.FixedDirectCall))
        :> IPortableProviderConversation

    let expectProvider name =
        match PortableProviderRef.tryCreate name 1 with
        | Ok value -> value
        | Error error -> failwithf "Expected provider reference, got %A." error

    let admission () : AuthorityAdmissionRequest =
        let relationship =
            { Request = relationshipId
              ProposedActor = participant
              Kind = ActorRelationshipKind.ComponentParticipant
              Evidence = [ authorityEvidence ] }
        let authority =
            { Request = authorityId
              Relationship = relationshipId
              Capability = capability
              Target = authorityTarget
              Operation = operation
              Scope = authorityScope
              Unlimited = false }
        { Request = AdmissionRequestId.create "admission.integration"
          Participant = participant
          EvaluationTime = evaluationTime
          Evidence =
            [ { Evidence = authorityEvidence
                Issuer = authorityIssuer
                Subject = participant
                Verification = AdmissionEvidenceVerification.Verified
                ValidFrom = evaluationTime.AddHours(-1.0)
                ExpiresAt = evaluationTime.AddHours(1.0)
                State = AdmissionEvidenceState.Current } ]
          Relationships = [ relationship ]
          Authority = [ authority ]
          Policy =
            { Policy = AuthorityPolicyId.create "policy.integration"
              TrustedIssuers = [ authorityIssuer ]
              RelationshipRules =
                [ { Rule = PolicyRuleId.create "rule.component-participant"
                    ProposedActor = participant
                    Kind = ActorRelationshipKind.ComponentParticipant
                    Disposition = PolicyDisposition.Allow
                    LocalActor = Some(LocalActorReferenceId.create "local.cooling-provider")
                    RequiredEvidence = [ authorityEvidence ]
                    KnownMistake = false
                    Rationale = "component participant admitted" } ]
              AuthorityRules =
                [ { Rule = PolicyRuleId.create "rule.cooling-control"
                    RelationshipKind = ActorRelationshipKind.ComponentParticipant
                    Capability = capability
                    Target = authorityTarget
                    Operation = operation
                    Scope = authorityScope
                    Disposition = PolicyDisposition.Allow
                    KnownMistake = false
                    Rationale = "narrow cooling control admitted" } ] } }

    [<Test>]
    member _.``completed direct one to one resolution enters portable preflight``() =
        let resolution = resolve (Cardinality.parse "1..1")
        let result =
            memberOf resolution
            |> selection
            |> ComponentBindingIntegration.prepare resolution
        match result with
        | Prepared memberValue ->
            multiple (fun () ->
                Assert.That(CompositionStage.token memberValue.Stage, Is.EqualTo "local-initialisation")
                Assert.That(memberValue.TryPlan, Is.EqualTo(None))
                Assert.That(memberValue.TryFact "bindingScope", Is.EqualTo(Some "scope.cooling"))
                Assert.That(
                    memberValue.TryFact "selectedProvision",
                    Is.EqualTo(Some(PortableProviderRef.text CoolingFixture.provider)))
                Assert.That(resolution.Effects, Is.EqualTo Cm2EffectObservation.none))
        | Refused failure -> Assert.Fail(sprintf "Expected preparation, got %A." failure)

    [<Test>]
    member _.``explicit mapping cannot name a different occurrence``() =
        let resolution = resolve (Cardinality.parse "1..1")
        let mapping =
            { selection (memberOf resolution) with
                Occurrence = OccurrenceId.create "occ.unselected" }
        match ComponentBindingIntegration.prepare resolution mapping with
        | Refused failure ->
            multiple (fun () ->
                Assert.That(
                    failure.Kind,
                    Is.EqualTo ComponentBindingIntegrationFailureKind.SelectionMismatch)
                Assert.That(failure.Code, Is.EqualTo "selection-mismatch"))
        | Prepared _ -> Assert.Fail "A mismatched occurrence reached portable preflight."

    [<Test>]
    member _.``wider provider set is refused instead of narrowed``() =
        let resolution = resolve (Cardinality.parse "1..2")
        match ComponentBindingIntegration.prepare resolution (selection (memberOf resolution)) with
        | Refused failure ->
            multiple (fun () ->
                Assert.That(
                    failure.Kind,
                    Is.EqualTo ComponentBindingIntegrationFailureKind.CardinalityUnsupported)
                Assert.That(failure.Code, Is.EqualTo "cardinality-unsupported"))
        | Prepared _ -> Assert.Fail "A wider Provider Set was narrowed into a portable member."

    [<Test>]
    member _.``refused resolution never reaches portable preflight``() =
        let resolution =
            FakeGenerationResolver.resolve
                { request (Cardinality.parse "1..1") with Candidates = [] }
        let synthetic =
            { Definition = provider
              Occurrence = OccurrenceId.create "occ.synthetic"
              Source = None
              Publisher = PublisherId.create "pub.test"
              Package = None
              Retained = false
              Evidence = []
              Authority = []
              FailureDomain = "failure.synthetic"
              AttachmentNode = None }
        match ComponentBindingIntegration.prepare resolution (selection synthetic) with
        | Refused failure ->
            multiple (fun () ->
                Assert.That(
                    failure.Kind,
                    Is.EqualTo ComponentBindingIntegrationFailureKind.ResolutionNotComplete)
                Assert.That(resolution.Effects, Is.EqualTo Cm2EffectObservation.none))
        | Prepared _ -> Assert.Fail "A refused resolution reached portable preflight."

    [<Test>]
    member _.``missing endpoint designation is refused before portable preflight``() =
        let resolution = resolve (Cardinality.parse "1..1")
        let mapping =
            { selection (memberOf resolution) with ProviderEndpoint = "" }
        match ComponentBindingIntegration.prepare resolution mapping with
        | Refused failure ->
            multiple (fun () ->
                Assert.That(
                    failure.Kind,
                    Is.EqualTo ComponentBindingIntegrationFailureKind.MappingInvalid)
                Assert.That(failure.Code, Is.EqualTo "endpoint-invalid"))
        | Prepared _ -> Assert.Fail "An empty endpoint reached portable preflight."

    [<Test>]
    member _.``singleton lifecycle derives CM4 stages and releases only after Active``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let planValue = plan [ occurrence ]
            let group = planValue.Groups |> List.exactlyOne
            let request =
                { runtimeRequest planValue with
                    StageOutcomes =
                        [ { Group = group.Group
                            Member = occurrence
                            Stage = ActivationStage.LocalInitialisation
                            Succeeded = false
                            Detail = "untrusted caller claim" } ] }
            let! result =
                ComponentBindingLifecycle.activate
                    resolution
                    selected
                    request
                    (directCooling CoolingFixture.contract)

            match result.Member, result.Runtime, result.Failure with
            | Some memberValue, Some runtime, None ->
                multiple (fun () ->
                    Assert.That(runtime.Kind, Is.EqualTo ActivationRuntimeOutcomeKind.Active)
                    Assert.That(runtime.Observation.Effects.Released, Is.True)
                    Assert.That(runtime.Observation.Effects.CapabilityGranted, Is.False)
                    Assert.That(CompositionStage.token memberValue.Stage, Is.EqualTo "released")
                    Assert.That(memberValue.TryPlan.IsSome, Is.True))
            | state -> Assert.Fail(sprintf "Expected Active lifecycle, got %A." state)
        }

    [<Test>]
    member _.``CM4 preflight refusal prevents provider contact``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let request =
                { runtimeRequest (plan [ occurrence ]) with
                    Release =
                        { Release = ReleaseId.create "release.integration"
                          FailureMoment = ReleaseFailureMoment.BeforeCutover } }
            let! result =
                ComponentBindingLifecycle.activate
                    resolution
                    selected
                    request
                    (directCooling CoolingFixture.contract)

            match result.Member, result.Runtime, result.Failure with
            | Some memberValue, Some runtime, Some failure ->
                multiple (fun () ->
                    Assert.That(
                        failure.Kind,
                        Is.EqualTo ComponentBindingLifecycleFailureKind.RuntimeRefusedBeforeStart)
                    Assert.That(
                        runtime.Kind,
                        Is.EqualTo ActivationRuntimeOutcomeKind.ReleaseFailedBeforeCutover)
                    Assert.That(
                        CompositionStage.token memberValue.Stage,
                        Is.EqualTo "local-initialisation")
                    Assert.That(memberValue.TryPlan, Is.EqualTo None))
            | state -> Assert.Fail(sprintf "Expected preflight refusal, got %A." state)
        }

    [<Test>]
    member _.``unsupported activation group is refused before provider contact``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let extra = OccurrenceId.create "occ.extra"
            let! result =
                ComponentBindingLifecycle.activate
                    resolution
                    selected
                    (runtimeRequest (plan [ occurrence; extra ]))
                    (directCooling CoolingFixture.contract)

            match result.Member, result.Runtime, result.Failure with
            | Some memberValue, None, Some failure ->
                multiple (fun () ->
                    Assert.That(
                        failure.Kind,
                        Is.EqualTo ComponentBindingLifecycleFailureKind.PlanUnsupported)
                    Assert.That(
                        CompositionStage.token memberValue.Stage,
                        Is.EqualTo "local-initialisation"))
            | state -> Assert.Fail(sprintf "Expected unsupported-plan refusal, got %A." state)
        }

    [<Test>]
    member _.``activation plan cannot replace the CBI1 selected occurrence``() =
        task {
            let resolution, selected, _ = prepared ()
            let! result =
                ComponentBindingLifecycle.activate
                    resolution
                    selected
                    (runtimeRequest (plan [ OccurrenceId.create "occ.replacement" ]))
                    (directCooling CoolingFixture.contract)

            match result.Member, result.Runtime, result.Failure with
            | Some memberValue, None, Some failure ->
                multiple (fun () ->
                    Assert.That(
                        failure.Kind,
                        Is.EqualTo ComponentBindingLifecycleFailureKind.PlanUnsupported)
                    Assert.That(
                        CompositionStage.token memberValue.Stage,
                        Is.EqualTo "local-initialisation")
                    Assert.That(memberValue.TryPlan, Is.EqualTo None))
            | state -> Assert.Fail(sprintf "Expected selected-occurrence refusal, got %A." state)
        }

    [<Test>]
    member _.``portable interconnection refusal becomes CM4 establishment failure``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let substituted =
                { CoolingFixture.contract with
                    Provider = expectProvider "brontide.fake.substituted" }
            let! result =
                ComponentBindingLifecycle.activate
                    resolution
                    selected
                    (runtimeRequest (plan [ occurrence ]))
                    (directCooling substituted)

            match result.Member, result.Runtime, result.Failure with
            | Some memberValue, Some runtime, Some failure ->
                multiple (fun () ->
                    Assert.That(
                        failure.Kind,
                        Is.EqualTo ComponentBindingLifecycleFailureKind.PortableInterconnectionRefused)
                    Assert.That(
                        runtime.Kind,
                        Is.EqualTo ActivationRuntimeOutcomeKind.EstablishmentFailed)
                    Assert.That(
                        CompositionStage.token memberValue.Stage,
                        Is.Not.EqualTo "released"))
            | state -> Assert.Fail(sprintf "Expected establishment refusal, got %A." state)
        }

    [<Test>]
    member _.``exact CM5 admission gates one released Active member``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence; Participant = participant }
                    (runtimeRequest (plan [ occurrence ]))
                    (admission ())
                    (directCooling CoolingFixture.contract)

            match result.Authority, result.Lifecycle, result.Failure with
            | Some authority, Some lifecycle, None ->
                let memberValue = lifecycle.Member |> Option.get
                let bindingPlan = memberValue.TryPlan |> Option.get
                multiple (fun () ->
                    Assert.That(authority.Kind, Is.EqualTo AuthorityAdmissionOutcomeKind.Admitted)
                    Assert.That(authority.Observation.Relationships, Has.Length.EqualTo 1)
                    Assert.That(authority.Observation.Grants, Has.Length.EqualTo 1)
                    Assert.That(CompositionStage.token memberValue.Stage, Is.EqualTo "released")
                    Assert.That((BindingPlan.authority bindingPlan).NoCapabilityTransfer, Is.True))
            | state -> Assert.Fail(sprintf "Expected authority-gated activation, got %A." state)
        }

    [<Test>]
    member _.``authority mapping mismatch stops before CM5 and portable preflight``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence
                      Participant = ActorId.create "actor.other" }
                    (runtimeRequest (plan [ occurrence ]))
                    (admission ())
                    (directCooling CoolingFixture.contract)

            multiple (fun () ->
                Assert.That(
                    result.Failure.Value.Kind,
                    Is.EqualTo ComponentAuthorityIntegrationFailureKind.MappingInvalid)
                Assert.That(result.Authority, Is.EqualTo None)
                Assert.That(result.Lifecycle, Is.EqualTo None))
        }

    [<Test>]
    member _.``revoked CM5 evidence prevents provider contact``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let denied =
                { admission () with
                    Evidence =
                        (admission ()).Evidence
                        |> List.map (fun evidence ->
                            { evidence with State = AdmissionEvidenceState.Revoked }) }
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence; Participant = participant }
                    (runtimeRequest (plan [ occurrence ]))
                    denied
                    (directCooling CoolingFixture.contract)

            multiple (fun () ->
                Assert.That(
                    result.Failure.Value.Kind,
                    Is.EqualTo ComponentAuthorityIntegrationFailureKind.AuthorityRefused)
                Assert.That(
                    result.Authority.Value.Kind,
                    Is.EqualTo AuthorityAdmissionOutcomeKind.Denied)
                Assert.That(result.Authority.Value.Observation.Grants, Is.Empty)
                Assert.That(result.Lifecycle, Is.EqualTo None))
        }

    [<Test>]
    member _.``additional authority request is refused before CM5 evaluation``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let baseline = admission ()
            let additional =
                { List.exactlyOne baseline.Authority with
                    Request = AuthorityRequestId.create "authority.additional" }
            let wider = { baseline with Authority = baseline.Authority @ [ additional ] }
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence; Participant = participant }
                    (runtimeRequest (plan [ occurrence ]))
                    wider
                    (directCooling CoolingFixture.contract)

            multiple (fun () ->
                Assert.That(
                    result.Failure.Value.Kind,
                    Is.EqualTo ComponentAuthorityIntegrationFailureKind.AuthorityShapeUnsupported)
                Assert.That(result.Authority, Is.EqualTo None)
                Assert.That(result.Lifecycle, Is.EqualTo None))
        }

    [<Test>]
    member _.``caller authored CM4 binding authority is refused before CM5 evaluation``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let runtime =
                { runtimeRequest (plan [ occurrence ]) with
                    BindingExercises =
                        [ { Exercise = BindingExerciseId.create "exercise.caller"
                            Binding = BindingId.create "binding.caller"
                            Consumer = occurrence
                            Provider = occurrence
                            Source = SourceId.create "source.caller"
                            Exposure = BindingExposureKind.Distinct
                            Mediation = None
                            Routing = RoutingDecisionId.create "routing.caller"
                            AuthorityAdmitted = true
                            Delivery = BindingDeliveryResult.Delivered
                            Failure = None } ] }
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence; Participant = participant }
                    runtime
                    (admission ())
                    (directCooling CoolingFixture.contract)

            multiple (fun () ->
                Assert.That(
                    result.Failure.Value.Kind,
                    Is.EqualTo ComponentAuthorityIntegrationFailureKind.AuthorityShapeUnsupported)
                Assert.That(result.Authority, Is.EqualTo None)
                Assert.That(result.Lifecycle, Is.EqualTo None))
        }

    [<Test>]
    member _.``structurally invalid CM5 request prevents provider contact``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let baseline = admission ()
            let invalid =
                { baseline with
                    Evidence =
                        [ List.exactlyOne baseline.Evidence
                          List.exactlyOne baseline.Evidence ] }
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence; Participant = participant }
                    (runtimeRequest (plan [ occurrence ]))
                    invalid
                    (directCooling CoolingFixture.contract)

            multiple (fun () ->
                Assert.That(
                    result.Failure.Value.Kind,
                    Is.EqualTo ComponentAuthorityIntegrationFailureKind.AuthorityRefused)
                Assert.That(
                    result.Authority.Value.Kind,
                    Is.EqualTo AuthorityAdmissionOutcomeKind.InvalidRequest)
                Assert.That(result.Lifecycle, Is.EqualTo None))
        }

    [<Test>]
    member _.``portable failure remains inactive after CM5 admission``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let substituted =
                { CoolingFixture.contract with
                    Provider = expectProvider "brontide.fake.substituted" }
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence; Participant = participant }
                    (runtimeRequest (plan [ occurrence ]))
                    (admission ())
                    (directCooling substituted)

            multiple (fun () ->
                Assert.That(
                    result.Authority.Value.Kind,
                    Is.EqualTo AuthorityAdmissionOutcomeKind.Admitted)
                Assert.That(
                    result.Failure.Value.Kind,
                    Is.EqualTo ComponentAuthorityIntegrationFailureKind.LifecycleRefused)
                Assert.That(
                    CompositionStage.token result.Lifecycle.Value.Member.Value.Stage,
                    Is.Not.EqualTo "released"))
        }

    [<Test>]
    member _.``shared CBI4 vectors pin the complete native profiles``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi4-integrated-comparison-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI4 vector identity must be a string"
                    | value -> value
                let resolution, originalSelection, occurrence = prepared ()
                let selected =
                    { originalSelection with
                        HostEndpoint = "component-comparison-host" }
                let mutable mapping =
                    { Occurrence = occurrence
                      Participant = participant }
                let mutable authority = admission ()
                let mutable document = CoolingFixture.contract
                match scenario with
                | "cbi4-01-active" -> ()
                | "cbi4-02-authority-denied" ->
                    authority <-
                        { authority with
                            Evidence =
                                authority.Evidence
                                |> List.map (fun evidence ->
                                    { evidence with
                                        State = AdmissionEvidenceState.Revoked }) }
                | "cbi4-03-authority-shape" ->
                    authority <-
                        { authority with
                            Authority =
                                authority.Authority
                                @ [ { List.exactlyOne authority.Authority with
                                        Request =
                                            AuthorityRequestId.create "authority.additional" } ] }
                | "cbi4-04-mapping" ->
                    mapping <-
                        { mapping with
                            Participant = ActorId.create "actor.other" }
                | "cbi4-05-lifecycle" ->
                    document <-
                        { document with
                            Provider = expectProvider "brontide.fake.substituted" }
                | other -> invalidArg (nameof scenario) (sprintf "unknown CBI4 vector %s" other)
                let! result =
                    ComponentAuthorityIntegration.activate
                        resolution
                        selected
                        mapping
                        (runtimeRequest (plan [ occurrence ]))
                        authority
                        (directCooling document)
                let profile = ComponentAuthorityComparison.profile scenario result
                let digest = ComponentAuthorityComparison.digest profile
                use parsedProfile = JsonDocument.Parse profile
                multiple (fun () ->
                    Assert.That(
                        digest,
                        Is.EqualTo(vector.GetProperty("expectedProfileSha256").GetString()),
                        scenario)
                    Assert.That(
                        parsedProfile.RootElement.GetProperty("active").GetBoolean(),
                        Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()),
                        scenario)
                    let expectedFailure =
                        let value = vector.GetProperty("expectedIntegrationFailure")
                        if value.ValueKind = JsonValueKind.Null then null else value.GetString()
                    let actualFailure =
                        let value = parsedProfile.RootElement.GetProperty("integrationFailure")
                        if value.ValueKind = JsonValueKind.Null then
                            null
                        else
                            value.GetProperty("kind").GetString()
                    Assert.That(actualFailure, Is.EqualTo(expectedFailure), scenario))
        }
