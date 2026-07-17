# Fabric milestone evidence

This file records the retained evidence for Fabric Implementation Plan 0.2. It distinguishes
current behavioural evidence from historical process claims that cannot be reconstructed from the
repository.

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

The M0 test-first requirement is a process gate. Fabric's source and tests originally arrived in one
commit, so the repository cannot prove that the expected failures were observed before their
implementations. This limitation is documented rather than represented as reconstructed evidence.

The Architecture 0.5 image workspace supplies Fabric-local evidence only. A real Linen Component
and cross-machine or cross-authority-domain binding are not present and remain outstanding
cross-stack evidence. GPU execution is intentionally separate: it is a planned experimental
sideline project, not a required part of this milestone or a substitute for the current vector
evidence.

The repeatable current verification is:

```powershell
dotnet restore .\Fabric.sln
dotnet build .\Fabric.sln --no-restore
dotnet test .\Fabric.sln --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\build\verify-dependencies.ps1
```
