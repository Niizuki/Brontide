namespace Brontide.Minimal.Host.Tests

open System
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
