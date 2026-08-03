namespace Brontide.Minimal.Host

open System
open System.Buffers.Binary
open System.IO
open System.Security.Cryptography
open System.Text
open System.Threading
open System.Threading.Tasks

type ProviderPublisherTrustPolicyFloorRetentionResult =
    { Code: string
      Stored: ProviderPublisherTrustPolicyRecoveryFloor }
    member this.IsRetained = this.Code = "policy-floor-retained" || this.Code = "policy-floor-unchanged"

[<RequireQualifiedAccess>]
module ProviderPublisherTrustPolicyFloorRecord =
    [<Literal>]
    let MaxBytes = 65536
    [<Literal>]
    let private TagBytes = 32

    let private writeInt32 (output: Stream) value =
        let buffer = Array.zeroCreate<byte> 4
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(), value)
        output.Write buffer
    let private writeInt64 (output: Stream) value =
        let buffer = Array.zeroCreate<byte> 8
        BinaryPrimitives.WriteInt64BigEndian(buffer.AsSpan(), value)
        output.Write buffer
    let private writeString output (value: string) =
        let encoded = UTF8Encoding(false, true).GetBytes value
        writeInt32 output encoded.Length
        output.Write encoded

    let encode (authority: ProviderPublisherTrustPolicyAuthorityId) sequence policyIdentity =
        use output = new MemoryStream()
        writeString output "CBI42"
        writeString output (ProviderPublisherTrustPolicyAuthorityId.value authority)
        writeInt64 output sequence
        writeInt32 output (if Option.isSome policyIdentity then 1 else 0)
        policyIdentity |> Option.iter (ProviderPublisherTrustPolicyId.value >> writeString output)
        let record = output.ToArray()
        Array.append record (SHA256.HashData record)

    /// Answers the authority, sequence, and policy identity of a well-formed image, or nothing when
    /// the tag, structure, or values do not hold.
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
                    let value = BinaryPrimitives.ReadInt32BigEndian(record.AsSpan(offset, 4))
                    offset <- offset + 4
                    value
                let readInt64 () =
                    ensure 8
                    let value = BinaryPrimitives.ReadInt64BigEndian(record.AsSpan(offset, 8))
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
                    if readString () <> "CBI42" then None
                    else
                        let authority = readString () |> ProviderPublisherTrustPolicyAuthorityId.create
                        let sequence = readInt64 ()
                        let presence = readInt32 ()
                        if sequence < 0L || (presence <> 0 && presence <> 1) then None
                        else
                            let identity =
                                if presence = 1 then Some(readString () |> ProviderPublisherTrustPolicyId.create)
                                else None
                            // A sequence above zero without an identity, or the reverse, is not a
                            // floor any issuer produces, so it is refused rather than half-read.
                            if offset <> record.Length || (sequence = 0L) <> Option.isNone identity then None
                            else Some(authority, sequence, identity)
                with
                | :? InvalidDataException
                | :? ArgumentException
                | :? DecoderFallbackException -> None

/// Durable custody of the CBI38 recovery floor. The integrity tag detects corruption and truncation;
/// it is not a defence against an adversary who can write this file, because such an adversary
/// recomputes the tag. Real custody is a separate privilege domain and is not implemented here.
type DurableProviderPublisherTrustPolicyFloorStore private (
    path: string,
    authority: ProviderPublisherTrustPolicyAuthorityId,
    initial: ProviderPublisherTrustPolicyRecoveryFloor) =

    let syncRoot = obj ()
    let mutable stored = initial

    static let tryDelete path =
        try if File.Exists path then File.Delete path
        with :? IOException | :? UnauthorizedAccessException -> ()

    static let tryWrite path (floor: ProviderPublisherTrustPolicyRecoveryFloor) =
        let temporary = path + ".tmp"
        try
            match Path.GetDirectoryName(path: string) with
            | null -> invalidArg (nameof path) "A floor path must have a parent directory."
            | parent -> Directory.CreateDirectory parent |> ignore
            let bytes =
                ProviderPublisherTrustPolicyFloorRecord.encode
                    floor.AuthorityIdentity floor.Sequence floor.PolicyIdentity
            if bytes.Length > ProviderPublisherTrustPolicyFloorRecord.MaxBytes then false
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
        | :? IOException
        | :? UnauthorizedAccessException
        | :? NotSupportedException ->
            tryDelete temporary
            false

    member _.Stored = lock syncRoot (fun () -> stored)

    static member Open(path: string, authority: ProviderPublisherTrustPolicyAuthorityId) =
        if String.IsNullOrWhiteSpace path then invalidArg (nameof path) "A floor path is required."
        let fullPath = Path.GetFullPath path
        tryDelete (fullPath + ".tmp")
        if not (File.Exists fullPath) then
            let empty = ProviderPublisherTrustPolicyRecoveryFloor.Restore(authority, 0L, None)
            // Establishing at zero before anything is published is what lets a later absence mean
            // "the guard was removed" rather than "nothing has happened yet".
            if not (tryWrite fullPath empty) then "policy-floor-write-failed", None
            else "policy-floor-established", Some(DurableProviderPublisherTrustPolicyFloorStore(fullPath, authority, empty))
        else
            let read =
                try ProviderPublisherTrustPolicyFloorRecord.decode (File.ReadAllBytes fullPath)
                with :? IOException | :? UnauthorizedAccessException -> None
            match read with
            | None -> "policy-floor-corrupt", None
            | Some(storedAuthority, _, _) when storedAuthority <> authority ->
                "policy-floor-authority-mismatch", None
            | Some(_, sequence, identity) ->
                let floor = ProviderPublisherTrustPolicyRecoveryFloor.Restore(authority, sequence, identity)
                "policy-floor-recovered", Some(DurableProviderPublisherTrustPolicyFloorStore(fullPath, authority, floor))

    member _.Retain(floor: ProviderPublisherTrustPolicyRecoveryFloor) =
        if isNull (box floor) then nullArg (nameof floor)
        lock syncRoot (fun () ->
            if floor.AuthorityIdentity <> authority then
                { Code = "policy-floor-authority-mismatch"; Stored = stored }
            elif floor.Sequence = stored.Sequence && floor.PolicyIdentity = stored.PolicyIdentity then
                { Code = "policy-floor-unchanged"; Stored = stored }
            // Same sequence under a different identity is a fork, which is a regression rather than
            // an advance: the floor would stop recognising the chain it was retained from.
            elif floor.Sequence <= stored.Sequence then
                { Code = "policy-floor-regressed"; Stored = stored }
            elif not (tryWrite path floor) then
                { Code = "policy-floor-write-failed"; Stored = stored }
            else
                stored <- floor
                { Code = "policy-floor-retained"; Stored = stored })

    /// A refused retention reaches CBI41 as a failed handoff rather than being swallowed, so the
    /// cycle reports an advanced-but-unretained floor instead of claiming custody it does not have.
    member this.Sink: ProviderPublisherTrustPolicyFloorSink =
        fun floor cancellationToken ->
            cancellationToken.ThrowIfCancellationRequested()
            let result = this.Retain floor
            if not result.IsRetained then
                invalidOp $"The recovery floor was not retained: {result.Code}."
            Task.CompletedTask

[<RequireQualifiedAccess>]
module ProviderPublisherTrustPolicyCustody =
    /// Opens the durable registry under the floor its own store holds. The floor CBI38 reports back
    /// is never written to the store: a checkpoint that could raise its own guard would let a forged
    /// chain reaching further than the true one refuse every genuine successor afterwards.
    let open' (checkpointPath: string) (floorPath: string) authority =
        if String.IsNullOrWhiteSpace checkpointPath then
            invalidArg (nameof checkpointPath) "A checkpoint path is required."
        if String.IsNullOrWhiteSpace floorPath then
            invalidArg (nameof floorPath) "A floor path is required."
        let checkpointExists = File.Exists(Path.GetFullPath checkpointPath)
        if not (File.Exists(Path.GetFullPath floorPath)) && checkpointExists then
            "policy-floor-missing", None, None, None
        else
            match DurableProviderPublisherTrustPolicyFloorStore.Open(floorPath, authority) with
            | code, None -> code, None, None, None
            | _, Some store ->
                match DurableProviderPublisherTrustPolicyRegistry.Open(
                        checkpointPath, authority, Some store.Stored) with
                | code, None, _ -> code, Some code, None, None
                | code, Some registry, _ -> "policy-floor-opened", Some code, Some registry, Some store
