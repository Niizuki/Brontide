namespace Brontide.Minimal.ComponentManagement.Tests

open System
open System.IO
open NUnit.Framework
open Brontide.Minimal.Experimental.ComponentManagement

[<TestFixture>]
type FixtureTests() =

    let fixturePath name =
        Path.Combine(TestContext.CurrentContext.TestDirectory, "component-management", "fixtures", name)

    let catalogJson () = File.ReadAllText(fixturePath "cm0-catalog.json")
    let miceJson () = File.ReadAllText(fixturePath "cm0-mice-topology.json")

    /// Returns true when the thunk raises ArgumentException; keeps NUnit delegate overloads out of F#.
    let raisesArgument (action: unit -> unit) : bool =
        try
            action ()
            false
        with :? ArgumentException -> true

    /// Runs a loader expected to fail closed and returns its accumulated failure list.
    let rejection (load: unit -> unit) : string list =
        try
            load ()
            Assert.Fail "expected FixtureFormatException but loading succeeded"
            []
        with FixtureFormatException failures -> failures

    /// Structural equality assertion; sidesteps NUnit's ambiguous list/nullable EqualTo overloads.
    let equal (actual: 'T) (expected: 'T) =
        Assert.That((actual = expected), Is.True, sprintf "expected %A but was %A" expected actual)

    [<TestCase("")>]
    [<TestCase("Has Spaces")>]
    [<TestCase("UPPER")>]
    [<TestCase("under_score")>]
    member _.``Identifier creation rejects invalid syntax``(value: string) =
        Assert.That(raisesArgument (fun () -> SourceId.create value |> ignore), Is.True)

    [<Test>]
    member _.``Identifier spaces are distinct types over one primitive``() =
        let source = SourceId.create "shared.token"
        let publisher = PublisherId.create "shared.token"
        equal (SourceId.value source) (PublisherId.value publisher)
        Assert.That((source.GetType() <> publisher.GetType()), Is.True)

    [<TestCase("1..1", 1)>]
    [<TestCase("0..*", 0)>]
    member _.``Cardinality parses declared forms``(text: string, minimum: int) =
        equal (Cardinality.parse text).Minimum minimum

    [<TestCase("1")>]
    [<TestCase("2..1")>]
    [<TestCase("-1..2")>]
    member _.``Cardinality rejects invalid forms``(text: string) =
        Assert.That(raisesArgument (fun () -> Cardinality.parse text |> ignore), Is.True)

    [<Test>]
    member _.``Catalog fixture loads with expected shape``() =
        let fixture = FixtureLoader.loadCatalog (catalogJson ())
        equal (List.length fixture.Sources) 3
        equal (List.length fixture.Packages) 5
        equal (List.length fixture.ComponentDefinitions) 5
        equal (List.length fixture.ActivatedOccurrences) 3
        equal (List.length fixture.Storefront) 3

    [<Test>]
    member _.``Catalog expectations surface every required cm0 case``() =
        let expectations = (FixtureLoader.loadCatalog (catalogJson ())).Expectations
        equal expectations.DuplicateComponentIdentityAcrossSources [ PackageId.create "pkg.contoso.cooling" ]
        equal expectations.MirroredPublishers [ PublisherId.create "pub.contoso" ]
        equal expectations.MultiPublisherSources [ SourceId.create "src.bazaar" ]
        equal (List.length expectations.ContractsWithSeveralDefinitions) 2
        equal expectations.DefinitionsWithSeveralOccurrences [ DefinitionId.create "def.northwind.telemetry" ]
        equal (List.length expectations.OccupiedBindings) 2
        equal expectations.SystemDefaultScopes [ BindingScopeId.create "scope.system" ]
        equal (List.length expectations.ExplicitPreferences) 1
        equal expectations.GenericCandidates [ DefinitionId.create "def.contoso.generic-telemetry" ]
        equal expectations.ConflictingVersionClaims [ PackageId.create "pkg.contoso.cooling" ]
        equal expectations.MissingArtifacts [ ArtifactId.create "art.missing-db" ]
        equal (List.length expectations.ContradictoryEvidence) 1

    [<Test>]
    member _.``Catalog loading is deterministic across repeated loads``() =
        let first = FixtureLoader.loadCatalog (catalogJson ())
        let second = FixtureLoader.loadCatalog (catalogJson ())
        equal second.Packages first.Packages
        equal second.Advertisements first.Advertisements
        equal second.OccupiedBindings first.OccupiedBindings
        equal second.Expectations.MissingArtifacts first.Expectations.MissingArtifacts

    [<Test>]
    member _.``Loading grants nothing and preserves claim observation distinction``() =
        let fixture = FixtureLoader.loadCatalog (catalogJson ())
        let advertised =
            fixture.Advertisements
            |> List.find (fun a -> a.Source = SourceId.create "src.bazaar" && a.Package = PackageId.create "pkg.contoso.cooling")
        let declared = fixture.Packages |> List.find (fun p -> p.Package = PackageId.create "pkg.contoso.cooling")
        Assert.That(advertised.AdvertisedVersion, Is.Not.EqualTo declared.Version)

    [<Test>]
    member _.``Malformed json is rejected``() =
        let failures = rejection (fun () -> FixtureLoader.loadCatalog "{ not json" |> ignore)
        Assert.That(failures |> List.exists (fun f -> f.Contains "not valid JSON"), Is.True)

    [<Test>]
    member _.``Unknown schema version is rejected``() =
        let mutated = (catalogJson ()).Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2")
        let failures = rejection (fun () -> FixtureLoader.loadCatalog mutated |> ignore)
        Assert.That(failures |> List.exists (fun f -> f.Contains "schemaVersion"), Is.True)

    [<Test>]
    member _.``Duplicate identifier is rejected by name``() =
        let mutated = (catalogJson ()).Replace("\"source\": \"src.contoso-mirror\"", "\"source\": \"src.local-cache\"")
        let failures = rejection (fun () -> FixtureLoader.loadCatalog mutated |> ignore)
        Assert.That(failures |> List.exists (fun f -> f.Contains "duplicate identifier 'src.local-cache'"), Is.True)

    [<Test>]
    member _.``Unresolved reference is rejected by name``() =
        let mutated = (catalogJson ()).Replace("\"package\": \"pkg.fabrikam.cooling\", \"advertisedVersion\"", "\"package\": \"pkg.unknown\", \"advertisedVersion\"")
        let failures = rejection (fun () -> FixtureLoader.loadCatalog mutated |> ignore)
        Assert.That(failures |> List.exists (fun f -> f.Contains "pkg.unknown"), Is.True)

    [<Test>]
    member _.``Unknown top level property is rejected``() =
        let mutated = (catalogJson ()).Replace("\"schemaVersion\": 1,", "\"schemaVersion\": 1, \"surprise\": true,")
        let failures = rejection (fun () -> FixtureLoader.loadCatalog mutated |> ignore)
        Assert.That(failures |> List.exists (fun f -> f.Contains "unknown property 'surprise'"), Is.True)

    [<Test>]
    member _.``Undeclared missing artifact is rejected``() =
        let mutated = (catalogJson ()).Replace("\"missingArtifacts\": [\"art.missing-db\"]", "\"missingArtifacts\": []")
        let failures = rejection (fun () -> FixtureLoader.loadCatalog mutated |> ignore)
        Assert.That(failures |> List.exists (fun f -> f.Contains "art.missing-db"), Is.True)

    [<Test>]
    member _.``Digest mismatch is rejected``() =
        let mutated = (catalogJson ()).Replace("fake-artifact:contoso-cooling:1.4.0", "fake-artifact:tampered")
        let failures = rejection (fun () -> FixtureLoader.loadCatalog mutated |> ignore)
        Assert.That(failures |> List.exists (fun f -> f.Contains "digest mismatch"), Is.True)

    [<Test>]
    member _.``Expectation disagreeing with data is rejected``() =
        let mutated = (catalogJson ()).Replace("\"genericCandidates\": [\"def.contoso.generic-telemetry\"]", "\"genericCandidates\": []")
        let failures = rejection (fun () -> FixtureLoader.loadCatalog mutated |> ignore)
        Assert.That(failures |> List.exists (fun f -> f.Contains "genericCandidates"), Is.True)

    [<Test>]
    member _.``Mice fixture keeps two distinct nodes with four functions each``() =
        let fixture = FixtureLoader.loadMiceTopology (miceJson ())
        let nodesWithFunctions = fixture.Functions |> List.map (fun f -> f.Node) |> List.distinct
        equal (List.length nodesWithFunctions) 2
        equal fixture.Expectations.FunctionsPerMouseNode 4
        equal (fixture.Functions |> List.filter (fun f -> f.Node = TopologyNodeId.create "node.mouse-a") |> List.length) 4
        equal (fixture.Functions |> List.filter (fun f -> f.Node = TopologyNodeId.create "node.mouse-b") |> List.length) 4

    [<Test>]
    member _.``Mice fixture surfaces malicious and contradictory claims without dropping them``() =
        let fixture = FixtureLoader.loadMiceTopology (miceJson ())
        equal fixture.Expectations.MaliciousClaims [ ClaimId.create "claim.b-hosts-root"; ClaimId.create "claim.b-owns-a" ]
        equal (List.length fixture.Expectations.ContradictoryClaims) 1
        Assert.That(fixture.Claims |> List.map (fun c -> c.Claim) |> List.contains (ClaimId.create "claim.b-owns-a"), Is.True)

    [<Test>]
    member _.``Mice fixture rejects unclassified claims``() =
        let mutated = (miceJson ()).Replace("\"maliciousClaims\": [\"claim.b-hosts-root\", \"claim.b-owns-a\"]", "\"maliciousClaims\": [\"claim.b-owns-a\"]")
        let failures = rejection (fun () -> FixtureLoader.loadMiceTopology mutated |> ignore)
        Assert.That(failures |> List.exists (fun f -> f.Contains "claim.b-hosts-root"), Is.True)
