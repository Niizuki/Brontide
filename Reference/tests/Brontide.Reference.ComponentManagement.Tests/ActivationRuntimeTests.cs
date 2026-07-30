using System.Text.Json;
using Brontide.Reference.Experimental.ComponentManagement;
using NUnit.Framework;

namespace Brontide.Reference.ComponentManagement.Tests;

[TestFixture]
public sealed class ActivationRuntimeTests
{
    private static readonly ContractId Peer = ContractId.Create("brontide.fake.peer");
    private static readonly VersionLiteral V1 = VersionLiteral.Create("1.0");
    private static readonly OccurrenceId First = OccurrenceId.Create("occ.first");
    private static readonly OccurrenceId Second = OccurrenceId.Create("occ.second");
    private static readonly RegionId Region = RegionId.Create("region.root");
    private static readonly RestartScopeId TargetScope = RestartScopeId.Create("restart.target");
    private static readonly RestartScopeId OtherScope = RestartScopeId.Create("restart.other");
    private static readonly GenerationId Retained = GenerationId.Create("gen.retained");
    private static readonly GenerationId Other = GenerationId.Create("gen.other");

    [Test]
    public void Neutral_vector_inventory_is_complete_and_data_only()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "component-management",
            "fixtures",
            "cm4-activation-runtime-vectors.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var ids = root.GetProperty("vectors")
            .EnumerateArray()
            .Select(vector => vector.GetProperty("id").GetString())
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1));
            Assert.That(root.GetProperty("fixture").GetString(), Is.EqualTo("cm4-activation-runtime-vectors"));
            Assert.That(ids, Is.EqualTo(Enumerable.Range(1, 20).Select(index => $"cm4-{index:00}").ToArray()));
            Assert.That(root.GetRawText(), Does.Not.Contain("algorithm"));
        });
    }

    [Test]
    public void Complete_establishment_releases_once_and_preserves_unrelated_scope()
    {
        var request = Request(Plan(ordinaryCycle: true));

        var outcome = new FakeActivationRuntime().Activate(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.Active));
            Assert.That(outcome.Observation.Events.Count(item => item.Kind == "release"), Is.EqualTo(1));
            Assert.That(outcome.Observation.Events.Count(item => item.Kind == "cutover"), Is.EqualTo(1));
            Assert.That(outcome.Observation.Release, Is.EqualTo(request.Release));
            Assert.That(outcome.Observation.RetainedDisposition, Is.EqualTo(request.RetainedDisposition));
            Assert.That(outcome.Observation.Effects.Released, Is.True);
            Assert.That(outcome.Observation.Effects.ActiveGenerationMutated, Is.True);
            Assert.That(outcome.Observation.Effects.CapabilityGranted, Is.False);
            Assert.That(Scope(outcome, TargetScope).Generation, Is.EqualTo(request.Plan.Generation));
            Assert.That(Scope(outcome, OtherScope), Is.EqualTo(new RuntimeScopeObservation(OtherScope, Other, RuntimeScopeStatus.Active)));
            Assert.That(
                outcome.Observation.Events.Where(item => item.Kind == "stage-completed").All(item => item.Member is null),
                Is.True);
        });
    }

    [Test]
    public void Preparation_is_optional_effect_free_and_failure_stops_before_establishment()
    {
        var request = Request(Plan()) with
        {
            Preparation = new(
                PreparationId.Create("prep.one"),
                new[] { PreparationStepKind.ValidateArtifact, PreparationStepKind.WarmCache },
                false),
        };

        var outcome = new FakeActivationRuntime().Activate(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.PreparationFailed));
            Assert.That(outcome.Observation.Effects, Is.EqualTo(ActivationRuntimeEffects.None));
            Assert.That(outcome.Observation.Events.Select(item => item.Kind), Is.EqualTo(new[] { "preparation" }));
            Assert.That(Scope(outcome, TargetScope).Generation, Is.EqualTo(Retained));
            Assert.That(Scope(outcome, OtherScope).Generation, Is.EqualTo(Other));
        });
    }

    [Test]
    public void Failed_member_stage_is_a_prefix_and_prevents_release()
    {
        var request = Request(Plan(relational: true));
        var failed = request.StageOutcomes.Select(item =>
            item.Member == Second && item.Stage == ActivationStage.RelationalInitialisation
                ? item with { Succeeded = false, Detail = "handshake failed" }
                : item).ToArray();

        var outcome = new FakeActivationRuntime().Activate(request with { StageOutcomes = failed });

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.EstablishmentFailed));
            Assert.That(outcome.Failure!.Member, Is.EqualTo(Second));
            Assert.That(outcome.Failure.Stage, Is.EqualTo(ActivationStage.RelationalInitialisation));
            Assert.That(outcome.Observation.Events.Any(item => item.Kind == "release"), Is.False);
            Assert.That(outcome.Observation.Events.Any(item =>
                item.Kind == "stage-completed" && item.Stage == ActivationStage.Ready), Is.False);
            Assert.That(Scope(outcome, TargetScope).Generation, Is.EqualTo(Retained));
        });
    }

    [Test]
    public void Gates_refuse_ordinary_pre_release_and_admit_only_declared_relational_protocol()
    {
        var plan = Plan(relational: true);
        var group = plan.Groups.Single();
        var protocol = group.Protocols[0];
        var lifecycle = new RuntimeInteractionAttempt(
            RuntimeInteractionId.Create("interaction.lifecycle"),
            group.Group,
            protocol.From,
            protocol.To,
            RuntimeInteractionPhase.RelationalInitialisation,
            RuntimeInteractionKind.Lifecycle,
            protocol.Edge,
            protocol.Operation,
            protocol.Authority[0],
            protocol.InputShape);
        var lifecycleOutcome = new FakeActivationRuntime().Activate(
            Request(plan) with { InteractionAttempts = new[] { lifecycle } });
        var ordinary = lifecycle with
        {
            Interaction = RuntimeInteractionId.Create("interaction.ordinary"),
            Phase = RuntimeInteractionPhase.LocalInitialisation,
            Kind = RuntimeInteractionKind.Ordinary,
            Operation = null,
            Capability = null,
            InputShape = null,
        };
        var ordinaryOutcome = new FakeActivationRuntime().Activate(
            Request(plan) with { InteractionAttempts = new[] { ordinary } });
        var ordinaryPlan = Plan();
        var ordinaryGroup = ordinaryPlan.Groups.Single(item => item.Members.Any(member => member.Occurrence == First));
        var ordinaryEdge = ordinaryPlan.Groups.SelectMany(item => item.InternalEdges)
            .Concat(Array.Empty<ActivationDependency>())
            .SingleOrDefault();
        var activeOrdinary = ordinary with
        {
            Interaction = RuntimeInteractionId.Create("interaction.active"),
            Group = ordinaryGroup.Group,
            Phase = RuntimeInteractionPhase.Active,
            Edge = ordinaryEdge?.Edge ?? ActivationEdgeId.Create("edge.first-second"),
            From = First,
            To = Second,
        };
        var activeOutcome = new FakeActivationRuntime().Activate(
            Request(ordinaryPlan) with { InteractionAttempts = new[] { activeOrdinary } });
        var undeclaredOutcome = new FakeActivationRuntime().Activate(
            Request(ordinaryPlan) with
            {
                InteractionAttempts = new[]
                {
                    activeOrdinary with { Edge = ActivationEdgeId.Create("edge.undeclared") },
                },
            });

        Assert.Multiple(() =>
        {
            Assert.That(lifecycleOutcome.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.Active));
            Assert.That(lifecycleOutcome.Observation.Effects.LifecycleOperationExecuted, Is.True);
            Assert.That(lifecycleOutcome.Observation.Interactions.Single().Admitted, Is.True);
            Assert.That(ordinaryOutcome.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.InteractionRefused));
            Assert.That(ordinaryOutcome.Observation.Interactions.Single().Admitted, Is.False);
            Assert.That(ordinaryOutcome.Observation.Effects.OrdinaryInteractionAdmitted, Is.False);
            Assert.That(activeOutcome.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.Active));
            Assert.That(activeOutcome.Observation.Effects.OrdinaryInteractionAdmitted, Is.True);
            Assert.That(undeclaredOutcome.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.InteractionRefused));
        });
    }

    [Test]
    public void Post_release_bindings_preserve_identity_provenance_authority_and_failure()
    {
        var plan = Plan();
        var delivered = Exercise("exercise.delivered", BindingExposureKind.Distinct, null, true, BindingDeliveryResult.Delivered, null);
        var failed = Exercise(
            "exercise.failed",
            BindingExposureKind.Mediated,
            MediationId.Create("mediation.runtime"),
            true,
            BindingDeliveryResult.Failed,
            "provider unavailable");

        var outcome = new FakeActivationRuntime().Activate(
            Request(plan) with { BindingExercises = new[] { delivered, failed } });

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.Active));
            Assert.That(outcome.Observation.BindingExercises, Has.Count.EqualTo(2));
            Assert.That(outcome.Observation.BindingExercises.Single(item => item.Exercise == failed.Exercise).Failure,
                Is.EqualTo("provider unavailable"));
            Assert.That(outcome.Observation.BindingExercises.All(item => item.Source == SourceId.Create("source.fixture")), Is.True);
            Assert.That(outcome.Observation.Effects.OrdinaryInteractionAdmitted, Is.True);
        });
    }

    [Test]
    public void Release_failure_obeys_before_cutover_and_each_post_cutover_rollback_disposition()
    {
        var request = Request(Plan());
        var before = new FakeActivationRuntime().Activate(request with
        {
            Release = request.Release with { FailureMoment = ReleaseFailureMoment.BeforeCutover },
        });
        var rolledBack = new FakeActivationRuntime().Activate(request with
        {
            Release = request.Release with { FailureMoment = ReleaseFailureMoment.AfterCutover },
            Rollback = RollbackAvailability.Available,
        });
        var unavailable = new FakeActivationRuntime().Activate(request with
        {
            Release = request.Release with { FailureMoment = ReleaseFailureMoment.AfterCutover },
            Rollback = RollbackAvailability.Unavailable,
        });
        var corrupted = new FakeActivationRuntime().Activate(request with
        {
            Release = request.Release with { FailureMoment = ReleaseFailureMoment.AfterCutover },
            Rollback = RollbackAvailability.RetainedGenerationCorrupted,
        });

        Assert.Multiple(() =>
        {
            Assert.That(before.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.ReleaseFailedBeforeCutover));
            Assert.That(Scope(before, TargetScope).Generation, Is.EqualTo(Retained));
            Assert.That(rolledBack.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.RolledBack));
            Assert.That(Scope(rolledBack, TargetScope).Generation, Is.EqualTo(Retained));
            Assert.That(rolledBack.Observation.Effects.RollbackAttempted, Is.True);
            Assert.That(unavailable.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.RollbackUnavailable));
            Assert.That(Scope(unavailable, TargetScope).Status, Is.EqualTo(RuntimeScopeStatus.Degraded));
            Assert.That(unavailable.Observation.Effects.RollbackAttempted, Is.False);
            Assert.That(corrupted.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.RetainedGenerationCorrupted));
            Assert.That(corrupted.Observation.Effects.RollbackAttempted, Is.True);
            Assert.That(Scope(corrupted, OtherScope).Generation, Is.EqualTo(Other));
        });
    }

    [Test]
    public void Child_activation_requires_open_port_and_replacement_lifecycle_and_orders_host_assisted_export()
    {
        var request = Request(Plan());
        var child = new ChildActivationDeclaration(
            OtherScope,
            Other,
            PortId.Create("port.child"),
            true,
            false,
            false,
            true,
            1,
            2,
            false);
        var accepted = new FakeActivationRuntime().Activate(request with { Child = child });
        var closed = new FakeActivationRuntime().Activate(request with { Child = child with { RuntimeOpen = false } });
        var replacement = new FakeActivationRuntime().Activate(request with { Child = child with { Occupied = true } });
        var order = new FakeActivationRuntime().Activate(request with
        {
            Child = child with { ExportReleaseSequence = 1 },
        });

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.Active));
            Assert.That(Scope(accepted, OtherScope).Generation, Is.EqualTo(Other));
            Assert.That(accepted.Observation.Events.FindIndex(item => item.Kind == "release"),
                Is.LessThan(accepted.Observation.Events.FindIndex(item => item.Kind == "outer-boundary-released")));
            Assert.That(closed.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.ChildPortClosed));
            Assert.That(replacement.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.ReplacementLifecycleRequired));
            Assert.That(order.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.HostAssistedOrderConflict));
        });
    }

    [Test]
    public void Conflicting_scope_stage_and_binding_observations_fail_closed()
    {
        var request = Request(Plan());
        var scope = new FakeActivationRuntime().Activate(request with { RequestedRestartScope = OtherScope });
        var sameGeneration = new FakeActivationRuntime().Activate(request with
        {
            RetainedGeneration = request.Plan.Generation,
            ActiveScopes = request.ActiveScopes.Select(item =>
                item.Scope == TargetScope ? item with { Generation = request.Plan.Generation } : item).ToArray(),
        });
        var missingStage = new FakeActivationRuntime().Activate(request with
        {
            StageOutcomes = request.StageOutcomes.Skip(1).ToArray(),
        });
        var badBinding = Exercise(
            "exercise.denied",
            BindingExposureKind.Distinct,
            null,
            false,
            BindingDeliveryResult.Delivered,
            null);
        var binding = new FakeActivationRuntime().Activate(request with { BindingExercises = new[] { badBinding } });

        Assert.Multiple(() =>
        {
            Assert.That(scope.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.RestartScopeConflict));
            Assert.That(sameGeneration.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.RestartScopeConflict));
            Assert.That(missingStage.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.StageObservationConflict));
            Assert.That(binding.Kind, Is.EqualTo(ActivationRuntimeOutcomeKind.BindingObservationConflict));
            Assert.That(scope.Observation.Effects, Is.EqualTo(ActivationRuntimeEffects.None));
            Assert.That(binding.Observation.BindingExercises, Is.Empty);
        });
    }

    [Test]
    public void Complete_observation_is_permutation_invariant_and_detached()
    {
        var request = Request(Plan(ordinaryCycle: true));
        var forward = new FakeActivationRuntime().Activate(request);
        var reverse = new FakeActivationRuntime().Activate(request with
        {
            ActiveScopes = request.ActiveScopes.Reverse().ToArray(),
            StageOutcomes = request.StageOutcomes.Reverse().ToArray(),
        });
        var scopes = (ActiveScopeSnapshot[])request.ActiveScopes;
        scopes[0] = scopes[0] with { Generation = GenerationId.Create("gen.changed") };

        Assert.Multiple(() =>
        {
            Assert.That(reverse.Kind, Is.EqualTo(forward.Kind));
            Assert.That(reverse.Observation.Events, Is.EqualTo(forward.Observation.Events));
            Assert.That(reverse.Observation.Scopes, Is.EqualTo(forward.Observation.Scopes));
            Assert.That(reverse.Observation.Effects, Is.EqualTo(forward.Observation.Effects));
            Assert.That(Scope(forward, TargetScope).Generation, Is.EqualTo(request.Plan.Generation));
            Assert.That(Scope(forward, OtherScope).Generation, Is.EqualTo(Other));
            Assert.That(
                () => ((IList<ActivationRuntimeEvent>)forward.Observation.Events)[0] =
                    new ActivationRuntimeEvent(1, "changed", null, null, null, ""),
                Throws.TypeOf<NotSupportedException>());
        });
    }

    private static RuntimeScopeObservation Scope(ActivationRuntimeOutcome outcome, RestartScopeId scope) =>
        outcome.Observation.Scopes.Single(item => item.Scope == scope);

    private static BindingExerciseDeclaration Exercise(
        string id,
        BindingExposureKind exposure,
        MediationId? mediation,
        bool authority,
        BindingDeliveryResult delivery,
        string? failure) =>
        new(
            BindingExerciseId.Create(id),
            BindingId.Create($"binding.{id}"),
            First,
            Second,
            SourceId.Create("source.fixture"),
            exposure,
            mediation,
            RoutingDecisionId.Create($"route.{id}"),
            authority,
            delivery,
            failure);

    private static ActivationRuntimeRequest Request(ActivationGroupPlan plan)
    {
        var stages = plan.Groups.SelectMany(group =>
            group.Stages.SelectMany(stage =>
                group.Members.Select(member =>
                    new MemberStageOutcome(group.Group, member.Occurrence, stage.Stage, true, "completed")))).ToArray();
        return new(
            ActivationAttemptId.Create("activation.test"),
            plan,
            TargetScope,
            Retained,
            new[]
            {
                new ActiveScopeSnapshot(TargetScope, Retained, RuntimeScopeStatus.Active),
                new ActiveScopeSnapshot(OtherScope, Other, RuntimeScopeStatus.Active),
            },
            null,
            stages,
            Array.Empty<RuntimeInteractionAttempt>(),
            Array.Empty<BindingExerciseDeclaration>(),
            new ReleaseDeclaration(ReleaseId.Create("release.test"), ReleaseFailureMoment.None),
            RollbackAvailability.Available,
            RetainedGenerationDisposition.TerminateAfterRelease,
            null);
    }

    private static ActivationGroupPlan Plan(bool relational = false, bool ordinaryCycle = false)
    {
        var firstToSecond = Edge("edge.first-second", First, Second);
        var edges = ordinaryCycle || relational
            ? new[] { firstToSecond, Edge("edge.second-first", Second, First) }
            : new[] { firstToSecond };
        IReadOnlyList<LifecycleProtocolDeclaration> protocols = Array.Empty<LifecycleProtocolDeclaration>();
        if (relational)
        {
            edges = edges.Select(edge => edge with
            {
                Kind = ActivationDependencyKind.RelationalInitialisation,
                Protocol = LifecycleProtocolId.Create($"protocol.{edge.Edge.Value}"),
                ObservedBeforeRelease = true,
            }).ToArray();
            protocols = edges.Select(Protocol).ToArray();
        }

        var request = new ActivationGroupRequest(
            ActivationGroupRequestId.Create("group.runtime"),
            GenerationId.Create("gen.target"),
            TargetScope,
            new[] { Member(First), Member(Second) },
            edges,
            protocols,
            Array.Empty<RegionCrossingDeclaration>());
        return new FakeActivationGroupPlanner().Plan(request).Plan!;
    }

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

    private static LifecycleProtocolDeclaration Protocol(ActivationDependency dependency) =>
        new(
            dependency.Protocol!.Value,
            dependency.Edge,
            dependency.From,
            dependency.To,
            LifecycleOperationId.Create($"operation.{dependency.Edge.Value}"),
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
}

internal static class ActivationRuntimeTestExtensions
{
    public static int FindIndex<T>(this IReadOnlyList<T> values, Func<T, bool> predicate)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (predicate(values[index]))
            {
                return index;
            }
        }

        return -1;
    }
}
