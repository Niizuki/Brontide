namespace Brontide.Minimal.Interchange.Tests.Portable

open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// The deterministic representation, measured against the neutral golden encodings.
///
/// The golden bytes are read from the neutral artifacts and reproduced, never restated here: a
/// codec that agreed with a hand-copied constant but not with the contract would pass a test that
/// proves nothing.
[<TestFixture>]
type PortableEncodingTests() =

    let catalog = catalogOf CoolingFixture.contract
    let limits = PortableLimits.declared

    [<Test>]
    member _.``every golden value encodes to exactly the neutral bytes``() =
        use document = readNeutral [ "vectors"; "golden-encodings.json" ]

        let entries = document.RootElement.GetProperty "encodings" |> _.EnumerateArray() |> List.ofSeq

        Assert.That(List.length entries, Is.EqualTo 6)

        for entry in entries do
            let id = entry.GetProperty("id").GetString() |> str
            let shape = readDescribedShape (entry.GetProperty "shape")
            let value = readDescribedValue (entry.GetProperty "value")
            let expected = entry.GetProperty("cbor").GetString() |> str

            let encoded =
                PortableValueCodec.encode catalog shape value
                |> Result.bind PortableCbor.encode
                |> expectOk

            Assert.That(hex encoded, Is.EqualTo expected, $"{id} did not encode to its golden bytes.")

    [<Test>]
    member _.``every golden encoding decodes back to the described value``() =
        use document = readNeutral [ "vectors"; "golden-encodings.json" ]

        for entry in document.RootElement.GetProperty("encodings").EnumerateArray() do
            let id = entry.GetProperty("id").GetString() |> str
            let shape = readDescribedShape (entry.GetProperty "shape")
            let value = readDescribedValue (entry.GetProperty "value")
            let bytes = fromHex (entry.GetProperty("cbor").GetString() |> str)

            // The Fragment the golden values attach is the one the negotiated Operation declares,
            // so decoding uses that declaration rather than an empty set.
            let decoded =
                PortableCbor.decode bytes limits
                |> Result.bind (PortableValueCodec.decode catalog shape [ CoolingFixture.hostContext ])
                |> expectOk

            Assert.That(decoded, Is.EqualTo value, $"{id} did not round-trip through the schema-guided codec.")

    [<Test>]
    member _.``deterministic key order is not ordinal string order``() =
        use document = readNeutral [ "vectors"; "golden-encodings.json" ]

        let g1 =
            document.RootElement.GetProperty("encodings").EnumerateArray()
            |> Seq.find (fun entry -> (entry.GetProperty("id").GetString() |> str) = "G1-COOLING-COMMAND-MINIMAL")

        let encoded = fromHex (g1.GetProperty("cbor").GetString() |> str)
        let text = hex encoded

        // 'loop' precedes 'enabled' because the encoded key sorts first, while ordinal string order
        // would reverse them. This is the divergence the migration obligation names.
        let loopAt = text.IndexOf "646c6f6f70"
        let enabledAt = text.IndexOf "67656e61626c6564"
        Assert.That(loopAt, Is.GreaterThanOrEqualTo 0)
        Assert.That(enabledAt, Is.GreaterThan loopAt)

    [<Test>]
    member _.``the framed example carries the declared big-endian length prefix``() =
        use document = readNeutral [ "vectors"; "golden-encodings.json" ]
        let framedExample = document.RootElement.GetProperty "framedExample"
        let framed = fromHex (framedExample.GetProperty("framed").GetString() |> str)
        let bodyBytes = framedExample.GetProperty("bodyBytes").GetInt32()

        let of' = framedExample.GetProperty("of").GetString() |> str

        let body =
            document.RootElement.GetProperty("encodings").EnumerateArray()
            |> Seq.find (fun entry -> (entry.GetProperty("id").GetString() |> str) = of')
            |> fun entry -> fromHex (entry.GetProperty("cbor").GetString() |> str)

        assertAll (fun () ->
            Assert.That(framed.Length, Is.EqualTo(PortableFraming.PrefixBytes + bodyBytes))
            shouldEqual [| 0uy; 0uy; 0uy; byte bodyBytes |] (Array.sub framed 0 4)
            shouldEqual body (Array.sub framed 4 bodyBytes))

    [<Test>]
    member _.``every rejected encoding is refused with its declared category``() =
        use document = readNeutral [ "vectors"; "golden-encodings.json" ]

        let rejected =
            document.RootElement.GetProperty "rejectedEncodings" |> _.EnumerateArray() |> List.ofSeq

        Assert.That(List.length rejected, Is.EqualTo 7)

        for entry in rejected do
            let id = entry.GetProperty("id").GetString() |> str
            let bytes = fromHex (entry.GetProperty("cbor").GetString() |> str)
            let expected = entry.GetProperty("expected").GetProperty("category").GetString() |> str

            let fault = expectRefusal (PortableCbor.decode bytes limits)

            Assert.That(
                ProtocolCategory.token fault.Category,
                Is.EqualTo expected,
                $"{id} was not refused with the category the neutral contract states."
            )

    [<Test>]
    member _.``a decimal has exactly one encoding``() =
        // The mantissa is normalized so the same fraction cannot travel two ways.
        shouldEqual (0, 123L) (expectOk (CborDecimal.normalize -1 1230L))
        shouldEqual (0, 0L) (expectOk (CborDecimal.normalize 5 0L))

    [<Test>]
    member _.``an empty frame body and an oversized prefix are separate refusals``() =
        let oversized = Array.zeroCreate<byte> (limits.MaxFrameBytes + 1)

        let fault =
            expectRefusal (PortableFraming.writeFrame (new System.IO.MemoryStream()) oversized limits System.Threading.CancellationToken.None |> _.Result)

        Assert.That(fault.Category, Is.EqualTo ProtocolCategory.LimitExceeded)

        let empty =
            expectRefusal (
                PortableFraming.writeFrame (new System.IO.MemoryStream()) Array.empty limits System.Threading.CancellationToken.None
                |> _.Result
            )

        Assert.That(empty.Category, Is.EqualTo ProtocolCategory.MalformedMessage)
