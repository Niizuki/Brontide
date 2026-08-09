namespace Brontide.Minimal.Host

open System
open System.IO
open System.Threading
open System.Threading.Tasks

/// A live operating-system exclusion over one CBI48 cadence run. CBI68 publishes an epoch in the
/// record itself, which makes a holder that has been written past harmless; it cannot stop a second
/// host from reaching the record at all, and a cadence writes only after its cycle has run, so the
/// fence's detection point is behind the effect. This holds a lock beside the journal for the
/// supervision's lifetime so the competitor never opens the run.
///
/// It publishes no state of its own. CBI54 pairs its lock with a durable epoch because CBI53 has
/// none; the cadence journal already carries one, and a second record of a fact the first holds is a
/// thing that can disagree with it.
type ProviderTrustCadenceRunSupervision
    private (held: FileStream, journalPath: string, runIdentity: ProviderTrustCadenceRunId) =
    let gate = new SemaphoreSlim(1, 1)
    let mutable disposed = false

    let isLive () = not disposed && not held.SafeFileHandle.IsClosed

    let isCurrent (journal: DurableProviderTrustCadenceJournal) =
        isLive ()
        && String.Equals(journal.RecordPath, journalPath, StringComparison.Ordinal)
        && journal.Snapshot.RunIdentity = runIdentity

    /// The exclusion path, which is derived so two supervisors cannot pick different ones.
    static member LockPathFor(journalPath: string) =
        if String.IsNullOrWhiteSpace journalPath then
            invalidArg (nameof journalPath) "A journal path is required."
        Path.GetFullPath journalPath + ".lock"

    member _.IsLive = isLive ()

    member _.RunIdentity = runIdentity

    /// Whether this supervision covers the journal it is handed. The lock is over a path, so a
    /// supervision paired with a journal at some other path would gate a run it excludes nobody from.
    member _.IsCurrentFor(journal: DurableProviderTrustCadenceJournal) =
        if isNull (box journal) then nullArg (nameof journal)
        gate.Wait()
        try isCurrent journal
        finally gate.Release() |> ignore

    member internal _.TryEnterAsync(journal: DurableProviderTrustCadenceJournal) = task {
        do! gate.WaitAsync()
        if not (isCurrent journal) then
            gate.Release() |> ignore
            return None
        else
            let mutable released = false
            return Some
                { new IDisposable with
                    member _.Dispose() =
                        if not released then
                            released <- true
                            gate.Release() |> ignore }
    }

    /// Takes the exclusion. Nothing about the journal is read or written, so a run may be supervised
    /// before it is established and CBI68's rule that ownership is claimed by writing is untouched.
    static member Acquire(journalPath: string, runIdentity: ProviderTrustCadenceRunId) =
        if String.IsNullOrWhiteSpace journalPath then
            invalidArg (nameof journalPath) "A journal path is required."
        if isNull (box runIdentity) then
            invalidArg (nameof runIdentity) "A valid cadence run identity is required."
        let fullPath = Path.GetFullPath journalPath
        let lockPath = fullPath + ".lock"
        let prepared =
            try
                match Path.GetDirectoryName fullPath with
                | null -> invalidArg (nameof journalPath) "A journal path must have a parent directory."
                | parent -> Directory.CreateDirectory parent |> ignore
                Ok()
            with :? IOException | :? UnauthorizedAccessException | :? NotSupportedException ->
                Error "cadence-supervision-unavailable"
        let opened =
            match prepared with
            | Error code -> Error code
            | Ok () ->
                try Ok(new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                with
                // Another live supervisor, in this process or another one. It is the only outcome
                // this slice adds that a caller is expected to act on.
                | :? IOException -> Error "cadence-supervision-busy"
                | :? UnauthorizedAccessException | :? NotSupportedException ->
                    Error "cadence-supervision-unavailable"
        match opened with
        | Error code -> { Code = code; Supervision = None }
        | Ok stream ->
            { Code = "cadence-supervision-acquired"
              Supervision = Some(new ProviderTrustCadenceRunSupervision(stream, fullPath, runIdentity)) }

    /// Releases the exclusion. The lock file itself stays: deleting it would race a supervisor that
    /// has already opened it, and an empty file is not state.
    interface IDisposable with
        member _.Dispose() =
            gate.Wait()
            try
                if not disposed then
                    held.Dispose()
                    disposed <- true
            finally gate.Release() |> ignore

and ProviderTrustCadenceSupervisionResult =
    { Code: string
      Supervision: ProviderTrustCadenceRunSupervision option }

/// Advances a cadence only while its run is supervised. The exclusion is held across the whole
/// advance, including the cycle, because the window the fence cannot cover is exactly the one the
/// cycle runs in.
[<RequireQualifiedAccess>]
module SupervisedProviderTrustCadenceRecovery =
    let advance
        (supervision: ProviderTrustCadenceRunSupervision)
        (journal: DurableProviderTrustCadenceJournal)
        (cycle: ProviderServingTrustCycle)
        (delay: ProviderServingTrustCadenceDelay)
        (cancellationToken: CancellationToken) = task {
        if isNull (box supervision) then nullArg (nameof supervision)
        if isNull (box journal) then nullArg (nameof journal)
        let! entered = supervision.TryEnterAsync journal
        match entered with
        | None -> return { Code = "cadence-supervision-required"; Snapshot = journal.Snapshot }
        | Some held ->
            use _ = held
            return! ProviderTrustCadenceRecovery.advance journal cycle delay cancellationToken
    }
