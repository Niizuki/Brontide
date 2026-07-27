namespace Brontide.Minimal.Binding.Portable

open System
open System.IO
open System.Threading
open System.Threading.Tasks

/// A bounded, length-delimited duplex seam.
///
/// The seam speaks frames rather than envelopes, so nothing about the Channel semantics leaks into
/// the transport and the same transport carries a deliberately malformed frame in a test.
type IPortableDuplex =
    abstract Send: frame: byte array -> Task<PortableResult<unit>>
    /// Reads one frame, or reports a process failure. It never returns a partial frame.
    abstract Receive: unit -> Task<PortableResult<byte array>>
    abstract Close: unit -> unit

/// The portable framing: a 4-byte big-endian length prefix followed by exactly that many bytes of
/// deterministic CBOR.
///
/// The prefix is checked before the body is read, so an oversized frame is refused on the prefix
/// alone and no partial request effect begins. A stream that ends inside a declared body is an
/// interruption, not a malformed message: the bytes never formed a message to be malformed.
[<RequireQualifiedAccess>]
module PortableFraming =

    [<Literal>]
    let PrefixBytes = 4

    let private writePrefix (length: int) =
        [| byte ((length >>> 24) &&& 0xFF)
           byte ((length >>> 16) &&& 0xFF)
           byte ((length >>> 8) &&& 0xFF)
           byte (length &&& 0xFF) |]

    let private readPrefix (prefix: byte array) =
        (uint32 prefix.[0] <<< 24)
        ||| (uint32 prefix.[1] <<< 16)
        ||| (uint32 prefix.[2] <<< 8)
        ||| uint32 prefix.[3]

    /// Reads until the buffer is full or the stream ends, reporting how much arrived.
    let private fill (stream: Stream) (buffer: byte array) (token: CancellationToken) =
        task {
            let mutable filled = 0
            let mutable ended = false

            while not ended && filled < buffer.Length do
                let! read = stream.ReadAsync(Memory<byte>(buffer, filled, buffer.Length - filled), token)

                if read = 0 then ended <- true else filled <- filled + read

            return filled
        }

    let writeFrame (stream: Stream) (body: byte array) limits (token: CancellationToken) =
        task {
            if body.Length = 0 then
                return malformed "empty-frame" "A frame body carries at least one byte."
            elif body.Length > limits.MaxFrameBytes then
                return
                    limitExceeded
                        "frame-bound"
                        $"A frame body of {body.Length} bytes exceeds the declared bound of {limits.MaxFrameBytes}."
            else
                do! stream.WriteAsync(ReadOnlyMemory<byte>(writePrefix body.Length), token).AsTask()
                do! stream.WriteAsync(ReadOnlyMemory<byte> body, token).AsTask()
                do! stream.FlushAsync token
                return Ok()
        }

    let readFrame (stream: Stream) limits (token: CancellationToken) =
        task {
            let prefix = Array.zeroCreate<byte> PrefixBytes
            let! prefixBytes = fill stream prefix token

            if prefixBytes = 0 then
                // Nothing at all arrived, so the peer ended between frames rather than inside one.
                return
                    lost ProcessCategory.PeerTerminated FailureDomain.RemoteProvider "The peer closed the seam between frames."
            elif prefixBytes < PrefixBytes then
                return
                    lost
                        ProcessCategory.TransportInterrupted
                        FailureDomain.Transport
                        "The stream ended inside a length prefix; the partial frame was discarded."
            else
                let declared = readPrefix prefix

                if declared = 0u then
                    return malformed "empty-frame" "A length prefix declaring zero bytes is malformed."
                elif declared > uint32 limits.MaxFrameBytes then
                    return
                        limitExceeded
                            "frame-bound"
                            $"A length prefix declaring {declared} bytes exceeds the declared bound of {limits.MaxFrameBytes}."
                else
                    let body = Array.zeroCreate<byte> (int declared)
                    let! filled = fill stream body token

                    if filled < body.Length then
                        return
                            lost
                                ProcessCategory.TransportInterrupted
                                FailureDomain.Transport
                                "The stream ended before the declared body length; the partial frame was discarded."
                    else
                        return Ok body
        }

/// A duplex over one inbound and one outbound stream, bounded by the declared io timeout.
type PortableStreamDuplex(inbound: Stream, outbound: Stream, limits: PortableLimits, ownsStreams: bool) =

    let guard (work: CancellationToken -> Task<PortableResult<'T>>) =
        task {
            use timeout = new CancellationTokenSource(PortableLimits.ioTimeout limits)

            // The classification below is total on purpose. It previously named only the
            // cancellation, disposed-stream, and I/O cases, so any other failure a stream can raise
            // travelled out of the binding as a runtime type — the one thing C4 says never crosses
            // the seam. Being total is also what gives 'resource-exhausted' and 'unknown' a way to
            // occur at all: both are declared by the Channel taxonomy, and neither had a path here
            // before.
            //
            // Catching an allocation failure is ordinarily poor practice. At this boundary it is the
            // contract: the alternative is not a healthier process but a foreign exception in the
            // caller's hands, and the taxonomy has a value for exactly this condition.
            try
                return! work timeout.Token
            with
            | :? OperationCanceledException ->
                return
                    lost ProcessCategory.Timeout FailureDomain.Transport "The seam exceeded the declared io timeout."
            | :? OutOfMemoryException ->
                return
                    lost ProcessCategory.ResourceExhausted FailureDomain.Transport "The seam ran out of memory."
            | :? ObjectDisposedException ->
                return lost ProcessCategory.TransportUnavailable FailureDomain.Transport "The seam is unavailable."
            | :? IOException ->
                return lost ProcessCategory.TransportUnavailable FailureDomain.Transport "The seam is unavailable."
            | _ ->
                // 'unknown' retains why narrower attribution was impossible, which is the whole of
                // what the Channel vector asks of it. The runtime type is deliberately not in the text.
                return
                    lost
                        ProcessCategory.Unknown
                        FailureDomain.Unknown
                        "The seam failed and reported no condition this endpoint can attribute more narrowly."
        }

    interface IPortableDuplex with
        member _.Send frame =
            guard (fun token -> PortableFraming.writeFrame outbound frame limits token)

        member _.Receive() =
            guard (fun token -> PortableFraming.readFrame inbound limits token)

        member _.Close() =
            if ownsStreams then
                inbound.Dispose()
                outbound.Dispose()
