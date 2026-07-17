# Linen milestone evidence

This document distinguishes implemented Linen behaviour from the cross-stack experiments that the
implementation plan intentionally postpones. Passing native tests are evidence for Linen; they are
not evidence that Fabric and Linen already interoperate.

| Milestone | Status | Current evidence |
| --- | --- | --- |
| M0 — F# skeleton | Implemented foundation | 15 `.fsproj` projects in `Linen.slnx`; boundary guard; no `global.json`; no Fabric project reference. |
| M1 — pure Base kernel | Implemented foundation | Canonical names, actors, additive registries, constraints, capabilities, attenuation, fail-closed evaluation, immutable transitions, logical time, Outcomes, Events, provenance. |
| M2 — native Cooling | Implemented foundation | Pure two-way Cooling transition, ShapeValue representation, headless host output, native tests. The fuller sensor/controller capability narrative remains conformance work. |
| M3 — Shape composition | Partial | Additive versions, strict record validation, authored/open Fragments, explicit projection. Velocity-specific conformance cases remain. |
| M4 — targeted Enrichment | Experimental foundation | Explicit provider registry, vocabulary selection, fragment attachment, claims, visible unsupported-provider errors. The §16.6 pointer-temperature scenario remains. |
| M5 — external binding | Linen side implemented | Versioned manifests, required contract negotiation, tagged JSON values, private-type boundary. A Fabric-owned endpoint is intentionally not referenced here. |
| M6 — two-way interchange | Deferred | This is the later entanglement experiment requested by the project owner. Current tests simulate an external runtime, not Fabric. |
| M7 — Events and Flow | Implemented foundation | Optimistic Event streams, deterministic folding, DAG validation, fan-out/fan-in Flow state machines. Cross-stack gap recovery remains. |
| M8 — Macro Operation | Not yet implemented | Activity lifecycle and cross-stack provider exchange remain. |
| M9 — 0.5 Composition | Implemented foundation | Four dependency-strength claims, support/opposition, deterministic provider selection, boxed values, CPU imaging, optimisation eligibility, explanations. |
| M10 — mixed-stack workspace | Deferred | Linen has independent multi-layer image workspace semantics. Fabric/Linen/third-provider substitution is the later entanglement proof. |

## Executable suites

- `Linen.Conformance`: Base behaviour and authority boundaries.
- `Linen.Kernel.Tests`: Cooling, Event streams, and Flow.
- `Linen.Enrichment.Tests`: isolated non-conformance Enrichment experiments.
- `Linen.Composition.Tests`: isolated Atlas 0.5 Composition and Imaging experiments.
- `Linen.Interchange.Tests`: external manifest/value seam, deliberately without Fabric references.
