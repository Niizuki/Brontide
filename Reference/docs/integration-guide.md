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

For a host-controlled local executable, use `LocalProviderArtifactActivator.AcquireAndLaunch` in
Reference Studio instead of constructing a process directly. Supply the canonical source path,
expected uppercase SHA-256 digest, parsed argument vector, allowed root, and exact admitted argument
vector. A launched owner exposes the existing portable conversation and owns process-tree cleanup.
`dedicated-process` is an isolation observation, not a sandbox claim; CBI31 does not secure mutable
or link-controlled source directories.

For a complete local provider output, construct a `ProviderArtifactSet` from safe relative paths and
uppercase member digests, derive its `ProviderArtifactSetId` with
`ProviderArtifactSetIdentity.Compute`, and call `ContentAddressedProviderStore.Stage`. Staging is
inactive. `Activate` leases the staged set and enters the existing CBI31 lifecycle; dispose the
returned owner before calling `Remove`. One store instance owns the in-memory lease state. CBI32 does
not add cross-process locking, crash recovery, remote acquisition, publisher evidence, or garbage
collection.

To acquire that declaration from a stream source, add exact byte lengths, an expected
`ProviderArtifactSourceId`, and a total limit in `ProviderArtifactAcquisitionRequest`, then call
`ProviderArtifactAcquirer.Acquire`. The source is opened once per member in canonical order. Inspect
`TransportCode`, `PublisherEvidenceCode`, and `AdmissionCode` independently; only `IsStaged` exposes
a value suitable for the existing CBI32 activation lifecycle. This synchronous seam bounds bytes,
not time, and does not authenticate the named source or implement a network protocol.

For detached publisher-key evidence, sign `ProviderArtifactPublisherManifest.Encode(request)` with
ECDSA P-256/SHA-256 using an RFC 3279 DER signature. Set `ProviderPublisherKeyId` to the uppercase
SHA-256 digest of the exact SubjectPublicKeyInfo bytes, then call
`ProviderArtifactPublisherEvidenceVerifier.Verify`. A valid result supplies a detached verified
value, but `TrustCode` remains `publisher-trust-not-evaluated` and `AdmissionCode` remains
`admission-not-attempted`; host policy must decide whether to proceed to CBI33.

Build a canonical `ProviderPublisherTrustPolicy` with exact key dispositions, derive its identity
with `ProviderPublisherTrustPolicyIdentity.Compute`, and pass that snapshot plus the CBI34 verified
value to `ProviderPublisherTrustEvaluator.Evaluate`. Only `publisher-trusted` carries a
`TrustedProviderPublisherAuthorization`; require its content identity and payload digest to match
the acquisition request before explicitly invoking CBI33. Revoked and unknown are distinct, and the
evaluator never opens a source or attempts admission.

To enforce that decision, construct `TrustedProviderArtifactAcquirer` around the existing CBI33
acquirer and call `Acquire` with the request, source, and CBI35 authorization. The gate snapshots and
validates the request, then matches both content identity and canonical publisher-payload digest
before CBI33 can inspect the source. `TrustedProviderPublisherAuthorization` can no longer be
constructed directly; obtain it from a successful `ProviderPublisherTrustEvaluator.Evaluate` result.

For authoritative policy changes, pin `ProviderPublisherTrustPolicyAuthorityId` from trusted host
configuration and apply signed `ProviderPublisherTrustPolicyUpdate` values to
`ProviderPublisherTrustPolicyRegistry`. Bootstrap is sequence 1 without a predecessor; successors
increment once and name the current policy. Wrap CBI36 with `GovernedProviderArtifactAcquirer` so an
authorization issued under a superseded snapshot is refused before source access. Registry state is
process-local and must not be presented as durable anti-rollback storage.

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

## Fake Component Management CM3

Construct a complete immutable `ActivationGroupRequest` from resolved occurrences, dependency
edges, lifecycle-protocol declarations, and Region-crossing declarations, then call
`FakeActivationGroupPlanner.Plan`. Success returns maximal strongly connected groups, a
dependency-first condensation order, explicit closed-gate lifecycle stages through Ready, retained
Region crossings, and deterministic decisions. A Region escape that policy may widen returns a
`WiderParentGenerationRequired` proposal; every other invalid graph returns a structured refusal.

CM3 is analysis only. It neither prepares nor establishes Components, invokes lifecycle Operations,
accepts runtime Ready reports, releases ordinary interaction, nor mutates active generations.
Relational Initialisation is group-internal; cross-group relational traffic is refused. The C1-C9
boundary and completed absence audit are
[`../../component-management/cm3-capability-contract.md`](../../component-management/cm3-capability-contract.md)
and
[`../../component-management/cm3-contract-completeness-review.md`](../../component-management/cm3-contract-completeness-review.md).

## Fake Component Management CM4

Construct an `ActivationRuntimeRequest` from one successful CM3 `ActivationGroupPlan`, the exact
restart scope and retained generation, a complete active-scope snapshot, one member outcome for
every planned stage, explicit interaction and post-Release binding observations, a typed Release,
rollback availability, retained-generation disposition, and an optional child-Port declaration.
Call `FakeActivationRuntime.Activate`.

The Host validates all inputs before establishment, advances groups and stages without inventing
member order, admits only exact declared lifecycle traffic before Release, admits declared ordinary
edges and binding exercises only afterward, and returns complete scope/effect evidence for success,
pre-cutover failure, restoration, or degradation. It is deterministic fake runtime evidence, not a
loader, process supervisor, durable rollback system, or authority policy. The C1-C10 boundary and
completed absence audit are
[`../../component-management/cm4-capability-contract.md`](../../component-management/cm4-capability-contract.md)
and
[`../../component-management/cm4-contract-completeness-review.md`](../../component-management/cm4-contract-completeness-review.md).
