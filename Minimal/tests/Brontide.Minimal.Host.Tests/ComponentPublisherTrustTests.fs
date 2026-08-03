namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi35MemorySource(identity: ProviderArtifactSourceId, bytes: byte array) =
    let mutable opens = 0
    member _.OpenCount = opens
    interface IProviderArtifactSource with
        member _.Identity = identity
        member _.OpenRead path =
            opens <- opens + 1
            if path = "provider.bin" then Some(new MemoryStream(bytes, false) :> Stream) else None

[<TestFixture>]
type ComponentPublisherTrustTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI35 fixture value was missing." | present -> present

    let fixture () =
        JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "component-management", "fixtures", "cbi35-publisher-trust-vectors.json")))

    let policy (root: JsonElement) =
        let value = root.GetProperty "canonicalPolicy"
        let entries =
            value.GetProperty("entries").EnumerateArray()
            |> Seq.map (fun entry ->
                { PublisherKeyId = entry.GetProperty("publisherKeyId").GetString() |> required |> ProviderPublisherKeyId.create
                  Disposition = if entry.GetProperty("disposition").GetString() = "admitted" then Admitted else Revoked })
            |> Seq.toList
        { Identity = value.GetProperty("identity").GetString() |> required |> ProviderPublisherTrustPolicyId.create
          Entries = entries }

    let evidence (root: JsonElement) key =
        let value = root.GetProperty "verifiedEvidence"
        { ContentIdentity = value.GetProperty("contentIdentity").GetString() |> required |> ProviderArtifactSetId.create
          PublisherKeyId = String(key, 64) |> ProviderPublisherKeyId.create
          PayloadSha256 = value.GetProperty("payloadSha256").GetString() |> required }

    let run (root: JsonElement) (vector: JsonElement) =
        let canonical = policy root
        let mutation = vector.GetProperty("mutation").GetString() |> required
        let changed =
            match mutation with
            | "identity" -> { canonical with Identity = String('0', 64) |> ProviderPublisherTrustPolicyId.create }
            | "duplicate" -> { canonical with Entries = [ canonical.Entries.Head; canonical.Entries.Head ] }
            | "empty" -> { canonical with Entries = [] }
            | _ -> canonical
        ProviderPublisherTrustEvaluator.evaluate changed
            (if mutation = "unverified" then None
             else Some(evidence root ((vector.GetProperty("key").GetString() |> required).[0])))

    [<Test>]
    member _.``shared CBI35 vectors evaluate verified publishers without attempting admission``() =
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let result = run document.RootElement vector
            Assert.Multiple(Action(fun () ->
                Assert.That(result.Code, Is.EqualTo(vector.GetProperty("code").GetString()))
                Assert.That(result.IsTrusted, Is.EqualTo(vector.GetProperty("authorized").GetBoolean()))
                Assert.That(result.AdmissionCode, Is.EqualTo(document.RootElement.GetProperty("admissionCode").GetString()))))

    [<Test>]
    member _.``CBI35 C1 policy is a canonical immutable snapshot``() =
        use document = fixture ()
        let canonical = policy document.RootElement
        Assert.That(ProviderPublisherTrustPolicyIdentity.compute canonical.Entries, Is.EqualTo(canonical.Identity))
        Assert.That(ProviderPublisherTrustPolicyIdentity.compute (List.rev canonical.Entries), Is.EqualTo(canonical.Identity))
        let invalidMutations = Set [ "identity"; "duplicate"; "empty" ]
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            if invalidMutations.Contains(vector.GetProperty("mutation").GetString() |> required) then
                Assert.That((run document.RootElement vector).Authorization.IsNone, Is.True)

    [<Test>]
    member _.``CBI35 C2 only verified publisher evidence is eligible``() =
        use document = fixture ()
        let result = run document.RootElement (document.RootElement.GetProperty("vectors")[3])
        Assert.Multiple(Action(fun () ->
            Assert.That(result.Code, Is.EqualTo("publisher-evidence-not-verified"))
            Assert.That(result.PublisherKeyId.IsNone, Is.True)
            Assert.That(result.Authorization.IsNone, Is.True)))

    [<Test>]
    member _.``CBI35 C3 admitted revoked and unknown keys are distinct``() =
        use document = fixture ()
        let results = document.RootElement.GetProperty("vectors").EnumerateArray() |> Seq.take 3 |> Seq.map (run document.RootElement) |> Seq.toList
        Assert.That(results |> List.map _.Code, Is.EqualTo([ "publisher-trusted"; "publisher-key-revoked"; "publisher-key-unknown" ] :> obj))
        Assert.That(results |> List.map _.IsTrusted, Is.EqualTo([ true; false; false ] :> obj))

    [<Test>]
    member _.``CBI35 C4 trust evaluation is not artifact admission``() =
        use document = fixture ()
        let result = run document.RootElement (document.RootElement.GetProperty("vectors")[0])
        Assert.Multiple(Action(fun () ->
            Assert.That(result.EvidenceCode, Is.EqualTo("publisher-evidence-valid"))
            Assert.That(result.IsTrusted, Is.True)
            Assert.That(result.AdmissionCode, Is.EqualTo("admission-not-attempted"))))

    [<Test>]
    member _.``CBI35 C5 caller may require matching trust before CBI33 acquisition``() =
        use document = fixture ()
        let bytes = Encoding.UTF8.GetBytes "provider"
        let digest = SHA256.HashData bytes |> Convert.ToHexString
        let sourceId = ProviderArtifactSourceId.create "fixture://brontide/trusted-composition"
        let files: ProviderArtifactAcquisitionFile list = [ { RelativePath = "provider.bin"; Sha256 = digest; Length = int64 bytes.Length } ]
        let identity = ProviderArtifactSetIdentity.compute [ { RelativePath = "provider.bin"; Sha256 = digest } ] "provider.bin" []
        let request =
            { ExpectedSource = sourceId; Identity = identity; Files = files; ExecutablePath = "provider.bin"
              Arguments = []; MaxTotalBytes = int64 bytes.Length }: ProviderArtifactAcquisitionRequest
        let verified = { evidence document.RootElement 'A' with ContentIdentity = identity }
        let trust = ProviderPublisherTrustEvaluator.evaluate (policy document.RootElement) (Some verified)
        let testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi35-{Guid.NewGuid():N}")
        try
            let source = Cbi35MemorySource(sourceId, bytes)
            let acquisition =
                match trust.Authorization with
                | Some authorization when authorization.ContentIdentity = request.Identity ->
                    Some(ProviderArtifactAcquirer(ContentAddressedProviderStore(Path.Combine(testRoot, "store")),
                        Path.Combine(testRoot, "transactions")).Acquire(request, source))
                | _ -> None
            Assert.Multiple(Action(fun () ->
                Assert.That(trust.Authorization.Value.PayloadSha256, Is.EqualTo(verified.PayloadSha256))
                Assert.That(acquisition.Value.AdmissionCode, Is.EqualTo("staged"))
                Assert.That(source.OpenCount, Is.EqualTo(1))))
        finally
            if Directory.Exists testRoot then
                Directory.EnumerateFiles(testRoot, "*", SearchOption.AllDirectories)
                |> Seq.iter (fun file -> File.SetAttributes(file, FileAttributes.Normal))
                Directory.Delete(testRoot, true)

    [<Test>]
    member _.``CBI35 C6 both roots agree on portable observations``() =
        use document = fixture ()
        let vectors = document.RootElement.GetProperty("vectors").EnumerateArray() |> Seq.toList
        Assert.That(vectors |> List.map (run document.RootElement >> _.Code),
            Is.EqualTo(vectors |> List.map (fun vector -> vector.GetProperty("code").GetString()) :> obj))
