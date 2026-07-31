# CBI15 contract-completeness review

Date: 2026-07-30

Scope: absence review of the CBI15 multi-member participant revision contract, separate from
conformance review.

## Findings and dispositions

1. **Whether a change is admitted against the activation or against the member was the question
   CBI14 left.** Disposition: decided per member, checked against the activation. Admission is about
   an occurrence, so changing one member's set decides nothing about another member's authority; but
   CBI13's identity and Actor-mapping rules are activation-wide, so the *result* is checked across
   every member. Splitting the question that way is what makes both earlier answers hold at once
   rather than one overriding the other.
2. **A declined change and a discovered lapse could have been collapsed into one outcome.**
   Disposition: they are opposite in scope and both reachable from the same call. A revision the
   activation will not admit changes nothing at all; a retained participant that no longer renews is
   CBI14's case and retires the whole activation. A vector produces the retirement from a lapse in
   the member that was *not* being revised, so the distinction is exercised rather than described.
3. **A wrongly named member set could have retired the activation, as it does in CBI14.**
   Disposition: declined here. Revalidation asserts continuity of the whole activation and then
   cannot demonstrate it, which is evidence about authority; a revision merely asks for something
   the activation will not do, which is not. The contract states the difference because two slices
   treating the same input differently is exactly what a later reader would otherwise call an
   inconsistency.
4. **An activation-wide restatement that changes nothing could have passed as a revision.**
   Disposition: refused. Restating what is in force is a revalidation and belongs to CBI14.
5. **Each member's declaration could have been checked against the wrong definition.**
   Disposition: every member's declaration names the requested authority CM2 records for that
   member's own selected definition, and coverage is checked per member against that member's own
   grants. No member's declaration constrains another's.
6. **An addition to one member is a fresh opportunity for the collisions CBI13 refuses.**
   Disposition: identities are checked across every member's intended set, and the Actor mapping is
   re-checked as a function and injective over the whole revised activation. Two vectors exercise
   each against a member that is not being changed.
7. **A dropped participant is not evaluated.** Disposition: deliberate, carried over from CBI9 —
   after the revision it holds nothing in this activation, so its current admission state cannot
   affect the outcome.
8. **Coverage is verified after evaluation, not before.** Disposition: necessary, because the grants
   a member will hold are known only once its intended set is evaluated. The pre-check that the
   *current* set covers its declaration is separate and happens first, so a declaration that never
   held is refused before anything is evaluated.
9. **The remaining single-member slices still have not been lifted.** Disposition: CBI8's
   declaration-free extension, CBI10's observed-interaction verification, and CBI11's succession
   still govern one member. CBI15 covers revision only and does not extend them by implication.
10. **Two implementations can still agree where this contract is silent.** Disposition: retained as
    a structural limitation. The vectors force the per-member-versus-activation, decline-versus-
    retire, identity, Actor-mapping, and coverage answers; they cannot establish general
    multi-member authority lifecycle completeness.

## Result

The CBI15 contract is complete for revising the participant sets of one multi-member, protocol-free
activation under per-member declarations. Finding 1 answers what CBI14 left open, and findings 2 and
3 record where this slice deliberately differs from its neighbours and why. Finding 9 is what
remains: three single-member slices still unlifted. No finding requires widening this contract into
scoped replacement, member addition or removal, Relational Initialisation, mediation, real
distribution, or Architecture 0.8 conformance.
