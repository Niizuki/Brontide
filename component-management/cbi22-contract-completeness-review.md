# CBI22 contract-completeness review

Date: 2026-08-01

Scope: absence review of the CBI22 child-Port activation contract, separate from conformance review.

## Findings and dispositions

1. **The integration did not refuse a Port-contained position; it ignored the containment.**
   Disposition: corrected, and it is the finding to carry forward. A `ProviderSetObservation` carries
   the Region and Port CM2 resolved a position into, and CBI1 read neither, so such a position was
   flattened into an ordinary one and activated in whatever restart scope the caller's plan named —
   no child declaration, no parent generation, and the restart boundary the Port exists to give
   silently dropped. Both activation paths now refuse it, and the child path is the way through.
2. **The future-work index asserted the opposite, and nothing tested it.** Disposition: recorded
   separately from finding 1, because the two are different failures. The claim that "CBI1 refuses a
   position carrying a child Port envelope" was written into the priority document by the slice
   before this one, from reading the contract rather than the code. It is the third stated limit in
   four slices that turned out to be a description rather than a rule, and the first that was written
   by the programme itself rather than inherited.
3. **Which Port an attachment names is the generation's statement, not the caller's.** Disposition:
   every member must be contained in one Port, the attachment must name that Port, and the
   lifecycle facts are read from the resolved envelope. This is CBI20's rule for membership applied
   to containment, and it is what makes the caller unable to attach a Component to a Port CM2 did not
   put it in.
4. **CM2 refuses a sealed Port at resolution, so CM4's closed-Port refusal is nearly unreachable
   here.** Disposition: stated rather than given a manufactured path, as PB6 did for
   `peer-unavailable`. What remains reachable is a caller declaring a Port runtime-open that the
   generation resolved as activation-open, which is the disagreement the vectors pin. The code CM4
   would produce for a closed Port is mapped but only reachable through a caller that bypasses the
   containment check.
5. **A containment disagreement is refused before any authority is evaluated.** Disposition:
   deliberate, and it moved during implementation. Left inside the activation, the check ran after
   CM5 had admitted the child's participant set, so a structural disagreement cost an authority
   evaluation and reported an admission count. Structural refusals evaluate nothing, as CBI15 and
   CBI16 both phrase it, so the check was hoisted into the child path.
6. **The child's authority has to be its own request, and the first draft's did not.** Disposition:
   fixed in the fixture, and worth recording because the test caught it rather than the code. Reusing
   the parent's authority request identity for the child produced the same grant identity in both
   activations without CM5 having decided anything about the child, and the C8 assertion failed for
   that reason. Nothing in the code prevents a caller doing this; what the contract says is that the
   child is admitted afresh, which remains true — the identity collision is the caller's to avoid.
7. **The parent-untouched property needs a parent that was up.** Disposition: the property is
   conditioned on the vector's own expectation, because one vector's parent activation fails on its
   own before any child runs. Asserting "no parent member is retired" unconditionally would have made
   that vector fail for something the child did not do.
8. **Nothing here models traffic between a parent member and a child member.** Disposition: retained
   as a limitation, and it is the same one CBI21 recorded from the other side. The portable seam binds
   a host to a provider and models no Component-to-Component binding, so a child that must call its
   parent does so through the composition root, exactly as siblings do.
9. **A child of a child is not reached.** Disposition: bounded. CM4's model permits it — the child's
   own scope could be another attachment's parent — and nothing in this slice forbids it, but no
   vector exercises it and the contract does not claim it.
10. **Two implementations can still agree where this contract is silent.** Disposition: retained as a
    structural limitation. The vectors force the attachment rules, the containment rules in both
    directions, CM4's three child refusals, and the parent's independence; they cannot establish
    general child-Port completeness.

## Result

The CBI22 contract is complete for attaching one child activation to one runtime-open Port of one
released parent. Finding 1 is the one to carry forward, and finding 2 is the one worth carrying
further: a stated limit written by this programme, about this programme, was wrong in the same way
the inherited ones were. No finding requires widening this contract into nested children, Port
migration, mediation, or Relational Initialisation, which remains blocked on Decision 13.
