namespace Brontide.Minimal.Host

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open Brontide.Minimal.Experimental.ComponentManagement

type ProviderRestartAttemptRunId =
    private | ProviderRestartAttemptRunId of string
    member this.Value = let (ProviderRestartAttemptRunId value) = this in value

[<RequireQualifiedAccess>]
module ProviderRestartAttemptRunId =
    let create (value: string) =
        if String.IsNullOrWhiteSpace value then invalidArg (nameof value) "A restart run identity is required."
        if value.Length > 128 || value <> value.Trim() then
            invalidArg (nameof value) "A restart run identity must contain 1-128 trimmed characters."
        ProviderRestartAttemptRunId value

type ProviderRestartAttemptRecoveryDecision = Retry | Abandon

type ProviderRestartAttemptObservation =
    { Index: int
      Instant: DateTimeOffset
      Code: string
      RefusedBy: string
      ProviderStarted: bool
      LifecycleReconstructed: bool
      Completed: bool }

type ProviderRestartAttemptJournalSnapshot =
    { RunIdentity: ProviderRestartAttemptRunId
      Occurrence: OccurrenceId
      StagedIdentity: ProviderArtifactSetId
      Code: string
      Phase: string
      MaximumAttempts: int
      Delay: TimeSpan
      Attempts: ProviderRestartAttemptObservation list
      NextAttemptIndex: int
      InFlightIndex: int option
      InFlightInstant: DateTimeOffset option
      InterruptionCount: int
      RetryCount: int }

type ProviderRestartAttemptJournalTransitionResult =
    { Code: string
      Snapshot: ProviderRestartAttemptJournalSnapshot }

type private RestartAttemptState = ProviderRestartAttemptObservation

type private RestartJournalState =
    { Format: string
      RunIdentity: string
      Occurrence: string
      StagedIdentity: string
      MaximumAttempts: int
      DelayTicks: int64
      Phase: string
      TerminalCode: string option
      Attempts: RestartAttemptState list
      InFlightIndex: int option
      InFlightInstant: DateTimeOffset option
      InterruptionCount: int
      RetryCount: int }

[<RequireQualifiedAccess>]
module private ProviderRestartAttemptJournalRecord =
    [<Literal>]
    let MaxBytes = 65536
    [<Literal>]
    let TagBytes = 32

    let isValid (value: RestartJournalState) =
        let basic =
            value.Format = "CBI53"
            && not (String.IsNullOrWhiteSpace value.RunIdentity)
            && value.RunIdentity.Length <= 128 && value.RunIdentity = value.RunIdentity.Trim()
            && not (String.IsNullOrWhiteSpace value.Occurrence)
            && value.StagedIdentity.Length = 64
            && value.MaximumAttempts >= 1 && value.MaximumAttempts <= 8
            && value.DelayTicks > 0L && value.DelayTicks <= TimeSpan.FromHours(1.0).Ticks
            && List.contains value.Phase [ "ready"; "waiting"; "in-flight"; "terminal" ]
            && value.Attempts.Length <= value.MaximumAttempts
            && value.InterruptionCount >= 0 && value.RetryCount >= 0
            && value.RetryCount <= value.InterruptionCount
            && (value.Attempts |> List.mapi (fun index attempt ->
                attempt.Index = index
                && not (String.IsNullOrWhiteSpace attempt.Code)
                && not (String.IsNullOrWhiteSpace attempt.RefusedBy)
                && (not attempt.LifecycleReconstructed || attempt.ProviderStarted)
                && (not attempt.Completed || attempt.LifecycleReconstructed)
                && (index = 0 || attempt.Instant >= value.Attempts[index - 1].Instant.AddTicks value.DelayTicks))
                |> List.forall id)
        if not basic then false
        elif value.Phase = "in-flight" then
            value.TerminalCode.IsNone
            && value.InFlightIndex = Some value.Attempts.Length
            && value.InFlightInstant.IsSome
            && value.Attempts.Length < value.MaximumAttempts
        elif value.InFlightIndex.IsSome || value.InFlightInstant.IsSome then false
        elif value.Phase = "terminal" then
            match value.TerminalCode with
            | Some "durable-restart-completed" -> value.Attempts.Length > 0 && value.Attempts[value.Attempts.Length - 1].Completed
            | Some "durable-restart-exhausted" -> value.Attempts.Length = value.MaximumAttempts && value.Attempts |> List.forall (fun item -> not item.Completed)
            | Some "durable-restart-abandoned" -> true
            | _ -> false
        else
            value.TerminalCode.IsNone
            && (value.Phase = "ready" || value.Attempts.Length > 0 && value.Attempts.Length < value.MaximumAttempts)

    let encode (value: RestartJournalState) =
        use output = new MemoryStream()
        use writer = new Utf8JsonWriter(output)
        writer.WriteStartObject()
        writer.WriteString("format", value.Format)
        writer.WriteString("runIdentity", value.RunIdentity)
        writer.WriteString("occurrence", value.Occurrence)
        writer.WriteString("stagedIdentity", value.StagedIdentity)
        writer.WriteNumber("maximumAttempts", value.MaximumAttempts)
        writer.WriteNumber("delayTicks", value.DelayTicks)
        writer.WriteString("phase", value.Phase)
        match value.TerminalCode with Some code -> writer.WriteString("terminalCode", code) | None -> writer.WriteNull("terminalCode")
        writer.WritePropertyName("attempts")
        writer.WriteStartArray()
        for attempt in value.Attempts do
            writer.WriteStartObject()
            writer.WriteNumber("index", attempt.Index)
            writer.WriteString("instant", attempt.Instant)
            writer.WriteString("code", attempt.Code)
            writer.WriteString("refusedBy", attempt.RefusedBy)
            writer.WriteBoolean("providerStarted", attempt.ProviderStarted)
            writer.WriteBoolean("lifecycleReconstructed", attempt.LifecycleReconstructed)
            writer.WriteBoolean("completed", attempt.Completed)
            writer.WriteEndObject()
        writer.WriteEndArray()
        match value.InFlightIndex with Some index -> writer.WriteNumber("inFlightIndex", index) | None -> writer.WriteNull("inFlightIndex")
        match value.InFlightInstant with Some instant -> writer.WriteString("inFlightInstant", instant) | None -> writer.WriteNull("inFlightInstant")
        writer.WriteNumber("interruptionCount", value.InterruptionCount)
        writer.WriteNumber("retryCount", value.RetryCount)
        writer.WriteEndObject()
        writer.Flush()
        output.ToArray()

    let private optionalInt (value: JsonElement) = if value.ValueKind = JsonValueKind.Null then None else Some(value.GetInt32())
    let private optionalInstant (value: JsonElement) = if value.ValueKind = JsonValueKind.Null then None else Some(value.GetDateTimeOffset())
    let private optionalString (value: JsonElement) = if value.ValueKind = JsonValueKind.Null then None else value.GetString() |> Option.ofObj
    let private text (value: JsonElement) = value.GetString() |> Option.ofObj |> Option.defaultValue ""

    let decode (record: byte array) =
        try
            use document = JsonDocument.Parse record
            let root = document.RootElement
            let attempts = root.GetProperty("attempts").EnumerateArray() |> Seq.map (fun item ->
                { Index = item.GetProperty("index").GetInt32()
                  Instant = item.GetProperty("instant").GetDateTimeOffset()
                  Code = item.GetProperty("code") |> text
                  RefusedBy = item.GetProperty("refusedBy") |> text
                  ProviderStarted = item.GetProperty("providerStarted").GetBoolean()
                  LifecycleReconstructed = item.GetProperty("lifecycleReconstructed").GetBoolean()
                  Completed = item.GetProperty("completed").GetBoolean() }) |> Seq.toList
            let value =
                { Format = root.GetProperty("format") |> text
                  RunIdentity = root.GetProperty("runIdentity") |> text
                  Occurrence = root.GetProperty("occurrence") |> text
                  StagedIdentity = root.GetProperty("stagedIdentity") |> text
                  MaximumAttempts = root.GetProperty("maximumAttempts").GetInt32()
                  DelayTicks = root.GetProperty("delayTicks").GetInt64()
                  Phase = root.GetProperty("phase") |> text
                  TerminalCode = root.GetProperty("terminalCode") |> optionalString
                  Attempts = attempts
                  InFlightIndex = root.GetProperty("inFlightIndex") |> optionalInt
                  InFlightInstant = root.GetProperty("inFlightInstant") |> optionalInstant
                  InterruptionCount = root.GetProperty("interruptionCount").GetInt32()
                  RetryCount = root.GetProperty("retryCount").GetInt32() }
            if isValid value then Some value else None
        with
        | :? JsonException | :? InvalidOperationException | :? Collections.Generic.KeyNotFoundException
        | :? FormatException | :? ArgumentException -> None

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
                use output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)
                output.Write(record, 0, record.Length)
                let tag = SHA256.HashData record
                output.Write(tag, 0, tag.Length)
                output.Flush true
                output.Dispose()
                File.Move(temporary, path, true)
                true
        with :? IOException | :? UnauthorizedAccessException | :? NotSupportedException ->
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

/// Host-local durable CBI53 restart-attempt history.
type DurableProviderRestartAttemptJournal private (path: string, initial: RestartJournalState) =
    let syncRoot = obj()
    let mutable state = initial

    let project value =
        { RunIdentity = ProviderRestartAttemptRunId.create value.RunIdentity
          Occurrence = OccurrenceId.create value.Occurrence
          StagedIdentity = ProviderArtifactSetId.create value.StagedIdentity
          Code = if value.Phase = "in-flight" then "durable-restart-indeterminate" elif value.Phase = "terminal" then value.TerminalCode.Value else "durable-restart-active"
          Phase = value.Phase
          MaximumAttempts = value.MaximumAttempts
          Delay = TimeSpan.FromTicks value.DelayTicks
          Attempts = value.Attempts
          NextAttemptIndex = value.Attempts.Length
          InFlightIndex = value.InFlightIndex
          InFlightInstant = value.InFlightInstant
          InterruptionCount = value.InterruptionCount
          RetryCount = value.RetryCount }

    let current code = { Code = code; Snapshot = project state }
    let transition mutation code =
        let next = mutation state
        if not (ProviderRestartAttemptJournalRecord.isValid next)
           || not (ProviderRestartAttemptJournalRecord.tryWrite path next) then current "durable-restart-write-failed"
        else state <- next; current code

    member _.Snapshot = lock syncRoot (fun () -> project state)
    member _.BeginAttempt(instant: DateTimeOffset) = lock syncRoot (fun () ->
        if state.Phase = "terminal" then current state.TerminalCode.Value
        elif state.Phase = "in-flight" then current "durable-restart-indeterminate"
        elif state.Attempts.Length > 0 && instant < state.Attempts[state.Attempts.Length - 1].Instant.AddTicks state.DelayTicks then current "durable-restart-waiting"
        else transition (fun value -> { value with Phase = "in-flight"; InFlightIndex = Some value.Attempts.Length; InFlightInstant = Some instant }) "durable-restart-attempt-started")

    member _.CommitAttempt(code, refusedBy, providerStarted, lifecycleReconstructed, completed) =
        if String.IsNullOrWhiteSpace code then invalidArg (nameof code) "An attempt code is required."
        if String.IsNullOrWhiteSpace refusedBy then invalidArg (nameof refusedBy) "An attempt origin is required."
        lock syncRoot (fun () ->
            if state.Phase = "terminal" then current state.TerminalCode.Value
            elif state.Phase <> "in-flight" then current "durable-restart-attempt-not-started"
            else
                let terminal = if completed then Some "durable-restart-completed" elif state.Attempts.Length + 1 = state.MaximumAttempts then Some "durable-restart-exhausted" else None
                transition (fun value ->
                    let attempt =
                        { Index = value.InFlightIndex.Value; Instant = value.InFlightInstant.Value
                          Code = code; RefusedBy = refusedBy; ProviderStarted = providerStarted
                          LifecycleReconstructed = lifecycleReconstructed; Completed = completed }
                    { value with
                        Attempts = value.Attempts @ [ attempt ]
                        InFlightIndex = None
                        InFlightInstant = None
                        Phase = (if terminal.IsSome then "terminal" else "waiting")
                        TerminalCode = terminal })
                    (terminal |> Option.defaultValue "durable-restart-attempt-committed"))

    member _.ResolveInterrupted(decision) = lock syncRoot (fun () ->
        if state.Phase = "terminal" then current state.TerminalCode.Value
        elif state.Phase <> "in-flight" then current "durable-restart-reconciliation-not-required"
        else match decision with
             | ProviderRestartAttemptRecoveryDecision.Retry ->
                transition (fun value ->
                    { value with
                        Phase = "ready"
                        InFlightIndex = None
                        InFlightInstant = None
                        InterruptionCount = value.InterruptionCount + 1
                        RetryCount = value.RetryCount + 1 }) "durable-restart-retry-ready"
             | ProviderRestartAttemptRecoveryDecision.Abandon ->
                transition (fun value ->
                    { value with
                        Phase = "terminal"
                        TerminalCode = Some "durable-restart-abandoned"
                        InFlightIndex = None
                        InFlightInstant = None
                        InterruptionCount = value.InterruptionCount + 1 }) "durable-restart-abandoned")

    member internal _.Matches(activation: ProviderServingActivation) =
        state.Occurrence = OccurrenceId.value activation.OccurrenceId
        && activation.DistributionChain.StagedIdentity |> Option.exists (ProviderArtifactSetId.value >> (=) state.StagedIdentity)
    member internal _.Policy = ProviderRestartPolicy.create state.MaximumAttempts (TimeSpan.FromTicks state.DelayTicks)

    static member Establish(path, runIdentity: ProviderRestartAttemptRunId, occurrence, stagedIdentity, policy: ProviderRestartPolicy) =
        if String.IsNullOrWhiteSpace path then invalidArg (nameof path) "A journal path is required."
        if isNull (box policy) then nullArg (nameof policy)
        let fullPath = Path.GetFullPath path
        ProviderRestartAttemptJournalRecord.tryDelete (fullPath + ".tmp")
        if File.Exists fullPath then { Code = "durable-restart-already-exists"; Journal = None }
        else
            let value =
                { Format = "CBI53"; RunIdentity = runIdentity.Value; Occurrence = OccurrenceId.value occurrence
                  StagedIdentity = ProviderArtifactSetId.value stagedIdentity; MaximumAttempts = policy.MaximumAttempts
                  DelayTicks = policy.Delay.Ticks; Phase = "ready"; TerminalCode = None; Attempts = []
                  InFlightIndex = None; InFlightInstant = None; InterruptionCount = 0; RetryCount = 0 }
            if not (ProviderRestartAttemptJournalRecord.isValid value) then invalidArg (nameof occurrence) "Valid lineage identities are required."
            elif not (ProviderRestartAttemptJournalRecord.tryWrite fullPath value) then { Code = "durable-restart-write-failed"; Journal = None }
            else { Code = "durable-restart-established"; Journal = Some(DurableProviderRestartAttemptJournal(fullPath, value)) }

    static member Open(path, runIdentity: ProviderRestartAttemptRunId, occurrence, stagedIdentity) =
        if String.IsNullOrWhiteSpace path then invalidArg (nameof path) "A journal path is required."
        let fullPath = Path.GetFullPath path
        ProviderRestartAttemptJournalRecord.tryDelete (fullPath + ".tmp")
        if not (File.Exists fullPath) then { Code = "durable-restart-missing"; Journal = None }
        else match ProviderRestartAttemptJournalRecord.tryRead fullPath with
             | None -> { Code = "durable-restart-corrupt"; Journal = None }
             | Some value when value.RunIdentity <> runIdentity.Value || value.Occurrence <> OccurrenceId.value occurrence || value.StagedIdentity <> ProviderArtifactSetId.value stagedIdentity ->
                { Code = "durable-restart-lineage-mismatch"; Journal = None }
             | Some value ->
                let code = if value.Phase = "in-flight" then "durable-restart-indeterminate" elif value.Phase = "terminal" then value.TerminalCode.Value else "durable-restart-recovered"
                { Code = code; Journal = Some(DurableProviderRestartAttemptJournal(fullPath, value)) }

and ProviderRestartAttemptJournalOpenResult =
    { Code: string
      Journal: DurableProviderRestartAttemptJournal option }

type DurableProviderRestartResult =
    { Code: string
      Snapshot: ProviderRestartAttemptJournalSnapshot
      Decision: ProviderRestartDecision option
      Enforcement: ProviderRestartEnforcementResult option }

[<RequireQualifiedAccess>]
module DurableProviderRestartRecovery =
    let run
        (journal: DurableProviderRestartAttemptJournal)
        (registry: DurableProviderPublisherTrustPolicyRegistry)
        (store: ContentAddressedProviderStore)
        (activation: ProviderServingActivation)
        cause currentCyclePolicyIdentity now = task {
        if isNull (box journal) then nullArg (nameof journal)
        if isNull (box registry) then nullArg (nameof registry)
        if isNull (box store) then nullArg (nameof store)
        if isNull (box activation) then nullArg (nameof activation)
        let snapshot = journal.Snapshot
        if not (journal.Matches activation) then return { Code = "durable-restart-lineage-mismatch"; Snapshot = snapshot; Decision = None; Enforcement = None }
        elif snapshot.Phase = "terminal" then return { Code = snapshot.Code; Snapshot = snapshot; Decision = None; Enforcement = None }
        elif snapshot.Phase = "in-flight" then return { Code = "durable-restart-indeterminate"; Snapshot = snapshot; Decision = None; Enforcement = None }
        else
            let lastAttempt = snapshot.Attempts |> List.tryLast |> Option.map _.Instant
            let decision = journal.Policy.Evaluate(registry, activation, cause, currentCyclePolicyIdentity, now, snapshot.Attempts.Length, lastAttempt)
            if not decision.MayRestart then return { Code = decision.Code; Snapshot = snapshot; Decision = Some decision; Enforcement = None }
            else
                let begun = journal.BeginAttempt now
                if begun.Code <> "durable-restart-attempt-started" then return { Code = begun.Code; Snapshot = begun.Snapshot; Decision = Some decision; Enforcement = None }
                else
                    let! enforcement = ProviderRestartEnforcement.run journal.Policy registry store activation cause currentCyclePolicyIdentity now snapshot.Attempts.Length lastAttempt
                    let committed = journal.CommitAttempt(enforcement.Code, enforcement.RefusedBy, enforcement.ProviderStarted, enforcement.LifecycleReconstructed, enforcement.Activation.IsSome)
                    return { Code = committed.Code; Snapshot = committed.Snapshot; Decision = Some enforcement.Decision; Enforcement = Some enforcement }
    }
