namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Text.Json
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi49TemporaryJournal() =
    let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi49-{Guid.NewGuid():N}")
    member _.Path = Path.Combine(root, "cadence.bin")
    interface IDisposable with
        member _.Dispose() =
            try if Directory.Exists root then Directory.Delete(root, true)
            with :? IOException | :? UnauthorizedAccessException -> ()

[<TestFixture>]
type ComponentProviderTrustOfflinePolicyTests() =
    let runIdentity = ProviderTrustCadenceRunId.create "cadence-run.cbi49"
    let start = DateTimeOffset(2026, 8, 5, 8, 0, 0, TimeSpan.Zero)
    let schedule = ProviderServingTrustCadenceSchedule.create 2 (TimeSpan.FromMinutes 1.0)

    let interrupted path =
        let journal =
            DurableProviderTrustCadenceJournal.Establish(path, runIdentity, schedule, start).Journal.Value
        Assert.That(journal.BeginCycle().Code, Is.EqualTo "durable-cadence-cycle-started")
        DurableProviderTrustCadenceJournal.Open(path, runIdentity).Journal.Value

    let policy () = ProviderTrustOfflinePolicy.create (TimeSpan.FromMinutes 5.0) (TimeSpan.FromMinutes 1.0)

    [<Test>]
    member _.``CBI49 C1 offline policy is explicit and bounded``() =
        Assert.Throws<ArgumentException>(Action(fun () ->
            ProviderTrustOfflinePolicy.create TimeSpan.Zero (TimeSpan.FromMinutes 1.0) |> ignore)) |> ignore
        Assert.Throws<ArgumentException>(Action(fun () ->
            ProviderTrustOfflinePolicy.create (TimeSpan.FromMinutes 1.0) (TimeSpan.FromMinutes 2.0) |> ignore)) |> ignore
        Assert.Throws<ArgumentException>(Action(fun () ->
            ProviderTrustOfflinePolicy.create (TimeSpan.FromHours 25.0) (TimeSpan.FromMinutes 1.0) |> ignore)) |> ignore
        let result = (policy()).Evaluate(
            start.AddMinutes 2.0, Some start, "policy-poll-exhausted", Some "policy-distribution-timeout", 1)
        let overflowing = (policy()).Evaluate(
            DateTimeOffset.MaxValue, Some(DateTimeOffset.MaxValue.AddMinutes -1.0),
            "policy-poll-exhausted", Some "policy-distribution-timeout", 1)
        Assert.Multiple(Action(fun () ->
            Assert.That(result.Deadline, Is.EqualTo(Some(start.AddMinutes 5.0)))
            Assert.That(result.RetryAt, Is.EqualTo(Some(start.AddMinutes 3.0)))
            Assert.That(overflowing.Code, Is.EqualTo "offline-observation-invalid")))

    [<Test>]
    member _.``CBI49 C2 only endpoint unavailability is grace eligible``() =
        let cases =
            [ "policy-poll-refused", "policy-distribution-endpoint-signature-invalid"
              "policy-poll-exhausted", "policy-distribution-stale"
              "policy-poll-exhausted", "policy-distribution-superseded"
              "policy-poll-canceled", "policy-distribution-canceled"
              "policy-poll-floor-unretained", "policy-distribution-update-applied" ]
        for pollCode, attemptCode in cases do
            let result = (policy()).Evaluate(start.AddMinutes 1.0, Some start, pollCode, Some attemptCode, 1)
            Assert.That(result.Code, Is.EqualTo "offline-service-stop-required", $"{pollCode}/{attemptCode}")
            Assert.That(result.MayContinueExistingService, Is.False)

    [<Test>]
    member _.``CBI49 C3 grace requires prior current and never refreshes it``() =
        let subject = policy ()
        let evaluate now baseline =
            subject.Evaluate(now, baseline, "policy-poll-exhausted", Some "policy-distribution-timeout", 1)
        let noBaseline = evaluate (start.AddMinutes 1.0) None
        let before = evaluate (start.AddMinutes 4.0) (Some start)
        let atDeadline = evaluate (start.AddMinutes 5.0) (Some start)
        let later = evaluate (start.AddMinutes 6.0) (Some start)
        Assert.Multiple(Action(fun () ->
            Assert.That(noBaseline.Code, Is.EqualTo "offline-service-stop-required")
            Assert.That(before.Code, Is.EqualTo "offline-existing-service")
            Assert.That(atDeadline.Code, Is.EqualTo "offline-grace-expired")
            Assert.That(later.Code, Is.EqualTo "offline-grace-expired")
            Assert.That(later.Deadline, Is.EqualTo before.Deadline)))

    [<Test>]
    member _.``CBI49 C4 offline continuation is existing service only``() =
        let subject = policy ()
        let evaluate count =
            subject.Evaluate(start.AddMinutes 2.0, Some start,
                "policy-poll-exhausted", Some "policy-distribution-transport-failed", count)
        let serving = evaluate 2
        let idle = evaluate 0
        Assert.Multiple(Action(fun () ->
            Assert.That(serving.MayContinueExistingService, Is.True)
            Assert.That(serving.MayStartProvider, Is.False)
            Assert.That(idle.Code, Is.EqualTo "offline-idle")
            Assert.That(idle.MayContinueExistingService, Is.False)
            Assert.That(idle.MayStartProvider, Is.False)))

    [<Test>]
    member _.``CBI49 C5 reconciliation evidence names the interrupted attempt exactly``() =
        use temporary = new Cbi49TemporaryJournal()
        let journal = interrupted temporary.Path
        let before = File.ReadAllBytes temporary.Path
        let evidence =
            { RunIdentity = runIdentity; AttemptIndex = 1; AttemptInstant = start
              Verdict = ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed }
        let result = ProviderTrustCadenceReconciliation.apply journal evidence
        Assert.Multiple(Action(fun () ->
            Assert.That(result.Code, Is.EqualTo "cadence-reconciliation-mismatch")
            Assert.That(result.Snapshot.Phase, Is.EqualTo "in-flight")
            Assert.That(Convert.ToHexString(File.ReadAllBytes temporary.Path), Is.EqualTo(Convert.ToHexString before))))

    [<Test>]
    member _.``CBI49 C6 unknown evidence leaves the interruption inert``() =
        use temporary = new Cbi49TemporaryJournal()
        let journal = interrupted temporary.Path
        let before = File.ReadAllBytes temporary.Path
        let evidence =
            { RunIdentity = runIdentity; AttemptIndex = 0; AttemptInstant = start
              Verdict = ProviderTrustCadenceReconciliationVerdict.Unknown }
        let result = ProviderTrustCadenceReconciliation.apply journal evidence
        Assert.Multiple(Action(fun () ->
            Assert.That(result.Code, Is.EqualTo "cadence-reconciliation-deferred")
            Assert.That(result.Snapshot.Phase, Is.EqualTo "in-flight")
            Assert.That(result.Snapshot.InterruptionCount, Is.Zero)
            Assert.That(Convert.ToHexString(File.ReadAllBytes temporary.Path), Is.EqualTo(Convert.ToHexString before))))

    [<Test>]
    member _.``CBI49 C7 conclusive evidence selects one CBI48 transition``() =
        use retryTemporary = new Cbi49TemporaryJournal()
        use abandonTemporary = new Cbi49TemporaryJournal()
        let retry = interrupted retryTemporary.Path
        let abandon = interrupted abandonTemporary.Path
        let evidence verdict =
            { RunIdentity = runIdentity; AttemptIndex = 0; AttemptInstant = start; Verdict = verdict }
        let retried = ProviderTrustCadenceReconciliation.apply retry (evidence ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed)
        let repeated = ProviderTrustCadenceReconciliation.apply retry (evidence ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed)
        let abandoned = ProviderTrustCadenceReconciliation.apply abandon (evidence ProviderTrustCadenceReconciliationVerdict.EffectsAccountedFor)
        Assert.Multiple(Action(fun () ->
            Assert.That(retried.Code, Is.EqualTo "cadence-reconciliation-retry-ready")
            Assert.That(retried.Snapshot.InterruptionCount, Is.EqualTo 1)
            Assert.That(retried.Snapshot.RetryCount, Is.EqualTo 1)
            Assert.That(repeated.Code, Is.EqualTo "cadence-reconciliation-not-required")
            Assert.That(repeated.Snapshot.InterruptionCount, Is.EqualTo 1)
            Assert.That(abandoned.Code, Is.EqualTo "cadence-reconciliation-abandoned")
            Assert.That(abandoned.Snapshot.InterruptionCount, Is.EqualTo 1)
            Assert.That(abandoned.Snapshot.RetryCount, Is.Zero)))

    [<Test>]
    member _.``CBI49 C8 minimal executes the shared policy model``() =
        let fixturePath = Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi49-offline-reconciliation-vectors.json")
        use document = JsonDocument.Parse(File.ReadAllText fixturePath)
        let root = document.RootElement
        let fixturePolicy = root.GetProperty "policy"
        let graceSeconds = (fixturePolicy.GetProperty "graceSeconds").GetDouble()
        let retrySeconds = (fixturePolicy.GetProperty "retrySeconds").GetDouble()
        let subject = ProviderTrustOfflinePolicy.create (TimeSpan.FromSeconds graceSeconds) (TimeSpan.FromSeconds retrySeconds)
        let fixtureLastCurrent = (fixturePolicy.GetProperty "lastCurrent").GetDateTimeOffset()
        let stringProperty (element: JsonElement) (name: string) : string =
            match (element.GetProperty name).GetString() with
            | null -> failwith $"Fixture property {name} is null."
            | value -> value
        for vector in (root.GetProperty "offlineVectors").EnumerateArray() do
            let id = stringProperty vector "id"
            let baseline =
                if id = "no-current-baseline" then None
                else
                    let mutable property = Unchecked.defaultof<JsonElement>
                    if vector.TryGetProperty("lastCurrent", &property) && property.ValueKind <> JsonValueKind.Null then
                        Some(property.GetDateTimeOffset())
                    else Some fixtureLastCurrent
            let lastAttempt =
                let mutable property = Unchecked.defaultof<JsonElement>
                if vector.TryGetProperty("lastAttemptCode", &property) && property.ValueKind <> JsonValueKind.Null then
                    property.GetString() |> Option.ofObj
                else None
            let result = subject.Evaluate(
                vector.GetProperty("now").GetDateTimeOffset(), baseline,
                stringProperty vector "pollCode", lastAttempt,
                vector.GetProperty("servingCount").GetInt32())
            let optionalInstant (name: string) =
                let property = vector.GetProperty name
                if property.ValueKind = JsonValueKind.Null then None else Some(property.GetDateTimeOffset())
            Assert.Multiple(Action(fun () ->
                Assert.That(result.Code, Is.EqualTo(stringProperty vector "expectedCode"), id)
                Assert.That(result.MayContinueExistingService,
                    Is.EqualTo(vector.GetProperty("continueExisting").GetBoolean()), id)
                Assert.That(result.MayStartProvider, Is.EqualTo(vector.GetProperty("mayStart").GetBoolean()), id)
                Assert.That(result.Deadline, Is.EqualTo(optionalInstant "deadline"), id)
                Assert.That(result.RetryAt, Is.EqualTo(optionalInstant "retryAt"), id)))

        for vector in (root.GetProperty "reconciliationVectors").EnumerateArray() do
            use temporary = new Cbi49TemporaryJournal()
            let journal = interrupted temporary.Path
            let verdict =
                match stringProperty vector "verdict" with
                | "no-effects-confirmed" -> ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed
                | "effects-accounted-for" -> ProviderTrustCadenceReconciliationVerdict.EffectsAccountedFor
                | _ -> ProviderTrustCadenceReconciliationVerdict.Unknown
            let mutable indexProperty = Unchecked.defaultof<JsonElement>
            let index =
                if vector.TryGetProperty("attemptIndex", &indexProperty) then indexProperty.GetInt32() else 0
            let evidence =
                { RunIdentity = runIdentity; AttemptIndex = index
                  AttemptInstant = start; Verdict = verdict }
            let result = ProviderTrustCadenceReconciliation.apply journal evidence
            let id = stringProperty vector "id"
            Assert.Multiple(Action(fun () ->
                Assert.That(result.Code, Is.EqualTo(stringProperty vector "expectedCode"), id)
                Assert.That(result.Snapshot.Phase, Is.EqualTo(stringProperty vector "expectedPhase"), id)
                Assert.That(result.Snapshot.InterruptionCount,
                    Is.EqualTo(vector.GetProperty("interruptions").GetInt32()), id)
                Assert.That(result.Snapshot.RetryCount, Is.EqualTo(vector.GetProperty("retries").GetInt32()), id)))
