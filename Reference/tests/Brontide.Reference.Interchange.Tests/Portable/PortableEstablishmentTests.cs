using System.Collections.Immutable;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>Neutral vectors PB-01 through PB-17: contract establishment and the Shape floor.</summary>
public sealed class PortableEstablishmentTests
{
    private static readonly PortableShapeCatalog Catalog =
        PortableShapeCatalog.FromContract(CoolingPortableFixture.Contract);

    private static PortableBindingPlan Negotiate(
        PortableContractDocument? required = null,
        PortableContractDocument? offered = null) =>
        PortableNegotiation.Negotiate(
            required ?? CoolingPortableFixture.Contract,
            offered ?? CoolingPortableFixture.Contract,
            PortableRealization.FixedDirectCall,
            "host",
            "provider",
            "test");

    private static CborMap EncodedContract() =>
        (CborMap)PortableContractCodec.Encode(CoolingPortableFixture.Contract);

    private static PortableProtocolCategory CategoryOf(Action action) =>
        Assert.Throws<PortableFaultException>(action)!.Category;

    // PB-01-EXACT-ESTABLISHMENT
    [Test]
    public void Exact_establishment_freezes_a_plan_without_a_provider_effect()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        var endpoint = PortableTestHarness.CoolingEndpoint(PortableRealization.FixedDirectCall, handler);
        var accepted = endpoint.Establish(CoolingPortableFixture.Contract, "host");

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Plan, Is.Not.Null);
            Assert.That(endpoint.Plan!.Operations, Does.Contain(CoolingPortableFixture.SetEnabled));
            Assert.That(accepted.CompactIdentifiers, Is.Not.Empty);
            Assert.That(handler.ProviderEffectCount, Is.Zero);
            Assert.That(endpoint.State, Is.EqualTo(PortableLifecycleState.Established));
        });
    }

    // PB-02-CONTRACT-VERSION-SKEW
    [Test]
    public void A_contract_version_the_endpoint_does_not_recognize_fails_closed()
    {
        var skewed = EncodedContract().With("contractVersion", new CborInteger(2));
        Assert.That(
            CategoryOf(() => PortableContractCodec.Decode(skewed)),
            Is.EqualTo(PortableProtocolCategory.UnsupportedVersion));
    }

    // PB-03-REQUIRED-REQUIREMENT-UNMET
    [Test]
    public void A_required_requirement_without_a_matching_provision_fails_closed()
    {
        Assert.That(
            CategoryOf(() => Negotiate(offered: CoolingPortableFixture.WithoutProfileProvision())),
            Is.EqualTo(PortableProtocolCategory.UnsupportedContract));
    }

    // PB-04-OPPOSED-REQUIREMENT-OFFERED
    [Test]
    public void An_opposed_requirement_that_is_offered_is_refused_rather_than_ignored()
    {
        Assert.That(
            CategoryOf(() => Negotiate(offered: CoolingPortableFixture.WithStreamingProvision())),
            Is.EqualTo(PortableProtocolCategory.UnsupportedContract));
    }

    // PB-05-UNKNOWN-CONTRACT-FIELD
    [Test]
    public void An_undeclared_contract_field_is_rejected_before_negotiation()
    {
        var extended = EncodedContract().With("hint", new CborText("ignore me"));
        Assert.That(
            CategoryOf(() => PortableContractCodec.Decode(extended)),
            Is.EqualTo(PortableProtocolCategory.MalformedMessage));
    }

    // PB-06-UNKNOWN-ENUMERATION-VALUE
    [Test]
    public void A_value_outside_a_declared_enumeration_is_not_defaulted_to_a_permissive_member()
    {
        var contract = EncodedContract();
        var requirements = contract.Array("requirements");
        var first = ((CborMap)requirements.Items[0]).With("strength", new CborText("mandatory"));
        Assert.That(
            CategoryOf(() => PortableContractCodec.Decode(contract.With("requirements", requirements.Replace(0, first)))),
            Is.EqualTo(PortableProtocolCategory.MalformedMessage));
    }

    // PB-07-COMPACT-IDENTIFIER-BEFORE-NEGOTIATION
    [Test]
    public void A_compact_identifier_this_binding_never_assigned_resolves_to_nothing()
    {
        var endpoint = PortableTestHarness.CoolingEndpoint(PortableRealization.FixedDirectCall);
        endpoint.Establish(CoolingPortableFixture.Contract, "host");
        endpoint.SignalReady();

        Assert.That(
            CategoryOf(() => endpoint.Request(
                PortableChannelRequestId.New(),
                null,
                4242,
                CoolingPortableFixture.CommandV1,
                CoolingPortableFixture.Command("primary", enabled: true),
                [])),
            Is.EqualTo(PortableProtocolCategory.UnsupportedContract));
    }

    // PB-08-NAME-OUTSIDE-PORTABLE-PROFILE
    [Test]
    public void A_name_outside_the_portable_profile_is_rejected_rather_than_kept_as_an_opaque_string()
    {
        Assert.Multiple(() =>
        {
            // A Unicode-letter name is a valid Brontide canonical name but is not portable in 0.1.
            Assert.That(PortableName.TryParse("interchange.tests.kühlung", out _), Is.False);
            Assert.That(PortableName.TryParse("a:b:c", out _), Is.False);
            Assert.That(PortableName.TryParse("interchange..tests", out _), Is.False);
            Assert.That(PortableName.TryParse("interchange.tests.cooling-component", out _), Is.True);
            Assert.That(
                CategoryOf(() => PortableName.Parse("interchange.tests.kühlung")),
                Is.EqualTo(PortableProtocolCategory.MalformedMessage));
        });
    }

    // PB-09-REQUEST-BEFORE-READY
    [Test]
    public void A_request_before_the_readiness_signal_produces_no_provider_effect()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        var endpoint = PortableTestHarness.CoolingEndpoint(PortableRealization.FixedDirectCall, handler);
        endpoint.Establish(CoolingPortableFixture.Contract, "host");

        Assert.Multiple(() =>
        {
            Assert.That(
                CategoryOf(() => endpoint.Request(
                    PortableChannelRequestId.New(),
                    CoolingPortableFixture.SetEnabled,
                    null,
                    CoolingPortableFixture.CommandV1,
                    CoolingPortableFixture.Command("primary", enabled: true),
                    [])),
                Is.EqualTo(PortableProtocolCategory.StateViolation));
            Assert.That(handler.ProviderEffectCount, Is.Zero);
        });
    }

    // PB-10-INLINE-NESTED-AND-REPEATED-VALUES
    [Test]
    public async Task Nested_and_repeated_inline_values_preserve_sequence_order_exactly()
    {
        await using var host = await PortableTestHarness.DirectHostAsync(
            CatalogPortableFixture.Contract,
            new CatalogPortableHandler());

        var upsert = await host.InvokeAsync(
            CatalogPortableFixture.Upsert,
            CatalogPortableFixture.UpsertCommand,
            CatalogPortableFixture.UpsertCommandValue(
                CatalogPortableFixture.ItemValue("a", "Alpha", "one", "two"),
                CatalogPortableFixture.ItemValue("b", "Beta", "two", "one")),
            PortableTestHarness.Permitted(),
            [CatalogPortableFixture.Handle()]);
        Assert.That(upsert.ResultClass, Is.EqualTo(PortableResultClass.OutcomeSucceeded));

        var find = await host.InvokeAsync(
            CatalogPortableFixture.Find,
            CatalogPortableFixture.FindCommand,
            CatalogPortableFixture.FindCommandValue("b", "a"),
            PortableTestHarness.Permitted(),
            [CatalogPortableFixture.Handle()]);

        var items = (PortableSequenceValue)((PortableRecordValue)find.Value!).Fields["items"];
        var ids = items.Items
            .Cast<PortableRecordValue>()
            .Select(item => ((PortableTextValue)item.Fields["id"]).Value)
            .ToImmutableArray();
        Assert.That(ids, Is.EqualTo(new[] { "b", "a" }).AsCollection, "Sequence order is semantic and is preserved.");
    }

    // PB-11-ADDITIVE-PROJECTION and PB-57-OBSERVATION-RECORDS-MAPPING-OBLIGATIONS
    [Test]
    public async Task An_additive_payload_projects_and_the_projection_is_a_recorded_obligation()
    {
        await using var host = await PortableTestHarness.DirectHostAsync();
        var decision = host.PrepareRequest(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV2,
            PortableTestHarness.Permitted());

        Assert.Multiple(() =>
        {
            Assert.That(decision.FrameDecision, Is.EqualTo(PortableFrameDecision.Emit));
            Assert.That(decision.ResultClass, Is.EqualTo(PortableResultClass.Request));
            Assert.That(decision.MappingObligations, Has.One.Contains("projected:"));
        });

        var result = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV2,
            CoolingPortableFixture.Command("primary", enabled: true, requestedBy: "operator-2"),
            PortableTestHarness.Permitted(),
            prepared: decision);

        Assert.Multiple(() =>
        {
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.OutcomeSucceeded));
            Assert.That(result.Observation.MappingObligations, Has.One.Contains("projected:"));
        });
    }

    // PB-12-NON-ADDITIVE-SKEW
    [Test]
    public async Task A_non_additive_Shape_version_is_refused_and_begins_no_provider_effect()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        await using var host = await PortableTestHarness.DirectHostAsync(handler: handler);
        var result = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV3,
            PortableRecordValue.Of(
                ("loop", new PortableTextValue("primary")),
                ("enabled", new PortableBooleanValue(true)),
                ("reason", new PortableTextValue("maintenance"))),
            PortableTestHarness.Permitted());

        Assert.Multiple(() =>
        {
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.ProtocolError));
            Assert.That(result.Category, Is.EqualTo(PortableProtocolCategory.InvalidPayload));
            Assert.That(result.Observation.ProviderEffectCount, Is.Zero);
            Assert.That(handler.ProviderEffectCount, Is.Zero);
        });
    }

    // PB-13-CLOSED-FRAGMENT-POLICY-VIOLATED
    [Test]
    public async Task A_closed_Shape_refuses_a_Fragment_the_Operation_does_not_declare()
    {
        await using var host = await PortableTestHarness.DirectHostAsync(handler: new NotingCoolingHandler());
        var result = await host.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableTestHarness.Permitted());

        Assert.Multiple(() =>
        {
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.ProtocolError));
            Assert.That(result.Category, Is.EqualTo(PortableProtocolCategory.InvalidPayload));
        });
    }

    // PB-14-NULL-IN-NON-UNIT-OPTIONAL-FIELD
    [Test]
    public void A_present_but_null_optional_field_is_refused_because_absence_is_omission()
    {
        var record = CborArray.Of(
            CborMap.Of(
            [
                ("loop", new CborText("primary")),
                ("enabled", new CborBoolean(true)),
                ("failureMode", CborNull.Instance)
            ]),
            CborMap.Empty);

        Assert.That(
            CategoryOf(() => PortableValueCodec.Decode(Catalog, CoolingPortableFixture.CommandV1, record)),
            Is.EqualTo(PortableProtocolCategory.InvalidPayload));
    }

    // PB-15-UNKNOWN-CHOICE-ALTERNATIVE
    [Test]
    public void An_alternative_outside_the_declared_set_is_an_invalid_payload_not_an_unknown_kind()
    {
        var choice = CborArray.Of(new CborText("elsewhere"), new CborText("value"));
        Assert.That(
            CategoryOf(() => PortableValueCodec.Decode(Catalog, CoolingPortableFixture.EncodingChoice, choice)),
            Is.EqualTo(PortableProtocolCategory.InvalidPayload),
            "The envelope kind was recognized, so the refusal belongs to the payload.");
    }

    // PB-16-EXCEPTION-SHAPED-PAYLOAD-DATA
    [Test]
    public void Foreign_runtime_identity_in_a_payload_position_is_refused_before_semantic_use()
    {
        var body = CborMap.Of(
        [
            ("operation", CborAccessTestDouble.Reference(CoolingPortableFixture.SetEnabled)),
            ("inputShape", CborAccessTestDouble.Reference(CoolingPortableFixture.CommandV1)),
            ("input", CborArray.Of(
                CborMap.Of([("loop", new CborText("primary")), ("stackTrace", new CborText("at Provider.Invoke"))]),
                CborMap.Empty)),
            ("resources", new CborArray([]))
        ]);

        Assert.That(
            CategoryOf(() => PortableRequestBody.Decode(body)),
            Is.EqualTo(PortableProtocolCategory.InvalidPayload));
    }

    // PB-17-EXCEPTION-SHAPED-CONTROL-DATA
    [Test]
    public void Foreign_runtime_identity_in_a_control_position_is_malformed()
    {
        var envelope = CborMap.Of(
        [
            ("contractVersion", new CborInteger(1)),
            ("kind", new CborText("ready")),
            ("channelId", new CborText("c1")),
            ("exception", new CborText("System.InvalidOperationException")),
            ("body", CborMap.Empty)
        ]);

        Assert.That(
            CategoryOf(() => PortableEnvelopeCodec.Decode(PortableCbor.Encode(envelope), PortableLimits.Declared)),
            Is.EqualTo(PortableProtocolCategory.MalformedMessage));
    }

    /// <summary>A provider whose result attaches a Fragment the negotiated Operation does not declare.</summary>
    private sealed class NotingCoolingHandler : IPortableOperationHandler
    {
        public PortableOperationEffect Invoke(
            PortableOperationReference operation,
            PortableValue input,
            IReadOnlyList<PortableResource> resources) =>
            PortableOperationEffect.Success(
                PortableRecordValue.Of(
                        ("loop", new PortableTextValue("primary")),
                        ("coolingEnabled", new PortableBooleanValue(true)),
                        ("revision", new PortableIntegerValue(1)),
                        ("providerEffectCount", new PortableIntegerValue(1)))
                    .WithFragment(CoolingPortableFixture.Note, ("note", new PortableTextValue("undeclared"))),
                1);
    }
}

/// <summary>Builds the structured canonical reference the control positions carry.</summary>
internal static class CborAccessTestDouble
{
    public static CborItem Reference(PortableOperationReference reference) =>
        CborMap.Of([("name", new CborText(reference.Name.Value)), ("version", new CborInteger(reference.Version))]);

    public static CborItem Reference(PortableShapeReference reference) =>
        CborMap.Of([("name", new CborText(reference.Name.Value)), ("version", new CborInteger(reference.Version))]);
}
