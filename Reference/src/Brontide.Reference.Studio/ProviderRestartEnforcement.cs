namespace Brontide.Reference.Studio;

public sealed record ProviderRestartEnforcementResult(
    string Code,
    string RefusedBy,
    ProviderRestartDecision Decision,
    ProviderServingActivation? Activation,
    bool ProviderStarted,
    bool LifecycleReconstructed,
    bool LogicalGenerationPreserved);

/// <summary>
/// Reconstructs a stopped provider connection from the activation's retained verified recipe.
/// </summary>
public static class ProviderRestartEnforcement
{
    public static async ValueTask<ProviderRestartEnforcementResult> RunAsync(
        ProviderRestartPolicy policy,
        DurableProviderPublisherTrustPolicyRegistry registry,
        ContentAddressedProviderStore store,
        ProviderServingActivation activation,
        ProviderRestartCause cause,
        ProviderPublisherTrustPolicyId currentCyclePolicyIdentity,
        DateTimeOffset now,
        int attemptCount,
        DateTimeOffset? lastAttempt)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(activation);

        var decision = policy.Evaluate(
            registry, activation, cause, currentCyclePolicyIdentity, now, attemptCount, lastAttempt);
        if (!decision.MayRestart)
            return Result(decision.Code, decision.RefusedBy, decision);

        var claim = activation.BeginRestart();
        if (claim != "restart-claimed")
            return Result(claim, "claim", decision);

        var completed = false;
        StagedProviderProcess? launchedProvider = null;
        try
        {
            var current = registry.Current;
            if (current is null || decision.PolicyIdentity is not { } policyIdentity
                || current.Policy.Identity != policyIdentity)
                return Result("provider-restart-current-proof-required", "current-cycle", decision);

            var priorChain = activation.Chain;
            if (priorChain.Provider is not { } priorProvider || activation.Lifecycle is not { } priorLifecycle)
                return Result("provider-restart-activation-unavailable", "state", decision);

            var staged = priorProvider.StagedArtifacts;
            var launched = store.Activate(staged, staged.Arguments);
            if (launched.Owner is not { } provider)
                return Result(launched.Failure!.Code, "cbi31", decision);
            launchedProvider = provider;

            var chain = ProviderDistributionChainResult.Restarted(priorChain, provider, policyIdentity);
            var lifecycle = await ComponentBindingLifecycle.RestartAsync(
                priorLifecycle,
                activation.Resolution,
                activation.Selection,
                activation.Request,
                provider.Conversation).ConfigureAwait(false);
            if (!lifecycle.IsActive)
            {
                if (lifecycle.Member is { } member)
                    await member.DisposeAsync().ConfigureAwait(false);
                return Result(
                    lifecycle.Failure?.Code ?? "restart-lifecycle-incomplete",
                    "cbi2", decision, providerStarted: true);
            }

            var successor = new ProviderServingActivation(
                chain, lifecycle, activation.Occurrence,
                activation.Resolution, activation.Selection, activation.Request);
            completed = true;
            return Result(
                "provider-restart-completed", "none", decision,
                successor, providerStarted: true, lifecycleReconstructed: true,
                logicalGenerationPreserved: lifecycle.Runtime == priorLifecycle.Runtime);
        }
        finally
        {
            if (!completed && launchedProvider is not null)
                await launchedProvider.DisposeAsync().ConfigureAwait(false);
            activation.FinishRestart(completed);
        }
    }

    private static ProviderRestartEnforcementResult Result(
        string code,
        string refusedBy,
        ProviderRestartDecision decision,
        ProviderServingActivation? activation = null,
        bool providerStarted = false,
        bool lifecycleReconstructed = false,
        bool logicalGenerationPreserved = false) =>
        new(code, refusedBy, decision, activation, providerStarted,
            lifecycleReconstructed, logicalGenerationPreserved);
}
