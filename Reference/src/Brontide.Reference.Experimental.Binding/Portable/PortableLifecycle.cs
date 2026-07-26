using System.Collections.Immutable;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>The explicit binding lifecycle states.</summary>
/// <remarks>
/// The baseline experiments left their lifecycles implicit in message ordering. Making the states
/// explicit is what lets an illegal transition be a state violation rather than an accident of
/// arrival order.
/// </remarks>
public enum PortableLifecycleState
{
    Unestablished,
    Establishing,
    Established,
    Ready,
    Active,
    Withdrawn,
    Terminated,
    Failed
}

public static class PortableLifecycleStates
{
    public static string Token(this PortableLifecycleState state) => state switch
    {
        PortableLifecycleState.Unestablished => "unestablished",
        PortableLifecycleState.Establishing => "establishing",
        PortableLifecycleState.Established => "established",
        PortableLifecycleState.Ready => "ready",
        PortableLifecycleState.Active => "active",
        PortableLifecycleState.Withdrawn => "withdrawn",
        PortableLifecycleState.Terminated => "terminated",
        PortableLifecycleState.Failed => "failed",
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };

    public static bool IsTerminal(this PortableLifecycleState state) =>
        state is PortableLifecycleState.Terminated or PortableLifecycleState.Failed;
}

/// <summary>
/// The lifecycle state machine for one binding scope at one endpoint.
/// </summary>
/// <remarks>
/// No provider effect occurs before the ready state is reached, a request is legal only in ready
/// because the declared concurrency is one, and correlation mismatch, illegal state, peer
/// termination, timeout, and interruption never fabricate a success.
/// </remarks>
public sealed class PortableLifecycle
{
    private static readonly ImmutableArray<(PortableLifecycleState From, PortableEnvelopeKind On, PortableLifecycleState To)> Transitions =
    [
        (PortableLifecycleState.Unestablished, PortableEnvelopeKind.Establish, PortableLifecycleState.Establishing),
        (PortableLifecycleState.Establishing, PortableEnvelopeKind.EstablishAccepted, PortableLifecycleState.Established),
        (PortableLifecycleState.Established, PortableEnvelopeKind.Ready, PortableLifecycleState.Ready),
        (PortableLifecycleState.Ready, PortableEnvelopeKind.Request, PortableLifecycleState.Active),
        (PortableLifecycleState.Active, PortableEnvelopeKind.Outcome, PortableLifecycleState.Ready),
        (PortableLifecycleState.Ready, PortableEnvelopeKind.Withdraw, PortableLifecycleState.Withdrawn),
        (PortableLifecycleState.Active, PortableEnvelopeKind.Withdraw, PortableLifecycleState.Withdrawn),
        (PortableLifecycleState.Withdrawn, PortableEnvelopeKind.Terminate, PortableLifecycleState.Terminated),
        (PortableLifecycleState.Ready, PortableEnvelopeKind.Terminate, PortableLifecycleState.Terminated),
        (PortableLifecycleState.Established, PortableEnvelopeKind.Terminate, PortableLifecycleState.Terminated)
    ];

    private readonly HashSet<string> _acceptedRequests = new(StringComparer.Ordinal);
    private bool _replayProtectionDeclared;

    public PortableLifecycle(bool replayProtectionDeclared) => _replayProtectionDeclared = replayProtectionDeclared;

    /// <summary>
    /// Adopts the replay declaration the Binding Plan froze. It is declared during establishment,
    /// which is the only point at which the window can start.
    /// </summary>
    public void DeclareReplayProtection(bool declared) => _replayProtectionDeclared = declared;

    public PortableLifecycleState State { get; private set; } = PortableLifecycleState.Unestablished;

    /// <summary>Applies one envelope kind, or refuses it as a state violation.</summary>
    public void Apply(PortableEnvelopeKind kind)
    {
        // A protocol error is legal in every non-terminal state and always ends the binding.
        if (kind == PortableEnvelopeKind.ProtocolError)
        {
            RequireNotTerminal(kind);
            State = PortableLifecycleState.Failed;
            return;
        }

        RequireNotTerminal(kind);
        foreach (var transition in Transitions)
        {
            if (transition.From == State && transition.On == kind)
            {
                State = transition.To;
                return;
            }
        }

        throw new PortableFaultException(
            PortableProtocolCategory.StateViolation,
            "illegal-transition",
            $"A '{kind.Token()}' frame is illegal in state '{State.Token()}'.");
    }

    public void Fail() => State = PortableLifecycleState.Failed;

    /// <summary>
    /// Records an accepted request identity inside the declared replay window, refusing a repeat.
    /// </summary>
    public void RecordRequest(PortableChannelRequestId request)
    {
        if (!_replayProtectionDeclared)
        {
            return;
        }

        if (!_acceptedRequests.Add(request.Value))
        {
            throw new PortableFaultException(
                PortableProtocolCategory.ReplayDetected,
                "replay",
                $"Request '{request}' already appeared inside the declared replay window.");
        }
    }

    private void RequireNotTerminal(PortableEnvelopeKind kind)
    {
        if (State.IsTerminal())
        {
            throw new PortableFaultException(
                PortableProtocolCategory.StateViolation,
                "terminal-state",
                $"No frame is legal in terminal state '{State.Token()}'; '{kind.Token()}' was refused.");
        }
    }
}
