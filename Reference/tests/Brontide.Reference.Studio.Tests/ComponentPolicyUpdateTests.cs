using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed record Cbi37Observation(string Code, long Sequence, string? PolicyIdentity, bool Changed);

    private static ProviderPublisherTrustPolicy Cbi37Policy(bool revoked)
    {
        ProviderPublisherTrustEntry[] entries = [new(
            ProviderPublisherKeyId.Create(new string('A', 64)),
            revoked ? ProviderPublisherTrustDisposition.Revoked : ProviderPublisherTrustDisposition.Admitted)];
        return new(ProviderPublisherTrustPolicyIdentity.Compute(entries), entries);
    }

    private static ProviderPublisherTrustPolicyUpdate Cbi37Sign(
        ECDsa key, long sequence, ProviderPublisherTrustPolicyId? previous,
        ProviderPublisherTrustPolicy policy, string algorithm = "ECDSA-P256-SHA256")
    {
        var publicKey = key.ExportSubjectPublicKeyInfo();
        var signature = key.SignData(
            ProviderPublisherTrustPolicyUpdateManifest.Encode(sequence, previous, policy.Identity),
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        return new(sequence, previous, policy, algorithm,
            Convert.ToBase64String(publicKey), Convert.ToBase64String(signature));
    }

    private static Cbi37Observation Cbi37Run(JsonElement vector)
    {
        var mutation = vector.GetProperty("mutation").GetString()!;
        using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var authorityId = ProviderPublisherTrustPolicyAuthorityId.Create(
            Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo())));
        var registry = new ProviderPublisherTrustPolicyRegistry(authorityId);
        var initial = Cbi37Policy(false);
        var successor = Cbi37Policy(true);
        if (mutation != "none") Assert.That(registry.Apply(Cbi37Sign(authority, 1, null, initial)).IsApplied, Is.True);
        var before = registry.Current;
        var update = mutation switch
        {
            "none" => Cbi37Sign(authority, 1, null, initial),
            "successor" => Cbi37Sign(authority, 2, initial.Identity, successor),
            "replay" => Cbi37Sign(authority, 1, null, initial),
            "gap" => Cbi37Sign(authority, 3, initial.Identity, successor),
            "fork" => Cbi37Sign(authority, 2, ProviderPublisherTrustPolicyId.Create(new string('0', 64)), successor),
            "policy" => Cbi37Sign(authority, 2, initial.Identity,
                successor with { Identity = ProviderPublisherTrustPolicyId.Create(new string('0', 64)) }),
            "algorithm" => Cbi37Sign(authority, 2, initial.Identity, successor, "unknown"),
            _ => Cbi37Sign(authority, 2, initial.Identity, successor),
        };
        if (mutation == "authority")
        {
            using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            update = Cbi37Sign(other, 2, initial.Identity, successor);
        }
        else if (mutation == "signature")
        {
            var signature = Convert.FromBase64String(update.SignatureBase64);
            signature[^1] ^= 1;
            update = update with { SignatureBase64 = Convert.ToBase64String(signature) };
        }
        var result = registry.Apply(update);
        return new(result.Code, result.Current?.Sequence ?? 0,
            result.Current?.Policy.Identity.Value,
            !ReferenceEquals(before, result.Current));
    }

    private static JsonDocument Cbi37Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi37-policy-update-vectors.json")));

    [Test]
    public void Shared_cbi37_vectors_apply_only_authoritative_monotonic_updates()
    {
        using var fixture = Cbi37Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var result = Cbi37Run(vector);
            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo(vector.GetProperty("code").GetString()));
                Assert.That(result.Sequence, Is.EqualTo(vector.GetProperty("sequence").GetInt64()));
                Assert.That(result.Changed, Is.EqualTo(vector.GetProperty("changed").GetBoolean()));
            });
        }
    }

    [Test]
    public void Cbi37_C1_one_pinned_authority_controls_policy_provenance()
    {
        using var fixture = Cbi37Fixture();
        Assert.That(Cbi37Run(fixture.RootElement.GetProperty("vectors")[2]).Code,
            Is.EqualTo("policy-update-authority-mismatch"));
    }

    [Test]
    public void Cbi37_C2_signature_covers_a_canonical_complete_update_payload()
    {
        using var fixture = Cbi37Fixture();
        var golden = fixture.RootElement.GetProperty("golden");
        var initial = Cbi37Policy(false);
        var successor = Cbi37Policy(true);
        Assert.Multiple(() =>
        {
            Assert.That(initial.Identity.Value, Is.EqualTo(golden.GetProperty("initialPolicyIdentity").GetString()));
            Assert.That(successor.Identity.Value, Is.EqualTo(golden.GetProperty("successorPolicyIdentity").GetString()));
            Assert.That(ProviderPublisherTrustPolicyUpdateManifest.Digest(1, null, initial.Identity),
                Is.EqualTo(golden.GetProperty("initialPayloadSha256").GetString()));
            Assert.That(ProviderPublisherTrustPolicyUpdateManifest.Digest(2, initial.Identity, successor.Identity),
                Is.EqualTo(golden.GetProperty("successorPayloadSha256").GetString()));
        });
    }

    [Test]
    public void Cbi37_C3_updates_form_one_strict_monotonic_predecessor_chain()
    {
        using var fixture = Cbi37Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors");
        foreach (var index in new[] { 5, 6, 7 }) Assert.That(Cbi37Run(vectors[index]).Changed, Is.False);
    }

    [Test]
    public void Cbi37_C4_application_publishes_one_issuer_controlled_snapshot_and_refusal_preserves_it()
    {
        Assert.That(typeof(VerifiedProviderPublisherTrustPolicySnapshot).GetConstructors(), Is.Empty);
        using var fixture = Cbi37Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray().Skip(2))
            Assert.That(Cbi37Run(vector).Sequence, Is.EqualTo(1));
    }

    [Test]
    public void Cbi37_C5_current_policy_supersedes_outstanding_acquisition_authorization()
    {
        using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var registry = new ProviderPublisherTrustPolicyRegistry(ProviderPublisherTrustPolicyAuthorityId.Create(
            Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo()))));
        var request = Cbi36Request();
        var authorization = Cbi36Authorization(request, "none");
        var oldSource = new Cbi36Source(request.ExpectedSource, System.Text.Encoding.UTF8.GetBytes("provider"));
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi37-{Guid.NewGuid():N}");
        try
        {
            var store = new ContentAddressedProviderStore(Path.Combine(root, "store"));
            var governed = new GovernedProviderArtifactAcquirer(registry,
                new TrustedProviderArtifactAcquirer(new ProviderArtifactAcquirer(store, Path.Combine(root, "transactions"))));
            var unavailable = governed.Acquire(request, oldSource, authorization);
            var initial = Cbi37Policy(false);
            Assert.That(registry.Apply(Cbi37Sign(authority, 1, null, initial)).IsApplied, Is.True);
            var admitted = governed.Acquire(request, oldSource, authorization);
            Assert.That(store.Remove(request.Identity).Code, Is.EqualTo("removed"));
            var successor = Cbi37Policy(true);
            Assert.That(registry.Apply(Cbi37Sign(authority, 2, initial.Identity, successor)).IsApplied, Is.True);
            var newSource = new Cbi36Source(request.ExpectedSource, System.Text.Encoding.UTF8.GetBytes("provider"));
            var superseded = governed.Acquire(request, newSource, authorization);
            var verified = new VerifiedProviderPublisherEvidence(request.Identity,
                ProviderPublisherKeyId.Create(new string('A', 64)), ProviderArtifactPublisherManifest.Digest(request));
            Assert.Multiple(() =>
            {
                Assert.That(unavailable.TrustCode, Is.EqualTo("publisher-trust-policy-unavailable"));
                Assert.That(admitted.IsStaged, Is.True);
                Assert.That(superseded.TrustCode, Is.EqualTo("publisher-authorization-superseded"));
                Assert.That(newSource.IdentityReads + newSource.OpenCount, Is.Zero);
                Assert.That(ProviderPublisherTrustEvaluator.Evaluate(successor, verified).Authorization, Is.Null);
            });
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
    public void Cbi37_C6_both_roots_agree_on_portable_observations()
    {
        using var fixture = Cbi37Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors").EnumerateArray().ToArray();
        Assert.That(vectors.Select(Cbi37Run).Select(value => (value.Code, value.Sequence, value.Changed)),
            Is.EqualTo(vectors.Select(value => (value.GetProperty("code").GetString(),
                value.GetProperty("sequence").GetInt64(), value.GetProperty("changed").GetBoolean()))));
    }
}
