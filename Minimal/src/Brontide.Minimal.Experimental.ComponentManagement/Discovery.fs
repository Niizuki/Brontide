namespace Brontide.Minimal.Experimental.ComponentManagement

open System
open System.Security.Cryptography
open System.Text
open System.Text.Json

type DiscoveryQueryId = private DiscoveryQueryId of string
type TargetEnvironmentId = private TargetEnvironmentId of string
type EvidencePolicyId = private EvidencePolicyId of string
type RegionId = private RegionId of string
type PortId = private PortId of string

[<RequireQualifiedAccess>]
module DiscoveryQueryId =
    let create value = DiscoveryQueryId(IdentifierSyntax.require "DiscoveryQueryId" value)
    let value (DiscoveryQueryId value) = value

[<RequireQualifiedAccess>]
module TargetEnvironmentId =
    let create value = TargetEnvironmentId(IdentifierSyntax.require "TargetEnvironmentId" value)
    let value (TargetEnvironmentId value) = value

[<RequireQualifiedAccess>]
module EvidencePolicyId =
    let create value = EvidencePolicyId(IdentifierSyntax.require "EvidencePolicyId" value)
    let value (EvidencePolicyId value) = value

[<RequireQualifiedAccess>]
module RegionId =
    let create value = RegionId(IdentifierSyntax.require "RegionId" value)
    let value (RegionId value) = value

[<RequireQualifiedAccess>]
module PortId =
    let create value = PortId(IdentifierSyntax.require "PortId" value)
    let value (PortId value) = value

type LifecycleRole =
    | Ordinary
    | LocalInitialisation
    | Interconnection
    | RelationalInitialisation

type DefinitionConstraint = { Name: string; Value: string }

type SourceEvidenceAvailability =
    { Source: SourceId
      Evidence: EvidenceId }

type SourceEvidenceFixture =
    { Description: string
      Availability: SourceEvidenceAvailability list }

type DiscoveryQuery =
    { Query: DiscoveryQueryId
      Contract: ContractId
      Version: VersionLiteral
      TargetEnvironment: TargetEnvironmentId
      LifecycleRole: LifecycleRole
      Requester: DefinitionId option
      RequesterPublisher: PublisherId option
      DefinitionConstraints: DefinitionConstraint list
      PreferredProviders: DefinitionId list
      ExistingBinding: BindingId option
      ContainingRegion: RegionId option
      ContainingPort: PortId option
      TopologyRequirements: TopologyNodeId list }

type Cm1EffectObservation =
    { Selected: bool
      Resolved: bool
      Prepared: bool
      Activated: bool
      ActorEstablished: bool
      CapabilityGranted: bool }

[<RequireQualifiedAccess>]
module Cm1EffectObservation =
    let none =
        { Selected = false
          Resolved = false
          Prepared = false
          Activated = false
          ActorEstablished = false
          CapabilityGranted = false }

type DiscoveryCandidate =
    { Query: DiscoveryQueryId
      Source: SourceId
      Publisher: PublisherId
      Package: PackageId
      Definition: DefinitionId
      Contract: ContractId
      Version: VersionLiteral
      AdvertisedPackageVersion: VersionLiteral
      Artifact: ArtifactId
      AvailableEvidence: EvidenceId list
      Storefront: StorefrontEntry option }

type DiscoveryOutcome =
    { Query: DiscoveryQuery
      ConsultedSources: SourceId list
      Candidates: DiscoveryCandidate list
      Effects: Cm1EffectObservation }

type AttributedEvidence =
    { SuppliedBy: SourceId
      Evidence: EvidenceEntry }

type EvidencePolicyDecision =
    { Policy: EvidencePolicyId
      SuppliedBy: SourceId
      Evidence: EvidenceId
      Issuer: IssuerId
      Accepted: bool
      Reason: string }

type FakeEvidencePolicy = { Identity: EvidencePolicyId }

[<RequireQualifiedAccess>]
module FakeEvidencePolicy =
    let create identity = { Identity = identity }

    let evaluate source policy evidence =
        let accepted = evidence.Verdict = EvidenceVerdict.Accepted
        { Policy = policy.Identity
          SuppliedBy = source
          Evidence = evidence.Evidence
          Issuer = evidence.Issuer
          Accepted = accepted
          Reason =
            sprintf
                "policy %s %s the attributable %A claim"
                (EvidencePolicyId.value policy.Identity)
                (if accepted then "accepts" else "rejects")
                evidence.Kind }

type StagedArtifact =
    { Source: SourceId
      Package: PackageEntry
      Definitions: ComponentDefinitionEntry list
      Artifact: ArtifactEntry
      Evidence: AttributedEvidence list
      PolicyDecisions: EvidencePolicyDecision list
      Storefront: StorefrontEntry option
      Effects: Cm1EffectObservation }

type AcquisitionFailureKind =
    | SourceUnavailable
    | PackageNotAdvertised
    | ArtifactUnavailable
    | ArtifactIntegrityFailed

type AcquisitionFailure =
    { Kind: AcquisitionFailureKind
      Source: SourceId
      Package: PackageId
      Reason: string }

type AcquisitionResult =
    | Staged of StagedArtifact
    | Refused of AcquisitionFailure

type AcquisitionResult with
    member _.Effects = Cm1EffectObservation.none

/// Strict CM1-only loader. The separate file keeps source provenance explicit without changing the
/// retained CM0 catalog schema or pretending that every repository serving an artifact supplied
/// every claim about it.
[<RequireQualifiedAccess>]
module Cm1FixtureLoader =
    let loadSourceEvidence (json: string) (catalog: CatalogFixture) : SourceEvidenceFixture =
        let failures = ResizeArray<string>()

        let checkObject context (allowed: Set<string>) (element: JsonElement) =
            if element.ValueKind <> JsonValueKind.Object then
                failures.Add(sprintf "%s: expected an object." context)
            else
                let names = element.EnumerateObject() |> Seq.map (fun property -> property.Name) |> Set.ofSeq
                for unknown in Set.difference names allowed do
                    failures.Add(sprintf "%s: unknown property '%s'." context unknown)
                for missing in Set.difference allowed names do
                    failures.Add(sprintf "%s: missing property '%s'." context missing)

        let readString (context: string) (property: string) (element: JsonElement) : string option =
            match element.TryGetProperty property with
            | true, value when value.ValueKind = JsonValueKind.String ->
                match value.GetString() with
                | null ->
                    failures.Add(sprintf "%s: '%s' must be a non-null string." context property)
                    None
                | text -> Some text
            | _ ->
                failures.Add(sprintf "%s: '%s' must be a string." context property)
                None

        let parsed =
            try
                Some(JsonDocument.Parse json)
            with :? JsonException as exceptionValue ->
                failures.Add(sprintf "source-evidence: invalid JSON: %s" exceptionValue.Message)
                None

        match parsed with
        | None -> raise (FixtureFormatException(List.ofSeq failures))
        | Some document ->
            use document = document
            let root = document.RootElement
            checkObject
                "source-evidence"
                (Set.ofList [ "schemaVersion"; "fixture"; "description"; "availability" ])
                root

            if root.ValueKind <> JsonValueKind.Object then
                raise (FixtureFormatException(List.ofSeq failures))

            match root.TryGetProperty "schemaVersion" with
            | true, value when value.ValueKind = JsonValueKind.Number ->
                match value.TryGetInt32() with
                | true, 1 -> ()
                | _ -> failures.Add "source-evidence: schemaVersion must be 1."
            | _ -> failures.Add "source-evidence: schemaVersion must be 1."

            match readString "source-evidence" "fixture" root with
            | Some "cm1-source-evidence" -> ()
            | Some _ -> failures.Add "source-evidence: fixture must be 'cm1-source-evidence'."
            | None -> ()

            let description = readString "source-evidence" "description" root |> Option.defaultValue ""
            let availability = ResizeArray<SourceEvidenceAvailability>()
            match root.TryGetProperty "availability" with
            | true, entries when entries.ValueKind = JsonValueKind.Array ->
                entries.EnumerateArray()
                |> Seq.iteri (fun index entry ->
                    let context = sprintf "source-evidence.availability[%d]" index
                    checkObject context (Set.ofList [ "source"; "evidence" ]) entry
                    match readString context "source" entry, readString context "evidence" entry with
                    | Some source, Some evidence ->
                        try
                            availability.Add
                                { Source = SourceId.create source
                                  Evidence = EvidenceId.create evidence }
                        with :? ArgumentException as exceptionValue ->
                            failures.Add(sprintf "%s: %s" context exceptionValue.Message)
                    | _ -> ())
            | _ -> failures.Add "source-evidence: availability must be an array."

            availability
            |> Seq.groupBy (fun entry -> entry.Source, entry.Evidence)
            |> Seq.filter (fun (_, entries) -> Seq.length entries > 1)
            |> Seq.map fst
            |> Seq.sortBy (fun (source, evidence) -> SourceId.value source, EvidenceId.value evidence)
            |> Seq.iter (fun (source, evidence) ->
                failures.Add(
                    sprintf
                        "source-evidence: duplicate availability '%s|%s'."
                        (SourceId.value source)
                        (EvidenceId.value evidence)))

            let sourceIds = catalog.Sources |> List.map (fun source -> source.Source) |> Set.ofList
            let evidenceById = catalog.Evidence |> List.map (fun evidence -> evidence.Evidence, evidence) |> Map.ofList
            let packagesById = catalog.Packages |> List.map (fun package -> package.Package, package) |> Map.ofList
            for entry in availability do
                if not (Set.contains entry.Source sourceIds) then
                    failures.Add(sprintf "source-evidence: unknown source '%s'." (SourceId.value entry.Source))
                match Map.tryFind entry.Evidence evidenceById with
                | None ->
                    failures.Add(sprintf "source-evidence: unknown evidence '%s'." (EvidenceId.value entry.Evidence))
                | Some evidence ->
                    let sourceCarriesSubject =
                        catalog.Advertisements
                        |> List.exists (fun advertisement ->
                            advertisement.Source = entry.Source
                            && (match Map.tryFind advertisement.Package packagesById with
                                | Some package -> package.Artifact = evidence.SubjectArtifact
                                | None -> false))
                    if not sourceCarriesSubject then
                        failures.Add(
                            sprintf
                                "source-evidence: source '%s' does not advertise a package carrying '%s' for '%s'."
                                (SourceId.value entry.Source)
                                (ArtifactId.value evidence.SubjectArtifact)
                                (EvidenceId.value entry.Evidence))

            if failures.Count > 0 then
                raise (FixtureFormatException(List.ofSeq failures))

            { Description = description
              Availability = List.ofSeq availability }

type FakeComponentSource =
    private
        { Fixture: CatalogFixture
          EvidenceAvailability: SourceEvidenceAvailability list
          Source: SourceEntry
          Advertisements: AdvertisementEntry list
          Available: bool }

[<RequireQualifiedAccess>]
module FakeComponentSource =
    let private sourceEntry (fixture: CatalogFixture) source =
        fixture.Sources
        |> List.tryFind (fun candidate -> candidate.Source = source)
        |> Option.defaultWith (fun () -> invalidArg "source" (sprintf "Fixture has no source '%s'." (SourceId.value source)))

    let private advertisements (fixture: CatalogFixture) source =
        fixture.Advertisements |> List.filter (fun candidate -> candidate.Source = source)

    let create (fixture: CatalogFixture) (sourceEvidence: SourceEvidenceFixture) source : FakeComponentSource =
        { Fixture = fixture
          EvidenceAvailability = sourceEvidence.Availability
          Source = sourceEntry fixture source
          Advertisements = advertisements fixture source
          Available = true }

    let createWithAdvertisementOrder (fixture: CatalogFixture) (sourceEvidence: SourceEvidenceFixture) source order : FakeComponentSource =
        let declared = advertisements fixture source
        let byPackage = declared |> List.map (fun item -> item.Package, item) |> Map.ofList
        let distinctOrder = order |> List.distinct
        if List.length order <> List.length declared
           || List.length distinctOrder <> List.length declared
           || order |> List.exists (fun package -> not (Map.containsKey package byPackage)) then
            invalidArg
                "order"
                (sprintf "Advertisement enumeration for '%s' must name every advertised package exactly once." (SourceId.value source))
        { Fixture = fixture
          EvidenceAvailability = sourceEvidence.Availability
          Source = sourceEntry fixture source
          Advertisements = order |> List.map (fun package -> Map.find package byPackage)
          Available = true }

    let identity (source: FakeComponentSource) = source.Source.Source
    let kind (source: FakeComponentSource) = source.Source.Kind
    let isAvailable (source: FakeComponentSource) = source.Available
    let remove (source: FakeComponentSource) = { source with Available = false }

    let discover (query: DiscoveryQuery) (source: FakeComponentSource) : DiscoveryCandidate list =
        if not source.Available then
            []
        else
            [ for advertisement in source.Advertisements do
                  let package =
                      source.Fixture.Packages
                      |> List.find (fun candidate -> candidate.Package = advertisement.Package)
                  let evidence =
                      source.Fixture.Evidence
                      |> List.filter (fun candidate ->
                          candidate.SubjectArtifact = package.Artifact
                          && (source.EvidenceAvailability
                              |> List.exists (fun availability ->
                                  availability.Source = source.Source.Source
                                  && availability.Evidence = candidate.Evidence)))
                      |> List.map (fun candidate -> candidate.Evidence)
                      |> List.sortBy EvidenceId.value
                  let storefront =
                      source.Fixture.Storefront
                      |> List.tryFind (fun candidate ->
                          candidate.Source = source.Source.Source && candidate.Package = package.Package)
                  for definition in
                      source.Fixture.ComponentDefinitions
                      |> List.filter (fun candidate -> candidate.Package = package.Package) do
                      for provision in
                          definition.Provides
                          |> List.filter (fun candidate ->
                              candidate.Contract = query.Contract && candidate.Version = query.Version) do
                          { Query = query.Query
                            Source = source.Source.Source
                            Publisher = package.Publisher
                            Package = package.Package
                            Definition = definition.Definition
                            Contract = provision.Contract
                            Version = provision.Version
                            AdvertisedPackageVersion = advertisement.AdvertisedVersion
                            Artifact = package.Artifact
                            AvailableEvidence = evidence
                            Storefront = storefront } ]

    let private refusal kind (source: FakeComponentSource) package reason =
        Refused
            { Kind = kind
              Source = source.Source.Source
              Package = package
              Reason = reason }

    let acquire policy packageIdentity (source: FakeComponentSource) =
        if not source.Available then
            refusal SourceUnavailable source packageIdentity "source is unavailable"
        elif source.Advertisements |> List.exists (fun item -> item.Package = packageIdentity) |> not then
            refusal PackageNotAdvertised source packageIdentity "package is not advertised by this source"
        else
            let package = source.Fixture.Packages |> List.find (fun item -> item.Package = packageIdentity)
            match source.Fixture.Artifacts |> List.tryFind (fun item -> item.Artifact = package.Artifact) with
            | None ->
                refusal
                    ArtifactUnavailable
                    source
                    packageIdentity
                    (sprintf "artifact '%s' is unavailable" (ArtifactId.value package.Artifact))
            | Some artifact ->
                let actualDigest =
                    artifact.Content
                    |> Encoding.UTF8.GetBytes
                    |> SHA256.HashData
                    |> Convert.ToHexString
                if not (String.Equals(actualDigest, artifact.Sha256, StringComparison.Ordinal)) then
                    refusal
                        ArtifactIntegrityFailed
                        source
                        packageIdentity
                        (sprintf "artifact '%s' digest does not match its immutable content" (ArtifactId.value artifact.Artifact))
                else
                    let definitions =
                        source.Fixture.ComponentDefinitions
                        |> List.filter (fun item -> item.Package = packageIdentity)
                        |> List.sortBy (fun item -> DefinitionId.value item.Definition)
                    let evidence =
                        source.Fixture.Evidence
                        |> List.filter (fun item ->
                            item.SubjectArtifact = artifact.Artifact
                            && (source.EvidenceAvailability
                                |> List.exists (fun availability ->
                                    availability.Source = source.Source.Source
                                    && availability.Evidence = item.Evidence)))
                        |> List.sortBy (fun item -> EvidenceId.value item.Evidence)
                        |> List.map (fun item -> { SuppliedBy = source.Source.Source; Evidence = item })
                    let decisions =
                        evidence
                        |> List.map (fun item -> FakeEvidencePolicy.evaluate source.Source.Source policy item.Evidence)
                    let storefront =
                        source.Fixture.Storefront
                        |> List.tryFind (fun item ->
                            item.Source = source.Source.Source && item.Package = packageIdentity)
                    Staged
                        { Source = source.Source.Source
                          Package = package
                          Definitions = definitions
                          Artifact = artifact
                          Evidence = evidence
                          PolicyDecisions = decisions
                          Storefront = storefront
                          Effects = Cm1EffectObservation.none }

[<RequireQualifiedAccess>]
module FakeDiscovery =
    let run (query: DiscoveryQuery) (sources: FakeComponentSource list) : DiscoveryOutcome =
        let available = sources |> List.filter FakeComponentSource.isAvailable
        { Query = query
          ConsultedSources = available |> List.map FakeComponentSource.identity |> List.sortBy SourceId.value
          Candidates =
            available
            |> List.collect (FakeComponentSource.discover query)
            |> List.sortBy (fun candidate ->
                SourceId.value candidate.Source,
                PackageId.value candidate.Package,
                DefinitionId.value candidate.Definition)
          Effects = Cm1EffectObservation.none }
