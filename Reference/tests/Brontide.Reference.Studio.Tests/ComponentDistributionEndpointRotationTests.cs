using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed class ComponentDistributionEndpointRotationTests
{
    private sealed class Source(ECDsa endpoint) : IProviderPublisherTrustPolicyDistributionSource
    {
        public int Attempts { get; private set; }

        public Task<ProviderPublisherTrustPolicyDistributionResponse> FetchAsync(
            ProviderPublisherTrustPolicyDistributionRequest request, CancellationToken cancellationToken)
        {
            Attempts++;
            var issued = 1_800_000_000L;
            var expires = issued + 60;
            var publicKey = endpoint.ExportSubjectPublicKeyInfo();
            var signature = endpoint.SignData(
                ProviderPublisherTrustPolicyDistributionManifest.Encode(
                    request.Challenge, request.CurrentSequence, request.CurrentPolicyIdentity,
                    issued, expires, null),
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
            return Task.FromResult(new ProviderPublisherTrustPolicyDistributionResponse(
                request.Challenge, request.CurrentSequence, request.CurrentPolicyIdentity,
                issued, expires, null, "ECDSA-P256-SHA256",
                Convert.ToBase64String(publicKey), Convert.ToBase64String(signature)));
        }
    }

    private static ProviderPublisherTrustPolicyDistributionEndpointId Id(ECDsa key) =>
        ProviderPublisherTrustPolicyDistributionEndpointId.Create(
            Convert.ToHexString(SHA256.HashData(key.ExportSubjectPublicKeyInfo())));

    private static ProviderPublisherTrustPolicyAuthorityId AuthorityId(ECDsa key) =>
        ProviderPublisherTrustPolicyAuthorityId.Create(
            Convert.ToHexString(SHA256.HashData(key.ExportSubjectPublicKeyInfo())));

    private static ProviderDistributionEndpointRotationStatement Statement(
        long generation,
        ECDsa previous,
        ECDsa next,
        ECDsa? signer = null,
        ECDsa? publishedKey = null)
    {
        var previousId = Id(previous);
        var nextId = Id(next);
        var signature = (signer ?? previous).SignData(
            ProviderDistributionEndpointRotationManifest.Encode(generation, previousId, nextId),
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        return new(generation, previousId, nextId, "ECDSA-P256-SHA256",
            Convert.ToBase64String((publishedKey ?? previous).ExportSubjectPublicKeyInfo()),
            Convert.ToBase64String(signature));
    }

    private static DurableProviderPublisherTrustPolicyRegistry Registry(string root, ECDsa authority) =>
        DurableProviderPublisherTrustPolicyRegistry.Open(
            Path.Combine(root, "policy.checkpoint"), AuthorityId(authority)).Registry!;

    private static string NewRoot() => Path.Combine(
        Path.GetTempPath(), $"brontide-cbi56-{Guid.NewGuid():N}");

    [Test]
    public void Cbi56_C1_the_durable_anchor_records_initial_active_generation_and_stage()
    {
        var root = NewRoot();
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var path = Path.Combine(root, "rotation.anchor");
            var opened = DurableProviderDistributionEndpointRotation.Open(path, Id(a));
            Assert.Multiple(() =>
            {
                Assert.That(opened.Code, Is.EqualTo("endpoint-rotation-established"));
                Assert.That(opened.Snapshot!.Generation, Is.Zero);
                Assert.That(opened.Snapshot.ActiveEndpoint, Is.EqualTo(Id(a)));
            });
            Assert.That(opened.Rotation!.Stage(Statement(1, a, b)).Code,
                Is.EqualTo("endpoint-rotation-staged"));
            var recovered = DurableProviderDistributionEndpointRotation.Open(path, Id(a));
            Assert.Multiple(() =>
            {
                Assert.That(recovered.Code, Is.EqualTo("endpoint-rotation-recovered"));
                Assert.That(recovered.Snapshot!.StagedGeneration, Is.EqualTo(1));
                Assert.That(recovered.Snapshot.StagedEndpoint, Is.EqualTo(Id(b)));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi56_C2_only_the_active_endpoint_can_sign_the_exact_successor()
    {
        var root = NewRoot();
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var c = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var rotation = DurableProviderDistributionEndpointRotation.Open(
                Path.Combine(root, "rotation.anchor"), Id(a)).Rotation!;
            Assert.That(rotation.Stage(Statement(1, a, b, c)).Code,
                Is.EqualTo("endpoint-rotation-signature-invalid"));
            Assert.That(rotation.Stage(Statement(1, a, b, a, c)).Code,
                Is.EqualTo("endpoint-rotation-key-mismatch"));
            Assert.That(rotation.Snapshot.StagedEndpoint, Is.Null);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi56_C3_staging_is_strict_durable_and_single_successor()
    {
        var root = NewRoot();
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var c = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var rotation = DurableProviderDistributionEndpointRotation.Open(
                Path.Combine(root, "rotation.anchor"), Id(a)).Rotation!;
            Assert.That(rotation.Stage(Statement(2, a, b)).Code,
                Is.EqualTo("endpoint-rotation-generation-invalid"));
            Assert.That(rotation.Stage(Statement(1, c, b)).Code,
                Is.EqualTo("endpoint-rotation-predecessor-mismatch"));
            Assert.That(rotation.Stage(Statement(1, a, a)).Code,
                Is.EqualTo("endpoint-rotation-self-refused"));
            Assert.That(rotation.Stage(Statement(1, a, b)).Code,
                Is.EqualTo("endpoint-rotation-staged"));
            Assert.That(rotation.Stage(Statement(1, a, b)).Code,
                Is.EqualTo("endpoint-rotation-already-staged"));
            Assert.That(rotation.Stage(Statement(1, a, c)).Code,
                Is.EqualTo("endpoint-rotation-successor-conflict"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task Cbi56_C4_ordinary_distribution_remains_pinned_only_to_the_active_endpoint()
    {
        var root = NewRoot();
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var rotation = DurableProviderDistributionEndpointRotation.Open(
                Path.Combine(root, "rotation.anchor"), Id(a)).Rotation!;
            rotation.Stage(Statement(1, a, b));
            var result = await rotation.CreateCurrentClient(Registry(root, authority)).SynchronizeAsync(
                new Source(b), DateTimeOffset.FromUnixTimeSeconds(1_800_000_000), TimeSpan.FromSeconds(1));
            Assert.That(result.Code, Is.EqualTo("policy-distribution-endpoint-mismatch"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task Cbi56_C5_confirmation_uses_one_staged_endpoint_attempt_and_fails_closed()
    {
        var root = NewRoot();
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var rotation = DurableProviderDistributionEndpointRotation.Open(
                Path.Combine(root, "rotation.anchor"), Id(a)).Rotation!;
            rotation.Stage(Statement(1, a, b));
            var source = new Source(a);
            var result = await rotation.ConfirmAsync(Registry(root, authority), source,
                DateTimeOffset.FromUnixTimeSeconds(1_800_000_000), TimeSpan.FromSeconds(1));
            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo("endpoint-rotation-confirmation-refused"));
                Assert.That(result.DistributionCode, Is.EqualTo("policy-distribution-endpoint-mismatch"));
                Assert.That(result.Snapshot.ActiveEndpoint, Is.EqualTo(Id(a)));
                Assert.That(result.Snapshot.StagedEndpoint, Is.EqualTo(Id(b)));
                Assert.That(source.Attempts, Is.EqualTo(1));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task Cbi56_C6_activation_is_durable_only_after_successful_confirmation()
    {
        var root = NewRoot();
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var path = Path.Combine(root, "rotation.anchor");
            var rotation = DurableProviderDistributionEndpointRotation.Open(path, Id(a)).Rotation!;
            rotation.Stage(Statement(1, a, b));
            var result = await rotation.ConfirmAsync(Registry(root, authority), new Source(b),
                DateTimeOffset.FromUnixTimeSeconds(1_800_000_000), TimeSpan.FromSeconds(1));
            var recovered = DurableProviderDistributionEndpointRotation.Open(path, Id(a));
            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo("endpoint-rotation-applied"));
                Assert.That(result.DistributionCode, Is.EqualTo("policy-distribution-current"));
                Assert.That(recovered.Snapshot!.Generation, Is.EqualTo(1));
                Assert.That(recovered.Snapshot.ActiveEndpoint, Is.EqualTo(Id(b)));
                Assert.That(recovered.Snapshot.StagedEndpoint, Is.Null);
            });

            var failedPath = Path.Combine(root, "failed-rotation.anchor");
            var failed = DurableProviderDistributionEndpointRotation.Open(failedPath, Id(a)).Rotation!;
            failed.Stage(Statement(1, a, b));
            Directory.CreateDirectory(failedPath + ".tmp");
            var writeFailure = await failed.ConfirmAsync(Registry(root, authority), new Source(b),
                DateTimeOffset.FromUnixTimeSeconds(1_800_000_000), TimeSpan.FromSeconds(1));
            Assert.Multiple(() =>
            {
                Assert.That(writeFailure.Code, Is.EqualTo("endpoint-rotation-write-failed"));
                Assert.That(writeFailure.DistributionCode, Is.EqualTo("policy-distribution-current"));
                Assert.That(writeFailure.Snapshot.ActiveEndpoint, Is.EqualTo(Id(a)));
                Assert.That(writeFailure.Snapshot.StagedEndpoint, Is.EqualTo(Id(b)));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task Cbi56_C7_recovery_rejects_damage_and_rollback_below_the_retained_floor()
    {
        var root = NewRoot();
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var path = Path.Combine(root, "rotation.anchor");
            var rotation = DurableProviderDistributionEndpointRotation.Open(path, Id(a)).Rotation!;
            var initialBytes = File.ReadAllBytes(path);
            rotation.Stage(Statement(1, a, b));
            var applied = await rotation.ConfirmAsync(Registry(root, authority), new Source(b),
                DateTimeOffset.FromUnixTimeSeconds(1_800_000_000), TimeSpan.FromSeconds(1));
            File.WriteAllBytes(path, initialBytes);
            Assert.That(DurableProviderDistributionEndpointRotation.Open(path, Id(a), applied.Floor).Code,
                Is.EqualTo("endpoint-rotation-rollback-detected"));
            var damaged = initialBytes.ToArray();
            damaged[0] ^= 1;
            File.WriteAllBytes(path, damaged);
            Assert.That(DurableProviderDistributionEndpointRotation.Open(path, Id(a)).Code,
                Is.EqualTo("endpoint-rotation-corrupt"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi56_C8_shared_vectors_pin_portable_rotation_observations()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi56-distribution-endpoint-rotation-vectors.json")));
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var root = NewRoot();
            try
            {
                using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                using var c = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                var keys = new Dictionary<string, ECDsa> { ["A"] = a, ["B"] = b, ["C"] = c };
                var previous = keys[vector.GetProperty("previous").GetString()!];
                var next = keys[vector.GetProperty("next").GetString()!];
                var evidence = vector.GetProperty("evidence").GetString();
                var statement = Statement(vector.GetProperty("generation").GetInt64(), previous, next,
                    evidence == "wrong-signer" ? c : previous,
                    evidence == "wrong-public-key" ? c : previous);
                var actual = DurableProviderDistributionEndpointRotation.Open(
                    Path.Combine(root, "rotation.anchor"), Id(a)).Rotation!.Stage(statement);
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()),
                    vector.GetProperty("name").GetString());
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}
