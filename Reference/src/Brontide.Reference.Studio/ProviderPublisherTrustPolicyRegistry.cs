using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Brontide.Reference.Studio;

public readonly record struct ProviderPublisherTrustPolicyAuthorityId
{
    private ProviderPublisherTrustPolicyAuthorityId(string value) => Value = value;
    public string Value { get; }

    public static ProviderPublisherTrustPolicyAuthorityId Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!ContentAddressedProviderStore.IsDigest(value))
        {
            throw new ArgumentException("A policy authority identity must be an uppercase SHA-256 digest.", nameof(value));
        }
        return new(value);
    }
}

public sealed record ProviderPublisherTrustPolicyUpdate(
    long Sequence,
    ProviderPublisherTrustPolicyId? PreviousPolicyIdentity,
    ProviderPublisherTrustPolicy Policy,
    string Algorithm,
    string AuthorityPublicKeySpkiBase64,
    string SignatureBase64);

public static class ProviderPublisherTrustPolicyUpdateManifest
{
    public static byte[] Encode(
        long sequence,
        ProviderPublisherTrustPolicyId? previousPolicyIdentity,
        ProviderPublisherTrustPolicyId policyIdentity)
    {
        if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
        using var output = new MemoryStream();
        Append(output, "CBI37");
        Append(output, sequence);
        Append(output, previousPolicyIdentity.HasValue ? 1 : 0);
        if (previousPolicyIdentity.HasValue) Append(output, previousPolicyIdentity.Value.Value);
        Append(output, policyIdentity.Value);
        return output.ToArray();
    }

    public static string Digest(
        long sequence,
        ProviderPublisherTrustPolicyId? previousPolicyIdentity,
        ProviderPublisherTrustPolicyId policyIdentity) =>
        Convert.ToHexString(SHA256.HashData(Encode(sequence, previousPolicyIdentity, policyIdentity)));

    private static void Append(Stream output, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        output.Write(bytes);
    }

    private static void Append(Stream output, long value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        output.Write(bytes);
    }

    private static void Append(Stream output, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Append(output, bytes.Length);
        output.Write(bytes);
    }
}

public sealed record VerifiedProviderPublisherTrustPolicySnapshot
{
    private VerifiedProviderPublisherTrustPolicySnapshot(
        ProviderPublisherTrustPolicyAuthorityId authorityIdentity,
        long sequence,
        ProviderPublisherTrustPolicy policy)
    {
        AuthorityIdentity = authorityIdentity;
        Sequence = sequence;
        Policy = policy;
    }

    public ProviderPublisherTrustPolicyAuthorityId AuthorityIdentity { get; }
    public long Sequence { get; }
    public ProviderPublisherTrustPolicy Policy { get; }

    internal static VerifiedProviderPublisherTrustPolicySnapshot Issue(
        ProviderPublisherTrustPolicyAuthorityId authorityIdentity,
        long sequence,
        ProviderPublisherTrustPolicy policy) => new(authorityIdentity, sequence, policy);
}

public sealed record ProviderPublisherTrustPolicyUpdateResult(
    string Code,
    VerifiedProviderPublisherTrustPolicySnapshot? Current)
{
    public bool IsApplied => Code == "policy-update-applied";
}

public sealed class ProviderPublisherTrustPolicyRegistry
{
    private readonly object _sync = new();
    private readonly ProviderPublisherTrustPolicyAuthorityId _authorityIdentity;
    private VerifiedProviderPublisherTrustPolicySnapshot? _current;

    public ProviderPublisherTrustPolicyRegistry(ProviderPublisherTrustPolicyAuthorityId authorityIdentity) =>
        _authorityIdentity = authorityIdentity;

    internal ProviderPublisherTrustPolicyAuthorityId AuthorityIdentity => _authorityIdentity;

    public VerifiedProviderPublisherTrustPolicySnapshot? Current
    {
        get { lock (_sync) return _current; }
    }

    public ProviderPublisherTrustPolicyUpdateResult Apply(ProviderPublisherTrustPolicyUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        lock (_sync)
        {
            var code = Validate(update, out var policy);
            if (code is not null) return new(code, _current);

            if (_current is null)
            {
                if (update.Sequence != 1) return new("policy-update-sequence-invalid", null);
                if (update.PreviousPolicyIdentity.HasValue) return new("policy-update-predecessor-mismatch", null);
            }
            else
            {
                if (update.Sequence != _current.Sequence + 1) return new("policy-update-sequence-invalid", _current);
                if (update.PreviousPolicyIdentity != _current.Policy.Identity)
                    return new("policy-update-predecessor-mismatch", _current);
            }

            _current = VerifiedProviderPublisherTrustPolicySnapshot.Issue(
                _authorityIdentity, update.Sequence, policy!);
            return new("policy-update-applied", _current);
        }
    }

    internal T WithCurrent<T>(Func<VerifiedProviderPublisherTrustPolicySnapshot?, T> action)
    {
        lock (_sync) return action(_current);
    }

    private string? Validate(
        ProviderPublisherTrustPolicyUpdate update,
        out ProviderPublisherTrustPolicy? policy)
    {
        policy = null;
        if (update.Sequence <= 0 || update.Policy is null
            || !ProviderPublisherTrustEvaluator.TrySnapshot(update.Policy, out var snapshot))
            return "policy-update-policy-invalid";
        policy = snapshot;
        if (update.Algorithm != "ECDSA-P256-SHA256") return "policy-update-unsupported";
        try
        {
            var publicKey = Convert.FromBase64String(update.AuthorityPublicKeySpkiBase64);
            var signature = Convert.FromBase64String(update.SignatureBase64);
            var identity = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(publicKey)));
            if (identity != _authorityIdentity) return "policy-update-authority-mismatch";
            using var verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(publicKey, out var bytesRead);
            var parameters = verifier.ExportParameters(false);
            if (bytesRead != publicKey.Length || verifier.KeySize != 256
                || parameters.Curve.Oid.Value != "1.2.840.10045.3.1.7")
                return "policy-update-malformed";
            return verifier.VerifyData(
                ProviderPublisherTrustPolicyUpdateManifest.Encode(
                    update.Sequence, update.PreviousPolicyIdentity, snapshot.Identity),
                signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                ? null : "policy-update-signature-invalid";
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException or ArgumentException)
        {
            return "policy-update-malformed";
        }
    }
}

public sealed class GovernedProviderArtifactAcquirer
{
    private readonly ProviderPublisherTrustPolicyRegistry _registry;
    private readonly TrustedProviderArtifactAcquirer _acquirer;

    public GovernedProviderArtifactAcquirer(
        ProviderPublisherTrustPolicyRegistry registry,
        TrustedProviderArtifactAcquirer acquirer)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(acquirer);
        _registry = registry;
        _acquirer = acquirer;
    }

    public TrustedProviderArtifactAcquisitionResult Acquire(
        ProviderArtifactAcquisitionRequest request,
        IProviderArtifactSource source,
        TrustedProviderPublisherAuthorization? authorization)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(source);
        return _registry.WithCurrent(current =>
        {
            if (current is null)
                return TrustedProviderArtifactAcquisitionResult.Refused(
                    "publisher-trust-policy-unavailable", "publisher-evidence-not-evaluated", request.ExpectedSource);
            if (authorization is not null && authorization.PolicyIdentity != current.Policy.Identity)
                return TrustedProviderArtifactAcquisitionResult.Refused(
                    "publisher-authorization-superseded", "publisher-evidence-valid", request.ExpectedSource,
                    authorization.PolicyIdentity, authorization.PublisherKeyId);
            return _acquirer.Acquire(request, source, authorization);
        });
    }
}
