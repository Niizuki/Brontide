using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Channels;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// Shared scaffolding for the portable-binding vectors: both realizations, the neutral artifacts,
/// and the value-description reader that turns a golden encoding's description into a shaped value.
/// </summary>
internal static class PortableTestHarness
{
    public static string NeutralRoot { get; } = Path.Combine(AppContext.BaseDirectory, "binding", "portable");

    public static JsonDocument ReadNeutral(params string[] relative)
    {
        var path = Path.Combine(new[] { NeutralRoot }.Concat(relative).ToArray());
        Assert.That(File.Exists(path), Is.True, $"The neutral artifact '{path}' was not copied to the test output.");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    /// <summary>Every vector id declared by the neutral layer, across all vector files.</summary>
    public static ImmutableArray<string> NeutralVectorIds()
    {
        var ids = ImmutableArray.CreateBuilder<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(NeutralRoot, "vectors"), "*.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            if (!document.RootElement.TryGetProperty("vectors", out var vectors))
            {
                continue;
            }

            foreach (var vector in vectors.EnumerateArray())
            {
                ids.Add(vector.GetProperty("id").GetString()!);
            }
        }

        return ids.ToImmutable();
    }

    public static PortableProviderEndpoint CoolingEndpoint(
        PortableRealization realization,
        IPortableOperationHandler? handler = null,
        PortableContractDocument? contract = null) =>
        new(
            contract ?? CoolingPortableFixture.Contract,
            handler ?? new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            realization);

    /// <summary>The fixed direct-call realization: no wire, no encoding, no copy.</summary>
    public static ValueTask<PortableBindingHost> DirectHostAsync(
        PortableContractDocument? contract = null,
        IPortableOperationHandler? handler = null)
    {
        var document = contract ?? CoolingPortableFixture.Contract;
        var endpoint = new PortableProviderEndpoint(
            document,
            handler ?? new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            PortableRealization.FixedDirectCall);
        return PortableBindingHost.EstablishAsync(document, new PortableDirectConversation(endpoint), "reference-direct");
    }

    /// <summary>
    /// The negotiated process realization over a local duplex seam: real length-delimited framing
    /// and a real deterministic-CBOR encode and decode on both sides.
    /// </summary>
    public static async Task<(PortableBindingHost Host, PortableLocalSeam Seam)> ProcessHostAsync(
        PortableContractDocument? contract = null,
        IPortableOperationHandler? handler = null)
    {
        var document = contract ?? CoolingPortableFixture.Contract;
        var seam = PortableLocalSeam.Create(document.Limits);
        var endpoint = new PortableProviderEndpoint(
            document,
            handler ?? new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            PortableRealization.NegotiatedProcess);
        seam.StartProvider(endpoint, document.Limits);
        var host = await PortableBindingHost.EstablishAsync(
            document,
            new PortableProcessConversation(seam.HostDuplex, document.Limits),
            "reference-process");
        return (host, seam);
    }

    public static PortableConstraint Permitted() =>
        PortableConstraint.AllOf(
            PortableConstraint.Atom(PortableTruth.Satisfied),
            PortableConstraint.Atom(PortableTruth.Satisfied));

    public static PortableCopiedBlobResource Blob(string name = "profile", string content = "cooling-profile") =>
        PortableCopiedBlobResource.Create(name, System.Text.Encoding.UTF8.GetBytes(content));

    public static string Hex(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(bytes);

    public static byte[] FromHex(string hex) => Convert.FromHexString(hex);

    /// <summary>
    /// Reads a golden encoding's explicit value description into a shaped value. The description
    /// carries kind discriminators; the wire form does not, which is the point of the comparison.
    /// </summary>
    public static PortableValue ReadDescribedValue(JsonElement element)
    {
        var kind = element.GetProperty("kind").GetString();
        switch (kind)
        {
            case "unit":
                return PortableUnitValue.Instance;
            case "text":
                return new PortableTextValue(element.GetProperty("value").GetString()!);
            case "boolean":
                return new PortableBooleanValue(element.GetProperty("value").GetBoolean());
            case "integer":
                return new PortableIntegerValue(element.GetProperty("value").GetInt64());
            case "decimal":
                return new PortableDecimalValue(
                    element.GetProperty("exponent").GetInt32(),
                    element.GetProperty("mantissa").GetInt64());
            case "bytes":
                return new PortableBytesValue(FromHex(element.GetProperty("hex").GetString()!));
            case "sequence":
                return new PortableSequenceValue(
                    [.. element.GetProperty("items").EnumerateArray().Select(ReadDescribedValue)]);
            case "choice":
                return new PortableChoiceValue(
                    element.GetProperty("case").GetString()!,
                    ReadDescribedValue(element.GetProperty("value")));
            case "record":
            {
                var record = PortableRecordValue.Empty;
                foreach (var field in element.GetProperty("fields").EnumerateObject())
                {
                    record = record.WithField(field.Name, ReadDescribedValue(field.Value));
                }

                foreach (var fragment in element.GetProperty("fragments").EnumerateObject())
                {
                    var reference = PortableFragmentReference.ParseText(fragment.Name);
                    record = record.WithFragment(
                        reference,
                        [.. fragment.Value.EnumerateObject()
                            .Select(field => (field.Name, ReadDescribedValue(field.Value)))]);
                }

                return record;
            }
            default:
                throw new InvalidOperationException($"The golden value description uses unknown kind '{kind}'.");
        }
    }

    public static PortableShapeReference ReadShapeReference(JsonElement element) =>
        PortableShapeReference.Parse(
            element.GetProperty("name").GetString()!,
            element.GetProperty("version").GetInt32());
}

/// <summary>Small edits over decoded control items, so an adversarial frame can be built exactly.</summary>
internal static class CborTestExtensions
{
    public static CborMap With(this CborMap map, string key, CborItem value) =>
        new([.. map.Entries.Where(entry => entry.Key != key).Append(new CborMapEntry(key, value))]);

    public static CborArray Array(this CborMap map, string key) =>
        (CborArray)map.Entries.Single(entry => entry.Key == key).Value;

    public static CborArray Replace(this CborArray array, int index, CborItem value) =>
        new(array.Items.SetItem(index, value));
}

/// <summary>
/// A duplex seam between a host and a provider endpoint inside one test process.
/// </summary>
/// <remarks>
/// It is a real pipe pair with real framing, so the process realization's encoding, bounds, and
/// copy accounting are exercised. It is not process isolation: the cross-process evidence lives in
/// the gated tests that launch the provider executable.
/// </remarks>
internal sealed class PortableLocalSeam : IAsyncDisposable
{
    private readonly SeamStream _hostToProvider;
    private readonly SeamStream _providerToHost;
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _provider;

    private PortableLocalSeam(SeamStream hostToProvider, SeamStream providerToHost, PortableLimits limits)
    {
        _hostToProvider = hostToProvider;
        _providerToHost = providerToHost;
        HostDuplex = new PortableStreamDuplex(providerToHost, hostToProvider, limits, ownsStreams: false);
        ProviderDuplex = new PortableStreamDuplex(hostToProvider, providerToHost, limits, ownsStreams: false);
    }

    public IPortableDuplex HostDuplex { get; }

    public IPortableDuplex ProviderDuplex { get; }

    public static PortableLocalSeam Create(PortableLimits limits) =>
        new(new SeamStream(), new SeamStream(), limits);

    public void StartProvider(PortableProviderEndpoint endpoint, PortableLimits limits) =>
        _provider = Task.Run(
            () => PortableProviderProcessLoop.RunAsync(ProviderDuplex, endpoint, limits, _cancellation.Token),
            CancellationToken.None);

    /// <summary>Runs a deliberately misbehaving peer instead of the conforming endpoint.</summary>
    public void StartPeer(Func<IPortableDuplex, CancellationToken, Task> peer) =>
        _provider = Task.Run(() => peer(ProviderDuplex, _cancellation.Token), CancellationToken.None);

    /// <summary>
    /// Ends the provider's outbound direction, which the host observes as a clean end between
    /// frames rather than as an interrupted one.
    /// </summary>
    public void CloseProviderOutput() => _providerToHost.CompleteWriting();

    public async ValueTask DisposeAsync()
    {
        await _cancellation.CancelAsync();
        _hostToProvider.CompleteWriting();
        _providerToHost.CompleteWriting();

        try
        {
            if (_provider is not null)
            {
                await _provider.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }
        catch (Exception)
        {
            // Teardown: a peer failing as its seam disappears is the expected outcome, and the test
            // has already asserted whatever it came to assert.
        }

        _cancellation.Dispose();
    }

    /// <summary>
    /// One direction of the seam: a byte stream whose reader blocks until bytes arrive, observes
    /// cancellation, and sees a clean end once the writer completes.
    /// </summary>
    /// <remarks>
    /// An anonymous pipe would be closer to the real transport, but its reads neither observe
    /// cancellation nor separate "writer closed" from "handle disposed" on Windows. Those are
    /// exactly the two distinctions these vectors turn on, so the seam models them explicitly.
    /// </remarks>
    private sealed class SeamStream : Stream
    {
        private readonly Channel<byte[]> _segments = Channel.CreateUnbounded<byte[]>();
        private byte[] _current = [];
        private int _offset;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public void CompleteWriting() => _segments.Writer.TryComplete();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            while (_offset == _current.Length)
            {
                if (!await _segments.Reader.WaitToReadAsync(cancellationToken))
                {
                    return 0;
                }

                if (_segments.Reader.TryRead(out var segment))
                {
                    _current = segment;
                    _offset = 0;
                }
            }

            var count = Math.Min(buffer.Length, _current.Length - _offset);
            _current.AsSpan(_offset, count).CopyTo(buffer.Span);
            _offset += count;
            return count;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_segments.Writer.TryWrite(buffer.ToArray()))
            {
                throw new IOException("The seam is closed for writing.");
            }

            return ValueTask.CompletedTask;
        }

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Flush() { }
    }
}
