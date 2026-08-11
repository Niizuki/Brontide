using System.Collections.Immutable;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>Neutral vectors PB-53 through PB-62: plan facts, observations, and realization parity.</summary>
public sealed class PortablePlanObservationAndParityTests
{
    // PB-53-PLAN-IS-FROZEN-AND-INSPECTABLE
    [Test]
    public async Task Every_declared_plan_fact_answers_by_name_without_invoking_the_provider()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        await using var host = await PortableTestHarness.DirectHostAsync(handler: handler);
        var plan = host.Plan;

        var expected = new[]
        {
            "planId", "contractVersion", "component", "provider", "operations", "shapes", "fragments",
            "satisfiedRequirements", "compactIdentifierAssignments", "hostEndpoint", "providerEndpoint",
            "selectedProvider", "selectionReason", "authorityPresentationMode", "trustBoundaryCrossed",
            "noCapabilityTransfer", "constraintPolicy", "authorityDecisionPoint", "chainConjunctionTier",
            "representation", "framing", "resourceFlavors", "acceptedResourceHandles", "resourceOwnership",
            "implicitCopyPermitted", "integrityAlgorithm", "maxConcurrentRequests", "orderingGuarantee",
            "realization", "crossedBoundaries", "limits", "replayProtectionDeclared", "replayWindow",
            "lifecycleFeatures", "terminalStates", "failureDomainsObservable", "exceptionTransportPermitted"
        };

        Assert.Multiple(() =>
        {
            Assert.That(plan.FactNames, Is.EquivalentTo(expected));
            foreach (var name in expected)
            {
                Assert.That(plan.Fact(name), Is.Not.Null, $"Plan fact '{name}' did not answer.");
            }

            Assert.That(plan.Fact("constraintPolicy"), Is.EqualTo("fail-closed"));
            Assert.That(plan.Fact("implicitCopyPermitted"), Is.EqualTo("false"));
            Assert.That(plan.Fact("exceptionTransportPermitted"), Is.EqualTo("false"));
            Assert.That(plan.Fact("chainConjunctionTier"), Is.EqualTo("carried"));
            Assert.That(plan.Fact("orderingGuarantee"), Is.EqualTo("none"));
            Assert.That(plan.Fact("terminalStates"), Is.EqualTo("terminated, failed"));
            Assert.That(handler.ProviderEffectCount, Is.Zero, "Inspection invokes no provider.");
        });
    }

    // PB-54-COMPACT-IDENTIFIERS-ARE-NOT-IDENTITY
    [Test]
    public async Task The_plan_and_the_observation_report_canonical_references_never_compact_ones()
    {
        await using var host = await PortableTestHarness.DirectHostAsync();
        var plan = host.Plan;

        Assert.Multiple(() =>
        {
            Assert.That(plan.CompactIdentifierAssignments, Is.Not.Empty);
            Assert.That(plan.Fact("operations"), Does.Contain("interchange.tests.cooling.set-enabled@1"));
            Assert.That(plan.Fact("operations"), Does.Not.Match(@"^\d+$"));
            foreach (var assignment in plan.CompactIdentifierAssignments)
            {
                Assert.That(assignment.CompactId.Value, Is.InRange(0, PortableCompactIdentifier.MaxValue));
                Assert.That(assignment.Reference, Does.Contain("@"));
            }
        });
    }

    // PB-55-OBSERVATION-COMPLETE-ON-SUCCESS
    [Test]
    public async Task A_successful_interaction_reports_every_normative_field()
    {
        await using var host = await PortableTestHarness.DirectHostAsync();
        var result = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableTestHarness.Permitted());
        var observation = result.Observation;

        Assert.Multiple(() =>
        {
            Assert.That(observation.TerminalStatus, Is.EqualTo(PortableTerminalStatus.Succeeded));
            Assert.That(observation.FailureDomain, Is.Null, "A success reports no failure domain.");
            Assert.That(observation.SelectedProvider, Is.EqualTo(CoolingPortableFixture.Provider));
            Assert.That(observation.SelectionReason, Is.Not.Empty);
            Assert.That(observation.NegotiatedOperations, Does.Contain(CoolingPortableFixture.SetEnabled));
            Assert.That(observation.NegotiatedContractVersion, Is.EqualTo(1));
            Assert.That(observation.Representation, Is.EqualTo(PortableRepresentations.PortableCborCore));
            Assert.That(observation.RetryCount, Is.Zero);
            Assert.That(observation.Interrupted, Is.False);
            Assert.That(observation.ProviderEffectCount, Is.EqualTo(1));
            Assert.That(observation.Timing.RequestElapsedMilliseconds, Is.GreaterThanOrEqualTo(0));
            Assert.That(
                observation.CorrelationMapping.HostNativeExecution.Value,
                Is.Not.EqualTo(observation.CorrelationMapping.ChannelId.Value));
        });
    }

    // PB8 B3 — an endpoint cannot fabricate a zero effect count when the peer's effect is unknowable.
    [Test]
    public async Task An_unobservable_provider_effect_uses_the_declared_unknown_form()
    {
        await using var host = await PortableTestHarness.DirectHostAsync();
        var succeeded = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableTestHarness.Permitted());

        var unknown = succeeded.Observation with
        {
            TerminalStatus = PortableTerminalStatus.ProcessFailure,
            FailureDomain = PortableFailureDomain.Transport,
            ProviderEffectCount = null
        };

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(unknown.RequireComplete);
            Assert.That(unknown.ParityProfile()["providerEffectCount"], Is.EqualTo("unknown"));
            Assert.Throws<InvalidOperationException>(() =>
                (unknown with { TerminalStatus = PortableTerminalStatus.Succeeded, FailureDomain = null }).RequireComplete());
        });
    }

    // PB-58-DIRECT-AND-PROCESS-PARITY-ON-SUCCESS
    [Test]
    public async Task Both_realizations_agree_on_every_field_of_the_parity_profile_for_a_success()
    {
        var direct = await RunAsync(direct: true, resources: []);
        var process = await RunAsync(direct: false, resources: []);

        Assert.Multiple(() =>
        {
            Assert.That(process.Profile, Is.EqualTo(direct.Profile));
            Assert.That(direct.Result.ResultClass, Is.EqualTo(PortableResultClass.OutcomeSucceeded));
            Assert.That(direct.Plan.Fact("realization"), Is.EqualTo("fixed-direct-call"));
            Assert.That(process.Plan.Fact("realization"), Is.EqualTo("negotiated-process"));
            Assert.That(direct.Plan.Fact("framing"), Is.EqualTo("direct-call"));
            Assert.That(process.Plan.Fact("framing"), Is.EqualTo("length-delimited"));
        });
    }

    [Test]
    public async Task The_two_plans_differ_only_in_realization_framing_and_crossed_boundaries()
    {
        var direct = await RunAsync(direct: true, resources: []);
        var process = await RunAsync(direct: false, resources: []);
        var permitted = new[] { "realization", "framing", "crossedBoundaries", "planId", "hostEndpoint" };

        var differing = direct.Plan.Facts
            .Where(fact => process.Plan.Facts[fact.Key] != fact.Value)
            .Select(fact => fact.Key)
            .ToImmutableArray();
        Assert.That(differing, Is.SubsetOf(permitted));
    }

    // PB-59-DIRECT-AND-PROCESS-PARITY-ON-DENIAL
    [Test]
    public async Task Both_realizations_make_the_same_no_frame_decision_for_a_local_denial()
    {
        var denial = PortableConstraint.AllOf(
            PortableConstraint.Atom(PortableTruth.Satisfied),
            PortableConstraint.Atom(PortableTruth.Unknown));

        await using var directHost = await PortableTestHarness.DirectHostAsync();
        var directResult = await directHost.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            denial);

        var (processHost, seam) = await PortableTestHarness.ProcessHostAsync();
        await using (seam)
        await using (processHost)
        {
            var processResult = await processHost.InvokeAsync(
                CoolingPortableFixture.SetEnabled,
                CoolingPortableFixture.CommandV1,
                CoolingPortableFixture.Command("primary", enabled: true),
                denial);

            Assert.Multiple(() =>
            {
                Assert.That(processResult.ParityProfile(), Is.EqualTo(directResult.ParityProfile()));
                Assert.That(directResult.FrameDecision, Is.EqualTo(PortableFrameDecision.None));
                Assert.That(processResult.FrameDecision, Is.EqualTo(PortableFrameDecision.None));
                Assert.That(processResult.Observation.ProviderEffectCount, Is.Zero);
            });
        }
    }

    // PB-60-COPY-ACCOUNTING-DIFFERS-WITHOUT-BREAKING-PARITY
    [Test]
    public async Task Copy_accounting_differs_by_realization_while_parity_still_holds()
    {
        var direct = await RunAsync(direct: true, resources: [PortableTestHarness.Blob()]);
        var process = await RunAsync(direct: false, resources: [PortableTestHarness.Blob()]);

        Assert.Multiple(() =>
        {
            Assert.That(direct.Result.Observation.CopyCount, Is.Zero);
            Assert.That(process.Result.Observation.CopyCount, Is.EqualTo(1));
            Assert.That(process.Profile, Is.EqualTo(direct.Profile), "Copy accounting is an excluded field.");
            Assert.That(
                process.Result.Observation.ReferencedResources[0].Flavor,
                Is.EqualTo(direct.Result.Observation.ReferencedResources[0].Flavor));
            Assert.That(
                process.Result.Observation.ReferencedResources[0].IntegrityVerified,
                Is.EqualTo(direct.Result.Observation.ReferencedResources[0].IntegrityVerified));
        });
    }

    [Test]
    public void A_refused_request_leaves_the_endpoint_failed_in_the_direct_realization_too()
    {
        // The lifecycle's declared transition on a protocol error is to the failed state. Applying
        // it only in the transport would leave the two realizations in different endpoint states
        // after the same refusal.
        var endpoint = PortableTestHarness.CoolingEndpoint(PortableRealization.FixedDirectCall);
        endpoint.Establish(CoolingPortableFixture.Contract, "host");
        endpoint.SignalReady();

        Assert.Throws<PortableFaultException>(() => endpoint.Request(
            PortableChannelRequestId.New(),
            PortableOperationReference.Parse("interchange.tests.cooling.set-disabled", 1),
            null,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            []));

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.State, Is.EqualTo(PortableLifecycleState.Failed));
            Assert.That(endpoint.State.IsTerminal(), Is.True);
        });
    }

    // PB-62-NO-SHARED-SEMANTIC-RUNTIME
    [Test]
    public void The_portable_layer_imports_no_other_stack_runtime_and_the_neutral_layer_stays_data_only()
    {
        var referenced = typeof(PortableBindingHost).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .ToImmutableArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                referenced.Where(name => name.StartsWith("Brontide.Minimal", StringComparison.Ordinal)),
                Is.Empty);
            Assert.That(referenced.Where(name => name.StartsWith("FSharp", StringComparison.Ordinal)), Is.Empty);

            var neutral = Directory.GetFiles(PortableTestHarness.NeutralRoot, "*", SearchOption.AllDirectories);
            Assert.That(neutral, Is.Not.Empty);
            Assert.That(
                neutral.Where(path => Path.GetExtension(path) is not (".json" or ".md")),
                Is.Empty,
                "The neutral layer carries data and documentation only.");
        });
    }

    private static async Task<(PortableInteractionResult Result, PortableBindingPlan Plan, ImmutableSortedDictionary<string, string> Profile)> RunAsync(
        bool direct,
        IReadOnlyList<PortableResource> resources)
    {
        if (direct)
        {
            await using var host = await PortableTestHarness.DirectHostAsync();
            var result = await host.InvokeAsync(
                CoolingPortableFixture.SetEnabled,
                CoolingPortableFixture.CommandV1,
                CoolingPortableFixture.Command("primary", enabled: true),
                PortableTestHarness.Permitted(),
                resources);
            return (result, host.Plan, result.ParityProfile());
        }

        var (processHost, seam) = await PortableTestHarness.ProcessHostAsync();
        await using (seam)
        await using (processHost)
        {
            var result = await processHost.InvokeAsync(
                CoolingPortableFixture.SetEnabled,
                CoolingPortableFixture.CommandV1,
                CoolingPortableFixture.Command("primary", enabled: true),
                PortableTestHarness.Permitted(),
                resources);
            return (result, processHost.Plan, result.ParityProfile());
        }
    }
}
