using Brontide.Reference.Core;
using Brontide.Reference.Experimental.PersistentInformation;
using NUnit.Framework;

namespace Brontide.Reference.Conformance;

public sealed class Architecture08D5ConformanceTests
{
    private static readonly StoreRoleId CoreRole = StoreRoleId.Parse("core");

    [Test]
    public async Task D5_C1_BR_08_ADV_C10_001_creation_derives_resource_authority_from_provider()
    {
        var fixture = CreateFixture("tenant/orders");
        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.Requester, fixture.Create, fixture.CreateGrant, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(fixture.Issuance, Is.Not.Null);
            Assert.That(fixture.Issuance!.ResourceCapability.Parent, Is.SameAs(fixture.ProviderAuthority));
            Assert.That(fixture.Issuance.ResourceCapability.Holder, Is.SameAs(fixture.Requester));
            Assert.That(fixture.Issuance.ResourceCapability.Target, Is.SameAs(fixture.Provider));
            Assert.That(fixture.Issuance.ResourceCapability.RootOperations, Is.EquivalentTo(new[] { fixture.Use }));
            Assert.That(fixture.Issuance.ResourceCapability.AddedConstraints,
                Has.Exactly(1).InstanceOf<DatasetAuthorityConstraint>());
        });
    }

    [Test]
    public async Task D5_C2_BR_08_ADV_C10_001_issuance_is_an_attributable_delegation_record()
    {
        var fixture = CreateFixture("tenant/profile");
        var capabilityCount = fixture.Domain.Capabilities.Count;
        var genesisCount = fixture.Domain.GenesisOccurrences.Count;
        await fixture.Domain.ExecuteDraft08Async(fixture.Requester, fixture.Create, fixture.CreateGrant, ShapeValue.Unit);

        var issuance = fixture.Issuance!;
        var chain = issuance.ResourceCapability.DerivationChain();
        Assert.Multiple(() =>
        {
            Assert.That(fixture.Domain.Capabilities.Count, Is.EqualTo(capabilityCount + 1));
            Assert.That(fixture.Domain.GenesisOccurrences.Count, Is.EqualTo(genesisCount));
            Assert.That(chain[0].IsPrimordial, Is.True);
            Assert.That(chain[^1], Is.SameAs(issuance.ResourceCapability));
            Assert.That(issuance.ProviderAuthority, Is.SameAs(fixture.ProviderAuthority));
            Assert.That(issuance.Execution, Is.EqualTo(fixture.Execution));
            Assert.That(issuance.Dataset.Id, Is.EqualTo(DatasetId.Parse("tenant/profile")));
        });
    }

    [Test]
    public async Task D5_C3_BR_08_ADV_C10_002_out_of_scope_issuance_refuses_without_resource_effects()
    {
        var fixture = CreateFixture("other/orders");
        var capabilityCount = fixture.Domain.Capabilities.Count;
        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.Requester, fixture.Create, fixture.CreateGrant, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Failed));
            Assert.That(fixture.RefusalCode, Is.EqualTo("dataset-authority-exceeded"));
            Assert.That(fixture.Registry.Datasets, Is.Empty);
            Assert.That(fixture.Domain.Capabilities.Count, Is.EqualTo(capabilityCount));
            Assert.That(fixture.Store.AppendCount, Is.Zero);
        });
    }

    [Test]
    public async Task D5_C3_wrong_holder_provider_authority_refuses_without_resource_effects()
    {
        var fixture = CreateFixture("tenant/orders");
        fixture.AuthorityToIssue = fixture.CreateGrant;
        var capabilityCount = fixture.Domain.Capabilities.Count;
        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.Requester, fixture.Create, fixture.CreateGrant, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Failed));
            Assert.That(fixture.RefusalCode, Is.EqualTo("dataset-authority-invalid"));
            Assert.That(fixture.Registry.Datasets, Is.Empty);
            Assert.That(fixture.Domain.Capabilities.Count, Is.EqualTo(capabilityCount));
        });
    }

    private static Fixture CreateFixture(string dataset)
    {
        var fixture = new Fixture();
        fixture.Dataset = DatasetId.Parse(dataset);
        fixture.Store = new InMemoryStore(StoreId.Parse("primary"), EndpointGuarantee.Durable);
        fixture.Create = OperationReference.Parse("Dataset.Create.D5");
        fixture.Use = OperationReference.Parse("Dataset.Use.D5");
        fixture.Domain = AuthorityDomain.Create("architecture-0.8-d5", genesis =>
        {
            fixture.Requester = genesis.Actor("Requester");
            fixture.Provider = genesis.Actor("Dataset provider");
            genesis.Constraint(DatasetAuthorityConstraint.Declaration, DatasetAuthorityConstraint.Evaluate);
            genesis.Operation(fixture.Use, fixture.Provider, ShapeContract.Unit, ShapeContract.Unit,
                "use one Dataset", _ => OperationEffect.SucceededAsync(ShapeValue.Unit));
            genesis.Operation(fixture.Create, fixture.Provider, ShapeContract.Unit, ShapeContract.Unit,
                "create one Dataset", context =>
                {
                    var issued = fixture.Registry.IssueWithAuthority(
                        context,
                        fixture.AuthorityToIssue,
                        Corpus(),
                        fixture.Dataset,
                        new Dictionary<StoreRoleId, IStoreEndpoint> { [CoreRole] = fixture.Store });
                    if (!issued.IsSuccess)
                    {
                        fixture.RefusalCode = issued.Code;
                        return OperationEffect.FailedAsync(ShapeContract.Unit, ShapeValue.Unit, issued.Failure!.Reason);
                    }

                    fixture.Issuance = issued.Value;
                    fixture.Execution = context.Execution.Id;
                    return OperationEffect.SucceededAsync(ShapeValue.Unit);
                });
            fixture.CreateGrant = genesis.Grant(fixture.Requester, fixture.Provider, [fixture.Create]);
            fixture.ProviderAuthority = genesis.Grant(
                fixture.Provider,
                fixture.Provider,
                [fixture.Use],
                [DatasetAuthorityConstraint.ForSpace("tenant/")]);
            fixture.AuthorityToIssue = fixture.ProviderAuthority;
        });
        return fixture;
    }

    private static OpaqueCorpus Corpus() => OpaqueCorpus.Create(
        CorpusId.Parse("settings"),
        "1",
        ConcurrentAccessMode.SingleWriter,
        [new StoreRoleDefinition(CoreRole, true, true, StoreRoleAbsenceBehavior.DatasetUnavailable)]).Value!;

    private sealed class Fixture
    {
        public AuthorityDomain Domain { get; set; } = null!;
        public DatasetRegistry Registry { get; } = new();
        public ActorReference Requester { get; set; } = null!;
        public ActorReference Provider { get; set; } = null!;
        public Capability CreateGrant { get; set; } = null!;
        public Capability ProviderAuthority { get; set; } = null!;
        public Capability AuthorityToIssue { get; set; } = null!;
        public OperationReference Create { get; set; }
        public OperationReference Use { get; set; }
        public DatasetId Dataset { get; set; }
        public InMemoryStore Store { get; set; } = null!;
        public DatasetAuthorityIssuance? Issuance { get; set; }
        public ExecutionId Execution { get; set; }
        public string? RefusalCode { get; set; }
    }
}
