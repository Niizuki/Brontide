using Brontide.Reference.Core;
using NUnit.Framework;

namespace Brontide.Reference.Conformance;

[TestFixture]
public sealed class Architecture08D3ConformanceTests
{
    private static readonly CanonicalName DeclinedName = CanonicalName.Parse("Example:DeclinedPolicy");
    private static readonly CanonicalName NewName = CanonicalName.Parse("Example:AreaPolicy.V2");
    private static readonly CanonicalName OldName = CanonicalName.Parse("Example:AreaPolicy.V1");
    private static readonly ShapeReference AreaV1 = ShapeReference.Parse("Example:Area", 1);
    private static readonly ShapeReference AreaV2 = ShapeReference.Parse("Example:Area", 2);
    private static readonly ShapeReference PayloadV1 = ShapeReference.Parse("Example:Payload", 1);
    private static readonly ShapeReference PayloadV2 = ShapeReference.Parse("Example:Payload", 2);
    private static readonly OperationReference Execute = new(CanonicalName.Parse("Example:ExecuteD3"));

    [Test]
    public async Task D3_C1_BR_08_ADV_C9_001_declined_declaration_is_named_and_denies_before_effect()
    {
        var effects = 0;
        var fixture = CreateDomain(
            genesis => genesis.Constraint(Declaration(DeclinedName, BuiltInShapes.Text, "declined policy")),
            [new ValueConstraint(DeclinedName, ShapeValue.Text("sensitive-policy-value"))],
            _ => effects++);

        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.Holder, Execute, fixture.Capability, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(result.Outcome.Message, Does.Contain(DeclinedName.ToString()));
            Assert.That(result.Outcome.Message, Does.Not.Contain("sensitive-policy-value"));
            Assert.That(effects, Is.Zero);
        });
    }

    [Test]
    public void D3_C2_BR_08_ADV_C9_002_changed_semantics_under_one_name_is_rejected()
    {
        var error = Assert.Throws<InvalidOperationException>(() => AuthorityDomain.Create(
            "Brontide.Reference.Tests:D3.ImmutableDeclarations",
            genesis =>
            {
                RegisterShapes(genesis);
                genesis.Constraint(Declaration(NewName, AreaV1, "region must be admitted"), Allow);
                genesis.Constraint(Declaration(NewName, AreaV2, "region and exclusions must be admitted"), Allow);
            }));

        Assert.That(error!.Message, Does.Contain("new canonical name"));
    }

    [Test]
    public void D3_C3_BR_08_ADV_C9_003_recognition_set_is_complete_ordered_and_effect_free()
    {
        var evaluatorCalls = 0;
        var fixture = CreateDomain(
            genesis =>
            {
                genesis.Constraint(Declaration(DeclinedName, BuiltInShapes.Text, "declined policy"));
                genesis.Constraint(Declaration(OldName, AreaV1, "known policy"), (constraint, context) =>
                {
                    evaluatorCalls++;
                    return Allow(constraint, context);
                });
            });

        var first = fixture.Domain.ConstraintRecognitionSet;
        var second = fixture.Domain.ConstraintRecognitionSet;

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(first.Select(item => item.Declaration.Name.ToString()), Is.Ordered);
            Assert.That(first.Single(item => item.Declaration.Name == DeclinedName).Decision,
                Is.EqualTo(ConstraintRecognitionDecision.Declined));
            Assert.That(first.Single(item => item.Declaration.Name == OldName).Decision,
                Is.EqualTo(ConstraintRecognitionDecision.Implemented));
            Assert.That(first.Any(item => item.Declaration.Name == StandardConstraintNames.DelegationDepth), Is.True);
            Assert.That(evaluatorCalls, Is.Zero);
        });
    }

    [Test]
    public async Task D3_C4_BR_08_ADV_C8_001_constraint_value_version_is_not_projected()
    {
        var evaluatorCalls = 0;
        var effects = 0;
        var fixture = CreateDomain(
            genesis => genesis.Constraint(Declaration(NewName, AreaV1, "version-one policy"),
                (constraint, context) =>
                {
                    evaluatorCalls++;
                    return Allow(constraint, context);
                }),
            [new ValueConstraint(NewName, AreaValue(AreaV2, includeExclusions: true))],
            _ => effects++);

        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.Holder, Execute, fixture.Capability, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(evaluatorCalls, Is.Zero);
            Assert.That(effects, Is.Zero);
        });
    }

    [Test]
    public async Task D3_C5_BR_08_ADV_C8_002_authored_old_constraint_fallback_authorises()
    {
        var newCalls = 0;
        var oldCalls = 0;
        var effects = 0;
        var fixture = CreateDomain(
            genesis =>
            {
                genesis.Constraint(Declaration(NewName, AreaV1, "new policy"), (constraint, context) =>
                {
                    newCalls++;
                    return Allow(constraint, context);
                });
                genesis.Constraint(Declaration(OldName, AreaV1, "old fallback policy"), (constraint, context) =>
                {
                    oldCalls++;
                    return Allow(constraint, context);
                });
            },
            expressions:
            [
                new AnyOfConstraintExpression(
                    new ValueConstraint(NewName, AreaValue(AreaV2, includeExclusions: true)),
                    new ValueConstraint(OldName, AreaValue(AreaV1, includeExclusions: false)))
            ],
            effect: _ => effects++);

        var result = await fixture.Domain.ExecuteDraft08Async(
            fixture.Holder, Execute, fixture.Capability, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(newCalls, Is.Zero);
            Assert.That(oldCalls, Is.EqualTo(1));
            Assert.That(effects, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task D3_C6_BR_08_ADV_C8_003_payload_projection_remains_additive()
    {
        ShapeValue? delivered = null;
        ActorReference holder = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var domain = AuthorityDomain.Create("Brontide.Reference.Tests:D3.Payload", genesis =>
        {
            RegisterShapes(genesis);
            holder = genesis.Actor("Holder");
            target = genesis.Actor("Target");
            genesis.Operation(Execute, target, ShapeContract.For(PayloadV1), ShapeContract.Unit,
                "payload projection control", context =>
                {
                    delivered = context.Input;
                    return OperationEffect.SucceededAsync(ShapeValue.Unit);
                });
            capability = genesis.Grant(holder, target, [Execute]);
        });

        var presented = ShapeValue.Record(
            PayloadV2,
            ("command", ShapeValue.Text("run")),
            ("optional-note", ShapeValue.Text("ignored")));
        var result = await domain.ExecuteDraft08Async(holder, Execute, capability, presented);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(delivered!.Reference, Is.EqualTo(PayloadV1));
            Assert.That(((RecordShapeValue)delivered).Fields.Keys, Is.EqualTo(new[] { "command" }));
        });
    }

    private static DomainFixture CreateDomain(
        Action<AuthorityDomain.GenesisContext> declare,
        IEnumerable<Constraint>? constraints = null,
        Action<Brontide.Reference.Core.ExecutionContext>? effect = null,
        IEnumerable<ConstraintExpression>? expressions = null)
    {
        ActorReference holder = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var domain = AuthorityDomain.Create("Brontide.Reference.Tests:D3", genesis =>
        {
            RegisterShapes(genesis);
            holder = genesis.Actor("Holder");
            target = genesis.Actor("Target");
            declare(genesis);
            genesis.Operation(Execute, target, ShapeContract.Unit, ShapeContract.Unit, "D3 effect", context =>
            {
                effect?.Invoke(context);
                return OperationEffect.SucceededAsync(ShapeValue.Unit);
            });
            capability = expressions is null
                ? genesis.Grant(holder, target, [Execute], constraints)
                : genesis.GrantExpressions(holder, target, [Execute], expressions);
        });
        return new(domain, holder, capability);
    }

    private static void RegisterShapes(AuthorityDomain.GenesisContext genesis)
    {
        genesis.Shape(ShapeDefinition.Record(AreaV1, FragmentPolicy.Open,
            RecordField.Required("region", BuiltInShapes.Text)));
        genesis.Shape(ShapeDefinition.Record(AreaV2, FragmentPolicy.Open,
            RecordField.Required("region", BuiltInShapes.Text),
            RecordField.Optional("exclusions", BuiltInShapes.Text)));
        genesis.Shape(ShapeDefinition.Record(PayloadV1, FragmentPolicy.Open,
            RecordField.Required("command", BuiltInShapes.Text)));
        genesis.Shape(ShapeDefinition.Record(PayloadV2, FragmentPolicy.Open,
            RecordField.Required("command", BuiltInShapes.Text),
            RecordField.Optional("optional-note", BuiltInShapes.Text)));
    }

    private static ConstraintDeclaration Declaration(
        CanonicalName name,
        ShapeReference valueShape,
        string semantics) =>
        new(
            name,
            1,
            ShapeContract.For(valueShape),
            semantics,
            ConstraintEvaluatorDomain.TargetAuthority,
            ConstraintUnknownBehavior.Deny,
            ConstraintAccountingScope.NotQuantified,
            ConstraintEvolutionPolicy.ParallelCanonicalName);

    private static ShapeValue AreaValue(ShapeReference shape, bool includeExclusions) =>
        includeExclusions
            ? ShapeValue.Record(shape,
                ("region", ShapeValue.Text("north")),
                ("exclusions", ShapeValue.Text("restricted")))
            : ShapeValue.Record(shape, ("region", ShapeValue.Text("north")));

    private static ConstraintDecision Allow(Constraint constraint, ConstraintEvaluationContext _) =>
        ConstraintDecision.Allow(constraint.Name, "recognised and satisfied");

    private sealed record DomainFixture(AuthorityDomain Domain, ActorReference Holder, Capability Capability);
}
