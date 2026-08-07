namespace Brontide.Reference.Studio;

/// <summary>
/// Decides what an unavailable policy endpoint means for the providers a cadence is watching, and
/// performs whatever that decision requires. The seam returns the cycle's projection of CBI50 rather
/// than CBI50's result, so a cadence never depends on the enforcement component's types.
/// </summary>
public interface IProviderOfflineEnforcementCycle
{
    ValueTask<ProviderTrustCycleAvailability> EnforceAsync(
        DateTimeOffset now,
        DateTimeOffset? lastCurrent,
        string pollCode,
        string? lastAttemptCode,
        CancellationToken cancellationToken);
}

/// <summary>Binds one CBI49 policy and CBI50 enforcement to the serving set owned by its host.</summary>
public sealed class ProviderOfflineEnforcementCycle(
    ProviderTrustOfflinePolicy policy,
    Func<CancellationToken, ValueTask<IReadOnlyList<ProviderServingActivation>>> servingSet,
    string retirementReason) : IProviderOfflineEnforcementCycle
{
    public async ValueTask<ProviderTrustCycleAvailability> EnforceAsync(
        DateTimeOffset now,
        DateTimeOffset? lastCurrent,
        string pollCode,
        string? lastAttemptCode,
        CancellationToken cancellationToken)
    {
        var activations = await servingSet(cancellationToken).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(activations);
        var result = await ProviderOfflineServiceEnforcement.RunAsync(
            policy, now, lastCurrent, pollCode, lastAttemptCode, activations, retirementReason)
            .ConfigureAwait(false);
        return new(
            result.Code, result.Decision?.Code, result.Decision?.Deadline, result.Decision?.RetryAt,
            result.AdmittedCount, result.StoppedCount);
    }
}

/// <summary>
/// Applies CBI49's availability policy to a cadence that cannot establish current policy, and CBI50's
/// enforcement to whatever that decides. It wraps the whole cycle rather than sitting inside it,
/// because the cycle code it must leave alone is the one CBI61's governed wrapper computes.
///
/// One instance belongs to one cadence run: the baseline it holds is the instant of the most recent
/// cycle of that run whose poll established current policy. An outage never refreshes it, which is
/// what makes the deadline arrive — a cadence that took each cycle's own instant would report existing
/// service forever, and CBI49's own vectors cannot see the difference because they evaluate once.
/// </summary>
public sealed class ProviderAvailabilityTrustCycle(
    IProviderServingTrustCycle inner,
    IProviderOfflineEnforcementCycle enforcement) : IProviderServingTrustCycle
{
    private DateTimeOffset? lastCurrent;

    public async Task<ProviderServingTrustCycleResult> RunAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var result = await inner.RunAsync(now, cancellationToken).ConfigureAwait(false);
        if (result.Poll is { IsCurrent: true })
        {
            lastCurrent = now;
            return result;
        }

        // Cancellation is the host stopping its own loop rather than the endpoint failing, and a cycle
        // its rotation stopped never asked the endpoint anything. Neither is an availability
        // observation, and CBI49 has no code that means "no poll was made".
        if (result.IsCanceled || result.Poll is null) return result;

        var availability = await enforcement.EnforceAsync(
            now, lastCurrent, result.Poll.Code, result.Poll.LastAttemptCode, cancellationToken)
            .ConfigureAwait(false);
        return result with
        {
            Code = availability.PermitsContinuation
                ? ProviderServingTrustCycleCodes.Offline
                : result.Code,
            Availability = availability,
        };
    }
}
