namespace Brontide.Reference.Studio;

public enum ProviderRestartCause
{
    UnexpectedExit,
    OfflineAvailability,
    PublisherTrustWithdrawal,
    OperatorRetirement,
}

public sealed record ProviderRestartDecision(
    string Code,
    string RefusedBy,
    bool MayRestart,
    DateTimeOffset? RetryAt,
    ProviderPublisherTrustPolicyId? PolicyIdentity,
    TrustedProviderPublisherAuthorization? Authorization);

/// <summary>
/// Decides whether one stopped activation is eligible for a later restart effect. A current-cycle
/// policy identity is required because a retained registry snapshot alone proves no availability.
/// </summary>
public sealed class ProviderRestartPolicy
{
    private ProviderRestartPolicy(int maximumAttempts, TimeSpan delay)
    {
        MaximumAttempts = maximumAttempts;
        Delay = delay;
    }

    public int MaximumAttempts { get; }
    public TimeSpan Delay { get; }

    public static ProviderRestartPolicy Create(int maximumAttempts, TimeSpan delay)
    {
        if (maximumAttempts is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        if (delay <= TimeSpan.Zero || delay > TimeSpan.FromHours(1))
            throw new ArgumentOutOfRangeException(nameof(delay));
        return new(maximumAttempts, delay);
    }

    public ProviderRestartDecision Evaluate(
        DurableProviderPublisherTrustPolicyRegistry registry,
        ProviderServingActivation activation,
        ProviderRestartCause cause,
        ProviderPublisherTrustPolicyId currentCyclePolicyIdentity,
        DateTimeOffset now,
        int attemptCount,
        DateTimeOffset? lastAttempt)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(activation);

        if (attemptCount < 0 || attemptCount > MaximumAttempts
            || attemptCount == 0 && lastAttempt is not null
            || attemptCount > 0 && lastAttempt is null
            || lastAttempt > now
            || lastAttempt is { } observed
                && observed.Ticks > DateTimeOffset.MaxValue.Ticks - Delay.Ticks)
            return Denied("provider-restart-observation-invalid", "preflight");

        if (activation.IsServing)
            return Denied("provider-restart-not-required", "state");
        if (cause is ProviderRestartCause.PublisherTrustWithdrawal or ProviderRestartCause.OperatorRetirement)
            return Denied("provider-restart-cause-refused", "cause");
        if (cause is not ProviderRestartCause.UnexpectedExit and not ProviderRestartCause.OfflineAvailability)
            throw new ArgumentOutOfRangeException(nameof(cause));

        var chain = activation.Chain;
        var current = registry.Current;
        if (current is null
            || current.Policy.Identity != currentCyclePolicyIdentity
            || chain.PolicyAuthorityIdentity != registry.AuthorityIdentity)
            return Denied("provider-restart-current-proof-required", "current-cycle");
        if (chain.Provider is null || !chain.Provider.HasExited
            || chain.VerifiedEvidence is not { } evidence
            || chain.StagedIdentity is not { } stagedIdentity)
            return Denied("provider-restart-activation-unavailable", "state", current.Policy.Identity);

        var trust = ProviderPublisherTrustEvaluator.Evaluate(current.Policy, evidence);
        if (trust.Authorization is not { } authorization
            || authorization.ContentIdentity != stagedIdentity)
            return Denied(trust.Code, "cbi35", current.Policy.Identity);

        if (attemptCount == MaximumAttempts)
            return Denied("provider-restart-exhausted", "budget", current.Policy.Identity);
        if (lastAttempt is { } attempted)
        {
            var retryAt = attempted.Add(Delay);
            if (now < retryAt)
                return new("provider-restart-waiting", "none", false, retryAt,
                    current.Policy.Identity, authorization);
        }

        return new("provider-restart-ready", "none", true, null,
            current.Policy.Identity, authorization);
    }

    private static ProviderRestartDecision Denied(
        string code,
        string refusedBy,
        ProviderPublisherTrustPolicyId? policyIdentity = null) =>
        new(code, refusedBy, false, null, policyIdentity, null);
}
