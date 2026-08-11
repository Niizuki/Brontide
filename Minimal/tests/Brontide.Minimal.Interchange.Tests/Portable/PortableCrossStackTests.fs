namespace Brontide.Minimal.Interchange.Tests.Portable

open System
open System.Diagnostics
open System.IO
open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// PB5: a Minimal host drives a *Reference* provider over the portable contract.
///
/// This is the second direction of the cross-stack matrix, and the first evidence that the two
/// stacks can speak to each other over the reusable layer rather than over the retained
/// line-delimited Cooling and Catalog experiments. Nothing is shared but the data: the Reference
/// endpoint is a separate process built from C# sources that reference no Minimal assembly, and the
/// only thing crossing is deterministic CBOR described by the neutral schemas.
///
/// The scenarios are the PB4 parity matrix, unchanged. Reusing it is the point — the claim is not
/// that some request works across the stacks, but that the same category-level observations this
/// host reports when it talks to itself are what it reports when it talks to Reference.
[<TestFixture>]
[<Category("CrossProcess")>]
[<Category("CrossStack")>]
type PortableCrossStackTests() =

    let startReferenceProvider (arguments: string list) =
        match Environment.GetEnvironmentVariable "BRONTIDE_REFERENCE_PROVIDER" |> Option.ofObj with
        | Some path when File.Exists path ->
            let info = ProcessStartInfo(path, String.concat " " arguments)
            info.RedirectStandardInput <- true
            info.RedirectStandardOutput <- true
            info.RedirectStandardError <- true
            info.UseShellExecute <- false
            info.CreateNoWindow <- true

            match Process.Start info |> Option.ofObj with
            | Some provider -> provider
            | None -> failwith "The Reference provider process did not start."
        | _ ->
            Assert.Ignore "BRONTIDE_REFERENCE_PROVIDER does not name a built Reference provider endpoint."
            failwith "The cross-stack test was ignored."

    let conversation (provider: Process) =
        PortableProcessConversation(
            PortableStreamDuplex(
                provider.StandardOutput.BaseStream,
                provider.StandardInput.BaseStream,
                PortableLimits.declared,
                false
            ),
            PortableLimits.declared
        )

    let stop (provider: Process) =
        if not provider.HasExited then
            provider.StandardInput.Close()

            if not (provider.WaitForExit 5000) then
                provider.Kill true

    let establish (provider: Process) contract =
        PortableBindingHost.Establish(contract, conversation provider, "minimal-hosts-reference").Result
        |> expectOk

    static member Scenarios: ParityScenario seq = Seq.ofList PortableParityMatrix.scenarios

    [<TestCaseSource("Scenarios")>]
    member _.``a Minimal host and a Reference provider agree on every parity scenario``(scenario: ParityScenario) =
        // The baseline is this stack talking to itself, so a difference is attributable to the peer
        // rather than to the scenario.
        let native = runScenario scenario true
        use provider = startReferenceProvider scenario.ProviderArguments

        try
            let host = establish provider scenario.Contract

            let crossed =
                host
                    .Invoke(
                        scenario.Operation,
                        scenario.InputShape,
                        scenario.Input,
                        scenario.Authority,
                        scenario.Resources
                    )
                    .Result

            assertAll (fun () ->
                Assert.That(crossed.FrameDecision, Is.EqualTo scenario.Frame)
                Assert.That(crossed.ResultClass, Is.EqualTo scenario.Result)
                Assert.That(crossed.Category, Is.EqualTo scenario.Category)

                Assert.That(
                    (InteractionResult.parityProfile crossed = InteractionResult.parityProfile native),
                    Is.True,
                    parityDifference native crossed
                ))
        finally
            stop provider

    /// The negotiated plan names the Reference provider, so the binding is demonstrably not this
    /// stack's own endpoint under another name.
    [<Test>]
    member _.``the established plan records the Reference provider and a crossed process boundary``() =
        use provider = startReferenceProvider [ "--portable" ]

        try
            let host = establish provider CoolingFixture.contract

            let result =
                host
                    .Invoke(
                        CoolingFixture.setEnabled,
                        CoolingFixture.commandV1,
                        CoolingFixture.authorizedCommand "primary" true,
                        permitted,
                        [ blob "cooling-profile" ]
                    )
                    .Result

            assertAll (fun () ->
                Assert.That(BindingPlan.tryFact "framing" host.Plan, Is.EqualTo(Some "length-delimited"))
                Assert.That(BindingPlan.tryFact "realization" host.Plan, Is.EqualTo(Some "negotiated-process"))
                Assert.That(result.Observation.CrossedBoundaries, Contains.Item "process")
                Assert.That(result.Observation.SelectedProvider, Is.EqualTo CoolingFixture.provider)
                Assert.That(result.Observation.CopyCount, Is.EqualTo 1L)
                Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo(Some 1L)))

            expectOk (host.Withdraw().Result)
            expectOk (host.Terminate().Result)
        finally
            stop provider

    /// A shaped failed Outcome produced by the Reference provider's own domain crosses as data, with
    /// no CLR exception, runtime type name, or stack trace anywhere in the observation.
    [<Test>]
    member _.``a Reference provider failure crosses as shaped data and not as a foreign runtime value``() =
        use provider = startReferenceProvider [ "--portable" ]

        try
            let host = establish provider CoolingFixture.contract

            let refusing =
                CoolingFixture.command "primary" true (Some "requested-failure") (Some "operator") None

            let result = invoke host CoolingFixture.setEnabled CoolingFixture.commandV1 refusing

            let message = result.Observation.LocalMessage |> Option.defaultValue ""

            assertAll (fun () ->
                Assert.That(result.ResultClass, Is.EqualTo ResultClass.OutcomeFailed)
                // A domain refusal is not a protocol error.
                Assert.That(result.Category, Is.EqualTo None)
                Assert.That(result.Observation.FailureDomain, Is.EqualTo(Some FailureDomain.RemoteProvider))
                Assert.That(message, Does.Not.Contain "Brontide.Reference")
                Assert.That(message, Does.Not.Contain "Exception"))
        finally
            stop provider
