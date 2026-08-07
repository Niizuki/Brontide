using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private static readonly DateTimeOffset Cbi65Start = DateTimeOffset.FromUnixTimeSeconds(1_786_230_000);
    private static readonly TimeSpan Cbi65Interval = TimeSpan.FromSeconds(60);

    private static JsonDocument Cbi65Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi65-availability-baseline-vectors.json")));

    private static JsonElement Cbi65Vector(JsonDocument fixture, string name) =>
        fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(vector => vector.GetProperty("name").GetString() == name);

    /// <summary>
    /// Builds the snapshot a vector describes. The instants are the ones a cadence on the shared
    /// schedule prepares, so a vector never names a time the journal could not have recorded.
    /// </summary>
    private static ProviderTrustCadenceJournalSnapshot Cbi65Snapshot(JsonElement vector)
    {
        var cycles = vector.GetProperty("cycles").EnumerateArray()
            .Select((code, index) => new ProviderTrustCadenceJournalCycle(
                index, Cbi65Start + Cbi65Interval * index, code.GetString()!))
            .ToArray();
        return new(
            ProviderTrustCadenceRunId.Create("cbi65-run"), "durable-cadence-established", "waiting",
            8, Cbi65Interval, Cbi65Start + Cbi65Interval * cycles.Length, cycles,
            Enumerable.Repeat(Cbi65Interval, Math.Max(cycles.Length - 1, 0)).ToArray(),
            cycles.Length, 0, 0);
    }

    private static (string Code, DateTimeOffset? Instant) Cbi65Expected(JsonElement vector)
    {
        var cycle = vector.GetProperty("baselineCycle");
        return (vector.GetProperty("code").GetString()!,
            cycle.ValueKind == JsonValueKind.Null
                ? null
                : Cbi65Start + Cbi65Interval * cycle.GetInt32());
    }

    [Test]
    public void Shared_cbi65_vectors_derive_the_availability_baseline()
    {
        using var fixture = Cbi65Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = ProviderTrustCadenceAvailabilityRecovery.Derive(Cbi65Snapshot(vector));
            Assert.That((actual.Code, actual.Instant), Is.EqualTo(Cbi65Expected(vector)),
                $"vector {vector.GetProperty("name").GetString()}");
        }
    }

    [Test]
    public void Cbi65_C1_deriving_a_baseline_writes_nothing()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi65-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "cadence.bin");
        try
        {
            var journal = Cbi65Journal(path);
            Assert.That(journal.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));
            Assert.That(journal.CommitCycle(ProviderServingTrustCycleCodes.Current).Code,
                Is.EqualTo("durable-cadence-cycle-committed"));

            var before = File.ReadAllBytes(path);
            var derived = ProviderTrustCadenceAvailabilityRecovery.Derive(journal.Snapshot);
            // A refused derivation must be silent too, which a synthesised snapshot is the only way
            // to reach: the journal cannot produce the record that provokes it.
            var refused = ProviderTrustCadenceAvailabilityRecovery.Derive(
                journal.Snapshot with
                {
                    Cycles = [new(0, Cbi65Start, ProviderServingTrustCycleCodes.Stopped)],
                });
            Assert.Multiple(() =>
            {
                Assert.That(derived.Code, Is.EqualTo("cadence-baseline-derived"));
                Assert.That(refused.Code, Is.EqualTo("cadence-baseline-observation-invalid"));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
            });
        }
        finally
        {
            Cbi32DeleteTree(root);
        }
    }

    [Test]
    public void Cbi65_C2_the_vocabulary_answers_for_every_code_it_holds()
    {
        using var fixture = Cbi65Fixture();
        var classification = fixture.RootElement.GetProperty("classification");
        Assert.Multiple(() =>
        {
            foreach (var code in ProviderServingTrustCycleCodes.All)
            {
                var expected = classification.GetProperty(code);
                Assert.That(ProviderServingTrustCycleCodes.Establishes(code),
                    Is.EqualTo(expected.ValueKind == JsonValueKind.Null ? null : expected.GetBoolean()),
                    code);
                // Every code the vocabulary answers for is one a cadence may continue after, which is
                // what makes the unanswered ones unreachable in a record CBI48 wrote.
                if (ProviderServingTrustCycleCodes.Establishes(code) is not null)
                    Assert.That(ProviderServingTrustCycleCodes.Continues(code), Is.True, code);
            }
        });
    }

    /// <summary>
    /// The derivation reproduces the instant the live cadence held rather than a value that merely
    /// looks plausible: the same run is executed against a real journal and the two are compared.
    /// </summary>
    [Test]
    public async Task Cbi65_C2_a_replayed_run_yields_the_baseline_the_live_cadence_held()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi65-{Guid.NewGuid():N}");
        try
        {
            var journal = Cbi65Journal(Path.Combine(root, "cadence.bin"));
            var (cycle, observed) = Cbi65Cadence(
                ["current", "current", "transport", "transport"], baseline: null);
            await Cbi65AdvanceAsync(journal, cycle, 4);

            var derived = ProviderTrustCadenceAvailabilityRecovery.Derive(journal.Snapshot);
            Assert.Multiple(() =>
            {
                // The live cadence anchored its deadline on the second current cycle.
                Assert.That(derived.Instant, Is.EqualTo(Cbi65Start + Cbi65Interval));
                Assert.That(observed, Has.Count.EqualTo(2));
                foreach (var availability in observed)
                    Assert.That(availability.Deadline,
                        Is.EqualTo(derived.Instant + TimeSpan.FromMinutes(5)));
            });
        }
        finally
        {
            Cbi32DeleteTree(root);
        }
    }

    [Test]
    public void Cbi65_C3_the_baseline_does_not_depend_on_the_run_or_its_terminal_code()
    {
        using var fixture = Cbi65Fixture();
        var vector = Cbi65Vector(fixture, "an-outage-does-not-move-it");
        var snapshot = Cbi65Snapshot(vector);
        var ended = snapshot with
        {
            RunIdentity = ProviderTrustCadenceRunId.Create("cbi65-some-earlier-run"),
            Phase = "terminal",
            Code = "durable-cadence-complete",
        };
        Assert.Multiple(() =>
        {
            // A host that shut down cleanly holds the same fact as one that crashed; refusing the
            // completed run would stop service at its first outage for no gain.
            Assert.That(ProviderTrustCadenceAvailabilityRecovery.Derive(ended),
                Is.EqualTo(ProviderTrustCadenceAvailabilityRecovery.Derive(snapshot)));
            Assert.That(ProviderTrustCadenceAvailabilityRecovery.Derive(ended).Code,
                Is.EqualTo("cadence-baseline-derived"));
        });
    }

    [Test]
    public void Cbi65_C4_a_record_with_no_establishing_cycle_yields_no_instant()
    {
        using var fixture = Cbi65Fixture();
        foreach (var name in new[]
                 {
                     "a-run-that-never-reached-the-endpoint-has-none",
                     "an-empty-record-has-none",
                 })
        {
            var actual = ProviderTrustCadenceAvailabilityRecovery.Derive(
                Cbi65Snapshot(Cbi65Vector(fixture, name)));
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo("cadence-baseline-absent"), name);
                Assert.That(actual.Instant, Is.Null, name);
            });
        }
    }

    [Test]
    public void Cbi65_C5_an_attempt_in_flight_changes_no_derivation()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi65-{Guid.NewGuid():N}");
        try
        {
            var journal = Cbi65Journal(Path.Combine(root, "cadence.bin"));
            journal.BeginCycle();
            journal.CommitCycle(ProviderServingTrustCycleCodes.Current);
            journal.CompleteGap(Cbi65Start + Cbi65Interval);
            var committed = ProviderTrustCadenceAvailabilityRecovery.Derive(journal.Snapshot);

            Assert.That(journal.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));
            var inFlight = ProviderTrustCadenceAvailabilityRecovery.Derive(journal.Snapshot);
            Assert.Multiple(() =>
            {
                Assert.That(journal.Snapshot.Phase, Is.EqualTo("in-flight"));
                Assert.That(inFlight, Is.EqualTo(committed));
            });
        }
        finally
        {
            Cbi32DeleteTree(root);
        }
    }

    /// <summary>
    /// C6 claims CBI48 cannot place an unclassifiable observation in front of another. That is a claim
    /// about a dependency, so it is probed: every continuing code keeps the run going and the first
    /// non-continuing one ends it in the same write.
    /// </summary>
    [Test]
    public void Cbi65_C6_the_journal_never_records_an_unclassifiable_observation_before_another()
    {
        foreach (var code in ProviderServingTrustCycleCodes.All)
        {
            var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi65-{Guid.NewGuid():N}");
            try
            {
                var journal = Cbi65Journal(Path.Combine(root, "cadence.bin"));
                journal.BeginCycle();
                var committed = journal.CommitCycle(code);
                var unanswered = ProviderServingTrustCycleCodes.Establishes(code) is null;
                Assert.Multiple(() =>
                {
                    Assert.That(committed.Snapshot.Phase,
                        Is.EqualTo(unanswered ? "terminal" : "waiting"), code);
                    // A terminal journal accepts nothing further, so no later observation can follow
                    // the one the derivation could not classify.
                    if (unanswered)
                        Assert.That(journal.BeginCycle().Code, Does.StartWith("durable-cadence-"), code);
                    Assert.That(ProviderTrustCadenceAvailabilityRecovery.Derive(journal.Snapshot).Code,
                        Is.EqualTo(unanswered
                            ? "cadence-baseline-observation-invalid"
                            : ProviderServingTrustCycleCodes.Establishes(code) == true
                                ? "cadence-baseline-derived"
                                : "cadence-baseline-absent"), code);
                });
            }
            finally
            {
                Cbi32DeleteTree(root);
            }
        }
    }

    /// <summary>
    /// The composed effect. Three successors run the same outage cycle at the same instant and differ
    /// only in the baseline they start from: the derived one, none at all, and the restart instant —
    /// the tempting wrong answer, which renews grace on every restart so a crash loop never expires.
    /// </summary>
    [Test]
    public async Task Cbi65_C7_a_resumed_cadence_continues_the_outage_it_was_in()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi65-{Guid.NewGuid():N}");
        try
        {
            var journal = Cbi65Journal(Path.Combine(root, "cadence.bin"));
            var (first, before) = Cbi65Cadence(["current", "transport", "transport"], baseline: null);
            await Cbi65AdvanceAsync(journal, first, 3);
            var derived = ProviderTrustCadenceAvailabilityRecovery.Derive(journal.Snapshot);

            var restart = journal.Snapshot.PreparedInstant;
            var resumed = await Cbi65OutageAsync(derived.Instant, restart);
            var none = await Cbi65OutageAsync(null, restart);
            var renewed = await Cbi65OutageAsync(restart, restart);

            Assert.Multiple(() =>
            {
                // The outage the host was in is the outage it comes back to.
                Assert.That(resumed!.Deadline, Is.EqualTo(before[^1].Deadline));
                Assert.That(resumed.DecisionCode, Is.EqualTo("offline-idle"));
                // Without a baseline the run stops service instead, which is CBI64's stated limit and
                // what this slice removes.
                Assert.That(none!.DecisionCode, Is.EqualTo("offline-service-stop-required"));
                // Anchoring on the restart moves the deadline forward, so an outage spanning restarts
                // would never expire.
                Assert.That(renewed!.Deadline, Is.GreaterThan(before[^1].Deadline!.Value));
            });
        }
        finally
        {
            Cbi32DeleteTree(root);
        }
    }

    private static async Task<ProviderTrustCycleAvailability?> Cbi65OutageAsync(
        DateTimeOffset? baseline, DateTimeOffset instant)
    {
        var (cycle, observed) = Cbi65Cadence(["transport"], baseline);
        await cycle.RunAsync(instant, CancellationToken.None);
        return observed.SingleOrDefault();
    }

    [Test]
    public void Cbi66_C1_the_journal_records_the_gap_that_elapsed()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi66-{Guid.NewGuid():N}");
        try
        {
            var journal = Cbi65Journal(Path.Combine(root, "cadence.bin"));
            journal.BeginCycle();
            journal.CommitCycle(ProviderServingTrustCycleCodes.Current);
            // A gap shorter than the schedule interval, which is what an availability retry instant
            // asks for. The journal accepts the instant and must record the time that passed.
            var shortened = TimeSpan.FromSeconds(20);
            var completed = journal.CompleteGap(Cbi65Start + shortened);
            var snapshot = completed.Snapshot;
            Assert.Multiple(() =>
            {
                Assert.That(completed.Code, Is.EqualTo("durable-cadence-gap-completed"));
                Assert.That(snapshot.Gaps.Single(), Is.EqualTo(shortened));
                Assert.That(snapshot.Gaps.Single(),
                    Is.EqualTo(snapshot.PreparedInstant - snapshot.Cycles[^1].Instant));
            });
        }
        finally
        {
            Cbi32DeleteTree(root);
        }
    }

    private static JsonDocument Cbi66Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi66-retry-aware-gaps-vectors.json")));

    private static JsonElement Cbi66Vector(JsonDocument fixture, string name) =>
        fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(vector => vector.GetProperty("name").GetString() == name);

    private sealed record Cbi66Observation(
        string CadenceCode, string Instants, string Gaps, string DecisionCodes, int? Expiry);

    /// <summary>
    /// Runs one scripted cadence over an empty serving set, which keeps CBI49's decision, deadline and
    /// retry instant real without launching a provider. Instants and gaps are reported in seconds from
    /// the run start, which is how the vectors state them.
    /// </summary>
    private static async Task<Cbi66Observation> Cbi66RunAsync(JsonElement fixture, JsonElement vector)
    {
        var start = DateTimeOffset.FromUnixTimeSeconds(
            fixture.GetProperty("schedule").GetProperty("startUnixSeconds").GetInt64());
        var interval = TimeSpan.FromSeconds(vector.GetProperty("intervalSeconds").GetInt32());
        var polls = fixture.GetProperty("polls");
        var script = vector.GetProperty("cycles").EnumerateArray().Select(name =>
        {
            var poll = polls.GetProperty(name.GetString()!);
            return (poll.GetProperty("code").GetString()!,
                (string?)poll.GetProperty("lastAttemptCode").GetString());
        }).ToArray();

        var policy = new Cbi64PolicyCycle(script, null!);
        var cadence = new ProviderServingTrustCadence(
            ProviderServingTrustCadenceSchedule.Create(script.Length, interval));
        var cycle = new Cbi66RecordingCycle(
            new ProviderAvailabilityTrustCycle(
                new ProviderServingTrustCycle(policy, new Cbi64Sweep()),
                new ProviderOfflineEnforcementCycle(
                    ProviderTrustOfflinePolicy.Create(
                        TimeSpan.FromSeconds(vector.GetProperty("graceSeconds").GetInt32()),
                        TimeSpan.FromSeconds(vector.GetProperty("retrySeconds").GetInt32())),
                    _ => ValueTask.FromResult<IReadOnlyList<ProviderServingActivation>>([]),
                    "offline availability withdrawn")),
            policy);

        var result = await cadence.RunAsync(cycle, new Cbi66Delay(), start);
        var decisions = result.Cycles
            .Select(item => item.Result.Availability?.DecisionCode ?? "none").ToArray();
        var expiry = result.Cycles
            .Where(item => item.Result.Availability?.DecisionCode == "offline-grace-expired")
            .Select(item => (int?)(item.Instant - start).TotalSeconds)
            .FirstOrDefault();
        return new(
            result.Code,
            Cbi60Join(result.Cycles.Select(item => (int)(item.Instant - start).TotalSeconds)),
            Cbi60Join(result.Gaps.Select(gap => (int)gap.TotalSeconds)),
            Cbi60Join(decisions),
            expiry);
    }

    private static Cbi66Observation Cbi66Expected(JsonElement vector)
    {
        var expiry = vector.GetProperty("expirySeconds");
        return new(
            vector.GetProperty("cadenceCode").GetString()!,
            Cbi60Join(vector.GetProperty("instantSeconds").EnumerateArray().Select(v => v.GetInt32())),
            Cbi60Join(vector.GetProperty("gapSeconds").EnumerateArray().Select(v => v.GetInt32())),
            Cbi60Join(vector.GetProperty("decisionCodes").EnumerateArray()
                .Select(v => v.ValueKind == JsonValueKind.Null ? "none" : v.GetString())),
            expiry.ValueKind == JsonValueKind.Null ? null : expiry.GetInt32());
    }

    /// <summary>Waits exactly the duration the cadence asked for, so a vector pins the cadence's own arithmetic.</summary>
    private sealed class Cbi66Delay : IProviderServingTrustCadenceDelay
    {
        public Task<DateTimeOffset> DelayAsync(
            DateTimeOffset now, TimeSpan duration, CancellationToken cancellationToken) =>
            Task.FromResult(now + duration);
    }

    private sealed class Cbi66RecordingCycle(IProviderServingTrustCycle inner, Cbi64PolicyCycle policy)
        : IProviderServingTrustCycle
    {
        public async Task<ProviderServingTrustCycleResult> RunAsync(
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            var result = await inner.RunAsync(now, cancellationToken).ConfigureAwait(false);
            policy.Cycle++;
            return result;
        }
    }

    [Test]
    public async Task Shared_cbi66_vectors_shorten_a_cadence_gap_to_the_retry_instant()
    {
        using var fixture = Cbi66Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi66RunAsync(fixture.RootElement, vector);
            Assert.That(actual, Is.EqualTo(Cbi66Expected(vector)),
                $"vector {vector.GetProperty("name").GetString()}");
        }
    }

    [Test]
    public void Cbi66_C1_a_gap_longer_than_the_interval_is_refused()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi66-{Guid.NewGuid():N}");
        try
        {
            var journal = Cbi65Journal(Path.Combine(root, "cadence.bin"));
            journal.BeginCycle();
            journal.CommitCycle(ProviderServingTrustCycleCodes.Current);
            // The interval is the host's own upper bound, so a gap beyond it is not a shortened one.
            var refused = journal.CompleteGap(Cbi65Start + Cbi65Interval + TimeSpan.FromSeconds(1));
            Assert.Multiple(() =>
            {
                Assert.That(refused.Code, Is.EqualTo("durable-cadence-gap-invalid"));
                Assert.That(refused.Snapshot.Gaps, Is.Empty);
                Assert.That(journal.CompleteGap(Cbi65Start + Cbi65Interval).Code,
                    Is.EqualTo("durable-cadence-gap-completed"));
            });
        }
        finally
        {
            Cbi32DeleteTree(root);
        }
    }

    [Test]
    public async Task Cbi66_C3_every_gap_is_positive_and_within_the_interval()
    {
        using var fixture = Cbi66Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var name = vector.GetProperty("name").GetString();
            var interval = vector.GetProperty("intervalSeconds").GetInt32();
            var actual = await Cbi66RunAsync(fixture.RootElement, vector);
            foreach (var gap in Cbi60Split(actual.Gaps).Select(int.Parse))
            {
                Assert.That(gap, Is.GreaterThan(0), name);
                Assert.That(gap, Is.LessThanOrEqualTo(interval), name);
            }
        }
    }

    [Test]
    public async Task Cbi66_C4_a_run_with_no_outage_keeps_the_interval()
    {
        using var fixture = Cbi66Fixture();
        var actual = await Cbi66RunAsync(
            fixture.RootElement, Cbi66Vector(fixture, "a-run-with-no-outage-keeps-the-interval"));
        Assert.Multiple(() =>
        {
            Assert.That(actual.Gaps, Is.EqualTo("30,30"));
            Assert.That(actual.DecisionCodes, Is.EqualTo("none,none,none"));
        });
    }

    [Test]
    public void Cbi66_C5_a_record_whose_gaps_are_all_the_interval_reopens_unchanged()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi66-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "cadence.bin");
        try
        {
            var journal = Cbi65Journal(path);
            journal.BeginCycle();
            journal.CommitCycle(ProviderServingTrustCycleCodes.Current);
            journal.CompleteGap(Cbi65Start + Cbi65Interval);
            journal.BeginCycle();
            journal.CommitCycle(ProviderServingTrustCycleCodes.Current);
            var written = journal.Snapshot.Gaps;

            var reopened = DurableProviderTrustCadenceJournal.Open(
                path, ProviderTrustCadenceRunId.Create("cbi65-run"));
            Assert.Multiple(() =>
            {
                Assert.That(written, Is.EqualTo(new[] { Cbi65Interval }));
                Assert.That(reopened.Journal, Is.Not.Null);
                Assert.That(reopened.Journal!.Snapshot.Gaps, Is.EqualTo(written));
            });
        }
        finally
        {
            Cbi32DeleteTree(root);
        }
    }

    /// <summary>
    /// A cadence cannot detect an outage before it looks, so the first outage cycle still falls on the
    /// ordinary interval. Where one was seen before the deadline, expiry must land on the deadline
    /// itself rather than at the next scheduled cycle.
    /// </summary>
    [Test]
    public async Task Cbi66_C6_expiry_is_observed_at_the_deadline_once_an_outage_is_seen()
    {
        using var fixture = Cbi66Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var name = vector.GetProperty("name").GetString();
            var actual = await Cbi66RunAsync(fixture.RootElement, vector);
            if (actual.Expiry is not { } expiry) continue;
            var decisions = Cbi60Split(actual.DecisionCodes);
            var instants = Cbi60Split(actual.Instants).Select(int.Parse).ToArray();
            var establishing = decisions.Select((code, index) => (code, index))
                .Where(item => item.code == "none").Select(item => instants[item.index]).ToArray();
            var deadline = establishing[^1] + vector.GetProperty("graceSeconds").GetInt32();
            var sawOutageFirst = decisions.Any(code => code == "offline-idle");
            Assert.That(expiry, sawOutageFirst
                ? Is.EqualTo(deadline)
                : Is.GreaterThanOrEqualTo(deadline), name);
        }
    }

    /// <summary>
    /// The shared vectors run over an empty serving set, so their gaps are the gaps a serving cadence
    /// waits only if CBI49's retry instant does not depend on the serving count. That is a claim about
    /// a dependency, so it is probed rather than assumed.
    /// </summary>
    [Test]
    public void Cbi66_C2_the_retry_instant_does_not_depend_on_the_serving_count()
    {
        var policy = ProviderTrustOfflinePolicy.Create(
            TimeSpan.FromSeconds(100), TimeSpan.FromSeconds(40));
        var lastCurrent = Cbi65Start;
        var now = Cbi65Start + TimeSpan.FromSeconds(30);
        var idle = policy.Evaluate(
            now, lastCurrent, "policy-poll-exhausted", "policy-distribution-transport-failed", 0);
        var serving = policy.Evaluate(
            now, lastCurrent, "policy-poll-exhausted", "policy-distribution-transport-failed", 2);
        Assert.Multiple(() =>
        {
            Assert.That(idle.Code, Is.EqualTo("offline-idle"));
            Assert.That(serving.Code, Is.EqualTo("offline-existing-service"));
            Assert.That(idle.RetryAt, Is.EqualTo(serving.RetryAt));
            Assert.That(idle.Deadline, Is.EqualTo(serving.Deadline));
        });
    }

    private static DurableProviderTrustCadenceJournal Cbi65Journal(string path) =>
        DurableProviderTrustCadenceJournal.Establish(
            path, ProviderTrustCadenceRunId.Create("cbi65-run"),
            ProviderServingTrustCadenceSchedule.Create(8, Cbi65Interval), Cbi65Start).Journal!;

    /// <summary>
    /// One availability-governed cycle over a scripted policy endpoint and an empty serving set, which
    /// keeps CBI49's decision and its deadline real without launching a provider. The returned list
    /// collects every availability observation the cycle reported.
    /// </summary>
    private static (IProviderServingTrustCycle Cycle, List<ProviderTrustCycleAvailability> Observed)
        Cbi65Cadence(IReadOnlyList<string> script, DateTimeOffset? baseline)
    {
        var polls = script.Select(name => name == "current"
            ? ("policy-poll-current", (string?)"policy-distribution-current")
            : ("policy-poll-exhausted", "policy-distribution-transport-failed")).ToArray();
        var policy = new Cbi64PolicyCycle(polls, null!);
        var observed = new List<ProviderTrustCycleAvailability>();
        var cycle = new ProviderAvailabilityTrustCycle(
            new ProviderServingTrustCycle(policy, new Cbi64Sweep()),
            new ProviderOfflineEnforcementCycle(
                ProviderTrustOfflinePolicy.Create(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1)),
                _ => ValueTask.FromResult<IReadOnlyList<ProviderServingActivation>>([]),
                "offline availability withdrawn"),
            baseline);
        return (new Cbi65RecordingCycle(cycle, policy, observed), observed);
    }

    private sealed class Cbi65RecordingCycle(
        IProviderServingTrustCycle inner,
        Cbi64PolicyCycle policy,
        List<ProviderTrustCycleAvailability> observed) : IProviderServingTrustCycle
    {
        public async Task<ProviderServingTrustCycleResult> RunAsync(
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            var result = await inner.RunAsync(now, cancellationToken).ConfigureAwait(false);
            if (result.Availability is { } availability) observed.Add(availability);
            policy.Cycle++;
            return result;
        }
    }

    /// <summary>
    /// Drives the journal the way a host does — begin, run, commit, complete the gap — so the recorded
    /// instants are the ones the cycles actually ran at.
    /// </summary>
    private static async Task Cbi65AdvanceAsync(
        DurableProviderTrustCadenceJournal journal,
        IProviderServingTrustCycle cycle,
        int cycles)
    {
        for (var index = 0; index < cycles; index++)
        {
            var instant = journal.Snapshot.PreparedInstant;
            Assert.That(journal.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));
            var result = await cycle.RunAsync(instant, CancellationToken.None);
            var committed = journal.CommitCycle(result.Code);
            if (committed.Snapshot.Phase == "waiting")
                journal.CompleteGap(instant + Cbi65Interval);
        }
    }
}
