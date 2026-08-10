# Minimal experimental persistent information

Designed for Brontide Architecture 0.7 sections 12, 18.2, and 21.1, Complete Draft.

This native F# experimental component implements the M4 evidence slice with opaque identity unions,
result-valued refusals, immutable Corpus and Dataset records, deterministic in-memory Store
endpoints, and a Router whose declared logical guarantees stay stable across backing changes.

It depends on `Brontide.Minimal.Model` and `Brontide.Minimal.Kernel`. Dataset-specific authority
remains here: the D5 coordinator preflights provider scope, invokes `World.stepDraft08`, and returns
the immutable World containing the derived requester Capability.

Quick verification:

```powershell
dotnet test Minimal/tests/Brontide.Minimal.PersistentInformation.Tests/Brontide.Minimal.PersistentInformation.Tests.fsproj
```

See [`docs/integration-guide.md`](docs/integration-guide.md) for the authority and operation boundary.
