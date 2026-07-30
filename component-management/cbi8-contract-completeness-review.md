# CBI8 contract-completeness review

Date: 2026-07-30

Scope: absence review of the CBI8 in-place participant extension contract, separate from conformance
review.

## Findings and dispositions

1. **"Replacement" is two operations with opposite risk, and naming them together hid that.**
   Disposition: only addition is admitted in place. Adding grows authority and withdraws nothing the
   member may rely on; removing or substituting withdraws authority while the member is released,
   and nothing in an admitted set says whether the member relies on it. A substitute holding the
   same Capability, target Actor, Operation, and scope is not a stand-in, because the holder is part
   of what makes a grant that grant (CBI6 C3). Removal and substitution are declined with an
   explicit route — CBI7 retirement and a fresh CBI6 admission — and stay future work until a member
   can declare which grants it depends on. Closed on 2026-07-30 by
   [CBI9](./cbi9-capability-contract.md), which adds that declaration and admits removal and
   substitution while every declared dependency stays covered.
2. **Precedence between participants was named as the question this slice had to answer first.**
   Disposition: it does not arise. Precedence would be needed to decide which participant may be
   dropped from a live set; refusing every drop removes the question rather than answering it
   badly. Recorded here so a future removal slice knows precedence is its prerequisite.
3. **Two different problems with a retained participant could have been collapsed into one
   outcome.** Disposition: they are separated deliberately. A request that does not re-identify a
   retained participant's authority is declined — nothing was evaluated, so nothing was learned, and
   the member's release still rests on the last admission that did hold. An evaluated outcome that
   no longer reproduces the identical relationship and grants is positive evidence of loss and
   retires the member. The contract states the invariant directly: no result both retires the member
   and reports zero evaluations.
4. **A set could have been extended on top of authority that had itself lapsed.** Disposition:
   retained participants are revalidated in the same all-or-none evaluation as the additions, and a
   lapse outranks any problem with an addition, so a call that would both retire and decline retires.
5. **Cross-set rules could have been applied only among the newly added participants.** Disposition:
   identity distinctness and distinct receiving-domain Actors are checked across the complete
   extended set, including against participants that are already live. An addition is a fresh
   opportunity for exactly the collisions CBI6 refuses, now against a set in force.
6. **An intended set identical to the current one was ambiguous.** Disposition: declined with its
   own code. Renewing the current set is CBI7's decision, and quietly making it here would duplicate
   that decision in a second place.
7. **The result of an extension could have been a dead end.** Disposition: a successful extension
   returns the set in the same form CBI6 produces, so CBI7 revalidates it and CBI8 can extend it
   again. Each stack proves that chain rather than asserting it.
8. **The provider is never told the participant set changed.** Disposition: deliberate and unchanged
   from CBI6. No CM5 identity, grant, evidence, or decision crosses the portable boundary, and the
   participant count is not visible to the provider. Recorded as a limit, not a gap.
9. **Occurrence-to-Actor mappings are not resupplied.** Disposition: the member and its occurrence
   are already fixed by the active result, so re-stating them could only introduce drift. CBI7 set
   this precedent and CBI8 keeps it.
10. **Two implementations can still agree where this contract is silent.** Disposition: retained as
    a structural limitation. The vectors force the outcome kind, code, evaluated count, in-force set
    size, and released state for every scenario — including the declined cases, where "nothing
    changed" is the answer being checked — but cannot establish general multi-party authority
    lifecycle completeness.

## Result

The CBI8 contract is complete for fail-closed in-place growth of one participant set over one
released singleton binding. Findings 1 and 2 narrow the item this slice was named for and say why;
removal, substitution, and participant precedence remain future work with a stated prerequisite. No
finding requires widening it into CM4 binding-exercise projection, cross-vocabulary Operation
mapping, multi-member or relational lifecycles, mediation, real distribution, or Architecture 0.8
conformance.
