# Architecture 0.8 A08-D3 completeness review

Reviewed: 2026-08-10

This review asks what the six canonical C8/C9 vectors could otherwise leave silent. A08-D3 remains
breaking experimental evidence and does not retarget either implementation.

| Area | Potentially silent question | Disposition |
| --- | --- | --- |
| Declaration completeness | Can evaluator code exist without authority-plane metadata? | Standard and vocabulary Constraints are stored through `ConstraintDeclaration`. The retained Minimal convenience function constructs the complete fixed declaration rather than a second representation. |
| Decline versus omission | Can a domain claim a deliberate decline when it cannot name the declaration? | No. A decline row exists only for a registered declaration. Unknown internal references fail closed but do not count as recognition evidence. |
| Diagnostic secrecy | Does naming a declined Constraint reveal its value? | No. Both C9-001 tests use a sensitive value and assert that the denial names only the canonical Constraint type. |
| Recognition ordering | Can map or registration order make evidence unstable? | No. Both recognition sets sort by canonical name, include standard declarations, and are observed twice without invoking evaluators. |
| Recognition authority | Does appearing as implemented in the set grant or pre-authorize anything? | No. Recognition is an observation over declarations and evaluator availability. Presentation still evaluates every carrying atom through the full chain. |
| Same-name evolution | Is only duplicate registration rejected, leaving semantic drift undiagnosed? | Changed Shape/version or semantics receives the explicit new-canonical-name failure in both stacks; identical duplication is separately reported as already declared. |
| Evaluator invocation | Can a version-two value be projected before the evaluator notices? | No. C8-001 counts evaluator calls and effects; both remain zero. Exact Shape/version validation precedes dispatch. |
| Authored Fragments | Can an extra authority-plane Fragment bypass the version check? | Reference exact validation rejects projected-away or version-skewed Fragments. Minimal rejects presented Fragments outside the exact registered Shape's accepted set. |
| Fallback soundness | Can Unknown itself become the authorizing branch? | No. C8-002 observes zero calls to the version-skewed evaluator and exactly one satisfied old-branch call before one effect. |
| Payload validity | Does projection ignore malformed later structure without first validating it? | No. Minimal validates the complete presented Shape/version before projection; Reference's Shape registry already does the same. The handlers see only their accepted canonical fields. |
| Ordinary 0.7 behavior | Did Shape-aware Draft-0.8 payload input silently retarget `World.step`? | No. Ordinary Minimal execution continues validating against the Operation Shape directly. Reference already carried Shape identity in all values and retains its ordinary entry point. |
| Deferred accounting | Does declaring an accounting scope implement quantified budgets? | No. Declarations carry the metadata required by C9; A08-D4 owns occurrence-pooled accounting and denial where a declared scope cannot be enforced. |

No additional capability is required inside A08-D3's C9/C8 boundary. The next separately authorized
slice is A08-D4: liveness-scoped ancestor evaluation and occurrence-pooled quantified accounting.
