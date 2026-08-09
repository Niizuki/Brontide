namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Text.Json
open Brontide.Minimal.Host
open NUnit.Framework

[<TestFixture>]
type ComponentCadenceRunOwnershipTests() =
    let multiple action = Assert.Multiple(Action action)

    let textValue (value: JsonElement) = value.GetString() |> Option.ofObj |> Option.defaultValue ""

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi68-cadence-run-ownership-vectors.json")))

    let deleteTree path = try Directory.Delete(path, true) with _ -> ()

    let newRoot () = Path.Combine(Path.GetTempPath(), $"brontide-cbi68-{Guid.NewGuid():N}")

    /// Runs one scripted sequence of holder actions over a single journal. `first` is the establishing
    /// holder and `second` is the one the first open returns, so a vector can drive a superseded holder
    /// and the current owner independently.
    let runVector (fixtureRoot: JsonElement) (vector: JsonElement) =
        let root = newRoot ()
        let path = Path.Combine(root, "cadence.bin")
        let run = ProviderTrustCadenceRunId.create (textValue (fixtureRoot.GetProperty "runIdentity"))
        let start =
            DateTimeOffset.FromUnixTimeSeconds(fixtureRoot.GetProperty("startUnixSeconds").GetInt64())
        let interval =
            TimeSpan.FromSeconds(float (fixtureRoot.GetProperty("intervalSeconds").GetInt32()))
        let schedule =
            ProviderServingTrustCadenceSchedule.create
                (fixtureRoot.GetProperty("maximumCycles").GetInt32()) interval
        try
            let mutable first: DurableProviderTrustCadenceJournal option = None
            let mutable second: DurableProviderTrustCadenceJournal option = None
            let codes = ResizeArray<string>()
            for step in vector.GetProperty("steps").EnumerateArray() |> Seq.map textValue do
                match step with
                | "establish" ->
                    let established =
                        DurableProviderTrustCadenceJournal.Establish(path, run, schedule, start)
                    first <- established.Journal
                    codes.Add established.Code
                | "open" ->
                    let opened = DurableProviderTrustCadenceJournal.Open(path, run)
                    if Option.isNone second then second <- opened.Journal
                    codes.Add opened.Code
                | other ->
                    let holder = if other.StartsWith("first:", StringComparison.Ordinal) then first.Value else second.Value
                    let verb = other.Substring(other.IndexOf ':' + 1)
                    let code =
                        match verb with
                        | "begin" -> (holder.BeginCycle None).Code
                        | "commit" -> (holder.CommitCycle ProviderServingTrustCycleCodes.Current).Code
                        | "gap" -> (holder.CompleteGap(holder.Snapshot.PreparedInstant + interval)).Code
                        | value -> failwithf "Unknown step verb %s." value
                    codes.Add code
            // Reopening is the only way to read what the record actually retains, which is the point:
            // a holder's own snapshot is its view, and the vector pins the durable one.
            let reopened = (DurableProviderTrustCadenceJournal.Open(path, run)).Journal.Value
            String.Join(",", codes), reopened.OwnerEpoch, reopened.Snapshot.Cycles.Length,
            reopened.Snapshot.Phase
        finally
            deleteTree root

    let expectedOf (vector: JsonElement) =
        String.Join(",",
            vector.GetProperty("codes").EnumerateArray() |> Seq.map textValue),
        vector.GetProperty("finalEpoch").GetInt64(),
        vector.GetProperty("committedCycles").GetInt32(),
        textValue (vector.GetProperty "phase")

    let vectorNamed (document: JsonDocument) name =
        document.RootElement.GetProperty("vectors").EnumerateArray()
        |> Seq.find (fun vector -> textValue (vector.GetProperty "name") = name)

    let establishAt path run start =
        (DurableProviderTrustCadenceJournal.Establish(
            path, run, ProviderServingTrustCadenceSchedule.create 4 (TimeSpan.FromSeconds 60.0),
            start)).Journal.Value

    [<Test>]
    member _.``CBI68 C7 minimal fences a superseded holder over the shared vectors``() =
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            Assert.That(
                runVector document.RootElement vector, Is.EqualTo(expectedOf vector),
                textValue (vector.GetProperty "name"))

    [<Test>]
    member _.``CBI68 C1 every write advances the epoch``() =
        use document = fixture ()
        let _, finalEpoch, _, _ =
            runVector document.RootElement
                (vectorNamed document "establishing-owns-and-each-write-advances-the-epoch")
        // Establish, begin, commit: three writes, so a holder that saw any earlier one is detectable.
        Assert.That(finalEpoch, Is.EqualTo 3L)

    /// Opening is how a host inspects a run, so it must not take it. CBI48's C3 opens a journal from
    /// inside a running cycle and then expects the driving holder to commit; its C5 and C7 compare the
    /// durable bytes across a recovery.
    [<Test>]
    member _.``CBI68 C2 opening observes without taking the run``() =
        use document = fixture ()
        let observedCodes, _, _, _ =
            runVector document.RootElement (vectorNamed document "opening-observes-without-taking-the-run")
        let _, inertEpoch, _, _ =
            runVector document.RootElement (vectorNamed document "repeated-opening-is-inert")
        multiple (fun () ->
            Assert.That(observedCodes, Does.EndWith "durable-cadence-cycle-started")
            // Establish plus one open plus one open plus one begin: only two of those write.
            Assert.That(inertEpoch, Is.EqualTo 2L))

    /// The defect the slice exists for, in both orderings. A holder whose memory was behind used to
    /// write its stale copy back and erase a committed cycle; a holder superseded while current used to
    /// move the phase of a run it no longer owned.
    [<Test>]
    member _.``CBI68 C3 a superseded holder writes nothing``() =
        use document = fixture ()
        for name in
            [ "a-writing-holder-supersedes-the-one-that-was-driving"
              "a-holder-whose-memory-is-behind-erases-nothing" ] do
            let vector = vectorNamed document name
            let codes, _, cycles, phase = runVector document.RootElement vector
            multiple (fun () ->
                Assert.That(codes, Does.Contain "durable-cadence-owner-superseded", name)
                Assert.That(cycles, Is.EqualTo(vector.GetProperty("committedCycles").GetInt32()), name)
                Assert.That(phase, Is.EqualTo(textValue (vector.GetProperty "phase")), name))

    /// The guard lives at the top of each transition rather than only at the write, so a future
    /// transition that forgot it would report a phase precondition instead of the lost run.
    [<Test>]
    member _.``CBI68 C3 every transition reports the lost run rather than a phase``() =
        let root = newRoot ()
        let path = Path.Combine(root, "cadence.bin")
        let run = ProviderTrustCadenceRunId.create "cadence-run.test.1"
        let start = DateTimeOffset.FromUnixTimeSeconds 1786230000L
        try
            let first = establishAt path run start
            let second = (DurableProviderTrustCadenceJournal.Open(path, run)).Journal.Value
            second.BeginCycle None |> ignore
            let transitions =
                [ "BeginCycle", fun () -> (first.BeginCycle None).Code
                  "CommitCycle", fun () -> (first.CommitCycle ProviderServingTrustCycleCodes.Current).Code
                  "CompleteGap", fun () -> (first.CompleteGap(start + TimeSpan.FromSeconds 60.0)).Code
                  "ResolveInterrupted",
                    fun () -> (first.ResolveInterrupted ProviderTrustCadenceRecoveryDecision.Retry).Code ]
            multiple (fun () ->
                for name, act in transitions do
                    Assert.That(act (), Is.EqualTo("durable-cadence-owner-superseded"), name))
        finally
            deleteTree root

    [<Test>]
    member _.``CBI68 C4 a record written before this slice needs no adoption rule``() =
        let root = newRoot ()
        let path = Path.Combine(root, "cadence.bin")
        let run = ProviderTrustCadenceRunId.create "cadence-run.test.1"
        let start = DateTimeOffset.FromUnixTimeSeconds 1786230000L
        try
            establishAt path run start |> ignore
            // Rewrite the record without an owner epoch, which is what a pre-CBI68 host wrote.
            let bytes = File.ReadAllBytes path
            let json = Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length - 32)
            let stripped =
                match Nodes.JsonNode.Parse json with
                | null -> failwith "The journal record did not parse as JSON."
                | node -> node.AsObject()
            stripped.Remove "ownerEpoch" |> ignore
            let body = Text.Encoding.UTF8.GetBytes(stripped.ToJsonString())
            File.WriteAllBytes(path, Array.append body (Security.Cryptography.SHA256.HashData body))

            let opened = DurableProviderTrustCadenceJournal.Open(path, run)
            let seenBeforeWriting = opened.Journal.Value.OwnerEpoch
            let began = opened.Journal.Value.BeginCycle None
            let reopened = (DurableProviderTrustCadenceJournal.Open(path, run)).Journal.Value
            multiple (fun () ->
                // Nothing special happens: a record that never carried an epoch reads as zero, and the
                // first write claims it at one.
                Assert.That(opened.Code, Is.EqualTo "durable-cadence-recovered")
                Assert.That(seenBeforeWriting, Is.EqualTo 0L)
                Assert.That(began.Code, Is.EqualTo "durable-cadence-cycle-started")
                Assert.That(reopened.OwnerEpoch, Is.EqualTo 1L))
        finally
            deleteTree root

    /// An unreadable record cannot prove supersession, and CBI48 already defines what happens to a
    /// journal whose write path fails. The guard leaves that outcome alone rather than reclassifying it.
    [<Test>]
    member _.``CBI68 C5 an unreadable record keeps the outcome CBI48 already defines``() =
        let root = newRoot ()
        let path = Path.Combine(root, "cadence.bin")
        let run = ProviderTrustCadenceRunId.create "cadence-run.test.1"
        let start = DateTimeOffset.FromUnixTimeSeconds 1786230000L
        try
            let journal = establishAt path run start
            File.Delete path
            Directory.CreateDirectory path |> ignore
            Assert.That((journal.BeginCycle None).Code, Is.EqualTo "durable-cadence-write-failed")
        finally
            deleteTree root

    [<Test>]
    member _.``CBI68 C6 observation is not gated by ownership``() =
        let root = newRoot ()
        let path = Path.Combine(root, "cadence.bin")
        let run = ProviderTrustCadenceRunId.create "cadence-run.test.1"
        let start = DateTimeOffset.FromUnixTimeSeconds 1786230000L
        try
            let first = establishAt path run start
            (DurableProviderTrustCadenceJournal.Open(path, run)).Journal.Value.BeginCycle None |> ignore
            // A superseded holder can still say what it last knew, which is what a host needs in order
            // to report why it stopped. What it cannot do is make that view durable.
            multiple (fun () ->
                Assert.That(first.Snapshot.RunIdentity, Is.EqualTo run)
                Assert.That(first.Snapshot.Phase, Is.EqualTo "ready")
                Assert.That((first.BeginCycle None).Code, Is.EqualTo "durable-cadence-owner-superseded"))
        finally
            deleteTree root
