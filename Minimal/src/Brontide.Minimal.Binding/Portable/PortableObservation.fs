namespace Brontide.Minimal.Binding.Portable

/// The Channel correlation identities and the host-native Execution they map onto.
///
/// The host-native Execution is a distinct identity space by the Channel identity rule, so it never
/// equals a Channel correlation identity.
type CorrelationMapping =
    { Channel: ChannelId
      Request: ChannelRequestId
      HostNativeExecution: HostExecutionId }

/// Timing is recorded but never drives a semantic decision, so parity excludes it.
type Timing =
    { EstablishedAtElapsedMilliseconds: int64
      RequestElapsedMilliseconds: int64 }

/// The unified observation every completed or rejected interaction reports.
///
/// An observation records what happened; it never decides what happens. Every normative field is
/// present on every terminal observation, including rejections and denials: an unobservable value
/// uses its declared absent form rather than being omitted.
[<StructuralEquality; NoComparison>]
type Observation =
    { SelectedProvider: PortableProviderRef
      SelectionReason: string
      NegotiatedOperations: PortableOperationRef list
      NegotiatedContractVersion: int
      Representation: string
      CrossedBoundaries: string list
      CopyCount: int64
      ReferencedResources: ResourceObservation list
      AuthorityDecisionPoint: AuthorityDecisionPoint
      AuthorityDecision: AuthorityDecision
      MappingObligations: string list
      RetryCount: int64
      Interrupted: bool
      FailureDomain: FailureDomain option
      TerminalStatus: TerminalStatus
      Correlation: CorrelationMapping
      ProviderEffectCount: int64 option
      Timing: Timing
      LocalCode: string option
      LocalMessage: string option }

[<RequireQualifiedAccess>]
module Observation =

    /// Names every way this observation would break the completeness rule. An empty list is the
    /// rule holding; the list exists so a violation is reportable rather than merely absent.
    let completenessFailures observation =
        [ if observation.SelectionReason.Length = 0 then
              "An observation records why the provider was selected."
          if observation.RetryCount <> 0L then
              "Version 0.1 makes no retry promise, so the retry count is always zero."
          if (observation.TerminalStatus = TerminalStatus.Succeeded) = observation.FailureDomain.IsSome then
              "A failure domain is present exactly when the terminal status is not 'succeeded'."
          if List.isEmpty observation.CrossedBoundaries then
              "Crossed boundaries are reported, using 'none' when nothing was crossed."
          if observation.ProviderEffectCount |> Option.exists (fun count -> count < 0L) || observation.CopyCount < 0L then
              "Effect and copy counts are never negative."
          if observation.TerminalStatus = TerminalStatus.Succeeded && observation.ProviderEffectCount.IsNone then
              "A succeeded observation has a known provider effect count."
          if
              HostExecutionId.value observation.Correlation.HostNativeExecution = ChannelRequestId.value
                  observation.Correlation.Request
              || HostExecutionId.value observation.Correlation.HostNativeExecution = ChannelId.value
                  observation.Correlation.Channel
          then
              "The host-native Execution identity is distinct from every Channel correlation identity." ]

    let isComplete observation = List.isEmpty (completenessFailures observation)

    /// The category-level fields two realizations of the same vector must report identically.
    ///
    /// Representation, framing, crossed boundaries, copy accounting, correlation, timing, and local
    /// codes are excluded: the realization is exactly what differs, and copy accounting differs by
    /// realization by design.
    let parityProfile observation =
        Map.ofList
            [ "authorityDecision", AuthorityDecision.token observation.AuthorityDecision
              "authorityDecisionPoint", AuthorityDecisionPoint.token observation.AuthorityDecisionPoint
              "terminalStatus", TerminalStatus.token observation.TerminalStatus
              "failureDomain",
              (match observation.FailureDomain with
               | Some domain -> FailureDomain.token domain
               | None -> "absent")
              "providerEffectCount", (observation.ProviderEffectCount |> Option.map string |> Option.defaultValue "unknown")
              "negotiatedOperations",
              System.String.Join(", ", observation.NegotiatedOperations |> List.map PortableOperationRef.text)
              "negotiatedContractVersion", string observation.NegotiatedContractVersion
              "mappingObligations", System.String.Join(", ", observation.MappingObligations)
              "interrupted", (if observation.Interrupted then "true" else "false")
              "retryCount", string observation.RetryCount
              "referencedResources",
              System.String.Join(
                  "; ",
                  observation.ReferencedResources
                  |> List.map (fun resource ->
                      $"{resource.Flavor}/{resource.Ownership}/{resource.IntegrityVerified}/{resource.Accepted}")
              ) ]

/// One interaction's category-level result together with its observation.
[<StructuralEquality; NoComparison>]
type InteractionResult =
    { FrameDecision: FrameDecision
      ResultClass: ResultClass
      Category: ProtocolCategory option
      ProcessCategory: ProcessCategory option
      Value: PortableValue option
      Observation: Observation }

[<RequireQualifiedAccess>]
module InteractionResult =

    /// The parity profile plus the category-level result the vectors state.
    let parityProfile result =
        Observation.parityProfile result.Observation
        |> Map.add "frameDecision" (FrameDecision.token result.FrameDecision)
        |> Map.add "resultClass" (ResultClass.token result.ResultClass)
        |> Map.add
            "category"
            (match result.Category with
             | Some category -> ProtocolCategory.token category
             | None -> "absent")
        |> Map.add
            "processCategory"
            (match result.ProcessCategory with
             | Some category -> ProcessCategory.token category
             | None -> "absent")

/// Builds observations that satisfy the completeness rule by construction.
[<RequireQualifiedAccess>]
module ObservationBuilder =

    let build
        plan
        terminalStatus
        authorityDecision
        authorityDecisionPoint
        correlation
        failureDomain
        providerEffectCount
        copyCount
        resources
        mappingObligations
        interrupted
        timing
        localCode
        localMessage
        =
        { SelectedProvider = BindingPlan.selectedProvider plan
          SelectionReason = BindingPlan.selectionReason plan
          NegotiatedOperations = BindingPlan.operations plan
          NegotiatedContractVersion = BindingPlan.contractVersion plan
          Representation = BindingPlan.representation plan
          CrossedBoundaries = BindingPlan.crossedBoundaries plan
          CopyCount = copyCount
          ReferencedResources = resources
          AuthorityDecisionPoint = authorityDecisionPoint
          AuthorityDecision = authorityDecision
          MappingObligations = mappingObligations
          RetryCount = 0L
          Interrupted = interrupted
          FailureDomain = failureDomain
          TerminalStatus = terminalStatus
          Correlation = correlation
          ProviderEffectCount = Some providerEffectCount
          Timing = timing
          LocalCode = localCode
          LocalMessage = localMessage }

    /// The denial profile: a local denial produces a complete observation even though no frame was
    /// emitted, and it crosses no boundary because it never left the host.
    let denial plan correlation decision timing reason =
        { SelectedProvider = BindingPlan.selectedProvider plan
          SelectionReason = BindingPlan.selectionReason plan
          NegotiatedOperations = BindingPlan.operations plan
          NegotiatedContractVersion = BindingPlan.contractVersion plan
          Representation = BindingPlan.representation plan
          CrossedBoundaries = [ "none" ]
          CopyCount = 0L
          ReferencedResources = []
          AuthorityDecisionPoint = AuthorityDecisionPoint.HostLocal
          AuthorityDecision = decision
          MappingObligations = []
          RetryCount = 0L
          Interrupted = false
          FailureDomain = Some FailureDomain.LocalEndpoint
          TerminalStatus = TerminalStatus.Denied
          Correlation = correlation
          ProviderEffectCount = Some 0L
          Timing = timing
          LocalCode = Some "local-denial"
          LocalMessage = Some reason }
