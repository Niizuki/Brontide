using System.Diagnostics;
using System.Text.Json;
using Brontide.Reference.Experimental.ComponentManagement;

namespace Brontide.Reference.Studio.Tests;

[TestFixture]
public sealed class ComponentProviderRestartOwnershipTests
{
    private static readonly ProviderRestartAttemptRunId RunId = ProviderRestartAttemptRunId.Create("restart-run.test.1");
    private static readonly OccurrenceId Occurrence = OccurrenceId.Create("occ.def.test.cooling-provider.1");
    private static readonly ProviderArtifactSetId Staged = ProviderArtifactSetId.Create(new string('A', 64));
    private static readonly ProviderRestartOwnerId Owner = ProviderRestartOwnerId.Create("owner-a");
    private static readonly ProviderRestartOwnershipLeaseId Lease = ProviderRestartOwnershipLeaseId.Create("lease-a");

    private sealed class TemporaryOwnership : IDisposable
    {
        public string Root { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"brontide-cbi54-{Guid.NewGuid():N}");
        /// <summary>
        /// Ownership is acquired for a journal, so the journal path is what the tests name. The lock
        /// and its state are derived from it rather than chosen, which is the property C2 now rests on.
        /// </summary>
        public string JournalPath => System.IO.Path.Combine(Root, "restart.journal");
        public string LockPath => JournalPath + ".ownership";
        public string StatePath => LockPath + ".state";
        public void Dispose()
        {
            for (var attempt = 0; attempt < 250; attempt++)
            {
                try
                {
                    if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
                    return;
                }
                catch (IOException) when (attempt < 249)
                {
                    Thread.Sleep(20);
                }
            }
        }
    }

    private static ProviderRestartOwnershipAcquireResult Acquire(string path, string owner = "owner-a", string lease = "lease-a") =>
        DurableProviderRestartOwnership.Acquire(
            path, ProviderRestartOwnerId.Create(owner), ProviderRestartOwnershipLeaseId.Create(lease),
            RunId, Occurrence, Staged);

    /// <summary>
    /// Acquires after a holder process was killed. A process that has reported exit has not
    /// necessarily had its file handles released by the kernel yet, so the first attempt can still see
    /// the lock held and answer <c>restart-ownership-busy</c> with no snapshot. Retrying on exactly
    /// that code waits for the release without waiting a fixed time for it, and without weakening what
    /// the caller then asserts: any other code is returned immediately, so a lock that is never
    /// released still fails the test rather than passing late.
    /// </summary>
    private static ProviderRestartOwnershipAcquireResult AcquireAfterProcessLoss(
        string path, string owner, string lease)
    {
        for (var attempt = 0; ; attempt++)
        {
            var result = Acquire(path, owner, lease);
            if (result.Code != "restart-ownership-busy" || attempt == 249) return result;
            Thread.Sleep(20);
        }
    }

    private static async Task<int> ProbeAsync(string provider, string verb, string path, bool waitForHeld = false)
    {
        var start = new ProcessStartInfo(provider) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true };
        start.ArgumentList.Add($"--{verb}-exclusive-file={path}");
        using var process = Process.Start(start)!;
        if (waitForHeld) Assert.That(await process.StandardOutput.ReadLineAsync(), Is.EqualTo("held"));
        if (waitForHeld)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
        else await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static string ProviderPath()
    {
        var path = Environment.GetEnvironmentVariable("BRONTIDE_REFERENCE_PROVIDER");
        if (path is null || !File.Exists(path)) Assert.Ignore("BRONTIDE_REFERENCE_PROVIDER does not name a built provider endpoint.");
        return Path.GetFullPath(path!);
    }

    [Test]
    public void Cbi54_C1_ownership_is_bound_to_one_restart_lineage()
    {
        using var temporary = new TemporaryOwnership();
        var acquired = Acquire(temporary.JournalPath);
        acquired.Ownership!.Dispose();
        var before = File.ReadAllBytes(temporary.StatePath);
        var mismatch = DurableProviderRestartOwnership.Acquire(
            temporary.JournalPath, Owner, Lease, ProviderRestartAttemptRunId.Create("restart-run.other"), Occurrence, Staged);
        Assert.Multiple(() =>
        {
            Assert.That(acquired.Code, Is.EqualTo("restart-ownership-acquired"));
            Assert.That(mismatch.Code, Is.EqualTo("restart-ownership-lineage-mismatch"));
            Assert.That(File.ReadAllBytes(temporary.StatePath), Is.EqualTo(before));
        });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi54_C2_one_operating_system_owner_excludes_other_processes()
    {
        using var temporary = new TemporaryOwnership();
        var acquired = Acquire(temporary.JournalPath);
        Assert.That(await ProbeAsync(ProviderPath(), "probe", temporary.LockPath), Is.EqualTo(74));
        acquired.Ownership!.Dispose();
        Assert.That(await ProbeAsync(ProviderPath(), "probe", temporary.LockPath), Is.Zero);
    }

    [Test]
    public void Cbi54_C3_every_acquisition_advances_an_atomic_durable_fence()
    {
        using var temporary = new TemporaryOwnership();
        var first = Acquire(temporary.JournalPath);
        first.Ownership!.Dispose();
        var before = File.ReadAllBytes(temporary.StatePath);
        Directory.CreateDirectory(temporary.StatePath + ".tmp");
        var refused = Acquire(temporary.JournalPath, "owner-b", "lease-b");
        var afterRefused = File.ReadAllBytes(temporary.StatePath);
        Directory.Delete(temporary.StatePath + ".tmp");
        var second = Acquire(temporary.JournalPath, "owner-b", "lease-b");
        Assert.Multiple(() =>
        {
            Assert.That(refused.Code, Is.EqualTo("restart-ownership-write-failed"));
            Assert.That(afterRefused, Is.EqualTo(before));
            Assert.That(second.Snapshot!.Epoch, Is.EqualTo(2));
        });
        second.Ownership!.Dispose();
    }

    [Test]
    public void Cbi54_C4_only_the_current_live_lease_matches_the_journal()
    {
        using var temporary = new TemporaryOwnership();
        var journal = DurableProviderRestartAttemptJournal.Establish(
            temporary.JournalPath, RunId, Occurrence, Staged,
            ProviderRestartPolicy.Create(2, TimeSpan.FromMinutes(1))).Journal!;
        var before = File.ReadAllBytes(temporary.JournalPath);
        var ownership = Acquire(temporary.JournalPath).Ownership!;
        Assert.That(ownership.IsCurrentFor(journal), Is.True);
        ownership.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(ownership.IsCurrentFor(journal), Is.False);
            Assert.That(File.ReadAllBytes(temporary.JournalPath), Is.EqualTo(before));
        });
    }

    /// <summary>
    /// The finding this capability exists for. Lineage identities do not identify a journal: a second
    /// journal carrying the same run, occurrence, and staged identity — which a copied journal file
    /// carries by construction — was previously indistinguishable from the one the lease was acquired
    /// for, so a lease could fence a journal nobody was excluded from. Deriving the lock path from the
    /// journal makes two hosts on one journal collide; comparing the path here catches a lease pointed
    /// at a different journal that the identity comparison alone accepts.
    /// </summary>
    [Test]
    public void Cbi54_C4_a_lease_fences_only_the_journal_it_names()
    {
        using var temporary = new TemporaryOwnership();
        var policy = ProviderRestartPolicy.Create(2, TimeSpan.FromMinutes(1));
        var owned = DurableProviderRestartAttemptJournal.Establish(
            temporary.JournalPath, RunId, Occurrence, Staged, policy).Journal!;
        var impostorPath = System.IO.Path.Combine(temporary.Root, "copied.journal");
        var impostor = DurableProviderRestartAttemptJournal.Establish(
            impostorPath, RunId, Occurrence, Staged, policy).Journal!;
        // Released even when an assertion below fails, so the lock never outlives the fixture and
        // wedges its own temporary-directory cleanup.
        using var ownership = Acquire(temporary.JournalPath).Ownership!;
        Assert.Multiple(() =>
        {
            // Identical lineage on both journals, so identity comparison alone cannot separate them.
            Assert.That(impostor.Snapshot.RunIdentity, Is.EqualTo(owned.Snapshot.RunIdentity));
            Assert.That(impostor.Snapshot.Occurrence, Is.EqualTo(owned.Snapshot.Occurrence));
            Assert.That(impostor.Snapshot.StagedIdentity, Is.EqualTo(owned.Snapshot.StagedIdentity));
            Assert.That(ownership.IsCurrentFor(owned), Is.True);
            Assert.That(ownership.IsCurrentFor(impostor), Is.False);
            Assert.That(ownership.JournalPath, Is.EqualTo(System.IO.Path.GetFullPath(temporary.JournalPath)));
        });
    }

    [Test]
    public void Cbi54_C5_released_and_superseded_leases_are_stale()
    {
        using var temporary = new TemporaryOwnership();
        var first = Acquire(temporary.JournalPath);
        first.Ownership!.Dispose();
        var second = Acquire(temporary.JournalPath);
        Assert.Multiple(() =>
        {
            Assert.That(first.Ownership.Snapshot.IsLive, Is.False);
            Assert.That(second.Snapshot!.Epoch, Is.EqualTo(2));
            Assert.That(second.Snapshot.IsLive, Is.True);
        });
        second.Ownership!.Dispose();
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi54_C6_process_loss_relinquishes_exclusivity_without_erasing_history()
    {
        using var temporary = new TemporaryOwnership();
        var first = Acquire(temporary.JournalPath);
        first.Ownership!.Dispose();
        var start = new ProcessStartInfo(ProviderPath())
            { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true };
        start.ArgumentList.Add($"--hold-exclusive-file={temporary.LockPath}");
        using var holder = Process.Start(start)!;
        Assert.That(await holder.StandardOutput.ReadLineAsync(), Is.EqualTo("held"));
        // Strict while the holder is alive: exclusivity must be refused at the first attempt.
        Assert.That(Acquire(temporary.JournalPath, "owner-b", "lease-b").Code, Is.EqualTo("restart-ownership-busy"));
        holder.Kill(entireProcessTree: true);
        await holder.WaitForExitAsync();
        var recovered = AcquireAfterProcessLoss(temporary.JournalPath, "owner-b", "lease-b");
        Assert.Multiple(() =>
        {
            Assert.That(recovered.Code, Is.EqualTo("restart-ownership-acquired"));
            Assert.That(recovered.Snapshot!.Epoch, Is.EqualTo(2));
        });
        recovered.Ownership!.Dispose();
    }

    [Test]
    public void Cbi54_C7_inspection_is_bounded_and_fails_closed()
    {
        using var temporary = new TemporaryOwnership();
        Assert.That(DurableProviderRestartOwnership.Inspect(temporary.JournalPath, RunId, Occurrence, Staged).Code,
            Is.EqualTo("restart-ownership-missing"));
        var acquired = Acquire(temporary.JournalPath);
        acquired.Ownership!.Dispose();
        var bytes = File.ReadAllBytes(temporary.StatePath);
        bytes[0] ^= 0x7F;
        File.WriteAllBytes(temporary.StatePath, bytes);
        Assert.That(DurableProviderRestartOwnership.Inspect(temporary.JournalPath, RunId, Occurrence, Staged).Code,
            Is.EqualTo("restart-ownership-corrupt"));
    }

    [Test]
    public void Cbi54_C8_reference_executes_the_shared_ownership_model()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures", "cbi54-restart-ownership-vectors.json")));
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            using var temporary = new TemporaryOwnership();
            DurableProviderRestartOwnership? ownership = null;
            ProviderRestartOwnershipSnapshot? snapshot = null;
            var code = "none";
            foreach (var action in vector.GetProperty("actions").EnumerateArray().Select(value => value.GetString()!))
            {
                if (action.StartsWith("acquire:", StringComparison.Ordinal))
                {
                    var parts = action.Split(':');
                    var result = Acquire(temporary.JournalPath, parts[1], parts[2]);
                    code = result.Code; ownership = result.Ownership; snapshot = result.Snapshot;
                }
                else if (action == "release") { ownership!.Dispose(); snapshot = ownership.Snapshot; }
                else if (action == "inspect")
                {
                    var result = DurableProviderRestartOwnership.Inspect(temporary.JournalPath, RunId, Occurrence, Staged);
                    code = result.Code; snapshot = result.Snapshot;
                }
            }
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()));
                Assert.That(snapshot!.Epoch, Is.EqualTo(vector.GetProperty("expectedEpoch").GetInt64()));
                Assert.That(snapshot.Owner.Value, Is.EqualTo(vector.GetProperty("expectedOwner").GetString()));
                Assert.That(snapshot.Lease.Value, Is.EqualTo(vector.GetProperty("expectedLease").GetString()));
                Assert.That(snapshot.IsLive, Is.EqualTo(vector.GetProperty("expectedLive").GetBoolean()));
            });
            ownership?.Dispose();
        }
    }
}
