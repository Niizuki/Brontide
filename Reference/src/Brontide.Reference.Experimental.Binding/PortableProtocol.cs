using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Brontide.Reference.Core;

namespace Brontide.Reference.Experimental.Binding;

public static class PortableProtocol
{
    public const int Version = 2;

    internal static string Activate(BindingRequestId request, PortableManifest manifest) =>
        Write(writer =>
        {
            Start(writer, "activate", request);
            writer.WritePropertyName("manifest");
            WriteJson(writer, ManifestCodec.Encode(manifest));
            writer.WriteEndObject();
        });

    internal static string Invoke(
        BindingRequestId request,
        BindingExecutionId execution,
        BindingOccurrenceId occurrence,
        ShapeValue input)
        => Write(writer =>
        {
            Start(writer, "invoke", request);
            writer.WriteString("executionId", execution.ToString());
            writer.WriteString("occurrenceId", occurrence.ToString());
            WriteReference(writer, "operation", new WireReference(InterchangeCoolingContract.Operation));
            WriteReference(writer, "inputShape", new WireReference(InterchangeCoolingContract.CommandShape));
            WriteReference(writer, "outputShape", new WireReference(InterchangeCoolingContract.ResultShape));
            writer.WritePropertyName("input");
            PortableShapeValueCodec.Write(writer, input);
            writer.WriteEndObject();
        });

    internal static string Shutdown(BindingRequestId request) =>
        Write(writer =>
        {
            Start(writer, "shutdown", request);
            writer.WriteEndObject();
        });

    internal static string Activation(BindingRequestId request, PortableManifest manifest) =>
        Write(writer =>
        {
            Start(writer, "activation", request);
            writer.WriteBoolean("accepted", true);
            writer.WritePropertyName("manifest");
            WriteJson(writer, ManifestCodec.Encode(manifest));
            writer.WriteEndObject();
        });

    internal static string Outcome(
        BindingRequestId request,
        BindingExecutionId execution,
        BindingOccurrenceId occurrence,
        PortableProviderEffect effect,
        JsonElement forwardedInput,
        WireReference provider)
        => Write(writer =>
        {
            Start(writer, "outcome", request);
            writer.WriteString("executionId", execution.ToString());
            writer.WriteString("occurrenceId", occurrence.ToString());
            writer.WriteString("status", effect.Succeeded ? "succeeded" : "failed");
            WriteReference(writer, "provider", provider);
            writer.WriteNumber("providerEffectCount", effect.ProviderEffectCount);
            if (effect.Succeeded)
            {
                writer.WritePropertyName("result");
                PortableShapeValueCodec.Write(writer, effect.Value);
            }
            else
            {
                writer.WritePropertyName("details");
                PortableShapeValueCodec.Write(writer, effect.Value);
            }

            writer.WritePropertyName("forwardedInput");
            forwardedInput.WriteTo(writer);
            writer.WriteEndObject();
        });

    internal static string ProtocolError(BindingRequestId request, string code, string message) =>
        Write(writer =>
        {
            Start(writer, "protocol-error", request);
            writer.WriteString("code", code);
            writer.WriteString("message", message);
            writer.WriteEndObject();
        });

    internal static string ShutdownAck(BindingRequestId request) =>
        Write(writer =>
        {
            Start(writer, "shutdown-ack", request);
            writer.WriteEndObject();
        });

    internal static JsonElement Required(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property)
            ? property
            : throw new BoundaryProtocolException($"The protocol message is missing '{name}'.");

    internal static BindingRequestId RequestId(JsonElement element) =>
        new(ParseGuid(Required(element, "requestId"), "requestId"));

    internal static BindingExecutionId ExecutionId(JsonElement element) =>
        new(ParseGuid(Required(element, "executionId"), "executionId"));

    internal static BindingOccurrenceId OccurrenceId(JsonElement element) =>
        new(ParseGuid(Required(element, "occurrenceId"), "occurrenceId"));

    internal static WireReference Reference(JsonElement element, string name)
    {
        var reference = Required(element, name);
        var result = new WireReference(
            Required(reference, "name").GetString() ?? string.Empty,
            Required(reference, "version").GetInt32());
        result.Validate($"The {name} reference");
        return result;
    }

    private static Guid ParseGuid(JsonElement element, string description)
    {
        if (element.ValueKind != JsonValueKind.String || !Guid.TryParse(element.GetString(), out var value))
        {
            throw new BoundaryProtocolException($"The binding-scoped {description} is invalid.");
        }

        return value;
    }

    private static void Start(Utf8JsonWriter writer, string kind, BindingRequestId request)
    {
        writer.WriteStartObject();
        writer.WriteNumber("protocolVersion", Version);
        writer.WriteString("kind", kind);
        writer.WriteString("requestId", request.ToString());
    }

    private static void WriteReference(Utf8JsonWriter writer, string name, WireReference reference)
    {
        writer.WriteStartObject(name);
        writer.WriteString("name", reference.Name);
        writer.WriteNumber("version", reference.Version);
        writer.WriteEndObject();
    }

    private static void WriteJson(Utf8JsonWriter writer, string json)
    {
        using var document = JsonDocument.Parse(json);
        document.RootElement.WriteTo(writer);
    }

    private static string Write(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            write(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

public sealed record ProviderLaunch(string FileName, string Arguments = "");

public sealed record PortableProviderEffect(bool Succeeded, ShapeValue Value, long ProviderEffectCount)
{
    public static PortableProviderEffect Success(ShapeValue result, long effectCount) =>
        new(true, result, effectCount);

    public static PortableProviderEffect Failure(ShapeValue details, long effectCount) =>
        new(false, details, effectCount);
}

public sealed record ProviderInvocationResult(
    bool Succeeded,
    ShapeValue Value,
    ShapeValue ForwardedInput,
    string Provider,
    long ProviderEffectCount,
    ImmutableArray<string> Messages);

public sealed class ProcessBindingClient(ProviderLaunch launch, TimeSpan timeout)
{
    public async ValueTask<ProviderInvocationResult> InvokeAsync(
        PortableManifest requiredManifest,
        BindingRequestId request,
        BindingExecutionId execution,
        BindingOccurrenceId occurrence,
        ShapeValue input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requiredManifest);
        ArgumentNullException.ThrowIfNull(input);
        var startInfo = new ProcessStartInfo
        {
            FileName = launch.FileName,
            Arguments = launch.Arguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = new Process { StartInfo = startInfo };
        var messages = ImmutableArray.CreateBuilder<string>();
        try
        {
            if (!process.Start())
            {
                throw new ProviderProcessException("start", "The provider process did not start.");
            }

            await SendAsync(process, PortableProtocol.Activate(request, requiredManifest), cancellationToken)
                .ConfigureAwait(false);
            var activation = await ReadAsync(process, "activation", cancellationToken).ConfigureAwait(false);
            messages.Add(activation);
            using (var activationDocument = BoundaryJson.Parse(activation))
            {
                var root = activationDocument.RootElement;
                RequireProtocol(root);
                var kind = PortableProtocol.Required(root, "kind").GetString();
                if (kind == "protocol-error")
                {
                    throw new BoundaryNegotiationException(ReadProtocolError(root));
                }

                if (kind != "activation" || !PortableProtocol.Required(root, "accepted").GetBoolean())
                {
                    throw new BoundaryNegotiationException("The provider did not accept activation.");
                }

                var offered = ManifestCodec.Decode(PortableProtocol.Required(root, "manifest").GetRawText());
                ManifestCodec.NegotiateExact(requiredManifest, offered);
                ManifestCodec.NegotiateExact(offered, requiredManifest);
            }

            await SendAsync(process, PortableProtocol.Invoke(request, execution, occurrence, input), cancellationToken)
                .ConfigureAwait(false);
            var outcomeLine = await ReadAsync(process, "outcome", cancellationToken).ConfigureAwait(false);
            messages.Add(outcomeLine);
            ProviderInvocationResult result;
            using (var outcomeDocument = BoundaryJson.Parse(outcomeLine))
            {
                var root = outcomeDocument.RootElement;
                RequireProtocol(root);
                var kind = PortableProtocol.Required(root, "kind").GetString();
                if (kind == "protocol-error")
                {
                    throw new BoundaryProtocolException(ReadProtocolError(root));
                }

                if (kind != "outcome" || PortableProtocol.RequestId(root) != request ||
                    PortableProtocol.ExecutionId(root) != execution ||
                    PortableProtocol.OccurrenceId(root) != occurrence)
                {
                    throw new BoundaryProtocolException("The provider Outcome identifiers do not match the invocation.");
                }

                var status = PortableProtocol.Required(root, "status").GetString();
                var succeeded = status == "succeeded";
                if (!succeeded && status != "failed")
                {
                    throw new BoundaryProtocolException("The provider returned an unknown terminal Outcome status.");
                }

                var value = PortableShapeValueCodec.Decode(
                    PortableProtocol.Required(root, succeeded ? "result" : "details"),
                    succeeded ? InterchangeCoolingContract.ResultShape : InterchangeCoolingContract.DetailsShape);
                var forwarded = PortableShapeValueCodec.Decode(
                    PortableProtocol.Required(root, "forwardedInput"),
                    InterchangeCoolingContract.CommandShape);
                var provider = PortableProtocol.Reference(root, "provider").Name;
                var effectCount = PortableProtocol.Required(root, "providerEffectCount").GetInt64();
                result = new ProviderInvocationResult(
                    succeeded,
                    value,
                    forwarded,
                    provider,
                    effectCount,
                    messages.ToImmutable());
            }

            await SendAsync(process, PortableProtocol.Shutdown(request), cancellationToken).ConfigureAwait(false);
            var shutdown = await ReadAsync(process, "shutdown acknowledgement", cancellationToken).ConfigureAwait(false);
            messages.Add(shutdown);
            return result with { Messages = messages.ToImmutable() };
        }
        catch (ProviderProcessException)
        {
            throw;
        }
        catch (BoundaryNegotiationException)
        {
            throw;
        }
        catch (BoundaryProtocolException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            throw new ProviderProcessException("exchange", $"The provider process failed: {exception.Message}");
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private async Task SendAsync(Process process, string line, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        await process.StandardInput.WriteLineAsync(line.AsMemory(), timeoutSource.Token).ConfigureAwait(false);
        await process.StandardInput.FlushAsync(timeoutSource.Token).ConfigureAwait(false);
    }

    private async Task<string> ReadAsync(Process process, string stage, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            var line = await process.StandardOutput.ReadLineAsync(timeoutSource.Token).ConfigureAwait(false);
            if (line is null)
            {
                var diagnostics = await process.StandardError.ReadToEndAsync(timeoutSource.Token).ConfigureAwait(false);
                throw new ProviderProcessException(
                    stage,
                    string.IsNullOrWhiteSpace(diagnostics)
                        ? $"The provider process ended during {stage}."
                        : $"The provider process ended during {stage}; diagnostics: {diagnostics.Trim()}");
            }

            return line;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ProviderProcessException(stage, $"The provider process timed out during {stage}.");
        }
    }

    private static void RequireProtocol(JsonElement root)
    {
        if (PortableProtocol.Required(root, "protocolVersion").GetInt32() != PortableProtocol.Version)
        {
            throw new BoundaryProtocolException("The provider response uses an unsupported protocol version.");
        }
    }

    private static string ReadProtocolError(JsonElement root) =>
        $"{PortableProtocol.Required(root, "code").GetString()}: " +
        PortableProtocol.Required(root, "message").GetString();
}

public sealed class ProviderProcessException(string stage, string message) : IOException(message)
{
    public string Stage { get; } = stage;
}
