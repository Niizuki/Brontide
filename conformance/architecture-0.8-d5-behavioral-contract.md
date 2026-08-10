# Architecture 0.8 A08-D5 behavioral contract

Status: experimental runtime delivery contract for the separately authorized A08-D5 slice.

This contract delivers C10 provider resource-Capability issuance by derivation through the
experimental Persistent Information Dataset seam. It does not change either stack's Architecture
0.7 target or ordinary 0.7 execution entry point.

## Capabilities

### D5-C1 — successful creation derives resource authority from the provider

An authorized creating Execution may issue a Dataset Capability to its initiator only by Delegation
from an explicit Capability held by the creating provider over the same target resource space. The
issued Capability inherits the provider Capability's target, Operations, and complete ancestor
conjunction and adds an exact Dataset designation.

Property: every successful Dataset authority issuance has the requester as holder, the creating
provider as issuer, and the explicit provider Capability as its immediate parent; issuance never
constructs a new primordial grant.

Evidence: `BR-08-ADV-C10-001` in each native D5 conformance suite.

### D5-C2 — issuance remains an ordinary, attributable Delegation record

The Dataset issuance result records the authorized Execution, provider authority, created Dataset,
and derived resource Capability. Following the parent relationship from that Capability reaches the
provider's complete chain and terminates in its primordial grant.

Property: every successful issuance adds exactly one derived Capability whose recorded parent and
issuer identify the provider Delegation; it adds no Genesis occurrence and no unrelated authority.

Evidence: `BR-08-ADV-C10-001` in each native D5 conformance suite.

### D5-C3 — provider scope is checked before the creating effect

Every Dataset-space Constraint in the provider authority chain is evaluated against the requested
Dataset designation before the creating Operation or Dataset registry effect. A designation outside
any ancestor space, an authority not held by the creating provider, or an authority for another
target is refused.

Property: every refused provider-authority preflight preserves the Dataset registry, Capability set,
Store observations, and every other resource effect owned by the issuance coordinator.

Evidence: `BR-08-ADV-C10-002` in each native D5 conformance suite.

## Phase boundary

- D5 is an experimental Persistent Information integration over the explicit Draft-0.8 authority
  paths; it does not make Dataset a Base concept.
- Dataset-space syntax is owned by the experimental component and narrows authority only; it cannot
  expand the parent's target or Operation set.
- Reference records Delegation through the registered carried Capability; Minimal records the same
  parent and `IssuedBy` relation in its resolved `World` representation.
- The status registry, hash-pinned Architecture 0.7 matrices, and both `Designed for` declarations
  remain unchanged.
- Terminus (A08-D6/C12) requires separate authorization.
