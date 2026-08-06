namespace Brontide.Reference.Studio;

public sealed record ProviderPolicyAuthorityCycleSchedule
{
    private ProviderPolicyAuthorityCycleSchedule(
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

    public static ProviderPolicyAuthorityCycleSchedule Create(
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
        // CBI58 refuses a longer attempt timeout, so a schedule that carried one could never be run.
        if (attemptTimeout <= TimeSpan.Zero || attemptTimeout > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(attemptTimeout));
        return new(maximumAttempts, baseDelay, backoffMultiplier, maximumDelay, attemptTimeout);
    }

    /// <summary>
    /// The gap before the retry that follows <paramref name="consecutiveFailures"/> consecutive
    /// failures. An applied rotation is progress and resets the count, so this is not a function of
    /// the attempt index. It carries no jitter, which is what lets a shared vector pin an exact gap
    /// sequence across two independent realizations.
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
/// Retains the authority floor outside the checkpoint it describes. CBI38 detects an authority
/// rollback only against state held independently of the file it guards, so custody is the host's.
/// </summary>
public interface IProviderPolicyAuthorityFloorSink
{
    Task RetainAsync(ProviderPolicyAuthorityFloor floor, CancellationToken cancellationToken);
}

/// <summary>
/// The cycle's only source of elapsed time. It answers with the instant the gap ended, so a cycle is
/// a function of what the host injects rather than of an ambient clock.
/// </summary>
public interface IProviderPolicyAuthorityCycleDelay
{
    Task<DateTimeOffset> DelayAsync(DateTimeOffset now, TimeSpan duration, CancellationToken cancellationToken);
}

public sealed record ProviderPolicyAuthorityCycleResult(
    string Code,
    string? LastAttemptCode,
    int Attempts,
    IReadOnlyList<TimeSpan> Delays,
    IReadOnlyList<long> AppliedGenerations,
    IReadOnlyList<long> RetainedGenerations,
    long Generation,
    ProviderPublisherTrustPolicyAuthorityId ActiveAuthority,
    ProviderPolicyAuthorityFloor Floor)
{
    public bool IsCurrent => Code == "policy-authority-cycle-current";
}

/// <summary>
/// One bounded, host-driven cycle of CBI58 rotation attempts. It is a call the host makes, not a
/// daemon: nothing here decides when the host makes it or keeps a schedule across the process.
/// </summary>
public sealed class ProviderPolicyAuthorityRotationCycle
{
    private readonly DurableProviderPublisherTrustPolicyRegistry registry;
    private readonly ProviderPolicyAuthorityRotationDistributionClient client;
    private readonly ProviderPolicyAuthorityCycleSchedule schedule;

    public ProviderPolicyAuthorityRotationCycle(
        DurableProviderPublisherTrustPolicyRegistry registry,
        ProviderPublisherTrustPolicyDistributionEndpointId endpointIdentity,
        ProviderPolicyAuthorityCycleSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(schedule);
        this.registry = registry;
        // The client is constructed here rather than accepted, so a cycle cannot report on one
        // registry while advancing another.
        client = new(registry, endpointIdentity);
        this.schedule = schedule;
    }

    public async Task<ProviderPolicyAuthorityCycleResult> RunAsync(
        IProviderPolicyAuthorityRotationDistributionSource source,
        IProviderPolicyAuthorityFloorSink sink,
        IProviderPolicyAuthorityCycleDelay delay,
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
                return Result("policy-authority-cycle-canceled", lastAttemptCode, attempts, delays, applied, retained);
            if (attempts >= schedule.MaximumAttempts)
                return Result("policy-authority-cycle-exhausted", lastAttemptCode, attempts, delays, applied, retained);

            if (attempts > 0)
            {
                var duration = schedule.DelayForConsecutiveFailures(consecutiveFailures);
                try
                {
                    instant = await delay.DelayAsync(instant, duration, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return Result("policy-authority-cycle-canceled", lastAttemptCode, attempts, delays, applied, retained);
                }
                // A gap is recorded only once it has been waited, so a cancelled gap is not one.
                delays.Add(duration);
                if (cancellationToken.IsCancellationRequested)
                    return Result("policy-authority-cycle-canceled", lastAttemptCode, attempts, delays, applied, retained);
            }

            attempts++;
            var attempt = await client
                .SynchronizeAsync(source, instant, schedule.AttemptTimeout, cancellationToken)
                .ConfigureAwait(false);
            lastAttemptCode = attempt.Code;

            if (attempt.Code == "policy-authority-distribution-current")
                return new("policy-authority-cycle-current", lastAttemptCode, attempts, delays, applied, retained,
                    attempt.Generation, attempt.ActiveAuthority, attempt.Floor);

            if (attempt.IsApplied)
            {
                applied.Add(attempt.Floor.Generation);
                consecutiveFailures = 0;
                // CBI57 publishes the rotation into the retained chain before advancing the live
                // authority, so the floor describing it cannot be offered any earlier than here.
                try
                {
                    await sink.RetainAsync(attempt.Floor, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OutOfMemoryException)
                {
                    return new("policy-authority-cycle-floor-unretained", lastAttemptCode, attempts, delays,
                        applied, retained, attempt.Generation, attempt.ActiveAuthority, attempt.Floor);
                }
                retained.Add(attempt.Floor.Generation);
                continue;
            }

            if (attempt.Code == "policy-authority-distribution-canceled")
                return new("policy-authority-cycle-canceled", lastAttemptCode, attempts, delays, applied, retained,
                    attempt.Generation, attempt.ActiveAuthority, attempt.Floor);

            if (!IsRetryable(attempt.Code))
                return new("policy-authority-cycle-refused", lastAttemptCode, attempts, delays, applied, retained,
                    attempt.Generation, attempt.ActiveAuthority, attempt.Floor);

            consecutiveFailures++;
        }
    }

    /// <summary>
    /// A retry changes the challenge, the cursor read from the registry, and the network. Every
    /// endpoint-authentication outcome is decided by a key the retry does not change, and every
    /// native CBI57 refusal by a statement the endpoint would send again.
    /// </summary>
    private static bool IsRetryable(string code) => code is
        "policy-authority-distribution-transport-failed"
        or "policy-authority-distribution-timeout"
        or "policy-authority-distribution-stale"
        or "policy-authority-distribution-superseded";

    private ProviderPolicyAuthorityCycleResult Result(
        string code,
        string? lastAttemptCode,
        int attempts,
        IReadOnlyList<TimeSpan> delays,
        IReadOnlyList<long> applied,
        IReadOnlyList<long> retained) =>
        new(code, lastAttemptCode, attempts, delays, applied, retained,
            registry.AuthorityGeneration, registry.ActiveAuthorityIdentity, registry.AuthorityFloor);
}
