namespace Brontide.Reference.Studio;

public sealed record TrustedProviderArtifactAcquisitionResult
{
    private TrustedProviderArtifactAcquisitionResult(
        string trustCode,
        string evidenceCode,
        ProviderPublisherTrustPolicyId? policyIdentity,
        ProviderPublisherKeyId? publisherKeyId,
        ProviderArtifactSourceId sourceIdentity,
        string transportCode,
        string admissionCode,
        StagedProviderArtifactSet? staged)
    {
        TrustCode = trustCode;
        EvidenceCode = evidenceCode;
        PolicyIdentity = policyIdentity;
        PublisherKeyId = publisherKeyId;
        SourceIdentity = sourceIdentity;
        TransportCode = transportCode;
        AdmissionCode = admissionCode;
        Staged = staged;
    }

    public string TrustCode { get; }
    public string EvidenceCode { get; }
    public ProviderPublisherTrustPolicyId? PolicyIdentity { get; }
    public ProviderPublisherKeyId? PublisherKeyId { get; }
    public ProviderArtifactSourceId SourceIdentity { get; }
    public string TransportCode { get; }
    public string AdmissionCode { get; }
    public StagedProviderArtifactSet? Staged { get; }
    public bool IsStaged => Staged is not null;

    internal static TrustedProviderArtifactAcquisitionResult Refused(
        string trustCode,
        string evidenceCode,
        ProviderArtifactSourceId sourceIdentity,
        ProviderPublisherTrustPolicyId? policyIdentity = null,
        ProviderPublisherKeyId? publisherKeyId = null,
        string transportCode = "transport-not-attempted") =>
        new(trustCode, evidenceCode, policyIdentity, publisherKeyId, sourceIdentity,
            transportCode, "admission-not-attempted", null);

    internal static TrustedProviderArtifactAcquisitionResult FromAcquisition(
        TrustedProviderPublisherAuthorization authorization,
        ProviderArtifactAcquisitionResult acquisition) =>
        new("publisher-trusted", "publisher-evidence-valid", authorization.PolicyIdentity,
            authorization.PublisherKeyId, acquisition.SourceIdentity, acquisition.TransportCode,
            acquisition.AdmissionCode, acquisition.Staged);
}

public sealed class TrustedProviderArtifactAcquirer
{
    private readonly ProviderArtifactAcquirer _acquirer;

    public TrustedProviderArtifactAcquirer(ProviderArtifactAcquirer acquirer)
    {
        ArgumentNullException.ThrowIfNull(acquirer);
        _acquirer = acquirer;
    }

    public TrustedProviderArtifactAcquisitionResult Acquire(
        ProviderArtifactAcquisitionRequest request,
        IProviderArtifactSource source,
        TrustedProviderPublisherAuthorization? authorization)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(source);
        var snapshot = ProviderArtifactPublisherManifest.Snapshot(request);
        if (!ProviderArtifactPublisherManifest.TryValidate(snapshot))
        {
            return TrustedProviderArtifactAcquisitionResult.Refused(
                "publisher-trust-not-evaluated",
                "publisher-evidence-not-evaluated",
                request.ExpectedSource,
                transportCode: "acquisition-invalid");
        }

        if (authorization is null)
        {
            return TrustedProviderArtifactAcquisitionResult.Refused(
                "publisher-trust-required",
                "publisher-evidence-not-evaluated",
                snapshot.ExpectedSource);
        }

        if (authorization.ContentIdentity != snapshot.Identity)
        {
            return TrustedProviderArtifactAcquisitionResult.Refused(
                "publisher-authorization-content-mismatch",
                "publisher-evidence-valid",
                snapshot.ExpectedSource,
                authorization.PolicyIdentity,
                authorization.PublisherKeyId);
        }

        if (!string.Equals(
            authorization.PayloadSha256,
            ProviderArtifactPublisherManifest.Digest(snapshot),
            StringComparison.Ordinal))
        {
            return TrustedProviderArtifactAcquisitionResult.Refused(
                "publisher-authorization-payload-mismatch",
                "publisher-evidence-valid",
                snapshot.ExpectedSource,
                authorization.PolicyIdentity,
                authorization.PublisherKeyId);
        }

        return TrustedProviderArtifactAcquisitionResult.FromAcquisition(
            authorization,
            _acquirer.Acquire(snapshot, source));
    }
}
