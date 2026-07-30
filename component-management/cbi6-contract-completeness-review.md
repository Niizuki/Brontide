# CBI6 contract-completeness review

Date: 2026-07-30

Scope: absence review of the CBI6 multi-participant and multi-grant admission contract, separate
from conformance review.

## Findings and dispositions

1. **A CM5 request names exactly one participant, so "a set of participants" had no defined home.**
   Disposition: the set is an explicit caller-supplied list of mapping and request pairs. CM5 keeps
   deciding one request at a time, and every rule that spans requests belongs to the composition
   root, which is the only place that can see more than one.
2. **Identities repeated across requests are invisible to the evaluator.** Disposition: admission,
   relationship, and authority request identities must be distinct across the whole set. The
   authority rule matters most: the grant identity is derived from the authority request identity,
   so a collision would produce two aggregate grants that cannot be told apart by identity while
   each request stayed individually valid.
3. **Two participants could be mapped onto one receiving-domain Actor.** Disposition: refused after
   evaluation and before provider contact. Local policy maps `(proposed Actor, relationship kind)`
   without seeing the rest of the set, so nothing below this coordinator can notice that two remote
   parties' grants were about to share a holder.
4. **One participant could request the same narrow authority twice under two identities.**
   Disposition: Capability, target Actor, Operation, and scope tuples are distinct within a request.
   Across participants the same tuple is allowed and is not a defect: the holders differ, which is
   what makes them different grants, and the receiving domain decided each one separately.
5. **The result could depend on the order the caller built the set in.** Disposition: participants
   are ordered by Actor before evaluation and aggregate grants by grant identity, so evaluation
   order, retained observations, and the grant list are all independent of caller order.
6. **Stopping at the first refusal would hide the rest of the set.** Disposition: CM5 evaluation is
   effect-free, so every participant is evaluated and every observation retained for attribution.
   The shared vectors pin the number of evaluations for each scenario, which forces both stacks to
   answer this question rather than agreeing by silence.
7. **A partially admitted set could be read as partial authority.** Disposition: aggregate grants
   exist only when the complete set was admitted exactly. Every refusal reports an empty grant set,
   leaves no portable member, and reaches no provider; the vectors assert zero provider effects and
   an absent lifecycle for every refusal.
8. **A wider participant set could leak into the portable seam.** Disposition: no CM5 identity,
   grant, evidence, or decision enters the portable contract or Binding Plan, and the participant
   count is not visible to the provider. Each stack compares the complete portable fact set of a
   two-participant activation against a one-participant activation and requires them equal.
9. **The mapping asserts a correspondence this slice cannot verify.** Disposition: CBI6 checks that
   every mapping names the CBI1-selected occurrence and its own request's participant. That the
   Actor genuinely participates in that Component remains a caller claim, exactly as in CBI3, and
   is recorded as a limit rather than silently treated as verified.
10. **Withdrawal was left undefined for a set.** Disposition: retained as a stated limit. CBI5
    revalidates one relationship and grant behind one member; what a set should do when one
    participant of several loses authority — retire the shared member, or narrow the set — is a
    decision this slice does not make and does not approximate. Closed on 2026-07-30 by
    [CBI7](./cbi7-capability-contract.md), which retires the shared member and refuses narrowing.
11. **Two implementations can still agree where this contract is silent.** Disposition: retained as
    a structural limitation, and sharper here than in earlier slices because both realizations were
    written by one reader. The vectors force the ordering, evaluation-count, identity-distinctness,
    local-Actor, and empty-grant answers above; they cannot establish general multi-party authority
    completeness.

## Result

The CBI6 contract is complete for fail-closed admission of a participant set holding several exact
narrow grants over one singleton binding. No finding requires widening it into set revalidation or
withdrawal, participant precedence, CM4 binding-exercise projection, cross-vocabulary Operation
mapping, multi-member or relational lifecycles, replacement, mediation, real distribution, or
Architecture 0.8 conformance.
