namespace Brontide.Minimal.Host

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Threading
open System.Threading.Tasks

type ProviderTrustCadenceRunId =
    private
    | ProviderTrustCadenceRunId of string
    member this.Value = let (ProviderTrustCadenceRunId value) = this in value
    override this.ToString() = this.Value

[<RequireQualifiedAccess>]
module ProviderTrustCadenceRunId =
    let create (value: string) =
        if String.IsNullOrWhiteSpace value then
            invalidArg (nameof value) "A cadence run identity is required."
        if value.Length > 128 || value <> value.Trim() then
            invalidArg (nameof value) "A cadence run identity must contain 1-128 trimmed characters."
        ProviderTrustCadenceRunId value

type ProviderTrustCadenceRecoveryDecision =
    | Retry
    | Abandon

type ProviderTrustCadenceJournalCycle =
    { Index: int
      Instant: DateTimeOffset
      Code: string }

type ProviderTrustCadenceJournalSnapshot =
    { RunIdentity: ProviderTrustCadenceRunId
      Code: string
      Phase: string
      MaximumCycles: int
      Interval: TimeSpan
      PreparedInstant: DateTimeOffset
      Cycles: ProviderTrustCadenceJournalCycle list
      Gaps: TimeSpan list
      NextCycleIndex: int
      InterruptionCount: int
      RetryCount: int }

type ProviderTrustCadenceJournalTransitionResult =
    { Code: string
      Snapshot: ProviderTrustCadenceJournalSnapshot }

type private JournalCycleState =
    { Index: int
      Instant: DateTimeOffset
      Code: string }

type private JournalState =
    { Format: string
      RunIdentity: string
      MaximumCycles: int
      IntervalTicks: int64
      PreparedInstant: DateTimeOffset
      Phase: string
      TerminalCode: string option
      Cycles: JournalCycleState list
      GapTicks: int64 list
      InterruptionCount: int
      RetryCount: int }

[<RequireQualifiedAccess>]
module private ProviderTrustCadenceJournalRecord =
    [<Literal>]
    let MaxBytes = 65536
    [<Literal>]
    let TagBytes = 32

    // Drawn from the one vocabulary the cycles produce from, so a code cannot be returned by a cycle
    // and refused here — which is what CBI61's two additions were until CBI62.
    let isCycleCode code =
        ProviderServingTrustCycleCodes.isKnown code

    let isValid (value: JournalState) =
        let basic =
            value.Format = "CBI48"
            && not (String.IsNullOrWhiteSpace value.RunIdentity)
            && value.RunIdentity.Length <= 128
            && value.RunIdentity = value.RunIdentity.Trim()
            && value.MaximumCycles >= 1 && value.MaximumCycles <= 64
            && value.IntervalTicks > 0L
            && value.IntervalTicks <= TimeSpan.FromHours(24.0).Ticks
            && List.contains value.Phase [ "ready"; "waiting"; "in-flight"; "terminal" ]
            && value.Cycles.Length <= value.MaximumCycles
            && (value.GapTicks |> List.forall ((=) value.IntervalTicks))
            && value.InterruptionCount >= 0 && value.RetryCount >= 0
            && value.RetryCount <= value.InterruptionCount
            && (value.Cycles
                |> List.mapi (fun index cycle ->
                    cycle.Index = index
                    && isCycleCode cycle.Code
                    && (index = 0 || cycle.Instant > value.Cycles[index - 1].Instant))
                |> List.forall id)
        if not basic then false
        elif value.Phase = "waiting" then
            value.Cycles.Length > 0
            && value.Cycles.Length < value.MaximumCycles
            && value.GapTicks.Length = value.Cycles.Length - 1
            && Option.isNone value.TerminalCode
        elif value.Phase = "ready" || value.Phase = "in-flight" then
            value.GapTicks.Length = value.Cycles.Length && Option.isNone value.TerminalCode
        else
            match value.TerminalCode with
            | Some "durable-cadence-complete" ->
                value.Cycles.Length = value.MaximumCycles
                && value.GapTicks.Length = value.Cycles.Length - 1
            | Some "durable-cadence-stopped"
            | Some "durable-cadence-canceled" ->
                value.Cycles.Length > 0 && value.GapTicks.Length = value.Cycles.Length - 1
            | Some "durable-cadence-abandoned" ->
                value.GapTicks.Length = value.Cycles.Length
                || value.GapTicks.Length = max (value.Cycles.Length - 1) 0
            | _ -> false

    let encode (value: JournalState) =
        use output = new MemoryStream()
        use writer = new Utf8JsonWriter(output)
        writer.WriteStartObject()
        writer.WriteString("format", value.Format)
        writer.WriteString("runIdentity", value.RunIdentity)
        writer.WriteNumber("maximumCycles", value.MaximumCycles)
        writer.WriteNumber("intervalTicks", value.IntervalTicks)
        writer.WriteString("preparedInstant", value.PreparedInstant)
        writer.WriteString("phase", value.Phase)
        match value.TerminalCode with
        | Some code -> writer.WriteString("terminalCode", code)
        | None -> writer.WriteNull("terminalCode")
        writer.WritePropertyName("cycles")
        writer.WriteStartArray()
        for cycle in value.Cycles do
            writer.WriteStartObject()
            writer.WriteNumber("index", cycle.Index)
            writer.WriteString("instant", cycle.Instant)
            writer.WriteString("code", cycle.Code)
            writer.WriteEndObject()
        writer.WriteEndArray()
        writer.WritePropertyName("gapTicks")
        writer.WriteStartArray()
        for gap in value.GapTicks do writer.WriteNumberValue gap
        writer.WriteEndArray()
        writer.WriteNumber("interruptionCount", value.InterruptionCount)
        writer.WriteNumber("retryCount", value.RetryCount)
        writer.WriteEndObject()
        writer.Flush()
        output.ToArray()

    let decode (record: byte array) =
        try
            use document = JsonDocument.Parse record
            let root = document.RootElement
            let terminal =
                let property = root.GetProperty "terminalCode"
                if property.ValueKind = JsonValueKind.Null then None
                else property.GetString() |> Option.ofObj
            let cycles =
                root.GetProperty("cycles").EnumerateArray()
                |> Seq.map (fun cycle ->
                    { Index = cycle.GetProperty("index").GetInt32()
                      Instant = cycle.GetProperty("instant").GetDateTimeOffset()
                      Code = cycle.GetProperty("code").GetString() |> Option.ofObj |> Option.defaultValue "" })
                |> Seq.toList
            let value =
                { Format = root.GetProperty("format").GetString() |> Option.ofObj |> Option.defaultValue ""
                  RunIdentity = root.GetProperty("runIdentity").GetString() |> Option.ofObj |> Option.defaultValue ""
                  MaximumCycles = root.GetProperty("maximumCycles").GetInt32()
                  IntervalTicks = root.GetProperty("intervalTicks").GetInt64()
                  PreparedInstant = root.GetProperty("preparedInstant").GetDateTimeOffset()
                  Phase = root.GetProperty("phase").GetString() |> Option.ofObj |> Option.defaultValue ""
                  TerminalCode = terminal
                  Cycles = cycles
                  GapTicks = root.GetProperty("gapTicks").EnumerateArray() |> Seq.map _.GetInt64() |> Seq.toList
                  InterruptionCount = root.GetProperty("interruptionCount").GetInt32()
                  RetryCount = root.GetProperty("retryCount").GetInt32() }
            if isValid value then Some value else None
        with
        | :? JsonException
        | :? InvalidOperationException
        | :? Collections.Generic.KeyNotFoundException
        | :? FormatException
        | :? ArgumentException -> None

    let tryDelete path =
        try if File.Exists path then File.Delete path
        with :? IOException | :? UnauthorizedAccessException -> ()

    let tryWrite path value =
        let temporary = path + ".tmp"
        try
            match Path.GetDirectoryName(path: string) with
            | null -> invalidArg (nameof path) "A journal path must have a parent directory."
            | parent -> Directory.CreateDirectory parent |> ignore
            let record = encode value
            if record.Length + TagBytes > MaxBytes then false
            else
                let bytes = Array.append record (SHA256.HashData record)
                use output = new FileStream(
                    temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)
                output.Write bytes
                output.Flush true
                output.Dispose()
                File.Move(temporary, path, true)
                true
        with
        | :? IOException
        | :? UnauthorizedAccessException
        | :? NotSupportedException ->
            tryDelete temporary
            false

    let tryRead path =
        try
            let bytes = File.ReadAllBytes path
            if bytes.Length <= TagBytes || bytes.Length > MaxBytes then None
            else
                let record = bytes[.. bytes.Length - TagBytes - 1]
                let tag = ReadOnlySpan(bytes, bytes.Length - TagBytes, TagBytes)
                if not (CryptographicOperations.FixedTimeEquals(ReadOnlySpan(SHA256.HashData record), tag)) then None
                else decode record
        with :? IOException | :? UnauthorizedAccessException -> None

/// A host-local CBI48 progress journal. Its hash detects accidental damage, not a writer that can
/// replace the record and its hash. Cross-process ownership and secure custody are separate work.
type DurableProviderTrustCadenceJournal private (path: string, initial: JournalState) =
    let syncRoot = obj ()
    let mutable state = initial

    let project (value: JournalState) =
        { RunIdentity = ProviderTrustCadenceRunId.create value.RunIdentity
          Code =
            if value.Phase = "in-flight" then "durable-cadence-indeterminate"
            elif value.Phase = "terminal" then value.TerminalCode.Value
            else "durable-cadence-active"
          Phase = value.Phase
          MaximumCycles = value.MaximumCycles
          Interval = TimeSpan.FromTicks value.IntervalTicks
          PreparedInstant = value.PreparedInstant
          Cycles = value.Cycles |> List.map (fun item ->
              { Index = item.Index; Instant = item.Instant; Code = item.Code })
          Gaps = value.GapTicks |> List.map TimeSpan.FromTicks
          NextCycleIndex = value.Cycles.Length
          InterruptionCount = value.InterruptionCount
          RetryCount = value.RetryCount }

    let current code = { Code = code; Snapshot = project state }

    let transition mutation acceptedCode =
        let next = mutation state
        if not (ProviderTrustCadenceJournalRecord.isValid next)
           || not (ProviderTrustCadenceJournalRecord.tryWrite path next) then
            current "durable-cadence-write-failed"
        else
            state <- next
            current acceptedCode

    member _.Snapshot = lock syncRoot (fun () -> project state)

    member _.BeginCycle() = lock syncRoot (fun () ->
        if state.Phase = "terminal" then current state.TerminalCode.Value
        elif state.Phase = "in-flight" then current "durable-cadence-indeterminate"
        elif state.Phase <> "ready" then current "durable-cadence-waiting"
        else transition (fun value -> { value with Phase = "in-flight" }) "durable-cadence-cycle-started")

    member _.CompleteGap(nextInstant: DateTimeOffset) = lock syncRoot (fun () ->
        if state.Phase = "terminal" then current state.TerminalCode.Value
        elif state.Phase = "in-flight" then current "durable-cadence-indeterminate"
        elif state.Phase <> "waiting" || nextInstant <= state.PreparedInstant then
            current "durable-cadence-gap-invalid"
        else
            transition (fun value ->
                { value with GapTicks = value.GapTicks @ [ value.IntervalTicks ]
                             PreparedInstant = nextInstant; Phase = "ready" })
                "durable-cadence-gap-completed")

    member _.CommitCycle(cycleCode: string) =
        if String.IsNullOrWhiteSpace cycleCode then
            invalidArg (nameof cycleCode) "A cycle code is required."
        lock syncRoot (fun () ->
            if state.Phase = "terminal" then current state.TerminalCode.Value
            elif state.Phase <> "in-flight" then current "durable-cadence-cycle-not-started"
            elif not (ProviderTrustCadenceJournalRecord.isCycleCode cycleCode) then
                current "durable-cadence-result-invalid"
            else
                // The journal names the run's outcome and never renames the cycle's: every
                // non-continuing code other than cancellation stops the run, and which one it was
                // stays in the committed observation for the host to read.
                let transitionCode =
                    if cycleCode = ProviderServingTrustCycleCodes.Canceled then "durable-cadence-canceled"
                    elif not (ProviderServingTrustCycleCodes.continues cycleCode) then "durable-cadence-stopped"
                    elif state.Cycles.Length + 1 = state.MaximumCycles then "durable-cadence-complete"
                    else "durable-cadence-cycle-committed"
                transition (fun value ->
                    let cycles = value.Cycles @ [
                        { Index = value.Cycles.Length; Instant = value.PreparedInstant; Code = cycleCode } ]
                    if List.contains transitionCode [
                        "durable-cadence-stopped"; "durable-cadence-canceled"; "durable-cadence-complete" ] then
                        { value with Cycles = cycles; Phase = "terminal"; TerminalCode = Some transitionCode }
                    else { value with Cycles = cycles; Phase = "waiting" }) transitionCode)

    member _.ResolveInterrupted(decision: ProviderTrustCadenceRecoveryDecision) = lock syncRoot (fun () ->
        if state.Phase = "terminal" then current state.TerminalCode.Value
        elif state.Phase <> "in-flight" then current "durable-cadence-reconciliation-not-required"
        else
            match decision with
            | ProviderTrustCadenceRecoveryDecision.Retry ->
                transition (fun value ->
                    { value with Phase = "ready"
                                 InterruptionCount = value.InterruptionCount + 1
                                 RetryCount = value.RetryCount + 1 })
                    "durable-cadence-retry-ready"
            | ProviderTrustCadenceRecoveryDecision.Abandon ->
                transition (fun value ->
                    { value with Phase = "terminal"; TerminalCode = Some "durable-cadence-abandoned"
                                 InterruptionCount = value.InterruptionCount + 1 })
                    "durable-cadence-abandoned")

    static member Establish(
        path: string,
        runIdentity: ProviderTrustCadenceRunId,
        schedule: ProviderServingTrustCadenceSchedule,
        start: DateTimeOffset) =
        if String.IsNullOrWhiteSpace path then invalidArg (nameof path) "A journal path is required."
        if isNull (box schedule) then nullArg (nameof schedule)
        if isNull (box runIdentity) then invalidArg (nameof runIdentity) "A valid cadence run identity is required."
        let fullPath = Path.GetFullPath path
        ProviderTrustCadenceJournalRecord.tryDelete (fullPath + ".tmp")
        if File.Exists fullPath then
            { Code = "durable-cadence-already-exists"; Journal = None }
        else
            let value =
                { Format = "CBI48"; RunIdentity = runIdentity.Value
                  MaximumCycles = schedule.MaximumCycles; IntervalTicks = schedule.Interval.Ticks
                  PreparedInstant = start; Phase = "ready"; TerminalCode = None
                  Cycles = []; GapTicks = []; InterruptionCount = 0; RetryCount = 0 }
            if not (ProviderTrustCadenceJournalRecord.tryWrite fullPath value) then
                { Code = "durable-cadence-write-failed"; Journal = None }
            else
                { Code = "durable-cadence-established"
                  Journal = Some(DurableProviderTrustCadenceJournal(fullPath, value)) }

    static member Open(path: string, runIdentity: ProviderTrustCadenceRunId) =
        if String.IsNullOrWhiteSpace path then invalidArg (nameof path) "A journal path is required."
        if isNull (box runIdentity) then invalidArg (nameof runIdentity) "A valid cadence run identity is required."
        let fullPath = Path.GetFullPath path
        ProviderTrustCadenceJournalRecord.tryDelete (fullPath + ".tmp")
        if not (File.Exists fullPath) then { Code = "durable-cadence-missing"; Journal = None }
        else
            match ProviderTrustCadenceJournalRecord.tryRead fullPath with
            | None -> { Code = "durable-cadence-corrupt"; Journal = None }
            | Some value when value.RunIdentity <> runIdentity.Value ->
                { Code = "durable-cadence-run-mismatch"; Journal = None }
            | Some value ->
                let code =
                    if value.Phase = "in-flight" then "durable-cadence-indeterminate"
                    elif value.Phase = "terminal" then value.TerminalCode.Value
                    else "durable-cadence-recovered"
                { Code = code; Journal = Some(DurableProviderTrustCadenceJournal(fullPath, value)) }

and ProviderTrustCadenceJournalOpenResult =
    { Code: string
      Journal: DurableProviderTrustCadenceJournal option }

[<RequireQualifiedAccess>]
module ProviderTrustCadenceRecovery =
    let advance
        (journal: DurableProviderTrustCadenceJournal)
        (cycle: ProviderServingTrustCycle)
        (delay: ProviderServingTrustCadenceDelay)
        (cancellationToken: CancellationToken) = task {
        if isNull (box journal) then nullArg (nameof journal)
        if isNull (box cycle) then nullArg (nameof cycle)
        if isNull (box delay) then nullArg (nameof delay)

        let snapshot = journal.Snapshot
        if snapshot.Phase = "terminal" then return { Code = snapshot.Code; Snapshot = snapshot }
        elif snapshot.Phase = "in-flight" then
            return { Code = "durable-cadence-indeterminate"; Snapshot = snapshot }
        elif cancellationToken.IsCancellationRequested then
            return { Code = "durable-cadence-wait-canceled"; Snapshot = snapshot }
        else
            let! gap = task {
                if snapshot.Phase <> "waiting" then return None
                else
                    try
                        let! next = delay snapshot.PreparedInstant snapshot.Interval cancellationToken
                        if cancellationToken.IsCancellationRequested then
                            return Some { Code = "durable-cadence-wait-canceled"; Snapshot = journal.Snapshot }
                        else return Some(journal.CompleteGap next)
                    with :? OperationCanceledException ->
                        return Some { Code = "durable-cadence-wait-canceled"; Snapshot = journal.Snapshot }
            }
            match gap with
            | Some result when result.Code <> "durable-cadence-gap-completed" -> return result
            | _ ->
                let started = journal.BeginCycle()
                if started.Code <> "durable-cadence-cycle-started" then return started
                else
                    let! result = cycle started.Snapshot.PreparedInstant cancellationToken
                    return journal.CommitCycle result.Code
    }
