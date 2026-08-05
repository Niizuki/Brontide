namespace Brontide.Reference.Studio;

public sealed record ProviderServingTrustCadenceSchedule
{
    private ProviderServingTrustCadenceSchedule(int maximumCycles, TimeSpan interval)
    {
        MaximumCycles = maximumCycles;
        Interval = interval;
    }

    public int MaximumCycles { get; }
    public TimeSpan Interval { get; }

    public static ProviderServingTrustCadenceSchedule Create(int maximumCycles, TimeSpan interval)
    {
        if (maximumCycles is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(maximumCycles));
        if (interval <= TimeSpan.Zero || interval > TimeSpan.FromHours(24))
            throw new ArgumentOutOfRangeException(nameof(interval));
        return new(maximumCycles, interval);
    }
}

public interface IProviderPublisherTrustPolicyCycle
{
    Task<ProviderPublisherTrustPolicyPollResult> PollAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

/// <summary>Binds one CBI41 poller to the endpoint, floor, and delay owned by its host.</summary>
public sealed class ProviderPublisherTrustPolicyCycle(
    ProviderPublisherTrustPolicyPoller poller,
    IProviderPublisherTrustPolicyDistributionSource source,
    IProviderPublisherTrustPolicyFloorSink floorSink,
    IProviderPublisherTrustPolicyPollDelay delay) : IProviderPublisherTrustPolicyCycle
{
    public Task<ProviderPublisherTrustPolicyPollResult> PollAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        poller.PollAsync(source, floorSink, delay, now, cancellationToken);
}

public interface IProviderServingTrustSweepCycle
{
    /// <summary>Returns null only when the current serving-set snapshot is empty.</summary>
    ValueTask<ProviderServingTrustSweepResult?> SweepAsync(CancellationToken cancellationToken);
}

/// <summary>Snapshots the serving set after policy polling and invokes CBI46 when it is non-empty.</summary>
public sealed class ProviderServingTrustSweepCycle(
    DurableProviderPublisherTrustPolicyRegistry registry,
    ContentAddressedProviderStore store,
    Func<CancellationToken, ValueTask<IReadOnlyList<ProviderServingActivation>>> servingSet,
    string retirementReason) : IProviderServingTrustSweepCycle
{
    public async ValueTask<ProviderServingTrustSweepResult?> SweepAsync(CancellationToken cancellationToken)
    {
        var activations = await servingSet(cancellationToken).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(activations);
        if (activations.Count == 0) return null;
        return await ProviderServingTrustSweep.RunAsync(
            registry, store, activations, retirementReason, cancellationToken).ConfigureAwait(false);
    }
}

public sealed record ProviderServingTrustCycleResult(
    string Code,
    ProviderPublisherTrustPolicyPollResult Poll,
    ProviderServingTrustSweepResult? Sweep,
    int ServingCount)
{
    public bool CanContinue => Code is
        "provider-trust-cycle-current" or "provider-trust-cycle-withdrawn";
    public bool IsCanceled => Code == "provider-trust-cycle-canceled";
}

public interface IProviderServingTrustCycle
{
    Task<ProviderServingTrustCycleResult> RunAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

/// <summary>Composes one current-policy poll with at most one current-serving-set sweep.</summary>
public sealed class ProviderServingTrustCycle(
    IProviderPublisherTrustPolicyCycle policy,
    IProviderServingTrustSweepCycle serving) : IProviderServingTrustCycle
{
    public async Task<ProviderServingTrustCycleResult> RunAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var poll = await policy.PollAsync(now, cancellationToken).ConfigureAwait(false);
        if (poll.Code == "policy-poll-canceled")
            return new("provider-trust-cycle-canceled", poll, null, 0);
        if (!poll.IsCurrent)
            return new("provider-trust-cycle-stopped", poll, null, 0);

        var sweep = await serving.SweepAsync(cancellationToken).ConfigureAwait(false);
        if (sweep is null)
            return new("provider-trust-cycle-current", poll, null, 0);
        return sweep.Code switch
        {
            "serving-trust-sweep-current" =>
                new("provider-trust-cycle-current", poll, sweep, sweep.Members.Count),
            "serving-trust-sweep-withdrawn" =>
                new("provider-trust-cycle-withdrawn", poll, sweep, sweep.Members.Count),
            _ => new("provider-trust-cycle-stopped", poll, sweep, sweep.Members.Count),
        };
    }
}

public interface IProviderServingTrustCadenceDelay
{
    Task<DateTimeOffset> DelayAsync(
        DateTimeOffset now,
        TimeSpan duration,
        CancellationToken cancellationToken);
}

public sealed record ProviderServingTrustCadenceCycle(
    DateTimeOffset Instant,
    ProviderServingTrustCycleResult Result);

public sealed record ProviderServingTrustCadenceResult(
    string Code,
    IReadOnlyList<ProviderServingTrustCadenceCycle> Cycles,
    IReadOnlyList<TimeSpan> Gaps);

/// <summary>Runs a bounded host cadence without an ambient timer or hidden continuation.</summary>
public sealed class ProviderServingTrustCadence(ProviderServingTrustCadenceSchedule schedule)
{
    public async Task<ProviderServingTrustCadenceResult> RunAsync(
        IProviderServingTrustCycle cycle,
        IProviderServingTrustCadenceDelay delay,
        DateTimeOffset start,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cycle);
        ArgumentNullException.ThrowIfNull(delay);

        var cycles = new List<ProviderServingTrustCadenceCycle>();
        var gaps = new List<TimeSpan>();
        var instant = start;

        while (cycles.Count < schedule.MaximumCycles)
        {
            if (cancellationToken.IsCancellationRequested)
                return new("provider-trust-cadence-canceled", cycles, gaps);

            if (cycles.Count > 0)
            {
                DateTimeOffset next;
                try
                {
                    next = await delay.DelayAsync(instant, schedule.Interval, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return new("provider-trust-cadence-canceled", cycles, gaps);
                }
                if (cancellationToken.IsCancellationRequested)
                    return new("provider-trust-cadence-canceled", cycles, gaps);
                if (next <= instant)
                    throw new InvalidOperationException("A cadence delay must advance the cycle instant.");
                instant = next;
                gaps.Add(schedule.Interval);
            }

            var result = await cycle.RunAsync(instant, cancellationToken).ConfigureAwait(false);
            cycles.Add(new(instant, result));
            if (result.IsCanceled)
                return new("provider-trust-cadence-canceled", cycles, gaps);
            if (!result.CanContinue)
                return new("provider-trust-cadence-stopped", cycles, gaps);
        }

        return new("provider-trust-cadence-complete", cycles, gaps);
    }
}
