namespace Brontide.Minimal.Binding.Portable

/// The Channel envelope kinds.
///
/// There is deliberately no denial kind: a local authority denial emits no frame at all, so the
/// baseline experiment's denial message is not carried into the portable envelope set. Process loss
/// is likewise observed locally rather than signalled.
[<RequireQualifiedAccess>]
type EnvelopeKind =
    | Establish
    | EstablishAccepted
    | Ready
    | Request
    | Outcome
    | ProtocolError
    | Withdraw
    | Terminate

[<RequireQualifiedAccess>]
module EnvelopeKind =
    let token kind =
        match kind with
        | EnvelopeKind.Establish -> "establish"
        | EnvelopeKind.EstablishAccepted -> "establish-accepted"
        | EnvelopeKind.Ready -> "ready"
        | EnvelopeKind.Request -> "request"
        | EnvelopeKind.Outcome -> "outcome"
        | EnvelopeKind.ProtocolError -> "protocol-error"
        | EnvelopeKind.Withdraw -> "withdraw"
        | EnvelopeKind.Terminate -> "terminate"

    let all =
        [ EnvelopeKind.Establish
          EnvelopeKind.EstablishAccepted
          EnvelopeKind.Ready
          EnvelopeKind.Request
          EnvelopeKind.Outcome
          EnvelopeKind.ProtocolError
          EnvelopeKind.Withdraw
          EnvelopeKind.Terminate ]

    let tryParse value : PortableResult<EnvelopeKind> =
        match all |> List.tryFind (fun kind -> token kind = value) with
        | Some kind -> Ok kind
        | None ->
            refuse
                ProtocolCategory.UnsupportedKind
                "unknown-kind"
                $"Envelope kind '{value}' is outside the declared set; an unknown kind is never treated as a no-op."

    /// A request and an outcome both require a request identity; neither is ever matched to an
    /// outstanding request by position.
    let requiresRequestId kind =
        kind = EnvelopeKind.Request || kind = EnvelopeKind.Outcome

/// The explicit binding lifecycle states.
///
/// The baseline experiments left their lifecycles implicit in message ordering. Making the states
/// explicit is what lets an illegal transition be a state violation rather than an accident of
/// arrival order.
[<RequireQualifiedAccess>]
type LifecycleState =
    | Unestablished
    | Establishing
    | Established
    | Ready
    | Active
    | Withdrawn
    | Terminated
    | Failed

[<RequireQualifiedAccess>]
module LifecycleState =
    let token state =
        match state with
        | LifecycleState.Unestablished -> "unestablished"
        | LifecycleState.Establishing -> "establishing"
        | LifecycleState.Established -> "established"
        | LifecycleState.Ready -> "ready"
        | LifecycleState.Active -> "active"
        | LifecycleState.Withdrawn -> "withdrawn"
        | LifecycleState.Terminated -> "terminated"
        | LifecycleState.Failed -> "failed"

    let isTerminal state =
        state = LifecycleState.Terminated || state = LifecycleState.Failed

/// The lifecycle of one binding scope at one endpoint, together with the accepted request
/// identities inside the declared replay window.
///
/// The record is immutable and every transition returns a new one, so an illegal transition leaves
/// the previous state untouched rather than half-applied.
[<StructuralEquality; NoComparison>]
type Lifecycle =
    private
        { State: LifecycleState
          AcceptedRequests: Set<string>
          ReplayProtectionDeclared: bool }

[<RequireQualifiedAccess>]
module Lifecycle =

    let private transitions =
        [ LifecycleState.Unestablished, EnvelopeKind.Establish, LifecycleState.Establishing
          LifecycleState.Establishing, EnvelopeKind.EstablishAccepted, LifecycleState.Established
          LifecycleState.Established, EnvelopeKind.Ready, LifecycleState.Ready
          LifecycleState.Ready, EnvelopeKind.Request, LifecycleState.Active
          LifecycleState.Active, EnvelopeKind.Outcome, LifecycleState.Ready
          LifecycleState.Ready, EnvelopeKind.Withdraw, LifecycleState.Withdrawn
          LifecycleState.Active, EnvelopeKind.Withdraw, LifecycleState.Withdrawn
          LifecycleState.Withdrawn, EnvelopeKind.Terminate, LifecycleState.Terminated
          LifecycleState.Ready, EnvelopeKind.Terminate, LifecycleState.Terminated
          LifecycleState.Established, EnvelopeKind.Terminate, LifecycleState.Terminated ]

    let create replayProtectionDeclared =
        { State = LifecycleState.Unestablished
          AcceptedRequests = Set.empty
          ReplayProtectionDeclared = replayProtectionDeclared }

    let state (lifecycle: Lifecycle) = lifecycle.State

    /// Adopts the replay declaration the Binding Plan froze. Establishment is the only point at
    /// which the window can start.
    let declareReplayProtection declared (lifecycle: Lifecycle) =
        { lifecycle with ReplayProtectionDeclared = declared }

    let fail (lifecycle: Lifecycle) =
        { lifecycle with State = LifecycleState.Failed }

    /// Applies one envelope kind, or refuses it as a state violation.
    let apply kind (lifecycle: Lifecycle) : PortableResult<Lifecycle> =
        if LifecycleState.isTerminal lifecycle.State then
            stateViolation
                "terminal-state"
                $"No frame is legal in terminal state '{LifecycleState.token lifecycle.State}'; '{EnvelopeKind.token kind}' was refused."
        elif kind = EnvelopeKind.ProtocolError then
            // A protocol error is legal in every non-terminal state and always ends the binding.
            Ok { lifecycle with State = LifecycleState.Failed }
        else
            match
                transitions
                |> List.tryFind (fun (from, on, _) -> from = lifecycle.State && on = kind)
            with
            | Some(_, _, next) -> Ok { lifecycle with State = next }
            | None ->
                stateViolation
                    "illegal-transition"
                    $"A '{EnvelopeKind.token kind}' frame is illegal in state '{LifecycleState.token lifecycle.State}'."

    /// Records an accepted request identity inside the declared replay window, refusing a repeat.
    let recordRequest (request: ChannelRequestId) (lifecycle: Lifecycle) : PortableResult<Lifecycle> =
        if not lifecycle.ReplayProtectionDeclared then
            Ok lifecycle
        else
            let identity = ChannelRequestId.value request

            if Set.contains identity lifecycle.AcceptedRequests then
                refuse
                    ProtocolCategory.ReplayDetected
                    "replay"
                    $"Request '{identity}' already appeared inside the declared replay window."
            else
                Ok
                    { lifecycle with
                        AcceptedRequests = Set.add identity lifecycle.AcceptedRequests }
