namespace Brontide.Minimal.Binding.Portable

open System.Threading.Tasks

/// What the host received back from one request.
[<StructuralEquality; NoComparison>]
type OutcomeReceipt =
    { Status: OutcomeStatus
      ValueShape: PortableShapeRef
      Value: PortableValue
      ProviderEffectCount: int64 }

/// One realization of the seam between a host and a provider endpoint.
///
/// The interface carries shaped values rather than bytes so that the fixed direct-call realization
/// is a real direct call: it never encodes, so its copy accounting is genuinely zero.
type IPortableProviderConversation =
    abstract Realization: Realization

    abstract Establish:
        required: ContractDocument * hostEndpoint: string * channel: ChannelId -> Task<PortableResult<ContractDocument>>

    abstract AwaitReady: channel: ChannelId -> Task<PortableResult<unit>>

    abstract Request:
        plan: BindingPlan *
        channel: ChannelId *
        request: ChannelRequestId *
        execution: ChannelExecutionId option *
        designation: OperationDesignation *
        inputShape: PortableShapeRef *
        input: PortableValue *
        resources: PortableResource list ->
            Task<PortableResult<OutcomeReceipt>>

    abstract Withdraw: channel: ChannelId -> Task<PortableResult<unit>>
    abstract Terminate: channel: ChannelId -> Task<PortableResult<unit>>
    abstract Close: unit -> unit

/// The fixed direct-call realization: no wire, no encoding, no copy.
///
/// A refusal the provider endpoint decided is reported the way the process realization reports the
/// same decision. The failure domain names which endpoint decided, relative to the observer, and the
/// provider endpoint is the host's peer in both realizations; what the realization changes is the
/// distance between them, not who decided. A domain that tracked the distance would turn an
/// observer-relative fact into a transport fact, and one vector would report two domains.
type PortableDirectConversation(endpoint: PortableProviderEndpoint) =

    interface IPortableProviderConversation with
        member _.Realization = Realization.FixedDirectCall

        member _.Establish(required, hostEndpoint, _) =
            endpoint.Establish(required, hostEndpoint)
            |> Result.map (fun accepted -> accepted.Contract)
            |> PortableFault.asPeerDecision
            |> Task.FromResult

        member _.AwaitReady _ =
            endpoint.SignalReady() |> PortableFault.asPeerDecision |> Task.FromResult

        member _.Request(_, _, request, _, designation, inputShape, input, resources) =
            endpoint.Request(request, designation, inputShape, input, resources)
            |> Result.map (fun outcome ->
                { Status = outcome.Status
                  ValueShape = outcome.ValueShape
                  Value = outcome.Value
                  ProviderEffectCount = outcome.ProviderEffectCount })
            |> PortableFault.asPeerDecision
            |> Task.FromResult

        member _.Withdraw _ = endpoint.Withdraw() |> Task.FromResult

        member _.Terminate _ = endpoint.Terminate() |> Task.FromResult

        member _.Close() = ()

/// The negotiated process realization over a bounded, length-delimited duplex.
type PortableProcessConversation(duplex: IPortableDuplex, limits: PortableLimits) =

    let send envelope =
        task {
            match EnvelopeCodec.encode envelope with
            | Error error -> return Error error
            | Ok frame -> return! duplex.Send frame
        }

    let receive () =
        task {
            let! frame = duplex.Receive()
            return frame |> Result.bind (fun frame -> EnvelopeCodec.decode frame limits)
        }

    /// Turns a peer's protocol error back into the same portable category on this side, so a local
    /// code never becomes the semantics.
    let requireKind (envelope: Envelope) expected =
        if envelope.Kind = expected then
            Ok envelope
        elif envelope.Kind = EnvelopeKind.ProtocolError then
            ProtocolErrorBody.decode envelope.Body
            |> Result.bind (fun error ->
                Error(Refused(PortableFault.fromRemote error.Category error.LocalCode "The peer rejected the interaction.")))
        else
            stateViolation
                "unexpected-kind"
                $"A '{EnvelopeKind.token envelope.Kind}' frame arrived where '{EnvelopeKind.token expected}' was legal."

    interface IPortableProviderConversation with
        member _.Realization = Realization.NegotiatedProcess

        member _.Establish(required, _, channel) =
            task {
                let! sent = send (Envelope.control EnvelopeKind.Establish channel (ContractCodec.encode required))

                match sent with
                | Error error -> return Error error
                | Ok() ->
                    let! received = receive ()

                    return
                        received
                        |> Result.bind (fun envelope -> requireKind envelope EnvelopeKind.EstablishAccepted)
                        |> Result.bind (fun envelope -> EstablishAcceptedBody.decode envelope.Body)
                        |> Result.map (fun body -> body.Contract)
            }

        member _.AwaitReady _ =
            task {
                let! received = receive ()

                return
                    received
                    |> Result.bind (fun envelope -> requireKind envelope EnvelopeKind.Ready)
                    |> Result.map ignore
            }

        member _.Request(plan, channel, request, execution, designation, inputShape, input, resources) =
            task {
                let catalog = BindingPlan.catalog plan

                let prepared =
                    portable {
                        let! encodedInput = PortableValueCodec.encode catalog inputShape input

                        let body =
                            { Operation = designation
                              InputShape = inputShape
                              Input = encodedInput
                              Resources = resources |> List.map ResourceCodec.encode }

                        return
                            Envelope.correlated
                                EnvelopeKind.Request
                                channel
                                (Some request)
                                execution
                                (RequestBody.encode body)
                    }

                match prepared with
                | Error error -> return Error error
                | Ok envelope ->
                    let! sent = send envelope

                    match sent with
                    | Error error -> return Error error
                    | Ok() ->
                        let! received = receive ()

                        return
                            portable {
                                let! envelope = received
                                let! envelope = requireKind envelope EnvelopeKind.Outcome

                                do!
                                    ensure
                                        (envelope.Request = Some request
                                         && (execution.IsNone || envelope.Execution = execution))
                                        (fun () ->
                                            refuse
                                                ProtocolCategory.CorrelationMismatch
                                                "correlation-mismatch"
                                                "The claimed Outcome does not correlate with the outstanding request.")

                                let! outcome = OutcomeBody.decode envelope.Body

                                // The Operation's declared Fragments govern the decode, exactly as
                                // they govern the direct realization's validation. Decoding with an
                                // empty set would refuse a declared Fragment on the wire that a
                                // direct call accepts.
                                let! declaredFragments =
                                    match designation with
                                    | OperationDesignation.Canonical reference ->
                                        BindingPlan.operation reference plan
                                        |> Result.map (fun declaration -> declaration.RequiredFragments)
                                    | OperationDesignation.Compact _ -> Ok []

                                let! value =
                                    PortableValueCodec.decode catalog outcome.ValueShape declaredFragments outcome.Value

                                let receipt: OutcomeReceipt =
                                    { Status = outcome.Status
                                      ValueShape = outcome.ValueShape
                                      Value = value
                                      ProviderEffectCount = outcome.ProviderEffectCount }

                                return receipt
                            }
            }

        member _.Withdraw channel = send (Envelope.empty EnvelopeKind.Withdraw channel)

        member _.Terminate channel = send (Envelope.empty EnvelopeKind.Terminate channel)

        member _.Close() = duplex.Close()

/// The provider side of the negotiated process realization: it reads frames, applies the endpoint's
/// decisions, and answers with exactly one envelope per inbound frame.
[<RequireQualifiedAccess>]
module PortableProviderProcessLoop =

    let private send (duplex: IPortableDuplex) envelope =
        task {
            match EnvelopeCodec.encode envelope with
            | Error error -> return Error error
            | Ok frame -> return! duplex.Send frame
        }

    let private handleRequest (endpoint: PortableProviderEndpoint) (envelope: Envelope) =
        portable {
            let! body = RequestBody.decode envelope.Body

            let! plan =
                match endpoint.Plan with
                | Some plan -> Ok plan
                | None -> stateViolation "unestablished" "A request arrived before any contract was established."

            // The authority scan runs before the value is given a Shape, so authority-bearing
            // content is refused as an authority presentation rather than as an undeclared field.
            if BindingPlan.trustBoundaryCrossed plan then
                do! PortableAuthorityVocabulary.requireNoCapabilityContent body.Input

            let! declaration = endpoint.ResolveOperation body.Operation

            let! input =
                PortableValueCodec.decode
                    (BindingPlan.catalog plan)
                    body.InputShape
                    declaration.RequiredFragments
                    body.Input

            let! resources =
                body.Resources
                |> traverse (fun resource ->
                    ResourceCodec.decode
                        resource
                        (BindingPlan.resourceFlavors plan)
                        (BindingPlan.acceptedResourceHandles plan)
                        (BindingPlan.limits plan))

            let! request =
                match envelope.Request with
                | Some request -> Ok request
                | None -> malformed "correlation-absent" "A request envelope carries a request identity."

            let! outcome = endpoint.Request(request, body.Operation, body.InputShape, input, resources)

            let! encoded =
                PortableValueCodec.encode (BindingPlan.catalog plan) outcome.ValueShape outcome.Value

            let body: OutcomeBody =
                { Status = outcome.Status
                  ValueShape = outcome.ValueShape
                  Value = encoded
                  ProviderEffectCount = outcome.ProviderEffectCount }

            return body
        }

    /// Reports a refusal to the peer as one protocol-error frame, then ends the binding. Process
    /// loss is never reported this way, because the seam that would carry it is what was lost.
    let private report (duplex: IPortableDuplex) (endpoint: PortableProviderEndpoint) channel request (fault: PortableFault) =
        task {
            endpoint.Fail()

            let body =
                { Category = fault.Category
                  LocalCode = fault.LocalCode
                  FailureDomain = FailureDomain.RemoteEndpoint }

            let! _ =
                send
                    duplex
                    (Envelope.correlated EnvelopeKind.ProtocolError channel request None (ProtocolErrorBody.encode body))

            return ()
        }

    let run (duplex: IPortableDuplex) (endpoint: PortableProviderEndpoint) (limits: PortableLimits) =
        task {
            let mutable running = true

            while running do
                let! frame = duplex.Receive()

                match frame |> Result.bind (fun frame -> EnvelopeCodec.decode frame limits) with
                | Error(Interrupted _) ->
                    // Process loss is observed, never signalled: the seam is gone, so no frame can
                    // go out.
                    endpoint.Fail()
                    running <- false
                | Error(Refused fault) ->
                    do! report duplex endpoint (ChannelId.next ()) None fault
                    running <- false
                | Ok envelope ->
                    let! handled =
                        task {
                            match envelope.Kind with
                            | EnvelopeKind.Establish ->
                                match ContractCodec.decode envelope.Body |> Result.bind (fun required -> endpoint.Establish(required, "host")) with
                                | Error error -> return Error error
                                | Ok accepted ->
                                    let! sent =
                                        send
                                            duplex
                                            (Envelope.control
                                                EnvelopeKind.EstablishAccepted
                                                envelope.Channel
                                                (EstablishAcceptedBody.encode accepted))

                                    match sent |> Result.bind (fun () -> endpoint.SignalReady()) with
                                    | Error error -> return Error error
                                    | Ok() ->
                                        let! ready = send duplex (Envelope.empty EnvelopeKind.Ready envelope.Channel)
                                        return ready |> Result.map (fun () -> true)
                            | EnvelopeKind.Request ->
                                match handleRequest endpoint envelope with
                                | Error error -> return Error error
                                | Ok body ->
                                    let! sent =
                                        send
                                            duplex
                                            (Envelope.correlated
                                                EnvelopeKind.Outcome
                                                envelope.Channel
                                                envelope.Request
                                                envelope.Execution
                                                (OutcomeBody.encode body))

                                    return sent |> Result.map (fun () -> true)
                            | EnvelopeKind.Withdraw -> return endpoint.Withdraw() |> Result.map (fun () -> true)
                            | EnvelopeKind.Terminate -> return endpoint.Terminate() |> Result.map (fun () -> false)
                            | EnvelopeKind.EstablishAccepted
                            | EnvelopeKind.Ready
                            | EnvelopeKind.Outcome
                            | EnvelopeKind.ProtocolError ->
                                return
                                    stateViolation
                                        "unexpected-kind"
                                        $"A '{EnvelopeKind.token envelope.Kind}' frame is not directed at a provider endpoint."
                        }

                    match handled with
                    | Ok keepRunning -> running <- keepRunning
                    | Error(Interrupted _) ->
                        endpoint.Fail()
                        running <- false
                    | Error(Refused fault) ->
                        do! report duplex endpoint envelope.Channel envelope.Request fault
                        running <- false
        }
