# CBI23 contract-completeness review

Date: 2026-08-02

Scope: absence review of the CBI23 nested child-Port contract, separate from conformance review.

## Findings and dispositions

1. **The capability existed before the slice did, and CBI22 said so accurately.** Disposition:
   recorded as the one place in four slices where a stated limit was right. CBI22's review said a
   child of a child "is not reached... nothing in this slice forbids it, but no vector exercises it
   and the contract does not claim it", and that is exactly what was true: the first nesting test
   passed against unmodified code. What CBI23 adds is the claim, the vectors, and the question the
   chain forces.
2. **CM4 models no relationship between parent and child after attachment.** Disposition: the finding
   to carry forward. It requires the parent scope active at attach time and preserves it through the
   activation, and that is all — no runtime record that a scope has children, and nothing that stands
   a child down when its parent goes. Every earlier slice could take the runtime's shape as the answer
   to an ordering question; here there is no shape to take, so the ordering is the composition root's
   and the contract has to say which part of it the root can see.
3. **A child is retired before the parent whose Port it occupies.** Disposition: deepest-first, and it
   is derived rather than preferred — an attachment occupies a Port *of a generation*, so it cannot
   outlive the generation offering the Port. CBI22's independence claim is not contradicted, because
   that claim was one-directional: a child's activation does not disturb its parent. The reverse
   direction is only askable once a chain exists.
4. **The root can only order what it is given, and that is a real hole.** Disposition: stated in the
   contract rather than implied away. A child the caller omits is invisible, so the cascade will retire
   its parent without knowing. Deriving the full forest would need a record of a scope's children that
   neither CM2 nor CM4 keeps. The mitigation is reporting: every outcome names exactly the scopes it
   retired, so what was not ordered is visible by absence.
5. **A cycle in the relation is unreachable through any sequence of attachments.** Disposition:
   reported rather than refused, and no vector manufactures one — the PB treatment of
   `peer-unavailable`. Each attachment needs a released activation as its parent and records that
   parent's scope, so a cycle would need an activation to exist before the one it hangs from. The
   guard remains because the ordering must terminate on any input, and naming the cycle is how it says
   it cannot order the set. The first draft of the vectors tried to build one and produced a duplicate
   scope instead, which is what established the unreachability.
6. **Depth is not bounded, and the reason is that no model bounds it.** Disposition: answered rather
   than deferred. A limit would be a number this programme invented, which is what CBI11 refuses for
   elapsed time and interaction counts. What is bounded is the shape: a finite forest with a
   terminating order. A fourth level is exercised to show the second was not special.
7. **An attachment beneath a retired parent needs no new rule.** Disposition: CBI22's precondition
   already refuses it, because a retired activation is not a released one. Recorded because the chain
   makes it look like a new case and it is not.
8. **The cascade continues past a cleanup failure.** Disposition: consistent with CBI19's
   post-cutover cleanup failure. Stopping would leave the caller with a half-ordered tree and no
   statement about the rest; restoring an already-retired level would claim a state the runtime does
   not model. The failure is reported against the scope it happened in.
9. **Nothing here models traffic between levels.** Disposition: retained, and it is the same
   limitation CBI21 and CBI22 both record. A child that must call its parent does so through the
   composition root, because the portable seam binds a host to a provider.
10. **Two implementations can still agree where this contract is silent.** Disposition: retained as a
    structural limitation. The vectors force the per-level rules, the cascade order, the partial-set
    answer, and the cleanup behaviour; they cannot establish general nesting completeness.

## Result

The CBI23 contract is complete for nesting attachments and withdrawing an attachment forest the caller
supplies. Finding 2 is the one to carry forward: this is the first slice whose ordering question the
runtime does not answer, so the answer had to be derived from what an attachment *is* rather than from
what CM4 models. Finding 4 is the honest hole, and finding 5 is the guard that exists for termination
rather than for a caller. No finding requires widening this contract into discovering unnamed children,
Port migration, mediation, or Relational Initialisation, which remains blocked on Decision 13.
