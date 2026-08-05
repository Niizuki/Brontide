using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Brontide.Reference.Studio;

public readonly record struct ProviderTrustCadenceRunId
{
    private ProviderTrustCadenceRunId(string value) => Value = value;

    public string Value { get; }

    public static ProviderTrustCadenceRunId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 128 || value != value.Trim())
            throw new ArgumentException("A cadence run identity must contain 1-128 trimmed characters.", nameof(value));
        return new(value);
    }

    public override string ToString() => Value;
}

public enum ProviderTrustCadenceRecoveryDecision
{
    Retry,
    Abandon,
}

public sealed record ProviderTrustCadenceJournalCycle(int Index, DateTimeOffset Instant, string Code);

public sealed record ProviderTrustCadenceJournalSnapshot(
    ProviderTrustCadenceRunId RunIdentity,
    string Code,
    string Phase,
    int MaximumCycles,
    TimeSpan Interval,
    DateTimeOffset PreparedInstant,
    IReadOnlyList<ProviderTrustCadenceJournalCycle> Cycles,
    IReadOnlyList<TimeSpan> Gaps,
    int NextCycleIndex,
    int InterruptionCount,
    int RetryCount);

public sealed record ProviderTrustCadenceJournalOpenResult(
    string Code,
    DurableProviderTrustCadenceJournal? Journal);

public sealed record ProviderTrustCadenceJournalTransitionResult(
    string Code,
    ProviderTrustCadenceJournalSnapshot Snapshot);

/// <summary>
/// A host-local CBI48 progress journal. Its hash detects accidental damage, not a writer that can
/// replace the record and its hash. Cross-process ownership and secure custody are separate work.
/// </summary>
public sealed class DurableProviderTrustCadenceJournal
{
    private const int MaxBytes = 64 * 1024;
    private const int TagBytes = 32;
    private readonly object sync = new();
    private readonly string path;
    private JournalState state;

    private DurableProviderTrustCadenceJournal(string path, JournalState state)
    {
        this.path = path;
        this.state = state;
    }

    public ProviderTrustCadenceJournalSnapshot Snapshot
    {
        get { lock (sync) return Project(state); }
    }

    public static ProviderTrustCadenceJournalOpenResult Establish(
        string path,
        ProviderTrustCadenceRunId runIdentity,
        ProviderServingTrustCadenceSchedule schedule,
        DateTimeOffset start)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(schedule);
        if (string.IsNullOrWhiteSpace(runIdentity.Value))
            throw new ArgumentException("A valid cadence run identity is required.", nameof(runIdentity));
        path = Path.GetFullPath(path);
        TryDelete(path + ".tmp");
        if (File.Exists(path)) return new("durable-cadence-already-exists", null);

        var state = new JournalState
        {
            Format = "CBI48",
            RunIdentity = runIdentity.Value,
            MaximumCycles = schedule.MaximumCycles,
            IntervalTicks = schedule.Interval.Ticks,
            PreparedInstant = start,
            Phase = "ready",
            TerminalCode = null,
        };
        if (!TryWrite(path, state)) return new("durable-cadence-write-failed", null);
        return new("durable-cadence-established", new(path, state));
    }

    public static ProviderTrustCadenceJournalOpenResult Open(
        string path,
        ProviderTrustCadenceRunId runIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (string.IsNullOrWhiteSpace(runIdentity.Value))
            throw new ArgumentException("A valid cadence run identity is required.", nameof(runIdentity));
        path = Path.GetFullPath(path);
        TryDelete(path + ".tmp");
        if (!File.Exists(path)) return new("durable-cadence-missing", null);
        if (!TryRead(path, out var state)) return new("durable-cadence-corrupt", null);
        if (!StringComparer.Ordinal.Equals(state.RunIdentity, runIdentity.Value))
            return new("durable-cadence-run-mismatch", null);
        var journal = new DurableProviderTrustCadenceJournal(path, state);
        return new(OpenCode(state), journal);
    }

    public ProviderTrustCadenceJournalTransitionResult BeginCycle()
    {
        lock (sync)
        {
            if (state.Phase == "terminal") return Current(state.TerminalCode!);
            if (state.Phase == "in-flight") return Current("durable-cadence-indeterminate");
            if (state.Phase != "ready") return Current("durable-cadence-waiting");
            return Transition(next => next.Phase = "in-flight", "durable-cadence-cycle-started");
        }
    }

    public ProviderTrustCadenceJournalTransitionResult CompleteGap(DateTimeOffset nextInstant)
    {
        lock (sync)
        {
            if (state.Phase == "terminal") return Current(state.TerminalCode!);
            if (state.Phase == "in-flight") return Current("durable-cadence-indeterminate");
            if (state.Phase != "waiting") return Current("durable-cadence-gap-invalid");
            if (nextInstant <= state.PreparedInstant) return Current("durable-cadence-gap-invalid");
            return Transition(next =>
            {
                next.GapTicks.Add(next.IntervalTicks);
                next.PreparedInstant = nextInstant;
                next.Phase = "ready";
            }, "durable-cadence-gap-completed");
        }
    }

    public ProviderTrustCadenceJournalTransitionResult CommitCycle(string cycleCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cycleCode);
        lock (sync)
        {
            if (state.Phase == "terminal") return Current(state.TerminalCode!);
            if (state.Phase != "in-flight") return Current("durable-cadence-cycle-not-started");
            if (!IsCycleCode(cycleCode)) return Current("durable-cadence-result-invalid");

            var transitionCode = cycleCode switch
            {
                "provider-trust-cycle-stopped" => "durable-cadence-stopped",
                "provider-trust-cycle-canceled" => "durable-cadence-canceled",
                _ when state.Cycles.Count + 1 == state.MaximumCycles => "durable-cadence-complete",
                _ => "durable-cadence-cycle-committed",
            };
            return Transition(next =>
            {
                next.Cycles.Add(new JournalCycle
                {
                    Index = next.Cycles.Count,
                    Instant = next.PreparedInstant,
                    Code = cycleCode,
                });
                if (transitionCode is "durable-cadence-stopped" or "durable-cadence-canceled"
                    or "durable-cadence-complete")
                {
                    next.Phase = "terminal";
                    next.TerminalCode = transitionCode;
                }
                else
                {
                    next.Phase = "waiting";
                }
            }, transitionCode);
        }
    }

    public ProviderTrustCadenceJournalTransitionResult ResolveInterrupted(
        ProviderTrustCadenceRecoveryDecision decision)
    {
        lock (sync)
        {
            if (state.Phase == "terminal") return Current(state.TerminalCode!);
            if (state.Phase != "in-flight") return Current("durable-cadence-reconciliation-not-required");
            return decision switch
            {
                ProviderTrustCadenceRecoveryDecision.Retry => Transition(next =>
                {
                    next.Phase = "ready";
                    next.InterruptionCount++;
                    next.RetryCount++;
                }, "durable-cadence-retry-ready"),
                ProviderTrustCadenceRecoveryDecision.Abandon => Transition(next =>
                {
                    next.Phase = "terminal";
                    next.TerminalCode = "durable-cadence-abandoned";
                    next.InterruptionCount++;
                }, "durable-cadence-abandoned"),
                _ => throw new ArgumentOutOfRangeException(nameof(decision)),
            };
        }
    }

    private ProviderTrustCadenceJournalTransitionResult Transition(
        Action<JournalState> mutation,
        string acceptedCode)
    {
        var next = state.Clone();
        mutation(next);
        if (!IsValid(next) || !TryWrite(path, next))
            return Current("durable-cadence-write-failed");
        state = next;
        return Current(acceptedCode);
    }

    private ProviderTrustCadenceJournalTransitionResult Current(string code) =>
        new(code, Project(state));

    private static string OpenCode(JournalState value) => value.Phase switch
    {
        "in-flight" => "durable-cadence-indeterminate",
        "terminal" => value.TerminalCode!,
        _ => "durable-cadence-recovered",
    };

    private static ProviderTrustCadenceJournalSnapshot Project(JournalState value) =>
        new(
            ProviderTrustCadenceRunId.Create(value.RunIdentity),
            value.Phase switch
            {
                "in-flight" => "durable-cadence-indeterminate",
                "terminal" => value.TerminalCode!,
                _ => "durable-cadence-active",
            },
            value.Phase,
            value.MaximumCycles,
            TimeSpan.FromTicks(value.IntervalTicks),
            value.PreparedInstant,
            value.Cycles.Select(item =>
                new ProviderTrustCadenceJournalCycle(item.Index, item.Instant, item.Code)).ToArray(),
            value.GapTicks.Select(TimeSpan.FromTicks).ToArray(),
            value.Cycles.Count,
            value.InterruptionCount,
            value.RetryCount);

    private static bool IsCycleCode(string code) => code is
        "provider-trust-cycle-current" or
        "provider-trust-cycle-withdrawn" or
        "provider-trust-cycle-stopped" or
        "provider-trust-cycle-canceled";

    private static bool IsValid(JournalState value)
    {
        if (value.Format != "CBI48" || string.IsNullOrWhiteSpace(value.RunIdentity)
            || value.RunIdentity.Length > 128 || value.RunIdentity != value.RunIdentity.Trim()
            || value.MaximumCycles is < 1 or > 64
            || value.IntervalTicks <= 0 || value.IntervalTicks > TimeSpan.FromHours(24).Ticks
            || value.Phase is not ("ready" or "waiting" or "in-flight" or "terminal")
            || value.Cycles.Count > value.MaximumCycles
            || value.GapTicks.Any(gap => gap != value.IntervalTicks)
            || value.InterruptionCount < 0 || value.RetryCount < 0
            || value.RetryCount > value.InterruptionCount)
            return false;
        for (var index = 0; index < value.Cycles.Count; index++)
        {
            var cycle = value.Cycles[index];
            if (cycle.Index != index || !IsCycleCode(cycle.Code)) return false;
            if (index > 0 && cycle.Instant <= value.Cycles[index - 1].Instant) return false;
        }
        if (value.Phase == "waiting")
            return value.Cycles.Count is > 0 && value.Cycles.Count < value.MaximumCycles
                && value.GapTicks.Count == value.Cycles.Count - 1 && value.TerminalCode is null;
        if (value.Phase is "ready" or "in-flight")
            return value.GapTicks.Count == value.Cycles.Count && value.TerminalCode is null;
        if (value.TerminalCode is not ("durable-cadence-complete" or "durable-cadence-stopped"
            or "durable-cadence-canceled" or "durable-cadence-abandoned")) return false;
        if (value.TerminalCode == "durable-cadence-complete")
            return value.Cycles.Count == value.MaximumCycles
                && value.GapTicks.Count == value.Cycles.Count - 1;
        if (value.TerminalCode is "durable-cadence-stopped" or "durable-cadence-canceled")
            return value.Cycles.Count > 0 && value.GapTicks.Count == value.Cycles.Count - 1;
        return value.GapTicks.Count == value.Cycles.Count
            || value.GapTicks.Count == Math.Max(value.Cycles.Count - 1, 0);
    }

    private static bool TryWrite(string path, JournalState value)
    {
        var temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var record = JsonSerializer.SerializeToUtf8Bytes(value);
            if (record.Length + TagBytes > MaxBytes) return false;
            var bytes = new byte[record.Length + TagBytes];
            record.CopyTo(bytes, 0);
            SHA256.HashData(record).CopyTo(bytes, record.Length);
            using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None,
                       4096, FileOptions.WriteThrough))
            {
                output.Write(bytes);
                output.Flush(true);
            }
            File.Move(temporary, path, true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or NotSupportedException)
        {
            TryDelete(temporary);
            return false;
        }
    }

    private static bool TryRead(string path, out JournalState value)
    {
        value = null!;
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length <= TagBytes || bytes.Length > MaxBytes) return false;
            var record = bytes.AsSpan(0, bytes.Length - TagBytes);
            if (!CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(record), bytes.AsSpan(bytes.Length - TagBytes))) return false;
            value = JsonSerializer.Deserialize<JournalState>(record)!;
            return value is not null && IsValid(value);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or JsonException or ArgumentException or DecoderFallbackException)
        {
            value = null!;
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }

    private sealed class JournalState
    {
        public string Format { get; set; } = "";
        public string RunIdentity { get; set; } = "";
        public int MaximumCycles { get; set; }
        public long IntervalTicks { get; set; }
        public DateTimeOffset PreparedInstant { get; set; }
        public string Phase { get; set; } = "";
        public string? TerminalCode { get; set; }
        public List<JournalCycle> Cycles { get; set; } = [];
        public List<long> GapTicks { get; set; } = [];
        public int InterruptionCount { get; set; }
        public int RetryCount { get; set; }

        public JournalState Clone() => new()
        {
            Format = Format,
            RunIdentity = RunIdentity,
            MaximumCycles = MaximumCycles,
            IntervalTicks = IntervalTicks,
            PreparedInstant = PreparedInstant,
            Phase = Phase,
            TerminalCode = TerminalCode,
            Cycles = Cycles.Select(item => item.Clone()).ToList(),
            GapTicks = [.. GapTicks],
            InterruptionCount = InterruptionCount,
            RetryCount = RetryCount,
        };
    }

    private sealed class JournalCycle
    {
        public int Index { get; set; }
        public DateTimeOffset Instant { get; set; }
        public string Code { get; set; } = "";
        public JournalCycle Clone() => new() { Index = Index, Instant = Instant, Code = Code };
    }
}

public static class ProviderTrustCadenceRecovery
{
    public static async Task<ProviderTrustCadenceJournalTransitionResult> AdvanceAsync(
        DurableProviderTrustCadenceJournal journal,
        IProviderServingTrustCycle cycle,
        IProviderServingTrustCadenceDelay delay,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(cycle);
        ArgumentNullException.ThrowIfNull(delay);

        var snapshot = journal.Snapshot;
        if (snapshot.Phase == "terminal") return new(snapshot.Code, snapshot);
        if (snapshot.Phase == "in-flight")
            return new("durable-cadence-indeterminate", snapshot);
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

        var started = journal.BeginCycle();
        if (started.Code != "durable-cadence-cycle-started") return started;
        var result = await cycle.RunAsync(started.Snapshot.PreparedInstant, cancellationToken)
            .ConfigureAwait(false);
        return journal.CommitCycle(result.Code);
    }
}
