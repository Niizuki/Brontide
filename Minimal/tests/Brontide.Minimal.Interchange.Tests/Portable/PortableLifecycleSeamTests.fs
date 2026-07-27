namespace Brontide.Minimal.Interchange.Tests.Portable

open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// PB6: the C8 lifecycle refusals, decided by a provider endpoint across a real seam.
///
/// PB-09 and PB-36 through PB-39 drive the lifecycle value directly. That proves the state machine
/// rejects an illegal transition; it does not prove the endpoint applies it to an arriving frame,
/// which is where a peer's illegal sequence actually lands. The two can differ: a frame is decoded,
/// its kind resolved, and its body read before any state is consulted, so an endpoint can refuse for
/// the wrong reason, in the wrong order, or accept something the state machine alone would reject.
///
/// Each case sends a deliberate sequence and reads what came back. None of them may produce an
/// Outcome, and none may reach the provider.
[<TestFixture>]
type PortableLifecycleSeamTests() =

    let establishFrame channel contract =
        Envelope.control EnvelopeKind.Establish channel (ContractCodec.encode contract)

    let requestFrame channel =
        let catalog = catalogOf CoolingFixture.contract

        let body: RequestBody =
            { Operation = OperationDesignation.Canonical CoolingFixture.setEnabled
              InputShape = CoolingFixture.commandV1
              Input =
                expectOk (
                    PortableValueCodec.encode catalog CoolingFixture.commandV1 (CoolingFixture.authorizedCommand "primary" true)
                )
              Resources = [] }

        Envelope.correlated EnvelopeKind.Request channel (Some(ChannelRequestId.next ())) None (RequestBody.encode body)

    /// Runs a sequence of frames against a conforming endpoint and collects every reply.
    let converse (offered: ContractDocument) (frames: ChannelId -> Envelope list) expectedReplies =
        let handler = CoolingHandler()
        use seam = new PortableLocalSeam(PortableLimits.declared)
        let endpoint = PortableProviderEndpoint(offered, handler, Realization.NegotiatedProcess)
        seam.StartProvider endpoint

        let channel = ChannelId.next ()

        for frame in frames channel do
            expectOk (seam.HostDuplex.Send(expectOk (EnvelopeCodec.encode frame)).Result)

        let replies =
            [ for _ in 1..expectedReplies ->
                  expectOk (EnvelopeCodec.decode (expectOk (seam.HostDuplex.Receive().Result)) PortableLimits.declared) ]

        replies, handler.ProviderEffectCount

    let categoryOf (envelope: Envelope) =
        (expectOk (ProtocolErrorBody.decode envelope.Body)).Category

    /// Establishment that fails negotiation starts nothing: no readiness signal, no provider.
    ///
    /// This is the C8 order that matters most. A provider that activated first and negotiated second
    /// would satisfy every other vector here and still be wrong.
    [<Test>]
    member _.``an establishment that fails negotiation never signals readiness or reaches the provider``() =
        // The endpoint offers a contract without the profile provision the host requires.
        let replies, effects =
            converse (CoolingFixture.withoutProfileProvision ()) (fun channel -> [ establishFrame channel CoolingFixture.contract ]) 1

        let last = List.last replies

        assertAll (fun () ->
            Assert.That(last.Kind, Is.EqualTo EnvelopeKind.ProtocolError)
            Assert.That(categoryOf last, Is.EqualTo ProtocolCategory.UnsupportedContract)

            // A binding that never negotiated must not report readiness.
            Assert.That(replies |> List.map (fun reply -> reply.Kind), Does.Not.Contain EnvelopeKind.Ready)
            Assert.That(replies |> List.map (fun reply -> reply.Kind), Does.Not.Contain EnvelopeKind.EstablishAccepted)
            Assert.That(effects, Is.EqualTo 0L))

    // PB-09-REQUEST-BEFORE-READY, over the seam rather than against the lifecycle value.
    [<Test>]
    member _.``a request before any establishment is refused and starts no provider``() =
        let replies, effects = converse CoolingFixture.contract (fun channel -> [ requestFrame channel ]) 1
        let last = List.last replies

        assertAll (fun () ->
            Assert.That(last.Kind, Is.EqualTo EnvelopeKind.ProtocolError)
            Assert.That(categoryOf last, Is.EqualTo ProtocolCategory.StateViolation)
            Assert.That(effects, Is.EqualTo 0L))

    // PB-39-ESTABLISH-ON-ESTABLISHED-BINDING, over the seam.
    [<Test>]
    member _.``a second establishment is refused rather than renegotiated in place``() =
        let replies, effects =
            converse
                CoolingFixture.contract
                (fun channel ->
                    [ establishFrame channel CoolingFixture.contract
                      establishFrame channel CoolingFixture.contract ])
                // establish-accepted, ready, then the refusal.
                3

        assertAll (fun () ->
            Assert.That(replies[0].Kind, Is.EqualTo EnvelopeKind.EstablishAccepted)
            Assert.That(replies[1].Kind, Is.EqualTo EnvelopeKind.Ready)
            Assert.That(replies[2].Kind, Is.EqualTo EnvelopeKind.ProtocolError)
            Assert.That(categoryOf replies[2], Is.EqualTo ProtocolCategory.StateViolation)
            Assert.That(effects, Is.EqualTo 0L))

    /// A request after withdrawal is refused, and the withdrawal itself is not answered.
    [<Test>]
    member _.``a request after withdrawal is refused and starts no provider``() =
        let replies, effects =
            converse
                CoolingFixture.contract
                (fun channel ->
                    [ establishFrame channel CoolingFixture.contract
                      Envelope.empty EnvelopeKind.Withdraw channel
                      requestFrame channel ])
                3

        assertAll (fun () ->
            Assert.That(replies[1].Kind, Is.EqualTo EnvelopeKind.Ready)
            // A withdrawal is not answered, so the third reply is the refusal of the request.
            Assert.That(replies[2].Kind, Is.EqualTo EnvelopeKind.ProtocolError)
            Assert.That(categoryOf replies[2], Is.EqualTo ProtocolCategory.StateViolation)
            Assert.That(effects, Is.EqualTo 0L))

    /// An envelope kind that is declared, but is not one a provider endpoint receives.
    ///
    /// This is the case the lifecycle value cannot express on its own: `outcome` is a legal kind in
    /// the taxonomy and a legal transition for a host, and only the direction makes it illegal here.
    /// A provider that switched on the kind without considering direction would treat its own reply
    /// shape as an instruction.
    ///
    /// An `outcome` carries a correlation identity by declaration, so the frame is built with one:
    /// without it the refusal would be about shape rather than direction.
    [<Test>]
    member _.``a kind a provider never receives is refused as a state violation``() =
        let wellFormed kind channel =
            if kind = EnvelopeKind.Outcome then
                Envelope.correlated kind channel (Some(ChannelRequestId.next ())) None (CborMap [])
            else
                Envelope.empty kind channel

        for kind in [ EnvelopeKind.EstablishAccepted; EnvelopeKind.Ready; EnvelopeKind.Outcome ] do
            let replies, effects =
                converse
                    CoolingFixture.contract
                    (fun channel -> [ establishFrame channel CoolingFixture.contract; wellFormed kind channel ])
                    3

            assertAll (fun () ->
                Assert.That(replies[2].Kind, Is.EqualTo EnvelopeKind.ProtocolError, $"{kind}")
                Assert.That(categoryOf replies[2], Is.EqualTo ProtocolCategory.StateViolation, $"{kind}")
                Assert.That(effects, Is.EqualTo 0L, $"{kind}"))

    /// A frame that cannot be read is refused as malformed, before its kind's direction is weighed.
    ///
    /// Recorded as its own case because the ordering is easy to get backwards, and getting it
    /// backwards would report a shape problem as a state problem — telling a peer to fix its
    /// sequencing when its encoder is what is wrong.
    [<Test>]
    member _.``a malformed frame is refused before its direction is considered``() =
        let replies, effects =
            converse
                CoolingFixture.contract
                (fun channel ->
                    [ establishFrame channel CoolingFixture.contract
                      // An outcome without the correlation identity its kind declares.
                      Envelope.empty EnvelopeKind.Outcome channel ])
                3

        assertAll (fun () ->
            Assert.That(categoryOf replies[2], Is.EqualTo ProtocolCategory.MalformedMessage)
            Assert.That(effects, Is.EqualTo 0L))

    // PB-45-UNKNOWN-ENVELOPE-KIND, over the seam rather than against the codec.
    [<Test>]
    member _.``an unrecognized envelope kind is refused before any state is consulted``() =
        let handler = CoolingHandler()
        use seam = new PortableLocalSeam(PortableLimits.declared)
        let endpoint = PortableProviderEndpoint(CoolingFixture.contract, handler, Realization.NegotiatedProcess)
        seam.StartProvider endpoint

        // Built as raw CBOR: the envelope type cannot represent a kind the taxonomy does not declare.
        let gossip =
            CborMap
                [ "contractVersion", CborInteger(int64 ContractDocument.SupportedContractVersion)
                  "kind", CborText "gossip"
                  "channelId", CborText(ChannelId.value (ChannelId.next ()))
                  "body", CborMap [] ]

        expectOk (seam.HostDuplex.Send(expectOk (PortableCbor.encode gossip)).Result)

        let reply =
            expectOk (EnvelopeCodec.decode (expectOk (seam.HostDuplex.Receive().Result)) PortableLimits.declared)

        assertAll (fun () ->
            Assert.That(reply.Kind, Is.EqualTo EnvelopeKind.ProtocolError)
            Assert.That(categoryOf reply, Is.EqualTo ProtocolCategory.UnsupportedKind)
            Assert.That(handler.ProviderEffectCount, Is.EqualTo 0L))

    /// A repeated request identity inside the declared window is refused without repeating the
    /// effect, and the endpoint says so rather than answering twice.
    [<Test>]
    member _.``a replayed request identity is refused without repeating the effect``() =
        let handler = CoolingHandler()
        use seam = new PortableLocalSeam(PortableLimits.declared)
        let endpoint = PortableProviderEndpoint(CoolingFixture.contract, handler, Realization.NegotiatedProcess)
        seam.StartProvider endpoint

        let channel = ChannelId.next ()

        expectOk (seam.HostDuplex.Send(expectOk (EnvelopeCodec.encode (establishFrame channel CoolingFixture.contract))).Result)
        expectOk (seam.HostDuplex.Receive().Result) |> ignore
        expectOk (seam.HostDuplex.Receive().Result) |> ignore

        let request = requestFrame channel
        let encoded = expectOk (EnvelopeCodec.encode request)

        expectOk (seam.HostDuplex.Send(encoded).Result)

        let outcome =
            expectOk (EnvelopeCodec.decode (expectOk (seam.HostDuplex.Receive().Result)) PortableLimits.declared)

        let effectsAfterFirst = handler.ProviderEffectCount

        expectOk (seam.HostDuplex.Send(encoded).Result)

        let replay =
            expectOk (EnvelopeCodec.decode (expectOk (seam.HostDuplex.Receive().Result)) PortableLimits.declared)

        assertAll (fun () ->
            Assert.That(outcome.Kind, Is.EqualTo EnvelopeKind.Outcome)
            Assert.That(effectsAfterFirst, Is.EqualTo 1L)
            Assert.That(replay.Kind, Is.EqualTo EnvelopeKind.ProtocolError)
            Assert.That(categoryOf replay, Is.EqualTo ProtocolCategory.ReplayDetected)
            // A replayed identity must not repeat the effect.
            Assert.That(handler.ProviderEffectCount, Is.EqualTo effectsAfterFirst))
