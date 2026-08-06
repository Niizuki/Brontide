using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    /// <summary>
    /// Answers one scripted outcome per attempt, so a vector states the endpoint's behaviour over a
    /// whole cycle rather than over one call.
    /// </summary>
    private sealed class Cbi60Source(
        IReadOnlyList<string> script,
        IReadOnlyList<ECDsa> authorities,
        ECDsa endpoint,
        ECDsa otherEndpoint,
        DateTimeOffset now)
        : IProviderPolicyAuthorityRotationDistributionSource
    {
        public int Attempts { get; private set; }

        public Task<ProviderPolicyAuthorityRotationDistributionResponse> FetchAsync(
            ProviderPolicyAuthorityRotationDistributionRequest request, CancellationToken cancellationToken)
        {
            var mutation = script[Math.Min(Attempts, script.Count - 1)];
            Attempts++;
            if (mutation == "transport") throw new IOException("unavailable");
            // The offered statement is derived from the cursor the request carries, so a cycle that
            // applies one rotation is answered with the next rather than with the one it already has.
            var index = (int)request.AuthorityGeneration;
            var statement = Cbi57Statement(
                request.AuthorityGeneration + (mutation == "native" ? 2 : 1), 0, null,
                authorities[index], authorities[index + 1], other: otherEndpoint);
            return Task.FromResult(Cbi58Respond(mutation, request,
                mutation == "endpoint" ? otherEndpoint : endpoint, statement, now));
        }
    }

    private sealed class Cbi60Delay : IProviderPolicyAuthorityCycleDelay
    {
        public Task<DateTimeOffset> DelayAsync(DateTimeOffset now, TimeSpan duration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(now + duration);
        }
    }

    private sealed class Cbi60RefusingSink : IProviderPolicyAuthorityFloorSink
    {
        public Task RetainAsync(ProviderPolicyAuthorityFloor floor, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("custody refused");
    }

    /// <summary>
    /// The three sequences are compared as joined text so the record's structural equality reaches
    /// their elements rather than their references, and so a failure names the sequence that differs.
    /// </summary>
    private sealed record Cbi60Observation(
        string Code,
        string? LastAttemptCode,
        int Attempts,
        string DelayMilliseconds,
        string Applied,
        string Retained,
        long Stored,
        long Recovered);

    private static string Cbi60Join<T>(IEnumerable<T> values) =>
        string.Join(",", values.Select(value => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)));

    private static string[] Cbi60Split(string values) =>
        values.Length == 0 ? [] : values.Split(',');

    private static JsonDocument Cbi60Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi60-policy-authority-cycle-vectors.json")));

    private static ProviderPolicyAuthorityCycleSchedule Cbi60Schedule(JsonElement fixture, int maximumAttempts)
    {
        var schedule = fixture.GetProperty("schedule");
        return ProviderPolicyAuthorityCycleSchedule.Create(
            maximumAttempts,
            TimeSpan.FromMilliseconds(schedule.GetProperty("baseDelayMilliseconds").GetInt32()),
            schedule.GetProperty("backoffMultiplier").GetInt32(),
            TimeSpan.FromMilliseconds(schedule.GetProperty("maximumDelayMilliseconds").GetInt32()),
            TimeSpan.FromMilliseconds(schedule.GetProperty("attemptTimeoutMilliseconds").GetInt32()));
    }

    private static async Task<Cbi60Observation> Cbi60RunAsync(JsonElement fixture, JsonElement vector)
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi60-{Guid.NewGuid():N}");
        var checkpoint = Path.Combine(root, "policy.checkpoint");
        var authorityFloorPath = Path.Combine(root, "authority.floor");
        var keys = new List<ECDsa>();
        try
        {
            for (var index = 0; index < 5; index++) keys.Add(ECDsa.Create(ECCurve.NamedCurves.nistP256));
            using var endpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var otherEndpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var pin = Cbi57Authority(keys[0]);
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, pin).Registry!;
            var store = DurableProviderPolicyAuthorityFloorStore.Open(authorityFloorPath, pin).Store!;
            IProviderPolicyAuthorityFloorSink sink =
                vector.GetProperty("sink").GetString() == "refusing" ? new Cbi60RefusingSink() : store;

            var script = vector.GetProperty("attempts").EnumerateArray()
                .Select(value => value.GetString()!).ToArray();
            var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
            var source = new Cbi60Source(script, keys, endpoint, otherEndpoint, now);
            var endpointId = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
                Convert.ToHexString(SHA256.HashData(endpoint.ExportSubjectPublicKeyInfo())));
            var cycle = new ProviderPolicyAuthorityRotationCycle(durable, endpointId,
                Cbi60Schedule(fixture, vector.GetProperty("maximumAttempts").GetInt32()));

            var result = await cycle.RunAsync(source, sink, new Cbi60Delay(), now);
            var recovered = DurableProviderPublisherTrustPolicyRegistry.Open(
                checkpoint, pin, authorityFloor: store.Stored);
            Assert.That(source.Attempts, Is.EqualTo(result.Attempts),
                "the cycle must report exactly the calls it made");
            return new(result.Code, result.LastAttemptCode, result.Attempts,
                Cbi60Join(result.Delays.Select(value => value.TotalMilliseconds)),
                Cbi60Join(result.AppliedGenerations), Cbi60Join(result.RetainedGenerations),
                store.Stored.Generation, recovered.Registry?.AuthorityGeneration ?? -1);
        }
        finally
        {
            foreach (var key in keys) key.Dispose();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static Cbi60Observation Cbi60Expected(JsonElement vector) => new(
        vector.GetProperty("code").GetString()!,
        vector.GetProperty("lastAttemptCode").GetString(),
        vector.GetProperty("attemptCount").GetInt32(),
        Cbi60Join(vector.GetProperty("delaysMilliseconds").EnumerateArray().Select(value => value.GetDouble())),
        Cbi60Join(vector.GetProperty("appliedGenerations").EnumerateArray().Select(value => value.GetInt64())),
        Cbi60Join(vector.GetProperty("retainedGenerations").EnumerateArray().Select(value => value.GetInt64())),
        vector.GetProperty("storedGeneration").GetInt64(),
        vector.GetProperty("recoveredGeneration").GetInt64());

    private static JsonElement Cbi60Vector(JsonDocument fixture, string name) =>
        fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(vector => vector.GetProperty("name").GetString() == name);

    [Test]
    public async Task Shared_cbi60_vectors_schedule_and_retain_authority_rotations()
    {
        using var fixture = Cbi60Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi60RunAsync(fixture.RootElement, vector);
            Assert.That(actual, Is.EqualTo(Cbi60Expected(vector)),
                $"vector {vector.GetProperty("name").GetString()}");
        }
    }

    [Test]
    public async Task Cbi60_C1_a_cycle_is_bounded_and_records_one_gap_between_attempts()
    {
        using var fixture = Cbi60Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi60RunAsync(fixture.RootElement, vector);
            var budget = vector.GetProperty("maximumAttempts").GetInt32();
            Assert.Multiple(() =>
            {
                Assert.That(actual.Attempts, Is.LessThanOrEqualTo(budget));
                Assert.That(Cbi60Split(actual.DelayMilliseconds), Has.Length.EqualTo(actual.Attempts - 1));
            });
        }
        Assert.That((await Cbi60RunAsync(fixture.RootElement, Cbi60Vector(fixture, "budget-is-exhausted"))).Code,
            Is.EqualTo("policy-authority-cycle-exhausted"));
    }

    [Test]
    public async Task Cbi60_C2_only_a_changeable_outcome_is_retried()
    {
        using var fixture = Cbi60Fixture();
        foreach (var name in new[]
                 {
                     "endpoint-mismatch-ends-the-cycle", "challenge-mismatch-ends-the-cycle",
                     "cursor-mismatch-ends-the-cycle", "native-refusal-ends-the-cycle",
                 })
        {
            var actual = await Cbi60RunAsync(fixture.RootElement, Cbi60Vector(fixture, name));
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo("policy-authority-cycle-refused"), name);
                // The budget is six, so a refusal that stops at one attempt stopped because it was
                // not retried rather than because it ran out.
                Assert.That(actual.Attempts, Is.EqualTo(1), name);
            });
        }
        var retried = await Cbi60RunAsync(fixture.RootElement, Cbi60Vector(fixture, "stale-window-is-retried"));
        Assert.That(retried.Code, Is.EqualTo("policy-authority-cycle-current"));
        var midRetry = await Cbi60RunAsync(fixture.RootElement,
            Cbi60Vector(fixture, "invalid-signature-ends-the-cycle-mid-retry"));
        Assert.Multiple(() =>
        {
            Assert.That(midRetry.Code, Is.EqualTo("policy-authority-cycle-refused"));
            Assert.That(midRetry.Attempts, Is.EqualTo(2));
        });
    }

    [Test]
    public void Cbi60_C3_backoff_follows_consecutive_failures_and_clamps()
    {
        var schedule = ProviderPolicyAuthorityCycleSchedule.Create(
            8, TimeSpan.FromMilliseconds(100), 2, TimeSpan.FromMilliseconds(800), TimeSpan.FromSeconds(1));
        Assert.Multiple(() =>
        {
            Assert.That(schedule.DelayForConsecutiveFailures(0), Is.EqualTo(TimeSpan.Zero));
            Assert.That(schedule.DelayForConsecutiveFailures(1), Is.EqualTo(TimeSpan.FromMilliseconds(100)));
            Assert.That(schedule.DelayForConsecutiveFailures(4), Is.EqualTo(TimeSpan.FromMilliseconds(800)));
            Assert.That(schedule.DelayForConsecutiveFailures(64), Is.EqualTo(TimeSpan.FromMilliseconds(800)));
        });
        Assert.Throws<ArgumentOutOfRangeException>(() => ProviderPolicyAuthorityCycleSchedule.Create(
            1, TimeSpan.FromMilliseconds(100), 2, TimeSpan.FromMilliseconds(800), TimeSpan.FromMinutes(2)));
    }

    [Test]
    public async Task Cbi60_C3_an_applied_rotation_resets_the_gap()
    {
        using var fixture = Cbi60Fixture();
        var actual = await Cbi60RunAsync(fixture.RootElement, Cbi60Vector(fixture, "progress-resets-backoff"));
        // Without the reset the third gap would be the doubled 200ms rather than the base 100ms.
        Assert.That(actual.DelayMilliseconds, Is.EqualTo("100,0,100"));
    }

    [Test]
    public async Task Cbi60_C4_the_floor_is_handed_off_after_publication_and_never_before()
    {
        using var fixture = Cbi60Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi60RunAsync(fixture.RootElement, vector);
            var name = vector.GetProperty("name").GetString();
            var applied = Cbi60Split(actual.Applied);
            var retained = Cbi60Split(actual.Retained);
            Assert.Multiple(() =>
            {
                Assert.That(applied.Take(retained.Length), Is.EqualTo(retained), name);
                Assert.That(applied.Length - retained.Length, Is.InRange(0, 1), name);
                Assert.That(actual.Stored, Is.LessThanOrEqualTo(actual.Recovered), name);
            });
        }
        var unretained = await Cbi60RunAsync(fixture.RootElement,
            Cbi60Vector(fixture, "refused-handoff-stops-the-cycle"));
        Assert.Multiple(() =>
        {
            Assert.That(unretained.Code, Is.EqualTo("policy-authority-cycle-floor-unretained"));
            // The rotation is durable and cannot be undone, so the checkpoint is ahead of the guard.
            Assert.That(unretained.Applied, Is.EqualTo("1"));
            Assert.That(unretained.Retained, Is.Empty);
            Assert.That(unretained.Recovered, Is.EqualTo(1));
        });
    }

    [Test]
    public void Cbi60_C5_custody_is_bound_to_the_pin_and_never_regresses()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi60-custody-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "authority.floor");
        try
        {
            using var pinKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var firstKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var forkKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var pin = Cbi57Authority(pinKey);
            var opened = DurableProviderPolicyAuthorityFloorStore.Open(path, pin);
            var store = opened.Store!;
            var advanced = store.Retain(ProviderPolicyAuthorityFloor.Restore(1, Cbi57Authority(firstKey)));
            var unchanged = store.Retain(ProviderPolicyAuthorityFloor.Restore(1, Cbi57Authority(firstKey)));
            var fork = store.Retain(ProviderPolicyAuthorityFloor.Restore(1, Cbi57Authority(forkKey)));
            var regressed = store.Retain(ProviderPolicyAuthorityFloor.Restore(0, pin));
            // Generation zero under anything but the pin is the one floor no unrotated checkpoint
            // could satisfy, so it is refused as a pin mismatch rather than as a regression.
            var zeroUnderOther = store.Retain(ProviderPolicyAuthorityFloor.Restore(0, Cbi57Authority(forkKey)));
            var reopened = DurableProviderPolicyAuthorityFloorStore.Open(path, pin);
            var foreign = DurableProviderPolicyAuthorityFloorStore.Open(path, Cbi57Authority(forkKey));
            var bytes = File.ReadAllBytes(path);
            bytes[^1] ^= 1;
            File.WriteAllBytes(path, bytes);
            var corrupt = DurableProviderPolicyAuthorityFloorStore.Open(path, pin);
            Assert.Multiple(() =>
            {
                Assert.That(opened.Code, Is.EqualTo("policy-authority-floor-established"));
                Assert.That(advanced.Code, Is.EqualTo("policy-authority-floor-retained"));
                Assert.That(unchanged.Code, Is.EqualTo("policy-authority-floor-unchanged"));
                Assert.That(fork.Code, Is.EqualTo("policy-authority-floor-regressed"));
                Assert.That(regressed.Code, Is.EqualTo("policy-authority-floor-regressed"));
                Assert.That(zeroUnderOther.Code, Is.EqualTo("policy-authority-floor-authority-mismatch"));
                Assert.That(store.Stored.Generation, Is.EqualTo(1));
                Assert.That(reopened.Code, Is.EqualTo("policy-authority-floor-recovered"));
                Assert.That(reopened.Store!.Stored.Generation, Is.EqualTo(1));
                Assert.That(foreign.Code, Is.EqualTo("policy-authority-floor-authority-mismatch"));
                Assert.That(corrupt.Code, Is.EqualTo("policy-authority-floor-corrupt"));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi60_C6_only_the_authority_floor_detects_a_truncated_trailing_rotation()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi60-truncation-{Guid.NewGuid():N}");
        var checkpoint = Path.Combine(root, "policy.checkpoint");
        try
        {
            using var pinKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var successorKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var pin = Cbi57Authority(pinKey);
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, pin).Registry!;
            var policy = Cbi37Policy(false);
            var update = durable.Apply(Cbi37Sign(pinKey, 1, null, policy));
            Assert.That(update.IsApplied, Is.True);
            var beforeRotation = File.ReadAllBytes(checkpoint);
            var rotated = durable.Rotate(Cbi57Statement(1, 1, policy.Identity, pinKey, successorKey));
            Assert.That(rotated.IsApplied, Is.True);

            // The trailing rotation is dropped; every policy update in the chain survives it.
            File.WriteAllBytes(checkpoint, beforeRotation);
            var underPolicyFloor = DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, pin, update.Floor);
            var underAuthorityFloor = DurableProviderPublisherTrustPolicyRegistry.Open(
                checkpoint, pin, update.Floor, rotated.Floor);

            // A truncation that dropped a rotation carrying later updates is unconstructible: those
            // updates are signed by the successor the truncation removed.
            var truncated = underPolicyFloor.Registry!;
            var successorSigned = truncated.Apply(
                Cbi37Sign(successorKey, 2, policy.Identity, Cbi37Policy(true)));

            Assert.Multiple(() =>
            {
                Assert.That(underPolicyFloor.Code, Is.EqualTo("policy-checkpoint-recovered"));
                Assert.That(underAuthorityFloor.Code, Is.EqualTo("policy-authority-rollback-detected"));
                Assert.That(successorSigned.IsApplied, Is.False);
                Assert.That(successorSigned.Code, Is.EqualTo("policy-update-authority-mismatch"));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi60_C6_an_absent_authority_guard_is_adopted_rather_than_recovered()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi60-adoption-{Guid.NewGuid():N}");
        var checkpoint = Path.Combine(root, "policy.checkpoint");
        var floor = Path.Combine(root, "policy.floor");
        var authorityFloor = Path.Combine(root, "authority.floor");
        try
        {
            using var pinKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var successorKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var pin = Cbi57Authority(pinKey);
            var fresh = ProviderPolicyAuthorityCustody.Open(checkpoint, floor, authorityFloor, pin);
            Assert.That(fresh.Code, Is.EqualTo("policy-authority-floor-opened"));
            Assert.That(fresh.Registry!.Rotate(Cbi57Statement(1, 0, null, pinKey, successorKey)).IsApplied, Is.True);

            // A guard introduced after the checkpoint it must guard cannot read its own absence as a
            // removal, so it adopts the host at zero and says so.
            File.Delete(authorityFloor);
            var adopted = ProviderPolicyAuthorityCustody.Open(checkpoint, floor, authorityFloor, pin);
            File.Delete(floor);
            var missing = ProviderPolicyAuthorityCustody.Open(checkpoint, floor, authorityFloor, pin);
            Assert.Multiple(() =>
            {
                Assert.That(adopted.Code, Is.EqualTo("policy-authority-floor-adopted"));
                Assert.That(adopted.IsOpened, Is.True);
                Assert.That(adopted.AuthorityFloors!.Stored.Generation, Is.EqualTo(0));
                Assert.That(adopted.Registry!.AuthorityGeneration, Is.EqualTo(1));
                // CBI42's guard could be ordered before its checkpoint, so its absence is still a
                // refusal, and deleting both is caught by that one.
                Assert.That(missing.Code, Is.EqualTo("policy-floor-missing"));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task Cbi60_C7_cancellation_ends_the_cycle_without_a_further_attempt()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi60-cancel-{Guid.NewGuid():N}");
        var keys = new List<ECDsa>();
        try
        {
            for (var index = 0; index < 3; index++) keys.Add(ECDsa.Create(ECCurve.NamedCurves.nistP256));
            using var endpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var otherEndpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var pin = Cbi57Authority(keys[0]);
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), pin).Registry!;
            var store = DurableProviderPolicyAuthorityFloorStore.Open(
                Path.Combine(root, "authority.floor"), pin).Store!;
            var endpointId = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
                Convert.ToHexString(SHA256.HashData(endpoint.ExportSubjectPublicKeyInfo())));
            var cycle = new ProviderPolicyAuthorityRotationCycle(durable, endpointId,
                ProviderPolicyAuthorityCycleSchedule.Create(
                    4, TimeSpan.FromMilliseconds(10), 2, TimeSpan.FromMilliseconds(20), TimeSpan.FromSeconds(1)));
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var source = new Cbi60Source(["current"], keys, endpoint, otherEndpoint,
                DateTimeOffset.FromUnixTimeSeconds(1_800_000_000));
            var result = await cycle.RunAsync(source, store, new Cbi60Delay(),
                DateTimeOffset.FromUnixTimeSeconds(1_800_000_000), cancellation.Token);
            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo("policy-authority-cycle-canceled"));
                Assert.That(result.Attempts, Is.Zero);
                Assert.That(source.Attempts, Is.Zero);
            });
        }
        finally
        {
            foreach (var key in keys) key.Dispose();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
