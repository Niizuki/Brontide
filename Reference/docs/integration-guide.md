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
process-local unless it is wrapped by `DurableProviderPublisherTrustPolicyRegistry`. Open that
registry with a host-owned checkpoint path, the authority pin, and the last independently retained
recovery floor. Retain each returned floor separately only after a successful update. Recovery
re-verifies the full signed chain and can create the same governed acquisition gate; the checkpoint
does not itself provide secure floor custody or multi-process coordination.

For one remote synchronization attempt, construct
`ProviderPublisherTrustPolicyDistributionClient` with that durable registry and the separately
configured distribution-endpoint SPKI identity. Implement
`IProviderPublisherTrustPolicyDistributionSource` for the chosen transport and pass the host clock,
a timeout no greater than one minute, and cancellation to `SynchronizeAsync`. The endpoint signs the
CBI39 response manifest; the optional update still needs its independent CBI37 authority signature.
The source abstraction is not an HTTP/TLS profile and the client performs no retries.

CBI40 supplies that concrete source for HTTPS. Construct
`HttpProviderPublisherTrustPolicyDistributionSource` with a long-lived host-configured `HttpClient`
and one absolute HTTPS endpoint, then pass it to the CBI39 client. The source sends the canonical
CBI40 binary request and accepts only the exact response status, final URI, media type, unencoded
body, and 1 MiB stream bound. Configure certificate, redirect, DNS, and proxy policy on the injected
handler; the source does not own or dispose the client and still performs no retry.

CBI41 is where retry lives. Build a `ProviderPublisherTrustPolicyPollSchedule` with an attempt
budget, base delay, multiplier, delay cap, and per-attempt timeout, then call
`ProviderPublisherTrustPolicyPoller.PollAsync` with the source, an
`IProviderPublisherTrustPolicyFloorSink`, an `IProviderPublisherTrustPolicyPollDelay`, and the
current instant. One call performs one cycle and returns; scheduling the next one, and any jitter,
belong to your delay implementation and your host. Persist each floor the sink receives in storage
independent of the checkpoint file and feed the latest one back to
`DurableProviderPublisherTrustPolicyRegistry.Open` on the next start — a `policy-poll-floor-unretained`
result means an update is durable that your floor does not yet cover.

CBI42 is the store to persist it in. Call `ProviderPublisherTrustPolicyCustody.Open` with the
checkpoint path, a floor path, and the pinned authority; it establishes the floor store on a first
start, refuses a checkpoint whose store is missing or unreadable, and opens the durable registry
under the stored floor. Pass the returned `Floors` straight to `PollAsync` as its sink. Put the floor
somewhere the checkpoint's writer cannot reach — its integrity tag detects corruption, not tampering
— and do not write a recovered floor back to it: the store is advanced by handoffs only, on purpose.

CBI43 wires the stages together. `ProviderDistributionChain.Run` takes the durable registry, a
content-addressed store, a transaction root, the acquisition request with its publisher evidence and
allowed arguments, and the source; it answers a launched provider or a refusal that carries the
originating slice in `RefusedBy`. Dispose the returned provider to release the removal lease, then
remove the staged set.

CBI44 makes the launch take its own trust decision inside that call. Before the store activates the
staged set, the verified publisher evidence is evaluated again against the policy the registry holds
then, so a publisher revoked between acquisition and launch does not run. `AcquisitionPolicyIdentity`
and `LaunchPolicyIdentity` report the two decisions, and they may differ without refusing anything —
what has to hold is the decision, not the snapshot. Read `Revalidated` to tell a refusal that
happened before the launch decision from one that happened after it; the refusal codes are CBI35's
either way.

CBI45 revalidates after Release. Call `ProviderServingTrustRevalidation.ActivateAsync` to bind the
launched chain result to the lifecycle created over that provider's own conversation; keep the opaque
`ProviderServingActivation`, then pass it with the durable registry and store to `RevalidateAsync`.
`publisher-trust-current` leaves service alone. A CBI35 revocation or unknown-key refusal retires the
member, terminates the provider, releases its lease, and attempts removal. Inspect `RetirementCode`
for cleanup failure. The call has no timer or fan-out policy.

CBI46 supplies the explicit fan-out call. Pass 1-64 opaque activations to
`ProviderServingTrustSweep.RunAsync`; it preflights the complete set, orders by `Occurrence`, and
returns one CBI45 result per member plus aggregate counts. Invalid sets have no effect. The sweep
owns staged-set removal so an identity shared with a continuing member remains staged. Callers still
own writer serialization, durable retry, and restart policy.

CBI47 supplies one bounded cadence. Build a `ProviderPublisherTrustPolicyCycle` around the CBI41
poller and a `ProviderServingTrustSweepCycle` around the source of current opaque activations, then
pass both through `ProviderServingTrustCycle` to `ProviderServingTrustCadence.RunAsync`. The first
cycle is immediate; later cycles use only the injected delay. A current policy is required before the
serving set is snapped. Empty sets are successful no-ops, successful withdrawals continue, and any
non-current poll or incomplete sweep stops before another gap. The result is an observation of this
run, not a durable schedule or restart instruction.

CBI48 persists one bounded run. Create a distinct `ProviderTrustCadenceRunId`, then call
`DurableProviderTrustCadenceJournal.Establish` with the CBI47 schedule and start instant. Drive one
recoverable step with `ProviderTrustCadenceRecovery.AdvanceAsync`; it writes in-flight before calling
the cycle and commits the returned code afterward. On restart, call `Open`. Ready or waiting journals
continue from the next uncommitted index, terminal journals are inert, and
`durable-cadence-indeterminate` requires external reconciliation before calling `ResolveInterrupted`
with `Retry` or `Abandon`. Retry is an explicit replay decision, not an exactly-once guarantee.

CBI49 makes those host decisions explicit. Create `ProviderTrustOfflinePolicy` with a grace and retry
interval, then call `Evaluate` with injected `now`, the last cycle instant that established current
policy, the CBI41 poll and last-attempt codes, and the current serving count. Only an exhausted
transport failure or timeout can produce `offline-existing-service`, and that result never permits a
provider start. `offline-grace-expired` and `offline-service-stop-required` are instructions for host
supervision; this policy does not terminate providers itself. For an in-flight CBI48 journal, pass a
`ProviderTrustCadenceReconciliationEvidence` naming its exact run, index, and instant to
`ProviderTrustCadenceReconciliation.Apply`. Unknown or mismatched evidence preserves the journal;
confirmed no-effect selects retry, while accounted effects select abandonment.

CBI50 connects the offline decision to serving effects. Call
`ProviderOfflineServiceEnforcement.RunAsync` with the policy inputs, the exact current activation
snapshot, and a retirement reason. A within-grace result leaves every provider untouched. Expiry or
any fail-closed stop decision retires and terminates every admitted activation in typed occurrence
order and returns one observation per member. Staged artifacts remain: this is availability
enforcement, not trust revocation, and the coordinator never restarts a provider.

CBI51 decides whether that later restart may be attempted. Create `ProviderRestartPolicy` with a
maximum attempt count and delay, then call `Evaluate` with the stopped activation, a typed cause, the
exact policy identity established by the latest successful current cycle, injected time, and attempt
history. `provider-restart-ready` carries a fresh authorization for the activation's retained content;
waiting, exhausted, terminal-cause, stale-proof, trust, and malformed-history outcomes deny. The
policy is effect-free and does not recreate the process or portable lifecycle.

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
