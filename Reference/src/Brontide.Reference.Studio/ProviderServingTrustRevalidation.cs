namespace Brontide.Reference.Studio;

public sealed record ProviderServingTrustRevalidationResult(
    string Code,
    string RefusedBy,
    bool Revalidated,
    bool Continued,
    ProviderPublisherTrustPolicyId? LaunchPolicyIdentity,
    ProviderPublisherTrustPolicyId? ServingPolicyIdentity,
    TrustedProviderPublisherAuthorization? Authorization,
    string RetirementCode);

public sealed class ProviderServingActivation : IAsyncDisposable
{
    internal ProviderServingActivation(
        ProviderDistributionChainResult chain,
        ComponentBindingLifecycleResult? lifecycle)
    {
        Chain = chain;
        Lifecycle = lifecycle;
    }

    internal ProviderDistributionChainResult Chain { get; }
    internal ComponentBindingLifecycleResult? Lifecycle { get; }

    public bool IsServing =>
        Chain.Provider is { HasExited: false } && Lifecycle?.IsActive == true;

    public bool MemberReleased => Lifecycle?.Member?.IsReleased == true;

    public async ValueTask RetireAsync(string reason, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Lifecycle?.Member is { IsReleased: true } member)
        {
            await member.RetireAsync(reason, cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Lifecycle?.Member is { } member)
        {
            await member.DisposeAsync().ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Takes one current publisher-trust decision for an already released portable member. A lapsed
/// publisher is retired and its concrete provider is terminated; cadence remains a host concern.
/// </summary>
public static class ProviderServingTrustRevalidation
{
    public static async ValueTask<ProviderServingActivation> ActivateAsync(
        ProviderDistributionChainResult chain,
        Brontide.Reference.Experimental.ComponentManagement.ResolutionOutcome resolution,
        ComponentBindingSelection selection,
        Brontide.Reference.Experimental.ComponentManagement.ActivationRuntimeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chain);
        if (chain.Provider is not { } provider || !chain.Revalidated)
        {
            return new(chain, null);
        }

        var lifecycle = await ComponentBindingLifecycle.ActivateAsync(
            resolution, selection, request, provider.Conversation, cancellationToken).ConfigureAwait(false);
        return new(chain, lifecycle);
    }

    public static async ValueTask<ProviderServingTrustRevalidationResult> RevalidateAsync(
        DurableProviderPublisherTrustPolicyRegistry registry,
        ContentAddressedProviderStore store,
        ProviderServingActivation activation,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(activation);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        var chain = activation.Chain;
        var lifecycle = activation.Lifecycle;

        if (!chain.IsLaunched || !chain.Revalidated || chain.Provider is not { } provider
            || chain.VerifiedEvidence is not { } evidence || chain.StagedIdentity is null
            || chain.PolicyAuthorityIdentity != registry.AuthorityIdentity || provider.HasExited
            || lifecycle?.IsActive != true || lifecycle.Member is not { IsReleased: true } member)
        {
            return new(
                "serving-activation-unavailable", "none", false, false,
                chain.LaunchPolicyIdentity, null, null, "retirement-not-attempted");
        }

        var current = registry.Current;
        if (current is null)
        {
            // A matching registry that launched this provider cannot clear its current snapshot.
            // Keep the boundary fail closed if an implementation later gains such a transition.
            return new(
                "publisher-trust-policy-unavailable", "cbi37", false, false,
                chain.LaunchPolicyIdentity, null, null, "retirement-not-attempted");
        }

        var trust = ProviderPublisherTrustEvaluator.Evaluate(current.Policy, evidence);
        if (trust.Authorization is not null)
        {
            return new(
                "publisher-trust-current", "none", true, true,
                chain.LaunchPolicyIdentity, current.Policy.Identity, trust.Authorization,
                "retirement-not-attempted");
        }

        var retirementCode = "retired";
        try
        {
            await member.RetireAsync(retirementReason, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Trust withdrawal still terminates the concrete provider. The separate cleanup code
            // preserves the failed graceful-retirement observation without reopening service.
            retirementCode = "retirement-failed";
        }

        await provider.DisposeAsync().ConfigureAwait(false);
        var removal = store.Remove(chain.StagedIdentity.Value);
        if (!removal.Removed && removal.Code != "artifact-set-not-staged")
        {
            retirementCode = $"{retirementCode};{removal.Code}";
        }

        return new(
            trust.Code, "cbi35", true, false,
            chain.LaunchPolicyIdentity, current.Policy.Identity, null, retirementCode);
    }
}
