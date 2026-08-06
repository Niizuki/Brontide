namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Brontide.Minimal.Host
open NUnit.Framework

/// The three sequences are compared as joined text so structural equality reaches their elements and
/// so a failure names the sequence that differs.
type private Cbi60Observation =
    { Code: string
      LastAttemptCode: string
      Attempts: int
      DelayMilliseconds: string
      Applied: string
      Retained: string
      Stored: int64
      Recovered: int64 }

[<TestFixture>]
type ComponentPolicyAuthorityRotationCycleTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI60 fixture value was missing." | present -> present

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi60-policy-authority-cycle-vectors.json")))

    let authorityId (key: ECDsa) =
        key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyAuthorityId.create

    let endpointId (key: ECDsa) =
        key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyDistributionEndpointId.create

    let policy revoked : ProviderPublisherTrustPolicy =
        let entries = [ { PublisherKeyId = ProviderPublisherKeyId.create (String('A', 64))
                          Disposition = if revoked then Revoked else Admitted } ]
        { Identity = ProviderPublisherTrustPolicyIdentity.compute entries; Entries = entries }

    let signUpdate (key: ECDsa) sequence previous (policy: ProviderPublisherTrustPolicy) =
        let signature = key.SignData(
            ProviderPublisherTrustPolicyUpdateManifest.encode sequence previous policy.Identity,
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
        { Sequence = sequence; PreviousPolicyIdentity = previous; Policy = policy
          Algorithm = "ECDSA-P256-SHA256"
          AuthorityPublicKeySpkiBase64 = key.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
          SignatureBase64 = Convert.ToBase64String signature }

    let statement generation policySequence policyIdentity (previous: ECDsa) (next: ECDsa) =
        let previousId = authorityId previous
        let nextId = authorityId next
        let manifest =
            ProviderPolicyAuthorityRotationManifest.encode
                (max generation 1L) policySequence policyIdentity previousId nextId
        let sign (key: ECDsa) =
            key.SignData(manifest, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            |> Convert.ToBase64String
        { Generation = generation; PolicySequence = policySequence; PolicyIdentity = policyIdentity
          PreviousAuthority = previousId; NextAuthority = nextId; Algorithm = "ECDSA-P256-SHA256"
          PreviousAuthorityPublicKeySpkiBase64 = previous.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
          NextAuthorityPublicKeySpkiBase64 = next.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
          PreviousSignatureBase64 = sign previous; NextSignatureBase64 = sign next }

    let respond mutation (request: ProviderPolicyAuthorityRotationDistributionRequest)
        (endpoint: ECDsa) rotation (now: DateTimeOffset) =
        let issued = if mutation = "expired" then now.AddMinutes -2.0 else now
        let unsigned =
            { Challenge = if mutation = "challenge" then String('0', 64) else request.Challenge
              PolicySequence = request.PolicySequence
              PolicyIdentity = request.PolicyIdentity
              AuthorityGeneration =
                if mutation = "cursor" then request.AuthorityGeneration + 1L else request.AuthorityGeneration
              ActiveAuthority = request.ActiveAuthority
              IssuedAtUnixSeconds = issued.ToUnixTimeSeconds()
              ExpiresAtUnixSeconds =
                (if mutation = "expired" then now.AddMinutes -1.0 else now.AddMinutes 1.0).ToUnixTimeSeconds()
              Rotation = if mutation = "current" then None else Some rotation
              Algorithm = "ECDSA-P256-SHA256"
              EndpointPublicKeySpkiBase64 = endpoint.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
              SignatureBase64 = "" }
        let signature =
            endpoint.SignData(
                ProviderPolicyAuthorityRotationDistributionManifest.encode unsigned,
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            |> Convert.ToBase64String
        let signed = { unsigned with SignatureBase64 = signature }
        if mutation = "signature" then
            let changed = Convert.FromBase64String signed.SignatureBase64
            changed[changed.Length - 1] <- changed[changed.Length - 1] ^^^ 1uy
            { signed with SignatureBase64 = Convert.ToBase64String changed }
        else signed

    /// Answers one scripted outcome per attempt, so a vector states the endpoint's behaviour over a
    /// whole cycle rather than over one call.
    let source (script: string list) (keys: ECDsa list) (endpoint: ECDsa) (other: ECDsa) now =
        let attempts = ref 0
        let instance =
            { new IProviderPolicyAuthorityRotationDistributionSource with
                member _.FetchAsync(request, _) =
                    let mutation = script[min attempts.Value (script.Length - 1)]
                    attempts.Value <- attempts.Value + 1
                    if mutation = "transport" then raise (IOException "unavailable")
                    // The offered statement is derived from the cursor the request carries, so a
                    // cycle that applies one rotation is answered with the next.
                    let index = int request.AuthorityGeneration
                    let rotation =
                        statement
                            (request.AuthorityGeneration + (if mutation = "native" then 2L else 1L))
                            0L None keys[index] keys[index + 1]
                    let key = if mutation = "endpoint" then other else endpoint
                    Task.FromResult(respond mutation request key rotation now) }
        instance, attempts

    let delay: ProviderPolicyAuthorityCycleDelay =
        fun now duration cancellationToken ->
            cancellationToken.ThrowIfCancellationRequested()
            Task.FromResult(now + duration)

    let refusingSink: ProviderPolicyAuthorityFloorSink =
        fun _ _ -> raise (InvalidOperationException "custody refused")

    let join (values: seq<string>) = String.Join(",", values)

    let split (value: string) = if value.Length = 0 then [||] else value.Split ','

    let schedule (document: JsonElement) maximumAttempts =
        let block = document.GetProperty "schedule"
        ProviderPolicyAuthorityCycleSchedule.create
            maximumAttempts
            (TimeSpan.FromMilliseconds(float (block.GetProperty("baseDelayMilliseconds").GetInt32())))
            (block.GetProperty("backoffMultiplier").GetInt32())
            (TimeSpan.FromMilliseconds(float (block.GetProperty("maximumDelayMilliseconds").GetInt32())))
            (TimeSpan.FromMilliseconds(float (block.GetProperty("attemptTimeoutMilliseconds").GetInt32())))

    let run (document: JsonElement) (vector: JsonElement) = task {
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi60-{Guid.NewGuid():N}")
        let checkpoint = Path.Combine(root, "policy.checkpoint")
        let keys = [ for _ in 1..5 -> ECDsa.Create ECCurve.NamedCurves.nistP256 ]
        try
            use endpoint = ECDsa.Create ECCurve.NamedCurves.nistP256
            use other = ECDsa.Create ECCurve.NamedCurves.nistP256
            let pin = authorityId keys[0]
            let _, opened, _ = DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, pin, None)
            let durable = opened.Value
            let _, store = DurableProviderPolicyAuthorityFloorStore.Open(Path.Combine(root, "authority.floor"), pin)
            let store = store.Value
            let sink =
                if required (vector.GetProperty("sink").GetString()) = "refusing" then refusingSink
                else store.Sink
            let script =
                [ for value in vector.GetProperty("attempts").EnumerateArray() -> required (value.GetString()) ]
            let now = DateTimeOffset.FromUnixTimeSeconds 1800000000L
            let endpointSource, attempts = source script keys endpoint other now
            let cycle =
                ProviderPolicyAuthorityRotationCycle(
                    durable, endpointId endpoint,
                    schedule document (vector.GetProperty("maximumAttempts").GetInt32()))
            let! result = cycle.RunAsync(endpointSource, sink, delay, now, CancellationToken.None)
            let _, recovered, _, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, pin, None, Some store.Stored)
            Assert.That(attempts.Value, Is.EqualTo result.Attempts,
                        "the cycle must report exactly the calls it made")
            return
                { Code = result.Code
                  LastAttemptCode = result.LastAttemptCode |> Option.defaultValue ""
                  Attempts = result.Attempts
                  DelayMilliseconds = result.Delays |> List.map (fun value -> string value.TotalMilliseconds) |> join
                  Applied = result.AppliedGenerations |> List.map string |> join
                  Retained = result.RetainedGenerations |> List.map string |> join
                  Stored = store.Stored.Generation
                  Recovered = recovered |> Option.map _.AuthorityGeneration |> Option.defaultValue -1L }
        finally
            for key in keys do key.Dispose()
            if Directory.Exists root then Directory.Delete(root, true)
    }

    let expected (vector: JsonElement) =
        { Code = required (vector.GetProperty("code").GetString())
          LastAttemptCode = required (vector.GetProperty("lastAttemptCode").GetString())
          Attempts = vector.GetProperty("attemptCount").GetInt32()
          DelayMilliseconds =
            [ for value in vector.GetProperty("delaysMilliseconds").EnumerateArray() -> string (value.GetDouble()) ]
            |> join
          Applied =
            [ for value in vector.GetProperty("appliedGenerations").EnumerateArray() -> string (value.GetInt64()) ]
            |> join
          Retained =
            [ for value in vector.GetProperty("retainedGenerations").EnumerateArray() -> string (value.GetInt64()) ]
            |> join
          Stored = vector.GetProperty("storedGeneration").GetInt64()
          Recovered = vector.GetProperty("recoveredGeneration").GetInt64() }

    let named (document: JsonDocument) name =
        document.RootElement.GetProperty("vectors").EnumerateArray()
        |> Seq.find (fun vector -> vector.GetProperty("name").GetString() = name)

    [<Test>]
    member _.``shared CBI60 vectors schedule and retain authority rotations``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run document.RootElement vector
            let name = vector.GetProperty "name" |> _.GetString()
            Assert.That(actual, Is.EqualTo(expected vector), $"vector {name}")
    }

    [<Test>]
    member _.``CBI60 C1 a cycle is bounded and records one gap between attempts``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run document.RootElement vector
            let budget = vector.GetProperty("maximumAttempts").GetInt32()
            Assert.That(actual.Attempts, Is.LessThanOrEqualTo budget)
            Assert.That(split actual.DelayMilliseconds, Has.Length.EqualTo(actual.Attempts - 1))
        let! exhausted = run document.RootElement (named document "budget-is-exhausted")
        Assert.That(exhausted.Code, Is.EqualTo "policy-authority-cycle-exhausted")
    }

    [<Test>]
    member _.``CBI60 C2 only a changeable outcome is retried``() = task {
        use document = fixture ()
        for name in [ "endpoint-mismatch-ends-the-cycle"; "challenge-mismatch-ends-the-cycle"
                      "cursor-mismatch-ends-the-cycle"; "native-refusal-ends-the-cycle" ] do
            let! actual = run document.RootElement (named document name)
            Assert.That(actual.Code, Is.EqualTo "policy-authority-cycle-refused", name)
            // The budget is six, so a refusal that stops at one attempt stopped because it was not
            // retried rather than because it ran out.
            Assert.That(actual.Attempts, Is.EqualTo 1, name)
        let! retried = run document.RootElement (named document "stale-window-is-retried")
        let! midRetry = run document.RootElement (named document "invalid-signature-ends-the-cycle-mid-retry")
        Assert.That(retried.Code, Is.EqualTo "policy-authority-cycle-current")
        Assert.That(midRetry.Code, Is.EqualTo "policy-authority-cycle-refused")
        Assert.That(midRetry.Attempts, Is.EqualTo 2)
    }

    [<Test>]
    member _.``CBI60 C3 backoff follows consecutive failures and clamps``() =
        let schedule =
            ProviderPolicyAuthorityCycleSchedule.create
                8 (TimeSpan.FromMilliseconds 100.0) 2 (TimeSpan.FromMilliseconds 800.0) (TimeSpan.FromSeconds 1.0)
        Assert.Multiple(Action(fun () ->
            Assert.That(schedule.DelayForConsecutiveFailures 0, Is.EqualTo TimeSpan.Zero)
            Assert.That(schedule.DelayForConsecutiveFailures 1, Is.EqualTo(TimeSpan.FromMilliseconds 100.0))
            Assert.That(schedule.DelayForConsecutiveFailures 4, Is.EqualTo(TimeSpan.FromMilliseconds 800.0))
            Assert.That(schedule.DelayForConsecutiveFailures 64, Is.EqualTo(TimeSpan.FromMilliseconds 800.0))))
        Assert.Throws<ArgumentException>(Action(fun () ->
            ProviderPolicyAuthorityCycleSchedule.create
                1 (TimeSpan.FromMilliseconds 100.0) 2 (TimeSpan.FromMilliseconds 800.0) (TimeSpan.FromMinutes 2.0)
            |> ignore)) |> ignore

    [<Test>]
    member _.``CBI60 C3 an applied rotation resets the gap``() = task {
        use document = fixture ()
        let! actual = run document.RootElement (named document "progress-resets-backoff")
        // Without the reset the third gap would be the doubled 200ms rather than the base 100ms.
        Assert.That(actual.DelayMilliseconds, Is.EqualTo "100,0,100")
    }

    [<Test>]
    member _.``CBI60 C4 the floor is handed off after publication and never before``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run document.RootElement vector
            let name = vector.GetProperty("name").GetString()
            let applied = split actual.Applied
            let retained = split actual.Retained
            Assert.That(Array.truncate retained.Length applied, Is.EqualTo<string> retained, name)
            Assert.That(applied.Length - retained.Length, Is.InRange(0, 1), name)
            Assert.That(actual.Stored, Is.LessThanOrEqualTo actual.Recovered, name)
        let! unretained = run document.RootElement (named document "refused-handoff-stops-the-cycle")
        Assert.That(unretained.Code, Is.EqualTo "policy-authority-cycle-floor-unretained")
        // The rotation is durable and cannot be undone, so the checkpoint is ahead of the guard.
        Assert.That(unretained.Applied, Is.EqualTo "1")
        Assert.That(unretained.Retained, Is.Empty)
        Assert.That(unretained.Recovered, Is.EqualTo 1L)
    }

    [<Test>]
    member _.``CBI60 C5 custody is bound to the pin and never regresses``() =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi60-custody-{Guid.NewGuid():N}")
        let path = Path.Combine(root, "authority.floor")
        try
            use pinKey = ECDsa.Create ECCurve.NamedCurves.nistP256
            use firstKey = ECDsa.Create ECCurve.NamedCurves.nistP256
            use forkKey = ECDsa.Create ECCurve.NamedCurves.nistP256
            let pin = authorityId pinKey
            let openedCode, opened = DurableProviderPolicyAuthorityFloorStore.Open(path, pin)
            let store = opened.Value
            let advanced = store.Retain(ProviderPolicyAuthorityFloor.Restore(1L, authorityId firstKey))
            let unchanged = store.Retain(ProviderPolicyAuthorityFloor.Restore(1L, authorityId firstKey))
            let fork = store.Retain(ProviderPolicyAuthorityFloor.Restore(1L, authorityId forkKey))
            let regressed = store.Retain(ProviderPolicyAuthorityFloor.Restore(0L, pin))
            // Generation zero under anything but the pin is the one floor no unrotated checkpoint
            // could satisfy, so it is refused as a pin mismatch rather than as a regression.
            let zeroUnderOther = store.Retain(ProviderPolicyAuthorityFloor.Restore(0L, authorityId forkKey))
            let reopenedCode, reopened = DurableProviderPolicyAuthorityFloorStore.Open(path, pin)
            let foreignCode, _ = DurableProviderPolicyAuthorityFloorStore.Open(path, authorityId forkKey)
            let bytes = File.ReadAllBytes path
            bytes[bytes.Length - 1] <- bytes[bytes.Length - 1] ^^^ 1uy
            File.WriteAllBytes(path, bytes)
            let corruptCode, _ = DurableProviderPolicyAuthorityFloorStore.Open(path, pin)
            Assert.Multiple(Action(fun () ->
                Assert.That(openedCode, Is.EqualTo "policy-authority-floor-established")
                Assert.That(advanced.Code, Is.EqualTo "policy-authority-floor-retained")
                Assert.That(unchanged.Code, Is.EqualTo "policy-authority-floor-unchanged")
                Assert.That(fork.Code, Is.EqualTo "policy-authority-floor-regressed")
                Assert.That(regressed.Code, Is.EqualTo "policy-authority-floor-regressed")
                Assert.That(zeroUnderOther.Code, Is.EqualTo "policy-authority-floor-authority-mismatch")
                Assert.That(store.Stored.Generation, Is.EqualTo 1L)
                Assert.That(reopenedCode, Is.EqualTo "policy-authority-floor-recovered")
                Assert.That(reopened.Value.Stored.Generation, Is.EqualTo 1L)
                Assert.That(foreignCode, Is.EqualTo "policy-authority-floor-authority-mismatch")
                Assert.That(corruptCode, Is.EqualTo "policy-authority-floor-corrupt")))
        finally
            if Directory.Exists root then Directory.Delete(root, true)

    [<Test>]
    member _.``CBI60 C6 only the authority floor detects a truncated trailing rotation``() =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi60-truncation-{Guid.NewGuid():N}")
        let checkpoint = Path.Combine(root, "policy.checkpoint")
        try
            use pinKey = ECDsa.Create ECCurve.NamedCurves.nistP256
            use successorKey = ECDsa.Create ECCurve.NamedCurves.nistP256
            let pin = authorityId pinKey
            let _, opened, _ = DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, pin, None)
            let durable = opened.Value
            let current = policy false
            let update = durable.Apply(signUpdate pinKey 1L None current)
            Assert.That(update.IsApplied, Is.True)
            let beforeRotation = File.ReadAllBytes checkpoint
            let rotated = durable.Rotate(statement 1L 1L (Some current.Identity) pinKey successorKey)
            Assert.That(rotated.IsApplied, Is.True)

            // The trailing rotation is dropped; every policy update in the chain survives it.
            File.WriteAllBytes(checkpoint, beforeRotation)
            let policyCode, truncated, _, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, pin, Some update.Floor, None)
            let authorityCode, _, _, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(
                    checkpoint, pin, Some update.Floor, Some rotated.Floor)

            // A truncation that dropped a rotation carrying later updates is unconstructible: those
            // updates name the successor authority the truncation removed.
            let successorSigned =
                truncated.Value.Apply(signUpdate successorKey 2L (Some current.Identity) (policy true))

            Assert.Multiple(Action(fun () ->
                Assert.That(policyCode, Is.EqualTo "policy-checkpoint-recovered")
                Assert.That(authorityCode, Is.EqualTo "policy-authority-rollback-detected")
                Assert.That(successorSigned.IsApplied, Is.False)
                Assert.That(successorSigned.Code, Is.EqualTo "policy-update-authority-mismatch")))
        finally
            if Directory.Exists root then Directory.Delete(root, true)

    [<Test>]
    member _.``CBI60 C6 an absent authority guard is adopted rather than recovered``() =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi60-adoption-{Guid.NewGuid():N}")
        let checkpoint = Path.Combine(root, "policy.checkpoint")
        let floor = Path.Combine(root, "policy.floor")
        let authorityFloor = Path.Combine(root, "authority.floor")
        try
            use pinKey = ECDsa.Create ECCurve.NamedCurves.nistP256
            use successorKey = ECDsa.Create ECCurve.NamedCurves.nistP256
            let pin = authorityId pinKey
            let freshCode, _, freshRegistry, _, _ =
                ProviderPolicyAuthorityCustody.open' checkpoint floor authorityFloor pin
            Assert.That(freshCode, Is.EqualTo "policy-authority-floor-opened")
            Assert.That(freshRegistry.Value.Rotate(statement 1L 0L None pinKey successorKey).IsApplied, Is.True)

            // A guard introduced after the checkpoint it must guard cannot read its own absence as a
            // removal, so it adopts the host at zero and says so.
            File.Delete authorityFloor
            let adoptedCode, _, adoptedRegistry, _, adoptedFloors =
                ProviderPolicyAuthorityCustody.open' checkpoint floor authorityFloor pin
            File.Delete floor
            let missingCode, _, _, _, _ =
                ProviderPolicyAuthorityCustody.open' checkpoint floor authorityFloor pin
            Assert.Multiple(Action(fun () ->
                Assert.That(adoptedCode, Is.EqualTo "policy-authority-floor-adopted")
                Assert.That(adoptedFloors.Value.Stored.Generation, Is.EqualTo 0L)
                Assert.That(adoptedRegistry.Value.AuthorityGeneration, Is.EqualTo 1L)
                // CBI42's guard could be ordered before its checkpoint, so its absence is still a
                // refusal, and deleting both is caught by that one.
                Assert.That(missingCode, Is.EqualTo "policy-floor-missing")))
        finally
            if Directory.Exists root then Directory.Delete(root, true)

    [<Test>]
    member _.``CBI60 C7 cancellation ends the cycle without a further attempt``() = task {
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi60-cancel-{Guid.NewGuid():N}")
        let keys = [ for _ in 1..3 -> ECDsa.Create ECCurve.NamedCurves.nistP256 ]
        try
            use endpoint = ECDsa.Create ECCurve.NamedCurves.nistP256
            use other = ECDsa.Create ECCurve.NamedCurves.nistP256
            let pin = authorityId keys[0]
            let _, opened, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(Path.Combine(root, "policy.checkpoint"), pin, None)
            let _, store = DurableProviderPolicyAuthorityFloorStore.Open(Path.Combine(root, "authority.floor"), pin)
            let cycle =
                ProviderPolicyAuthorityRotationCycle(
                    opened.Value, endpointId endpoint,
                    ProviderPolicyAuthorityCycleSchedule.create
                        4 (TimeSpan.FromMilliseconds 10.0) 2 (TimeSpan.FromMilliseconds 20.0)
                        (TimeSpan.FromSeconds 1.0))
            use cancellation = new CancellationTokenSource()
            cancellation.Cancel()
            let now = DateTimeOffset.FromUnixTimeSeconds 1800000000L
            let endpointSource, attempts = source [ "current" ] keys endpoint other now
            let! result = cycle.RunAsync(endpointSource, store.Value.Sink, delay, now, cancellation.Token)
            Assert.Multiple(Action(fun () ->
                Assert.That(result.Code, Is.EqualTo "policy-authority-cycle-canceled")
                Assert.That(result.Attempts, Is.Zero)
                Assert.That(attempts.Value, Is.Zero)))
        finally
            for key in keys do key.Dispose()
            if Directory.Exists root then Directory.Delete(root, true)
    }
