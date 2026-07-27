namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>Portable protocol-error categories, reproduced from the Channel 0.1 taxonomy.</summary>
/// <remarks>
/// The neutral contract states that this set is reproduced exactly and never extended, so the
/// enumeration and its portable tokens are the whole vocabulary a rejection may use.
/// </remarks>
public enum PortableProtocolCategory
{
    MalformedMessage,
    UnsupportedVersion,
    UnsupportedContract,
    UnsupportedKind,
    UnsupportedOperation,
    CorrelationMismatch,
    InvalidPayload,
    InvalidAuthorityPresentation,
    ReplayDetected,
    LimitExceeded,
    StateViolation,
    InternalProtocolFailure
}

public enum PortableProcessCategory
{
    TransportUnavailable,
    TransportInterrupted,
    Timeout,
    PeerTerminated,
    PeerUnavailable,
    ResourceExhausted,
    Unknown
}

public enum PortableFailureDomain
{
    LocalEndpoint,
    Transport,
    RemoteEndpoint,
    RemoteProvider,
    Unknown
}

public enum PortableFrameDecision
{
    None,
    Emit,
    Accept,
    Reject
}

public enum PortableResultClass
{
    Negotiation,
    Request,
    OutcomeSucceeded,
    OutcomeFailed,
    ProtocolError,
    ProcessFailure,
    Denial
}

public enum PortableTerminalStatus
{
    Succeeded,
    Failed,
    Denied,
    ProtocolError,
    ProcessFailure
}

public enum PortableAuthorityDecision
{
    Permitted,
    Denied,
    Unknown
}

public enum PortableAuthorityDecisionPoint
{
    HostLocal,
    TargetBoundary,
    ProviderDomain
}

/// <summary>Renders each taxonomy value as the exact token the neutral contract declares.</summary>
public static class PortableTokens
{
    public static string Token(this PortableProtocolCategory category) => category switch
    {
        PortableProtocolCategory.MalformedMessage => "malformed-message",
        PortableProtocolCategory.UnsupportedVersion => "unsupported-version",
        PortableProtocolCategory.UnsupportedContract => "unsupported-contract",
        PortableProtocolCategory.UnsupportedKind => "unsupported-kind",
        PortableProtocolCategory.UnsupportedOperation => "unsupported-operation",
        PortableProtocolCategory.CorrelationMismatch => "correlation-mismatch",
        PortableProtocolCategory.InvalidPayload => "invalid-payload",
        PortableProtocolCategory.InvalidAuthorityPresentation => "invalid-authority-presentation",
        PortableProtocolCategory.ReplayDetected => "replay-detected",
        PortableProtocolCategory.LimitExceeded => "limit-exceeded",
        PortableProtocolCategory.StateViolation => "state-violation",
        PortableProtocolCategory.InternalProtocolFailure => "internal-protocol-failure",
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    public static string Token(this PortableProcessCategory category) => category switch
    {
        PortableProcessCategory.TransportUnavailable => "transport-unavailable",
        PortableProcessCategory.TransportInterrupted => "transport-interrupted",
        PortableProcessCategory.Timeout => "timeout",
        PortableProcessCategory.PeerTerminated => "peer-terminated",
        PortableProcessCategory.PeerUnavailable => "peer-unavailable",
        PortableProcessCategory.ResourceExhausted => "resource-exhausted",
        PortableProcessCategory.Unknown => "unknown",
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    public static string Token(this PortableFailureDomain domain) => domain switch
    {
        PortableFailureDomain.LocalEndpoint => "local-endpoint",
        PortableFailureDomain.Transport => "transport",
        PortableFailureDomain.RemoteEndpoint => "remote-endpoint",
        PortableFailureDomain.RemoteProvider => "remote-provider",
        PortableFailureDomain.Unknown => "unknown",
        _ => throw new ArgumentOutOfRangeException(nameof(domain))
    };

    public static string Token(this PortableFrameDecision decision) => decision switch
    {
        PortableFrameDecision.None => "none",
        PortableFrameDecision.Emit => "emit",
        PortableFrameDecision.Accept => "accept",
        PortableFrameDecision.Reject => "reject",
        _ => throw new ArgumentOutOfRangeException(nameof(decision))
    };

    public static string Token(this PortableResultClass resultClass) => resultClass switch
    {
        PortableResultClass.Negotiation => "negotiation",
        PortableResultClass.Request => "request",
        PortableResultClass.OutcomeSucceeded => "outcome-succeeded",
        PortableResultClass.OutcomeFailed => "outcome-failed",
        PortableResultClass.ProtocolError => "protocol-error",
        PortableResultClass.ProcessFailure => "process-failure",
        PortableResultClass.Denial => "denial",
        _ => throw new ArgumentOutOfRangeException(nameof(resultClass))
    };

    public static string Token(this PortableTerminalStatus status) => status switch
    {
        PortableTerminalStatus.Succeeded => "succeeded",
        PortableTerminalStatus.Failed => "failed",
        PortableTerminalStatus.Denied => "denied",
        PortableTerminalStatus.ProtocolError => "protocol-error",
        PortableTerminalStatus.ProcessFailure => "process-failure",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static string Token(this PortableAuthorityDecision decision) => decision switch
    {
        PortableAuthorityDecision.Permitted => "permitted",
        PortableAuthorityDecision.Denied => "denied",
        PortableAuthorityDecision.Unknown => "unknown",
        _ => throw new ArgumentOutOfRangeException(nameof(decision))
    };

    public static string Token(this PortableAuthorityDecisionPoint point) => point switch
    {
        PortableAuthorityDecisionPoint.HostLocal => "host-local",
        PortableAuthorityDecisionPoint.TargetBoundary => "target-boundary",
        PortableAuthorityDecisionPoint.ProviderDomain => "provider-domain",
        _ => throw new ArgumentOutOfRangeException(nameof(point))
    };
}

/// <summary>
/// A portable rejection carrying the taxonomy category and a non-normative local diagnostic code.
/// </summary>
/// <remarks>
/// The exception is the layer's internal control flow only. It never crosses the seam: an endpoint
/// converts it to a <c>protocol-error</c> envelope or an observation, so no runtime type, stack
/// trace, or message of a private type is transported.
/// </remarks>
public sealed class PortableFaultException(
    PortableProtocolCategory category,
    string localCode,
    string message,
    PortableFailureDomain domain = PortableFailureDomain.LocalEndpoint) : InvalidOperationException(message)
{
    public PortableProtocolCategory Category { get; } = category;

    public string LocalCode { get; } = localCode;

    /// <summary>
    /// The failure domain relative to the endpoint that raised it. A rejection this endpoint decided
    /// is local; one a peer reported back is remote from here. No domain value claims global topology.
    /// </summary>
    public PortableFailureDomain Domain { get; } = domain;

    /// <summary>The same refusal, observed from another endpoint's position.</summary>
    /// <remarks>
    /// Only the domain moves. The category, the local code, and the message describe the decision,
    /// which is unchanged by who is looking at it.
    /// </remarks>
    public PortableFaultException AtDomain(PortableFailureDomain domain) =>
        new(Category, LocalCode, Message, domain);

    public static PortableFaultException Malformed(string localCode, string message) =>
        new(PortableProtocolCategory.MalformedMessage, localCode, message);

    public static PortableFaultException LimitExceeded(string localCode, string message) =>
        new(PortableProtocolCategory.LimitExceeded, localCode, message);

    public static PortableFaultException InvalidPayload(string localCode, string message) =>
        new(PortableProtocolCategory.InvalidPayload, localCode, message);
}

/// <summary>A locally observed process failure; it is never signalled by a frame.</summary>
public sealed class PortableProcessFailureException(
    PortableProcessCategory category,
    PortableFailureDomain domain,
    string message) : IOException(message)
{
    public PortableProcessCategory Category { get; } = category;

    public PortableFailureDomain Domain { get; } = domain;
}
