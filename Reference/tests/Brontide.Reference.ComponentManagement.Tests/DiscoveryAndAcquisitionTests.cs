using System.Text.Json;
using Brontide.Reference.Experimental.ComponentManagement;
using NUnit.Framework;

namespace Brontide.Reference.ComponentManagement.Tests;

[TestFixture]
public sealed class DiscoveryAndAcquisitionTests
{
    private static string FixturePath =>
        Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "component-management",
            "fixtures",
            "cm0-catalog.json");

    private static CatalogFixture LoadFixture() => FixtureLoader.LoadCatalog(File.ReadAllText(FixturePath));

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
        var local = new FakeComponentSource(fixture, SourceId.Create("src.local-cache"));
        var mirror = new FakeComponentSource(fixture, SourceId.Create("src.contoso-mirror"));
        var bazaar = new FakeComponentSource(fixture, SourceId.Create("src.bazaar"));

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
            AssertNoEffects(zero.Effects);
            AssertNoEffects(one.Effects);
            AssertNoEffects(several.Effects);
        }
    }

    [Test]
    public void Discovery_is_deterministic_under_source_and_advertisement_permutation()
    {
        var fixture = LoadFixture();
        var normalBazaar = new FakeComponentSource(fixture, SourceId.Create("src.bazaar"));
        var reversedBazaar = new FakeComponentSource(
            fixture,
            SourceId.Create("src.bazaar"),
            fixture.Advertisements
                .Where(advertisement => advertisement.Source == SourceId.Create("src.bazaar"))
                .Select(advertisement => advertisement.Package)
                .Reverse()
                .ToArray());
        var local = new FakeComponentSource(fixture, SourceId.Create("src.local-cache"));

        var first = FakeDiscovery.Run(Query(), new[] { normalBazaar, local });
        var second = FakeDiscovery.Run(Query(), new[] { local, reversedBazaar });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second.ConsultedSources, Is.EqualTo(first.ConsultedSources));
            Assert.That(
                JsonSerializer.Serialize(second.Candidates),
                Is.EqualTo(JsonSerializer.Serialize(first.Candidates)));
            Assert.That(
                first.Candidates.All(
                    candidate =>
                        candidate.Contract == Query().Contract
                        && candidate.Version == Query().Version),
                Is.True);
        }
    }

    [Test]
    public void Source_endpoint_and_publisher_remain_distinct()
    {
        var fixture = LoadFixture();
        var bazaar = new FakeComponentSource(fixture, SourceId.Create("src.bazaar"));

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
    public void Acquisition_is_immutable_after_source_removal_and_later_acquisition_is_refused()
    {
        var fixture = LoadFixture();
        var source = new FakeComponentSource(fixture, SourceId.Create("src.local-cache"));

        var acquired = source.Acquire(PackageId.Create("pkg.contoso.cooling"), Policy());
        Assert.That(acquired.IsSuccess, Is.True);
        var before = JsonSerializer.Serialize(acquired.Staged);

        source.Remove();

        var after = JsonSerializer.Serialize(acquired.Staged);
        var refused = source.Acquire(PackageId.Create("pkg.contoso.cooling"), Policy());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(after, Is.EqualTo(before));
            Assert.That(refused.IsSuccess, Is.False);
            Assert.That(refused.Failure?.Kind, Is.EqualTo(AcquisitionFailureKind.SourceUnavailable));
            Assert.That(refused.Staged, Is.Null);
            AssertNoEffects(acquired.Staged!.Effects);
        }
    }

    [Test]
    public void Acquisition_preserves_contested_evidence_and_policy_attribution()
    {
        var fixture = LoadFixture();
        var source = new FakeComponentSource(fixture, SourceId.Create("src.bazaar"));

        var result = source.Acquire(PackageId.Create("pkg.fabrikam.cooling"), Policy());

        Assert.That(result.IsSuccess, Is.True);
        var staged = result.Staged!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(staged.Evidence, Has.Count.EqualTo(2));
            Assert.That(staged.PolicyDecisions, Has.Count.EqualTo(2));
            Assert.That(staged.Evidence.Select(item => item.SuppliedBy).Distinct(), Is.EqualTo(new[] { source.Identity }));
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
        var local = new FakeComponentSource(fixture, SourceId.Create("src.local-cache"));
        var bazaar = new FakeComponentSource(fixture, SourceId.Create("src.bazaar"));
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
        var corrupt = new FakeComponentSource(corruptFixture, SourceId.Create("src.local-cache"));

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
}
