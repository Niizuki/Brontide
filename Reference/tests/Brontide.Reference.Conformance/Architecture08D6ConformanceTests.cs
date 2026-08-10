using Brontide.Reference.Core;
using NUnit.Framework;

namespace Brontide.Reference.Conformance;

[TestFixture]
public sealed class Architecture08D6ConformanceTests
{
    [Test]
    public void D6_C1_BR_08_ADV_C12_003_terminus_is_attributable_enumerable_and_policy_declared()
    {
        var fixture = CreateFixture();
        var record = fixture.Domain.OccurTerminus(fixture.PolicyActor, fixture.Grantor, "account retired");
        Assert.That(
            () => fixture.Domain.OccurTerminus(fixture.PolicyActor, fixture.Grantor, "duplicate"),
            Throws.InvalidOperationException);

        Assert.Multiple(() =>
        {
            Assert.That(record.Interaction.Actor, Is.SameAs(fixture.PolicyActor));
            Assert.That(record.ActorRetired, Is.SameAs(fixture.Grantor));
            Assert.That(record.Reason, Is.EqualTo("account retired"));
            Assert.That(record.Policy, Is.EqualTo(fixture.Domain.TerminusPolicy));
            Assert.That(record.HeldCapabilitiesExtinguished, Does.Contain(fixture.Root));
            Assert.That(record.OutboundGrantsSurviving, Does.Contain(fixture.Immortal));
            Assert.That(record.OutboundGrantsExtinguished, Does.Contain(fixture.LiveOutbound));
            Assert.That(fixture.Domain.TerminusOccurrences, Is.EqualTo(new[] { record }));
            Assert.That(fixture.Domain.Provenance.Last().Kind, Is.EqualTo(ProvenanceKind.Terminus));
            Assert.That(fixture.Domain.TerminusPolicy.OutboundGrantDisposition,
                Is.EqualTo(OutboundGrantTerminusDisposition.ImmortalSurvivesIndefinitely));
            Assert.That(fixture.Domain.TerminusPolicy.ActorReferenceDisposition,
                Is.EqualTo(ActorReferenceTerminusDisposition.RetainedWithoutReuse));
        });
    }

    [Test]
    public async Task D6_C2_BR_08_ADV_C12_001_held_authority_denies_after_retirement_without_erasing_identity()
    {
        var fixture = CreateFixture();
        fixture.Domain.OccurTerminus(fixture.PolicyActor, fixture.Grantor, "holder retired");

        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.Grantor, fixture.Operation, fixture.Root, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(result.Outcome.Message, Does.Contain("retired"));
            Assert.That(fixture.Effects(), Is.Zero);
            Assert.That(fixture.Domain.Actors, Does.Contain(fixture.Grantor));
            Assert.That(fixture.Domain.Capabilities, Does.Contain(fixture.Root));
            Assert.That(fixture.Domain.RetiredActors, Does.Contain(fixture.Grantor));
        });
    }

    [Test]
    public async Task D6_C3_BR_08_ADV_C12_002_immortal_outbound_grant_survives_with_grantor_attribution()
    {
        var fixture = CreateFixture();
        fixture.Domain.OccurTerminus(fixture.PolicyActor, fixture.Grantor, "grantor retired");

        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.Holder, fixture.Operation, fixture.Immortal, ShapeValue.Unit);
        var narrowed = fixture.Immortal.Delegate(fixture.DescendantHolder);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(fixture.Effects(), Is.EqualTo(1));
            Assert.That(fixture.Immortal.Parent, Is.SameAs(fixture.Root));
            Assert.That(fixture.Immortal.Parent!.Holder, Is.SameAs(fixture.Grantor));
            Assert.That(narrowed.Parent, Is.SameAs(fixture.Immortal));
            Assert.That(fixture.Domain.Capabilities, Does.Contain(narrowed));
        });
    }

    [Test]
    public async Task D6_C4_liveness_scoped_outbound_grant_and_descendants_end_at_terminus()
    {
        var fixture = CreateFixture();
        var liveDescendant = fixture.LiveOutbound.Delegate(fixture.DescendantHolder);
        fixture.Domain.OccurTerminus(fixture.PolicyActor, fixture.Grantor, "relationship ended");

        var direct = await fixture.Domain.ExecuteDraft08Async(
            fixture.Holder, fixture.Operation, fixture.LiveOutbound, ShapeValue.Unit);
        var descendant = await fixture.Domain.ExecuteDraft08Async(
            fixture.DescendantHolder, fixture.Operation, liveDescendant, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(direct.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(descendant.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(fixture.Effects(), Is.Zero);
            Assert.That(fixture.Lease.Renew(fixture.Grantor), Is.False);
            Assert.That(() => fixture.Root.Delegate(fixture.Holder), Throws.InvalidOperationException);
        });
    }

    private static Fixture CreateFixture()
    {
        var effects = 0;
        var clock = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var fixture = new Fixture { Effects = () => effects, Clock = clock };
        fixture.Operation = OperationReference.Parse("Example:ExecuteD6");
        fixture.Domain = AuthorityDomain.Create("architecture-0.8-d6", clock, genesis =>
        {
            fixture.PolicyActor = genesis.Actor("Retirement policy");
            fixture.Grantor = genesis.Actor("Grantor A");
            fixture.Holder = genesis.Actor("Holder B");
            fixture.DescendantHolder = genesis.Actor("Holder C");
            fixture.Target = genesis.Actor("Target");
            genesis.Operation(fixture.Operation, fixture.Target, ShapeContract.Unit, ShapeContract.Unit,
                "D6 checked effect", _ => { effects++; return OperationEffect.SucceededAsync(ShapeValue.Unit); });
            fixture.Root = genesis.Grant(fixture.Grantor, fixture.Target, [fixture.Operation]);
            fixture.Immortal = fixture.Root.Delegate(fixture.Holder);
            fixture.Lease = genesis.Lease(fixture.Grantor, TimeSpan.FromHours(1));
            fixture.LiveRoot = genesis.Grant(
                fixture.Grantor, fixture.Target, [fixture.Operation], [new LivenessLeaseConstraint(fixture.Lease)]);
            fixture.LiveOutbound = fixture.LiveRoot.Delegate(fixture.Holder);
        });
        return fixture;
    }

    private sealed class Fixture
    {
        public AuthorityDomain Domain { get; set; } = null!;
        public ManualTimeProvider Clock { get; set; } = null!;
        public ActorReference PolicyActor { get; set; } = null!;
        public ActorReference Grantor { get; set; } = null!;
        public ActorReference Holder { get; set; } = null!;
        public ActorReference DescendantHolder { get; set; } = null!;
        public ActorReference Target { get; set; } = null!;
        public OperationReference Operation { get; set; }
        public Capability Root { get; set; } = null!;
        public Capability Immortal { get; set; } = null!;
        public LivenessLease Lease { get; set; } = null!;
        public Capability LiveRoot { get; set; } = null!;
        public Capability LiveOutbound { get; set; } = null!;
        public Func<int> Effects { get; set; } = null!;
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
