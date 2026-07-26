namespace Brontide.Minimal.Binding.Portable

open System.Diagnostics
open System.Threading.Tasks

/// The host's decision about one request before anything leaves the host.
///
/// It exists as a separate observation because a denial decided here is frameless: there is no
/// later stage at which to report it, and reporting it as a rejected frame would misdescribe what
/// happened.
[<StructuralEquality; NoComparison>]
type RequestDecision =
    { FrameDecision: FrameDecision
      ResultClass: ResultClass
      Admission: PortableAdmission
      MappingObligations: string list
      Correlation: CorrelationMapping
      DenialObservation: Observation option }

    member this.MayEmit = this.FrameDecision = FrameDecision.Emit

/// The host side of one established portable binding.
///
/// The host evaluates its local authority boundary before any frame is emitted, so a denial starts
/// no provider and produces no frame in either realization. Everything after that point is the
/// realization's business, which is why the same host drives both.
type PortableBindingHost
    private (plan: BindingPlan, conversation: IPortableProviderConversation, channel: ChannelId, initial: Lifecycle, clock: Stopwatch) =

    let mutable lifecycle = initial
    let establishedAt = clock.ElapsedMilliseconds

    let timing requestElapsed =
        { EstablishedAtElapsedMilliseconds = establishedAt
          RequestElapsedMilliseconds = requestElapsed }

    let newCorrelation () =
        { Channel = channel
          Request = ChannelRequestId.next ()
          HostNativeExecution = HostExecutionId.next () }

    let advance kind =
        Lifecycle.apply kind lifecycle |> Result.map (fun next -> lifecycle <- next)

    let rejected (fault: PortableFault) correlation obligations resources started =
        { FrameDecision = FrameDecision.Reject
          ResultClass = ResultClass.ProtocolError
          Category = Some fault.Category
          ProcessCategory = None
          Value = None
          Observation =
            ObservationBuilder.build
                plan
                TerminalStatus.ProtocolError
                AuthorityDecision.Permitted
                (BindingPlan.authorityDecisionPoint plan)
                correlation
                (Some fault.Domain)
                // A rejection never produces a partial provider effect.
                0L
                0L
                resources
                obligations
                false
                (timing (clock.ElapsedMilliseconds - started))
                (Some fault.LocalCode)
                (Some fault.Message) }

    let interrupted (failure: ProcessFailure) correlation obligations resources started =
        { FrameDecision = FrameDecision.None
          ResultClass = ResultClass.ProcessFailure
          Category = None
          ProcessCategory = Some failure.Category
          Value = None
          Observation =
            ObservationBuilder.build
                plan
                TerminalStatus.ProcessFailure
                AuthorityDecision.Permitted
                (BindingPlan.authorityDecisionPoint plan)
                correlation
                (Some failure.Domain)
                0L
                0L
                resources
                obligations
                (failure.Category = ProcessCategory.TransportInterrupted)
                (timing (clock.ElapsedMilliseconds - started))
                (Some(ProcessCategory.token failure.Category))
                (Some failure.Message) }

    let prepareWith operation inputShape authority correlation =
        let admission = PortableAuthorityGate.evaluate (BindingPlan.authority plan) authority

        if not admission.MayProceed then
            Ok
                { FrameDecision = FrameDecision.None
                  ResultClass = ResultClass.Denial
                  Admission = admission
                  MappingObligations = []
                  Correlation = correlation
                  DenialObservation =
                    Some(ObservationBuilder.denial plan correlation admission.Decision (timing 0L) admission.Reason) }
        else
            BindingPlan.operation operation plan
            |> Result.map (fun declaration ->
                { FrameDecision = FrameDecision.Emit
                  ResultClass = ResultClass.Request
                  Admission = admission
                  MappingObligations =
                    if inputShape = declaration.InputShape then
                        []
                    else
                        [ $"projected:{PortableShapeRef.text inputShape}->{PortableShapeRef.text declaration.InputShape}" ]
                  Correlation = correlation
                  DenialObservation = None })

    member _.Plan = plan

    member _.Channel = channel

    member _.State = Lifecycle.state lifecycle

    member _.Realization = conversation.Realization

    /// Establishes one binding scope: negotiate, freeze the plan, and wait for readiness. No
    /// provider effect may occur before the readiness signal arrives.
    static member Establish
        (required: ContractDocument, conversation: IPortableProviderConversation, hostEndpoint: string)
        : Task<PortableResult<PortableBindingHost>> =
        task {
            let clock = Stopwatch.StartNew()
            let channel = ChannelId.next ()
            let lifecycle = Lifecycle.create required.Lifecycle.ReplayProtectionDeclared

            match Lifecycle.apply EnvelopeKind.Establish lifecycle with
            | Error error -> return Error error
            | Ok establishing ->
                let! offered = conversation.Establish(required, hostEndpoint, channel)

                match offered with
                | Error error -> return Error error
                | Ok offered ->
                    let negotiated =
                        portable {
                            let! plan =
                                PortableNegotiation.negotiate
                                    required
                                    offered
                                    conversation.Realization
                                    hostEndpoint
                                    (PortableProviderRef.text offered.Provider)
                                    "the only endpoint offering the required contract; version 0.1 applies no selection policy"

                            let! established = Lifecycle.apply EnvelopeKind.EstablishAccepted establishing
                            return plan, established
                        }

                    match negotiated with
                    | Error error -> return Error error
                    | Ok(plan, established) ->
                        let! ready = conversation.AwaitReady channel

                        return
                            portable {
                                do! ready
                                let! signalled = Lifecycle.apply EnvelopeKind.Ready established
                                return PortableBindingHost(plan, conversation, channel, signalled, clock)
                            }
        }

    /// Evaluates the local authority boundary and the mapping obligations of one request.
    member _.PrepareRequest(operation, inputShape, authority) =
        prepareWith operation inputShape authority (newCorrelation ())

    /// Runs one complete interaction and reports its category-level result.
    ///
    /// A refusal is reported rather than raised, because a complete observation is required even
    /// when the interaction never reached the provider.
    member _.Invoke
        (
            operation: PortableOperationRef,
            inputShape: PortableShapeRef,
            input: PortableValue,
            authority: PortableConstraint,
            ?resources: PortableResource list,
            ?execution: ChannelExecutionId,
            ?prepared: RequestDecision
        ) : Task<InteractionResult> =
        task {
            let presented = defaultArg resources []
            let started = clock.ElapsedMilliseconds

            // The correlation exists before the preparation does, because a refusal raised while
            // preparing still has to be reported as a complete observation rather than thrown at
            // the caller. Naming an Operation outside the contract is exactly such a refusal.
            let correlation =
                match prepared with
                | Some decision -> decision.Correlation
                | None -> newCorrelation ()

            let resourceObservations =
                presented
                |> List.map (fun resource -> PortableResource.observe resource conversation.Realization)

            let copies = resourceObservations |> List.sumBy (fun resource -> resource.Copies)

            let decision =
                match prepared with
                | Some decision -> Ok decision
                | None -> prepareWith operation inputShape authority correlation

            match decision with
            | Error(Refused fault) ->
                lifecycle <- Lifecycle.fail lifecycle
                return rejected fault correlation [] resourceObservations started
            | Error(Interrupted failure) ->
                lifecycle <- Lifecycle.fail lifecycle
                return interrupted failure correlation [] resourceObservations started
            | Ok decision when not decision.MayEmit ->
                return
                    { FrameDecision = FrameDecision.None
                      ResultClass = ResultClass.Denial
                      Category = None
                      ProcessCategory = None
                      Value = None
                      Observation =
                        decision.DenialObservation
                        |> Option.defaultValue (
                            ObservationBuilder.denial
                                plan
                                correlation
                                decision.Admission.Decision
                                (timing 0L)
                                decision.Admission.Reason
                        ) }
            | Ok decision ->
                let obligations = decision.MappingObligations

                let opened =
                    portable {
                        do! advance EnvelopeKind.Request
                        let! next = Lifecycle.recordRequest correlation.Request lifecycle
                        lifecycle <- next
                    }

                match opened with
                | Error(Refused fault) ->
                    lifecycle <- Lifecycle.fail lifecycle
                    return rejected fault correlation obligations resourceObservations started
                | Error(Interrupted failure) ->
                    lifecycle <- Lifecycle.fail lifecycle
                    return interrupted failure correlation obligations resourceObservations started
                | Ok() ->
                    let! receipt =
                        conversation.Request(
                            plan,
                            channel,
                            correlation.Request,
                            execution,
                            OperationDesignation.Canonical operation,
                            inputShape,
                            input,
                            presented
                        )

                    let completed =
                        portable {
                            let! receipt = receipt
                            do! advance EnvelopeKind.Outcome
                            let! declaration = BindingPlan.operation operation plan

                            let expected =
                                match receipt.Status with
                                | OutcomeStatus.Succeeded -> declaration.ResultShape
                                | OutcomeStatus.Failed -> declaration.DetailShape

                            do!
                                ensure (receipt.ValueShape = expected) (fun () ->
                                    invalidPayload
                                        "outcome-shape"
                                        $"The Outcome declares Shape {PortableShapeRef.text receipt.ValueShape} where {PortableShapeRef.text expected} was negotiated.")

                            do!
                                PortableValueCodec.validate
                                    (BindingPlan.catalog plan)
                                    expected
                                    declaration.RequiredFragments
                                    receipt.Value

                            return receipt
                        }

                    match completed with
                    | Error(Refused fault) ->
                        lifecycle <- Lifecycle.fail lifecycle
                        return rejected fault correlation obligations resourceObservations started
                    | Error(Interrupted failure) ->
                        lifecycle <- Lifecycle.fail lifecycle
                        return interrupted failure correlation obligations resourceObservations started
                    | Ok receipt ->
                        let succeeded = receipt.Status = OutcomeStatus.Succeeded

                        return
                            { FrameDecision = FrameDecision.Accept
                              ResultClass =
                                if succeeded then
                                    ResultClass.OutcomeSucceeded
                                else
                                    ResultClass.OutcomeFailed
                              Category = None
                              ProcessCategory = None
                              Value = Some receipt.Value
                              Observation =
                                ObservationBuilder.build
                                    plan
                                    (if succeeded then TerminalStatus.Succeeded else TerminalStatus.Failed)
                                    AuthorityDecision.Permitted
                                    (BindingPlan.authorityDecisionPoint plan)
                                    correlation
                                    (if succeeded then None else Some FailureDomain.RemoteProvider)
                                    receipt.ProviderEffectCount
                                    copies
                                    resourceObservations
                                    obligations
                                    false
                                    (timing (clock.ElapsedMilliseconds - started))
                                    None
                                    None }
        }

    member _.Withdraw() =
        task {
            match advance EnvelopeKind.Withdraw with
            | Error error -> return Error error
            | Ok() -> return! conversation.Withdraw channel
        }

    member _.Terminate() =
        task {
            match advance EnvelopeKind.Terminate with
            | Error error -> return Error error
            | Ok() -> return! conversation.Terminate channel
        }

    member _.Close() = conversation.Close()
