namespace Brontide.Minimal.Host

open System
open System.Buffers.Binary
open System.IO
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Threading
open System.Threading.Tasks

type private Cbi59Reader(bytes: byte array) =
    let mutable offset = 0
    let ensure length = if length > bytes.Length - offset then raise (InvalidDataException())
    member _.End = offset = bytes.Length
    member _.Int32() =
        ensure 4
        let value = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4))
        offset <- offset + 4
        value
    member _.Int64() =
        ensure 8
        let value = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(offset, 8))
        offset <- offset + 8
        value
    member this.String() =
        let length = this.Int32()
        if length < 0 || length > 1024 * 1024 then raise (InvalidDataException())
        ensure length
        let value = UTF8Encoding(false, true).GetString(bytes, offset, length)
        offset <- offset + length
        value
    member this.OptionalPolicyIdentity() =
        match this.Int32() with
        | 0 -> None
        | 1 -> this.String() |> ProviderPublisherTrustPolicyId.create |> Some
        | _ -> raise (InvalidDataException())
    member this.AuthorityIdentity() =
        this.String() |> ProviderPublisherTrustPolicyAuthorityId.create
    member this.Rotation() =
        { Generation = this.Int64()
          PolicySequence = this.Int64()
          PolicyIdentity = this.OptionalPolicyIdentity()
          PreviousAuthority = this.AuthorityIdentity()
          NextAuthority = this.AuthorityIdentity()
          Algorithm = this.String()
          PreviousAuthorityPublicKeySpkiBase64 = this.String()
          NextAuthorityPublicKeySpkiBase64 = this.String()
          PreviousSignatureBase64 = this.String()
          NextSignatureBase64 = this.String() }

[<RequireQualifiedAccess>]
module ProviderPolicyAuthorityRotationDistributionWireCodec =
    [<Literal>]
    let MaximumMessageBytes = 1024 * 1024

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
    let private writeOptional output identity =
        writeInt32 output (if Option.isSome identity then 1 else 0)
        identity |> Option.iter (ProviderPublisherTrustPolicyId.value >> writeString output)
    let private writeRotation output (rotation: ProviderPolicyAuthorityRotationStatement) =
        writeInt64 output rotation.Generation
        writeInt64 output rotation.PolicySequence
        writeOptional output rotation.PolicyIdentity
        writeString output (ProviderPublisherTrustPolicyAuthorityId.value rotation.PreviousAuthority)
        writeString output (ProviderPublisherTrustPolicyAuthorityId.value rotation.NextAuthority)
        writeString output rotation.Algorithm
        writeString output rotation.PreviousAuthorityPublicKeySpkiBase64
        writeString output rotation.NextAuthorityPublicKeySpkiBase64
        writeString output rotation.PreviousSignatureBase64
        writeString output rotation.NextSignatureBase64
    let private finish (output: MemoryStream) =
        if output.Length > int64 MaximumMessageBytes then raise (InvalidDataException "The wire message is too large.")
        output.ToArray()
    let private validCursor challenge sequence identity generation authority =
        let validDigest value =
            not (String.IsNullOrEmpty value) && value.Length = 64
            && value |> Seq.forall (fun character ->
                (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'))
        validDigest challenge && sequence >= 0L && generation >= 0L
        && validDigest (ProviderPublisherTrustPolicyAuthorityId.value authority)
        && ((sequence = 0L && Option.isNone identity) || (sequence > 0L && Option.isSome identity))

    let encodeRequest (request: ProviderPolicyAuthorityRotationDistributionRequest) =
        if isNull (box request) then nullArg (nameof request)
        if not (validCursor request.Challenge request.PolicySequence request.PolicyIdentity request.AuthorityGeneration request.ActiveAuthority) then
            invalidArg (nameof request) "The authority-rotation distribution request cursor is invalid."
        use output = new MemoryStream()
        writeString output "CBI59-REQUEST"
        writeString output request.Challenge
        writeInt64 output request.PolicySequence
        writeOptional output request.PolicyIdentity
        writeInt64 output request.AuthorityGeneration
        writeString output (ProviderPublisherTrustPolicyAuthorityId.value request.ActiveAuthority)
        finish output

    let private decode operation (bytes: byte array) =
        if bytes.Length = 0 || bytes.Length > MaximumMessageBytes then
            raise (InvalidDataException "The wire message size is invalid.")
        try
            let reader = Cbi59Reader bytes
            let value = operation reader
            if not reader.End then raise (InvalidDataException())
            value
        with
        | error when (error :? ArgumentException) || (error :? DecoderFallbackException)
            || (error :? OverflowException) || (error :? NullReferenceException) ->
            raise (InvalidDataException("The wire message is malformed.", error))

    let decodeRequest bytes = decode (fun (reader: Cbi59Reader) ->
        if reader.String() <> "CBI59-REQUEST" then raise (InvalidDataException())
        let challenge = reader.String()
        let policySequence = reader.Int64()
        let policyIdentity = reader.OptionalPolicyIdentity()
        let authorityGeneration = reader.Int64()
        let activeAuthority = reader.AuthorityIdentity()
        let cursorValid =
            validCursor challenge policySequence policyIdentity authorityGeneration activeAuthority
        if not cursorValid then
            raise (InvalidDataException())
        { Challenge = challenge; PolicySequence = policySequence; PolicyIdentity = policyIdentity
          AuthorityGeneration = authorityGeneration; ActiveAuthority = activeAuthority }
        : ProviderPolicyAuthorityRotationDistributionRequest) bytes

    let encodeResponse (response: ProviderPolicyAuthorityRotationDistributionResponse) =
        if isNull (box response) then nullArg (nameof response)
        use output = new MemoryStream()
        writeString output "CBI59-RESPONSE"
        writeString output response.Challenge
        writeInt64 output response.PolicySequence
        writeOptional output response.PolicyIdentity
        writeInt64 output response.AuthorityGeneration
        writeString output (ProviderPublisherTrustPolicyAuthorityId.value response.ActiveAuthority)
        writeInt64 output response.IssuedAtUnixSeconds
        writeInt64 output response.ExpiresAtUnixSeconds
        writeInt32 output (if Option.isSome response.Rotation then 1 else 0)
        response.Rotation |> Option.iter (writeRotation output)
        writeString output response.Algorithm
        writeString output response.EndpointPublicKeySpkiBase64
        writeString output response.SignatureBase64
        finish output

    let decodeResponse bytes = decode (fun (reader: Cbi59Reader) ->
        if reader.String() <> "CBI59-RESPONSE" then raise (InvalidDataException())
        let challenge = reader.String()
        let policySequence = reader.Int64()
        let policyIdentity = reader.OptionalPolicyIdentity()
        let generation = reader.Int64()
        let authority = reader.AuthorityIdentity()
        let issued = reader.Int64()
        let expires = reader.Int64()
        let rotation =
            match reader.Int32() with
            | 0 -> None
            | 1 -> Some(reader.Rotation())
            | _ -> raise (InvalidDataException())
        { Challenge = challenge; PolicySequence = policySequence; PolicyIdentity = policyIdentity
          AuthorityGeneration = generation; ActiveAuthority = authority; IssuedAtUnixSeconds = issued
          ExpiresAtUnixSeconds = expires; Rotation = rotation; Algorithm = reader.String()
          EndpointPublicKeySpkiBase64 = reader.String(); SignatureBase64 = reader.String() }
        : ProviderPolicyAuthorityRotationDistributionResponse) bytes

type HttpProviderPolicyAuthorityRotationDistributionSource(client: HttpClient, endpoint: Uri) =
    static let mediaType = "application/vnd.brontide.cbi59"
    do
        if not endpoint.IsAbsoluteUri || endpoint.Scheme <> Uri.UriSchemeHttps
            || not (String.IsNullOrEmpty endpoint.UserInfo) || not (String.IsNullOrEmpty endpoint.Fragment) then
            invalidArg (nameof endpoint) "An absolute HTTPS endpoint without user information or a fragment is required."
    static member MediaType = mediaType
    interface IProviderPolicyAuthorityRotationDistributionSource with
        member _.FetchAsync(request, cancellationToken) = task {
            use message = new HttpRequestMessage(HttpMethod.Post, endpoint)
            message.Headers.Accept.Add(MediaTypeWithQualityHeaderValue mediaType)
            let content = new ByteArrayContent(
                ProviderPolicyAuthorityRotationDistributionWireCodec.encodeRequest request)
            content.Headers.ContentType <- MediaTypeHeaderValue mediaType
            let nullableContent: HttpContent | null = content
            message.Content <- nullableContent
            use! response = client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            let responseEndpoint = response.RequestMessage |> Option.ofObj |> Option.bind (fun value -> value.RequestUri |> Option.ofObj)
            let contentType = response.Content.Headers.ContentType |> Option.ofObj
            if response.StatusCode <> HttpStatusCode.OK || responseEndpoint <> Some endpoint
                || (contentType |> Option.map _.MediaType) <> Some mediaType
                || (contentType |> Option.exists (fun value -> value.Parameters.Count > 0))
                || response.Content.Headers.ContentEncoding.Count > 0 then
                raise (InvalidDataException "The HTTP authority-rotation response metadata is invalid.")
            match response.Content.Headers.ContentLength |> Option.ofNullable with
            | Some length when length > int64 ProviderPolicyAuthorityRotationDistributionWireCodec.MaximumMessageBytes ->
                raise (InvalidDataException "The HTTP authority-rotation response is too large.")
            | _ -> ()
            use! input = response.Content.ReadAsStreamAsync cancellationToken
            use output = new MemoryStream()
            let buffer = Array.zeroCreate<byte> 8192
            let mutable reading = true
            while reading do
                let! count = input.ReadAsync(buffer.AsMemory(), cancellationToken)
                if count = 0 then reading <- false
                elif output.Length + int64 count > int64 ProviderPolicyAuthorityRotationDistributionWireCodec.MaximumMessageBytes then
                    raise (InvalidDataException "The HTTP authority-rotation response is too large.")
                else output.Write(buffer, 0, count)
            return ProviderPolicyAuthorityRotationDistributionWireCodec.decodeResponse (output.ToArray())
        }
