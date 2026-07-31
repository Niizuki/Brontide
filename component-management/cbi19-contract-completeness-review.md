# CBI19 contract-completeness review

Date: 2026-07-31

Scope: absence review of the CBI19 scoped activation-replacement contract, separate from conformance
review.

## Findings and dispositions

1. **The operation three earlier slices deferred to does not exist.** Disposition: recorded as the
   finding worth carrying forward, and it corrects them rather than extending them. CBI14, CBI15, and
   CBI18 each said that retiring one member while its scope keeps running "is a scoped replacement,
   an operation CM4 declares separately". Reading CM4 to implement it shows scoped replacement targets
   a restart scope holding a *generation*, and its Release makes the successor generation active
   atomically for the whole scope. Nothing in CM4 retires one member and leaves its siblings running.
   So those slices' answer — retire everything — was not a placeholder awaiting this one; it was
   correct, and the forward reference was reaching for a capability the model does not have. A reader
   who followed those pointers expecting relief here would otherwise have concluded the programme had
   simply not got round to it.
2. **What a replacement does to authority admitted against a departing occurrence was the first
   question this item recorded.** Disposition: authority follows the occurrence, and is re-established
   rather than inherited. CBI13 admits against an occurrence *because* an occurrence is durable where
   an activation attempt is not; a replacement is the event that ends an attempt while occurrences may
   persist, so it is the case that justification was written for and never exercised. A surviving
   occurrence must be admitted with a request that re-identifies what admitted it, so a replacement
   cannot be used to quietly change what a surviving occurrence may do; a new occurrence is admitted
   afresh.
3. **Nothing is inherited even when everything matches.** Disposition: deliberate, and stated
   separately from finding 2 because it is the tempting shortcut. A successor member on a surviving
   occurrence with an identical request could have kept its grants without re-evaluation. Refusing
   that keeps one rule — no member is released without its own admission in this attempt — instead of
   two, and means a revocation landing between the two attempts is seen.
4. **Whether the release barrier re-arms for the replacement alone was the second question.**
   Disposition: it re-arms for the whole successor activation, from CM4's shape as CBI12's original
   barrier did. One Release per attempt and one atomic cutover for the scope leave no room for a
   partial one. The question presupposed a per-member replacement, which finding 1 disposes of.
5. **Cutover is the ordering boundary for the retained members.** Disposition: they are retired after
   it and never before. CM4 requires a pre-cutover failure to leave the retained generation active,
   and a retained member that had already been stood down could not honour that. This is the one
   ordering in the slice that a plausible implementation would get wrong in the cheap direction —
   retiring the old members as soon as the successor reports Ready looks tidier and is wrong.
6. **The retained activation is not a rollback target.** Disposition: recorded because the CM4
   vocabulary invites the confusion. A pre-cutover failure does not *restore* the retained
   activation; the retained activation never stopped serving. Rollback in CM4's sense applies only
   after cutover, and CBI19 reports CM4's classification rather than forming its own.
7. **Cleanup failure after a successful cutover does not undo the cutover.** Disposition: the
   successor stays released and the failure is named. The scope has already cut over, so restoring a
   retained member would be the outcome CM4's C7 property forbids — both generations serving.
8. **Two members of the same binding scope exist at once, briefly.** Disposition: accepted and
   bounded. Between the successor reaching Ready and the retained members retiring, the retained and
   successor portable members are both established against the same binding scope. That is what a
   hot replacement is; the contract does not claim the provider distinguishes them, and no state is
   migrated between them.
9. **No position is added or removed.** Disposition: the successor generation resolves the same
   positions. A successor that adds or drops a position is member addition or removal, which remains
   future work and is not reachable through this slice.
10. **Two implementations can still agree where this contract is silent.** Disposition: retained as a
    structural limitation. The vectors force the occurrence-authority rule in both directions, the
    barrier scope, the cutover ordering, and the pre- and post-cutover failure answers; they cannot
    establish general replacement completeness.

## Result

The CBI19 contract is complete for replacing the generation occupying one restart scope over
protocol-free members. Finding 1 is the one to carry forward, because it corrects three earlier
slices' forward references rather than fulfilling them; findings 2 and 4 answer the two questions the
item recorded, the second of which turned out to rest on the premise finding 1 removes. Finding 5 is
the ordering a plausible implementation gets wrong. No finding requires widening this contract into
member addition or removal, Relational Initialisation, child Ports, mediation, real distribution, or
Architecture 0.8 conformance.
