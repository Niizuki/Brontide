# CBI14 contract-completeness review

Date: 2026-07-30

Scope: absence review of the CBI14 multi-member revalidation and withdrawal contract, separate from
conformance review.

## Findings and dispositions

1. **What one member's lapsed authority does to the others was the question CBI13 left.**
   Disposition: the whole activation retires, and the answer comes from CM4 rather than from
   preference. A CM4 activation has exactly one restart scope and every member of a CBI12 activation
   is inside it; CM4 models no way to retire one member while its scope keeps running, because doing
   that is a scoped replacement — an operation it declares separately. The members came up together
   inside one scope and they go down together.
2. **Independence looked like an argument for retiring only the lapsed member.** Disposition: it is
   not. CBI12's members are independent in what they need from each other — separate positions,
   contracts, conversations, and plans — and that says nothing about what scope they share. Treating
   independence as a fate argument would have imported a conclusion from the wrong premise, which is
   why the contract states the distinction rather than leaving it implicit.
3. **A changed set of members could have passed as a revalidation.** Disposition: membership is
   compared before anything is evaluated. A different set of members is not this activation, whatever
   its requests would say.
4. **Evaluating members one at a time could have left a prefix evaluated.** Disposition: the
   all-or-none rule CBI6 and CBI13 established holds across the activation, and the vectors pin zero
   evaluations for both structural refusals.
5. **The cause of a withdrawal could have been indistinguishable from its consequence.**
   Disposition: the result names the members whose authority lapsed and, inside each, the
   participants that caused it. A member retired only because a sibling lapsed is retired without
   being named as lapsed, and each stack asserts that its unrenewed list is empty.
6. **Cleanup failure could have reopened a member or hidden a partial retirement.** Disposition:
   every member's gate closes before its withdrawal traffic; replacement records are reported for the
   members that retired cleanly, and a cleanup failure is reported without fabricating one for the
   member whose peer state is unknown. The retirement-failure vector pins one replacement out of two.
7. **A revalidation of an unavailable activation could have created effects.** Disposition: only a
   released CBI13 result with every member admitted is eligible; every other input returns
   activation-unavailable with no evaluation and no retirement attempt.
8. **This is not a scoped replacement.** Disposition: recorded as a limit. Retiring an activation
   and standing up a replacement inside the same scope is what CM4's scoped replacement models, and
   nothing here performs it. Finding 1's reasoning is what makes the boundary meaningful rather than
   arbitrary.
9. **The remaining single-member slices did not follow.** Disposition: CBI8 through CBI11 —
   extension, revision, verification, and succession — still govern one member. CBI14 covers only
   revalidation and withdrawal, and does not extend the others by implication.
10. **Two implementations can still agree where this contract is silent.** Disposition: retained as
    a structural limitation. The vectors force the shared-fate, membership, all-or-none,
    attribution, and cleanup answers; they cannot establish general multi-member authority lifecycle
    completeness.

## Result

The CBI14 contract is complete for revalidating and withdrawing one multi-member, protocol-free
activation. Finding 1 answers what CBI13 left open, and finding 2 records the argument that was
rejected and why. Finding 9 is what the programme still owes: four single-member slices that have
not been lifted. No finding requires widening this contract into scoped replacement, Relational
Initialisation, mediation, real distribution, or Architecture 0.8 conformance.
