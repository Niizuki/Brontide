namespace Brontide.Minimal.Host

open System

type ProviderTrustOfflineDecision =
    { Code: string
      MayContinueExistingService: bool
      MayStartProvider: bool
      Deadline: DateTimeOffset option
      RetryAt: DateTimeOffset option }

/// One host-owned availability decision. Grace preserves existing service only; it is not trust
/// evidence and never authorizes acquisition, launch, admission, or restart.
type ProviderTrustOfflinePolicy internal (grace: TimeSpan, retry: TimeSpan) =
    member _.Grace = grace
    member _.Retry = retry

    member _.Evaluate(
        now: DateTimeOffset,
        lastCurrent: DateTimeOffset option,
        pollCode: string,
        lastAttemptCode: string option,
        servingCount: int) =
        if String.IsNullOrWhiteSpace pollCode then
            invalidArg (nameof pollCode) "A poll outcome code is required."
        elif servingCount < 0 || servingCount > 64
             || (lastCurrent |> Option.exists (fun value ->
                    value > now || value.Ticks > DateTimeOffset.MaxValue.Ticks - grace.Ticks)) then
            { Code = "offline-observation-invalid"; MayContinueExistingService = false
              MayStartProvider = false; Deadline = None; RetryAt = None }
        else
            match lastCurrent with
            | None ->
                { Code = "offline-service-stop-required"; MayContinueExistingService = false
                  MayStartProvider = false; Deadline = None; RetryAt = None }
            | Some observed ->
                let deadline = observed.Add grace
                let eligible =
                    pollCode = "policy-poll-exhausted"
                    && (lastAttemptCode = Some "policy-distribution-transport-failed"
                        || lastAttemptCode = Some "policy-distribution-timeout")
                if not eligible then
                    { Code = "offline-service-stop-required"; MayContinueExistingService = false
                      MayStartProvider = false; Deadline = Some deadline; RetryAt = None }
                elif now >= deadline then
                    { Code = "offline-grace-expired"; MayContinueExistingService = false
                      MayStartProvider = false; Deadline = Some deadline; RetryAt = None }
                else
                    let remaining = deadline - now
                    let retryAt = if retry >= remaining then deadline else now.Add retry
                    { Code = if servingCount = 0 then "offline-idle" else "offline-existing-service"
                      MayContinueExistingService = servingCount > 0
                      MayStartProvider = false
                      Deadline = Some deadline
                      RetryAt = Some retryAt }

[<RequireQualifiedAccess>]
module ProviderTrustOfflinePolicy =
    let create (grace: TimeSpan) (retry: TimeSpan) =
        if grace <= TimeSpan.Zero || grace > TimeSpan.FromHours 24.0 then
            invalidArg (nameof grace) "Offline grace must be positive and no greater than twenty-four hours."
        if retry <= TimeSpan.Zero || retry > grace then
            invalidArg (nameof retry) "Retry must be positive and no greater than offline grace."
        ProviderTrustOfflinePolicy(grace, retry)

type ProviderTrustCadenceReconciliationVerdict =
    | NoEffectsConfirmed
    | EffectsAccountedFor
    | Unknown

type ProviderTrustCadenceReconciliationEvidence =
    { RunIdentity: ProviderTrustCadenceRunId
      AttemptIndex: int
      AttemptInstant: DateTimeOffset
      Verdict: ProviderTrustCadenceReconciliationVerdict }

/// Checks that host evidence names the exact interrupted attempt before selecting CBI48's durable
/// transition. The host owns the evidence; this module does not infer external effects.
[<RequireQualifiedAccess>]
module ProviderTrustCadenceReconciliation =
    let private current code snapshot: ProviderTrustCadenceJournalTransitionResult =
        { Code = code; Snapshot = snapshot }

    let private map acceptedCode (result: ProviderTrustCadenceJournalTransitionResult) =
        if result.Code = "durable-cadence-retry-ready"
           || result.Code = "durable-cadence-abandoned" then
            { result with Code = acceptedCode }
        else result

    let apply
        (journal: DurableProviderTrustCadenceJournal)
        (evidence: ProviderTrustCadenceReconciliationEvidence) =
        if isNull (box journal) then nullArg (nameof journal)
        if isNull (box evidence) then nullArg (nameof evidence)
        let snapshot = journal.Snapshot
        if snapshot.Phase <> "in-flight" then
            current "cadence-reconciliation-not-required" snapshot
        elif snapshot.RunIdentity <> evidence.RunIdentity
             || snapshot.NextCycleIndex <> evidence.AttemptIndex
             || snapshot.PreparedInstant <> evidence.AttemptInstant then
            current "cadence-reconciliation-mismatch" snapshot
        else
            match evidence.Verdict with
            | ProviderTrustCadenceReconciliationVerdict.Unknown ->
                current "cadence-reconciliation-deferred" snapshot
            | ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed ->
                journal.ResolveInterrupted ProviderTrustCadenceRecoveryDecision.Retry
                |> map "cadence-reconciliation-retry-ready"
            | ProviderTrustCadenceReconciliationVerdict.EffectsAccountedFor ->
                journal.ResolveInterrupted ProviderTrustCadenceRecoveryDecision.Abandon
                |> map "cadence-reconciliation-abandoned"
