using System.Collections.Immutable;
using Brontide.Reference.Core;

namespace Brontide.Reference.Experimental.Binding;

public static class InterchangeCoolingContract
{
    public const int ProtocolVersion = 2;
    public static readonly WireReference Component = new("interchange.tests.cooling-component", 1);
    public static readonly WireReference Profile = new("interchange.tests.cooling-profile", 1);
    public static readonly WireReference InlineJsonBinding = new("interchange.tests.inline-tagged-json", 1);
    public static readonly OperationReference Operation = OperationReference.Parse("interchange.tests.cooling.set-enabled");
    public static readonly ShapeReference CommandShape = ShapeReference.Parse("interchange.tests.cooling.command", 1);
    public static readonly ShapeReference ResultShape = ShapeReference.Parse("interchange.tests.cooling.result", 1);
    public static readonly ShapeReference DetailsShape = ShapeReference.Parse("interchange.tests.cooling.details", 1);
    public static readonly FragmentReference HostContext = FragmentReference.Parse("interchange.tests.cooling.host-context", 1);
    public static readonly FragmentReference OptionalForwardingNote = FragmentReference.Parse("third-party.cooling.note", 1);

    public static PortableManifest CreateManifest(string providerName, int protocolVersion = ProtocolVersion)
    {
        var operation = new ManifestOperation(
            new WireReference(Operation),
            new WireReference(CommandShape),
            new WireReference(ResultShape),
            [new WireReference(HostContext)],
            new AuthorityRequirement(true, "fail-closed"));
        return new PortableManifest(
            protocolVersion,
            Component,
            new WireReference(providerName, 1),
            [operation],
            [
                new ManifestShape(
                    new WireReference(CommandShape),
                    "record",
                    "open",
                    [
                        new ManifestField("loop", new WireReference("Text", 1), true),
                        new ManifestField("enabled", new WireReference("Boolean", 1), true),
                        new ManifestField("failureMode", new WireReference("Text", 1), false)
                    ]),
                new ManifestShape(
                    new WireReference(ResultShape),
                    "record",
                    "closed",
                    [
                        new ManifestField("loop", new WireReference("Text", 1), true),
                        new ManifestField("coolingEnabled", new WireReference("Boolean", 1), true),
                        new ManifestField("revision", new WireReference("Integer.Signed64", 1), true),
                        new ManifestField("providerEffectCount", new WireReference("Integer.Signed64", 1), true)
                    ]),
                new ManifestShape(
                    new WireReference(DetailsShape),
                    "record",
                    "closed",
                    [
                        new ManifestField("code", new WireReference("Text", 1), true),
                        new ManifestField("message", new WireReference("Text", 1), true)
                    ])
            ],
            [
                new ManifestFragment(
                    new WireReference(HostContext),
                    new WireReference(CommandShape),
                    [new ManifestField("requesterLabel", new WireReference("Text", 1), true)])
            ],
            [
                new ManifestDependency("profile", Profile, "required", false),
                new ManifestDependency("binding", InlineJsonBinding, "required", true)
            ],
            new BindingDeclaration(
                ["inline-tagged-json"],
                ["process"],
                ["single-invocation", "no-capability-transfer", "no-referenced-resources"]));
    }

    public static void RegisterShapes(ShapeRegistry registry, bool includeOptionalLocalFragment)
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Register(ShapeDefinition.Record(
            CommandShape,
            FragmentPolicy.Open,
            RecordField.Required("loop", BuiltInShapes.Text),
            RecordField.Required("enabled", BuiltInShapes.Boolean),
            RecordField.Optional("failureMode", BuiltInShapes.Text)));
        registry.Register(DeclaredFragmentDefinition.Attached(
            HostContext,
            CommandShape,
            RecordField.Required("requesterLabel", BuiltInShapes.Text)));
        if (includeOptionalLocalFragment)
        {
            registry.Register(DeclaredFragmentDefinition.Attached(
                OptionalForwardingNote,
                CommandShape,
                RecordField.Required("note", BuiltInShapes.Text)));
        }

        registry.Register(ShapeDefinition.Record(
            ResultShape,
            FragmentPolicy.Closed,
            RecordField.Required("loop", BuiltInShapes.Text),
            RecordField.Required("coolingEnabled", BuiltInShapes.Boolean),
            RecordField.Required("revision", BuiltInShapes.Signed64),
            RecordField.Required("providerEffectCount", BuiltInShapes.Signed64)));
        registry.Register(ShapeDefinition.Record(
            DetailsShape,
            FragmentPolicy.Closed,
            RecordField.Required("code", BuiltInShapes.Text),
            RecordField.Required("message", BuiltInShapes.Text)));
    }

    public static void RegisterShapes(
        AuthorityDomain.GenesisContext genesis,
        bool includeOptionalLocalFragment)
    {
        ArgumentNullException.ThrowIfNull(genesis);
        genesis.Shape(ShapeDefinition.Record(
            CommandShape,
            FragmentPolicy.Open,
            RecordField.Required("loop", BuiltInShapes.Text),
            RecordField.Required("enabled", BuiltInShapes.Boolean),
            RecordField.Optional("failureMode", BuiltInShapes.Text)));
        genesis.Shape(DeclaredFragmentDefinition.Attached(
            HostContext,
            CommandShape,
            RecordField.Required("requesterLabel", BuiltInShapes.Text)));
        if (includeOptionalLocalFragment)
        {
            genesis.Shape(DeclaredFragmentDefinition.Attached(
                OptionalForwardingNote,
                CommandShape,
                RecordField.Required("note", BuiltInShapes.Text)));
        }

        genesis.Shape(ShapeDefinition.Record(
            ResultShape,
            FragmentPolicy.Closed,
            RecordField.Required("loop", BuiltInShapes.Text),
            RecordField.Required("coolingEnabled", BuiltInShapes.Boolean),
            RecordField.Required("revision", BuiltInShapes.Signed64),
            RecordField.Required("providerEffectCount", BuiltInShapes.Signed64)));
        genesis.Shape(ShapeDefinition.Record(
            DetailsShape,
            FragmentPolicy.Closed,
            RecordField.Required("code", BuiltInShapes.Text),
            RecordField.Required("message", BuiltInShapes.Text)));
    }

    public static ShapeValue Command(
        string loop,
        bool enabled,
        string? failureMode = null,
        string? requesterLabel = null,
        string? forwardingNote = null)
    {
        var fields = new List<(string Name, ShapeValue Value)>
        {
            ("loop", ShapeValue.Text(loop)),
            ("enabled", ShapeValue.Boolean(enabled))
        };
        if (failureMode is not null)
        {
            fields.Add(("failureMode", ShapeValue.Text(failureMode)));
        }

        var fragments = new List<(FragmentReference Fragment, IReadOnlyDictionary<string, ShapeValue> Fields)>();
        if (requesterLabel is not null)
        {
            fragments.Add((HostContext, new Dictionary<string, ShapeValue>(StringComparer.Ordinal)
            {
                ["requesterLabel"] = ShapeValue.Text(requesterLabel)
            }));
        }

        if (forwardingNote is not null)
        {
            fragments.Add((OptionalForwardingNote, new Dictionary<string, ShapeValue>(StringComparer.Ordinal)
            {
                ["note"] = ShapeValue.Text(forwardingNote)
            }));
        }

        return ShapeValue.Record(CommandShape, fields, fragments);
    }

    public static CoolingCommand ReadCommand(ShapeValue input)
    {
        var record = input as RecordShapeValue ??
            throw new BoundaryProtocolException("Cooling input must be a record ShapeValue.");
        var loop = record.RequireField("loop").RequireScalar<string>();
        var enabled = record.RequireField("enabled").RequireScalar<bool>();
        var failureMode = record.Fields.TryGetValue("failureMode", out var value)
            ? value.RequireScalar<string>()
            : null;
        return new CoolingCommand(loop, enabled, failureMode);
    }

    public static ShapeValue Result(string loop, bool enabled, long revision, long effectCount) =>
        ShapeValue.Record(
            ResultShape,
            ("loop", ShapeValue.Text(loop)),
            ("coolingEnabled", ShapeValue.Boolean(enabled)),
            ("revision", ShapeValue.Signed64(revision)),
            ("providerEffectCount", ShapeValue.Signed64(effectCount)));

    public static ShapeValue Details(string code, string message) =>
        ShapeValue.Record(
            DetailsShape,
            ("code", ShapeValue.Text(code)),
            ("message", ShapeValue.Text(message)));
}

public sealed record CoolingCommand(string Loop, bool Enabled, string? FailureMode);
