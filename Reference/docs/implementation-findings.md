# Brontide Reference Stack implementation findings

These findings describe retained implementation evidence, including historical Architecture 0.5
results. New architectural decisions come from
[Architecture 0.7](../../Brontide-Architecture-0.7.md); planned Reference responses are classified
in [`architecture-0.7-delivery.md`](./architecture-0.7-delivery.md). A historical finding is not
silently upgraded to 0.7 evidence.

## Architecture 0.5 composition guardrails and evidence boundary

- **Brontide sections:** 6.12–6.14, 18.2, 30.1, 32.2, 33
- **Minimal scenario:** the image workspace executes one semantic `Brontide:Image.Invert` Operation
  first through a small CPU module, adopts execution history, searchable metadata, and shared
  workspace state independently, replaces its metadata provider, and requests acceleration for a
  larger workload.
- **Observed result:** the transformation module has no system-facility dependencies or inferred
  execution properties. Dependency requirements retain generic-contract, stronger-Profile,
  system-preference, and provider-specific strength. The vector provider is selected only when it
  explicitly claims purity, determinism, batchability, and accelerator compatibility. Selection,
  placement, representation, boundaries, batching, copies, retries, fallback, provider, failure
  domain, Outcome, and timing are returned as one structured experimental explanation. Opaque boxed
  Components remain valid descriptors without exposing private dependencies.
- **Expected result:** Architecture 0.5 requires operational truth and explicit optimisation claims
  while deliberately leaving the portable Component descriptor, dependency negotiation, execution
  explanation, and optimisation-property vocabularies open.
- **Classification:** experimental result
- **Current disposition:** keep the implementation in `Brontide.Reference.Experimental.Composition` and its
  tests outside normative conformance. The vector path uses `System.Numerics` and is identified as a
  vector provider, not a GPU. Real Brontide Minimal Stack interchange and cross-machine/cross-domain binding remain
  outstanding cross-stack evidence. GPU execution is separately classified as a planned
  experimental sideline project; its compilation, copy, dispatch, failure, and fallback evidence is
  neither simulated nor counted against the current showcase. Cooling interchange is now real
  cross-stack evidence, but the mixed image workspace and machine boundary remain outstanding.

## First portable-binding experiment separates authority from invocation

- **Brontide sections:** 16, 18.1-18.2, 30, 33
- **Minimal scenario:** Brontide Reference Stack hosts a Brontide Minimal Stack Cooling provider and Brontide Minimal Stack hosts a Brontide Reference Stack Cooling
  provider through protocol-versioned JSON lines while a denied host request, unknown Constraint,
  missing required Fragment, semantic failure, and provider crash are exercised.
- **Observed result:** exact contract negotiation and host authority can remain entirely native.
  The provider needs accepted shaped input plus binding-scoped identifiers, not a serialized
  Capability. Optional authored data can be ignored by the semantic provider yet preserved by the
  binding. Provider selection, copies, boundary, authority point, mapping/Adapter obligations,
  interruption, retry, fallback, failure domain, Outcome, and explicit clock time fit one
  inspectable experimental observation in both stacks.
- **Expected result:** Architecture 0.5 asks interchange to discover the minimum portable seam
  without allowing either implementation's private model to become Brontide.
- **Classification:** experimental cross-stack result
- **Current disposition:** retain protocol version 2, its descriptor, and observation types in
  experimental binding projects. The Brontide Reference Stack binary-to-fan mapping is an explicit semantic Adapter;
  it loses no information promised by the neutral binary contract. Treat the result as input to
  the Architecture 0.8 Portable Binding and Channel work, not as ratification: Event/Flow, bulk
  resources, a machine boundary, and a third implementation are still absent.

## Targeted Enrichment failure attribution

- **Brontide section:** 16.6
- **Minimal scenario:** `Input.Pointer.Move` requires `Experiment:ThermalContext`, but its active
  composition has no provider or lacks the declared telemetry source.
- **Observed result:** resolution fails deterministically before `AuthorityDomain.ExecuteAsync`;
  therefore no Base Execution exists in the Core provenance log. The experimental exception names
  the composition, boundary, provider, and missing or competing source.
- **Expected result:** Architecture 0.4 deliberately leaves open whether pre-Execution Enrichment
  failures need attributable Brontide occurrence semantics.
- **Classification:** experimental result
- **Current disposition:** retain visible experimental failure outside Core and normative
  conformance. The experiment uses direct materialisation by default and records the selected
  realisation strategy in `EnrichmentTrace`; no strategy is treated as semantic.

## Scalar authority payloads and mutable constraint carriers

- **Brontide sections:** 10, 11, 16
- **Minimal scenario:** an arbitrary CLR object, including a `Capability` or mutable collection,
  is wrapped in a scalar `ShapeValue` and copied through Enrichment or used as a Constraint value.
- **Observed result:** scalar projection previously checked only the Shape name and wrapper kind;
  the object could cross a boundary or mutate after Capability issuance.
- **Expected result:** Shape values carry structural information rather than authority, and a
  Capability's effective Constraints cannot be rewritten or broadened after issuance.
- **Classification:** Brontide Reference Stack defect
- **Current disposition:** fixed. Scalar Shapes now declare one approved immutable carrier type,
  projection checks that representation, and arbitrary reference objects and Capabilities are
  rejected. Core and experimental tests exercise actual payload rejection.

## Direct Event origin skipped Capability mortality

- **Brontide sections:** 10.1, 10.3, 15
- **Minimal scenario:** a Device-origin Capability also carries a liveness lease; after expiry the
  holder calls the public Event-emission API directly.
- **Observed result:** the old origin check inspected only the origin grant and holder, so the Event
  retained `Origin.Device` despite the expired lease.
- **Expected result:** direct origin assertion evaluates every applicable authority restriction and
  fails closed; an already-authorised Execution may transfer its checked origin only while its
  handler is active.
- **Classification:** Brontide Reference Stack defect
- **Current disposition:** fixed. Direct Event origin evaluates mortality and rejects Constraints
  without Event semantics; Execution contexts are deactivated when dispatch completes.

## Flow bypassed Base authority and origin checks

- **Brontide sections:** 15, 19.1
- **Minimal scenario:** call `RecoverableFlow.Publish` directly and pass `Origin.Device`, then call
  replay directly through the cursor.
- **Observed result:** Flow creation, Item publication, and replay were plain method calls; the
  caller selected trusted origin without presenting authority.
- **Expected result:** Flow uses Base Operations and Outcomes, effectful Item publication is
  independently or continuously authorised, gap detection is visible, and replay is derived.
- **Classification:** Brontide Reference Stack defect
- **Current disposition:** fixed. `FlowRuntime` registers checked open, Item-publication, and replay
  Operations; opening returns a Flow handle, each Item is independently authorised, gaps emit
  `Flow.GapDetected`, spoofed Device origin denies, and replay preserves the producer while marking
  origin Derived.

## M0 failing-first observation is not retained

- **Brontide section:** 29
- **Minimal scenario:** audit the Git history for the M0 tests failing before implementation.
- **Observed result:** the Brontide Reference Stack implementation and tests entered the repository together in one
  commit, so the observation cannot be reconstructed.
- **Expected result:** the implementation plan asks that expected failures be observed before the
  implementation is added.
- **Classification:** plan gap
- **Current disposition:** documented in `docs/milestone-evidence.md`; current behavioural gates are
  repeatable, but historical evidence is not claimed or fabricated.

## Catalog/resource interchange widens experimental evidence

- **Brontide section:** Architecture 0.7 §30 interchange challenge, retained Architecture 0.5
  evidence baseline, and implementation correction plan
- **Minimal scenario:** upsert nested/repeated catalog items, find them through a second Operation,
  request a missing item, replay a request, cross an out-of-scope resource handle, and exceed the
  protocol line limit against the independently implemented Minimal provider.
- **Observed result:** both host directions preserve item order/tags, return shaped failure,
  refuse the resource before mutation, and visibly reject malformed, unknown, version-skewed,
  replayed, and over-limit input.
- **Classification:** experimental cross-stack result
- **Current disposition:** retain Catalog v1 beside Cooling v2 as a separate proof. The 65,536-byte
  limit, depth 32, process-local replay window, ephemeral provider state, and lack of Capability
  transfer are public limits. Do not infer persistence, network isolation, authority federation, or
  portable-binding ratification.
