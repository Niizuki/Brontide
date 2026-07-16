using Fabric.Core;

namespace Fabric.Conformance;

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
}
