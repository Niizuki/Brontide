using Brontide.Reference.Experimental.Binding.Portable;
using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed record Cbi44Observation(
        string Code,
        string? RefusedBy,
        bool PolicyApplied,
        bool Authorized,
        bool SourceOpened,
        bool Staged,
        bool Revalidated,
        bool Launched,
        bool Released,
        bool? LaunchPolicyChanged,
        long RegistrySequence,
        long StoredFloor,
        bool StagedSetRemains,
        bool ProviderRunning,
        bool LaunchPolicyIsCurrent,
        bool LaunchAdmitsPublisher,
        bool StagedIsRequested);

    /// <summary>
    /// The window CBI44 closes exists only while one chain call is in flight, so the fixture advances
    /// the registry from the artifact source — the same device CBI41 uses to reach CBI39's superseded
    /// cursor. The write lands after the governed acquirer has already checked supersession, which is
    /// what makes it the post-acquisition window rather than CBI36's.
    /// </summary>
    private sealed class Cbi44Source(MemoryArtifactSource inner, Action onFirstOpen) : IProviderArtifactSource
    {
        private bool _advanced;

        public ProviderArtifactSourceId Identity => inner.Identity;

        public int OpenCount => inner.OpenCount;

        public Stream? OpenRead(string relativePath)
        {
            if (!_advanced)
            {
                _advanced = true;
                onFirstOpen();
            }

            return inner.OpenRead(relativePath);
        }
    }

    private static readonly ProviderPublisherKeyId Cbi44OtherPublisher =
        ProviderPublisherKeyId.Create(new string('B', 64));

    private static ProviderPublisherTrustPolicy Cbi44Policy(params ProviderPublisherTrustEntry[] entries) =>
        new(ProviderPublisherTrustPolicyIdentity.Compute(entries), entries);

    private static ProviderPublisherTrustEntry Cbi44Entry(ProviderPublisherKeyId key, bool revoked) =>
        new(key, revoked
            ? ProviderPublisherTrustDisposition.Revoked
            : ProviderPublisherTrustDisposition.Admitted);

    private static ProviderPublisherTrustPolicy? Cbi44Successor(string mutation, ProviderPublisherKeyId publisher) =>
        mutation switch
        {
            "revoked-at-launch" => Cbi44Policy(Cbi44Entry(publisher, revoked: true)),
            // The successor simply stops naming the publisher, which CBI35 keeps distinct from revoking it.
            "removed-at-launch" => Cbi44Policy(Cbi44Entry(Cbi44OtherPublisher, revoked: false)),
            // A real policy update that has nothing to do with this publisher. It moves the policy
            // identity, so a chain comparing snapshots rather than decisions refuses here.
            "unrelated-revocation" => Cbi44Policy(
                Cbi44Entry(publisher, revoked: false),
                Cbi44Entry(Cbi44OtherPublisher, revoked: true)),
            _ => null,
        };

    private static async Task<Cbi44Observation> Cbi44RunAsync(JsonElement vector)
    {
        var mutation = vector.GetProperty("mutation").GetString()!;
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi44-{Guid.NewGuid():N}");
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

            var (request, memory) = Cbi33Input("reference", "none");
            var evidence = Cbi43Evidence(publisher, request);
            var initial = Cbi44Policy(
                Cbi44Entry(evidence.PublisherKeyId, mutation == "revoked-before-acquisition"));

            // 1. Custody, then one poll that applies exactly the first policy.
            var custody = ProviderPublisherTrustPolicyCustody.Open(
                Path.Combine(root, "policy.checkpoint"), Path.Combine(root, "policy.floor"), authorityId);
            Assert.That(custody.IsOpened, Is.True);
            var registry = custody.Registry!;
            var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
            var update = Cbi37Sign(authority, 1, null, initial);
            var served = 0;
            var pollSource = new Cbi39Source((distribution, _) => Task.FromResult(
                Cbi41RespondWith(served++ == 0 ? "update" : "current", distribution, endpointKey, update, now)));
            var poll = await new ProviderPublisherTrustPolicyPoller(
                    registry, endpointId,
                    ProviderPublisherTrustPolicyPollSchedule.Create(
                        4, TimeSpan.FromSeconds(1), 2, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1)))
                .PollAsync(pollSource, custody.Floors!, new Cbi42Delay(), now);
            Assert.That(poll.Code, Is.EqualTo("policy-poll-current"));

            // 2. The successor is applied from inside the acquisition rather than by a second poll,
            // so no floor is handed off and the stored floor stays behind the live sequence — which
            // is CBI41's lagging floor, not a defect in this chain.
            var successor = Cbi44Successor(mutation, evidence.PublisherKeyId);
            var source = new Cbi44Source(memory, () =>
            {
                if (successor is not null)
                {
                    Assert.That(
                        registry.Apply(Cbi37Sign(authority, 2, initial.Identity, successor)).IsApplied,
                        Is.True, "the fixture's own successor must apply");
                }
            });

            var store = new ContentAddressedProviderStore(Path.Combine(root, "store"));
            var chain = ProviderDistributionChain.Run(
                registry, store, Path.Combine(root, "transactions"),
                new(request, evidence, mutation == "launch-refused" ? ["--not-allowed"] : ["--portable"]),
                source);
            provider = chain.Provider;

            var released = false;
            var code = chain.Code;
            var refusedBy = chain.RefusedBy;
            if (provider is not null)
            {
                var (resolution, selection, occurrence) = LifecycleInput();
                var lifecycle = await ComponentBindingLifecycle.ActivateAsync(
                    resolution, selection, RuntimeRequest(Plan(occurrence)), provider.Conversation);
                released = lifecycle.Member?.IsReleased == true;
                code = lifecycle.Failure?.Code ?? "active";
                refusedBy = lifecycle.IsActive ? null : "cbi30";
                if (lifecycle.Member is not null)
                {
                    if (lifecycle.IsActive) await lifecycle.Member.RetireAsync("CBI44 chain completed.");
                    await lifecycle.Member.DisposeAsync();
                }
            }

            var running = false;
            if (provider is not null)
            {
                running = !await provider.WaitForExitAsync(TimeSpan.FromSeconds(5));
                await provider.DisposeAsync();
                store.Remove(chain.StagedIdentity!.Value);
                provider = null;
            }

            var final = registry.Current!;
            var storeRoot = Path.GetFullPath(Path.Combine(root, "store"));
            return new(
                code,
                refusedBy,
                PolicyApplied: true,
                chain.Authorized,
                memory.OpenCount > 0,
                chain.Staged,
                chain.Revalidated,
                chain.IsLaunched,
                released,
                chain.LaunchPolicyIdentity is null
                    ? null
                    : chain.LaunchPolicyIdentity != chain.AcquisitionPolicyIdentity,
                final.Sequence,
                custody.Floors!.Stored.Sequence,
                Directory.Exists(storeRoot) && Directory.EnumerateDirectories(storeRoot).Any(),
                running,
                chain.LaunchPolicyIdentity is null || chain.LaunchPolicyIdentity == final.Policy.Identity,
                final.Policy.Entries.Any(entry => entry.PublisherKeyId == evidence.PublisherKeyId
                    && entry.Disposition == ProviderPublisherTrustDisposition.Admitted),
                chain.StagedIdentity is null || chain.StagedIdentity == request.Identity);
        }
        finally
        {
            if (provider is not null) await provider.DisposeAsync();
            Cbi32DeleteTree(root);
        }
    }

    private static JsonDocument Cbi44Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi44-launch-revalidation-vectors.json")));

    private static async Task<Cbi44Observation> Cbi44RunAsync(JsonDocument fixture, string mutation) =>
        await Cbi44RunAsync(fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(vector => vector.GetProperty("mutation").GetString() == mutation));

    [Test]
    [Category("CrossProcess")]
    public async Task Shared_cbi44_vectors_revalidate_trust_between_acquisition_and_launch()
    {
        using var fixture = Cbi44Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi44RunAsync(vector);
            var label = vector.GetProperty("mutation").GetString();
            var expectedChange = vector.GetProperty("launchPolicyChanged");
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()), label);
                Assert.That(actual.RefusedBy, Is.EqualTo(vector.GetProperty("refusedBy").GetString()), label);
                Assert.That(actual.PolicyApplied,
                    Is.EqualTo(vector.GetProperty("policyApplied").GetBoolean()), label);
                Assert.That(actual.Authorized, Is.EqualTo(vector.GetProperty("authorized").GetBoolean()), label);
                Assert.That(actual.SourceOpened,
                    Is.EqualTo(vector.GetProperty("sourceOpened").GetBoolean()), label);
                Assert.That(actual.Staged, Is.EqualTo(vector.GetProperty("staged").GetBoolean()), label);
                Assert.That(actual.Revalidated,
                    Is.EqualTo(vector.GetProperty("revalidated").GetBoolean()), label);
                Assert.That(actual.Launched, Is.EqualTo(vector.GetProperty("launched").GetBoolean()), label);
                Assert.That(actual.Released, Is.EqualTo(vector.GetProperty("released").GetBoolean()), label);
                Assert.That(actual.LaunchPolicyChanged,
                    Is.EqualTo(expectedChange.ValueKind == JsonValueKind.Null
                        ? null
                        : (bool?)expectedChange.GetBoolean()), label);
                Assert.That(actual.RegistrySequence,
                    Is.EqualTo(vector.GetProperty("registrySequence").GetInt64()), label);
                Assert.That(actual.StoredFloor, Is.EqualTo(vector.GetProperty("storedFloor").GetInt64()), label);
                Assert.That(actual.StagedSetRemains,
                    Is.EqualTo(vector.GetProperty("stagedSetRemains").GetBoolean()), label);
                Assert.That(actual.ProviderRunning,
                    Is.EqualTo(vector.GetProperty("providerRunning").GetBoolean()), label);

                // Phase-wide properties, over every vector rather than per case.
                bool[] ladder = [actual.PolicyApplied, actual.Authorized, actual.SourceOpened,
                    actual.Staged, actual.Revalidated, actual.Launched, actual.Released];
                Assert.That(ladder.SkipWhile(reached => reached).Any(reached => reached), Is.False,
                    $"{label}: the ladder must be a true-prefix");
                if (actual.Launched) Assert.That(actual.LaunchAdmitsPublisher, Is.True, label);
                Assert.That(actual.LaunchPolicyIsCurrent, Is.True, label);
                Assert.That(actual.StagedIsRequested, Is.True, label);
                if (!actual.Launched)
                {
                    Assert.That(actual.StagedSetRemains, Is.False, label);
                    Assert.That(actual.ProviderRunning, Is.False, label);
                }
            });
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi44_C1_the_launch_decision_is_taken_not_remembered()
    {
        using var fixture = Cbi44Fixture();
        var actual = await Cbi44RunAsync(fixture, "complete");
        Assert.Multiple(() =>
        {
            Assert.That(actual.Revalidated, Is.True);
            Assert.That(actual.Released, Is.True);
            // Nothing moved, so the two decisions name the same policy — and both were taken.
            Assert.That(actual.LaunchPolicyChanged, Is.False);
            Assert.That(actual.LaunchPolicyIsCurrent, Is.True);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi44_C2_a_publisher_the_current_policy_no_longer_admits_does_not_launch()
    {
        using var fixture = Cbi44Fixture();
        foreach (var mutation in new[] { "revoked-at-launch", "removed-at-launch" })
        {
            var actual = await Cbi44RunAsync(fixture, mutation);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Staged, Is.True, mutation);
                Assert.That(actual.Revalidated, Is.False, mutation);
                Assert.That(actual.Launched, Is.False, mutation);
                Assert.That(actual.RefusedBy, Is.EqualTo("cbi35"), mutation);
            });
        }

        // The same code and the same origin as an acquisition-time revocation. Only the ladder
        // separates them, which is what CBI43's C2 exists for.
        var early = await Cbi44RunAsync(fixture, "revoked-before-acquisition");
        var late = await Cbi44RunAsync(fixture, "revoked-at-launch");
        Assert.Multiple(() =>
        {
            Assert.That(late.Code, Is.EqualTo(early.Code));
            Assert.That(late.RefusedBy, Is.EqualTo(early.RefusedBy));
            Assert.That(early.Authorized, Is.False);
            Assert.That(late.Authorized, Is.True);
            Assert.That(early.SourceOpened, Is.False);
            Assert.That(late.SourceOpened, Is.True);
            Assert.That(early.Staged, Is.False);
            Assert.That(late.Staged, Is.True);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi44_C3_a_changed_policy_that_still_admits_the_publisher_launches()
    {
        using var fixture = Cbi44Fixture();
        var actual = await Cbi44RunAsync(fixture, "unrelated-revocation");
        Assert.Multiple(() =>
        {
            // The snapshot moved and the decision did not, so a chain comparing policy identities
            // would refuse this and a chain comparing decisions runs it.
            Assert.That(actual.LaunchPolicyChanged, Is.True);
            Assert.That(actual.RegistrySequence, Is.EqualTo(2));
            Assert.That(actual.Revalidated, Is.True);
            Assert.That(actual.Released, Is.True);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi44_C4_a_refused_launch_leaves_no_staged_set_process_or_advanced_floor()
    {
        using var fixture = Cbi44Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Where(vector => !vector.GetProperty("released").GetBoolean()))
        {
            var actual = await Cbi44RunAsync(vector);
            var label = vector.GetProperty("mutation").GetString();
            Assert.Multiple(() =>
            {
                Assert.That(actual.StagedSetRemains, Is.False, label);
                Assert.That(actual.ProviderRunning, Is.False, label);
                // One poll applied one update, so the floor is one however far the live registry ran.
                Assert.That(actual.StoredFloor, Is.EqualTo(1), label);
            });
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi44_C5_the_ladder_gains_a_stage_and_stays_a_true_prefix()
    {
        using var fixture = Cbi44Fixture();
        // A refusal after the launch decision proves the new stage sits before launch rather than
        // standing in for it: revalidated is true and launched is false in the same vector.
        var actual = await Cbi44RunAsync(fixture, "launch-refused");
        Assert.Multiple(() =>
        {
            Assert.That(actual.Revalidated, Is.True);
            Assert.That(actual.Launched, Is.False);
            Assert.That(actual.RefusedBy, Is.EqualTo("cbi31"));
        });
    }
}
