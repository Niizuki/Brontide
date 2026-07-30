using System.Collections.Immutable;
using System.Globalization;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>The declared payload representations.</summary>
public static class PortableRepresentations
{
    /// <summary>The portable wire for the negotiated process realization.</summary>
    public const string PortableCborCore = "portable-cbor-core-0.1";

    /// <summary>
    /// The retained Cooling and Catalog representation. It is diagnostic and legacy only and is not
    /// the portable wire contract, so a portable binding refuses it rather than negotiating it.
    /// </summary>
    public const string InlineTaggedJson = "inline-tagged-json";

    public const string LengthDelimited = "length-delimited";
    public const string DirectCall = "direct-call";
}

/// <summary>
/// One immutable, inspectable Binding Plan for one binding scope.
/// </summary>
/// <remarks>
/// Every fact is fixed when negotiation succeeds and never changes. A change requires a new binding
/// scope with a new plan identifier: there is no renegotiation in place. Each fact is also
/// retrievable by name through <see cref="Fact"/> without invoking the provider, so a compiled
/// realization answers the same inspection questions as an explicit-data one.
/// </remarks>
public sealed record PortableBindingPlan
{
    private readonly ImmutableSortedDictionary<string, string> _facts;

    internal PortableBindingPlan(
        PortablePlanId planId,
        PortableContractDocument contract,
        ImmutableArray<PortableOperationReference> operations,
        ImmutableArray<PortableShapeReference> shapes,
        ImmutableArray<PortableFragmentReference> fragments,
        ImmutableArray<PortableDependencyReference> satisfiedRequirements,
        ImmutableArray<PortableCompactAssignment> compactIdentifierAssignments,
        string hostEndpoint,
        string providerEndpoint,
        string selectionReason,
        PortableRealization realization)
    {
        PlanId = planId;
        Contract = contract;
        Operations = operations;
        Shapes = shapes;
        Fragments = fragments;
        SatisfiedRequirements = satisfiedRequirements;
        CompactIdentifierAssignments = compactIdentifierAssignments;
        HostEndpoint = hostEndpoint;
        ProviderEndpoint = providerEndpoint;
        SelectionReason = selectionReason;
        Realization = realization;
        Catalog = PortableShapeCatalog.FromContract(contract);
        _facts = BuildFacts();
    }

    public PortablePlanId PlanId { get; }

    public PortableContractDocument Contract { get; }

    public PortableShapeCatalog Catalog { get; }

    public int ContractVersion => Contract.ContractVersion;

    public PortableComponentReference Component => Contract.Component;

    public PortableProviderReference Provider => Contract.Provider;

    public ImmutableArray<PortableOperationReference> Operations { get; }

    public ImmutableArray<PortableShapeReference> Shapes { get; }

    public ImmutableArray<PortableFragmentReference> Fragments { get; }

    public ImmutableArray<PortableDependencyReference> SatisfiedRequirements { get; }

    public ImmutableArray<PortableCompactAssignment> CompactIdentifierAssignments { get; }

    public string HostEndpoint { get; }

    public string ProviderEndpoint { get; }

    public PortableProviderReference SelectedProvider => Contract.Provider;

    public string SelectionReason { get; }

    public PortableAuthorityMode AuthorityPresentationMode => Contract.Authority.PresentationMode;

    public bool TrustBoundaryCrossed => Contract.Authority.TrustBoundaryCrossed;

    public bool NoCapabilityTransfer => Contract.Authority.NoCapabilityTransfer;

    public PortableAuthorityDecisionPoint AuthorityDecisionPoint =>
        AuthorityPresentationMode == PortableAuthorityMode.CrossTrustNoCapabilityTransfer
            ? PortableAuthorityDecisionPoint.ProviderDomain
            : PortableAuthorityDecisionPoint.TargetBoundary;

    /// <summary>
    /// Cross-trust revocability is carried-tier by default; a resolver or indirection point is a
    /// declared capability, never an assumed one.
    /// </summary>
    public string ChainConjunctionTier => "carried";

    public string Representation => Contract.Representation.Representation;

    public string Framing => Realization == PortableRealization.FixedDirectCall
        ? PortableRepresentations.DirectCall
        : Contract.Representation.Framing;

    public ImmutableArray<string> ResourceFlavors => Contract.Representation.ResourceFlavors;

    public ImmutableArray<string> AcceptedResourceHandles => Contract.Representation.AcceptedResourceHandles;

    public string ResourceOwnership => ResourceFlavors.IsEmpty
        ? "none"
        : ResourceFlavors.Contains(PortableResourceFlavors.CopiedImmutableBlob, StringComparer.Ordinal)
            ? "copied"
            : "provider-retained";

    public bool ImplicitCopyPermitted => false;

    public string? IntegrityAlgorithm =>
        ResourceFlavors.Contains(PortableResourceFlavors.CopiedImmutableBlob, StringComparer.Ordinal) ? "SHA-256" : null;

    public int MaxConcurrentRequests => Contract.Limits.MaxConcurrentRequests;

    public string OrderingGuarantee => "none";

    public PortableRealization Realization { get; }

    public ImmutableArray<string> CrossedBoundaries
    {
        get
        {
            var boundaries = ImmutableArray.CreateBuilder<string>();
            if (Realization == PortableRealization.NegotiatedProcess)
            {
                boundaries.Add("process");
            }

            if (TrustBoundaryCrossed)
            {
                boundaries.Add("trust");
            }

            return boundaries.Count == 0 ? ["none"] : boundaries.ToImmutable();
        }
    }

    public PortableLimits Limits => Contract.Limits;

    public bool ReplayProtectionDeclared => Contract.Lifecycle.ReplayProtectionDeclared;

    public string ReplayWindow => Contract.Lifecycle.ReplayWindow;

    public ImmutableSortedDictionary<string, bool> LifecycleFeatures => Contract.Lifecycle.Features;

    public ImmutableArray<string> TerminalStates { get; } = ["terminated", "failed"];

    public ImmutableArray<string> FailureDomainsObservable { get; } =
    [
        "local-endpoint", "transport", "remote-endpoint", "remote-provider", "unknown"
    ];

    public bool ExceptionTransportPermitted => false;

    public IEnumerable<string> FactNames => _facts.Keys;

    /// <summary>Retrieves one plan fact by name without invoking the provider.</summary>
    public string Fact(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _facts.TryGetValue(name, out var value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(name), $"The Binding Plan declares no fact named '{name}'.");
    }

    public IReadOnlyDictionary<string, string> Facts => _facts;

    public PortableOperationDeclaration Operation(PortableOperationReference reference) =>
        Contract.Operations.FirstOrDefault(operation => operation.Reference == reference) ??
        throw new PortableFaultException(
            PortableProtocolCategory.UnsupportedOperation,
            "unknown-operation",
            $"Operation {reference} is outside the established contract.");

    private ImmutableSortedDictionary<string, string> BuildFacts()
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        builder["planId"] = PlanId.Value;
        builder["contractVersion"] = ContractVersion.ToString(CultureInfo.InvariantCulture);
        builder["component"] = Component.ToString();
        builder["provider"] = Provider.ToString();
        builder["operations"] = Join(Operations.Select(operation => operation.ToString()));
        builder["shapes"] = Join(Shapes.Select(shape => shape.ToString()));
        builder["fragments"] = Join(Fragments.Select(fragment => fragment.ToString()));
        builder["satisfiedRequirements"] = Join(SatisfiedRequirements.Select(requirement => requirement.ToString()));
        builder["compactIdentifierAssignments"] = Join(CompactIdentifierAssignments
            .Select(assignment =>
                $"{IdentitySpaceToken(assignment.Space)}:{assignment.Reference}={assignment.CompactId}"));
        builder["hostEndpoint"] = HostEndpoint;
        builder["providerEndpoint"] = ProviderEndpoint;
        builder["selectedProvider"] = SelectedProvider.ToString();
        builder["selectionReason"] = SelectionReason;
        builder["authorityPresentationMode"] = AuthorityPresentationMode.Token();
        builder["trustBoundaryCrossed"] = Render(TrustBoundaryCrossed);
        builder["noCapabilityTransfer"] = Render(NoCapabilityTransfer);
        builder["constraintPolicy"] = Contract.Authority.ConstraintPolicy;
        builder["authorityDecisionPoint"] = AuthorityDecisionPoint.Token();
        builder["chainConjunctionTier"] = ChainConjunctionTier;
        builder["representation"] = Representation;
        builder["framing"] = Framing;
        builder["resourceFlavors"] = Join(ResourceFlavors);
        builder["acceptedResourceHandles"] = Join(AcceptedResourceHandles);
        builder["resourceOwnership"] = ResourceOwnership;
        builder["implicitCopyPermitted"] = Render(ImplicitCopyPermitted);
        builder["integrityAlgorithm"] = IntegrityAlgorithm ?? "absent";
        builder["maxConcurrentRequests"] = MaxConcurrentRequests.ToString(CultureInfo.InvariantCulture);
        builder["orderingGuarantee"] = OrderingGuarantee;
        builder["realization"] = Realization.Token();
        builder["crossedBoundaries"] = Join(CrossedBoundaries);
        builder["limits"] = Join(
        [
            $"maxFrameBytes={Limits.MaxFrameBytes}",
            $"maxNestingDepth={Limits.MaxNestingDepth}",
            $"maxRecordFields={Limits.MaxRecordFields}",
            $"maxFragmentsPerRecord={Limits.MaxFragmentsPerRecord}",
            $"maxSequenceItems={Limits.MaxSequenceItems}",
            $"maxTextBytes={Limits.MaxTextBytes}",
            $"maxByteStringBytes={Limits.MaxByteStringBytes}",
            $"maxResourceBytes={Limits.MaxResourceBytes}",
            $"ioTimeoutMilliseconds={Limits.IoTimeoutMilliseconds}",
            $"maxConcurrentRequests={Limits.MaxConcurrentRequests}"
        ]);
        builder["replayProtectionDeclared"] = Render(ReplayProtectionDeclared);
        builder["replayWindow"] = ReplayWindow;
        builder["lifecycleFeatures"] = Join(LifecycleFeatures.Select(feature => $"{feature.Key}={Render(feature.Value)}"));
        builder["terminalStates"] = Join(TerminalStates);
        builder["failureDomainsObservable"] = Join(FailureDomainsObservable);
        builder["exceptionTransportPermitted"] = Render(ExceptionTransportPermitted);
        return builder.ToImmutable();
    }

    private static string IdentitySpaceToken(PortableIdentitySpace space) => space switch
    {
        PortableIdentitySpace.Operation => "operation",
        PortableIdentitySpace.Shape => "shape",
        PortableIdentitySpace.Fragment => "fragment",
        _ => throw new ArgumentOutOfRangeException(nameof(space)),
    };

    private static string Join(IEnumerable<string> values) => string.Join(", ", values);

    private static string Render(bool value) => value ? "true" : "false";
}
