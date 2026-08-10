# Current documentation

This directory contains implemented behavior, currently used implementation targets, and
operational repository policy. It does not contain planned implementation work.

## Entry points

- [Brontide: The Idea](./overview/Brontide-Introduction.md) is the readable overview.
- [Implementation module boundaries](./policies/module-boundaries.md) records the dependency
  boundaries enforced by the repository.
- [Public API rationale](./policies/public-api-rationale.md) records public-surface compatibility
  decisions.
- [.NET SDK support policy](./policies/sdk-policy.md) records the selected-SDK policy.
- [ADR — "Stack" becomes "Graph"](./policies/ADR-stack-becomes-graph.md) records the accepted
  terminology direction and the opportunistic execution policy that goes with it.
- [Agent feedback](./ai-feedback/README.md) is the channel for reporting friction with the rules in
  `AGENTS.md` as evidence rather than opinion, plus the open months' entries; swept months move to
  `docs/archive/ai-feedback/`.

The following current documents now live within this directory tree; the stack READMEs remain with
their implementations:

- [Architecture 0.8](./architecture/Brontide-Architecture-0.8.md), the locally declared Complete Draft
  implementation target for both stacks, not ratified;
- [Architecture 0.7](./architecture/Brontide-Architecture-0.7.md), retained historical compatibility
  evidence;
- [Architecture 0.8 D2 migration](./architecture/architecture-0.8-d2-breaking-migration.md) and
  [D3 migration](./architecture/architecture-0.8-d3-breaking-migration.md), retained current
  public-surface guidance;
- [architecture change history](./architecture/Brontide-Architecture-Change-History.md);
- [public boundaries](./policies/public-boundaries.md);
- [Reference documentation](../../Reference/README.md); and
- [Minimal documentation](../../Minimal/README.md).

Current means applicable or implemented, not ratified. Consult each document's status and the
executable evidence before making a conformance claim.
