# CBI13 multi-member authority capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI12 moved the lifecycle to several members while authority still governed one. CBI13 closes that
gap, and answers the two questions the plan raised when it named this item.

**Authority is admitted per member, not per activation.** CM5 admits participants against a
receiving domain for a target Actor, and CBI3 ties that admission to an occurrence. An occurrence is
a durable thing; an activation attempt is a runtime event. Admitting a set against an attempt would
attach authority to the wrong thing, and would have to be re-decided on every restart.

**The authority barrier and the release barrier are two barriers, and the authority one is
strictly earlier.** The plan guessed they might be the same. They are not: authority is a
precondition evaluated before any provider is contacted, and Release is a barrier reached after
every member has reported Ready. What they share is being all-or-none over the activation — no
member's authority failure lets any other member proceed — but they sit at opposite ends of it.

## C1 — every member carries its own participant set

Each member of the activation supplies its own occurrence, its own CM5 requests, and its own
admission. Nothing merges the sets: two members admitting the same participant do so separately, and
each grant names the member's own target Actor, Operation, and scope.

Property: no member's admission decision is derived from another member's requests or outcomes.

## C2 — every set is admitted before any provider is contacted

All members' sets are admitted first, in a deterministic order, and only then does the activation
begin. CM5 evaluation is effect-free, so admitting them all costs nothing that a refusal would have
to undo.

Property: after every authority refusal, no member has been prepared, no provider has been reached,
and no provider effect exists.

## C3 — identities stay distinct across the whole activation

Admission, relationship, and authority request identities are pairwise distinct across every request
of every member, not only within one member's set. A grant identity derives from an authority
request identity, so a collision between two members would produce grants the activation cannot tell
apart — the same reason CBI6 requires it within a set, one level out.

Property: any identity repeated across two members refuses the activation before a single request is
evaluated.

## C4 — the receiving-domain Actor mapping is a function and it is injective

Across the activation, one participant maps to exactly one local Actor, and one local Actor is
mapped from exactly one participant. The same party participating in two members is legitimate and
must map consistently; two different parties arriving at one local Actor is the conflation CBI6
refuses within a set, and it is no less a conflation across members.

Property: every activated set of members has as many distinct local Actors as it has distinct
participants, and no participant holds two.

## C5 — a refused member refuses the activation

If any member's set is not admitted exactly, the activation does not begin. The other members'
admissions are reported for attribution, and the member whose authority was refused is named.

Property: no member is prepared or established while any member's authority is unadmitted.

## C6 — admission does not replace the release barrier

An admitted activation still has to pass CBI12: every member established, every member Ready, CM4
Active, and only then Release for all of them. Authority permits the attempt; it does not conclude
it.

Property: an activation with every member's authority admitted can still end with no member
released, and reports both facts.

## C7 — authority still never crosses the portable trust boundary

As in CBI3 and CBI6, admitted relationships and grants are receiving-domain observations controlling
whether the composition may continue. Nothing from any member's CM5 decision enters any member's
portable contract, Binding Plan, constraint, or payload, and no member learns how many participants
another member has.

Property: changing any member's participant set can change whether the activation proceeds, but
cannot change any portable contract or Binding Plan fact.

## C8 — both composition roots implement independently

Reference Studio and Minimal Host own separate group-authority coordinators over their native CM5,
CM4, and PB7 types. CBI13 is additive: CBI6's single-member admission is unchanged and is now reached
through the same effect-free admission step this slice uses.

Property: deleting either CBI13 coordinator leaves native CM5, CBI1-CBI12, and Portable Binding
behavior unchanged.

## C9 — evidence remains bounded

CBI13 proves per-member admission gating one multi-member, protocol-free activation. It does not
revalidate, extend, revise, or verify a multi-member set — CBI7 through CBI11 remain single-member —
admit authority for a cyclic group, order members by dependency, share a grant between members, or
provide production identity, policy, distribution, or security.

Property: every CBI13 status statement preserves these limits.
