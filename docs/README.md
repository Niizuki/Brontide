# Brontide documentation map

This index is the authoritative classification of repository documentation. It separates
implemented or operationally authoritative material from future work, short-lived execution notes,
and retained history. The Priority 0 relocation aligned every repository-wide document's location
with its classification and repinned every dependent evidence path.

## Classification rules

- **Current** documents describe implemented behavior, the architecture currently used as an
  implementation target, or an operationally authoritative repository policy.
- **Future** documents describe planned, draft, work-in-progress, proposed, or otherwise
  unimplemented work. “Current architecture” does not mean “currently implemented”; Architecture
  0.8 therefore remains a future implementation direction.
- **Temporary** documents coordinate a bounded programme and state their own deletion gate. They
  are not architecture and must not be cited as permanent completion evidence.
- **Archive** documents preserve superseded architecture, executed plans, and completed programmes.
  Architecture 0.5 and earlier material is grouped under `foundation`; later archives are grouped
  by area rather than by date.
- This index remains authoritative for classification. After the Priority 0 relocation no design,
  plan, ledger, or correction document remains at the repository root: repository-wide material
  lives under `docs/`, and implementation-owned documentation lives under `Reference/` or `Minimal/`.

## Current

### Architecture and governance

- [`Brontide-Architecture-Status.json`](../Brontide-Architecture-Status.json) selects the current and
  latest ratified architecture; do not infer either from filenames.
- [`Brontide-Architecture-0.7.md`](./current/architecture/Brontide-Architecture-0.7.md) remains the locally declared
  implementation target for both stacks.
- [`Brontide-Architecture-Change-History.md`](./current/architecture/Brontide-Architecture-Change-History.md) is the
  maintained cross-version history.

### Current implementation and evidence references

- [`Brontide: The Idea`](./current/overview/Brontide-Introduction.md) is the readable introduction.
- [`module-boundaries.md`](./current/policies/module-boundaries.md),
  [`public-api-rationale.md`](./current/policies/public-api-rationale.md), and
  [`sdk-policy.md`](./current/policies/sdk-policy.md) describe maintained repository policy.
- [`public-boundaries.md`](./current/policies/public-boundaries.md) is maintained boundary policy,
  now under `docs/current/policies/` with its conformance-matrix evidence repinned.
- Stack-specific current documentation is indexed by
  [`Reference/README.md`](../Reference/README.md) and [`Minimal/README.md`](../Minimal/README.md).

See [`current/README.md`](./current/README.md) for the compact current-material index.

## Future work

[`future/README.md`](./future/README.md) is the single entry point for planned and unimplemented
work. Its Priority 0 item, the
[`Pinned Documentation Relocation Plan`](./future/documentation/Brontide-Pinned-Documentation-Relocation-Plan-0.1.md),
has been executed: the previously pinned root and `docs/` documents now live in their classified
locations and every dependent path and hash pin has been repinned. Its confirming fresh independent
reviews and closure are the remaining step before the plan is archived.

The principal planned areas are:

- [`Portable Component Binding`](./future/binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md);
- [`Component Management`](./future/component-management/Brontide-Design-Note-Component-Management-0.1.md)
  and its [`implementation plan`](./future/component-management/Brontide-Component-Management-Implementation-Plan-0.1.md);
- [`Channel evidence`](./future/channel/architecture-0.8-channel-requirements-and-risk-ledger.md);
  and
- [`Composition Without a Kernel`](./future/architecture/Brontide-Architecture-Composition-Without-a-Kernel.md).

Architecture 0.8, the remaining design notes, the Channel contract, both 0.3 stack plans, and the
Component Management implementation plan are also future work. They now live under
`docs/future/<area>/` and each stack's `docs/future/`, with their evidence trails repinned.

## Temporary

[`Brontide-Temporary-Current-Architecture-Implementation-Brief-0.1.md`](./temporary/Brontide-Temporary-Current-Architecture-Implementation-Brief-0.1.md)
is the only active temporary document and now lives under `docs/temporary/`. Delete it only when its
own completion gate is satisfied and all lasting information has moved into tests and current
implementation-owned documentation.

The former temporary implementation-correction plan was deleted after its authorized closure gate.
Do not recreate it; use the permanent status and archived completion report instead.

See [`temporary/README.md`](./temporary/README.md) for the active temporary-material index.

## Archive

### Foundation: through Architecture 0.5

- [`Architecture 0.4`](./archive/foundation/Brontide-Architecture-0.4.md)
- [`Architecture 0.5`](./archive/foundation/Brontide-Architecture-0.5.md)
- [`Minimal Stack Implementation Plan 0.2`](./archive/foundation/Brontide-Minimal-Stack-Implementation-Plan-0.2.md)
- [`Reference Stack Implementation Plan 0.2`](./archive/foundation/Brontide-Reference-Stack-Implementation-Plan-0.2.md)

### Architecture

- [`Architecture 0.6`](./archive/architecture/Brontide-Architecture-0.6.md)
- [`Architecture 0.7 Change Plan`](./archive/architecture/Brontide-Architecture-0.7-Change-Plan.md) is the executed,
  archival change plan, now under `docs/archive/architecture/`.
- [`Architecture 0.8 Change Plan`](./archive/architecture/Brontide-Architecture-0.8-Change-Plan.md) records the completed
  authoring programme and is retained as pinned adversarial-vector evidence.
- [`architecture-0.7-mediation-risk-ledger.md`](./archive/architecture/architecture-0.7-mediation-risk-ledger.md) is
  retained architecture evidence with its conformance-matrix pins refreshed.

### Interchange

- [`Reference/Minimal Interchange Implementation Plan 0.1`](./archive/interchange/Brontide-Interchange-Implementation-Plan-0.1.md)
  is the implemented experimental programme and evidence index. The implementations and tests it
  records remain active evidence even though the plan itself is archival.

### Corrections

- [`Implementation correction completion report`](./archive/corrections/implementation-correction-completion-report.md)
  is the permanent narrative archive.
- [`implementation-correction-status.md`](./archive/corrections/implementation-correction-status.md) is the permanent,
  evidence-pinned closure record for the completed correction programme.

See [`archive/README.md`](./archive/README.md) for the compact archive index.

## Stable-path discipline

The Priority 0 relocation is the authorized migration that moved the last pinned documents off the
repository root and into these classified locations. The repinned `docs/` and `<stack>/docs/`
locations are now the stable evidence paths. Before moving or rewriting any architecture, plan,
design note, ledger, matrix, or correction record, search the status registry, `conformance/`, both
current stack plans, both stack delivery matrices, and pinned architecture text for exact or
transitive path references. Do not invalidate a closed evidence trail during ordinary cleanup;
perform any further move only with explicit authorization to repin and freshly review the evidence.
