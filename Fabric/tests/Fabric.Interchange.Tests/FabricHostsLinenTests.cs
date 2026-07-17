using Fabric.Core;
using Fabric.Experimental.Binding;
using Fabric.Experimental.Enrichment;

namespace Fabric.Interchange.Tests;

[Category("CrossProcess")]
public sealed class FabricHostsLinenTests
{
    private static ProviderLaunch LinenProvider(params string[] arguments)
    {
        var path = Environment.GetEnvironmentVariable("ATLAS_LINEN_PROVIDER");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Assert.Ignore("ATLAS_LINEN_PROVIDER does not name a built Linen provider endpoint.");
        }

        return new ProviderLaunch(path!, string.Join(' ', arguments));
    }

    [Test]
    public async Task Compatible_activation_success_forwarding_provenance_and_host_enrichment_are_visible()
    {
        var host = new FabricCoolingBindingHost(LinenProvider(), TimeProvider.System);
        var provider = EnrichmentProvider.Project(
            CanonicalName.Parse("interchange.tests.fabric-host-context"),
            InterchangeCoolingContract.Operation,
            InterchangeCoolingContract.HostContext,
            new EnrichmentSourceRequirement("requester", ShapeContract.For(BuiltInShapes.Text)),
            "requesterLabel",
            value => value);
        var enrichment = new TargetedEnrichmentComposition(
            "Fabric interchange host",
            host.Domain.Shapes,
            [provider]);
        var baseInput = InterchangeCoolingContract.Command(
            "primary",
            enabled: true,
            forwardingNote: "preserve exactly");
        var enriched = enrichment.Resolve(
            InterchangeCoolingContract.Operation,
            InterchangeCoolingContract.HostContext,
            baseInput,
            [AvailableValue.Direct("requester", ShapeValue.Text("fabric-requester"), "host-local actor label")]);

        var result = await host.ExecuteAsync(host.AuthorizedActor, host.AuthorizedCapability, enriched.Input);
        var native = (RecordShapeValue)result.Execution.Outcome.Result!;
        var forwarded = (RecordShapeValue)result.ForwardedInput!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Execution.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
            Assert.That(native.RequireField("coolingEnabled").RequireScalar<bool>(), Is.True);
            Assert.That(result.Observation.SelectedProvider, Is.EqualTo("linen-fsharp-provider"));
            Assert.That(result.Observation.HostAuthorityDecision, Is.EqualTo("allowed"));
            Assert.That(result.Observation.CrossedBoundaries, Does.Contain("process"));
            Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo(1));
            Assert.That(result.Observation.RequestId.Value,
                Is.Not.EqualTo(result.Observation.BindingExecutionId.Value));
            Assert.That(result.Observation.HostExecutionId.Value,
                Is.Not.EqualTo(result.Observation.BindingExecutionId.Value));
            Assert.That(forwarded.Fragments.Keys,
                Does.Contain(InterchangeCoolingContract.OptionalForwardingNote));
            Assert.That(enriched.Trace.Sources["requester"], Is.EqualTo("host-local actor label"));
        });
    }

    [Test]
    public async Task Authority_unknown_constraint_and_missing_required_fragment_stop_before_provider_effect()
    {
        var deniedHost = new FabricCoolingBindingHost(LinenProvider(), TimeProvider.System);
        var denied = await deniedHost.ExecuteAsync(
            deniedHost.DeniedActor,
            deniedHost.AuthorizedCapability,
            InterchangeCoolingContract.Command("primary", true, requesterLabel: "denied"));

        var unknownHost = new FabricCoolingBindingHost(LinenProvider(), TimeProvider.System);
        var unknown = await unknownHost.ExecuteAsync(
            unknownHost.UnknownConstraintActor,
            unknownHost.UnknownConstraintCapability,
            InterchangeCoolingContract.Command("primary", true, requesterLabel: "unknown"));

        var fragmentHost = new FabricCoolingBindingHost(LinenProvider(), TimeProvider.System);
        var missing = await fragmentHost.ExecuteAsync(
            fragmentHost.AuthorizedActor,
            fragmentHost.AuthorizedCapability,
            InterchangeCoolingContract.Command("primary", true));

        Assert.Multiple(() =>
        {
            Assert.That(denied.Execution.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(unknown.Execution.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(missing.Execution.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
            Assert.That(deniedHost.ProviderStarts, Is.Zero);
            Assert.That(unknownHost.ProviderStarts, Is.Zero);
            Assert.That(fragmentHost.ProviderStarts, Is.Zero);
            Assert.That(unknown.Execution.Outcome.Message, Does.Contain("unrecognised by target"));
            Assert.That(missing.Execution.Outcome.Message, Does.Contain("required Fragment").IgnoreCase);
        });
    }

    [Test]
    public async Task Semantic_failure_crosses_as_shaped_failed_Outcome_without_exception_transport()
    {
        var host = new FabricCoolingBindingHost(LinenProvider(), TimeProvider.System);
        var result = await host.ExecuteAsync(
            host.AuthorizedActor,
            host.AuthorizedCapability,
            InterchangeCoolingContract.Command(
                "primary",
                true,
                failureMode: "semantic",
                requesterLabel: "fabric-requester"));

        var details = (RecordShapeValue)result.Execution.Outcome.Details!;
        Assert.Multiple(() =>
        {
            Assert.That(result.Execution.Outcome.Status, Is.EqualTo(OutcomeStatus.Failed));
            Assert.That(details.RequireField("code").RequireScalar<string>(), Is.EqualTo("requested-failure"));
            Assert.That(result.Observation.ProviderEffectCount, Is.Zero);
            Assert.That(result.Execution.Outcome.Message, Does.Not.Contain("Exception"));
        });
    }

    [Test]
    public async Task Incompatible_manifests_fail_before_activation_and_name_missing_Operations_and_Shapes()
    {
        var shapeHost = new FabricCoolingBindingHost(
            LinenProvider(),
            TimeProvider.System,
            manifest => manifest with
            {
                Shapes = manifest.Shapes
                    .Where(shape => shape.Reference.Name != InterchangeCoolingContract.ResultShape.Name.Value)
                    .ToImmutableArray()
            });
        var missingShape = await shapeHost.ExecuteAsync(
            shapeHost.AuthorizedActor,
            shapeHost.AuthorizedCapability,
            InterchangeCoolingContract.Command("primary", true, requesterLabel: "fabric-requester"));

        var operationHost = new FabricCoolingBindingHost(
            LinenProvider(),
            TimeProvider.System,
            manifest => manifest with { Operations = [] });
        var missingOperation = await operationHost.ExecuteAsync(
            operationHost.AuthorizedActor,
            operationHost.AuthorizedCapability,
            InterchangeCoolingContract.Command("primary", true, requesterLabel: "fabric-requester"));

        Assert.That(missingShape.Execution.Outcome.Status, Is.EqualTo(OutcomeStatus.Failed));
        Assert.That(missingShape.Observation.FailureDomain, Is.EqualTo("binding-negotiation"));
        Assert.That(missingShape.Execution.Outcome.Message, Does.Contain("Shape"));
        Assert.That(missingShape.Observation.ProviderEffectCount, Is.Null);
        Assert.That(missingOperation.Execution.Outcome.Status, Is.EqualTo(OutcomeStatus.Failed));
        Assert.That(missingOperation.Observation.FailureDomain, Is.EqualTo("binding-negotiation"));
        Assert.That(missingOperation.Execution.Outcome.Message, Does.Contain("Operation"));
        Assert.That(missingOperation.Observation.ProviderEffectCount, Is.Null);
    }

    [Test]
    public async Task Protocol_and_provider_process_failures_are_explicit_and_never_fabricate_success()
    {
        var protocolHost = new FabricCoolingBindingHost(
            LinenProvider("--reject-protocol"),
            TimeProvider.System);
        var protocol = await protocolHost.ExecuteAsync(
            protocolHost.AuthorizedActor,
            protocolHost.AuthorizedCapability,
            InterchangeCoolingContract.Command("primary", true, requesterLabel: "fabric-requester"));

        var crashHost = new FabricCoolingBindingHost(
            LinenProvider("--crash-after-activation"),
            TimeProvider.System);
        var crashed = await crashHost.ExecuteAsync(
            crashHost.AuthorizedActor,
            crashHost.AuthorizedCapability,
            InterchangeCoolingContract.Command("primary", true, requesterLabel: "fabric-requester"));

        Assert.Multiple(() =>
        {
            Assert.That(protocol.Execution.Outcome.Status, Is.EqualTo(OutcomeStatus.Failed));
            Assert.That(protocol.Observation.FailureDomain, Is.EqualTo("binding-negotiation"));
            Assert.That(crashed.Execution.Outcome.Status, Is.EqualTo(OutcomeStatus.Failed));
            Assert.That(crashed.Observation.ProviderProcessFailure, Is.True);
            Assert.That(crashed.Observation.Interrupted, Is.True);
            Assert.That(crashed.Observation.RetryCount, Is.Zero);
            Assert.That(crashed.Observation.Fallback, Is.EqualTo("none"));
        });
    }
}
