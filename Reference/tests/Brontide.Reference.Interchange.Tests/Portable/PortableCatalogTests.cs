using System.Collections.Immutable;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// Neutral vectors PB-64 through PB-69, and the three properties the Catalog group declares.
/// </summary>
/// <remarks>
/// Every other vector group is authored against the Cooling fixture, which declares one Operation,
/// no detail Shape worth distinguishing, and no referenced handle. These cover only what Cooling
/// structurally cannot state: a negotiated Operation set, a repeated container whose elements are
/// themselves repeated containers, a declared detail Shape, and the provider-scoped addressing-only
/// handle.
///
/// The properties at the end are the Decision 10 practice: each quantifies over every scenario in
/// the group rather than over one case, because the PB6 defects were all invariants no single
/// expectation stated.
/// </remarks>
public sealed class PortableCatalogTests
{
    private static PortableBindingPlan Negotiate() =>
        PortableNegotiation.Negotiate(
            CatalogPortableFixture.Contract,
            CatalogPortableFixture.Contract,
            PortableRealization.FixedDirectCall,
            "host",
            "provider",
            "test");

    private static ValueTask<PortableBindingHost> CatalogHostAsync(CatalogPortableHandler handler) =>
        PortableTestHarness.DirectHostAsync(CatalogPortableFixture.Contract, handler);

    private static ImmutableArray<string> ItemIds(PortableValue result) =>
    [
        .. ((PortableSequenceValue)((PortableRecordValue)result).Fields["items"]).Items
            .Cast<PortableRecordValue>()
            .Select(item => ((PortableTextValue)item.Fields["id"]).Value)
    ];

    // PB-64-CATALOG-MULTIPLE-OPERATIONS-NEGOTIATED
    [Test]
    public async Task A_contract_declaring_two_Operations_negotiates_both_into_one_plan()
    {
        var plan = Negotiate();
        var handler = new CatalogPortableHandler();
        await using var host = await CatalogHostAsync(handler);

        Assert.Multiple(() =>
        {
            Assert.That(
                plan.Operations,
                Is.EqualTo(new[] { CatalogPortableFixture.Upsert, CatalogPortableFixture.Find }).AsCollection);

            // Each Operation keeps its own three Shape positions. Cooling cannot show this: with one
            // Operation, "the plan's result Shape" and "this Operation's result Shape" coincide.
            var upsert = plan.Operation(CatalogPortableFixture.Upsert);
            var find = plan.Operation(CatalogPortableFixture.Find);
            Assert.That(upsert.InputShape, Is.EqualTo(CatalogPortableFixture.UpsertCommand));
            Assert.That(upsert.ResultShape, Is.EqualTo(CatalogPortableFixture.UpsertResult));
            Assert.That(find.InputShape, Is.EqualTo(CatalogPortableFixture.FindCommand));
            Assert.That(find.ResultShape, Is.EqualTo(CatalogPortableFixture.FindResult));
            Assert.That(upsert.ResultShape, Is.Not.EqualTo(find.ResultShape));

            // Both declare the same detail Shape, which is a fixture choice rather than a rule.
            Assert.That(upsert.DetailShape, Is.EqualTo(CatalogPortableFixture.Details));
            Assert.That(find.DetailShape, Is.EqualTo(CatalogPortableFixture.Details));

            Assert.That(handler.ProviderEffectCount, Is.Zero, "Establishment activates no Operation.");
        });
    }

    // PB-65-CATALOG-REQUEST-SELECTS-ITS-OPERATION
    [Test]
    public async Task Each_request_is_routed_by_the_Operation_it_names_over_one_binding()
    {
        var handler = new CatalogPortableHandler();
        await using var host = await CatalogHostAsync(handler);

        var upsert = await host.InvokeAsync(
            CatalogPortableFixture.Upsert,
            CatalogPortableFixture.UpsertCommand,
            CatalogPortableFixture.UpsertCommandValue(CatalogPortableFixture.ItemValue("a", "Alpha", "one")),
            PortableTestHarness.Permitted(),
            [CatalogPortableFixture.Handle()]);

        var find = await host.InvokeAsync(
            CatalogPortableFixture.Find,
            CatalogPortableFixture.FindCommand,
            CatalogPortableFixture.FindCommandValue("a"),
            PortableTestHarness.Permitted(),
            [CatalogPortableFixture.Handle()]);

        Assert.Multiple(() =>
        {
            Assert.That(upsert.ResultClass, Is.EqualTo(PortableResultClass.OutcomeSucceeded));
            Assert.That(find.ResultClass, Is.EqualTo(PortableResultClass.OutcomeSucceeded));

            // Each result is shaped by its own Operation's result Shape: upsert answers with a count,
            // find with items. Routing is therefore by the named Operation, not by the only one there is.
            Assert.That(((PortableRecordValue)upsert.Value!).Fields, Does.ContainKey("stored"));
            Assert.That(((PortableRecordValue)upsert.Value!).Fields, Does.Not.ContainKey("items"));
            Assert.That(((PortableRecordValue)find.Value!).Fields, Does.ContainKey("items"));
            Assert.That(((PortableRecordValue)find.Value!).Fields, Does.Not.ContainKey("stored"));

            // Sequential invocation is legal: single-invocation bounds concurrency at one request,
            // it does not cap how many a binding may serve over its lifetime.
            Assert.That(handler.ProviderEffectCount, Is.EqualTo(2));
        });
    }

    // PB-66-CATALOG-SEQUENCE-OF-RECORDS-CARRYING-SEQUENCES
    [Test]
    public async Task A_repeated_container_of_repeated_containers_round_trips_exactly()
    {
        var handler = new CatalogPortableHandler();
        await using var host = await CatalogHostAsync(handler);

        await host.InvokeAsync(
            CatalogPortableFixture.Upsert,
            CatalogPortableFixture.UpsertCommand,
            CatalogPortableFixture.UpsertCommandValue(
                CatalogPortableFixture.ItemValue("a", "Alpha", "one", "two"),
                CatalogPortableFixture.ItemValue("b", "Beta", "two"),
                CatalogPortableFixture.ItemValue("c", "Gamma")),
            PortableTestHarness.Permitted(),
            [CatalogPortableFixture.Handle()]);

        var find = await host.InvokeAsync(
            CatalogPortableFixture.Find,
            CatalogPortableFixture.FindCommand,
            CatalogPortableFixture.FindCommandValue("c", "a", "b"),
            PortableTestHarness.Permitted(),
            [CatalogPortableFixture.Handle()]);

        var items = ((PortableSequenceValue)((PortableRecordValue)find.Value!).Fields["items"]).Items
            .Cast<PortableRecordValue>()
            .ToImmutableArray();

        ImmutableArray<string> TagsOf(int index) =>
        [
            .. ((PortableSequenceValue)items[index].Fields["tags"]).Items
                .Cast<PortableTextValue>()
                .Select(tag => tag.Value)
        ];

        Assert.Multiple(() =>
        {
            Assert.That(ItemIds(find.Value!), Is.EqualTo(new[] { "c", "a", "b" }).AsCollection);

            // The inner sequences survive independently of the outer one.
            Assert.That(TagsOf(0), Is.Empty, "An empty inner sequence stays empty rather than becoming absent or null.");
            Assert.That(TagsOf(1), Is.EqualTo(new[] { "one", "two" }).AsCollection);
            Assert.That(TagsOf(2), Is.EqualTo(new[] { "two" }).AsCollection);

            // Absence and emptiness are different: the field is present and its value is an empty
            // sequence, which is exactly the distinction PB-14 turns on for null.
            Assert.That(items[0].Fields, Does.ContainKey("tags"));
        });
    }

    // PB-67-CATALOG-FAILED-OUTCOME-USES-THE-DECLARED-DETAIL-SHAPE
    [Test]
    public async Task A_semantic_failure_is_shaped_by_the_Operations_declared_detail_Shape()
    {
        var handler = new CatalogPortableHandler();
        await using var host = await CatalogHostAsync(handler);
        var plan = Negotiate();

        // Nothing was stored, so the lookup cannot be satisfied.
        var find = await host.InvokeAsync(
            CatalogPortableFixture.Find,
            CatalogPortableFixture.FindCommand,
            CatalogPortableFixture.FindCommandValue("absent"),
            PortableTestHarness.Permitted(),
            [CatalogPortableFixture.Handle()]);

        var detailShape = plan.Operation(CatalogPortableFixture.Find).DetailShape;
        var declaredDetailFields = CatalogPortableFixture.Contract.Shapes
            .Single(shape => shape.Reference == detailShape)
            .Fields
            .Select(field => field.Name)
            .ToImmutableArray();

        Assert.Multiple(() =>
        {
            // A semantic failure is an Outcome, not a protocol error and not an exception.
            Assert.That(find.FrameDecision, Is.EqualTo(PortableFrameDecision.Accept));
            Assert.That(find.ResultClass, Is.EqualTo(PortableResultClass.OutcomeFailed));
            Assert.That(find.Category, Is.Null);
            Assert.That(find.ProcessCategory, Is.Null);

            // The detail conforms to the Shape the Operation declares. The retained Catalog
            // experiment declared no detail Shape at all; the neutral contract requires all three
            // positions, and this is what that addition buys.
            var detail = (PortableRecordValue)find.Value!;
            Assert.That(detail.Fields.Keys, Is.EquivalentTo(declaredDetailFields));

            // The Shape is normative; the code's spelling is not. PB-48 already fixes that two
            // realizations may choose different local codes for the same portable category, so
            // asserting a particular string here would assert something the contract does not say.
            Assert.That(((PortableTextValue)detail.Fields["code"]).Value, Is.Not.Empty);

            Assert.That(find.Observation.TerminalStatus, Is.EqualTo(PortableTerminalStatus.Failed));
            Assert.That(handler.ProviderEffectCount, Is.Zero, "A lookup that stored nothing performed no effect.");
        });
    }

    // PB-68-CATALOG-HANDLE-IS-NOT-AN-ADMISSION-DECISION
    [Test]
    public async Task An_accepted_handle_addresses_the_domain_without_deciding_its_answer()
    {
        var handler = new CatalogPortableHandler();
        await using var host = await CatalogHostAsync(handler);
        var handle = CatalogPortableFixture.Handle();

        var stored = await host.InvokeAsync(
            CatalogPortableFixture.Upsert,
            CatalogPortableFixture.UpsertCommand,
            CatalogPortableFixture.UpsertCommandValue(CatalogPortableFixture.ItemValue("a", "Alpha", "one")),
            PortableTestHarness.Permitted(),
            [handle]);

        var satisfiable = await host.InvokeAsync(
            CatalogPortableFixture.Find,
            CatalogPortableFixture.FindCommand,
            CatalogPortableFixture.FindCommandValue("a"),
            PortableTestHarness.Permitted(),
            [handle]);

        // Every requested identifier is absent. A partial match is deliberately not used: the
        // fixture contract declares no partial-match rule, so a vector turning on one would assert
        // undeclared domain behaviour rather than the handle rule this vector is about.
        var unsatisfiable = await host.InvokeAsync(
            CatalogPortableFixture.Find,
            CatalogPortableFixture.FindCommand,
            CatalogPortableFixture.FindCommandValue("absent"),
            PortableTestHarness.Permitted(),
            [handle]);

        Assert.Multiple(() =>
        {
            // The same in-scope handle is admitted every time, and carries no octets.
            foreach (var result in new[] { stored, satisfiable, unsatisfiable })
            {
                Assert.That(result.FrameDecision, Is.EqualTo(PortableFrameDecision.Accept));
                Assert.That(result.Observation.ReferencedResources, Has.Length.EqualTo(1));
                Assert.That(result.Observation.ReferencedResources[0].Ownership, Is.EqualTo("provider-retained"));
                Assert.That(result.Observation.CopyCount, Is.Zero);
            }

            // The outcomes still differ, so admitting the handle and admitting the request are two
            // decisions. Possession conveys where to look, never what may be done.
            Assert.That(satisfiable.ResultClass, Is.EqualTo(PortableResultClass.OutcomeSucceeded));
            Assert.That(unsatisfiable.ResultClass, Is.EqualTo(PortableResultClass.OutcomeFailed));
        });
    }

    // PB-69-CATALOG-UNNEGOTIATED-FLAVOR-IN-REQUEST
    [Test]
    public async Task A_flavor_outside_the_frozen_plan_is_refused_after_establishment_succeeded()
    {
        var handler = new CatalogPortableHandler();
        await using var host = await CatalogHostAsync(handler);

        // copied-immutable-blob is a declared 0.1 flavor and the Cooling binding negotiates it. This
        // binding did not, so negotiating it elsewhere confers nothing here.
        var result = await host.InvokeAsync(
            CatalogPortableFixture.Upsert,
            CatalogPortableFixture.UpsertCommand,
            CatalogPortableFixture.UpsertCommandValue(CatalogPortableFixture.ItemValue("a", "Alpha", "one")),
            PortableTestHarness.Permitted(),
            [PortableTestHarness.Blob()]);

        Assert.Multiple(() =>
        {
            Assert.That(result.ResultClass, Is.EqualTo(PortableResultClass.ProtocolError));

            // This is where a resource refusal splits between two categories. A flavor is a term of
            // the frozen contract, so refusing one is unsupported-contract whether it is reached
            // during negotiation (PB-29) or afterwards (here). Refusing a particular resource of a
            // negotiated flavor is invalid-payload instead: PB-28's out-of-scope handle and PB-26's
            // failed content hash.
            Assert.That(result.Category, Is.EqualTo(PortableProtocolCategory.UnsupportedContract));
            Assert.That(
                result.Category,
                Is.Not.EqualTo(PortableProtocolCategory.InvalidPayload),
                "The flavor itself was never negotiated, so this is not an instance-level payload decision.");
            Assert.That(handler.ProviderEffectCount, Is.Zero);

            // The refused resource is still observed, and is not reported as an admission that
            // never completed.
            Assert.That(result.Observation.ReferencedResources, Has.Length.EqualTo(1));
            Assert.That(result.Observation.ReferencedResources[0].Accepted, Is.False);
            Assert.That(result.Observation.ReferencedResources[0].IntegrityVerified, Is.False);
        });
    }

    // ------------------------------------------------------------------------------------------
    // Properties over the whole group (Decision 10).
    //
    // A per-vector expectation states what one case should produce. These state what must hold of
    // every case, including ones nobody wrote, which is the class of claim PB6 found missing: all
    // three of its defects were invariants that every individual expectation happened to satisfy.
    // ------------------------------------------------------------------------------------------

    /// <summary>
    /// Every interaction the group performs, paired with the Operation the request named, so a
    /// property can quantify over them. The observation does not carry the invoked Operation, so it
    /// is recorded here rather than inferred.
    /// </summary>
    private static async Task<ImmutableArray<(PortableOperationReference Operation, PortableInteractionResult Result)>>
        EveryGroupInteractionAsync()
    {
        var results =
            ImmutableArray.CreateBuilder<(PortableOperationReference, PortableInteractionResult)>();
        var handler = new CatalogPortableHandler();
        await using var host = await CatalogHostAsync(handler);
        var handle = CatalogPortableFixture.Handle();

        results.Add((CatalogPortableFixture.Upsert, await host.InvokeAsync(
            CatalogPortableFixture.Upsert,
            CatalogPortableFixture.UpsertCommand,
            CatalogPortableFixture.UpsertCommandValue(
                CatalogPortableFixture.ItemValue("a", "Alpha", "one", "two"),
                CatalogPortableFixture.ItemValue("c", "Gamma")),
            PortableTestHarness.Permitted(),
            [handle])));

        results.Add((CatalogPortableFixture.Find, await host.InvokeAsync(
            CatalogPortableFixture.Find,
            CatalogPortableFixture.FindCommand,
            CatalogPortableFixture.FindCommandValue("c", "a"),
            PortableTestHarness.Permitted(),
            [handle])));

        results.Add((CatalogPortableFixture.Find, await host.InvokeAsync(
            CatalogPortableFixture.Find,
            CatalogPortableFixture.FindCommand,
            CatalogPortableFixture.FindCommandValue("absent"),
            PortableTestHarness.Permitted(),
            [handle])));

        results.Add((CatalogPortableFixture.Upsert, await host.InvokeAsync(
            CatalogPortableFixture.Upsert,
            CatalogPortableFixture.UpsertCommand,
            CatalogPortableFixture.UpsertCommandValue(CatalogPortableFixture.ItemValue("a", "Alpha", "one")),
            PortableTestHarness.Permitted(),
            [PortableTestHarness.Blob()])));

        return results.ToImmutable();
    }

    // CATALOG-P1
    [Test]
    public async Task Property_every_named_Operation_is_a_member_of_the_established_plan()
    {
        var plan = Negotiate();
        var results = await EveryGroupInteractionAsync();

        Assert.Multiple(() =>
        {
            Assert.That(results, Is.Not.Empty);
            foreach (var (operation, result) in results)
            {
                Assert.That(
                    result.Observation.NegotiatedOperations,
                    Is.EqualTo(plan.Operations).AsCollection,
                    "A binding never reports an Operation set other than the one it negotiated.");
                Assert.That(
                    plan.Operations,
                    Does.Contain(operation),
                    "A request's Operation is always a member of its own plan.");
            }
        });
    }

    // CATALOG-P2
    [Test]
    public async Task Property_no_octets_ever_cross_for_this_bindings_only_negotiated_flavor()
    {
        var results = await EveryGroupInteractionAsync();

        Assert.Multiple(() =>
        {
            foreach (var (_, result) in results)
            {
                Assert.That(
                    result.Observation.CopyCount,
                    Is.Zero,
                    "The addressing-only handle is the only negotiated flavor and it carries no octets.");
                foreach (var resource in result.Observation.ReferencedResources)
                {
                    // Quantifying over *accepted* resources rather than over reported ones is the
                    // point. A refused resource is still observed, so the reported set legitimately
                    // contains flavors the plan never froze; what must never happen is one of them
                    // being reported as admitted.
                    if (resource.Accepted)
                    {
                        Assert.That(
                            resource.Flavor,
                            Is.EqualTo(PortableResourceFlavors.AddressingOnlyHandle),
                            "An accepted resource is always of a flavor the plan froze.");
                    }
                    else
                    {
                        Assert.That(
                            resource.IntegrityVerified,
                            Is.False,
                            "An admission that never completed claims no integrity check.");
                    }
                }
            }
        });
    }

    // CATALOG-P3
    [Test]
    public async Task Property_an_outcome_never_carries_both_a_result_and_a_detail()
    {
        var results = await EveryGroupInteractionAsync();

        Assert.Multiple(() =>
        {
            foreach (var (_, result) in results)
            {
                switch (result.ResultClass)
                {
                    case PortableResultClass.OutcomeSucceeded:
                        Assert.That(result.Value, Is.Not.Null, "A success carries its result.");
                        Assert.That(
                            result.Observation.TerminalStatus,
                            Is.EqualTo(PortableTerminalStatus.Succeeded));
                        break;
                    case PortableResultClass.OutcomeFailed:
                        Assert.That(result.Value, Is.Not.Null, "A shaped failure carries its detail.");
                        Assert.That(
                            result.Observation.TerminalStatus,
                            Is.EqualTo(PortableTerminalStatus.Failed),
                            "A failed Outcome never reports a succeeded terminal status.");
                        break;
                    default:
                        Assert.That(
                            result.Value,
                            Is.Null,
                            "A non-Outcome result class carries neither a result nor a detail.");
                        Assert.That(
                            result.Observation.TerminalStatus,
                            Is.Not.EqualTo(PortableTerminalStatus.Succeeded),
                            "Success is never fabricated for a refused request.");
                        break;
                }
            }
        });
    }
}
