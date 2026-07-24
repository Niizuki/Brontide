# Portable Binding — open owner decisions (PB0 exit blockers)

**Status:** PB0 decisions — **both recorded 2026-07-24** (Decision 1: deterministic CBOR core;
Decision 2: copied immutable blob). Non-pinned. These were the PB0 exit blockers (plan §5:
"unresolved encoding questions are explicit blockers rather than implicit code choices"); with both
resolved, PB1 may author `schemas/` and `vectors/`.

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
