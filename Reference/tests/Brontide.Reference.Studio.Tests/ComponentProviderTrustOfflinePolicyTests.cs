using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed class ComponentProviderTrustOfflinePolicyTests
{
    private sealed class TemporaryJournal : IDisposable
    {
        private readonly string root = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"brontide-cbi49-{Guid.NewGuid():N}");

        public string Path => System.IO.Path.Combine(root, "cadence.bin");

        public void Dispose()
        {
            try { if (Directory.Exists(root)) Directory.Delete(root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private sealed record Fixture(
        FixturePolicy Policy,
        IReadOnlyList<OfflineVector> OfflineVectors,
        IReadOnlyList<ReconciliationVector> ReconciliationVectors);

    private sealed record FixturePolicy(int GraceSeconds, int RetrySeconds, DateTimeOffset LastCurrent);

    private sealed record OfflineVector(
        string Id,
        DateTimeOffset Now,
        DateTimeOffset? LastCurrent,
        string PollCode,
        string? LastAttemptCode,
        int ServingCount,
        string ExpectedCode,
        bool ContinueExisting,
        bool MayStart,
        DateTimeOffset? Deadline,
        DateTimeOffset? RetryAt);

    private sealed record ReconciliationVector(
        string Id,
        string Verdict,
        int? AttemptIndex,
        string ExpectedCode,
        string ExpectedPhase,
        int Interruptions,
        int Retries);

    private static readonly ProviderTrustCadenceRunId RunId =
        ProviderTrustCadenceRunId.Create("cadence-run.cbi49");

    private static readonly DateTimeOffset Start =
        new(2026, 8, 5, 8, 0, 0, TimeSpan.Zero);

    private static readonly ProviderServingTrustCadenceSchedule Schedule =
        ProviderServingTrustCadenceSchedule.Create(2, TimeSpan.FromMinutes(1));

    private static Fixture LoadFixture()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory,
            "component-management", "fixtures", "cbi49-offline-reconciliation-vectors.json");
        return JsonSerializer.Deserialize<Fixture>(File.ReadAllText(path), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        })!;
    }

    private static DurableProviderTrustCadenceJournal Interrupted(string path)
    {
        var journal = DurableProviderTrustCadenceJournal.Establish(path, RunId, Schedule, Start).Journal!;
        Assert.That(journal.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));
        return DurableProviderTrustCadenceJournal.Open(path, RunId).Journal!;
    }

    [Test]
    public void Cbi49_C1_offline_policy_is_explicit_and_bounded()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ProviderTrustOfflinePolicy.Create(TimeSpan.Zero, TimeSpan.FromMinutes(1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ProviderTrustOfflinePolicy.Create(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ProviderTrustOfflinePolicy.Create(TimeSpan.FromHours(25), TimeSpan.FromMinutes(1)));
        });

        var policy = ProviderTrustOfflinePolicy.Create(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));
        var result = policy.Evaluate(Start.AddMinutes(2), Start,
            "policy-poll-exhausted", "policy-distribution-timeout", 1);
        var overflowing = policy.Evaluate(DateTimeOffset.MaxValue,
            DateTimeOffset.MaxValue.AddMinutes(-1),
            "policy-poll-exhausted", "policy-distribution-timeout", 1);
        Assert.Multiple(() =>
        {
            Assert.That(result.Deadline, Is.EqualTo(Start.AddMinutes(5)));
            Assert.That(result.RetryAt, Is.EqualTo(Start.AddMinutes(3)));
            Assert.That(overflowing.Code, Is.EqualTo("offline-observation-invalid"));
        });
    }

    [Test]
    public void Cbi49_C2_only_endpoint_unavailability_is_grace_eligible()
    {
        var policy = ProviderTrustOfflinePolicy.Create(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));
        foreach (var (poll, attempt) in new[]
        {
            ("policy-poll-refused", "policy-distribution-endpoint-signature-invalid"),
            ("policy-poll-exhausted", "policy-distribution-stale"),
            ("policy-poll-exhausted", "policy-distribution-superseded"),
            ("policy-poll-canceled", "policy-distribution-canceled"),
            ("policy-poll-floor-unretained", "policy-distribution-update-applied"),
        })
        {
            var result = policy.Evaluate(Start.AddMinutes(1), Start, poll, attempt, 1);
            Assert.That(result.Code, Is.EqualTo("offline-service-stop-required"), $"{poll}/{attempt}");
            Assert.That(result.MayContinueExistingService, Is.False);
        }
    }

    [Test]
    public void Cbi49_C3_grace_requires_prior_current_and_never_refreshes_it()
    {
        var policy = ProviderTrustOfflinePolicy.Create(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));
        var noBaseline = policy.Evaluate(Start.AddMinutes(1), null,
            "policy-poll-exhausted", "policy-distribution-timeout", 1);
        var before = policy.Evaluate(Start.AddMinutes(4), Start,
            "policy-poll-exhausted", "policy-distribution-timeout", 1);
        var atDeadline = policy.Evaluate(Start.AddMinutes(5), Start,
            "policy-poll-exhausted", "policy-distribution-timeout", 1);
        var later = policy.Evaluate(Start.AddMinutes(6), Start,
            "policy-poll-exhausted", "policy-distribution-timeout", 1);

        Assert.Multiple(() =>
        {
            Assert.That(noBaseline.Code, Is.EqualTo("offline-service-stop-required"));
            Assert.That(before.Code, Is.EqualTo("offline-existing-service"));
            Assert.That(atDeadline.Code, Is.EqualTo("offline-grace-expired"));
            Assert.That(later.Code, Is.EqualTo("offline-grace-expired"));
            Assert.That(later.Deadline, Is.EqualTo(before.Deadline));
        });
    }

    [Test]
    public void Cbi49_C4_offline_continuation_is_existing_service_only()
    {
        var policy = ProviderTrustOfflinePolicy.Create(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));
        var serving = policy.Evaluate(Start.AddMinutes(2), Start,
            "policy-poll-exhausted", "policy-distribution-transport-failed", 2);
        var idle = policy.Evaluate(Start.AddMinutes(2), Start,
            "policy-poll-exhausted", "policy-distribution-transport-failed", 0);

        Assert.Multiple(() =>
        {
            Assert.That(serving.MayContinueExistingService, Is.True);
            Assert.That(serving.MayStartProvider, Is.False);
            Assert.That(idle.Code, Is.EqualTo("offline-idle"));
            Assert.That(idle.MayContinueExistingService, Is.False);
            Assert.That(idle.MayStartProvider, Is.False);
        });
    }

    [Test]
    public void Cbi49_C5_reconciliation_evidence_names_the_interrupted_attempt_exactly()
    {
        using var temporary = new TemporaryJournal();
        var journal = Interrupted(temporary.Path);
        var before = File.ReadAllBytes(temporary.Path);
        var mismatch = new ProviderTrustCadenceReconciliationEvidence(
            RunId, 1, Start, ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed);
        var result = ProviderTrustCadenceReconciliation.Apply(journal, mismatch);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("cadence-reconciliation-mismatch"));
            Assert.That(result.Snapshot.Phase, Is.EqualTo("in-flight"));
            Assert.That(File.ReadAllBytes(temporary.Path), Is.EqualTo(before));
        });
    }

    [Test]
    public void Cbi49_C6_unknown_evidence_leaves_the_interruption_inert()
    {
        using var temporary = new TemporaryJournal();
        var journal = Interrupted(temporary.Path);
        var before = File.ReadAllBytes(temporary.Path);
        var evidence = new ProviderTrustCadenceReconciliationEvidence(
            RunId, 0, Start, ProviderTrustCadenceReconciliationVerdict.Unknown);
        var result = ProviderTrustCadenceReconciliation.Apply(journal, evidence);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("cadence-reconciliation-deferred"));
            Assert.That(result.Snapshot.Phase, Is.EqualTo("in-flight"));
            Assert.That(result.Snapshot.InterruptionCount, Is.Zero);
            Assert.That(File.ReadAllBytes(temporary.Path), Is.EqualTo(before));
        });
    }

    [Test]
    public void Cbi49_C7_conclusive_evidence_selects_one_cbi48_transition()
    {
        using var retryTemporary = new TemporaryJournal();
        using var abandonTemporary = new TemporaryJournal();
        var retry = Interrupted(retryTemporary.Path);
        var abandon = Interrupted(abandonTemporary.Path);

        var retried = ProviderTrustCadenceReconciliation.Apply(retry,
            new(RunId, 0, Start, ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed));
        var repeated = ProviderTrustCadenceReconciliation.Apply(retry,
            new(RunId, 0, Start, ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed));
        var abandoned = ProviderTrustCadenceReconciliation.Apply(abandon,
            new(RunId, 0, Start, ProviderTrustCadenceReconciliationVerdict.EffectsAccountedFor));

        Assert.Multiple(() =>
        {
            Assert.That(retried.Code, Is.EqualTo("cadence-reconciliation-retry-ready"));
            Assert.That(retried.Snapshot.InterruptionCount, Is.EqualTo(1));
            Assert.That(retried.Snapshot.RetryCount, Is.EqualTo(1));
            Assert.That(repeated.Code, Is.EqualTo("cadence-reconciliation-not-required"));
            Assert.That(repeated.Snapshot.InterruptionCount, Is.EqualTo(1));
            Assert.That(abandoned.Code, Is.EqualTo("cadence-reconciliation-abandoned"));
            Assert.That(abandoned.Snapshot.InterruptionCount, Is.EqualTo(1));
            Assert.That(abandoned.Snapshot.RetryCount, Is.Zero);
        });
    }

    [Test]
    public void Cbi49_C8_reference_executes_the_shared_policy_model()
    {
        var fixture = LoadFixture();
        var policy = ProviderTrustOfflinePolicy.Create(
            TimeSpan.FromSeconds(fixture.Policy.GraceSeconds),
            TimeSpan.FromSeconds(fixture.Policy.RetrySeconds));

        foreach (var vector in fixture.OfflineVectors)
        {
            DateTimeOffset? baseline = vector.Id == "no-current-baseline"
                ? null
                : vector.LastCurrent ?? fixture.Policy.LastCurrent;
            var result = policy.Evaluate(vector.Now, baseline,
                vector.PollCode, vector.LastAttemptCode, vector.ServingCount);
            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo(vector.ExpectedCode), vector.Id);
                Assert.That(result.MayContinueExistingService, Is.EqualTo(vector.ContinueExisting), vector.Id);
                Assert.That(result.MayStartProvider, Is.EqualTo(vector.MayStart), vector.Id);
                Assert.That(result.Deadline, Is.EqualTo(vector.Deadline), vector.Id);
                Assert.That(result.RetryAt, Is.EqualTo(vector.RetryAt), vector.Id);
            });
        }

        foreach (var vector in fixture.ReconciliationVectors)
        {
            using var temporary = new TemporaryJournal();
            var journal = Interrupted(temporary.Path);
            var verdict = vector.Verdict switch
            {
                "no-effects-confirmed" => ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed,
                "effects-accounted-for" => ProviderTrustCadenceReconciliationVerdict.EffectsAccountedFor,
                _ => ProviderTrustCadenceReconciliationVerdict.Unknown,
            };
            var result = ProviderTrustCadenceReconciliation.Apply(journal,
                new(RunId, vector.AttemptIndex ?? 0, Start, verdict));
            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo(vector.ExpectedCode), vector.Id);
                Assert.That(result.Snapshot.Phase, Is.EqualTo(vector.ExpectedPhase), vector.Id);
                Assert.That(result.Snapshot.InterruptionCount, Is.EqualTo(vector.Interruptions), vector.Id);
                Assert.That(result.Snapshot.RetryCount, Is.EqualTo(vector.Retries), vector.Id);
            });
        }
    }
}
