using System.Collections.Immutable;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>
/// The Catalog experiment restated as a fixture over the reusable portable layer.
/// </summary>
/// <remarks>
/// Catalog contributes what Cooling cannot: more than one Operation, nested and repeated values,
/// and the provider-scoped addressing-only handle. Like Cooling it is data and a handler, so the
/// reusable layer stays free of any Catalog rule.
/// </remarks>
public static class CatalogPortableFixture
{
    public const string AcceptedHandle = "catalog-provider/primary";

    public static PortableComponentReference Component { get; } =
        PortableComponentReference.Parse("interchange.tests.catalog-component", 1);

    public static PortableProviderReference Provider { get; } =
        PortableProviderReference.Parse("interchange.tests.catalog-provider", 1);

    public static PortableOperationReference Upsert { get; } =
        PortableOperationReference.Parse("interchange.tests.catalog.upsert-items", 1);

    public static PortableOperationReference Find { get; } =
        PortableOperationReference.Parse("interchange.tests.catalog.find-items", 1);

    public static PortableShapeReference Item { get; } =
        PortableShapeReference.Parse("interchange.tests.catalog.item", 1);

    public static PortableShapeReference ItemSequence { get; } =
        PortableShapeReference.Parse("interchange.tests.catalog.item-sequence", 1);

    public static PortableShapeReference TextSequence { get; } =
        PortableShapeReference.Parse("interchange.tests.catalog.text-sequence", 1);

    public static PortableShapeReference UpsertCommand { get; } =
        PortableShapeReference.Parse("interchange.tests.catalog.upsert-command", 1);

    public static PortableShapeReference UpsertResult { get; } =
        PortableShapeReference.Parse("interchange.tests.catalog.upsert-result", 1);

    public static PortableShapeReference FindCommand { get; } =
        PortableShapeReference.Parse("interchange.tests.catalog.find-command", 1);

    public static PortableShapeReference FindResult { get; } =
        PortableShapeReference.Parse("interchange.tests.catalog.find-result", 1);

    public static PortableShapeReference Details { get; } =
        PortableShapeReference.Parse("interchange.tests.catalog.details", 1);

    public static PortableContractDocument Contract { get; } = BuildContract();

    public static PortableRecordValue ItemValue(string id, string title, params string[] tags) =>
        PortableRecordValue.Of(
            ("id", new PortableTextValue(id)),
            ("title", new PortableTextValue(title)),
            ("tags", new PortableSequenceValue([.. tags.Select(tag => (PortableValue)new PortableTextValue(tag))])));

    public static PortableRecordValue UpsertCommandValue(params PortableRecordValue[] items) =>
        PortableRecordValue.Of(
            ("items", new PortableSequenceValue([.. items.Cast<PortableValue>()])));

    public static PortableRecordValue FindCommandValue(params string[] ids) =>
        PortableRecordValue.Of(
            ("ids", new PortableSequenceValue([.. ids.Select(id => (PortableValue)new PortableTextValue(id))])));

    public static PortableHandleResource Handle(string provider = "catalog-provider", string id = "primary") =>
        new("catalog", provider, id);

    private static PortableContractDocument BuildContract()
    {
        var text = PortableBuiltInShapes.Text;
        var signed = PortableBuiltInShapes.Signed64;
        var flavor = PortableResourceFlavors.AddressingOnlyHandle;

        return new PortableContractDocument(
            PortableContractDocument.SupportedContractVersion,
            Component,
            Provider,
            [
                new PortableProvision(
                    PortableDependencyKind.Operation,
                    PortableDependencyReference.Parse("interchange.tests.catalog.upsert-items", 1),
                    false),
                new PortableProvision(
                    PortableDependencyKind.Operation,
                    PortableDependencyReference.Parse("interchange.tests.catalog.find-items", 1),
                    false),
                new PortableProvision(
                    PortableDependencyKind.ResourceFlavor,
                    PortableDependencyReference.Parse("interchange.tests.addressing-only-handle", 1),
                    false)
            ],
            [
                new PortableRequirement(
                    PortableDependencyKind.ResourceFlavor,
                    PortableDependencyReference.Parse("interchange.tests.addressing-only-handle", 1),
                    PortableRequirementStrength.Required,
                    false)
            ],
            [
                new PortableOperationDeclaration(Upsert, UpsertCommand, UpsertResult, Details, [], [flavor]),
                new PortableOperationDeclaration(Find, FindCommand, FindResult, Details, [], [flavor])
            ],
            [
                PortableShapeDeclaration.Record(
                    Item,
                    PortableFragmentPolicy.Closed,
                    new PortableFieldDeclaration("id", text, true),
                    new PortableFieldDeclaration("title", text, true),
                    new PortableFieldDeclaration("tags", TextSequence, true)),
                PortableShapeDeclaration.Sequence(ItemSequence, Item),
                PortableShapeDeclaration.Sequence(TextSequence, text),
                PortableShapeDeclaration.Record(
                    UpsertCommand,
                    PortableFragmentPolicy.Closed,
                    new PortableFieldDeclaration("items", ItemSequence, true)),
                PortableShapeDeclaration.Record(
                    UpsertResult,
                    PortableFragmentPolicy.Closed,
                    new PortableFieldDeclaration("stored", signed, true)),
                PortableShapeDeclaration.Record(
                    FindCommand,
                    PortableFragmentPolicy.Closed,
                    new PortableFieldDeclaration("ids", TextSequence, true)),
                PortableShapeDeclaration.Record(
                    FindResult,
                    PortableFragmentPolicy.Closed,
                    new PortableFieldDeclaration("items", ItemSequence, true)),
                PortableShapeDeclaration.Record(
                    Details,
                    PortableFragmentPolicy.Closed,
                    new PortableFieldDeclaration("code", text, true),
                    new PortableFieldDeclaration("message", text, true))
            ],
            [],
            new PortableAuthorityDeclaration(
                PortableAuthorityMode.CrossTrustNoCapabilityTransfer,
                true,
                true,
                PortableAuthorityDeclaration.OnlyPermittedConstraintPolicy),
            new PortableRepresentationDeclaration(
                PortableRepresentations.PortableCborCore,
                PortableRepresentations.LengthDelimited,
                [flavor],
                [AcceptedHandle]),
            PortableLimits.Declared,
            new PortableLifecycleDeclaration(
                true,
                "binding",
                ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, new Dictionary<string, bool>
                {
                    ["establishment"] = true,
                    ["readiness-signal"] = true,
                    ["single-invocation"] = true,
                    ["clean-withdrawal"] = true,
                    ["clean-termination"] = true,
                    ["retry"] = false,
                    ["cancellation"] = false,
                    ["streaming"] = false,
                    ["ordering-guarantee"] = false,
                    ["exactly-once-execution"] = false
                })));
    }
}

/// <summary>
/// The Catalog provider domain: one session's stored items, addressed by a provider-retained handle.
/// </summary>
/// <remarks>
/// Possession of the handle is not an admission decision. The handle names where to look; this
/// domain still decides for itself what to do, and reports a miss as a shaped failed Outcome.
/// </remarks>
public sealed class CatalogPortableHandler : IPortableOperationHandler
{
    private readonly Dictionary<string, PortableRecordValue> _items = new(StringComparer.Ordinal);

    public long ProviderEffectCount { get; private set; }

    public PortableOperationEffect Invoke(
        PortableOperationReference operation,
        PortableValue input,
        IReadOnlyList<PortableResource> resources)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(resources);
        var record = input as PortableRecordValue ?? throw PortableFaultException.InvalidPayload(
            "catalog-input",
            "A Catalog command is a record.");

        if (operation == CatalogPortableFixture.Upsert)
        {
            var items = (PortableSequenceValue)record.Fields["items"];
            foreach (var item in items.Items.Cast<PortableRecordValue>())
            {
                _items[((PortableTextValue)item.Fields["id"]).Value] = item;
            }

            ProviderEffectCount++;
            return PortableOperationEffect.Success(
                PortableRecordValue.Of(("stored", new PortableIntegerValue(items.Items.Length))),
                ProviderEffectCount);
        }

        var ids = ((PortableSequenceValue)record.Fields["ids"]).Items
            .Cast<PortableTextValue>()
            .Select(id => id.Value)
            .ToImmutableArray();
        var missing = ids.Where(id => !_items.ContainsKey(id)).ToImmutableArray();
        if (missing.Length > 0)
        {
            return PortableOperationEffect.Failure(
                PortableRecordValue.Of(
                    ("code", new PortableTextValue("catalog.items-missing")),
                    ("message", new PortableTextValue($"{missing.Length} requested item(s) are not stored."))),
                0);
        }

        ProviderEffectCount++;
        return PortableOperationEffect.Success(
            PortableRecordValue.Of(("items", new PortableSequenceValue([.. ids.Select(id => (PortableValue)_items[id])]))),
            ProviderEffectCount);
    }
}
