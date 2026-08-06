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

/// <summary>
/// Every code a trust cycle can return, with whether the cadence may continue after it. Producers
/// use these constants rather than literals and CBI48's journal validates against this set, so a new
/// code cannot be produced without the journal knowing it — which is what CBI61's two additions did
/// before this was one vocabulary.
/// </summary>
public static class ProviderServingTrustCycleCodes
{
    public const string Current = "provider-trust-cycle-current";
    public const string Withdrawn = "provider-trust-cycle-withdrawn";
    public const string Stopped = "provider-trust-cycle-stopped";
    public const string Canceled = "provider-trust-cycle-canceled";
    public const string AuthorityBehind = "provider-trust-cycle-authority-behind";
    public const string AuthorityUnretained = "provider-trust-cycle-authority-unretained";

    private static readonly Dictionary<string, bool> Continuing = new(StringComparer.Ordinal)
    {
        [Current] = true,
        [Withdrawn] = true,
        [Stopped] = false,
        [Canceled] = false,
        [AuthorityBehind] = false,
        [AuthorityUnretained] = false,
    };

    public static IReadOnlyCollection<string> All => Continuing.Keys;

    public static bool IsKnown(string code) => code is not null && Continuing.ContainsKey(code);

    public static bool Continues(string code) =>
        code is not null && Continuing.TryGetValue(code, out var value) && value;
}

/// <summary>
/// One cycle's observations. <paramref name="Rotation"/> is absent for a cadence that does not
/// govern authority rotation, which is every cadence composed before CBI61; a governed cycle carries
/// the CBI60 result it ran before the poll, and <paramref name="Poll"/> is absent when that rotation
/// stopped the cycle before the policy endpoint was contacted.
/// </summary>
public sealed record ProviderServingTrustCycleResult(
    string Code,
    ProviderPublisherTrustPolicyPollResult? Poll,
    ProviderServingTrustSweepResult? Sweep,
    int ServingCount,
    ProviderPolicyAuthorityCycleResult? Rotation = null)
{
    public bool CanContinue => ProviderServingTrustCycleCodes.Continues(Code);
    public bool IsCanceled => Code == ProviderServingTrustCycleCodes.Canceled;
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
            return new(ProviderServingTrustCycleCodes.Canceled, poll, null, 0);
        if (!poll.IsCurrent)
            return new(ProviderServingTrustCycleCodes.Stopped, poll, null, 0);

        var sweep = await serving.SweepAsync(cancellationToken).ConfigureAwait(false);
        if (sweep is null)
            return new(ProviderServingTrustCycleCodes.Current, poll, null, 0);
        return sweep.Code switch
        {
            "serving-trust-sweep-current" =>
                new(ProviderServingTrustCycleCodes.Current, poll, sweep, sweep.Members.Count),
            "serving-trust-sweep-withdrawn" =>
                new(ProviderServingTrustCycleCodes.Withdrawn, poll, sweep, sweep.Members.Count),
            _ => new(ProviderServingTrustCycleCodes.Stopped, poll, sweep, sweep.Members.Count),
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
