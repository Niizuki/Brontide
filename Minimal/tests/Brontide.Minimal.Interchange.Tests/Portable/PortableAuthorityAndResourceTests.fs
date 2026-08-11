namespace Brontide.Minimal.Interchange.Tests.Portable

open System
open System.Text
open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// C3 and C6: local authority, no-capability-transfer, and referenced resources.
[<TestFixture>]
type PortableAuthorityAndResourceTests() =

    let limits = PortableLimits.declared

    let denied = PortableConstraint.Atom PortableTruth.Unsatisfied

    /// An unrecognized Constraint value version makes the atom Unknown; it is never projected onto a
    /// version the evaluator does recognize, because that would widen authority.
    let unrecognizedConstraintVersion = PortableConstraint.Atom PortableTruth.Unknown

    let coolingHostAndHandler () =
        let handler = CoolingHandler()
        directHost CoolingFixture.contract handler, handler

    [<Test>]
    member _.``PB-18 a local denial emits no frame and starts no provider``() =
        let host, handler = coolingHostAndHandler ()

        let result =
            host
                .Invoke(
                    CoolingFixture.setEnabled,
                    CoolingFixture.commandV1,
                    CoolingFixture.authorizedCommand "primary" true,
                    denied
                )
                .Result

        assertAll (fun () ->
            Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.None)
            Assert.That(result.ResultClass, Is.EqualTo ResultClass.Denial)
            Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo(Some 0L))
            Assert.That(handler.ProviderEffectCount, Is.EqualTo 0L))

    [<Test>]
    member _.``PB-19 an unrecognized Constraint value version denies rather than projecting``() =
        let host, handler = coolingHostAndHandler ()

        let result =
            host
                .Invoke(
                    CoolingFixture.setEnabled,
                    CoolingFixture.commandV1,
                    CoolingFixture.authorizedCommand "primary" true,
                    unrecognizedConstraintVersion
                )
                .Result

        assertAll (fun () ->
            Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.None)
            Assert.That(result.ResultClass, Is.EqualTo ResultClass.Denial)
            Assert.That(result.Observation.AuthorityDecision, Is.EqualTo AuthorityDecision.Unknown)
            Assert.That(handler.ProviderEffectCount, Is.EqualTo 0L))

    [<Test>]
    member _.``PB-20 AnyOf with a satisfied atom permits despite an unknown one``() =
        let host = directCoolingHost ()

        let decision =
            expectOk (
                host.PrepareRequest(
                    CoolingFixture.setEnabled,
                    CoolingFixture.commandV1,
                    PortableConstraint.AnyOf
                        [ PortableConstraint.Atom PortableTruth.Satisfied
                          PortableConstraint.Atom PortableTruth.Unknown ]
                )
            )

        assertAll (fun () ->
            Assert.That(decision.FrameDecision, Is.EqualTo FrameDecision.Emit)
            Assert.That(decision.ResultClass, Is.EqualTo ResultClass.Request)
            Assert.That(decision.Admission.Decision, Is.EqualTo AuthorityDecision.Permitted))

    [<Test>]
    member _.``PB-21 AllOf with an unknown atom denies without a far-side effect``() =
        let host, handler = coolingHostAndHandler ()

        let result =
            host
                .Invoke(
                    CoolingFixture.setEnabled,
                    CoolingFixture.commandV1,
                    CoolingFixture.authorizedCommand "primary" true,
                    PortableConstraint.AllOf
                        [ PortableConstraint.Atom PortableTruth.Satisfied
                          PortableConstraint.Atom PortableTruth.Unknown ]
                )
                .Result

        assertAll (fun () ->
            Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.None)
            Assert.That(result.ResultClass, Is.EqualTo ResultClass.Denial)
            Assert.That(result.Observation.AuthorityDecision, Is.EqualTo AuthorityDecision.Unknown)
            Assert.That(handler.ProviderEffectCount, Is.EqualTo 0L))

    [<Test>]
    member _.``strong Kleene negation of unknown stays unknown``() =
        Assert.That(
            PortableConstraint.evaluate (PortableConstraint.Not(PortableConstraint.Atom PortableTruth.Unknown)),
            Is.EqualTo PortableTruth.Unknown
        )

    [<Test>]
    member _.``PB-22 a Capability in the body of a trust-crossing binding fails closed``() =
        let host, handler = coolingHostAndHandler ()

        let carrying =
            CoolingFixture.authorizedCommand "primary" true
            |> PortableRecord.withField "capability" (PortableText "cooling.write@1")

        let result = invoke host CoolingFixture.setEnabled CoolingFixture.commandV1 carrying

        assertAll (fun () ->
            Assert.That(result.FrameDecision, Is.EqualTo FrameDecision.Reject)
            Assert.That(result.Category, Is.EqualTo(Some ProtocolCategory.InvalidAuthorityPresentation))
            Assert.That(handler.ProviderEffectCount, Is.EqualTo 0L))

    [<Test>]
    member _.``PB-23 a trust-crossing contract without the declaration fails closed``() =
        let permissive =
            { CoolingFixture.contract with
                Authority =
                    { CoolingFixture.contract.Authority with
                        NoCapabilityTransfer = false } }

        expectCategory
            ProtocolCategory.InvalidAuthorityPresentation
            (PortableNegotiation.negotiate permissive permissive Realization.FixedDirectCall "host" "provider" "fixed")
        |> ignore

    [<Test>]
    member _.``PB-24 a missing required Fragment is refused before provider activation``() =
        let host, handler = coolingHostAndHandler ()
        let unattributed = CoolingFixture.command "primary" true None None None

        let result = invoke host CoolingFixture.setEnabled CoolingFixture.commandV1 unattributed

        assertAll (fun () ->
            Assert.That(result.Category, Is.EqualTo(Some ProtocolCategory.InvalidPayload))
            Assert.That(handler.ProviderEffectCount, Is.EqualTo 0L))

    [<Test>]
    member _.``PB-25 a copied immutable blob is accepted and observed as copied``() =
        let host = directCoolingHost ()

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
            Assert.That(List.length result.Observation.ReferencedResources, Is.EqualTo 1)

            let observed = List.head result.Observation.ReferencedResources
            Assert.That(observed.Ownership, Is.EqualTo "copied")
            Assert.That(observed.IntegrityVerified, Is.True)
            Assert.That(observed.Accepted, Is.True)
            // A direct call materializes no copy at all.
            Assert.That(result.Observation.CopyCount, Is.EqualTo 0L))

    [<Test>]
    member _.``PB-26 a content hash that does not verify is refused before any effect``() =
        let tampered =
            CopiedBlob("profile", Text.Encoding.UTF8.GetBytes "cooling-profile", String.replicate 64 "a")

        expectCategory
            ProtocolCategory.InvalidPayload
            (ResourceCodec.admit tampered [ ResourceFlavor.CopiedImmutableBlobToken ] [] limits)
        |> ignore

    [<Test>]
    member _.``PB-27 a handle inside the accept list conveys addressing and no octets``() =
        let host = directHost CatalogFixture.contract (CatalogHandler())

        let result =
            host
                .Invoke(
                    CatalogFixture.upsert,
                    CatalogFixture.upsertCommand,
                    CatalogFixture.upsertCommandValue [ CatalogFixture.itemValue "a" "Alpha" [ "cold" ] ],
                    permitted,
                    [ CatalogFixture.handle "catalog-provider" "primary" ]
                )
                .Result

        assertAll (fun () ->
            Assert.That(result.ResultClass, Is.EqualTo ResultClass.OutcomeSucceeded)
            Assert.That(result.Observation.CopyCount, Is.EqualTo 0L)

            let observed = List.head result.Observation.ReferencedResources
            Assert.That(observed.Ownership, Is.EqualTo "provider-retained")
            Assert.That(observed.IntegrityVerified, Is.False))

    [<Test>]
    member _.``PB-28 a handle outside the accept list is a payload refusal, not an authority one``() =
        let outside = CatalogFixture.handle "catalog-provider" "secondary"

        let fault =
            expectCategory
                ProtocolCategory.InvalidPayload
                (ResourceCodec.admit
                    outside
                    [ ResourceFlavor.AddressingOnlyHandleToken ]
                    [ CatalogFixture.acceptedHandle ]
                    limits)

        // The local code stays non-normative; the portable category is what decides.
        Assert.That(fault.LocalCode, Is.EqualTo "resource-refused")

    [<Test>]
    member _.``PB-29 a 0.1 non-goal resource flavor fails negotiation closed``() =
        let borrowed =
            { CatalogFixture.contract with
                Representation =
                    { CatalogFixture.contract.Representation with
                        ResourceFlavors = [ "borrowed-read-only-region" ]
                        AcceptedResourceHandles = [] } }

        Assert.That(ResourceFlavor.nonGoals, Contains.Item "borrowed-read-only-region")

        expectCategory
            ProtocolCategory.UnsupportedContract
            (PortableNegotiation.negotiate borrowed borrowed Realization.FixedDirectCall "host" "provider" "fixed")
        |> ignore

    [<Test>]
    member _.``PB-30 resource octets for a handle-flavored resource are a forbidden implicit copy``() =
        let encoded =
            CborMap
                [ "flavor", CborText ResourceFlavor.AddressingOnlyHandleToken
                  "name", CborText "catalog"
                  "provider", CborText "catalog-provider"
                  "id", CborText "primary"
                  "content", CborBytes [| 1uy; 2uy; 3uy |] ]

        let fault =
            expectCategory
                ProtocolCategory.InvalidPayload
                (ResourceCodec.decode
                    encoded
                    [ ResourceFlavor.AddressingOnlyHandleToken ]
                    [ CatalogFixture.acceptedHandle ]
                    limits)

        Assert.That(fault.LocalCode, Is.EqualTo "forbidden-implicit-copy")

    [<Test>]
    member _.``PB-31 a resource beyond the declared bound is refused before uncontrolled work``() =
        let oversized =
            PortableResource.blob "profile" (Array.zeroCreate (limits.MaxResourceBytes + 1))

        expectCategory
            ProtocolCategory.LimitExceeded
            (ResourceCodec.admit oversized [ ResourceFlavor.CopiedImmutableBlobToken ] [] limits)
        |> ignore

    [<Test>]
    member _.``PB-32 a release signal for the copied flavor is a state violation``() =
        let encoded =
            CborMap
                [ "flavor", CborText ResourceFlavor.CopiedImmutableBlobToken
                  "name", CborText "profile"
                  "content", CborBytes(Text.Encoding.UTF8.GetBytes "cooling-profile")
                  "integrity", CborText(PortableResource.hashOf (Text.Encoding.UTF8.GetBytes "cooling-profile"))
                  "release", CborText "completed" ]

        expectCategory
            ProtocolCategory.StateViolation
            (ResourceCodec.decode encoded [ ResourceFlavor.CopiedImmutableBlobToken ] [] limits)
        |> ignore

    [<Test>]
    member _.``PB-56 a denial still produces a complete observation``() =
        let host = directCoolingHost ()

        let result =
            host
                .Invoke(
                    CoolingFixture.setEnabled,
                    CoolingFixture.commandV1,
                    CoolingFixture.authorizedCommand "primary" true,
                    denied
                )
                .Result

        let observation = result.Observation

        assertAll (fun () ->
            Assert.That(Observation.completenessFailures observation, Is.Empty)
            Assert.That(observation.TerminalStatus, Is.EqualTo TerminalStatus.Denied)
            Assert.That(observation.AuthorityDecisionPoint, Is.EqualTo AuthorityDecisionPoint.HostLocal)
            Assert.That(observation.ProviderEffectCount, Is.EqualTo(Some 0L))
            Assert.That(observation.CopyCount, Is.EqualTo 0L)
            shouldEqual [ "none" ] observation.CrossedBoundaries
            Assert.That(observation.Interrupted, Is.False)
            Assert.That(observation.RetryCount, Is.EqualTo 0L)
            // An unobservable value uses its declared absent form rather than being omitted.
            Assert.That(observation.FailureDomain, Is.EqualTo(Some FailureDomain.LocalEndpoint)))
