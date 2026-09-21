using System.Diagnostics;

namespace Brontide.Reference.Studio.Tests;

/// <summary>
/// Every durable store replaces its record by renaming a freshly written temporary over it. On
/// Windows a scanner or indexer can hold either file open for a moment without sharing deletion,
/// and the rename then fails with the exceptions each store maps to its write-failed code. The hold
/// is transient and the code was not: two suites reported it on conforming runs.
/// </summary>
public sealed class DurableRecordReplacementTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);

    private static DurableProviderTrustCadenceJournal Establish(string path) =>
        DurableProviderTrustCadenceJournal.Establish(
            path, ProviderTrustCadenceRunId.Create("replacement-run"),
            ProviderServingTrustCadenceSchedule.Create(4, TimeSpan.FromSeconds(5)), Start).Journal!;

    [Test]
    public void A_transient_reader_on_the_record_does_not_fail_the_write()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-replacement-{Guid.NewGuid():N}");
        try
        {
            var path = Path.Combine(root, "cadence.bin");
            var journal = Establish(path);
            // Held the way a scanner holds it: readable, and not shareable for deletion or
            // replacement, for a moment that ends while the write is still trying.
            var holder = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var release = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(40));
                holder.Dispose();
            });
            try
            {
                Assert.That(journal.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));
            }
            finally
            {
                release.Wait();
                holder.Dispose();
            }
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void A_record_replaced_by_a_directory_still_fails_the_write_within_a_bound()
    {
        // The refused case, and the bound on it: a condition that does not pass is reported as it
        // always was, after a wait a caller cannot mistake for a hang.
        var root = Path.Combine(Path.GetTempPath(), $"brontide-replacement-{Guid.NewGuid():N}");
        try
        {
            var path = Path.Combine(root, "cadence.bin");
            var journal = Establish(path);
            File.Delete(path);
            Directory.CreateDirectory(path);
            var elapsed = Stopwatch.StartNew();
            var code = journal.BeginCycle().Code;
            elapsed.Stop();
            Assert.Multiple(() =>
            {
                Assert.That(code, Is.EqualTo("durable-cadence-write-failed"));
                Assert.That(elapsed.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
