namespace Brontide.Minimal.Experimental.ComponentManagement

open System
open System.Security.Cryptography
open System.Text
open System.Text.Json

/// Shared syntax for the fake Component Manager's identifier spaces. Every space is a distinct
/// single-case union even though all are backed by the same lowercase token syntax, so mixing
/// spaces is a compile-time error rather than a silent bug. Construction is issuer-controlled:
/// the only public path is the validating `create`, and the backing string is exposed only at
/// the serialization seam through `value`.
module internal IdentifierSyntax =
    let require (space: string) (value: string) : string =
        if String.IsNullOrEmpty value then
            invalidArg "value" (sprintf "%s requires a non-empty value." space)
        for ch in value do
            let valid =
                (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch = '.' || ch = '-'
            if not valid then
                invalidArg
                    "value"
                    (sprintf "%s '%s' contains invalid character '%c'; use lowercase letters, digits, '.', or '-'." space value ch)
        value

type SourceId = private SourceId of string
type PublisherId = private PublisherId of string
type PackageId = private PackageId of string
type DefinitionId = private DefinitionId of string
type OccurrenceId = private OccurrenceId of string
type ActorId = private ActorId of string
type ContractId = private ContractId of string
type VersionLiteral = private VersionLiteral of string
type BindingScopeId = private BindingScopeId of string
type BindingId = private BindingId of string
type ArtifactId = private ArtifactId of string
type EvidenceId = private EvidenceId of string
type IssuerId = private IssuerId of string
type PreferenceId = private PreferenceId of string
type TopologyNodeId = private TopologyNodeId of string
type FunctionId = private FunctionId of string
type ClaimId = private ClaimId of string
type ObserverId = private ObserverId of string

[<RequireQualifiedAccess>]
module SourceId =
    let create v = SourceId(IdentifierSyntax.require "SourceId" v)
    let value (SourceId v) = v

[<RequireQualifiedAccess>]
module PublisherId =
    let create v = PublisherId(IdentifierSyntax.require "PublisherId" v)
    let value (PublisherId v) = v

[<RequireQualifiedAccess>]
module PackageId =
    let create v = PackageId(IdentifierSyntax.require "PackageId" v)
    let value (PackageId v) = v

[<RequireQualifiedAccess>]
module DefinitionId =
    let create v = DefinitionId(IdentifierSyntax.require "DefinitionId" v)
    let value (DefinitionId v) = v

[<RequireQualifiedAccess>]
module OccurrenceId =
    let create v = OccurrenceId(IdentifierSyntax.require "OccurrenceId" v)
    let value (OccurrenceId v) = v

[<RequireQualifiedAccess>]
module ActorId =
    let create v = ActorId(IdentifierSyntax.require "ActorId" v)
    let value (ActorId v) = v

[<RequireQualifiedAccess>]
module ContractId =
    let create v = ContractId(IdentifierSyntax.require "ContractId" v)
    let value (ContractId v) = v

[<RequireQualifiedAccess>]
module VersionLiteral =
    let create v = VersionLiteral(IdentifierSyntax.require "VersionLiteral" v)
    let value (VersionLiteral v) = v

[<RequireQualifiedAccess>]
module BindingScopeId =
    let create v = BindingScopeId(IdentifierSyntax.require "BindingScopeId" v)
    let value (BindingScopeId v) = v

[<RequireQualifiedAccess>]
module BindingId =
    let create v = BindingId(IdentifierSyntax.require "BindingId" v)
    let value (BindingId v) = v

[<RequireQualifiedAccess>]
module ArtifactId =
    let create v = ArtifactId(IdentifierSyntax.require "ArtifactId" v)
    let value (ArtifactId v) = v

[<RequireQualifiedAccess>]
module EvidenceId =
    let create v = EvidenceId(IdentifierSyntax.require "EvidenceId" v)
    let value (EvidenceId v) = v

[<RequireQualifiedAccess>]
module IssuerId =
    let create v = IssuerId(IdentifierSyntax.require "IssuerId" v)
    let value (IssuerId v) = v

[<RequireQualifiedAccess>]
module PreferenceId =
    let create v = PreferenceId(IdentifierSyntax.require "PreferenceId" v)
    let value (PreferenceId v) = v

[<RequireQualifiedAccess>]
module TopologyNodeId =
    let create v = TopologyNodeId(IdentifierSyntax.require "TopologyNodeId" v)
    let value (TopologyNodeId v) = v

[<RequireQualifiedAccess>]
module FunctionId =
    let create v = FunctionId(IdentifierSyntax.require "FunctionId" v)
    let value (FunctionId v) = v

[<RequireQualifiedAccess>]
module ClaimId =
    let create v = ClaimId(IdentifierSyntax.require "ClaimId" v)
    let value (ClaimId v) = v

[<RequireQualifiedAccess>]
module ObserverId =
    let create v = ObserverId(IdentifierSyntax.require "ObserverId" v)
    let value (ObserverId v) = v

/// Requirement cardinality such as `1..1` or `0..*`.
type Cardinality =
    { Minimum: int
      Maximum: int option }

[<RequireQualifiedAccess>]
module Cardinality =
    let parse (value: string) : Cardinality =
        let separator = value.IndexOf("..", StringComparison.Ordinal)
        if separator <= 0 then
            invalidArg "value" (sprintf "Cardinality '%s' must use the form 'min..max'." value)
        let minimumText = value.Substring(0, separator)
        let maximumText = value.Substring(separator + 2)
        match Int32.TryParse minimumText with
        | true, minimum when minimum >= 0 ->
            if maximumText = "*" then
                { Minimum = minimum; Maximum = None }
            else
                match Int32.TryParse maximumText with
                | true, bounded when bounded >= minimum -> { Minimum = minimum; Maximum = Some bounded }
                | _ -> invalidArg "value" (sprintf "Cardinality '%s' has an invalid maximum." value)
        | _ -> invalidArg "value" (sprintf "Cardinality '%s' has an invalid minimum." value)

type SourceKind =
    | Local
    | Remote

type ScopeKind =
    | SystemDefault
    | Application
    | Session

type EvidenceKind =
    | Integrity
    | Origin
    | Signature
    | Review

type EvidenceVerdict =
    | Accepted
    | Rejected

type TopologyNodeKind =
    | Host
    | Attachment

type TopologyRelation =
    | PartOf
    | AttachedThrough
    | HostedBy
    | SamePhysicalAssembly
    | SharesPowerDomain
    | SharesFailureDomain

type ContractEntry = { Contract: ContractId; Versions: VersionLiteral list }
type PublisherEntry = { Publisher: PublisherId; DisplayName: string }

type SourceEntry =
    { Source: SourceId
      Kind: SourceKind
      DisplayName: string
      ServesPublishers: PublisherId list }

type PackageEntry =
    { Package: PackageId
      Publisher: PublisherId
      Version: VersionLiteral
      Artifact: ArtifactId }

type AdvertisementEntry =
    { Source: SourceId
      Package: PackageId
      AdvertisedVersion: VersionLiteral }

type ProvidedContract = { Contract: ContractId; Version: VersionLiteral }

type RequiredContract =
    { Contract: ContractId
      Version: VersionLiteral
      Scope: BindingScopeId
      Cardinality: Cardinality }

type ComponentDefinitionEntry =
    { Definition: DefinitionId
      Package: PackageId
      Provides: ProvidedContract list
      Requires: RequiredContract list
      Generic: bool }

type BindingScopeEntry = { Scope: BindingScopeId; Kind: ScopeKind }

type ActivatedOccurrenceEntry =
    { Occurrence: OccurrenceId
      Definition: DefinitionId
      Actors: ActorId list }

type OccupiedBindingEntry =
    { Binding: BindingId
      Scope: BindingScopeId
      Contract: ContractId
      OccupantDefinition: DefinitionId
      OccupantOccurrence: OccurrenceId }

type PreferenceEntry =
    { Preference: PreferenceId
      DeclaredBy: DefinitionId
      Contract: ContractId
      PreferredDefinition: DefinitionId }

type ArtifactEntry = { Artifact: ArtifactId; Content: string; Sha256: string }

type EvidenceEntry =
    { Evidence: EvidenceId
      SubjectArtifact: ArtifactId
      Kind: EvidenceKind
      Issuer: IssuerId
      Verdict: EvidenceVerdict
      Detail: string }

type StorefrontEntry =
    { Source: SourceId
      Package: PackageId
      DisplayName: string
      Description: string
      Imagery: string
      Categories: string list
      Version: VersionLiteral
      Compatibility: string
      EvidenceStatus: string
      LifecycleState: string
      DependencySummary: string list
      Alternatives: PackageId list }

type CatalogExpectations =
    { DuplicateComponentIdentityAcrossSources: PackageId list
      MirroredPublishers: PublisherId list
      MultiPublisherSources: SourceId list
      ContractsWithSeveralDefinitions: ContractId list
      DefinitionsWithSeveralOccurrences: DefinitionId list
      OccupiedBindings: BindingId list
      SystemDefaultScopes: BindingScopeId list
      ExplicitPreferences: PreferenceId list
      GenericCandidates: DefinitionId list
      ConflictingVersionClaims: PackageId list
      MissingArtifacts: ArtifactId list
      ContradictoryEvidence: EvidenceId list list }

type CatalogFixture =
    { Description: string
      Contracts: ContractEntry list
      Publishers: PublisherEntry list
      Sources: SourceEntry list
      Packages: PackageEntry list
      Advertisements: AdvertisementEntry list
      ComponentDefinitions: ComponentDefinitionEntry list
      BindingScopes: BindingScopeEntry list
      ActivatedOccurrences: ActivatedOccurrenceEntry list
      OccupiedBindings: OccupiedBindingEntry list
      Preferences: PreferenceEntry list
      Artifacts: ArtifactEntry list
      Evidence: EvidenceEntry list
      Storefront: StorefrontEntry list
      Expectations: CatalogExpectations }

type ObserverEntry = { Observer: ObserverId }
type TopologyNodeEntry = { Node: TopologyNodeId; Observer: ObserverId; Kind: TopologyNodeKind }

type FunctionEntry =
    { Function: FunctionId
      Contract: ContractId
      Node: TopologyNodeId
      Actor: ActorId }

type TopologyClaimEntry =
    { Claim: ClaimId
      AssertedBy: ObserverId
      Relation: TopologyRelation
      From: TopologyNodeId
      To: TopologyNodeId }

type MiceExpectations =
    { DistinctMouseNodes: TopologyNodeId list
      FunctionsPerMouseNode: int
      AttributableClaims: ClaimId list
      ContradictoryClaims: ClaimId list list
      MaliciousClaims: ClaimId list }

type MiceTopologyFixture =
    { Description: string
      Contracts: ContractEntry list
      Observers: ObserverEntry list
      TopologyNodes: TopologyNodeEntry list
      Functions: FunctionEntry list
      Claims: TopologyClaimEntry list
      Expectations: MiceExpectations }

/// Raised when a fixture is malformed, references unknown identities, or disagrees with its own
/// declared expectations. Carries every failure so a defect report is deterministic and complete.
exception FixtureFormatException of failures: string list

/// Strict loader for the shared data-only fixtures under `component-management/fixtures`. Parsing
/// fails closed: unknown schema versions, unknown properties, duplicate identifiers, unresolved
/// references, digest mismatches, and expectation mismatches are all rejected with a complete,
/// deterministic failure list. Loading grants no Capability and establishes no Actor.
[<RequireQualifiedAccess>]
module FixtureLoader =

    type private Reader() =
        let failures = ResizeArray<string>()
        member _.HasFailures = failures.Count > 0
        member _.Fail(failure: string) = failures.Add failure

        member _.Rejection() =
            let ordered =
                failures
                |> Seq.sortWith (fun a b -> String.CompareOrdinal(a, b))
                |> Seq.toList
            FixtureFormatException(if List.isEmpty ordered then [ "unknown fixture failure" ] else ordered)

        member this.CheckProperties(element: JsonElement, path: string, allowed: string list) =
            for property in element.EnumerateObject() do
                if not (List.contains property.Name allowed) then
                    this.Fail(sprintf "%s: unknown property '%s'." path property.Name)
            for required in allowed do
                match element.TryGetProperty required with
                | true, _ -> ()
                | _ -> this.Fail(sprintf "%s: missing property '%s'." path required)

        member this.GetString(element: JsonElement, property: string) : string =
            let invalid () = invalidArg "property" (sprintf "property '%s' must be a non-empty string." property)
            match element.TryGetProperty property with
            | true, value when value.ValueKind = JsonValueKind.String ->
                match value.GetString() with
                | null | "" -> invalid ()
                | text -> text
            | _ -> invalid ()

        member this.GetInt(element: JsonElement, property: string) : int =
            match element.TryGetProperty property with
            | true, value when value.ValueKind = JsonValueKind.Number -> value.GetInt32()
            | _ -> invalidArg "property" (sprintf "property '%s' must be an integer." property)

        member this.GetBool(element: JsonElement, property: string) : bool =
            match element.TryGetProperty property with
            | true, value when value.ValueKind = JsonValueKind.True || value.ValueKind = JsonValueKind.False ->
                value.GetBoolean()
            | _ -> invalidArg "property" (sprintf "property '%s' must be a Boolean." property)

        member this.GetStringList(element: JsonElement, property: string) : string list =
            let invalidItem () = invalidArg "property" (sprintf "property '%s' must contain only non-empty strings." property)
            match element.TryGetProperty property with
            | true, value when value.ValueKind = JsonValueKind.Array ->
                [ for entry in value.EnumerateArray() ->
                      if entry.ValueKind <> JsonValueKind.String then
                          invalidItem ()
                      else
                          match entry.GetString() with
                          | null | "" -> invalidItem ()
                          | text -> text ]
            | _ -> invalidArg "property" (sprintf "property '%s' must be an array of strings." property)

        member this.GetNestedStringLists(element: JsonElement, property: string) : string list list =
            match element.TryGetProperty property with
            | true, value when value.ValueKind = JsonValueKind.Array ->
                [ for entry in value.EnumerateArray() ->
                      if entry.ValueKind <> JsonValueKind.Array then
                          invalidArg "property" (sprintf "property '%s' must contain only arrays." property)
                      [ for item in entry.EnumerateArray() ->
                            if item.ValueKind <> JsonValueKind.String then
                                invalidArg "property" (sprintf "property '%s' must contain only non-empty strings." property)
                            else
                                match item.GetString() with
                                | null | "" -> invalidArg "property" (sprintf "property '%s' must contain only non-empty strings." property)
                                | text -> text ] ]
            | _ -> invalidArg "property" (sprintf "property '%s' must be an array of string arrays." property)

        member this.GetEnum(element: JsonElement, property: string, tokens: (string * 'T) list) : 'T =
            let text = this.GetString(element, property)
            match List.tryFind (fun (token, _) -> token = text) tokens with
            | Some(_, value) -> value
            | None -> invalidArg "property" (sprintf "property '%s' has unsupported value '%s'." property text)

        member this.Attempt(path: string, parse: unit -> 'T) : 'T option =
            try
                Some(parse ())
            with :? ArgumentException as exn ->
                this.Fail(sprintf "%s: %s" path exn.Message)
                None

        member this.ParseEntries(root: JsonElement, section: string, allowed: string list, parse: JsonElement -> Reader -> 'T) : 'T list =
            match root.TryGetProperty section with
            | true, array when array.ValueKind = JsonValueKind.Array ->
                let results = ResizeArray<'T>()
                let mutable index = 0
                for element in array.EnumerateArray() do
                    if element.ValueKind <> JsonValueKind.Object then
                        this.Fail(sprintf "%s[%d]: entry must be an object." section index)
                    else
                        this.CheckProperties(element, sprintf "%s[%d]" section index, allowed)
                        match this.Attempt(sprintf "%s[%d]" section index, fun () -> parse element this) with
                        | Some entry -> results.Add entry
                        | None -> ()
                    index <- index + 1
                List.ofSeq results
            | _ ->
                this.Fail(sprintf "%s: section must be an array." section)
                []

        member this.ParseNested(parent: JsonElement, property: string, allowed: string list, parse: JsonElement -> Reader -> 'T) : 'T list =
            match parent.TryGetProperty property with
            | true, array when array.ValueKind = JsonValueKind.Array ->
                let results = ResizeArray<'T>()
                let mutable index = 0
                for element in array.EnumerateArray() do
                    this.CheckProperties(element, sprintf "%s[%d]" property index, allowed)
                    match this.Attempt(sprintf "%s[%d]" property index, fun () -> parse element this) with
                    | Some entry -> results.Add entry
                    | None -> ()
                    index <- index + 1
                List.ofSeq results
            | _ ->
                this.Fail(sprintf "%s: nested list must be an array." property)
                []

        member this.RequireDistinct(section: string, values: string seq) =
            values
            |> Seq.groupBy id
            |> Seq.filter (fun (_, group) -> Seq.length group > 1)
            |> Seq.iter (fun (key, _) -> this.Fail(sprintf "%s: duplicate identifier '%s'." section key))

        member this.RequireExpectation(field: string, computed: string seq, declared: string seq) =
            let order (values: string seq) =
                values
                |> Seq.distinct
                |> Seq.sortWith (fun a b -> String.CompareOrdinal(a, b))
                |> Seq.toList
            let computedSorted = order computed
            let declaredSorted = order declared
            if computedSorted <> declaredSorted then
                this.Fail(
                    sprintf
                        "expectations: '%s' declares [%s] but the data computes [%s]."
                        field
                        (String.Join(", ", declaredSorted))
                        (String.Join(", ", computedSorted)))

    let private parseRoot (reader: Reader) (json: string) (expectedFixture: string) (allowed: string list) : JsonElement option =
        let mutable parsed = Unchecked.defaultof<JsonDocument>
        let ok =
            try
                parsed <- JsonDocument.Parse json
                true
            with :? JsonException as exn ->
                reader.Fail(sprintf "fixture is not valid JSON: %s" exn.Message)
                false
        if not ok then
            None
        else
            let root = parsed.RootElement
            if root.ValueKind <> JsonValueKind.Object then
                reader.Fail "fixture root must be a JSON object."
                None
            else
                reader.CheckProperties(root, "fixture", allowed)
                let schemaOk =
                    match root.TryGetProperty "schemaVersion" with
                    | true, schema when schema.ValueKind = JsonValueKind.Number && schema.GetInt32() = 1 -> true
                    | _ ->
                        reader.Fail "fixture schemaVersion must be 1."
                        false
                let nameOk =
                    match root.TryGetProperty "fixture" with
                    | true, name when name.GetString() = expectedFixture -> true
                    | _ ->
                        reader.Fail(sprintf "fixture name must be '%s'." expectedFixture)
                        false
                if schemaOk && nameOk then Some root else None

    let private tryGetSection (reader: Reader) (root: JsonElement) (name: string) : JsonElement option =
        match root.TryGetProperty name with
        | true, section when section.ValueKind = JsonValueKind.Object -> Some section
        | _ ->
            reader.Fail(sprintf "%s: section must be an object." name)
            None

    let private hexDigest (content: string) : string =
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes content))

    let private parseContracts (reader: Reader) (root: JsonElement) =
        reader.ParseEntries(
            root,
            "contracts",
            [ "contract"; "versions" ],
            fun e r ->
                { Contract = ContractId.create (r.GetString(e, "contract"))
                  Versions = r.GetStringList(e, "versions") |> List.map VersionLiteral.create })

    let loadCatalog (json: string) : CatalogFixture =
        let reader = Reader()
        match parseRoot reader json "cm0-catalog"
                  [ "schemaVersion"; "fixture"; "description"; "contracts"; "publishers"; "sources"
                    "packages"; "advertisements"; "componentDefinitions"; "bindingScopes"
                    "activatedOccurrences"; "occupiedBindings"; "preferences"; "artifacts"; "evidence"
                    "storefront"; "expectations" ] with
        | None -> raise (reader.Rejection())
        | Some root ->
            let description =
                match reader.Attempt("description", fun () -> reader.GetString(root, "description")) with
                | Some value -> value
                | None -> ""
            let contracts = parseContracts reader root
            let publishers =
                reader.ParseEntries(
                    root, "publishers", [ "publisher"; "displayName" ],
                    fun e r -> { Publisher = PublisherId.create (r.GetString(e, "publisher")); DisplayName = r.GetString(e, "displayName") })
            let sources =
                reader.ParseEntries(
                    root, "sources", [ "source"; "kind"; "displayName"; "servesPublishers" ],
                    fun e r ->
                        { Source = SourceId.create (r.GetString(e, "source"))
                          Kind = r.GetEnum(e, "kind", [ "local", Local; "remote", Remote ])
                          DisplayName = r.GetString(e, "displayName")
                          ServesPublishers = r.GetStringList(e, "servesPublishers") |> List.map PublisherId.create })
            let packages =
                reader.ParseEntries(
                    root, "packages", [ "package"; "publisher"; "version"; "artifact" ],
                    fun e r ->
                        { Package = PackageId.create (r.GetString(e, "package"))
                          Publisher = PublisherId.create (r.GetString(e, "publisher"))
                          Version = VersionLiteral.create (r.GetString(e, "version"))
                          Artifact = ArtifactId.create (r.GetString(e, "artifact")) })
            let advertisements =
                reader.ParseEntries(
                    root, "advertisements", [ "source"; "package"; "advertisedVersion" ],
                    fun e r ->
                        { Source = SourceId.create (r.GetString(e, "source"))
                          Package = PackageId.create (r.GetString(e, "package"))
                          AdvertisedVersion = VersionLiteral.create (r.GetString(e, "advertisedVersion")) })
            let definitions =
                reader.ParseEntries(
                    root, "componentDefinitions", [ "definition"; "package"; "provides"; "requires"; "generic" ],
                    fun e r ->
                        { Definition = DefinitionId.create (r.GetString(e, "definition"))
                          Package = PackageId.create (r.GetString(e, "package"))
                          Provides =
                              r.ParseNested(
                                  e, "provides", [ "contract"; "version" ],
                                  fun p r2 -> { Contract = ContractId.create (r2.GetString(p, "contract")); Version = VersionLiteral.create (r2.GetString(p, "version")) })
                          Requires =
                              r.ParseNested(
                                  e, "requires", [ "contract"; "version"; "scope"; "cardinality" ],
                                  fun p r2 ->
                                      { Contract = ContractId.create (r2.GetString(p, "contract"))
                                        Version = VersionLiteral.create (r2.GetString(p, "version"))
                                        Scope = BindingScopeId.create (r2.GetString(p, "scope"))
                                        Cardinality = Cardinality.parse (r2.GetString(p, "cardinality")) })
                          Generic = r.GetBool(e, "generic") })
            let scopes =
                reader.ParseEntries(
                    root, "bindingScopes", [ "scope"; "kind" ],
                    fun e r ->
                        { Scope = BindingScopeId.create (r.GetString(e, "scope"))
                          Kind = r.GetEnum(e, "kind", [ "system-default", SystemDefault; "application", Application; "session", Session ]) })
            let occurrences =
                reader.ParseEntries(
                    root, "activatedOccurrences", [ "occurrence"; "definition"; "actors" ],
                    fun e r ->
                        { Occurrence = OccurrenceId.create (r.GetString(e, "occurrence"))
                          Definition = DefinitionId.create (r.GetString(e, "definition"))
                          Actors = r.GetStringList(e, "actors") |> List.map ActorId.create })
            let bindings =
                reader.ParseEntries(
                    root, "occupiedBindings", [ "binding"; "scope"; "contract"; "occupantDefinition"; "occupantOccurrence" ],
                    fun e r ->
                        { Binding = BindingId.create (r.GetString(e, "binding"))
                          Scope = BindingScopeId.create (r.GetString(e, "scope"))
                          Contract = ContractId.create (r.GetString(e, "contract"))
                          OccupantDefinition = DefinitionId.create (r.GetString(e, "occupantDefinition"))
                          OccupantOccurrence = OccurrenceId.create (r.GetString(e, "occupantOccurrence")) })
            let preferences =
                reader.ParseEntries(
                    root, "preferences", [ "preference"; "declaredBy"; "contract"; "preferredDefinition" ],
                    fun e r ->
                        { Preference = PreferenceId.create (r.GetString(e, "preference"))
                          DeclaredBy = DefinitionId.create (r.GetString(e, "declaredBy"))
                          Contract = ContractId.create (r.GetString(e, "contract"))
                          PreferredDefinition = DefinitionId.create (r.GetString(e, "preferredDefinition")) })
            let artifacts =
                reader.ParseEntries(
                    root, "artifacts", [ "artifact"; "content"; "sha256" ],
                    fun e r -> { Artifact = ArtifactId.create (r.GetString(e, "artifact")); Content = r.GetString(e, "content"); Sha256 = r.GetString(e, "sha256") })
            let evidence =
                reader.ParseEntries(
                    root, "evidence", [ "evidence"; "subjectArtifact"; "kind"; "issuer"; "verdict"; "detail" ],
                    fun e r ->
                        { Evidence = EvidenceId.create (r.GetString(e, "evidence"))
                          SubjectArtifact = ArtifactId.create (r.GetString(e, "subjectArtifact"))
                          Kind = r.GetEnum(e, "kind", [ "integrity", Integrity; "origin", Origin; "signature", Signature; "review", Review ])
                          Issuer = IssuerId.create (r.GetString(e, "issuer"))
                          Verdict = r.GetEnum(e, "verdict", [ "accepted", Accepted; "rejected", Rejected ])
                          Detail = r.GetString(e, "detail") })
            let storefront =
                reader.ParseEntries(
                    root, "storefront",
                    [ "source"; "package"; "displayName"; "description"; "imagery"; "categories"; "version"
                      "compatibility"; "evidenceStatus"; "lifecycleState"; "dependencySummary"; "alternatives" ],
                    fun e r ->
                        { Source = SourceId.create (r.GetString(e, "source"))
                          Package = PackageId.create (r.GetString(e, "package"))
                          DisplayName = r.GetString(e, "displayName")
                          Description = r.GetString(e, "description")
                          Imagery = r.GetString(e, "imagery")
                          Categories = r.GetStringList(e, "categories")
                          Version = VersionLiteral.create (r.GetString(e, "version"))
                          Compatibility = r.GetString(e, "compatibility")
                          EvidenceStatus = r.GetString(e, "evidenceStatus")
                          LifecycleState = r.GetString(e, "lifecycleState")
                          DependencySummary = r.GetStringList(e, "dependencySummary")
                          Alternatives = r.GetStringList(e, "alternatives") |> List.map PackageId.create })

            let expectations =
                match tryGetSection reader root "expectations" with
                | None -> None
                | Some element ->
                    reader.CheckProperties(
                        element, "expectations",
                        [ "duplicateComponentIdentityAcrossSources"; "mirroredPublishers"; "multiPublisherSources"
                          "contractsWithSeveralDefinitions"; "definitionsWithSeveralOccurrences"; "occupiedBindings"
                          "systemDefaultScopes"; "explicitPreferences"; "genericCandidates"
                          "conflictingVersionClaims"; "missingArtifacts"; "contradictoryEvidence" ])
                    reader.Attempt(
                        "expectations",
                        fun () ->
                            { DuplicateComponentIdentityAcrossSources = reader.GetStringList(element, "duplicateComponentIdentityAcrossSources") |> List.map PackageId.create
                              MirroredPublishers = reader.GetStringList(element, "mirroredPublishers") |> List.map PublisherId.create
                              MultiPublisherSources = reader.GetStringList(element, "multiPublisherSources") |> List.map SourceId.create
                              ContractsWithSeveralDefinitions = reader.GetStringList(element, "contractsWithSeveralDefinitions") |> List.map ContractId.create
                              DefinitionsWithSeveralOccurrences = reader.GetStringList(element, "definitionsWithSeveralOccurrences") |> List.map DefinitionId.create
                              OccupiedBindings = reader.GetStringList(element, "occupiedBindings") |> List.map BindingId.create
                              SystemDefaultScopes = reader.GetStringList(element, "systemDefaultScopes") |> List.map BindingScopeId.create
                              ExplicitPreferences = reader.GetStringList(element, "explicitPreferences") |> List.map PreferenceId.create
                              GenericCandidates = reader.GetStringList(element, "genericCandidates") |> List.map DefinitionId.create
                              ConflictingVersionClaims = reader.GetStringList(element, "conflictingVersionClaims") |> List.map PackageId.create
                              MissingArtifacts = reader.GetStringList(element, "missingArtifacts") |> List.map ArtifactId.create
                              ContradictoryEvidence = reader.GetNestedStringLists(element, "contradictoryEvidence") |> List.map (List.map EvidenceId.create) })

            match expectations with
            | None -> raise (reader.Rejection())
            | Some expectations when reader.HasFailures -> raise (reader.Rejection())
            | Some expectations ->
                reader.RequireDistinct("contracts", contracts |> Seq.map (fun c -> ContractId.value c.Contract))
                reader.RequireDistinct("publishers", publishers |> Seq.map (fun p -> PublisherId.value p.Publisher))
                reader.RequireDistinct("sources", sources |> Seq.map (fun s -> SourceId.value s.Source))
                reader.RequireDistinct("packages", packages |> Seq.map (fun p -> PackageId.value p.Package))
                reader.RequireDistinct("componentDefinitions", definitions |> Seq.map (fun d -> DefinitionId.value d.Definition))
                reader.RequireDistinct("bindingScopes", scopes |> Seq.map (fun s -> BindingScopeId.value s.Scope))
                reader.RequireDistinct("activatedOccurrences", occurrences |> Seq.map (fun o -> OccurrenceId.value o.Occurrence))
                reader.RequireDistinct("occupiedBindings", bindings |> Seq.map (fun b -> BindingId.value b.Binding))
                reader.RequireDistinct("preferences", preferences |> Seq.map (fun p -> PreferenceId.value p.Preference))
                reader.RequireDistinct("artifacts", artifacts |> Seq.map (fun a -> ArtifactId.value a.Artifact))
                reader.RequireDistinct("evidence", evidence |> Seq.map (fun e -> EvidenceId.value e.Evidence))
                reader.RequireDistinct("advertisements", advertisements |> Seq.map (fun a -> SourceId.value a.Source + "|" + PackageId.value a.Package))
                reader.RequireDistinct("storefront", storefront |> Seq.map (fun s -> SourceId.value s.Source + "|" + PackageId.value s.Package))
                reader.RequireDistinct("actors", occurrences |> Seq.collect (fun o -> o.Actors) |> Seq.map ActorId.value)

                let contractVersions = contracts |> List.map (fun c -> c.Contract, (c.Versions |> List.map VersionLiteral.value |> Set.ofList)) |> Map.ofList
                let publisherIds = publishers |> List.map (fun p -> p.Publisher) |> Set.ofList
                let sourceIds = sources |> List.map (fun s -> s.Source) |> Set.ofList
                let packagesById = packages |> List.map (fun p -> p.Package, p) |> Map.ofList
                let definitionsById = definitions |> List.map (fun d -> d.Definition, d) |> Map.ofList
                let scopeIds = scopes |> List.map (fun s -> s.Scope) |> Set.ofList
                let occurrencesById = occurrences |> List.map (fun o -> o.Occurrence, o) |> Map.ofList
                let artifactIds = artifacts |> List.map (fun a -> a.Artifact) |> Set.ofList

                for source in sources do
                    for publisher in source.ServesPublishers do
                        if not (Set.contains publisher publisherIds) then
                            reader.Fail(sprintf "sources: '%s' serves unknown publisher '%s'." (SourceId.value source.Source) (PublisherId.value publisher))
                for package in packages do
                    if not (Set.contains package.Publisher publisherIds) then
                        reader.Fail(sprintf "packages: '%s' names unknown publisher '%s'." (PackageId.value package.Package) (PublisherId.value package.Publisher))
                for advertisement in advertisements do
                    if not (Set.contains advertisement.Source sourceIds) then
                        reader.Fail(sprintf "advertisements: unknown source '%s'." (SourceId.value advertisement.Source))
                    if not (Map.containsKey advertisement.Package packagesById) then
                        reader.Fail(sprintf "advertisements: unknown package '%s'." (PackageId.value advertisement.Package))
                for definition in definitions do
                    if not (Map.containsKey definition.Package packagesById) then
                        reader.Fail(sprintf "componentDefinitions: '%s' names unknown package '%s'." (DefinitionId.value definition.Definition) (PackageId.value definition.Package))
                    for provided in definition.Provides do
                        match Map.tryFind provided.Contract contractVersions with
                        | None -> reader.Fail(sprintf "componentDefinitions: '%s' provides unknown contract '%s'." (DefinitionId.value definition.Definition) (ContractId.value provided.Contract))
                        | Some versions ->
                            if not (Set.contains (VersionLiteral.value provided.Version) versions) then
                                reader.Fail(sprintf "componentDefinitions: '%s' provides undeclared version '%s' of '%s'." (DefinitionId.value definition.Definition) (VersionLiteral.value provided.Version) (ContractId.value provided.Contract))
                    for required in definition.Requires do
                        if not (Map.containsKey required.Contract contractVersions) then
                            reader.Fail(sprintf "componentDefinitions: '%s' requires unknown contract '%s'." (DefinitionId.value definition.Definition) (ContractId.value required.Contract))
                        if not (Set.contains required.Scope scopeIds) then
                            reader.Fail(sprintf "componentDefinitions: '%s' requires unknown scope '%s'." (DefinitionId.value definition.Definition) (BindingScopeId.value required.Scope))
                for occurrence in occurrences do
                    if not (Map.containsKey occurrence.Definition definitionsById) then
                        reader.Fail(sprintf "activatedOccurrences: '%s' names unknown definition '%s'." (OccurrenceId.value occurrence.Occurrence) (DefinitionId.value occurrence.Definition))
                for binding in bindings do
                    if not (Set.contains binding.Scope scopeIds) then
                        reader.Fail(sprintf "occupiedBindings: '%s' names unknown scope '%s'." (BindingId.value binding.Binding) (BindingScopeId.value binding.Scope))
                    if not (Map.containsKey binding.Contract contractVersions) then
                        reader.Fail(sprintf "occupiedBindings: '%s' names unknown contract '%s'." (BindingId.value binding.Binding) (ContractId.value binding.Contract))
                    match Map.tryFind binding.OccupantDefinition definitionsById with
                    | None -> reader.Fail(sprintf "occupiedBindings: '%s' names unknown definition '%s'." (BindingId.value binding.Binding) (DefinitionId.value binding.OccupantDefinition))
                    | Some occupant ->
                        if occupant.Provides |> List.forall (fun p -> p.Contract <> binding.Contract) then
                            reader.Fail(sprintf "occupiedBindings: '%s' occupant '%s' does not provide '%s'." (BindingId.value binding.Binding) (DefinitionId.value binding.OccupantDefinition) (ContractId.value binding.Contract))
                    match Map.tryFind binding.OccupantOccurrence occurrencesById with
                    | None -> reader.Fail(sprintf "occupiedBindings: '%s' names unknown occurrence '%s'." (BindingId.value binding.Binding) (OccurrenceId.value binding.OccupantOccurrence))
                    | Some occurrence ->
                        if occurrence.Definition <> binding.OccupantDefinition then
                            reader.Fail(sprintf "occupiedBindings: '%s' occurrence '%s' does not realise '%s'." (BindingId.value binding.Binding) (OccurrenceId.value binding.OccupantOccurrence) (DefinitionId.value binding.OccupantDefinition))
                for preference in preferences do
                    match Map.tryFind preference.DeclaredBy definitionsById with
                    | None -> reader.Fail(sprintf "preferences: '%s' declared by unknown definition '%s'." (PreferenceId.value preference.Preference) (DefinitionId.value preference.DeclaredBy))
                    | Some declaring ->
                        if declaring.Requires |> List.forall (fun r -> r.Contract <> preference.Contract) then
                            reader.Fail(sprintf "preferences: '%s' declarer '%s' has no requirement on '%s'." (PreferenceId.value preference.Preference) (DefinitionId.value preference.DeclaredBy) (ContractId.value preference.Contract))
                    match Map.tryFind preference.PreferredDefinition definitionsById with
                    | None -> reader.Fail(sprintf "preferences: '%s' prefers unknown definition '%s'." (PreferenceId.value preference.Preference) (DefinitionId.value preference.PreferredDefinition))
                    | Some preferred ->
                        if preferred.Provides |> List.forall (fun p -> p.Contract <> preference.Contract) then
                            reader.Fail(sprintf "preferences: '%s' preferred definition does not provide '%s'." (PreferenceId.value preference.Preference) (ContractId.value preference.Contract))

                let declaredMissing = expectations.MissingArtifacts |> Set.ofList
                for package in packages do
                    if not (Set.contains package.Artifact artifactIds) && not (Set.contains package.Artifact declaredMissing) then
                        reader.Fail(sprintf "packages: '%s' references missing artifact '%s' that expectations do not declare." (PackageId.value package.Package) (ArtifactId.value package.Artifact))
                for evidenceEntry in evidence do
                    if not (Set.contains evidenceEntry.SubjectArtifact artifactIds) then
                        reader.Fail(sprintf "evidence: '%s' names unknown artifact '%s'." (EvidenceId.value evidenceEntry.Evidence) (ArtifactId.value evidenceEntry.SubjectArtifact))
                let advertisedPairs = advertisements |> List.map (fun a -> a.Source, a.Package) |> Set.ofList
                for entry in storefront do
                    if not (Set.contains (entry.Source, entry.Package) advertisedPairs) then
                        reader.Fail(sprintf "storefront: '%s' does not advertise '%s'." (SourceId.value entry.Source) (PackageId.value entry.Package))
                    for alternative in entry.Alternatives do
                        if not (Map.containsKey alternative packagesById) then
                            reader.Fail(sprintf "storefront: alternative '%s' is not a known package." (PackageId.value alternative))
                for artifact in artifacts do
                    let digest = hexDigest artifact.Content
                    if not (String.Equals(digest, artifact.Sha256, StringComparison.Ordinal)) then
                        reader.Fail(sprintf "artifacts: '%s' digest mismatch; recorded %s but content hashes to %s." (ArtifactId.value artifact.Artifact) artifact.Sha256 digest)

                let computedDuplicates =
                    advertisements
                    |> List.groupBy (fun a -> a.Package)
                    |> List.filter (fun (_, group) -> group |> List.map (fun a -> a.Source) |> List.distinct |> List.length > 1)
                    |> List.map (fun (key, _) -> PackageId.value key)
                reader.RequireExpectation("duplicateComponentIdentityAcrossSources", computedDuplicates, expectations.DuplicateComponentIdentityAcrossSources |> List.map PackageId.value)

                let computedMirrored =
                    publishers
                    |> List.filter (fun p -> sources |> List.filter (fun s -> List.contains p.Publisher s.ServesPublishers) |> List.length > 1)
                    |> List.map (fun p -> PublisherId.value p.Publisher)
                reader.RequireExpectation("mirroredPublishers", computedMirrored, expectations.MirroredPublishers |> List.map PublisherId.value)

                reader.RequireExpectation(
                    "multiPublisherSources",
                    sources |> List.filter (fun s -> List.length s.ServesPublishers > 1) |> List.map (fun s -> SourceId.value s.Source),
                    expectations.MultiPublisherSources |> List.map SourceId.value)

                let computedMultiDefinition =
                    definitions
                    |> List.collect (fun d -> d.Provides |> List.map (fun p -> p.Contract))
                    |> List.groupBy id
                    |> List.filter (fun (_, group) -> List.length group > 1)
                    |> List.map (fun (key, _) -> ContractId.value key)
                reader.RequireExpectation("contractsWithSeveralDefinitions", computedMultiDefinition, expectations.ContractsWithSeveralDefinitions |> List.map ContractId.value)

                let computedMultiOccurrence =
                    occurrences
                    |> List.groupBy (fun o -> o.Definition)
                    |> List.filter (fun (_, group) -> List.length group > 1)
                    |> List.map (fun (key, _) -> DefinitionId.value key)
                reader.RequireExpectation("definitionsWithSeveralOccurrences", computedMultiOccurrence, expectations.DefinitionsWithSeveralOccurrences |> List.map DefinitionId.value)

                reader.RequireExpectation("occupiedBindings", bindings |> List.map (fun b -> BindingId.value b.Binding), expectations.OccupiedBindings |> List.map BindingId.value)
                reader.RequireExpectation(
                    "systemDefaultScopes",
                    scopes |> List.filter (fun s -> s.Kind = SystemDefault) |> List.map (fun s -> BindingScopeId.value s.Scope),
                    expectations.SystemDefaultScopes |> List.map BindingScopeId.value)
                reader.RequireExpectation("explicitPreferences", preferences |> List.map (fun p -> PreferenceId.value p.Preference), expectations.ExplicitPreferences |> List.map PreferenceId.value)
                reader.RequireExpectation(
                    "genericCandidates",
                    definitions |> List.filter (fun d -> d.Generic) |> List.map (fun d -> DefinitionId.value d.Definition),
                    expectations.GenericCandidates |> List.map DefinitionId.value)

                let computedConflicting =
                    advertisements
                    |> List.filter (fun a ->
                        match Map.tryFind a.Package packagesById with
                        | Some package -> package.Version <> a.AdvertisedVersion
                        | None -> false)
                    |> List.map (fun a -> PackageId.value a.Package)
                reader.RequireExpectation("conflictingVersionClaims", computedConflicting, expectations.ConflictingVersionClaims |> List.map PackageId.value)

                let computedMissing =
                    packages
                    |> List.filter (fun p -> not (Set.contains p.Artifact artifactIds))
                    |> List.map (fun p -> ArtifactId.value p.Artifact)
                reader.RequireExpectation("missingArtifacts", computedMissing, expectations.MissingArtifacts |> List.map ArtifactId.value)

                let computedContradictory =
                    evidence
                    |> List.groupBy (fun e -> e.SubjectArtifact, e.Kind)
                    |> List.filter (fun (_, group) -> group |> List.map (fun e -> e.Verdict) |> List.distinct |> List.length > 1)
                    |> List.map (fun (_, group) -> group |> List.map (fun e -> EvidenceId.value e.Evidence) |> List.sortWith (fun a b -> String.CompareOrdinal(a, b)) |> String.concat "+")
                let declaredContradictory =
                    expectations.ContradictoryEvidence
                    |> List.map (fun pair -> pair |> List.map EvidenceId.value |> List.sortWith (fun a b -> String.CompareOrdinal(a, b)) |> String.concat "+")
                reader.RequireExpectation("contradictoryEvidence", computedContradictory, declaredContradictory)

                if reader.HasFailures then
                    raise (reader.Rejection())

                { Description = description
                  Contracts = contracts
                  Publishers = publishers
                  Sources = sources
                  Packages = packages
                  Advertisements = advertisements
                  ComponentDefinitions = definitions
                  BindingScopes = scopes
                  ActivatedOccurrences = occurrences
                  OccupiedBindings = bindings
                  Preferences = preferences
                  Artifacts = artifacts
                  Evidence = evidence
                  Storefront = storefront
                  Expectations = expectations }

    let loadMiceTopology (json: string) : MiceTopologyFixture =
        let reader = Reader()
        match parseRoot reader json "cm0-mice-topology"
                  [ "schemaVersion"; "fixture"; "description"; "contracts"; "observers"; "topologyNodes"
                    "functions"; "claims"; "expectations" ] with
        | None -> raise (reader.Rejection())
        | Some root ->
            let description =
                match reader.Attempt("description", fun () -> reader.GetString(root, "description")) with
                | Some value -> value
                | None -> ""
            let contracts = parseContracts reader root
            let observers =
                reader.ParseEntries(root, "observers", [ "observer" ], fun e r -> { Observer = ObserverId.create (r.GetString(e, "observer")) })
            let nodes =
                reader.ParseEntries(
                    root, "topologyNodes", [ "node"; "observer"; "kind" ],
                    fun e r ->
                        { Node = TopologyNodeId.create (r.GetString(e, "node"))
                          Observer = ObserverId.create (r.GetString(e, "observer"))
                          Kind = r.GetEnum(e, "kind", [ "host", Host; "attachment", Attachment ]) })
            let functions =
                reader.ParseEntries(
                    root, "functions", [ "function"; "contract"; "node"; "actor" ],
                    fun e r ->
                        { Function = FunctionId.create (r.GetString(e, "function"))
                          Contract = ContractId.create (r.GetString(e, "contract"))
                          Node = TopologyNodeId.create (r.GetString(e, "node"))
                          Actor = ActorId.create (r.GetString(e, "actor")) })
            let claims =
                reader.ParseEntries(
                    root, "claims", [ "claim"; "assertedBy"; "relation"; "from"; "to" ],
                    fun e r ->
                        { Claim = ClaimId.create (r.GetString(e, "claim"))
                          AssertedBy = ObserverId.create (r.GetString(e, "assertedBy"))
                          Relation =
                              r.GetEnum(
                                  e, "relation",
                                  [ "PartOf", PartOf; "AttachedThrough", AttachedThrough; "HostedBy", HostedBy
                                    "SamePhysicalAssembly", SamePhysicalAssembly; "SharesPowerDomain", SharesPowerDomain
                                    "SharesFailureDomain", SharesFailureDomain ])
                          From = TopologyNodeId.create (r.GetString(e, "from"))
                          To = TopologyNodeId.create (r.GetString(e, "to")) })

            let expectations =
                match tryGetSection reader root "expectations" with
                | None -> None
                | Some element ->
                    reader.CheckProperties(
                        element, "expectations",
                        [ "distinctMouseNodes"; "functionsPerMouseNode"; "attributableClaims"; "contradictoryClaims"; "maliciousClaims" ])
                    reader.Attempt(
                        "expectations",
                        fun () ->
                            { DistinctMouseNodes = reader.GetStringList(element, "distinctMouseNodes") |> List.map TopologyNodeId.create
                              FunctionsPerMouseNode = reader.GetInt(element, "functionsPerMouseNode")
                              AttributableClaims = reader.GetStringList(element, "attributableClaims") |> List.map ClaimId.create
                              ContradictoryClaims = reader.GetNestedStringLists(element, "contradictoryClaims") |> List.map (List.map ClaimId.create)
                              MaliciousClaims = reader.GetStringList(element, "maliciousClaims") |> List.map ClaimId.create })

            match expectations with
            | None -> raise (reader.Rejection())
            | Some expectations when reader.HasFailures -> raise (reader.Rejection())
            | Some expectations ->
                reader.RequireDistinct("contracts", contracts |> Seq.map (fun c -> ContractId.value c.Contract))
                reader.RequireDistinct("observers", observers |> Seq.map (fun o -> ObserverId.value o.Observer))
                reader.RequireDistinct("topologyNodes", nodes |> Seq.map (fun n -> TopologyNodeId.value n.Node))
                reader.RequireDistinct("functions", functions |> Seq.map (fun f -> FunctionId.value f.Function))
                reader.RequireDistinct("claims", claims |> Seq.map (fun c -> ClaimId.value c.Claim))
                reader.RequireDistinct("actors", functions |> Seq.map (fun f -> ActorId.value f.Actor))

                let observerIds = observers |> List.map (fun o -> o.Observer) |> Set.ofList
                let nodeIds = nodes |> List.map (fun n -> n.Node) |> Set.ofList
                let contractIds = contracts |> List.map (fun c -> c.Contract) |> Set.ofList

                for node in nodes do
                    if not (Set.contains node.Observer observerIds) then
                        reader.Fail(sprintf "topologyNodes: '%s' names unknown observer '%s'." (TopologyNodeId.value node.Node) (ObserverId.value node.Observer))
                for func in functions do
                    if not (Set.contains func.Contract contractIds) then
                        reader.Fail(sprintf "functions: '%s' names unknown contract '%s'." (FunctionId.value func.Function) (ContractId.value func.Contract))
                    if not (Set.contains func.Node nodeIds) then
                        reader.Fail(sprintf "functions: '%s' names unknown node '%s'." (FunctionId.value func.Function) (TopologyNodeId.value func.Node))
                for claim in claims do
                    if not (Set.contains claim.AssertedBy observerIds) then
                        reader.Fail(sprintf "claims: '%s' asserted by unknown observer '%s'." (ClaimId.value claim.Claim) (ObserverId.value claim.AssertedBy))
                    if not (Set.contains claim.From nodeIds) || not (Set.contains claim.To nodeIds) then
                        reader.Fail(sprintf "claims: '%s' relates unknown nodes." (ClaimId.value claim.Claim))
                    if claim.From = claim.To then
                        reader.Fail(sprintf "claims: '%s' relates a node to itself." (ClaimId.value claim.Claim))

                let functionBearing = functions |> List.groupBy (fun f -> f.Node)
                reader.RequireExpectation(
                    "distinctMouseNodes",
                    functionBearing |> List.map (fun (node, _) -> TopologyNodeId.value node),
                    expectations.DistinctMouseNodes |> List.map TopologyNodeId.value)
                for (node, group) in functionBearing |> List.sortWith (fun (a, _) (b, _) -> String.CompareOrdinal(TopologyNodeId.value a, TopologyNodeId.value b)) do
                    if List.length group <> expectations.FunctionsPerMouseNode then
                        reader.Fail(sprintf "expectations: node '%s' bears %d functions, expected %d." (TopologyNodeId.value node) (List.length group) expectations.FunctionsPerMouseNode)

                let claimIds = claims |> List.map (fun c -> c.Claim) |> Set.ofList
                let attributable = expectations.AttributableClaims |> Set.ofList
                let malicious = expectations.MaliciousClaims |> Set.ofList
                let pairMembers = expectations.ContradictoryClaims |> List.collect id |> Set.ofList

                for referenced in Set.unionMany [ attributable; malicious; pairMembers ] do
                    if not (Set.contains referenced claimIds) then
                        reader.Fail(sprintf "expectations: unknown claim '%s'." (ClaimId.value referenced))
                for pair in expectations.ContradictoryClaims do
                    if List.length pair <> 2 then
                        reader.Fail "expectations: contradictoryClaims entries must contain exactly two claims."
                for overlap in Set.intersect attributable malicious do
                    reader.Fail(sprintf "expectations: claim '%s' cannot be both attributable and malicious." (ClaimId.value overlap))
                for claim in claims do
                    if not (Set.contains claim.Claim attributable) && not (Set.contains claim.Claim malicious) && not (Set.contains claim.Claim pairMembers) then
                        reader.Fail(sprintf "expectations: claim '%s' is not classified as attributable, malicious, or contradictory." (ClaimId.value claim.Claim))

                if reader.HasFailures then
                    raise (reader.Rejection())

                { Description = description
                  Contracts = contracts
                  Observers = observers
                  TopologyNodes = nodes
                  Functions = functions
                  Claims = claims
                  Expectations = expectations }
