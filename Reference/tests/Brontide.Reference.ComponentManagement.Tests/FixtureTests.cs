using Brontide.Reference.Experimental.ComponentManagement;
using NUnit.Framework;

namespace Brontide.Reference.ComponentManagement.Tests;

[TestFixture]
public sealed class FixtureTests
{
    private static string FixturePath(string name) =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "component-management", "fixtures", name);

    private static string CatalogJson() => File.ReadAllText(FixturePath("cm0-catalog.json"));

    private static string MiceJson() => File.ReadAllText(FixturePath("cm0-mice-topology.json"));

    [TestCase("")]
    [TestCase("Has Spaces")]
    [TestCase("UPPER")]
    [TestCase("under_score")]
    public void Identifier_creation_rejects_invalid_syntax(string value)
    {
        Assert.That(() => SourceId.Create(value), Throws.ArgumentException);
    }

    [Test]
    public void Identifier_spaces_are_distinct_types_over_one_primitive()
    {
        var source = SourceId.Create("shared.token");
        var publisher = PublisherId.Create("shared.token");
        Assert.Multiple(() =>
        {
            Assert.That(source.Value, Is.EqualTo(publisher.Value));
            Assert.That(source.GetType(), Is.Not.EqualTo(publisher.GetType()));
        });
    }

    [TestCase("1..1", 1, 1)]
    [TestCase("0..*", 0, null)]
    public void Cardinality_parses_declared_forms(string text, int minimum, int? maximum)
    {
        var cardinality = Cardinality.Parse(text);
        Assert.Multiple(() =>
        {
            Assert.That(cardinality.Minimum, Is.EqualTo(minimum));
            Assert.That(cardinality.Maximum, Is.EqualTo(maximum));
        });
    }

    [TestCase("1")]
    [TestCase("2..1")]
    [TestCase("-1..2")]
    public void Cardinality_rejects_invalid_forms(string text)
    {
        Assert.That(() => Cardinality.Parse(text), Throws.ArgumentException);
    }

    [Test]
    public void Catalog_fixture_loads_with_expected_shape()
    {
        var fixture = FixtureLoader.LoadCatalog(CatalogJson());
        Assert.Multiple(() =>
        {
            Assert.That(fixture.Sources, Has.Count.EqualTo(3));
            Assert.That(fixture.Packages, Has.Count.EqualTo(5));
            Assert.That(fixture.ComponentDefinitions, Has.Count.EqualTo(5));
            Assert.That(fixture.ActivatedOccurrences, Has.Count.EqualTo(3));
            Assert.That(fixture.Storefront, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void Catalog_expectations_surface_every_required_cm0_case()
    {
        var expectations = FixtureLoader.LoadCatalog(CatalogJson()).Expectations;
        Assert.Multiple(() =>
        {
            Assert.That(expectations.DuplicateComponentIdentityAcrossSources, Is.EqualTo(new[] { PackageId.Create("pkg.contoso.cooling") }));
            Assert.That(expectations.MirroredPublishers, Is.EqualTo(new[] { PublisherId.Create("pub.contoso") }));
            Assert.That(expectations.MultiPublisherSources, Is.EqualTo(new[] { SourceId.Create("src.bazaar") }));
            Assert.That(expectations.ContractsWithSeveralDefinitions, Has.Count.EqualTo(2));
            Assert.That(expectations.DefinitionsWithSeveralOccurrences, Is.EqualTo(new[] { DefinitionId.Create("def.northwind.telemetry") }));
            Assert.That(expectations.OccupiedBindings, Has.Count.EqualTo(2));
            Assert.That(expectations.SystemDefaultScopes, Is.EqualTo(new[] { BindingScopeId.Create("scope.system") }));
            Assert.That(expectations.ExplicitPreferences, Has.Count.EqualTo(1));
            Assert.That(expectations.GenericCandidates, Is.EqualTo(new[] { DefinitionId.Create("def.contoso.generic-telemetry") }));
            Assert.That(expectations.ConflictingVersionClaims, Is.EqualTo(new[] { PackageId.Create("pkg.contoso.cooling") }));
            Assert.That(expectations.MissingArtifacts, Is.EqualTo(new[] { ArtifactId.Create("art.missing-db") }));
            Assert.That(expectations.ContradictoryEvidence, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Catalog_loading_is_deterministic_across_repeated_loads()
    {
        var first = FixtureLoader.LoadCatalog(CatalogJson());
        var second = FixtureLoader.LoadCatalog(CatalogJson());
        Assert.Multiple(() =>
        {
            Assert.That(second.Packages, Is.EqualTo(first.Packages));
            Assert.That(second.Advertisements, Is.EqualTo(first.Advertisements));
            Assert.That(second.OccupiedBindings, Is.EqualTo(first.OccupiedBindings));
            Assert.That(second.Expectations.MissingArtifacts, Is.EqualTo(first.Expectations.MissingArtifacts));
        });
    }

    [Test]
    public void Loading_grants_nothing_and_preserves_claim_observation_distinction()
    {
        var fixture = FixtureLoader.LoadCatalog(CatalogJson());
        var advertised = fixture.Advertisements.Single(a =>
            a.Source == SourceId.Create("src.bazaar") && a.Package == PackageId.Create("pkg.contoso.cooling"));
        var declared = fixture.Packages.Single(p => p.Package == PackageId.Create("pkg.contoso.cooling"));
        Assert.That(advertised.AdvertisedVersion, Is.Not.EqualTo(declared.Version),
            "the source's claim must stay distinguishable from the package's declared version");
    }

    [Test]
    public void Malformed_json_is_rejected()
    {
        var exception = Assert.Throws<FixtureFormatException>(() => FixtureLoader.LoadCatalog("{ not json"));
        Assert.That(exception!.Failures, Has.Some.Contains("not valid JSON"));
    }

    [Test]
    public void Unknown_schema_version_is_rejected()
    {
        var mutated = CatalogJson().Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2", StringComparison.Ordinal);
        var exception = Assert.Throws<FixtureFormatException>(() => FixtureLoader.LoadCatalog(mutated));
        Assert.That(exception!.Failures, Has.Some.Contains("schemaVersion"));
    }

    [Test]
    public void Duplicate_identifier_is_rejected_by_name()
    {
        var mutated = CatalogJson().Replace("\"source\": \"src.contoso-mirror\"", "\"source\": \"src.local-cache\"", StringComparison.Ordinal);
        var exception = Assert.Throws<FixtureFormatException>(() => FixtureLoader.LoadCatalog(mutated));
        Assert.That(exception!.Failures, Has.Some.Contains("duplicate identifier 'src.local-cache'"));
    }

    [Test]
    public void Unresolved_reference_is_rejected_by_name()
    {
        var mutated = CatalogJson().Replace("\"package\": \"pkg.fabrikam.cooling\", \"advertisedVersion\"", "\"package\": \"pkg.unknown\", \"advertisedVersion\"", StringComparison.Ordinal);
        var exception = Assert.Throws<FixtureFormatException>(() => FixtureLoader.LoadCatalog(mutated));
        Assert.That(exception!.Failures, Has.Some.Contains("pkg.unknown"));
    }

    [Test]
    public void Unknown_top_level_property_is_rejected()
    {
        var mutated = CatalogJson().Replace("\"schemaVersion\": 1,", "\"schemaVersion\": 1, \"surprise\": true,", StringComparison.Ordinal);
        var exception = Assert.Throws<FixtureFormatException>(() => FixtureLoader.LoadCatalog(mutated));
        Assert.That(exception!.Failures, Has.Some.Contains("unknown property 'surprise'"));
    }

    [Test]
    public void Undeclared_missing_artifact_is_rejected()
    {
        var mutated = CatalogJson().Replace("\"missingArtifacts\": [\"art.missing-db\"]", "\"missingArtifacts\": []", StringComparison.Ordinal);
        var exception = Assert.Throws<FixtureFormatException>(() => FixtureLoader.LoadCatalog(mutated));
        Assert.That(exception!.Failures, Has.Some.Contains("art.missing-db"));
    }

    [Test]
    public void Digest_mismatch_is_rejected()
    {
        var mutated = CatalogJson().Replace("fake-artifact:contoso-cooling:1.4.0", "fake-artifact:tampered", StringComparison.Ordinal);
        var exception = Assert.Throws<FixtureFormatException>(() => FixtureLoader.LoadCatalog(mutated));
        Assert.That(exception!.Failures, Has.Some.Contains("digest mismatch"));
    }

    [Test]
    public void Expectation_disagreeing_with_data_is_rejected()
    {
        var mutated = CatalogJson().Replace("\"genericCandidates\": [\"def.contoso.generic-telemetry\"]", "\"genericCandidates\": []", StringComparison.Ordinal);
        var exception = Assert.Throws<FixtureFormatException>(() => FixtureLoader.LoadCatalog(mutated));
        Assert.That(exception!.Failures, Has.Some.Contains("genericCandidates"));
    }

    [Test]
    public void Mice_fixture_keeps_two_distinct_nodes_with_four_functions_each()
    {
        var fixture = FixtureLoader.LoadMiceTopology(MiceJson());
        var nodesWithFunctions = fixture.Functions.Select(f => f.Node).Distinct().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(nodesWithFunctions, Has.Length.EqualTo(2));
            Assert.That(fixture.Expectations.FunctionsPerMouseNode, Is.EqualTo(4));
            Assert.That(fixture.Functions.Count(f => f.Node == TopologyNodeId.Create("node.mouse-a")), Is.EqualTo(4));
            Assert.That(fixture.Functions.Count(f => f.Node == TopologyNodeId.Create("node.mouse-b")), Is.EqualTo(4));
        });
    }

    [Test]
    public void Mice_fixture_surfaces_malicious_and_contradictory_claims_without_dropping_them()
    {
        var fixture = FixtureLoader.LoadMiceTopology(MiceJson());
        Assert.Multiple(() =>
        {
            Assert.That(fixture.Expectations.MaliciousClaims, Is.EqualTo(new[]
            {
                ClaimId.Create("claim.b-hosts-root"),
                ClaimId.Create("claim.b-owns-a"),
            }));
            Assert.That(fixture.Expectations.ContradictoryClaims, Has.Count.EqualTo(1));
            Assert.That(fixture.Claims.Select(c => c.Claim), Does.Contain(ClaimId.Create("claim.b-owns-a")),
                "malicious claims stay recorded as attributable data rather than being silently dropped");
        });
    }

    [Test]
    public void Mice_fixture_rejects_unclassified_claims()
    {
        var mutated = MiceJson().Replace(
            "\"maliciousClaims\": [\"claim.b-hosts-root\", \"claim.b-owns-a\"]",
            "\"maliciousClaims\": [\"claim.b-owns-a\"]",
            StringComparison.Ordinal);
        var exception = Assert.Throws<FixtureFormatException>(() => FixtureLoader.LoadMiceTopology(mutated));
        Assert.That(exception!.Failures, Has.Some.Contains("claim.b-hosts-root"));
    }
}
