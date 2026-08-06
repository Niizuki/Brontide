using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed class Cbi59Handler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Attempts { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Attempts++;
            return send(request, cancellationToken);
        }
    }

    private sealed class Cbi59UnknownLengthContent(byte[] bytes) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(bytes).AsTask();
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(new MemoryStream(bytes, false));
    }

    private sealed record Cbi59Observation(string Code, bool Rotation, int Attempts, string? Sha256 = null);

    private static ProviderPolicyAuthorityRotationDistributionRequest Cbi59Request() => new(
        new string('A', 64), 0, null, 0,
        ProviderPublisherTrustPolicyAuthorityId.Create(new string('A', 64)));

    private static ProviderPolicyAuthorityRotationStatement Cbi59Rotation() => new(
        1, 0, null,
        ProviderPublisherTrustPolicyAuthorityId.Create(new string('A', 64)),
        ProviderPublisherTrustPolicyAuthorityId.Create(new string('B', 64)),
        "ECDSA-P256-SHA256", "previous-key", "next-key", "previous-signature", "next-signature");

    private static ProviderPolicyAuthorityRotationDistributionResponse Cbi59Response(
        ProviderPolicyAuthorityRotationStatement? rotation = null) => new(
        new string('A', 64), 0, null, 0,
        ProviderPublisherTrustPolicyAuthorityId.Create(new string('A', 64)),
        1_800_000_000, 1_800_000_060, rotation, "ECDSA-P256-SHA256",
        "endpoint-key", "endpoint-signature");

    private static async Task<Cbi59Observation> Cbi59RunAsync(JsonElement vector)
    {
        var mutation = vector.GetProperty("mutation").GetString()!;
        if (mutation == "request")
        {
            var bytes = ProviderPolicyAuthorityRotationDistributionWireCodec.EncodeRequest(Cbi59Request());
            var decoded = ProviderPolicyAuthorityRotationDistributionWireCodec.DecodeRequest(bytes);
            return new(decoded == Cbi59Request() ? "request-roundtrip" : "wire-invalid", false, 0,
                Convert.ToHexString(SHA256.HashData(bytes)));
        }
        if (mutation is "current" or "rotation")
        {
            var bytes = ProviderPolicyAuthorityRotationDistributionWireCodec.EncodeResponse(
                Cbi59Response(mutation == "rotation" ? Cbi59Rotation() : null));
            var decoded = ProviderPolicyAuthorityRotationDistributionWireCodec.DecodeResponse(bytes);
            return new(decoded.Rotation is null ? "response-current" : "response-rotation",
                decoded.Rotation is not null, 0, Convert.ToHexString(SHA256.HashData(bytes)));
        }
        if (mutation is "truncated" or "trailing" or "marker" or "utf8")
        {
            var bytes = ProviderPolicyAuthorityRotationDistributionWireCodec.EncodeResponse(Cbi59Response());
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
                ProviderPolicyAuthorityRotationDistributionWireCodec.DecodeResponse(bytes);
                return new("wire-accepted", false, 0);
            }
            catch (InvalidDataException) { return new("wire-invalid", false, 0); }
        }

        var endpoint = new Uri("https://policy.example.test/v1/authority-rotation");
        var encoded = ProviderPolicyAuthorityRotationDistributionWireCodec.EncodeResponse(Cbi59Response());
        var handler = new Cbi59Handler(async (request, cancellationToken) =>
        {
            if (mutation == "canceled") await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            var requestBytes = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
            Assert.Multiple(() =>
            {
                Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(request.RequestUri, Is.EqualTo(endpoint));
                Assert.That(request.Content.Headers.ContentType?.MediaType,
                    Is.EqualTo(HttpProviderPolicyAuthorityRotationDistributionSource.MediaType));
                Assert.That(request.Headers.Accept.Single().MediaType,
                    Is.EqualTo(HttpProviderPolicyAuthorityRotationDistributionSource.MediaType));
                Assert.That(ProviderPolicyAuthorityRotationDistributionWireCodec.DecodeRequest(requestBytes),
                    Is.EqualTo(Cbi59Request()));
            });
            HttpContent content = mutation switch
            {
                "declared-oversize" => new ByteArrayContent(new byte[
                    ProviderPolicyAuthorityRotationDistributionWireCodec.MaximumMessageBytes + 1]),
                "streamed-oversize" => new Cbi59UnknownLengthContent(new byte[
                    ProviderPolicyAuthorityRotationDistributionWireCodec.MaximumMessageBytes + 1]),
                _ => new ByteArrayContent(encoded),
            };
            content.Headers.ContentType = new MediaTypeHeaderValue(
                mutation == "content-type" ? "application/octet-stream"
                    : HttpProviderPolicyAuthorityRotationDistributionSource.MediaType);
            if (mutation == "content-encoding") content.Headers.ContentEncoding.Add("gzip");
            return new HttpResponseMessage(mutation == "status" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Post,
                    mutation == "redirect" ? new Uri("https://other.example.test/v1/authority-rotation") : endpoint),
                Content = content,
            };
        });
        using var client = new HttpClient(handler);
        var source = new HttpProviderPolicyAuthorityRotationDistributionSource(client, endpoint);
        using var cancellation = new CancellationTokenSource();
        if (mutation == "canceled") cancellation.Cancel();
        try
        {
            var response = await source.FetchAsync(Cbi59Request(), cancellation.Token);
            return new("transport-success", response.Rotation is not null, handler.Attempts);
        }
        catch (OperationCanceledException) { return new("transport-canceled", false, handler.Attempts); }
        catch (InvalidDataException) { return new("transport-invalid", false, handler.Attempts); }
    }

    private static JsonDocument Cbi59Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi59-policy-authority-wire-vectors.json")));

    [Test]
    public async Task Shared_cbi59_vectors_encode_and_transport_only_strict_bounded_messages()
    {
        using var fixture = Cbi59Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi59RunAsync(vector);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()));
                Assert.That(actual.Rotation, Is.EqualTo(vector.GetProperty("rotation").GetBoolean()));
                Assert.That(actual.Attempts, Is.LessThanOrEqualTo(1));
                if (vector.TryGetProperty("sha256", out var digest))
                    Assert.That(actual.Sha256, Is.EqualTo(digest.GetString()));
            });
        }
    }

    [Test]
    public void Cbi59_C1_request_and_response_have_one_canonical_portable_encoding()
    {
        var request = ProviderPolicyAuthorityRotationDistributionWireCodec.EncodeRequest(Cbi59Request());
        var response = ProviderPolicyAuthorityRotationDistributionWireCodec.EncodeResponse(Cbi59Response(Cbi59Rotation()));
        Assert.That(ProviderPolicyAuthorityRotationDistributionWireCodec.EncodeRequest(
            ProviderPolicyAuthorityRotationDistributionWireCodec.DecodeRequest(request)), Is.EqualTo(request));
        Assert.That(ProviderPolicyAuthorityRotationDistributionWireCodec.EncodeResponse(
            ProviderPolicyAuthorityRotationDistributionWireCodec.DecodeResponse(response)), Is.EqualTo(response));
    }

    [Test]
    public async Task Cbi59_C2_decoding_is_strict_total_and_bounded()
    {
        using var fixture = Cbi59Fixture();
        foreach (var index in new[] { 3, 4, 5, 6 })
            Assert.That((await Cbi59RunAsync(fixture.RootElement.GetProperty("vectors")[index])).Code,
                Is.EqualTo("wire-invalid"));
    }

    [Test]
    public void Cbi59_C3_concrete_source_requires_one_exact_HTTPS_endpoint()
    {
        using var client = new HttpClient(new Cbi59Handler((_, _) => throw new AssertionException("not called")));
        Assert.Throws<ArgumentException>(() =>
            new HttpProviderPolicyAuthorityRotationDistributionSource(client, new Uri("http://policy.example.test")));
    }

    [Test]
    public async Task Cbi59_C4_declared_and_streamed_size_are_independently_bounded()
    {
        using var fixture = Cbi59Fixture();
        foreach (var index in new[] { 11, 13 })
            Assert.That((await Cbi59RunAsync(fixture.RootElement.GetProperty("vectors")[index])).Code,
                Is.EqualTo("transport-invalid"));
    }

    [Test]
    public async Task Cbi59_C5_cancellation_propagates_and_the_adapter_never_retries()
    {
        using var fixture = Cbi59Fixture();
        foreach (var index in new[] { 7, 8, 9, 10, 11, 12, 13, 14 })
            Assert.That((await Cbi59RunAsync(fixture.RootElement.GetProperty("vectors")[index])).Attempts,
                Is.EqualTo(1));
    }

    [Test]
    public async Task Cbi59_C6_HTTPS_source_composes_through_Cbi58_and_durable_Cbi57()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi59-{Guid.NewGuid():N}");
        try
        {
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var successor = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var endpointKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var authorityId = Cbi57Authority(authority);
            var endpointId = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
                Convert.ToHexString(SHA256.HashData(endpointKey.ExportSubjectPublicKeyInfo())));
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), authorityId).Registry!;
            var endpoint = new Uri("https://policy.example.test/v1/authority-rotation");
            var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
            var rotation = Cbi57Statement(1, 0, null, authority, successor, other: endpointKey);
            var handler = new Cbi59Handler(async (request, cancellationToken) =>
            {
                var decoded = ProviderPolicyAuthorityRotationDistributionWireCodec.DecodeRequest(
                    await request.Content!.ReadAsByteArrayAsync(cancellationToken));
                var response = Cbi58Respond("applied", decoded, endpointKey, rotation, now);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint),
                    Content = new ByteArrayContent(
                        ProviderPolicyAuthorityRotationDistributionWireCodec.EncodeResponse(response))
                    { Headers = { ContentType = new MediaTypeHeaderValue(
                        HttpProviderPolicyAuthorityRotationDistributionSource.MediaType) } },
                };
            });
            using var client = new HttpClient(handler);
            var source = new HttpProviderPolicyAuthorityRotationDistributionSource(client, endpoint);
            var result = await new ProviderPolicyAuthorityRotationDistributionClient(durable, endpointId)
                .SynchronizeAsync(source, now, TimeSpan.FromSeconds(1));
            Assert.Multiple(() =>
            {
                Assert.That(result.IsApplied, Is.True);
                Assert.That(result.Generation, Is.EqualTo(1));
                Assert.That(handler.Attempts, Is.EqualTo(1));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
