using System.Collections.ObjectModel;

namespace Brontide.Reference.Experimental.ComponentManagement;

public enum PreparationStepKind
{
    ValidateArtifact,
    BuildImage,
    WarmCache,
    PrepareStateSnapshot,
}

public enum RuntimeInteractionPhase
{
    LocalInitialisation,
    Interconnection,
    RelationalInitialisation,
    Ready,
    Active,
}

public enum RuntimeInteractionKind
{
    Lifecycle,
    Ordinary,
}

public enum BindingExposureKind
{
    Distinct,
    Mediated,
}

public enum BindingDeliveryResult
{
    Delivered,
    Denied,
    Failed,
}

public enum ReleaseFailureMoment
{
    None,
    BeforeCutover,
    AfterCutover,
}

public enum RollbackAvailability
{
    Available,
    Unavailable,
    RetainedGenerationCorrupted,
}

public enum RetainedGenerationDisposition
{
    TerminateAfterRelease,
    RetainForRollback,
}

public enum RuntimeScopeStatus
{
    Active,
    Inactive,
    Degraded,
}

public enum ActivationRuntimeOutcomeKind
{
    Active,
    RolledBack,
    PreparationFailed,
    EstablishmentFailed,
    ReleaseFailedBeforeCutover,
    RollbackUnavailable,
    RetainedGenerationCorrupted,
    InvalidCm3Plan,
    RestartScopeConflict,
    StageObservationConflict,
    InteractionRefused,
    BindingObservationConflict,
    ChildPortClosed,
    ReplacementLifecycleRequired,
    HostAssistedOrderConflict,
}

public sealed record PreparationDeclaration(
    PreparationId Preparation,
    IReadOnlyList<PreparationStepKind> Steps,
    bool Succeeds);

public sealed record ActiveScopeSnapshot(
    RestartScopeId Scope,
    GenerationId Generation,
    RuntimeScopeStatus Status);

public sealed record MemberStageOutcome(
    ActivationGroupId Group,
    OccurrenceId Member,
    ActivationStage Stage,
    bool Succeeded,
    string Detail);

public sealed record RuntimeInteractionAttempt(
    RuntimeInteractionId Interaction,
    ActivationGroupId Group,
    OccurrenceId From,
    OccurrenceId To,
    RuntimeInteractionPhase Phase,
    RuntimeInteractionKind Kind,
    ActivationEdgeId Edge,
    LifecycleOperationId? Operation,
    CapabilityId? Capability,
    ShapeId? InputShape);

public sealed record BindingExerciseDeclaration(
    BindingExerciseId Exercise,
    BindingId Binding,
    OccurrenceId Consumer,
    OccurrenceId Provider,
    SourceId Source,
    BindingExposureKind Exposure,
    MediationId? Mediation,
    RoutingDecisionId Routing,
    bool AuthorityAdmitted,
    BindingDeliveryResult Delivery,
    string? Failure);

public sealed record ReleaseDeclaration(
    ReleaseId Release,
    ReleaseFailureMoment FailureMoment);

public sealed record ChildActivationDeclaration(
    RestartScopeId ParentScope,
    GenerationId ParentGeneration,
    PortId Port,
    bool RuntimeOpen,
    bool Occupied,
    bool ReplacementLifecycleDeclared,
    bool HostAssisted,
    int InternalReleaseSequence,
    int ExportReleaseSequence,
    bool OuterHostOwnsAdmission);

public sealed record ActivationRuntimeRequest(
    ActivationAttemptId Attempt,
    ActivationGroupPlan Plan,
    RestartScopeId RequestedRestartScope,
    GenerationId RetainedGeneration,
    IReadOnlyList<ActiveScopeSnapshot> ActiveScopes,
    PreparationDeclaration? Preparation,
    IReadOnlyList<MemberStageOutcome> StageOutcomes,
    IReadOnlyList<RuntimeInteractionAttempt> InteractionAttempts,
    IReadOnlyList<BindingExerciseDeclaration> BindingExercises,
    ReleaseDeclaration Release,
    RollbackAvailability Rollback,
    RetainedGenerationDisposition RetainedDisposition,
    ChildActivationDeclaration? Child);

public sealed record ActivationRuntimeEffects(
    bool Prepared,
    bool EstablishmentStarted,
    bool ActorEndpointEstablished,
    bool LifecycleOperationExecuted,
    bool MemberReportedReady,
    bool Released,
    bool OrdinaryInteractionAdmitted,
    bool ActiveGenerationMutated,
    bool RetainedGenerationRetired,
    bool RollbackAttempted,
    bool CapabilityGranted)
{
    public static ActivationRuntimeEffects None { get; } =
        new(false, false, false, false, false, false, false, false, false, false, false);
}

public sealed record RuntimeInteractionDecision(
    RuntimeInteractionId Interaction,
    bool Admitted,
    string Reason);

public sealed record BindingExerciseObservation(
    BindingExerciseId Exercise,
    BindingId Binding,
    OccurrenceId Consumer,
    OccurrenceId Provider,
    SourceId Source,
    BindingExposureKind Exposure,
    MediationId? Mediation,
    RoutingDecisionId Routing,
    bool AuthorityAdmitted,
    BindingDeliveryResult Delivery,
    string? Failure);

public sealed record RuntimeScopeObservation(
    RestartScopeId Scope,
    GenerationId? Generation,
    RuntimeScopeStatus Status);

public sealed record ActivationRuntimeEvent(
    int Sequence,
    string Kind,
    ActivationGroupId? Group,
    ActivationStage? Stage,
    OccurrenceId? Member,
    string Detail);

public sealed record ActivationRuntimeFailure(
    ActivationRuntimeOutcomeKind Kind,
    string Reason,
    ActivationGroupId? Group,
    ActivationStage? Stage,
    OccurrenceId? Member,
    RuntimeInteractionId? Interaction,
    BindingExerciseId? Exercise,
    PortId? Port);

public sealed record ActivationRuntimeObservation(
    ActivationAttemptId Attempt,
    GenerationId TargetGeneration,
    GenerationId RetainedGeneration,
    RestartScopeId RestartScope,
    PreparationDeclaration? Preparation,
    ReleaseDeclaration Release,
    RetainedGenerationDisposition RetainedDisposition,
    IReadOnlyList<ActivationRuntimeEvent> Events,
    IReadOnlyList<RuntimeInteractionDecision> Interactions,
    IReadOnlyList<BindingExerciseObservation> BindingExercises,
    IReadOnlyList<RuntimeScopeObservation> Scopes,
    ChildActivationDeclaration? Child,
    ActivationRuntimeEffects Effects);

public sealed record ActivationRuntimeOutcome(
    ActivationRuntimeOutcomeKind Kind,
    ActivationRuntimeObservation Observation,
    ActivationRuntimeFailure? Failure)
{
    public bool IsActive => Kind == ActivationRuntimeOutcomeKind.Active;
}

public sealed class FakeActivationRuntime
{
    public ActivationRuntimeOutcome Activate(ActivationRuntimeRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var request = Snapshot(input);
        var events = new List<ActivationRuntimeEvent>();
        var interactions = new List<RuntimeInteractionDecision>();
        var bindingObservations = new List<BindingExerciseObservation>();
        var unchangedScopes = ScopeObservations(request.ActiveScopes);
        var effects = ActivationRuntimeEffects.None;

        ActivationRuntimeOutcome Refuse(
            ActivationRuntimeOutcomeKind kind,
            string reason,
            ActivationGroupId? group = null,
            ActivationStage? stage = null,
            OccurrenceId? member = null,
            RuntimeInteractionId? interaction = null,
            BindingExerciseId? exercise = null,
            PortId? port = null) =>
            Outcome(
                request,
                kind,
                events,
                interactions,
                bindingObservations,
                unchangedScopes,
                effects,
                new(kind, reason, group, stage, member, interaction, exercise, port));

        if (!ValidPlan(request.Plan))
        {
            return Refuse(ActivationRuntimeOutcomeKind.InvalidCm3Plan, "CM3 plan is not a closed-gate Release-pending plan");
        }

        if (request.RequestedRestartScope != request.Plan.RestartScope)
        {
            return Refuse(ActivationRuntimeOutcomeKind.RestartScopeConflict, "requested restart scope differs from the CM3 plan");
        }

        if (request.Plan.Generation == request.RetainedGeneration)
        {
            return Refuse(ActivationRuntimeOutcomeKind.RestartScopeConflict, "target generation must differ from the retained generation");
        }

        var duplicateScope = request.ActiveScopes
            .GroupBy(item => item.Scope)
            .FirstOrDefault(group => group.Count() > 1);
        var targetScopes = request.ActiveScopes.Where(item => item.Scope == request.Plan.RestartScope).ToArray();
        if (duplicateScope is not null
            || targetScopes.Length != 1
            || targetScopes[0].Status != RuntimeScopeStatus.Active
            || targetScopes[0].Generation != request.RetainedGeneration)
        {
            return Refuse(
                ActivationRuntimeOutcomeKind.RestartScopeConflict,
                duplicateScope is not null
                    ? $"duplicate active scope {duplicateScope.Key}"
                    : "target scope is missing, inactive, or carries a different retained generation");
        }

        var childFailure = ValidateChild(request);
        if (childFailure is not null)
        {
            return Refuse(childFailure.Value.Kind, childFailure.Value.Reason, port: request.Child?.Port);
        }

        var stageValidation = ValidateStageOutcomes(request);
        if (stageValidation is not null)
        {
            return Refuse(
                ActivationRuntimeOutcomeKind.StageObservationConflict,
                stageValidation.Value.Reason,
                stageValidation.Value.Group,
                stageValidation.Value.Stage,
                stageValidation.Value.Member);
        }

        var interactionValidation = ValidateInteractions(request);
        if (interactionValidation is not null)
        {
            interactions.Add(new(interactionValidation.Value.Attempt.Interaction, false, interactionValidation.Value.Reason));
            return Refuse(
                ActivationRuntimeOutcomeKind.InteractionRefused,
                interactionValidation.Value.Reason,
                interactionValidation.Value.Attempt.Group,
                member: interactionValidation.Value.Attempt.From,
                interaction: interactionValidation.Value.Attempt.Interaction);
        }

        var bindingValidation = ValidateBindings(request);
        if (bindingValidation is not null)
        {
            return Refuse(
                ActivationRuntimeOutcomeKind.BindingObservationConflict,
                bindingValidation.Value.Reason,
                member: bindingValidation.Value.Exercise.Consumer,
                exercise: bindingValidation.Value.Exercise.Exercise);
        }

        if (request.Preparation is not null)
        {
            events.Add(Event(events, "preparation", detail: request.Preparation.Succeeds ? "completed" : "failed"));
            if (!request.Preparation.Succeeds)
            {
                return Outcome(
                    request,
                    ActivationRuntimeOutcomeKind.PreparationFailed,
                    events,
                    interactions,
                    bindingObservations,
                    unchangedScopes,
                    effects,
                    new(
                        ActivationRuntimeOutcomeKind.PreparationFailed,
                        "optional preparation failed before establishment",
                        null,
                        null,
                        null,
                        null,
                        null,
                        null));
            }

            effects = effects with { Prepared = true };
        }

        effects = effects with { EstablishmentStarted = true };
        events.Add(Event(events, "establishment-started", detail: request.Plan.RestartScope.Value));

        var orderedOutcomes = OrderedStageOutcomes(request).ToArray();
        foreach (var stageGroup in orderedOutcomes.GroupBy(item => (item.Group, item.Stage)))
        {
            var failure = stageGroup.FirstOrDefault(item => !item.Succeeded);
            if (failure is not null)
            {
                events.Add(Event(
                    events,
                    "stage-failed",
                    failure.Group,
                    failure.Stage,
                    failure.Member,
                    failure.Detail));
                return Outcome(
                    request,
                    ActivationRuntimeOutcomeKind.EstablishmentFailed,
                    events,
                    interactions,
                    bindingObservations,
                    unchangedScopes,
                    effects,
                    new(
                        ActivationRuntimeOutcomeKind.EstablishmentFailed,
                        failure.Detail,
                        failure.Group,
                        failure.Stage,
                        failure.Member,
                        null,
                        null,
                        null));
            }

            events.Add(Event(events, "stage-completed", stageGroup.Key.Group, stageGroup.Key.Stage, detail: "all members"));
            effects = stageGroup.Key.Stage switch
            {
                ActivationStage.Interconnection => effects with { ActorEndpointEstablished = true },
                ActivationStage.RelationalInitialisation => effects with
                {
                    LifecycleOperationExecuted = request.InteractionAttempts.Any(item =>
                        item.Group == stageGroup.Key.Group
                        && item.Phase == RuntimeInteractionPhase.RelationalInitialisation),
                },
                ActivationStage.Ready => effects with { MemberReportedReady = true },
                _ => effects,
            };
        }

        interactions.AddRange(request.InteractionAttempts
            .Where(item => item.Phase == RuntimeInteractionPhase.RelationalInitialisation)
            .OrderBy(item => item.Interaction.Value, StringComparer.Ordinal)
            .Select(item => new RuntimeInteractionDecision(item.Interaction, true, "declared lifecycle protocol")));

        if (request.Release.FailureMoment == ReleaseFailureMoment.BeforeCutover)
        {
            events.Add(Event(events, "release-failed", detail: "before cutover"));
            return Outcome(
                request,
                ActivationRuntimeOutcomeKind.ReleaseFailedBeforeCutover,
                events,
                interactions,
                bindingObservations,
                unchangedScopes,
                effects,
                new(
                    ActivationRuntimeOutcomeKind.ReleaseFailedBeforeCutover,
                    "release failed before cutover; retained generation remains active",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null));
        }

        events.Add(Event(events, "release", detail: request.Release.Release.Value));
        events.Add(Event(events, "cutover", detail: request.Plan.Generation.Value));
        effects = effects with { Released = true, ActiveGenerationMutated = true };

        if (request.Release.FailureMoment == ReleaseFailureMoment.AfterCutover)
        {
            effects = effects with { RollbackAttempted = request.Rollback != RollbackAvailability.Unavailable };
            return request.Rollback switch
            {
                RollbackAvailability.Available => RollBack(request, events, interactions, bindingObservations, effects),
                RollbackAvailability.Unavailable => FailedAfterCutover(
                    request,
                    ActivationRuntimeOutcomeKind.RollbackUnavailable,
                    "rollback unavailable after cutover",
                    events,
                    interactions,
                    bindingObservations,
                    effects),
                _ => FailedAfterCutover(
                    request,
                    ActivationRuntimeOutcomeKind.RetainedGenerationCorrupted,
                    "retained generation is corrupted and cannot be restored",
                    events,
                    interactions,
                    bindingObservations,
                    effects),
            };
        }

        interactions.AddRange(request.InteractionAttempts
            .Where(item => item.Phase == RuntimeInteractionPhase.Active)
            .OrderBy(item => item.Interaction.Value, StringComparer.Ordinal)
            .Select(item => new RuntimeInteractionDecision(item.Interaction, true, "ordinary gate released")));
        bindingObservations.AddRange(request.BindingExercises
            .OrderBy(item => item.Exercise.Value, StringComparer.Ordinal)
            .Select(ObserveBinding));
        effects = effects with
        {
            OrdinaryInteractionAdmitted =
                interactions.Any(item => item.Admitted && item.Reason == "ordinary gate released")
                || bindingObservations.Any(item => item.Delivery == BindingDeliveryResult.Delivered),
            RetainedGenerationRetired =
                request.RetainedDisposition == RetainedGenerationDisposition.TerminateAfterRelease,
        };
        if (effects.RetainedGenerationRetired)
        {
            events.Add(Event(events, "retained-generation-terminated", detail: request.RetainedGeneration.Value));
        }

        if (request.Child is not null)
        {
            events.Add(Event(events, "child-attached", detail: request.Child.Port.Value));
            if (request.Child.HostAssisted)
            {
                events.Add(Event(events, "outer-boundary-released", detail: request.Child.Port.Value));
            }
        }

        return Outcome(
            request,
            ActivationRuntimeOutcomeKind.Active,
            events,
            interactions,
            bindingObservations,
            ReplaceTargetScope(request, request.Plan.Generation, RuntimeScopeStatus.Active),
            effects,
            null);
    }

    private static ActivationRuntimeOutcome RollBack(
        ActivationRuntimeRequest request,
        List<ActivationRuntimeEvent> events,
        List<RuntimeInteractionDecision> interactions,
        List<BindingExerciseObservation> bindings,
        ActivationRuntimeEffects effects)
    {
        events.Add(Event(events, "rollback-restored", detail: request.RetainedGeneration.Value));
        return Outcome(
            request,
            ActivationRuntimeOutcomeKind.RolledBack,
            events,
            interactions,
            bindings,
            ScopeObservations(request.ActiveScopes),
            effects,
            new(
                ActivationRuntimeOutcomeKind.RolledBack,
                "post-cutover failure restored the retained generation",
                null,
                null,
                null,
                null,
                null,
                null));
    }

    private static ActivationRuntimeOutcome FailedAfterCutover(
        ActivationRuntimeRequest request,
        ActivationRuntimeOutcomeKind kind,
        string reason,
        List<ActivationRuntimeEvent> events,
        List<RuntimeInteractionDecision> interactions,
        List<BindingExerciseObservation> bindings,
        ActivationRuntimeEffects effects)
    {
        events.Add(Event(events, "activation-degraded", detail: reason));
        return Outcome(
            request,
            kind,
            events,
            interactions,
            bindings,
            ReplaceTargetScope(request, request.Plan.Generation, RuntimeScopeStatus.Degraded),
            effects,
            new(kind, reason, null, null, null, null, null, null));
    }

    private static (ActivationRuntimeOutcomeKind Kind, string Reason)? ValidateChild(ActivationRuntimeRequest request)
    {
        if (request.Child is null)
        {
            return null;
        }

        var child = request.Child;
        var parent = request.ActiveScopes.SingleOrDefault(item => item.Scope == child.ParentScope);
        if (parent is null
            || parent.Status != RuntimeScopeStatus.Active
            || parent.Generation != child.ParentGeneration
            || child.ParentScope == request.Plan.RestartScope)
        {
            return (ActivationRuntimeOutcomeKind.RestartScopeConflict, "child activation requires a distinct active parent scope");
        }

        if (!child.RuntimeOpen)
        {
            return (ActivationRuntimeOutcomeKind.ChildPortClosed, "child Port is not runtime-open");
        }

        if (child.Occupied && !child.ReplacementLifecycleDeclared)
        {
            return (ActivationRuntimeOutcomeKind.ReplacementLifecycleRequired, "occupied Port replacement requires lifecycle declarations");
        }

        if (child.HostAssisted
            && (child.InternalReleaseSequence <= 0
                || child.ExportReleaseSequence <= child.InternalReleaseSequence))
        {
            return (ActivationRuntimeOutcomeKind.HostAssistedOrderConflict, "host-assisted export must follow internal child Release");
        }

        return null;
    }

    private static (string Reason, ActivationGroupId? Group, ActivationStage? Stage, OccurrenceId? Member)?
        ValidateStageOutcomes(ActivationRuntimeRequest request)
    {
        var expected = request.Plan.Groups
            .SelectMany(group => group.Stages.SelectMany(stage =>
                group.Members.Select(member => (group.Group, member.Occurrence, stage.Stage))))
            .ToArray();
        var duplicate = request.StageOutcomes
            .GroupBy(item => (item.Group, item.Member, item.Stage))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            return ("duplicate member-stage outcome", duplicate.Key.Group, duplicate.Key.Stage, duplicate.Key.Member);
        }

        foreach (var item in request.StageOutcomes)
        {
            if (!expected.Contains((item.Group, item.Member, item.Stage)))
            {
                return ("outcome names a member, group, or stage outside the CM3 plan", item.Group, item.Stage, item.Member);
            }
        }

        var missing = expected.FirstOrDefault(item => !request.StageOutcomes.Any(outcome =>
            outcome.Group == item.Group
            && outcome.Member == item.Occurrence
            && outcome.Stage == item.Stage));
        return missing == default
            ? null
            : ("missing member-stage outcome", missing.Group, missing.Stage, missing.Occurrence);
    }

    private static (RuntimeInteractionAttempt Attempt, string Reason)? ValidateInteractions(ActivationRuntimeRequest request)
    {
        var duplicate = request.InteractionAttempts
            .GroupBy(item => item.Interaction)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            return (duplicate.First(), "duplicate interaction identity");
        }

        foreach (var attempt in request.InteractionAttempts.OrderBy(item => item.Interaction.Value, StringComparer.Ordinal))
        {
            var group = request.Plan.Groups.SingleOrDefault(item => item.Group == attempt.Group);
            if (group is null
                || !group.Members.Any(item => item.Occurrence == attempt.From)
                || !request.Plan.Groups.SelectMany(item => item.Members).Any(item => item.Occurrence == attempt.To))
            {
                return (attempt, "interaction names a member or group outside the CM3 plan");
            }

            if (attempt.Kind == RuntimeInteractionKind.Ordinary)
            {
                if (attempt.Phase != RuntimeInteractionPhase.Active)
                {
                    return (attempt, "ordinary interaction is closed before Release");
                }

                var toGroup = request.Plan.Groups.Single(item =>
                    item.Members.Any(member => member.Occurrence == attempt.To));
                var declaredInternal = group.InternalEdges.Any(edge =>
                    edge.Edge == attempt.Edge
                    && edge.From == attempt.From
                    && edge.To == attempt.To
                    && edge.Kind == ActivationDependencyKind.OrdinaryInteraction);
                var declaredInterGroup = request.Plan.InterGroupEdges.Any(edge =>
                    edge.Edge == attempt.Edge
                    && edge.FromGroup == group.Group
                    && edge.ToGroup == toGroup.Group);
                if (!declaredInternal && !declaredInterGroup)
                {
                    return (attempt, "ordinary interaction does not match a declared CM3 edge");
                }

                continue;
            }

            if (attempt.Phase != RuntimeInteractionPhase.RelationalInitialisation)
            {
                return (attempt, "lifecycle interaction is admitted only during Relational Initialisation");
            }

            var protocol = group.Protocols.SingleOrDefault(item =>
                item.Edge == attempt.Edge
                && item.From == attempt.From
                && item.To == attempt.To
                && item.Operation == attempt.Operation
                && item.InputShape == attempt.InputShape
                && attempt.Capability is not null
                && item.Authority.Contains(attempt.Capability.Value));
            if (protocol is null)
            {
                return (attempt, "lifecycle interaction does not match one declared bounded protocol");
            }
        }

        return null;
    }

    private static (BindingExerciseDeclaration Exercise, string Reason)? ValidateBindings(ActivationRuntimeRequest request)
    {
        var members = request.Plan.Groups.SelectMany(item => item.Members).Select(item => item.Occurrence).ToHashSet();
        var duplicate = request.BindingExercises
            .GroupBy(item => item.Exercise)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            return (duplicate.First(), "duplicate binding-exercise identity");
        }

        foreach (var exercise in request.BindingExercises.OrderBy(item => item.Exercise.Value, StringComparer.Ordinal))
        {
            if (!members.Contains(exercise.Consumer) || !members.Contains(exercise.Provider))
            {
                return (exercise, "binding exercise names an occurrence outside the CM3 plan");
            }

            if ((exercise.Exposure == BindingExposureKind.Distinct && exercise.Mediation is not null)
                || (exercise.Exposure == BindingExposureKind.Mediated && exercise.Mediation is null))
            {
                return (exercise, "binding exposure and Mediation identity conflict");
            }

            if (!exercise.AuthorityAdmitted && exercise.Delivery == BindingDeliveryResult.Delivered)
            {
                return (exercise, "delivery cannot succeed when the external authority check denied it");
            }

            if (exercise.Delivery == BindingDeliveryResult.Failed && string.IsNullOrWhiteSpace(exercise.Failure))
            {
                return (exercise, "failed delivery requires an attributable failure");
            }
        }

        return null;
    }

    private static bool ValidPlan(ActivationGroupPlan plan) =>
        plan.Effects == Cm3EffectObservation.None
        && plan.Groups.All(group =>
        {
            var expected = group.Protocols.Count == 0
                ? new[]
                {
                    ActivationStage.LocalInitialisation,
                    ActivationStage.Interconnection,
                    ActivationStage.Ready,
                }
                : new[]
                {
                    ActivationStage.LocalInitialisation,
                    ActivationStage.Interconnection,
                    ActivationStage.RelationalInitialisation,
                    ActivationStage.Ready,
                };
            return group.ReleasePending
                && group.Stages.Select(stage => stage.Stage).SequenceEqual(expected)
                && group.Stages.All(stage => !stage.OrdinaryGateOpen);
        });

    private static IEnumerable<MemberStageOutcome> OrderedStageOutcomes(ActivationRuntimeRequest request) =>
        request.Plan.Groups.SelectMany(group =>
            group.Stages.SelectMany(stage =>
                request.StageOutcomes
                    .Where(item => item.Group == group.Group && item.Stage == stage.Stage)
                    .OrderBy(item => item.Member.Value, StringComparer.Ordinal)));

    private static BindingExerciseObservation ObserveBinding(BindingExerciseDeclaration item) =>
        new(
            item.Exercise,
            item.Binding,
            item.Consumer,
            item.Provider,
            item.Source,
            item.Exposure,
            item.Mediation,
            item.Routing,
            item.AuthorityAdmitted,
            item.Delivery,
            item.Failure);

    private static ActivationRuntimeEvent Event(
        IReadOnlyCollection<ActivationRuntimeEvent> events,
        string kind,
        ActivationGroupId? group = null,
        ActivationStage? stage = null,
        OccurrenceId? member = null,
        string detail = "") =>
        new(events.Count + 1, kind, group, stage, member, detail);

    private static ReadOnlyCollection<RuntimeScopeObservation> ReplaceTargetScope(
        ActivationRuntimeRequest request,
        GenerationId generation,
        RuntimeScopeStatus status) =>
        ReadOnly(request.ActiveScopes
            .Select(item => item.Scope == request.Plan.RestartScope
                ? new RuntimeScopeObservation(item.Scope, generation, status)
                : new RuntimeScopeObservation(item.Scope, item.Generation, item.Status))
            .OrderBy(item => item.Scope.Value, StringComparer.Ordinal));

    private static ReadOnlyCollection<RuntimeScopeObservation> ScopeObservations(
        IEnumerable<ActiveScopeSnapshot> scopes) =>
        ReadOnly(scopes
            .Select(item => new RuntimeScopeObservation(item.Scope, item.Generation, item.Status))
            .OrderBy(item => item.Scope.Value, StringComparer.Ordinal));

    private static ActivationRuntimeOutcome Outcome(
        ActivationRuntimeRequest request,
        ActivationRuntimeOutcomeKind kind,
        IEnumerable<ActivationRuntimeEvent> events,
        IEnumerable<RuntimeInteractionDecision> interactions,
        IEnumerable<BindingExerciseObservation> bindings,
        IEnumerable<RuntimeScopeObservation> scopes,
        ActivationRuntimeEffects effects,
        ActivationRuntimeFailure? failure) =>
        new(
            kind,
            new(
                request.Attempt,
                request.Plan.Generation,
                request.RetainedGeneration,
                request.Plan.RestartScope,
                request.Preparation,
                request.Release,
                request.RetainedDisposition,
                ReadOnly(events),
                ReadOnly(interactions.OrderBy(item => item.Interaction.Value, StringComparer.Ordinal)),
                ReadOnly(bindings.OrderBy(item => item.Exercise.Value, StringComparer.Ordinal)),
                ReadOnly(scopes.OrderBy(item => item.Scope.Value, StringComparer.Ordinal)),
                request.Child,
                effects),
            failure);

    private static ReadOnlyCollection<T> ReadOnly<T>(IEnumerable<T> values) =>
        Array.AsReadOnly(values.ToArray());

    private static ActivationRuntimeRequest Snapshot(ActivationRuntimeRequest request) =>
        request with
        {
            Plan = SnapshotPlan(request.Plan),
            ActiveScopes = request.ActiveScopes.ToArray(),
            Preparation = request.Preparation is null
                ? null
                : request.Preparation with { Steps = ReadOnly(request.Preparation.Steps) },
            StageOutcomes = request.StageOutcomes.ToArray(),
            InteractionAttempts = request.InteractionAttempts.ToArray(),
            BindingExercises = request.BindingExercises.ToArray(),
        };

    private static ActivationGroupPlan SnapshotPlan(ActivationGroupPlan plan) =>
        plan with
        {
            Groups = ReadOnly(plan.Groups.Select(group => group with
            {
                Members = ReadOnly(group.Members.Select(member => member with
                {
                    Provides = ReadOnly(member.Provides),
                    RequiredReadyInputs = ReadOnly(member.RequiredReadyInputs),
                    AvailableReadyInputs = ReadOnly(member.AvailableReadyInputs),
                    WaitsForReadyOf = ReadOnly(member.WaitsForReadyOf),
                })),
                InternalEdges = ReadOnly(group.InternalEdges),
                Protocols = ReadOnly(group.Protocols.Select(protocol =>
                    protocol with { Authority = ReadOnly(protocol.Authority) })),
                RegionCrossings = ReadOnly(group.RegionCrossings),
                Stages = ReadOnly(group.Stages),
            })),
            InterGroupEdges = ReadOnly(plan.InterGroupEdges),
            RegionCrossings = ReadOnly(plan.RegionCrossings),
            Decisions = ReadOnly(plan.Decisions),
        };
}
