# Catalog/resource contract matrix

| Neutral contract item | Reference mapping | Minimal mapping | Boundary evidence | Limit |
| --- | --- | --- | --- | --- |
| `upsert-items` | C# provider-owned ordinal dictionary | F# provider-owned ordinal dictionary | Two or more nested items and repeated tags cross unchanged | One provider process owns ephemeral state |
| `find-items` | C# ordered lookup | F# ordered lookup | A second Operation observes the preceding upsert in the same process | No persistence after process exit |
| Missing items | `CatalogProviderReply.Failure` | `CatalogProviderReply.failure` | Explicit `failed` Outcome with `missing-items` and repeated missing IDs | No exception or private diagnostic crosses |
| Resource handle | `CatalogResourceReference` | `CatalogResourceReference` record | Only `catalog-sandbox/shared` is accepted; another handle returns `resource-refused` | Addressing is not authority; no Capability transfer |
| Replay | Per-endpoint `HashSet<CatalogRequestId>` | Per-endpoint `HashSet<CatalogRequestId>` | Reusing a request ID returns protocol code `replay` | Window ends with the provider process |
| Payload | UTF-8 byte count before parse/write acceptance | UTF-8 byte count before parse/write acceptance | A generated 65,537-character vector is rejected | 65,536 encoded bytes per line; JSON depth 32 |

The two implementations share only the JSON fixtures and documented field meanings. They do not
share runtime types, codec code, provider state, semantic adapters, or exceptions.
