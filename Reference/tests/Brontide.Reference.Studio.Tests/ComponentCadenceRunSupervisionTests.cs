using System.Diagnostics;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

[TestFixture]
public sealed class ComponentCadenceRunSupervisionTests
{
    private static readonly ProviderTrustCadenceRunId RunId =
        ProviderTrustCadenceRunId.Create("cadence-run.test.1");

    private static readonly DateTimeOffset Start = new(2026, 8, 9, 8, 0, 0, TimeSpan.Zero);

    private static readonly ProviderServingTrustCadenceSchedule Schedule =
        ProviderServingTrustCadenceSchedule.Create(4, TimeSpan.FromSeconds(60));

    private sealed class TemporaryJournal : IDisposable
    {
        private readonly string root = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"brontide-cbi69-{Guid.NewGuid():N}");

        public string Path => System.IO.Path.Combine(root, "cadence.bin");

        public void Dispose()
        {
            for (var attempt = 0; attempt < 250; attempt++)
            {
                try
                {
                    if (Directory.Exists(root)) Directory.Delete(root, true);
                    return;
                }
                catch (IOException) when (attempt < 249) { Thread.Sleep(20); }
                catch (UnauthorizedAccessException) { return; }
            }
        }
    }

    private sealed class CountingCycle(Action? during = null) : IProviderServingTrustCycle
    {
        public int Calls { get; private set; }

        public Task<ProviderServingTrustCycleResult> RunAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            Calls++;
            during?.Invoke();
            return Task.FromResult(new ProviderServingTrustCycleResult(
                ProviderServingTrustCycleCodes.Current, null, null, 0));
        }
    }

    private sealed class ImmediateDelay : IProviderServingTrustCadenceDelay
    {
        public Task<DateTimeOffset> DelayAsync(
            DateTimeOffset now,
            TimeSpan duration,
            CancellationToken cancellationToken) => Task.FromResult(now + duration);
    }

    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllText(System.IO.Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi69-cadence-run-supervision-vectors.json")));

    private static JsonElement Vector(JsonDocument fixture, string name) =>
        fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(v => v.GetProperty("name").GetString() == name);

    private sealed record Observation(
        string Codes, long FinalEpoch, int CommittedCycles, string Phase);

    /// <summary>
    /// Runs one scripted sequence over a single journal under two supervisors. `driving` is the
    /// establishing holder, `competitor` is the one an open returns, and `a`/`b` are supervisions of
    /// the same run, so a vector can drive the excluded party and the current one independently.
    /// </summary>
    private static async Task<Observation> RunAsync(JsonElement fixture, JsonElement vector)
    {
        using var temporary = new TemporaryJournal();
        var interval = TimeSpan.FromSeconds(fixture.GetProperty("intervalSeconds").GetInt32());
        var run = ProviderTrustCadenceRunId.Create(fixture.GetProperty("runIdentity").GetString()!);
        var start = DateTimeOffset.FromUnixTimeSeconds(fixture.GetProperty("startUnixSeconds").GetInt64());
        var schedule = ProviderServingTrustCadenceSchedule.Create(
            fixture.GetProperty("maximumCycles").GetInt32(), interval);

        DurableProviderTrustCadenceJournal? driving = null;
        DurableProviderTrustCadenceJournal? competitor = null;
        var supervisions = new Dictionary<string, ProviderTrustCadenceRunSupervision?>(StringComparer.Ordinal);
        var codes = new List<string>();
        try
        {
            foreach (var step in vector.GetProperty("steps").EnumerateArray().Select(v => v.GetString()!))
            {
                var name = step[(step.IndexOf(':') + 1)..];
                switch (step)
                {
                    case "establish":
                        var established = DurableProviderTrustCadenceJournal.Establish(
                            temporary.Path, run, schedule, start);
                        driving = established.Journal;
                        codes.Add(established.Code);
                        break;
                    case "open":
                        var opened = DurableProviderTrustCadenceJournal.Open(temporary.Path, run);
                        competitor = opened.Journal;
                        codes.Add(opened.Code);
                        break;
                    case "unsupervised-advance-against-a-competitor":
                        codes.AddRange(await AdvanceAgainstCompetitorAsync(
                            temporary.Path, run, driving!, supervision: null));
                        break;
                    case var _ when step.StartsWith("acquire:", StringComparison.Ordinal):
                        var acquired = ProviderTrustCadenceRunSupervision.Acquire(temporary.Path, run);
                        // A refused acquisition returns nothing, so an earlier live supervision under
                        // the same name is kept rather than overwritten with null.
                        if (acquired.Supervision is not null) supervisions[name] = acquired.Supervision;
                        codes.Add(acquired.Code);
                        break;
                    case var _ when step.StartsWith("release:", StringComparison.Ordinal):
                        var releasing = supervisions[name]!;
                        releasing.Dispose();
                        codes.Add(releasing.IsLive
                            ? "cadence-supervision-live"
                            : "cadence-supervision-released");
                        break;
                    case var _ when step.StartsWith("advance:", StringComparison.Ordinal):
                        var advanced = await SupervisedProviderTrustCadenceRecovery.AdvanceAsync(
                            supervisions[name]!, driving!, new CountingCycle(), new ImmediateDelay(),
                            CancellationToken.None);
                        codes.Add(advanced.Code);
                        break;
                    case var _ when step.StartsWith(
                        "supervised-advance-against-a-competitor:", StringComparison.Ordinal):
                        codes.AddRange(await AdvanceAgainstCompetitorAsync(
                            temporary.Path, run, driving!, supervisions[name]!));
                        break;
                    case var _ when step.StartsWith("competitor:", StringComparison.Ordinal):
                        codes.Add(name switch
                        {
                            "begin" => competitor!.BeginCycle().Code,
                            "reconcile" => competitor!
                                .ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Retry).Code,
                            _ => throw new ArgumentOutOfRangeException(nameof(vector), name, null),
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(vector), step, null);
                }
            }

            // Reopening is the only way to read what the record actually retains: a holder's own
            // snapshot is its view, and the vector pins the durable one.
            var reopened = DurableProviderTrustCadenceJournal.Open(temporary.Path, run).Journal!;
            return new(
                string.Join(",", codes), reopened.OwnerEpoch,
                reopened.Snapshot.Cycles.Count, reopened.Snapshot.Phase);
        }
        finally
        {
            foreach (var supervision in supervisions.Values) supervision?.Dispose();
        }
    }

    /// <summary>
    /// Advances the driving holder while a competitor tries to take the run from inside the cycle,
    /// which is the only window in which it can reconcile an attempt that is still running. Without a
    /// supervision the competitor opens the journal directly; with one it must acquire first, and the
    /// codes say which happened.
    /// </summary>
    private static async Task<IReadOnlyList<string>> AdvanceAgainstCompetitorAsync(
        string path,
        ProviderTrustCadenceRunId run,
        DurableProviderTrustCadenceJournal driving,
        ProviderTrustCadenceRunSupervision? supervision)
    {
        var codes = new List<string>();
        var cycle = new CountingCycle(during: () =>
        {
            if (supervision is null)
            {
                var taking = DurableProviderTrustCadenceJournal.Open(path, run).Journal!;
                codes.Add(taking.ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Retry).Code);
                return;
            }
            var contended = ProviderTrustCadenceRunSupervision.Acquire(path, run);
            codes.Add(contended.Code);
            contended.Supervision?.Dispose();
        });

        var advanced = supervision is null
            ? await ProviderTrustCadenceRecovery.AdvanceAsync(
                driving, cycle, new ImmediateDelay(), CancellationToken.None)
            : await SupervisedProviderTrustCadenceRecovery.AdvanceAsync(
                supervision, driving, cycle, new ImmediateDelay(), CancellationToken.None);
        codes.Add(advanced.Code);
        return codes;
    }

    private static Observation Expected(JsonElement vector) => new(
        string.Join(",", vector.GetProperty("codes").EnumerateArray().Select(v => v.GetString())),
        vector.GetProperty("finalEpoch").GetInt64(),
        vector.GetProperty("committedCycles").GetInt32(),
        vector.GetProperty("phase").GetString()!);

    [Test]
    public async Task Shared_cbi69_vectors_supervise_one_cadence_run()
    {
        using var fixture = Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            Assert.That(await RunAsync(fixture.RootElement, vector), Is.EqualTo(Expected(vector)),
                $"vector {vector.GetProperty("name").GetString()}");
        }
    }

    /// <summary>
    /// The exclusion is over the run, not over the caller: a second supervisor is refused whether it
    /// runs in this process or another one. The child process proves the operating system is doing
    /// the excluding rather than a field this process happens to hold.
    /// </summary>
    [Test]
    public void Cbi69_C1_one_live_supervisor_excludes_a_second_in_this_process()
    {
        using var temporary = new TemporaryJournal();
        var journal = DurableProviderTrustCadenceJournal.Establish(
            temporary.Path, RunId, Schedule, Start).Journal!;
        var before = File.ReadAllBytes(temporary.Path);
        var first = ProviderTrustCadenceRunSupervision.Acquire(temporary.Path, RunId);
        var second = ProviderTrustCadenceRunSupervision.Acquire(temporary.Path, RunId);
        first.Supervision!.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(first.Code, Is.EqualTo("cadence-supervision-acquired"));
            Assert.That(second.Code, Is.EqualTo("cadence-supervision-busy"));
            Assert.That(second.Supervision, Is.Null);
            Assert.That(File.ReadAllBytes(temporary.Path), Is.EqualTo(before));
            Assert.That(journal.OwnerEpoch, Is.EqualTo(1));
        });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi69_C1_one_live_supervisor_excludes_another_process()
    {
        var provider = ProviderPath();
        using var temporary = new TemporaryJournal();
        DurableProviderTrustCadenceJournal.Establish(temporary.Path, RunId, Schedule, Start);
        var lockPath = ProviderTrustCadenceRunSupervision.LockPathFor(temporary.Path);
        var held = ProviderTrustCadenceRunSupervision.Acquire(temporary.Path, RunId);
        try
        {
            Assert.That(await ProbeAsync(provider, lockPath), Is.EqualTo(74));
        }
        finally { held.Supervision!.Dispose(); }
        Assert.That(await ProbeAsync(provider, lockPath), Is.Zero);
    }

    /// <summary>
    /// A supervisor holds a run it has not read. CBI68's C2 makes opening observe rather than claim,
    /// and exclusion must not undo that: the record's epoch is still moved only by a write.
    /// </summary>
    [Test]
    public void Cbi69_C2_supervision_excludes_writers_without_claiming_the_run()
    {
        using var temporary = new TemporaryJournal();
        // Acquired before the run exists at all, which is the ordering a host must use if the lock is
        // to cover establishment.
        var supervision = ProviderTrustCadenceRunSupervision.Acquire(temporary.Path, RunId).Supervision!;
        var established = DurableProviderTrustCadenceJournal.Establish(
            temporary.Path, RunId, Schedule, Start);
        var before = File.ReadAllBytes(temporary.Path);
        supervision.Dispose();
        var again = ProviderTrustCadenceRunSupervision.Acquire(temporary.Path, RunId);
        again.Supervision!.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(established.Code, Is.EqualTo("durable-cadence-established"));
            Assert.That(established.Journal!.OwnerEpoch, Is.EqualTo(1));
            Assert.That(File.ReadAllBytes(temporary.Path), Is.EqualTo(before));
            Assert.That(again.Code, Is.EqualTo("cadence-supervision-acquired"));
        });
    }

    /// <summary>
    /// CBI54 pairs its lock with a durable epoch because CBI53 has none. This journal already
    /// publishes one, so the slice adds a lock and no state: the lock file is never read and never
    /// carries anything, and the only record beside it is the journal itself.
    /// </summary>
    [Test]
    public void Cbi69_C3_supervision_publishes_no_state_of_its_own()
    {
        using var temporary = new TemporaryJournal();
        DurableProviderTrustCadenceJournal.Establish(temporary.Path, RunId, Schedule, Start);
        var lockPath = ProviderTrustCadenceRunSupervision.LockPathFor(temporary.Path);
        // Bytes a durable record would have to read, planted where one would live. Nothing reads them,
        // so a supervisor finds them irrelevant and leaves them exactly as they were — which is what
        // makes the file a lock rather than state.
        File.WriteAllText(lockPath, "not state");
        var supervision = ProviderTrustCadenceRunSupervision.Acquire(temporary.Path, RunId);
        supervision.Supervision!.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(supervision.Code, Is.EqualTo("cadence-supervision-acquired"));
            Assert.That(File.ReadAllText(lockPath), Is.EqualTo("not state"));
            Assert.That(
                Directory.GetFiles(System.IO.Path.GetDirectoryName(temporary.Path)!)
                    .Select(System.IO.Path.GetFileName).Order(StringComparer.Ordinal),
                Is.EqualTo(new[] { "cadence.bin", "cadence.bin.lock" }));
        });
    }

    /// <summary>
    /// Releasing is idempotent and a released supervisor drives nothing. The cadence does not advance
    /// on its own afterwards, which is what distinguishes "the lock is gone" from "the run is over".
    /// </summary>
    [Test]
    public async Task Cbi69_C4_a_released_supervisor_cannot_drive_the_cadence()
    {
        using var temporary = new TemporaryJournal();
        var journal = DurableProviderTrustCadenceJournal.Establish(
            temporary.Path, RunId, Schedule, Start).Journal!;
        var supervision = ProviderTrustCadenceRunSupervision.Acquire(temporary.Path, RunId).Supervision!;
        supervision.Dispose();
        supervision.Dispose();
        var cycle = new CountingCycle();
        var advanced = await SupervisedProviderTrustCadenceRecovery.AdvanceAsync(
            supervision, journal, cycle, new ImmediateDelay(), CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(supervision.IsLive, Is.False);
            Assert.That(supervision.IsCurrentFor(journal), Is.False);
            Assert.That(advanced.Code, Is.EqualTo("cadence-supervision-required"));
            Assert.That(cycle.Calls, Is.Zero, "the cycle must not run");
            Assert.That(journal.OwnerEpoch, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Acquiring is not a recovery. A run interrupted in flight stays interrupted for the next
    /// supervisor to reconcile through CBI48, because nothing about the lock says what the previous
    /// holder had done.
    /// </summary>
    [Test]
    public void Cbi69_C4_acquiring_resolves_no_interruption()
    {
        using var temporary = new TemporaryJournal();
        var journal = DurableProviderTrustCadenceJournal.Establish(
            temporary.Path, RunId, Schedule, Start).Journal!;
        var first = ProviderTrustCadenceRunSupervision.Acquire(temporary.Path, RunId).Supervision!;
        journal.BeginCycle();
        first.Dispose();
        var second = ProviderTrustCadenceRunSupervision.Acquire(temporary.Path, RunId);
        var reopened = DurableProviderTrustCadenceJournal.Open(temporary.Path, RunId);
        second.Supervision!.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(second.Code, Is.EqualTo("cadence-supervision-acquired"));
            Assert.That(reopened.Code, Is.EqualTo("durable-cadence-indeterminate"));
            Assert.That(reopened.Journal!.Snapshot.Phase, Is.EqualTo("in-flight"));
        });
    }

    /// <summary>
    /// The two guards cover different holders. What the lock cannot exclude — a holder that opened
    /// before supervision existed — the fence still refuses at its next write; what the fence cannot
    /// catch in time is the competitor that reconciles a run while its cycle is still executing, and
    /// the same scenario is run both ways here. The unsupervised half is the cost CBI68 named,
    /// executed rather than described.
    /// </summary>
    [Test]
    public async Task Cbi69_C5_the_lock_and_the_fence_cover_different_holders()
    {
        using var temporary = new TemporaryJournal();
        var driving = DurableProviderTrustCadenceJournal.Establish(
            temporary.Path, RunId, Schedule, Start).Journal!;
        var unsupervised = new CountingCycle(during: () =>
            DurableProviderTrustCadenceJournal.Open(temporary.Path, RunId).Journal!
                .ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Retry));
        var lost = await ProviderTrustCadenceRecovery.AdvanceAsync(
            driving, unsupervised, new ImmediateDelay(), CancellationToken.None);
        var afterLoss = DurableProviderTrustCadenceJournal.Open(temporary.Path, RunId).Journal!;

        using var second = new TemporaryJournal();
        var supervised = ProviderTrustCadenceRunSupervision.Acquire(second.Path, RunId).Supervision!;
        var kept = DurableProviderTrustCadenceJournal.Establish(
            second.Path, RunId, Schedule, Start).Journal!;
        var contended = "none";
        var excluded = new CountingCycle(during: () =>
        {
            var attempt = ProviderTrustCadenceRunSupervision.Acquire(second.Path, RunId);
            contended = attempt.Code;
            attempt.Supervision?.Dispose();
        });
        var held = await SupervisedProviderTrustCadenceRecovery.AdvanceAsync(
            supervised, kept, excluded, new ImmediateDelay(), CancellationToken.None);
        supervised.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(unsupervised.Calls, Is.EqualTo(1), "the cycle ran");
            Assert.That(lost.Code, Is.EqualTo("durable-cadence-owner-superseded"),
                "and the run was lost only afterwards");
            Assert.That(afterLoss.Snapshot.Cycles, Is.Empty, "so the record kept nothing of it");
            Assert.That(contended, Is.EqualTo("cadence-supervision-busy"),
                "the same competitor never reaches the record under a lock");
            Assert.That(held.Code, Is.EqualTo("durable-cadence-cycle-committed"));
        });
    }

    /// <summary>
    /// The fence is unchanged by supervision: a holder that was superseded is refused with the code
    /// CBI68 already produces, whether or not a lock is held over the run.
    /// </summary>
    [Test]
    public async Task Cbi69_C5_supervision_adds_no_code_to_the_write_path()
    {
        using var temporary = new TemporaryJournal();
        var driving = DurableProviderTrustCadenceJournal.Establish(
            temporary.Path, RunId, Schedule, Start).Journal!;
        var supervision = ProviderTrustCadenceRunSupervision.Acquire(temporary.Path, RunId).Supervision!;
        // An unsupervised holder is not excluded by a lock it never asked for, which is exactly the
        // case the fence exists for.
        DurableProviderTrustCadenceJournal.Open(temporary.Path, RunId).Journal!.BeginCycle();
        var cycle = new CountingCycle();
        var advanced = await SupervisedProviderTrustCadenceRecovery.AdvanceAsync(
            supervision, driving, cycle, new ImmediateDelay(), CancellationToken.None);
        supervision.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(advanced.Code, Is.EqualTo("durable-cadence-owner-superseded"));
            Assert.That(cycle.Calls, Is.Zero);
        });
    }

    /// <summary>
    /// A supervision is bound to the run and the path it locks. Pairing it with a journal it does not
    /// cover would advance a cadence behind a lock that excludes nobody from it, so it refuses.
    /// </summary>
    [Test]
    public async Task Cbi69_C6_supervision_is_bound_to_the_run_and_path_it_names()
    {
        using var supervised = new TemporaryJournal();
        using var other = new TemporaryJournal();
        var supervision = ProviderTrustCadenceRunSupervision.Acquire(supervised.Path, RunId).Supervision!;
        var elsewhere = DurableProviderTrustCadenceJournal.Establish(
            other.Path, RunId, Schedule, Start).Journal!;
        var otherRun = ProviderTrustCadenceRunId.Create("cadence-run.test.2");
        var otherIdentity = DurableProviderTrustCadenceJournal.Establish(
            supervised.Path, otherRun, Schedule, Start).Journal!;
        var cycle = new CountingCycle();
        var wrongPath = await SupervisedProviderTrustCadenceRecovery.AdvanceAsync(
            supervision, elsewhere, cycle, new ImmediateDelay(), CancellationToken.None);
        var wrongRun = await SupervisedProviderTrustCadenceRecovery.AdvanceAsync(
            supervision, otherIdentity, cycle, new ImmediateDelay(), CancellationToken.None);
        supervision.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(supervision.RunIdentity, Is.EqualTo(RunId));
            Assert.That(wrongPath.Code, Is.EqualTo("cadence-supervision-required"));
            Assert.That(wrongRun.Code, Is.EqualTo("cadence-supervision-required"));
            Assert.That(cycle.Calls, Is.Zero);
        });
    }

    /// <summary>
    /// CBI68's residual limits say two holders that interleave writes "fence each other alternately
    /// rather than one winning permanently". They do not. A refused transition does not advance the
    /// refused holder's epoch, so the loser stays behind for good while the winner keeps writing; only
    /// a host that reopens rejoins, and reopening is a decision it has to make. That makes the
    /// unsupervised outcome a silent, permanent transfer rather than contention a host would notice.
    /// </summary>
    [Test]
    public void A_fenced_holder_stays_fenced_rather_than_alternating()
    {
        using var temporary = new TemporaryJournal();
        var winner = DurableProviderTrustCadenceJournal.Establish(
            temporary.Path, RunId, Schedule, Start).Journal!;
        var loser = DurableProviderTrustCadenceJournal.Open(temporary.Path, RunId).Journal!;
        var began = winner.BeginCycle();
        var refused = loser.CommitCycle(ProviderServingTrustCycleCodes.Current);
        var committed = winner.CommitCycle(ProviderServingTrustCycleCodes.Current);
        var refusedAgain = loser.BeginCycle();
        Assert.Multiple(() =>
        {
            Assert.That(began.Code, Is.EqualTo("durable-cadence-cycle-started"));
            Assert.That(refused.Code, Is.EqualTo("durable-cadence-owner-superseded"));
            Assert.That(committed.Code, Is.EqualTo("durable-cadence-cycle-committed"));
            Assert.That(refusedAgain.Code, Is.EqualTo("durable-cadence-owner-superseded"));
        });
    }

    private static async Task<int> ProbeAsync(string provider, string path)
    {
        var start = new ProcessStartInfo(provider)
            { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true };
        start.ArgumentList.Add($"--probe-exclusive-file={path}");
        using var process = Process.Start(start)!;
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static string ProviderPath()
    {
        var path = Environment.GetEnvironmentVariable("BRONTIDE_REFERENCE_PROVIDER");
        if (path is null || !File.Exists(path))
            Assert.Ignore("BRONTIDE_REFERENCE_PROVIDER does not name a built provider endpoint.");
        return System.IO.Path.GetFullPath(path!);
    }
}
