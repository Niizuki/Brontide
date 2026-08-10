# Architecture 0.8 A08-D1 behavioral contract

Status: experimental runtime evidence for the Complete Draft Architecture 0.8. Both stacks remain
designed for Architecture 0.7, so A08-D1 adds explicit Draft-0.8 execution and selection entry
points. The existing Architecture 0.7 poisoning entry points and their tests remain intact.

## Capability contract

| ID | Observable capability | Property over every vector | Canonical vectors |
| --- | --- | --- | --- |
| D1-C1 | Structural strong-Kleene expression evaluation. `Not(Unknown)` is Unknown; `AllOf` is False when any child is False, True only when all are True, otherwise Unknown; `AnyOf` is True when any child is True, False only when all are False, otherwise Unknown. | The outcome depends only on the recursively evaluated child outcomes. Repeated atoms are never correlated or simplified across expression positions. | BR-08-ADV-C7-001 through BR-08-ADV-C7-006 |
| D1-C2 | Draft-0.8 authority authorizes only a True expression. False and Unknown deny before effects. | Every non-True authority path has zero provider effects and a deterministic, non-sensitive decision; a True result may retain observed Unknown atom names without becoming Unknown. | BR-08-ADV-C7-001 through BR-08-ADV-C7-006 |
| D1-C3 | Draft-0.8 Definition selection retains only True candidates and records every observed Unknown atom for both retained and rejected candidates. | Candidate order and repeated Unknown occurrences cannot change the normalized explanatory set. | BR-08-ADV-C7-007, BR-08-ADV-C7-008 |
| D1-C4 | Base authorization is instantaneous. It completes once before the handler starts and is not implicitly re-evaluated while that handler is running. | A time boundary crossed after handler entry cannot retroactively change that Execution; a later Execution is evaluated against the later time. | BR-08-ADV-C3-001, BR-08-ADV-C3-002 |
| D1-C5 | Every ancestor's added Constraint expressions participate in the effective conjunction at the target. | A violating grandparent Constraint denies a grandchild even when both intermediate descendants add no Constraints. | BR-08-ADV-C4-001 |

## Failure behavior

Unknown atoms and evaluator failures remain Unknown and fail closed at authority and selection
boundaries. Invalid expression nodes remain Unknown. Draft-0.8 evaluation may short-circuit only
when the remaining children cannot change the truth value; implementations must still retain every
Unknown atom they actually evaluated. A08-D1 evaluates all children to keep diagnostics deterministic.

## Compatibility and proof boundary

The Draft-0.8 entry points are deliberately explicit. Existing `ExecuteAsync`, `World.step`, and
Architecture 0.7 Definition selection continue using whole-expression poisoning. A08-D1 proves
only C7, C3, and C4 over the 11 named vectors in the two native implementations. It does not advance
the status registry, ratify Architecture 0.8, implement C1/C2/C5/C6/C8-C12, or alter C13/C14.
