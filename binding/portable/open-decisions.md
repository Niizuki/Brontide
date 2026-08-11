# Portable Binding — open owner decisions

**Status:** Decisions 1 through 11 and Decision 13 are **recorded**; **Decisions 12 and 14 through 16
are open**, raised 2026-08-01 by CBI20 and 2026-08-02 by CBI23, CBI24, CBI26, and CBI27. The four
open decisions block nothing. Portable Binding 0.1 still refuses every activation of a CM3 group
that declares a bounded lifecycle protocol; Decision 13 records that this is a version limitation
and schedules the missing capability for a versioned 0.2 contract after PB8 review. Decisions 1 and
2 (the PB0 exit blockers) were
recorded 2026-07-24. Decisions 3 through 10 were raised by evidence in PB4, PB5, or PB6, ran on a
provisional implementer choice while each phase proceeded, and were recorded 2026-07-28. Four of
those eight confirm the provisional choice unchanged; four confirm it and create follow-on work,
tracked under [Work the rulings create](#work-the-rulings-create).
**Decision 11 was raised later, by PB7, and was recorded on 2026-07-30**: negotiation now compares
provider identity and the Binding Plan reports the provider that answered. **Decisions 12 and 13 were raised by
CBI20 and CBI21 on 2026-08-01, and Decisions 14, 15, and 16 by CBI23, CBI24, CBI26, and CBI27 on
2026-08-02. Decision 13 was recorded on 2026-08-11; the other four await rulings.** Non-pinned.

**How to read this file.** Every decision below is written to be answerable without any other
context: it states what was observed, what was running when the question was raised and why, what the
alternatives were, and what the ruling changed. A provisional choice was *not* a decision — it was a
placeholder the owner could confirm or overturn, and overturning one was cheap because each was
isolated behind a named seam. Each decision keeps its full option set after the ruling, so a later
reader can see what was rejected and on what grounds.

## Index

| # | Decision | Raised by | State |
| --- | --- | --- | --- |
| 1 | Portable wire representation | PB0 | **Recorded** 2026-07-24 — deterministic CBOR core |
| 2 | Referenced-shaped-resource v0.1 floor | PB0 | **Recorded** 2026-07-24 — copied immutable blob |
| 3 | Failure domain of a refusal decided by the provider endpoint | PB4 | **Recorded** 2026-07-28 — the domain names which endpoint decided |
| 4 | Where the no-capability-transfer scan is enforced | PB4 | **Recorded** 2026-07-28 — both host and endpoint |
| 5 | Catalog fixture canonical form, and whether it needs vectors | PB5 | **Recorded** 2026-07-28 — confirmed as declared; Catalog vectors owed |
| 6 | How fixture annotation is separated from contract data | PB5 | **Recorded** 2026-07-28 — the schema declares the mechanism |
| 7 | Catching an allocation failure at the transport boundary | PB6 | **Recorded** 2026-07-28 — `resource-exhausted` |
| 8 | `peer-unavailable` is unreachable in version 0.1 | PB6 | **Recorded** 2026-07-28 — by design for 0.1 |
| 9 | The resource floor leaves three C6 conditions unrepresentable | PB6 | **Recorded** 2026-07-28 — accepted; C6's text narrowed |
| 10 | What supplements independent implementation as a safeguard | PB6 | **Recorded** 2026-07-28 — property tests and completeness review |
| 11 | The plan's provider fact names who was asked, not who answered | PB7 | **Recorded** 2026-07-30 — negotiation compares the provider; the plan reports who answered |
| 12 | A receiving-domain Actor freed by a dropped member, taken by a different party | CBI20 | **Open** — raised 2026-08-01; Option A running |
| 13 | Relational Initialisation is declared out of scope, and CM4 needs it | CBI21 | **Recorded** 2026-08-11 — Option A retained for 0.1; Option B selected for 0.2 |
| 14 | Nothing records that a restart scope has children | CBI23, CBI24 | **Open** - raised 2026-08-02; Option A running |
| 15 | CM2 can declare a Mediation that owns authority; CM5 cannot represent one | CBI26 | **Open** - raised 2026-08-02; Option A running |
| 16 | A CM binding scope holds many bindings; a portable one names a single binding | CBI27 | **Open** - raised 2026-08-02; Option A running |

---

## Decision 1 — portable wire representation

**Owner:** Portable Binding contract maintainers.
**Blocks:** PB1 wire fixtures (C1/C4), PB4 direct-vs-process parity.

**Context.** The existing wire is line-delimited UTF-8 JSON (`inline-tagged-json`). Plan §5 PB4
requires the portable process wire to be **length-delimited and bounded**, and relegates the retained
JSON-lines protocol to a diagnostic/legacy path. The open question is which restricted, schema-guided
CBOR subset, scalar tags, canonicalization rules, identifier widths, and maximum bounds define the
first process realization.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Deterministic CBOR core** | RFC 8949 §4.2.1 core-deterministic CBOR: major types 0–5 only, definite-length items, sorted map keys, smallest-integer encoding; scalars → native CBOR (uint/nint/bytes/text/bool/null) with a tiny tag allowlist for `Decimal`; identifiers as canonical text with optional binding-scoped small-uint compact ids post-negotiation; bounds reuse Catalog's 65 536-byte frame + depth 32 | Meets PB4 (length-delimited, bounded); deterministic → stable cross-stack golden vectors; compact; both stacks already have a `ShapeValueCodec`/`PortableBinding` codec seam to target | New encoder/decoder work in each stack; must pin the canonicalization + tag allowlist precisely |
| **B. Retain JSON-lines as the portable wire** | Keep `inline-tagged-json` as the normative wire for 0.1 | No new codec; reuses everything | Contradicts PB4 (wants bounded length-delimited framing; JSON-lines is diagnostic-only); no deterministic byte form for golden vectors; not recommended |
| **C. Length-delimited JSON** | JSON body with a 4-byte length prefix + the Catalog bounds | Bounded framing without a CBOR codec; stays human-readable | JSON canonicalization is fiddly (number/whitespace/key-order); larger; still not the CBOR PB4 anticipates |

**Recommendation: Option A (deterministic CBOR core)**, with Catalog's byte/depth bounds as the
frame limits and canonical-text identifiers plus optional post-negotiation compact ids. Keep
JSON-lines as the diagnostic/legacy path. Rationale: it is the only option that satisfies PB4's
bounded-framing requirement *and* gives a deterministic byte form the cross-stack golden vectors
need. Exact scalar tags and canonicalization should be pinned against the two stacks' existing codecs
(`ShapeValueCodec.cs`, `PortableBinding.fs`) when PB1 authors the first `schemas/`.

**Decision (recorded):** **Option A — deterministic CBOR core.** Recorded 2026-07-24 by user:JakHoh.
JSON-lines retained as diagnostic/legacy only. PB1 pins the exact scalar tags, canonicalization, and
bounds against the stack codecs (`ShapeValueCodec.cs`, `PortableBinding.fs`) when authoring `schemas/`.

---

## Decision 2 — referenced-shaped-resource v0.1 floor

**Owner:** Portable Binding contract maintainers with both stack owners.
**Blocks:** PB1 resource schema (C6), PB6 hardening.

**Context.** Cooling declares `no-referenced-resources`. Catalog has the only existing referenced
resource: a provider-scoped **addressing-only** handle (`{provider,id}`, accepts
`catalog-sandbox/shared`, else `resource-refused`; conveys addressing, never authority). C6 wants a
declared form covering representation, scope, access, ownership/borrow interval, lifetime,
release/completion signal, integrity rule, and fallback policy. The question is the smallest viable
v0.1 floor.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Copied immutable blob** | Provider receives an immutable by-value copy; integrity via content hash; no borrow interval, no release signal | Smallest form that actually exercises representation + integrity + ownership; no lifetime/borrow machinery; deterministic | Does not exercise borrow/transfer semantics; copies bytes across the boundary |
| **B. Borrowed read-only region** | Host keeps ownership; provider gets a read-only borrow with a declared lifetime + release signal | Exercises scope/lifetime/release; no copy | Needs borrow-interval + release semantics and peer-loss handling now |
| **C. Transferred ownership** | Ownership moves to the provider; requires release/completion, integrity, and reclaim-on-failure | Most expressive | Heaviest; most failure paths to harden in 0.1 |
| **D. Addressing-only handle** | Formalise Catalog's existing accept/refuse handle; no bytes cross | Cheapest to reach from existing code | Doesn't exercise copy/borrow/ownership — barely more than what exists |

**Recommendation: Option A (copied immutable blob)** as the v0.1 floor, keeping Catalog's
addressing-only handle as a second, already-proven "reference" flavor. Rationale: A is the smallest
form that gives C6 real ownership + integrity + representation evidence without borrow/lifetime
complexity, while D alone would not advance beyond current evidence. Borrow (B) and transfer (C) are
deferred to a later version and named as non-goals for 0.1.

**Decision (recorded):** **Option A — copied immutable blob.** Recorded 2026-07-24 by user:JakHoh.
Integrity via content hash; Catalog's addressing-only handle retained as a second proven "reference"
flavor; borrow and transferred-ownership are non-goals for 0.1.

---

## After both decisions

Once recorded, PB1 can author the first `schemas/` and `vectors/` and PB0's exit criteria are met
(every C-item + Channel vector has an owner, evidence path, and expected observation; no unresolved
encoding question remains implicit).

---

# Decisions raised by PB4, PB5, and PB6

The eight below were recorded on 2026-07-28. Each arose because implementing or testing a phase
surfaced a question the contract did not answer. In every case the implementer chose provisionally so
the phase could finish, and the choice was named here so it could be confirmed or overturned
deliberately rather than inherited by accident. Every one was ruled on deliberately; none was
inherited.

## Work the rulings create

Four rulings confirm what was running and change nothing: Decisions 3, 4, 7, and 8. The other four
create work, listed here so a reader can tell a recorded decision from a discharged one:

| Ruling | Work it creates | State |
| --- | --- | --- |
| 5 | Author a neutral Catalog vector group, and give it executed evidence in both stacks | Done |
| 6 | Declare the annotation mechanism in `schemas/component-contract.json` | Done |
| 9 | Narrow C6's text so borrow, lifetime, release, and fallback read as declared-but-unexercised | Done |
| 10 | Adopt property-per-capability and phase-boundary completeness review as standing practice | Done |

---

## Decision 3 — the failure domain of a refusal decided by the provider endpoint

**Owner:** Portable Binding contract maintainers.
**Raised by:** PB4 direct-versus-process parity.
**Blocks:** nothing today; a ruling either confirms current behaviour or changes an observation field
that C7's parity profile compares and C9 declares normative.

**Context.** A request can be refused by the provider endpoint rather than by the host — a missing
required Fragment, a resource whose content hash does not verify, a handle outside the accepted list.
When PB4 first compared the two realizations of the same vector, they disagreed: the fixed
direct-call realization reported `local-endpoint` and the negotiated process realization reported
`remote-endpoint`. `failureDomain` is a **compared** field in the parity profile
(`schemas/binding-observation.json`), so this was a parity failure rather than a cosmetic difference.

The underlying question is what `failureDomain` *names*. If it names distance, the two realizations
are both right and the profile should not compare it. If it names which endpoint decided, relative to
the observer, then the direct realization was wrong: the provider endpoint is the host's peer in both
realizations, and only the distance between them changes.

CH-24 says the domain is recorded relative to the observer and that no domain value claims global
topology, which points at the second reading but does not settle it, because "remote" reads as a
statement about distance in ordinary use.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Domain names which endpoint decided** *(running)* | The direct realization re-attributes an endpoint-decided refusal to `remote-endpoint`, matching what the process realization already reported | One vector reports one domain; keeps `failureDomain` in the parity profile as PB1 declared it; consistent with CH-24's observer-relative wording | "Remote" describes a peer in the same process, which reads oddly until the rule is known |
| **B. Domain names distance** | Leave both realizations as they were and remove `failureDomain` from the compared set | No re-attribution; each value literally true about its transport | Weakens the parity profile by one normative field; makes an observation field a transport fact rather than a semantic one; C9 would need to say so |
| **C. Split the concept** | Report both a deciding-endpoint field and a distance field | Both facts available, neither overloaded | Adds a normative observation field in a version trying to stay small; every stack and the neutral provider must carry it |

**Recommendation: Option A**, which is what is running. The domain is most useful as an attribution
of *who decided*, because that is what a host acts on; distance is already visible in
`crossedBoundaries`, which the profile deliberately excludes. If the owner prefers B, the change is
small and local: drop `failureDomain` from `parityProfile.comparedFields` and revert the
re-attribution in each stack's direct conversation.

**Decision (recorded):** **Option A — the domain names which endpoint decided.** Recorded 2026-07-28
by user:JakHoh. `failureDomain` is an attribution of who decided, recorded relative to the observer as
CH-24 requires, and not a statement about distance; distance stays visible in `crossedBoundaries`,
which the parity profile deliberately excludes. `failureDomain` therefore remains in
`parityProfile.comparedFields`, and the direct realization's re-attribution of an endpoint-decided
refusal to `remote-endpoint` stands. No change to what is running.

---

## Decision 4 — where the no-capability-transfer scan is enforced

**Owner:** Portable Binding contract maintainers with both stack owners.
**Raised by:** PB4 direct-versus-process parity.
**Blocks:** nothing today; a ruling confirms or narrows where C3 is enforced.

**Context.** C3 says no Capability crosses a trust boundary. Before PB4 the scan for
authority-bearing content ran only at the **provider endpoint**. That produced a parity failure: in
the direct realization the endpoint's authority scan ran first and the refusal was
`invalid-authority-presentation`; in the process realization the host's schema-guided encoder
rejected the field that happened to carry the Capability first, and the refusal was
`invalid-payload`. The category was decided by whichever rule fired first rather than by what was
wrong.

The deeper issue is not the category. With the scan only at the far endpoint, a Capability in a
*declared* field would have been encoded and written to the wire before anything objected — the host
was willing to serialize one. Nothing in the vectors caught this because the fixture's Capability
lands in an undeclared field, which the encoder rejects for an unrelated reason.

The scan now also runs in the host before anything is emitted, in both stacks. The endpoint's scan is
retained, because an endpoint must still refuse a Capability arriving from an untrusted host.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Both host and endpoint scan** *(running)* | Host refuses before emitting; endpoint refuses on arrival | No Capability reaches the wire even in a declared field; the endpoint still defends against an untrusted host; both realizations name the authority rule | The scan runs twice on the happy path, over the whole request body |
| **B. Endpoint only** | Revert to the state before PB4 | One scan; the endpoint is the trust boundary, so arguably the only place that matters | A conforming host will serialize a Capability across the boundary before it is refused, which is what C3 forbids; the two realizations report different categories |
| **C. Host only** | Drop the endpoint scan | One scan, at the side that owns the outbound decision | An endpoint must never trust the peer to have scanned; removing it would make C3 depend on the peer's good behaviour |

**Recommendation: Option A**, which is what is running. The duplicate cost is a full walk of the
request body on every request, which is measurable if it matters; if the owner wants it reduced, the
honest reduction is to scan control positions eagerly and payload positions only when the plan
declares a trust boundary, not to remove either side.

**Decision (recorded):** **Option A — both the host and the endpoint scan.** Recorded 2026-07-28 by
user:JakHoh. The host refuses before emitting, so no Capability reaches the wire even in a declared
field; the endpoint's scan is retained because an endpoint must never depend on its peer having
scanned. The duplicate walk of the request body is accepted as the cost of C3 holding absolutely
rather than by the peer's good behaviour. No change to what is running.

---

## Decision 5 — the Catalog fixture's canonical form, and whether it needs vectors

**Owner:** Portable Binding contract maintainers.
**Raised by:** PB5 cross-stack matrix.
**Blocks:** nothing today; a ruling confirms the declaration both stacks are now measured against.

**Context.** PB1 declared only the Cooling fixture. Each stack therefore authored its own Catalog
fixture, and the two drifted without anything noticing, because each stack only ever ran Catalog
against itself. They disagreed in two ways, and negotiation matches both exactly, so the two stacks
could not establish a Catalog binding at all:

- **Operation names.** Reference used `interchange.tests.catalog.upsert-items` and `find-items`,
  preserving the retained experiment's names. Minimal used `upsert` and `find`.
- **`providerSpecific` on the addressing-only-handle dependency.** Reference declared `true`, Minimal
  `false`.

`vectors/catalog-fixture-contract.json` now declares the contract once, and both stacks were changed
to meet it: Minimal took the Operation names, Reference took the flag. The choices were:

- **Operation names** follow the retained experiment (`interchange/catalog/manifest-v1.json`), on the
  reasoning that the fixture restates that experiment and should not silently rename it.
- **`providerSpecific: false`** follows how the Cooling fixture declares `copied-immutable-blob`, on
  the reasoning that the *flavor* is a general capability while the *handle scope* is carried by
  `acceptedResourceHandles`. The opposite reading — that a provider-scoped handle makes its flavor
  provider-specific — is defensible and was rejected only for consistency.

A second question follows. The neutral layer now carries a Catalog **contract** but no Catalog
**vectors**; the Catalog cases live in each stack's parity matrix. Cooling has both.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Confirm as declared, no Catalog vectors** *(running)* | Keep the contract as the single declaration; leave the cases in the stack matrices | Smallest change; the cross-stack matrix already exercises them in both directions | The neutral layer states what Catalog *is* but not what it must *do*; a future third implementation gets a contract with no expected observations |
| **B. Confirm, and author Catalog vectors** | Add a neutral vector group for the multi-Operation, nested-data, and handle cases | The neutral layer becomes sufficient for an implementer working only from it — which PB5 showed is the property that matters | Vector authoring work; the ids must not collide with PB-01 through PB-63 |
| **C. Change a choice** | Adopt the short Operation names, or `providerSpecific: true`, or both | If either matches the owner's intent, better to correct it now than to pin the wrong form | Both stacks and the neutral provider change again |

**Recommendation: Option B.** Confirm the declaration as it stands, then author the Catalog vectors.
PB5's central lesson was that a contract nobody reads from the outside is a contract nobody has
checked; Catalog currently has that shape.

**Decision (recorded):** **Option B — confirm the declaration, and author the Catalog vectors.**
Recorded 2026-07-28 by user:JakHoh. The canonical form stands exactly as
`vectors/catalog-fixture-contract.json` declares it: the Operation names follow the retained
experiment (`interchange.tests.catalog.upsert-items` and `find-items`), and the addressing-only-handle
dependency is `providerSpecific: false`, on the reading that the flavor is a general capability while
the handle scope is carried by `acceptedResourceHandles`. Neither stack changes.

The second half of the ruling was work, and it is done. `vectors/catalog-vectors.json` declares
PB-64 through PB-71 over the Catalog fixture, and both stacks execute all eight plus the group's three
properties. The neutral layer now states what Catalog must *do* and not only what it is, so a third
implementation working from `binding/portable/` alone receives expected observations rather than a
bare contract.

Authoring them settled two boundaries no earlier vector had, and turned up one finding:

- **Where a resource refusal splits between two categories** (PB-69). A refusal at the *flavor* level
  is `unsupported-contract`, because a flavor is a term of the frozen contract — PB-29 reaches that
  during negotiation and PB-69 after establishment, and both answer the same way. A refusal at the
  *instance* level is `invalid-payload`: PB-28's out-of-scope handle, PB-26's failed content hash.
  The vector was first written asserting `invalid-payload` for both, and both stacks disagreed with
  it identically, each already carrying a deliberate `resource-flavor-unnegotiated` local code. The
  vector was wrong and was corrected to the implementations rather than the reverse.
- **That an accepted handle is not an admission decision** (PB-68). PB-27 states that a handle
  conveys addressing only; PB-68 states the consequence that makes it falsifiable — the same accepted
  handle accompanies both a success and a shaped failure.
- **A finding, now closed: the Catalog *handlers* had drifted**, in the same way PB5 found the
  contracts had. Minimal failed a lookup only when every requested identifier was missing and
  answered with those it found, while Reference and the implementation-neutral provider both failed
  when any was missing; Minimal answered `stored` with the session running total while the other two
  answered this request's item count. Nothing noticed, because until PB-64 through PB-69 no vector
  exercised Catalog across implementations.

  PB-70 and PB-71 settle both, and Minimal was brought to them. The rules are **derived from the
  declared Shapes, not adopted from whichever implementation was read first** — that distinction is
  the point, because making one stack conform to another is the collapse the two-stack design exists
  to prevent. `find-result` declares a sequence of items and no companion field for identifiers that
  missed, so a partial success would drop which ones were absent with no way for the caller to
  recover it, while the contract already declares a detail Shape for exactly that report; and
  `stored` acknowledges an `upsert-items` command carrying N items, so a session running total
  answers a question the command did not ask and makes an otherwise request-determined result depend
  on binding history. That the neutral provider — written cold from the published artifacts — had
  independently reached both readings is corroboration rather than proof, but it is the corroboration
  PB5 said to value.

  These two rules are about the fixture's domain rather than the binding layer, which is why they
  live in the vectors and not in `schemas/component-contract.json`: that schema deliberately says
  nothing about what a provider does, and teaching it to would cost more than it buys.

---

## Decision 6 — how fixture annotation is separated from contract data

**Owner:** Portable Binding contract maintainers.
**Raised by:** PB5, when the implementation-neutral provider first read a fixture as published.
**Blocks:** nothing today; a ruling confirms or replaces a convention now relied on by one consumer.

**Context.** The fixture files carry documentation alongside the contract — `additiveOver` on a Shape
version, `nonAdditiveOver` and `nonAdditiveReason` on another, `role` on the encoding-edge Shapes.
`schemas/component-contract.json` declares exactly which fields a contract document has and sets
`unknownFieldPolicy: reject`. A faithful transcode of a fixture file to the wire form is therefore a
**malformed contract**, and both stacks reject it.

Neither stack had ever discovered this, because neither reads the file: each hand-wrote its contract
from it and dropped the annotations by eye. The neutral provider was the first consumer to read the
published form, and could not use it.

Each fixture now declares its own `annotationFields`, listing the names that are documentation, and
the transcode drops exactly those. A future annotation must declare itself or it becomes a malformed
contract at the first consumer.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Per-fixture `annotationFields`** *(running)* | Each fixture lists its own annotation names | Local to the file that needs it; a new annotation declares itself; no schema change | The convention lives in the fixtures, so a consumer must know to look for it; two fixtures could disagree about the same name |
| **B. Declare the convention in the schema** | `component-contract.json` names the annotation mechanism once | One place to learn the rule; a consumer reading the schema alone gets it | Changes a PB1 schema; the schema then describes something that is not part of the contract document |
| **C. Separate the annotation entirely** | Move documentation to a sibling file keyed by Shape reference | The fixture becomes exactly a contract document, transcodable with no rules at all | Two files to keep in step; the annotation loses its adjacency to what it describes, which is most of its value |
| **D. Forbid annotation in fixtures** | Strip the fields; move the reasoning to prose | Nothing to transcode around | Loses machine-readable statements such as which Shape version is additive over which — that is real information, and PB-11 depends on it being true |

**Recommendation: Option B**, adopting the mechanism into the schema while keeping the per-fixture
list as its expression. An implementer working only from `schemas/` should not have to discover the
rule from a fixture. Option A is what runs today and is not wrong, only under-advertised.

**Decision (recorded):** **Option B — the schema declares the mechanism.** Recorded 2026-07-28 by
user:JakHoh. `schemas/component-contract.json` now names the annotation mechanism once, as part of the
`contractDocument` rules that already set `unknownFieldPolicy: reject`, so an implementer working only
from `schemas/` learns that a fixture may carry documentation fields and that they are dropped before
transcoding. The per-fixture `annotationFields` list stays as the expression of the rule: the schema
states that the mechanism exists and how a consumer applies it, and each fixture states which names it
uses. Option A was not wrong, only under-advertised, and this keeps its local self-declaration while
removing the need to discover the rule from a fixture — which is how PB5 found it.

---

## Decision 7 — catching an allocation failure at the transport boundary

**Owner:** Both stack owners, with the contract maintainers.
**Raised by:** PB6 hardening.
**Blocks:** nothing today; a ruling confirms or reverses a deliberate departure from ordinary
practice.

**Context.** The duplex previously mapped only `ObjectDisposedException` and `IOException` to a
portable process category. Every other failure a stream can raise travelled out of the binding as a
runtime type — which C4 forbids, and which a peer could provoke rather than requiring a defect in
this code. Classification is now total, and an allocation failure maps to `resource-exhausted`.

Catching an out-of-memory condition is ordinarily poor practice: the process may be unrecoverable and
swallowing the signal can prolong a bad state. The counter-argument, and the reason it runs today, is
that the alternative here is not a healthier process but a foreign exception in the caller's hands,
and the Channel taxonomy declares `resource-exhausted` for exactly this condition.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Map allocation failure to `resource-exhausted`** *(running)* | Total classification, including the allocation case | No foreign type crosses the seam under any input; gives a declared category a real path | Catches a condition many style guides say never to catch; the observation may be produced by a process that cannot continue |
| **B. Total classification, but allocation failure is `unknown`** | Catch it, classify it less specifically | Still no escape; avoids asserting a cause the layer cannot verify | `resource-exhausted` returns to having no path, which is the state PB6 found and objected to |
| **C. Let allocation failure escape** | Classify everything else; re-raise this one | Follows the common guidance | A peer can provoke a runtime type crossing the binding; C4 is then conditional rather than absolute |

**Recommendation: Option A**, which is what is running, on the grounds that a boundary whose whole
purpose is that nothing foreign crosses it cannot have an exception. If the owner disagrees, Option B
is the safer retreat and keeps the escape closed.

**Decision (recorded):** **Option A — an allocation failure maps to `resource-exhausted`.** Recorded
2026-07-28 by user:JakHoh. Classification at the transport boundary stays total. The ordinary
objection to catching an allocation failure assumes the alternative is a healthier process; here the
alternative is a foreign runtime type in the caller's hands, which C4 forbids, and the Channel
taxonomy declares `resource-exhausted` for exactly this condition. The known cost is accepted: the
observation may be produced by a process that cannot continue. No change to what is running.

---

## Decision 8 — `peer-unavailable` is unreachable in version 0.1

**Owner:** Portable Binding contract maintainers.
**Raised by:** PB6, while giving every declared process category behavioural evidence.
**Blocks:** a complete C8/CH-23 claim, if the owner considers an unreachable declared category a gap.

**Context.** Six of the seven declared process categories now have behavioural evidence in both
stacks. `peer-unavailable` has none, and the reason is structural rather than an oversight: the
binding layer never starts a peer. It is handed a duplex that is already connected, and starting a
peer is the host harness's concern, above the binding. There is no point in this layer at which
"the peer could not be reached" can be observed.

Both stacks assert that the unreachable set is exactly this one value, so if that changes the build
fails and this reasoning returns for review.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Record as by-design** *(running)* | The category stays declared, with an asserted note that 0.1 cannot reach it | Honest; the taxonomy stays aligned with the Channel contract, which is not this binding's to trim | A declared category with no evidence looks like a gap to a reviewer who has not read the note |
| **B. Give the binding layer peer startup** | The layer gains a way to launch or connect to a peer, making the category reachable | Every declared category gets evidence | Substantially widens the layer's responsibility for one observation; process lifetime and launch policy are not otherwise its concern |
| **C. Narrow the 0.1 profile** | Declare that version 0.1 supports six categories and name the seventh as out of profile | The declared set matches the reachable set exactly | The taxonomy is reproduced from `conformance/channel-0.1-vectors.json`, and `channel-envelope.json` says this contract adds no category and removes none; narrowing would break that rule |

**Recommendation: Option A**, which is what is running. Option C is attractive until it collides with
the rule that the taxonomy is reproduced exactly, which is worth more than an even-looking
enumeration.

**Decision (recorded):** **Option A — record as by-design.** Recorded 2026-07-28 by user:JakHoh.
`peer-unavailable` stays declared and unreachable in version 0.1. The binding layer is handed an
already-connected duplex and never starts a peer, so there is no point in it at which the condition
can be observed; giving it peer startup would widen the layer's responsibility to process lifetime and
launch policy for the sake of one observation. Narrowing the 0.1 profile was rejected because the
taxonomy is reproduced exactly from `conformance/channel-0.1-vectors.json` and `channel-envelope.json`
states that this contract adds no category and removes none. Both stacks continue to assert that the
unreachable set is exactly this one value, so a change fails the build and returns this reasoning for
review. No change to what is running.

---

## Decision 9 — the resource floor leaves three C6 conditions unrepresentable

**Owner:** Portable Binding contract maintainers with both stack owners.
**Raised by:** PB6 resource hardening.
**Blocks:** a complete C6 claim, if the owner considers these conditions in scope for 0.1.

**Context.** PB6 asks for adversarial coverage of premature reuse, release-then-use, and unsupported
fallback. Version 0.1's resource floor (Decision 2) makes all three **unrepresentable rather than
merely refused**:

- A **copied immutable blob** is transferred whole and declares no release signal.
- An **addressing-only handle** carries no octets, so there is nothing to release.
- Consequently no frame expresses "use this resource after its interval has ended". There is no
  malformed message for a vector to present.
- **Unsupported fallback** is the same shape: 0.1 declares no fallback policy, so a request cannot
  name one and have it refused.

This is a consequence of Decision 2 rather than a new question, but it only became visible when
someone tried to write the vectors. Both stacks now assert the declared flavor set, so adding a
borrowed or transferred flavor later fails the build and brings this back for review.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Accept, and record** *(running)* | The conditions are out of reach because the floor is deliberately narrow; assert the flavor set so a future widening is caught | No machinery added for conditions the contract cannot express; follows how PB-29 records the non-goal flavors | C6's text mentions borrow interval, lifetime, release, and fallback, so the capability reads as broader than the evidence |
| **B. Add a lifetime-bearing flavor to 0.1** | Adopt the borrowed read-only region rejected as Option B of Decision 2 | Makes premature reuse and release-then-use testable; exercises the parts of C6 currently only declared | Reopens a recorded decision and adds borrow-interval, release, and peer-loss semantics to a version trying to stay small |
| **C. Narrow C6's text for 0.1** | State in the capability that borrow interval, lifetime, release, and fallback are declared-but-unexercised at this floor | The capability text stops over-promising relative to the evidence | Editing a capability to match the implementation is the wrong direction unless the narrower capability is genuinely what 0.1 means |

**Recommendation: Option A** for 0.1, with Option C as a documentation follow-up if the owner agrees
that C6's text currently reads broader than the floor supports. Option B is a 0.2 conversation.

**Decision (recorded):** **Option A, with Option C as the follow-up.** Recorded 2026-07-28 by
user:JakHoh. The narrow floor stands for 0.1: premature reuse, release-then-use, and unsupported
fallback stay unrepresentable rather than being given manufactured paths, and both stacks continue to
assert the declared flavor set so that adding a borrowed or transferred flavor later fails the build
and brings this back for review. Decision 2 is not reopened, and a lifetime-bearing flavor is a 0.2
conversation.

Option C is adopted alongside it: C6's text in `contract-matrix.md` now states that borrow interval,
lifetime, release signal, and fallback policy are declared-but-unexercised at this floor, so the
capability stops reading broader than its evidence. That is a correction to an over-broad capability
statement, not an edit made to match an implementation — the narrower capability is what 0.1 means.

---

## Decision 10 — what supplements independent implementation as a safeguard

**Owner:** Brontide architecture maintainers.
**Raised by:** PB6, from the pattern across all three of its defects.
**Blocks:** nothing mechanical. It is the most consequential question in this file.

**Context.** Two deliberately independent implementations are the programme's central safeguard. PB4
and PB5 showed it working: pairing the stacks found four observation divergences, a fixture that had
silently drifted apart, and a neutral declaration that was not encodable as published. Every one of
those was a place where the two stacks **disagreed**, and disagreement is what independence detects.

PB6 found three defects, and every one was present **identically in both stacks**:

1. Resource observations claimed an acceptance and an integrity check that never happened.
2. The transport let foreign exceptions escape the binding.
3. Two declared process categories had no path that could produce them.

None could have been found by comparing the stacks, because the stacks agreed. The first is the
sharpest: `accepted` and `integrityVerified` are fields the C7 parity profile **compares**, so both
stacks reported the same wrong values and every parity check passed.

The pattern is that two implementations written from one contract by one reader diverge where the
contract is *ambiguous* and agree wherever it is *silent*. Independence tests the contract's
ambiguity. Nothing in the programme currently tests its silence. All three PB6 defects were found by
testing a property — "what must be true of every failure path" — rather than a case.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Property tests as standing practice** | Require every capability to state at least one property over all its vectors, not only per-vector expectations | Directly targets the gap; cheap; PB6 shows it works | Properties are harder to author than vectors and easier to write vacuously |
| **B. Contract-completeness review** | A review pass asking what the contract does *not* say, per capability, separate from conformance review | Attacks silence at the source rather than downstream | Needs a reviewer willing to work from absence, which is a hard brief |
| **C. Neutral-implementation-first** | Require the implementation-neutral endpoint to be written from the published artifacts *before* either stack implements a phase | PB5 showed a cold reader finds what insiders cannot; would have caught the annotation defect at PB1 | Costs a third implementation per phase; the neutral provider exists but is not maintained as a first-class stack |
| **D. Accept the limit** | Record that independence covers ambiguity and not silence, and rely on review | No process change | The three PB6 defects were real, reached the wire, and were found only because someone went looking |

**Recommendation:** A as an immediate practice, B at each phase boundary. C is the strongest and the
most expensive; the neutral provider makes a partial version of it cheap, and requiring it *before*
the stacks implement — rather than after — is what would have caught Decision 6's finding at PB1
instead of PB5. This is a decision about how the programme works, not about the binding, which is why
it is stated here rather than settled by an implementer.

**Decision (recorded):** **Options A and B together.** Recorded 2026-07-28 by user:JakHoh. Both become
standing practice rather than one-off responses to PB6:

- **A — a property per capability.** Every capability states at least one property that must hold over
  *all* of its vectors, not only per-vector expectations. This is what found all three PB6 defects,
  and it targets the gap directly: a property is a claim about every path, so it can fail where no
  single case was written. The known risk is a vacuous property, which is a review obligation on the
  property rather than a reason to skip it.
- **B — a contract-completeness review at each phase boundary.** A pass that asks what the contract
  does *not* say, per capability, kept separate from conformance review, which by construction can
  only check what was written down. It attacks silence at the source rather than downstream.

C is not adopted. Writing the implementation-neutral endpoint from the published artifacts before
either stack implements a phase is the strongest of the four and would have caught Decision 6's
finding at PB1 instead of PB5, but it costs a third implementation per phase and the neutral provider
is not maintained as a first-class stack. B is expected to reach most of what C would, at a fraction
of the cost. D is rejected: the three PB6 defects were real and reached the wire.

The finding this answers is worth restating, because it is the reason the ruling is not "test harder":
two implementations written from one contract by one reader **diverge where the contract is ambiguous
and agree where it is silent**. Independence — the programme's central safeguard — detects
disagreement, so it covers ambiguity and is structurally blind to silence. A and B are the practices
that test silence.

---

## Decision 11 — the plan's provider fact names who was asked, not who answered

**Owner:** Portable Binding contract maintainers.
**Raised by:** PB7, while giving the Composition handoff a provider identity to preserve.
**Blocks:** nothing today, because PB7's handoff performs the check itself. It blocks any host that
uses the binding layer without that seam and needs to know which provider answered.

**Context.** A contract document declares both a `component` and a `provider`. Negotiation compares
the Component by exact reference equality and **never compares the provider at all**. The Binding
Plan's `provider` and `selectedProvider` facts — and the `selectedProvider` field of every C9
observation built from them — are read from the **required** document, which is the host's own
declaration. So an endpoint may offer the required Component while answering as a different provider,
and the established plan will report the provider the host asked for.

Both stacks do this, identically, and nothing before PB7 could have noticed. Every fixture on both
sides derives from one neutral declaration, so the required and offered providers agree in every
vector, in both cross-stack directions, and against the implementation-neutral provider. The fact and
the truth coincide everywhere the programme has looked.

The `provider` field also means two different things by side: on an offered document it is the
endpoint identifying itself, and on a required document it is closer to an expectation. Nothing
reconciles the two.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Check at the composition seam** *(running)* | The handoff witnesses the offered contract during establishment, refuses a substitution as `unsupported-contract`, and abandons the binding | Which provision was selected is genuinely a composition fact; no contract or plan change; a substitution cannot reach Release | A host that uses the binding layer without the handoff still cannot learn who answered, and the plan fact remains a claim about the host's own declaration |
| **B. Negotiation compares provider identity** | Add provider equality to the negotiation steps, refusing a mismatch as `unsupported-contract` | The check lives where every other exact-match rule lives; every consumer gets it | Changes what a required document *means*: naming a provider becomes binding rather than expectational, and a host that wants "any provider of this Component" needs a way to say so |
| **C. The plan reports the answering provider** | Keep negotiation as it is, but read `provider`/`selectedProvider` from the offered document | The fact stops describing the host's own input; observations name who actually served | Changes a frozen plan fact and a C9 field; PB4's parity evidence and PB2/PB3 plan-fact vectors would need re-measuring, and it papers over the two meanings rather than resolving them |
| **D. Accept and document** | Record that the fact names the required declaration and leave it | No change | An observation field named `selectedProvider` that does not name the selected provider is exactly the shape of PB6's first defect |

**Recommendation: B, with C.** The two together make the fact true by construction: negotiation
refuses a mismatch, so the required and offered providers are equal whenever a plan exists, and
reading the fact from the offered document then says what actually happened. If B alone is chosen,
the two are equal anyway and C becomes cosmetic; if C alone is chosen, the fact becomes true without
the mismatch ever being refused. Option A stays valuable either way, because a composition still has
to check that the endpoint it reached is the provision its resolution selected — but it should not be
the only place the question is asked.

**Decision (recorded):** **Option B, then C. Option A retained.** Recorded 2026-07-30 by
user:JakHoh.

Negotiation compares the provider by exact reference equality and refuses a mismatch as
`unsupported-contract`; the Binding Plan's `provider` and `selectedProvider` facts, and the C9
`selectedProvider` observation, are read from the **offered** document. PB7's composition-seam check
stays, because it asks a different question — whether the endpoint reached is the provision the
resolution selected — which is a Component Management question that happens to coincide with this
one today.

Rationale beyond the recommendation above, as ruled:

- By the time negotiation runs in this programme the provider is not an open question. CM2
  resolution selects it and CBI1 carries that completed decision into preflight, so making a named
  provider binding matches how the layer is actually used. Every fixture, the neutral provider, and
  every CBI slice names a specific provider; the "any provider of this Component" host is
  hypothetical.
- B does not foreclose that host, it defers it. An absent or wildcard provider in the required
  document is an additive change if the need appears, whereas a permissive default could not be
  tightened later without breaking every existing host. **The wildcard is deliberately not defined
  now.**
- C is not cosmetic, and this is the part that decided the pairing. Under B the two are equal, so C
  changes no value — but it changes the fact from *true by invariant* to *true by construction*. If
  B is ever relaxed to admit the wildcard above, a plan reading from the required document silently
  becomes a claim about the host's own input again, which is PB6's first defect returning through
  B's own escape hatch. Reading from the offered document degrades gracefully to "who answered",
  which stays the honest reading of a field with that name.
- Ordering matters and is part of the ruling: **B lands first**, which makes C nearly free because
  the values do not change and the PB2/PB3 plan-fact vectors and PB4 parity evidence re-derive
  identically. C alone would have been both the expensive option and the incoherent one.

**Consequence for evidence.** After B, the required and offered providers are equal whenever a plan
exists, so **no black-box vector can distinguish C from reading the required document**. C is
observable only in the source and in what this record says the fact means. The vectors therefore pin
B — a substituted provider is refused — and C is carried by this decision and the schema text rather
than by a discriminating vector. That limit is stated here rather than left for a later reader to
notice, because it is exactly the shape Decision 10 warns about: the fixtures agree everywhere, so
the contract has to speak where the vectors cannot.

---

## Decision 12 — a receiving-domain Actor freed by a dropped member, taken by a different party

**Owner:** Component Management / Portable Binding integration owners.
**Raised by:** CBI20, twice and independently — two sessions implemented the slice from the same
priority document, and this is the only substantive answer they disagreed on.
**Blocks:** nothing today. The merged slice runs Option A, and the window the question is about is
internal to one synchronous call, so no caller can currently observe either answer.

**Context.** CBI6 refuses two participants of one set being mapped onto one receiving-domain Actor,
because that would merge their grants into one holder. CBI13 lifts the rule to the activation: across
the members of one activation the participant-to-local-Actor mapping must be a function and
injective. Until a membership could change, the rule had nothing to say about a replacement, because
the successor mapped the same parties onto the same local Actors as the generation it replaced.

CBI20 makes the mappings differ. A dropped member's participant releases its hold on a local Actor,
and an added member's participant may ask for that same Actor. The two implementations answered
differently, and both derived their answer from a rule already in the repository:

- the merged slice reads CBI13's rule as **a property of an activation**, and the retained activation
  ends at cutover, so a local Actor a *dropped* participant held is free for a different party in an
  *added* member; the same reuse against a **surviving** participant stays refused, because that one
  is a conflation inside the successor's own mapping;
- the superseded one read the same rule as **a property of the receiving domain**, and refused the
  reuse while the retained generation still held it, because CBI19 accepts that both generations are
  established against the same binding scope between the successor's Release and the retained
  members' retirement — so for that window a grant admitted for the new party and one admitted for
  the old are both in force on one local Actor, which is CBI6's stated reason for refusing it.

Two facts bear on it and neither is decisive on its own. The window is real but is inside one method,
so nothing in either stack can observe it and no vector distinguishes the answers. And CM5 — the
receiving domain itself — has no notion of an activation or an attempt at all: it admits against an
occurrence under local policy and would admit both mappings without complaint. The distinctness rule
exists only in the composition root, so this is a question about what invariant the root intends to
hold, not about what the modelled domain enforces.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Permitted for a dropped participant's Actor** *(running, merged)* | CBI13's rule is checked over the successor's membership alone; reuse against a surviving participant stays refused | The rule keeps one scope — an activation — rather than acquiring a second, special one for replacements; a replacement can genuinely re-house a receiving-domain identity; matches CM5, which decides per occurrence and knows nothing of attempts | For the width of the cutover, two parties hold one local Actor across two established generations, which is the state CBI6 refuses within a set; nothing records that the window is intended |
| **B. Refused while the retained generation still holds it** | The mapping is checked over the union of the retained and successor activations; reuse becomes available only after the retained members retire, through CBI14 retirement and a fresh admission | Preserves CBI6's reason rather than its wording — the conflation is never live, even briefly; fails closed | Gives the rule a scope no other slice uses; forbids a re-housing that is otherwise legitimate; the state it prevents is unobservable in this programme, so the cost is paid against a hazard nothing here can demonstrate |
| **C. Refused unconditionally across a replacement** | Neither party may change local Actor and no local Actor may change party across a cutover | Simplest to state | Strictly stronger than B and rejected by both readings: it also refuses re-homing a *surviving* party, which is an ordinary CBI13 question the successor's own mapping already answers |
| **D. Ask the receiving domain** | Model CM5 as holding local-Actor occupancy across attempts and let it refuse | Puts the rule where the identity actually lives | CM5 admits against an occurrence by design and holds no cross-attempt state; adding one is a CM5 contract change, not an integration decision |

**Recommendation: A, with the window written down.** A is running, and the argument that decided it
is the better-formed one: CBI13's rule is stated over an activation, the retained activation is over
at cutover, and A keeps the refusal for the case that is genuinely a conflation — a surviving
participant. B's argument is not wrong, but the state it prevents cannot be reached by any caller in
this programme, so adopting it would buy an unobservable guarantee at the cost of a rule with a scope
nothing else uses. What A is missing is not a refusal but a record: the overlap is a deliberate
window, and neither the CBI19 nor the CBI20 contract says a receiving-domain identity may be reused
inside it.

**Decision:** **Open.** Raised 2026-08-01.

**What would settle it.** Not a vector, on current evidence — the window is inside one call, so both
options produce identical observations, which is the shape Decision 10 warns about. It would become
observable if a replacement ever became interruptible, if retirement of the retained members were
deferred past the call that cuts over, or if CM5 gained any cross-attempt notion of local-Actor
occupancy. Any of those three turns this from a question about intent into one a test can answer, and
each of them argues for B; none is in scope for version 0.1.

---

## Decision 13 — Relational Initialisation is declared out of scope, and CM4 needs it

**Owner:** Portable Binding contract maintainers, with the Component Management integration owners.
**Raised by:** CBI21, on finding that the seam and CM4 disagree about whether the stage exists.
**Blocks:** no owner ruling remains. Portable Binding 0.1 continues to refuse any activation of a
CM3 group that declares a bounded lifecycle protocol as `relational-initialisation-unsupported`.
The recorded 0.2 work is required before those groups can activate through this seam; nothing else
is blocked, because a strongly connected group that declares no protocol activates today.

**Context.** CM3 plans a group carrying bounded lifecycle protocols with four stages — local
initialisation, interconnection, **relational initialisation**, ready — and CM4 admits a lifecycle
interaction only during the third, matching it against exactly one declared protocol by edge,
direction, Operation, Capability, and input Shape. The PB7 Composition handoff declares
`Relational Initialisation` in its `outOfScope` array, and its stages run local initialisation →
interconnection → ready → release with nothing between the last two.

Two independent things are missing, and the second is easy to overlook:

- **No verb.** The seam offers a composition exactly one traffic verb, and it is gated on Release.
  Establishment, readiness, withdrawal, and termination are the seam's own lifecycle traffic and none
  of them names an Operation, a Capability, or an input Shape, which a declared protocol does.
- **No window.** A portable member reports Ready *during* Interconnection: establishment and the
  readiness signal are one step, so the member is Ready the moment Interconnection returns. CM4
  requires Relational Initialisation to complete **before** Ready. Adding a verb without splitting
  that step would leave the handshake with nowhere to run that still precedes the readiness it must
  precede.

There is also a structural question underneath both. A portable member binds a **host to a provider**;
a declared protocol is traffic from one group member **to another**. Whether the second is expressible
as the first — the composition root standing in for the initiating Component, as CBI1 already has it
stand in for the consumer — or needs a Component-to-Component binding the layer does not define, is
the part of this decision with the widest blast radius.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Leave it out of scope** *(retained for 0.1)* | Version 0.1 keeps the declaration; CBI21's refusal names it and a composition needing a handshake does not use this seam for it | No contract change; the published schemas, vectors, and the pending independent review stay as they are; strongly connected groups that need no handshake are already delivered | CM3 and CM4 model a stage the integration can never reach through 0.1, so a whole class of CM4 plan stays unactivatable through that version |
| **B. Split readiness from establishment, and add a declared-protocol verb** *(selected for 0.2)* | Interconnection stops implying Ready; a `relational` stage sits between them, carrying an Operation, Capability, and input Shape drawn from the group's CM3 protocols and refused otherwise; Ready becomes a separate signal | Matches CM4's stage order exactly, so the derived observations stay derived rather than claimed; the refusal rules already exist in CM4 and would be mirrored, not invented | The largest change in the layer: a new stage in the published contract, a new envelope kind, new neutral vectors in both directions, and re-measurement of the parity profiles; it therefore follows rather than moves the pending PB8 review target |
| **C. Add the verb without splitting readiness** | A pre-Release traffic verb admitted while interconnected, with the handshake running after Ready | Much smaller than B; no envelope or stage change | Produces the wrong order: a handshake after Ready cannot be projected into CM4 as a Relational Initialisation interaction without claiming a sequence that did not happen, which is the class of false projection CBI10 and CBI16 exist to refuse. Not recommended in any form |
| **D. Model a Component-to-Component binding** | A binding whose two ends are both provisions, of which the protocol is ordinary traffic | Puts peer traffic where peer traffic belongs and would serve more than this stage | A new binding kind in a layer built end-to-end around one host and one provider; far beyond what the stage needs and not obviously required, since the composition root can initiate on a member's behalf |

**Recommendation: A for version 0.1, B for the version that follows, and C ruled out.** A is
running and costs nothing today; the group shapes that need no handshake are delivered, and the
refusal now names the missing capability rather than a group's shape. B is the only option that makes
the observation honest, and it is a version boundary's worth of work rather than a slice's — it
should be planned with the PB8 reviews rather than landed under them. C is listed only to record why
the cheap option is wrong, since it is the one a later implementer will reach for first.

**Decision (recorded):** **Option A retained for Portable Binding 0.1; Option B selected for Portable
Binding 0.2. Options C and D rejected for this decision.** Recorded 2026-08-11 by user:JakHoh.

The Composition handoff's `outOfScope` entry is a 0.1 version limitation, not a permanent boundary of
the seam. Version 0.1 remains unchanged and fails closed for every protocol-bearing group so its
published schemas, vectors, parity profiles, and then-pending PB8 independent-review target remain
stable.
The 0.2 contract will separate establishment from readiness and add distinct relational lifecycle
traffic before Ready. That traffic carries the exact CM3-declared edge, direction, initiating and
receiving members, Operation, Capability, and input Shape; anything not declared is refused before
delivery. Ordinary request traffic remains closed until Release.

The composition root may initiate the lifecycle Operation on a Component's behalf, as it already
stands in for the consumer at the PB7 seam. This ruling does not introduce a Component-to-Component
binding kind. Lifecycle authority must be explicit and exact rather than inferred from participant
admission or ordinary-interaction authority. A failed relational interaction prevents Ready and
Release, preserves any effects that actually occurred in the observation, and returns cleanup or
rollback to CM4 instead of fabricating a completed stage.

**Consequence for sequencing.** PB8 reviews the unchanged 0.1 evidence first. The 0.2 work then begins
with its own C1-Cn behavioural contract and phase-boundary completeness review before any public
surface or schema is added; it requires a migrated neutral contract, new envelope and lifecycle
transitions, independent native implementations and named tests in both stacks, parity remeasurement,
and integration evidence that a CM3 protocol-bearing group reaches CM4's exact stage order.

---

## Decision 14 — nothing records that a restart scope has children

**Owner:** Component Management / Portable Binding integration owners.
**Raised by:** CBI23 and CBI24, from opposite directions.
**Blocks:** nothing today. Both slices work when the caller names its attachments; neither can act
when the caller does not.

**Context.** CM4 models a child attachment as an input to one activation attempt: it requires the
parent scope active when the child attaches, preserves the parent through the child's activation, and
keeps no record afterwards. Its C2 property makes that explicit from the other side — every outcome
preserves the generation and activity state of every *unrelated* scope, and a child scope is
unrelated. CM2 records which Port a position was resolved into, but nothing records which scopes are
attached to a generation.

Two slices have now hit the consequence from opposite directions, which is why it is worth a ruling
rather than a third work-around:

- **CBI23** orders the withdrawal of an attachment forest deepest-first, and can only order the
  activations it is given. A child the caller omits is retired never, and its parent is retired
  anyway.
- **CBI24** replaces a generation that has attachments beneath it, and can only stand down the
  attachments it is given. A child the caller omits is silently orphaned: it keeps running, attached
  to a generation that is no longer active anywhere, and neither CM4 nor the composition root will
  ever look again.

Both slices state the hole rather than implying completeness, and both report exactly what they were
given so the omission is visible by absence. Neither can do better with the inputs it has.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Leave it with the caller** *(running)* | The composition root acts on the attachments it is told about and reports exactly those | No model change; matches CM4, which deliberately keeps a child's scope outside the parent's transaction; the two slices already fail closed on everything they can see | The one failure that matters — forgetting a child — is the one nothing catches, and it produces a Component running against a generation that no longer exists |
| **B. CM4 records a scope's children** | The runtime tracks attachments per scope, so a replacement can refuse while children are attached and a withdrawal can enumerate them | Makes both slices' holes closable, and makes "no orphan" checkable rather than promised | Changes CM4's shape: a child's scope stops being an unrelated scope, which is the property C2 states, and CM4 becomes stateful across attempts where today every attempt is self-contained |
| **C. The composition root keeps the registry** | The root records each attachment it makes and consults it on withdrawal and replacement | No CM4 change; the root already performs every attach, so it can see them all | Only covers attachments this root made in this process; a second root, or a restart, sees nothing, so it converts a visible hole into an invisible one |
| **D. Refuse to replace any generation that offers a Port** | Conservative: a generation with Ports declared cannot be replaced at all | Trivially prevents the orphan | Refuses the legitimate case this slice exists to serve, and CM2 declares Ports on generations that may never have anything attached |

**Recommendation: A, until the registry has a home that outlives a process.** B is the only option
that makes the guarantee checkable, and it is a change to a contract whose whole shape is
self-contained attempts — the property that makes CM4 testable. C looks cheap and is the worst of the
options on offer, because a partial registry reports success while missing exactly the attachments a
second root made. The question worth putting to owners is whether the guarantee is wanted enough to
make CM4 stateful across attempts, and that is a Component Management decision rather than an
integration one.

**Decision:** **Open.** Raised 2026-08-02.

**What would settle it.** A vector cannot: both options produce identical observations for every
attachment the caller does name, and the difference is only visible for one it does not — which is
precisely what no test can supply, because supplying it makes it named. It becomes decidable the
moment anything else needs to enumerate a scope's children — a supervisor, a status projection, or a
restart that must rebuild the forest — and each of those argues for B.

---

## Decision 15 — CM2 can declare a Mediation that owns authority; CM5 cannot represent one

**Owner:** Component Management owners.
**Raised by:** CBI26.
**Blocks:** nothing today. A Mediation declaring `OwnsAuthority` is refused, and every other
Mediation is admitted for what its mediator does itself.

**Context.** CM2's `MediationDeclaration` carries six ownership flags, one of which is
`OwnsAuthority`: the Mediation is responsible for the authority of the interaction it fronts. CM2
takes it seriously enough to require any policy-bearing Mediation to be realized as a dedicated
Component.

CM5 cannot express it. Its `ActorRelationshipKind` offers `AttachedDevice`, `ExternalPeer`, and
`ComponentParticipant`, none of which means *acts on behalf of*; and `LocalCapabilityGrant` names
exactly one `Holder`, a local Actor, with no beneficiary beside it. So a mediator is admitted for its
own interaction and there is no way to say that a grant it holds is exercised for a member behind it.

CBI26 refuses the declaration rather than approximating it, because the approximation — admit the
mediator, let its narrow grants stand for the members — reads as working and decides what a deputy is
in the least visible place available. The result is that the two models disagree about what is
expressible, which is a question for their owners rather than for a composition root.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Refuse the declaration** *(running)* | A Mediation that owns authority has no integration path; every other Mediation works | Honest about the disagreement; no model changes; the capability that cannot be represented is visibly unavailable rather than silently approximated | CM2 can declare something no downstream model can act on, so a resolution that is valid produces an integration that is refused |
| **B. CM5 grants gain a beneficiary** | A grant names a holder and, optionally, the Actor it is exercised for | Makes deputy authority representable where it belongs, next to the holder | Every consumer of a grant must decide what a beneficiary means for it, and CBI3's "one grant, one holder" rule — which several slices rest on — becomes "one grant, one holder, and maybe someone else" |
| **C. A deputy relationship kind** | `ActorRelationshipKind` gains a kind meaning "acts for", and admission records whom | Keeps grants simple; the relation is where CM5 already models who someone is | A relationship is between a participant and the receiving domain, not between two participants, so the kind would carry a second Actor no other kind has |
| **D. CM2 drops the flag** | `OwnsAuthority` is removed, and a Mediation that would own authority is modelled as the members delegating explicitly | Removes the disagreement at its source; the members' own admissions stay theirs | Loses a distinction CM2 currently makes, and the delegation it replaces has no model either |

**Recommendation: A, until something other than an integration slice needs it.** The disagreement is
real but nothing yet requires deputy authority to work — every Mediation this programme resolves owns
nothing. B and C both spend a model change on a capability with no consumer, and B in particular
weakens a rule that CBI3, CBI6, CBI13, and CBI20 all lean on. D is the cheapest and the most
destructive: the flag is CM2 saying something true about mediation, and removing it to resolve a
downstream limitation would be the wrong direction.

**Decision:** **Open.** Raised 2026-08-02.

**What would settle it.** A consumer. The question is decidable the moment a Mediation that owns
authority has to actually work — a real arbitration or aggregation mediator whose members cannot hold
their own grants — because that names what the beneficiary is for. Until then, every option is a
guess about a shape nobody has had to build.

---

## Decision 16 — a CM binding scope holds many bindings; a portable one names a single binding

**Owner:** Component Management owners, with Portable Binding owners for the seam's reading.
**Raised by:** CBI27.
**Blocks:** nothing today. Every member CBI1 prepares is well formed, and the collision is between two
members of one composition rather than inside either.

**Context.** The two models mean different things by "binding scope", and CBI1 maps one straight onto
the other.

The portable one is the composition's identity for a position. It *"survives withdrawal, termination,
and replacement"* where the planId does not, and the seam's `scope-uniqueness` declared silence says
uniqueness within a composition is the composition's responsibility, because *"a composition that
reuses a scope has two members claiming one position, which its own resolver is the place to reject"*.
One member holds one scope, and the handoff's own `laterUse` note expects to be called *"once per
'1..1' requirement"*.

The CM one is a container. `OccupiedBindingEntry` carries a `BindingId` beside its `BindingScopeId`,
CM2 looks occupied bindings up by scope **and contract**, and it refuses several occupied bindings in
one scope only when the requirement's cardinality is `1..1` — which is CM2 saying, in code, that a
scope holds one binding per member of a wider position and that `BindingId` is what tells them apart.

CBI1 unwraps `providerSet.Scope` into the portable scope, and its C4 states that *"every successful
member reports the same scope text the resolved Provider Set carried"*. That is a bijection under two
conditions it does not state: the position is `1..1`, and the scope holds one position. CBI27 breaks
the first by construction — a wide position has one scope and several members — and takes an explicit
portable scope per member instead, which is how CBI1 already handles every other portable identity.

**The second condition is already false and nothing noticed.** The multi-member slices from CBI12
onward resolve two or three positions in one CM binding scope, so their prepared members all report
`scope.cooling` and reach the seam as several members claiming one position. Both stacks do it
identically, because every fixture clones one requirement template and no vector ever compared two
members' scopes. A named test in each stack now pins it.

What makes this a decision rather than a fix is what a correction moves. The `bindingScope` fact is a
resolution fact, CBI4's canonical profile includes every resolution fact, and the shared fixture pins
that profile's SHA-256 per scenario. Changing how the portable scope is derived therefore invalidates
cross-stack pinned evidence and needs a repin, not a slice.

| Option | What it is | Pros | Cons |
| --- | --- | --- | --- |
| **A. Leave CBI1's mapping, take the scope explicitly where it cannot work** *(running)* | CBI1 keeps unwrapping the CM scope; CBI27 takes one portable scope per member and refuses collisions within a set | No repin, no digest change, and the case a wide set forces is handled correctly; the remaining collision is between positions the caller chose to put in one CM scope | Two positions in one CM scope still reach the seam as one, so the composition is doing exactly what the seam names as a resolver defect, and CBI1's C4 reads as a general rule when it is a property of `1..1` |
| **B. Derive the portable scope from the CM scope and the member** | CBI1 composes scope, contract, and occurrence into the portable identity | Faithful to what each model means; makes every member's scope unique by construction, with no caller involvement | Moves every member's `bindingScope` fact, so every CBI4 digest is repinned and the cross-stack comparison evidence is re-established; and the composition root becomes the author of an identity that survives replacement |
| **C. Take the portable scope explicitly everywhere** | Every `ComponentBindingSelection` carries the portable scope, as it carries the portable Component and provider | Matches CBI1's own C2 discipline exactly — identity correspondence explicit, never inferred from spelling; each stack checks distinctness across a composition | Changes the entry point of all 27 slices in both stacks, and moves the digests as B does unless every fixture happens to supply the old text |
| **D. Refuse a composition whose members' scopes collide** | The group activation path refuses two members holding one portable scope | Cheap, fails closed, and turns an invisible collision into a visible refusal | Refuses a legal CM resolution: two positions in one CM scope is what a CM scope is for, so the refusal would be the integration objecting to something CM2 permits |

**Recommendation: A now, C when the digests are being repinned for another reason.** B and C both fix
the concept; C is the honest one, because the portable scope belongs to the composition's identity
space and every other portable identity in this programme is supplied rather than derived. Neither is
worth spending a cross-stack repin on by itself, and D trades a latent identity problem for a refusal
of something CM2 allows. What A costs is precision in CBI1's C4, which this slice's contract now states
explicitly rather than leaving to a reader.

**Decision:** **Open.** Raised 2026-08-02.

**What would settle it.** Anything that reads a binding by its portable scope. Today nothing does —
withdrawal, replacement, and the gate all work from a member the caller already holds — so two members
sharing a scope produce two independent, correct bindings and two replacement records that happen to
name the same scope. The moment a composition looks a binding up by scope, or a replacement record is
matched to the position it replaces, the collision stops being latent and B or C becomes forced.
