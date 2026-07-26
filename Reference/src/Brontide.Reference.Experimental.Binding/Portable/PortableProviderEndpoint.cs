using System.Collections.Immutable;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>What one provider Operation did.</summary>
public sealed record PortableOperationEffect(bool Succeeded, PortableValue Value, long ProviderEffectCount)
{
    public static PortableOperationEffect Success(PortableValue result, long effectCount) => new(true, result, effectCount);

    /// <summary>
    /// A shaped failed Outcome: the Operation reported structured failure details in its declared
    /// detail Shape. It is not a protocol or process failure and transports no exception.
    /// </summary>
    public static PortableOperationEffect Failure(PortableValue details, long effectCount) => new(false, details, effectCount);
}

/// <summary>
/// The provider's domain behind the binding.
/// </summary>
/// <remarks>
/// This is where the provider's own admission happens for a cross-trust presentation: the handler
/// receives attributable context and exact addressing and decides for itself, reporting a refusal
/// as a shaped failed Outcome rather than as a protocol rejection.
/// </remarks>
public interface IPortableOperationHandler
{
    PortableOperationEffect Invoke(
        PortableOperationReference operation,
        PortableValue input,
        IReadOnlyList<PortableResource> resources);
}

/// <summary>The provider's answer to one request, before any transport decision.</summary>
public sealed record PortableProviderOutcome(
    PortableOutcomeStatus Status,
    PortableShapeReference ValueShape,
    PortableValue Value,
    long ProviderEffectCount,
    ImmutableArray<string> MappingObligations,
    ImmutableArray<PortableResourceObservation> Resources,
    long CopyCount);

/// <summary>
/// The semantic provider endpoint: lifecycle, negotiation, conformance, resource admission, and
/// dispatch, with no transport knowledge.
/// </summary>
/// <remarks>
/// Both realizations drive this same endpoint, which is what makes their category-level
/// observations comparable: the direct call and the process seam differ in how a request arrives,
/// not in what the endpoint decides about it.
/// </remarks>
public sealed class PortableProviderEndpoint
{
    private readonly PortableContractDocument _offered;
    private readonly IPortableOperationHandler _handler;
    private readonly PortableRealization _realization;
    private readonly PortableLifecycle _lifecycle = new(replayProtectionDeclared: false);

    public PortableProviderEndpoint(
        PortableContractDocument offered,
        IPortableOperationHandler handler,
        PortableRealization realization)
    {
        _offered = offered ?? throw new ArgumentNullException(nameof(offered));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _realization = realization;
    }

    public PortableBindingPlan? Plan { get; private set; }

    public PortableLifecycleState State => _lifecycle.State;

    public PortableEstablishAcceptedBody Establish(PortableContractDocument required, string hostEndpoint)
    {
        ArgumentNullException.ThrowIfNull(required);
        _lifecycle.Apply(PortableEnvelopeKind.Establish);
        var plan = PortableNegotiation.Negotiate(
            required,
            _offered,
            _realization,
            hostEndpoint,
            _offered.Provider.ToString(),
            "the provider endpoint offered the exact negotiated contract");
        _lifecycle.DeclareReplayProtection(plan.ReplayProtectionDeclared);
        _lifecycle.Apply(PortableEnvelopeKind.EstablishAccepted);
        Plan = plan;
        return new PortableEstablishAcceptedBody(_offered, plan.CompactIdentifierAssignments);
    }

    public void SignalReady() => _lifecycle.Apply(PortableEnvelopeKind.Ready);

    public void Withdraw() => _lifecycle.Apply(PortableEnvelopeKind.Withdraw);

    public void Terminate() => _lifecycle.Apply(PortableEnvelopeKind.Terminate);

    public void Fail() => _lifecycle.Fail();

    /// <summary>
    /// Handles one request, or refuses it with a portable category.
    /// </summary>
    /// <remarks>
    /// A refusal ends the binding, because the lifecycle's declared transition on a protocol error
    /// is to the failed state. Applying it here rather than in the transport is what keeps the
    /// direct realization's endpoint state equal to the process realization's.
    /// </remarks>
    public PortableProviderOutcome Request(
        PortableChannelRequestId requestId,
        PortableOperationReference? operation,
        int? compactOperation,
        PortableShapeReference inputShape,
        PortableValue input,
        IReadOnlyList<PortableResource> resources)
    {
        try
        {
            return RequestCore(requestId, operation, compactOperation, inputShape, input, resources);
        }
        catch (PortableFaultException)
        {
            _lifecycle.Fail();
            throw;
        }
    }

    private PortableProviderOutcome RequestCore(
        PortableChannelRequestId requestId,
        PortableOperationReference? operation,
        int? compactOperation,
        PortableShapeReference inputShape,
        PortableValue input,
        IReadOnlyList<PortableResource> resources)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(resources);
        var plan = Plan ?? throw new PortableFaultException(
            PortableProtocolCategory.StateViolation,
            "unestablished",
            "A request arrived before any contract was established.");

        _lifecycle.Apply(PortableEnvelopeKind.Request);
        _lifecycle.RecordRequest(requestId);

        var declaration = ResolveOperation(operation, compactOperation);
        var reference = declaration.Reference;

        if (plan.TrustBoundaryCrossed)
        {
            PortableAuthorityVocabulary.RequireNoCapabilityContent(input);
        }

        foreach (var resource in resources)
        {
            if (!declaration.ResourceFlavors.Contains(resource.Flavor.Token(), StringComparer.Ordinal))
            {
                throw new PortableFaultException(
                    PortableProtocolCategory.UnsupportedContract,
                    "resource-flavor-unnegotiated",
                    $"Operation {reference} declares no resource flavor '{resource.Flavor.Token()}'.");
            }

            PortableResourceCodec.Admit(resource, plan.ResourceFlavors, plan.AcceptedResourceHandles, plan.Limits);
        }

        var projection = PortableValueCodec.Project(
            plan.Catalog,
            input,
            inputShape,
            declaration.InputShape,
            declaration.RequiredFragments);
        PortableValueCodec.Validate(
            plan.Catalog,
            declaration.InputShape,
            projection.Value,
            declaration.RequiredFragments);
        RequireDeclaredFragments(projection.Value, declaration);

        PortableOperationEffect effect;
        try
        {
            effect = _handler.Invoke(reference, projection.Value, resources);
        }
        catch (Exception exception) when (exception is not PortableFaultException and not PortableProcessFailureException)
        {
            // The provider's own runtime failed. Only the portable category crosses: no exception,
            // stack trace, or runtime type name is admitted into the observation or the frame.
            throw new PortableFaultException(
                PortableProtocolCategory.InternalProtocolFailure,
                "handler-failure",
                "The endpoint cannot continue protocol processing.");
        }

        _lifecycle.Apply(PortableEnvelopeKind.Outcome);

        var observations = resources
            .Select(resource => PortableResourceCodec.Observe(resource, _realization))
            .ToImmutableArray();
        return new PortableProviderOutcome(
            effect.Succeeded ? PortableOutcomeStatus.Succeeded : PortableOutcomeStatus.Failed,
            effect.Succeeded ? declaration.ResultShape : declaration.DetailShape,
            effect.Value,
            effect.ProviderEffectCount,
            projection.MappingObligations,
            observations,
            observations.Sum(resource => resource.Copies));
    }

    /// <summary>
    /// Resolves the Operation a request names, canonically or by a binding-scoped compact
    /// identifier. The transport needs the declaration before it can decode a schema-guided input.
    /// </summary>
    public PortableOperationDeclaration ResolveOperation(PortableOperationReference? operation, int? compactOperation)
    {
        var plan = Plan ?? throw new PortableFaultException(
            PortableProtocolCategory.StateViolation,
            "unestablished",
            "No contract is established, so no Operation can be resolved.");

        if (operation is { } reference)
        {
            return plan.Operation(reference);
        }

        var compact = compactOperation ?? throw PortableFaultException.Malformed(
            "operation-designation",
            "A request names its Operation either canonically or by one compact identifier.");
        var assignment = plan.CompactIdentifierAssignments.FirstOrDefault(candidate =>
            candidate.Space == PortableIdentitySpace.Operation && candidate.CompactId.Value == compact);
        if (assignment is null)
        {
            // A compact identifier this binding never assigned resolves to no canonical identity.
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "compact-identifier-unassigned",
                $"Compact identifier {compact} was never assigned in this binding.");
        }

        return plan.Operation(plan.Operations.First(candidate => candidate.ToString() == assignment.Reference));
    }

    /// <summary>Attribution is never inferred from delivery, so a required Fragment must be present.</summary>
    private static void RequireDeclaredFragments(PortableValue value, PortableOperationDeclaration declaration)
    {
        if (declaration.RequiredFragments.IsEmpty)
        {
            return;
        }

        var attached = value is PortableRecordValue record
            ? record.Fragments.Keys.ToImmutableHashSet()
            : [];
        foreach (var fragment in declaration.RequiredFragments)
        {
            if (!attached.Contains(fragment))
            {
                throw PortableFaultException.InvalidPayload(
                    "required-fragment-absent",
                    $"Operation {declaration.Reference} requires Fragment {fragment}.");
            }
        }
    }
}
