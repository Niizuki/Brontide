# Brontide

Brontide is an architecture specification with two deliberately independent .NET 10 implementations:

- [Brontide Reference Stack](./Reference/README.md), the C#/Avalonia implementation and interactive showcase;
- [Brontide Minimal Stack](./Minimal/README.md), the F# implementation and headless counterpoint.

The current architecture source is
[Brontide Architecture 0.7](./Brontide-Architecture-0.7.md). Its document edit is complete, but it
is not yet ratified or implemented. The executable conformance baseline remains the historical
[Brontide Architecture 0.5](./Brontide-Architecture-0.5.md); evidence documents and matrices pinned
to 0.5 describe that implemented baseline and are not the source for new architectural design.

Architecture 0.7 delivery is planned independently in the
[Reference Stack Implementation Plan 0.3](./Reference/Brontide-Reference-Stack-Implementation-Plan-0.3.md)
and [Minimal Stack Implementation Plan 0.3](./Brontide-Minimal-Stack-Implementation-Plan-0.3.md).
The implementation-owned delivery ledgers make the complete C1-C8 routing explicit for
[Reference](./Reference/docs/architecture-0.7-delivery.md) and
[Minimal](./Minimal/docs/architecture-0.7-delivery.md), including the non-runtime and non-ratified
items.

Known implementation and evidence gaps are controlled separately by the
[temporary implementation correction plan](./Brontide-Temporary-Implementation-Correction-Plan-0.1.md).
That file is a request for corrective work, not evidence that the work is implemented, and remains
until its explicit deletion gate is satisfied.

The first programme of real cross-stack evidence remains
[Reference/Minimal Interchange Implementation Plan 0.1](./Brontide-Interchange-Implementation-Plan-0.1.md).
Its first two experimental proofs are implemented: two-way Cooling component interchange and a
materially different, resource-scoped Catalog interchange both cross real process boundaries. The
Catalog proof adds nested/repeated values, two Operations in one provider session, explicit failure,
resource refusal, replay detection, strict message variants, version skew, and a 65,536-byte line
limit. They test Brontide substitutability without sharing private CLR types or treating either
experimental binding protocol as ratified architecture. Run the retained gate with
`.\build\verify-interchange.ps1`.

Exact boundary assumptions are recorded in
[`docs/public-boundaries.md`](./docs/public-boundaries.md), and the reproducible manual/generated
source-cost inventory is [`interchange/binding-measurements.json`](./interchange/binding-measurements.json).
The current correction finding/deletion-gate status is summarized in
[`docs/implementation-correction-status.md`](./docs/implementation-correction-status.md).

Implementation-owned status and limitations are recorded in the
[Brontide Reference Stack milestone evidence](./Reference/docs/milestone-evidence.md) and
[Brontide Minimal Stack milestone evidence](./Minimal/docs/milestone-evidence.md).

Stable Architecture 0.5 requirement IDs live in
[`conformance/requirements.json`](./conformance/requirements.json). The checked per-stack matrices
are [`Reference/conformance/architecture-0.5.json`](./Reference/conformance/architecture-0.5.json)
and [`Minimal/conformance/architecture-0.5.json`](./Minimal/conformance/architecture-0.5.json).
These files remain deliberately version-pinned evidence. Architecture 0.7 requirement IDs and
matrices are plan deliverables and must be created separately rather than rewriting the 0.5 record.
