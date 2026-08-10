namespace Brontide.Minimal.Conformance

open System
open NUnit.Framework
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel
open Brontide.Minimal.Experimental.PersistentInformation

module private D5Helpers =
    let name value = CanonicalName.create value
    let get = function Ok value -> value | Error failure -> failwith (string failure)
    let timeDomain = TimeDomainReference.create (name "Brontide.Minimal.Tests:Draft08.D5.Clock")
    let mark milliseconds = { Milliseconds = milliseconds; TimeDomain = timeDomain; UncertaintyMilliseconds = None }
    let createOperation: OperationReference = { Name = name "Example:Dataset.Create.D5" }
    let useOperation: OperationReference = { Name = name "Example:Dataset.Use.D5" }
    let role = StoreRoleId.create "core"
    let corpus () =
        OpaqueCorpus.create
            (CorpusId.create "settings") "1" (Some SingleWriter)
            [ { Id = role; IdentityBearing = true; Required = true; AbsenceBehavior = DatasetUnavailable } ]
        |> get

    type Fixture =
        { World: World
          Requester: Actor
          Provider: Actor
          CreateGrant: Capability
          ProviderAuthority: Capability
          ScopeConstraint: ConstraintDefinition }

    let prepare () =
        let initial = World.create (Guid.NewGuid()) timeDomain
        let built, world =
            World.genesis
                (name "Example:D5.Policy") (mark 0L)
                (fun genesis world ->
                    let requester, world = Genesis.actor genesis (name "Example:Requester") world
                    let provider, world = Genesis.actor genesis (name "Example:DatasetProvider") world
                    let scopeConstraint, world =
                        World.registerConstraintDeclaration DatasetAuthority.constraintDeclaration world |> get
                    let world =
                        World.registerOperation
                            { Reference = createOperation; Description = "create Dataset"; Target = provider.Reference
                              CommandShape = BuiltIn.unitShape; ResultShape = BuiltIn.unitShape; Constraints = [] } world |> get
                    let world =
                        World.registerOperation
                            { Reference = useOperation; Description = "use Dataset"; Target = provider.Reference
                              CommandShape = BuiltIn.unitShape; ResultShape = BuiltIn.unitShape; Constraints = [] } world |> get
                    let createGrant, world =
                        Genesis.capability genesis (name "Example:D5.Create") requester.Reference provider.Reference
                            (Set.singleton createOperation) [] world |> get
                    let providerAuthority, world =
                        Genesis.capability genesis (name "Example:D5.ProviderAuthority") provider.Reference provider.Reference
                            (Set.singleton useOperation)
                            [ DatasetAuthority.spaceRequirement scopeConstraint "tenant/" ] world |> get
                    (requester, provider, createGrant, providerAuthority, scopeConstraint), world)
                initial |> get
        let requester, provider, createGrant, providerAuthority, scopeConstraint = built
        { World = world; Requester = requester; Provider = provider; CreateGrant = createGrant
          ProviderAuthority = providerAuthority; ScopeConstraint = scopeConstraint }

    let request fixture =
        { Request =
            { Initiator = fixture.Requester.Reference; Target = fixture.Provider.Reference
              PresentedCapability = fixture.CreateGrant.Reference; Operation = createOperation
              Command = UnitValue; Occurrence = None; Context = Map.empty }
          RequestedOrigin = OriginClass.Unverified
          PresentedCommandShape = BuiltIn.unitShape }

    let environment effects =
        { TrustedTime = mark 1L
          ConstraintEvaluators = Map.empty
          Handlers = Map.ofList [ createOperation, fun _ -> effects(); Ok(UnitValue, [], []) ] }

open D5Helpers

[<TestFixture>]
type Architecture08D5Tests() =

    [<Test>]
    member _.``D5_C1 BR_08_ADV_C10_001 creation derives resource authority from provider`` () =
        let fixture = prepare()
        let registry = DatasetRegistry()
        let store = MemoryStore(StoreId.create "primary", Set.singleton Durable)
        let result =
            registry.IssueWithAuthority(
                environment ignore, fixture.World, request fixture, fixture.ProviderAuthority,
                name "Example:D5.Resource.Orders", corpus(), DatasetId.create "tenant/orders",
                Map.ofList [ role, store :> IStoreEndpoint ]) |> get
        let issuance, _ = result
        Assert.Multiple(Action(fun () ->
            Assert.That(issuance.ResourceCapability.Parent, Is.EqualTo(Some fixture.ProviderAuthority.Reference))
            Assert.That(issuance.ResourceCapability.Holder, Is.EqualTo fixture.Requester.Reference)
            Assert.That(issuance.ResourceCapability.Target, Is.EqualTo fixture.Provider.Reference)
            Assert.That(issuance.ResourceCapability.Operations = Set.singleton useOperation, Is.True)))

    [<Test>]
    member _.``D5_C2 BR_08_ADV_C10_001 issuance is an attributable delegation record`` () =
        let fixture = prepare()
        let registry = DatasetRegistry()
        let before = World.capabilities fixture.World |> List.length
        let issuance, world =
            registry.IssueWithAuthority(
                environment ignore, fixture.World, request fixture, fixture.ProviderAuthority,
                name "Example:D5.Resource.Profile", corpus(), DatasetId.create "tenant/profile",
                Map.ofList [ role, MemoryStore(StoreId.create "primary", Set.singleton Durable) :> IStoreEndpoint ]) |> get
        let chain = World.capabilityDerivationChain issuance.ResourceCapability.Reference world |> get
        Assert.Multiple(Action(fun () ->
            Assert.That(World.capabilities world |> List.length, Is.EqualTo(before + 1))
            Assert.That(chain.Head.Parent.IsNone, Is.True)
            Assert.That(chain |> List.last, Is.EqualTo issuance.ResourceCapability)
            Assert.That(issuance.ResourceCapability.IssuedBy, Is.EqualTo(Some fixture.Provider.Reference))
            Assert.That(issuance.ProviderAuthority, Is.EqualTo fixture.ProviderAuthority.Reference)
            Assert.That(issuance.Dataset.Id, Is.EqualTo(DatasetId.create "tenant/profile"))))

    [<Test>]
    member _.``D5_C3 BR_08_ADV_C10_002 out of scope issuance refuses without resource effects`` () =
        let fixture = prepare()
        let registry = DatasetRegistry()
        let store = MemoryStore(StoreId.create "primary", Set.singleton Durable)
        let mutable effects = 0
        let result =
            registry.IssueWithAuthority(
                environment (fun () -> effects <- effects + 1), fixture.World, request fixture,
                fixture.ProviderAuthority, name "Example:D5.Resource.Other", corpus(),
                DatasetId.create "other/orders", Map.ofList [ role, store :> IStoreEndpoint ])
        let failure = match result with Error failure -> failure | Ok _ -> failwith "Expected refusal."
        Assert.Multiple(Action(fun () ->
            Assert.That(failure.Code, Is.EqualTo "dataset-authority-exceeded")
            Assert.That(effects, Is.Zero)
            Assert.That(registry.Datasets, Is.Empty)
            Assert.That(World.capabilities fixture.World |> List.length, Is.EqualTo 2)
            Assert.That(store.AppendCount, Is.Zero)))

    [<Test>]
    member _.``D5_C3 wrong holder provider authority refuses without resource effects`` () =
        let fixture = prepare()
        let registry = DatasetRegistry()
        let mutable effects = 0
        let result =
            registry.IssueWithAuthority(
                environment (fun () -> effects <- effects + 1), fixture.World, request fixture,
                fixture.CreateGrant, name "Example:D5.Resource.Invalid", corpus(),
                DatasetId.create "tenant/orders",
                Map.ofList [ role, MemoryStore(StoreId.create "primary", Set.singleton Durable) :> IStoreEndpoint ])
        let failure = match result with Error failure -> failure | Ok _ -> failwith "Expected refusal."
        Assert.Multiple(Action(fun () ->
            Assert.That(failure.Code, Is.EqualTo "dataset-authority-invalid")
            Assert.That(effects, Is.Zero)
            Assert.That(registry.Datasets, Is.Empty)
            Assert.That(World.capabilities fixture.World |> List.length, Is.EqualTo 2)))
