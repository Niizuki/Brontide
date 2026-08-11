using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>Neutral vectors PB-33 through PB-52: declared limits, lifecycle, and the Channel taxonomy.</summary>
public sealed class PortableLifecycleAndChannelTests
{
    private static readonly PortableShapeCatalog Catalog =
        PortableShapeCatalog.FromContract(CoolingPortableFixture.Contract);

    private static PortableFaultException Fault(Action action) =>
        Assert.Throws<PortableFaultException>(action)!;

    private static PortableProviderEndpoint ReadyEndpoint(
        IPortableOperationHandler? handler = null,
        PortableRealization realization = PortableRealization.FixedDirectCall)
    {
        var endpoint = PortableTestHarness.CoolingEndpoint(realization, handler);
        endpoint.Establish(CoolingPortableFixture.Contract, "host");
        endpoint.SignalReady();
        return endpoint;
    }

    // PB-33-FRAME-EXCEEDS-DECLARED-BOUND
    [Test]
    public void A_length_prefix_beyond_the_declared_bound_is_refused_on_the_prefix_alone()
    {
        var prefix = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(prefix, (uint)PortableLimits.Declared.MaxFrameBytes + 1);
        using var stream = new MemoryStream(prefix);

        var fault = Assert.ThrowsAsync<PortableFaultException>(async () =>
            await PortableFraming.ReadFrameAsync(stream, PortableLimits.Declared, CancellationToken.None));
        Assert.Multiple(() =>
        {
            Assert.That(fault!.Category, Is.EqualTo(PortableProtocolCategory.LimitExceeded));
            Assert.That(stream.Position, Is.EqualTo(4), "The body was never read.");
        });
    }

    // PB-34-NESTING-EXCEEDS-DECLARED-DEPTH
    [Test]
    public void Nesting_beyond_the_declared_depth_stops_decoding_rather_than_recursing()
    {
        var bytes = new byte[PortableLimits.Declared.MaxNestingDepth + 2];
        Array.Fill(bytes, (byte)0x81, 0, bytes.Length - 1);
        bytes[^1] = 0xF6;

        Assert.That(
            Fault(() => PortableCbor.Decode(bytes, PortableLimits.Declared)).Category,
            Is.EqualTo(PortableProtocolCategory.LimitExceeded));
    }

    // PB-35-REPLAY-INSIDE-DECLARED-WINDOW
    [Test]
    public void A_repeated_request_identity_inside_the_window_does_not_repeat_the_effect()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        var endpoint = ReadyEndpoint(handler);
        var request = PortableChannelRequestId.New();
        var input = CoolingPortableFixture.Command("primary", enabled: true);

        endpoint.Request(request, CoolingPortableFixture.SetEnabled, null, CoolingPortableFixture.CommandV1, input, []);
        var effectsAfterFirst = handler.ProviderEffectCount;

        Assert.Multiple(() =>
        {
            Assert.That(
                Fault(() => endpoint.Request(
                    request,
                    CoolingPortableFixture.SetEnabled,
                    null,
                    CoolingPortableFixture.CommandV1,
                    input,
                    [])).Category,
                Is.EqualTo(PortableProtocolCategory.ReplayDetected));
            Assert.That(handler.ProviderEffectCount, Is.EqualTo(effectsAfterFirst));
        });
    }

    // PB-36-SECOND-CONCURRENT-REQUEST
    [Test]
    public void A_second_request_while_one_is_outstanding_is_refused()
    {
        var lifecycle = ReadyLifecycle();
        lifecycle.Apply(PortableEnvelopeKind.Request);

        Assert.Multiple(() =>
        {
            Assert.That(lifecycle.State, Is.EqualTo(PortableLifecycleState.Active));
            Assert.That(
                Fault(() => lifecycle.Apply(PortableEnvelopeKind.Request)).Category,
                Is.EqualTo(PortableProtocolCategory.StateViolation));
        });
    }

    // PB-37-DUPLICATE-TERMINAL-OUTCOME
    [Test]
    public void A_duplicate_terminal_outcome_does_not_overwrite_the_first()
    {
        var lifecycle = ReadyLifecycle();
        lifecycle.Apply(PortableEnvelopeKind.Request);
        lifecycle.Apply(PortableEnvelopeKind.Outcome);

        Assert.Multiple(() =>
        {
            Assert.That(lifecycle.State, Is.EqualTo(PortableLifecycleState.Ready));
            Assert.That(
                Fault(() => lifecycle.Apply(PortableEnvelopeKind.Outcome)).Category,
                Is.EqualTo(PortableProtocolCategory.StateViolation));
        });
    }

    // PB-38-CLEAN-WITHDRAWAL-AND-TERMINATION
    [Test]
    public void Withdrawal_and_termination_are_legal_and_close_the_binding_to_new_requests()
    {
        var lifecycle = ReadyLifecycle();
        lifecycle.Apply(PortableEnvelopeKind.Withdraw);
        Assert.That(lifecycle.State, Is.EqualTo(PortableLifecycleState.Withdrawn));
        Assert.That(
            Fault(() => lifecycle.Apply(PortableEnvelopeKind.Request)).Category,
            Is.EqualTo(PortableProtocolCategory.StateViolation));

        lifecycle.Apply(PortableEnvelopeKind.Terminate);
        Assert.Multiple(() =>
        {
            Assert.That(lifecycle.State, Is.EqualTo(PortableLifecycleState.Terminated));
            Assert.That(lifecycle.State.IsTerminal(), Is.True);
            Assert.That(
                Fault(() => lifecycle.Apply(PortableEnvelopeKind.Terminate)).Category,
                Is.EqualTo(PortableProtocolCategory.StateViolation));
        });
    }

    // PB-84-OUTCOME-AFTER-WITHDRAWAL-REFUSED
    [Test]
    public void An_outcome_after_withdrawal_is_refused_because_no_request_remains_active()
    {
        var lifecycle = new PortableLifecycle(replayProtectionDeclared: true);
        lifecycle.Apply(PortableEnvelopeKind.Establish);
        lifecycle.Apply(PortableEnvelopeKind.EstablishAccepted);
        lifecycle.Apply(PortableEnvelopeKind.Ready);
        lifecycle.Apply(PortableEnvelopeKind.Request);
        lifecycle.Apply(PortableEnvelopeKind.Withdraw);

        Assert.Multiple(() =>
        {
            Assert.That(lifecycle.State, Is.EqualTo(PortableLifecycleState.Withdrawn));
            Assert.That(
                Fault(() => lifecycle.Apply(PortableEnvelopeKind.Outcome)).Category,
                Is.EqualTo(PortableProtocolCategory.StateViolation));
        });
    }

    // PB-39-ESTABLISH-ON-ESTABLISHED-BINDING
    [Test]
    public void A_second_establish_on_the_same_binding_is_refused_rather_than_renegotiated_in_place()
    {
        var endpoint = PortableTestHarness.CoolingEndpoint(PortableRealization.FixedDirectCall);
        var first = endpoint.Establish(CoolingPortableFixture.Contract, "host");

        Assert.Multiple(() =>
        {
            Assert.That(first.Contract.Component, Is.EqualTo(CoolingPortableFixture.Component));
            Assert.That(
                Fault(() => endpoint.Establish(CoolingPortableFixture.Contract, "host")).Category,
                Is.EqualTo(PortableProtocolCategory.StateViolation));
        });
    }

    // PB-40-IO-TIMEOUT
    [Test]
    public void No_frame_within_the_declared_bound_is_a_timeout_rather_than_a_fabricated_success()
    {
        // A silent stream stands in for the peer: an anonymous pipe read does not observe
        // cancellation on Windows, so it could not distinguish a timeout from a blocked test.
        var limits = PortableLimits.Declared with { IoTimeoutMilliseconds = 100 };
        var duplex = new PortableStreamDuplex(new SilentStream(), Stream.Null, limits, ownsStreams: false);

        var failure = Assert.ThrowsAsync<PortableProcessFailureException>(async () =>
            await duplex.ReceiveAsync(CancellationToken.None));
        Assert.Multiple(() =>
        {
            Assert.That(failure!.Category, Is.EqualTo(PortableProcessCategory.Timeout));
            Assert.That(failure.Domain, Is.EqualTo(PortableFailureDomain.Transport));
        });
    }

    // PB-41-INTERRUPTED-FRAME
    [Test]
    public void A_stream_that_ends_inside_a_declared_body_is_an_interruption_not_a_malformed_message()
    {
        var prefix = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(prefix, 10);
        using var stream = new MemoryStream([.. prefix, 1, 2, 3]);

        var failure = Assert.ThrowsAsync<PortableProcessFailureException>(async () =>
            await PortableFraming.ReadFrameAsync(stream, PortableLimits.Declared, CancellationToken.None));
        Assert.Multiple(() =>
        {
            Assert.That(failure!.Category, Is.EqualTo(PortableProcessCategory.TransportInterrupted));
            Assert.That(failure.Domain, Is.EqualTo(PortableFailureDomain.Transport));
        });
    }

    [Test]
    public void A_stream_that_ends_inside_the_length_prefix_is_also_an_interruption()
    {
        // Two bytes of a four-byte prefix is not a clean end between frames; reporting it as a
        // terminated peer would attribute the loss to the wrong domain.
        using var partial = new MemoryStream([0, 0]);
        var interrupted = Assert.ThrowsAsync<PortableProcessFailureException>(async () =>
            await PortableFraming.ReadFrameAsync(partial, PortableLimits.Declared, CancellationToken.None));

        using var empty = new MemoryStream([]);
        var terminated = Assert.ThrowsAsync<PortableProcessFailureException>(async () =>
            await PortableFraming.ReadFrameAsync(empty, PortableLimits.Declared, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(interrupted!.Category, Is.EqualTo(PortableProcessCategory.TransportInterrupted));
            Assert.That(interrupted.Domain, Is.EqualTo(PortableFailureDomain.Transport));
            Assert.That(terminated!.Category, Is.EqualTo(PortableProcessCategory.PeerTerminated));
            Assert.That(terminated.Domain, Is.EqualTo(PortableFailureDomain.RemoteProvider));
        });
    }

    // PB-42-CORRELATION-ECHO
    [Test]
    public async Task An_Outcome_echoing_every_carried_identity_is_accepted()
    {
        var (host, seam) = await PortableTestHarness.ProcessHostAsync();
        await using (seam)
        await using (host)
        {
            var result = await host.InvokeAsync(
                CoolingPortableFixture.SetEnabled,
                CoolingPortableFixture.CommandV1,
                CoolingPortableFixture.Command("primary", enabled: true),
                PortableTestHarness.Permitted(),
                execution: PortableChannelExecutionId.New());

            Assert.Multiple(() =>
            {
                Assert.That(result.FrameDecision, Is.EqualTo(PortableFrameDecision.Accept));
                Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.OutcomeSucceeded));
                Assert.That(
                    result.Observation.CorrelationMapping.HostNativeExecution.Value,
                    Is.Not.EqualTo(result.Observation.CorrelationMapping.RequestId.Value));
            });
        }
    }

    // PB-43-CORRELATION-MISMATCH
    [Test]
    public async Task An_Outcome_claiming_a_request_this_host_never_sent_is_not_accepted()
    {
        await using var seam = PortableLocalSeam.Create(PortableLimits.Declared);
        seam.StartPeer((duplex, token) => MisleadingPeerAsync(duplex, token, echoCorrelation: false));
        await using var host = await PortableBindingHost.EstablishAsync(
            CoolingPortableFixture.Contract,
            new PortableProcessConversation(seam.HostDuplex, PortableLimits.Declared),
            "reference-process");

        var result = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableTestHarness.Permitted());

        Assert.Multiple(() =>
        {
            Assert.That(result.Category, Is.EqualTo(PortableProtocolCategory.CorrelationMismatch));
            Assert.That(result.Observation.FailureDomain, Is.EqualTo(PortableFailureDomain.LocalEndpoint));
            Assert.That(
                result.Observation.ProviderEffectCount,
                Is.Null,
                "A mismatched Outcome cannot attribute the peer provider's effect.");
        });
    }

    // PB-44-MISSING-CORRELATION-IDENTITY
    [Test]
    public void An_Outcome_without_a_request_identity_is_not_matched_by_position()
    {
        var envelope = CborMap.Of(
        [
            ("contractVersion", new CborInteger(1)),
            ("kind", new CborText("outcome")),
            ("channelId", new CborText("c1")),
            ("body", CborMap.Empty)
        ]);

        Assert.That(
            Fault(() => PortableEnvelopeCodec.Decode(PortableCbor.Encode(envelope), PortableLimits.Declared)).Category,
            Is.EqualTo(PortableProtocolCategory.MalformedMessage));
    }

    // PB-45-UNKNOWN-ENVELOPE-KIND
    [Test]
    public void An_unrecognized_envelope_kind_is_never_treated_as_a_no_op()
    {
        var envelope = CborMap.Of(
        [
            ("contractVersion", new CborInteger(1)),
            ("kind", new CborText("gossip")),
            ("channelId", new CborText("c1")),
            ("body", CborMap.Empty)
        ]);

        Assert.That(
            Fault(() => PortableEnvelopeCodec.Decode(PortableCbor.Encode(envelope), PortableLimits.Declared)).Category,
            Is.EqualTo(PortableProtocolCategory.UnsupportedKind));
    }

    // PB-46-UNSUPPORTED-OPERATION
    [Test]
    public void A_request_naming_an_Operation_outside_the_contract_begins_no_provider_effect()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        var endpoint = ReadyEndpoint(handler);

        Assert.Multiple(() =>
        {
            Assert.That(
                Fault(() => endpoint.Request(
                    PortableChannelRequestId.New(),
                    PortableOperationReference.Parse("interchange.tests.cooling.set-disabled", 1),
                    null,
                    CoolingPortableFixture.CommandV1,
                    CoolingPortableFixture.Command("primary", enabled: true),
                    [])).Category,
                Is.EqualTo(PortableProtocolCategory.UnsupportedOperation));
            Assert.That(handler.ProviderEffectCount, Is.Zero);
        });
    }

    [Test]
    public async Task An_Operation_outside_the_contract_is_reported_as_a_result_not_raised_at_the_caller()
    {
        // The host reports every terminal interaction as an observation. A rejection decided while
        // preparing the request is still a rejection, so it must not escape as an exception.
        await using var host = await PortableTestHarness.DirectHostAsync();
        var result = await host.InvokeAsync(
            PortableOperationReference.Parse("interchange.tests.cooling.set-disabled", 1),
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableTestHarness.Permitted());

        Assert.Multiple(() =>
        {
            Assert.That(result.Category, Is.EqualTo(PortableProtocolCategory.UnsupportedOperation));
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.ProtocolError));
            Assert.That(result.Observation.TerminalStatus, Is.EqualTo(PortableTerminalStatus.ProtocolError));
            Assert.That(result.Observation.ProviderEffectCount, Is.Zero);
        });
    }

    // PB-47-SEMANTIC-FAILED-OUTCOME
    [Test]
    public async Task A_shaped_failed_Outcome_is_not_a_protocol_or_process_failure()
    {
        await using var host = await PortableTestHarness.DirectHostAsync();
        var result = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true, failureMode: "requested-failure"),
            PortableTestHarness.Permitted());

        Assert.Multiple(() =>
        {
            Assert.That(result.FrameDecision, Is.EqualTo(PortableFrameDecision.Accept));
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.OutcomeFailed));
            Assert.That(result.Category, Is.Null);
            Assert.That(result.ProcessCategory, Is.Null);
            Assert.That(((PortableRecordValue)result.Value!).Fields, Does.ContainKey("code"));
            Assert.That(result.Observation.TerminalStatus, Is.EqualTo(PortableTerminalStatus.Failed));
        });
    }

    // PB-48-LOCAL-CODE-IS-NON-NORMATIVE
    [Test]
    public void Two_realizations_may_use_different_local_codes_for_the_same_portable_category()
    {
        var first = new PortableFaultException(
            PortableProtocolCategory.UnsupportedContract,
            "unsupported-protocol",
            "one realization's wording");
        var second = new PortableFaultException(
            PortableProtocolCategory.UnsupportedContract,
            "contract-refused",
            "another realization's wording");

        Assert.Multiple(() =>
        {
            Assert.That(first.Category, Is.EqualTo(second.Category));
            Assert.That(first.LocalCode, Is.Not.EqualTo(second.LocalCode));
            Assert.That(first.Category.Token(), Is.EqualTo("unsupported-contract"));
        });
    }

    // PB-49-INTERNAL-PROTOCOL-FAILURE
    [Test]
    public async Task A_local_runtime_failure_crosses_as_a_category_and_nothing_else()
    {
        await using var host = await PortableTestHarness.DirectHostAsync(handler: new CollapsingHandler());
        var result = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableTestHarness.Permitted());

        Assert.Multiple(() =>
        {
            Assert.That(result.Category, Is.EqualTo(PortableProtocolCategory.InternalProtocolFailure));
            Assert.That(result.Observation.LocalMessage, Does.Not.Contain("Exception"));
            Assert.That(result.Observation.LocalMessage, Does.Not.Contain("CollapsingHandler"));
        });
    }

    // PB-50-PEER-TERMINATED
    [Test]
    public async Task A_peer_that_ends_between_frames_is_observed_rather_than_signalled()
    {
        await using var seam = PortableLocalSeam.Create(PortableLimits.Declared);
        seam.StartPeer((duplex, token) => MisleadingPeerAsync(duplex, token, echoCorrelation: false, answer: false));
        await using var host = await PortableBindingHost.EstablishAsync(
            CoolingPortableFixture.Contract,
            new PortableProcessConversation(seam.HostDuplex, PortableLimits.Declared),
            "reference-process");

        seam.CloseProviderOutput();
        var result = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableTestHarness.Permitted());

        Assert.Multiple(() =>
        {
            Assert.That(result.FrameDecision, Is.EqualTo(PortableFrameDecision.None));
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.ProcessFailure));
            Assert.That(result.ProcessCategory, Is.EqualTo(PortableProcessCategory.PeerTerminated));
            Assert.That(result.Observation.FailureDomain, Is.EqualTo(PortableFailureDomain.RemoteProvider));
            Assert.That(result.Observation.TerminalStatus, Is.EqualTo(PortableTerminalStatus.ProcessFailure));
            Assert.That(
                result.Observation.ProviderEffectCount,
                Is.Null,
                "A terminated peer cannot prove whether its provider performed the requested effect.");
        });
    }

    // PB-51-PROCESS-FAILURE-CATEGORY-SELECTION
    [Test]
    public void Exactly_one_process_category_is_selected_and_the_declared_set_is_complete()
    {
        var declared = Enum.GetValues<PortableProcessCategory>().Select(category => category.Token()).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(declared, Is.EquivalentTo(new[]
            {
                "transport-unavailable", "transport-interrupted", "timeout", "peer-terminated",
                "peer-unavailable", "resource-exhausted", "unknown"
            }));
            Assert.That(declared, Is.Unique);
            Assert.That(
                new PortableProcessFailureException(
                    PortableProcessCategory.Unknown,
                    PortableFailureDomain.Unknown,
                    "narrower attribution was impossible: the seam reported no reason").Message,
                Does.Contain("narrower attribution"),
                "Unknown retains why narrower attribution was impossible.");
        });
    }

    // PB-52-FAILURE-DOMAIN-RELATIVITY
    [Test]
    public void A_failure_domain_is_recorded_relative_to_the_observer()
    {
        var declared = Enum.GetValues<PortableFailureDomain>().Select(domain => domain.Token()).ToArray();
        var local = new PortableFaultException(
            PortableProtocolCategory.CorrelationMismatch,
            "correlation-mismatch",
            "decided here");
        var remote = new PortableFaultException(
            PortableProtocolCategory.InvalidPayload,
            "peer-refused",
            "reported back",
            PortableFailureDomain.RemoteEndpoint);

        Assert.Multiple(() =>
        {
            Assert.That(declared, Is.EquivalentTo(new[]
            {
                "local-endpoint", "transport", "remote-endpoint", "remote-provider", "unknown"
            }));
            Assert.That(local.Domain, Is.EqualTo(PortableFailureDomain.LocalEndpoint));
            Assert.That(remote.Domain, Is.EqualTo(PortableFailureDomain.RemoteEndpoint));
        });
    }

    private static PortableLifecycle ReadyLifecycle()
    {
        var lifecycle = new PortableLifecycle(replayProtectionDeclared: true);
        lifecycle.Apply(PortableEnvelopeKind.Establish);
        lifecycle.Apply(PortableEnvelopeKind.EstablishAccepted);
        lifecycle.Apply(PortableEnvelopeKind.Ready);
        return lifecycle;
    }

    /// <summary>
    /// A peer that establishes correctly and then misbehaves in exactly one declared way, so the
    /// host's observation is attributable to that one difference.
    /// </summary>
    private static async Task MisleadingPeerAsync(
        IPortableDuplex duplex,
        CancellationToken cancellationToken,
        bool echoCorrelation,
        bool answer = true)
    {
        var limits = PortableLimits.Declared;
        var establish = PortableEnvelopeCodec.Decode(
            await duplex.ReceiveAsync(cancellationToken),
            limits);
        var accepted = new PortableEstablishAcceptedBody(CoolingPortableFixture.Contract, []);
        await duplex.SendAsync(
            PortableEnvelopeCodec.Encode(PortableEnvelope.Control(
                PortableEnvelopeKind.EstablishAccepted,
                establish.ChannelId,
                accepted.Encode())),
            cancellationToken);
        await duplex.SendAsync(
            PortableEnvelopeCodec.Encode(PortableEnvelope.Control(PortableEnvelopeKind.Ready, establish.ChannelId)),
            cancellationToken);

        if (!answer)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return;
        }

        var request = PortableEnvelopeCodec.Decode(await duplex.ReceiveAsync(cancellationToken), limits);
        var body = new PortableOutcomeBody(
            PortableOutcomeStatus.Succeeded,
            CoolingPortableFixture.Result,
            PortableValueCodec.Encode(
                Catalog,
                CoolingPortableFixture.Result,
                PortableRecordValue.Of(
                    ("loop", new PortableTextValue("primary")),
                    ("coolingEnabled", new PortableBooleanValue(true)),
                    ("revision", new PortableIntegerValue(1)),
                    ("providerEffectCount", new PortableIntegerValue(1)))),
            1);
        await duplex.SendAsync(
            PortableEnvelopeCodec.Encode(new PortableEnvelope(
                PortableContractDocument.SupportedContractVersion,
                PortableEnvelopeKind.Outcome,
                request.ChannelId,
                echoCorrelation ? request.RequestId : PortableChannelRequestId.New(),
                request.ExecutionId,
                body.Encode())),
            cancellationToken);
    }

    /// <summary>A peer that never sends anything and honours cancellation while it does so.</summary>
    private sealed class SilentStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Flush() { }
    }

    /// <summary>A provider whose own runtime fails while handling a request.</summary>
    private sealed class CollapsingHandler : IPortableOperationHandler
    {
        public PortableOperationEffect Invoke(
            PortableOperationReference operation,
            PortableValue input,
            IReadOnlyList<PortableResource> resources) =>
            throw new InvalidOperationException("A private runtime failure that must not cross the seam.");
    }
}
