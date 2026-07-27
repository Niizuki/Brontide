using System.Collections.Immutable;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// PB6 / CH-23: the transport selects exactly one declared process category, for every way it can
/// fail — including the ways nobody wrote a vector for.
/// </summary>
/// <remarks>
/// PB-51 asserted the declared set is complete and unique. That is a statement about an enumeration,
/// not about behaviour: two of its values, <c>resource-exhausted</c> and <c>unknown</c>, had no path
/// that could produce them, and the failures that should have produced them escaped as runtime types
/// instead. These drive each condition through a real duplex and check the category it selects.
/// </remarks>
public sealed class PortableProcessCategoryTests
{
    /// <summary>A stream that fails in one declared way, so the observation is attributable to it.</summary>
    private sealed class FailingStream(Func<Exception> fail) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
            throw fail();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            throw fail();

        public override int Read(byte[] buffer, int offset, int count) => throw fail();
        public override void Write(byte[] buffer, int offset, int count) => throw fail();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Flush() { }
    }

    private static PortableStreamDuplex Duplex(Func<Exception> fail) =>
        new(new FailingStream(fail), new FailingStream(fail), PortableLimits.Declared, ownsStreams: false);

    private static IEnumerable<TestCaseData> Conditions()
    {
        yield return new TestCaseData(
            (Func<Exception>)(() => new OutOfMemoryException("allocation refused")),
            PortableProcessCategory.ResourceExhausted,
            PortableFailureDomain.Transport).SetName("allocation failure is resource-exhausted");

        yield return new TestCaseData(
            (Func<Exception>)(() => new IOException("the pipe is broken")),
            PortableProcessCategory.TransportUnavailable,
            PortableFailureDomain.Transport).SetName("a broken pipe is transport-unavailable");

        yield return new TestCaseData(
            (Func<Exception>)(() => new ObjectDisposedException("stream")),
            PortableProcessCategory.TransportUnavailable,
            PortableFailureDomain.Transport).SetName("a disposed stream is transport-unavailable");

        // A stream is free to raise anything. Before PB6 this escaped the binding entirely.
        yield return new TestCaseData(
            (Func<Exception>)(() => new InvalidOperationException("a condition this layer does not model")),
            PortableProcessCategory.Unknown,
            PortableFailureDomain.Unknown).SetName("an unmodelled condition is unknown");

        yield return new TestCaseData(
            (Func<Exception>)(() => new NotSupportedException("the stream refuses the operation")),
            PortableProcessCategory.Unknown,
            PortableFailureDomain.Unknown).SetName("an unsupported stream operation is unknown");
    }

    [TestCaseSource(nameof(Conditions))]
    public void Receiving_selects_exactly_one_declared_category(
        Func<Exception> fail,
        PortableProcessCategory category,
        PortableFailureDomain domain)
    {
        var duplex = Duplex(fail);

        var failure = Assert.ThrowsAsync<PortableProcessFailureException>(
            async () => await duplex.ReceiveAsync(CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(failure!.Category, Is.EqualTo(category));
            Assert.That(failure.Domain, Is.EqualTo(domain));
            Assert.That(failure.Message, Is.Not.Empty);
        });
    }

    [TestCaseSource(nameof(Conditions))]
    public void Sending_selects_exactly_one_declared_category(
        Func<Exception> fail,
        PortableProcessCategory category,
        PortableFailureDomain domain)
    {
        var duplex = Duplex(fail);

        var failure = Assert.ThrowsAsync<PortableProcessFailureException>(
            async () => await duplex.SendAsync(new byte[] { 1, 2, 3 }, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(failure!.Category, Is.EqualTo(category));
            Assert.That(failure.Domain, Is.EqualTo(domain));
        });
    }

    /// <summary>
    /// No transport failure carries a runtime type out of the binding, whatever the stream raised.
    /// </summary>
    [TestCaseSource(nameof(Conditions))]
    public void No_transport_failure_names_the_runtime_type_that_caused_it(
        Func<Exception> fail,
        PortableProcessCategory category,
        PortableFailureDomain domain)
    {
        _ = category;
        _ = domain;
        var raised = fail();
        var duplex = Duplex(fail);

        var failure = Assert.ThrowsAsync<PortableProcessFailureException>(
            async () => await duplex.ReceiveAsync(CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(failure!.Message, Does.Not.Contain(raised.GetType().Name));
            Assert.That(failure.Message, Does.Not.Contain(raised.Message));
            Assert.That(failure.Message, Does.Not.Contain("System."));
        });
    }

    /// <summary>
    /// <c>unknown</c> retains why narrower attribution was impossible, rather than being a silent
    /// fallback that says nothing.
    /// </summary>
    [Test]
    public void Unknown_states_why_narrower_attribution_was_impossible()
    {
        var duplex = Duplex(() => new InvalidOperationException("a condition this layer does not model"));

        var failure = Assert.ThrowsAsync<PortableProcessFailureException>(
            async () => await duplex.ReceiveAsync(CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(failure!.Category, Is.EqualTo(PortableProcessCategory.Unknown));
            Assert.That(failure.Message, Does.Contain("attribute more narrowly"));
        });
    }

    /// <summary>
    /// Which declared categories this version can reach, stated rather than left to inference.
    /// </summary>
    /// <remarks>
    /// <c>peer-unavailable</c> is the one declared category the binding layer cannot produce in
    /// version 0.1, and the honest reason is that this layer never starts a peer: it is handed a
    /// duplex that is already connected. Starting one is the host harness's concern, above the
    /// binding. Recording that here is better than manufacturing a path so the enumeration looks
    /// evenly covered.
    /// </remarks>
    [Test]
    public void The_categories_this_version_cannot_reach_are_named_and_justified()
    {
        var reachable = ImmutableHashSet.Create(
            PortableProcessCategory.TransportUnavailable,
            PortableProcessCategory.TransportInterrupted,
            PortableProcessCategory.Timeout,
            PortableProcessCategory.PeerTerminated,
            PortableProcessCategory.ResourceExhausted,
            PortableProcessCategory.Unknown);

        var unreachable = Enum.GetValues<PortableProcessCategory>()
            .Where(category => !reachable.Contains(category))
            .ToImmutableArray();

        Assert.That(
            unreachable,
            Is.EquivalentTo(new[] { PortableProcessCategory.PeerUnavailable }),
            "The set of categories this version cannot reach changed; the reason above needs revisiting.");
    }
}
