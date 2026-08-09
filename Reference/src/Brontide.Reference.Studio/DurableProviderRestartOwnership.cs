using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio;

public readonly record struct ProviderRestartOwnerId
{
    private ProviderRestartOwnerId(string value) => Value = value;
    public string Value { get; }
    public static ProviderRestartOwnerId Create(string value) => new(Validate(value, "owner"));
    private static string Validate(string value, string kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 128 || value != value.Trim())
            throw new ArgumentException($"A restart {kind} identity must contain 1-128 trimmed characters.", nameof(value));
        return value;
    }
}

public readonly record struct ProviderRestartOwnershipLeaseId
{
    private ProviderRestartOwnershipLeaseId(string value) => Value = value;
    public string Value { get; }
    public static ProviderRestartOwnershipLeaseId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 128 || value != value.Trim())
            throw new ArgumentException("A restart lease identity must contain 1-128 trimmed characters.", nameof(value));
        return new(value);
    }
}

public sealed record ProviderRestartOwnershipSnapshot(
    string Code,
    long Epoch,
    ProviderRestartOwnerId Owner,
    ProviderRestartOwnershipLeaseId Lease,
    ProviderRestartAttemptRunId RunIdentity,
    Brontide.Reference.Experimental.ComponentManagement.OccurrenceId Occurrence,
    ProviderArtifactSetId StagedIdentity,
    bool IsLive);

public sealed record ProviderRestartOwnershipAcquireResult(
    string Code,
    DurableProviderRestartOwnership? Ownership,
    ProviderRestartOwnershipSnapshot? Snapshot);

public sealed record ProviderRestartOwnershipInspection(
    string Code,
    ProviderRestartOwnershipSnapshot? Snapshot);

/// <summary>One host-local cross-process CBI54 lock with a durable fencing epoch.</summary>
public sealed class DurableProviderRestartOwnership : IDisposable
{
    private const int MaxBytes = 16 * 1024;
    private const int TagBytes = 32;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly FileStream lockStream;
    private readonly string statePath;
    private readonly OwnershipState state;
    private bool disposed;

    private DurableProviderRestartOwnership(FileStream lockStream, string statePath, OwnershipState state)
    {
        this.lockStream = lockStream;
        this.statePath = statePath;
        this.state = state;
    }

    public ProviderRestartOwnershipSnapshot Snapshot => Project(
        state, !disposed && !lockStream.SafeFileHandle.IsClosed, "restart-ownership-current");

    public static ProviderRestartOwnershipAcquireResult Acquire(
        string path,
        ProviderRestartOwnerId owner,
        ProviderRestartOwnershipLeaseId lease,
        ProviderRestartAttemptRunId runIdentity,
        Brontide.Reference.Experimental.ComponentManagement.OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Validate(owner, lease, runIdentity, occurrence, stagedIdentity);
        var lockPath = Path.GetFullPath(path);
        var statePath = lockPath + ".state";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new("restart-ownership-unavailable", null, null);
        }

        FileStream held;
        try
        {
            held = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        }
        catch (IOException)
        {
            return new("restart-ownership-busy", null, null);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or NotSupportedException)
        {
            return new("restart-ownership-unavailable", null, null);
        }

        OwnershipState? prior = null;
        if (File.Exists(statePath) && !TryRead(statePath, out prior))
        {
            held.Dispose();
            return new("restart-ownership-corrupt", null, null);
        }
        if (prior is not null && !Matches(prior, runIdentity, occurrence, stagedIdentity))
        {
            held.Dispose();
            return new("restart-ownership-lineage-mismatch", null, Project(prior, false, "restart-ownership-observed"));
        }
        if (prior?.Epoch == long.MaxValue)
        {
            held.Dispose();
            return new("restart-ownership-epoch-exhausted", null, Project(prior, false, "restart-ownership-observed"));
        }

        var next = new OwnershipState
        {
            Format = "CBI54",
            Epoch = (prior?.Epoch ?? 0) + 1,
            Owner = owner.Value,
            Lease = lease.Value,
            RunIdentity = runIdentity.Value,
            Occurrence = occurrence.Value,
            StagedIdentity = stagedIdentity.Value,
        };
        if (!TryWrite(statePath, next))
        {
            held.Dispose();
            return new("restart-ownership-write-failed", null, prior is null ? null : Project(prior, false, "restart-ownership-observed"));
        }
        var ownership = new DurableProviderRestartOwnership(held, statePath, next);
        return new("restart-ownership-acquired", ownership, ownership.Snapshot);
    }

    public static ProviderRestartOwnershipInspection Inspect(
        string path,
        ProviderRestartAttemptRunId runIdentity,
        Brontide.Reference.Experimental.ComponentManagement.OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var statePath = Path.GetFullPath(path) + ".state";
        if (!File.Exists(statePath)) return new("restart-ownership-missing", null);
        if (!TryRead(statePath, out var value)) return new("restart-ownership-corrupt", null);
        if (!Matches(value, runIdentity, occurrence, stagedIdentity))
            return new("restart-ownership-lineage-mismatch", Project(value, false, "restart-ownership-observed"));
        return new("restart-ownership-observed", Project(value, false, "restart-ownership-observed"));
    }

    public bool IsCurrentFor(ProviderRestartAttemptJournalSnapshot journal)
    {
        ArgumentNullException.ThrowIfNull(journal);
        gate.Wait();
        try { return IsCurrent(journal); }
        finally { gate.Release(); }
    }

    internal async ValueTask<IDisposable?> TryEnterAsync(ProviderRestartAttemptJournalSnapshot journal)
    {
        await gate.WaitAsync().ConfigureAwait(false);
        if (!IsCurrent(journal))
        {
            gate.Release();
            return null;
        }
        return new GateRelease(gate);
    }

    private bool IsCurrent(ProviderRestartAttemptJournalSnapshot journal)
    {
        if (disposed || lockStream.SafeFileHandle.IsClosed
            || state.RunIdentity != journal.RunIdentity.Value
            || state.Occurrence != journal.Occurrence.Value
            || state.StagedIdentity != journal.StagedIdentity.Value)
            return false;
        return TryRead(statePath, out var current)
            && current.Epoch == state.Epoch
            && current.Owner == state.Owner
            && current.Lease == state.Lease
            && Matches(current, journal.RunIdentity, journal.Occurrence, journal.StagedIdentity);
    }

    public void Dispose()
    {
        gate.Wait();
        try
        {
            if (disposed) return;
            lockStream.Dispose();
            disposed = true;
        }
        finally { gate.Release(); }
    }

    private static void Validate(
        ProviderRestartOwnerId owner,
        ProviderRestartOwnershipLeaseId lease,
        ProviderRestartAttemptRunId runIdentity,
        Brontide.Reference.Experimental.ComponentManagement.OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity)
    {
        if (string.IsNullOrWhiteSpace(owner.Value) || string.IsNullOrWhiteSpace(lease.Value)
            || string.IsNullOrWhiteSpace(runIdentity.Value) || string.IsNullOrWhiteSpace(occurrence.Value)
            || string.IsNullOrWhiteSpace(stagedIdentity.Value))
            throw new ArgumentException("Valid restart ownership identities are required.");
    }

    private static bool Matches(
        OwnershipState value,
        ProviderRestartAttemptRunId runIdentity,
        Brontide.Reference.Experimental.ComponentManagement.OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity) =>
        value.RunIdentity == runIdentity.Value && value.Occurrence == occurrence.Value
        && value.StagedIdentity == stagedIdentity.Value;

    private static bool IsValid(OwnershipState value) =>
        value.Format == "CBI54" && value.Epoch > 0
        && ValidText(value.Owner) && ValidText(value.Lease) && ValidText(value.RunIdentity)
        && !string.IsNullOrWhiteSpace(value.Occurrence) && value.StagedIdentity.Length == 64;

    private static bool ValidText(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && value == value.Trim();

    private static ProviderRestartOwnershipSnapshot Project(OwnershipState value, bool live, string code) => new(
        code, value.Epoch, ProviderRestartOwnerId.Create(value.Owner),
        ProviderRestartOwnershipLeaseId.Create(value.Lease), ProviderRestartAttemptRunId.Create(value.RunIdentity),
        Brontide.Reference.Experimental.ComponentManagement.OccurrenceId.Create(value.Occurrence),
        ProviderArtifactSetId.Create(value.StagedIdentity), live);

    private static bool TryWrite(string path, OwnershipState value)
    {
        var temporary = path + ".tmp";
        try
        {
            var record = JsonSerializer.SerializeToUtf8Bytes(value);
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
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException) { }
            return false;
        }
    }

    private static bool TryRead(string path, out OwnershipState value)
    {
        value = null!;
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length <= TagBytes || bytes.Length > MaxBytes) return false;
            var record = bytes.AsSpan(0, bytes.Length - TagBytes);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(record), bytes.AsSpan(bytes.Length - TagBytes))) return false;
            value = JsonSerializer.Deserialize<OwnershipState>(record)!;
            return value is not null && IsValid(value);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException
            or NotSupportedException or InvalidOperationException or FormatException or ArgumentException)
        {
            return false;
        }
    }

    private sealed class GateRelease(SemaphoreSlim gate) : IDisposable
    {
        private bool released;
        public void Dispose()
        {
            if (released) return;
            released = true;
            gate.Release();
        }
    }

    private sealed class OwnershipState
    {
        public string Format { get; set; } = "";
        public long Epoch { get; set; }
        public string Owner { get; set; } = "";
        public string Lease { get; set; } = "";
        public string RunIdentity { get; set; } = "";
        public string Occurrence { get; set; } = "";
        public string StagedIdentity { get; set; } = "";
    }
}

public static class CrossProcessProviderRestartRecovery
{
    public static async ValueTask<DurableProviderRestartResult> RunAsync(
        DurableProviderRestartOwnership ownership,
        DurableProviderRestartAttemptJournal journal,
        DurableProviderPublisherTrustPolicyRegistry registry,
        ContentAddressedProviderStore store,
        ProviderServingActivation activation,
        ProviderStopAttribution attribution,
        ProviderPublisherTrustPolicyId currentCyclePolicyIdentity,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(journal);
        var snapshot = journal.Snapshot;
        using var held = await ownership.TryEnterAsync(snapshot).ConfigureAwait(false);
        if (held is null)
            return new("restart-ownership-required", snapshot, null, null);
        return await DurableProviderRestartRecovery.RunAsync(
            journal, registry, store, activation, attribution, currentCyclePolicyIdentity, now).ConfigureAwait(false);
    }
}
