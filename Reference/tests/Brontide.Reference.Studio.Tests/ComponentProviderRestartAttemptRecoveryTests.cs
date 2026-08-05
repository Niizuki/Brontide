using System.Text.Json;
using Brontide.Reference.Experimental.ComponentManagement;

namespace Brontide.Reference.Studio.Tests;

public sealed class ComponentProviderRestartAttemptRecoveryTests
{
    private static readonly ProviderRestartAttemptRunId RunId = ProviderRestartAttemptRunId.Create("restart-run.test.1");
    private static readonly OccurrenceId Occurrence = OccurrenceId.Create("occ.def.test.cooling-provider.1");
    private static readonly ProviderArtifactSetId Staged = ProviderArtifactSetId.Create(new string('A', 64));
    private static readonly ProviderRestartPolicy Policy = ProviderRestartPolicy.Create(2, TimeSpan.FromMinutes(1));
    private static readonly DateTimeOffset Start = new(2026, 8, 5, 9, 0, 0, TimeSpan.Zero);

    private sealed class TemporaryJournal : IDisposable
    {
        public TemporaryJournal() => Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"brontide-cbi53-{Guid.NewGuid():N}", "restart.journal");
        public string Path { get; }
        public void Dispose()
        {
            var root = System.IO.Path.GetDirectoryName(Path)!;
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static DurableProviderRestartAttemptJournal Establish(string path) =>
        DurableProviderRestartAttemptJournal.Establish(path, RunId, Occurrence, Staged, Policy).Journal!;

    private static ProviderRestartAttemptJournalTransitionResult Commit(
        DurableProviderRestartAttemptJournal journal, DateTimeOffset instant, string code)
    {
        Assert.That(journal.BeginAttempt(instant).Code, Is.EqualTo("durable-restart-attempt-started"));
        var completed = code == "provider-restart-completed";
        return journal.CommitAttempt(
            code,
            completed ? "none" : code == "portable-process-interrupted" ? "cbi2" : "cbi31",
            providerStarted: code == "portable-process-interrupted" || completed,
            lifecycleReconstructed: completed,
            completed);
    }

    [Test]
    public void Cbi53_C1_one_journal_names_one_bounded_restart_lineage()
    {
        using var temporary = new TemporaryJournal();
        var established = DurableProviderRestartAttemptJournal.Establish(temporary.Path, RunId, Occurrence, Staged, Policy);
        var duplicate = DurableProviderRestartAttemptJournal.Establish(temporary.Path, RunId, Occurrence, Staged, Policy);
        var mismatch = DurableProviderRestartAttemptJournal.Open(
            temporary.Path, RunId, OccurrenceId.Create("occ.def.test.other.1"), Staged);
        Assert.Multiple(() =>
        {
            Assert.That(established.Code, Is.EqualTo("durable-restart-established"));
            Assert.That(duplicate.Code, Is.EqualTo("durable-restart-already-exists"));
            Assert.That(mismatch.Code, Is.EqualTo("durable-restart-lineage-mismatch"));
            Assert.That(established.Journal!.Snapshot.MaximumAttempts, Is.EqualTo(2));
        });
    }

    [Test]
    public void Cbi53_C2_every_transition_is_atomic_and_integrity_checked()
    {
        using var temporary = new TemporaryJournal();
        var journal = Establish(temporary.Path);
        var original = File.ReadAllBytes(temporary.Path);
        Directory.CreateDirectory(temporary.Path + ".tmp");
        var refused = journal.BeginAttempt(Start);
        Directory.Delete(temporary.Path + ".tmp");
        Assert.Multiple(() =>
        {
            Assert.That(refused.Code, Is.EqualTo("durable-restart-write-failed"));
            Assert.That(File.ReadAllBytes(temporary.Path), Is.EqualTo(original));
        });
        var bytes = File.ReadAllBytes(temporary.Path);
        bytes[0] ^= 0x7F;
        File.WriteAllBytes(temporary.Path, bytes);
        Assert.That(DurableProviderRestartAttemptJournal.Open(temporary.Path, RunId, Occurrence, Staged).Code,
            Is.EqualTo("durable-restart-corrupt"));
    }

    [Test]
    public void Cbi53_C3_non_ready_policy_history_changes_no_journal_state()
    {
        using var temporary = new TemporaryJournal();
        var journal = Establish(temporary.Path);
        Commit(journal, Start, "staged-artifact-integrity-failed");
        var before = File.ReadAllBytes(temporary.Path);
        var waiting = journal.BeginAttempt(Start.AddSeconds(59));
        Assert.Multiple(() =>
        {
            Assert.That(waiting.Code, Is.EqualTo("durable-restart-waiting"));
            Assert.That(File.ReadAllBytes(temporary.Path), Is.EqualTo(before));
        });
    }

    [Test]
    public void Cbi53_C4_in_flight_state_precedes_restart_effects()
    {
        using var temporary = new TemporaryJournal();
        var journal = Establish(temporary.Path);
        Assert.That(journal.BeginAttempt(Start).Code, Is.EqualTo("durable-restart-attempt-started"));
        var reopened = DurableProviderRestartAttemptJournal.Open(temporary.Path, RunId, Occurrence, Staged);
        Assert.Multiple(() =>
        {
            Assert.That(reopened.Code, Is.EqualTo("durable-restart-indeterminate"));
            Assert.That(reopened.Journal!.Snapshot.InFlightIndex, Is.Zero);
        });
    }

    [Test]
    public void Cbi53_C5_committed_failures_drive_delay_and_exhaustion()
    {
        using var temporary = new TemporaryJournal();
        var journal = Establish(temporary.Path);
        Commit(journal, Start, "portable-process-interrupted");
        var exhausted = Commit(journal, Start.AddMinutes(1), "staged-artifact-integrity-failed");
        Assert.Multiple(() =>
        {
            Assert.That(exhausted.Code, Is.EqualTo("durable-restart-exhausted"));
            Assert.That(exhausted.Snapshot.Attempts, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void Cbi53_C6_interrupted_work_requires_explicit_reconciliation()
    {
        using var retryFile = new TemporaryJournal();
        var retry = Establish(retryFile.Path);
        retry.BeginAttempt(Start);
        var ready = retry.ResolveInterrupted(ProviderRestartAttemptRecoveryDecision.Retry);
        using var abandonFile = new TemporaryJournal();
        var abandon = Establish(abandonFile.Path);
        abandon.BeginAttempt(Start);
        var abandoned = abandon.ResolveInterrupted(ProviderRestartAttemptRecoveryDecision.Abandon);
        Assert.Multiple(() =>
        {
            Assert.That(ready.Code, Is.EqualTo("durable-restart-retry-ready"));
            Assert.That(ready.Snapshot.RetryCount, Is.EqualTo(1));
            Assert.That(abandoned.Code, Is.EqualTo("durable-restart-abandoned"));
        });
    }

    [Test]
    public void Cbi53_C7_terminal_recovery_is_idempotent_and_effect_free()
    {
        using var temporary = new TemporaryJournal();
        var journal = Establish(temporary.Path);
        Commit(journal, Start, "provider-restart-completed");
        var before = File.ReadAllBytes(temporary.Path);
        var reopened = DurableProviderRestartAttemptJournal.Open(temporary.Path, RunId, Occurrence, Staged).Journal!;
        var after = reopened.BeginAttempt(Start.AddMinutes(1));
        Assert.Multiple(() =>
        {
            Assert.That(after.Code, Is.EqualTo("durable-restart-completed"));
            Assert.That(File.ReadAllBytes(temporary.Path), Is.EqualTo(before));
        });
    }

    [Test]
    public void Cbi53_C8_reference_executes_the_shared_history_model()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(System.IO.Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi53-durable-restart-attempt-vectors.json")));
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            using var temporary = new TemporaryJournal();
            var journal = Establish(temporary.Path);
            var now = Start;
            var code = "durable-restart-established";
            foreach (var actionElement in vector.GetProperty("actions").EnumerateArray())
            {
                var action = actionElement.GetString()!;
                if (action.StartsWith("attempt:", StringComparison.Ordinal))
                    code = Commit(journal, now, action[8..]).Code;
                else if (action.StartsWith("advance:", StringComparison.Ordinal))
                    now = now.AddSeconds(int.Parse(action[8..], System.Globalization.CultureInfo.InvariantCulture));
                else if (action == "crash") code = journal.BeginAttempt(now).Code;
                else if (action == "reopen")
                {
                    var opened = DurableProviderRestartAttemptJournal.Open(temporary.Path, RunId, Occurrence, Staged);
                    code = opened.Code;
                    journal = opened.Journal!;
                }
                else if (action == "retry") code = journal.ResolveInterrupted(ProviderRestartAttemptRecoveryDecision.Retry).Code;
                else if (action == "abandon") code = journal.ResolveInterrupted(ProviderRestartAttemptRecoveryDecision.Abandon).Code;
            }
            var expectedInFlight = vector.GetProperty("expectedInFlight");
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()), vector.GetProperty("name").GetString());
                Assert.That(journal.Snapshot.Phase, Is.EqualTo(vector.GetProperty("expectedPhase").GetString()));
                Assert.That(journal.Snapshot.Attempts.Select(item => item.Code), Is.EqualTo(vector.GetProperty("expectedAttemptCodes").EnumerateArray().Select(item => item.GetString())));
                Assert.That(journal.Snapshot.NextAttemptIndex, Is.EqualTo(vector.GetProperty("expectedNextAttempt").GetInt32()));
                Assert.That(journal.Snapshot.InFlightIndex, Is.EqualTo(expectedInFlight.ValueKind == JsonValueKind.Null ? null : expectedInFlight.GetInt32()));
                Assert.That(journal.Snapshot.InterruptionCount, Is.EqualTo(vector.GetProperty("expectedInterruptions").GetInt32()));
                Assert.That(journal.Snapshot.RetryCount, Is.EqualTo(vector.GetProperty("expectedRetries").GetInt32()));
            });
        }
    }
}
