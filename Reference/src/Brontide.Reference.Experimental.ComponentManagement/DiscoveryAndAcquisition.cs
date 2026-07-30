using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace Brontide.Reference.Experimental.ComponentManagement;

public enum LifecycleRole
{
    Ordinary,
    LocalInitialisation,
    Interconnection,
    RelationalInitialisation,
}

public sealed record DefinitionConstraint(string Name, string Value);

public sealed record DiscoveryQuery(
    DiscoveryQueryId Query,
    ContractId Contract,
    VersionLiteral Version,
    TargetEnvironmentId TargetEnvironment,
    LifecycleRole LifecycleRole,
    DefinitionId? Requester,
    PublisherId? RequesterPublisher,
    IReadOnlyList<DefinitionConstraint> DefinitionConstraints,
    IReadOnlyList<DefinitionId> PreferredProviders,
    BindingId? ExistingBinding,
    RegionId? ContainingRegion,
    PortId? ContainingPort,
    IReadOnlyList<TopologyNodeId> TopologyRequirements);

public sealed record Cm1EffectObservation(
    bool Selected,
    bool Resolved,
    bool Prepared,
    bool Activated,
    bool ActorEstablished,
    bool CapabilityGranted)
{
    public static Cm1EffectObservation None { get; } = new(false, false, false, false, false, false);
}

public sealed record DiscoveryCandidate(
    DiscoveryQueryId Query,
    SourceId Source,
    PublisherId Publisher,
    PackageId Package,
    DefinitionId Definition,
    ContractId Contract,
    VersionLiteral Version,
    VersionLiteral AdvertisedPackageVersion,
    ArtifactId Artifact,
    IReadOnlyList<EvidenceId> AvailableEvidence,
    StorefrontEntry? Storefront);

public sealed record DiscoveryOutcome(
    DiscoveryQuery Query,
    IReadOnlyList<SourceId> ConsultedSources,
    IReadOnlyList<DiscoveryCandidate> Candidates,
    Cm1EffectObservation Effects);

public sealed record AttributedEvidence(SourceId SuppliedBy, EvidenceEntry Evidence);

public sealed record EvidencePolicyDecision(
    EvidencePolicyId Policy,
    SourceId SuppliedBy,
    EvidenceId Evidence,
    IssuerId Issuer,
    bool Accepted,
    string Reason);

public sealed record StagedArtifact(
    SourceId Source,
    PackageEntry Package,
    IReadOnlyList<ComponentDefinitionEntry> Definitions,
    ArtifactEntry Artifact,
    IReadOnlyList<AttributedEvidence> Evidence,
    IReadOnlyList<EvidencePolicyDecision> PolicyDecisions,
    StorefrontEntry? Storefront,
    Cm1EffectObservation Effects);

public enum AcquisitionFailureKind
{
    SourceUnavailable,
    PackageNotAdvertised,
    ArtifactUnavailable,
    ArtifactIntegrityFailed,
}

public sealed record AcquisitionFailure(
    AcquisitionFailureKind Kind,
    SourceId Source,
    PackageId Package,
    string Reason);

public sealed record AcquisitionResult
{
    private AcquisitionResult(StagedArtifact? staged, AcquisitionFailure? failure)
    {
        Staged = staged;
        Failure = failure;
    }

    public StagedArtifact? Staged { get; }

    public AcquisitionFailure? Failure { get; }

    public bool IsSuccess => Staged is not null;

    internal static AcquisitionResult Success(StagedArtifact staged) => new(staged, null);

    internal static AcquisitionResult Refused(AcquisitionFailure failure) => new(null, failure);
}

/// <summary>
/// An attributable fake policy. It deliberately decides each evidence item independently so
/// contradictory claims remain visible instead of collapsing into a source-level trust flag.
/// </summary>
public sealed class FakeEvidencePolicy
{
    public FakeEvidencePolicy(EvidencePolicyId identity)
    {
        Identity = identity;
    }

    public EvidencePolicyId Identity { get; }

    public EvidencePolicyDecision Evaluate(SourceId source, EvidenceEntry evidence) =>
        new(
            Identity,
            source,
            evidence.Evidence,
            evidence.Issuer,
            evidence.Verdict == EvidenceVerdict.Accepted,
            evidence.Verdict == EvidenceVerdict.Accepted
                ? $"policy {Identity} accepts the attributable {evidence.Kind} claim"
                : $"policy {Identity} rejects the attributable {evidence.Kind} claim");
}

/// <summary>
/// Controlled fake source over one immutable fixture snapshot. Availability may change, while any
/// staged value already returned remains detached from this object.
/// </summary>
public sealed class FakeComponentSource
{
    private readonly CatalogFixture _fixture;
    private readonly SourceEntry _source;
    private readonly ReadOnlyCollection<AdvertisementEntry> _advertisements;
    private bool _available = true;

    public FakeComponentSource(
        CatalogFixture fixture,
        SourceId source,
        IReadOnlyList<PackageId>? advertisementEnumeration = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = SnapshotFixture(fixture);
        _source = _fixture.Sources.SingleOrDefault(candidate => candidate.Source == source)
            ?? throw new ArgumentException($"Fixture has no source '{source}'.", nameof(source));

        var advertisements = _fixture.Advertisements.Where(candidate => candidate.Source == source).ToArray();
        if (advertisementEnumeration is not null)
        {
            var byPackage = advertisements.ToDictionary(candidate => candidate.Package);
            if (advertisementEnumeration.Count != advertisements.Length
                || advertisementEnumeration.Distinct().Count() != advertisements.Length
                || advertisementEnumeration.Any(package => !byPackage.ContainsKey(package)))
            {
                throw new ArgumentException(
                    $"Advertisement enumeration for '{source}' must name every advertised package exactly once.",
                    nameof(advertisementEnumeration));
            }

            advertisements = advertisementEnumeration.Select(package => byPackage[package]).ToArray();
        }

        _advertisements = Array.AsReadOnly(advertisements);
    }

    public SourceId Identity => _source.Source;

    public SourceKind Kind => _source.Kind;

    public bool IsAvailable => _available;

    public void Remove() => _available = false;

    internal IReadOnlyList<DiscoveryCandidate> Discover(DiscoveryQuery query)
    {
        if (!_available)
        {
            return Array.Empty<DiscoveryCandidate>();
        }

        var candidates = new List<DiscoveryCandidate>();
        foreach (var advertisement in _advertisements)
        {
            var package = _fixture.Packages.Single(candidate => candidate.Package == advertisement.Package);
            var evidence = _fixture.Evidence
                .Where(candidate => candidate.SubjectArtifact == package.Artifact)
                .Select(candidate => candidate.Evidence)
                .OrderBy(candidate => candidate.Value, StringComparer.Ordinal)
                .ToArray();
            var storefront = _fixture.Storefront.SingleOrDefault(
                candidate => candidate.Source == Identity && candidate.Package == package.Package);

            foreach (var definition in _fixture.ComponentDefinitions.Where(
                         candidate => candidate.Package == package.Package))
            {
                foreach (var provision in definition.Provides.Where(
                             candidate => candidate.Contract == query.Contract && candidate.Version == query.Version))
                {
                    candidates.Add(
                        new DiscoveryCandidate(
                            query.Query,
                            Identity,
                            package.Publisher,
                            package.Package,
                            definition.Definition,
                            provision.Contract,
                            provision.Version,
                            advertisement.AdvertisedVersion,
                            package.Artifact,
                            Array.AsReadOnly(evidence),
                            storefront));
                }
            }
        }

        return candidates;
    }

    public AcquisitionResult Acquire(PackageId packageIdentity, FakeEvidencePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (!_available)
        {
            return Refuse(AcquisitionFailureKind.SourceUnavailable, packageIdentity, "source is unavailable");
        }

        if (!_advertisements.Any(candidate => candidate.Package == packageIdentity))
        {
            return Refuse(
                AcquisitionFailureKind.PackageNotAdvertised,
                packageIdentity,
                "package is not advertised by this source");
        }

        var package = _fixture.Packages.Single(candidate => candidate.Package == packageIdentity);
        var artifact = _fixture.Artifacts.SingleOrDefault(candidate => candidate.Artifact == package.Artifact);
        if (artifact is null)
        {
            return Refuse(
                AcquisitionFailureKind.ArtifactUnavailable,
                packageIdentity,
                $"artifact '{package.Artifact}' is unavailable");
        }

        var actualDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(artifact.Content)));
        if (!string.Equals(actualDigest, artifact.Sha256, StringComparison.Ordinal))
        {
            return Refuse(
                AcquisitionFailureKind.ArtifactIntegrityFailed,
                packageIdentity,
                $"artifact '{artifact.Artifact}' digest does not match its immutable content");
        }

        var definitions = _fixture.ComponentDefinitions
            .Where(candidate => candidate.Package == packageIdentity)
            .OrderBy(candidate => candidate.Definition.Value, StringComparer.Ordinal)
            .ToArray();
        var evidence = _fixture.Evidence
            .Where(candidate => candidate.SubjectArtifact == artifact.Artifact)
            .OrderBy(candidate => candidate.Evidence.Value, StringComparer.Ordinal)
            .Select(candidate => new AttributedEvidence(Identity, candidate))
            .ToArray();
        var decisions = evidence.Select(candidate => policy.Evaluate(Identity, candidate.Evidence)).ToArray();
        var storefront = _fixture.Storefront.SingleOrDefault(
            candidate => candidate.Source == Identity && candidate.Package == packageIdentity);

        return AcquisitionResult.Success(
            new StagedArtifact(
                Identity,
                package with { },
                Array.AsReadOnly(definitions),
                artifact with { },
                Array.AsReadOnly(evidence),
                Array.AsReadOnly(decisions),
                storefront is null ? null : storefront with { },
                Cm1EffectObservation.None));
    }

    private AcquisitionResult Refuse(AcquisitionFailureKind kind, PackageId package, string reason) =>
        AcquisitionResult.Refused(new AcquisitionFailure(kind, Identity, package, reason));

    private static CatalogFixture SnapshotFixture(CatalogFixture fixture) =>
        fixture with
        {
            Sources = Array.AsReadOnly(
                fixture.Sources
                    .Select(
                        source => source with
                        {
                            ServesPublishers = Array.AsReadOnly(source.ServesPublishers.ToArray()),
                        })
                    .ToArray()),
            Packages = Array.AsReadOnly(fixture.Packages.Select(package => package with { }).ToArray()),
            Advertisements = Array.AsReadOnly(
                fixture.Advertisements.Select(advertisement => advertisement with { }).ToArray()),
            ComponentDefinitions = Array.AsReadOnly(
                fixture.ComponentDefinitions
                    .Select(
                        definition => definition with
                        {
                            Provides = Array.AsReadOnly(definition.Provides.ToArray()),
                            Requires = Array.AsReadOnly(definition.Requires.ToArray()),
                        })
                    .ToArray()),
            Artifacts = Array.AsReadOnly(fixture.Artifacts.Select(artifact => artifact with { }).ToArray()),
            Evidence = Array.AsReadOnly(fixture.Evidence.Select(evidence => evidence with { }).ToArray()),
            Storefront = Array.AsReadOnly(
                fixture.Storefront
                    .Select(
                        storefront => storefront with
                        {
                            Categories = Array.AsReadOnly(storefront.Categories.ToArray()),
                            DependencySummary = Array.AsReadOnly(storefront.DependencySummary.ToArray()),
                            Alternatives = Array.AsReadOnly(storefront.Alternatives.ToArray()),
                        })
                    .ToArray()),
        };
}

public static class FakeDiscovery
{
    public static DiscoveryOutcome Run(DiscoveryQuery query, IEnumerable<FakeComponentSource> sources)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(sources);

        var sourceSnapshot = sources.ToArray();
        var consulted = sourceSnapshot
            .Where(source => source.IsAvailable)
            .Select(source => source.Identity)
            .OrderBy(source => source.Value, StringComparer.Ordinal)
            .ToArray();
        var candidates = sourceSnapshot
            .SelectMany(source => source.Discover(query))
            .OrderBy(candidate => candidate.Source.Value, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Package.Value, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Definition.Value, StringComparer.Ordinal)
            .ToArray();

        return new DiscoveryOutcome(
            query,
            Array.AsReadOnly(consulted),
            Array.AsReadOnly(candidates),
            Cm1EffectObservation.None);
    }
}
