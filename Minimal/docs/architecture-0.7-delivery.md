# Brontide Minimal Stack Architecture 0.7 implementation notes

Designed for: [Brontide Architecture 0.7](../../docs/current/architecture/Brontide-Architecture-0.7.md)

Status: M1-M2 tested; remaining work is partial; no Architecture 0.7 conformance or ratification claim

These notes record useful implementation detail against the stated target. The related
[implementation plan](./future/Brontide-Minimal-Stack-Implementation-Plan-0.3.md),
[requirements](../../conformance/architecture-0.7-requirements.json), and
[test matrix](../conformance/architecture-0.7.json) are supporting material rather than a formal
routing hierarchy. They do not relabel retained Architecture 0.5 evidence. The permanent
[implementation correction status](../../docs/archive/corrections/implementation-correction-status.md) records those
separate gates and their independently reviewed deletion outcome.

## Architecture 0.7 change coverage

| 0.7 change | Minimal delivery target | Planned evidence | Status boundary |
| --- | --- | --- | --- |
| C1 — composite Constraint poisoning (§10.1, §18.1, §23, §29.2) | `Brontide.Minimal.Model` for the result model, `Brontide.Minimal.Kernel` for authority evaluation, and `Brontide.Minimal.Experimental.Composition` for selection | Minimal-native vectors for nested `AllOf`, `AnyOf`, and `Not`, every unknown-atom position, authority denial, candidate exclusion, and deterministic diagnostic categories | M1 tested as Architecture 0.7 Complete Draft evidence; not a ratified conformance claim |
| C2 — typed member canonical names (§6.10, §22.4) | Opaque name types in Model plus codecs and external adapters at their owning seams | Parse/format/compare/round-trip vectors for the provisional examples and an arbitrary future member kind; malformed and version-suffix rejection | M2 tested as additive Complete Draft evidence; the provisional member-kind catalogue remains an open validated token and no wire consumer existed to migrate |
| C3 — static Attribute-constrained binding (§18.1) | `Brontide.Minimal.Experimental.Composition` | One-time resolution, immutable effective values and provenance, restoration without reselection, and mutation/removal/tie/unsupported-constraint vectors | Planned experimental evidence in M3; not Brontide Base conformance |
| C4 — Router logical-endpoint guarantees (§18.2) | Proposed `Brontide.Minimal.Experimental.PersistentInformation` boundary | Router-owned guarantee tests, including backing-Store changes and refusal of guarantees the Router cannot uphold | Planned experimental evidence in M4; deeper Router policy remains open |
| C5 — Dataset authority, identity, and concurrency (§12, §18.2, §21.1) | Proposed persistent-information experiment outside Model and Kernel | Capability designation and denial, Dataset-record identity, identity-bearing Store roles, declared concurrency, and Genesis-versus-authorised-issuance evidence | Planned experimental evidence in M4; other persistent-information roles remain deferred |
| C6 — extraction and term-status registry (§7.1, §16.6, §18.1, §18.2) | Documentation, project classification, and evidence labels | Review that Enrichment, Composition, and Persistent Information remain outside Base and point to their companion design notes | `non-runtime`: documentation/classification audit complete; no runtime conformance requirement |
| C7 — editorial and authority-machinery clarification (§8 and cross-references) | Documentation and test-rationale review | Confirm implementation docs use the 0.7 terms and preserve Minimal's explicit minting, custody, and target-side evaluation boundaries | `non-runtime`: authority-boundary audit complete; no invented runtime work |
| C8 — Mediation direction (§6.9, §18.2, §26.1 and Composition design note) | Requirement/risk ledger only | Record Selection, Distribution, and Arbitration implications without introducing a normative `Mediator` type | `non-runtime`: shared risk ledger complete; no ratified or implemented Mediation claim |

## M0 audit record

The audit reviewed commit `06e473e05c7e8b4ae1364db17b4884807f0a676c` against the registry-pinned
Architecture 0.7 path, Complete Draft status, and SHA-256. The checked current-draft inventory and
matrix split the observations into stable requirements and preserve predecessor links to retained
Architecture 0.5 evidence.

- C1-C5 were classified `missing` actions at the audited baseline. M1 and M2 now supply C1-C2's
  native implementation and positive/negative evidence. C3-C5 remain missing experimental
  actions. M2 is additive: existing concept names and wire contracts are unchanged.
- C6 is `non-runtime`: the experimental registry and delivery documentation keep Enrichment,
  Composition, and Persistent Information outside Base and retain companion-note routing.
- C7 is `non-runtime`: Genesis/World minting, opaque Model custody, and target-side `World.step`
  evaluation stay separate, and the process seam carries no Capability.
- C8 is `non-runtime`: the repository-wide
  [Mediation risk ledger](../../docs/archive/architecture/architecture-0.7-mediation-risk-ledger.md) records Selection,
  Distribution, Arbitration, authority topology, provenance, deputy, affinity, residue, and trust
  obligations without adding a `Mediator` participant.

## Evidence sequence

1. M0 established stable Architecture 0.7 requirement IDs and distinct current-draft matrices.
   The 0.5 matrix stays immutable evidence for the implemented baseline.
2. M1-M2 supply independently failing-first C1-C2 vectors and tested native behavior. Deliver M3
   through M4 in their owning F# projects and nearest NUnit suites. Model, Kernel, or
   public semantic changes require the complete Minimal suite and boundary guard.
3. Compare only data-level observations with Reference after each stack has independent native
   vectors. Shared fixtures cannot contain semantic implementation logic.
4. Update this ledger, `milestone-evidence.md`, `implementation-findings.md`, and the experimental
   registry when a planned item gains accepted evidence or changes classification.
5. Keep claims at “Architecture 0.7 Complete Draft evidence” until ratification permits a stronger
   statement.

## M1 evidence record

Minimal models recursive Constraint expressions and three-state results natively in Model while
keeping the existing flat requirements as atomic compatibility leaves. Kernel evaluates the full
Capability expression chain before dispatch, retains opaque issuer-controlled references, sorts
unsupported names deterministically, and records redacted denials. Experimental Composition
selection excludes an indeterminate provider without referencing Reference code or types.

The accepted anchors are `BR_07_CONSTRAINT_001` in Kernel tests,
`BR_07_CONSTRAINT_002` in conformance tests, and `BR_07_CONSTRAINT_003` in Composition tests. The
checked mapping remains in
[`../conformance/architecture-0.7.json`](../conformance/architecture-0.7.json).

## M2 evidence record

Minimal keeps concept identity in opaque `CanonicalName` values and models the provisional
typed-member suffix as distinct opaque `CanonicalMemberName`, `MemberKind`, and `MemberName`
values. The parser accepts exactly `[AuthorityPath ":"] ConceptPath "#" MemberKind "." MemberName`,
keeps versions outside identity, compares ordinally, and rejects ambiguous or extra-delimiter
forms. `MemberKind` remains an open validated token because Architecture 0.7 explicitly leaves the
catalogue and final glyph provisional. No existing codec or external adapter carried typed-member
text, so M2 is additive and has no legacy wire spelling to accept or emit.

The accepted positive and negative anchors are in `Brontide.Minimal.Conformance`; the checked
mapping remains in [`../conformance/architecture-0.7.json`](../conformance/architecture-0.7.json).
This is Complete Draft evidence, not canonical-name ratification.

## Architecture 0.8 preparation

M6 originally tracked the three directions named by Architecture 0.7 for 0.8. Architecture 0.8 is
now the current architecture as a complete draft: it executes change plan C1-C14
with authored adversarial vectors
([`architecture-0.8-adversarial-vectors.json`](../../conformance/architecture-0.8-adversarial-vectors.json))
and decides the evidence order Channel, then Portable Binding and Shape floor, then Flow
conformance. M6 therefore prepares its requirements and risk ledger against that document. Two 0.8
facts already touch recorded Minimal evidence without changing it:

- 0.8 C7 supersedes the delivered 0.7 composite-poisoning rule with structural three-valued
  evaluation, so the tested `BR-07-CONSTRAINT-001/-002/-003` behavior becomes `conflicting` rework
  in a future 0.8 delivery audit. The M1 record above remains valid Architecture 0.7 Complete
  Draft evidence and is not relabelled.
- 0.8 §11 makes the chain-conjunction representation choice the revocation ceiling. M6 must record
  Minimal's representation choice here before the Portable Binding freezes one.

The recorded 0.8 composition, Component-management, minimum-topology, and trust-admission
directions remain non-normative; their experimental coverage belongs to the
[Component Management Implementation Plan 0.1](../../docs/future/component-management/Brontide-Component-Management-Implementation-Plan-0.1.md),
not to this ledger. That work is a requirements and risk ledger only; it does not expand the
Architecture 0.7 implementation claim or pre-ratify 0.8.
