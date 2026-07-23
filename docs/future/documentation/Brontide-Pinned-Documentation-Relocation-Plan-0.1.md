# Brontide Pinned Documentation Relocation Plan 0.1

**Status:** Priority 0 future work; execute before other planned implementation work
**Date:** 2026-07-23
**Scope:** Documentation paths and evidence controls only; no architecture or implementation
semantics change

## 1. Purpose

Finish the root cleanup that cannot safely occur during ordinary documentation maintenance. Several
documents are directly pinned by the architecture status registry, conformance vectors, delivery
matrices, independent attestations, or closure record. Other root files are transitively pinned
because those immutable documents link to their exact paths.

Moving them without repinning would leave valid content behind broken navigation or would invalidate
the completed independent-review trail. This plan therefore runs as one deliberate evidence
migration with fresh review.

## 2. Target placement

Move the remaining repository-wide Markdown files as follows:

| Present stable-path material | Target classification |
| --- | --- |
| Architecture 0.7 and architecture change history | `docs/current/architecture/` |
| Architecture 0.8 | `docs/future/architecture/` |
| Architecture 0.7 and 0.8 change plans | `docs/archive/architecture/` |
| Composition, Channel, Enrichment, Persistent Information, and Topology design notes | `docs/future/<area>/` |
| Draft Channel Contract | `docs/future/channel/` |
| Architecture 0.8 Channel requirements and risk ledger | `docs/future/channel/` |
| Component Management implementation plan | `docs/future/component-management/` |
| Minimal and Reference 0.3 implementation plans | `<stack>/docs/future/` |
| Temporary current-architecture brief | `docs/temporary/` |
| Architecture 0.7 mediation ledger | `docs/archive/architecture/` |
| Implementation-correction status | `docs/archive/corrections/` |
| Public-boundaries evidence | `docs/current/policies/` |

After this migration, the repository root should contain only standard project-control files,
`README.md`, `AGENTS.md`, and `Brontide-Architecture-Status.json`.

## 3. Required migration sequence

1. Freeze a clean starting commit and inventory every direct and transitive path/hash reference.
2. Move all listed documents and repair Markdown links plus plain-text canonical path references.
3. Update the architecture status registry, conformance source records, stack delivery matrices,
   review request, and any generated evidence hashes.
4. Run text, link, vector, evidence, dependency, build, test, and cross-stack gates.
5. Commit the relocation as the new immutable review target.
6. Generate fresh Reference and Minimal review packets in isolated contexts. Reviewers confirm that
   the move changed no architecture or implementation semantics and that all evidence remains
   reachable.
7. Commit conforming attestations, regenerate the closure with their hashes, preserve the original
   user authorization for the already-deleted correction plan, and run the strict closure gate.
8. Update all documentation indexes and verify that no repository-wide design or plan document
   remains at the root.

## 4. Completion gate

The relocation is complete only when:

- no stale old path occurs in tracked documentation, JSON evidence, build comments, or review data;
- canonical content hashes change only where repaired links require it;
- the status registry and all delivery matrices verify;
- fresh independent reviews conform for both stacks;
- the strict independent-review closure check passes;
- the complete repository gate passes from a clean worktree; and
- `docs/README.md`, `docs/current/README.md`, `docs/future/README.md`,
  `docs/temporary/README.md`, and `docs/archive/README.md` agree.

## Open questions (owners needed)

| Owner | Question | Blocking point |
| --- | --- | --- |
| Repository maintainer | When should the evidence-repinning and fresh-review window be authorized? | Blocks execution of this Priority 0 plan; ordinary work must preserve the current stable paths. |

## Resolved questions

- **2026-07-23 — Priority:** complete this relocation before other planned implementation work once
  the required review window is authorized.
- **2026-07-23 — Interim behavior:** move safely unpinned documents now and retain direct or
  transitive pinned paths without root redirect stubs.
- **2026-07-23 — Taxonomy:** implemented/current material belongs in `docs/current`, planned or
  unimplemented material in `docs/future`, deletion-gated notes in `docs/temporary`, and completed
  or superseded material in `docs/archive`.
