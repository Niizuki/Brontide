namespace Brontide.Minimal.Interchange.Tests.Portable

open System
open System.IO
open System.Text.Json
open System.Threading
open System.Threading.Channels
open System.Threading.Tasks
open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// One direction of a seam: a byte stream whose reader blocks until bytes arrive, observes
/// cancellation, and sees a clean end once the writer completes.
///
/// An anonymous pipe would be closer to the real transport, but its reads neither observe
/// cancellation nor separate "writer closed" from "handle disposed" on Windows. Those are exactly
/// the two distinctions these vectors turn on, so the seam models them explicitly.
type SeamStream() =
    inherit Stream()

    let segments = Channel.CreateUnbounded<byte array>()
    let mutable current: byte array = Array.empty
    let mutable offset = 0

    member _.CompleteWriting() = segments.Writer.TryComplete() |> ignore

    override _.CanRead = true
    override _.CanSeek = false
    override _.CanWrite = true
    override _.Length = raise (NotSupportedException())

    override _.Position
        with get () = raise (NotSupportedException())
        and set (_: int64) = raise (NotSupportedException())

    override _.Flush() = ()
    override _.Read(_: byte array, _: int, _: int) = raise (NotSupportedException())
    override _.Write(_: byte array, _: int, _: int) = raise (NotSupportedException())
    override _.Seek(_: int64, _: SeekOrigin) = raise (NotSupportedException())
    override _.SetLength(_: int64) = raise (NotSupportedException())
    override _.FlushAsync(_: CancellationToken) = Task.CompletedTask

    override _.ReadAsync(buffer: Memory<byte>, token: CancellationToken) =
        ValueTask<int>(
            task {
                let mutable waiting = true
                let mutable ended = false

                while waiting && offset = current.Length do
                    let! available = segments.Reader.WaitToReadAsync token

                    if not available then
                        waiting <- false
                        ended <- true
                    else
                        match segments.Reader.TryRead() with
                        | true, segment ->
                            current <- segment
                            offset <- 0
                            waiting <- false
                        | _ -> ()

                if ended then
                    return 0
                else
                    let count = min buffer.Length (current.Length - offset)
                    ReadOnlyMemory<byte>(current, offset, count).CopyTo buffer
                    offset <- offset + count
                    return count
            }
        )

    override _.WriteAsync(buffer: ReadOnlyMemory<byte>, _: CancellationToken) =
        if segments.Writer.TryWrite(buffer.ToArray()) then
            ValueTask()
        else
            raise (IOException "The seam is closed for writing.")

/// A duplex seam between a host and a provider endpoint inside one test process.
///
/// It is a real pipe pair with real framing, so the process realization's encoding, bounds, and
/// copy accounting are exercised. It is not process isolation: the cross-process evidence lives in
/// the gated test that launches the provider executable.
type PortableLocalSeam(limits: PortableLimits) =
    let hostToProvider = new SeamStream()
    let providerToHost = new SeamStream()
    let cancellation = new CancellationTokenSource()
    let mutable provider: Task = Task.CompletedTask

    member _.HostDuplex: IPortableDuplex =
        PortableStreamDuplex(providerToHost, hostToProvider, limits, false)

    member _.ProviderDuplex: IPortableDuplex =
        PortableStreamDuplex(hostToProvider, providerToHost, limits, false)

    member this.StartProvider(endpoint: PortableProviderEndpoint) =
        provider <- PortableProviderProcessLoop.run this.ProviderDuplex endpoint limits :> Task

    /// Runs a deliberately misbehaving peer instead of the conforming endpoint.
    member this.StartPeer(peer: IPortableDuplex -> CancellationToken -> Task) =
        provider <- peer this.ProviderDuplex cancellation.Token

    /// Ends the provider's outbound direction, which the host observes as a clean end between
    /// frames rather than as an interrupted one.
    member _.CloseProviderOutput() = providerToHost.CompleteWriting()

    member _.CloseProviderInput() = hostToProvider.CompleteWriting()

    interface IDisposable with
        member _.Dispose() =
            cancellation.Cancel()
            hostToProvider.CompleteWriting()
            providerToHost.CompleteWriting()

            try
                provider.Wait(TimeSpan.FromSeconds 5.0) |> ignore
            with _ ->
                // Teardown: a peer failing as its seam disappears is the expected outcome, and the
                // test has already asserted whatever it came to assert.
                ()

            cancellation.Dispose()
            hostToProvider.Dispose()
            providerToHost.Dispose()

[<AutoOpen>]
module PortableTestHarness =

    /// The neutral artifacts are authored data, so a missing string there is a broken fixture
    /// rather than a case the tests should tolerate.
    let str (value: string | null) =
        match value with
        | null -> failwith "The neutral artifact declares a text value that is absent."
        | value -> value

    /// NUnit's multiple-assertion block takes a delegate; naming it keeps the call sites readable.
    let assertAll (assertions: unit -> unit) = Assert.Multiple(Action assertions)

    /// NUnit's equality constraint is ambiguous over F# lists, sets, maps, and arrays, so structural
    /// comparison happens here and the values are reported when it fails.
    let shouldEqual (expected: 'T) (actual: 'T) =
        Assert.That(
            (actual = expected),
            Is.True,
            $"Expected %A{expected}{Environment.NewLine}but was  %A{actual}"
        )

    let neutralRoot =
        Path.Combine(TestContext.CurrentContext.TestDirectory, "binding", "portable")

    let readNeutral (parts: string list) =
        let path = Path.Combine(Array.ofList (neutralRoot :: parts))
        Assert.That(File.Exists path, Is.True, $"The neutral artifact '{path}' was not copied to the test output.")
        JsonDocument.Parse(File.ReadAllText path)

    /// Every vector id declared by the neutral layer, across all vector files.
    let neutralVectorIds () =
        Directory.GetFiles(Path.Combine(neutralRoot, "vectors"), "*.json")
        |> Array.collect (fun file ->
            use document = JsonDocument.Parse(File.ReadAllText file)

            match document.RootElement.TryGetProperty "vectors" with
            | true, vectors -> vectors.EnumerateArray() |> Seq.map (fun vector -> vector.GetProperty("id").GetString() |> str) |> Array.ofSeq
            | _ -> Array.empty)
        |> List.ofArray

    let expectOk (result: PortableResult<'T>) =
        match result with
        | Ok value -> value
        | Error(Refused fault) ->
            Assert.Fail($"Expected success but the layer refused with '{ProtocolCategory.token fault.Category}': {fault.Message}")
            failwith "unreachable"
        | Error(Interrupted failure) ->
            Assert.Fail($"Expected success but the seam reported '{ProcessCategory.token failure.Category}': {failure.Message}")
            failwith "unreachable"

    let expectRefusal (result: PortableResult<'T>) =
        match result with
        | Error(Refused fault) -> fault
        | Error(Interrupted failure) ->
            Assert.Fail($"Expected a refusal but the seam reported '{ProcessCategory.token failure.Category}'.")
            failwith "unreachable"
        | Ok _ ->
            Assert.Fail "Expected a refusal but the layer accepted the input."
            failwith "unreachable"

    /// Asserts the exact portable category a vector states, which is the whole point of the
    /// taxonomy: a refusal is right only when its category is right.
    let expectCategory category (result: PortableResult<'T>) =
        let fault = expectRefusal result
        Assert.That(ProtocolCategory.token fault.Category, Is.EqualTo(ProtocolCategory.token category))
        fault

    let expectProcessFailure (result: PortableResult<'T>) =
        match result with
        | Error(Interrupted failure) -> failure
        | Error(Refused fault) ->
            Assert.Fail($"Expected a process failure but the layer refused with '{ProtocolCategory.token fault.Category}'.")
            failwith "unreachable"
        | Ok _ ->
            Assert.Fail "Expected a process failure but the interaction completed."
            failwith "unreachable"

    let permitted =
        PortableConstraint.AllOf
            [ PortableConstraint.Atom PortableTruth.Satisfied
              PortableConstraint.Atom PortableTruth.Satisfied ]

    let blob (content: string) =
        PortableResource.blob "profile" (Text.Encoding.UTF8.GetBytes content)

    let hex (bytes: byte array) = PortableHex.encode bytes

    let fromHex (value: string) = PortableHex.decode value

    let catalogOf (document: ContractDocument) = expectOk (ShapeCatalog.fromContract document)

    /// The fixed direct-call realization: no wire, no encoding, no copy.
    let directHost (document: ContractDocument) (handler: IPortableOperationHandler) =
        let endpoint = PortableProviderEndpoint(document, handler, Realization.FixedDirectCall)

        PortableBindingHost
            .Establish(document, PortableDirectConversation endpoint, "minimal-direct")
            .Result
        |> expectOk

    let directCoolingHost () =
        directHost CoolingFixture.contract (CoolingHandler())

    /// The negotiated process realization over a local duplex seam: real length-delimited framing
    /// and a real deterministic-CBOR encode and decode on both sides.
    let processHost (document: ContractDocument) (handler: IPortableOperationHandler) =
        let seam = new PortableLocalSeam(document.Limits)
        let endpoint = PortableProviderEndpoint(document, handler, Realization.NegotiatedProcess)
        seam.StartProvider endpoint

        let host =
            PortableBindingHost
                .Establish(document, PortableProcessConversation(seam.HostDuplex, document.Limits), "minimal-process")
                .Result
            |> expectOk

        host, seam

    let processCoolingHost () =
        processHost CoolingFixture.contract (CoolingHandler())

    let invoke (host: PortableBindingHost) operation shape value =
        host.Invoke(operation, shape, value, permitted).Result

    /// Reads a golden encoding's explicit value description into a shaped value. The description
    /// carries kind discriminators; the wire form does not, which is the point of the comparison.
    let rec readDescribedValue (element: JsonElement) : PortableValue =
        match element.GetProperty("kind").GetString() |> str with
        | "unit" -> PortableUnit
        | "text" -> PortableText(element.GetProperty("value").GetString() |> str)
        | "boolean" -> PortableBoolean(element.GetProperty("value").GetBoolean())
        | "integer" -> PortableInteger(element.GetProperty("value").GetInt64())
        | "decimal" ->
            PortableDecimalValue(element.GetProperty("exponent").GetInt32(), element.GetProperty("mantissa").GetInt64())
        | "bytes" -> PortableBytesValue(fromHex (element.GetProperty("hex").GetString() |> str))
        | "sequence" ->
            PortableSequence(element.GetProperty("items").EnumerateArray() |> Seq.map readDescribedValue |> List.ofSeq)
        | "choice" ->
            PortableChoice(
                element.GetProperty("case").GetString() |> str,
                readDescribedValue (element.GetProperty "value")
            )
        | "record" ->
            let fields =
                element.GetProperty("fields").EnumerateObject()
                |> Seq.map (fun field -> field.Name, readDescribedValue field.Value)
                |> Map.ofSeq

            let fragments =
                element.GetProperty("fragments").EnumerateObject()
                |> Seq.map (fun fragment ->
                    let reference = expectOk (PortableFragmentRef.tryParseText fragment.Name)

                    let fields =
                        fragment.Value.EnumerateObject()
                        |> Seq.map (fun field -> field.Name, readDescribedValue field.Value)
                        |> Map.ofSeq

                    reference, fields)
                |> Map.ofSeq

            PortableRecord(fields, fragments)
        | other -> failwith $"The golden value description uses unknown kind '{other}'."

    let readDescribedShape (element: JsonElement) =
        expectOk (PortableShapeRef.tryCreate (element.GetProperty("name").GetString() |> str) (element.GetProperty("version").GetInt32()))
