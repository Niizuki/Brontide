# Brontide Portable Component Binding Implementation Plan 0.1

**Status:** Partially implemented experimental work — PB0 through PB7 complete; PB8 partly complete
(evidence and documentation delivered; Decision 11 ruled on 2026-07-30; independent review outstanding)
**Date:** 2026-07-23 (delivery status updated 2026-07-30)
**Designed for:** [Brontide Architecture 0.8](../../current/architecture/Brontide-Architecture-0.8.md) §16 and
§18.1, Complete Draft, not ratified
**Design sources:** [Composition and Components](../composition/Brontide-Design-Note-Composition-0.1.md),
[Channel](../channel/Brontide-Design-Note-Channel-0.1.md), and
[Draft Channel Contract 0.1](../channel/Brontide-Draft-Channel-Contract-0.1.md)
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

The work remains experimental until the architecture and Channel contract are ratified. Both stacks
now target the implemented Architecture 0.8 Complete Draft, but this plan does not ratify the
architecture, ratify Channel, or make Portable Binding part of Brontide Base.

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

### Delivery status (2026-07-30)

| Phase | State | Evidence |
| --- | --- | --- |
| PB0 — baseline and contract freeze | **Complete** | [`contract-matrix.md`](../../../binding/portable/contract-matrix.md), [`representation-choice.md`](../../../binding/portable/representation-choice.md), [`open-decisions.md`](../../../binding/portable/open-decisions.md) |
| PB1 — neutral manifests, plans, and vectors | **Complete** | [`schemas/`](../../../binding/portable/schemas/README.md) (8 files at PB1; 9 since PB7), [`vectors/`](../../../binding/portable/vectors/README.md) (63 vectors at PB1; 82 since the Catalog group Decision 5 added and PB7's Composition handoff, plus 6 golden encodings), [`build/verify-portable-binding.ps1`](../../../build/verify-portable-binding.ps1) |
| PB2 — Reference native implementation | **Complete** | [`Reference/src/Brontide.Reference.Experimental.Binding/Portable/`](../../../Reference/src/Brontide.Reference.Experimental.Binding/Portable/), [`Reference/tests/Brontide.Reference.Interchange.Tests/Portable/`](../../../Reference/tests/Brontide.Reference.Interchange.Tests/Portable/), [`build/verify-portable-binding.ps1`](../../../build/verify-portable-binding.ps1) |
| PB3 — Minimal native implementation | **Complete** | [`Minimal/src/Brontide.Minimal.Binding/Portable/`](../../../Minimal/src/Brontide.Minimal.Binding/Portable/), [`Minimal/tests/Brontide.Minimal.Interchange.Tests/Portable/`](../../../Minimal/tests/Brontide.Minimal.Interchange.Tests/Portable/), [`build/verify-portable-binding.ps1`](../../../build/verify-portable-binding.ps1) |
| PB4 — direct and process realization parity | **Complete** | [`Reference .../Portable/PortableRealizationParityTests.cs`](../../../Reference/tests/Brontide.Reference.Interchange.Tests/Portable/PortableRealizationParityTests.cs), [`Minimal .../Portable/PortableRealizationParityTests.fs`](../../../Minimal/tests/Brontide.Minimal.Interchange.Tests/Portable/PortableRealizationParityTests.fs), both `PortableChannelVectorCoverageTests`, both `PortableCrossProcessTests` |
| PB5 — cross-stack and independent-provider matrix | **Complete** | both stacks' `PortableCrossStackTests` and `PortableNeutralProviderTests`, [`binding/neutral-provider/`](../../../binding/neutral-provider/README.md), [`catalog-fixture-contract.json`](../../../binding/portable/vectors/catalog-fixture-contract.json) |
| PB6 — resource, lifecycle, and hardening completion | **Complete** | both stacks' `PortableDecoderPropertyTests`, `PortableProcessCategoryTests`, `PortableResourceSeamTests`, `PortableLifecycleSeamTests`, and the `a failure path leaks nothing` cases in `PortableRealizationParityTests` |
| PB7 — Composition handoff | **Complete** | [`schemas/composition-handoff.json`](../../../binding/portable/schemas/composition-handoff.json), [`vectors/composition-handoff.json`](../../../binding/portable/vectors/composition-handoff.json) (PB-72 - PB-82, plus three group properties), [`Reference .../Portable/PortableCompositionHandoff.cs`](../../../Reference/src/Brontide.Reference.Experimental.Binding/Portable/PortableCompositionHandoff.cs), [`Minimal .../Portable/PortableCompositionHandoff.fs`](../../../Minimal/src/Brontide.Minimal.Binding/Portable/PortableCompositionHandoff.fs), both `PortableCompositionHandoffTests` |
| PB8 — evidence, documentation, and review closure | **Partly complete** — steps 1-4 delivered; steps 5 and 6 outstanding | [`contract-matrix.md`](../../../binding/portable/contract-matrix.md) executed-evidence table, [`channel ledger`](../channel/architecture-0.8-channel-requirements-and-risk-ledger.md) §4, [`binding-measurements.json`](../../../interchange/binding-measurements.json) schema 2, [`public-boundaries.md`](../../current/policies/public-boundaries.md) portable seam, both stacks' `CHANGELOG.md` |

Only PB8 remains. The neutral contract exists and is gated, both stacks implement it natively in both
realizations, PB4 measured those two realizations against each other across the portable observation
set, PB5 paired the two stacks and added a provider that depends on neither, PB6 hardened both, and
PB7 added the seam by which composition machinery reaches the layer at all. Every C item has
executable evidence in each stack independently and, since PB5, paired evidence across them. PB2
discharged the three migration obligations PB1 recorded:
map keys are sorted on their complete encoding rather than by the Cooling codec's ordinal string
comparison, portable values are schema-guided and carry no kind discriminator, and local denial is
frameless, so the Cooling `denial` message kind did not enter the portable envelope set.

PB2 also closed three gaps in the PB1 fixture that made vectors unsatisfiable as written. The
fixture required the `cooling-profile` at strength `required` while offering no matching provision,
which contradicted PB-01; it declared no choice Shape, which PB-15 needs; and it declared no Fragment
outside the negotiated Operation, which PB-13 needs. All three were added to
[`vectors/fixture-contract.json`](../../../binding/portable/vectors/fixture-contract.json) as data;
no vector, schema, or golden encoding changed.

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

**Delivered.** The reusable layer lives under
`Reference/src/Brontide.Reference.Experimental.Binding/Portable/` and owns the deterministic CBOR
core, the portable references and Shape floor, the contract document and negotiation, the frozen and
inspectable Binding Plan, local authority under strong Kleene evaluation, referenced resources, the
lifecycle machine with declared limits and replay, the Channel envelopes, and the C9 observation
set. Cooling and Catalog are fixtures over that layer: each is a contract document plus a handler,
and the reusable layer contains no rule of either. `PortableCoreAdapter` is the Reference-owned
adapter between the stack's `ShapeValue` model and the neutral positions.

Evidence covers PB-01 through PB-60 and PB-62; PB-61 and PB-63 are the cross-stack matrix and stay
deferred to PB5, which `PortableVectorCoverageTests` asserts explicitly rather than leaving implied.
The golden encodings are read from the neutral artifacts and reproduced byte for byte rather than
restated. Parity is measured between the fixed direct-call realization and the negotiated process
realization over a local duplex seam, and the same contract additionally runs across a real process
boundary through the `--portable` verb of `Brontide.Reference.Interchange.Provider`.

Two limits of this phase are worth stating plainly. The retained line-delimited Cooling and Catalog
experiments are untouched and remain the cross-stack baseline, because retiring them needs the
Minimal side of the portable contract from PB3. And `copyCount` counts referenced-resource copies
only: an inline payload is the message rather than a copy of a resource, which is what makes the
PB-60 accounting of one copy across the process seam and none in the direct call exact.

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

**Delivered.** The reusable layer lives under `Minimal/src/Brontide.Minimal.Binding/Portable/` and
owns the same contract Reference implements, in Minimal's own terms rather than as a translation of
the Reference surface. Every refusal is an explicit `PortableResult` value carrying its portable
category, so a denial that never leaves the endpoint is a returned value rather than a raised
failure; the Shape body is an algebraic union, so "required for one kind and forbidden for the rest"
is structural; the lifecycle is an immutable record whose illegal transition leaves the previous
state intact; and the two resource flavors are separate union cases, so a handle has nowhere to put
octets and the forbidden implicit copy is unrepresentable in memory as well as refused on the wire.
`PortableModelAdapter` is the Minimal-owned adapter between the stack's `ShapeValue` model and the
neutral positions, and it refuses a decimal that does not fit rather than rounding one.

PB3 discharged the same three migration obligations on the Minimal side: map keys are sorted on their
complete encoding rather than by the retained JSON codec's ordinal comparison, portable values are
schema-guided and carry no kind discriminator, and local denial is frameless, so the retained
`denial` message kind did not enter the portable envelope set.

Evidence covers PB-01 through PB-60 and PB-62; PB-61 and PB-63 are the cross-stack matrix and stay
deferred to PB5, which `PortableVectorCoverageTests` asserts explicitly rather than leaving implied.
The golden encodings are read from the neutral artifacts and reproduced byte for byte. Parity is
measured between the fixed direct-call realization and the negotiated process realization over a
local duplex seam, and the same contract additionally runs across a real process boundary through the
`--portable` verb of `Brontide.Minimal.Interchange.Provider`.

Two limits of this phase are worth stating plainly. The retained line-delimited Cooling and Catalog
experiments in both stacks are untouched and remain the cross-stack baseline, because retiring them
needs PB5 to pair the two portable implementations. And the two stacks have still never spoken to
each other over the portable contract: each has passed the same neutral vectors alone, which is what
PB3 promised and no more.

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

**Delivered.** Each stack owns a parity matrix of thirteen scenarios, stated once as data and
executed unchanged in both realizations. A scenario is a record rather than a function, because a
function could send a different request to each realization and still report parity. The matrix
covers every portable result class a host can reach — success, shaped failed Outcome, local denial,
and protocol rejection — which is what PB2 and PB3 did not: they measured a success, a denial, and a
resource, leaving the refusals, whose decision point genuinely moves between the realizations,
unmeasured.

Measuring them found four divergences, the same four in each stack independently, and PB4 closed
them:

1. **A refusal the provider endpoint decided reported two different failure domains.** The direct
   realization reported `local-endpoint` and the process realization `remote-endpoint`, for the
   missing-required-Fragment, resource-integrity-mismatch, and resource-scope-refused vectors. The
   failure domain names which endpoint decided, relative to the observer, and the provider endpoint
   is the host's peer in both realizations; only the distance between them changes. A domain that
   tracked the distance would turn an observer-relative fact into a transport fact. The fixed
   direct-call realization now attributes an endpoint-decided refusal the way the process
   realization already did.
2. **An authority-bearing request body was rejected under two different categories.** The direct
   realization refused it as `invalid-authority-presentation`, because the endpoint's authority scan
   runs before the body is given a Shape; the process realization refused it as `invalid-payload`,
   because the host's schema-guided encoder rejected the carrying field first. The category was
   therefore decided by whichever rule happened to fire, and only the far endpoint enforced C3. The
   host now scans for authority-bearing content before it emits anything, so no Capability crosses a
   trust boundary even when a declared field could carry one, and both realizations name the
   authority rule.

The excluded fields are asserted to differ as their stated reasons permit rather than to agree
silently: a copied blob is one copy across the seam and none in a direct call, correlation identities
are per-run, and crossed boundaries name the realization. The parity profiles are additionally
reproduced against a provider in its own operating-system process, for every scenario rather than one
happy path, through the `--portable` verb of each stack's interchange provider.

Every Channel 0.1 vector now has executed evidence in each stack, accounted for by derivation rather
than by assertion: `PortableChannelVectorCoverageTests` reads
[`conformance/channel-0.1-vectors.json`](../../../conformance/channel-0.1-vectors.json) and the
neutral vectors' own `channelVectors` declarations, and a Channel vector counts as executed only when
some portable vector the neutral layer says preserves it is executed rather than deferred by that
stack. Removing a test, deferring a vector, or renaming a Channel vector fails the build. The two
PB5 deferrals are checked not to be the sole cover for any Channel vector.

The portable wire is length-delimited and bounded, and a retained line-delimited JSON message is
refused on its length prefix alone, so the legacy protocol cannot silently become the portable wire
contract.

One limit of this phase is worth stating plainly. Parity here is parity *within* a stack: each stack
compared its own two realizations. The two stacks have still never spoken to each other over the
portable contract, and neither has hosted an implementation-neutral provider. Both remain PB5.

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

**Delivered.** All six combinations pass. The scenarios are the PB4 parity matrix unchanged, and the
baseline each combination is measured against is the hosting stack talking to itself, so a difference
is attributable to the peer rather than to the scenario. The two direct-call rows were already
covered by PB4; the four process rows are new.

Pairing the implementations found two things that four phases of independent work had not.

**Catalog was never a shared contract.** PB1 declared only the Cooling fixture, so each stack
authored its own Catalog fixture and the two drifted: Reference kept the retained experiment's
`upsert-items`/`find-items` Operation names while Minimal shortened them, and the two disagreed on
`providerSpecific` for the addressing-only-handle dependency. Negotiation matches both exactly, so
the stacks could not establish a Catalog binding at all — and the drift was invisible while each ran
Catalog only against itself.
[`catalog-fixture-contract.json`](../../../binding/portable/vectors/catalog-fixture-contract.json) is
now the single declaration both are measured against; neither stack was simply right, and both moved.
A `PortableFixtureAlignmentTests` in each stack compares both fixtures against their neutral
declarations, so the next drift fails there rather than in the cross-stack matrix where the cause is
far less obvious.

**The neutral declaration was not encodable as published.** The fixture files carry documentation
alongside the contract — `additiveOver` on a Shape version, `role` on the encoding-edge Shapes — and
[`component-contract.json`](../../../binding/portable/schemas/component-contract.json) declares
exactly which fields a contract document has, rejecting unknown ones. A faithful transcode of the
file was therefore a malformed contract. Neither stack had noticed, because neither reads the file:
each hand-wrote its contract from it and dropped the annotations by eye. The fixtures now declare
their own `annotationFields`, so the distinction is data rather than a convention someone has to
know. This is the clearest single result of the phase: the first consumer to read the published form
found it insufficient, which is exactly what an implementation-neutral endpoint is for.

The independent provider lives at [`binding/neutral-provider/`](../../../binding/neutral-provider/README.md).
Three properties make it evidence rather than a third implementation, and each is checked: it imports
no Brontide assembly (the gate reads its resolved `.deps.json`, which names two libraries, neither
from a stack); it transcodes the contract from the checked-in declaration at run time rather than
restating it in source; and it reads and writes the wire with the base class library's CBOR codec
rather than either stack's. That two hand-written deterministic-CBOR cores and an off-the-shelf
decoder all agree is what makes "the representation is standard CBOR" a fact about the representation
rather than about the two codecs.

PB-61 and PB-63 are no longer deferred in either stack's coverage map, and no neutral vector is.

### PB6 — resource, lifecycle, and hardening completion

Add adversarial coverage for ownership transfer/borrowing, premature reuse, release/completion,
scope escape, integrity mismatch, unsupported fallback, and forbidden implicit copy. Record memory
domain and copy facts even when version 0.1 supports only a conservative resource subset.

Exercise establishment failure before activation, withdrawal, clean termination, peer loss,
timeout, interrupted frames, unknown lifecycle actions, duplicate terminal responses, and resource
exhaustion. Fuzz or property-test decoders within deterministic bounds. Prove that failure paths do
not leak a provider effect, authority, resource handle, exception, or false success.

**Exit:** C6 and C8 have positive and negative evidence in both stacks and across the process seam.

**Delivered.** C6 and C8 have positive and negative evidence in both stacks and across the process
seam, so the exit criterion is met. What follows separates what was built from what building it
found, because the findings are the more useful half.

#### Delivered

**Decoders are property-tested within deterministic bounds.** Every vector before this presented
input a person wrote. The new suites present input nobody wrote: arbitrary bytes, every single-byte
mutation and every truncation of a valid frame, nesting past the declared depth up to 10 000 levels,
and hostile length prefixes. The generators are seeded and the iteration counts fixed, so a failure
reproduces and the suite cannot go intermittently red. Minimal states the property more strongly than
Reference can: refusals there are returned values rather than raised failures, so its claim is not
"it raises only the right exception" but "it does not raise at all".

**Failure paths are proved to leak nothing.** Across every failing scenario in the matrix, in both
realizations: no provider effect, no value presented by a refusal, no runtime type or stack trace in
the diagnostics, no resource observed by a frameless denial, and no false success.

**The transport's process-category classification is total.**

**The C6 refusals are decided by an endpoint across a real seam.** PB-26 and PB-29 through PB-32
called the resource codec directly, which proves a static function refuses a malformed resource but
not that the endpoint does — and the endpoint is what a hostile peer actually reaches. Admission sits
behind decode, lifecycle, and operation resolution, so a refusal a unit test reaches in one call may
be unreachable, or reached in the wrong order, once those run first. Both stacks now present each
frame to a conforming endpoint over the seam: octets beside a handle, a release signal on the copied
flavor, a resource past the declared bound, a non-goal flavor named on a request, and a content hash
that does not verify. Each is refused with the category the vector states, and none reaches the
provider. The frames are built by hand rather than through a host, because a conforming host cannot
produce most of them.

**The C8 lifecycle refusals are decided by an endpoint across a real seam.** PB-09 and PB-36 through
PB-39 drive the lifecycle object directly, which proves the state machine rejects an illegal
transition but not that the endpoint applies it to an arriving frame — and an arriving frame is where
a peer's illegal sequence actually lands. A frame is decoded, its kind resolved, and its body read
before any state is consulted, so an endpoint can refuse for the wrong reason or in the wrong order.
Both stacks now send deliberate sequences: a request before any establishment, a second
establishment, a request after withdrawal, a declared kind a provider never receives, an
unrecognized kind, and a replayed request identity. None produces an Outcome and none reaches the
provider.

The case that most needed the seam is establishment failure. An endpoint that activated its provider
first and negotiated second would satisfy every other vector in the phase and still be wrong, so the
test asserts the absence of both the readiness signal and the acceptance, not merely the presence of
a refusal.

Writing these surfaced an ordering worth recording, though not a defect: **a malformed frame is
refused before its kind's direction is weighed.** An `outcome` carries a correlation identity by
declaration, so one built without it is refused as malformed rather than as a state violation. That
order is right — a frame that cannot be read has no direction to judge — but it is easy to assume the
reverse, and assuming the reverse would tell a peer to fix its sequencing when its encoder is what is
wrong. Both stacks assert the ordering explicitly.

**Two of the phase's conditions are unrepresentable in the 0.1 floor rather than merely refused.**
Premature reuse and a release-then-use sequence need a resource with a lifetime a peer can observe
ending, and the declared floor has neither: a copied immutable blob is transferred whole and has no
release signal, and an addressing-only handle carries no octets to release. There is no frame that
expresses "use this after its interval", so there is nothing for a vector to present. Unsupported
fallback is the same shape — no fallback policy is declared for 0.1, so a request cannot name one to
have it refused. Both stacks assert the declared flavor set rather than only recording this in prose,
so adding a borrowed or transferred flavor later fails the build and brings the reasoning back for
review. This follows how PB-29 records the non-goal flavors, rather than inventing a vector for a
condition the contract cannot express.

#### Findings

Each of the three appeared identically in both stacks, which is itself worth recording: independent
implementation catches divergence between the two, and cannot catch a blind spot they share. All
three were found by testing a property rather than a case.

1. **Resource observations claimed an acceptance and an integrity check that never happened.** They
   were built before dispatch from facts about the *flavor*, so an interaction that failed still
   reported `accepted: true`, and a blob whose content hash did not verify still reported
   `integrityVerified: true`. Both are false successes in the C9 observation set, and both are fields
   the C7 parity profile compares — so the two stacks agreed with each other while both were wrong,
   and no parity check could have found it. Acceptance and integrity are facts about a completed
   admission rather than about a flavor; a failed interaction now reports neither, while flavor,
   ownership, and copy count still describe what was presented.
2. **The transport let foreign exceptions escape.** The duplex named only the disposed-stream and I/O
   cases, so any other failure a stream can raise travelled out of the binding as a runtime type —
   the one thing C4 says never crosses the seam, and reachable from a peer's behaviour rather than
   from a defect in this code. Classification is now total: an allocation failure is
   `resource-exhausted`, a disposed stream or I/O failure is `transport-unavailable`, a cancellation
   past the declared bound is `timeout`, and anything else is `unknown` carrying why narrower
   attribution was impossible.
3. **Two declared process categories had no reachable path.** `resource-exhausted` and `unknown` were
   declared because the Channel taxonomy requires them, and PB-51 asserted the declared set was
   complete and unique — a statement about an enumeration, not about behaviour. Closing finding 2
   gave both a genuine path, and each now has behavioural evidence in both stacks.

A fourth observation is recorded rather than fixed: **`peer-unavailable` is unreachable in version
0.1 by design.** The binding layer never starts a peer; it is handed a duplex that is already
connected, and starting one is the host harness's concern above the binding. Both stacks assert the
unreachable set is exactly this one value, so a future change to it fails the build and brings the
reasoning back for review. Manufacturing a path so the enumeration looked evenly covered would have
been the dishonest alternative.

#### On method

Every defect in this phase was found by testing a property rather than a case, and every one of them
appeared identically in both stacks. That combination is worth stating plainly, because the
programme's central safeguard is independent implementation, and independent implementation is
exactly what cannot find these. Two stacks written from one contract by one reader share that
reader's assumptions. They diverge where the contract is ambiguous — which is what PB4 and PB5
found — and they agree wherever the contract is silent, which is where PB6's defects lived.

The sharpest illustration is the resource-observation defect. `accepted` and `integrityVerified` are
fields the C7 parity profile compares, so both stacks reported the same wrong values and every parity
check passed. No amount of cross-checking the two implementations against each other could have
surfaced it; only asking what the observation *claimed*, against what had actually happened, did.

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

**Delivered.** A controlled experimental composition in each stack establishes and releases portable
bindings, and the binding stays outside Base/Core/Model/Kernel: the seam lives in each stack's
existing experimental binding project, and the composition that drives it lives in the test estate.

#### Delivered

**The seam is declared before it is implemented**, in
[`schemas/composition-handoff.json`](../../../binding/portable/schemas/composition-handoff.json). It
owns the resolved-requirement record, the offered-provision record, the six-step preflight order, the
stage model, the interaction gate, and the replacement record. Both stacks are measured against that
one declaration by the eleven vectors in
[`vectors/composition-handoff.json`](../../../binding/portable/vectors/composition-handoff.json),
which is the PB5 lesson applied in advance: the two stacks agree where the contract speaks and drift
where it is silent, so the seam's shape is data before either stack has an opinion about it.

**The handoff consumes a resolution and produces one Binding Plan.** A resolved requirement carries a
binding scope, the required Component, an optional required provider, a cardinality, an exposure, and
the host endpoint; an offered provision carries the selected provider and its endpoint. Preflight
matches them, negotiation establishes the contract, and the plan is frozen with the scope preserved
alongside it. The scope survives the plan: a replacement re-binds the same scope with a new plan
identifier, because there is no renegotiation in place.

**Four of the eleven vectors are refusals of work this seam does not do.** A cardinality other than
`1..1` is refused rather than narrowed to a first member; a mediated exposure is refused rather than
erased into a direct binding; a provision naming a provider the resolution did not select is refused
rather than accepted as a compatible substitute; and a requirement, provision, and host declaration
that disagree about the Component are refused before a conversation exists. Discovery, acquisition,
selection policy, generations, mediation, and hot swap stay in the Component Management programme,
and a seam that approximated any of them would be making that programme's decisions here, invisibly.

**Every preflight refusal is frameless by construction rather than by assertion.** Preflight runs
before a conversation object exists, so there is nothing to emit through: no frame, no provider, no
effect. That is the same discipline as the frameless local denial C3 requires, with a different
reason — a contract refusal rather than an authority decision — which is why its result class is
`protocol-error` rather than `denial`.

**The ordinary-interaction gate is the release barrier.** An interaction attempted before Release is
refused as a state violation with a complete observation, emits no frame, and reaches no provider
effect — asserted against the provider's own effect counter, not only against the observation.
Establishment, readiness, withdrawal, and termination stay permitted, because they are lifecycle
traffic rather than ordinary interaction. Each stack's activation group holds the other half of the
contract: it opens no member's gate until every required member is ready, and a member that never
interconnected keeps the whole group closed.

**Withdrawal and termination produce a replacement record** naming the scope, the retired plan, the
Component, the provider that answered, the terminal state, and whether replacement is permitted. It
grants nothing: a replacement generation resolves, preflights, negotiates, and releases from the
beginning. Replacement is permitted after a clean end and refused after a failure, because a failed
binding leaves this seam no account of the provider's state.

The two implementations remain independent in their idiom. Reference refuses by raising the portable
fault its layer already uses and holds the stage as an enumeration beside the binding; Minimal
refuses by returning a `PortableResult` and makes the stage a union that *carries* the binding, so a
member outside the released case has no host to interact through rather than a flag saying it must
not.

**Both Decision 10 practices ran on this phase.** The group states three properties over all eleven
vectors — a plan exists exactly when Interconnection completed and always names the provider that
answered; the provider records an effect only while a member is released, counted at the provider
rather than read from the observation; and the resolution facts answer identically at every stage,
which is what makes "the scope outlives its plan" a checked claim. The phase-boundary completeness
review is recorded in [`completeness-reviews.md`](../../../binding/portable/completeness-reviews.md).
It found five questions the contract had not answered, four of them things both stacks already did
the same way — agreement that proved one reader had made one choice twice, not that the contract had
made it. Three are now declared in the schema; two are named as the Component Management programme's
to answer.

#### Findings

**The Binding Plan's provider fact reports who the host asked for, not who answered.** The plan's
`provider` and `selectedProvider` facts, and the `selectedProvider` field of every observation built
from them, are read from the *required* contract document. Version 0.1 negotiation compares the
Component by exact reference equality and never compares provider identity at all, so an endpoint may
offer the required Component while answering as a different provider, and the established plan will
report the provider the host wrote in its own declaration. Both stacks do this, identically.

Nothing before PB7 could have found it. The fixtures on both sides are built from one neutral
declaration, so the required and offered providers agree in every vector, in both cross-stack
directions, and against the implementation-neutral provider; the fact and the truth coincide
everywhere the programme looked. PB7 is the first phase in which *which provision was selected*
exists as a fact separate from the contract, and therefore the first phase that could ask whether the
plan reports it.

The provisional choice is to check it at the composition seam: the handoff witnesses the offered
contract during establishment, refuses a substitution as `unsupported-contract`, and abandons the
binding rather than releasing it. That is defensible on its own terms — which provision was selected
is a composition fact rather than a contract fact — but it leaves the plan fact itself unchanged, and
a host using the binding layer without this seam still has no way to learn who answered. Whether
negotiation should compare provider identity, and whether the plan's provider fact should name the
answering endpoint, is **Decision 11** for the contract maintainers. It was ruled on 2026-07-30:
negotiation compares the provider and refuses a mismatch, and the plan reads the fact from the
offered document. The composition-seam check is retained for the case negotiation cannot see — a
required contract naming a provider the resolution did not select.

This is the third phase in a row whose finding was invisible to comparing the two stacks against each
other, and it is the same shape as PB6's first defect: an observation field that describes what was
asked for while claiming to describe what happened. Decision 10 had already ruled on what supplements
independent implementation — a property per capability, and a contract-completeness review at each
phase boundary — and this finding is a data point for that ruling rather than against it: it was
found by asking what a *new* consumer of the layer needs to know, which is the completeness question
in its most useful form.

**A declared refusal with no reachable path was caught before it was written.** The first draft
stated the release rule twice — a stage check and a separate readiness check — and the readiness
branch was unreachable, because version 0.1 completes Interconnection only with a readiness signal.
Following PB6's discipline, the rule is now stated once: a member that has no readiness signal cannot
be released, which the Local Initialisation case reaches. The two stacks state it the same way.

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

**Partly delivered.** Steps 1 through 4 are complete; steps 5 and 6 are not, and neither can be
completed by the implementer.

#### Delivered

**Step 1 — implementation documentation.** Both stack READMEs name the portable layer and where it
sits in the dependency rule; both `experimental-and-sideline-projects.md` inventories carry the
per-phase state; and both `CHANGELOG.md` files record the added experimental surface under their own
heading, explicitly as experimental evidence rather than a component-version change. The public
boundary document gains a portable-seam section stating its declared bounds, timeout and failure
classification, cleanup ownership, replay and denial-of-service assumptions, and what does not cross.
The two retained JSON-lines rows and their pinned anchors are untouched, so the existing
conformance-matrix evidence pins remain valid.

**Step 2 — Channel ledger and contract matrix.** The contract matrix gains an executed-evidence
table: per capability, which realizations have run it — direct, process, cross-process, cross-stack —
and the suite that carries it. The Channel ledger records CH-R11 as `realisation-executed` rather
than awaiting stack harnesses, adds a section on what the realisation evidenced for CH-R2, CH-R6,
CH-R8, CH-K2, CH-K3, and CH-K4, and marks two of its recorded forward scenarios delivered and one
partly delivered. Neither document upgrades ratification or architecture-target language, and the
Decision 10 caveat is carried into the ledger rather than left in the binding programme.

**Step 3 — re-measurement.** [`interchange/binding-measurements.json`](../../../interchange/binding-measurements.json)
moves to schema 2. Every source file now declares its layer — retained experiment or portable — and
each stack records per-layer totals; the portable layer measures 7,337 Reference lines against 6,392
Minimal lines, two independent implementations of one contract within about 13% of each other. The
file also records the representation, framing, allocation, copy-accounting, and payload-bound facts
for both realizations, each stating whether it is declared by the contract, asserted by a named
vector in both stacks, or measured from the artifacts. The gate recomputes every count and every
layer total, fails on a portable source file that is not measured at all, and rejects a recorded fact
that names no provenance — checked by deliberately breaking each rule and confirming the failure.
Runtime cost stays method-recorded rather than threshold-gated, for the reason §3 gives: optimising
the hot path before contract and observation parity are demonstrated is an explicit non-goal, so what
is gated is structural cost.

**Step 4 — gates.** `build/verify-portable-binding.ps1` and the complete repository gate
`build/verify-interchange.ps1` both pass.

#### Outstanding, and why the implementer cannot close them

**Step 5 — fresh independent reviews.** An automated attestation counts as independent only when the
reviewer has an identity distinct from every implementation actor, starts in a fresh isolated
context, and has no access to the implementation session's private reasoning. The session that
implemented PB7 and this documentation is an implementation actor for exactly this evidence, so it
cannot review it. The existing control plane under `conformance/reviews/` is pinned to the completed
implementation-correction programme; using it would require a new request rather than an edit to that
closed record, which is an owner decision about what is being reviewed and by whom.

**Step 6 — question closure.** Decision 11 was ruled on 2026-07-30 and is recorded in
[`open-decisions.md`](../../../binding/portable/open-decisions.md): negotiation compares provider
identity, and the plan reports the provider that answered. The Channel naming question remains open
and still awaits an *owner* ruling rather than an implementer's. Moving it to `Resolved questions`
without a ruling would convert a provisional choice into a decision by writing it down, which is the
one thing the open-decisions file exists to prevent. The eight decisions raised by PB4 through PB6
were ruled on separately and are already recorded there.

The Channel naming question remains open work for PB8; nothing in steps 1 through 4 depends on it.

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

One question is open. The eight raised by PB4, PB5, and PB6 were all recorded on 2026-07-28, and
Decision 11, raised by PB7, was recorded on 2026-07-30; all nine have moved to
[Resolved questions](#resolved-questions). Each remains written up in full — what was observed, what
was running and why, the alternatives with their trade-offs, and what the ruling changed — in
[`binding/portable/open-decisions.md`](../../../binding/portable/open-decisions.md).

| Owner | Question | Blocking point |
| --- | --- | --- |
| Brontide architecture maintainers | Ratify the provisional Channel Shape/category names or publish an explicitly migrated revision? | Blocks a stable public Portable Binding version; experimental PB0-PB6 may proceed against a versioned draft. |

## Resolved questions

The eight below were raised by PB4, PB5, and PB6, ran on a provisional implementer choice while each
phase proceeded, and were recorded on **2026-07-28**. Four confirm the provisional choice unchanged;
four create follow-on work, marked as such. Decision 11, raised by PB7, follows them. Full option
sets and rationale stay in
[`binding/portable/open-decisions.md`](../../../binding/portable/open-decisions.md).

- **2026-07-30 — Decision 11, the plan's provider fact:** **negotiation compares provider identity**
  and refuses a mismatch as `unsupported-contract`, and the plan's `provider`/`selectedProvider`
  facts and the C9 observation are read from the **offered** document. A required document naming a
  provider is binding rather than expectational; version 0.1 deliberately defines no way to say "any
  provider of this Component", which stays an additive change if it is ever needed. PB7's
  composition-seam check is retained for the case negotiation cannot see — a required contract naming
  a provider the resolution did not select — and is reachable only when the requirement names no
  provider. Creates follow-on work, delivered with the ruling: PB-83 pins the refusal, and both
  stacks re-derive the plan fact. Because the two providers are equal whenever a plan exists, no
  black-box vector can distinguish the read side; that limit is recorded in the decision.

- **2026-07-28 — Decision 3, failure domain of an endpoint-decided refusal:** `failureDomain` names
  **which endpoint decided**, recorded relative to the observer as CH-24 requires, not how far away it
  was. Distance stays visible in `crossedBoundaries`, which the parity profile excludes. The field
  remains in `parityProfile.comparedFields` and the direct realization's re-attribution to
  `remote-endpoint` stands. No change to what runs.
- **2026-07-28 — Decision 4, where the no-capability-transfer scan is enforced:** **both the host and
  the endpoint** scan. The host refuses before emitting, so no Capability reaches the wire even in a
  declared field; the endpoint scan is retained because an endpoint must never depend on its peer
  having scanned. The duplicate walk of the request body is the accepted cost of C3 holding
  absolutely. No change to what runs.
- **2026-07-28 — Decision 5, the Catalog fixture's canonical form and its vectors:** the declaration
  in [`catalog-fixture-contract.json`](../../../binding/portable/vectors/catalog-fixture-contract.json)
  is **confirmed as it stands** — Operation names following the retained experiment, and the
  addressing-only-handle dependency `providerSpecific: false`. Neither stack changes. **Creates work,
  now done:** [`catalog-vectors.json`](../../../binding/portable/vectors/catalog-vectors.json)
  declares PB-64 through PB-71 plus three group properties, and both stacks execute all of them, so
  the neutral layer states what Catalog must *do* and not only what it is. Authoring them fixed where
  a resource refusal splits between `unsupported-contract` (flavor level) and `invalid-payload`
  (instance level), and found the Catalog *handlers* drifted apart across all three implementations
  on partial-match and count semantics. PB-70 and PB-71 settle both, deriving the rules from the
  declared Shapes rather than from whichever implementation was read first, and Minimal was brought
  to them.
- **2026-07-28 — Decision 6, separating fixture annotation from contract data:** **the schema declares
  the mechanism.** `component-contract.json` now carries a `contractDocument.annotation` block naming
  how a document declares its own documentation fields and which four root names are the artifact
  envelope; the per-fixture `annotationFields` list stays as the expression of the rule. An
  implementer working only from `schemas/` no longer has to discover it from a fixture — which is how
  PB5 found it. Enforced by the portable-binding gate, which drops the declared annotation and root
  envelope from each fixture and requires what remains to be exactly the contract document.
- **2026-07-28 — Decision 7, allocation failure at the transport boundary:** an allocation failure maps
  to **`resource-exhausted`** and classification stays total. The ordinary objection to catching it
  assumes the alternative is a healthier process; here the alternative is a foreign runtime type in
  the caller's hands, which C4 forbids. No change to what runs.
- **2026-07-28 — Decision 8, `peer-unavailable` unreachable in 0.1:** **by design.** The binding layer
  is handed an already-connected duplex and never starts a peer, so the condition has no observation
  point in it; owning peer startup would widen the layer to process lifetime and launch policy for one
  observation. Narrowing the 0.1 profile was rejected because the taxonomy is reproduced exactly from
  `conformance/channel-0.1-vectors.json`. Both stacks keep asserting the unreachable set is exactly
  this one value. No change to what runs.
- **2026-07-28 — Decision 9, three C6 conditions unrepresentable at the resource floor:** **accepted
  for 0.1**, and Decision 2 is not reopened — premature reuse, release-then-use, and unsupported
  fallback stay unrepresentable rather than being given manufactured paths, with the declared flavor
  set asserted so a future widening fails the build. **Creates work, now done:** C6's text in
  [`contract-matrix.md`](../../../binding/portable/contract-matrix.md) states that borrow interval,
  lifetime, release signal, and fallback policy are declared-but-unexercised at this floor, so the
  capability stops reading broader than its evidence. A lifetime-bearing flavor is a 0.2 conversation.
- **2026-07-28 — Decision 10, what supplements independent implementation:** **a property per
  capability, and a contract-completeness review at each phase boundary.** Two implementations written
  from one contract by one reader diverge where it is ambiguous and agree where it is silent, so
  independence detects ambiguity and is structurally blind to silence — which is why all three PB6
  defects appeared identically in both stacks, in fields the parity profile compares. Every capability
  now states at least one property holding over all its vectors, and each phase boundary gets a review
  pass asking what the contract does *not* say. Neutral-implementation-first was considered and not
  adopted: strongest, most expensive, and the completeness review should reach most of it far cheaper.
  **Creates work, now done:** adopted as a standing ground rule in
  [`AGENTS.md`](../../../AGENTS.md).

- **2026-07-25 — Wire representation:** deterministic CBOR core. RFC 8949 §4.2.1 core-deterministic
  encoding, definite lengths, shortest-form arguments, bytewise-on-encoded-key map ordering, major
  types 0-5 plus simple values 20/21/22 and the single allowlisted tag 4 for `Decimal`; 4-byte
  big-endian length-delimited framing bounded at 65 536 bytes. Values are schema-guided and carry no
  kind discriminator. Retained JSON-lines is diagnostic and legacy only. Decision recorded
  2026-07-24 in [`open-decisions.md`](../../../binding/portable/open-decisions.md); exact rules pinned
  by PB1 in [`payload-representation.json`](../../../binding/portable/schemas/payload-representation.json).
- **2026-07-25 — Referenced-shaped-resource v0.1 floor:** copied immutable blob, with integrity by
  SHA-256 content hash, no borrow interval and no release signal, and no fallback. Catalog's
  addressing-only handle is retained as a second declared flavor. Borrowed read-only regions and
  transferred ownership are 0.1 non-goals that fail negotiation closed. Decision recorded 2026-07-24;
  schema pinned by PB1 in the same file.

- **2026-08-10 — Architecture target closure:** both stacks now target Architecture 0.8 after the
  separate D1-D6 closure. Portable Binding remains outside Base and does not imply ratification.
- **2026-07-23 — Architecture scope:** Portable Binding remains outside Brontide Base; its work does
  not itself choose either stack's architecture target.
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
