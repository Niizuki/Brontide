# Architecture 0.8 A08-D3 behavioral contract

Status: experimental delivery contract; does not change either stack's Architecture 0.7 target or
claim whole-Architecture 0.8 conformance.

This slice delivers Architecture 0.8 changes C9 and C8. Constraint declarations and their
recognition decisions are authority-plane data. Payload projection remains a separate covariant
operation.

## D3-C1 — a declared but declined Constraint is identifiable and denies

A well-formed Constraint declaration states its canonical name and declaration version, exact value
Shape/version, target-authority evaluator domain, fail-closed unknown behavior, evaluation semantics,
accounting scope, and parallel-name evolution policy. A domain may record that declaration without
implementing its evaluator. Presentation then denies before the requested effect and identifies the
declined canonical name in a deterministic, non-sensitive diagnostic.

Canonical vector: `BR-08-ADV-C9-001`.

Property: every declined-declaration path is effect-free and names only the declaration, never the
Constraint value.

## D3-C2 — authority semantics are immutable under one canonical name

A domain refuses a second declaration under an existing Constraint canonical name when its value
Shape or evaluation semantics differ. A vocabulary must introduce a new canonical name and author an
explicit strong-Kleene fallback instead.

Canonical vector: `BR-08-ADV-C9-002`.

Property: no accepted declaration registry contains two semantic or value-Shape meanings for one
canonical Constraint name.

## D3-C3 — every static domain exposes a deterministic recognition set

Each domain exposes its complete Constraint recognition set in canonical-name order. Every row
contains the full declaration and exactly one decision: implemented or declined. Standard Constraints
are included. The set is evidence only; it grants no authority and invokes no evaluator.

Canonical vector: `BR-08-ADV-C9-003`.

Property: repeated recognition-set observations are equal, ordered, complete, and effect-free.

## D3-C4 — Constraint values are validated exactly and never projected

A Constraint atom is evaluatable only when its presented value uses the declaration's exact Shape
version and contains no projected-away structure. A later additive value version is Unknown even when
its version-one projection would satisfy the evaluator; standing alone it denies before effects.

Canonical vector: `BR-08-ADV-C8-001`.

Property: no authority decision invokes an evaluator with a value whose presented Shape/version
differs from the declaration.

## D3-C5 — authored fallback remains available under version skew

`AnyOf(NewConstraint, OldConstraint)` evaluates structurally. If the new atom is Unknown because its
value cannot be evaluated exactly and the old atom is recognised and satisfied, the expression is
True and the effect may run once.

Canonical vector: `BR-08-ADV-C8-002`.

Property: the fallback can authorise only through a satisfied recognised branch; the Unknown branch
is never evaluated through projection.

## D3-C6 — ordinary payload projection is unchanged

An Operation accepting version one of an open additive payload Shape accepts a valid version-two
value, delivers its canonical version-one projection to the handler, and ignores the added optional
constituent. This rule is confined to the payload plane.

Canonical vector: `BR-08-ADV-C8-003`.

Property: every accepted payload projection validates the complete presented value first and the
handler receives only fields declared by its accepted Shape/version.

## Phase boundary

Reference and Minimal implement the six items independently and name all six canonical vectors in
native NUnit tests. D3 does not implement quantified accounting, composition-time catalogue
negotiation, liveness, resource issuance, or Terminus. It does not modify the status registry,
pinned Architecture 0.7 evidence, or the canonical Architecture 0.8 vector inventory.
