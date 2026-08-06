namespace Brontide.Minimal.Host

open System
open System.Buffers.Binary
open System.IO
open System.Security.Cryptography
open System.Text
open System.Threading.Tasks

type ProviderPolicyAuthorityFloorRetentionResult =
    { Code: string
      Stored: ProviderPolicyAuthorityFloor }
    member this.IsRetained =
        this.Code = "policy-authority-floor-retained" || this.Code = "policy-authority-floor-unchanged"

[<RequireQualifiedAccess>]
module ProviderPolicyAuthorityFloorRecord =
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

    let encode
        (pin: ProviderPublisherTrustPolicyAuthorityId)
        generation
        (activeAuthority: ProviderPublisherTrustPolicyAuthorityId) =
        use output = new MemoryStream()
        writeString output "CBI60"
        writeString output (ProviderPublisherTrustPolicyAuthorityId.value pin)
        writeInt64 output generation
        writeString output (ProviderPublisherTrustPolicyAuthorityId.value activeAuthority)
        let record = output.ToArray()
        Array.append record (SHA256.HashData record)

    /// Answers the pin, generation, and active authority of a well-formed image, or nothing when the
    /// tag, structure, or values do not hold.
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
                    if readString () <> "CBI60" then None
                    else
                        let pin = readString () |> ProviderPublisherTrustPolicyAuthorityId.create
                        let generation = readInt64 ()
                        if generation < 0L then None
                        else
                            let active = readString () |> ProviderPublisherTrustPolicyAuthorityId.create
                            // Generation zero under any authority but the pin is not a floor an
                            // issuer produces, and it would refuse the empty checkpoint it is
                            // supposed to admit.
                            if offset <> record.Length || (generation = 0L && active <> pin) then None
                            else Some(pin, generation, active)
                with
                | :? InvalidDataException
                | :? ArgumentException
                | :? DecoderFallbackException -> None

/// Durable custody of the CBI38 authority floor. The integrity tag detects corruption and truncation;
/// it is not a defence against an adversary who can write this file, because such an adversary
/// recomputes the tag. Real custody is a separate privilege domain and is not implemented here.
type DurableProviderPolicyAuthorityFloorStore private (
    path: string,
    pin: ProviderPublisherTrustPolicyAuthorityId,
    initial: ProviderPolicyAuthorityFloor) =

    let syncRoot = obj ()
    let mutable stored = initial

    static let tryDelete path =
        try if File.Exists path then File.Delete path
        with :? IOException | :? UnauthorizedAccessException -> ()

    static let tryWrite path pin (floor: ProviderPolicyAuthorityFloor) =
        let temporary = path + ".tmp"
        try
            match Path.GetDirectoryName(path: string) with
            | null -> invalidArg (nameof path) "An authority floor path must have a parent directory."
            | parent -> Directory.CreateDirectory parent |> ignore
            let bytes =
                ProviderPolicyAuthorityFloorRecord.encode pin floor.Generation floor.ActiveAuthority
            if bytes.Length > ProviderPolicyAuthorityFloorRecord.MaxBytes then false
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

    static member Open(path: string, pin: ProviderPublisherTrustPolicyAuthorityId) =
        if String.IsNullOrWhiteSpace path then invalidArg (nameof path) "An authority floor path is required."
        if isNull (box pin) then nullArg (nameof pin)
        let fullPath = Path.GetFullPath path
        tryDelete (fullPath + ".tmp")
        if not (File.Exists fullPath) then
            // Generation zero names the pin itself, because that is the only authority floor an
            // unrotated checkpoint can satisfy.
            let empty = ProviderPolicyAuthorityFloor.Restore(0L, pin)
            if not (tryWrite fullPath pin empty) then "policy-authority-floor-write-failed", None
            else
                "policy-authority-floor-established",
                Some(DurableProviderPolicyAuthorityFloorStore(fullPath, pin, empty))
        else
            let read =
                try ProviderPolicyAuthorityFloorRecord.decode (File.ReadAllBytes fullPath)
                with :? IOException | :? UnauthorizedAccessException -> None
            match read with
            | None -> "policy-authority-floor-corrupt", None
            | Some(storedPin, _, _) when storedPin <> pin -> "policy-authority-floor-authority-mismatch", None
            | Some(_, generation, active) ->
                let floor = ProviderPolicyAuthorityFloor.Restore(generation, active)
                "policy-authority-floor-recovered",
                Some(DurableProviderPolicyAuthorityFloorStore(fullPath, pin, floor))

    member _.Retain(floor: ProviderPolicyAuthorityFloor) =
        if isNull (box floor) then nullArg (nameof floor)
        lock syncRoot (fun () ->
            if floor.Generation = 0L && floor.ActiveAuthority <> pin then
                { Code = "policy-authority-floor-authority-mismatch"; Stored = stored }
            elif floor.Generation = stored.Generation && floor.ActiveAuthority = stored.ActiveAuthority then
                { Code = "policy-authority-floor-unchanged"; Stored = stored }
            // An equal generation naming a different active authority is a fork rather than an
            // advance: the floor would stop recognising the chain it was retained from.
            elif floor.Generation <= stored.Generation then
                { Code = "policy-authority-floor-regressed"; Stored = stored }
            elif not (tryWrite path pin floor) then
                { Code = "policy-authority-floor-write-failed"; Stored = stored }
            else
                stored <- floor
                { Code = "policy-authority-floor-retained"; Stored = stored })

    /// A refused retention reaches CBI60's cycle as a failed handoff rather than being swallowed, so
    /// the cycle reports an advanced-but-unretained floor instead of claiming custody it does not
    /// have.
    member this.Sink: ProviderPolicyAuthorityFloorSink =
        fun floor cancellationToken ->
            cancellationToken.ThrowIfCancellationRequested()
            let result = this.Retain floor
            if not result.IsRetained then
                invalidOp $"The authority floor was not retained: {result.Code}."
            Task.CompletedTask

[<RequireQualifiedAccess>]
module ProviderPolicyAuthorityCustody =
    /// Opens the durable registry under both guards. CBI42 establishes its policy floor before the
    /// checkpoint exists, which is what lets a later absence mean the guard was removed; a guard
    /// introduced after those checkpoints already exist cannot use that ordering, so an absent
    /// authority floor is adopted at zero and reported as such rather than being refused or being
    /// reported as a recovery the host never made.
    let open' (checkpointPath: string) (floorPath: string) (authorityFloorPath: string) pin =
        if String.IsNullOrWhiteSpace checkpointPath then
            invalidArg (nameof checkpointPath) "A checkpoint path is required."
        if String.IsNullOrWhiteSpace floorPath then
            invalidArg (nameof floorPath) "A floor path is required."
        if String.IsNullOrWhiteSpace authorityFloorPath then
            invalidArg (nameof authorityFloorPath) "An authority floor path is required."
        let checkpointExists = File.Exists(Path.GetFullPath checkpointPath)
        if not (File.Exists(Path.GetFullPath floorPath)) && checkpointExists then
            "policy-floor-missing", None, None, None, None
        else
            let adopted = not (File.Exists(Path.GetFullPath authorityFloorPath)) && checkpointExists
            match DurableProviderPublisherTrustPolicyFloorStore.Open(floorPath, pin) with
            | code, None -> code, None, None, None, None
            | _, Some floors ->
                match DurableProviderPolicyAuthorityFloorStore.Open(authorityFloorPath, pin) with
                | code, None -> code, None, None, Some floors, None
                | _, Some authorityFloors ->
                    match DurableProviderPublisherTrustPolicyRegistry.Open(
                            checkpointPath, pin, Some floors.Stored, Some authorityFloors.Stored) with
                    | code, None, _, _ -> code, Some code, None, Some floors, Some authorityFloors
                    | code, Some registry, _, _ ->
                        (if adopted then "policy-authority-floor-adopted" else "policy-authority-floor-opened"),
                        Some code, Some registry, Some floors, Some authorityFloors
