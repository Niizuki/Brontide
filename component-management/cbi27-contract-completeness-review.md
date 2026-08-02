# CBI27 contract-completeness review

Date: 2026-08-02

Scope: absence review of the CBI27 wider-Provider-Set contract, separate from conformance review.

## Findings and dispositions

1. **A Provider Set's members have a representation the seam holds and the set does not.**
   Disposition: the answer, reached by CBI25's test rather than by preference. Each member is one
   provider answering one contract in one scope, which is exactly what the seam binds; what has no
   representation is the statement that these bindings answer one requirement together. The set
   therefore stays at the composition root, which already holds several members as one activation, and
   the seam is neither widened nor relaxed.
2. **The portable binding scope is not CM2's binding scope, and CBI1 mapped one onto the other.**
   Disposition: the finding to carry forward. The portable scope is the composition's identity for a
   position, one member holds it, and the seam's `scope-uniqueness` silence says a composition that
   reuses one has two members claiming one position. A CM scope is a container: CM2 looks occupied
   bindings up by scope **and contract**, distinguishes them by `BindingId`, and refuses several in
   one scope only when the position is `1..1`. CBI1's mapping is therefore a bijection under two
   conditions it never states — the position is `1..1`, and the scope holds one position.
3. **The second condition is already false, and this slice does not fix it.** Disposition: pinned by a
   named test and raised as **Decision 16**. Two positions resolved in one CM scope reach the seam as
   two members reporting one scope; both stacks do it identically and no vector ever asked, because
   every fixture derives its positions from one requirement template. This is the shape Decision 10
   describes, and it is the fourth stated limit in this programme that turned out to describe how
   something was called rather than a rule it applied. Correcting it moves every member's
   `bindingScope` fact and so every CBI4 profile digest the shared fixture pins, which is a repin
   rather than a slice's work.
4. **The caller names each member's scope, and nothing here can check it against CM2.** Disposition:
   deliberate, and the honest consequence of finding 2. CBI1 already takes every other portable
   identity explicitly and checks only that it is well formed; the scope becomes one more, checked for
   distinctness within the set. Deriving one from the CM scope and the occurrence was considered and
   rejected: it would make the composition root the author of which binding is which, in the identity
   space that survives withdrawal and replacement.
5. **All-or-none comes from the seam's own words.** Disposition: derived rather than chosen. The seam
   refuses a wide cardinality *"rather than narrowed to a first member"*, so a composition root that
   kept the members which happened to prepare would perform that narrowing one level up, where the
   seam cannot see it. Two vectors exercise it, one failing in the mapping and one at the seam
   boundary.
6. **A `1..1` position is refused here even though the path could serve it.** Disposition: refused, as
   CBI25 refuses a distinct position. Two paths for one shape would let a caller choose which rules
   apply, and CBI1's are stricter about membership than a fan-out over one member would be.
7. **A distinct position declaring a Mediation is caught twice, and the order decides the code.**
   Disposition: recorded. Removing the declaration half of the exposure check does not admit the
   position — the binding-plan check refuses it, because CM2 stamps the Mediation onto every plan
   observation — so the vector's real content is that the caller is told CBI25 is the path rather than
   that the generation is malformed. The mutation run confirms the check decides the code in both
   stacks.
8. **How many members a wide position resolves is a fact about the request.** Disposition: exercised
   rather than assumed. CM2 fills a Provider Set to its declared **minimum** and then takes explicit
   preselections up to its maximum, so a `1..3` position with one candidate resolves one member and a
   `1..2` with a preselection resolves two. Three vectors pin the three shapes, and a wide position
   that resolved one member is still this path's rather than CBI1's.
9. **Set satisfaction under member loss has no owner, and the activation is stricter than the set.**
   Disposition: stated, not decided. `Cardinality.Minimum` says when a position is satisfied, so a
   `1..3` set could survive losing one of three; CBI14 retires the whole activation for any lapse.
   That is safe and it is what the runtime models, but leaving it unsaid would let a reader take the
   strictness for a decision about set semantics that nobody made.
10. **A fanned-out set has no activation path.** Disposition: the deliberate stop, and the next slice.
    The group activation path prepares each member from a selection through CBI1, which refuses a wide
    position, so the members this slice produces cannot yet be activated. Wiring it means teaching
    that path to take prepared members or to take a wide position, and doing it here would mix the
    translation question with a change to every multi-member slice's entry point.
11. **Two implementations can still agree where this contract is silent.** Disposition: retained as a
    structural limitation. The vectors force the wide-versus-`1..1` partition, the mediated partition
    in both directions, the membership rule in three directions, scope distinctness, all-or-none, and
    the unfilled outcome; they cannot establish general Provider Set completeness.

## Result

The CBI27 contract is complete for translating a wide distinct position into one portable member per
resolved member, at preflight. Finding 2 is the one to carry forward and finding 3 is its unfixed
half: a mapping that held because two conditions happened to be true, one of which a wide set breaks
by construction and the other of which was already broken. Finding 9 is the boundary a later reader is
most likely to mistake for a decision, and finding 10 is the next slice. No finding requires widening
this contract into activating a fanned-out set, deciding set satisfaction, or teaching the seam what a
Provider Set is.
