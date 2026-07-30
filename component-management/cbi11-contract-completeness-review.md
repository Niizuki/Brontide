# CBI11 contract-completeness review

Date: 2026-07-30

Scope: absence review of the CBI11 declaration-succession contract, separate from conformance
review.

## Findings and dispositions

1. **The question this slice inherited was "what evidence retires an unexercised declaration".**
   Disposition: none does. The permission is the Component's own re-declaration, carried by a
   successor resolution, and observation appears only as a veto. The contract says so explicitly, so
   a later implementer does not reach for elapsed time, an interaction count, or a quiet period —
   each of which would be the fallacy CBI10's finding 8 named, dressed as a threshold.
2. **A successor could have been a different binding.** Disposition: it must resolve the same
   requirement to the same definition and occurrence, as one direct `1..1` distinct position, under
   the binding scope the live member itself records. The scope is read from the member's own
   resolution fact rather than from a caller claim, so the check cannot be satisfied by assertion.
3. **Succession could have widened.** Disposition: the successor's names must be a strict subset.
   New authority is admitted by admitting participants that hold it, not by re-declaring a live
   binding, and an unchanged declaration is refused because there is nothing to succeed.
4. **A retained dependency could have been re-pointed.** Disposition: every name the successor keeps
   must map to the identical tuple. Succession removes dependencies; silently changing what one
   means would be a different declaration wearing the same name.
5. **Observation's role could have been inverted.** Disposition: exercised authority vetoes its own
   removal, and unexercised authority permits nothing. This is CBI10's asymmetry applied in the only
   direction it is sound, and the vectors pin both halves.
6. **A refusal could have disturbed the binding.** Disposition: CBI11 has no retirement path at all.
   Every vector pins the member as released, which makes "this slice never retires" a checked answer
   rather than a claim in prose.
7. **Narrowing could have been mistaken for removing participants.** Disposition: it only changes
   what a later CBI9 revision will admit. Each stack proves the difference by running the same
   revision before and after a succession — declined for uncovered dependency first, admitted
   second — rather than asserting that narrowing is sufficient.
8. **The successor's own honesty is not checked.** Disposition: recorded as a limit, and it is
   bounded by the slice that came before. CBI11 checks that the successor resolves the same position
   and declares less, not that the narrower declaration is truthful. A Component that narrows
   dishonestly and then exercises what it dropped is caught by CBI10 as undeclared use, which
   retires it. Succession therefore cannot be used to launder authority; it can only move the
   binding to a declaration the Component will be held to.
9. **Nothing replaces the member with the successor generation's member.** Disposition: deliberate.
   The successor is consulted for what it declares, not activated. Replacement of a live member by a
   new generation's member remains future work.
10. **Two implementations can still agree where this contract is silent.** Disposition: retained as
    a structural limitation. The vectors force the position, narrowing, tuple-stability, veto,
    attribution, and never-retires answers; they cannot establish general declaration-lifecycle
    completeness.

## Result

The CBI11 contract is complete for narrowing one declaration to a successor resolution of the same
position, over one released singleton binding, with observed use as a veto. Findings 1 and 8 are the
pair worth carrying forward: no evidence of disuse ever narrows a declaration, and a narrowing the
Component did not earn is caught afterwards rather than prevented here. No finding requires widening
it into member replacement, multi-member or relational lifecycles, mediation, real distribution, or
Architecture 0.8 conformance.
