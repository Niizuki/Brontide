# CBI16 contract-completeness review

Date: 2026-07-31

Scope: absence review of the CBI16 multi-member observed-interaction verification contract, separate
from conformance review.

## Findings and dispositions

1. **Whether one member's undeclared use condemns the activation was the question this item
   raised.** Disposition: it does, and the answer is not a preference. A CBI12 activation is one CM4
   request, so the projection is one list of exercises and CM4 refuses the request on the first
   offending exercise rather than excusing the members that behaved. The runtime's shape decides, as
   it did for CBI12's release barrier. That CBI14's independent reason — one restart scope, one fate
   — reaches the same answer is recorded because two arguments converging is weaker evidence than it
   looks: they would have had to be weighed against each other had they disagreed, and the contract
   says which one it follows.
2. **Retiring only the offending member was the alternative.** Disposition: refused for CBI14's
   reason. CM4 models no way to retire one member while its scope keeps running; that is a scoped
   replacement, an operation it declares separately. A verifier is a worse place than a revalidator
   to invent one.
3. **The same Operation appearing in two members had to be decided.** Disposition: it is not a
   collision. Attribution is per member because the declaration is per member, so two Components
   that both expose an Operation of the same name are two independent attributions. Within one
   member a repeated Operation is still refused, as CBI10 refuses it. A vector attributes the same
   Operation reference in both members and is admitted, so the answer is forced rather than assumed —
   the single-member contract could not raise the question at all.
4. **One member's grants could have admitted another member's use.** Disposition: refused. CBI13
   admits authority per member, so the admission fact of a member's exercise is derived from that
   member's own declaration and its own grants. Pooling them across the activation would make one
   member's participants silently authorize another's interaction.
5. **Exercise identity became activation-wide when the request did.** Disposition: identities are
   derived from the member occurrence as well as the position, because CM4 refuses a request with a
   repeated binding-exercise identity. This is mechanical rather than semantic, and it is recorded
   because the single-member projection could number exercises from one without consequence.
6. **A structural refusal and an observed violation could have been collapsed.** Disposition: they
   are opposite in effect, as they are in CBI15. A member set the verification names wrongly, a
   repeated Operation within a member, or a declaration the generation does not record evaluates
   nothing, so it learns nothing and changes nothing; only a delivered interaction condemns.
7. **The order of two simultaneous violations across members was undefined.** Disposition:
   undeclared use is named before ungranted use over the whole activation, which is CBI10's rule at
   one level out. A vector produces one of each in different members.
8. **Which member caused the retirement had to stay visible.** Disposition: the result names the
   violating members, and a member retired because a sibling violated is reported as retired without
   being named as a cause — CBI14's cause-versus-consequence separation.
9. **Unexercised and uncovered declared authorities are still not violations.** Disposition: carried
   over from CBI10 unchanged, and now reported per member. A member may declare a real dependency it
   has not used yet, and that says nothing about its siblings.
10. **The remaining single-member slices are now two.** Disposition: CBI8's declaration-free
    extension and CBI11's succession still govern one member. CBI16 covers verification only and does
    not extend them by implication.
11. **Two implementations can still agree where this contract is silent.** Disposition: retained as
    a structural limitation. The vectors force the condemnation scope, per-member attribution,
    per-member derivation, violation ordering, decline-versus-retire, and reporting answers; they
    cannot establish general multi-member interaction-verification completeness.

## Result

The CBI16 contract is complete for verifying the declarations of one multi-member, protocol-free
activation against a given set of observed interactions. Finding 1 answers what the item raised and
records why the answer is the runtime's rather than a preference; findings 3 and 4 are the two
questions a second member creates that a single member could not pose. Finding 10 is what remains.
No finding requires widening this contract into scoped replacement, member addition or removal,
Relational Initialisation, mediation, real distribution, or Architecture 0.8 conformance.
