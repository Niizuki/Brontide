# Brontide Portable Component Binding Implementation Plan 0.1

**Status:** Planned experimental work
**Date:** 2026-07-23
**Designed for:** [Brontide Architecture 0.8](../../../Brontide-Architecture-0.8.md) §16 and
§18.1, Complete Draft, not ratified
**Design sources:** [Composition and Components](../../../Brontide-Design-Note-Composition-0.1.md),
[Channel](../../../Brontide-Design-Note-Channel-0.1.md), and
[Draft Channel Contract 0.1](../../../Brontide-Draft-Channel-Contract-0.1.md)
**Evidence baseline:** [Reference/Minimal Interchange Implementation Plan 0.1](../../archive/interchange/Brontide-Interchange-Implementation-Plan-0.1.md)

## 1. Goal and evidence boundary

Deliver the first reusable, independently implemented Brontide Portable Component Binding contract.
The binding lets a Host establish a precomputed Binding Plan and invoke a compatible Component
without sharing a language object model, runtime library, private exception type, or authority
object. Reference and Minimal must implement the same observable contract natively and demonstrate
both direct-call and process-isolated realizations.

This is not a greenfield protocol project. The repository already contains two-way Cooling and
resource-scoped Catalog experiments in:

- `Reference/src/Brontide.Reference.Experimental.Binding`;
- `Minimal/src/Brontide.Minimal.Binding`;
- `Reference/tests/Brontide.Reference.Interchange.Tests`;
- `Minimal/tests/Brontide.Minimal.Interchange.Tests`; and
- the neutral fixtures under `interchange/`.

Those experiments prove useful mechanics: strict manifests and values, exact negotiation, shaped
success and failure, host-side authority before a provider effect, cross-process invocation in both
directions, correlation identities, process failure observations, replay and size defenses, and a
provider-scoped resource handle. They remain fixture-specific experimental evidence. They do not
yet define a reusable Binding Plan, execute every Channel vector, demonstrate direct/process
semantic parity, publish a portable representation contract, or expose a general referenced-shaped-
resource model.

The work remains experimental until the architecture and Channel contract are ratified. It does not
change either stack's Architecture 0.7 implementation target and does not make the Portable Binding
part of Brontide Base.

## 2. Capability contract

Implementation begins by accepting the following observable contract. Public surface design is
subordinate to these capabilities.

### C1 — neutral contract establishment

A Host and provider establish one versioned Component contract before any provider effect. The
contract uses canonical Operation, Shape, Fragment, Component, and dependency identities; unknown
required versions, identities, control fields, or features fail closed. Binding-scoped compact
identifiers may be assigned only after canonical negotiation and never become persistent identity.

### C2 — complete Binding Plan

One immutable, inspectable Binding Plan fixes the negotiated contracts, actor endpoints, authority
presentation mode, payload representation, resource ownership, synchronization, delivery limits,
and failure/lifecycle behavior for one binding scope. The plan may be explicit data or compiled
away, but both realizations expose equivalent evidence of what was fixed.

### C3 — authority remains local

Within one authority domain, a declared Capability presentation may be evaluated at the target
boundary. Across a trust boundary no Capability crosses: the provider receives only attributable
context and exact addressing, and its domain performs its own admission. A local denial or unknown
authority condition starts no provider and emits no Channel frame.

### C4 — Channel 0.1 semantics

Every realization preserves the Channel envelope kinds, correlation rules, two-plane variance,
standard protocol-error categories, process-failure observations, and relative failure domains.
Semantic failure is a shaped failed Outcome. Protocol rejection and process loss remain distinct.
No private exception, stack trace, runtime type name, or authority object crosses the seam.

### C5 — portable shaped values

The contract supports a measured Shape floor covering the standard scalars required by the chosen
version, nested records, sequences, required fields, open/closed record policy, declared Fragments,
additive payload projection, and strict authority/control positions. Mapping preserves one semantic
contract; conversion between different Operations or Shapes is an explicit Adapter Component, not
a hidden codec feature.

### C6 — inline and referenced payloads

The binding supports inline shaped values and a minimal referenced-shaped-resource form. A resource
reference declares its representation, scope, access, ownership or borrowing interval, lifetime,
release/completion signal, integrity rule, and fallback policy. Incompatible resources or forbidden
implicit copies are rejected visibly rather than reported as successful negotiation.

### C7 — realization independence and parity

Reference and Minimal implement the contract independently. A fixed direct-call realization and a
process-isolated realization produce the same category-level semantic observations for equivalent
vectors. Neither implementation references the other's assemblies, private CLR types, codecs, or
semantic runtime logic.

### C8 — bounded and explicit lifecycle

Establishment, readiness, invocation, withdrawal, and termination are explicit states. Declared
frame, payload, nesting, field-count, and resource limits are enforced before uncontrolled work.
Correlation mismatch, illegal state, peer termination, timeout, and interruption never fabricate a
success. Retry, cancellation, ordering, streaming, and exactly-once execution are not implied.

### C9 — attributable observations

Each completed or rejected interaction can report the selected provider, selection reason,
negotiated identities and versions, representation, crossed boundaries, copies and referenced
resources, authority decision point, mapping/adapter obligations, retry count, interruption,
failure domain, terminal status, correlation mapping, timing, and provider-effect count where it is
observable. Diagnostics do not drive portable semantics.

### C10 — executable interoperability evidence

Both host/provider directions pass the same neutral vectors; both stacks also pass native direct
realization tests. At least one provider endpoint or fixture implementation depends on neither stack
and proves that the contract is implementable without importing either private model.

## 3. Non-goals

- Ratifying Architecture 0.8, Channel, Composition, or a final wire encoding.
- Implementing the complete Component Manager, source discovery, package acquisition, marketplace,
  hot swap, mediation, or generational resolver.
- Moving transport, composition, persistence, or provider selection into Base/Core/Model/Kernel.
- Sharing one binding implementation between Reference and Minimal.
- Promising network security, identity federation, exactly-once delivery, retries, cancellation,
  streaming, or long-running lifecycle semantics in version 0.1.
- Treating JSON, CBOR, CLR records, F# records, or any one in-memory layout as the semantic model.
- Optimizing the hot path before the contract and observation parity are demonstrated.

## 4. Ownership and target layout

Repository-wide contract artifacts belong under `binding/portable/` and contain data-only schemas,
manifests, golden values, adversarial vectors, and an implementation-neutral contract matrix. They
must not contain executable semantic logic shared by the stacks.

Reference owns its implementation and tests under its existing experimental binding project until
promotion is explicitly approved. Minimal owns its implementation and tests under its binding
project, which remains documented as experimental evidence despite its shorter assembly name. Each
stack owns native adapters between its private Shape/authority model and the neutral positions.

Cross-process orchestration remains in the interchange test estate. A new root
`build/verify-portable-binding.ps1` should build required provider endpoints, run native and both
cross-stack directions, validate neutral artifacts, and restore any generated evidence
deterministically. The repository-wide gate invokes it.

## 5. Delivery sequence

### PB0 — baseline and contract freeze

1. Inventory every existing Cooling and Catalog manifest field, message kind, value variant,
   correlation identity, error code, limit, resource rule, and observation field.
2. Map each existing behavior to C1-C10 and to the Channel 0.1 vectors. Mark behavior as reusable,
   fixture-specific, contradictory, or missing; do not copy contradictions into the new contract.
3. Create the neutral contract matrix and data-only vector directories under `binding/portable/`.
4. Resolve the version-0.1 representation and resource-floor questions listed at the end of this
   plan. Record exact canonicalization and bounds before implementing another codec.
5. Preserve the existing Cooling and Catalog gates throughout extraction so the baseline never
   disappears while the reusable surface is built.

**Exit:** every C item and Channel vector has an owner, evidence path, and expected category-level
observation; unresolved encoding questions are explicit blockers rather than implicit code choices.

### PB1 — neutral manifests, plans, and vectors

Define data-only versioned contracts for:

- canonical references and the supported Shape floor;
- Component provisions and requirements;
- negotiated Operations, input/result/detail Shapes, and required Fragments;
- authority-presentation mode and cross-trust `no-capability-transfer` declaration;
- inline representations and referenced-shaped-resource declarations;
- delivery/hardening limits and lifecycle features;
- immutable Binding Plan facts;
- Channel envelopes, correlation, protocol errors, and process-failure observations; and
- binding observations required by C9.

Include valid, additive-compatible, and adversarial fixtures. Unknown fields in control/authority
positions, unknown variants, duplicate fields, version skew, malformed data, mismatched
correlation, replay where declared, limit violations, illegal lifecycle transitions, incompatible
Shapes/Fragments, forbidden resource scope, and exception-shaped data must all have exact expected
outcomes.

Keep the neutral layer free of generated C#/F# source and runtime helpers. If schemas generate code,
generation runs separately in each stack and the checked neutral source remains authoritative.

**Exit:** the artifacts are self-contained, deterministic, linkable from both implementations, and
validated without loading either stack.

### PB2 — Reference native implementation

Refactor reusable behavior from `Brontide.Reference.Experimental.Binding` behind a fixture-neutral
contract. Implement strict decode/validation, plan compilation, native Shape projection, authority
presentation/admission, direct-call dispatch, process framing, resource-scope checks, lifecycle,
and C9 observations using Reference-owned types.

Cooling and Catalog become adapters/fixtures over the reusable layer rather than definitions of the
layer. Keep Core free of binding and transport dependencies. Tests cover each neutral vector before
cross-stack orchestration is involved.

**Exit:** Reference passes all neutral vectors and both its direct-call and local process
realizations report equal semantic observations.

### PB3 — Minimal native implementation

Perform the corresponding extraction in `Brontide.Minimal.Binding`, using Minimal-owned algebraic
data types and explicit results. Do not translate the Reference surface mechanically or introduce
an object-oriented compatibility facade merely to make tests look alike. Keep Model/Kernel free of
transport and composition dependencies.

Cooling and Catalog use the reusable contract through Minimal-native adapters. Tests cover the same
neutral vectors, including strong three-valued authority evaluation and polarity-flip cases at the
Channel boundary.

**Exit:** Minimal passes all neutral vectors and both its direct-call and local process
realizations report equal semantic observations.

### PB4 — direct and process realization parity

Exercise one fixed-contract direct realization and one negotiated process realization in each
stack. Normalize only category-level portable observations; retain implementation-specific
diagnostic codes as non-normative data. Verify that denial/no-frame decisions, semantic Outcomes,
protocol categories, correlation, failure domains, payload projection, and resource refusal match.

The process realization uses a real duplex process boundary. Framing must be length-delimited and
bounded for the portable realization; the retained line-delimited JSON protocol may remain as a
diagnostic/legacy experiment but cannot silently become the portable wire contract.

**Exit:** every Channel 0.1 vector executes independently in both stacks, and direct/process parity
holds for the portable observation set.

### PB5 — cross-stack and independent-provider matrix

Run at least these combinations:

| Host | Provider | Realization |
| --- | --- | --- |
| Reference | Minimal | negotiated process |
| Minimal | Reference | negotiated process |
| Reference | implementation-neutral fixture | negotiated process |
| Minimal | implementation-neutral fixture | negotiated process |
| Reference | Reference | fixed direct call |
| Minimal | Minimal | fixed direct call |

Use Cooling for authority, projection, enrichment, shaped failure, and provider-effect checks. Use
Catalog for multiple Operations, nested/repeated data, provider-scoped resources, one-session state,
replay, explicit refusal, and bounds. Add a materially different small fixture only if C1-C10 cannot
be demonstrated without teaching the reusable layer fixture-specific rules.

**Exit:** both directions and the independent provider pass without shared executable semantic
logic or private runtime types.

### PB6 — resource, lifecycle, and hardening completion

Add adversarial coverage for ownership transfer/borrowing, premature reuse, release/completion,
scope escape, integrity mismatch, unsupported fallback, and forbidden implicit copy. Record memory
domain and copy facts even when version 0.1 supports only a conservative resource subset.

Exercise establishment failure before activation, withdrawal, clean termination, peer loss,
timeout, interrupted frames, unknown lifecycle actions, duplicate terminal responses, and resource
exhaustion. Fuzz or property-test decoders within deterministic bounds. Prove that failure paths do
not leak a provider effect, authority, resource handle, exception, or false success.

**Exit:** C6 and C8 have positive and negative evidence in both stacks and across the process seam.

### PB7 — Composition handoff without Component Manager expansion

Add the narrow adapter by which a resolved Component requirement and offered provision can produce
a Binding Plan during activation preflight. Preserve binding scope and provider identity. The seam
must be usable later by the Component Manager plan, but PB7 does not implement discovery,
acquisition, provider selection policy, generations, mediation, or hot swap.

Record which Binding Plan facts are fixed before Interconnection, which readiness signal is required
before Release, and how withdrawal/termination informs a future replacement generation. No
ordinary interaction starts before the plan is established and the provider is ready.

**Exit:** one controlled experimental composition in each stack establishes and releases a portable
binding without moving the binding into Base/Core/Model/Kernel.

### PB8 — evidence, documentation, and review closure

1. Update both stack READMEs, experimental-project inventories, milestone evidence, public boundary
   documentation, and changelogs where observable behavior changed.
2. Update the Channel ledger and contract matrix with direct/process and cross-stack evidence. Do
   not upgrade ratification or architecture-target language.
3. Re-measure source/runtime costs and record representation, allocation, copy, and payload-bound
   facts for both realizations.
4. Run `build/verify-portable-binding.ps1`, then the complete repository gate
   `build/verify-interchange.ps1` from a clean worktree.
5. Obtain fresh independent reviews of Reference, Minimal, and the neutral contract. Reviewers must
   evaluate C1-C10 and the current Architecture 0.8 draft while respecting each stack's stated 0.7
   implementation target.
6. Move every answered question to `Resolved questions`; retain only actual blockers under `Open
   questions (owners needed)`.

**Exit:** all C1-C10 evidence is passing and discoverable, limitations are current, the complete
gate is green, and independent reviews contain no unresolved in-scope findings.

## 6. Mandatory evidence matrix

At minimum, automated evidence covers:

- exact and incompatible contract establishment before provider activation;
- unknown Operation, Shape, Fragment, dependency, feature, message kind, and version;
- every Channel 0.1 protocol-error category and process-failure observation;
- matching, missing, extra, replayed, and mismatched correlation identities;
- local denial, unknown constraint, and missing required Fragment producing zero provider effects;
- successful inline nested/repeated values and additive payload projection;
- shaped semantic failure without exception transport;
- authority/control unknowns failing closed despite payload covariance;
- direct/process category-level parity in each stack;
- Reference-hosts-Minimal and Minimal-hosts-Reference success and failure;
- an implementation-neutral provider accepted by both hosts;
- referenced resource success, scope refusal, ownership/lifetime failure, and unsupported fallback;
- frame, payload, depth, field-count, and resource limits;
- establishment, readiness, withdrawal, termination, timeout, interruption, and peer loss;
- observation completeness, including copies, boundaries, authority point, failure domain, and
  provider-effect count; and
- dependency guards proving that neither stack nor neutral artifacts import the other's runtime.

## 7. Completion gate

The Portable Component Binding 0.1 evidence goal is complete only when:

1. C1-C10 are implemented independently and mapped to passing evidence in both stacks;
2. the neutral contract is data-only and self-contained;
3. fixed direct and negotiated process realizations have semantic parity;
4. both cross-stack directions and an implementation-neutral provider pass;
5. no Capability, private exception, runtime type identity, or shared semantic runtime crosses;
6. inline and the selected referenced-resource floor have positive and adversarial evidence;
7. both dependency guards and the portable-binding gate pass without warnings;
8. the full repository gate passes from a clean worktree;
9. current documentation states the experimental status and remaining limitations accurately; and
10. fresh independent reviewers find no unresolved in-scope contract or implementation defect.

Passing this gate establishes experimental implementation evidence. Ratification, public package
promotion, and an Architecture 0.8 implementation claim remain separate decisions.

## Open questions (owners needed)

| Owner | Question | Blocking point |
| --- | --- | --- |
| Brontide architecture maintainers | Ratify the provisional Channel Shape/category names or publish an explicitly migrated revision? | Blocks a stable public Portable Binding version; experimental PB0-PB6 may proceed against a versioned draft. |
| Portable Binding contract maintainers | Which restricted schema-guided CBOR subset, scalar tags, canonicalization rules, identifier widths, and maximum bounds define the first process realization? | Blocks PB1 wire fixtures and PB4 portable-process conformance. |
| Portable Binding contract maintainers with both stack owners | What is the smallest referenced-shaped-resource v0.1 contract: copied immutable blob, borrowed read-only region, transferred ownership, or a deliberately smaller subset? | Blocks PB1 resource schema and PB6 completion. |

## Resolved questions

- **2026-07-23 — Architecture scope:** Portable Binding remains outside Brontide Base and does not
  change either stack's Architecture 0.7 target.
- **2026-07-23 — Starting point:** reuse and refactor the Cooling/Catalog evidence; do not replace it
  with a disconnected greenfield protocol.
- **2026-07-23 — Independence:** share data-only contracts and vectors, never executable semantic
  runtime logic or private stack types.
- **2026-07-23 — Authority:** no Capability crosses a trust boundary; local denial produces no
  provider effect and no Channel frame.
- **2026-07-23 — Realizations:** both fixed direct-call and negotiated process realizations are
  required because Channel readiness explicitly requires their semantic parity.
- **2026-07-23 — Failure model:** semantic Outcome, protocol error, process failure, and local denial
  remain distinct; no exception transport is permitted.
- **2026-07-23 — Adaptation:** representation mapping may preserve one Shape contract; semantic
  translation requires an explicit Adapter Component.
- **2026-07-23 — Lifecycle promises:** retry, cancellation, streaming, ordering, and exactly-once
  execution remain non-promises for version 0.1.
- **2026-07-23 — Component Management boundary:** this plan delivers only the Binding Plan handoff;
  discovery, selection, acquisition, generations, mediation, and hot swap remain in the separate
  Component Management programme.
- **2026-07-23 — Promotion:** implementations remain experimental until architecture ratification
  and a separate public-package decision.
