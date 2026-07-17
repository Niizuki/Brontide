using Fabric.Core;

namespace Fabric.Studio.Scenes;

public sealed record WorkedAttackStudioResult(
    bool Weakened,
    ExecutionResult Execution,
    int Effects,
    AuthorityDomain Domain);

public static class WorkedAttackStudioScene
{
    public static async ValueTask<WorkedAttackStudioResult> RunAsync(bool weakened)
    {
        ActorReference operations = null!;
        ActorReference buildAgent = null!;
        ActorReference plugin = null!;
        ActorReference target = null!;
        Capability stagingApproval = null!;
        var effects = 0;
        var approve = OperationReference.Parse("Deployment.Approve");
        var environment = ShapeReference.Parse("Deployment.Environment", 1);
        var request = ShapeReference.Parse("Deployment.Request", 1);
        var constraintName = CanonicalName.Parse("Deployment:Environment");
        var domain = AuthorityDomain.Create("Studio worked attack", genesis =>
        {
            operations = genesis.Actor("OperationsSystem");
            buildAgent = genesis.Actor("BuildAgent");
            plugin = genesis.Actor("PluginActor");
            target = genesis.Actor("DeploymentTarget");
            genesis.Shape(ShapeDefinition.Scalar<string>(environment));
            genesis.Shape(ShapeDefinition.Record(request, FragmentPolicy.Closed,
                RecordField.Required("deployment", BuiltInShapes.Text),
                RecordField.Required("environment", environment)));
            genesis.Constraint(constraintName, ShapeContract.For(environment), (constraint, context) =>
            {
                var allowed = constraint.Value.RequireScalar<string>();
                var actual = context.Input.RequireField("environment").RequireScalar<string>();
                return allowed == actual
                    ? ConstraintDecision.Allow(constraintName, "environment matches")
                    : ConstraintDecision.Deny(constraintName, $"environment {actual} is outside {allowed}");
            });
            genesis.Operation(approve, target, ShapeContract.For(request), ShapeContract.Unit, "approve deployment",
                _ => { effects++; return OperationEffect.SucceededAsync(ShapeValue.Unit); });
            var root = genesis.Grant(operations, target, [approve]);
            stagingApproval = weakened
                ? root.Delegate(buildAgent, new PermittedOperationsConstraint(approve))
                : root.Delegate(
                    buildAgent,
                    new PermittedOperationsConstraint(approve),
                    new ValueConstraint(
                        constraintName,
                        ShapeValue.Scalar(environment, "staging")));
        });
        var input = ShapeValue.Record(
            request,
            ("deployment", ShapeValue.Text("#1234")),
            ("environment", ShapeValue.Scalar(environment, "production")));
        var execution = await domain.ExecuteOnBehalfAsync(
            buildAgent,
            plugin,
            approve,
            stagingApproval.AsOwnAuthority("BuildAgent approval policy"),
            input);
        return new WorkedAttackStudioResult(weakened, execution, effects, domain);
    }
}
