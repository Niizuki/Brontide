using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>Neutral vectors PB-18 through PB-32: local authority and referenced resources.</summary>
public sealed class PortableAuthorityAndResourceTests
{
    private static PortableFaultException Fault(Action action) =>
        Assert.Throws<PortableFaultException>(action)!;

    // PB-18-LOCAL-DENIAL-EMITS-NO-FRAME and PB-56-OBSERVATION-COMPLETE-ON-DENIAL
    [Test]
    public async Task A_local_denial_emits_no_frame_and_still_produces_a_complete_observation()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        await using var host = await PortableTestHarness.DirectHostAsync(handler: handler);
        var result = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableConstraint.AllOf(
                PortableConstraint.Atom(PortableTruth.Satisfied),
                PortableConstraint.Atom(PortableTruth.Unsatisfied)));

        Assert.Multiple(() =>
        {
            Assert.That(result.FrameDecision, Is.EqualTo(PortableFrameDecision.None));
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.Denial));
            Assert.That(result.Observation.TerminalStatus, Is.EqualTo(PortableTerminalStatus.Denied));
            Assert.That(result.Observation.AuthorityDecision, Is.EqualTo(PortableAuthorityDecision.Denied));
            Assert.That(
                result.Observation.AuthorityDecisionPoint,
                Is.EqualTo(PortableAuthorityDecisionPoint.HostLocal));
            Assert.That(result.Observation.ProviderEffectCount, Is.Zero);
            Assert.That(result.Observation.CopyCount, Is.Zero);
            Assert.That(result.Observation.CrossedBoundaries, Is.EqualTo(new[] { "none" }).AsCollection);
            Assert.That(result.Observation.Interrupted, Is.False);
            Assert.That(result.Observation.RetryCount, Is.Zero);
            Assert.That(handler.ProviderEffectCount, Is.Zero);
        });
    }

    // PB-19-AUTHORITY-VALUE-NOT-PROJECTED
    [Test]
    public async Task An_unrecognized_Constraint_value_version_denies_rather_than_projecting()
    {
        await using var host = await PortableTestHarness.DirectHostAsync();

        // The evaluator recognizes Constraint value version 1 only. Projecting the version 2 value
        // would discard its narrowing semantics and widen authority, so the atom stays Unknown.
        var result = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableConstraint.AllOf(
                PortableConstraint.Atom(PortableTruth.Satisfied),
                PortableConstraint.Atom(PortableTruth.Unknown)));

        Assert.Multiple(() =>
        {
            Assert.That(result.FrameDecision, Is.EqualTo(PortableFrameDecision.None));
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.Denial));
            Assert.That(result.Observation.AuthorityDecision, Is.EqualTo(PortableAuthorityDecision.Unknown));
            Assert.That(result.Observation.ProviderEffectCount, Is.Zero);
        });
    }

    // PB-20-STRONG-KLEENE-ANYOF-PERMITS and PB-21-STRONG-KLEENE-ALLOF-DENIES
    [Test]
    public void Strong_Kleene_evaluation_permits_only_a_true_expression()
    {
        var satisfied = PortableConstraint.Atom(PortableTruth.Satisfied);
        var unknown = PortableConstraint.Atom(PortableTruth.Unknown);
        var unsatisfied = PortableConstraint.Atom(PortableTruth.Unsatisfied);

        Assert.Multiple(() =>
        {
            Assert.That(PortableConstraint.AnyOf(satisfied, unknown).Evaluate(), Is.EqualTo(PortableTruth.Satisfied));
            Assert.That(PortableConstraint.AllOf(satisfied, unknown).Evaluate(), Is.EqualTo(PortableTruth.Unknown));
            Assert.That(PortableConstraint.AnyOf(unsatisfied, unknown).Evaluate(), Is.EqualTo(PortableTruth.Unknown));
            Assert.That(PortableConstraint.AllOf(satisfied, satisfied).Evaluate(), Is.EqualTo(PortableTruth.Satisfied));
            Assert.That(PortableConstraint.Not(unknown).Evaluate(), Is.EqualTo(PortableTruth.Unknown));
        });
    }

    [Test]
    public async Task An_AnyOf_expression_containing_a_satisfied_atom_may_emit()
    {
        await using var host = await PortableTestHarness.DirectHostAsync();
        var decision = host.PrepareRequest(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            PortableConstraint.AnyOf(
                PortableConstraint.Atom(PortableTruth.Satisfied),
                PortableConstraint.Atom(PortableTruth.Unknown)));

        Assert.Multiple(() =>
        {
            Assert.That(decision.FrameDecision, Is.EqualTo(PortableFrameDecision.Emit));
            Assert.That(decision.ResultClass, Is.EqualTo(PortableResultClass.Request));
            Assert.That(decision.DenialObservation, Is.Null);
        });
    }

    // PB-22-CAPABILITY-IN-BODY-ACROSS-TRUST
    [Test]
    public async Task A_Capability_in_a_body_across_a_trust_boundary_fails_closed()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        await using var host = await PortableTestHarness.DirectHostAsync(handler: handler);
        var result = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true)
                .WithField("capability", new PortableTextValue("cooling.write")),
            PortableTestHarness.Permitted());

        Assert.Multiple(() =>
        {
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.ProtocolError));
            Assert.That(result.Category, Is.EqualTo(PortableProtocolCategory.InvalidAuthorityPresentation));
            Assert.That(handler.ProviderEffectCount, Is.Zero);
        });
    }

    // PB-23-MISSING-NO-CAPABILITY-TRANSFER-DECLARATION
    [Test]
    public void A_trust_crossing_contract_without_the_declaration_does_not_default_to_permissive()
    {
        var permissive = CoolingPortableFixture.Contract with
        {
            Authority = CoolingPortableFixture.Contract.Authority with { NoCapabilityTransfer = false }
        };

        Assert.That(
            Fault(() => PortableNegotiation.Negotiate(
                permissive,
                permissive,
                PortableRealization.NegotiatedProcess,
                "host",
                "provider",
                "test")).Category,
            Is.EqualTo(PortableProtocolCategory.InvalidAuthorityPresentation));
    }

    // PB-24-MISSING-REQUIRED-FRAGMENT
    [Test]
    public async Task A_missing_required_Fragment_is_refused_before_provider_activation()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        await using var host = await PortableTestHarness.DirectHostAsync(handler: handler);
        var result = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true, requesterLabel: null),
            PortableTestHarness.Permitted());

        Assert.Multiple(() =>
        {
            Assert.That(result.Category, Is.EqualTo(PortableProtocolCategory.InvalidPayload));
            Assert.That(handler.ProviderEffectCount, Is.Zero, "Attribution is never inferred from delivery.");
        });
    }

    // PB-25-COPIED-IMMUTABLE-BLOB-ACCEPTED
    [Test]
    public async Task A_copied_immutable_blob_is_accepted_and_reports_its_ownership()
    {
        await using var host = await PortableTestHarness.DirectHostAsync();
        var result = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableTestHarness.Permitted(),
            [PortableTestHarness.Blob()]);

        Assert.Multiple(() =>
        {
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.OutcomeSucceeded));
            Assert.That(result.Observation.ReferencedResources, Has.Length.EqualTo(1));
            Assert.That(result.Observation.ReferencedResources[0].Ownership, Is.EqualTo("copied"));
            Assert.That(result.Observation.ReferencedResources[0].IntegrityVerified, Is.True);
            Assert.That(result.Observation.CopyCount, Is.Zero, "A direct call materializes no copy.");
        });
    }

    // PB-26-RESOURCE-INTEGRITY-MISMATCH
    [Test]
    public void A_content_hash_that_does_not_verify_is_refused_before_any_provider_effect()
    {
        var tampered = new PortableCopiedBlobResource(
            "profile",
            System.Text.Encoding.UTF8.GetBytes("tampered"),
            PortableCopiedBlobResource.HashOf(System.Text.Encoding.UTF8.GetBytes("original")));

        Assert.That(
            Fault(() => PortableResourceCodec.Admit(
                tampered,
                [PortableResourceFlavors.CopiedImmutableBlob],
                [],
                PortableLimits.Declared)).Category,
            Is.EqualTo(PortableProtocolCategory.InvalidPayload));
    }

    // PB-27-ADDRESSING-HANDLE-ACCEPTED
    [Test]
    public async Task An_addressing_only_handle_inside_the_accept_list_crosses_no_octets()
    {
        await using var host = await PortableTestHarness.DirectHostAsync(
            CatalogPortableFixture.Contract,
            new CatalogPortableHandler());
        var result = await host.InvokeAsync(
            CatalogPortableFixture.Upsert,
            CatalogPortableFixture.UpsertCommand,
            CatalogPortableFixture.UpsertCommandValue(CatalogPortableFixture.ItemValue("a", "Alpha", "one")),
            PortableTestHarness.Permitted(),
            [CatalogPortableFixture.Handle()]);

        Assert.Multiple(() =>
        {
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.OutcomeSucceeded));
            Assert.That(result.Observation.ReferencedResources[0].Ownership, Is.EqualTo("provider-retained"));
            Assert.That(result.Observation.CopyCount, Is.Zero);
        });
    }

    // PB-28-RESOURCE-SCOPE-REFUSED
    [Test]
    public void A_handle_outside_the_accept_list_is_a_payload_decision_with_a_non_normative_local_code()
    {
        var fault = Fault(() => PortableResourceCodec.Admit(
            CatalogPortableFixture.Handle("catalog-provider", "secondary"),
            [PortableResourceFlavors.AddressingOnlyHandle],
            [CatalogPortableFixture.AcceptedHandle],
            PortableLimits.Declared));

        Assert.Multiple(() =>
        {
            Assert.That(fault.Category, Is.EqualTo(PortableProtocolCategory.InvalidPayload));
            Assert.That(fault.LocalCode, Is.EqualTo("resource-refused"));
            Assert.That(
                fault.Category,
                Is.Not.EqualTo(PortableProtocolCategory.InvalidAuthorityPresentation),
                "A handle carries no authority, so refusing one is not an authority decision.");
        });
    }

    // PB-29-UNSUPPORTED-RESOURCE-FLAVOR
    [Test]
    public void A_zero_one_non_goal_flavor_fails_negotiation_with_no_silent_downgrade()
    {
        foreach (var nonGoal in PortableResourceFlavors.NonGoals)
        {
            var contract = CoolingPortableFixture.Contract with
            {
                Representation = CoolingPortableFixture.Contract.Representation with { ResourceFlavors = [nonGoal] }
            };

            Assert.That(
                Fault(() => PortableNegotiation.Negotiate(
                    contract,
                    contract,
                    PortableRealization.NegotiatedProcess,
                    "host",
                    "provider",
                    "test")).Category,
                Is.EqualTo(PortableProtocolCategory.UnsupportedContract),
                $"Flavor '{nonGoal}' must fail closed.");
        }
    }

    // PB-30-FORBIDDEN-IMPLICIT-COPY
    [Test]
    public void Resource_octets_alongside_a_handle_are_refused_rather_than_silently_materialized()
    {
        var frame = CborMap.Of(
        [
            ("flavor", new CborText(PortableResourceFlavors.AddressingOnlyHandle)),
            ("name", new CborText("catalog")),
            ("provider", new CborText("catalog-provider")),
            ("id", new CborText("primary")),
            ("content", new CborBytes(new byte[] { 1, 2, 3 }))
        ]);

        var fault = Fault(() => PortableResourceCodec.Decode(
            frame,
            [PortableResourceFlavors.AddressingOnlyHandle],
            [CatalogPortableFixture.AcceptedHandle],
            PortableLimits.Declared));

        Assert.Multiple(() =>
        {
            Assert.That(fault.Category, Is.EqualTo(PortableProtocolCategory.InvalidPayload));
            Assert.That(fault.LocalCode, Is.EqualTo("forbidden-implicit-copy"));
        });
    }

    // PB-31-RESOURCE-EXCEEDS-DECLARED-BOUND
    [Test]
    public void A_resource_beyond_the_declared_bound_is_refused_before_uncontrolled_work()
    {
        var oversized = PortableCopiedBlobResource.Create(
            "profile",
            new byte[PortableLimits.Declared.MaxResourceBytes + 1]);

        Assert.That(
            Fault(() => PortableResourceCodec.Admit(
                oversized,
                [PortableResourceFlavors.CopiedImmutableBlob],
                [],
                PortableLimits.Declared)).Category,
            Is.EqualTo(PortableProtocolCategory.LimitExceeded));
    }

    // PB-32-RELEASE-SIGNAL-FOR-COPIED-FLAVOR
    [Test]
    public void A_release_signal_for_a_flavor_that_defines_none_is_not_a_harmless_no_op()
    {
        var blob = PortableTestHarness.Blob();
        var frame = CborMap.Of(
        [
            ("flavor", new CborText(PortableResourceFlavors.CopiedImmutableBlob)),
            ("name", new CborText(blob.Name)),
            ("content", new CborBytes(blob.Content)),
            ("integrity", new CborText(blob.Integrity)),
            ("release", new CborBoolean(true))
        ]);

        Assert.That(
            Fault(() => PortableResourceCodec.Decode(
                frame,
                [PortableResourceFlavors.CopiedImmutableBlob],
                [],
                PortableLimits.Declared)).Category,
            Is.EqualTo(PortableProtocolCategory.StateViolation));
    }
}
