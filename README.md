# Brontide

Brontide is an architecture specification with two deliberately independent .NET 10 implementations:

- [Brontide Reference Stack](./Reference/README.md), the C#/Avalonia implementation and interactive showcase;
- [Brontide Minimal Stack](./Minimal/README.md), the F# implementation and headless counterpoint.

The current specification is [Brontide Architecture 0.5](./Brontide-Architecture-0.5.md). Both native
implementation gates are in place; the current programme is the first real cross-stack evidence:
[Reference/Minimal Interchange Implementation Plan 0.1](./Brontide-Interchange-Implementation-Plan-0.1.md).

That plan's first proof is implemented: two-way Cooling component interchange crosses real process
boundaries. It tests Brontide substitutability without sharing private CLR types or treating the
experimental binding protocol as ratified architecture. Run the retained gate with
`.\build\verify-interchange.ps1`.

Implementation-owned status and limitations are recorded in the
[Brontide Reference Stack milestone evidence](./Reference/docs/milestone-evidence.md) and
[Brontide Minimal Stack milestone evidence](./Minimal/docs/milestone-evidence.md).
