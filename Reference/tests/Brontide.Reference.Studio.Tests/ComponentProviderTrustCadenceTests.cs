using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed class ComponentProviderTrustCadenceTests
{
    private sealed class FakeCycle(IEnumerable<string> codes) : IProviderServingTrustCycle
    {
        private readonly Queue<string> codes = new(codes);

        public Task<ProviderServingTrustCycleResult> RunAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ProviderServingTrustCycleResult(codes.Dequeue(), null!, null, 0));
    }

    private sealed class FakeDelay(CancellationTokenSource cancellation, bool cancelDuringGap)
        : IProviderServingTrustCadenceDelay
    {
        public List<TimeSpan> Durations { get; } = [];

        public Task<DateTimeOffset> DelayAsync(
            DateTimeOffset now,
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            if (cancelDuringGap)
            {
                cancellation.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }
            Durations.Add(duration);
            return Task.FromResult(now + duration);
        }
    }

    private sealed class FakePolicyCycle(string code) : IProviderPublisherTrustPolicyCycle
    {
        public int Calls { get; private set; }

        public Task<ProviderPublisherTrustPolicyPollResult> PollAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new ProviderPublisherTrustPolicyPollResult(
                code, null, 0, [], [], [], null, null!));
        }
    }

    private sealed class FakeSweepCycle(ProviderServingTrustSweepResult? result)
        : IProviderServingTrustSweepCycle
    {
        public int Calls { get; private set; }

        public ValueTask<ProviderServingTrustSweepResult?> SweepAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return ValueTask.FromResult(result);
        }
    }

    private static async Task<ProviderServingTrustCadenceResult> RunAsync(
        IReadOnlyList<string> codes,
        string cancellation,
        int maximumCycles = 2)
    {
        using var source = new CancellationTokenSource();
        if (cancellation == "before-first") source.Cancel();
        var delay = new FakeDelay(source, cancellation == "during-gap");
        return await new ProviderServingTrustCadence(
                ProviderServingTrustCadenceSchedule.Create(maximumCycles, TimeSpan.FromSeconds(5)))
            .RunAsync(new FakeCycle(codes), delay,
                new DateTimeOffset(2026, 8, 5, 8, 0, 0, TimeSpan.Zero), source.Token);
    }

    [Test]
    public void Cbi47_C1_cadence_is_bounded_and_explicit()
    {
        var schedule = ProviderServingTrustCadenceSchedule.Create(2, TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(schedule.MaximumCycles, Is.EqualTo(2));
            Assert.That(schedule.Interval, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ProviderServingTrustCadenceSchedule.Create(0, TimeSpan.FromSeconds(5)));
        });
    }

    [Test]
    public async Task Cbi47_C2_the_first_cycle_is_immediate_and_later_cycles_use_injected_time()
    {
        var result = await RunAsync(
            ["provider-trust-cycle-current", "provider-trust-cycle-current"], "none");

        Assert.Multiple(() =>
        {
            Assert.That(result.Cycles.Select(cycle => cycle.Instant), Is.EqualTo(new[]
            {
                new DateTimeOffset(2026, 8, 5, 8, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 5, 8, 0, 5, TimeSpan.Zero),
            }));
            Assert.That(result.Gaps, Is.EqualTo(new[] { TimeSpan.FromSeconds(5) }));
        });
    }

    [Test]
    public async Task Cbi47_C3_current_policy_precedes_any_serving_sweep()
    {
        var policy = new FakePolicyCycle("policy-poll-refused");
        var serving = new FakeSweepCycle(null);
        var result = await new ProviderServingTrustCycle(policy, serving)
            .RunAsync(DateTimeOffset.UnixEpoch, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("provider-trust-cycle-stopped"));
            Assert.That(policy.Calls, Is.EqualTo(1));
            Assert.That(serving.Calls, Is.Zero);
        });
    }

    [Test]
    public async Task Cbi47_C4_the_current_serving_set_is_swept_once()
    {
        var policy = new FakePolicyCycle("policy-poll-current");
        var serving = new FakeSweepCycle(null);
        var result = await new ProviderServingTrustCycle(policy, serving)
            .RunAsync(DateTimeOffset.UnixEpoch, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("provider-trust-cycle-current"));
            Assert.That(result.ServingCount, Is.Zero);
            Assert.That(serving.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Cbi47_C5_successful_withdrawal_does_not_stop_cadence() =>
        Assert.That((await RunAsync(
            ["provider-trust-cycle-withdrawn", "provider-trust-cycle-current"], "none")).Code,
            Is.EqualTo("provider-trust-cadence-complete"));

    [Test]
    public async Task Cbi47_C6_an_invalid_or_incomplete_sweep_stops_before_another_gap()
    {
        foreach (var sweepCode in new[]
                 {
                     "serving-trust-sweep-invalid",
                     "serving-trust-sweep-incomplete",
                     "serving-trust-sweep-cleanup-incomplete",
                 })
        {
            var cycle = await new ProviderServingTrustCycle(
                    new FakePolicyCycle("policy-poll-current"),
                    new FakeSweepCycle(new(sweepCode, "test", [], 0, 0)))
                .RunAsync(DateTimeOffset.UnixEpoch, CancellationToken.None);
            Assert.That(cycle.Code, Is.EqualTo("provider-trust-cycle-stopped"), sweepCode);
        }

        var result = await RunAsync(
            ["provider-trust-cycle-current", "provider-trust-cycle-stopped"], "none", 3);
        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("provider-trust-cadence-stopped"));
            Assert.That(result.Cycles, Has.Count.EqualTo(2));
            Assert.That(result.Gaps, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Cbi47_C7_cancellation_has_an_exact_boundary()
    {
        var before = await RunAsync(["provider-trust-cycle-current"], "before-first");
        var during = await RunAsync(
            ["provider-trust-cycle-current", "provider-trust-cycle-current"], "during-gap");
        Assert.Multiple(() =>
        {
            Assert.That(before.Code, Is.EqualTo("provider-trust-cadence-canceled"));
            Assert.That(before.Cycles, Is.Empty);
            Assert.That(during.Code, Is.EqualTo("provider-trust-cadence-canceled"));
            Assert.That(during.Cycles, Has.Count.EqualTo(1));
            Assert.That(during.Gaps, Is.Empty);
        });
    }

    [Test]
    public async Task Cbi47_C8_reference_executes_the_shared_cadence_vectors()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi47-provider-trust-cadence-vectors.json")));
        var schedule = fixture.RootElement.GetProperty("schedule");
        Assert.That(schedule.GetProperty("maximumCycles").GetInt32(),
            Is.EqualTo(ProviderServingTrustCadenceSchedule.Create(
                schedule.GetProperty("maximumCycles").GetInt32(),
                TimeSpan.FromSeconds(schedule.GetProperty("intervalSeconds").GetInt32())).MaximumCycles));

        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var name = vector.GetProperty("name").GetString()!;
            var result = await RunAsync(
                vector.GetProperty("cycleCodes").EnumerateArray()
                    .Select(value => value.GetString()!).ToArray(),
                vector.GetProperty("cancel").GetString()!);
            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()), name);
                Assert.That(result.Cycles, Has.Count.EqualTo(vector.GetProperty("expectedCycles").GetInt32()), name);
                Assert.That(result.Cycles.Select(cycle => cycle.Result.Code),
                    Is.EqualTo(vector.GetProperty("expectedCycleCodes").EnumerateArray()
                        .Select(value => value.GetString()).ToArray()), name);
                Assert.That(result.Cycles.Select(cycle => cycle.Instant),
                    Is.EqualTo(vector.GetProperty("expectedCycleInstants").EnumerateArray()
                        .Select(value => value.GetDateTimeOffset()).ToArray()), name);
                Assert.That(result.Gaps.Select(gap => (int)gap.TotalSeconds),
                    Is.EqualTo(vector.GetProperty("expectedGapsSeconds").EnumerateArray()
                        .Select(value => value.GetInt32()).ToArray()), name);
            });
        }
    }
}
