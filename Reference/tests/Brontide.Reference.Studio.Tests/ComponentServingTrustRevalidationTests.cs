using System.Security.Cryptography;
using System.Text.Json;
using Brontide.Reference.Experimental.ComponentManagement;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed record Cbi46Observation(
        string Code,
        string RefusedBy,
        string[] Order,
        string[] MemberCodes,
        int Continued,
        int Withdrawn,
        bool FirstServing,
        bool SecondServing,
        bool StagedSetRemains);

    private static async Task<Cbi46Observation> Cbi46RunAsync(string scenario)
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi46-{Guid.NewGuid():N}");
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
            Assert.That(custody.IsOpened, Is.True);
            var registry = custody.Registry!;
            Assert.That(registry.Apply(Cbi37Sign(authority, 1, null, initial)).IsApplied, Is.True);
            var storeRoot = Path.Combine(root, "store");
            var store = new ContentAddressedProviderStore(storeRoot);
            var chains = evidence.Select((item, index) => ProviderDistributionChain.Run(
                registry, store, Path.Combine(root, $"transactions-{index}"),
                new(request, item, ["--portable"]), source)).ToArray();
            providers.AddRange(chains.Select(chain => chain.Provider!));

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
                var occurrence = selections[index].Occurrence;
                activations.Add(await ProviderServingTrustRevalidation.ActivateAsync(
                    chains[index], resolution, selections[index], RuntimeRequest(Plan(occurrence))));
                Assert.That(activations[index].IsServing, Is.True);
            }

            if (scenario is "first-withdrawn-second-current" or "all-withdrawn")
            {
                var successor = Cbi44Policy(
                    Cbi44Entry(evidence[0].PublisherKeyId, revoked: true),
                    Cbi44Entry(evidence[1].PublisherKeyId, revoked: scenario == "all-withdrawn"));
                Assert.That(registry.Apply(Cbi37Sign(authority, 2, initial.Identity, successor)).IsApplied, Is.True);
            }

            IReadOnlyList<ProviderServingActivation> input = scenario switch
            {
                "reverse-all-current" or "offline-expired" or "offline-integrity-refusal" =>
                    [activations[1], activations[0]],
                "duplicate-occurrence" or "offline-duplicate" => [activations[0], activations[0]],
                _ => activations,
            };
            if (scenario is "unavailable-member" or "offline-unavailable")
            {
                await activations[1].RetireAsync("make unavailable before sweep");
            }

            if (scenario.StartsWith("offline-", StringComparison.Ordinal))
            {
                var baseline = new DateTimeOffset(2026, 8, 5, 8, 0, 0, TimeSpan.Zero);
                var now = scenario == "offline-expired" ? baseline.AddMinutes(5) : baseline.AddMinutes(2);
                var pollCode = scenario == "offline-integrity-refusal"
                    ? "policy-poll-refused" : "policy-poll-exhausted";
                var offline = await ProviderOfflineServiceEnforcement.RunAsync(
                    ProviderTrustOfflinePolicy.Create(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1)),
                    now, baseline, pollCode, "policy-distribution-timeout", input, "offline grace ended");
                return new(
                    offline.Code, offline.RefusedBy,
                    offline.Members.Select(member => member.Occurrence.Value).ToArray(),
                    offline.Members.Select(member => member.RetirementCode).ToArray(),
                    offline.AdmittedCount - offline.StoppedCount, offline.StoppedCount,
                    activations[0].IsServing, activations[1].IsServing,
                    Directory.Exists(storeRoot) && Directory.EnumerateDirectories(storeRoot).Any());
            }

            var result = await ProviderServingTrustSweep.RunAsync(
                registry, store, input, "publisher trust lapsed");
            return new(
                result.Code,
                result.RefusedBy,
                result.Members.Select(member => member.Occurrence.Value).ToArray(),
                result.Members.Select(member => member.Result.Code).ToArray(),
                result.ContinuedCount,
                result.WithdrawnCount,
                activations[0].IsServing,
                activations[1].IsServing,
                Directory.Exists(storeRoot) && Directory.EnumerateDirectories(storeRoot).Any());
        }
        finally
        {
            foreach (var activation in activations)
            {
                if (activation.IsServing) await activation.RetireAsync("CBI46 test completed");
                await activation.DisposeAsync();
            }
            foreach (var provider in providers)
            {
                if (!provider.HasExited) await provider.DisposeAsync();
            }
            Cbi32DeleteTree(root);
        }
    }

    [Test]
    public async Task Cbi46_C1_the_serving_set_is_bounded_and_valid_before_effects()
    {
        var result = await ProviderServingTrustSweep.RunAsync(
            null!, null!, Array.Empty<ProviderServingActivation>(), "publisher trust lapsed");

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("serving-trust-sweep-invalid"));
            Assert.That(result.RefusedBy, Is.EqualTo("preflight"));
            Assert.That(result.Members, Is.Empty);
        });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi46_C2_typed_occurrence_identity_determines_order()
    {
        var actual = await Cbi46RunAsync("reverse-all-current");
        Assert.That(actual.Order, Is.EqualTo(new[]
        {
            "occ.def.test.cooling-provider.1",
            "occ.def.test.cooling-provider.2",
        }));
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi46_C3_every_admitted_member_receives_one_current_decision()
    {
        var actual = await Cbi46RunAsync("reverse-all-current");
        Assert.Multiple(() =>
        {
            Assert.That(actual.MemberCodes, Is.EqualTo(new[] { "publisher-trust-current", "publisher-trust-current" }));
            Assert.That(actual.Continued, Is.EqualTo(2));
        });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi46_C4_trust_withdrawal_reaches_every_affected_member()
    {
        var actual = await Cbi46RunAsync("all-withdrawn");
        Assert.Multiple(() =>
        {
            Assert.That(actual.Withdrawn, Is.EqualTo(2));
            Assert.That(actual.FirstServing, Is.False);
            Assert.That(actual.SecondServing, Is.False);
            Assert.That(actual.StagedSetRemains, Is.False);
        });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi46_C5_one_members_outcome_does_not_hide_its_sibling()
    {
        var actual = await Cbi46RunAsync("first-withdrawn-second-current");
        Assert.Multiple(() =>
        {
            Assert.That(actual.Code, Is.EqualTo("serving-trust-sweep-withdrawn"));
            Assert.That(actual.Continued, Is.EqualTo(1));
            Assert.That(actual.Withdrawn, Is.EqualTo(1));
            Assert.That(actual.StagedSetRemains, Is.True,
                "a staged set shared with a continuing sibling must remain available");
        });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi46_C6_preflight_refusal_has_zero_effect()
    {
        foreach (var scenario in new[] { "duplicate-occurrence", "unavailable-member" })
        {
            var actual = await Cbi46RunAsync(scenario);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo("serving-trust-sweep-invalid"), scenario);
                Assert.That(actual.Order, Is.Empty, scenario);
                Assert.That(actual.FirstServing, Is.True, scenario);
                Assert.That(actual.StagedSetRemains, Is.True, scenario);
            });
        }
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi46_C7_reference_executes_the_shared_sweep_vectors()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi46-serving-trust-sweep-vectors.json")));
        Assert.That(fixture.RootElement.GetProperty("maximumMembers").GetInt32(),
            Is.EqualTo(ProviderServingTrustSweep.MaximumMembers));
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var name = vector.GetProperty("name").GetString()!;
            var actual = await Cbi46RunAsync(name);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()), name);
                Assert.That(actual.Order, Is.EqualTo(vector.GetProperty("expectedOrder").EnumerateArray()
                    .Select(value => value.GetString()).ToArray()), name);
                Assert.That(actual.Continued, Is.EqualTo(vector.GetProperty("continued").GetInt32()), name);
                Assert.That(actual.Withdrawn, Is.EqualTo(vector.GetProperty("withdrawn").GetInt32()), name);
            });
        }
    }

    [Test]
    public async Task Cbi50_C1_one_snapshot_determines_decision_and_effects()
    {
        var policy = ProviderTrustOfflinePolicy.Create(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));
        var now = DateTimeOffset.UtcNow;
        var result = await ProviderOfflineServiceEnforcement.RunAsync(policy, now,
            now, "policy-poll-exhausted", "policy-distribution-timeout",
            Array.Empty<ProviderServingActivation>(), "offline grace ended");
        Assert.Multiple(() => { Assert.That(result.AdmittedCount, Is.Zero); Assert.That(result.Decision!.Code, Is.EqualTo("offline-idle")); });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi50_C2_permitted_continuation_is_effect_free()
    {
        var actual = await Cbi46RunAsync("offline-within-grace");
        Assert.Multiple(() => { Assert.That(actual.Code, Is.EqualTo("offline-enforcement-continuing")); Assert.That(actual.FirstServing, Is.True); Assert.That(actual.Order, Is.Empty); });
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi50_C3_every_stop_decision_reaches_every_member()
    {
        foreach (var scenario in new[] { "offline-expired", "offline-integrity-refusal" }) { var actual = await Cbi46RunAsync(scenario); Assert.Multiple(() => { Assert.That(actual.Withdrawn, Is.EqualTo(2), scenario); Assert.That(actual.FirstServing, Is.False, scenario); Assert.That(actual.SecondServing, Is.False, scenario); }); }
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi50_C4_typed_occurrence_identity_determines_order()
    {
        var actual = await Cbi46RunAsync("offline-expired");
        Assert.That(actual.Order, Is.EqualTo(new[] { "occ.def.test.cooling-provider.1", "occ.def.test.cooling-provider.2" }));
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi50_C5_one_outcome_does_not_hide_siblings()
    {
        var actual = await Cbi46RunAsync("offline-expired");
        Assert.That(actual.MemberCodes, Has.Length.EqualTo(2));
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi50_C6_availability_stop_retains_staged_artifacts()
    {
        var actual = await Cbi46RunAsync("offline-expired");
        Assert.That(actual.StagedSetRemains, Is.True);
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi50_C7_preflight_refusal_has_zero_effect()
    {
        foreach (var scenario in new[] { "offline-duplicate", "offline-unavailable" }) { var actual = await Cbi46RunAsync(scenario); Assert.Multiple(() => { Assert.That(actual.Code, Is.EqualTo("offline-enforcement-invalid"), scenario); Assert.That(actual.FirstServing, Is.True, scenario); Assert.That(actual.Order, Is.Empty, scenario); }); }
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi50_C8_reference_executes_the_shared_enforcement_model()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "component-management", "fixtures", "cbi50-offline-service-enforcement-vectors.json")));
        Assert.That(fixture.RootElement.GetProperty("maximumMembers").GetInt32(), Is.EqualTo(ProviderOfflineServiceEnforcement.MaximumMembers));
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray()) { var name = vector.GetProperty("name").GetString()!; var actual = await Cbi46RunAsync(name); Assert.Multiple(() => { Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()), name); Assert.That(actual.Order, Is.EqualTo(vector.GetProperty("expectedOrder").EnumerateArray().Select(value => value.GetString()).ToArray()), name); Assert.That(actual.Withdrawn, Is.EqualTo(vector.GetProperty("stopped").GetInt32()), name); Assert.That(actual.FirstServing, Is.EqualTo(vector.GetProperty("serving").GetBoolean()), name); Assert.That(actual.StagedSetRemains, Is.EqualTo(vector.GetProperty("stagedSetRemains").GetBoolean()), name); }); }
    }

    private sealed record Cbi45Observation(
        string Code,
        string? RefusedBy,
        bool Revalidated,
        bool Continued,
        bool PolicyChanged,
        bool MemberReleased,
        bool ProviderRunning,
        bool StagedSetRemains,
        bool ServingPolicyIsCurrent,
        bool DecisionMatchesStagedIdentity);

    private static ProviderPublisherTrustPolicy Cbi45Successor(
        string mutation,
        ProviderPublisherKeyId publisher) => mutation switch
        {
            "publisher-revoked" => Cbi44Policy(Cbi44Entry(publisher, revoked: true)),
            "publisher-removed" => Cbi44Policy(Cbi44Entry(Cbi44OtherPublisher, revoked: false)),
            "unrelated-revocation" => Cbi44Policy(
                Cbi44Entry(publisher, revoked: false),
                Cbi44Entry(Cbi44OtherPublisher, revoked: true)),
            _ => Cbi44Policy(
                Cbi44Entry(publisher, revoked: false),
                Cbi44Entry(Cbi44OtherPublisher, revoked: false)),
        };

    private static async Task<Cbi45Observation> Cbi45RunAsync(JsonElement vector, bool repeat = false)
    {
        var mutation = vector.GetProperty("mutation").GetString()!;
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi45-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        StagedProviderProcess? provider = null;
        ProviderServingActivation? activation = null;
        try
        {
            using var authority = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var publisher = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var authorityId = ProviderPublisherTrustPolicyAuthorityId.Create(
                Convert.ToHexString(SHA256.HashData(authority.ExportSubjectPublicKeyInfo())));
            var (request, source) = Cbi33Input("reference", "none");
            var evidence = Cbi43Evidence(publisher, request);
            var initial = Cbi44Policy(
                Cbi44Entry(evidence.PublisherKeyId, revoked: false),
                Cbi44Entry(Cbi44OtherPublisher, revoked: false));
            var custody = ProviderPublisherTrustPolicyCustody.Open(
                Path.Combine(root, "policy.checkpoint"), Path.Combine(root, "policy.floor"), authorityId);
            Assert.That(custody.IsOpened, Is.True);
            var registry = custody.Registry!;
            Assert.That(registry.Apply(Cbi37Sign(authority, 1, null, initial)).IsApplied, Is.True);

            var store = new ContentAddressedProviderStore(Path.Combine(root, "store"));
            var chain = ProviderDistributionChain.Run(
                registry, store, Path.Combine(root, "transactions"),
                new(request, evidence, ["--portable"]), source);
            provider = chain.Provider;
            Assert.That(provider, Is.Not.Null);

            var (resolution, selection, occurrence) = LifecycleInput();
            activation = await ProviderServingTrustRevalidation.ActivateAsync(
                chain, resolution, selection, RuntimeRequest(Plan(occurrence)));
            Assert.That(activation.IsServing, Is.True);

            if (mutation != "unchanged")
            {
                var successor = Cbi45Successor(mutation, evidence.PublisherKeyId);
                Assert.That(
                    registry.Apply(Cbi37Sign(authority, 2, initial.Identity, successor)).IsApplied,
                    Is.True);
            }

            var result = await ProviderServingTrustRevalidation.RevalidateAsync(
                registry, store, activation, "publisher trust lapsed");
            if (repeat)
            {
                var repeated = await ProviderServingTrustRevalidation.RevalidateAsync(
                    registry, store, activation, "publisher trust still lapsed");
                Assert.Multiple(() =>
                {
                    Assert.That(repeated.Code, Is.EqualTo("serving-activation-unavailable"));
                    Assert.That(repeated.Revalidated, Is.False);
                });
            }

            var current = registry.Current!;
            var storeRoot = Path.Combine(root, "store");
            var observation = new Cbi45Observation(
                result.Code,
                result.RefusedBy == "none" ? null : result.RefusedBy,
                result.Revalidated,
                result.Continued,
                result.ServingPolicyIdentity != chain.LaunchPolicyIdentity,
                activation.MemberReleased,
                !provider.HasExited,
                Directory.Exists(storeRoot) && Directory.EnumerateDirectories(storeRoot).Any(),
                result.ServingPolicyIdentity is null || result.ServingPolicyIdentity == current.Policy.Identity,
                result.Authorization is null || result.Authorization.ContentIdentity == chain.StagedIdentity);

            if (result.Continued)
            {
                await activation.RetireAsync("CBI45 test completed.");
            }
            await activation.DisposeAsync();
            activation = null;
            if (!provider!.HasExited) await provider.DisposeAsync();
            store.Remove(chain.StagedIdentity!.Value);
            provider = null;
            return observation;
        }
        finally
        {
            if (activation is not null) await activation.DisposeAsync();
            if (provider is not null) await provider.DisposeAsync();
            Cbi32DeleteTree(root);
        }
    }

    private static JsonDocument Cbi45Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi45-serving-revalidation-vectors.json")));

    private static async Task<Cbi45Observation> Cbi45RunAsync(JsonDocument fixture, string mutation, bool repeat = false) =>
        await Cbi45RunAsync(fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(vector => vector.GetProperty("mutation").GetString() == mutation), repeat);

    private static async Task<Cbi45Observation> Cbi45RunAsync(string mutation, bool repeat = false)
    {
        using var fixture = Cbi45Fixture();
        return await Cbi45RunAsync(fixture, mutation, repeat);
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi45_C6_both_roots_execute_the_shared_serving_vectors()
    {
        using var fixture = Cbi45Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = await Cbi45RunAsync(vector);
            var label = vector.GetProperty("mutation").GetString();
            Assert.Multiple(() =>
            {
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()), label);
                Assert.That(actual.RefusedBy, Is.EqualTo(vector.GetProperty("refusedBy").GetString()), label);
                Assert.That(actual.Revalidated, Is.EqualTo(vector.GetProperty("revalidated").GetBoolean()), label);
                Assert.That(actual.Continued, Is.EqualTo(vector.GetProperty("continued").GetBoolean()), label);
                Assert.That(actual.PolicyChanged, Is.EqualTo(vector.GetProperty("policyChanged").GetBoolean()), label);
                Assert.That(actual.MemberReleased, Is.EqualTo(vector.GetProperty("memberReleased").GetBoolean()), label);
                Assert.That(actual.ProviderRunning, Is.EqualTo(vector.GetProperty("providerRunning").GetBoolean()), label);
                Assert.That(actual.StagedSetRemains, Is.EqualTo(vector.GetProperty("stagedSetRemains").GetBoolean()), label);
                Assert.That(actual.ServingPolicyIsCurrent, Is.True, label);
                Assert.That(actual.DecisionMatchesStagedIdentity, Is.True, label);
            });
        }
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi45_C1_the_serving_decision_is_current() =>
        Assert.That((await Cbi45RunAsync("unchanged")).Revalidated, Is.True);

    [Test, Category("CrossProcess")]
    public async Task Cbi45_C2_lapsed_trust_stops_service()
    {
        using var fixture = Cbi45Fixture();
        foreach (var mutation in new[] { "publisher-revoked", "publisher-removed" })
        {
            var actual = await Cbi45RunAsync(fixture, mutation);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Continued, Is.False, mutation);
                Assert.That(actual.MemberReleased, Is.False, mutation);
                Assert.That(actual.ProviderRunning, Is.False, mutation);
                Assert.That(actual.StagedSetRemains, Is.False, mutation);
            });
        }
    }

    [Test, Category("CrossProcess")]
    public async Task Cbi45_C3_an_unrelated_policy_change_preserves_service() =>
        Assert.That((await Cbi45RunAsync("unrelated-revocation")).Continued, Is.True);

    [Test, Category("CrossProcess")]
    public async Task Cbi45_C4_retained_verified_evidence_is_evaluated() =>
        Assert.That((await Cbi45RunAsync("unchanged")).DecisionMatchesStagedIdentity, Is.True);

    [Test, Category("CrossProcess")]
    public async Task Cbi45_C5_a_withdrawn_activation_cannot_be_revalidated_twice() =>
        Assert.That((await Cbi45RunAsync("publisher-revoked", repeat: true)).Continued, Is.False);
}
