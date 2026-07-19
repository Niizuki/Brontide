# Brontide Minimal Stack Implementation Plan 0.3

Status: Planned  
Architecture target: [Brontide Architecture 0.7](Brontide-Architecture-0.7.md), Complete Draft

Implementation baseline: Minimal Stack 0.2 / Architecture 0.5 evidence

Delivery ledger: [Minimal Architecture 0.7 delivery](Minimal/docs/architecture-0.7-delivery.md)

## 1. Purpose

This is the Minimal stack's independent delivery plan for the changes completed in the
[Architecture 0.7 document edit](Brontide-Architecture-0.7-Change-Plan.md). It supersedes Plan 0.2
only for new 0.7 work; Plan 0.2 and its
milestone evidence remain the record of the Architecture 0.5 implementation baseline.

Before this plan can support an implementation claim, the applicable gates in
[the temporary correction plan](Brontide-Temporary-Implementation-Correction-Plan-0.1.md) must be
closed. That plan owns the known corrective work. This roadmap deliberately does not repeat or
pre-claim those changes.

The temporary
[current-architecture implementation brief](Brontide-Temporary-Current-Architecture-Implementation-Brief-0.1.md)
defines the aligned audit actions and executable delivery instructions for C1-C8. It is subordinate
to this plan and may be deleted only by its own completion gate.

Architecture 0.7 is a complete draft, not a ratified or implemented release. Completing a phase
below creates evidence for review; it does not change that status by itself.

## 2. Delivery rules

- Derive behavior directly from Architecture 0.7 and its term registry, not from Reference code.
- Express the smallest model that preserves every required observation; minimal does not mean
  weaker authority, identity, or failure semantics.
- Begin each normative phase with independent failing vectors.
- Keep persistent-information work experimental until its evidence gate is accepted.
- Record requirement IDs and evidence in the permanent conformance matrix required by the temporary
  correction plan.
- Share only data-only interchange fixtures and expected observations with Reference.

### 2.1 Complete Architecture 0.7 coverage

The implementation-owned
[Architecture 0.7 delivery ledger](Minimal/docs/architecture-0.7-delivery.md) maps every executed
0.7 change C1-C8 to an owner, planned evidence, and status boundary. M1-M4 implement C1-C5. C6 and
C7 require documentation and classification review rather than invented runtime behavior. C8
remains a recorded, non-ratified direction and must not produce a `Mediator` conformance claim. M6
tracks the separate Architecture 0.8 handoff named in §35.1.

## 3. Phase M0 — independent delta ledger

Goal: make the exact 0.5-to-0.7 delta reviewable before production changes.

Deliverables:

1. Classify each Architecture 0.7 change with exactly one action: `implemented` means a production
   code no-op backed by complete existing evidence; `missing` means implement now; `conflicting`
   means reconcile the contradiction now with migration where required; and `non-runtime` means
   complete an explicit documentation, evidence-audit, risk-ledger, or tracked-deferral
   disposition.
2. Add stable requirement IDs and map each to any Architecture 0.5 predecessor.
3. Write Minimal-native failing vectors for M1 through M4 before reading the corresponding
   Reference implementation work.
4. Record the Architecture revision and commit used for the review.
5. Keep the delivery ledger synchronized with the stable requirement-ID inventory.

Exit evidence:

- no unclassified normative change;
- no 0.7 conformance claim in README or milestone evidence;
- known Base corrections remain owned by the temporary correction plan.

## 4. Phase M1 — composite Constraint poisoning

Goal: implement the 0.7 rule that an unknown atomic Constraint poisons the entire composite
expression.

Required behavior:

- constraint evaluation has an explicit unknown/indeterminate outcome;
- authority evaluation denies when an evaluated composite is poisoned;
- candidate selection excludes candidates with poisoned composites;
- AND, OR, and NOT cannot hide an unknown atom through evaluation order;
- diagnostics identify the unsupported kind without exposing protected values.

Vectors must cover every operand position, nested composites, apparently decisive siblings,
authority checks, selection, and deterministic diagnostic categories.

## 5. Phase M2 — typed member canonical names

Goal: implement the canonical grammar:

    [AuthorityPath ":"] ConceptPath ["#" MemberKind "." MemberName]

Required behavior:

- model concept names and typed member names without lossy string splitting;
- parse, validate, format, compare, and round-trip authority-qualified names;
- reject empty, malformed, ambiguous, and extra-delimiter forms;
- keep version outside canonical identity unless expressly required by the architecture;
- test every registered MemberKind and all boundary delimiters.

Any compatibility alias must be explicitly bounded and must never be emitted as canonical output.

## 6. Phase M3 — static Attribute-constrained binding

Goal: resolve Attribute-constrained bindings once and preserve their result.

Required behavior:

- binding creation selects once under the declared constraints;
- the binding contains an immutable resolved value and selection provenance;
- later candidate/attribute changes do not rebind it;
- unresolved creation fails explicitly rather than becoming a live query;
- restoration preserves prior resolution and does not silently select again.

Vectors must include mutation, removal, a better later candidate, ties, restoration, and unsupported
constraints.

## 7. Phase M4 — first persistent-information role evidence

Goal: build the smallest faithful Architecture 0.7 slice for Opaque Corpus, Dataset, and Store.

Proposed experimental boundaries:

- Minimal/src/Brontide.Minimal.Experimental.PersistentInformation
- Minimal/tests/Brontide.Minimal.PersistentInformation.Tests

Required behavior:

1. Capability-governed Dataset designation and creation, with the Genesis-versus-issuance choice
   explicit in the evidence.
2. Dataset identity carried by the Dataset record independently of any one Store, with the Corpus
   declaring which Store roles are identity-bearing.
3. Identity-bearing Opaque Corpus, Dataset, and Store role references.
4. Explicitly declared concurrent-access behavior; missing declarations are rejected.
5. Router endpoint guarantees belong to the Router and are not inferred from a backing Store.
6. A minimal read/write or append path proving identity, authority, persistence, and declared
   concurrency observations.
7. Negative evidence for wrong Capability, wrong target, unsupported concurrency, and leakage of
   Store guarantees through a Router.

Minimality is judged by surface area, not by omitting required semantics. Other
persistent-information roles remain planned.

## 8. Phase M5 — cross-stack comparison

Goal: compare observable outcomes while keeping implementation decisions independent.

Deliverables:

- consume common data-only vectors for M1 through M4 only after Minimal-native vectors exist;
- compare accepted results, denials, canonical names, provenance, restoration, and diagnostics
  categories across process boundaries;
- classify every disagreement as a defect, architecture ambiguity, or intentional choice;
- publish the exact proof boundary and exclusions.

Update the solution and dependency-boundary verifier only when the experimental projects are
actually implemented; this planning document does not authorize source changes now.

## 9. Phase M6 — Architecture 0.8 handoff

Prepare a requirements and risk ledger, without pre-implementing:

- Portable Binding Shape floor;
- Channel;
- Flow conformance.

Mediation remains a recorded direction unless a later architecture revision ratifies it.

## 10. Completion gate

Plan 0.3 is complete only when:

- M0 through M5 have accepted evidence and the full repository gate passes;
- applicable temporary correction-plan items, especially the Minimal Base authority corrections,
  are closed;
- Minimal milestone evidence identifies Architecture 0.7 requirement IDs and limitations;
- comparison evidence comes from independently implemented behavior;
- Architecture 0.7 has been ratified, or claims continue to use complete-draft language rather than
  released/implemented architecture;
- documentation accurately separates implemented, experimental, and planned scope.

Do not delete the temporary correction plan merely because this roadmap is complete. Its own
deletion gate controls its lifetime.
