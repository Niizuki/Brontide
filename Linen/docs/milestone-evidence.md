# Linen milestone evidence

This document distinguishes implemented Linen behaviour from cross-stack experiments. Native tests
remain Linen-only evidence; the separately retained foreign-process tests are now actual
Fabric/Linen Cooling interchange evidence.

The active cross-stack sequence is defined by
[`Atlas-Interchange-Implementation-Plan-0.1.md`](../../Atlas-Interchange-Implementation-Plan-0.1.md).
It closes the M3/M4 prerequisites relevant to interchange, completes the external binding surface,
and executes M6 in both host directions. The protocol remains an experimental test instrument.

| Milestone | Status | Current evidence |
| --- | --- | --- |
| M0 — F# skeleton | Implemented foundation | 16 `.fsproj` projects in `Linen.slnx`; boundary guard; no `global.json`; no Fabric project reference. |
| M1 — pure Base kernel | Implemented foundation | Canonical names, actors, additive registries, constraints, capabilities, attenuation, fail-closed evaluation, immutable transitions, logical time, Outcomes, Events, provenance. |
| M2 — native Cooling | Implemented foundation | Pure two-way Cooling transition, ShapeValue representation, headless host output, native tests. The fuller sensor/controller capability narrative remains conformance work. |
| M3 — Shape composition | Implemented foundation | Additive versions, strict record validation, authored/open Fragments, explicit canonical and required-Fragment projection, retained Velocity/DirectionalVelocity conformance case. |
| M4 — targeted Enrichment | Experimental evidence | Explicit target/Fragment/source declaration, additive attachment, pointer-temperature scenario, provenance, and visible conflict/missing-source errors outside Model/Kernel. |
| M5 — external binding | Implemented experimental surface | Historical v1 seam plus independent v2 manifests, exact negotiation, tagged values, message envelopes, binding-scoped identifiers, private-type checks, observations, host adapter, and provider endpoint. |
| M6 — two-way interchange | Green experimental cross-stack evidence | Real Fabric-hosts-Linen and Linen-hosts-Fabric provider processes pass the same Cooling matrix without project or assembly references. |
| M7 — Events and Flow | Implemented foundation | Optimistic Event streams, deterministic folding, DAG validation, fan-out/fan-in Flow state machines. Cross-stack gap recovery remains. |
| M8 — Macro Operation | Not yet implemented | Activity lifecycle and cross-stack provider exchange remain. |
| M9 — 0.5 Composition | Implemented foundation | Four dependency-strength claims, support/opposition, deterministic provider selection, boxed values, CPU imaging, optimisation eligibility, explanations. |
| M10 — mixed-stack workspace | Deferred | Linen has independent multi-layer image workspace semantics. Fabric/Linen/third-provider substitution is the later entanglement proof. |

## Executable suites

- `Linen.Conformance`: Base behaviour and authority boundaries.
- `Linen.Kernel.Tests`: Cooling, Event streams, and Flow.
- `Linen.Enrichment.Tests`: isolated non-conformance Enrichment experiments.
- `Linen.Composition.Tests`: isolated Atlas 0.5 Composition and Imaging experiments.
- `Linen.Interchange.Tests`: historical seam, neutral fixture validation, and real Fabric provider
  process evidence, deliberately without Fabric project or assembly references.

## First interchange gate

Phases P0-P4 are retained as executable evidence. Linen's immutable authority path prevents denied,
unknown-Constraint, and missing-required-Fragment requests from reaching the provider effect. The
host independently enriches the required Fragment; provider results become Linen-native Outcomes;
semantic failure details, optional authored data, provenance, process failure, retry/fallback facts,
and explicit time remain inspectable.

The next cross-stack gate is Event/Flow evidence, followed by Macro Operation exchange and the mixed
image workspace. Machine boundaries, authority federation, hot-swap, and a ratified portable
descriptor/protocol remain deferred.
