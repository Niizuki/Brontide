namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi37Source(identity: ProviderArtifactSourceId, bytes: byte array) =
    let mutable reads = 0
    member _.Accesses = reads
    interface IProviderArtifactSource with
        member _.Identity = reads <- reads + 1; identity
        member _.OpenRead path =
            reads <- reads + 1
            if path = "provider.bin" then Some(new MemoryStream(bytes, false) :> Stream) else None

type private Cbi37Observation =
    { Code: string; Sequence: int64; PolicyIdentity: string option; Changed: bool }

[<TestFixture>]
type ComponentPolicyUpdateTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI37 fixture value was missing." | present -> present

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi37-policy-update-vectors.json")))

    let policy revoked : ProviderPublisherTrustPolicy =
        let entries =
            [ { PublisherKeyId = ProviderPublisherKeyId.create (String('A', 64))
                Disposition = if revoked then Revoked else Admitted } ]
        { Identity = ProviderPublisherTrustPolicyIdentity.compute entries; Entries = entries }

    let sign (key: ECDsa) sequence previous (policy: ProviderPublisherTrustPolicy) algorithm =
        let publicKey = key.ExportSubjectPublicKeyInfo()
        let signature = key.SignData(
            ProviderPublisherTrustPolicyUpdateManifest.encode sequence previous policy.Identity,
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
        { Sequence = sequence; PreviousPolicyIdentity = previous; Policy = policy; Algorithm = algorithm
          AuthorityPublicKeySpkiBase64 = Convert.ToBase64String publicKey
          SignatureBase64 = Convert.ToBase64String signature }

    let state (snapshot: VerifiedProviderPublisherTrustPolicySnapshot option) =
        snapshot |> Option.map (fun value -> value.Sequence, ProviderPublisherTrustPolicyId.value value.Policy.Identity)

    let run (vector: JsonElement) =
        let mutation = vector.GetProperty("mutation").GetString() |> required
        use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
        let authorityId = authority.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
                          |> ProviderPublisherTrustPolicyAuthorityId.create
        let registry = ProviderPublisherTrustPolicyRegistry authorityId
        let initial, successor = policy false, policy true
        if mutation <> "none" then Assert.That((registry.Apply(sign authority 1L None initial "ECDSA-P256-SHA256")).IsApplied, Is.True)
        let before = state registry.Current
        let mutable update =
            match mutation with
            | "none" -> sign authority 1L None initial "ECDSA-P256-SHA256"
            | "successor" -> sign authority 2L (Some initial.Identity) successor "ECDSA-P256-SHA256"
            | "replay" -> sign authority 1L None initial "ECDSA-P256-SHA256"
            | "gap" -> sign authority 3L (Some initial.Identity) successor "ECDSA-P256-SHA256"
            | "fork" -> sign authority 2L (Some(ProviderPublisherTrustPolicyId.create (String('0', 64)))) successor "ECDSA-P256-SHA256"
            | "policy" -> sign authority 2L (Some initial.Identity)
                                { successor with Identity = ProviderPublisherTrustPolicyId.create (String('0', 64)) } "ECDSA-P256-SHA256"
            | "algorithm" -> sign authority 2L (Some initial.Identity) successor "unknown"
            | _ -> sign authority 2L (Some initial.Identity) successor "ECDSA-P256-SHA256"
        if mutation = "authority" then
            use other = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            update <- sign other 2L (Some initial.Identity) successor "ECDSA-P256-SHA256"
        elif mutation = "signature" then
            let changed = Convert.FromBase64String update.SignatureBase64
            changed[changed.Length - 1] <- changed[changed.Length - 1] ^^^ 1uy
            update <- { update with SignatureBase64 = Convert.ToBase64String changed }
        let result = registry.Apply update
        let after = state result.Current
        { Code = result.Code
          Sequence = result.Current |> Option.map _.Sequence |> Option.defaultValue 0L
          PolicyIdentity = result.Current |> Option.map (_.Policy.Identity >> ProviderPublisherTrustPolicyId.value)
          Changed = before <> after }

    let acquisitionRequest () : ProviderArtifactAcquisitionRequest =
        let bytes = Encoding.UTF8.GetBytes "provider"
        let digest = SHA256.HashData bytes |> Convert.ToHexString
        { ExpectedSource = ProviderArtifactSourceId.create "fixture://brontide/policy-governed"
          Identity = ProviderArtifactSetIdentity.compute [ { RelativePath = "provider.bin"; Sha256 = digest } ] "provider.bin" []
          Files = [ { RelativePath = "provider.bin"; Sha256 = digest; Length = int64 bytes.Length } ]
          ExecutablePath = "provider.bin"; Arguments = []; MaxTotalBytes = int64 bytes.Length }

    let authorization (request: ProviderArtifactAcquisitionRequest) =
        let trusted = policy false
        let evidence =
            { ContentIdentity = request.Identity
              PublisherKeyId = ProviderPublisherKeyId.create (String('A', 64))
              PayloadSha256 = ProviderArtifactPublisherManifest.digest request }
        (ProviderPublisherTrustEvaluator.evaluate trusted (Some evidence)).Authorization.Value

    [<Test>]
    member _.``shared CBI37 vectors apply only authoritative monotonic updates``() =
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let actual = run vector
            Assert.Multiple(Action(fun () ->
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()))
                Assert.That(actual.Sequence, Is.EqualTo(vector.GetProperty("sequence").GetInt64()))
                Assert.That(actual.Changed, Is.EqualTo(vector.GetProperty("changed").GetBoolean()))))

    [<Test>]
    member _.``CBI37 C1 one pinned authority controls policy provenance``() =
        use document = fixture ()
        Assert.That((run (document.RootElement.GetProperty("vectors")[2])).Code,
                    Is.EqualTo("policy-update-authority-mismatch"))

    [<Test>]
    member _.``CBI37 C2 signature covers a canonical complete update payload``() =
        use document = fixture ()
        let golden = document.RootElement.GetProperty "golden"
        let initial, successor = policy false, policy true
        Assert.Multiple(Action(fun () ->
            Assert.That(ProviderPublisherTrustPolicyId.value initial.Identity, Is.EqualTo(golden.GetProperty("initialPolicyIdentity").GetString()))
            Assert.That(ProviderPublisherTrustPolicyId.value successor.Identity, Is.EqualTo(golden.GetProperty("successorPolicyIdentity").GetString()))
            Assert.That(ProviderPublisherTrustPolicyUpdateManifest.digest 1L None initial.Identity, Is.EqualTo(golden.GetProperty("initialPayloadSha256").GetString()))
            Assert.That(ProviderPublisherTrustPolicyUpdateManifest.digest 2L (Some initial.Identity) successor.Identity, Is.EqualTo(golden.GetProperty("successorPayloadSha256").GetString()))))

    [<Test>]
    member _.``CBI37 C3 updates form one strict monotonic predecessor chain``() =
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        for index in [ 5; 6; 7 ] do Assert.That((run vectors[index]).Changed, Is.False)

    [<Test>]
    member _.``CBI37 C4 application publishes one issuer controlled snapshot and refusal preserves it``() =
        Assert.That(typeof<VerifiedProviderPublisherTrustPolicySnapshot>.GetConstructors(), Is.Empty)
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() |> Seq.skip 2 do
            Assert.That((run vector).Sequence, Is.EqualTo(1L))

    [<Test>]
    member _.``CBI37 C5 current policy supersedes outstanding acquisition authorization``() =
        use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
        let authorityId = authority.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
                          |> ProviderPublisherTrustPolicyAuthorityId.create
        let registry = ProviderPublisherTrustPolicyRegistry authorityId
        let request = acquisitionRequest ()
        let issued = authorization request
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi37-{Guid.NewGuid():N}")
        try
            let store = ContentAddressedProviderStore(Path.Combine(root, "store"))
            let governed = GovernedProviderArtifactAcquirer(registry,
                TrustedProviderArtifactAcquirer(ProviderArtifactAcquirer(store, Path.Combine(root, "transactions"))))
            let unavailableSource = Cbi37Source(request.ExpectedSource, Encoding.UTF8.GetBytes "provider")
            let unavailable = governed.Acquire(request, unavailableSource, Some issued)
            let initial = policy false
            Assert.That((registry.Apply(sign authority 1L None initial "ECDSA-P256-SHA256")).IsApplied, Is.True)
            let admitted = governed.Acquire(request, unavailableSource, Some issued)
            Assert.That(store.Remove(request.Identity).Code, Is.EqualTo("removed"))
            let successor = policy true
            Assert.That((registry.Apply(sign authority 2L (Some initial.Identity) successor "ECDSA-P256-SHA256")).IsApplied, Is.True)
            let supersededSource = Cbi37Source(request.ExpectedSource, Encoding.UTF8.GetBytes "provider")
            let superseded = governed.Acquire(request, supersededSource, Some issued)
            let verified =
                { ContentIdentity = request.Identity; PublisherKeyId = ProviderPublisherKeyId.create (String('A', 64))
                  PayloadSha256 = ProviderArtifactPublisherManifest.digest request }
            Assert.Multiple(Action(fun () ->
                Assert.That(unavailable.TrustCode, Is.EqualTo("publisher-trust-policy-unavailable"))
                Assert.That(admitted.IsStaged, Is.True)
                Assert.That(superseded.TrustCode, Is.EqualTo("publisher-authorization-superseded"))
                Assert.That(supersededSource.Accesses, Is.Zero)
                Assert.That((ProviderPublisherTrustEvaluator.evaluate successor (Some verified)).Authorization.IsNone, Is.True)))
        finally
            if Directory.Exists root then
                Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                |> Seq.iter (fun file -> File.SetAttributes(file, FileAttributes.Normal))
                Directory.Delete(root, true)

    [<Test>]
    member _.``CBI37 C6 both roots agree on portable observations``() =
        use document = fixture ()
        let vectors = document.RootElement.GetProperty("vectors").EnumerateArray() |> Seq.toList
        let actual = vectors |> List.map (fun vector -> let x = run vector in x.Code, x.Sequence, x.Changed)
        let expected = vectors |> List.map (fun vector -> vector.GetProperty("code").GetString(), vector.GetProperty("sequence").GetInt64(), vector.GetProperty("changed").GetBoolean())
        Assert.That(actual, Is.EqualTo(expected :> obj))
