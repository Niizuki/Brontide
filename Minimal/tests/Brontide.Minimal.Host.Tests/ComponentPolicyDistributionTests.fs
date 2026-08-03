namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi39Source(
    fetch: ProviderPublisherTrustPolicyDistributionRequest -> CancellationToken ->
        Task<ProviderPublisherTrustPolicyDistributionResponse>) =
    let mutable attempts = 0
    member _.Attempts = attempts
    interface IProviderPublisherTrustPolicyDistributionSource with
        member _.FetchAsync(request, cancellationToken) =
            attempts <- attempts + 1
            fetch request cancellationToken

type private Cbi39Observation = { Code: string; Sequence: int64; Attempts: int }

[<TestFixture>]
type ComponentPolicyDistributionTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI39 fixture value was missing." | present -> present

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi39-policy-distribution-vectors.json")))

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

    let endpointId (endpoint: ECDsa) =
        endpoint.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyDistributionEndpointId.create

    let authorityId (authority: ECDsa) =
        authority.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyAuthorityId.create

    let respond mutation (request: ProviderPublisherTrustPolicyDistributionRequest)
        (endpoint: ECDsa) (update: ProviderPublisherTrustPolicyUpdate) (now: DateTimeOffset) =
        let selectedUpdate =
            if mutation = "current" then None
            elif mutation = "policy-signature" then
                let changed = Convert.FromBase64String update.SignatureBase64
                changed[changed.Length - 1] <- changed[changed.Length - 1] ^^^ 1uy
                Some { update with SignatureBase64 = Convert.ToBase64String changed }
            else Some update
        let challenge = if mutation = "challenge" then String('0', 64) else request.Challenge
        let sequence = if mutation = "cursor" then request.CurrentSequence + 1L else request.CurrentSequence
        let issued =
            match mutation with
            | "expired" -> now.AddMinutes -2.0
            | "future" -> now.AddMinutes 1.0
            | _ -> now
        let expires = if mutation = "expired" then now.AddMinutes -1.0 else issued.AddMinutes 1.0
        let publicKey = endpoint.ExportSubjectPublicKeyInfo()
        let signature = endpoint.SignData(
            ProviderPublisherTrustPolicyDistributionManifest.encode challenge sequence request.CurrentPolicyIdentity
                (issued.ToUnixTimeSeconds()) (expires.ToUnixTimeSeconds()) selectedUpdate,
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
        let response =
            { Challenge = challenge; CurrentSequence = sequence; CurrentPolicyIdentity = request.CurrentPolicyIdentity
              IssuedAtUnixSeconds = issued.ToUnixTimeSeconds(); ExpiresAtUnixSeconds = expires.ToUnixTimeSeconds()
              Update = selectedUpdate; Algorithm = "ECDSA-P256-SHA256"
              EndpointPublicKeySpkiBase64 = Convert.ToBase64String publicKey
              SignatureBase64 = Convert.ToBase64String signature }
        if mutation = "signature" then
            let changed = Convert.FromBase64String response.SignatureBase64
            changed[changed.Length - 1] <- changed[changed.Length - 1] ^^^ 1uy
            { response with SignatureBase64 = Convert.ToBase64String changed }
        elif mutation = "oversized" then { response with SignatureBase64 = String('A', 1024 * 1024 + 1) }
        else response

    let run (vector: JsonElement) = task {
        let mutation = vector.GetProperty("mutation").GetString() |> required
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi39-{Guid.NewGuid():N}")
        try
            use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use endpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use otherEndpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let _, opened, _ = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), authorityId authority, None)
            let durable = opened.Value
            let update = signPolicy authority
            let now = DateTimeOffset.FromUnixTimeSeconds 1800000000L
            let source = Cbi39Source(fun request cancellationToken -> task {
                if mutation = "transport" then raise (IOException "unavailable")
                if mutation = "timeout" || mutation = "canceled" then
                    do! Task.Delay(TimeSpan.FromSeconds 1.0, cancellationToken)
                return respond mutation request (if mutation = "endpoint" then otherEndpoint else endpoint) update now })
            let client = ProviderPublisherTrustPolicyDistributionClient(durable, endpointId endpoint)
            let timeout = if mutation = "timeout" then TimeSpan.FromMilliseconds 10.0 else TimeSpan.FromSeconds 1.0
            use cancellation = new CancellationTokenSource()
            if mutation = "canceled" then cancellation.Cancel()
            let! actual = client.SynchronizeAsync(source, now, timeout, cancellation.Token)
            Assert.That(actual.Floor.Sequence, Is.EqualTo(actual.Current |> Option.map _.Sequence |> Option.defaultValue 0L))
            return { Code = actual.Code; Sequence = actual.Current |> Option.map _.Sequence |> Option.defaultValue 0L
                     Attempts = source.Attempts }
        finally
            if Directory.Exists root then Directory.Delete(root, true)
    }

    [<Test>]
    member _.``shared CBI39 vectors authenticate fresh bounded distribution``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run vector
            Assert.Multiple(Action(fun () ->
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()))
                Assert.That(actual.Sequence, Is.EqualTo(vector.GetProperty("sequence").GetInt64()))
                Assert.That(actual.Attempts, Is.EqualTo(1))))
    }

    [<Test>]
    member _.``CBI39 C1 only the pinned endpoint can authenticate a response``() = task {
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        let! endpoint = run vectors[2]
        let! signature = run vectors[3]
        Assert.That(endpoint.Code, Is.EqualTo("policy-distribution-endpoint-mismatch"))
        Assert.That(signature.Code, Is.EqualTo("policy-distribution-signature-invalid"))
    }

    [<Test>]
    member _.``CBI39 C2 signed challenge and cursor prevent replay and cross state delivery``() = task {
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        let! challenge = run vectors[4]
        let! cursor = run vectors[5]
        Assert.That(challenge.Code, Is.EqualTo("policy-distribution-challenge-mismatch"))
        Assert.That(cursor.Code, Is.EqualTo("policy-distribution-cursor-mismatch"))
    }

    [<Test>]
    member _.``CBI39 C3 only current short lived responses are accepted``() = task {
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        let! current = run vectors[0]
        let! update = run vectors[1]
        let! expired = run vectors[6]
        let! future = run vectors[7]
        Assert.That(current.Code, Is.EqualTo("policy-distribution-current"))
        Assert.That(update.Code, Is.EqualTo("policy-distribution-applied"))
        Assert.That(expired.Code, Is.EqualTo("policy-distribution-stale"))
        Assert.That(future.Code, Is.EqualTo("policy-distribution-stale"))
    }

    [<Test>]
    member _.``CBI39 C4 one attempt has strict size and time bounds``() = task {
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        for index in [ 8; 10; 11; 12 ] do
            let! actual = run vectors[index]
            Assert.That(actual.Attempts, Is.EqualTo(1))
    }

    [<Test>]
    member _.``CBI39 C5 only a native CBI37 update can advance the durable registry``() = task {
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        let! invalid = run vectors[9]
        let! valid = run vectors[1]
        Assert.That(invalid.Code, Is.EqualTo("policy-update-signature-invalid"))
        Assert.That(valid.Sequence, Is.EqualTo(1L))
    }

    [<Test>]
    member _.``CBI39 C6 both roots agree on portable observations``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run vector
            Assert.That((actual.Code, actual.Sequence), Is.EqualTo(
                (vector.GetProperty("code").GetString(), vector.GetProperty("sequence").GetInt64())))
    }
