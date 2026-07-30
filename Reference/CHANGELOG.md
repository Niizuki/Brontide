# Changelog

## Unreleased — Component Management CM2 experimental evidence

### Added

- A deterministic, effect-free recursive resolver that closes finite acyclic selections into an
  inspectable Proposed Stack and immutable generation.
- Occupied-binding stability, preference/publisher/generic/other ranking, policy exclusions,
  lower-bound Provider Sets, explicit optional preselection, occurrence sharing, direct and mediated
  Binding Plans, child Port envelopes, topology decisions, post-closure Activation Parameters, and
  structured refusals.
- The neutral CM2 vector inventory, complete permutation properties, and the completed CM2
  contract-completeness review.

CM2 remains fake Architecture 0.8 experimental evidence. It does not prepare, activate, establish an
Actor, grant authority, mutate an active generation, accept cyclic groups, or claim conformance.

## Unreleased — Component Management CM1 experimental evidence

### Added

- Standard contract/version discovery across zero or more controlled fake sources, with complete
  source-endpoint and publisher attribution, deterministic ordering, duplicate claims, advertised
  package-version observations, and the source-neutral storefront projection.
- Immutable staged acquisition with attributable evidence and fake-policy decisions, four
  structured fail-closed refusal categories, source disappearance, and an explicit observation that
  CM1 performs no selection, resolution, preparation, activation, Actor establishment, or Capability
  grant.
- A separate neutral source/evidence-availability fixture, exhaustive enumeration-permutation
  properties, a falsifiable local/remote storefront comparison, and the completed CM1
  contract-completeness review.

This remains a fake Architecture 0.8 experiment outside Brontide Base. It is not a marketplace,
package manager, loader, security product, conformance claim, or component-version change.

## Unreleased — Portable Component Binding 0.1 experimental evidence

### Added

- `Brontide.Reference.Experimental.Binding.Portable`: the Reference realization of the Portable
  Component Binding contract under [`binding/portable/`](../binding/portable/README.md). It adds a
  deterministic-CBOR core and length-delimited framing, portable references and the Shape floor,
  contract negotiation and a frozen, inspectable Binding Plan, local authority under strong Kleene
  evaluation with frameless denial, referenced resources, an explicit lifecycle with declared
  limits, the Channel envelopes, the C9 observation set, and a fixed direct-call alongside a
  negotiated process realization. `PortableCoreAdapter` is the Reference-owned adapter between the
  stack's `ShapeValue` model and the neutral positions.
- `PortableCompositionHandoff` and `PortableCompositionMember`: the seam by which a resolved
  Component requirement and an offered provision produce a Binding Plan during activation preflight,
  with the ordinary-interaction gate closed until a composition releases the member. Provider Sets,
  mediated exposure, an unselected provider, and a provider substituted by the answering endpoint are
  refused rather than approximated.

The retained line-delimited Cooling and Catalog experiments in the same project are unchanged and
remain diagnostic and legacy. This surface is experimental architecture evidence: it is not part of
Brontide Base, not an Architecture 0.8 conformance claim, not ratified, and not a component-version
change. These repository projects are not independently versioned packages, so no version is bumped.

## Unreleased — Architecture 0.7 Complete Draft evidence

### Added

- `CanonicalMemberName`, `MemberKind`, and `MemberName` value types for the provisional typed-member
  grammar. Existing `CanonicalName` parsing and all current wire contracts remain unchanged;
  `MemberKind` stays open while the architecture's catalogue and final glyph are provisional.

This addition is current-draft evidence, not ratification and not a component-version change.

## Unreleased — Architecture 0.5 implementation correction

### Changed

- Failed dynamic Genesis callbacks now roll back their actors, capabilities, newly issued leases,
  pre-existing mutable lease state, declarations, and Shape registrations before rethrowing.
  Lease reads and renewal are coordinated with the domain transaction, rolled-back leases are
  actively invalidated, other escaped references are rejected after rollback, and runtime effects
  or nested Genesis occurrences cannot run reentrantly inside the transaction. Genesis-context
  activity validation and mutation are atomic, so a concurrent issuer cannot resume after rollback.
- Rejected provenance retains execution metadata but does not retain the submitted protected input.
  Direct `ExecutionResult` records remain complete; audit consumers may inspect `HasInput`.
- A liveness lease remains terminally dead after trusted time observes expiry, even if the supplied
  clock later moves backward.

These repository projects are not independently versioned packages. The provenance behavior is a
security correction to an experimental public surface; future package extraction must choose its
initial compatibility baseline explicitly.
