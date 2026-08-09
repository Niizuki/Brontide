namespace Brontide.Minimal.Host

open System

type ProviderRestartDecision =
    { Code: string
      RefusedBy: string
      MayRestart: bool
      RetryAt: DateTimeOffset option
      PolicyIdentity: ProviderPublisherTrustPolicyId option
      Authorization: TrustedProviderPublisherAuthorization option }

/// Decides whether one stopped activation is eligible for a later restart effect. A current-cycle
/// policy identity is required because a retained registry snapshot alone proves no availability.
type ProviderRestartPolicy internal (maximumAttempts: int, delay: TimeSpan) =
    member _.MaximumAttempts = maximumAttempts
    member _.Delay = delay

    member _.Evaluate(
        registry: DurableProviderPublisherTrustPolicyRegistry,
        activation: ProviderServingActivation,
        attribution: ProviderStopAttribution,
        currentCyclePolicyIdentity: ProviderPublisherTrustPolicyId,
        now: DateTimeOffset,
        attemptCount: int,
        lastAttempt: DateTimeOffset option) =
        if isNull (box registry) then nullArg (nameof registry)
        if isNull (box activation) then nullArg (nameof activation)
        if isNull (box attribution) then nullArg (nameof attribution)
        let denied code refusedBy policyIdentity =
            { Code = code; RefusedBy = refusedBy; MayRestart = false; RetryAt = None
              PolicyIdentity = policyIdentity; Authorization = None }
        if attemptCount < 0 || attemptCount > maximumAttempts
           || (attemptCount = 0 && lastAttempt.IsSome)
           || (attemptCount > 0 && lastAttempt.IsNone)
           || (lastAttempt |> Option.exists (fun value ->
                value > now || value.Ticks > DateTimeOffset.MaxValue.Ticks - delay.Ticks)) then
            denied "provider-restart-observation-invalid" "preflight" None
        elif activation.IsServing then
            denied "provider-restart-not-required" "state" None
        // The attribution is issued about one activation, so one about a different occurrence is a
        // caller mistake rather than a cause. The refusals below are unchanged; what changed is that
        // the caller no longer chooses which of them applies.
        elif attribution.Occurrence <> activation.OccurrenceId then
            denied "provider-restart-attribution-mismatch" "attribution" None
        elif attribution.Cause = PublisherTrustWithdrawal || attribution.Cause = OperatorRetirement then
            denied "provider-restart-cause-refused" "cause" None
        else
            let chain = activation.DistributionChain
            match registry.Current with
            | None -> denied "provider-restart-current-proof-required" "current-cycle" None
            | Some current when current.Policy.Identity <> currentCyclePolicyIdentity
                                || chain.PolicyAuthorityIdentity <> Some registry.AuthorityIdentity ->
                denied "provider-restart-current-proof-required" "current-cycle" None
            | Some current ->
                match chain.Provider, chain.VerifiedEvidence, chain.StagedIdentity with
                | Some provider, Some evidence, Some stagedIdentity when provider.HasExited ->
                    let trust = ProviderPublisherTrustEvaluator.evaluate current.Policy (Some evidence)
                    match trust.Authorization with
                    | Some authorization when authorization.ContentIdentity = stagedIdentity ->
                        if attemptCount = maximumAttempts then
                            denied "provider-restart-exhausted" "budget" (Some current.Policy.Identity)
                        else
                            match lastAttempt with
                            | Some attempted when now < attempted.Add delay ->
                                { Code = "provider-restart-waiting"; RefusedBy = "none"; MayRestart = false
                                  RetryAt = Some(attempted.Add delay); PolicyIdentity = Some current.Policy.Identity
                                  Authorization = Some authorization }
                            | _ ->
                                { Code = "provider-restart-ready"; RefusedBy = "none"; MayRestart = true
                                  RetryAt = None; PolicyIdentity = Some current.Policy.Identity
                                  Authorization = Some authorization }
                    | _ -> denied trust.Code "cbi35" (Some current.Policy.Identity)
                | _ -> denied "provider-restart-activation-unavailable" "state" (Some current.Policy.Identity)

[<RequireQualifiedAccess>]
module ProviderRestartPolicy =
    let create maximumAttempts (delay: TimeSpan) =
        if maximumAttempts < 1 || maximumAttempts > 8 then
            invalidArg (nameof maximumAttempts) "Maximum restart attempts must be between one and eight."
        if delay <= TimeSpan.Zero || delay > TimeSpan.FromHours 1.0 then
            invalidArg (nameof delay) "Restart delay must be positive and no greater than one hour."
        ProviderRestartPolicy(maximumAttempts, delay)
