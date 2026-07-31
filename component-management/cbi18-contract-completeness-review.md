# CBI18 contract-completeness review

Date: 2026-07-31

Scope: absence review of the CBI18 multi-member participant-extension contract, separate from
conformance review.

## Findings and dispositions

1. **Whether an activation may hold declarations for some members and none for others was the first
   question this item recorded.** Disposition: it may, and the reason is that the question was
   mis-framed. A declaration says whether a departing participant may go, and growth removes nobody;
   coverage is monotone in the grants held, so a set that covered its declaration still covers it
   after growth. CBI18 therefore consults no declaration for any member, and the mix is not a state
   it has to tolerate — it is a state it cannot observe.
2. **The absent parameter is the contract.** Disposition: recorded deliberately. CBI18's entry point
   takes no resolution and no declaration, so "growth needs neither" is enforced by the signature
   rather than asserted in prose, the way CBI17's synchronous signature enforces that succession
   performs nothing. A later implementer who needs a declaration here has changed the slice.
3. **Whether growth of one member is checked against the activation was the second question.**
   Disposition: it is, unchanged from CBI15's finding 6. CBI13's identity and Actor-mapping rules are
   activation-wide, and an addition is a fresh opportunity for exactly those collisions against
   members already live. This one is a re-application rather than a discovery, and is recorded as
   such so a reader does not go looking for a novel argument.
4. **A party already participating in another member could have been refused as a duplicate.**
   Disposition: admitted, and this is the case only a multi-member activation can pose. CBI13 permits
   one party in two members and requires the Actor mapping to be a function, so the added request
   must map onto the identical local Actor that party already holds. The rule that usually refuses
   things is here the rule that permits one, and both directions get a vector — the same party under
   its established Actor is admitted, and under a second Actor is declined.
5. **A lapse in a retained participant could have retired only its own member.** Disposition:
   refused, by CBI14. The activation shares one restart scope, and CM4 models no way to retire one
   member while its scope keeps running. A lapse also outranks any problem with an addition, so a
   call that would both retire and decline retires — CBI8's ordering, unchanged.
6. **A declined extension and a discovered lapse remain opposite in scope.** Disposition: unchanged
   from CBI8 and CBI15. A malformed retained request evaluates nothing and therefore learns nothing;
   only evaluated loss is evidence. Both are reachable from one call.
7. **"At least one member must grow" is an activation-level rule, not a per-member one.**
   Disposition: deliberate, and the same shape CBI17 arrived at for succession. A member that gains
   nobody restates its own set and is untouched; an activation where no member gains anybody is a
   revalidation and belongs to CBI14.
8. **The inherited boundary becomes visible per member.** Disposition: recorded as the limit worth
   carrying forward. CBI16 derives an exercise's admission from a declaration, so it can verify only
   the members that have one. In a mixed activation an undeclared member's ordinary interaction is
   unverifiable — which is exactly CBI8's original boundary ("nothing says what this member's
   interaction depends on"), now sitting next to members that do not share it. CBI18 does not create
   that gap and cannot close it; a member closes it by acquiring a declaration and moving to CBI15.
9. **Nothing notifies a provider that a set changed.** Disposition: unchanged from CBI8. Authority is
   a receiving-domain fact and does not cross the portable boundary, so a provider has nothing to be
   told.
10. **The single-member lifts are now complete.** Disposition: CBI8, CBI10, and CBI11 have all been
    lifted, by CBI18, CBI16, and CBI17 respectively. What remains behind this programme is not a
    lift: member addition and removal, scoped replacement, Relational Initialisation, child Ports,
    mediation, wider Provider Sets, and real distribution.
11. **Two implementations can still agree where this contract is silent.** Disposition: retained as
    a structural limitation. The vectors force the growth-only rule, the shared-party answer in both
    directions, the decline-versus-retire split, and the activation-wide identity and Actor checks;
    they cannot establish general multi-member authority-lifecycle completeness.

## Result

The CBI18 contract is complete for declaration-free growth of the participant sets of one
protocol-free multi-member activation. Finding 1 answers the first question this item recorded by
dissolving it rather than deciding it, finding 3 records that the second was a re-application, and
finding 4 is the case neither could anticipate. Finding 8 is the boundary worth carrying forward, and
finding 10 records that the lifting programme is finished. No finding requires widening this contract
into member addition or removal, scoped replacement, Relational Initialisation, mediation, real
distribution, or Architecture 0.8 conformance.
