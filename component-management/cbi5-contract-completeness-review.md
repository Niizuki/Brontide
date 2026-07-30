# CBI5 contract-completeness review

Date: 2026-07-30

Scope: absence review of the CBI5 post-activation authority-revalidation contract, separate from
conformance review.

## Findings and dispositions

1. **A status-only revocation could leave an already released portable gate open.** Disposition:
   every failed revalidation retires the PB7 member; tests attempt an ordinary Operation afterward
   and require a state refusal with no handler effect.
2. **A replacement authority request could masquerade as continuity.** Disposition: the
   revalidation request must preserve the original request identity, participant, relationship
   request and kind, authority request, Capability, target Actor, Operation, and scope.
3. **A newly admitted but different local relationship or grant could be treated as renewal.**
   Disposition: continuation requires the evaluator to reproduce the exact local relationship and
   grant recorded by CBI3, including policy and rule attribution.
4. **Ambient time or evidence could make withdrawal nondeterministic.** Disposition: the caller
   supplies a complete fresh CM5 request, including evaluation time, evidence states, and policy.
5. **Peer cleanup failure could reopen or preserve authority-backed activity.** Disposition: PB7
   retirement closes the local stage gate before `Withdraw` and `Terminate`; cleanup failure is
   visible while the member remains retired. Applying this property exposed and corrected a
   Minimal realization ordering defect.
6. **Revalidation of a refused or incomplete activation could create new effects.** Disposition:
   only an exact successful CBI3 result with an Active, Ready, Released member is eligible; every
   other input returns activation-unavailable without evaluation or provider contact.
7. **Withdrawal could accidentally mint replacement authority.** Disposition: CBI5 returns only
   the fresh admission observation and retirement outcome. It creates no replacement relationship,
   grant, portable constraint, Binding Plan fact, or operation payload.
8. **The slice could be read as cancelling work already executing.** Disposition: CBI5 governs
   admission of subsequent ordinary interaction. It makes no claim about pre-emption, distributed
   revocation propagation, in-flight execution cancellation, or compensation.
9. **Two implementations can still agree where this contract is silent.** Disposition: retained as
   a structural limitation. The shared vectors force current, revoked, expired, mismatched, and
   cleanup-failure answers, but cannot establish general authority lifecycle completeness.

## Result

The CBI5 contract is complete for exact single-grant revalidation of one active singleton binding.
No finding requires widening it into multiple participants or grants, CM4 binding-exercise
projection, cross-vocabulary Operation mapping, multi-member or relational lifecycles,
replacement, mediation, real distribution, or Architecture 0.8 conformance.
