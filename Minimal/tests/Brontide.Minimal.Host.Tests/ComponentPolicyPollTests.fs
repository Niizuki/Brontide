namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi41Source(
    fetch: ProviderPublisherTrustPolicyDistributionRequest -> CancellationToken ->
        Task<ProviderPublisherTrustPolicyDistributionResponse>) =
    let mutable attempts = 0
    member _.Attempts = attempts
    interface IProviderPublisherTrustPolicyDistributionSource with
        member _.FetchAsync(request, cancellationToken) =
            attempts <- attempts + 1
            fetch request cancellationToken

type private Cbi41Observation =
    { Code: string
      LastAttemptCode: string option
      Attempts: int
      Delays: int list
      Applied: int64 list
      Retained: int64 list
      FinalSequence: int64
      PublicationPreceded: bool list
      RequestedGaps: int list
      SourceAttempts: int }

[<TestFixture>]
type ComponentPolicyPollTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI41 fixture value was missing." | present -> present

    let optional (value: string | null) =
        match value with null -> None | present -> Some present

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi41-policy-poll-vectors.json")))

    // NUnit's EqualTo overloads are ambiguous for an F# list and its object overload compares by
    // identity rather than element by element, so sequences are compared as their rendered form.
    let joined (values: 'a seq) = values |> Seq.map string |> String.concat ","

    let numbers (element: JsonElement) name =
        [ for value in element.GetProperty(name: string).EnumerateArray() -> value.GetInt32() ]

    let sequences (element: JsonElement) name =
        [ for value in element.GetProperty(name: string).EnumerateArray() -> value.GetInt64() ]

    let authorityId (authority: ECDsa) =
        authority.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyAuthorityId.create

    let endpointId (endpoint: ECDsa) =
        endpoint.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
        |> ProviderPublisherTrustPolicyDistributionEndpointId.create

    let policyFor (index: int64) =
        let entries =
            [ { PublisherKeyId = ProviderPublisherKeyId.create (index.ToString "X64"); Disposition = Admitted } ]
        { Identity = ProviderPublisherTrustPolicyIdentity.compute entries; Entries = entries }

    let signUpdate (key: ECDsa) sequence previous =
        let selected = policyFor sequence
        let publicKey = key.ExportSubjectPublicKeyInfo()
        let signature = key.SignData(
            ProviderPublisherTrustPolicyUpdateManifest.encode sequence previous selected.Identity,
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
        { Sequence = sequence; PreviousPolicyIdentity = previous; Policy = selected
          Algorithm = "ECDSA-P256-SHA256"
          AuthorityPublicKeySpkiBase64 = Convert.ToBase64String publicKey
          SignatureBase64 = Convert.ToBase64String signature }

    let respond kind (request: ProviderPublisherTrustPolicyDistributionRequest)
        (endpointKey: ECDsa) (otherEndpointKey: ECDsa) (authority: ECDsa) (foreignAuthority: ECDsa)
        (now: DateTimeOffset) =
        let update =
            match kind with
            | "update" -> Some(signUpdate authority (request.CurrentSequence + 1L) request.CurrentPolicyIdentity)
            | "foreign-authority" ->
                Some(signUpdate foreignAuthority (request.CurrentSequence + 1L) request.CurrentPolicyIdentity)
            | _ -> None
        let issued = if kind = "stale" then now.AddMinutes -2.0 else now
        let expires = if kind = "stale" then now.AddMinutes -1.0 else issued.AddMinutes 1.0
        let signer = if kind = "endpoint-mismatch" then otherEndpointKey else endpointKey
        let publicKey = signer.ExportSubjectPublicKeyInfo()
        let signature = signer.SignData(
            ProviderPublisherTrustPolicyDistributionManifest.encode request.Challenge request.CurrentSequence
                request.CurrentPolicyIdentity (issued.ToUnixTimeSeconds()) (expires.ToUnixTimeSeconds()) update,
            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
        let response =
            { Challenge = request.Challenge; CurrentSequence = request.CurrentSequence
              CurrentPolicyIdentity = request.CurrentPolicyIdentity
              IssuedAtUnixSeconds = issued.ToUnixTimeSeconds(); ExpiresAtUnixSeconds = expires.ToUnixTimeSeconds()
              Update = update; Algorithm = "ECDSA-P256-SHA256"
              EndpointPublicKeySpkiBase64 = Convert.ToBase64String publicKey
              SignatureBase64 = Convert.ToBase64String signature }
        if kind = "signature-invalid" then
            let changed = Convert.FromBase64String response.SignatureBase64
            changed[changed.Length - 1] <- changed[changed.Length - 1] ^^^ 1uy
            { response with SignatureBase64 = Convert.ToBase64String changed }
        else response

    let scheduleOf (element: JsonElement) =
        let span name = TimeSpan.FromMilliseconds(float (element.GetProperty(name: string).GetInt32()))
        ProviderPublisherTrustPolicyPollSchedule.create
            (element.GetProperty("maximumAttempts").GetInt32())
            (span "baseDelayMilliseconds")
            (element.GetProperty("backoffMultiplier").GetInt32())
            (span "maximumDelayMilliseconds")
            (span "attemptTimeoutMilliseconds")

    let run (vector: JsonElement) (schedule: JsonElement) = task {
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi41-{Guid.NewGuid():N}")
        try
            use authority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use foreignAuthority = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use endpointKey = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            use otherEndpointKey = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            let authorityIdentity = authorityId authority
            let checkpoint = Path.Combine(root, "policy.checkpoint")
            let _, opened, _ =
                DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, authorityIdentity, None)
            let durable = opened.Value
            let now = DateTimeOffset.FromUnixTimeSeconds 1800000000L

            let responses =
                [| for value in vector.GetProperty("responses").EnumerateArray() -> value.GetString() |> required |]
            let served = ref 0
            let source = Cbi41Source(fun request _ -> task {
                let kind = responses[min served.Value (responses.Length - 1)]
                served.Value <- served.Value + 1
                if kind = "transport" then raise (IOException "The distribution endpoint is unavailable.")
                let effective =
                    if kind = "superseded" then
                        // Another writer advances the registry while the attempt is in flight, which
                        // is the only way CBI39's superseded cursor is reachable.
                        durable.Apply(
                            signUpdate authority (request.CurrentSequence + 1L) request.CurrentPolicyIdentity)
                        |> ignore
                        "current"
                    else kind
                return respond effective request endpointKey otherEndpointKey authority foreignAuthority now })

            use cancellation = new CancellationTokenSource()
            let cancel = vector.GetProperty("cancel").GetString() |> required
            let requested = ResizeArray<TimeSpan>()
            let delay: ProviderPublisherTrustPolicyPollDelay =
                fun instant duration token ->
                    if cancel = "in-backoff" then
                        cancellation.Cancel()
                        raise (OperationCanceledException token)
                    requested.Add duration
                    Task.FromResult(instant + duration)

            let preceded = ResizeArray<bool>()
            let sinkFails = vector.GetProperty("sinkFails").GetBoolean()
            let floorSink: ProviderPublisherTrustPolicyFloorSink =
                fun floor _ ->
                    // C4 is an ordering claim, so it is observed here rather than described:
                    // reopening the checkpoint proves the update the floor names is already durable
                    // when the floor arrives.
                    let _, reopened, _ =
                        DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, authorityIdentity, None)
                    let published =
                        reopened |> Option.bind _.Current |> Option.map _.Sequence |> Option.defaultValue 0L
                    preceded.Add(published >= floor.Sequence)
                    if sinkFails then raise (IOException "The floor sink is unavailable.")
                    Task.CompletedTask

            if cancel = "before" then cancellation.Cancel()
            let poller =
                ProviderPublisherTrustPolicyPoller(durable, endpointId endpointKey, scheduleOf schedule)
            let! result = poller.PollAsync(source, floorSink, delay, now, cancellation.Token)
            return
                { Code = result.Code
                  LastAttemptCode = result.LastAttemptCode
                  Attempts = result.Attempts
                  Delays = result.Delays |> List.map (fun value -> int value.TotalMilliseconds)
                  Applied = result.AppliedSequences
                  Retained = result.RetainedSequences
                  FinalSequence = durable.Current |> Option.map _.Sequence |> Option.defaultValue 0L
                  PublicationPreceded = List.ofSeq preceded
                  RequestedGaps = requested |> Seq.map (fun value -> int value.TotalMilliseconds) |> List.ofSeq
                  SourceAttempts = source.Attempts }
        finally
            if Directory.Exists root then Directory.Delete(root, true)
    }

    let runIndex (document: JsonDocument) (index: int) =
        run (document.RootElement.GetProperty("vectors")[index]) (document.RootElement.GetProperty "schedule")

    [<Test>]
    member _.``shared CBI41 vectors run one bounded cycle``() = task {
        use document = fixture ()
        let schedule = document.RootElement.GetProperty "schedule"
        let budget = schedule.GetProperty("maximumAttempts").GetInt32()
        let cap = schedule.GetProperty("maximumDelayMilliseconds").GetInt32()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run vector schedule
            let label = vector.GetProperty("mutation").GetString() |> required
            Assert.Multiple(Action(fun () ->
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()), label)
                Assert.That(actual.LastAttemptCode,
                    Is.EqualTo(vector.GetProperty("lastAttemptCode").GetString() |> optional), label)
                Assert.That(actual.Attempts, Is.EqualTo(vector.GetProperty("attempts").GetInt32()), label)
                Assert.That(joined actual.Delays, Is.EqualTo(joined (numbers vector "delaysMilliseconds")), label)
                Assert.That(joined actual.Applied, Is.EqualTo(joined (sequences vector "appliedSequences")), label)
                Assert.That(joined actual.Retained, Is.EqualTo(joined (sequences vector "retainedSequences")), label)
                Assert.That(actual.FinalSequence,
                    Is.EqualTo(vector.GetProperty("finalSequence").GetInt64()), label)

                // Phase-wide properties, over every vector rather than per case.
                Assert.That(actual.Attempts, Is.LessThanOrEqualTo(budget), label)
                Assert.That(actual.Delays, Has.Length.EqualTo(max (actual.Attempts - 1) 0), label)
                Assert.That(actual.Delays, Is.All.LessThanOrEqualTo(cap), label)
                Assert.That(joined actual.Delays, Is.EqualTo(joined actual.RequestedGaps), label)
                Assert.That(joined actual.Retained,
                    Is.EqualTo(joined (actual.Applied |> List.truncate actual.Retained.Length)), label)
                Assert.That(actual.Applied, Is.Ordered.Ascending.And.Unique, label)
                Assert.That(actual.PublicationPreceded, Is.All.True, label)
                if not (vector.GetProperty("externalWrite").GetBoolean()) then
                    Assert.That(actual.FinalSequence,
                        Is.EqualTo(if actual.Applied.IsEmpty then 0L else List.last actual.Applied), label)))
    }

    [<Test>]
    member _.``CBI41 C1 a cycle advances until the endpoint reports the host current``() = task {
        use document = fixture ()
        let! chain = runIndex document 2
        let! already = runIndex document 0
        Assert.Multiple(Action(fun () ->
            Assert.That(chain.Code, Is.EqualTo("policy-poll-current"))
            Assert.That(joined chain.Applied, Is.EqualTo("1,2"))
            Assert.That(chain.FinalSequence, Is.EqualTo(2L))
            Assert.That(chain.SourceAttempts, Is.EqualTo(3))
            // Nothing to do is a cycle too, and it costs exactly one attempt.
            Assert.That(already.Applied, Is.Empty)
            Assert.That(already.SourceAttempts, Is.EqualTo(1))))
    }

    [<Test>]
    member _.``CBI41 C2 backoff is deterministic bounded and reset by progress``() = task {
        use document = fixture ()
        let schedule = scheduleOf (document.RootElement.GetProperty "schedule")
        let expected = numbers document.RootElement "backoffMilliseconds"
        expected
        |> List.iteri (fun index value ->
            Assert.That(int (schedule.DelayForConsecutiveFailures(index + 1)).TotalMilliseconds,
                Is.EqualTo(value), $"consecutive failures {index + 1}"))
        Assert.That(schedule.DelayForConsecutiveFailures 0, Is.EqualTo(TimeSpan.Zero))

        // Progress resets the count: the gap after the applied update is zero, and the failure that
        // follows it starts again at the base delay rather than continuing the earlier ramp.
        let! resets = runIndex document 4
        Assert.That(joined resets.Delays, Is.EqualTo("1000,0,1000"))

        let second = TimeSpan.FromSeconds 1.0
        Assert.Multiple(Action(fun () ->
            Assert.Throws<ArgumentException>(Action(fun () ->
                ProviderPublisherTrustPolicyPollSchedule.create 0 second 2 (TimeSpan.FromSeconds 10.0) second
                |> ignore)) |> ignore
            Assert.Throws<ArgumentException>(Action(fun () ->
                ProviderPublisherTrustPolicyPollSchedule.create 3 (TimeSpan.FromSeconds 20.0) 2
                    (TimeSpan.FromSeconds 10.0) second |> ignore)) |> ignore
            Assert.Throws<ArgumentException>(Action(fun () ->
                ProviderPublisherTrustPolicyPollSchedule.create 3 second 2 (TimeSpan.FromSeconds 10.0)
                    (TimeSpan.FromMinutes 2.0) |> ignore)) |> ignore))
    }

    [<Test>]
    member _.``CBI41 C3 a terminal outcome ends the cycle at its own attempt``() = task {
        use document = fixture ()
        for index in [ 8; 9; 10 ] do
            let! actual = runIndex document index
            Assert.Multiple(Action(fun () ->
                Assert.That(actual.Code, Is.EqualTo("policy-poll-refused"))
                Assert.That(actual.Attempts, Is.EqualTo(1))
                Assert.That(actual.SourceAttempts, Is.EqualTo(1))
                Assert.That(actual.Delays, Is.Empty)
                Assert.That(actual.FinalSequence, Is.EqualTo(0L))))
    }

    [<Test>]
    member _.``CBI41 C4 the floor is handed off after publication and never before``() = task {
        use document = fixture ()
        let! chain = runIndex document 2
        Assert.Multiple(Action(fun () ->
            Assert.That(joined chain.PublicationPreceded, Is.EqualTo("True,True"))
            Assert.That(joined chain.Retained, Is.EqualTo("1,2"))
            Assert.That(chain.Retained, Is.Ordered.Ascending.And.Unique)))
    }

    [<Test>]
    member _.``CBI41 C5 a refused handoff stops the cycle and reports the unretained floor``() = task {
        use document = fixture ()
        let! actual = runIndex document 13
        Assert.Multiple(Action(fun () ->
            Assert.That(actual.Code, Is.EqualTo("policy-poll-floor-unretained"))
            // The update is not undone, because it is already durable, and nothing advances past it.
            Assert.That(joined actual.Applied, Is.EqualTo("1"))
            Assert.That(actual.Retained, Is.Empty)
            Assert.That(actual.FinalSequence, Is.EqualTo(1L))
            Assert.That(actual.SourceAttempts, Is.EqualTo(1))))
    }

    [<Test>]
    member _.``CBI41 C6 cancellation is observed before every attempt and inside every gap``() = task {
        use document = fixture ()
        let! before = runIndex document 11
        let! during = runIndex document 12
        Assert.Multiple(Action(fun () ->
            Assert.That(before.Code, Is.EqualTo("policy-poll-canceled"))
            Assert.That(before.Attempts, Is.EqualTo(0))
            Assert.That(before.SourceAttempts, Is.EqualTo(0))
            Assert.That(during.Code, Is.EqualTo("policy-poll-canceled"))
            Assert.That(during.Attempts, Is.EqualTo(1))
            // A gap that was cancelled was never waited, so it is not recorded.
            Assert.That(during.Delays, Is.Empty)))
    }

    [<Test>]
    member _.``CBI41 C7 both roots agree on cycle observations``() = task {
        use document = fixture ()
        let schedule = document.RootElement.GetProperty "schedule"
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run vector schedule
            let projection =
                String.concat "|"
                    [ actual.Code; actual.LastAttemptCode |> Option.defaultValue "-"; string actual.Attempts
                      joined actual.Delays; joined actual.Applied; joined actual.Retained
                      string actual.FinalSequence ]
            let expected =
                String.concat "|"
                    [ vector.GetProperty("code").GetString() |> required
                      vector.GetProperty("lastAttemptCode").GetString() |> optional |> Option.defaultValue "-"
                      string (vector.GetProperty("attempts").GetInt32())
                      joined (numbers vector "delaysMilliseconds")
                      joined (sequences vector "appliedSequences")
                      joined (sequences vector "retainedSequences")
                      string (vector.GetProperty("finalSequence").GetInt64()) ]
            Assert.That(projection, Is.EqualTo(expected), vector.GetProperty("mutation").GetString())
    }
