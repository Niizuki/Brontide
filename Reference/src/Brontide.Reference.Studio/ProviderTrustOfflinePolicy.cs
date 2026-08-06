namespace Brontide.Reference.Studio;

public sealed record ProviderTrustOfflineDecision(
    string Code,
    bool MayContinueExistingService,
    bool MayStartProvider,
    DateTimeOffset? Deadline,
    DateTimeOffset? RetryAt);

/// <summary>
/// One host-owned availability decision. Grace preserves existing service only; it is not a trust
/// assertion and never authorizes acquisition, launch, admission, or restart.
/// </summary>
public sealed class ProviderTrustOfflinePolicy
{
    private ProviderTrustOfflinePolicy(TimeSpan grace, TimeSpan retry)
    {
        Grace = grace;
        Retry = retry;
    }

    public TimeSpan Grace { get; }
    public TimeSpan Retry { get; }

    public static ProviderTrustOfflinePolicy Create(TimeSpan grace, TimeSpan retry)
    {
        if (grace <= TimeSpan.Zero || grace > TimeSpan.FromHours(24))
            throw new ArgumentOutOfRangeException(nameof(grace));
        if (retry <= TimeSpan.Zero || retry > grace)
            throw new ArgumentOutOfRangeException(nameof(retry));
        return new(grace, retry);
    }

    public ProviderTrustOfflineDecision Evaluate(
        DateTimeOffset now,
        DateTimeOffset? lastCurrent,
        string pollCode,
        string? lastAttemptCode,
        int servingCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pollCode);
        if (servingCount is < 0 or > 64 || lastCurrent > now
            || lastCurrent is { } observed
                && observed.Ticks > DateTimeOffset.MaxValue.Ticks - Grace.Ticks)
            return new("offline-observation-invalid", false, false, null, null);

        if (lastCurrent is null)
            return new("offline-service-stop-required", false, false, null, null);
        var deadline = lastCurrent.Value.Add(Grace);

        var eligible = pollCode == "policy-poll-exhausted"
            && lastAttemptCode is "policy-distribution-transport-failed"
                or "policy-distribution-timeout";
        if (!eligible)
            return new("offline-service-stop-required", false, false, deadline, null);
        if (now >= deadline)
            return new("offline-grace-expired", false, false, deadline, null);

        var remaining = deadline - now;
        var retryAt = Retry >= remaining ? deadline : now.Add(Retry);
        return servingCount == 0
            ? new("offline-idle", false, false, deadline, retryAt)
            : new("offline-existing-service", true, false, deadline, retryAt);
    }
}

public enum ProviderTrustCadenceReconciliationVerdict
{
    NoEffectsConfirmed,
    EffectsAccountedFor,
    Unknown,
}

public sealed record ProviderTrustCadenceReconciliationEvidence(
    ProviderTrustCadenceRunId RunIdentity,
    int AttemptIndex,
    DateTimeOffset AttemptInstant,
    ProviderTrustCadenceReconciliationVerdict Verdict);

/// <summary>
/// Checks that host evidence names the exact interrupted attempt before selecting CBI48's durable
/// transition. The host owns the evidence; this class does not infer external effects.
/// </summary>
public static class ProviderTrustCadenceReconciliation
{
    public static ProviderTrustCadenceJournalTransitionResult Apply(
        DurableProviderTrustCadenceJournal journal,
        ProviderTrustCadenceReconciliationEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(evidence);

        var snapshot = journal.Snapshot;
        if (snapshot.Phase != "in-flight")
            return new("cadence-reconciliation-not-required", snapshot);
        // One verdict for everything the cycle did was right while a cycle was one loop. A governed
        // attempt recorded the cursor it was about to act on, and two of the three things it can do
        // are derivable from that, so this verdict would be speaking for effects the host need not
        // have inspected.
        if (snapshot.Cursor is not null)
            return new("cadence-reconciliation-governed", snapshot);
        if (snapshot.RunIdentity != evidence.RunIdentity
            || snapshot.NextCycleIndex != evidence.AttemptIndex
            || snapshot.PreparedInstant != evidence.AttemptInstant)
            return new("cadence-reconciliation-mismatch", snapshot);

        return evidence.Verdict switch
        {
            ProviderTrustCadenceReconciliationVerdict.Unknown =>
                new("cadence-reconciliation-deferred", snapshot),
            ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed =>
                Map(journal.ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Retry),
                    "cadence-reconciliation-retry-ready"),
            ProviderTrustCadenceReconciliationVerdict.EffectsAccountedFor =>
                Map(journal.ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Abandon),
                    "cadence-reconciliation-abandoned"),
            _ => throw new ArgumentOutOfRangeException(nameof(evidence)),
        };
    }

    private static ProviderTrustCadenceJournalTransitionResult Map(
        ProviderTrustCadenceJournalTransitionResult result,
        string acceptedCode) =>
        result.Code is "durable-cadence-retry-ready" or "durable-cadence-abandoned"
            ? new(acceptedCode, result.Snapshot)
            : new(result.Code, result.Snapshot);
}
