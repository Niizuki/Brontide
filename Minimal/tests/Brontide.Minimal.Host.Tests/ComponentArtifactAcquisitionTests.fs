namespace Brontide.Minimal.Host.Tests

open System
open System.Collections.Generic
open System.IO
open System.Security.Cryptography
open System.Text.Json
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi33Observation =
    { SourceIdentity: string
      TransportCode: string
      PublisherEvidenceCode: string
      AdmissionCode: string
      Staged: bool
      Reused: bool
      Activated: bool
      RemovalCode: string
      Residue: bool
      OpenCount: int
      Bounded: bool }

type private Cbi33CountingReadStream(inner: Stream, count: int64 -> unit) =
    inherit Stream()

    override _.CanRead = inner.CanRead
    override _.CanSeek = inner.CanSeek
    override _.CanWrite = false
    override _.Length = inner.Length
    override _.Position with get () = inner.Position and set value = inner.Position <- value
    override _.Flush() = inner.Flush()
    override _.Read(buffer: byte array, offset: int, readCount: int) =
        let read = inner.Read(buffer, offset, readCount)
        count (int64 read)
        read
    override _.Read(buffer: Span<byte>) =
        let read = inner.Read buffer
        count (int64 read)
        read
    override _.ReadByte() =
        let value = inner.ReadByte()
        if value >= 0 then count 1L
        value
    override _.Seek(offset, origin) = inner.Seek(offset, origin)
    override _.SetLength(_value) = raise (NotSupportedException())
    override _.Write(_buffer, _offset, _count) = raise (NotSupportedException())
    override this.Dispose(disposing) =
        if disposing then inner.Dispose()
        base.Dispose(disposing)

type private Cbi33FailingStream(bytes: byte array) =
    inherit MemoryStream(bytes)

    override _.Read(_buffer: byte array, _offset: int, _count: int) =
        raise (IOException "fixture read failure")

    override _.Read(_buffer: Span<byte>) =
        raise (IOException "fixture read failure")

type private Cbi33MemorySource(identity: ProviderArtifactSourceId, members: Map<string, unit -> Stream option>) =
    let mutable openCount = 0
    let mutable bytesRead = 0L

    member _.OpenCount = openCount
    member _.BytesRead = bytesRead

    interface IProviderArtifactSource with
        member _.Identity = identity
        member _.OpenRead relativePath =
            openCount <- openCount + 1
            members
            |> Map.tryFind relativePath
            |> Option.bind (fun openMember -> openMember ())
            |> Option.map (fun stream ->
                new Cbi33CountingReadStream(stream, fun count -> bytesRead <- bytesRead + count) :> Stream)

[<TestFixture>]
type ComponentArtifactAcquisitionTests() =
    let required (value: string | null) =
        match value with
        | null -> failwith "A CBI33 fixture value was missing."
        | present -> present

    let deleteTree path =
        let rec remove attempt =
            if Directory.Exists path then
                try
                    Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    |> Seq.iter (fun file -> File.SetAttributes(file, FileAttributes.Normal))
                    Directory.Delete(path, true)
                with
                | :? IOException when attempt < 9 ->
                    Threading.Thread.Sleep 25
                    remove (attempt + 1)
                | :? UnauthorizedAccessException when attempt < 9 ->
                    Threading.Thread.Sleep 25
                    remove (attempt + 1)
        remove 0

    let providerPath provider =
        let variable = if provider = "reference" then "BRONTIDE_REFERENCE_PROVIDER" else "BRONTIDE_MINIMAL_PROVIDER"
        match Environment.GetEnvironmentVariable variable |> Option.ofObj with
        | Some path when File.Exists path -> Path.GetFullPath path
        | _ ->
            Assert.Ignore($"{variable} does not name a built provider endpoint.")
            failwith "The cross-process test was ignored."

    let input provider mutation =
        let executable = providerPath provider
        let providerRoot = Path.GetDirectoryName executable |> required
        let bytes =
            Directory.EnumerateFiles providerRoot
            |> Seq.sort
            |> Seq.map (fun path -> Path.GetFileName path |> required, File.ReadAllBytes path)
            |> Map.ofSeq
        let mutable files =
            bytes
            |> Map.toList
            |> List.map (fun (path, content) ->
                { RelativePath = path
                  Sha256 = SHA256.HashData content |> Convert.ToHexString
                  Length = int64 content.LongLength }: ProviderArtifactAcquisitionFile)
        if mutation = "digest" then
            files <- { files.Head with Sha256 = String('0', 64) } :: files.Tail
        let expected = ProviderArtifactSourceId.create "fixture://brontide/provider-output"
        let actual =
            if mutation = "source-mismatch" then ProviderArtifactSourceId.create "fixture://brontide/other-output"
            else expected
        let first = files.Head.RelativePath
        let members =
            bytes
            |> Map.map (fun path content ->
                fun () ->
                    if mutation = "missing" && path = first then None
                    elif mutation = "read-failure" && path = first then
                        Some(new Cbi33FailingStream(content) :> Stream)
                    else
                        let supplied =
                            if mutation = "short" && path = first then content[..content.Length - 2]
                            elif mutation = "long" && path = first then Array.append content [| 0x7Fuy |]
                            else content
                        Some(new MemoryStream(supplied, false) :> Stream))
        let total = files |> List.sumBy _.Length
        let limit = if mutation = "budget" then total - 1L else total
        let artifactFiles = files |> List.map (fun file ->
            { RelativePath = file.RelativePath; Sha256 = file.Sha256 }: ProviderArtifactFile)
        let request: ProviderArtifactAcquisitionRequest =
            { ExpectedSource = expected
              Identity = ProviderArtifactSetIdentity.compute artifactFiles (Path.GetFileName executable |> required) [ "--portable" ]
              Files = files
              ExecutablePath = Path.GetFileName executable |> required
              Arguments = [ "--portable" ]
              MaxTotalBytes = limit }
        request, Cbi33MemorySource(actual, members)

    let vector id =
        use fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "component-management",
            "fixtures",
            "cbi33-attributable-acquisition-vectors.json")))
        fixture.RootElement.GetProperty("vectors").EnumerateArray()
        |> Seq.find (fun candidate -> candidate.GetProperty("id").GetString() = id)
        |> _.Clone()

    let run (item: JsonElement) =
        let testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi33-{Guid.NewGuid():N}")
        try
            let request, source =
                input (item.GetProperty("provider").GetString() |> required) (item.GetProperty("mutation").GetString() |> required)
            let store = ContentAddressedProviderStore(Path.Combine(testRoot, "store"))
            let transactionRoot = Path.Combine(testRoot, "transactions")
            let acquirer = ProviderArtifactAcquirer(store, transactionRoot)
            let result = acquirer.Acquire(request, source)
            let mutable reused = false
            let mutable activated = false
            let mutable removalCode = "artifact-set-not-staged"
            match result.Staged with
            | Some staged ->
                match acquirer.Acquire(request, source).Staged with
                | Some restaged -> reused <- restaged.Reused
                | None -> failwith "CBI33 restaging was refused."
                match store.Activate(staged, [ "--portable" ]) with
                | StagedProviderActivation.Launched owner ->
                    activated <- true
                    owner.Dispose()
                | StagedProviderActivation.Refused failure -> failwithf "CBI33 activation failed: %s" failure.Code
                removalCode <- store.Remove(request.Identity).Code
            | None -> ()
            { SourceIdentity = ProviderArtifactSourceId.value result.SourceIdentity
              TransportCode = result.TransportCode
              PublisherEvidenceCode = result.PublisherEvidenceCode
              AdmissionCode = result.AdmissionCode
              Staged = result.IsStaged
              Reused = reused
              Activated = activated
              RemovalCode = removalCode
              Residue = Directory.Exists transactionRoot && (Directory.EnumerateFileSystemEntries transactionRoot |> Seq.isEmpty |> not)
              OpenCount = source.OpenCount
              Bounded =
                  let attempts = if result.IsStaged then 2L else 1L
                  source.BytesRead <= attempts * (request.MaxTotalBytes + int64 request.Files.Length) }
        finally
            deleteTree testRoot

    [<Test; Category("CrossProcess")>]
    member _.``shared CBI33 vectors acquire attributable bounded artifacts``() =
        use fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi33-attributable-acquisition-vectors.json")))
        for item in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
            let observation = run item
            Assert.Multiple(Action(fun () ->
                Assert.That(observation.TransportCode, Is.EqualTo(item.GetProperty("transportCode").GetString()))
                Assert.That(observation.AdmissionCode, Is.EqualTo(item.GetProperty("admissionCode").GetString()))
                Assert.That(observation.Staged, Is.EqualTo(item.GetProperty("staged").GetBoolean()))
                Assert.That(observation.PublisherEvidenceCode, Is.EqualTo("publisher-evidence-not-evaluated"))
                Assert.That(observation.Residue, Is.False)
                Assert.That(observation.Bounded, Is.True)))

    [<Test; Category("CrossProcess")>]
    member _.``CBI33 C1 declaration is complete and bounded``() =
        let observation = run (vector "cbi33-09-invalid-budget")
        Assert.Multiple(Action(fun () ->
            Assert.That(observation.TransportCode, Is.EqualTo("acquisition-invalid"))
            Assert.That(observation.OpenCount, Is.Zero)
            Assert.That(observation.Residue, Is.False)
            Assert.That(observation.Bounded, Is.True)))

    [<TestCase("cbi33-03-source-mismatch", "acquisition-source-mismatch", 0)>]
    [<TestCase("cbi33-04-member-unavailable", "acquisition-member-unavailable", 1)>]
    [<TestCase("cbi33-05-short-stream", "acquisition-length-mismatch", 1)>]
    [<TestCase("cbi33-06-overlong-stream", "acquisition-length-mismatch", 1)>]
    [<TestCase("cbi33-07-transport-failure", "acquisition-transport-failed", 1)>]
    [<Category("CrossProcess")>]
    member _.``CBI33 C2 acquisition admits exact bounded streams``(id: string, transportCode: string, openCount: int) =
        let observation = run (vector id)
        Assert.Multiple(Action(fun () ->
            Assert.That(observation.Staged, Is.False)
            Assert.That(observation.TransportCode, Is.EqualTo transportCode)
            Assert.That(observation.OpenCount, Is.EqualTo openCount)
            Assert.That(observation.Residue, Is.False)))

    [<Test; Category("CrossProcess")>]
    member _.``CBI33 C3 source attribution is not publisher evidence``() =
        for id in [ "cbi33-01-reference-success"; "cbi33-08-integrity-refused" ] do
            let observation = run (vector id)
            Assert.That(observation.SourceIdentity, Is.EqualTo("fixture://brontide/provider-output"))
            Assert.That(observation.PublisherEvidenceCode, Is.EqualTo("publisher-evidence-not-evaluated"))

    [<Test; Category("CrossProcess")>]
    member _.``CBI33 C4 transport completion is not local admission``() =
        let observation = run (vector "cbi33-08-integrity-refused")
        Assert.Multiple(Action(fun () ->
            Assert.That(observation.TransportCode, Is.EqualTo("transport-completed"))
            Assert.That(observation.AdmissionCode, Is.EqualTo("artifact-set-integrity-failed"))
            Assert.That(observation.Staged, Is.False)))

    [<Test; Category("CrossProcess")>]
    member _.``CBI33 C5 admitted content composes with CBI32 lifecycle``() =
        let observation = run (vector "cbi33-01-reference-success")
        Assert.Multiple(Action(fun () ->
            Assert.That(observation.Staged, Is.True)
            Assert.That(observation.Reused, Is.True)
            Assert.That(observation.Activated, Is.True)
            Assert.That(observation.RemovalCode, Is.EqualTo("removed"))
            Assert.That(observation.Residue, Is.False)))

    [<Test; Category("CrossProcess")>]
    member _.``CBI33 C6 both roots agree on portable observations``() =
        let reference = run (vector "cbi33-01-reference-success")
        let minimal = run (vector "cbi33-02-minimal-success")
        Assert.That(reference.Staged, Is.True)
        Assert.That(minimal.Staged, Is.True)
        Assert.That({ reference with SourceIdentity = "same"; OpenCount = 0 },
                    Is.EqualTo({ minimal with SourceIdentity = "same"; OpenCount = 0 }))
