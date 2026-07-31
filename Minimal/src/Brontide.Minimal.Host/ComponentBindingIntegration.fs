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

    let internal trySupportedGroup plan selectedOccurrence =
        match plan.Groups with
        | [ group ] when
            group.Members.Length = 1
            && (List.exactlyOne group.Members).Occurrence = selectedOccurrence
            && group.Protocols.IsEmpty ->
            Some group
        | _ -> None

    let internal stageOutcomes group memberValue failedStage =
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

type ComponentParticipantRequest =
    { Mapping: ComponentAuthorityMapping
      Request: AuthorityAdmissionRequest }

[<RequireQualifiedAccess>]
type ComponentParticipantAdmissionFailureKind =
    | ParticipantSetInvalid
    | AuthorityShapeUnsupported
    | AuthorityRefused
    | LocalIdentityConflict
    | LifecycleRefused

type ComponentParticipantAdmissionFailure =
    { Kind: ComponentParticipantAdmissionFailureKind
      Code: string
      Reason: string }

type ComponentParticipantObservation =
    { Participant: ActorId
      Authority: AuthorityAdmissionOutcome }

/// The outcome of the effect-free admission step, before any provider is contacted.
type internal ComponentParticipantAdmissionStep =
    { Admissions: ComponentParticipantObservation list
      Grants: LocalCapabilityGrant list
      Failure: ComponentParticipantAdmissionFailure option }

type ComponentParticipantAdmissionResult =
    { Admissions: ComponentParticipantObservation list
      Grants: LocalCapabilityGrant list
      Lifecycle: ComponentBindingLifecycleResult option
      Failure: ComponentParticipantAdmissionFailure option }

/// Gates one CBI2 activation with a set of participants, each holding one or more exact narrow
/// CM5 grants.
///
/// A CM5 request names exactly one participant, so a participant set is a set of requests. The
/// evaluator sees each one alone, which leaves the cross-request questions — repeated identities
/// and two participants sharing one receiving-domain Actor — to this coordinator.
[<RequireQualifiedAccess>]
module ComponentParticipantAdmission =
    let private ordinal (left: string) (right: string) = String.CompareOrdinal(left, right)

    let internal firstDuplicate values =
        values
        |> List.countBy id
        |> List.filter (fun (_, count) -> count > 1)
        |> List.map fst
        |> List.sortWith ordinal
        |> List.tryHead

    let private refuse kind code reason admissions lifecycle =
        { Admissions = admissions
          Grants = []
          Lifecycle = lifecycle
          Failure = Some { Kind = kind; Code = code; Reason = reason } }

    let private lifecycleIsActive (lifecycle: ComponentBindingLifecycleResult) =
        lifecycle.Failure.IsNone
        && lifecycle.Runtime
           |> Option.exists (fun runtime -> runtime.Kind = ActivationRuntimeOutcomeKind.Active)
        && lifecycle.Member |> Option.exists _.IsReleased

    let isActive (result: ComponentParticipantAdmissionResult) =
        result.Failure.IsNone
        && not result.Grants.IsEmpty
        && result.Lifecycle |> Option.exists lifecycleIsActive

    /// Checks the identity rules that only span requests, which no single CM5 evaluation can see.
    let internal distinctIdentities (requests: AuthorityAdmissionRequest list) =
        let admissionRequests =
            requests |> List.map (fun request -> AdmissionRequestId.value request.Request)
        let relationshipRequests =
            requests
            |> List.collect (fun request ->
                request.Relationships
                |> List.map (fun relationship -> RelationshipRequestId.value relationship.Request))
        let authorityRequests =
            requests
            |> List.collect (fun request ->
                request.Authority
                |> List.map (fun authority -> AuthorityRequestId.value authority.Request))
        match
            firstDuplicate admissionRequests,
            firstDuplicate relationshipRequests,
            firstDuplicate authorityRequests
        with
        | Some admission, _, _ ->
            Error(
                "admission-identity-not-distinct",
                sprintf "Admission request identity '%s' is used by more than one participant." admission)
        | _, Some relationship, _ ->
            Error(
                "relationship-identity-not-distinct",
                sprintf
                    "Relationship request identity '%s' is used by more than one participant."
                    relationship)
        | _, _, Some authority ->
            Error(
                "authority-identity-not-distinct",
                sprintf
                    "Authority request identity '%s' is used by more than one participant, so its grants would share an identity."
                    authority)
        | None, None, None -> Ok()

    let private validateSet (selection: ComponentBindingSelection) participants =
        match participants with
        | [] ->
            Error("participant-set-empty", "CBI6 requires at least one participant admission request.")
        | _ when
            participants
            |> List.exists (fun entry ->
                entry.Mapping.Occurrence <> selection.Occurrence
                || entry.Mapping.Participant <> entry.Request.Participant)
            ->
            Error(
                "participant-mapping-invalid",
                "Every participant mapping must name the CBI1-selected occurrence and its own CM5 request participant.")
        | _ ->
            let participantActors =
                participants |> List.map (fun entry -> ActorId.value entry.Mapping.Participant)
            match firstDuplicate participantActors with
            | Some actor ->
                Error(
                    "participant-not-distinct",
                    sprintf "Participant '%s' appears in more than one admission request." actor)
            | None -> participants |> List.map _.Request |> distinctIdentities

    let internal supportedShape (request: AuthorityAdmissionRequest) =
        match request.Relationships, request.Authority with
        | [ relationship ], (_ :: _ as authority) ->
            relationship.Kind = ActorRelationshipKind.ComponentParticipant
            && relationship.ProposedActor = request.Participant
            && authority
               |> List.forall (fun item ->
                   item.Relationship = relationship.Request && not item.Unlimited)
            && authority
               |> List.map (fun item ->
                   sprintf
                       "%s|%s|%s|%s"
                       (CapabilityId.value item.Capability)
                       (ActorId.value item.Target)
                       (OperationId.value item.Operation)
                       (CapabilityScopeId.value item.Scope))
               |> firstDuplicate
               |> Option.isNone
        | _ -> false

    /// One grant per submitted request, matched on the complete tuple, so equal counts and a single
    /// match each make the correspondence a bijection rather than a coincidence.
    let internal isExactAdmission
        (request: AuthorityAdmissionRequest)
        (outcome: AuthorityAdmissionOutcome)
        =
        match outcome.Kind, outcome.Observation.Relationships, request.Relationships with
        | AuthorityAdmissionOutcomeKind.Admitted, [ established ], [ submitted ] when
            established.Request = submitted.Request
            && established.ProposedActor = submitted.ProposedActor
            && established.Kind = submitted.Kind
            && outcome.Observation.Grants.Length = request.Authority.Length
            ->
            request.Authority
            |> List.forall (fun authority ->
                outcome.Observation.Grants
                |> List.filter (fun grant ->
                    grant.Request = authority.Request
                    && grant.Holder = established.LocalActor
                    && grant.Capability = authority.Capability
                    && grant.Target = authority.Target
                    && grant.Operation = authority.Operation
                    && grant.Scope = authority.Scope)
                |> List.length = 1)
        | _ -> false

    /// The effect-free half: everything decided before a provider is contacted.
    ///
    /// Separated so an activation of several members can admit every member's set before any of them
    /// is established, which is what lets a refusal cost nothing to undo.
    let internal admit
        (selection: ComponentBindingSelection)
        (participants: ComponentParticipantRequest list)
        (runtimeRequest: ActivationRuntimeRequest)
        : ComponentParticipantAdmissionStep =
        let step kind code reason admissions =
            { Admissions = admissions
              Grants = []
              Failure = Some { Kind = kind; Code = code; Reason = reason } }
        // Ordering by participant makes evaluation, observation, and grant order independent of
        // the order the caller happened to build the set in.
        let ordered =
            participants
            |> List.sortWith (fun left right ->
                ordinal
                    (ActorId.value left.Mapping.Participant)
                    (ActorId.value right.Mapping.Participant))
        match validateSet selection ordered with
        | Error(code, reason) ->
            step ComponentParticipantAdmissionFailureKind.ParticipantSetInvalid code reason []
        | Ok() ->
            if
                not runtimeRequest.BindingExercises.IsEmpty
                || ordered |> List.exists (fun entry -> not (supportedShape entry.Request))
            then
                step
                    ComponentParticipantAdmissionFailureKind.AuthorityShapeUnsupported
                    "authority-shape-unsupported"
                    "CBI6 supports one ComponentParticipant relationship per participant, distinct narrow authority tuples dependent on it, and no caller-authored CM4 binding exercises."
                    []
            else
                let admissions =
                    ordered
                    |> List.map (fun entry ->
                        { Participant = entry.Mapping.Participant
                          Authority = FakeAuthorityAdmission.evaluate entry.Request })
                let refusedParticipants =
                    List.zip ordered admissions
                    |> List.filter (fun (entry, observation) ->
                        not (isExactAdmission entry.Request observation.Authority))
                    |> List.map (fun (entry, _) -> ActorId.value entry.Mapping.Participant)
                if not refusedParticipants.IsEmpty then
                    step
                        ComponentParticipantAdmissionFailureKind.AuthorityRefused
                        "authority-not-admitted"
                        (sprintf
                            "CM5 did not admit the exact submitted authority for %s."
                            (String.Join(", ", refusedParticipants)))
                        admissions
                else
                    let holders =
                        admissions
                        |> List.map (fun observation ->
                            observation.Authority.Observation.Relationships
                            |> List.exactlyOne
                            |> fun relationship -> LocalActorReferenceId.value relationship.LocalActor)
                    match firstDuplicate holders with
                    | Some shared ->
                        step
                            ComponentParticipantAdmissionFailureKind.LocalIdentityConflict
                            "local-actor-conflict"
                            (sprintf
                                "Two participants were mapped onto the receiving-domain Actor '%s', which would merge their grants into one holder."
                                shared)
                            admissions
                    | None ->
                        { Admissions = admissions
                          Grants =
                            admissions
                            |> List.collect (fun observation ->
                                observation.Authority.Observation.Grants)
                            |> List.sortWith (fun left right ->
                                ordinal
                                    (CapabilityGrantId.value left.Grant)
                                    (CapabilityGrantId.value right.Grant))
                          Failure = None }

    let activate
        (resolution: ResolutionOutcome)
        (selection: ComponentBindingSelection)
        (participants: ComponentParticipantRequest list)
        (runtimeRequest: ActivationRuntimeRequest)
        (conversation: IPortableProviderConversation)
        =
        task {
            let step = admit selection participants runtimeRequest
            match step.Failure with
            | Some failure ->
                return
                    { Admissions = step.Admissions
                      Grants = []
                      Lifecycle = None
                      Failure = Some failure }
            | None ->
                let! lifecycle =
                    ComponentBindingLifecycle.activate resolution selection runtimeRequest conversation
                if not (lifecycleIsActive lifecycle) then
                    return
                        refuse
                            ComponentParticipantAdmissionFailureKind.LifecycleRefused
                            (lifecycle.Failure
                             |> Option.map _.Code
                             |> Option.defaultValue "lifecycle-not-active")
                            (lifecycle.Failure
                             |> Option.map _.Reason
                             |> Option.defaultValue "CBI2 did not return a released Active member.")
                            step.Admissions
                            (Some lifecycle)
                else
                    return
                        { Admissions = step.Admissions
                          Grants = step.Grants
                          Lifecycle = Some lifecycle
                          Failure = None }
        }

[<RequireQualifiedAccess>]
type ComponentParticipantRevalidationKind =
    | Continued
    | Withdrawn
    | RetirementFailed
    | ActivationUnavailable

type ComponentParticipantRevalidationResult =
    { Kind: ComponentParticipantRevalidationKind
      CurrentAuthority: ComponentParticipantObservation list
      Unrenewed: ActorId list
      Replacement: ReplacementRecord option
      Code: string
      Reason: string }

/// Revalidates the complete CBI6 participant set behind one released member and retires it when the
/// set does not renew identically.
///
/// Retiring on partial loss rather than dropping the participant that lost authority is deliberate:
/// nothing in the admitted set says which participants the member's ordinary interaction depends
/// on, so continuing would decide that invisibly.
[<RequireQualifiedAccess>]
module ComponentParticipantRevalidation =
    let private ordinal (left: string) (right: string) = String.CompareOrdinal(left, right)

    let internal matchesPrior
        (prior: ComponentParticipantObservation)
        (request: AuthorityAdmissionRequest)
        =
        match prior.Authority.Observation.Relationships, request.Relationships with
        | [ relationship ], [ submitted ] ->
            let grants = prior.Authority.Observation.Grants
            request.Request = prior.Authority.Observation.Request
            && request.Policy.Policy = prior.Authority.Observation.Policy
            && request.Participant = prior.Participant
            && submitted.Request = relationship.Request
            && submitted.ProposedActor = relationship.ProposedActor
            && submitted.Kind = relationship.Kind
            && request.Authority.Length = grants.Length
            && grants
               |> List.forall (fun grant ->
                   request.Authority
                   |> List.filter (fun authority ->
                       authority.Request = grant.Request
                       && authority.Relationship = relationship.Request
                       && authority.Capability = grant.Capability
                       && authority.Target = grant.Target
                       && authority.Operation = grant.Operation
                       && authority.Scope = grant.Scope
                       && not authority.Unlimited)
                   |> List.length = 1)
        | _ -> false

    let internal isSameAdmission
        (prior: AuthorityAdmissionOutcome)
        (current: AuthorityAdmissionOutcome)
        =
        match current.Kind, current.Observation.Relationships with
        | AuthorityAdmissionOutcomeKind.Admitted, [ _ ] ->
            current.Observation.Relationships = prior.Observation.Relationships
            && current.Observation.Grants = prior.Observation.Grants
        | _ -> false

    /// Retires the member and classifies the peer outcome, without deciding what the caller's
    /// result looks like.
    let internal tryRetire (memberValue: CompositionMember) retirementReason =
        task {
            let! retired = memberValue.Retire retirementReason
            return
                match retired with
                | Ok replacement -> Ok replacement
                | Error(PortableError.Refused fault) ->
                    Error(sprintf "%s: %s" fault.LocalCode fault.Message)
                | Error(PortableError.Interrupted failure) -> Error failure.Message
        }

    let private retire
        (memberValue: CompositionMember)
        retirementReason
        code
        reason
        current
        unrenewed
        =
        task {
            let! retired = tryRetire memberValue retirementReason
            match retired with
            | Ok replacement ->
                return
                    { Kind = ComponentParticipantRevalidationKind.Withdrawn
                      CurrentAuthority = current
                      Unrenewed = unrenewed
                      Replacement = Some replacement
                      Code = code
                      Reason = reason }
            | Error detail ->
                return
                    { Kind = ComponentParticipantRevalidationKind.RetirementFailed
                      CurrentAuthority = current
                      Unrenewed = unrenewed
                      Replacement = None
                      Code = "authority-retirement-failed"
                      Reason = detail }
        }

    let revalidate
        (active: ComponentParticipantAdmissionResult)
        (requests: AuthorityAdmissionRequest list)
        retirementReason
        =
        task {
            if String.IsNullOrWhiteSpace retirementReason then
                invalidArg (nameof retirementReason) "retirement reason is required"

            match ComponentParticipantAdmission.isActive active, active.Lifecycle with
            | true, Some { Member = Some memberValue } ->
                let prior = active.Admissions
                let ordered =
                    requests
                    |> List.sortWith (fun left right ->
                        ordinal (ActorId.value left.Participant) (ActorId.value right.Participant))
                if
                    (ordered |> List.map _.Participant) <> (prior |> List.map _.Participant)
                then
                    return!
                        retire
                            memberValue
                            retirementReason
                            "participant-set-changed"
                            "The fresh requests do not name the same participants the admitted set named."
                            []
                            []
                elif
                    List.zip prior ordered
                    |> List.exists (fun (priorItem, request) -> not (matchesPrior priorItem request))
                then
                    return!
                        retire
                            memberValue
                            retirementReason
                            "authority-revalidation-mismatch"
                            "A fresh request does not identify the same relationship and grants that admitted this member."
                            []
                            []
                else
                    let current =
                        ordered
                        |> List.map (fun request ->
                            { Participant = request.Participant
                              Authority = FakeAuthorityAdmission.evaluate request })
                    let unrenewed =
                        List.zip prior current
                        |> List.filter (fun (priorItem, currentItem) ->
                            not (isSameAdmission priorItem.Authority currentItem.Authority))
                        |> List.map (fun (_, currentItem) -> currentItem.Participant)
                    if not unrenewed.IsEmpty then
                        return!
                            retire
                                memberValue
                                retirementReason
                                "authority-not-renewed"
                                (sprintf
                                    "The receiving domain no longer admits the identical authority for %s."
                                    (String.Join(", ", unrenewed |> List.map ActorId.value)))
                                current
                                unrenewed
                    else
                        return
                            { Kind = ComponentParticipantRevalidationKind.Continued
                              CurrentAuthority = current
                              Unrenewed = []
                              Replacement = None
                              Code = "authority-current"
                              Reason =
                                "Every participant still holds the identical receiving-domain relationship and grants." }
            | _ ->
                return
                    { Kind = ComponentParticipantRevalidationKind.ActivationUnavailable
                      CurrentAuthority = []
                      Unrenewed = []
                      Replacement = None
                      Code = "active-authority-unavailable"
                      Reason =
                        "CBI7 requires one released Active CBI6 result with a completely admitted participant set." }
        }

[<RequireQualifiedAccess>]
type ComponentParticipantExtensionKind =
    | Extended
    | Declined
    | Withdrawn
    | RetirementFailed
    | ActivationUnavailable

type ComponentParticipantExtensionResult =
    { Kind: ComponentParticipantExtensionKind
      InForce: ComponentParticipantAdmissionResult option
      CurrentAuthority: ComponentParticipantObservation list
      Unrenewed: ActorId list
      Replacement: ReplacementRecord option
      Code: string
      Reason: string }

/// Adds participants to an admitted CBI6 set while its member stays released.
///
/// Only growth is admitted. Removing or substituting a participant would withdraw authority the
/// member may rely on, and nothing in the set says whether it does; a substitute holding the same
/// tuple is a different grant because the holder is part of the grant. A declined extension is not
/// a failure of the binding and leaves it exactly as it was.
[<RequireQualifiedAccess>]
module ComponentParticipantExtension =
    let private ordinal (left: string) (right: string) = String.CompareOrdinal(left, right)

    let private decline (active: ComponentParticipantAdmissionResult) code reason current =
        { Kind = ComponentParticipantExtensionKind.Declined
          InForce = Some active
          CurrentAuthority = current
          Unrenewed = []
          Replacement = None
          Code = code
          Reason = reason }

    let private isRetained (prior: ComponentParticipantObservation list) participant =
        prior |> List.exists (fun existing -> existing.Participant = participant)

    let private structure
        (prior: ComponentParticipantObservation list)
        (intended: AuthorityAdmissionRequest list)
        =
        let repeated =
            intended
            |> List.map (fun request -> ActorId.value request.Participant)
            |> ComponentParticipantAdmission.firstDuplicate
        let missing =
            prior
            |> List.filter (fun existing ->
                intended
                |> List.forall (fun request -> request.Participant <> existing.Participant))
            |> List.map (fun existing -> ActorId.value existing.Participant)
        match repeated, missing with
        | Some actor, _ ->
            Error(
                "participant-not-distinct",
                sprintf "Participant '%s' appears in more than one request." actor)
        | None, (_ :: _) ->
            Error(
                "participant-not-retained",
                sprintf
                    "CBI8 only grows a set. Removing or substituting %s requires CBI7 retirement and a fresh CBI6 admission."
                    (String.Join(", ", missing)))
        | None, [] when intended.Length = prior.Length ->
            Error(
                "participant-set-unchanged",
                "The intended set adds no participant; revalidating the current set is CBI7.")
        | None, [] ->
            match ComponentParticipantAdmission.distinctIdentities intended with
            | Error collision -> Error collision
            | Ok() ->
                let added =
                    intended
                    |> List.filter (fun request -> not (isRetained prior request.Participant))
                if added |> List.forall ComponentParticipantAdmission.supportedShape then
                    Ok()
                else
                    Error(
                        "authority-shape-unsupported",
                        "CBI8 supports one ComponentParticipant relationship per added participant and distinct narrow authority tuples dependent on it.")

    let extend
        (active: ComponentParticipantAdmissionResult)
        (requests: AuthorityAdmissionRequest list)
        retirementReason
        =
        task {
            if String.IsNullOrWhiteSpace retirementReason then
                invalidArg (nameof retirementReason) "retirement reason is required"

            match ComponentParticipantAdmission.isActive active, active.Lifecycle with
            | true, Some { Member = Some memberValue } ->
                let prior = active.Admissions
                let ordered =
                    requests
                    |> List.sortWith (fun left right ->
                        ordinal (ActorId.value left.Participant) (ActorId.value right.Participant))
                match structure prior ordered with
                | Error(code, reason) -> return decline active code reason []
                | Ok() ->
                    let retained =
                        ordered |> List.filter (fun request -> isRetained prior request.Participant)
                    if
                        List.zip prior retained
                        |> List.exists (fun (priorItem, request) ->
                            not (ComponentParticipantRevalidation.matchesPrior priorItem request))
                    then
                        // Nothing was evaluated, so nothing was learned: this is a malformed
                        // request, not evidence that the retained authority is gone.
                        return
                            decline
                                active
                                "authority-revalidation-mismatch"
                                "A retained request does not identify the same relationship and grants that admitted this member."
                                []
                    else
                        let current =
                            ordered
                            |> List.map (fun request ->
                                { Participant = request.Participant
                                  Authority = FakeAuthorityAdmission.evaluate request })
                        let currentRetained =
                            current
                            |> List.filter (fun observation ->
                                isRetained prior observation.Participant)
                        let unrenewed =
                            List.zip prior currentRetained
                            |> List.filter (fun (priorItem, currentItem) ->
                                not (
                                    ComponentParticipantRevalidation.isSameAdmission
                                        priorItem.Authority
                                        currentItem.Authority))
                            |> List.map (fun (_, currentItem) -> currentItem.Participant)
                        if not unrenewed.IsEmpty then
                            // A lapse outranks any problem with the addition: the member's existing
                            // authority is gone, whatever the caller was trying to add.
                            let! retired =
                                ComponentParticipantRevalidation.tryRetire
                                    memberValue
                                    retirementReason
                            match retired with
                            | Ok replacement ->
                                return
                                    { Kind = ComponentParticipantExtensionKind.Withdrawn
                                      InForce = None
                                      CurrentAuthority = current
                                      Unrenewed = unrenewed
                                      Replacement = Some replacement
                                      Code = "authority-not-renewed"
                                      Reason =
                                        sprintf
                                            "The receiving domain no longer admits the identical authority for %s."
                                            (String.Join(
                                                ", ",
                                                unrenewed |> List.map ActorId.value)) }
                            | Error detail ->
                                return
                                    { Kind = ComponentParticipantExtensionKind.RetirementFailed
                                      InForce = None
                                      CurrentAuthority = current
                                      Unrenewed = unrenewed
                                      Replacement = None
                                      Code = "authority-retirement-failed"
                                      Reason = detail }
                        else
                            let refusedAdditions =
                                List.zip ordered current
                                |> List.filter (fun (request, observation) ->
                                    not (isRetained prior request.Participant)
                                    && not (
                                        ComponentParticipantAdmission.isExactAdmission
                                            request
                                            observation.Authority))
                                |> List.map (fun (request, _) -> ActorId.value request.Participant)
                            if not refusedAdditions.IsEmpty then
                                return
                                    decline
                                        active
                                        "authority-not-admitted"
                                        (sprintf
                                            "CM5 did not admit the exact submitted authority for %s."
                                            (String.Join(", ", refusedAdditions)))
                                        current
                            else
                                let holders =
                                    current
                                    |> List.map (fun observation ->
                                        observation.Authority.Observation.Relationships
                                        |> List.exactlyOne
                                        |> fun relationship ->
                                            LocalActorReferenceId.value relationship.LocalActor)
                                match ComponentParticipantAdmission.firstDuplicate holders with
                                | Some shared ->
                                    return
                                        decline
                                            active
                                            "local-actor-conflict"
                                            (sprintf
                                                "The extended set would map two participants onto the receiving-domain Actor '%s'."
                                                shared)
                                            current
                                | None ->
                                    let grants =
                                        current
                                        |> List.collect (fun observation ->
                                            observation.Authority.Observation.Grants)
                                        |> List.sortWith (fun left right ->
                                            ordinal
                                                (CapabilityGrantId.value left.Grant)
                                                (CapabilityGrantId.value right.Grant))
                                    return
                                        { Kind = ComponentParticipantExtensionKind.Extended
                                          InForce =
                                            Some
                                                { Admissions = current
                                                  Grants = grants
                                                  Lifecycle = active.Lifecycle
                                                  Failure = None }
                                          CurrentAuthority = current
                                          Unrenewed = []
                                          Replacement = None
                                          Code = "participant-set-extended"
                                          Reason =
                                            sprintf
                                                "The participant set now holds %d participants and %d grants."
                                                current.Length
                                                grants.Length }
            | _ ->
                return
                    { Kind = ComponentParticipantExtensionKind.ActivationUnavailable
                      InForce = None
                      CurrentAuthority = []
                      Unrenewed = []
                      Replacement = None
                      Code = "active-authority-unavailable"
                      Reason =
                        "CBI8 requires one released Active CBI6 result with a completely admitted participant set." }
        }

type ComponentGrantDependencyEntry =
    { DeclaredAuthority: string
      Capability: CapabilityId
      Target: ActorId
      Operation: OperationId
      Scope: CapabilityScopeId }

type ComponentGrantDependency =
    { Definition: DefinitionId
      Entries: ComponentGrantDependencyEntry list }

[<RequireQualifiedAccess>]
type ComponentParticipantRevisionKind =
    | Revised
    | Declined
    | Withdrawn
    | RetirementFailed
    | ActivationUnavailable

type ComponentParticipantRevisionResult =
    { Kind: ComponentParticipantRevisionKind
      InForce: ComponentParticipantAdmissionResult option
      CurrentAuthority: ComponentParticipantObservation list
      Unrenewed: ActorId list
      Replacement: ReplacementRecord option
      Code: string
      Reason: string }

/// Removes and substitutes participants of a live set, under a dependency the resolved Component
/// definition declared.
///
/// The declaration is what CBI7 and CBI8 lacked. Its names come from CM2's record of the selected
/// definition's requested authority, so the Component states what its interaction depends on and
/// the caller only maps each name to the CM5 tuple that satisfies it. Because the declaration names
/// tuples rather than holders, a substitute that satisfies the same dependency is enough.
[<RequireQualifiedAccess>]
module ComponentParticipantRevision =
    let private ordinal (left: string) (right: string) = String.CompareOrdinal(left, right)

    let internal entryTuple (entry: ComponentGrantDependencyEntry) =
        sprintf
            "%s|%s|%s|%s"
            (CapabilityId.value entry.Capability)
            (ActorId.value entry.Target)
            (OperationId.value entry.Operation)
            (CapabilityScopeId.value entry.Scope)

    let private grantTuple (grant: LocalCapabilityGrant) =
        sprintf
            "%s|%s|%s|%s"
            (CapabilityId.value grant.Capability)
            (ActorId.value grant.Target)
            (OperationId.value grant.Operation)
            (CapabilityScopeId.value grant.Scope)

    let internal uncovered (dependency: ComponentGrantDependency) grants =
        let held = grants |> List.map grantTuple |> Set.ofList
        dependency.Entries
        |> List.filter (fun entry -> not (Set.contains (entryTuple entry) held))
        |> List.map _.DeclaredAuthority
        |> List.sortWith ordinal

    let private decline (active: ComponentParticipantAdmissionResult) code reason current =
        { Kind = ComponentParticipantRevisionKind.Declined
          InForce = Some active
          CurrentAuthority = current
          Unrenewed = []
          Replacement = None
          Code = code
          Reason = reason }

    /// Checks that the declaration is the one the generation records, without asking whether the
    /// set in force covers it.
    let internal declarationShape
        (resolution: ResolutionOutcome)
        (selection: ComponentBindingSelection)
        (dependency: ComponentGrantDependency)
        =
        let declared =
            match resolution with
            | ResolutionOutcome.Resolved(_, generation) ->
                generation.RequestedAuthority
                |> List.tryFind (fun item -> item.Definition = selection.Definition)
            | _ -> None
        match declared with
        | None -> Error(
                      "dependency-declaration-mismatch",
                      "The declaration must name the CBI1-selected definition recorded by the completed generation.")
        | Some _ when dependency.Definition <> selection.Definition ->
            Error(
                "dependency-declaration-mismatch",
                "The declaration must name the CBI1-selected definition recorded by the completed generation.")
        | Some declared when declared.RequestedAuthority.IsEmpty ->
            Error(
                "dependency-declaration-empty",
                "The selected definition requests no authority, which states nothing about what its interaction depends on; use CBI8 to grow the set or CBI7 to retire it.")
        | Some declared ->
            let names = dependency.Entries |> List.map _.DeclaredAuthority |> List.sortWith ordinal
            let expected = declared.RequestedAuthority |> List.sortWith ordinal
            let repeatedTuple =
                dependency.Entries
                |> List.map entryTuple
                |> ComponentParticipantAdmission.firstDuplicate
            if names <> expected || repeatedTuple.IsSome then
                Error(
                    "dependency-declaration-mismatch",
                    "The declaration must map exactly the authority the selected definition requests, once each, to distinct tuples.")
            else
                Ok()

    let private declaration
        (resolution: ResolutionOutcome)
        (selection: ComponentBindingSelection)
        (dependency: ComponentGrantDependency)
        (active: ComponentParticipantAdmissionResult)
        =
        match declarationShape resolution selection dependency with
        | Error invalid -> Error invalid
        | Ok() ->
            match uncovered dependency active.Grants with
            | [] -> Ok()
            | missing ->
                Error(
                    "dependency-unsatisfied",
                    sprintf
                        "The set in force holds no grant satisfying declared authority %s, so it never covered this declaration."
                        (String.Join(", ", missing)))

    let private structure
        (prior: ComponentParticipantObservation list)
        (intended: AuthorityAdmissionRequest list)
        =
        let isRetained participant =
            prior |> List.exists (fun existing -> existing.Participant = participant)
        match intended with
        | [] ->
            Error(
                "participant-set-empty",
                "A revision must leave at least one participant; an empty set is not an admitted set.")
        | _ ->
            let repeated =
                intended
                |> List.map (fun request -> ActorId.value request.Participant)
                |> ComponentParticipantAdmission.firstDuplicate
            match repeated with
            | Some actor ->
                Error(
                    "participant-not-distinct",
                    sprintf "Participant '%s' appears in more than one request." actor)
            | None when
                intended.Length = prior.Length
                && intended |> List.forall (fun request -> isRetained request.Participant)
                ->
                Error(
                    "participant-set-unchanged",
                    "The intended set is the current one; revalidating it is CBI7.")
            | None ->
                match ComponentParticipantAdmission.distinctIdentities intended with
                | Error collision -> Error collision
                | Ok() ->
                    let added =
                        intended
                        |> List.filter (fun request -> not (isRetained request.Participant))
                    if added |> List.forall ComponentParticipantAdmission.supportedShape then
                        Ok()
                    else
                        Error(
                            "authority-shape-unsupported",
                            "CBI9 supports one ComponentParticipant relationship per added participant and distinct narrow authority tuples dependent on it.")

    let revise
        (resolution: ResolutionOutcome)
        (selection: ComponentBindingSelection)
        (active: ComponentParticipantAdmissionResult)
        (dependency: ComponentGrantDependency)
        (requests: AuthorityAdmissionRequest list)
        retirementReason
        =
        task {
            if String.IsNullOrWhiteSpace retirementReason then
                invalidArg (nameof retirementReason) "retirement reason is required"

            match ComponentParticipantAdmission.isActive active, active.Lifecycle with
            | true, Some { Member = Some memberValue } ->
                match declaration resolution selection dependency active with
                | Error(code, reason) -> return decline active code reason []
                | Ok() ->
                    let prior = active.Admissions
                    let priorOf participant =
                        prior |> List.tryFind (fun existing -> existing.Participant = participant)
                    let ordered =
                        requests
                        |> List.sortWith (fun left right ->
                            ordinal
                                (ActorId.value left.Participant)
                                (ActorId.value right.Participant))
                    match structure prior ordered with
                    | Error(code, reason) -> return decline active code reason []
                    | Ok() ->
                        let mismatched =
                            ordered
                            |> List.exists (fun request ->
                                match priorOf request.Participant with
                                | Some priorItem ->
                                    not (
                                        ComponentParticipantRevalidation.matchesPrior
                                            priorItem
                                            request)
                                | None -> false)
                        if mismatched then
                            return
                                decline
                                    active
                                    "authority-revalidation-mismatch"
                                    "A retained request does not identify the same relationship and grants that admitted this member."
                                    []
                        else
                            let current =
                                ordered
                                |> List.map (fun request ->
                                    { Participant = request.Participant
                                      Authority = FakeAuthorityAdmission.evaluate request })
                            let unrenewed =
                                current
                                |> List.filter (fun observation ->
                                    match priorOf observation.Participant with
                                    | Some priorItem ->
                                        not (
                                            ComponentParticipantRevalidation.isSameAdmission
                                                priorItem.Authority
                                                observation.Authority)
                                    | None -> false)
                                |> List.map _.Participant
                            if not unrenewed.IsEmpty then
                                let! retired =
                                    ComponentParticipantRevalidation.tryRetire
                                        memberValue
                                        retirementReason
                                match retired with
                                | Ok replacement ->
                                    return
                                        { Kind = ComponentParticipantRevisionKind.Withdrawn
                                          InForce = None
                                          CurrentAuthority = current
                                          Unrenewed = unrenewed
                                          Replacement = Some replacement
                                          Code = "authority-not-renewed"
                                          Reason =
                                            sprintf
                                                "The receiving domain no longer admits the identical authority for %s."
                                                (String.Join(
                                                    ", ",
                                                    unrenewed |> List.map ActorId.value)) }
                                | Error detail ->
                                    return
                                        { Kind = ComponentParticipantRevisionKind.RetirementFailed
                                          InForce = None
                                          CurrentAuthority = current
                                          Unrenewed = unrenewed
                                          Replacement = None
                                          Code = "authority-retirement-failed"
                                          Reason = detail }
                            else
                                let refusedAdditions =
                                    List.zip ordered current
                                    |> List.filter (fun (request, observation) ->
                                        (priorOf request.Participant).IsNone
                                        && not (
                                            ComponentParticipantAdmission.isExactAdmission
                                                request
                                                observation.Authority))
                                    |> List.map (fun (request, _) ->
                                        ActorId.value request.Participant)
                                if not refusedAdditions.IsEmpty then
                                    return
                                        decline
                                            active
                                            "authority-not-admitted"
                                            (sprintf
                                                "CM5 did not admit the exact submitted authority for %s."
                                                (String.Join(", ", refusedAdditions)))
                                            current
                                else
                                    let holders =
                                        current
                                        |> List.map (fun observation ->
                                            observation.Authority.Observation.Relationships
                                            |> List.exactlyOne
                                            |> fun relationship ->
                                                LocalActorReferenceId.value relationship.LocalActor)
                                    match ComponentParticipantAdmission.firstDuplicate holders with
                                    | Some shared ->
                                        return
                                            decline
                                                active
                                                "local-actor-conflict"
                                                (sprintf
                                                    "The revised set would map two participants onto the receiving-domain Actor '%s'."
                                                    shared)
                                                current
                                    | None ->
                                        let grants =
                                            current
                                            |> List.collect (fun observation ->
                                                observation.Authority.Observation.Grants)
                                            |> List.sortWith (fun left right ->
                                                ordinal
                                                    (CapabilityGrantId.value left.Grant)
                                                    (CapabilityGrantId.value right.Grant))
                                        match uncovered dependency grants with
                                        | (_ :: _) as missing ->
                                            return
                                                decline
                                                    active
                                                    "dependency-not-covered"
                                                    (sprintf
                                                        "The intended set holds no grant satisfying declared authority %s."
                                                        (String.Join(", ", missing)))
                                                    current
                                        | [] ->
                                            return
                                                { Kind = ComponentParticipantRevisionKind.Revised
                                                  InForce =
                                                    Some
                                                        { Admissions = current
                                                          Grants = grants
                                                          Lifecycle = active.Lifecycle
                                                          Failure = None }
                                                  CurrentAuthority = current
                                                  Unrenewed = []
                                                  Replacement = None
                                                  Code = "participant-set-revised"
                                                  Reason =
                                                    sprintf
                                                        "The participant set now holds %d participants and %d grants, still covering every declared dependency."
                                                        current.Length
                                                        grants.Length }
            | _ ->
                return
                    { Kind = ComponentParticipantRevisionKind.ActivationUnavailable
                      InForce = None
                      CurrentAuthority = []
                      Unrenewed = []
                      Replacement = None
                      Code = "active-authority-unavailable"
                      Reason =
                        "CBI9 requires one released Active CBI6 result with a completely admitted participant set." }
        }

type ComponentObservedInteraction =
    { Operation: PortableOperationRef
      Result: InteractionResult }

type ComponentOperationAuthorityMapping =
    { Operation: PortableOperationRef
      DeclaredAuthority: string }

[<RequireQualifiedAccess>]
type ComponentInteractionVerdictKind =
    | Consistent
    | UndeclaredUse
    | UngrantedUse
    | RetirementFailed
    | Declined
    | ActivationUnavailable

type ComponentInteractionVerdict =
    { Kind: ComponentInteractionVerdictKind
      Runtime: ActivationRuntimeOutcome option
      Exercises: BindingExerciseDeclaration list
      Unexercised: string list
      Uncovered: string list
      Replacement: ReplacementRecord option
      Code: string
      Reason: string }

/// Verifies a CBI9 declaration against what the member actually did, through CM4 binding exercises
/// projected from observed portable interactions.
///
/// The admission fact of each projected exercise is derived from the declaration and the grants in
/// force, so CM4's own rule — delivery cannot succeed when the external authority check denied it —
/// is what condemns use outside the declaration. The caller supplies observations and an attribution
/// mapping, never an admission.
[<RequireQualifiedAccess>]
module ComponentInteractionVerification =
    let private ordinal (left: string) (right: string) = String.CompareOrdinal(left, right)

    let private verdict kind code reason =
        { Kind = kind
          Runtime = None
          Exercises = []
          Unexercised = []
          Uncovered = []
          Replacement = None
          Code = code
          Reason = reason }

    /// Attributes every delivered interaction to a declared authority, or to none.
    ///
    /// No frame, no exercise: a locally denied request reached no provider. Any emitted frame
    /// counts, because the receiving domain cannot know what a frame the provider already saw
    /// caused.
    let internal attribute
        (attribution: ComponentOperationAuthorityMapping list)
        (observations: ComponentObservedInteraction list)
        =
        observations
        |> List.filter (fun item -> item.Result.FrameDecision <> FrameDecision.None)
        |> List.map (fun item ->
            attribution
            |> List.tryFind (fun entry -> entry.Operation = item.Operation)
            |> Option.map _.DeclaredAuthority)

    let verify
        (resolution: ResolutionOutcome)
        (selection: ComponentBindingSelection)
        (active: ComponentParticipantAdmissionResult)
        (dependency: ComponentGrantDependency)
        (attribution: ComponentOperationAuthorityMapping list)
        (observations: ComponentObservedInteraction list)
        (runtimeRequest: ActivationRuntimeRequest)
        retirementReason
        =
        task {
            if String.IsNullOrWhiteSpace retirementReason then
                invalidArg (nameof retirementReason) "retirement reason is required"

            match ComponentParticipantAdmission.isActive active, active.Lifecycle with
            | true, Some { Member = Some memberValue } ->
                match ComponentParticipantRevision.declarationShape resolution selection dependency with
                | Error(code, reason) ->
                    return verdict ComponentInteractionVerdictKind.Declined code reason
                | Ok() ->
                    let repeated =
                        attribution
                        |> List.map (fun entry -> PortableOperationRef.text entry.Operation)
                        |> ComponentParticipantAdmission.firstDuplicate
                    match
                        repeated,
                        ComponentBindingLifecycle.trySupportedGroup
                            runtimeRequest.Plan
                            selection.Occurrence
                    with
                    | Some operation, _ ->
                        return
                            verdict
                                ComponentInteractionVerdictKind.Declined
                                "operation-mapping-not-distinct"
                                (sprintf
                                    "Operation '%s' is attributed to more than one declared authority."
                                    operation)
                    | None, None ->
                        return
                            verdict
                                ComponentInteractionVerdictKind.Declined
                                "plan-unsupported"
                                "CBI10 projects exercises onto the one protocol-free activation group CBI2 activated."
                    | None, Some group ->
                        let declaredNames =
                            dependency.Entries |> List.map _.DeclaredAuthority |> Set.ofList
                        let uncoveredNames =
                            ComponentParticipantRevision.uncovered dependency active.Grants
                            |> Set.ofList
                        let attributed = attribute attribution observations
                        let admitted name =
                            match name with
                            | Some value ->
                                Set.contains value declaredNames
                                && not (Set.contains value uncoveredNames)
                            | None -> false
                        let projectExercise index name : BindingExerciseDeclaration =
                            { Exercise =
                                BindingExerciseId.create (
                                    sprintf "exercise.observed-%d" (index + 1))
                              Binding =
                                BindingId.create (
                                    sprintf "binding.%s" (OccurrenceId.value selection.Occurrence))
                              Consumer = selection.Occurrence
                              Provider = selection.Occurrence
                              Source = SourceId.create "source.portable-observation"
                              Exposure = BindingExposureKind.Distinct
                              Mediation = None
                              Routing =
                                RoutingDecisionId.create (sprintf "routing.observed-%d" (index + 1))
                              AuthorityAdmitted = admitted name
                              Delivery = BindingDeliveryResult.Delivered
                              Failure = None }
                        let exercises = attributed |> List.mapi projectExercise
                        let runtime =
                            FakeActivationRuntime.activate
                                { runtimeRequest with
                                    StageOutcomes =
                                        ComponentBindingLifecycle.stageOutcomes
                                            group
                                            selection.Occurrence
                                            None
                                    BindingExercises = exercises }
                        let exercised =
                            attributed
                            |> List.choose id
                            |> List.filter (fun name -> Set.contains name declaredNames)
                            |> Set.ofList
                        let unexercised =
                            dependency.Entries
                            |> List.map _.DeclaredAuthority
                            |> List.filter (fun name -> not (Set.contains name exercised))
                            |> List.sortWith ordinal
                        let uncoveredList = uncoveredNames |> Set.toList |> List.sortWith ordinal
                        let undeclared =
                            attributed
                            |> List.exists (fun name ->
                                match name with
                                | Some value -> not (Set.contains value declaredNames)
                                | None -> true)
                        let ungranted =
                            attributed
                            |> List.exists (fun name ->
                                match name with
                                | Some value -> Set.contains value uncoveredNames
                                | None -> false)
                        if undeclared || ungranted then
                            let! retired =
                                ComponentParticipantRevalidation.tryRetire
                                    memberValue
                                    retirementReason
                            match retired with
                            | Ok replacement ->
                                return
                                    { Kind =
                                        if undeclared then
                                            ComponentInteractionVerdictKind.UndeclaredUse
                                        else
                                            ComponentInteractionVerdictKind.UngrantedUse
                                      Runtime = Some runtime
                                      Exercises = exercises
                                      Unexercised = unexercised
                                      Uncovered = uncoveredList
                                      Replacement = Some replacement
                                      Code =
                                        if undeclared then
                                            "interaction-undeclared"
                                        else
                                            "interaction-ungranted"
                                      Reason =
                                        if undeclared then
                                            "A delivered interaction could not be attributed to any authority the Component declared."
                                        else
                                            "A delivered interaction exercised declared authority no participant holds a grant for." }
                            | Error detail ->
                                return
                                    { Kind = ComponentInteractionVerdictKind.RetirementFailed
                                      Runtime = Some runtime
                                      Exercises = exercises
                                      Unexercised = unexercised
                                      Uncovered = uncoveredList
                                      Replacement = None
                                      Code = "authority-retirement-failed"
                                      Reason = detail }
                        else
                            return
                                { Kind = ComponentInteractionVerdictKind.Consistent
                                  Runtime = Some runtime
                                  Exercises = exercises
                                  Unexercised = unexercised
                                  Uncovered = uncoveredList
                                  Replacement = None
                                  Code = "interaction-consistent"
                                  Reason =
                                    sprintf
                                        "%d delivered interaction(s) stayed inside the declaration."
                                        exercises.Length }
            | _ ->
                return
                    verdict
                        ComponentInteractionVerdictKind.ActivationUnavailable
                        "active-authority-unavailable"
                        "CBI10 requires one released Active CBI6 result with a completely admitted participant set."
        }

[<RequireQualifiedAccess>]
type ComponentDeclarationSuccessionKind =
    | Narrowed
    | Declined
    | ActivationUnavailable

type ComponentDeclarationSuccessionResult =
    { Kind: ComponentDeclarationSuccessionKind
      Declaration: ComponentGrantDependency option
      Dropped: string list
      Vetoed: string list
      Code: string
      Reason: string }

/// Narrows the declaration in force to a successor resolution of the same position, unless observed
/// use vetoes it.
///
/// Absence of use never justifies removing a dependency, so the permission comes from the
/// Component's own re-declaration and observation appears only as a veto. Nothing here retires a
/// member or changes the participant set; narrowing only changes what a later CBI9 revision will
/// admit.
[<RequireQualifiedAccess>]
module ComponentDeclarationSuccession =
    let private ordinal (left: string) (right: string) = String.CompareOrdinal(left, right)

    let private decline (declaration: ComponentGrantDependency) code reason =
        { Kind = ComponentDeclarationSuccessionKind.Declined
          Declaration = Some declaration
          Dropped = []
          Vetoed = []
          Code = code
          Reason = reason }

    let internal samePosition
        (successor: ResolutionOutcome)
        (selection: ComponentBindingSelection)
        (memberValue: CompositionMember)
        =
        match successor with
        | ResolutionOutcome.Resolved(_, generation) ->
            let matches =
                generation.ProviderSets
                |> List.filter (fun item -> item.Requirement = selection.Requirement)
            match matches with
            | [ providerSet ] ->
                if
                    providerSet.Cardinality.Minimum <> 1
                    || providerSet.Cardinality.Maximum <> Some 1
                    || providerSet.Exposure <> ProviderExposure.Distinct
                    || providerSet.Mediation.IsSome
                    || providerSet.Members.Length <> 1
                then
                    Some
                        "The successor position is not the direct 1..1 distinct position this member was bound under."
                else
                    let successorMember = List.exactlyOne providerSet.Members
                    if
                        successorMember.Definition = selection.Definition
                        && successorMember.Occurrence = selection.Occurrence
                        && memberValue.TryFact "bindingScope"
                           = Some(
                               Brontide.Minimal.Experimental.ComponentManagement.BindingScopeId.value
                                   providerSet.Scope)
                    then
                        None
                    else
                        Some
                            "The successor resolves a different definition, occurrence, or binding scope than the live member."
            | _ ->
                Some(
                    sprintf
                        "The successor generation contains %d provider positions for the requested requirement."
                        matches.Length)
        | _ -> Some "The successor resolution did not complete a generation."

    let succeed
        (resolution: ResolutionOutcome)
        (successor: ResolutionOutcome)
        (selection: ComponentBindingSelection)
        (active: ComponentParticipantAdmissionResult)
        (declaration: ComponentGrantDependency)
        (successorDeclaration: ComponentGrantDependency)
        (attribution: ComponentOperationAuthorityMapping list)
        (observations: ComponentObservedInteraction list)
        =
        match ComponentParticipantAdmission.isActive active, active.Lifecycle with
        | true, Some { Member = Some memberValue } ->
            match
                ComponentParticipantRevision.declarationShape resolution selection declaration,
                ComponentParticipantRevision.declarationShape successor selection successorDeclaration
            with
            | Error(code, reason), _
            | Ok(), Error(code, reason) -> decline declaration code reason
            | Ok(), Ok() ->
                match samePosition successor selection memberValue with
                | Some mismatch -> decline declaration "successor-position-mismatch" mismatch
                | None ->
                    let names =
                        declaration.Entries |> List.map _.DeclaredAuthority |> Set.ofList
                    let successorNames =
                        successorDeclaration.Entries |> List.map _.DeclaredAuthority |> Set.ofList
                    if not (Set.isProperSubset successorNames names) then
                        decline
                            declaration
                            "declaration-not-narrower"
                            "Succession only narrows: the successor must declare strictly fewer authorities, all of them already declared."
                    else
                        let tupleOf (entries: ComponentGrantDependencyEntry list) name =
                            entries
                            |> List.tryFind (fun entry -> entry.DeclaredAuthority = name)
                            |> Option.map ComponentParticipantRevision.entryTuple
                        let repointed =
                            successorDeclaration.Entries
                            |> List.filter (fun entry ->
                                tupleOf declaration.Entries entry.DeclaredAuthority
                                <> Some(ComponentParticipantRevision.entryTuple entry))
                            |> List.map _.DeclaredAuthority
                            |> List.sortWith ordinal
                        let repeated =
                            attribution
                            |> List.map (fun entry -> PortableOperationRef.text entry.Operation)
                            |> ComponentParticipantAdmission.firstDuplicate
                        match repointed, repeated with
                        | (_ :: _), _ ->
                            decline
                                declaration
                                "declaration-tuple-changed"
                                (sprintf
                                    "Succession removes dependencies; it does not re-point them. %s would change tuple."
                                    (String.Join(", ", repointed)))
                        | [], Some operation ->
                            decline
                                declaration
                                "operation-mapping-not-distinct"
                                (sprintf
                                    "Operation '%s' is attributed to more than one declared authority."
                                    operation)
                        | [], None ->
                            let dropped =
                                Set.difference names successorNames
                                |> Set.toList
                                |> List.sortWith ordinal
                            let exercised =
                                ComponentInteractionVerification.attribute attribution observations
                                |> List.choose id
                                |> Set.ofList
                            let vetoed =
                                dropped |> List.filter (fun name -> Set.contains name exercised)
                            match vetoed with
                            | (_ :: _) ->
                                { Kind = ComponentDeclarationSuccessionKind.Declined
                                  Declaration = Some declaration
                                  Dropped = []
                                  Vetoed = vetoed
                                  Code = "declaration-use-vetoed"
                                  Reason =
                                    sprintf
                                        "The member has already exercised %s, so the successor cannot narrow it away."
                                        (String.Join(", ", vetoed)) }
                            | [] ->
                                { Kind = ComponentDeclarationSuccessionKind.Narrowed
                                  Declaration = Some successorDeclaration
                                  Dropped = dropped
                                  Vetoed = []
                                  Code = "declaration-narrowed"
                                  Reason =
                                    sprintf
                                        "The declaration in force no longer includes %s."
                                        (String.Join(", ", dropped)) }
        | _ ->
            { Kind = ComponentDeclarationSuccessionKind.ActivationUnavailable
              Declaration = None
              Dropped = []
              Vetoed = []
              Code = "active-authority-unavailable"
              Reason =
                "CBI11 requires one released Active CBI6 result with a completely admitted participant set." }

type ComponentGroupMember =
    { Selection: ComponentBindingSelection
      Conversation: IPortableProviderConversation }

[<RequireQualifiedAccess>]
type ComponentGroupActivationFailureKind =
    | PlanUnsupported
    | PreparationUnavailable
    | RuntimeRefusedBeforeStart
    | MemberEstablishmentRefused
    | MemberReleaseRefused

type ComponentGroupActivationFailure =
    { Kind: ComponentGroupActivationFailureKind
      Code: string
      Reason: string
      Member: OccurrenceId option }

type ComponentGroupMemberOutcome =
    { Occurrence: OccurrenceId
      Member: CompositionMember }

type ComponentGroupActivationResult =
    { Runtime: ActivationRuntimeOutcome option
      Members: ComponentGroupMemberOutcome list
      Failure: ComponentGroupActivationFailure option }

/// Activates several independent members under one CM4 activation, with the release barrier at the
/// activation rather than at any one member.
///
/// CM4 models one logical Release for an activation attempt, so ordinary interaction opens for every
/// member at once or for none; the answer comes from the runtime's shape rather than from a choice
/// made here. Cyclic groups are refused: a multi-member group is a strongly connected component,
/// which is what Relational Initialisation exists for.
[<RequireQualifiedAccess>]
module ComponentGroupLifecycle =
    let private ordinal (left: string) (right: string) = String.CompareOrdinal(left, right)

    let private refuse kind code reason memberValue =
        { Runtime = None
          Members = []
          Failure =
            Some
                { Kind = kind
                  Code = code
                  Reason = reason
                  Member = memberValue } }

    let private portableError error =
        match error with
        | PortableError.Refused fault -> fault.LocalCode, fault.Message
        | PortableError.Interrupted failure -> "portable-process-interrupted", failure.Message

    let isActive (result: ComponentGroupActivationResult) =
        result.Failure.IsNone
        && result.Runtime
           |> Option.exists (fun runtime -> runtime.Kind = ActivationRuntimeOutcomeKind.Active)
        && not result.Members.IsEmpty
        && result.Members |> List.forall _.Member.IsReleased

    let internal supportedPlanFor (plan: ActivationGroupPlan) (occurrences: OccurrenceId list) =
        let selected = occurrences |> Set.ofList
        selected.Count = occurrences.Length
        && plan.Groups.Length = occurrences.Length
        && plan.Groups
           |> List.forall (fun group ->
               group.Members.Length = 1
               && group.Protocols.IsEmpty
               && Set.contains (List.exactlyOne group.Members).Occurrence selected)

    let private supportedPlan (plan: ActivationGroupPlan) (members: ComponentGroupMember list) =
        supportedPlanFor plan (members |> List.map _.Selection.Occurrence)

    let internal groupStageOutcomes (plan: ActivationGroupPlan) failedMember failedStage =
        plan.Groups
        |> List.collect (fun group ->
            group.Members
            |> List.collect (fun groupMember ->
                ComponentBindingLifecycle.stageOutcomes
                    group
                    groupMember.Occurrence
                    (if Some groupMember.Occurrence = failedMember then failedStage else None)))

    let rec private establish
        (remaining: (ComponentGroupMember * ComponentGroupMemberOutcome) list)
        =
        task {
            match remaining with
            | [] -> return None
            | (entry, outcome) :: rest ->
                let! interconnected = outcome.Member.Interconnect entry.Conversation
                match interconnected with
                | Error error ->
                    let code, reason = portableError error
                    return Some(outcome.Occurrence, ActivationStage.Interconnection, code, reason)
                | Ok() when not outcome.Member.IsReady ->
                    return
                        Some(
                            outcome.Occurrence,
                            ActivationStage.Ready,
                            "ready-missing",
                            "Portable Interconnection completed without a Ready lifecycle state.")
                | Ok() -> return! establish rest
        }

    let activate
        (resolution: ResolutionOutcome)
        (members: ComponentGroupMember list)
        (runtimeRequest: ActivationRuntimeRequest)
        =
        task {
            let ordered =
                members
                |> List.sortWith (fun left right ->
                    ordinal
                        (OccurrenceId.value left.Selection.Occurrence)
                        (OccurrenceId.value right.Selection.Occurrence))
            if not (supportedPlan runtimeRequest.Plan ordered) then
                return
                    refuse
                        ComponentGroupActivationFailureKind.PlanUnsupported
                        "plan-unsupported"
                        "CBI12 activates one protocol-free single-member group per selected occurrence, and no others."
                        None
            else
                let prepared =
                    ordered
                    |> List.map (fun entry ->
                        entry, ComponentBindingIntegration.prepare resolution entry.Selection)
                let refusedPreparation =
                    prepared
                    |> List.tryPick (fun (entry, preparation) ->
                        match preparation with
                        | ComponentBindingIntegrationResult.Refused failure ->
                            Some(entry.Selection.Occurrence, failure)
                        | _ -> None)
                match refusedPreparation with
                | Some(occurrence, failure) ->
                    return
                        refuse
                            ComponentGroupActivationFailureKind.PreparationUnavailable
                            failure.Code
                            failure.Reason
                            (Some occurrence)
                | None ->
                    let established =
                        prepared
                        |> List.map (fun (entry, preparation) ->
                            match preparation with
                            | ComponentBindingIntegrationResult.Prepared portable ->
                                entry,
                                { Occurrence = entry.Selection.Occurrence
                                  Member = portable }
                            | ComponentBindingIntegrationResult.Refused _ ->
                                failwith "preparation was already checked")
                    let outcomes = established |> List.map snd
                    let successful =
                        { runtimeRequest with
                            StageOutcomes = groupStageOutcomes runtimeRequest.Plan None None }
                    let preflight = FakeActivationRuntime.activate successful
                    if preflight.Kind <> ActivationRuntimeOutcomeKind.Active then
                        return
                            { Runtime = Some preflight
                              Members = outcomes
                              Failure =
                                Some
                                    { Kind =
                                        ComponentGroupActivationFailureKind.RuntimeRefusedBeforeStart
                                      Code = "runtime-refused-before-start"
                                      Reason =
                                        sprintf
                                            "CM4 refused the derived activation before provider establishment: %A."
                                            preflight.Kind
                                      Member = None } }
                    else
                        let! establishment = establish established
                        match establishment with
                        | Some(failedOccurrence, stage, code, reason) ->
                            let cleanup = ResizeArray<string>()
                            for outcome in outcomes do
                                let stageToken = CompositionStage.token outcome.Member.Stage
                                if stageToken <> "local-initialisation" && stageToken <> "retired" then
                                    let! retired =
                                        ComponentParticipantRevalidation.tryRetire
                                            outcome.Member
                                            (sprintf
                                                "activation failed at %s"
                                                (OccurrenceId.value failedOccurrence))
                                    match retired with
                                    | Ok _ -> ()
                                    | Error detail ->
                                        cleanup.Add(
                                            sprintf
                                                "%s: %s"
                                                (OccurrenceId.value outcome.Occurrence)
                                                detail)
                            let runtime =
                                FakeActivationRuntime.activate
                                    { runtimeRequest with
                                        StageOutcomes =
                                            groupStageOutcomes
                                                runtimeRequest.Plan
                                                (Some failedOccurrence)
                                                (Some stage) }
                            return
                                { Runtime = Some runtime
                                  Members = outcomes
                                  Failure =
                                    Some
                                        { Kind =
                                            ComponentGroupActivationFailureKind.MemberEstablishmentRefused
                                          Code = code
                                          Reason =
                                            if cleanup.Count = 0 then
                                                reason
                                            else
                                                sprintf
                                                    "%s Cleanup also failed for %s."
                                                    reason
                                                    (String.Join("; ", cleanup))
                                          Member = Some failedOccurrence } }
                        | None ->
                            let runtime = FakeActivationRuntime.activate successful
                            if runtime.Kind <> ActivationRuntimeOutcomeKind.Active then
                                return
                                    { Runtime = Some runtime
                                      Members = outcomes
                                      Failure =
                                        Some
                                            { Kind =
                                                ComponentGroupActivationFailureKind.RuntimeRefusedBeforeStart
                                              Code = "runtime-state-changed"
                                              Reason =
                                                sprintf
                                                    "CM4 no longer accepted the activation after every member reported Ready: %A."
                                                    runtime.Kind
                                              Member = None } }
                            else
                                // The barrier: every member reached Ready and CM4 accepted the
                                // activation, so ordinary interaction opens for all of them
                                // together.
                                let released =
                                    outcomes
                                    |> List.tryPick (fun outcome ->
                                        match outcome.Member.Release() with
                                        | Ok() -> None
                                        | Error error ->
                                            let code, reason = portableError error
                                            Some(outcome.Occurrence, code, reason))
                                match released with
                                | Some(occurrence, code, reason) ->
                                    return
                                        { Runtime = Some runtime
                                          Members = outcomes
                                          Failure =
                                            Some
                                                { Kind =
                                                    ComponentGroupActivationFailureKind.MemberReleaseRefused
                                                  Code = code
                                                  Reason = reason
                                                  Member = Some occurrence } }
                                | None ->
                                    return
                                        { Runtime = Some runtime
                                          Members = outcomes
                                          Failure = None }
        }

type ComponentGroupParticipant =
    { Member: ComponentGroupMember
      Participants: ComponentParticipantRequest list }

[<RequireQualifiedAccess>]
type ComponentGroupAuthorityFailureKind =
    | IdentityNotDistinct
    | MemberAuthorityRefused
    | ActorMappingInconsistent
    | ActivationRefused

type ComponentGroupAuthorityFailure =
    { Kind: ComponentGroupAuthorityFailureKind
      Code: string
      Reason: string
      Member: OccurrenceId option }

type ComponentGroupMemberAdmission =
    { Occurrence: OccurrenceId
      Participants: ComponentParticipantObservation list
      Grants: LocalCapabilityGrant list }

type ComponentGroupAuthorityResult =
    { Admissions: ComponentGroupMemberAdmission list
      Grants: LocalCapabilityGrant list
      Lifecycle: ComponentGroupActivationResult option
      Failure: ComponentGroupAuthorityFailure option }

/// Admits a participant set per member, then activates the members together.
///
/// Authority is admitted against an occurrence rather than an activation attempt, because an
/// occurrence is durable and an attempt is not. The authority barrier is therefore earlier than the
/// release barrier rather than the same one: every set is admitted before any provider is contacted,
/// and Release still waits for every member to reach Ready.
[<RequireQualifiedAccess>]
module ComponentGroupAuthority =
    let private ordinal (left: string) (right: string) = String.CompareOrdinal(left, right)

    let isActive (result: ComponentGroupAuthorityResult) =
        result.Failure.IsNone
        && result.Lifecycle |> Option.exists ComponentGroupLifecycle.isActive

    /// Across the activation, one participant holds one local Actor and one local Actor is held by
    /// one participant.
    ///
    /// The same party participating in two members is legitimate and must map consistently; two
    /// parties arriving at one local Actor is the conflation CBI6 refuses within a set, and it is no
    /// less a conflation across members.
    let internal actorMapping (admissions: ComponentGroupMemberAdmission list) =
        let observations =
            admissions
            |> List.collect _.Participants
            |> List.sortWith (fun left right ->
                ordinal (ActorId.value left.Participant) (ActorId.value right.Participant))
        let rec walk byParticipant byLocalActor remaining =
            match remaining with
            | [] -> None
            | (observation: ComponentParticipantObservation) :: rest ->
                let local =
                    observation.Authority.Observation.Relationships
                    |> List.exactlyOne
                    |> fun relationship -> relationship.LocalActor
                match Map.tryFind observation.Participant byParticipant with
                | Some existing when existing <> local ->
                    Some(
                        "participant-actor-not-single",
                        sprintf
                            "Participant '%s' is mapped onto both '%s' and '%s' in one activation."
                            (ActorId.value observation.Participant)
                            (LocalActorReferenceId.value existing)
                            (LocalActorReferenceId.value local))
                | _ ->
                    match Map.tryFind local byLocalActor with
                    | Some holder when holder <> observation.Participant ->
                        Some(
                            "local-actor-shared-across-members",
                            sprintf
                                "Participants '%s' and '%s' are both mapped onto the receiving-domain Actor '%s'."
                                (ActorId.value holder)
                                (ActorId.value observation.Participant)
                                (LocalActorReferenceId.value local))
                    | _ ->
                        walk
                            (Map.add observation.Participant local byParticipant)
                            (Map.add local observation.Participant byLocalActor)
                            rest
        walk Map.empty Map.empty observations

    let activate
        (resolution: ResolutionOutcome)
        (members: ComponentGroupParticipant list)
        (runtimeRequest: ActivationRuntimeRequest)
        =
        task {
            let ordered =
                members
                |> List.sortWith (fun left right ->
                    ordinal
                        (OccurrenceId.value left.Member.Selection.Occurrence)
                        (OccurrenceId.value right.Member.Selection.Occurrence))
            let requests =
                ordered |> List.collect (fun entry -> entry.Participants |> List.map _.Request)
            match ComponentParticipantAdmission.distinctIdentities requests with
            | Error(code, reason) ->
                return
                    { Admissions = []
                      Grants = []
                      Lifecycle = None
                      Failure =
                        Some
                            { Kind = ComponentGroupAuthorityFailureKind.IdentityNotDistinct
                              Code = code
                              Reason = reason
                              Member = None } }
            | Ok() ->
                // Every set is admitted before any member is prepared: CM5 evaluation is
                // effect-free, so a refusal here costs nothing to undo.
                let steps =
                    ordered
                    |> List.map (fun entry ->
                        entry,
                        ComponentParticipantAdmission.admit
                            entry.Member.Selection
                            entry.Participants
                            runtimeRequest)
                let refused =
                    steps
                    |> List.tryPick (fun (entry, step) ->
                        step.Failure
                        |> Option.map (fun failure -> entry.Member.Selection.Occurrence, failure))
                let admitted =
                    steps
                    |> List.filter (fun (_, step) -> step.Failure.IsNone)
                    |> List.map (fun (entry, step) ->
                        { Occurrence = entry.Member.Selection.Occurrence
                          Participants = step.Admissions
                          Grants = step.Grants })
                match refused with
                | Some(occurrence, failure) ->
                    return
                        { Admissions = admitted
                          Grants = []
                          Lifecycle = None
                          Failure =
                            Some
                                { Kind = ComponentGroupAuthorityFailureKind.MemberAuthorityRefused
                                  Code = failure.Code
                                  Reason = failure.Reason
                                  Member = Some occurrence } }
                | None ->
                    match actorMapping admitted with
                    | Some(code, reason) ->
                        return
                            { Admissions = admitted
                              Grants = []
                              Lifecycle = None
                              Failure =
                                Some
                                    { Kind =
                                        ComponentGroupAuthorityFailureKind.ActorMappingInconsistent
                                      Code = code
                                      Reason = reason
                                      Member = None } }
                    | None ->
                        let grants =
                            admitted
                            |> List.collect _.Grants
                            |> List.sortWith (fun left right ->
                                ordinal
                                    (CapabilityGrantId.value left.Grant)
                                    (CapabilityGrantId.value right.Grant))
                        let! lifecycle =
                            ComponentGroupLifecycle.activate
                                resolution
                                (ordered |> List.map _.Member)
                                runtimeRequest
                        if ComponentGroupLifecycle.isActive lifecycle then
                            return
                                { Admissions = admitted
                                  Grants = grants
                                  Lifecycle = Some lifecycle
                                  Failure = None }
                        else
                            return
                                { Admissions = admitted
                                  Grants = grants
                                  Lifecycle = Some lifecycle
                                  Failure =
                                    Some
                                        { Kind =
                                            ComponentGroupAuthorityFailureKind.ActivationRefused
                                          Code =
                                            lifecycle.Failure
                                            |> Option.map _.Code
                                            |> Option.defaultValue "activation-not-active"
                                          Reason =
                                            lifecycle.Failure
                                            |> Option.map _.Reason
                                            |> Option.defaultValue
                                                "CBI12 did not release every member."
                                          Member =
                                            lifecycle.Failure |> Option.bind _.Member } }
        }

type ComponentGroupMemberRequests =
    { Occurrence: OccurrenceId
      Requests: AuthorityAdmissionRequest list }

[<RequireQualifiedAccess>]
type ComponentGroupRevalidationKind =
    | Continued
    | Withdrawn
    | RetirementFailed
    | ActivationUnavailable

type ComponentGroupMemberRevalidation =
    { Occurrence: OccurrenceId
      CurrentAuthority: ComponentParticipantObservation list
      Unrenewed: ActorId list }

type ComponentGroupRevalidationResult =
    { Kind: ComponentGroupRevalidationKind
      Members: ComponentGroupMemberRevalidation list
      Lapsed: OccurrenceId list
      Replacements: ReplacementRecord list
      Code: string
      Reason: string }

/// Revalidates every member's authority and retires the whole activation when any of it lapses.
///
/// A CM4 activation has one restart scope and every member is inside it, and CM4 models no way to
/// retire one member while its scope keeps running — that is a scoped replacement, a different
/// operation. The members came up together inside one scope, so they go down together. Their being
/// otherwise independent is about what they need from each other, not about what scope they share.
[<RequireQualifiedAccess>]
module ComponentGroupRevalidation =
    let private ordinal (left: string) (right: string) = String.CompareOrdinal(left, right)

    let private byParticipant (requests: AuthorityAdmissionRequest list) =
        requests
        |> List.sortWith (fun left right ->
            ordinal (ActorId.value left.Participant) (ActorId.value right.Participant))

    let private retireAll
        (lifecycle: ComponentGroupActivationResult)
        retirementReason
        code
        reason
        members
        lapsed
        =
        task {
            let replacements = ResizeArray<ReplacementRecord>()
            let cleanup = ResizeArray<string>()
            for outcome in lifecycle.Members do
                let! retired =
                    ComponentParticipantRevalidation.tryRetire outcome.Member retirementReason
                match retired with
                | Ok replacement -> replacements.Add replacement
                | Error detail ->
                    cleanup.Add(
                        sprintf "%s: %s" (OccurrenceId.value outcome.Occurrence) detail)
            if cleanup.Count = 0 then
                return
                    { Kind = ComponentGroupRevalidationKind.Withdrawn
                      Members = members
                      Lapsed = lapsed
                      Replacements = List.ofSeq replacements
                      Code = code
                      Reason = reason }
            else
                return
                    { Kind = ComponentGroupRevalidationKind.RetirementFailed
                      Members = members
                      Lapsed = lapsed
                      Replacements = List.ofSeq replacements
                      Code = "authority-retirement-failed"
                      Reason = String.Join("; ", cleanup) }
        }

    let revalidate
        (active: ComponentGroupAuthorityResult)
        (requests: ComponentGroupMemberRequests list)
        retirementReason
        =
        task {
            if String.IsNullOrWhiteSpace retirementReason then
                invalidArg (nameof retirementReason) "retirement reason is required"

            match ComponentGroupAuthority.isActive active, active.Lifecycle with
            | true, Some lifecycle ->
                let prior = active.Admissions
                let ordered =
                    requests
                    |> List.sortWith (fun left right ->
                        ordinal (OccurrenceId.value left.Occurrence) (OccurrenceId.value right.Occurrence))
                if (ordered |> List.map _.Occurrence) <> (prior |> List.map _.Occurrence) then
                    return!
                        retireAll
                            lifecycle
                            retirementReason
                            "member-set-changed"
                            "The fresh requests do not name the same members the activation admitted."
                            []
                            []
                else
                    let paired = List.zip prior ordered
                    let mismatched =
                        paired
                        |> List.tryFind (fun (priorMember, member') ->
                            member'.Requests.Length <> priorMember.Participants.Length
                            || List.zip priorMember.Participants (byParticipant member'.Requests)
                               |> List.exists (fun (admitted, request) ->
                                   not (
                                       ComponentParticipantRevalidation.matchesPrior admitted request)))
                    match mismatched with
                    | Some(_, member') ->
                        return!
                            retireAll
                                lifecycle
                                retirementReason
                                "authority-revalidation-mismatch"
                                (sprintf
                                    "A fresh request for member %s does not identify the authority that admitted it."
                                    (OccurrenceId.value member'.Occurrence))
                                []
                                []
                    | None ->
                        let members =
                            paired
                            |> List.map (fun (priorMember, member') ->
                                let current =
                                    byParticipant member'.Requests
                                    |> List.map (fun request ->
                                        { Participant = request.Participant
                                          Authority = FakeAuthorityAdmission.evaluate request })
                                let unrenewed =
                                    List.zip priorMember.Participants current
                                    |> List.filter (fun (admitted, observation) ->
                                        not (
                                            ComponentParticipantRevalidation.isSameAdmission
                                                admitted.Authority
                                                observation.Authority))
                                    |> List.map (fun (_, observation) -> observation.Participant)
                                { Occurrence = member'.Occurrence
                                  CurrentAuthority = current
                                  Unrenewed = unrenewed })
                        let lapsed =
                            members
                            |> List.filter (fun member' -> not member'.Unrenewed.IsEmpty)
                            |> List.map _.Occurrence
                        if not lapsed.IsEmpty then
                            return!
                                retireAll
                                    lifecycle
                                    retirementReason
                                    "authority-not-renewed"
                                    (sprintf
                                        "The receiving domain no longer admits the identical authority for %s."
                                        (String.Join(", ", lapsed |> List.map OccurrenceId.value)))
                                    members
                                    lapsed
                        else
                            return
                                { Kind = ComponentGroupRevalidationKind.Continued
                                  Members = members
                                  Lapsed = []
                                  Replacements = []
                                  Code = "authority-current"
                                  Reason =
                                    "Every member still holds the identical receiving-domain authority the activation admitted." }
            | _ ->
                return
                    { Kind = ComponentGroupRevalidationKind.ActivationUnavailable
                      Members = []
                      Lapsed = []
                      Replacements = []
                      Code = "active-authority-unavailable"
                      Reason = "CBI14 requires one released CBI13 activation with every member admitted." }
        }

type ComponentGroupMemberRevision =
    { Occurrence: OccurrenceId
      Selection: ComponentBindingSelection
      Dependency: ComponentGrantDependency
      Requests: AuthorityAdmissionRequest list }

[<RequireQualifiedAccess>]
type ComponentGroupRevisionKind =
    | Revised
    | Declined
    | Withdrawn
    | RetirementFailed
    | ActivationUnavailable

type ComponentGroupRevisionResult =
    { Kind: ComponentGroupRevisionKind
      InForce: ComponentGroupAuthorityResult option
      CurrentAuthority: ComponentParticipantObservation list
      Lapsed: OccurrenceId list
      Code: string
      Reason: string }

/// Revises the participant sets of a multi-member activation under per-member declarations.
///
/// A change is decided per member, because admission is about an occurrence, and checked against the
/// activation, because CBI13's identity and Actor-mapping rules are activation-wide. A declined
/// change is local and alters nothing; a lapse discovered while evaluating is CBI14's case and
/// retires the whole activation, which shares a restart scope.
[<RequireQualifiedAccess>]
module ComponentGroupRevision =
    let private ordinal (left: string) (right: string) = String.CompareOrdinal(left, right)

    let private decline (active: ComponentGroupAuthorityResult) code reason current =
        { Kind = ComponentGroupRevisionKind.Declined
          InForce = Some active
          CurrentAuthority = current
          Lapsed = []
          Code = code
          Reason = reason }

    let private setChanged
        (prior: ComponentGroupMemberAdmission)
        (intended: ComponentGroupMemberRevision)
        =
        let names (values: ActorId list) =
            values |> List.map ActorId.value |> List.sortWith ordinal
        names (intended.Requests |> List.map _.Participant)
        <> names (prior.Participants |> List.map _.Participant)

    let private retire
        (lifecycle: ComponentGroupActivationResult)
        retirementReason
        current
        lapsed
        reason
        =
        task {
            let cleanup = ResizeArray<string>()
            for outcome in lifecycle.Members do
                let! retired =
                    ComponentParticipantRevalidation.tryRetire outcome.Member retirementReason
                match retired with
                | Ok _ -> ()
                | Error detail ->
                    cleanup.Add(sprintf "%s: %s" (OccurrenceId.value outcome.Occurrence) detail)
            if cleanup.Count = 0 then
                return
                    { Kind = ComponentGroupRevisionKind.Withdrawn
                      InForce = None
                      CurrentAuthority = current
                      Lapsed = lapsed
                      Code = "authority-not-renewed"
                      Reason = reason }
            else
                return
                    { Kind = ComponentGroupRevisionKind.RetirementFailed
                      InForce = None
                      CurrentAuthority = current
                      Lapsed = lapsed
                      Code = "authority-retirement-failed"
                      Reason = String.Join("; ", cleanup) }
        }

    let revise
        (resolution: ResolutionOutcome)
        (active: ComponentGroupAuthorityResult)
        (members: ComponentGroupMemberRevision list)
        retirementReason
        =
        task {
            if String.IsNullOrWhiteSpace retirementReason then
                invalidArg (nameof retirementReason) "retirement reason is required"

            match ComponentGroupAuthority.isActive active, active.Lifecycle with
            | true, Some lifecycle ->
                let prior = active.Admissions
                let ordered =
                    members
                    |> List.sortWith (fun left right ->
                        ordinal (OccurrenceId.value left.Occurrence) (OccurrenceId.value right.Occurrence))
                if (ordered |> List.map _.Occurrence) <> (prior |> List.map _.Occurrence) then
                    // Naming the wrong members is a malformed request, not evidence about authority,
                    // so the activation is left exactly as it was. CBI14 retires in the same
                    // situation because a revalidation asserts continuity it then cannot
                    // demonstrate.
                    return
                        decline
                            active
                            "member-set-changed"
                            "The revision does not name the members this activation admitted."
                            []
                elif
                    List.zip prior ordered
                    |> List.forall (fun (priorMember, member') -> not (setChanged priorMember member'))
                then
                    return
                        decline
                            active
                            "activation-unchanged"
                            "No member's participant set differs; revalidating what is in force is CBI14."
                            []
                else
                    let paired = List.zip prior ordered
                    let declarationProblem =
                        paired
                        |> List.tryPick (fun (priorMember, member') ->
                            match
                                ComponentParticipantRevision.declarationShape
                                    resolution
                                    member'.Selection
                                    member'.Dependency
                            with
                            | Error(code, reason) -> Some(code, reason)
                            | Ok() ->
                                match
                                    ComponentParticipantRevision.uncovered
                                        member'.Dependency
                                        priorMember.Grants
                                with
                                | [] -> None
                                | missing ->
                                    Some(
                                        "dependency-unsatisfied",
                                        sprintf
                                            "Member %s does not cover declared authority %s."
                                            (OccurrenceId.value member'.Occurrence)
                                            (String.Join(", ", missing))))
                    match declarationProblem with
                    | Some(code, reason) -> return decline active code reason []
                    | None ->
                        let intended = ordered |> List.collect _.Requests
                        match ComponentParticipantAdmission.distinctIdentities intended with
                        | Error(code, reason) -> return decline active code reason []
                        | Ok() ->
                            let malformedSet =
                                ordered
                                |> List.tryFind (fun member' ->
                                    member'.Requests.IsEmpty
                                    || (member'.Requests
                                        |> List.map (fun request -> ActorId.value request.Participant)
                                        |> ComponentParticipantAdmission.firstDuplicate)
                                       |> Option.isSome)
                            match malformedSet with
                            | Some member' ->
                                return
                                    decline
                                        active
                                        "participant-set-invalid"
                                        (sprintf
                                            "Member %s must keep at least one participant, each named once."
                                            (OccurrenceId.value member'.Occurrence))
                                        []
                            | None ->
                                let admittedOf (priorMember: ComponentGroupMemberAdmission) participant =
                                    priorMember.Participants
                                    |> List.tryFind (fun item -> item.Participant = participant)
                                let drifted =
                                    paired
                                    |> List.tryFind (fun (priorMember, member') ->
                                        member'.Requests
                                        |> List.exists (fun request ->
                                            match admittedOf priorMember request.Participant with
                                            | Some admitted ->
                                                not (
                                                    ComponentParticipantRevalidation.matchesPrior
                                                        admitted
                                                        request)
                                            | None -> false))
                                match drifted with
                                | Some(_, member') ->
                                    return
                                        decline
                                            active
                                            "authority-revalidation-mismatch"
                                            (sprintf
                                                "A retained request for member %s does not identify the authority that admitted it."
                                                (OccurrenceId.value member'.Occurrence))
                                            []
                                | None ->
                                    let evaluated =
                                        paired
                                        |> List.map (fun (priorMember, member') ->
                                            priorMember,
                                            member',
                                            member'.Requests
                                            |> List.sortWith (fun left right ->
                                                ordinal
                                                    (ActorId.value left.Participant)
                                                    (ActorId.value right.Participant))
                                            |> List.map (fun request ->
                                                { Participant = request.Participant
                                                  Authority =
                                                    FakeAuthorityAdmission.evaluate request }))
                                    let current =
                                        evaluated
                                        |> List.collect (fun (_, _, observations) -> observations)
                                    let lapsed =
                                        evaluated
                                        |> List.filter (fun (priorMember, _, observations) ->
                                            observations
                                            |> List.exists (fun observation ->
                                                match
                                                    admittedOf priorMember observation.Participant
                                                with
                                                | Some admitted ->
                                                    not (
                                                        ComponentParticipantRevalidation.isSameAdmission
                                                            admitted.Authority
                                                            observation.Authority)
                                                | None -> false))
                                        |> List.map (fun (_, member', _) -> member'.Occurrence)
                                    if not lapsed.IsEmpty then
                                        // A lapse is CBI14's case, not this one: the activation
                                        // shares a restart scope.
                                        return!
                                            retire
                                                lifecycle
                                                retirementReason
                                                current
                                                lapsed
                                                (sprintf
                                                    "The receiving domain no longer admits the identical authority for %s."
                                                    (String.Join(
                                                        ", ",
                                                        lapsed |> List.map OccurrenceId.value)))
                                    else
                                        let refused =
                                            evaluated
                                            |> List.collect (fun (priorMember, member', observations) ->
                                                List.zip
                                                    (member'.Requests
                                                     |> List.sortWith (fun left right ->
                                                         ordinal
                                                             (ActorId.value left.Participant)
                                                             (ActorId.value right.Participant)))
                                                    observations
                                                |> List.filter (fun (request, observation) ->
                                                    (admittedOf priorMember request.Participant).IsNone
                                                    && not (
                                                        ComponentParticipantAdmission.isExactAdmission
                                                            request
                                                            observation.Authority))
                                                |> List.map (fun (request, _) ->
                                                    ActorId.value request.Participant))
                                        if not refused.IsEmpty then
                                            return
                                                decline
                                                    active
                                                    "authority-not-admitted"
                                                    (sprintf
                                                        "CM5 did not admit the exact submitted authority for %s."
                                                        (String.Join(", ", refused)))
                                                    current
                                        else
                                            let revised =
                                                evaluated
                                                |> List.map (fun (_, member', observations) ->
                                                    { Occurrence = member'.Occurrence
                                                      Participants = observations
                                                      Grants =
                                                        observations
                                                        |> List.collect (fun observation ->
                                                            observation.Authority.Observation.Grants)
                                                        |> List.sortWith (fun left right ->
                                                            ordinal
                                                                (CapabilityGrantId.value left.Grant)
                                                                (CapabilityGrantId.value right.Grant)) })
                                            match ComponentGroupAuthority.actorMapping revised with
                                            | Some(code, reason) ->
                                                return decline active code reason current
                                            | None ->
                                                let uncoveredMember =
                                                    List.zip ordered revised
                                                    |> List.tryPick (fun (member', admission) ->
                                                        match
                                                            ComponentParticipantRevision.uncovered
                                                                member'.Dependency
                                                                admission.Grants
                                                        with
                                                        | [] -> None
                                                        | missing ->
                                                            Some(member'.Occurrence, missing))
                                                match uncoveredMember with
                                                | Some(occurrence, missing) ->
                                                    return
                                                        decline
                                                            active
                                                            "dependency-not-covered"
                                                            (sprintf
                                                                "Member %s would hold no grant satisfying declared authority %s."
                                                                (OccurrenceId.value occurrence)
                                                                (String.Join(", ", missing)))
                                                            current
                                                | None ->
                                                    let grants =
                                                        revised
                                                        |> List.collect _.Grants
                                                        |> List.sortWith (fun left right ->
                                                            ordinal
                                                                (CapabilityGrantId.value left.Grant)
                                                                (CapabilityGrantId.value right.Grant))
                                                    return
                                                        { Kind = ComponentGroupRevisionKind.Revised
                                                          InForce =
                                                            Some
                                                                { active with
                                                                    Admissions = revised
                                                                    Grants = grants }
                                                          CurrentAuthority = current
                                                          Lapsed = []
                                                          Code = "activation-revised"
                                                          Reason =
                                                            sprintf
                                                                "%d members now hold %d participants."
                                                                revised.Length
                                                                (revised
                                                                 |> List.sumBy (fun item ->
                                                                     item.Participants.Length)) }
            | _ ->
                return
                    { Kind = ComponentGroupRevisionKind.ActivationUnavailable
                      InForce = None
                      CurrentAuthority = []
                      Lapsed = []
                      Code = "active-authority-unavailable"
                      Reason = "CBI15 requires one released CBI13 activation with every member admitted." }
        }

type ComponentGroupMemberInteractions =
    { Selection: ComponentBindingSelection
      Dependency: ComponentGrantDependency
      Attribution: ComponentOperationAuthorityMapping list
      Observations: ComponentObservedInteraction list }

[<RequireQualifiedAccess>]
type ComponentGroupVerificationKind =
    | Consistent
    | UndeclaredUse
    | UngrantedUse
    | RetirementFailed
    | Declined
    | ActivationUnavailable

type ComponentGroupMemberVerification =
    { Occurrence: OccurrenceId
      Exercises: BindingExerciseDeclaration list
      Unexercised: string list
      Uncovered: string list
      UndeclaredUse: bool
      UngrantedUse: bool }

type ComponentGroupVerificationResult =
    { Kind: ComponentGroupVerificationKind
      Runtime: ActivationRuntimeOutcome option
      Members: ComponentGroupMemberVerification list
      Violating: OccurrenceId list
      Replacements: ReplacementRecord list
      Code: string
      Reason: string }

/// Verifies every member's declaration against what that member actually did, through one CM4
/// request carrying the whole activation's projected binding exercises.
///
/// A CBI12 activation is one CM4 request, so one member's undeclared use condemns all of them: CM4
/// refuses the request on the first offending exercise rather than excusing the members that
/// behaved. The answer comes from the runtime's shape, as CBI12's release barrier did, and agrees
/// with CBI14's separate reason that the activation shares a restart scope. Attribution stays per
/// member, because the declaration is per member.
[<RequireQualifiedAccess>]
module ComponentGroupVerification =
    let private ordinal (left: string) (right: string) = String.CompareOrdinal(left, right)

    let isViolating (verification: ComponentGroupMemberVerification) =
        verification.UndeclaredUse || verification.UngrantedUse

    let isConsistent (result: ComponentGroupVerificationResult) =
        result.Kind = ComponentGroupVerificationKind.Consistent

    let exercises (result: ComponentGroupVerificationResult) =
        result.Members |> List.collect _.Exercises

    let private decline kind code reason =
        { Kind = kind
          Runtime = None
          Members = []
          Violating = []
          Replacements = []
          Code = code
          Reason = reason }

    /// Projects one member's observations, deriving each exercise's admission from that member's own
    /// declaration and its own grants.
    ///
    /// Exercise identity carries the occurrence because CM4 refuses a request with a repeated
    /// binding-exercise identity, and the whole activation now shares one request.
    let private project
        (memberValue: ComponentGroupMemberInteractions)
        (grants: LocalCapabilityGrant list)
        =
        let occurrence = memberValue.Selection.Occurrence
        let declaredNames =
            memberValue.Dependency.Entries |> List.map _.DeclaredAuthority |> Set.ofList
        let uncoveredNames =
            ComponentParticipantRevision.uncovered memberValue.Dependency grants |> Set.ofList
        let attributed =
            ComponentInteractionVerification.attribute
                memberValue.Attribution
                memberValue.Observations
        let projectExercise index name : BindingExerciseDeclaration =
            { Exercise =
                BindingExerciseId.create (
                    sprintf "exercise.observed.%s.%d" (OccurrenceId.value occurrence) (index + 1))
              Binding = BindingId.create (sprintf "binding.%s" (OccurrenceId.value occurrence))
              Consumer = occurrence
              Provider = occurrence
              Source = SourceId.create "source.portable-observation"
              Exposure = BindingExposureKind.Distinct
              Mediation = None
              Routing =
                RoutingDecisionId.create (
                    sprintf "routing.observed.%s.%d" (OccurrenceId.value occurrence) (index + 1))
              AuthorityAdmitted =
                match name with
                | Some value ->
                    Set.contains value declaredNames && not (Set.contains value uncoveredNames)
                | None -> false
              Delivery = BindingDeliveryResult.Delivered
              Failure = None }
        let exercised =
            attributed
            |> List.choose id
            |> List.filter (fun name -> Set.contains name declaredNames)
            |> Set.ofList
        { Occurrence = occurrence
          Exercises = attributed |> List.mapi projectExercise
          Unexercised =
            memberValue.Dependency.Entries
            |> List.map _.DeclaredAuthority
            |> List.filter (fun name -> not (Set.contains name exercised))
            |> List.sortWith ordinal
          Uncovered = uncoveredNames |> Set.toList |> List.sortWith ordinal
          UndeclaredUse =
            attributed
            |> List.exists (fun name ->
                match name with
                | Some value -> not (Set.contains value declaredNames)
                | None -> true)
          UngrantedUse =
            attributed
            |> List.exists (fun name ->
                match name with
                | Some value -> Set.contains value uncoveredNames
                | None -> false) }

    let private structure
        (resolution: ResolutionOutcome)
        (ordered: ComponentGroupMemberInteractions list)
        =
        ordered
        |> List.tryPick (fun memberValue ->
            match
                ComponentParticipantRevision.declarationShape
                    resolution
                    memberValue.Selection
                    memberValue.Dependency
            with
            | Error(code, reason) -> Some(code, reason)
            | Ok() ->
                // Distinct within a member only: two Components may both expose an Operation of the
                // same name, and each attributes it against its own declaration.
                memberValue.Attribution
                |> List.map (fun entry -> PortableOperationRef.text entry.Operation)
                |> ComponentParticipantAdmission.firstDuplicate
                |> Option.map (fun operation ->
                    "operation-mapping-not-distinct",
                    sprintf
                        "Member %s attributes Operation '%s' to more than one declared authority."
                        (OccurrenceId.value memberValue.Selection.Occurrence)
                        operation))

    let verify
        (resolution: ResolutionOutcome)
        (active: ComponentGroupAuthorityResult)
        (members: ComponentGroupMemberInteractions list)
        (runtimeRequest: ActivationRuntimeRequest)
        retirementReason
        =
        task {
            if String.IsNullOrWhiteSpace retirementReason then
                invalidArg (nameof retirementReason) "retirement reason is required"

            match ComponentGroupAuthority.isActive active, active.Lifecycle with
            | true, Some lifecycle ->
                let prior = active.Admissions
                let ordered =
                    members
                    |> List.sortWith (fun left right ->
                        ordinal
                            (OccurrenceId.value left.Selection.Occurrence)
                            (OccurrenceId.value right.Selection.Occurrence))
                if (ordered |> List.map _.Selection.Occurrence) <> (prior |> List.map _.Occurrence) then
                    return
                        decline
                            ComponentGroupVerificationKind.Declined
                            "member-set-changed"
                            "The verification does not name the members this activation admitted."
                else
                    match structure resolution ordered with
                    | Some(code, reason) ->
                        return decline ComponentGroupVerificationKind.Declined code reason
                    | None ->
                        if
                            not (
                                ComponentGroupLifecycle.supportedPlanFor
                                    runtimeRequest.Plan
                                    (prior |> List.map _.Occurrence)
                            )
                        then
                            return
                                decline
                                    ComponentGroupVerificationKind.Declined
                                    "plan-unsupported"
                                    "CBI16 projects exercises onto the protocol-free activation groups CBI12 activated."
                        else
                            let verified =
                                List.zip ordered prior
                                |> List.map (fun (memberValue, admission) ->
                                    project memberValue admission.Grants)
                            // One request, one verdict: every member's exercises are judged together.
                            let runtime =
                                FakeActivationRuntime.activate
                                    { runtimeRequest with
                                        StageOutcomes =
                                            ComponentGroupLifecycle.groupStageOutcomes
                                                runtimeRequest.Plan
                                                None
                                                None
                                        BindingExercises =
                                            verified |> List.collect _.Exercises }
                            let violating =
                                verified |> List.filter isViolating |> List.map _.Occurrence
                            if violating.IsEmpty then
                                return
                                    { Kind = ComponentGroupVerificationKind.Consistent
                                      Runtime = Some runtime
                                      Members = verified
                                      Violating = []
                                      Replacements = []
                                      Code = "interaction-consistent"
                                      Reason =
                                        sprintf
                                            "%d delivered interaction(s) across %d members stayed inside their declarations."
                                            (verified |> List.sumBy (fun item -> item.Exercises.Length))
                                            verified.Length }
                            else
                                let undeclared = verified |> List.exists _.UndeclaredUse
                                let replacements = ResizeArray<ReplacementRecord>()
                                let cleanup = ResizeArray<string>()
                                for outcome in lifecycle.Members do
                                    let! retired =
                                        ComponentParticipantRevalidation.tryRetire
                                            outcome.Member
                                            retirementReason
                                    match retired with
                                    | Ok replacement -> replacements.Add replacement
                                    | Error detail ->
                                        cleanup.Add(
                                            sprintf
                                                "%s: %s"
                                                (OccurrenceId.value outcome.Occurrence)
                                                detail)
                                let named =
                                    String.Join(", ", violating |> List.map OccurrenceId.value)
                                if cleanup.Count = 0 then
                                    return
                                        { Kind =
                                            if undeclared then
                                                ComponentGroupVerificationKind.UndeclaredUse
                                            else
                                                ComponentGroupVerificationKind.UngrantedUse
                                          Runtime = Some runtime
                                          Members = verified
                                          Violating = violating
                                          Replacements = List.ofSeq replacements
                                          Code =
                                            if undeclared then
                                                "interaction-undeclared"
                                            else
                                                "interaction-ungranted"
                                          Reason =
                                            if undeclared then
                                                sprintf
                                                    "A delivered interaction of %s could not be attributed to any authority that member declared."
                                                    named
                                            else
                                                sprintf
                                                    "A delivered interaction of %s exercised declared authority no participant of that member holds a grant for."
                                                    named }
                                else
                                    return
                                        { Kind = ComponentGroupVerificationKind.RetirementFailed
                                          Runtime = Some runtime
                                          Members = verified
                                          Violating = violating
                                          Replacements = List.ofSeq replacements
                                          Code = "authority-retirement-failed"
                                          Reason = String.Join("; ", cleanup) }
            | _ ->
                return
                    decline
                        ComponentGroupVerificationKind.ActivationUnavailable
                        "active-authority-unavailable"
                        "CBI16 requires one released CBI13 activation with every member admitted."
        }

[<RequireQualifiedAccess>]
type ComponentGroupExtensionKind =
    | Extended
    | Declined
    | Withdrawn
    | RetirementFailed
    | ActivationUnavailable

type ComponentGroupExtensionResult =
    { Kind: ComponentGroupExtensionKind
      InForce: ComponentGroupAuthorityResult option
      CurrentAuthority: ComponentParticipantObservation list
      Grown: OccurrenceId list
      Lapsed: OccurrenceId list
      Code: string
      Reason: string }

/// Grows the participant sets of a multi-member activation while every member stays released.
///
/// No resolution and no declaration are taken, and the absent parameters are the contract: growth
/// removes nobody, coverage is monotone in the grants held, so a member holding a declaration is
/// grown by the same rule as one holding none and the two may sit in one activation. What is checked
/// against the whole activation is CBI13's identity and Actor-mapping rules, which an addition is a
/// fresh opportunity to violate against members already live.
[<RequireQualifiedAccess>]
module ComponentGroupExtension =
    let private ordinal (left: string) (right: string) = String.CompareOrdinal(left, right)

    let isExtended (result: ComponentGroupExtensionResult) =
        result.Kind = ComponentGroupExtensionKind.Extended

    let private decline (active: ComponentGroupAuthorityResult) code reason current =
        { Kind = ComponentGroupExtensionKind.Declined
          InForce = Some active
          CurrentAuthority = current
          Grown = []
          Lapsed = []
          Code = code
          Reason = reason }

    /// Checks one member's intended set: retains everyone, repeats nobody, adds soundly.
    let private structure
        (prior: ComponentGroupMemberAdmission)
        (intended: ComponentGroupMemberRequests)
        =
        let repeated =
            intended.Requests
            |> List.map (fun request -> ActorId.value request.Participant)
            |> ComponentParticipantAdmission.firstDuplicate
        match repeated with
        | Some participant ->
            Some(
                "participant-not-distinct",
                sprintf
                    "Participant '%s' appears in more than one request for member %s."
                    participant
                    (OccurrenceId.value intended.Occurrence))
        | None ->
            let missing =
                prior.Participants
                |> List.filter (fun existing ->
                    intended.Requests
                    |> List.forall (fun request -> request.Participant <> existing.Participant))
                |> List.map (fun existing -> ActorId.value existing.Participant)
            if not missing.IsEmpty then
                Some(
                    "participant-not-retained",
                    sprintf
                        "CBI18 only grows a set. Removing or substituting %s in member %s requires CBI15 under a declaration, or CBI14 retirement and a fresh admission."
                        (String.Join(", ", missing))
                        (OccurrenceId.value intended.Occurrence))
            else
                let added =
                    intended.Requests
                    |> List.filter (fun request ->
                        prior.Participants
                        |> List.forall (fun existing -> existing.Participant <> request.Participant))
                if added |> List.forall ComponentParticipantAdmission.supportedShape then
                    None
                else
                    Some(
                        "authority-shape-unsupported",
                        "CBI18 supports one ComponentParticipant relationship per added participant and distinct narrow authority tuples dependent on it.")

    let private retire
        (lifecycle: ComponentGroupActivationResult)
        retirementReason
        current
        lapsed
        =
        task {
            let cleanup = ResizeArray<string>()
            for outcome in lifecycle.Members do
                let! retired =
                    ComponentParticipantRevalidation.tryRetire outcome.Member retirementReason
                match retired with
                | Ok _ -> ()
                | Error detail ->
                    cleanup.Add(sprintf "%s: %s" (OccurrenceId.value outcome.Occurrence) detail)
            if cleanup.Count = 0 then
                return
                    { Kind = ComponentGroupExtensionKind.Withdrawn
                      InForce = None
                      CurrentAuthority = current
                      Grown = []
                      Lapsed = lapsed
                      Code = "authority-not-renewed"
                      Reason =
                        sprintf
                            "The receiving domain no longer admits the identical authority for %s."
                            (String.Join(", ", lapsed |> List.map OccurrenceId.value)) }
            else
                return
                    { Kind = ComponentGroupExtensionKind.RetirementFailed
                      InForce = None
                      CurrentAuthority = current
                      Grown = []
                      Lapsed = lapsed
                      Code = "authority-retirement-failed"
                      Reason = String.Join("; ", cleanup) }
        }

    let extend
        (active: ComponentGroupAuthorityResult)
        (members: ComponentGroupMemberRequests list)
        retirementReason
        =
        task {
            if String.IsNullOrWhiteSpace retirementReason then
                invalidArg (nameof retirementReason) "retirement reason is required"

            match ComponentGroupAuthority.isActive active, active.Lifecycle with
            | true, Some lifecycle ->
                let prior = active.Admissions
                let ordered =
                    members
                    |> List.sortWith (fun left right ->
                        ordinal (OccurrenceId.value left.Occurrence) (OccurrenceId.value right.Occurrence))
                if (ordered |> List.map _.Occurrence) <> (prior |> List.map _.Occurrence) then
                    return
                        { Kind = ComponentGroupExtensionKind.Declined
                          InForce = Some active
                          CurrentAuthority = []
                          Grown = []
                          Lapsed = []
                          Code = "member-set-changed"
                          Reason = "The extension does not name the members this activation admitted." }
                else
                    let paired = List.zip prior ordered
                    let structural =
                        paired
                        |> List.tryPick (fun (priorMember, intended) -> structure priorMember intended)
                    match structural with
                    | Some(code, reason) -> return decline active code reason []
                    | None ->
                        // A member that gains nobody restates its own set; an activation that gains
                        // nobody is a revalidation and belongs to CBI14.
                        let grows =
                            paired
                            |> List.exists (fun (priorMember, intended) ->
                                intended.Requests.Length > priorMember.Participants.Length)
                        if not grows then
                            return
                                decline
                                    active
                                    "activation-unchanged"
                                    "No member gains a participant; revalidating what is in force is CBI14."
                                    []
                        else
                            let intended = ordered |> List.collect _.Requests
                            match ComponentParticipantAdmission.distinctIdentities intended with
                            | Error(code, reason) -> return decline active code reason []
                            | Ok() ->
                                let admittedOf (priorMember: ComponentGroupMemberAdmission) participant =
                                    priorMember.Participants
                                    |> List.tryFind (fun item -> item.Participant = participant)
                                let drifted =
                                    paired
                                    |> List.tryFind (fun (priorMember, member') ->
                                        member'.Requests
                                        |> List.exists (fun request ->
                                            match admittedOf priorMember request.Participant with
                                            | Some admitted ->
                                                not (
                                                    ComponentParticipantRevalidation.matchesPrior
                                                        admitted
                                                        request)
                                            | None -> false))
                                match drifted with
                                | Some(_, member') ->
                                    // Nothing was evaluated, so nothing was learned: a malformed
                                    // request is not evidence that the retained authority is gone.
                                    return
                                        decline
                                            active
                                            "authority-revalidation-mismatch"
                                            (sprintf
                                                "A retained request for member %s does not identify the authority that admitted it."
                                                (OccurrenceId.value member'.Occurrence))
                                            []
                                | None ->
                                    let evaluated =
                                        paired
                                        |> List.map (fun (priorMember, member') ->
                                            priorMember,
                                            member',
                                            member'.Requests
                                            |> List.sortWith (fun left right ->
                                                ordinal
                                                    (ActorId.value left.Participant)
                                                    (ActorId.value right.Participant))
                                            |> List.map (fun request ->
                                                { Participant = request.Participant
                                                  Authority =
                                                    FakeAuthorityAdmission.evaluate request }))
                                    let current =
                                        evaluated
                                        |> List.collect (fun (_, _, observations) -> observations)
                                    let lapsed =
                                        evaluated
                                        |> List.filter (fun (priorMember, _, observations) ->
                                            observations
                                            |> List.exists (fun observation ->
                                                match
                                                    admittedOf priorMember observation.Participant
                                                with
                                                | Some admitted ->
                                                    not (
                                                        ComponentParticipantRevalidation.isSameAdmission
                                                            admitted.Authority
                                                            observation.Authority)
                                                | None -> false))
                                        |> List.map (fun (_, member', _) -> member'.Occurrence)
                                    if not lapsed.IsEmpty then
                                        // A lapse outranks any problem with an addition, and retires
                                        // the whole activation: the members share one restart scope,
                                        // so they share a fate.
                                        return! retire lifecycle retirementReason current lapsed
                                    else
                                        let refused =
                                            evaluated
                                            |> List.collect (fun (priorMember, member', observations) ->
                                                observations
                                                |> List.filter (fun observation ->
                                                    (admittedOf priorMember observation.Participant)
                                                        .IsNone
                                                    && (member'.Requests
                                                        |> List.tryFind (fun request ->
                                                            request.Participant = observation.Participant)
                                                        |> Option.map (fun request ->
                                                            not (
                                                                ComponentParticipantAdmission.isExactAdmission
                                                                    request
                                                                    observation.Authority))
                                                        |> Option.defaultValue false))
                                                |> List.map (fun observation ->
                                                    ActorId.value observation.Participant))
                                        if not refused.IsEmpty then
                                            return
                                                decline
                                                    active
                                                    "authority-not-admitted"
                                                    (sprintf
                                                        "CM5 did not admit the exact submitted authority for %s."
                                                        (String.Join(", ", refused)))
                                                    current
                                        else
                                            let extended =
                                                evaluated
                                                |> List.map (fun (_, member', observations) ->
                                                    { Occurrence = member'.Occurrence
                                                      Participants = observations
                                                      Grants =
                                                        observations
                                                        |> List.collect (fun observation ->
                                                            observation.Authority.Observation.Grants)
                                                        |> List.sortWith (fun left right ->
                                                            ordinal
                                                                (CapabilityGrantId.value left.Grant)
                                                                (CapabilityGrantId.value right.Grant)) })
                                            // The permitting direction matters here: a party already
                                            // participating in another member may be added to a
                                            // second, and must arrive at the local Actor it holds.
                                            match ComponentGroupAuthority.actorMapping extended with
                                            | Some(code, reason) ->
                                                return decline active code reason current
                                            | None ->
                                                let grown =
                                                    evaluated
                                                    |> List.filter (fun (priorMember, _, observations) ->
                                                        let held = priorMember.Participants.Length
                                                        observations.Length > held)
                                                    |> List.map (fun (_, member', _) ->
                                                        member'.Occurrence)
                                                let grants =
                                                    extended
                                                    |> List.collect _.Grants
                                                    |> List.sortWith (fun left right ->
                                                        ordinal
                                                            (CapabilityGrantId.value left.Grant)
                                                            (CapabilityGrantId.value right.Grant))
                                                return
                                                    { Kind = ComponentGroupExtensionKind.Extended
                                                      InForce =
                                                        Some
                                                            { active with
                                                                Admissions = extended
                                                                Grants = grants }
                                                      CurrentAuthority = current
                                                      Grown = grown
                                                      Lapsed = []
                                                      Code = "participant-set-extended"
                                                      Reason =
                                                        sprintf
                                                            "%d of %d members grew; the activation now holds %d participants."
                                                            grown.Length
                                                            extended.Length
                                                            (extended
                                                             |> List.sumBy (fun item ->
                                                                 item.Participants.Length)) }
            | _ ->
                return
                    { Kind = ComponentGroupExtensionKind.ActivationUnavailable
                      InForce = None
                      CurrentAuthority = []
                      Grown = []
                      Lapsed = []
                      Code = "active-authority-unavailable"
                      Reason = "CBI18 requires one released CBI13 activation with every member admitted." }
        }

type ComponentGroupMemberSuccession =
    { Selection: ComponentBindingSelection
      Declaration: ComponentGrantDependency
      SuccessorDeclaration: ComponentGrantDependency
      Attribution: ComponentOperationAuthorityMapping list
      Observations: ComponentObservedInteraction list }

[<RequireQualifiedAccess>]
type ComponentGroupSuccessionKind =
    | Narrowed
    | Declined
    | ActivationUnavailable

type ComponentGroupMemberDeclaration =
    { Occurrence: OccurrenceId
      Declaration: ComponentGrantDependency
      Dropped: string list
      Vetoed: string list }

type ComponentGroupSuccessionResult =
    { Kind: ComponentGroupSuccessionKind
      Members: ComponentGroupMemberDeclaration list
      Narrowed: OccurrenceId list
      Vetoing: OccurrenceId list
      Code: string
      Reason: string }

/// Narrows every member's declaration to one successor generation, unless any member's observed use
/// vetoes it.
///
/// The permission is a generation, and a CM2 generation is one immutable object resolving every
/// position at once, so a succession is one transaction: applying the members it narrows while
/// refusing the rest would leave the activation holding declarations from two generations. A member
/// the successor does not narrow is untouched rather than refused, which is the case CBI11's single
/// rule could not distinguish. Nothing here retires a member or touches a participant set, which is
/// why it returns without a task.
[<RequireQualifiedAccess>]
module ComponentGroupSuccession =
    let private ordinal (left: string) (right: string) = String.CompareOrdinal(left, right)

    let isNarrowed (result: ComponentGroupSuccessionResult) =
        result.Kind = ComponentGroupSuccessionKind.Narrowed

    let private names (declaration: ComponentGrantDependency) =
        declaration.Entries |> List.map _.DeclaredAuthority |> Set.ofList

    let private decline (members: ComponentGroupMemberSuccession list) code reason =
        { Kind = ComponentGroupSuccessionKind.Declined
          Members =
            members
            |> List.map (fun memberValue ->
                { Occurrence = memberValue.Selection.Occurrence
                  Declaration = memberValue.Declaration
                  Dropped = []
                  Vetoed = [] })
          Narrowed = []
          Vetoing = []
          Code = code
          Reason = reason }

    /// Checks one member's pair of declarations and its successor position, without asking what the
    /// member has exercised.
    let private structure
        (resolution: ResolutionOutcome)
        (successor: ResolutionOutcome)
        (memberValue: ComponentGroupMemberSuccession)
        (portable: CompositionMember)
        =
        match
            ComponentParticipantRevision.declarationShape
                resolution
                memberValue.Selection
                memberValue.Declaration,
            ComponentParticipantRevision.declarationShape
                successor
                memberValue.Selection
                memberValue.SuccessorDeclaration
        with
        | Error(code, reason), _
        | Ok(), Error(code, reason) -> Some(code, reason)
        | Ok(), Ok() ->
            // A generation that fails this for any member is not a successor of this activation.
            match
                ComponentDeclarationSuccession.samePosition successor memberValue.Selection portable
            with
            | Some mismatch ->
                Some(
                    "successor-position-mismatch",
                    sprintf
                        "%s: %s"
                        (OccurrenceId.value memberValue.Selection.Occurrence)
                        mismatch)
            | None ->
                if
                    not (
                        Set.isSubset
                            (names memberValue.SuccessorDeclaration)
                            (names memberValue.Declaration)
                    )
                then
                    Some(
                        "declaration-not-narrower",
                        sprintf
                            "Member %s would gain declared authority; succession only removes it."
                            (OccurrenceId.value memberValue.Selection.Occurrence))
                else
                    let tupleOf (entries: ComponentGrantDependencyEntry list) name =
                        entries
                        |> List.tryFind (fun entry -> entry.DeclaredAuthority = name)
                        |> Option.map ComponentParticipantRevision.entryTuple
                    let repointed =
                        memberValue.SuccessorDeclaration.Entries
                        |> List.filter (fun entry ->
                            tupleOf memberValue.Declaration.Entries entry.DeclaredAuthority
                            <> Some(ComponentParticipantRevision.entryTuple entry))
                        |> List.map _.DeclaredAuthority
                        |> List.sortWith ordinal
                    let repeated =
                        memberValue.Attribution
                        |> List.map (fun entry -> PortableOperationRef.text entry.Operation)
                        |> ComponentParticipantAdmission.firstDuplicate
                    match repointed, repeated with
                    | (_ :: _), _ ->
                        Some(
                            "declaration-tuple-changed",
                            sprintf
                                "Succession removes dependencies; it does not re-point them. %s: %s would change tuple."
                                (OccurrenceId.value memberValue.Selection.Occurrence)
                                (String.Join(", ", repointed)))
                    | [], Some operation ->
                        Some(
                            "operation-mapping-not-distinct",
                            sprintf
                                "Member %s attributes Operation '%s' to more than one declared authority."
                                (OccurrenceId.value memberValue.Selection.Occurrence)
                                operation)
                    | [], None -> None

    /// Computes what one member would drop, and what its own observed use vetoes.
    ///
    /// Exercised authority is per member, as CBI16 attributes it: one member's interaction cannot
    /// veto another member's narrowing.
    let private evaluate (memberValue: ComponentGroupMemberSuccession) =
        let dropped =
            Set.difference (names memberValue.Declaration) (names memberValue.SuccessorDeclaration)
            |> Set.toList
            |> List.sortWith ordinal
        let exercised =
            ComponentInteractionVerification.attribute
                memberValue.Attribution
                memberValue.Observations
            |> List.choose id
            |> Set.ofList
        memberValue.Selection.Occurrence,
        dropped,
        dropped |> List.filter (fun name -> Set.contains name exercised)

    let succeed
        (resolution: ResolutionOutcome)
        (successor: ResolutionOutcome)
        (active: ComponentGroupAuthorityResult)
        (members: ComponentGroupMemberSuccession list)
        =
        match ComponentGroupAuthority.isActive active, active.Lifecycle with
        | true, Some lifecycle ->
            let ordered =
                members
                |> List.sortWith (fun left right ->
                    ordinal
                        (OccurrenceId.value left.Selection.Occurrence)
                        (OccurrenceId.value right.Selection.Occurrence))
            if
                (ordered |> List.map _.Selection.Occurrence)
                <> (active.Admissions |> List.map _.Occurrence)
            then
                { Kind = ComponentGroupSuccessionKind.Declined
                  Members = []
                  Narrowed = []
                  Vetoing = []
                  Code = "member-set-changed"
                  Reason = "The succession does not name the members this activation admitted." }
            else
                let portableOf occurrence =
                    lifecycle.Members
                    |> List.find (fun outcome -> outcome.Occurrence = occurrence)
                    |> _.Member
                let structural =
                    ordered
                    |> List.tryPick (fun memberValue ->
                        structure
                            resolution
                            successor
                            memberValue
                            (portableOf memberValue.Selection.Occurrence))
                match structural with
                | Some(code, reason) -> decline ordered code reason
                | None ->
                    // Restating what is in force succeeds nothing; a member that restates its own is
                    // untouched. The subset check has already run, so a member whose names differ at
                    // all is one that narrows.
                    let changes =
                        ordered
                        |> List.exists (fun memberValue ->
                            names memberValue.SuccessorDeclaration <> names memberValue.Declaration)
                    if not changes then
                        decline
                            ordered
                            "activation-unchanged"
                            "No member's successor declares fewer authorities, so there is nothing to succeed."
                    else
                        let evaluated = ordered |> List.map evaluate
                        let vetoing =
                            evaluated
                            |> List.filter (fun (_, _, vetoed) -> not vetoed.IsEmpty)
                            |> List.map (fun (occurrence, _, _) -> occurrence)
                        if not vetoing.IsEmpty then
                            // One transaction, so a veto anywhere refuses every member's narrowing.
                            { Kind = ComponentGroupSuccessionKind.Declined
                              Members =
                                List.zip ordered evaluated
                                |> List.map (fun (memberValue, (_, _, vetoed)) ->
                                    { Occurrence = memberValue.Selection.Occurrence
                                      Declaration = memberValue.Declaration
                                      Dropped = []
                                      Vetoed = vetoed })
                              Narrowed = []
                              Vetoing = vetoing
                              Code = "declaration-use-vetoed"
                              Reason =
                                sprintf
                                    "%s has already exercised authority the successor would narrow away."
                                    (String.Join(
                                        ", ",
                                        vetoing |> List.map OccurrenceId.value)) }
                        else
                            let narrowed =
                                evaluated
                                |> List.filter (fun (_, dropped, _) -> not dropped.IsEmpty)
                                |> List.map (fun (occurrence, _, _) -> occurrence)
                            { Kind = ComponentGroupSuccessionKind.Narrowed
                              Members =
                                List.zip ordered evaluated
                                |> List.map (fun (memberValue, (_, dropped, _)) ->
                                    { Occurrence = memberValue.Selection.Occurrence
                                      Declaration = memberValue.SuccessorDeclaration
                                      Dropped = dropped
                                      Vetoed = [] })
                              Narrowed = narrowed
                              Vetoing = []
                              Code = "declaration-narrowed"
                              Reason =
                                sprintf
                                    "%d of %d members narrowed, dropping %d declared authorities."
                                    narrowed.Length
                                    ordered.Length
                                    (evaluated
                                     |> List.sumBy (fun (_, dropped, _) -> dropped.Length)) }
        | _ ->
            { Kind = ComponentGroupSuccessionKind.ActivationUnavailable
              Members = []
              Narrowed = []
              Vetoing = []
              Code = "active-authority-unavailable"
              Reason = "CBI17 requires one released CBI13 activation with every member admitted." }

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
