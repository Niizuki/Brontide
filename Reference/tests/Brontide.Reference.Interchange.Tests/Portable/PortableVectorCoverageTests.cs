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

            // PB5 owns the cross-stack matrix. Reference alone cannot host a Minimal provider or an
            // implementation-neutral one, so these stay deferred rather than being claimed here.
            ["PB-61-INDEPENDENT-PROVIDER-ACCEPTED-BY-BOTH-HOSTS"] = "deferred:PB5",
            ["PB-62-NO-SHARED-SEMANTIC-RUNTIME"] = "PortablePlanObservationAndParityTests",
            ["PB-63-BOTH-HOST-DIRECTIONS"] = "deferred:PB5"
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

    [Test]
    public void The_deferred_vectors_are_exactly_the_cross_stack_matrix()
    {
        var deferred = Coverage
            .Where(entry => entry.Value.StartsWith("deferred:", StringComparison.Ordinal))
            .Select(entry => entry.Key)
            .ToImmutableArray();

        Assert.That(deferred, Is.EquivalentTo(new[]
        {
            "PB-61-INDEPENDENT-PROVIDER-ACCEPTED-BY-BOTH-HOSTS",
            "PB-63-BOTH-HOST-DIRECTIONS"
        }));
    }
}
