namespace Brontide.Reference.Studio;

public sealed record ProviderPublisherTrustPolicyPollSchedule
{
    private ProviderPublisherTrustPolicyPollSchedule(
        int maximumAttempts,
        TimeSpan baseDelay,
        int backoffMultiplier,
        TimeSpan maximumDelay,
        TimeSpan attemptTimeout)
    {
        MaximumAttempts = maximumAttempts;
        BaseDelay = baseDelay;
        BackoffMultiplier = backoffMultiplier;
        MaximumDelay = maximumDelay;
        AttemptTimeout = attemptTimeout;
    }

    public int MaximumAttempts { get; }
    public TimeSpan BaseDelay { get; }
    public int BackoffMultiplier { get; }
    public TimeSpan MaximumDelay { get; }
    public TimeSpan AttemptTimeout { get; }

    public static ProviderPublisherTrustPolicyPollSchedule Create(
        int maximumAttempts,
        TimeSpan baseDelay,
        int backoffMultiplier,
        TimeSpan maximumDelay,
        TimeSpan attemptTimeout)
    {
        if (maximumAttempts is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        if (baseDelay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(baseDelay));
        if (backoffMultiplier is < 1 or > 16) throw new ArgumentOutOfRangeException(nameof(backoffMultiplier));
        if (maximumDelay < baseDelay || maximumDelay > TimeSpan.FromHours(1))
            throw new ArgumentOutOfRangeException(nameof(maximumDelay));
        // CBI39 refuses a longer attempt timeout, so a schedule that carried one could never be run.
        if (attemptTimeout <= TimeSpan.Zero || attemptTimeout > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(attemptTimeout));
        return new(maximumAttempts, baseDelay, backoffMultiplier, maximumDelay, attemptTimeout);
    }

    /// <summary>
    /// The gap before the retry that follows <paramref name="consecutiveFailures"/> consecutive
    /// failures. Progress resets the count, so this is not a function of the attempt index.
    /// </summary>
    public TimeSpan DelayForConsecutiveFailures(int consecutiveFailures)
    {
        if (consecutiveFailures <= 0) return TimeSpan.Zero;
        var ticks = BaseDelay.Ticks;
        // Clamping at every step keeps a long budget from overflowing before reaching the cap that
        // would have bounded the product anyway.
        for (var index = 1; index < consecutiveFailures && ticks < MaximumDelay.Ticks; index++)
            ticks = BackoffMultiplier > MaximumDelay.Ticks / ticks
                ? MaximumDelay.Ticks
                : ticks * BackoffMultiplier;
        return TimeSpan.FromTicks(Math.Min(ticks, MaximumDelay.Ticks));
    }
}

/// <summary>
/// Retains a recovery floor outside the checkpoint it describes. CBI38 detects rollback only against
/// state held independently of the file it guards, so custody of this value is the host's.
/// </summary>
public interface IProviderPublisherTrustPolicyFloorSink
{
    Task RetainAsync(
        ProviderPublisherTrustPolicyRecoveryFloor floor,
        CancellationToken cancellationToken);
}

/// <summary>
/// The cycle's only source of elapsed time. It returns the instant the gap ended, so a cycle is a
/// function of what the host injects rather than of an ambient clock.
/// </summary>
public interface IProviderPublisherTrustPolicyPollDelay
{
    Task<DateTimeOffset> DelayAsync(
        DateTimeOffset now,
        TimeSpan duration,
        CancellationToken cancellationToken);
}

public sealed record ProviderPublisherTrustPolicyPollResult(
    string Code,
    string? LastAttemptCode,
    int Attempts,
    IReadOnlyList<TimeSpan> Delays,
    IReadOnlyList<long> AppliedSequences,
    IReadOnlyList<long> RetainedSequences,
    VerifiedProviderPublisherTrustPolicySnapshot? Current,
    ProviderPublisherTrustPolicyRecoveryFloor Floor)
{
    public bool IsCurrent => Code == "policy-poll-current";
}

public sealed class ProviderPublisherTrustPolicyPoller
{
    private readonly DurableProviderPublisherTrustPolicyRegistry registry;
    private readonly ProviderPublisherTrustPolicyDistributionClient client;
    private readonly ProviderPublisherTrustPolicyPollSchedule schedule;

    public ProviderPublisherTrustPolicyPoller(
        DurableProviderPublisherTrustPolicyRegistry registry,
        ProviderPublisherTrustPolicyDistributionEndpointId endpointIdentity,
        ProviderPublisherTrustPolicyPollSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(schedule);
        this.registry = registry;
        // The client is constructed here rather than accepted, so a poller cannot report on one
        // registry while advancing another.
        client = new(registry, endpointIdentity);
        this.schedule = schedule;
    }

    public async Task<ProviderPublisherTrustPolicyPollResult> PollAsync(
        IProviderPublisherTrustPolicyDistributionSource source,
        IProviderPublisherTrustPolicyFloorSink sink,
        IProviderPublisherTrustPolicyPollDelay delay,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(delay);

        var delays = new List<TimeSpan>();
        var applied = new List<long>();
        var retained = new List<long>();
        var attempts = 0;
        var consecutiveFailures = 0;
        string? lastAttemptCode = null;
        var instant = now;

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
                return Result("policy-poll-canceled", lastAttemptCode, attempts, delays, applied, retained);
            if (attempts >= schedule.MaximumAttempts)
                return Result("policy-poll-exhausted", lastAttemptCode, attempts, delays, applied, retained);

            if (attempts > 0)
            {
                var duration = schedule.DelayForConsecutiveFailures(consecutiveFailures);
                try
                {
                    instant = await delay.DelayAsync(instant, duration, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return Result("policy-poll-canceled", lastAttemptCode, attempts, delays, applied, retained);
                }
                // A gap is recorded only once it has been waited, so a cancelled gap is not one.
                delays.Add(duration);
                if (cancellationToken.IsCancellationRequested)
                    return Result("policy-poll-canceled", lastAttemptCode, attempts, delays, applied, retained);
            }

            attempts++;
            var attempt = await client
                .SynchronizeAsync(source, instant, schedule.AttemptTimeout, cancellationToken)
                .ConfigureAwait(false);
            lastAttemptCode = attempt.Code;

            if (attempt.Code == "policy-distribution-current")
                return new("policy-poll-current", lastAttemptCode, attempts, delays, applied, retained,
                    attempt.Current, attempt.Floor);

            if (attempt.IsApplied)
            {
                applied.Add(attempt.Floor.Sequence);
                consecutiveFailures = 0;
                // The checkpoint is already published and live at this point; the floor describes it
                // and so cannot be offered any earlier.
                try
                {
                    await sink.RetainAsync(attempt.Floor, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OutOfMemoryException)
                {
                    return new("policy-poll-floor-unretained", lastAttemptCode, attempts, delays, applied,
                        retained, attempt.Current, attempt.Floor);
                }
                retained.Add(attempt.Floor.Sequence);
                continue;
            }

            if (attempt.Code == "policy-distribution-canceled")
                return new("policy-poll-canceled", lastAttemptCode, attempts, delays, applied, retained,
                    attempt.Current, attempt.Floor);

            if (!IsRetryable(attempt.Code))
                return new("policy-poll-refused", lastAttemptCode, attempts, delays, applied, retained,
                    attempt.Current, attempt.Floor);

            consecutiveFailures++;
        }
    }

    /// <summary>
    /// A retry changes the challenge, the cursor read, and the network. Everything else is decided by
    /// a key the retry does not change, by the caller, or by the registry's own state.
    /// </summary>
    private static bool IsRetryable(string code) => code is
        "policy-distribution-transport-failed"
        or "policy-distribution-timeout"
        or "policy-distribution-stale"
        or "policy-distribution-superseded";

    private ProviderPublisherTrustPolicyPollResult Result(
        string code,
        string? lastAttemptCode,
        int attempts,
        IReadOnlyList<TimeSpan> delays,
        IReadOnlyList<long> applied,
        IReadOnlyList<long> retained) =>
        new(code, lastAttemptCode, attempts, delays, applied, retained, registry.Current, registry.Floor);
}
