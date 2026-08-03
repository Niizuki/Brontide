using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Brontide.Reference.Studio;

public readonly record struct ProviderPublisherKeyId
{
    private ProviderPublisherKeyId(string value) => Value = value;

    public string Value { get; }

    public static ProviderPublisherKeyId Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!ContentAddressedProviderStore.IsDigest(value))
        {
            throw new ArgumentException("A publisher key identity must be an uppercase SHA-256 digest.", nameof(value));
        }

        return new(value);
    }

    public override string ToString() => Value;
}

public sealed record ProviderPublisherEvidence(
    ProviderPublisherKeyId PublisherKeyId,
    string Algorithm,
    string PublicKeySpkiBase64,
    string SignatureBase64);

public sealed record VerifiedProviderPublisherEvidence(
    ProviderArtifactSetId ContentIdentity,
    ProviderPublisherKeyId PublisherKeyId,
    string PayloadSha256);

public sealed record ProviderPublisherEvidenceResult
{
    private ProviderPublisherEvidenceResult(
        string code,
        string payloadSha256,
        ProviderPublisherKeyId? publisherKeyId,
        VerifiedProviderPublisherEvidence? verified)
    {
        Code = code;
        PayloadSha256 = payloadSha256;
        PublisherKeyId = publisherKeyId;
        Verified = verified;
    }

    public string Code { get; }

    public string PayloadSha256 { get; }

    public ProviderPublisherKeyId? PublisherKeyId { get; }

    public VerifiedProviderPublisherEvidence? Verified { get; }

    public bool IsVerified => Verified is not null;

    public string TrustCode => "publisher-trust-not-evaluated";

    public string AdmissionCode => "admission-not-attempted";

    internal static ProviderPublisherEvidenceResult Refused(
        string code,
        string payloadSha256,
        ProviderPublisherKeyId? publisherKeyId = null) => new(code, payloadSha256, publisherKeyId, null);

    internal static ProviderPublisherEvidenceResult Valid(VerifiedProviderPublisherEvidence verified) =>
        new("publisher-evidence-valid", verified.PayloadSha256, verified.PublisherKeyId, verified);
}

public static class ProviderArtifactPublisherManifest
{
    public static byte[] Encode(ProviderArtifactAcquisitionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        request = Snapshot(request);
        if (!TryValidate(request))
        {
            throw new ArgumentException("The acquisition request is not a canonical publisher manifest.", nameof(request));
        }

        using var output = new MemoryStream();
        Append(output, "CBI34");
        Append(output, request.Identity.Value);
        Append(output, request.Files.Count);
        foreach (var file in request.Files.OrderBy(file => file.RelativePath, StringComparer.Ordinal))
        {
            Append(output, file.RelativePath);
            Append(output, file.Sha256);
            Append(output, file.Length);
        }

        Append(output, request.ExecutablePath);
        Append(output, request.Arguments.Count);
        foreach (var argument in request.Arguments)
        {
            Append(output, argument);
        }

        return output.ToArray();
    }

    public static string Digest(ProviderArtifactAcquisitionRequest request) =>
        Convert.ToHexString(SHA256.HashData(Encode(request)));

    internal static bool TryValidate(ProviderArtifactAcquisitionRequest request)
    {
        if (request.Files is null
            || request.Arguments is null
            || request.Files.Count == 0
            || string.IsNullOrWhiteSpace(request.ExpectedSource.Value)
            || request.MaxTotalBytes <= 0
            || request.Arguments.Any(string.IsNullOrEmpty)
            || !IsSafeRelativePath(request.ExecutablePath)
            || request.Files.Any(file =>
                file is null
                || !IsSafeRelativePath(file.RelativePath)
                || !ContentAddressedProviderStore.IsDigest(file.Sha256)
                || file.Length < 0)
            || request.Files.Select(file => file.RelativePath).Distinct(StringComparer.Ordinal).Count()
                != request.Files.Count
            || !request.Files.Any(file => file.RelativePath == request.ExecutablePath))
        {
            return false;
        }

        long total = 0;
        foreach (var file in request.Files)
        {
            if (file.Length > request.MaxTotalBytes - total)
            {
                return false;
            }

            total += file.Length;
        }

        return ProviderArtifactSetIdentity.Compute(
            request.Files.Select(file => new ProviderArtifactFile(file.RelativePath, file.Sha256)),
            request.ExecutablePath,
            request.Arguments) == request.Identity;
    }

    internal static ProviderArtifactAcquisitionRequest Snapshot(ProviderArtifactAcquisitionRequest request)
    {
        if (request.Files is null || request.Arguments is null)
        {
            return request;
        }

        return request with
        {
            Files = Array.AsReadOnly(request.Files.ToArray()),
            Arguments = Array.AsReadOnly(request.Arguments.ToArray()),
        };
    }

    private static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathFullyQualified(path) || path.Contains('\\'))
        {
            return false;
        }

        return path.Split('/').All(segment => segment.Length > 0 && segment is not "." and not "..");
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

public static class ProviderArtifactPublisherEvidenceVerifier
{
    private const string Algorithm = "ECDSA-P256-SHA256";
    private const string P256Oid = "1.2.840.10045.3.1.7";

    public static ProviderPublisherEvidenceResult Verify(
        ProviderArtifactAcquisitionRequest request,
        ProviderPublisherEvidence? evidence)
    {
        ArgumentNullException.ThrowIfNull(request);
        request = ProviderArtifactPublisherManifest.Snapshot(request);
        if (!ProviderArtifactPublisherManifest.TryValidate(request))
        {
            return ProviderPublisherEvidenceResult.Refused("publisher-evidence-request-invalid", string.Empty);
        }

        var payload = ProviderArtifactPublisherManifest.Encode(request);
        var payloadDigest = Convert.ToHexString(SHA256.HashData(payload));
        if (evidence is null)
        {
            return ProviderPublisherEvidenceResult.Refused("publisher-evidence-not-provided", payloadDigest);
        }

        if (!string.Equals(evidence.Algorithm, Algorithm, StringComparison.Ordinal))
        {
            return ProviderPublisherEvidenceResult.Refused(
                "publisher-evidence-unsupported",
                payloadDigest,
                evidence.PublisherKeyId);
        }

        try
        {
            var publicKey = Convert.FromBase64String(evidence.PublicKeySpkiBase64);
            var signature = Convert.FromBase64String(evidence.SignatureBase64);
            var computedKeyId = ProviderPublisherKeyId.Create(Convert.ToHexString(SHA256.HashData(publicKey)));
            if (computedKeyId != evidence.PublisherKeyId)
            {
                return ProviderPublisherEvidenceResult.Refused(
                    "publisher-evidence-malformed",
                    payloadDigest,
                    evidence.PublisherKeyId);
            }

            using var verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(publicKey, out var bytesRead);
            var parameters = verifier.ExportParameters(includePrivateParameters: false);
            if (bytesRead != publicKey.Length
                || verifier.KeySize != 256
                || !string.Equals(parameters.Curve.Oid.Value, P256Oid, StringComparison.Ordinal))
            {
                return ProviderPublisherEvidenceResult.Refused(
                    "publisher-evidence-malformed",
                    payloadDigest,
                    evidence.PublisherKeyId);
            }

            if (!verifier.VerifyData(
                    payload,
                    signature,
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.Rfc3279DerSequence))
            {
                return ProviderPublisherEvidenceResult.Refused(
                    "publisher-evidence-invalid",
                    payloadDigest,
                    computedKeyId);
            }

            return ProviderPublisherEvidenceResult.Valid(new(
                request.Identity,
                computedKeyId,
                payloadDigest));
        }
        catch (Exception exception) when (
            exception is FormatException or CryptographicException or ArgumentException)
        {
            return ProviderPublisherEvidenceResult.Refused(
                "publisher-evidence-malformed",
                payloadDigest,
                evidence.PublisherKeyId);
        }
    }
}
