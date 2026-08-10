# Minimal Architecture 0.8 handoff implementation notes

Status: M6 planning evidence only. Brontide Minimal Stack remains designed for Architecture 0.7;
this note makes no Architecture 0.8 implementation or revocation claim.

## BR-08-ADV-C11-001 representation choice

Minimal currently uses a **resolved parent-reference chain** (`resolved-parent-reference`). Each
immutable `Capability` carries an opaque `Parent: CapabilityReference option`; authorization's
`capabilityChain` resolves each parent through the current immutable `World.Capabilities` map and
conjoins the added Constraints from every resolved record. Ancestor Constraints are neither
flattened into the leaf nor pre-evaluated into a static table.

This supplies a resolver-shaped input to a future C4 audit. It is current Architecture 0.7 behavior,
not accepted Architecture 0.8 evidence.

## Revocation ceiling

The `World` lookup is an explicit resolver point, but the current model has no retirement marker,
revocation tombstone, subtree invalidation transition, or survival policy. Existing immutable World
snapshots also retain their prior maps. Minimal therefore claims **no current post-issuance
revocation semantics**.

A future current-World policy could refuse a missing or tombstoned link naturally at this resolver,
provided every authorization uses that governed current World and defines snapshot disposition.
That possible extension is the representation's revocation ceiling; it is not behavior delivered by
this handoff.

## Portable boundary

The Portable Binding transfers no Capability or `CapabilityReference`. Its boundary-relative
authority context therefore does not freeze Minimal's World-resolved chain representation or imply
cross-domain revocation.
