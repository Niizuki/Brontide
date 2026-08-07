using System.Security.Cryptography;
using System.Text.Json;
using Brontide.Reference.Experimental.ComponentManagement;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    /// <summary>
    /// Returns the scripted poll outcome for the cycle the cadence has reached. The poller itself is
    /// pinned by CBI41's own vectors; what CBI64 owns is which outcomes reach an availability
    /// decision, so a vector states the outcome rather than the endpoint behaviour producing it.
    /// </summary>
    private sealed class Cbi64PolicyCycle(
        IReadOnlyList<(string Code, string? LastAttemptCode)> cycles,
        ProviderPublisherTrustPolicyRecoveryFloor floor) : IProviderPublisherTrustPolicyCycle
    {
        public int Cycle { get; set; }

        public Task<ProviderPublisherTrustPolicyPollResult> PollAsync(
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            var (code, lastAttemptCode) = cycles[Math.Min(Cycle, cycles.Count - 1)];
            return Task.FromResult(new ProviderPublisherTrustPolicyPollResult(
                code, lastAttemptCode, 1, [], [], [], null, floor));
        }
    }

    private sealed class Cbi64Sweep : IProviderServingTrustSweepCycle
    {
        // CBI47 C4 makes an empty serving set a successful no-op, which keeps this slice's evidence on
        // the availability path rather than on CBI46's members. The serving set the availability seam
        // snapshots is the one that matters here, and it is supplied separately.
        public ValueTask<ProviderServingTrustSweepResult?> SweepAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<ProviderServingTrustSweepResult?>(null);
    }

    private sealed class Cbi64CadenceDelay(Cbi64PolicyCycle policy) : IProviderServingTrustCadenceDelay
    {
        public Task<DateTimeOffset> DelayAsync(
            DateTimeOffset now, TimeSpan duration, CancellationToken cancellationToken)
        {
            policy.Cycle++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(now + duration);
        }
    }

    private sealed record Cbi64Observation(
        string CadenceCode,
        string CycleCodes,
        string DecisionCodes,
        string EnforcementCodes,
        string Stopped,
        bool DeadlineMoved,
        int FinalServing);

    private static JsonDocument Cbi64Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi64-cadence-availability-vectors.json")));

    private static JsonElement Cbi64Vector(JsonDocument fixture, string name) =>
        fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(vector => vector.GetProperty("name").GetString() == name);

    /// <summary>
    /// Runs one scripted cadence over two real serving activations in their own provider processes.
    /// The availability seam is the production binding, so CBI49's decision and CBI50's effects are
    /// the real ones rather than a harness restating them.
    /// </summary>
    private static async Task<Cbi64Observation> Cbi64RunAsync(JsonElement fixture, JsonElement vector)
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi64-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var providers = new List<StagedProviderProcess>();
        var activations = new List<ProviderServingActivation>();
        try
        {
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var firstPublisher = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var secondPublisher = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var authorityId = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo())));
            var (request, source) = Cbi33Input("reference", "none");
            var evidence = new[]
            {
                Cbi43Evidence(firstPublisher, request),
                Cbi43Evidence(secondPublisher, request),
            };
            var initial = Cbi44Policy(evidence
                .Select(item => Cbi44Entry(item.PublisherKeyId, revoked: false)).ToArray());
            var custody = ProviderPublisherTrustPolicyCustody.Open(
                Path.Combine(root, "policy.checkpoint"), Path.Combine(root, "policy.floor"), authorityId);
            var registry = custody.Registry!;
            Assert.That(registry.Apply(Cbi37Sign(authority, 1, null, initial)).IsApplied, Is.True);
            var store = new ContentAddressedProviderStore(Path.Combine(root, "store"));
            // A vector's serving count is the host's real one: an empty set launches no provider at
            // all, so an idle decision is not being read off a set that is quietly still serving.
            var servingCount = vector.GetProperty("servingCount").GetInt32();
            var chains = evidence.Take(servingCount).Select((item, index) => ProviderDistributionChain.Run(
                registry, store, Path.Combine(root, $"transactions-{index}"),
                new(request, item, ["--portable"]), source)).ToArray();
            providers.AddRange(chains.Select(chain => chain.Provider!));

            if (servingCount > 0)
            {
                var pair = RequestFor(Requirement, SecondaryRequirement);
                var consumer = pair.Definitions.Single(item => item.Definition == Consumer);
                var resolution = new FakeGenerationResolver().Resolve(pair with
                {
                    Definitions = pair.Definitions.Select(item => item.Definition switch
                    {
                        var definition when definition == Consumer => item with
                        {
                            Requirements = consumer.Requirements
                                .Select(requirement => requirement with { Contract = Contract }).ToArray(),
                        },
                        var definition when definition == SecondaryProvider => item with
                        {
                            Provides = [new ProvidedContract(Contract, Version)],
                        },
                        _ => item,
                    }).ToArray(),
                    Candidates = pair.Candidates.Select(item => item.Definition == SecondaryProvider
                        ? item with { Provides = [new ProvidedContract(Contract, Version)] }
                        : item).ToArray(),
                });
                var selections = new[] { Requirement, SecondaryRequirement }
                    .Select(requirement => Selection(resolution.Generation!.ProviderSets
                        .Single(set => set.Requirement == requirement).Members.Single()) with
                    {
                        Requirement = requirement,
                    }).ToArray();
                for (var index = 0; index < chains.Length; index++)
                {
                    activations.Add(await ProviderServingTrustRevalidation.ActivateAsync(
                        chains[index], resolution, selections[index],
                        RuntimeRequest(Plan(selections[index].Occurrence))));
                    Assert.That(activations[index].IsServing, Is.True);
                }
            }

            var schedule = fixture.GetProperty("schedule");
            var policy = ProviderTrustOfflinePolicy.Create(
                TimeSpan.FromSeconds(schedule.GetProperty("graceSeconds").GetInt32()),
                TimeSpan.FromSeconds(schedule.GetProperty("retrySeconds").GetInt32()));
            var duplicate = vector.TryGetProperty("duplicateSnapshot", out var flag) && flag.GetBoolean();
            IReadOnlyList<ProviderServingActivation> snapshot = duplicate
                ? [activations[0], activations[0]]
                : activations;

            var polls = fixture.GetProperty("polls");
            var script = vector.GetProperty("cycles").EnumerateArray().Select(cycle =>
            {
                var poll = polls.GetProperty(cycle.GetString()!);
                var attempt = poll.GetProperty("lastAttemptCode");
                return (poll.GetProperty("code").GetString()!,
                    attempt.ValueKind == JsonValueKind.Null ? null : attempt.GetString());
            }).ToArray();

            var policyCycle = new Cbi64PolicyCycle(script, registry.Floor);
            var cadence = new ProviderServingTrustCadence(ProviderServingTrustCadenceSchedule.Create(
                script.Length,
                TimeSpan.FromSeconds(schedule.GetProperty("intervalSeconds").GetInt32())));
            var cycle = new ProviderAvailabilityTrustCycle(
                new ProviderServingTrustCycle(policyCycle, new Cbi64Sweep()),
                new ProviderOfflineEnforcementCycle(
                    policy, _ => ValueTask.FromResult(snapshot), "offline availability withdrawn"));

            var start = new DateTimeOffset(2026, 8, 7, 9, 0, 0, TimeSpan.Zero);
            var result = await cadence.RunAsync(cycle, new Cbi64CadenceDelay(policyCycle), start);

            var availability = result.Cycles.Select(item => item.Result.Availability).ToArray();
            return new(
                result.Code,
                Cbi60Join(result.Cycles.Select(item => item.Result.Code)),
                Cbi60Join(availability.Select(item => item?.DecisionCode ?? "none")),
                Cbi60Join(availability.Select(item => item?.EnforcementCode ?? "none")),
                Cbi60Join(availability.Select(item => item?.StoppedCount ?? 0)),
                availability.Where(item => item is not null).Select(item => item!.Deadline)
                    .Distinct().Count() > 1,
                activations.Count(activation => activation.IsServing));
        }
        finally
        {
            foreach (var activation in activations)
            {
                if (activation.IsServing) await activation.RetireAsync("CBI64 test completed");
                await activation.DisposeAsync();
            }
            foreach (var provider in providers)
            {
                if (!provider.HasExited) await provider.DisposeAsync();
            }
            Cbi32DeleteTree(root);
        }
    }

    private static Cbi64Observation Cbi64Expected(JsonElement vector) => new(
        vector.GetProperty("cadenceCode").GetString()!,
        Cbi60Join(vector.GetProperty("cycleCodes").EnumerateArray().Select(value => value.GetString())),
        Cbi60Join(vector.GetProperty("decisionCodes").EnumerateArray()
            .Select(value => value.ValueKind == JsonValueKind.Null ? "none" : value.GetString())),
        Cbi60Join(vector.GetProperty("enforcementCodes").EnumerateArray()
            .Select(value => value.ValueKind == JsonValueKind.Null ? "none" : value.GetString())),
        Cbi60Join(vector.GetProperty("stopped").EnumerateArray().Select(value => value.GetInt32())),
        vector.GetProperty("deadlineMoved").GetBoolean(),
        vector.GetProperty("finalServing").GetInt32());

    [Test, Category("CrossProcess")]
    public async Task Shared_cbi64_vectors_enforce_availability_across_a_cadence()
    {
        using var fixture = Cbi64Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi64RunAsync(fixture.RootElement, vector);
            Assert.That(actual, Is.EqualTo(Cbi64Expected(vector)),
                $"vector {vector.GetProperty("name").GetString()}");
        }
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi64_C1_an_outage_with_no_earlier_current_cycle_has_no_baseline()
    {
        using var fixture = Cbi64Fixture();
        var actual = await Cbi64RunAsync(fixture.RootElement,
            Cbi64Vector(fixture, "an-outage-with-no-baseline-stops-service"));
        Assert.Multiple(() =>
        {
            // CBI49 refuses to invent a baseline, so a grace-eligible outage still stops service.
            Assert.That(actual.DecisionCodes, Is.EqualTo("offline-service-stop-required"));
            Assert.That(actual.Stopped, Is.EqualTo("2"));
            Assert.That(actual.FinalServing, Is.Zero);
        });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi64_C2_a_running_cadence_cannot_extend_its_own_deadline()
    {
        using var fixture = Cbi64Fixture();
        var expiring = await Cbi64RunAsync(fixture.RootElement,
            Cbi64Vector(fixture, "grace-expires-while-the-cadence-runs"));
        var recovering = await Cbi64RunAsync(fixture.RootElement,
            Cbi64Vector(fixture, "recovery-inside-grace-rearms-the-deadline"));
        Assert.Multiple(() =>
        {
            // Four cycles of outage against one baseline, and the deadline never moves. A cadence
            // that took each cycle's own instant would report existing service forever.
            Assert.That(expiring.DeadlineMoved, Is.False);
            Assert.That(expiring.CycleCodes, Does.EndWith("provider-trust-cycle-stopped"));
            Assert.That(expiring.DecisionCodes, Does.EndWith("offline-grace-expired"));
            // Only a cycle that establishes current policy moves it.
            Assert.That(recovering.DeadlineMoved, Is.True);
            Assert.That(recovering.CadenceCode, Is.EqualTo("provider-trust-cadence-complete"));
        });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi64_C3_availability_is_decided_only_where_a_poll_decided_nothing()
    {
        using var fixture = Cbi64Fixture();
        var actual = await Cbi64RunAsync(fixture.RootElement,
            Cbi64Vector(fixture, "cancellation-enforces-nothing"));
        Assert.Multiple(() =>
        {
            Assert.That(actual.CadenceCode, Is.EqualTo("provider-trust-cadence-canceled"));
            Assert.That(actual.EnforcementCodes, Is.EqualTo("none,none"));
            Assert.That(actual.FinalServing, Is.EqualTo(2));
        });
    }

    /// <summary>
    /// A governed cycle whose rotation stopped it carries no poll, so there is nothing for CBI49 to
    /// evaluate. Composed rather than taken from the fixture, because the shared vectors script the
    /// policy endpoint and this case never reaches it.
    /// </summary>
    [Test]
    public async Task Cbi64_C3_a_cycle_that_reached_no_endpoint_enforces_nothing()
    {
        var enforced = 0;
        var cycle = new ProviderAvailabilityTrustCycle(
            new Cbi64FixedCycle(new(
                ProviderServingTrustCycleCodes.AuthorityUnretained, null, null, 0)),
            new Cbi64CountingEnforcement(() => enforced++));
        var result = await cycle.RunAsync(DateTimeOffset.UnixEpoch, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(ProviderServingTrustCycleCodes.AuthorityUnretained));
            Assert.That(result.Availability, Is.Null);
            Assert.That(enforced, Is.Zero);
        });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi64_C4_every_ineligible_outcome_stops_service()
    {
        using var fixture = Cbi64Fixture();
        foreach (var name in new[]
                 {
                     "an-exhausted-stale-window-is-not-availability",
                     "a-registry-refusal-is-not-availability",
                     "an-unretained-policy-floor-is-not-availability",
                 })
        {
            var actual = await Cbi64RunAsync(fixture.RootElement, Cbi64Vector(fixture, name));
            Assert.Multiple(() =>
            {
                Assert.That(actual.DecisionCodes, Does.EndWith("offline-service-stop-required"), name);
                Assert.That(actual.CycleCodes, Does.EndWith("provider-trust-cycle-stopped"), name);
                Assert.That(actual.FinalServing, Is.Zero, name);
            });
        }
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi64_C4_every_non_current_cycle_with_a_poll_carries_one_enforcement()
    {
        using var fixture = Cbi64Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var name = vector.GetProperty("name").GetString();
            var actual = await Cbi64RunAsync(fixture.RootElement, vector);
            var codes = actual.CycleCodes.Split(',');
            var enforcements = actual.EnforcementCodes.Split(',');
            Assert.Multiple(() =>
            {
                for (var index = 0; index < codes.Length; index++)
                {
                    var expected = codes[index] is ProviderServingTrustCycleCodes.Current
                        or ProviderServingTrustCycleCodes.Withdrawn
                        or ProviderServingTrustCycleCodes.Canceled ? "none" : "some";
                    Assert.That(enforcements[index] == "none" ? "none" : "some",
                        Is.EqualTo(expected), $"{name} cycle {index}");
                }
            });
        }
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi64_C5_availability_never_relabels_a_cycle_it_did_not_preserve()
    {
        using var fixture = Cbi64Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var name = vector.GetProperty("name").GetString();
            var actual = await Cbi64RunAsync(fixture.RootElement, vector);
            var codes = actual.CycleCodes.Split(',');
            var decisions = actual.DecisionCodes.Split(',');
            Assert.Multiple(() =>
            {
                for (var index = 0; index < codes.Length; index++)
                {
                    if (codes[index] != ProviderServingTrustCycleCodes.Offline) continue;
                    Assert.That(decisions[index], Is.EqualTo("offline-existing-service")
                        .Or.EqualTo("offline-idle"), $"{name} cycle {index}");
                }
            });
        }
    }

    /// <summary>
    /// CBI61 attributes an unverifiable update to an incomplete rotation. That refusal is never
    /// grace-eligible, so an availability stop in the same cycle must leave the attribution intact —
    /// which is why the availability wrapper is outermost rather than inside the governed one.
    /// </summary>
    [Test]
    public async Task Cbi64_C5_an_availability_stop_preserves_the_governed_attribution()
    {
        var poll = new ProviderPublisherTrustPolicyPollResult(
            "policy-poll-refused", "policy-update-authority-mismatch", 1, [], [], [], null, null!);
        var cycle = new ProviderAvailabilityTrustCycle(
            new Cbi64FixedCycle(new(
                ProviderServingTrustCycleCodes.AuthorityBehind, poll, null, 0)),
            new Cbi64FixedEnforcement(new(
                "offline-enforcement-stopped", "offline-service-stop-required", null, null, 2, 2)));
        var result = await cycle.RunAsync(DateTimeOffset.UnixEpoch, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(ProviderServingTrustCycleCodes.AuthorityBehind));
            Assert.That(result.Availability!.StoppedCount, Is.EqualTo(2));
        });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi64_C6_continuation_stops_nobody_and_expiry_keeps_the_staged_set()
    {
        using var fixture = Cbi64Fixture();
        var continuing = await Cbi64RunAsync(fixture.RootElement,
            Cbi64Vector(fixture, "outage-inside-grace-preserves-service"));
        var refused = await Cbi64RunAsync(fixture.RootElement,
            Cbi64Vector(fixture, "a-refused-snapshot-stops-nothing"));
        Assert.Multiple(() =>
        {
            Assert.That(continuing.Stopped.Split(',').All(value => value == "0"), Is.True);
            Assert.That(continuing.FinalServing, Is.EqualTo(2));
            // CBI50 C7: a snapshot it refuses is evaluated against no policy and stops nobody.
            Assert.That(refused.DecisionCodes, Is.EqualTo("none,none"));
            Assert.That(refused.FinalServing, Is.EqualTo(2));
        });
    }

    [Test]
    public void Cbi64_C7_the_offline_code_is_one_the_journal_knows_and_continues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ProviderServingTrustCycleCodes.IsKnown(
                ProviderServingTrustCycleCodes.Offline), Is.True);
            Assert.That(ProviderServingTrustCycleCodes.Continues(
                ProviderServingTrustCycleCodes.Offline), Is.True);
        });
    }

    [Test]
    public void Cbi64_C7_a_cadence_that_continued_through_an_outage_is_journalled()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi64-journal-{Guid.NewGuid():N}");
        try
        {
            var journal = DurableProviderTrustCadenceJournal.Establish(
                Path.Combine(root, "cadence.bin"),
                ProviderTrustCadenceRunId.Create("cbi64-offline-run"),
                ProviderServingTrustCadenceSchedule.Create(2, TimeSpan.FromSeconds(60)),
                new DateTimeOffset(2026, 8, 7, 9, 0, 0, TimeSpan.Zero)).Journal!;
            Assert.That(journal.BeginCycle().Code, Is.EqualTo("durable-cadence-cycle-started"));
            var committed = journal.CommitCycle(ProviderServingTrustCycleCodes.Offline);
            Assert.Multiple(() =>
            {
                Assert.That(committed.Code, Is.EqualTo("durable-cadence-cycle-committed"));
                Assert.That(committed.Snapshot.Phase, Is.EqualTo("waiting"));
            });
        }
        finally
        {
            Cbi32DeleteTree(root);
        }
    }

    private sealed class Cbi64FixedCycle(ProviderServingTrustCycleResult result)
        : IProviderServingTrustCycle
    {
        public Task<ProviderServingTrustCycleResult> RunAsync(
            DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private sealed class Cbi64FixedEnforcement(ProviderTrustCycleAvailability availability)
        : IProviderOfflineEnforcementCycle
    {
        public ValueTask<ProviderTrustCycleAvailability> EnforceAsync(
            DateTimeOffset now, DateTimeOffset? lastCurrent, string pollCode, string? lastAttemptCode,
            CancellationToken cancellationToken) => ValueTask.FromResult(availability);
    }

    private sealed class Cbi64CountingEnforcement(Action observed) : IProviderOfflineEnforcementCycle
    {
        public ValueTask<ProviderTrustCycleAvailability> EnforceAsync(
            DateTimeOffset now, DateTimeOffset? lastCurrent, string pollCode, string? lastAttemptCode,
            CancellationToken cancellationToken)
        {
            observed();
            return ValueTask.FromResult(new ProviderTrustCycleAvailability(
                "offline-enforcement-stopped", "offline-service-stop-required", null, null, 0, 0));
        }
    }
}
