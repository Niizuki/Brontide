# CBI15 multi-member participant revision capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI15 lifts CBI9's revision — removing, substituting, and adding participants under a declared
dependency — to a multi-member activation, and answers the question CBI14 left.

**A change is decided per member and checked against the activation.** CBI13 admits authority per
member because admission is about an occurrence; CBI14 retires per activation because retirement is
about a restart scope. Revision is an admission-level operation, so changing member A's set decides
nothing about member B's authority. But the invariants CBI13 established are activation-wide, so the
*result* is checked against every member: identities stay distinct across the activation, and the
receiving-domain Actor mapping stays a function and injective across it.

**A declined change is local; a discovered lapse is global.** The same call can produce either. A
revision the activation will not admit changes nothing at all, as CBI8 and CBI9 established. But
evaluating the intended sets can reveal that a *retained* participant's authority has lapsed, and
that is CBI14's case rather than this one: the whole activation retires.

## C1 — the intended activation is stated in full, and something must change

The caller states the complete intended participant set for every member of the activation, naming
exactly the members it has. Members that are not changing restate their current sets. At least one
member's set must differ, because an activation-wide restatement of what is already in force is a
revalidation and belongs to CBI14.

Property: no revision proceeds that names a different set of members, or that changes no member.

## C2 — a member set named wrongly is declined, not retired

Naming the wrong members is a malformed request rather than evidence about authority, so it leaves
the activation exactly as it was. This is deliberately unlike CBI14, where a changed member set
retires: revalidation asserts continuity of the whole activation and cannot demonstrate it, while a
revision merely asks for something the activation will not do.

Property: every structural refusal leaves every member released and every participant set unchanged.

## C3 — each member's declaration is its own

Each member's declared dependency names the requested authority CM2 records for that member's own
selected definition, and each member's intended set must cover that member's own declaration. No
member's declaration constrains another's.

Property: a member's revision is admitted or refused on its own declaration and its own coverage,
never on another member's.

## C4 — the activation-wide rules are checked over the result

Admission, relationship, and authority request identities stay pairwise distinct across every
member's intended set, and the receiving-domain Actor mapping stays a function and injective across
the activation. An addition to one member is a new opportunity for exactly the collisions CBI13
refuses, now against members that are already live.

Property: no revised activation contains a repeated identity, a participant holding two local
Actors, or two participants sharing one.

## C5 — retained participants are revalidated in the same evaluation

Every member's intended set is evaluated together, all or none. A participant a member keeps must
reproduce its established relationship and grants exactly; a participant being added must be
admitted exactly as CBI13 requires. A participant being dropped is not evaluated, because after the
revision it holds nothing in this activation.

Property: a result carries either no CM5 observation at all or exactly one per intended participant
of every member.

## C6 — a lapse in any member retires the whole activation

If a retained participant of any member no longer renews, the activation retires — every member,
gate closed first — whatever the revision was trying to do, and whether or not the lapsed
participant belonged to the member being changed. The activation shares a restart scope, so it
shares a fate.

Property: no result both retires the activation and reports zero evaluations, and after every
retirement either every member is released or none is.

## C7 — a malformed request decides nothing

A retained request that does not re-identify its participant's authority is declined with the
activation untouched: nothing was evaluated, so nothing was learned. Only evaluated loss retires.

Property: every declined result leaves the activation released with the sets it already had.

## C8 — a revision produces an activation the other slices accept

A successful revision returns the activation in the form CBI13 produces: every member's current
observations and grants, and the same released members. CBI14 can revalidate that result, and a
further CBI15 call can revise it.

Property: the result of a revision is accepted by CBI14 revalidation, and revalidating it
immediately with the same requests continues it.

## C9 — both composition roots implement independently

Reference Studio and Minimal Host own separate activation revisers over their native CM2, CM5, and
PB7 types. CBI15 is additive: CBI9's single-member revision is unchanged.

Property: deleting either CBI15 reviser leaves native CM2, CM5, CBI1-CBI14, and Portable Binding
behavior unchanged.

## C10 — evidence remains bounded

CBI15 proves fail-closed revision of the participant sets of one multi-member, protocol-free
activation under per-member declarations. It does not extend a set without a declaration, verify a
declaration against observed interaction, narrow one by succession, add or remove a member, perform
a scoped replacement, or provide production identity, policy, distribution, or security.

Property: every CBI15 status statement preserves these limits.
