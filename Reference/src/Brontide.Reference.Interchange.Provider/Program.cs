using Brontide.Reference.Experimental.Binding;
using Brontide.Reference.Experimental.Binding.Portable;
using Brontide.Reference.Experimental.ComponentManagement;
using Brontide.Reference.Vocabularies.Cooling;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

static FileStream? AcquireRestartEffectLease()
{
    var leasePath = Environment.GetEnvironmentVariable("BRONTIDE_RESTART_EFFECT_LEASE");
    var receiptPath = Environment.GetEnvironmentVariable("BRONTIDE_RESTART_EFFECT_RECEIPT");
    var token = Environment.GetEnvironmentVariable("BRONTIDE_RESTART_EFFECT_TOKEN");
    var stagedIdentity = Environment.GetEnvironmentVariable("BRONTIDE_RESTART_EFFECT_STAGED_IDENTITY");
    if (leasePath is null && receiptPath is null && token is null && stagedIdentity is null) return null;
    if (string.IsNullOrWhiteSpace(leasePath) || string.IsNullOrWhiteSpace(receiptPath)
        || string.IsNullOrWhiteSpace(token) || token.Length > 128 || token != token.Trim()
        || stagedIdentity is null || stagedIdentity.Length != 64)
        throw new InvalidOperationException("The restart effect environment is incomplete.");

    var lease = new FileStream(Path.GetFullPath(leasePath), FileMode.OpenOrCreate,
        FileAccess.ReadWrite, FileShare.Read);
    try
    {
        using var process = Process.GetCurrentProcess();
        var executableName = Path.GetFileName(Environment.ProcessPath);
        if (string.IsNullOrWhiteSpace(executableName))
            throw new InvalidOperationException("The provider executable name is unavailable.");
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output))
        {
            writer.WriteStartObject();
            writer.WriteString("format", "CBI55");
            writer.WriteString("token", token);
            writer.WriteString("stagedIdentity", stagedIdentity);
            writer.WriteNumber("processId", Environment.ProcessId);
            writer.WriteNumber("processStartUtcTicks", process.StartTime.ToUniversalTime().Ticks);
            writer.WriteString("executableName", executableName);
            writer.WriteEndObject();
        }
        var record = output.ToArray();
        var temporary = Path.GetFullPath(receiptPath) + ".tmp";
        using (var receipt = new FileStream(temporary, FileMode.Create, FileAccess.Write,
                   FileShare.None, 4096, FileOptions.WriteThrough))
        {
            receipt.Write(record);
            receipt.Write(SHA256.HashData(record));
            receipt.Flush(flushToDisk: true);
        }
        File.Move(temporary, Path.GetFullPath(receiptPath), overwrite: true);
        return lease;
    }
    catch
    {
        lease.Dispose();
        throw;
    }
}

FileStream? restartEffectLease;
try { restartEffectLease = AcquireRestartEffectLease(); }
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
    or InvalidOperationException or NotSupportedException)
{
    return 76;
}
using var heldRestartEffectLease = restartEffectLease;

const string ownershipProbePrefix = "--probe-exclusive-file=";
const string ownershipHoldPrefix = "--hold-exclusive-file=";
var ownershipHold = args.FirstOrDefault(argument =>
    argument.StartsWith(ownershipHoldPrefix, StringComparison.Ordinal));
if (ownershipHold is not null)
{
    using var held = new FileStream(
        ownershipHold[ownershipHoldPrefix.Length..], FileMode.OpenOrCreate,
        FileAccess.ReadWrite, FileShare.Read);
    await Console.Out.WriteLineAsync("held");
    await Console.Out.FlushAsync();
    await Console.In.ReadLineAsync();
    return 0;
}

var ownershipProbe = args.FirstOrDefault(argument =>
    argument.StartsWith(ownershipProbePrefix, StringComparison.Ordinal));
if (ownershipProbe is not null)
{
    try
    {
        using var held = new FileStream(
            ownershipProbe[ownershipProbePrefix.Length..], FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.Read);
        return 0;
    }
    catch (IOException)
    {
        return 74;
    }
}

if (args.Contains("--component-management", StringComparer.Ordinal))
{
    await FakeAuthorityComparisonEndpoint.RunAsync(
        Console.In,
        Console.Out,
        "reference-csharp",
        CancellationToken.None);
    return 0;
}

// The portable verbs run the reusable Portable Component Binding over a real duplex process
// boundary. The verbs below them remain the retained line-delimited experiments, which stay the
// cross-stack baseline until the Minimal side implements the portable contract.
if (args.Contains("--portable", StringComparer.Ordinal))
{
    const string failAfterFirstPrefix = "--portable-fail-after-first=";
    var failAfterFirst = args.FirstOrDefault(argument =>
        argument.StartsWith(failAfterFirstPrefix, StringComparison.Ordinal));
    if (failAfterFirst is not null)
    {
        try
        {
            await using var marker = new FileStream(
                failAfterFirst[failAfterFirstPrefix.Length..], FileMode.CreateNew,
                FileAccess.Write, FileShare.None);
        }
        catch (IOException)
        {
            return 73;
        }
    }

    var portableCatalog = args.Contains("--catalog", StringComparer.Ordinal);
    var portableEndpoint = new PortableProviderEndpoint(
        portableCatalog ? CatalogPortableFixture.Contract : CoolingPortableFixture.Contract,
        portableCatalog
            ? new CatalogPortableHandler()
            : new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
        PortableRealization.NegotiatedProcess);
    await using var portableDuplex = new PortableStreamDuplex(
        Console.OpenStandardInput(),
        Console.OpenStandardOutput(),
        PortableLimits.Declared,
        ownsStreams: true);
    await PortableProviderProcessLoop.RunAsync(portableDuplex, portableEndpoint, PortableLimits.Declared);
    return 0;
}

var crashAfterActivation = args.Contains("--crash-after-activation", StringComparer.Ordinal);
var rejectProtocol = args.Contains("--reject-protocol", StringComparer.Ordinal);
if (args.Contains("--catalog", StringComparer.Ordinal))
{
    var catalog = new Dictionary<string, CatalogItem>(StringComparer.Ordinal);
    var catalogEndpoint = new CatalogProviderEndpoint(invocation =>
    {
        if (invocation.Resource != new CatalogResourceReference("catalog-sandbox", "shared"))
        {
            return CatalogProviderReply.Failure("resource-refused");
        }

        if (invocation.Operation == CatalogContract.UpsertOperation)
        {
            foreach (var item in invocation.Items)
            {
                catalog[item.Id] = item;
            }
            return CatalogProviderReply.StoredItems(invocation.Items.Length);
        }

        var missing = invocation.ItemIds.Where(id => !catalog.ContainsKey(id)).ToArray();
        return missing.Length == 0
            ? CatalogProviderReply.FoundItems(invocation.ItemIds.Select(id => catalog[id]))
            : CatalogProviderReply.Failure("missing-items", missing);
    });
    return await catalogEndpoint.RunAsync(Console.In, Console.Out, CancellationToken.None);
}

var cooling = BinaryCoolingComponent.Create();
var endpoint = new PortableCoolingProviderEndpoint(
    "brontide-reference-csharp-provider",
    async (command, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (command.FailureMode == "semantic")
        {
            return PortableProviderEffect.Failure(
                InterchangeCoolingContract.Details(
                    "requested-failure",
                    "The test contract requested a semantic failure."),
                cooling.EffectCount);
        }

        var result = await cooling.SetEnabledAsync(command.Enabled).ConfigureAwait(false);
        if (!result.IsAuthorized || result.Outcome.Status != Brontide.Reference.Core.OutcomeStatus.Succeeded)
        {
            return PortableProviderEffect.Failure(
                InterchangeCoolingContract.Details("native-cooling-failure", result.Outcome.Message),
                cooling.EffectCount);
        }

        return PortableProviderEffect.Success(
            InterchangeCoolingContract.Result(
                command.Loop,
                cooling.CoolingEnabled,
                cooling.Revision,
                cooling.EffectCount),
            cooling.EffectCount);
    });

return await endpoint.RunAsync(
    Console.In,
    Console.Out,
    crashAfterActivation,
    rejectProtocol,
    CancellationToken.None);
