namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Security.Cryptography
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi59Handler(send: HttpRequestMessage -> CancellationToken -> Task<HttpResponseMessage>) =
    inherit HttpMessageHandler()
    let mutable attempts = 0
    member _.Attempts = attempts
    override _.SendAsync(request, cancellationToken) =
        attempts <- attempts + 1
        send request cancellationToken

type private Cbi59UnknownLengthContent(bytes: byte array) =
    inherit HttpContent()
    override _.SerializeToStreamAsync(stream, _) = stream.WriteAsync(bytes).AsTask()
    override _.TryComputeLength(length: byref<int64>) = length <- 0L; false
    override _.CreateContentReadStreamAsync() = Task.FromResult<Stream>(new MemoryStream(bytes, false))

type private Cbi59Observation = { Code: string; Rotation: bool; Attempts: int; Sha256: string option }

[<TestFixture>]
type ComponentPolicyAuthorityRotationWireTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI59 fixture value was missing." | present -> present
    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi59-policy-authority-wire-vectors.json")))
    let authority value = ProviderPublisherTrustPolicyAuthorityId.create (String(value, 64))
    let request () : ProviderPolicyAuthorityRotationDistributionRequest =
        { Challenge = String('A', 64); PolicySequence = 0L; PolicyIdentity = None
          AuthorityGeneration = 0L; ActiveAuthority = authority 'A' }
    let rotation () : ProviderPolicyAuthorityRotationStatement =
        { Generation = 1L; PolicySequence = 0L; PolicyIdentity = None
          PreviousAuthority = authority 'A'; NextAuthority = authority 'B'; Algorithm = "ECDSA-P256-SHA256"
          PreviousAuthorityPublicKeySpkiBase64 = "previous-key"
          NextAuthorityPublicKeySpkiBase64 = "next-key"
          PreviousSignatureBase64 = "previous-signature"; NextSignatureBase64 = "next-signature" }
    let response rotation : ProviderPolicyAuthorityRotationDistributionResponse =
        { Challenge = String('A', 64); PolicySequence = 0L; PolicyIdentity = None
          AuthorityGeneration = 0L; ActiveAuthority = authority 'A'; IssuedAtUnixSeconds = 1800000000L
          ExpiresAtUnixSeconds = 1800000060L; Rotation = rotation; Algorithm = "ECDSA-P256-SHA256"
          EndpointPublicKeySpkiBase64 = "endpoint-key"; SignatureBase64 = "endpoint-signature" }
    let httpResponse (status: HttpStatusCode) (endpoint: Uri) (content: HttpContent) =
        new HttpResponseMessage(status, RequestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint), Content = content)
    let contentOf (message: HttpRequestMessage) = message.Content |> Option.ofObj |> Option.get

    let run (vector: JsonElement) = task {
        let mutation = vector.GetProperty("mutation").GetString() |> required
        if mutation = "request" then
            let bytes = request () |> ProviderPolicyAuthorityRotationDistributionWireCodec.encodeRequest
            let decoded = bytes |> ProviderPolicyAuthorityRotationDistributionWireCodec.decodeRequest
            return { Code = if decoded = request () then "request-roundtrip" else "wire-invalid"
                     Rotation = false; Attempts = 0; Sha256 = Some(bytes |> SHA256.HashData |> Convert.ToHexString) }
        elif mutation = "current" || mutation = "rotation" then
            let bytes = response (if mutation = "rotation" then Some(rotation ()) else None)
                        |> ProviderPolicyAuthorityRotationDistributionWireCodec.encodeResponse
            let decoded = bytes |> ProviderPolicyAuthorityRotationDistributionWireCodec.decodeResponse
            return { Code = if decoded.Rotation.IsSome then "response-rotation" else "response-current"
                     Rotation = decoded.Rotation.IsSome; Attempts = 0
                     Sha256 = Some(bytes |> SHA256.HashData |> Convert.ToHexString) }
        elif [ "truncated"; "trailing"; "marker"; "utf8" ] |> List.contains mutation then
            let original = response None |> ProviderPolicyAuthorityRotationDistributionWireCodec.encodeResponse
            let bytes =
                match mutation with
                | "truncated" -> original[.. original.Length / 2 - 1]
                | "trailing" -> Array.append original [| 0uy |]
                | _ -> Array.copy original
            if mutation = "marker" then bytes[4] <- bytes[4] ^^^ 1uy
            if mutation = "utf8" then bytes[4] <- 0xFFuy
            let code =
                try ProviderPolicyAuthorityRotationDistributionWireCodec.decodeResponse bytes |> ignore; "wire-accepted"
                with :? InvalidDataException -> "wire-invalid"
            return { Code = code; Rotation = false; Attempts = 0; Sha256 = None }
        else
            let endpoint = Uri "https://policy.example.test/v1/authority-rotation"
            let encoded = response None |> ProviderPolicyAuthorityRotationDistributionWireCodec.encodeResponse
            let handler = new Cbi59Handler(fun message cancellationToken -> task {
                if mutation = "canceled" then do! Task.Delay(TimeSpan.FromSeconds 1.0, cancellationToken)
                let requestContent = contentOf message
                let! bytes = requestContent.ReadAsByteArrayAsync cancellationToken
                Assert.Multiple(Action(fun () ->
                    Assert.That(message.Method, Is.EqualTo HttpMethod.Post)
                    Assert.That(message.RequestUri, Is.EqualTo endpoint)
                    Assert.That(requestContent.Headers.ContentType |> Option.ofObj |> Option.map _.MediaType,
                        Is.EqualTo(Some HttpProviderPolicyAuthorityRotationDistributionSource.MediaType))
                    Assert.That(message.Headers.Accept |> Seq.exactlyOne |> _.MediaType,
                        Is.EqualTo HttpProviderPolicyAuthorityRotationDistributionSource.MediaType)
                    Assert.That(ProviderPolicyAuthorityRotationDistributionWireCodec.decodeRequest bytes,
                        Is.EqualTo(request ()))))
                let content: HttpContent =
                    match mutation with
                    | "declared-oversize" -> new ByteArrayContent(Array.zeroCreate(
                        ProviderPolicyAuthorityRotationDistributionWireCodec.MaximumMessageBytes + 1))
                    | "streamed-oversize" -> new Cbi59UnknownLengthContent(Array.zeroCreate(
                        ProviderPolicyAuthorityRotationDistributionWireCodec.MaximumMessageBytes + 1))
                    | _ -> new ByteArrayContent(encoded)
                content.Headers.ContentType <- MediaTypeHeaderValue(
                    if mutation = "content-type" then "application/octet-stream"
                    else HttpProviderPolicyAuthorityRotationDistributionSource.MediaType)
                if mutation = "content-encoding" then content.Headers.ContentEncoding.Add "gzip"
                let responseEndpoint = if mutation = "redirect" then Uri "https://other.example.test/v1/authority-rotation" else endpoint
                return httpResponse (if mutation = "status" then HttpStatusCode.ServiceUnavailable else HttpStatusCode.OK)
                    responseEndpoint content })
            use client = new HttpClient(handler)
            let source = HttpProviderPolicyAuthorityRotationDistributionSource(client, endpoint)
            use cancellation = new CancellationTokenSource()
            if mutation = "canceled" then cancellation.Cancel()
            try
                let! actual = (source :> IProviderPolicyAuthorityRotationDistributionSource)
                                  .FetchAsync(request (), cancellation.Token)
                return { Code = "transport-success"; Rotation = actual.Rotation.IsSome
                         Attempts = handler.Attempts; Sha256 = None }
            with
            | :? OperationCanceledException ->
                return { Code = "transport-canceled"; Rotation = false; Attempts = handler.Attempts; Sha256 = None }
            | :? InvalidDataException ->
                return { Code = "transport-invalid"; Rotation = false; Attempts = handler.Attempts; Sha256 = None }
    }

    [<Test>]
    member _.``shared CBI59 vectors encode and transport only strict bounded messages``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run vector
            Assert.Multiple(Action(fun () ->
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()))
                Assert.That(actual.Rotation, Is.EqualTo(vector.GetProperty("rotation").GetBoolean()))
                Assert.That(actual.Attempts, Is.LessThanOrEqualTo 1)
                match vector.TryGetProperty "sha256" with
                | true, digest -> Assert.That(actual.Sha256, Is.EqualTo(Some(digest.GetString())))
                | _ -> ()))
    }

    [<Test>]
    member _.``CBI59 C1 request and response have one canonical portable encoding``() =
        let requestBytes = request () |> ProviderPolicyAuthorityRotationDistributionWireCodec.encodeRequest
        let responseBytes = response (Some(rotation ())) |> ProviderPolicyAuthorityRotationDistributionWireCodec.encodeResponse
        Assert.That(requestBytes |> ProviderPolicyAuthorityRotationDistributionWireCodec.decodeRequest
                         |> ProviderPolicyAuthorityRotationDistributionWireCodec.encodeRequest, Is.EqualTo(requestBytes :> obj))
        Assert.That(responseBytes |> ProviderPolicyAuthorityRotationDistributionWireCodec.decodeResponse
                         |> ProviderPolicyAuthorityRotationDistributionWireCodec.encodeResponse, Is.EqualTo(responseBytes :> obj))

    [<Test>]
    member _.``CBI59 C2 decoding is strict total and bounded``() = task {
        use document = fixture ()
        for index in [ 3; 4; 5; 6 ] do
            let! actual = run (document.RootElement.GetProperty("vectors")[index])
            Assert.That(actual.Code, Is.EqualTo "wire-invalid")
    }

    [<Test>]
    member _.``CBI59 C3 concrete source requires one exact HTTPS endpoint``() =
        use client = new HttpClient(new Cbi59Handler(fun _ _ -> raise (AssertionException "not called")))
        Assert.Throws<ArgumentException>(Action(fun () ->
            HttpProviderPolicyAuthorityRotationDistributionSource(client, Uri "http://policy.example.test") |> ignore))
        |> ignore

    [<Test>]
    member _.``CBI59 C4 declared and streamed size are independently bounded``() = task {
        use document = fixture ()
        for index in [ 11; 13 ] do
            let! actual = run (document.RootElement.GetProperty("vectors")[index])
            Assert.That(actual.Code, Is.EqualTo "transport-invalid")
    }

    [<Test>]
    member _.``CBI59 C5 cancellation propagates and the adapter never retries``() = task {
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        for index in [ 7..14 ] do
            let! actual = run vectors[index]
            Assert.That(actual.Attempts, Is.EqualTo 1)
    }

    [<Test>]
    member _.``CBI59 C6 HTTPS source composes through CBI58 and durable CBI57``() = task {
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi59-{Guid.NewGuid():N}")
        try
            use predecessor = ECDsa.Create ECCurve.NamedCurves.nistP256
            use successor = ECDsa.Create ECCurve.NamedCurves.nistP256
            use endpointKey = ECDsa.Create ECCurve.NamedCurves.nistP256
            let identify (key: ECDsa) = key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
                                         |> ProviderPublisherTrustPolicyAuthorityId.create
            let predecessorId, successorId = identify predecessor, identify successor
            let manifest = ProviderPolicyAuthorityRotationManifest.encode 1L 0L None predecessorId successorId
            let sign (key: ECDsa) = key.SignData(manifest, HashAlgorithmName.SHA256,
                DSASignatureFormat.Rfc3279DerSequence) |> Convert.ToBase64String
            let rotation =
                { Generation = 1L; PolicySequence = 0L; PolicyIdentity = None
                  PreviousAuthority = predecessorId; NextAuthority = successorId; Algorithm = "ECDSA-P256-SHA256"
                  PreviousAuthorityPublicKeySpkiBase64 = predecessor.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                  NextAuthorityPublicKeySpkiBase64 = successor.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                  PreviousSignatureBase64 = sign predecessor; NextSignatureBase64 = sign successor }
            let endpointIdentity = endpointKey.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
                                   |> ProviderPublisherTrustPolicyDistributionEndpointId.create
            let _, opened, _ = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), predecessorId, None)
            let endpoint = Uri "https://policy.example.test/v1/authority-rotation"
            let now = DateTimeOffset.FromUnixTimeSeconds 1800000000L
            let handler = new Cbi59Handler(fun message cancellationToken -> task {
                let! bytes = (contentOf message).ReadAsByteArrayAsync cancellationToken
                let decoded = ProviderPolicyAuthorityRotationDistributionWireCodec.decodeRequest bytes
                let unsigned =
                    { Challenge = decoded.Challenge; PolicySequence = decoded.PolicySequence
                      PolicyIdentity = decoded.PolicyIdentity; AuthorityGeneration = decoded.AuthorityGeneration
                      ActiveAuthority = decoded.ActiveAuthority; IssuedAtUnixSeconds = now.ToUnixTimeSeconds()
                      ExpiresAtUnixSeconds = now.AddMinutes(1.0).ToUnixTimeSeconds(); Rotation = Some rotation
                      Algorithm = "ECDSA-P256-SHA256"
                      EndpointPublicKeySpkiBase64 = endpointKey.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                      SignatureBase64 = "" }
                let signature = endpointKey.SignData(
                    ProviderPolicyAuthorityRotationDistributionManifest.encode unsigned,
                    HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence) |> Convert.ToBase64String
                let envelope = { unsigned with SignatureBase64 = signature }
                let content = new ByteArrayContent(ProviderPolicyAuthorityRotationDistributionWireCodec.encodeResponse envelope)
                content.Headers.ContentType <- MediaTypeHeaderValue HttpProviderPolicyAuthorityRotationDistributionSource.MediaType
                return httpResponse HttpStatusCode.OK endpoint content })
            use httpClient = new HttpClient(handler)
            let source = HttpProviderPolicyAuthorityRotationDistributionSource(httpClient, endpoint)
            let client = ProviderPolicyAuthorityRotationDistributionClient(opened.Value, endpointIdentity)
            let! result = client.SynchronizeAsync(source, now, TimeSpan.FromSeconds 1.0, CancellationToken.None)
            Assert.Multiple(Action(fun () ->
                Assert.That(result.IsApplied, Is.True)
                Assert.That(result.Generation, Is.EqualTo 1L)
                Assert.That(handler.Attempts, Is.EqualTo 1)))
        finally
            if Directory.Exists root then Directory.Delete(root, true)
    }
