# CBI12 contract-completeness review

Date: 2026-07-30

Scope: absence review of the CBI12 multi-member activation contract, separate from conformance
review.

## Findings and dispositions

1. **The release barrier could have been placed at the member.** Disposition: it is the activation.
   CM4 models one logical Release for an activation attempt, so the answer comes from the runtime's
   own shape rather than from a preference expressed here. Both stacks assert it as a property over
   every vector — either every member is released or none is — so a per-member release could not
   pass unnoticed.
2. **A member that succeeded could have been left holding an open channel.** Disposition: when any
   member fails, every member already interconnected is retired, gate first. Each stack then attempts
   an ordinary Operation on the member that had succeeded and requires a state refusal, so "no other
   member is reachable" is checked rather than asserted.
3. **A cyclic group could have been approximated.** Disposition: refused. A group with several
   members is a strongly connected component, which is what Relational Initialisation exists for, and
   activating one without that stage would decide CM3's semantics invisibly. The vector uses a
   genuinely cyclic CM3 plan — two relational edges, each with its own complete protocol — rather
   than a hand-made shape, so the refusal is against the real thing.
4. **The plan and the selections could have disagreed silently.** Disposition: exact correspondence
   is required in both directions — every planned member selected, every selected member planned,
   one member per group, no protocols — so neither an unselected planned member nor an unplanned
   selection can slip through.
5. **Order could have decided which failure is reported.** Disposition: preparation, establishment,
   Ready checks, Release, and retirement all follow one order derived from the occurrences, so the
   caller's list order changes nothing.
6. **A failure could have been unattributable.** Disposition: the failing occurrence is named with
   its portable code, and the CM4 outcome is derived from that member's failed stage. Cleanup
   failures met while retiring the others are appended to the reason rather than replacing the cause.
7. **Members could have been coupled through the composition.** Disposition: each has its own
   resolved position, portable contract, conversation, and Binding Plan. The fixture gives the two
   members different contracts so the independence is concrete rather than incidental.
8. **This is not a multi-member authority story.** Disposition: recorded as the limit worth carrying
   forward. CBI3 and CBI6 through CBI11 still govern one member: admitting participants for a
   several-member activation, revalidating such a set, and revising it are not covered, and CBI12
   deliberately does not extend them by implication.
9. **Nothing orders members by dependency.** Disposition: the members here are independent by
   construction — separate singleton groups. Dependency-first ordering across groups is CM3's, and
   consuming it is future work rather than something this slice approximates.
10. **Two implementations can still agree where this contract is silent.** Disposition: retained as
    a structural limitation. The vectors force the barrier, cleanup, cyclic-refusal, correspondence,
    and attribution answers; they cannot establish general multi-member activation completeness.

## Result

The CBI12 contract is complete for one activation of several independent, protocol-free members with
the barrier at the activation. Finding 8 is the one to carry forward: the lifecycle now spans several
members while authority still does not, and closing that gap is the next thing the programme owes.
No finding requires widening this contract into Relational Initialisation, member replacement,
mediation, real distribution, or Architecture 0.8 conformance.
