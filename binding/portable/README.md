# Portable Component Binding — neutral contract artifacts (`binding/portable/`)

**Status:** PB1 complete, implemented natively in both stacks (PB2 Reference, PB3 Minimal), measured
for direct-versus-process parity in each (PB4), paired across the stacks and against an
[implementation-neutral provider](../neutral-provider/README.md) (PB5), hardened (PB6), and given the
Composition handoff by which composition machinery reaches the layer (PB7) — planned experimental
work; not ratified; not part of Brontide Base.
**Designed for:** Brontide Architecture 0.8 §16 and §18.1 (Complete Draft).
**Plan:** [Portable Component Binding Implementation Plan 0.1](../../docs/future/binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md)

This directory holds the **implementation-neutral, data-only** contract for the Portable
Component Binding programme: schemas, manifests, golden values, adversarial vectors, and the
cross-stack contract matrix. It MUST NOT contain executable semantic logic shared by the
Reference (C#) and Minimal (F#) stacks; each stack generates its own code from the checked-in
neutral source and owns its adapters.

## Layout

| Path | Contents | Introduced |
| --- | --- | --- |
| `contract-matrix.md` | C1–C10 baseline inventory: owner, existing basis, classification, gap-to-close per capability | PB0 |
| `representation-choice.md` | D3 / §11 chain-conjunction representation choice and revocation ceiling per stack (Portable Binding freeze prerequisite) | PB0 |
| [`open-decisions.md`](open-decisions.md) | Owner decisions with option sets and recommendations. The two PB0 encoding blockers are recorded; **nine further decisions raised by PB4, PB5, PB6, and PB7 are open** and each is running on a provisional implementer choice until ruled on | PB0, extended PB4-PB7 |
| [`schemas/`](schemas/README.md) | Data-only versioned neutral contracts (references, Shape floor, plans, envelopes, and the PB7 Composition handoff) | PB1, extended PB7 |
| [`vectors/`](vectors/README.md) | Valid, additive-compatible, and adversarial fixtures with expected outcomes | PB1, extended PB7 |

## PB0 exit checklist (plan §5)

- [x] Inventory the existing Cooling/Catalog surface and map each field, message kind, value
  variant, correlation identity, error code, limit, resource rule, and observation to C1–C10 with an
  owner, classification, and expected category-level observation (`contract-matrix.md`).
- [x] Author the neutral vectors so every C-item and Channel vector has an evidence path
  ([`vectors/`](vectors/README.md), PB1).
- [x] Chain-conjunction representation choice recorded per stack (`representation-choice.md`) —
  **non-pinned interim**; transcription into the pinned delivery ledgers is deferred to the
  authorized repinning / fresh-review window.
- [x] Resolve the two encoding blockers — **deterministic CBOR core** (wire) and **copied immutable
  blob** (referenced-resource floor), recorded 2026-07-24 (see `open-decisions.md`).

## PB1 exit checklist (plan §5)

- [x] Data-only versioned contracts for every PB1 bullet: canonical references and the Shape floor;
  Component provisions and requirements; negotiated Operations, input/result/detail Shapes and
  required Fragments; authority-presentation mode and the cross-trust `no-capability-transfer`
  declaration; inline and referenced-shaped-resource declarations; delivery/hardening limits and
  lifecycle features; immutable Binding Plan facts; Channel envelopes, correlation, protocol errors
  and process-failure observations; and the C9 observation set
  ([`schemas/`](schemas/README.md), eight files).
- [x] Valid, additive-compatible, and adversarial fixtures with exact expected outcomes: 63 vectors
  covering C1–C10, all 24 Channel 0.1 vectors, every protocol-error and process-failure category, and
  every failure domain ([`vectors/`](vectors/README.md)).
- [x] Deterministic byte forms — six golden CBOR encodings plus seven encodings that must be
  rejected. The gate re-derives each golden value from its description rather than trusting the
  checked-in bytes.
- [x] Neutral layer free of generated stack source and runtime helpers; the gate fails on any file
  here that is not `.json` or `.md`.
- [x] Validated without loading either stack:
  [`build/verify-portable-binding.ps1`](../../build/verify-portable-binding.ps1), invoked by
  [`build/verify-interchange.ps1`](../../build/verify-interchange.ps1).

**Exit:** the artifacts are self-contained, deterministic, linkable from both implementations, and
validated without loading either stack.

## What PB1 deliberately left to later phases

PB1 fixes the contract; it does not implement it. Three findings recorded here are migration
obligations for **PB2** (Reference) and **PB3** (Minimal), each stated in the schema that creates it
rather than only in this index:

1. **Map key ordering** is RFC 8949 bytewise-on-encoded-key order, not the ordinal string comparison
   the Cooling JSON codec uses. `G1` in the golden encodings is the smallest case where the two
   disagree.
2. **Values are schema-guided** and carry no kind discriminator, unlike the retained
   `inline-tagged-json` representation.
3. **Denial is frameless.** The Cooling `denial` message kind must not become a portable envelope
   kind.

Reference discharged all three in PB2 and Minimal discharged the same three independently in PB3.

The C7 and C10 vectors carry a `phase` marker (PB4 or PB5) because they state an obligation a stack
harness discharges later. PB1 fixes what must be equal and what may differ; it does not execute the
comparison.

## What PB2 changed here

PB2 implemented the contract in the Reference stack and found three places where the fixture could
not satisfy its own vectors. Each was corrected in
[`vectors/fixture-contract.json`](vectors/fixture-contract.json) as data; no vector, schema, or
golden encoding changed:

1. The fixture required `interchange.tests.cooling-profile` at strength `required` while offering no
   matching provision, so PB-01's "every required requirement matches a provision" could not hold
   against the fixture negotiating with itself. The provision was added.
2. There was no choice Shape, so PB-15 had no declared alternative set to violate.
   `interchange.tests.encoding.choice@1` was added.
3. There was no Fragment declared by the contract but outside the negotiated Operation, so PB-13's
   closed-policy refusal could not be distinguished from an undeclared-Fragment refusal.
   `interchange.tests.cooling.note@1` was added, hosted by the closed result Shape.

## What PB3 changed here

Nothing. PB3 implemented the same contract in the Minimal stack and found no fixture, schema, vector,
or golden encoding that needed correcting, which is the first evidence that the PB2 corrections above
were fixture defects rather than contract ones. The gate now runs both stacks' native evidence after
validating the neutral layer.

## What PB4 changed here

Nothing again, and this time that is the more interesting result. PB4 executed the C7 parity
obligation these vectors state, over every portable result class a host can reach, and the four
divergences it found were both stacks reporting an endpoint-decided refusal in two failure domains
and an authority-bearing body under two categories. Both were implementation defects measured against
the parity profile in [`schemas/binding-observation.json`](schemas/binding-observation.json), which
already said which fields must be equal and which may differ and why. The contract needed no
amendment to adjudicate them.

The `phase` markers stay: PB-58 through PB-60 are now executed, and the remaining PB5 markers
(PB-61, PB-63) are still the obligations no single stack can discharge alone. The Channel accounting
each stack runs derives from the `channelVectors` declarations in these vector files, so a Channel
vector that loses its portable cover here fails both stacks' builds.

## What PB5 changed here

Two things, and both were found by doing something no earlier phase did: reading these files from
outside the two stacks.

1. **A Catalog fixture contract now exists.** PB1 declared only Cooling, so each stack authored its
   own Catalog fixture and the two drifted — different Operation names, and a disagreeing
   `providerSpecific` flag. Negotiation matches both exactly, so the stacks could not establish a
   Catalog binding at all, and nothing noticed while each ran Catalog only against itself.
   [`vectors/catalog-fixture-contract.json`](vectors/catalog-fixture-contract.json) is the single
   declaration both are now measured against, and both stacks moved to meet it.
2. **The fixtures now separate annotation from contract data.** They carry documentation alongside
   the contract — `additiveOver` on a Shape version, `role` on the encoding-edge Shapes — and
   [`schemas/component-contract.json`](schemas/component-contract.json) declares exactly which fields
   a contract document has, with `unknownFieldPolicy: reject`. A faithful transcode of these files
   was therefore a malformed contract. Both stacks had hand-written their contracts from these files
   and dropped the annotations by eye, so neither had ever discovered it. Each fixture now declares
   its own `annotationFields`, making the distinction data rather than a convention someone has to
   know, and forcing a future annotation to declare itself.

No schema, vector, or golden encoding changed.

## What PB7 changed here

PB7 added the first new artifacts since PB1: [`schemas/composition-handoff.json`](schemas/composition-handoff.json)
and [`vectors/composition-handoff.json`](vectors/composition-handoff.json), eleven vectors that take
the total to 74. `binding-plan.json` already carried a `compositionHandoff` stub naming the phase;
it now points at the schema that owns the seam.

The seam was declared here **before** either stack implemented it, which is Decision 10's Option C
applied in the cheapest available form. The declaration fixes what a resolved requirement and an
offered provision carry, the order preflight checks them in, the named stages and what each fixes,
the ordinary-interaction gate, and the replacement record — including two things a silent contract
would have left each stack to invent: that a preflight refusal is `frameDecision: none` with
`resultClass: protocol-error` (frameless like a denial, but a contract refusal rather than an
authority decision), and that a gate refusal reports `authorityDecision: unknown` because the gate
refuses before the authority boundary is reached.

PB7 also found that **negotiation never compares provider identity**, so the Binding Plan's
`provider` fact reports the required document's value — who the host asked for — rather than who
answered. Every fixture here derives from one declaration, so the two have always agreed and nothing
could have observed the difference. Decision 11 in [`open-decisions.md`](open-decisions.md) records
it with its options; no schema or vector changed for it, because the provisional fix is a check at
the composition seam rather than a contract change.

## Boundary

Nothing here changes either stack's Architecture 0.7 implementation target or asserts Architecture
0.8 conformance. This is planned experimental scaffolding; the reusable surface is refactored out
of the existing Cooling/Catalog experiments rather than replacing them.
