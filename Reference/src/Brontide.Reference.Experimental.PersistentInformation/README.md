# Reference experimental persistent information

Designed for Brontide Architecture 0.7 sections 12, 18.2, and 21.1, Complete Draft.

This independently consumable experimental component implements the R4 evidence slice: Opaque
Corpus declarations, typed Dataset and Store-role identity, attributable Dataset issuance records,
single-writer Dataset operations, deterministic in-memory Store endpoints, and Router-owned logical
endpoint guarantees with bounded fallback and topology redaction.

It depends only on `Brontide.Reference.Core`. It is not Brontide Base, a database, a durable-media
store, or a general Router implementation.

Quick verification:

```powershell
dotnet test Reference/tests/Brontide.Reference.PersistentInformation.Tests/Brontide.Reference.PersistentInformation.Tests.csproj
```

See [`docs/integration-guide.md`](docs/integration-guide.md) for the authority and operation boundary.
