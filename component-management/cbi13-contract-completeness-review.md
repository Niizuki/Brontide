# CBI13 contract-completeness review

Date: 2026-07-30

Scope: absence review of the CBI13 multi-member authority contract, separate from conformance
review.

## Findings and dispositions

1. **Per member or per activation was the question the plan raised.** Disposition: per member. CM5
   admits participants against a receiving domain for a target Actor, and CBI3 ties that admission to
   an occurrence. An occurrence is durable; an activation attempt is not. Admitting a set against an
   attempt would attach authority to the shorter-lived thing and force it to be re-decided on every
   restart, which is not what the receiving domain decided.
2. **The plan guessed the authority barrier and the release barrier might be the same barrier.**
   Disposition: they are two, and the authority one is strictly earlier — a precondition before any
   provider contact, against a barrier reached after every member is Ready. What they share is being
   all-or-none over the activation. Both stacks check the separation directly: every authority
   refusal leaves no lifecycle at all, so nothing was even attempted.
3. **Identity collisions across members are invisible to any one member's admission.** Disposition:
   admission, relationship, and authority request identities are distinct across the whole
   activation, checked before a single request is evaluated. It is CBI6's rule one level out, for the
   same reason: a grant identity derives from an authority request identity.
4. **The Actor mapping could have been checked only within each member.** Disposition: across the
   activation it must be a function and injective. The same party participating in two members is
   legitimate and must map consistently; two parties arriving at one local Actor is the conflation
   CBI6 refuses within a set. A vector admits the legitimate case rather than only refusing the
   illegitimate ones, so the rule is shown to permit what it should.
5. **Admitting members one at a time could have reached a provider before the last admission.**
   Disposition: every set is admitted first. CM5 evaluation is effect-free, so admitting all of them
   costs nothing that a refusal would have to undo, and the vectors pin zero provider effects and an
   absent lifecycle for every authority refusal.
6. **Admission could have been read as concluding the activation.** Disposition: CBI12's release
   barrier still applies afterwards. A vector admits every member and still ends with no member
   released, and reports both facts, so "authority permits the attempt" is checked rather than
   asserted.
7. **Grants are not shared between members.** Disposition: each member's grant names that member's
   own target Actor, Operation, and scope, and nothing merges them. A participant in two members
   holds one grant per member. Recorded as a limit rather than a mechanism.
8. **The post-activation slices did not follow.** Disposition: recorded as the limit worth carrying
   forward. CBI7 through CBI11 — revalidation, withdrawal, extension, revision, verification, and
   succession — still govern one member. CBI13 deliberately does not extend them by implication, and
   the first question they raise across members is what one member's lapsed authority should do to
   the others.
9. **Nothing admits authority for a cyclic group.** Disposition: CBI12 refuses those activations
   before authority is consulted, so the question does not arise here and is not answered by
   omission.
10. **Two implementations can still agree where this contract is silent.** Disposition: retained as
    a structural limitation. The vectors force the per-member, barrier-ordering, identity, mapping,
    and effect-count answers; they cannot establish general multi-member authority completeness.

## Result

The CBI13 contract is complete for per-member admission gating one multi-member, protocol-free
activation. Findings 1 and 2 answer the two questions the plan raised, and finding 2 corrects the
guess it recorded: the barriers are distinct and ordered rather than the same. Finding 8 is what the
programme now owes — the post-activation slices are still single-member. No finding requires widening
this contract into Relational Initialisation, member replacement, mediation, real distribution, or
Architecture 0.8 conformance.
