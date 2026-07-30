namespace Brontide.Minimal.Experimental.ComponentManagement

open System
open System.Security.Cryptography
open System.Text

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

type FakeComponentSource =
    private
        { Fixture: CatalogFixture
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

    let create (fixture: CatalogFixture) source : FakeComponentSource =
        { Fixture = fixture
          Source = sourceEntry fixture source
          Advertisements = advertisements fixture source
          Available = true }

    let createWithAdvertisementOrder (fixture: CatalogFixture) source order : FakeComponentSource =
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
                      |> List.filter (fun candidate -> candidate.SubjectArtifact = package.Artifact)
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
                        |> List.filter (fun item -> item.SubjectArtifact = artifact.Artifact)
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
