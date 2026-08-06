namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Brontide.Minimal.Host
open NUnit.Framework

type private Cbi61Observation =
    { CadenceCode: string
      CycleCodes: string
      RotationCodes: string
      PollCodes: string
      Polled: string
      FinalGeneration: int64
      FinalSequence: int64 }

/// Holds the scripted behaviour of one cadence run. Both endpoints read the cycle index from it, so
/// a vector states what each channel does per cycle rather than per call.
type private Cbi61Script(cycles: (string * string) list) =
    let mutable cycle = 0
    member _.Cycle with get () = cycle and set value = cycle <- value
    member val RotationServedThisCycle = false with get, set
    member val PolicyServedThisCycle = false with get, set
    member _.Rotation = fst cycles[min cycle (cycles.Length - 1)]
    member _.Policy = snd cycles[min cycle (cycles.Length - 1)]

[<TestFixture>]
type ComponentGovernedTrustCadenceTests() =
    let required (value: string | null) =
        match value with null -> failwith "A CBI61 fixture value was missing." | present -> present

    let fixture () = JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi61-governed-trust-cadence-vectors.json")))

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

    let statement generation (previous: ECDsa) (next: ECDsa) =
        let previousId = authorityId previous
        let nextId = authorityId next
        let manifest =
            ProviderPolicyAuthorityRotationManifest.encode (max generation 1L) 0L None previousId nextId
        let sign (key: ECDsa) =
            key.SignData(manifest, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            |> Convert.ToBase64String
        { Generation = generation; PolicySequence = 0L; PolicyIdentity = None
          PreviousAuthority = previousId; NextAuthority = nextId; Algorithm = "ECDSA-P256-SHA256"
          PreviousAuthorityPublicKeySpkiBase64 = previous.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
          NextAuthorityPublicKeySpkiBase64 = next.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
          PreviousSignatureBase64 = sign previous; NextSignatureBase64 = sign next }

    let rotationResponse mutation (request: ProviderPolicyAuthorityRotationDistributionRequest)
        (endpoint: ECDsa) rotation (now: DateTimeOffset) =
        let unsigned =
            { Challenge = request.Challenge
              PolicySequence = request.PolicySequence
              PolicyIdentity = request.PolicyIdentity
              AuthorityGeneration = request.AuthorityGeneration
              ActiveAuthority = request.ActiveAuthority
              IssuedAtUnixSeconds = now.ToUnixTimeSeconds()
              ExpiresAtUnixSeconds = now.AddMinutes(1.0).ToUnixTimeSeconds()
              Rotation = if mutation = "rotate" then Some rotation else None
              Algorithm = "ECDSA-P256-SHA256"
              EndpointPublicKeySpkiBase64 = endpoint.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
              SignatureBase64 = "" }
        let signature =
            endpoint.SignData(
                ProviderPolicyAuthorityRotationDistributionManifest.encode unsigned,
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            |> Convert.ToBase64String
        { unsigned with SignatureBase64 = signature }

    let rotationSource (script: Cbi61Script) (keys: ECDsa list) (endpoint: ECDsa) (other: ECDsa)
        now (cancellation: CancellationTokenSource) =
        { new IProviderPolicyAuthorityRotationDistributionSource with
            member _.FetchAsync(request, _) =
                let mutation = script.Rotation
                if mutation = "transport" then raise (IOException "unavailable")
                if mutation = "canceled" then
                    cancellation.Cancel()
                    raise (OperationCanceledException cancellation.Token)
                let index = int request.AuthorityGeneration
                let rotation = statement (request.AuthorityGeneration + 1L) keys[index] keys[index + 1]
                // One pending rotation per cycle, then the endpoint reports the host current. CBI60
                // continues after progress, so an endpoint that kept offering successors would
                // rotate repeatedly inside one cycle, which no publisher does.
                let offers =
                    (mutation = "rotate" || mutation = "rotate-unretained")
                    && not script.RotationServedThisCycle
                if offers then script.RotationServedThisCycle <- true
                let kind =
                    if offers then "rotate"
                    elif mutation = "rotate" || mutation = "rotate-unretained" then "current"
                    else mutation
                let key = if mutation = "endpoint" then other else endpoint
                Task.FromResult(rotationResponse kind request key rotation now) }

    let policySource (script: Cbi61Script) (pin: ECDsa) (successor: ECDsa) (foreign: ECDsa)
        (endpoint: ECDsa) (other: ECDsa) (now: DateTimeOffset) =
        { new IProviderPublisherTrustPolicyDistributionSource with
            member _.FetchAsync(request, _) =
                let mutation = script.Policy
                // One update per cycle, then the endpoint reports the host current. A real endpoint
                // answers relative to the cursor it is given, and this keeps a cycle from applying
                // the same sequence twice while CBI41 continues after progress.
                let offers =
                    (mutation = "update" || mutation = "successor-update" || mutation = "foreign-update")
                    && not script.PolicyServedThisCycle
                if offers then script.PolicyServedThisCycle <- true
                let signer =
                    match mutation with
                    | "successor-update" -> successor
                    | "foreign-update" -> foreign
                    | _ -> pin
                let update =
                    if offers then
                        Some(signUpdate signer (request.CurrentSequence + 1L) request.CurrentPolicyIdentity
                                 (policyFor (request.CurrentSequence + 1L)))
                    else None
                let issued = now
                let expires = issued.AddMinutes 1.0
                let responder = if mutation = "endpoint" then other else endpoint
                let signature =
                    responder.SignData(
                        ProviderPublisherTrustPolicyDistributionManifest.encode
                            request.Challenge request.CurrentSequence request.CurrentPolicyIdentity
                            (issued.ToUnixTimeSeconds()) (expires.ToUnixTimeSeconds()) update,
                        HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                    |> Convert.ToBase64String
                Task.FromResult(
                    { Challenge = request.Challenge
                      CurrentSequence = request.CurrentSequence
                      CurrentPolicyIdentity = request.CurrentPolicyIdentity
                      IssuedAtUnixSeconds = issued.ToUnixTimeSeconds()
                      ExpiresAtUnixSeconds = expires.ToUnixTimeSeconds()
                      Update = update
                      Algorithm = "ECDSA-P256-SHA256"
                      EndpointPublicKeySpkiBase64 = responder.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                      SignatureBase64 = signature }) }

    let instantDelay: ProviderPolicyAuthorityCycleDelay =
        fun now duration cancellationToken ->
            cancellationToken.ThrowIfCancellationRequested()
            Task.FromResult(now + duration)

    let pollDelay: ProviderPublisherTrustPolicyPollDelay =
        fun now duration cancellationToken ->
            cancellationToken.ThrowIfCancellationRequested()
            Task.FromResult(now + duration)

    let join (values: seq<string>) = String.Join(",", values)

    let run (document: JsonElement) (vector: JsonElement) = task {
        let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi61-{Guid.NewGuid():N}")
        let checkpoint = Path.Combine(root, "policy.checkpoint")
        let keys = [ for _ in 1..4 -> ECDsa.Create ECCurve.NamedCurves.nistP256 ]
        try
            use foreign = ECDsa.Create ECCurve.NamedCurves.nistP256
            use endpoint = ECDsa.Create ECCurve.NamedCurves.nistP256
            use other = ECDsa.Create ECCurve.NamedCurves.nistP256
            let pin = authorityId keys[0]
            let _, opened, _ = DurableProviderPublisherTrustPolicyRegistry.Open(checkpoint, pin, None)
            let durable = opened.Value
            let _, policyFloors =
                DurableProviderPublisherTrustPolicyFloorStore.Open(Path.Combine(root, "policy.floor"), pin)
            let _, authorityFloors =
                DurableProviderPolicyAuthorityFloorStore.Open(Path.Combine(root, "authority.floor"), pin)

            let script =
                Cbi61Script(
                    [ for cycle in vector.GetProperty("cycles").EnumerateArray() ->
                        required (cycle.GetProperty("rotation").GetString()),
                        required (cycle.GetProperty("policy").GetString()) ])
            let now = DateTimeOffset.FromUnixTimeSeconds 1800000000L
            use cancellation = new CancellationTokenSource()

            let authorityFloorSink: ProviderPolicyAuthorityFloorSink =
                fun floor cancellationToken ->
                    if script.Rotation = "rotate-unretained" then
                        raise (IOException "The authority floor store is unavailable.")
                    authorityFloors.Value.Sink floor cancellationToken

            let rotationCycle =
                ProviderGovernedTrustCycle.rotationBinding
                    (ProviderPolicyAuthorityRotationCycle(
                        durable, endpointId endpoint,
                        ProviderPolicyAuthorityCycleSchedule.create
                            2 (TimeSpan.FromMilliseconds 1.0) 2 (TimeSpan.FromMilliseconds 2.0)
                            (TimeSpan.FromSeconds 1.0)))
                    (rotationSource script keys endpoint other now cancellation)
                    authorityFloorSink
                    instantDelay

            let pollCodes = ResizeArray<string>()
            let basePolicy =
                ProviderServingTrustCycleBinding.policy
                    (ProviderPublisherTrustPolicyPoller(
                        durable, endpointId endpoint,
                        ProviderPublisherTrustPolicyPollSchedule.create
                            2 (TimeSpan.FromMilliseconds 1.0) 2 (TimeSpan.FromMilliseconds 2.0)
                            (TimeSpan.FromSeconds 1.0)))
                    (policySource script keys[0] keys[1] foreign endpoint other now)
                    policyFloors.Value.Sink
                    pollDelay
            // Records that the policy endpoint was reached, which is what C2 and C3 observe.
            let policyCycle: ProviderPublisherTrustPolicyCycle =
                fun instant cancellationToken -> task {
                    let! result = basePolicy instant cancellationToken
                    pollCodes.Add result.Code
                    return result
                }
            // CBI47 C4 makes an empty serving set a successful no-op, which keeps this slice's
            // evidence on the rotation-versus-poll interaction rather than on CBI46's members.
            let emptySweep: ProviderServingTrustSweepCycle = fun _ -> Task.FromResult None

            let governed =
                ProviderGovernedTrustCycle.create rotationCycle
                    (ProviderServingTrustCycle.create policyCycle emptySweep)
            let cadenceDelay: ProviderServingTrustCadenceDelay =
                fun instant duration cancellationToken ->
                    script.Cycle <- script.Cycle + 1
                    script.RotationServedThisCycle <- false
                    script.PolicyServedThisCycle <- false
                    cancellationToken.ThrowIfCancellationRequested()
                    Task.FromResult(instant + duration)

            let schedule =
                ProviderServingTrustCadenceSchedule.create
                    (vector.GetProperty("cycles").GetArrayLength())
                    (TimeSpan.FromMilliseconds(
                        float (document.GetProperty("schedule").GetProperty("intervalMilliseconds").GetInt32())))
            let! result =
                ProviderServingTrustCadence.run schedule governed cadenceDelay now cancellation.Token

            return
                { CadenceCode = result.Code
                  CycleCodes = result.Cycles |> List.map _.Result.Code |> join
                  RotationCodes =
                    result.Cycles
                    |> List.map (fun cycle ->
                        cycle.Result.Rotation |> Option.map _.Code |> Option.defaultValue "none")
                    |> join
                  PollCodes =
                    result.Cycles
                    |> List.map (fun cycle ->
                        cycle.Result.Poll |> Option.map _.Code |> Option.defaultValue "none")
                    |> join
                  Polled =
                    result.Cycles
                    |> List.map (fun cycle -> string (Option.isSome cycle.Result.Poll))
                    |> join
                  FinalGeneration = durable.AuthorityGeneration
                  FinalSequence = durable.Current |> Option.map _.Sequence |> Option.defaultValue 0L }
        finally
            for key in keys do key.Dispose()
            if Directory.Exists root then Directory.Delete(root, true)
    }

    let expected (vector: JsonElement) =
        { CadenceCode = required (vector.GetProperty("cadenceCode").GetString())
          CycleCodes =
            [ for value in vector.GetProperty("cycleCodes").EnumerateArray() -> required (value.GetString()) ]
            |> join
          RotationCodes =
            [ for value in vector.GetProperty("rotationCodes").EnumerateArray() -> required (value.GetString()) ]
            |> join
          PollCodes =
            [ for value in vector.GetProperty("pollCodes").EnumerateArray() ->
                if value.ValueKind = JsonValueKind.Null then "none" else required (value.GetString()) ]
            |> join
          Polled =
            [ for value in vector.GetProperty("polled").EnumerateArray() -> string (value.GetBoolean()) ]
            |> join
          FinalGeneration = vector.GetProperty("finalGeneration").GetInt64()
          FinalSequence = vector.GetProperty("finalSequence").GetInt64() }

    let named (document: JsonDocument) name =
        document.RootElement.GetProperty("vectors").EnumerateArray()
        |> Seq.find (fun vector -> vector.GetProperty("name").GetString() = name)

    [<Test>]
    member _.``shared CBI61 vectors govern the trust cadence``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run document.RootElement vector
            let name = vector.GetProperty "name" |> _.GetString()
            Assert.That(actual, Is.EqualTo(expected vector), $"vector {name}")
    }

    [<Test>]
    member _.``CBI61 C1 rotating before polling is what lets a successor signed update apply``() = task {
        use document = fixture ()
        let! actual = run document.RootElement (named document "rotation-precedes-poll")
        // Polling first would refuse this update as an authority mismatch, so the applied sequence
        // is what distinguishes the two orders rather than a comment claiming one.
        Assert.Multiple(Action(fun () ->
            Assert.That(actual.FinalGeneration, Is.EqualTo 1L)
            Assert.That(actual.FinalSequence, Is.EqualTo 1L)
            Assert.That(actual.CadenceCode, Is.EqualTo "provider-trust-cadence-complete")))
    }

    [<Test>]
    member _.``CBI61 C2 a rotation that changed nothing still reaches the poll``() = task {
        use document = fixture ()
        for name in [ "refused-rotation-does-not-stop-the-cadence"
                      "exhausted-rotation-does-not-stop-the-cadence" ] do
            let! actual = run document.RootElement (named document name)
            Assert.That(actual.CadenceCode, Is.EqualTo "provider-trust-cadence-complete", name)
            Assert.That(actual.Polled.Split ',' |> Array.forall (fun value -> value = "True"), Is.True, name)
            Assert.That(actual.FinalGeneration, Is.EqualTo 0L, name)
    }

    [<Test>]
    member _.``CBI61 C3 an unretained authority floor stops before the policy endpoint``() = task {
        use document = fixture ()
        let! actual = run document.RootElement (named document "unretained-floor-stops-before-the-poll")
        Assert.Multiple(Action(fun () ->
            Assert.That(actual.CycleCodes, Is.EqualTo "provider-trust-cycle-authority-unretained")
            Assert.That(actual.Polled, Is.EqualTo "False")
            Assert.That(actual.PollCodes, Is.EqualTo "none")
            // The rotation is durable; only its guard is behind.
            Assert.That(actual.FinalGeneration, Is.EqualTo 1L)
            Assert.That(actual.CadenceCode, Is.EqualTo "provider-trust-cadence-stopped")))
    }

    [<Test>]
    member _.``CBI61 C4 neither loop reports the other loop's code``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run document.RootElement vector
            let name = vector.GetProperty "name" |> _.GetString()
            for code in actual.RotationCodes.Split ',' |> Array.filter (fun value -> value <> "none") do
                Assert.That(code, Does.StartWith "policy-authority-cycle-", name)
            for code in actual.PollCodes.Split ',' |> Array.filter (fun value -> value <> "none") do
                Assert.That(code, Does.StartWith "policy-poll-", name)
    }

    [<Test>]
    member _.``CBI61 C5 an authority mismatch is attributed only when a rotation is incomplete``() = task {
        use document = fixture ()
        let! behind = run document.RootElement (named document "authority-behind-is-attributed")
        let! foreign = run document.RootElement (named document "foreign-authority-is-not-attributed")
        let! unrelated = run document.RootElement (named document "policy-refusal-stops-with-its-own-code")
        Assert.Multiple(Action(fun () ->
            // The two differ only in what the rotation reported; the poll code is identical.
            Assert.That(behind.PollCodes, Is.EqualTo foreign.PollCodes)
            Assert.That(behind.CycleCodes, Is.EqualTo "provider-trust-cycle-authority-behind")
            Assert.That(foreign.CycleCodes, Is.EqualTo "provider-trust-cycle-stopped")
            // A poll refused for an unrelated reason is never attributed, even though its rotation
            // also reported current.
            Assert.That(unrelated.CycleCodes, Is.EqualTo "provider-trust-cycle-stopped")))
    }

    [<Test>]
    member _.``CBI61 C6 rotation cancellation reaches no policy endpoint``() = task {
        use document = fixture ()
        let! actual = run document.RootElement (named document "rotation-cancellation-precedes-any-poll")
        Assert.Multiple(Action(fun () ->
            Assert.That(actual.CadenceCode, Is.EqualTo "provider-trust-cadence-canceled")
            Assert.That(actual.Polled, Is.EqualTo "False")
            Assert.That(actual.FinalGeneration, Is.EqualTo 0L)))
    }

    [<Test>]
    member _.``CBI61 C7 a cadence never exceeds its budget and stops at its last cycle``() = task {
        use document = fixture ()
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let! actual = run document.RootElement vector
            let budget = vector.GetProperty("cycles").GetArrayLength()
            let codes = actual.CycleCodes.Split ','
            let name = vector.GetProperty "name" |> _.GetString()
            Assert.That(codes, Has.Length.LessThanOrEqualTo budget, name)
            for code in codes[.. codes.Length - 2] do
                Assert.That(
                    code,
                    Is.EqualTo("provider-trust-cycle-current")
                        .Or.EqualTo("provider-trust-cycle-withdrawn"),
                    name)
    }
