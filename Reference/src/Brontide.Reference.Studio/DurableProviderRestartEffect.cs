using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Brontide.Reference.Experimental.ComponentManagement;

namespace Brontide.Reference.Studio;

public readonly record struct ProviderRestartEffectToken
{
    private ProviderRestartEffectToken(string value) => Value = value;
    public string Value { get; }
    public static ProviderRestartEffectToken Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 128 || value != value.Trim())
            throw new ArgumentException("A restart effect token must contain 1-128 trimmed characters.", nameof(value));
        return new(value);
    }
}

public sealed record ProviderRestartEffectSnapshot(
    ProviderRestartAttemptRunId RunIdentity,
    OccurrenceId Occurrence,
    ProviderArtifactSetId StagedIdentity,
    int AttemptIndex,
    DateTimeOffset AttemptInstant,
    long FencingEpoch,
    ProviderRestartEffectToken Token,
    string ExecutableName,
    string LeasePath,
    string ReceiptPath);

public sealed record ProviderRestartEffectOpenResult(
    string Code,
    DurableProviderRestartEffect? Effect,
    ProviderRestartEffectSnapshot? Snapshot);

public sealed record ProviderRestartEffectReconciliationResult(
    string Code,
    ProviderRestartEffectSnapshot? Effect,
    ProviderRestartAttemptJournalSnapshot Journal,
    long CurrentFencingEpoch,
    bool ProcessTerminated,
    bool LeaseAvailable);

/// <summary>Durable CBI55 identity for one externally observable restart effect.</summary>
public sealed class DurableProviderRestartEffect
{
    private const int MaxBytes = 16 * 1024;
    private const int TagBytes = 32;
    private readonly EffectState state;

    private DurableProviderRestartEffect(string path, EffectState state)
    {
        Path = path;
        this.state = state;
    }

    public string Path { get; }
    public ProviderRestartEffectSnapshot Snapshot => Project(Path, state);

    public IReadOnlyDictionary<string, string> Environment => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["BRONTIDE_RESTART_EFFECT_LEASE"] = Snapshot.LeasePath,
        ["BRONTIDE_RESTART_EFFECT_RECEIPT"] = Snapshot.ReceiptPath,
        ["BRONTIDE_RESTART_EFFECT_TOKEN"] = Snapshot.Token.Value,
        ["BRONTIDE_RESTART_EFFECT_STAGED_IDENTITY"] = Snapshot.StagedIdentity.Value,
    };

    public static ProviderRestartEffectOpenResult Prepare(
        string path,
        ProviderRestartAttemptRunId runIdentity,
        OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity,
        int attemptIndex,
        DateTimeOffset attemptInstant,
        long fencingEpoch,
        ProviderRestartEffectToken token,
        string executableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(executableName);
        Validate(runIdentity, occurrence, stagedIdentity, attemptIndex, fencingEpoch, token, executableName);
        path = System.IO.Path.GetFullPath(path);
        try { Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new("restart-effect-unavailable", null, null);
        }

        EffectState? prior = null;
        if (File.Exists(path) && !TryRead(path, out prior))
            return new("restart-effect-corrupt", null, null);
        if (prior is not null && !SameLineage(prior, runIdentity, occurrence, stagedIdentity))
            return new("restart-effect-lineage-mismatch", null, Project(path, prior));
        if (prior is not null
            && (fencingEpoch < prior.FencingEpoch
                || fencingEpoch == prior.FencingEpoch && attemptIndex <= prior.AttemptIndex))
            return new("restart-effect-not-successor", null, Project(path, prior));

        var next = new EffectState
        {
            Format = "CBI55",
            RunIdentity = runIdentity.Value,
            Occurrence = occurrence.Value,
            StagedIdentity = stagedIdentity.Value,
            AttemptIndex = attemptIndex,
            AttemptInstantUtcTicks = attemptInstant.UtcTicks,
            FencingEpoch = fencingEpoch,
            Token = token.Value,
            ExecutableName = executableName,
        };
        if (!TryWrite(path, next)) return new("restart-effect-write-failed", null, prior is null ? null : Project(path, prior));
        TryDelete(path + ".receipt");
        var effect = new DurableProviderRestartEffect(path, next);
        return new("restart-effect-prepared", effect, effect.Snapshot);
    }

    public static ProviderRestartEffectOpenResult Open(
        string path,
        ProviderRestartAttemptRunId runIdentity,
        OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = System.IO.Path.GetFullPath(path);
        if (!File.Exists(path)) return new("restart-effect-missing", null, null);
        if (!TryRead(path, out var value)) return new("restart-effect-corrupt", null, null);
        if (!SameLineage(value, runIdentity, occurrence, stagedIdentity))
            return new("restart-effect-lineage-mismatch", null, Project(path, value));
        var effect = new DurableProviderRestartEffect(path, value);
        return new("restart-effect-opened", effect, effect.Snapshot);
    }

    private static void Validate(
        ProviderRestartAttemptRunId runIdentity,
        OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity,
        int attemptIndex,
        long fencingEpoch,
        ProviderRestartEffectToken token,
        string executableName)
    {
        if (string.IsNullOrWhiteSpace(runIdentity.Value) || string.IsNullOrWhiteSpace(occurrence.Value)
            || string.IsNullOrWhiteSpace(stagedIdentity.Value) || string.IsNullOrWhiteSpace(token.Value)
            || attemptIndex < 0 || attemptIndex > 7 || fencingEpoch <= 0
            || executableName != System.IO.Path.GetFileName(executableName) || executableName.Length > 260)
            throw new ArgumentException("Valid exact restart effect facts are required.");
    }

    private static bool SameLineage(
        EffectState value,
        ProviderRestartAttemptRunId runIdentity,
        OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity) =>
        value.RunIdentity == runIdentity.Value && value.Occurrence == occurrence.Value
        && value.StagedIdentity == stagedIdentity.Value;

    private static bool IsValid(EffectState value) =>
        value.Format == "CBI55" && !string.IsNullOrWhiteSpace(value.RunIdentity)
        && !string.IsNullOrWhiteSpace(value.Occurrence) && value.StagedIdentity.Length == 64
        && value.AttemptIndex is >= 0 and <= 7 && value.AttemptInstantUtcTicks > 0
        && value.FencingEpoch > 0 && !string.IsNullOrWhiteSpace(value.Token) && value.Token.Length <= 128
        && value.Token == value.Token.Trim() && !string.IsNullOrWhiteSpace(value.ExecutableName)
        && value.ExecutableName == System.IO.Path.GetFileName(value.ExecutableName) && value.ExecutableName.Length <= 260;

    private static ProviderRestartEffectSnapshot Project(string path, EffectState value) => new(
        ProviderRestartAttemptRunId.Create(value.RunIdentity), OccurrenceId.Create(value.Occurrence),
        ProviderArtifactSetId.Create(value.StagedIdentity), value.AttemptIndex,
        new DateTimeOffset(value.AttemptInstantUtcTicks, TimeSpan.Zero), value.FencingEpoch,
        ProviderRestartEffectToken.Create(value.Token), value.ExecutableName,
        path + ".lease", path + ".receipt");

    private static bool TryWrite(string path, EffectState value)
    {
        var temporary = path + ".tmp";
        try
        {
            var record = Encode(value);
            if (!IsValid(value) || record.Length + TagBytes > MaxBytes) return false;
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

    private static bool TryRead(string path, out EffectState value)
    {
        value = null!;
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length <= TagBytes || bytes.Length > MaxBytes) return false;
            var record = bytes.AsSpan(0, bytes.Length - TagBytes);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(record), bytes.AsSpan(bytes.Length - TagBytes))) return false;
            using var document = JsonDocument.Parse(record.ToArray());
            var root = document.RootElement;
            value = new EffectState
            {
                Format = root.GetProperty("format").GetString() ?? "",
                RunIdentity = root.GetProperty("runIdentity").GetString() ?? "",
                Occurrence = root.GetProperty("occurrence").GetString() ?? "",
                StagedIdentity = root.GetProperty("stagedIdentity").GetString() ?? "",
                AttemptIndex = root.GetProperty("attemptIndex").GetInt32(),
                AttemptInstantUtcTicks = root.GetProperty("attemptInstantUtcTicks").GetInt64(),
                FencingEpoch = root.GetProperty("fencingEpoch").GetInt64(),
                Token = root.GetProperty("token").GetString() ?? "",
                ExecutableName = root.GetProperty("executableName").GetString() ?? "",
            };
            return IsValid(value);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException
            or InvalidOperationException or FormatException or ArgumentException or KeyNotFoundException)
        {
            return false;
        }
    }

    private static byte[] Encode(EffectState value)
    {
        using var output = new MemoryStream();
        using var writer = new Utf8JsonWriter(output);
        writer.WriteStartObject();
        writer.WriteString("format", value.Format);
        writer.WriteString("runIdentity", value.RunIdentity);
        writer.WriteString("occurrence", value.Occurrence);
        writer.WriteString("stagedIdentity", value.StagedIdentity);
        writer.WriteNumber("attemptIndex", value.AttemptIndex);
        writer.WriteNumber("attemptInstantUtcTicks", value.AttemptInstantUtcTicks);
        writer.WriteNumber("fencingEpoch", value.FencingEpoch);
        writer.WriteString("token", value.Token);
        writer.WriteString("executableName", value.ExecutableName);
        writer.WriteEndObject();
        writer.Flush();
        return output.ToArray();
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }

    private sealed class EffectState
    {
        public string Format { get; set; } = "";
        public string RunIdentity { get; set; } = "";
        public string Occurrence { get; set; } = "";
        public string StagedIdentity { get; set; } = "";
        public int AttemptIndex { get; set; }
        public long AttemptInstantUtcTicks { get; set; }
        public long FencingEpoch { get; set; }
        public string Token { get; set; } = "";
        public string ExecutableName { get; set; } = "";
    }
}

public static class ExternallyReconciledProviderRestartRecovery
{
    private const int ReceiptMaxBytes = 16 * 1024;
    private const int TagBytes = 32;

    public static async ValueTask<DurableProviderRestartResult> RunAsync(
        DurableProviderRestartOwnership ownership,
        DurableProviderRestartAttemptJournal journal,
        string effectPath,
        ProviderRestartEffectToken nextToken,
        DurableProviderPublisherTrustPolicyRegistry registry,
        ContentAddressedProviderStore store,
        ProviderServingActivation activation,
        ProviderStopAttribution attribution,
        ProviderPublisherTrustPolicyId currentCyclePolicyIdentity,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(journal);
        var initial = journal.Snapshot;
        using var held = await ownership.TryEnterAsync(initial).ConfigureAwait(false);
        if (held is null) return new("restart-ownership-required", initial, null, null);
        if (initial.Phase == "in-flight")
        {
            var reconciled = ReconcileHeld(ownership, journal, effectPath);
            return new(reconciled.Code, reconciled.Journal, null, null);
        }

        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(activation);
        if (!journal.Matches(activation)) return new("durable-restart-lineage-mismatch", initial, null, null);
        if (initial.Phase == "terminal") return new(initial.Code, initial, null, null);
        DateTimeOffset? lastAttempt = initial.Attempts.Count == 0 ? null : initial.Attempts[^1].Instant;
        var decision = journal.Policy.Evaluate(registry, activation, attribution, currentCyclePolicyIdentity,
            now, initial.Attempts.Count, lastAttempt);
        if (!decision.MayRestart) return new(decision.Code, initial, decision, null);

        var provider = activation.Chain.Provider;
        if (provider is null) return new("provider-restart-activation-unavailable", initial, decision, null);
        var prepared = DurableProviderRestartEffect.Prepare(
            effectPath, initial.RunIdentity, initial.Occurrence, initial.StagedIdentity,
            initial.NextAttemptIndex, now, ownership.Snapshot.Epoch, nextToken,
            System.IO.Path.GetFileName(provider.StagedArtifacts.ExecutablePath));
        if (prepared.Effect is null) return new(prepared.Code, initial, decision, null);
        var begun = journal.BeginAttempt(now);
        if (begun.Code != "durable-restart-attempt-started") return new(begun.Code, begun.Snapshot, decision, null);
        var enforcement = await ProviderRestartEnforcement.RunWithEffectEnvironmentAsync(
            journal.Policy, registry, store, activation, attribution, currentCyclePolicyIdentity,
            now, initial.Attempts.Count, lastAttempt, prepared.Effect.Environment).ConfigureAwait(false);
        var committed = journal.CommitAttempt(
            enforcement.Code, enforcement.RefusedBy, enforcement.ProviderStarted,
            enforcement.LifecycleReconstructed, enforcement.Activation is not null);
        return new(committed.Code, committed.Snapshot, enforcement.Decision, enforcement);
    }

    public static async ValueTask<ProviderRestartEffectReconciliationResult> ReconcileAsync(
        DurableProviderRestartOwnership ownership,
        DurableProviderRestartAttemptJournal journal,
        string effectPath)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(journal);
        var initial = journal.Snapshot;
        using var held = await ownership.TryEnterAsync(initial).ConfigureAwait(false);
        if (held is null)
            return Result("restart-ownership-required", null, initial, ownership.Snapshot.Epoch, false, false);
        return ReconcileHeld(ownership, journal, effectPath);
    }

    private static ProviderRestartEffectReconciliationResult ReconcileHeld(
        DurableProviderRestartOwnership ownership,
        DurableProviderRestartAttemptJournal journal,
        string effectPath)
    {
        var initial = journal.Snapshot;
        var currentEpoch = ownership.Snapshot.Epoch;
        if (initial.Phase != "in-flight")
            return Result("restart-effect-reconciliation-not-required", null, initial, currentEpoch, false, false);
        var opened = DurableProviderRestartEffect.Open(
            effectPath, initial.RunIdentity, initial.Occurrence, initial.StagedIdentity);
        if (opened.Effect is null)
            return Result(opened.Code, opened.Snapshot, initial, currentEpoch, false, false);
        var effect = opened.Snapshot!;
        if (effect.AttemptIndex != initial.InFlightIndex || effect.AttemptInstant != initial.InFlightInstant)
            return Result("restart-effect-attempt-mismatch", effect, initial, currentEpoch, false, false);
        if (effect.FencingEpoch >= currentEpoch)
            return Result("restart-effect-successor-fence-required", effect, initial, currentEpoch, false, false);

        if (TryLease(effect.LeasePath))
            return Retry("restart-effect-no-live-provider", effect, journal, currentEpoch, false);
        if (!TryReadReceipt(effect.ReceiptPath, out var receipt))
            return Result("restart-effect-reconciliation-deferred", effect, initial, currentEpoch, false, false);
        if (receipt.Token != effect.Token.Value || receipt.StagedIdentity != effect.StagedIdentity.Value
            || !string.Equals(receipt.ExecutableName, effect.ExecutableName, StringComparison.OrdinalIgnoreCase))
            return Result("restart-effect-receipt-mismatch", effect, initial, currentEpoch, false, false);

        Process? process = null;
        try
        {
            process = Process.GetProcessById(receipt.ProcessId);
            var actualExecutableName = System.IO.Path.GetFileName(process.MainModule?.FileName) ?? "";
            if (process.HasExited || process.StartTime.ToUniversalTime().Ticks != receipt.ProcessStartUtcTicks
                || !string.Equals(actualExecutableName, effect.ExecutableName, StringComparison.OrdinalIgnoreCase))
                return Result("restart-effect-process-mismatch", effect, initial, currentEpoch, false, false);
            process.Kill(entireProcessTree: true);
            if (!process.WaitForExit(5000))
                return Result("restart-effect-termination-failed", effect, initial, currentEpoch, false, false);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            return Result("restart-effect-process-unavailable", effect, initial, currentEpoch, false, false);
        }
        finally { process?.Dispose(); }

        for (var attempt = 0; attempt < 250; attempt++)
        {
            if (TryLease(effect.LeasePath))
                return Retry("restart-effect-provider-terminated", effect, journal, currentEpoch, true);
            Thread.Sleep(20);
        }
        return Result("restart-effect-lease-still-busy", effect, initial, currentEpoch, true, false);
    }

    private static ProviderRestartEffectReconciliationResult Retry(
        string code,
        ProviderRestartEffectSnapshot effect,
        DurableProviderRestartAttemptJournal journal,
        long currentEpoch,
        bool terminated)
    {
        var transitioned = journal.ResolveInterrupted(ProviderRestartAttemptRecoveryDecision.Retry);
        if (transitioned.Code != "durable-restart-retry-ready")
            return Result(transitioned.Code, effect, transitioned.Snapshot, currentEpoch, terminated, true);
        return Result(code, effect, transitioned.Snapshot, currentEpoch, terminated, true);
    }

    private static bool TryLease(string path)
    {
        try
        {
            using var lease = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            return true;
        }
        catch (IOException) { return false; }
        catch (Exception exception) when (exception is UnauthorizedAccessException or NotSupportedException) { return false; }
    }

    private static bool TryReadReceipt(string path, out Receipt receipt)
    {
        receipt = null!;
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length <= TagBytes || bytes.Length > ReceiptMaxBytes) return false;
            var record = bytes.AsSpan(0, bytes.Length - TagBytes);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(record), bytes.AsSpan(bytes.Length - TagBytes))) return false;
            using var document = JsonDocument.Parse(record.ToArray());
            var root = document.RootElement;
            if (root.GetProperty("format").GetString() != "CBI55") return false;
            receipt = new Receipt(
                root.GetProperty("token").GetString() ?? "",
                root.GetProperty("stagedIdentity").GetString() ?? "",
                root.GetProperty("processId").GetInt32(),
                root.GetProperty("processStartUtcTicks").GetInt64(),
                root.GetProperty("executableName").GetString() ?? "");
            return receipt.ProcessId > 0 && receipt.ProcessStartUtcTicks > 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException
            or InvalidOperationException or FormatException or KeyNotFoundException) { return false; }
    }

    private static ProviderRestartEffectReconciliationResult Result(
        string code,
        ProviderRestartEffectSnapshot? effect,
        ProviderRestartAttemptJournalSnapshot journal,
        long currentEpoch,
        bool terminated,
        bool leaseAvailable) =>
        new(code, effect, journal, currentEpoch, terminated, leaseAvailable);

    private sealed record Receipt(
        string Token,
        string StagedIdentity,
        int ProcessId,
        long ProcessStartUtcTicks,
        string ExecutableName);
}
