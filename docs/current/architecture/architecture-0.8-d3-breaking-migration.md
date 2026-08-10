# Architecture 0.8 A08-D3 breaking migration

Status: authorized experimental migration for A08-D3; Architecture 0.8 remains a Complete Draft.

## Decision

A08-D3 separates payload compatibility from authority evaluation. Constraint types now have
first-class declarations and deterministic recognition decisions. Constraint values carry an exact
Shape/version and are never additively projected. Operation payloads retain additive projection.

This is a public breaking change even though both stacks retain their Architecture 0.7 implementation
target. It does not change the status registry or pinned conformance matrices.

## Reference migration

- Replace `GenesisContext.Constraint(name, valueShape, evaluator)` with
  `GenesisContext.Constraint(ConstraintDeclaration.Create(name, valueShape, semantics), evaluator)`.
- Pass no evaluator to record a known but deliberately declined Constraint declaration.
- Read `AuthorityDomain.ConstraintRecognitionSet` for deterministic implemented/declined evidence.
- A Constraint value must now use the declaration's exact `ShapeReference`; later additive versions
  evaluate Unknown rather than being projected. Write `AnyOf(NewConstraint, OldConstraint)` when a
  vocabulary needs an authored version-skew fallback.
- Operation input remains projected through the ordinary Shape registry and is unaffected.

## Minimal migration

- Add `ParameterShape` to every `ConstraintRequirement`. It is the Shape/version actually presented
  with the authority value, not an inferred copy of the evaluator's accepted version.
- Add `PresentedCommandShape` to `Draft08ExecutionRequest`. Existing same-version requests normally
  use the Operation's declared command Shape.
- Use `World.registerConstraintDeclaration` for explicit declaration metadata. The retained
  `World.registerConstraint` convenience function creates the fixed target-authority, deny-on-unknown,
  non-quantified, parallel-name declaration for version 1.
- Use `World.constraintRecognitionSet environment world` for implemented/declined evidence.
- Later additive Constraint value versions deny without invoking the evaluator; later additive
  payload versions may still project to the Operation's accepted version.

## Compatibility boundary

The new fields are required because Minimal's former raw `ShapeValue` carried no Shape identity, so
it could not distinguish a version-two authority value from version one. Defaults or inference would
recreate the ambiguity C8 removes. The ordinary Architecture 0.7 execution entry point remains
available; the shaped payload wrapper is confined to the experimental Draft-0.8 path.
