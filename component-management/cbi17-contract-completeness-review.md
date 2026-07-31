# CBI17 contract-completeness review

Date: 2026-07-31

Scope: absence review of the CBI17 multi-member declaration-succession contract, separate from
conformance review.

## Findings and dispositions

1. **Whether a successor narrows one member's declaration or the activation's set of them at once
   was the first question this item recorded.** Disposition: at once, and the reason is CM2's rather
   than a preference. The permission is a generation, and a generation is one immutable object that
   resolves every position together; applying the members it narrows while refusing the rest would
   leave the activation holding declarations from two generations, which is a state no generation
   records. This is the same move CBI12 made with CM4's single Release and CBI16 with CM4's single
   request — the runtime object's shape settles the scope.
2. **Whether a member whose position has no successor blocks the others was the second.**
   Disposition: it blocks them. A generation that does not resolve every member's position as the
   live activation holds it is not a successor *of this activation*, so it may narrow none of it.
   The alternative — narrowing the members it does describe — would let a caller move part of an
   activation onto a declaration drawn from a composition that does not contain the rest, and the
   declaration is exactly what CBI16 holds a member to afterwards.
3. **CBI11's refusal of an unchanged declaration turned out to conflate two things.** Disposition:
   separated here. *Nothing to succeed* is an activation-level condition and is still refused; *this
   member is untouched* is a per-member outcome and is now ordinary, because a successor that narrows
   one Component and leaves another alone is the common case. Only a second member can pose this,
   which is why CBI11 could state one rule for both. A vector narrows one member while the other
   restates its declaration unchanged, so the distinction is exercised rather than described.
4. **The veto could have been scoped to the member that raised it.** Disposition: refused, by
   finding 1. The succession is one transaction, so a veto anywhere refuses all of it — including
   the narrowings of members that had none. A vector puts the veto in the member that was not the
   one being narrowed first, so the whole-transaction effect is visible rather than incidental.
5. **Exercised authority is computed per member.** Disposition: each member's attribution and
   observations decide only its own exercised set, as CBI16 established for attribution. Pooling them
   would let one member's interaction veto another member's narrowing, which is the mirror of the
   grant-pooling CBI16 refused.
6. **A partial application could have been offered as a convenience.** Disposition: refused with
   finding 1, and worth naming separately because it is the shape a caller will ask for. A caller
   that wants one member narrowed and another left alone already has that: it supplies a successor
   generation that narrows one and restates the other, which finding 3 makes an ordinary success.
7. **This slice still never retires.** Disposition: carried over from CBI11 and checked at the
   activation level — every vector pins every member as released, and the operation is synchronous in
   both stacks because it has no peer traffic to perform. That the signature cannot await anything is
   itself evidence rather than a comment.
8. **Narrowing still permits rather than performs.** Disposition: unchanged from CBI11 and proved
   the same way one level out — each stack runs the same CBI15 revision before and after a
   succession, declined for an uncovered dependency first and admitted second, rather than asserting
   that narrowing is sufficient. The admissions and grants in force are also checked identical across
   every outcome.
9. **The successor's own honesty is still not checked.** Disposition: unchanged from CBI11's finding
   8, and the containment now runs through CBI16 rather than CBI10: a member that narrows dishonestly
   and then exercises what it dropped is undeclared use, which under CBI16 retires the whole
   activation. Succession still cannot launder authority; it can only move the activation to
   declarations the Components will be held to, and the consequence of abusing it is now larger.
10. **Nothing replaces a member with the successor generation's member.** Disposition: deliberate and
    unchanged. The successor is consulted for what it declares, not activated. Member replacement,
    addition, and removal remain future work.
11. **The last single-member slice is CBI8.** Disposition: CBI8's declaration-free extension still
    governs one member. CBI17 covers succession only and does not extend it by implication.
12. **Two implementations can still agree where this contract is silent.** Disposition: retained as
    a structural limitation. The vectors force the transaction scope, the blocked-position answer,
    the unchanged-versus-refused distinction, the veto scope, and the never-retires answer; they
    cannot establish general declaration-lifecycle completeness across an activation.

## Result

The CBI17 contract is complete for narrowing the declarations of one protocol-free multi-member
activation to a successor generation, with observed use as a veto. Findings 1 and 2 answer the two
questions this item recorded, and finding 3 is the one it did not anticipate: a rule CBI11 could
state as a single refusal turns out to have been two rules that only a second member separates.
Finding 11 is what remains. No finding requires widening this contract into member replacement,
addition or removal, scoped replacement, Relational Initialisation, mediation, real distribution, or
Architecture 0.8 conformance.
