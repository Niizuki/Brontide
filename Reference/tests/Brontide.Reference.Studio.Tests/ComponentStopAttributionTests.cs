using System.Text.Json;
using Brontide.Reference.Experimental.ComponentManagement;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private static JsonDocument Cbi67Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi67-stop-attribution-vectors.json")));

    private static ProviderRestartCause Cbi67Cause(string name) => name switch
    {
        "offline-availability" => ProviderRestartCause.OfflineAvailability,
        "publisher-trust-withdrawal" => ProviderRestartCause.PublisherTrustWithdrawal,
        "operator-retirement" => ProviderRestartCause.OperatorRetirement,
        "unexpected-exit" => ProviderRestartCause.UnexpectedExit,
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    private sealed record Cbi67Observation(string Code, string Cause, bool RestartRefused);

    /// <summary>
    /// Seeds one store as the vector describes and asks it about the activation's identities. No
    /// provider is launched: what the store answers is decided by the record it holds, and that CBI51
    /// acts on the answer is pinned by the restart scenarios, which do run real providers.
    /// </summary>
    private static Cbi67Observation Cbi67Run(JsonElement fixture, JsonElement vector)
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi67-{Guid.NewGuid():N}");
        try
        {
            var store = DurableProviderStopAttributionStore
                .Open(Path.Combine(root, "stops.bin")).Store!;
            var occurrence = OccurrenceId.Create(fixture.GetProperty("occurrence").GetString()!);
            var staged = ProviderArtifactSetId.Create(
                fixture.GetProperty("stagedIdentity").GetString()!);
            var other = ProviderArtifactSetId.Create(
                fixture.GetProperty("otherStagedIdentity").GetString()!);
            var recordedAt = DateTimeOffset.FromUnixTimeSeconds(
                fixture.GetProperty("recordedAtUnixSeconds").GetInt64());

            var recorded = vector.GetProperty("recorded");
            if (recorded.ValueKind != JsonValueKind.Null)
            {
                var under = vector.GetProperty("recordedUnder").GetString() == "other" ? other : staged;
                Assert.That(
                    store.Record(occurrence, under, recordedAt, Cbi67Cause(recorded.GetString()!)),
                    Is.EqualTo("provider-stop-attribution-recorded"));
            }

            var result = store.Attribute(occurrence, staged);
            var cause = result.Attribution?.Cause;
            return new(
                result.Code,
                cause is null ? "none" : Cbi67CauseName(cause.Value),
                cause is ProviderRestartCause.PublisherTrustWithdrawal
                    or ProviderRestartCause.OperatorRetirement || result.Attribution is null);
        }
        finally
        {
            Cbi32DeleteTree(root);
        }
    }

    private static string Cbi67CauseName(ProviderRestartCause cause) => cause switch
    {
        ProviderRestartCause.OfflineAvailability => "offline-availability",
        ProviderRestartCause.PublisherTrustWithdrawal => "publisher-trust-withdrawal",
        ProviderRestartCause.OperatorRetirement => "operator-retirement",
        _ => "unexpected-exit",
    };

    private static Cbi67Observation Cbi67Expected(JsonElement vector)
    {
        var cause = vector.GetProperty("cause");
        return new(
            vector.GetProperty("code").GetString()!,
            cause.ValueKind == JsonValueKind.Null ? "none" : cause.GetString()!,
            vector.GetProperty("restartRefused").GetBoolean());
    }

    [Test]
    public void Shared_cbi67_vectors_attribute_a_stop()
    {
        using var fixture = Cbi67Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = Cbi67Run(fixture.RootElement, vector);
            Assert.That(actual, Is.EqualTo(Cbi67Expected(vector)),
                $"vector {vector.GetProperty("name").GetString()}");
        }
    }

    /// <summary>
    /// The store is the only issuer. A caller cannot construct an attribution, which is the whole of
    /// what C2 buys: CBI51's refusals are unchanged and the caller no longer chooses which applies.
    /// </summary>
    [Test]
    public void Cbi67_C2_an_attribution_has_no_public_construction_path()
    {
        var constructors = typeof(ProviderStopAttribution)
            .GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Assert.That(constructors, Is.Empty);
    }

    [Test]
    public void Cbi67_C4_absence_yields_one_cause_and_never_a_refusal()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi67-{Guid.NewGuid():N}");
        try
        {
            var store = DurableProviderStopAttributionStore.Open(Path.Combine(root, "stops.bin")).Store!;
            var result = store.Attribute(
                OccurrenceId.Create("occ.def.test.absent.1"),
                ProviderArtifactSetId.Create(new string('A', 64)));
            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo("provider-stop-attribution-unrecorded"));
                Assert.That(result.Attribution!.Cause, Is.EqualTo(ProviderRestartCause.UnexpectedExit));
                Assert.That(result.Attribution.Instant, Is.Null);
            });
        }
        finally
        {
            Cbi32DeleteTree(root);
        }
    }

    [Test]
    public void Cbi67_C5_an_unexpected_exit_cannot_be_recorded()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi67-{Guid.NewGuid():N}");
        try
        {
            var store = DurableProviderStopAttributionStore.Open(Path.Combine(root, "stops.bin")).Store!;
            // Absence is what an unexpected exit is. A record naming it would be a record of the host
            // not having stopped anything, and the operator path is the only way the one cause this
            // slice exists to attribute comes into existence.
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Record(
                OccurrenceId.Create("occ.def.test.cooling-provider.1"),
                ProviderArtifactSetId.Create(new string('A', 64)),
                DateTimeOffset.UnixEpoch,
                ProviderRestartCause.UnexpectedExit));
        }
        finally
        {
            Cbi32DeleteTree(root);
        }
    }

    [Test]
    public void Cbi67_C6_a_cleared_record_no_longer_attributes()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi67-{Guid.NewGuid():N}");
        try
        {
            var store = DurableProviderStopAttributionStore.Open(Path.Combine(root, "stops.bin")).Store!;
            var occurrence = OccurrenceId.Create("occ.def.test.cooling-provider.1");
            var staged = ProviderArtifactSetId.Create(new string('A', 64));
            store.Record(occurrence, staged, DateTimeOffset.UnixEpoch,
                ProviderRestartCause.OfflineAvailability);
            Assert.Multiple(() =>
            {
                Assert.That(store.Attribute(occurrence, staged).Code,
                    Is.EqualTo("provider-stop-attribution-issued"));
                Assert.That(store.Clear(occurrence), Is.EqualTo("provider-stop-attribution-cleared"));
                // A stale record must not authorize a second restart of a provider already running.
                Assert.That(store.Attribute(occurrence, staged).Code,
                    Is.EqualTo("provider-stop-attribution-unrecorded"));
                Assert.That(store.Clear(occurrence), Is.EqualTo("provider-stop-attribution-absent"));
            });
        }
        finally
        {
            Cbi32DeleteTree(root);
        }
    }

    [Test]
    public void Cbi67_C7_a_corrupted_record_is_refused_and_survives_a_reopen_when_intact()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi67-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "stops.bin");
        try
        {
            var store = DurableProviderStopAttributionStore.Open(path).Store!;
            var occurrence = OccurrenceId.Create("occ.def.test.cooling-provider.1");
            var staged = ProviderArtifactSetId.Create(new string('A', 64));
            store.Record(occurrence, staged, DateTimeOffset.UnixEpoch,
                ProviderRestartCause.OperatorRetirement);

            var reopened = DurableProviderStopAttributionStore.Open(path);
            var intact = reopened.Store!.Attribute(occurrence, staged);

            // A byte the parser accepts, so only the tag can refuse it — the case a store that never
            // checked its tag would pass, which CBI42 had to learn by deliberate defect.
            var bytes = File.ReadAllBytes(path);
            bytes[^40] ^= 0x01;
            File.WriteAllBytes(path, bytes);
            var corrupt = DurableProviderStopAttributionStore.Open(path);

            Assert.Multiple(() =>
            {
                Assert.That(reopened.Code, Is.EqualTo("provider-stop-attribution-opened"));
                Assert.That(intact.Attribution!.Cause,
                    Is.EqualTo(ProviderRestartCause.OperatorRetirement));
                Assert.That(corrupt.Code, Is.EqualTo("provider-stop-attribution-corrupt"));
                Assert.That(corrupt.Store, Is.Null);
            });
        }
        finally
        {
            Cbi32DeleteTree(root);
        }
    }
}
