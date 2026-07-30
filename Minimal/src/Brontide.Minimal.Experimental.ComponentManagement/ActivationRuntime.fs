namespace Brontide.Minimal.Experimental.ComponentManagement

open System

type PreparationStepKind =
    | ValidateArtifact
    | BuildImage
    | WarmCache
    | PrepareStateSnapshot

type RuntimeInteractionPhase =
    | LocalInitialisation
    | Interconnection
    | RelationalInitialisation
    | Ready
    | Active

type RuntimeInteractionKind =
    | Lifecycle
    | Ordinary

type BindingExposureKind =
    | Distinct
    | Mediated

type BindingDeliveryResult =
    | Delivered
    | Denied
    | Failed

type ReleaseFailureMoment =
    | NoReleaseFailure
    | BeforeCutover
    | AfterCutover

[<RequireQualifiedAccess>]
type RollbackAvailability =
    | Available
    | Unavailable
    | RetainedGenerationCorrupted

type RetainedGenerationDisposition =
    | TerminateAfterRelease
    | RetainForRollback

type RuntimeScopeStatus =
    | ActiveScope
    | InactiveScope
    | DegradedScope

[<RequireQualifiedAccess>]
type ActivationRuntimeOutcomeKind =
    | Active
    | RolledBack
    | PreparationFailed
    | EstablishmentFailed
    | ReleaseFailedBeforeCutover
    | RollbackUnavailable
    | RetainedGenerationCorrupted
    | InvalidCm3Plan
    | RestartScopeConflict
    | StageObservationConflict
    | InteractionRefused
    | BindingObservationConflict
    | ChildPortClosed
    | ReplacementLifecycleRequired
    | HostAssistedOrderConflict

type PreparationDeclaration =
    { Preparation: PreparationId
      Steps: PreparationStepKind list
      Succeeds: bool }

type ActiveScopeSnapshot =
    { Scope: RestartScopeId
      Generation: GenerationId
      Status: RuntimeScopeStatus }

type MemberStageOutcome =
    { Group: ActivationGroupId
      Member: OccurrenceId
      Stage: ActivationStage
      Succeeded: bool
      Detail: string }

type RuntimeInteractionAttempt =
    { Interaction: RuntimeInteractionId
      Group: ActivationGroupId
      From: OccurrenceId
      To: OccurrenceId
      Phase: RuntimeInteractionPhase
      Kind: RuntimeInteractionKind
      Edge: ActivationEdgeId
      Operation: LifecycleOperationId option
      Capability: CapabilityId option
      InputShape: ShapeId option }

type BindingExerciseDeclaration =
    { Exercise: BindingExerciseId
      Binding: BindingId
      Consumer: OccurrenceId
      Provider: OccurrenceId
      Source: SourceId
      Exposure: BindingExposureKind
      Mediation: MediationId option
      Routing: RoutingDecisionId
      AuthorityAdmitted: bool
      Delivery: BindingDeliveryResult
      Failure: string option }

type ReleaseDeclaration =
    { Release: ReleaseId
      FailureMoment: ReleaseFailureMoment }

type ChildActivationDeclaration =
    { ParentScope: RestartScopeId
      ParentGeneration: GenerationId
      Port: PortId
      RuntimeOpen: bool
      Occupied: bool
      ReplacementLifecycleDeclared: bool
      HostAssisted: bool
      InternalReleaseSequence: int
      ExportReleaseSequence: int
      OuterHostOwnsAdmission: bool }

type ActivationRuntimeRequest =
    { Attempt: ActivationAttemptId
      Plan: ActivationGroupPlan
      RequestedRestartScope: RestartScopeId
      RetainedGeneration: GenerationId
      ActiveScopes: ActiveScopeSnapshot list
      Preparation: PreparationDeclaration option
      StageOutcomes: MemberStageOutcome list
      InteractionAttempts: RuntimeInteractionAttempt list
      BindingExercises: BindingExerciseDeclaration list
      Release: ReleaseDeclaration
      Rollback: RollbackAvailability
      RetainedDisposition: RetainedGenerationDisposition
      Child: ChildActivationDeclaration option }

type ActivationRuntimeEffects =
    { Prepared: bool
      EstablishmentStarted: bool
      ActorEndpointEstablished: bool
      LifecycleOperationExecuted: bool
      MemberReportedReady: bool
      Released: bool
      OrdinaryInteractionAdmitted: bool
      ActiveGenerationMutated: bool
      RetainedGenerationRetired: bool
      RollbackAttempted: bool
      CapabilityGranted: bool }

[<RequireQualifiedAccess>]
module ActivationRuntimeEffects =
    let none =
        { Prepared = false
          EstablishmentStarted = false
          ActorEndpointEstablished = false
          LifecycleOperationExecuted = false
          MemberReportedReady = false
          Released = false
          OrdinaryInteractionAdmitted = false
          ActiveGenerationMutated = false
          RetainedGenerationRetired = false
          RollbackAttempted = false
          CapabilityGranted = false }

type RuntimeInteractionDecision =
    { Interaction: RuntimeInteractionId
      Admitted: bool
      Reason: string }

type BindingExerciseObservation =
    { Exercise: BindingExerciseId
      Binding: BindingId
      Consumer: OccurrenceId
      Provider: OccurrenceId
      Source: SourceId
      Exposure: BindingExposureKind
      Mediation: MediationId option
      Routing: RoutingDecisionId
      AuthorityAdmitted: bool
      Delivery: BindingDeliveryResult
      Failure: string option }

type RuntimeScopeObservation =
    { Scope: RestartScopeId
      Generation: GenerationId option
      Status: RuntimeScopeStatus }

type ActivationRuntimeEvent =
    { Sequence: int
      Kind: string
      Group: ActivationGroupId option
      Stage: ActivationStage option
      Member: OccurrenceId option
      Detail: string }

type ActivationRuntimeFailure =
    { Kind: ActivationRuntimeOutcomeKind
      Reason: string
      Group: ActivationGroupId option
      Stage: ActivationStage option
      Member: OccurrenceId option
      Interaction: RuntimeInteractionId option
      Exercise: BindingExerciseId option
      Port: PortId option }

type ActivationRuntimeObservation =
    { Attempt: ActivationAttemptId
      TargetGeneration: GenerationId
      RetainedGeneration: GenerationId
      RestartScope: RestartScopeId
      Preparation: PreparationDeclaration option
      Release: ReleaseDeclaration
      RetainedDisposition: RetainedGenerationDisposition
      Events: ActivationRuntimeEvent list
      Interactions: RuntimeInteractionDecision list
      BindingExercises: BindingExerciseObservation list
      Scopes: RuntimeScopeObservation list
      Child: ChildActivationDeclaration option
      Effects: ActivationRuntimeEffects }

type ActivationRuntimeOutcome =
    { Kind: ActivationRuntimeOutcomeKind
      Observation: ActivationRuntimeObservation
      Failure: ActivationRuntimeFailure option }

[<RequireQualifiedAccess>]
module FakeActivationRuntime =
    let private scopeObservations (scopes: ActiveScopeSnapshot list) : RuntimeScopeObservation list =
        scopes
        |> List.map (fun item ->
            { Scope = item.Scope
              Generation = Some item.Generation
              Status = item.Status })
        |> List.sortBy (fun item -> RestartScopeId.value item.Scope)

    let private replaceTarget
        (request: ActivationRuntimeRequest)
        (generation: GenerationId)
        (status: RuntimeScopeStatus)
        : RuntimeScopeObservation list =
        request.ActiveScopes
        |> List.map (fun item ->
            if item.Scope = request.Plan.RestartScope then
                { Scope = item.Scope
                  Generation = Some generation
                  Status = status }
            else
                { Scope = item.Scope
                  Generation = Some item.Generation
                  Status = item.Status })
        |> List.sortBy (fun item -> RestartScopeId.value item.Scope)

    let private outcome
        (request: ActivationRuntimeRequest)
        (kind: ActivationRuntimeOutcomeKind)
        (events: seq<ActivationRuntimeEvent>)
        (interactions: seq<RuntimeInteractionDecision>)
        (bindings: seq<BindingExerciseObservation>)
        (scopes: RuntimeScopeObservation list)
        (effects: ActivationRuntimeEffects)
        (failureValue: ActivationRuntimeFailure option)
        : ActivationRuntimeOutcome =
        { Kind = kind
          Observation =
            { Attempt = request.Attempt
              TargetGeneration = request.Plan.Generation
              RetainedGeneration = request.RetainedGeneration
              RestartScope = request.Plan.RestartScope
              Preparation = request.Preparation
              Release = request.Release
              RetainedDisposition = request.RetainedDisposition
              Events = List.ofSeq events
              Interactions =
                interactions
                |> List.ofSeq
                |> List.sortBy (fun item -> RuntimeInteractionId.value item.Interaction)
              BindingExercises =
                bindings
                |> List.ofSeq
                |> List.sortBy (fun item -> BindingExerciseId.value item.Exercise)
              Scopes = scopes |> List.sortBy (fun item -> RestartScopeId.value item.Scope)
              Child = request.Child
              Effects = effects }
          Failure = failureValue }

    let private failure
        (kind: ActivationRuntimeOutcomeKind)
        reason
        group
        stage
        memberValue
        interaction
        exercise
        port
        : ActivationRuntimeFailure =
        { Kind = kind
          Reason = reason
          Group = group
          Stage = stage
          Member = memberValue
          Interaction = interaction
          Exercise = exercise
          Port = port }

    let private addEvent
        (events: ResizeArray<ActivationRuntimeEvent>)
        kind
        group
        stage
        memberValue
        detail
        =
        events.Add
            { Sequence = events.Count + 1
              Kind = kind
              Group = group
              Stage = stage
              Member = memberValue
              Detail = detail }

    let private validPlan (plan: ActivationGroupPlan) =
        plan.Effects = Cm3EffectObservation.none
        && (plan.Groups
            |> List.forall (fun group ->
                let expected =
                    if List.isEmpty group.Protocols then
                        [ ActivationStage.LocalInitialisation
                          ActivationStage.Interconnection
                          ActivationStage.Ready ]
                    else
                        [ ActivationStage.LocalInitialisation
                          ActivationStage.Interconnection
                          ActivationStage.RelationalInitialisationStage
                          ActivationStage.Ready ]
                group.ReleasePending
                && (group.Stages |> List.map (fun stage -> stage.Stage)) = expected
                && (group.Stages |> List.forall (fun stage -> not stage.OrdinaryGateOpen))))

    let private validateChild (request: ActivationRuntimeRequest) =
        match request.Child with
        | None -> Ok()
        | Some child ->
            let parents =
                request.ActiveScopes
                |> List.filter (fun item -> item.Scope = child.ParentScope)
            match parents with
            | [ parent ] when
                parent.Status = RuntimeScopeStatus.ActiveScope
                && parent.Generation = child.ParentGeneration
                && child.ParentScope <> request.Plan.RestartScope ->
                if not child.RuntimeOpen then
                    Error(ActivationRuntimeOutcomeKind.ChildPortClosed, "child Port is not runtime-open")
                elif child.Occupied && not child.ReplacementLifecycleDeclared then
                    Error(
                        ActivationRuntimeOutcomeKind.ReplacementLifecycleRequired,
                        "occupied Port replacement requires lifecycle declarations"
                    )
                elif
                    child.HostAssisted
                    && (child.InternalReleaseSequence <= 0
                        || child.ExportReleaseSequence <= child.InternalReleaseSequence)
                then
                    Error(
                        ActivationRuntimeOutcomeKind.HostAssistedOrderConflict,
                        "host-assisted export must follow internal child Release"
                    )
                else
                    Ok()
            | _ ->
                Error(
                    ActivationRuntimeOutcomeKind.RestartScopeConflict,
                    "child activation requires a distinct active parent scope"
                )

    let private validateStages (request: ActivationRuntimeRequest) =
        let expected =
            request.Plan.Groups
            |> List.collect (fun group ->
                group.Stages
                |> List.collect (fun stage ->
                    group.Members
                    |> List.map (fun memberValue -> group.Group, memberValue.Occurrence, stage.Stage)))
        let duplicate =
            request.StageOutcomes
            |> List.groupBy (fun item -> item.Group, item.Member, item.Stage)
            |> List.tryFind (fun (_, values) -> List.length values > 1)
        match duplicate with
        | Some((group, memberValue, stage), _) ->
            Error("duplicate member-stage outcome", Some group, Some stage, Some memberValue)
        | None ->
            match
                request.StageOutcomes
                |> List.tryFind (fun item ->
                    not (List.contains (item.Group, item.Member, item.Stage) expected))
            with
            | Some item ->
                Error(
                    "outcome names a member, group, or stage outside the CM3 plan",
                    Some item.Group,
                    Some item.Stage,
                    Some item.Member
                )
            | None ->
                match
                    expected
                    |> List.tryFind (fun (group, memberValue, stage) ->
                        request.StageOutcomes
                        |> List.exists (fun item ->
                            item.Group = group && item.Member = memberValue && item.Stage = stage)
                        |> not)
                with
                | Some(group, memberValue, stage) ->
                    Error("missing member-stage outcome", Some group, Some stage, Some memberValue)
                | None -> Ok()

    let private validateInteractions (request: ActivationRuntimeRequest) =
        let duplicate =
            request.InteractionAttempts
            |> List.groupBy (fun item -> item.Interaction)
            |> List.tryFind (fun (_, values) -> List.length values > 1)
        match duplicate with
        | Some(_, first :: _) -> Error(first, "duplicate interaction identity")
        | Some(_, []) -> failwith "groupBy cannot produce an empty group"
        | None ->
            request.InteractionAttempts
            |> List.sortBy (fun item -> RuntimeInteractionId.value item.Interaction)
            |> List.tryPick (fun attempt ->
                match request.Plan.Groups |> List.tryFind (fun item -> item.Group = attempt.Group) with
                | None -> Some(Error(attempt, "interaction names a group outside the CM3 plan"))
                | Some group ->
                    let fromKnown =
                        group.Members |> List.exists (fun item -> item.Occurrence = attempt.From)
                    let toKnown =
                        request.Plan.Groups
                        |> List.collect (fun item -> item.Members)
                        |> List.exists (fun item -> item.Occurrence = attempt.To)
                    if not fromKnown || not toKnown then
                        Some(Error(attempt, "interaction names a member outside the CM3 plan"))
                    elif attempt.Kind = RuntimeInteractionKind.Ordinary then
                        if attempt.Phase <> RuntimeInteractionPhase.Active then
                            Some(Error(attempt, "ordinary interaction is closed before Release"))
                        else
                            let toGroup =
                                request.Plan.Groups
                                |> List.find (fun candidate ->
                                    candidate.Members
                                    |> List.exists (fun item -> item.Occurrence = attempt.To))
                            let declaredInternal =
                                group.InternalEdges
                                |> List.exists (fun candidate ->
                                    candidate.Edge = attempt.Edge
                                    && candidate.From = attempt.From
                                    && candidate.To = attempt.To
                                    && candidate.Kind =
                                        ActivationDependencyKind.OrdinaryInteraction)
                            let declaredInterGroup =
                                request.Plan.InterGroupEdges
                                |> List.exists (fun candidate ->
                                    candidate.Edge = attempt.Edge
                                    && candidate.FromGroup = group.Group
                                    && candidate.ToGroup = toGroup.Group)
                            if declaredInternal || declaredInterGroup then None
                            else
                                Some(
                                    Error(
                                        attempt,
                                        "ordinary interaction does not match a declared CM3 edge"
                                    )
                                )
                    elif attempt.Phase <> RuntimeInteractionPhase.RelationalInitialisation then
                        Some(
                            Error(
                                attempt,
                                "lifecycle interaction is admitted only during Relational Initialisation"
                            )
                        )
                    else
                        let matching =
                            group.Protocols
                            |> List.filter (fun protocol ->
                                protocol.Edge = attempt.Edge
                                && protocol.From = attempt.From
                                && protocol.To = attempt.To
                                && Some protocol.Operation = attempt.Operation
                                && Some protocol.InputShape = attempt.InputShape
                                && (attempt.Capability
                                    |> Option.exists (fun capability ->
                                        List.contains capability protocol.Authority)))
                        if List.length matching = 1 then None
                        else
                            Some(
                                Error(
                                    attempt,
                                    "lifecycle interaction does not match one declared bounded protocol"
                                )
                            ))
            |> Option.defaultValue (Ok())

    let private validateBindings (request: ActivationRuntimeRequest) =
        let members =
            request.Plan.Groups
            |> List.collect (fun item -> item.Members)
            |> List.map (fun item -> item.Occurrence)
            |> Set.ofList
        let duplicate =
            request.BindingExercises
            |> List.groupBy (fun item -> item.Exercise)
            |> List.tryFind (fun (_, values) -> List.length values > 1)
        match duplicate with
        | Some(_, first :: _) -> Error(first, "duplicate binding-exercise identity")
        | Some(_, []) -> failwith "groupBy cannot produce an empty group"
        | None ->
            request.BindingExercises
            |> List.sortBy (fun item -> BindingExerciseId.value item.Exercise)
            |> List.tryPick (fun exercise ->
                if not (Set.contains exercise.Consumer members)
                   || not (Set.contains exercise.Provider members) then
                    Some(Error(exercise, "binding exercise names an occurrence outside the CM3 plan"))
                elif
                    (exercise.Exposure = BindingExposureKind.Distinct
                     && Option.isSome exercise.Mediation)
                    || (exercise.Exposure = BindingExposureKind.Mediated
                        && Option.isNone exercise.Mediation)
                then
                    Some(Error(exercise, "binding exposure and Mediation identity conflict"))
                elif
                    not exercise.AuthorityAdmitted
                    && exercise.Delivery = BindingDeliveryResult.Delivered
                then
                    Some(
                        Error(
                            exercise,
                            "delivery cannot succeed when the external authority check denied it"
                        )
                    )
                elif
                    exercise.Delivery = BindingDeliveryResult.Failed
                    && (exercise.Failure
                        |> Option.forall String.IsNullOrWhiteSpace)
                then
                    Some(Error(exercise, "failed delivery requires an attributable failure"))
                else
                    None)
            |> Option.defaultValue (Ok())

    let private orderedStageGroups (request: ActivationRuntimeRequest) =
        request.Plan.Groups
        |> List.collect (fun group ->
            group.Stages
            |> List.map (fun stage ->
                let outcomes =
                    request.StageOutcomes
                    |> List.filter (fun item ->
                        item.Group = group.Group && item.Stage = stage.Stage)
                    |> List.sortBy (fun item -> OccurrenceId.value item.Member)
                group.Group, stage.Stage, outcomes))

    let private observeBinding (item: BindingExerciseDeclaration) : BindingExerciseObservation =
        { Exercise = item.Exercise
          Binding = item.Binding
          Consumer = item.Consumer
          Provider = item.Provider
          Source = item.Source
          Exposure = item.Exposure
          Mediation = item.Mediation
          Routing = item.Routing
          AuthorityAdmitted = item.AuthorityAdmitted
          Delivery = item.Delivery
          Failure = item.Failure }

    let activate (input: ActivationRuntimeRequest) =
        let request =
            { input with
                ActiveScopes = List.ofSeq input.ActiveScopes
                Preparation =
                    input.Preparation
                    |> Option.map (fun item -> { item with Steps = List.ofSeq item.Steps })
                StageOutcomes = List.ofSeq input.StageOutcomes
                InteractionAttempts = List.ofSeq input.InteractionAttempts
                BindingExercises = List.ofSeq input.BindingExercises }
        let events = ResizeArray<ActivationRuntimeEvent>()
        let interactions = ResizeArray<RuntimeInteractionDecision>()
        let bindings = ResizeArray<BindingExerciseObservation>()
        let unchanged = scopeObservations request.ActiveScopes
        let mutable effects = ActivationRuntimeEffects.none
        let refuse kind reason group stage memberValue interaction exercise port =
            outcome
                request
                kind
                events
                interactions
                bindings
                unchanged
                effects
                (Some(failure kind reason group stage memberValue interaction exercise port))

        if not (validPlan request.Plan) then
            refuse
                ActivationRuntimeOutcomeKind.InvalidCm3Plan
                "CM3 plan is not a closed-gate Release-pending plan"
                None None None None None None
        elif request.RequestedRestartScope <> request.Plan.RestartScope then
            refuse
                ActivationRuntimeOutcomeKind.RestartScopeConflict
                "requested restart scope differs from the CM3 plan"
                None None None None None None
        elif request.Plan.Generation = request.RetainedGeneration then
            refuse
                ActivationRuntimeOutcomeKind.RestartScopeConflict
                "target generation must differ from the retained generation"
                None None None None None None
        else
            let duplicateScope =
                request.ActiveScopes
                |> List.groupBy (fun item -> item.Scope)
                |> List.tryFind (fun (_, values) -> List.length values > 1)
            let targetScopes =
                request.ActiveScopes
                |> List.filter (fun item -> item.Scope = request.Plan.RestartScope)
            match duplicateScope, targetScopes with
            | Some _, _
            | _, [ { Status = RuntimeScopeStatus.ActiveScope } ] when
                duplicateScope |> Option.isSome ->
                refuse
                    ActivationRuntimeOutcomeKind.RestartScopeConflict
                    "duplicate active scope"
                    None None None None None None
            | None, [ target ] when
                target.Status = RuntimeScopeStatus.ActiveScope
                && target.Generation = request.RetainedGeneration ->
                match validateChild request with
                | Error(kind, reason) ->
                    refuse kind reason None None None None None (request.Child |> Option.map (fun item -> item.Port))
                | Ok() ->
                    match validateStages request with
                    | Error(reason, group, stage, memberValue) ->
                        refuse
                            ActivationRuntimeOutcomeKind.StageObservationConflict
                            reason group stage memberValue None None None
                    | Ok() ->
                        match validateInteractions request with
                        | Error(attempt, reason) ->
                            interactions.Add
                                { Interaction = attempt.Interaction
                                  Admitted = false
                                  Reason = reason }
                            refuse
                                ActivationRuntimeOutcomeKind.InteractionRefused
                                reason
                                (Some attempt.Group)
                                None
                                (Some attempt.From)
                                (Some attempt.Interaction)
                                None
                                None
                        | Ok() ->
                            match validateBindings request with
                            | Error(exercise, reason) ->
                                refuse
                                    ActivationRuntimeOutcomeKind.BindingObservationConflict
                                    reason
                                    None
                                    None
                                    (Some exercise.Consumer)
                                    None
                                    (Some exercise.Exercise)
                                    None
                            | Ok() ->
                                let preparationFailed =
                                    match request.Preparation with
                                    | Some preparation ->
                                        addEvent
                                            events
                                            "preparation"
                                            None
                                            None
                                            None
                                            (if preparation.Succeeds then "completed" else "failed")
                                        if preparation.Succeeds then
                                            effects <- { effects with Prepared = true }
                                            false
                                        else
                                            true
                                    | None -> false
                                if preparationFailed then
                                    outcome
                                        request
                                        ActivationRuntimeOutcomeKind.PreparationFailed
                                        events
                                        interactions
                                        bindings
                                        unchanged
                                        ActivationRuntimeEffects.none
                                        (Some(
                                            failure
                                                ActivationRuntimeOutcomeKind.PreparationFailed
                                                "optional preparation failed before establishment"
                                                None None None None None None
                                        ))
                                else
                                    effects <- { effects with EstablishmentStarted = true }
                                    addEvent
                                        events
                                        "establishment-started"
                                        None
                                        None
                                        None
                                        (RestartScopeId.value request.Plan.RestartScope)
                                    let mutable stageFailure: MemberStageOutcome option = None
                                    for (group, stage, stageOutcomes) in orderedStageGroups request do
                                        if Option.isNone stageFailure then
                                            match stageOutcomes |> List.tryFind (fun item -> not item.Succeeded) with
                                            | Some failed ->
                                                stageFailure <- Some failed
                                                addEvent
                                                    events
                                                    "stage-failed"
                                                    (Some failed.Group)
                                                    (Some failed.Stage)
                                                    (Some failed.Member)
                                                    failed.Detail
                                            | None ->
                                                addEvent
                                                    events
                                                    "stage-completed"
                                                    (Some group)
                                                    (Some stage)
                                                    None
                                                    "all members"
                                                match stage with
                                                | ActivationStage.Interconnection ->
                                                    effects <-
                                                        { effects with
                                                            ActorEndpointEstablished = true }
                                                | ActivationStage.RelationalInitialisationStage ->
                                                    effects <-
                                                        { effects with
                                                            LifecycleOperationExecuted =
                                                                request.InteractionAttempts
                                                                |> List.exists (fun item ->
                                                                    item.Group = group
                                                                    && item.Phase =
                                                                        RuntimeInteractionPhase.RelationalInitialisation) }
                                                | ActivationStage.Ready ->
                                                    effects <-
                                                        { effects with MemberReportedReady = true }
                                                | _ -> ()
                                    match stageFailure with
                                    | Some failed ->
                                        outcome
                                            request
                                            ActivationRuntimeOutcomeKind.EstablishmentFailed
                                            events
                                            interactions
                                            bindings
                                            unchanged
                                            effects
                                            (Some(
                                                failure
                                                    ActivationRuntimeOutcomeKind.EstablishmentFailed
                                                    failed.Detail
                                                    (Some failed.Group)
                                                    (Some failed.Stage)
                                                    (Some failed.Member)
                                                    None None None
                                            ))
                                    | None ->
                                        request.InteractionAttempts
                                        |> List.filter (fun item ->
                                            item.Phase =
                                                RuntimeInteractionPhase.RelationalInitialisation)
                                        |> List.sortBy (fun item ->
                                            RuntimeInteractionId.value item.Interaction)
                                        |> List.iter (fun item ->
                                            interactions.Add
                                                { Interaction = item.Interaction
                                                  Admitted = true
                                                  Reason = "declared lifecycle protocol" })
                                        if request.Release.FailureMoment =
                                           ReleaseFailureMoment.BeforeCutover then
                                            addEvent events "release-failed" None None None "before cutover"
                                            outcome
                                                request
                                                ActivationRuntimeOutcomeKind.ReleaseFailedBeforeCutover
                                                events
                                                interactions
                                                bindings
                                                unchanged
                                                effects
                                                (Some(
                                                    failure
                                                        ActivationRuntimeOutcomeKind.ReleaseFailedBeforeCutover
                                                        "release failed before cutover; retained generation remains active"
                                                        None None None None None None
                                                ))
                                        else
                                            addEvent
                                                events
                                                "release"
                                                None
                                                None
                                                None
                                                (ReleaseId.value request.Release.Release)
                                            addEvent
                                                events
                                                "cutover"
                                                None
                                                None
                                                None
                                                (GenerationId.value request.Plan.Generation)
                                            effects <-
                                                { effects with
                                                    Released = true
                                                    ActiveGenerationMutated = true }
                                            if request.Release.FailureMoment =
                                               ReleaseFailureMoment.AfterCutover then
                                                match request.Rollback with
                                                | RollbackAvailability.Available ->
                                                    effects <-
                                                        { effects with RollbackAttempted = true }
                                                    addEvent
                                                        events
                                                        "rollback-restored"
                                                        None
                                                        None
                                                        None
                                                        (GenerationId.value request.RetainedGeneration)
                                                    outcome
                                                        request
                                                        ActivationRuntimeOutcomeKind.RolledBack
                                                        events
                                                        interactions
                                                        bindings
                                                        unchanged
                                                        effects
                                                        (Some(
                                                            failure
                                                                ActivationRuntimeOutcomeKind.RolledBack
                                                                "post-cutover failure restored the retained generation"
                                                                None None None None None None
                                                        ))
                                                | RollbackAvailability.Unavailable ->
                                                    addEvent
                                                        events
                                                        "activation-degraded"
                                                        None
                                                        None
                                                        None
                                                        "rollback unavailable after cutover"
                                                    outcome
                                                        request
                                                        ActivationRuntimeOutcomeKind.RollbackUnavailable
                                                        events
                                                        interactions
                                                        bindings
                                                        (replaceTarget
                                                            request
                                                            request.Plan.Generation
                                                            RuntimeScopeStatus.DegradedScope)
                                                        effects
                                                        (Some(
                                                            failure
                                                                ActivationRuntimeOutcomeKind.RollbackUnavailable
                                                                "rollback unavailable after cutover"
                                                                None None None None None None
                                                        ))
                                                | RollbackAvailability.RetainedGenerationCorrupted ->
                                                    effects <-
                                                        { effects with RollbackAttempted = true }
                                                    addEvent
                                                        events
                                                        "activation-degraded"
                                                        None
                                                        None
                                                        None
                                                        "retained generation is corrupted and cannot be restored"
                                                    outcome
                                                        request
                                                        ActivationRuntimeOutcomeKind.RetainedGenerationCorrupted
                                                        events
                                                        interactions
                                                        bindings
                                                        (replaceTarget
                                                            request
                                                            request.Plan.Generation
                                                            RuntimeScopeStatus.DegradedScope)
                                                        effects
                                                        (Some(
                                                            failure
                                                                ActivationRuntimeOutcomeKind.RetainedGenerationCorrupted
                                                                "retained generation is corrupted and cannot be restored"
                                                                None None None None None None
                                                        ))
                                            else
                                                request.InteractionAttempts
                                                |> List.filter (fun item ->
                                                    item.Phase = RuntimeInteractionPhase.Active)
                                                |> List.sortBy (fun item ->
                                                    RuntimeInteractionId.value item.Interaction)
                                                |> List.iter (fun item ->
                                                    interactions.Add
                                                        { Interaction = item.Interaction
                                                          Admitted = true
                                                          Reason = "ordinary gate released" })
                                                request.BindingExercises
                                                |> List.sortBy (fun item ->
                                                    BindingExerciseId.value item.Exercise)
                                                |> List.map observeBinding
                                                |> List.iter bindings.Add
                                                effects <-
                                                    { effects with
                                                        OrdinaryInteractionAdmitted =
                                                            (interactions
                                                             |> Seq.exists (fun item ->
                                                                 item.Admitted
                                                                 && item.Reason =
                                                                     "ordinary gate released"))
                                                            || (bindings
                                                                |> Seq.exists (fun item ->
                                                                    item.Delivery =
                                                                        BindingDeliveryResult.Delivered))
                                                        RetainedGenerationRetired =
                                                            request.RetainedDisposition =
                                                                RetainedGenerationDisposition.TerminateAfterRelease }
                                                if effects.RetainedGenerationRetired then
                                                    addEvent
                                                        events
                                                        "retained-generation-terminated"
                                                        None
                                                        None
                                                        None
                                                        (GenerationId.value request.RetainedGeneration)
                                                match request.Child with
                                                | Some child ->
                                                    addEvent
                                                        events
                                                        "child-attached"
                                                        None
                                                        None
                                                        None
                                                        (PortId.value child.Port)
                                                    if child.HostAssisted then
                                                        addEvent
                                                            events
                                                            "outer-boundary-released"
                                                            None
                                                            None
                                                            None
                                                            (PortId.value child.Port)
                                                | None -> ()
                                                outcome
                                                    request
                                                    ActivationRuntimeOutcomeKind.Active
                                                    events
                                                    interactions
                                                    bindings
                                                    (replaceTarget
                                                        request
                                                        request.Plan.Generation
                                                        RuntimeScopeStatus.ActiveScope)
                                                    effects
                                                    None
            | _ ->
                refuse
                    ActivationRuntimeOutcomeKind.RestartScopeConflict
                    "target scope is missing, inactive, or carries a different retained generation"
                    None None None None None None
