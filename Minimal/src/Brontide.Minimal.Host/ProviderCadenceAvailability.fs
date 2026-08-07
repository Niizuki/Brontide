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
    /// One cycle belongs to one cadence run: the baseline it closes over is the instant of the most
    /// recent cycle of that run whose poll established current policy. An outage never refreshes it,
    /// which is what makes the deadline arrive — a cadence that took each cycle's own instant would
    /// report existing service forever, and CBI49's own vectors cannot see the difference because they
    /// evaluate once.
    let create
        (inner: ProviderServingTrustCycle)
        (enforce: ProviderOfflineEnforcementCycle)
        : ProviderServingTrustCycle =
        let lastCurrent = ref None
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
