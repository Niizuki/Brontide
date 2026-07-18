# Brontide Reference Stack milestone evidence

This file records the retained evidence for Brontide Reference Stack Implementation Plan 0.2. It distinguishes
current behavioural evidence from historical process claims that cannot be reconstructed from the
repository.

The active cross-stack sequence is defined by
[`Brontide-Interchange-Implementation-Plan-0.1.md`](../../Brontide-Interchange-Implementation-Plan-0.1.md).
Brontide Reference Stack now retains an experimental, Brontide Reference Stack-owned host adapter and provider endpoint for the neutral
Cooling fixture. It exchanges process data only and does not alter the normative status of Brontide Reference Stack
Core, Architecture 0.5 Composition, or the proposed portable binding.

| Milestone | Retained evidence | Status |
| --- | --- | --- |
| M0 | Solution/dependency verifier; section-cited §29.2 and §29.4 tests | Functional gate green; the original failing-first observation was not retained in Git history |
| M1 | Core and conformance suites cover attenuation, fail-closed constraints, typed immutable scalar carriers, mortality, provenance, and origin checks | Green |
| M2 | `CoolingConformanceTests` exercises the complete headless Cooling scenario | Green |
| M3 | `ShapeConformanceTests` and `OutcomeConformanceTests` cover additive projection, fragments, forwarding, and result/details separation | Green |
| M4 | Experimental Enrichment suite covers local availability, conflict/missing-source failures, pure derivation, explicit store acquisition, and rejection of actual Capability payloads | Green experimental evidence |
| M5 | Studio inspector scene tests cover actors, capability trees, live Executions, and articulate denials | Green showcase evidence |
| M6 | Studio scene plus section-cited Origin conformance tests cover attachment, Device origin, masquerade denial, mortality, and unverified remote input | Green |
| M7 | Experimental extension test covers capability-gated publication/observation, fan-out, emitter preservation, and replay | Green provisional-extension evidence |
| M8 | Experimental extension test covers checked `Flow.Open`, independently authorised Item publication, `Flow.GapDetected`, checked replay, and derived replay origin; §15 conformance covers spoof resistance | Green provisional-extension evidence |
| M9 | Studio scene plus section-cited Outcome conformance cover delegated `Audit.Start`, activity creation, and later terminal completion | Green |
| Architecture 0.5 delta | Experimental composition tests cover explicit dependency strength, optional boxed boundaries, non-inferred accelerator eligibility, visible provider substitution, operational observations, vector execution, and fallback | Green experimental evidence; not represented as ratified Component, Binding Plan, system-service, or optimisation semantics |
| Interchange P0-P4 | Neutral fixtures; independent v2 manifest/value/message implementations; Brontide Reference Stack host adapter and provider endpoint; real two-way foreign-process tests; dependency/output audit | Green experimental cross-stack evidence; protocol and observation format remain unratified |

The M0 test-first requirement is a process gate. Brontide Reference Stack's source and tests originally arrived in one
commit, so the repository cannot prove that the expected failures were observed before their
implementations. This limitation is documented rather than represented as reconstructed evidence.

The Architecture 0.5 image workspace still supplies Brontide Reference Stack-local evidence only. A real Brontide Minimal Stack
Cooling Component now interchanges with Brontide Reference Stack, but the mixed image workspace and cross-machine or
cross-authority-domain binding remain outstanding. GPU execution is intentionally separate: it is a planned experimental
sideline project, not a required part of this milestone or a substitute for the current vector
evidence.

## First interchange gate

Phases P0-P4 are retained as executable evidence. Exact versions negotiate before invocation;
authority and unknown Constraints fail closed in the Brontide Reference Stack host; the required host-context
Fragment is locally enriched; optional authored data is canonically ignored and transparently
forwarded; failed semantic Outcomes and provider-process failures stay explicit; and no Capability,
exception, private CLR type, assembly, static state, or service container crosses the seam.

The next cross-stack gate is Event/Flow evidence, followed by Macro Operation exchange and the mixed
image workspace. The current result does not claim a machine boundary, Capability federation,
hot-swap, or ratified descriptor/protocol semantics.

The repeatable current verification is:

```powershell
dotnet restore .\Brontide.Reference.sln
dotnet build .\Brontide.Reference.sln --no-restore
dotnet test .\Brontide.Reference.sln --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\build\verify-dependencies.ps1
```

The complete cross-stack gate is `..\..\build\verify-interchange.ps1` from this directory, or
`.\build\verify-interchange.ps1` from the repository root.
