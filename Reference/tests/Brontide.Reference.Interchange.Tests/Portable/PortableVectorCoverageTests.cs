using System.Collections.Immutable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// The accounting between the neutral vectors and this stack's evidence.
/// </summary>
/// <remarks>
/// Every neutral vector is either executed here or explicitly deferred with the phase that owns it.
/// The map is checked against the neutral artifacts, so a vector added there without evidence here
/// fails the build rather than silently going uncovered.
/// </remarks>
public sealed class PortableVectorCoverageTests
{
    internal static readonly ImmutableSortedDictionary<string, string> Coverage =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PB-01-EXACT-ESTABLISHMENT"] = "PortableEstablishmentTests",
            ["PB-02-CONTRACT-VERSION-SKEW"] = "PortableEstablishmentTests",
            ["PB-03-REQUIRED-REQUIREMENT-UNMET"] = "PortableEstablishmentTests",
            ["PB-04-OPPOSED-REQUIREMENT-OFFERED"] = "PortableEstablishmentTests",
            ["PB-05-UNKNOWN-CONTRACT-FIELD"] = "PortableEstablishmentTests",
            ["PB-06-UNKNOWN-ENUMERATION-VALUE"] = "PortableEstablishmentTests",
            ["PB-07-COMPACT-IDENTIFIER-BEFORE-NEGOTIATION"] = "PortableEstablishmentTests",
            ["PB-08-NAME-OUTSIDE-PORTABLE-PROFILE"] = "PortableEstablishmentTests",
            ["PB-09-REQUEST-BEFORE-READY"] = "PortableEstablishmentTests",
            ["PB-10-INLINE-NESTED-AND-REPEATED-VALUES"] = "PortableEstablishmentTests",
            ["PB-11-ADDITIVE-PROJECTION"] = "PortableEstablishmentTests",
            ["PB-12-NON-ADDITIVE-SKEW"] = "PortableEstablishmentTests",
            ["PB-13-CLOSED-FRAGMENT-POLICY-VIOLATED"] = "PortableEstablishmentTests",
            ["PB-14-NULL-IN-NON-UNIT-OPTIONAL-FIELD"] = "PortableEstablishmentTests",
            ["PB-15-UNKNOWN-CHOICE-ALTERNATIVE"] = "PortableEstablishmentTests",
            ["PB-16-EXCEPTION-SHAPED-PAYLOAD-DATA"] = "PortableEstablishmentTests",
            ["PB-17-EXCEPTION-SHAPED-CONTROL-DATA"] = "PortableEstablishmentTests",
            ["PB-18-LOCAL-DENIAL-EMITS-NO-FRAME"] = "PortableAuthorityAndResourceTests",
            ["PB-19-AUTHORITY-VALUE-NOT-PROJECTED"] = "PortableAuthorityAndResourceTests",
            ["PB-20-STRONG-KLEENE-ANYOF-PERMITS"] = "PortableAuthorityAndResourceTests",
            ["PB-21-STRONG-KLEENE-ALLOF-DENIES"] = "PortableAuthorityAndResourceTests",
            ["PB-22-CAPABILITY-IN-BODY-ACROSS-TRUST"] = "PortableAuthorityAndResourceTests",
            ["PB-23-MISSING-NO-CAPABILITY-TRANSFER-DECLARATION"] = "PortableAuthorityAndResourceTests",
            ["PB-24-MISSING-REQUIRED-FRAGMENT"] = "PortableAuthorityAndResourceTests",
            ["PB-25-COPIED-IMMUTABLE-BLOB-ACCEPTED"] = "PortableAuthorityAndResourceTests",
            ["PB-26-RESOURCE-INTEGRITY-MISMATCH"] = "PortableAuthorityAndResourceTests",
            ["PB-27-ADDRESSING-HANDLE-ACCEPTED"] = "PortableAuthorityAndResourceTests",
            ["PB-28-RESOURCE-SCOPE-REFUSED"] = "PortableAuthorityAndResourceTests",
            ["PB-29-UNSUPPORTED-RESOURCE-FLAVOR"] = "PortableAuthorityAndResourceTests",
            ["PB-30-FORBIDDEN-IMPLICIT-COPY"] = "PortableAuthorityAndResourceTests",
            ["PB-31-RESOURCE-EXCEEDS-DECLARED-BOUND"] = "PortableAuthorityAndResourceTests",
            ["PB-32-RELEASE-SIGNAL-FOR-COPIED-FLAVOR"] = "PortableAuthorityAndResourceTests",
            ["PB-33-FRAME-EXCEEDS-DECLARED-BOUND"] = "PortableLifecycleAndChannelTests",
            ["PB-34-NESTING-EXCEEDS-DECLARED-DEPTH"] = "PortableLifecycleAndChannelTests",
            ["PB-35-REPLAY-INSIDE-DECLARED-WINDOW"] = "PortableLifecycleAndChannelTests",
            ["PB-36-SECOND-CONCURRENT-REQUEST"] = "PortableLifecycleAndChannelTests",
            ["PB-37-DUPLICATE-TERMINAL-OUTCOME"] = "PortableLifecycleAndChannelTests",
            ["PB-38-CLEAN-WITHDRAWAL-AND-TERMINATION"] = "PortableLifecycleAndChannelTests",
            ["PB-39-ESTABLISH-ON-ESTABLISHED-BINDING"] = "PortableLifecycleAndChannelTests",
            ["PB-40-IO-TIMEOUT"] = "PortableLifecycleAndChannelTests",
            ["PB-41-INTERRUPTED-FRAME"] = "PortableLifecycleAndChannelTests",
            ["PB-42-CORRELATION-ECHO"] = "PortableLifecycleAndChannelTests",
            ["PB-43-CORRELATION-MISMATCH"] = "PortableLifecycleAndChannelTests",
            ["PB-44-MISSING-CORRELATION-IDENTITY"] = "PortableLifecycleAndChannelTests",
            ["PB-45-UNKNOWN-ENVELOPE-KIND"] = "PortableLifecycleAndChannelTests",
            ["PB-46-UNSUPPORTED-OPERATION"] = "PortableLifecycleAndChannelTests",
            ["PB-47-SEMANTIC-FAILED-OUTCOME"] = "PortableLifecycleAndChannelTests",
            ["PB-48-LOCAL-CODE-IS-NON-NORMATIVE"] = "PortableLifecycleAndChannelTests",
            ["PB-49-INTERNAL-PROTOCOL-FAILURE"] = "PortableLifecycleAndChannelTests",
            ["PB-50-PEER-TERMINATED"] = "PortableLifecycleAndChannelTests",
            ["PB-51-PROCESS-FAILURE-CATEGORY-SELECTION"] = "PortableLifecycleAndChannelTests",
            ["PB-52-FAILURE-DOMAIN-RELATIVITY"] = "PortableLifecycleAndChannelTests",
            ["PB-53-PLAN-IS-FROZEN-AND-INSPECTABLE"] = "PortablePlanObservationAndParityTests",
            ["PB-54-COMPACT-IDENTIFIERS-ARE-NOT-IDENTITY"] = "PortablePlanObservationAndParityTests",
            ["PB-55-OBSERVATION-COMPLETE-ON-SUCCESS"] = "PortablePlanObservationAndParityTests",
            ["PB-56-OBSERVATION-COMPLETE-ON-DENIAL"] = "PortableAuthorityAndResourceTests",
            ["PB-57-OBSERVATION-RECORDS-MAPPING-OBLIGATIONS"] = "PortableEstablishmentTests",
            ["PB-58-DIRECT-AND-PROCESS-PARITY-ON-SUCCESS"] = "PortablePlanObservationAndParityTests",
            ["PB-59-DIRECT-AND-PROCESS-PARITY-ON-DENIAL"] = "PortablePlanObservationAndParityTests",
            ["PB-60-COPY-ACCOUNTING-DIFFERS-WITHOUT-BREAKING-PARITY"] = "PortablePlanObservationAndParityTests",

            // PB5 discharged the cross-stack matrix: this stack now hosts a Minimal provider and an
            // implementation-neutral one, so nothing here is deferred.
            ["PB-61-INDEPENDENT-PROVIDER-ACCEPTED-BY-BOTH-HOSTS"] = "PortableNeutralProviderTests",
            ["PB-62-NO-SHARED-SEMANTIC-RUNTIME"] = "PortablePlanObservationAndParityTests",
            ["PB-63-BOTH-HOST-DIRECTIONS"] = "PortableCrossStackTests",

            // Decision 5 gave the Catalog fixture the vectors it had never had: the neutral layer
            // declared what Catalog is without stating what it must do.
            ["PB-64-CATALOG-MULTIPLE-OPERATIONS-NEGOTIATED"] = "PortableCatalogTests",
            ["PB-65-CATALOG-REQUEST-SELECTS-ITS-OPERATION"] = "PortableCatalogTests",
            ["PB-66-CATALOG-SEQUENCE-OF-RECORDS-CARRYING-SEQUENCES"] = "PortableCatalogTests",
            ["PB-67-CATALOG-FAILED-OUTCOME-USES-THE-DECLARED-DETAIL-SHAPE"] = "PortableCatalogTests",
            ["PB-68-CATALOG-HANDLE-IS-NOT-AN-ADMISSION-DECISION"] = "PortableCatalogTests",
            ["PB-69-CATALOG-UNNEGOTIATED-FLAVOR-IN-REQUEST"] = "PortableCatalogTests",

            // Two fixture-domain rules the contract had left unstated, and which the three
            // implementations answered differently until the vectors settled them.
            ["PB-70-CATALOG-PARTIAL-MATCH-FAILS-WHOLE"] = "PortableCatalogTests",
            ["PB-71-CATALOG-UPSERT-COUNT-IS-THIS-REQUEST"] = "PortableCatalogTests",

            // PB7's Composition handoff: the seam from a resolved requirement to a Binding Plan, and
            // the release barrier that keeps ordinary interaction out until the provider is ready.
            ["PB-72-HANDOFF-PRODUCES-A-PLAN"] = "PortableCompositionHandoffTests",
            ["PB-73-PREFLIGHT-FIXES-NO-PLAN-FACT"] = "PortableCompositionHandoffTests",
            ["PB-74-PREFLIGHT-REFUSES-COMPONENT-MISMATCH"] = "PortableCompositionHandoffTests",
            ["PB-75-PREFLIGHT-REFUSES-UNSELECTED-PROVIDER"] = "PortableCompositionHandoffTests",
            ["PB-76-CARDINALITY-BEYOND-ONE-TO-ONE-REFUSED"] = "PortableCompositionHandoffTests",
            ["PB-77-MEDIATED-EXPOSURE-REFUSED"] = "PortableCompositionHandoffTests",
            ["PB-78-ENDPOINT-SUBSTITUTES-PROVIDER"] = "PortableCompositionHandoffTests",
            ["PB-79-INTERACTION-BEFORE-RELEASE-REFUSED"] = "PortableCompositionHandoffTests",
            ["PB-80-RELEASE-REQUIRES-READY"] = "PortableCompositionHandoffTests",
            ["PB-81-RELEASED-BINDING-INTERACTS"] = "PortableCompositionHandoffTests",
            ["PB-82-WITHDRAWAL-INFORMS-A-REPLACEMENT"] = "PortableCompositionHandoffTests"
        }.ToImmutableSortedDictionary(StringComparer.Ordinal);

    [Test]
    public void Every_neutral_vector_is_either_executed_here_or_explicitly_deferred()
    {
        var declared = PortableTestHarness.NeutralVectorIds();

        Assert.Multiple(() =>
        {
            Assert.That(declared, Is.Not.Empty);
            Assert.That(
                declared.Where(id => !Coverage.ContainsKey(id)),
                Is.Empty,
                "A neutral vector has no recorded Reference evidence or deferral.");
            Assert.That(
                Coverage.Keys.Where(id => !declared.Contains(id)),
                Is.Empty,
                "The coverage map names a vector the neutral layer no longer declares.");
        });
    }

    /// <summary>
    /// Nothing is deferred any more. PB5 was the last phase that owed a vector no single stack could
    /// discharge alone, so a deferral reappearing here is a claim that needs its own justification.
    /// </summary>
    [Test]
    public void No_neutral_vector_is_deferred()
    {
        var deferred = Coverage
            .Where(entry => entry.Value.StartsWith("deferred:", StringComparison.Ordinal))
            .Select(entry => entry.Key)
            .ToImmutableArray();

        Assert.That(deferred, Is.Empty);
    }
}
