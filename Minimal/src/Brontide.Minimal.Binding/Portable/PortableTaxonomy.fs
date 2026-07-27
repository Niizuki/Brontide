namespace Brontide.Minimal.Binding.Portable

/// The Channel 0.1 protocol-error categories, reproduced exactly. The neutral contract adds no
/// category and removes none, so this union is the whole vocabulary a rejection may use.
[<RequireQualifiedAccess>]
type ProtocolCategory =
    | MalformedMessage
    | UnsupportedVersion
    | UnsupportedContract
    | UnsupportedKind
    | UnsupportedOperation
    | CorrelationMismatch
    | InvalidPayload
    | InvalidAuthorityPresentation
    | ReplayDetected
    | LimitExceeded
    | StateViolation
    | InternalProtocolFailure

[<RequireQualifiedAccess>]
type ProcessCategory =
    | TransportUnavailable
    | TransportInterrupted
    | Timeout
    | PeerTerminated
    | PeerUnavailable
    | ResourceExhausted
    | Unknown

[<RequireQualifiedAccess>]
type FailureDomain =
    | LocalEndpoint
    | Transport
    | RemoteEndpoint
    | RemoteProvider
    | Unknown

[<RequireQualifiedAccess>]
type FrameDecision =
    | None
    | Emit
    | Accept
    | Reject

[<RequireQualifiedAccess>]
type ResultClass =
    | Negotiation
    | Request
    | OutcomeSucceeded
    | OutcomeFailed
    | ProtocolError
    | ProcessFailure
    | Denial

[<RequireQualifiedAccess>]
type TerminalStatus =
    | Succeeded
    | Failed
    | Denied
    | ProtocolError
    | ProcessFailure

[<RequireQualifiedAccess>]
type AuthorityDecision =
    | Permitted
    | Denied
    | Unknown

[<RequireQualifiedAccess>]
type AuthorityDecisionPoint =
    | HostLocal
    | TargetBoundary
    | ProviderDomain

[<RequireQualifiedAccess>]
module ProtocolCategory =
    let token category =
        match category with
        | ProtocolCategory.MalformedMessage -> "malformed-message"
        | ProtocolCategory.UnsupportedVersion -> "unsupported-version"
        | ProtocolCategory.UnsupportedContract -> "unsupported-contract"
        | ProtocolCategory.UnsupportedKind -> "unsupported-kind"
        | ProtocolCategory.UnsupportedOperation -> "unsupported-operation"
        | ProtocolCategory.CorrelationMismatch -> "correlation-mismatch"
        | ProtocolCategory.InvalidPayload -> "invalid-payload"
        | ProtocolCategory.InvalidAuthorityPresentation -> "invalid-authority-presentation"
        | ProtocolCategory.ReplayDetected -> "replay-detected"
        | ProtocolCategory.LimitExceeded -> "limit-exceeded"
        | ProtocolCategory.StateViolation -> "state-violation"
        | ProtocolCategory.InternalProtocolFailure -> "internal-protocol-failure"

    let all =
        [ ProtocolCategory.MalformedMessage
          ProtocolCategory.UnsupportedVersion
          ProtocolCategory.UnsupportedContract
          ProtocolCategory.UnsupportedKind
          ProtocolCategory.UnsupportedOperation
          ProtocolCategory.CorrelationMismatch
          ProtocolCategory.InvalidPayload
          ProtocolCategory.InvalidAuthorityPresentation
          ProtocolCategory.ReplayDetected
          ProtocolCategory.LimitExceeded
          ProtocolCategory.StateViolation
          ProtocolCategory.InternalProtocolFailure ]

    let tryParse (value: string) =
        all |> List.tryFind (fun category -> token category = value)

[<RequireQualifiedAccess>]
module ProcessCategory =
    let token category =
        match category with
        | ProcessCategory.TransportUnavailable -> "transport-unavailable"
        | ProcessCategory.TransportInterrupted -> "transport-interrupted"
        | ProcessCategory.Timeout -> "timeout"
        | ProcessCategory.PeerTerminated -> "peer-terminated"
        | ProcessCategory.PeerUnavailable -> "peer-unavailable"
        | ProcessCategory.ResourceExhausted -> "resource-exhausted"
        | ProcessCategory.Unknown -> "unknown"

    /// The complete declared set, in the order the Channel taxonomy lists it. It exists so evidence
    /// can be measured against the whole set rather than against the cases someone remembered.
    let declared =
        [ ProcessCategory.TransportUnavailable
          ProcessCategory.TransportInterrupted
          ProcessCategory.Timeout
          ProcessCategory.PeerTerminated
          ProcessCategory.PeerUnavailable
          ProcessCategory.ResourceExhausted
          ProcessCategory.Unknown ]

[<RequireQualifiedAccess>]
module FailureDomain =
    let token domain =
        match domain with
        | FailureDomain.LocalEndpoint -> "local-endpoint"
        | FailureDomain.Transport -> "transport"
        | FailureDomain.RemoteEndpoint -> "remote-endpoint"
        | FailureDomain.RemoteProvider -> "remote-provider"
        | FailureDomain.Unknown -> "unknown"

    let all =
        [ FailureDomain.LocalEndpoint
          FailureDomain.Transport
          FailureDomain.RemoteEndpoint
          FailureDomain.RemoteProvider
          FailureDomain.Unknown ]

    let tryParse (value: string) = all |> List.tryFind (fun domain -> token domain = value)

[<RequireQualifiedAccess>]
module FrameDecision =
    let token decision =
        match decision with
        | FrameDecision.None -> "none"
        | FrameDecision.Emit -> "emit"
        | FrameDecision.Accept -> "accept"
        | FrameDecision.Reject -> "reject"

[<RequireQualifiedAccess>]
module ResultClass =
    let token resultClass =
        match resultClass with
        | ResultClass.Negotiation -> "negotiation"
        | ResultClass.Request -> "request"
        | ResultClass.OutcomeSucceeded -> "outcome-succeeded"
        | ResultClass.OutcomeFailed -> "outcome-failed"
        | ResultClass.ProtocolError -> "protocol-error"
        | ResultClass.ProcessFailure -> "process-failure"
        | ResultClass.Denial -> "denial"

[<RequireQualifiedAccess>]
module TerminalStatus =
    let token status =
        match status with
        | TerminalStatus.Succeeded -> "succeeded"
        | TerminalStatus.Failed -> "failed"
        | TerminalStatus.Denied -> "denied"
        | TerminalStatus.ProtocolError -> "protocol-error"
        | TerminalStatus.ProcessFailure -> "process-failure"

[<RequireQualifiedAccess>]
module AuthorityDecision =
    let token decision =
        match decision with
        | AuthorityDecision.Permitted -> "permitted"
        | AuthorityDecision.Denied -> "denied"
        | AuthorityDecision.Unknown -> "unknown"

[<RequireQualifiedAccess>]
module AuthorityDecisionPoint =
    let token point =
        match point with
        | AuthorityDecisionPoint.HostLocal -> "host-local"
        | AuthorityDecisionPoint.TargetBoundary -> "target-boundary"
        | AuthorityDecisionPoint.ProviderDomain -> "provider-domain"

/// A refusal carrying the portable category, a non-normative local diagnostic code, and the failure
/// domain relative to the endpoint that decided it. No domain value claims global topology.
type PortableFault =
    { Category: ProtocolCategory
      LocalCode: string
      Message: string
      Domain: FailureDomain }

/// A locally observed process failure. It is never signalled by a frame, because the seam through
/// which a frame would travel is exactly what was lost.
type ProcessFailure =
    { Category: ProcessCategory
      Domain: FailureDomain
      Message: string }

/// The two ways an interaction can end other than by producing a value.
///
/// Modelling both as data rather than as exceptions is what keeps a refusal describable: a decision
/// that never leaves this endpoint has no frame to carry it, so it must be a returned value.
type PortableError =
    | Refused of PortableFault
    | Interrupted of ProcessFailure

/// Every operation in this layer returns one of these, so no refusal is expressible as an
/// unhandled runtime failure that could escape the seam.
type PortableResult<'T> = Result<'T, PortableError>

[<RequireQualifiedAccess>]
module PortableFault =
    let create category localCode message =
        { Category = category
          LocalCode = localCode
          Message = message
          Domain = FailureDomain.LocalEndpoint }

    /// Restates a fault the peer reported, which is remote from here even though the category is
    /// the same on both sides.
    let fromRemote category localCode message =
        { Category = category
          LocalCode = localCode
          Message = message
          Domain = FailureDomain.RemoteEndpoint }

    /// The same refusal observed from another endpoint's position. Only the domain moves: the
    /// category, the local code, and the message describe the decision, not who is looking at it.
    let atDomain domain (fault: PortableFault) = { fault with Domain = domain }

    /// Restates a refusal a peer endpoint decided, leaving one it already attributed elsewhere
    /// alone.
    let asPeerDecision result =
        result
        |> Result.mapError (function
            | Refused fault when fault.Domain = FailureDomain.LocalEndpoint ->
                Refused(atDomain FailureDomain.RemoteEndpoint fault)
            | error -> error)

[<AutoOpen>]
module PortableResults =

    let refuse category localCode message : PortableResult<'T> =
        Error(Refused(PortableFault.create category localCode message))

    let malformed localCode message : PortableResult<'T> =
        refuse ProtocolCategory.MalformedMessage localCode message

    let invalidPayload localCode message : PortableResult<'T> =
        refuse ProtocolCategory.InvalidPayload localCode message

    let unsupportedContract localCode message : PortableResult<'T> =
        refuse ProtocolCategory.UnsupportedContract localCode message

    let limitExceeded localCode message : PortableResult<'T> =
        refuse ProtocolCategory.LimitExceeded localCode message

    let stateViolation localCode message : PortableResult<'T> =
        refuse ProtocolCategory.StateViolation localCode message

    let invalidAuthority localCode message : PortableResult<'T> =
        refuse ProtocolCategory.InvalidAuthorityPresentation localCode message

    let lost category domain message : PortableResult<'T> =
        Error(
            Interrupted
                { Category = category
                  Domain = domain
                  Message = message }
        )

    /// Guards a condition without producing a value, so a chain of checks reads as a chain.
    let ensure condition (onFailure: unit -> PortableResult<unit>) : PortableResult<unit> =
        if condition then Ok() else onFailure ()

    /// Applies a fallible step to every element, stopping at the first refusal.
    let traverse (step: 'a -> PortableResult<'b>) (items: 'a list) : PortableResult<'b list> =
        let rec walk accumulated remaining =
            match remaining with
            | [] -> Ok(List.rev accumulated)
            | head :: tail ->
                match step head with
                | Ok value -> walk (value :: accumulated) tail
                | Error error -> Error error

        walk [] items

    /// Applies a fallible step for its refusal only, which is what a validation pass needs.
    let iterate (step: 'a -> PortableResult<unit>) (items: 'a list) : PortableResult<unit> =
        let rec walk remaining =
            match remaining with
            | [] -> Ok()
            | head :: tail ->
                match step head with
                | Ok() -> walk tail
                | Error error -> Error error

        walk items

    [<Sealed>]
    type PortableBuilder() =
        member _.Return(value) : PortableResult<'T> = Ok value
        member _.ReturnFrom(result: PortableResult<'T>) = result
        member _.Bind(result: PortableResult<'T>, binder: 'T -> PortableResult<'U>) = Result.bind binder result
        member _.Zero() : PortableResult<unit> = Ok()
        member _.Delay(generator: unit -> PortableResult<'T>) = generator
        member _.Run(generator: unit -> PortableResult<'T>) = generator ()

        member _.Combine(first: PortableResult<unit>, rest: unit -> PortableResult<'T>) =
            Result.bind rest first

        member _.For(items: 'a seq, body: 'a -> PortableResult<unit>) =
            iterate body (List.ofSeq items)

    let portable = PortableBuilder()
