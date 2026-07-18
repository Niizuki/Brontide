using System.Collections.Immutable;
using System.Diagnostics;
using Brontide.Reference.Core;
using Brontide.Reference.Experimental.Binding;

var iterations = ReadIterations(args, 1_000);
var operation = OperationReference.Parse("Benchmarks.Constraint.Checked");
var constraintName = CanonicalName.Parse("Benchmarks:Allow");
ActorReference holder = null!;
ActorReference target = null!;
Capability capability = null!;
var domain = AuthorityDomain.Create("Reference benchmark", genesis =>
{
    holder = genesis.Actor("benchmark holder");
    target = genesis.Actor("benchmark target");
    genesis.Constraint(
        constraintName,
        ShapeContract.For(BuiltInShapes.Text),
        (constraint, _) => constraint.Value.RequireScalar<string>() == "allow"
            ? ConstraintDecision.Allow(constraint.Name, "benchmark allow")
            : ConstraintDecision.Deny(constraint.Name, "benchmark deny"));
    genesis.Operation(
        operation,
        target,
        ShapeContract.Unit,
        ShapeContract.Unit,
        "Benchmark one checked execution.",
        _ => OperationEffect.SucceededAsync(ShapeValue.Unit));
    capability = genesis.Grant(
        holder,
        target,
        [operation],
        [new ValueConstraint(constraintName, ShapeValue.Text("allow"))]);
});

for (var index = 0; index < 25; index++)
{
    _ = await domain.ExecuteAsync(holder, operation, capability, ShapeValue.Unit);
}

var constraintElapsed = Stopwatch.StartNew();
for (var index = 0; index < iterations; index++)
{
    var result = await domain.ExecuteAsync(holder, operation, capability, ShapeValue.Unit);
    if (result.Outcome.Status != OutcomeStatus.Succeeded)
    {
        throw new InvalidOperationException("The constraint benchmark did not succeed.");
    }
}
constraintElapsed.Stop();
Report("constraint-evaluation-and-execution", iterations, constraintElapsed.Elapsed);

var serializationElapsed = Stopwatch.StartNew();
for (var index = 0; index < iterations; index++)
{
    var json = CatalogManifestCodec.Encode(CatalogContract.Manifest);
    _ = CatalogManifestCodec.Decode(json);
}
serializationElapsed.Stop();
Report("catalog-manifest-serialize-roundtrip", iterations, serializationElapsed.Elapsed);

var historyElapsed = Stopwatch.StartNew();
long historyChecksum = 0;
for (var index = 0; index < iterations; index++)
{
    historyChecksum += domain.Provenance.Count(entry => entry.Kind is ProvenanceKind.Execution or ProvenanceKind.Outcome);
}
historyElapsed.Stop();
if (historyChecksum == 0)
{
    throw new InvalidOperationException("The provenance history benchmark observed no records.");
}
Report("provenance-history-snapshot-and-scan", iterations, historyElapsed.Elapsed);

var minimalProvider = Environment.GetEnvironmentVariable("BRONTIDE_MINIMAL_PROVIDER");
if (!string.IsNullOrWhiteSpace(minimalProvider) && File.Exists(minimalProvider))
{
    const int processIterations = 3;
    var client = new CatalogProcessClient(new ProviderLaunch(minimalProvider, "--catalog"), TimeSpan.FromSeconds(10));
    var items = ImmutableArray.Create(new CatalogItem("bench", "Benchmark", ["nested", "repeatable"]));
    var processElapsed = Stopwatch.StartNew();
    for (var index = 0; index < processIterations; index++)
    {
        var result = await client.RunScenarioAsync(new("catalog-sandbox", "shared"), items);
        if (!result.Upsert.Succeeded || !result.Find.Succeeded || result.Missing.Succeeded)
        {
            throw new InvalidOperationException("The cross-process benchmark scenario failed.");
        }
    }
    processElapsed.Stop();
    Report("foreign-catalog-process-scenario", processIterations, processElapsed.Elapsed);
}
else
{
    Console.WriteLine("foreign-catalog-process-scenario: skipped (BRONTIDE_MINIMAL_PROVIDER is absent)");
}

return;

static int ReadIterations(string[] arguments, int fallback)
{
    var index = Array.IndexOf(arguments, "--iterations");
    return index >= 0 && index + 1 < arguments.Length && int.TryParse(arguments[index + 1], out var value) && value > 0
        ? value
        : fallback;
}

static void Report(string name, int count, TimeSpan elapsed) =>
    Console.WriteLine(
        $"{name}: count={count}; elapsedMs={elapsed.TotalMilliseconds:F3}; meanMicroseconds={elapsed.TotalMicroseconds / count:F3}");
