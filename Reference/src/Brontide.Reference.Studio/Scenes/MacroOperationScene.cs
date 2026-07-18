using System.Collections.Immutable;
using Brontide.Reference.Core;

namespace Brontide.Reference.Studio.Scenes;

public sealed record MacroOperationResult(
    AuthorityDomain Domain,
    Capability WorkerCapability,
    ExecutionResult StartExecution,
    ExecutionResult CompleteExecution,
    ActivityReference Activity,
    Outcome ActivityOutcome,
    ImmutableArray<string> Transcript);

public static class MacroOperationScene
{
    public static async ValueTask<MacroOperationResult> RunAsync()
    {
        ActorReference operationsSystem = null!;
        ActorReference coordinator = null!;
        ActorReference worker = null!;
        ActorReference auditPlatform = null!;
        Capability workerCapability = null!;
        ActivityReference activity = default;
        Outcome activityOutcome = null!;
        var start = OperationReference.Parse("Audit.Start");
        var complete = OperationReference.Parse("Audit.Complete");
        var request = ShapeReference.Parse("Audit.Start.Request", 1);
        var organisationConstraint = CanonicalName.Parse("Audit:Organisation");
        var transcript = ImmutableArray.CreateBuilder<string>();
        var domain = AuthorityDomain.Create("macro-operation", genesis =>
        {
            operationsSystem = genesis.Actor("OperationsSystem");
            coordinator = genesis.Actor("AuditCoordinator");
            worker = genesis.Actor("AuditWorker");
            auditPlatform = genesis.Actor("AuditPlatform");
            genesis.Shape(ShapeDefinition.Record(request, FragmentPolicy.Closed,
                RecordField.Required("organisation", BuiltInShapes.Text),
                RecordField.Required("scope", BuiltInShapes.Text)));
            genesis.Constraint(organisationConstraint, ShapeContract.For(BuiltInShapes.Text), (constraint, context) =>
            {
                if (context.Operation == complete)
                {
                    return ConstraintDecision.Allow(organisationConstraint, "completion retains the activity's bound scope");
                }

                var expected = constraint.Value.RequireScalar<string>();
                var actual = context.Input.RequireField("organisation").RequireScalar<string>();
                return actual == expected
                    ? ConstraintDecision.Allow(organisationConstraint, "organisation matches")
                    : ConstraintDecision.Deny(organisationConstraint, $"organisation {actual} is outside {expected}");
            });
            genesis.Operation(
                start,
                auditPlatform,
                ShapeContract.For(request),
                ShapeContract.For(BuiltInShapes.Activity),
                "create one long-lived audit activity",
                context =>
                {
                    activity = context.CreateActivity(CanonicalName.Parse("Audit.Activity"));
                    return OperationEffect.SucceededAsync(
                        ShapeValue.Record(
                            BuiltInShapes.Activity,
                            ("id", ShapeValue.Text(activity.Value.ToString("N"))),
                            ("kind", ShapeValue.Text(activity.Kind.ToString()))),
                        "audit activity created");
                });
            genesis.Operation(
                complete,
                auditPlatform,
                ShapeContract.Unit,
                ShapeContract.Unit,
                "terminate the created audit activity",
                context =>
                {
                    activityOutcome = context.CompleteActivity(activity, message: "audit activity completed");
                    return OperationEffect.SucceededAsync(ShapeValue.Unit, "completion recorded");
                });
            var root = genesis.Grant(operationsSystem, auditPlatform, [start, complete]);
            var coordinatorCapability = root.Delegate(
                coordinator,
                new ValueConstraint(organisationConstraint, ShapeValue.Text("Erste")));
            workerCapability = coordinatorCapability.Delegate(
                worker,
                new PermittedOperationsConstraint(start, complete));
        });

        var started = await domain.ExecuteAsync(
            worker,
            start,
            workerCapability,
            ShapeValue.Record(
                request,
                ("organisation", ShapeValue.Text("Erste")),
                ("scope", ShapeValue.Text("FinancialControls"))));
        transcript.Add($"Audit.Start: {started.Outcome.Status}; created {activity}");
        var completed = await domain.ExecuteAsync(
            worker,
            complete,
            workerCapability,
            ShapeValue.Unit);
        transcript.Add($"Audit.Complete: {completed.Outcome.Status}; activity terminal {activityOutcome.Status}");

        return new MacroOperationResult(
            domain,
            workerCapability,
            started,
            completed,
            activity,
            activityOutcome,
            transcript.ToImmutable());
    }
}
