# Architecture 0.8 A08-D1 completeness review

Reviewed: 2026-08-10

This review asks what the A08-D1 contract and its 11-vector conformance pass could otherwise leave
silent. A08-D1 is experimental Complete-Draft evidence; it does not change either stack's target.

| Area | Potentially silent question | Disposition |
| --- | --- | --- |
| 0.7 compatibility | Does implementing C7 silently invalidate the pinned 0.7 poisoning evidence? | No. Both stacks retain their original evaluator, execution, and selection entry points and their 0.7 tests. Draft-0.8 behavior is explicit. |
| Determined expressions with Unknown children | Is an Unknown atom lost when `AnyOf(True, Unknown)` becomes True or `AllOf(False, Unknown)` becomes False? | No. Both native evaluator results retain the normalized Unknown names even when the truth value is determined. |
| Structural evaluation | Could `AnyOf(X, Not(X))` be simplified classically? | No. Both evaluators recursively evaluate positions without correlating repeated atoms; the named vector denies. |
| Diagnostic determinism | Could child order change the Unknown explanation? | Names are distinct, ordinally sorted, and deduplicated in both stacks. All children are evaluated for deterministic observations. |
| Authority silence | Could False or Unknown reach an effect? | No. Six authority vectors per stack observe zero effects on every non-True path; the True positive control reaches exactly one effect. |
| Selection explanation | Does a retained candidate hide an Unknown branch? | No. The new assessment collection covers eligible and rejected candidates, so C7-007 records the Unknown while retaining the candidate. |
| Instantaneous authorization | Does validity expiry during a handler retroactively change the running Execution? | No. Each stack evaluates before dispatch, crosses the boundary inside the handler, and completes that Execution; a separately named later-execution vector denies. |
| Minimal time model | Did Minimal gain an ambient clock to imitate Reference? | No. Its test injects the changing validity observation through the existing evaluator closure; `World.stepDraft08` remains deterministic over explicit environment and evaluator inputs. |
| Chain representation | Do both stacks prove C4 by sharing one representation? | No. Reference walks carried parent objects; Minimal resolves opaque parent references through `World`. Both independently deny at grandchild depth. |
| Delegation and origin | Does D1 accidentally claim C6 or C2? | No. Boolean delegability and current origin handling are unchanged; A08-D2 remains the next proposed breaking slice. |
| Architecture status | Does executed experimental evidence ratify 0.8 or change the stack target? | No. The status registry and `Designed for` declarations remain unchanged. |

No additional capability is required inside A08-D1's C7/C3/C4 boundary. The next separately
authorized slice is A08-D2.
