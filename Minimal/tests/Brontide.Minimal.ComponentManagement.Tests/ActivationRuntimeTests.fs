namespace Brontide.Minimal.ComponentManagement.Tests

open System
open System.IO
open System.Text.Json
open NUnit.Framework
open Brontide.Minimal.Experimental.ComponentManagement

[<TestFixture>]
type ActivationRuntimeTests() =
    let peer = ContractId.create "brontide.fake.peer"
    let version = VersionLiteral.create "1.0"
    let first = OccurrenceId.create "occ.first"
    let second = OccurrenceId.create "occ.second"
    let region = RegionId.create "region.root"
    let targetScope = RestartScopeId.create "restart.target"
    let otherScope = RestartScopeId.create "restart.other"
    let retained = GenerationId.create "gen.retained"
    let other = GenerationId.create "gen.other"
    let multiple action = Assert.Multiple(Action action)

    let memberValue occurrence =
        { Occurrence = occurrence
          Definition = DefinitionId.create (sprintf "def.%s" (OccurrenceId.value occurrence))
          Region = region
          Provides = [ { Contract = peer; Version = version } ]
          RequiredReadyInputs = []
          AvailableReadyInputs = []
          WaitsForReadyOf = [] }

    let edge identity fromOccurrence toOccurrence =
        { Edge = ActivationEdgeId.create identity
          From = fromOccurrence
          To = toOccurrence
          Kind = ActivationDependencyKind.OrdinaryInteraction
          Contract = peer
          Version = version
          ObservedBeforeRelease = false
          Protocol = None
          CrossingPort = None
          AllowWiderRegionProposal = false }

    let protocol (dependency: ActivationDependency) =
        { Protocol = dependency.Protocol |> Option.get
          Edge = dependency.Edge
          From = dependency.From
          To = dependency.To
          Operation =
            LifecycleOperationId.create
                (sprintf "operation.%s" (ActivationEdgeId.value dependency.Edge))
          Authority = [ CapabilityId.create "authority.lifecycle" ]
          InputShape = ShapeId.create "shape.lifecycle-input"
          OutputShape = ShapeId.create "shape.lifecycle-output"
          Ordering = "concurrent"
          TimeoutMilliseconds = 1000
          RetryLimit = 1
          Idempotent = true
          Completion = "peer-acknowledged"
          Failure = "fail-group"
          Rollback = "discard-provisional-state" }

    let plan relational ordinaryCycle =
        let baseEdges =
            if relational || ordinaryCycle then
                [ edge "edge.first-second" first second
                  edge "edge.second-first" second first ]
            else
                [ edge "edge.first-second" first second ]
        let edges =
            if relational then
                baseEdges
                |> List.map (fun item ->
                    { item with
                        Kind = ActivationDependencyKind.RelationalInitialisation
                        Protocol =
                            Some(
                                LifecycleProtocolId.create
                                    (sprintf "protocol.%s" (ActivationEdgeId.value item.Edge))
                            )
                        ObservedBeforeRelease = true })
            else
                baseEdges
        let groupRequest =
            { Request = ActivationGroupRequestId.create "group.runtime"
              Generation = GenerationId.create "gen.target"
              RestartScope = targetScope
              Members = [ memberValue first; memberValue second ]
              Edges = edges
              Protocols = if relational then edges |> List.map protocol else []
              RegionCrossings = [] }
        match FakeActivationGroupPlanner.plan groupRequest with
        | Planned value -> value
        | outcome -> failwithf "Expected CM3 plan, got %A" outcome

    let request planValue =
        let stages =
            planValue.Groups
            |> List.collect (fun group ->
                group.Stages
                |> List.collect (fun stage ->
                    group.Members
                    |> List.map (fun item ->
                        { Group = group.Group
                          Member = item.Occurrence
                          Stage = stage.Stage
                          Succeeded = true
                          Detail = "completed" })))
        { Attempt = ActivationAttemptId.create "activation.test"
          Plan = planValue
          RequestedRestartScope = targetScope
          RetainedGeneration = retained
          ActiveScopes =
            [ { Scope = targetScope
                Generation = retained
                Status = RuntimeScopeStatus.ActiveScope }
              { Scope = otherScope
                Generation = other
                Status = RuntimeScopeStatus.ActiveScope } ]
          Preparation = None
          StageOutcomes = stages
          InteractionAttempts = []
          BindingExercises = []
          Release =
            { Release = ReleaseId.create "release.test"
              FailureMoment = ReleaseFailureMoment.NoReleaseFailure }
          Rollback = RollbackAvailability.Available
          RetainedDisposition = RetainedGenerationDisposition.TerminateAfterRelease
          Child = None }

    let scope scopeId outcome =
        outcome.Observation.Scopes |> List.find (fun item -> item.Scope = scopeId)

    let exercise
        identity
        exposure
        mediation
        authority
        delivery
        failureValue
        : BindingExerciseDeclaration =
        { Exercise = BindingExerciseId.create identity
          Binding = BindingId.create (sprintf "binding.%s" identity)
          Consumer = first
          Provider = second
          Source = SourceId.create "source.fixture"
          Exposure = exposure
          Mediation = mediation
          Routing = RoutingDecisionId.create (sprintf "route.%s" identity)
          AuthorityAdmitted = authority
          Delivery = delivery
          Failure = failureValue }

    [<Test>]
    member _.``neutral vector inventory is complete and data only``() =
        let path =
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "component-management",
                "fixtures",
                "cm4-activation-runtime-vectors.json"
            )
        use document = JsonDocument.Parse(File.ReadAllText(path))
        let root = document.RootElement
        let ids =
            root.GetProperty("vectors").EnumerateArray()
            |> Seq.map (fun vector -> vector.GetProperty("id").GetString())
            |> Seq.toList
        multiple (fun () ->
            Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1))
            Assert.That(
                root.GetProperty("fixture").GetString(),
                Is.EqualTo("cm4-activation-runtime-vectors")
            )
            Assert.That(ids, Is.EqualTo(box ([ 1..20 ] |> List.map (sprintf "cm4-%02d"))))
            Assert.That(root.GetRawText(), Does.Not.Contain("algorithm")))

    [<Test>]
    member _.``complete establishment releases once and preserves unrelated scope``() =
        let runtimeRequest = request (plan false true)
        let outcome = FakeActivationRuntime.activate runtimeRequest
        multiple (fun () ->
            Assert.That(outcome.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.Active))
            Assert.That(
                outcome.Observation.Events |> List.filter (fun item -> item.Kind = "release"),
                Has.Length.EqualTo(1)
            )
            Assert.That(outcome.Observation.Effects.Released, Is.True)
            Assert.That(outcome.Observation.Effects.CapabilityGranted, Is.False)
            Assert.That(outcome.Observation.Release, Is.EqualTo(runtimeRequest.Release))
            Assert.That(
                outcome.Observation.RetainedDisposition,
                Is.EqualTo(runtimeRequest.RetainedDisposition)
            )
            Assert.That((scope targetScope outcome).Generation, Is.EqualTo(Some runtimeRequest.Plan.Generation))
            Assert.That((scope otherScope outcome).Generation, Is.EqualTo(Some other))
            Assert.That(
                outcome.Observation.Events
                |> List.filter (fun item -> item.Kind = "stage-completed")
                |> List.forall (fun item -> Option.isNone item.Member),
                Is.True
            ))

    [<Test>]
    member _.``preparation failure is effect free and preserves scopes``() =
        let runtimeRequest =
            { request (plan false false) with
                Preparation =
                    Some
                        { Preparation = PreparationId.create "prep.one"
                          Steps = [ PreparationStepKind.ValidateArtifact; PreparationStepKind.WarmCache ]
                          Succeeds = false } }
        let outcome = FakeActivationRuntime.activate runtimeRequest
        multiple (fun () ->
            Assert.That(outcome.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.PreparationFailed))
            Assert.That(outcome.Observation.Effects, Is.EqualTo(ActivationRuntimeEffects.none))
            Assert.That(outcome.Observation.Events |> List.map (fun item -> item.Kind),
                Is.EqualTo(box [ "preparation" ]))
            Assert.That((scope targetScope outcome).Generation, Is.EqualTo(Some retained))
            Assert.That((scope otherScope outcome).Generation, Is.EqualTo(Some other)))

    [<Test>]
    member _.``failed stage is a prefix and prevents release``() =
        let runtimeRequest = request (plan true false)
        let failed =
            runtimeRequest.StageOutcomes
            |> List.map (fun item ->
                if
                    item.Member = second
                    && item.Stage = ActivationStage.RelationalInitialisationStage
                then
                    { item with
                        Succeeded = false
                        Detail = "handshake failed" }
                else
                    item)
        let outcome =
            FakeActivationRuntime.activate { runtimeRequest with StageOutcomes = failed }
        multiple (fun () ->
            Assert.That(outcome.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.EstablishmentFailed))
            Assert.That(outcome.Failure.Value.Member, Is.EqualTo(Some second))
            Assert.That(
                outcome.Observation.Events |> List.exists (fun item -> item.Kind = "release"),
                Is.False
            )
            Assert.That((scope targetScope outcome).Generation, Is.EqualTo(Some retained)))

    [<Test>]
    member _.``gates refuse ordinary pre release and admit exact lifecycle protocol``() =
        let planValue = plan true false
        let group = List.exactlyOne planValue.Groups
        let declaration = List.head group.Protocols
        let lifecycle =
            { Interaction = RuntimeInteractionId.create "interaction.lifecycle"
              Group = group.Group
              From = declaration.From
              To = declaration.To
              Phase = RuntimeInteractionPhase.RelationalInitialisation
              Kind = RuntimeInteractionKind.Lifecycle
              Edge = declaration.Edge
              Operation = Some declaration.Operation
              Capability = Some(List.head declaration.Authority)
              InputShape = Some declaration.InputShape }
        let accepted =
            FakeActivationRuntime.activate
                { request planValue with InteractionAttempts = [ lifecycle ] }
        let ordinary =
            { lifecycle with
                Interaction = RuntimeInteractionId.create "interaction.ordinary"
                Phase = RuntimeInteractionPhase.LocalInitialisation
                Kind = RuntimeInteractionKind.Ordinary
                Operation = None
                Capability = None
                InputShape = None }
        let refused =
            FakeActivationRuntime.activate
                { request planValue with InteractionAttempts = [ ordinary ] }
        let ordinaryPlan = plan false false
        let ordinaryGroup =
            ordinaryPlan.Groups
            |> List.find (fun candidate ->
                candidate.Members |> List.exists (fun item -> item.Occurrence = first))
        let activeOrdinary =
            { ordinary with
                Interaction = RuntimeInteractionId.create "interaction.active"
                Group = ordinaryGroup.Group
                Phase = RuntimeInteractionPhase.Active
                Edge = ActivationEdgeId.create "edge.first-second"
                From = first
                To = second }
        let active =
            FakeActivationRuntime.activate
                { request ordinaryPlan with InteractionAttempts = [ activeOrdinary ] }
        let undeclared =
            FakeActivationRuntime.activate
                { request ordinaryPlan with
                    InteractionAttempts =
                        [ { activeOrdinary with
                              Edge = ActivationEdgeId.create "edge.undeclared" } ] }
        multiple (fun () ->
            Assert.That(accepted.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.Active))
            Assert.That(accepted.Observation.Effects.LifecycleOperationExecuted, Is.True)
            Assert.That((List.exactlyOne accepted.Observation.Interactions).Admitted, Is.True)
            Assert.That(refused.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.InteractionRefused))
            Assert.That((List.exactlyOne refused.Observation.Interactions).Admitted, Is.False)
            Assert.That(active.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.Active))
            Assert.That(active.Observation.Effects.OrdinaryInteractionAdmitted, Is.True)
            Assert.That(undeclared.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.InteractionRefused)))

    [<Test>]
    member _.``binding observations retain provenance and release failure follows rollback policy``() =
        let runtimeRequest = request (plan false false)
        let bindings: BindingExerciseDeclaration list =
            [ exercise
                  "exercise.delivered"
                  BindingExposureKind.Distinct
                  None
                  true
                  BindingDeliveryResult.Delivered
                  None
              exercise
                  "exercise.failed"
                  BindingExposureKind.Mediated
                  (Some(MediationId.create "mediation.runtime"))
                  true
                  BindingDeliveryResult.Failed
                  (Some "provider unavailable") ]
        let active =
            FakeActivationRuntime.activate
                { runtimeRequest with BindingExercises = bindings }
        let before =
            FakeActivationRuntime.activate
                { runtimeRequest with
                    Release =
                        { runtimeRequest.Release with
                            FailureMoment = ReleaseFailureMoment.BeforeCutover } }
        let rolledBack =
            FakeActivationRuntime.activate
                { runtimeRequest with
                    Release =
                        { runtimeRequest.Release with
                            FailureMoment = ReleaseFailureMoment.AfterCutover }
                    Rollback = RollbackAvailability.Available }
        let unavailable =
            FakeActivationRuntime.activate
                { runtimeRequest with
                    Release =
                        { runtimeRequest.Release with
                            FailureMoment = ReleaseFailureMoment.AfterCutover }
                    Rollback = RollbackAvailability.Unavailable }
        let corrupted =
            FakeActivationRuntime.activate
                { runtimeRequest with
                    Release =
                        { runtimeRequest.Release with
                            FailureMoment = ReleaseFailureMoment.AfterCutover }
                    Rollback = RollbackAvailability.RetainedGenerationCorrupted }
        multiple (fun () ->
            Assert.That(active.Observation.BindingExercises, Has.Length.EqualTo(2))
            Assert.That(active.Observation.Effects.OrdinaryInteractionAdmitted, Is.True)
            Assert.That(before.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.ReleaseFailedBeforeCutover))
            Assert.That((scope targetScope before).Generation, Is.EqualTo(Some retained))
            Assert.That(rolledBack.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.RolledBack))
            Assert.That((scope targetScope rolledBack).Generation, Is.EqualTo(Some retained))
            Assert.That(unavailable.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.RollbackUnavailable))
            Assert.That((scope targetScope unavailable).Status, Is.EqualTo(RuntimeScopeStatus.DegradedScope))
            Assert.That(
                corrupted.Kind,
                Is.EqualTo(ActivationRuntimeOutcomeKind.RetainedGenerationCorrupted)
            )
            Assert.That(corrupted.Observation.Effects.RollbackAttempted, Is.True)
            Assert.That((scope otherScope corrupted).Generation, Is.EqualTo(Some other)))

    [<Test>]
    member _.``child activation enforces Port and host assisted ordering``() =
        let runtimeRequest = request (plan false false)
        let child =
            { ParentScope = otherScope
              ParentGeneration = other
              Port = PortId.create "port.child"
              RuntimeOpen = true
              Occupied = false
              ReplacementLifecycleDeclared = false
              HostAssisted = true
              InternalReleaseSequence = 1
              ExportReleaseSequence = 2
              OuterHostOwnsAdmission = false }
        let accepted =
            FakeActivationRuntime.activate { runtimeRequest with Child = Some child }
        let closed =
            FakeActivationRuntime.activate
                { runtimeRequest with Child = Some { child with RuntimeOpen = false } }
        let replacement =
            FakeActivationRuntime.activate
                { runtimeRequest with Child = Some { child with Occupied = true } }
        let order =
            FakeActivationRuntime.activate
                { runtimeRequest with
                    Child = Some { child with ExportReleaseSequence = 1 } }
        multiple (fun () ->
            Assert.That(accepted.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.Active))
            Assert.That((scope otherScope accepted).Generation, Is.EqualTo(Some other))
            Assert.That(closed.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.ChildPortClosed))
            Assert.That(
                replacement.Kind,
                Is.EqualTo(ActivationRuntimeOutcomeKind.ReplacementLifecycleRequired)
            )
            Assert.That(order.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.HostAssistedOrderConflict)))

    [<Test>]
    member _.``scope stage binding and permutation properties fail closed or remain deterministic``() =
        let runtimeRequest = request (plan false true)
        let scopeConflict =
            FakeActivationRuntime.activate
                { runtimeRequest with RequestedRestartScope = otherScope }
        let sameGeneration =
            FakeActivationRuntime.activate
                { runtimeRequest with
                    RetainedGeneration = runtimeRequest.Plan.Generation
                    ActiveScopes =
                        runtimeRequest.ActiveScopes
                        |> List.map (fun item ->
                            if item.Scope = targetScope then
                                { item with Generation = runtimeRequest.Plan.Generation }
                            else
                                item) }
        let stageConflict =
            FakeActivationRuntime.activate
                { runtimeRequest with StageOutcomes = List.tail runtimeRequest.StageOutcomes }
        let badBinding =
            exercise
                "exercise.denied"
                BindingExposureKind.Distinct
                None
                false
                BindingDeliveryResult.Delivered
                None
        let bindingConflict =
            FakeActivationRuntime.activate
                { runtimeRequest with BindingExercises = [ badBinding ] }
        let forward = FakeActivationRuntime.activate runtimeRequest
        let reverse =
            FakeActivationRuntime.activate
                { runtimeRequest with
                    ActiveScopes = List.rev runtimeRequest.ActiveScopes
                    StageOutcomes = List.rev runtimeRequest.StageOutcomes }
        multiple (fun () ->
            Assert.That(scopeConflict.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.RestartScopeConflict))
            Assert.That(sameGeneration.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.RestartScopeConflict))
            Assert.That(stageConflict.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.StageObservationConflict))
            Assert.That(
                bindingConflict.Kind,
                Is.EqualTo(ActivationRuntimeOutcomeKind.BindingObservationConflict)
            )
            Assert.That(reverse, Is.EqualTo(forward)))
