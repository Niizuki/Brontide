using System.Collections.Immutable;
using System.Globalization;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>
/// The Channel correlation identities and the host-native Execution they map onto.
/// </summary>
/// <remarks>
/// The host-native Execution is a distinct identity space by the Channel identity rule, so it never
/// equals a Channel correlation identity.
/// </remarks>
public sealed record PortableCorrelationMapping(
    PortableChannelId ChannelId,
    PortableChannelRequestId RequestId,
    PortableHostExecutionId HostNativeExecution);

/// <summary>Timing is recorded but never drives a semantic decision, so parity excludes it.</summary>
public sealed record PortableTiming(long EstablishedAtElapsedMilliseconds, long RequestElapsedMilliseconds);

/// <summary>
/// The unified observation every completed or rejected interaction reports.
/// </summary>
/// <remarks>
/// An observation records what happened; it never decides what happens. Every normative field is
/// present on every terminal observation, including rejections and denials: an unobservable value
/// uses its declared absent form rather than being omitted.
/// </remarks>
public sealed record PortableObservation(
    PortableProviderReference SelectedProvider,
    string SelectionReason,
    ImmutableArray<PortableOperationReference> NegotiatedOperations,
    int NegotiatedContractVersion,
    string Representation,
    ImmutableArray<string> CrossedBoundaries,
    long CopyCount,
    ImmutableArray<PortableResourceObservation> ReferencedResources,
    PortableAuthorityDecisionPoint AuthorityDecisionPoint,
    PortableAuthorityDecision AuthorityDecision,
    ImmutableArray<string> MappingObligations,
    long RetryCount,
    bool Interrupted,
    PortableFailureDomain? FailureDomain,
    PortableTerminalStatus TerminalStatus,
    PortableCorrelationMapping CorrelationMapping,
    long? ProviderEffectCount,
    PortableTiming Timing,
    string? LocalCode,
    string? LocalMessage)
{
    /// <summary>
    /// Verifies the completeness rule. A missing declared-absent form would let an unobservable
    /// value masquerade as an omission.
    /// </summary>
    public void RequireComplete()
    {
        if (SelectionReason.Length == 0)
        {
            throw new InvalidOperationException("An observation records why the provider was selected.");
        }

        if (RetryCount != 0)
        {
            throw new InvalidOperationException("Version 0.1 makes no retry promise, so the retry count is always zero.");
        }

        var succeeded = TerminalStatus == PortableTerminalStatus.Succeeded;
        if (succeeded == FailureDomain.HasValue)
        {
            throw new InvalidOperationException(
                "A failure domain is present exactly when the terminal status is not 'succeeded'.");
        }

        if (CrossedBoundaries.IsDefaultOrEmpty)
        {
            throw new InvalidOperationException("Crossed boundaries are reported, using 'none' when nothing was crossed.");
        }

        if (ProviderEffectCount is < 0 || CopyCount < 0)
        {
            throw new InvalidOperationException("Effect and copy counts are never negative.");
        }

        if (succeeded && !ProviderEffectCount.HasValue)
        {
            throw new InvalidOperationException("A succeeded observation has a known provider effect count.");
        }

        if (CorrelationMapping.HostNativeExecution.Value == CorrelationMapping.RequestId.Value ||
            CorrelationMapping.HostNativeExecution.Value == CorrelationMapping.ChannelId.Value)
        {
            throw new InvalidOperationException(
                "The host-native Execution identity is distinct from every Channel correlation identity.");
        }
    }

    /// <summary>
    /// The category-level fields two realizations of the same vector must report identically.
    /// </summary>
    /// <remarks>
    /// Representation, framing, crossed boundaries, copy accounting, correlation, timing, and local
    /// codes are excluded: the realization is exactly what differs, and copy accounting differs by
    /// realization by design.
    /// </remarks>
    public ImmutableSortedDictionary<string, string> ParityProfile()
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        builder["authorityDecision"] = AuthorityDecision.Token();
        builder["authorityDecisionPoint"] = AuthorityDecisionPoint.Token();
        builder["terminalStatus"] = TerminalStatus.Token();
        builder["failureDomain"] = FailureDomain?.Token() ?? "absent";
        builder["providerEffectCount"] = ProviderEffectCount?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
        builder["negotiatedOperations"] = string.Join(", ", NegotiatedOperations.Select(operation => operation.ToString()));
        builder["negotiatedContractVersion"] = NegotiatedContractVersion.ToString(CultureInfo.InvariantCulture);
        builder["mappingObligations"] = string.Join(", ", MappingObligations);
        builder["interrupted"] = Interrupted ? "true" : "false";
        builder["retryCount"] = RetryCount.ToString(CultureInfo.InvariantCulture);
        builder["referencedResources"] = string.Join(
            "; ",
            ReferencedResources.Select(resource =>
                $"{resource.Flavor}/{resource.Ownership}/{resource.IntegrityVerified}/{resource.Accepted}"));
        return builder.ToImmutable();
    }
}

/// <summary>One interaction's category-level result together with its observation.</summary>
public sealed record PortableInteractionResult(
    PortableFrameDecision FrameDecision,
    PortableResultClass ResultClass,
    PortableProtocolCategory? Category,
    PortableProcessCategory? ProcessCategory,
    PortableValue? Value,
    PortableObservation Observation)
{
    /// <summary>The parity profile plus the category-level result the vectors state.</summary>
    public ImmutableSortedDictionary<string, string> ParityProfile()
    {
        var builder = Observation.ParityProfile().ToBuilder();
        builder["frameDecision"] = FrameDecision.Token();
        builder["resultClass"] = ResultClass.Token();
        builder["category"] = Category?.Token() ?? "absent";
        builder["processCategory"] = ProcessCategory?.Token() ?? "absent";
        return builder.ToImmutable();
    }
}

/// <summary>Builds observations that satisfy the completeness rule by construction.</summary>
public sealed class PortableObservationBuilder(PortableBindingPlan plan)
{
    private readonly PortableBindingPlan _plan = plan ?? throw new ArgumentNullException(nameof(plan));

    public PortableObservation Build(
        PortableTerminalStatus terminalStatus,
        PortableAuthorityDecision authorityDecision,
        PortableAuthorityDecisionPoint authorityDecisionPoint,
        PortableCorrelationMapping correlation,
        PortableFailureDomain? failureDomain,
        long? providerEffectCount,
        long copyCount,
        ImmutableArray<PortableResourceObservation> resources,
        ImmutableArray<string> mappingObligations,
        bool interrupted,
        PortableTiming timing,
        string? localCode,
        string? localMessage)
    {
        var observation = new PortableObservation(
            _plan.SelectedProvider,
            _plan.SelectionReason,
            _plan.Operations,
            _plan.ContractVersion,
            _plan.Representation,
            _plan.CrossedBoundaries,
            copyCount,
            resources,
            authorityDecisionPoint,
            authorityDecision,
            mappingObligations,
            0,
            interrupted,
            failureDomain,
            terminalStatus,
            correlation,
            providerEffectCount,
            timing,
            localCode,
            localMessage);
        observation.RequireComplete();
        return observation;
    }

    /// <summary>
    /// The denial profile: a local denial produces a complete observation even though no frame was
    /// emitted, and it crosses no boundary because it never left the host.
    /// </summary>
    public PortableObservation Denial(
        PortableCorrelationMapping correlation,
        PortableAuthorityDecision decision,
        PortableTiming timing,
        string reason)
    {
        var observation = new PortableObservation(
            _plan.SelectedProvider,
            _plan.SelectionReason,
            _plan.Operations,
            _plan.ContractVersion,
            _plan.Representation,
            ["none"],
            0,
            [],
            PortableAuthorityDecisionPoint.HostLocal,
            decision,
            [],
            0,
            false,
            PortableFailureDomain.LocalEndpoint,
            PortableTerminalStatus.Denied,
            correlation,
            0,
            timing,
            "local-denial",
            reason);
        observation.RequireComplete();
        return observation;
    }
}
