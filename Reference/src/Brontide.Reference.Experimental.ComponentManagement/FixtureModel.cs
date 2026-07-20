namespace Brontide.Reference.Experimental.ComponentManagement;

public enum SourceKind
{
    Local,
    Remote,
}

public enum ScopeKind
{
    SystemDefault,
    Application,
    Session,
}

public enum EvidenceKind
{
    Integrity,
    Origin,
    Signature,
    Review,
}

public enum EvidenceVerdict
{
    Accepted,
    Rejected,
}

public enum TopologyNodeKind
{
    Host,
    Attachment,
}

public enum TopologyRelation
{
    PartOf,
    AttachedThrough,
    HostedBy,
    SamePhysicalAssembly,
    SharesPowerDomain,
    SharesFailureDomain,
}

public sealed record ContractEntry(ContractId Contract, IReadOnlyList<VersionLiteral> Versions);

public sealed record PublisherEntry(PublisherId Publisher, string DisplayName);

public sealed record SourceEntry(
    SourceId Source,
    SourceKind Kind,
    string DisplayName,
    IReadOnlyList<PublisherId> ServesPublishers);

public sealed record PackageEntry(PackageId Package, PublisherId Publisher, VersionLiteral Version, ArtifactId Artifact);

public sealed record AdvertisementEntry(SourceId Source, PackageId Package, VersionLiteral AdvertisedVersion);

public sealed record ProvidedContract(ContractId Contract, VersionLiteral Version);

public sealed record RequiredContract(ContractId Contract, VersionLiteral Version, BindingScopeId Scope, Cardinality Cardinality);

public sealed record ComponentDefinitionEntry(
    DefinitionId Definition,
    PackageId Package,
    IReadOnlyList<ProvidedContract> Provides,
    IReadOnlyList<RequiredContract> Requires,
    bool Generic);

public sealed record BindingScopeEntry(BindingScopeId Scope, ScopeKind Kind);

public sealed record ActivatedOccurrenceEntry(OccurrenceId Occurrence, DefinitionId Definition, IReadOnlyList<ActorId> Actors);

public sealed record OccupiedBindingEntry(
    BindingId Binding,
    BindingScopeId Scope,
    ContractId Contract,
    DefinitionId OccupantDefinition,
    OccurrenceId OccupantOccurrence);

public sealed record PreferenceEntry(
    PreferenceId Preference,
    DefinitionId DeclaredBy,
    ContractId Contract,
    DefinitionId PreferredDefinition);

public sealed record ArtifactEntry(ArtifactId Artifact, string Content, string Sha256);

public sealed record EvidenceEntry(
    EvidenceId Evidence,
    ArtifactId SubjectArtifact,
    EvidenceKind Kind,
    IssuerId Issuer,
    EvidenceVerdict Verdict,
    string Detail);

public sealed record StorefrontEntry(
    SourceId Source,
    PackageId Package,
    string DisplayName,
    string Description,
    string Imagery,
    IReadOnlyList<string> Categories,
    VersionLiteral Version,
    string Compatibility,
    string EvidenceStatus,
    string LifecycleState,
    IReadOnlyList<string> DependencySummary,
    IReadOnlyList<PackageId> Alternatives);

public sealed record CatalogExpectations(
    IReadOnlyList<PackageId> DuplicateComponentIdentityAcrossSources,
    IReadOnlyList<PublisherId> MirroredPublishers,
    IReadOnlyList<SourceId> MultiPublisherSources,
    IReadOnlyList<ContractId> ContractsWithSeveralDefinitions,
    IReadOnlyList<DefinitionId> DefinitionsWithSeveralOccurrences,
    IReadOnlyList<BindingId> OccupiedBindings,
    IReadOnlyList<BindingScopeId> SystemDefaultScopes,
    IReadOnlyList<PreferenceId> ExplicitPreferences,
    IReadOnlyList<DefinitionId> GenericCandidates,
    IReadOnlyList<PackageId> ConflictingVersionClaims,
    IReadOnlyList<ArtifactId> MissingArtifacts,
    IReadOnlyList<IReadOnlyList<EvidenceId>> ContradictoryEvidence);

public sealed record CatalogFixture(
    string Description,
    IReadOnlyList<ContractEntry> Contracts,
    IReadOnlyList<PublisherEntry> Publishers,
    IReadOnlyList<SourceEntry> Sources,
    IReadOnlyList<PackageEntry> Packages,
    IReadOnlyList<AdvertisementEntry> Advertisements,
    IReadOnlyList<ComponentDefinitionEntry> ComponentDefinitions,
    IReadOnlyList<BindingScopeEntry> BindingScopes,
    IReadOnlyList<ActivatedOccurrenceEntry> ActivatedOccurrences,
    IReadOnlyList<OccupiedBindingEntry> OccupiedBindings,
    IReadOnlyList<PreferenceEntry> Preferences,
    IReadOnlyList<ArtifactEntry> Artifacts,
    IReadOnlyList<EvidenceEntry> Evidence,
    IReadOnlyList<StorefrontEntry> Storefront,
    CatalogExpectations Expectations);

public sealed record ObserverEntry(ObserverId Observer);

public sealed record TopologyNodeEntry(TopologyNodeId Node, ObserverId Observer, TopologyNodeKind Kind);

public sealed record FunctionEntry(FunctionId Function, ContractId Contract, TopologyNodeId Node, ActorId Actor);

public sealed record TopologyClaimEntry(
    ClaimId Claim,
    ObserverId AssertedBy,
    TopologyRelation Relation,
    TopologyNodeId From,
    TopologyNodeId To);

public sealed record MiceExpectations(
    IReadOnlyList<TopologyNodeId> DistinctMouseNodes,
    int FunctionsPerMouseNode,
    IReadOnlyList<ClaimId> AttributableClaims,
    IReadOnlyList<IReadOnlyList<ClaimId>> ContradictoryClaims,
    IReadOnlyList<ClaimId> MaliciousClaims);

public sealed record MiceTopologyFixture(
    string Description,
    IReadOnlyList<ContractEntry> Contracts,
    IReadOnlyList<ObserverEntry> Observers,
    IReadOnlyList<TopologyNodeEntry> TopologyNodes,
    IReadOnlyList<FunctionEntry> Functions,
    IReadOnlyList<TopologyClaimEntry> Claims,
    MiceExpectations Expectations);

/// <summary>
/// Raised when a fixture is malformed, references unknown identities, or disagrees with its own
/// declared expectations. Carries every failure so a defect report is deterministic and complete.
/// </summary>
public sealed class FixtureFormatException : Exception
{
    public FixtureFormatException(IReadOnlyList<string> failures)
        : base("Component-management fixture rejected: " + string.Join("; ", failures))
    {
        Failures = failures;
    }

    public IReadOnlyList<string> Failures { get; }
}
