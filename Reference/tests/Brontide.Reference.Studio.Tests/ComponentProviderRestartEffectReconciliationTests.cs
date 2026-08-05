using System.Diagnostics;
using System.Text.Json;
using Brontide.Reference.Experimental.ComponentManagement;

namespace Brontide.Reference.Studio.Tests;

[TestFixture]
public sealed class ComponentProviderRestartEffectReconciliationTests
{
    private static readonly ProviderRestartAttemptRunId RunId = ProviderRestartAttemptRunId.Create("restart-run.effect.1");
    private static readonly OccurrenceId Occurrence = OccurrenceId.Create("occ.def.test.cooling-provider.1");
    private static readonly ProviderArtifactSetId Staged = ProviderArtifactSetId.Create(new string('B', 64));
    private static readonly DateTimeOffset Instant = new(2026, 8, 5, 10, 0, 0, TimeSpan.Zero);
    private static readonly string ProviderExecutableName = OperatingSystem.IsWindows()
        ? "Brontide.Reference.Interchange.Provider.exe"
        : "Brontide.Reference.Interchange.Provider";

    private sealed class TemporaryEffect : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), $"brontide-cbi55-{Guid.NewGuid():N}");
        public string OwnershipPath => Path.Combine(Root, "restart.owner");
        public string JournalPath => Path.Combine(Root, "restart.journal");
        public string EffectPath => Path.Combine(Root, "restart.effect");
        public void Dispose()
        {
            for (var attempt = 0; attempt < 250; attempt++)
            {
                try { if (Directory.Exists(Root)) Directory.Delete(Root, true); return; }
                catch (IOException) when (attempt < 249) { Thread.Sleep(20); }
            }
        }
    }

    private static string ProviderPath()
    {
        var path = Environment.GetEnvironmentVariable("BRONTIDE_REFERENCE_PROVIDER");
        if (path is null || !File.Exists(path)) Assert.Ignore("BRONTIDE_REFERENCE_PROVIDER does not name a built provider endpoint.");
        return Path.GetFullPath(path!);
    }

    private static DurableProviderRestartAttemptJournal Journal(TemporaryEffect temporary)
        => DurableProviderRestartAttemptJournal.Establish(
            temporary.JournalPath, RunId, Occurrence, Staged,
            ProviderRestartPolicy.Create(2, TimeSpan.FromMinutes(1))).Journal!;

    private static DurableProviderRestartOwnership Acquire(TemporaryEffect temporary, string owner, string lease)
        => DurableProviderRestartOwnership.Acquire(
            temporary.OwnershipPath, ProviderRestartOwnerId.Create(owner), ProviderRestartOwnershipLeaseId.Create(lease),
            RunId, Occurrence, Staged).Ownership!;

    private static DurableProviderRestartEffect Prepare(TemporaryEffect temporary, long epoch, int index = 0,
        DateTimeOffset? instant = null)
        => DurableProviderRestartEffect.Prepare(
            temporary.EffectPath, RunId, Occurrence, Staged, index, instant ?? Instant, epoch,
            ProviderRestartEffectToken.Create("effect-token-1"), ProviderExecutableName).Effect!;

    private static Process StartProvider(DurableProviderRestartEffect effect)
    {
        var start = new ProcessStartInfo(ProviderPath())
            { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true };
        foreach (var fact in effect.Environment) start.Environment[fact.Key] = fact.Value;
        return Process.Start(start)!;
    }

    private static async Task AwaitReceiptAsync(DurableProviderRestartEffect effect)
    {
        for (var attempt = 0; attempt < 250 && !File.Exists(effect.Snapshot.ReceiptPath); attempt++) await Task.Delay(20);
        Assert.That(File.Exists(effect.Snapshot.ReceiptPath), Is.True, "The provider did not publish its bounded CBI55 receipt.");
    }

    [Test]
    public void Cbi55_C1_record_binds_the_exact_attempt_and_fence()
    {
        using var temporary = new TemporaryEffect();
        var effect = DurableProviderRestartEffect.Prepare(
            temporary.EffectPath, RunId, Occurrence, Staged, 0, Instant, 7,
            ProviderRestartEffectToken.Create("effect-token-1"), ProviderExecutableName);
        var exact = DurableProviderRestartEffect.Open(temporary.EffectPath, RunId, Occurrence, Staged);
        var mismatch = DurableProviderRestartEffect.Open(
            temporary.EffectPath, ProviderRestartAttemptRunId.Create("restart-run.other"), Occurrence, Staged);
        Assert.Multiple(() =>
        {
            Assert.That(effect.Code, Is.EqualTo("restart-effect-prepared"));
            Assert.That(exact.Snapshot, Is.EqualTo(effect.Snapshot));
            Assert.That(exact.Snapshot!.AttemptIndex, Is.Zero);
            Assert.That(exact.Snapshot.FencingEpoch, Is.EqualTo(7));
            Assert.That(mismatch.Code, Is.EqualTo("restart-effect-lineage-mismatch"));
        });
    }

    [Test]
    public void Cbi55_C2_record_and_provider_facts_precede_the_in_flight_transition()
    {
        using var temporary = new TemporaryEffect();
        var journal = Journal(temporary);
        using var owner = Acquire(temporary, "owner-a", "lease-a");
        var effect = Prepare(temporary, owner.Snapshot.Epoch);
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(temporary.EffectPath), Is.True);
            Assert.That(effect.Environment.Keys, Does.Contain("BRONTIDE_RESTART_EFFECT_LEASE"));
            Assert.That(journal.Snapshot.Phase, Is.EqualTo("ready"));
        });
        Assert.That(journal.BeginAttempt(Instant).Code, Is.EqualTo("durable-restart-attempt-started"));
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi55_C3_provider_holds_the_token_lease_and_writes_its_receipt()
    {
        using var temporary = new TemporaryEffect();
        using var owner = Acquire(temporary, "owner-a", "lease-a");
        var effect = Prepare(temporary, owner.Snapshot.Epoch);
        using var provider = StartProvider(effect);
        try
        {
            await AwaitReceiptAsync(effect);
            Assert.Throws<IOException>(() =>
            {
                using var unavailable = new FileStream(effect.Snapshot.LeasePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            });
        }
        finally
        {
            if (!provider.HasExited) provider.Kill(true);
            await provider.WaitForExitAsync();
        }
    }

    [Test]
    public async Task Cbi55_C4_a_free_lease_proves_retry_is_safe()
    {
        using var temporary = new TemporaryEffect();
        var journal = Journal(temporary);
        var first = Acquire(temporary, "owner-a", "lease-a");
        Prepare(temporary, first.Snapshot.Epoch);
        Assert.That(journal.BeginAttempt(Instant).Code, Is.EqualTo("durable-restart-attempt-started"));
        first.Dispose();
        using var successor = Acquire(temporary, "owner-b", "lease-b");
        var result = await ExternallyReconciledProviderRestartRecovery.ReconcileAsync(successor, journal, temporary.EffectPath);
        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("restart-effect-no-live-provider"));
            Assert.That(result.LeaseAvailable, Is.True);
            Assert.That(result.Journal.Phase, Is.EqualTo("ready"));
            Assert.That(result.Journal.RetryCount, Is.EqualTo(1));
        });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi55_C5_an_exact_orphan_is_terminated_before_retry()
    {
        using var temporary = new TemporaryEffect();
        var journal = Journal(temporary);
        var first = Acquire(temporary, "owner-a", "lease-a");
        var effect = Prepare(temporary, first.Snapshot.Epoch);
        Assert.That(journal.BeginAttempt(Instant).Code, Is.EqualTo("durable-restart-attempt-started"));
        using var provider = StartProvider(effect);
        await AwaitReceiptAsync(effect);
        first.Dispose();
        using var successor = Acquire(temporary, "owner-b", "lease-b");
        var result = await ExternallyReconciledProviderRestartRecovery.ReconcileAsync(successor, journal, temporary.EffectPath);
        await provider.WaitForExitAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("restart-effect-provider-terminated"));
            Assert.That(result.ProcessTerminated, Is.True);
            Assert.That(result.Journal.Phase, Is.EqualTo("ready"));
        });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi55_C6_uncertain_evidence_remains_in_flight()
    {
        using var temporary = new TemporaryEffect();
        var journal = Journal(temporary);
        var first = Acquire(temporary, "owner-a", "lease-a");
        var effect = Prepare(temporary, first.Snapshot.Epoch);
        journal.BeginAttempt(Instant);
        var start = new ProcessStartInfo(ProviderPath())
            { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true };
        start.ArgumentList.Add($"--hold-exclusive-file={effect.Snapshot.LeasePath}");
        using var holder = Process.Start(start)!;
        Assert.That(await holder.StandardOutput.ReadLineAsync(), Is.EqualTo("held"));
        first.Dispose();
        using var successor = Acquire(temporary, "owner-b", "lease-b");
        var result = await ExternallyReconciledProviderRestartRecovery.ReconcileAsync(successor, journal, temporary.EffectPath);
        holder.Kill(true);
        await holder.WaitForExitAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("restart-effect-reconciliation-deferred"));
            Assert.That(result.Journal.Phase, Is.EqualTo("in-flight"));
            Assert.That(result.Journal.RetryCount, Is.Zero);
        });
    }

    [Test]
    public async Task Cbi55_C7_only_a_successor_fence_may_reconcile()
    {
        using var temporary = new TemporaryEffect();
        var journal = Journal(temporary);
        using var owner = Acquire(temporary, "owner-a", "lease-a");
        Prepare(temporary, owner.Snapshot.Epoch);
        journal.BeginAttempt(Instant);
        var result = await ExternallyReconciledProviderRestartRecovery.ReconcileAsync(owner, journal, temporary.EffectPath);
        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("restart-effect-successor-fence-required"));
            Assert.That(result.Journal.Phase, Is.EqualTo("in-flight"));
        });
    }

    [Test]
    public async Task Cbi55_C8_reference_executes_the_shared_reconciliation_model()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures", "cbi55-restart-effect-reconciliation-vectors.json")));
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            using var temporary = new TemporaryEffect();
            var journal = Journal(temporary);
            var first = Acquire(temporary, "owner-a", "lease-a");
            var effectKind = vector.GetProperty("effect").GetString();
            if (effectKind != "missing")
                Prepare(temporary, effectKind == "exact-current-fence" ? 2 : 1,
                    effectKind == "wrong-attempt" ? 1 : 0);
            journal.BeginAttempt(Instant);
            first.Dispose();
            using var successor = Acquire(temporary, "owner-b", "lease-b");
            var result = await ExternallyReconciledProviderRestartRecovery.ReconcileAsync(successor, journal, temporary.EffectPath);
            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()));
                Assert.That(result.Journal.Phase, Is.EqualTo(vector.GetProperty("expectedPhase").GetString()));
                Assert.That(result.Journal.RetryCount, Is.EqualTo(vector.GetProperty("expectedRetries").GetInt32()));
            });
        }
    }
}
