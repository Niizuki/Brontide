using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Brontide.Reference.Studio;

public readonly record struct ProviderPublisherTrustPolicyId
{
    private ProviderPublisherTrustPolicyId(string value) => Value = value;

    public string Value { get; }

    public static ProviderPublisherTrustPolicyId Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!ContentAddressedProviderStore.IsDigest(value))
        {
            throw new ArgumentException("A publisher trust policy identity must be an uppercase SHA-256 digest.", nameof(value));
        }

        return new(value);
    }
}

public enum ProviderPublisherTrustDisposition
{
    Admitted,
    Revoked,
}

public sealed record ProviderPublisherTrustEntry(
    ProviderPublisherKeyId PublisherKeyId,
    ProviderPublisherTrustDisposition Disposition);

public sealed record ProviderPublisherTrustPolicy(
    ProviderPublisherTrustPolicyId Identity,
    IReadOnlyList<ProviderPublisherTrustEntry> Entries);

public static class ProviderPublisherTrustPolicyIdentity
{
    public static ProviderPublisherTrustPolicyId Compute(IEnumerable<ProviderPublisherTrustEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        using var output = new MemoryStream();
        Append(output, "CBI35");
        var ordered = entries.OrderBy(entry => entry.PublisherKeyId.Value, StringComparer.Ordinal).ToArray();
        Append(output, ordered.Length);
        foreach (var entry in ordered)
        {
            Append(output, entry.PublisherKeyId.Value);
            Append(output, entry.Disposition switch
            {
                ProviderPublisherTrustDisposition.Admitted => "admitted",
                ProviderPublisherTrustDisposition.Revoked => "revoked",
                _ => "invalid",
            });
        }

        return ProviderPublisherTrustPolicyId.Create(Convert.ToHexString(SHA256.HashData(output.ToArray())));
    }

    private static void Append(Stream output, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        output.Write(bytes);
    }

    private static void Append(Stream output, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Append(output, bytes.Length);
        output.Write(bytes);
    }
}

public sealed record TrustedProviderPublisherAuthorization
{
    private TrustedProviderPublisherAuthorization(
        ProviderPublisherTrustPolicyId policyIdentity,
        ProviderPublisherKeyId publisherKeyId,
        ProviderArtifactSetId contentIdentity,
        string payloadSha256)
    {
        PolicyIdentity = policyIdentity;
        PublisherKeyId = publisherKeyId;
        ContentIdentity = contentIdentity;
        PayloadSha256 = payloadSha256;
    }

    public ProviderPublisherTrustPolicyId PolicyIdentity { get; }
    public ProviderPublisherKeyId PublisherKeyId { get; }
    public ProviderArtifactSetId ContentIdentity { get; }
    public string PayloadSha256 { get; }

    internal static TrustedProviderPublisherAuthorization Issue(
        ProviderPublisherTrustPolicyId policyIdentity,
        ProviderPublisherKeyId publisherKeyId,
        ProviderArtifactSetId contentIdentity,
        string payloadSha256) => new(policyIdentity, publisherKeyId, contentIdentity, payloadSha256);
}

public sealed record ProviderPublisherTrustResult
{
    private ProviderPublisherTrustResult(
        string code,
        string evidenceCode,
        ProviderPublisherTrustPolicyId policyIdentity,
        ProviderPublisherKeyId? publisherKeyId,
        ProviderArtifactSetId? contentIdentity,
        TrustedProviderPublisherAuthorization? authorization)
    {
        Code = code;
        EvidenceCode = evidenceCode;
        PolicyIdentity = policyIdentity;
        PublisherKeyId = publisherKeyId;
        ContentIdentity = contentIdentity;
        Authorization = authorization;
    }

    public string Code { get; }
    public string EvidenceCode { get; }
    public ProviderPublisherTrustPolicyId PolicyIdentity { get; }
    public ProviderPublisherKeyId? PublisherKeyId { get; }
    public ProviderArtifactSetId? ContentIdentity { get; }
    public TrustedProviderPublisherAuthorization? Authorization { get; }
    public bool IsTrusted => Authorization is not null;
    public string AdmissionCode => "admission-not-attempted";

    internal static ProviderPublisherTrustResult Refused(
        string code,
        string evidenceCode,
        ProviderPublisherTrustPolicyId policyIdentity,
        ProviderPublisherKeyId? publisherKeyId = null,
        ProviderArtifactSetId? contentIdentity = null) =>
        new(code, evidenceCode, policyIdentity, publisherKeyId, contentIdentity, null);

    internal static ProviderPublisherTrustResult Trusted(TrustedProviderPublisherAuthorization authorization) =>
        new(
            "publisher-trusted",
            "publisher-evidence-valid",
            authorization.PolicyIdentity,
            authorization.PublisherKeyId,
            authorization.ContentIdentity,
            authorization);
}

public static class ProviderPublisherTrustEvaluator
{
    internal static bool TrySnapshot(
        ProviderPublisherTrustPolicy policy,
        out ProviderPublisherTrustPolicy snapshot)
    {
        var entries = policy.Entries?.ToArray();
        if (entries is null
            || entries.Length == 0
            || entries.Any(entry => entry is null
                || string.IsNullOrWhiteSpace(entry.PublisherKeyId.Value)
                || !Enum.IsDefined(entry.Disposition))
            || entries.Select(entry => entry.PublisherKeyId).Distinct().Count() != entries.Length
            || ProviderPublisherTrustPolicyIdentity.Compute(entries) != policy.Identity)
        {
            snapshot = policy;
            return false;
        }

        snapshot = policy with { Entries = Array.AsReadOnly(entries) };
        return true;
    }

    public static ProviderPublisherTrustResult Evaluate(
        ProviderPublisherTrustPolicy policy,
        VerifiedProviderPublisherEvidence? evidence)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var evidenceCode = evidence is null ? "publisher-evidence-not-verified" : "publisher-evidence-valid";
        if (!TrySnapshot(policy, out var snapshot))
        {
            return ProviderPublisherTrustResult.Refused(
                "publisher-trust-policy-invalid",
                evidenceCode,
                policy.Identity,
                evidence?.PublisherKeyId,
                evidence?.ContentIdentity);
        }

        if (evidence is null)
        {
            return ProviderPublisherTrustResult.Refused(
                "publisher-evidence-not-verified",
                evidenceCode,
                policy.Identity);
        }

        var entry = snapshot.Entries.SingleOrDefault(candidate => candidate.PublisherKeyId == evidence.PublisherKeyId);
        if (entry is null)
        {
            return ProviderPublisherTrustResult.Refused(
                "publisher-key-unknown",
                evidenceCode,
                policy.Identity,
                evidence.PublisherKeyId,
                evidence.ContentIdentity);
        }

        if (entry.Disposition == ProviderPublisherTrustDisposition.Revoked)
        {
            return ProviderPublisherTrustResult.Refused(
                "publisher-key-revoked",
                evidenceCode,
                policy.Identity,
                evidence.PublisherKeyId,
                evidence.ContentIdentity);
        }

        return ProviderPublisherTrustResult.Trusted(TrustedProviderPublisherAuthorization.Issue(
            policy.Identity,
            evidence.PublisherKeyId,
            evidence.ContentIdentity,
            evidence.PayloadSha256));
    }
}
