namespace Brontide.Minimal.Conformance

open System
open NUnit.Framework
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel

module private D6Helpers =
    let name value = CanonicalName.create value
    let get = function Ok value -> value | Error message -> failwith (string message)
    let timeDomain = TimeDomainReference.create (name "Brontide.Minimal.Tests:Draft08.D6.Clock")
    let mark milliseconds = { Milliseconds = milliseconds; TimeDomain = timeDomain; UncertaintyMilliseconds = None }
    let operation: OperationReference = { Name = name "Example:ExecuteD6" }

    type Fixture =
        { World: World
          PolicyActor: Actor
          Grantor: Actor
          Holder: Actor
          DescendantHolder: Actor
          Target: Actor
          Root: Capability
          Immortal: Capability
          Lease: LivenessLease
          LiveRoot: Capability
          LiveOutbound: Capability }

    let prepare () =
        let initial = World.create (Guid.NewGuid()) timeDomain
        let built, world =
            World.genesis
                (name "Example:D6.Genesis") (mark 0L)
                (fun genesis world ->
                    let policyActor, world = Genesis.actor genesis (name "Example:RetirementPolicy") world
                    let grantor, world = Genesis.actor genesis (name "Example:GrantorA") world
                    let holder, world = Genesis.actor genesis (name "Example:HolderB") world
                    let descendantHolder, world = Genesis.actor genesis (name "Example:HolderC") world
                    let target, world = Genesis.actor genesis (name "Example:Target") world
                    let world =
                        World.registerOperation
                            { Reference = operation; Description = "D6 checked effect"; Target = target.Reference
                              CommandShape = BuiltIn.unitShape; ResultShape = BuiltIn.unitShape; Constraints = [] }
                            world |> get
                    let root, world =
                        Genesis.capability genesis (name "Example:D6.Root") grantor.Reference target.Reference
                            (Set.singleton operation) [] world |> get
                    let immortal, world =
                        World.delegateCapability (name "Example:D6.Immortal") grantor.Reference holder.Reference
                            root.Reference [] world |> get
                    let lease, world = Genesis.livenessLease genesis grantor.Reference 3600000L world |> get
                    let liveRequirement = World.livenessLeaseConstraint lease world |> get
                    let liveRoot, world =
                        Genesis.capability genesis (name "Example:D6.LiveRoot") grantor.Reference target.Reference
                            (Set.singleton operation) [ liveRequirement ] world |> get
                    let liveOutbound, world =
                        World.delegateCapability (name "Example:D6.LiveOutbound") grantor.Reference holder.Reference
                            liveRoot.Reference [] world |> get
                    (policyActor, grantor, holder, descendantHolder, target, root, immortal, lease, liveRoot, liveOutbound), world)
                initial |> get
        let policyActor, grantor, holder, descendantHolder, target, root, immortal, lease, liveRoot, liveOutbound = built
        { World = world; PolicyActor = policyActor; Grantor = grantor; Holder = holder
          DescendantHolder = descendantHolder; Target = target; Root = root; Immortal = immortal
          Lease = lease; LiveRoot = liveRoot; LiveOutbound = liveOutbound }

    let request (fixture: Fixture) (actor: Actor) (capability: Capability) =
        { Request =
            { Initiator = actor.Reference; Target = fixture.Target.Reference
              PresentedCapability = capability.Reference; Operation = operation; Command = UnitValue
              Occurrence = None; Context = Map.empty }
          RequestedOrigin = OriginClass.Unverified
          PresentedCommandShape = BuiltIn.unitShape }

    let environment effects =
        { TrustedTime = mark 2L
          ConstraintEvaluators = Map.empty
          Handlers = Map.ofList [ operation, fun _ -> effects(); Ok(UnitValue, [], []) ] }

open D6Helpers

[<TestFixture>]
type Architecture08D6Tests() =
    [<Test>]
    member _.``D6_C1 BR_08_ADV_C12_003 Terminus is attributable enumerable and policy declared`` () =
        let fixture = prepare()
        let record, world =
            World.terminateActor fixture.PolicyActor.Reference fixture.Grantor.Reference "account retired" (mark 1L) fixture.World |> get
        let duplicate =
            World.terminateActor fixture.PolicyActor.Reference fixture.Grantor.Reference "duplicate" (mark 2L) world
        Assert.Multiple(Action(fun () ->
            Assert.That(record.PolicyActor, Is.EqualTo fixture.PolicyActor.Reference)
            Assert.That(record.ActorRetired, Is.EqualTo fixture.Grantor.Reference)
            Assert.That(record.Reason, Is.EqualTo "account retired")
            Assert.That(record.Policy, Is.EqualTo(World.terminusPolicy world))
            Assert.That(record.HeldCapabilitiesExtinguished, Does.Contain fixture.Root.Reference)
            Assert.That(record.OutboundGrantsSurviving, Does.Contain fixture.Immortal.Reference)
            Assert.That(record.OutboundGrantsExtinguished, Does.Contain fixture.LiveOutbound.Reference)
            Assert.That(World.terminusOccurrences world = [ record ], Is.True)
            Assert.That(duplicate |> Result.isError, Is.True)
            Assert.That((World.terminusPolicy world).OutboundGrantDisposition,
                Is.EqualTo ImmortalSurvivesIndefinitely)
            Assert.That((World.terminusPolicy world).ActorReferenceDisposition,
                Is.EqualTo RetainedWithoutReuse)))

    [<Test>]
    member _.``D6_C2 BR_08_ADV_C12_001 held authority denies after retirement without erasing identity`` () =
        let fixture = prepare()
        let _, world =
            World.terminateActor fixture.PolicyActor.Reference fixture.Grantor.Reference "holder retired" (mark 1L) fixture.World |> get
        let mutable effects = 0
        let result = World.stepDraft08 (environment (fun () -> effects <- effects + 1)) world (request fixture fixture.Grantor fixture.Root)
        Assert.Multiple(Action(fun () ->
            Assert.That(result.Outcome.Status, Is.EqualTo Denied)
            Assert.That(result.Outcome.Reason.Value, Does.Contain "retired")
            Assert.That(effects, Is.Zero)
            Assert.That(World.actors world |> List.map _.Reference, Does.Contain fixture.Grantor.Reference)
            Assert.That(World.capabilities world |> List.map _.Reference, Does.Contain fixture.Root.Reference)
            Assert.That(World.retiredActors world, Does.Contain fixture.Grantor.Reference)))

    [<Test>]
    member _.``D6_C3 BR_08_ADV_C12_002 immortal outbound grant survives with grantor attribution`` () =
        let fixture = prepare()
        let _, world =
            World.terminateActor fixture.PolicyActor.Reference fixture.Grantor.Reference "grantor retired" (mark 1L) fixture.World |> get
        let mutable effects = 0
        let result = World.stepDraft08 (environment (fun () -> effects <- effects + 1)) world (request fixture fixture.Holder fixture.Immortal)
        let narrowed, nextWorld =
            World.delegateCapability (name "Example:D6.Descendant") fixture.Holder.Reference
                fixture.DescendantHolder.Reference fixture.Immortal.Reference [] result.World |> get
        Assert.Multiple(Action(fun () ->
            Assert.That(result.Outcome.Status, Is.EqualTo Succeeded)
            Assert.That(effects, Is.EqualTo 1)
            Assert.That(fixture.Immortal.Parent, Is.EqualTo(Some fixture.Root.Reference))
            Assert.That(fixture.Immortal.IssuedBy, Is.EqualTo(Some fixture.Grantor.Reference))
            Assert.That(narrowed.Parent, Is.EqualTo(Some fixture.Immortal.Reference))
            Assert.That(World.capabilities nextWorld, Does.Contain narrowed)))

    [<Test>]
    member _.``D6_C4 liveness scoped outbound grant and descendants end at Terminus`` () =
        let fixture = prepare()
        let liveDescendant, withDescendant =
            World.delegateCapability (name "Example:D6.LiveDescendant") fixture.Holder.Reference
                fixture.DescendantHolder.Reference fixture.LiveOutbound.Reference [] fixture.World |> get
        let record, world =
            World.terminateActor fixture.PolicyActor.Reference fixture.Grantor.Reference "relationship ended" (mark 1L) withDescendant |> get
        let mutable effects = 0
        let direct = World.stepDraft08 (environment (fun () -> effects <- effects + 1)) world (request fixture fixture.Holder fixture.LiveOutbound)
        let descendant = World.stepDraft08 (environment (fun () -> effects <- effects + 1)) world (request fixture fixture.DescendantHolder liveDescendant)
        let delegation =
            World.delegateCapability (name "Example:D6.Invalid") fixture.Grantor.Reference fixture.Holder.Reference
                fixture.Root.Reference [] world
        Assert.Multiple(Action(fun () ->
            Assert.That(direct.Outcome.Status, Is.EqualTo Denied)
            Assert.That(descendant.Outcome.Status, Is.EqualTo Denied)
            Assert.That(effects, Is.Zero)
            Assert.That(record.OutboundGrantsExtinguished, Does.Contain fixture.LiveOutbound.Reference)
            Assert.That(World.tryFindLivenessLease fixture.Lease.Reference world |> Option.get |> _.Dead, Is.True)
            Assert.That(delegation |> Result.isError, Is.True)))
