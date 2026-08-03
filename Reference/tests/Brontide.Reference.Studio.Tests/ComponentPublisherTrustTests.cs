using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private static async Task<JsonDocument> Cbi35FixtureAsync() => JsonDocument.Parse(
        await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "component-management", "fixtures", "cbi35-publisher-trust-vectors.json")));

    private static ProviderPublisherTrustPolicy Cbi35Policy(JsonElement root)
    {
        var value = root.GetProperty("canonicalPolicy");
        var entries = value.GetProperty("entries").EnumerateArray().Select(entry => new ProviderPublisherTrustEntry(
            ProviderPublisherKeyId.Create(entry.GetProperty("publisherKeyId").GetString()!),
            entry.GetProperty("disposition").GetString() == "admitted"
                ? ProviderPublisherTrustDisposition.Admitted : ProviderPublisherTrustDisposition.Revoked)).ToArray();
        return new(ProviderPublisherTrustPolicyId.Create(value.GetProperty("identity").GetString()!), entries);
    }

    private static VerifiedProviderPublisherEvidence Cbi35Evidence(JsonElement root, string key) => new(
        ProviderArtifactSetId.Create(root.GetProperty("verifiedEvidence").GetProperty("contentIdentity").GetString()!),
        ProviderPublisherKeyId.Create(new string(key[0], 64)),
        root.GetProperty("verifiedEvidence").GetProperty("payloadSha256").GetString()!);

    private static ProviderPublisherTrustResult Cbi35Run(JsonElement root, JsonElement vector)
    {
        var policy = Cbi35Policy(root);
        var mutation = vector.GetProperty("mutation").GetString()!;
        policy = mutation switch
        {
            "identity" => policy with { Identity = ProviderPublisherTrustPolicyId.Create(new string('0', 64)) },
            "duplicate" => policy with { Entries = [policy.Entries[0], policy.Entries[0]] },
            "empty" => policy with { Entries = [] },
            _ => policy,
        };
        return ProviderPublisherTrustEvaluator.Evaluate(policy,
            mutation == "unverified" ? null : Cbi35Evidence(root, vector.GetProperty("key").GetString()!));
    }

    [Test]
    public async Task Shared_cbi35_vectors_evaluate_verified_publishers_without_attempting_admission()
    {
        using var fixture = await Cbi35FixtureAsync();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var result = Cbi35Run(fixture.RootElement, vector);
            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo(vector.GetProperty("code").GetString()));
                Assert.That(result.IsTrusted, Is.EqualTo(vector.GetProperty("authorized").GetBoolean()));
                Assert.That(result.AdmissionCode, Is.EqualTo(fixture.RootElement.GetProperty("admissionCode").GetString()));
            });
        }
    }

    [Test]
    public async Task Cbi35_C1_policy_is_a_canonical_immutable_snapshot()
    {
        using var fixture = await Cbi35FixtureAsync();
        var policy = Cbi35Policy(fixture.RootElement);
        Assert.That(ProviderPublisherTrustPolicyIdentity.Compute(policy.Entries), Is.EqualTo(policy.Identity));
        Assert.That(ProviderPublisherTrustPolicyIdentity.Compute(policy.Entries.Reverse()), Is.EqualTo(policy.Identity));
        foreach (var mutation in new[] { "identity", "duplicate", "empty" })
        {
            var vector = fixture.RootElement.GetProperty("vectors").EnumerateArray()
                .Single(item => item.GetProperty("mutation").GetString() == mutation);
            Assert.That(Cbi35Run(fixture.RootElement, vector).Authorization, Is.Null);
        }
    }

    [Test]
    public async Task Cbi35_C2_only_verified_publisher_evidence_is_eligible()
    {
        using var fixture = await Cbi35FixtureAsync();
        var vector = fixture.RootElement.GetProperty("vectors")[3];
        var result = Cbi35Run(fixture.RootElement, vector);
        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("publisher-evidence-not-verified"));
            Assert.That(result.PublisherKeyId, Is.Null);
            Assert.That(result.Authorization, Is.Null);
        });
    }

    [Test]
    public async Task Cbi35_C3_admitted_revoked_and_unknown_keys_are_distinct()
    {
        using var fixture = await Cbi35FixtureAsync();
        var results = fixture.RootElement.GetProperty("vectors").EnumerateArray().Take(3)
            .Select(vector => Cbi35Run(fixture.RootElement, vector)).ToArray();
        Assert.That(results.Select(result => result.Code), Is.EqualTo(new[]
            { "publisher-trusted", "publisher-key-revoked", "publisher-key-unknown" }));
        Assert.That(results.Select(result => result.IsTrusted), Is.EqualTo(new[] { true, false, false }));
    }

    [Test]
    public async Task Cbi35_C4_trust_evaluation_is_not_artifact_admission()
    {
        using var fixture = await Cbi35FixtureAsync();
        var result = Cbi35Run(fixture.RootElement, fixture.RootElement.GetProperty("vectors")[0]);
        Assert.Multiple(() =>
        {
            Assert.That(result.EvidenceCode, Is.EqualTo("publisher-evidence-valid"));
            Assert.That(result.IsTrusted, Is.True);
            Assert.That(result.AdmissionCode, Is.EqualTo("admission-not-attempted"));
        });
    }

    [Test]
    public async Task Cbi35_C5_caller_may_require_matching_trust_before_CBI33_acquisition()
    {
        using var fixture = await Cbi35FixtureAsync();
        var bytes = Encoding.UTF8.GetBytes("provider");
        var digest = Convert.ToHexString(SHA256.HashData(bytes));
        var sourceId = ProviderArtifactSourceId.Create("fixture://brontide/trusted-composition");
        ProviderArtifactAcquisitionFile[] files = [new("provider.bin", digest, bytes.Length)];
        var identity = ProviderArtifactSetIdentity.Compute(
            files.Select(file => new ProviderArtifactFile(file.RelativePath, file.Sha256)), "provider.bin", []);
        var request = new ProviderArtifactAcquisitionRequest(sourceId, identity, files, "provider.bin", [], bytes.Length);
        var policy = Cbi35Policy(fixture.RootElement);
        var evidence = Cbi35Evidence(fixture.RootElement, "A") with { ContentIdentity = identity };
        var trust = ProviderPublisherTrustEvaluator.Evaluate(policy, evidence);
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi35-{Guid.NewGuid():N}");
        try
        {
            var source = new MemoryArtifactSource(sourceId, new Dictionary<string, Func<Stream?>>
                { ["provider.bin"] = () => new MemoryStream(bytes, false) });
            var acquisition = trust.Authorization?.ContentIdentity == request.Identity
                ? new ProviderArtifactAcquirer(new ContentAddressedProviderStore(Path.Combine(root, "store")),
                    Path.Combine(root, "transactions")).Acquire(request, source)
                : null;
            Assert.Multiple(() =>
            {
                Assert.That(trust.Authorization?.PayloadSha256, Is.EqualTo(evidence.PayloadSha256));
                Assert.That(acquisition?.AdmissionCode, Is.EqualTo("staged"));
                Assert.That(source.OpenCount, Is.EqualTo(1));
            });
        }
        finally
        {
            if (Directory.Exists(root))
            {
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(root, true);
            }
        }
    }

    [Test]
    public async Task Cbi35_C6_both_roots_agree_on_portable_observations()
    {
        using var fixture = await Cbi35FixtureAsync();
        var vectors = fixture.RootElement.GetProperty("vectors").EnumerateArray().ToArray();
        Assert.That(vectors.Select(vector => Cbi35Run(fixture.RootElement, vector).Code),
            Is.EqualTo(vectors.Select(vector => vector.GetProperty("code").GetString())));
    }
}
