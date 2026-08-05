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

/// <summary>
/// One CBI57 authority transition. It carries both signatures because the predecessor authorizes the
/// transition and the successor proves its own key exists; a statement is meaningless without both.
/// </summary>
public sealed record ProviderPolicyAuthorityRotationStatement(
    long Generation,
    long PolicySequence,
    ProviderPublisherTrustPolicyId? PolicyIdentity,
    ProviderPublisherTrustPolicyAuthorityId PreviousAuthority,
    ProviderPublisherTrustPolicyAuthorityId NextAuthority,
    string Algorithm,
    string PreviousAuthorityPublicKeySpkiBase64,
    string NextAuthorityPublicKeySpkiBase64,
    string PreviousSignatureBase64,
    string NextSignatureBase64);

public static class ProviderPolicyAuthorityRotationManifest
{
    public static byte[] Encode(
        long generation,
        long policySequence,
        ProviderPublisherTrustPolicyId? policyIdentity,
        ProviderPublisherTrustPolicyAuthorityId previousAuthority,
        ProviderPublisherTrustPolicyAuthorityId nextAuthority)
    {
        if (generation <= 0 || policySequence < 0)
            throw new ArgumentException("A valid policy authority transition is required.");
        using var output = new MemoryStream();
        Append(output, "CBI57");
        Append(output, generation);
        Append(output, policySequence);
        Append(output, policyIdentity.HasValue ? 1 : 0);
        if (policyIdentity.HasValue) Append(output, policyIdentity.Value.Value);
        Append(output, previousAuthority.Value);
        Append(output, nextAuthority.Value);
        return output.ToArray();
    }

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

public sealed record ProviderPolicyAuthorityRotationResult(
    string Code,
    long Generation,
    ProviderPublisherTrustPolicyAuthorityId ActiveAuthority)
{
    public bool IsApplied => Code == "policy-authority-rotation-applied";
}

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
    private ProviderPublisherTrustPolicyAuthorityId _activeAuthority;
    private long _authorityGeneration;
    private VerifiedProviderPublisherTrustPolicySnapshot? _current;

    public ProviderPublisherTrustPolicyRegistry(ProviderPublisherTrustPolicyAuthorityId authorityIdentity)
    {
        _authorityIdentity = authorityIdentity;
        _activeAuthority = authorityIdentity;
    }

    /// <summary>
    /// The out-of-band pin. It never moves, which is what lets a chain recorded against it stay
    /// comparable across an authority rotation.
    /// </summary>
    internal ProviderPublisherTrustPolicyAuthorityId AuthorityIdentity => _authorityIdentity;

    /// <summary>The authority that may sign the next policy update, which a rotation moves.</summary>
    public ProviderPublisherTrustPolicyAuthorityId ActiveAuthorityIdentity
    {
        get { lock (_sync) return _activeAuthority; }
    }

    public long AuthorityGeneration
    {
        get { lock (_sync) return _authorityGeneration; }
    }

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

            // The snapshot names the pin rather than the signing key: it is the trust root the policy
            // was verified under, and every downstream comparison of it must survive a rotation.
            _current = VerifiedProviderPublisherTrustPolicySnapshot.Issue(
                _authorityIdentity, update.Sequence, policy!);
            return new("policy-update-applied", _current);
        }
    }

    public ProviderPolicyAuthorityRotationResult Rotate(ProviderPolicyAuthorityRotationStatement statement)
    {
        ArgumentNullException.ThrowIfNull(statement);
        lock (_sync)
        {
            var code = ValidateRotation(statement);
            if (code is not null) return new(code, _authorityGeneration, _activeAuthority);
            _activeAuthority = statement.NextAuthority;
            _authorityGeneration = statement.Generation;
            return new("policy-authority-rotation-applied", _authorityGeneration, _activeAuthority);
        }
    }

    internal T WithCurrent<T>(Func<VerifiedProviderPublisherTrustPolicySnapshot?, T> action)
    {
        lock (_sync) return action(_current);
    }

    private string? ValidateRotation(ProviderPolicyAuthorityRotationStatement statement)
    {
        if (statement.Generation != _authorityGeneration + 1) return "policy-authority-generation-invalid";
        if (statement.PreviousAuthority != _activeAuthority) return "policy-authority-predecessor-mismatch";
        if (statement.NextAuthority == statement.PreviousAuthority) return "policy-authority-self-refused";
        if (statement.PolicySequence != (_current?.Sequence ?? 0)
            || statement.PolicyIdentity != _current?.Policy.Identity)
            return "policy-authority-chain-mismatch";
        if (statement.Algorithm != "ECDSA-P256-SHA256"
            || string.IsNullOrWhiteSpace(statement.PreviousAuthorityPublicKeySpkiBase64)
            || string.IsNullOrWhiteSpace(statement.NextAuthorityPublicKeySpkiBase64)
            || string.IsNullOrWhiteSpace(statement.PreviousSignatureBase64)
            || string.IsNullOrWhiteSpace(statement.NextSignatureBase64))
            return "policy-authority-evidence-invalid";
        try
        {
            var previousKey = Convert.FromBase64String(statement.PreviousAuthorityPublicKeySpkiBase64);
            var nextKey = Convert.FromBase64String(statement.NextAuthorityPublicKeySpkiBase64);
            if (Identify(previousKey) != statement.PreviousAuthority
                || Identify(nextKey) != statement.NextAuthority)
                return "policy-authority-key-mismatch";
            var manifest = ProviderPolicyAuthorityRotationManifest.Encode(
                statement.Generation, statement.PolicySequence, statement.PolicyIdentity,
                statement.PreviousAuthority, statement.NextAuthority);
            if (!TryVerify(previousKey, manifest, statement.PreviousSignatureBase64, out var previousValid)
                || !TryVerify(nextKey, manifest, statement.NextSignatureBase64, out var nextValid))
                return "policy-authority-evidence-invalid";
            if (!previousValid) return "policy-authority-signature-invalid";
            return nextValid ? null : "policy-authority-successor-unproven";
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException or ArgumentException)
        {
            return "policy-authority-evidence-invalid";
        }
    }

    private static ProviderPublisherTrustPolicyAuthorityId Identify(byte[] publicKey) =>
        ProviderPublisherTrustPolicyAuthorityId.Create(Convert.ToHexString(SHA256.HashData(publicKey)));

    private static bool TryVerify(byte[] publicKey, byte[] manifest, string signatureBase64, out bool valid)
    {
        valid = false;
        var signature = Convert.FromBase64String(signatureBase64);
        using var verifier = ECDsa.Create();
        verifier.ImportSubjectPublicKeyInfo(publicKey, out var bytesRead);
        var parameters = verifier.ExportParameters(false);
        if (bytesRead != publicKey.Length || verifier.KeySize != 256
            || parameters.Curve.Oid.Value != "1.2.840.10045.3.1.7")
            return false;
        valid = verifier.VerifyData(manifest, signature, HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);
        return true;
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
            if (Identify(publicKey) != _activeAuthority) return "policy-update-authority-mismatch";
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
