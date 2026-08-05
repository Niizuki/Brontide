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
    private readonly object _restartSync = new();
    private int _restartState;

    internal ProviderServingActivation(
        ProviderDistributionChainResult chain,
        ComponentBindingLifecycleResult? lifecycle,
        Brontide.Reference.Experimental.ComponentManagement.OccurrenceId occurrence,
        Brontide.Reference.Experimental.ComponentManagement.ResolutionOutcome resolution,
        ComponentBindingSelection selection,
        Brontide.Reference.Experimental.ComponentManagement.ActivationRuntimeRequest request)
    {
        Chain = chain;
        Lifecycle = lifecycle;
        Occurrence = occurrence;
        Resolution = resolution;
        Selection = selection;
        Request = request;
    }

    internal ProviderDistributionChainResult Chain { get; }
    internal ComponentBindingLifecycleResult? Lifecycle { get; }
    internal Brontide.Reference.Experimental.ComponentManagement.ResolutionOutcome Resolution { get; }
    internal ComponentBindingSelection Selection { get; }
    internal Brontide.Reference.Experimental.ComponentManagement.ActivationRuntimeRequest Request { get; }

    public Brontide.Reference.Experimental.ComponentManagement.OccurrenceId Occurrence { get; }

    public bool IsServing =>
        Chain.Provider is { HasExited: false } && Lifecycle?.IsActive == true;

    public bool MemberReleased => Lifecycle?.Member?.IsReleased == true;

    internal string BeginRestart()
    {
        lock (_restartSync)
        {
            return _restartState switch
            {
                0 => Claim(),
                1 => "provider-restart-in-progress",
                _ => "provider-restart-already-completed",
            };
        }

        string Claim()
        {
            _restartState = 1;
            return "restart-claimed";
        }
    }

    internal void FinishRestart(bool completed)
    {
        lock (_restartSync)
        {
            _restartState = completed ? 2 : 0;
        }
    }

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
            return new(chain, null, selection.Occurrence, resolution, selection, request);
        }

        var lifecycle = await ComponentBindingLifecycle.ActivateAsync(
            resolution, selection, request, provider.Conversation, cancellationToken).ConfigureAwait(false);
        return new(chain, lifecycle, selection.Occurrence, resolution, selection, request);
    }

    public static async ValueTask<ProviderServingTrustRevalidationResult> RevalidateAsync(
        DurableProviderPublisherTrustPolicyRegistry registry,
        ContentAddressedProviderStore store,
        ProviderServingActivation activation,
        string retirementReason,
        CancellationToken cancellationToken = default) =>
        await RevalidateAsync(
            registry, store, activation, retirementReason, removeStagedSet: true, cancellationToken)
            .ConfigureAwait(false);

    internal static async ValueTask<ProviderServingTrustRevalidationResult> RevalidateAsync(
        DurableProviderPublisherTrustPolicyRegistry registry,
        ContentAddressedProviderStore store,
        ProviderServingActivation activation,
        string retirementReason,
        bool removeStagedSet,
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
        if (removeStagedSet)
        {
            var removal = store.Remove(chain.StagedIdentity.Value);
            if (!removal.Removed && removal.Code != "artifact-set-not-staged")
            {
                retirementCode = $"{retirementCode};{removal.Code}";
            }
        }

        return new(
            trust.Code, "cbi35", true, false,
            chain.LaunchPolicyIdentity, current.Policy.Identity, null, retirementCode);
    }
}

public sealed record ProviderServingTrustSweepMember(
    Brontide.Reference.Experimental.ComponentManagement.OccurrenceId Occurrence,
    ProviderServingTrustRevalidationResult Result);

public sealed record ProviderServingTrustSweepResult(
    string Code,
    string RefusedBy,
    IReadOnlyList<ProviderServingTrustSweepMember> Members,
    int ContinuedCount,
    int WithdrawnCount);

public sealed record ProviderOfflineServiceEnforcementMember(
    Brontide.Reference.Experimental.ComponentManagement.OccurrenceId Occurrence,
    string RetirementCode,
    bool ProviderStopped);

public sealed record ProviderOfflineServiceEnforcementResult(
    string Code,
    string RefusedBy,
    ProviderTrustOfflineDecision? Decision,
    IReadOnlyList<ProviderOfflineServiceEnforcementMember> Members,
    int AdmittedCount,
    int StoppedCount);

/// <summary>
/// Applies one offline-policy decision to the exact supplied serving set. Availability withdrawal
/// stops service but deliberately retains staged artifacts for the separate restart policy.
/// </summary>
public static class ProviderOfflineServiceEnforcement
{
    public const int MaximumMembers = 64;

    public static async ValueTask<ProviderOfflineServiceEnforcementResult> RunAsync(
        ProviderTrustOfflinePolicy policy,
        DateTimeOffset now,
        DateTimeOffset? lastCurrent,
        string pollCode,
        string? lastAttemptCode,
        IReadOnlyList<ProviderServingActivation> activations,
        string retirementReason)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(activations);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (activations.Count > MaximumMembers
            || activations.Any(activation => activation is null || !activation.IsServing)
            || activations.Select(activation => activation.Occurrence).Distinct().Count() != activations.Count)
        {
            return new("offline-enforcement-invalid", "preflight", null,
                Array.Empty<ProviderOfflineServiceEnforcementMember>(), 0, 0);
        }

        var decision = policy.Evaluate(now, lastCurrent, pollCode, lastAttemptCode, activations.Count);
        if (decision.Code == "offline-existing-service")
            return new("offline-enforcement-continuing", "none", decision,
                Array.Empty<ProviderOfflineServiceEnforcementMember>(), activations.Count, 0);
        if (decision.Code == "offline-idle")
            return new("offline-enforcement-idle", "none", decision,
                Array.Empty<ProviderOfflineServiceEnforcementMember>(), 0, 0);

        var members = new List<ProviderOfflineServiceEnforcementMember>(activations.Count);
        foreach (var activation in activations.OrderBy(
                     activation => activation.Occurrence.Value, StringComparer.Ordinal))
        {
            var retirementCode = "retirement-not-attempted";
            if (activation.Lifecycle?.Member is { IsReleased: true } member)
            {
                try
                {
                    await member.RetireAsync(retirementReason, CancellationToken.None).ConfigureAwait(false);
                    retirementCode = "retired";
                }
                catch (Exception)
                {
                    retirementCode = "retirement-failed";
                }
            }

            var providerStopped = activation.Chain.Provider?.HasExited != false;
            if (activation.Chain.Provider is { HasExited: false } provider)
            {
                try
                {
                    await provider.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Observe the concrete process after disposal: an exception does not prove that
                    // termination failed, and a successful return does not replace that observation.
                }
                providerStopped = provider.HasExited;
            }
            members.Add(new(activation.Occurrence, retirementCode, providerStopped));
        }

        var stopped = members.Count(member => member.ProviderStopped);
        var code = stopped != members.Count
            ? "offline-enforcement-incomplete"
            : members.Any(member => member.RetirementCode != "retired")
                ? "offline-enforcement-cleanup-incomplete"
                : "offline-enforcement-stopped";
        return new(code, "none", decision, members, activations.Count, stopped);
    }
}

/// <summary>
/// Applies one deterministic, bounded host-owned trust sweep. Invocation cadence remains external.
/// </summary>
public static class ProviderServingTrustSweep
{
    public const int MaximumMembers = 64;

    public static async ValueTask<ProviderServingTrustSweepResult> RunAsync(
        DurableProviderPublisherTrustPolicyRegistry registry,
        ContentAddressedProviderStore store,
        IReadOnlyList<ProviderServingActivation> activations,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activations);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (activations.Count is < 1 or > MaximumMembers
            || activations.Any(activation => activation is null || !activation.IsServing)
            || activations.Select(activation => activation.Occurrence).Distinct().Count() != activations.Count)
        {
            return new(
                "serving-trust-sweep-invalid", "preflight",
                Array.Empty<ProviderServingTrustSweepMember>(), 0, 0);
        }

        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(store);

        var ordered = activations
            .OrderBy(activation => activation.Occurrence.Value, StringComparer.Ordinal)
            .ToArray();
        var members = new List<ProviderServingTrustSweepMember>(ordered.Length);

        foreach (var activation in ordered)
        {
            var result = await ProviderServingTrustRevalidation.RevalidateAsync(
                registry, store, activation, retirementReason, removeStagedSet: false, cancellationToken)
                .ConfigureAwait(false);
            members.Add(new(activation.Occurrence, result));
        }

        foreach (var stagedGroup in ordered
                     .Where(activation => activation.Chain.StagedIdentity is not null)
                     .GroupBy(activation => activation.Chain.StagedIdentity!.Value))
        {
            var occurrences = stagedGroup.Select(activation => activation.Occurrence).ToHashSet();
            if (members.Any(member => occurrences.Contains(member.Occurrence)
                                      && (member.Result.Continued || !member.Result.Revalidated)))
            {
                continue;
            }

            var removal = store.Remove(stagedGroup.Key);
            if (!removal.Removed && removal.Code != "artifact-set-not-staged")
            {
                for (var index = 0; index < members.Count; index++)
                {
                    if (occurrences.Contains(members[index].Occurrence)
                        && !members[index].Result.Continued)
                    {
                        members[index] = members[index] with
                        {
                            Result = members[index].Result with
                            {
                                RetirementCode = $"{members[index].Result.RetirementCode};{removal.Code}",
                            },
                        };
                    }
                }
            }
        }

        var continued = members.Count(member => member.Result.Continued);
        var code = members.Any(member => !member.Result.Revalidated)
            ? "serving-trust-sweep-incomplete"
            : members.Any(member => !member.Result.Continued && member.Result.RetirementCode != "retired")
                ? "serving-trust-sweep-cleanup-incomplete"
                : continued == members.Count
                    ? "serving-trust-sweep-current"
                    : "serving-trust-sweep-withdrawn";

        return new(code, "none", members, continued, members.Count - continued);
    }
}
