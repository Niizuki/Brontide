namespace Brontide.Minimal.Host

open System

type ProviderRestartEnforcementResult =
    { Code: string
      RefusedBy: string
      Decision: ProviderRestartDecision
      Activation: ProviderServingActivation option
      ProviderStarted: bool
      LifecycleReconstructed: bool
      LogicalGenerationPreserved: bool }

/// Reconstructs a stopped provider connection from the activation's retained verified recipe.
[<RequireQualifiedAccess>]
module ProviderRestartEnforcement =
    let private result code refusedBy decision activation providerStarted lifecycleReconstructed logicalGenerationPreserved =
        { Code = code
          RefusedBy = refusedBy
          Decision = decision
          Activation = activation
          ProviderStarted = providerStarted
          LifecycleReconstructed = lifecycleReconstructed
          LogicalGenerationPreserved = logicalGenerationPreserved }

    let private runCore environment
        (policy: ProviderRestartPolicy)
        (registry: DurableProviderPublisherTrustPolicyRegistry)
        (store: ContentAddressedProviderStore)
        (activation: ProviderServingActivation)
        cause
        currentCyclePolicyIdentity
        now
        attemptCount
        lastAttempt = task {
        if isNull (box policy) then nullArg (nameof policy)
        if isNull (box registry) then nullArg (nameof registry)
        if isNull (box store) then nullArg (nameof store)
        if isNull (box activation) then nullArg (nameof activation)

        let decision =
            policy.Evaluate(
                registry, activation, cause, currentCyclePolicyIdentity,
                now, attemptCount, lastAttempt)

        if not decision.MayRestart then
            return result decision.Code decision.RefusedBy decision None false false false
        else
            let claim = activation.BeginRestart()
            if claim <> "restart-claimed" then
                return result claim "claim" decision None false false false
            else
                let mutable completed = false
                let mutable launchedProvider: StagedProviderProcess option = None
                try
                    match registry.Current, decision.PolicyIdentity with
                    | Some current, Some identity when current.Policy.Identity = identity ->
                        match activation.DistributionChain.Provider, activation.BindingLifecycle with
                        | None, _ | _, None ->
                            return result "provider-restart-activation-unavailable" "state" decision None false false false
                        | Some priorProvider, Some priorLifecycle ->
                            let staged = priorProvider.StagedArtifacts
                            match
                                if Map.isEmpty environment then store.Activate(staged, staged.Arguments)
                                else store.ActivateWithEnvironment(staged, staged.Arguments, environment)
                            with
                            | StagedProviderActivation.Refused failure ->
                                return result failure.Code "cbi31" decision None false false false
                            | StagedProviderActivation.Launched provider ->
                                launchedProvider <- Some provider
                                let chain =
                                    ProviderDistributionChain.restarted
                                        activation.DistributionChain provider identity
                                let! lifecycle =
                                    ComponentBindingLifecycle.restart
                                        priorLifecycle
                                        activation.RetainedResolution
                                        activation.RetainedSelection
                                        activation.RetainedRequest
                                        provider.Conversation
                                match lifecycle.Failure, lifecycle.Runtime, lifecycle.Member with
                                | Some failure, _, _ ->
                                    return result failure.Code "cbi2" decision None true false false
                                | None, Some runtime, Some memberValue
                                    when runtime.Kind = Brontide.Minimal.Experimental.ComponentManagement.ActivationRuntimeOutcomeKind.Active
                                         && memberValue.IsReleased ->
                                    let successor = activation.Restarted(chain, lifecycle)
                                    completed <- true
                                    return result "provider-restart-completed" "none" decision (Some successor) true true
                                        (lifecycle.Runtime = priorLifecycle.Runtime)
                                | _ ->
                                    return result "restart-lifecycle-incomplete" "cbi2" decision None true false false
                    | _ ->
                        return result "provider-restart-current-proof-required" "current-cycle" decision None false false false
                finally
                    if not completed then
                        launchedProvider |> Option.iter (fun provider -> provider.Dispose())
                    activation.FinishRestart completed
    }

    let run policy registry store activation cause currentCyclePolicyIdentity now attemptCount lastAttempt =
        runCore Map.empty policy registry store activation cause currentCyclePolicyIdentity now attemptCount lastAttempt

    let internal runWithEffectEnvironment environment policy registry store activation cause currentCyclePolicyIdentity now attemptCount lastAttempt =
        runCore environment policy registry store activation cause currentCyclePolicyIdentity now attemptCount lastAttempt
