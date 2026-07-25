using System.Buffers.Binary;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>A bounded, length-delimited duplex seam.</summary>
public interface IPortableDuplex : IAsyncDisposable
{
    ValueTask SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken);

    /// <summary>Reads one frame, or reports a process failure. It never returns a partial frame.</summary>
    ValueTask<byte[]> ReceiveAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The portable framing: a 4-byte big-endian length prefix followed by exactly that many bytes of
/// deterministic CBOR.
/// </summary>
/// <remarks>
/// The prefix is checked before the body is read, so an oversized frame is refused on the prefix
/// alone and no partial request effect begins. A stream that ends inside a declared body is an
/// interruption, not a malformed message: the bytes never formed a message to be malformed.
/// </remarks>
public static class PortableFraming
{
    public const int PrefixBytes = 4;

    public static async ValueTask WriteFrameAsync(
        Stream stream,
        ReadOnlyMemory<byte> body,
        PortableLimits limits,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(limits);
        if (body.Length is 0)
        {
            throw PortableFaultException.Malformed("empty-frame", "A frame body carries at least one byte.");
        }

        if (body.Length > limits.MaxFrameBytes)
        {
            throw PortableFaultException.LimitExceeded(
                "frame-bound",
                $"A frame body of {body.Length} bytes exceeds the declared bound of {limits.MaxFrameBytes}.");
        }

        var prefix = new byte[PrefixBytes];
        BinaryPrimitives.WriteUInt32BigEndian(prefix, (uint)body.Length);
        await stream.WriteAsync(prefix, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<byte[]> ReadFrameAsync(
        Stream stream,
        PortableLimits limits,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(limits);
        var prefix = new byte[PrefixBytes];
        if (!await FillAsync(stream, prefix, allowCleanEnd: true, cancellationToken).ConfigureAwait(false))
        {
            throw new PortableProcessFailureException(
                PortableProcessCategory.PeerTerminated,
                PortableFailureDomain.RemoteProvider,
                "The peer closed the seam between frames.");
        }

        var declared = BinaryPrimitives.ReadUInt32BigEndian(prefix);
        if (declared == 0)
        {
            throw PortableFaultException.Malformed("empty-frame", "A length prefix declaring zero bytes is malformed.");
        }

        if (declared > (uint)limits.MaxFrameBytes)
        {
            throw PortableFaultException.LimitExceeded(
                "frame-bound",
                $"A length prefix declaring {declared} bytes exceeds the declared bound of {limits.MaxFrameBytes}.");
        }

        var body = new byte[declared];
        if (!await FillAsync(stream, body, allowCleanEnd: false, cancellationToken).ConfigureAwait(false))
        {
            throw new PortableProcessFailureException(
                PortableProcessCategory.TransportInterrupted,
                PortableFailureDomain.Transport,
                "The stream ended before the declared body length; the partial frame was discarded.");
        }

        return body;
    }

    private static async ValueTask<bool> FillAsync(
        Stream stream,
        Memory<byte> buffer,
        bool allowCleanEnd,
        CancellationToken cancellationToken)
    {
        var filled = 0;
        while (filled < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[filled..], cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return allowCleanEnd && filled == 0 ? false : filled == buffer.Length;
            }

            filled += read;
        }

        return true;
    }
}

/// <summary>A duplex over one inbound and one outbound stream, bounded by the declared timeout.</summary>
public sealed class PortableStreamDuplex(Stream inbound, Stream outbound, PortableLimits limits, bool ownsStreams)
    : IPortableDuplex
{
    public async ValueTask SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken)
    {
        using var timeout = CreateTimeout(cancellationToken);
        try
        {
            await PortableFraming.WriteFrameAsync(outbound, frame, limits, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PortableProcessFailureException(
                PortableProcessCategory.Timeout,
                PortableFailureDomain.Transport,
                "A blocking write exceeded the declared io timeout.");
        }
        catch (Exception exception) when (
            exception is ObjectDisposedException ||
            (exception is IOException and not PortableProcessFailureException))
        {
            throw new PortableProcessFailureException(
                PortableProcessCategory.TransportUnavailable,
                PortableFailureDomain.Transport,
                "The seam is unavailable for writing.");
        }
    }

    public async ValueTask<byte[]> ReceiveAsync(CancellationToken cancellationToken)
    {
        using var timeout = CreateTimeout(cancellationToken);
        try
        {
            return await PortableFraming.ReadFrameAsync(inbound, limits, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PortableProcessFailureException(
                PortableProcessCategory.Timeout,
                PortableFailureDomain.Transport,
                "No frame arrived within the declared io timeout.");
        }
        catch (Exception exception) when (
            exception is ObjectDisposedException ||
            (exception is IOException and not PortableProcessFailureException))
        {
            throw new PortableProcessFailureException(
                PortableProcessCategory.TransportUnavailable,
                PortableFailureDomain.Transport,
                "The seam is unavailable for reading.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!ownsStreams)
        {
            return;
        }

        await inbound.DisposeAsync().ConfigureAwait(false);
        await outbound.DisposeAsync().ConfigureAwait(false);
    }

    private CancellationTokenSource CreateTimeout(CancellationToken cancellationToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(limits.IoTimeout);
        return source;
    }
}
