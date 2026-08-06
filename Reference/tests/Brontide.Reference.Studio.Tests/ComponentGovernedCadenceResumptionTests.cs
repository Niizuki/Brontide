using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed record Cbi62Observation(
        string Code,
        string Phase,
        string Committed,
        int NextCycleIndex,
        int Gaps,
        int Interruptions,
        int Retries);

    private static JsonDocument Cbi62Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi62-governed-cadence-resumption-vectors.json")));

    private static readonly ProviderTrustCadenceRunId Cbi62Run =
        ProviderTrustCadenceRunId.Create("cbi62-governed-run");

    private static readonly DateTimeOffset Cbi62Start = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);

    private static Cbi62Observation Cbi62Run_(JsonElement fixture, JsonElement vector, string path)
    {
        var interval = TimeSpan.FromMilliseconds(
            fixture.GetProperty("schedule").GetProperty("intervalMilliseconds").GetInt32());
        var journal = DurableProviderTrustCadenceJournal.Establish(
            path, Cbi62Run,
            ProviderServingTrustCadenceSchedule.Create(
                vector.GetProperty("maximumCycles").GetInt32(), interval),
            Cbi62Start).Journal!;
        var code = "durable-cadence-established";

        foreach (var step in vector.GetProperty("steps").EnumerateArray())
        {
            if (journal.Snapshot.Phase == "waiting")
            {
                var gap = journal.CompleteGap(journal.Snapshot.PreparedInstant + interval);
                Assert.That(gap.Code, Is.EqualTo("durable-cadence-gap-completed"));
            }
            Assert.That(journal.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));

            if (step.TryGetProperty("interrupt", out var decision))
            {
                // Reopening is what a restart does, and it must see the interruption rather than a
                // cursor that advanced on its own.
                var reopened = DurableProviderTrustCadenceJournal.Open(path, Cbi62Run);
                Assert.That(reopened.Code, Is.EqualTo("durable-cadence-indeterminate"));
                journal = reopened.Journal!;
                code = journal.ResolveInterrupted(decision.GetString() == "retry"
                    ? ProviderTrustCadenceRecoveryDecision.Retry
                    : ProviderTrustCadenceRecoveryDecision.Abandon).Code;
                continue;
            }

            code = journal.CommitCycle(step.GetProperty("commit").GetString()!).Code;
        }

        var snapshot = journal.Snapshot;
        return new(code, snapshot.Phase,
            Cbi60Join(snapshot.Cycles.Select(cycle => cycle.Code)),
            snapshot.NextCycleIndex, snapshot.Gaps.Count,
            snapshot.InterruptionCount, snapshot.RetryCount);
    }

    private static Cbi62Observation Cbi62RunVector(JsonElement fixture, JsonElement vector)
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi62-{Guid.NewGuid():N}");
        try { return Cbi62Run_(fixture, vector, Path.Combine(root, "cadence.bin")); }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static Cbi62Observation Cbi62Expected(JsonElement vector) => new(
        vector.GetProperty("code").GetString()!,
        vector.GetProperty("phase").GetString()!,
        Cbi60Join(vector.GetProperty("committed").EnumerateArray().Select(value => value.GetString())),
        vector.GetProperty("nextCycleIndex").GetInt32(),
        vector.GetProperty("gaps").GetInt32(),
        vector.GetProperty("interruptions").GetInt32(),
        vector.GetProperty("retries").GetInt32());

    [Test]
    public void Shared_cbi62_vectors_resume_a_governed_cadence()
    {
        using var fixture = Cbi62Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
            Assert.That(Cbi62RunVector(fixture.RootElement, vector), Is.EqualTo(Cbi62Expected(vector)),
                $"vector {vector.GetProperty("name").GetString()}");
    }

    [Test]
    public void Cbi62_C1_every_code_in_the_vocabulary_is_committable_and_classified()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi62-vocabulary-{Guid.NewGuid():N}");
        try
        {
            // The guard is over the class rather than today's six: a code added to the vocabulary and
            // left out of the journal fails here, which is what CBI61's two additions did not do.
            Assert.That(ProviderServingTrustCycleCodes.All, Is.Not.Empty);
            foreach (var code in ProviderServingTrustCycleCodes.All)
            {
                var journal = DurableProviderTrustCadenceJournal.Establish(
                    Path.Combine(root, $"{code}.bin"), Cbi62Run,
                    ProviderServingTrustCadenceSchedule.Create(4, TimeSpan.FromSeconds(5)),
                    Cbi62Start).Journal!;
                Assert.That(journal.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));
                var committed = journal.CommitCycle(code);
                Assert.Multiple(() =>
                {
                    Assert.That(committed.Code, Is.Not.EqualTo("durable-cadence-result-invalid"), code);
                    Assert.That(journal.Snapshot.Phase, Is.Not.EqualTo("in-flight"), code);
                    Assert.That(journal.Snapshot.Cycles.Single().Code, Is.EqualTo(code), code);
                });
            }
            // A code outside the vocabulary is still refused, so the guard did not become permissive.
            var stray = DurableProviderTrustCadenceJournal.Establish(
                Path.Combine(root, "stray.bin"), Cbi62Run,
                ProviderServingTrustCadenceSchedule.Create(4, TimeSpan.FromSeconds(5)),
                Cbi62Start).Journal!;
            Assert.That(stray.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));
            Assert.That(stray.CommitCycle("provider-trust-cycle-invented").Code,
                Is.EqualTo("durable-cadence-result-invalid"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi62_C2_the_run_outcome_never_renames_the_cycle_outcome()
    {
        using var fixture = Cbi62Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = Cbi62RunVector(fixture.RootElement, vector);
            var expected = Cbi60Join(
                vector.GetProperty("committed").EnumerateArray().Select(value => value.GetString()));
            Assert.That(actual.Committed, Is.EqualTo(expected),
                vector.GetProperty("name").GetString());
        }
    }

    /// <summary>
    /// One rotation endpoint and one policy endpoint over a real registry. By default each answers
    /// relative to the cursor it is given, which is what an honest endpoint does after a host's own
    /// state has moved. In <see cref="Replay"/> mode both re-offer the identical statement and update
    /// whatever the cursor says, which is the stale or hostile endpoint a retry must also survive.
    /// </summary>
    private sealed class Cbi62Endpoints(
        ECDsa pin,
        ECDsa successor,
        ECDsa endpoint,
        ECDsa otherEndpoint,
        DateTimeOffset now,
        bool rotationReachable)
        : IProviderPolicyAuthorityRotationDistributionSource, IProviderPublisherTrustPolicyDistributionSource
    {
        private ProviderPolicyAuthorityRotationStatement? statement;
        private ProviderPublisherTrustPolicyUpdate? update;

        public bool Replay { get; set; }

        public Task<ProviderPolicyAuthorityRotationDistributionResponse> FetchAsync(
            ProviderPolicyAuthorityRotationDistributionRequest request, CancellationToken cancellationToken)
        {
            if (!rotationReachable) throw new IOException("unavailable");
            statement ??= Cbi57Statement(1, 0, null, pin, successor, other: otherEndpoint);
            var offers = Replay || request.AuthorityGeneration == 0;
            return Task.FromResult(Cbi58Respond(
                offers ? "rotate" : "current", request, endpoint, statement, now));
        }

        public Task<ProviderPublisherTrustPolicyDistributionResponse> FetchAsync(
            ProviderPublisherTrustPolicyDistributionRequest request, CancellationToken cancellationToken)
        {
            update ??= Cbi37Sign(successor, 1, null, Cbi41Policy(1));
            var offered = Replay || request.CurrentSequence == 0 ? update : null;
            var expires = now.AddMinutes(1);
            var signature = endpoint.SignData(
                ProviderPublisherTrustPolicyDistributionManifest.Encode(
                    request.Challenge, request.CurrentSequence, request.CurrentPolicyIdentity,
                    now.ToUnixTimeSeconds(), expires.ToUnixTimeSeconds(), offered),
                HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
            return Task.FromResult(new ProviderPublisherTrustPolicyDistributionResponse(
                request.Challenge, request.CurrentSequence, request.CurrentPolicyIdentity,
                now.ToUnixTimeSeconds(), expires.ToUnixTimeSeconds(), offered, "ECDSA-P256-SHA256",
                Convert.ToBase64String(endpoint.ExportSubjectPublicKeyInfo()),
                Convert.ToBase64String(signature)));
        }
    }

    private sealed record Cbi62Governed(
        IProviderServingTrustCycle Cycle,
        DurableProviderPublisherTrustPolicyRegistry Registry,
        Cbi62Endpoints Endpoints);

    private static Cbi62Governed Cbi62Compose(
        string root, ECDsa pin, ECDsa successor, ECDsa endpoint, ECDsa otherEndpoint, bool rotationReachable)
    {
        var identity = Cbi57Authority(pin);
        var durable = DurableProviderPublisherTrustPolicyRegistry.Open(
            Path.Combine(root, "policy.checkpoint"), identity).Registry!;
        var policyFloors = DurableProviderPublisherTrustPolicyFloorStore
            .Open(Path.Combine(root, "policy.floor"), identity).Store!;
        var authorityFloors = DurableProviderPolicyAuthorityFloorStore
            .Open(Path.Combine(root, "authority.floor"), identity).Store!;
        var endpointId = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
            Convert.ToHexString(SHA256.HashData(endpoint.ExportSubjectPublicKeyInfo())));
        var endpoints = new Cbi62Endpoints(
            pin, successor, endpoint, otherEndpoint, Cbi62Start, rotationReachable);
        var instant = new Cbi61InstantDelay();

        var rotation = new ProviderPolicyAuthorityRotationCycleBinding(
            new ProviderPolicyAuthorityRotationCycle(durable, endpointId,
                ProviderPolicyAuthorityCycleSchedule.Create(
                    2, TimeSpan.FromMilliseconds(1), 2, TimeSpan.FromMilliseconds(2), TimeSpan.FromSeconds(1))),
            endpoints, authorityFloors, instant);
        var policy = new ProviderPublisherTrustPolicyCycle(
            new ProviderPublisherTrustPolicyPoller(durable, endpointId,
                ProviderPublisherTrustPolicyPollSchedule.Create(
                    2, TimeSpan.FromMilliseconds(1), 2, TimeSpan.FromMilliseconds(2), TimeSpan.FromSeconds(1))),
            endpoints, policyFloors, instant);

        return new(new ProviderGovernedTrustCycle(
            rotation, new ProviderServingTrustCycle(policy, new Cbi61EmptySweep())), durable, endpoints);
    }

    [Test]
    public async Task Cbi62_C3_the_journal_says_nothing_about_which_loop_ran()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi62-loops-{Guid.NewGuid():N}");
        try
        {
            using var pin = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var successor = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var endpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var otherEndpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            var images = new List<byte[]>();
            var generations = new List<long>();
            // Two runs identical in every journal-visible respect and differing only in whether the
            // rotation reached its endpoint. A journal that recorded which loop ran would differ.
            foreach (var rotationReachable in new[] { true, false })
            {
                var directory = Path.Combine(root, rotationReachable ? "reached" : "unreached");
                var journalPath = Path.Combine(directory, "cadence.bin");
                var journal = DurableProviderTrustCadenceJournal.Establish(
                    journalPath, Cbi62Run,
                    ProviderServingTrustCadenceSchedule.Create(3, TimeSpan.FromSeconds(5)),
                    Cbi62Start).Journal!;
                var governed = Cbi62Compose(directory, pin, successor, endpoint, otherEndpoint, rotationReachable);
                Assert.That(journal.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));
                // The cycle runs and the process then dies before any commit.
                await governed.Cycle.RunAsync(Cbi62Start, CancellationToken.None);
                images.Add(File.ReadAllBytes(journalPath));
                generations.Add(governed.Registry.AuthorityGeneration);
            }

            Assert.Multiple(() =>
            {
                Assert.That(images[0], Is.EqualTo(images[1]),
                    "the journal must hold no field that distinguishes the two runs");
                // The difference is real and is recorded where it can be trusted: the retained chain.
                Assert.That(generations[0], Is.EqualTo(1));
                Assert.That(generations[1], Is.Zero);
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task Cbi62_C4_retrying_an_interrupted_governed_cycle_replays_neither_half()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi62-retry-{Guid.NewGuid():N}");
        try
        {
            using var pin = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var successor = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var endpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var otherEndpoint = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var journalPath = Path.Combine(root, "cadence.bin");
            var journal = DurableProviderTrustCadenceJournal.Establish(
                journalPath, Cbi62Run,
                ProviderServingTrustCadenceSchedule.Create(3, TimeSpan.FromSeconds(5)),
                Cbi62Start).Journal!;
            var governed = Cbi62Compose(root, pin, successor, endpoint, otherEndpoint, rotationReachable: true);

            Assert.That(journal.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));
            var first = await governed.Cycle.RunAsync(Cbi62Start, CancellationToken.None);
            var appliedGeneration = governed.Registry.AuthorityGeneration;
            var appliedSequence = governed.Registry.Current?.Sequence ?? 0;

            // The process dies after both halves took effect and before the commit.
            var reopened = DurableProviderTrustCadenceJournal.Open(journalPath, Cbi62Run);
            Assert.That(reopened.Code, Is.EqualTo("durable-cadence-indeterminate"));
            Assert.That(reopened.Journal!.ResolveInterrupted(ProviderTrustCadenceRecoveryDecision.Retry).Code,
                Is.EqualTo("durable-cadence-retry-ready"));
            Assert.That(reopened.Journal.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));

            // The honest path: the host's own cursor moved, so both endpoints answer that it is
            // current and the retry has nothing to re-apply.
            var retried = await governed.Cycle.RunAsync(Cbi62Start, CancellationToken.None);

            // The defensive path: a stale endpoint re-offers the identical statement and update, and
            // both are refused by the ordinary generation and sequence rules.
            governed.Endpoints.Replay = true;
            var replayed = await governed.Cycle.RunAsync(Cbi62Start, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(first.Code, Is.EqualTo(ProviderServingTrustCycleCodes.Current));
                Assert.That(appliedGeneration, Is.EqualTo(1));
                Assert.That(appliedSequence, Is.EqualTo(1));
                Assert.That(retried.Code, Is.EqualTo(ProviderServingTrustCycleCodes.Current));
                Assert.That(retried.Rotation!.IsCurrent, Is.True);
                // Neither half double-applies on either path, and neither needed to know about the
                // interruption to refuse.
                Assert.That(replayed.Rotation!.LastAttemptCode,
                    Is.EqualTo("policy-authority-generation-invalid"));
                Assert.That(replayed.Poll!.LastAttemptCode, Is.EqualTo("policy-update-sequence-invalid"));
                Assert.That(governed.Registry.AuthorityGeneration, Is.EqualTo(appliedGeneration));
                Assert.That(governed.Registry.Current?.Sequence ?? 0, Is.EqualTo(appliedSequence));
                Assert.That(reopened.Journal.Snapshot.RetryCount, Is.EqualTo(1));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi62_C5_an_ungoverned_cadence_reaches_the_same_terminal_states()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi62-ungoverned-{Guid.NewGuid():N}");
        try
        {
            // The four codes CBI48 always knew keep their exact terminal mapping.
            foreach (var (code, terminal) in new[]
                     {
                         (ProviderServingTrustCycleCodes.Current, "durable-cadence-cycle-committed"),
                         (ProviderServingTrustCycleCodes.Withdrawn, "durable-cadence-cycle-committed"),
                         (ProviderServingTrustCycleCodes.Stopped, "durable-cadence-stopped"),
                         (ProviderServingTrustCycleCodes.Canceled, "durable-cadence-canceled"),
                     })
            {
                var journal = DurableProviderTrustCadenceJournal.Establish(
                    Path.Combine(root, $"{code}.bin"), Cbi62Run,
                    ProviderServingTrustCadenceSchedule.Create(4, TimeSpan.FromSeconds(5)),
                    Cbi62Start).Journal!;
                Assert.That(journal.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));
                Assert.Multiple(() =>
                {
                    Assert.That(journal.CommitCycle(code).Code, Is.EqualTo(terminal), code);
                    Assert.That(journal.Snapshot.Cycles.Single().Code, Is.EqualTo(code), code);
                });
            }
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
