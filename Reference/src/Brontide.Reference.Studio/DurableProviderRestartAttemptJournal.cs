using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio;

public readonly record struct ProviderRestartAttemptRunId
{
    private ProviderRestartAttemptRunId(string value) => Value = value;
    public string Value { get; }

    public static ProviderRestartAttemptRunId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 128 || value != value.Trim())
            throw new ArgumentException("A restart run identity must contain 1-128 trimmed characters.", nameof(value));
        return new(value);
    }
}

public enum ProviderRestartAttemptRecoveryDecision { Retry, Abandon }

public sealed record ProviderRestartAttemptObservation(
    int Index,
    DateTimeOffset Instant,
    string Code,
    string RefusedBy,
    bool ProviderStarted,
    bool LifecycleReconstructed,
    bool Completed);

public sealed record ProviderRestartAttemptJournalSnapshot(
    ProviderRestartAttemptRunId RunIdentity,
    Brontide.Reference.Experimental.ComponentManagement.OccurrenceId Occurrence,
    ProviderArtifactSetId StagedIdentity,
    string Code,
    string Phase,
    int MaximumAttempts,
    TimeSpan Delay,
    IReadOnlyList<ProviderRestartAttemptObservation> Attempts,
    int NextAttemptIndex,
    int? InFlightIndex,
    DateTimeOffset? InFlightInstant,
    int InterruptionCount,
    int RetryCount);

public sealed record ProviderRestartAttemptJournalOpenResult(
    string Code,
    DurableProviderRestartAttemptJournal? Journal);

public sealed record ProviderRestartAttemptJournalTransitionResult(
    string Code,
    ProviderRestartAttemptJournalSnapshot Snapshot);

/// <summary>
/// Host-local durable CBI53 attempt history. The integrity tag detects accidental damage; ownership
/// and hostile-writer custody are separate boundaries.
/// </summary>
public sealed class DurableProviderRestartAttemptJournal
{
    private const int MaxBytes = 64 * 1024;
    private const int TagBytes = 32;
    private readonly object sync = new();
    private readonly string path;
    private JournalState state;

    private DurableProviderRestartAttemptJournal(string path, JournalState state)
    {
        this.path = path;
        this.state = state;
    }

    public ProviderRestartAttemptJournalSnapshot Snapshot
    {
        get { lock (sync) return Project(state); }
    }

    public static ProviderRestartAttemptJournalOpenResult Establish(
        string path,
        ProviderRestartAttemptRunId runIdentity,
        Brontide.Reference.Experimental.ComponentManagement.OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity,
        ProviderRestartPolicy policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(policy);
        ValidateIdentities(runIdentity, occurrence, stagedIdentity);
        path = Path.GetFullPath(path);
        TryDelete(path + ".tmp");
        if (File.Exists(path)) return new("durable-restart-already-exists", null);
        var value = new JournalState
        {
            Format = "CBI53",
            RunIdentity = runIdentity.Value,
            Occurrence = occurrence.Value,
            StagedIdentity = stagedIdentity.Value,
            MaximumAttempts = policy.MaximumAttempts,
            DelayTicks = policy.Delay.Ticks,
            Phase = "ready",
        };
        if (!TryWrite(path, value)) return new("durable-restart-write-failed", null);
        return new("durable-restart-established", new(path, value));
    }

    public static ProviderRestartAttemptJournalOpenResult Open(
        string path,
        ProviderRestartAttemptRunId runIdentity,
        Brontide.Reference.Experimental.ComponentManagement.OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ValidateIdentities(runIdentity, occurrence, stagedIdentity);
        path = Path.GetFullPath(path);
        TryDelete(path + ".tmp");
        if (!File.Exists(path)) return new("durable-restart-missing", null);
        if (!TryRead(path, out var value)) return new("durable-restart-corrupt", null);
        if (value.RunIdentity != runIdentity.Value
            || value.Occurrence != occurrence.Value
            || value.StagedIdentity != stagedIdentity.Value)
            return new("durable-restart-lineage-mismatch", null);
        var journal = new DurableProviderRestartAttemptJournal(path, value);
        return new(OpenCode(value), journal);
    }

    public ProviderRestartAttemptJournalTransitionResult BeginAttempt(DateTimeOffset instant)
    {
        lock (sync)
        {
            if (state.Phase == "terminal") return Current(state.TerminalCode!);
            if (state.Phase == "in-flight") return Current("durable-restart-indeterminate");
            if (state.Attempts.Count > 0
                && instant < state.Attempts[^1].Instant.AddTicks(state.DelayTicks))
                return Current("durable-restart-waiting");
            return Transition(next =>
            {
                next.Phase = "in-flight";
                next.InFlightIndex = next.Attempts.Count;
                next.InFlightInstant = instant;
            }, "durable-restart-attempt-started");
        }
    }

    public ProviderRestartAttemptJournalTransitionResult CommitAttempt(
        string code,
        string refusedBy,
        bool providerStarted,
        bool lifecycleReconstructed,
        bool completed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(refusedBy);
        lock (sync)
        {
            if (state.Phase == "terminal") return Current(state.TerminalCode!);
            if (state.Phase != "in-flight") return Current("durable-restart-attempt-not-started");
            var terminal = completed
                ? "durable-restart-completed"
                : state.Attempts.Count + 1 == state.MaximumAttempts
                    ? "durable-restart-exhausted"
                    : null;
            return Transition(next =>
            {
                next.Attempts.Add(new AttemptState
                {
                    Index = next.InFlightIndex!.Value,
                    Instant = next.InFlightInstant!.Value,
                    Code = code,
                    RefusedBy = refusedBy,
                    ProviderStarted = providerStarted,
                    LifecycleReconstructed = lifecycleReconstructed,
                    Completed = completed,
                });
                next.InFlightIndex = null;
                next.InFlightInstant = null;
                next.Phase = terminal is null ? "waiting" : "terminal";
                next.TerminalCode = terminal;
            }, terminal ?? "durable-restart-attempt-committed");
        }
    }

    public ProviderRestartAttemptJournalTransitionResult ResolveInterrupted(
        ProviderRestartAttemptRecoveryDecision decision)
    {
        lock (sync)
        {
            if (state.Phase == "terminal") return Current(state.TerminalCode!);
            if (state.Phase != "in-flight") return Current("durable-restart-reconciliation-not-required");
            return decision switch
            {
                ProviderRestartAttemptRecoveryDecision.Retry => Transition(next =>
                {
                    next.Phase = "ready";
                    next.InFlightIndex = null;
                    next.InFlightInstant = null;
                    next.InterruptionCount++;
                    next.RetryCount++;
                }, "durable-restart-retry-ready"),
                ProviderRestartAttemptRecoveryDecision.Abandon => Transition(next =>
                {
                    next.Phase = "terminal";
                    next.TerminalCode = "durable-restart-abandoned";
                    next.InFlightIndex = null;
                    next.InFlightInstant = null;
                    next.InterruptionCount++;
                }, "durable-restart-abandoned"),
                _ => throw new ArgumentOutOfRangeException(nameof(decision)),
            };
        }
    }

    internal bool Matches(ProviderServingActivation activation) =>
        state.Occurrence == activation.Occurrence.Value
        && state.StagedIdentity == activation.Chain.StagedIdentity?.Value;

    internal ProviderRestartPolicy Policy =>
        ProviderRestartPolicy.Create(state.MaximumAttempts, TimeSpan.FromTicks(state.DelayTicks));

    private ProviderRestartAttemptJournalTransitionResult Transition(Action<JournalState> mutate, string code)
    {
        var next = state.Clone();
        mutate(next);
        if (!IsValid(next) || !TryWrite(path, next)) return Current("durable-restart-write-failed");
        state = next;
        return Current(code);
    }

    private ProviderRestartAttemptJournalTransitionResult Current(string code) => new(code, Project(state));

    private static string OpenCode(JournalState value) => value.Phase switch
    {
        "in-flight" => "durable-restart-indeterminate",
        "terminal" => value.TerminalCode!,
        _ => "durable-restart-recovered",
    };

    private static ProviderRestartAttemptJournalSnapshot Project(JournalState value) => new(
        ProviderRestartAttemptRunId.Create(value.RunIdentity),
        Brontide.Reference.Experimental.ComponentManagement.OccurrenceId.Create(value.Occurrence),
        ProviderArtifactSetId.Create(value.StagedIdentity),
        value.Phase == "in-flight" ? "durable-restart-indeterminate"
            : value.Phase == "terminal" ? value.TerminalCode! : "durable-restart-active",
        value.Phase,
        value.MaximumAttempts,
        TimeSpan.FromTicks(value.DelayTicks),
        value.Attempts.Select(item => new ProviderRestartAttemptObservation(
            item.Index, item.Instant, item.Code, item.RefusedBy, item.ProviderStarted,
            item.LifecycleReconstructed, item.Completed)).ToArray(),
        value.Attempts.Count,
        value.InFlightIndex,
        value.InFlightInstant,
        value.InterruptionCount,
        value.RetryCount);

    private static void ValidateIdentities(
        ProviderRestartAttemptRunId runIdentity,
        Brontide.Reference.Experimental.ComponentManagement.OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity)
    {
        if (string.IsNullOrWhiteSpace(runIdentity.Value)) throw new ArgumentException("A valid restart run identity is required.", nameof(runIdentity));
        if (string.IsNullOrWhiteSpace(occurrence.Value)) throw new ArgumentException("A valid occurrence is required.", nameof(occurrence));
        if (string.IsNullOrWhiteSpace(stagedIdentity.Value)) throw new ArgumentException("A valid staged identity is required.", nameof(stagedIdentity));
    }

    private static bool IsValid(JournalState value)
    {
        if (value.Format != "CBI53" || string.IsNullOrWhiteSpace(value.RunIdentity)
            || value.RunIdentity.Length > 128 || value.RunIdentity != value.RunIdentity.Trim()
            || string.IsNullOrWhiteSpace(value.Occurrence)
            || value.StagedIdentity.Length != 64
            || value.MaximumAttempts is < 1 or > 8
            || value.DelayTicks <= 0 || value.DelayTicks > TimeSpan.FromHours(1).Ticks
            || value.Phase is not ("ready" or "waiting" or "in-flight" or "terminal")
            || value.Attempts.Count > value.MaximumAttempts
            || value.InterruptionCount < 0 || value.RetryCount < 0 || value.RetryCount > value.InterruptionCount)
            return false;
        for (var index = 0; index < value.Attempts.Count; index++)
        {
            var attempt = value.Attempts[index];
            if (attempt.Index != index || string.IsNullOrWhiteSpace(attempt.Code)
                || string.IsNullOrWhiteSpace(attempt.RefusedBy)
                || attempt.LifecycleReconstructed && !attempt.ProviderStarted
                || attempt.Completed && !attempt.LifecycleReconstructed
                || index > 0 && attempt.Instant < value.Attempts[index - 1].Instant.AddTicks(value.DelayTicks))
                return false;
        }
        if (value.Phase == "in-flight")
            return value.TerminalCode is null && value.InFlightIndex == value.Attempts.Count
                && value.InFlightInstant is not null && value.Attempts.Count < value.MaximumAttempts;
        if (value.InFlightIndex is not null || value.InFlightInstant is not null) return false;
        if (value.Phase == "terminal")
            return value.TerminalCode switch
            {
                "durable-restart-completed" => value.Attempts.Count > 0 && value.Attempts[^1].Completed,
                "durable-restart-exhausted" => value.Attempts.Count == value.MaximumAttempts && value.Attempts.All(item => !item.Completed),
                "durable-restart-abandoned" => true,
                _ => false,
            };
        return value.TerminalCode is null
            && (value.Phase == "ready" || value.Attempts.Count > 0 && value.Attempts.Count < value.MaximumAttempts);
    }

    private static bool TryWrite(string path, JournalState value)
    {
        var temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var record = JsonSerializer.SerializeToUtf8Bytes(value);
            if (record.Length + TagBytes > MaxBytes) return false;
            using var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
            output.Write(record);
            output.Write(SHA256.HashData(record));
            output.Flush(flushToDisk: true);
            output.Dispose();
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
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
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(record), bytes.AsSpan(bytes.Length - TagBytes))) return false;
            value = JsonSerializer.Deserialize<JournalState>(record)!;
            return value is not null && IsValid(value);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return false;
        }
    }

    private static void TryDelete(string value)
    {
        try { if (File.Exists(value)) File.Delete(value); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }

    public sealed class JournalState
    {
        public string Format { get; set; } = "";
        public string RunIdentity { get; set; } = "";
        public string Occurrence { get; set; } = "";
        public string StagedIdentity { get; set; } = "";
        public int MaximumAttempts { get; set; }
        public long DelayTicks { get; set; }
        public string Phase { get; set; } = "";
        public string? TerminalCode { get; set; }
        public List<AttemptState> Attempts { get; set; } = [];
        public int? InFlightIndex { get; set; }
        public DateTimeOffset? InFlightInstant { get; set; }
        public int InterruptionCount { get; set; }
        public int RetryCount { get; set; }

        public JournalState Clone() => new()
        {
            Format = Format, RunIdentity = RunIdentity, Occurrence = Occurrence,
            StagedIdentity = StagedIdentity, MaximumAttempts = MaximumAttempts,
            DelayTicks = DelayTicks, Phase = Phase, TerminalCode = TerminalCode,
            Attempts = Attempts.Select(item => item.Clone()).ToList(),
            InFlightIndex = InFlightIndex, InFlightInstant = InFlightInstant,
            InterruptionCount = InterruptionCount, RetryCount = RetryCount,
        };
    }

    public sealed class AttemptState
    {
        public int Index { get; set; }
        public DateTimeOffset Instant { get; set; }
        public string Code { get; set; } = "";
        public string RefusedBy { get; set; } = "";
        public bool ProviderStarted { get; set; }
        public bool LifecycleReconstructed { get; set; }
        public bool Completed { get; set; }
        public AttemptState Clone() => (AttemptState)MemberwiseClone();
    }
}

public sealed record DurableProviderRestartResult(
    string Code,
    ProviderRestartAttemptJournalSnapshot Snapshot,
    ProviderRestartDecision? Decision,
    ProviderRestartEnforcementResult? Enforcement);

public static class DurableProviderRestartRecovery
{
    public static async ValueTask<DurableProviderRestartResult> RunAsync(
        DurableProviderRestartAttemptJournal journal,
        DurableProviderPublisherTrustPolicyRegistry registry,
        ContentAddressedProviderStore store,
        ProviderServingActivation activation,
        ProviderRestartCause cause,
        ProviderPublisherTrustPolicyId currentCyclePolicyIdentity,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(activation);
        var snapshot = journal.Snapshot;
        if (!journal.Matches(activation)) return new("durable-restart-lineage-mismatch", snapshot, null, null);
        if (snapshot.Phase == "terminal") return new(snapshot.Code, snapshot, null, null);
        if (snapshot.Phase == "in-flight") return new("durable-restart-indeterminate", snapshot, null, null);
        DateTimeOffset? lastAttempt = snapshot.Attempts.Count == 0 ? null : snapshot.Attempts[^1].Instant;
        var decision = journal.Policy.Evaluate(
            registry, activation, cause, currentCyclePolicyIdentity, now,
            snapshot.Attempts.Count, lastAttempt);
        if (!decision.MayRestart) return new(decision.Code, snapshot, decision, null);
        var begun = journal.BeginAttempt(now);
        if (begun.Code != "durable-restart-attempt-started") return new(begun.Code, begun.Snapshot, decision, null);
        var enforcement = await ProviderRestartEnforcement.RunAsync(
            journal.Policy, registry, store, activation, cause, currentCyclePolicyIdentity,
            now, snapshot.Attempts.Count, lastAttempt).ConfigureAwait(false);
        var committed = journal.CommitAttempt(
            enforcement.Code, enforcement.RefusedBy, enforcement.ProviderStarted,
            enforcement.LifecycleReconstructed, enforcement.Activation is not null);
        return new(committed.Code, committed.Snapshot, enforcement.Decision, enforcement);
    }
}
