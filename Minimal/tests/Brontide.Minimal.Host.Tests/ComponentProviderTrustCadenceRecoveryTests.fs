namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open Brontide.Minimal.Host

type private Cbi48TemporaryJournal() =
    let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi48-{Guid.NewGuid():N}")
    member _.Path = Path.Combine(root, "cadence.bin")
    interface IDisposable with
        member _.Dispose() =
            try if Directory.Exists root then Directory.Delete(root, true)
            with :? IOException | :? UnauthorizedAccessException -> ()

[<TestFixture>]
type ComponentProviderTrustCadenceRecoveryTests() =
    let runIdentity = ProviderTrustCadenceRunId.create "cadence-run.test.1"
    let schedule = ProviderServingTrustCadenceSchedule.create 2 (TimeSpan.FromSeconds 5.0)
    let start = DateTimeOffset(2026, 8, 5, 8, 0, 0, TimeSpan.Zero)
    let cycleResult code : ProviderServingTrustCycleResult =
        { Code = code
          Poll = None
          Sweep = None
          ServingCount = 0
          Rotation = None
          Availability = None }
    let establish path =
        DurableProviderTrustCadenceJournal.Establish(path, runIdentity, schedule, start).Journal.Value
    let multiple action = Assert.Multiple(Action action)

    [<Test>]
    member _.``CBI48 C1 a durable run is bounded and distinctly identified``() =
        use temporary = new Cbi48TemporaryJournal()
        let established =
            DurableProviderTrustCadenceJournal.Establish(temporary.Path, runIdentity, schedule, start)
        let duplicate =
            DurableProviderTrustCadenceJournal.Establish(temporary.Path, runIdentity, schedule, start)
        let mismatch = DurableProviderTrustCadenceJournal.Open(
            temporary.Path, ProviderTrustCadenceRunId.create "cadence-run.other")
        multiple (fun () ->
            Assert.That(established.Code, Is.EqualTo "durable-cadence-established")
            Assert.That(established.Journal.Value.Snapshot.RunIdentity, Is.EqualTo runIdentity)
            Assert.That(duplicate.Code, Is.EqualTo "durable-cadence-already-exists")
            Assert.That(mismatch.Code, Is.EqualTo "durable-cadence-run-mismatch")
            Assert.Throws<ArgumentException>(Action(fun () ->
                DurableProviderTrustCadenceJournal.Open(
                    temporary.Path, Unchecked.defaultof<ProviderTrustCadenceRunId>) |> ignore))
            |> ignore)

    [<Test>]
    member _.``CBI48 C2 every transition is atomic and integrity checked``() =
        use temporary = new Cbi48TemporaryJournal()
        let journal = establish temporary.Path
        Assert.That(journal.BeginCycle().Code, Is.EqualTo "durable-cadence-cycle-started")
        let bytes = File.ReadAllBytes temporary.Path
        bytes[bytes.Length - 1] <- bytes[bytes.Length - 1] ^^^ 0xffuy
        File.WriteAllBytes(temporary.Path, bytes)
        Assert.That(DurableProviderTrustCadenceJournal.Open(temporary.Path, runIdentity).Code,
            Is.EqualTo "durable-cadence-corrupt")

    [<Test>]
    member _.``CBI48 C3 in flight state precedes the effectful cycle``() = task {
        use temporary = new Cbi48TemporaryJournal()
        let journal = establish temporary.Path
        let mutable calls = 0
        let mutable observed = ""
        let cycle: ProviderServingTrustCycle = fun _ _ ->
            calls <- calls + 1
            observed <- DurableProviderTrustCadenceJournal.Open(temporary.Path, runIdentity).Code
            Task.FromResult(cycleResult "provider-trust-cycle-stopped")
        let delay: ProviderServingTrustCadenceDelay = fun now duration _ -> Task.FromResult(now + duration)
        let! _ = ProviderTrustCadenceRecovery.advance journal cycle delay CancellationToken.None
        multiple (fun () ->
            Assert.That(observed, Is.EqualTo "durable-cadence-indeterminate")
            Assert.That(calls, Is.EqualTo 1)
            Assert.That(journal.Snapshot.Code, Is.EqualTo "durable-cadence-stopped"))

        use failedWriteTemporary = new Cbi48TemporaryJournal()
        let failedWrite = establish failedWriteTemporary.Path
        File.Delete failedWriteTemporary.Path
        Directory.CreateDirectory failedWriteTemporary.Path |> ignore
        let mutable forbiddenCalls = 0
        let forbiddenCycle: ProviderServingTrustCycle = fun _ _ ->
            forbiddenCalls <- forbiddenCalls + 1
            Task.FromResult(cycleResult "provider-trust-cycle-current")
        let! refused =
            ProviderTrustCadenceRecovery.advance failedWrite forbiddenCycle delay CancellationToken.None
        multiple (fun () ->
            Assert.That(refused.Code, Is.EqualTo "durable-cadence-write-failed")
            Assert.That(forbiddenCalls, Is.Zero)) }

    [<Test>]
    member _.``CBI48 C4 completed work resumes from the next clean boundary``() = task {
        use temporary = new Cbi48TemporaryJournal()
        let first = establish temporary.Path
        let mutable firstCalls = 0
        let firstCycle: ProviderServingTrustCycle = fun _ _ ->
            firstCalls <- firstCalls + 1
            Task.FromResult(cycleResult "provider-trust-cycle-current")
        let noDelay: ProviderServingTrustCadenceDelay = fun now duration _ -> Task.FromResult(now + duration)
        let! _ = ProviderTrustCadenceRecovery.advance first firstCycle noDelay CancellationToken.None

        let waitingBytes = File.ReadAllBytes temporary.Path
        use canceled = new CancellationTokenSource()
        canceled.Cancel()
        let mutable canceledCycleCalls = 0
        let mutable canceledDelayCalls = 0
        let canceledCycle: ProviderServingTrustCycle = fun _ _ ->
            canceledCycleCalls <- canceledCycleCalls + 1
            Task.FromResult(cycleResult "provider-trust-cycle-current")
        let canceledDelay: ProviderServingTrustCadenceDelay = fun now duration _ ->
            canceledDelayCalls <- canceledDelayCalls + 1
            Task.FromResult(now + duration)
        let! canceledResult =
            ProviderTrustCadenceRecovery.advance first canceledCycle canceledDelay canceled.Token
        multiple (fun () ->
            Assert.That(canceledResult.Code, Is.EqualTo "durable-cadence-wait-canceled")
            Assert.That(canceledCycleCalls, Is.Zero)
            Assert.That(canceledDelayCalls, Is.Zero)
            Assert.That(File.ReadAllBytes temporary.Path, Is.EqualTo(box waitingBytes)))

        let recovered = DurableProviderTrustCadenceJournal.Open(temporary.Path, runIdentity).Journal.Value
        let mutable secondCalls = 0
        let mutable delayCalls = 0
        let secondCycle: ProviderServingTrustCycle = fun _ _ ->
            secondCalls <- secondCalls + 1
            Task.FromResult(cycleResult "provider-trust-cycle-current")
        let delay: ProviderServingTrustCadenceDelay = fun now duration _ ->
            delayCalls <- delayCalls + 1
            Task.FromResult(now + duration)
        let! _ = ProviderTrustCadenceRecovery.advance recovered secondCycle delay CancellationToken.None
        multiple (fun () ->
            Assert.That(firstCalls, Is.EqualTo 1)
            Assert.That(secondCalls, Is.EqualTo 1)
            Assert.That(delayCalls, Is.EqualTo 1)
            Assert.That(recovered.Snapshot.Code, Is.EqualTo "durable-cadence-complete")
            Assert.That(recovered.Snapshot.Cycles |> List.map _.Index, Is.EqualTo(box [ 0; 1 ]))) }

    [<Test>]
    member _.``CBI48 C5 an interrupted effect is indeterminate and inert``() = task {
        use temporary = new Cbi48TemporaryJournal()
        let journal = establish temporary.Path
        journal.BeginCycle() |> ignore
        let before = File.ReadAllBytes temporary.Path
        let recovered = DurableProviderTrustCadenceJournal.Open(temporary.Path, runIdentity)
        let mutable cycleCalls = 0
        let mutable delayCalls = 0
        let cycle: ProviderServingTrustCycle = fun _ _ ->
            cycleCalls <- cycleCalls + 1
            Task.FromResult(cycleResult "provider-trust-cycle-current")
        let delay: ProviderServingTrustCadenceDelay = fun now duration _ ->
            delayCalls <- delayCalls + 1
            Task.FromResult(now + duration)
        let! result = ProviderTrustCadenceRecovery.advance recovered.Journal.Value cycle delay CancellationToken.None
        multiple (fun () ->
            Assert.That(recovered.Code, Is.EqualTo "durable-cadence-indeterminate")
            Assert.That(result.Code, Is.EqualTo "durable-cadence-indeterminate")
            Assert.That(cycleCalls, Is.Zero)
            Assert.That(delayCalls, Is.Zero)
            Assert.That(File.ReadAllBytes temporary.Path, Is.EqualTo(box before))) }

    [<Test>]
    member _.``CBI48 C6 retry or abandonment requires explicit reconciliation``() =
        use retryTemporary = new Cbi48TemporaryJournal()
        let retry = establish retryTemporary.Path
        let attempted = retry.BeginCycle().Snapshot
        let ready = retry.ResolveInterrupted ProviderTrustCadenceRecoveryDecision.Retry
        use abandonTemporary = new Cbi48TemporaryJournal()
        let abandon = establish abandonTemporary.Path
        abandon.BeginCycle() |> ignore
        let abandoned = abandon.ResolveInterrupted ProviderTrustCadenceRecoveryDecision.Abandon
        multiple (fun () ->
            Assert.That(ready.Code, Is.EqualTo "durable-cadence-retry-ready")
            Assert.That(ready.Snapshot.NextCycleIndex, Is.EqualTo attempted.NextCycleIndex)
            Assert.That(ready.Snapshot.PreparedInstant, Is.EqualTo attempted.PreparedInstant)
            Assert.That(ready.Snapshot.InterruptionCount, Is.EqualTo 1)
            Assert.That(ready.Snapshot.RetryCount, Is.EqualTo 1)
            Assert.That(abandoned.Code, Is.EqualTo "durable-cadence-abandoned")
            Assert.That(abandoned.Snapshot.InterruptionCount, Is.EqualTo 1))

    [<Test>]
    member _.``CBI48 C7 terminal recovery is idempotent and effect free``() = task {
        use temporary = new Cbi48TemporaryJournal()
        let journal = establish temporary.Path
        journal.BeginCycle() |> ignore
        journal.CommitCycle "provider-trust-cycle-stopped" |> ignore
        let before = File.ReadAllBytes temporary.Path
        let recovered = DurableProviderTrustCadenceJournal.Open(temporary.Path, runIdentity).Journal.Value
        let mutable cycleCalls = 0
        let mutable delayCalls = 0
        let cycle: ProviderServingTrustCycle = fun _ _ ->
            cycleCalls <- cycleCalls + 1
            Task.FromResult(cycleResult "provider-trust-cycle-current")
        let delay: ProviderServingTrustCadenceDelay = fun now duration _ ->
            delayCalls <- delayCalls + 1
            Task.FromResult(now + duration)
        let! advanced = ProviderTrustCadenceRecovery.advance recovered cycle delay CancellationToken.None
        let reconciled = recovered.ResolveInterrupted ProviderTrustCadenceRecoveryDecision.Retry
        multiple (fun () ->
            Assert.That(advanced.Code, Is.EqualTo "durable-cadence-stopped")
            Assert.That(reconciled.Code, Is.EqualTo "durable-cadence-stopped")
            Assert.That(cycleCalls, Is.Zero)
            Assert.That(delayCalls, Is.Zero)
            Assert.That(File.ReadAllBytes temporary.Path, Is.EqualTo(box before))) }

    [<Test>]
    member _.``CBI48 C8 minimal executes the shared recovery vectors``() =
        use fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi48-durable-cadence-vectors.json")))
        let textValue (value: JsonElement) = value.GetString() |> Option.ofObj |> Option.defaultValue ""
        for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
            use temporary = new Cbi48TemporaryJournal()
            let mutable journal = establish temporary.Path
            for actionElement in vector.GetProperty("actions").EnumerateArray() do
                let action = textValue actionElement
                if action.StartsWith("cycle:", StringComparison.Ordinal) then
                    journal.BeginCycle() |> ignore
                    journal.CommitCycle(action[6..]) |> ignore
                elif action = "gap" then
                    journal.CompleteGap(journal.Snapshot.PreparedInstant + schedule.Interval) |> ignore
                elif action = "crash" then journal.BeginCycle() |> ignore
                elif action = "reopen" then
                    journal <- DurableProviderTrustCadenceJournal.Open(temporary.Path, runIdentity).Journal.Value
                elif action = "retry" then
                    journal.ResolveInterrupted ProviderTrustCadenceRecoveryDecision.Retry |> ignore
                elif action = "abandon" then
                    journal.ResolveInterrupted ProviderTrustCadenceRecoveryDecision.Abandon |> ignore
                else Assert.Fail $"Unknown action {action}."
            let name = vector.GetProperty("name") |> textValue
            let snapshot = journal.Snapshot
            multiple (fun () ->
                Assert.That(snapshot.Code, Is.EqualTo(vector.GetProperty("expectedCode") |> textValue), name)
                Assert.That(snapshot.Phase, Is.EqualTo(vector.GetProperty("expectedPhase") |> textValue), name)
                Assert.That(snapshot.Cycles |> List.map _.Code, Is.EqualTo(box (
                    vector.GetProperty("expectedCycleCodes").EnumerateArray()
                    |> Seq.map textValue |> Seq.toList)), name)
                Assert.That(snapshot.Cycles |> List.map _.Instant, Is.EqualTo(box (
                    vector.GetProperty("expectedCycleInstants").EnumerateArray()
                    |> Seq.map _.GetDateTimeOffset() |> Seq.toList)), name)
                Assert.That(snapshot.Gaps |> List.map (fun gap -> int gap.TotalSeconds), Is.EqualTo(box (
                    vector.GetProperty("expectedGapsSeconds").EnumerateArray()
                    |> Seq.map _.GetInt32() |> Seq.toList)), name)
                Assert.That(snapshot.NextCycleIndex,
                    Is.EqualTo(vector.GetProperty("expectedNextCycle").GetInt32()), name)
                Assert.That(snapshot.InterruptionCount,
                    Is.EqualTo(vector.GetProperty("expectedInterruptions").GetInt32()), name)
                Assert.That(snapshot.RetryCount,
                    Is.EqualTo(vector.GetProperty("expectedRetries").GetInt32()), name))
