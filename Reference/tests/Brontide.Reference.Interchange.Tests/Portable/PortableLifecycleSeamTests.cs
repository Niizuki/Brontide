using System.Collections.Immutable;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// PB6: the C8 lifecycle refusals, decided by a provider endpoint across a real seam.
/// </summary>
/// <remarks>
/// PB-09 and PB-36 through PB-39 drive the lifecycle object directly. That proves the state machine
/// rejects an illegal transition; it does not prove the endpoint applies it to an arriving frame,
/// which is where a peer's illegal sequence actually lands. The two can differ: a frame is decoded,
/// its kind resolved, and its body read before any state is consulted, so an endpoint can refuse for
/// the wrong reason, in the wrong order, or accept something the state machine alone would reject.
///
/// Each case sends a deliberate sequence and reads what came back. None of them may produce an
/// Outcome, and none may reach the provider.
/// </remarks>
public sealed class PortableLifecycleSeamTests
{
    /// <summary>What one hostile exchange produced.</summary>
    private sealed record Exchange(
        ImmutableArray<PortableEnvelope> Replies,
        long ProviderEffects)
    {
        public PortableEnvelope Last => Replies[^1];

        public PortableProtocolCategory Category =>
            PortableProtocolErrorBody.Decode(Last.Body).Category;
    }

    /// <summary>
    /// Runs a sequence of frames against a conforming endpoint and collects every reply.
    /// </summary>
    /// <remarks>
    /// The reply count is not known in advance — establishment answers twice, a refusal once, and a
    /// withdrawal not at all — so replies are read until the endpoint stops answering rather than a
    /// fixed number of times.
    /// </remarks>
    private static async Task<Exchange> ConverseAsync(
        PortableContractDocument offered,
        Func<PortableChannelId, IEnumerable<PortableEnvelope>> frames,
        int expectedReplies)
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        await using var seam = PortableLocalSeam.Create(PortableLimits.Declared);
        var endpoint = new PortableProviderEndpoint(offered, handler, PortableRealization.NegotiatedProcess);
        seam.StartProvider(endpoint, PortableLimits.Declared);

        var channel = PortableChannelId.New();
        foreach (var frame in frames(channel))
        {
            await seam.HostDuplex.SendAsync(PortableEnvelopeCodec.Encode(frame), CancellationToken.None);
        }

        var replies = ImmutableArray.CreateBuilder<PortableEnvelope>();
        for (var index = 0; index < expectedReplies; index++)
        {
            replies.Add(PortableEnvelopeCodec.Decode(
                await seam.HostDuplex.ReceiveAsync(CancellationToken.None),
                PortableLimits.Declared));
        }

        return new Exchange(replies.ToImmutable(), handler.ProviderEffectCount);
    }

    private static PortableEnvelope Establish(PortableChannelId channel, PortableContractDocument contract) =>
        PortableEnvelope.Control(
            PortableEnvelopeKind.Establish,
            channel,
            PortableContractCodec.Encode(contract));

    private static PortableEnvelope Request(PortableChannelId channel)
    {
        var catalog = PortableShapeCatalog.FromContract(CoolingPortableFixture.Contract);
        var body = new PortableRequestBody(
            CoolingPortableFixture.SetEnabled,
            null,
            CoolingPortableFixture.CommandV1,
            PortableValueCodec.Encode(
                catalog,
                CoolingPortableFixture.CommandV1,
                CoolingPortableFixture.Command("primary", enabled: true)),
            []);

        return new PortableEnvelope(
            PortableContractDocument.SupportedContractVersion,
            PortableEnvelopeKind.Request,
            channel,
            PortableChannelRequestId.New(),
            null,
            body.Encode());
    }

    /// <summary>
    /// Establishment that fails negotiation starts nothing: no readiness signal, no provider.
    /// </summary>
    /// <remarks>
    /// This is the C8 order that matters most. A provider that activated first and negotiated second
    /// would satisfy every other vector here and still be wrong.
    /// </remarks>
    [Test]
    public async Task An_establishment_that_fails_negotiation_never_signals_readiness_or_reaches_the_provider()
    {
        // The endpoint offers a contract without the profile provision the host requires.
        var exchange = await ConverseAsync(
            CoolingPortableFixture.WithoutProfileProvision(),
            channel => [Establish(channel, CoolingPortableFixture.Contract)],
            expectedReplies: 1);

        Assert.Multiple(() =>
        {
            Assert.That(exchange.Last.Kind, Is.EqualTo(PortableEnvelopeKind.ProtocolError));
            Assert.That(exchange.Category, Is.EqualTo(PortableProtocolCategory.UnsupportedContract));
            Assert.That(
                exchange.Replies.Select(reply => reply.Kind),
                Does.Not.Contain(PortableEnvelopeKind.Ready),
                "A binding that never negotiated must not report readiness.");
            Assert.That(
                exchange.Replies.Select(reply => reply.Kind),
                Does.Not.Contain(PortableEnvelopeKind.EstablishAccepted));
            Assert.That(exchange.ProviderEffects, Is.Zero);
        });
    }

    // PB-09-REQUEST-BEFORE-READY, over the seam rather than against the lifecycle object.
    [Test]
    public async Task A_request_before_any_establishment_is_refused_and_starts_no_provider()
    {
        var exchange = await ConverseAsync(
            CoolingPortableFixture.Contract,
            channel => [Request(channel)],
            expectedReplies: 1);

        Assert.Multiple(() =>
        {
            Assert.That(exchange.Last.Kind, Is.EqualTo(PortableEnvelopeKind.ProtocolError));
            Assert.That(exchange.Category, Is.EqualTo(PortableProtocolCategory.StateViolation));
            Assert.That(exchange.ProviderEffects, Is.Zero);
        });
    }

    // PB-39-ESTABLISH-ON-ESTABLISHED-BINDING, over the seam.
    [Test]
    public async Task A_second_establishment_is_refused_rather_than_renegotiated_in_place()
    {
        var exchange = await ConverseAsync(
            CoolingPortableFixture.Contract,
            channel =>
            [
                Establish(channel, CoolingPortableFixture.Contract),
                Establish(channel, CoolingPortableFixture.Contract)
            ],
            // establish-accepted, ready, then the refusal.
            expectedReplies: 3);

        Assert.Multiple(() =>
        {
            Assert.That(exchange.Replies[0].Kind, Is.EqualTo(PortableEnvelopeKind.EstablishAccepted));
            Assert.That(exchange.Replies[1].Kind, Is.EqualTo(PortableEnvelopeKind.Ready));
            Assert.That(exchange.Last.Kind, Is.EqualTo(PortableEnvelopeKind.ProtocolError));
            Assert.That(exchange.Category, Is.EqualTo(PortableProtocolCategory.StateViolation));
            Assert.That(exchange.ProviderEffects, Is.Zero);
        });
    }

    /// <summary>A request after withdrawal is refused, and the withdrawal itself is not answered.</summary>
    [Test]
    public async Task A_request_after_withdrawal_is_refused_and_starts_no_provider()
    {
        var exchange = await ConverseAsync(
            CoolingPortableFixture.Contract,
            channel =>
            [
                Establish(channel, CoolingPortableFixture.Contract),
                PortableEnvelope.Control(PortableEnvelopeKind.Withdraw, channel),
                Request(channel)
            ],
            expectedReplies: 3);

        Assert.Multiple(() =>
        {
            Assert.That(exchange.Replies[1].Kind, Is.EqualTo(PortableEnvelopeKind.Ready));
            Assert.That(
                exchange.Last.Kind,
                Is.EqualTo(PortableEnvelopeKind.ProtocolError),
                "A withdrawal is not answered, so the third reply is the refusal of the request.");
            Assert.That(exchange.Category, Is.EqualTo(PortableProtocolCategory.StateViolation));
            Assert.That(exchange.ProviderEffects, Is.Zero);
        });
    }

    /// <summary>
    /// An envelope kind that is declared, but is not one a provider endpoint receives.
    /// </summary>
    /// <remarks>
    /// This is the case the lifecycle object cannot express on its own: <c>outcome</c> is a legal
    /// kind in the taxonomy and a legal transition for a host, and only the direction makes it
    /// illegal here. A provider that switched on the kind without considering direction would treat
    /// its own reply shape as an instruction.
    /// </remarks>
    [TestCaseSource(nameof(KindsNotDirectedAtAProvider))]
    public async Task A_kind_a_provider_never_receives_is_refused_as_a_state_violation(PortableEnvelopeKind kind)
    {
        var exchange = await ConverseAsync(
            CoolingPortableFixture.Contract,
            channel =>
            [
                Establish(channel, CoolingPortableFixture.Contract),
                WellFormed(kind, channel)
            ],
            expectedReplies: 3);

        Assert.Multiple(() =>
        {
            Assert.That(exchange.Last.Kind, Is.EqualTo(PortableEnvelopeKind.ProtocolError));
            Assert.That(exchange.Category, Is.EqualTo(PortableProtocolCategory.StateViolation));
            Assert.That(exchange.ProviderEffects, Is.Zero);
        });
    }

    public static IEnumerable<PortableEnvelopeKind> KindsNotDirectedAtAProvider =>
    [
        PortableEnvelopeKind.EstablishAccepted,
        PortableEnvelopeKind.Ready,
        PortableEnvelopeKind.Outcome
    ];

    /// <summary>
    /// A frame of the given kind that is well formed, so the refusal is about its direction rather
    /// than about its shape.
    /// </summary>
    /// <remarks>
    /// An <c>outcome</c> carries a correlation identity by declaration, so one built without a
    /// request identity is refused as malformed long before anything considers who may send it. That
    /// ordering is correct — a frame that cannot be read has no direction to judge — but it would
    /// make this vector assert the wrong rule.
    /// </remarks>
    private static PortableEnvelope WellFormed(PortableEnvelopeKind kind, PortableChannelId channel) =>
        kind == PortableEnvelopeKind.Outcome
            ? new PortableEnvelope(
                PortableContractDocument.SupportedContractVersion,
                kind,
                channel,
                PortableChannelRequestId.New(),
                null,
                CborMap.Empty)
            : PortableEnvelope.Control(kind, channel);

    /// <summary>
    /// A frame that cannot be read is refused as malformed, before its kind's direction is weighed.
    /// </summary>
    /// <remarks>
    /// Recorded as its own case because the ordering is easy to get backwards, and getting it
    /// backwards would report a shape problem as a state problem — telling a peer to fix its
    /// sequencing when its encoder is what is wrong.
    /// </remarks>
    [Test]
    public async Task A_malformed_frame_is_refused_before_its_direction_is_considered()
    {
        var exchange = await ConverseAsync(
            CoolingPortableFixture.Contract,
            channel =>
            [
                Establish(channel, CoolingPortableFixture.Contract),

                // An outcome without the correlation identity its kind declares.
                PortableEnvelope.Control(PortableEnvelopeKind.Outcome, channel)
            ],
            expectedReplies: 3);

        Assert.Multiple(() =>
        {
            Assert.That(exchange.Category, Is.EqualTo(PortableProtocolCategory.MalformedMessage));
            Assert.That(exchange.ProviderEffects, Is.Zero);
        });
    }

    // PB-45-UNKNOWN-ENVELOPE-KIND, over the seam rather than against the codec.
    [Test]
    public async Task An_unrecognized_envelope_kind_is_refused_before_any_state_is_consulted()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        await using var seam = PortableLocalSeam.Create(PortableLimits.Declared);
        var endpoint = new PortableProviderEndpoint(
            CoolingPortableFixture.Contract,
            handler,
            PortableRealization.NegotiatedProcess);
        seam.StartProvider(endpoint, PortableLimits.Declared);

        // Built as raw CBOR: the envelope type cannot represent a kind the taxonomy does not declare.
        var gossip = CborMap.Of(
        [
            ("contractVersion", new CborInteger(PortableContractDocument.SupportedContractVersion)),
            ("kind", new CborText("gossip")),
            ("channelId", new CborText(PortableChannelId.New().Value)),
            ("body", CborMap.Empty)
        ]);

        await seam.HostDuplex.SendAsync(PortableCbor.Encode(gossip), CancellationToken.None);
        var reply = PortableEnvelopeCodec.Decode(
            await seam.HostDuplex.ReceiveAsync(CancellationToken.None),
            PortableLimits.Declared);

        Assert.Multiple(() =>
        {
            Assert.That(reply.Kind, Is.EqualTo(PortableEnvelopeKind.ProtocolError));
            Assert.That(
                PortableProtocolErrorBody.Decode(reply.Body).Category,
                Is.EqualTo(PortableProtocolCategory.UnsupportedKind));
            Assert.That(handler.ProviderEffectCount, Is.Zero);
        });
    }

    /// <summary>
    /// A repeated request identity inside the declared window is refused without repeating the
    /// effect, and the endpoint says so rather than answering twice.
    /// </summary>
    [Test]
    public async Task A_replayed_request_identity_is_refused_without_repeating_the_effect()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        await using var seam = PortableLocalSeam.Create(PortableLimits.Declared);
        var endpoint = new PortableProviderEndpoint(
            CoolingPortableFixture.Contract,
            handler,
            PortableRealization.NegotiatedProcess);
        seam.StartProvider(endpoint, PortableLimits.Declared);

        var channel = PortableChannelId.New();
        await seam.HostDuplex.SendAsync(
            PortableEnvelopeCodec.Encode(Establish(channel, CoolingPortableFixture.Contract)),
            CancellationToken.None);
        _ = await seam.HostDuplex.ReceiveAsync(CancellationToken.None);
        _ = await seam.HostDuplex.ReceiveAsync(CancellationToken.None);

        var request = Request(channel);
        await seam.HostDuplex.SendAsync(PortableEnvelopeCodec.Encode(request), CancellationToken.None);
        var outcome = PortableEnvelopeCodec.Decode(
            await seam.HostDuplex.ReceiveAsync(CancellationToken.None),
            PortableLimits.Declared);
        var effectsAfterFirst = handler.ProviderEffectCount;

        await seam.HostDuplex.SendAsync(PortableEnvelopeCodec.Encode(request), CancellationToken.None);
        var replay = PortableEnvelopeCodec.Decode(
            await seam.HostDuplex.ReceiveAsync(CancellationToken.None),
            PortableLimits.Declared);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(PortableEnvelopeKind.Outcome));
            Assert.That(effectsAfterFirst, Is.EqualTo(1));
            Assert.That(replay.Kind, Is.EqualTo(PortableEnvelopeKind.ProtocolError));
            Assert.That(
                PortableProtocolErrorBody.Decode(replay.Body).Category,
                Is.EqualTo(PortableProtocolCategory.ReplayDetected));
            Assert.That(
                handler.ProviderEffectCount,
                Is.EqualTo(effectsAfterFirst),
                "A replayed identity must not repeat the effect.");
        });
    }
}
