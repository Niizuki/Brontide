# Brontide Minimal Stack milestone evidence

Designed for: [Brontide Architecture 0.7](../../Brontide-Architecture-0.7.md)

The mechanically checked source for Architecture 0.5 requirement status is
[`../conformance/architecture-0.5.json`](../conformance/architecture-0.5.json). This document is the
narrative summary; `build/verify-evidence.ps1` rejects missing, duplicate, stale, or unreferenced
requirement IDs and stale evidence anchors.

This document distinguishes implemented Brontide Minimal Stack behaviour from cross-stack
experiments. Native tests remain Minimal-only evidence; the separately retained foreign-process
tests are actual Reference/Minimal Cooling and Catalog/resource interchange evidence.

The older Architecture 0.5 entries are retained evidence, not a second implementation target.
Architecture 0.7 implementation detail is recorded in
[`architecture-0.7-delivery.md`](./architecture-0.7-delivery.md). No table entry below is, by itself,
evidence of complete 0.7 conformance. The permanent implementation-correction status records known
claim gaps and their independently reviewed closure state.

Architecture 0.7 evidence is checked separately through
[`../conformance/architecture-0.7.json`](../conformance/architecture-0.7.json). The current M1-M2
evidence covers recursive Constraint evaluation, target-side poisoning, experimental selection
exclusion, and provisional typed-member canonical names; it does not imply that the remaining
target is implemented.

The active cross-stack sequence is defined by
[`Brontide-Interchange-Implementation-Plan-0.1.md`](../../Brontide-Interchange-Implementation-Plan-0.1.md).
It closes the M3/M4 prerequisites relevant to interchange, completes the external binding surface,
and executes M6 in both host directions. Both protocols remain experimental test instruments.

| Milestone | Status | Current evidence |
| --- | --- | --- |
| M0 — F# skeleton | Implemented foundation | 17 `.fsproj` projects in `Brontide.Minimal.slnx`; boundary guard; no `global.json`; no Reference project reference. |
| M1 — pure Base kernel | Implemented foundation | Opaque issued references, canonical names, explicit target/presented Capability, provenance, constraints, narrowing delegation, fail-closed execution, trusted logical time, redacted audits, shaped Outcomes, attributed Events. |
| M2 — native Cooling | Implemented foundation | Pure two-way Cooling transition, ShapeValue representation, headless host output, native tests. The fuller sensor/controller capability narrative remains conformance work. |
| M3 — Shape composition | Implemented foundation | Additive versions, strict record validation, authored/open Fragments, explicit canonical and required-Fragment projection, retained Velocity/DirectionalVelocity conformance case. |
| M4 — targeted Enrichment | Experimental evidence | Explicit target/Fragment/source declaration, additive attachment, pointer-temperature scenario, provenance, and visible conflict/missing-source errors outside Model/Kernel. |
| M5 — external binding | Implemented experimental surface | Historical v1 seam, independent Cooling v2 and Catalog v1 protocols, exact negotiation, strict tagged/nested values, binding-scoped identifiers, private-type checks, replay and payload controls, host clients, and provider endpoints. |
| M6 — two-way interchange | Green experimental cross-stack evidence | Real Reference-hosts-Minimal and Minimal-hosts-Reference provider processes pass Cooling plus the two-Operation Catalog/resource matrix without project or assembly references. |
| M7 — Events and Flow | Implemented foundation | Optimistic Event streams, deterministic folding, DAG validation, fan-out/fan-in Flow state machines. Cross-stack gap recovery remains. |
| M8 — Macro Operation | Not yet implemented | Activity lifecycle and cross-stack provider exchange remain. |
| M9 — 0.5 Composition | Implemented foundation | Four dependency-strength claims, support/opposition, deterministic provider selection, boxed values, CPU imaging, optimisation eligibility, explanations. |
| M10 — mixed-stack workspace | Deferred | Minimal has independent multi-layer image workspace semantics. Reference/Minimal/third-provider substitution is the later entanglement proof. |
| Architecture 0.7 M1-M2 | Tested Complete Draft evidence | Model/Kernel three-state recursive Constraint evaluation, conformance denial/redaction, experimental Composition candidate exclusion, and opaque typed-member parse/format/compare/rejection vectors. C3-C5 remain planned. |

## Executable suites

- `Brontide.Minimal.Conformance`: Base behaviour and authority boundaries.
- `Brontide.Minimal.Kernel.Tests`: Cooling, Event streams, and Flow.
- `Brontide.Minimal.Enrichment.Tests`: isolated non-conformance Enrichment experiments.
- `Brontide.Minimal.Composition.Tests`: isolated Architecture 0.5 Composition and Imaging experiments.
- `Brontide.Minimal.Interchange.Tests`: historical seam, neutral fixture validation, adversarial vectors,
  and real Reference provider-process evidence without Reference project or assembly references.

## Interchange gates

Cooling phases P0-P4 remain executable evidence. Minimal's immutable authority path prevents denied,
unknown-Constraint, and missing-required-Fragment requests from reaching the provider effect. The
host independently enriches the required Fragment; provider results become Minimal-native Outcomes;
semantic failure details, optional authored data, provenance, process failure, retry/fallback facts,
and explicit time remain inspectable.

The correction breadth proof adds independently implemented Catalog batch upsert and lookup in one
provider session. It retains nested/repeated tags, returns explicit missing-item failures, refuses
an out-of-scope provider resource before mutation, rejects malformed/unknown/version-skew/replay
vectors, and caps each line at 65,536 UTF-8 bytes. `interchange/binding-measurements.json` records the
manual/generated source split and is recomputed by the full gate.

The next planned cross-stack gate is Event/Flow evidence, followed by Macro Operation exchange and
the mixed image workspace. Machine boundaries, authority federation, hot-swap, and a ratified
portable descriptor/protocol remain deferred.
