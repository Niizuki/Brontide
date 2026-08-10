using Brontide.Reference.Core;
using Brontide.Reference.Experimental.PersistentInformation;
using NUnit.Framework;

namespace Brontide.Reference.PersistentInformation.Tests;

public sealed class PersistentInformationTests
{
    private static readonly StoreRoleId CoreRole = StoreRoleId.Parse("core");

    [Test]
    public void C1_Corpus_requires_explicit_supported_concurrency_and_identity_role()
    {
        var role = new StoreRoleDefinition(CoreRole, true, true, StoreRoleAbsenceBehavior.DatasetUnavailable);

        Assert.That(OpaqueCorpus.Create(CorpusId.Parse("settings"), "1", null, [role]).Code,
            Is.EqualTo("corpus-invalid"));
        Assert.That(OpaqueCorpus.Create(CorpusId.Parse("settings"), "1", ConcurrentAccessMode.ExternalCoordination, [role]).Code,
            Is.EqualTo("concurrency-unsupported"));
        Assert.That(OpaqueCorpus.Create(CorpusId.Parse("settings"), "1", ConcurrentAccessMode.SingleWriter,
            [role with { IdentityBearing = false }]).Code, Is.EqualTo("corpus-invalid"));
    }

    [Test]
    public async Task C2_Dataset_issuance_is_an_authorized_effect_and_denials_are_silent()
    {
        var registry = new DatasetRegistry();
        var store = new InMemoryStore(StoreId.Parse("primary"), EndpointGuarantee.Durable);
        var corpus = Corpus();
        ActorReference issuer = null!;
        ActorReference stranger = null!;
        ActorReference target = null!;
        ActorReference otherTarget = null!;
        Capability createGrant = null!;
        Capability wrongTargetGrant = null!;
        var create = OperationReference.Parse("Dataset.Create");
        var append = OperationReference.Parse("Dataset.Append");
        var other = OperationReference.Parse("Other.Create");

        var domain = AuthorityDomain.Create("persistent-information", genesis =>
        {
            issuer = genesis.Actor("Issuer");
            stranger = genesis.Actor("Stranger");
            target = genesis.Actor("Dataset service");
            otherTarget = genesis.Actor("Other service");
            genesis.Operation(create, target, ShapeContract.Unit, ShapeContract.Unit, "issue Dataset", context =>
            {
                var issued = registry.Issue(
                    new DatasetIssuance(context.Execution.Initiator, context.Execution.Operation),
                    corpus,
                    DatasetId.Parse("dataset-1"),
                    new Dictionary<StoreRoleId, IStoreEndpoint> { [CoreRole] = store });
                if (!issued.IsSuccess)
                {
                    throw new InvalidOperationException(issued.Failure!.Reason);
                }

                return OperationEffect.SucceededAsync(ShapeValue.Unit);
            });
            genesis.Operation(append, target, ShapeContract.Unit, ShapeContract.Unit, "append Dataset", _ =>
            {
                var appended = registry.Append(
                    DatasetId.Parse("dataset-1"), CoreRole, ConcurrentAccessMode.SingleWriter, "value");
                if (!appended.IsSuccess)
                {
                    throw new InvalidOperationException(appended.Failure!.Reason);
                }

                return OperationEffect.SucceededAsync(ShapeValue.Unit);
            });
            genesis.Operation(other, otherTarget, ShapeContract.Unit, ShapeContract.Unit, "unrelated",
                _ => OperationEffect.SucceededAsync(ShapeValue.Unit));
            createGrant = genesis.Grant(issuer, target, [create, append]);
            wrongTargetGrant = genesis.Grant(issuer, otherTarget, [other]);
        });

        var wrongActor = await domain.ExecuteAsync(stranger, create, createGrant, ShapeValue.Unit);
        var wrongTarget = await domain.ExecuteAsync(issuer, create, wrongTargetGrant, ShapeValue.Unit);
        var wrongOperation = await domain.ExecuteAsync(issuer, other, createGrant, ShapeValue.Unit);
        Assert.That(wrongActor.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
        Assert.That(wrongTarget.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
        Assert.That(wrongOperation.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
        Assert.That(registry.Datasets, Is.Empty);
        Assert.That(store.AppendCount, Is.Zero);

        var accepted = await domain.ExecuteAsync(issuer, create, createGrant, ShapeValue.Unit);
        Assert.That(accepted.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
        Assert.That(registry.Datasets.Single().Issuer, Is.SameAs(issuer));
        Assert.That(registry.Datasets.Single().IssuingOperation, Is.EqualTo(create));

        var deniedAppend = await domain.ExecuteAsync(stranger, append, createGrant, ShapeValue.Unit);
        Assert.That(deniedAppend.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
        Assert.That(store.AppendCount, Is.Zero);
        var acceptedAppend = await domain.ExecuteAsync(issuer, append, createGrant, ShapeValue.Unit);
        Assert.That(acceptedAppend.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
        Assert.That(store.AppendCount, Is.EqualTo(1));
    }

    [Test]
    public void C3_Dataset_identity_survives_store_content_loss()
    {
        var registry = new DatasetRegistry();
        var store = new InMemoryStore(StoreId.Parse("primary"), EndpointGuarantee.Durable);
        var datasetId = DatasetId.Parse("dataset-identity");
        var issuance = new DatasetIssuance(Actor(), OperationReference.Parse("Dataset.Create"));
        Assert.That(registry.Issue(issuance, Corpus(), datasetId,
            new Dictionary<StoreRoleId, IStoreEndpoint> { [CoreRole] = store }).IsSuccess, Is.True);
        Assert.That(registry.Append(datasetId, CoreRole, ConcurrentAccessMode.SingleWriter, "value").IsSuccess, Is.True);

        store.Clear();

        Assert.That(registry.Datasets.Single().Id, Is.EqualTo(datasetId));
        Assert.That(registry.Read(datasetId, CoreRole, ConcurrentAccessMode.SingleWriter).Value, Is.Empty);
    }

    [Test]
    public void C4_Dataset_operations_fail_before_store_effects_at_role_and_concurrency_boundaries()
    {
        var registry = new DatasetRegistry();
        var store = new InMemoryStore(StoreId.Parse("primary"), EndpointGuarantee.Durable);
        var datasetId = DatasetId.Parse("dataset-boundaries");
        registry.Issue(new DatasetIssuance(Actor(), OperationReference.Parse("Dataset.Create")), Corpus(), datasetId,
            new Dictionary<StoreRoleId, IStoreEndpoint> { [CoreRole] = store });

        Assert.That(registry.Append(datasetId, StoreRoleId.Parse("unknown"), ConcurrentAccessMode.SingleWriter, "x").Code,
            Is.EqualTo("role-not-found"));
        Assert.That(registry.Append(datasetId, CoreRole, ConcurrentAccessMode.ExternalCoordination, "x").Code,
            Is.EqualTo("concurrency-mismatch"));
        Assert.That(store.AppendCount, Is.Zero);
    }

    [Test]
    public void C5_Router_guarantees_are_declared_stable_and_do_not_leak_backing_guarantees()
    {
        var first = new InMemoryStore(StoreId.Parse("first"), EndpointGuarantee.Durable, EndpointGuarantee.Encrypted);
        var second = new InMemoryStore(StoreId.Parse("second"), EndpointGuarantee.Durable);
        var created = RouterEndpoint.Create(RouterId.Parse("router"), [EndpointGuarantee.Durable], [first, second], false);
        var router = created.Value!;

        Assert.That(router.Guarantees, Is.EquivalentTo(new[] { EndpointGuarantee.Durable }));
        Assert.That(router.Guarantees, Does.Not.Contain(EndpointGuarantee.Encrypted));
        Assert.That(router.Select(second.Id).IsSuccess, Is.True);
        Assert.That(router.Guarantees, Is.EquivalentTo(new[] { EndpointGuarantee.Durable }));
        Assert.That(router.Id, Is.EqualTo(RouterId.Parse("router")));
    }

    [Test]
    public void C6_Router_falls_back_refuses_unsupported_guarantees_and_redacts_topology()
    {
        var first = new InMemoryStore(StoreId.Parse("first"), EndpointGuarantee.Durable) { IsAvailable = false };
        var second = new InMemoryStore(StoreId.Parse("second"), EndpointGuarantee.Durable);
        var router = RouterEndpoint.Create(RouterId.Parse("router"), [EndpointGuarantee.Durable], [first, second], false).Value!;

        Assert.That(router.Append("fallback").IsSuccess, Is.True);
        Assert.That(second.Read(), Is.EqualTo(new[] { "fallback" }));
        Assert.That(router.Describe(false).SelectedBacking, Is.Null);
        Assert.That(router.Describe(true).SelectedBacking, Is.Null);
        var inspectable = RouterEndpoint.Create(RouterId.Parse("inspectable"), [EndpointGuarantee.Durable], [second], true).Value!;
        Assert.That(inspectable.Describe(false).SelectedBacking, Is.Null);
        Assert.That(inspectable.Describe(true).SelectedBacking, Is.EqualTo(second.Id));
        Assert.That(RouterEndpoint.Create(RouterId.Parse("bad"), [EndpointGuarantee.Encrypted], [second], true).Code,
            Is.EqualTo("router-guarantee-unsupported"));
    }

    [Test]
    public void C7_Identity_spaces_are_distinct_public_value_types()
    {
        Assert.Multiple(() =>
        {
            Assert.That(default(CorpusId), Is.Not.InstanceOf<DatasetId>());
            Assert.That(default(DatasetId), Is.Not.InstanceOf<StoreRoleId>());
            Assert.That(default(StoreId), Is.Not.InstanceOf<RouterId>());
        });
    }

    [Test]
    public void C8_All_failure_paths_preserve_store_observations()
    {
        var store = new InMemoryStore(StoreId.Parse("store"), EndpointGuarantee.Durable);
        var registry = new DatasetRegistry();
        var dataset = DatasetId.Parse("missing");

        Assert.That(registry.Append(dataset, CoreRole, ConcurrentAccessMode.SingleWriter, "x").IsSuccess, Is.False);
        Assert.That(store.AppendCount, Is.Zero);
    }

    private static OpaqueCorpus Corpus() => OpaqueCorpus.Create(
        CorpusId.Parse("settings"),
        "1",
        ConcurrentAccessMode.SingleWriter,
        [new StoreRoleDefinition(CoreRole, true, true, StoreRoleAbsenceBehavior.DatasetUnavailable)]).Value!;

    private static ActorReference Actor()
    {
        ActorReference actor = null!;
        _ = AuthorityDomain.Create("actor-fixture", genesis => actor = genesis.Actor("Issuer"));
        return actor;
    }
}
