namespace Brontide.Minimal.ComponentManagement.Tests

open System
open System.IO
open System.Text.Json
open NUnit.Framework
open Brontide.Minimal.Experimental.ComponentManagement

[<TestFixture>]
type ActivationGroupTests() =
    let peer = ContractId.create "brontide.fake.peer"
    let version = VersionLiteral.create "1.0"
    let first = OccurrenceId.create "occ.first"
    let second = OccurrenceId.create "occ.second"
    let region = RegionId.create "region.root"
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

    let emptyRequest members edges =
        { Request = ActivationGroupRequestId.create "activation.test"
          Generation = GenerationId.create "gen.test"
          RestartScope = RestartScopeId.create "restart.test"
          Members = members
          Edges = edges
          Protocols = []
          RegionCrossings = [] }

    let ordinaryCycle () =
        emptyRequest
            [ memberValue first; memberValue second ]
            [ edge "edge.first-second" first second
              edge "edge.second-first" second first ]

    let protocol (dependency: ActivationDependency) protocolId =
        { Protocol = protocolId
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

    let relationalCycle () =
        let firstEdge =
            { edge "edge.first-second" first second with
                Kind = ActivationDependencyKind.RelationalInitialisation
                Protocol = Some(LifecycleProtocolId.create "protocol.first-second")
                ObservedBeforeRelease = true }
        let secondEdge =
            { edge "edge.second-first" second first with
                Kind = ActivationDependencyKind.RelationalInitialisation
                Protocol = Some(LifecycleProtocolId.create "protocol.second-first")
                ObservedBeforeRelease = true }
        { emptyRequest [ memberValue first; memberValue second ] [ firstEdge; secondEdge ] with
            Protocols =
                [ protocol firstEdge (Option.get firstEdge.Protocol)
                  protocol secondEdge (Option.get secondEdge.Protocol) ] }

    let rec permutations values =
        match values with
        | [] -> [ [] ]
        | _ ->
            [ for index in 0 .. List.length values - 1 do
                  let head = List.item index values
                  let tail =
                      values
                      |> List.indexed
                      |> List.choose (fun (candidateIndex, value) ->
                          if candidateIndex = index then None else Some value)
                  for suffix in permutations tail do
                      yield head :: suffix ]

    [<Test>]
    member _.``Neutral vector inventory is complete and data only``() =
        let path =
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "component-management",
                "fixtures",
                "cm3-activation-group-vectors.json")
        use document = JsonDocument.Parse(File.ReadAllText path)
        let root = document.RootElement
        let ids =
            root.GetProperty("vectors").EnumerateArray()
            |> Seq.map (fun vector -> vector.GetProperty("id").GetString())
            |> Seq.toArray
        multiple (fun () ->
            Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1))
            Assert.That(root.GetProperty("fixture").GetString(), Is.EqualTo("cm3-activation-group-vectors"))
            Assert.That((ids = [| for index in 1 .. 18 -> sprintf "cm3-%02d" index |]), Is.True)
            Assert.That(root.GetRawText(), Does.Not.Contain("algorithm")))

    [<Test>]
    member _.``Ordinary cycle forms one Ready group without startup order or effects``() =
        match FakeActivationGroupPlanner.plan (ordinaryCycle ()) with
        | Planned plan ->
            let group = List.exactlyOne plan.Groups
            let expectedStages =
                [ ActivationStage.LocalInitialisation
                  ActivationStage.Interconnection
                  ActivationStage.Ready ]
            multiple (fun () ->
                Assert.That(group.Cyclic, Is.True)
                Assert.That(
                    ((group.Members |> List.map (fun item -> item.Occurrence)) = [ first; second ]),
                    Is.True)
                Assert.That(group.InternalEdges, Has.Length.EqualTo(2))
                Assert.That(group.Protocols, Is.Empty)
                Assert.That((group.Stages |> List.map (fun item -> item.Stage)) = expectedStages, Is.True)
                Assert.That(group.Stages |> List.forall (fun item -> not item.OrdinaryGateOpen), Is.True)
                Assert.That(group.ReleasePending, Is.True)
                Assert.That(plan.Effects, Is.EqualTo(Cm3EffectObservation.none)))
        | outcome -> Assert.Fail(sprintf "Expected activation-group plan, got %A" outcome)

    [<Test>]
    member _.``Complete bounded relational cycle adds only lifecycle stage``() =
        match FakeActivationGroupPlanner.plan (relationalCycle ()) with
        | Planned plan ->
            let group = List.exactlyOne plan.Groups
            let expectedStages =
                [ ActivationStage.LocalInitialisation
                  ActivationStage.Interconnection
                  ActivationStage.RelationalInitialisationStage
                  ActivationStage.Ready ]
            multiple (fun () ->
                Assert.That(group.Protocols, Has.Length.EqualTo(2))
                Assert.That((group.Stages |> List.map (fun item -> item.Stage)) = expectedStages, Is.True)
                Assert.That(group.Stages |> List.forall (fun item -> not item.OrdinaryGateOpen), Is.True)
                Assert.That(plan.Decisions |> List.filter (fun item -> item.Kind = "relational-protocol"), Has.Length.EqualTo(2)))
        | outcome -> Assert.Fail(sprintf "Expected relational plan, got %A" outcome)

    [<Test>]
    member _.``Descriptor cycle and version conflict fail without partial plan``() =
        let request = ordinaryCycle ()
        let descriptor =
            { request with
                Edges =
                    request.Edges
                    |> List.mapi (fun index item ->
                        if index = 0 then
                            { item with Kind = ActivationDependencyKind.DescriptorExpansion }
                        else item) }
            |> FakeActivationGroupPlanner.plan
        let conflict =
            { request with
                Edges =
                    request.Edges
                    |> List.mapi (fun index item ->
                        if index = 0 then
                            { item with Version = VersionLiteral.create "2.0" }
                        else item) }
            |> FakeActivationGroupPlanner.plan
        multiple (fun () ->
            match descriptor with
            | ActivationGroupRefused failure ->
                Assert.That(failure.Kind, Is.EqualTo(ActivationGroupFailureKind.RecursiveDescriptorExpansion))
            | outcome -> Assert.Fail(sprintf "Expected descriptor refusal, got %A" outcome)
            match conflict with
            | ActivationGroupRefused failure ->
                Assert.That(failure.Kind, Is.EqualTo(ActivationGroupFailureKind.ContractVersionConflict))
                Assert.That(failure.Source, Is.EqualTo(Some first))
                Assert.That(failure.Target, Is.EqualTo(Some second))
                Assert.That(failure.Contract, Is.EqualTo(Some peer))
                Assert.That(failure.Version, Is.EqualTo(Some(VersionLiteral.create "2.0")))
            | outcome -> Assert.Fail(sprintf "Expected version refusal, got %A" outcome)
            Assert.That(descriptor.Effects, Is.EqualTo(Cm3EffectObservation.none)))

    [<Test>]
    member _.``Lifecycle and ordinary gate violations are structured``() =
        let relational = relationalCycle ()
        let missing =
            { relational with
                Edges =
                    relational.Edges
                    |> List.mapi (fun index item ->
                        if index = 0 then { item with Protocol = None } else item) }
            |> FakeActivationGroupPlanner.plan
        let incomplete =
            { relational with
                Protocols =
                    relational.Protocols
                    |> List.mapi (fun index item ->
                        if index = 0 then { item with TimeoutMilliseconds = 0 } else item) }
            |> FakeActivationGroupPlanner.plan
        let ordinary = ordinaryCycle ()
        let early =
            { ordinary with
                Edges =
                    ordinary.Edges
                    |> List.mapi (fun index item ->
                        if index = 0 then { item with ObservedBeforeRelease = true } else item) }
            |> FakeActivationGroupPlanner.plan
        let undeclared =
            { ordinary with
                Edges =
                    ordinary.Edges
                    |> List.mapi (fun index item ->
                        if index = 0 then
                            { item with Protocol = Some(LifecycleProtocolId.create "protocol.unexpected") }
                        else item) }
            |> FakeActivationGroupPlanner.plan
        let oneRelationalEdge = relational.Edges.Head
        let crossGroup =
            { emptyRequest relational.Members [ oneRelationalEdge ] with
                Protocols = [ relational.Protocols.Head ] }
            |> FakeActivationGroupPlanner.plan
        let failureKind outcome =
            match outcome with
            | ActivationGroupRefused failure -> failure.Kind
            | value -> failwithf "Expected refusal, got %A" value
        multiple (fun () ->
            Assert.That(failureKind missing, Is.EqualTo(ActivationGroupFailureKind.LifecycleProtocolRequired))
            Assert.That(failureKind incomplete, Is.EqualTo(ActivationGroupFailureKind.LifecycleProtocolIncomplete))
            Assert.That(failureKind early, Is.EqualTo(ActivationGroupFailureKind.OrdinaryPreReleaseTraffic))
            Assert.That(failureKind undeclared, Is.EqualTo(ActivationGroupFailureKind.UndeclaredLifecycleTraffic))
            Assert.That(failureKind crossGroup, Is.EqualTo(ActivationGroupFailureKind.UndeclaredLifecycleTraffic)))

    [<Test>]
    member _.``Ready requires local inputs and acyclic wait graph``() =
        let request = ordinaryCycle ()
        let missing =
            { request with
                Members =
                    request.Members
                    |> List.mapi (fun index item ->
                        if index = 0 then
                            { item with RequiredReadyInputs = [ LifecycleInputId.create "input.missing" ] }
                        else item) }
            |> FakeActivationGroupPlanner.plan
        let waiting =
            { request with
                Members =
                    [ { request.Members[0] with WaitsForReadyOf = [ second ] }
                      { request.Members[1] with WaitsForReadyOf = [ first ] } ] }
            |> FakeActivationGroupPlanner.plan
        multiple (fun () ->
            match missing with
            | ActivationGroupRefused failure ->
                Assert.That(failure.Kind, Is.EqualTo(ActivationGroupFailureKind.ReadyInputUnavailable))
            | outcome -> Assert.Fail(sprintf "Expected input refusal, got %A" outcome)
            match waiting with
            | ActivationGroupRefused failure ->
                Assert.That(failure.Kind, Is.EqualTo(ActivationGroupFailureKind.CircularReadyWait))
                Assert.That(failure.Member, Is.Not.Null)
            | outcome -> Assert.Fail(sprintf "Expected Ready wait refusal, got %A" outcome))

    [<Test>]
    member _.``Cross Region cycle requires matching Port import export or widening``() =
        let port = PortId.create "port.peer"
        let childRegion = RegionId.create "region.child"
        let ordinary = ordinaryCycle ()
        let request =
            { ordinary with
                Members =
                    [ ordinary.Members[0]
                      { ordinary.Members[1] with Region = childRegion } ]
                Edges =
                    ordinary.Edges
                    |> List.map (fun item -> { item with CrossingPort = Some port }) }
        let crossings =
            request.Edges
            |> List.map (fun item ->
                { Edge = item.Edge
                  FromRegion = if item.From = first then region else childRegion
                  ToRegion = if item.To = second then childRegion else region
                  Port = port
                  ImportDeclared = true
                  ExportDeclared = true })
        let accepted =
            FakeActivationGroupPlanner.plan { request with RegionCrossings = crossings }
        let widened =
            FakeActivationGroupPlanner.plan
                { request with
                    Edges =
                        request.Edges
                        |> List.map (fun item -> { item with AllowWiderRegionProposal = true }) }
        let refused = FakeActivationGroupPlanner.plan request
        let conflict =
            FakeActivationGroupPlanner.plan
                { request with
                    RegionCrossings =
                        crossings
                        |> List.mapi (fun index item ->
                            if index = 0 then { item with ImportDeclared = false } else item) }
        multiple (fun () ->
            match accepted with
            | Planned plan ->
                Assert.That((List.exactlyOne plan.Groups).RegionCrossings, Has.Length.EqualTo(2))
                Assert.That(plan.RegionCrossings, Has.Length.EqualTo(2))
            | outcome -> Assert.Fail(sprintf "Expected cross-Region plan, got %A" outcome)
            match widened with
            | WiderParentGenerationRequired proposal -> Assert.That(proposal.Port, Is.EqualTo(port))
            | outcome -> Assert.Fail(sprintf "Expected widening proposal, got %A" outcome)
            match refused with
            | ActivationGroupRefused failure ->
                Assert.That(failure.Kind, Is.EqualTo(ActivationGroupFailureKind.RegionCrossingRequired))
            | outcome -> Assert.Fail(sprintf "Expected crossing refusal, got %A" outcome)
            match conflict with
            | ActivationGroupRefused failure ->
                Assert.That(failure.Kind, Is.EqualTo(ActivationGroupFailureKind.RegionCrossingConflict))
            | outcome -> Assert.Fail(sprintf "Expected crossing conflict, got %A" outcome))

    [<Test>]
    member _.``Acyclic condensation is dependency first and permutation invariant``() =
        let third = OccurrenceId.create "occ.third"
        let request =
            emptyRequest
                [ memberValue first; memberValue second; memberValue third ]
                [ edge "edge.first-second" first second
                  edge "edge.second-third" second third ]
        let baseline = FakeActivationGroupPlanner.plan request
        for members in permutations request.Members do
            for edges in permutations request.Edges do
                let outcome =
                    FakeActivationGroupPlanner.plan { request with Members = members; Edges = edges }
                Assert.That(outcome, Is.EqualTo(baseline))
        match baseline with
        | Planned plan ->
            let ordered =
                plan.Groups
                |> List.map (fun group -> (List.exactlyOne group.Members).Occurrence)
            Assert.That((ordered = [ third; second; first ]), Is.True)
        | outcome -> Assert.Fail(sprintf "Expected acyclic plan, got %A" outcome)

    [<Test>]
    member _.``Duplicate and missing identities fail closed``() =
        let request = ordinaryCycle ()
        let duplicate =
            FakeActivationGroupPlanner.plan
                { request with Members = request.Members @ [ request.Members.Head ] }
        let missing =
            FakeActivationGroupPlanner.plan
                { request with Members = [ request.Members.Head ] }
        let duplicateProvision =
            FakeActivationGroupPlanner.plan
                { request with
                    Members =
                        request.Members
                        |> List.mapi (fun index item ->
                            if index = 1 then
                                { item with Provides = item.Provides @ item.Provides }
                            else item) }
        let relational = relationalCycle ()
        let duplicateProtocolEdge =
            FakeActivationGroupPlanner.plan
                { relational with
                    Protocols =
                        relational.Protocols
                        @ [ { relational.Protocols.Head with
                                Protocol = LifecycleProtocolId.create "protocol.duplicate-edge" } ] }
        multiple (fun () ->
            match duplicate with
            | ActivationGroupRefused failure ->
                Assert.That(failure.Kind, Is.EqualTo(ActivationGroupFailureKind.ContradictoryIdentity))
            | outcome -> Assert.Fail(sprintf "Expected duplicate refusal, got %A" outcome)
            match missing with
            | ActivationGroupRefused failure ->
                Assert.That(failure.Kind, Is.EqualTo(ActivationGroupFailureKind.MissingMember))
            | outcome -> Assert.Fail(sprintf "Expected missing refusal, got %A" outcome)
            match duplicateProvision with
            | ActivationGroupRefused failure ->
                Assert.That(failure.Kind, Is.EqualTo(ActivationGroupFailureKind.ContractVersionConflict))
            | outcome -> Assert.Fail(sprintf "Expected duplicate provision refusal, got %A" outcome)
            match duplicateProtocolEdge with
            | ActivationGroupRefused failure ->
                Assert.That(failure.Kind, Is.EqualTo(ActivationGroupFailureKind.ContradictoryIdentity))
            | outcome -> Assert.Fail(sprintf "Expected duplicate protocol-edge refusal, got %A" outcome)
            Assert.That(missing.Effects, Is.EqualTo(Cm3EffectObservation.none)))
