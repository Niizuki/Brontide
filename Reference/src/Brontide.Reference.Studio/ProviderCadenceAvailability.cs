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
/// The baseline it holds is the instant of the most recent cycle whose poll established current
/// policy. An outage never refreshes it, which is what makes the deadline arrive — a cadence that took
/// each cycle's own instant would report existing service forever, and CBI49's own vectors cannot see
/// the difference because they evaluate once. A resumed cadence is given the baseline CBI65 derives
/// from what CBI48 committed, so a crash inside an outage does not restart grace.
/// </summary>
public sealed class ProviderAvailabilityTrustCycle(
    IProviderServingTrustCycle inner,
    IProviderOfflineEnforcementCycle enforcement,
    DateTimeOffset? baseline = null) : IProviderServingTrustCycle
{
    private DateTimeOffset? lastCurrent = baseline;

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

public sealed record ProviderTrustCadenceAvailabilityBaseline(string Code, DateTimeOffset? Instant);

/// <summary>
/// Recovers CBI64's availability baseline from what CBI48 already committed. The journal has recorded
/// each cycle's instant and code since CBI48, so nothing new is written and nothing is written here:
/// a record written about a derivation would be a less trustworthy copy of the record it read, which
/// is the reasoning CBI62 established and CBI63 applied.
/// </summary>
public static class ProviderTrustCadenceAvailabilityRecovery
{
    public static ProviderTrustCadenceAvailabilityBaseline Derive(
        ProviderTrustCadenceJournalSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        DateTimeOffset? baseline = null;
        foreach (var cycle in snapshot.Cycles)
        {
            // The vocabulary answers this rather than a list here, so a later cycle code cannot be
            // added without deciding what it means for a baseline. A code it does not classify is
            // refused: `provider-trust-cycle-stopped` covers both a poll that was not current and a
            // current poll whose sweep failed, and nothing in the record says which.
            switch (ProviderServingTrustCycleCodes.Establishes(cycle.Code))
            {
                case null:
                    return new("cadence-baseline-observation-invalid", null);
                case true:
                    baseline = cycle.Instant;
                    break;
                case false:
                    break;
            }
        }

        return baseline is null
            ? new("cadence-baseline-absent", null)
            : new("cadence-baseline-derived", baseline);
    }
}
