using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Brontide.Reference.Studio;

public static class ProviderPolicyAuthorityRotationDistributionWireCodec
{
    public const int MaximumMessageBytes = 1024 * 1024;

    public static byte[] EncodeRequest(ProviderPolicyAuthorityRotationDistributionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!ValidCursor(request.Challenge, request.PolicySequence, request.PolicyIdentity,
                request.AuthorityGeneration, request.ActiveAuthority))
            throw new ArgumentException("The authority-rotation distribution request cursor is invalid.", nameof(request));
        using var output = new MemoryStream();
        Write(output, "CBI59-REQUEST");
        Write(output, request.Challenge);
        Write(output, request.PolicySequence);
        WriteOptional(output, request.PolicyIdentity);
        Write(output, request.AuthorityGeneration);
        Write(output, request.ActiveAuthority.Value);
        return Finish(output);
    }

    public static ProviderPolicyAuthorityRotationDistributionRequest DecodeRequest(ReadOnlySpan<byte> bytes) =>
        Decode<ProviderPolicyAuthorityRotationDistributionRequest>(bytes, (ref Reader reader) =>
        {
            if (reader.String() != "CBI59-REQUEST") throw new InvalidDataException();
            var request = new ProviderPolicyAuthorityRotationDistributionRequest(
                reader.String(), reader.Int64(), reader.OptionalPolicyIdentity(), reader.Int64(), reader.AuthorityIdentity());
            if (!ValidCursor(request.Challenge, request.PolicySequence, request.PolicyIdentity,
                    request.AuthorityGeneration, request.ActiveAuthority)) throw new InvalidDataException();
            return request;
        });

    public static byte[] EncodeResponse(ProviderPolicyAuthorityRotationDistributionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        using var output = new MemoryStream();
        Write(output, "CBI59-RESPONSE");
        Write(output, response.Challenge);
        Write(output, response.PolicySequence);
        WriteOptional(output, response.PolicyIdentity);
        Write(output, response.AuthorityGeneration);
        Write(output, response.ActiveAuthority.Value);
        Write(output, response.IssuedAtUnixSeconds);
        Write(output, response.ExpiresAtUnixSeconds);
        Write(output, response.Rotation is null ? 0 : 1);
        if (response.Rotation is not null) WriteRotation(output, response.Rotation);
        Write(output, response.Algorithm);
        Write(output, response.EndpointPublicKeySpkiBase64);
        Write(output, response.SignatureBase64);
        return Finish(output);
    }

    public static ProviderPolicyAuthorityRotationDistributionResponse DecodeResponse(ReadOnlySpan<byte> bytes) =>
        Decode<ProviderPolicyAuthorityRotationDistributionResponse>(bytes, (ref Reader reader) =>
        {
            if (reader.String() != "CBI59-RESPONSE") throw new InvalidDataException();
            var challenge = reader.String();
            var policySequence = reader.Int64();
            var policyIdentity = reader.OptionalPolicyIdentity();
            var generation = reader.Int64();
            var authority = reader.AuthorityIdentity();
            var issued = reader.Int64();
            var expires = reader.Int64();
            var presence = reader.Int32();
            if (presence is not 0 and not 1) throw new InvalidDataException();
            var rotation = presence == 1 ? reader.Rotation() : null;
            return new(challenge, policySequence, policyIdentity, generation, authority, issued, expires,
                rotation, reader.String(), reader.String(), reader.String());
        });

    private static T Decode<T>(ReadOnlySpan<byte> bytes, ReaderAction<T> action)
    {
        if (bytes.Length == 0 || bytes.Length > MaximumMessageBytes)
            throw new InvalidDataException("The wire message size is invalid.");
        try
        {
            var reader = new Reader(bytes);
            var value = action(ref reader);
            if (!reader.End) throw new InvalidDataException();
            return value;
        }
        catch (Exception exception) when (exception is ArgumentException or DecoderFallbackException
            or OverflowException or NullReferenceException)
        {
            throw new InvalidDataException("The wire message is malformed.", exception);
        }
    }

    private static bool ValidCursor(string challenge, long sequence, ProviderPublisherTrustPolicyId? policyIdentity,
        long generation, ProviderPublisherTrustPolicyAuthorityId authority) =>
        ContentAddressedProviderStore.IsDigest(challenge) && sequence >= 0 && generation >= 0
        && ContentAddressedProviderStore.IsDigest(authority.Value)
        && ((sequence == 0 && !policyIdentity.HasValue) || (sequence > 0 && policyIdentity.HasValue));

    private static byte[] Finish(MemoryStream output)
    {
        if (output.Length > MaximumMessageBytes) throw new InvalidDataException("The wire message is too large.");
        return output.ToArray();
    }

    private static void WriteRotation(Stream output, ProviderPolicyAuthorityRotationStatement rotation)
    {
        Write(output, rotation.Generation);
        Write(output, rotation.PolicySequence);
        WriteOptional(output, rotation.PolicyIdentity);
        Write(output, rotation.PreviousAuthority.Value);
        Write(output, rotation.NextAuthority.Value);
        Write(output, rotation.Algorithm);
        Write(output, rotation.PreviousAuthorityPublicKeySpkiBase64);
        Write(output, rotation.NextAuthorityPublicKeySpkiBase64);
        Write(output, rotation.PreviousSignatureBase64);
        Write(output, rotation.NextSignatureBase64);
    }

    private static void WriteOptional(Stream output, ProviderPublisherTrustPolicyId? identity)
    {
        Write(output, identity.HasValue ? 1 : 0);
        if (identity.HasValue) Write(output, identity.Value.Value);
    }

    private static void Write(Stream output, int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        output.Write(buffer);
    }

    private static void Write(Stream output, long value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(buffer, value);
        output.Write(buffer);
    }

    private static void Write(Stream output, string value)
    {
        var encoded = new UTF8Encoding(false, true).GetBytes(value);
        Write(output, encoded.Length);
        output.Write(encoded);
    }

    private delegate T ReaderAction<T>(ref Reader reader);

    private ref struct Reader
    {
        private readonly ReadOnlySpan<byte> bytes;
        private int offset;
        internal Reader(ReadOnlySpan<byte> bytes) { this.bytes = bytes; offset = 0; }
        internal bool End => offset == bytes.Length;
        internal int Int32() { Ensure(4); var value = BinaryPrimitives.ReadInt32BigEndian(bytes[offset..]); offset += 4; return value; }
        internal long Int64() { Ensure(8); var value = BinaryPrimitives.ReadInt64BigEndian(bytes[offset..]); offset += 8; return value; }
        internal string String()
        {
            var length = Int32();
            if (length < 0 || length > MaximumMessageBytes) throw new InvalidDataException();
            Ensure(length);
            var value = new UTF8Encoding(false, true).GetString(bytes.Slice(offset, length));
            offset += length;
            return value;
        }
        internal ProviderPublisherTrustPolicyId? OptionalPolicyIdentity()
        {
            var presence = Int32();
            if (presence is not 0 and not 1) throw new InvalidDataException();
            return presence == 1 ? ProviderPublisherTrustPolicyId.Create(String()) : null;
        }
        internal ProviderPublisherTrustPolicyAuthorityId AuthorityIdentity() =>
            ProviderPublisherTrustPolicyAuthorityId.Create(String());
        internal ProviderPolicyAuthorityRotationStatement Rotation() => new(
            Int64(), Int64(), OptionalPolicyIdentity(), AuthorityIdentity(), AuthorityIdentity(),
            String(), String(), String(), String(), String());
        private void Ensure(int length) { if (length > bytes.Length - offset) throw new InvalidDataException(); }
    }
}

public sealed class HttpProviderPolicyAuthorityRotationDistributionSource
    : IProviderPolicyAuthorityRotationDistributionSource
{
    public const string MediaType = "application/vnd.brontide.cbi59";
    private readonly HttpClient client;
    private readonly Uri endpoint;

    public HttpProviderPolicyAuthorityRotationDistributionSource(HttpClient client, Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!endpoint.IsAbsoluteUri || endpoint.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new ArgumentException("An absolute HTTPS endpoint without user information or a fragment is required.", nameof(endpoint));
        this.client = client;
        this.endpoint = endpoint;
    }

    public async Task<ProviderPolicyAuthorityRotationDistributionResponse> FetchAsync(
        ProviderPolicyAuthorityRotationDistributionRequest request, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaType));
        message.Content = new ByteArrayContent(ProviderPolicyAuthorityRotationDistributionWireCodec.EncodeRequest(request));
        message.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaType);
        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK || response.RequestMessage?.RequestUri != endpoint
            || response.Content.Headers.ContentType?.MediaType != MediaType
            || response.Content.Headers.ContentType.Parameters.Count > 0
            || response.Content.Headers.ContentEncoding.Count > 0)
            throw new InvalidDataException("The HTTP authority-rotation response metadata is invalid.");
        if (response.Content.Headers.ContentLength > ProviderPolicyAuthorityRotationDistributionWireCodec.MaximumMessageBytes)
            throw new InvalidDataException("The HTTP authority-rotation response is too large.");
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (output.Length + read > ProviderPolicyAuthorityRotationDistributionWireCodec.MaximumMessageBytes)
                throw new InvalidDataException("The HTTP authority-rotation response is too large.");
            output.Write(buffer, 0, read);
        }
        return ProviderPolicyAuthorityRotationDistributionWireCodec.DecodeResponse(output.ToArray());
    }
}
