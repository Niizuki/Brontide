# Brontide Reference Stack Architecture 0.7 implementation notes

Designed for: [Brontide Architecture 0.7](../../Brontide-Architecture-0.7.md)

Status: R1-R2 tested; remaining work is partial; no Architecture 0.7 conformance or ratification claim

These notes record useful implementation detail against the stated target. The related
[implementation plan](../Brontide-Reference-Stack-Implementation-Plan-0.3.md),
[requirements](../../conformance/architecture-0.7-requirements.json), and
[test matrix](../conformance/architecture-0.7.json) are supporting material rather than a formal
routing hierarchy. They do not relabel retained Architecture 0.5 evidence. The permanent
[implementation correction status](../../docs/implementation-correction-status.md) records those
separate gates and their independently reviewed deletion outcome.

## Architecture 0.7 change coverage

| 0.7 change | Reference delivery target | Planned evidence | Status boundary |
| --- | --- | --- | --- |
| C1 — composite Constraint poisoning (§10.1, §18.1, §23, §29.2) | `Brontide.Reference.Core` for authority evaluation and `Brontide.Reference.Experimental.Composition` for candidate selection | Table-driven positive and negative vectors for nested `AllOf`, `AnyOf`, and `Not`, including every unknown-atom position and deterministic diagnostic categories | R1 tested as Architecture 0.7 Complete Draft evidence; not a ratified conformance claim |
| C2 — typed member canonical names (§6.10, §22.4) | Core name value types plus serialization and external adapters at their owning seams | Parse/format/compare/round-trip vectors for the provisional examples and an arbitrary future member kind; malformed and version-suffix rejection | R2 tested as additive Complete Draft evidence; the provisional member-kind catalogue remains an open validated token and no wire consumer existed to migrate |
| C3 — static Attribute-constrained binding (§18.1) | `Brontide.Reference.Experimental.Composition` | One-time resolution, immutable effective values and provenance, restoration without reselection, and mutation/removal/tie/unsupported-constraint vectors | Planned experimental evidence in R3; not Brontide Base conformance |
| C4 — Router logical-endpoint guarantees (§18.2) | Proposed `Brontide.Reference.Experimental.PersistentInformation` boundary | Router-owned guarantee tests, including backing-Store changes and refusal of guarantees the Router cannot uphold | Planned experimental evidence in R4; deeper Router policy remains open |
| C5 — Dataset authority, identity, and concurrency (§12, §18.2, §21.1) | Proposed persistent-information experiment, separate from Core | Capability designation and denial, Dataset-record identity, identity-bearing Store roles, declared concurrency, and Genesis-versus-authorised-issuance evidence | Planned experimental evidence in R4; other persistent-information roles remain deferred |
| C6 — extraction and term-status registry (§7.1, §16.6, §18.1, §18.2) | Documentation, project classification, and evidence labels | Review that Enrichment, Composition, and Persistent Information remain outside Base and point to their companion design notes | `non-runtime`: documentation/classification audit complete; no runtime conformance requirement |
| C7 — editorial and authority-machinery clarification (§8 and cross-references) | Documentation and test-rationale review | Confirm implementation docs use the 0.7 terms and cite the correct authority boundary without moving minting, custody, or evaluation into a universal service | `non-runtime`: authority-boundary audit complete; no invented runtime work |
| C8 — Mediation direction (§6.9, §18.2, §26.1 and Composition design note) | Requirement/risk ledger only | Record Selection, Distribution, and Arbitration implications without introducing a normative `Mediator` type | `non-runtime`: shared risk ledger complete; no ratified or implemented Mediation claim |

## R0 audit record

The audit reviewed commit `06e473e05c7e8b4ae1364db17b4884807f0a676c` against the registry-pinned
Architecture 0.7 path, Complete Draft status, and SHA-256. The checked current-draft inventory and
matrix split the observations into stable requirements and preserve predecessor links to retained
Architecture 0.5 evidence.

- C1-C5 were classified `missing` actions at the audited baseline. R1 and R2 now supply C1-C2's
  native implementation and positive/negative evidence. C3-C5 remain missing experimental
  actions. R2 is additive: existing concept names and wire contracts are unchanged.
- C6 is `non-runtime`: the experimental registry and delivery documentation keep Enrichment,
  Composition, and Persistent Information outside Base and retain companion-note routing.
- C7 is `non-runtime`: `AuthorityDomain` remains the target-side evaluator, domain issuance and
  custody remain separate implementation responsibilities, and the process seam carries no
  Capability.
- C8 is `non-runtime`: the repository-wide
  [Mediation risk ledger](../../docs/architecture-0.7-mediation-risk-ledger.md) records Selection,
  Distribution, Arbitration, authority topology, provenance, deputy, affinity, residue, and trust
  obligations without adding a `Mediator` participant.

## Evidence sequence

1. R0 established stable Architecture 0.7 requirement IDs and distinct current-draft matrices.
   The 0.5 matrix stays immutable evidence for the implemented baseline.
2. R1-R2 supply independently failing-first C1-C2 vectors and tested native behavior. Deliver R3
   through R4 in their owning Reference projects and nearest NUnit suites. Core or
   public semantic changes require the complete Reference suite and dependency guard.
3. Compare only data-level observations with Minimal after each stack has independent native
   vectors. Shared fixtures cannot contain semantic implementation logic.
4. Update this ledger, `milestone-evidence.md`, `implementation-findings.md`, and the experimental
   registry when a planned item gains accepted evidence or changes classification.
5. Keep claims at “Architecture 0.7 Complete Draft evidence” until ratification permits a stronger
   statement.

## R1 evidence record

Reference models atomic Constraints as compatibility leaves beneath recursive `AllOf`, `AnyOf`,
and `Not` expressions. The pure evaluator retains satisfied, unsatisfied, and indeterminate states,
evaluates every sibling, normalizes unsupported names ordinally, and fails closed for an unknown
expression node. `AuthorityDomain` evaluates the full Capability chain before dispatch and records
redacted deterministic denial details. Experimental Composition selection excludes an
indeterminate candidate.

The accepted anchors are `BR_07_CONSTRAINT_001` in Core tests,
`BR_07_CONSTRAINT_002` in conformance tests, and `BR_07_CONSTRAINT_003` in the Studio-owned
experimental Composition suite. The checked mapping remains in
[`../conformance/architecture-0.7.json`](../conformance/architecture-0.7.json).

## R2 evidence record

Reference keeps concept identity in `CanonicalName` and models the provisional typed-member suffix
as the distinct `CanonicalMemberName`, `MemberKind`, and `MemberName` value types. The parser accepts
exactly `[AuthorityPath ":"] ConceptPath "#" MemberKind "." MemberName`, keeps versions outside
identity, compares ordinally, and rejects ambiguous or extra-delimiter forms. `MemberKind` is an
open validated token because Architecture 0.7 explicitly leaves the catalogue and final glyph
provisional. No existing serializer or public adapter carried typed-member text, so R2 is additive
and has no legacy wire spelling to accept or emit.

The accepted positive and negative anchors are in `CanonicalMemberNameTests`; the checked mapping
remains in [`../conformance/architecture-0.7.json`](../conformance/architecture-0.7.json). This is
Complete Draft evidence, not canonical-name ratification.

## Architecture 0.8 preparation

R6 originally tracked the three directions named by Architecture 0.7 for 0.8. Architecture 0.8 is
now the current architecture as a complete draft: it executes change plan C1-C14
with authored adversarial vectors
([`architecture-0.8-adversarial-vectors.json`](../../conformance/architecture-0.8-adversarial-vectors.json))
and decides the evidence order Channel, then Portable Binding and Shape floor, then Flow
conformance. R6 therefore prepares its requirements and risk ledger against that document. Two 0.8
facts already touch recorded Reference evidence without changing it:

- 0.8 C7 supersedes the delivered 0.7 composite-poisoning rule with structural three-valued
  evaluation, so the tested `BR-07-CONSTRAINT-001/-002/-003` behavior becomes `conflicting` rework
  in a future 0.8 delivery audit. The R1 record above remains valid Architecture 0.7 Complete
  Draft evidence and is not relabelled.
- 0.8 §11 makes the chain-conjunction representation choice the revocation ceiling. R6 must record
  Reference's representation choice here before the Portable Binding freezes one.

The recorded 0.8 composition, Component-management, minimum-topology, and trust-admission
directions remain non-normative; their experimental coverage belongs to the
[Component Management Implementation Plan 0.1](../../Brontide-Component-Management-Implementation-Plan-0.1.md),
not to this ledger. That work is a requirements and risk ledger only; it does not expand the
Architecture 0.7 implementation claim or pre-ratify 0.8.
