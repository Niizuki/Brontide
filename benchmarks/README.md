# Repeatable implementation benchmarks

These are small Stopwatch baselines, not normative performance requirements and not merge
thresholds. Each stack owns an independent executable and reports count, elapsed milliseconds, and
mean microseconds for:

- one allowed constraint evaluation through the complete checked execution path;
- Catalog manifest serialization plus strict decode;
- snapshot/scan of representative in-memory execution or provenance history; and
- three complete foreign Catalog provider-process scenarios when the opposite provider path is set.

The history case is a representative information-history operation only. It does not claim the
provisional Architecture 0.7 Corpus/Dataset/Store model or durable persistence.

Build both providers, set `BRONTIDE_MINIMAL_PROVIDER` and `BRONTIDE_REFERENCE_PROVIDER` as the full
executable paths, then run from the repository root:

```powershell
dotnet run --project .\Reference\benchmarks\Brontide.Reference.Benchmarks\Brontide.Reference.Benchmarks.csproj --configuration Release -- --iterations 1000
dotnet run --project .\Minimal\benchmarks\Brontide.Minimal.Benchmarks\Brontide.Minimal.Benchmarks.fsproj --configuration Release -- --iterations 1000
```

Use the same SDK, configuration, iteration count, provider build, power mode, and otherwise idle
machine when comparing observations. Run once for warm-up and record at least five subsequent runs;
report the median rather than selecting the fastest result. CI builds the executables but does not
assert machine-dependent timing thresholds.
