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

    let run (policy: ProviderTrustOfflinePolicy) now lastCurrent pollCode lastAttemptCode
        (activations: ProviderServingActivation list) retirementReason = task {
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
