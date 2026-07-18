# Brontide

Brontide is an architecture specification with two deliberately independent .NET 10 implementations:

- [Brontide Reference Stack](./Reference/README.md), the C#/Avalonia implementation and interactive showcase;
- [Brontide Minimal Stack](./Minimal/README.md), the F# implementation and headless counterpoint.

The implemented specification baseline is
[Brontide Architecture 0.5](./Brontide-Architecture-0.5.md). The current architecture-development
document is [Brontide Architecture 0.7](./Brontide-Architecture-0.7.md): its document edit is
complete, but it is not yet ratified or implemented. Architecture 0.7 delivery is planned
independently in the
[Reference Stack Implementation Plan 0.3](./Reference/Brontide-Reference-Stack-Implementation-Plan-0.3.md)
and [Minimal Stack Implementation Plan 0.3](./Brontide-Minimal-Stack-Implementation-Plan-0.3.md).

Known implementation and evidence gaps are controlled separately by the
[temporary implementation correction plan](./Brontide-Temporary-Implementation-Correction-Plan-0.1.md).
That file is a request for corrective work, not evidence that the work is implemented, and remains
until its explicit deletion gate is satisfied.

The first programme of real cross-stack evidence remains
[Reference/Minimal Interchange Implementation Plan 0.1](./Brontide-Interchange-Implementation-Plan-0.1.md).
Its first proof is implemented: two-way Cooling component interchange crosses real process
boundaries. It tests Brontide substitutability without sharing private CLR types or treating the
experimental binding protocol as ratified architecture. Run the retained gate with
`.\build\verify-interchange.ps1`.

Implementation-owned status and limitations are recorded in the
[Brontide Reference Stack milestone evidence](./Reference/docs/milestone-evidence.md) and
[Brontide Minimal Stack milestone evidence](./Minimal/docs/milestone-evidence.md).
