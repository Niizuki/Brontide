# CBI12 multi-member activation capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

Every slice from CBI1 to CBI11 held one thing fixed: a single member of a single direct `1..1`
position. CBI12 activates several members together, and answers the first question a second member
raises.

**The release barrier is the activation, not the member.** CM4 models one logical Release for an
activation attempt, so ordinary interaction opens for every member at once or for none — the answer
comes from the runtime's own shape rather than from a choice made here. A member that reached Ready
while another failed is not released; it is retired.

Cyclic groups are out of scope. A group with several members is a strongly connected component,
which is what Relational Initialisation exists for, and approximating that stage would decide
CM3's semantics invisibly. CBI12 therefore takes several protocol-free single-member groups within
one activation, which is what independent members resolve to.

## C1 — the plan and the selections describe the same members

Each selected occurrence is the sole member of its own group, every group is protocol-free, and the
groups' members are exactly the selected occurrences — no more, no fewer. A plan carrying a
lifecycle protocol is refused rather than activated without its Relational Initialisation stage.

Property: no activation proceeds where a planned member is unselected, a selected member is
unplanned, a group holds more than one member, or any group declares a protocol.

## C2 — every member is prepared before any provider is contacted

Each selection is prepared through CBI1 in a deterministic order. If any preparation is refused, no
member is prepared into a live binding and no provider is reached.

Property: a refused preparation leaves no portable member interconnected and no provider effect.

## C3 — CM4 is validated before establishment and again before Release

The whole activation is validated once with every member's stages derived as successful, before any
provider is contacted, and again after every member reports Ready. Neither pass takes a caller's
stage claim.

Property: a CM4 refusal before establishment reaches no provider, and no member is released while
CM4 refuses the activation.

## C4 — the release barrier is the activation

No member's ordinary-interaction gate opens until every member has been established, has reported
Ready, and CM4 has accepted the activation. Release is then performed for every member.

Property: in every result, either every member is released or none is.

## C5 — a failed member retires the members that succeeded

When any member fails to establish or fails to report Ready, the activation fails, and every member
already interconnected is retired: gate closed, then withdrawal and termination. A member that
succeeded is not left holding an open channel because another member failed.

Property: after every failed activation no member is released, and an ordinary interaction cannot
reach any provider.

## C6 — the failure names the member that caused it

A failed activation reports which occurrence failed and the portable code it failed with, and the
CM4 outcome derived from that member's failed stage. Cleanup failures encountered while retiring the
others are reported alongside it rather than replacing it.

Property: every establishment failure names exactly one member as its cause.

## C7 — members stay independent of each other

Each member has its own resolved position, its own portable contract, its own conversation, and its
own Binding Plan. Nothing about one member's provider enters another's contract, plan, or payload,
and the members are not required to share a contract, provider, or endpoint.

Property: no member's portable facts depend on how many other members the activation has or on
which providers they bound to.

## C8 — the ordering is deterministic

Preparation, establishment, Ready checks, Release, and retirement all follow one order derived from
the selected occurrences, so the same inputs produce the same sequence of provider contacts and the
same reported failure when more than one member could fail.

Property: the reported outcome does not depend on the order the caller listed the members in.

## C9 — both composition roots implement independently

Reference Studio and Minimal Host own separate group coordinators over their native CM2, CM3, CM4,
and PB7 types. CBI12 is additive: CBI2's single-member lifecycle and everything built on it are
unchanged.

Property: deleting either CBI12 coordinator leaves native CM2-CM5, CBI1-CBI11, and Portable Binding
behavior unchanged.

## C10 — evidence remains bounded

CBI12 proves one activation of several independent, protocol-free members with the barrier at the
activation. It does not activate a cyclic group, execute Relational Initialisation, order members by
dependency, admit authority for several members, revalidate or revise a multi-member set, replace a
member of a live activation, or provide production identity, policy, distribution, or security.

Property: every CBI12 status statement preserves these limits.
