# CBI24 contract-completeness review

Date: 2026-08-02

Scope: absence review of the CBI24 attached-replacement contract, separate from conformance review.

## Findings and dispositions

1. **A replacement silently orphans every attachment beneath the generation it replaces, and CM4 does
   it on purpose.** Disposition: the finding to carry forward. CM4's C2 property preserves the
   generation and activity state of every *unrelated* scope, and a child scope is unrelated — on
   cutover the runtime rewrites the target scope and carries every other one through untouched. The
   child keeps running while the `ParentGeneration` its attachment recorded is no longer active
   anywhere, and nothing looks again, because the attachment was validated once at attach time. This
   is not a defect in CM4; it is CM4 declining to model a relationship, which is a different thing
   from CBI23's finding that it models none after attachment.
2. **There is no migration operation, and the item's name was optimistic.** Disposition: recorded.
   Re-pointing an attachment at the successor would need CM4 to hold the declaration as mutable
   state, and it holds it as an input to one activation attempt. A Port does not migrate: a child is
   stood down and stood up again, and the standing-up is the child's own attachment against a
   generation that must already exist. The future index called this "Port migration between
   generations"; what it is, is a cascade and a replacement in a fixed order.
3. **The cascade runs before the cutover, which is the opposite of CBI19's retained members.**
   Disposition: derived, not preferred, and the asymmetry is the interesting part. A retained member
   is *inside* the transaction and CM4 requires a pre-cutover failure to leave it serving, so it goes
   after. An attachment is *outside* the transaction, in a scope CM4 will not touch either way, so
   leaving it up during the replacement would produce exactly the orphan the slice exists to prevent.
   Which side of the boundary a thing lives on decides when it goes.
4. **The supplied set is a forest, and the first draft got that wrong.** Disposition: corrected before
   the contract was finished. The rule started as "every supplied attachment names the retained
   generation", and the two-level vector failed against it, because a grandchild is beneath the
   generation being replaced without being attached to it. The rule is now that each supplied
   activation is attached either to the generation being replaced or to another scope in the set,
   which is the same closure CBI23's ordering already assumes.
5. **A failed replacement does not restore the attachments.** Disposition: deliberate, and it is the
   asymmetry a reader will question. CBI19 guarantees the retained generation keeps serving after a
   pre-cutover failure, so the parent survives while its children do not. Restoring one would be a
   fresh activation against a generation this call did not establish, and calling that a restoration
   would claim a continuity the runtime does not model. What the contract owes instead is reporting:
   every outcome names every scope the cascade retired.
6. **A cascade whose cleanup fails stops before the cutover.** Disposition: replacing on top of it
   would report a cutover whose starting state nobody can describe. This differs from CBI23, where
   the cascade continues past a failure, because there the cascade is the whole operation and here it
   is a precondition for another one.
7. **A caller that does not present its attachments is not detected.** Disposition: stated as the
   hole, and it is the same one CBI23 records from the other side. A replacement's inputs carry no
   record of what is attached beneath the retained generation and the runtime keeps none, so CBI19
   and CBI20 stay reachable with children attached. The named test proves the orphan rather than
   describing it.
8. **Whether CM4 should record a scope's children is not this slice's to decide.** Disposition:
   raised as Decision 14 rather than answered. Two slices have now hit the same missing relation from
   opposite directions — CBI23 could not discover unnamed children to order, and CBI24 cannot detect
   unnamed children to protect — which is the evidence a decision should rest on.
9. **Two implementations can still agree where this contract is silent.** Disposition: retained as a
   structural limitation. The vectors force the forest rule, the ordering, the pre-cascade refusals,
   and the failure answers; they cannot establish general attached-replacement completeness.

## Result

The CBI24 contract is complete for replacing a generation with attachments beneath it, over
protocol-free members. Finding 1 is the one to carry forward, and finding 3 is the reasoning worth
keeping: the same programme retires one thing after cutover and another before it, and the rule that
decides which is whether CM4 considers the thing part of the transaction. Finding 7 is the honest
hole, now raised as Decision 14. No finding requires widening this contract into re-attaching on the
caller's behalf, mediation, or Relational Initialisation, which remains blocked on Decision 13.
