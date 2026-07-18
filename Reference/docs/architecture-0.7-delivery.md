# Brontide Reference Stack Architecture 0.7 delivery ledger

Status: Planned; no Architecture 0.7 conformance claim

Architecture source: [Brontide Architecture 0.7](../../Brontide-Architecture-0.7.md), Complete
Draft; implementation evidence pending; not ratified

Plan of record:
[Brontide Reference Stack Implementation Plan 0.3](../Brontide-Reference-Stack-Implementation-Plan-0.3.md)

Implemented evidence baseline:
[Architecture 0.5 conformance matrix](../conformance/architecture-0.5.json)

This ledger routes current Reference work to Architecture 0.7 without relabelling retained
Architecture 0.5 evidence. The
[temporary implementation correction plan](../../Brontide-Temporary-Implementation-Correction-Plan-0.1.md)
continues to control its own open gates and deletion conditions.

## Architecture 0.7 change coverage

| 0.7 change | Reference delivery target | Planned evidence | Status boundary |
| --- | --- | --- | --- |
| C1 — composite Constraint poisoning (§10.1, §18.1, §23, §29.2) | `Brontide.Reference.Core` for authority evaluation and `Brontide.Reference.Experimental.Composition` for candidate selection | Table-driven positive and negative vectors for nested `AllOf`, `AnyOf`, and `Not`, including every unknown-atom position and deterministic diagnostic categories | Planned in R1; no 0.7 requirement is currently claimed |
| C2 — typed member canonical names (§6.10, §22.4) | Core name value types plus serialization and external adapters at their owning seams | Parse/format/compare/round-trip vectors for every registered member kind; malformed and legacy-spelling rejection or an explicitly bounded migration alias | Planned in R2; public API and serialized-form changes require a breaking-change decision |
| C3 — static Attribute-constrained binding (§18.1) | `Brontide.Reference.Experimental.Composition` | One-time resolution, immutable effective values and provenance, restoration without reselection, and mutation/removal/tie/unsupported-constraint vectors | Planned experimental evidence in R3; not Brontide Base conformance |
| C4 — Router logical-endpoint guarantees (§18.2) | Proposed `Brontide.Reference.Experimental.PersistentInformation` boundary | Router-owned guarantee tests, including backing-Store changes and refusal of guarantees the Router cannot uphold | Planned experimental evidence in R4; deeper Router policy remains open |
| C5 — Dataset authority, identity, and concurrency (§12, §18.2, §21.1) | Proposed persistent-information experiment, separate from Core | Capability designation and denial, Dataset-record identity, identity-bearing Store roles, declared concurrency, and Genesis-versus-authorised-issuance evidence | Planned experimental evidence in R4; other persistent-information roles remain deferred |
| C6 — extraction and term-status registry (§7.1, §16.6, §18.1, §18.2) | Documentation, project classification, and evidence labels | Review that Enrichment, Composition, and Persistent Information remain outside Base and point to their companion design notes | Document-routing work; no runtime conformance requirement |
| C7 — editorial and authority-machinery clarification (§8 and cross-references) | Documentation and test-rationale review | Confirm implementation docs use the 0.7 terms and cite the correct authority boundary without moving minting, custody, or evaluation into a universal service | Documentation review; no invented runtime work |
| C8 — Mediation direction (§6.9, §18.2, §26.1 and Composition design note) | Requirement/risk ledger only | Record Selection, Distribution, and Arbitration implications without introducing a normative `Mediator` type | Direction only; must not be represented as ratified or implemented conformance |

## Evidence sequence

1. Complete R0 with stable Architecture 0.7 requirement IDs and failing vectors. Future 0.7
   evidence belongs in a distinct `conformance/architecture-0.7.json` matrix; the 0.5 matrix stays
   immutable evidence for the implemented baseline.
2. Deliver R1 through R4 in their owning Reference projects and nearest NUnit suites. Core or
   public semantic changes require the complete Reference suite and dependency guard.
3. Compare only data-level observations with Minimal after each stack has independent native
   vectors. Shared fixtures cannot contain semantic implementation logic.
4. Update this ledger, `milestone-evidence.md`, `implementation-findings.md`, and the experimental
   registry when a planned item gains accepted evidence or changes classification.
5. Keep claims at “Architecture 0.7 Complete Draft evidence” until ratification permits a stronger
   statement.

## Architecture 0.8 preparation

R6 tracks the three directions named by Architecture 0.7 for 0.8: the Portable Binding Shape floor,
Channel, and Flow conformance. That work is a requirements and risk ledger only; it does not expand
the Architecture 0.7 implementation claim or pre-ratify 0.8.
