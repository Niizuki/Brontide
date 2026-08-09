namespace Brontide.Minimal.Host

open System
open Brontide.Minimal.Binding.Portable
open Brontide.Minimal.Experimental.ComponentManagement

type ProviderServingTrustRevalidationResult =
    { Code: string
      RefusedBy: string
      Revalidated: bool
      Continued: bool
      LaunchPolicyIdentity: ProviderPublisherTrustPolicyId option
      ServingPolicyIdentity: ProviderPublisherTrustPolicyId option
      Authorization: TrustedProviderPublisherAuthorization option
      RetirementCode: string }

type ProviderServingActivation =
    private
        { Chain: ProviderDistributionChainResult
          Lifecycle: ComponentBindingLifecycleResult option
          Occurrence: OccurrenceId
          Resolution: ResolutionOutcome
          Selection: ComponentBindingSelection
          Request: ActivationRuntimeRequest
          mutable RestartState: int }
    member this.OccurrenceId = this.Occurrence
    member internal this.DistributionChain = this.Chain
    member internal this.BindingLifecycle = this.Lifecycle
    member internal this.RetainedResolution = this.Resolution
    member internal this.RetainedSelection = this.Selection
    member internal this.RetainedRequest = this.Request
    member internal this.BeginRestart() =
        lock this (fun () ->
            match this.RestartState with
            | 0 -> this.RestartState <- 1; "restart-claimed"
            | 1 -> "provider-restart-in-progress"
            | _ -> "provider-restart-already-completed")
    member internal this.FinishRestart completed =
        lock this (fun () -> this.RestartState <- if completed then 2 else 0)
    member internal this.Restarted(chain, lifecycle) =
        { Chain = chain
          Lifecycle = Some lifecycle
          Occurrence = this.Occurrence
          Resolution = this.Resolution
          Selection = this.Selection
          Request = this.Request
          RestartState = 0 }
    member this.IsServing =
        this.Chain.Provider |> Option.exists (fun provider -> not provider.HasExited)
        && this.Lifecycle |> Option.exists (fun lifecycle ->
            lifecycle.Failure.IsNone
            && lifecycle.Member |> Option.exists _.IsReleased
            && lifecycle.Runtime |> Option.exists (fun runtime -> runtime.Kind = ActivationRuntimeOutcomeKind.Active))
    member this.MemberReleased =
        this.Lifecycle |> Option.bind _.Member |> Option.exists _.IsReleased
    member this.Retire reason =
        task {
            if String.IsNullOrWhiteSpace reason then invalidArg (nameof reason) "retirement reason is required"
            match this.Lifecycle |> Option.bind _.Member with
            | Some memberValue when memberValue.IsReleased ->
                let! _ = memberValue.Retire reason
                return ()
            | _ -> return ()
        }

/// Takes one current publisher-trust decision for an already released portable member. A lapsed
/// publisher is retired and its concrete provider is terminated; cadence remains a host concern.
[<RequireQualifiedAccess>]
module ProviderServingTrustRevalidation =
    let activate
        (chain: ProviderDistributionChainResult)
        resolution
        selection
        request
        =
        task {
            match chain.Provider with
            | Some provider when chain.Revalidated ->
                let! lifecycle =
                    ComponentBindingLifecycle.activate resolution selection request provider.Conversation
                return
                    { Chain = chain; Lifecycle = Some lifecycle; Occurrence = selection.Occurrence
                      Resolution = resolution; Selection = selection; Request = request; RestartState = 0 }
            | _ ->
                return
                    { Chain = chain; Lifecycle = None; Occurrence = selection.Occurrence
                      Resolution = resolution; Selection = selection; Request = request; RestartState = 0 }
        }

    let private revalidateCore
        (registry: DurableProviderPublisherTrustPolicyRegistry)
        (store: ContentAddressedProviderStore)
        (activation: ProviderServingActivation)
        retirementReason
        removeStagedSet
        =
        task {
            if isNull (box registry) then nullArg (nameof registry)
            if isNull (box store) then nullArg (nameof store)
            if String.IsNullOrWhiteSpace retirementReason then
                invalidArg (nameof retirementReason) "retirement reason is required"

            let chain = activation.Chain

            match
                chain.Provider,
                chain.VerifiedEvidence,
                chain.StagedIdentity,
                chain.PolicyAuthorityIdentity,
                activation.Lifecycle |> Option.bind _.Member
            with
            | Some provider, Some evidence, Some stagedIdentity, Some authority, Some memberValue
                when chain.Revalidated
                     && authority = registry.AuthorityIdentity
                     && not provider.HasExited
                     && memberValue.IsReleased
                     && activation.IsServing ->
                match registry.Current with
                | None ->
                    return
                        { Code = "publisher-trust-policy-unavailable"; RefusedBy = "cbi37"
                          Revalidated = false; Continued = false
                          LaunchPolicyIdentity = chain.LaunchPolicyIdentity
                          ServingPolicyIdentity = None; Authorization = None
                          RetirementCode = "retirement-not-attempted" }
                | Some current ->
                    let trust = ProviderPublisherTrustEvaluator.evaluate current.Policy (Some evidence)
                    match trust.Authorization with
                    | Some authorization ->
                        return
                            { Code = "publisher-trust-current"; RefusedBy = "none"
                              Revalidated = true; Continued = true
                              LaunchPolicyIdentity = chain.LaunchPolicyIdentity
                              ServingPolicyIdentity = Some current.Policy.Identity
                              Authorization = Some authorization
                              RetirementCode = "retirement-not-attempted" }
                    | None ->
                        let! retired = memberValue.Retire retirementReason
                        let mutable retirementCode =
                            match retired with Ok _ -> "retired" | Error _ -> "retirement-failed"
                        provider.Dispose()
                        if removeStagedSet then
                            let removal = store.Remove stagedIdentity
                            if not removal.Removed && removal.Code <> "artifact-set-not-staged" then
                                retirementCode <- $"{retirementCode};{removal.Code}"
                        return
                            { Code = trust.Code; RefusedBy = "cbi35"
                              Revalidated = true; Continued = false
                              LaunchPolicyIdentity = chain.LaunchPolicyIdentity
                              ServingPolicyIdentity = Some current.Policy.Identity
                              Authorization = None; RetirementCode = retirementCode }
            | _ ->
                return
                    { Code = "serving-activation-unavailable"; RefusedBy = "none"
                      Revalidated = false; Continued = false
                      LaunchPolicyIdentity = chain.LaunchPolicyIdentity
                      ServingPolicyIdentity = None; Authorization = None
                      RetirementCode = "retirement-not-attempted" }
        }

    let revalidate registry store activation retirementReason =
        revalidateCore registry store activation retirementReason true

    let internal revalidateForSweep registry store activation retirementReason =
        revalidateCore registry store activation retirementReason false

type ProviderServingTrustSweepMember =
    { Occurrence: OccurrenceId
      Result: ProviderServingTrustRevalidationResult }

type ProviderServingTrustSweepResult =
    { Code: string
      RefusedBy: string
      Members: ProviderServingTrustSweepMember list
      ContinuedCount: int
      WithdrawnCount: int }

/// Applies one deterministic, bounded host-owned trust sweep. Invocation cadence remains external.
[<RequireQualifiedAccess>]
module ProviderServingTrustSweep =
    [<Literal>]
    let MaximumMembers = 64

    let runRecording
        (registry: DurableProviderPublisherTrustPolicyRegistry)
        (store: ContentAddressedProviderStore)
        (activations: ProviderServingActivation list)
        retirementReason
        (attributions: DurableProviderStopAttributionStore option)
        (recordedAt: DateTimeOffset)
        =
        task {
            if String.IsNullOrWhiteSpace retirementReason then
                invalidArg (nameof retirementReason) "retirement reason is required"

            let distinct =
                activations
                |> List.map _.OccurrenceId
                |> List.distinct
                |> List.length

            if List.isEmpty activations
               || List.length activations > MaximumMembers
               || activations |> List.exists (fun activation -> not activation.IsServing)
               || distinct <> List.length activations then
                return
                    { Code = "serving-trust-sweep-invalid"; RefusedBy = "preflight"
                      Members = []; ContinuedCount = 0; WithdrawnCount = 0 }
            else
                if isNull (box registry) then nullArg (nameof registry)
                if isNull (box store) then nullArg (nameof store)

                let ordered =
                    activations
                    |> List.sortWith (fun left right ->
                        String.CompareOrdinal(
                            OccurrenceId.value left.OccurrenceId,
                            OccurrenceId.value right.OccurrenceId))
                let members = ResizeArray<ProviderServingTrustSweepMember>()
                for activation in ordered do
                    let! result =
                        ProviderServingTrustRevalidation.revalidateForSweep registry store activation retirementReason
                    members.Add { Occurrence = activation.OccurrenceId; Result = result }
                    // After the effect, never before: the record states that this member was stopped.
                    match attributions, activation.DistributionChain.StagedIdentity with
                    | Some store, Some staged when not result.Continued ->
                        store.Record(activation.OccurrenceId, staged, recordedAt, PublisherTrustWithdrawal)
                        |> ignore
                    | _ -> ()
                let grouped =
                    ordered
                    |> List.choose (fun activation ->
                        activation.Chain.StagedIdentity
                        |> Option.map (fun staged -> staged, activation.OccurrenceId))
                    |> List.groupBy fst
                for stagedIdentity, groupedMembers in grouped do
                    let occurrences = groupedMembers |> List.map snd |> Set.ofList
                    let mustRetain =
                        members
                        |> Seq.exists (fun memberValue ->
                            Set.contains memberValue.Occurrence occurrences
                            && (memberValue.Result.Continued || not memberValue.Result.Revalidated))
                    if not mustRetain then
                        let removal = store.Remove stagedIdentity
                        if not removal.Removed && removal.Code <> "artifact-set-not-staged" then
                            for index in 0 .. members.Count - 1 do
                                let memberValue = members[index]
                                if Set.contains memberValue.Occurrence occurrences
                                   && not memberValue.Result.Continued then
                                    members[index] <-
                                        { memberValue with
                                            Result =
                                                { memberValue.Result with
                                                    RetirementCode =
                                                        $"{memberValue.Result.RetirementCode};{removal.Code}" } }
                let observations = List.ofSeq members
                let continued = observations |> List.sumBy (fun memberValue -> if memberValue.Result.Continued then 1 else 0)
                let code =
                    if observations |> List.exists (fun memberValue -> not memberValue.Result.Revalidated) then
                        "serving-trust-sweep-incomplete"
                    elif observations |> List.exists (fun memberValue ->
                        not memberValue.Result.Continued && memberValue.Result.RetirementCode <> "retired") then
                        "serving-trust-sweep-cleanup-incomplete"
                    elif continued = observations.Length then
                        "serving-trust-sweep-current"
                    else
                        "serving-trust-sweep-withdrawn"
                return
                    { Code = code; RefusedBy = "none"; Members = observations
                      ContinuedCount = continued; WithdrawnCount = observations.Length - continued }
        }

    let run registry store activations retirementReason =
        runRecording registry store activations retirementReason None DateTimeOffset.UnixEpoch
