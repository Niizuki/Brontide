using Fabric.Experimental.Binding;
using Fabric.Vocabularies.Cooling;

var crashAfterActivation = args.Contains("--crash-after-activation", StringComparer.Ordinal);
var rejectProtocol = args.Contains("--reject-protocol", StringComparer.Ordinal);
var cooling = BinaryCoolingComponent.Create();
var endpoint = new PortableCoolingProviderEndpoint(
    "fabric-csharp-provider",
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
        if (!result.IsAuthorized || result.Outcome.Status != Fabric.Core.OutcomeStatus.Succeeded)
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
