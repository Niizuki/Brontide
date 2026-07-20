using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Brontide.Reference.Experimental.ComponentManagement;

/// <summary>
/// Strict loader for the shared data-only fixtures under <c>component-management/fixtures</c>.
/// Parsing fails closed: unknown schema versions, unknown properties, duplicate identifiers,
/// unresolved references, digest mismatches, and expectation mismatches are all rejected with a
/// complete, deterministic failure list. Loading grants no Capability and establishes no Actor.
/// </summary>
public static class FixtureLoader
{
    public static CatalogFixture LoadCatalog(string json)
    {
        var reader = new Reader();
        var root = reader.ParseRoot(json, "cm0-catalog", new[]
        {
            "schemaVersion", "fixture", "description", "contracts", "publishers", "sources",
            "packages", "advertisements", "componentDefinitions", "bindingScopes",
            "activatedOccurrences", "occupiedBindings", "preferences", "artifacts", "evidence",
            "storefront", "expectations",
        });
        if (root is null)
        {
            throw reader.Rejection();
        }

        var document = root.Value;
        var description = reader.GetString(document, "description");
        var contracts = reader.ParseEntries(document, "contracts", new[] { "contract", "versions" }, (e, r) =>
            new ContractEntry(
                ContractId.Create(r.GetString(e, "contract")),
                r.GetStringList(e, "versions").Select(VersionLiteral.Create).ToArray()));
        var publishers = reader.ParseEntries(document, "publishers", new[] { "publisher", "displayName" }, (e, r) =>
            new PublisherEntry(PublisherId.Create(r.GetString(e, "publisher")), r.GetString(e, "displayName")));
        var sources = reader.ParseEntries(document, "sources", new[] { "source", "kind", "displayName", "servesPublishers" }, (e, r) =>
            new SourceEntry(
                SourceId.Create(r.GetString(e, "source")),
                r.GetEnum(e, "kind", new Dictionary<string, SourceKind>
                {
                    ["local"] = SourceKind.Local,
                    ["remote"] = SourceKind.Remote,
                }),
                r.GetString(e, "displayName"),
                r.GetStringList(e, "servesPublishers").Select(PublisherId.Create).ToArray()));
        var packages = reader.ParseEntries(document, "packages", new[] { "package", "publisher", "version", "artifact" }, (e, r) =>
            new PackageEntry(
                PackageId.Create(r.GetString(e, "package")),
                PublisherId.Create(r.GetString(e, "publisher")),
                VersionLiteral.Create(r.GetString(e, "version")),
                ArtifactId.Create(r.GetString(e, "artifact"))));
        var advertisements = reader.ParseEntries(document, "advertisements", new[] { "source", "package", "advertisedVersion" }, (e, r) =>
            new AdvertisementEntry(
                SourceId.Create(r.GetString(e, "source")),
                PackageId.Create(r.GetString(e, "package")),
                VersionLiteral.Create(r.GetString(e, "advertisedVersion"))));
        var definitions = reader.ParseEntries(document, "componentDefinitions", new[] { "definition", "package", "provides", "requires", "generic" }, (e, r) =>
            new ComponentDefinitionEntry(
                DefinitionId.Create(r.GetString(e, "definition")),
                PackageId.Create(r.GetString(e, "package")),
                r.ParseNested(e, "provides", new[] { "contract", "version" }, (p, r2) =>
                    new ProvidedContract(ContractId.Create(r2.GetString(p, "contract")), VersionLiteral.Create(r2.GetString(p, "version")))),
                r.ParseNested(e, "requires", new[] { "contract", "version", "scope", "cardinality" }, (p, r2) =>
                    new RequiredContract(
                        ContractId.Create(r2.GetString(p, "contract")),
                        VersionLiteral.Create(r2.GetString(p, "version")),
                        BindingScopeId.Create(r2.GetString(p, "scope")),
                        Cardinality.Parse(r2.GetString(p, "cardinality")))),
                r.GetBool(e, "generic")));
        var scopes = reader.ParseEntries(document, "bindingScopes", new[] { "scope", "kind" }, (e, r) =>
            new BindingScopeEntry(
                BindingScopeId.Create(r.GetString(e, "scope")),
                r.GetEnum(e, "kind", new Dictionary<string, ScopeKind>
                {
                    ["system-default"] = ScopeKind.SystemDefault,
                    ["application"] = ScopeKind.Application,
                    ["session"] = ScopeKind.Session,
                })));
        var occurrences = reader.ParseEntries(document, "activatedOccurrences", new[] { "occurrence", "definition", "actors" }, (e, r) =>
            new ActivatedOccurrenceEntry(
                OccurrenceId.Create(r.GetString(e, "occurrence")),
                DefinitionId.Create(r.GetString(e, "definition")),
                r.GetStringList(e, "actors").Select(ActorId.Create).ToArray()));
        var bindings = reader.ParseEntries(document, "occupiedBindings", new[] { "binding", "scope", "contract", "occupantDefinition", "occupantOccurrence" }, (e, r) =>
            new OccupiedBindingEntry(
                BindingId.Create(r.GetString(e, "binding")),
                BindingScopeId.Create(r.GetString(e, "scope")),
                ContractId.Create(r.GetString(e, "contract")),
                DefinitionId.Create(r.GetString(e, "occupantDefinition")),
                OccurrenceId.Create(r.GetString(e, "occupantOccurrence"))));
        var preferences = reader.ParseEntries(document, "preferences", new[] { "preference", "declaredBy", "contract", "preferredDefinition" }, (e, r) =>
            new PreferenceEntry(
                PreferenceId.Create(r.GetString(e, "preference")),
                DefinitionId.Create(r.GetString(e, "declaredBy")),
                ContractId.Create(r.GetString(e, "contract")),
                DefinitionId.Create(r.GetString(e, "preferredDefinition"))));
        var artifacts = reader.ParseEntries(document, "artifacts", new[] { "artifact", "content", "sha256" }, (e, r) =>
            new ArtifactEntry(ArtifactId.Create(r.GetString(e, "artifact")), r.GetString(e, "content"), r.GetString(e, "sha256")));
        var evidence = reader.ParseEntries(document, "evidence", new[] { "evidence", "subjectArtifact", "kind", "issuer", "verdict", "detail" }, (e, r) =>
            new EvidenceEntry(
                EvidenceId.Create(r.GetString(e, "evidence")),
                ArtifactId.Create(r.GetString(e, "subjectArtifact")),
                r.GetEnum(e, "kind", new Dictionary<string, EvidenceKind>
                {
                    ["integrity"] = EvidenceKind.Integrity,
                    ["origin"] = EvidenceKind.Origin,
                    ["signature"] = EvidenceKind.Signature,
                    ["review"] = EvidenceKind.Review,
                }),
                IssuerId.Create(r.GetString(e, "issuer")),
                r.GetEnum(e, "verdict", new Dictionary<string, EvidenceVerdict>
                {
                    ["accepted"] = EvidenceVerdict.Accepted,
                    ["rejected"] = EvidenceVerdict.Rejected,
                }),
                r.GetString(e, "detail")));
        var storefront = reader.ParseEntries(document, "storefront", new[]
        {
            "source", "package", "displayName", "description", "imagery", "categories", "version",
            "compatibility", "evidenceStatus", "lifecycleState", "dependencySummary", "alternatives",
        }, (e, r) =>
            new StorefrontEntry(
                SourceId.Create(r.GetString(e, "source")),
                PackageId.Create(r.GetString(e, "package")),
                r.GetString(e, "displayName"),
                r.GetString(e, "description"),
                r.GetString(e, "imagery"),
                r.GetStringList(e, "categories"),
                VersionLiteral.Create(r.GetString(e, "version")),
                r.GetString(e, "compatibility"),
                r.GetString(e, "evidenceStatus"),
                r.GetString(e, "lifecycleState"),
                r.GetStringList(e, "dependencySummary"),
                r.GetStringList(e, "alternatives").Select(PackageId.Create).ToArray()));

        CatalogExpectations? expectations = null;
        if (reader.TryGetSection(document, "expectations", out var expectationsElement))
        {
            reader.CheckProperties(expectationsElement, "expectations", new[]
            {
                "duplicateComponentIdentityAcrossSources", "mirroredPublishers", "multiPublisherSources",
                "contractsWithSeveralDefinitions", "definitionsWithSeveralOccurrences", "occupiedBindings",
                "systemDefaultScopes", "explicitPreferences", "genericCandidates",
                "conflictingVersionClaims", "missingArtifacts", "contradictoryEvidence",
            });
            expectations = reader.Attempt("expectations", () => new CatalogExpectations(
                reader.GetStringList(expectationsElement, "duplicateComponentIdentityAcrossSources").Select(PackageId.Create).ToArray(),
                reader.GetStringList(expectationsElement, "mirroredPublishers").Select(PublisherId.Create).ToArray(),
                reader.GetStringList(expectationsElement, "multiPublisherSources").Select(SourceId.Create).ToArray(),
                reader.GetStringList(expectationsElement, "contractsWithSeveralDefinitions").Select(ContractId.Create).ToArray(),
                reader.GetStringList(expectationsElement, "definitionsWithSeveralOccurrences").Select(DefinitionId.Create).ToArray(),
                reader.GetStringList(expectationsElement, "occupiedBindings").Select(BindingId.Create).ToArray(),
                reader.GetStringList(expectationsElement, "systemDefaultScopes").Select(BindingScopeId.Create).ToArray(),
                reader.GetStringList(expectationsElement, "explicitPreferences").Select(PreferenceId.Create).ToArray(),
                reader.GetStringList(expectationsElement, "genericCandidates").Select(DefinitionId.Create).ToArray(),
                reader.GetStringList(expectationsElement, "conflictingVersionClaims").Select(PackageId.Create).ToArray(),
                reader.GetStringList(expectationsElement, "missingArtifacts").Select(ArtifactId.Create).ToArray(),
                reader.GetNestedStringLists(expectationsElement, "contradictoryEvidence")
                    .Select(pair => (IReadOnlyList<EvidenceId>)pair.Select(EvidenceId.Create).ToArray())
                    .ToArray()));
        }

        if (reader.HasFailures || expectations is null)
        {
            throw reader.Rejection();
        }

        ValidateCatalog(reader,
            new CatalogFixture(description ?? string.Empty, contracts, publishers, sources, packages,
                advertisements, definitions, scopes, occurrences, bindings, preferences, artifacts,
                evidence, storefront, expectations),
            out var fixture);

        if (reader.HasFailures)
        {
            throw reader.Rejection();
        }

        return fixture;
    }

    public static MiceTopologyFixture LoadMiceTopology(string json)
    {
        var reader = new Reader();
        var root = reader.ParseRoot(json, "cm0-mice-topology", new[]
        {
            "schemaVersion", "fixture", "description", "contracts", "observers", "topologyNodes",
            "functions", "claims", "expectations",
        });
        if (root is null)
        {
            throw reader.Rejection();
        }

        var document = root.Value;
        var description = reader.GetString(document, "description");
        var contracts = reader.ParseEntries(document, "contracts", new[] { "contract", "versions" }, (e, r) =>
            new ContractEntry(
                ContractId.Create(r.GetString(e, "contract")),
                r.GetStringList(e, "versions").Select(VersionLiteral.Create).ToArray()));
        var observers = reader.ParseEntries(document, "observers", new[] { "observer" }, (e, r) =>
            new ObserverEntry(ObserverId.Create(r.GetString(e, "observer"))));
        var nodes = reader.ParseEntries(document, "topologyNodes", new[] { "node", "observer", "kind" }, (e, r) =>
            new TopologyNodeEntry(
                TopologyNodeId.Create(r.GetString(e, "node")),
                ObserverId.Create(r.GetString(e, "observer")),
                r.GetEnum(e, "kind", new Dictionary<string, TopologyNodeKind>
                {
                    ["host"] = TopologyNodeKind.Host,
                    ["attachment"] = TopologyNodeKind.Attachment,
                })));
        var functions = reader.ParseEntries(document, "functions", new[] { "function", "contract", "node", "actor" }, (e, r) =>
            new FunctionEntry(
                FunctionId.Create(r.GetString(e, "function")),
                ContractId.Create(r.GetString(e, "contract")),
                TopologyNodeId.Create(r.GetString(e, "node")),
                ActorId.Create(r.GetString(e, "actor"))));
        var claims = reader.ParseEntries(document, "claims", new[] { "claim", "assertedBy", "relation", "from", "to" }, (e, r) =>
            new TopologyClaimEntry(
                ClaimId.Create(r.GetString(e, "claim")),
                ObserverId.Create(r.GetString(e, "assertedBy")),
                r.GetEnum(e, "relation", new Dictionary<string, TopologyRelation>
                {
                    ["PartOf"] = TopologyRelation.PartOf,
                    ["AttachedThrough"] = TopologyRelation.AttachedThrough,
                    ["HostedBy"] = TopologyRelation.HostedBy,
                    ["SamePhysicalAssembly"] = TopologyRelation.SamePhysicalAssembly,
                    ["SharesPowerDomain"] = TopologyRelation.SharesPowerDomain,
                    ["SharesFailureDomain"] = TopologyRelation.SharesFailureDomain,
                }),
                TopologyNodeId.Create(r.GetString(e, "from")),
                TopologyNodeId.Create(r.GetString(e, "to"))));

        MiceExpectations? expectations = null;
        if (reader.TryGetSection(document, "expectations", out var expectationsElement))
        {
            reader.CheckProperties(expectationsElement, "expectations", new[]
            {
                "distinctMouseNodes", "functionsPerMouseNode", "attributableClaims",
                "contradictoryClaims", "maliciousClaims",
            });
            expectations = reader.Attempt("expectations", () => new MiceExpectations(
                reader.GetStringList(expectationsElement, "distinctMouseNodes").Select(TopologyNodeId.Create).ToArray(),
                reader.GetInt(expectationsElement, "functionsPerMouseNode"),
                reader.GetStringList(expectationsElement, "attributableClaims").Select(ClaimId.Create).ToArray(),
                reader.GetNestedStringLists(expectationsElement, "contradictoryClaims")
                    .Select(pair => (IReadOnlyList<ClaimId>)pair.Select(ClaimId.Create).ToArray())
                    .ToArray(),
                reader.GetStringList(expectationsElement, "maliciousClaims").Select(ClaimId.Create).ToArray()));
        }

        if (reader.HasFailures || expectations is null)
        {
            throw reader.Rejection();
        }

        var fixture = new MiceTopologyFixture(description ?? string.Empty, contracts, observers, nodes, functions, claims, expectations);
        ValidateMiceTopology(reader, fixture);
        if (reader.HasFailures)
        {
            throw reader.Rejection();
        }

        return fixture;
    }

    private static void ValidateCatalog(Reader reader, CatalogFixture candidate, out CatalogFixture fixture)
    {
        fixture = candidate;
        reader.RequireDistinct("contracts", candidate.Contracts.Select(c => c.Contract.Value));
        reader.RequireDistinct("publishers", candidate.Publishers.Select(p => p.Publisher.Value));
        reader.RequireDistinct("sources", candidate.Sources.Select(s => s.Source.Value));
        reader.RequireDistinct("packages", candidate.Packages.Select(p => p.Package.Value));
        reader.RequireDistinct("componentDefinitions", candidate.ComponentDefinitions.Select(d => d.Definition.Value));
        reader.RequireDistinct("bindingScopes", candidate.BindingScopes.Select(s => s.Scope.Value));
        reader.RequireDistinct("activatedOccurrences", candidate.ActivatedOccurrences.Select(o => o.Occurrence.Value));
        reader.RequireDistinct("occupiedBindings", candidate.OccupiedBindings.Select(b => b.Binding.Value));
        reader.RequireDistinct("preferences", candidate.Preferences.Select(p => p.Preference.Value));
        reader.RequireDistinct("artifacts", candidate.Artifacts.Select(a => a.Artifact.Value));
        reader.RequireDistinct("evidence", candidate.Evidence.Select(e => e.Evidence.Value));
        reader.RequireDistinct("advertisements", candidate.Advertisements.Select(a => a.Source.Value + "|" + a.Package.Value));
        reader.RequireDistinct("storefront", candidate.Storefront.Select(s => s.Source.Value + "|" + s.Package.Value));
        reader.RequireDistinct("actors", candidate.ActivatedOccurrences.SelectMany(o => o.Actors).Select(a => a.Value));

        var contractVersions = candidate.Contracts.ToDictionary(c => c.Contract, c => c.Versions.Select(v => v.Value).ToHashSet(StringComparer.Ordinal));
        var publisherIds = candidate.Publishers.Select(p => p.Publisher).ToHashSet();
        var sourceIds = candidate.Sources.Select(s => s.Source).ToHashSet();
        var packagesById = candidate.Packages.ToDictionary(p => p.Package);
        var definitionsById = candidate.ComponentDefinitions.ToDictionary(d => d.Definition);
        var scopeIds = candidate.BindingScopes.Select(s => s.Scope).ToHashSet();
        var occurrencesById = candidate.ActivatedOccurrences.ToDictionary(o => o.Occurrence);
        var artifactIds = candidate.Artifacts.Select(a => a.Artifact).ToHashSet();

        foreach (var source in candidate.Sources)
        {
            foreach (var publisher in source.ServesPublishers.Where(p => !publisherIds.Contains(p)))
            {
                reader.Fail($"sources: '{source.Source}' serves unknown publisher '{publisher}'.");
            }
        }

        foreach (var package in candidate.Packages)
        {
            if (!publisherIds.Contains(package.Publisher))
            {
                reader.Fail($"packages: '{package.Package}' names unknown publisher '{package.Publisher}'.");
            }
        }

        foreach (var advertisement in candidate.Advertisements)
        {
            if (!sourceIds.Contains(advertisement.Source))
            {
                reader.Fail($"advertisements: unknown source '{advertisement.Source}'.");
            }

            if (!packagesById.ContainsKey(advertisement.Package))
            {
                reader.Fail($"advertisements: unknown package '{advertisement.Package}'.");
            }
        }

        foreach (var definition in candidate.ComponentDefinitions)
        {
            if (!packagesById.ContainsKey(definition.Package))
            {
                reader.Fail($"componentDefinitions: '{definition.Definition}' names unknown package '{definition.Package}'.");
            }

            foreach (var provided in definition.Provides)
            {
                if (!contractVersions.TryGetValue(provided.Contract, out var versions))
                {
                    reader.Fail($"componentDefinitions: '{definition.Definition}' provides unknown contract '{provided.Contract}'.");
                }
                else if (!versions.Contains(provided.Version.Value))
                {
                    reader.Fail($"componentDefinitions: '{definition.Definition}' provides undeclared version '{provided.Version}' of '{provided.Contract}'.");
                }
            }

            foreach (var required in definition.Requires)
            {
                if (!contractVersions.ContainsKey(required.Contract))
                {
                    reader.Fail($"componentDefinitions: '{definition.Definition}' requires unknown contract '{required.Contract}'.");
                }

                if (!scopeIds.Contains(required.Scope))
                {
                    reader.Fail($"componentDefinitions: '{definition.Definition}' requires unknown scope '{required.Scope}'.");
                }
            }
        }

        foreach (var occurrence in candidate.ActivatedOccurrences)
        {
            if (!definitionsById.ContainsKey(occurrence.Definition))
            {
                reader.Fail($"activatedOccurrences: '{occurrence.Occurrence}' names unknown definition '{occurrence.Definition}'.");
            }
        }

        foreach (var binding in candidate.OccupiedBindings)
        {
            if (!scopeIds.Contains(binding.Scope))
            {
                reader.Fail($"occupiedBindings: '{binding.Binding}' names unknown scope '{binding.Scope}'.");
            }

            if (!contractVersions.ContainsKey(binding.Contract))
            {
                reader.Fail($"occupiedBindings: '{binding.Binding}' names unknown contract '{binding.Contract}'.");
            }

            if (!definitionsById.TryGetValue(binding.OccupantDefinition, out var occupant))
            {
                reader.Fail($"occupiedBindings: '{binding.Binding}' names unknown definition '{binding.OccupantDefinition}'.");
            }
            else if (occupant.Provides.All(p => p.Contract != binding.Contract))
            {
                reader.Fail($"occupiedBindings: '{binding.Binding}' occupant '{binding.OccupantDefinition}' does not provide '{binding.Contract}'.");
            }

            if (!occurrencesById.TryGetValue(binding.OccupantOccurrence, out var occurrence))
            {
                reader.Fail($"occupiedBindings: '{binding.Binding}' names unknown occurrence '{binding.OccupantOccurrence}'.");
            }
            else if (occurrence.Definition != binding.OccupantDefinition)
            {
                reader.Fail($"occupiedBindings: '{binding.Binding}' occurrence '{binding.OccupantOccurrence}' does not realise '{binding.OccupantDefinition}'.");
            }
        }

        foreach (var preference in candidate.Preferences)
        {
            if (!definitionsById.TryGetValue(preference.DeclaredBy, out var declaring))
            {
                reader.Fail($"preferences: '{preference.Preference}' declared by unknown definition '{preference.DeclaredBy}'.");
            }
            else if (declaring.Requires.All(r => r.Contract != preference.Contract))
            {
                reader.Fail($"preferences: '{preference.Preference}' declarer '{preference.DeclaredBy}' has no requirement on '{preference.Contract}'.");
            }

            if (!definitionsById.TryGetValue(preference.PreferredDefinition, out var preferred))
            {
                reader.Fail($"preferences: '{preference.Preference}' prefers unknown definition '{preference.PreferredDefinition}'.");
            }
            else if (preferred.Provides.All(p => p.Contract != preference.Contract))
            {
                reader.Fail($"preferences: '{preference.Preference}' preferred definition does not provide '{preference.Contract}'.");
            }
        }

        var declaredMissing = candidate.Expectations.MissingArtifacts.ToHashSet();
        foreach (var package in candidate.Packages)
        {
            if (!artifactIds.Contains(package.Artifact) && !declaredMissing.Contains(package.Artifact))
            {
                reader.Fail($"packages: '{package.Package}' references missing artifact '{package.Artifact}' that expectations do not declare.");
            }
        }

        foreach (var evidence in candidate.Evidence)
        {
            if (!artifactIds.Contains(evidence.SubjectArtifact))
            {
                reader.Fail($"evidence: '{evidence.Evidence}' names unknown artifact '{evidence.SubjectArtifact}'.");
            }
        }

        var advertisedPairs = candidate.Advertisements.Select(a => (a.Source, a.Package)).ToHashSet();
        foreach (var entry in candidate.Storefront)
        {
            if (!advertisedPairs.Contains((entry.Source, entry.Package)))
            {
                reader.Fail($"storefront: '{entry.Source}' does not advertise '{entry.Package}'.");
            }

            foreach (var alternative in entry.Alternatives.Where(a => !packagesById.ContainsKey(a)))
            {
                reader.Fail($"storefront: alternative '{alternative}' is not a known package.");
            }
        }

        foreach (var artifact in candidate.Artifacts)
        {
            var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(artifact.Content)));
            if (!string.Equals(digest, artifact.Sha256, StringComparison.Ordinal))
            {
                reader.Fail($"artifacts: '{artifact.Artifact}' digest mismatch; recorded {artifact.Sha256} but content hashes to {digest}.");
            }
        }

        var computedDuplicates = candidate.Advertisements
            .GroupBy(a => a.Package)
            .Where(g => g.Select(a => a.Source).Distinct().Count() > 1)
            .Select(g => g.Key.Value);
        reader.RequireExpectation("duplicateComponentIdentityAcrossSources", computedDuplicates,
            candidate.Expectations.DuplicateComponentIdentityAcrossSources.Select(p => p.Value));

        var computedMirrored = candidate.Publishers
            .Where(p => candidate.Sources.Count(s => s.ServesPublishers.Contains(p.Publisher)) > 1)
            .Select(p => p.Publisher.Value);
        reader.RequireExpectation("mirroredPublishers", computedMirrored,
            candidate.Expectations.MirroredPublishers.Select(p => p.Value));

        reader.RequireExpectation("multiPublisherSources",
            candidate.Sources.Where(s => s.ServesPublishers.Count > 1).Select(s => s.Source.Value),
            candidate.Expectations.MultiPublisherSources.Select(s => s.Value));

        var computedMultiDefinition = candidate.ComponentDefinitions
            .SelectMany(d => d.Provides.Select(p => p.Contract))
            .GroupBy(c => c)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.Value);
        reader.RequireExpectation("contractsWithSeveralDefinitions", computedMultiDefinition,
            candidate.Expectations.ContractsWithSeveralDefinitions.Select(c => c.Value));

        var computedMultiOccurrence = candidate.ActivatedOccurrences
            .GroupBy(o => o.Definition)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.Value);
        reader.RequireExpectation("definitionsWithSeveralOccurrences", computedMultiOccurrence,
            candidate.Expectations.DefinitionsWithSeveralOccurrences.Select(d => d.Value));

        reader.RequireExpectation("occupiedBindings",
            candidate.OccupiedBindings.Select(b => b.Binding.Value),
            candidate.Expectations.OccupiedBindings.Select(b => b.Value));

        reader.RequireExpectation("systemDefaultScopes",
            candidate.BindingScopes.Where(s => s.Kind == ScopeKind.SystemDefault).Select(s => s.Scope.Value),
            candidate.Expectations.SystemDefaultScopes.Select(s => s.Value));

        reader.RequireExpectation("explicitPreferences",
            candidate.Preferences.Select(p => p.Preference.Value),
            candidate.Expectations.ExplicitPreferences.Select(p => p.Value));

        reader.RequireExpectation("genericCandidates",
            candidate.ComponentDefinitions.Where(d => d.Generic).Select(d => d.Definition.Value),
            candidate.Expectations.GenericCandidates.Select(d => d.Value));

        var computedConflicting = candidate.Advertisements
            .Where(a => packagesById.TryGetValue(a.Package, out var package) && package.Version != a.AdvertisedVersion)
            .Select(a => a.Package.Value);
        reader.RequireExpectation("conflictingVersionClaims", computedConflicting,
            candidate.Expectations.ConflictingVersionClaims.Select(p => p.Value));

        var computedMissing = candidate.Packages
            .Where(p => !artifactIds.Contains(p.Artifact))
            .Select(p => p.Artifact.Value);
        reader.RequireExpectation("missingArtifacts", computedMissing,
            candidate.Expectations.MissingArtifacts.Select(a => a.Value));

        var computedContradictory = candidate.Evidence
            .GroupBy(e => (e.SubjectArtifact, e.Kind))
            .Where(g => g.Select(e => e.Verdict).Distinct().Count() > 1)
            .Select(g => string.Join("+", g.Select(e => e.Evidence.Value).OrderBy(v => v, StringComparer.Ordinal)));
        var declaredContradictory = candidate.Expectations.ContradictoryEvidence
            .Select(pair => string.Join("+", pair.Select(e => e.Value).OrderBy(v => v, StringComparer.Ordinal)));
        reader.RequireExpectation("contradictoryEvidence", computedContradictory, declaredContradictory);
    }

    private static void ValidateMiceTopology(Reader reader, MiceTopologyFixture fixture)
    {
        reader.RequireDistinct("contracts", fixture.Contracts.Select(c => c.Contract.Value));
        reader.RequireDistinct("observers", fixture.Observers.Select(o => o.Observer.Value));
        reader.RequireDistinct("topologyNodes", fixture.TopologyNodes.Select(n => n.Node.Value));
        reader.RequireDistinct("functions", fixture.Functions.Select(f => f.Function.Value));
        reader.RequireDistinct("claims", fixture.Claims.Select(c => c.Claim.Value));
        reader.RequireDistinct("actors", fixture.Functions.Select(f => f.Actor.Value));

        var observerIds = fixture.Observers.Select(o => o.Observer).ToHashSet();
        var nodeIds = fixture.TopologyNodes.Select(n => n.Node).ToHashSet();
        var contractIds = fixture.Contracts.Select(c => c.Contract).ToHashSet();

        foreach (var node in fixture.TopologyNodes.Where(n => !observerIds.Contains(n.Observer)))
        {
            reader.Fail($"topologyNodes: '{node.Node}' names unknown observer '{node.Observer}'.");
        }

        foreach (var function in fixture.Functions)
        {
            if (!contractIds.Contains(function.Contract))
            {
                reader.Fail($"functions: '{function.Function}' names unknown contract '{function.Contract}'.");
            }

            if (!nodeIds.Contains(function.Node))
            {
                reader.Fail($"functions: '{function.Function}' names unknown node '{function.Node}'.");
            }
        }

        foreach (var claim in fixture.Claims)
        {
            if (!observerIds.Contains(claim.AssertedBy))
            {
                reader.Fail($"claims: '{claim.Claim}' asserted by unknown observer '{claim.AssertedBy}'.");
            }

            if (!nodeIds.Contains(claim.From) || !nodeIds.Contains(claim.To))
            {
                reader.Fail($"claims: '{claim.Claim}' relates unknown nodes.");
            }

            if (claim.From == claim.To)
            {
                reader.Fail($"claims: '{claim.Claim}' relates a node to itself.");
            }
        }

        var functionBearing = fixture.Functions
            .GroupBy(f => f.Node)
            .ToDictionary(g => g.Key, g => g.Count());
        reader.RequireExpectation("distinctMouseNodes",
            functionBearing.Keys.Select(n => n.Value),
            fixture.Expectations.DistinctMouseNodes.Select(n => n.Value));
        foreach (var (node, count) in functionBearing.OrderBy(p => p.Key.Value, StringComparer.Ordinal))
        {
            if (count != fixture.Expectations.FunctionsPerMouseNode)
            {
                reader.Fail($"expectations: node '{node}' bears {count} functions, expected {fixture.Expectations.FunctionsPerMouseNode}.");
            }
        }

        var claimIds = fixture.Claims.Select(c => c.Claim).ToHashSet();
        var attributable = fixture.Expectations.AttributableClaims.ToHashSet();
        var malicious = fixture.Expectations.MaliciousClaims.ToHashSet();
        var pairMembers = fixture.Expectations.ContradictoryClaims.SelectMany(p => p).ToHashSet();

        foreach (var referenced in attributable.Concat(malicious).Concat(pairMembers).Where(c => !claimIds.Contains(c)))
        {
            reader.Fail($"expectations: unknown claim '{referenced}'.");
        }

        foreach (var pair in fixture.Expectations.ContradictoryClaims.Where(p => p.Count != 2))
        {
            reader.Fail("expectations: contradictoryClaims entries must contain exactly two claims.");
        }

        foreach (var overlap in attributable.Intersect(malicious))
        {
            reader.Fail($"expectations: claim '{overlap}' cannot be both attributable and malicious.");
        }

        foreach (var claim in fixture.Claims)
        {
            if (!attributable.Contains(claim.Claim) && !malicious.Contains(claim.Claim) && !pairMembers.Contains(claim.Claim))
            {
                reader.Fail($"expectations: claim '{claim.Claim}' is not classified as attributable, malicious, or contradictory.");
            }
        }
    }

    /// <summary>Failure-accumulating strict JSON reader; every helper records rather than throws.</summary>
    private sealed class Reader
    {
        private readonly List<string> failures = new();
        private JsonDocument? document;

        public bool HasFailures => failures.Count > 0;

        public void Fail(string failure) => failures.Add(failure);

        public FixtureFormatException Rejection()
        {
            var ordered = failures.OrderBy(f => f, StringComparer.Ordinal).ToArray();
            return new FixtureFormatException(ordered.Length > 0 ? ordered : new[] { "unknown fixture failure" });
        }

        public JsonElement? ParseRoot(string json, string expectedFixture, IReadOnlyList<string> allowed)
        {
            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException exception)
            {
                Fail($"fixture is not valid JSON: {exception.Message}");
                return null;
            }

            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                Fail("fixture root must be a JSON object.");
                return null;
            }

            CheckProperties(root, "fixture", allowed);
            if (!root.TryGetProperty("schemaVersion", out var schema) || schema.ValueKind != JsonValueKind.Number || schema.GetInt32() != 1)
            {
                Fail("fixture schemaVersion must be 1.");
                return null;
            }

            if (!root.TryGetProperty("fixture", out var name) || name.GetString() != expectedFixture)
            {
                Fail($"fixture name must be '{expectedFixture}'.");
                return null;
            }

            return root;
        }

        public void CheckProperties(JsonElement element, string path, IReadOnlyList<string> allowed)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (!allowed.Contains(property.Name, StringComparer.Ordinal))
                {
                    Fail($"{path}: unknown property '{property.Name}'.");
                }
            }

            foreach (var required in allowed)
            {
                if (!element.TryGetProperty(required, out _))
                {
                    Fail($"{path}: missing property '{required}'.");
                }
            }
        }

        public bool TryGetSection(JsonElement root, string name, out JsonElement section)
        {
            if (root.TryGetProperty(name, out section) && section.ValueKind == JsonValueKind.Object)
            {
                return true;
            }

            Fail($"{name}: section must be an object.");
            return false;
        }

        public IReadOnlyList<T> ParseEntries<T>(JsonElement root, string section, IReadOnlyList<string> allowed, Func<JsonElement, Reader, T> parse)
            where T : class
        {
            var results = new List<T>();
            if (!root.TryGetProperty(section, out var array) || array.ValueKind != JsonValueKind.Array)
            {
                Fail($"{section}: section must be an array.");
                return results;
            }

            var index = 0;
            foreach (var element in array.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    Fail($"{section}[{index}]: entry must be an object.");
                }
                else
                {
                    CheckProperties(element, $"{section}[{index}]", allowed);
                    var entry = Attempt($"{section}[{index}]", () => parse(element, this));
                    if (entry is not null)
                    {
                        results.Add(entry);
                    }
                }

                index++;
            }

            return results;
        }

        public IReadOnlyList<T> ParseNested<T>(JsonElement parent, string property, IReadOnlyList<string> allowed, Func<JsonElement, Reader, T> parse)
            where T : class
        {
            var results = new List<T>();
            if (!parent.TryGetProperty(property, out var array) || array.ValueKind != JsonValueKind.Array)
            {
                Fail($"{property}: nested list must be an array.");
                return results;
            }

            var index = 0;
            foreach (var element in array.EnumerateArray())
            {
                CheckProperties(element, $"{property}[{index}]", allowed);
                var entry = Attempt($"{property}[{index}]", () => parse(element, this));
                if (entry is not null)
                {
                    results.Add(entry);
                }

                index++;
            }

            return results;
        }

        public T? Attempt<T>(string path, Func<T> parse)
            where T : class
        {
            try
            {
                return parse();
            }
            catch (ArgumentException exception)
            {
                Fail($"{path}: {exception.Message}");
                return null;
            }
        }

        public string GetString(JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }
            }

            throw new ArgumentException($"property '{property}' must be a non-empty string.");
        }

        public int GetInt(JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number)
            {
                return value.GetInt32();
            }

            throw new ArgumentException($"property '{property}' must be an integer.");
        }

        public bool GetBool(JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var value) &&
                value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return value.GetBoolean();
            }

            throw new ArgumentException($"property '{property}' must be a Boolean.");
        }

        public IReadOnlyList<string> GetStringList(JsonElement element, string property)
        {
            if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException($"property '{property}' must be an array of strings.");
            }

            var results = new List<string>();
            foreach (var entry in value.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(entry.GetString()))
                {
                    throw new ArgumentException($"property '{property}' must contain only non-empty strings.");
                }

                results.Add(entry.GetString()!);
            }

            return results;
        }

        public IReadOnlyList<IReadOnlyList<string>> GetNestedStringLists(JsonElement element, string property)
        {
            if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException($"property '{property}' must be an array of string arrays.");
            }

            var results = new List<IReadOnlyList<string>>();
            foreach (var entry in value.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Array)
                {
                    throw new ArgumentException($"property '{property}' must contain only arrays.");
                }

                var inner = new List<string>();
                foreach (var item in entry.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(item.GetString()))
                    {
                        throw new ArgumentException($"property '{property}' must contain only non-empty strings.");
                    }

                    inner.Add(item.GetString()!);
                }

                results.Add(inner);
            }

            return results;
        }

        public TEnum GetEnum<TEnum>(JsonElement element, string property, IReadOnlyDictionary<string, TEnum> tokens)
            where TEnum : struct
        {
            var text = GetString(element, property);
            if (tokens.TryGetValue(text, out var value))
            {
                return value;
            }

            throw new ArgumentException($"property '{property}' has unsupported value '{text}'.");
        }

        public void RequireDistinct(string section, IEnumerable<string> values)
        {
            foreach (var duplicate in values.GroupBy(v => v, StringComparer.Ordinal).Where(g => g.Count() > 1))
            {
                Fail($"{section}: duplicate identifier '{duplicate.Key}'.");
            }
        }

        public void RequireExpectation(string field, IEnumerable<string> computed, IEnumerable<string> declared)
        {
            var computedSorted = computed.Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToArray();
            var declaredSorted = declared.Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToArray();
            if (!computedSorted.SequenceEqual(declaredSorted, StringComparer.Ordinal))
            {
                Fail($"expectations: '{field}' declares [{string.Join(", ", declaredSorted)}] but the data computes [{string.Join(", ", computedSorted)}].");
            }
        }
    }
}
