# Portable Binding — open owner decisions

**Status:** Decisions 1 and 2 (the PB0 exit blockers) are **recorded**. Decisions 3 through 10 are
**open and awaiting an owner ruling**; each was raised by evidence in PB4, PB5, or PB6, and each is
currently running on a provisional choice made by the implementer so that the phase could proceed.
Non-pinned.

**How to read this file.** Every decision below is written to be answerable without any other
context: it states what was observed, what is running today and why, what the alternatives are, and
what a ruling would change. A provisional choice is *not* a decision — it is a placeholder that the
owner may confirm or overturn, and overturning one is expected to be cheap because each is isolated
behind a named seam.

## Index

| # | Decision | Raised by | State |
| --- | --- | --- | --- |
| 1 | Portable wire representation | PB0 | **Recorded** 2026-07-24 — deterministic CBOR core |
| 2 | Referenced-shaped-resource v0.1 floor | PB0 | **Recorded** 2026-07-24 — copied immutable blob |
| 3 | Failure domain of a refusal decided by the provider endpoint | PB4 | **Open** — running provisionally |
| 4 | Where the no-capability-transfer scan is enforced | PB4 | **Open** — running provisionally |
| 5 | Catalog fixture canonical form, and whether it needs vectors | PB5 | **Open** — running provisionally |
| 6 | How fixture annotation is separated from contract data | PB5 | **Open** — running provisionally |
| 7 | Catching an allocation failure at the transport boundary | PB6 | **Open** — running provisionally |
| 8 | `peer-unavailable` is unreachable in version 0.1 | PB6 | **Open** — recorded as by-design |
| 9 | The resource floor leaves three C6 conditions unrepresentable | PB6 | **Open** — recorded as by-design |
| 10 | What supplements independent implementation as a safeguard | PB6 | **Open** — no provisional answer |

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

The eight below are open. Each arose because implementing or testing a phase surfaced a question the
contract did not answer. In every case the implementer chose provisionally so the phase could finish,
and the choice is named here so it can be confirmed or overturned deliberately rather than inherited
by accident.

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

**Decision (open).**

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

**Decision (open).**

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

**Decision (open).**

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

**Decision (open).**

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

**Decision (open).**

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

**Decision (open).**

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

**Decision (open).**

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

**Decision (open).**
