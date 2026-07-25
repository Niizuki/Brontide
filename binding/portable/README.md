# Portable Component Binding — neutral contract artifacts (`binding/portable/`)

**Status:** PB0 scaffold — planned experimental work; not ratified; not part of Brontide Base.
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
| `open-decisions.md` | The two open owner decisions (wire representation; referenced-resource floor) with option sets and recommendations | PB0 |
| `schemas/` | Data-only versioned neutral contracts (references, Shape floor, plans, envelopes) | PB1 |
| `vectors/` | Valid, additive-compatible, and adversarial fixtures with expected outcomes | PB1 |

## PB0 exit checklist (plan §5)

- [x] Inventory the existing Cooling/Catalog surface and map each field, message kind, value
  variant, correlation identity, error code, limit, resource rule, and observation to C1–C10 with an
  owner, classification, and expected category-level observation (`contract-matrix.md`).
- [ ] Author the neutral vectors so every C-item and Channel vector has an evidence path (PB1).
- [x] Chain-conjunction representation choice recorded per stack (`representation-choice.md`) —
  **non-pinned interim**; transcription into the pinned delivery ledgers is deferred to the
  authorized repinning / fresh-review window.
- [x] Resolve the two encoding blockers — **deterministic CBOR core** (wire) and **copied immutable
  blob** (referenced-resource floor), recorded 2026-07-24 (see `open-decisions.md`).

## Boundary

Nothing here changes either stack's Architecture 0.7 implementation target or asserts Architecture
0.8 conformance. This is planned experimental scaffolding; the reusable surface is refactored out
of the existing Cooling/Catalog experiments rather than replacing them.
