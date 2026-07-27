using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using PortableBinding.NeutralProvider;

// The implementation-neutral provider endpoint for the Portable Component Binding.
//
// It serves the same contract the Reference and Minimal stacks serve, over the same length-delimited
// deterministic-CBOR wire, and it imports neither stack. Its verbs match theirs so a host can drive
// it with no host-side change: --portable serves the Cooling contract, --portable --catalog the
// Catalog one.
//
// What it proves is C10: that the published contract is implementable from its published form. The
// contract document it answers with is transcoded from the checked-in neutral declaration rather
// than restated here, and the CBOR is written and read by the base class library's codec rather
// than by either stack's.

var catalog = args.Contains("--catalog", StringComparer.Ordinal);
var contractPath = Path.Combine(
    AppContext.BaseDirectory,
    "contracts",
    catalog ? "catalog-fixture-contract.json" : "fixture-contract.json");
var contract = ServedContract.Load(contractPath);
var handler = catalog ? new CatalogDomain() : (IDomain)new CoolingDomain();

using var input = Console.OpenStandardInput();
using var output = Console.OpenStandardOutput();
var endpoint = new Endpoint(contract, handler);

while (true)
{
    var frame = Framing.Read(input);
    if (frame is null)
    {
        // A clean end between frames is the peer terminating. Nothing is left to answer.
        return 0;
    }

    Cbor reply;
    try
    {
        var envelope = Cbor.Decode(frame);
        var answers = endpoint.Handle(envelope, out var terminate);
        foreach (var answer in answers)
        {
            Framing.Write(output, answer.Encode());
        }

        if (terminate)
        {
            return 0;
        }

        continue;
    }
    catch (PortableFault fault)
    {
        reply = Envelopes.ProtocolError(endpoint.ChannelId, endpoint.LastRequestId, fault);
    }
#pragma warning disable CA1031 // The endpoint's own runtime failure must cross as a category, never as a type.
    catch (Exception)
#pragma warning restore CA1031
    {
        reply = Envelopes.ProtocolError(
            endpoint.ChannelId,
            endpoint.LastRequestId,
            new PortableFault(
                "internal-protocol-failure",
                "endpoint-failure",
                "The endpoint cannot continue protocol processing."));
    }

    Framing.Write(output, reply.Encode());
    return 0;
}

namespace PortableBinding.NeutralProvider
{
    /// <summary>4-byte big-endian length prefix, bounded, exactly as the representation declares.</summary>
    internal static class Framing
    {
        private const int MaxFrameBytes = 65536;

        public static byte[]? Read(Stream stream)
        {
            var prefix = new byte[4];
            if (!ReadExactly(stream, prefix))
            {
                return null;
            }

            var length = BinaryPrimitives.ReadUInt32BigEndian(prefix);
            if (length > MaxFrameBytes)
            {
                throw new PortableFault(
                    "limit-exceeded",
                    "frame-bound",
                    $"A length prefix declaring {length} bytes exceeds the declared bound of {MaxFrameBytes}.");
            }

            var body = new byte[length];
            return ReadExactly(stream, body)
                ? body
                : throw new PortableFault("malformed-message", "frame-truncated", "A frame ended inside its body.");
        }

        public static void Write(Stream stream, byte[] body)
        {
            var prefix = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(prefix, (uint)body.Length);
            stream.Write(prefix);
            stream.Write(body);
            stream.Flush();
        }

        private static bool ReadExactly(Stream stream, Span<byte> buffer)
        {
            var read = 0;
            while (read < buffer.Length)
            {
                var count = stream.Read(buffer[read..]);
                if (count == 0)
                {
                    return read == 0 ? false : throw new PortableFault(
                        "malformed-message",
                        "frame-truncated",
                        "The stream ended inside a frame.");
                }

                read += count;
            }

            return true;
        }
    }

    /// <summary>Envelope construction, in the field names the neutral envelope schema declares.</summary>
    internal static class Envelopes
    {
        public const int ContractVersion = 1;

        public static Cbor Control(string kind, string channelId, Cbor body) =>
            Cbor.Map(
                ("contractVersion", Cbor.Integer(ContractVersion)),
                ("kind", Cbor.Text(kind)),
                ("channelId", Cbor.Text(channelId)),
                ("body", body));

        public static Cbor Correlated(string kind, string channelId, string? requestId, string? executionId, Cbor body)
        {
            var entries = new List<(string, Cbor)>
            {
                ("contractVersion", Cbor.Integer(ContractVersion)),
                ("kind", Cbor.Text(kind)),
                ("channelId", Cbor.Text(channelId)),
                ("body", body)
            };

            if (requestId is not null)
            {
                entries.Add(("requestId", Cbor.Text(requestId)));
            }

            if (executionId is not null)
            {
                entries.Add(("executionId", Cbor.Text(executionId)));
            }

            return Cbor.Map(entries);
        }

        public static Cbor ProtocolError(string channelId, string? requestId, PortableFault fault) =>
            Correlated(
                "protocol-error",
                channelId,
                requestId,
                null,
                Cbor.Map(
                    ("category", Cbor.Text(fault.Category)),
                    ("localCode", Cbor.Text(fault.LocalCode)),
                    ("failureDomain", Cbor.Text("remote-endpoint"))));
    }

    /// <summary>The provider's domain behind the binding.</summary>
    internal interface IDomain
    {
        /// <summary>Answers one request, as a succeeded result or a shaped failed detail.</summary>
        (bool Succeeded, Cbor Value, long ProviderEffectCount) Invoke(string operation, Cbor input);
    }

    /// <summary>
    /// The lifecycle, negotiation, conformance, and dispatch this endpoint performs.
    /// </summary>
    /// <remarks>
    /// It is deliberately the smallest endpoint that answers the contract honestly: it refuses what
    /// the contract says must be refused, with the portable category the contract names, and it
    /// fabricates no success.
    /// </remarks>
    internal sealed class Endpoint(ServedContract contract, IDomain domain)
    {
        private readonly HashSet<string> _seenRequests = new(StringComparer.Ordinal);
        private string _state = "unestablished";

        public string ChannelId { get; private set; } = "unestablished";

        public string? LastRequestId { get; private set; }

        public IReadOnlyList<Cbor> Handle(Cbor envelope, out bool terminate)
        {
            terminate = false;
            if (envelope.IntegerField("contractVersion", "envelope") != Envelopes.ContractVersion)
            {
                throw new PortableFault("unsupported-version", "contract-version", "The declared version is unknown.");
            }

            var kind = envelope.TextField("kind", "envelope");
            ChannelId = envelope.TextField("channelId", "envelope");
            LastRequestId = envelope.Has("requestId") ? envelope.TextField("requestId", "envelope") : null;

            switch (kind)
            {
                case "establish":
                    RequireState("unestablished", kind);
                    _state = "ready";
                    return
                    [
                        Envelopes.Control(
                            "establish-accepted",
                            ChannelId,
                            Cbor.Map(
                                ("contract", contract.Encode()),
                                ("compactIdentifiers", Cbor.Array()))),
                        Envelopes.Control("ready", ChannelId, Cbor.Map())
                    ];
                case "request":
                    RequireState("ready", kind);
                    return [Answer(envelope)];
                case "withdraw":
                    RequireState("ready", kind);
                    _state = "withdrawn";
                    return [];
                case "terminate":
                    terminate = true;
                    return [];
                default:
                    throw new PortableFault("unsupported-kind", "envelope-kind", $"'{kind}' is not an envelope kind.");
            }
        }

        private void RequireState(string expected, string kind)
        {
            if (_state != expected)
            {
                throw new PortableFault(
                    "state-violation",
                    "illegal-transition",
                    $"A '{kind}' frame is illegal in state '{_state}'.");
            }
        }

        private Cbor Answer(Cbor envelope)
        {
            var requestId = LastRequestId
                ?? throw new PortableFault("malformed-message", "request-id", "A request carries a request identity.");
            if (!_seenRequests.Add(requestId))
            {
                throw new PortableFault("replay-detected", "replay", "The request identity was already accepted.");
            }

            var body = envelope.Field("body", "request");

            // The authority scan runs before the body is given a Shape, so authority-bearing content
            // is refused as an authority presentation rather than as an undeclared field.
            if (contract.TrustBoundaryCrossed)
            {
                Values.RequireNoCapabilityContent(body.Field("input", "request"));
            }

            var operationReference = body.Field("operation", "request").Reference("operation");
            var declaration = contract.Operation(operationReference.Name, operationReference.Version);
            var inputShape = body.Field("inputShape", "request").Reference("inputShape");

            foreach (var resource in body.Field("resources", "request").Items("resources"))
            {
                Resources.Admit(resource, declaration, contract);
            }

            var projected = Values.Project(
                body.Field("input", "request"),
                inputShape,
                declaration.InputShape,
                contract);
            Values.Validate(projected, declaration.InputShape, contract, declaration.RequiredFragments);

            var (succeeded, value, effects) = domain.Invoke(declaration.Name, projected);
            var valueShape = succeeded ? declaration.ResultShape : declaration.DetailShape;
            return Envelopes.Correlated(
                "outcome",
                ChannelId,
                requestId,
                envelope.Has("executionId") ? envelope.TextField("executionId", "envelope") : null,
                Cbor.Map(
                    ("status", Cbor.Text(succeeded ? "succeeded" : "failed")),
                    ("valueShape", Cbor.Reference(valueShape.Name, valueShape.Version)),
                    ("value", value),
                    ("providerEffectCount", Cbor.Integer(effects))));
        }
    }

    /// <summary>Schema-guided value handling: the Shape decides what each position means.</summary>
    internal static class Values
    {
        private static readonly string[] AuthorityBearing =
            ["capability", "constraint", "authority", "derivationChain", "capabilities"];

        /// <summary>A record is a two-element array of a field map and a Fragment map.</summary>
        public static (Cbor Fields, Cbor Fragments) Record(Cbor value, string context)
        {
            var items = value.Items(context);
            return items.Count == 2
                ? (items[0], items[1])
                : throw new PortableFault("invalid-payload", context, $"'{context}' is not a record.");
        }

        public static void RequireNoCapabilityContent(Cbor value)
        {
            switch (value)
            {
                case CborMapValue map:
                    foreach (var entry in map.Entries)
                    {
                        if (AuthorityBearing.Contains(entry.Key, StringComparer.OrdinalIgnoreCase))
                        {
                            throw new PortableFault(
                                "invalid-authority-presentation",
                                "capability-in-body",
                                $"Member '{entry.Key}' presents authority across a trust boundary that carries none.");
                        }

                        RequireNoCapabilityContent(entry.Value);
                    }

                    break;
                case CborArrayValue array:
                    foreach (var item in array.Items)
                    {
                        RequireNoCapabilityContent(item);
                    }

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Projects a presented value onto the version this endpoint recognizes, dropping the fields
        /// the target does not declare. Only an additive difference projects; anything else is a
        /// payload the contract cannot accept.
        /// </summary>
        public static Cbor Project(
            Cbor value,
            (string Name, int Version) presented,
            (string Name, int Version) declared,
            ServedContract contract)
        {
            if (presented == declared)
            {
                return value;
            }

            if (presented.Name != declared.Name || presented.Version < declared.Version)
            {
                throw new PortableFault(
                    "invalid-payload",
                    "non-additive-skew",
                    $"'{presented.Name}@{presented.Version}' does not project onto '{declared.Name}@{declared.Version}'.");
            }

            var target = contract.Shape(declared);
            var (fields, fragments) = Record(value, "input");
            var retained = fields
                .Entries("input-fields")
                .Where(entry => target.Fields.Any(field => field.Name == entry.Key));
            return Cbor.Array(Cbor.Map(retained), fragments);
        }

        public static void Validate(
            Cbor value,
            (string Name, int Version) reference,
            ServedContract contract,
            IReadOnlyList<(string Name, int Version)> requiredFragments)
        {
            var shape = contract.Shape(reference);
            switch (shape.Kind)
            {
                case "record":
                {
                    var (fields, fragments) = Record(value, shape.Name);
                    var present = fields.Entries("fields");
                    foreach (var entry in present)
                    {
                        var declared = shape.Fields.FirstOrDefault(field => field.Name == entry.Key);
                        if (declared.Name is null)
                        {
                            throw new PortableFault(
                                "invalid-payload",
                                "undeclared-field",
                                $"Shape '{shape.Name}' declares no field '{entry.Key}'.");
                        }

                        ValidateMember(entry.Value, declared.Shape, contract);
                    }

                    foreach (var required in shape.Fields.Where(field => field.Required))
                    {
                        if (!present.Any(entry => entry.Key == required.Name))
                        {
                            throw new PortableFault(
                                "invalid-payload",
                                "required-field-absent",
                                $"Shape '{shape.Name}' requires field '{required.Name}'.");
                        }
                    }

                    // Attribution is never inferred from delivery, so a required Fragment must be
                    // present rather than assumed from the fact that the request arrived.
                    var attached = fragments.Entries("fragments").Select(entry => entry.Key).ToList();
                    foreach (var fragment in requiredFragments)
                    {
                        if (!attached.Contains($"{fragment.Name}@{fragment.Version}", StringComparer.Ordinal))
                        {
                            throw new PortableFault(
                                "invalid-payload",
                                "required-fragment-absent",
                                $"The Operation requires Fragment '{fragment.Name}@{fragment.Version}'.");
                        }
                    }

                    break;
                }
                case "sequence":
                {
                    foreach (var item in value.Items(shape.Name))
                    {
                        ValidateMember(item, shape.ItemShape!.Value, contract);
                    }

                    break;
                }
                default:
                    break;
            }
        }

        private static void ValidateMember(Cbor value, (string Name, int Version) reference, ServedContract contract)
        {
            switch (reference.Name)
            {
                case "Text":
                    if (value is not CborTextValue)
                    {
                        throw new PortableFault("invalid-payload", "shape-mismatch", "A declared Text position is not text.");
                    }

                    return;
                case "Boolean":
                    if (value is not CborBooleanValue)
                    {
                        throw new PortableFault("invalid-payload", "shape-mismatch", "A declared Boolean position is not a boolean.");
                    }

                    return;
                case "Integer.Signed64":
                    if (value is not CborIntegerValue)
                    {
                        throw new PortableFault("invalid-payload", "shape-mismatch", "A declared Integer position is not an integer.");
                    }

                    return;
                default:
                    Validate(value, reference, contract, []);
                    return;
            }
        }
    }

    /// <summary>Referenced-resource admission for the two declared flavors.</summary>
    internal static class Resources
    {
        public static void Admit(Cbor resource, OperationDeclaration operation, ServedContract contract)
        {
            var flavor = resource.TextField("flavor", "resource");
            if (!operation.ResourceFlavors.Contains(flavor, StringComparer.Ordinal) ||
                !contract.ResourceFlavors.Contains(flavor, StringComparer.Ordinal))
            {
                throw new PortableFault(
                    "unsupported-contract",
                    "resource-flavor-unnegotiated",
                    $"The established contract negotiated no resource flavor '{flavor}'.");
            }

            switch (flavor)
            {
                case "copied-immutable-blob":
                {
                    if (resource.Has("release"))
                    {
                        throw new PortableFault(
                            "state-violation",
                            "release-signal",
                            "The copied flavor defines no release signal, so a frame carrying one is illegal.");
                    }

                    var content = resource.Field("content", "resource") is CborBytesValue bytes
                        ? bytes.Value
                        : throw new PortableFault("invalid-payload", "resource-content", "A blob carries its octets.");
                    if (content.Length > contract.MaxResourceBytes)
                    {
                        throw new PortableFault("limit-exceeded", "resource-bound", "The resource exceeds the declared bound.");
                    }

                    var declared = resource.TextField("integrity", "resource");
                    var actual = Convert.ToHexStringLower(SHA256.HashData(content));
                    if (!string.Equals(declared, actual, StringComparison.Ordinal))
                    {
                        throw new PortableFault(
                            "invalid-payload",
                            "integrity-mismatch",
                            "The received octets hash to a different value than the resource declares.");
                    }

                    return;
                }
                case "addressing-only-handle":
                {
                    if (resource.Has("content"))
                    {
                        throw new PortableFault(
                            "invalid-payload",
                            "forbidden-implicit-copy",
                            "A handle addresses a resource; octets alongside one would be an implicit copy.");
                    }

                    var handle = $"{resource.TextField("provider", "resource")}/{resource.TextField("id", "resource")}";
                    if (!contract.AcceptedResourceHandles.Contains(handle, StringComparer.Ordinal))
                    {
                        throw new PortableFault(
                            "invalid-payload",
                            "resource-refused",
                            $"The Binding Plan accepts no handle '{handle}'.");
                    }

                    return;
                }
                default:
                    throw new PortableFault("unsupported-contract", "resource-flavor", $"Flavor '{flavor}' is unknown.");
            }
        }
    }

    /// <summary>The Cooling domain, as a consumer of the contract rather than a definition of it.</summary>
    internal sealed class CoolingDomain : IDomain
    {
        private long _revision;
        private long _effects;

        public (bool Succeeded, Cbor Value, long ProviderEffectCount) Invoke(string operation, Cbor input)
        {
            var (fields, _) = Values.Record(input, "command");
            var loop = fields.TextField("loop", "command");
            var enabled = fields.Field("enabled", "command") is CborBooleanValue flag && flag.Value;
            var failureMode = fields.Has("failureMode") ? fields.TextField("failureMode", "command") : null;

            if (failureMode is not null)
            {
                // The provider's own domain refused. That is a shaped failed Outcome, not a protocol
                // rejection, and it carries no effect.
                return (
                    false,
                    Record(
                        ("code", Cbor.Text(failureMode)),
                        ("message", Cbor.Text($"The cooling loop '{loop}' refused the command."))),
                    0);
            }

            _revision++;
            _effects++;
            return (
                true,
                Record(
                    ("loop", Cbor.Text(loop)),
                    ("coolingEnabled", Cbor.Boolean(enabled)),
                    ("revision", Cbor.Integer(_revision)),
                    ("providerEffectCount", Cbor.Integer(_effects))),
                _effects);
        }

        internal static Cbor Record(params (string Key, Cbor Value)[] fields) =>
            Cbor.Array(Cbor.Map(fields), Cbor.Map());
    }

    /// <summary>The Catalog domain: multiple Operations over one session's ordered state.</summary>
    internal sealed class CatalogDomain : IDomain
    {
        private readonly Dictionary<string, Cbor> _stored = new(StringComparer.Ordinal);
        private long _effects;

        public (bool Succeeded, Cbor Value, long ProviderEffectCount) Invoke(string operation, Cbor input)
        {
            var (fields, _) = Values.Record(input, "command");
            if (operation.EndsWith("upsert-items", StringComparison.Ordinal))
            {
                var items = fields.Field("items", "command").Items("items");
                foreach (var item in items)
                {
                    var (itemFields, _) = Values.Record(item, "item");
                    _stored[itemFields.TextField("id", "item")] = item;
                }

                _effects++;
                return (true, CoolingDomain.Record(("stored", Cbor.Integer(items.Count))), _effects);
            }

            var ids = fields.Field("ids", "command").Items("ids");
            var found = new List<Cbor>();
            var missing = new List<string>();
            foreach (var id in ids)
            {
                var key = id is CborTextValue text
                    ? text.Value
                    : throw new PortableFault("invalid-payload", "id", "An identifier is text.");
                if (_stored.TryGetValue(key, out var item))
                {
                    found.Add(item);
                }
                else
                {
                    missing.Add(key);
                }
            }

            if (missing.Count > 0)
            {
                return (
                    false,
                    CoolingDomain.Record(
                        ("code", Cbor.Text("missing-items")),
                        ("message", Cbor.Text($"No item is stored for {string.Join(", ", missing)}."))),
                    0);
            }

            _effects++;
            return (true, CoolingDomain.Record(("items", Cbor.Array(found))), _effects);
        }
    }
}
