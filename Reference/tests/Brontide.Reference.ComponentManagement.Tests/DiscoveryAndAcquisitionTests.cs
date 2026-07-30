using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Brontide.Reference.Experimental.ComponentManagement;
using NUnit.Framework;

namespace Brontide.Reference.ComponentManagement.Tests;

[TestFixture]
public sealed class DiscoveryAndAcquisitionTests
{
    private static string FixturePath(string name) =>
        Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "component-management",
            "fixtures",
            name);

    private static CatalogFixture LoadFixture() =>
        FixtureLoader.LoadCatalog(File.ReadAllText(FixturePath("cm0-catalog.json")));

    private static SourceEvidenceFixture LoadSourceEvidence(CatalogFixture fixture) =>
        Cm1FixtureLoader.LoadSourceEvidence(
            File.ReadAllText(FixturePath("cm1-source-evidence.json")),
            fixture);

    private static DiscoveryQuery Query(string version = "1.1") =>
        new(
            DiscoveryQueryId.Create("query.cooling"),
            ContractId.Create("brontide.fake.cooling-control"),
            VersionLiteral.Create(version),
            TargetEnvironmentId.Create("environment.fake-platform-1"),
            LifecycleRole.Ordinary,
            DefinitionId.Create("def.requester"),
            PublisherId.Create("pub.contoso"),
            Array.Empty<DefinitionConstraint>(),
            Array.Empty<DefinitionId>(),
            null,
            null,
            null,
            Array.Empty<TopologyNodeId>());

    private static FakeEvidencePolicy Policy() =>
        new(EvidencePolicyId.Create("policy.fake-local"));

    [Test]
    public void Discovery_accepts_zero_one_and_several_sources_without_lifecycle_effects()
    {
        var fixture = LoadFixture();
        var evidence = LoadSourceEvidence(fixture);
        var local = new FakeComponentSource(fixture, evidence, SourceId.Create("src.local-cache"));
        var mirror = new FakeComponentSource(fixture, evidence, SourceId.Create("src.contoso-mirror"));
        var bazaar = new FakeComponentSource(fixture, evidence, SourceId.Create("src.bazaar"));

        var zero = FakeDiscovery.Run(Query(), Array.Empty<FakeComponentSource>());
        var one = FakeDiscovery.Run(Query(), new[] { local });
        var several = FakeDiscovery.Run(Query(), new[] { bazaar, mirror, local });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(zero.Candidates, Is.Empty);
            Assert.That(one.Candidates, Has.Count.EqualTo(1));
            Assert.That(several.Candidates, Has.Count.EqualTo(3));
            Assert.That(
                several.Candidates.Select(candidate => candidate.Source),
                Is.EqualTo(
                    new[]
                    {
                        SourceId.Create("src.bazaar"),
                        SourceId.Create("src.contoso-mirror"),
                        SourceId.Create("src.local-cache"),
                    }));
            Assert.That(
                several.Candidates.Select(candidate => candidate.AdvertisedPackageVersion.Value),
                Is.EqualTo(new[] { "1.5.0-claimed", "1.4.0", "1.4.0" }));
            Assert.That(
                several.Candidates.Select(candidate => candidate.AvailableEvidence.Count),
                Is.EqualTo(new[] { 0, 2, 2 }));
            AssertNoEffects(zero.Effects);
            AssertNoEffects(one.Effects);
            AssertNoEffects(several.Effects);
        }
    }

    [Test]
    public void Source_evidence_fixture_fails_closed_on_unknown_duplicate_or_invented_attribution()
    {
        var catalog = LoadFixture();
        var json = File.ReadAllText(FixturePath("cm1-source-evidence.json"));
        var unknown = json.Replace(
            "\"source\": \"src.bazaar\", \"evidence\": \"ev.review-fab-positive\"",
            "\"source\": \"src.unknown\", \"evidence\": \"ev.review-fab-positive\"",
            StringComparison.Ordinal);
        var invented = json.Replace(
            "\"source\": \"src.bazaar\", \"evidence\": \"ev.review-fab-positive\"",
            "\"source\": \"src.local-cache\", \"evidence\": \"ev.review-fab-positive\"",
            StringComparison.Ordinal);
        var unknownEvidence = json.Replace(
            "\"evidence\": \"ev.review-fab-positive\"",
            "\"evidence\": \"ev.unknown\"",
            StringComparison.Ordinal);
        var duplicate = json.Replace(
            "{ \"source\": \"src.local-cache\", \"evidence\": \"ev.integrity-cooling\" },",
            "{ \"source\": \"src.local-cache\", \"evidence\": \"ev.integrity-cooling\" },\n    { \"source\": \"src.local-cache\", \"evidence\": \"ev.integrity-cooling\" },",
            StringComparison.Ordinal);

        var unknownFailure = Assert.Throws<FixtureFormatException>(
            () => Cm1FixtureLoader.LoadSourceEvidence(unknown, catalog));
        var inventedFailure = Assert.Throws<FixtureFormatException>(
            () => Cm1FixtureLoader.LoadSourceEvidence(invented, catalog));
        var unknownEvidenceFailure = Assert.Throws<FixtureFormatException>(
            () => Cm1FixtureLoader.LoadSourceEvidence(unknownEvidence, catalog));
        var duplicateFailure = Assert.Throws<FixtureFormatException>(
            () => Cm1FixtureLoader.LoadSourceEvidence(duplicate, catalog));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unknownFailure!.Failures, Has.Some.Contains("unknown source 'src.unknown'"));
            Assert.That(inventedFailure!.Failures, Has.Some.Contains("does not advertise a package carrying"));
            Assert.That(unknownEvidenceFailure!.Failures, Has.Some.Contains("unknown evidence 'ev.unknown'"));
            Assert.That(duplicateFailure!.Failures, Has.Some.Contains("duplicate availability"));
        }
    }

    [Test]
    public void Discovery_is_deterministic_under_source_and_advertisement_permutation()
    {
        var fixture = LoadFixture();
        var evidence = LoadSourceEvidence(fixture);
        var sourceIds = fixture.Sources.Select(source => source.Source).ToArray();
        var baseline = FakeDiscovery.Run(
            Query(),
            sourceIds.Select(source => new FakeComponentSource(fixture, evidence, source)));
        var baselineJson = JsonSerializer.Serialize(baseline);

        foreach (var sourceOrder in Permutations(sourceIds))
        {
            Assert.That(
                JsonSerializer.Serialize(
                    FakeDiscovery.Run(
                        Query(),
                        sourceOrder.Select(source => new FakeComponentSource(fixture, evidence, source)))),
                Is.EqualTo(baselineJson));
        }

        foreach (var source in sourceIds)
        {
            var advertisedPackages = fixture.Advertisements
                .Where(advertisement => advertisement.Source == source)
                .Select(advertisement => advertisement.Package)
                .ToArray();
            foreach (var advertisementOrder in Permutations(advertisedPackages))
            {
                var sources = sourceIds
                    .Select(
                        candidate =>
                            candidate == source
                                ? new FakeComponentSource(fixture, evidence, candidate, advertisementOrder)
                                : new FakeComponentSource(fixture, evidence, candidate))
                    .ToArray();
                Assert.That(
                    JsonSerializer.Serialize(FakeDiscovery.Run(Query(), sources)),
                    Is.EqualTo(baselineJson));
            }
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                baseline.Candidates.All(
                    candidate =>
                        candidate.Contract == Query().Contract
                        && candidate.Version == Query().Version
                        && fixture.Advertisements.Any(
                            advertisement =>
                                advertisement.Source == candidate.Source
                                && advertisement.Package == candidate.Package)),
                Is.True);
        }
    }

    [Test]
    public void Discovery_carries_context_without_giving_it_CM2_filtering_semantics()
    {
        var fixture = LoadFixture();
        var evidence = LoadSourceEvidence(fixture);
        var constraints = new[] { new DefinitionConstraint("constraint.fake", "value") };
        var preferred = new[] { DefinitionId.Create("def.fabrikam.cooling") };
        var topology = new[] { TopologyNodeId.Create("node.attachment-1") };
        var query = Query() with
        {
            DefinitionConstraints = constraints,
            PreferredProviders = preferred,
            ExistingBinding = BindingId.Create("bind.system-telemetry"),
            ContainingRegion = RegionId.Create("region.fake"),
            ContainingPort = PortId.Create("port.fake"),
            TopologyRequirements = topology,
        };
        var outcome = FakeDiscovery.Run(
            query,
            new[] { new FakeComponentSource(fixture, evidence, SourceId.Create("src.local-cache")) });

        constraints[0] = new DefinitionConstraint("constraint.mutated", "mutated");
        preferred[0] = DefinitionId.Create("def.mutated");
        topology[0] = TopologyNodeId.Create("node.mutated");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome.Query.DefinitionConstraints.Single().Name, Is.EqualTo("constraint.fake"));
            Assert.That(outcome.Query.PreferredProviders.Single(), Is.EqualTo(DefinitionId.Create("def.fabrikam.cooling")));
            Assert.That(outcome.Query.TopologyRequirements.Single(), Is.EqualTo(TopologyNodeId.Create("node.attachment-1")));
            Assert.That(outcome.Candidates, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void Source_endpoint_and_publisher_remain_distinct()
    {
        var fixture = LoadFixture();
        var evidence = LoadSourceEvidence(fixture);
        var bazaar = new FakeComponentSource(fixture, evidence, SourceId.Create("src.bazaar"));

        var contoso = FakeDiscovery.Run(Query("1.1"), new[] { bazaar }).Candidates.Single();
        var fabrikam = FakeDiscovery.Run(Query("1.0"), new[] { bazaar }).Candidates.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contoso.Source, Is.EqualTo(SourceId.Create("src.bazaar")));
            Assert.That(contoso.Publisher, Is.EqualTo(PublisherId.Create("pub.contoso")));
            Assert.That(fabrikam.Source, Is.EqualTo(SourceId.Create("src.bazaar")));
            Assert.That(fabrikam.Publisher, Is.EqualTo(PublisherId.Create("pub.fabrikam")));
            Assert.That(contoso.Storefront, Is.Null);
            Assert.That(fabrikam.Storefront, Is.Not.Null);
        }
    }

    [Test]
    public void Local_and_remote_sources_project_the_same_storefront_fields()
    {
        var fixture = LoadFixture();
        var evidence = LoadSourceEvidence(fixture);
        var local = FakeDiscovery.Run(
            Query(),
            new[] { new FakeComponentSource(fixture, evidence, SourceId.Create("src.local-cache")) }).Candidates.Single();
        var remote = FakeDiscovery.Run(
            Query(),
            new[] { new FakeComponentSource(fixture, evidence, SourceId.Create("src.contoso-mirror")) }).Candidates.Single();

        Assert.That(local.Storefront, Is.Not.Null);
        Assert.That(remote.Storefront, Is.Not.Null);
        Assert.That(
            JsonSerializer.Serialize(local.Storefront! with { Source = remote.Source }),
            Is.EqualTo(JsonSerializer.Serialize(remote.Storefront)));
    }

    [Test]
    public void Acquisition_is_immutable_after_source_removal_and_later_acquisition_is_refused()
    {
        var fixture = LoadFixture();
        var evidence = LoadSourceEvidence(fixture);
        var source = new FakeComponentSource(fixture, evidence, SourceId.Create("src.local-cache"));
        var fixtureArtifacts = (IList<ArtifactEntry>)fixture.Artifacts;
        var coolingIndex = fixtureArtifacts.ToList().FindIndex(
            artifact => artifact.Artifact == ArtifactId.Create("art.cooling-1-4-0"));
        fixtureArtifacts[coolingIndex] = fixtureArtifacts[coolingIndex] with
        {
            Content = "mutated-after-source-snapshot",
        };

        var acquired = source.Acquire(PackageId.Create("pkg.contoso.cooling"), Policy());
        Assert.That(acquired.IsSuccess, Is.True);
        var before = JsonSerializer.Serialize(acquired.Staged);
        var digest = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(acquired.Staged!.Artifact.Content)));

        source.Remove();

        var after = JsonSerializer.Serialize(acquired.Staged);
        var refused = source.Acquire(PackageId.Create("pkg.contoso.cooling"), Policy());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(after, Is.EqualTo(before));
            Assert.That(refused.IsSuccess, Is.False);
            Assert.That(refused.Failure?.Kind, Is.EqualTo(AcquisitionFailureKind.SourceUnavailable));
            Assert.That(refused.Staged, Is.Null);
            Assert.That(acquired.Staged.Artifact.Content, Is.EqualTo("fake-artifact:contoso-cooling:1.4.0"));
            Assert.That(digest, Is.EqualTo(acquired.Staged.Artifact.Sha256));
            AssertNoEffects(acquired.Staged!.Effects);
            AssertNoEffects(acquired.Effects);
            AssertNoEffects(refused.Effects);
            Assert.That(FakeDiscovery.Run(Query(), new[] { source }).ConsultedSources, Is.Empty);
        }
    }

    [Test]
    public void Acquisition_preserves_contested_evidence_and_policy_attribution()
    {
        var fixture = LoadFixture();
        var evidence = LoadSourceEvidence(fixture);
        var source = new FakeComponentSource(fixture, evidence, SourceId.Create("src.bazaar"));

        var result = source.Acquire(PackageId.Create("pkg.fabrikam.cooling"), Policy());

        Assert.That(result.IsSuccess, Is.True);
        var staged = result.Staged!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(staged.Evidence, Has.Count.EqualTo(2));
            Assert.That(staged.PolicyDecisions, Has.Count.EqualTo(2));
            Assert.That(staged.Evidence.Select(item => item.SuppliedBy).Distinct(), Is.EqualTo(new[] { source.Identity }));
            Assert.That(
                staged.Evidence.All(
                    item => evidence.Availability.Any(
                        availability =>
                            availability.Source == item.SuppliedBy
                            && availability.Evidence == item.Evidence.Evidence)),
                Is.True);
            Assert.That(
                staged.Evidence.Select(item => item.Evidence.Verdict),
                Is.EquivalentTo(new[] { EvidenceVerdict.Accepted, EvidenceVerdict.Rejected }));
            Assert.That(
                staged.PolicyDecisions.Select(item => item.Accepted),
                Is.EquivalentTo(new[] { true, false }));
            Assert.That(
                staged.PolicyDecisions.Select(item => item.Evidence),
                Is.EqualTo(staged.Evidence.Select(item => item.Evidence.Evidence)));
        }
    }

    [Test]
    public void Acquisition_refuses_every_declared_failure_without_a_partial_stage()
    {
        var fixture = LoadFixture();
        var evidence = LoadSourceEvidence(fixture);
        var local = new FakeComponentSource(fixture, evidence, SourceId.Create("src.local-cache"));
        var bazaar = new FakeComponentSource(fixture, evidence, SourceId.Create("src.bazaar"));
        var corruptArtifact = fixture.Artifacts.Single(
            artifact => artifact.Artifact == ArtifactId.Create("art.cooling-1-4-0")) with
        {
            Content = "corrupted-after-validation",
        };
        var corruptFixture = fixture with
        {
            Artifacts = fixture.Artifacts
                .Select(artifact => artifact.Artifact == corruptArtifact.Artifact ? corruptArtifact : artifact)
                .ToArray(),
        };
        var corrupt = new FakeComponentSource(corruptFixture, evidence, SourceId.Create("src.local-cache"));

        var unadvertised = local.Acquire(PackageId.Create("pkg.fabrikam.cooling"), Policy());
        var missing = bazaar.Acquire(PackageId.Create("pkg.northwind.database"), Policy());
        var integrity = corrupt.Acquire(PackageId.Create("pkg.contoso.cooling"), Policy());

        using (Assert.EnterMultipleScope())
        {
            AssertRefusal(unadvertised, AcquisitionFailureKind.PackageNotAdvertised);
            AssertRefusal(missing, AcquisitionFailureKind.ArtifactUnavailable);
            AssertRefusal(integrity, AcquisitionFailureKind.ArtifactIntegrityFailed);
        }
    }

    private static void AssertRefusal(AcquisitionResult result, AcquisitionFailureKind kind)
    {
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Staged, Is.Null);
        Assert.That(result.Failure?.Kind, Is.EqualTo(kind));
        AssertNoEffects(result.Effects);
    }

    private static void AssertNoEffects(Cm1EffectObservation effects)
    {
        Assert.That(
            new[]
            {
                effects.Selected,
                effects.Resolved,
                effects.Prepared,
                effects.Activated,
                effects.ActorEstablished,
                effects.CapabilityGranted,
            },
            Is.All.False);
    }

    private static IEnumerable<IReadOnlyList<T>> Permutations<T>(IReadOnlyList<T> values)
    {
        if (values.Count == 0)
        {
            yield return Array.Empty<T>();
            yield break;
        }

        for (var index = 0; index < values.Count; index++)
        {
            var head = values[index];
            var tail = values.Where((_, candidateIndex) => candidateIndex != index).ToArray();
            foreach (var suffix in Permutations(tail))
            {
                yield return new[] { head }.Concat(suffix).ToArray();
            }
        }
    }
}
