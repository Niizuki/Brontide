# CBI28 contract-completeness review

Date: 2026-08-02

Scope: absence review of the CBI28 fanned-out-activation contract, separate from conformance review.

## Findings and dispositions

1. **Nothing downstream needed teaching, and that is the result rather than a convenience.**
   Disposition: recorded. A wide position's members are distinct occurrences, and every slice from
   CBI12 onward is per-occurrence — CM3 plans occurrences, CM4 stages them, CM5 admits against them,
   CBI16 attributes interaction per member. The one thing missing was the one CBI27 found CM2 does not
   supply, a binding scope per member, so the entry point takes it and the rest follows unchanged.
2. **A wide position can be supplied half-complete and both existing checks pass.** Disposition: the
   finding, closed here. CBI12 refuses a member the plan does not carry and a planned member the
   activation did not select, but both compare **the caller's member list with the caller's plan**, and
   a caller who omits one member of a position and builds the plan from the rest satisfies both. The
   position would come up short-bound with no refusal anywhere. Routing a wide position through CBI27
   as a whole makes the generation the authority, and a named test in each stack shows the activation
   succeeding when that check is removed.
3. **The check is against the generation for the same reason CBI20's is.** Disposition: consistent by
   construction rather than by restatement — the refusal a caller receives is CBI27's own
   `membership-not-resolved`, because the wide path calls CBI27 rather than reimplementing its rule.
4. **The position's declared minimum is not a runtime concept.** Disposition: stated, with the
   alternative named. `Cardinality.Minimum` says a `1..3` position is satisfied by one provider, which
   makes "keep serving with two of three" look reachable. It is not: CM2 uses the number at resolution
   and then stops carrying it, the required-versus-optional split survives only as a decision in the
   Proposed Stack rather than as a fact about a member, and neither CM3's plan nor CM4's attempt has
   any notion of an optional member. A runtime that wanted to run degraded could not tell which
   members it may lose. This is CBI27's C7 exercised rather than asserted.
5. **Where the split does survive is worth naming precisely.** Disposition: recorded. The Proposed
   Stack's decisions distinguish `required-provider-selected` from `optional-provider-preselected`, so
   the information exists — as provenance about how the generation was formed, keyed by requirement
   and definition. A runtime reading it to decide what it may drop would be taking lifecycle policy
   from a diagnostic, and the resolved generation the activation actually consumes does not carry it
   at all.
6. **Scope distinctness stops at the position, and the asymmetry is deliberate.** Disposition: bounded
   by Decision 16. Two ordinary positions resolved in one CM binding scope already reach the seam as
   two members reporting one scope, so an activation-wide check would refuse what CBI1 has produced
   since the first multi-member slice. The contract states this rather than leaving the narrower check
   looking like an oversight, and a property re-pins the collision here.
7. **An activation member gained a field, which is breaking in one stack and not the other.**
   Disposition: accepted and marked. F# requires every field at construction, so Minimal's record
   change touches every existing construction site; C#'s optional parameter does not. The migration is
   the same in both: leave the scope absent for a member of a `1..1` position, which is every member
   every earlier slice activates.
8. **A wide position in a child Port is not exercised.** Disposition: bounded. CBI22's Port containment
   runs before preparation and is per selection, so a Port-contained wide position would be routed to
   the child path and then fanned out there; nothing in either rule contradicts the other, and no
   vector combines them. Stated rather than claimed to work.
9. **Two implementations can still agree where this contract is silent.** Disposition: retained as a
   structural limitation. The vectors force the scope rule in both directions, the whole-position rule,
   the barrier over a wide position, per-member admission, and the mixed activation; they cannot
   establish general Provider Set activation completeness.

## Result

The CBI28 contract is complete for activating a fanned-out position over the fake runtime. Finding 2 is
the one to carry forward — a rule checked against another thing the caller supplied is not checked —
and finding 4 is the one a later reader is most likely to reach for, because a declared minimum looks
like permission to run degraded and is not. Findings 6 and 8 are the stated bounds. No finding requires
widening this contract into deciding set satisfaction, filling optional capacity, or teaching the seam
what a Provider Set is.
