namespace Brontide.Minimal.Interchange.Tests.Portable

open System.Text
open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// PB6: the C6 refusals, decided by a provider endpoint across a real seam.
///
/// PB-29 through PB-32 call the resource codec directly. That proves a static function refuses a
/// malformed resource; it does not prove the endpoint does, and the endpoint is what a hostile peer
/// actually reaches. The difference matters because admission sits behind decode, lifecycle, and
/// operation resolution — a refusal a unit test reaches in one call may be unreachable, or reached
/// in the wrong order, once those run first.
///
/// These tests drive the wire directly rather than through the host, because the host's own encoder
/// cannot produce most of them: a conforming host never puts octets beside a handle. A hostile host
/// is the only thing that presents these frames, so a hostile host is what asks the question.
[<TestFixture>]
type PortableResourceSeamTests() =

    /// Establishes a binding over the seam and returns the channel it runs on.
    let establish (seam: PortableLocalSeam) (contract: ContractDocument) =
        let channel = ChannelId.next ()
        let host = seam.HostDuplex

        expectOk
            (host
                .Send(expectOk (EnvelopeCodec.encode (Envelope.control EnvelopeKind.Establish channel (ContractCodec.encode contract))))
                .Result)

        let accepted =
            expectOk (EnvelopeCodec.decode (expectOk (host.Receive().Result)) PortableLimits.declared)

        Assert.That(accepted.Kind, Is.EqualTo EnvelopeKind.EstablishAccepted)

        let ready =
            expectOk (EnvelopeCodec.decode (expectOk (host.Receive().Result)) PortableLimits.declared)

        Assert.That(ready.Kind, Is.EqualTo EnvelopeKind.Ready)
        channel

    /// Presents one hand-built resource frame to a conforming endpoint and reports what came back.
    let present (contract: ContractDocument) operation inputShape input resource =
        let handler: IPortableOperationHandler =
            if contract = CatalogFixture.contract then
                CatalogHandler()
            else
                CoolingHandler()

        use seam = new PortableLocalSeam(PortableLimits.declared)
        let endpoint = PortableProviderEndpoint(contract, handler, Realization.NegotiatedProcess)
        seam.StartProvider endpoint

        let channel = establish seam contract
        let catalog = catalogOf contract

        let body: RequestBody =
            { Operation = OperationDesignation.Canonical operation
              InputShape = inputShape
              Input = expectOk (PortableValueCodec.encode catalog inputShape input)
              Resources = [ resource ] }

        let request =
            Envelope.correlated EnvelopeKind.Request channel (Some(ChannelRequestId.next ())) None (RequestBody.encode body)

        expectOk (seam.HostDuplex.Send(expectOk (EnvelopeCodec.encode request)).Result)

        let reply =
            expectOk (EnvelopeCodec.decode (expectOk (seam.HostDuplex.Receive().Result)) PortableLimits.declared)

        // A refused resource must come back as a protocol error, never as an Outcome.
        Assert.That(reply.Kind, Is.EqualTo EnvelopeKind.ProtocolError)

        let error = expectOk (ProtocolErrorBody.decode reply.Body)

        let effects =
            match handler with
            | :? CoolingHandler as cooling -> cooling.ProviderEffectCount
            | :? CatalogHandler as catalogHandler -> catalogHandler.ProviderEffectCount
            | _ -> 0L

        error.Category, error.LocalCode, effects

    let coolingCommand () = CoolingFixture.authorizedCommand "primary" true

    let blobFields (content: byte array) =
        [ "flavor", CborText ResourceFlavor.CopiedImmutableBlobToken
          "name", CborText "profile"
          "content", CborBytes content
          "integrity", CborText(PortableResource.hashOf content) ]

    // PB-30-FORBIDDEN-IMPLICIT-COPY, across the seam.
    [<Test>]
    member _.``octets beside a handle are refused by the endpoint, not only by the codec``() =
        let frame =
            CborMap
                [ "flavor", CborText ResourceFlavor.AddressingOnlyHandleToken
                  "name", CborText "catalog"
                  "provider", CborText "catalog-provider"
                  "id", CborText "primary"
                  "content", CborBytes [| 1uy; 2uy; 3uy |] ]

        let category, localCode, effects =
            present
                CatalogFixture.contract
                CatalogFixture.upsert
                CatalogFixture.upsertCommand
                (CatalogFixture.upsertCommandValue [ CatalogFixture.itemValue "a" "Alpha" [ "one" ] ])
                frame

        assertAll (fun () ->
            Assert.That(category, Is.EqualTo ProtocolCategory.InvalidPayload)
            Assert.That(localCode, Is.EqualTo "forbidden-implicit-copy")
            Assert.That(effects, Is.EqualTo 0L))

    // PB-32-RELEASE-SIGNAL-FOR-COPIED-FLAVOR, across the seam.
    [<Test>]
    member _.``a release signal for the copied flavor is refused by the endpoint``() =
        let content = Encoding.UTF8.GetBytes "cooling-profile"
        let frame = CborMap(blobFields content @ [ "release", CborBoolean true ])

        let category, _, effects =
            present CoolingFixture.contract CoolingFixture.setEnabled CoolingFixture.commandV1 (coolingCommand ()) frame

        assertAll (fun () ->
            Assert.That(category, Is.EqualTo ProtocolCategory.StateViolation)
            Assert.That(effects, Is.EqualTo 0L))

    // PB-31-RESOURCE-EXCEEDS-DECLARED-BOUND, across the seam.
    [<Test>]
    member _.``a resource beyond the declared bound is refused before the provider is reached``() =
        let oversized = Array.zeroCreate<byte> (PortableLimits.declared.MaxResourceBytes + 1)
        let frame = CborMap(blobFields oversized)

        let category, _, effects =
            present CoolingFixture.contract CoolingFixture.setEnabled CoolingFixture.commandV1 (coolingCommand ()) frame

        assertAll (fun () ->
            Assert.That(category, Is.EqualTo ProtocolCategory.LimitExceeded)
            Assert.That(effects, Is.EqualTo 0L))

    /// A version-0.1 non-goal flavor presented on the wire is refused, not silently downgraded to
    /// the flavor the binding did negotiate.
    ///
    /// PB-29 refuses these at negotiation, where a contract declares one. This is the other way in:
    /// a peer that negotiated the copied blob and then names a borrowed region on a request. Both
    /// have to fail closed, and only this one reaches the endpoint's admission path.
    [<Test>]
    member _.``a non-goal flavor named on a request fails closed``() =
        for flavor in ResourceFlavor.nonGoals do
            let content = [| 1uy; 2uy; 3uy |]

            let frame =
                CborMap
                    [ "flavor", CborText flavor
                      "name", CborText "profile"
                      "content", CborBytes content
                      "integrity", CborText(PortableResource.hashOf content) ]

            let category, _, effects =
                present CoolingFixture.contract CoolingFixture.setEnabled CoolingFixture.commandV1 (coolingCommand ()) frame

            assertAll (fun () ->
                Assert.That(
                    category,
                    Is.AnyOf(ProtocolCategory.UnsupportedContract, ProtocolCategory.MalformedMessage),
                    $"Flavor '{flavor}' was neither refused as unnegotiated nor as unreadable."
                )

                Assert.That(effects, Is.EqualTo 0L))

    // PB-26-RESOURCE-INTEGRITY-MISMATCH, decided by the endpoint rather than by a direct admit call.
    [<Test>]
    member _.``a content hash that does not verify is refused by the endpoint``() =
        let frame =
            CborMap
                [ "flavor", CborText ResourceFlavor.CopiedImmutableBlobToken
                  "name", CborText "profile"
                  "content", CborBytes(Encoding.UTF8.GetBytes "tampered")
                  "integrity", CborText(PortableResource.hashOf (Encoding.UTF8.GetBytes "original")) ]

        let category, _, effects =
            present CoolingFixture.contract CoolingFixture.setEnabled CoolingFixture.commandV1 (coolingCommand ()) frame

        assertAll (fun () ->
            Assert.That(category, Is.EqualTo ProtocolCategory.InvalidPayload)
            Assert.That(effects, Is.EqualTo 0L))

    /// Version 0.1's referenced-resource floor makes two of PB6's conditions unrepresentable rather
    /// than merely refused, which is worth recording where the refusals live.
    ///
    /// Premature reuse and a release-then-use sequence need a resource with a lifetime a peer can
    /// observe ending. The declared floor has neither: a copied immutable blob is transferred whole
    /// and has no release signal, and an addressing-only handle carries no octets to release. There
    /// is no frame that expresses "use this after its interval", so there is nothing for a vector to
    /// present. Unsupported fallback is the same shape: no fallback policy is declared for 0.1, so a
    /// request cannot name one to have it refused.
    ///
    /// This is asserted rather than written only in prose, so that adding a borrowed or transferred
    /// flavor later fails here and brings the reasoning back for review.
    [<Test>]
    member _.``the 0.1 floor makes reuse and fallback unrepresentable rather than refused``() =
        let declared =
            Set.ofList (
                CoolingFixture.contract.Representation.ResourceFlavors
                @ CatalogFixture.contract.Representation.ResourceFlavors
            )

        assertAll (fun () ->
            // A flavor with an observable lifetime would make premature reuse expressible, and it
            // would then need a vector rather than this note.
            shouldEqual
                (Set.ofList [ ResourceFlavor.CopiedImmutableBlobToken; ResourceFlavor.AddressingOnlyHandleToken ])
                declared

            // The non-goal flavors are what make borrowing and transfer fail negotiation rather than
            // admission.
            Assert.That(ResourceFlavor.nonGoals, Is.Not.Empty))
