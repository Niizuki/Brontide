# Atlas

Atlas is an architecture specification with two deliberately independent .NET 10 implementations:

- [Fabric](./Fabric/README.md), the C#/Avalonia implementation and interactive showcase;
- [Linen](./Linen/README.md), the F# implementation and headless counterpoint.

The current specification is [Atlas Architecture 0.5](./Atlas-Architecture-0.5.md). Both native
implementation gates are in place; the current programme is the first real cross-stack evidence:
[Fabric/Linen Interchange Implementation Plan 0.1](./Atlas-Interchange-Implementation-Plan-0.1.md).

That plan's first proof is implemented: two-way Cooling component interchange crosses real process
boundaries. It tests Atlas substitutability without sharing private CLR types or treating the
experimental binding protocol as ratified architecture. Run the retained gate with
`.\build\verify-interchange.ps1`.

Implementation-owned status and limitations are recorded in the
[Fabric milestone evidence](./Fabric/docs/milestone-evidence.md) and
[Linen milestone evidence](./Linen/docs/milestone-evidence.md).
