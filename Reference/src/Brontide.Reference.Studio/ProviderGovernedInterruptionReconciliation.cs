namespace Brontide.Reference.Studio;

/// <summary>
/// CBI48's recovery advance for a governed run. The only difference is that it hands the durable
/// cursor to <c>BeginCycle</c>, so the write that already marks the attempt in-flight also records
/// the state the attempt is about to act on.
/// </summary>
public static class ProviderGovernedTrustCadenceRecovery
{
    public static async Task<ProviderTrustCadenceJournalTransitionResult> AdvanceAsync(
        DurableProviderTrustCadenceJournal journal,
        DurableProviderPublisherTrustPolicyRegistry registry,
        IProviderServingTrustCycle cycle,
        IProviderServingTrustCadenceDelay delay,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(cycle);
        ArgumentNullException.ThrowIfNull(delay);

        var snapshot = journal.Snapshot;
        if (snapshot.Phase == "terminal") return new(snapshot.Code, snapshot);
        if (snapshot.Phase == "in-flight") return new("durable-cadence-indeterminate", snapshot);
        if (cancellationToken.IsCancellationRequested)
            return new("durable-cadence-wait-canceled", snapshot);

        if (snapshot.Phase == "waiting")
        {
            DateTimeOffset next;
            try
            {
                next = await delay.DelayAsync(
                    snapshot.PreparedInstant, snapshot.Interval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return new("durable-cadence-wait-canceled", journal.Snapshot);
            }
            if (cancellationToken.IsCancellationRequested)
                return new("durable-cadence-wait-canceled", journal.Snapshot);
            var gap = journal.CompleteGap(next);
            if (gap.Code != "durable-cadence-gap-completed") return gap;
        }

        var started = journal.BeginCycle(Cursor(registry));
        if (started.Code != "durable-cadence-cycle-started") return started;
        var result = await cycle.RunAsync(started.Snapshot.PreparedInstant, cancellationToken)
            .ConfigureAwait(false);
        return journal.CommitCycle(result.Code);
    }

    public static ProviderTrustCadenceJournalCursor Cursor(
        DurableProviderPublisherTrustPolicyRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var current = registry.Current;
        return new(registry.AuthorityGeneration, registry.ActiveAuthorityIdentity.Value,
            current?.Sequence ?? 0, current?.Policy.Identity.Value);
    }
}

/// <summary>
/// What the host observed about the serving set of an interrupted governed cycle. This is the only
/// verdict governed evidence carries: the rotation and the policy update record themselves durably,
/// and the sweep is the one thing CBI48 says no local journal can commit atomically with its cursor.
/// </summary>
public enum ProviderGovernedServingObservation
{
    NoEffectsConfirmed,
    EffectsAccountedFor,
    Unknown,
}

public sealed record ProviderGovernedReconciliationEvidence(
    ProviderTrustCadenceRunId RunIdentity,
    int AttemptIndex,
    DateTimeOffset AttemptInstant,
    ProviderGovernedServingObservation Serving);

/// <summary>
/// What the durable record says the interrupted attempt did, derived rather than asserted. Both
/// observations are reported next to the decision; neither is ever read from the evidence.
/// </summary>
public sealed record ProviderGovernedDerivedEffects(
    bool RotationApplied,
    long RecordedGeneration,
    long ObservedGeneration,
    bool PolicyApplied,
    long RecordedSequence,
    long ObservedSequence);

public sealed record ProviderGovernedReconciliationResult(
    string Code,
    ProviderTrustCadenceJournalSnapshot Snapshot,
    ProviderGovernedDerivedEffects? Derived);

/// <summary>
/// Reconciles an interrupted governed cycle. It derives what the local durable record already states
/// and requires the host to assert only what nothing can check.
/// </summary>
public static class ProviderGovernedInterruptionReconciliation
{
    public static ProviderGovernedReconciliationResult Apply(
        DurableProviderTrustCadenceJournal journal,
        ProviderGovernedReconciliationEvidence evidence,
        DurableProviderPublisherTrustPolicyRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(registry);

        var snapshot = journal.Snapshot;
        if (snapshot.Phase != "in-flight")
            return new("governed-reconciliation-not-required", snapshot, null);
        if (snapshot.RunIdentity != evidence.RunIdentity
            || snapshot.NextCycleIndex != evidence.AttemptIndex
            || snapshot.PreparedInstant != evidence.AttemptInstant)
            return new("governed-reconciliation-mismatch", snapshot, null);
        // A journal written before this slice has no baseline, and inventing one would be the guess
        // the whole design exists to avoid. Such a run is CBI49's, which is correct for it.
        if (snapshot.Cursor is null)
            return new("governed-reconciliation-cursor-absent", snapshot, null);

        var observedGeneration = registry.AuthorityGeneration;
        var observedSequence = registry.Current?.Sequence ?? 0;
        // Below the recorded cursor is a rollback the floors exist to prevent, so it is refused here
        // rather than reported as an absence of effect.
        if (observedGeneration < snapshot.Cursor.AuthorityGeneration
            || observedSequence < snapshot.Cursor.PolicySequence)
            return new("governed-reconciliation-cursor-regressed", snapshot, null);

        var derived = new ProviderGovernedDerivedEffects(
            observedGeneration > snapshot.Cursor.AuthorityGeneration,
            snapshot.Cursor.AuthorityGeneration, observedGeneration,
            observedSequence > snapshot.Cursor.PolicySequence,
            snapshot.Cursor.PolicySequence, observedSequence);

        // A derived effect is reported rather than used as a veto: CBI62 established that a retried
        // governed cycle cannot double-apply either half, so what decides is the one observation
        // nothing here can make for itself.
        return evidence.Serving switch
        {
            ProviderGovernedServingObservation.Unknown =>
                new("governed-reconciliation-deferred", journal.Snapshot, derived),
            ProviderGovernedServingObservation.NoEffectsConfirmed =>
                Map(journal.ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Retry),
                    "governed-reconciliation-retry-ready", derived),
            ProviderGovernedServingObservation.EffectsAccountedFor =>
                Map(journal.ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Abandon),
                    "governed-reconciliation-abandoned", derived),
            _ => throw new ArgumentOutOfRangeException(nameof(evidence)),
        };
    }

    private static ProviderGovernedReconciliationResult Map(
        ProviderTrustCadenceJournalTransitionResult transition,
        string acceptedCode,
        ProviderGovernedDerivedEffects derived) =>
        new(transition.Code is "durable-cadence-retry-ready" or "durable-cadence-abandoned"
            ? acceptedCode
            : transition.Code, transition.Snapshot, derived);
}
