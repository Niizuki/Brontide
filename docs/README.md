# Brontide documentation map

This index is the authoritative classification of repository documentation. It separates
implemented or operationally authoritative material from future work, short-lived execution notes,
and retained history. The Priority 0 relocation aligned every repository-wide document's location
with its classification and repinned every dependent evidence path.

## Classification rules

- **Current** documents describe implemented behavior, the architecture currently used as an
  implementation target, or an operationally authoritative repository policy.
- **Future** documents describe planned, draft, work-in-progress, proposed, or otherwise
  unimplemented work. The hash-pinned pre-implementation Architecture 0.8 snapshot remains at its
  former future path only as closed review evidence; the implemented current copy is under `current`.
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
- [`Brontide-Architecture-0.8.md`](./current/architecture/Brontide-Architecture-0.8.md) is the locally
  declared implementation target for both stacks. Its Complete Draft implementation evidence does
  not constitute ratification.
- [`Brontide-Architecture-0.7.md`](./current/architecture/Brontide-Architecture-0.7.md) is retained
  historical compatibility evidence.
- [`Brontide-Architecture-Change-History.md`](./current/architecture/Brontide-Architecture-Change-History.md) is the
  maintained cross-version history.

### Current implementation and evidence references

- [`Brontide: The Idea`](./current/overview/Brontide-Introduction.md) is the readable introduction.
- [`module-boundaries.md`](./current/policies/module-boundaries.md),
  [`public-api-rationale.md`](./current/policies/public-api-rationale.md), and
  [`sdk-policy.md`](./current/policies/sdk-policy.md) describe maintained repository policy.
- [`ADR-stack-becomes-graph.md`](./current/policies/ADR-stack-becomes-graph.md) records the accepted
  rename of "Stack" to "Graph" and why its execution is deferred and opportunistic.
- [`public-boundaries.md`](./current/policies/public-boundaries.md) is maintained boundary policy,
  now under `docs/current/policies/` with its conformance-matrix evidence repinned.
- [`br-07-binding-001-contract.md`](../conformance/br-07-binding-001-contract.md) is the shared
  behavioural contract behind Architecture 0.7 change C3, read by both stacks' native
  Attribute-constrained binding evidence.
- [`ai-feedback/`](./current/ai-feedback/README.md) carries the agent-feedback convention and the
  open months' entries — friction with the rules in `AGENTS.md`, reported as evidence rather than
  opinion. It is not a fifth classification: the convention is operational policy and an unswept
  month is live, so both sit under `current`, and a month whose report records the disposition of
  each entry moves to `docs/archive/ai-feedback/`.
- Stack-specific current documentation is indexed by
  [`Reference/README.md`](../Reference/README.md) and [`Minimal/README.md`](../Minimal/README.md).

See [`current/README.md`](./current/README.md) for the compact current-material index.

## Future work

[`future/README.md`](./future/README.md) is the single entry point for planned and unimplemented
work. Its former Priority 0 item, the
[`Pinned Documentation Relocation Plan`](./archive/documentation/Brontide-Pinned-Documentation-Relocation-Plan-0.1.md),
is complete and archived: the previously pinned root and `docs/` documents now live in their
classified locations, every dependent path and hash pin has been repinned, and fresh independent
reviews and an authorized closure confirm the move changed no architecture or implementation
semantics. [`Portable Component Binding`](./future/binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md)
records the completed experimental predecessor. The
[`Channel 0.2 Redesign and Migration Plan`](./future/channel/Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md)
is now the head of planned work.

The principal planned areas are:

- [`Channel 0.2 redesign and migration`](./future/channel/README.md), with its first-batch design
  foundation complete, its four owner rulings resolved, and a fresh independent closure re-review
  outstanding;
- completed experimental [`Portable Component Binding 0.1`](./future/binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md);
- [`Component Management`](./future/component-management/Brontide-Design-Note-Component-Management-0.1.md)
  and its completed, evidence-path-retained
  [`implementation plan`](./future/component-management/Brontide-Component-Management-Implementation-Plan-0.1.md);
- [`Channel evidence`](./future/channel/architecture-0.8-channel-requirements-and-risk-ledger.md);
  and
- [`Composition Without a Kernel`](./future/architecture/Brontide-Architecture-Composition-Without-a-Kernel.md).

Architecture 0.8, the remaining design notes, the Channel contract, and both 0.3 stack plans are
future work. The Component Management implementation programme is complete, but its plan remains
under `docs/future/component-management/` because transitive evidence pins make that path stable;
moving it requires explicit authorization to repin and independently review the affected evidence.

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
