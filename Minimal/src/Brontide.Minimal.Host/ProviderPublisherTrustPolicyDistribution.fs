namespace Brontide.Minimal.Host

open System
open System.Buffers.Binary
open System.IO
open System.Security.Cryptography
open System.Text
open System.Threading
open System.Threading.Tasks

[<StructuralEquality; StructuralComparison>]
type ProviderPublisherTrustPolicyDistributionEndpointId =
    private ProviderPublisherTrustPolicyDistributionEndpointId of string

[<RequireQualifiedAccess>]
module ProviderPublisherTrustPolicyDistributionEndpointId =
    let create value =
        let valid =
            not (String.IsNullOrEmpty value) && value.Length = 64
            && value |> Seq.forall (fun character ->
                (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'))
        if not valid then invalidArg (nameof value) "A distribution endpoint identity must be an uppercase SHA-256 digest."
        ProviderPublisherTrustPolicyDistributionEndpointId value
    let value (ProviderPublisherTrustPolicyDistributionEndpointId value) = value

type ProviderPublisherTrustPolicyDistributionRequest =
    { Challenge: string
      CurrentSequence: int64
      CurrentPolicyIdentity: ProviderPublisherTrustPolicyId option }

type ProviderPublisherTrustPolicyDistributionResponse =
    { Challenge: string
      CurrentSequence: int64
      CurrentPolicyIdentity: ProviderPublisherTrustPolicyId option
      IssuedAtUnixSeconds: int64
      ExpiresAtUnixSeconds: int64
      Update: ProviderPublisherTrustPolicyUpdate option
      Algorithm: string
      EndpointPublicKeySpkiBase64: string
      SignatureBase64: string }

type IProviderPublisherTrustPolicyDistributionSource =
    abstract FetchAsync:
        ProviderPublisherTrustPolicyDistributionRequest * CancellationToken ->
            Task<ProviderPublisherTrustPolicyDistributionResponse>

type ProviderPublisherTrustPolicyDistributionResult =
    { Code: string
      Current: VerifiedProviderPublisherTrustPolicySnapshot option
      Floor: ProviderPublisherTrustPolicyRecoveryFloor }
    member this.IsApplied = this.Code = "policy-distribution-applied"

[<RequireQualifiedAccess>]
module ProviderPublisherTrustPolicyDistributionManifest =
    let private appendInt32 (output: Stream) value =
        let bytes = Array.zeroCreate<byte> 4
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(), value)
        output.Write bytes
    let private appendInt64 (output: Stream) value =
        let bytes = Array.zeroCreate<byte> 8
        BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(), value)
        output.Write bytes
    let private appendString output (value: string) =
        let bytes = Encoding.UTF8.GetBytes value
        appendInt32 output bytes.Length
        output.Write bytes

    let updateDigest (update: ProviderPublisherTrustPolicyUpdate) =
        if isNull (box update) then nullArg (nameof update)
        use output = new MemoryStream()
        appendString output "CBI39-UPDATE"
        appendInt64 output update.Sequence
        appendInt32 output (if update.PreviousPolicyIdentity.IsSome then 1 else 0)
        update.PreviousPolicyIdentity |> Option.iter (ProviderPublisherTrustPolicyId.value >> appendString output)
        appendString output (ProviderPublisherTrustPolicyId.value update.Policy.Identity)
        let entries = update.Policy.Entries |> List.sortBy (fun entry -> ProviderPublisherKeyId.value entry.PublisherKeyId)
        appendInt32 output entries.Length
        for entry in entries do
            appendString output (ProviderPublisherKeyId.value entry.PublisherKeyId)
            appendString output (if entry.Disposition = Admitted then "admitted" else "revoked")
        appendString output update.Algorithm
        appendString output update.AuthorityPublicKeySpkiBase64
        appendString output update.SignatureBase64
        output.ToArray() |> SHA256.HashData |> Convert.ToHexString

    let encode challenge currentSequence currentPolicyIdentity issuedAt expiresAt update =
        use output = new MemoryStream()
        appendString output "CBI39"
        appendString output challenge
        appendInt64 output currentSequence
        appendInt32 output (if Option.isSome currentPolicyIdentity then 1 else 0)
        currentPolicyIdentity |> Option.iter (ProviderPublisherTrustPolicyId.value >> appendString output)
        appendInt64 output issuedAt
        appendInt64 output expiresAt
        appendInt32 output (if Option.isSome update then 1 else 0)
        update |> Option.iter (updateDigest >> appendString output)
        output.ToArray()

type ProviderPublisherTrustPolicyDistributionClient(
    registry: DurableProviderPublisherTrustPolicyRegistry,
    endpointIdentity: ProviderPublisherTrustPolicyDistributionEndpointId) =

    do
        if isNull (box registry) then nullArg (nameof registry)
        if isNull (box endpointIdentity) then invalidArg (nameof endpointIdentity) "An endpoint identity is required."

    let maximumCharacters = 1024L * 1024L
    let maximumValidity = TimeSpan.FromMinutes 15.0
    let maximumFutureSkew = TimeSpan.FromSeconds 30.0

    let result code = { Code = code; Current = registry.Current; Floor = registry.Floor }

    let cursorMatches (request: ProviderPublisherTrustPolicyDistributionRequest) =
        match registry.Current with
        | None -> request.CurrentSequence = 0L && request.CurrentPolicyIdentity.IsNone
        | Some current ->
            request.CurrentSequence = current.Sequence
            && request.CurrentPolicyIdentity = Some current.Policy.Identity

    let bounded (response: ProviderPublisherTrustPolicyDistributionResponse) =
        if isNull (box response) || response.Algorithm <> "ECDSA-P256-SHA256" || response.Challenge.Length <> 64
            || response.Challenge |> Seq.exists (Uri.IsHexDigit >> not) then false
        else
            match response.Update with
            | Some update when isNull (box update.Policy) || update.Policy.Entries.Length > 4096
                -> false
            | update ->
                let mutable characters =
                    int64 response.Challenge.Length + int64 response.Algorithm.Length
                    + int64 response.EndpointPublicKeySpkiBase64.Length + int64 response.SignatureBase64.Length
                match update with
                | Some value ->
                    characters <- characters + int64 (
                        value.Algorithm.Length + value.AuthorityPublicKeySpkiBase64.Length + value.SignatureBase64.Length)
                    characters <- characters + (value.Policy.Entries |> List.sumBy (fun entry ->
                        int64 (ProviderPublisherKeyId.value entry.PublisherKeyId).Length + 8L))
                | None -> ()
                characters <= maximumCharacters

    member _.SynchronizeAsync(
        source: IProviderPublisherTrustPolicyDistributionSource,
        now: DateTimeOffset,
        timeout: TimeSpan,
        cancellationToken: CancellationToken) = task {
        if isNull (box source) then nullArg (nameof source)
        if timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes 1.0 then
            invalidArg (nameof timeout) "The timeout must be positive and no greater than one minute."
        let current = registry.Current
        let request =
            { Challenge = RandomNumberGenerator.GetBytes 32 |> Convert.ToHexString
              CurrentSequence = current |> Option.map _.Sequence |> Option.defaultValue 0L
              CurrentPolicyIdentity = current |> Option.map _.Policy.Identity }
        use timeoutSource = CancellationTokenSource.CreateLinkedTokenSource cancellationToken
        timeoutSource.CancelAfter timeout
        let! fetched = task {
            try
                let! response = source.FetchAsync(request, timeoutSource.Token).WaitAsync(timeout, cancellationToken)
                return Ok response
            with
            | :? TimeoutException -> return Error "policy-distribution-timeout"
            | :? OperationCanceledException when cancellationToken.IsCancellationRequested ->
                return Error "policy-distribution-canceled"
            | :? OperationCanceledException -> return Error "policy-distribution-timeout"
            | error when not (error :? OutOfMemoryException) ->
                return Error "policy-distribution-transport-failed"
        }
        match fetched with
        | Error code -> return result code
        | Ok response when not (bounded response) -> return result "policy-distribution-response-invalid"
        | Ok response ->
            let verification =
                try
                    let publicKey = Convert.FromBase64String response.EndpointPublicKeySpkiBase64
                    let signature = Convert.FromBase64String response.SignatureBase64
                    let suppliedEndpoint =
                        publicKey |> SHA256.HashData |> Convert.ToHexString
                        |> ProviderPublisherTrustPolicyDistributionEndpointId.create
                    if suppliedEndpoint <> endpointIdentity then Error "policy-distribution-endpoint-mismatch"
                    else
                        use verifier = ECDsa.Create()
                        let mutable bytesRead = 0
                        verifier.ImportSubjectPublicKeyInfo(publicKey.AsSpan(), &bytesRead)
                        let curve = verifier.ExportParameters(false).Curve.Oid.Value |> Option.ofObj
                        if bytesRead <> publicKey.Length || verifier.KeySize <> 256
                            || curve <> Some "1.2.840.10045.3.1.7" then
                            Error "policy-distribution-response-invalid"
                        elif verifier.VerifyData(
                            ProviderPublisherTrustPolicyDistributionManifest.encode
                                response.Challenge response.CurrentSequence response.CurrentPolicyIdentity
                                response.IssuedAtUnixSeconds response.ExpiresAtUnixSeconds response.Update,
                            signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence) then Ok ()
                        else Error "policy-distribution-signature-invalid"
                with
                | :? FormatException
                | :? CryptographicException
                | :? ArgumentException
                | :? NullReferenceException -> Error "policy-distribution-response-invalid"
            match verification with
            | Error code -> return result code
            | Ok () when response.Challenge <> request.Challenge ->
                return result "policy-distribution-challenge-mismatch"
            | Ok () when response.CurrentSequence <> request.CurrentSequence
                || response.CurrentPolicyIdentity <> request.CurrentPolicyIdentity ->
                return result "policy-distribution-cursor-mismatch"
            | Ok () ->
                let freshness =
                    try
                        let issued = DateTimeOffset.FromUnixTimeSeconds response.IssuedAtUnixSeconds
                        let expires = DateTimeOffset.FromUnixTimeSeconds response.ExpiresAtUnixSeconds
                        (issued <= now || issued - now <= maximumFutureSkew) && expires > now && expires > issued
                        && expires - issued <= maximumValidity
                    with :? ArgumentOutOfRangeException -> false
                if not freshness then return result "policy-distribution-stale"
                elif not (cursorMatches request) then return result "policy-distribution-superseded"
                else
                    match response.Update with
                    | None -> return result "policy-distribution-current"
                    | Some update ->
                        let applied = registry.Apply update
                        return { Code = if applied.IsApplied then "policy-distribution-applied" else applied.Code
                                 Current = applied.Current; Floor = applied.Floor }
    }
