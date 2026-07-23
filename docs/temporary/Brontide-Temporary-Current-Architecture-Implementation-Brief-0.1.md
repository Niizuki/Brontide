# Brontide Temporary Current-Architecture Implementation Brief 0.1

Date: 2026-07-19
Status: Temporary execution brief
Designed for: Brontide Architecture 0.7
Authority: Non-normative implementation notes.

## 1. Purpose and lifetime

This is a self-contained implementation brief for Architecture 0.7 work. Use
`Brontide-Architecture-Status.json` only to locate the current architecture; use each stack README's
local `Designed for` statement for its implementation target.

This brief aligns the action meanings used while auditing the current architecture changes and
then directs the necessary Reference and Minimal work. It does not ratify the current architecture,
upgrade an implementation claim, or supersede either stack's notes. Delete this file only after the
completion gate in section 11 is satisfied and all lasting information has moved into tests and
concise implementation-owned documentation.

The separate correction programme retains its own authority and deletion gate while its temporary
plan is present; afterward, the permanent correction status and review records preserve the closure.
Its independent attestations and closure may be completed before or in parallel with the
failing-vector work below, but current-architecture implementation must not be claimed until that
correction gate permits it.

The C1-C8 items below are the Architecture 0.7 change set described by the two 0.3 implementation
notes. The current architecture has meanwhile advanced to the Architecture 0.8 complete
draft, which supersedes one delivered 0.7 rule: 0.8 C7 replaces composite-expression poisoning
with structural three-valued evaluation. Audit and deliver C1-C8 against the Architecture 0.7 text
they came from; record each such supersession in the stacks' Architecture 0.8 handoff phases
(M6/R6) as future rework rather than silently projecting 0.8 semantics into 0.7 evidence or
relabelling 0.7 evidence as 0.8 conformance.

## 2. Instructions to Codex

1. Read `AGENTS.md`, the current architecture, and each stack's local implementation target.
2. Read the relevant implementation and companion design notes for the work in scope.
3. Preserve the complete independence of the C# Reference and F# Minimal implementations. Derive
   each native implementation from the architecture text. Share only versioned data contracts,
   external manifests, process protocols, and data-only comparison vectors.
4. Audit before editing production code. Record direct evidence or a concrete work disposition for
   every C1-C8 item in the relevant implementation notes.
5. Write native failing tests before each missing or conflicting semantic implementation. A test
   that passes before the intended change is not a failing vector.
6. Keep C3-C5 in explicitly experimental projects. They do not enlarge Brontide Base and do not
   become normative conformance merely because executable evidence exists.
7. Treat public API and serialized-form changes as breaking-change decisions. Record affected
   consumers and migration, update the owning changelog and component version where applicable,
   and use the repository's required breaking-change commit/PR marker when publishing such a
   change.
8. Update the owning README, tests, and relevant implementation notes as behaviour changes. Retained
   detailed matrices may be updated when useful, but do not create a second target declaration.
9. Run the nearest native tests while iterating. Run both complete implementation suites, both
   dependency guards, and the full repository gate for cross-stack or shared-evidence changes.
10. Do not claim ratification. Preserve the architecture status read from the central registry.

## 3. Action-classification contract

Use exactly these four classifications. They are actions, not confidence labels.

| Classification | Required meaning | Required action |
| --- | --- | --- |
| `implemented` | The current behavior already satisfies the complete observable requirement. | Make no production-code change. Link the existing positive and negative executable evidence. If complete evidence cannot be linked, this classification is invalid. |
| `missing` | The required behavior or evidence-bearing public surface does not exist. | Add native failing vectors, implement the smallest conforming behavior, and record the resulting evidence. |
| `conflicting` | Existing behavior or a public/serialized contract contradicts the current architecture. | Reconcile it now: add a failing regression vector, replace or migrate the conflicting behavior, document affected consumers, and record the breaking-change decision when applicable. Do not defer a known contradiction behind compatibility prose. |
| `non-runtime` | The change intentionally requires no runtime semantics. | Assign and complete one explicit disposition: documentation update, evidence/classification audit, risk/requirements ledger, or architecture-tracked deferral. Never use `non-runtime` to hide missing executable behavior. |

`Implemented` is a code no-op, not permission to make opportunistic refactors. `Missing` and
`conflicting` both result in implementation work now. `Non-runtime` is complete only when its named
disposition exists and is linked from the owning ledger.

If one C-item contains separable requirements with different states, split it into stable
subrequirements and classify each. Do not average a partially implemented item into
`implemented`.

## 4. Provisional audit baseline

The following is a starting hypothesis from the current source tree, not accepted evidence. Codex
must confirm or replace every entry with executable or documentary evidence before implementation.

| Change | Reference starting state | Minimal starting state | Required disposition |
| --- | --- | --- | --- |
| C1 — composite Constraint poisoning | `missing`: atomic fail-closed evaluation exists, but no recursive expression model or selection poisoning is evident | `missing`: flat Constraint requirements exist, but no recursive expression model or selection poisoning is evident | Implement in both stacks independently; preserve existing atomic behavior as the compatibility floor |
| C2 — typed member canonical names | `missing`: `CanonicalName` accepts concept paths but not the typed-member suffix | `missing`: `CanonicalName` accepts concept paths but not the typed-member suffix | Add strongly typed member identity and gateway parsing without pretending the provisional member-kind catalogue is ratified |
| C3 — static Attribute-constrained binding | `missing`, unless an existing resolver is being presented as this binding | `missing`, unless an existing resolver is being presented as this binding | Implement experimental one-time resolution; if an existing binding silently reselects, reclassify that surface as `conflicting` and migrate it now |
| C4 — Router logical-endpoint guarantees | `missing` for the planned persistent-information experiment | `missing` for the planned persistent-information experiment | Implement the minimum experimental Router guarantee slice in each stack |
| C5 — Dataset authority, identity, and concurrency | `missing` for the planned persistent-information experiment | `missing` for the planned persistent-information experiment | Implement the minimum Opaque Corpus/Dataset/Store-role slice in each stack |
| C6 — extraction and term-status routing | `non-runtime` | `non-runtime` | Audit implementation documentation and project classification against the companion notes; fix links and labels only |
| C7 — editorial and authority-boundary clarification | `non-runtime` | `non-runtime` | Audit implementation terminology, test rationale, and authority-boundary documentation; any discovered behavioral contradiction becomes `conflicting` under its semantic requirement |
| C8 — Mediation direction | `non-runtime` | `non-runtime` | Produce a requirements/risk ledger only; do not introduce a normative `Mediator` participant or conformance claim |

## 5. Foundation: permanent requirements and failing vectors

Complete this foundation before production implementation:

1. Create a revision-specific current-architecture requirement inventory rather than changing the
   retained baseline inventory.
2. Create distinct current-architecture matrices for Reference and Minimal. Each entry names its
   architecture section, classification, implementation location or non-runtime disposition,
   positive evidence, negative evidence, status, and rationale.
3. Map every new requirement to its retained-baseline predecessor when one exists. Absence of a
   predecessor must be explicit.
4. Record the exact architecture path, status, content hash, and reviewed commit selected by the
   central registry.
5. Add independently authored failing vectors for C1-C5. Minimal vectors must exist before Minimal
   reads Reference implementation details, and the reverse applies equally.
6. Extend mechanical evidence verification for the new inventory and matrices without weakening
   validation of the retained baseline.

Do not place current-architecture requirements into the retained baseline matrices. Evidence for a
complete draft must remain distinguishable from ratified conformance.

## 6. C1 — composite Constraint poisoning

### Observable behavior

- Model recursive atomic, `AllOf`, `AnyOf`, and `Not` expressions without reducing unknown to
  `false`.
- Evaluation distinguishes satisfied, unsatisfied, and unevaluatable/indeterminate outcomes.
- One unrecognised atom anywhere poisons the complete expression, including apparently decisive
  siblings and short-circuit positions.
- In authority evaluation, a poisoned expression denies before effects and produces a visible,
  non-sensitive decision record.
- In selection, a poisoned Definition Constraint excludes the candidate and records the
  unrecognised atom and deterministic diagnostic category.
- Evaluation order must not change the result or explanation category.

### Reference scope

- Authority expression and result semantics belong in `Brontide.Reference.Core`.
- Selection semantics belong in `Brontide.Reference.Experimental.Composition`.
- Preserve existing atomic Constraints as a valid leaf or provide a documented migration if the
  public evaluator contract changes.
- Add the nearest Core, conformance, and Composition NUnit vectors.

### Minimal scope

- Expression and result types belong in `Brontide.Minimal.Model`; deterministic target-side
  evaluation belongs in `Brontide.Minimal.Kernel`.
- Selection semantics belong in `Brontide.Minimal.Experimental.Composition`.
- Keep issuer-controlled references opaque and preserve the existing flat requirements through an
  explicit leaf/migration path.
- Add the nearest Kernel, conformance, and Composition NUnit vectors.

### Mandatory vectors

Cover unknown atoms in every operand position; nested `AllOf`, `AnyOf`, and `Not`; a matching
sibling beside an unknown atom; an unsatisfied sibling beside an unknown atom; authority denial
before effect; selection exclusion; protected-value redaction; and deterministic explanations
under reordered inputs.

## 7. C2 — typed member canonical names

Implement the grammar selected by the architecture source:

```text
[AuthorityPath ":"] ConceptPath ["#" MemberKind "." MemberName]
```

### Required behavior

- Represent typed member identity with a dedicated public type, distinct from concept identity and
  from every other identifier space.
- Represent `MemberKind` as a validated typed token or explicitly experimental catalogue. Do not
  freeze a closed enum that implies the provisional catalogue is ratified.
- Parse, validate, compare ordinally, format canonically, and round-trip qualified and unqualified
  concept names plus typed members.
- Keep versions outside canonical identity.
- Reject empty owner, kind, or member segments; repeated or misplaced `:`/`#`; extra delimiters;
  ambiguous dot-segment encodings; whitespace variants; and a member suffix without an owning
  concept.
- Never emit a legacy or NonStrict alias as canonical output. Any accepted migration alias must be
  bounded, documented, and tested separately from canonical parsing.
- Update serializers, manifests, codecs, and external adapters at their owning seams. Bare strings
  remain only at parsing, serialization, storage, and external-system boundaries.

Before changing a public type or wire spelling, identify all consumers and decide the migration.
This work is likely breaking and must follow the repository's breaking-change rules.

## 8. C3 — static Attribute-constrained binding

Implement this only in each stack's experimental Composition project.

- Define an Attribute source through an exact Operation, vocabulary claim, result Shape, and result
  path; it is not a free-floating label.
- Resolve a binding exactly once at composition or activation resolution.
- Store the resolved target, effective Attribute values, matched branches, and provenance in an
  immutable resolved record.
- Later candidate, Attribute, registry, or provider changes must not alter the record.
- Restoration must restore the recorded resolution and must not query or select again.
- Missing Attributes, unsupported Constraint atoms, ambiguous ties, or an inability to establish
  provenance produce explicit creation failure.
- Candidate removal, mutation, a better later candidate, equal-score ties, restoration, and
  unsupported constraints require native tests.

Do not turn static binding into lifecycle, discovery, service-location, or automatic-rebinding
machinery.

## 9. C4 and C5 — first persistent-information experiment

Create independent experimental production and NUnit test projects at the paths named by each
stack's implementation notes. Register them in the appropriate solution and dependency guard.
Keep all persistent-information types out of Reference Core and Minimal Model/Kernel.

### Minimum model

- Strongly typed Corpus, Dataset, Store-role, Store, and Router identities/references.
- An Opaque Corpus version with one or more Store roles and an explicit concurrent-access
  declaration. Absence of that declaration is rejected.
- Corpus-declared identity-bearing Store roles.
- A Dataset record whose identity is independent of any Store's content and survives loss of a
  non-identity-bearing role according to its declared absence behavior.
- Store-role bindings to exactly one logical Store endpoint per role.
- Dataset creation as attributable authorised issuance by an existing Actor, not a new Genesis
  escape hatch.
- Dataset/Store operations exercised through existing Capability evaluation, with wrong actor,
  capability, target, role, and operation denied before effects.

### Router guarantee slice

- A Router presents a logical Store-compatible endpoint while delegating to backing Stores.
- Endpoint Attributes are the Router's declared guarantees, not copied from the currently selected
  backing Store.
- Construction or configuration fails when declared guarantees cannot be upheld across declared
  backing and fallback behavior.
- Changing the selected backing must not silently change the logical endpoint guarantee.
- Tests cover backing changes, outage/fallback, unsupported guarantees, topology visibility under
  management authority, and refusal to leak confidential routing policy.

Implement only the minimum read/write or append observations needed to prove identity, authority,
persistence, declared concurrency, and Router guarantees. Do not invent a database, transaction
system, full lifecycle service, Mirror/Backup semantics, or normative Storage vocabulary.

## 10. C6-C8 non-runtime dispositions and comparison

### C6 — documentation and classification audit

- Confirm Enrichment, Composition, and Persistent Information remain outside Base and are linked to
  their companion design notes.
- Update each implementation's README, milestone evidence, findings, and experimental-project
  registry when classification or evidence changes.
- Confirm project names and public documentation do not imply ratification.

### C7 — terminology and authority-boundary audit

- Confirm implementation docs and test rationale use the current architecture terms and correct
  section references.
- Confirm minting, custody, target-side evaluation, and cross-domain authority remain separate.
- Confirm no Capability crosses the Reference/Minimal process seam.
- Reclassify any observed runtime contradiction as `conflicting` under its semantic requirement and
  reconcile it now.

### C8 — requirements/risk ledger only

- Record Selection, Distribution, and Arbitration implications; authority-topology enforcement;
  provenance, deputy discipline, affinity, residue obligations, and trust-surface risks.
- Classify existing routers or event mediators only as experimental examples where accurate.
- Do not add a universal `Mediator` type, implicit interposition, discovery-derived authority, or a
  Mediation conformance claim.

Architecture 0.8 additionally records Aggregation as a fourth Mediation species beside Selection,
Distribution, and Arbitration; that delta belongs to the M6/R6 Architecture 0.8 handoff ledgers,
not to this 0.7 disposition.

### Independent comparison

After both native implementations pass their own vectors, compare C1-C5 observations through
data-only fixtures and process boundaries. Compare accepted results, denials, canonical forms,
static-resolution provenance, restoration, Router guarantees, and diagnostic categories. Classify
every disagreement as a defect, architecture ambiguity, or intentional implementation choice.
Local fixtures that imitate the other runtime are not cross-stack proof.

## 11. Completion and deletion gate

This brief is complete only when all of the following are true:

- every C1-C8 item and separable subrequirement has one accepted action classification for each
  stack;
- every `implemented` item links complete existing positive and negative evidence and caused no
  production-code change;
- every `missing` item is implemented with native positive and negative evidence;
- every `conflicting` item is reconciled now with migration and breaking-change records where
  applicable;
- every `non-runtime` item has its named documentation, audit, ledger, or tracked-deferral output;
- current-architecture requirement inventory and distinct per-stack matrices are mechanically
  verified without rewriting the retained baseline;
- both stacks pass their full suites and dependency guards, and the full repository gate passes;
- cross-stack claims use actual process-boundary evidence from independently implemented behavior;
- implementation-owned documentation and the central status registry accurately route all
  accepted evidence while preserving draft and ratification status;
- no temporary implementation conclusion exists only in this file.

After those conditions are met, migrate any remaining durable rationale to its owning document,
run link/text/evidence verification, delete this file, and verify the repository again. The
separate correction programme remains governed exclusively by its own checked deletion gate and
permanent closure record.
