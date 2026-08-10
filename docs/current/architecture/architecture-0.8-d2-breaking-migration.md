# Architecture 0.8 A08-D2 breaking migration

Status: authorized experimental migration for A08-D2; Architecture 0.8 remains a Complete Draft.

## Decision

A08-D2 removes delegability as a separately granted Boolean. Capabilities are delegable by default,
and further Delegation is narrowed only by ordinary Constraints that conjoin along the derivation
chain. Every derivation also adds the ordinary `Origin.Derived` ceiling Constraint.

This is a public breaking change even though both stacks retain their overall Architecture 0.7
implementation target. The new surface is experimental Draft-0.8 evidence and does not change the
status registry or pinned conformance matrices.

## Reference migration

- Remove the `delegable` argument from `GenesisContext.Grant` and `GrantExpressions` calls.
- Stop reading `Capability.DelegationAllowed`; that property no longer exists.
- Replace `delegable: false` with `new DelegationDepthConstraint(0)` in the grant's Constraints.
- A call to `Capability.Delegate` remains structural. If an ancestor delegation-depth Constraint is
  exceeded, presenting the returned descendant through `ExecuteDraft08Async` is denied before effects.
- Every returned descendant contains an implicit `OriginCeilingConstraint(OriginClass.Derived)`.

## Minimal migration

- Remove the Boolean argument immediately before `world` from `Genesis.capability` and
  `Genesis.capabilityWithExpressions` calls.
- Stop reading the removed `Capability.DelegationAllowed` field.
- Resolve the standard delegation-depth definition with
  `World.tryFindConstraintByName BuiltIn.delegationDepthConstraintName`, then add a requirement with
  `IntegerValue 0L` to replace a former `false` argument.
- `World.stepDraft08` accepts a `Draft08ExecutionRequest`, pairing the ordinary request with a typed
  requested origin. Use `OriginClass.Unverified` when no origin assertion is exercised.
- Every returned descendant contains an implicit standard origin-ceiling requirement for
  `OriginClass.Derived`.

## Compatibility boundary

The ordinary Architecture 0.7 strong/poisoning evaluator split from A08-D1 remains intact. The
breaking change is limited to Capability construction and representation plus the experimental
Draft-0.8 request wrapper. No compatibility Boolean is retained because doing so would preserve the
separate right that C6 explicitly removes and allow callers to keep expressing the wrong algebra.
