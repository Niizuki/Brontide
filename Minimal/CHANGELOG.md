# Changelog

## Unreleased — Architecture 0.5 implementation correction

### Breaking

- Issuer-controlled Actor, Capability, Constraint, Execution, Occurrence, and Activity references
  no longer expose public record construction. Carry references returned by `Genesis`, `World`, or
  execution APIs instead of constructing scope/value records.
- `World.create` now requires an explicit `TimeDomainReference`; execution receives a trusted
  `TemporalMark` from the host.
- `ExecutionRequest` now requires `Initiator`, `Target`, and `PresentedCapability`. Migrate callers
  from ambient grant/step helpers to `World.step environment world request`.
- `OperationDefinition` now declares its target Actor. Capability issuance records holder, target,
  operation scope, constraints, parent, issuer, and delegation permission; use
  `World.delegateCapability` to narrow authority.
- Operation handlers return `OperationFailure` rather than text. Use
  `OperationFailure.withoutDetails` or `OperationFailure.withDetails` so failure details have an
  independently validated Shape.
- Operation and Event identity is name-only. Remove semantic version arguments; Shape and Fragment
  references remain versioned.

These projects are repository components rather than independently published packages, so this
change has no package-version field to bump. Any future package extraction must choose its initial
version and treat the corrected API as the baseline.

### Added

- Attributed terminal Outcome Events, redacted execution audits, Genesis occurrence records, and
  authority-qualified canonical names.
- Independent Catalog/resource process binding, strict adversarial vectors, replay and payload
  controls, and reproducible binding source-cost measurements.
