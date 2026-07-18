using Brontide.Reference.Core;

namespace Brontide.Reference.Conformance;

public sealed class OutcomeConformanceTests
{
    [Test]
    [SpecSection("13.1")]
    [SpecSection("14.2")]
    [SpecSection("16")]
    public async Task Successful_result_and_failure_details_use_independent_shapes()
    {
        ActorReference actor = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var success = OperationReference.Parse("Example.Succeed");
        var fail = OperationReference.Parse("Example.Fail");
        var resultShape = ShapeReference.Parse("Example.Result", 1);
        var detailsShape = ShapeReference.Parse("Example.FailureDetails", 1);
        var domain = AuthorityDomain.Create("outcomes", genesis =>
        {
            actor = genesis.Actor("Actor");
            target = genesis.Actor("Target");
            genesis.Shape(ShapeDefinition.Record(resultShape, FragmentPolicy.Closed,
                RecordField.Required("answer", BuiltInShapes.Signed64)));
            genesis.Shape(ShapeDefinition.Record(detailsShape, FragmentPolicy.Closed,
                RecordField.Required("code", BuiltInShapes.Text)));
            genesis.Operation(success, target, ShapeContract.Unit, ShapeContract.For(resultShape), "success",
                _ => OperationEffect.SucceededAsync(
                    ShapeValue.Record(resultShape, ("answer", ShapeValue.Signed64(42)))));
            genesis.Operation(fail, target, ShapeContract.Unit, ShapeContract.For(resultShape), "failure",
                _ => OperationEffect.FailedAsync(
                    ShapeContract.For(detailsShape),
                    ShapeValue.Record(detailsShape, ("code", ShapeValue.Text("E_TEST"))),
                    "expected failure"));
            capability = genesis.Grant(actor, target, [success, fail]);
        });

        var succeeded = await domain.ExecuteAsync(actor, success, capability, ShapeValue.Unit);
        var failed = await domain.ExecuteAsync(actor, fail, capability, ShapeValue.Unit);

        Assert.That(succeeded.Outcome.Result!.Reference, Is.EqualTo(resultShape));
        Assert.That(succeeded.Outcome.Details, Is.Null);
        Assert.That(failed.Outcome.Status, Is.EqualTo(OutcomeStatus.Failed));
        Assert.That(failed.Outcome.Result, Is.Null);
        Assert.That(failed.Outcome.DetailsShape!.Canonical, Is.EqualTo(detailsShape));
        Assert.That(failed.Outcome.Details!.Reference, Is.EqualTo(detailsShape));
    }

    [Test]
    [SpecSection("14.2")]
    [SpecSection("21.2")]
    [SpecSection("25")]
    public async Task Successful_macro_operation_can_create_an_activity_that_terminates_later()
    {
        ActorReference worker = null!;
        ActorReference platform = null!;
        Capability capability = null!;
        ActivityReference activity = default;
        Outcome terminalActivityOutcome = null!;
        var start = OperationReference.Parse("Audit.Start");
        var complete = OperationReference.Parse("Audit.Complete");
        var domain = AuthorityDomain.Create("macro-outcome", genesis =>
        {
            worker = genesis.Actor("AuditWorker");
            platform = genesis.Actor("AuditPlatform");
            genesis.Operation(
                start,
                platform,
                ShapeContract.Unit,
                ShapeContract.For(BuiltInShapes.Activity),
                "create a long-lived audit activity",
                context =>
                {
                    activity = context.CreateActivity(CanonicalName.Parse("Audit.Activity"));
                    return OperationEffect.SucceededAsync(
                        ShapeValue.Record(
                            BuiltInShapes.Activity,
                            ("id", ShapeValue.Text(activity.Value.ToString("N"))),
                            ("kind", ShapeValue.Text(activity.Kind.ToString()))));
                });
            genesis.Operation(
                complete,
                platform,
                ShapeContract.Unit,
                ShapeContract.Unit,
                "complete the audit activity",
                context =>
                {
                    terminalActivityOutcome = context.CompleteActivity(activity);
                    return OperationEffect.SucceededAsync(ShapeValue.Unit);
                });
            capability = genesis.Grant(worker, platform, [start, complete]);
        });

        var started = await domain.ExecuteAsync(worker, start, capability, ShapeValue.Unit);
        var completed = await domain.ExecuteAsync(worker, complete, capability, ShapeValue.Unit);

        Assert.Multiple(() =>
        {
            Assert.That(started.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(started.Outcome.Result!.RequireField("id").RequireScalar<string>(),
                Is.EqualTo(activity.Value.ToString("N")));
            Assert.That(completed.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(terminalActivityOutcome.Status, Is.EqualTo(OutcomeStatus.Completed));
            Assert.That(terminalActivityOutcome.TerminalFor, Is.EqualTo(TerminalReference.For(activity)));
        });
    }
}
