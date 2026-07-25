using System.Collections.Immutable;
using Brontide.Reference.Core;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>
/// The Cooling experiment restated as a fixture over the reusable portable layer.
/// </summary>
/// <remarks>
/// Cooling is now a consumer of the contract rather than its definition: everything below is data
/// and a handler, and nothing in the reusable layer knows a Cooling rule. The declarations mirror
/// the neutral fixture under <c>binding/portable/vectors/</c>, which is what lets the neutral
/// vectors be executed against this stack without either side importing the other.
/// </remarks>
public static class CoolingPortableFixture
{
    public static PortableComponentReference Component { get; } =
        PortableComponentReference.Parse("interchange.tests.cooling-component", 1);

    public static PortableProviderReference Provider { get; } =
        PortableProviderReference.Parse("interchange.tests.fixture-provider", 1);

    public static PortableOperationReference SetEnabled { get; } =
        PortableOperationReference.Parse("interchange.tests.cooling.set-enabled", 1);

    public static PortableShapeReference CommandV1 { get; } =
        PortableShapeReference.Parse("interchange.tests.cooling.command", 1);

    public static PortableShapeReference CommandV2 { get; } =
        PortableShapeReference.Parse("interchange.tests.cooling.command", 2);

    public static PortableShapeReference CommandV3 { get; } =
        PortableShapeReference.Parse("interchange.tests.cooling.command", 3);

    public static PortableShapeReference Result { get; } =
        PortableShapeReference.Parse("interchange.tests.cooling.result", 1);

    public static PortableShapeReference Details { get; } =
        PortableShapeReference.Parse("interchange.tests.cooling.details", 1);

    public static PortableFragmentReference HostContext { get; } =
        PortableFragmentReference.Parse("interchange.tests.cooling.host-context", 1);

    /// <summary>
    /// Declared by the contract but not by the negotiated Operation, which is what the closed
    /// fragment policy refuses rather than projects away.
    /// </summary>
    public static PortableFragmentReference Note { get; } =
        PortableFragmentReference.Parse("interchange.tests.cooling.note", 1);

    public static PortableShapeReference EncodingTags { get; } =
        PortableShapeReference.Parse("interchange.tests.encoding.tags", 1);

    public static PortableShapeReference EncodingTextSequence { get; } =
        PortableShapeReference.Parse("interchange.tests.encoding.text-sequence", 1);

    public static PortableShapeReference EncodingChoice { get; } =
        PortableShapeReference.Parse("interchange.tests.encoding.choice", 1);

    public static PortableShapeReference EncodingIntegers { get; } =
        PortableShapeReference.Parse("interchange.tests.encoding.integers", 1);

    public static PortableShapeReference EncodingScalars { get; } =
        PortableShapeReference.Parse("interchange.tests.encoding.scalars", 1);

    /// <summary>The subject contract, in the neutral form.</summary>
    public static PortableContractDocument Contract { get; } = BuildContract();

    /// <summary>
    /// The same contract with the streaming feature offered as a provision, which the fixture's
    /// opposed requirement must refuse rather than ignore.
    /// </summary>
    public static PortableContractDocument WithStreamingProvision() =>
        Contract with
        {
            Provisions = Contract.Provisions.Add(new PortableProvision(
                PortableDependencyKind.Feature,
                PortableDependencyReference.Parse("interchange.tests.streaming", 1),
                false))
        };

    /// <summary>The contract with the required cooling profile withdrawn from the provider's provisions.</summary>
    public static PortableContractDocument WithoutProfileProvision() =>
        Contract with
        {
            Provisions =
            [
                .. Contract.Provisions.Where(provision =>
                    provision.Reference != PortableDependencyReference.Parse("interchange.tests.cooling-profile", 1))
            ]
        };

    public static PortableRecordValue Command(
        string loop,
        bool enabled,
        string? failureMode = null,
        string? requesterLabel = "operator",
        string? requestedBy = null)
    {
        var command = PortableRecordValue.Of(
            ("loop", new PortableTextValue(loop)),
            ("enabled", new PortableBooleanValue(enabled)));
        if (failureMode is not null)
        {
            command = command.WithField("failureMode", new PortableTextValue(failureMode));
        }

        if (requestedBy is not null)
        {
            command = command.WithField("requestedBy", new PortableTextValue(requestedBy));
        }

        if (requesterLabel is not null)
        {
            command = command.WithFragment(HostContext, ("requesterLabel", new PortableTextValue(requesterLabel)));
        }

        return command;
    }

    /// <summary>Registers the Cooling Shapes in the stack's own registry, for adapter round trips.</summary>
    public static ShapeRegistry CreateNativeRegistry()
    {
        var registry = ShapeRegistry.CreateWithBuiltIns();
        registry.Register(ShapeDefinition.Record(
            PortableCoreAdapter.ToCoreShape(CommandV1),
            FragmentPolicy.Open,
            RecordField.Required("loop", BuiltInShapes.Text),
            RecordField.Required("enabled", BuiltInShapes.Boolean),
            RecordField.Optional("failureMode", BuiltInShapes.Text)));
        registry.Register(DeclaredFragmentDefinition.Attached(
            PortableCoreAdapter.ToCoreFragment(HostContext),
            PortableCoreAdapter.ToCoreShape(CommandV1),
            RecordField.Required("requesterLabel", BuiltInShapes.Text)));
        registry.Register(ShapeDefinition.Record(
            PortableCoreAdapter.ToCoreShape(Result),
            FragmentPolicy.Closed,
            RecordField.Required("loop", BuiltInShapes.Text),
            RecordField.Required("coolingEnabled", BuiltInShapes.Boolean),
            RecordField.Required("revision", BuiltInShapes.Signed64),
            RecordField.Required("providerEffectCount", BuiltInShapes.Signed64)));
        registry.Register(ShapeDefinition.Record(
            PortableCoreAdapter.ToCoreShape(Details),
            FragmentPolicy.Closed,
            RecordField.Required("code", BuiltInShapes.Text),
            RecordField.Required("message", BuiltInShapes.Text)));
        return registry;
    }

    private static PortableContractDocument BuildContract()
    {
        var text = PortableBuiltInShapes.Text;
        var boolean = PortableBuiltInShapes.Boolean;
        var signed = PortableBuiltInShapes.Signed64;

        return new PortableContractDocument(
            PortableContractDocument.SupportedContractVersion,
            Component,
            Provider,
            [
                new PortableProvision(
                    PortableDependencyKind.Operation,
                    PortableDependencyReference.Parse("interchange.tests.cooling.set-enabled", 1),
                    false),
                new PortableProvision(
                    PortableDependencyKind.Profile,
                    PortableDependencyReference.Parse("interchange.tests.cooling-profile", 1),
                    false),
                new PortableProvision(
                    PortableDependencyKind.Binding,
                    PortableDependencyReference.Parse("interchange.tests.portable-cbor-core", 1),
                    true),
                new PortableProvision(
                    PortableDependencyKind.ResourceFlavor,
                    PortableDependencyReference.Parse("interchange.tests.copied-immutable-blob", 1),
                    false)
            ],
            [
                new PortableRequirement(
                    PortableDependencyKind.Profile,
                    PortableDependencyReference.Parse("interchange.tests.cooling-profile", 1),
                    PortableRequirementStrength.Required,
                    false),
                new PortableRequirement(
                    PortableDependencyKind.Binding,
                    PortableDependencyReference.Parse("interchange.tests.portable-cbor-core", 1),
                    PortableRequirementStrength.Required,
                    true),
                new PortableRequirement(
                    PortableDependencyKind.Feature,
                    PortableDependencyReference.Parse("interchange.tests.streaming", 1),
                    PortableRequirementStrength.Opposed,
                    false)
            ],
            [
                new PortableOperationDeclaration(
                    SetEnabled,
                    CommandV1,
                    Result,
                    Details,
                    [HostContext],
                    [PortableResourceFlavors.CopiedImmutableBlob])
            ],
            [
                PortableShapeDeclaration.Record(
                    CommandV1,
                    PortableFragmentPolicy.Open,
                    new PortableFieldDeclaration("loop", text, true),
                    new PortableFieldDeclaration("enabled", boolean, true),
                    new PortableFieldDeclaration("failureMode", text, false)),
                PortableShapeDeclaration.Record(
                    CommandV2,
                    PortableFragmentPolicy.Open,
                    new PortableFieldDeclaration("loop", text, true),
                    new PortableFieldDeclaration("enabled", boolean, true),
                    new PortableFieldDeclaration("failureMode", text, false),
                    new PortableFieldDeclaration("requestedBy", text, false)),
                PortableShapeDeclaration.Record(
                    CommandV3,
                    PortableFragmentPolicy.Open,
                    new PortableFieldDeclaration("loop", text, true),
                    new PortableFieldDeclaration("enabled", boolean, true),
                    new PortableFieldDeclaration("reason", text, true)),
                PortableShapeDeclaration.Record(
                    Result,
                    PortableFragmentPolicy.Closed,
                    new PortableFieldDeclaration("loop", text, true),
                    new PortableFieldDeclaration("coolingEnabled", boolean, true),
                    new PortableFieldDeclaration("revision", signed, true),
                    new PortableFieldDeclaration("providerEffectCount", signed, true)),
                PortableShapeDeclaration.Record(
                    Details,
                    PortableFragmentPolicy.Closed,
                    new PortableFieldDeclaration("code", text, true),
                    new PortableFieldDeclaration("message", text, true)),
                PortableShapeDeclaration.Record(
                    EncodingTags,
                    PortableFragmentPolicy.Closed,
                    new PortableFieldDeclaration("tags", EncodingTextSequence, true)),
                PortableShapeDeclaration.Sequence(EncodingTextSequence, text),
                PortableShapeDeclaration.Choice(
                    EncodingChoice,
                    new PortableAlternativeDeclaration("text", text),
                    new PortableAlternativeDeclaration("count", signed)),
                PortableShapeDeclaration.Record(
                    EncodingIntegers,
                    PortableFragmentPolicy.Closed,
                    [.. Enumerable.Range(0, 10).Select(index =>
                        new PortableFieldDeclaration($"i{index:00}", signed, true))]),
                PortableShapeDeclaration.Record(
                    EncodingScalars,
                    PortableFragmentPolicy.Closed,
                    new PortableFieldDeclaration("b", PortableBuiltInShapes.Bytes, true),
                    new PortableFieldDeclaration("d", PortableBuiltInShapes.Decimal, true),
                    new PortableFieldDeclaration("u", PortableBuiltInShapes.Unit, true))
            ],
            [
                new PortableFragmentDeclaration(
                    HostContext,
                    CommandV1,
                    [new PortableFieldDeclaration("requesterLabel", PortableBuiltInShapes.Text, true)]),
                new PortableFragmentDeclaration(
                    Note,
                    Result,
                    [new PortableFieldDeclaration("note", PortableBuiltInShapes.Text, true)])
            ],
            new PortableAuthorityDeclaration(
                PortableAuthorityMode.CrossTrustNoCapabilityTransfer,
                true,
                true,
                PortableAuthorityDeclaration.OnlyPermittedConstraintPolicy),
            new PortableRepresentationDeclaration(
                PortableRepresentations.PortableCborCore,
                PortableRepresentations.LengthDelimited,
                [PortableResourceFlavors.CopiedImmutableBlob],
                []),
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
/// The Cooling provider domain.
/// </summary>
/// <remarks>
/// The handler is the provider's own domain: for a cross-trust presentation it receives only
/// attributable context and exact addressing and decides for itself, reporting a refusal as a
/// shaped failed Outcome rather than as a protocol rejection.
/// </remarks>
public sealed class CoolingPortableHandler(ShapeRegistry registry) : IPortableOperationHandler
{
    private readonly ShapeRegistry _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    private long _revision;

    public long ProviderEffectCount { get; private set; }

    public PortableOperationEffect Invoke(
        PortableOperationReference operation,
        PortableValue input,
        IReadOnlyList<PortableResource> resources)
    {
        ArgumentNullException.ThrowIfNull(input);

        // The command crosses into the stack's own model here, which is the point of the adapter:
        // the domain logic below never sees a portable type.
        var native = PortableCoreAdapter.ToCore(
            input,
            PortableCoreAdapter.ToCoreShape(CoolingPortableFixture.CommandV1),
            _registry);
        var loop = native.RequireField("loop").RequireScalar<string>();
        var enabled = native.RequireField("enabled").RequireScalar<bool>();
        var failureMode = native is RecordShapeValue record && record.Fields.TryGetValue("failureMode", out var mode)
            ? mode.RequireScalar<string>()
            : null;

        if (failureMode is not null)
        {
            return PortableOperationEffect.Failure(
                PortableRecordValue.Of(
                    ("code", new PortableTextValue(failureMode)),
                    ("message", new PortableTextValue($"The cooling loop '{loop}' refused the command."))),
                0);
        }

        _revision++;
        ProviderEffectCount++;
        return PortableOperationEffect.Success(
            PortableRecordValue.Of(
                ("loop", new PortableTextValue(loop)),
                ("coolingEnabled", new PortableBooleanValue(enabled)),
                ("revision", new PortableIntegerValue(_revision)),
                ("providerEffectCount", new PortableIntegerValue(ProviderEffectCount))),
            ProviderEffectCount);
    }
}
