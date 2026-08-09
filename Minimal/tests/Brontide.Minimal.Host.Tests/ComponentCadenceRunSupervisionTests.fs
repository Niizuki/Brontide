namespace Brontide.Minimal.Host.Tests

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open Brontide.Minimal.Host

type private SupervisionTemporaryJournal() =
    let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi69-{Guid.NewGuid():N}")
    member _.Path = Path.Combine(root, "cadence.bin")
    interface IDisposable with
        // A holder process that has reported exit may not have had its handles released yet, so the
        // first delete can still see the lock. The retry is a loop rather than a recursion: a lock
        // that is genuinely never released would otherwise leave 250 nested handler frames behind.
        member _.Dispose() =
            let mutable attempt = 0
            let mutable removed = false
            while not removed && attempt < 250 do
                try
                    if Directory.Exists root then Directory.Delete(root, true)
                    removed <- true
                with
                | :? IOException -> Thread.Sleep 20
                | :? UnauthorizedAccessException -> removed <- true
                attempt <- attempt + 1

[<TestFixture>]
type ComponentCadenceRunSupervisionTests() =
    let multiple action = Assert.Multiple(Action action)
    let textValue (value: JsonElement) = value.GetString() |> Option.ofObj |> Option.defaultValue ""
    let runIdentity = ProviderTrustCadenceRunId.create "cadence-run.test.1"
    let start = DateTimeOffset(2026, 8, 9, 8, 0, 0, TimeSpan.Zero)
    let schedule = ProviderServingTrustCadenceSchedule.create 4 (TimeSpan.FromSeconds 60.0)

    let release (supervision: ProviderTrustCadenceRunSupervision) =
        (supervision :> IDisposable).Dispose()

    /// A cycle that counts its calls and can take an action from inside the call, which is the only
    /// window in which a competitor can reach a run whose attempt is still in flight.
    let countingCycle (calls: int ref) (during: unit -> unit) : ProviderServingTrustCycle =
        fun _ _ -> task {
            calls.Value <- calls.Value + 1
            during ()
            return
                { Code = ProviderServingTrustCycleCodes.Current
                  Poll = None; Sweep = None; ServingCount = 0; Rotation = None; Availability = None }
        }

    let immediateDelay: ProviderServingTrustCadenceDelay =
        fun now duration _ -> Task.FromResult(now + duration)

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi69-cadence-run-supervision-vectors.json")))

    let vectorNamed (document: JsonDocument) name =
        document.RootElement.GetProperty("vectors").EnumerateArray()
        |> Seq.find (fun vector -> textValue (vector.GetProperty "name") = name)

    let establishAt path = (DurableProviderTrustCadenceJournal.Establish(path, runIdentity, schedule, start)).Journal.Value

    let acquireAt path = ProviderTrustCadenceRunSupervision.Acquire(path, runIdentity)

    /// Advances the driving holder while a competitor tries to take the run from inside the cycle.
    /// Without a supervision the competitor opens the journal directly; with one it must acquire
    /// first, and the codes say which happened.
    let advanceAgainstCompetitor path run (driving: DurableProviderTrustCadenceJournal)
        (supervision: ProviderTrustCadenceRunSupervision option) = task {
        let codes = ResizeArray<string>()
        let calls = ref 0
        let cycle =
            countingCycle calls (fun () ->
                match supervision with
                | None ->
                    let taking = (DurableProviderTrustCadenceJournal.Open(path, run)).Journal.Value
                    codes.Add (taking.ResolveInterrupted ProviderTrustCadenceRecoveryDecision.Retry).Code
                | Some _ ->
                    let contended = ProviderTrustCadenceRunSupervision.Acquire(path, run)
                    codes.Add contended.Code
                    contended.Supervision |> Option.iter release)
        let! advanced =
            match supervision with
            | None -> ProviderTrustCadenceRecovery.advance driving cycle immediateDelay CancellationToken.None
            | Some held ->
                SupervisedProviderTrustCadenceRecovery.advance held driving cycle immediateDelay CancellationToken.None
        codes.Add advanced.Code
        return List.ofSeq codes, calls.Value
    }

    /// Runs one scripted sequence over a single journal under two supervisors. `driving` is the
    /// establishing holder, `competitor` is the one an open returns, and `a`/`b` are supervisions of
    /// the same run, so a vector can drive the excluded party and the current one independently.
    let runVector (fixtureRoot: JsonElement) (vector: JsonElement) = task {
        use temporary = new SupervisionTemporaryJournal()
        let path = temporary.Path
        let run = ProviderTrustCadenceRunId.create (textValue (fixtureRoot.GetProperty "runIdentity"))
        let vectorStart =
            DateTimeOffset.FromUnixTimeSeconds(fixtureRoot.GetProperty("startUnixSeconds").GetInt64())
        let interval = TimeSpan.FromSeconds(float (fixtureRoot.GetProperty("intervalSeconds").GetInt32()))
        let vectorSchedule =
            ProviderServingTrustCadenceSchedule.create
                (fixtureRoot.GetProperty("maximumCycles").GetInt32()) interval
        let supervisions = Dictionary<string, ProviderTrustCadenceRunSupervision>(StringComparer.Ordinal)
        let codes = ResizeArray<string>()
        let mutable driving: DurableProviderTrustCadenceJournal option = None
        let mutable competitor: DurableProviderTrustCadenceJournal option = None
        try
            for step in vector.GetProperty("steps").EnumerateArray() |> Seq.map textValue do
                let name = step.Substring(step.IndexOf ':' + 1)
                match step with
                | "establish" ->
                    let established =
                        DurableProviderTrustCadenceJournal.Establish(path, run, vectorSchedule, vectorStart)
                    driving <- established.Journal
                    codes.Add established.Code
                | "open" ->
                    let opened = DurableProviderTrustCadenceJournal.Open(path, run)
                    competitor <- opened.Journal
                    codes.Add opened.Code
                | "unsupervised-advance-against-a-competitor" ->
                    let! contended, _ = advanceAgainstCompetitor path run driving.Value None
                    codes.AddRange contended
                | _ when step.StartsWith("acquire:", StringComparison.Ordinal) ->
                    let acquired = ProviderTrustCadenceRunSupervision.Acquire(path, run)
                    // A refused acquisition returns nothing, so an earlier live supervision under the
                    // same name is kept rather than overwritten.
                    acquired.Supervision |> Option.iter (fun value -> supervisions[name] <- value)
                    codes.Add acquired.Code
                | _ when step.StartsWith("release:", StringComparison.Ordinal) ->
                    let releasing = supervisions[name]
                    release releasing
                    codes.Add(
                        if releasing.IsLive then "cadence-supervision-live"
                        else "cadence-supervision-released")
                | _ when step.StartsWith("advance:", StringComparison.Ordinal) ->
                    let! advanced =
                        SupervisedProviderTrustCadenceRecovery.advance
                            supervisions[name] driving.Value (countingCycle (ref 0) id) immediateDelay
                            CancellationToken.None
                    codes.Add advanced.Code
                | _ when step.StartsWith("supervised-advance-against-a-competitor:", StringComparison.Ordinal) ->
                    let! contended, _ =
                        advanceAgainstCompetitor path run driving.Value (Some supervisions[name])
                    codes.AddRange contended
                | _ when step.StartsWith("competitor:", StringComparison.Ordinal) ->
                    let code =
                        match name with
                        | "begin" -> (competitor.Value.BeginCycle None).Code
                        | "reconcile" ->
                            (competitor.Value.ResolveInterrupted ProviderTrustCadenceRecoveryDecision.Retry).Code
                        | value -> failwithf "Unknown competitor verb %s." value
                    codes.Add code
                | value -> failwithf "Unknown step %s." value

            // Reopening is the only way to read what the record actually retains: a holder's own
            // snapshot is its view, and the vector pins the durable one.
            let reopened = (DurableProviderTrustCadenceJournal.Open(path, run)).Journal.Value
            return
                String.Join(",", codes), reopened.OwnerEpoch, reopened.Snapshot.Cycles.Length,
                reopened.Snapshot.Phase
        finally
            for supervision in supervisions.Values do release supervision
    }

    let expectedOf (vector: JsonElement) =
        String.Join(",", vector.GetProperty("codes").EnumerateArray() |> Seq.map textValue),
        vector.GetProperty("finalEpoch").GetInt64(),
        vector.GetProperty("committedCycles").GetInt32(),
        textValue (vector.GetProperty "phase")

    let providerPath () =
        match Environment.GetEnvironmentVariable "BRONTIDE_MINIMAL_PROVIDER" with
        | null | "" ->
            Assert.Ignore "BRONTIDE_MINIMAL_PROVIDER does not name a built provider endpoint."
            ""
        | path when not (File.Exists path) ->
            Assert.Ignore "BRONTIDE_MINIMAL_PROVIDER does not name a built provider endpoint."
            ""
        | path -> Path.GetFullPath path

    let probe provider path = task {
        let start =
            ProcessStartInfo(
                provider, UseShellExecute = false, RedirectStandardInput = true,
                RedirectStandardOutput = true)
        start.ArgumentList.Add $"--probe-exclusive-file={path}"
        use process' =
            match Process.Start start |> Option.ofObj with
            | Some value -> value
            | None -> failwith "The provider probe process did not start."
        do! process'.WaitForExitAsync()
        return process'.ExitCode
    }

    [<Test>]
    member _.``CBI69 C7 minimal supervises one cadence run over the shared vectors``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = runVector document.RootElement vector
            Assert.That(actual, Is.EqualTo(expectedOf vector), textValue (vector.GetProperty "name"))
    }

    /// The exclusion is over the run, not over the caller: a second supervisor is refused whether it
    /// runs in this process or another one.
    [<Test>]
    member _.``CBI69 C1 one live supervisor excludes a second in this process``() =
        use temporary = new SupervisionTemporaryJournal()
        let journal = establishAt temporary.Path
        let before = File.ReadAllBytes temporary.Path
        let first = acquireAt temporary.Path
        let second = acquireAt temporary.Path
        release first.Supervision.Value
        multiple (fun () ->
            Assert.That(first.Code, Is.EqualTo "cadence-supervision-acquired")
            Assert.That(second.Code, Is.EqualTo "cadence-supervision-busy")
            Assert.That(second.Supervision, Is.EqualTo None)
            Assert.That(File.ReadAllBytes temporary.Path, Is.EqualTo(box before))
            Assert.That(journal.OwnerEpoch, Is.EqualTo 1L))

    /// The child process proves the operating system is doing the excluding rather than a field this
    /// process happens to hold.
    [<Test; Category("CrossProcess")>]
    member _.``CBI69 C1 one live supervisor excludes another process``() = task {
        let provider = providerPath ()
        use temporary = new SupervisionTemporaryJournal()
        establishAt temporary.Path |> ignore
        let lockPath = ProviderTrustCadenceRunSupervision.LockPathFor temporary.Path
        let held = acquireAt temporary.Path
        try
            let! refused = probe provider lockPath
            Assert.That(refused, Is.EqualTo 74)
        finally
            release held.Supervision.Value
        let! allowed = probe provider lockPath
        Assert.That(allowed, Is.EqualTo 0)
    }

    /// A supervisor holds a run it has not read. CBI68's C2 makes opening observe rather than claim,
    /// and exclusion must not undo that: the record's epoch is still moved only by a write.
    [<Test>]
    member _.``CBI69 C2 supervision excludes writers without claiming the run``() =
        use temporary = new SupervisionTemporaryJournal()
        // Acquired before the run exists at all, which is the ordering a host must use if the lock is
        // to cover establishment.
        let supervision = (acquireAt temporary.Path).Supervision.Value
        let established =
            DurableProviderTrustCadenceJournal.Establish(temporary.Path, runIdentity, schedule, start)
        let before = File.ReadAllBytes temporary.Path
        release supervision
        let again = acquireAt temporary.Path
        release again.Supervision.Value
        multiple (fun () ->
            Assert.That(established.Code, Is.EqualTo "durable-cadence-established")
            Assert.That(established.Journal.Value.OwnerEpoch, Is.EqualTo 1L)
            Assert.That(File.ReadAllBytes temporary.Path, Is.EqualTo(box before))
            Assert.That(again.Code, Is.EqualTo "cadence-supervision-acquired"))

    /// CBI54 pairs its lock with a durable epoch because CBI53 has none. This journal already
    /// publishes one, so the slice adds a lock and no state: the lock file is never read and never
    /// written, and the only record beside it is the journal itself.
    [<Test>]
    member _.``CBI69 C3 supervision publishes no state of its own``() =
        use temporary = new SupervisionTemporaryJournal()
        establishAt temporary.Path |> ignore
        let lockPath = ProviderTrustCadenceRunSupervision.LockPathFor temporary.Path
        // Bytes a durable record would have to read, planted where one would live. Nothing reads them,
        // so a supervisor finds them irrelevant and leaves them exactly as they were.
        File.WriteAllText(lockPath, "not state")
        let supervision = acquireAt temporary.Path
        release supervision.Supervision.Value
        multiple (fun () ->
            Assert.That(supervision.Code, Is.EqualTo "cadence-supervision-acquired")
            Assert.That(File.ReadAllText lockPath, Is.EqualTo "not state")
            Assert.That(
                DirectoryInfo(Path.GetDirectoryName(temporary.Path: string) |> string).GetFiles()
                |> Array.map _.Name
                |> Array.sortWith (fun left right -> String.CompareOrdinal(left, right)),
                Is.EqualTo(box [| "cadence.bin"; "cadence.bin.lock" |])))

    /// Releasing is idempotent and a released supervisor drives nothing.
    [<Test>]
    member _.``CBI69 C4 a released supervisor cannot drive the cadence``() = task {
        use temporary = new SupervisionTemporaryJournal()
        let journal = establishAt temporary.Path
        let supervision = (acquireAt temporary.Path).Supervision.Value
        release supervision
        release supervision
        let calls = ref 0
        let! advanced =
            SupervisedProviderTrustCadenceRecovery.advance
                supervision journal (countingCycle calls id) immediateDelay CancellationToken.None
        multiple (fun () ->
            Assert.That(supervision.IsLive, Is.False)
            Assert.That(supervision.IsCurrentFor journal, Is.False)
            Assert.That(advanced.Code, Is.EqualTo "cadence-supervision-required")
            Assert.That(calls.Value, Is.EqualTo 0, "the cycle must not run")
            Assert.That(journal.OwnerEpoch, Is.EqualTo 1L))
    }

    /// Acquiring is not a recovery. A run interrupted in flight stays interrupted for the next
    /// supervisor to reconcile through CBI48.
    [<Test>]
    member _.``CBI69 C4 acquiring resolves no interruption``() =
        use temporary = new SupervisionTemporaryJournal()
        let journal = establishAt temporary.Path
        let first = (acquireAt temporary.Path).Supervision.Value
        journal.BeginCycle None |> ignore
        release first
        let second = acquireAt temporary.Path
        let reopened = DurableProviderTrustCadenceJournal.Open(temporary.Path, runIdentity)
        release second.Supervision.Value
        multiple (fun () ->
            Assert.That(second.Code, Is.EqualTo "cadence-supervision-acquired")
            Assert.That(reopened.Code, Is.EqualTo "durable-cadence-indeterminate")
            Assert.That(reopened.Journal.Value.Snapshot.Phase, Is.EqualTo "in-flight"))

    /// The two guards cover different holders. What the lock cannot exclude — a holder that opened
    /// before supervision existed — the fence still refuses at its next write; what the fence cannot
    /// catch in time is the competitor that reconciles a run while its cycle is still executing, and
    /// the same scenario is run both ways here.
    [<Test>]
    member _.``CBI69 C5 the lock and the fence cover different holders``() = task {
        use unsupervised = new SupervisionTemporaryJournal()
        let driving = establishAt unsupervised.Path
        let! lostCodes, lostCalls =
            advanceAgainstCompetitor unsupervised.Path runIdentity driving None
        let afterLoss = (DurableProviderTrustCadenceJournal.Open(unsupervised.Path, runIdentity)).Journal.Value

        use supervised = new SupervisionTemporaryJournal()
        let held = (acquireAt supervised.Path).Supervision.Value
        let kept = establishAt supervised.Path
        let! keptCodes, _ = advanceAgainstCompetitor supervised.Path runIdentity kept (Some held)
        release held

        multiple (fun () ->
            Assert.That(lostCalls, Is.EqualTo 1, "the cycle ran")
            Assert.That(
                lostCodes,
                Is.EqualTo(box [ "durable-cadence-retry-ready"; "durable-cadence-owner-superseded" ]),
                "and the run was lost only afterwards")
            Assert.That(afterLoss.Snapshot.Cycles, Is.Empty, "so the record kept nothing of it")
            Assert.That(
                keptCodes,
                Is.EqualTo(box [ "cadence-supervision-busy"; "durable-cadence-cycle-committed" ]),
                "the same competitor never reaches the record under a lock"))
    }

    /// The fence is unchanged by supervision: a holder that was superseded is refused with the code
    /// CBI68 already produces, whether or not a lock is held over the run.
    [<Test>]
    member _.``CBI69 C5 supervision adds no code to the write path``() = task {
        use temporary = new SupervisionTemporaryJournal()
        let driving = establishAt temporary.Path
        let supervision = (acquireAt temporary.Path).Supervision.Value
        // An unsupervised holder is not excluded by a lock it never asked for, which is exactly the
        // case the fence exists for.
        (DurableProviderTrustCadenceJournal.Open(temporary.Path, runIdentity)).Journal.Value.BeginCycle None
        |> ignore
        let calls = ref 0
        let! advanced =
            SupervisedProviderTrustCadenceRecovery.advance
                supervision driving (countingCycle calls id) immediateDelay CancellationToken.None
        release supervision
        multiple (fun () ->
            Assert.That(advanced.Code, Is.EqualTo "durable-cadence-owner-superseded")
            Assert.That(calls.Value, Is.EqualTo 0))
    }

    /// CBI68's residual limits say two holders that interleave writes "fence each other alternately
    /// rather than one winning permanently". They do not. A refused transition does not advance the
    /// refused holder's epoch, so the loser stays behind for good while the winner keeps writing; only
    /// a host that reopens rejoins, and reopening is a decision it has to make. That makes the
    /// unsupervised outcome a silent, permanent transfer rather than contention a host would notice.
    [<Test>]
    member _.``CBI69 a fenced holder stays fenced rather than alternating``() =
        use temporary = new SupervisionTemporaryJournal()
        let winner = establishAt temporary.Path
        let loser = (DurableProviderTrustCadenceJournal.Open(temporary.Path, runIdentity)).Journal.Value
        let began = winner.BeginCycle None
        let refused = loser.CommitCycle ProviderServingTrustCycleCodes.Current
        let committed = winner.CommitCycle ProviderServingTrustCycleCodes.Current
        let refusedAgain = loser.BeginCycle None
        multiple (fun () ->
            Assert.That(began.Code, Is.EqualTo "durable-cadence-cycle-started")
            Assert.That(refused.Code, Is.EqualTo "durable-cadence-owner-superseded")
            Assert.That(committed.Code, Is.EqualTo "durable-cadence-cycle-committed")
            Assert.That(refusedAgain.Code, Is.EqualTo "durable-cadence-owner-superseded"))

    /// A supervision is bound to the run and the path it locks. Pairing it with a journal it does not
    /// cover would advance a cadence behind a lock that excludes nobody from it, so it refuses.
    [<Test>]
    member _.``CBI69 C6 supervision is bound to the run and path it names``() = task {
        use supervised = new SupervisionTemporaryJournal()
        use other = new SupervisionTemporaryJournal()
        let supervision = (acquireAt supervised.Path).Supervision.Value
        let elsewhere = establishAt other.Path
        let otherRun = ProviderTrustCadenceRunId.create "cadence-run.test.2"
        let otherIdentity =
            (DurableProviderTrustCadenceJournal.Establish(supervised.Path, otherRun, schedule, start)).Journal.Value
        let calls = ref 0
        let! wrongPath =
            SupervisedProviderTrustCadenceRecovery.advance
                supervision elsewhere (countingCycle calls id) immediateDelay CancellationToken.None
        let! wrongRun =
            SupervisedProviderTrustCadenceRecovery.advance
                supervision otherIdentity (countingCycle calls id) immediateDelay CancellationToken.None
        release supervision
        multiple (fun () ->
            Assert.That(supervision.RunIdentity, Is.EqualTo runIdentity)
            Assert.That(wrongPath.Code, Is.EqualTo "cadence-supervision-required")
            Assert.That(wrongRun.Code, Is.EqualTo "cadence-supervision-required")
            Assert.That(calls.Value, Is.EqualTo 0))
    }
