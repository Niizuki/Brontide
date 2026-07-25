# `binding/portable/schemas/`

Data-only versioned neutral contracts, authored in **PB1** (plan §5).

No generated C#/F# source and no runtime helpers live here. If schemas generate code, generation runs
separately in each stack and this checked-in neutral source stays authoritative. Every file validates
without loading either stack; [`build/verify-portable-binding.ps1`](../../../build/verify-portable-binding.ps1)
is the gate.

## Contents

| File | Plan §5 PB1 bullet | Capabilities |
| --- | --- | --- |
| [`references-and-shape-floor.json`](references-and-shape-floor.json) | canonical references and the supported Shape floor | C1, C5 |
| [`component-contract.json`](component-contract.json) | Component provisions and requirements; negotiated Operations, input/result/detail Shapes, and required Fragments | C1, C5 |
| [`authority-presentation.json`](authority-presentation.json) | authority-presentation mode and cross-trust `no-capability-transfer` declaration | C3 |
| [`payload-representation.json`](payload-representation.json) | inline representations and referenced-shaped-resource declarations | C5, C6 |
| [`limits-and-lifecycle.json`](limits-and-lifecycle.json) | delivery/hardening limits and lifecycle features | C8 |
| [`binding-plan.json`](binding-plan.json) | immutable Binding Plan facts | C2 |
| [`channel-envelope.json`](channel-envelope.json) | Channel envelopes, correlation, protocol errors, and process-failure observations | C4 |
| [`binding-observation.json`](binding-observation.json) | binding observations required by C9 | C9 |

## Decisions these schemas realize

- **Wire representation** — deterministic CBOR core, recorded 2026-07-24 (see
  [`open-decisions.md`](../open-decisions.md) Decision 1). Pinned exactly in
  `payload-representation.json`. Its `recordedDecisionRefinement` notes where the recorded shorthand
  ("major types 0-5 only") required refinement, since booleans and null are major type 7 simple
  values and the allowlisted `Decimal` tag is major type 6.
- **Referenced-resource floor** — copied immutable blob, recorded 2026-07-24 (Decision 2). Pinned in
  `payload-representation.json` alongside the retained Catalog addressing-only handle.
- **Chain-conjunction ceiling** — [`representation-choice.md`](../representation-choice.md) is the
  operative D3 record; `authority-presentation.json` carries its portable consequence (carried-tier
  by default) without capping either stack.

## Normalizations applied

PB0 recorded five cross-cutting findings that PB1 had to reconcile. Where the neutral contract
departs from an existing experiment, the schema says so in place rather than leaving the difference
for an implementer to discover:

1. **One reference encoding.** Structured `{name, version}` is canonical; `name@version` is a text
   rendering permitted only where a single string key is structurally required.
2. **One limit and lifecycle surface.** The tighter bound wins wherever Cooling and Catalog disagree,
   and lifecycle states are explicit rather than implicit in message ordering.
3. **One Binding Plan and one observation set**, replacing the scattered `binding` block, flat
   `resourceBoundary`/`payloadLimitBytes` fields, and ad-hoc observation fields.
4. **A general referenced-resource form**, with the Catalog addressing-only handle retained as one
   declared flavor rather than as the only shape a resource can take.
5. **Denial is frameless.** The Cooling `denial` message kind does not become a portable envelope
   kind; `channel-envelope.json` records the migration obligation for PB2 and PB3.

Two further normalizations follow from the CBOR decision and are called out because they are
migration work rather than documentation:

- **Map key ordering** is RFC 8949 bytewise-on-encoded-key order, which is not the ordinal string
  order the Cooling JSON codec uses today.
- **Values are schema-guided**, carrying no kind discriminator, unlike the retained
  `inline-tagged-json` representation whose values are self-describing.
