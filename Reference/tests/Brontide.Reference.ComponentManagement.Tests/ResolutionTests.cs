using System.Text.Json;
using Brontide.Reference.Experimental.ComponentManagement;
using NUnit.Framework;

namespace Brontide.Reference.ComponentManagement.Tests;

[TestFixture]
public sealed class ResolutionTests
{
    private static readonly ContractId Telemetry = ContractId.Create("brontide.fake.telemetry-sink");
    private static readonly VersionLiteral V2 = VersionLiteral.Create("2.0");
    private static readonly BindingScopeId SystemScope = BindingScopeId.Create("scope.system");
    private static readonly DefinitionId App = DefinitionId.Create("def.test.app");
    private static readonly DefinitionId Northwind = DefinitionId.Create("def.northwind.telemetry");
    private static readonly DefinitionId Generic = DefinitionId.Create("def.contoso.generic-telemetry");

    [Test]
    public void Neutral_vector_inventory_is_complete_and_data_only()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "component-management",
            "fixtures",
            "cm2-resolution-vectors.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var ids = root.GetProperty("vectors")
            .EnumerateArray()
            .Select(vector => vector.GetProperty("id").GetString())
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1));
            Assert.That(root.GetProperty("fixture").GetString(), Is.EqualTo("cm2-resolution-vectors"));
            Assert.That(ids, Is.EqualTo(Enumerable.Range(1, 15).Select(index => $"cm2-{index:00}").ToArray()));
            Assert.That(root.GetRawText(), Does.Not.Contain("implementation"));
            Assert.That(root.GetRawText(), Does.Not.Contain("algorithm"));
        });
    }

    [Test]
    public void Compatible_occupied_binding_is_retained_and_preference_remains_visible()
    {
        var request = BaseRequest();

        var outcome = new FakeGenerationResolver().Resolve(request);

        Assert.That(outcome.IsResolved, Is.True);
        var set = outcome.Generation!.ProviderSets.Single();
        Assert.Multiple(() =>
        {
            Assert.That(set.Members, Has.Count.EqualTo(1));
            Assert.That(set.Members[0].Retained, Is.True);
            Assert.That(set.Members[0].Occurrence, Is.EqualTo(OccurrenceId.Create("occ.telemetry-retained")));
            Assert.That(outcome.Proposed!.Preferences.Single().Used, Is.False);
            Assert.That(outcome.Proposed.Preferences.Single().Reason, Is.EqualTo("compatible-occupant-retained"));
            Assert.That(outcome.Effects, Is.EqualTo(Cm2EffectObservation.None));
            Assert.That(outcome.Generation.Effects, Is.EqualTo(Cm2EffectObservation.None));
            Assert.That(outcome.Proposed.RetainedActiveGeneration, Is.EqualTo(GenerationId.Create("gen.active")));
        });
    }

    [Test]
    public void Authorised_replacement_uses_explicit_preference_before_affinity_generic_and_other()
    {
        var request = BaseRequest() with
        {
            AuthorisedReplacements = new[] { BindingId.Create("bind.telemetry") },
            Preferences = new[]
            {
                new PreferenceEntry(
                    PreferenceId.Create("pref.generic"),
                    App,
                    Telemetry,
                    Generic),
            },
        };

        var outcome = new FakeGenerationResolver().Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Generation!.ProviderSets.Single().Members.Single().Definition, Is.EqualTo(Generic));
            Assert.That(outcome.Proposed!.Preferences.Single().Used, Is.True);
            Assert.That(outcome.Proposed.Exclusions.Single().Definition, Is.EqualTo(DefinitionId.Create("def.test.excluded")));
            Assert.That(outcome.Proposed.Exclusions.Single().Domain, Is.EqualTo(CandidatePolicyDomain.Trust));
        });
    }

    [Test]
    public void Optional_capacity_is_empty_unless_an_additional_member_is_preselected()
    {
        var requirement = Requirement("req.providers", new Cardinality(1, 3));
        var request = BaseRequest(requirement) with
        {
            OccupiedBindings = Array.Empty<OccupiedBindingEntry>(),
            Preferences = Array.Empty<PreferenceEntry>(),
        };

        var ordinary = new FakeGenerationResolver().Resolve(request);
        var preselected = new FakeGenerationResolver().Resolve(
            request with
            {
                PreselectedProviders = new[]
                {
                    new ProviderPreselection(requirement.Requirement, Northwind),
                },
            });

        Assert.Multiple(() =>
        {
            Assert.That(ordinary.Generation!.ProviderSets.Single().Members, Has.Count.EqualTo(1));
            Assert.That(ordinary.Generation.ProviderSets.Single().OptionalPositionsUnfilled, Is.EqualTo(2));
            Assert.That(preselected.Generation!.ProviderSets.Single().Members, Has.Count.EqualTo(2));
            Assert.That(preselected.Generation.ProviderSets.Single().OptionalPositionsUnfilled, Is.EqualTo(1));
        });
    }

    [Test]
    public void Recursive_composition_closes_before_activation_parameters_are_bound()
    {
        var child = DefinitionId.Create("def.test.child");
        var leaf = DefinitionId.Create("def.test.leaf");
        var compositionParameter = ParameterId.Create("param.child");
        var activationParameter = ParameterId.Create("param.path");
        var app = Definition(
            App,
            Array.Empty<ResolutionRequirement>(),
            composition: new[]
            {
                new CompositionParameterDeclaration(compositionParameter, new[] { child }, true),
            },
            activation: new[]
            {
                new ActivationParameterDeclaration(activationParameter, true, null),
            });
        var childDefinition = Definition(
            child,
            new[] { Requirement("req.child-leaf", Cardinality.Parse("1..1")) });
        var leafDefinition = Definition(leaf, Array.Empty<ResolutionRequirement>(), provides: new[] { new ProvidedContract(Telemetry, V2) });
        var request = EmptyRequest(app, childDefinition, leafDefinition) with
        {
            CompositionParameters = new[]
            {
                new CompositionParameterSelection(App, compositionParameter, child),
            },
            ActivationParameters = new[]
            {
                new ActivationParameterValue(activationParameter, "fake-resource"),
                new ActivationParameterValue(ParameterId.Create("param.unused"), "ignored"),
            },
            Candidates = new[] { Candidate(leaf, PublisherId.Create("pub.test"), false) },
        };

        var outcome = new FakeGenerationResolver().Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Generation!.Definitions, Is.EqualTo(new[] { App, child, leaf }.OrderBy(item => item.Value)));
            Assert.That(outcome.Generation.Parameters.Single().Value, Is.EqualTo("fake-resource"));
            Assert.That(outcome.Generation.Parameters.Single().Provenance, Is.EqualTo("environment"));
            Assert.That(outcome.Proposed!.UnusedActivationParameters.Single().Parameter, Is.EqualTo(ParameterId.Create("param.unused")));
        });

        var missingStructure = new FakeGenerationResolver().Resolve(request with { Candidates = Array.Empty<ResolutionCandidate>() });
        Assert.Multiple(() =>
        {
            Assert.That(missingStructure.Failure!.Kind, Is.EqualTo(ResolutionFailureKind.MissingDependency));
            Assert.That(missingStructure.Failure.Parameter, Is.Null);
        });
    }

    [Test]
    public void Sharing_requires_requirement_and_provider_isolation_lifecycle_authority_and_scope_agreement()
    {
        var first = Requirement("req.first", Cardinality.Parse("1..1"), allowSharing: true);
        var second = Requirement("req.second", Cardinality.Parse("1..1"), allowSharing: true);
        var app = Definition(App, new[] { first, second });
        var provider = Definition(Generic, Array.Empty<ResolutionRequirement>(), provides: new[] { new ProvidedContract(Telemetry, V2) });
        var request = EmptyRequest(app, provider) with { Candidates = new[] { Candidate(Generic, PublisherId.Create("pub.contoso"), true) } };

        var shared = new FakeGenerationResolver().Resolve(request);
        var separate = new FakeGenerationResolver().Resolve(
            request with
            {
                Candidates = new[]
                {
                    Candidate(Generic, PublisherId.Create("pub.contoso"), true) with
                    {
                        Sharing = new SharingDeclaration(true, false, true),
                    },
                },
            });

        Assert.Multiple(() =>
        {
            Assert.That(
                shared.Generation!.ProviderSets.SelectMany(set => set.Members).Select(member => member.Occurrence).Distinct().Count(),
                Is.EqualTo(1));
            Assert.That(
                separate.Generation!.ProviderSets.SelectMany(set => set.Members).Select(member => member.Occurrence).Distinct().Count(),
                Is.EqualTo(2));
            Assert.That(
                separate.Generation.ProviderSets.SelectMany(set => set.Members).Select(member => member.AttachmentNode).Distinct().Count(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void Mirrored_sources_remain_alternatives_but_fill_one_definition_position()
    {
        var requirement = Requirement("req.mirror", Cardinality.Parse("1..1"));
        var request = EmptyRequest(
            Definition(App, new[] { requirement }),
            Definition(Generic, Array.Empty<ResolutionRequirement>(), provides: new[] { new ProvidedContract(Telemetry, V2) })) with
        {
            Candidates = new[]
            {
                Candidate(Generic, PublisherId.Create("pub.contoso"), true),
                Candidate(Generic, PublisherId.Create("pub.contoso"), true) with
                {
                    Source = SourceId.Create("src.mirror"),
                },
            },
        };

        var outcome = new FakeGenerationResolver().Resolve(request);

        Assert.Multiple(() =>
        {
            var set = outcome.Generation!.ProviderSets.Single();
            Assert.That(set.Members, Has.Count.EqualTo(1));
            Assert.That(set.Alternatives, Has.Count.EqualTo(2));
            Assert.That(set.Alternatives.Select(item => item.Source), Is.EqualTo(set.Alternatives.Select(item => item.Source).OrderBy(item => item.Value)));
        });
    }

    [Test]
    public void Occupied_binding_without_matching_occurrence_fails_closed()
    {
        var request = BaseRequest() with { ExistingOccurrences = Array.Empty<ActivatedOccurrenceEntry>() };

        var outcome = new FakeGenerationResolver().Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Failure!.Kind, Is.EqualTo(ResolutionFailureKind.ContradictoryIdentity));
            Assert.That(outcome.Failure.Reason, Does.Contain("no matching retained occurrence"));
            Assert.That(outcome.Generation, Is.Null);
        });
    }

    [Test]
    public void Multi_member_logical_endpoint_requires_visible_mediation_and_policy_bearing_mediation_requires_component()
    {
        var requirement = Requirement(
            "req.logical",
            Cardinality.Parse("2..2"),
            exposure: ProviderExposure.Mediated);
        var secondProvider = DefinitionId.Create("def.test.second");
        var app = Definition(App, new[] { requirement });
        var first = Definition(Generic, Array.Empty<ResolutionRequirement>(), provides: new[] { new ProvidedContract(Telemetry, V2) });
        var second = Definition(secondProvider, Array.Empty<ResolutionRequirement>(), provides: new[] { new ProvidedContract(Telemetry, V2) });
        var request = EmptyRequest(app, first, second) with
        {
            Candidates = new[]
            {
                Candidate(Generic, PublisherId.Create("pub.contoso"), true),
                Candidate(secondProvider, PublisherId.Create("pub.other"), false),
            },
        };

        var undeclared = new FakeGenerationResolver().Resolve(request);
        Assert.That(undeclared.Failure!.Kind, Is.EqualTo(ResolutionFailureKind.MediationRequired));

        var hostMediation = new MediationDeclaration(
            MediationId.Create("med.logical"),
            MediationKind.Aggregation,
            MediationRealization.StaticHost,
            null,
            false,
            false,
            true,
            false,
            false,
            false);
        var policyBearing = new FakeGenerationResolver().Resolve(
            ReplaceRequirement(request, requirement with { Mediation = hostMediation }));
        Assert.That(policyBearing.Failure!.Kind, Is.EqualTo(ResolutionFailureKind.MediationRequiresComponent));

        var mediator = DefinitionId.Create("def.test.aggregator");
        var dedicated = hostMediation with
        {
            Realization = MediationRealization.DedicatedComponent,
            Component = mediator,
        };
        var resolved = new FakeGenerationResolver().Resolve(
            ReplaceRequirement(
                request with
                {
                    Definitions = request.Definitions.Append(
                        Definition(mediator, Array.Empty<ResolutionRequirement>())).ToArray(),
                },
                requirement with { Mediation = dedicated }));
        Assert.Multiple(() =>
        {
            Assert.That(resolved.IsResolved, Is.True);
            Assert.That(resolved.Generation!.ProviderSets.Single().Mediation, Is.EqualTo(dedicated));
            Assert.That(resolved.Generation.ProviderSets.Single().BindingPlans.All(plan => !plan.Direct), Is.True);
            Assert.That(resolved.Generation.ProviderSets.Single().Members, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void Port_envelope_is_enforced_without_mutating_parent_and_can_request_wider_generation()
    {
        var region = RegionId.Create("region.parent");
        var port = PortId.Create("port.child");
        var requirement = Requirement(
            "req.child",
            Cardinality.Parse("1..1"),
            region: region,
            port: port,
            runtime: true,
            authority: new[] { "authority.read" },
            topology: new[] { TopologyRelation.AttachedThrough });
        var envelope = new PortEnvelope(
            region,
            port,
            PortLifecycleMode.RuntimeOpen,
            new[] { new ProvidedContract(Telemetry, V2) },
            Cardinality.Parse("0..1"),
            new[] { "import.clock" },
            new[] { "export.telemetry" },
            new[] { "authority.read" },
            new[] { TopologyRelation.AttachedThrough },
            "contain",
            "child",
            false);
        var request = EmptyRequest(
            Definition(App, new[] { requirement }),
            Definition(Generic, Array.Empty<ResolutionRequirement>(), provides: new[] { new ProvidedContract(Telemetry, V2) })) with
        {
            Candidates = new[] { Candidate(Generic, PublisherId.Create("pub.contoso"), true) },
            Ports = new[] { envelope },
        };

        Assert.That(new FakeGenerationResolver().Resolve(request).IsResolved, Is.True);

        var excess = ReplaceRequirement(
            request,
            requirement with { RequestedAuthority = new[] { "authority.write" } });
        var refused = new FakeGenerationResolver().Resolve(excess);
        var wider = new FakeGenerationResolver().Resolve(
            excess with { Ports = new[] { envelope with { AllowWiderGenerationProposal = true } } });
        Assert.Multiple(() =>
        {
            Assert.That(refused.Failure!.Kind, Is.EqualTo(ResolutionFailureKind.PortEnvelopeExceeded));
            Assert.That(wider.WiderGeneration!.Region, Is.EqualTo(region));
            Assert.That(wider.WiderGeneration.Port, Is.EqualTo(port));
            Assert.That(wider.Generation, Is.Null);
            Assert.That(wider.Effects.ActiveGenerationMutated, Is.False);
        });

        var resolved = new FakeGenerationResolver().Resolve(request);
        Assert.Multiple(() =>
        {
            Assert.That(resolved.Generation!.Ports.Single().Region, Is.EqualTo(envelope.Region));
            Assert.That(resolved.Generation.Ports.Single().Port, Is.EqualTo(envelope.Port));
            Assert.That(resolved.Generation.Ports.Single().Lifecycle, Is.EqualTo(envelope.Lifecycle));
            Assert.That(resolved.Generation.ProviderSets.Single().ContainingRegion, Is.EqualTo(region));
            Assert.That(resolved.Generation.ProviderSets.Single().ContainingPort, Is.EqualTo(port));
        });
    }

    [Test]
    public void Topology_claims_are_attributable_and_keep_relations_distinct()
    {
        var host = TopologyNodeId.Create("node.host");
        var attachment = TopologyNodeId.Create("node.mouse");
        var claims = new[]
        {
            new TopologyPolicyInput(
                ClaimId.Create("claim.accept"),
                ObserverId.Create("observer.local"),
                TopologyRelation.AttachedThrough,
                attachment,
                host,
                TopologyPolicyDisposition.Accepted,
                null,
                "local attachment observation"),
            new TopologyPolicyInput(
                ClaimId.Create("claim.refine"),
                ObserverId.Create("observer.device"),
                TopologyRelation.SamePhysicalAssembly,
                attachment,
                host,
                TopologyPolicyDisposition.Refined,
                TopologyRelation.HostedBy,
                "only hosting was locally observed"),
            new TopologyPolicyInput(
                ClaimId.Create("claim.reject"),
                ObserverId.Create("observer.device"),
                TopologyRelation.SharesPowerDomain,
                attachment,
                host,
                TopologyPolicyDisposition.Rejected,
                null,
                "unsupported claim"),
        };
        var request = EmptyRequest(Definition(App, Array.Empty<ResolutionRequirement>())) with { TopologyClaims = claims };

        var outcome = new FakeGenerationResolver().Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Generation!.Topology.Select(item => item.Disposition), Is.EqualTo(new[]
            {
                TopologyPolicyDisposition.Accepted,
                TopologyPolicyDisposition.Refined,
                TopologyPolicyDisposition.Rejected,
            }));
            Assert.That(outcome.Generation.Topology[1].EffectiveRelation, Is.EqualTo(TopologyRelation.HostedBy));
            Assert.That(outcome.Generation.Topology[2].EffectiveRelation, Is.Null);
        });
    }

    [Test]
    public void Resolver_is_deterministic_under_definition_candidate_and_root_permutations()
    {
        var requirement = Requirement("req.providers", Cardinality.Parse("2..2"));
        var second = DefinitionId.Create("def.test.second");
        var request = EmptyRequest(
            Definition(App, new[] { requirement }),
            Definition(Generic, Array.Empty<ResolutionRequirement>(), provides: new[] { new ProvidedContract(Telemetry, V2) }),
            Definition(second, Array.Empty<ResolutionRequirement>(), provides: new[] { new ProvidedContract(Telemetry, V2) })) with
        {
            Candidates = new[]
            {
                Candidate(Generic, PublisherId.Create("pub.contoso"), true),
                Candidate(second, PublisherId.Create("pub.other"), false),
            },
        };
        var baseline = Canonical(new FakeGenerationResolver().Resolve(request));

        foreach (var definitions in Permutations(request.Definitions))
        {
            foreach (var candidates in Permutations(request.Candidates))
            {
                var outcome = new FakeGenerationResolver().Resolve(
                    request with { Definitions = definitions, Candidates = candidates });
                Assert.That(Canonical(outcome), Is.EqualTo(baseline));
            }
        }
    }

    [TestCase(ResolutionFailureKind.UnsupportedConstraint)]
    [TestCase(ResolutionFailureKind.UnboundedRequiredCardinality)]
    [TestCase(ResolutionFailureKind.ActivationParameterUnavailable)]
    [TestCase(ResolutionFailureKind.CycleRequiresCm3)]
    public void Declared_failure_categories_are_structured_and_effect_free(ResolutionFailureKind kind)
    {
        ResolutionRequest request = kind switch
        {
            ResolutionFailureKind.UnsupportedConstraint =>
                EmptyRequest(Definition(App, new[]
                {
                    Requirement("req.failure", Cardinality.Parse("1..1")) with
                    {
                        Constraints = new[] { new DefinitionConstraint("unknown", "x") },
                    },
                })),
            ResolutionFailureKind.UnboundedRequiredCardinality =>
                EmptyRequest(Definition(App, new[] { Requirement("req.failure", Cardinality.Parse("1..*")) })),
            ResolutionFailureKind.ActivationParameterUnavailable =>
                EmptyRequest(Definition(
                    App,
                    Array.Empty<ResolutionRequirement>(),
                    activation: new[] { new ActivationParameterDeclaration(ParameterId.Create("param.missing"), true, null) })),
            ResolutionFailureKind.CycleRequiresCm3 => CycleRequest(),
            _ => throw new AssertionException($"Unhandled test kind {kind}."),
        };

        var outcome = new FakeGenerationResolver().Resolve(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Failure!.Kind, Is.EqualTo(kind));
            Assert.That(outcome.Generation, Is.Null);
            Assert.That(outcome.Proposed, Is.Null);
            Assert.That(outcome.Effects, Is.EqualTo(Cm2EffectObservation.None));
        });
    }

    private static ResolutionRequest BaseRequest(ResolutionRequirement? requirement = null)
    {
        requirement ??= Requirement("req.telemetry", Cardinality.Parse("1..1"));
        var app = Definition(App, new[] { requirement });
        var northwind = Definition(Northwind, Array.Empty<ResolutionRequirement>(), provides: new[] { new ProvidedContract(Telemetry, V2) }, publisher: PublisherId.Create("pub.northwind"));
        var generic = Definition(Generic, Array.Empty<ResolutionRequirement>(), provides: new[] { new ProvidedContract(Telemetry, V2) }, publisher: PublisherId.Create("pub.contoso"));
        var excluded = Definition(
            DefinitionId.Create("def.test.excluded"),
            Array.Empty<ResolutionRequirement>(),
            provides: new[] { new ProvidedContract(Telemetry, V2) },
            publisher: PublisherId.Create("pub.test"));
        return EmptyRequest(app, northwind, generic, excluded) with
        {
            ActiveGeneration = GenerationId.Create("gen.active"),
            Candidates = new[]
            {
                Candidate(Northwind, PublisherId.Create("pub.northwind"), false),
                Candidate(Generic, PublisherId.Create("pub.contoso"), true),
                Candidate(DefinitionId.Create("def.test.excluded"), PublisherId.Create("pub.test"), false) with
                {
                    Policy = new[]
                    {
                        new CandidatePolicyObservation(CandidatePolicyDomain.Trust, false, "fake trust policy excludes candidate"),
                    },
                },
            },
            ExistingOccurrences = new[]
            {
                new ActivatedOccurrenceEntry(
                    OccurrenceId.Create("occ.telemetry-retained"),
                    Northwind,
                    new[] { ActorId.Create("actor.telemetry-retained") }),
            },
            OccupiedBindings = new[]
            {
                new OccupiedBindingEntry(
                    BindingId.Create("bind.telemetry"),
                    SystemScope,
                    Telemetry,
                    Northwind,
                    OccurrenceId.Create("occ.telemetry-retained")),
            },
            Preferences = new[]
            {
                new PreferenceEntry(PreferenceId.Create("pref.generic"), App, Telemetry, Generic),
            },
        };
    }

    private static ResolutionRequest EmptyRequest(params ResolutionDefinition[] definitions) =>
        new(
            ResolutionRequestId.Create("resolution.test"),
            GenerationId.Create("gen.proposed"),
            null,
            RestartScopeId.Create("restart.test"),
            new[] { App },
            definitions,
            Array.Empty<ResolutionCandidate>(),
            Array.Empty<ActivatedOccurrenceEntry>(),
            Array.Empty<OccupiedBindingEntry>(),
            Array.Empty<PreferenceEntry>(),
            Array.Empty<BindingId>(),
            Array.Empty<CompositionParameterSelection>(),
            Array.Empty<ActivationParameterValue>(),
            Array.Empty<ProviderPreselection>(),
            Array.Empty<PortEnvelope>(),
            Array.Empty<TopologyPolicyInput>());

    private static ResolutionDefinition Definition(
        DefinitionId definition,
        IReadOnlyList<ResolutionRequirement> requirements,
        IReadOnlyList<CompositionParameterDeclaration>? composition = null,
        IReadOnlyList<ActivationParameterDeclaration>? activation = null,
        IReadOnlyList<ProvidedContract>? provides = null,
        PublisherId? publisher = null) =>
        new(
            definition,
            publisher ?? PublisherId.Create("pub.contoso"),
            provides ?? Array.Empty<ProvidedContract>(),
            requirements,
            composition ?? Array.Empty<CompositionParameterDeclaration>(),
            activation ?? Array.Empty<ActivationParameterDeclaration>(),
            Array.Empty<string>());

    private static ResolutionRequirement Requirement(
        string id,
        Cardinality cardinality,
        bool allowSharing = false,
        ProviderExposure exposure = ProviderExposure.Distinct,
        RegionId? region = null,
        PortId? port = null,
        bool runtime = false,
        IReadOnlyList<string>? authority = null,
        IReadOnlyList<TopologyRelation>? topology = null) =>
        new(
            RequirementId.Create(id),
            Telemetry,
            V2,
            SystemScope,
            cardinality,
            allowSharing,
            exposure,
            null,
            Array.Empty<DefinitionConstraint>(),
            region,
            port,
            runtime,
            authority ?? Array.Empty<string>(),
            topology ?? Array.Empty<TopologyRelation>());

    private static ResolutionCandidate Candidate(DefinitionId definition, PublisherId publisher, bool generic) =>
        new(
            definition,
            SourceId.Create($"src.{definition.Value}"),
            publisher,
            PackageId.Create($"pkg.{definition.Value}"),
            new[] { new ProvidedContract(Telemetry, V2) },
            generic,
            new SharingDeclaration(true, true, true),
            new[]
            {
                new CandidatePolicyObservation(CandidatePolicyDomain.Trust, true, "accepted by fake trust policy"),
                new CandidatePolicyObservation(CandidatePolicyDomain.Platform, true, "fake platform matches"),
            },
            new[] { EvidenceId.Create($"ev.{definition.Value}") },
            new[] { "authority.read" },
            $"failure.{definition.Value}",
            TopologyNodeId.Create($"node.{definition.Value}"));

    private static ResolutionRequest ReplaceRequirement(ResolutionRequest request, ResolutionRequirement replacement)
    {
        var definitions = request.Definitions
            .Select(definition => definition.Definition == App
                ? definition with { Requirements = new[] { replacement } }
                : definition)
            .ToArray();
        return request with { Definitions = definitions };
    }

    private static ResolutionRequest CycleRequest()
    {
        var second = DefinitionId.Create("def.test.second");
        var firstRequirement = Requirement("req.first-second", Cardinality.Parse("1..1"));
        var secondRequirement = Requirement("req.second-first", Cardinality.Parse("1..1"));
        var app = Definition(App, new[] { firstRequirement }, provides: new[] { new ProvidedContract(Telemetry, V2) });
        var other = Definition(second, new[] { secondRequirement }, provides: new[] { new ProvidedContract(Telemetry, V2) });
        return EmptyRequest(app, other) with
        {
            Candidates = new[]
            {
                Candidate(App, PublisherId.Create("pub.contoso"), false),
                Candidate(second, PublisherId.Create("pub.other"), false),
            },
        };
    }

    private static string Canonical(ResolutionOutcome outcome) =>
        JsonSerializer.Serialize(outcome);

    private static IEnumerable<T[]> Permutations<T>(IReadOnlyList<T> values)
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
