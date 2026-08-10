namespace Brontide.Minimal.Host

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Threading
open Brontide.Minimal.Experimental.ComponentManagement

type ProviderRestartOwnerId =
    private | ProviderRestartOwnerId of string
    member this.Value = let (ProviderRestartOwnerId value) = this in value

[<RequireQualifiedAccess>]
module ProviderRestartOwnerId =
    let create (value: string) =
        if String.IsNullOrWhiteSpace value || value.Length > 128 || value <> value.Trim() then
            invalidArg (nameof value) "A restart owner identity must contain 1-128 trimmed characters."
        ProviderRestartOwnerId value

type ProviderRestartOwnershipLeaseId =
    private | ProviderRestartOwnershipLeaseId of string
    member this.Value = let (ProviderRestartOwnershipLeaseId value) = this in value

[<RequireQualifiedAccess>]
module ProviderRestartOwnershipLeaseId =
    let create (value: string) =
        if String.IsNullOrWhiteSpace value || value.Length > 128 || value <> value.Trim() then
            invalidArg (nameof value) "A restart lease identity must contain 1-128 trimmed characters."
        ProviderRestartOwnershipLeaseId value

type ProviderRestartOwnershipSnapshot =
    { Code: string
      Epoch: int64
      Owner: ProviderRestartOwnerId
      Lease: ProviderRestartOwnershipLeaseId
      RunIdentity: ProviderRestartAttemptRunId
      Occurrence: OccurrenceId
      StagedIdentity: ProviderArtifactSetId
      IsLive: bool }

type ProviderRestartOwnershipInspection =
    { Code: string
      Snapshot: ProviderRestartOwnershipSnapshot option }

type private RestartOwnershipState =
    { Format: string
      Epoch: int64
      Owner: string
      Lease: string
      RunIdentity: string
      Occurrence: string
      StagedIdentity: string }

[<RequireQualifiedAccess>]
module private ProviderRestartOwnershipRecord =
    [<Literal>]
    let MaxBytes = 16384
    [<Literal>]
    let TagBytes = 32

    let validText (value: string) =
        not (String.IsNullOrWhiteSpace value) && value.Length <= 128 && value = value.Trim()

    let isValid value =
        value.Format = "CBI54" && value.Epoch > 0L
        && validText value.Owner && validText value.Lease && validText value.RunIdentity
        && not (String.IsNullOrWhiteSpace value.Occurrence) && value.StagedIdentity.Length = 64

    let matches (value: RestartOwnershipState) (runIdentity: ProviderRestartAttemptRunId) occurrence stagedIdentity =
        value.RunIdentity = runIdentity.Value
        && value.Occurrence = OccurrenceId.value occurrence
        && value.StagedIdentity = ProviderArtifactSetId.value stagedIdentity

    let project code live (value: RestartOwnershipState) =
        { Code = code
          Epoch = value.Epoch
          Owner = ProviderRestartOwnerId.create value.Owner
          Lease = ProviderRestartOwnershipLeaseId.create value.Lease
          RunIdentity = ProviderRestartAttemptRunId.create value.RunIdentity
          Occurrence = OccurrenceId.create value.Occurrence
          StagedIdentity = ProviderArtifactSetId.create value.StagedIdentity
          IsLive = live }

    let encode (value: RestartOwnershipState) =
        use output = new MemoryStream()
        use writer = new Utf8JsonWriter(output)
        writer.WriteStartObject()
        writer.WriteString("format", value.Format)
        writer.WriteNumber("epoch", value.Epoch)
        writer.WriteString("owner", value.Owner)
        writer.WriteString("lease", value.Lease)
        writer.WriteString("runIdentity", value.RunIdentity)
        writer.WriteString("occurrence", value.Occurrence)
        writer.WriteString("stagedIdentity", value.StagedIdentity)
        writer.WriteEndObject()
        writer.Flush()
        output.ToArray()

    let private text (value: JsonElement) = value.GetString() |> Option.ofObj |> Option.defaultValue ""

    let decode (record: byte array) : RestartOwnershipState option =
        try
            use document = JsonDocument.Parse record
            let root = document.RootElement
            let value =
                { Format = root.GetProperty("format") |> text
                  Epoch = root.GetProperty("epoch").GetInt64()
                  Owner = root.GetProperty("owner") |> text
                  Lease = root.GetProperty("lease") |> text
                  RunIdentity = root.GetProperty("runIdentity") |> text
                  Occurrence = root.GetProperty("occurrence") |> text
                  StagedIdentity = root.GetProperty("stagedIdentity") |> text }
            if isValid value then Some value else None
        with
        | :? JsonException | :? InvalidOperationException | :? Collections.Generic.KeyNotFoundException
        | :? FormatException | :? ArgumentException -> None

    let tryWrite path value =
        let temporary = path + ".tmp"
        try
            let record = encode value
            if not (isValid value) || record.Length + TagBytes > MaxBytes then false
            else
                use output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)
                output.Write(record, 0, record.Length)
                let tag = SHA256.HashData record
                output.Write(tag, 0, tag.Length)
                output.Flush true
                output.Dispose()
                File.Move(temporary, path, true)
                true
        with :? IOException | :? UnauthorizedAccessException | :? NotSupportedException ->
            try if File.Exists temporary then File.Delete temporary
            with :? IOException | :? UnauthorizedAccessException -> ()
            false

    let tryRead path : RestartOwnershipState option =
        try
            let bytes = File.ReadAllBytes path
            if bytes.Length <= TagBytes || bytes.Length > MaxBytes then None
            else
                let record = bytes[.. bytes.Length - TagBytes - 1]
                let tag = ReadOnlySpan(bytes, bytes.Length - TagBytes, TagBytes)
                if not (CryptographicOperations.FixedTimeEquals(ReadOnlySpan(SHA256.HashData record), tag)) then None
                else decode record
        with :? IOException | :? UnauthorizedAccessException -> None

type DurableProviderRestartOwnership private (lockStream: FileStream, journalPath: string, statePath: string, initial: RestartOwnershipState) =
    let gate = new SemaphoreSlim(1, 1)
    let mutable disposed = false
    let state = initial

    // Lineage identities do not identify a journal: two journals can carry the same run, occurrence,
    // and staged identity, and a copied journal file carries them by construction. The path is what
    // distinguishes them, so it is compared alongside the identities rather than trusted.
    let isCurrent (journal: DurableProviderRestartAttemptJournal) =
        let snapshot = journal.Snapshot
        not disposed && not lockStream.SafeFileHandle.IsClosed
        && String.Equals(journalPath, journal.Path, StringComparison.Ordinal)
        && state.RunIdentity = snapshot.RunIdentity.Value
        && state.Occurrence = OccurrenceId.value snapshot.Occurrence
        && state.StagedIdentity = ProviderArtifactSetId.value snapshot.StagedIdentity
        && (ProviderRestartOwnershipRecord.tryRead statePath
            |> Option.exists (fun current ->
                current.Epoch = state.Epoch && current.Owner = state.Owner && current.Lease = state.Lease
                && ProviderRestartOwnershipRecord.matches current snapshot.RunIdentity snapshot.Occurrence snapshot.StagedIdentity))

    member _.Snapshot =
        ProviderRestartOwnershipRecord.project "restart-ownership-current"
            (not disposed && not lockStream.SafeFileHandle.IsClosed) state

    /// The journal path this lease was acquired for. A lease fences exactly the journal it names.
    member _.JournalPath = journalPath

    member _.IsCurrentFor(journal: DurableProviderRestartAttemptJournal) =
        if isNull (box journal) then nullArg (nameof journal)
        gate.Wait()
        try isCurrent journal
        finally gate.Release() |> ignore

    member internal _.TryEnterAsync(journal: DurableProviderRestartAttemptJournal) = task {
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

    interface IDisposable with
        member _.Dispose() =
            gate.Wait()
            try
                if not disposed then
                    lockStream.Dispose()
                    disposed <- true
            finally gate.Release() |> ignore

    /// `journalPath` is the CBI53 journal this lease fences, not a free ownership path. The lock path
    /// is derived from it so that two hosts coordinating one journal cannot pick two different lock
    /// files and exclude nobody; one journal has exactly one ownership path by construction.
    static member Acquire(journalPath, owner: ProviderRestartOwnerId, lease: ProviderRestartOwnershipLeaseId,
                          runIdentity: ProviderRestartAttemptRunId, occurrence, stagedIdentity) =
        if String.IsNullOrWhiteSpace journalPath then invalidArg (nameof journalPath) "A journal path is required."
        let resolvedJournalPath = Path.GetFullPath journalPath
        let lockPath = resolvedJournalPath + ".ownership"
        let statePath = lockPath + ".state"
        let prepared =
            try
                match Path.GetDirectoryName lockPath with
                | null -> invalidArg (nameof journalPath) "A journal path must have a parent directory."
                | parent -> Directory.CreateDirectory parent |> ignore
                Ok()
            with :? IOException | :? UnauthorizedAccessException | :? NotSupportedException ->
                Error "restart-ownership-unavailable"
        let opened =
            match prepared with
            | Error code -> Error code
            | Ok () ->
                try Ok(new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                with
                | :? IOException -> Error "restart-ownership-busy"
                | :? UnauthorizedAccessException | :? NotSupportedException -> Error "restart-ownership-unavailable"
        match opened with
        | Error code -> { Code = code; Ownership = None; Snapshot = None }
        | Ok held ->
            let prior = if File.Exists statePath then ProviderRestartOwnershipRecord.tryRead statePath else None
            if File.Exists statePath && prior.IsNone then
                held.Dispose()
                { Code = "restart-ownership-corrupt"; Ownership = None; Snapshot = None }
            elif prior |> Option.exists (fun value -> not (ProviderRestartOwnershipRecord.matches value runIdentity occurrence stagedIdentity)) then
                held.Dispose()
                { Code = "restart-ownership-lineage-mismatch"; Ownership = None
                  Snapshot = prior |> Option.map (ProviderRestartOwnershipRecord.project "restart-ownership-observed" false) }
            elif prior |> Option.exists (fun value -> value.Epoch = Int64.MaxValue) then
                held.Dispose()
                { Code = "restart-ownership-epoch-exhausted"; Ownership = None
                  Snapshot = prior |> Option.map (ProviderRestartOwnershipRecord.project "restart-ownership-observed" false) }
            else
                let next =
                    { Format = "CBI54"; Epoch = (prior |> Option.map _.Epoch |> Option.defaultValue 0L) + 1L
                      Owner = owner.Value; Lease = lease.Value; RunIdentity = runIdentity.Value
                      Occurrence = OccurrenceId.value occurrence; StagedIdentity = ProviderArtifactSetId.value stagedIdentity }
                if not (ProviderRestartOwnershipRecord.isValid next) then
                    held.Dispose()
                    invalidArg (nameof owner) "Valid restart ownership identities are required."
                elif not (ProviderRestartOwnershipRecord.tryWrite statePath next) then
                    held.Dispose()
                    { Code = "restart-ownership-write-failed"; Ownership = None
                      Snapshot = prior |> Option.map (ProviderRestartOwnershipRecord.project "restart-ownership-observed" false) }
                else
                    let ownership = new DurableProviderRestartOwnership(held, resolvedJournalPath, statePath, next)
                    { Code = "restart-ownership-acquired"; Ownership = Some ownership; Snapshot = Some ownership.Snapshot }

    static member Inspect(journalPath, runIdentity, occurrence, stagedIdentity) =
        if String.IsNullOrWhiteSpace journalPath then invalidArg (nameof journalPath) "A journal path is required."
        let statePath = Path.GetFullPath(journalPath) + ".ownership.state"
        if not (File.Exists statePath) then { Code = "restart-ownership-missing"; Snapshot = None }
        else
            match ProviderRestartOwnershipRecord.tryRead statePath with
            | None -> { Code = "restart-ownership-corrupt"; Snapshot = None }
            | Some value when not (ProviderRestartOwnershipRecord.matches value runIdentity occurrence stagedIdentity) ->
                { Code = "restart-ownership-lineage-mismatch"
                  Snapshot = Some(ProviderRestartOwnershipRecord.project "restart-ownership-observed" false value) }
            | Some value ->
                { Code = "restart-ownership-observed"
                  Snapshot = Some(ProviderRestartOwnershipRecord.project "restart-ownership-observed" false value) }

and ProviderRestartOwnershipAcquireResult =
    { Code: string
      Ownership: DurableProviderRestartOwnership option
      Snapshot: ProviderRestartOwnershipSnapshot option }

[<RequireQualifiedAccess>]
module CrossProcessProviderRestartRecovery =
    let run (ownership: DurableProviderRestartOwnership) (journal: DurableProviderRestartAttemptJournal) registry store activation cause currentCyclePolicyIdentity now = task {
        if isNull (box ownership) then nullArg (nameof ownership)
        if isNull (box journal) then nullArg (nameof journal)
        let snapshot = journal.Snapshot
        let! held = ownership.TryEnterAsync journal
        match held with
        | None -> return { Code = "restart-ownership-required"; Snapshot = snapshot; Decision = None; Enforcement = None }
        | Some lease ->
            use _ = lease
            return! DurableProviderRestartRecovery.run journal registry store activation cause currentCyclePolicyIdentity now
    }
