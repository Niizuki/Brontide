namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Text.Json
open Brontide.Minimal.Experimental.ComponentManagement
open Brontide.Minimal.Host
open NUnit.Framework

[<TestFixture>]
type ComponentStopAttributionTests() =
    let multiple action = Assert.Multiple(Action action)

    let textValue (value: JsonElement) = value.GetString() |> Option.ofObj |> Option.defaultValue ""

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi67-stop-attribution-vectors.json")))

    let deleteTree path = try Directory.Delete(path, true) with _ -> ()

    let causeOf name =
        match name with
        | "offline-availability" -> OfflineAvailability
        | "publisher-trust-withdrawal" -> PublisherTrustWithdrawal
        | "operator-retirement" -> OperatorRetirement
        | "unexpected-exit" -> UnexpectedExit
        | other -> failwithf "Unknown cause %s." other

    let causeName cause =
        match cause with
        | OfflineAvailability -> "offline-availability"
        | PublisherTrustWithdrawal -> "publisher-trust-withdrawal"
        | OperatorRetirement -> "operator-retirement"
        | UnexpectedExit -> "unexpected-exit"

    let storeAt root =
        (DurableProviderStopAttributionStore.Open(Path.Combine(root, "stops.bin"))).Store.Value

    let newRoot () = Path.Combine(Path.GetTempPath(), $"brontide-cbi67-{Guid.NewGuid():N}")

    /// Seeds one store as the vector describes and asks it about the activation's identities. No
    /// provider is launched: what the store answers is decided by the record it holds, and that CBI51
    /// acts on the answer is pinned by the restart scenarios, which do run real providers.
    let runVector (fixtureRoot: JsonElement) (vector: JsonElement) =
        let root = newRoot ()
        try
            let store = storeAt root
            let occurrence = OccurrenceId.create (textValue (fixtureRoot.GetProperty "occurrence"))
            let staged = ProviderArtifactSetId.create (textValue (fixtureRoot.GetProperty "stagedIdentity"))
            let other =
                ProviderArtifactSetId.create (textValue (fixtureRoot.GetProperty "otherStagedIdentity"))
            let recordedAt =
                DateTimeOffset.FromUnixTimeSeconds(
                    fixtureRoot.GetProperty("recordedAtUnixSeconds").GetInt64())
            let recorded = vector.GetProperty "recorded"
            if recorded.ValueKind <> JsonValueKind.Null then
                let under =
                    if textValue (vector.GetProperty "recordedUnder") = "other" then other else staged
                Assert.That(
                    store.Record(occurrence, under, recordedAt, causeOf (textValue recorded)),
                    Is.EqualTo "provider-stop-attribution-recorded")
            let result = store.Attribute(occurrence, staged)
            let cause = result.Attribution |> Option.map _.Cause
            result.Code,
            (cause |> Option.map causeName |> Option.defaultValue "none"),
            (match cause with
             | Some PublisherTrustWithdrawal | Some OperatorRetirement -> true
             | _ -> Option.isNone result.Attribution)
        finally
            deleteTree root

    let expectedOf (vector: JsonElement) =
        let cause = vector.GetProperty "cause"
        textValue (vector.GetProperty "code"),
        (if cause.ValueKind = JsonValueKind.Null then "none" else textValue cause),
        vector.GetProperty("restartRefused").GetBoolean()

    [<Test>]
    member _.``CBI67 C8 minimal attributes the shared stop vectors``() =
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            Assert.That(
                runVector document.RootElement vector, Is.EqualTo(expectedOf vector),
                textValue (vector.GetProperty "name"))

    /// The store is the only issuer. A caller cannot construct an attribution, which is the whole of
    /// what C2 buys: CBI51's refusals are unchanged and the caller no longer chooses which applies.
    [<Test>]
    member _.``CBI67 C2 an attribution has no public construction path``() =
        let attribution = typeof<ProviderStopAttribution>
        let publicFactories =
            attribution.GetMethods(
                System.Reflection.BindingFlags.Public ||| System.Reflection.BindingFlags.Static)
            |> Array.filter _.Name.StartsWith("New", StringComparison.Ordinal)
        multiple (fun () ->
            Assert.That(
                attribution.GetConstructors(
                    System.Reflection.BindingFlags.Public ||| System.Reflection.BindingFlags.Instance),
                Is.Empty)
            // The single case is private, so F# emits no public case constructor either.
            Assert.That(publicFactories, Is.Empty))

    [<Test>]
    member _.``CBI67 C4 absence yields one cause and never a refusal``() =
        let root = newRoot ()
        try
            let store = storeAt root
            let result =
                store.Attribute(
                    OccurrenceId.create "occ.def.test.absent.1",
                    ProviderArtifactSetId.create (String('A', 64)))
            multiple (fun () ->
                Assert.That(result.Code, Is.EqualTo "provider-stop-attribution-unrecorded")
                Assert.That(result.Attribution |> Option.map _.Cause, Is.EqualTo(Some UnexpectedExit))
                Assert.That(
                    result.Attribution |> Option.bind _.Instant, Is.EqualTo(None: DateTimeOffset option)))
        finally
            deleteTree root

    [<Test>]
    member _.``CBI67 C5 an unexpected exit cannot be recorded``() =
        let root = newRoot ()
        try
            let store = storeAt root
            // Absence is what an unexpected exit is. A record naming it would be a record of the host
            // not having stopped anything, and the operator path is the only way the one cause this
            // slice exists to attribute comes into existence.
            Assert.Throws<ArgumentException>(
                Action(fun () ->
                    store.Record(
                        OccurrenceId.create "occ.def.test.cooling-provider.1",
                        ProviderArtifactSetId.create (String('A', 64)),
                        DateTimeOffset.UnixEpoch,
                        UnexpectedExit)
                    |> ignore))
            |> ignore
        finally
            deleteTree root

    [<Test>]
    member _.``CBI67 C6 a cleared record no longer attributes``() =
        let root = newRoot ()
        try
            let store = storeAt root
            let occurrence = OccurrenceId.create "occ.def.test.cooling-provider.1"
            let staged = ProviderArtifactSetId.create (String('A', 64))
            store.Record(occurrence, staged, DateTimeOffset.UnixEpoch, OfflineAvailability) |> ignore
            multiple (fun () ->
                Assert.That(
                    store.Attribute(occurrence, staged).Code,
                    Is.EqualTo "provider-stop-attribution-issued")
                Assert.That(store.Clear occurrence, Is.EqualTo "provider-stop-attribution-cleared")
                // A stale record must not authorize a second restart of a provider already running.
                Assert.That(
                    store.Attribute(occurrence, staged).Code,
                    Is.EqualTo "provider-stop-attribution-unrecorded")
                Assert.That(store.Clear occurrence, Is.EqualTo "provider-stop-attribution-absent"))
        finally
            deleteTree root

    [<Test>]
    member _.``CBI67 C7 a corrupted record is refused and survives a reopen when intact``() =
        let root = newRoot ()
        let path = Path.Combine(root, "stops.bin")
        try
            let store = storeAt root
            let occurrence = OccurrenceId.create "occ.def.test.cooling-provider.1"
            let staged = ProviderArtifactSetId.create (String('A', 64))
            store.Record(occurrence, staged, DateTimeOffset.UnixEpoch, OperatorRetirement) |> ignore
            let reopened = DurableProviderStopAttributionStore.Open path
            let intact = reopened.Store.Value.Attribute(occurrence, staged)
            // A byte the parser accepts, so only the tag can refuse it — the case a store that never
            // checked its tag would pass, which CBI42 had to learn by deliberate defect.
            let bytes = File.ReadAllBytes path
            bytes[bytes.Length - 40] <- bytes[bytes.Length - 40] ^^^ 1uy
            File.WriteAllBytes(path, bytes)
            let corrupt = DurableProviderStopAttributionStore.Open path
            multiple (fun () ->
                Assert.That(reopened.Code, Is.EqualTo "provider-stop-attribution-opened")
                Assert.That(intact.Attribution |> Option.map _.Cause, Is.EqualTo(Some OperatorRetirement))
                Assert.That(corrupt.Code, Is.EqualTo "provider-stop-attribution-corrupt")
                Assert.That(corrupt.Store, Is.EqualTo(None: DurableProviderStopAttributionStore option)))
        finally
            deleteTree root
