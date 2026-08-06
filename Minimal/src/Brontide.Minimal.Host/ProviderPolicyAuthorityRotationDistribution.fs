namespace Brontide.Minimal.Host

open System
open System.Buffers.Binary
open System.IO
open System.Security.Cryptography
open System.Text
open System.Threading
open System.Threading.Tasks

type ProviderPolicyAuthorityRotationDistributionRequest =
    { Challenge: string
      PolicySequence: int64
      PolicyIdentity: ProviderPublisherTrustPolicyId option
      AuthorityGeneration: int64
      ActiveAuthority: ProviderPublisherTrustPolicyAuthorityId }

type ProviderPolicyAuthorityRotationDistributionResponse =
    { Challenge: string
      PolicySequence: int64
      PolicyIdentity: ProviderPublisherTrustPolicyId option
      AuthorityGeneration: int64
      ActiveAuthority: ProviderPublisherTrustPolicyAuthorityId
      IssuedAtUnixSeconds: int64
      ExpiresAtUnixSeconds: int64
      Rotation: ProviderPolicyAuthorityRotationStatement option
      Algorithm: string
      EndpointPublicKeySpkiBase64: string
      SignatureBase64: string }

type IProviderPolicyAuthorityRotationDistributionSource =
    abstract FetchAsync:
        ProviderPolicyAuthorityRotationDistributionRequest * CancellationToken ->
            Task<ProviderPolicyAuthorityRotationDistributionResponse>

type ProviderPolicyAuthorityRotationDistributionResult =
    { Code: string
      Generation: int64
      ActiveAuthority: ProviderPublisherTrustPolicyAuthorityId
      Floor: ProviderPolicyAuthorityFloor }
    member this.IsApplied = this.Code = "policy-authority-distribution-applied"

[<RequireQualifiedAccess>]
module ProviderPolicyAuthorityRotationDistributionManifest =
    let private appendInt32 (output: Stream) value =
        let bytes = Array.zeroCreate<byte> 4
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(), value)
        output.Write bytes
    let private appendInt64 (output: Stream) value =
        let bytes = Array.zeroCreate<byte> 8
        BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(), value)
        output.Write bytes
    let private appendString output (value: string) =
        let bytes = UTF8Encoding(false, true).GetBytes value
        appendInt32 output bytes.Length
        output.Write bytes
    let private appendOptional output identity =
        appendInt32 output (if Option.isSome identity then 1 else 0)
        identity |> Option.iter (ProviderPublisherTrustPolicyId.value >> appendString output)

    let rotationDigest (rotation: ProviderPolicyAuthorityRotationStatement) =
        if isNull (box rotation) then nullArg (nameof rotation)
        use output = new MemoryStream()
        appendString output "CBI58-ROTATION"
        appendInt64 output rotation.Generation
        appendInt64 output rotation.PolicySequence
        appendOptional output rotation.PolicyIdentity
        appendString output (ProviderPublisherTrustPolicyAuthorityId.value rotation.PreviousAuthority)
        appendString output (ProviderPublisherTrustPolicyAuthorityId.value rotation.NextAuthority)
        appendString output rotation.Algorithm
        appendString output rotation.PreviousAuthorityPublicKeySpkiBase64
        appendString output rotation.NextAuthorityPublicKeySpkiBase64
        appendString output rotation.PreviousSignatureBase64
        appendString output rotation.NextSignatureBase64
        output.ToArray() |> SHA256.HashData |> Convert.ToHexString

    let encode (response: ProviderPolicyAuthorityRotationDistributionResponse) =
        if isNull (box response) then nullArg (nameof response)
        use output = new MemoryStream()
        appendString output "CBI58"
        appendString output response.Challenge
        appendInt64 output response.PolicySequence
        appendOptional output response.PolicyIdentity
        appendInt64 output response.AuthorityGeneration
        appendString output (ProviderPublisherTrustPolicyAuthorityId.value response.ActiveAuthority)
        appendInt64 output response.IssuedAtUnixSeconds
        appendInt64 output response.ExpiresAtUnixSeconds
        appendInt32 output (if Option.isSome response.Rotation then 1 else 0)
        response.Rotation |> Option.iter (rotationDigest >> appendString output)
        output.ToArray()

type ProviderPolicyAuthorityRotationDistributionClient(
    registry: DurableProviderPublisherTrustPolicyRegistry,
    endpointIdentity: ProviderPublisherTrustPolicyDistributionEndpointId) =

    do
        if isNull (box registry) then nullArg (nameof registry)
        if isNull (box endpointIdentity) then invalidArg (nameof endpointIdentity) "An endpoint identity is required."

    let maximumCharacters = 1024L * 1024L
    let maximumValidity = TimeSpan.FromMinutes 15.0
    let maximumFutureSkew = TimeSpan.FromSeconds 30.0

    let result code =
        { Code = code
          Generation = registry.AuthorityGeneration
          ActiveAuthority = registry.ActiveAuthorityIdentity
          Floor = registry.AuthorityFloor }

    let cursorMatches (request: ProviderPolicyAuthorityRotationDistributionRequest) =
        let current = registry.Current
        request.PolicySequence = (current |> Option.map _.Sequence |> Option.defaultValue 0L)
        && request.PolicyIdentity = (current |> Option.map _.Policy.Identity)
        && request.AuthorityGeneration = registry.AuthorityGeneration
        && request.ActiveAuthority = registry.ActiveAuthorityIdentity

    let bounded (response: ProviderPolicyAuthorityRotationDistributionResponse) =
        if isNull (box response) || String.IsNullOrEmpty response.Challenge || response.Algorithm <> "ECDSA-P256-SHA256"
            || String.IsNullOrEmpty response.EndpointPublicKeySpkiBase64 || String.IsNullOrEmpty response.SignatureBase64
            || isNull (box response.ActiveAuthority)
            || response.Challenge.Length <> 64 || response.Challenge |> Seq.exists (Uri.IsHexDigit >> not) then false
        else
            let mutable count = int64 (response.Challenge.Length + response.Algorithm.Length
                + response.EndpointPublicKeySpkiBase64.Length + response.SignatureBase64.Length
                + (ProviderPublisherTrustPolicyAuthorityId.value response.ActiveAuthority).Length)
            match response.Rotation with
            | Some rotation when String.IsNullOrEmpty rotation.Algorithm
                || String.IsNullOrEmpty rotation.PreviousAuthorityPublicKeySpkiBase64
                || String.IsNullOrEmpty rotation.NextAuthorityPublicKeySpkiBase64
                || String.IsNullOrEmpty rotation.PreviousSignatureBase64
                || String.IsNullOrEmpty rotation.NextSignatureBase64 -> false
            | Some rotation ->
                count <- count + int64 (rotation.Algorithm.Length + rotation.PreviousAuthorityPublicKeySpkiBase64.Length
                    + rotation.NextAuthorityPublicKeySpkiBase64.Length + rotation.PreviousSignatureBase64.Length
                    + rotation.NextSignatureBase64.Length)
                count <= maximumCharacters
            | None -> count <= maximumCharacters

    member _.SynchronizeAsync(
        source: IProviderPolicyAuthorityRotationDistributionSource,
        now: DateTimeOffset,
        timeout: TimeSpan,
        cancellationToken: CancellationToken) = task {
        if isNull (box source) then nullArg (nameof source)
        if timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes 1.0 then
            invalidArg (nameof timeout) "The timeout must be positive and no greater than one minute."
        let current = registry.Current
        let request =
            { Challenge = RandomNumberGenerator.GetBytes 32 |> Convert.ToHexString
              PolicySequence = current |> Option.map _.Sequence |> Option.defaultValue 0L
              PolicyIdentity = current |> Option.map _.Policy.Identity
              AuthorityGeneration = registry.AuthorityGeneration
              ActiveAuthority = registry.ActiveAuthorityIdentity }
        use timeoutSource = CancellationTokenSource.CreateLinkedTokenSource cancellationToken
        timeoutSource.CancelAfter timeout
        let! fetched = task {
            try
                let! response = source.FetchAsync(request, timeoutSource.Token).WaitAsync(timeout, cancellationToken)
                return Ok response
            with
            | :? TimeoutException -> return Error "policy-authority-distribution-timeout"
            | :? OperationCanceledException when cancellationToken.IsCancellationRequested ->
                return Error "policy-authority-distribution-canceled"
            | :? OperationCanceledException -> return Error "policy-authority-distribution-timeout"
            | error when not (error :? OutOfMemoryException) ->
                return Error "policy-authority-distribution-transport-failed"
        }
        match fetched with
        | Error code -> return result code
        | Ok response when not (bounded response) ->
            return result "policy-authority-distribution-response-invalid"
        | Ok response ->
            let verification =
                try
                    let publicKey = Convert.FromBase64String response.EndpointPublicKeySpkiBase64
                    let supplied = publicKey |> SHA256.HashData |> Convert.ToHexString
                                   |> ProviderPublisherTrustPolicyDistributionEndpointId.create
                    if supplied <> endpointIdentity then Error "policy-authority-distribution-endpoint-mismatch"
                    else
                        use verifier = ECDsa.Create()
                        let mutable bytesRead = 0
                        verifier.ImportSubjectPublicKeyInfo(publicKey.AsSpan(), &bytesRead)
                        let curve = verifier.ExportParameters(false).Curve.Oid.Value |> Option.ofObj
                        if bytesRead <> publicKey.Length || verifier.KeySize <> 256
                            || curve <> Some "1.2.840.10045.3.1.7" then
                            Error "policy-authority-distribution-response-invalid"
                        else
                            let signature = Convert.FromBase64String response.SignatureBase64
                            if verifier.VerifyData(
                                ProviderPolicyAuthorityRotationDistributionManifest.encode response,
                                signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence) then Ok ()
                            else Error "policy-authority-distribution-signature-invalid"
                with
                | :? FormatException
                | :? CryptographicException
                | :? ArgumentException
                | :? NullReferenceException -> Error "policy-authority-distribution-response-invalid"
            match verification with
            | Error code -> return result code
            | Ok () when response.Challenge <> request.Challenge ->
                return result "policy-authority-distribution-challenge-mismatch"
            | Ok () when response.PolicySequence <> request.PolicySequence
                || response.PolicyIdentity <> request.PolicyIdentity
                || response.AuthorityGeneration <> request.AuthorityGeneration
                || response.ActiveAuthority <> request.ActiveAuthority ->
                return result "policy-authority-distribution-cursor-mismatch"
            | Ok () ->
                let fresh =
                    try
                        let issued = DateTimeOffset.FromUnixTimeSeconds response.IssuedAtUnixSeconds
                        let expires = DateTimeOffset.FromUnixTimeSeconds response.ExpiresAtUnixSeconds
                        (issued <= now || issued - now <= maximumFutureSkew) && expires > now && expires > issued
                        && expires - issued <= maximumValidity
                    with :? ArgumentOutOfRangeException -> false
                if not fresh then return result "policy-authority-distribution-stale"
                elif not (cursorMatches request) then return result "policy-authority-distribution-superseded"
                else
                    match response.Rotation with
                    | None -> return result "policy-authority-distribution-current"
                    | Some rotation ->
                        let applied = registry.Rotate rotation
                        return result (if applied.IsApplied then "policy-authority-distribution-applied" else applied.Code)
    }
