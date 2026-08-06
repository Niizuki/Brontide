namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi58Source(
    fetch: ProviderPolicyAuthorityRotationDistributionRequest -> CancellationToken ->
        Task<ProviderPolicyAuthorityRotationDistributionResponse>) =
    let mutable attempts = 0
    member _.Attempts = attempts
    interface IProviderPolicyAuthorityRotationDistributionSource with
        member _.FetchAsync(request, cancellationToken) =
            attempts <- attempts + 1
            fetch request cancellationToken

type private Cbi58Observation = { Code: string; Generation: int64; Attempts: int }

[<TestFixture>]
type ComponentPolicyAuthorityRotationDistributionTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI58 fixture value was missing." | present -> present
    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi58-policy-authority-distribution-vectors.json")))
    let authorityId (key: ECDsa) =
        key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyAuthorityId.create
    let endpointId (key: ECDsa) =
        key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyDistributionEndpointId.create
    let statement generation (previous: ECDsa) (next: ECDsa) =
        let previousId = authorityId previous
        let nextId = authorityId next
        let manifest = ProviderPolicyAuthorityRotationManifest.encode (max generation 1L) 0L None previousId nextId
        let sign (key: ECDsa) =
            key.SignData(manifest, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            |> Convert.ToBase64String
        { Generation = generation; PolicySequence = 0L; PolicyIdentity = None
          PreviousAuthority = previousId; NextAuthority = nextId; Algorithm = "ECDSA-P256-SHA256"
          PreviousAuthorityPublicKeySpkiBase64 = previous.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
          NextAuthorityPublicKeySpkiBase64 = next.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
          PreviousSignatureBase64 = sign previous; NextSignatureBase64 = sign next }
    let respond mutation (request: ProviderPolicyAuthorityRotationDistributionRequest)
        (endpoint: ECDsa) rotation (now: DateTimeOffset) =
        let issued = if mutation = "expired" then now.AddMinutes -2.0 else now
        let unsigned =
            { Challenge = if mutation = "challenge" then String('0', 64) else request.Challenge
              PolicySequence = request.PolicySequence; PolicyIdentity = request.PolicyIdentity
              AuthorityGeneration = if mutation = "cursor" then request.AuthorityGeneration + 1L else request.AuthorityGeneration
              ActiveAuthority = request.ActiveAuthority
              IssuedAtUnixSeconds = issued.ToUnixTimeSeconds()
              ExpiresAtUnixSeconds = (if mutation = "expired" then now.AddMinutes -1.0 else now.AddMinutes 1.0).ToUnixTimeSeconds()
              Rotation = if mutation = "current" then None else Some rotation
              Algorithm = "ECDSA-P256-SHA256"
              EndpointPublicKeySpkiBase64 = endpoint.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
              SignatureBase64 = "" }
        let signature =
            endpoint.SignData(
                ProviderPolicyAuthorityRotationDistributionManifest.encode unsigned,
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            |> Convert.ToBase64String
        let signed = { unsigned with SignatureBase64 = signature }
        if mutation = "signature" then
            let changed = Convert.FromBase64String signed.SignatureBase64
            changed[changed.Length - 1] <- changed[changed.Length - 1] ^^^ 1uy
            { signed with SignatureBase64 = Convert.ToBase64String changed }
        else signed
    let run (vector: JsonElement) = task {
        let mutation = vector.GetProperty("mutation").GetString() |> required
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi58-{Guid.NewGuid():N}")
        try
            use authority = ECDsa.Create ECCurve.NamedCurves.nistP256
            use successor = ECDsa.Create ECCurve.NamedCurves.nistP256
            use endpoint = ECDsa.Create ECCurve.NamedCurves.nistP256
            use otherEndpoint = ECDsa.Create ECCurve.NamedCurves.nistP256
            let _, opened, _ = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), authorityId authority, None)
            let durable = opened.Value
            let rotation = statement (if mutation = "native" then 2L else 1L) authority successor
            let now = DateTimeOffset.FromUnixTimeSeconds 1800000000L
            let source = Cbi58Source(fun request _ -> Task.FromResult(
                respond mutation request (if mutation = "endpoint" then otherEndpoint else endpoint) rotation now))
            let client = ProviderPolicyAuthorityRotationDistributionClient(durable, endpointId endpoint)
            let! actual = client.SynchronizeAsync(source, now, TimeSpan.FromSeconds 1.0, CancellationToken.None)
            let _, recovered, _, _ = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), authorityId authority, None, Some actual.Floor)
            return { Code = actual.Code; Generation = recovered.Value.AuthorityGeneration; Attempts = source.Attempts }
        finally
            if Directory.Exists root then Directory.Delete(root, true)
    }

    [<Test>]
    member _.``shared CBI58 vectors deliver authority rotation``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run vector
            Assert.Multiple(Action(fun () ->
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()))
                Assert.That(actual.Generation, Is.EqualTo(vector.GetProperty("generation").GetInt64()))
                Assert.That(actual.Attempts, Is.EqualTo 1)))
    }

    [<Test>]
    member _.``CBI58 C1 every attempt names the exact durable authority cursor``() = task {
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        let! actual = run vectors[5]
        Assert.That(actual.Code, Is.EqualTo "policy-authority-distribution-cursor-mismatch")
    }

    [<Test>]
    member _.``CBI58 C2 the active distribution endpoint authenticates the whole response``() = task {
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        let! endpoint = run vectors[2]
        let! signature = run vectors[3]
        Assert.That(endpoint.Code, Is.EqualTo "policy-authority-distribution-endpoint-mismatch")
        Assert.That(signature.Code, Is.EqualTo "policy-authority-distribution-signature-invalid")
    }

    [<Test>]
    member _.``CBI58 C3 challenge freshness and concurrent movement fail closed``() = task {
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        let! challenge = run vectors[4]
        let! stale = run vectors[6]
        Assert.That(challenge.Code, Is.EqualTo "policy-authority-distribution-challenge-mismatch")
        Assert.That(stale.Code, Is.EqualTo "policy-authority-distribution-stale")
    }

    [<Test>]
    member _.``CBI58 C4 only CBI57 decides whether a delivered statement applies``() = task {
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        let! applied = run vectors[1]
        let! refused = run vectors[7]
        Assert.That(applied.Code, Is.EqualTo "policy-authority-distribution-applied")
        Assert.That(refused.Code, Is.EqualTo "policy-authority-generation-invalid")
    }

    [<Test>]
    member _.``CBI58 C5 bounds transport timeout and cancellation``() = task {
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi58-bounds-{Guid.NewGuid():N}")
        try
            use authority = ECDsa.Create ECCurve.NamedCurves.nistP256
            use successor = ECDsa.Create ECCurve.NamedCurves.nistP256
            use endpoint = ECDsa.Create ECCurve.NamedCurves.nistP256
            let _, opened, _ = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), authorityId authority, None)
            let client = ProviderPolicyAuthorityRotationDistributionClient(opened.Value, endpointId endpoint)
            let failed = Cbi58Source(fun _ _ -> raise (IOException "unavailable"))
            let! failedResult = client.SynchronizeAsync(failed, DateTimeOffset.UtcNow,
                TimeSpan.FromSeconds 1.0, CancellationToken.None)
            let delayed = Cbi58Source(fun _ token -> task {
                do! Task.Delay(TimeSpan.FromSeconds 1.0, token)
                return raise (InvalidOperationException()) })
            let! timeoutResult = client.SynchronizeAsync(delayed, DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds 10.0, CancellationToken.None)
            use canceled = new CancellationTokenSource()
            canceled.Cancel()
            let! canceledResult = client.SynchronizeAsync(delayed, DateTimeOffset.UtcNow,
                TimeSpan.FromSeconds 1.0, canceled.Token)
            let now = DateTimeOffset.UtcNow
            let rotation = statement 1L authority successor
            let superseding = Cbi58Source(fun request _ ->
                let response = respond "current" request endpoint rotation now
                Assert.That(opened.Value.Rotate(rotation).IsApplied, Is.True)
                Task.FromResult response)
            let! supersededResult = client.SynchronizeAsync(superseding, now,
                TimeSpan.FromSeconds 1.0, CancellationToken.None)
            Assert.Multiple(Action(fun () ->
                Assert.That(failedResult.Code, Is.EqualTo "policy-authority-distribution-transport-failed")
                Assert.That(timeoutResult.Code, Is.EqualTo "policy-authority-distribution-timeout")
                Assert.That(canceledResult.Code, Is.EqualTo "policy-authority-distribution-canceled")
                Assert.That(supersededResult.Code, Is.EqualTo "policy-authority-distribution-superseded")
                Assert.That(opened.Value.AuthorityGeneration, Is.EqualTo 1L)))
        finally
            if Directory.Exists root then Directory.Delete(root, true)
    }

    [<Test>]
    member _.``CBI58 C6 applied rotation is durable and shared``() = task {
        use document = fixture ()
        let! actual = run (document.RootElement.GetProperty("vectors")[1])
        Assert.That(actual, Is.EqualTo { Code = "policy-authority-distribution-applied"; Generation = 1L; Attempts = 1 })
    }
