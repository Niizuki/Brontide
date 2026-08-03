using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed record Cbi38Observation(string Code, long Sequence, bool TemporaryResidue);

    private static Cbi38Observation Cbi38Run(JsonElement vector)
    {
        var mutation = vector.GetProperty("mutation").GetString()!;
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi38-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "policy.checkpoint");
        Directory.CreateDirectory(root);
        try
        {
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var authorityId = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo())));
            var opened = DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId);
            if (mutation == "empty") return new(opened.Code, 0, File.Exists(path + ".tmp"));
            var durable = opened.Registry!;
            var initial = Cbi37Policy(false);
            var first = Cbi37Sign(authority, 1, null, initial);
            var firstResult = durable.Apply(first);
            Assert.That(firstResult.IsApplied, Is.True);
            var firstBytes = File.ReadAllBytes(path);
            if (mutation == "missing-rollback")
            {
                File.Delete(path);
                var result = DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId, firstResult.Floor);
                return new(result.Code, 0, File.Exists(path + ".tmp"));
            }
            if (mutation is "recovered" or "old-rollback")
            {
                var successor = Cbi37Policy(true);
                var second = durable.Apply(Cbi37Sign(authority, 2, initial.Identity, successor));
                Assert.That(second.IsApplied, Is.True);
                if (mutation == "old-rollback")
                {
                    File.WriteAllBytes(path, firstBytes);
                    var result = DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId, second.Floor);
                    return new(result.Code, 0, File.Exists(path + ".tmp"));
                }
                var recovered = DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId, second.Floor);
                return new(recovered.Code, recovered.Registry?.Current?.Sequence ?? 0, File.Exists(path + ".tmp"));
            }
            var bytes = File.ReadAllBytes(path);
            if (mutation == "truncated") File.WriteAllBytes(path, bytes[..(bytes.Length / 2)]);
            else if (mutation == "trailing") File.WriteAllBytes(path, [.. bytes, 0]);
            else if (mutation == "signature")
            {
                var signature = Encoding.UTF8.GetBytes(first.SignatureBase64);
                var offset = bytes.AsSpan().IndexOf(signature);
                Assert.That(offset, Is.GreaterThanOrEqualTo(0));
                bytes[offset] = bytes[offset] == (byte)'A' ? (byte)'B' : (byte)'A';
                File.WriteAllBytes(path, bytes);
            }
            var failed = DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId);
            return new(failed.Code, 0, File.Exists(path + ".tmp"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static JsonDocument Cbi38Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi38-policy-checkpoint-vectors.json")));

    [Test]
    public void Shared_cbi38_vectors_recover_only_complete_non_rolled_back_checkpoints()
    {
        using var fixture = Cbi38Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = Cbi38Run(vector);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()));
                Assert.That(actual.Sequence, Is.EqualTo(vector.GetProperty("sequence").GetInt64()));
                Assert.That(actual.TemporaryResidue, Is.False);
            });
        }
    }

    [Test]
    public void Cbi38_C1_bounded_checkpoint_contains_the_complete_signed_chain()
    {
        using var fixture = Cbi38Fixture();
        Assert.That(Cbi38Run(fixture.RootElement.GetProperty("vectors")[1]).Sequence, Is.EqualTo(2));

        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi38-bound-{Guid.NewGuid():N}");
        try
        {
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var authorityId = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo())));
            var entries = Enumerable.Range(0, 4097).Select(index => new ProviderPublisherTrustEntry(
                ProviderPublisherKeyId.Create(index.ToString("X64")), ProviderPublisherTrustDisposition.Admitted)).ToArray();
            var policy = new ProviderPublisherTrustPolicy(ProviderPublisherTrustPolicyIdentity.Compute(entries), entries);
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), authorityId).Registry!;
            var bounded = durable.Apply(Cbi37Sign(authority, 1, null, policy));
            Assert.Multiple(() =>
            {
                Assert.That(bounded.Code, Is.EqualTo("policy-checkpoint-write-failed"));
                Assert.That(durable.Current, Is.Null);
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi38_C2_recovery_reverifies_provenance_and_the_complete_chain()
    {
        using var fixture = Cbi38Fixture();
        Assert.That(Cbi38Run(fixture.RootElement.GetProperty("vectors")[4]).Code,
            Is.EqualTo("policy-checkpoint-invalid-chain"));
    }

    [Test]
    public void Cbi38_C3_publication_is_atomic_and_crash_residue_is_inert()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi38-residue-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "policy.checkpoint");
        try
        {
            File.WriteAllText(path + ".tmp", "partial");
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var authorityId = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo())));
            var result = DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId);
            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo("policy-checkpoint-empty"));
                Assert.That(File.Exists(path + ".tmp"), Is.False);
            });
        }
        finally { Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi38_C4_external_recovery_floor_detects_checkpoint_rollback()
    {
        using var fixture = Cbi38Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors");
        Assert.That(Cbi38Run(vectors[5]).Code, Is.EqualTo("policy-checkpoint-rollback-detected"));
        Assert.That(Cbi38Run(vectors[6]).Code, Is.EqualTo("policy-checkpoint-rollback-detected"));
        Assert.That(typeof(ProviderPublisherTrustPolicyRecoveryFloor).GetConstructors(), Is.Empty);
    }

    [Test]
    public void Cbi38_C5_checkpoint_failure_preserves_live_state_and_recovery_governs_acquisition()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi38-write-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "blocked");
        try
        {
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var authorityId = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo())));
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(path, authorityId).Registry!;
            Directory.CreateDirectory(path);
            var failed = durable.Apply(Cbi37Sign(authority, 1, null, Cbi37Policy(false)));
            Assert.Multiple(() =>
            {
                Assert.That(failed.Code, Is.EqualTo("policy-checkpoint-write-failed"));
                Assert.That(durable.Current, Is.Null);
                Assert.That(failed.Floor.Sequence, Is.Zero);
            });

            Directory.Delete(path);
            var checkpoint = Path.Combine(root, "policy.checkpoint");
            var initial = Cbi37Policy(false);
            var opened = DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, authorityId).Registry!;
            var applied = opened.Apply(Cbi37Sign(authority, 1, null, initial));
            var recovered = DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, authorityId, applied.Floor);
            var request = Cbi36Request();
            var store = new ContentAddressedProviderStore(Path.Combine(root, "store"));
            var governed = recovered.Registry!.Govern(
                new TrustedProviderArtifactAcquirer(new ProviderArtifactAcquirer(
                    store, Path.Combine(root, "transactions"))));
            var acquired = governed.Acquire(request,
                new Cbi36Source(request.ExpectedSource, Encoding.UTF8.GetBytes("provider")),
                Cbi36Authorization(request, "none"));
            Assert.That(acquired.IsStaged, Is.True);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);
                Directory.Delete(root, true);
            }
        }
    }

    [Test]
    public void Cbi38_C6_both_roots_agree_on_portable_observations()
    {
        using var fixture = Cbi38Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors").EnumerateArray().ToArray();
        Assert.That(vectors.Select(Cbi38Run).Select(value => (value.Code, value.Sequence)),
            Is.EqualTo(vectors.Select(value =>
                (value.GetProperty("code").GetString(), value.GetProperty("sequence").GetInt64()))));
    }
}
