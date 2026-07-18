# Brontide Reference Stack interchange integration guide

## Rules for coding agents

- Keep the binding in `Brontide.Reference.Experimental.Binding`; never move transport or provider selection into
  `Brontide.Reference.Core`.
- Never reference Brontide Minimal Stack projects or assemblies. Exchange only the versioned root fixtures and one
  JSON object per line.
- Evaluate Brontide Reference Stack Actor, Capability, target, Operation, Shape, and Constraints before starting the
  provider process. Never serialize a Capability.
- Treat Cooling protocol version 2, Catalog protocol version 1, their manifests, and all binding
  observations as experimental.
- Enforce the Catalog 65,536-byte line limit, exact field sets, process-local replay set, and
  provider-scoped resource check before semantic mutation.

## Quick reference

`ReferenceCoolingBindingHost` launches a foreign provider named by `ProviderLaunch`. The host accepts
the neutral `interchange.tests.cooling.set-enabled` contract. Its input must contain the required
`interchange.tests.cooling.host-context` Fragment; `TargetedEnrichmentComposition` can construct it
from an already available host-local value.

`Brontide.Reference.Interchange.Provider` serves the same contract using `BinaryCoolingComponent`. It maps
enabled to native `Fan.SetSpeed(100)` and disabled to `Fan.Stop`. `--reject-protocol` and
`--crash-after-activation` are deterministic test modes.

With `--catalog`, the provider serves `upsert-items` followed by `find-items` against ephemeral
provider-owned state. `CatalogProcessClient.RunScenarioAsync` verifies nested/repeated items,
explicit missing-item failure, and normal shutdown in one process. A resource other than
`catalog-sandbox/shared` returns `resource-refused`; the handle never conveys authority.

See [`../../docs/public-boundaries.md`](../../docs/public-boundaries.md) for exact payload, timeout,
cleanup, replay, redaction, and threat assumptions.

Ordinary tests skip real Brontide Minimal Stack launch when `BRONTIDE_MINIMAL_PROVIDER` is absent. Use the root
`build/verify-interchange.ps1` command for the required two-way process evidence.
