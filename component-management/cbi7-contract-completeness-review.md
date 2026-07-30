# CBI7 contract-completeness review

Date: 2026-07-30

Scope: absence review of the CBI7 participant-set revalidation and withdrawal contract, separate
from conformance review.

## Findings and dispositions

1. **The central question was what a shared member does when one participant of several loses
   authority.** CBI6 recorded it as undecided rather than answering it in passing. Disposition:
   the member is retired. Narrowing the set is refused because nothing in an admitted set says
   which participants the member's ordinary interaction depends on — the set is unordered, none is
   marked required, and the member declares no dependency on particular grants — so continuing
   would decide that invisibly. A caller that wants a smaller set admits one through CBI6.
2. **A retirement decided by one participant's loss could look like that participant's grants were
   worthless.** Disposition: the result names exactly which participants failed to renew, and each
   retained current observation is complete, so a still-admitted participant is visible as
   still-admitted next to the retirement it did not cause. A test asserts precisely that state.
3. **A changed participant set could pass as a renewal.** Disposition: membership is compared
   before any evaluation. Added, removed, substituted, or repeated participants retire the member
   without evaluating a single request, because a different set is not a renewal of the old one
   regardless of what its requests would say.
4. **Dropping or adding a grant inside an otherwise identical request could pass as a renewal.**
   Disposition: each fresh request must carry exactly one authority request per prior grant with
   identical local identity and tuple. Grant count and grant identity are both part of what is
   being renewed.
5. **Evaluating requests one at a time could leave a prefix of the set evaluated.** Disposition:
   the all-or-none rule CBI6 established is preserved: either every participant is evaluated or
   none is, and the shared vectors pin the evaluated count for every scenario, including zero for
   the structural refusals.
6. **Ambient time or evidence could make withdrawal nondeterministic.** Disposition: the caller
   supplies a complete fresh request per participant, including evaluation time, evidence states,
   and policy. CBI7 reinterprets none of them.
7. **Peer cleanup failure could reopen or preserve authority-backed activity.** Disposition: PB7
   retirement closes the local gate before `Withdraw` and `Terminate`. Cleanup failure is visible,
   the member stays retired, and no replacement record is fabricated; each stack attempts an
   ordinary Operation after every non-continued outcome and requires a state refusal with no
   provider effect.
8. **Revalidating a refused or incomplete set could create effects.** Disposition: only a complete
   successful CBI6 result with a released Active member is eligible. Every other input returns
   activation-unavailable with no evaluation, no retirement attempt, and no provider contact.
9. **Withdrawal could mint replacement authority.** Disposition: CBI7 returns the fresh
   observations, the unrenewed participants, and the retirement outcome. It creates no replacement
   relationship, grant, portable constraint, Binding Plan fact, or operation payload.
10. **The slice could be read as revocation propagation.** Disposition: CBI7 governs whether this
    receiving domain keeps admitting subsequent ordinary interaction over one member. It makes no
    claim about propagating a revocation to other domains, pre-empting in-flight execution,
    compensating completed work, or replacing a participant in place.
11. **Two implementations can still agree where this contract is silent.** Disposition: retained as
    a structural limitation. The vectors force the membership, identity-drift, grant-count,
    evaluated-count, unrenewed-count, and cleanup-failure answers; they cannot establish general
    multi-party authority lifecycle completeness.

## Result

The CBI7 contract is complete for exact revalidation and fail-closed withdrawal of one participant
set over one released singleton binding. Finding 1 is the decision CBI6 deferred and is now closed;
finding 10's alternatives — participant replacement in place, precedence between participants, and
revocation propagation — remain future work rather than gaps in this contract. No finding requires
widening it into CM4 binding-exercise projection, cross-vocabulary Operation mapping, multi-member
or relational lifecycles, mediation, real distribution, or Architecture 0.8 conformance.
