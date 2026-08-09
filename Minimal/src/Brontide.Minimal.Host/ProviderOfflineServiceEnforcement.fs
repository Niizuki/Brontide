namespace Brontide.Minimal.Host

open System
open Brontide.Minimal.Experimental.ComponentManagement

type ProviderOfflineServiceEnforcementMember =
    { Occurrence: OccurrenceId
      RetirementCode: string
      ProviderStopped: bool }

type ProviderOfflineServiceEnforcementResult =
    { Code: string
      RefusedBy: string
      Decision: ProviderTrustOfflineDecision option
      Members: ProviderOfflineServiceEnforcementMember list
      AdmittedCount: int
      StoppedCount: int }

/// Applies one offline-policy decision to the exact supplied serving set. Availability withdrawal
/// stops service but deliberately retains staged artifacts for the separate restart policy.
[<RequireQualifiedAccess>]
module ProviderOfflineServiceEnforcement =
    [<Literal>]
    let MaximumMembers = 64

    let runRecording (policy: ProviderTrustOfflinePolicy) now lastCurrent pollCode lastAttemptCode
        (activations: ProviderServingActivation list) retirementReason
        (attributions: DurableProviderStopAttributionStore option) = task {
        if isNull (box policy) then nullArg (nameof policy)
        if isNull (box activations) then nullArg (nameof activations)
        if String.IsNullOrWhiteSpace retirementReason then
            invalidArg (nameof retirementReason) "retirement reason is required"
        if activations.Length > MaximumMembers
           || (activations |> List.exists (fun activation -> isNull (box activation) || not activation.IsServing))
           || (activations |> List.map _.OccurrenceId |> List.distinct |> List.length) <> activations.Length then
            return { Code = "offline-enforcement-invalid"; RefusedBy = "preflight"; Decision = None
                     Members = []; AdmittedCount = 0; StoppedCount = 0 }
        else
            let decision = policy.Evaluate(now, lastCurrent, pollCode, lastAttemptCode, activations.Length)
            if decision.Code = "offline-existing-service" then
                return { Code = "offline-enforcement-continuing"; RefusedBy = "none"; Decision = Some decision
                         Members = []; AdmittedCount = activations.Length; StoppedCount = 0 }
            elif decision.Code = "offline-idle" then
                return { Code = "offline-enforcement-idle"; RefusedBy = "none"; Decision = Some decision
                         Members = []; AdmittedCount = 0; StoppedCount = 0 }
            else
                let members = ResizeArray<ProviderOfflineServiceEnforcementMember>()
                for activation in activations |> List.sortBy (fun value -> OccurrenceId.value value.OccurrenceId) do
                    let! retirementCode = task {
                        match activation.BindingLifecycle |> Option.bind _.Member with
                        | Some memberValue when memberValue.IsReleased ->
                            let! retired = memberValue.Retire retirementReason
                            return match retired with Ok _ -> "retired" | Error _ -> "retirement-failed"
                        | _ -> return "retirement-not-attempted" }
                    let providerStopped =
                        match activation.DistributionChain.Provider with
                        | Some provider when not provider.HasExited ->
                            try provider.Dispose() with _ -> ()
                            provider.HasExited
                        | _ -> true
                    members.Add { Occurrence = activation.OccurrenceId; RetirementCode = retirementCode
                                  ProviderStopped = providerStopped }
                    // After the effect, never before. An interruption here leaves a stop with no
                    // record, which reads as an unexpected exit — restartable, which is what an
                    // availability stop wanted anyway; the opposite order would claim a stop that had
                    // not happened.
                    match attributions, activation.DistributionChain.StagedIdentity with
                    | Some store, Some staged ->
                        store.Record(activation.OccurrenceId, staged, now, OfflineAvailability) |> ignore
                    | _ -> ()
                let observations = List.ofSeq members
                let stopped = observations |> List.filter _.ProviderStopped |> List.length
                let code =
                    if stopped <> observations.Length then "offline-enforcement-incomplete"
                    elif observations |> List.exists (fun value -> value.RetirementCode <> "retired") then
                        "offline-enforcement-cleanup-incomplete"
                    else "offline-enforcement-stopped"
                return { Code = code; RefusedBy = "none"; Decision = Some decision
                         Members = observations; AdmittedCount = activations.Length; StoppedCount = stopped }
    }

    let run policy now lastCurrent pollCode lastAttemptCode activations retirementReason =
        runRecording policy now lastCurrent pollCode lastAttemptCode activations retirementReason None

/// Records one stop for an activation, which is what every writer in the host holds.
[<RequireQualifiedAccess>]
module ProviderStopAttributions =
    let record
        (attributions: DurableProviderStopAttributionStore)
        (activation: ProviderServingActivation)
        (instant: DateTimeOffset)
        cause
        =
        match activation.DistributionChain.StagedIdentity with
        | Some staged -> attributions.Record(activation.OccurrenceId, staged, instant, cause)
        | None -> "provider-stop-attribution-activation-unavailable"

    let attribute
        (attributions: DurableProviderStopAttributionStore)
        (activation: ProviderServingActivation)
        =
        match activation.DistributionChain.StagedIdentity with
        | Some staged -> attributions.Attribute(activation.OccurrenceId, staged)
        | None -> { Code = "provider-stop-attribution-activation-unavailable"; Attribution = None }

/// The one path by which an operator retirement becomes attributable. A retirement issued outside the
/// host leaves no record and an exited process, which is indistinguishable from an unexpected exit —
/// that is the bound on what this slice can attribute, and it is stated rather than implied away.
[<RequireQualifiedAccess>]
module ProviderOperatorRetirement =
    let retire
        (attributions: DurableProviderStopAttributionStore)
        (activation: ProviderServingActivation)
        (reason: string)
        (now: DateTimeOffset)
        =
        task {
            if isNull (box attributions) then nullArg (nameof attributions)
            if isNull (box activation) then nullArg (nameof activation)
            if String.IsNullOrWhiteSpace reason then
                invalidArg (nameof reason) "A retirement reason is required."
            match activation.DistributionChain.StagedIdentity with
            | None -> return "provider-stop-attribution-activation-unavailable"
            | Some stagedIdentity ->
                if activation.IsServing then
                    let! _ = activation.Retire reason
                    ()
                match activation.DistributionChain.Provider with
                | Some provider when not provider.HasExited -> provider.Dispose()
                | _ -> ()
                return attributions.Record(
                    activation.OccurrenceId, stagedIdentity, now, OperatorRetirement)
        }
