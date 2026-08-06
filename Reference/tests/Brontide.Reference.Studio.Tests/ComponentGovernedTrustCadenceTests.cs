using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    /// <summary>
    /// Holds the scripted behaviour of one cadence run. Both endpoints read the cycle index from it,
    /// so a vector states what each channel does per cycle rather than per call.
    /// </summary>
    private sealed class Cbi61Script(IReadOnlyList<(string Rotation, string Policy)> cycles)
    {
        public int Cycle { get; set; }
        public bool PolicyServedThisCycle { get; set; }
        public bool RotationServedThisCycle { get; set; }
        public List<string?> PollCodes { get; } = [];
        public string Rotation => cycles[Math.Min(Cycle, cycles.Count - 1)].Rotation;
        public string Policy => cycles[Math.Min(Cycle, cycles.Count - 1)].Policy;
    }

    private sealed class Cbi61RotationSource(
        Cbi61Script script,
        IReadOnlyList<ECDsa> authorities,
        ECDsa endpoint,
        ECDsa otherEndpoint,
        DateTimeOffset now,
        CancellationTokenSource cancellation)
        : IProviderPolicyAuthorityRotationDistributionSource
    {
        public Task<ProviderPolicyAuthorityRotationDistributionResponse> FetchAsync(
            ProviderPolicyAuthorityRotationDistributionRequest request, CancellationToken cancellationToken)
        {
            var mutation = script.Rotation;
            if (mutation == "transport") throw new IOException("unavailable");
            if (mutation == "canceled")
            {
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            }
            var index = (int)request.AuthorityGeneration;
            var statement = Cbi57Statement(request.AuthorityGeneration + 1, 0, null,
                authorities[index], authorities[index + 1], other: otherEndpoint);
            // One pending rotation per cycle, then the endpoint reports the host current. CBI60
            // continues after progress, so an endpoint that kept offering successors would rotate
            // repeatedly inside one cycle, which no publisher does.
            var offers = mutation is "rotate" or "rotate-unretained" && !script.RotationServedThisCycle;
            if (offers) script.RotationServedThisCycle = true;
            var kind = offers ? "rotate" : mutation is "rotate" or "rotate-unretained" ? "current" : mutation;
            return Task.FromResult(Cbi58Respond(kind, request,
                mutation == "endpoint" ? otherEndpoint : endpoint, statement, now));
        }
    }

    private sealed class Cbi61AuthorityFloorSink(
        Cbi61Script script,
        DurableProviderPolicyAuthorityFloorStore store) : IProviderPolicyAuthorityFloorSink
    {
        public Task RetainAsync(ProviderPolicyAuthorityFloor floor, CancellationToken cancellationToken)
        {
            if (script.Rotation == "rotate-unretained")
                throw new IOException("The authority floor store is unavailable.");
            return store.RetainAsync(floor, cancellationToken);
        }
    }

    private sealed class Cbi61PolicySource(
        Cbi61Script script,
        ECDsa pin,
        ECDsa successor,
        ECDsa foreignAuthority,
        ECDsa endpoint,
        ECDsa otherEndpoint,
        DateTimeOffset now)
        : IProviderPublisherTrustPolicyDistributionSource
    {
        public Task<ProviderPublisherTrustPolicyDistributionResponse> FetchAsync(
            ProviderPublisherTrustPolicyDistributionRequest request, CancellationToken cancellationToken)
        {
            var mutation = script.Policy;
            // One update per cycle, then the endpoint reports the host current. A real endpoint
            // answers relative to the cursor it is given, and this keeps a cycle from applying the
            // same sequence twice while CBI41 continues after progress.
            var offers = mutation is "update" or "successor-update" or "foreign-update"
                && !script.PolicyServedThisCycle;
            if (offers) script.PolicyServedThisCycle = true;
            var signer = mutation switch
            {
                "successor-update" => successor,
                "foreign-update" => foreignAuthority,
                _ => pin,
            };
            var update = offers
                ? Cbi37Sign(signer, request.CurrentSequence + 1, request.CurrentPolicyIdentity,
                    Cbi41Policy(request.CurrentSequence + 1))
                : null;
            var issued = now;
            var expires = issued.AddMinutes(1);
            var responder = mutation == "endpoint" ? otherEndpoint : endpoint;
            var signature = responder.SignData(
                ProviderPublisherTrustPolicyDistributionManifest.Encode(
                    request.Challenge, request.CurrentSequence, request.CurrentPolicyIdentity,
                    issued.ToUnixTimeSeconds(), expires.ToUnixTimeSeconds(), update),
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
            return Task.FromResult(new ProviderPublisherTrustPolicyDistributionResponse(
                request.Challenge, request.CurrentSequence, request.CurrentPolicyIdentity,
                issued.ToUnixTimeSeconds(), expires.ToUnixTimeSeconds(), update,
                "ECDSA-P256-SHA256",
                Convert.ToBase64String(responder.ExportSubjectPublicKeyInfo()),
                Convert.ToBase64String(signature)));
        }
    }

    /// <summary>Records that the policy endpoint was reached, which is what C2 and C3 observe.</summary>
    private sealed class Cbi61PolicyCycle(
        Cbi61Script script,
        IProviderPublisherTrustPolicyCycle inner) : IProviderPublisherTrustPolicyCycle
    {
        public async Task<ProviderPublisherTrustPolicyPollResult> PollAsync(
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            script.PolicyServedThisCycle = false;
            var result = await inner.PollAsync(now, cancellationToken).ConfigureAwait(false);
            script.PollCodes.Add(result.Code);
            return result;
        }
    }

    private sealed class Cbi61EmptySweep : IProviderServingTrustSweepCycle
    {
        // CBI47 C4 makes an empty serving set a successful no-op, which keeps this slice's evidence
        // on the rotation-versus-poll interaction rather than on CBI46's members.
        public ValueTask<ProviderServingTrustSweepResult?> SweepAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<ProviderServingTrustSweepResult?>(null);
    }

    private sealed class Cbi61CadenceDelay(Cbi61Script script) : IProviderServingTrustCadenceDelay
    {
        public Task<DateTimeOffset> DelayAsync(
            DateTimeOffset now, TimeSpan duration, CancellationToken cancellationToken)
        {
            script.Cycle++;
            script.RotationServedThisCycle = false;
            script.PolicyServedThisCycle = false;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(now + duration);
        }
    }

    private sealed class Cbi61InstantDelay
        : IProviderPolicyAuthorityCycleDelay, IProviderPublisherTrustPolicyPollDelay
    {
        public Task<DateTimeOffset> DelayAsync(
            DateTimeOffset now, TimeSpan duration, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(now + duration);
        }
    }

    private sealed record Cbi61Observation(
        string CadenceCode,
        string CycleCodes,
        string RotationCodes,
        string PollCodes,
        string Polled,
        long FinalGeneration,
        long FinalSequence);

    private static JsonDocument Cbi61Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi61-governed-trust-cadence-vectors.json")));

    private static async Task<Cbi61Observation> Cbi61RunAsync(JsonElement fixture, JsonElement vector)
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi61-{Guid.NewGuid():N}");
        var checkpoint = Path.Combine(root, "policy.checkpoint");
        var keys = new List<ECDsa>();
        try
        {
            for (var index = 0; index < 4; index++) keys.Add(ECDsa.Create(ECCurve.NamedCurves.nistP256));
            using var foreignAuthority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var endpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var otherEndpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var pin = Cbi57Authority(keys[0]);
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, pin).Registry!;
            var policyFloors = DurableProviderPublisherTrustPolicyFloorStore
                .Open(Path.Combine(root, "policy.floor"), pin).Store!;
            var authorityFloors = DurableProviderPolicyAuthorityFloorStore
                .Open(Path.Combine(root, "authority.floor"), pin).Store!;
            var endpointId = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
                Convert.ToHexString(SHA256.HashData(endpoint.ExportSubjectPublicKeyInfo())));

            var script = new Cbi61Script(
            [
                .. vector.GetProperty("cycles").EnumerateArray().Select(cycle =>
                    (cycle.GetProperty("rotation").GetString()!, cycle.GetProperty("policy").GetString()!)),
            ]);
            var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
            using var cancellation = new CancellationTokenSource();
            var instant = new Cbi61InstantDelay();

            var rotationCycle = new ProviderPolicyAuthorityRotationCycleBinding(
                new ProviderPolicyAuthorityRotationCycle(durable, endpointId,
                    ProviderPolicyAuthorityCycleSchedule.Create(
                        2, TimeSpan.FromMilliseconds(1), 2, TimeSpan.FromMilliseconds(2), TimeSpan.FromSeconds(1))),
                new Cbi61RotationSource(script, keys, endpoint, otherEndpoint, now, cancellation),
                new Cbi61AuthorityFloorSink(script, authorityFloors),
                instant);

            var policyCycle = new Cbi61PolicyCycle(script, new ProviderPublisherTrustPolicyCycle(
                new ProviderPublisherTrustPolicyPoller(durable, endpointId,
                    ProviderPublisherTrustPolicyPollSchedule.Create(
                        2, TimeSpan.FromMilliseconds(1), 2, TimeSpan.FromMilliseconds(2), TimeSpan.FromSeconds(1))),
                new Cbi61PolicySource(script, keys[0], keys[1], foreignAuthority, endpoint, otherEndpoint, now),
                policyFloors,
                instant));

            var governed = new ProviderGovernedTrustCycle(
                rotationCycle, new ProviderServingTrustCycle(policyCycle, new Cbi61EmptySweep()));
            var schedule = fixture.GetProperty("schedule");
            var cadence = new ProviderServingTrustCadence(ProviderServingTrustCadenceSchedule.Create(
                vector.GetProperty("cycles").GetArrayLength(),
                TimeSpan.FromMilliseconds(schedule.GetProperty("intervalMilliseconds").GetInt32())));

            var result = await cadence.RunAsync(
                governed, new Cbi61CadenceDelay(script), now, cancellation.Token);

            return new(result.Code,
                Cbi60Join(result.Cycles.Select(cycle => cycle.Result.Code)),
                Cbi60Join(result.Cycles.Select(cycle => cycle.Result.Rotation?.Code ?? "none")),
                Cbi60Join(result.Cycles.Select(cycle => cycle.Result.Poll?.Code ?? "none")),
                Cbi60Join(result.Cycles.Select(cycle => cycle.Result.Poll is not null)),
                durable.AuthorityGeneration, durable.Current?.Sequence ?? 0);
        }
        finally
        {
            foreach (var key in keys) key.Dispose();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static Cbi61Observation Cbi61Expected(JsonElement vector) => new(
        vector.GetProperty("cadenceCode").GetString()!,
        Cbi60Join(vector.GetProperty("cycleCodes").EnumerateArray().Select(value => value.GetString())),
        Cbi60Join(vector.GetProperty("rotationCodes").EnumerateArray().Select(value => value.GetString())),
        Cbi60Join(vector.GetProperty("pollCodes").EnumerateArray()
            .Select(value => value.ValueKind == JsonValueKind.Null ? "none" : value.GetString())),
        Cbi60Join(vector.GetProperty("polled").EnumerateArray().Select(value => value.GetBoolean())),
        vector.GetProperty("finalGeneration").GetInt64(),
        vector.GetProperty("finalSequence").GetInt64());

    private static JsonElement Cbi61Vector(JsonDocument fixture, string name) =>
        fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(vector => vector.GetProperty("name").GetString() == name);

    [Test]
    public async Task Shared_cbi61_vectors_govern_the_trust_cadence()
    {
        using var fixture = Cbi61Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi61RunAsync(fixture.RootElement, vector);
            Assert.That(actual, Is.EqualTo(Cbi61Expected(vector)),
                $"vector {vector.GetProperty("name").GetString()}");
        }
    }

    [Test]
    public async Task Cbi61_C1_rotating_before_polling_is_what_lets_a_successor_signed_update_apply()
    {
        using var fixture = Cbi61Fixture();
        var actual = await Cbi61RunAsync(fixture.RootElement, Cbi61Vector(fixture, "rotation-precedes-poll"));
        Assert.Multiple(() =>
        {
            // Polling first would refuse this update as an authority mismatch, so the applied
            // sequence is what distinguishes the two orders rather than a comment claiming one.
            Assert.That(actual.FinalGeneration, Is.EqualTo(1));
            Assert.That(actual.FinalSequence, Is.EqualTo(1));
            Assert.That(actual.CadenceCode, Is.EqualTo("provider-trust-cadence-complete"));
        });
    }

    [Test]
    public async Task Cbi61_C2_a_rotation_that_changed_nothing_still_reaches_the_poll()
    {
        using var fixture = Cbi61Fixture();
        foreach (var name in new[]
                 {
                     "refused-rotation-does-not-stop-the-cadence",
                     "exhausted-rotation-does-not-stop-the-cadence",
                 })
        {
            var actual = await Cbi61RunAsync(fixture.RootElement, Cbi61Vector(fixture, name));
            Assert.Multiple(() =>
            {
                Assert.That(actual.CadenceCode, Is.EqualTo("provider-trust-cadence-complete"), name);
                Assert.That(actual.Polled.Split(',').All(value => value == "True"), Is.True, name);
                Assert.That(actual.FinalGeneration, Is.Zero, name);
            });
        }
    }

    [Test]
    public async Task Cbi61_C3_an_unretained_authority_floor_stops_before_the_policy_endpoint()
    {
        using var fixture = Cbi61Fixture();
        var actual = await Cbi61RunAsync(fixture.RootElement,
            Cbi61Vector(fixture, "unretained-floor-stops-before-the-poll"));
        Assert.Multiple(() =>
        {
            Assert.That(actual.CycleCodes, Is.EqualTo("provider-trust-cycle-authority-unretained"));
            Assert.That(actual.Polled, Is.EqualTo("False"));
            Assert.That(actual.PollCodes, Is.EqualTo("none"));
            // The rotation is durable; only its guard is behind.
            Assert.That(actual.FinalGeneration, Is.EqualTo(1));
            Assert.That(actual.CadenceCode, Is.EqualTo("provider-trust-cadence-stopped"));
        });
    }

    [Test]
    public async Task Cbi61_C4_neither_loop_reports_the_other_loops_code()
    {
        using var fixture = Cbi61Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi61RunAsync(fixture.RootElement, vector);
            var name = vector.GetProperty("name").GetString();
            Assert.Multiple(() =>
            {
                foreach (var code in actual.RotationCodes.Split(',').Where(value => value != "none"))
                    Assert.That(code, Does.StartWith("policy-authority-cycle-"), name);
                foreach (var code in actual.PollCodes.Split(',').Where(value => value != "none"))
                    Assert.That(code, Does.StartWith("policy-poll-"), name);
            });
        }
    }

    [Test]
    public async Task Cbi61_C5_an_authority_mismatch_is_attributed_only_when_a_rotation_is_incomplete()
    {
        using var fixture = Cbi61Fixture();
        var behind = await Cbi61RunAsync(fixture.RootElement,
            Cbi61Vector(fixture, "authority-behind-is-attributed"));
        var foreign = await Cbi61RunAsync(fixture.RootElement,
            Cbi61Vector(fixture, "foreign-authority-is-not-attributed"));
        var unrelated = await Cbi61RunAsync(fixture.RootElement,
            Cbi61Vector(fixture, "policy-refusal-stops-with-its-own-code"));
        Assert.Multiple(() =>
        {
            // The two differ only in what the rotation reported; the poll code is identical.
            Assert.That(behind.PollCodes, Is.EqualTo(foreign.PollCodes));
            Assert.That(behind.CycleCodes, Is.EqualTo("provider-trust-cycle-authority-behind"));
            Assert.That(foreign.CycleCodes, Is.EqualTo("provider-trust-cycle-stopped"));
            // A poll refused for an unrelated reason is never attributed, even though its rotation
            // also reported current.
            Assert.That(unrelated.CycleCodes, Is.EqualTo("provider-trust-cycle-stopped"));
        });
    }

    [Test]
    public async Task Cbi61_C6_rotation_cancellation_reaches_no_policy_endpoint()
    {
        using var fixture = Cbi61Fixture();
        var actual = await Cbi61RunAsync(fixture.RootElement,
            Cbi61Vector(fixture, "rotation-cancellation-precedes-any-poll"));
        Assert.Multiple(() =>
        {
            Assert.That(actual.CadenceCode, Is.EqualTo("provider-trust-cadence-canceled"));
            Assert.That(actual.Polled, Is.EqualTo("False"));
            Assert.That(actual.FinalGeneration, Is.Zero);
        });
    }

    [Test]
    public async Task Cbi61_C7_a_cadence_never_exceeds_its_budget_and_stops_at_its_last_cycle()
    {
        using var fixture = Cbi61Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi61RunAsync(fixture.RootElement, vector);
            var budget = vector.GetProperty("cycles").GetArrayLength();
            var codes = actual.CycleCodes.Split(',');
            var name = vector.GetProperty("name").GetString();
            Assert.Multiple(() =>
            {
                Assert.That(codes, Has.Length.LessThanOrEqualTo(budget), name);
                foreach (var code in codes[..^1])
                    Assert.That(code, Is.EqualTo("provider-trust-cycle-current")
                        .Or.EqualTo("provider-trust-cycle-withdrawn"), name);
            });
        }
    }
}
