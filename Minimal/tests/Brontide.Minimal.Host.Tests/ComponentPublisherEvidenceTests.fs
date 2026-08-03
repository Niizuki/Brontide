namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi34Observation =
    { Code: string
      Verified: bool
      PayloadSha256: string
      PublisherKeyId: string option
      TrustCode: string
      AdmissionCode: string }

type private Cbi34MemorySource(identity: ProviderArtifactSourceId, members: Map<string, byte array>) =
    interface IProviderArtifactSource with
        member _.Identity = identity
        member _.OpenRead path =
            members |> Map.tryFind path |> Option.map (fun bytes -> new MemoryStream(bytes, false) :> Stream)

[<TestFixture>]
type ComponentPublisherEvidenceTests() =
    let required (value: string | null) =
        match value with
        | null -> failwith "A CBI34 fixture value was missing."
        | present -> present

    let fixture () =
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "component-management",
            "fixtures",
            "cbi34-publisher-evidence-vectors.json")))

    let canonicalRequest (manifest: JsonElement) =
        let files =
            manifest.GetProperty("files").EnumerateArray()
            |> Seq.map (fun file ->
                { RelativePath = file.GetProperty("path").GetString() |> required
                  Sha256 = file.GetProperty("sha256").GetString() |> required
                  Length = file.GetProperty("length").GetInt64() }: ProviderArtifactAcquisitionFile)
            |> Seq.toList
        { ExpectedSource = ProviderArtifactSourceId.create "fixture://brontide/publisher-evidence"
          Identity = manifest.GetProperty("identity").GetString() |> required |> ProviderArtifactSetId.create
          Files = files
          ExecutablePath = manifest.GetProperty("executablePath").GetString() |> required
          Arguments = manifest.GetProperty("arguments").EnumerateArray() |> Seq.map (fun value -> value.GetString() |> required) |> Seq.toList
          MaxTotalBytes = files |> List.sumBy _.Length }: ProviderArtifactAcquisitionRequest

    let mutate (request: ProviderArtifactAcquisitionRequest) mutation =
        let files =
            match mutation with
            | "path" -> request.Files |> List.map (fun file ->
                if file.RelativePath = "data/config.json" then { file with RelativePath = "data/settings.json" } else file)
            | "digest" -> { request.Files.Head with Sha256 = String('C', 64) } :: request.Files.Tail
            | "length" -> { request.Files.Head with Length = request.Files.Head.Length + 1L } :: request.Files.Tail
            | _ -> request.Files
        let executable =
            if mutation = "executable" then files |> List.find (fun file -> file.RelativePath <> request.ExecutablePath) |> _.RelativePath
            else request.ExecutablePath
        let arguments = if mutation = "arguments" then request.Arguments @ [ "--changed" ] else request.Arguments
        let artifacts = files |> List.map (fun file ->
            { RelativePath = file.RelativePath; Sha256 = file.Sha256 }: ProviderArtifactFile)
        { request with
            Identity = ProviderArtifactSetIdentity.compute artifacts executable arguments
            Files = files
            ExecutablePath = executable
            Arguments = arguments
            MaxTotalBytes = files |> List.sumBy _.Length }

    let sign (request: ProviderArtifactAcquisitionRequest) (key: ECDsa) mutation =
        let publicKey = key.ExportSubjectPublicKeyInfo()
        let keyId = SHA256.HashData publicKey |> Convert.ToHexString |> ProviderPublisherKeyId.create
        let signature = key.SignData(
            ProviderArtifactPublisherManifest.encode request,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence)
        let evidence =
            { PublisherKeyId = keyId
              Algorithm = "ECDSA-P256-SHA256"
              PublicKeySpkiBase64 = Convert.ToBase64String publicKey
              SignatureBase64 = Convert.ToBase64String signature }
        match mutation with
        | "algorithm" -> { evidence with Algorithm = "unknown" }
        | "key" -> { evidence with PublicKeySpkiBase64 = "not-base64" }
        | "key-id" -> { evidence with PublisherKeyId = ProviderPublisherKeyId.create (String('0', 64)) }
        | "signature" ->
            let changed = key.SignData(
                Encoding.UTF8.GetBytes "different payload",
                HashAlgorithmName.SHA256,
                DSASignatureFormat.Rfc3279DerSequence)
            { evidence with SignatureBase64 = Convert.ToBase64String changed }
        | _ -> evidence

    let run (manifest: JsonElement) (vector: JsonElement) =
        let mutation = vector.GetProperty("mutation").GetString() |> required
        let original = canonicalRequest manifest
        use key = ECDsa.Create(ECCurve.NamedCurves.nistP256)
        let evidence = sign original key mutation
        let request =
            if [ "path"; "digest"; "length"; "executable"; "arguments" ] |> List.contains mutation then
                mutate original mutation
            else original
        let result = ProviderArtifactPublisherEvidenceVerifier.verify request (if mutation = "absent" then None else Some evidence)
        { Code = result.Code
          Verified = result.IsVerified
          PayloadSha256 = result.PayloadSha256
          PublisherKeyId = result.PublisherKeyId |> Option.map ProviderPublisherKeyId.value
          TrustCode = result.TrustCode
          AdmissionCode = result.AdmissionCode }

    [<Test>]
    member _.``shared CBI34 vectors verify publisher evidence without granting trust``() =
        use document = fixture ()
        let manifest = document.RootElement.GetProperty("canonicalManifest")
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let observation = run manifest vector
            Assert.Multiple(Action(fun () ->
                Assert.That(observation.Code, Is.EqualTo(vector.GetProperty("code").GetString()))
                Assert.That(observation.Verified, Is.EqualTo(vector.GetProperty("verified").GetBoolean()))
                Assert.That(observation.TrustCode, Is.EqualTo("publisher-trust-not-evaluated"))
                Assert.That(observation.AdmissionCode, Is.EqualTo("admission-not-attempted"))))

    [<Test>]
    member _.``CBI34 C1 canonical payload covers the complete acquisition manifest``() =
        use document = fixture ()
        let manifest = document.RootElement.GetProperty("canonicalManifest")
        Assert.That(
            ProviderArtifactPublisherManifest.digest (canonicalRequest manifest),
            Is.EqualTo(manifest.GetProperty("payloadSha256").GetString()))
        let request = canonicalRequest manifest
        let hostPolicyChanged =
            { request with
                ExpectedSource = ProviderArtifactSourceId.create "fixture://brontide/other-source"
                MaxTotalBytes = request.MaxTotalBytes + 100L }
        Assert.That(ProviderArtifactPublisherManifest.encode hostPolicyChanged,
                    Is.EqualTo(ProviderArtifactPublisherManifest.encode request :> obj))

    [<Test>]
    member _.``CBI34 C2 evidence has an explicit key identity and algorithm``() =
        use document = fixture ()
        let manifest = document.RootElement.GetProperty("canonicalManifest")
        let ids = Set [ "cbi34-02-not-provided"; "cbi34-03-unsupported"; "cbi34-04-malformed-key"; "cbi34-05-key-id-mismatch" ]
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            if ids.Contains(vector.GetProperty("id").GetString() |> required) then
                let observation = run manifest vector
                Assert.That(observation.Code, Is.EqualTo(vector.GetProperty("code").GetString()))
                Assert.That(observation.Verified, Is.False)
        let request = canonicalRequest manifest
        use wrongCurve = ECDsa.Create(ECCurve.NamedCurves.nistP384)
        let wrongCurveEvidence = sign request wrongCurve "none"
        Assert.That(
            (ProviderArtifactPublisherEvidenceVerifier.verify request (Some wrongCurveEvidence)).Code,
            Is.EqualTo("publisher-evidence-malformed"))

    [<Test>]
    member _.``CBI34 C3 verification binds the exact canonical payload``() =
        use document = fixture ()
        let manifest = document.RootElement.GetProperty("canonicalManifest")
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let id = vector.GetProperty("id").GetString() |> required
            if id.CompareTo("cbi34-06") >= 0 then
                Assert.That((run manifest vector).Code, Is.EqualTo("publisher-evidence-invalid"))

    [<Test>]
    member _.``CBI34 C4 validity is not trust or admission``() =
        use document = fixture ()
        let observation = run (document.RootElement.GetProperty("canonicalManifest"))
                              (document.RootElement.GetProperty("vectors")[0])
        Assert.Multiple(Action(fun () ->
            Assert.That(observation.Verified, Is.True)
            Assert.That(observation.TrustCode, Is.EqualTo("publisher-trust-not-evaluated"))
            Assert.That(observation.AdmissionCode, Is.EqualTo("admission-not-attempted"))))

    [<Test>]
    member _.``CBI34 C5 explicit caller policy may compose valid evidence with CBI33``() =
        let bytes = Encoding.UTF8.GetBytes "provider"
        let digest = SHA256.HashData bytes |> Convert.ToHexString
        let files: ProviderArtifactAcquisitionFile list =
            [ { RelativePath = "provider.bin"; Sha256 = digest; Length = int64 bytes.Length } ]
        let artifacts: ProviderArtifactFile list =
            [ { RelativePath = "provider.bin"; Sha256 = digest } ]
        let sourceId = ProviderArtifactSourceId.create "fixture://brontide/composition"
        let request: ProviderArtifactAcquisitionRequest =
            { ExpectedSource = sourceId
              Identity = ProviderArtifactSetIdentity.compute artifacts "provider.bin" []
              Files = files
              ExecutablePath = "provider.bin"
              Arguments = []
              MaxTotalBytes = int64 bytes.Length }
        use key = ECDsa.Create(ECCurve.NamedCurves.nistP256)
        let verification = ProviderArtifactPublisherEvidenceVerifier.verify request (Some(sign request key "none"))
        let testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi34-{Guid.NewGuid():N}")
        try
            let store = ContentAddressedProviderStore(Path.Combine(testRoot, "store"))
            let source = Cbi34MemorySource(sourceId, Map [ "provider.bin", bytes ])
            let acquisition = ProviderArtifactAcquirer(store, Path.Combine(testRoot, "transactions")).Acquire(request, source)
            Assert.Multiple(Action(fun () ->
                Assert.That(verification.IsVerified, Is.True)
                Assert.That(verification.AdmissionCode, Is.EqualTo("admission-not-attempted"))
                Assert.That(acquisition.TransportCode, Is.EqualTo("transport-completed"))
                Assert.That(acquisition.AdmissionCode, Is.EqualTo("staged"))))
            Assert.That(store.Remove(request.Identity).Code, Is.EqualTo("removed"))
        finally
            if Directory.Exists testRoot then
                Directory.EnumerateFiles(testRoot, "*", SearchOption.AllDirectories)
                |> Seq.iter (fun file -> File.SetAttributes(file, FileAttributes.Normal))
                Directory.Delete(testRoot, true)

    [<Test>]
    member _.``CBI34 C6 both roots agree on portable observations``() =
        use document = fixture ()
        let manifest = document.RootElement.GetProperty("canonicalManifest")
        let actual = document.RootElement.GetProperty("vectors").EnumerateArray() |> Seq.map (run manifest >> _.Code) |> Seq.toList
        let expected: string list =
            document.RootElement.GetProperty("vectors").EnumerateArray()
            |> Seq.map (fun vector -> vector.GetProperty("code").GetString() |> required)
            |> Seq.toList
        Assert.That(actual, Is.EqualTo(expected :> obj))
