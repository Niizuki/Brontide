using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed class Cbi39Source(
        Func<ProviderPublisherTrustPolicyDistributionRequest, CancellationToken,
            Task<ProviderPublisherTrustPolicyDistributionResponse>> fetch)
        : IProviderPublisherTrustPolicyDistributionSource
    {
        public int Attempts { get; private set; }
        public Task<ProviderPublisherTrustPolicyDistributionResponse> FetchAsync(
            ProviderPublisherTrustPolicyDistributionRequest request, CancellationToken cancellationToken)
        {
            Attempts++;
            return fetch(request, cancellationToken);
        }
    }

    private sealed record Cbi39Observation(string Code, long Sequence, int Attempts);

    private static ProviderPublisherTrustPolicyDistributionResponse Cbi39Respond(
        string mutation,
        ProviderPublisherTrustPolicyDistributionRequest request,
        ECDsa endpoint,
        ProviderPublisherTrustPolicyUpdate update,
        DateTimeOffset now)
    {
        var selectedUpdate = mutation == "current" ? null : update;
        if (mutation == "policy-signature")
        {
            var changed = Convert.FromBase64String(update.SignatureBase64);
            changed[^1] ^= 1;
            selectedUpdate = update with { SignatureBase64 = Convert.ToBase64String(changed) };
        }
        var challenge = mutation == "challenge" ? new string('0', 64) : request.Challenge;
        var sequence = mutation == "cursor" ? request.CurrentSequence + 1 : request.CurrentSequence;
        var issued = mutation switch
        {
            "expired" => now.AddMinutes(-2),
            "future" => now.AddMinutes(1),
            _ => now,
        };
        var expires = mutation == "expired" ? now.AddMinutes(-1) : issued.AddMinutes(1);
        var publicKey = endpoint.ExportSubjectPublicKeyInfo();
        var signature = endpoint.SignData(
            ProviderPublisherTrustPolicyDistributionManifest.Encode(
                challenge, sequence, request.CurrentPolicyIdentity, issued.ToUnixTimeSeconds(),
                expires.ToUnixTimeSeconds(), selectedUpdate),
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        var response = new ProviderPublisherTrustPolicyDistributionResponse(
            challenge, sequence, request.CurrentPolicyIdentity, issued.ToUnixTimeSeconds(),
            expires.ToUnixTimeSeconds(), selectedUpdate, "ECDSA-P256-SHA256",
            Convert.ToBase64String(publicKey), Convert.ToBase64String(signature));
        if (mutation == "signature")
        {
            var changed = Convert.FromBase64String(response.SignatureBase64);
            changed[^1] ^= 1;
            response = response with { SignatureBase64 = Convert.ToBase64String(changed) };
        }
        if (mutation == "oversized") response = response with { SignatureBase64 = new string('A', 1024 * 1024 + 1) };
        return response;
    }

    private static async Task<Cbi39Observation> Cbi39RunAsync(JsonElement vector)
    {
        var mutation = vector.GetProperty("mutation").GetString()!;
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi39-{Guid.NewGuid():N}");
        try
        {
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var endpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var otherEndpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var authorityId = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo())));
            var endpointId = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
                Convert.ToHexString(SHA256.HashData(endpoint.ExportSubjectPublicKeyInfo())));
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), authorityId).Registry!;
            var update = Cbi37Sign(authority, 1, null, Cbi37Policy(false));
            var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
            var source = new Cbi39Source(async (request, cancellationToken) =>
            {
                if (mutation == "transport") throw new IOException("unavailable");
                if (mutation is "timeout" or "canceled")
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                return Cbi39Respond(mutation, request,
                    mutation == "endpoint" ? otherEndpoint : endpoint, update, now);
            });
            using var cancellation = new CancellationTokenSource();
            if (mutation == "canceled") cancellation.Cancel();
            var result = await new ProviderPublisherTrustPolicyDistributionClient(durable, endpointId)
                .SynchronizeAsync(source, now, mutation == "timeout" ? TimeSpan.FromMilliseconds(10) : TimeSpan.FromSeconds(1),
                    cancellation.Token);
            Assert.That(result.Floor.Sequence, Is.EqualTo(result.Current?.Sequence ?? 0));
            return new(result.Code, result.Current?.Sequence ?? 0, source.Attempts);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static JsonDocument Cbi39Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi39-policy-distribution-vectors.json")));

    [Test]
    public async Task Shared_cbi39_vectors_authenticate_fresh_bounded_distribution()
    {
        using var fixture = Cbi39Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi39RunAsync(vector);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()));
                Assert.That(actual.Sequence, Is.EqualTo(vector.GetProperty("sequence").GetInt64()));
                Assert.That(actual.Attempts, Is.EqualTo(1));
            });
        }
    }

    [Test]
    public async Task Cbi39_C1_only_the_pinned_endpoint_can_authenticate_a_response()
    {
        using var fixture = Cbi39Fixture();
        Assert.That((await Cbi39RunAsync(fixture.RootElement.GetProperty("vectors")[2])).Code,
            Is.EqualTo("policy-distribution-endpoint-mismatch"));
        Assert.That((await Cbi39RunAsync(fixture.RootElement.GetProperty("vectors")[3])).Code,
            Is.EqualTo("policy-distribution-signature-invalid"));
    }

    [Test]
    public async Task Cbi39_C2_signed_challenge_and_cursor_prevent_replay_and_cross_state_delivery()
    {
        using var fixture = Cbi39Fixture();
        Assert.That((await Cbi39RunAsync(fixture.RootElement.GetProperty("vectors")[4])).Code,
            Is.EqualTo("policy-distribution-challenge-mismatch"));
        Assert.That((await Cbi39RunAsync(fixture.RootElement.GetProperty("vectors")[5])).Code,
            Is.EqualTo("policy-distribution-cursor-mismatch"));
    }

    [Test]
    public async Task Cbi39_C3_only_current_short_lived_responses_are_accepted()
    {
        using var fixture = Cbi39Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors");
        Assert.That((await Cbi39RunAsync(vectors[0])).Code, Is.EqualTo("policy-distribution-current"));
        Assert.That((await Cbi39RunAsync(vectors[1])).Code, Is.EqualTo("policy-distribution-applied"));
        Assert.That((await Cbi39RunAsync(vectors[6])).Code, Is.EqualTo("policy-distribution-stale"));
        Assert.That((await Cbi39RunAsync(vectors[7])).Code, Is.EqualTo("policy-distribution-stale"));
    }

    [Test]
    public async Task Cbi39_C4_one_attempt_has_strict_size_and_time_bounds()
    {
        using var fixture = Cbi39Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors");
        foreach (var index in new[] { 8, 10, 11, 12 })
            Assert.That((await Cbi39RunAsync(vectors[index])).Attempts, Is.EqualTo(1));
    }

    [Test]
    public async Task Cbi39_C5_only_a_native_cbi37_update_can_advance_the_durable_registry()
    {
        using var fixture = Cbi39Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors");
        Assert.That((await Cbi39RunAsync(vectors[9])).Code, Is.EqualTo("policy-update-signature-invalid"));
        Assert.That((await Cbi39RunAsync(vectors[1])).Sequence, Is.EqualTo(1));
    }

    [Test]
    public async Task Cbi39_C6_both_roots_agree_on_portable_observations()
    {
        using var fixture = Cbi39Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi39RunAsync(vector);
            Assert.That((actual.Code, actual.Sequence), Is.EqualTo(
                (vector.GetProperty("code").GetString(), vector.GetProperty("sequence").GetInt64())));
        }
    }
}
