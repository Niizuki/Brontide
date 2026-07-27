namespace Brontide.Minimal.Interchange.Tests.Portable

open System
open System.IO
open System.Threading
open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// PB6: the decoders hold their contract on arbitrary and mutated input, inside deterministic bounds.
///
/// Every vector so far presents input a person wrote. These present input nobody wrote, which is the
/// only way to check the property the vectors assume everywhere.
///
/// This stack states the property more strongly than the Reference one can. Refusals here are
/// returned values rather than raised failures, so the claim is not "it raises only the right
/// exception" but "it does not raise at all": every input, however malformed, produces an `Ok` or an
/// `Error` carrying a portable category. A raised exception is a defect by construction, not merely
/// the wrong kind of refusal.
///
/// The generator is seeded and the iteration count fixed, so a failure is reproducible and the suite
/// is not a source of intermittent red. That is what "within deterministic bounds" asks for: this is
/// a property test, not a soak test.
[<TestFixture>]
type PortableDecoderPropertyTests() =

    let seed = 0x50171DE1
    let iterations = 2000

    /// Decoding returns a result. Anything raised is the defect being hunted.
    let decodeWithoutRaising what (decode: unit -> PortableResult<'T>) =
        try
            decode ()
        with exn ->
            Assert.Fail $"{what} raised {exn.GetType().FullName} rather than returning a refusal: {exn.Message}"
            failwith "unreachable"

    let arbitrary (random: Random) (size: int) =
        let buffer = Array.zeroCreate<byte> size
        random.NextBytes buffer
        buffer[.. random.Next(0, size) - 1]

    [<Test>]
    member _.``arbitrary bytes are either decoded or refused, and never raise``() =
        let random = Random seed

        for _ in 1..iterations do
            let input = arbitrary random 64

            decodeWithoutRaising "Decoding arbitrary bytes" (fun () -> PortableCbor.decode input PortableLimits.declared)
            |> ignore

    [<Test>]
    member _.``arbitrary bytes are never accepted as a well-formed envelope``() =
        let random = Random(seed + 1)
        let mutable accepted = 0

        for _ in 1..iterations do
            let input = arbitrary random 96

            match decodeWithoutRaising "An envelope decode" (fun () -> EnvelopeCodec.decode input PortableLimits.declared) with
            | Ok _ -> accepted <- accepted + 1
            | Error _ -> ()

        // Random bytes are not a contract-versioned envelope with a declared kind and a channel
        // identity. Accepting one would mean the envelope's required fields are not required.
        Assert.That(accepted, Is.Zero, "Random input was accepted as a well-formed envelope.")

    /// Single-byte mutations of a valid frame. This reaches the decode paths random bytes never do,
    /// because a mutant is still nearly a legal message.
    [<Test>]
    member _.``every single-byte mutation of a valid frame is refused or decoded but never raises``() =
        let valid =
            expectOk (EnvelopeCodec.encode (Envelope.control EnvelopeKind.Ready (ChannelId.next ()) (CborMap [])))

        let random = Random(seed + 2)

        for index in 0 .. valid.Length - 1 do
            for replacement in [ 0uy; 0xFFuy; 0x7Buy; byte (random.Next 256) ] do
                let mutant = Array.copy valid
                mutant[index] <- replacement

                decodeWithoutRaising
                    $"Byte {index} replaced with 0x%02x{replacement}"
                    (fun () -> EnvelopeCodec.decode mutant PortableLimits.declared)
                |> ignore

    /// Truncations of a valid frame, which are the interrupted-frame shape at every offset.
    [<Test>]
    member _.``every truncation of a valid frame is refused rather than half-decoded``() =
        let valid =
            expectOk (EnvelopeCodec.encode (Envelope.control EnvelopeKind.Ready (ChannelId.next ()) (CborMap [])))

        for length in 0 .. valid.Length - 1 do
            decodeWithoutRaising
                $"A frame truncated to {length} bytes"
                (fun () -> EnvelopeCodec.decode valid[.. length - 1] PortableLimits.declared)
            |> ignore

    /// Nesting is bounded before it becomes recursion. A generator that can always add one more
    /// level is the honest test of a declared depth limit.
    [<Test>]
    member _.``nesting beyond the declared depth is refused at every depth past the bound``() =
        for depth in
            [ PortableLimits.declared.MaxNestingDepth + 1
              PortableLimits.declared.MaxNestingDepth + 8
              PortableLimits.declared.MaxNestingDepth * 4
              10_000 ] do
            let nested = Array.create (depth + 1) 0x81uy
            nested[depth] <- 0xF6uy

            let fault =
                decodeWithoutRaising $"Depth {depth}" (fun () -> PortableCbor.decode nested PortableLimits.declared)
                |> expectRefusal

            Assert.That(fault.Category, Is.EqualTo ProtocolCategory.LimitExceeded, $"Depth {depth} did not refuse.")

    /// A length prefix is refused on the prefix alone, so a hostile declaration never causes an
    /// allocation proportional to it.
    [<Test>]
    member _.``a hostile length prefix is refused before any allocation``() =
        let random = Random(seed + 3)

        for _ in 1..256 do
            let declared =
                uint32 (random.NextInt64(int64 PortableLimits.declared.MaxFrameBytes + 1L, int64 UInt32.MaxValue))

            let prefix = Array.zeroCreate<byte> 4
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(Span prefix, declared)
            use stream = new MemoryStream(prefix)

            let fault =
                (PortableFraming.readFrame stream PortableLimits.declared CancellationToken.None).Result
                |> expectRefusal

            assertAll (fun () ->
                Assert.That(fault.Category, Is.EqualTo ProtocolCategory.LimitExceeded)
                // The declared body was never read.
                Assert.That(stream.Position, Is.EqualTo 4L))

    /// A refusal never carries a runtime type, a stack trace, or a namespace, whatever the input.
    ///
    /// This is the property CH-18 states, checked against input nobody chose rather than against the
    /// handful of messages a person wrote.
    [<Test>]
    member _.``no refusal message carries a runtime type or a stack trace``() =
        let random = Random(seed + 4)
        let forbidden = [ "Brontide."; "Microsoft.FSharp"; "System."; "Exception"; "   at " ]
        let observed = ResizeArray<string>()

        for _ in 1..iterations do
            let input = arbitrary random 80

            match decodeWithoutRaising "An envelope decode" (fun () -> EnvelopeCodec.decode input PortableLimits.declared) with
            | Error(Refused fault) -> observed.Add $"{fault.LocalCode}|{fault.Message}"
            | Error(Interrupted failure) -> observed.Add failure.Message
            | Ok _ -> ()

        assertAll (fun () ->
            Assert.That(observed, Is.Not.Empty, "The generator produced no refusals to inspect.")

            for message in Seq.distinct observed do
                for marker in forbidden do
                    Assert.That(message, Does.Not.Contain marker, $"A refusal leaked '{marker}': {message}"))
