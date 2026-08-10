# BR-07-CROSS-STACK-COMPARISON-001 capability contract

Status: experimental Architecture 0.7 comparison evidence.

This contract defines the R5/M5 comparison seam. It does not make either implementation normative,
and it does not allow either stack to reference the other.

## Capabilities

- **C1 — data-only questions.** A versioned JSON fixture identifies R1/M1 through R4/M4 scenarios,
  supplies primitive inputs, and declares the canonical observation expected from each. No CLR type,
  assembly name, implementation exception, or stack-private serialization appears in the fixture.
- **C2 — independent process observations.** One native executable per stack reads the same fixture,
  invokes only its own stack's public implementation, and emits one canonical JSON observation for
  every vector. Missing, duplicate, or unsolicited observations fail comparison; ordering is not
  semantic and is normalized by vector id.
- **C3 — complete observable comparison.** The gate compares acceptance or denial, canonical value,
  diagnostic category, provenance, restoration, and persistent-information observations whenever the
  vector declares those fields. It compares JSON structure, not diagnostic prose.
- **C4 — expected and paired agreement.** Each native observation must agree both with the fixture's
  expected observation and with the other stack. Two implementations agreeing on a wrong answer is a
  failure, not parity evidence.
- **C5 — disagreement accountability.** Any allowed disagreement must be listed in the comparison
  report with its vector id, both observations, and exactly one classification: `defect`,
  `architecture-ambiguity`, or `intentional-implementation-choice`. This delivery permits no allowed
  disagreements; an unclassified difference fails closed.
- **C6 — bounded proof.** Passing proves only that the independently built Architecture 0.7
  experimental surfaces answer the published finite vectors alike across real process boundaries.
  It does not prove Architecture 0.7 ratification, exhaustive semantics, compatible private models,
  wire-protocol interoperability, durability, multi-writer coordination, or Architecture 0.8 behavior.

## Properties over every vector

- Every vector id is unique, belongs to exactly one delivered phase, and receives exactly one result
  from each process.
- A denied observation has a non-empty diagnostic category and no effect-bearing observation.
- Canonical observations contain no stack name, private type name, exception text, file path, or
  nondeterministic identifier.
- Comparison is ordinal and invariant under fixture order; output is normalized by vector id.

## Evidence

`build/verify-architecture-0.7-comparison.ps1` names and checks C1 through C6, starts both native
executables, validates each result against the data-only fixture, and compares the normalized result
sets. The shared vectors are `conformance/architecture-0.7-comparison-vectors.json`.
