using Brontide.Reference.Experimental.Binding.Portable;
using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed record Cbi43Observation(
        string Code,
        string? RefusedBy,
        bool PolicyApplied,
        bool Authorized,
        bool SourceOpened,
        bool Staged,
        bool Launched,
        bool Released,
        long StoredFloor,
        bool StagedSetRemains,
        bool ProviderRunning,
        bool ExecutableInsideStore);

    private static ProviderPublisherEvidence Cbi43Evidence(
        ECDsa key, ProviderArtifactAcquisitionRequest request)
    {
        var publicKey = key.ExportSubjectPublicKeyInfo();
        return new(
            ProviderPublisherKeyId.Create(Convert.ToHexString(SHA256.HashData(publicKey))),
            "ECDSA-P256-SHA256",
            Convert.ToBase64String(publicKey),
            Convert.ToBase64String(key.SignData(
                ProviderArtifactPublisherManifest.Encode(request),
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)));
    }

    private static ProviderPublisherTrustPolicy Cbi43Policy(ProviderPublisherKeyId key, bool revoked)
    {
        ProviderPublisherTrustEntry[] entries = [new(key, revoked
            ? ProviderPublisherTrustDisposition.Revoked
            : ProviderPublisherTrustDisposition.Admitted)];
        return new(ProviderPublisherTrustPolicyIdentity.Compute(entries), entries);
    }

    private static async Task<Cbi43Observation> Cbi43RunAsync(JsonElement vector)
    {
        var mutation = vector.GetProperty("mutation").GetString()!;
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi43-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        StagedProviderProcess? provider = null;
        try
        {
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var publisher = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var endpointKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var authorityId = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo())));
            var endpointId = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
                Convert.ToHexString(SHA256.HashData(endpointKey.ExportSubjectPublicKeyInfo())));

            var (request, source) = Cbi33Input(
                "reference", mutation == "delivered-digest-mismatch" ? "digest" : "none");
            var evidence = Cbi43Evidence(publisher, request);
            var admitted = mutation == "publisher-unknown"
                ? ProviderPublisherKeyId.Create(new string('7', 64))
                : evidence.PublisherKeyId;

            // 1. Custody, then one poll that either delivers the policy or does not.
            var custody = ProviderPublisherTrustPolicyCustody.Open(
                Path.Combine(root, "policy.checkpoint"), Path.Combine(root, "policy.floor"), authorityId);
            Assert.That(custody.IsOpened, Is.True);
            var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
            var update = Cbi37Sign(authority, 1, null,
                Cbi43Policy(admitted, mutation == "publisher-revoked"));
            var served = 0;
            var pollSource = new Cbi39Source((distribution, _) =>
            {
                // "policy-undelivered" answers current forever, so nothing is ever applied.
                var kind = mutation != "policy-undelivered" && served++ == 0 ? "update" : "current";
                return Task.FromResult(Cbi41RespondWith(kind, distribution, endpointKey, update, now));
            });
            var poll = await new ProviderPublisherTrustPolicyPoller(
                    custody.Registry!, endpointId,
                    ProviderPublisherTrustPolicyPollSchedule.Create(
                        4, TimeSpan.FromSeconds(1), 2, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1)))
                .PollAsync(pollSource, custody.Floors!, new Cbi42Delay(), now);
            Assert.That(poll.Code, Is.EqualTo("policy-poll-current"));
            var policyApplied = custody.Registry!.Current is not null;

            // 2-5. Evidence, trust, governed acquisition, staging, and launch.
            var store = new ContentAddressedProviderStore(Path.Combine(root, "store"));
            var chain = ProviderDistributionChain.Run(
                custody.Registry, store, Path.Combine(root, "transactions"),
                new(request, mutation == "evidence-unsigned" ? null : evidence,
                    mutation == "launch-refused" ? ["--not-allowed"] : ["--portable"]),
                source);
            provider = chain.Provider;

            var released = false;
            var code = chain.Code;
            var refusedBy = chain.RefusedBy;
            var storeRoot = Path.GetFullPath(Path.Combine(root, "store"));
            // The executable that ran must live inside the content-addressed store, not at the path
            // the caller named as the acquisition source.
            var executableInsideStore = chain.StagedExecutablePath is not null
                && chain.StagedExecutablePath.StartsWith(storeRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && File.Exists(chain.StagedExecutablePath);
            var providerStoppedEarly = false;
            if (provider is not null)
            {
                // 6. CBI30 activation across the launched provider's own conversation.
                if (mutation == "provider-lost")
                {
                    await provider.DisposeAsync();
                    providerStoppedEarly = true;
                }
                var (resolution, selection, occurrence) = LifecycleInput();
                var lifecycle = await ComponentBindingLifecycle.ActivateAsync(
                    resolution, selection, RuntimeRequest(Plan(occurrence)), provider.Conversation);
                released = lifecycle.Member?.IsReleased == true;
                code = lifecycle.Failure?.Code ?? "active";
                refusedBy = lifecycle.IsActive ? null : "cbi30";
                if (lifecycle.Member is not null)
                {
                    if (lifecycle.IsActive) await lifecycle.Member.RetireAsync("CBI43 chain completed.");
                    await lifecycle.Member.DisposeAsync();
                }
            }

            // The question is whether the chain leaves a process behind once it has returned, so the
            // exit is observed after teardown rather than during it.
            var running = false;
            if (provider is not null)
            {
                if (!providerStoppedEarly)
                {
                    running = !await provider.WaitForExitAsync(TimeSpan.FromSeconds(5));
                    await provider.DisposeAsync();
                }
                // The lease is released by disposal, so removal here is what the chain owes a host.
                store.Remove(chain.StagedIdentity!.Value);
                provider = null;
            }

            return new(code, refusedBy, policyApplied, chain.Authorized, source.OpenCount > 0,
                chain.Staged, chain.IsLaunched, released,
                custody.Floors!.Stored.Sequence,
                Directory.Exists(storeRoot) && Directory.EnumerateDirectories(storeRoot).Any(),
                running, executableInsideStore);
        }
        finally
        {
            if (provider is not null) await provider.DisposeAsync();
            Cbi32DeleteTree(root);
        }
    }

    private static ProviderPublisherTrustPolicyDistributionResponse Cbi41RespondWith(
        string kind,
        ProviderPublisherTrustPolicyDistributionRequest request,
        ECDsa endpointKey,
        ProviderPublisherTrustPolicyUpdate update,
        DateTimeOffset now)
    {
        var selected = kind == "update" ? update : null;
        var issued = now.ToUnixTimeSeconds();
        var expires = now.AddMinutes(1).ToUnixTimeSeconds();
        var signature = endpointKey.SignData(
            ProviderPublisherTrustPolicyDistributionManifest.Encode(
                request.Challenge, request.CurrentSequence, request.CurrentPolicyIdentity,
                issued, expires, selected),
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        return new(request.Challenge, request.CurrentSequence, request.CurrentPolicyIdentity,
            issued, expires, selected, "ECDSA-P256-SHA256",
            Convert.ToBase64String(endpointKey.ExportSubjectPublicKeyInfo()),
            Convert.ToBase64String(signature));
    }

    private static JsonDocument Cbi43Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi43-distribution-chain-vectors.json")));

    private static async Task<Cbi43Observation> Cbi43RunAsync(JsonDocument fixture, string mutation) =>
        await Cbi43RunAsync(fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(vector => vector.GetProperty("mutation").GetString() == mutation));

    [Test]
    [Category("CrossProcess")]
    public async Task Shared_cbi43_vectors_run_the_distribution_chain_end_to_end()
    {
        using var fixture = Cbi43Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi43RunAsync(vector);
            var label = vector.GetProperty("mutation").GetString();
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()), label);
                Assert.That(actual.RefusedBy,
                    Is.EqualTo(vector.GetProperty("refusedBy").GetString()), label);
                Assert.That(actual.PolicyApplied,
                    Is.EqualTo(vector.GetProperty("policyApplied").GetBoolean()), label);
                Assert.That(actual.Authorized, Is.EqualTo(vector.GetProperty("authorized").GetBoolean()), label);
                Assert.That(actual.SourceOpened,
                    Is.EqualTo(vector.GetProperty("sourceOpened").GetBoolean()), label);
                Assert.That(actual.Staged, Is.EqualTo(vector.GetProperty("staged").GetBoolean()), label);
                Assert.That(actual.Launched, Is.EqualTo(vector.GetProperty("launched").GetBoolean()), label);
                Assert.That(actual.Released, Is.EqualTo(vector.GetProperty("released").GetBoolean()), label);
                Assert.That(actual.StoredFloor,
                    Is.EqualTo(vector.GetProperty("storedFloor").GetInt64()), label);
                Assert.That(actual.StagedSetRemains,
                    Is.EqualTo(vector.GetProperty("stagedSetRemains").GetBoolean()), label);
                Assert.That(actual.ProviderRunning,
                    Is.EqualTo(vector.GetProperty("providerRunning").GetBoolean()), label);

                // Phase-wide properties, over every vector rather than per case.
                bool[] ladder = [actual.PolicyApplied, actual.Authorized, actual.SourceOpened,
                    actual.Staged, actual.Launched, actual.Released];
                Assert.That(ladder.SkipWhile(reached => reached).Any(reached => reached), Is.False,
                    $"{label}: the ladder must be a true-prefix");
                Assert.That(actual.StagedSetRemains, Is.False, label);
                Assert.That(actual.ProviderRunning, Is.False, label);
                if (!actual.SourceOpened) Assert.That(actual.Staged, Is.False, label);
            });
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi43_C1_the_chain_composes_from_polled_policy_to_released_member()
    {
        using var fixture = Cbi43Fixture();
        var actual = await Cbi43RunAsync(fixture, "complete");
        Assert.Multiple(() =>
        {
            Assert.That(actual.Code, Is.EqualTo("active"));
            Assert.That(actual.Released, Is.True);
            Assert.That(actual.PolicyApplied, Is.True);
            Assert.That(actual.Launched, Is.True);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi43_C3_a_policy_that_never_applied_opens_no_source()
    {
        using var fixture = Cbi43Fixture();
        foreach (var mutation in new[] { "policy-undelivered", "publisher-revoked", "publisher-unknown", "evidence-unsigned" })
        {
            var actual = await Cbi43RunAsync(fixture, mutation);
            Assert.Multiple(() =>
            {
                Assert.That(actual.SourceOpened, Is.False, mutation);
                Assert.That(actual.Staged, Is.False, mutation);
                Assert.That(actual.Launched, Is.False, mutation);
            });
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi43_C4_a_refusal_leaves_no_staged_set_process_or_advanced_floor()
    {
        using var fixture = Cbi43Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Where(vector => !vector.GetProperty("released").GetBoolean()))
        {
            var actual = await Cbi43RunAsync(vector);
            var label = vector.GetProperty("mutation").GetString();
            Assert.Multiple(() =>
            {
                Assert.That(actual.StagedSetRemains, Is.False, label);
                Assert.That(actual.ProviderRunning, Is.False, label);
                Assert.That(actual.StoredFloor, Is.EqualTo(actual.PolicyApplied ? 1 : 0), label);
            });
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi43_C6_the_provider_launched_is_the_artifact_acquired()
    {
        using var fixture = Cbi43Fixture();
        var actual = await Cbi43RunAsync(fixture, "complete");
        // The executable ran from inside the content-addressed store rather than from the source the
        // caller named, so activation used the bytes the publisher signed.
        Assert.That(actual.ExecutableInsideStore, Is.True);
    }
}
