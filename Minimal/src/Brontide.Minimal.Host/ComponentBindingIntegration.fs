namespace Brontide.Minimal.Host

open System
open System.Security.Cryptography
open System.Text
open System.Text.Json.Nodes
open Brontide.Minimal.Binding.Portable
open Brontide.Minimal.Experimental.ComponentManagement

[<RequireQualifiedAccess>]
type ComponentBindingIntegrationFailureKind =
    | ResolutionNotComplete
    | RequirementNotResolved
    | CardinalityUnsupported
    | ExposureUnsupported
    | MembershipUnsupported
    | BindingNotDirect
    | SelectionMismatch
    | MappingInvalid
    | PortableHandoffRefused

type ComponentBindingSelection =
    { Requirement: RequirementId
      Definition: DefinitionId
      Occurrence: OccurrenceId
      Component: PortableComponentRef
      Provider: PortableProviderRef
      HostEndpoint: string
      ProviderEndpoint: string
      RequiredContract: ContractDocument }

type ComponentBindingIntegrationFailure =
    { Kind: ComponentBindingIntegrationFailureKind
      Code: string
      Reason: string }

type ComponentBindingIntegrationResult =
    | Prepared of CompositionMember
    | Refused of ComponentBindingIntegrationFailure

/// Composition-root adapter from one completed CM2 provider position to PB7 preflight.
///
/// The adapter deliberately lives in Host: Component Management and Portable Binding remain
/// independent experiments, while the composition root connects their public seams.
[<RequireQualifiedAccess>]
module ComponentBindingIntegration =
    let private refuse kind code reason =
        Refused { Kind = kind; Code = code; Reason = reason }

    let private validEndpoint maximumTextBytes (value: string) =
        not (System.String.IsNullOrWhiteSpace value)
        && Encoding.UTF8.GetByteCount value <= maximumTextBytes

    let prepare resolution selection =
        match resolution with
        | ResolutionOutcome.WiderGenerationRequired _
        | ResolutionOutcome.Refused _ ->
            refuse
                ComponentBindingIntegrationFailureKind.ResolutionNotComplete
                "resolution-not-complete"
                "Portable preflight requires a completed CM2 generation."
        | ResolutionOutcome.Resolved(_, generation) ->
            let matches =
                generation.ProviderSets
                |> List.filter (fun item -> item.Requirement = selection.Requirement)
            match matches with
            | [ providerSet ] ->
                if providerSet.Cardinality.Minimum <> 1 || providerSet.Cardinality.Maximum <> Some 1 then
                    refuse
                        ComponentBindingIntegrationFailureKind.CardinalityUnsupported
                        "cardinality-unsupported"
                        (sprintf "CBI1 accepts only cardinality 1..1, not %O." providerSet.Cardinality)
                elif
                    providerSet.Exposure <> ProviderExposure.Distinct
                    || providerSet.Mediation.IsSome
                then
                    refuse
                        ComponentBindingIntegrationFailureKind.ExposureUnsupported
                        "exposure-unsupported"
                        "CBI1 accepts only distinct exposure without Mediation."
                elif providerSet.Members.Length <> 1 then
                    refuse
                        ComponentBindingIntegrationFailureKind.MembershipUnsupported
                        "membership-unsupported"
                        (sprintf
                            "A direct 1..1 position must have exactly one member, not %d."
                            providerSet.Members.Length)
                else
                    let memberValue = List.exactlyOne providerSet.Members
                    let direct =
                        providerSet.BindingPlans
                        |> List.filter (fun item ->
                            item.Member = memberValue.Occurrence
                            && item.Direct
                            && item.Mediation.IsNone)
                    if providerSet.BindingPlans.Length <> 1 || direct.Length <> 1 then
                        refuse
                            ComponentBindingIntegrationFailureKind.BindingNotDirect
                            "binding-not-direct"
                            "The resolved position does not contain exactly one direct binding observation for its member."
                    elif
                        memberValue.Definition <> selection.Definition
                        || memberValue.Occurrence <> selection.Occurrence
                    then
                        refuse
                            ComponentBindingIntegrationFailureKind.SelectionMismatch
                            "selection-mismatch"
                            "The explicit portable mapping does not name the definition and occurrence selected by CM2."
                    elif
                        not
                            (validEndpoint
                                selection.RequiredContract.Limits.MaxTextBytes
                                selection.HostEndpoint)
                        || not
                            (validEndpoint
                                selection.RequiredContract.Limits.MaxTextBytes
                                selection.ProviderEndpoint)
                    then
                        refuse
                            ComponentBindingIntegrationFailureKind.MappingInvalid
                            "endpoint-invalid"
                            (sprintf
                                "Endpoint designations must be non-empty UTF-8 text within the portable contract's %d-byte text bound."
                                selection.RequiredContract.Limits.MaxTextBytes)
                    else
                        match
                            Brontide.Minimal.Binding.Portable.BindingScopeId.tryCreate
                                (Brontide.Minimal.Experimental.ComponentManagement.BindingScopeId.value
                                    providerSet.Scope)
                        with
                        | Error(PortableError.Refused fault) ->
                            refuse
                                ComponentBindingIntegrationFailureKind.PortableHandoffRefused
                                fault.LocalCode
                                fault.Message
                        | Error(PortableError.Interrupted failure) ->
                            refuse
                                ComponentBindingIntegrationFailureKind.PortableHandoffRefused
                                "portable-process-interrupted"
                                failure.Message
                        | Ok scope ->
                            let requirement =
                                ResolvedRequirement.oneToOneProvider
                                    scope
                                    selection.Component
                                    selection.Provider
                                    selection.HostEndpoint
                            let provision =
                                { Component = selection.Component
                                  Provider = selection.Provider
                                  ProviderEndpoint = selection.ProviderEndpoint }
                            match
                                PortableCompositionHandoff.prepare
                                    requirement
                                    provision
                                    selection.RequiredContract
                            with
                            | Ok memberValue -> Prepared memberValue
                            | Error(PortableError.Refused fault) ->
                                refuse
                                    ComponentBindingIntegrationFailureKind.PortableHandoffRefused
                                    fault.LocalCode
                                    fault.Message
                            | Error(PortableError.Interrupted failure) ->
                                refuse
                                    ComponentBindingIntegrationFailureKind.PortableHandoffRefused
                                    "portable-process-interrupted"
                                    failure.Message
            | _ ->
                refuse
                    ComponentBindingIntegrationFailureKind.RequirementNotResolved
                    "requirement-not-resolved"
                    (sprintf
                        "The completed generation contains %d provider positions for the requested requirement."
                        matches.Length)

[<RequireQualifiedAccess>]
type ComponentBindingLifecycleFailureKind =
    | PreparationUnavailable
    | PlanUnsupported
    | RuntimeRefusedBeforeStart
    | PortableInterconnectionRefused
    | PortableReleaseRefused

type ComponentBindingLifecycleFailure =
    { Kind: ComponentBindingLifecycleFailureKind
      Code: string
      Reason: string }

type ComponentBindingLifecycleResult =
    { Runtime: ActivationRuntimeOutcome option
      Member: CompositionMember option
      Failure: ComponentBindingLifecycleFailure option }

[<RequireQualifiedAccess>]
module ComponentBindingLifecycle =
    let private refuse kind code reason runtime memberValue =
        { Runtime = runtime
          Member = memberValue
          Failure = Some { Kind = kind; Code = code; Reason = reason } }

    let private trySupportedGroup plan selectedOccurrence =
        match plan.Groups with
        | [ group ] when
            group.Members.Length = 1
            && (List.exactlyOne group.Members).Occurrence = selectedOccurrence
            && group.Protocols.IsEmpty ->
            Some group
        | _ -> None

    let private stageOutcomes group memberValue failedStage =
        group.Stages
        |> List.map (fun stage ->
            { Group = group.Group
              Member = memberValue
              Stage = stage.Stage
              Succeeded =
                match failedStage with
                | None -> true
                | Some ActivationStage.Interconnection ->
                    stage.Stage = ActivationStage.LocalInitialisation
                | Some ActivationStage.Ready ->
                    stage.Stage <> ActivationStage.Ready
                | Some _ -> false
              Detail =
                if Some stage.Stage = failedStage then
                    "portable stage failed"
                else
                    "derived from portable member" })

    let private portableError error =
        match error with
        | PortableError.Refused fault -> fault.LocalCode, fault.Message
        | PortableError.Interrupted failure -> "portable-process-interrupted", failure.Message

    let activate resolution selection request conversation =
        task {
            let preparation = ComponentBindingIntegration.prepare resolution selection
            match preparation with
            | ComponentBindingIntegrationResult.Refused _ ->
                return
                    refuse
                        ComponentBindingLifecycleFailureKind.PreparationUnavailable
                        "preparation-unavailable"
                        "CBI2 requires a successfully prepared CBI1 member."
                        None
                        None
            | ComponentBindingIntegrationResult.Prepared memberValue ->
                match trySupportedGroup request.Plan selection.Occurrence with
                | None ->
                    return
                        refuse
                            ComponentBindingLifecycleFailureKind.PlanUnsupported
                            "plan-unsupported"
                            "CBI2 supports exactly one protocol-free activation group containing only the selected occurrence."
                            None
                            (Some memberValue)
                | Some group ->
                    let successfulRequest =
                        { request with
                            StageOutcomes = stageOutcomes group selection.Occurrence None }
                    let preflight = FakeActivationRuntime.activate successfulRequest
                    if preflight.Kind <> ActivationRuntimeOutcomeKind.Active then
                        return
                            refuse
                                ComponentBindingLifecycleFailureKind.RuntimeRefusedBeforeStart
                                "runtime-refused-before-start"
                                (sprintf
                                    "CM4 refused the derived lifecycle before provider establishment: %A."
                                    preflight.Kind)
                                (Some preflight)
                                (Some memberValue)
                    else
                        let! interconnected = memberValue.Interconnect conversation
                        match interconnected with
                        | Error error ->
                            let failedRequest =
                                { request with
                                    StageOutcomes =
                                        stageOutcomes
                                            group
                                            selection.Occurrence
                                            (Some ActivationStage.Interconnection) }
                            let code, reason = portableError error
                            return
                                refuse
                                    ComponentBindingLifecycleFailureKind.PortableInterconnectionRefused
                                    code
                                    reason
                                    (Some(FakeActivationRuntime.activate failedRequest))
                                    (Some memberValue)
                        | Ok() when not memberValue.IsReady ->
                            let failedRequest =
                                { request with
                                    StageOutcomes =
                                        stageOutcomes
                                            group
                                            selection.Occurrence
                                            (Some ActivationStage.Ready) }
                            return
                                refuse
                                    ComponentBindingLifecycleFailureKind.PortableInterconnectionRefused
                                    "ready-missing"
                                    "Portable Interconnection completed without a Ready lifecycle state."
                                    (Some(FakeActivationRuntime.activate failedRequest))
                                    (Some memberValue)
                        | Ok() ->
                            let runtime = FakeActivationRuntime.activate successfulRequest
                            if runtime.Kind <> ActivationRuntimeOutcomeKind.Active then
                                return
                                    refuse
                                        ComponentBindingLifecycleFailureKind.RuntimeRefusedBeforeStart
                                        "runtime-state-changed"
                                        (sprintf
                                            "CM4 no longer accepted the lifecycle after portable Ready: %A."
                                            runtime.Kind)
                                        (Some runtime)
                                        (Some memberValue)
                            else
                                match memberValue.Release() with
                                | Ok() ->
                                    return
                                        { Runtime = Some runtime
                                          Member = Some memberValue
                                          Failure = None }
                                | Error error ->
                                    let code, reason = portableError error
                                    return
                                        refuse
                                            ComponentBindingLifecycleFailureKind.PortableReleaseRefused
                                            code
                                            reason
                                            (Some runtime)
                                            (Some memberValue)
        }

type ComponentAuthorityMapping =
    { Occurrence: OccurrenceId
      Participant: ActorId }

[<RequireQualifiedAccess>]
type ComponentAuthorityIntegrationFailureKind =
    | MappingInvalid
    | AuthorityShapeUnsupported
    | AuthorityRefused
    | LifecycleRefused

type ComponentAuthorityIntegrationFailure =
    { Kind: ComponentAuthorityIntegrationFailureKind
      Code: string
      Reason: string }

type ComponentAuthorityIntegrationResult =
    { Authority: AuthorityAdmissionOutcome option
      Lifecycle: ComponentBindingLifecycleResult option
      Failure: ComponentAuthorityIntegrationFailure option }

[<RequireQualifiedAccess>]
module ComponentAuthorityIntegration =
    let private refuse kind code reason authority lifecycle =
        { Authority = authority
          Lifecycle = lifecycle
          Failure = Some { Kind = kind; Code = code; Reason = reason } }

    let private trySupportedAuthorityShape
        (request: AuthorityAdmissionRequest)
        (runtime: ActivationRuntimeRequest)
        =
        match request.Relationships, request.Authority with
        | [ relationship ], [ authority ] when
            relationship.Kind = ActorRelationshipKind.ComponentParticipant
            && relationship.ProposedActor = request.Participant
            && authority.Relationship = relationship.Request
            && not authority.Unlimited
            && runtime.BindingExercises.IsEmpty ->
            Some(relationship, authority)
        | _ -> None

    let private isExactAdmission
        (outcome: AuthorityAdmissionOutcome)
        (requestedRelationship: ActorRelationshipRequest)
        (requestedAuthority: AuthorityRequest)
        =
        match outcome.Kind, outcome.Observation.Relationships, outcome.Observation.Grants with
        | AuthorityAdmissionOutcomeKind.Admitted, [ relationship ], [ grant ] ->
            relationship.Request = requestedRelationship.Request
            && relationship.ProposedActor = requestedRelationship.ProposedActor
            && grant.Request = requestedAuthority.Request
            && grant.Holder = relationship.LocalActor
            && grant.Capability = requestedAuthority.Capability
            && grant.Target = requestedAuthority.Target
            && grant.Operation = requestedAuthority.Operation
            && grant.Scope = requestedAuthority.Scope
        | _ -> false

    let activate
        (resolution: ResolutionOutcome)
        (selection: ComponentBindingSelection)
        (mapping: ComponentAuthorityMapping)
        (runtimeRequest: ActivationRuntimeRequest)
        (authorityRequest: AuthorityAdmissionRequest)
        (conversation: IPortableProviderConversation)
        =
        task {
            if
                mapping.Occurrence <> selection.Occurrence
                || mapping.Participant <> authorityRequest.Participant
            then
                return
                    refuse
                        ComponentAuthorityIntegrationFailureKind.MappingInvalid
                        "authority-mapping-invalid"
                        "CBI3 requires the explicit occurrence and participant mapping to match the CBI1 selection and CM5 request."
                        None
                        None
            else
                match trySupportedAuthorityShape authorityRequest runtimeRequest with
                | None ->
                    return
                        refuse
                            ComponentAuthorityIntegrationFailureKind.AuthorityShapeUnsupported
                            "authority-shape-unsupported"
                            "CBI3 supports one ComponentParticipant relationship, one dependent narrow authority request, and no caller-authored CM4 binding exercises."
                            None
                            None
                | Some(requestedRelationship, requestedAuthority) ->
                    let admission = FakeAuthorityAdmission.evaluate authorityRequest
                    if not (isExactAdmission admission requestedRelationship requestedAuthority) then
                        return
                            refuse
                                ComponentAuthorityIntegrationFailureKind.AuthorityRefused
                                "authority-not-admitted"
                                (sprintf
                                    "CM5 did not admit exactly one attributable relationship and grant: %A."
                                    admission.Kind)
                                (Some admission)
                                None
                    else
                        let! lifecycle =
                            ComponentBindingLifecycle.activate
                                resolution
                                selection
                                runtimeRequest
                                conversation
                        match lifecycle.Failure with
                        | None when
                            lifecycle.Runtime
                            |> Option.exists (fun runtime ->
                                runtime.Kind = ActivationRuntimeOutcomeKind.Active)
                            && lifecycle.Member |> Option.exists _.IsReleased ->
                            return
                                { Authority = Some admission
                                  Lifecycle = Some lifecycle
                                  Failure = None }
                        | _ ->
                            return
                                refuse
                                    ComponentAuthorityIntegrationFailureKind.LifecycleRefused
                                    (lifecycle.Failure
                                     |> Option.map _.Code
                                     |> Option.defaultValue "lifecycle-not-active")
                                    (lifecycle.Failure
                                     |> Option.map _.Reason
                                     |> Option.defaultValue
                                         "CBI2 did not return a released Active member.")
                                    (Some admission)
                                    (Some lifecycle)
        }

[<RequireQualifiedAccess>]
type ComponentAuthorityRevalidationKind =
    | Continued
    | Withdrawn
    | RetirementFailed
    | ActivationUnavailable

type ComponentAuthorityRevalidationResult =
    { Kind: ComponentAuthorityRevalidationKind
      CurrentAuthority: AuthorityAdmissionOutcome option
      Replacement: ReplacementRecord option
      Code: string
      Reason: string }

[<RequireQualifiedAccess>]
module ComponentAuthorityRevalidation =
    let private matchesPreviousRequest
        (previous: AuthorityAdmissionOutcome)
        (request: AuthorityAdmissionRequest)
        =
        match
            previous.Observation.Relationships,
            previous.Observation.Grants,
            request.Relationships,
            request.Authority
        with
        | [ oldRelationship ], [ oldGrant ], [ relationship ], [ authority ] ->
            request.Request = previous.Observation.Request
            && request.Policy.Policy = previous.Observation.Policy
            && request.Participant = oldRelationship.ProposedActor
            && relationship.Request = oldRelationship.Request
            && relationship.ProposedActor = oldRelationship.ProposedActor
            && relationship.Kind = oldRelationship.Kind
            && authority.Request = oldGrant.Request
            && authority.Relationship = oldRelationship.Request
            && authority.Capability = oldGrant.Capability
            && authority.Target = oldGrant.Target
            && authority.Operation = oldGrant.Operation
            && authority.Scope = oldGrant.Scope
            && not authority.Unlimited
        | _ -> false

    let private isSameAdmission
        (previous: AuthorityAdmissionOutcome)
        (current: AuthorityAdmissionOutcome)
        =
        match
            current.Kind,
            previous.Observation.Relationships,
            current.Observation.Relationships,
            previous.Observation.Grants,
            current.Observation.Grants
        with
        | AuthorityAdmissionOutcomeKind.Admitted,
          [ oldRelationship ],
          [ newRelationship ],
          [ oldGrant ],
          [ newGrant ] ->
            newRelationship = oldRelationship && newGrant = oldGrant
        | _ -> false

    let revalidate
        (active: ComponentAuthorityIntegrationResult)
        (request: AuthorityAdmissionRequest)
        retirementReason
        =
        task {
            if String.IsNullOrWhiteSpace retirementReason then
                invalidArg (nameof retirementReason) "retirement reason is required"

            match active.Authority, active.Lifecycle, active.Failure with
            | Some previous, Some lifecycle, None when
                previous.Kind = AuthorityAdmissionOutcomeKind.Admitted
                && previous.Observation.Relationships.Length = 1
                && previous.Observation.Grants.Length = 1
                && lifecycle.Failure.IsNone
                && lifecycle.Runtime
                   |> Option.exists (fun runtime ->
                       runtime.Kind = ActivationRuntimeOutcomeKind.Active)
                && lifecycle.Member |> Option.exists _.IsReleased ->
                let memberValue = lifecycle.Member.Value
                let current, code =
                    if matchesPreviousRequest previous request then
                        let evaluated = FakeAuthorityAdmission.evaluate request
                        Some evaluated, "authority-not-renewed"
                    else
                        None, "authority-revalidation-mismatch"

                match current with
                | Some evaluated when isSameAdmission previous evaluated ->
                    return
                        { Kind = ComponentAuthorityRevalidationKind.Continued
                          CurrentAuthority = current
                          Replacement = None
                          Code = "authority-current"
                          Reason =
                            "The exact receiving-domain relationship and grant remain admitted." }
                | _ ->
                    let! retired = memberValue.Retire retirementReason
                    match retired with
                    | Ok replacement ->
                        return
                            { Kind = ComponentAuthorityRevalidationKind.Withdrawn
                              CurrentAuthority = current
                              Replacement = Some replacement
                              Code = code
                              Reason =
                                "The prior authority is no longer current, so the portable member was retired." }
                    | Error error ->
                        let detail =
                            match error with
                            | PortableError.Refused fault ->
                                sprintf "%s: %s" fault.LocalCode fault.Message
                            | PortableError.Interrupted failure -> failure.Message
                        return
                            { Kind = ComponentAuthorityRevalidationKind.RetirementFailed
                              CurrentAuthority = current
                              Replacement = None
                              Code = "authority-retirement-failed"
                              Reason = detail }
            | _ ->
                return
                    { Kind = ComponentAuthorityRevalidationKind.ActivationUnavailable
                      CurrentAuthority = None
                      Replacement = None
                      Code = "active-authority-unavailable"
                      Reason =
                        "CBI5 requires one released Active CBI3 result with one relationship and grant." }
        }

[<RequireQualifiedAccess>]
module ComponentAuthorityComparison =
    let private stringNode (value: string) : JsonNode | null = JsonValue.Create value
    let private boolNode (value: bool) : JsonNode | null = JsonValue.Create value
    let private intNode (value: int) : JsonNode | null = JsonValue.Create value

    let private setString (node: JsonObject) (name: string) (value: string) =
        node[name] <- stringNode value

    let private setBoolean (node: JsonObject) (name: string) (value: bool) =
        node[name] <- boolNode value

    let private digestText (value: string) =
        value
        |> Encoding.UTF8.GetBytes
        |> SHA256.HashData
        |> Convert.ToHexStringLower

    let private authorityOutcomeToken kind =
        match kind with
        | AuthorityAdmissionOutcomeKind.Admitted -> "admitted"
        | AuthorityAdmissionOutcomeKind.PartiallyAdmitted -> "partially-admitted"
        | AuthorityAdmissionOutcomeKind.Denied -> "denied"
        | AuthorityAdmissionOutcomeKind.InvalidRequest -> "invalid-request"

    let private runtimeOutcomeToken kind =
        match kind with
        | ActivationRuntimeOutcomeKind.Active -> "active"
        | ActivationRuntimeOutcomeKind.RolledBack -> "rolled-back"
        | ActivationRuntimeOutcomeKind.PreparationFailed -> "preparation-failed"
        | ActivationRuntimeOutcomeKind.EstablishmentFailed -> "establishment-failed"
        | ActivationRuntimeOutcomeKind.ReleaseFailedBeforeCutover -> "release-failed-before-cutover"
        | ActivationRuntimeOutcomeKind.RollbackUnavailable -> "rollback-unavailable"
        | ActivationRuntimeOutcomeKind.RetainedGenerationCorrupted -> "retained-generation-corrupted"
        | ActivationRuntimeOutcomeKind.InvalidCm3Plan -> "invalid-cm3-plan"
        | ActivationRuntimeOutcomeKind.RestartScopeConflict -> "restart-scope-conflict"
        | ActivationRuntimeOutcomeKind.StageObservationConflict -> "stage-observation-conflict"
        | ActivationRuntimeOutcomeKind.InteractionRefused -> "interaction-refused"
        | ActivationRuntimeOutcomeKind.BindingObservationConflict -> "binding-observation-conflict"
        | ActivationRuntimeOutcomeKind.ChildPortClosed -> "child-port-closed"
        | ActivationRuntimeOutcomeKind.ReplacementLifecycleRequired -> "replacement-lifecycle-required"
        | ActivationRuntimeOutcomeKind.HostAssistedOrderConflict -> "host-assisted-order-conflict"

    let private integrationFailureNode
        (failure: ComponentAuthorityIntegrationFailure option)
        : JsonNode | null =
        match failure with
        | None -> null
        | Some failure ->
            let kind =
                match failure.Kind with
                | ComponentAuthorityIntegrationFailureKind.MappingInvalid -> "mapping-invalid"
                | ComponentAuthorityIntegrationFailureKind.AuthorityShapeUnsupported ->
                    "authority-shape-unsupported"
                | ComponentAuthorityIntegrationFailureKind.AuthorityRefused -> "authority-refused"
                | ComponentAuthorityIntegrationFailureKind.LifecycleRefused -> "lifecycle-refused"
            let node = JsonObject()
            setString node "kind" kind
            setString node "code" failure.Code
            node

    let private authorityNode
        (authority: AuthorityAdmissionOutcome option)
        : JsonNode | null =
        match authority with
        | None -> null
        | Some authority ->
            let node = JsonObject()
            setString node "outcome" (authorityOutcomeToken authority.Kind)
            setString
                node
                "profileSha256"
                (FakeAuthorityComparison.canonicalProfile authority |> digestText)
            node

    let private effectsNode (effects: ActivationRuntimeEffects) =
        let node = JsonObject()
        setBoolean node "prepared" effects.Prepared
        setBoolean node "establishmentStarted" effects.EstablishmentStarted
        setBoolean node "actorEndpointEstablished" effects.ActorEndpointEstablished
        setBoolean node "lifecycleOperationExecuted" effects.LifecycleOperationExecuted
        setBoolean node "memberReportedReady" effects.MemberReportedReady
        setBoolean node "released" effects.Released
        setBoolean node "ordinaryInteractionAdmitted" effects.OrdinaryInteractionAdmitted
        setBoolean node "activeGenerationMutated" effects.ActiveGenerationMutated
        setBoolean node "retainedGenerationRetired" effects.RetainedGenerationRetired
        setBoolean node "rollbackAttempted" effects.RollbackAttempted
        setBoolean node "capabilityGranted" effects.CapabilityGranted
        node

    let private runtimeNode
        (runtime: ActivationRuntimeOutcome option)
        : JsonNode | null =
        match runtime with
        | None -> null
        | Some runtime ->
            let node = JsonObject()
            setString node "kind" (runtimeOutcomeToken runtime.Kind)
            match runtime.Failure with
            | Some failure -> setString node "failureKind" (runtimeOutcomeToken failure.Kind)
            | None -> node["failureKind"] <- null
            node["effects"] <- effectsNode runtime.Observation.Effects
            node

    let private memberNode
        (memberValue: CompositionMember option)
        : JsonNode | null =
        match memberValue with
        | None -> null
        | Some memberValue ->
            let planFacts =
                memberValue.TryPlan
                |> Option.map (fun plan ->
                    BindingPlan.factNames plan
                    |> List.choose (fun name ->
                        BindingPlan.tryFact name plan |> Option.map (fun value -> name, value))
                    |> Map.ofList)
                |> Option.defaultValue Map.empty
                |> Map.remove "planId"
            let facts =
                (memberValue.ResolutionFacts, planFacts)
                ||> Map.fold (fun state key value -> Map.add key value state)
            let factNode = JsonObject()
            facts |> Map.iter (fun key value -> setString factNode key value)
            let node = JsonObject()
            setString node "stage" (CompositionStage.token memberValue.Stage)
            setBoolean node "ready" memberValue.IsReady
            setBoolean node "released" memberValue.IsReleased
            node["facts"] <- factNode
            node

    let private lifecycleFailureNode
        (failure: ComponentBindingLifecycleFailure option)
        : JsonNode | null =
        match failure with
        | None -> null
        | Some failure ->
            let kind =
                match failure.Kind with
                | ComponentBindingLifecycleFailureKind.PreparationUnavailable ->
                    "preparation-unavailable"
                | ComponentBindingLifecycleFailureKind.PlanUnsupported -> "plan-unsupported"
                | ComponentBindingLifecycleFailureKind.RuntimeRefusedBeforeStart ->
                    "runtime-refused-before-start"
                | ComponentBindingLifecycleFailureKind.PortableInterconnectionRefused ->
                    "portable-interconnection-refused"
                | ComponentBindingLifecycleFailureKind.PortableReleaseRefused ->
                    "portable-release-refused"
            let node = JsonObject()
            setString node "kind" kind
            setString node "code" failure.Code
            node

    let private lifecycleNode
        (lifecycle: ComponentBindingLifecycleResult option)
        : JsonNode | null =
        match lifecycle with
        | None -> null
        | Some lifecycle ->
            let node = JsonObject()
            node["runtime"] <- runtimeNode lifecycle.Runtime
            node["member"] <- memberNode lifecycle.Member
            node["failure"] <- lifecycleFailureNode lifecycle.Failure
            node

    let private isActive (result: ComponentAuthorityIntegrationResult) =
        match result.Authority, result.Lifecycle, result.Failure with
        | Some authority, Some lifecycle, None ->
            authority.Kind = AuthorityAdmissionOutcomeKind.Admitted
            && authority.Observation.Grants.Length = 1
            && lifecycle.Failure.IsNone
            && lifecycle.Runtime
               |> Option.exists (fun runtime ->
                   runtime.Kind = ActivationRuntimeOutcomeKind.Active)
            && lifecycle.Member |> Option.exists _.IsReleased
        | _ -> false

    let profile scenario (result: ComponentAuthorityIntegrationResult) =
        if String.IsNullOrWhiteSpace scenario then
            invalidArg (nameof scenario) "scenario identity is required"
        let node = JsonObject()
        node["schemaVersion"] <- intNode 1
        setString node "scenario" scenario
        setBoolean node "active" (isActive result)
        node["integrationFailure"] <- integrationFailureNode result.Failure
        node["authority"] <- authorityNode result.Authority
        node["lifecycle"] <- lifecycleNode result.Lifecycle
        node.ToJsonString()

    let digest (profile: string) =
        ArgumentNullException.ThrowIfNull profile
        digestText profile
