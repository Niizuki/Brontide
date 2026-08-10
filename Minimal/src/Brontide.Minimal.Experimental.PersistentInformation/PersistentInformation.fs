namespace Brontide.Minimal.Experimental.PersistentInformation

open System
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel

module private Identity =
    let validate parameterName (value: string) =
        if String.IsNullOrWhiteSpace value || value <> value.Trim() then
            invalidArg parameterName "An identity must be non-empty and cannot be padded."
        value

[<Struct; StructuralEquality; StructuralComparison>]
type CorpusId = private CorpusId of string

[<RequireQualifiedAccess>]
module CorpusId =
    let create value = CorpusId(Identity.validate (nameof value) value)
    let value (CorpusId value) = value

[<Struct; StructuralEquality; StructuralComparison>]
type DatasetId = private DatasetId of string

[<RequireQualifiedAccess>]
module DatasetId =
    let create value = DatasetId(Identity.validate (nameof value) value)
    let value (DatasetId value) = value

[<Struct; StructuralEquality; StructuralComparison>]
type StoreRoleId = private StoreRoleId of string

[<RequireQualifiedAccess>]
module StoreRoleId =
    let create value = StoreRoleId(Identity.validate (nameof value) value)
    let value (StoreRoleId value) = value

[<Struct; StructuralEquality; StructuralComparison>]
type StoreId = private StoreId of string

[<RequireQualifiedAccess>]
module StoreId =
    let create value = StoreId(Identity.validate (nameof value) value)
    let value (StoreId value) = value

[<Struct; StructuralEquality; StructuralComparison>]
type RouterId = private RouterId of string

[<RequireQualifiedAccess>]
module RouterId =
    let create value = RouterId(Identity.validate (nameof value) value)
    let value (RouterId value) = value

type ConcurrentAccessMode =
    | SingleWriter
    | ExternalCoordination

type StoreRoleAbsenceBehavior =
    | DatasetUnavailable
    | RoleUnavailable

type EndpointGuarantee =
    | Durable
    | Encrypted
    | Local

type StoreRoleDefinition =
    { Id: StoreRoleId
      IdentityBearing: bool
      Required: bool
      AbsenceBehavior: StoreRoleAbsenceBehavior }

type PersistentFailure =
    { Code: string
      Reason: string }

module private Refusal =
    let create code reason = Error { Code = code; Reason = reason }

type OpaqueCorpus =
    private
        { Id: CorpusId
          Version: string
          ConcurrentAccess: ConcurrentAccessMode
          Roles: StoreRoleDefinition list }

[<RequireQualifiedAccess>]
module OpaqueCorpus =
    let create id version concurrentAccess roles =
        match concurrentAccess with
        | None -> Refusal.create "corpus-invalid" "A Corpus requires an explicit concurrent-access declaration."
        | Some ExternalCoordination ->
            Refusal.create "concurrency-unsupported" "This experiment enforces only single-writer access."
        | Some SingleWriter ->
            if
                String.IsNullOrWhiteSpace version
                || List.isEmpty roles
                || (roles |> List.map (fun (role: StoreRoleDefinition) -> role.Id) |> List.distinct |> List.length) <> List.length roles
                || not (roles |> List.exists (fun (role: StoreRoleDefinition) -> role.IdentityBearing))
            then
                Refusal.create "corpus-invalid" "A Corpus requires distinct Store roles and at least one identity-bearing role."
            else
                Ok
                    { Id = id
                      Version = version
                      ConcurrentAccess = SingleWriter
                      Roles = roles }

type IStoreEndpoint =
    abstract Guarantees: Set<EndpointGuarantee>
    abstract IsAvailable: bool
    abstract Append: string -> Result<int, PersistentFailure>
    abstract Read: unit -> Result<string list, PersistentFailure>

type MemoryStore(id: StoreId, guarantees: Set<EndpointGuarantee>) =
    let values = ResizeArray<string>()
    let mutable available = true
    let mutable appendCount = 0

    member _.Id = id
    member _.Guarantees = guarantees
    member _.AppendCount = appendCount
    member _.IsAvailable with get () = available and set value = available <- value
    member _.Read() = values |> Seq.toList
    member _.Clear() = values.Clear()

    interface IStoreEndpoint with
        member _.Guarantees = guarantees
        member _.IsAvailable = available
        member _.Append value =
            if not available then
                Refusal.create "store-unavailable" $"Store '{StoreId.value id}' is unavailable."
            else
                values.Add value
                appendCount <- appendCount + 1
                Ok values.Count
        member _.Read() =
            if not available then
                Refusal.create "store-unavailable" $"Store '{StoreId.value id}' is unavailable."
            else
                Ok(values |> Seq.toList)

type DatasetIssuance =
    { Issuer: ActorReference
      IssuingOperation: OperationReference }

type DatasetRecord =
    { Id: DatasetId
      Corpus: CorpusId
      CorpusVersion: string
      Issuer: ActorReference
      IssuingOperation: OperationReference
      ConcurrentAccess: ConcurrentAccessMode
      RoleBindings: Map<StoreRoleId, IStoreEndpoint>
      IdentityBearingRoles: Set<StoreRoleId> }

type DatasetAuthorityIssuance =
    { Execution: ExecutionReference
      ProviderAuthority: CapabilityReference
      Dataset: DatasetRecord
      ResourceCapability: Capability }

[<RequireQualifiedAccess>]
module DatasetAuthority =
    let constraintName = CanonicalName.create "Brontide.Experimental.PersistentInformation:DatasetAuthority"

    let constraintDeclaration: ConstraintDeclaration =
        { Name = constraintName
          Version = 1
          ValueShape = BuiltIn.textShape
          EvaluationSemantics = "the addressed Dataset is within the provider's declared Dataset space"
          EvaluatorDomain = ConstraintEvaluatorDomain.TargetAuthority
          UnknownBehavior = ConstraintUnknownBehavior.Deny
          AccountingScope = ConstraintAccountingScope.NotQuantified
          EvolutionPolicy = ConstraintEvolutionPolicy.ParallelCanonicalName }

    let private requirement (definition: ConstraintDefinition) value =
        if definition.Name <> constraintName then
            invalidArg (nameof definition) "The Constraint definition is not Dataset authority."
        { Constraint = definition.Reference
          ParameterShape = definition.ParameterShape
          Parameters = TextValue value }

    let spaceRequirement definition prefix =
        requirement definition ("space:" + Identity.validate (nameof prefix) prefix)

    let resourceRequirement definition dataset =
        requirement definition ("dataset:" + DatasetId.value dataset)

    let allows dataset requirement =
        match requirement.Parameters with
        | TextValue value when value.StartsWith("space:", StringComparison.Ordinal) ->
            (DatasetId.value dataset).StartsWith(value.Substring(6), StringComparison.Ordinal)
        | TextValue value when value.StartsWith("dataset:", StringComparison.Ordinal) ->
            String.Equals(DatasetId.value dataset, value.Substring(8), StringComparison.Ordinal)
        | _ -> false

type DatasetRegistry() =
    let mutable datasets = Map.empty<DatasetId, DatasetRecord>

    member _.Datasets = datasets |> Map.toList |> List.map snd

    member _.Issue(issuance: DatasetIssuance, corpus: OpaqueCorpus, dataset, bindings: Map<StoreRoleId, IStoreEndpoint>) =
        if Map.containsKey dataset datasets then
            Refusal.create "dataset-invalid" $"Dataset '{DatasetId.value dataset}' already exists."
        elif corpus.Roles |> List.exists (fun role -> role.Required && not (Map.containsKey role.Id bindings)) then
            Refusal.create "role-unavailable" "A required Store role has no logical endpoint."
        elif bindings |> Map.exists (fun role _ -> corpus.Roles |> List.forall (fun declared -> declared.Id <> role)) then
            Refusal.create "role-not-found" "A binding names a role the Corpus does not declare."
        else
            let record =
                { Id = dataset
                  Corpus = corpus.Id
                  CorpusVersion = corpus.Version
                  Issuer = issuance.Issuer
                  IssuingOperation = issuance.IssuingOperation
                  ConcurrentAccess = corpus.ConcurrentAccess
                  RoleBindings = bindings
                  IdentityBearingRoles =
                    corpus.Roles
                    |> List.filter _.IdentityBearing
                    |> List.map _.Id
                    |> Set.ofList }
            datasets <- Map.add dataset record datasets
            Ok record

    member this.IssueWithAuthority(
        environment: Environment,
        world: World,
        request: Draft08ExecutionRequest,
        providerAuthority: Capability,
        resourceCapabilityName: CanonicalName,
        corpus: OpaqueCorpus,
        dataset: DatasetId,
        bindings: Map<StoreRoleId, IStoreEndpoint>) =
        let validateDataset () =
            if Map.containsKey dataset datasets then
                Refusal.create "dataset-invalid" $"Dataset '{DatasetId.value dataset}' already exists."
            elif corpus.Roles |> List.exists (fun role -> role.Required && not (Map.containsKey role.Id bindings)) then
                Refusal.create "role-unavailable" "A required Store role has no logical endpoint."
            elif bindings |> Map.exists (fun role _ -> corpus.Roles |> List.forall (fun declared -> declared.Id <> role)) then
                Refusal.create "role-not-found" "A binding names a role the Corpus does not declare."
            else Ok()

        let providerScope () =
            match World.tryFindCapability providerAuthority.Reference world with
            | None -> Refusal.create "dataset-authority-invalid" "Provider authority is not registered by this authority domain."
            | Some registered when registered <> providerAuthority ->
                Refusal.create "dataset-authority-invalid" "Provider authority does not match the registered Capability."
            | Some _ when providerAuthority.Holder <> request.Request.Target || providerAuthority.Target <> request.Request.Target ->
                Refusal.create "dataset-authority-invalid" "Provider authority must be held by and target the creating provider."
            | Some _ ->
                match World.capabilityDerivationChain providerAuthority.Reference world with
                | Error message -> Refusal.create "dataset-authority-invalid" message
                | Ok chain ->
                    let scopes =
                        chain
                        |> List.collect (fun capability ->
                            World.capabilityConstraintExpressions capability.Reference world
                            |> Option.defaultValue []
                            |> List.collect ConstraintExpression.atoms)
                        |> List.choose (fun requirement ->
                            match World.tryFindConstraint requirement.Constraint world with
                            | Some definition when definition.Name = DatasetAuthority.constraintName -> Some requirement
                            | _ -> None)
                    if scopes |> List.forall (DatasetAuthority.allows dataset) then Ok()
                    else Refusal.create "dataset-authority-exceeded" "The requested Dataset is outside the provider's effective resource-space authority."

        match validateDataset(), providerScope() with
        | Error failure, _ | _, Error failure -> Error failure
        | Ok(), Ok() ->
            let stepped = World.stepDraft08 environment world request
            if stepped.Outcome.Status <> Succeeded then
                Refusal.create "dataset-creation-refused" (stepped.Outcome.Reason |> Option.defaultValue "The creating Execution did not succeed.")
            else
                let scopeDefinition =
                    World.tryFindConstraintByName DatasetAuthority.constraintName stepped.World
                    |> Option.defaultWith (fun () -> invalidOp "The Dataset-authority Constraint declaration is missing.")
                match
                    World.delegateCapability
                        resourceCapabilityName
                        request.Request.Target
                        request.Request.Initiator
                        providerAuthority.Reference
                        [ DatasetAuthority.resourceRequirement scopeDefinition dataset ]
                        stepped.World
                with
                | Error message -> Refusal.create "dataset-authority-invalid" message
                | Ok(resourceCapability, nextWorld) ->
                    match this.Issue(
                        { Issuer = request.Request.Target; IssuingOperation = request.Request.Operation },
                        corpus, dataset, bindings) with
                    | Error failure -> Error failure
                    | Ok record ->
                        Ok(
                            { Execution = stepped.Outcome.Execution
                              ProviderAuthority = providerAuthority.Reference
                              Dataset = record
                              ResourceCapability = resourceCapability },
                            nextWorld)

    member private _.Resolve(dataset, role, requestedConcurrency) =
        match Map.tryFind dataset datasets with
        | None -> Refusal.create "dataset-not-found" $"Dataset '{DatasetId.value dataset}' is unknown."
        | Some record when requestedConcurrency <> record.ConcurrentAccess ->
            Refusal.create "concurrency-mismatch" "The requested access mode differs from the Corpus declaration."
        | Some record ->
            match Map.tryFind role record.RoleBindings with
            | None -> Refusal.create "role-not-found" $"Role '{StoreRoleId.value role}' is not bound."
            | Some endpoint -> Ok endpoint

    member this.Append(dataset, role, requestedConcurrency, value) =
        match this.Resolve(dataset, role, requestedConcurrency) with
        | Error failure -> Error failure
        | Ok endpoint -> endpoint.Append value

    member this.Read(dataset, role, requestedConcurrency) =
        match this.Resolve(dataset, role, requestedConcurrency) with
        | Error failure -> Error failure
        | Ok endpoint -> endpoint.Read()

type RouterDescription =
    { Id: RouterId
      Guarantees: Set<EndpointGuarantee>
      SelectedBacking: StoreId option }

type RouterEndpoint private (id, guarantees: Set<EndpointGuarantee>, backings: MemoryStore list, exposeTopology) =
    let mutable selected = 0

    member _.Id = id
    member _.Guarantees = guarantees

    member _.Select(store: StoreId) =
        match backings |> List.tryFindIndex (fun candidate -> candidate.Id = store) with
        | None -> Refusal.create "router-invalid" $"Store '{StoreId.value store}' is not a declared backing."
        | Some index ->
            selected <- index
            Ok()

    member _.Describe managementAuthorized =
        { Id = id
          Guarantees = guarantees
          SelectedBacking =
            if managementAuthorized && exposeTopology then Some backings[selected].Id else None }

    member private _.OrderedBackings =
        backings[selected] :: (backings |> List.indexed |> List.choose (fun (index, store) -> if index = selected then None else Some store))

    member this.Append value =
        match this.OrderedBackings |> List.tryFind _.IsAvailable with
        | None -> Refusal.create "store-unavailable" "No declared Router backing is available."
        | Some store -> (store :> IStoreEndpoint).Append value

    member this.Read() =
        match this.OrderedBackings |> List.tryFind _.IsAvailable with
        | None -> Refusal.create "store-unavailable" "No declared Router backing is available."
        | Some store -> (store :> IStoreEndpoint).Read()

    interface IStoreEndpoint with
        member this.Guarantees = guarantees
        member this.IsAvailable = this.OrderedBackings |> List.exists _.IsAvailable
        member this.Append value = this.Append value
        member this.Read() = this.Read()

    static member internal Create(id, guarantees, backings, exposeTopology) =
        RouterEndpoint(id, guarantees, backings, exposeTopology)

[<RequireQualifiedAccess>]
module RouterEndpoint =
    let create id guarantees (backings: MemoryStore list) exposeTopology =
        if
            List.isEmpty backings
            || (backings |> List.map _.Id |> List.distinct |> List.length) <> List.length backings
        then
            Refusal.create "router-invalid" "A Router requires distinct declared backing Stores."
        elif backings |> List.exists (fun store -> not (Set.isSubset guarantees store.Guarantees)) then
            Refusal.create "router-guarantee-unsupported" "Every backing and fallback path must uphold every Router guarantee."
        else
            Ok(RouterEndpoint.Create(id, guarantees, backings, exposeTopology))
