namespace Brontide.Minimal.Interchange.Tests.Portable

open System.IO
open System.Reflection
open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// C2, C7, C9, and C10: plan facts, observation completeness, realization parity, and the
/// independence the neutral layer requires.
[<TestFixture>]
type PortablePlanObservationAndParityTests() =

    let establishedPlan () =
        expectOk (
            PortableNegotiation.negotiate
                CoolingFixture.contract
                CoolingFixture.contract
                Realization.FixedDirectCall
                "minimal-direct"
                "minimal-provider"
                "fixed"
        )

    [<Test>]
    member _.``PB-53 every declared plan fact answers without invoking the provider``() =
        let plan = establishedPlan ()
        use document = readNeutral [ "schemas"; "binding-plan.json" ]

        let declared =
            document.RootElement.GetProperty("facts").EnumerateArray()
            |> Seq.collect (fun group -> group.GetProperty("entries").EnumerateArray())
            |> Seq.map (fun entry -> entry.GetProperty("id").GetString() |> str)
            |> List.ofSeq

        Assert.That(List.length declared, Is.GreaterThan 30)

        for name in declared do
            Assert.That(BindingPlan.tryFact name plan, Is.Not.Null, $"The Binding Plan answers no fact named '{name}'.")

            match BindingPlan.tryFact name plan with
            | Some _ -> ()
            | None -> Assert.Fail $"The Binding Plan answers no fact named '{name}'."

        assertAll (fun () ->
            Assert.That(BindingPlan.tryFact "constraintPolicy" plan, Is.EqualTo(Some "fail-closed"))
            Assert.That(BindingPlan.tryFact "implicitCopyPermitted" plan, Is.EqualTo(Some "false"))
            Assert.That(BindingPlan.tryFact "exceptionTransportPermitted" plan, Is.EqualTo(Some "false"))
            Assert.That(BindingPlan.tryFact "chainConjunctionTier" plan, Is.EqualTo(Some "carried"))
            Assert.That(BindingPlan.tryFact "orderingGuarantee" plan, Is.EqualTo(Some "none"))
            Assert.That(BindingPlan.tryFact "maxConcurrentRequests" plan, Is.EqualTo(Some "1")))

    [<Test>]
    member _.``PB-54 compact identifiers are plan data and never identity``() =
        let plan = establishedPlan ()
        let assignments = BindingPlan.compactAssignments plan

        Assert.That(assignments, Is.Not.Empty)

        // Every assignment names a canonical reference; the compact value only labels it inside this
        // binding.
        for assignment in assignments do
            Assert.That(assignment.Reference, Does.Contain "@")
            Assert.That(CompactId.value assignment.Compact, Is.GreaterThanOrEqualTo 0)

        let operations = expectOk (Ok(BindingPlan.tryFact "operations" plan))

        Assert.That(operations, Is.EqualTo(Some(PortableOperationRef.text CoolingFixture.setEnabled)))

        // The observation reports canonical references too.
        let host = directCoolingHost ()

        let result =
            invoke host CoolingFixture.setEnabled CoolingFixture.commandV1 (CoolingFixture.authorizedCommand "primary" true)

        shouldEqual [ CoolingFixture.setEnabled ] result.Observation.NegotiatedOperations

    [<Test>]
    member _.``PB-55 a successful interaction reports every normative field``() =
        let host = directCoolingHost ()

        let result =
            invoke host CoolingFixture.setEnabled CoolingFixture.commandV1 (CoolingFixture.authorizedCommand "primary" true)

        let observation = result.Observation

        assertAll (fun () ->
            Assert.That(Observation.completenessFailures observation, Is.Empty)
            Assert.That(observation.TerminalStatus, Is.EqualTo TerminalStatus.Succeeded)
            Assert.That(observation.FailureDomain, Is.EqualTo None)
            Assert.That(observation.SelectionReason, Is.Not.Empty)
            Assert.That(observation.NegotiatedContractVersion, Is.EqualTo 1)
            Assert.That(observation.RetryCount, Is.EqualTo 0L)
            Assert.That(observation.Interrupted, Is.False)
            Assert.That(observation.ProviderEffectCount, Is.EqualTo 1L)
            // Timing is recorded but drives no semantic decision.
            Assert.That(observation.Timing.RequestElapsedMilliseconds, Is.GreaterThanOrEqualTo 0L))

    [<Test>]
    member _.``PB-58 both realizations report the same category-level profile on success``() =
        let direct = directCoolingHost ()
        let processHost, seam = processCoolingHost ()
        use _ = seam

        let command = CoolingFixture.authorizedCommand "primary" true
        let directResult = invoke direct CoolingFixture.setEnabled CoolingFixture.commandV1 command
        let processResult = invoke processHost CoolingFixture.setEnabled CoolingFixture.commandV1 command

        shouldEqual (InteractionResult.parityProfile directResult) (InteractionResult.parityProfile processResult)

        // The realization is exactly what may differ.
        assertAll (fun () ->
            Assert.That(direct.Realization, Is.EqualTo Realization.FixedDirectCall)
            Assert.That(processHost.Realization, Is.EqualTo Realization.NegotiatedProcess)

            Assert.That(
                BindingPlan.tryFact "framing" direct.Plan,
                Is.EqualTo(Some PortableRepresentations.DirectCall)
            )

            Assert.That(
                BindingPlan.tryFact "framing" processHost.Plan,
                Is.EqualTo(Some PortableRepresentations.LengthDelimited)
            ))

    [<Test>]
    member _.``PB-59 both realizations report the same no-frame denial``() =
        let direct = directCoolingHost ()
        let processHost, seam = processCoolingHost ()
        use _ = seam

        let command = CoolingFixture.authorizedCommand "primary" true
        let denied = PortableConstraint.Atom PortableTruth.Unsatisfied

        let directResult =
            direct.Invoke(CoolingFixture.setEnabled, CoolingFixture.commandV1, command, denied).Result

        let processResult =
            processHost
                .Invoke(CoolingFixture.setEnabled, CoolingFixture.commandV1, command, denied)
                .Result

        assertAll (fun () ->
            Assert.That(directResult.FrameDecision, Is.EqualTo FrameDecision.None)
            Assert.That(processResult.FrameDecision, Is.EqualTo FrameDecision.None)
            Assert.That(directResult.ResultClass, Is.EqualTo ResultClass.Denial)
            Assert.That(processResult.ResultClass, Is.EqualTo ResultClass.Denial)

            shouldEqual (InteractionResult.parityProfile directResult) (InteractionResult.parityProfile processResult))

    [<Test>]
    member _.``PB-60 copy accounting differs by realization without breaking parity``() =
        let direct = directCoolingHost ()
        let processHost, seam = processCoolingHost ()
        use _ = seam

        let command = CoolingFixture.authorizedCommand "primary" true

        let directResult =
            direct
                .Invoke(CoolingFixture.setEnabled, CoolingFixture.commandV1, command, permitted, [ blob "cooling-profile" ])
                .Result

        let processResult =
            processHost
                .Invoke(CoolingFixture.setEnabled, CoolingFixture.commandV1, command, permitted, [ blob "cooling-profile" ])
                .Result

        assertAll (fun () ->
            Assert.That(directResult.Observation.CopyCount, Is.EqualTo 0L)
            Assert.That(processResult.Observation.CopyCount, Is.EqualTo 1L)

            // Parity still holds, because copy accounting is an excluded field with exactly this
            // stated reason, and the compared resource facts are equal.
            shouldEqual (InteractionResult.parityProfile directResult) (InteractionResult.parityProfile processResult))

    [<Test>]
    member _.``a refusal leaves both realizations in the same lifecycle state``() =
        let direct = directCoolingHost ()
        let processHost, seam = processCoolingHost ()
        use _ = seam

        // The Operation declares the host-context Fragment as required, so omitting it is refused
        // wherever the refusal is decided.
        let unattributed = CoolingFixture.command "primary" true None None None

        let directResult = invoke direct CoolingFixture.setEnabled CoolingFixture.commandV1 unattributed
        let processResult = invoke processHost CoolingFixture.setEnabled CoolingFixture.commandV1 unattributed

        assertAll (fun () ->
            Assert.That(directResult.Category, Is.EqualTo(Some ProtocolCategory.InvalidPayload))
            Assert.That(processResult.Category, Is.EqualTo(Some ProtocolCategory.InvalidPayload))
            Assert.That(directResult.Observation.ProviderEffectCount, Is.EqualTo 0L)
            Assert.That(processResult.Observation.ProviderEffectCount, Is.EqualTo 0L)
            // The declared transition to 'failed' is applied by the endpoint rather than by the
            // transport, so the same refusal leaves both realizations in the same state.
            Assert.That(processHost.State, Is.EqualTo direct.State)
            Assert.That(direct.State, Is.EqualTo LifecycleState.Failed))

    [<Test>]
    member _.``PB-62 the binding imports no other stack and the neutral layer stays data-only``() =
        let assembly = typeof<PortableBindingHost>.Assembly

        let referenced =
            assembly.GetReferencedAssemblies()
            |> Array.map (fun reference -> reference.Name)
            |> Array.append [| assembly.GetName().Name |]

        for name in referenced do
            Assert.That(name, Does.Not.StartWith "Brontide.Reference")

        // The neutral layer contains no generated stack source and no runtime helper.
        let files = Directory.GetFiles(neutralRoot, "*", SearchOption.AllDirectories)
        Assert.That(files, Is.Not.Empty)

        for file in files do
            let extension = Path.GetExtension(file: string)
            Assert.That([ ".json"; ".md" ], Contains.Item extension, $"'{file}' is not data.")

    [<Test>]
    member _.``a plan for the same contract differs only in the realization facts``() =
        let direct = establishedPlan ()

        let negotiated =
            expectOk (
                PortableNegotiation.negotiate
                    CoolingFixture.contract
                    CoolingFixture.contract
                    Realization.NegotiatedProcess
                    "minimal-direct"
                    "minimal-provider"
                    "fixed"
            )

        let differing =
            BindingPlan.factNames direct
            |> List.filter (fun name -> BindingPlan.tryFact name direct <> BindingPlan.tryFact name negotiated)
            |> List.filter (fun name -> name <> "planId")
            |> Set.ofList

        shouldEqual (Set.ofList [ "framing"; "realization"; "crossedBoundaries" ]) differing
