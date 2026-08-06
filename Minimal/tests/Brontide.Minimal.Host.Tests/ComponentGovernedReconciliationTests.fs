namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi63Observation =
    { Code: string
      Phase: string
      RotationApplied: string
      PolicyApplied: string
      Interruptions: int
      Retries: int }

[<TestFixture>]
type ComponentGovernedReconciliationTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI63 fixture value was missing." | present -> present

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi63-governed-reconciliation-vectors.json")))

    let runIdentity = ProviderTrustCadenceRunId.create "cbi63-governed-run"
    let start = DateTimeOffset.FromUnixTimeSeconds 1800000000L

    let authorityId (key: ECDsa) =
        key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyAuthorityId.create

    let policyFor (index: int64) : ProviderPublisherTrustPolicy =
        let entries = [ { PublisherKeyId = ProviderPublisherKeyId.create (index.ToString "X64")
                          Disposition = Admitted } ]
        { Identity = ProviderPublisherTrustPolicyIdentity.compute entries; Entries = entries }

    let signUpdate (key: ECDsa) sequence previous (policy: ProviderPublisherTrustPolicy) =
        let signature = key.SignData(
            ProviderPublisherTrustPolicyUpdateManifest.encode sequence previous policy.Identity,
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
        { Sequence = sequence; PreviousPolicyIdentity = previous; Policy = policy
          Algorithm = "ECDSA-P256-SHA256"
          AuthorityPublicKeySpkiBase64 = key.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
          SignatureBase64 = Convert.ToBase64String signature }

    let statementFor (previous: ECDsa) (next: ECDsa) =
        let previousId = authorityId previous
        let nextId = authorityId next
        let manifest = ProviderPolicyAuthorityRotationManifest.encode 1L 0L None previousId nextId
        let sign (key: ECDsa) =
            key.SignData(manifest, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            |> Convert.ToBase64String
        { Generation = 1L; PolicySequence = 0L; PolicyIdentity = None
          PreviousAuthority = previousId; NextAuthority = nextId; Algorithm = "ECDSA-P256-SHA256"
          PreviousAuthorityPublicKeySpkiBase64 = previous.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
          NextAuthorityPublicKeySpkiBase64 = next.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
          PreviousSignatureBase64 = sign previous; NextSignatureBase64 = sign next }

    let establish path =
        DurableProviderTrustCadenceJournal.Establish(
            path, runIdentity,
            ProviderServingTrustCadenceSchedule.create 3 (TimeSpan.FromSeconds 5.0), start)
            .Journal.Value

    /// Interrupts one governed attempt, advances the registry by the named effects, and applies one
    /// serving observation. The effects are produced by the real registry rather than described, so
    /// the derivation is compared against something a wrong implementation could disagree with.
    let runVector (vector: JsonElement) =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi63-{Guid.NewGuid():N}")
        try
            use pin = ECDsa.Create ECCurve.NamedCurves.nistP256
            use successor = ECDsa.Create ECCurve.NamedCurves.nistP256
            let _, opened, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(
                    Path.Combine(root, "policy.checkpoint"), authorityId pin, None)
            let registry = opened.Value
            let journal = establish (Path.Combine(root, "cadence.bin"))

            let cursor =
                match required (vector.GetProperty("cursor").GetString()) with
                | "absent" -> None
                // A cursor ahead of the registry is the rollback case: nothing advanced, yet the
                // recorded baseline claims more than the chain holds.
                | "recorded-ahead" ->
                    Some
                        { AuthorityGeneration = 5L
                          ActiveAuthority = ProviderPublisherTrustPolicyAuthorityId.value (authorityId successor)
                          PolicySequence = 0L
                          PolicyIdentity = None }
                | _ -> Some(ProviderGovernedTrustCadenceRecovery.cursor registry)
            Assert.That(journal.BeginCycle(cursor).Code, Is.EqualTo "durable-cadence-cycle-started")

            let effects = required (vector.GetProperty("effects").GetString())
            if effects = "rotation" || effects = "rotation-and-policy" then
                Assert.That(registry.Rotate(statementFor pin successor).IsApplied, Is.True)
            if effects = "policy" || effects = "rotation-and-policy" then
                let signer = if effects = "rotation-and-policy" then successor else pin
                Assert.That(registry.Apply(signUpdate signer 1L None (policyFor 1L)).IsApplied, Is.True)

            let serving = required (vector.GetProperty("serving").GetString())
            let evidence =
                { RunIdentity = runIdentity
                  AttemptIndex = if serving = "wrong-index" then 7 else journal.Snapshot.NextCycleIndex
                  AttemptInstant = start
                  Serving =
                    match serving with
                    | "effects-accounted-for" -> ProviderGovernedServingObservation.EffectsAccountedFor
                    | "unknown" -> ProviderGovernedServingObservation.Unknown
                    | _ -> ProviderGovernedServingObservation.NoEffectsConfirmed }

            let result = ProviderGovernedInterruptionReconciliation.apply journal evidence registry
            { Code = result.Code
              Phase = result.Snapshot.Phase
              RotationApplied =
                result.Derived |> Option.map (fun d -> string d.RotationApplied) |> Option.defaultValue "none"
              PolicyApplied =
                result.Derived |> Option.map (fun d -> string d.PolicyApplied) |> Option.defaultValue "none"
              Interruptions = result.Snapshot.InterruptionCount
              Retries = result.Snapshot.RetryCount }
        finally
            if Directory.Exists root then Directory.Delete(root, true)

    let expected (vector: JsonElement) =
        let flag (value: JsonElement) =
            if value.ValueKind = JsonValueKind.Null then "none" else string (value.GetBoolean())
        { Code = required (vector.GetProperty("code").GetString())
          Phase = required (vector.GetProperty("phase").GetString())
          RotationApplied = flag (vector.GetProperty "rotationApplied")
          PolicyApplied = flag (vector.GetProperty "policyApplied")
          Interruptions = vector.GetProperty("interruptions").GetInt32()
          Retries = vector.GetProperty("retries").GetInt32() }

    let named (document: JsonDocument) name =
        document.RootElement.GetProperty("vectors").EnumerateArray()
        |> Seq.find (fun vector -> vector.GetProperty("name").GetString() = name)

    [<Test>]
    member _.``shared CBI63 vectors reconcile a governed interruption``() =
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let name = vector.GetProperty "name" |> _.GetString()
            Assert.That(runVector vector, Is.EqualTo(expected vector), $"vector {name}")

    [<Test>]
    member _.``CBI63 C1 recording the cursor adds no journal write``() =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi63-writes-{Guid.NewGuid():N}")
        try
            use pin = ECDsa.Create ECCurve.NamedCurves.nistP256
            let _, opened, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(
                    Path.Combine(root, "policy.checkpoint"), authorityId pin, None)
            let transitions governed =
                let journal = establish (Path.Combine(root, $"{governed}.bin"))
                let cursor =
                    if governed then Some(ProviderGovernedTrustCadenceRecovery.cursor opened.Value) else None
                let codes =
                    [ journal.BeginCycle(cursor).Code
                      journal.CommitCycle(ProviderServingTrustCycleCodes.Current).Code ]
                // The cursor describes an attempt in flight and does not outlive it.
                Assert.That(journal.Snapshot.Cursor, Is.EqualTo None)
                codes
            let ungoverned = transitions false
            let governedCodes = transitions true
            // A governed run performs exactly the transitions an ungoverned one does.
            Assert.That(governedCodes, Is.EqualTo<string> ungoverned)
        finally
            if Directory.Exists root then Directory.Delete(root, true)

    [<Test>]
    member _.``CBI63 C2 a governed interruption is refused by the ungoverned path``() =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi63-path-{Guid.NewGuid():N}")
        try
            use pin = ECDsa.Create ECCurve.NamedCurves.nistP256
            let _, opened, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(
                    Path.Combine(root, "policy.checkpoint"), authorityId pin, None)
            let governedPath = Path.Combine(root, "governed.bin")
            let governed = establish governedPath
            Assert.That(
                governed.BeginCycle(Some(ProviderGovernedTrustCadenceRecovery.cursor opened.Value)).Code,
                Is.EqualTo "durable-cadence-cycle-started")
            let before = File.ReadAllBytes governedPath
            let refused =
                ProviderTrustCadenceReconciliation.apply governed
                    { RunIdentity = runIdentity; AttemptIndex = 0; AttemptInstant = start
                      Verdict = ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed }

            // The ungoverned run is unaffected, so the refusal is about the recorded cursor rather
            // than about the path having become unusable.
            let ungoverned = establish (Path.Combine(root, "ungoverned.bin"))
            Assert.That(ungoverned.BeginCycle().Code, Is.EqualTo "durable-cadence-cycle-started")
            let accepted =
                ProviderTrustCadenceReconciliation.apply ungoverned
                    { RunIdentity = runIdentity; AttemptIndex = 0; AttemptInstant = start
                      Verdict = ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed }

            Assert.Multiple(Action(fun () ->
                Assert.That(refused.Code, Is.EqualTo "cadence-reconciliation-governed")
                Assert.That(refused.Snapshot.Phase, Is.EqualTo "in-flight")
                Assert.That(File.ReadAllBytes governedPath, Is.EqualTo<byte> before)
                Assert.That(accepted.Code, Is.EqualTo "cadence-reconciliation-retry-ready")))
        finally
            if Directory.Exists root then Directory.Delete(root, true)

    [<Test>]
    member _.``CBI63 C4 the derived effects come from the registry and not the evidence``() =
        use document = fixture ()
        // Identical evidence over four different registry outcomes: only the derivation moves, which
        // is what makes it derived rather than restated.
        let observations =
            [ named document "no-effects-confirmed-retries"
              named document "a-rotation-is-derived-not-asserted"
              named document "a-policy-update-is-derived-not-asserted"
              named document "both-derived-effects-still-permit-retry" ]
            |> List.map runVector
        Assert.Multiple(Action(fun () ->
            Assert.That(
                observations |> List.map _.Code |> List.distinct,
                Is.EqualTo<string> [ "governed-reconciliation-retry-ready" ])
            Assert.That(
                observations |> List.map (fun value -> $"{value.RotationApplied}/{value.PolicyApplied}"),
                Is.EqualTo<string> [ "False/False"; "True/False"; "False/True"; "True/True" ])))

    [<Test>]
    member _.``CBI63 C5 an absent or regressed cursor derives nothing``() =
        use document = fixture ()
        for name in [ "an-absent-cursor-is-refused-not-guessed"; "a-regressed-cursor-is-refused" ] do
            let actual = runVector (named document name)
            Assert.That(actual.Phase, Is.EqualTo "in-flight", name)
            Assert.That(actual.RotationApplied, Is.EqualTo "none", name)
            Assert.That(actual.PolicyApplied, Is.EqualTo "none", name)
            Assert.That(actual.Interruptions, Is.Zero, name)

    [<Test>]
    member _.``CBI63 C6 the serving verdict alone decides and counts as CBI49 does``() =
        use document = fixture ()
        let retried = runVector (named document "both-derived-effects-still-permit-retry")
        let abandoned = runVector (named document "effects-accounted-for-abandons")
        let deferred = runVector (named document "a-derived-effect-with-unknown-serving-still-defers")
        Assert.Multiple(Action(fun () ->
            // Two derived effects and a retry: the derivation reports, it does not veto.
            Assert.That(retried.RotationApplied, Is.EqualTo "True")
            Assert.That((retried.Interruptions, retried.Retries), Is.EqualTo((1, 1)))
            Assert.That((abandoned.Interruptions, abandoned.Retries), Is.EqualTo((1, 0)))
            Assert.That((deferred.Interruptions, deferred.Retries), Is.EqualTo((0, 0)))
            Assert.That(deferred.Phase, Is.EqualTo "in-flight")))
