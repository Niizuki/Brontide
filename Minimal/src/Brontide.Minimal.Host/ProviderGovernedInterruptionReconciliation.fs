namespace Brontide.Minimal.Host

open System
open System.Threading
open System.Threading.Tasks

/// What the host observed about the serving set of an interrupted governed cycle. This is the only
/// verdict governed evidence carries: the rotation and the policy update record themselves durably,
/// and the sweep is the one thing CBI48 says no local journal can commit atomically with its cursor.
[<RequireQualifiedAccess>]
type ProviderGovernedServingObservation =
    | NoEffectsConfirmed
    | EffectsAccountedFor
    | Unknown

type ProviderGovernedReconciliationEvidence =
    { RunIdentity: ProviderTrustCadenceRunId
      AttemptIndex: int
      AttemptInstant: DateTimeOffset
      Serving: ProviderGovernedServingObservation }

/// What the durable record says the interrupted attempt did, derived rather than asserted. Both
/// observations are reported next to the decision; neither is ever read from the evidence.
type ProviderGovernedDerivedEffects =
    { RotationApplied: bool
      RecordedGeneration: int64
      ObservedGeneration: int64
      PolicyApplied: bool
      RecordedSequence: int64
      ObservedSequence: int64 }

type ProviderGovernedReconciliationResult =
    { Code: string
      Snapshot: ProviderTrustCadenceJournalSnapshot
      Derived: ProviderGovernedDerivedEffects option }

[<RequireQualifiedAccess>]
module ProviderGovernedTrustCadenceRecovery =
    let cursor (registry: DurableProviderPublisherTrustPolicyRegistry) : ProviderTrustCadenceJournalCursor =
        if isNull (box registry) then nullArg (nameof registry)
        let current = registry.Current
        { AuthorityGeneration = registry.AuthorityGeneration
          ActiveAuthority = ProviderPublisherTrustPolicyAuthorityId.value registry.ActiveAuthorityIdentity
          PolicySequence = current |> Option.map _.Sequence |> Option.defaultValue 0L
          PolicyIdentity =
            current |> Option.map (fun snapshot -> ProviderPublisherTrustPolicyId.value snapshot.Policy.Identity) }

    /// CBI48's recovery advance for a governed run. The only difference is that it hands the durable
    /// cursor to `BeginCycle`, so the write that already marks the attempt in-flight also records the
    /// state the attempt is about to act on.
    let advance
        (journal: DurableProviderTrustCadenceJournal)
        (registry: DurableProviderPublisherTrustPolicyRegistry)
        (cycle: ProviderServingTrustCycle)
        (delay: ProviderServingTrustCadenceDelay)
        (cancellationToken: CancellationToken)
        = task {
            if isNull (box journal) then nullArg (nameof journal)
            if isNull (box registry) then nullArg (nameof registry)
            if isNull (box cycle) then nullArg (nameof cycle)
            if isNull (box delay) then nullArg (nameof delay)

            let snapshot = journal.Snapshot
            if snapshot.Phase = "terminal" then
                return { Code = snapshot.Code; Snapshot = snapshot }
            elif snapshot.Phase = "in-flight" then
                return { Code = "durable-cadence-indeterminate"; Snapshot = snapshot }
            elif cancellationToken.IsCancellationRequested then
                return { Code = "durable-cadence-wait-canceled"; Snapshot = snapshot }
            else
                let! gapped = task {
                    if snapshot.Phase <> "waiting" then return Ok()
                    else
                        try
                            let! next = delay snapshot.PreparedInstant snapshot.Interval cancellationToken
                            if cancellationToken.IsCancellationRequested then
                                return Error { Code = "durable-cadence-wait-canceled"; Snapshot = journal.Snapshot }
                            else
                                let gap = journal.CompleteGap next
                                if gap.Code <> "durable-cadence-gap-completed" then return Error gap
                                else return Ok()
                        with :? OperationCanceledException ->
                            return Error { Code = "durable-cadence-wait-canceled"; Snapshot = journal.Snapshot }
                }
                match gapped with
                | Error result -> return result
                | Ok() ->
                    let started = journal.BeginCycle(Some(cursor registry))
                    if started.Code <> "durable-cadence-cycle-started" then return started
                    else
                        let! result = cycle started.Snapshot.PreparedInstant cancellationToken
                        return journal.CommitCycle result.Code
        }

/// Reconciles an interrupted governed cycle. It derives what the local durable record already states
/// and requires the host to assert only what nothing can check.
[<RequireQualifiedAccess>]
module ProviderGovernedInterruptionReconciliation =
    let private map acceptedCode derived (result: ProviderTrustCadenceJournalTransitionResult) =
        { Code =
            if result.Code = "durable-cadence-retry-ready" || result.Code = "durable-cadence-abandoned" then
                acceptedCode
            else result.Code
          Snapshot = result.Snapshot
          Derived = Some derived }

    let apply
        (journal: DurableProviderTrustCadenceJournal)
        (evidence: ProviderGovernedReconciliationEvidence)
        (registry: DurableProviderPublisherTrustPolicyRegistry) =
        if isNull (box journal) then nullArg (nameof journal)
        if isNull (box evidence) then nullArg (nameof evidence)
        if isNull (box registry) then nullArg (nameof registry)

        let snapshot = journal.Snapshot
        if snapshot.Phase <> "in-flight" then
            { Code = "governed-reconciliation-not-required"; Snapshot = snapshot; Derived = None }
        elif snapshot.RunIdentity <> evidence.RunIdentity
             || snapshot.NextCycleIndex <> evidence.AttemptIndex
             || snapshot.PreparedInstant <> evidence.AttemptInstant then
            { Code = "governed-reconciliation-mismatch"; Snapshot = snapshot; Derived = None }
        else
            // A journal written before this slice has no baseline, and inventing one would be the
            // guess the whole design exists to avoid. Such a run is CBI49's, which is correct for it.
            match snapshot.Cursor with
            | None ->
                { Code = "governed-reconciliation-cursor-absent"; Snapshot = snapshot; Derived = None }
            | Some cursor ->
                let observedGeneration = registry.AuthorityGeneration
                let observedSequence = registry.Current |> Option.map _.Sequence |> Option.defaultValue 0L
                // Below the recorded cursor is a rollback the floors exist to prevent, so it is
                // refused here rather than reported as an absence of effect.
                if observedGeneration < cursor.AuthorityGeneration
                   || observedSequence < cursor.PolicySequence then
                    { Code = "governed-reconciliation-cursor-regressed"; Snapshot = snapshot; Derived = None }
                else
                    let derived =
                        { RotationApplied = observedGeneration > cursor.AuthorityGeneration
                          RecordedGeneration = cursor.AuthorityGeneration
                          ObservedGeneration = observedGeneration
                          PolicyApplied = observedSequence > cursor.PolicySequence
                          RecordedSequence = cursor.PolicySequence
                          ObservedSequence = observedSequence }
                    // A derived effect is reported rather than used as a veto: CBI62 established
                    // that a retried governed cycle cannot double-apply either half, so what decides
                    // is the one observation nothing here can make for itself.
                    match evidence.Serving with
                    | ProviderGovernedServingObservation.Unknown ->
                        { Code = "governed-reconciliation-deferred"
                          Snapshot = journal.Snapshot
                          Derived = Some derived }
                    | ProviderGovernedServingObservation.NoEffectsConfirmed ->
                        journal.ResolveInterrupted ProviderTrustCadenceRecoveryDecision.Retry
                        |> map "governed-reconciliation-retry-ready" derived
                    | ProviderGovernedServingObservation.EffectsAccountedFor ->
                        journal.ResolveInterrupted ProviderTrustCadenceRecoveryDecision.Abandon
                        |> map "governed-reconciliation-abandoned" derived
