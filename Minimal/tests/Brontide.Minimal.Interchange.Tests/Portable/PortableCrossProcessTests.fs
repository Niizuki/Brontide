namespace Brontide.Minimal.Interchange.Tests.Portable

open System
open System.Diagnostics
open System.IO
open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// The negotiated process realization across a real operating-system process boundary.
///
/// The default suite exercises the same realization over a local pipe seam, which proves the
/// encoding, bounds, and copy accounting. This suite proves the remaining claim that seam cannot:
/// that the endpoint needs nothing of the host's process to reach the same observations.
[<TestFixture>]
[<Category("CrossProcess")>]
type PortableCrossProcessTests() =

    let startProvider (arguments: string list) =
        match Environment.GetEnvironmentVariable "BRONTIDE_MINIMAL_PROVIDER" |> Option.ofObj with
        | Some path when File.Exists path ->
            let info = ProcessStartInfo(path, String.concat " " arguments)
            info.RedirectStandardInput <- true
            info.RedirectStandardOutput <- true
            info.RedirectStandardError <- true
            info.UseShellExecute <- false
            info.CreateNoWindow <- true

            match Process.Start info |> Option.ofObj with
            | Some process' -> process'
            | None -> failwith "The provider process did not start."
        | _ ->
            Assert.Ignore "BRONTIDE_MINIMAL_PROVIDER does not name a built Minimal provider endpoint."
            failwith "The cross-process test was ignored."

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

    [<Test>]
    member _.``a provider in its own process establishes, invokes, and reports a success``() =
        use provider = startProvider [ "--portable" ]

        try
            let host =
                PortableBindingHost
                    .Establish(CoolingFixture.contract, conversation provider, "minimal-cross-process")
                    .Result
                |> expectOk

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
                Assert.That(result.ResultClass, Is.EqualTo ResultClass.OutcomeSucceeded)
                Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo 1L)
                // A copied blob crosses the process seam exactly once.
                Assert.That(result.Observation.CopyCount, Is.EqualTo 1L)
                Assert.That(result.Observation.CrossedBoundaries, Contains.Item "process")
                Assert.That(BindingPlan.tryFact "framing" host.Plan, Is.EqualTo(Some "length-delimited")))

            expectOk (host.Withdraw().Result)
            expectOk (host.Terminate().Result)
        finally
            stop provider

    [<Test>]
    member _.``a shaped failure crosses a real process boundary without an exception``() =
        use provider = startProvider [ "--portable" ]

        try
            let host =
                PortableBindingHost
                    .Establish(CoolingFixture.contract, conversation provider, "minimal-cross-process")
                    .Result
                |> expectOk

            let refusing =
                CoolingFixture.command "primary" true (Some "semantic") (Some "operator") None

            let result = invoke host CoolingFixture.setEnabled CoolingFixture.commandV1 refusing

            assertAll (fun () ->
                Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.Accept)
                Assert.That(result.ResultClass, Is.EqualTo ResultClass.OutcomeFailed)
                Assert.That(result.Observation.FailureDomain, Is.EqualTo(Some FailureDomain.RemoteProvider))
                Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo 0L))
        finally
            stop provider

    [<Test>]
    member _.``a local denial across a real process boundary still emits no frame``() =
        use provider = startProvider [ "--portable" ]

        try
            let host =
                PortableBindingHost
                    .Establish(CoolingFixture.contract, conversation provider, "minimal-cross-process")
                    .Result
                |> expectOk

            let result =
                host
                    .Invoke(
                        CoolingFixture.setEnabled,
                        CoolingFixture.commandV1,
                        CoolingFixture.authorizedCommand "primary" true,
                        PortableConstraint.Atom PortableTruth.Unsatisfied
                    )
                    .Result

            assertAll (fun () ->
                Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.None)
                Assert.That(result.ResultClass, Is.EqualTo ResultClass.Denial)
                Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo 0L))
        finally
            stop provider
