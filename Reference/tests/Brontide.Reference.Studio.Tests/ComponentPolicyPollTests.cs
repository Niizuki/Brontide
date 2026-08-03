using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed class Cbi41Delay(CancellationTokenSource? cancelDuringGap) : IProviderPublisherTrustPolicyPollDelay
    {
        public List<TimeSpan> Requested { get; } = [];

        public Task<DateTimeOffset> DelayAsync(DateTimeOffset now, TimeSpan duration, CancellationToken cancellationToken)
        {
            if (cancelDuringGap is not null)
            {
                cancelDuringGap.Cancel();
                throw new OperationCanceledException(cancellationToken);
            }
            Requested.Add(duration);
            return Task.FromResult(now + duration);
        }
    }

    private sealed class Cbi41Sink(
        string checkpointPath,
        ProviderPublisherTrustPolicyAuthorityId authority,
        bool fails) : IProviderPublisherTrustPolicyFloorSink
    {
        public List<long> Retained { get; } = [];
        public List<bool> PublicationPreceded { get; } = [];

        public Task RetainAsync(ProviderPublisherTrustPolicyRecoveryFloor floor, CancellationToken cancellationToken)
        {
            // C4 is an ordering claim, so it is observed here rather than described: reopening the
            // checkpoint proves the update the floor names is already durable when the floor arrives.
            var reopened = DurableProviderPublisherTrustPolicyRegistry.Open(checkpointPath, authority);
            PublicationPreceded.Add(reopened.Registry?.Current?.Sequence >= floor.Sequence);
            if (fails) throw new IOException("The floor sink is unavailable.");
            Retained.Add(floor.Sequence);
            return Task.CompletedTask;
        }
    }

    private sealed record Cbi41Observation(
        string Code,
        string? LastAttemptCode,
        int Attempts,
        IReadOnlyList<int> Delays,
        IReadOnlyList<long> Applied,
        IReadOnlyList<long> Retained,
        long FinalSequence,
        IReadOnlyList<bool> PublicationPreceded,
        IReadOnlyList<int> RequestedGaps,
        int SourceAttempts);

    private static ProviderPublisherTrustPolicy Cbi41Policy(long index)
    {
        ProviderPublisherTrustEntry[] entries = [new(
            ProviderPublisherKeyId.Create(index.ToString("X64")), ProviderPublisherTrustDisposition.Admitted)];
        return new(ProviderPublisherTrustPolicyIdentity.Compute(entries), entries);
    }

    private static ProviderPublisherTrustPolicyUpdate Cbi41Update(
        ECDsa authority, long sequence, ProviderPublisherTrustPolicyId? previous) =>
        Cbi37Sign(authority, sequence, previous, Cbi41Policy(sequence));

    private static ProviderPublisherTrustPolicyDistributionResponse Cbi41Respond(
        string kind,
        ProviderPublisherTrustPolicyDistributionRequest request,
        ECDsa endpointKey,
        ECDsa otherEndpointKey,
        ECDsa authority,
        ECDsa foreignAuthority,
        DateTimeOffset now)
    {
        var update = kind switch
        {
            "update" => Cbi41Update(authority, request.CurrentSequence + 1, request.CurrentPolicyIdentity),
            "foreign-authority" => Cbi41Update(foreignAuthority, request.CurrentSequence + 1, request.CurrentPolicyIdentity),
            _ => null,
        };
        var issued = kind == "stale" ? now.AddMinutes(-2) : now;
        var expires = kind == "stale" ? now.AddMinutes(-1) : issued.AddMinutes(1);
        var signer = kind == "endpoint-mismatch" ? otherEndpointKey : endpointKey;
        var publicKey = signer.ExportSubjectPublicKeyInfo();
        var signature = signer.SignData(
            ProviderPublisherTrustPolicyDistributionManifest.Encode(
                request.Challenge, request.CurrentSequence, request.CurrentPolicyIdentity,
                issued.ToUnixTimeSeconds(), expires.ToUnixTimeSeconds(), update),
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        var response = new ProviderPublisherTrustPolicyDistributionResponse(
            request.Challenge, request.CurrentSequence, request.CurrentPolicyIdentity,
            issued.ToUnixTimeSeconds(), expires.ToUnixTimeSeconds(), update, "ECDSA-P256-SHA256",
            Convert.ToBase64String(publicKey), Convert.ToBase64String(signature));
        if (kind == "signature-invalid")
        {
            var changed = Convert.FromBase64String(response.SignatureBase64);
            changed[^1] ^= 1;
            response = response with { SignatureBase64 = Convert.ToBase64String(changed) };
        }
        return response;
    }

    private static ProviderPublisherTrustPolicyPollSchedule Cbi41Schedule(JsonElement schedule) =>
        ProviderPublisherTrustPolicyPollSchedule.Create(
            schedule.GetProperty("maximumAttempts").GetInt32(),
            TimeSpan.FromMilliseconds(schedule.GetProperty("baseDelayMilliseconds").GetInt32()),
            schedule.GetProperty("backoffMultiplier").GetInt32(),
            TimeSpan.FromMilliseconds(schedule.GetProperty("maximumDelayMilliseconds").GetInt32()),
            TimeSpan.FromMilliseconds(schedule.GetProperty("attemptTimeoutMilliseconds").GetInt32()));

    private static async Task<Cbi41Observation> Cbi41RunAsync(JsonElement vector, JsonElement schedule)
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi41-{Guid.NewGuid():N}");
        try
        {
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var foreignAuthority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var endpointKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var otherEndpointKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var authorityId = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo())));
            var endpointId = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
                Convert.ToHexString(SHA256.HashData(endpointKey.ExportSubjectPublicKeyInfo())));
            var checkpoint = Path.Combine(root, "policy.checkpoint");
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, authorityId).Registry!;
            var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);

            var responses = vector.GetProperty("responses").EnumerateArray()
                .Select(element => element.GetString()!).ToArray();
            var served = 0;
            var source = new Cbi39Source((request, cancellationToken) =>
            {
                var kind = responses[Math.Min(served++, responses.Length - 1)];
                if (kind == "transport") throw new IOException("The distribution endpoint is unavailable.");
                if (kind == "superseded")
                {
                    // Another writer advances the registry while the attempt is in flight, which is
                    // the only way CBI39's superseded cursor is reachable.
                    durable.Apply(Cbi41Update(authority, request.CurrentSequence + 1, request.CurrentPolicyIdentity));
                    kind = "current";
                }
                return Task.FromResult(Cbi41Respond(
                    kind, request, endpointKey, otherEndpointKey, authority, foreignAuthority, now));
            });

            var cancel = vector.GetProperty("cancel").GetString();
            using var cancellation = new CancellationTokenSource();
            var delay = new Cbi41Delay(cancel == "in-backoff" ? cancellation : null);
            var sink = new Cbi41Sink(checkpoint, authorityId, vector.GetProperty("sinkFails").GetBoolean());
            if (cancel == "before") cancellation.Cancel();

            var result = await new ProviderPublisherTrustPolicyPoller(durable, endpointId, Cbi41Schedule(schedule))
                .PollAsync(source, sink, delay, now, cancellation.Token);
            return new(result.Code, result.LastAttemptCode, result.Attempts,
                [.. result.Delays.Select(value => (int)value.TotalMilliseconds)],
                result.AppliedSequences, result.RetainedSequences,
                durable.Current?.Sequence ?? 0, sink.PublicationPreceded,
                [.. delay.Requested.Select(value => (int)value.TotalMilliseconds)], source.Attempts);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static JsonDocument Cbi41Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi41-policy-poll-vectors.json")));

    private static async Task<Cbi41Observation> Cbi41RunAsync(JsonDocument fixture, int index) =>
        await Cbi41RunAsync(fixture.RootElement.GetProperty("vectors")[index],
            fixture.RootElement.GetProperty("schedule"));

    private static int[] Cbi41Numbers(JsonElement element, string name) =>
        [.. element.GetProperty(name).EnumerateArray().Select(value => value.GetInt32())];

    private static long[] Cbi41Sequences(JsonElement element, string name) =>
        [.. element.GetProperty(name).EnumerateArray().Select(value => value.GetInt64())];

    [Test]
    public async Task Shared_cbi41_vectors_run_one_bounded_cycle()
    {
        using var fixture = Cbi41Fixture();
        var schedule = fixture.RootElement.GetProperty("schedule");
        var budget = schedule.GetProperty("maximumAttempts").GetInt32();
        var cap = schedule.GetProperty("maximumDelayMilliseconds").GetInt32();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi41RunAsync(vector, schedule);
            var label = vector.GetProperty("mutation").GetString();
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()), label);
                Assert.That(actual.LastAttemptCode,
                    Is.EqualTo(vector.GetProperty("lastAttemptCode").GetString()), label);
                Assert.That(actual.Attempts, Is.EqualTo(vector.GetProperty("attempts").GetInt32()), label);
                Assert.That(actual.Delays, Is.EqualTo(Cbi41Numbers(vector, "delaysMilliseconds")), label);
                Assert.That(actual.Applied, Is.EqualTo(Cbi41Sequences(vector, "appliedSequences")), label);
                Assert.That(actual.Retained, Is.EqualTo(Cbi41Sequences(vector, "retainedSequences")), label);
                Assert.That(actual.FinalSequence,
                    Is.EqualTo(vector.GetProperty("finalSequence").GetInt64()), label);

                // Phase-wide properties, over every vector rather than per case.
                Assert.That(actual.Attempts, Is.LessThanOrEqualTo(budget), label);
                Assert.That(actual.Delays, Has.Count.EqualTo(Math.Max(actual.Attempts - 1, 0)), label);
                Assert.That(actual.Delays, Is.All.LessThanOrEqualTo(cap), label);
                Assert.That(actual.Delays, Is.EqualTo(actual.RequestedGaps), label);
                Assert.That(actual.Retained, Is.EqualTo(actual.Applied.Take(actual.Retained.Count)), label);
                Assert.That(actual.Applied, Is.Ordered.Ascending.And.Unique, label);
                Assert.That(actual.PublicationPreceded, Is.All.True, label);
                if (!vector.GetProperty("externalWrite").GetBoolean())
                    Assert.That(actual.FinalSequence,
                        Is.EqualTo(actual.Applied.Count == 0 ? 0 : actual.Applied[^1]), label);
            });
        }
    }

    [Test]
    public async Task Cbi41_C1_a_cycle_advances_until_the_endpoint_reports_the_host_current()
    {
        using var fixture = Cbi41Fixture();
        var chain = await Cbi41RunAsync(fixture, 2);
        var already = await Cbi41RunAsync(fixture, 0);
        Assert.Multiple(() =>
        {
            Assert.That(chain.Code, Is.EqualTo("policy-poll-current"));
            Assert.That(chain.Applied, Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(chain.FinalSequence, Is.EqualTo(2));
            Assert.That(chain.SourceAttempts, Is.EqualTo(3));
            // Nothing to do is a cycle too, and it costs exactly one attempt.
            Assert.That(already.Applied, Is.Empty);
            Assert.That(already.SourceAttempts, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Cbi41_C2_backoff_is_deterministic_bounded_and_reset_by_progress()
    {
        using var fixture = Cbi41Fixture();
        var schedule = Cbi41Schedule(fixture.RootElement.GetProperty("schedule"));
        var expected = Cbi41Numbers(fixture.RootElement, "backoffMilliseconds");
        for (var failures = 1; failures <= expected.Length; failures++)
            Assert.That((int)schedule.DelayForConsecutiveFailures(failures).TotalMilliseconds,
                Is.EqualTo(expected[failures - 1]), $"consecutive failures {failures}");
        Assert.That(schedule.DelayForConsecutiveFailures(0), Is.EqualTo(TimeSpan.Zero));

        // Progress resets the count: the gap after the applied update is zero, and the failure that
        // follows it starts again at the base delay rather than continuing the earlier ramp.
        var resets = await Cbi41RunAsync(fixture, 4);
        Assert.That(resets.Delays, Is.EqualTo(new[] { 1000, 0, 1000 }));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ProviderPublisherTrustPolicyPollSchedule.Create(
                0, TimeSpan.FromSeconds(1), 2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => ProviderPublisherTrustPolicyPollSchedule.Create(
                3, TimeSpan.FromSeconds(20), 2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => ProviderPublisherTrustPolicyPollSchedule.Create(
                3, TimeSpan.FromSeconds(1), 2, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(2)));
        });
    }

    [Test]
    public async Task Cbi41_C3_a_terminal_outcome_ends_the_cycle_at_its_own_attempt()
    {
        using var fixture = Cbi41Fixture();
        foreach (var index in new[] { 8, 9, 10 })
        {
            var actual = await Cbi41RunAsync(fixture, index);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo("policy-poll-refused"));
                Assert.That(actual.Attempts, Is.EqualTo(1));
                Assert.That(actual.SourceAttempts, Is.EqualTo(1));
                Assert.That(actual.Delays, Is.Empty);
                Assert.That(actual.FinalSequence, Is.EqualTo(0));
            });
        }
    }

    [Test]
    public async Task Cbi41_C4_the_floor_is_handed_off_after_publication_and_never_before()
    {
        using var fixture = Cbi41Fixture();
        var chain = await Cbi41RunAsync(fixture, 2);
        Assert.Multiple(() =>
        {
            Assert.That(chain.PublicationPreceded, Is.EqualTo(new[] { true, true }));
            Assert.That(chain.Retained, Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(chain.Retained, Is.Ordered.Ascending.And.Unique);
        });
    }

    [Test]
    public async Task Cbi41_C5_a_refused_handoff_stops_the_cycle_and_reports_the_unretained_floor()
    {
        using var fixture = Cbi41Fixture();
        var actual = await Cbi41RunAsync(fixture, 13);
        Assert.Multiple(() =>
        {
            Assert.That(actual.Code, Is.EqualTo("policy-poll-floor-unretained"));
            // The update is not undone, because it is already durable, and nothing advances past it.
            Assert.That(actual.Applied, Is.EqualTo(new long[] { 1 }));
            Assert.That(actual.Retained, Is.Empty);
            Assert.That(actual.FinalSequence, Is.EqualTo(1));
            Assert.That(actual.SourceAttempts, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Cbi41_C6_cancellation_is_observed_before_every_attempt_and_inside_every_gap()
    {
        using var fixture = Cbi41Fixture();
        var before = await Cbi41RunAsync(fixture, 11);
        var during = await Cbi41RunAsync(fixture, 12);
        Assert.Multiple(() =>
        {
            Assert.That(before.Code, Is.EqualTo("policy-poll-canceled"));
            Assert.That(before.Attempts, Is.EqualTo(0));
            Assert.That(before.SourceAttempts, Is.EqualTo(0));
            Assert.That(during.Code, Is.EqualTo("policy-poll-canceled"));
            Assert.That(during.Attempts, Is.EqualTo(1));
            // A gap that was cancelled was never waited, so it is not recorded.
            Assert.That(during.Delays, Is.Empty);
        });
    }

    [Test]
    public async Task Cbi41_C7_both_roots_agree_on_cycle_observations()
    {
        using var fixture = Cbi41Fixture();
        var schedule = fixture.RootElement.GetProperty("schedule");
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi41RunAsync(vector, schedule);
            var projection = string.Join('|',
                actual.Code, actual.LastAttemptCode ?? "-", actual.Attempts,
                string.Join(',', actual.Delays), string.Join(',', actual.Applied),
                string.Join(',', actual.Retained), actual.FinalSequence);
            var expected = string.Join('|',
                vector.GetProperty("code").GetString(),
                vector.GetProperty("lastAttemptCode").GetString() ?? "-",
                vector.GetProperty("attempts").GetInt32(),
                string.Join(',', Cbi41Numbers(vector, "delaysMilliseconds")),
                string.Join(',', Cbi41Sequences(vector, "appliedSequences")),
                string.Join(',', Cbi41Sequences(vector, "retainedSequences")),
                vector.GetProperty("finalSequence").GetInt64());
            Assert.That(projection, Is.EqualTo(expected), vector.GetProperty("mutation").GetString());
        }
    }
}
