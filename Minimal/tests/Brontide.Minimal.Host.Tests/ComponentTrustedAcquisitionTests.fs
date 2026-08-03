namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi36Source(identity: ProviderArtifactSourceId, bytes: byte array) =
    let mutable identityReads = 0
    let mutable opens = 0
    member _.IdentityReads = identityReads
    member _.OpenCount = opens
    interface IProviderArtifactSource with
        member _.Identity =
            identityReads <- identityReads + 1
            identity
        member _.OpenRead path =
            opens <- opens + 1
            if path = "provider.bin" then Some(new MemoryStream(bytes, false) :> Stream) else None

type private Cbi36Observation =
    { TrustCode: string
      TransportCode: string
      AdmissionCode: string
      Staged: bool
      IdentityReads: int
      OpenCount: int
      Residue: bool }

[<TestFixture>]
type ComponentTrustedAcquisitionTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI36 fixture value was missing." | present -> present

    let request () =
        let bytes = Encoding.UTF8.GetBytes "provider"
        let digest = SHA256.HashData bytes |> Convert.ToHexString
        let files: ProviderArtifactAcquisitionFile list =
            [ { RelativePath = "provider.bin"; Sha256 = digest; Length = int64 bytes.Length } ]
        { ExpectedSource = ProviderArtifactSourceId.create "fixture://brontide/trusted-acquisition"
          Identity = ProviderArtifactSetIdentity.compute
              [ { RelativePath = "provider.bin"; Sha256 = digest } ] "provider.bin" []
          Files = files
          ExecutablePath = "provider.bin"
          Arguments = []
          MaxTotalBytes = int64 bytes.Length }: ProviderArtifactAcquisitionRequest

    let authorization (canonical: ProviderArtifactAcquisitionRequest) mutation =
        let key = ProviderPublisherKeyId.create (String('A', 64))
        let entries = [ { PublisherKeyId = key; Disposition = Admitted } ]
        let policy =
            { Identity = ProviderPublisherTrustPolicyIdentity.compute entries
              Entries = entries }
        let evidence =
            { ContentIdentity = if mutation = "content" then ProviderArtifactSetId.create (String('0', 64)) else canonical.Identity
              PublisherKeyId = key
              PayloadSha256 = if mutation = "payload" then String('0', 64) else ProviderArtifactPublisherManifest.digest canonical }
        (ProviderPublisherTrustEvaluator.evaluate policy (Some evidence)).Authorization.Value

    let fixture () =
        JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "component-management", "fixtures", "cbi36-trusted-acquisition-vectors.json")))

    let run (vector: JsonElement) =
        let mutation = vector.GetProperty("mutation").GetString() |> required
        let canonical = request ()
        let requested = if mutation = "invalid" then { canonical with MaxTotalBytes = 0L } else canonical
        let sourceId =
            if mutation = "source" then ProviderArtifactSourceId.create "fixture://brontide/other-source"
            else canonical.ExpectedSource
        let bytes = Encoding.UTF8.GetBytes(if mutation = "integrity" then "changed!" else "provider")
        let source = Cbi36Source(sourceId, bytes)
        let testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi36-{Guid.NewGuid():N}")
        try
            let store = ContentAddressedProviderStore(Path.Combine(testRoot, "store"))
            let gate = TrustedProviderArtifactAcquirer(
                ProviderArtifactAcquirer(store, Path.Combine(testRoot, "transactions")))
            let result = gate.Acquire(requested, source,
                if mutation = "missing" then None else Some(authorization canonical mutation))
            let transactionRoot = Path.Combine(testRoot, "transactions")
            let observation =
                { TrustCode = result.TrustCode
                  TransportCode = result.TransportCode
                  AdmissionCode = result.AdmissionCode
                  Staged = result.IsStaged
                  IdentityReads = source.IdentityReads
                  OpenCount = source.OpenCount
                  Residue = Directory.Exists transactionRoot && not (Seq.isEmpty (Directory.EnumerateFileSystemEntries transactionRoot)) }
            if result.IsStaged then Assert.That(store.Remove(canonical.Identity).Code, Is.EqualTo("removed"))
            observation
        finally
            if Directory.Exists testRoot then
                Directory.EnumerateFiles(testRoot, "*", SearchOption.AllDirectories)
                |> Seq.iter (fun file -> File.SetAttributes(file, FileAttributes.Normal))
                Directory.Delete(testRoot, true)

    [<Test>]
    member _.``CBI36 C1 trusted authorization is issuer controlled``() =
        Assert.That(typeof<TrustedProviderPublisherAuthorization>.GetConstructors(), Is.Empty)

    [<Test>]
    member _.``shared CBI36 vectors gate acquisition before source access``() =
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let actual = run vector
            Assert.Multiple(Action(fun () ->
                Assert.That(actual.TrustCode, Is.EqualTo(vector.GetProperty("trustCode").GetString()))
                Assert.That(actual.TransportCode, Is.EqualTo(vector.GetProperty("transportCode").GetString()))
                Assert.That(actual.AdmissionCode, Is.EqualTo(vector.GetProperty("admissionCode").GetString()))
                Assert.That(actual.Staged, Is.EqualTo(vector.GetProperty("staged").GetBoolean()))
                Assert.That(actual.IdentityReads, Is.EqualTo(vector.GetProperty("identityReads").GetInt32()))
                Assert.That(actual.OpenCount, Is.EqualTo(vector.GetProperty("openCount").GetInt32()))
                Assert.That(actual.Residue, Is.False)))

    [<Test>]
    member _.``CBI36 C2 complete request is validated before trust composition``() =
        use document = fixture ()
        let actual = run (document.RootElement.GetProperty("vectors")[4])
        Assert.Multiple(Action(fun () ->
            Assert.That(actual.TrustCode, Is.EqualTo("publisher-trust-not-evaluated"))
            Assert.That(actual.TransportCode, Is.EqualTo("acquisition-invalid"))
            Assert.That(actual.IdentityReads + actual.OpenCount, Is.Zero)))

    [<Test>]
    member _.``CBI36 C3 authorization matches exact content and payload``() =
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        Assert.That((run vectors[2]).TrustCode, Is.EqualTo("publisher-authorization-content-mismatch"))
        Assert.That((run vectors[3]).TrustCode, Is.EqualTo("publisher-authorization-payload-mismatch"))

    [<Test>]
    member _.``CBI36 C4 trust succeeds before any source access``() =
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        for index in [ 1; 2; 3 ] do
            let actual = run vectors[index]
            Assert.That(actual.IdentityReads + actual.OpenCount, Is.Zero)
        let mismatch = run vectors[5]
        Assert.Multiple(Action(fun () ->
            Assert.That(mismatch.TrustCode, Is.EqualTo("publisher-trusted"))
            Assert.That(mismatch.TransportCode, Is.EqualTo("acquisition-source-mismatch"))))

    [<Test>]
    member _.``CBI36 C5 trusted composition preserves CBI33 and CBI32 outcomes``() =
        use document = fixture ()
        let vectors = document.RootElement.GetProperty "vectors"
        let staged = run vectors[0]
        let rejected = run vectors[6]
        Assert.Multiple(Action(fun () ->
            Assert.That(staged.Staged, Is.True)
            Assert.That(staged.AdmissionCode, Is.EqualTo("staged"))
            Assert.That(rejected.TrustCode, Is.EqualTo("publisher-trusted"))
            Assert.That(rejected.TransportCode, Is.EqualTo("transport-completed"))
            Assert.That(rejected.AdmissionCode, Is.EqualTo("artifact-set-integrity-failed"))))

    [<Test>]
    member _.``CBI36 C6 both roots agree on portable observations``() =
        use document = fixture ()
        let vectors = document.RootElement.GetProperty("vectors").EnumerateArray() |> Seq.toList
        let actual = vectors |> List.map (fun vector ->
            let value = run vector
            value.TrustCode, value.TransportCode, value.AdmissionCode, value.Staged, value.IdentityReads, value.OpenCount)
        let expected = vectors |> List.map (fun vector ->
            vector.GetProperty("trustCode").GetString(),
            vector.GetProperty("transportCode").GetString(),
            vector.GetProperty("admissionCode").GetString(),
            vector.GetProperty("staged").GetBoolean(),
            vector.GetProperty("identityReads").GetInt32(),
            vector.GetProperty("openCount").GetInt32())
        Assert.That(actual, Is.EqualTo(expected :> obj))
