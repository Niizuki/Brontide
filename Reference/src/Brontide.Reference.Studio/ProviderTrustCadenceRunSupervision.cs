namespace Brontide.Reference.Studio;

public sealed record ProviderTrustCadenceSupervisionResult(
    string Code,
    ProviderTrustCadenceRunSupervision? Supervision);

/// <summary>
/// A live operating-system exclusion over one CBI48 cadence run. CBI68 publishes an epoch in the
/// record itself, which makes a holder that has been written past harmless; it cannot stop a second
/// host from reaching the record at all, and a cadence writes only after its cycle has run, so the
/// fence's detection point is behind the effect. This holds a lock beside the journal for the
/// supervision's lifetime so the competitor never opens the run.
///
/// It publishes no state of its own. CBI54 pairs its lock with a durable epoch because CBI53 has
/// none; the cadence journal already carries one, and a second record of a fact the first holds is a
/// thing that can disagree with it.
/// </summary>
public sealed class ProviderTrustCadenceRunSupervision : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly FileStream held;
    private readonly string journalPath;
    private readonly ProviderTrustCadenceRunId runIdentity;
    private bool disposed;

    private ProviderTrustCadenceRunSupervision(
        FileStream held,
        string journalPath,
        ProviderTrustCadenceRunId runIdentity)
    {
        this.held = held;
        this.journalPath = journalPath;
        this.runIdentity = runIdentity;
    }

    /// <summary>The exclusion path, which is derived so two supervisors cannot pick different ones.</summary>
    public static string LockPathFor(string journalPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalPath);
        return Path.GetFullPath(journalPath) + ".lock";
    }

    public bool IsLive => !disposed && !held.SafeFileHandle.IsClosed;

    public ProviderTrustCadenceRunId RunIdentity => runIdentity;

    /// <summary>
    /// Takes the exclusion. Nothing about the journal is read or written, so a run may be supervised
    /// before it is established and CBI68's rule that ownership is claimed by writing is untouched.
    /// </summary>
    public static ProviderTrustCadenceSupervisionResult Acquire(
        string journalPath,
        ProviderTrustCadenceRunId runIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalPath);
        if (string.IsNullOrWhiteSpace(runIdentity.Value))
            throw new ArgumentException("A valid cadence run identity is required.", nameof(runIdentity));
        var fullPath = Path.GetFullPath(journalPath);
        var lockPath = fullPath + ".lock";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or NotSupportedException)
        {
            return new("cadence-supervision-unavailable", null);
        }

        FileStream stream;
        try
        {
            stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        }
        catch (IOException)
        {
            // Another live supervisor, in this process or another one. It is the only outcome this
            // slice adds that a caller is expected to act on.
            return new("cadence-supervision-busy", null);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or NotSupportedException)
        {
            return new("cadence-supervision-unavailable", null);
        }

        return new("cadence-supervision-acquired",
            new ProviderTrustCadenceRunSupervision(stream, fullPath, runIdentity));
    }

    /// <summary>
    /// Whether this supervision covers the journal it is handed. The lock is over a path, so a
    /// supervision paired with a journal at some other path would gate a run it excludes nobody from.
    /// </summary>
    public bool IsCurrentFor(DurableProviderTrustCadenceJournal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);
        gate.Wait();
        try { return IsCurrent(journal); }
        finally { gate.Release(); }
    }

    internal async ValueTask<IDisposable?> TryEnterAsync(DurableProviderTrustCadenceJournal journal)
    {
        await gate.WaitAsync().ConfigureAwait(false);
        if (!IsCurrent(journal))
        {
            gate.Release();
            return null;
        }
        return new GateRelease(gate);
    }

    private bool IsCurrent(DurableProviderTrustCadenceJournal journal) =>
        IsLive
        && StringComparer.Ordinal.Equals(journal.RecordPath, journalPath)
        && journal.Snapshot.RunIdentity == runIdentity;

    /// <summary>
    /// Releases the exclusion. The lock file itself stays: deleting it would race a supervisor that
    /// has already opened it, and an empty file is not state.
    /// </summary>
    public void Dispose()
    {
        gate.Wait();
        try
        {
            if (disposed) return;
            held.Dispose();
            disposed = true;
        }
        finally { gate.Release(); }
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
}

/// <summary>
/// Advances a cadence only while its run is supervised. The exclusion is held across the whole
/// advance, including the cycle, because the window the fence cannot cover is exactly the one the
/// cycle runs in.
/// </summary>
public static class SupervisedProviderTrustCadenceRecovery
{
    public static async Task<ProviderTrustCadenceJournalTransitionResult> AdvanceAsync(
        ProviderTrustCadenceRunSupervision supervision,
        DurableProviderTrustCadenceJournal journal,
        IProviderServingTrustCycle cycle,
        IProviderServingTrustCadenceDelay delay,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(supervision);
        ArgumentNullException.ThrowIfNull(journal);
        using var entered = await supervision.TryEnterAsync(journal).ConfigureAwait(false);
        if (entered is null)
            return new("cadence-supervision-required", journal.Snapshot);
        return await ProviderTrustCadenceRecovery.AdvanceAsync(journal, cycle, delay, cancellationToken)
            .ConfigureAwait(false);
    }
}
