# Brontide Reference Stack Implementation Plan 0.3

Status: Planned  
Designed for: [Brontide Architecture 0.7](../Brontide-Architecture-0.7.md), Complete Draft

Related notes: [Reference Architecture 0.7 implementation notes](docs/architecture-0.7-delivery.md)

## 1. Purpose

These are the Reference stack's implementation notes for the changes completed in the
[Architecture 0.7 document edit](../Brontide-Architecture-0.7-Change-Plan.md). It supersedes Plan
0.2 only for new 0.7 work; Plan 0.2 remains as historical context.

Before this plan can support an implementation claim, the applicable gates in
[the temporary correction plan](../Brontide-Temporary-Implementation-Correction-Plan-0.1.md)
must be closed. That plan owns the known corrective work. This roadmap deliberately does not repeat
or pre-claim those changes.

The temporary
[current-architecture implementation brief](../Brontide-Temporary-Current-Architecture-Implementation-Brief-0.1.md)
defines the aligned audit actions and executable delivery instructions for C1-C8. It is subordinate
to this plan and may be deleted only by its own completion gate.

Architecture 0.7 is a complete draft, not a ratified or implemented release. Completing a phase
below creates evidence for review; it does not change that status by itself.

## 2. Delivery rules

- Derive behavior from Architecture 0.7 and its term registry, not from the Minimal implementation.
- Begin each normative phase with observable positive and negative vectors.
- Keep new persistent-information work experimental until the evidence gate is accepted.
- Record requirement IDs, implementation locations, and test evidence in the permanent conformance
  matrix required by the temporary correction plan.
- Preserve the independence boundary: shared interchange fixtures may describe inputs and expected
  observations, but may not supply semantic implementation logic.

### 2.1 Complete Architecture 0.7 coverage

The implementation-owned
[Architecture 0.7 implementation notes](docs/architecture-0.7-delivery.md) map every executed 0.7
change C1-C8 to an owner, planned evidence, and status boundary. R1-R4 implement C1-C5. C6 and C7
require documentation and classification review rather than invented runtime behavior. C8 remains
a recorded, non-ratified direction and must not produce a `Mediator` conformance claim. R6 tracks
the separate Architecture 0.8 handoff named in Architecture 0.7 §35.1; Architecture 0.8 is now a
complete draft, and §9 defines the expanded handoff scope.

## 3. Phase R0 — baseline and change ledger

Goal: make the exact 0.5-to-0.7 delta reviewable before implementation.

Deliverables:

1. Classify each Architecture 0.7 change with exactly one action: `implemented` means a production
   code no-op backed by complete existing evidence; `missing` means implement now; `conflicting`
   means reconcile the contradiction now with migration where required; and `non-runtime` means
   complete an explicit documentation, evidence-audit, risk-ledger, or tracked-deferral
   disposition.
2. Add stable requirement IDs for every claimed 0.7 requirement and link them to the corresponding
   Architecture 0.5 predecessor where one exists.
3. Capture failing vectors for R1 through R4 before production changes.
4. Record the Architecture revision and commit used for the review.
5. Keep the implementation notes synchronized with any retained detailed requirement inventory.

Exit evidence:

- no unclassified 0.7 normative change;
- no 0.7 conformance claim in README or milestone evidence;
- temporary correction-plan status is linked, not duplicated.

## 4. Phase R1 — composite Constraint poisoning

Goal: implement the 0.7 rule that an unknown atomic Constraint poisons the complete composite
expression.

Required behavior:

- evaluation returns an explicit indeterminate/unknown result rather than treating an unknown atom
  as false in isolation;
- authority evaluation denies the request when any evaluated composite is poisoned;
- candidate selection excludes a candidate whose composite is poisoned;
- AND, OR, and NOT cannot mask an unknown atom through short-circuit order;
- diagnostics identify the unsupported constraint kind without disclosing protected values.

Required vectors include unknown atoms in every operand position, nested composites, apparently
decisive siblings, authority use, and selection use.

## 5. Phase R2 — typed member canonical names

Goal: implement the canonical grammar:

    [AuthorityPath ":"] ConceptPath ["#" MemberKind "." MemberName]

Required behavior:

- parse, validate, format, compare, and round-trip authority-qualified concept names and typed
  member names;
- reject empty, malformed, ambiguous, or extra-delimiter forms;
- keep version outside canonical identity except where the architecture explicitly defines a
  versioned value;
- update serializers and public adapters without accepting multiple canonical spellings;
- include all registered MemberKind values in table-driven tests.

Compatibility behavior for pre-0.7 names must be explicit. If a migration alias is accepted, it
must never be emitted as the canonical spelling.

## 6. Phase R3 — static Attribute-constrained binding

Goal: ensure an Attribute-constrained binding resolves once and does not silently rebind.

Required behavior:

- creation resolves the selected target once under the declared constraints;
- the binding stores an immutable resolved value plus selection provenance;
- later attribute, registry, or candidate changes do not alter the resolved target;
- inability to resolve produces an explicit creation failure rather than a live query;
- serialization preserves the value and provenance necessary for audit without turning restoration
  into a new selection.

Required vectors cover candidate mutation, candidate removal, better later candidates, equal-score
ties, restoration, and unsupported constraints.

## 7. Phase R4 — first persistent-information role evidence

Goal: demonstrate the smallest coherent Architecture 0.7 slice for Opaque Corpus, Dataset, and
Store roles.

Proposed experimental boundaries:

- Reference/src/Brontide.Reference.Experimental.PersistentInformation
- Reference/tests/Brontide.Reference.PersistentInformation.Tests

Required behavior:

1. Capability-governed Dataset designation and creation, with the Genesis-versus-issuance choice
   explicit in the evidence.
2. Dataset identity carried by the Dataset record independently of any one Store, with the Corpus
   declaring which Store roles are identity-bearing.
3. Identity-bearing Opaque Corpus, Dataset, and Store role references.
4. Explicit declared concurrent-access behavior; absence of a declaration is rejected.
5. Router endpoint guarantees are the Router's guarantees, never inferred from a backing Store.
6. Minimal read/write or append observations sufficient to prove identity, authority, persistence,
   and declared concurrency behavior.
7. Negative evidence for wrong Capability, wrong target, unsupported concurrency, and accidental
   leakage of backing-Store guarantees.

This phase is not permission to implement the entire persistent-information chapter. Additional
roles remain architectural or planned until separately evidenced.

## 8. Phase R5 — independent comparison

Goal: compare observable outcomes with the Minimal stack without creating a shared implementation.

Deliverables:

- common data-only vectors for R1 through R4;
- cross-process comparison of accepted results, denials, canonical names, provenance, and
  diagnostics categories;
- documented differences, each resolved as a defect, an architecture ambiguity, or an intentional
  implementation choice;
- exact statement of what the comparison does and does not prove.

## 9. Phase R6 — Architecture 0.8 handoff

Architecture 0.8 is now a complete draft: it executes change plan C1-C14 with authored
adversarial vectors and records new non-normative composition, Component-management, topology,
and trust-admission directions. Prepare, but do not pre-implement, a requirement and risk ledger
against [that document](../Brontide-Architecture-0.8.md) covering:

- the evidence programme in its decided order: Channel first, then the Portable Binding and
  Shape floor as Channel's first conforming realisation, then Flow conformance;
- the decided constraint-algebra and authority-lifecycle changes (C1-C14), mapped against
  `conformance/architecture-0.8-adversarial-vectors.json`;
- the known supersession: 0.8 C7 replaces the delivered 0.7 composite-poisoning rule (R1) with
  structural three-valued evaluation, so the `BR-07-CONSTRAINT-001/-002/-003` family becomes
  `conflicting` rework in a future 0.8 delivery audit while remaining valid Architecture 0.7
  evidence; and
- Reference's chain-conjunction representation choice and its revocation ceiling (0.8 §11),
  recorded in the implementation notes before the Portable Binding freezes a representation.

The recorded composition, Component-management, minimum-topology, and trust-admission directions
(0.8 §18.1, §19, §20.1, §24, §33) stay outside this ledger's conformance scope; the fake-manager
harness planned by the
[Component Management Implementation Plan 0.1](../Brontide-Component-Management-Implementation-Plan-0.1.md)
owns that experimental work. The output is a requirement and risk ledger only. Mediation —
recorded in 0.8 with Aggregation as a fourth species beside Selection, Distribution, and
Arbitration — remains a recorded direction unless a later architecture revision ratifies it.

## 10. Completion gate

Plan 0.3 is complete only when:

- R0 through R5 have accepted evidence and the full repository gate passes;
- applicable temporary correction-plan items are closed;
- Reference milestone evidence identifies Architecture 0.7 requirement IDs and limitations;
- the Minimal comparison used independently implemented behavior;
- Architecture 0.7 has been ratified, or all claims continue to say complete draft rather than
  released/implemented architecture;
- documentation accurately separates implemented, experimental, and planned scope.

Do not delete the temporary correction plan merely because this roadmap is complete. Its own
deletion gate controls its lifetime.
