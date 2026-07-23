# Changelog

## Unreleased — Architecture 0.7 Complete Draft evidence

### Added

- `CanonicalMemberName`, `MemberKind`, and `MemberName` value types for the provisional typed-member
  grammar. Existing `CanonicalName` parsing and all current wire contracts remain unchanged;
  `MemberKind` stays open while the architecture's catalogue and final glyph are provisional.

This addition is current-draft evidence, not ratification and not a component-version change.

## Unreleased — Architecture 0.5 implementation correction

### Changed

- Failed dynamic Genesis callbacks now roll back their actors, capabilities, liveness leases,
  declarations, and Shape registrations before rethrowing. Escaped references are rejected after
  rollback, and runtime effects or nested Genesis occurrences cannot run reentrantly inside the
  transaction.
- Rejected provenance retains execution metadata but does not retain the submitted protected input.
  Direct `ExecutionResult` records remain complete; audit consumers may inspect `HasInput`.
- A liveness lease remains terminally dead after trusted time observes expiry, even if the supplied
  clock later moves backward.

These repository projects are not independently versioned packages. The provenance behavior is a
security correction to an experimental public surface; future package extraction must choose its
initial compatibility baseline explicitly.
