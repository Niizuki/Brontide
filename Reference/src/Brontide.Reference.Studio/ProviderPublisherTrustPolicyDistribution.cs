using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Brontide.Reference.Studio;

public readonly record struct ProviderPublisherTrustPolicyDistributionEndpointId
{
    private ProviderPublisherTrustPolicyDistributionEndpointId(string value) => Value = value;
    public string Value { get; }

    public static ProviderPublisherTrustPolicyDistributionEndpointId Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!ContentAddressedProviderStore.IsDigest(value))
            throw new ArgumentException("A distribution endpoint identity must be an uppercase SHA-256 digest.", nameof(value));
        return new(value);
    }
}

public sealed record ProviderPublisherTrustPolicyDistributionRequest(
    string Challenge,
    long CurrentSequence,
    ProviderPublisherTrustPolicyId? CurrentPolicyIdentity);

public sealed record ProviderPublisherTrustPolicyDistributionResponse(
    string Challenge,
    long CurrentSequence,
    ProviderPublisherTrustPolicyId? CurrentPolicyIdentity,
    long IssuedAtUnixSeconds,
    long ExpiresAtUnixSeconds,
    ProviderPublisherTrustPolicyUpdate? Update,
    string Algorithm,
    string EndpointPublicKeySpkiBase64,
    string SignatureBase64);

public interface IProviderPublisherTrustPolicyDistributionSource
{
    Task<ProviderPublisherTrustPolicyDistributionResponse> FetchAsync(
        ProviderPublisherTrustPolicyDistributionRequest request,
        CancellationToken cancellationToken);
}

public sealed record ProviderPublisherTrustPolicyDistributionResult(
    string Code,
    VerifiedProviderPublisherTrustPolicySnapshot? Current,
    ProviderPublisherTrustPolicyRecoveryFloor Floor)
{
    public bool IsApplied => Code == "policy-distribution-applied";
}

public static class ProviderPublisherTrustPolicyDistributionManifest
{
    public static byte[] Encode(
        string challenge,
        long currentSequence,
        ProviderPublisherTrustPolicyId? currentPolicyIdentity,
        long issuedAtUnixSeconds,
        long expiresAtUnixSeconds,
        ProviderPublisherTrustPolicyUpdate? update)
    {
        using var output = new MemoryStream();
        Append(output, "CBI39");
        Append(output, challenge);
        Append(output, currentSequence);
        Append(output, currentPolicyIdentity.HasValue ? 1 : 0);
        if (currentPolicyIdentity.HasValue) Append(output, currentPolicyIdentity.Value.Value);
        Append(output, issuedAtUnixSeconds);
        Append(output, expiresAtUnixSeconds);
        Append(output, update is null ? 0 : 1);
        if (update is not null) Append(output, UpdateDigest(update));
        return output.ToArray();
    }

    public static string UpdateDigest(ProviderPublisherTrustPolicyUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        using var output = new MemoryStream();
        Append(output, "CBI39-UPDATE");
        Append(output, update.Sequence);
        Append(output, update.PreviousPolicyIdentity.HasValue ? 1 : 0);
        if (update.PreviousPolicyIdentity.HasValue) Append(output, update.PreviousPolicyIdentity.Value.Value);
        Append(output, update.Policy.Identity.Value);
        var entries = update.Policy.Entries.OrderBy(entry => entry.PublisherKeyId.Value, StringComparer.Ordinal).ToArray();
        Append(output, entries.Length);
        foreach (var entry in entries)
        {
            Append(output, entry.PublisherKeyId.Value);
            Append(output, entry.Disposition == ProviderPublisherTrustDisposition.Admitted ? "admitted" : "revoked");
        }
        Append(output, update.Algorithm);
        Append(output, update.AuthorityPublicKeySpkiBase64);
        Append(output, update.SignatureBase64);
        return Convert.ToHexString(SHA256.HashData(output.ToArray()));
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
        var bytes = Encoding.UTF8.GetBytes(value);
        Append(output, bytes.Length);
        output.Write(bytes);
    }
}

public sealed class ProviderPublisherTrustPolicyDistributionClient
{
    private const int MaxEncodedCharacters = 1024 * 1024;
    private static readonly TimeSpan MaximumValidity = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaximumFutureSkew = TimeSpan.FromSeconds(30);
    private readonly DurableProviderPublisherTrustPolicyRegistry registry;
    private readonly ProviderPublisherTrustPolicyDistributionEndpointId endpointIdentity;

    public ProviderPublisherTrustPolicyDistributionClient(
        DurableProviderPublisherTrustPolicyRegistry registry,
        ProviderPublisherTrustPolicyDistributionEndpointId endpointIdentity)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if (endpointIdentity.Value is null) throw new ArgumentException("An endpoint identity is required.", nameof(endpointIdentity));
        this.registry = registry;
        this.endpointIdentity = endpointIdentity;
    }

    public async Task<ProviderPublisherTrustPolicyDistributionResult> SynchronizeAsync(
        IProviderPublisherTrustPolicyDistributionSource source,
        DateTimeOffset now,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(timeout));

        var current = registry.Current;
        var request = new ProviderPublisherTrustPolicyDistributionRequest(
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            current?.Sequence ?? 0,
            current?.Policy.Identity);
        ProviderPublisherTrustPolicyDistributionResponse response;
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            response = await source.FetchAsync(request, timeoutSource.Token)
                .WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return Result("policy-distribution-timeout");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result("policy-distribution-canceled");
        }
        catch (OperationCanceledException)
        {
            return Result("policy-distribution-timeout");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return Result("policy-distribution-transport-failed");
        }

        if (!IsBounded(response)) return Result("policy-distribution-response-invalid");
        byte[] publicKey;
        byte[] signature;
        try
        {
            publicKey = Convert.FromBase64String(response.EndpointPublicKeySpkiBase64);
            signature = Convert.FromBase64String(response.SignatureBase64);
            var suppliedIdentity = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
                Convert.ToHexString(SHA256.HashData(publicKey)));
            if (suppliedIdentity != endpointIdentity) return Result("policy-distribution-endpoint-mismatch");
            using var verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(publicKey, out var bytesRead);
            var parameters = verifier.ExportParameters(false);
            if (bytesRead != publicKey.Length || verifier.KeySize != 256
                || parameters.Curve.Oid.Value != "1.2.840.10045.3.1.7")
                return Result("policy-distribution-response-invalid");
            if (!verifier.VerifyData(ProviderPublisherTrustPolicyDistributionManifest.Encode(
                    response.Challenge, response.CurrentSequence, response.CurrentPolicyIdentity,
                    response.IssuedAtUnixSeconds, response.ExpiresAtUnixSeconds, response.Update),
                signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))
                return Result("policy-distribution-signature-invalid");
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException
            or ArgumentException or NullReferenceException)
        {
            return Result("policy-distribution-response-invalid");
        }

        if (response.Challenge != request.Challenge) return Result("policy-distribution-challenge-mismatch");
        if (response.CurrentSequence != request.CurrentSequence
            || response.CurrentPolicyIdentity != request.CurrentPolicyIdentity)
            return Result("policy-distribution-cursor-mismatch");
        try
        {
            var issued = DateTimeOffset.FromUnixTimeSeconds(response.IssuedAtUnixSeconds);
            var expires = DateTimeOffset.FromUnixTimeSeconds(response.ExpiresAtUnixSeconds);
            if ((issued > now && issued - now > MaximumFutureSkew)
                || expires <= now || expires <= issued || expires - issued > MaximumValidity)
                return Result("policy-distribution-stale");
        }
        catch (ArgumentOutOfRangeException)
        {
            return Result("policy-distribution-stale");
        }
        if (!CursorMatches(request)) return Result("policy-distribution-superseded");
        if (response.Update is null) return Result("policy-distribution-current");

        var applied = registry.Apply(response.Update);
        return new(applied.IsApplied ? "policy-distribution-applied" : applied.Code, applied.Current, applied.Floor);
    }

    private bool CursorMatches(ProviderPublisherTrustPolicyDistributionRequest request)
    {
        var current = registry.Current;
        return (current?.Sequence ?? 0) == request.CurrentSequence
            && current?.Policy.Identity == request.CurrentPolicyIdentity;
    }

    private ProviderPublisherTrustPolicyDistributionResult Result(string code) =>
        new(code, registry.Current, registry.Floor);

    private static bool IsBounded(ProviderPublisherTrustPolicyDistributionResponse? response)
    {
        if (response is null || response.Challenge is null || response.Algorithm != "ECDSA-P256-SHA256"
            || response.EndpointPublicKeySpkiBase64 is null || response.SignatureBase64 is null
            || response.Challenge.Length != 64 || response.Challenge.Any(character => !Uri.IsHexDigit(character)))
            return false;
        var update = response.Update;
        if (update is not null && (update.Policy is null || update.Policy.Entries.Count > 4096
            || update.Algorithm is null || update.AuthorityPublicKeySpkiBase64 is null || update.SignatureBase64 is null))
            return false;
        long characters = (long)response.Challenge.Length + response.Algorithm.Length
            + response.EndpointPublicKeySpkiBase64.Length + response.SignatureBase64.Length;
        if (update is not null)
        {
            characters += update.Algorithm.Length + update.AuthorityPublicKeySpkiBase64.Length + update.SignatureBase64.Length;
            characters += update.Policy.Entries.Sum(entry => (long)entry.PublisherKeyId.Value.Length + 8);
        }
        return characters <= MaxEncodedCharacters;
    }
}
