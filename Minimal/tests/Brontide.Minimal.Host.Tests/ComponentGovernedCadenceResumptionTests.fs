namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi62Observation =
    { Code: string
      Phase: string
      Committed: string
      NextCycleIndex: int
      Gaps: int
      Interruptions: int
      Retries: int }

/// One rotation endpoint and one policy endpoint over a real registry. By default each answers
/// relative to the cursor it is given, which is what an honest endpoint does after a host's own state
/// has moved. In replay mode both re-offer the identical statement and update whatever the cursor
/// says, which is the stale or hostile endpoint a retry must also survive.
type private Cbi62Endpoints(
    statement: ProviderPolicyAuthorityRotationStatement,
    update: ProviderPublisherTrustPolicyUpdate,
    respondRotation:
        string -> ProviderPolicyAuthorityRotationDistributionRequest
            -> ProviderPolicyAuthorityRotationStatement -> ProviderPolicyAuthorityRotationDistributionResponse,
    respondPolicy:
        ProviderPublisherTrustPolicyDistributionRequest -> ProviderPublisherTrustPolicyUpdate option
            -> ProviderPublisherTrustPolicyDistributionResponse,
    rotationReachable: bool) =

    member val Replay = false with get, set

    interface IProviderPolicyAuthorityRotationDistributionSource with
        member this.FetchAsync(request, _) =
            if not rotationReachable then raise (IOException "unavailable")
            let offers = this.Replay || request.AuthorityGeneration = 0L
            Task.FromResult(respondRotation (if offers then "rotate" else "current") request statement)

    interface IProviderPublisherTrustPolicyDistributionSource with
        member this.FetchAsync(request, _) =
            let offered = if this.Replay || request.CurrentSequence = 0L then Some update else None
            Task.FromResult(respondPolicy request offered)

[<TestFixture>]
type ComponentGovernedCadenceResumptionTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI62 fixture value was missing." | present -> present

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi62-governed-cadence-resumption-vectors.json")))

    let runIdentity = ProviderTrustCadenceRunId.create "cbi62-governed-run"
    let start = DateTimeOffset.FromUnixTimeSeconds 1800000000L
    let join (values: seq<string>) = String.Join(",", values)

    let authorityId (key: ECDsa) =
        key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyAuthorityId.create

    let endpointId (key: ECDsa) =
        key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyDistributionEndpointId.create

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

    let respondRotation (endpoint: ECDsa) (now: DateTimeOffset) kind
        (request: ProviderPolicyAuthorityRotationDistributionRequest) rotation =
        let unsigned =
            { Challenge = request.Challenge
              PolicySequence = request.PolicySequence
              PolicyIdentity = request.PolicyIdentity
              AuthorityGeneration = request.AuthorityGeneration
              ActiveAuthority = request.ActiveAuthority
              IssuedAtUnixSeconds = now.ToUnixTimeSeconds()
              ExpiresAtUnixSeconds = now.AddMinutes(1.0).ToUnixTimeSeconds()
              Rotation = if kind = "rotate" then Some rotation else None
              Algorithm = "ECDSA-P256-SHA256"
              EndpointPublicKeySpkiBase64 = endpoint.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
              SignatureBase64 = "" }
        let signature =
            endpoint.SignData(
                ProviderPolicyAuthorityRotationDistributionManifest.encode unsigned,
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            |> Convert.ToBase64String
        { unsigned with SignatureBase64 = signature }

    let respondPolicy (endpoint: ECDsa) (now: DateTimeOffset)
        (request: ProviderPublisherTrustPolicyDistributionRequest) offered =
        let expires = now.AddMinutes 1.0
        let signature =
            endpoint.SignData(
                ProviderPublisherTrustPolicyDistributionManifest.encode
                    request.Challenge request.CurrentSequence request.CurrentPolicyIdentity
                    (now.ToUnixTimeSeconds()) (expires.ToUnixTimeSeconds()) offered,
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            |> Convert.ToBase64String
        { Challenge = request.Challenge
          CurrentSequence = request.CurrentSequence
          CurrentPolicyIdentity = request.CurrentPolicyIdentity
          IssuedAtUnixSeconds = now.ToUnixTimeSeconds()
          ExpiresAtUnixSeconds = expires.ToUnixTimeSeconds()
          Update = offered
          Algorithm = "ECDSA-P256-SHA256"
          EndpointPublicKeySpkiBase64 = endpoint.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
          SignatureBase64 = signature }

    let instantDelay: ProviderPolicyAuthorityCycleDelay =
        fun now duration cancellationToken ->
            cancellationToken.ThrowIfCancellationRequested()
            Task.FromResult(now + duration)

    let pollDelay: ProviderPublisherTrustPolicyPollDelay =
        fun now duration cancellationToken ->
            cancellationToken.ThrowIfCancellationRequested()
            Task.FromResult(now + duration)

    /// Composes a governed cycle over a real registry rooted at the given directory.
    let compose root (pin: ECDsa) (successor: ECDsa) (endpoint: ECDsa) rotationReachable =
        let identity = authorityId pin
        let _, opened, _ =
            DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), identity, None)
        let durable = opened.Value
        let _, policyFloors =
            DurableProviderPublisherTrustPolicyFloorStore.Open(Path.Combine(root, "policy.floor"), identity)
        let _, authorityFloors =
            DurableProviderPolicyAuthorityFloorStore.Open(Path.Combine(root, "authority.floor"), identity)
        let endpoints =
            Cbi62Endpoints(
                statementFor pin successor,
                signUpdate successor 1L None (policyFor 1L),
                respondRotation endpoint start,
                respondPolicy endpoint start,
                rotationReachable)
        let rotation =
            ProviderGovernedTrustCycle.rotationBinding
                (ProviderPolicyAuthorityRotationCycle(
                    durable, endpointId endpoint,
                    ProviderPolicyAuthorityCycleSchedule.create
                        2 (TimeSpan.FromMilliseconds 1.0) 2 (TimeSpan.FromMilliseconds 2.0)
                        (TimeSpan.FromSeconds 1.0)))
                endpoints authorityFloors.Value.Sink instantDelay
        let policy =
            ProviderServingTrustCycleBinding.policy
                (ProviderPublisherTrustPolicyPoller(
                    durable, endpointId endpoint,
                    ProviderPublisherTrustPolicyPollSchedule.create
                        2 (TimeSpan.FromMilliseconds 1.0) 2 (TimeSpan.FromMilliseconds 2.0)
                        (TimeSpan.FromSeconds 1.0)))
                endpoints policyFloors.Value.Sink pollDelay
        let emptySweep: ProviderServingTrustSweepCycle = fun _ -> Task.FromResult None
        let cycle =
            ProviderGovernedTrustCycle.create rotation (ProviderServingTrustCycle.create policy emptySweep)
        cycle, durable, endpoints

    let establish path maximumCycles (interval: TimeSpan) =
        DurableProviderTrustCadenceJournal.Establish(
            path, runIdentity, ProviderServingTrustCadenceSchedule.create maximumCycles interval, start)
            .Journal.Value

    let runVector (document: JsonElement) (vector: JsonElement) =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi62-{Guid.NewGuid():N}")
        let path = Path.Combine(root, "cadence.bin")
        try
            let interval =
                TimeSpan.FromMilliseconds(
                    float (document.GetProperty("schedule").GetProperty("intervalMilliseconds").GetInt32()))
            let mutable journal = establish path (vector.GetProperty("maximumCycles").GetInt32()) interval
            let mutable code = "durable-cadence-established"
            for step in vector.GetProperty("steps").EnumerateArray() do
                if journal.Snapshot.Phase = "waiting" then
                    let gap = journal.CompleteGap(journal.Snapshot.PreparedInstant + interval)
                    Assert.That(gap.Code, Is.EqualTo "durable-cadence-gap-completed")
                Assert.That(journal.BeginCycle().Code, Is.EqualTo "durable-cadence-cycle-started")
                let mutable decision = Unchecked.defaultof<JsonElement>
                if step.TryGetProperty("interrupt", &decision) then
                    // Reopening is what a restart does, and it must see the interruption rather than
                    // a cursor that advanced on its own.
                    let reopened = DurableProviderTrustCadenceJournal.Open(path, runIdentity)
                    Assert.That(reopened.Code, Is.EqualTo "durable-cadence-indeterminate")
                    journal <- reopened.Journal.Value
                    code <-
                        journal.ResolveInterrupted(
                            if decision.GetString() = "retry" then Retry else Abandon).Code
                else
                    code <- journal.CommitCycle(required (step.GetProperty("commit").GetString())).Code
            let snapshot = journal.Snapshot
            { Code = code
              Phase = snapshot.Phase
              Committed = snapshot.Cycles |> List.map _.Code |> join
              NextCycleIndex = snapshot.NextCycleIndex
              Gaps = snapshot.Gaps.Length
              Interruptions = snapshot.InterruptionCount
              Retries = snapshot.RetryCount }
        finally
            if Directory.Exists root then Directory.Delete(root, true)

    let expected (vector: JsonElement) =
        { Code = required (vector.GetProperty("code").GetString())
          Phase = required (vector.GetProperty("phase").GetString())
          Committed =
            [ for value in vector.GetProperty("committed").EnumerateArray() -> required (value.GetString()) ]
            |> join
          NextCycleIndex = vector.GetProperty("nextCycleIndex").GetInt32()
          Gaps = vector.GetProperty("gaps").GetInt32()
          Interruptions = vector.GetProperty("interruptions").GetInt32()
          Retries = vector.GetProperty("retries").GetInt32() }

    [<Test>]
    member _.``shared CBI62 vectors resume a governed cadence``() =
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let name = vector.GetProperty "name" |> _.GetString()
            Assert.That(runVector document.RootElement vector, Is.EqualTo(expected vector), $"vector {name}")

    [<Test>]
    member _.``CBI62 C1 every code in the vocabulary is committable and classified``() =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi62-vocabulary-{Guid.NewGuid():N}")
        try
            // The guard is over the class rather than today's six: a code added to the vocabulary and
            // left out of the journal fails here, which is what CBI61's two additions did not do.
            Assert.That(ProviderServingTrustCycleCodes.all, Is.Not.Empty)
            for code in ProviderServingTrustCycleCodes.all do
                let journal = establish (Path.Combine(root, $"{code}.bin")) 4 (TimeSpan.FromSeconds 5.0)
                Assert.That(journal.BeginCycle().Code, Is.EqualTo "durable-cadence-cycle-started")
                let committed = journal.CommitCycle code
                Assert.That(committed.Code, Is.Not.EqualTo "durable-cadence-result-invalid", code)
                Assert.That(journal.Snapshot.Phase, Is.Not.EqualTo "in-flight", code)
                Assert.That((List.exactlyOne journal.Snapshot.Cycles).Code, Is.EqualTo code, code)
            // A code outside the vocabulary is still refused, so the guard did not become permissive.
            let stray = establish (Path.Combine(root, "stray.bin")) 4 (TimeSpan.FromSeconds 5.0)
            Assert.That(stray.BeginCycle().Code, Is.EqualTo "durable-cadence-cycle-started")
            Assert.That(stray.CommitCycle("provider-trust-cycle-invented").Code,
                        Is.EqualTo "durable-cadence-result-invalid")
        finally
            if Directory.Exists root then Directory.Delete(root, true)

    [<Test>]
    member _.``CBI62 C2 the run outcome never renames the cycle outcome``() =
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let actual = runVector document.RootElement vector
            let want =
                [ for value in vector.GetProperty("committed").EnumerateArray() -> required (value.GetString()) ]
                |> join
            Assert.That(actual.Committed, Is.EqualTo want, vector.GetProperty("name").GetString())

    [<Test>]
    member _.``CBI62 C3 the journal says nothing about which loop ran``() = task {
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi62-loops-{Guid.NewGuid():N}")
        try
            use pin = ECDsa.Create ECCurve.NamedCurves.nistP256
            use successor = ECDsa.Create ECCurve.NamedCurves.nistP256
            use endpoint = ECDsa.Create ECCurve.NamedCurves.nistP256
            let images = ResizeArray<byte array>()
            let generations = ResizeArray<int64>()
            // Two runs identical in every journal-visible respect and differing only in whether the
            // rotation reached its endpoint. A journal that recorded which loop ran would differ.
            for rotationReachable in [ true; false ] do
                let directory = Path.Combine(root, (if rotationReachable then "reached" else "unreached"))
                let journalPath = Path.Combine(directory, "cadence.bin")
                let journal = establish journalPath 3 (TimeSpan.FromSeconds 5.0)
                let cycle, durable, _ = compose directory pin successor endpoint rotationReachable
                Assert.That(journal.BeginCycle().Code, Is.EqualTo "durable-cadence-cycle-started")
                // The cycle runs and the process then dies before any commit.
                let! _ = cycle start CancellationToken.None
                images.Add(File.ReadAllBytes journalPath)
                generations.Add durable.AuthorityGeneration
            Assert.Multiple(Action(fun () ->
                Assert.That(images[0], Is.EqualTo<byte> images[1],
                            "the journal must hold no field that distinguishes the two runs")
                // The difference is real and is recorded where it can be trusted: the retained chain.
                Assert.That(generations[0], Is.EqualTo 1L)
                Assert.That(generations[1], Is.EqualTo 0L)))
        finally
            if Directory.Exists root then Directory.Delete(root, true)
    }

    [<Test>]
    member _.``CBI62 C4 retrying an interrupted governed cycle replays neither half``() = task {
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi62-retry-{Guid.NewGuid():N}")
        try
            use pin = ECDsa.Create ECCurve.NamedCurves.nistP256
            use successor = ECDsa.Create ECCurve.NamedCurves.nistP256
            use endpoint = ECDsa.Create ECCurve.NamedCurves.nistP256
            let journalPath = Path.Combine(root, "cadence.bin")
            let journal = establish journalPath 3 (TimeSpan.FromSeconds 5.0)
            let cycle, durable, endpoints = compose root pin successor endpoint true

            Assert.That(journal.BeginCycle().Code, Is.EqualTo "durable-cadence-cycle-started")
            let! first = cycle start CancellationToken.None
            let appliedGeneration = durable.AuthorityGeneration
            let appliedSequence = durable.Current |> Option.map _.Sequence |> Option.defaultValue 0L

            // The process dies after both halves took effect and before the commit.
            let reopened = DurableProviderTrustCadenceJournal.Open(journalPath, runIdentity)
            let resumed = reopened.Journal.Value
            Assert.That(reopened.Code, Is.EqualTo "durable-cadence-indeterminate")
            Assert.That(resumed.ResolveInterrupted(Retry).Code, Is.EqualTo "durable-cadence-retry-ready")
            Assert.That(resumed.BeginCycle().Code, Is.EqualTo "durable-cadence-cycle-started")

            // The honest path: the host's own cursor moved, so both endpoints answer that it is
            // current and the retry has nothing to re-apply.
            let! retried = cycle start CancellationToken.None

            // The defensive path: a stale endpoint re-offers the identical statement and update, and
            // both are refused by the ordinary generation and sequence rules.
            endpoints.Replay <- true
            let! replayed = cycle start CancellationToken.None

            Assert.Multiple(Action(fun () ->
                Assert.That(first.Code, Is.EqualTo ProviderServingTrustCycleCodes.Current)
                Assert.That(appliedGeneration, Is.EqualTo 1L)
                Assert.That(appliedSequence, Is.EqualTo 1L)
                Assert.That(retried.Code, Is.EqualTo ProviderServingTrustCycleCodes.Current)
                Assert.That(retried.Rotation.Value.IsCurrent, Is.True)
                // Neither half double-applies on either path, and neither needed to know about the
                // interruption to refuse.
                Assert.That(replayed.Rotation.Value.LastAttemptCode,
                            Is.EqualTo(Some "policy-authority-generation-invalid"))
                Assert.That(replayed.Poll.Value.LastAttemptCode,
                            Is.EqualTo(Some "policy-update-sequence-invalid"))
                Assert.That(durable.AuthorityGeneration, Is.EqualTo appliedGeneration)
                Assert.That(durable.Current |> Option.map _.Sequence |> Option.defaultValue 0L,
                            Is.EqualTo appliedSequence)
                Assert.That(resumed.Snapshot.RetryCount, Is.EqualTo 1)))
        finally
            if Directory.Exists root then Directory.Delete(root, true)
    }

    [<Test>]
    member _.``CBI62 C5 an ungoverned cadence reaches the same terminal states``() =
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi62-ungoverned-{Guid.NewGuid():N}")
        try
            // The four codes CBI48 always knew keep their exact terminal mapping.
            for code, terminal in
                [ ProviderServingTrustCycleCodes.Current, "durable-cadence-cycle-committed"
                  ProviderServingTrustCycleCodes.Withdrawn, "durable-cadence-cycle-committed"
                  ProviderServingTrustCycleCodes.Stopped, "durable-cadence-stopped"
                  ProviderServingTrustCycleCodes.Canceled, "durable-cadence-canceled" ] do
                let journal = establish (Path.Combine(root, $"{code}.bin")) 4 (TimeSpan.FromSeconds 5.0)
                Assert.That(journal.BeginCycle().Code, Is.EqualTo "durable-cadence-cycle-started")
                Assert.That(journal.CommitCycle(code).Code, Is.EqualTo terminal, code)
                Assert.That((List.exactlyOne journal.Snapshot.Cycles).Code, Is.EqualTo code, code)
        finally
            if Directory.Exists root then Directory.Delete(root, true)
