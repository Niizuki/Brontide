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
