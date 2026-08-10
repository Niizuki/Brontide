namespace Brontide.Minimal.Host

open System
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Threading
open Brontide.Minimal.Experimental.ComponentManagement

module private RestartEffectPath =
    let fileName (path: string) =
        Path.GetFileName path |> Option.ofObj |> Option.defaultValue ""

type ProviderRestartEffectToken =
    private | ProviderRestartEffectToken of string
    member this.Value = let (ProviderRestartEffectToken value) = this in value

[<RequireQualifiedAccess>]
module ProviderRestartEffectToken =
    let create (value: string) =
        if String.IsNullOrWhiteSpace value || value.Length > 128 || value <> value.Trim() then
            invalidArg (nameof value) "A restart effect token must contain 1-128 trimmed characters."
        ProviderRestartEffectToken value

type ProviderRestartEffectSnapshot =
    { RunIdentity: ProviderRestartAttemptRunId
      Occurrence: OccurrenceId
      StagedIdentity: ProviderArtifactSetId
      AttemptIndex: int
      AttemptInstant: DateTimeOffset
      FencingEpoch: int64
      Token: ProviderRestartEffectToken
      ExecutableName: string
      LeasePath: string
      ReceiptPath: string }

type ProviderRestartEffectReconciliationResult =
    { Code: string
      Effect: ProviderRestartEffectSnapshot option
      Journal: ProviderRestartAttemptJournalSnapshot
      CurrentFencingEpoch: int64
      ProcessTerminated: bool
      LeaseAvailable: bool }

type private RestartEffectState =
    { Format: string
      RunIdentity: string
      Occurrence: string
      StagedIdentity: string
      AttemptIndex: int
      AttemptInstantUtcTicks: int64
      FencingEpoch: int64
      Token: string
      ExecutableName: string }

[<RequireQualifiedAccess>]
module private ProviderRestartEffectRecord =
    [<Literal>]
    let MaxBytes = 16384
    [<Literal>]
    let TagBytes = 32

    let isValid value =
        value.Format = "CBI55" && not (String.IsNullOrWhiteSpace value.RunIdentity)
        && not (String.IsNullOrWhiteSpace value.Occurrence) && value.StagedIdentity.Length = 64
        && value.AttemptIndex >= 0 && value.AttemptIndex <= 7 && value.AttemptInstantUtcTicks > 0L
        && value.FencingEpoch > 0L && not (String.IsNullOrWhiteSpace value.Token)
        && value.Token.Length <= 128 && value.Token = value.Token.Trim()
        && not (String.IsNullOrWhiteSpace value.ExecutableName)
        && value.ExecutableName = RestartEffectPath.fileName value.ExecutableName && value.ExecutableName.Length <= 260

    let sameLineage value (runIdentity: ProviderRestartAttemptRunId) occurrence stagedIdentity =
        value.RunIdentity = runIdentity.Value && value.Occurrence = OccurrenceId.value occurrence
        && value.StagedIdentity = ProviderArtifactSetId.value stagedIdentity

    let encode value =
        use output = new MemoryStream()
        use writer = new Utf8JsonWriter(output)
        writer.WriteStartObject()
        writer.WriteString("format", value.Format)
        writer.WriteString("runIdentity", value.RunIdentity)
        writer.WriteString("occurrence", value.Occurrence)
        writer.WriteString("stagedIdentity", value.StagedIdentity)
        writer.WriteNumber("attemptIndex", value.AttemptIndex)
        writer.WriteNumber("attemptInstantUtcTicks", value.AttemptInstantUtcTicks)
        writer.WriteNumber("fencingEpoch", value.FencingEpoch)
        writer.WriteString("token", value.Token)
        writer.WriteString("executableName", value.ExecutableName)
        writer.WriteEndObject()
        writer.Flush()
        output.ToArray()

    let text (value: JsonElement) = value.GetString() |> Option.ofObj |> Option.defaultValue ""

    let decode (record: byte array) =
        try
            use document = JsonDocument.Parse record
            let root = document.RootElement
            let value =
                { Format = root.GetProperty("format") |> text
                  RunIdentity = root.GetProperty("runIdentity") |> text
                  Occurrence = root.GetProperty("occurrence") |> text
                  StagedIdentity = root.GetProperty("stagedIdentity") |> text
                  AttemptIndex = root.GetProperty("attemptIndex").GetInt32()
                  AttemptInstantUtcTicks = root.GetProperty("attemptInstantUtcTicks").GetInt64()
                  FencingEpoch = root.GetProperty("fencingEpoch").GetInt64()
                  Token = root.GetProperty("token") |> text
                  ExecutableName = root.GetProperty("executableName") |> text }
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

    let project path value =
        { RunIdentity = ProviderRestartAttemptRunId.create value.RunIdentity
          Occurrence = OccurrenceId.create value.Occurrence
          StagedIdentity = ProviderArtifactSetId.create value.StagedIdentity
          AttemptIndex = value.AttemptIndex
          AttemptInstant = DateTimeOffset(value.AttemptInstantUtcTicks, TimeSpan.Zero)
          FencingEpoch = value.FencingEpoch
          Token = ProviderRestartEffectToken.create value.Token
          ExecutableName = value.ExecutableName
          LeasePath = path + ".lease"
          ReceiptPath = path + ".receipt" }

type DurableProviderRestartEffect private (path: string, state: RestartEffectState) =
    member _.Path = path
    member _.Snapshot = ProviderRestartEffectRecord.project path state
    member this.Environment =
        Map [ "BRONTIDE_RESTART_EFFECT_LEASE", this.Snapshot.LeasePath
              "BRONTIDE_RESTART_EFFECT_RECEIPT", this.Snapshot.ReceiptPath
              "BRONTIDE_RESTART_EFFECT_TOKEN", this.Snapshot.Token.Value
              "BRONTIDE_RESTART_EFFECT_STAGED_IDENTITY", ProviderArtifactSetId.value this.Snapshot.StagedIdentity ]

    static member Prepare(path, runIdentity: ProviderRestartAttemptRunId, occurrence, stagedIdentity,
                          attemptIndex, attemptInstant: DateTimeOffset, fencingEpoch, token: ProviderRestartEffectToken,
                          executableName: string) =
        if String.IsNullOrWhiteSpace path then invalidArg (nameof path) "An effect path is required."
        if String.IsNullOrWhiteSpace executableName || executableName <> RestartEffectPath.fileName executableName
           || executableName.Length > 260 || attemptIndex < 0 || attemptIndex > 7 || fencingEpoch <= 0L then
            invalidArg (nameof executableName) "Valid exact restart effect facts are required."
        let fullPath = Path.GetFullPath path
        let prepared =
            try
                match Path.GetDirectoryName fullPath with
                | null -> invalidArg (nameof path) "An effect path must have a parent directory."
                | parent -> Directory.CreateDirectory parent |> ignore
                Ok()
            with :? IOException | :? UnauthorizedAccessException | :? NotSupportedException -> Error "restart-effect-unavailable"
        match prepared with
        | Error code -> { Code = code; Effect = None; Snapshot = None }
        | Ok () ->
            let prior = if File.Exists fullPath then ProviderRestartEffectRecord.tryRead fullPath else None
            if File.Exists fullPath && prior.IsNone then { Code = "restart-effect-corrupt"; Effect = None; Snapshot = None }
            elif prior |> Option.exists (fun value -> not (ProviderRestartEffectRecord.sameLineage value runIdentity occurrence stagedIdentity)) then
                { Code = "restart-effect-lineage-mismatch"; Effect = None
                  Snapshot = prior |> Option.map (ProviderRestartEffectRecord.project fullPath) }
            elif prior |> Option.exists (fun value -> fencingEpoch < value.FencingEpoch || fencingEpoch = value.FencingEpoch && attemptIndex <= value.AttemptIndex) then
                { Code = "restart-effect-not-successor"; Effect = None
                  Snapshot = prior |> Option.map (ProviderRestartEffectRecord.project fullPath) }
            else
                let next =
                    { Format = "CBI55"; RunIdentity = runIdentity.Value; Occurrence = OccurrenceId.value occurrence
                      StagedIdentity = ProviderArtifactSetId.value stagedIdentity; AttemptIndex = attemptIndex
                      AttemptInstantUtcTicks = attemptInstant.UtcTicks; FencingEpoch = fencingEpoch
                      Token = token.Value; ExecutableName = executableName }
                if not (ProviderRestartEffectRecord.tryWrite fullPath next) then
                    { Code = "restart-effect-write-failed"; Effect = None
                      Snapshot = prior |> Option.map (ProviderRestartEffectRecord.project fullPath) }
                else
                    ProviderRestartEffectRecord.tryDelete (fullPath + ".receipt")
                    let effect = DurableProviderRestartEffect(fullPath, next)
                    { Code = "restart-effect-prepared"; Effect = Some effect; Snapshot = Some effect.Snapshot }

    static member Open(path, runIdentity, occurrence, stagedIdentity) =
        if String.IsNullOrWhiteSpace path then invalidArg (nameof path) "An effect path is required."
        let fullPath = Path.GetFullPath path
        if not (File.Exists fullPath) then { Code = "restart-effect-missing"; Effect = None; Snapshot = None }
        else
            match ProviderRestartEffectRecord.tryRead fullPath with
            | None -> { Code = "restart-effect-corrupt"; Effect = None; Snapshot = None }
            | Some value when not (ProviderRestartEffectRecord.sameLineage value runIdentity occurrence stagedIdentity) ->
                { Code = "restart-effect-lineage-mismatch"; Effect = None
                  Snapshot = Some(ProviderRestartEffectRecord.project fullPath value) }
            | Some value ->
                let effect = DurableProviderRestartEffect(fullPath, value)
                { Code = "restart-effect-opened"; Effect = Some effect; Snapshot = Some effect.Snapshot }

and ProviderRestartEffectOpenResult =
    { Code: string
      Effect: DurableProviderRestartEffect option
      Snapshot: ProviderRestartEffectSnapshot option }

type private RestartEffectReceipt =
    { Token: string
      StagedIdentity: string
      ProcessId: int
      ProcessStartUtcTicks: int64
      ExecutableName: string }

[<RequireQualifiedAccess>]
module ExternallyReconciledProviderRestartRecovery =
    [<Literal>]
    let private ReceiptMaxBytes = 16384
    [<Literal>]
    let private TagBytes = 32

    let private result code effect journal epoch terminated available =
        { Code = code; Effect = effect; Journal = journal; CurrentFencingEpoch = epoch
          ProcessTerminated = terminated; LeaseAvailable = available }

    let private tryLease path =
        try
            use _ = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)
            true
        with :? IOException | :? UnauthorizedAccessException | :? NotSupportedException -> false

    let private tryReadReceipt path =
        try
            let bytes = File.ReadAllBytes path
            if bytes.Length <= TagBytes || bytes.Length > ReceiptMaxBytes then None
            else
                let record = bytes[.. bytes.Length - TagBytes - 1]
                let tag = ReadOnlySpan(bytes, bytes.Length - TagBytes, TagBytes)
                if not (CryptographicOperations.FixedTimeEquals(ReadOnlySpan(SHA256.HashData record), tag)) then None
                else
                    use document = JsonDocument.Parse record
                    let root = document.RootElement
                    if (root.GetProperty("format") |> ProviderRestartEffectRecord.text) <> "CBI55" then None
                    else
                        let receipt =
                            { Token = root.GetProperty("token") |> ProviderRestartEffectRecord.text
                              StagedIdentity = root.GetProperty("stagedIdentity") |> ProviderRestartEffectRecord.text
                              ProcessId = root.GetProperty("processId").GetInt32()
                              ProcessStartUtcTicks = root.GetProperty("processStartUtcTicks").GetInt64()
                              ExecutableName = root.GetProperty("executableName") |> ProviderRestartEffectRecord.text }
                        if receipt.ProcessId > 0 && receipt.ProcessStartUtcTicks > 0L then Some receipt else None
        with
        | :? IOException | :? UnauthorizedAccessException | :? JsonException | :? InvalidOperationException
        | :? FormatException | :? Collections.Generic.KeyNotFoundException -> None

    let private retry code effect (journal: DurableProviderRestartAttemptJournal) epoch terminated =
        let transitioned = journal.ResolveInterrupted ProviderRestartAttemptRecoveryDecision.Retry
        if transitioned.Code = "durable-restart-retry-ready" then
            result code (Some effect) transitioned.Snapshot epoch terminated true
        else result transitioned.Code (Some effect) transitioned.Snapshot epoch terminated true

    let private reconcileHeld (ownership: DurableProviderRestartOwnership) (journal: DurableProviderRestartAttemptJournal) effectPath =
        let initial = journal.Snapshot
        let currentEpoch = ownership.Snapshot.Epoch
        if initial.Phase <> "in-flight" then
            result "restart-effect-reconciliation-not-required" None initial currentEpoch false false
        else
            let opened = DurableProviderRestartEffect.Open(effectPath, initial.RunIdentity, initial.Occurrence, initial.StagedIdentity)
            match opened.Effect, opened.Snapshot with
            | None, snapshot -> result opened.Code snapshot initial currentEpoch false false
            | Some _, Some effect when effect.AttemptIndex <> initial.InFlightIndex.Value || effect.AttemptInstant <> initial.InFlightInstant.Value ->
                result "restart-effect-attempt-mismatch" (Some effect) initial currentEpoch false false
            | Some _, Some effect when effect.FencingEpoch >= currentEpoch ->
                result "restart-effect-successor-fence-required" (Some effect) initial currentEpoch false false
            | Some _, Some effect when tryLease effect.LeasePath ->
                retry "restart-effect-no-live-provider" effect journal currentEpoch false
            | Some _, Some effect ->
                match tryReadReceipt effect.ReceiptPath with
                | None -> result "restart-effect-reconciliation-deferred" (Some effect) initial currentEpoch false false
                | Some receipt when receipt.Token <> effect.Token.Value
                                    || receipt.StagedIdentity <> ProviderArtifactSetId.value effect.StagedIdentity
                                    || not (String.Equals(receipt.ExecutableName, effect.ExecutableName, StringComparison.OrdinalIgnoreCase)) ->
                    result "restart-effect-receipt-mismatch" (Some effect) initial currentEpoch false false
                | Some receipt ->
                    try
                        use child = Process.GetProcessById receipt.ProcessId
                        let actualName =
                            child.MainModule
                            |> Option.ofObj
                            |> Option.map (fun moduleValue -> RestartEffectPath.fileName moduleValue.FileName)
                            |> Option.defaultValue ""
                        if child.HasExited || child.StartTime.ToUniversalTime().Ticks <> receipt.ProcessStartUtcTicks
                           || not (String.Equals(actualName, effect.ExecutableName, StringComparison.OrdinalIgnoreCase)) then
                            result "restart-effect-process-mismatch" (Some effect) initial currentEpoch false false
                        else
                            child.Kill true
                            if not (child.WaitForExit 5000) then
                                result "restart-effect-termination-failed" (Some effect) initial currentEpoch false false
                            else
                                let rec awaitLease attempt =
                                    if tryLease effect.LeasePath then
                                        retry "restart-effect-provider-terminated" effect journal currentEpoch true
                                    elif attempt = 249 then
                                        result "restart-effect-lease-still-busy" (Some effect) initial currentEpoch true false
                                    else
                                        Thread.Sleep 20
                                        awaitLease (attempt + 1)
                                awaitLease 0
                    with
                    | :? ArgumentException | :? InvalidOperationException | :? Win32Exception | :? NotSupportedException ->
                        result "restart-effect-process-unavailable" (Some effect) initial currentEpoch false false
            | _ -> result "restart-effect-corrupt" None initial currentEpoch false false

    let reconcile (ownership: DurableProviderRestartOwnership) (journal: DurableProviderRestartAttemptJournal) effectPath = task {
        if isNull (box ownership) then nullArg (nameof ownership)
        if isNull (box journal) then nullArg (nameof journal)
        let initial = journal.Snapshot
        let! held = ownership.TryEnterAsync journal
        match held with
        | None -> return result "restart-ownership-required" None initial ownership.Snapshot.Epoch false false
        | Some lease ->
            use _ = lease
            return reconcileHeld ownership journal effectPath
    }

    let run
        (ownership: DurableProviderRestartOwnership)
        (journal: DurableProviderRestartAttemptJournal)
        effectPath nextToken
        (registry: DurableProviderPublisherTrustPolicyRegistry)
        (store: ContentAddressedProviderStore)
        (activation: ProviderServingActivation)
        cause currentCyclePolicyIdentity now = task {
        if isNull (box ownership) then nullArg (nameof ownership)
        if isNull (box journal) then nullArg (nameof journal)
        let initial = journal.Snapshot
        let! held = ownership.TryEnterAsync journal
        match held with
        | None -> return { Code = "restart-ownership-required"; Snapshot = initial; Decision = None; Enforcement = None }
        | Some lease ->
            use _ = lease
            if initial.Phase = "in-flight" then
                let reconciled = reconcileHeld ownership journal effectPath
                return { Code = reconciled.Code; Snapshot = reconciled.Journal; Decision = None; Enforcement = None }
            else
                if isNull (box registry) then nullArg (nameof registry)
                if isNull (box store) then nullArg (nameof store)
                if isNull (box activation) then nullArg (nameof activation)
                if not (journal.Matches activation) then
                    return { Code = "durable-restart-lineage-mismatch"; Snapshot = initial; Decision = None; Enforcement = None }
                elif initial.Phase = "terminal" then
                    return { Code = initial.Code; Snapshot = initial; Decision = None; Enforcement = None }
                else
                    let lastAttempt = initial.Attempts |> List.tryLast |> Option.map _.Instant
                    let decision = journal.Policy.Evaluate(registry, activation, cause, currentCyclePolicyIdentity, now, initial.Attempts.Length, lastAttempt)
                    if not decision.MayRestart then
                        return { Code = decision.Code; Snapshot = initial; Decision = Some decision; Enforcement = None }
                    else
                        match activation.DistributionChain.Provider with
                        | None -> return { Code = "provider-restart-activation-unavailable"; Snapshot = initial; Decision = Some decision; Enforcement = None }
                        | Some provider ->
                            let prepared = DurableProviderRestartEffect.Prepare(
                                effectPath, initial.RunIdentity, initial.Occurrence, initial.StagedIdentity,
                                initial.NextAttemptIndex, now, ownership.Snapshot.Epoch, nextToken,
                                RestartEffectPath.fileName provider.StagedArtifacts.ExecutablePath)
                            match prepared.Effect with
                            | None -> return { Code = prepared.Code; Snapshot = initial; Decision = Some decision; Enforcement = None }
                            | Some effect ->
                                let begun = journal.BeginAttempt now
                                if begun.Code <> "durable-restart-attempt-started" then
                                    return { Code = begun.Code; Snapshot = begun.Snapshot; Decision = Some decision; Enforcement = None }
                                else
                                    let! enforcement =
                                        ProviderRestartEnforcement.runWithEffectEnvironment effect.Environment journal.Policy registry store activation
                                            cause currentCyclePolicyIdentity now initial.Attempts.Length lastAttempt
                                    let committed = journal.CommitAttempt(
                                        enforcement.Code, enforcement.RefusedBy, enforcement.ProviderStarted,
                                        enforcement.LifecycleReconstructed, enforcement.Activation.IsSome)
                                    return { Code = committed.Code; Snapshot = committed.Snapshot
                                             Decision = Some enforcement.Decision; Enforcement = Some enforcement }
    }
