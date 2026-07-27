using System.Collections.Immutable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// PB4's other half: every Channel 0.1 vector executes in this stack, independently of the other.
/// </summary>
/// <remarks>
/// The accounting is derived, never restated. A Channel vector counts as executed here only when
/// some portable vector that the neutral artifacts say preserves it is executed by this stack's
/// evidence — so removing a test, or renaming a Channel vector, fails the build instead of quietly
/// leaving a hole. The two source files are the authored
/// <c>conformance/channel-0.1-vectors.json</c> and the neutral portable vectors.
/// </remarks>
public sealed class PortableChannelVectorCoverageTests
{
    private static ImmutableDictionary<string, ImmutableArray<string>> ExecutingVectors()
    {
        var references = PortableTestHarness.NeutralChannelReferences();
        var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string>>(StringComparer.Ordinal);
        foreach (var channelVector in PortableTestHarness.ChannelVectorIds())
        {
            builder[channelVector] =
            [
                .. references
                    .Where(entry => entry.Value.Contains(channelVector))
                    .Where(entry => Executed(entry.Key))
                    .Select(entry => entry.Key)
                    .Order(StringComparer.Ordinal)
            ];
        }

        return builder.ToImmutable();
    }

    /// <summary>A portable vector counts when this stack runs it rather than defers it.</summary>
    private static bool Executed(string vector) =>
        PortableVectorCoverageTests.Coverage.TryGetValue(vector, out var evidence) &&
        !evidence.StartsWith("deferred:", StringComparison.Ordinal);

    [Test]
    public void Every_Channel_vector_is_executed_by_this_stack()
    {
        var executing = ExecutingVectors();
        var uncovered = executing
            .Where(entry => entry.Value.IsEmpty)
            .Select(entry => entry.Key)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();

        Assert.Multiple(() =>
        {
            Assert.That(executing, Is.Not.Empty);
            Assert.That(
                uncovered,
                Is.Empty,
                "A Channel 0.1 vector has no executed portable vector in this stack: " + string.Join(", ", uncovered));
        });
    }

    /// <summary>
    /// The cross-stack deferral is bounded: PB5's deferred vectors preserve no Channel vector that
    /// only they cover, so deferring them leaves the Channel taxonomy fully executed here.
    /// </summary>
    [Test]
    public void No_Channel_vector_depends_on_a_deferred_portable_vector_alone()
    {
        var references = PortableTestHarness.NeutralChannelReferences();
        var deferredOnly = PortableTestHarness.ChannelVectorIds()
            .Where(channelVector =>
            {
                var referencing = references.Where(entry => entry.Value.Contains(channelVector)).ToImmutableArray();
                return referencing.Length > 0 && referencing.All(entry => !Executed(entry.Key));
            })
            .ToImmutableArray();

        Assert.That(deferredOnly, Is.Empty);
    }

    /// <summary>
    /// The parity matrix carries the Channel vectors PB4 names specifically: correlation, payload
    /// covariance, the two strong-Kleene outcomes, the frameless denial, the shaped failed Outcome,
    /// and the two rejection categories a host can reach.
    /// </summary>
    [Test]
    public void The_parity_matrix_measures_the_Channel_vectors_PB4_names()
    {
        var measured = PortableParityMatrix.Scenarios
            .SelectMany(scenario => scenario.ChannelVectors)
            .ToImmutableHashSet();

        Assert.That(measured, Is.SupersetOf(new[]
        {
            "CH-01-CORRELATION-ECHO",
            "CH-07-PAYLOAD-COVARIANCE",
            "CH-08-AUTHORITY-NO-PROJECTION",
            "CH-09-STRONG-KLEENE-FALLBACK",
            "CH-10-STRONG-KLEENE-UNKNOWN-DENIES",
            "CH-11-NO-CAPABILITY-TRANSFER",
            "CH-12-DENIAL-IS-NOT-A-FRAME",
            "CH-13-SEMANTIC-FAILED-OUTCOME",
            "CH-19-UNSUPPORTED-OPERATION",
            "CH-20-INVALID-PAYLOAD"
        }));
    }
}
