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

See [`../../docs/current/policies/public-boundaries.md`](../../docs/current/policies/public-boundaries.md) for exact payload, timeout,
cleanup, replay, redaction, and threat assumptions.

Ordinary tests skip real Brontide Minimal Stack launch when `BRONTIDE_MINIMAL_PROVIDER` is absent. Use the root
`build/verify-interchange.ps1` command for the required two-way process evidence.

## Fake Component Management CM1

Load `cm1-source-evidence.json` with `Cm1FixtureLoader.LoadSourceEvidence`, then construct each
`FakeComponentSource` from the catalog, that explicit availability fixture, and its source identity.
Use `FakeDiscovery.Run` with a `DiscoveryQuery` and any number of those sources. Candidates are
attributable and deterministically ordered; a source endpoint is not a publisher. Call
`FakeComponentSource.Acquire` with a `FakeEvidencePolicy` to obtain either a detached
`StagedArtifact` or one structured refusal. Removing the source affects later calls only.

CM1 is deliberately inert. Do not add resolution behavior to the discovery or acquisition path.
The observable C1-C7 boundary is in
[`../../component-management/cm1-capability-contract.md`](../../component-management/cm1-capability-contract.md).
The completed absence audit is
[`../../component-management/cm1-contract-completeness-review.md`](../../component-management/cm1-contract-completeness-review.md).

## Fake Component Management CM2

Construct a `ResolutionRequest` from explicit definitions, candidates, existing occurrences,
occupied bindings, preferences, Parameter observations, Port envelopes, topology policy, and local
candidate-policy observations, then call `FakeGenerationResolver.Resolve`. Success returns both an
inspectable `ProposedStack` and immutable `ResolvedGeneration`; Port excess may return a wider-parent
proposal; every other failure is a structured value without a partial generation.

CM2 fills required Provider Set positions only, retains compatible occupied `1..1` bindings by
default, and preserves alternatives and exclusions. It is effect-free: it has no preparation,
activation, Actor, authority, or active-generation API. Dependency cycles return
`CycleRequiresCm3`. The C1-C10 boundary and completed absence audit are
[`../../component-management/cm2-capability-contract.md`](../../component-management/cm2-capability-contract.md)
and
[`../../component-management/cm2-contract-completeness-review.md`](../../component-management/cm2-contract-completeness-review.md).
