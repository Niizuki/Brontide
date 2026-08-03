namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi42Source(
    fetch: ProviderPublisherTrustPolicyDistributionRequest ->
        Task<ProviderPublisherTrustPolicyDistributionResponse>) =
    interface IProviderPublisherTrustPolicyDistributionSource with
        member _.FetchAsync(request, _) = fetch request

type private Cbi42Observation =
    { Code: string
      CheckpointCode: string option
      Opened: bool
      StoredBefore: int64
      StoredAfter: int64
      StoreChanged: bool }

[<TestFixture>]
type ComponentPolicyFloorCustodyTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI42 fixture value was missing." | present -> present

    let optional (value: string | null) =
        match value with null -> None | present -> Some present

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi42-floor-custody-vectors.json")))

    let authorityId (authority: ECDsa) =
        authority.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyAuthorityId.create

    let policyFor (index: int64) =
        let entries =
            [ { PublisherKeyId = ProviderPublisherKeyId.create (index.ToString "X64"); Disposition = Admitted } ]
        { Identity = ProviderPublisherTrustPolicyIdentity.compute entries; Entries = entries }

    let signUpdate (key: ECDsa) sequence previous policyIndex =
        let selected = policyFor policyIndex
        let signature = key.SignData(
            ProviderPublisherTrustPolicyUpdateManifest.encode sequence previous selected.Identity,
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
        { Sequence = sequence; PreviousPolicyIdentity = previous; Policy = selected
          Algorithm = "ECDSA-P256-SHA256"
          AuthorityPublicKeySpkiBase64 = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo())
          SignatureBase64 = Convert.ToBase64String signature }

    /// Applies count further chained updates from wherever the registry stands, retaining each floor
    /// when a store is given, and answers the last floor issued.
    let seed
        (registry: DurableProviderPublisherTrustPolicyRegistry)
        (store: DurableProviderPublisherTrustPolicyFloorStore option)
        (signer: ECDsa)
        count
        policyOffset =
        let mutable floor = registry.Floor
        for _ in 1..count do
            let sequence = (registry.Current |> Option.map _.Sequence |> Option.defaultValue 0L) + 1L
            let previous = registry.Current |> Option.map _.Policy.Identity
            let applied = registry.Apply(signUpdate signer sequence previous (sequence + policyOffset))
            Assert.That(applied.IsApplied, Is.True)
            store |> Option.iter (fun value -> Assert.That((value.Retain applied.Floor).IsRetained, Is.True))
            floor <- applied.Floor
        floor

    /// A floor cannot be fabricated, only issued, so every candidate a retention vector offers comes
    /// from a real application against a throwaway registry.
    let issuedFloor (root: string) (name: string) (signer: ECDsa) authority count policyOffset =
        let _, opened, _ =
            DurableProviderPublisherTrustPolicyRegistry.Open(Path.Combine(root, name), authority, None)
        seed opened.Value None signer count policyOffset

    let storedSequence (floorPath: string) authority foreign =
        if not (File.Exists floorPath) then 0L
        else
            match DurableProviderPublisherTrustPolicyFloorStore.Open(floorPath, authority) with
            | _, Some store -> store.Stored.Sequence
            | _ ->
                match DurableProviderPublisherTrustPolicyFloorStore.Open(floorPath, foreign) with
                | _, Some store -> store.Stored.Sequence
                | _ -> 0L

    let retain mutation (authority: ECDsa) (foreignAuthority: ECDsa) authorityIdentity foreignIdentity
        (root: string) (floorPath: string) (checkpointPath: string) =
        let openedCode, openedStore = DurableProviderPublisherTrustPolicyFloorStore.Open(floorPath, authorityIdentity)
        if mutation = "establish" then
            { Code = openedCode; CheckpointCode = None; Opened = false; StoredBefore = 0L
              StoredAfter = openedStore.Value.Stored.Sequence; StoreChanged = File.Exists floorPath }
        else
            let store = openedStore.Value
            let _, registryOption, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(checkpointPath, authorityIdentity, None)
            let registry = registryOption.Value
            // Bring the store to the sequence the vector says it holds before the retention.
            let seeded =
                match mutation with
                | "retain-first" -> 0
                | "retain-regressed" -> 2
                | _ -> 1
            seed registry (Some store) authority seeded 0L |> ignore

            let before = store.Stored
            let bytesBefore = File.ReadAllBytes floorPath
            let candidate =
                match mutation with
                | "retain-first" | "retain-advance" -> seed registry None authority 1 0L
                | "retain-identical" -> before
                // Same sequence, different policy: a fork rather than an advance.
                | "retain-forked" -> issuedFloor root "fork.checkpoint" authority authorityIdentity 1 98L
                | "retain-regressed" -> issuedFloor root "older.checkpoint" authority authorityIdentity 1 0L
                // A sequence that would otherwise advance, under an unpinned authority.
                | _ -> issuedFloor root "foreign.checkpoint" foreignAuthority foreignIdentity 2 0L
            let result = store.Retain candidate
            { Code = result.Code; CheckpointCode = None; Opened = false; StoredBefore = before.Sequence
              StoredAfter = store.Stored.Sequence
              StoreChanged = bytesBefore <> File.ReadAllBytes floorPath }

    let start mutation (authority: ECDsa) authorityIdentity foreignIdentity (root: string)
        (floorPath: string) (checkpointPath: string) tamperOffset =
        if mutation <> "start-fresh" then
            let _, store = DurableProviderPublisherTrustPolicyFloorStore.Open(floorPath, authorityIdentity)
            let _, registryOption, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(checkpointPath, authorityIdentity, None)
            let registry = registryOption.Value
            let seeded =
                match mutation with
                | "start-guard-removed" -> 0
                | "start-lagging-floor" -> 1
                | _ -> 2
            seed registry store authority seeded 0L |> ignore
            // The lagging vector publishes one update the store never saw, which is CBI41's crash
            // window: the checkpoint reaches two while the retained floor stayed at one.
            if mutation = "start-lagging-floor" || mutation = "start-guard-removed" then
                seed registry None authority 1 0L |> ignore

        match mutation with
        | "start-rolled-back" ->
            // A genuine older checkpoint replaces the current one, which is the rollback the floor
            // exists to catch.
            let older = Path.Combine(root, "older.checkpoint")
            let _, shadow, _ = DurableProviderPublisherTrustPolicyRegistry.Open(older, authorityIdentity, None)
            seed shadow.Value None authority 1 0L |> ignore
            File.Copy(older, checkpointPath, true)
        | "start-checkpoint-removed" -> File.Delete checkpointPath
        | "start-guard-removed" -> File.Delete floorPath
        | "start-corrupt-store" ->
            // The version marker: refused by structure before the tag is consulted.
            let bytes = File.ReadAllBytes floorPath
            bytes[8] <- bytes[8] ^^^ 1uy
            File.WriteAllBytes(floorPath, bytes)
        | "start-tampered-sequence" ->
            // A byte the parser would happily accept - a different but well-formed sequence. Only
            // the integrity tag can refuse this one.
            let bytes = File.ReadAllBytes floorPath
            bytes[tamperOffset] <- bytes[tamperOffset] ^^^ 1uy
            File.WriteAllBytes(floorPath, bytes)
        | "start-truncated-store" ->
            let bytes = File.ReadAllBytes floorPath
            File.WriteAllBytes(floorPath, bytes[.. bytes.Length - 5])
        | "start-trailing-store" ->
            File.WriteAllBytes(floorPath, Array.append (File.ReadAllBytes floorPath) [| 0uy |])
        | "start-foreign-store" ->
            File.WriteAllBytes(floorPath,
                ProviderPublisherTrustPolicyFloorRecord.encode foreignIdentity 2L (Some (policyFor 2L).Identity))
        | _ -> ()

        let before = if File.Exists floorPath then File.ReadAllBytes floorPath else [||]
        let storedBefore = storedSequence floorPath authorityIdentity foreignIdentity
        let code, checkpointCode, registry, _ =
            ProviderPublisherTrustPolicyCustody.open' checkpointPath floorPath authorityIdentity
        let after = if File.Exists floorPath then File.ReadAllBytes floorPath else [||]
        { Code = code; CheckpointCode = checkpointCode; Opened = Option.isSome registry
          StoredBefore = storedBefore
          StoredAfter = storedSequence floorPath authorityIdentity foreignIdentity
          StoreChanged = before <> after }

    let cycle mutation (authority: ECDsa) (endpointKey: ECDsa) authorityIdentity (root: string)
        (floorPath: string) (checkpointPath: string) = task {
        let endpointIdentity =
            endpointKey.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
            |> ProviderPublisherTrustPolicyDistributionEndpointId.create
        let _, _, registry, store =
            ProviderPublisherTrustPolicyCustody.open' checkpointPath floorPath authorityIdentity
        Assert.That(Option.isSome registry, Is.True)
        let storedBefore = store.Value.Stored.Sequence

        let now = DateTimeOffset.FromUnixTimeSeconds 1800000000L
        let served = ref 0
        let source = Cbi42Source(fun request -> task {
            let update =
                if served.Value < 2 then
                    Some(signUpdate authority (request.CurrentSequence + 1L) request.CurrentPolicyIdentity
                             (request.CurrentSequence + 1L))
                else None
            served.Value <- served.Value + 1
            let issued, expires = now.ToUnixTimeSeconds(), now.AddMinutes(1.0).ToUnixTimeSeconds()
            let signature = endpointKey.SignData(
                ProviderPublisherTrustPolicyDistributionManifest.encode request.Challenge request.CurrentSequence
                    request.CurrentPolicyIdentity issued expires update,
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            return
                { Challenge = request.Challenge; CurrentSequence = request.CurrentSequence
                  CurrentPolicyIdentity = request.CurrentPolicyIdentity; IssuedAtUnixSeconds = issued
                  ExpiresAtUnixSeconds = expires; Update = update; Algorithm = "ECDSA-P256-SHA256"
                  EndpointPublicKeySpkiBase64 = Convert.ToBase64String(endpointKey.ExportSubjectPublicKeyInfo())
                  SignatureBase64 = Convert.ToBase64String signature } })

        let schedule =
            ProviderPublisherTrustPolicyPollSchedule.create 6 (TimeSpan.FromSeconds 1.0) 4
                (TimeSpan.FromSeconds 10.0) (TimeSpan.FromSeconds 1.0)
        let delay: ProviderPublisherTrustPolicyPollDelay =
            fun instant duration _ -> Task.FromResult(instant + duration)
        let poller = ProviderPublisherTrustPolicyPoller(registry.Value, endpointIdentity, schedule)
        let! result = poller.PollAsync(source, store.Value.Sink, delay, now, CancellationToken.None)
        Assert.Multiple(Action(fun () ->
            Assert.That(result.Code, Is.EqualTo("policy-poll-current"))
            Assert.That(result.RetainedSequences |> Seq.map string |> String.concat ",", Is.EqualTo("1,2"))))

        if mutation = "cycle-then-rollback" then
            let older = Path.Combine(root, "older.checkpoint")
            let _, shadow, _ = DurableProviderPublisherTrustPolicyRegistry.Open(older, authorityIdentity, None)
            seed shadow.Value None authority 1 0L |> ignore
            File.Copy(older, checkpointPath, true)

        // The process is torn down: nothing is carried across but the two files. The change flag
        // reports the restart, which is the operation under test, not the cycle that preceded it.
        let before = File.ReadAllBytes floorPath
        let code, checkpointCode, restarted, _ =
            ProviderPublisherTrustPolicyCustody.open' checkpointPath floorPath authorityIdentity
        return
            { Code = code; CheckpointCode = checkpointCode; Opened = Option.isSome restarted
              StoredBefore = storedBefore
              StoredAfter = storedSequence floorPath authorityIdentity authorityIdentity
              StoreChanged = before <> File.ReadAllBytes floorPath }
    }

    let run (vector: JsonElement) = task {
        let mutation = vector.GetProperty("mutation").GetString() |> required
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi42-{Guid.NewGuid():N}")
        Directory.CreateDirectory root |> ignore
        try
            use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use foreignAuthority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use endpointKey = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let authorityIdentity = authorityId authority
            let foreignIdentity = authorityId foreignAuthority
            let floorPath = Path.Combine(root, "policy.floor")
            let checkpointPath = Path.Combine(root, "policy.checkpoint")
            match vector.GetProperty("kind").GetString() |> required with
            | "retain" ->
                return retain mutation authority foreignAuthority authorityIdentity foreignIdentity
                           root floorPath checkpointPath
            | "cycle" ->
                return! cycle mutation authority endpointKey authorityIdentity root floorPath checkpointPath
            | _ ->
                let tamperOffset =
                    match vector.TryGetProperty "tamperOffset" with
                    | true, value -> value.GetInt32()
                    | _ -> 0
                return start mutation authority authorityIdentity foreignIdentity root floorPath
                           checkpointPath tamperOffset
        finally
            if Directory.Exists root then Directory.Delete(root, true)
    }

    let runNamed (document: JsonDocument) (mutation: string) =
        document.RootElement.GetProperty("vectors").EnumerateArray()
        |> Seq.find (fun vector -> vector.GetProperty("mutation").GetString() = mutation)
        |> run

    [<Test>]
    member _.``shared CBI42 vectors keep durable custody of the recovery floor``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run vector
            let label = vector.GetProperty("mutation").GetString() |> required
            Assert.Multiple(Action(fun () ->
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()), label)
                Assert.That(actual.CheckpointCode,
                    Is.EqualTo(vector.GetProperty("checkpointCode").GetString() |> optional), label)
                Assert.That(actual.Opened, Is.EqualTo(vector.GetProperty("opened").GetBoolean()), label)
                Assert.That(actual.StoredBefore,
                    Is.EqualTo(vector.GetProperty("storedSequenceBefore").GetInt64()), label)
                Assert.That(actual.StoredAfter,
                    Is.EqualTo(vector.GetProperty("storedSequenceAfter").GetInt64()), label)
                Assert.That(actual.StoreChanged,
                    Is.EqualTo(vector.GetProperty("storeChanged").GetBoolean()), label)

                // Phase-wide properties, over every vector rather than per case.
                Assert.That(actual.StoredAfter, Is.GreaterThanOrEqualTo(actual.StoredBefore), label)
                if actual.Code <> "policy-floor-opened" && actual.Code <> "policy-floor-established"
                    && actual.Code <> "policy-floor-retained" then
                    Assert.That(actual.StoreChanged, Is.False, label)
                if not actual.Opened then
                    Assert.That(actual.CheckpointCode, Is.Not.EqualTo(Some "policy-checkpoint-recovered"), label)))
    }

    [<Test>]
    member _.``CBI42 C1 the stored record is canonical atomic and integrity checked``() =
        use document = fixture ()
        let golden = document.RootElement.GetProperty "goldenImage"
        let image =
            ProviderPublisherTrustPolicyFloorRecord.encode
                (golden.GetProperty("authorityIdentity").GetString() |> required
                 |> ProviderPublisherTrustPolicyAuthorityId.create)
                (golden.GetProperty("sequence").GetInt64())
                (golden.GetProperty("policyIdentity").GetString() |> required
                 |> ProviderPublisherTrustPolicyId.create |> Some)
        Assert.Multiple(Action(fun () ->
            Assert.That(image.Length, Is.EqualTo(golden.GetProperty("bytes").GetInt32()))
            Assert.That(image |> SHA256.HashData |> Convert.ToHexString,
                Is.EqualTo(golden.GetProperty("sha256").GetString()))))

    [<Test>]
    member _.``CBI42 C1 only the integrity tag refuses a well formed tampered record``() = task {
        // The structural checks cannot reach this one: the altered byte yields a different but
        // entirely parseable sequence, so a store that skipped its tag would accept it.
        use document = fixture ()
        let! actual = runNamed document "start-tampered-sequence"
        Assert.Multiple(Action(fun () ->
            Assert.That(actual.Code, Is.EqualTo("policy-floor-corrupt"))
            Assert.That(actual.Opened, Is.False)))
    }

    [<Test>]
    member _.``CBI42 C2 the store is established before the checkpoint it guards exists``() = task {
        use document = fixture ()
        let! fresh = runNamed document "start-fresh"
        let! removed = runNamed document "start-guard-removed"
        Assert.Multiple(Action(fun () ->
            // A first start establishes at zero; a checkpoint without a store is the guard removed.
            Assert.That(fresh.Code, Is.EqualTo("policy-floor-opened"))
            Assert.That(fresh.CheckpointCode, Is.EqualTo(Some "policy-checkpoint-empty"))
            Assert.That(fresh.StoredAfter, Is.EqualTo(0L))
            Assert.That(removed.Code, Is.EqualTo("policy-floor-missing"))
            Assert.That(removed.Opened, Is.False)))
    }

    [<Test>]
    member _.``CBI42 C3 a refused store refuses the start``() = task {
        use document = fixture ()
        for mutation in [ "start-corrupt-store"; "start-tampered-sequence"; "start-truncated-store"
                          "start-trailing-store"; "start-foreign-store" ] do
            let! actual = runNamed document mutation
            Assert.Multiple(Action(fun () ->
                Assert.That(actual.Opened, Is.False, mutation)
                Assert.That(actual.CheckpointCode, Is.EqualTo(None: string option), mutation)
                Assert.That(actual.StoreChanged, Is.False, mutation)))
    }

    [<Test>]
    member _.``CBI42 C4 a recovered checkpoint never raises the floor that guards it``() = task {
        use document = fixture ()
        let! lagging = runNamed document "start-lagging-floor"
        Assert.Multiple(Action(fun () ->
            // The checkpoint holds two and the store holds one; opening reports the checkpoint's
            // state and leaves the store exactly where the last handoff left it.
            Assert.That(lagging.Code, Is.EqualTo("policy-floor-opened"))
            Assert.That(lagging.StoredBefore, Is.EqualTo(1L))
            Assert.That(lagging.StoredAfter, Is.EqualTo(1L))
            Assert.That(lagging.StoreChanged, Is.False)))
    }

    [<Test>]
    member _.``CBI42 C5 retention is monotonic idempotent and refused to the cycle``() = task {
        use document = fixture ()
        for mutation in [ "retain-regressed"; "retain-forked"; "retain-foreign-authority" ] do
            let! actual = runNamed document mutation
            Assert.That(actual.StoreChanged, Is.False, mutation)
        let! identical = runNamed document "retain-identical"
        Assert.That(identical.Code, Is.EqualTo("policy-floor-unchanged"))

        // The composition refuses to start in the state a regressing handoff needs, so the sink's
        // refusal is pinned directly against a store seeded above the registry it is given.
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi42-{Guid.NewGuid():N}")
        Directory.CreateDirectory root |> ignore
        try
            use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let authorityIdentity = authorityId authority
            let _, store =
                DurableProviderPublisherTrustPolicyFloorStore.Open(
                    Path.Combine(root, "policy.floor"), authorityIdentity)
            let _, registry, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(
                    Path.Combine(root, "policy.checkpoint"), authorityIdentity, None)
            seed registry.Value store authority 2 0L |> ignore
            let older = issuedFloor root "older.checkpoint" authority authorityIdentity 1 0L
            Assert.ThrowsAsync<InvalidOperationException>(
                Func<Task>(fun () -> store.Value.Sink older CancellationToken.None)) |> ignore
        finally
            if Directory.Exists root then Directory.Delete(root, true)
    }

    [<Test>]
    member _.``CBI42 C6 the composition closes the poll loop across a restart``() = task {
        use document = fixture ()
        let! restarted = runNamed document "cycle-then-restart"
        let! rolledBack = runNamed document "cycle-then-rollback"
        Assert.Multiple(Action(fun () ->
            Assert.That(restarted.Code, Is.EqualTo("policy-floor-opened"))
            Assert.That(restarted.StoredAfter, Is.EqualTo(2L))
            // The same cycle, followed by an older checkpoint, is refused at the next start.
            Assert.That(rolledBack.Code, Is.EqualTo("policy-checkpoint-rollback-detected"))
            Assert.That(rolledBack.Opened, Is.False)
            Assert.That(rolledBack.StoredAfter, Is.EqualTo(2L))))
    }

    [<Test>]
    member _.``CBI42 C7 both roots agree on custody observations``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run vector
            let projection =
                String.concat "|"
                    [ actual.Code; actual.CheckpointCode |> Option.defaultValue "-"; string actual.Opened
                      string actual.StoredBefore; string actual.StoredAfter; string actual.StoreChanged ]
            let expected =
                String.concat "|"
                    [ vector.GetProperty("code").GetString() |> required
                      vector.GetProperty("checkpointCode").GetString() |> optional |> Option.defaultValue "-"
                      string (vector.GetProperty("opened").GetBoolean())
                      string (vector.GetProperty("storedSequenceBefore").GetInt64())
                      string (vector.GetProperty("storedSequenceAfter").GetInt64())
                      string (vector.GetProperty("storeChanged").GetBoolean()) ]
            Assert.That(projection, Is.EqualTo(expected), vector.GetProperty("mutation").GetString())
    }
