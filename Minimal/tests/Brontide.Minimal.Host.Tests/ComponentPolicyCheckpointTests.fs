namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi38Observation = { Code: string; Sequence: int64; TemporaryResidue: bool }

type private Cbi38Source(identity: ProviderArtifactSourceId, bytes: byte array) =
    interface IProviderArtifactSource with
        member _.Identity = identity
        member _.OpenRead path =
            if path = "provider.bin" then Some(new MemoryStream(bytes, false) :> Stream) else None

[<TestFixture>]
type ComponentPolicyCheckpointTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI38 fixture value was missing." | present -> present

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi38-policy-checkpoint-vectors.json")))

    let policy revoked : ProviderPublisherTrustPolicy =
        let entries = [ { PublisherKeyId = ProviderPublisherKeyId.create (String('A', 64))
                          Disposition = if revoked then Revoked else Admitted } ]
        { Identity = ProviderPublisherTrustPolicyIdentity.compute entries; Entries = entries }

    let sign (key: ECDsa) sequence previous (policy: ProviderPublisherTrustPolicy) =
        let publicKey = key.ExportSubjectPublicKeyInfo()
        let signature = key.SignData(
            ProviderPublisherTrustPolicyUpdateManifest.encode sequence previous policy.Identity,
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
        { Sequence = sequence; PreviousPolicyIdentity = previous; Policy = policy; Algorithm = "ECDSA-P256-SHA256"
          AuthorityPublicKeySpkiBase64 = Convert.ToBase64String publicKey
          SignatureBase64 = Convert.ToBase64String signature }

    let authorityId (key: ECDsa) =
        key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyAuthorityId.create

    let acquisitionRequest () : ProviderArtifactAcquisitionRequest =
        let bytes = Encoding.UTF8.GetBytes "provider"
        let digest = SHA256.HashData bytes |> Convert.ToHexString
        { ExpectedSource = ProviderArtifactSourceId.create "fixture://brontide/policy-checkpoint"
          Identity = ProviderArtifactSetIdentity.compute [ { RelativePath = "provider.bin"; Sha256 = digest } ] "provider.bin" []
          Files = [ { RelativePath = "provider.bin"; Sha256 = digest; Length = int64 bytes.Length } ]
          ExecutablePath = "provider.bin"; Arguments = []; MaxTotalBytes = int64 bytes.Length }

    let authorization (request: ProviderArtifactAcquisitionRequest) =
        let evidence =
            { ContentIdentity = request.Identity
              PublisherKeyId = ProviderPublisherKeyId.create (String('A', 64))
              PayloadSha256 = ProviderArtifactPublisherManifest.digest request }
        (ProviderPublisherTrustEvaluator.evaluate (policy false) (Some evidence)).Authorization.Value

    let findBytes (bytes: byte array) (pattern: byte array) =
        seq { 0 .. bytes.Length - pattern.Length }
        |> Seq.tryFind (fun offset -> bytes.AsSpan(offset, pattern.Length).SequenceEqual(pattern))
        |> Option.defaultValue -1

    let run (vector: JsonElement) =
        let mutation = vector.GetProperty("mutation").GetString() |> required
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi38-{Guid.NewGuid():N}")
        let path = Path.Combine(root, "policy.checkpoint")
        Directory.CreateDirectory root |> ignore
        try
            use authorityKey = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let authority = authorityId authorityKey
            let emptyCode, emptyRegistry, _ = DurableProviderPublisherTrustPolicyRegistry.Open(path, authority, None)
            if mutation = "empty" then
                { Code = emptyCode; Sequence = 0L; TemporaryResidue = File.Exists(path + ".tmp") }
            else
                let durable = emptyRegistry.Value
                let initial = policy false
                let firstUpdate = sign authorityKey 1L None initial
                let first = durable.Apply firstUpdate
                Assert.That(first.IsApplied, Is.True)
                let firstBytes = File.ReadAllBytes path
                if mutation = "missing-rollback" then
                    File.Delete path
                    let code, _, _ = DurableProviderPublisherTrustPolicyRegistry.Open(path, authority, Some first.Floor)
                    { Code = code; Sequence = 0L; TemporaryResidue = File.Exists(path + ".tmp") }
                elif mutation = "recovered" || mutation = "old-rollback" then
                    let second = durable.Apply(sign authorityKey 2L (Some initial.Identity) (policy true))
                    Assert.That(second.IsApplied, Is.True)
                    if mutation = "old-rollback" then
                        File.WriteAllBytes(path, firstBytes)
                        let code, _, _ = DurableProviderPublisherTrustPolicyRegistry.Open(path, authority, Some second.Floor)
                        { Code = code; Sequence = 0L; TemporaryResidue = File.Exists(path + ".tmp") }
                    else
                        let code, recovered, _ = DurableProviderPublisherTrustPolicyRegistry.Open(path, authority, Some second.Floor)
                        { Code = code; Sequence = recovered.Value.Current.Value.Sequence; TemporaryResidue = File.Exists(path + ".tmp") }
                else
                    let bytes = File.ReadAllBytes path
                    match mutation with
                    | "truncated" -> File.WriteAllBytes(path, bytes[.. bytes.Length / 2 - 1])
                    | "trailing" -> File.WriteAllBytes(path, Array.append bytes [| 0uy |])
                    | "signature" ->
                        let signature = Encoding.UTF8.GetBytes firstUpdate.SignatureBase64
                        let offset = findBytes bytes signature
                        Assert.That(offset, Is.GreaterThanOrEqualTo(0))
                        bytes[offset] <- if bytes[offset] = byte 'A' then byte 'B' else byte 'A'
                        File.WriteAllBytes(path, bytes)
                    | _ -> ()
                    let code, _, _ = DurableProviderPublisherTrustPolicyRegistry.Open(path, authority, None)
                    { Code = code; Sequence = 0L; TemporaryResidue = File.Exists(path + ".tmp") }
        finally
            if Directory.Exists root then Directory.Delete(root, true)

    [<Test>]
    member _.``shared CBI38 vectors recover only complete non rolled back checkpoints``() =
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let actual = run vector
            Assert.Multiple(Action(fun () ->
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()))
                Assert.That(actual.Sequence, Is.EqualTo(vector.GetProperty("sequence").GetInt64()))
                Assert.That(actual.TemporaryResidue, Is.False)))

    [<Test>]
    member _.``CBI38 C1 bounded checkpoint contains the complete signed chain``() =
        use document = fixture ()
        Assert.That((run (document.RootElement.GetProperty("vectors")[1])).Sequence, Is.EqualTo(2L))

        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi38-bound-{Guid.NewGuid():N}")
        try
            use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let entries =
                [ for index in 0..4096 ->
                    { PublisherKeyId = ProviderPublisherKeyId.create (index.ToString("X64")); Disposition = Admitted } ]
            let oversized = { Identity = ProviderPublisherTrustPolicyIdentity.compute entries; Entries = entries }
            let _, opened, _ = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), authorityId authority, None)
            let bounded = opened.Value.Apply(sign authority 1L None oversized)
            Assert.Multiple(Action(fun () ->
                Assert.That(bounded.Code, Is.EqualTo("policy-checkpoint-write-failed"))
                Assert.That(opened.Value.Current.IsNone, Is.True)))
        finally
            if Directory.Exists root then Directory.Delete(root, true)

    [<Test>]
    member _.``CBI38 C2 recovery reverifies provenance and the complete chain``() =
        use document = fixture ()
        Assert.That((run (document.RootElement.GetProperty("vectors")[4])).Code,
                    Is.EqualTo("policy-checkpoint-invalid-chain"))

    [<Test>]
    member _.``CBI38 C3 publication is atomic and crash residue is inert``() =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi38-residue-{Guid.NewGuid():N}")
        Directory.CreateDirectory root |> ignore
        let path = Path.Combine(root, "policy.checkpoint")
        try
            File.WriteAllText(path + ".tmp", "partial")
            use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let code, _, _ = DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId authority, None)
            Assert.Multiple(Action(fun () ->
                Assert.That(code, Is.EqualTo("policy-checkpoint-empty"))
                Assert.That(File.Exists(path + ".tmp"), Is.False)))
        finally
            Directory.Delete(root, true)

    [<Test>]
    member _.``CBI38 C4 external recovery floor detects checkpoint rollback``() =
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        Assert.That((run vectors[5]).Code, Is.EqualTo("policy-checkpoint-rollback-detected"))
        Assert.That((run vectors[6]).Code, Is.EqualTo("policy-checkpoint-rollback-detected"))
        Assert.That(typeof<ProviderPublisherTrustPolicyRecoveryFloor>.GetConstructors(), Is.Empty)

    [<Test>]
    member _.``CBI38 C5 checkpoint failure preserves live state and recovered state governs acquisition``() =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi38-write-{Guid.NewGuid():N}")
        Directory.CreateDirectory root |> ignore
        let path = Path.Combine(root, "blocked")
        try
            use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let _, opened, _ = DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId authority, None)
            let durable = opened.Value
            Directory.CreateDirectory path |> ignore
            let failed = durable.Apply(sign authority 1L None (policy false))
            Assert.Multiple(Action(fun () ->
                Assert.That(failed.Code, Is.EqualTo("policy-checkpoint-write-failed"))
                Assert.That(durable.Current.IsNone, Is.True)
                Assert.That(failed.Floor.Sequence, Is.Zero)))

            Directory.Delete path
            let checkpoint = Path.Combine(root, "policy.checkpoint")
            let _, firstRegistry, _ = DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, authorityId authority, None)
            let applied = firstRegistry.Value.Apply(sign authority 1L None (policy false))
            let _, recovered, _ = DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, authorityId authority, Some applied.Floor)
            let request = acquisitionRequest ()
            let store = ContentAddressedProviderStore(Path.Combine(root, "store"))
            let governed = recovered.Value.Govern(
                TrustedProviderArtifactAcquirer(ProviderArtifactAcquirer(store, Path.Combine(root, "transactions"))))
            let acquired = governed.Acquire(request,
                Cbi38Source(request.ExpectedSource, Encoding.UTF8.GetBytes "provider"), Some(authorization request))
            Assert.That(acquired.IsStaged, Is.True)
        finally
            if Directory.Exists root then
                Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                |> Seq.iter (fun file -> File.SetAttributes(file, FileAttributes.Normal))
                Directory.Delete(root, true)

    [<Test>]
    member _.``CBI38 C6 both roots agree on portable observations``() =
        use document = fixture ()
        let vectors = document.RootElement.GetProperty("vectors").EnumerateArray() |> Seq.toList
        let actual = vectors |> List.map (fun vector -> let value = run vector in value.Code, value.Sequence)
        let expected = vectors |> List.map (fun vector -> vector.GetProperty("code").GetString(), vector.GetProperty("sequence").GetInt64())
        Assert.That(actual, Is.EqualTo(expected :> obj))
