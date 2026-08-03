using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed class Cbi40Handler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Attempts { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Attempts++;
            return send(request, cancellationToken);
        }
    }

    private sealed class Cbi40UnknownLengthContent(byte[] bytes) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(bytes).AsTask();
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(new MemoryStream(bytes, false));
    }

    private sealed record Cbi40Observation(string Code, bool Update, int Attempts, string? Sha256 = null);

    private static ProviderPublisherTrustPolicyDistributionRequest Cbi40Request() =>
        new(new string('A', 64), 0, null);

    private static ProviderPublisherTrustPolicyDistributionResponse Cbi40Response(
        ProviderPublisherTrustPolicyUpdate? update = null) =>
        new(new string('A', 64), 0, null, 1_800_000_000, 1_800_000_060, update,
            "ECDSA-P256-SHA256", "endpoint-key", "endpoint-signature");

    private static ProviderPublisherTrustPolicyUpdate Cbi40Update()
    {
        ProviderPublisherTrustEntry[] entries = [new(
            ProviderPublisherKeyId.Create(new string('A', 64)), ProviderPublisherTrustDisposition.Admitted)];
        return new(1, null, new(ProviderPublisherTrustPolicyIdentity.Compute(entries), entries),
            "ECDSA-P256-SHA256", "authority-key", "authority-signature");
    }

    private static async Task<Cbi40Observation> Cbi40RunAsync(JsonElement vector)
    {
        var mutation = vector.GetProperty("mutation").GetString()!;
        var update = Cbi40Update();
        if (mutation == "request")
        {
            var bytes = ProviderPublisherTrustPolicyDistributionWireCodec.EncodeRequest(Cbi40Request());
            var decoded = ProviderPublisherTrustPolicyDistributionWireCodec.DecodeRequest(bytes);
            return new(decoded == Cbi40Request() ? "request-roundtrip" : "wire-invalid", false, 0,
                Convert.ToHexString(SHA256.HashData(bytes)));
        }
        if (mutation is "current" or "update")
        {
            var bytes = ProviderPublisherTrustPolicyDistributionWireCodec.EncodeResponse(
                Cbi40Response(mutation == "update" ? update : null));
            var decoded = ProviderPublisherTrustPolicyDistributionWireCodec.DecodeResponse(bytes);
            return new(decoded.Update is null ? "response-current" : "response-update", decoded.Update is not null, 0,
                Convert.ToHexString(SHA256.HashData(bytes)));
        }
        if (mutation is "truncated" or "trailing" or "marker" or "utf8")
        {
            var bytes = ProviderPublisherTrustPolicyDistributionWireCodec.EncodeResponse(Cbi40Response());
            bytes = mutation switch
            {
                "truncated" => bytes[..(bytes.Length / 2)],
                "trailing" => [.. bytes, 0],
                _ => bytes,
            };
            if (mutation == "marker") bytes[4] ^= 1;
            if (mutation == "utf8") bytes[4] = 0xFF;
            try
            {
                ProviderPublisherTrustPolicyDistributionWireCodec.DecodeResponse(bytes);
                return new("wire-accepted", false, 0);
            }
            catch (InvalidDataException) { return new("wire-invalid", false, 0); }
        }

        var endpoint = new Uri("https://policy.example.test/v1/trust");
        var encoded = ProviderPublisherTrustPolicyDistributionWireCodec.EncodeResponse(Cbi40Response());
        var handler = new Cbi40Handler(async (request, cancellationToken) =>
        {
            if (mutation == "canceled") await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            var requestBytes = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
            Assert.Multiple(() =>
            {
                Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(request.RequestUri, Is.EqualTo(endpoint));
                Assert.That(request.Content.Headers.ContentType?.MediaType,
                    Is.EqualTo(HttpProviderPublisherTrustPolicyDistributionSource.MediaType));
                Assert.That(request.Headers.Accept.Single().MediaType,
                    Is.EqualTo(HttpProviderPublisherTrustPolicyDistributionSource.MediaType));
                Assert.That(ProviderPublisherTrustPolicyDistributionWireCodec.DecodeRequest(requestBytes),
                    Is.EqualTo(Cbi40Request()));
            });
            HttpContent content = mutation switch
            {
                "declared-oversize" => new ByteArrayContent(new byte[
                    ProviderPublisherTrustPolicyDistributionWireCodec.MaximumMessageBytes + 1]),
                "streamed-oversize" => new Cbi40UnknownLengthContent(new byte[
                    ProviderPublisherTrustPolicyDistributionWireCodec.MaximumMessageBytes + 1]),
                _ => new ByteArrayContent(encoded),
            };
            content.Headers.ContentType = new MediaTypeHeaderValue(
                mutation == "content-type" ? "application/octet-stream"
                    : HttpProviderPublisherTrustPolicyDistributionSource.MediaType);
            if (mutation == "content-encoding") content.Headers.ContentEncoding.Add("gzip");
            return new HttpResponseMessage(mutation == "status" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Post,
                    mutation == "redirect" ? new Uri("https://other.example.test/v1/trust") : endpoint),
                Content = content,
            };
        });
        using var client = new HttpClient(handler);
        var source = new HttpProviderPublisherTrustPolicyDistributionSource(client, endpoint);
        using var cancellation = new CancellationTokenSource();
        if (mutation == "canceled") cancellation.Cancel();
        try
        {
            var response = await source.FetchAsync(Cbi40Request(), cancellation.Token);
            return new("transport-success", response.Update is not null, handler.Attempts);
        }
        catch (OperationCanceledException) { return new("transport-canceled", false, handler.Attempts); }
        catch (InvalidDataException) { return new("transport-invalid", false, handler.Attempts); }
    }

    private static JsonDocument Cbi40Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi40-policy-distribution-wire-vectors.json")));

    [Test]
    public async Task Shared_cbi40_vectors_encode_and_transport_only_strict_bounded_messages()
    {
        using var fixture = Cbi40Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi40RunAsync(vector);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()));
                Assert.That(actual.Update, Is.EqualTo(vector.GetProperty("update").GetBoolean()));
                Assert.That(actual.Attempts, Is.LessThanOrEqualTo(1));
                if (vector.TryGetProperty("sha256", out var digest))
                    Assert.That(actual.Sha256, Is.EqualTo(digest.GetString()));
            });
        }
    }

    [Test]
    public void Cbi40_C1_request_codec_is_canonical_complete_and_strict()
    {
        var bytes = ProviderPublisherTrustPolicyDistributionWireCodec.EncodeRequest(Cbi40Request());
        Assert.That(ProviderPublisherTrustPolicyDistributionWireCodec.EncodeRequest(
            ProviderPublisherTrustPolicyDistributionWireCodec.DecodeRequest(bytes)), Is.EqualTo(bytes));
        Assert.Throws<InvalidDataException>(() =>
            ProviderPublisherTrustPolicyDistributionWireCodec.DecodeRequest([.. bytes, 0]));
    }

    [Test]
    public void Cbi40_C2_response_codec_preserves_the_complete_optional_update()
    {
        using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var update = Cbi37Sign(authority, 1, null, Cbi37Policy(false));
        var decoded = ProviderPublisherTrustPolicyDistributionWireCodec.DecodeResponse(
            ProviderPublisherTrustPolicyDistributionWireCodec.EncodeResponse(Cbi40Response(update)));
        Assert.That(ProviderPublisherTrustPolicyDistributionManifest.UpdateDigest(decoded.Update!),
            Is.EqualTo(ProviderPublisherTrustPolicyDistributionManifest.UpdateDigest(update)));
    }

    [Test]
    public async Task Cbi40_C3_malformed_truncated_and_trailing_messages_are_refused()
    {
        using var fixture = Cbi40Fixture();
        foreach (var index in new[] { 3, 4, 5, 6 })
            Assert.That((await Cbi40RunAsync(fixture.RootElement.GetProperty("vectors")[index])).Code,
                Is.EqualTo("wire-invalid"));
    }

    [Test]
    public void Cbi40_C4_transport_requires_an_exact_https_endpoint()
    {
        using var client = new HttpClient(new Cbi40Handler((_, _) => throw new AssertionException("not called")));
        Assert.Throws<ArgumentException>(() =>
            new HttpProviderPublisherTrustPolicyDistributionSource(client, new Uri("http://policy.example.test")));
    }

    [Test]
    public async Task Cbi40_C5_http_metadata_and_streaming_are_bounded_without_retry()
    {
        using var fixture = Cbi40Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors");
        foreach (var index in new[] { 7, 8, 9, 10, 11, 13, 14 })
            Assert.That((await Cbi40RunAsync(vectors[index])).Attempts, Is.EqualTo(1));
    }

    [Test]
    public async Task Cbi40_C6_https_source_composes_with_authenticated_durable_distribution()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi40-{Guid.NewGuid():N}");
        try
        {
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var endpointKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var authorityId = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo())));
            var endpointId = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
                Convert.ToHexString(SHA256.HashData(endpointKey.ExportSubjectPublicKeyInfo())));
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), authorityId).Registry!;
            var endpoint = new Uri("https://policy.example.test/v1/trust");
            var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
            var update = Cbi37Sign(authority, 1, null, Cbi37Policy(false));
            var handler = new Cbi40Handler(async (request, cancellationToken) =>
            {
                var decoded = ProviderPublisherTrustPolicyDistributionWireCodec.DecodeRequest(
                    await request.Content!.ReadAsByteArrayAsync(cancellationToken));
                var response = Cbi39Respond("update", decoded, endpointKey, update, now);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint),
                    Content = new ByteArrayContent(ProviderPublisherTrustPolicyDistributionWireCodec.EncodeResponse(response))
                    { Headers = { ContentType = new MediaTypeHeaderValue(HttpProviderPublisherTrustPolicyDistributionSource.MediaType) } },
                };
            });
            using var client = new HttpClient(handler);
            var source = new HttpProviderPublisherTrustPolicyDistributionSource(client, endpoint);
            var result = await new ProviderPublisherTrustPolicyDistributionClient(durable, endpointId)
                .SynchronizeAsync(source, now, TimeSpan.FromSeconds(1));
            Assert.Multiple(() =>
            {
                Assert.That(result.IsApplied, Is.True);
                Assert.That(result.Floor.Sequence, Is.EqualTo(1));
                Assert.That(handler.Attempts, Is.EqualTo(1));
                Assert.That(File.Exists(Path.Combine(root, "policy.checkpoint")), Is.True);
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
