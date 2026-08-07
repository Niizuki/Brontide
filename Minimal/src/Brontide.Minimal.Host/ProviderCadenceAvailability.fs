namespace Brontide.Minimal.Host

open System
open System.Threading
open System.Threading.Tasks

/// Decides what an unavailable policy endpoint means for the providers a cadence is watching, and
/// performs whatever that decision requires. The seam returns the cycle's projection of CBI50 rather
/// than CBI50's result, so a cadence never depends on the enforcement component's types.
type ProviderOfflineEnforcementCycle =
    DateTimeOffset
        -> DateTimeOffset option
        -> string
        -> string option
        -> CancellationToken
        -> Task<ProviderTrustCycleAvailability>

[<RequireQualifiedAccess>]
module ProviderAvailabilityTrustCycle =
    /// Binds one CBI49 policy and CBI50 enforcement to the serving set owned by its host.
    let enforcement
        (policy: ProviderTrustOfflinePolicy)
        (servingSet: CancellationToken -> Task<ProviderServingActivation list>)
        retirementReason
        : ProviderOfflineEnforcementCycle =
        fun now lastCurrent pollCode lastAttemptCode cancellationToken -> task {
            let! activations = servingSet cancellationToken
            let! result =
                ProviderOfflineServiceEnforcement.run
                    policy now lastCurrent pollCode lastAttemptCode activations retirementReason
            return
                { EnforcementCode = result.Code
                  DecisionCode = result.Decision |> Option.map _.Code
                  Deadline = result.Decision |> Option.bind _.Deadline
                  RetryAt = result.Decision |> Option.bind _.RetryAt
                  AdmittedCount = result.AdmittedCount
                  StoppedCount = result.StoppedCount }
        }

    /// Applies CBI49's availability policy to a cadence that cannot establish current policy, and
    /// CBI50's enforcement to whatever that decides. It wraps the whole cycle rather than sitting
    /// inside it, because the cycle code it must leave alone is the one CBI61's governed wrapper
    /// computes.
    ///
    /// The baseline it closes over is the instant of the most recent cycle whose poll established
    /// current policy. An outage never refreshes it, which is what makes the deadline arrive — a
    /// cadence that took each cycle's own instant would report existing service forever, and CBI49's
    /// own vectors cannot see the difference because they evaluate once. A resumed cadence is given
    /// the baseline CBI65 derives from what CBI48 committed, so a crash inside an outage does not
    /// restart grace.
    let resume
        (baseline: DateTimeOffset option)
        (inner: ProviderServingTrustCycle)
        (enforce: ProviderOfflineEnforcementCycle)
        : ProviderServingTrustCycle =
        let lastCurrent = ref baseline
        fun now cancellationToken -> task {
            let! result = inner now cancellationToken
            match result.Poll with
            | Some poll when poll.IsCurrent ->
                lastCurrent.Value <- Some now
                return result
            | Some poll when not result.IsCanceled ->
                let! availability =
                    enforce now lastCurrent.Value poll.Code poll.LastAttemptCode cancellationToken
                let code =
                    if availability.PermitsContinuation then ProviderServingTrustCycleCodes.Offline
                    else result.Code
                return { result with Code = code; Availability = Some availability }
            // Cancellation is the host stopping its own loop rather than the endpoint failing, and a
            // cycle its rotation stopped never asked the endpoint anything. Neither is an availability
            // observation, and CBI49 has no code that means "no poll was made".
            | _ -> return result
        }

    /// A cadence that has no durable record to resume from starts with no baseline, which is CBI49's
    /// own answer to a missing one.
    let create (inner: ProviderServingTrustCycle) (enforce: ProviderOfflineEnforcementCycle) =
        resume None inner enforce

type ProviderTrustCadenceAvailabilityBaseline =
    { Code: string
      Instant: DateTimeOffset option }

/// Recovers CBI64's availability baseline from what CBI48 already committed. The journal has recorded
/// each cycle's instant and code since CBI48, so nothing new is written and nothing is written here: a
/// record written about a derivation would be a less trustworthy copy of the record it read, which is
/// the reasoning CBI62 established and CBI63 applied.
[<RequireQualifiedAccess>]
module ProviderTrustCadenceAvailabilityRecovery =
    let derive (snapshot: ProviderTrustCadenceJournalSnapshot) =
        if isNull (box snapshot) then nullArg (nameof snapshot)
        // The vocabulary answers this rather than a list here, so a later cycle code cannot be added
        // without deciding what it means for a baseline. A code it does not classify is refused:
        // `provider-trust-cycle-stopped` covers both a poll that was not current and a current poll
        // whose sweep failed, and nothing in the record says which.
        let rec walk baseline remaining =
            match remaining with
            | [] ->
                match baseline with
                | None -> { Code = "cadence-baseline-absent"; Instant = None }
                | instant -> { Code = "cadence-baseline-derived"; Instant = instant }
            | (cycle: ProviderTrustCadenceJournalCycle) :: rest ->
                match ProviderServingTrustCycleCodes.establishes cycle.Code with
                | None -> { Code = "cadence-baseline-observation-invalid"; Instant = None }
                | Some true -> walk (Some cycle.Instant) rest
                | Some false -> walk baseline rest
        walk None snapshot.Cycles
