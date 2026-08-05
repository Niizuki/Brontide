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

/// Guards the authority generation the retained chain reaches. It is deliberately separate from the
/// policy recovery floor: the two describe different monotone facts and have different custodians.
type ProviderPolicyAuthorityFloor =
    private
    | ProviderPolicyAuthorityFloor of int64 * ProviderPublisherTrustPolicyAuthorityId
    member this.Generation =
        let (ProviderPolicyAuthorityFloor(generation, _)) = this
        generation
    member this.ActiveAuthority =
        let (ProviderPolicyAuthorityFloor(_, authority)) = this
        authority

    static member internal Issue(generation, activeAuthority) =
        ProviderPolicyAuthorityFloor(generation, activeAuthority)

    static member Restore(generation, activeAuthority) =
        if generation < 0L then
            invalidArg (nameof generation) "A valid policy authority floor is required."
        ProviderPolicyAuthorityFloor(generation, activeAuthority)

type DurableProviderPublisherTrustPolicyUpdateResult =
    { Code: string
      Current: VerifiedProviderPublisherTrustPolicySnapshot option
      Floor: ProviderPublisherTrustPolicyRecoveryFloor }
    member this.IsApplied = this.Code = "policy-update-applied"

type DurableProviderPolicyAuthorityRotationResult =
    { Code: string
      Generation: int64
      ActiveAuthority: ProviderPublisherTrustPolicyAuthorityId
      Floor: ProviderPolicyAuthorityFloor }
    member this.IsApplied = this.Code = "policy-authority-rotation-applied"

/// One retained link: either a policy update or the authority rotation that decides who may sign the
/// updates after it. Both live in one record because recovery has to re-verify each update against the
/// authority in force at its own position, which only their shared order states.
type internal ProviderPolicyChainLink =
    | PolicyUpdateLink of ProviderPublisherTrustPolicyUpdate
    | AuthorityRotationLink of ProviderPolicyAuthorityRotationStatement

[<RequireQualifiedAccess>]
module private ProviderPublisherTrustCheckpointCodec =
    let maxBytes = 1024 * 1024
    let maxLinks = 4096
    let maxEntries = 4096
    let updateTag = 0
    let rotationTag = 1

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

    let private writeRotation memory (rotation: ProviderPolicyAuthorityRotationStatement) =
        writeInt64 memory rotation.Generation
        writeInt64 memory rotation.PolicySequence
        writeInt32 memory (if rotation.PolicyIdentity.IsSome then 1 else 0)
        rotation.PolicyIdentity |> Option.iter (ProviderPublisherTrustPolicyId.value >> writeString memory)
        writeString memory (ProviderPublisherTrustPolicyAuthorityId.value rotation.PreviousAuthority)
        writeString memory (ProviderPublisherTrustPolicyAuthorityId.value rotation.NextAuthority)
        writeString memory rotation.Algorithm
        writeString memory rotation.PreviousAuthorityPublicKeySpkiBase64
        writeString memory rotation.NextAuthorityPublicKeySpkiBase64
        writeString memory rotation.PreviousSignatureBase64
        writeString memory rotation.NextSignatureBase64

    /// Writes the CBI38 record while the chain holds only updates and the tagged CBI57 record once it
    /// holds a rotation, so a host that never rotates keeps a record shape earlier evidence pins and an
    /// existing checkpoint stays readable.
    let write path authority (links: ProviderPolicyChainLink list) =
        let temporary = path + ".tmp"
        let entriesOf link =
            match link with
            | PolicyUpdateLink update -> update.Policy.Entries.Length
            | AuthorityRotationLink _ -> 0
        try
            if links.Length > maxLinks || (links |> List.exists (fun link -> entriesOf link > maxEntries)) then
                false
            else
                let rotated = links |> List.exists (function AuthorityRotationLink _ -> true | _ -> false)
                match Path.GetDirectoryName path with
                | null -> invalidArg (nameof path) "A checkpoint path must have a parent directory."
                | parent -> Directory.CreateDirectory parent |> ignore
                use memory = new MemoryStream()
                writeString memory (if rotated then "CBI57" else "CBI38")
                writeString memory (ProviderPublisherTrustPolicyAuthorityId.value authority)
                writeInt32 memory links.Length
                for link in links do
                    if rotated then
                        writeInt32 memory (match link with PolicyUpdateLink _ -> updateTag | _ -> rotationTag)
                    match link with
                    | AuthorityRotationLink rotation -> writeRotation memory rotation
                    | PolicyUpdateLink update ->
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
                let format = string ()
                if format <> "CBI38" && format <> "CBI57" then Error "corrupt"
                else
                    let tagged = format = "CBI57"
                    let authority = string () |> ProviderPublisherTrustPolicyAuthorityId.create
                    let count = int32 ()
                    if count < 0 || count > maxLinks then Error "corrupt"
                    else
                        let links = ResizeArray<ProviderPolicyChainLink>()
                        for _ in 1..count do
                            let tag = if tagged then int32 () else updateTag
                            if tag <> updateTag && tag <> rotationTag then raise (InvalidDataException())
                            if tag = rotationTag then
                                let generation = int64 ()
                                let policySequence = int64 ()
                                let presence = int32 ()
                                if presence <> 0 && presence <> 1 then raise (InvalidDataException())
                                let policyIdentity =
                                    if presence = 1 then Some(string () |> ProviderPublisherTrustPolicyId.create) else None
                                let previousAuthority = string () |> ProviderPublisherTrustPolicyAuthorityId.create
                                let nextAuthority = string () |> ProviderPublisherTrustPolicyAuthorityId.create
                                links.Add(AuthorityRotationLink
                                    { Generation = generation; PolicySequence = policySequence
                                      PolicyIdentity = policyIdentity
                                      PreviousAuthority = previousAuthority; NextAuthority = nextAuthority
                                      Algorithm = string ()
                                      PreviousAuthorityPublicKeySpkiBase64 = string ()
                                      NextAuthorityPublicKeySpkiBase64 = string ()
                                      PreviousSignatureBase64 = string ()
                                      NextSignatureBase64 = string () })
                            else
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
                                links.Add(PolicyUpdateLink
                                    { Sequence = sequence; PreviousPolicyIdentity = previous
                                      Policy = { Identity = identity; Entries = entries }
                                      Algorithm = string (); AuthorityPublicKeySpkiBase64 = string (); SignatureBase64 = string () })
                        if offset <> bytes.Length then Error "corrupt" else Ok(authority, List.ofSeq links)
        with
        | :? IOException
        | :? UnauthorizedAccessException
        | :? InvalidDataException
        | :? ArgumentException
        | :? DecoderFallbackException -> Error "corrupt"

    let removeTemporary path = tryDelete (path + ".tmp")

[<RequireQualifiedAccess>]
module private ProviderPolicyChainReplay =
    /// Replays a retained chain in stored order, so each update is verified against the authority the
    /// preceding rotations put in force rather than against the pin alone.
    let run pin (stored: ProviderPolicyChainLink list) =
        let replayed = ProviderPublisherTrustPolicyRegistry pin
        let complete =
            stored
            |> List.forall (function
                | PolicyUpdateLink update -> (replayed.Apply update).IsApplied
                | AuthorityRotationLink rotation -> (replayed.Rotate rotation).IsApplied)
        replayed, complete

type DurableProviderPublisherTrustPolicyRegistry private (
    path: string,
    registry: ProviderPublisherTrustPolicyRegistry,
    initialLinks: ProviderPolicyChainLink list) =
    let syncRoot = obj ()
    let mutable links = initialLinks

    let floor () =
        let current = registry.Current
        ProviderPublisherTrustPolicyRecoveryFloor(
            registry.AuthorityIdentity,
            current |> Option.map _.Sequence |> Option.defaultValue 0L,
            current |> Option.map _.Policy.Identity)

    let authorityFloor () =
        ProviderPolicyAuthorityFloor.Issue(registry.AuthorityGeneration, registry.ActiveAuthorityIdentity)

    let replay = ProviderPolicyChainReplay.run

    member _.Current = registry.Current

    member _.AuthorityIdentity = registry.AuthorityIdentity

    member _.ActiveAuthorityIdentity = registry.ActiveAuthorityIdentity

    member _.AuthorityGeneration = registry.AuthorityGeneration

    member _.Floor = floor ()

    member _.AuthorityFloor = authorityFloor ()

    member _.Apply(update: ProviderPublisherTrustPolicyUpdate) =
        if isNull (box update) then nullArg (nameof update)
        lock syncRoot (fun () ->
            let shadow, _ = replay registry.AuthorityIdentity links
            let validation = shadow.Apply update
            if not validation.IsApplied then
                { Code = validation.Code; Current = registry.Current; Floor = floor () }
            else
                let normalizedPolicy = ProviderPublisherTrustEvaluator.tryValidate update.Policy |> Option.get
                let normalized = { update with Policy = normalizedPolicy }
                let successor = links @ [ PolicyUpdateLink normalized ]
                if not (ProviderPublisherTrustCheckpointCodec.write path registry.AuthorityIdentity successor) then
                    { Code = "policy-checkpoint-write-failed"; Current = registry.Current; Floor = floor () }
                else
                    let applied = registry.Apply normalized
                    if not applied.IsApplied then invalidOp "A published checkpoint must apply to its unchanged live registry."
                    links <- successor
                    { Code = applied.Code; Current = applied.Current; Floor = floor () })

    /// Publishes one authority transition and only then advances the live authority, in the order CBI38
    /// applies to a policy update: a live authority no checkpoint records would be forgotten by the next
    /// recovery, which is the direction that loses work rather than the one that repeats it.
    member _.Rotate(statement: ProviderPolicyAuthorityRotationStatement) =
        if isNull (box statement) then nullArg (nameof statement)
        lock syncRoot (fun () ->
            let shadow, _ = replay registry.AuthorityIdentity links
            let validation = shadow.Rotate statement
            if not validation.IsApplied then
                { Code = validation.Code
                  Generation = registry.AuthorityGeneration
                  ActiveAuthority = registry.ActiveAuthorityIdentity
                  Floor = authorityFloor () }
            else
                let successor = links @ [ AuthorityRotationLink statement ]
                if not (ProviderPublisherTrustCheckpointCodec.write path registry.AuthorityIdentity successor) then
                    { Code = "policy-checkpoint-write-failed"
                      Generation = registry.AuthorityGeneration
                      ActiveAuthority = registry.ActiveAuthorityIdentity
                      Floor = authorityFloor () }
                else
                    let applied = registry.Rotate statement
                    if not applied.IsApplied then invalidOp "A published rotation must apply to its unchanged live registry."
                    links <- successor
                    { Code = applied.Code
                      Generation = applied.Generation
                      ActiveAuthority = applied.ActiveAuthority
                      Floor = authorityFloor () })

    member _.Govern(acquirer: TrustedProviderArtifactAcquirer) = GovernedProviderArtifactAcquirer(registry, acquirer)

    static member Open(
        path: string,
        authority: ProviderPublisherTrustPolicyAuthorityId,
        recoveryFloor: ProviderPublisherTrustPolicyRecoveryFloor option) =
        let code, registry, floor, _ =
            DurableProviderPublisherTrustPolicyRegistry.Open(path, authority, recoveryFloor, None)
        code, registry, floor

    static member Open(
        path: string,
        authority: ProviderPublisherTrustPolicyAuthorityId,
        recoveryFloor: ProviderPublisherTrustPolicyRecoveryFloor option,
        authorityFloor: ProviderPolicyAuthorityFloor option) =
        if String.IsNullOrWhiteSpace path then invalidArg (nameof path) "A checkpoint path is required."
        let fullPath = Path.GetFullPath path
        match recoveryFloor with
        | Some value when value.AuthorityIdentity <> authority ->
            "policy-checkpoint-authority-mismatch", None, None, None
        | _ ->
            ProviderPublisherTrustCheckpointCodec.removeTemporary fullPath
            if not (File.Exists fullPath) then
                match recoveryFloor, authorityFloor with
                | Some value, _ when value.Sequence > 0L -> "policy-checkpoint-rollback-detected", None, None, None
                | _, Some value when value.Generation > 0L || value.ActiveAuthority <> authority ->
                    "policy-authority-rollback-detected", None, None, None
                | _ ->
                    let durable = DurableProviderPublisherTrustPolicyRegistry(fullPath, ProviderPublisherTrustPolicyRegistry authority, [])
                    "policy-checkpoint-empty", Some durable,
                    Some(ProviderPublisherTrustPolicyRecoveryFloor(authority, 0L, None)),
                    Some durable.AuthorityFloor
            else
                match ProviderPublisherTrustCheckpointCodec.read fullPath with
                | Error _ -> "policy-checkpoint-corrupt", None, None, None
                | Ok(storedAuthority, _) when storedAuthority <> authority ->
                    "policy-checkpoint-authority-mismatch", None, None, None
                | Ok(_, storedLinks) ->
                    let recovered, complete = ProviderPolicyChainReplay.run authority storedLinks
                    if not complete then "policy-checkpoint-invalid-chain", None, None, None
                    else
                        let rollback =
                            match recoveryFloor, recovered.Current with
                            | Some floor, None -> floor.Sequence > 0L
                            | Some floor, Some current ->
                                current.Sequence < floor.Sequence
                                || (current.Sequence = floor.Sequence && Some current.Policy.Identity <> floor.PolicyIdentity)
                            | None, _ -> false
                        let authorityRollback =
                            authorityFloor
                            |> Option.exists (fun value ->
                                recovered.AuthorityGeneration < value.Generation
                                || (recovered.AuthorityGeneration = value.Generation
                                    && recovered.ActiveAuthorityIdentity <> value.ActiveAuthority))
                        if rollback then "policy-checkpoint-rollback-detected", None, None, None
                        elif authorityRollback then "policy-authority-rollback-detected", None, None, None
                        else
                            let durable = DurableProviderPublisherTrustPolicyRegistry(fullPath, recovered, storedLinks)
                            let current = recovered.Current
                            "policy-checkpoint-recovered", Some durable,
                            Some(ProviderPublisherTrustPolicyRecoveryFloor(
                                authority,
                                current |> Option.map _.Sequence |> Option.defaultValue 0L,
                                current |> Option.map _.Policy.Identity)),
                            Some durable.AuthorityFloor
