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
        bool revalidated,
        ProviderPublisherTrustPolicyId? acquisitionPolicyIdentity,
        ProviderPublisherTrustPolicyId? launchPolicyIdentity,
        ProviderPublisherTrustPolicyAuthorityId? policyAuthorityIdentity,
        VerifiedProviderPublisherEvidence? verifiedEvidence,
        StagedProviderProcess? provider,
        ProviderArtifactSetId? stagedIdentity,
        string? stagedExecutablePath)
    {
        Code = code;
        RefusedBy = refusedBy;
        Authorized = authorized;
        Staged = staged;
        Revalidated = revalidated;
        AcquisitionPolicyIdentity = acquisitionPolicyIdentity;
        LaunchPolicyIdentity = launchPolicyIdentity;
        PolicyAuthorityIdentity = policyAuthorityIdentity;
        VerifiedEvidence = verifiedEvidence;
        Provider = provider;
        StagedIdentity = stagedIdentity;
        StagedExecutablePath = stagedExecutablePath;
    }

    public string Code { get; }

    /// <summary>The slice that produced the refusal, so a composed failure keeps its origin.</summary>
    public string RefusedBy { get; }

    public bool Authorized { get; }
    public bool Staged { get; }

    /// <summary>The launch decision was taken against the policy in force and admitted the publisher.</summary>
    public bool Revalidated { get; }

    /// <summary>The policy that authorized acquisition, absent when trust was never evaluated.</summary>
    public ProviderPublisherTrustPolicyId? AcquisitionPolicyIdentity { get; }

    /// <summary>
    /// The policy the launch decision was taken against, absent when the chain never reached it.
    /// It may differ from the acquisition policy without refusing anything: what must still hold is
    /// the decision, not the snapshot that produced it.
    /// </summary>
    public ProviderPublisherTrustPolicyId? LaunchPolicyIdentity { get; }

    /// <summary>The authority whose policy governed both trust decisions.</summary>
    public ProviderPublisherTrustPolicyAuthorityId? PolicyAuthorityIdentity { get; }

    /// <summary>The evidence verified inside the chain and retained for later serving revalidation.</summary>
    public VerifiedProviderPublisherEvidence? VerifiedEvidence { get; }

    public StagedProviderProcess? Provider { get; }
    public ProviderArtifactSetId? StagedIdentity { get; }

    /// <summary>The verified path the launched provider actually ran from, inside the store.</summary>
    public string? StagedExecutablePath { get; }

    public bool IsLaunched => Provider is not null;

    internal static ProviderDistributionChainResult Refused(
        string code, string refusedBy, bool authorized = false, bool staged = false,
        bool revalidated = false,
        ProviderPublisherTrustPolicyId? acquisitionPolicyIdentity = null,
        ProviderPublisherTrustPolicyId? launchPolicyIdentity = null,
        ProviderPublisherTrustPolicyAuthorityId? policyAuthorityIdentity = null,
        VerifiedProviderPublisherEvidence? verifiedEvidence = null,
        ProviderArtifactSetId? stagedIdentity = null) =>
        new(code, refusedBy, authorized, staged, revalidated, acquisitionPolicyIdentity, launchPolicyIdentity,
            policyAuthorityIdentity, verifiedEvidence, null, stagedIdentity, null);

    internal static ProviderDistributionChainResult Launched(
        StagedProviderProcess provider,
        ProviderPublisherTrustPolicyId acquisitionPolicyIdentity,
        ProviderPublisherTrustPolicyId launchPolicyIdentity,
        ProviderPublisherTrustPolicyAuthorityId policyAuthorityIdentity,
        VerifiedProviderPublisherEvidence verifiedEvidence,
        ProviderArtifactSetId stagedIdentity,
        string executablePath) =>
        new("provider-launched", "none", true, true, true, acquisitionPolicyIdentity, launchPolicyIdentity,
            policyAuthorityIdentity, verifiedEvidence, provider, stagedIdentity, executablePath);

    internal static ProviderDistributionChainResult Restarted(
        ProviderDistributionChainResult prior,
        StagedProviderProcess provider,
        ProviderPublisherTrustPolicyId launchPolicyIdentity) =>
        new("provider-restarted", "none", true, true, true,
            prior.AcquisitionPolicyIdentity, launchPolicyIdentity,
            prior.PolicyAuthorityIdentity, prior.VerifiedEvidence, provider,
            prior.StagedIdentity, prior.StagedExecutablePath);
}

/// <summary>
/// Runs the distribution slices as one path: publisher evidence, host trust policy, governed
/// acquisition, content-addressed staging, a launch decision against the policy in force, and a
/// launched provider process holding a removal lease. It reclassifies no refusal — each one keeps
/// the code and the origin of the slice that made it.
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

        var authorizingPolicy = trust.Authorization.PolicyIdentity;
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
                authorized: true,
                acquisitionPolicyIdentity: authorizingPolicy);
        }

        // The launch is a second effect and takes its own decision rather than spending the one that
        // authorized acquisition. The registry advances and never clears, so a policy present before
        // acquisition is present now; what may have changed is what it says. Comparing the two policy
        // identities instead would refuse every update that left this publisher alone.
        var atLaunch = registry.Current ?? current;
        var relaunch = ProviderPublisherTrustEvaluator.Evaluate(atLaunch.Policy, evidence.Verified);
        if (relaunch.Authorization is null)
        {
            // Decided before the store touches the artifact, so a lapsed publisher is never reported
            // as whatever the staged bytes happened to look like. Removing them is residue hygiene
            // rather than a security act: they are content-addressed, and integrity is not what lapsed.
            store.Remove(acquired.Staged.Identity);
            return ProviderDistributionChainResult.Refused(
                relaunch.Code, "cbi35", authorized: true, staged: true,
                acquisitionPolicyIdentity: authorizingPolicy,
                launchPolicyIdentity: atLaunch.Policy.Identity);
        }

        // Activation re-verifies the staged bytes under their content address, so the executable that
        // runs is the one the publisher signed rather than a path the caller named.
        var activation = store.Activate(acquired.Staged, request.AllowedArguments);
        if (activation.Owner is null)
        {
            store.Remove(acquired.Staged.Identity);
            return ProviderDistributionChainResult.Refused(
                activation.Failure!.Code, "cbi31", authorized: true, staged: true, revalidated: true,
                acquisitionPolicyIdentity: authorizingPolicy,
                launchPolicyIdentity: relaunch.Authorization.PolicyIdentity);
        }

        return ProviderDistributionChainResult.Launched(
            activation.Owner,
            authorizingPolicy,
            relaunch.Authorization.PolicyIdentity,
            registry.AuthorityIdentity,
            evidence.Verified,
            acquired.Staged.Identity,
            Path.GetFullPath(Path.Combine(acquired.Staged.RootPath, acquired.Staged.ExecutablePath)));
    }
}
