# CBI20 contract-completeness review

Date: 2026-08-01

Scope: absence review of the CBI20 activation membership-change contract, separate from conformance
review.

## Findings and dispositions

1. **CBI19's stated limit was a description of how it was called, not a rule it applied.**
   Disposition: corrected in this change, and recorded as the finding to carry forward. CBI19's
   contract says it "does not add or remove a position" and its completeness review says such a
   successor "is not reachable through this slice". Reading it to build CBI20 shows otherwise: the
   replacer takes the caller's member list, treats an occurrence the retained activation does not
   hold as a new member admitted afresh, and never visits a retained occurrence the list omits. Both
   halves of a membership change went through unannounced, and the review's word *reachable* was
   wrong rather than imprecise. CBI19 now refuses a member-set change and names this slice; both
   stacks have a named test that goes red when the guard is removed, which is what makes the claim
   an enforced one this time.
2. **What a dropped position does to the authority admitted against its occurrence was the first
   question the item recorded.** Disposition: nothing needs undoing, and the question's premise is
   what dissolves. It presupposed that authority attaches to a durable occurrence and must therefore
   be disposed of when the occurrence leaves. CBI19 already establishes that no authority survives an
   activation attempt — every successor member carries its own admission, and every retained member
   is retired at cutover whether or not its position was dropped. A drop is therefore visible only as
   the absence of a successor admission. What an occurrence's durability governs is *what may be
   re-admitted*, not a grant that persists.
3. **Whether an added position may join a released activation was the second question.** Disposition:
   only across a cutover, and the absent operation is the answer. Which positions exist is a property
   of a CM2 generation, a generation is immutable and resolves every position at once, and it reaches
   a scope only through CM4's Release and cutover — of which there is one per attempt. Releasing a
   newly added member into a live activation would be the partial Release CBI12 established CM4 does
   not model. This is the same shape CBI19 found for removal, arrived at from the other side.
4. **CBI18's in-place growth is not a precedent for in-place addition.** Disposition: recorded,
   because the two look alike and are not. CBI18 grows a member's *participant set* while every
   member stays released; that changes no position, so no generation changes and no barrier is
   involved. Growing the *member set* changes the generation. The contract states the distinction so
   a later reader does not treat CBI18 as the pattern to extend.
5. **The case only a membership change can pose is a local Actor freed by a drop and taken by a
   different party.** Disposition: refused, by checking CBI13's mapping rules over the union of the
   retained and successor activations rather than over the successor alone. CBI19's own review
   accepted that both generations are established against the same binding scope for a bounded window
   between Ready and retirement; that window is exactly when such a conflation would be live, which
   is CBI6's reason for refusing it inside a set. While the positions are the same the check is
   vacuous, which is why no earlier slice needed it. Both directions have a vector, and re-homing a
   party across a replacement is refused for the same reason and routes through CBI14 retirement and
   a fresh CBI13 activation.
6. **The asymmetry between addition and removal is deliberate.** Disposition: a drop must be one the
   successor generation makes; an addition is a mapping the caller supplies. Recorded because the
   symmetry is tempting and wrong. CBI1 has always required an explicit typed mapping for each
   resolved position the caller takes into portable preflight, so a position the generation resolves
   and the caller does not map is one this activation does not cover — nothing is taken away. A drop
   removes something that is live, so it must be the composition's decision rather than the caller's.
7. **The declared drop set is derivable, and is required anyway.** Disposition: deliberate, and it is
   the refusal a caller is most likely to find bureaucratic. The set the caller must declare is
   exactly the retained occurrences its member list omits, so nothing is learned from it. What it
   buys is that an accidental omission is refused instead of dropping a member silently — the failure
   mode CBI19 could not see, restated as an input the caller has to get right.
8. **A membership change reports what it applied, not what was asked for.** Disposition: a declined
   change names no additions and no drops. Recorded because reporting the intended change on a
   refusal would read as though something happened, and the vectors pin the count at zero for every
   refusal.
9. **The neighbouring slices are reached by refusal, never by fallback.** Disposition: deliberate. A
   successor holding no member is CBI14 withdrawal and is refused rather than performed; a successor
   over the positions already held is CBI19 and is declined rather than delegated to. Each refusal
   names the slice that does the job, so no call quietly becomes a different operation than the one
   asked for.
10. **The successor generation may resolve positions the activation does not cover.** Disposition:
    accepted and bounded. CBI20 does not require the member list to cover every position the
    successor resolves, because a generation may contain positions CBI1 cannot translate at all —
    wider cardinality, mediation, indirection. The consequence is that "the activation holds exactly
    the positions its generation resolves" is not an invariant this slice establishes, and a later
    slice that wants it will have to say so.
11. **Nothing here models a grant outliving the attempt it was admitted in.** Disposition: recorded
    as a limit rather than a finding, because it bounds finding 2. Grants are per-attempt data in
    this fake programme, so a dropped occurrence needs no withdrawal step. A receiving domain that
    persists grants beyond an attempt would need one, and CBI20 does not supply it.
12. **Two implementations can still agree where this contract is silent.** Disposition: retained as a
    structural limitation. The vectors force the drop-declaration rules, the generation's authority
    over what may be dropped, the overlap mapping rule in both directions, the barrier, and the
    cutover ordering; they cannot establish general membership-change completeness.

## Result

The CBI20 contract is complete for adding and removing positions of one protocol-free multi-member
activation across a scoped replacement. Finding 1 is the one to carry forward, because it corrects a
claim an earlier review made about its own unreachability rather than extending it. Findings 2 and 3
answer the two questions the item recorded, the first by dissolving its premise. Finding 5 is the
case only a membership change can pose, and finding 7 is the refusal whose value is invisible until
it fires. No finding requires widening this contract into Relational Initialisation, child Ports,
mediation, wider Provider Sets, real distribution, or Architecture 0.8 conformance.
