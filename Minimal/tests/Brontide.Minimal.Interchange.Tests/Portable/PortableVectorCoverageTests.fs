namespace Brontide.Minimal.Interchange.Tests.Portable

open NUnit.Framework

/// The accounting between the neutral vectors and this stack's evidence.
///
/// Every neutral vector is either executed here or explicitly deferred with the phase that owns it.
/// The map is checked against the neutral artifacts, so a vector added there without evidence here
/// fails the build rather than silently going uncovered. The Channel accounting reads it too, which
/// is why it is a module value rather than a field of the fixture.
[<RequireQualifiedAccess>]
module PortableVectorCoverage =

    let map =
        Map.ofList
            [ "PB-01-EXACT-ESTABLISHMENT", "PortableEstablishmentTests"
              "PB-02-CONTRACT-VERSION-SKEW", "PortableEstablishmentTests"
              "PB-03-REQUIRED-REQUIREMENT-UNMET", "PortableEstablishmentTests"
              "PB-04-OPPOSED-REQUIREMENT-OFFERED", "PortableEstablishmentTests"
              "PB-05-UNKNOWN-CONTRACT-FIELD", "PortableEstablishmentTests"
              "PB-06-UNKNOWN-ENUMERATION-VALUE", "PortableEstablishmentTests"
              "PB-07-COMPACT-IDENTIFIER-BEFORE-NEGOTIATION", "PortableEstablishmentTests"
              "PB-08-NAME-OUTSIDE-PORTABLE-PROFILE", "PortableEstablishmentTests"
              "PB-09-REQUEST-BEFORE-READY", "PortableEstablishmentTests"
              "PB-10-INLINE-NESTED-AND-REPEATED-VALUES", "PortableEstablishmentTests"
              "PB-11-ADDITIVE-PROJECTION", "PortableEstablishmentTests"
              "PB-12-NON-ADDITIVE-SKEW", "PortableEstablishmentTests"
              "PB-13-CLOSED-FRAGMENT-POLICY-VIOLATED", "PortableEstablishmentTests"
              "PB-14-NULL-IN-NON-UNIT-OPTIONAL-FIELD", "PortableEstablishmentTests"
              "PB-15-UNKNOWN-CHOICE-ALTERNATIVE", "PortableEstablishmentTests"
              "PB-16-EXCEPTION-SHAPED-PAYLOAD-DATA", "PortableEstablishmentTests"
              "PB-17-EXCEPTION-SHAPED-CONTROL-DATA", "PortableEstablishmentTests"
              "PB-18-LOCAL-DENIAL-EMITS-NO-FRAME", "PortableAuthorityAndResourceTests"
              "PB-19-AUTHORITY-VALUE-NOT-PROJECTED", "PortableAuthorityAndResourceTests"
              "PB-20-STRONG-KLEENE-ANYOF-PERMITS", "PortableAuthorityAndResourceTests"
              "PB-21-STRONG-KLEENE-ALLOF-DENIES", "PortableAuthorityAndResourceTests"
              "PB-22-CAPABILITY-IN-BODY-ACROSS-TRUST", "PortableAuthorityAndResourceTests"
              "PB-23-MISSING-NO-CAPABILITY-TRANSFER-DECLARATION", "PortableAuthorityAndResourceTests"
              "PB-24-MISSING-REQUIRED-FRAGMENT", "PortableAuthorityAndResourceTests"
              "PB-25-COPIED-IMMUTABLE-BLOB-ACCEPTED", "PortableAuthorityAndResourceTests"
              "PB-26-RESOURCE-INTEGRITY-MISMATCH", "PortableAuthorityAndResourceTests"
              "PB-27-ADDRESSING-HANDLE-ACCEPTED", "PortableAuthorityAndResourceTests"
              "PB-28-RESOURCE-SCOPE-REFUSED", "PortableAuthorityAndResourceTests"
              "PB-29-UNSUPPORTED-RESOURCE-FLAVOR", "PortableAuthorityAndResourceTests"
              "PB-30-FORBIDDEN-IMPLICIT-COPY", "PortableAuthorityAndResourceTests"
              "PB-31-RESOURCE-EXCEEDS-DECLARED-BOUND", "PortableAuthorityAndResourceTests"
              "PB-32-RELEASE-SIGNAL-FOR-COPIED-FLAVOR", "PortableAuthorityAndResourceTests"
              "PB-33-FRAME-EXCEEDS-DECLARED-BOUND", "PortableLifecycleAndChannelTests"
              "PB-34-NESTING-EXCEEDS-DECLARED-DEPTH", "PortableLifecycleAndChannelTests"
              "PB-35-REPLAY-INSIDE-DECLARED-WINDOW", "PortableLifecycleAndChannelTests"
              "PB-36-SECOND-CONCURRENT-REQUEST", "PortableLifecycleAndChannelTests"
              "PB-37-DUPLICATE-TERMINAL-OUTCOME", "PortableLifecycleAndChannelTests"
              "PB-38-CLEAN-WITHDRAWAL-AND-TERMINATION", "PortableLifecycleAndChannelTests"
              "PB-39-ESTABLISH-ON-ESTABLISHED-BINDING", "PortableLifecycleAndChannelTests"
              "PB-40-IO-TIMEOUT", "PortableLifecycleAndChannelTests"
              "PB-41-INTERRUPTED-FRAME", "PortableLifecycleAndChannelTests"
              "PB-42-CORRELATION-ECHO", "PortableLifecycleAndChannelTests"
              "PB-43-CORRELATION-MISMATCH", "PortableLifecycleAndChannelTests"
              "PB-44-MISSING-CORRELATION-IDENTITY", "PortableLifecycleAndChannelTests"
              "PB-45-UNKNOWN-ENVELOPE-KIND", "PortableLifecycleAndChannelTests"
              "PB-46-UNSUPPORTED-OPERATION", "PortableLifecycleAndChannelTests"
              "PB-47-SEMANTIC-FAILED-OUTCOME", "PortableLifecycleAndChannelTests"
              "PB-48-LOCAL-CODE-IS-NON-NORMATIVE", "PortableLifecycleAndChannelTests"
              "PB-49-INTERNAL-PROTOCOL-FAILURE", "PortableLifecycleAndChannelTests"
              "PB-50-PEER-TERMINATED", "PortableLifecycleAndChannelTests"
              "PB-51-PROCESS-FAILURE-CATEGORY-SELECTION", "PortableLifecycleAndChannelTests"
              "PB-52-FAILURE-DOMAIN-RELATIVITY", "PortableLifecycleAndChannelTests"
              "PB-53-PLAN-IS-FROZEN-AND-INSPECTABLE", "PortablePlanObservationAndParityTests"
              "PB-54-COMPACT-IDENTIFIERS-ARE-NOT-IDENTITY", "PortablePlanObservationAndParityTests"
              "PB-55-OBSERVATION-COMPLETE-ON-SUCCESS", "PortablePlanObservationAndParityTests"
              "PB-56-OBSERVATION-COMPLETE-ON-DENIAL", "PortableAuthorityAndResourceTests"
              "PB-57-OBSERVATION-RECORDS-MAPPING-OBLIGATIONS", "PortableEstablishmentTests"
              "PB-58-DIRECT-AND-PROCESS-PARITY-ON-SUCCESS", "PortablePlanObservationAndParityTests"
              "PB-59-DIRECT-AND-PROCESS-PARITY-ON-DENIAL", "PortablePlanObservationAndParityTests"
              "PB-60-COPY-ACCOUNTING-DIFFERS-WITHOUT-BREAKING-PARITY", "PortablePlanObservationAndParityTests"

              // PB5 owns the cross-stack matrix. Minimal alone cannot host a Reference provider or an
              // implementation-neutral one, so these stay deferred rather than being claimed here.
              "PB-61-INDEPENDENT-PROVIDER-ACCEPTED-BY-BOTH-HOSTS", "deferred:PB5"
              "PB-62-NO-SHARED-SEMANTIC-RUNTIME", "PortablePlanObservationAndParityTests"
              "PB-63-BOTH-HOST-DIRECTIONS", "deferred:PB5" ]

    /// A portable vector counts as executed when this stack runs it rather than defers it.
    let executed vector =
        match Map.tryFind vector map with
        | Some evidence -> not (evidence.StartsWith "deferred:")
        | None -> false

[<TestFixture>]
type PortableVectorCoverageTests() =

    let coverage = PortableVectorCoverage.map

    [<Test>]
    member _.``every neutral vector is either executed here or explicitly deferred``() =
        let declared = neutralVectorIds ()

        assertAll (fun () ->
            Assert.That(declared, Is.Not.Empty)

            Assert.That(
                declared |> List.filter (fun id -> not (Map.containsKey id coverage)),
                Is.Empty,
                "A neutral vector has no recorded Minimal evidence or deferral."
            )

            Assert.That(
                coverage |> Map.toList |> List.map fst |> List.filter (fun id -> not (List.contains id declared)),
                Is.Empty,
                "The coverage map names a vector the neutral layer no longer declares."
            ))

    [<Test>]
    member _.``the deferred vectors are exactly the cross-stack matrix``() =
        let deferred =
            coverage
            |> Map.toList
            |> List.filter (fun (_, evidence) -> evidence.StartsWith "deferred:")
            |> List.map fst

        Assert.That(
            deferred,
            Is.EquivalentTo [ "PB-61-INDEPENDENT-PROVIDER-ACCEPTED-BY-BOTH-HOSTS"; "PB-63-BOTH-HOST-DIRECTIONS" ]
        )
