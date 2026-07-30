using System.Text.Json;
using Brontide.Reference.Experimental.ComponentManagement;
using NUnit.Framework;

namespace Brontide.Reference.ComponentManagement.Tests;

[TestFixture]
public sealed class ActivationGroupTests
{
    private static readonly ContractId Peer = ContractId.Create("brontide.fake.peer");
    private static readonly VersionLiteral V1 = VersionLiteral.Create("1.0");
    private static readonly OccurrenceId First = OccurrenceId.Create("occ.first");
    private static readonly OccurrenceId Second = OccurrenceId.Create("occ.second");
    private static readonly RegionId Region = RegionId.Create("region.root");

    [Test]
    public void Neutral_vector_inventory_is_complete_and_data_only()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "component-management",
            "fixtures",
            "cm3-activation-group-vectors.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var ids = root.GetProperty("vectors")
            .EnumerateArray()
            .Select(vector => vector.GetProperty("id").GetString())
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1));
            Assert.That(root.GetProperty("fixture").GetString(), Is.EqualTo("cm3-activation-group-vectors"));
            Assert.That(ids, Is.EqualTo(Enumerable.Range(1, 18).Select(index => $"cm3-{index:00}").ToArray()));
            Assert.That(root.GetRawText(), Does.Not.Contain("algorithm"));
        });
    }

    [Test]
    public void Ordinary_cycle_forms_one_ready_group_without_startup_order_or_effects()
    {
        var outcome = new FakeActivationGroupPlanner().Plan(OrdinaryCycle());

        Assert.That(outcome.IsPlanned, Is.True);
        var group = outcome.Plan!.Groups.Single();
        Assert.Multiple(() =>
        {
            Assert.That(group.Cyclic, Is.True);
            Assert.That(group.Members.Select(item => item.Occurrence), Is.EqualTo(new[] { First, Second }));
            Assert.That(group.InternalEdges, Has.Count.EqualTo(2));
            Assert.That(group.Protocols, Is.Empty);
            Assert.That(group.Stages.Select(item => item.Stage), Is.EqualTo(new[]
            {
                ActivationStage.LocalInitialisation,
                ActivationStage.Interconnection,
                ActivationStage.Ready,
            }));
            Assert.That(group.Stages.All(item => !item.OrdinaryGateOpen), Is.True);
            Assert.That(group.ReleasePending, Is.True);
            Assert.That(outcome.Effects, Is.EqualTo(Cm3EffectObservation.None));
            Assert.That(outcome.Plan.Effects, Is.EqualTo(Cm3EffectObservation.None));
        });
    }

    [Test]
    public void Complete_bounded_relational_cycle_adds_only_the_lifecycle_stage()
    {
        var request = RelationalCycle();

        var outcome = new FakeActivationGroupPlanner().Plan(request);

        var group = outcome.Plan!.Groups.Single();
        Assert.Multiple(() =>
        {
            Assert.That(group.Protocols, Has.Count.EqualTo(2));
            Assert.That(group.Stages.Select(item => item.Stage), Is.EqualTo(new[]
            {
                ActivationStage.LocalInitialisation,
                ActivationStage.Interconnection,
                ActivationStage.RelationalInitialisation,
                ActivationStage.Ready,
            }));
            Assert.That(group.Stages.All(item => !item.OrdinaryGateOpen), Is.True);
            Assert.That(outcome.Plan.Decisions.Count(item => item.Kind == "relational-protocol"), Is.EqualTo(2));
        });
    }

    [Test]
    public void Descriptor_cycle_and_version_conflict_fail_without_partial_plan()
    {
        var cycle = OrdinaryCycle();
        var descriptor = new FakeActivationGroupPlanner().Plan(
            cycle with
            {
                Edges = cycle.Edges.Select((edge, index) =>
                    index == 0 ? edge with { Kind = ActivationDependencyKind.DescriptorExpansion } : edge).ToArray(),
            });
        var conflict = new FakeActivationGroupPlanner().Plan(
            cycle with
            {
                Edges = cycle.Edges.Select((edge, index) =>
                    index == 0 ? edge with { Version = VersionLiteral.Create("2.0") } : edge).ToArray(),
            });

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.RecursiveDescriptorExpansion));
            Assert.That(descriptor.Plan, Is.Null);
            Assert.That(conflict.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.ContractVersionConflict));
            Assert.That(conflict.Failure.Source, Is.EqualTo(First));
            Assert.That(conflict.Failure.Target, Is.EqualTo(Second));
            Assert.That(conflict.Failure.Contract, Is.EqualTo(Peer));
            Assert.That(conflict.Failure.Version, Is.EqualTo(VersionLiteral.Create("2.0")));
            Assert.That(conflict.Plan, Is.Null);
            Assert.That(conflict.Effects, Is.EqualTo(Cm3EffectObservation.None));
        });
    }

    [Test]
    public void Lifecycle_and_ordinary_gate_violations_are_structured()
    {
        var relational = RelationalCycle();
        var missing = new FakeActivationGroupPlanner().Plan(
            relational with
            {
                Edges = relational.Edges.Select((edge, index) =>
                    index == 0 ? edge with { Protocol = null } : edge).ToArray(),
            });
        var incomplete = new FakeActivationGroupPlanner().Plan(
            relational with
            {
                Protocols = relational.Protocols.Select((protocol, index) =>
                    index == 0 ? protocol with { TimeoutMilliseconds = 0 } : protocol).ToArray(),
            });
        var ordinary = OrdinaryCycle();
        var early = new FakeActivationGroupPlanner().Plan(
            ordinary with
            {
                Edges = ordinary.Edges.Select((edge, index) =>
                    index == 0 ? edge with { ObservedBeforeRelease = true } : edge).ToArray(),
            });
        var undeclared = new FakeActivationGroupPlanner().Plan(
            ordinary with
            {
                Edges = ordinary.Edges.Select((edge, index) =>
                    index == 0 ? edge with { Protocol = LifecycleProtocolId.Create("protocol.unexpected") } : edge).ToArray(),
            });
        var oneRelationalEdge = relational.Edges[0];
        var crossGroup = new FakeActivationGroupPlanner().Plan(
            Empty(relational.Members, new[] { oneRelationalEdge }) with
            {
                Protocols = new[] { relational.Protocols[0] },
            });

        Assert.Multiple(() =>
        {
            Assert.That(missing.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.LifecycleProtocolRequired));
            Assert.That(incomplete.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.LifecycleProtocolIncomplete));
            Assert.That(early.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.OrdinaryPreReleaseTraffic));
            Assert.That(undeclared.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.UndeclaredLifecycleTraffic));
            Assert.That(crossGroup.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.UndeclaredLifecycleTraffic));
        });
    }

    [Test]
    public void Ready_requires_local_inputs_and_an_acyclic_wait_graph()
    {
        var request = OrdinaryCycle();
        var missing = new FakeActivationGroupPlanner().Plan(
            request with
            {
                Members = request.Members.Select((member, index) =>
                    index == 0
                        ? member with { RequiredReadyInputs = new[] { LifecycleInputId.Create("input.missing") } }
                        : member).ToArray(),
            });
        var waiting = new FakeActivationGroupPlanner().Plan(
            request with
            {
                Members = new[]
                {
                    request.Members[0] with { WaitsForReadyOf = new[] { Second } },
                    request.Members[1] with { WaitsForReadyOf = new[] { First } },
                },
            });

        Assert.Multiple(() =>
        {
            Assert.That(missing.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.ReadyInputUnavailable));
            Assert.That(waiting.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.CircularReadyWait));
            Assert.That(waiting.Failure.Member, Is.Not.Null);
            Assert.That(waiting.Plan, Is.Null);
        });
    }

    [Test]
    public void Cross_region_cycle_requires_matching_import_export_or_explicit_widening()
    {
        var port = PortId.Create("port.peer");
        var request = OrdinaryCycle();
        request = request with
        {
            Members = new[]
            {
                request.Members[0],
                request.Members[1] with { Region = RegionId.Create("region.child") },
            },
            Edges = request.Edges.Select(edge => edge with { CrossingPort = port }).ToArray(),
        };
        var crossings = request.Edges.Select(edge => new RegionCrossingDeclaration(
            edge.Edge,
            edge.From == First ? Region : RegionId.Create("region.child"),
            edge.To == Second ? RegionId.Create("region.child") : Region,
            port,
            true,
            true)).ToArray();
        var accepted = new FakeActivationGroupPlanner().Plan(request with { RegionCrossings = crossings });
        var widened = new FakeActivationGroupPlanner().Plan(
            request with
            {
                Edges = request.Edges.Select(edge => edge with { AllowWiderRegionProposal = true }).ToArray(),
            });
        var refused = new FakeActivationGroupPlanner().Plan(request);
        var conflict = new FakeActivationGroupPlanner().Plan(
            request with
            {
                RegionCrossings = crossings.Select((crossing, index) =>
                    index == 0 ? crossing with { ImportDeclared = false } : crossing).ToArray(),
            });

        Assert.Multiple(() =>
        {
            Assert.That(accepted.IsPlanned, Is.True);
            Assert.That(accepted.Plan!.Groups.Single().RegionCrossings, Has.Count.EqualTo(2));
            Assert.That(accepted.Plan.RegionCrossings, Has.Count.EqualTo(2));
            Assert.That(
                () => ((IList<RegionCrossingDeclaration>)accepted.Plan.RegionCrossings)[0] = crossings[0],
                Throws.TypeOf<NotSupportedException>());
            Assert.That(widened.Wider!.Port, Is.EqualTo(port));
            Assert.That(widened.Plan, Is.Null);
            Assert.That(refused.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.RegionCrossingRequired));
            Assert.That(conflict.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.RegionCrossingConflict));
        });
    }

    [Test]
    public void Acyclic_condensation_is_dependency_first_and_permutation_invariant()
    {
        var third = OccurrenceId.Create("occ.third");
        var request = Empty(
            new[] { Member(First), Member(Second), Member(third) },
            new[]
            {
                Edge("edge.first-second", First, Second),
                Edge("edge.second-third", Second, third),
            });
        var baseline = Canonical(new FakeActivationGroupPlanner().Plan(request));

        foreach (var members in Permutations(request.Members))
        {
            foreach (var edges in Permutations(request.Edges))
            {
                var outcome = new FakeActivationGroupPlanner().Plan(request with { Members = members, Edges = edges });
                Assert.That(Canonical(outcome), Is.EqualTo(baseline));
            }
        }

        var groups = new FakeActivationGroupPlanner().Plan(request).Plan!.Groups;
        Assert.That(
            groups.Select(group => group.Members.Single().Occurrence),
            Is.EqualTo(new[] { third, Second, First }));
    }

    [Test]
    public void Duplicate_and_missing_identities_fail_closed()
    {
        var request = OrdinaryCycle();
        var duplicate = new FakeActivationGroupPlanner().Plan(
            request with { Members = request.Members.Concat(request.Members.Take(1)).ToArray() });
        var missing = new FakeActivationGroupPlanner().Plan(
            request with { Members = request.Members.Take(1).ToArray() });
        var duplicateProvision = new FakeActivationGroupPlanner().Plan(
            request with
            {
                Members = request.Members.Select((member, index) =>
                    index == 1
                        ? member with { Provides = member.Provides.Concat(member.Provides).ToArray() }
                        : member).ToArray(),
            });
        var relational = RelationalCycle();
        var duplicateProtocolEdge = new FakeActivationGroupPlanner().Plan(
            relational with
            {
                Protocols = relational.Protocols.Concat(new[]
                {
                    relational.Protocols[0] with
                    {
                        Protocol = LifecycleProtocolId.Create("protocol.duplicate-edge"),
                    },
                }).ToArray(),
            });

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.ContradictoryIdentity));
            Assert.That(missing.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.MissingMember));
            Assert.That(duplicateProvision.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.ContractVersionConflict));
            Assert.That(duplicateProtocolEdge.Failure!.Kind, Is.EqualTo(ActivationGroupFailureKind.ContradictoryIdentity));
            Assert.That(duplicate.Effects, Is.EqualTo(Cm3EffectObservation.None));
            Assert.That(missing.Plan, Is.Null);
        });
    }

    [Test]
    public void Returned_nested_collections_are_read_only_and_detached()
    {
        var provides = new[] { new ProvidedContract(Peer, V1) };
        var first = Member(First) with { Provides = provides };
        var request = Empty(new[] { first, Member(Second) }, new[] { Edge("edge.first-second", First, Second) });

        var outcome = new FakeActivationGroupPlanner().Plan(request);
        provides[0] = new ProvidedContract(ContractId.Create("brontide.fake.changed"), V1);

        var returned = outcome.Plan!.Groups.Single(group => group.Members.Any(item => item.Occurrence == First))
            .Members.Single(item => item.Occurrence == First);
        Assert.Multiple(() =>
        {
            Assert.That(returned.Provides.Single().Contract, Is.EqualTo(Peer));
            Assert.That(
                () => ((IList<ProvidedContract>)returned.Provides)[0] = new ProvidedContract(Peer, V1),
                Throws.TypeOf<NotSupportedException>());
        });
    }

    private static ActivationGroupRequest OrdinaryCycle() =>
        Empty(
            new[] { Member(First), Member(Second) },
            new[]
            {
                Edge("edge.first-second", First, Second),
                Edge("edge.second-first", Second, First),
            });

    private static ActivationGroupRequest RelationalCycle()
    {
        var edges = new[]
        {
            Edge("edge.first-second", First, Second) with
            {
                Kind = ActivationDependencyKind.RelationalInitialisation,
                Protocol = LifecycleProtocolId.Create("protocol.first-second"),
                ObservedBeforeRelease = true,
            },
            Edge("edge.second-first", Second, First) with
            {
                Kind = ActivationDependencyKind.RelationalInitialisation,
                Protocol = LifecycleProtocolId.Create("protocol.second-first"),
                ObservedBeforeRelease = true,
            },
        };
        return Empty(
            new[] { Member(First), Member(Second) },
            edges) with
        {
            Protocols = edges.Select(edge => Protocol(edge, edge.Protocol!.Value)).ToArray(),
        };
    }

    private static ActivationGroupRequest Empty(
        IReadOnlyList<ActivationGroupMember> members,
        IReadOnlyList<ActivationDependency> edges) =>
        new(
            ActivationGroupRequestId.Create("activation.test"),
            GenerationId.Create("gen.test"),
            RestartScopeId.Create("restart.test"),
            members,
            edges,
            Array.Empty<LifecycleProtocolDeclaration>(),
            Array.Empty<RegionCrossingDeclaration>());

    private static ActivationGroupMember Member(OccurrenceId occurrence) =>
        new(
            occurrence,
            DefinitionId.Create($"def.{occurrence.Value}"),
            Region,
            new[] { new ProvidedContract(Peer, V1) },
            Array.Empty<LifecycleInputId>(),
            Array.Empty<LifecycleInputId>(),
            Array.Empty<OccurrenceId>());

    private static ActivationDependency Edge(string id, OccurrenceId from, OccurrenceId to) =>
        new(
            ActivationEdgeId.Create(id),
            from,
            to,
            ActivationDependencyKind.OrdinaryInteraction,
            Peer,
            V1,
            false,
            null,
            null,
            false);

    private static LifecycleProtocolDeclaration Protocol(
        ActivationDependency edge,
        LifecycleProtocolId protocol) =>
        new(
            protocol,
            edge.Edge,
            edge.From,
            edge.To,
            LifecycleOperationId.Create($"operation.{edge.Edge.Value}"),
            new[] { CapabilityId.Create("authority.lifecycle") },
            ShapeId.Create("shape.lifecycle-input"),
            ShapeId.Create("shape.lifecycle-output"),
            "concurrent",
            1000,
            1,
            true,
            "peer-acknowledged",
            "fail-group",
            "discard-provisional-state");

    private static string Canonical(ActivationGroupOutcome outcome) =>
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
