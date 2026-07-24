# Brontide Minimal Stack interchange integration guide

## Rules for coding agents

- Keep binding and host machinery outside `Brontide.Minimal.Model` and `Brontide.Minimal.Kernel`; Kernel remains a pure
  authority/transition dependency.
- Never reference Brontide Reference Stack projects or assemblies. Exchange only the versioned root fixtures and one
  JSON object per line.
- Run `World.step` authority and Constraint checks before provider activation. Never serialize a
  Capability or reinterpret delivery as authority.
- Treat Cooling protocol version 2, Catalog protocol version 1, their manifests, and all binding
  observations as experimental.
- Enforce the Catalog 65,536-byte line limit, exact field sets, process-local replay set, and
  provider-scoped resource check before semantic mutation.

## Quick reference

`MinimalCoolingBindingHost` launches a foreign provider named by `ProviderLaunch`. The neutral input
requires `interchange.tests.cooling.host-context`; `TargetedEnrichment.resolve` can add that
Fragment from an already available host value before `World.step`.

`Brontide.Minimal.Interchange.Provider` serves the same contract through native
`Cooling.apply (SetCoolingEnabled enabled)`. `--reject-protocol` and
`--crash-after-activation` are deterministic test modes.

With `--catalog`, the provider serves `upsert-items` followed by `find-items` against ephemeral
provider-owned state. `CatalogProcessClient.runScenario` verifies nested/repeated items, explicit
missing-item failure, and normal shutdown in one process. A resource other than
`catalog-sandbox/shared` returns `resource-refused`; the handle never conveys authority.

See [`../../docs/public-boundaries.md`](../../docs/public-boundaries.md) for exact payload, timeout,
cleanup, replay, redaction, and threat assumptions.

Ordinary tests skip real Brontide Reference Stack launch when `BRONTIDE_REFERENCE_PROVIDER` is absent. Use the root
`build/verify-interchange.ps1` command for the required two-way process evidence.
