# Reference Architecture 0.8 handoff implementation notes

Status: R6 planning evidence only. Brontide Reference Stack remains designed for Architecture 0.7;
this note makes no Architecture 0.8 implementation or revocation claim.

## BR-08-ADV-C11-001 representation choice

Reference currently uses a **carried parent chain** (`carried-parent-chain`). Each immutable
`Capability` retains a direct `Parent` object and only its locally added Constraint expressions.
At authorization, `DerivationChain()` walks those parent objects and
`EffectiveConstraintExpressions()` conjoins every link. Ancestor Constraints are not flattened into
the leaf, pre-evaluated into a table, or obtained from a cross-process resolver.

This satisfies the current Architecture 0.7 chain behavior and supplies an input to a future C4
audit. It is not accepted Architecture 0.8 evidence.

## Revocation ceiling

The carried objects are immutable and a child keeps its parent reachable. Reference has no
withdrawal registry, per-link liveness tombstone, subtree invalidation operation, or authorization
check against revocation state. Its present revocation ceiling is therefore **no post-issuance
revocation** beyond Constraints already evaluatable at presentation.

Future subtree revocation would require a deliberately introduced indirection point consulted for
every chain link, consistent with Architecture 0.8 §11's recorded revocation-via-indirection
candidate. Replacing a collection entry or losing an external reference would not be sufficient,
because existing children retain their direct parent objects.

## Portable boundary

The Portable Binding does not serialize or transfer this Capability chain. It carries only the
boundary-relative attributable context allowed by its contract, so its encoding does not freeze
Reference's authority representation or enlarge this ceiling.
