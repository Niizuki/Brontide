using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed class Cbi58Source(
        Func<ProviderPolicyAuthorityRotationDistributionRequest, CancellationToken,
            Task<ProviderPolicyAuthorityRotationDistributionResponse>> fetch)
        : IProviderPolicyAuthorityRotationDistributionSource
    {
        public int Attempts { get; private set; }
        public Task<ProviderPolicyAuthorityRotationDistributionResponse> FetchAsync(
            ProviderPolicyAuthorityRotationDistributionRequest request, CancellationToken cancellationToken)
        {
            Attempts++;
            return fetch(request, cancellationToken);
        }
    }

    private sealed record Cbi58Observation(string Code, long Generation, int Attempts);

    private static ProviderPolicyAuthorityRotationDistributionResponse Cbi58Respond(
        string mutation,
        ProviderPolicyAuthorityRotationDistributionRequest request,
        ECDsa endpoint,
        ProviderPolicyAuthorityRotationStatement statement,
        DateTimeOffset now)
    {
        var issued = mutation == "expired" ? now.AddMinutes(-2) : now;
        var response = new ProviderPolicyAuthorityRotationDistributionResponse(
            mutation == "challenge" ? new string('0', 64) : request.Challenge,
            request.PolicySequence,
            request.PolicyIdentity,
            mutation == "cursor" ? request.AuthorityGeneration + 1 : request.AuthorityGeneration,
            request.ActiveAuthority,
            issued.ToUnixTimeSeconds(),
            (mutation == "expired" ? now.AddMinutes(-1) : now.AddMinutes(1)).ToUnixTimeSeconds(),
            mutation == "current" ? null : statement,
            "ECDSA-P256-SHA256",
            Convert.ToBase64String(endpoint.ExportSubjectPublicKeyInfo()),
            string.Empty);
        response = response with
        {
            SignatureBase64 = Convert.ToBase64String(endpoint.SignData(
                ProviderPolicyAuthorityRotationDistributionManifest.Encode(response),
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))
        };
        if (mutation == "signature")
        {
            var changed = Convert.FromBase64String(response.SignatureBase64);
            changed[^1] ^= 1;
            response = response with { SignatureBase64 = Convert.ToBase64String(changed) };
        }
        return response;
    }

    private static async Task<Cbi58Observation> Cbi58RunAsync(JsonElement vector)
    {
        var mutation = vector.GetProperty("mutation").GetString()!;
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi58-{Guid.NewGuid():N}");
        try
        {
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var successor = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var endpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var otherEndpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), Cbi57Authority(authority)).Registry!;
            var statement = Cbi57Statement(mutation == "native" ? 2 : 1, 0, null,
                authority, successor, other: otherEndpoint);
            var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
            var source = new Cbi58Source((request, _) => Task.FromResult(Cbi58Respond(
                mutation, request, mutation == "endpoint" ? otherEndpoint : endpoint, statement, now)));
            var endpointId = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
                Convert.ToHexString(SHA256.HashData(endpoint.ExportSubjectPublicKeyInfo())));
            var result = await new ProviderPolicyAuthorityRotationDistributionClient(durable, endpointId)
                .SynchronizeAsync(source, now, TimeSpan.FromSeconds(1));
            var recovered = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), Cbi57Authority(authority),
                authorityFloor: result.Floor).Registry!;
            return new(result.Code, recovered.AuthorityGeneration, source.Attempts);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static JsonDocument Cbi58Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi58-policy-authority-distribution-vectors.json")));

    [Test]
    public async Task Shared_cbi58_vectors_deliver_authority_rotation()
    {
        using var fixture = Cbi58Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi58RunAsync(vector);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()));
                Assert.That(actual.Generation, Is.EqualTo(vector.GetProperty("generation").GetInt64()));
                Assert.That(actual.Attempts, Is.EqualTo(1));
            });
        }
    }

    [Test]
    public async Task Cbi58_C1_every_attempt_names_the_exact_durable_authority_cursor()
    {
        using var fixture = Cbi58Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors");
        Assert.That((await Cbi58RunAsync(vectors[5])).Code,
            Is.EqualTo("policy-authority-distribution-cursor-mismatch"));
    }

    [Test]
    public async Task Cbi58_C2_the_active_distribution_endpoint_authenticates_the_whole_response()
    {
        using var fixture = Cbi58Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors");
        Assert.That((await Cbi58RunAsync(vectors[2])).Code,
            Is.EqualTo("policy-authority-distribution-endpoint-mismatch"));
        Assert.That((await Cbi58RunAsync(vectors[3])).Code,
            Is.EqualTo("policy-authority-distribution-signature-invalid"));
    }

    [Test]
    public async Task Cbi58_C3_challenge_freshness_and_concurrent_movement_fail_closed()
    {
        using var fixture = Cbi58Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors");
        Assert.That((await Cbi58RunAsync(vectors[4])).Code,
            Is.EqualTo("policy-authority-distribution-challenge-mismatch"));
        Assert.That((await Cbi58RunAsync(vectors[6])).Code,
            Is.EqualTo("policy-authority-distribution-stale"));
    }

    [Test]
    public async Task Cbi58_C4_only_Cbi57_decides_whether_a_delivered_statement_applies()
    {
        using var fixture = Cbi58Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors");
        Assert.That((await Cbi58RunAsync(vectors[1])).Code,
            Is.EqualTo("policy-authority-distribution-applied"));
        Assert.That((await Cbi58RunAsync(vectors[7])).Code,
            Is.EqualTo("policy-authority-generation-invalid"));
    }

    [Test]
    public async Task Cbi58_C5_bounds_transport_timeout_and_cancellation()
    {
        using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var endpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi58-bounds-{Guid.NewGuid():N}");
        try
        {
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), Cbi57Authority(authority)).Registry!;
            using var successor = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var endpointId = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
                Convert.ToHexString(SHA256.HashData(endpoint.ExportSubjectPublicKeyInfo())));
            var client = new ProviderPolicyAuthorityRotationDistributionClient(durable, endpointId);
            var failed = await client.SynchronizeAsync(new Cbi58Source((_, _) =>
                throw new IOException("unavailable")), DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
            var timeout = await client.SynchronizeAsync(new Cbi58Source(async (_, token) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1), token);
                throw new InvalidOperationException();
            }), DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(10));
            using var canceledSource = new CancellationTokenSource();
            canceledSource.Cancel();
            var canceled = await client.SynchronizeAsync(new Cbi58Source(async (_, token) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1), token);
                throw new InvalidOperationException();
            }), DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1), canceledSource.Token);
            var now = DateTimeOffset.UtcNow;
            var rotation = Cbi57Statement(1, 0, null, authority, successor, other: endpoint);
            var superseded = await client.SynchronizeAsync(new Cbi58Source((request, _) =>
            {
                var response = Cbi58Respond("current", request, endpoint, rotation, now);
                Assert.That(durable.Rotate(rotation).IsApplied, Is.True);
                return Task.FromResult(response);
            }), now, TimeSpan.FromSeconds(1));
            Assert.Multiple(() =>
            {
                Assert.That(failed.Code, Is.EqualTo("policy-authority-distribution-transport-failed"));
                Assert.That(timeout.Code, Is.EqualTo("policy-authority-distribution-timeout"));
                Assert.That(canceled.Code, Is.EqualTo("policy-authority-distribution-canceled"));
                Assert.That(superseded.Code, Is.EqualTo("policy-authority-distribution-superseded"));
                Assert.That(durable.AuthorityGeneration, Is.EqualTo(1));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task Cbi58_C6_applied_rotation_is_durable_and_shared()
    {
        using var fixture = Cbi58Fixture();
        var applied = await Cbi58RunAsync(fixture.RootElement.GetProperty("vectors")[1]);
        Assert.That(applied, Is.EqualTo(new Cbi58Observation("policy-authority-distribution-applied", 1, 1)));
    }
}
