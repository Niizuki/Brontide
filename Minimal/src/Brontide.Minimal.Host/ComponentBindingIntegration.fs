namespace Brontide.Minimal.Host

open System.Text
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
