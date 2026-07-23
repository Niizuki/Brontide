# Brontide documentation map

This index is the authoritative classification of repository documentation. It separates material
that directs current work from short-lived execution notes and retained history without disturbing
the stable paths used by architecture hashes, conformance matrices, and independent-review records.

## Classification rules

- **Current** documents describe the architecture selected by the status registry, an
  implementation's stated target, an active design direction, or planned work that has not been
  completed or abandoned.
- **Temporary** documents coordinate a bounded programme and state their own deletion gate. They
  are not architecture and must not be cited as permanent completion evidence.
- **Archive** documents preserve superseded architecture, executed plans, and completed programmes.
  Architecture 0.5 and earlier material is grouped under `foundation`; later archives are grouped
  by area rather than by date.
- A document may remain at a historical root path when current pinned evidence links to that exact
  path. Its classification in this index still controls; location alone does not make it current.
- Implementation-owned documentation remains under `Reference/` or `Minimal/`. Repository-wide
  material belongs under `docs/`, except for stable-path documents listed below.

## Current

### Architecture and governance

- [`Brontide-Architecture-Status.json`](../Brontide-Architecture-Status.json) selects the current and
  latest ratified architecture; do not infer either from filenames.
- [`Brontide-Architecture-0.8.md`](../Brontide-Architecture-0.8.md) is the current complete draft,
  not a ratified architecture.
- [`Brontide-Architecture-0.7.md`](../Brontide-Architecture-0.7.md) remains the locally declared
  implementation target for both stacks.
- [`Brontide-Architecture-Change-History.md`](../Brontide-Architecture-Change-History.md) is the
  maintained cross-version history.
- [`Composition Without a Kernel`](./current/architecture/Brontide-Architecture-Composition-Without-a-Kernel.md)
  is a current proposed architecture document.

### Current implementation plans

- [`Portable Component Binding Implementation Plan 0.1`](./current/binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md)
  is the next cross-stack goal.
- [`Component Management Implementation Plan 0.1`](../Brontide-Component-Management-Implementation-Plan-0.1.md)
  remains planned experimental work. Its root path is retained because both pinned stack plans link
  to it.
- [`Minimal Stack Implementation Plan 0.3`](../Brontide-Minimal-Stack-Implementation-Plan-0.3.md)
  and [`Reference Stack Implementation Plan 0.3`](../Reference/Brontide-Reference-Stack-Implementation-Plan-0.3.md)
  are the current locally owned stack plans and are path-and-hash pinned.

### Current design and contract directions

- [`Composition`](../Brontide-Design-Note-Composition-0.1.md)
- [`Component Management and Distribution`](../Brontide-Design-Note-Component-Management-0.1.md)
- [`Channel`](../Brontide-Design-Note-Channel-0.1.md)
- [`Draft Channel Contract 0.1`](../Brontide-Draft-Channel-Contract-0.1.md)
- [`Enrichment`](../Brontide-Design-Note-Enrichment-0.1.md)
- [`Persistent Information`](../Brontide-Design-Note-Persistent-Information-0.1.md)
- [`Topology`](../Brontide-Design-Note-Topology-0.1.md)

These files remain at stable root paths because the current architecture and evidence set link to
them. They record non-ratified directions unless their own status says otherwise.

### Current implementation and evidence references

- [`public-boundaries.md`](./public-boundaries.md), [`module-boundaries.md`](./module-boundaries.md),
  [`public-api-rationale.md`](./public-api-rationale.md), and [`sdk-policy.md`](./sdk-policy.md)
  describe maintained repository boundaries and policy.
- [`architecture-0.8-channel-requirements-and-risk-ledger.md`](./architecture-0.8-channel-requirements-and-risk-ledger.md)
  tracks the current Channel evidence programme.
- [`implementation-correction-status.md`](./implementation-correction-status.md) is the permanent,
  evidence-pinned closure record for the completed correction programme.
- Stack-specific current documentation is indexed by
  [`Reference/README.md`](../Reference/README.md) and [`Minimal/README.md`](../Minimal/README.md).

## Temporary

[`Brontide-Temporary-Current-Architecture-Implementation-Brief-0.1.md`](../Brontide-Temporary-Current-Architecture-Implementation-Brief-0.1.md)
is the only active temporary document. Its root path remains because the pinned 0.3 stack plans
link to it. Delete it only when its own completion gate is satisfied and all lasting information has
moved into tests and current implementation-owned documentation.

The former temporary implementation-correction plan was deleted after its authorized closure gate.
Do not recreate it; use the permanent status and archived completion report instead.

## Archive

### Foundation: through Architecture 0.5

- [`Architecture 0.4`](./archive/foundation/Brontide-Architecture-0.4.md)
- [`Architecture 0.5`](./archive/foundation/Brontide-Architecture-0.5.md)
- [`Minimal Stack Implementation Plan 0.2`](./archive/foundation/Brontide-Minimal-Stack-Implementation-Plan-0.2.md)
- [`Reference Stack Implementation Plan 0.2`](./archive/foundation/Brontide-Reference-Stack-Implementation-Plan-0.2.md)

### Architecture

- [`Architecture 0.6`](./archive/architecture/Brontide-Architecture-0.6.md)
- [`Architecture 0.7 Change Plan`](../Brontide-Architecture-0.7-Change-Plan.md) is executed and
  archival, but remains at the root because the pinned 0.3 stack plans link to it.
- [`Architecture 0.8 Change Plan`](../Brontide-Architecture-0.8-Change-Plan.md) records the completed
  authoring programme and remains at its stable evidence path.
- [`architecture-0.7-mediation-risk-ledger.md`](./architecture-0.7-mediation-risk-ledger.md) is
  retained architecture evidence at a matrix-pinned path.

### Interchange

- [`Reference/Minimal Interchange Implementation Plan 0.1`](./archive/interchange/Brontide-Interchange-Implementation-Plan-0.1.md)
  is the implemented experimental programme and evidence index. The implementations and tests it
  records remain active evidence even though the plan itself is archival.

### Corrections

- [`Implementation correction completion report`](./archive/corrections/implementation-correction-completion-report.md)
  is the permanent narrative archive. Machine-checkable status remains at its pinned current path.

## Stable-path discipline

Before moving or rewriting a root-level architecture, plan, ledger, matrix, or correction record,
search the status registry, `conformance/`, both current stack plans, and both stack delivery
matrices for exact path or hash references. Update evidence deliberately only when the work itself
requires a new pinned target; documentation cleanup alone is not sufficient reason to invalidate a
closed evidence trail.
