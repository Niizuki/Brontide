# CBI21 contract-completeness review

Date: 2026-08-01

Scope: absence review of the CBI21 strongly connected activation-group contract, separate from
conformance review.

## Findings and dispositions

1. **CBI12's justification for refusing a multi-member group was wrong about CM3.** Disposition:
   corrected, and it is the finding to carry forward. CBI12 refuses a group with more than one member
   and says that "a multi-member group is a strongly connected component, which is what Relational
   Initialisation exists for". CM3 computes strongly connected components over **every** edge, so two
   Components with mutual ordinary-interaction edges are one cyclic group carrying no protocol, no
   Relational Initialisation stage, and a stage plan CM4 activates. The sentence conflates *being
   cyclic* with *needing a handshake*, and only the second is a reason this seam cannot host it. The
   probe that settled it was run before the contract was written: CM3 planned the ordinary cycle and
   CM4 returned Active for it.
2. **The refusal that remains belongs to Portable Binding, and was already written down.** The PB7
   Composition handoff schema lists Relational Initialisation in its `outOfScope` array, and its
   `refusalRule` says an input naming anything in that array is refused with a declared category.
   Disposition: CBI21 refuses it with a code that names the stage, and this slice claims no authorship
   of the decision — it locates it. The vectors show CM3 produced the plan and CM4 accepts it with its
   own declared handshakes supplied, so the integration is the only refusal in the chain.
3. **Two independent reasons the seam cannot host the stage, and the second is the one a later
   implementer would miss.** Disposition: both are stated. The obvious one is that the seam offers a
   composition exactly one traffic verb and gates it on Release. The other is ordering: a portable
   member reports Ready *during* Interconnection, because establishment and the readiness signal are
   one step, while CM4 requires Relational Initialisation to complete before Ready. Even if a verb
   existed, there is no point in the portable lifecycle at which a handshake could run and still
   precede the readiness it must precede. A later slice that adds only a verb would still not have the
   stage.
4. **The three questions the item recorded are unreachable, not undecided.** Disposition: recorded as
   unreachable, and deliberately not answered. What a bounded protocol means for the release barrier,
   whether lifecycle-traffic authority is CBI13's admission or a separate one, and what a handshake
   failing midway leaves behind are all questions about traffic that cannot occur while the seam
   declares the stage out of scope. Answering them here would decide a Portable Binding contract
   question inside a Component Management slice, invisibly — the failure mode PB7's own refusals exist
   to prevent.
5. **What the seam would need is recorded as an owner decision rather than built.** Disposition:
   Decision 13. Changing `outOfScope`, adding a stage between interconnection and ready, and adding a
   declared-protocol traffic verb are changes to a published data-only contract with its own schemas,
   vectors, and a pending independent review. That is an owner's call, and the decision states the
   three options and what each costs.
6. **A group of one member with a self-relational edge is refused for the stage, not the shape.**
   Disposition: deliberate, and it is why C2 is phrased over protocols rather than over cyclicity. CM3
   marks a single member with a self-edge cyclic, and such a group would declare a protocol; the code
   is the same one a cyclic pair gets, because the missing capability is the same.
7. **CBI12's plan refusal reported one code for four conditions.** Disposition: split, and the two
   CBI12 vectors that pinned the collapsed code now pin the specific one. A caller previously could
   not tell a protocol-bearing group from a plan naming an unselected member, which is the class of
   silence Decision 10 describes: the code was correct for every input the vectors offered and
   uninformative for all of them.
8. **The delivered group's internal edges are declarations this slice never performs.** Disposition:
   bounded and asserted. The ordinary-interaction edges are what made the component strongly
   connected; whether the members then interact is CBI16's question over exercises a host supplies.
   CM3 refuses an ordinary edge observed before Release in any case, so there is no pre-Release peer
   traffic to have an opinion about.
9. **Nothing here proves two Components can actually interact once released.** Disposition: retained
   as a limitation. CBI21 activates a strongly connected group; it does not establish a channel
   between its members, because the portable seam binds a host to a provider and models no
   Component-to-Component binding at all. A composition whose members must call each other still does
   so through the composition root.
10. **Two implementations can still agree where this contract is silent.** Disposition: retained as a
    structural limitation. The vectors force the protocol refusal, the delivered cycle, the mixed
    plan, and the three membership conditions; they cannot establish general grouping completeness.

## Result

The CBI21 contract is complete for activating a strongly connected protocol-free group across the
portable seam and refusing a protocol-bearing one. Finding 1 is the one to carry forward, because it
corrects a predecessor's stated reason rather than extending it, and finding 3 is the one a later
implementer needs, because the missing capability is two things and only one of them is obvious. No
finding requires widening this contract into performing a handshake, which finding 5 places with the
Portable Binding owners as Decision 13.
