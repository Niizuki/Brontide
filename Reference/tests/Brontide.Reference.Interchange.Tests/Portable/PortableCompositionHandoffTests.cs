using System.Collections.Immutable;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// PB7: the Composition handoff, exercised by a controlled experimental composition.
/// </summary>
/// <remarks>
/// The handoff consumes a resolution and produces one Binding Plan. What it refuses matters as much
/// as what it produces: a Provider Set, a mediated exposure, a provider the resolution did not
/// select, and a provider substituted by the answering endpoint are all refused rather than
/// approximated, because each is a decision the Component Management programme owns.
///
/// The composition below is deliberately small — two members, two realizations, one release barrier
/// — and it is a test harness rather than a Component Manager. It exists to prove the seam is usable
/// from composition machinery without that machinery existing yet.
/// </remarks>
public sealed class PortableCompositionHandoffTests
{
    private const string HostEndpoint = "reference-composition";

    private static PortableBindingScopeId Scope(string value) => PortableBindingScopeId.Parse(value);

    private static PortableResolvedRequirement CoolingRequirement(string scope = "workspace.cooling") =>
        PortableResolvedRequirement.OneToOneProvider(
            Scope(scope),
            CoolingPortableFixture.Component,
            CoolingPortableFixture.Provider,
            HostEndpoint);

    private static PortableOfferedProvision CoolingProvision() =>
        new(CoolingPortableFixture.Component, CoolingPortableFixture.Provider, "cooling-endpoint");

    private static PortableCompositionMember PrepareCooling(string scope = "workspace.cooling") =>
        PortableCompositionHandoff.Prepare(
            CoolingRequirement(scope),
            CoolingProvision(),
            CoolingPortableFixture.Contract);

    /// <summary>The fixed direct-call realization behind one member.</summary>
    private static IPortableProviderConversation DirectCooling(CoolingPortableHandler handler) =>
        new PortableDirectConversation(new PortableProviderEndpoint(
            CoolingPortableFixture.Contract,
            handler,
            PortableRealization.FixedDirectCall));

    private static PortableValue CoolingCommand() =>
        CoolingPortableFixture.Command("primary", enabled: true);

    private static ValueTask<PortableInteractionResult> SetEnabledAsync(PortableCompositionMember member) =>
        member.InvokeAsync(
            CoolingPortableFixture.SetEnabled,
            CoolingPortableFixture.CommandV1,
            CoolingCommand(),
            PortableTestHarness.Permitted());

    // -- PB-64, PB-65: what the handoff produces, and what it has not produced yet ---------------

    /// <summary>PB-64: a resolution plus a provision is a Binding Plan, and nothing more.</summary>
    [Test]
    public async Task A_resolved_requirement_and_an_offered_provision_produce_a_plan()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        await using var member = PrepareCooling();
        await member.InterconnectAsync(DirectCooling(handler));

        Assert.Multiple(() =>
        {
            Assert.That(member.Stage, Is.EqualTo(PortableCompositionStage.Interconnected));
            Assert.That(member.Plan, Is.Not.Null);
            Assert.That(member.Plan!.HostEndpoint, Is.EqualTo(HostEndpoint), "The plan binds from the endpoint the resolution named.");
            Assert.That(member.AnsweringProvider, Is.EqualTo(CoolingPortableFixture.Provider));
            Assert.That(member.Fact("bindingScope"), Is.EqualTo("workspace.cooling"));
            Assert.That(member.Fact("cardinality"), Is.EqualTo("1..1"));
            Assert.That(member.Fact("exposure"), Is.EqualTo("distinct"));
            Assert.That(
                member.Fact("planId"),
                Is.EqualTo(member.Plan.PlanId.Value),
                "One member answers both its resolution facts and its plan facts, from the same scope.");
            Assert.That(
                handler.ProviderEffectCount,
                Is.Zero,
                "Establishing a binding is not an ordinary interaction and reaches no provider effect.");
        });
    }

    /// <summary>
    /// PB-65: before Interconnection the resolution is answerable and the plan is not.
    /// </summary>
    /// <remarks>
    /// A plan fact answered here would claim an agreement that does not exist. The seam has to say
    /// "there is no plan yet" rather than produce a placeholder that later turns out to be wrong.
    /// </remarks>
    [Test]
    public void Preflight_fixes_the_resolution_and_no_plan_fact()
    {
        var member = PrepareCooling();

        Assert.Multiple(() =>
        {
            Assert.That(member.Stage, Is.EqualTo(PortableCompositionStage.LocalInitialisation));
            Assert.That(member.Plan, Is.Null);
            Assert.That(
                member.ResolutionFacts.Keys,
                Is.EquivalentTo(new[]
                {
                    "bindingScope", "resolvedComponent", "requiredProvider", "cardinality", "exposure",
                    "resolvedHostEndpoint", "selectedProvision", "selectedProviderEndpoint"
                }));
            Assert.That(member.Fact("resolvedComponent"), Is.EqualTo(CoolingPortableFixture.Component.ToString()));
            Assert.That(member.Fact("requiredProvider"), Is.EqualTo(CoolingPortableFixture.Provider.ToString()));
            Assert.That(
                () => member.Fact("planId"),
                Throws.InstanceOf<InvalidOperationException>(),
                "No Binding Plan fact is answerable before negotiation has fixed one.");
            Assert.That(member.IsReady, Is.False);
            Assert.That(member.IsReleased, Is.False);
        });
    }

    /// <summary>An absent required provider is reported in its declared absent form.</summary>
    [Test]
    public void A_contract_only_requirement_records_no_required_provider()
    {
        var member = PortableCompositionHandoff.Prepare(
            PortableResolvedRequirement.OneToOneContract(
                Scope("workspace.cooling"),
                CoolingPortableFixture.Component,
                HostEndpoint),
            CoolingProvision(),
            CoolingPortableFixture.Contract);

        Assert.That(member.Fact("requiredProvider"), Is.EqualTo("absent"));
    }

    // -- PB-66 through PB-69: what preflight refuses, before any conversation exists -------------

    /// <summary>
    /// PB-66: the requirement, the provision, and the host's own declaration must name one Component.
    /// </summary>
    [Test]
    public void Preflight_refuses_a_component_the_provision_does_not_provide()
    {
        var mismatchedProvision = Assert.Throws<PortableFaultException>(() =>
            PortableCompositionHandoff.Prepare(
                CoolingRequirement(),
                new PortableOfferedProvision(
                    CatalogPortableFixture.Component,
                    CoolingPortableFixture.Provider,
                    "catalog-endpoint"),
                CoolingPortableFixture.Contract));

        // The host's own declaration is checked here too, so a local inconsistency is not discovered
        // at negotiation, where the diagnostic would name the peer for the host's mistake.
        var mismatchedDeclaration = Assert.Throws<PortableFaultException>(() =>
            PortableCompositionHandoff.Prepare(
                CoolingRequirement(),
                CoolingProvision(),
                CatalogPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(mismatchedProvision!.Category, Is.EqualTo(PortableProtocolCategory.UnsupportedContract));
            Assert.That(mismatchedProvision.LocalCode, Is.EqualTo("component-mismatch"));
            Assert.That(mismatchedProvision.Domain, Is.EqualTo(PortableFailureDomain.LocalEndpoint));
            Assert.That(mismatchedDeclaration!.Category, Is.EqualTo(PortableProtocolCategory.UnsupportedContract));
            Assert.That(mismatchedDeclaration.LocalCode, Is.EqualTo("declaration-mismatch"));
        });
    }

    /// <summary>PB-67: a provider-specific resolution accepts no compatible substitute.</summary>
    [Test]
    public void Preflight_refuses_a_provider_the_resolution_did_not_select()
    {
        var fault = Assert.Throws<PortableFaultException>(() =>
            PortableCompositionHandoff.Prepare(
                CoolingRequirement(),
                new PortableOfferedProvision(
                    CoolingPortableFixture.Component,
                    PortableProviderReference.Parse("interchange.tests.other-provider", 1),
                    "other-endpoint"),
                CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(fault!.Category, Is.EqualTo(PortableProtocolCategory.UnsupportedContract));
            Assert.That(fault.LocalCode, Is.EqualTo("provider-not-selected"));
        });
    }

    /// <summary>PB-68: a Provider Set is refused rather than narrowed to its first member.</summary>
    [Test]
    public void Preflight_refuses_a_cardinality_outside_one_to_one()
    {
        var fault = Assert.Throws<PortableFaultException>(() =>
            PortableCompositionHandoff.Prepare(
                CoolingRequirement() with { Cardinality = new PortableProviderCardinality(1, 4) },
                CoolingProvision(),
                CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(fault!.Category, Is.EqualTo(PortableProtocolCategory.UnsupportedContract));
            Assert.That(fault.LocalCode, Is.EqualTo("cardinality-unsupported"));
            Assert.That(
                fault.Message,
                Does.Contain("1..4"),
                "The refusal names the cardinality it refused, so a resolver can tell what to change.");
        });
    }

    /// <summary>PB-69: a declared Mediation is refused rather than erased into a direct binding.</summary>
    [Test]
    public void Preflight_refuses_mediated_exposure()
    {
        var fault = Assert.Throws<PortableFaultException>(() =>
            PortableCompositionHandoff.Prepare(
                CoolingRequirement() with { Exposure = PortableExposure.Mediated },
                CoolingProvision(),
                CoolingPortableFixture.Contract));

        Assert.Multiple(() =>
        {
            Assert.That(fault!.Category, Is.EqualTo(PortableProtocolCategory.UnsupportedContract));
            Assert.That(fault.LocalCode, Is.EqualTo("exposure-unsupported"));
        });
    }

    // -- PB-70: the substitution only this seam can catch ----------------------------------------

    /// <summary>
    /// PB-70: negotiation accepts an endpoint that answers as another provider; the handoff does not.
    /// </summary>
    /// <remarks>
    /// Version 0.1 negotiation matches the Component by exact reference equality and never compares
    /// provider identity, so the establishment below succeeds on both sides. Which provision was
    /// selected is a composition fact, and this is the one place that holds it.
    /// </remarks>
    [Test]
    public async Task An_endpoint_that_answers_as_another_provider_is_refused()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        var substituted = CoolingPortableFixture.Contract with
        {
            Provider = PortableProviderReference.Parse("interchange.tests.substitute-provider", 1)
        };
        var endpoint = new PortableProviderEndpoint(substituted, handler, PortableRealization.FixedDirectCall);

        await using var member = PrepareCooling();
        var fault = Assert.ThrowsAsync<PortableFaultException>(async () =>
            await member.InterconnectAsync(new PortableDirectConversation(endpoint)));

        Assert.Multiple(() =>
        {
            Assert.That(
                endpoint.Plan,
                Is.Not.Null,
                "The contract negotiated on the provider side, which is what makes the substitution invisible to negotiation.");
            Assert.That(fault!.Category, Is.EqualTo(PortableProtocolCategory.UnsupportedContract));
            Assert.That(fault.LocalCode, Is.EqualTo("provider-substituted"));
            Assert.That(member.Plan, Is.Null, "An abandoned binding leaves the member without a plan.");
            Assert.That(member.Stage, Is.EqualTo(PortableCompositionStage.LocalInitialisation));
            Assert.That(member.IsReleased, Is.False);
            Assert.That(handler.ProviderEffectCount, Is.Zero);
        });
    }

    // -- PB-71 through PB-74: the gate, the release barrier, and retirement ----------------------

    /// <summary>PB-71: the gate refuses an ordinary interaction before Release.</summary>
    [Test]
    public async Task An_ordinary_interaction_before_release_is_refused()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        await using var member = PrepareCooling();
        await member.InterconnectAsync(DirectCooling(handler));

        var result = await SetEnabledAsync(member);

        Assert.Multiple(() =>
        {
            Assert.That(member.IsReady, Is.True, "The provider signalled readiness; only the gate is closed.");
            Assert.That(result.FrameDecision, Is.EqualTo(PortableFrameDecision.None));
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.ProtocolError));
            Assert.That(result.Category, Is.EqualTo(PortableProtocolCategory.StateViolation));
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Observation.TerminalStatus, Is.EqualTo(PortableTerminalStatus.ProtocolError));
            Assert.That(
                result.Observation.AuthorityDecision,
                Is.EqualTo(PortableAuthorityDecision.Unknown),
                "The gate refuses before the authority boundary, so no authority decision was made.");
            Assert.That(result.Observation.AuthorityDecisionPoint, Is.EqualTo(PortableAuthorityDecisionPoint.HostLocal));
            Assert.That(result.Observation.FailureDomain, Is.EqualTo(PortableFailureDomain.LocalEndpoint));
            Assert.That(result.Observation.ProviderEffectCount, Is.Zero);
            Assert.That(result.Observation.LocalCode, Is.EqualTo("gate-closed"));
            Assert.That(handler.ProviderEffectCount, Is.Zero, "The provider itself recorded no effect.");
        });
    }

    /// <summary>PB-72: a member that never interconnected has no readiness signal to release on.</summary>
    [Test]
    public void Release_requires_the_readiness_signal()
    {
        var member = PrepareCooling();
        var fault = Assert.Throws<PortableFaultException>(member.Release);

        // The gate is closed in Local Initialisation too, where there is no plan to observe against.
        var interaction = Assert.ThrowsAsync<PortableFaultException>(async () => await SetEnabledAsync(member));

        Assert.Multiple(() =>
        {
            Assert.That(fault!.Category, Is.EqualTo(PortableProtocolCategory.StateViolation));
            Assert.That(fault.LocalCode, Is.EqualTo("release-before-ready"));
            Assert.That(interaction!.Category, Is.EqualTo(PortableProtocolCategory.StateViolation));
            Assert.That(interaction.LocalCode, Is.EqualTo("gate-closed"));
            Assert.That(member.IsReleased, Is.False);
        });
    }

    /// <summary>PB-73: after Release the ordinary interaction reaches the provider.</summary>
    [Test]
    public async Task A_released_binding_interacts()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        await using var member = PrepareCooling();
        await member.InterconnectAsync(DirectCooling(handler));
        member.Release();

        var result = await SetEnabledAsync(member);

        Assert.Multiple(() =>
        {
            Assert.That(member.Stage, Is.EqualTo(PortableCompositionStage.Released));
            Assert.That(result.FrameDecision, Is.EqualTo(PortableFrameDecision.Accept));
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.OutcomeSucceeded));
            Assert.That(result.Observation.ProviderEffectCount, Is.EqualTo(1));
            Assert.That(handler.ProviderEffectCount, Is.EqualTo(1));
            Assert.That(
                result.Observation.SelectedProvider,
                Is.EqualTo(member.Plan!.SelectedProvider),
                "The observation reports the plan the handoff froze.");
        });
    }

    /// <summary>
    /// PB-74: withdrawal and termination inform a replacement generation without granting it
    /// anything.
    /// </summary>
    [Test]
    public async Task Withdrawal_informs_a_replacement_generation()
    {
        var handler = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        var member = PrepareCooling();
        await member.InterconnectAsync(DirectCooling(handler));
        member.Release();
        var retiredPlan = member.Plan!.PlanId;

        var record = await member.RetireAsync("the composition replaced this generation");
        var afterRetirement = await SetEnabledAsync(member);

        // A replacement generation resolves and establishes from the beginning; the record tells it
        // which scope it is re-binding, not how to resume the retired plan.
        var replacement = PrepareCooling();
        await replacement.InterconnectAsync(
            DirectCooling(new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry())));
        replacement.Release();

        Assert.Multiple(() =>
        {
            Assert.That(record.Scope, Is.EqualTo(Scope("workspace.cooling")));
            Assert.That(record.RetiredPlanId, Is.EqualTo(retiredPlan));
            Assert.That(record.Component, Is.EqualTo(CoolingPortableFixture.Component));
            Assert.That(record.Provider, Is.EqualTo(CoolingPortableFixture.Provider), "The record names who answered.");
            Assert.That(record.TerminalState, Is.EqualTo("terminated"));
            Assert.That(record.ReplacementPermitted, Is.True);
            Assert.That(member.Stage, Is.EqualTo(PortableCompositionStage.Retired));
            Assert.That(
                afterRetirement.Category,
                Is.EqualTo(PortableProtocolCategory.StateViolation),
                "Retirement closes the gate again, reported the way the pre-Release gate reports it.");
            Assert.That(afterRetirement.Observation.LocalCode, Is.EqualTo("gate-closed"));
            Assert.That(afterRetirement.Observation.ProviderEffectCount, Is.Zero);
            Assert.That(replacement.Scope, Is.EqualTo(record.Scope), "The binding scope survives its plan.");
            Assert.That(
                replacement.Plan!.PlanId,
                Is.Not.EqualTo(record.RetiredPlanId),
                "There is no renegotiation in place: a replacement is a new plan.");
        });

        await member.DisposeAsync();
        await replacement.DisposeAsync();
    }

    // -- The controlled experimental composition -------------------------------------------------

    /// <summary>
    /// One activation group: two members, two realizations, and one release barrier.
    /// </summary>
    /// <remarks>
    /// The group is the composition's part of the contract: it validates that every required member
    /// is ready and only then opens the gates. The seam's part is per member, which is why the group
    /// can be this small and still be honest about what it does.
    /// </remarks>
    private sealed class ActivationGroup : IAsyncDisposable
    {
        private readonly List<PortableCompositionMember> _members = [];
        private readonly List<PortableLocalSeam> _seams = [];

        public ImmutableArray<PortableCompositionMember> Members => [.. _members];

        public bool Released { get; private set; }

        /// <summary>Adds a resolved member the group requires but has not interconnected.</summary>
        public void Add(PortableCompositionMember member) => _members.Add(member);

        public async Task<PortableCompositionMember> InterconnectAsync(
            PortableResolvedRequirement requirement,
            PortableOfferedProvision provision,
            PortableContractDocument contract,
            IPortableOperationHandler handler,
            PortableRealization realization)
        {
            var member = PortableCompositionHandoff.Prepare(requirement, provision, contract);
            var endpoint = new PortableProviderEndpoint(contract, handler, realization);

            IPortableProviderConversation conversation;
            if (realization == PortableRealization.FixedDirectCall)
            {
                conversation = new PortableDirectConversation(endpoint);
            }
            else
            {
                var seam = PortableLocalSeam.Create(contract.Limits);
                seam.StartProvider(endpoint, contract.Limits);
                _seams.Add(seam);
                conversation = new PortableProcessConversation(seam.HostDuplex, contract.Limits);
            }

            await member.InterconnectAsync(conversation);
            _members.Add(member);
            return member;
        }

        /// <summary>Release is refused for the whole group unless every member is ready.</summary>
        public void Release()
        {
            if (_members.Any(member => !member.IsReady))
            {
                throw new InvalidOperationException("The group is not Ready, so no member is released.");
            }

            foreach (var member in _members)
            {
                member.Release();
            }

            Released = true;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var member in _members)
            {
                await member.DisposeAsync();
            }

            foreach (var seam in _seams)
            {
                await seam.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// The PB7 exit case: one composition establishes and releases a portable binding per member,
    /// across both realizations, with the gate closed until the group is Ready.
    /// </summary>
    [Test]
    public async Task One_activation_group_establishes_and_releases_two_members()
    {
        var cooling = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        var catalog = new CatalogPortableHandler();
        await using var group = new ActivationGroup();

        var coolingMember = await group.InterconnectAsync(
            CoolingRequirement(),
            CoolingProvision(),
            CoolingPortableFixture.Contract,
            cooling,
            PortableRealization.FixedDirectCall);
        var catalogMember = await group.InterconnectAsync(
            PortableResolvedRequirement.OneToOneProvider(
                Scope("workspace.catalog"),
                CatalogPortableFixture.Component,
                CatalogPortableFixture.Provider,
                HostEndpoint),
            new PortableOfferedProvision(
                CatalogPortableFixture.Component,
                CatalogPortableFixture.Provider,
                "catalog-endpoint"),
            CatalogPortableFixture.Contract,
            catalog,
            PortableRealization.NegotiatedProcess);

        // Interconnection is complete for both members and the gate is still closed for both.
        var beforeRelease = await SetEnabledAsync(coolingMember);

        group.Release();
        var afterRelease = await SetEnabledAsync(coolingMember);
        var catalogResult = await catalogMember.InvokeAsync(
            CatalogPortableFixture.Upsert,
            CatalogPortableFixture.UpsertCommand,
            CatalogPortableFixture.UpsertCommandValue(CatalogPortableFixture.ItemValue("a", "Alpha", "one")),
            PortableTestHarness.Permitted());

        Assert.Multiple(() =>
        {
            Assert.That(group.Members.Select(member => member.Scope.Value), Is.EquivalentTo(new[]
            {
                "workspace.cooling", "workspace.catalog"
            }));
            Assert.That(
                group.Members.Select(member => member.Plan!.Realization),
                Is.EquivalentTo(new[] { PortableRealization.FixedDirectCall, PortableRealization.NegotiatedProcess }),
                "One seam serves both realizations; the handoff does not choose between them.");
            Assert.That(
                beforeRelease.Category,
                Is.EqualTo(PortableProtocolCategory.StateViolation),
                "No member is Active before the group's release barrier.");
            Assert.That(cooling.ProviderEffectCount, Is.EqualTo(1), "Only the interaction after Release reached the provider.");
            Assert.That(afterRelease.ResultClass, Is.EqualTo(PortableResultClass.OutcomeSucceeded));
            Assert.That(catalogResult.ResultClass, Is.EqualTo(PortableResultClass.OutcomeSucceeded));
            Assert.That(catalog.ProviderEffectCount, Is.EqualTo(1));
            Assert.That(group.Released, Is.True);
        });
    }

    /// <summary>
    /// A member that never interconnected keeps the whole group closed, rather than releasing the
    /// members that did establish.
    /// </summary>
    [Test]
    public async Task A_member_that_is_not_ready_prevents_the_group_from_releasing()
    {
        var cooling = new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry());
        await using var group = new ActivationGroup();
        var member = await group.InterconnectAsync(
            CoolingRequirement(),
            CoolingProvision(),
            CoolingPortableFixture.Contract,
            cooling,
            PortableRealization.FixedDirectCall);

        // The second member is resolved but never interconnected, which is how an establishment
        // failure reaches the group: it has no readiness signal to contribute.
        var unready = PortableCompositionHandoff.Prepare(
            PortableResolvedRequirement.OneToOneProvider(
                Scope("workspace.catalog"),
                CatalogPortableFixture.Component,
                CatalogPortableFixture.Provider,
                HostEndpoint),
            new PortableOfferedProvision(
                CatalogPortableFixture.Component,
                CatalogPortableFixture.Provider,
                "catalog-endpoint"),
            CatalogPortableFixture.Contract);
        group.Add(unready);

        Assert.Throws<InvalidOperationException>(group.Release);

        var afterRefusedRelease = await SetEnabledAsync(member);

        Assert.Multiple(() =>
        {
            Assert.That(member.IsReady, Is.True);
            Assert.That(unready.IsReady, Is.False);
            Assert.That(
                member.IsReleased,
                Is.False,
                "A member that established is still not Active while a required peer is not Ready.");
            Assert.That(group.Released, Is.False);
            Assert.That(afterRefusedRelease.Category, Is.EqualTo(PortableProtocolCategory.StateViolation));
            Assert.That(cooling.ProviderEffectCount, Is.Zero);
        });
    }
}
