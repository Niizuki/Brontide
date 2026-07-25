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

## Golden encodings are verified, not asserted

Every `cbor` field in `golden-encodings.json` is re-derived by the gate from that entry's `value`
description using an independent deterministic-CBOR encoder, and compared byte for byte. A hand-typed
byte string that does not match the encoding rules fails the build rather than silently becoming the
contract. The `rejectedEncodings` entries are the converse: byte strings that must not decode.

`G1` is the case worth reading first. Its two keys order `loop` before `enabled` because deterministic
CBOR sorts on the encoded key, while the Cooling JSON codec's ordinal string comparison would reverse
them. That divergence is migration work for PB2 and PB3, and it is why the golden bytes exist.

## Coverage the gate enforces

- every capability `C1`-`C10` has at least one vector;
- every Channel vector `CH-01` through `CH-24` is referenced by at least one vector;
- every protocol-error category, process-failure category, and failure domain in the Channel taxonomy
  is covered;
- all three classifications are present; and
- every enumeration value used here exists in [`../schemas/`](../schemas/README.md).
