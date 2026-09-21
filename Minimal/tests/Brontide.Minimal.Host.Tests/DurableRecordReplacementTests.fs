namespace Brontide.Minimal.Host.Tests

open System
open System.Diagnostics
open System.IO
open System.Threading.Tasks
open Brontide.Minimal.Host
open NUnit.Framework

/// Every durable store replaces its record by renaming a freshly written temporary over it. On
/// Windows a scanner or indexer can hold either file open for a moment without sharing deletion,
/// and the rename then fails with the exceptions each store maps to its write-failed code. The hold
/// is transient and the code was not: two suites reported it on conforming runs.
[<TestFixture>]
type DurableRecordReplacementTests() =

    let start = DateTimeOffset.FromUnixTimeSeconds 1_800_000_000L

    let establish path =
        let opened =
            DurableProviderTrustCadenceJournal.Establish(
                path,
                ProviderTrustCadenceRunId.create "replacement-run",
                ProviderServingTrustCadenceSchedule.create 4 (TimeSpan.FromSeconds 5.0),
                start)
        opened.Journal.Value

    let deleteTree path = try Directory.Delete(path, true) with _ -> ()

    [<Test>]
    member _.``a transient reader on the record does not fail the write``() =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-replacement-{Guid.NewGuid():N}")
        try
            let path = Path.Combine(root, "cadence.bin")
            let journal = establish path
            // Held the way a scanner holds it: readable, and not shareable for deletion or
            // replacement, for a moment that ends while the write is still trying.
            let holder = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            let release =
                Task.Run(fun () ->
                    Task.Delay(TimeSpan.FromMilliseconds 40.0).Wait()
                    holder.Dispose())
            try
                Assert.That(journal.BeginCycle(None).Code, Is.EqualTo "durable-cadence-cycle-started")
            finally
                release.Wait()
                holder.Dispose()
        finally
            deleteTree root

    [<Test>]
    member _.``a record replaced by a directory still fails the write within a bound``() =
        // The refused case, and the bound on it: a condition that does not pass is reported as it
        // always was, after a wait a caller cannot mistake for a hang.
        let root = Path.Combine(Path.GetTempPath(), $"brontide-replacement-{Guid.NewGuid():N}")
        try
            let path = Path.Combine(root, "cadence.bin")
            let journal = establish path
            File.Delete path
            Directory.CreateDirectory path |> ignore
            let elapsed = Stopwatch.StartNew()
            let code = journal.BeginCycle(None).Code
            elapsed.Stop()
            Assert.Multiple(Action(fun () ->
                Assert.That(code, Is.EqualTo "durable-cadence-write-failed")
                Assert.That(elapsed.Elapsed, Is.LessThan(TimeSpan.FromSeconds 5.0))))
        finally
            deleteTree root
