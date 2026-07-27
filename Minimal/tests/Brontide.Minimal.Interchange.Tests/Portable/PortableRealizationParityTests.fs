namespace Brontide.Minimal.Interchange.Tests.Portable

open System.Text
open System.Threading
open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// One request, stated once, so the two realizations execute the same vector rather than two
/// similar ones.
///
/// The scenario is data, not a function producing a request. A function could quietly send a
/// different request to each realization and still report parity; a record cannot, because both
/// runs read the same fields. The handler alone is a factory, because each realization needs its
/// own provider state.
[<ReferenceEquality>]
type ParityScenario =
    { Id: string
      Vectors: string list
      ChannelVectors: string list
      Contract: ContractDocument
      Handler: unit -> IPortableOperationHandler
      ProviderArguments: string list
      Operation: PortableOperationRef
      InputShape: PortableShapeRef
      Input: PortableValue
      Authority: PortableConstraint
      Resources: PortableResource list
      Frame: FrameDecision
      Result: ResultClass
      Category: ProtocolCategory option }

    override this.ToString() = this.Id

/// The PB4 parity matrix: every scenario runs in the fixed direct-call realization and in the
/// negotiated process realization, and the two must report the same category-level profile.
///
/// PB3 measured parity for a success, a denial, and a resource. That left the interesting half
/// unmeasured: the refusals, whose decision point genuinely moves between the realizations. The
/// matrix below covers each portable result class the host can reach — success, shaped failure,
/// denial, and protocol rejection — because a parity claim that skips rejections claims very little.
[<RequireQualifiedAccess>]
module PortableParityMatrix =

    let private denied =
        PortableConstraint.AllOf
            [ PortableConstraint.Atom PortableTruth.Satisfied
              PortableConstraint.Atom PortableTruth.Unsatisfied ]

    let private unknown =
        PortableConstraint.AllOf
            [ PortableConstraint.Atom PortableTruth.Satisfied
              PortableConstraint.Atom PortableTruth.Unknown ]

    let private anyOfPermits =
        PortableConstraint.AnyOf
            [ PortableConstraint.Atom PortableTruth.Satisfied
              PortableConstraint.Atom PortableTruth.Unknown ]

    let private cooling
        id
        vectors
        channelVectors
        input
        frame
        result
        (category, operation, inputShape, authority, resources)
        =
        { Id = id
          Vectors = vectors
          ChannelVectors = channelVectors
          Contract = CoolingFixture.contract
          Handler = fun () -> CoolingHandler() :> IPortableOperationHandler
          ProviderArguments = [ "--portable" ]
          Operation = defaultArg operation CoolingFixture.setEnabled
          InputShape = defaultArg inputShape CoolingFixture.commandV1
          Input = input
          Authority = defaultArg authority permitted
          Resources = defaultArg resources []
          Frame = frame
          Result = result
          Category = category }

    /// The ordinary Cooling defaults: the negotiated Operation, command version 1, a permitted
    /// authority, and no referenced resource.
    let private ordinary = (None, None, None, None, None)

    let private catalog id vectors resource frame result category =
        { Id = id
          Vectors = vectors
          ChannelVectors = []
          Contract = CatalogFixture.contract
          Handler = fun () -> CatalogHandler() :> IPortableOperationHandler
          ProviderArguments = [ "--portable"; "--catalog" ]
          Operation = CatalogFixture.upsert
          InputShape = CatalogFixture.upsertCommand
          Input = CatalogFixture.upsertCommandValue [ CatalogFixture.itemValue "a" "Alpha" [ "one" ] ]
          Authority = permitted
          Resources = [ resource ]
          Frame = frame
          Result = result
          Category = category }

    /// A blob whose declared content hash does not describe its content.
    let private tampered =
        CopiedBlob("profile", Encoding.UTF8.GetBytes "tampered", PortableResource.hashOf (Encoding.UTF8.GetBytes "original"))

    let scenarios =
        [ cooling
              "success"
              [ "PB-58-DIRECT-AND-PROCESS-PARITY-ON-SUCCESS" ]
              [ "CH-01-CORRELATION-ECHO" ]
              (CoolingFixture.authorizedCommand "primary" true)
              FrameDecision.Accept
              ResultClass.OutcomeSucceeded
              ordinary

          cooling
              "additive-projection"
              [ "PB-11-ADDITIVE-PROJECTION"; "PB-57-OBSERVATION-RECORDS-MAPPING-OBLIGATIONS" ]
              [ "CH-07-PAYLOAD-COVARIANCE" ]
              (CoolingFixture.command "primary" true None (Some "operator") (Some "operator"))
              FrameDecision.Accept
              ResultClass.OutcomeSucceeded
              (None, None, Some CoolingFixture.commandV2, None, None)

          cooling
              "strong-kleene-anyof-permits"
              [ "PB-20-STRONG-KLEENE-ANYOF-PERMITS" ]
              [ "CH-09-STRONG-KLEENE-FALLBACK" ]
              (CoolingFixture.authorizedCommand "primary" true)
              FrameDecision.Accept
              ResultClass.OutcomeSucceeded
              (None, None, None, Some anyOfPermits, None)

          cooling
              "strong-kleene-unknown-denies"
              [ "PB-19-AUTHORITY-VALUE-NOT-PROJECTED"; "PB-21-STRONG-KLEENE-ALLOF-DENIES" ]
              [ "CH-08-AUTHORITY-NO-PROJECTION"; "CH-10-STRONG-KLEENE-UNKNOWN-DENIES" ]
              (CoolingFixture.authorizedCommand "primary" true)
              FrameDecision.None
              ResultClass.Denial
              (None, None, None, Some unknown, None)

          cooling
              "local-denial"
              [ "PB-18-LOCAL-DENIAL-EMITS-NO-FRAME"
                "PB-56-OBSERVATION-COMPLETE-ON-DENIAL"
                "PB-59-DIRECT-AND-PROCESS-PARITY-ON-DENIAL" ]
              [ "CH-12-DENIAL-IS-NOT-A-FRAME" ]
              (CoolingFixture.authorizedCommand "primary" true)
              FrameDecision.None
              ResultClass.Denial
              (None, None, None, Some denied, None)

          cooling
              "shaped-failed-outcome"
              [ "PB-47-SEMANTIC-FAILED-OUTCOME" ]
              [ "CH-13-SEMANTIC-FAILED-OUTCOME" ]
              (CoolingFixture.command "primary" true (Some "requested-failure") (Some "operator") None)
              FrameDecision.Accept
              ResultClass.OutcomeFailed
              ordinary

          cooling
              "unsupported-operation"
              [ "PB-46-UNSUPPORTED-OPERATION" ]
              [ "CH-19-UNSUPPORTED-OPERATION" ]
              (CoolingFixture.authorizedCommand "primary" true)
              FrameDecision.Reject
              ResultClass.ProtocolError
              (Some ProtocolCategory.UnsupportedOperation,
               Some(PortableFixtureSupport.operationRef "interchange.tests.cooling.set-disabled" 1),
               None,
               None,
               None)

          cooling
              "missing-required-fragment"
              [ "PB-24-MISSING-REQUIRED-FRAGMENT" ]
              [ "CH-20-INVALID-PAYLOAD" ]
              (CoolingFixture.command "primary" true None None None)
              FrameDecision.Reject
              ResultClass.ProtocolError
              (Some ProtocolCategory.InvalidPayload, None, None, None, None)

          cooling
              "capability-in-body-across-trust"
              [ "PB-22-CAPABILITY-IN-BODY-ACROSS-TRUST" ]
              [ "CH-11-NO-CAPABILITY-TRANSFER" ]
              (CoolingFixture.authorizedCommand "primary" true
               |> PortableRecord.withField "capability" (PortableText "cooling.write"))
              FrameDecision.Reject
              ResultClass.ProtocolError
              (Some ProtocolCategory.InvalidAuthorityPresentation, None, None, None, None)

          cooling
              "copied-immutable-blob"
              [ "PB-25-COPIED-IMMUTABLE-BLOB-ACCEPTED"
                "PB-60-COPY-ACCOUNTING-DIFFERS-WITHOUT-BREAKING-PARITY" ]
              []
              (CoolingFixture.authorizedCommand "primary" true)
              FrameDecision.Accept
              ResultClass.OutcomeSucceeded
              (None, None, None, None, Some [ blob "cooling-profile" ])

          cooling
              "resource-integrity-mismatch"
              [ "PB-26-RESOURCE-INTEGRITY-MISMATCH" ]
              []
              (CoolingFixture.authorizedCommand "primary" true)
              FrameDecision.Reject
              ResultClass.ProtocolError
              (Some ProtocolCategory.InvalidPayload, None, None, None, Some [ tampered ])

          catalog
              "addressing-only-handle"
              [ "PB-27-ADDRESSING-HANDLE-ACCEPTED" ]
              (CatalogFixture.handle "catalog-provider" "primary")
              FrameDecision.Accept
              ResultClass.OutcomeSucceeded
              None

          catalog
              "resource-scope-refused"
              [ "PB-28-RESOURCE-SCOPE-REFUSED" ]
              (CatalogFixture.handle "catalog-provider" "secondary")
              FrameDecision.Reject
              ResultClass.ProtocolError
              (Some ProtocolCategory.InvalidPayload) ]

[<AutoOpen>]
module PortableParitySupport =

    let runScenario (scenario: ParityScenario) direct =
        let invoke (host: PortableBindingHost) =
            host
                .Invoke(scenario.Operation, scenario.InputShape, scenario.Input, scenario.Authority, scenario.Resources)
                .Result

        if direct then
            invoke (directHost scenario.Contract (scenario.Handler()))
        else
            let host, seam = processHost scenario.Contract (scenario.Handler())
            use _ = seam
            invoke host

    /// Names the fields that differ, so a parity failure reports what diverged.
    let parityDifference (direct: InteractionResult) (crossed: InteractionResult) =
        let left = InteractionResult.parityProfile direct
        let right = InteractionResult.parityProfile crossed

        left
        |> Map.toList
        |> List.filter (fun (field, value) -> Map.tryFind field right <> Some value)
        |> List.map (fun (field, value) ->
            let other = Map.tryFind field right |> Option.defaultValue "<absent>"
            $"{field}: direct='{value}' other='{other}'")
        |> String.concat "; "
        |> sprintf "The realizations disagree on %s"

/// PB4: the fixed direct-call realization and the negotiated process realization report the same
/// category-level portable observations for every vector in the matrix.
///
/// Only the portable observation set is normalized. Representation, framing, crossed boundaries,
/// copy accounting, correlation, timing, and the endpoint's own diagnostic code are excluded by the
/// neutral parity profile, and this suite asserts that they are excluded for the stated reasons
/// rather than because nothing ever differs.
[<TestFixture>]
type PortableRealizationParityTests() =

    static member Scenarios: ParityScenario seq = Seq.ofList PortableParityMatrix.scenarios

    [<TestCaseSource("Scenarios")>]
    member _.``both realizations report the same parity profile``(scenario: ParityScenario) =
        let direct = runScenario scenario true
        let crossed = runScenario scenario false

        assertAll (fun () ->
            Assert.That(direct.FrameDecision, Is.EqualTo scenario.Frame, "The direct realization decided a different frame.")
            Assert.That(crossed.FrameDecision, Is.EqualTo scenario.Frame, "The process realization decided a different frame.")
            Assert.That(direct.ResultClass, Is.EqualTo scenario.Result)
            Assert.That(crossed.ResultClass, Is.EqualTo scenario.Result)
            Assert.That(direct.Category, Is.EqualTo scenario.Category)
            Assert.That(crossed.Category, Is.EqualTo scenario.Category)

            Assert.That(
                (InteractionResult.parityProfile crossed = InteractionResult.parityProfile direct),
                Is.True,
                parityDifference direct crossed
            ))

    /// A rejection reports zero provider effects wherever it was decided, and the counted effect of
    /// a success is the same number in both realizations.
    [<TestCaseSource("Scenarios")>]
    member _.``provider effects are counted the same way in both realizations``(scenario: ParityScenario) =
        let direct = runScenario scenario true
        let crossed = runScenario scenario false

        let expected =
            if scenario.Result = ResultClass.OutcomeSucceeded then 1L else 0L

        assertAll (fun () ->
            Assert.That(direct.Observation.ProviderEffectCount, Is.EqualTo expected)
            Assert.That(crossed.Observation.ProviderEffectCount, Is.EqualTo expected))

    /// PB6: a failure path leaks no provider effect, no authority value, no resource, no runtime
    /// type, and no false success — in either realization.
    ///
    /// Each vector already asserts its own category. This asserts what none of them does
    /// individually: that across every way an interaction can fail, the observation carries nothing
    /// it should not. A leak is much likelier to appear in the paths nobody looked at twice.
    [<TestCaseSource("FailingScenarios")>]
    member _.``a failure path leaks nothing``(scenario: ParityScenario) =
        for direct in [ true; false ] do
            let result = runScenario scenario direct
            let realization = if direct then "direct" else "process"

            let diagnostics =
                let code = result.Observation.LocalCode |> Option.defaultValue ""
                let message = result.Observation.LocalMessage |> Option.defaultValue ""
                $"{code}|{message}"

            assertAll (fun () ->
                Assert.That(
                    result.Observation.ProviderEffectCount,
                    Is.EqualTo 0L,
                    $"{realization}: a failure reported a provider effect."
                )

                Assert.That(
                    result.Observation.TerminalStatus,
                    Is.Not.EqualTo TerminalStatus.Succeeded,
                    $"{realization}: a failure reported success."
                )

                // A shaped failed Outcome carries its declared detail value; a denial and a protocol
                // rejection carry no value at all, because there is no shaped position for one.
                if scenario.Result <> ResultClass.OutcomeFailed then
                    Assert.That(result.Value, Is.EqualTo None, $"{realization}: a refusal presented a value.")

                // Nothing of the runtime crosses into the observation.
                for marker in [ "Brontide."; "Microsoft.FSharp"; "System."; "Exception"; "   at " ] do
                    Assert.That(diagnostics, Does.Not.Contain marker, $"{realization}: leaked '{marker}'.")

                // A frameless denial never left the host, so it observed no resource at all.
                if scenario.Result = ResultClass.Denial then
                    Assert.That(
                        result.Observation.ReferencedResources,
                        Is.Empty,
                        $"{realization}: a frameless denial observed a resource."
                    )

                    Assert.That(
                        result.Observation.CopyCount,
                        Is.EqualTo 0L,
                        $"{realization}: a denial copied something."
                    )

                // No presented resource is reported as accepted by an interaction that failed on it.
                if scenario.Category = Some ProtocolCategory.InvalidPayload && not scenario.Resources.IsEmpty then
                    Assert.That(
                        result.Observation.ReferencedResources |> List.filter (fun resource -> resource.Accepted),
                        Is.Empty,
                        $"{realization}: a refused resource was reported as accepted."
                    ))

    static member FailingScenarios: ParityScenario seq =
        PortableParityMatrix.scenarios
        |> List.filter (fun scenario -> scenario.Result <> ResultClass.OutcomeSucceeded)
        |> Seq.ofList

    /// The excluded fields are excluded because they genuinely differ, not because they happen to
    /// agree. A copied blob is one copy across the seam and none in a direct call.
    [<Test>]
    member _.``the excluded fields differ exactly as their stated reasons permit``() =
        let scenario =
            PortableParityMatrix.scenarios
            |> List.find (fun scenario -> scenario.Id = "copied-immutable-blob")

        let direct = runScenario scenario true
        let crossed = runScenario scenario false

        assertAll (fun () ->
            Assert.That(direct.Observation.CopyCount, Is.EqualTo 0L)
            Assert.That(crossed.Observation.CopyCount, Is.EqualTo 1L)
            Assert.That(direct.Observation.CrossedBoundaries, Does.Not.Contain "process")
            Assert.That(crossed.Observation.CrossedBoundaries, Contains.Item "process")

            // Correlation identities are per-run, which is why parity excludes them.
            Assert.That(
                ChannelRequestId.value crossed.Observation.Correlation.Request,
                Is.Not.EqualTo(ChannelRequestId.value direct.Observation.Correlation.Request)
            )

            shouldEqual (InteractionResult.parityProfile direct) (InteractionResult.parityProfile crossed))

    /// A refusal's local diagnostic code stays non-normative: it may differ between realizations
    /// while the portable category does not.
    [<Test>]
    member _.``a refusal carries the same portable category whatever its local code says``() =
        let scenario =
            PortableParityMatrix.scenarios
            |> List.find (fun scenario -> scenario.Id = "resource-scope-refused")

        let direct = runScenario scenario true
        let crossed = runScenario scenario false

        assertAll (fun () ->
            Assert.That(direct.Category, Is.EqualTo(Some ProtocolCategory.InvalidPayload))
            Assert.That(crossed.Category, Is.EqualTo(Some ProtocolCategory.InvalidPayload))
            Assert.That(direct.Observation.LocalCode, Is.Not.EqualTo None)
            Assert.That(crossed.Observation.LocalCode, Is.Not.EqualTo None))

    /// The portable process realization is length-delimited and bounded, and the retained
    /// line-delimited JSON protocol cannot pass for it.
    ///
    /// The two experiments still share a repository, so "the portable wire" has to be something the
    /// portable reader can tell apart from the legacy one rather than a claim in a document. A JSON
    /// line's first four bytes are read as a length prefix, and every such prefix is far beyond the
    /// declared bound, so the legacy protocol is refused on the prefix alone.
    [<Test>]
    member _.``a line-delimited JSON message is not a portable frame``() =
        let line =
            Encoding.UTF8.GetBytes
                "{\"kind\":\"invoke\",\"requestId\":\"r1\",\"operation\":\"interchange.tests.cooling.set-enabled\"}\n"

        use stream = new System.IO.MemoryStream(line)
        let fault =
            expectRefusal (PortableFraming.readFrame stream PortableLimits.declared CancellationToken.None).Result

        assertAll (fun () ->
            Assert.That(fault.Category, Is.EqualTo ProtocolCategory.LimitExceeded)
            // The portable frame bound is finite.
            Assert.That(PortableLimits.declared.MaxFrameBytes, Is.EqualTo 65536))

    /// The matrix is measured against the neutral layer rather than against itself: a scenario that
    /// names a vector the neutral artifacts no longer declare is a stale claim.
    [<Test>]
    member _.``every scenario names vectors the neutral layer declares``() =
        let declared = neutralVectorIds ()
        let channel = channelVectorIds ()

        assertAll (fun () ->
            for scenario in PortableParityMatrix.scenarios do
                Assert.That(scenario.Vectors, Is.Not.Empty, $"Scenario '{scenario.Id}' names no vector.")

                Assert.That(
                    scenario.Vectors |> List.filter (fun vector -> not (List.contains vector declared)),
                    Is.Empty,
                    $"Scenario '{scenario.Id}' names an undeclared portable vector."
                )

                Assert.That(
                    scenario.ChannelVectors |> List.filter (fun vector -> not (List.contains vector channel)),
                    Is.Empty,
                    $"Scenario '{scenario.Id}' names an undeclared Channel vector."
                )

            Assert.That(PortableParityMatrix.scenarios |> List.map (fun scenario -> scenario.Id), Is.Unique))

    /// Every portable result class the host can reach is measured for parity.
    [<Test>]
    member _.``the matrix covers every result class a host can reach``() =
        let covered =
            PortableParityMatrix.scenarios
            |> List.map (fun scenario -> scenario.Result)
            |> Set.ofList

        shouldEqual
            (Set.ofList
                [ ResultClass.OutcomeSucceeded
                  ResultClass.OutcomeFailed
                  ResultClass.Denial
                  ResultClass.ProtocolError ])
            covered
