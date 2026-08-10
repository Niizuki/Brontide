# Architecture 0.8 delivery audit

Reviewed: 2026-08-10

This audit implements the phase authorized after the R6/M6 handoff. It compares the current native
Reference and Minimal surfaces with the Complete Draft Architecture 0.8 C1-C14 register and all 33
canonical vectors. It changes no runtime source, accepts no Architecture 0.8 conformance, and leaves
both stack targets at Architecture 0.7.

## Post-audit delivery status

A08-D1 was subsequently authorized and is now delivered as explicit experimental Draft-0.8 paths in
both stacks. Its five-item behavioral contract and 11 C7/C3/C4 vectors execute natively, while the
ordinary Architecture 0.7 evaluator, execution, and selection entry points retain poisoning
semantics. This does not rewrite the audit findings below, which are the pre-delivery inventory.

A08-D2 was subsequently authorized and is now delivered as a breaking experimental migration in
both stacks. The Boolean delegability field and issuance arguments are removed; default-on
Delegation is narrowed by a carrying-link depth Constraint, and every derivation implicitly adds an
ordinary `Origin.Derived` ceiling. All four C6/C2 vectors plus a phase-wide property execute natively.
The migration is recorded separately and does not rewrite the pre-delivery findings below.

A08-D3 was subsequently authorized and is now delivered as a breaking experimental migration in
both stacks. Constraint types carry first-class declaration metadata and deterministic recognition
decisions; authority values require the declaration's exact Shape/version and are never projected,
while Operation payload projection remains additive. All six C9/C8 vectors execute natively. This
does not rewrite the pre-delivery audit findings below.

A08-D4 was subsequently authorized and is now delivered as experimental runtime evidence in both
stacks. Every liveness-scoped ancestor is evaluated at the Draft-0.8 presentation instant; Base
execution-rate budgets pool at their exact chain occurrence, denied Executions consume nothing, and
unenforceable vocabulary scopes remain named declines. All six C1/C5 vectors execute natively. This
does not rewrite the pre-delivery audit findings below.

A08-D5 was subsequently authorized and is now delivered as experimental runtime evidence in both
stacks. Dataset creation issues requester authority by ordinary Delegation from an explicit
provider-held resource-space chain, while exceeded ancestor scope refuses before resource effects.
Both C10 vectors execute natively. This does not rewrite the pre-delivery audit findings below.

## Findings

| Change | Reference | Minimal | Audit conclusion |
| --- | --- | --- | --- |
| C1 | `candidate-partial` | `missing` | Reference has atomic presentation-time liveness plus carried ancestry; Minimal has ancestry but no liveness-scoped authority type. Neither executes the three vectors. |
| C2 | `candidate-partial` | `missing` | Reference has special origin ceilings but not implicit `Origin.Derived` in the ordinary algebra; Minimal has no corresponding origin Constraint. |
| C3 | `candidate-reusable` | `candidate-reusable` | Both roots deny before effect dispatch. Canonical instantaneous-authorization vectors are still required before acceptance. |
| C4 | `candidate-reusable` | `candidate-reusable` | Reference carries parents and Minimal resolves them through `World`; both traverse ancestors, but neither executes the canonical grandparent vector. |
| C5 | `missing` | `missing` | Neither stack has general occurrence-pooled quantified accounting or declared-scope enforcement. |
| C6 | `conflicting` | `conflicting` | Both expose explicit Boolean delegability, contrary to default-on Constraint narrowing. |
| C7 | `conflicting` | `conflicting` | Both deliberately implement 0.7 whole-expression poisoning in authority and selection. |
| C8 | `candidate-partial` | `candidate-partial` | Portable Binding protects its no-Capability authority seam and supports payload projection, but it does not evaluate versioned Constraint values. |
| C9 | `candidate-partial` | `candidate-partial` | Both fail unknown kinds closed and have portable declarations, but neither declares a stack-wide recognition set or complete Constraint evolution/accounting metadata. |
| C10 | `candidate-partial` | `candidate-partial` | Dataset creation is attributable and effect-gated but issues no Capability derived from provider resource-space authority. |
| C11 | `handoff-attested` | `handoff-attested` | The distinct carried and resolved choices and their no-revocation ceilings are already recorded. |
| C12 | `missing` | `missing` | Neither stack has Terminus or an attributable Actor-retirement disposition policy. |
| C13 | `architecture-only` | `architecture-only` | No runtime implementation follows from the legibility scope statement. |
| C14 | `architecture-only` | `architecture-only` | Holder introspection remains an open decision and is not a runtime requirement. |

No canonical runtime vector is currently executed by name. C11's evidence vector is attested by the
handoff notes; C13 and C14 are documentation-only coverage.

## Proposed runtime delivery queue

This queue is an audit output, not runtime authorization.

| Slice | Changes | Dependency | Bounded outcome |
| --- | --- | --- | --- |
| A08-D1 | C7, C3, C4 | Audit complete | Replace poisoning with structural strong-Kleene evaluation in authority and selection, while pinning pre-effect and full-chain behavior with the 11 applicable canonical vectors. Preserve explicit 0.7 evidence as historical tests rather than silently rewriting its claim. |
| A08-D2 | C6, C2 | A08-D1 | Replace Boolean delegability with default-on Constraint narrowing and establish `Origin.Derived` inside the same algebra. This is a breaking public-surface decision and requires an explicit migration note. |
| A08-D3 | C9, C8 | A08-D1 | Introduce first-class Constraint declarations and recognition-set evidence, then enforce the projection exemption while retaining payload projection. |
| A08-D4 | C1, C5 | A08-D2, A08-D3 | Add liveness-scoped ancestor evaluation and occurrence-pooled quantified accounting with fail-closed scope declarations. |
| A08-D5 | C10 | A08-D2, A08-D4 | Issue provider resource Capabilities by derivation without exceeding the provider chain. |
| A08-D6 | C12 | A08-D5 | Add Terminus and the domain's attributable held/outbound authority disposition policy. |

C11 remains an operational constraint on every slice rather than a new runtime phase. C13 and C14
remain outside the runtime queue. Channel and Portable Binding remain reusable experimental inputs;
Flow conformance still follows the decided Channel → Portable Binding and Shape floor → Flow order.

## Next authorization boundary

A08-D1 through A08-D5 are delivered with named failing-first tests and independent implementations
in each stack. A08-D2 and A08-D3 carry their required breaking-surface migration decisions. The next
implementable runtime slice is **A08-D6**: C12 attributable Terminus and the domain's explicit
held/outbound authority-disposition policy. It requires a separate
explicit request and must not change the status registry, pinned 0.7 matrices, or either stack's
`Designed for` declaration.
