namespace Brontide.Minimal.Interchange.Tests.Portable

open System
open NUnit.Framework
open Brontide.Minimal.Binding.Portable

/// PB7: the Composition handoff, exercised by a controlled experimental composition.
///
/// The handoff consumes a resolution and produces one Binding Plan. What it refuses matters as much
/// as what it produces: a Provider Set, a mediated exposure, a provider the resolution did not
/// select, and a provider substituted by the answering endpoint are all refused rather than
/// approximated, because each is a decision the Component Management programme owns.
///
/// The composition below is deliberately small — two members, two realizations, one release barrier
/// — and it is a test harness rather than a Component Manager. It exists to prove the seam is usable
/// from composition machinery without that machinery existing yet.
[<TestFixture>]
type PortableCompositionHandoffTests() =

    let hostEndpoint = "minimal-composition"

    let scope value = expectOk (BindingScopeId.tryCreate value)

    let coolingRequirement () =
        ResolvedRequirement.oneToOneProvider (scope "workspace.cooling") CoolingFixture.component' CoolingFixture.provider hostEndpoint

    let coolingProvision () =
        { Component = CoolingFixture.component'
          Provider = CoolingFixture.provider
          ProviderEndpoint = "cooling-endpoint" }

    let catalogRequirement () =
        ResolvedRequirement.oneToOneProvider (scope "workspace.catalog") CatalogFixture.component' CatalogFixture.provider hostEndpoint

    let catalogProvision () =
        { Component = CatalogFixture.component'
          Provider = CatalogFixture.provider
          ProviderEndpoint = "catalog-endpoint" }

    let prepareCooling () =
        expectOk (PortableCompositionHandoff.prepare (coolingRequirement ()) (coolingProvision ()) CoolingFixture.contract)

    /// The fixed direct-call realization behind one member.
    let directConversation (document: ContractDocument) (handler: IPortableOperationHandler) =
        PortableDirectConversation(PortableProviderEndpoint(document, handler, Realization.FixedDirectCall))
        :> IPortableProviderConversation

    let interconnect (composed: CompositionMember) (conversation: IPortableProviderConversation) =
        expectOk (composed.Interconnect conversation).Result

    let setEnabled (composed: CompositionMember) =
        (composed.Invoke(
            CoolingFixture.setEnabled,
            CoolingFixture.commandV1,
            CoolingFixture.authorizedCommand "primary" true,
            permitted
        ))
            .Result

    // -- PB-64, PB-65: what the handoff produces, and what it has not produced yet ---------------

    /// PB-64: a resolution plus a provision is a Binding Plan, and nothing more.
    [<Test>]
    member _.``A resolved requirement and an offered provision produce a plan``() =
        let handler = CoolingHandler()
        let composed = prepareCooling ()
        interconnect composed (directConversation CoolingFixture.contract handler)
        let plan = Option.get composed.TryPlan

        assertAll (fun () ->
            CompositionStage.token composed.Stage |> shouldEqual "interconnected"
            BindingPlan.hostEndpoint plan |> shouldEqual hostEndpoint
            composed.AnsweringProvider |> shouldEqual (Some CoolingFixture.provider)
            composed.TryFact "bindingScope" |> shouldEqual (Some "workspace.cooling")
            composed.TryFact "cardinality" |> shouldEqual (Some "1..1")
            composed.TryFact "exposure" |> shouldEqual (Some "distinct")

            // One member answers both its resolution facts and its plan facts, from the same scope.
            composed.TryFact "planId" |> shouldEqual (Some(PlanId.value (BindingPlan.planId plan)))

            // Establishing a binding is not an ordinary interaction and reaches no provider effect.
            handler.ProviderEffectCount |> shouldEqual 0L)

        composed.Close()

    /// PB-65: before Interconnection the resolution is answerable and the plan is not.
    ///
    /// A plan fact answered here would claim an agreement that does not exist. The seam has to say
    /// "there is no plan yet" rather than produce a placeholder that later turns out to be wrong.
    [<Test>]
    member _.``Preflight fixes the resolution and no plan fact``() =
        let composed = prepareCooling ()

        assertAll (fun () ->
            CompositionStage.token composed.Stage |> shouldEqual "local-initialisation"
            composed.TryPlan |> shouldEqual None

            composed.ResolutionFacts
            |> Map.toList
            |> List.map fst
            |> shouldEqual
                [ "bindingScope"
                  "cardinality"
                  "exposure"
                  "requiredProvider"
                  "resolvedComponent"
                  "resolvedHostEndpoint"
                  "selectedProviderEndpoint"
                  "selectedProvision" ]

            composed.TryFact "resolvedComponent"
            |> shouldEqual (Some(PortableComponentRef.text CoolingFixture.component'))

            // No Binding Plan fact is answerable before negotiation has fixed one.
            composed.TryFact "planId" |> shouldEqual None
            composed.IsReady |> shouldEqual false
            composed.IsReleased |> shouldEqual false)

    /// An absent required provider is reported in its declared absent form.
    [<Test>]
    member _.``A contract-only requirement records no required provider``() =
        let requirement =
            ResolvedRequirement.oneToOneContract (scope "workspace.cooling") CoolingFixture.component' hostEndpoint

        let composed =
            expectOk (PortableCompositionHandoff.prepare requirement (coolingProvision ()) CoolingFixture.contract)

        composed.TryFact "requiredProvider" |> shouldEqual (Some "absent")

    // -- PB-66 through PB-69: what preflight refuses, before any conversation exists -------------

    /// PB-66: the requirement, the provision, and the host's own declaration must name one Component.
    [<Test>]
    member _.``Preflight refuses a component the provision does not provide``() =
        let mismatchedProvision =
            PortableCompositionHandoff.prepare
                (coolingRequirement ())
                { catalogProvision () with Provider = CoolingFixture.provider }
                CoolingFixture.contract
            |> expectCategory ProtocolCategory.UnsupportedContract

        // The host's own declaration is checked here too, so a local inconsistency is not discovered
        // at negotiation, where the diagnostic would name the peer for the host's mistake.
        let mismatchedDeclaration =
            PortableCompositionHandoff.prepare (coolingRequirement ()) (coolingProvision ()) CatalogFixture.contract
            |> expectCategory ProtocolCategory.UnsupportedContract

        assertAll (fun () ->
            mismatchedProvision.LocalCode |> shouldEqual "component-mismatch"
            mismatchedProvision.Domain |> shouldEqual FailureDomain.LocalEndpoint
            mismatchedDeclaration.LocalCode |> shouldEqual "declaration-mismatch")

    /// PB-67: a provider-specific resolution accepts no compatible substitute.
    [<Test>]
    member _.``Preflight refuses a provider the resolution did not select``() =
        let other = expectOk (PortableProviderRef.tryCreate "interchange.tests.other-provider" 1)

        let fault =
            PortableCompositionHandoff.prepare
                (coolingRequirement ())
                { coolingProvision () with
                    Provider = other
                    ProviderEndpoint = "other-endpoint" }
                CoolingFixture.contract
            |> expectCategory ProtocolCategory.UnsupportedContract

        fault.LocalCode |> shouldEqual "provider-not-selected"

    /// PB-68: a Provider Set is refused rather than narrowed to its first member.
    [<Test>]
    member _.``Preflight refuses a cardinality outside one to one``() =
        let requirement =
            { coolingRequirement () with Cardinality = { Minimum = 1; Maximum = 4 } }

        let fault =
            PortableCompositionHandoff.prepare requirement (coolingProvision ()) CoolingFixture.contract
            |> expectCategory ProtocolCategory.UnsupportedContract

        assertAll (fun () ->
            fault.LocalCode |> shouldEqual "cardinality-unsupported"

            // The refusal names the cardinality it refused, so a resolver can tell what to change.
            Assert.That(fault.Message, Does.Contain "1..4"))

    /// PB-69: a declared Mediation is refused rather than erased into a direct binding.
    [<Test>]
    member _.``Preflight refuses mediated exposure``() =
        let requirement =
            { coolingRequirement () with Exposure = Exposure.Mediated }

        let fault =
            PortableCompositionHandoff.prepare requirement (coolingProvision ()) CoolingFixture.contract
            |> expectCategory ProtocolCategory.UnsupportedContract

        fault.LocalCode |> shouldEqual "exposure-unsupported"

    // -- PB-70: the substitution only this seam can catch ----------------------------------------

    /// PB-70: negotiation accepts an endpoint that answers as another provider; the handoff does not.
    ///
    /// Negotiation now refuses an endpoint that answers as a provider the host did not require
    /// (Decision 11), so the composition seam is left with the case negotiation cannot see: a host
    /// whose required contract names a provider its own resolution did not select. It is reachable
    /// only when the requirement names no provider, because preflight otherwise settles it before
    /// any contact.
    [<Test>]
    member _.``An endpoint that is not the selected provision is refused``() =
        let handler = CoolingHandler()
        let endpoint = PortableProviderEndpoint(CoolingFixture.contract, handler, Realization.FixedDirectCall)

        let unselected =
            expectOk (PortableProviderRef.tryCreate "interchange.tests.substitute-provider" 1)

        // The requirement names no provider, so the resolution's provision is free of preflight and
        // reaches step 6; the contract the host requires is still the one the endpoint offers.
        let composed =
            expectOk (
                PortableCompositionHandoff.prepare
                    (ResolvedRequirement.oneToOneContract
                        (scope "workspace.cooling")
                        CoolingFixture.component'
                        hostEndpoint)
                    { Component = CoolingFixture.component'
                      Provider = unselected
                      ProviderEndpoint = "cooling-endpoint" }
                    CoolingFixture.contract)

        let fault =
            (composed.Interconnect(PortableDirectConversation endpoint)).Result
            |> expectCategory ProtocolCategory.UnsupportedContract

        assertAll (fun () ->
            // Negotiation passed: the endpoint answered as exactly the provider the host required.
            Assert.That(endpoint.Plan, Is.Not.Null)
            fault.LocalCode |> shouldEqual "provider-substituted"
            composed.TryPlan |> shouldEqual None
            CompositionStage.token composed.Stage |> shouldEqual "local-initialisation"
            composed.IsReleased |> shouldEqual false
            handler.ProviderEffectCount |> shouldEqual 0L)

    // -- PB-71 through PB-74: the gate, the release barrier, and retirement ----------------------

    /// PB-71: the gate refuses an ordinary interaction before Release.
    [<Test>]
    member _.``An ordinary interaction before release is refused``() =
        let handler = CoolingHandler()
        let composed = prepareCooling ()
        interconnect composed (directConversation CoolingFixture.contract handler)

        let result = expectOk (setEnabled composed)

        assertAll (fun () ->
            // The provider signalled readiness; only the gate is closed.
            composed.IsReady |> shouldEqual true
            result.FrameDecision |> shouldEqual FrameDecision.None
            result.ResultClass |> shouldEqual ResultClass.ProtocolError
            result.Category |> shouldEqual (Some ProtocolCategory.StateViolation)
            result.Value |> shouldEqual None
            result.Observation.TerminalStatus |> shouldEqual TerminalStatus.ProtocolError

            // The gate refuses before the authority boundary, so no authority decision was made.
            result.Observation.AuthorityDecision |> shouldEqual AuthorityDecision.Unknown
            result.Observation.AuthorityDecisionPoint |> shouldEqual AuthorityDecisionPoint.HostLocal
            result.Observation.FailureDomain |> shouldEqual (Some FailureDomain.LocalEndpoint)
            result.Observation.ProviderEffectCount |> shouldEqual 0L
            result.Observation.LocalCode |> shouldEqual (Some "gate-closed")
            Observation.completenessFailures result.Observation |> shouldEqual []

            // The provider itself recorded no effect.
            handler.ProviderEffectCount |> shouldEqual 0L)

        composed.Close()

    /// PB-72: a member that never interconnected has no readiness signal to release on.
    [<Test>]
    member _.``Release requires the readiness signal``() =
        let composed = prepareCooling ()
        let fault = composed.Release() |> expectCategory ProtocolCategory.StateViolation

        // The gate is closed in Local Initialisation too, where there is no plan to observe against.
        let interaction = expectCategory ProtocolCategory.StateViolation (setEnabled composed)

        assertAll (fun () ->
            fault.LocalCode |> shouldEqual "release-before-ready"
            interaction.LocalCode |> shouldEqual "gate-closed"
            composed.IsReleased |> shouldEqual false)

    /// PB-73: after Release the ordinary interaction reaches the provider.
    [<Test>]
    member _.``A released binding interacts``() =
        let handler = CoolingHandler()
        let composed = prepareCooling ()
        interconnect composed (directConversation CoolingFixture.contract handler)
        expectOk (composed.Release())

        let result = expectOk (setEnabled composed)
        let plan = Option.get composed.TryPlan

        assertAll (fun () ->
            CompositionStage.token composed.Stage |> shouldEqual "released"
            result.FrameDecision |> shouldEqual FrameDecision.Accept
            result.ResultClass |> shouldEqual ResultClass.OutcomeSucceeded
            result.Observation.ProviderEffectCount |> shouldEqual 1L
            handler.ProviderEffectCount |> shouldEqual 1L

            // The observation reports the plan the handoff froze.
            result.Observation.SelectedProvider |> shouldEqual (BindingPlan.selectedProvider plan))

        composed.Close()

    /// PB-74: withdrawal and termination inform a replacement generation without granting it
    /// anything.
    [<Test>]
    member _.``Withdrawal informs a replacement generation``() =
        let composed = prepareCooling ()
        interconnect composed (directConversation CoolingFixture.contract (CoolingHandler()))
        expectOk (composed.Release())
        let retiredPlan = BindingPlan.planId (Option.get composed.TryPlan)

        let record = expectOk (composed.Retire "the composition replaced this generation").Result
        let afterRetirement = expectOk (setEnabled composed)

        // A replacement generation resolves and establishes from the beginning; the record tells it
        // which scope it is re-binding, not how to resume the retired plan.
        let replacement = prepareCooling ()
        interconnect replacement (directConversation CoolingFixture.contract (CoolingHandler()))
        expectOk (replacement.Release())

        assertAll (fun () ->
            record.Scope |> shouldEqual (scope "workspace.cooling")
            record.RetiredPlan |> shouldEqual retiredPlan
            record.Component |> shouldEqual CoolingFixture.component'

            // The record names who answered.
            record.Provider |> shouldEqual CoolingFixture.provider
            record.TerminalState |> shouldEqual "terminated"
            record.ReplacementPermitted |> shouldEqual true
            CompositionStage.token composed.Stage |> shouldEqual "retired"

            // Retirement closes the gate again, and reports it the way the pre-Release gate does
            // rather than as a third behaviour.
            afterRetirement.Category |> shouldEqual (Some ProtocolCategory.StateViolation)
            afterRetirement.Observation.LocalCode |> shouldEqual (Some "gate-closed")
            afterRetirement.Observation.ProviderEffectCount |> shouldEqual 0L

            // The binding scope survives its plan, and there is no renegotiation in place: a
            // replacement is a new plan.
            replacement.Scope |> shouldEqual record.Scope
            Assert.That(BindingPlan.planId (Option.get replacement.TryPlan), Is.Not.EqualTo record.RetiredPlan))

        composed.Close()
        replacement.Close()

    // -- The controlled experimental composition -------------------------------------------------

    /// The PB7 exit case: one composition establishes and releases a portable binding per member,
    /// across both realizations, with the gate closed until the group is Ready.
    ///
    /// The group is the composition's part of the contract: it validates that every required member
    /// is ready and only then opens the gates. The seam's part is per member, which is why the group
    /// can be this small and still be honest about what it does.
    [<Test>]
    member _.``One activation group establishes and releases two members``() =
        let cooling = CoolingHandler()
        let catalog = CatalogHandler()
        let coolingMember = prepareCooling ()

        let catalogMember =
            expectOk (PortableCompositionHandoff.prepare (catalogRequirement ()) (catalogProvision ()) CatalogFixture.contract)

        interconnect coolingMember (directConversation CoolingFixture.contract cooling)

        // The Catalog member runs over a real duplex seam, so the group spans both realizations.
        use seam = new PortableLocalSeam(CatalogFixture.contract.Limits)
        seam.StartProvider(PortableProviderEndpoint(CatalogFixture.contract, catalog, Realization.NegotiatedProcess))

        interconnect
            catalogMember
            (PortableProcessConversation(seam.HostDuplex, CatalogFixture.contract.Limits))

        let members = [ coolingMember; catalogMember ]

        // Interconnection is complete for both members and the gate is still closed for both.
        let beforeRelease = expectOk (setEnabled coolingMember)

        // The barrier is one condition: every required member is Ready, and only then does any
        // member's gate open.
        let released =
            members |> List.forall (fun composed -> composed.IsReady)
            && members
               |> List.forall (fun composed ->
                   match composed.Release() with
                   | Ok() -> true
                   | Error _ -> false)

        let afterRelease = expectOk (setEnabled coolingMember)

        let catalogInvocation =
            catalogMember.Invoke(
                CatalogFixture.upsert,
                CatalogFixture.upsertCommand,
                CatalogFixture.upsertCommandValue [ CatalogFixture.itemValue "a" "Alpha" [ "one" ] ],
                permitted
            )

        let catalogResult = expectOk catalogInvocation.Result

        assertAll (fun () ->
            members |> List.map (fun composed -> BindingScopeId.value composed.Scope)
            |> shouldEqual [ "workspace.cooling"; "workspace.catalog" ]

            // One seam serves both realizations; the handoff does not choose between them.
            members
            |> List.map (fun composed -> Realization.token (BindingPlan.realization (Option.get composed.TryPlan)))
            |> shouldEqual [ "fixed-direct-call"; "negotiated-process" ]

            // No member is Active before the group's release barrier.
            beforeRelease.Category |> shouldEqual (Some ProtocolCategory.StateViolation)
            released |> shouldEqual true

            // Only the interaction after Release reached the provider.
            cooling.ProviderEffectCount |> shouldEqual 1L
            afterRelease.ResultClass |> shouldEqual ResultClass.OutcomeSucceeded
            catalogResult.ResultClass |> shouldEqual ResultClass.OutcomeSucceeded
            catalog.ProviderEffectCount |> shouldEqual 1L)

        coolingMember.Close()
        catalogMember.Close()

    // -- Properties over the whole group ---------------------------------------------------------

    /// Runs every stage this group reaches, on one member, and reports what was observed at each.
    ///
    /// The properties below quantify over this sequence rather than over one case, which is the
    /// Decision 10 practice: a property is a claim about every path, so it can fail where no single
    /// vector was written.
    member private _.EveryStage() =
        let handler = CoolingHandler()
        let composed = prepareCooling ()
        let facts = ResizeArray [ "local-initialisation", composed.ResolutionFacts ]
        let interactions = ResizeArray()

        interconnect composed (directConversation CoolingFixture.contract handler)
        facts.Add("interconnected", composed.ResolutionFacts)
        interactions.Add("interconnected", expectOk (setEnabled composed))

        expectOk (composed.Release())
        facts.Add("released", composed.ResolutionFacts)
        interactions.Add("released", expectOk (setEnabled composed))

        expectOk (composed.Retire "the property harness retires the member").Result
        |> ignore

        facts.Add("retired", composed.ResolutionFacts)
        interactions.Add("retired", expectOk (setEnabled composed))

        composed, handler, List.ofSeq interactions, List.ofSeq facts

    /// HANDOFF-P1: a plan exists exactly when Interconnection completed.
    [<Test>]
    member this.``Property a plan exists exactly when interconnection completed``() =
        let substituting =
            PortableProviderEndpoint(
                { CoolingFixture.contract with
                    Provider = expectOk (PortableProviderRef.tryCreate "interchange.tests.substitute-provider" 1) },
                CoolingHandler(),
                Realization.FixedDirectCall
            )

        let refused = prepareCooling ()

        (refused.Interconnect(PortableDirectConversation substituting)).Result
        |> expectCategory ProtocolCategory.UnsupportedContract
        |> ignore

        let composed, _, _, _ = this.EveryStage()

        assertAll (fun () ->
            // A refusal that established a binding before refusing it leaves no plan behind.
            refused.TryPlan |> shouldEqual None
            refused.AnsweringProvider |> shouldEqual None

            // A completed handoff always has its plan, answered by the selected provision.
            (Option.isSome composed.TryPlan) |> shouldEqual true
            composed.AnsweringProvider |> shouldEqual (Some composed.Provision.Provider))

        composed.Close()
        refused.Close()

    /// HANDOFF-P2: the provider records an effect only while the member is released.
    [<Test>]
    member this.``Property only a released member reaches a provider effect``() =
        let composed, handler, interactions, _ = this.EveryStage()
        let released = interactions |> List.filter (fun (stage, _) -> stage = "released")

        assertAll (fun () ->
            List.length interactions |> shouldEqual 3

            for stage, result in interactions |> List.filter (fun (stage, _) -> stage <> "released") do
                Assert.That(
                    ResultClass.token result.ResultClass,
                    Is.EqualTo(ResultClass.token ResultClass.ProtocolError),
                    $"An interaction in stage '{stage}' is refused rather than admitted."
                )

                result.Observation.ProviderEffectCount |> shouldEqual 0L

            // Counted at the provider, not read from the observation: the observation is what the
            // binding says happened, and the counter is what did.
            handler.ProviderEffectCount |> shouldEqual (int64 (List.length released)))

        composed.Close()

    /// HANDOFF-P3: the resolution facts never change, at any stage.
    [<Test>]
    member this.``Property the resolution facts outlive the plan``() =
        let composed, _, _, facts = this.EveryStage()
        let _, first = List.head facts

        assertAll (fun () ->
            List.length facts |> shouldEqual 4

            for stage, sample in facts do
                Assert.That(
                    (sample = first),
                    Is.True,
                    $"The resolution facts changed at stage '{stage}'; the scope outlives the plan."
                )

            Map.tryFind "bindingScope" first
            |> shouldEqual (Some(BindingScopeId.value composed.Scope)))

        composed.Close()

    /// A member that never interconnected keeps the whole group closed, rather than releasing the
    /// members that did establish.
    [<Test>]
    member _.``A member that is not ready prevents the group from releasing``() =
        let cooling = CoolingHandler()
        let established = prepareCooling ()
        interconnect established (directConversation CoolingFixture.contract cooling)

        // The second member is resolved but never interconnected, which is how an establishment
        // failure reaches the group: it has no readiness signal to contribute.
        let unready =
            expectOk (PortableCompositionHandoff.prepare (catalogRequirement ()) (catalogProvision ()) CatalogFixture.contract)

        let members = [ established; unready ]
        let groupReady = members |> List.forall (fun composed -> composed.IsReady)
        let afterRefusedRelease = expectOk (setEnabled established)

        assertAll (fun () ->
            established.IsReady |> shouldEqual true
            unready.IsReady |> shouldEqual false
            groupReady |> shouldEqual false

            // A member that established is still not Active while a required peer is not Ready.
            established.IsReleased |> shouldEqual false
            afterRefusedRelease.Category |> shouldEqual (Some ProtocolCategory.StateViolation)
            cooling.ProviderEffectCount |> shouldEqual 0L)

        established.Close()
