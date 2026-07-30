namespace Brontide.Minimal.ComponentManagement.Tests

open System.IO
open NUnit.Framework
open Brontide.Minimal.Experimental.ComponentManagement

[<TestFixture>]
type DiscoveryTests() =

    let fixturePath =
        Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "component-management",
            "fixtures",
            "cm0-catalog.json")

    let fixture () = fixturePath |> File.ReadAllText |> FixtureLoader.loadCatalog

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

    [<Test>]
    member _.``Discovery accepts zero one and several sources without lifecycle effects``() =
        let data = fixture ()
        let local = FakeComponentSource.create data (SourceId.create "src.local-cache")
        let mirror = FakeComponentSource.create data (SourceId.create "src.contoso-mirror")
        let bazaar = FakeComponentSource.create data (SourceId.create "src.bazaar")

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
        assertNoEffects zero.Effects
        assertNoEffects one.Effects
        assertNoEffects several.Effects

    [<Test>]
    member _.``Discovery is deterministic under source and advertisement permutation``() =
        let data = fixture ()
        let bazaarId = SourceId.create "src.bazaar"
        let normalBazaar = FakeComponentSource.create data bazaarId
        let reversedOrder =
            data.Advertisements
            |> List.filter (fun advertisement -> advertisement.Source = bazaarId)
            |> List.map (fun advertisement -> advertisement.Package)
            |> List.rev
        let reversedBazaar = FakeComponentSource.createWithAdvertisementOrder data bazaarId reversedOrder
        let local = FakeComponentSource.create data (SourceId.create "src.local-cache")

        let first = FakeDiscovery.run (query "1.1") [ normalBazaar; local ]
        let second = FakeDiscovery.run (query "1.1") [ local; reversedBazaar ]

        equal second.ConsultedSources first.ConsultedSources
        equal second.Candidates first.Candidates
        Assert.That(
            first.Candidates
            |> List.forall (fun candidate ->
                candidate.Contract = (query "1.1").Contract
                && candidate.Version = (query "1.1").Version),
            Is.True)

    [<Test>]
    member _.``One source preserves unrelated publisher identities and one storefront shape``() =
        let data = fixture ()
        let bazaar = FakeComponentSource.create data (SourceId.create "src.bazaar")
        let contoso = (FakeDiscovery.run (query "1.1") [ bazaar ]).Candidates |> List.exactlyOne
        let fabrikam = (FakeDiscovery.run (query "1.0") [ bazaar ]).Candidates |> List.exactlyOne

        equal contoso.Source (SourceId.create "src.bazaar")
        equal contoso.Publisher (PublisherId.create "pub.contoso")
        equal fabrikam.Source (SourceId.create "src.bazaar")
        equal fabrikam.Publisher (PublisherId.create "pub.fabrikam")
        equal contoso.Storefront None
        Assert.That(fabrikam.Storefront.IsSome, Is.True)

    [<Test>]
    member _.``Staged acquisition survives source disappearance and grants nothing``() =
        let data = fixture ()
        let source = FakeComponentSource.create data (SourceId.create "src.local-cache")
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
        equal (refusalKind refused) AcquisitionFailureKind.SourceUnavailable
        assertNoEffects staged.Effects

    [<Test>]
    member _.``Acquisition preserves contested evidence and policy attribution``() =
        let source = FakeComponentSource.create (fixture ()) (SourceId.create "src.bazaar")
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
        let local = FakeComponentSource.create data (SourceId.create "src.local-cache")
        let bazaar = FakeComponentSource.create data (SourceId.create "src.bazaar")
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
        let corrupt = FakeComponentSource.create corrupted (SourceId.create "src.local-cache")

        let unadvertised =
            FakeComponentSource.acquire (policy ()) (PackageId.create "pkg.fabrikam.cooling") local
        let missing =
            FakeComponentSource.acquire (policy ()) (PackageId.create "pkg.northwind.database") bazaar
        let integrity =
            FakeComponentSource.acquire (policy ()) (PackageId.create "pkg.contoso.cooling") corrupt

        equal (refusalKind unadvertised) AcquisitionFailureKind.PackageNotAdvertised
        equal (refusalKind missing) AcquisitionFailureKind.ArtifactUnavailable
        equal (refusalKind integrity) AcquisitionFailureKind.ArtifactIntegrityFailed
