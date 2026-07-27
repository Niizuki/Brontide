namespace Brontide.Minimal.Interchange.Tests.Portable

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// A stream that fails in one declared way, so the observation is attributable to it.
type FailingStream(fail: unit -> exn) =
    inherit Stream()

    override _.CanRead = true
    override _.CanSeek = false
    override _.CanWrite = true
    override _.Length = raise (NotSupportedException())

    override _.Position
        with get () = raise (NotSupportedException())
        and set (_: int64) = raise (NotSupportedException())

    override _.Flush() = ()
    override _.Read(_: byte array, _: int, _: int) = raise (fail ())
    override _.Write(_: byte array, _: int, _: int) = raise (fail ())
    override _.Seek(_: int64, _: SeekOrigin) = raise (NotSupportedException())
    override _.SetLength(_: int64) = raise (NotSupportedException())
    override _.ReadAsync(_: Memory<byte>, _: CancellationToken) : ValueTask<int> = raise (fail ())
    override _.WriteAsync(_: ReadOnlyMemory<byte>, _: CancellationToken) : ValueTask = raise (fail ())

/// PB6 / CH-23: the transport selects exactly one declared process category, for every way it can
/// fail — including the ways nobody wrote a vector for.
///
/// PB-51 asserted the declared set is complete and unique. That is a statement about an enumeration,
/// not about behaviour: two of its values, `resource-exhausted` and `unknown`, had no path that
/// could produce them, and the failures that should have produced them escaped as runtime types
/// instead. These drive each condition through a real duplex and check the category it selects.
[<TestFixture>]
type PortableProcessCategoryTests() =

    /// Each condition names what the stream raises and the one category it must select.
    ///
    /// They are a list rather than an NUnit case source because the conditions carry functions, and
    /// boxing those through `obj array` fights this stack's nullness analysis for no benefit.
    let conditions =
        [ "allocation failure",
          (fun () -> OutOfMemoryException "allocation refused" :> exn),
          ProcessCategory.ResourceExhausted,
          FailureDomain.Transport
          "a broken pipe",
          (fun () -> IOException "the pipe is broken" :> exn),
          ProcessCategory.TransportUnavailable,
          FailureDomain.Transport
          "a disposed stream",
          (fun () -> ObjectDisposedException "stream" :> exn),
          ProcessCategory.TransportUnavailable,
          FailureDomain.Transport
          // A stream is free to raise anything. Before PB6 this escaped the binding entirely.
          "an unmodelled condition",
          (fun () -> InvalidOperationException "a condition this layer does not model" :> exn),
          ProcessCategory.Unknown,
          FailureDomain.Unknown
          "an unsupported stream operation",
          (fun () -> NotSupportedException "the stream refuses the operation" :> exn),
          ProcessCategory.Unknown,
          FailureDomain.Unknown ]

    let duplex (fail: unit -> exn) =
        PortableStreamDuplex(new FailingStream(fail), new FailingStream(fail), PortableLimits.declared, false)
        :> IPortableDuplex

    [<Test>]
    member _.``receiving selects exactly one declared category``() =
        for name, fail, category, domain in conditions do
            let failure = expectProcessFailure ((duplex fail).Receive().Result)

            assertAll (fun () ->
                Assert.That(failure.Category, Is.EqualTo category, $"{name} selected the wrong category.")
                Assert.That(failure.Domain, Is.EqualTo domain, $"{name} selected the wrong domain.")
                Assert.That(failure.Message, Is.Not.Empty))

    [<Test>]
    member _.``sending selects exactly one declared category``() =
        for name, fail, category, domain in conditions do
            let failure = expectProcessFailure ((duplex fail).Send([| 1uy; 2uy; 3uy |]).Result)

            assertAll (fun () ->
                Assert.That(failure.Category, Is.EqualTo category, $"{name} selected the wrong category.")
                Assert.That(failure.Domain, Is.EqualTo domain, $"{name} selected the wrong domain."))

    /// No transport failure carries a runtime type out of the binding, whatever the stream raised.
    [<Test>]
    member _.``no transport failure names the runtime type that caused it``() =
        for name, fail, _, _ in conditions do
            let raised = fail ()
            let failure = expectProcessFailure ((duplex fail).Receive().Result)

            assertAll (fun () ->
                Assert.That(failure.Message, Does.Not.Contain(raised.GetType().Name), name)
                Assert.That(failure.Message, Does.Not.Contain raised.Message, name)
                Assert.That(failure.Message, Does.Not.Contain "System.", name))

    /// `unknown` retains why narrower attribution was impossible, rather than being a silent
    /// fallback that says nothing.
    [<Test>]
    member _.``unknown states why narrower attribution was impossible``() =
        let seam =
            duplex (fun () -> InvalidOperationException "a condition this layer does not model" :> exn)

        let failure = expectProcessFailure (seam.Receive().Result)

        assertAll (fun () ->
            Assert.That(failure.Category, Is.EqualTo ProcessCategory.Unknown)
            Assert.That(failure.Message, Does.Contain "attribute more narrowly"))

    /// Which declared categories this version can reach, stated rather than left to inference.
    ///
    /// `peer-unavailable` is the one declared category the binding layer cannot produce in version
    /// 0.1, and the honest reason is that this layer never starts a peer: it is handed a duplex that
    /// is already connected. Starting one is the host harness's concern, above the binding.
    /// Recording that here is better than manufacturing a path so the enumeration looks evenly
    /// covered.
    [<Test>]
    member _.``the categories this version cannot reach are named and justified``() =
        let reachable =
            Set.ofList
                [ ProcessCategory.TransportUnavailable
                  ProcessCategory.TransportInterrupted
                  ProcessCategory.Timeout
                  ProcessCategory.PeerTerminated
                  ProcessCategory.ResourceExhausted
                  ProcessCategory.Unknown ]

        let unreachable = Set.difference (Set.ofList ProcessCategory.declared) reachable

        shouldEqual (Set.ofList [ ProcessCategory.PeerUnavailable ]) unreachable
