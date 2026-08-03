using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed record Cbi42Observation(
        string Code,
        string? CheckpointCode,
        bool Opened,
        long StoredBefore,
        long StoredAfter,
        bool StoreChanged);

    private sealed class Cbi42Delay : IProviderPublisherTrustPolicyPollDelay
    {
        public Task<DateTimeOffset> DelayAsync(DateTimeOffset now, TimeSpan duration, CancellationToken cancellationToken) =>
            Task.FromResult(now + duration);
    }

    private static string Cbi42FloorPath(string root) => Path.Combine(root, "policy.floor");
    private static string Cbi42CheckpointPath(string root) => Path.Combine(root, "policy.checkpoint");

    /// <summary>
    /// Applies <paramref name="count"/> further chained updates from wherever the registry stands,
    /// retaining each floor when a store is given, and answers the last floor issued.
    /// </summary>
    private static ProviderPublisherTrustPolicyRecoveryFloor Cbi42Seed(
        DurableProviderPublisherTrustPolicyRegistry registry,
        DurableProviderPublisherTrustPolicyFloorStore? store,
        ECDsa authority,
        long count,
        long policyOffset = 0)
    {
        var floor = registry.Floor;
        for (var index = 0L; index < count; index++)
        {
            var sequence = (registry.Current?.Sequence ?? 0) + 1;
            var applied = registry.Apply(Cbi37Sign(
                authority, sequence, registry.Current?.Policy.Identity, Cbi41Policy(sequence + policyOffset)));
            Assert.That(applied.IsApplied, Is.True);
            if (store is not null) Assert.That(store.Retain(applied.Floor).IsRetained, Is.True);
            floor = applied.Floor;
        }
        return floor;
    }

    /// <summary>
    /// A floor cannot be fabricated, only issued, so every candidate a retention vector offers comes
    /// from a real application against a throwaway registry.
    /// </summary>
    private static ProviderPublisherTrustPolicyRecoveryFloor Cbi42IssuedFloor(
        string root,
        string name,
        ECDsa signer,
        ProviderPublisherTrustPolicyAuthorityId authorityId,
        long count,
        long policyOffset = 0)
    {
        var registry = DurableProviderPublisherTrustPolicyRegistry.Open(
            Path.Combine(root, name), authorityId).Registry!;
        return Cbi42Seed(registry, null, signer, count, policyOffset);
    }

    private static async Task<Cbi42Observation> Cbi42RunAsync(JsonElement vector)
    {
        var mutation = vector.GetProperty("mutation").GetString()!;
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi42-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var foreignAuthority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var endpointKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var authorityId = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo())));
            var foreignId = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(foreignAuthority.ExportSubjectPublicKeyInfo())));
            var floorPath = Cbi42FloorPath(root);
            var checkpointPath = Cbi42CheckpointPath(root);

            if (vector.GetProperty("kind").GetString() == "retain")
                return Cbi42Retain(mutation, authority, foreignAuthority, authorityId, foreignId,
                    root, floorPath, checkpointPath);
            if (vector.GetProperty("kind").GetString() == "cycle")
                return await Cbi42CycleAsync(mutation, authority, endpointKey, authorityId, floorPath, checkpointPath);
            return Cbi42Start(mutation, authority, authorityId, foreignId, floorPath, checkpointPath,
                vector.TryGetProperty("tamperOffset", out var offset) ? offset.GetInt32() : 0);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static Cbi42Observation Cbi42Retain(
        string mutation,
        ECDsa authority,
        ECDsa foreignAuthority,
        ProviderPublisherTrustPolicyAuthorityId authorityId,
        ProviderPublisherTrustPolicyAuthorityId foreignId,
        string root,
        string floorPath,
        string checkpointPath)
    {
        var opened = DurableProviderPublisherTrustPolicyFloorStore.Open(floorPath, authorityId);
        if (mutation == "establish")
            return new(opened.Code, null, false, 0, opened.Store!.Stored.Sequence, File.Exists(floorPath));

        var store = opened.Store!;
        var registry = DurableProviderPublisherTrustPolicyRegistry.Open(checkpointPath, authorityId).Registry!;
        // Bring the store to the sequence the vector says it holds before the retention under test.
        Cbi42Seed(registry, store, authority,
            mutation switch { "retain-first" => 0, "retain-regressed" => 2, _ => 1 });

        var before = store.Stored;
        var bytesBefore = File.ReadAllBytes(floorPath);
        var candidate = mutation switch
        {
            "retain-first" or "retain-advance" => Cbi42Seed(registry, null, authority, 1),
            "retain-identical" => before,
            // Same sequence, different policy: a fork rather than an advance.
            "retain-forked" => Cbi42IssuedFloor(root, "fork.checkpoint", authority, authorityId, 1, 98),
            "retain-regressed" => Cbi42IssuedFloor(root, "older.checkpoint", authority, authorityId, 1),
            // A sequence that would otherwise advance, under an authority that is not the pinned one.
            _ => Cbi42IssuedFloor(root, "foreign.checkpoint", foreignAuthority, foreignId, 2),
        };
        var result = store.Retain(candidate);
        return new(result.Code, null, false, before.Sequence, store.Stored.Sequence,
            !bytesBefore.SequenceEqual(File.ReadAllBytes(floorPath)));
    }

    private static Cbi42Observation Cbi42Start(
        string mutation,
        ECDsa authority,
        ProviderPublisherTrustPolicyAuthorityId authorityId,
        ProviderPublisherTrustPolicyAuthorityId foreignId,
        string floorPath,
        string checkpointPath,
        int tamperOffset)
    {
        var seeded = mutation switch
        {
            "start-fresh" or "start-guard-removed" => 0,
            "start-lagging-floor" => 1,
            _ => 2,
        };
        if (mutation != "start-fresh")
        {
            var store = DurableProviderPublisherTrustPolicyFloorStore.Open(floorPath, authorityId).Store!;
            var registry = DurableProviderPublisherTrustPolicyRegistry.Open(checkpointPath, authorityId).Registry!;
            // The lagging vector publishes one update the store never saw, which is CBI41's crash
            // window: the checkpoint reaches two while the retained floor stayed at one.
            Cbi42Seed(registry, store, authority, seeded);
            if (mutation == "start-lagging-floor") Cbi42Seed(registry, null, authority, 1);
            if (mutation == "start-guard-removed") Cbi42Seed(registry, null, authority, 1);
        }

        switch (mutation)
        {
            case "start-rolled-back":
                {
                    // A genuine older checkpoint replaces the current one, which is the rollback the
                    // floor exists to catch.
                    var older = Path.Combine(Path.GetDirectoryName(checkpointPath)!, "older.checkpoint");
                    var shadow = DurableProviderPublisherTrustPolicyRegistry.Open(older, authorityId).Registry!;
                    Cbi42Seed(shadow, null, authority, 1);
                    File.Copy(older, checkpointPath, true);
                    break;
                }
            case "start-checkpoint-removed": File.Delete(checkpointPath); break;
            case "start-guard-removed": File.Delete(floorPath); break;
            case "start-corrupt-store":
                {
                    // The version marker: refused by structure before the tag is consulted.
                    var bytes = File.ReadAllBytes(floorPath);
                    bytes[8] ^= 1;
                    File.WriteAllBytes(floorPath, bytes);
                    break;
                }
            case "start-tampered-sequence":
                {
                    // A byte the parser would happily accept — a different but well-formed sequence.
                    // Only the integrity tag can refuse this one.
                    var bytes = File.ReadAllBytes(floorPath);
                    bytes[tamperOffset] ^= 1;
                    File.WriteAllBytes(floorPath, bytes);
                    break;
                }
            case "start-truncated-store":
                File.WriteAllBytes(floorPath, File.ReadAllBytes(floorPath)[..^4]);
                break;
            case "start-trailing-store":
                File.WriteAllBytes(floorPath, [.. File.ReadAllBytes(floorPath), 0]);
                break;
            case "start-foreign-store":
                File.WriteAllBytes(floorPath, DurableProviderPublisherTrustPolicyFloorStore.EncodeRecord(
                    foreignId, 2, Cbi41Policy(2).Identity));
                break;
        }

        var before = File.Exists(floorPath) ? File.ReadAllBytes(floorPath) : [];
        var storedBefore = Cbi42StoredSequence(floorPath, authorityId, foreignId);
        var custody = ProviderPublisherTrustPolicyCustody.Open(checkpointPath, floorPath, authorityId);
        var after = File.Exists(floorPath) ? File.ReadAllBytes(floorPath) : [];
        return new(custody.Code, custody.CheckpointCode, custody.Registry is not null,
            storedBefore, Cbi42StoredSequence(floorPath, authorityId, foreignId), !before.SequenceEqual(after));
    }

    private static async Task<Cbi42Observation> Cbi42CycleAsync(
        string mutation,
        ECDsa authority,
        ECDsa endpointKey,
        ProviderPublisherTrustPolicyAuthorityId authorityId,
        string floorPath,
        string checkpointPath)
    {
        var endpointId = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
            Convert.ToHexString(SHA256.HashData(endpointKey.ExportSubjectPublicKeyInfo())));
        var first = ProviderPublisherTrustPolicyCustody.Open(checkpointPath, floorPath, authorityId);
        Assert.That(first.IsOpened, Is.True);
        var storedBefore = first.Floors!.Stored.Sequence;

        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        var served = 0;
        using var foreign = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var source = new Cbi39Source((request, _) =>
        {
            var kind = served++ < 2 ? "update" : "current";
            return Task.FromResult(Cbi41Respond(kind, request, endpointKey, endpointKey, authority, foreign, now));
        });
        var schedule = ProviderPublisherTrustPolicyPollSchedule.Create(
            6, TimeSpan.FromSeconds(1), 4, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
        var cycle = await new ProviderPublisherTrustPolicyPoller(first.Registry!, endpointId, schedule)
            .PollAsync(source, first.Floors, new Cbi42Delay(), now);
        Assert.Multiple(() =>
        {
            Assert.That(cycle.Code, Is.EqualTo("policy-poll-current"));
            Assert.That(cycle.RetainedSequences, Is.EqualTo(new long[] { 1, 2 }));
        });

        if (mutation == "cycle-then-rollback")
        {
            var older = Path.Combine(Path.GetDirectoryName(checkpointPath)!, "older.checkpoint");
            var shadow = DurableProviderPublisherTrustPolicyRegistry.Open(older, authorityId).Registry!;
            Cbi42Seed(shadow, null, authority, 1);
            File.Copy(older, checkpointPath, true);
        }

        // The process is torn down: nothing is carried across but the two files. The change flag
        // reports the restart, which is the operation under test, not the cycle that preceded it.
        var before = File.ReadAllBytes(floorPath);
        var restart = ProviderPublisherTrustPolicyCustody.Open(checkpointPath, floorPath, authorityId);
        return new(restart.Code, restart.CheckpointCode, restart.Registry is not null,
            storedBefore, Cbi42StoredSequence(floorPath, authorityId, authorityId),
            !before.SequenceEqual(File.ReadAllBytes(floorPath)));
    }

    private static long Cbi42StoredSequence(
        string floorPath,
        ProviderPublisherTrustPolicyAuthorityId authority,
        ProviderPublisherTrustPolicyAuthorityId foreign)
    {
        if (!File.Exists(floorPath)) return 0;
        var opened = DurableProviderPublisherTrustPolicyFloorStore.Open(floorPath, authority);
        if (opened.Store is not null) return opened.Store.Stored.Sequence;
        var alternate = DurableProviderPublisherTrustPolicyFloorStore.Open(floorPath, foreign);
        return alternate.Store?.Stored.Sequence ?? 0;
    }

    private static JsonDocument Cbi42Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi42-floor-custody-vectors.json")));

    private static async Task<Cbi42Observation> Cbi42RunAsync(JsonDocument fixture, string mutation) =>
        await Cbi42RunAsync(fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(vector => vector.GetProperty("mutation").GetString() == mutation));

    [Test]
    public async Task Shared_cbi42_vectors_keep_durable_custody_of_the_recovery_floor()
    {
        using var fixture = Cbi42Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi42RunAsync(vector);
            var label = vector.GetProperty("mutation").GetString();
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()), label);
                Assert.That(actual.CheckpointCode,
                    Is.EqualTo(vector.GetProperty("checkpointCode").GetString()), label);
                Assert.That(actual.Opened, Is.EqualTo(vector.GetProperty("opened").GetBoolean()), label);
                Assert.That(actual.StoredBefore,
                    Is.EqualTo(vector.GetProperty("storedSequenceBefore").GetInt64()), label);
                Assert.That(actual.StoredAfter,
                    Is.EqualTo(vector.GetProperty("storedSequenceAfter").GetInt64()), label);
                Assert.That(actual.StoreChanged,
                    Is.EqualTo(vector.GetProperty("storeChanged").GetBoolean()), label);

                // Phase-wide properties, over every vector rather than per case.
                Assert.That(actual.StoredAfter, Is.GreaterThanOrEqualTo(actual.StoredBefore), label);
                if (actual.Code != "policy-floor-opened" && actual.Code != "policy-floor-established"
                    && actual.Code != "policy-floor-retained")
                    Assert.That(actual.StoreChanged, Is.False, label);
                if (!actual.Opened) Assert.That(actual.CheckpointCode, Is.Not.EqualTo("policy-checkpoint-recovered"), label);
            });
        }
    }

    [Test]
    public void Cbi42_C1_the_stored_record_is_canonical_atomic_and_integrity_checked()
    {
        using var fixture = Cbi42Fixture();
        var golden = fixture.RootElement.GetProperty("goldenImage");
        var image = DurableProviderPublisherTrustPolicyFloorStore.EncodeRecord(
            ProviderPublisherTrustPolicyAuthorityId.Create(golden.GetProperty("authorityIdentity").GetString()!),
            golden.GetProperty("sequence").GetInt64(),
            ProviderPublisherTrustPolicyId.Create(golden.GetProperty("policyIdentity").GetString()!));
        Assert.Multiple(() =>
        {
            Assert.That(image, Has.Length.EqualTo(golden.GetProperty("bytes").GetInt32()));
            Assert.That(Convert.ToHexString(SHA256.HashData(image)),
                Is.EqualTo(golden.GetProperty("sha256").GetString()));
        });
    }

    [Test]
    public async Task Cbi42_C1_only_the_integrity_tag_refuses_a_well_formed_tampered_record()
    {
        // The structural checks cannot reach this one: the altered byte yields a different but
        // entirely parseable sequence, so a store that skipped its tag would accept it.
        using var fixture = Cbi42Fixture();
        var actual = await Cbi42RunAsync(fixture, "start-tampered-sequence");
        Assert.Multiple(() =>
        {
            Assert.That(actual.Code, Is.EqualTo("policy-floor-corrupt"));
            Assert.That(actual.Opened, Is.False);
        });
    }

    [Test]
    public async Task Cbi42_C2_the_store_is_established_before_the_checkpoint_it_guards_exists()
    {
        using var fixture = Cbi42Fixture();
        var fresh = await Cbi42RunAsync(fixture, "start-fresh");
        var removed = await Cbi42RunAsync(fixture, "start-guard-removed");
        Assert.Multiple(() =>
        {
            // A first start establishes at zero; a checkpoint without a store is the guard removed.
            Assert.That(fresh.Code, Is.EqualTo("policy-floor-opened"));
            Assert.That(fresh.CheckpointCode, Is.EqualTo("policy-checkpoint-empty"));
            Assert.That(fresh.StoredAfter, Is.EqualTo(0));
            Assert.That(removed.Code, Is.EqualTo("policy-floor-missing"));
            Assert.That(removed.Opened, Is.False);
        });
    }

    [Test]
    public async Task Cbi42_C3_a_refused_store_refuses_the_start()
    {
        using var fixture = Cbi42Fixture();
        foreach (var mutation in new[] { "start-corrupt-store", "start-tampered-sequence",
            "start-truncated-store", "start-trailing-store", "start-foreign-store" })
        {
            var actual = await Cbi42RunAsync(fixture, mutation);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Opened, Is.False, mutation);
                Assert.That(actual.CheckpointCode, Is.Null, mutation);
                Assert.That(actual.StoreChanged, Is.False, mutation);
            });
        }
    }

    [Test]
    public async Task Cbi42_C4_a_recovered_checkpoint_never_raises_the_floor_that_guards_it()
    {
        using var fixture = Cbi42Fixture();
        var lagging = await Cbi42RunAsync(fixture, "start-lagging-floor");
        Assert.Multiple(() =>
        {
            // The checkpoint holds two and the store holds one; opening reports the checkpoint's
            // state and leaves the store exactly where the last handoff left it.
            Assert.That(lagging.Code, Is.EqualTo("policy-floor-opened"));
            Assert.That(lagging.StoredBefore, Is.EqualTo(1));
            Assert.That(lagging.StoredAfter, Is.EqualTo(1));
            Assert.That(lagging.StoreChanged, Is.False);
        });
    }

    [Test]
    public async Task Cbi42_C5_retention_is_monotonic_idempotent_and_refused_to_the_cycle()
    {
        using var fixture = Cbi42Fixture();
        foreach (var mutation in new[] { "retain-regressed", "retain-forked", "retain-foreign-authority" })
            Assert.That((await Cbi42RunAsync(fixture, mutation)).StoreChanged, Is.False, mutation);
        Assert.That((await Cbi42RunAsync(fixture, "retain-identical")).Code, Is.EqualTo("policy-floor-unchanged"));

        // The composition refuses to start in the state a regressing handoff needs, so the sink's
        // refusal is pinned directly against a store seeded above the registry it is given.
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi42-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var authorityId = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo())));
            var store = DurableProviderPublisherTrustPolicyFloorStore.Open(Cbi42FloorPath(root), authorityId).Store!;
            var registry = DurableProviderPublisherTrustPolicyRegistry.Open(
                Cbi42CheckpointPath(root), authorityId).Registry!;
            Cbi42Seed(registry, store, authority, 2);
            var older = Cbi42IssuedFloor(root, "older.checkpoint", authority, authorityId, 1);
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await store.RetainAsync(older, CancellationToken.None));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task Cbi42_C6_the_composition_closes_the_poll_loop_across_a_restart()
    {
        using var fixture = Cbi42Fixture();
        var restarted = await Cbi42RunAsync(fixture, "cycle-then-restart");
        var rolledBack = await Cbi42RunAsync(fixture, "cycle-then-rollback");
        Assert.Multiple(() =>
        {
            Assert.That(restarted.Code, Is.EqualTo("policy-floor-opened"));
            Assert.That(restarted.StoredAfter, Is.EqualTo(2));
            // The same cycle, followed by an older checkpoint, is refused at the next start.
            Assert.That(rolledBack.Code, Is.EqualTo("policy-checkpoint-rollback-detected"));
            Assert.That(rolledBack.Opened, Is.False);
            Assert.That(rolledBack.StoredAfter, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Cbi42_C7_both_roots_agree_on_custody_observations()
    {
        using var fixture = Cbi42Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi42RunAsync(vector);
            var projection = string.Join('|', actual.Code, actual.CheckpointCode ?? "-", actual.Opened,
                actual.StoredBefore, actual.StoredAfter, actual.StoreChanged);
            var expected = string.Join('|',
                vector.GetProperty("code").GetString(),
                vector.GetProperty("checkpointCode").GetString() ?? "-",
                vector.GetProperty("opened").GetBoolean(),
                vector.GetProperty("storedSequenceBefore").GetInt64(),
                vector.GetProperty("storedSequenceAfter").GetInt64(),
                vector.GetProperty("storeChanged").GetBoolean());
            Assert.That(projection, Is.EqualTo(expected), vector.GetProperty("mutation").GetString());
        }
    }
}
