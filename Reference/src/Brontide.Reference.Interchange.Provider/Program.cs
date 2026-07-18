using Brontide.Reference.Experimental.Binding;
using Brontide.Reference.Vocabularies.Cooling;

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
