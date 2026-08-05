namespace Brontide.Minimal.Host.Tests

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open System.Threading.Tasks
open NUnit.Framework
open Brontide.Minimal.Experimental.ComponentManagement
open Brontide.Minimal.Host

type private RestartTemporaryOwnership() =
    let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi54-{Guid.NewGuid():N}")
    member _.Root = root
    member _.Path = Path.Combine(root, "restart.owner")
    interface IDisposable with
        member _.Dispose() =
            let rec remove attempt =
                try if Directory.Exists root then Directory.Delete(root, true)
                with :? IOException when attempt < 249 ->
                    Threading.Thread.Sleep 20
                    remove (attempt + 1)
            remove 0

[<TestFixture>]
type ComponentProviderRestartOwnershipTests() =
    let multiple action = Assert.Multiple(Action action)
    let runIdentity = ProviderRestartAttemptRunId.create "restart-run.test.1"
    let occurrence = OccurrenceId.create "occ.def.test.cooling-provider.1"
    let staged = ProviderArtifactSetId.create (String('A', 64))

    let acquire path owner lease =
        DurableProviderRestartOwnership.Acquire(
            path, ProviderRestartOwnerId.create owner, ProviderRestartOwnershipLeaseId.create lease,
            runIdentity, occurrence, staged)

    let providerPath () =
        match Environment.GetEnvironmentVariable "BRONTIDE_MINIMAL_PROVIDER" with
        | null | "" -> Assert.Ignore "BRONTIDE_MINIMAL_PROVIDER does not name a built provider endpoint."; ""
        | path when not (File.Exists path) -> Assert.Ignore "BRONTIDE_MINIMAL_PROVIDER does not name a built provider endpoint."; ""
        | path -> Path.GetFullPath path

    let probe verb path = task {
        let start = ProcessStartInfo(providerPath(), UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true)
        start.ArgumentList.Add($"--{verb}-exclusive-file={path}")
        use child =
            match Process.Start start |> Option.ofObj with
            | Some value -> value
            | None -> failwith "The provider probe process did not start."
        do! child.WaitForExitAsync()
        return child.ExitCode
    }

    [<Test>]
    member _.``CBI54 C1 ownership is bound to one restart lineage``() =
        use temporary = new RestartTemporaryOwnership()
        let acquired = acquire temporary.Path "owner-a" "lease-a"
        (acquired.Ownership.Value :> IDisposable).Dispose()
        let before = File.ReadAllBytes(temporary.Path + ".state")
        let mismatch = DurableProviderRestartOwnership.Acquire(
            temporary.Path, ProviderRestartOwnerId.create "owner-a", ProviderRestartOwnershipLeaseId.create "lease-a",
            ProviderRestartAttemptRunId.create "restart-run.other", occurrence, staged)
        multiple (fun () ->
            Assert.That(acquired.Code, Is.EqualTo "restart-ownership-acquired")
            Assert.That(mismatch.Code, Is.EqualTo "restart-ownership-lineage-mismatch")
            Assert.That(File.ReadAllBytes(temporary.Path + ".state"), Is.EqualTo(box before)))

    [<Test; Category("CrossProcess")>]
    member _.``CBI54 C2 one operating system owner excludes other processes``() = task {
        use temporary = new RestartTemporaryOwnership()
        let acquired = acquire temporary.Path "owner-a" "lease-a"
        let! blocked = probe "probe" temporary.Path
        Assert.That(blocked, Is.EqualTo 74)
        (acquired.Ownership.Value :> IDisposable).Dispose()
        let! available = probe "probe" temporary.Path
        Assert.That(available, Is.Zero)
    }

    [<Test>]
    member _.``CBI54 C3 every acquisition advances an atomic durable fence``() =
        use temporary = new RestartTemporaryOwnership()
        let first = acquire temporary.Path "owner-a" "lease-a"
        (first.Ownership.Value :> IDisposable).Dispose()
        let before = File.ReadAllBytes(temporary.Path + ".state")
        Directory.CreateDirectory(temporary.Path + ".state.tmp") |> ignore
        let refused = acquire temporary.Path "owner-b" "lease-b"
        let afterRefused = File.ReadAllBytes(temporary.Path + ".state")
        Directory.Delete(temporary.Path + ".state.tmp")
        let second = acquire temporary.Path "owner-b" "lease-b"
        multiple (fun () ->
            Assert.That(refused.Code, Is.EqualTo "restart-ownership-write-failed")
            Assert.That(afterRefused, Is.EqualTo(box before))
            Assert.That(second.Snapshot.Value.Epoch, Is.EqualTo 2L))
        (second.Ownership.Value :> IDisposable).Dispose()

    [<Test>]
    member _.``CBI54 C4 only the current live lease matches the journal``() =
        use temporary = new RestartTemporaryOwnership()
        let journalPath = Path.Combine(temporary.Root, "restart.journal")
        let journal = DurableProviderRestartAttemptJournal.Establish(
            journalPath, runIdentity, occurrence, staged,
            ProviderRestartPolicy.create 2 (TimeSpan.FromMinutes 1.0)).Journal.Value
        let before = File.ReadAllBytes journalPath
        let ownership = (acquire temporary.Path "owner-a" "lease-a").Ownership.Value
        Assert.That(ownership.IsCurrentFor journal.Snapshot, Is.True)
        (ownership :> IDisposable).Dispose()
        multiple (fun () ->
            Assert.That(ownership.IsCurrentFor journal.Snapshot, Is.False)
            Assert.That(File.ReadAllBytes journalPath, Is.EqualTo(box before)))

    [<Test>]
    member _.``CBI54 C5 released and superseded leases are stale``() =
        use temporary = new RestartTemporaryOwnership()
        let first = acquire temporary.Path "owner-a" "lease-a"
        (first.Ownership.Value :> IDisposable).Dispose()
        let second = acquire temporary.Path "owner-a" "lease-a"
        multiple (fun () ->
            Assert.That(first.Ownership.Value.Snapshot.IsLive, Is.False)
            Assert.That(second.Snapshot.Value.Epoch, Is.EqualTo 2L)
            Assert.That(second.Snapshot.Value.IsLive, Is.True))
        (second.Ownership.Value :> IDisposable).Dispose()

    [<Test; Category("CrossProcess")>]
    member _.``CBI54 C6 process loss relinquishes exclusivity without erasing history``() = task {
        use temporary = new RestartTemporaryOwnership()
        let first = acquire temporary.Path "owner-a" "lease-a"
        (first.Ownership.Value :> IDisposable).Dispose()
        let start = ProcessStartInfo(providerPath(), UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true)
        start.ArgumentList.Add($"--hold-exclusive-file={temporary.Path}")
        use holder =
            match Process.Start start |> Option.ofObj with
            | Some value -> value
            | None -> failwith "The provider holder process did not start."
        let! held = holder.StandardOutput.ReadLineAsync()
        Assert.That(held, Is.EqualTo "held")
        Assert.That((acquire temporary.Path "owner-b" "lease-b").Code, Is.EqualTo "restart-ownership-busy")
        holder.Kill(true)
        do! holder.WaitForExitAsync()
        let recovered = acquire temporary.Path "owner-b" "lease-b"
        Assert.That(recovered.Snapshot.Value.Epoch, Is.EqualTo 2L)
        (recovered.Ownership.Value :> IDisposable).Dispose()
    }

    [<Test>]
    member _.``CBI54 C7 inspection is bounded and fails closed``() =
        use temporary = new RestartTemporaryOwnership()
        Assert.That(DurableProviderRestartOwnership.Inspect(temporary.Path, runIdentity, occurrence, staged).Code,
            Is.EqualTo "restart-ownership-missing")
        let acquired = acquire temporary.Path "owner-a" "lease-a"
        (acquired.Ownership.Value :> IDisposable).Dispose()
        let bytes = File.ReadAllBytes(temporary.Path + ".state")
        bytes[0] <- bytes[0] ^^^ 0x7Fuy
        File.WriteAllBytes(temporary.Path + ".state", bytes)
        Assert.That(DurableProviderRestartOwnership.Inspect(temporary.Path, runIdentity, occurrence, staged).Code,
            Is.EqualTo "restart-ownership-corrupt")

    [<Test>]
    member _.``CBI54 C8 minimal executes the shared ownership model``() =
        use fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures", "cbi54-restart-ownership-vectors.json")))
        let text (value: JsonElement) = value.GetString() |> Option.ofObj |> Option.defaultValue ""
        for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
            use temporary = new RestartTemporaryOwnership()
            let mutable ownership: DurableProviderRestartOwnership option = None
            let mutable snapshot: ProviderRestartOwnershipSnapshot option = None
            let mutable code = "none"
            for actionElement in vector.GetProperty("actions").EnumerateArray() do
                let action = text actionElement
                if action.StartsWith("acquire:", StringComparison.Ordinal) then
                    let parts = action.Split ':'
                    let result = acquire temporary.Path parts[1] parts[2]
                    code <- result.Code
                    ownership <- result.Ownership
                    snapshot <- result.Snapshot
                elif action = "release" then
                    (ownership.Value :> IDisposable).Dispose()
                    snapshot <- Some ownership.Value.Snapshot
                elif action = "inspect" then
                    let result = DurableProviderRestartOwnership.Inspect(temporary.Path, runIdentity, occurrence, staged)
                    code <- result.Code
                    snapshot <- result.Snapshot
            multiple (fun () ->
                Assert.That(code, Is.EqualTo(vector.GetProperty("expectedCode") |> text))
                Assert.That(snapshot.Value.Epoch, Is.EqualTo(vector.GetProperty("expectedEpoch").GetInt64()))
                Assert.That(snapshot.Value.Owner.Value, Is.EqualTo(vector.GetProperty("expectedOwner") |> text))
                Assert.That(snapshot.Value.Lease.Value, Is.EqualTo(vector.GetProperty("expectedLease") |> text))
                Assert.That(snapshot.Value.IsLive, Is.EqualTo(vector.GetProperty("expectedLive").GetBoolean())))
            ownership |> Option.iter (fun value -> (value :> IDisposable).Dispose())
