namespace Brontide.Minimal.Conformance

open System
open NUnit.Framework
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel

module private D1Helpers =
    let name value = CanonicalName.create value

    let get = function
        | Ok value -> value
        | Error message -> failwith message

    let timeDomain = TimeDomainReference.create (name "Brontide.Minimal.Tests:Draft08Clock")

    let mark milliseconds =
        { Milliseconds = milliseconds
          TimeDomain = timeDomain
          UncertaintyMilliseconds = None }

    let operation: OperationReference =
        { Name = name "Brontide.Minimal.Tests:Draft08.Execute" }

    type AuthorityObservation =
        { Execution: StepResult
          Effects: int
          UnknownName: CanonicalName }

    let executeExpression expressionFactory =
        let initial = World.create (Guid.NewGuid()) timeDomain
        let satisfied, world =
            World.registerConstraint
                (name "Brontide.Minimal.Tests:Draft08.Satisfied")
                BuiltIn.textShape
                "satisfied atom"
                initial
            |> get
        let unsatisfied, world =
            World.registerConstraint
                (name "Brontide.Minimal.Tests:Draft08.Unsatisfied")
                BuiltIn.textShape
                "unsatisfied atom"
                world
            |> get
        let unknown, world =
            World.registerConstraint
                (name "Brontide.Minimal.Tests:Draft08.Unknown")
                BuiltIn.textShape
                "unknown atom"
                world
            |> get
        let atom (definition: ConstraintDefinition) value =
            AtomicConstraint
                { Constraint = definition.Reference
                  Parameters = TextValue value }
        let expression =
            expressionFactory
                (atom satisfied "satisfied")
                (atom unsatisfied "unsatisfied")
                (atom unknown "protected-value")
        let fixture, ready =
            World.genesis
                (name "Brontide.Minimal.Tests:Draft08.Policy")
                (mark 0L)
                (fun genesis genesisWorld ->
                    let holder, genesisWorld = Genesis.actor genesis (name "Brontide.Minimal.Tests:Draft08.Holder") genesisWorld
                    let target, genesisWorld = Genesis.actor genesis (name "Brontide.Minimal.Tests:Draft08.Target") genesisWorld
                    let definition =
                        { Reference = operation
                          Description = "Draft 0.8 expression"
                          Target = target.Reference
                          CommandShape = BuiltIn.textShape
                          ResultShape = BuiltIn.textShape
                          Constraints = [] }
                    let genesisWorld = World.registerOperation definition genesisWorld |> get
                    let capability, genesisWorld =
                        Genesis.capabilityWithExpressions
                            genesis
                            (name "Brontide.Minimal.Tests:Draft08.Capability")
                            holder.Reference
                            target.Reference
                            (Set.singleton operation)
                            [ expression ]
                            genesisWorld
                        |> get
                    (holder, target, capability), genesisWorld)
                world
            |> get
        let holder, target, capability = fixture
        let mutable effects = 0
        let environment =
            { TrustedTime = mark 1L
              ConstraintEvaluators =
                Map.ofList
                    [ satisfied.Reference, fun _ _ -> Ok()
                      unsatisfied.Reference, fun _ _ -> Error "the atom is false" ]
              Handlers =
                Map.ofList
                    [ operation,
                      fun request ->
                          effects <- effects + 1
                          Ok(request.Command, [], []) ] }
        let request =
            { Initiator = holder.Reference
              Target = target.Reference
              PresentedCapability = capability.Reference
              Operation = operation
              Command = TextValue "command"
              Occurrence = None
              Context = Map.empty }

        { Execution =
            World.stepDraft08 environment ready { Request = request; RequestedOrigin = OriginClass.Unverified }
          Effects = effects
          UnknownName = unknown.Name }

    type ValidityObservation =
        { Execution: StepResult
          Effects: int
          WallTimeAfterExecution: int64 }

    let executeValidity wallTimeAtPresentation =
        let initial = World.create (Guid.NewGuid()) timeDomain
        let validity, world =
            World.registerConstraint
                (name "Brontide.Minimal.Tests:Draft08.Validity")
                BuiltIn.textShape
                "not-after validity"
                initial
            |> get
        let fixture, ready =
            World.genesis
                (name "Brontide.Minimal.Tests:Draft08.InstantaneousPolicy")
                (mark 0L)
                (fun genesis genesisWorld ->
                    let holder, genesisWorld = Genesis.actor genesis (name "Brontide.Minimal.Tests:Draft08.TimeHolder") genesisWorld
                    let target, genesisWorld = Genesis.actor genesis (name "Brontide.Minimal.Tests:Draft08.TimeTarget") genesisWorld
                    let definition =
                        { Reference = operation
                          Description = "cross validity boundary"
                          Target = target.Reference
                          CommandShape = BuiltIn.textShape
                          ResultShape = BuiltIn.textShape
                          Constraints = [] }
                    let genesisWorld = World.registerOperation definition genesisWorld |> get
                    let expression =
                        AtomicConstraint
                            { Constraint = validity.Reference
                              Parameters = TextValue "not-after:5" }
                    let capability, genesisWorld =
                        Genesis.capabilityWithExpressions
                            genesis
                            (name "Brontide.Minimal.Tests:Draft08.TimeCapability")
                            holder.Reference
                            target.Reference
                            (Set.singleton operation)
                            [ expression ]
                            genesisWorld
                        |> get
                    (holder, target, capability), genesisWorld)
                world
            |> get
        let holder, target, capability = fixture
        let mutable wallTime = wallTimeAtPresentation
        let mutable effects = 0
        let environment =
            { TrustedTime = mark 1L
              ConstraintEvaluators =
                Map.ofList
                    [ validity.Reference,
                      fun _ _ -> if wallTime <= 5L then Ok() else Error "validity expired" ]
              Handlers =
                Map.ofList
                    [ operation,
                      fun request ->
                          effects <- effects + 1
                          wallTime <- 10L
                          Ok(request.Command, [], []) ] }
        let request =
            { Initiator = holder.Reference
              Target = target.Reference
              PresentedCapability = capability.Reference
              Operation = operation
              Command = TextValue "command"
              Occurrence = None
              Context = Map.empty }

        { Execution =
            World.stepDraft08 environment ready { Request = request; RequestedOrigin = OriginClass.Unverified }
          Effects = effects
          WallTimeAfterExecution = wallTime }

open D1Helpers

[<TestFixture>]
type Architecture08D1Tests() =
    [<Test>]
    member _.``BR_08_ADV_C7_001 not unknown denies before effect`` () =
        let observation = executeExpression (fun _ _ unknown -> Not unknown)
        Assert.That(observation.Execution.Outcome.Status, Is.EqualTo Denied)
        Assert.That(observation.Effects, Is.Zero)
        Assert.That(observation.Execution.Outcome.Reason.Value, Does.Contain(CanonicalName.value observation.UnknownName))
        Assert.That(observation.Execution.Outcome.Reason.Value, Does.Not.Contain "protected-value")

    [<Test>]
    member _.``BR_08_ADV_C7_002 any true unknown authorizes`` () =
        let observation = executeExpression (fun satisfied _ unknown -> AnyOf [ satisfied; unknown ])
        Assert.That(observation.Execution.Outcome.Status, Is.EqualTo Succeeded)
        Assert.That(observation.Effects, Is.EqualTo 1)

    [<Test>]
    member _.``BR_08_ADV_C7_003 all true unknown denies before effect`` () =
        let observation = executeExpression (fun satisfied _ unknown -> AllOf [ satisfied; unknown ])
        Assert.That(observation.Execution.Outcome.Status, Is.EqualTo Denied)
        Assert.That(observation.Effects, Is.Zero)
        Assert.That(observation.Execution.Outcome.Reason.Value, Does.Contain(CanonicalName.value observation.UnknownName))

    [<Test>]
    member _.``BR_08_ADV_C7_004 any unknown false denies before effect`` () =
        let observation = executeExpression (fun _ unsatisfied unknown -> AnyOf [ unknown; unsatisfied ])
        Assert.That(observation.Execution.Outcome.Status, Is.EqualTo Denied)
        Assert.That(observation.Effects, Is.Zero)
        Assert.That(observation.Execution.Outcome.Reason.Value, Does.Contain(CanonicalName.value observation.UnknownName))

    [<Test>]
    member _.``BR_08_ADV_C7_005 all false unknown is false and denies`` () =
        let observation = executeExpression (fun _ unsatisfied unknown -> AllOf [ unsatisfied; unknown ])
        Assert.That(observation.Execution.Outcome.Status, Is.EqualTo Denied)
        Assert.That(observation.Effects, Is.Zero)
        Assert.That(observation.Execution.Outcome.Reason.Value, Does.Contain "Unsatisfied")
        Assert.That(observation.Execution.Outcome.Reason.Value, Does.Contain(CanonicalName.value observation.UnknownName))

    [<Test>]
    member _.``BR_08_ADV_C7_006 unknown excluded middle remains unknown`` () =
        let observation = executeExpression (fun _ _ unknown -> AnyOf [ unknown; Not unknown ])
        Assert.That(observation.Execution.Outcome.Status, Is.EqualTo Denied)
        Assert.That(observation.Effects, Is.Zero)
        Assert.That(observation.Execution.Outcome.Reason.Value, Does.Contain(CanonicalName.value observation.UnknownName))

    [<Test>]
    member _.``BR_08_ADV_C3_001 expiry after effect start does not retroactively deny`` () =
        let observation = executeValidity 0L
        Assert.That(observation.Execution.Outcome.Status, Is.EqualTo Succeeded)
        Assert.That(observation.Effects, Is.EqualTo 1)
        Assert.That(observation.WallTimeAfterExecution, Is.EqualTo 10L)

    [<Test>]
    member _.``BR_08_ADV_C3_002 new execution after expiry is denied`` () =
        let observation = executeValidity 10L
        Assert.That(observation.Execution.Outcome.Status, Is.EqualTo Denied)
        Assert.That(observation.Effects, Is.Zero)

    [<Test>]
    member _.``BR_08_ADV_C4_001 grandparent constraint denies grandchild`` () =
        let initial = World.create (Guid.NewGuid()) timeDomain
        let narrowing, world =
            World.registerConstraint
                (name "Brontide.Minimal.Tests:Draft08.GrandparentNarrowing")
                BuiltIn.textShape
                "grandparent narrowing"
                initial
            |> get
        let fixture, ready =
            World.genesis
                (name "Brontide.Minimal.Tests:Draft08.ChainPolicy")
                (mark 0L)
                (fun genesis genesisWorld ->
                    let actorA, genesisWorld = Genesis.actor genesis (name "Brontide.Minimal.Tests:Draft08.ActorA") genesisWorld
                    let actorB, genesisWorld = Genesis.actor genesis (name "Brontide.Minimal.Tests:Draft08.ActorB") genesisWorld
                    let actorD, genesisWorld = Genesis.actor genesis (name "Brontide.Minimal.Tests:Draft08.ActorD") genesisWorld
                    let target, genesisWorld = Genesis.actor genesis (name "Brontide.Minimal.Tests:Draft08.ChainTarget") genesisWorld
                    let definition =
                        { Reference = operation
                          Description = "grandparent denial"
                          Target = target.Reference
                          CommandShape = BuiltIn.textShape
                          ResultShape = BuiltIn.textShape
                          Constraints = [] }
                    let genesisWorld = World.registerOperation definition genesisWorld |> get
                    let expression =
                        AtomicConstraint
                            { Constraint = narrowing.Reference
                              Parameters = TextValue "deny" }
                    let root, genesisWorld =
                        Genesis.capabilityWithExpressions
                            genesis
                            (name "Brontide.Minimal.Tests:Draft08.Root")
                            actorA.Reference
                            target.Reference
                            (Set.singleton operation)
                            [ expression ]
                            genesisWorld
                        |> get
                    (actorA, actorB, actorD, target, root), genesisWorld)
                world
            |> get
        let actorA, actorB, actorD, target, root = fixture
        let child, ready =
            World.delegateCapabilityWithExpressions
                (name "Brontide.Minimal.Tests:Draft08.Child")
                actorA.Reference
                actorB.Reference
                root.Reference
                []
                ready
            |> get
        let grandchild, ready =
            World.delegateCapabilityWithExpressions
                (name "Brontide.Minimal.Tests:Draft08.Grandchild")
                actorB.Reference
                actorD.Reference
                child.Reference
                []
                ready
            |> get
        let mutable effects = 0
        let environment =
            { TrustedTime = mark 1L
              ConstraintEvaluators = Map.ofList [ narrowing.Reference, fun _ _ -> Error "grandparent denied" ]
              Handlers = Map.ofList [ operation, fun request -> effects <- effects + 1; Ok(request.Command, [], []) ] }
        let request =
            { Initiator = actorD.Reference
              Target = target.Reference
              PresentedCapability = grandchild.Reference
              Operation = operation
              Command = TextValue "command"
              Occurrence = None
              Context = Map.empty }

        let execution =
            World.stepDraft08 environment ready { Request = request; RequestedOrigin = OriginClass.Unverified }

        Assert.That(execution.Outcome.Status, Is.EqualTo Denied)
        Assert.That(effects, Is.Zero)
        Assert.That(List.length grandchild.AddedConstraints, Is.EqualTo 1)
