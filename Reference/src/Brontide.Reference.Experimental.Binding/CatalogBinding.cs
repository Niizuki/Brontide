using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brontide.Reference.Experimental.Binding;

public readonly record struct CatalogRequestId(Guid Value)
{
    public static CatalogRequestId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public readonly record struct CatalogExecutionId(Guid Value)
{
    public static CatalogExecutionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}

public sealed record CatalogResourceReference(string Provider, string Id);
public sealed record CatalogItem(string Id, string Title, ImmutableArray<string> Tags);
public sealed record CatalogManifestOperation(string Name, string InputShape, string OutputShape);
public sealed record CatalogManifest(
    int ProtocolVersion,
    string Component,
    int ContractVersion,
    string ResourceBoundary,
    int PayloadLimitBytes,
    ImmutableArray<CatalogManifestOperation> Operations);

public static class CatalogContract
{
    public const int ProtocolVersion = 1;
    public const int PayloadLimitBytes = 65_536;
    public const string UpsertOperation = "interchange.tests.catalog.upsert-items";
    public const string FindOperation = "interchange.tests.catalog.find-items";

    public static CatalogManifest Manifest { get; } = new(
        ProtocolVersion,
        "interchange.tests.catalog-component",
        1,
        "provider-scoped-resource-handle",
        PayloadLimitBytes,
        [
            new(UpsertOperation, "interchange.tests.catalog.upsert-command@1", "interchange.tests.catalog.upsert-result@1"),
            new(FindOperation, "interchange.tests.catalog.find-command@1", "interchange.tests.catalog.find-result@1")
        ]);
}

public static class CatalogManifestCodec
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static CatalogManifest Decode(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var manifest = JsonSerializer.Deserialize<CatalogManifest>(json, Options) ??
            throw new CatalogProtocolException("The catalog manifest is null.");
        Validate(manifest);
        return manifest;
    }

    public static string Encode(CatalogManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        Validate(manifest);
        return JsonSerializer.Serialize(manifest, Options);
    }

    public static void Validate(CatalogManifest manifest)
    {
        if (manifest.ProtocolVersion != CatalogContract.ProtocolVersion ||
            manifest.ContractVersion != 1 ||
            manifest.Component != CatalogContract.Manifest.Component ||
            manifest.ResourceBoundary != "provider-scoped-resource-handle" ||
            manifest.PayloadLimitBytes != CatalogContract.PayloadLimitBytes ||
            manifest.Operations.IsDefaultOrEmpty ||
            manifest.Operations.Select(operation => operation.Name).Distinct(StringComparer.Ordinal).Count() != 2 ||
            !manifest.Operations.Any(operation => operation.Name == CatalogContract.UpsertOperation) ||
            !manifest.Operations.Any(operation => operation.Name == CatalogContract.FindOperation))
        {
            throw new CatalogProtocolException("The catalog manifest is incompatible with contract version 1.");
        }
    }
}

public sealed record CatalogInvocation(
    CatalogRequestId Request,
    CatalogExecutionId Execution,
    string Operation,
    CatalogResourceReference Resource,
    ImmutableArray<CatalogItem> Items,
    ImmutableArray<string> ItemIds);

public sealed record CatalogProviderReply(
    bool Succeeded,
    long Stored,
    ImmutableArray<CatalogItem> Items,
    string? Code,
    ImmutableArray<string> MissingIds)
{
    public static CatalogProviderReply StoredItems(long count) =>
        new(true, count, [], null, []);

    public static CatalogProviderReply FoundItems(IEnumerable<CatalogItem> items) =>
        new(true, 0, [.. items], null, []);

    public static CatalogProviderReply Failure(string code, IEnumerable<string>? missingIds = null) =>
        new(false, 0, [], code, missingIds is null ? [] : [.. missingIds]);
}

public sealed record CatalogOutcome(
    CatalogRequestId Request,
    CatalogExecutionId Execution,
    string Operation,
    bool Succeeded,
    long Stored,
    ImmutableArray<CatalogItem> Items,
    string? Code,
    ImmutableArray<string> MissingIds);

public sealed record CatalogScenarioResult(
    CatalogOutcome Upsert,
    CatalogOutcome Find,
    CatalogOutcome Missing,
    int ProviderStarts);

public sealed class CatalogProviderEndpoint(Func<CatalogInvocation, CatalogProviderReply> invoke)
{
    public async Task<int> RunAsync(
        TextReader input,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        var seenRequests = new HashSet<CatalogRequestId>();

        while (await input.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            CatalogRequestId request = default;
            try
            {
                if (Encoding.UTF8.GetByteCount(line) > CatalogContract.PayloadLimitBytes)
                {
                    throw new CatalogProtocolException(
                        "The protocol line exceeds the 65536-byte payload limit.",
                        "payload-limit");
                }

                using var document = Parse(line);
                var root = document.RootElement;
                var kind = RequiredString(root, "kind");
                if (kind == "shutdown")
                {
                    RequireExactProperties(root, "protocolVersion", "kind", "requestId");
                    RequireVersion(root);
                    request = ReadRequest(root);
                    await WriteAsync(output, WriteShutdownAck(request), cancellationToken).ConfigureAwait(false);
                    return 0;
                }

                RequireExactProperties(root, "protocolVersion", "kind", "requestId", "executionId", "operation", "resource", "input");
                RequireVersion(root);
                request = ReadRequest(root);
                if (kind != "invoke")
                {
                    throw new CatalogProtocolException($"Unknown protocol message kind '{kind}'.");
                }

                if (!seenRequests.Add(request))
                {
                    await WriteAsync(output, WriteProtocolError(request, "replay", "The requestId has already been used."), cancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                var invocation = ReadInvocation(root, request);
                var reply = invoke(invocation) ?? throw new CatalogProtocolException("The catalog provider returned no result.");
                await WriteAsync(output, WriteOutcome(invocation, reply), cancellationToken).ConfigureAwait(false);
            }
            catch (CatalogProtocolException exception)
            {
                await WriteAsync(output, WriteProtocolError(request, exception.Code, exception.Message), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                await WriteAsync(
                    output,
                    WriteProtocolError(request, "provider-internal-failure", "The provider could not process the message."),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return 0;
    }

    private static CatalogInvocation ReadInvocation(JsonElement root, CatalogRequestId request)
    {
        var execution = new CatalogExecutionId(RequiredGuid(root, "executionId"));
        var operation = RequiredString(root, "operation");
        var resourceElement = Required(root, "resource");
        RequireExactProperties(resourceElement, "provider", "id");
        var resource = new CatalogResourceReference(
            RequiredNonEmptyString(resourceElement, "provider"),
            RequiredNonEmptyString(resourceElement, "id"));
        var input = Required(root, "input");

        return operation switch
        {
            CatalogContract.UpsertOperation => ReadUpsert(request, execution, operation, resource, input),
            CatalogContract.FindOperation => ReadFind(request, execution, operation, resource, input),
            _ => throw new CatalogProtocolException($"Unknown catalog operation '{operation}'.", "unknown-operation")
        };
    }

    private static CatalogInvocation ReadUpsert(
        CatalogRequestId request,
        CatalogExecutionId execution,
        string operation,
        CatalogResourceReference resource,
        JsonElement input)
    {
        RequireExactProperties(input, "items");
        var itemsElement = Required(input, "items");
        if (itemsElement.ValueKind != JsonValueKind.Array)
        {
            throw new CatalogProtocolException("Catalog items must be an array.");
        }

        var items = itemsElement.EnumerateArray().Select(ReadItem).ToImmutableArray();
        if (items.IsDefaultOrEmpty || items.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != items.Length)
        {
            throw new CatalogProtocolException("An upsert requires uniquely identified catalog items.");
        }

        return new(request, execution, operation, resource, items, []);
    }

    private static CatalogInvocation ReadFind(
        CatalogRequestId request,
        CatalogExecutionId execution,
        string operation,
        CatalogResourceReference resource,
        JsonElement input)
    {
        RequireExactProperties(input, "itemIds");
        var idsElement = Required(input, "itemIds");
        if (idsElement.ValueKind != JsonValueKind.Array)
        {
            throw new CatalogProtocolException("Catalog itemIds must be an array.");
        }

        var ids = idsElement.EnumerateArray().Select(item =>
            item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString())
                ? item.GetString()!
                : throw new CatalogProtocolException("Each catalog itemId must be non-empty text.")).ToImmutableArray();
        if (ids.IsDefaultOrEmpty)
        {
            throw new CatalogProtocolException("A find requires at least one itemId.");
        }

        return new(request, execution, operation, resource, [], ids);
    }

    private static CatalogItem ReadItem(JsonElement element)
    {
        RequireExactProperties(element, "id", "title", "tags");
        var tagsElement = Required(element, "tags");
        if (tagsElement.ValueKind != JsonValueKind.Array)
        {
            throw new CatalogProtocolException("Catalog item tags must be an array.");
        }

        var tags = tagsElement.EnumerateArray().Select(tag =>
            tag.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(tag.GetString())
                ? tag.GetString()!
                : throw new CatalogProtocolException("Catalog tags must be non-empty text.")).ToImmutableArray();
        return new(RequiredNonEmptyString(element, "id"), RequiredNonEmptyString(element, "title"), tags);
    }

    internal static string WriteInvocation(CatalogInvocation invocation) => Write(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("protocolVersion", CatalogContract.ProtocolVersion);
        writer.WriteString("kind", "invoke");
        writer.WriteString("requestId", invocation.Request.ToString());
        writer.WriteString("executionId", invocation.Execution.ToString());
        writer.WriteString("operation", invocation.Operation);
        WriteResource(writer, invocation.Resource);
        writer.WriteStartObject("input");
        if (invocation.Operation == CatalogContract.UpsertOperation)
        {
            writer.WriteStartArray("items");
            foreach (var item in invocation.Items)
            {
                WriteItem(writer, item);
            }
            writer.WriteEndArray();
        }
        else
        {
            writer.WriteStartArray("itemIds");
            foreach (var id in invocation.ItemIds)
            {
                writer.WriteStringValue(id);
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
        writer.WriteEndObject();
    });

    internal static CatalogOutcome ReadOutcome(string line, CatalogInvocation expected)
    {
        if (Encoding.UTF8.GetByteCount(line) > CatalogContract.PayloadLimitBytes)
        {
            throw new CatalogProtocolException(
                "The provider response exceeds the 65536-byte payload limit.",
                "payload-limit");
        }

        using var document = Parse(line);
        var root = document.RootElement;
        RequireVersion(root);
        var kind = RequiredString(root, "kind");
        if (kind == "protocol-error")
        {
            RequireExactProperties(root, "protocolVersion", "kind", "requestId", "code", "message");
            throw new CatalogProtocolException(RequiredString(root, "message"), RequiredString(root, "code"));
        }

        RequireExactProperties(root, "protocolVersion", "kind", "requestId", "executionId", "operation", "status", "result", "details");
        if (kind != "outcome" || ReadRequest(root) != expected.Request ||
            new CatalogExecutionId(RequiredGuid(root, "executionId")) != expected.Execution ||
            RequiredString(root, "operation") != expected.Operation)
        {
            throw new CatalogProtocolException("The catalog Outcome does not match its invocation.");
        }

        var status = RequiredString(root, "status");
        var result = Required(root, "result");
        var details = Required(root, "details");
        if (status == "succeeded")
        {
            if (details.ValueKind != JsonValueKind.Null)
            {
                throw new CatalogProtocolException("A successful catalog Outcome cannot carry failure details.");
            }

            if (expected.Operation == CatalogContract.UpsertOperation)
            {
                RequireExactProperties(result, "stored");
                return new(expected.Request, expected.Execution, expected.Operation, true,
                    Required(result, "stored").GetInt64(), [], null, []);
            }

            RequireExactProperties(result, "items");
            var itemsElement = Required(result, "items");
            return new(expected.Request, expected.Execution, expected.Operation, true, 0,
                itemsElement.EnumerateArray().Select(ReadItem).ToImmutableArray(), null, []);
        }

        if (status != "failed" || result.ValueKind != JsonValueKind.Null)
        {
            throw new CatalogProtocolException("The catalog Outcome status or payload is invalid.");
        }

        RequireExactProperties(details, "code", "missingIds");
        var missing = Required(details, "missingIds").EnumerateArray().Select(item => item.GetString() ?? string.Empty)
            .ToImmutableArray();
        return new(expected.Request, expected.Execution, expected.Operation, false, 0, [],
            RequiredString(details, "code"), missing);
    }

    internal static string WriteShutdown(CatalogRequestId request) => Write(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("protocolVersion", CatalogContract.ProtocolVersion);
        writer.WriteString("kind", "shutdown");
        writer.WriteString("requestId", request.ToString());
        writer.WriteEndObject();
    });

    internal static void ReadShutdownAck(string line, CatalogRequestId expected)
    {
        using var document = Parse(line);
        var root = document.RootElement;
        RequireExactProperties(root, "protocolVersion", "kind", "requestId");
        RequireVersion(root);
        if (RequiredString(root, "kind") != "shutdown-ack" || ReadRequest(root) != expected)
        {
            throw new CatalogProtocolException("The provider did not acknowledge shutdown.");
        }
    }

    private static string WriteOutcome(CatalogInvocation invocation, CatalogProviderReply reply) => Write(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("protocolVersion", CatalogContract.ProtocolVersion);
        writer.WriteString("kind", "outcome");
        writer.WriteString("requestId", invocation.Request.ToString());
        writer.WriteString("executionId", invocation.Execution.ToString());
        writer.WriteString("operation", invocation.Operation);
        writer.WriteString("status", reply.Succeeded ? "succeeded" : "failed");
        if (reply.Succeeded)
        {
            writer.WriteStartObject("result");
            if (invocation.Operation == CatalogContract.UpsertOperation)
            {
                writer.WriteNumber("stored", reply.Stored);
            }
            else
            {
                writer.WriteStartArray("items");
                foreach (var item in reply.Items)
                {
                    WriteItem(writer, item);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
            writer.WriteNull("details");
        }
        else
        {
            writer.WriteNull("result");
            writer.WriteStartObject("details");
            writer.WriteString("code", reply.Code ?? "provider-failure");
            writer.WriteStartArray("missingIds");
            foreach (var id in reply.MissingIds)
            {
                writer.WriteStringValue(id);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    });

    private static string WriteProtocolError(CatalogRequestId request, string code, string message) => Write(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("protocolVersion", CatalogContract.ProtocolVersion);
        writer.WriteString("kind", "protocol-error");
        writer.WriteString("requestId", request.ToString());
        writer.WriteString("code", code);
        writer.WriteString("message", message);
        writer.WriteEndObject();
    });

    private static string WriteShutdownAck(CatalogRequestId request) => Write(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("protocolVersion", CatalogContract.ProtocolVersion);
        writer.WriteString("kind", "shutdown-ack");
        writer.WriteString("requestId", request.ToString());
        writer.WriteEndObject();
    });

    private static async Task WriteAsync(TextWriter output, string line, CancellationToken cancellationToken)
    {
        await output.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static JsonDocument Parse(string line)
    {
        try
        {
            return JsonDocument.Parse(line, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 32
            });
        }
        catch (JsonException exception)
        {
            throw new CatalogProtocolException($"Malformed catalog protocol JSON: {exception.Message}");
        }
    }

    private static void RequireVersion(JsonElement root)
    {
        var version = Required(root, "protocolVersion").GetInt32();
        if (version != CatalogContract.ProtocolVersion)
        {
            throw new CatalogProtocolException($"Catalog protocol version {version} is not supported.", "unsupported-version");
        }
    }

    private static CatalogRequestId ReadRequest(JsonElement root) => new(RequiredGuid(root, "requestId"));

    private static Guid RequiredGuid(JsonElement root, string name) =>
        Guid.TryParse(RequiredString(root, name), out var value)
            ? value
            : throw new CatalogProtocolException($"The {name} is not a UUID.");

    private static string RequiredNonEmptyString(JsonElement root, string name)
    {
        var value = RequiredString(root, name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new CatalogProtocolException($"The {name} must not be empty.")
            : value;
    }

    private static string RequiredString(JsonElement root, string name)
    {
        var value = Required(root, name);
        return value.ValueKind == JsonValueKind.String && value.GetString() is { } text
            ? text
            : throw new CatalogProtocolException($"The {name} must be text.");
    }

    private static JsonElement Required(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value)
            ? value
            : throw new CatalogProtocolException($"The protocol value is missing '{name}'.");

    private static void RequireExactProperties(JsonElement element, params string[] expected)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new CatalogProtocolException("A catalog protocol object was expected.");
        }

        var actual = element.EnumerateObject().Select(property => property.Name).ToArray();
        if (actual.Length != expected.Length || actual.Except(expected, StringComparer.Ordinal).Any())
        {
            var unknown = actual.FirstOrDefault(name => !expected.Contains(name, StringComparer.Ordinal));
            throw new CatalogProtocolException(unknown is null
                ? "A catalog protocol object is missing a required field."
                : $"Unknown catalog protocol field '{unknown}'.", "unknown-field");
        }
    }

    private static void WriteResource(Utf8JsonWriter writer, CatalogResourceReference resource)
    {
        writer.WriteStartObject("resource");
        writer.WriteString("provider", resource.Provider);
        writer.WriteString("id", resource.Id);
        writer.WriteEndObject();
    }

    private static void WriteItem(Utf8JsonWriter writer, CatalogItem item)
    {
        writer.WriteStartObject();
        writer.WriteString("id", item.Id);
        writer.WriteString("title", item.Title);
        writer.WriteStartArray("tags");
        foreach (var tag in item.Tags)
        {
            writer.WriteStringValue(tag);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static string Write(Action<Utf8JsonWriter> action)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            action(writer);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

}

public sealed class CatalogProcessClient(ProviderLaunch launch, TimeSpan timeout)
{
    public async Task<CatalogScenarioResult> RunScenarioAsync(
        CatalogResourceReference resource,
        ImmutableArray<CatalogItem> items,
        CancellationToken cancellationToken = default)
    {
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
        if (!process.Start())
        {
            throw new ProviderProcessException("start", "The catalog provider process did not start.");
        }

        try
        {
            var upsertInvocation = new CatalogInvocation(
                CatalogRequestId.New(), CatalogExecutionId.New(), CatalogContract.UpsertOperation, resource, items, []);
            var upsert = await ExchangeAsync(process, upsertInvocation, cancellationToken).ConfigureAwait(false);
            var findInvocation = new CatalogInvocation(
                CatalogRequestId.New(), CatalogExecutionId.New(), CatalogContract.FindOperation, resource, [],
                [.. items.Select(item => item.Id)]);
            var find = await ExchangeAsync(process, findInvocation, cancellationToken).ConfigureAwait(false);
            var missingInvocation = new CatalogInvocation(
                CatalogRequestId.New(), CatalogExecutionId.New(), CatalogContract.FindOperation, resource, [], ["missing-item"]);
            var missing = await ExchangeAsync(process, missingInvocation, cancellationToken).ConfigureAwait(false);

            var shutdown = CatalogRequestId.New();
            await SendAsync(process, CatalogProviderEndpoint.WriteShutdown(shutdown), cancellationToken).ConfigureAwait(false);
            var shutdownLine = await ReadAsync(process, "catalog shutdown", cancellationToken).ConfigureAwait(false);
            CatalogProviderEndpoint.ReadShutdownAck(shutdownLine, shutdown);
            return new(upsert, find, missing, 1);
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

    private async Task<CatalogOutcome> ExchangeAsync(
        Process process,
        CatalogInvocation invocation,
        CancellationToken cancellationToken)
    {
        await SendAsync(process, CatalogProviderEndpoint.WriteInvocation(invocation), cancellationToken).ConfigureAwait(false);
        var line = await ReadAsync(process, invocation.Operation, cancellationToken).ConfigureAwait(false);
        return CatalogProviderEndpoint.ReadOutcome(line, invocation);
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
                throw new ProviderProcessException(stage, string.IsNullOrWhiteSpace(diagnostics)
                    ? $"The catalog provider ended during {stage}."
                    : $"The catalog provider ended during {stage}; diagnostics: {diagnostics.Trim()}");
            }
            return line;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ProviderProcessException(stage, $"The catalog provider timed out during {stage}.");
        }
    }
}

public sealed class CatalogProtocolException(string message, string code = "invalid-message") : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
