namespace Brontide.Minimal.Interchange.Tests.Portable

open System
open System.Diagnostics
open System.IO
open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// PB5 / PB-61: this host establishes and invokes against a provider that depends on neither stack.
///
/// The endpoint under `binding/neutral-provider/` imports no Brontide assembly. It answers with the
/// contract transcoded from the checked-in neutral declaration rather than one restated in its own
/// source, and it reads and writes the wire with the base class library's CBOR codec rather than
/// with either stack's. Passing the same parity matrix against it is what turns "the contract is
/// implementable without importing either private model" from a claim into evidence.
[<TestFixture>]
[<Category("CrossProcess")>]
[<Category("NeutralProvider")>]
type PortableNeutralProviderTests() =

    let startNeutralProvider (arguments: string list) =
        match Environment.GetEnvironmentVariable "BRONTIDE_NEUTRAL_PROVIDER" |> Option.ofObj with
        | Some path when File.Exists path ->
            let info = ProcessStartInfo(path, String.concat " " arguments)
            info.RedirectStandardInput <- true
            info.RedirectStandardOutput <- true
            info.RedirectStandardError <- true
            info.UseShellExecute <- false
            info.CreateNoWindow <- true

            match Process.Start info |> Option.ofObj with
            | Some provider -> provider
            | None -> failwith "The neutral provider process did not start."
        | _ ->
            Assert.Ignore "BRONTIDE_NEUTRAL_PROVIDER does not name a built implementation-neutral provider."
            failwith "The neutral-provider test was ignored."

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
        PortableBindingHost.Establish(contract, conversation provider, "minimal-hosts-neutral").Result
        |> expectOk

    static member Scenarios: ParityScenario seq = Seq.ofList PortableParityMatrix.scenarios

    [<TestCaseSource("Scenarios")>]
    member _.``a provider depending on neither stack reaches the same observations``(scenario: ParityScenario) =
        let native = runScenario scenario true
        use provider = startNeutralProvider scenario.ProviderArguments

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

    /// The neutral endpoint negotiates the contract it was given rather than one it invented: the
    /// established plan carries the same declared facts this stack's own negotiation produces.
    [<Test>]
    member _.``the negotiated plan matches the one this stack negotiates with itself``() =
        let own = directCoolingHost ()
        use provider = startNeutralProvider [ "--portable" ]

        try
            let host = establish provider CoolingFixture.contract
            let permitted = Set.ofList [ "realization"; "framing"; "crossedBoundaries"; "planId"; "hostEndpoint" ]

            let differing =
                BindingPlan.factNames own.Plan
                |> List.filter (fun name -> BindingPlan.tryFact name own.Plan <> BindingPlan.tryFact name host.Plan)
                |> Set.ofList

            Assert.That(
                Set.isSubset differing permitted,
                Is.True,
                $"Unexpected plan differences: %A{Set.difference differing permitted}"
            )
        finally
            stop provider
