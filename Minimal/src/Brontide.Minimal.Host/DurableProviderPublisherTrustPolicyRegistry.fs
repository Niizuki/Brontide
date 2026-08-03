namespace Brontide.Minimal.Host

open System
open System.Buffers.Binary
open System.IO
open System.Text

type ProviderPublisherTrustPolicyRecoveryFloor =
    private
    | ProviderPublisherTrustPolicyRecoveryFloor of
        ProviderPublisherTrustPolicyAuthorityId * int64 * ProviderPublisherTrustPolicyId option
    member this.AuthorityIdentity =
        let (ProviderPublisherTrustPolicyRecoveryFloor(authority, _, _)) = this
        authority
    member this.Sequence =
        let (ProviderPublisherTrustPolicyRecoveryFloor(_, sequence, _)) = this
        sequence
    member this.PolicyIdentity =
        let (ProviderPublisherTrustPolicyRecoveryFloor(_, _, policy)) = this
        policy

    /// Rebuilds a floor from a durable record. A stored floor names a policy identity without the
    /// policy behind it, so it cannot be reissued the way a live one is, and this stays internal so
    /// a floor remains something issued rather than something a caller can make.
    static member internal Restore(authority, sequence, policyIdentity) =
        ProviderPublisherTrustPolicyRecoveryFloor(authority, sequence, policyIdentity)

type DurableProviderPublisherTrustPolicyUpdateResult =
    { Code: string
      Current: VerifiedProviderPublisherTrustPolicySnapshot option
      Floor: ProviderPublisherTrustPolicyRecoveryFloor }
    member this.IsApplied = this.Code = "policy-update-applied"

[<RequireQualifiedAccess>]
module private ProviderPublisherTrustCheckpointCodec =
    let maxBytes = 1024 * 1024
    let maxUpdates = 4096
    let maxEntries = 4096

    let private writeInt32 (output: Stream) value =
        let bytes = Array.zeroCreate<byte> 4
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(), value)
        output.Write bytes
    let private writeInt64 (output: Stream) value =
        let bytes = Array.zeroCreate<byte> 8
        BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(), value)
        output.Write bytes
    let private writeString output (value: string) =
        let bytes = Encoding.UTF8.GetBytes value
        writeInt32 output bytes.Length
        output.Write bytes

    let private tryDelete path =
        try if File.Exists path then File.Delete path
        with :? IOException | :? UnauthorizedAccessException -> ()

    let write path authority (updates: ProviderPublisherTrustPolicyUpdate list) =
        let temporary = path + ".tmp"
        try
            if updates.Length > maxUpdates || (updates |> List.exists (fun update -> update.Policy.Entries.Length > maxEntries)) then
                false
            else
                match Path.GetDirectoryName path with
                | null -> invalidArg (nameof path) "A checkpoint path must have a parent directory."
                | parent -> Directory.CreateDirectory parent |> ignore
                use memory = new MemoryStream()
                writeString memory "CBI38"
                writeString memory (ProviderPublisherTrustPolicyAuthorityId.value authority)
                writeInt32 memory updates.Length
                for update in updates do
                    writeInt64 memory update.Sequence
                    writeInt32 memory (if update.PreviousPolicyIdentity.IsSome then 1 else 0)
                    update.PreviousPolicyIdentity |> Option.iter (ProviderPublisherTrustPolicyId.value >> writeString memory)
                    writeString memory (ProviderPublisherTrustPolicyId.value update.Policy.Identity)
                    let entries = update.Policy.Entries |> List.sortBy (fun entry -> ProviderPublisherKeyId.value entry.PublisherKeyId)
                    writeInt32 memory entries.Length
                    for entry in entries do
                        writeString memory (ProviderPublisherKeyId.value entry.PublisherKeyId)
                        writeString memory (if entry.Disposition = Admitted then "admitted" else "revoked")
                    writeString memory update.Algorithm
                    writeString memory update.AuthorityPublicKeySpkiBase64
                    writeString memory update.SignatureBase64
                if memory.Length > int64 maxBytes then false
                else
                    use output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)
                    memory.Position <- 0L
                    memory.CopyTo output
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

    let read path =
        try
            let bytes = File.ReadAllBytes path
            if bytes.Length = 0 || bytes.Length > maxBytes then Error "corrupt"
            else
                let mutable offset = 0
                let ensure length = if length > bytes.Length - offset then raise (InvalidDataException())
                let int32 () =
                    ensure 4
                    let value = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4))
                    offset <- offset + 4
                    value
                let int64 () =
                    ensure 8
                    let value = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(offset, 8))
                    offset <- offset + 8
                    value
                let string () =
                    let length = int32 ()
                    if length < 0 || length > maxBytes then raise (InvalidDataException())
                    ensure length
                    let value = UTF8Encoding(false, true).GetString(bytes, offset, length)
                    offset <- offset + length
                    value
                if string () <> "CBI38" then Error "corrupt"
                else
                    let authority = string () |> ProviderPublisherTrustPolicyAuthorityId.create
                    let count = int32 ()
                    if count < 0 || count > maxUpdates then Error "corrupt"
                    else
                        let updates = ResizeArray<ProviderPublisherTrustPolicyUpdate>()
                        for _ in 1..count do
                            let sequence = int64 ()
                            let presence = int32 ()
                            if presence <> 0 && presence <> 1 then raise (InvalidDataException())
                            let previous = if presence = 1 then Some(string () |> ProviderPublisherTrustPolicyId.create) else None
                            let identity = string () |> ProviderPublisherTrustPolicyId.create
                            let entryCount = int32 ()
                            if entryCount < 0 || entryCount > maxEntries then raise (InvalidDataException())
                            let entries =
                                [ for _ in 1..entryCount do
                                    let key = string () |> ProviderPublisherKeyId.create
                                    let disposition = match string () with "admitted" -> Admitted | "revoked" -> Revoked | _ -> raise (InvalidDataException())
                                    { PublisherKeyId = key; Disposition = disposition } ]
                            updates.Add
                                { Sequence = sequence; PreviousPolicyIdentity = previous
                                  Policy = { Identity = identity; Entries = entries }
                                  Algorithm = string (); AuthorityPublicKeySpkiBase64 = string (); SignatureBase64 = string () }
                        if offset <> bytes.Length then Error "corrupt" else Ok(authority, List.ofSeq updates)
        with
        | :? IOException
        | :? UnauthorizedAccessException
        | :? InvalidDataException
        | :? ArgumentException
        | :? DecoderFallbackException -> Error "corrupt"

    let removeTemporary path = tryDelete (path + ".tmp")

type DurableProviderPublisherTrustPolicyRegistry private (
    path: string,
    registry: ProviderPublisherTrustPolicyRegistry,
    initialUpdates: ProviderPublisherTrustPolicyUpdate list) =
    let syncRoot = obj ()
    let mutable updates = initialUpdates

    let floor () =
        let current = registry.Current
        ProviderPublisherTrustPolicyRecoveryFloor(
            registry.AuthorityIdentity,
            current |> Option.map _.Sequence |> Option.defaultValue 0L,
            current |> Option.map _.Policy.Identity)

    member _.Current = registry.Current

    member _.Floor = floor ()

    member _.Apply(update: ProviderPublisherTrustPolicyUpdate) =
        if isNull (box update) then nullArg (nameof update)
        lock syncRoot (fun () ->
            let shadow = ProviderPublisherTrustPolicyRegistry registry.AuthorityIdentity
            updates |> List.iter (shadow.Apply >> ignore)
            let validation = shadow.Apply update
            if not validation.IsApplied then
                { Code = validation.Code; Current = registry.Current; Floor = floor () }
            else
                let normalizedPolicy = ProviderPublisherTrustEvaluator.tryValidate update.Policy |> Option.get
                let normalized = { update with Policy = normalizedPolicy }
                let successor = updates @ [ normalized ]
                if not (ProviderPublisherTrustCheckpointCodec.write path registry.AuthorityIdentity successor) then
                    { Code = "policy-checkpoint-write-failed"; Current = registry.Current; Floor = floor () }
                else
                    let applied = registry.Apply normalized
                    if not applied.IsApplied then invalidOp "A published checkpoint must apply to its unchanged live registry."
                    updates <- successor
                    { Code = applied.Code; Current = applied.Current; Floor = floor () })

    member _.Govern(acquirer: TrustedProviderArtifactAcquirer) = GovernedProviderArtifactAcquirer(registry, acquirer)

    static member Open(
        path: string,
        authority: ProviderPublisherTrustPolicyAuthorityId,
        recoveryFloor: ProviderPublisherTrustPolicyRecoveryFloor option) =
        if String.IsNullOrWhiteSpace path then invalidArg (nameof path) "A checkpoint path is required."
        let fullPath = Path.GetFullPath path
        match recoveryFloor with
        | Some value when value.AuthorityIdentity <> authority ->
            "policy-checkpoint-authority-mismatch", None, None
        | _ ->
            ProviderPublisherTrustCheckpointCodec.removeTemporary fullPath
            if not (File.Exists fullPath) then
                match recoveryFloor with
                | Some value when value.Sequence > 0L -> "policy-checkpoint-rollback-detected", None, None
                | _ ->
                    let durable = DurableProviderPublisherTrustPolicyRegistry(fullPath, ProviderPublisherTrustPolicyRegistry authority, [])
                    "policy-checkpoint-empty", Some durable, Some(durable |> fun value ->
                        ProviderPublisherTrustPolicyRecoveryFloor(authority, 0L, None))
            else
                match ProviderPublisherTrustCheckpointCodec.read fullPath with
                | Error _ -> "policy-checkpoint-corrupt", None, None
                | Ok(storedAuthority, _) when storedAuthority <> authority -> "policy-checkpoint-authority-mismatch", None, None
                | Ok(_, storedUpdates) ->
                    let recovered = ProviderPublisherTrustPolicyRegistry authority
                    let valid = storedUpdates |> List.forall (fun update -> (recovered.Apply update).IsApplied)
                    if not valid then "policy-checkpoint-invalid-chain", None, None
                    else
                        let rollback =
                            match recoveryFloor, recovered.Current with
                            | Some floor, None -> floor.Sequence > 0L
                            | Some floor, Some current ->
                                current.Sequence < floor.Sequence
                                || (current.Sequence = floor.Sequence && Some current.Policy.Identity <> floor.PolicyIdentity)
                            | None, _ -> false
                        if rollback then "policy-checkpoint-rollback-detected", None, None
                        else
                            let durable = DurableProviderPublisherTrustPolicyRegistry(fullPath, recovered, storedUpdates)
                            "policy-checkpoint-recovered", Some durable, Some(durable |> fun _ ->
                                let current = recovered.Current
                                ProviderPublisherTrustPolicyRecoveryFloor(
                                    authority,
                                    current |> Option.map _.Sequence |> Option.defaultValue 0L,
                                    current |> Option.map _.Policy.Identity))
