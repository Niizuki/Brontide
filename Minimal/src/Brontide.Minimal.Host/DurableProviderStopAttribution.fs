namespace Brontide.Minimal.Host

open System
open System.Buffers.Binary
open System.Collections.Generic
open System.IO
open System.Security.Cryptography
open System.Text
open System.Threading.Tasks
open Brontide.Minimal.Experimental.ComponentManagement

type ProviderRestartCause =
    | UnexpectedExit
    | OfflineAvailability
    | PublisherTrustWithdrawal
    | OperatorRetirement

/// Why the host stopped one occurrence's provider, as the store issued it. The single case is private,
/// so there is no construction path outside this file: CBI51 used to take a `ProviderRestartCause` the
/// caller chose, and two of its four values are refusals, so a caller could select which refusal
/// applied to it. The only way to obtain one of these is to ask the store about an activation.
type ProviderStopAttribution =
    private
    | ProviderStopAttribution of OccurrenceId * ProviderArtifactSetId * DateTimeOffset option * ProviderRestartCause

    member this.Occurrence =
        let (ProviderStopAttribution(value, _, _, _)) = this
        value
    member this.StagedIdentity =
        let (ProviderStopAttribution(_, value, _, _)) = this
        value
    /// `None` when the host holds no record, which is what an unexpected exit looks like.
    member this.Instant =
        let (ProviderStopAttribution(_, _, value, _)) = this
        value
    member this.Cause =
        let (ProviderStopAttribution(_, _, _, value)) = this
        value

type ProviderStopAttributionResult =
    { Code: string
      Attribution: ProviderStopAttribution option }

type private ProviderStopEntry =
    { Occurrence: string
      StagedIdentity: string
      Instant: DateTimeOffset
      Cause: ProviderRestartCause }

[<RequireQualifiedAccess>]
module private ProviderStopAttributionRecord =
    [<Literal>]
    let MaxBytes = 65536
    [<Literal>]
    let TagBytes = 32
    [<Literal>]
    let MaximumRecords = 64

    let private causeCode cause =
        match cause with
        | UnexpectedExit -> 0
        | OfflineAvailability -> 1
        | PublisherTrustWithdrawal -> 2
        | OperatorRetirement -> 3

    let private causeOf value =
        match value with
        | 1 -> Some OfflineAvailability
        | 2 -> Some PublisherTrustWithdrawal
        | 3 -> Some OperatorRetirement
        // Zero is an unexpected exit, which is what an absent record means; a record naming it would
        // be a record of the host not having stopped anything.
        | _ -> None

    let private writeInt32 (output: Stream) value =
        let buffer = Array.zeroCreate<byte> 4
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(), value)
        output.Write buffer
    let private writeInt64 (output: Stream) (value: int64) =
        let buffer = Array.zeroCreate<byte> 8
        BinaryPrimitives.WriteInt64BigEndian(buffer.AsSpan(), value)
        output.Write buffer
    let private writeString output (value: string) =
        let encoded = UTF8Encoding(false, true).GetBytes value
        writeInt32 output encoded.Length
        output.Write encoded

    let encode (entries: ProviderStopEntry list) =
        use output = new MemoryStream()
        writeString output "CBI67"
        writeInt32 output entries.Length
        // Ordered, so one set of records has one encoding and a rewrite that changed nothing produces
        // the same bytes.
        for entry in entries |> List.sortWith (fun left right ->
                                    String.CompareOrdinal(left.Occurrence, right.Occurrence)) do
            writeString output entry.Occurrence
            writeString output entry.StagedIdentity
            writeInt64 output (entry.Instant.ToUnixTimeMilliseconds())
            writeInt32 output (causeCode entry.Cause)
        let record = output.ToArray()
        Array.append record (SHA256.HashData record)

    let decode (bytes: byte array) =
        if bytes.Length <= TagBytes || bytes.Length > MaxBytes then None
        else
            let record = bytes[.. bytes.Length - TagBytes - 1]
            if not (CryptographicOperations.FixedTimeEquals(
                        ReadOnlySpan(SHA256.HashData record),
                        ReadOnlySpan(bytes, bytes.Length - TagBytes, TagBytes))) then None
            else
                let mutable offset = 0
                let ensure length = if length > record.Length - offset then raise (InvalidDataException())
                let readInt32 () =
                    ensure 4
                    let value = BinaryPrimitives.ReadInt32BigEndian(ReadOnlySpan(record, offset, 4))
                    offset <- offset + 4
                    value
                let readInt64 () =
                    ensure 8
                    let value = BinaryPrimitives.ReadInt64BigEndian(ReadOnlySpan(record, offset, 8))
                    offset <- offset + 8
                    value
                let readString () =
                    let length = readInt32 ()
                    if length < 0 || length > MaxBytes then raise (InvalidDataException())
                    ensure length
                    let value = UTF8Encoding(false, true).GetString(record, offset, length)
                    offset <- offset + length
                    value
                try
                    if readString () <> "CBI67" then None
                    else
                        let count = readInt32 ()
                        if count < 0 || count > MaximumRecords then None
                        else
                            let entries = ResizeArray<ProviderStopEntry>()
                            let mutable valid = true
                            for _ in 1..count do
                                if valid then
                                    let occurrence = readString ()
                                    let stagedIdentity = readString ()
                                    let instant = DateTimeOffset.FromUnixTimeMilliseconds(readInt64 ())
                                    match causeOf (readInt32 ()) with
                                    | Some cause when occurrence.Length > 0 && stagedIdentity.Length > 0
                                                      && not (entries |> Seq.exists (fun entry ->
                                                                entry.Occurrence = occurrence)) ->
                                        entries.Add
                                            { Occurrence = occurrence; StagedIdentity = stagedIdentity
                                              Instant = instant; Cause = cause }
                                    | _ -> valid <- false
                            if valid && offset = record.Length then Some(List.ofSeq entries) else None
                with
                | :? InvalidDataException | :? ArgumentException | :? DecoderFallbackException -> None

type ProviderStopAttributionStoreResult =
    { Code: string
      Store: DurableProviderStopAttributionStore option }

/// A host-local record of why the host stopped each occurrence's provider. Every path in the host that
/// stops one writes here after the effect is complete, and CBI51 reads the cause from here instead of
/// being told it.
///
/// The integrity tag detects corruption and truncation, exactly as CBI42's floor store does and with
/// the same limit: it is not a defence against an adversary who can write this file, because such an
/// adversary recomputes the tag.
and DurableProviderStopAttributionStore private (path: string, initial: ProviderStopEntry list) =
    let syncRoot = obj ()
    let mutable entries = initial

    static let tryDelete path =
        try if File.Exists path then File.Delete path with
        | :? IOException | :? UnauthorizedAccessException -> ()

    static let tryWrite path (values: ProviderStopEntry list) =
        let temporary = path + ".tmp"
        try
            match Path.GetDirectoryName(path: string) with
            | null -> invalidArg (nameof path) "A stop-attribution path must have a parent directory."
            | parent -> Directory.CreateDirectory parent |> ignore
            let bytes = ProviderStopAttributionRecord.encode values
            if bytes.Length > ProviderStopAttributionRecord.MaxBytes then false
            else
                use output =
                    new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096,
                                   FileOptions.WriteThrough)
                output.Write bytes
                output.Flush true
                output.Dispose()
                File.Move(temporary, path, true)
                true
        with
        | :? IOException | :? UnauthorizedAccessException | :? NotSupportedException ->
            tryDelete temporary
            false

    static member Open(path: string) =
        if String.IsNullOrWhiteSpace path then invalidArg (nameof path) "A store path is required."
        let fullPath = Path.GetFullPath path
        tryDelete (fullPath + ".tmp")
        if not (File.Exists fullPath) then
            if tryWrite fullPath [] then
                { Code = "provider-stop-attribution-established"
                  Store = Some(DurableProviderStopAttributionStore(fullPath, [])) }
            else { Code = "provider-stop-attribution-write-failed"; Store = None }
        else
            match ProviderStopAttributionRecord.decode (File.ReadAllBytes fullPath) with
            | None -> { Code = "provider-stop-attribution-corrupt"; Store = None }
            | Some stored ->
                { Code = "provider-stop-attribution-opened"
                  Store = Some(DurableProviderStopAttributionStore(fullPath, stored)) }

    /// Records one stop. Callers invoke this once the effect is complete, never before: a record is a
    /// statement about something that happened, so it cannot precede the thing it describes — CBI41's
    /// rule about its own floor, in its third instance. A record written first and then interrupted
    /// would claim a stop that did not occur, and CBI52 would launch a second provider for an
    /// occurrence that is still serving.
    member _.Record(occurrence: OccurrenceId, stagedIdentity: ProviderArtifactSetId,
                    instant: DateTimeOffset, cause: ProviderRestartCause) =
        match cause with
        | UnexpectedExit -> invalidArg (nameof cause) "An unexpected exit is an absence, not a record."
        | _ ->
            lock syncRoot (fun () ->
                let key = OccurrenceId.value occurrence
                let next =
                    { Occurrence = key
                      StagedIdentity = ProviderArtifactSetId.value stagedIdentity
                      Instant = instant
                      Cause = cause }
                    :: (entries |> List.filter (fun entry -> entry.Occurrence <> key))
                if next.Length > ProviderStopAttributionRecord.MaximumRecords then
                    "provider-stop-attribution-full"
                elif not (tryWrite path next) then "provider-stop-attribution-write-failed"
                else
                    entries <- next
                    "provider-stop-attribution-recorded")

    /// Removes the record a successful reconstruction consumed.
    member _.Clear(occurrence: OccurrenceId) =
        lock syncRoot (fun () ->
            let key = OccurrenceId.value occurrence
            if not (entries |> List.exists (fun entry -> entry.Occurrence = key)) then
                "provider-stop-attribution-absent"
            else
                let next = entries |> List.filter (fun entry -> entry.Occurrence <> key)
                if not (tryWrite path next) then "provider-stop-attribution-write-failed"
                else
                    entries <- next
                    "provider-stop-attribution-cleared")

    /// Issues the attribution for one activation. A record the store holds for that occurrence under a
    /// different staged identity describes a different deployment and is refused rather than resolved
    /// either way. No record at all is an unexpected exit, because every stop the host performs writes
    /// one.
    member _.Attribute(occurrence: OccurrenceId, stagedIdentity: ProviderArtifactSetId) =
        lock syncRoot (fun () ->
            let key = OccurrenceId.value occurrence
            match entries |> List.tryFind (fun entry -> entry.Occurrence = key) with
            | None ->
                { Code = "provider-stop-attribution-unrecorded"
                  Attribution = Some(ProviderStopAttribution(occurrence, stagedIdentity, None, UnexpectedExit)) }
            | Some entry when entry.StagedIdentity <> ProviderArtifactSetId.value stagedIdentity ->
                { Code = "provider-restart-attribution-stale"; Attribution = None }
            | Some entry ->
                { Code = "provider-stop-attribution-issued"
                  Attribution =
                    Some(ProviderStopAttribution(
                            occurrence, stagedIdentity, Some entry.Instant, entry.Cause)) })
