namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Brontide.Minimal.Host
open NUnit.Framework

[<TestFixture>]
type ComponentAvailabilityBaselineTests() =
    let multiple action = Assert.Multiple(Action action)

    let start = DateTimeOffset.FromUnixTimeSeconds 1786230000L
    let interval = TimeSpan.FromSeconds 60.0

    let textValue (value: JsonElement) = value.GetString() |> Option.ofObj |> Option.defaultValue ""

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi65-availability-baseline-vectors.json")))

    let vectorNamed (document: JsonDocument) name =
        document.RootElement.GetProperty("vectors").EnumerateArray()
        |> Seq.find (fun vector -> textValue (vector.GetProperty "name") = name)

    let instantOf index = start + interval * float (index: int)

    /// Builds the snapshot a vector describes. The instants are the ones a cadence on the shared
    /// schedule prepares, so a vector never names a time the journal could not have recorded.
    let snapshotOf (vector: JsonElement) : ProviderTrustCadenceJournalSnapshot =
        let cycles =
            vector.GetProperty("cycles").EnumerateArray()
            |> Seq.mapi (fun index code ->
                { Index = index; Instant = instantOf index; Code = textValue code }
                : ProviderTrustCadenceJournalCycle)
            |> List.ofSeq
        { RunIdentity = ProviderTrustCadenceRunId.create "cbi65-run"
          Code = "durable-cadence-established"
          Phase = "waiting"
          MaximumCycles = 8
          Interval = interval
          PreparedInstant = instantOf cycles.Length
          Cycles = cycles
          Gaps = List.replicate (max (cycles.Length - 1) 0) interval
          NextCycleIndex = cycles.Length
          InterruptionCount = 0
          RetryCount = 0
          Cursor = None }

    let expectedOf (vector: JsonElement) =
        let cycle = vector.GetProperty "baselineCycle"
        textValue (vector.GetProperty "code"),
        (if cycle.ValueKind = JsonValueKind.Null then None else Some(instantOf (cycle.GetInt32())))

    let journalAt path =
        let opened =
            DurableProviderTrustCadenceJournal.Establish(
                path,
                ProviderTrustCadenceRunId.create "cbi65-run",
                ProviderServingTrustCadenceSchedule.create 8 interval,
                start)
        opened.Journal.Value

    let deleteTree path = try Directory.Delete(path, true) with _ -> ()

    let poll code lastAttemptCode : ProviderPublisherTrustPolicyPollResult =
        { Code = code; LastAttemptCode = lastAttemptCode; Attempts = 1; Delays = []
          AppliedSequences = []; RetainedSequences = []; Current = None
          Floor = Unchecked.defaultof<ProviderPublisherTrustPolicyRecoveryFloor> }

    /// One availability-governed cycle over a scripted policy endpoint and an empty serving set, which
    /// keeps CBI49's decision and its deadline real without launching a provider. The returned list
    /// collects every availability observation the cycle reported.
    let cadenceOver (script: string list) (baseline: DateTimeOffset option) =
        let observed = ResizeArray<ProviderTrustCycleAvailability>()
        let mutable index = 0
        let policyCycle: ProviderPublisherTrustPolicyCycle =
            fun _ _ ->
                let name = script[min index (script.Length - 1)]
                if name = "current" then poll "policy-poll-current" (Some "policy-distribution-current")
                else poll "policy-poll-exhausted" (Some "policy-distribution-transport-failed")
                |> Task.FromResult
        let sweep: ProviderServingTrustSweepCycle = fun _ -> Task.FromResult None
        let enforce =
            ProviderAvailabilityTrustCycle.enforcement
                (ProviderTrustOfflinePolicy.create (TimeSpan.FromMinutes 5.0) (TimeSpan.FromMinutes 1.0))
                (fun _ -> Task.FromResult [])
                "offline availability withdrawn"
        let inner =
            ProviderAvailabilityTrustCycle.resume baseline
                (ProviderServingTrustCycle.create policyCycle sweep) enforce
        let cycle: ProviderServingTrustCycle =
            fun now cancellationToken -> task {
                let! result = inner now cancellationToken
                result.Availability |> Option.iter observed.Add
                index <- index + 1
                return result
            }
        cycle, observed

    /// Drives the journal the way a host does — begin, run, commit, complete the gap — so the recorded
    /// instants are the ones the cycles actually ran at.
    let advance (journal: DurableProviderTrustCadenceJournal) (cycle: ProviderServingTrustCycle) count =
        task {
            for _ in 1..count do
                let instant = journal.Snapshot.PreparedInstant
                Assert.That(journal.BeginCycle(None).Code, Is.EqualTo "durable-cadence-cycle-started")
                let! result = cycle instant CancellationToken.None
                let committed = journal.CommitCycle result.Code
                if committed.Snapshot.Phase = "waiting" then
                    journal.CompleteGap(instant + interval) |> ignore
        }

    let outage baseline instant =
        task {
            let cycle, observed = cadenceOver [ "transport" ] baseline
            let! _ = cycle instant CancellationToken.None
            return Seq.tryHead observed
        }

    [<Test>]
    member _.``CBI65 C8 minimal derives the shared availability baselines``() =
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let actual = ProviderTrustCadenceAvailabilityRecovery.derive (snapshotOf vector)
            Assert.That(
                (actual.Code, actual.Instant), Is.EqualTo(expectedOf vector),
                textValue (vector.GetProperty "name"))

    [<Test>]
    member _.``CBI65 C1 deriving a baseline writes nothing``() =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi65-{Guid.NewGuid():N}")
        let path = Path.Combine(root, "cadence.bin")
        try
            let journal = journalAt path
            Assert.That(journal.BeginCycle(None).Code, Is.EqualTo "durable-cadence-cycle-started")
            Assert.That(
                (journal.CommitCycle ProviderServingTrustCycleCodes.Current).Code,
                Is.EqualTo "durable-cadence-cycle-committed")
            let before = File.ReadAllBytes path
            let derived = ProviderTrustCadenceAvailabilityRecovery.derive journal.Snapshot
            // A refused derivation must be silent too, which a synthesised snapshot is the only way to
            // reach: the journal cannot produce the record that provokes it.
            let refused =
                ProviderTrustCadenceAvailabilityRecovery.derive
                    { journal.Snapshot with
                        Cycles =
                            [ { Index = 0; Instant = start
                                Code = ProviderServingTrustCycleCodes.Stopped } ] }
            multiple (fun () ->
                Assert.That(derived.Code, Is.EqualTo "cadence-baseline-derived")
                Assert.That(refused.Code, Is.EqualTo "cadence-baseline-observation-invalid")
                Assert.That(File.ReadAllBytes path, Is.EqualTo(box before)))
        finally
            deleteTree root

    [<Test>]
    member _.``CBI65 C2 the vocabulary answers for every code it holds``() =
        use document = fixture ()
        let classification = document.RootElement.GetProperty "classification"
        multiple (fun () ->
            for code in ProviderServingTrustCycleCodes.all do
                let expected = classification.GetProperty code
                let answer = ProviderServingTrustCycleCodes.establishes code
                Assert.That(
                    answer,
                    Is.EqualTo(
                        if expected.ValueKind = JsonValueKind.Null then None
                        else Some(expected.GetBoolean())),
                    code)
                // Every code the vocabulary answers for is one a cadence may continue after, which is
                // what makes the unanswered ones unreachable in a record CBI48 wrote.
                if Option.isSome answer then
                    Assert.That(ProviderServingTrustCycleCodes.continues code, Is.True, code))

    /// The derivation reproduces the instant the live cadence held rather than a value that merely
    /// looks plausible: the same run is executed against a real journal and the two are compared.
    [<Test>]
    member _.``CBI65 C2 a replayed run yields the baseline the live cadence held``() = task {
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi65-{Guid.NewGuid():N}")
        try
            let journal = journalAt (Path.Combine(root, "cadence.bin"))
            let cycle, observed = cadenceOver [ "current"; "current"; "transport"; "transport" ] None
            do! advance journal cycle 4
            let derived = ProviderTrustCadenceAvailabilityRecovery.derive journal.Snapshot
            multiple (fun () ->
                // The live cadence anchored its deadline on the second current cycle.
                Assert.That(derived.Instant, Is.EqualTo(Some(instantOf 1)))
                Assert.That(observed, Has.Count.EqualTo 2)
                for availability in observed do
                    Assert.That(
                        availability.Deadline,
                        Is.EqualTo(derived.Instant |> Option.map (fun value -> value.AddMinutes 5.0))))
        finally
            deleteTree root }

    [<Test>]
    member _.``CBI65 C3 the baseline does not depend on the run or its terminal code``() =
        use document = fixture ()
        let snapshot = snapshotOf (vectorNamed document "an-outage-does-not-move-it")
        let ended =
            { snapshot with
                RunIdentity = ProviderTrustCadenceRunId.create "cbi65-some-earlier-run"
                Phase = "terminal"
                Code = "durable-cadence-complete" }
        multiple (fun () ->
            // A host that shut down cleanly holds the same fact as one that crashed; refusing the
            // completed run would stop service at its first outage for no gain.
            Assert.That(
                ProviderTrustCadenceAvailabilityRecovery.derive ended,
                Is.EqualTo(ProviderTrustCadenceAvailabilityRecovery.derive snapshot))
            Assert.That(
                (ProviderTrustCadenceAvailabilityRecovery.derive ended).Code,
                Is.EqualTo "cadence-baseline-derived"))

    [<Test>]
    member _.``CBI65 C4 a record with no establishing cycle yields no instant``() =
        use document = fixture ()
        for name in [ "a-run-that-never-reached-the-endpoint-has-none"; "an-empty-record-has-none" ] do
            let actual =
                ProviderTrustCadenceAvailabilityRecovery.derive (snapshotOf (vectorNamed document name))
            multiple (fun () ->
                Assert.That(actual.Code, Is.EqualTo("cadence-baseline-absent"), name)
                Assert.That(actual.Instant, Is.EqualTo(None: DateTimeOffset option), name))

    [<Test>]
    member _.``CBI65 C5 an attempt in flight changes no derivation``() =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi65-{Guid.NewGuid():N}")
        try
            let journal = journalAt (Path.Combine(root, "cadence.bin"))
            journal.BeginCycle None |> ignore
            journal.CommitCycle ProviderServingTrustCycleCodes.Current |> ignore
            journal.CompleteGap(start + interval) |> ignore
            let committed = ProviderTrustCadenceAvailabilityRecovery.derive journal.Snapshot
            Assert.That(journal.BeginCycle(None).Code, Is.EqualTo "durable-cadence-cycle-started")
            let inFlight = ProviderTrustCadenceAvailabilityRecovery.derive journal.Snapshot
            multiple (fun () ->
                Assert.That(journal.Snapshot.Phase, Is.EqualTo "in-flight")
                Assert.That(inFlight, Is.EqualTo committed))
        finally
            deleteTree root

    /// C6 claims CBI48 cannot place an unclassifiable observation in front of another. That is a claim
    /// about a dependency, so it is probed: every continuing code keeps the run going and the first
    /// non-continuing one ends it in the same write.
    [<Test>]
    member _.``CBI65 C6 the journal never records an unclassifiable observation before another``() =
        for code in ProviderServingTrustCycleCodes.all do
            let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi65-{Guid.NewGuid():N}")
            try
                let journal = journalAt (Path.Combine(root, "cadence.bin"))
                journal.BeginCycle None |> ignore
                let committed = journal.CommitCycle code
                let unanswered = Option.isNone (ProviderServingTrustCycleCodes.establishes code)
                let expected =
                    if unanswered then "cadence-baseline-observation-invalid"
                    elif ProviderServingTrustCycleCodes.establishes code = Some true then
                        "cadence-baseline-derived"
                    else "cadence-baseline-absent"
                multiple (fun () ->
                    Assert.That(
                        committed.Snapshot.Phase,
                        Is.EqualTo(if unanswered then "terminal" else "waiting"), code)
                    // A terminal journal accepts nothing further, so no later observation can follow
                    // the one the derivation could not classify.
                    if unanswered then
                        Assert.That(journal.BeginCycle(None).Code, Does.StartWith "durable-cadence-", code)
                    Assert.That(
                        (ProviderTrustCadenceAvailabilityRecovery.derive journal.Snapshot).Code,
                        Is.EqualTo(expected), code))
            finally
                deleteTree root

    /// The composed effect. Three successors run the same outage cycle at the same instant and differ
    /// only in the baseline they start from: the derived one, none at all, and the restart instant —
    /// the tempting wrong answer, which renews grace on every restart so a crash loop never expires.
    [<Test>]
    member _.``CBI65 C7 a resumed cadence continues the outage it was in``() = task {
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi65-{Guid.NewGuid():N}")
        try
            let journal = journalAt (Path.Combine(root, "cadence.bin"))
            let cycle, before = cadenceOver [ "current"; "transport"; "transport" ] None
            do! advance journal cycle 3
            let derived = ProviderTrustCadenceAvailabilityRecovery.derive journal.Snapshot
            let restart = journal.Snapshot.PreparedInstant
            let! resumed = outage derived.Instant restart
            let! none = outage None restart
            let! renewed = outage (Some restart) restart
            let interrupted = (Seq.last before).Deadline
            multiple (fun () ->
                // The outage the host was in is the outage it comes back to.
                Assert.That(resumed |> Option.bind _.Deadline, Is.EqualTo interrupted)
                Assert.That(resumed |> Option.bind _.DecisionCode, Is.EqualTo(Some "offline-idle"))
                // Without a baseline the run stops service instead, which is CBI64's stated limit and
                // what this slice removes.
                Assert.That(
                    none |> Option.bind _.DecisionCode,
                    Is.EqualTo(Some "offline-service-stop-required"))
                // Anchoring on the restart moves the deadline forward, so an outage spanning restarts
                // would never expire.
                Assert.That(renewed |> Option.bind _.Deadline |> Option.get, Is.GreaterThan(Option.get interrupted)))
        finally
            deleteTree root }
