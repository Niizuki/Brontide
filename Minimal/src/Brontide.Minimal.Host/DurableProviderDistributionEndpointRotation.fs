namespace Brontide.Minimal.Host

open System
open System.Buffers.Binary
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Threading

type ProviderDistributionEndpointRotationStatement =
    { Generation: int64
      PreviousEndpoint: ProviderPublisherTrustPolicyDistributionEndpointId
      NextEndpoint: ProviderPublisherTrustPolicyDistributionEndpointId
      Algorithm: string
      PreviousEndpointPublicKeySpkiBase64: string
      SignatureBase64: string }

type ProviderDistributionEndpointRotationSnapshot =
    { InitialEndpoint: ProviderPublisherTrustPolicyDistributionEndpointId
      Generation: int64
      ActiveEndpoint: ProviderPublisherTrustPolicyDistributionEndpointId
      StagedGeneration: int64 option
      StagedEndpoint: ProviderPublisherTrustPolicyDistributionEndpointId option }

[<Sealed>]
type ProviderDistributionEndpointRotationFloor private (
    generation: int64,
    activeEndpoint: ProviderPublisherTrustPolicyDistributionEndpointId) =
    member _.Generation = generation
    member _.ActiveEndpoint = activeEndpoint

    static member internal Issue(snapshot: ProviderDistributionEndpointRotationSnapshot) =
        ProviderDistributionEndpointRotationFloor(snapshot.Generation, snapshot.ActiveEndpoint)

    static member Restore(generation, activeEndpoint: ProviderPublisherTrustPolicyDistributionEndpointId) =
        if generation < 0L || isNull (box activeEndpoint) then
            invalidArg (nameof generation) "A valid endpoint-rotation floor is required."
        ProviderDistributionEndpointRotationFloor(generation, activeEndpoint)

type ProviderDistributionEndpointRotationResult =
    { Code: string
      Snapshot: ProviderDistributionEndpointRotationSnapshot
      Floor: ProviderDistributionEndpointRotationFloor
      DistributionCode: string }

[<RequireQualifiedAccess>]
module ProviderDistributionEndpointRotationManifest =
    let private appendInt64 (output: Stream) value =
        let bytes = Array.zeroCreate<byte> 8
        BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(), value)
        output.Write bytes

    let private appendString (output: Stream) (value: string) =
        let bytes = Encoding.UTF8.GetBytes value
        let length = Array.zeroCreate<byte> 4
        BinaryPrimitives.WriteInt32BigEndian(length.AsSpan(), bytes.Length)
        output.Write length
        output.Write bytes

    let encode generation previousEndpoint nextEndpoint =
        if generation <= 0L || isNull (box previousEndpoint) || isNull (box nextEndpoint) then
            invalidArg (nameof generation) "A valid endpoint rotation transition is required."
        use output = new MemoryStream()
        appendString output "CBI56"
        appendInt64 output generation
        previousEndpoint |> ProviderPublisherTrustPolicyDistributionEndpointId.value |> appendString output
        nextEndpoint |> ProviderPublisherTrustPolicyDistributionEndpointId.value |> appendString output
        output.ToArray()

type private DistributionEndpointRotationState =
    { Format: string
      InitialEndpoint: string
      Generation: int64
      ActiveEndpoint: string
      StagedGeneration: int64 option
      StagedEndpoint: string option }

[<RequireQualifiedAccess>]
module private DistributionEndpointRotationRecord =
    [<Literal>]
    let MaxBytes = 16384
    [<Literal>]
    let TagBytes = 32

    let private isDigest (value: string) =
        not (String.IsNullOrEmpty value) && value.Length = 64
        && value |> Seq.forall (fun character ->
            (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'))

    let isValid value =
        value.Format = "CBI56" && isDigest value.InitialEndpoint && isDigest value.ActiveEndpoint
        && value.Generation >= 0L
        && match value.StagedGeneration, value.StagedEndpoint with
           | None, None -> true
           | Some generation, Some endpoint ->
               generation = value.Generation + 1L && isDigest endpoint && endpoint <> value.ActiveEndpoint
           | _ -> false

    let project value =
        { InitialEndpoint = ProviderPublisherTrustPolicyDistributionEndpointId.create value.InitialEndpoint
          Generation = value.Generation
          ActiveEndpoint = ProviderPublisherTrustPolicyDistributionEndpointId.create value.ActiveEndpoint
          StagedGeneration = value.StagedGeneration
          StagedEndpoint = value.StagedEndpoint |> Option.map ProviderPublisherTrustPolicyDistributionEndpointId.create }

    let encode value =
        use output = new MemoryStream()
        use writer = new Utf8JsonWriter(output)
        writer.WriteStartObject()
        writer.WriteString("format", value.Format)
        writer.WriteString("initialEndpoint", value.InitialEndpoint)
        writer.WriteNumber("generation", value.Generation)
        writer.WriteString("activeEndpoint", value.ActiveEndpoint)
        match value.StagedGeneration, value.StagedEndpoint with
        | Some generation, Some endpoint ->
            writer.WriteNumber("stagedGeneration", generation)
            writer.WriteString("stagedEndpoint", endpoint)
        | _ ->
            writer.WriteNull("stagedGeneration")
            writer.WriteNull("stagedEndpoint")
        writer.WriteEndObject()
        writer.Flush()
        output.ToArray()

    let tryDelete path =
        try if File.Exists path then File.Delete path
        with :? IOException | :? UnauthorizedAccessException -> ()

    let tryWrite path value =
        let temporary = path + ".tmp"
        try
            if not (isValid value) then false
            else
                let record = encode value
                if record.Length + TagBytes > MaxBytes then false
                else
                    match Path.GetDirectoryName path with
                    | null -> false
                    | parent ->
                        Directory.CreateDirectory parent |> ignore
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

    let private text (element: JsonElement) =
        element.GetString() |> Option.ofObj |> Option.defaultValue ""

    let tryRead path =
        try
            let bytes = File.ReadAllBytes path
            if bytes.Length <= TagBytes || bytes.Length > MaxBytes then None
            else
                let record = bytes[.. bytes.Length - TagBytes - 1]
                let tag = ReadOnlySpan(bytes, bytes.Length - TagBytes, TagBytes)
                if not (CryptographicOperations.FixedTimeEquals(ReadOnlySpan(SHA256.HashData record), tag)) then None
                else
                    use document = JsonDocument.Parse record
                    let root = document.RootElement
                    let stagedGeneration =
                        let element = root.GetProperty "stagedGeneration"
                        if element.ValueKind = JsonValueKind.Null then None else Some(element.GetInt64())
                    let stagedEndpoint =
                        let element = root.GetProperty "stagedEndpoint"
                        if element.ValueKind = JsonValueKind.Null then None else Some(text element)
                    let value =
                        { Format = root.GetProperty("format") |> text
                          InitialEndpoint = root.GetProperty("initialEndpoint") |> text
                          Generation = root.GetProperty("generation").GetInt64()
                          ActiveEndpoint = root.GetProperty("activeEndpoint") |> text
                          StagedGeneration = stagedGeneration
                          StagedEndpoint = stagedEndpoint }
                    if isValid value then Some value else None
        with
        | :? IOException | :? UnauthorizedAccessException | :? JsonException
        | :? InvalidOperationException | :? Collections.Generic.KeyNotFoundException
        | :? FormatException | :? ArgumentException -> None

type DurableProviderDistributionEndpointRotation private (
    path: string,
    initialState: DistributionEndpointRotationState) =

    let gate = new SemaphoreSlim(1, 1)
    let mutable state = initialState

    let snapshot () = DistributionEndpointRotationRecord.project state
    let result code distributionCode projected =
        { Code = code
          Snapshot = projected
          Floor = ProviderDistributionEndpointRotationFloor.Issue projected
          DistributionCode = distributionCode }

    let verify (statement: ProviderDistributionEndpointRotationStatement) =
        if statement.Algorithm <> "ECDSA-P256-SHA256"
            || String.IsNullOrWhiteSpace statement.PreviousEndpointPublicKeySpkiBase64
            || String.IsNullOrWhiteSpace statement.SignatureBase64 then
            Some "endpoint-rotation-evidence-invalid"
        else
            try
                let publicKey = Convert.FromBase64String statement.PreviousEndpointPublicKeySpkiBase64
                let signature = Convert.FromBase64String statement.SignatureBase64
                let identity =
                    publicKey |> SHA256.HashData |> Convert.ToHexString
                    |> ProviderPublisherTrustPolicyDistributionEndpointId.create
                if identity <> statement.PreviousEndpoint then Some "endpoint-rotation-key-mismatch"
                else
                    use verifier = ECDsa.Create()
                    let mutable bytesRead = 0
                    verifier.ImportSubjectPublicKeyInfo(publicKey.AsSpan(), &bytesRead)
                    let curve = verifier.ExportParameters(false).Curve.Oid.Value |> Option.ofObj
                    if bytesRead <> publicKey.Length || verifier.KeySize <> 256
                        || curve <> Some "1.2.840.10045.3.1.7" then
                        Some "endpoint-rotation-evidence-invalid"
                    elif verifier.VerifyData(
                        ProviderDistributionEndpointRotationManifest.encode statement.Generation
                            statement.PreviousEndpoint statement.NextEndpoint,
                        signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence) then None
                    else Some "endpoint-rotation-signature-invalid"
            with
            | :? FormatException | :? CryptographicException
            | :? ArgumentException | :? NullReferenceException -> Some "endpoint-rotation-evidence-invalid"

    member _.Snapshot =
        gate.Wait()
        try snapshot ()
        finally gate.Release() |> ignore

    member this.Floor = ProviderDistributionEndpointRotationFloor.Issue this.Snapshot

    member _.Stage(statement: ProviderDistributionEndpointRotationStatement) =
        if isNull (box statement) then nullArg (nameof statement)
        gate.Wait()
        try
            let current = snapshot ()
            match state.StagedGeneration, state.StagedEndpoint with
            | Some generation, Some endpoint ->
                if generation = statement.Generation
                    && state.ActiveEndpoint = ProviderPublisherTrustPolicyDistributionEndpointId.value statement.PreviousEndpoint
                    && endpoint = ProviderPublisherTrustPolicyDistributionEndpointId.value statement.NextEndpoint then
                    result "endpoint-rotation-already-staged" "not-attempted" current
                else result "endpoint-rotation-successor-conflict" "not-attempted" current
            | _ when statement.Generation <> state.Generation + 1L ->
                result "endpoint-rotation-generation-invalid" "not-attempted" current
            | _ when ProviderPublisherTrustPolicyDistributionEndpointId.value statement.PreviousEndpoint <> state.ActiveEndpoint ->
                result "endpoint-rotation-predecessor-mismatch" "not-attempted" current
            | _ when statement.NextEndpoint = statement.PreviousEndpoint ->
                result "endpoint-rotation-self-refused" "not-attempted" current
            | _ ->
                match verify statement with
                | Some code -> result code "not-attempted" current
                | None ->
                    let next =
                        { state with
                            StagedGeneration = Some statement.Generation
                            StagedEndpoint = Some(ProviderPublisherTrustPolicyDistributionEndpointId.value statement.NextEndpoint) }
                    if not (DistributionEndpointRotationRecord.tryWrite path next) then
                        result "endpoint-rotation-write-failed" "not-attempted" current
                    else
                        state <- next
                        result "endpoint-rotation-staged" "not-attempted" (snapshot ())
        finally gate.Release() |> ignore

    member _.CreateCurrentClient(registry: DurableProviderPublisherTrustPolicyRegistry) =
        if isNull (box registry) then nullArg (nameof registry)
        gate.Wait()
        try
            ProviderPublisherTrustPolicyDistributionClient(
                registry, ProviderPublisherTrustPolicyDistributionEndpointId.create state.ActiveEndpoint)
        finally gate.Release() |> ignore

    member _.ConfirmAsync(
        registry: DurableProviderPublisherTrustPolicyRegistry,
        source: IProviderPublisherTrustPolicyDistributionSource,
        now: DateTimeOffset,
        timeout: TimeSpan,
        cancellationToken: CancellationToken) = task {
        if isNull (box registry) then nullArg (nameof registry)
        if isNull (box source) then nullArg (nameof source)
        do! gate.WaitAsync cancellationToken
        try
            let before = snapshot ()
            match state.StagedGeneration, state.StagedEndpoint with
            | Some generation, Some endpoint ->
                let client = ProviderPublisherTrustPolicyDistributionClient(
                    registry, ProviderPublisherTrustPolicyDistributionEndpointId.create endpoint)
                let! distribution = client.SynchronizeAsync(source, now, timeout, cancellationToken)
                if distribution.Code <> "policy-distribution-current"
                    && distribution.Code <> "policy-distribution-applied" then
                    return result "endpoint-rotation-confirmation-refused" distribution.Code before
                else
                    let next =
                        { state with
                            Generation = generation
                            ActiveEndpoint = endpoint
                            StagedGeneration = None
                            StagedEndpoint = None }
                    if not (DistributionEndpointRotationRecord.tryWrite path next) then
                        return result "endpoint-rotation-write-failed" distribution.Code before
                    else
                        state <- next
                        return result "endpoint-rotation-applied" distribution.Code (snapshot ())
            | _ -> return result "endpoint-rotation-not-staged" "not-attempted" before
        finally gate.Release() |> ignore
    }

    static member Open(
        path: string,
        initialEndpoint: ProviderPublisherTrustPolicyDistributionEndpointId,
        floor: ProviderDistributionEndpointRotationFloor option) =
        if String.IsNullOrWhiteSpace path then invalidArg (nameof path) "An endpoint-rotation path is required."
        if isNull (box initialEndpoint) then invalidArg (nameof initialEndpoint) "An initial endpoint identity is required."
        let fullPath = Path.GetFullPath path
        DistributionEndpointRotationRecord.tryDelete (fullPath + ".tmp")
        let opened code value =
            let rotation = DurableProviderDistributionEndpointRotation(fullPath, value)
            let projected = rotation.Snapshot
            { Code = code; Rotation = Some rotation; Snapshot = Some projected
              Floor = Some(ProviderDistributionEndpointRotationFloor.Issue projected) }
        if not (File.Exists fullPath) then
            if floor |> Option.exists (fun value -> value.Generation > 0L || value.ActiveEndpoint <> initialEndpoint) then
                { Code = "endpoint-rotation-rollback-detected"; Rotation = None; Snapshot = None; Floor = None }
            else
                let initial =
                    { Format = "CBI56"
                      InitialEndpoint = ProviderPublisherTrustPolicyDistributionEndpointId.value initialEndpoint
                      Generation = 0L
                      ActiveEndpoint = ProviderPublisherTrustPolicyDistributionEndpointId.value initialEndpoint
                      StagedGeneration = None
                      StagedEndpoint = None }
                if DistributionEndpointRotationRecord.tryWrite fullPath initial then
                    opened "endpoint-rotation-established" initial
                else { Code = "endpoint-rotation-write-failed"; Rotation = None; Snapshot = None; Floor = None }
        else
            match DistributionEndpointRotationRecord.tryRead fullPath with
            | None -> { Code = "endpoint-rotation-corrupt"; Rotation = None; Snapshot = None; Floor = None }
            | Some stored when stored.InitialEndpoint <> ProviderPublisherTrustPolicyDistributionEndpointId.value initialEndpoint ->
                { Code = "endpoint-rotation-initial-mismatch"; Rotation = None
                  Snapshot = Some(DistributionEndpointRotationRecord.project stored); Floor = None }
            | Some stored when floor |> Option.exists (fun value ->
                stored.Generation < value.Generation
                || stored.Generation = value.Generation
                   && stored.ActiveEndpoint <> ProviderPublisherTrustPolicyDistributionEndpointId.value value.ActiveEndpoint) ->
                { Code = "endpoint-rotation-rollback-detected"; Rotation = None
                  Snapshot = Some(DistributionEndpointRotationRecord.project stored); Floor = None }
            | Some stored -> opened "endpoint-rotation-recovered" stored

and ProviderDistributionEndpointRotationOpenResult =
    { Code: string
      Rotation: DurableProviderDistributionEndpointRotation option
      Snapshot: ProviderDistributionEndpointRotationSnapshot option
      Floor: ProviderDistributionEndpointRotationFloor option }
