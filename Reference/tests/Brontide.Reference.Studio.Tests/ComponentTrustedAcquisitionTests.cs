using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed class Cbi36Source(ProviderArtifactSourceId identity, byte[] bytes) : IProviderArtifactSource
    {
        public int IdentityReads { get; private set; }
        public int OpenCount { get; private set; }
        public ProviderArtifactSourceId Identity { get { IdentityReads++; return identity; } }
        public Stream? OpenRead(string relativePath)
        {
            OpenCount++;
            return relativePath == "provider.bin" ? new MemoryStream(bytes, false) : null;
        }
    }

    private sealed record Cbi36Observation(
        string TrustCode, string TransportCode, string AdmissionCode, bool Staged,
        int IdentityReads, int OpenCount, bool Residue);

    private static ProviderArtifactAcquisitionRequest Cbi36Request()
    {
        var bytes = Encoding.UTF8.GetBytes("provider");
        var digest = Convert.ToHexString(SHA256.HashData(bytes));
        ProviderArtifactAcquisitionFile[] files = [new("provider.bin", digest, bytes.Length)];
        return new(
            ProviderArtifactSourceId.Create("fixture://brontide/trusted-acquisition"),
            ProviderArtifactSetIdentity.Compute(
                files.Select(file => new ProviderArtifactFile(file.RelativePath, file.Sha256)),
                "provider.bin", []),
            files, "provider.bin", [], bytes.Length);
    }

    private static TrustedProviderPublisherAuthorization Cbi36Authorization(
        ProviderArtifactAcquisitionRequest request,
        string mutation)
    {
        var key = ProviderPublisherKeyId.Create(new string('A', 64));
        ProviderPublisherTrustEntry[] entries = [new(key, ProviderPublisherTrustDisposition.Admitted)];
        var policy = new ProviderPublisherTrustPolicy(
            ProviderPublisherTrustPolicyIdentity.Compute(entries), entries);
        var evidence = new VerifiedProviderPublisherEvidence(
            mutation == "content" ? ProviderArtifactSetId.Create(new string('0', 64)) : request.Identity,
            key,
            mutation == "payload" ? new string('0', 64) : ProviderArtifactPublisherManifest.Digest(request));
        return ProviderPublisherTrustEvaluator.Evaluate(policy, evidence).Authorization!;
    }

    private static Cbi36Observation Cbi36Run(JsonElement vector)
    {
        var mutation = vector.GetProperty("mutation").GetString()!;
        var canonical = Cbi36Request();
        var request = mutation == "invalid" ? canonical with { MaxTotalBytes = 0 } : canonical;
        var sourceId = mutation == "source"
            ? ProviderArtifactSourceId.Create("fixture://brontide/other-source")
            : canonical.ExpectedSource;
        var bytes = Encoding.UTF8.GetBytes(mutation == "integrity" ? "changed!" : "provider");
        var source = new Cbi36Source(sourceId, bytes);
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi36-{Guid.NewGuid():N}");
        try
        {
            var store = new ContentAddressedProviderStore(Path.Combine(root, "store"));
            var gate = new TrustedProviderArtifactAcquirer(
                new ProviderArtifactAcquirer(store, Path.Combine(root, "transactions")));
            var result = gate.Acquire(request, source,
                mutation == "missing" ? null : Cbi36Authorization(canonical, mutation));
            var observation = new Cbi36Observation(
                result.TrustCode, result.TransportCode, result.AdmissionCode, result.IsStaged,
                source.IdentityReads, source.OpenCount,
                Directory.Exists(Path.Combine(root, "transactions"))
                    && Directory.EnumerateFileSystemEntries(Path.Combine(root, "transactions")).Any());
            if (result.IsStaged)
            {
                Assert.That(store.Remove(canonical.Identity).Code, Is.EqualTo("removed"));
            }
            return observation;
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

    private static JsonDocument Cbi36Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi36-trusted-acquisition-vectors.json")));

    [Test]
    public void Cbi36_C1_trusted_authorization_is_issuer_controlled()
    {
        Assert.That(typeof(TrustedProviderPublisherAuthorization).GetConstructors(), Is.Empty);
    }

    [Test]
    public void Shared_cbi36_vectors_gate_acquisition_before_source_access()
    {
        using var fixture = Cbi36Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = Cbi36Run(vector);
            Assert.Multiple(() =>
            {
                Assert.That(actual.TrustCode, Is.EqualTo(vector.GetProperty("trustCode").GetString()));
                Assert.That(actual.TransportCode, Is.EqualTo(vector.GetProperty("transportCode").GetString()));
                Assert.That(actual.AdmissionCode, Is.EqualTo(vector.GetProperty("admissionCode").GetString()));
                Assert.That(actual.Staged, Is.EqualTo(vector.GetProperty("staged").GetBoolean()));
                Assert.That(actual.IdentityReads, Is.EqualTo(vector.GetProperty("identityReads").GetInt32()));
                Assert.That(actual.OpenCount, Is.EqualTo(vector.GetProperty("openCount").GetInt32()));
                Assert.That(actual.Residue, Is.False);
            });
        }
    }

    [Test]
    public void Cbi36_C2_complete_request_is_validated_before_trust_composition()
    {
        using var fixture = Cbi36Fixture();
        var actual = Cbi36Run(fixture.RootElement.GetProperty("vectors")[4]);
        Assert.Multiple(() =>
        {
            Assert.That(actual.TrustCode, Is.EqualTo("publisher-trust-not-evaluated"));
            Assert.That(actual.TransportCode, Is.EqualTo("acquisition-invalid"));
            Assert.That(actual.IdentityReads + actual.OpenCount, Is.Zero);
        });
    }

    [Test]
    public void Cbi36_C3_authorization_matches_exact_content_and_payload()
    {
        using var fixture = Cbi36Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors");
        Assert.That(Cbi36Run(vectors[2]).TrustCode, Is.EqualTo("publisher-authorization-content-mismatch"));
        Assert.That(Cbi36Run(vectors[3]).TrustCode, Is.EqualTo("publisher-authorization-payload-mismatch"));
    }

    [Test]
    public void Cbi36_C4_trust_succeeds_before_any_source_access()
    {
        using var fixture = Cbi36Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors");
        foreach (var index in new[] { 1, 2, 3 })
        {
            var actual = Cbi36Run(vectors[index]);
            Assert.That(actual.IdentityReads + actual.OpenCount, Is.Zero);
        }
        var sourceMismatch = Cbi36Run(vectors[5]);
        Assert.Multiple(() =>
        {
            Assert.That(sourceMismatch.TrustCode, Is.EqualTo("publisher-trusted"));
            Assert.That(sourceMismatch.TransportCode, Is.EqualTo("acquisition-source-mismatch"));
        });
    }

    [Test]
    public void Cbi36_C5_trusted_composition_preserves_CBI33_and_CBI32_outcomes()
    {
        using var fixture = Cbi36Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors");
        var staged = Cbi36Run(vectors[0]);
        var rejected = Cbi36Run(vectors[6]);
        Assert.Multiple(() =>
        {
            Assert.That(staged.Staged, Is.True);
            Assert.That(staged.AdmissionCode, Is.EqualTo("staged"));
            Assert.That(rejected.TrustCode, Is.EqualTo("publisher-trusted"));
            Assert.That(rejected.TransportCode, Is.EqualTo("transport-completed"));
            Assert.That(rejected.AdmissionCode, Is.EqualTo("artifact-set-integrity-failed"));
        });
    }

    [Test]
    public void Cbi36_C6_both_roots_agree_on_portable_observations()
    {
        using var fixture = Cbi36Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors").EnumerateArray().ToArray();
        Assert.That(vectors.Select(Cbi36Run).Select(value => new
            { value.TrustCode, value.TransportCode, value.AdmissionCode, value.Staged, value.IdentityReads, value.OpenCount }),
            Is.EqualTo(vectors.Select(value => new
            {
                TrustCode = value.GetProperty("trustCode").GetString(),
                TransportCode = value.GetProperty("transportCode").GetString(),
                AdmissionCode = value.GetProperty("admissionCode").GetString(),
                Staged = value.GetProperty("staged").GetBoolean(),
                IdentityReads = value.GetProperty("identityReads").GetInt32(),
                OpenCount = value.GetProperty("openCount").GetInt32(),
            })));
    }
}
