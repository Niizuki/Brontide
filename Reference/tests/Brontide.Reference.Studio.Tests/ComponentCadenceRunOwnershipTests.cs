using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

[TestFixture]
public sealed class ComponentCadenceRunOwnershipTests
{
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi68-cadence-run-ownership-vectors.json")));

    private sealed record Observation(
        string Codes, long FinalEpoch, int CommittedCycles, string Phase);

    /// <summary>
    /// Runs one scripted sequence of holder actions over a single journal. `first` is the establishing
    /// holder and `second` is the one the first open returns, so a vector can drive a superseded holder
    /// and the current owner independently.
    /// </summary>
    private static Observation Run(JsonElement fixture, JsonElement vector)
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi68-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "cadence.bin");
        var run = ProviderTrustCadenceRunId.Create(fixture.GetProperty("runIdentity").GetString()!);
        var start = DateTimeOffset.FromUnixTimeSeconds(fixture.GetProperty("startUnixSeconds").GetInt64());
        var interval = TimeSpan.FromSeconds(fixture.GetProperty("intervalSeconds").GetInt32());
        var schedule = ProviderServingTrustCadenceSchedule.Create(
            fixture.GetProperty("maximumCycles").GetInt32(), interval);
        try
        {
            DurableProviderTrustCadenceJournal? first = null;
            DurableProviderTrustCadenceJournal? second = null;
            var codes = new List<string>();

            foreach (var step in vector.GetProperty("steps").EnumerateArray().Select(v => v.GetString()!))
            {
                switch (step)
                {
                    case "establish":
                        var established = DurableProviderTrustCadenceJournal.Establish(path, run, schedule, start);
                        first = established.Journal;
                        codes.Add(established.Code);
                        break;
                    case "open":
                        var opened = DurableProviderTrustCadenceJournal.Open(path, run);
                        second ??= opened.Journal;
                        codes.Add(opened.Code);
                        break;
                    default:
                        var holder = step.StartsWith("first:", StringComparison.Ordinal) ? first! : second!;
                        var verb = step[(step.IndexOf(':') + 1)..];
                        codes.Add(verb switch
                        {
                            "begin" => holder.BeginCycle().Code,
                            "commit" => holder.CommitCycle(ProviderServingTrustCycleCodes.Current).Code,
                            "gap" => holder.CompleteGap(holder.Snapshot.PreparedInstant + interval).Code,
                            _ => throw new ArgumentOutOfRangeException(nameof(vector), verb, null),
                        });
                        break;
                }
            }

            // Reopening is the only way to read what the record actually retains, which is the point:
            // a holder's own snapshot is its view, and the vector pins the durable one.
            var reopened = DurableProviderTrustCadenceJournal.Open(path, run).Journal!;
            return new(
                string.Join(",", codes), reopened.OwnerEpoch,
                reopened.Snapshot.Cycles.Count, reopened.Snapshot.Phase);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }

    private static Observation Expected(JsonElement vector) => new(
        string.Join(",", vector.GetProperty("codes").EnumerateArray().Select(v => v.GetString())),
        vector.GetProperty("finalEpoch").GetInt64(),
        vector.GetProperty("committedCycles").GetInt32(),
        vector.GetProperty("phase").GetString()!);

    private static JsonElement Vector(JsonDocument fixture, string name) =>
        fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(v => v.GetProperty("name").GetString() == name);

    [Test]
    public void Shared_cbi68_vectors_fence_a_superseded_holder()
    {
        using var fixture = Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            Assert.That(Run(fixture.RootElement, vector), Is.EqualTo(Expected(vector)),
                $"vector {vector.GetProperty("name").GetString()}");
        }
    }

    [Test]
    public void Cbi68_C1_every_write_advances_the_epoch()
    {
        using var fixture = Fixture();
        var actual = Run(
            fixture.RootElement, Vector(fixture, "establishing-owns-and-each-write-advances-the-epoch"));
        // Establish, begin, commit: three writes, so a holder that saw any earlier one is detectable.
        Assert.That(actual.FinalEpoch, Is.EqualTo(3));
    }

    /// <summary>
    /// Opening is how a host inspects a run, so it must not take it. CBI48's C3 opens a journal from
    /// inside a running cycle and then expects the driving holder to commit; its C5 and C7 compare the
    /// durable bytes across an open.
    /// </summary>
    [Test]
    public void Cbi68_C2_opening_observes_without_taking_the_run()
    {
        using var fixture = Fixture();
        var observed = Run(fixture.RootElement, Vector(fixture, "opening-observes-without-taking-the-run"));
        var inert = Run(fixture.RootElement, Vector(fixture, "repeated-opening-is-inert"));
        Assert.Multiple(() =>
        {
            Assert.That(observed.Codes, Does.EndWith("durable-cadence-cycle-started"));
            // Establish plus one open plus one open plus one begin: only two of those write.
            Assert.That(inert.FinalEpoch, Is.EqualTo(2));
        });
    }

    /// <summary>
    /// The defect the slice exists for, in both orderings. A holder whose memory was behind used to
    /// write its stale copy back and erase a committed cycle; a holder superseded while current used
    /// to move the phase of a run it no longer owned. Both are now refused, and the record is
    /// untouched either way.
    /// </summary>
    [Test]
    public void Cbi68_C3_a_superseded_holder_writes_nothing()
    {
        using var fixture = Fixture();
        foreach (var name in new[]
                 {
                     "a-writing-holder-supersedes-the-one-that-was-driving",
                     "a-holder-whose-memory-is-behind-erases-nothing",
                 })
        {
            var vector = Vector(fixture, name);
            var actual = Run(fixture.RootElement, vector);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Codes, Does.Contain("durable-cadence-owner-superseded"), name);
                Assert.That(actual.CommittedCycles,
                    Is.EqualTo(vector.GetProperty("committedCycles").GetInt32()), name);
                Assert.That(actual.Phase, Is.EqualTo(vector.GetProperty("phase").GetString()), name);
            });
        }
    }

    /// <summary>
    /// The guard lives at the top of each transition rather than only at the write, so a future
    /// transition that forgot it would report a phase precondition instead of the lost run. This walks
    /// every transition a superseded holder can attempt and requires all of them to say so.
    /// </summary>
    [Test]
    public void Cbi68_C3_every_transition_reports_the_lost_run_rather_than_a_phase()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi68-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "cadence.bin");
        var run = ProviderTrustCadenceRunId.Create("cadence-run.test.1");
        var start = DateTimeOffset.FromUnixTimeSeconds(1_786_230_000);
        try
        {
            var first = DurableProviderTrustCadenceJournal.Establish(
                path, run, ProviderServingTrustCadenceSchedule.Create(4, TimeSpan.FromSeconds(60)),
                start).Journal!;
            var second = DurableProviderTrustCadenceJournal.Open(path, run).Journal!;
            second.BeginCycle();

            var transitions = new (string Name, Func<ProviderTrustCadenceJournalTransitionResult> Act)[]
            {
                ("BeginCycle", () => first.BeginCycle()),
                ("CommitCycle", () => first.CommitCycle(ProviderServingTrustCycleCodes.Current)),
                ("CompleteGap", () => first.CompleteGap(start + TimeSpan.FromSeconds(60))),
                ("ResolveInterrupted",
                    () => first.ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Retry)),
            };
            Assert.Multiple(() =>
            {
                foreach (var (name, act) in transitions)
                    Assert.That(act().Code, Is.EqualTo("durable-cadence-owner-superseded"), name);
            });
        }
        finally
        {
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }

    [Test]
    public void Cbi68_C4_a_record_written_before_this_slice_is_adopted()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi68-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "cadence.bin");
        var run = ProviderTrustCadenceRunId.Create("cadence-run.test.1");
        var start = DateTimeOffset.FromUnixTimeSeconds(1_786_230_000);
        try
        {
            DurableProviderTrustCadenceJournal.Establish(
                path, run, ProviderServingTrustCadenceSchedule.Create(4, TimeSpan.FromSeconds(60)), start);
            // Rewrite the record without an owner epoch, which is what a pre-CBI68 host wrote.
            var bytes = File.ReadAllBytes(path);
            var json = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length - 32);
            var stripped = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
            stripped.Remove("OwnerEpoch");
            var body = System.Text.Encoding.UTF8.GetBytes(stripped.ToJsonString());
            File.WriteAllBytes(path, [.. body, .. System.Security.Cryptography.SHA256.HashData(body)]);

            var opened = DurableProviderTrustCadenceJournal.Open(path, run);
            var seenBeforeWriting = opened.Journal!.OwnerEpoch;
            var began = opened.Journal.BeginCycle();
            var reopened = DurableProviderTrustCadenceJournal.Open(path, run).Journal!;
            Assert.Multiple(() =>
            {
                // Nothing special happens: a record that never carried an epoch reads as zero, and the
                // first write claims it at one. Because opening observes rather than claims, the
                // migration needs no adoption rule of its own — a host mid-run keeps its run.
                Assert.That(opened.Code, Is.EqualTo("durable-cadence-recovered"));
                Assert.That(seenBeforeWriting, Is.Zero);
                Assert.That(began.Code, Is.EqualTo("durable-cadence-cycle-started"));
                Assert.That(reopened.OwnerEpoch, Is.EqualTo(1));
            });
        }
        finally
        {
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }

    /// <summary>
    /// An unreadable record cannot prove supersession, and CBI48 already defines what happens to a
    /// journal whose write path fails. The guard therefore leaves that outcome alone rather than
    /// reclassifying it, which its own C3 requires: a journal replaced by a directory still reports
    /// a failed write.
    /// </summary>
    [Test]
    public void Cbi68_C5_an_unreadable_record_keeps_the_outcome_cbi48_already_defines()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi68-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "cadence.bin");
        var run = ProviderTrustCadenceRunId.Create("cadence-run.test.1");
        var start = DateTimeOffset.FromUnixTimeSeconds(1_786_230_000);
        try
        {
            var journal = DurableProviderTrustCadenceJournal.Establish(
                path, run, ProviderServingTrustCadenceSchedule.Create(4, TimeSpan.FromSeconds(60)),
                start).Journal!;
            File.Delete(path);
            Directory.CreateDirectory(path);
            Assert.That(journal.BeginCycle().Code, Is.EqualTo("durable-cadence-write-failed"));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }

    [Test]
    public void Cbi68_C6_observation_is_not_gated_by_ownership()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi68-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "cadence.bin");
        var run = ProviderTrustCadenceRunId.Create("cadence-run.test.1");
        var start = DateTimeOffset.FromUnixTimeSeconds(1_786_230_000);
        try
        {
            var first = DurableProviderTrustCadenceJournal.Establish(
                path, run, ProviderServingTrustCadenceSchedule.Create(4, TimeSpan.FromSeconds(60)),
                start).Journal!;
            DurableProviderTrustCadenceJournal.Open(path, run).Journal!.BeginCycle();
            // A superseded holder can still say what it last knew, which is what a host needs in order
            // to report why it stopped. What it cannot do is make that view durable.
            Assert.Multiple(() =>
            {
                Assert.That(first.Snapshot.RunIdentity, Is.EqualTo(run));
                Assert.That(first.Snapshot.Phase, Is.EqualTo("ready"));
                Assert.That(first.BeginCycle().Code, Is.EqualTo("durable-cadence-owner-superseded"));
            });
        }
        finally
        {
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }
}
