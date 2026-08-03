using System.Security.Cryptography;
using System.Collections;
using System.Text;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed class SwitchingArtifactFiles(IReadOnlyList<ProviderArtifactAcquisitionFile> original)
        : IReadOnlyList<ProviderArtifactAcquisitionFile>
    {
        private int _enumerations;

        public int Count => original.Count;

        public ProviderArtifactAcquisitionFile this[int index] => original[index];

        public IEnumerator<ProviderArtifactAcquisitionFile> GetEnumerator()
        {
            _enumerations++;
            return (_enumerations == 1
                ? original
                : original.Select((file, index) => index == 0 ? file with { Sha256 = new string('C', 64) } : file)
                    .ToArray()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed record Cbi34Observation(
        string Code,
        bool Verified,
        string PayloadSha256,
        string? PublisherKeyId,
        string TrustCode,
        string AdmissionCode);

    private static ProviderArtifactAcquisitionRequest Cbi34CanonicalRequest(JsonElement manifest)
    {
        var files = manifest.GetProperty("files").EnumerateArray()
            .Select(file => new ProviderArtifactAcquisitionFile(
                file.GetProperty("path").GetString()!,
                file.GetProperty("sha256").GetString()!,
                file.GetProperty("length").GetInt64()))
            .ToArray();
        return new(
            ProviderArtifactSourceId.Create("fixture://brontide/publisher-evidence"),
            ProviderArtifactSetId.Create(manifest.GetProperty("identity").GetString()!),
            files,
            manifest.GetProperty("executablePath").GetString()!,
            manifest.GetProperty("arguments").EnumerateArray().Select(value => value.GetString()!).ToArray(),
            files.Sum(file => file.Length));
    }

    private static ProviderArtifactAcquisitionRequest Cbi34Mutate(
        ProviderArtifactAcquisitionRequest request,
        string mutation)
    {
        var files = request.Files.ToArray();
        var executable = request.ExecutablePath;
        var arguments = request.Arguments.ToArray();
        if (mutation == "path")
        {
            var index = Array.FindIndex(files, file => file.RelativePath == "data/config.json");
            files[index] = files[index] with { RelativePath = "data/settings.json" };
        }
        else if (mutation == "digest")
        {
            files[0] = files[0] with { Sha256 = new string('C', 64) };
        }
        else if (mutation == "length")
        {
            files[0] = files[0] with { Length = files[0].Length + 1 };
        }
        else if (mutation == "executable")
        {
            executable = files.Single(file => file.RelativePath != request.ExecutablePath).RelativePath;
        }
        else if (mutation == "arguments")
        {
            arguments = [.. arguments, "--changed"];
        }

        var identity = ProviderArtifactSetIdentity.Compute(
            files.Select(file => new ProviderArtifactFile(file.RelativePath, file.Sha256)), executable, arguments);
        return request with
        {
            Identity = identity,
            Files = files,
            ExecutablePath = executable,
            Arguments = arguments,
            MaxTotalBytes = files.Sum(file => file.Length),
        };
    }

    private static ProviderPublisherEvidence Cbi34Sign(
        ProviderArtifactAcquisitionRequest request,
        ECDsa key,
        string mutation)
    {
        var publicKey = key.ExportSubjectPublicKeyInfo();
        var keyId = ProviderPublisherKeyId.Create(Convert.ToHexString(SHA256.HashData(publicKey)));
        var signature = key.SignData(
            ProviderArtifactPublisherManifest.Encode(request),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);
        var evidence = new ProviderPublisherEvidence(
            keyId,
            "ECDSA-P256-SHA256",
            Convert.ToBase64String(publicKey),
            Convert.ToBase64String(signature));
        return mutation switch
        {
            "absent" => evidence,
            "algorithm" => evidence with { Algorithm = "unknown" },
            "key" => evidence with { PublicKeySpkiBase64 = "not-base64" },
            "key-id" => evidence with { PublisherKeyId = ProviderPublisherKeyId.Create(new string('0', 64)) },
            "signature" => evidence with
            {
                SignatureBase64 = Convert.ToBase64String(key.SignData(
                    Encoding.UTF8.GetBytes("different payload"),
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.Rfc3279DerSequence)),
            },
            _ => evidence,
        };
    }

    private static Cbi34Observation Cbi34Run(JsonElement manifest, JsonElement vector)
    {
        var mutation = vector.GetProperty("mutation").GetString()!;
        var original = Cbi34CanonicalRequest(manifest);
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var evidence = Cbi34Sign(original, key, mutation);
        var request = mutation is "path" or "digest" or "length" or "executable" or "arguments"
            ? Cbi34Mutate(original, mutation)
            : original;
        var result = ProviderArtifactPublisherEvidenceVerifier.Verify(
            request,
            mutation == "absent" ? null : evidence);
        return new(
            result.Code,
            result.IsVerified,
            result.PayloadSha256,
            result.PublisherKeyId?.Value,
            result.TrustCode,
            result.AdmissionCode);
    }

    private static async Task<JsonDocument> Cbi34FixtureAsync() => JsonDocument.Parse(
        await File.ReadAllTextAsync(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "component-management",
            "fixtures",
            "cbi34-publisher-evidence-vectors.json")));

    [Test]
    public async Task Shared_cbi34_vectors_verify_publisher_evidence_without_granting_trust()
    {
        using var fixture = await Cbi34FixtureAsync();
        var manifest = fixture.RootElement.GetProperty("canonicalManifest");
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var observation = Cbi34Run(manifest, vector);
            Assert.Multiple(() =>
            {
                Assert.That(observation.Code, Is.EqualTo(vector.GetProperty("code").GetString()));
                Assert.That(observation.Verified, Is.EqualTo(vector.GetProperty("verified").GetBoolean()));
                Assert.That(observation.TrustCode, Is.EqualTo("publisher-trust-not-evaluated"));
                Assert.That(observation.AdmissionCode, Is.EqualTo("admission-not-attempted"));
            });
        }
    }

    [Test]
    public async Task Cbi34_C1_canonical_payload_covers_the_complete_acquisition_manifest()
    {
        using var fixture = await Cbi34FixtureAsync();
        var manifest = fixture.RootElement.GetProperty("canonicalManifest");
        var request = Cbi34CanonicalRequest(manifest);
        Assert.That(ProviderArtifactPublisherManifest.Digest(request),
            Is.EqualTo(manifest.GetProperty("payloadSha256").GetString()));
        Assert.That(ProviderArtifactPublisherManifest.Encode(request with
            {
                ExpectedSource = ProviderArtifactSourceId.Create("fixture://brontide/other-source"),
                MaxTotalBytes = request.MaxTotalBytes + 100,
            }), Is.EqualTo(ProviderArtifactPublisherManifest.Encode(request)));
        Assert.That(ProviderArtifactPublisherManifest.Digest(request with
            {
                Files = new SwitchingArtifactFiles(request.Files),
            }), Is.EqualTo(manifest.GetProperty("payloadSha256").GetString()));
    }

    [Test]
    public async Task Cbi34_C2_evidence_has_an_explicit_key_identity_and_algorithm()
    {
        using var fixture = await Cbi34FixtureAsync();
        var manifest = fixture.RootElement.GetProperty("canonicalManifest");
        foreach (var id in new[] { "cbi34-02-not-provided", "cbi34-03-unsupported", "cbi34-04-malformed-key", "cbi34-05-key-id-mismatch" })
        {
            var vector = fixture.RootElement.GetProperty("vectors").EnumerateArray()
                .Single(item => item.GetProperty("id").GetString() == id);
            var observation = Cbi34Run(manifest, vector);
            Assert.That(observation.Code, Is.EqualTo(vector.GetProperty("code").GetString()));
            Assert.That(observation.Verified, Is.False);
        }

        var request = Cbi34CanonicalRequest(manifest);
        using var wrongCurve = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var wrongCurveEvidence = Cbi34Sign(request, wrongCurve, "none");
        Assert.That(ProviderArtifactPublisherEvidenceVerifier.Verify(request, wrongCurveEvidence).Code,
            Is.EqualTo("publisher-evidence-malformed"));
    }

    [Test]
    public async Task Cbi34_C3_verification_binds_the_exact_canonical_payload()
    {
        using var fixture = await Cbi34FixtureAsync();
        var manifest = fixture.RootElement.GetProperty("canonicalManifest");
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray()
                     .Where(item => item.GetProperty("id").GetString() is
                         "cbi34-06-signature-changed" or "cbi34-07-path-changed" or
                         "cbi34-08-digest-changed" or "cbi34-09-length-changed" or
                         "cbi34-10-executable-changed" or "cbi34-11-arguments-changed"))
        {
            Assert.That(Cbi34Run(manifest, vector).Code, Is.EqualTo("publisher-evidence-invalid"));
        }
    }

    [Test]
    public async Task Cbi34_C4_validity_is_not_trust_or_admission()
    {
        using var fixture = await Cbi34FixtureAsync();
        var valid = fixture.RootElement.GetProperty("vectors").EnumerateArray().First();
        var observation = Cbi34Run(fixture.RootElement.GetProperty("canonicalManifest"), valid);
        Assert.Multiple(() =>
        {
            Assert.That(observation.Verified, Is.True);
            Assert.That(observation.TrustCode, Is.EqualTo("publisher-trust-not-evaluated"));
            Assert.That(observation.AdmissionCode, Is.EqualTo("admission-not-attempted"));
        });
    }

    [Test]
    [Category("CrossProcess")]
    public void Cbi34_C5_explicit_caller_policy_may_compose_valid_evidence_with_cbi33()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi34-{Guid.NewGuid():N}");
        try
        {
            var (request, source) = Cbi33Input("reference", "none");
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var verification = ProviderArtifactPublisherEvidenceVerifier.Verify(request, Cbi34Sign(request, key, "none"));
            Assert.That(verification.IsVerified, Is.True);
            var store = new ContentAddressedProviderStore(Path.Combine(testRoot, "store"));
            var acquisition = new ProviderArtifactAcquirer(store, Path.Combine(testRoot, "transactions"))
                .Acquire(request, source);
            Assert.Multiple(() =>
            {
                Assert.That(verification.AdmissionCode, Is.EqualTo("admission-not-attempted"));
                Assert.That(acquisition.TransportCode, Is.EqualTo("transport-completed"));
                Assert.That(acquisition.AdmissionCode, Is.EqualTo("staged"));
            });
            Assert.That(store.Remove(request.Identity).Code, Is.EqualTo("removed"));
        }
        finally
        {
            Cbi32DeleteTree(testRoot);
        }
    }

    [Test]
    public async Task Cbi34_C6_both_roots_agree_on_portable_observations()
    {
        using var fixture = await Cbi34FixtureAsync();
        var manifest = fixture.RootElement.GetProperty("canonicalManifest");
        var vectors = fixture.RootElement.GetProperty("vectors").EnumerateArray().ToArray();
        Assert.That(vectors.Select(vector => Cbi34Run(manifest, vector).Code),
            Is.EqualTo(vectors.Select(vector => vector.GetProperty("code").GetString())));
    }
}
