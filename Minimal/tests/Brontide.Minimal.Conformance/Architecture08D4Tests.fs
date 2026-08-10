namespace Brontide.Minimal.Conformance

open System
open NUnit.Framework
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel

module private D4Helpers =
    let name value = CanonicalName.create value
    let get = function Ok value -> value | Error message -> failwith message
    let getSome message = function Some value -> value | None -> failwith message
    let timeDomain = TimeDomainReference.create (name "Brontide.Minimal.Tests:Draft08.D4.Clock")
    let mark milliseconds =
        { Milliseconds = milliseconds
          TimeDomain = timeDomain
          UncertaintyMilliseconds = None }
    let operation : OperationReference = { Name = name "Example:ExecuteD4" }
    let remoteLiveness = name "Example:RemoteLiveness"
    let unrelatedPolicy = name "Example:UnrelatedPolicy"
    let perHolderRate = name "Example:PerHolderRate"

    let declaration constraintName scope semantics : ConstraintDeclaration =
        { Name = constraintName
          Version = 1
          ValueShape = BuiltIn.textShape
          EvaluationSemantics = semantics
          EvaluatorDomain = ConstraintEvaluatorDomain.TargetAuthority
          UnknownBehavior = ConstraintUnknownBehavior.Deny
          AccountingScope = scope
          EvolutionPolicy = ConstraintEvolutionPolicy.ParallelCanonicalName }

    type Fixture =
        { World: World
          Grantor: Actor
          FirstHolder: Actor
          SecondHolder: Actor
          Target: Actor
          Root: Capability
          Custom: ConstraintDefinition option }

    let prepare trustedAt configure =
        let initial = World.create (Guid.NewGuid()) timeDomain
        let built, ready =
            World.genesis
                (name "Brontide.Minimal.Tests:Draft08.D4.Policy")
                (mark 0L)
                (fun genesis world ->
                    let grantor, world = Genesis.actor genesis (name "Example:Grantor") world
                    let firstHolder, world = Genesis.actor genesis (name "Example:FirstHolder") world
                    let secondHolder, world = Genesis.actor genesis (name "Example:SecondHolder") world
                    let target, world = Genesis.actor genesis (name "Example:Target") world
                    let world =
                        World.registerOperation
                            { Reference = operation
                              Description = "A08-D4 checked effect"
                              Target = target.Reference
                              CommandShape = BuiltIn.unitShape
                              ResultShape = BuiltIn.unitShape
                              Constraints = [] }
                            world
                        |> get
                    let requirements, custom, world = configure genesis grantor world
                    let root, world =
                        Genesis.capability
                            genesis
                            (name "Example:D4.Root")
                            grantor.Reference
                            target.Reference
                            (Set.singleton operation)
                            requirements
                            world
                        |> get
                    (grantor, firstHolder, secondHolder, target, root, custom), world)
                initial
            |> get
        let grantor, firstHolder, secondHolder, target, root, custom = built
        { World = ready
          Grantor = grantor
          FirstHolder = firstHolder
          SecondHolder = secondHolder
          Target = target
          Root = root
          Custom = custom }, trustedAt

    let child (fixture: Fixture) (holder: Actor) nameValue world =
        World.delegateCapability
            (name nameValue)
            fixture.Grantor.Reference
            holder.Reference
            fixture.Root.Reference
            []
            world
        |> get

    let request (fixture: Fixture) (holder: Actor) (capability: Capability) =
        { Request =
            { Initiator = holder.Reference
              Target = fixture.Target.Reference
              PresentedCapability = capability.Reference
              Operation = operation
              Command = UnitValue
              Occurrence = None
              Context = Map.empty }
          RequestedOrigin = OriginClass.Unverified
          PresentedCommandShape = BuiltIn.unitShape }

    let environment trustedAt evaluators effect =
        { TrustedTime = mark trustedAt
          ConstraintEvaluators = evaluators
          Handlers = Map.ofList [ operation, fun _ -> effect (); Ok(UnitValue, [], []) ] }

open D4Helpers

[<TestFixture>]
type Architecture08D4Tests() =
    [<Test>]
    member _.``D4_C1 BR_08_ADV_C1_001 expired ancestor liveness denies before effect`` () =
        let fixture, trustedAt =
            prepare 6L (fun genesis grantor world ->
                let lease, world = Genesis.livenessLease genesis grantor.Reference 5L world |> get
                [ World.livenessLeaseConstraint lease world |> get ], None, world)
        let child, world = child fixture fixture.FirstHolder "Example:D4.Child" fixture.World
        let mutable effects = 0
        let result =
            World.stepDraft08
                (environment trustedAt Map.empty (fun () -> effects <- effects + 1))
                world
                (request fixture fixture.FirstHolder child)
        Assert.Multiple(Action(fun () ->
            Assert.That(result.Outcome.Status, Is.EqualTo Denied)
            Assert.That(effects, Is.Zero)))

    [<Test>]
    member _.``D4_C2 BR_08_ADV_C1_002 unavailable liveness evaluator denies with redacted category`` () =
        let fixture, trustedAt =
            prepare 1L (fun _ _ world ->
                let definition, world =
                    World.registerConstraintDeclaration
                        (declaration remoteLiveness ConstraintAccountingScope.NotQuantified "remote liveness scope is active")
                        world
                    |> get
                [ { Constraint = definition.Reference
                    ParameterShape = BuiltIn.textShape
                    Parameters = TextValue "sensitive-scope-reference" } ], Some definition, world)
        let mutable effects = 0
        let result =
            World.stepDraft08
                (environment trustedAt Map.empty (fun () -> effects <- effects + 1))
                fixture.World
                (request fixture fixture.Grantor fixture.Root)
        Assert.Multiple(Action(fun () ->
            Assert.That(result.Outcome.Status, Is.EqualTo Denied)
            Assert.That(result.Outcome.Reason.Value, Does.Contain(CanonicalName.value remoteLiveness))
            Assert.That(result.Outcome.Reason.Value, Does.Not.Contain "sensitive-scope-reference")
            Assert.That(effects, Is.Zero)))

    [<Test>]
    member _.``D4_C3 BR_08_ADV_C1_003 live ancestor authorises exactly one effect`` () =
        let fixture, trustedAt =
            prepare 1L (fun genesis grantor world ->
                let lease, world = Genesis.livenessLease genesis grantor.Reference 5L world |> get
                [ World.livenessLeaseConstraint lease world |> get ], None, world)
        let child, world = child fixture fixture.FirstHolder "Example:D4.Child" fixture.World
        let mutable effects = 0
        let result =
            World.stepDraft08
                (environment trustedAt Map.empty (fun () -> effects <- effects + 1))
                world
                (request fixture fixture.FirstHolder child)
        Assert.Multiple(Action(fun () ->
            Assert.That(result.Outcome.Status, Is.EqualTo Succeeded)
            Assert.That(effects, Is.EqualTo 1)))

    [<Test>]
    member _.``D4_C4 BR_08_ADV_C5_001 sibling delegations share ancestor occurrence budget`` () =
        let fixture, trustedAt =
            prepare 1L (fun _ _ world ->
                [ World.executionRateLimitConstraint 2L 60_000L world |> get ], None, world)
        let first, world = child fixture fixture.FirstHolder "Example:D4.First" fixture.World
        let second, world = child fixture fixture.SecondHolder "Example:D4.Second" world
        let mutable effects = 0
        let env = environment trustedAt Map.empty (fun () -> effects <- effects + 1)
        let one = World.stepDraft08 env world (request fixture fixture.FirstHolder first)
        let two = World.stepDraft08 env one.World (request fixture fixture.FirstHolder first)
        let sibling = World.stepDraft08 env two.World (request fixture fixture.SecondHolder second)
        Assert.Multiple(Action(fun () ->
            Assert.That(one.Outcome.Status, Is.EqualTo Succeeded)
            Assert.That(two.Outcome.Status, Is.EqualTo Succeeded)
            Assert.That(sibling.Outcome.Status, Is.EqualTo Denied)
            Assert.That(effects, Is.EqualTo 2)))

    [<Test>]
    member _.``D4_C5 BR_08_ADV_C5_002 denied executions consume no budget`` () =
        let fixture, trustedAt =
            prepare 1L (fun _ _ world ->
                let unrelated, world =
                    World.registerConstraintDeclaration
                        (declaration unrelatedPolicy ConstraintAccountingScope.NotQuantified "unrelated policy is satisfied")
                        world
                    |> get
                [ World.executionRateLimitConstraint 1L 60_000L world |> get
                  { Constraint = unrelated.Reference
                    ParameterShape = BuiltIn.textShape
                    Parameters = TextValue "check" } ], Some unrelated, world)
        let unrelated = fixture.Custom |> getSome "unrelated Constraint missing"
        let mutable allows = false
        let mutable effects = 0
        let evaluator _ _ = if allows then Ok() else Error "unrelated policy denied"
        let env () = environment trustedAt (Map.ofList [ unrelated.Reference, evaluator ]) (fun () -> effects <- effects + 1)
        let denied1 = World.stepDraft08 (env ()) fixture.World (request fixture fixture.Grantor fixture.Root)
        let denied2 = World.stepDraft08 (env ()) denied1.World (request fixture fixture.Grantor fixture.Root)
        allows <- true
        let allowed = World.stepDraft08 (env ()) denied2.World (request fixture fixture.Grantor fixture.Root)
        Assert.Multiple(Action(fun () ->
            Assert.That(denied1.Outcome.Status, Is.EqualTo Denied)
            Assert.That(denied2.Outcome.Status, Is.EqualTo Denied)
            Assert.That(allowed.Outcome.Status, Is.EqualTo Succeeded)
            Assert.That(effects, Is.EqualTo 1)))

    [<Test>]
    member _.``D4_C6 BR_08_ADV_C5_003 unenforceable vocabulary scope denies before evaluator`` () =
        let fixture, trustedAt =
            prepare 1L (fun _ _ world ->
                let scoped, world =
                    World.registerConstraintDeclaration
                        (declaration perHolderRate
                            (ConstraintAccountingScope.VocabularyDefined(name "Example:PerHolder", true))
                            "one execution per holder")
                        world
                    |> get
                [ { Constraint = scoped.Reference
                    ParameterShape = BuiltIn.textShape
                    Parameters = TextValue "1" } ], Some scoped, world)
        let scoped = fixture.Custom |> getSome "scoped Constraint missing"
        let mutable evaluatorCalls = 0
        let mutable effects = 0
        let evaluator _ _ = evaluatorCalls <- evaluatorCalls + 1; Ok()
        let result =
            World.stepDraft08
                (environment trustedAt (Map.ofList [ scoped.Reference, evaluator ]) (fun () -> effects <- effects + 1))
                fixture.World
                (request fixture fixture.Grantor fixture.Root)
        let recognition =
            World.constraintRecognitionSet
                (environment trustedAt (Map.ofList [ scoped.Reference, evaluator ]) ignore)
                fixture.World
        Assert.Multiple(Action(fun () ->
            Assert.That(result.Outcome.Status, Is.EqualTo Denied)
            Assert.That(
                recognition |> List.find (fun item -> item.Declaration.Name = perHolderRate) |> _.Decision,
                Is.EqualTo ConstraintRecognitionDecision.Declined)
            Assert.That(evaluatorCalls, Is.Zero)
            Assert.That(effects, Is.Zero)))
