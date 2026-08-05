namespace Brontide.Minimal.Host.Tests

open System
open System.Globalization
open System.IO
open System.Text.Json
open NUnit.Framework
open Brontide.Minimal.Experimental.ComponentManagement
open Brontide.Minimal.Host

type private RestartTemporaryJournal() =
    let path = Path.Combine(Path.GetTempPath(), $"brontide-cbi53-{Guid.NewGuid():N}", "restart.journal")
    member _.Path = path
    interface IDisposable with
        member _.Dispose() =
            match Path.GetDirectoryName path with
            | null | "" -> ()
            | root when Directory.Exists root -> Directory.Delete(root, true)
            | _ -> ()

[<TestFixture>]
type ComponentProviderRestartAttemptRecoveryTests() =
    let multiple action = Assert.Multiple(Action action)
    let runIdentity = ProviderRestartAttemptRunId.create "restart-run.test.1"
    let occurrence = OccurrenceId.create "occ.def.test.cooling-provider.1"
    let staged = ProviderArtifactSetId.create (String('A', 64))
    let policy = ProviderRestartPolicy.create 2 (TimeSpan.FromMinutes 1.0)
    let start = DateTimeOffset(2026, 8, 5, 9, 0, 0, TimeSpan.Zero)

    let establish path =
        DurableProviderRestartAttemptJournal.Establish(path, runIdentity, occurrence, staged, policy).Journal.Value

    let commit (journal: DurableProviderRestartAttemptJournal) instant code =
        Assert.That(journal.BeginAttempt(instant).Code, Is.EqualTo "durable-restart-attempt-started")
        let completed = code = "provider-restart-completed"
        let refusedBy = if completed then "none" elif code = "portable-process-interrupted" then "cbi2" else "cbi31"
        journal.CommitAttempt(code, refusedBy, code = "portable-process-interrupted" || completed, completed, completed)

    [<Test>]
    member _.``CBI53 C1 one journal names one bounded restart lineage``() =
        use temporary = new RestartTemporaryJournal()
        let established = DurableProviderRestartAttemptJournal.Establish(temporary.Path, runIdentity, occurrence, staged, policy)
        let duplicate = DurableProviderRestartAttemptJournal.Establish(temporary.Path, runIdentity, occurrence, staged, policy)
        let mismatch = DurableProviderRestartAttemptJournal.Open(temporary.Path, runIdentity, OccurrenceId.create "occ.def.test.other.1", staged)
        multiple (fun () ->
            Assert.That(established.Code, Is.EqualTo "durable-restart-established")
            Assert.That(duplicate.Code, Is.EqualTo "durable-restart-already-exists")
            Assert.That(mismatch.Code, Is.EqualTo "durable-restart-lineage-mismatch")
            Assert.That(established.Journal.Value.Snapshot.MaximumAttempts, Is.EqualTo 2))

    [<Test>]
    member _.``CBI53 C2 every transition is atomic and integrity checked``() =
        use temporary = new RestartTemporaryJournal()
        let journal = establish temporary.Path
        let original = File.ReadAllBytes temporary.Path
        Directory.CreateDirectory(temporary.Path + ".tmp") |> ignore
        let refused = journal.BeginAttempt start
        Directory.Delete(temporary.Path + ".tmp")
        multiple (fun () ->
            Assert.That(refused.Code, Is.EqualTo "durable-restart-write-failed")
            Assert.That(File.ReadAllBytes temporary.Path, Is.EqualTo(box original)))
        let bytes = File.ReadAllBytes temporary.Path
        bytes[0] <- bytes[0] ^^^ 0x7Fuy
        File.WriteAllBytes(temporary.Path, bytes)
        Assert.That(DurableProviderRestartAttemptJournal.Open(temporary.Path, runIdentity, occurrence, staged).Code,
            Is.EqualTo "durable-restart-corrupt")

    [<Test>]
    member _.``CBI53 C3 non ready policy history changes no journal state``() =
        use temporary = new RestartTemporaryJournal()
        let journal = establish temporary.Path
        commit journal start "staged-artifact-integrity-failed" |> ignore
        let before = File.ReadAllBytes temporary.Path
        let waiting = journal.BeginAttempt(start.AddSeconds 59.0)
        multiple (fun () ->
            Assert.That(waiting.Code, Is.EqualTo "durable-restart-waiting")
            Assert.That(File.ReadAllBytes temporary.Path, Is.EqualTo(box before)))

    [<Test>]
    member _.``CBI53 C4 in flight state precedes restart effects``() =
        use temporary = new RestartTemporaryJournal()
        let journal = establish temporary.Path
        Assert.That(journal.BeginAttempt(start).Code, Is.EqualTo "durable-restart-attempt-started")
        let reopened = DurableProviderRestartAttemptJournal.Open(temporary.Path, runIdentity, occurrence, staged)
        multiple (fun () ->
            Assert.That(reopened.Code, Is.EqualTo "durable-restart-indeterminate")
            Assert.That(reopened.Journal.Value.Snapshot.InFlightIndex, Is.EqualTo(Some 0)))

    [<Test>]
    member _.``CBI53 C5 committed failures drive delay and exhaustion``() =
        use temporary = new RestartTemporaryJournal()
        let journal = establish temporary.Path
        commit journal start "portable-process-interrupted" |> ignore
        let exhausted = commit journal (start.AddMinutes 1.0) "staged-artifact-integrity-failed"
        multiple (fun () ->
            Assert.That(exhausted.Code, Is.EqualTo "durable-restart-exhausted")
            Assert.That(exhausted.Snapshot.Attempts, Has.Length.EqualTo 2))

    [<Test>]
    member _.``CBI53 C6 interrupted work requires explicit reconciliation``() =
        use retryFile = new RestartTemporaryJournal()
        let retry = establish retryFile.Path
        retry.BeginAttempt(start) |> ignore
        let ready = retry.ResolveInterrupted ProviderRestartAttemptRecoveryDecision.Retry
        use abandonFile = new RestartTemporaryJournal()
        let abandon = establish abandonFile.Path
        abandon.BeginAttempt(start) |> ignore
        let abandoned = abandon.ResolveInterrupted ProviderRestartAttemptRecoveryDecision.Abandon
        multiple (fun () ->
            Assert.That(ready.Code, Is.EqualTo "durable-restart-retry-ready")
            Assert.That(ready.Snapshot.RetryCount, Is.EqualTo 1)
            Assert.That(abandoned.Code, Is.EqualTo "durable-restart-abandoned"))

    [<Test>]
    member _.``CBI53 C7 terminal recovery is idempotent and effect free``() =
        use temporary = new RestartTemporaryJournal()
        let journal = establish temporary.Path
        commit journal start "provider-restart-completed" |> ignore
        let before = File.ReadAllBytes temporary.Path
        let reopened = DurableProviderRestartAttemptJournal.Open(temporary.Path, runIdentity, occurrence, staged).Journal.Value
        let after = reopened.BeginAttempt(start.AddMinutes 1.0)
        multiple (fun () ->
            Assert.That(after.Code, Is.EqualTo "durable-restart-completed")
            Assert.That(File.ReadAllBytes temporary.Path, Is.EqualTo(box before)))

    [<Test>]
    member _.``CBI53 C8 minimal executes the shared history model``() =
        use fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi53-durable-restart-attempt-vectors.json")))
        let text (value: JsonElement) = value.GetString() |> Option.ofObj |> Option.defaultValue ""
        for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
            use temporary = new RestartTemporaryJournal()
            let mutable journal = establish temporary.Path
            let mutable now = start
            let mutable code = "durable-restart-established"
            for actionElement in vector.GetProperty("actions").EnumerateArray() do
                let action = text actionElement
                if action.StartsWith("attempt:", StringComparison.Ordinal) then
                    code <- (commit journal now action[8..]).Code
                elif action.StartsWith("advance:", StringComparison.Ordinal) then
                    now <- now.AddSeconds(Double.Parse(action[8..], CultureInfo.InvariantCulture))
                elif action = "crash" then code <- journal.BeginAttempt(now).Code
                elif action = "reopen" then
                    let opened = DurableProviderRestartAttemptJournal.Open(temporary.Path, runIdentity, occurrence, staged)
                    code <- opened.Code
                    journal <- opened.Journal.Value
                elif action = "retry" then code <- journal.ResolveInterrupted(ProviderRestartAttemptRecoveryDecision.Retry).Code
                elif action = "abandon" then code <- journal.ResolveInterrupted(ProviderRestartAttemptRecoveryDecision.Abandon).Code
            let expectedInFlight = vector.GetProperty "expectedInFlight"
            multiple (fun () ->
                Assert.That(code, Is.EqualTo(vector.GetProperty("expectedCode") |> text), vector.GetProperty("name") |> text)
                Assert.That(journal.Snapshot.Phase, Is.EqualTo(vector.GetProperty("expectedPhase") |> text))
                Assert.That(journal.Snapshot.Attempts |> List.map _.Code,
                    Is.EqualTo(box (vector.GetProperty("expectedAttemptCodes").EnumerateArray() |> Seq.map text |> Seq.toList)))
                Assert.That(journal.Snapshot.NextAttemptIndex, Is.EqualTo(vector.GetProperty("expectedNextAttempt").GetInt32()))
                Assert.That(journal.Snapshot.InFlightIndex,
                    Is.EqualTo(box (if expectedInFlight.ValueKind = JsonValueKind.Null then None else Some(expectedInFlight.GetInt32()))))
                Assert.That(journal.Snapshot.InterruptionCount, Is.EqualTo(vector.GetProperty("expectedInterruptions").GetInt32()))
                Assert.That(journal.Snapshot.RetryCount, Is.EqualTo(vector.GetProperty("expectedRetries").GetInt32())))
