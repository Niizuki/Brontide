namespace Brontide.Minimal.Conformance

open System
open NUnit.Framework
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel

module private D2Helpers =
    let name value = CanonicalName.create value

    let get = function
        | Ok value -> value
        | Error message -> failwith message

    let getSome message = function
        | Some value -> value
        | None -> failwith message

    let timeDomain = TimeDomainReference.create (name "Brontide.Minimal.Tests:Draft08.D2.Clock")

    let mark milliseconds =
        { Milliseconds = milliseconds
          TimeDomain = timeDomain
          UncertaintyMilliseconds = None }

    let operation: OperationReference =
        { Name = name "Brontide.Minimal.Tests:Draft08.D2.Execute" }

    type Fixture =
        { World: World
          ActorA: Actor
          ActorB: Actor
          ActorC: Actor
          Target: Actor
          Root: Capability
          DelegationDepth: ConstraintDefinition
          OriginGrant: ConstraintDefinition
          OriginCeiling: ConstraintDefinition }

    let prepare requirements =
        let initial = World.create (Guid.NewGuid()) timeDomain
        let delegationDepth =
            World.tryFindConstraintByName BuiltIn.delegationDepthConstraintName initial
            |> getSome "delegation-depth definition missing"
        let originGrant =
            World.tryFindConstraintByName BuiltIn.originGrantConstraintName initial
            |> getSome "origin-grant definition missing"
        let originCeiling =
            World.tryFindConstraintByName BuiltIn.originCeilingConstraintName initial
            |> getSome "origin-ceiling definition missing"
        let fixture, ready =
            World.genesis
                (name "Brontide.Minimal.Tests:Draft08.D2.Policy")
                (mark 0L)
                (fun genesis world ->
                    let actorA, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:Draft08.D2.ActorA") world
                    let actorB, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:Draft08.D2.ActorB") world
                    let actorC, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:Draft08.D2.ActorC") world
                    let target, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:Draft08.D2.Target") world
                    let definition =
                        { Reference = operation
                          Description = "A08-D2 checked effect"
                          Target = target.Reference
                          CommandShape = BuiltIn.unitShape
                          ResultShape = BuiltIn.unitShape
                          Constraints = [] }
                    let world = World.registerOperation definition world |> get
                    let root, world =
                        Genesis.capability
                            genesis
                            (name "Brontide.Minimal.Tests:Draft08.D2.Root")
                            actorA.Reference
                            target.Reference
                            (Set.singleton operation)
                            (requirements delegationDepth originGrant originCeiling)
                            world
                        |> get
                    (actorA, actorB, actorC, target, root), world)
                initial
            |> get
        let actorA, actorB, actorC, target, root = fixture
        { World = ready
          ActorA = actorA
          ActorB = actorB
          ActorC = actorC
          Target = target
          Root = root
          DelegationDepth = delegationDepth
          OriginGrant = originGrant
          OriginCeiling = originCeiling }

    let environment effects =
        { TrustedTime = mark 1L
          ConstraintEvaluators = Map.empty
          Handlers =
            Map.ofList
                [ operation,
                  fun request ->
                      effects ()
                      Ok(request.Command, [], []) ] }

    let request (fixture: Fixture) (actor: Actor) (capability: Capability) (origin: OriginClass) : Draft08ExecutionRequest =
        { Request =
            { Initiator = actor.Reference
              Target = fixture.Target.Reference
              PresentedCapability = capability.Reference
              Operation = operation
              Command = UnitValue
              Occurrence = None
              Context = Map.empty }
          RequestedOrigin = origin
          PresentedCommandShape = BuiltIn.unitShape }

open D2Helpers

[<TestFixture>]
type Architecture08D2Tests() =
    [<Test>]
    member _.``D2_C1 BR_08_ADV_C6_001 unadorned Capability is delegable by default`` () =
        let fixture = prepare (fun _ _ _ -> [])
        let child, world =
            World.delegateCapability
                (name "Brontide.Minimal.Tests:Draft08.D2.Child")
                fixture.ActorA.Reference
                fixture.ActorB.Reference
                fixture.Root.Reference
                []
                fixture.World
            |> get
        let mutable effects = 0
        let execution =
            World.stepDraft08
                (environment (fun () -> effects <- effects + 1))
                world
                (request fixture fixture.ActorB child OriginClass.Unverified)

        Assert.Multiple(Action(fun () ->
            Assert.That(execution.Outcome.Status, Is.EqualTo Succeeded)
            Assert.That(effects, Is.EqualTo 1)
            Assert.That(child.Parent, Is.EqualTo(Some fixture.Root.Reference))
            Assert.That(child.Target, Is.EqualTo fixture.Root.Target)
            Assert.That(child.Operations = fixture.Root.Operations, Is.True)))

    [<Test>]
    member _.``D2_C2 BR_08_ADV_C6_002 delegation depth Constraint denies every descendant`` () =
        let fixture =
            prepare (fun depth _ _ ->
                [ { Constraint = depth.Reference
                    ParameterShape = depth.ParameterShape
                    Parameters = IntegerValue 0L } ])
        let child, world =
            World.delegateCapability
                (name "Brontide.Minimal.Tests:Draft08.D2.Child")
                fixture.ActorA.Reference
                fixture.ActorB.Reference
                fixture.Root.Reference
                []
                fixture.World
            |> get
        let grandchild, world =
            World.delegateCapability
                (name "Brontide.Minimal.Tests:Draft08.D2.Grandchild")
                fixture.ActorB.Reference
                fixture.ActorC.Reference
                child.Reference
                []
                world
            |> get
        let mutable effects = 0
        let env = environment (fun () -> effects <- effects + 1)
        let rootResult = World.stepDraft08 env world (request fixture fixture.ActorA fixture.Root OriginClass.Unverified)
        effects <- 0
        let childResult = World.stepDraft08 env world (request fixture fixture.ActorB child OriginClass.Unverified)
        let grandchildResult = World.stepDraft08 env world (request fixture fixture.ActorC grandchild OriginClass.Unverified)

        Assert.Multiple(Action(fun () ->
            Assert.That(rootResult.Outcome.Status, Is.EqualTo Succeeded)
            Assert.That(childResult.Outcome.Status, Is.EqualTo Denied)
            Assert.That(grandchildResult.Outcome.Status, Is.EqualTo Denied)
            Assert.That(childResult.Outcome.Reason.Value, Does.Contain "delegation depth")
            Assert.That(grandchildResult.Outcome.Reason.Value, Does.Contain "delegation depth")
            Assert.That(effects, Is.Zero)))

    [<Test>]
    member _.``D2_C3 BR_08_ADV_C2_001 delegated origin is capped by implicit Constraint`` () =
        let fixture =
            prepare (fun _ grant _ ->
                [ { Constraint = grant.Reference
                    ParameterShape = grant.ParameterShape
                    Parameters = TextValue "Device" } ])
        let child, world =
            World.delegateCapability
                (name "Brontide.Minimal.Tests:Draft08.D2.Child")
                fixture.ActorA.Reference
                fixture.ActorB.Reference
                fixture.Root.Reference
                []
                fixture.World
            |> get
        let grandchild, world =
            World.delegateCapability
                (name "Brontide.Minimal.Tests:Draft08.D2.Grandchild")
                fixture.ActorB.Reference
                fixture.ActorC.Reference
                child.Reference
                []
                world
            |> get
        let mutable effects = 0
        let env = environment (fun () -> effects <- effects + 1)
        let spoofed = World.stepDraft08 env world (request fixture fixture.ActorB child OriginClass.Device)
        let derived = World.stepDraft08 env world (request fixture fixture.ActorB child OriginClass.Derived)
        let unverified = World.stepDraft08 env world (request fixture fixture.ActorB child OriginClass.Unverified)
        let implicitCeiling =
            child.AddedConstraints
            |> List.find (fun requirement -> requirement.Constraint = fixture.OriginCeiling.Reference)
        let grandchildCeiling =
            grandchild.AddedConstraints
            |> List.find (fun requirement -> requirement.Constraint = fixture.OriginCeiling.Reference)

        Assert.Multiple(Action(fun () ->
            Assert.That(spoofed.Outcome.Status, Is.EqualTo Denied)
            Assert.That(derived.Outcome.Status, Is.EqualTo Succeeded)
            Assert.That(derived.Outcome.Event.Origin, Is.EqualTo OriginClass.Derived)
            Assert.That(unverified.Outcome.Status, Is.EqualTo Succeeded)
            Assert.That(unverified.Outcome.Event.Origin, Is.EqualTo OriginClass.Unverified)
            Assert.That(implicitCeiling.Parameters, Is.EqualTo(TextValue "Derived"))
            Assert.That(grandchildCeiling.Parameters, Is.EqualTo(TextValue "Derived"))
            Assert.That(effects, Is.EqualTo 2)))

    [<Test>]
    member _.``D2_C4 BR_08_ADV_C2_002 primordial origin grant remains vouched`` () =
        let fixture =
            prepare (fun _ grant _ ->
                [ { Constraint = grant.Reference
                    ParameterShape = grant.ParameterShape
                    Parameters = TextValue "Device" } ])
        let mutable effects = 0
        let execution =
            World.stepDraft08
                (environment (fun () -> effects <- effects + 1))
                fixture.World
                (request fixture fixture.ActorA fixture.Root OriginClass.Device)

        Assert.Multiple(Action(fun () ->
            Assert.That(execution.Outcome.Status, Is.EqualTo Succeeded)
            Assert.That(execution.Outcome.Event.Origin, Is.EqualTo OriginClass.Device)
            Assert.That(
                fixture.Root.AddedConstraints
                |> List.exists (fun requirement -> requirement.Constraint = fixture.OriginCeiling.Reference),
                Is.False)))

    [<Test>]
    member _.``D2_C5 phase property denials are effect free and Boolean surface is removed`` () =
        let fixture =
            prepare (fun depth _ _ ->
                [ { Constraint = depth.Reference
                    ParameterShape = depth.ParameterShape
                    Parameters = IntegerValue 0L } ])
        let child, world =
            World.delegateCapability
                (name "Brontide.Minimal.Tests:Draft08.D2.Child")
                fixture.ActorA.Reference
                fixture.ActorB.Reference
                fixture.Root.Reference
                []
                fixture.World
            |> get
        let mutable effects = 0
        let execution =
            World.stepDraft08
                (environment (fun () -> effects <- effects + 1))
                world
                (request fixture fixture.ActorB child OriginClass.Unverified)

        Assert.Multiple(Action(fun () ->
            Assert.That(execution.Outcome.Status, Is.EqualTo Denied)
            Assert.That(effects, Is.Zero)
            Assert.That(typeof<Capability>.GetProperty("DelegationAllowed"), Is.Null)))
