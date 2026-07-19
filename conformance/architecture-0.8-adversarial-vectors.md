# Architecture 0.8 adversarial conformance vectors

Status: authored companion to
[`architecture-0.8-adversarial-vectors.json`](./architecture-0.8-adversarial-vectors.json),
which is the canonical vector inventory. This document explains intent and conventions; it does
not restate the vectors. The vectors derive from
[`Brontide-Architecture-0.8-Change-Plan.md`](../Brontide-Architecture-0.8-Change-Plan.md)
(C1–C14) and use Architecture 0.7 section numbers, per that plan.

These vectors are adversarial by design: most encode an attack or divergence trap failing, not
a feature working. Positive controls are included so that a stack cannot pass by denying
everything. Evidence vectors are attestation checks assembled with each stack's ledger and
matrix rather than runtime behaviour.

## Why these vectors exist

The 0.7 stress test showed that every hole in the constraint algebra lived at a seam — where
the pure predicate calculus meets state (C5), versioning (C8), time (C1, C3), sibling
mechanisms (C2), or representation choices (C4, C11). Seams are precisely where two
independently implemented stacks diverge silently: each implementation is locally reasonable
and the disagreement only appears when an adversary chooses the evaluator. Machine-checked
vectors at the seams are therefore the cheapest form of the project's stated goal that
automated verification should carry the load wherever semantics are mechanically testable
(§29.3).

## The three divergence traps

Three vectors deserve explicit rationale because they pin behaviour a reasonable implementer
would get wrong in a defensible way:

- **BR-08-ADV-C7-006 (`AnyOf(X, Not(X))`, X unrecognised → deny).** The expression is a
  classical tautology, and an implementation that normalises or SAT-solves expressions will
  authorise. Three-valued evaluation is structural: no reasoning across repeated atoms. A
  supervaluating implementation diverges silently from a Kleene implementation on exactly this
  class of expression, which is why the non-clever answer is pinned.
- **BR-08-ADV-C8-001 (version-skewed Constraint value → deny, never project).** Additive Shape
  projection is correct for payloads and is the vulnerability for Constraint values. The vector
  asserts the evaluator refuses the projection it is normally required to perform — the
  polarity flip of the two-plane principle (C9) made mechanical.
- **BR-08-ADV-C4-001 (grandparent Constraint denies at the grandchild).** The vector is
  constructed so that a leaf-only evaluator — one that checks just the presented Capability's
  own added Constraints — authorises and fails. It distinguishes audit provenance (optional)
  from authorisation-time chain conjunction (mandatory).

## Budget-pool semantics (C5)

`BR-08-ADV-C5-001/-002/-003` fix the three accounting decisions: sibling derivations share the
budget at the Constraint's occurrence (delegation never multiplies quantified authority),
denied Executions consume nothing, and a declared accounting scope the evaluator cannot enforce
denies fail-closed. Stacks implementing rate or capacity Constraints should expect these three
to be the vectors that catch an otherwise clean implementation.

## Behavioural inversions from 0.7

`BR-08-ADV-C7-007` deliberately inverts the 0.7 §29.2 selection example: `AnyOf` of an
unrecognised atom and a matching atom now retains the candidate and records the Unknown atom.
The inversion is recorded in change C7 of the plan; the 0.7 poisoning rule is superseded, and
its underlying rationale (unknown never becomes `false`) survives inside the Kleene rule and is
still enforced by `BR-08-ADV-C7-001`.

## Coverage

C13 and C14 are documentation-only changes and carry no behavioural vectors; per §29.3 the
guaranteed surface of those changes is their normative text. Evidence vectors (C9-002/-003,
C11-001, C12-003) are checked against ledgers and declared recognition sets, in the same manner
as the existing evidence verification scripts, and are candidates for inclusion in the
repository gate once the 0.8 document edit lands.

## Relationship to requirement inventories

These vectors precede the 0.8 requirements inventory. When
`conformance/architecture-0.8-requirements.json` is authored for the document edit, each vector
maps onto a requirement ID in the established `BR-08-*` vocabulary with `predecessors` links to
the `BR-07-*` requirements it supersedes or extends — in particular, the C7 vectors supersede
the poisoning-era `BR-07-CONSTRAINT-001/-002/-003` family. Vectors are evidence discipline, not
architecture: nothing here ratifies the change plan.
