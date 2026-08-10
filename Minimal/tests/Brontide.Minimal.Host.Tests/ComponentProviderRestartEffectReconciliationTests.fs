namespace Brontide.Minimal.Host.Tests

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open System.Threading.Tasks
open NUnit.Framework
open Brontide.Minimal.Experimental.ComponentManagement
open Brontide.Minimal.Host

type private RestartTemporaryEffect() =
    let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi55-{Guid.NewGuid():N}")
    member _.Root = root
    member _.JournalPath = Path.Combine(root, "restart.journal")
    member _.EffectPath = Path.Combine(root, "restart.effect")
    interface IDisposable with
        member _.Dispose() =
            let rec remove attempt =
                try if Directory.Exists root then Directory.Delete(root, true)
                with :? IOException when attempt < 249 ->
                    Threading.Thread.Sleep 20
                    remove (attempt + 1)
            remove 0

[<TestFixture>]
type ComponentProviderRestartEffectReconciliationTests() =
    let multiple action = Assert.Multiple(Action action)
    let runIdentity = ProviderRestartAttemptRunId.create "restart-run.effect.1"
    let occurrence = OccurrenceId.create "occ.def.test.cooling-provider.1"
    let staged = ProviderArtifactSetId.create (String('B', 64))
    let instant = DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.Zero)
    let providerExecutableName =
        if OperatingSystem.IsWindows() then "Brontide.Minimal.Interchange.Provider.exe"
        else "Brontide.Minimal.Interchange.Provider"

    let providerPath () =
        match Environment.GetEnvironmentVariable "BRONTIDE_MINIMAL_PROVIDER" |> Option.ofObj with
        | None -> Assert.Ignore "BRONTIDE_MINIMAL_PROVIDER does not name a built provider endpoint."; ""
        | Some path when not (File.Exists path) -> Assert.Ignore "BRONTIDE_MINIMAL_PROVIDER does not name a built provider endpoint."; ""
        | Some path -> Path.GetFullPath path

    let journal (temporary: RestartTemporaryEffect) =
        DurableProviderRestartAttemptJournal.Establish(
            temporary.JournalPath, runIdentity, occurrence, staged,
            ProviderRestartPolicy.create 2 (TimeSpan.FromMinutes 1.0)).Journal.Value

    let acquire (temporary: RestartTemporaryEffect) owner lease =
        DurableProviderRestartOwnership.Acquire(
            temporary.JournalPath, ProviderRestartOwnerId.create owner, ProviderRestartOwnershipLeaseId.create lease,
            runIdentity, occurrence, staged).Ownership.Value

    let prepare (temporary: RestartTemporaryEffect) epoch index attemptInstant =
        DurableProviderRestartEffect.Prepare(
            temporary.EffectPath, runIdentity, occurrence, staged, index, attemptInstant, epoch,
            ProviderRestartEffectToken.create "effect-token-1", providerExecutableName).Effect.Value

    let startProvider (effect: DurableProviderRestartEffect) =
        let start = ProcessStartInfo(providerPath(), UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true)
        effect.Environment |> Map.iter (fun key value -> start.Environment[key] <- value)
        match Process.Start start |> Option.ofObj with
        | Some child -> child
        | None -> failwith "The provider process did not start."

    let awaitReceipt (effect: DurableProviderRestartEffect) = task {
        let mutable attempt = 0
        while attempt < 250 && not (File.Exists effect.Snapshot.ReceiptPath) do
            do! Task.Delay 20
            attempt <- attempt + 1
        Assert.That(File.Exists effect.Snapshot.ReceiptPath, Is.True,
            "The provider did not publish its bounded CBI55 receipt.")
    }

    [<Test>]
    member _.``CBI55 C1 record binds the exact attempt and fence``() =
        use temporary = new RestartTemporaryEffect()
        let prepared = DurableProviderRestartEffect.Prepare(
            temporary.EffectPath, runIdentity, occurrence, staged, 0, instant, 7L,
            ProviderRestartEffectToken.create "effect-token-1", providerExecutableName)
        let exact = DurableProviderRestartEffect.Open(temporary.EffectPath, runIdentity, occurrence, staged)
        let mismatch = DurableProviderRestartEffect.Open(
            temporary.EffectPath, ProviderRestartAttemptRunId.create "restart-run.other", occurrence, staged)
        multiple (fun () ->
            Assert.That(prepared.Code, Is.EqualTo "restart-effect-prepared")
            Assert.That(exact.Snapshot, Is.EqualTo prepared.Snapshot)
            Assert.That(exact.Snapshot.Value.AttemptIndex, Is.Zero)
            Assert.That(exact.Snapshot.Value.FencingEpoch, Is.EqualTo 7L)
            Assert.That(mismatch.Code, Is.EqualTo "restart-effect-lineage-mismatch"))

    [<Test>]
    member _.``CBI55 C2 record and provider facts precede the in flight transition``() =
        use temporary = new RestartTemporaryEffect()
        let attemptJournal = journal temporary
        use owner = acquire temporary "owner-a" "lease-a"
        let effect = prepare temporary owner.Snapshot.Epoch 0 instant
        multiple (fun () ->
            Assert.That(File.Exists temporary.EffectPath, Is.True)
            Assert.That(effect.Environment.ContainsKey "BRONTIDE_RESTART_EFFECT_LEASE", Is.True)
            Assert.That(attemptJournal.Snapshot.Phase, Is.EqualTo "ready"))
        Assert.That(attemptJournal.BeginAttempt(instant).Code, Is.EqualTo "durable-restart-attempt-started")

    [<Test; Category("CrossProcess")>]
    member _.``CBI55 C3 provider holds the token lease and writes its receipt``() = task {
        use temporary = new RestartTemporaryEffect()
        use owner = acquire temporary "owner-a" "lease-a"
        let effect = prepare temporary owner.Snapshot.Epoch 0 instant
        use provider = startProvider effect
        try
            do! awaitReceipt effect
            Assert.Throws<IOException>(Action(fun () ->
                use _ = new FileStream(effect.Snapshot.LeasePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)
                ())) |> ignore
        finally
            if not provider.HasExited then provider.Kill true
            provider.WaitForExit()
    }

    [<Test>]
    member _.``CBI55 C4 a free lease proves retry is safe``() = task {
        use temporary = new RestartTemporaryEffect()
        let attemptJournal = journal temporary
        let first = acquire temporary "owner-a" "lease-a"
        prepare temporary first.Snapshot.Epoch 0 instant |> ignore
        Assert.That(attemptJournal.BeginAttempt(instant).Code, Is.EqualTo "durable-restart-attempt-started")
        (first :> IDisposable).Dispose()
        use successor = acquire temporary "owner-b" "lease-b"
        let! result = ExternallyReconciledProviderRestartRecovery.reconcile successor attemptJournal temporary.EffectPath
        multiple (fun () ->
            Assert.That(result.Code, Is.EqualTo "restart-effect-no-live-provider")
            Assert.That(result.LeaseAvailable, Is.True)
            Assert.That(result.Journal.Phase, Is.EqualTo "ready")
            Assert.That(result.Journal.RetryCount, Is.EqualTo 1))
    }

    [<Test; Category("CrossProcess")>]
    member _.``CBI55 C5 an exact orphan is terminated before retry``() = task {
        use temporary = new RestartTemporaryEffect()
        let attemptJournal = journal temporary
        let first = acquire temporary "owner-a" "lease-a"
        let effect = prepare temporary first.Snapshot.Epoch 0 instant
        Assert.That(attemptJournal.BeginAttempt(instant).Code, Is.EqualTo "durable-restart-attempt-started")
        use provider = startProvider effect
        do! awaitReceipt effect
        (first :> IDisposable).Dispose()
        use successor = acquire temporary "owner-b" "lease-b"
        let! result = ExternallyReconciledProviderRestartRecovery.reconcile successor attemptJournal temporary.EffectPath
        do! provider.WaitForExitAsync()
        multiple (fun () ->
            Assert.That(result.Code, Is.EqualTo "restart-effect-provider-terminated")
            Assert.That(result.ProcessTerminated, Is.True)
            Assert.That(result.Journal.Phase, Is.EqualTo "ready"))
    }

    [<Test; Category("CrossProcess")>]
    member _.``CBI55 C6 uncertain evidence remains in flight``() = task {
        use temporary = new RestartTemporaryEffect()
        let attemptJournal = journal temporary
        let first = acquire temporary "owner-a" "lease-a"
        let effect = prepare temporary first.Snapshot.Epoch 0 instant
        attemptJournal.BeginAttempt instant |> ignore
        let start = ProcessStartInfo(providerPath(), UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true)
        start.ArgumentList.Add($"--hold-exclusive-file={effect.Snapshot.LeasePath}")
        use holder = Process.Start start |> Option.ofObj |> Option.defaultWith (fun () -> failwith "The holder process did not start.")
        let! held = holder.StandardOutput.ReadLineAsync()
        Assert.That(held, Is.EqualTo "held")
        (first :> IDisposable).Dispose()
        use successor = acquire temporary "owner-b" "lease-b"
        let! result = ExternallyReconciledProviderRestartRecovery.reconcile successor attemptJournal temporary.EffectPath
        holder.Kill true
        do! holder.WaitForExitAsync()
        multiple (fun () ->
            Assert.That(result.Code, Is.EqualTo "restart-effect-reconciliation-deferred")
            Assert.That(result.Journal.Phase, Is.EqualTo "in-flight")
            Assert.That(result.Journal.RetryCount, Is.Zero))
    }

    [<Test>]
    member _.``CBI55 C7 only a successor fence may reconcile``() = task {
        use temporary = new RestartTemporaryEffect()
        let attemptJournal = journal temporary
        use owner = acquire temporary "owner-a" "lease-a"
        prepare temporary owner.Snapshot.Epoch 0 instant |> ignore
        attemptJournal.BeginAttempt instant |> ignore
        let! result = ExternallyReconciledProviderRestartRecovery.reconcile owner attemptJournal temporary.EffectPath
        multiple (fun () ->
            Assert.That(result.Code, Is.EqualTo "restart-effect-successor-fence-required")
            Assert.That(result.Journal.Phase, Is.EqualTo "in-flight"))
    }

    [<Test>]
    member _.``CBI55 C8 minimal executes the shared reconciliation model``() = task {
        use fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures", "cbi55-restart-effect-reconciliation-vectors.json")))
        for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
            use temporary = new RestartTemporaryEffect()
            let attemptJournal = journal temporary
            let first = acquire temporary "owner-a" "lease-a"
            let effectKind = vector.GetProperty("effect").GetString()
            if effectKind <> "missing" then
                prepare temporary (if effectKind = "exact-current-fence" then 2L else 1L)
                    (if effectKind = "wrong-attempt" then 1 else 0) instant |> ignore
            attemptJournal.BeginAttempt instant |> ignore
            (first :> IDisposable).Dispose()
            use successor = acquire temporary "owner-b" "lease-b"
            let! result = ExternallyReconciledProviderRestartRecovery.reconcile successor attemptJournal temporary.EffectPath
            multiple (fun () ->
                Assert.That(result.Code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()))
                Assert.That(result.Journal.Phase, Is.EqualTo(vector.GetProperty("expectedPhase").GetString()))
                Assert.That(result.Journal.RetryCount, Is.EqualTo(vector.GetProperty("expectedRetries").GetInt32())))
    }
