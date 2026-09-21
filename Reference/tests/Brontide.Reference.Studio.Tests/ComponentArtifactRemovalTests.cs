using System.Diagnostics;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    /// <summary>
    /// A staged set is removed the moment its last lease is released, which after a withdrawal is
    /// the moment its provider was killed -- and Windows lets go of a killed process's image a
    /// little after the process is gone, later still when many are torn down at once. A hold that
    /// ends is a wait, not a failure.
    /// </summary>
    [Test, Category("CrossProcess")]
    public void Cbi32_removal_outwaits_a_transient_hold_on_a_staged_file()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi32-{Guid.NewGuid():N}");
        try
        {
            var store = new ContentAddressedProviderStore(Path.Combine(testRoot, "store"));
            var declaration = Cbi32Declaration("reference", Path.Combine(testRoot, "source"), "none");
            var staged = store.Stage(declaration);
            Assert.That(staged.IsStaged, Is.True);
            var stagedFile = Directory.EnumerateFiles(staged.Staged!.RootPath, "*", SearchOption.AllDirectories).First();
            var holder = new FileStream(stagedFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var release = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(300));
                holder.Dispose();
            });
            try
            {
                Assert.That(store.Remove(declaration.Identity).Code, Is.EqualTo("removed"));
            }
            finally
            {
                release.Wait();
                holder.Dispose();
            }
            Assert.That(Directory.Exists(staged.Staged.RootPath), Is.False);
        }
        finally
        {
            Cbi32DeleteTree(testRoot);
        }
    }

    [Test, Category("CrossProcess")]
    public void Cbi32_removal_reports_a_hold_that_does_not_end_within_a_bound()
    {
        // The refused case, and the bound on it: a hold that outlasts the wait is reported as the
        // removal failure it always was, and the set stays where a later removal can find it.
        var testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi32-{Guid.NewGuid():N}");
        try
        {
            var store = new ContentAddressedProviderStore(Path.Combine(testRoot, "store"));
            var declaration = Cbi32Declaration("reference", Path.Combine(testRoot, "source"), "none");
            var staged = store.Stage(declaration);
            Assert.That(staged.IsStaged, Is.True);
            var stagedFile = Directory.EnumerateFiles(staged.Staged!.RootPath, "*", SearchOption.AllDirectories).First();
            var elapsed = Stopwatch.StartNew();
            ProviderArtifactRemoval removal;
            using (new FileStream(stagedFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                removal = store.Remove(declaration.Identity);
            }
            elapsed.Stop();
            Assert.Multiple(() =>
            {
                Assert.That(removal.Code, Is.EqualTo("artifact-set-removal-failed"));
                Assert.That(elapsed.Elapsed, Is.LessThan(TimeSpan.FromSeconds(15)));
            });
            Assert.That(store.Remove(declaration.Identity).Code, Is.EqualTo("removed"));
        }
        finally
        {
            Cbi32DeleteTree(testRoot);
        }
    }
}
