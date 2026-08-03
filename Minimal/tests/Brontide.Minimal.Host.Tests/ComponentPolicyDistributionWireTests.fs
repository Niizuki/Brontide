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

type private Cbi40Handler(send: HttpRequestMessage -> CancellationToken -> Task<HttpResponseMessage>) =
    inherit HttpMessageHandler()
    let mutable attempts = 0
    member _.Attempts = attempts
    override _.SendAsync(request, cancellationToken) =
        attempts <- attempts + 1
        send request cancellationToken

type private Cbi40UnknownLengthContent(bytes: byte array) =
    inherit HttpContent()
    override _.SerializeToStreamAsync(stream, _) = stream.WriteAsync(bytes).AsTask()
    override _.TryComputeLength(length: byref<int64>) = length <- 0L; false
    override _.CreateContentReadStreamAsync() = Task.FromResult<Stream>(new MemoryStream(bytes, false))

type private Cbi40Observation = { Code: string; Update: bool; Attempts: int; Sha256: string option }

[<TestFixture>]
type ComponentPolicyDistributionWireTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI40 fixture value was missing." | present -> present

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi40-policy-distribution-wire-vectors.json")))

    let request () : ProviderPublisherTrustPolicyDistributionRequest =
        { Challenge = String('A', 64); CurrentSequence = 0L; CurrentPolicyIdentity = None }

    let policy () =
        let entries = [ { PublisherKeyId = ProviderPublisherKeyId.create (String('A', 64)); Disposition = Admitted } ]
        { Identity = ProviderPublisherTrustPolicyIdentity.compute entries; Entries = entries }

    let signPolicy (authority: ECDsa) =
        let selected = policy ()
        let publicKey = authority.ExportSubjectPublicKeyInfo()
        let signature = authority.SignData(
            ProviderPublisherTrustPolicyUpdateManifest.encode 1L None selected.Identity,
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
        { Sequence = 1L; PreviousPolicyIdentity = None; Policy = selected; Algorithm = "ECDSA-P256-SHA256"
          AuthorityPublicKeySpkiBase64 = Convert.ToBase64String publicKey
          SignatureBase64 = Convert.ToBase64String signature }

    let wireUpdate () =
        let selected = policy ()
        { Sequence = 1L; PreviousPolicyIdentity = None; Policy = selected; Algorithm = "ECDSA-P256-SHA256"
          AuthorityPublicKeySpkiBase64 = "authority-key"; SignatureBase64 = "authority-signature" }

    let response update : ProviderPublisherTrustPolicyDistributionResponse =
        { Challenge = String('A', 64); CurrentSequence = 0L; CurrentPolicyIdentity = None
          IssuedAtUnixSeconds = 1800000000L; ExpiresAtUnixSeconds = 1800000060L; Update = update
          Algorithm = "ECDSA-P256-SHA256"; EndpointPublicKeySpkiBase64 = "endpoint-key"
          SignatureBase64 = "endpoint-signature" }

    let httpResponse (status: HttpStatusCode) (endpoint: Uri) (content: HttpContent) =
        new HttpResponseMessage(
            status,
            RequestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint),
            Content = content)

    let contentOf (message: HttpRequestMessage) = message.Content |> Option.ofObj |> Option.get

    let run (vector: JsonElement) = task {
        let mutation = vector.GetProperty("mutation").GetString() |> required
        let update = wireUpdate ()
        if mutation = "request" then
            let bytes = request () |> ProviderPublisherTrustPolicyDistributionWireCodec.encodeRequest
            let decoded = bytes |> ProviderPublisherTrustPolicyDistributionWireCodec.decodeRequest
            return { Code = if decoded = request () then "request-roundtrip" else "wire-invalid"
                     Update = false; Attempts = 0; Sha256 = Some(bytes |> SHA256.HashData |> Convert.ToHexString) }
        elif mutation = "current" || mutation = "update" then
            let bytes = response (if mutation = "update" then Some update else None)
                        |> ProviderPublisherTrustPolicyDistributionWireCodec.encodeResponse
            let decoded = bytes |> ProviderPublisherTrustPolicyDistributionWireCodec.decodeResponse
            return { Code = if decoded.Update.IsSome then "response-update" else "response-current"
                     Update = decoded.Update.IsSome; Attempts = 0
                     Sha256 = Some(bytes |> SHA256.HashData |> Convert.ToHexString) }
        elif [ "truncated"; "trailing"; "marker"; "utf8" ] |> List.contains mutation then
            let original = response None |> ProviderPublisherTrustPolicyDistributionWireCodec.encodeResponse
            let bytes =
                match mutation with
                | "truncated" -> original[.. original.Length / 2 - 1]
                | "trailing" -> Array.append original [| 0uy |]
                | _ -> Array.copy original
            if mutation = "marker" then bytes[4] <- bytes[4] ^^^ 1uy
            if mutation = "utf8" then bytes[4] <- 0xFFuy
            let code =
                try ProviderPublisherTrustPolicyDistributionWireCodec.decodeResponse bytes |> ignore; "wire-accepted"
                with :? InvalidDataException -> "wire-invalid"
            return { Code = code; Update = false; Attempts = 0; Sha256 = None }
        else
            let endpoint = Uri "https://policy.example.test/v1/trust"
            let encoded = response None |> ProviderPublisherTrustPolicyDistributionWireCodec.encodeResponse
            let handler = new Cbi40Handler(fun message cancellationToken -> task {
                if mutation = "canceled" then do! Task.Delay(TimeSpan.FromSeconds 1.0, cancellationToken)
                let requestContent = contentOf message
                let! requestBytes = requestContent.ReadAsByteArrayAsync cancellationToken
                Assert.Multiple(Action(fun () ->
                    Assert.That(message.Method, Is.EqualTo(HttpMethod.Post))
                    Assert.That(message.RequestUri, Is.EqualTo(endpoint))
                    Assert.That(requestContent.Headers.ContentType |> Option.ofObj |> Option.map _.MediaType,
                        Is.EqualTo(Some HttpProviderPublisherTrustPolicyDistributionSource.MediaType))
                    Assert.That(message.Headers.Accept |> Seq.exactlyOne |> _.MediaType,
                        Is.EqualTo(HttpProviderPublisherTrustPolicyDistributionSource.MediaType))
                    Assert.That(ProviderPublisherTrustPolicyDistributionWireCodec.decodeRequest requestBytes,
                        Is.EqualTo(request ()))))
                let content: HttpContent =
                    match mutation with
                    | "declared-oversize" ->
                        new ByteArrayContent(Array.zeroCreate (ProviderPublisherTrustPolicyDistributionWireCodec.MaximumMessageBytes + 1))
                    | "streamed-oversize" ->
                        new Cbi40UnknownLengthContent(
                            Array.zeroCreate (ProviderPublisherTrustPolicyDistributionWireCodec.MaximumMessageBytes + 1))
                    | _ -> new ByteArrayContent(encoded)
                content.Headers.ContentType <- MediaTypeHeaderValue(
                    if mutation = "content-type" then "application/octet-stream"
                    else HttpProviderPublisherTrustPolicyDistributionSource.MediaType)
                if mutation = "content-encoding" then content.Headers.ContentEncoding.Add "gzip"
                let responseEndpoint = if mutation = "redirect" then Uri "https://other.example.test/v1/trust" else endpoint
                return httpResponse
                    (if mutation = "status" then HttpStatusCode.ServiceUnavailable else HttpStatusCode.OK)
                    responseEndpoint content })
            use client = new HttpClient(handler)
            let source = HttpProviderPublisherTrustPolicyDistributionSource(client, endpoint)
            use cancellation = new CancellationTokenSource()
            if mutation = "canceled" then cancellation.Cancel()
            try
                let! actual = (source :> IProviderPublisherTrustPolicyDistributionSource)
                                  .FetchAsync(request (), cancellation.Token)
                return { Code = "transport-success"; Update = actual.Update.IsSome; Attempts = handler.Attempts; Sha256 = None }
            with
            | :? OperationCanceledException ->
                return { Code = "transport-canceled"; Update = false; Attempts = handler.Attempts; Sha256 = None }
            | :? InvalidDataException ->
                return { Code = "transport-invalid"; Update = false; Attempts = handler.Attempts; Sha256 = None }
    }

    [<Test>]
    member _.``shared CBI40 vectors encode and transport only strict bounded messages``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run vector
            Assert.Multiple(Action(fun () ->
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()))
                Assert.That(actual.Update, Is.EqualTo(vector.GetProperty("update").GetBoolean()))
                Assert.That(actual.Attempts, Is.LessThanOrEqualTo(1))
                match vector.TryGetProperty "sha256" with
                | true, digest -> Assert.That(actual.Sha256, Is.EqualTo(Some(digest.GetString())))
                | _ -> ()))
    }

    [<Test>]
    member _.``CBI40 C1 request codec is canonical complete and strict``() =
        let bytes = request () |> ProviderPublisherTrustPolicyDistributionWireCodec.encodeRequest
        Assert.That(bytes |> ProviderPublisherTrustPolicyDistributionWireCodec.decodeRequest
                          |> ProviderPublisherTrustPolicyDistributionWireCodec.encodeRequest,
                    Is.EqualTo(bytes :> obj))
        Assert.Throws<InvalidDataException>(Action(fun () ->
            Array.append bytes [| 0uy |] |> ProviderPublisherTrustPolicyDistributionWireCodec.decodeRequest |> ignore))
        |> ignore

    [<Test>]
    member _.``CBI40 C2 response codec preserves the complete optional update``() =
        use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
        let update = signPolicy authority
        let decoded = response (Some update) |> ProviderPublisherTrustPolicyDistributionWireCodec.encodeResponse
                      |> ProviderPublisherTrustPolicyDistributionWireCodec.decodeResponse
        Assert.That(ProviderPublisherTrustPolicyDistributionManifest.updateDigest decoded.Update.Value,
                    Is.EqualTo(ProviderPublisherTrustPolicyDistributionManifest.updateDigest update))

    [<Test>]
    member _.``CBI40 C3 malformed truncated and trailing messages are refused``() = task {
        use document = fixture ()
        for index in [ 3; 4; 5; 6 ] do
            let! actual = run (document.RootElement.GetProperty("vectors")[index])
            Assert.That(actual.Code, Is.EqualTo("wire-invalid"))
    }

    [<Test>]
    member _.``CBI40 C4 transport requires an exact HTTPS endpoint``() =
        use client = new HttpClient(new Cbi40Handler(fun _ _ -> raise (AssertionException "not called")))
        Assert.Throws<ArgumentException>(Action(fun () ->
            HttpProviderPublisherTrustPolicyDistributionSource(client, Uri "http://policy.example.test") |> ignore))
        |> ignore

    [<Test>]
    member _.``CBI40 C5 HTTP metadata and streaming are bounded without retry``() = task {
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        for index in [ 7; 8; 9; 10; 11; 13; 14 ] do
            let! actual = run vectors[index]
            Assert.That(actual.Attempts, Is.EqualTo(1))
    }

    [<Test>]
    member _.``CBI40 C6 HTTPS source composes with authenticated durable distribution``() = task {
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi40-{Guid.NewGuid():N}")
        try
            use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use endpointKey = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let authorityIdentity =
                authority.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
                |> ProviderPublisherTrustPolicyAuthorityId.create
            let endpointIdentity =
                endpointKey.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
                |> ProviderPublisherTrustPolicyDistributionEndpointId.create
            let _, opened, _ = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), authorityIdentity, None)
            let durable = opened.Value
            let endpoint = Uri "https://policy.example.test/v1/trust"
            let now = DateTimeOffset.FromUnixTimeSeconds 1800000000L
            let update = signPolicy authority
            let handler = new Cbi40Handler(fun message cancellationToken -> task {
                let! bytes = (contentOf message).ReadAsByteArrayAsync cancellationToken
                let decoded = ProviderPublisherTrustPolicyDistributionWireCodec.decodeRequest bytes
                let publicKey = endpointKey.ExportSubjectPublicKeyInfo()
                let issued, expires = now.ToUnixTimeSeconds(), now.AddMinutes(1.0).ToUnixTimeSeconds()
                let signature = endpointKey.SignData(
                    ProviderPublisherTrustPolicyDistributionManifest.encode decoded.Challenge decoded.CurrentSequence
                        decoded.CurrentPolicyIdentity issued expires (Some update),
                    HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                let envelope =
                    { Challenge = decoded.Challenge; CurrentSequence = decoded.CurrentSequence
                      CurrentPolicyIdentity = decoded.CurrentPolicyIdentity; IssuedAtUnixSeconds = issued
                      ExpiresAtUnixSeconds = expires; Update = Some update; Algorithm = "ECDSA-P256-SHA256"
                      EndpointPublicKeySpkiBase64 = Convert.ToBase64String publicKey
                      SignatureBase64 = Convert.ToBase64String signature }
                let content = new ByteArrayContent(
                    ProviderPublisherTrustPolicyDistributionWireCodec.encodeResponse envelope)
                content.Headers.ContentType <- MediaTypeHeaderValue(
                    HttpProviderPublisherTrustPolicyDistributionSource.MediaType)
                return httpResponse HttpStatusCode.OK endpoint content })
            use httpClient = new HttpClient(handler)
            let source = HttpProviderPublisherTrustPolicyDistributionSource(httpClient, endpoint)
            let client = ProviderPublisherTrustPolicyDistributionClient(durable, endpointIdentity)
            let! result = client.SynchronizeAsync(source, now, TimeSpan.FromSeconds 1.0, CancellationToken.None)
            Assert.Multiple(Action(fun () ->
                Assert.That(result.IsApplied, Is.True)
                Assert.That(result.Floor.Sequence, Is.EqualTo(1L))
                Assert.That(handler.Attempts, Is.EqualTo(1))
                Assert.That(File.Exists(Path.Combine(root, "policy.checkpoint")), Is.True)))
        finally
            if Directory.Exists root then Directory.Delete(root, true)
    }
