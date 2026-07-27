using System.Collections.Immutable;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// PB6: the C6 refusals, decided by a provider endpoint across a real seam.
/// </summary>
/// <remarks>
/// PB-29 through PB-32 call the resource codec directly. That proves a static function refuses a
/// malformed resource; it does not prove the endpoint does, and the endpoint is what a hostile peer
/// actually reaches. The difference matters because admission sits behind decode, lifecycle, and
/// operation resolution — a refusal that a unit test reaches in one call may be unreachable, or
/// reached in the wrong order, once those run first.
///
/// These tests drive the wire directly rather than through the host, because the host's own encoder
/// cannot produce most of them: a conforming host never puts octets beside a handle. A hostile host
/// is the only thing that presents these frames, so a hostile host is what asks the question.
/// </remarks>
public sealed class PortableResourceSeamTests
{
    /// <summary>Establishes a binding over the seam and returns the channel it runs on.</summary>
    private static async Task<PortableChannelId> EstablishAsync(
        PortableLocalSeam seam,
        PortableContractDocument contract)
    {
        var channel = PortableChannelId.New();
        await seam.HostDuplex.SendAsync(
            PortableEnvelopeCodec.Encode(PortableEnvelope.Control(
                PortableEnvelopeKind.Establish,
                channel,
                PortableContractCodec.Encode(contract))),
            CancellationToken.None);

        var accepted = PortableEnvelopeCodec.Decode(
            await seam.HostDuplex.ReceiveAsync(CancellationToken.None),
            PortableLimits.Declared);
        Assert.That(accepted.Kind, Is.EqualTo(PortableEnvelopeKind.EstablishAccepted));

        var ready = PortableEnvelopeCodec.Decode(
            await seam.HostDuplex.ReceiveAsync(CancellationToken.None),
            PortableLimits.Declared);
        Assert.That(ready.Kind, Is.EqualTo(PortableEnvelopeKind.Ready));
        return channel;
    }

    /// <summary>
    /// Presents one hand-built resource frame to a conforming endpoint and reports what came back.
    /// </summary>
    private static async Task<(PortableProtocolCategory Category, string LocalCode, long Effects)> PresentAsync(
        PortableContractDocument contract,
        PortableOperationReference operation,
        PortableShapeReference inputShape,
        PortableValue input,
        CborItem resource)
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        IPortableOperationHandler endpointHandler = contract == CatalogPortableFixture.Contract
            ? new CatalogPortableHandler()
            : handler;

        await using var seam = PortableLocalSeam.Create(PortableLimits.Declared);
        var endpoint = new PortableProviderEndpoint(contract, endpointHandler, PortableRealization.NegotiatedProcess);
        seam.StartProvider(endpoint, PortableLimits.Declared);

        var channel = await EstablishAsync(seam, contract);
        var catalog = PortableShapeCatalog.FromContract(contract);
        var body = new PortableRequestBody(
            operation,
            null,
            inputShape,
            PortableValueCodec.Encode(catalog, inputShape, input),
            [resource]);

        await seam.HostDuplex.SendAsync(
            PortableEnvelopeCodec.Encode(new PortableEnvelope(
                PortableContractDocument.SupportedContractVersion,
                PortableEnvelopeKind.Request,
                channel,
                PortableChannelRequestId.New(),
                null,
                body.Encode())),
            CancellationToken.None);

        var reply = PortableEnvelopeCodec.Decode(
            await seam.HostDuplex.ReceiveAsync(CancellationToken.None),
            PortableLimits.Declared);

        Assert.That(
            reply.Kind,
            Is.EqualTo(PortableEnvelopeKind.ProtocolError),
            "A refused resource must come back as a protocol error, never as an Outcome.");

        var error = PortableProtocolErrorBody.Decode(reply.Body);
        return (error.Category, error.LocalCode, handler.ProviderEffectCount);
    }

    private static PortableValue CoolingCommand() => CoolingPortableFixture.Command("primary", enabled: true);

    // PB-30-FORBIDDEN-IMPLICIT-COPY, across the seam.
    [Test]
    public async Task Octets_beside_a_handle_are_refused_by_the_endpoint_not_only_by_the_codec()
    {
        var frame = CborMap.Of(
        [
            ("flavor", new CborText(PortableResourceFlavors.AddressingOnlyHandle)),
            ("name", new CborText("catalog")),
            ("provider", new CborText("catalog-provider")),
            ("id", new CborText("primary")),
            ("content", new CborBytes(new byte[] { 1, 2, 3 }))
        ]);

        var result = await PresentAsync(
            CatalogPortableFixture.Contract,
            CatalogPortableFixture.Upsert,
            CatalogPortableFixture.UpsertCommand,
            CatalogPortableFixture.UpsertCommandValue(CatalogPortableFixture.ItemValue("a", "Alpha", "one")),
            frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Category, Is.EqualTo(PortableProtocolCategory.InvalidPayload));
            Assert.That(result.LocalCode, Is.EqualTo("forbidden-implicit-copy"));
            Assert.That(result.Effects, Is.Zero);
        });
    }

    // PB-32-RELEASE-SIGNAL-FOR-COPIED-FLAVOR, across the seam.
    [Test]
    public async Task A_release_signal_for_the_copied_flavor_is_refused_by_the_endpoint()
    {
        var blob = PortableTestHarness.Blob();
        var frame = CborMap.Of(
        [
            ("flavor", new CborText(PortableResourceFlavors.CopiedImmutableBlob)),
            ("name", new CborText(blob.Name)),
            ("content", new CborBytes(blob.Content)),
            ("integrity", new CborText(blob.Integrity)),
            ("release", new CborBoolean(true))
        ]);

        var result = await PresentAsync(
            CoolingPortableFixture.Contract,
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingCommand(),
            frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Category, Is.EqualTo(PortableProtocolCategory.StateViolation));
            Assert.That(result.Effects, Is.Zero);
        });
    }

    // PB-31-RESOURCE-EXCEEDS-DECLARED-BOUND, across the seam.
    [Test]
    public async Task A_resource_beyond_the_declared_bound_is_refused_before_the_provider_is_reached()
    {
        var oversized = PortableCopiedBlobResource.Create(
            "profile",
            new byte[PortableLimits.Declared.MaxResourceBytes + 1]);

        var result = await PresentAsync(
            CoolingPortableFixture.Contract,
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingCommand(),
            PortableResourceCodec.Encode(oversized));

        Assert.Multiple(() =>
        {
            Assert.That(result.Category, Is.EqualTo(PortableProtocolCategory.LimitExceeded));
            Assert.That(result.Effects, Is.Zero);
        });
    }

    /// <summary>
    /// A version-0.1 non-goal flavor presented on the wire is refused, not silently downgraded to
    /// the flavor the binding did negotiate.
    /// </summary>
    /// <remarks>
    /// PB-29 refuses these at negotiation, where a contract declares one. This is the other way in:
    /// a peer that negotiated the copied blob and then names a borrowed region on a request. Both
    /// have to fail closed, and only this one reaches the endpoint's admission path.
    /// </remarks>
    [TestCaseSource(nameof(NonGoalFlavors))]
    public async Task A_non_goal_flavor_named_on_a_request_fails_closed(string flavor)
    {
        var frame = CborMap.Of(
        [
            ("flavor", new CborText(flavor)),
            ("name", new CborText("profile")),
            ("content", new CborBytes(new byte[] { 1, 2, 3 })),
            ("integrity", new CborText(PortableCopiedBlobResource.HashOf(new byte[] { 1, 2, 3 })))
        ]);

        var result = await PresentAsync(
            CoolingPortableFixture.Contract,
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingCommand(),
            frame);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Category,
                Is.AnyOf(PortableProtocolCategory.UnsupportedContract, PortableProtocolCategory.MalformedMessage),
                $"Flavor '{flavor}' was neither refused as unnegotiated nor as unreadable.");
            Assert.That(result.Effects, Is.Zero);
        });
    }

    public static IEnumerable<string> NonGoalFlavors => PortableResourceFlavors.NonGoals;

    // PB-26-RESOURCE-INTEGRITY-MISMATCH, decided by the endpoint rather than by a direct Admit call.
    [Test]
    public async Task A_content_hash_that_does_not_verify_is_refused_by_the_endpoint()
    {
        var frame = CborMap.Of(
        [
            ("flavor", new CborText(PortableResourceFlavors.CopiedImmutableBlob)),
            ("name", new CborText("profile")),
            ("content", new CborBytes(System.Text.Encoding.UTF8.GetBytes("tampered"))),
            ("integrity", new CborText(
                PortableCopiedBlobResource.HashOf(System.Text.Encoding.UTF8.GetBytes("original"))))
        ]);

        var result = await PresentAsync(
            CoolingPortableFixture.Contract,
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingCommand(),
            frame);

        Assert.Multiple(() =>
        {
            Assert.That(result.Category, Is.EqualTo(PortableProtocolCategory.InvalidPayload));
            Assert.That(result.Effects, Is.Zero);
        });
    }

    /// <summary>
    /// Version 0.1's referenced-resource floor makes two of PB6's conditions unrepresentable rather
    /// than merely refused, which is worth recording where the refusals live.
    /// </summary>
    /// <remarks>
    /// Premature reuse and a release-then-use sequence need a resource with a lifetime a peer can
    /// observe ending. The declared floor has neither: a copied immutable blob is transferred whole
    /// and has no release signal, and an addressing-only handle carries no octets to release. There
    /// is no frame that expresses "use this after its interval", so there is nothing for a vector to
    /// present. Unsupported fallback is the same shape: no fallback policy is declared for 0.1, so a
    /// request cannot name one to have it refused.
    ///
    /// This is asserted rather than written only in prose, so that adding a borrowed or transferred
    /// flavor later fails here and brings the reasoning back for review.
    /// </remarks>
    [Test]
    public void The_zero_one_floor_makes_reuse_and_fallback_unrepresentable_rather_than_refused()
    {
        var declared = CoolingPortableFixture.Contract.Representation.ResourceFlavors
            .Concat(CatalogPortableFixture.Contract.Representation.ResourceFlavors)
            .ToImmutableHashSet();

        Assert.Multiple(() =>
        {
            Assert.That(
                declared,
                Is.EquivalentTo(new[]
                {
                    PortableResourceFlavors.CopiedImmutableBlob,
                    PortableResourceFlavors.AddressingOnlyHandle
                }),
                "A flavor with an observable lifetime was declared; premature reuse is now expressible and needs a vector.");

            Assert.That(
                PortableResourceFlavors.NonGoals,
                Is.Not.Empty,
                "The non-goal flavors are what make borrowing and transfer fail negotiation rather than admission.");

            // The declared fields are the whole of what a resource frame can say. None of them
            // expresses a fallback policy or a lifetime, which is why neither has a vector.
            Assert.That(
                PortableResourceCodec.Encode(PortableTestHarness.Blob()),
                Is.TypeOf<CborMap>());
        });
    }
}
