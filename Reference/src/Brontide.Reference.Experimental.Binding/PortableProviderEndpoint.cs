using System.Text.Json;
using Brontide.Reference.Core;

namespace Brontide.Reference.Experimental.Binding;

public sealed class PortableCoolingProviderEndpoint(
    string providerName,
    Func<CoolingCommand, CancellationToken, ValueTask<PortableProviderEffect>> invoke)
{
    private readonly PortableManifest _manifest = InterchangeCoolingContract.CreateManifest(providerName);

    public async Task<int> RunAsync(
        TextReader input,
        TextWriter output,
        bool crashAfterActivation = false,
        bool rejectProtocol = false,
        CancellationToken cancellationToken = default)
    {
        var activated = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await input.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                return 0;
            }

            BindingRequestId request = default;
            try
            {
                using var document = BoundaryJson.Parse(line);
                var root = document.RootElement;
                request = PortableProtocol.RequestId(root);
                var version = PortableProtocol.Required(root, "protocolVersion").GetInt32();
                if (version != PortableProtocol.Version)
                {
                    await WriteAsync(
                        output,
                        PortableProtocol.ProtocolError(request, "unsupported-protocol", $"Protocol version {version} is not supported."),
                        cancellationToken).ConfigureAwait(false);
                    continue;
                }

                switch (PortableProtocol.Required(root, "kind").GetString())
                {
                    case "activate":
                    {
                        if (rejectProtocol)
                        {
                            await WriteAsync(
                                output,
                                PortableProtocol.ProtocolError(
                                    request,
                                    "unsupported-protocol",
                                    "The provider deliberately rejected protocol version 2."),
                                cancellationToken).ConfigureAwait(false);
                            break;
                        }

                        var offered = ManifestCodec.Decode(PortableProtocol.Required(root, "manifest").GetRawText());
                        ManifestCodec.NegotiateExact(_manifest, offered);
                        ManifestCodec.NegotiateExact(offered, _manifest);
                        activated = true;
                        await WriteAsync(output, PortableProtocol.Activation(request, _manifest), cancellationToken)
                            .ConfigureAwait(false);
                        if (crashAfterActivation)
                        {
                            return 23;
                        }
                        break;
                    }
                    case "invoke" when !activated:
                        await WriteAsync(
                            output,
                            PortableProtocol.ProtocolError(request, "not-activated", "The provider has not negotiated a contract."),
                            cancellationToken).ConfigureAwait(false);
                        break;
                    case "invoke":
                    {
                        var execution = PortableProtocol.ExecutionId(root);
                        var occurrence = PortableProtocol.OccurrenceId(root);
                        RequireReference(root, "operation", new WireReference(InterchangeCoolingContract.Operation));
                        RequireReference(root, "inputShape", new WireReference(InterchangeCoolingContract.CommandShape));
                        RequireReference(root, "outputShape", new WireReference(InterchangeCoolingContract.ResultShape));
                        var inputElement = PortableProtocol.Required(root, "input");
                        var value = PortableShapeValueCodec.Decode(inputElement, InterchangeCoolingContract.CommandShape);
                        ValidateCommand(value);
                        var effect = await invoke(InterchangeCoolingContract.ReadCommand(value), cancellationToken)
                            .ConfigureAwait(false);
                        await WriteAsync(
                            output,
                            PortableProtocol.Outcome(
                                request,
                                execution,
                                occurrence,
                                effect,
                                inputElement,
                                _manifest.Provider),
                            cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    case "shutdown":
                        await WriteAsync(output, PortableProtocol.ShutdownAck(request), cancellationToken)
                            .ConfigureAwait(false);
                        return 0;
                    default:
                        await WriteAsync(
                            output,
                            PortableProtocol.ProtocolError(request, "unknown-message", "The protocol message kind is unknown."),
                            cancellationToken).ConfigureAwait(false);
                        break;
                }
            }
            catch (BoundaryNegotiationException exception)
            {
                await WriteAsync(
                    output,
                    PortableProtocol.ProtocolError(request, "incompatible-manifest", exception.Message),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (BoundaryProtocolException exception)
            {
                await WriteAsync(
                    output,
                    PortableProtocol.ProtocolError(request, "invalid-message", exception.Message),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await WriteAsync(
                    output,
                    PortableProtocol.ProtocolError(request, "provider-internal-failure", "The provider could not process the message."),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return 0;
    }

    private static void ValidateCommand(ShapeValue value)
    {
        var record = value as RecordShapeValue ??
            throw new BoundaryProtocolException("Cooling input must be a record.");
        _ = record.RequireField("loop").RequireScalar<string>();
        _ = record.RequireField("enabled").RequireScalar<bool>();
        if (!record.Fragments.TryGetValue(InterchangeCoolingContract.HostContext, out var context) ||
            !context.TryGetValue("requesterLabel", out var requester))
        {
            throw new BoundaryProtocolException(
                $"Required Fragment {InterchangeCoolingContract.HostContext.Name.Value}@{InterchangeCoolingContract.HostContext.Version} is missing.");
        }

        _ = requester.RequireScalar<string>();
    }

    private static void RequireReference(JsonElement root, string name, WireReference expected)
    {
        var actual = PortableProtocol.Reference(root, name);
        if (actual != expected)
        {
            throw new BoundaryProtocolException($"The message names unsupported {name} {actual}.");
        }
    }

    private static async Task WriteAsync(TextWriter output, string line, CancellationToken cancellationToken)
    {
        await output.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
