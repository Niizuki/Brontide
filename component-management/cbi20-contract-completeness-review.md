# CBI20 contract-completeness review

Date: 2026-07-31

Scope: absence review of the CBI20 membership-replacement contract, separate from conformance review.

## Findings and dispositions

1. **CBI19 did not enforce the membership limit it declared.** Disposition: corrected, and it is the
   finding to carry forward. CBI19's C1 names "one entry per successor member" and its C10 says no
   position is added or removed; nothing in either stack compared the supplied members with the
   positions the successor generation resolves, so a caller passing a strict subset — with a CM3 plan
   built from that same subset, which CM4 then validates against itself — cut a scope over to a
   generation whose plan covered fewer members than CM2 resolved. The retained members were all retired,
   so the dropped Component vanished without any refusal. CBI19's vectors are structurally unable to
   catch it: each one derives the member list, the participant sets, and the plan from one declaration,
   so no vector ever asked whether they agreed. This is the class of defect Decision 10 was recorded
   for, and it surfaced the same way PB7's provider-identity defect did — by pointing a new consumer at
   a layer six slices had used without asking it this question. Both stacks now refuse an
   under-supplied membership, an over-supplied one, and a changed one.
2. **What a dropped position does to authority admitted against its occurrence was the first question
   this item recorded.** Disposition: nothing follows it, because there is nowhere for it to follow to.
   CBI19 admits against an occurrence and re-establishes rather than inherits, so a grant that is not
   re-established in this attempt is simply not in force in the successor. The question reads as though
   it needed a new rule and it needed none; what it needed was for the departure to be visible, which
   C2's derived sets supply.
3. **An explicit withdrawal for the dropped occurrence's grants was considered and refused.**
   Disposition: recorded separately from finding 2 because it is the plausible mistake. Performing a
   revocation or withdrawal against the receiving domain on drop would imply the grant had been carried
   across the replacement, which contradicts CBI19's rule that nothing is inherited, and would give
   CBI20 an effect on local authority records that no other slice has. A dropped occurrence's grants
   end by not being renewed, exactly as a lapsed participant's do in CBI14.
4. **Whether an added position may join a released activation was the second question.** Disposition:
   only across a cutover, and the answer is the runtime's. A CM2 generation is one immutable object
   resolving every position at once, and a CM4 attempt carries one plan covering every member with one
   atomic cutover; neither can represent a member arriving into a generation already serving. The
   question presupposed that an in-place path might exist, and what it was reaching for is CBI18 — which
   grows participant sets inside existing members precisely because that changes no generation.
5. **An emptied membership is not a replacement.** Disposition: refused, and stated because the contract
   would otherwise be silent about it. A successor resolving nothing would cut a scope over to a
   generation with no member to release, which is CBI14's withdrawal reached through the wrong door and
   with none of its reporting. This is the one refusal in the slice with no analogue in CBI19.
6. **A local Actor a dropped participant held may be taken by a different party in an added member.**
   Disposition: admitted, and it is the case only a changed membership can pose. CBI13's mapping rule is
   a property of an activation, the retained activation's mapping ends with it, and the successor's
   mapping is checked as a function and injective within itself. Refusing the reuse was considered: it
   would require checking injectivity across the retained and successor activations together, which
   invents a rule CM5 does not state and which the pre-cutover window — CBI19's finding 8, where both
   memberships briefly exist — would make arbitrary. The same reuse against a *surviving* member's
   participant stays refused, and both directions have a vector.
7. **The two barriers stay where CBI13 put them, and a changed membership does not move them.**
   Disposition: recorded because an addition looks like a reason to admit later. Authority is admitted
   per member before any provider is contacted, including for an added member, and Release still waits
   for every successor member; an added member whose admission is denied therefore reaches no provider
   at all. Nothing about the member being new makes its authority an activation-level question.
8. **Cutover ordering is unchanged, including for the members whose positions are dropped.**
   Disposition: they are retired after cutover with every other retained member, never before. This is
   CBI19's finding 5 under a new input, and the cheap wrong answer is more tempting here: a member whose
   position the successor does not resolve looks like one that could be stood down early, and doing so
   would break CM4's requirement that a pre-cutover failure leave the retained generation serving.
9. **A membership replacement is reported, not merely performed.** Disposition: the result names the
   added, dropped, and surviving occurrences, so a member retired because its position is gone is
   distinguishable from one retired because its generation was replaced. Without it the two are the same
   observation, which is what let finding 1 stay invisible.
10. **A CBI20 call whose membership is unchanged is its CBI19 call.** Disposition: true by construction,
    because CBI20 delegates the cutover to CBI19's core rather than restating it, and therefore *not*
    asserted as a test — an assertion no implementation could fail is a finding against the assertion,
    as CBI17's first draft was. It is recorded here instead, as the reason CBI19 keeps its own entry
    point rather than being replaced by this slice.
11. **Two implementations can still agree where this contract is silent.** Disposition: retained as a
    structural limitation. The vectors force the membership-versus-generation rule in both directions,
    the derived sets, the cutover-only rule for an addition, the emptied-membership refusal, the Actor
    reuse answer in both directions, and the pre- and post-cutover failure answers; they cannot
    establish general membership-replacement completeness.

## Result

The CBI20 contract is complete for replacing the generation occupying one restart scope with a successor
generation resolving a different set of positions, over protocol-free members. Finding 1 is the one to
carry forward, because the lift's first result was a defect in the slice it lifts rather than a new
capability. Findings 2 and 4 answer the two questions the item recorded, and both turned out to need no
new rule — the first because CBI19 decided authority at the right granularity, the second because the
runtime cannot represent the alternative. Findings 5 and 6 are the two things only a changed membership
could raise. No finding requires widening this contract into Relational Initialisation, child Ports,
mediation, wider Provider Sets, real distribution, or Architecture 0.8 conformance.
