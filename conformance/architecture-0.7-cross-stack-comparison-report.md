# Architecture 0.7 R5/M5 cross-stack comparison report

Date: 2026-08-10

The shared fixture contains 15 data-only questions across recursive Constraint evaluation, typed
member canonical names, static Attribute-constrained binding and restoration, and experimental
persistent information. Independent C# and F# executables evaluate those questions in separate
processes using only their own native public surfaces.

## Result

All 15 expected observations agree with the fixture oracle and with the other stack. The comparison
found no disagreements, so there are no entries to classify as a defect, architecture ambiguity, or
intentional implementation choice. `allowedDisagreements` is empty and the gate fails if that changes.

Compared fields include acceptance and denial, canonical values, diagnostic categories, binding
provenance, restoration, Dataset identity after Store content loss, effect counts at a concurrency
denial, Router guarantees, fallback selection, and topology redaction.

## Proof boundary

Passing proves that the independently built Architecture 0.7 experimental surfaces answer this
published finite vector set alike across real process boundaries. It does not prove Architecture
0.7 ratification, exhaustive conformance, compatible private models, a cross-stack wire protocol,
durability, multi-writer coordination, or Architecture 0.8 behavior. The native R1–R4 suites remain
the broader local semantic evidence.

The executable gate is `build/verify-architecture-0.7-comparison.ps1`; its capability contract and
absence review are `br-07-cross-stack-comparison-001-contract.md` and
`br-07-cross-stack-comparison-001-contract-completeness-review.md` in this directory.
