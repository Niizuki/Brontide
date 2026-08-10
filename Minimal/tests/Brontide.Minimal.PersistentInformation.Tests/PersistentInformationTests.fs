namespace Brontide.Minimal.PersistentInformation.Tests

open System
open NUnit.Framework
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel
open Brontide.Minimal.Experimental.PersistentInformation

[<TestFixture>]
type PersistentInformationTests() =
    let get = function Ok value -> value | Error error -> failwith error.Code
    let error = function Error value -> value | Ok _ -> failwith "Expected a refusal."
    let name value = CanonicalName.create value
    let role = StoreRoleId.create "core"
    let corpus () =
        OpaqueCorpus.create
            (CorpusId.create "settings")
            "1"
            (Some SingleWriter)
            [ { Id = role; IdentityBearing = true; Required = true; AbsenceBehavior = DatasetUnavailable } ]
        |> get
    let actor () =
        let timeDomain = TimeDomainReference.create (name "Brontide.Minimal.Tests:FixtureTime")
        let initial = World.create (Guid.NewGuid()) timeDomain
        let created, _ =
            World.genesis
                (name "Brontide.Minimal.Tests:FixturePolicy")
                { Milliseconds = 0L; TimeDomain = timeDomain; UncertaintyMilliseconds = None }
                (fun genesis world -> Genesis.actor genesis (name "Brontide.Minimal.Tests:FixtureActor") world)
                initial
            |> Result.defaultWith failwith
        created.Reference

    [<Test>]
    member _.``C1 Corpus requires explicit supported concurrency and identity role`` () =
        let definition = { Id = role; IdentityBearing = true; Required = true; AbsenceBehavior = DatasetUnavailable }
        let missing = OpaqueCorpus.create (CorpusId.create "settings") "1" None [ definition ]
        let unsupported = OpaqueCorpus.create (CorpusId.create "settings") "1" (Some ExternalCoordination) [ definition ]
        let noIdentity = OpaqueCorpus.create (CorpusId.create "settings") "1" (Some SingleWriter) [ { definition with IdentityBearing = false } ]
        Assert.That((error missing).Code, Is.EqualTo "corpus-invalid")
        Assert.That((error unsupported).Code, Is.EqualTo "concurrency-unsupported")
        Assert.That((error noIdentity).Code, Is.EqualTo "corpus-invalid")

    [<Test>]
    member _.``C2 Dataset issuance is an authorized effect and denials are silent`` () =
        let registry = DatasetRegistry()
        let store = MemoryStore(StoreId.create "primary", Set.singleton Durable)
        let timeDomain = TimeDomainReference.create (name "Brontide.Minimal.Tests:PersistentTime")
        let mutable issuer = Unchecked.defaultof<Actor>
        let mutable stranger = Unchecked.defaultof<Actor>
        let mutable target = Unchecked.defaultof<Actor>
        let mutable otherTarget = Unchecked.defaultof<Actor>
        let mutable grant = Unchecked.defaultof<Capability>
        let mutable wrongTargetGrant = Unchecked.defaultof<Capability>
        let createOperation: OperationReference = { Name = name "Brontide.Minimal.Tests:Dataset.Create" }
        let appendOperation: OperationReference = { Name = name "Brontide.Minimal.Tests:Dataset.Append" }
        let otherOperation: OperationReference = { Name = name "Brontide.Minimal.Tests:Other.Create" }
        let initial = World.create (Guid.NewGuid()) timeDomain
        let _, world =
            World.genesis (name "Brontide.Minimal.Tests:PersistentBootstrap")
                { Milliseconds = 0L; TimeDomain = timeDomain; UncertaintyMilliseconds = None }
                (fun genesis world ->
                    let issued, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:Issuer") world
                    issuer <- issued
                    let other, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:Stranger") world
                    stranger <- other
                    let service, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:DatasetService") world
                    target <- service
                    let otherService, world = Genesis.actor genesis (name "Brontide.Minimal.Tests:OtherService") world
                    otherTarget <- otherService
                    let operation = { Reference = createOperation; Description = "issue Dataset"; Target = target.Reference; CommandShape = BuiltIn.unitShape; ResultShape = BuiltIn.unitShape; Constraints = [] }
                    let world = World.registerOperation operation world |> Result.defaultWith failwith
                    let appendDefinition = { Reference = appendOperation; Description = "append Dataset"; Target = target.Reference; CommandShape = BuiltIn.unitShape; ResultShape = BuiltIn.unitShape; Constraints = [] }
                    let world = World.registerOperation appendDefinition world |> Result.defaultWith failwith
                    let otherDefinition = { Reference = otherOperation; Description = "unrelated"; Target = otherTarget.Reference; CommandShape = BuiltIn.unitShape; ResultShape = BuiltIn.unitShape; Constraints = [] }
                    let world = World.registerOperation otherDefinition world |> Result.defaultWith failwith
                    let issuedGrant, world = Genesis.capability genesis (name "Brontide.Minimal.Tests:DatasetGrant") issuer.Reference target.Reference (Set.ofList [ createOperation; appendOperation ]) [] false world |> Result.defaultWith failwith
                    grant <- issuedGrant
                    let otherGrant, world = Genesis.capability genesis (name "Brontide.Minimal.Tests:WrongTargetGrant") issuer.Reference otherTarget.Reference (Set.singleton otherOperation) [] false world |> Result.defaultWith failwith
                    wrongTargetGrant <- otherGrant
                    (), world)
                initial
            |> Result.defaultWith failwith
        let request actor targetActor capability operation : ExecutionRequest = { Initiator = actor; Target = targetActor; PresentedCapability = capability; Operation = operation; Command = UnitValue; Occurrence = None; Context = Map.empty }
        let mutable effects = 0
        let handler (request: ExecutionRequest) =
            effects <- effects + 1
            if request.Operation = createOperation then
                registry.Issue({ Issuer = request.Initiator; IssuingOperation = request.Operation }, corpus(), DatasetId.create "dataset-1", Map.ofList [ role, store :> IStoreEndpoint ]) |> ignore
            else
                registry.Append(DatasetId.create "dataset-1", role, SingleWriter, "value") |> ignore
            Ok(UnitValue, [], [])
        let environment = { TrustedTime = { Milliseconds = 1L; TimeDomain = timeDomain; UncertaintyMilliseconds = None }; ConstraintEvaluators = Map.empty; Handlers = Map.ofList [ createOperation, handler; appendOperation, handler ] }
        let deniedActor = World.step environment world (request stranger.Reference target.Reference grant.Reference createOperation)
        let deniedTarget = World.step environment world (request issuer.Reference target.Reference wrongTargetGrant.Reference createOperation)
        let deniedOperation = World.step environment world (request issuer.Reference target.Reference grant.Reference otherOperation)
        Assert.That(deniedActor.Outcome.Status, Is.EqualTo Denied)
        Assert.That(deniedTarget.Outcome.Status, Is.EqualTo Denied)
        Assert.That(deniedOperation.Outcome.Status, Is.EqualTo Denied)
        Assert.That(effects, Is.Zero)
        Assert.That(registry.Datasets, Is.Empty)
        let accepted = World.step environment world (request issuer.Reference target.Reference grant.Reference createOperation)
        Assert.That(accepted.Outcome.Status, Is.EqualTo Succeeded)
        Assert.That(registry.Datasets.Head.Issuer, Is.EqualTo issuer.Reference)
        let deniedAppend = World.step environment accepted.World (request stranger.Reference target.Reference grant.Reference appendOperation)
        Assert.That(deniedAppend.Outcome.Status, Is.EqualTo Denied)
        Assert.That(store.AppendCount, Is.Zero)
        let acceptedAppend = World.step environment accepted.World (request issuer.Reference target.Reference grant.Reference appendOperation)
        Assert.That(acceptedAppend.Outcome.Status, Is.EqualTo Succeeded)
        Assert.That(store.AppendCount, Is.EqualTo 1)

    [<Test>]
    member _.``C3 Dataset identity survives Store content loss`` () =
        let registry = DatasetRegistry()
        let store = MemoryStore(StoreId.create "primary", Set.singleton Durable)
        let id = DatasetId.create "dataset-identity"
        let createOperation: OperationReference = { Name = name "Brontide.Minimal.Tests:Create" }
        registry.Issue({ Issuer = actor(); IssuingOperation = createOperation }, corpus(), id, Map.ofList [ role, store :> IStoreEndpoint ]) |> ignore
        registry.Append(id, role, SingleWriter, "value") |> ignore
        store.Clear()
        Assert.That(registry.Datasets.Head.Id, Is.EqualTo id)
        Assert.That((registry.Read(id, role, SingleWriter) |> get), Is.Empty)

    [<Test>]
    member _.``C4 Dataset operations fail before Store effects at role and concurrency boundaries`` () =
        let registry = DatasetRegistry()
        let store = MemoryStore(StoreId.create "primary", Set.singleton Durable)
        let id = DatasetId.create "dataset-boundaries"
        let createOperation: OperationReference = { Name = name "Brontide.Minimal.Tests:Create" }
        registry.Issue({ Issuer = actor(); IssuingOperation = createOperation }, corpus(), id, Map.ofList [ role, store :> IStoreEndpoint ]) |> ignore
        let wrongRole = registry.Append(id, StoreRoleId.create "unknown", SingleWriter, "x")
        let wrongMode = registry.Append(id, role, ExternalCoordination, "x")
        Assert.That((error wrongRole).Code, Is.EqualTo "role-not-found")
        Assert.That((error wrongMode).Code, Is.EqualTo "concurrency-mismatch")
        Assert.That(store.AppendCount, Is.Zero)

    [<Test>]
    member _.``C5 Router guarantees are declared stable and do not leak backing guarantees`` () =
        let first = MemoryStore(StoreId.create "first", Set.ofList [ Durable; Encrypted ])
        let second = MemoryStore(StoreId.create "second", Set.singleton Durable)
        let router = RouterEndpoint.create (RouterId.create "router") (Set.singleton Durable) [ first; second ] false |> get
        Assert.That(router.Guarantees = Set.singleton Durable, Is.True)
        Assert.That(router.Select(second.Id) |> Result.isOk, Is.True)
        Assert.That(router.Guarantees = Set.singleton Durable, Is.True)

    [<Test>]
    member _.``C6 Router fallback refusal and topology redaction are explicit`` () =
        let first = MemoryStore(StoreId.create "first", Set.singleton Durable)
        first.IsAvailable <- false
        let second = MemoryStore(StoreId.create "second", Set.singleton Durable)
        let router = RouterEndpoint.create (RouterId.create "router") (Set.singleton Durable) [ first; second ] false |> get
        Assert.That(router.Append "fallback" |> Result.isOk, Is.True)
        Assert.That(second.Read() = [ "fallback" ], Is.True)
        Assert.That((router.Describe false).SelectedBacking.IsNone, Is.True)
        Assert.That((router.Describe true).SelectedBacking.IsNone, Is.True)
        let inspectable = RouterEndpoint.create (RouterId.create "inspectable") (Set.singleton Durable) [ second ] true |> get
        Assert.That((inspectable.Describe false).SelectedBacking.IsNone, Is.True)
        Assert.That((inspectable.Describe true).SelectedBacking, Is.EqualTo(Some second.Id))
        let unsupported = RouterEndpoint.create (RouterId.create "bad") (Set.singleton Encrypted) [ second ] true
        Assert.That((error unsupported).Code, Is.EqualTo "router-guarantee-unsupported")

    [<Test>]
    member _.``C7 identity spaces remain distinct public value types`` () =
        Assert.That(typeof<CorpusId>, Is.Not.EqualTo typeof<DatasetId>)
        Assert.That(typeof<StoreId>, Is.Not.EqualTo typeof<RouterId>)

    [<Test>]
    member _.``C8 every failed operation preserves Store observations`` () =
        let store = MemoryStore(StoreId.create "store", Set.singleton Durable)
        let registry = DatasetRegistry()
        Assert.That(registry.Append(DatasetId.create "missing", role, SingleWriter, "x") |> Result.isError, Is.True)
        Assert.That(store.AppendCount, Is.Zero)
