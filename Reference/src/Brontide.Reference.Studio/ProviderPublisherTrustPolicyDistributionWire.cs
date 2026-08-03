using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Brontide.Reference.Studio;

public static class ProviderPublisherTrustPolicyDistributionWireCodec
{
    public const int MaximumMessageBytes = 1024 * 1024;
    private const int MaximumEntries = 4096;

    public static byte[] EncodeRequest(ProviderPublisherTrustPolicyDistributionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!ValidCursor(request.Challenge, request.CurrentSequence, request.CurrentPolicyIdentity))
            throw new ArgumentException("The distribution request cursor is invalid.", nameof(request));
        using var output = new MemoryStream();
        Write(output, "CBI40-REQUEST");
        Write(output, request.Challenge);
        Write(output, request.CurrentSequence);
        WriteOptional(output, request.CurrentPolicyIdentity);
        return Finish(output);
    }

    public static ProviderPublisherTrustPolicyDistributionRequest DecodeRequest(ReadOnlySpan<byte> bytes) =>
        Decode<ProviderPublisherTrustPolicyDistributionRequest>(bytes, (ref Reader reader) =>
        {
            if (reader.String() != "CBI40-REQUEST") throw new InvalidDataException();
            var request = new ProviderPublisherTrustPolicyDistributionRequest(
                reader.String(), reader.Int64(), reader.OptionalPolicyIdentity());
            if (!ValidCursor(request.Challenge, request.CurrentSequence, request.CurrentPolicyIdentity))
                throw new InvalidDataException();
            return request;
        });

    public static byte[] EncodeResponse(ProviderPublisherTrustPolicyDistributionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        using var output = new MemoryStream();
        Write(output, "CBI40-RESPONSE");
        Write(output, response.Challenge);
        Write(output, response.CurrentSequence);
        WriteOptional(output, response.CurrentPolicyIdentity);
        Write(output, response.IssuedAtUnixSeconds);
        Write(output, response.ExpiresAtUnixSeconds);
        Write(output, response.Update is null ? 0 : 1);
        if (response.Update is not null) WriteUpdate(output, response.Update);
        Write(output, response.Algorithm);
        Write(output, response.EndpointPublicKeySpkiBase64);
        Write(output, response.SignatureBase64);
        return Finish(output);
    }

    public static ProviderPublisherTrustPolicyDistributionResponse DecodeResponse(ReadOnlySpan<byte> bytes) =>
        Decode<ProviderPublisherTrustPolicyDistributionResponse>(bytes, (ref Reader reader) =>
        {
            if (reader.String() != "CBI40-RESPONSE") throw new InvalidDataException();
            var challenge = reader.String();
            var sequence = reader.Int64();
            var identity = reader.OptionalPolicyIdentity();
            var issued = reader.Int64();
            var expires = reader.Int64();
            var updatePresence = reader.Int32();
            if (updatePresence is not 0 and not 1) throw new InvalidDataException();
            var update = updatePresence == 1 ? reader.Update() : null;
            return new(challenge, sequence, identity, issued, expires, update,
                reader.String(), reader.String(), reader.String());
        });

    private static T Decode<T>(ReadOnlySpan<byte> bytes, ReaderAction<T> action)
    {
        if (bytes.Length == 0 || bytes.Length > MaximumMessageBytes) throw new InvalidDataException("The wire message size is invalid.");
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

    private static bool ValidCursor(string challenge, long sequence, ProviderPublisherTrustPolicyId? identity) =>
        ContentAddressedProviderStore.IsDigest(challenge)
        && sequence >= 0 && ((sequence == 0 && !identity.HasValue) || (sequence > 0 && identity.HasValue));

    private static byte[] Finish(MemoryStream output)
    {
        if (output.Length > MaximumMessageBytes) throw new InvalidDataException("The wire message is too large.");
        return output.ToArray();
    }

    private static void WriteUpdate(Stream output, ProviderPublisherTrustPolicyUpdate update)
    {
        if (update.Policy is null || update.Policy.Entries.Count > MaximumEntries) throw new InvalidDataException();
        Write(output, update.Sequence);
        WriteOptional(output, update.PreviousPolicyIdentity);
        Write(output, update.Policy.Identity.Value);
        var entries = update.Policy.Entries.OrderBy(entry => entry.PublisherKeyId.Value, StringComparer.Ordinal).ToArray();
        Write(output, entries.Length);
        foreach (var entry in entries)
        {
            Write(output, entry.PublisherKeyId.Value);
            Write(output, entry.Disposition == ProviderPublisherTrustDisposition.Admitted ? "admitted" : "revoked");
        }
        Write(output, update.Algorithm);
        Write(output, update.AuthorityPublicKeySpkiBase64);
        Write(output, update.SignatureBase64);
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
        internal ProviderPublisherTrustPolicyUpdate Update()
        {
            var sequence = Int64();
            var previous = OptionalPolicyIdentity();
            var identity = ProviderPublisherTrustPolicyId.Create(String());
            var count = Int32();
            if (count < 0 || count > MaximumEntries) throw new InvalidDataException();
            var entries = new ProviderPublisherTrustEntry[count];
            for (var index = 0; index < count; index++)
            {
                var key = ProviderPublisherKeyId.Create(String());
                entries[index] = new(key, String() switch
                {
                    "admitted" => ProviderPublisherTrustDisposition.Admitted,
                    "revoked" => ProviderPublisherTrustDisposition.Revoked,
                    _ => throw new InvalidDataException(),
                });
            }
            return new(sequence, previous, new(identity, entries), String(), String(), String());
        }
        private void Ensure(int length) { if (length > bytes.Length - offset) throw new InvalidDataException(); }
    }
}

public sealed class HttpProviderPublisherTrustPolicyDistributionSource : IProviderPublisherTrustPolicyDistributionSource
{
    public const string MediaType = "application/vnd.brontide.cbi40";
    private readonly HttpClient client;
    private readonly Uri endpoint;

    public HttpProviderPublisherTrustPolicyDistributionSource(HttpClient client, Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!endpoint.IsAbsoluteUri || endpoint.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new ArgumentException("An absolute HTTPS endpoint without user information or a fragment is required.", nameof(endpoint));
        this.client = client;
        this.endpoint = endpoint;
    }

    public async Task<ProviderPublisherTrustPolicyDistributionResponse> FetchAsync(
        ProviderPublisherTrustPolicyDistributionRequest request,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaType));
        message.Content = new ByteArrayContent(ProviderPublisherTrustPolicyDistributionWireCodec.EncodeRequest(request));
        message.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaType);
        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK
            || response.RequestMessage?.RequestUri != endpoint
            || response.Content.Headers.ContentType?.MediaType != MediaType
            || response.Content.Headers.ContentType.Parameters.Count > 0
            || response.Content.Headers.ContentEncoding.Count > 0)
            throw new InvalidDataException("The HTTP distribution response metadata is invalid.");
        if (response.Content.Headers.ContentLength > ProviderPublisherTrustPolicyDistributionWireCodec.MaximumMessageBytes)
            throw new InvalidDataException("The HTTP distribution response is too large.");
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (output.Length + read > ProviderPublisherTrustPolicyDistributionWireCodec.MaximumMessageBytes)
                throw new InvalidDataException("The HTTP distribution response is too large.");
            output.Write(buffer, 0, read);
        }
        return ProviderPublisherTrustPolicyDistributionWireCodec.DecodeResponse(output.ToArray());
    }
}
