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

type private Cbi40Reader(bytes: byte array) =
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

[<RequireQualifiedAccess>]
module ProviderPublisherTrustPolicyDistributionWireCodec =
    [<Literal>]
    let MaximumMessageBytes = 1024 * 1024
    [<Literal>]
    let private MaximumEntries = 4096

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
    let private finish (output: MemoryStream) =
        if output.Length > int64 MaximumMessageBytes then raise (InvalidDataException "The wire message is too large.")
        output.ToArray()
    let private validCursor challenge sequence identity =
        let validDigest =
            not (String.IsNullOrEmpty challenge) && challenge.Length = 64
            && challenge |> Seq.forall (fun character ->
                (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'))
        validDigest && sequence >= 0L
        && ((sequence = 0L && Option.isNone identity) || (sequence > 0L && Option.isSome identity))

    let encodeRequest (request: ProviderPublisherTrustPolicyDistributionRequest) =
        if isNull (box request) then nullArg (nameof request)
        if not (validCursor request.Challenge request.CurrentSequence request.CurrentPolicyIdentity) then
            invalidArg (nameof request) "The distribution request cursor is invalid."
        use output = new MemoryStream()
        writeString output "CBI40-REQUEST"
        writeString output request.Challenge
        writeInt64 output request.CurrentSequence
        writeOptional output request.CurrentPolicyIdentity
        finish output

    let private decode operation (bytes: byte array) =
        if bytes.Length = 0 || bytes.Length > MaximumMessageBytes then
            raise (InvalidDataException "The wire message size is invalid.")
        try
            let reader = Cbi40Reader bytes
            let value = operation reader
            if not reader.End then raise (InvalidDataException())
            value
        with
        | error when (error :? ArgumentException) || (error :? DecoderFallbackException)
            || (error :? OverflowException) || (error :? NullReferenceException) ->
            raise (InvalidDataException("The wire message is malformed.", error))

    let decodeRequest (bytes: byte array) : ProviderPublisherTrustPolicyDistributionRequest =
        decode (fun (reader: Cbi40Reader) ->
            if reader.String() <> "CBI40-REQUEST" then raise (InvalidDataException())
            let request: ProviderPublisherTrustPolicyDistributionRequest =
                { Challenge = reader.String(); CurrentSequence = reader.Int64()
                  CurrentPolicyIdentity = reader.OptionalPolicyIdentity() }
            if not (validCursor request.Challenge request.CurrentSequence request.CurrentPolicyIdentity) then
                raise (InvalidDataException())
            request) bytes

    let private writeUpdate output (update: ProviderPublisherTrustPolicyUpdate) =
        if isNull (box update.Policy) || update.Policy.Entries.Length > MaximumEntries then
            raise (InvalidDataException())
        writeInt64 output update.Sequence
        writeOptional output update.PreviousPolicyIdentity
        writeString output (ProviderPublisherTrustPolicyId.value update.Policy.Identity)
        let entries = update.Policy.Entries |> List.sortBy (fun entry -> ProviderPublisherKeyId.value entry.PublisherKeyId)
        writeInt32 output entries.Length
        for entry in entries do
            writeString output (ProviderPublisherKeyId.value entry.PublisherKeyId)
            writeString output (if entry.Disposition = Admitted then "admitted" else "revoked")
        writeString output update.Algorithm
        writeString output update.AuthorityPublicKeySpkiBase64
        writeString output update.SignatureBase64

    let encodeResponse (response: ProviderPublisherTrustPolicyDistributionResponse) =
        if isNull (box response) then nullArg (nameof response)
        use output = new MemoryStream()
        writeString output "CBI40-RESPONSE"
        writeString output response.Challenge
        writeInt64 output response.CurrentSequence
        writeOptional output response.CurrentPolicyIdentity
        writeInt64 output response.IssuedAtUnixSeconds
        writeInt64 output response.ExpiresAtUnixSeconds
        writeInt32 output (if Option.isSome response.Update then 1 else 0)
        response.Update |> Option.iter (writeUpdate output)
        writeString output response.Algorithm
        writeString output response.EndpointPublicKeySpkiBase64
        writeString output response.SignatureBase64
        finish output

    let decodeResponse bytes = decode (fun reader ->
        if reader.String() <> "CBI40-RESPONSE" then raise (InvalidDataException())
        let challenge = reader.String()
        let sequence = reader.Int64()
        let identity = reader.OptionalPolicyIdentity()
        let issued = reader.Int64()
        let expires = reader.Int64()
        let update =
            match reader.Int32() with
            | 0 -> None
            | 1 ->
                let updateSequence = reader.Int64()
                let previous = reader.OptionalPolicyIdentity()
                let policyIdentity = reader.String() |> ProviderPublisherTrustPolicyId.create
                let count = reader.Int32()
                if count < 0 || count > MaximumEntries then raise (InvalidDataException())
                let entries =
                    [ for _ in 1..count do
                        let key = reader.String() |> ProviderPublisherKeyId.create
                        let disposition =
                            match reader.String() with
                            | "admitted" -> Admitted
                            | "revoked" -> Revoked
                            | _ -> raise (InvalidDataException())
                        { PublisherKeyId = key; Disposition = disposition } ]
                Some
                    { Sequence = updateSequence; PreviousPolicyIdentity = previous
                      Policy = { Identity = policyIdentity; Entries = entries }
                      Algorithm = reader.String(); AuthorityPublicKeySpkiBase64 = reader.String()
                      SignatureBase64 = reader.String() }
            | _ -> raise (InvalidDataException())
        { Challenge = challenge; CurrentSequence = sequence; CurrentPolicyIdentity = identity
          IssuedAtUnixSeconds = issued; ExpiresAtUnixSeconds = expires; Update = update
          Algorithm = reader.String(); EndpointPublicKeySpkiBase64 = reader.String(); SignatureBase64 = reader.String() }) bytes

type HttpProviderPublisherTrustPolicyDistributionSource(client: HttpClient, endpoint: Uri) =
    static let mediaType = "application/vnd.brontide.cbi40"
    do
        if not endpoint.IsAbsoluteUri || endpoint.Scheme <> Uri.UriSchemeHttps
            || not (String.IsNullOrEmpty endpoint.UserInfo) || not (String.IsNullOrEmpty endpoint.Fragment) then
            invalidArg (nameof endpoint) "An absolute HTTPS endpoint without user information or a fragment is required."

    static member MediaType = mediaType

    interface IProviderPublisherTrustPolicyDistributionSource with
        member _.FetchAsync(request, cancellationToken) = task {
            use message = new HttpRequestMessage(HttpMethod.Post, endpoint)
            message.Headers.Accept.Add(MediaTypeWithQualityHeaderValue mediaType)
            let content = new ByteArrayContent(ProviderPublisherTrustPolicyDistributionWireCodec.encodeRequest request)
            content.Headers.ContentType <- MediaTypeHeaderValue mediaType
            let nullableContent: HttpContent | null = content
            message.Content <- nullableContent
            use! response = client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            let requestUriMatches =
                response.RequestMessage |> Option.ofObj
                |> Option.bind (fun value -> value.RequestUri |> Option.ofObj)
                |> Option.exists ((=) endpoint)
            let contentTypeMatches =
                response.Content.Headers.ContentType |> Option.ofObj
                |> Option.exists (fun value -> value.MediaType = mediaType && value.Parameters.Count = 0)
            let contentEncodingMatches = response.Content.Headers.ContentEncoding.Count = 0
            if response.StatusCode <> HttpStatusCode.OK || not requestUriMatches || not contentTypeMatches || not contentEncodingMatches then
                raise (InvalidDataException "The HTTP distribution response metadata is invalid.")
            match response.Content.Headers.ContentLength |> Option.ofNullable with
            | Some length when length > int64 ProviderPublisherTrustPolicyDistributionWireCodec.MaximumMessageBytes ->
                raise (InvalidDataException "The HTTP distribution response is too large.")
            | _ -> ()
            use! input = response.Content.ReadAsStreamAsync cancellationToken
            use output = new MemoryStream()
            let buffer = Array.zeroCreate<byte> 8192
            let mutable reading = true
            while reading do
                let! count = input.ReadAsync(buffer.AsMemory(), cancellationToken)
                if count = 0 then reading <- false
                elif output.Length + int64 count > int64 ProviderPublisherTrustPolicyDistributionWireCodec.MaximumMessageBytes then
                    raise (InvalidDataException "The HTTP distribution response is too large.")
                else output.Write(buffer, 0, count)
            return ProviderPublisherTrustPolicyDistributionWireCodec.decodeResponse (output.ToArray())
        }
