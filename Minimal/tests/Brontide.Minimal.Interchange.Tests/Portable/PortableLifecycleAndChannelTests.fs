namespace Brontide.Minimal.Interchange.Tests.Portable

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// C4 and C8: declared limits, lifecycle states, envelopes, correlation, and the failure taxonomy.
[<TestFixture>]
type PortableLifecycleAndChannelTests() =

    let limits = PortableLimits.declared
    let catalog = catalogOf CoolingFixture.contract

    let coolingResult () =
        PortableRecord.ofFields
            [ "loop", PortableText "primary"
              "coolingEnabled", PortableBoolean true
              "revision", PortableInteger 1L
              "providerEffectCount", PortableInteger 1L ]

    /// A peer that establishes correctly and then answers one request however the test asks.
    let peerAnswering (onRequest: Envelope -> Envelope) (duplex: IPortableDuplex) (_: CancellationToken) =
        task {
            let send envelope =
                task {
                    match EnvelopeCodec.encode envelope with
                    | Ok frame ->
                        let! _ = duplex.Send frame
                        return ()
                    | Error _ -> return ()
                }

            let mutable running = true

            while running do
                let! frame = duplex.Receive()

                match frame |> Result.bind (fun frame -> EnvelopeCodec.decode frame limits) with
                | Error _ -> running <- false
                | Ok envelope ->
                    match envelope.Kind with
                    | EnvelopeKind.Establish ->
                        do!
                            send (
                                Envelope.control
                                    EnvelopeKind.EstablishAccepted
                                    envelope.Channel
                                    (EstablishAcceptedBody.encode
                                        { Contract = CoolingFixture.contract
                                          CompactIdentifiers = [] })
                            )

                        do! send (Envelope.empty EnvelopeKind.Ready envelope.Channel)
                    | EnvelopeKind.Request -> do! send (onRequest envelope)
                    | EnvelopeKind.Terminate -> running <- false
                    | _ -> ()
        }
        :> Task

    let hostOverPeer (peer: IPortableDuplex -> CancellationToken -> Task) =
        let seam = new PortableLocalSeam(limits)
        seam.StartPeer peer

        let host =
            PortableBindingHost
                .Establish(
                    CoolingFixture.contract,
                    PortableProcessConversation(seam.HostDuplex, limits),
                    "minimal-process"
                )
                .Result
            |> expectOk

        host, seam

    let readFrom (bytes: byte array) bounds =
        use stream = new MemoryStream(bytes)
        (PortableFraming.readFrame stream bounds CancellationToken.None).Result

    [<Test>]
    member _.``PB-33 an oversized length prefix is refused before the body is read``() =
        // 65537 bytes declared, one past the bound, with no body behind it at all.
        let prefix = [| 0uy; 1uy; 0uy; 1uy |]
        let fault = expectRefusal (readFrom prefix limits)

        assertAll (fun () ->
            Assert.That(fault.Category, Is.EqualTo ProtocolCategory.LimitExceeded)
            Assert.That(fault.LocalCode, Is.EqualTo "frame-bound"))

    [<Test>]
    member _.``PB-34 nesting beyond the declared depth stops decoding``() =
        let deep =
            List.fold (fun item _ -> CborArray [ item ]) (CborInteger 1L) [ 1 .. limits.MaxNestingDepth + 2 ]

        let bytes = expectOk (PortableCbor.encode deep)
        expectCategory ProtocolCategory.LimitExceeded (PortableCbor.decode bytes limits) |> ignore

    [<Test>]
    member _.``PB-35 a repeated request identity inside the window is refused``() =
        let endpoint =
            PortableProviderEndpoint(CoolingFixture.contract, CoolingHandler(), Realization.FixedDirectCall)

        expectOk (endpoint.Establish(CoolingFixture.contract, "host")) |> ignore
        expectOk (endpoint.SignalReady())
        let request = ChannelRequestId.next ()

        let invoke () =
            endpoint.Request(
                request,
                OperationDesignation.Canonical CoolingFixture.setEnabled,
                CoolingFixture.commandV1,
                CoolingFixture.authorizedCommand "primary" true,
                []
            )

        let first = expectOk (invoke ())
        Assert.That(first.ProviderEffectCount, Is.EqualTo 1L)

        let replayed = expectCategory ProtocolCategory.ReplayDetected (invoke ())
        Assert.That(replayed.LocalCode, Is.EqualTo "replay")

    [<Test>]
    member _.``PB-36 a second concurrent request is refused``() =
        let lifecycle =
            Lifecycle.create true
            |> Lifecycle.apply EnvelopeKind.Establish
            |> Result.bind (Lifecycle.apply EnvelopeKind.EstablishAccepted)
            |> Result.bind (Lifecycle.apply EnvelopeKind.Ready)
            |> Result.bind (Lifecycle.apply EnvelopeKind.Request)
            |> expectOk

        Assert.That(Lifecycle.state lifecycle, Is.EqualTo LifecycleState.Active)
        expectCategory ProtocolCategory.StateViolation (Lifecycle.apply EnvelopeKind.Request lifecycle) |> ignore

    [<Test>]
    member _.``PB-37 a duplicate terminal outcome is refused rather than overwriting the first``() =
        let lifecycle =
            Lifecycle.create true
            |> Lifecycle.apply EnvelopeKind.Establish
            |> Result.bind (Lifecycle.apply EnvelopeKind.EstablishAccepted)
            |> Result.bind (Lifecycle.apply EnvelopeKind.Ready)
            |> Result.bind (Lifecycle.apply EnvelopeKind.Request)
            |> Result.bind (Lifecycle.apply EnvelopeKind.Outcome)
            |> expectOk

        Assert.That(Lifecycle.state lifecycle, Is.EqualTo LifecycleState.Ready)
        expectCategory ProtocolCategory.StateViolation (Lifecycle.apply EnvelopeKind.Outcome lifecycle) |> ignore

    [<Test>]
    member _.``PB-38 withdrawal and termination are legal and close the binding``() =
        let host, seam = processCoolingHost ()
        use _ = seam

        expectOk (host.Withdraw().Result)
        Assert.That(host.State, Is.EqualTo LifecycleState.Withdrawn)

        // No new request is accepted after withdrawal.
        let refused =
            invoke host CoolingFixture.setEnabled CoolingFixture.commandV1 (CoolingFixture.authorizedCommand "primary" true)

        Assert.That(refused.Category, Is.EqualTo(Some ProtocolCategory.StateViolation))

    [<Test>]
    member _.``PB-84 an outcome after withdrawal is refused because no request remains active``() =
        let lifecycle =
            Lifecycle.create true
            |> Lifecycle.apply EnvelopeKind.Establish
            |> Result.bind (Lifecycle.apply EnvelopeKind.EstablishAccepted)
            |> Result.bind (Lifecycle.apply EnvelopeKind.Ready)
            |> Result.bind (Lifecycle.apply EnvelopeKind.Request)
            |> Result.bind (Lifecycle.apply EnvelopeKind.Withdraw)
            |> expectOk

        Assert.That(Lifecycle.state lifecycle, Is.EqualTo LifecycleState.Withdrawn)
        expectCategory ProtocolCategory.StateViolation (Lifecycle.apply EnvelopeKind.Outcome lifecycle) |> ignore

    [<Test>]
    member _.``PB-38 a clean termination follows a withdrawal``() =
        let host, seam = processCoolingHost ()
        use _ = seam

        expectOk (host.Withdraw().Result)
        expectOk (host.Terminate().Result)
        Assert.That(host.State, Is.EqualTo LifecycleState.Terminated)

    [<Test>]
    member _.``PB-39 a second establish on the same binding is refused``() =
        let endpoint =
            PortableProviderEndpoint(CoolingFixture.contract, CoolingHandler(), Realization.FixedDirectCall)

        expectOk (endpoint.Establish(CoolingFixture.contract, "host")) |> ignore

        expectCategory ProtocolCategory.StateViolation (endpoint.Establish(CoolingFixture.contract, "host"))
        |> ignore

    [<Test>]
    member _.``PB-40 no frame within the declared io timeout is a timeout, not a success``() =
        let bounds = { limits with IoTimeoutMilliseconds = 150 }
        use silent = new SeamStream()
        let duplex: IPortableDuplex = PortableStreamDuplex(silent, silent, bounds, false)

        let failure = expectProcessFailure (duplex.Receive().Result)

        assertAll (fun () ->
            Assert.That(failure.Category, Is.EqualTo ProcessCategory.Timeout)
            Assert.That(failure.Domain, Is.EqualTo FailureDomain.Transport))

    [<Test>]
    member _.``PB-41 a stream ending inside a declared body is an interruption``() =
        let partial = Array.append [| 0uy; 0uy; 0uy; 10uy |] [| 1uy; 2uy; 3uy |]
        let failure = expectProcessFailure (readFrom partial limits)

        assertAll (fun () ->
            Assert.That(failure.Category, Is.EqualTo ProcessCategory.TransportInterrupted)
            Assert.That(failure.Domain, Is.EqualTo FailureDomain.Transport))

    [<Test>]
    member _.``PB-42 an outcome echoing every carried identity is accepted``() =
        let host, seam = processCoolingHost ()
        use _ = seam

        let result =
            host
                .Invoke(
                    CoolingFixture.setEnabled,
                    CoolingFixture.commandV1,
                    CoolingFixture.authorizedCommand "primary" true,
                    permitted,
                    [],
                    ChannelExecutionId.next ()
                )
                .Result

        assertAll (fun () ->
            Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.Accept)
            Assert.That(result.ResultClass, Is.EqualTo ResultClass.OutcomeSucceeded)

            // The host-native Execution is a distinct identity space from every Channel identity.
            let correlation = result.Observation.Correlation

            Assert.That(
                HostExecutionId.value correlation.HostNativeExecution,
                Is.Not.EqualTo(ChannelRequestId.value correlation.Request)
            ))

    [<Test>]
    member _.``PB-43 an outcome claiming a request this host never sent is refused``() =
        let mismatching (envelope: Envelope) =
            let value = expectOk (PortableValueCodec.encode catalog CoolingFixture.result (coolingResult ()))

            Envelope.correlated
                EnvelopeKind.Outcome
                envelope.Channel
                (Some(ChannelRequestId.received "rq-never-sent"))
                envelope.Execution
                (OutcomeBody.encode
                    { Status = OutcomeStatus.Succeeded
                      ValueShape = CoolingFixture.result
                      Value = value
                      ProviderEffectCount = 1L })

        let host, seam = hostOverPeer (peerAnswering mismatching)
        use _ = seam

        let result =
            invoke host CoolingFixture.setEnabled CoolingFixture.commandV1 (CoolingFixture.authorizedCommand "primary" true)

        assertAll (fun () ->
            Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.Reject)
            Assert.That(result.Category, Is.EqualTo(Some ProtocolCategory.CorrelationMismatch))
            // The failure is attributed to the endpoint that observed it.
            Assert.That(result.Observation.FailureDomain, Is.EqualTo(Some FailureDomain.LocalEndpoint)))

    [<Test>]
    member _.``PB-44 an outcome without a request identity is refused rather than matched by position``() =
        let value = expectOk (PortableValueCodec.encode catalog CoolingFixture.result (coolingResult ()))

        let item =
            EnvelopeCodec.toItem (
                Envelope.correlated
                    EnvelopeKind.Outcome
                    (ChannelId.next ())
                    (Some(ChannelRequestId.next ()))
                    None
                    (OutcomeBody.encode
                        { Status = OutcomeStatus.Succeeded
                          ValueShape = CoolingFixture.result
                          Value = value
                          ProviderEffectCount = 1L })
            )
            |> withoutEntry "requestId"

        expectCategory ProtocolCategory.MalformedMessage (EnvelopeCodec.ofItem item) |> ignore

    [<Test>]
    member _.``PB-45 an envelope kind outside the declared set is never a no-op``() =
        let item =
            EnvelopeCodec.toItem (Envelope.empty EnvelopeKind.Ready (ChannelId.next ()))
            |> withEntry "kind" (CborText "gossip")

        expectCategory ProtocolCategory.UnsupportedKind (EnvelopeCodec.ofItem item) |> ignore

    [<Test>]
    member _.``PB-46 a request naming an Operation outside the contract is refused``() =
        let host = directCoolingHost ()

        let result =
            invoke host CatalogFixture.find CoolingFixture.commandV1 (CoolingFixture.authorizedCommand "primary" true)

        assertAll (fun () ->
            Assert.That(result.Category, Is.EqualTo(Some ProtocolCategory.UnsupportedOperation))
            Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo(Some 0L)))

    [<Test>]
    member _.``PB-47 a shaped failed Outcome crosses without an exception``() =
        let host, seam = processCoolingHost ()
        use _ = seam

        let refusing =
            CoolingFixture.command "primary" true (Some "semantic") (Some "operator") None

        let result = invoke host CoolingFixture.setEnabled CoolingFixture.commandV1 refusing

        assertAll (fun () ->
            Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.Accept)
            Assert.That(result.ResultClass, Is.EqualTo ResultClass.OutcomeFailed)
            Assert.That(result.Observation.TerminalStatus, Is.EqualTo TerminalStatus.Failed)
            Assert.That(result.Observation.FailureDomain, Is.EqualTo(Some FailureDomain.RemoteProvider))
            // The details are shaped by the declared detail Shape; no result value is present.
            Assert.That(result.Value |> Option.bind (PortableRecord.tryField "code"), Is.EqualTo(Some(PortableText "semantic"))))

    [<Test>]
    member _.``PB-48 two different local codes report the same portable category``() =
        let unmet =
            expectRefusal (
                PortableNegotiation.negotiate
                    CoolingFixture.contract
                    (CoolingFixture.withoutProfileProvision ())
                    Realization.FixedDirectCall
                    "host"
                    "provider"
                    "fixed"
            )

        let opposed =
            expectRefusal (
                PortableNegotiation.negotiate
                    CoolingFixture.contract
                    (CoolingFixture.withStreamingProvision ())
                    Realization.FixedDirectCall
                    "host"
                    "provider"
                    "fixed"
            )

        assertAll (fun () ->
            Assert.That(unmet.LocalCode, Is.Not.EqualTo opposed.LocalCode)
            Assert.That(unmet.Category, Is.EqualTo ProtocolCategory.UnsupportedContract)
            Assert.That(opposed.Category, Is.EqualTo ProtocolCategory.UnsupportedContract))

    [<Test>]
    member _.``PB-49 a provider runtime failure crosses only as a portable category``() =
        let raising =
            { new IPortableOperationHandler with
                member _.Invoke(_, _, _) = raise (InvalidOperationException "provider internals") }

        let endpoint =
            PortableProviderEndpoint(CoolingFixture.contract, raising, Realization.FixedDirectCall)

        expectOk (endpoint.Establish(CoolingFixture.contract, "host")) |> ignore
        expectOk (endpoint.SignalReady())

        let fault =
            expectCategory
                ProtocolCategory.InternalProtocolFailure
                (endpoint.Request(
                    ChannelRequestId.next (),
                    OperationDesignation.Canonical CoolingFixture.setEnabled,
                    CoolingFixture.commandV1,
                    CoolingFixture.authorizedCommand "primary" true,
                    []
                ))

        assertAll (fun () ->
            Assert.That(fault.Message, Does.Not.Contain "InvalidOperationException")
            Assert.That(fault.Message, Does.Not.Contain "provider internals"))

    [<Test>]
    member _.``PB-50 a peer that terminates is observed, never fabricated into a success``() =
        let host, seam = processCoolingHost ()
        use _ = seam

        seam.CloseProviderOutput()

        let result =
            invoke host CoolingFixture.setEnabled CoolingFixture.commandV1 (CoolingFixture.authorizedCommand "primary" true)

        assertAll (fun () ->
            Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.None)
            Assert.That(result.ResultClass, Is.EqualTo ResultClass.ProcessFailure)
            Assert.That(result.ProcessCategory, Is.EqualTo(Some ProcessCategory.PeerTerminated))
            Assert.That(result.Observation.FailureDomain, Is.EqualTo(Some FailureDomain.RemoteProvider))
            Assert.That(result.Observation.TerminalStatus, Is.EqualTo TerminalStatus.ProcessFailure))

    [<Test>]
    member _.``PB-51 the process-failure categories are exactly the neutral set``() =
        use document = readNeutral [ "schemas"; "channel-envelope.json" ]

        let declared =
            document.RootElement.GetProperty("processFailureCategories").EnumerateArray()
            |> Seq.map (fun entry -> entry.GetProperty("id").GetString() |> str)
            |> Set.ofSeq

        let implemented =
            [ ProcessCategory.TransportUnavailable
              ProcessCategory.TransportInterrupted
              ProcessCategory.Timeout
              ProcessCategory.PeerTerminated
              ProcessCategory.PeerUnavailable
              ProcessCategory.ResourceExhausted
              ProcessCategory.Unknown ]
            |> List.map ProcessCategory.token
            |> Set.ofList

        shouldEqual declared implemented

    [<Test>]
    member _.``PB-52 a failure domain is recorded relative to the observer``() =
        use document = readNeutral [ "schemas"; "channel-envelope.json" ]

        let declared =
            document.RootElement.GetProperty("failureDomains").EnumerateArray()
            |> Seq.map (fun entry -> entry.GetProperty("id").GetString() |> str)
            |> Set.ofSeq

        shouldEqual declared (FailureDomain.all |> List.map FailureDomain.token |> Set.ofList)

        // The same category is local when this endpoint decided it and remote when a peer reported
        // it back; neither value claims global topology.
        let local = PortableFault.create ProtocolCategory.UnsupportedContract "requirement-unmet" "local"
        let remote = PortableFault.fromRemote ProtocolCategory.UnsupportedContract "peer-code" "remote"

        assertAll (fun () ->
            Assert.That(local.Domain, Is.EqualTo FailureDomain.LocalEndpoint)
            Assert.That(remote.Domain, Is.EqualTo FailureDomain.RemoteEndpoint)
            Assert.That(local.Category, Is.EqualTo remote.Category))
