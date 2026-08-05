using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed class ComponentProviderTrustCadenceRecoveryTests
{
    private sealed class FakeCycle(Func<DateTimeOffset, ProviderServingTrustCycleResult> run)
        : IProviderServingTrustCycle
    {
        public int Calls { get; private set; }

        public Task<ProviderServingTrustCycleResult> RunAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(run(now));
        }
    }

    private sealed class FakeDelay : IProviderServingTrustCadenceDelay
    {
        public int Calls { get; private set; }

        public Task<DateTimeOffset> DelayAsync(
            DateTimeOffset now,
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(now + duration);
        }
    }

    private sealed class TemporaryJournal : IDisposable
    {
        private readonly string root = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"brontide-cbi48-{Guid.NewGuid():N}");
        public string Path => System.IO.Path.Combine(root, "cadence.bin");
        public void Dispose()
        {
            try { if (Directory.Exists(root)) Directory.Delete(root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static readonly ProviderTrustCadenceRunId RunId =
        ProviderTrustCadenceRunId.Create("cadence-run.test.1");

    private static readonly ProviderServingTrustCadenceSchedule Schedule =
        ProviderServingTrustCadenceSchedule.Create(2, TimeSpan.FromSeconds(5));

    private static readonly DateTimeOffset Start =
        new(2026, 8, 5, 8, 0, 0, TimeSpan.Zero);

    private static ProviderServingTrustCycleResult CycleResult(string code) =>
        new(code, null!, null, 0);

    private static DurableProviderTrustCadenceJournal Establish(string path) =>
        DurableProviderTrustCadenceJournal.Establish(path, RunId, Schedule, Start).Journal!;

    [Test]
    public void Cbi48_C1_a_durable_run_is_bounded_and_distinctly_identified()
    {
        using var temporary = new TemporaryJournal();
        var established = DurableProviderTrustCadenceJournal.Establish(
            temporary.Path, RunId, Schedule, Start);
        var duplicate = DurableProviderTrustCadenceJournal.Establish(
            temporary.Path, RunId, Schedule, Start);
        var mismatch = DurableProviderTrustCadenceJournal.Open(
            temporary.Path, ProviderTrustCadenceRunId.Create("cadence-run.other"));

        Assert.Multiple(() =>
        {
            Assert.That(established.Code, Is.EqualTo("durable-cadence-established"));
            Assert.That(established.Journal!.Snapshot.RunIdentity, Is.EqualTo(RunId));
            Assert.That(duplicate.Code, Is.EqualTo("durable-cadence-already-exists"));
            Assert.That(mismatch.Code, Is.EqualTo("durable-cadence-run-mismatch"));
            Assert.Throws<ArgumentException>(() => DurableProviderTrustCadenceJournal.Open(
                temporary.Path, default));
        });
    }

    [Test]
    public void Cbi48_C2_every_transition_is_atomic_and_integrity_checked()
    {
        using var temporary = new TemporaryJournal();
        var journal = Establish(temporary.Path);
        Assert.That(journal.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));
        var bytes = File.ReadAllBytes(temporary.Path);
        bytes[^1] ^= 0xff;
        File.WriteAllBytes(temporary.Path, bytes);

        Assert.That(DurableProviderTrustCadenceJournal.Open(temporary.Path, RunId).Code,
            Is.EqualTo("durable-cadence-corrupt"));
    }

    [Test]
    public async Task Cbi48_C3_in_flight_state_precedes_the_effectful_cycle()
    {
        using var temporary = new TemporaryJournal();
        var journal = Establish(temporary.Path);
        var observed = "";
        var cycle = new FakeCycle(_ =>
        {
            observed = DurableProviderTrustCadenceJournal.Open(temporary.Path, RunId).Code;
            return CycleResult("provider-trust-cycle-stopped");
        });

        await ProviderTrustCadenceRecovery.AdvanceAsync(
            journal, cycle, new FakeDelay(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(observed, Is.EqualTo("durable-cadence-indeterminate"));
            Assert.That(cycle.Calls, Is.EqualTo(1));
            Assert.That(journal.Snapshot.Code, Is.EqualTo("durable-cadence-stopped"));
        });

        using var failedWriteTemporary = new TemporaryJournal();
        var failedWrite = Establish(failedWriteTemporary.Path);
        File.Delete(failedWriteTemporary.Path);
        Directory.CreateDirectory(failedWriteTemporary.Path);
        var forbiddenCycle = new FakeCycle(_ => CycleResult("provider-trust-cycle-current"));
        var refused = await ProviderTrustCadenceRecovery.AdvanceAsync(
            failedWrite, forbiddenCycle, new FakeDelay(), CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(refused.Code, Is.EqualTo("durable-cadence-write-failed"));
            Assert.That(forbiddenCycle.Calls, Is.Zero);
        });
    }

    [Test]
    public async Task Cbi48_C4_completed_work_resumes_from_the_next_clean_boundary()
    {
        using var temporary = new TemporaryJournal();
        var first = Establish(temporary.Path);
        var firstCycle = new FakeCycle(_ => CycleResult("provider-trust-cycle-current"));
        await ProviderTrustCadenceRecovery.AdvanceAsync(
            first, firstCycle, new FakeDelay(), CancellationToken.None);

        var waitingBytes = File.ReadAllBytes(temporary.Path);
        using (var canceled = new CancellationTokenSource())
        {
            canceled.Cancel();
            var canceledCycle = new FakeCycle(_ => CycleResult("provider-trust-cycle-current"));
            var canceledDelay = new FakeDelay();
            var canceledResult = await ProviderTrustCadenceRecovery.AdvanceAsync(
                first, canceledCycle, canceledDelay, canceled.Token);
            Assert.Multiple(() =>
            {
                Assert.That(canceledResult.Code, Is.EqualTo("durable-cadence-wait-canceled"));
                Assert.That(canceledCycle.Calls, Is.Zero);
                Assert.That(canceledDelay.Calls, Is.Zero);
                Assert.That(File.ReadAllBytes(temporary.Path), Is.EqualTo(waitingBytes));
            });
        }

        var recovered = DurableProviderTrustCadenceJournal.Open(temporary.Path, RunId).Journal!;
        var secondCycle = new FakeCycle(_ => CycleResult("provider-trust-cycle-current"));
        var delay = new FakeDelay();
        await ProviderTrustCadenceRecovery.AdvanceAsync(
            recovered, secondCycle, delay, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(firstCycle.Calls, Is.EqualTo(1));
            Assert.That(secondCycle.Calls, Is.EqualTo(1));
            Assert.That(delay.Calls, Is.EqualTo(1));
            Assert.That(recovered.Snapshot.Code, Is.EqualTo("durable-cadence-complete"));
            Assert.That(recovered.Snapshot.Cycles.Select(item => item.Index), Is.EqualTo(new[] { 0, 1 }));
        });
    }

    [Test]
    public async Task Cbi48_C5_an_interrupted_effect_is_indeterminate_and_inert()
    {
        using var temporary = new TemporaryJournal();
        var journal = Establish(temporary.Path);
        journal.BeginCycle();
        var before = File.ReadAllBytes(temporary.Path);
        var recovered = DurableProviderTrustCadenceJournal.Open(temporary.Path, RunId);
        var cycle = new FakeCycle(_ => CycleResult("provider-trust-cycle-current"));
        var delay = new FakeDelay();

        var result = await ProviderTrustCadenceRecovery.AdvanceAsync(
            recovered.Journal!, cycle, delay, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(recovered.Code, Is.EqualTo("durable-cadence-indeterminate"));
            Assert.That(result.Code, Is.EqualTo("durable-cadence-indeterminate"));
            Assert.That(cycle.Calls, Is.Zero);
            Assert.That(delay.Calls, Is.Zero);
            Assert.That(File.ReadAllBytes(temporary.Path), Is.EqualTo(before));
        });
    }

    [Test]
    public void Cbi48_C6_retry_or_abandonment_requires_explicit_reconciliation()
    {
        using var retryTemporary = new TemporaryJournal();
        var retry = Establish(retryTemporary.Path);
        var attempted = retry.BeginCycle().Snapshot;
        var ready = retry.ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Retry);

        using var abandonTemporary = new TemporaryJournal();
        var abandon = Establish(abandonTemporary.Path);
        abandon.BeginCycle();
        var abandoned = abandon.ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Abandon);

        Assert.Multiple(() =>
        {
            Assert.That(ready.Code, Is.EqualTo("durable-cadence-retry-ready"));
            Assert.That(ready.Snapshot.NextCycleIndex, Is.EqualTo(attempted.NextCycleIndex));
            Assert.That(ready.Snapshot.PreparedInstant, Is.EqualTo(attempted.PreparedInstant));
            Assert.That(ready.Snapshot.InterruptionCount, Is.EqualTo(1));
            Assert.That(ready.Snapshot.RetryCount, Is.EqualTo(1));
            Assert.That(abandoned.Code, Is.EqualTo("durable-cadence-abandoned"));
            Assert.That(abandoned.Snapshot.InterruptionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Cbi48_C7_terminal_recovery_is_idempotent_and_effect_free()
    {
        using var temporary = new TemporaryJournal();
        var journal = Establish(temporary.Path);
        journal.BeginCycle();
        journal.CommitCycle("provider-trust-cycle-stopped");
        var before = File.ReadAllBytes(temporary.Path);
        var recovered = DurableProviderTrustCadenceJournal.Open(temporary.Path, RunId).Journal!;
        var cycle = new FakeCycle(_ => CycleResult("provider-trust-cycle-current"));
        var delay = new FakeDelay();

        var advanced = await ProviderTrustCadenceRecovery.AdvanceAsync(
            recovered, cycle, delay, CancellationToken.None);
        var reconciled = recovered.ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Retry);

        Assert.Multiple(() =>
        {
            Assert.That(advanced.Code, Is.EqualTo("durable-cadence-stopped"));
            Assert.That(reconciled.Code, Is.EqualTo("durable-cadence-stopped"));
            Assert.That(cycle.Calls, Is.Zero);
            Assert.That(delay.Calls, Is.Zero);
            Assert.That(File.ReadAllBytes(temporary.Path), Is.EqualTo(before));
        });
    }

    [Test]
    public void Cbi48_C8_reference_executes_the_shared_recovery_vectors()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi48-durable-cadence-vectors.json")));
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            using var temporary = new TemporaryJournal();
            var journal = Establish(temporary.Path);
            foreach (var actionElement in vector.GetProperty("actions").EnumerateArray())
            {
                var action = actionElement.GetString()!;
                if (action.StartsWith("cycle:", StringComparison.Ordinal))
                {
                    journal.BeginCycle();
                    journal.CommitCycle(action[6..]);
                }
                else if (action == "gap")
                {
                    journal.CompleteGap(journal.Snapshot.PreparedInstant + Schedule.Interval);
                }
                else if (action == "crash") journal.BeginCycle();
                else if (action == "reopen")
                    journal = DurableProviderTrustCadenceJournal.Open(temporary.Path, RunId).Journal!;
                else if (action == "retry")
                    journal.ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Retry);
                else if (action == "abandon")
                    journal.ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Abandon);
                else Assert.Fail($"Unknown action {action}.");
            }

            var name = vector.GetProperty("name").GetString()!;
            var snapshot = journal.Snapshot;
            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()), name);
                Assert.That(snapshot.Phase, Is.EqualTo(vector.GetProperty("expectedPhase").GetString()), name);
                Assert.That(snapshot.Cycles.Select(item => item.Code),
                    Is.EqualTo(vector.GetProperty("expectedCycleCodes").EnumerateArray()
                        .Select(item => item.GetString()).ToArray()), name);
                Assert.That(snapshot.Cycles.Select(item => item.Instant),
                    Is.EqualTo(vector.GetProperty("expectedCycleInstants").EnumerateArray()
                        .Select(item => item.GetDateTimeOffset()).ToArray()), name);
                Assert.That(snapshot.Gaps.Select(item => (int)item.TotalSeconds),
                    Is.EqualTo(vector.GetProperty("expectedGapsSeconds").EnumerateArray()
                        .Select(item => item.GetInt32()).ToArray()), name);
                Assert.That(snapshot.NextCycleIndex,
                    Is.EqualTo(vector.GetProperty("expectedNextCycle").GetInt32()), name);
                Assert.That(snapshot.InterruptionCount,
                    Is.EqualTo(vector.GetProperty("expectedInterruptions").GetInt32()), name);
                Assert.That(snapshot.RetryCount,
                    Is.EqualTo(vector.GetProperty("expectedRetries").GetInt32()), name);
            });
        }
    }
}
