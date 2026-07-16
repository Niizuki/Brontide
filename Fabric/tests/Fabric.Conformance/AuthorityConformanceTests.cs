using Fabric.Core;

namespace Fabric.Conformance;

public sealed class AuthorityConformanceTests
{
    [Test]
    [SpecSection("11")]
    [SpecSection("29.2")]
    public async Task Delegation_adds_constraints_and_cannot_amplify()
    {
        ActorReference actorA = null!;
        ActorReference actorB = null!;
        ActorReference fan = null!;
        Capability root = null!;
        var stop = OperationReference.Parse("Fan.Stop");
        var setSpeed = OperationReference.Parse("Fan.SetSpeed");
        var effects = new List<string>();

        var domain = AuthorityDomain.Create("delegation", genesis =>
        {
            actorA = genesis.Actor("Actor A");
            actorB = genesis.Actor("Actor B");
            fan = genesis.Actor("Fan");
            genesis.Operation(stop, fan, ShapeContract.Unit, ShapeContract.Unit, "stop the fan",
                _ => OperationEffect.SucceededAsync(ShapeValue.Unit));
            genesis.Operation(setSpeed, fan, ShapeContract.Unit, ShapeContract.Unit, "set fan speed",
                _ => { effects.Add("set-speed"); return OperationEffect.SucceededAsync(ShapeValue.Unit); });
            root = genesis.Grant(actorA, fan, [stop, setSpeed]);
        });

        var derived = root.Delegate(actorB, new PermittedOperationsConstraint(stop));

        var stopResult = await domain.ExecuteAsync(actorB, stop, derived, ShapeValue.Unit);
        var speedResult = await domain.ExecuteAsync(actorB, setSpeed, derived, ShapeValue.Unit);

        Assert.That(stopResult.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
        Assert.That(speedResult.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
        Assert.That(effects, Is.Empty);
        Assert.That(derived.Parent, Is.SameAs(root));
        Assert.That(speedResult.Decisions.Any(decision =>
            !decision.Allowed && decision.ConstraintName == StandardConstraintNames.PermittedOperations), Is.True);
    }

    [Test]
    [SpecSection("10.1")]
    [SpecSection("13.5")]
    public async Task Unknown_constraint_denies_before_effect_and_is_recorded()
    {
        ActorReference actor = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var operation = OperationReference.Parse("Deployment.Approve");
        var effects = 0;

        var domain = AuthorityDomain.Create("fail-closed", genesis =>
        {
            actor = genesis.Actor("Actor");
            target = genesis.Actor("Target");
            genesis.Operation(operation, target, ShapeContract.Unit, ShapeContract.Unit, "approve",
                _ => { effects++; return OperationEffect.SucceededAsync(ShapeValue.Unit); });
            capability = genesis.Grant(actor, target, [operation],
                [new ValueConstraint(CanonicalName.Parse("Example:Unknown"), ShapeValue.Text("x"))]);
        });

        var result = await domain.ExecuteAsync(actor, operation, capability, ShapeValue.Unit);

        Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
        Assert.That(effects, Is.Zero);
        Assert.That(result.Outcome.Message, Does.Contain("unrecognised").IgnoreCase);
        Assert.That(domain.Provenance.Any(entry =>
            entry.Kind == ProvenanceKind.Execution && entry.Execution?.Id == result.Execution.Id), Is.True);
    }
}
