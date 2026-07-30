namespace Brontide.Minimal.ComponentManagement.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text
open NUnit.Framework
open Brontide.Minimal.Experimental.ComponentManagement

[<TestFixture>]
type DiscoveryTests() =

    let fixturePath name =
        Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "component-management",
            "fixtures",
            name)

    let fixture () = fixturePath "cm0-catalog.json" |> File.ReadAllText |> FixtureLoader.loadCatalog

    let sourceEvidence catalog =
        Cm1FixtureLoader.loadSourceEvidence
            (fixturePath "cm1-source-evidence.json" |> File.ReadAllText)
            catalog

    let sourceEvidenceRejection json catalog =
        try
            Cm1FixtureLoader.loadSourceEvidence json catalog |> ignore
            Assert.Fail "expected FixtureFormatException but loading succeeded"
            []
        with FixtureFormatException failures -> failures

    let query version =
        { Query = DiscoveryQueryId.create "query.cooling"
          Contract = ContractId.create "brontide.fake.cooling-control"
          Version = VersionLiteral.create version
          TargetEnvironment = TargetEnvironmentId.create "environment.fake-platform-1"
          LifecycleRole = LifecycleRole.Ordinary
          Requester = Some(DefinitionId.create "def.requester")
          RequesterPublisher = Some(PublisherId.create "pub.contoso")
          DefinitionConstraints = []
          PreferredProviders = []
          ExistingBinding = None
          ContainingRegion = None
          ContainingPort = None
          TopologyRequirements = [] }

    let policy () =
        EvidencePolicyId.create "policy.fake-local" |> FakeEvidencePolicy.create

    let equal actual expected =
        Assert.That((actual = expected), Is.True, sprintf "expected %A but was %A" expected actual)

    let assertNoEffects effects =
        equal
            [ effects.Selected
              effects.Resolved
              effects.Prepared
              effects.Activated
              effects.ActorEstablished
              effects.CapabilityGranted ]
            [ false; false; false; false; false; false ]

    let refusalKind result =
        match result with
        | Refused failure -> failure.Kind
        | Staged _ ->
            Assert.Fail "expected acquisition refusal but received a staged artifact"
            AcquisitionFailureKind.SourceUnavailable

    let rec permutations values =
        match values with
        | [] -> [ [] ]
        | _ ->
            [ for index in 0 .. List.length values - 1 do
                  let head = List.item index values
                  let tail = values |> List.indexed |> List.choose (fun (candidateIndex, value) -> if candidateIndex = index then None else Some value)
                  for suffix in permutations tail do
                      head :: suffix ]

    [<Test>]
    member _.``Discovery accepts zero one and several sources without lifecycle effects``() =
        let data = fixture ()
        let evidence = sourceEvidence data
        let local = FakeComponentSource.create data evidence (SourceId.create "src.local-cache")
        let mirror = FakeComponentSource.create data evidence (SourceId.create "src.contoso-mirror")
        let bazaar = FakeComponentSource.create data evidence (SourceId.create "src.bazaar")

        let zero = FakeDiscovery.run (query "1.1") []
        let one = FakeDiscovery.run (query "1.1") [ local ]
        let several = FakeDiscovery.run (query "1.1") [ bazaar; mirror; local ]

        equal zero.Candidates []
        equal (List.length one.Candidates) 1
        equal (List.length several.Candidates) 3
        equal
            (several.Candidates |> List.map (fun candidate -> candidate.Source))
            [ SourceId.create "src.bazaar"
              SourceId.create "src.contoso-mirror"
              SourceId.create "src.local-cache" ]
        equal
            (several.Candidates |> List.map (fun candidate -> VersionLiteral.value candidate.AdvertisedPackageVersion))
            [ "1.5.0-claimed"; "1.4.0"; "1.4.0" ]
        equal
            (several.Candidates |> List.map (fun candidate -> List.length candidate.AvailableEvidence))
            [ 0; 2; 2 ]
        assertNoEffects zero.Effects
        assertNoEffects one.Effects
        assertNoEffects several.Effects

    [<Test>]
    member _.``Source evidence fixture fails closed on unknown duplicate or invented attribution``() =
        let catalog = fixture ()
        let json = fixturePath "cm1-source-evidence.json" |> File.ReadAllText
        let unknown =
            json.Replace(
                "\"source\": \"src.bazaar\", \"evidence\": \"ev.review-fab-positive\"",
                "\"source\": \"src.unknown\", \"evidence\": \"ev.review-fab-positive\"")
        let invented =
            json.Replace(
                "\"source\": \"src.bazaar\", \"evidence\": \"ev.review-fab-positive\"",
                "\"source\": \"src.local-cache\", \"evidence\": \"ev.review-fab-positive\"")
        let unknownEvidence =
            json.Replace(
                "\"evidence\": \"ev.review-fab-positive\"",
                "\"evidence\": \"ev.unknown\"")
        let duplicate =
            json.Replace(
                "{ \"source\": \"src.local-cache\", \"evidence\": \"ev.integrity-cooling\" },",
                "{ \"source\": \"src.local-cache\", \"evidence\": \"ev.integrity-cooling\" },\n    { \"source\": \"src.local-cache\", \"evidence\": \"ev.integrity-cooling\" },")
        let unknownFailures = sourceEvidenceRejection unknown catalog
        let inventedFailures = sourceEvidenceRejection invented catalog
        let unknownEvidenceFailures = sourceEvidenceRejection unknownEvidence catalog
        let duplicateFailures = sourceEvidenceRejection duplicate catalog

        Assert.That(unknownFailures |> List.exists (fun failure -> failure.Contains "unknown source 'src.unknown'"), Is.True)
        Assert.That(inventedFailures |> List.exists (fun failure -> failure.Contains "does not advertise a package carrying"), Is.True)
        Assert.That(unknownEvidenceFailures |> List.exists (fun failure -> failure.Contains "unknown evidence 'ev.unknown'"), Is.True)
        Assert.That(duplicateFailures |> List.exists (fun failure -> failure.Contains "duplicate availability"), Is.True)

    [<Test>]
    member _.``Discovery is deterministic under source and advertisement permutation``() =
        let data = fixture ()
        let evidence = sourceEvidence data
        let sourceIds = data.Sources |> List.map (fun source -> source.Source)
        let baseline =
            sourceIds
            |> List.map (FakeComponentSource.create data evidence)
            |> FakeDiscovery.run (query "1.1")

        for sourceOrder in permutations sourceIds do
            let outcome =
                sourceOrder
                |> List.map (FakeComponentSource.create data evidence)
                |> FakeDiscovery.run (query "1.1")
            equal outcome baseline

        for source in sourceIds do
            let advertisedPackages =
                data.Advertisements
                |> List.filter (fun advertisement -> advertisement.Source = source)
                |> List.map (fun advertisement -> advertisement.Package)
            for advertisementOrder in permutations advertisedPackages do
                let sources =
                    sourceIds
                    |> List.map (fun candidate ->
                        if candidate = source then
                            FakeComponentSource.createWithAdvertisementOrder data evidence candidate advertisementOrder
                        else
                            FakeComponentSource.create data evidence candidate)
                equal (FakeDiscovery.run (query "1.1") sources) baseline

        Assert.That(
            baseline.Candidates
            |> List.forall (fun candidate ->
                candidate.Contract = (query "1.1").Contract
                && candidate.Version = (query "1.1").Version
                && (data.Advertisements
                    |> List.exists (fun advertisement ->
                        advertisement.Source = candidate.Source
                        && advertisement.Package = candidate.Package))),
            Is.True)

    [<Test>]
    member _.``Discovery carries context without giving it CM2 filtering semantics``() =
        let data = fixture ()
        let evidence = sourceEvidence data
        let contextual =
            { query "1.1" with
                DefinitionConstraints = [ { Name = "constraint.fake"; Value = "value" } ]
                PreferredProviders = [ DefinitionId.create "def.fabrikam.cooling" ]
                ExistingBinding = Some(BindingId.create "bind.system-telemetry")
                ContainingRegion = Some(RegionId.create "region.fake")
                ContainingPort = Some(PortId.create "port.fake")
                TopologyRequirements = [ TopologyNodeId.create "node.attachment-1" ] }
        let outcome =
            [ FakeComponentSource.create data evidence (SourceId.create "src.local-cache") ]
            |> FakeDiscovery.run contextual

        equal outcome.Query contextual
        equal (List.length outcome.Candidates) 1

    [<Test>]
    member _.``One source preserves unrelated publisher identities and one storefront shape``() =
        let data = fixture ()
        let evidence = sourceEvidence data
        let bazaar = FakeComponentSource.create data evidence (SourceId.create "src.bazaar")
        let contoso = (FakeDiscovery.run (query "1.1") [ bazaar ]).Candidates |> List.exactlyOne
        let fabrikam = (FakeDiscovery.run (query "1.0") [ bazaar ]).Candidates |> List.exactlyOne

        equal contoso.Source (SourceId.create "src.bazaar")
        equal contoso.Publisher (PublisherId.create "pub.contoso")
        equal fabrikam.Source (SourceId.create "src.bazaar")
        equal fabrikam.Publisher (PublisherId.create "pub.fabrikam")
        equal contoso.Storefront None
        Assert.That(fabrikam.Storefront.IsSome, Is.True)

    [<Test>]
    member _.``Local and remote sources project the same storefront fields``() =
        let data = fixture ()
        let evidence = sourceEvidence data
        let local =
            [ FakeComponentSource.create data evidence (SourceId.create "src.local-cache") ]
            |> FakeDiscovery.run (query "1.1")
            |> fun outcome -> outcome.Candidates |> List.exactlyOne
        let remote =
            [ FakeComponentSource.create data evidence (SourceId.create "src.contoso-mirror") ]
            |> FakeDiscovery.run (query "1.1")
            |> fun outcome -> outcome.Candidates |> List.exactlyOne

        match local.Storefront, remote.Storefront with
        | Some localProjection, Some remoteProjection ->
            equal { localProjection with Source = remote.Source } remoteProjection
        | _ -> Assert.Fail "both local and remote sources must carry the shared projection"

    [<Test>]
    member _.``Staged acquisition survives source disappearance and grants nothing``() =
        let data = fixture ()
        let evidence = sourceEvidence data
        let source = FakeComponentSource.create data evidence (SourceId.create "src.local-cache")
        let acquired = FakeComponentSource.acquire (policy ()) (PackageId.create "pkg.contoso.cooling") source

        let staged =
            match acquired with
            | Staged value -> value
            | Refused failure ->
                Assert.Fail(sprintf "acquisition was refused: %A" failure)
                Unchecked.defaultof<StagedArtifact>

        let removed = FakeComponentSource.remove source
        let refused =
            FakeComponentSource.acquire (policy ()) (PackageId.create "pkg.contoso.cooling") removed

        equal staged.Artifact.Content "fake-artifact:contoso-cooling:1.4.0"
        equal
            (staged.Artifact.Content |> Encoding.UTF8.GetBytes |> SHA256.HashData |> Convert.ToHexString)
            staged.Artifact.Sha256
        equal (refusalKind refused) AcquisitionFailureKind.SourceUnavailable
        assertNoEffects staged.Effects
        assertNoEffects acquired.Effects
        assertNoEffects refused.Effects
        equal (FakeDiscovery.run (query "1.1") [ removed ]).ConsultedSources []

    [<Test>]
    member _.``Acquisition preserves contested evidence and policy attribution``() =
        let data = fixture ()
        let evidence = sourceEvidence data
        let source = FakeComponentSource.create data evidence (SourceId.create "src.bazaar")
        let result =
            FakeComponentSource.acquire (policy ()) (PackageId.create "pkg.fabrikam.cooling") source

        match result with
        | Refused failure -> Assert.Fail(sprintf "acquisition was refused: %A" failure)
        | Staged staged ->
            equal (List.length staged.Evidence) 2
            equal (List.length staged.PolicyDecisions) 2
            equal
                (staged.Evidence |> List.map (fun item -> item.SuppliedBy) |> List.distinct)
                [ FakeComponentSource.identity source ]
            Assert.That(
                staged.Evidence
                |> List.forall (fun item ->
                    evidence.Availability
                    |> List.exists (fun availability ->
                        availability.Source = item.SuppliedBy
                        && availability.Evidence = item.Evidence.Evidence)),
                Is.True)
            equal
                (staged.Evidence |> List.map (fun item -> item.Evidence.Verdict) |> Set.ofList)
                (Set.ofList [ EvidenceVerdict.Accepted; EvidenceVerdict.Rejected ])
            equal
                (staged.PolicyDecisions |> List.map (fun item -> item.Accepted) |> Set.ofList)
                (Set.ofList [ true; false ])
            equal
                (staged.PolicyDecisions |> List.map (fun item -> item.Evidence))
                (staged.Evidence |> List.map (fun item -> item.Evidence.Evidence))

    [<Test>]
    member _.``Acquisition refusals never contain a partial stage``() =
        let data = fixture ()
        let evidence = sourceEvidence data
        let local = FakeComponentSource.create data evidence (SourceId.create "src.local-cache")
        let bazaar = FakeComponentSource.create data evidence (SourceId.create "src.bazaar")
        let artifactId = ArtifactId.create "art.cooling-1-4-0"
        let corrupted =
            { data with
                Artifacts =
                    data.Artifacts
                    |> List.map (fun artifact ->
                        if artifact.Artifact = artifactId then
                            { artifact with Content = "corrupted-after-validation" }
                        else
                            artifact) }
        let corrupt = FakeComponentSource.create corrupted evidence (SourceId.create "src.local-cache")

        let unadvertised =
            FakeComponentSource.acquire (policy ()) (PackageId.create "pkg.fabrikam.cooling") local
        let missing =
            FakeComponentSource.acquire (policy ()) (PackageId.create "pkg.northwind.database") bazaar
        let integrity =
            FakeComponentSource.acquire (policy ()) (PackageId.create "pkg.contoso.cooling") corrupt

        equal (refusalKind unadvertised) AcquisitionFailureKind.PackageNotAdvertised
        equal (refusalKind missing) AcquisitionFailureKind.ArtifactUnavailable
        equal (refusalKind integrity) AcquisitionFailureKind.ArtifactIntegrityFailed
