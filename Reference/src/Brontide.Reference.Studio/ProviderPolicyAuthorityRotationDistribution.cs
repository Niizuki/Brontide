using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Brontide.Reference.Studio;

public sealed record ProviderPolicyAuthorityRotationDistributionRequest(
    string Challenge,
    long PolicySequence,
    ProviderPublisherTrustPolicyId? PolicyIdentity,
    long AuthorityGeneration,
    ProviderPublisherTrustPolicyAuthorityId ActiveAuthority);

public sealed record ProviderPolicyAuthorityRotationDistributionResponse(
    string Challenge,
    long PolicySequence,
    ProviderPublisherTrustPolicyId? PolicyIdentity,
    long AuthorityGeneration,
    ProviderPublisherTrustPolicyAuthorityId ActiveAuthority,
    long IssuedAtUnixSeconds,
    long ExpiresAtUnixSeconds,
    ProviderPolicyAuthorityRotationStatement? Rotation,
    string Algorithm,
    string EndpointPublicKeySpkiBase64,
    string SignatureBase64);

public interface IProviderPolicyAuthorityRotationDistributionSource
{
    Task<ProviderPolicyAuthorityRotationDistributionResponse> FetchAsync(
        ProviderPolicyAuthorityRotationDistributionRequest request,
        CancellationToken cancellationToken);
}

public sealed record ProviderPolicyAuthorityRotationDistributionResult(
    string Code,
    long Generation,
    ProviderPublisherTrustPolicyAuthorityId ActiveAuthority,
    ProviderPolicyAuthorityFloor Floor)
{
    public bool IsApplied => Code == "policy-authority-distribution-applied";
}

public static class ProviderPolicyAuthorityRotationDistributionManifest
{
    public static byte[] Encode(ProviderPolicyAuthorityRotationDistributionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        using var output = new MemoryStream();
        Append(output, "CBI58");
        Append(output, response.Challenge);
        Append(output, response.PolicySequence);
        AppendOptional(output, response.PolicyIdentity);
        Append(output, response.AuthorityGeneration);
        Append(output, response.ActiveAuthority.Value);
        Append(output, response.IssuedAtUnixSeconds);
        Append(output, response.ExpiresAtUnixSeconds);
        Append(output, response.Rotation is null ? 0 : 1);
        if (response.Rotation is not null) Append(output, RotationDigest(response.Rotation));
        return output.ToArray();
    }

    public static string RotationDigest(ProviderPolicyAuthorityRotationStatement rotation)
    {
        ArgumentNullException.ThrowIfNull(rotation);
        using var output = new MemoryStream();
        Append(output, "CBI58-ROTATION");
        Append(output, rotation.Generation);
        Append(output, rotation.PolicySequence);
        AppendOptional(output, rotation.PolicyIdentity);
        Append(output, rotation.PreviousAuthority.Value);
        Append(output, rotation.NextAuthority.Value);
        Append(output, rotation.Algorithm);
        Append(output, rotation.PreviousAuthorityPublicKeySpkiBase64);
        Append(output, rotation.NextAuthorityPublicKeySpkiBase64);
        Append(output, rotation.PreviousSignatureBase64);
        Append(output, rotation.NextSignatureBase64);
        return Convert.ToHexString(SHA256.HashData(output.ToArray()));
    }

    private static void AppendOptional(Stream output, ProviderPublisherTrustPolicyId? identity)
    {
        Append(output, identity.HasValue ? 1 : 0);
        if (identity.HasValue) Append(output, identity.Value.Value);
    }

    private static void Append(Stream output, int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        output.Write(bytes);
    }

    private static void Append(Stream output, long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        output.Write(bytes);
    }

    private static void Append(Stream output, string value)
    {
        var bytes = new UTF8Encoding(false, true).GetBytes(value);
        Append(output, bytes.Length);
        output.Write(bytes);
    }
}

public sealed class ProviderPolicyAuthorityRotationDistributionClient
{
    private const int MaximumCharacters = 1024 * 1024;
    private static readonly TimeSpan MaximumValidity = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaximumFutureSkew = TimeSpan.FromSeconds(30);
    private readonly DurableProviderPublisherTrustPolicyRegistry registry;
    private readonly ProviderPublisherTrustPolicyDistributionEndpointId endpointIdentity;

    public ProviderPolicyAuthorityRotationDistributionClient(
        DurableProviderPublisherTrustPolicyRegistry registry,
        ProviderPublisherTrustPolicyDistributionEndpointId endpointIdentity)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if (endpointIdentity.Value is null) throw new ArgumentException("An endpoint identity is required.", nameof(endpointIdentity));
        this.registry = registry;
        this.endpointIdentity = endpointIdentity;
    }

    public async Task<ProviderPolicyAuthorityRotationDistributionResult> SynchronizeAsync(
        IProviderPolicyAuthorityRotationDistributionSource source,
        DateTimeOffset now,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(timeout));
        var current = registry.Current;
        var request = new ProviderPolicyAuthorityRotationDistributionRequest(
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), current?.Sequence ?? 0,
            current?.Policy.Identity, registry.AuthorityGeneration, registry.ActiveAuthorityIdentity);
        ProviderPolicyAuthorityRotationDistributionResponse response;
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            response = await source.FetchAsync(request, timeoutSource.Token)
                .WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException) { return Result("policy-authority-distribution-timeout"); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { return Result("policy-authority-distribution-canceled"); }
        catch (OperationCanceledException) { return Result("policy-authority-distribution-timeout"); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { return Result("policy-authority-distribution-transport-failed"); }

        if (!IsBounded(response)) return Result("policy-authority-distribution-response-invalid");
        try
        {
            var publicKey = Convert.FromBase64String(response.EndpointPublicKeySpkiBase64);
            var supplied = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
                Convert.ToHexString(SHA256.HashData(publicKey)));
            if (supplied != endpointIdentity) return Result("policy-authority-distribution-endpoint-mismatch");
            using var verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(publicKey, out var bytesRead);
            var parameters = verifier.ExportParameters(false);
            if (bytesRead != publicKey.Length || verifier.KeySize != 256
                || parameters.Curve.Oid.Value != "1.2.840.10045.3.1.7")
                return Result("policy-authority-distribution-response-invalid");
            var signature = Convert.FromBase64String(response.SignatureBase64);
            if (!verifier.VerifyData(ProviderPolicyAuthorityRotationDistributionManifest.Encode(response),
                signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))
                return Result("policy-authority-distribution-signature-invalid");
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException
            or ArgumentException or NullReferenceException)
        { return Result("policy-authority-distribution-response-invalid"); }

        if (response.Challenge != request.Challenge)
            return Result("policy-authority-distribution-challenge-mismatch");
        if (response.PolicySequence != request.PolicySequence || response.PolicyIdentity != request.PolicyIdentity
            || response.AuthorityGeneration != request.AuthorityGeneration
            || response.ActiveAuthority != request.ActiveAuthority)
            return Result("policy-authority-distribution-cursor-mismatch");
        try
        {
            var issued = DateTimeOffset.FromUnixTimeSeconds(response.IssuedAtUnixSeconds);
            var expires = DateTimeOffset.FromUnixTimeSeconds(response.ExpiresAtUnixSeconds);
            if ((issued > now && issued - now > MaximumFutureSkew) || expires <= now
                || expires <= issued || expires - issued > MaximumValidity)
                return Result("policy-authority-distribution-stale");
        }
        catch (ArgumentOutOfRangeException) { return Result("policy-authority-distribution-stale"); }
        if (!CursorMatches(request)) return Result("policy-authority-distribution-superseded");
        if (response.Rotation is null) return Result("policy-authority-distribution-current");
        var applied = registry.Rotate(response.Rotation);
        return applied.IsApplied
            ? Result("policy-authority-distribution-applied")
            : Result(applied.Code);
    }

    private bool CursorMatches(ProviderPolicyAuthorityRotationDistributionRequest request)
    {
        var current = registry.Current;
        return request.PolicySequence == (current?.Sequence ?? 0)
            && request.PolicyIdentity == current?.Policy.Identity
            && request.AuthorityGeneration == registry.AuthorityGeneration
            && request.ActiveAuthority == registry.ActiveAuthorityIdentity;
    }

    private ProviderPolicyAuthorityRotationDistributionResult Result(string code) =>
        new(code, registry.AuthorityGeneration, registry.ActiveAuthorityIdentity, registry.AuthorityFloor);

    private static bool IsBounded(ProviderPolicyAuthorityRotationDistributionResponse? response)
    {
        if (response is null || response.Challenge is null || response.Algorithm != "ECDSA-P256-SHA256"
            || response.EndpointPublicKeySpkiBase64 is null || response.SignatureBase64 is null
            || response.ActiveAuthority.Value is null || response.Challenge.Length != 64
            || response.Challenge.Any(character => !Uri.IsHexDigit(character))) return false;
        long count = response.Challenge.Length + response.Algorithm.Length
            + response.EndpointPublicKeySpkiBase64.Length + response.SignatureBase64.Length
            + response.ActiveAuthority.Value.Length;
        if (response.Rotation is not null)
        {
            var rotation = response.Rotation;
            if (rotation.Algorithm is null || rotation.PreviousAuthorityPublicKeySpkiBase64 is null
                || rotation.NextAuthorityPublicKeySpkiBase64 is null || rotation.PreviousSignatureBase64 is null
                || rotation.NextSignatureBase64 is null) return false;
            count += rotation.Algorithm.Length + rotation.PreviousAuthorityPublicKeySpkiBase64.Length
                + rotation.NextAuthorityPublicKeySpkiBase64.Length + rotation.PreviousSignatureBase64.Length
                + rotation.NextSignatureBase64.Length;
        }
        return count <= MaximumCharacters;
    }
}
