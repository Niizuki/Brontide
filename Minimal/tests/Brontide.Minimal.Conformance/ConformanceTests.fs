namespace Brontide.Minimal.Conformance

open System
open Microsoft.FSharp.Reflection
open NUnit.Framework
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel

module private Helpers =
    let name value = CanonicalName.create value

    let get = function
        | Ok value -> value
        | Error message -> failwith message

    let timeDomain = TimeDomainReference.create (name "Brontide.Minimal.Tests:LogicalTime")

    let mark milliseconds =
        { Milliseconds = milliseconds
          TimeDomain = timeDomain
          UncertaintyMilliseconds = None }

    let echoOperation: OperationReference =
        { Name = name "Brontide.Minimal.Tests:Echo" }

    let otherOperation: OperationReference =
        { Name = name "Brontide.Minimal.Tests:Other" }

    let echoedEvent: EventReference =
        { Name = name "Brontide.Minimal.Tests:Echoed" }

    type Fixture =
        { Holder: Actor
          Stranger: Actor
          Target: Actor
          OtherTarget: Actor
          Capability: Capability
          Constraint: ConstraintDefinition
          World: World }

    let prepareWorld () =
        let initial = World.create (Guid.NewGuid()) timeDomain

        let constraintDefinition, withConstraint =
            World.registerConstraint
                (name "Brontide.Minimal.Tests:PermitEcho")
                BuiltIn.textShape
                "Allows the requested Operation only when the evaluator accepts it."
                initial
            |> get

        let fixture, ready =
            World.genesis
                (name "Brontide.Minimal.Tests:BootstrapPolicy")
                (mark 0L)
                (fun genesis world ->
                    let holder, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:Holder") world
                    let stranger, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:Stranger") world
                    let target, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:Target") world
                    let otherTarget, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:OtherTarget") world

                    let echo: OperationDefinition =
                        { Reference = echoOperation
                          Description = "Echo a text value."
                          Target = target.Reference
                          CommandShape = BuiltIn.textShape
                          ResultShape = BuiltIn.textShape
                          Constraints = [] }

                    let other: OperationDefinition =
                        { Reference = otherOperation
                          Description = "A second Operation used for fail-closed tests."
                          Target = target.Reference
                          CommandShape = BuiltIn.textShape
                          ResultShape = BuiltIn.textShape
                          Constraints = [] }

                    let world = World.registerOperation echo world |> get
                    let world = World.registerOperation other world |> get

                    let world =
                        World.registerEvent
                            { Reference = echoedEvent
                              Description = "A target-attributed echo assertion."
                              AssertionShape = BuiltIn.textShape }
                            world
                        |> get

                    let capability, world =
                        Genesis.capability
                            genesis
                            (name "Brontide.Minimal.Tests:EchoGrant")
                            holder.Reference
                            target.Reference
                            (Set.singleton echoOperation)
                            []
                            true
                            world
                        |> get

                    ({ Holder = holder
                       Stranger = stranger
                       Target = target
                       OtherTarget = otherTarget
                       Capability = capability
                       Constraint = constraintDefinition
                       World = world },
                     world))
                withConstraint
            |> get

        { fixture with World = ready }

    let request _fixture initiator target capability operation command =
        { Initiator = initiator
          Target = target
          PresentedCapability = capability
          Operation = operation
          Command = command
          Occurrence = None
          Context = Map.empty }

    let environment milliseconds evaluators handler =
        { TrustedTime = mark milliseconds
          ConstraintEvaluators = evaluators
          Handlers = Map.ofList [ echoOperation, handler; otherOperation, handler ] }

open Helpers

[<TestFixture>]
type BaseAuthorityConformance() =
    [<Test>]
    member _.``BR_07_CONSTRAINT_002 poisoned Capability denies before effects with redacted diagnostics`` () =
        let fixture = prepareWorld ()
        let unsupported, withUnsupported =
            World.registerConstraint
                (name "Brontide.Minimal.Tests:UnsupportedComposite")
                BuiltIn.textShape
                "registered but not understood by the target evaluator"
                fixture.World
            |> get

        let composite =
            AnyOf
                [ AtomicConstraint
                    { Constraint = fixture.Constraint.Reference
                      Parameters = TextValue "known" }
                  Not(
                      AtomicConstraint
                          { Constraint = unsupported.Reference
                            Parameters = TextValue "protected-secret" }
                  ) ]

        let (capability: Capability), ready =
            World.genesis
                (name "Brontide.Minimal.Tests:CompositePolicy")
                (mark 1L)
                (fun genesis world ->
                    let capability, next =
                        Genesis.capabilityWithExpressions
                            genesis
                            (name "Brontide.Minimal.Tests:CompositeGrant")
                            fixture.Holder.Reference
                            fixture.Target.Reference
                            (Set.singleton echoOperation)
                            [ composite ]
                            false
                            world
                        |> get

                    capability, next)
                withUnsupported
            |> get

        let mutable invoked = false
        let evaluators =
            Map.ofList
                [ fixture.Constraint.Reference,
                  fun _ _ -> Ok() ]
        let execution =
            World.step
                (environment 2L evaluators (fun value -> invoked <- true; Ok(value.Command, [], [])))
                ready
                (request
                    fixture
                    fixture.Holder.Reference
                    fixture.Target.Reference
                    capability.Reference
                    echoOperation
                    (TextValue "command"))

        Assert.That(execution.Outcome.Status, Is.EqualTo Denied)
        Assert.That(invoked, Is.False)
        Assert.That(execution.Outcome.Reason.Value, Does.Contain "UnsupportedConstraint")
        Assert.That(execution.Outcome.Reason.Value, Does.Contain(CanonicalName.value unsupported.Name))
        Assert.That(execution.Outcome.Reason.Value, Does.Not.Contain "protected-secret")

    [<Test>]
    member _.``BR_05_NAME_001 canonical names parse authored qualification and reject ambiguity`` () =
        Assert.That(CanonicalName.tryCreate "Logitech.MX:Input.Scroll.SmartShift" |> Result.isOk, Is.True)
        Assert.That(CanonicalName.tryCreate "Brontide.Minimal.Tests:Good_Name-1" |> Result.isOk, Is.True)
        Assert.That(CanonicalName.tryCreate ".bad" |> Result.isError, Is.True)
        Assert.That(CanonicalName.tryCreate "authority::concept" |> Result.isError, Is.True)
        Assert.That(CanonicalName.tryCreate "authority:concept:extra" |> Result.isError, Is.True)

    [<Test>]
    member _.``BR_07_NAME_001 typed members round trip without becoming concept names`` () =
        let cases =
            [ "Brontide:Editor.Project#Store.Core", "Brontide:Editor.Project", "Store", "Core"
              "Brontide:Editor.Project#Parameter.HistoryDepth",
              "Brontide:Editor.Project",
              "Parameter",
              "HistoryDepth"
              "Example.Project#ExperimentalKind.Member_1",
              "Example.Project",
              "ExperimentalKind",
              "Member_1" ]

        for text, owner, kind, memberName in cases do
            let parsed = CanonicalMemberName.create text

            Assert.Multiple(Action(fun () ->
                Assert.That(parsed |> CanonicalMemberName.owner |> CanonicalName.value, Is.EqualTo owner)
                Assert.That(parsed |> CanonicalMemberName.kind |> MemberKind.value, Is.EqualTo kind)
                Assert.That(parsed |> CanonicalMemberName.name |> MemberName.value, Is.EqualTo memberName)
                Assert.That(CanonicalMemberName.value parsed, Is.EqualTo text)
                Assert.That(CanonicalMemberName.create text, Is.EqualTo parsed)
                Assert.That(CanonicalName.tryCreate text |> Result.isError, Is.True)))

    [<Test>]
    member _.``typed member parser rejects ambiguous and versioned forms`` () =
        [ ""
          " Brontide:Editor.Project#Store.Core"
          "Brontide:Editor.Project#Store.Core "
          "#Store.Core"
          "Brontide:Editor.Project#"
          "Brontide:Editor.Project#Store"
          "Brontide:Editor.Project#.Core"
          "Brontide:Editor.Project#Store."
          "Brontide:Editor.Project#Store.Core.More"
          "Brontide:Editor.Project##Store.Core"
          "Brontide::Editor.Project#Store.Core"
          "Brontide:Editor.Project#Store.Core@3" ]
        |> List.iter (fun text ->
            Assert.That(
                CanonicalMemberName.tryCreate text |> Result.isError,
                Is.True,
                $"Expected '{text}' to be rejected."
            ))

    [<Test>]
    member _.``member tokens are validated open types and comparison is ordinal`` () =
        let lower = CanonicalMemberName.create "Example:Definition#FutureKind.A"
        let upper = CanonicalMemberName.create "Example:Definition#FutureKind.B"

        Assert.Multiple(Action(fun () ->
            Assert.That(MemberKind.tryCreate "FutureKind" |> Result.isOk, Is.True)
            Assert.That(MemberKind.tryCreate "Future.Kind" |> Result.isError, Is.True)
            Assert.That(MemberName.tryCreate "Member-1" |> Result.isOk, Is.True)
            Assert.That(MemberName.tryCreate "Member.Name" |> Result.isError, Is.True)
            Assert.That(compare lower upper, Is.LessThan 0)))

    [<Test>]
    member _.``BR_05_NAME_002 only Shapes and Fragments carry structural versions`` () =
        let operationFields = FSharpType.GetRecordFields typeof<OperationReference> |> Array.map _.Name
        let eventFields = FSharpType.GetRecordFields typeof<EventReference> |> Array.map _.Name
        let shapeFields = FSharpType.GetRecordFields typeof<ShapeReference> |> Array.map _.Name

        Assert.That((operationFields = [| "Name" |]), Is.True)
        Assert.That((eventFields = [| "Name" |]), Is.True)
        Assert.That(shapeFields, Does.Contain "Version")

    [<Test>]
    member _.``BR_05_AUTH_001 foreign issued references cannot forge local authority`` () =
        let fixture = prepareWorld ()
        let foreign = prepareWorld ()
        let mutable invoked = false

        let execution =
            World.step
                (environment 1L Map.empty (fun value -> invoked <- true; Ok(value.Command, [], [])))
                fixture.World
                (request
                    fixture
                    fixture.Holder.Reference
                    fixture.Target.Reference
                    foreign.Capability.Reference
                    echoOperation
                    (TextValue "forged"))

        Assert.That(execution.Outcome.Status, Is.EqualTo Denied)
        Assert.That(execution.Outcome.Reason.Value, Does.Contain "not issued")
        Assert.That(invoked, Is.False)

    [<Test>]
    member _.``BR_05_AUTH_002 wrong holder is denied before effects and recorded without payload`` () =
        let fixture = prepareWorld ()
        let mutable invoked = false

        let execution =
            World.step
                (environment 1L Map.empty (fun value -> invoked <- true; Ok(value.Command, [], [])))
                fixture.World
                (request
                    fixture
                    fixture.Stranger.Reference
                    fixture.Target.Reference
                    fixture.Capability.Reference
                    echoOperation
                    (TextValue "protected-payload"))

        let audit = World.executions execution.World |> List.exactlyOne
        let auditFields = FSharpType.GetRecordFields typeof<ExecutionAudit> |> Array.map _.Name

        Assert.That(execution.Outcome.Status, Is.EqualTo Denied)
        Assert.That(execution.Outcome.Reason.Value, Does.Contain "initiating Actor")
        Assert.That(invoked, Is.False)
        Assert.That(audit.Status, Is.EqualTo Denied)
        Assert.That(auditFields, Does.Not.Contain "Command")
        Assert.That(auditFields, Does.Not.Contain "Payload")

    [<Test>]
    member _.``BR_05_AUTH_003 wrong target is denied before effects`` () =
        let fixture = prepareWorld ()
        let mutable invoked = false

        let execution =
            World.step
                (environment 1L Map.empty (fun value -> invoked <- true; Ok(value.Command, [], [])))
                fixture.World
                (request
                    fixture
                    fixture.Holder.Reference
                    fixture.OtherTarget.Reference
                    fixture.Capability.Reference
                    echoOperation
                    (TextValue "wrong target"))

        Assert.That(execution.Outcome.Status, Is.EqualTo Denied)
        Assert.That(invoked, Is.False)

    [<Test>]
    member _.``BR_05_AUTH_004 wrong Operation is denied before effects`` () =
        let fixture = prepareWorld ()
        let mutable invoked = false

        let execution =
            World.step
                (environment 1L Map.empty (fun value -> invoked <- true; Ok(value.Command, [], [])))
                fixture.World
                (request
                    fixture
                    fixture.Holder.Reference
                    fixture.Target.Reference
                    fixture.Capability.Reference
                    otherOperation
                    (TextValue "wrong operation"))

        Assert.That(execution.Outcome.Status, Is.EqualTo Denied)
        Assert.That(execution.Outcome.Reason.Value, Does.Contain "does not authorize")
        Assert.That(invoked, Is.False)

    [<Test>]
    member _.``BR_05_AUTH_005 failed Capability Constraint denies before effects`` () =
        let fixture = prepareWorld ()

        let child, delegatedWorld =
            World.delegateCapability
                (name "Brontide.Minimal.Tests:ConstrainedGrant")
                fixture.Holder.Reference
                fixture.Stranger.Reference
                fixture.Capability.Reference
                [ { Constraint = fixture.Constraint.Reference
                    Parameters = TextValue "echo-only" } ]
                fixture.World
            |> get

        let mutable invoked = false
        let evaluators =
            Map.ofList [ fixture.Constraint.Reference, fun _ _ -> Error "The Capability Constraint rejected the request." ]

        let execution =
            World.step
                (environment 1L evaluators (fun value -> invoked <- true; Ok(value.Command, [], [])))
                delegatedWorld
                (request
                    fixture
                    fixture.Stranger.Reference
                    fixture.Target.Reference
                    child.Reference
                    echoOperation
                    (TextValue "constrained"))

        Assert.That(execution.Outcome.Status, Is.EqualTo Denied)
        Assert.That(execution.Outcome.Reason.Value, Does.Contain "Constraint rejected")
        Assert.That(invoked, Is.False)

    [<Test>]
    member _.``BR_05_DELEGATION_001 Delegation preserves scope and appends auditable provenance`` () =
        let fixture = prepareWorld ()

        let child, delegatedWorld =
            World.delegateCapability
                (name "Brontide.Minimal.Tests:DerivedGrant")
                fixture.Holder.Reference
                fixture.Stranger.Reference
                fixture.Capability.Reference
                [ { Constraint = fixture.Constraint.Reference
                    Parameters = TextValue "echo-only" } ]
                fixture.World
            |> get

        let wideningAttempt =
            World.step
                (environment 1L Map.empty (fun value -> Ok(value.Command, [], [])))
                delegatedWorld
                (request
                    fixture
                    fixture.Stranger.Reference
                    fixture.Target.Reference
                    child.Reference
                    otherOperation
                    (TextValue "widen"))

        Assert.That(child.Parent, Is.EqualTo(Some fixture.Capability.Reference))
        Assert.That(child.IssuedBy, Is.EqualTo(Some fixture.Holder.Reference))
        Assert.That(child.Holder, Is.EqualTo fixture.Stranger.Reference)
        Assert.That(child.Target, Is.EqualTo fixture.Capability.Target)
        Assert.That(child.Operations = fixture.Capability.Operations, Is.True)
        Assert.That(List.length child.AddedConstraints, Is.EqualTo 1)
        Assert.That(wideningAttempt.Outcome.Status, Is.EqualTo Denied)

    [<Test>]
    member _.``BR_05_EXEC_001 explicit issue delegate execute and Event attribution path succeeds`` () =
        let fixture = prepareWorld ()

        let child, delegatedWorld =
            World.delegateCapability
                (name "Brontide.Minimal.Tests:ExecutableDerivedGrant")
                fixture.Holder.Reference
                fixture.Stranger.Reference
                fixture.Capability.Reference
                [ { Constraint = fixture.Constraint.Reference
                    Parameters = TextValue "echo-only" } ]
                fixture.World
            |> get

        let evaluators = Map.ofList [ fixture.Constraint.Reference, fun _ _ -> Ok() ]

        let execution =
            World.step
                (environment
                    1L
                    evaluators
                    (fun value ->
                        Ok(
                            value.Command,
                            [ { Reference = echoedEvent
                                Emitter = value.Target
                                Payload = value.Command
                                OccurredAt = Some(mark 1L) } ],
                            [ name "Brontide.Minimal.Tests:EchoedBy", "target" ]
                        )))
                delegatedWorld
                (request
                    fixture
                    fixture.Stranger.Reference
                    fixture.Target.Reference
                    child.Reference
                    echoOperation
                    (TextValue "hello"))

        let emitted = execution.EmittedEvents |> List.find (fun item -> item.Reference = echoedEvent)
        Assert.That(execution.Outcome.Status, Is.EqualTo Succeeded)
        Assert.That(emitted.Emitter, Is.EqualTo fixture.Target.Reference)
        Assert.That(emitted.CausedBy, Is.EqualTo execution.Outcome.Execution)
        Assert.That(emitted.EmittedAt, Is.EqualTo(mark 1L))
        Assert.That(World.events execution.World |> List.length, Is.EqualTo 2)
        Assert.That(World.executions execution.World |> List.length, Is.EqualTo 1)

    [<Test>]
    member _.``BR_05_OUTCOME_001 Outcome composes Event and keeps failure details separate from results`` () =
        let fixture = prepareWorld ()
        let failureDetails = TextValue "E_TEST"

        let execution =
            World.step
                (environment
                    1L
                    Map.empty
                    (fun _ ->
                        Error(
                            OperationFailure.withDetails
                                BuiltIn.textShape
                                failureDetails
                                "expected failure"
                        )))
                fixture.World
                (request
                    fixture
                    fixture.Holder.Reference
                    fixture.Target.Reference
                    fixture.Capability.Reference
                    echoOperation
                    (TextValue "fail"))

        Assert.That(execution.Outcome.Status, Is.EqualTo Failed)
        Assert.That(execution.Outcome.TerminalFor, Is.EqualTo execution.Outcome.Execution)
        Assert.That(execution.Outcome.Event.Reference, Is.EqualTo BuiltIn.executionOutcomeEvent)
        Assert.That(execution.Outcome.Event.Emitter, Is.EqualTo fixture.Target.Reference)
        Assert.That(execution.Outcome.Result.IsNone, Is.True)
        Assert.That(execution.Outcome.DetailsShape, Is.EqualTo(Some BuiltIn.textShape))
        Assert.That(execution.Outcome.Details, Is.EqualTo(Some failureDetails))
        Assert.That(execution.Outcome.Event.Payload, Is.EqualTo failureDetails)

    [<Test>]
    member _.``BR_05_EXEC_002 the same explicit request and environment produce the same transition`` () =
        let fixture = prepareWorld ()
        let executionRequest =
            request
                fixture
                fixture.Holder.Reference
                fixture.Target.Reference
                fixture.Capability.Reference
                echoOperation
                (TextValue "deterministic")

        let executionEnvironment =
            environment 1L Map.empty (fun value -> Ok(value.Command, [], []))

        let first = World.step executionEnvironment fixture.World executionRequest
        let second = World.step executionEnvironment fixture.World executionRequest
        let different =
            World.step
                executionEnvironment
                fixture.World
                { executionRequest with Command = TextValue "different branch" }

        Assert.That(first.Outcome.Execution, Is.EqualTo second.Outcome.Execution)
        Assert.That(first.Outcome, Is.EqualTo second.Outcome)
        Assert.That(World.executions first.World = World.executions second.World, Is.True)
        Assert.That(first.Outcome.Execution, Is.Not.EqualTo different.Outcome.Execution)

    [<Test>]
    member _.``BR_05_GENESIS_001 Genesis is enumerable attributable and cannot be reused`` () =
        let initial = World.create (Guid.NewGuid()) timeDomain
        let mutable captured = Unchecked.defaultof<GenesisContext>

        let actor, ready =
            World.genesis
                (name "Brontide.Minimal.Tests:AttachmentPolicy")
                (mark 5L)
                (fun genesis world ->
                    captured <- genesis
                    Genesis.actor genesis (name "Brontide.Minimal.Tests:AttachedActor") world)
                initial
            |> get

        let occurrence = World.genesisOccurrences ready |> List.exactlyOne

        Assert.That(occurrence.Policy, Is.EqualTo(name "Brontide.Minimal.Tests:AttachmentPolicy"))
        Assert.That(occurrence.IntroducedActors = [ actor.Reference ], Is.True)
        Assert.Throws<InvalidOperationException>(
            Action(fun () -> Genesis.actor captured (name "Brontide.Minimal.Tests:LateActor") ready |> ignore)
        )
        |> ignore

    [<Test>]
    member _.``BR_05_GENESIS_001 failed Genesis does not recycle escaped authority references`` () =
        let fixture = prepareWorld ()
        let mutable escapedActor = Unchecked.defaultof<ActorReference>
        let mutable escapedCapability = Unchecked.defaultof<CapabilityReference>
        let mutable runtimeStatus = Unchecked.defaultof<ExecutionStatus>
        let mutable nestedGenesisBlocked = false
        let mutable invoked = false

        Assert.Throws<InvalidOperationException>(
            Action(fun () ->
                World.genesis
                    (name "Brontide.Minimal.Tests:FailedPolicy")
                    (mark 1L)
                    (fun genesis world ->
                        let provisional, world =
                            Genesis.actor genesis (name "Brontide.Minimal.Tests:Provisional") world

                        let provisionalCapability, world =
                            Genesis.capability
                                genesis
                                (name "Brontide.Minimal.Tests:ProvisionalGrant")
                                fixture.Holder.Reference
                                fixture.Target.Reference
                                (Set.singleton echoOperation)
                                []
                                false
                                world
                            |> get

                        escapedActor <- provisional.Reference
                        escapedCapability <- provisionalCapability.Reference

                        runtimeStatus <-
                            World.step
                                (environment 1L Map.empty (fun value ->
                                    invoked <- true
                                    Ok(value.Command, [], [])))
                                world
                                (request
                                    fixture
                                    fixture.Holder.Reference
                                    fixture.Target.Reference
                                    fixture.Capability.Reference
                                    echoOperation
                                    (TextValue "must not execute in Genesis"))
                            |> _.Outcome.Status

                        nestedGenesisBlocked <-
                            World.genesis
                                (name "Brontide.Minimal.Tests:NestedPolicy")
                                (mark 1L)
                                (fun _ nestedWorld -> (), nestedWorld)
                                world
                            |> Result.isError

                        invalidOp "rollback")
                    fixture.World
                |> ignore)
        )
        |> ignore

        let (replacement: Actor, replacementCapability: Capability), ready =
            World.genesis
                (name "Brontide.Minimal.Tests:ReplacementPolicy")
                (mark 1L)
                (fun genesis world ->
                    let replacement, world =
                        Genesis.actor genesis (name "Brontide.Minimal.Tests:Replacement") world

                    let capability, world =
                        Genesis.capability
                            genesis
                            (name "Brontide.Minimal.Tests:ReplacementGrant")
                            replacement.Reference
                            fixture.Target.Reference
                            (Set.singleton echoOperation)
                            []
                            false
                            world
                        |> get

                    ((replacement, capability), world))
                fixture.World
            |> get

        let escapedActorResult =
            World.step
                (environment 2L Map.empty (fun value ->
                    invoked <- true
                    Ok(value.Command, [], [])))
                ready
                (request
                    fixture
                    escapedActor
                    fixture.Target.Reference
                    replacementCapability.Reference
                    echoOperation
                    (TextValue "must not execute"))

        let escapedCapabilityResult =
            World.step
                (environment 2L Map.empty (fun value ->
                    invoked <- true
                    Ok(value.Command, [], [])))
                ready
                (request
                    fixture
                    fixture.Holder.Reference
                    fixture.Target.Reference
                    escapedCapability
                    echoOperation
                    (TextValue "must not execute"))

        Assert.Multiple(Action(fun () ->
            Assert.That(escapedActor, Is.Not.EqualTo replacement.Reference)
            Assert.That(escapedCapability, Is.Not.EqualTo replacementCapability.Reference)
            Assert.That(runtimeStatus, Is.EqualTo Denied)
            Assert.That(nestedGenesisBlocked, Is.True)
            Assert.That(escapedActorResult.Outcome.Status, Is.EqualTo Denied)
            Assert.That(escapedCapabilityResult.Outcome.Status, Is.EqualTo Denied)
            Assert.That(invoked, Is.False)))

    [<Test>]
    member _.``BR_05_GENESIS_001 discarded persistent branch cannot recycle opaque references`` () =
        let fixture = prepareWorld ()
        let mutable discardedActor = Unchecked.defaultof<ActorReference>
        let mutable discardedCapability = Unchecked.defaultof<CapabilityReference>
        let mutable invoked = false

        let (acceptedActor: Actor, acceptedCapability: Capability), ready =
            World.genesis
                (name "Brontide.Minimal.Tests:BranchingPolicy")
                (mark 1L)
                (fun genesis world ->
                    let provisional, provisionalWorld =
                        Genesis.actor genesis (name "Brontide.Minimal.Tests:BranchedActor") world

                    let provisionalCapability, _ =
                        Genesis.capability
                            genesis
                            (name "Brontide.Minimal.Tests:BranchedGrant")
                            provisional.Reference
                            fixture.Target.Reference
                            (Set.singleton echoOperation)
                            []
                            false
                            provisionalWorld
                        |> get

                    discardedActor <- provisional.Reference
                    discardedCapability <- provisionalCapability.Reference

                    let accepted, acceptedWorld =
                        Genesis.actor genesis (name "Brontide.Minimal.Tests:BranchedActor") world

                    let acceptedCapability, acceptedWorld =
                        Genesis.capability
                            genesis
                            (name "Brontide.Minimal.Tests:BranchedGrant")
                            accepted.Reference
                            fixture.Target.Reference
                            (Set.singleton echoOperation)
                            []
                            false
                            acceptedWorld
                        |> get

                    ((accepted, acceptedCapability), acceptedWorld))
                fixture.World
            |> get

        let result =
            World.step
                (environment 2L Map.empty (fun value ->
                    invoked <- true
                    Ok(value.Command, [], [])))
                ready
                (request
                    fixture
                    discardedActor
                    fixture.Target.Reference
                    discardedCapability
                    echoOperation
                    (TextValue "must not execute"))

        Assert.Multiple(Action(fun () ->
            Assert.That(discardedActor, Is.Not.EqualTo acceptedActor.Reference)
            Assert.That(discardedCapability, Is.Not.EqualTo acceptedCapability.Reference)
            Assert.That(result.Outcome.Status, Is.EqualTo Denied)
            Assert.That(invoked, Is.False)))

    [<Test>]
    member _.``BR_05_TIME_001 trusted time is explicit monotonic and target scoped`` () =
        let fixture = prepareWorld ()
        let executionRequest =
            request
                fixture
                fixture.Holder.Reference
                fixture.Target.Reference
                fixture.Capability.Reference
                echoOperation
                (TextValue "time")

        let first =
            World.step (environment 10L Map.empty (fun value -> Ok(value.Command, [], []))) fixture.World executionRequest

        let regressed =
            World.step (environment 9L Map.empty (fun value -> Ok(value.Command, [], []))) first.World executionRequest

        let foreignEnvironment =
            { TrustedTime =
                { Milliseconds = 11L
                  TimeDomain = TimeDomainReference.create (name "Brontide.Minimal.Tests:ForeignClock")
                  UncertaintyMilliseconds = None }
              ConstraintEvaluators = Map.empty
              Handlers = Map.empty }

        let foreignClock = World.step foreignEnvironment first.World executionRequest

        Assert.That(first.Outcome.Status, Is.EqualTo Succeeded)
        Assert.That(regressed.Outcome.Status, Is.EqualTo Denied)
        Assert.That(regressed.Outcome.Reason.Value, Does.Contain "backwards")
        Assert.That(foreignClock.Outcome.Status, Is.EqualTo Denied)
        Assert.That(foreignClock.Outcome.Reason.Value, Does.Contain "trusted clock")

[<TestFixture>]
type ShapeConformance() =
    [<Test>]
    member _.``BR_05_SHAPE_001 versions are additive and projections are explicit`` () =
        let world = World.create (Guid.NewGuid()) timeDomain

        let v1: ShapeReference =
            { Name = name "Brontide.Minimal.Tests:Sample"
              Version = 1 }

        let v2 = { v1 with Version = 2 }

        let definition reference fields =
            { Reference = reference
              Description = "Projection test"
              Body = RecordShape fields
              AcceptedFragments = Set.empty
              IsOpenToFragments = false }

        let field fieldName required =
            { Name = fieldName
              Shape = BuiltIn.textShape
              Required = required }

        let withV1 = World.registerShape (definition v1 [ field "name" true ]) world |> get
        let withV2 = World.registerShape (definition v2 [ field "name" true; field "note" false ]) withV1 |> get
        let invalidV2 = definition { v1 with Version = 3 } [ field "name" false ]
        let value = RecordValue(Map.ofList [ "name", TextValue "sample"; "note", TextValue "new" ], Map.empty)
        let projected = World.projectRecord v1 value withV2 |> get

        Assert.That(projected, Is.EqualTo(RecordValue(Map.ofList [ "name", TextValue "sample" ], Map.empty)))
        Assert.That(World.registerShape invalidV2 withV2 |> Result.isError, Is.True)

    [<Test>]
    member _.``BR_05_SHAPE_002 open Velocity projects an authored DirectionalVelocity Fragment`` () =
        let world = World.create (Guid.NewGuid()) timeDomain
        let shape value version: ShapeReference = { Name = name value; Version = version }
        let velocity = shape "Velocity" 1
        let direction = shape "Bob:Direction" 1
        let fragmentShape = shape "Bob:DirectionalVelocity.Fields" 1
        let directional: FragmentReference = { Name = name "Bob:DirectionalVelocity"; Version = 1 }

        let shapeDefinition reference body isOpen =
            { Reference = reference
              Description = "Shape conformance fixture"
              Body = body
              AcceptedFragments = Set.empty
              IsOpenToFragments = isOpen }

        let ready =
            world
            |> World.registerShape
                (shapeDefinition
                    velocity
                    (RecordShape [ { Name = "speed"; Shape = BuiltIn.integerShape; Required = true } ])
                    true)
            |> get
            |> World.registerShape (shapeDefinition direction (ScalarShape Text) false)
            |> get
            |> World.registerShape
                (shapeDefinition
                    fragmentShape
                    (RecordShape [ { Name = "direction"; Shape = direction; Required = true } ])
                    false)
            |> get
            |> World.registerFragment
                { Reference = directional
                  Description = "Bob's authored velocity direction"
                  HostShape = velocity
                  Shape = fragmentShape }
            |> get

        let composed =
            RecordValue(
                Map.ofList [ "speed", IntegerValue 12L ],
                Map.ofList
                    [ directional,
                      RecordValue(Map.ofList [ "direction", TextValue "north" ], Map.empty) ]
            )

        let canonical = World.projectRecord velocity composed ready |> get
        let required = World.projectRecordWithFragments velocity (Set.singleton directional) composed ready |> get
        let baseOnly = RecordValue(Map.ofList [ "speed", IntegerValue 12L ], Map.empty)

        Assert.That(canonical, Is.EqualTo(RecordValue(Map.ofList [ "speed", IntegerValue 12L ], Map.empty)))
        Assert.That(required, Is.EqualTo composed)
        Assert.That(World.validateContract velocity (Set.singleton directional) baseOnly ready |> Result.isError, Is.True)

    [<Test>]
    member _.``BR_05_SHAPE_003 authored fragments stay within their host Shape lineage`` () =
        let world = World.create (Guid.NewGuid()) timeDomain
        let shape value version: ShapeReference = { Name = name value; Version = version }
        let hostA = shape "Example:HostA" 1
        let hostA2 = { hostA with Version = 2 }
        let hostB = shape "Example:HostB" 1
        let explicitHost = shape "Example:ExplicitHost" 1
        let fragmentShape = shape "Example:HostA.Note.Fields" 1
        let note: FragmentReference = { Name = name "Example:HostA.Note"; Version = 1 }

        let shapeDefinition reference accepted isOpen body =
            { Reference = reference
              Description = "Fragment host-lineage fixture"
              Body = body
              AcceptedFragments = accepted
              IsOpenToFragments = isOpen }

        let ready =
            world
            |> World.registerShape (shapeDefinition hostA Set.empty true (RecordShape []))
            |> get
            |> World.registerShape (shapeDefinition hostB Set.empty true (RecordShape []))
            |> get
            |> World.registerShape
                (shapeDefinition explicitHost (Set.singleton note) false (RecordShape []))
            |> get
            |> World.registerShape (shapeDefinition fragmentShape Set.empty false UnitShape)
            |> get
            |> World.registerFragment
                { Reference = note
                  Description = "Authored only for HostA and later HostA versions"
                  HostShape = hostA
                  Shape = fragmentShape }
            |> get
            |> World.registerShape (shapeDefinition hostA2 Set.empty true (RecordShape []))
            |> get

        let composed = RecordValue(Map.empty, Map.ofList [ note, UnitValue ])
        let unrelated = World.validateContract hostB Set.empty composed ready
        let sameLineage = World.validateContract hostA2 Set.empty composed ready
        let explicitlyIncluded = World.validateContract explicitHost Set.empty composed ready

        Assert.Multiple(Action(fun () ->
            Assert.That((unrelated = Error "The fragment is not declared for that host Shape lineage."), Is.True)
            Assert.That((sameLineage = Ok()), Is.True)
            Assert.That((explicitlyIncluded = Ok()), Is.True)))
