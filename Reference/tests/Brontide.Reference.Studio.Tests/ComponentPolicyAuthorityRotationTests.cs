using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed record Cbi57Observation(string Code, long Generation, bool BytesChanged);

    private static ProviderPublisherTrustPolicyAuthorityId Cbi57Authority(ECDsa key) =>
        ProviderPublisherTrustPolicyAuthorityId.Create(
            Convert.ToHexString(SHA256.HashData(key.ExportSubjectPublicKeyInfo())));

    /// <summary>
    /// Builds a rotation statement. A statement whose generation is not a transition at all is still
    /// signed over a well-formed manifest, so its refusal stays attributable to the generation rather
    /// than to bytes nobody would have verified.
    /// </summary>
    private static ProviderPolicyAuthorityRotationStatement Cbi57Statement(
        long generation,
        long policySequence,
        ProviderPublisherTrustPolicyId? policyIdentity,
        ECDsa previous,
        ECDsa next,
        string evidence = "valid",
        ECDsa? other = null)
    {
        var previousId = Cbi57Authority(previous);
        var nextId = Cbi57Authority(next);
        var manifest = ProviderPolicyAuthorityRotationManifest.Encode(
            Math.Max(generation, 1), policySequence, policyIdentity, previousId, nextId);
        byte[] Sign(ECDsa key) => key.SignData(
            manifest, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        var previousSigner = evidence == "wrong-predecessor-signer" ? other! : previous;
        var nextSigner = evidence == "wrong-successor-signer" ? other! : next;
        var previousKey = evidence == "wrong-predecessor-key" ? other! : previous;
        var nextKey = evidence == "wrong-successor-key" ? other! : next;
        return new(generation, policySequence, policyIdentity, previousId, nextId,
            evidence == "algorithm" ? "RSA-PSS-SHA256" : "ECDSA-P256-SHA256",
            Convert.ToBase64String(previousKey.ExportSubjectPublicKeyInfo()),
            Convert.ToBase64String(nextKey.ExportSubjectPublicKeyInfo()),
            Convert.ToBase64String(Sign(previousSigner)),
            Convert.ToBase64String(Sign(nextSigner)));
    }

    private static Cbi57Observation Cbi57Run(JsonElement vector)
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi57-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "policy.checkpoint");
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var c = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var keys = new Dictionary<string, ECDsa> { ["A"] = a, ["B"] = b, ["C"] = c };
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a)).Registry!;
            var policy = Cbi37Policy(false);
            Assert.That(durable.Apply(Cbi37Sign(a, 1, null, policy)).IsApplied, Is.True);
            var before = File.ReadAllBytes(path);
            var result = durable.Rotate(Cbi57Statement(
                vector.GetProperty("generation").GetInt64(),
                vector.GetProperty("policySequence").GetInt64(),
                vector.GetProperty("policyIdentity").GetString() switch
                {
                    "current" => policy.Identity,
                    "other" => ProviderPublisherTrustPolicyId.Create(new string('0', 64)),
                    _ => null,
                },
                keys[vector.GetProperty("previous").GetString()!],
                keys[vector.GetProperty("next").GetString()!],
                vector.GetProperty("evidence").GetString()!,
                c));
            return new(result.Code, result.Generation,
                !File.ReadAllBytes(path).AsSpan().SequenceEqual(before));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static JsonDocument Cbi57Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(
        TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
        "cbi57-policy-authority-rotation-vectors.json")));

    [Test]
    public void Shared_cbi57_vectors_rotate_only_on_a_countersigned_successor_of_the_active_authority()
    {
        using var fixture = Cbi57Fixture();
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var actual = Cbi57Run(vector);
            var label = vector.GetProperty("name").GetString();
            Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()), label);
        }
    }

    [Test]
    public void Cbi57_C1_the_pin_is_immutable_and_the_active_authority_is_derived_from_it()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi57-pin-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "policy.checkpoint");
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var opened = DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a));
            var durable = opened.Registry!;
            var policy = Cbi37Policy(false);
            durable.Apply(Cbi37Sign(a, 1, null, policy));
            Assert.Multiple(() =>
            {
                Assert.That(opened.AuthorityFloor!.Generation, Is.Zero);
                Assert.That(durable.ActiveAuthorityIdentity, Is.EqualTo(Cbi57Authority(a)));
            });
            var rotated = durable.Rotate(Cbi57Statement(1, 1, policy.Identity, a, b));

            var recovered = DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a));
            Assert.Multiple(() =>
            {
                Assert.That(rotated.Code, Is.EqualTo("policy-authority-rotation-applied"));
                Assert.That(recovered.Code, Is.EqualTo("policy-checkpoint-recovered"));
                Assert.That(recovered.Registry!.AuthorityIdentity, Is.EqualTo(Cbi57Authority(a)),
                    "the stored pin is never rewritten by a rotation");
                Assert.That(recovered.Registry.ActiveAuthorityIdentity, Is.EqualTo(Cbi57Authority(b)));
                Assert.That(recovered.Registry.AuthorityGeneration, Is.EqualTo(1));
                Assert.That(recovered.AuthorityFloor!.ActiveAuthority, Is.EqualTo(Cbi57Authority(b)));
            });

            // The successor is not a pin. Opening the same record as though B had been pinned out of
            // band is refused, because the chain it must be verified from starts at A.
            Assert.That(DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(b)).Code,
                Is.EqualTo("policy-checkpoint-authority-mismatch"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi57_C2_a_successor_must_be_authorized_and_countersigned_over_the_same_manifest()
    {
        using var fixture = Cbi57Fixture();
        var vectors = fixture.RootElement.GetProperty("vectors").EnumerateArray().ToArray();
        Assert.Multiple(() =>
        {
            foreach (var name in new[]
            {
                "predecessor-signature-invalid", "successor-unproven",
                "predecessor-key-mismatch", "successor-key-mismatch", "unsupported-algorithm",
            })
            {
                var vector = vectors.Single(item => item.GetProperty("name").GetString() == name);
                var actual = Cbi57Run(vector);
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()), name);
                Assert.That(actual.Generation, Is.Zero, name);
            }
        });

        // A countersignature is bound to the transition it accepts: B's signature over generation 1
        // does not carry to generation 2, which is what covering one manifest with both keys buys.
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi57-bind-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "policy.checkpoint");
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var c = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a)).Registry!;
            Assert.That(durable.Rotate(Cbi57Statement(1, 0, null, a, b)).IsApplied, Is.True);
            var lifted = Cbi57Statement(1, 0, null, b, c);
            var replayed = durable.Rotate(lifted with { Generation = 2 });
            Assert.Multiple(() =>
            {
                Assert.That(replayed.Code, Is.EqualTo("policy-authority-signature-invalid"));
                Assert.That(durable.ActiveAuthorityIdentity, Is.EqualTo(Cbi57Authority(b)));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi57_C3_a_rotation_is_one_atomic_link_and_nothing_is_ever_staged()
    {
        using var fixture = Cbi57Fixture();
        Assert.Multiple(() =>
        {
            foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
            {
                var actual = Cbi57Run(vector);
                var refused = vector.GetProperty("code").GetString() != "policy-authority-rotation-applied";
                var label = vector.GetProperty("name").GetString();
                Assert.That(actual.BytesChanged, Is.EqualTo(!refused), label);
                Assert.That(actual.Generation, Is.EqualTo(refused ? 0 : 1), label);
            }
        });

        // The absent phase is the contract: unlike CBI56 there is no staged successor to announce,
        // confirm, or abandon, because a countersignature already proves what a network attempt would.
        Assert.That(typeof(DurableProviderPublisherTrustPolicyRegistry).GetMembers()
            .Where(member => member.Name.Contains("Stage", StringComparison.Ordinal)
                || member.Name.Contains("Confirm", StringComparison.Ordinal)), Is.Empty);

        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi57-write-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "policy.checkpoint");
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a)).Registry!;
            Directory.CreateDirectory(path + ".tmp");
            var failed = durable.Rotate(Cbi57Statement(1, 0, null, a, b));
            Assert.Multiple(() =>
            {
                Assert.That(failed.Code, Is.EqualTo("policy-checkpoint-write-failed"));
                Assert.That(failed.ActiveAuthority, Is.EqualTo(Cbi57Authority(a)));
                Assert.That(durable.AuthorityGeneration, Is.Zero);
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi57_C4_retirement_is_immediate_and_the_predecessors_history_stays_verifiable()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi57-retire-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "policy.checkpoint");
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a)).Registry!;
            var initial = Cbi37Policy(false);
            var successor = Cbi37Policy(true);
            durable.Apply(Cbi37Sign(a, 1, null, initial));
            Assert.That(durable.Rotate(Cbi57Statement(1, 1, initial.Identity, a, b)).IsApplied, Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(durable.Apply(Cbi37Sign(a, 2, initial.Identity, successor)).Code,
                    Is.EqualTo("policy-update-authority-mismatch"),
                    "the retired predecessor can sign nothing further");
                Assert.That(durable.Apply(Cbi37Sign(b, 2, initial.Identity, successor)).IsApplied, Is.True);
            });

            var recovered = DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a));
            Assert.Multiple(() =>
            {
                Assert.That(recovered.Code, Is.EqualTo("policy-checkpoint-recovered"),
                    "the predecessor's own update is re-verified as its work");
                Assert.That(recovered.Registry!.Current!.Sequence, Is.EqualTo(2));
            });

            // An update cannot precede the rotation that authorized its signer, which is the order the
            // retained chain states and the reason a rotation is a link rather than a side record.
            var live = new ProviderPublisherTrustPolicyRegistry(Cbi57Authority(a));
            Assert.That(live.Apply(Cbi37Sign(b, 1, null, initial)).Code,
                Is.EqualTo("policy-update-authority-mismatch"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi57_C5_the_record_advances_its_format_only_when_a_rotation_exists()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi57-record-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "policy.checkpoint");
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a)).Registry!;
            var initial = Cbi37Policy(false);
            durable.Apply(Cbi37Sign(a, 1, null, initial));
            var updatesOnly = File.ReadAllBytes(path);
            Assert.That(Encoding.UTF8.GetString(updatesOnly, 4, 5), Is.EqualTo("CBI38"),
                "a host that never rotates keeps the record shape CBI38 wrote");
            var rotation = Cbi57Statement(1, 1, initial.Identity, a, b);
            Assert.That(durable.Rotate(rotation).IsApplied, Is.True);
            var rotated = File.ReadAllBytes(path);
            Assert.That(Encoding.UTF8.GetString(rotated, 4, 5), Is.EqualTo("CBI57"));

            // Damage to the retained rotation is refused rather than replayed: recovery re-verifies the
            // transition instead of trusting that it was verified once.
            var damaged = rotated.ToArray();
            var signature = Encoding.UTF8.GetBytes(rotation.NextSignatureBase64);
            var offset = damaged.AsSpan().IndexOf(signature);
            Assert.That(offset, Is.GreaterThanOrEqualTo(0));
            damaged[offset] = damaged[offset] == (byte)'A' ? (byte)'B' : (byte)'A';
            File.WriteAllBytes(path, damaged);
            Assert.That(DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a)).Code,
                Is.EqualTo("policy-checkpoint-invalid-chain"));

            // An unknown link tag is refused by decoding. The tag is the first int32 after the format
            // marker, the pinned authority, and the link count.
            var unknownTag = rotated.ToArray();
            unknownTag[4 + 5 + 4 + 64 + 4 + 3] = 2;
            File.WriteAllBytes(path, unknownTag);
            Assert.That(DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a)).Code,
                Is.EqualTo("policy-checkpoint-corrupt"));

            File.WriteAllBytes(path, [.. rotated, 0]);
            Assert.That(DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a)).Code,
                Is.EqualTo("policy-checkpoint-corrupt"));

            File.WriteAllBytes(path, rotated[..(rotated.Length / 2)]);
            Assert.That(DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a)).Code,
                Is.EqualTo("policy-checkpoint-corrupt"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi57_C6_an_external_authority_floor_detects_rotation_rollback()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi57-floor-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "policy.checkpoint");
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var c = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a)).Registry!;
            var initial = Cbi37Policy(false);
            durable.Apply(Cbi37Sign(a, 1, null, initial));
            var beforeRotation = File.ReadAllBytes(path);
            var rotated = durable.Rotate(Cbi57Statement(1, 1, initial.Identity, a, b));
            var floor = rotated.Floor;
            Assert.That(floor.Generation, Is.EqualTo(1));

            File.WriteAllBytes(path, beforeRotation);
            Assert.Multiple(() =>
            {
                Assert.That(DurableProviderPublisherTrustPolicyRegistry.Open(
                        path, Cbi57Authority(a), null, floor).Code,
                    Is.EqualTo("policy-authority-rollback-detected"));
                Assert.That(DurableProviderPublisherTrustPolicyRegistry.Open(
                        path, Cbi57Authority(a), null,
                        ProviderPolicyAuthorityFloor.Restore(0, Cbi57Authority(a))).Code,
                    Is.EqualTo("policy-checkpoint-recovered"),
                    "the pre-rotation record is exactly what its own floor describes");
            });

            // An equal generation reached under a different successor is a conflict rather than a
            // rollback of sequence, and it fails closed the same way.
            File.Delete(path);
            var second = DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a)).Registry!;
            second.Apply(Cbi37Sign(a, 1, null, initial));
            second.Rotate(Cbi57Statement(1, 1, initial.Identity, a, c));
            Assert.Multiple(() =>
            {
                Assert.That(DurableProviderPublisherTrustPolicyRegistry.Open(
                        path, Cbi57Authority(a), null, floor).Code,
                    Is.EqualTo("policy-authority-rollback-detected"));
                Assert.That(DurableProviderPublisherTrustPolicyRegistry.Open(
                        path, Cbi57Authority(a), null,
                        ProviderPolicyAuthorityFloor.Restore(1, Cbi57Authority(c))).Code,
                    Is.EqualTo("policy-checkpoint-recovered"));
            });

            // A deleted record cannot satisfy a floor that has seen a rotation.
            File.Delete(path);
            Assert.That(DurableProviderPublisherTrustPolicyRegistry.Open(
                    path, Cbi57Authority(a), null, floor).Code,
                Is.EqualTo("policy-authority-rollback-detected"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public void Cbi57_C7_a_rotation_moves_no_policy_disposition_or_compared_identity()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi57-trust-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "policy.checkpoint");
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a)).Registry!;
            var initial = Cbi37Policy(false);
            durable.Apply(Cbi37Sign(a, 1, null, initial));
            var before = durable.Current!;
            Assert.That(durable.Rotate(Cbi57Statement(1, 1, initial.Identity, a, b)).IsApplied, Is.True);
            var after = durable.Current!;
            Assert.Multiple(() =>
            {
                Assert.That(after.Policy.Identity, Is.EqualTo(before.Policy.Identity));
                Assert.That(after.Policy.Entries, Is.EqualTo(before.Policy.Entries));
                Assert.That(after.Sequence, Is.EqualTo(before.Sequence));
                Assert.That(durable.AuthorityIdentity, Is.EqualTo(Cbi57Authority(a)));
                Assert.That(durable.ActiveAuthorityIdentity, Is.EqualTo(Cbi57Authority(b)));
            });

            // The rotation leaves the snapshot untouched, so comparing it with itself would prove
            // nothing. What has to hold is that the next snapshot — verified under the successor — still
            // names the trust root, because that is the identity CBI44's launch decision and CBI45's
            // serving revalidation compare against what a launch recorded.
            var succeeded = durable.Apply(Cbi37Sign(b, 2, initial.Identity, Cbi37Policy(true)));
            Assert.Multiple(() =>
            {
                Assert.That(succeeded.IsApplied, Is.True);
                Assert.That(succeeded.Current!.AuthorityIdentity, Is.EqualTo(Cbi57Authority(a)));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi57_C7_a_serving_member_survives_a_rotation_and_an_update_signed_by_the_successor()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi57-serving-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        StagedProviderProcess? provider = null;
        ProviderServingActivation? activation = null;
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var publisher = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var (request, source) = Cbi33Input("reference", "none");
            var evidence = Cbi43Evidence(publisher, request);
            var initial = Cbi44Policy(
                Cbi44Entry(evidence.PublisherKeyId, revoked: false),
                Cbi44Entry(Cbi44OtherPublisher, revoked: false));
            var custody = ProviderPublisherTrustPolicyCustody.Open(
                Path.Combine(root, "policy.checkpoint"), Path.Combine(root, "policy.floor"),
                Cbi57Authority(a));
            Assert.That(custody.IsOpened, Is.True);
            var registry = custody.Registry!;
            Assert.That(registry.Apply(Cbi37Sign(a, 1, null, initial)).IsApplied, Is.True);

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

            Assert.That(registry.Rotate(Cbi57Statement(1, 1, initial.Identity, a, b)).IsApplied, Is.True);
            var successor = Cbi45Successor("unrelated-revocation", evidence.PublisherKeyId);
            Assert.That(registry.Apply(Cbi37Sign(b, 2, initial.Identity, successor)).IsApplied, Is.True,
                "the successor authority signs ordinary policy from the generation it becomes active");

            var result = await ProviderServingTrustRevalidation.RevalidateAsync(
                registry, store, activation, "publisher trust lapsed");
            Assert.Multiple(() =>
            {
                Assert.That(result.Code, Is.EqualTo("publisher-trust-current"));
                Assert.That(result.Continued, Is.True);
                Assert.That(provider!.HasExited, Is.False);
            });

            await activation.RetireAsync("CBI57 test completed.");
            await activation.DisposeAsync();
            activation = null;
            await provider!.DisposeAsync();
            store.Remove(chain.StagedIdentity!.Value);
            provider = null;
        }
        finally
        {
            if (activation is not null) await activation.DisposeAsync();
            if (provider is not null) await provider.DisposeAsync();
            Cbi32DeleteTree(root);
        }
    }

    [Test]
    public void Cbi57_C8_a_rotation_before_any_policy_exists_is_an_ordinary_transition()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brontide-cbi57-empty-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "policy.checkpoint");
        try
        {
            using var a = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var b = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var durable = DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a)).Registry!;
            Assert.That(durable.Rotate(Cbi57Statement(1, 0, null, a, b)).IsApplied, Is.True);
            var initial = Cbi37Policy(false);
            Assert.Multiple(() =>
            {
                Assert.That(durable.Apply(Cbi37Sign(a, 1, null, initial)).Code,
                    Is.EqualTo("policy-update-authority-mismatch"));
                Assert.That(durable.Apply(Cbi37Sign(b, 1, null, initial)).IsApplied, Is.True);
            });
            var recovered = DurableProviderPublisherTrustPolicyRegistry.Open(path, Cbi57Authority(a));
            Assert.Multiple(() =>
            {
                Assert.That(recovered.Code, Is.EqualTo("policy-checkpoint-recovered"));
                Assert.That(recovered.Registry!.Current!.Sequence, Is.EqualTo(1));
                Assert.That(recovered.Registry.AuthorityGeneration, Is.EqualTo(1));
            });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
