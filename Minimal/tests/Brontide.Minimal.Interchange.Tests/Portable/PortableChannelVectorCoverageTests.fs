namespace Brontide.Minimal.Interchange.Tests.Portable

open NUnit.Framework

/// PB4's other half: every Channel 0.1 vector executes in this stack, independently of the other.
///
/// The accounting is derived, never restated. A Channel vector counts as executed here only when
/// some portable vector that the neutral artifacts say preserves it is executed by this stack's
/// evidence — so removing a test, or renaming a Channel vector, fails the build instead of quietly
/// leaving a hole. The two source files are the authored `conformance/channel-0.1-vectors.json` and
/// the neutral portable vectors.
[<TestFixture>]
type PortableChannelVectorCoverageTests() =

    /// The executed portable vectors that preserve each declared Channel vector.
    let executingVectors () =
        let references = neutralChannelReferences ()

        channelVectorIds ()
        |> List.map (fun channelVector ->
            let executing =
                references
                |> Map.toList
                |> List.filter (fun (vector, preserved) ->
                    List.contains channelVector preserved && PortableVectorCoverage.executed vector)
                |> List.map fst
                |> List.sort

            channelVector, executing)

    [<Test>]
    member _.``every Channel vector is executed by this stack``() =
        let executing = executingVectors ()

        let uncovered =
            executing |> List.filter (fun (_, vectors) -> List.isEmpty vectors) |> List.map fst

        assertAll (fun () ->
            Assert.That(executing, Is.Not.Empty)

            Assert.That(
                uncovered,
                Is.Empty,
                "A Channel 0.1 vector has no executed portable vector in this stack: "
                + String.concat ", " uncovered
            ))

    /// The cross-stack deferral is bounded: PB5's deferred vectors preserve no Channel vector that
    /// only they cover, so deferring them leaves the Channel taxonomy fully executed here.
    [<Test>]
    member _.``no Channel vector depends on a deferred portable vector alone``() =
        let references = neutralChannelReferences ()

        let deferredOnly =
            channelVectorIds ()
            |> List.filter (fun channelVector ->
                let referencing =
                    references
                    |> Map.toList
                    |> List.filter (fun (_, preserved) -> List.contains channelVector preserved)

                not (List.isEmpty referencing)
                && referencing |> List.forall (fun (vector, _) -> not (PortableVectorCoverage.executed vector)))

        Assert.That(deferredOnly, Is.Empty)

    /// The parity matrix carries the Channel vectors PB4 names specifically: correlation, payload
    /// covariance, the two strong-Kleene outcomes, the frameless denial, the shaped failed Outcome,
    /// and the two rejection categories a host can reach.
    [<Test>]
    member _.``the parity matrix measures the Channel vectors PB4 names``() =
        let measured =
            PortableParityMatrix.scenarios
            |> List.collect (fun scenario -> scenario.ChannelVectors)
            |> Set.ofList

        let required =
            Set.ofList
                [ "CH-01-CORRELATION-ECHO"
                  "CH-07-PAYLOAD-COVARIANCE"
                  "CH-08-AUTHORITY-NO-PROJECTION"
                  "CH-09-STRONG-KLEENE-FALLBACK"
                  "CH-10-STRONG-KLEENE-UNKNOWN-DENIES"
                  "CH-11-NO-CAPABILITY-TRANSFER"
                  "CH-12-DENIAL-IS-NOT-A-FRAME"
                  "CH-13-SEMANTIC-FAILED-OUTCOME"
                  "CH-19-UNSUPPORTED-OPERATION"
                  "CH-20-INVALID-PAYLOAD" ]

        Assert.That(Set.isSubset required measured, Is.True, $"Missing: %A{Set.difference required measured}")
