using System.Security.Cryptography;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
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
