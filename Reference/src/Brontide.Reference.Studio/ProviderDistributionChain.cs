namespace Brontide.Reference.Studio;

public sealed record ProviderDistributionChainRequest(
    ProviderArtifactAcquisitionRequest Acquisition,
    ProviderPublisherEvidence? Evidence,
    IReadOnlyList<string> AllowedArguments);

public sealed record ProviderDistributionChainResult
{
    private ProviderDistributionChainResult(
        string code,
        string refusedBy,
        bool authorized,
        bool staged,
        StagedProviderProcess? provider,
        ProviderArtifactSetId? stagedIdentity,
        string? stagedExecutablePath)
    {
        Code = code;
        RefusedBy = refusedBy;
        Authorized = authorized;
        Staged = staged;
        Provider = provider;
        StagedIdentity = stagedIdentity;
        StagedExecutablePath = stagedExecutablePath;
    }

    public string Code { get; }

    /// <summary>The slice that produced the refusal, so a composed failure keeps its origin.</summary>
    public string RefusedBy { get; }

    public bool Authorized { get; }
    public bool Staged { get; }
    public StagedProviderProcess? Provider { get; }
    public ProviderArtifactSetId? StagedIdentity { get; }

    /// <summary>The verified path the launched provider actually ran from, inside the store.</summary>
    public string? StagedExecutablePath { get; }

    public bool IsLaunched => Provider is not null;

    internal static ProviderDistributionChainResult Refused(
        string code, string refusedBy, bool authorized = false, bool staged = false,
        ProviderArtifactSetId? stagedIdentity = null) =>
        new(code, refusedBy, authorized, staged, null, stagedIdentity, null);

    internal static ProviderDistributionChainResult Launched(
        StagedProviderProcess provider, ProviderArtifactSetId stagedIdentity, string executablePath) =>
        new("provider-launched", "none", true, true, provider, stagedIdentity, executablePath);
}

/// <summary>
/// Runs the distribution slices as one path: publisher evidence, host trust policy, governed
/// acquisition, content-addressed staging, and a launched provider process holding a removal lease.
/// It adds no capability of its own and reclassifies no refusal — each one keeps the code and the
/// origin of the slice that made it.
/// </summary>
public static class ProviderDistributionChain
{
    public static ProviderDistributionChainResult Run(
        DurableProviderPublisherTrustPolicyRegistry registry,
        ContentAddressedProviderStore store,
        string transactionRoot,
        ProviderDistributionChainRequest request,
        IProviderArtifactSource source)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionRoot);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(source);

        // A policy that never arrived authorizes nothing, and the chain stops before any evidence is
        // weighed or any source is opened.
        var current = registry.Current;
        if (current is null)
            return ProviderDistributionChainResult.Refused("publisher-trust-policy-unavailable", "cbi37");

        var evidence = ProviderArtifactPublisherEvidenceVerifier.Verify(request.Acquisition, request.Evidence);
        if (evidence.Verified is null)
            return ProviderDistributionChainResult.Refused(evidence.Code, "cbi34");

        // This gate is about attribution rather than protection: the governed acquirer refuses a null
        // authorization anyway, but it reports "trust required" where the policy actually said the
        // publisher key was revoked or unknown, and the chain owes a host the real reason.
        var trust = ProviderPublisherTrustEvaluator.Evaluate(current.Policy, evidence.Verified);
        if (trust.Authorization is null)
            return ProviderDistributionChainResult.Refused(trust.Code, "cbi35");

        var acquired = registry
            .Govern(new TrustedProviderArtifactAcquirer(new ProviderArtifactAcquirer(store, transactionRoot)))
            .Acquire(request.Acquisition, source, trust.Authorization);
        if (acquired.Staged is null)
        {
            // CBI33 keeps transport completion and local admission apart, and the chain preserves
            // that: delivery that completed and then failed its digest is CBI32 refusing admission,
            // not the transport failing.
            var delivered = acquired.TransportCode == "transport-completed";
            return ProviderDistributionChainResult.Refused(
                delivered ? acquired.AdmissionCode : acquired.TransportCode,
                delivered ? "cbi32" : "cbi33",
                authorized: true);
        }

        // Activation re-verifies the staged bytes under their content address, so the executable that
        // runs is the one the publisher signed rather than a path the caller named.
        var activation = store.Activate(acquired.Staged, request.AllowedArguments);
        if (activation.Owner is null)
        {
            store.Remove(acquired.Staged.Identity);
            return ProviderDistributionChainResult.Refused(
                activation.Failure!.Code, "cbi31", authorized: true, staged: true);
        }

        return ProviderDistributionChainResult.Launched(
            activation.Owner,
            acquired.Staged.Identity,
            Path.GetFullPath(Path.Combine(acquired.Staged.RootPath, acquired.Staged.ExecutablePath)));
    }
}
