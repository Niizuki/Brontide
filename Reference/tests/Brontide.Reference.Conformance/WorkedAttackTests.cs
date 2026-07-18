using Brontide.Reference.Core;

namespace Brontide.Reference.Conformance;

public sealed class WorkedAttackTests
{
    [Test]
    [SpecSection("10.1")]
    [SpecSection("13.6")]
    [SpecSection("29.4")]
    public async Task Production_approval_is_denied_at_the_target_boundary()
    {
        ActorReference operations = null!;
        ActorReference buildAgent = null!;
        ActorReference plugin = null!;
        ActorReference deploymentTarget = null!;
        Capability stagingApproval = null!;
        var approve = OperationReference.Parse("Deployment.Approve");
        var environmentShape = ShapeReference.Parse("Deployment.Environment", 1);
        var requestShape = ShapeReference.Parse("Deployment.Request", 1);
        var effects = 0;

        var domain = AuthorityDomain.Create("worked-attack", genesis =>
        {
            operations = genesis.Actor("OperationsSystem");
            buildAgent = genesis.Actor("BuildAgent");
            plugin = genesis.Actor("PluginActor");
            deploymentTarget = genesis.Actor("DeploymentTarget");

            genesis.Shape(ShapeDefinition.Scalar<string>(environmentShape));
            genesis.Shape(ShapeDefinition.Record(requestShape, FragmentPolicy.Closed,
                RecordField.Required("deployment", BuiltInShapes.Text),
                RecordField.Required("environment", environmentShape)));

            var environmentConstraint = CanonicalName.Parse("Deployment:Environment");
            genesis.Constraint(environmentConstraint, ShapeContract.For(environmentShape), (constraint, context) =>
            {
                var allowed = constraint.Value.RequireScalar<string>();
                var actual = context.Input.RequireField("environment").RequireScalar<string>();
                return allowed == actual
                    ? ConstraintDecision.Allow(environmentConstraint, $"environment is {allowed}")
                    : ConstraintDecision.Deny(environmentConstraint, $"environment {actual} is outside {allowed}");
            });

            genesis.Operation(approve, deploymentTarget, ShapeContract.For(requestShape), ShapeContract.Unit,
                "approve a deployment", _ => { effects++; return OperationEffect.SucceededAsync(ShapeValue.Unit); });

            var deploymentGrant = genesis.Grant(operations, deploymentTarget, [approve]);
            stagingApproval = deploymentGrant.Delegate(buildAgent,
                new PermittedOperationsConstraint(approve),
                new ValueConstraint(environmentConstraint, ShapeValue.Scalar(environmentShape, "staging")));
        });

        var production = ShapeValue.Record(requestShape,
            ("deployment", ShapeValue.Text("#1234")),
            ("environment", ShapeValue.Scalar(environmentShape, "production")));

        var result = await domain.ExecuteOnBehalfAsync(
            buildAgent,
            plugin,
            approve,
            stagingApproval.AsOwnAuthority("BuildAgent approval policy"),
            production);

        Assert.That(result.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
        Assert.That(effects, Is.Zero);
        Assert.That(result.Outcome.Message, Does.Contain("production").IgnoreCase);
        Assert.That(result.Execution.Requester, Is.SameAs(plugin));
        Assert.That(result.Execution.Initiator, Is.SameAs(buildAgent));
        Assert.That(result.Execution.AuthorityPresentation.Reason, Is.EqualTo("BuildAgent approval policy"));
    }
}
