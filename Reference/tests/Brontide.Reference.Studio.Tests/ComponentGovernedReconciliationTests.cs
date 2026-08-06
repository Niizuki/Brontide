using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed record Cbi63Observation(
        string Code,
        string Phase,
        string RotationApplied,
        string PolicyApplied,
        int Interruptions,
        int Retries);

    private static JsonDocument Cbi63Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi63-governed-reconciliation-vectors.json")));

    private static readonly ProviderTrustCadenceRunId Cbi63Run =
        ProviderTrustCadenceRunId.Create("cbi63-governed-run");

    /// <summary>
    /// Interrupts one governed attempt, advances the registry by the named effects, and applies one
    /// serving observation. The effects are produced by the real registry rather than described, so
    /// the derivation is compared against something a wrong implementation could disagree with.
    /// </summary>
    private static Cbi63Observation Cbi63RunVector(JsonElement vector)
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi63-{Guid.NewGuid():N}");
        var journalPath = Path.Combine(root, "cadence.bin");
        try
        {
            using var pin = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var successor = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var identity = Cbi57Authority(pin);
            var registry = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), identity).Registry!;
            var journal = DurableProviderTrustCadenceJournal.Establish(
                journalPath, Cbi63Run,
                ProviderServingTrustCadenceSchedule.Create(3, TimeSpan.FromSeconds(5)),
                Cbi62Start).Journal!;

            var cursorKind = vector.GetProperty("cursor").GetString();
            var cursor = cursorKind switch
            {
                "absent" => null,
                // A cursor ahead of the registry is the rollback case: nothing advanced, yet the
                // recorded baseline claims more than the chain holds.
                "recorded-ahead" => new ProviderTrustCadenceJournalCursor(
                    5, Cbi57Authority(successor).Value, 0, null),
                _ => ProviderGovernedTrustCadenceRecovery.Cursor(registry),
            };
            Assert.That(journal.BeginCycle(cursor).Code, Is.EqualTo("durable-cadence-cycle-started"));

            var effects = vector.GetProperty("effects").GetString()!;
            if (effects is "rotation" or "rotation-and-policy")
                Assert.That(registry.Rotate(Cbi57Statement(1, 0, null, pin, successor)).IsApplied, Is.True);
            if (effects is "policy" or "rotation-and-policy")
            {
                var signer = effects == "rotation-and-policy" ? successor : pin;
                Assert.That(registry.Apply(Cbi37Sign(signer, 1, null, Cbi41Policy(1))).IsApplied, Is.True);
            }

            var serving = vector.GetProperty("serving").GetString()!;
            var evidence = new ProviderGovernedReconciliationEvidence(
                Cbi63Run,
                serving == "wrong-index" ? 7 : journal.Snapshot.NextCycleIndex,
                Cbi62Start,
                serving switch
                {
                    "effects-accounted-for" => ProviderGovernedServingObservation.EffectsAccountedFor,
                    "unknown" => ProviderGovernedServingObservation.Unknown,
                    _ => ProviderGovernedServingObservation.NoEffectsConfirmed,
                });

            var result = ProviderGovernedInterruptionReconciliation.Apply(journal, evidence, registry);
            return new(result.Code, result.Snapshot.Phase,
                result.Derived is null ? "none" : result.Derived.RotationApplied.ToString(),
                result.Derived is null ? "none" : result.Derived.PolicyApplied.ToString(),
                result.Snapshot.InterruptionCount, result.Snapshot.RetryCount);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static Cbi63Observation Cbi63Expected(JsonElement vector)
    {
        static string Flag(JsonElement value) =>
            value.ValueKind == JsonValueKind.Null ? "none" : value.GetBoolean().ToString();
        return new(
            vector.GetProperty("code").GetString()!,
            vector.GetProperty("phase").GetString()!,
            Flag(vector.GetProperty("rotationApplied")),
            Flag(vector.GetProperty("policyApplied")),
            vector.GetProperty("interruptions").GetInt32(),
            vector.GetProperty("retries").GetInt32());
    }

    private static JsonElement Cbi63Vector(JsonDocument fixture, string name) =>
        fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(vector => vector.GetProperty("name").GetString() == name);

    [Test]
    public void Shared_cbi63_vectors_reconcile_a_governed_interruption()
    {
        using var fixture = Cbi63Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
            Assert.That(Cbi63RunVector(vector), Is.EqualTo(Cbi63Expected(vector)),
                $"vector {vector.GetProperty("name").GetString()}");
    }

    [Test]
    public void Cbi63_C1_recording_the_cursor_adds_no_journal_write()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi63-writes-{Guid.NewGuid():N}");
        try
        {
            using var pin = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var registry = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), Cbi57Authority(pin)).Registry!;
            List<string> Transitions(bool governed)
            {
                var journal = DurableProviderTrustCadenceJournal.Establish(
                    Path.Combine(root, $"{governed}.bin"), Cbi63Run,
                    ProviderServingTrustCadenceSchedule.Create(3, TimeSpan.FromSeconds(5)),
                    Cbi62Start).Journal!;
                List<string> codes =
                [
                    journal.BeginCycle(
                        governed ? ProviderGovernedTrustCadenceRecovery.Cursor(registry) : null).Code,
                    journal.CommitCycle(ProviderServingTrustCycleCodes.Current).Code,
                ];
                // The cursor describes an attempt in flight and does not outlive it.
                Assert.That(journal.Snapshot.Cursor, Is.Null);
                return codes;
            }

            var ungoverned = Transitions(false);
            var governedCodes = Transitions(true);
            // A governed run performs exactly the transitions an ungoverned one does.
            Assert.That(governedCodes, Is.EqualTo(ungoverned));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi63_C2_a_governed_interruption_is_refused_by_the_ungoverned_path()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi63-path-{Guid.NewGuid():N}");
        try
        {
            using var pin = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var registry = DurableProviderPublisherTrustPolicyRegistry.Open(
                Path.Combine(root, "policy.checkpoint"), Cbi57Authority(pin)).Registry!;
            var governedPath = Path.Combine(root, "governed.bin");
            var governed = DurableProviderTrustCadenceJournal.Establish(
                governedPath, Cbi63Run,
                ProviderServingTrustCadenceSchedule.Create(3, TimeSpan.FromSeconds(5)),
                Cbi62Start).Journal!;
            Assert.That(governed.BeginCycle(
                ProviderGovernedTrustCadenceRecovery.Cursor(registry)).Code,
                Is.EqualTo("durable-cadence-cycle-started"));
            var before = File.ReadAllBytes(governedPath);

            var refused = ProviderTrustCadenceReconciliation.Apply(governed,
                new ProviderTrustCadenceReconciliationEvidence(
                    Cbi63Run, 0, Cbi62Start, ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed));

            // The ungoverned run is unaffected, so the refusal is about the recorded cursor rather
            // than about the path having become unusable.
            var ungoverned = DurableProviderTrustCadenceJournal.Establish(
                Path.Combine(root, "ungoverned.bin"), Cbi63Run,
                ProviderServingTrustCadenceSchedule.Create(3, TimeSpan.FromSeconds(5)),
                Cbi62Start).Journal!;
            Assert.That(ungoverned.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));
            var accepted = ProviderTrustCadenceReconciliation.Apply(ungoverned,
                new ProviderTrustCadenceReconciliationEvidence(
                    Cbi63Run, 0, Cbi62Start, ProviderTrustCadenceReconciliationVerdict.NoEffectsConfirmed));

            Assert.Multiple(() =>
            {
                Assert.That(refused.Code, Is.EqualTo("cadence-reconciliation-governed"));
                Assert.That(refused.Snapshot.Phase, Is.EqualTo("in-flight"));
                Assert.That(File.ReadAllBytes(governedPath), Is.EqualTo(before));
                Assert.That(accepted.Code, Is.EqualTo("cadence-reconciliation-retry-ready"));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi63_C4_the_derived_effects_come_from_the_registry_and_not_the_evidence()
    {
        using var fixture = Cbi63Fixture();
        // Identical evidence over four different registry outcomes: only the derivation moves, which
        // is what makes it derived rather than restated.
        var observations = new[]
        {
            Cbi63RunVector(Cbi63Vector(fixture, "no-effects-confirmed-retries")),
            Cbi63RunVector(Cbi63Vector(fixture, "a-rotation-is-derived-not-asserted")),
            Cbi63RunVector(Cbi63Vector(fixture, "a-policy-update-is-derived-not-asserted")),
            Cbi63RunVector(Cbi63Vector(fixture, "both-derived-effects-still-permit-retry")),
        };
        Assert.Multiple(() =>
        {
            Assert.That(observations.Select(value => value.Code).Distinct().Single(),
                Is.EqualTo("governed-reconciliation-retry-ready"));
            Assert.That(observations.Select(value => $"{value.RotationApplied}/{value.PolicyApplied}"),
                Is.EqualTo(new[] { "False/False", "True/False", "False/True", "True/True" }));
        });
    }

    [Test]
    public void Cbi63_C5_an_absent_or_regressed_cursor_derives_nothing()
    {
        using var fixture = Cbi63Fixture();
        foreach (var name in new[]
                 {
                     "an-absent-cursor-is-refused-not-guessed",
                     "a-regressed-cursor-is-refused",
                 })
        {
            var actual = Cbi63RunVector(Cbi63Vector(fixture, name));
            Assert.Multiple(() =>
            {
                Assert.That(actual.Phase, Is.EqualTo("in-flight"), name);
                Assert.That(actual.RotationApplied, Is.EqualTo("none"), name);
                Assert.That(actual.PolicyApplied, Is.EqualTo("none"), name);
                Assert.That(actual.Interruptions, Is.Zero, name);
            });
        }
    }

    [Test]
    public void Cbi63_C6_the_serving_verdict_alone_decides_and_counts_as_Cbi49_does()
    {
        using var fixture = Cbi63Fixture();
        var retried = Cbi63RunVector(Cbi63Vector(fixture, "both-derived-effects-still-permit-retry"));
        var abandoned = Cbi63RunVector(Cbi63Vector(fixture, "effects-accounted-for-abandons"));
        var deferred = Cbi63RunVector(Cbi63Vector(fixture, "a-derived-effect-with-unknown-serving-still-defers"));
        Assert.Multiple(() =>
        {
            // Two derived effects and a retry: the derivation reports, it does not veto.
            Assert.That(retried.RotationApplied, Is.EqualTo("True"));
            Assert.That((retried.Interruptions, retried.Retries), Is.EqualTo((1, 1)));
            Assert.That((abandoned.Interruptions, abandoned.Retries), Is.EqualTo((1, 0)));
            Assert.That((deferred.Interruptions, deferred.Retries), Is.EqualTo((0, 0)));
            Assert.That(deferred.Phase, Is.EqualTo("in-flight"));
        });
    }
}
