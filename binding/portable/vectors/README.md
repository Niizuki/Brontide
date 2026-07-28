# `binding/portable/vectors/`

Valid, additive-compatible, and adversarial fixtures with exact expected outcomes, authored in
**PB1** (plan §5).

These are data only and validate without loading either stack.
[`build/verify-portable-binding.ps1`](../../../build/verify-portable-binding.ps1) is the gate.

## Contents

| File | Contents |
| --- | --- |
| [`fixture-contract.json`](fixture-contract.json) | The subject contract: the Cooling experiment restated in the neutral form, plus the encoding-edge Shapes the golden values need |
| [`golden-encodings.json`](golden-encodings.json) | Deterministic CBOR byte forms, and the encodings that must be rejected |
| [`establishment-and-shapes.json`](establishment-and-shapes.json) | C1 and C5 — negotiation, the Shape floor, projection, forbidden content |
| [`authority-and-resources.json`](authority-and-resources.json) | C3 and C6 — local authority, no-capability-transfer, referenced resources |
| [`limits-lifecycle-and-channel.json`](limits-lifecycle-and-channel.json) | C8 and C4 — declared limits, lifecycle states, envelopes, correlation, failure taxonomy |
| [`plan-observation-and-parity.json`](plan-observation-and-parity.json) | C2, C9, C7, C10 — plan facts, observation completeness, realization parity, interoperability |
| [`catalog-fixture-contract.json`](catalog-fixture-contract.json) | The second subject contract: the Catalog experiment restated in the neutral form |
| [`catalog-vectors.json`](catalog-vectors.json) | PB-64 – PB-69 over the Catalog fixture — a negotiated Operation set, nested repeated containers, a declared detail Shape, and the addressing-only handle |

## Vector form

Each vector carries an `id` matching `PB-<nn>-<NAME>`, the `capabilities` it evidences, the
`channelVectors` from [`conformance/channel-0.1-vectors.json`](../../../conformance/channel-0.1-vectors.json)
it preserves, a `classification`, `given`/`when`/`then` clauses, and an `expected` category-level
observation. Ids are unique across every file in this directory.

`classification` is one of:

- `valid` — the contract's intended behaviour;
- `additive-compatible` — a version-skew case that must still succeed through projection; and
- `adversarial` — a case that must fail closed with an exact category.

`expected` uses the Channel taxonomy verbatim: `frameDecision`, `resultClass`, and where they apply
`category`, `processCategory`, `failureDomain`, and `effectCount`. The gate rejects any value outside
that taxonomy, so a vector cannot invent a category.

Vectors carrying a `phase` (PB4 or PB5) state an obligation a stack harness discharges later. PB1
fixes what must be equal and what may differ; it does not execute the comparison.

Vector ids are two-digit by the gate's pattern, so the space is `PB-01` through `PB-99`; `PB-01`
through `PB-63` are PB1's and `PB-64` through `PB-69` are the Catalog group's.

## Golden encodings are verified, not asserted

Every `cbor` field in `golden-encodings.json` is re-derived by the gate from that entry's `value`
description using an independent deterministic-CBOR encoder, and compared byte for byte. A hand-typed
byte string that does not match the encoding rules fails the build rather than silently becoming the
contract. The `rejectedEncodings` entries are the converse: byte strings that must not decode.

`G1` is the case worth reading first. Its two keys order `loop` before `enabled` because deterministic
CBOR sorts on the encoded key, while the Cooling JSON codec's ordinal string comparison would reverse
them. That divergence is migration work for PB2 and PB3, and it is why the golden bytes exist.

## The Catalog group, and why it exists

Every group above except the last is authored against the Cooling fixture. PB5 declared the Catalog
fixture contract but no vectors over it, so the neutral layer stated what Catalog *is* without
stating what it must *do*, and a third implementation working only from `binding/portable/` received
a contract carrying no expected observations. Decision 5, recorded 2026-07-28, closed that.

`catalog-vectors.json` deliberately covers only what Cooling structurally cannot state — a
negotiated Operation *set* rather than a single Operation, a repeated container whose elements are
themselves repeated containers, a declared detail Shape, and the provider-scoped handle. Where an
existing vector already states a rule generally, the Catalog vector names it rather than restating
it.

Two of its vectors fix where a boundary falls that no earlier vector settled:

- **PB-69** fixes where a resource refusal splits between two categories. A refusal at the *flavor*
  level is `unsupported-contract`, because a flavor is a term of the frozen contract — PB-29 reaches
  that during negotiation and PB-69 after establishment, and both answer the same way. A refusal at
  the *instance* level is `invalid-payload`, because the flavor was negotiated and only this
  particular resource is inadmissible: PB-28's out-of-scope handle and PB-26's failed content hash.
- **PB-68** makes PB-27's claim falsifiable. PB-27 states that an accepted handle conveys addressing
  only; PB-68 states the consequence — the same accepted handle accompanies both a success and a
  shaped failure, so admitting the handle and admitting the request are two decisions.

## Properties, not only cases

`catalog-vectors.json` carries a `properties` block alongside its vectors. A vector states what one
case must produce; a property states what must hold over *every* case in the group, including ones
nobody wrote. This is the Decision 10 practice, adopted because all three PB6 defects were
invariants that every individual expectation happened to satisfy, and were therefore invisible to
comparing the two stacks.

The practice earned itself immediately: `CATALOG-P2` was first written as "every resource the
observation reports is of the negotiated flavor" and failed, because a *refused* resource is still
reported — with `accepted: false`, exactly as PB6 required. The property now quantifies over accepted
resources, which is the claim that is actually true.

## Coverage the gate enforces

- every capability `C1`-`C10` has at least one vector;
- every Channel vector `CH-01` through `CH-24` is referenced by at least one vector;
- every protocol-error category, process-failure category, and failure domain in the Channel taxonomy
  is covered;
- all three classifications are present; and
- every enumeration value used here exists in [`../schemas/`](../schemas/README.md).
