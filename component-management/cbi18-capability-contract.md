# CBI18 multi-member participant extension capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI18 lifts CBI8's declaration-free extension — growing an admitted participant set while its member
stays released, refusing removal and substitution in place — to a multi-member activation. It is the
last of the single-member slices to be lifted, and it answers the two questions that lift raises.

**An activation may hold declarations for some members and none for others, and growth is indifferent
to all of them.** CBI15 requires a declaration per member because it removes and substitutes
participants, and a declaration is what says whether a departing participant may go. Growth removes
nobody: a set that covered its declaration still covers it after growth, because coverage is
monotone in the grants held. So CBI18 takes no declaration for any member, and a member that has one
is grown by the same rule as a member that does not — the absent parameter is the contract.

**Growth of one member is checked against the whole activation**, as CBI15's revision is. CBI13's
identity and receiving-domain Actor rules are activation-wide, and an addition to one member is a
fresh opportunity for exactly those collisions, now against members that are already live.

## C1 — extension needs a released activation and the members it admitted

The input is a released CBI13 activation and one entry per member it admitted, each naming that
member's complete intended participant set as one fresh CM5 request each. Mappings are not
resupplied: each member and its occurrence are already fixed, so restating them could only introduce
drift. A member set the activation did not admit is declined.

Property: every unavailable input, and every entry list naming other members, produces no CM5
evaluation, no lifecycle effect, and no extended set.

## C2 — every member retains everyone, and the activation gains someone

No member's intended set may drop, substitute, or repeat a participant; every current participant of
every member appears in that member's intended set. Across the activation at least one member must
gain a participant, because an activation-wide restatement is a revalidation and belongs to CBI14. A
member that gains nobody restates its own set and is untouched.

Property: no intended activation that removes, substitutes, or repeats a participant changes
anything, and no extension is applied in which no member grew.

## C3 — no declaration is consulted, for any member

CBI18 reads no resolution and no declaration. Growth cannot uncover a declared dependency, so a
member holding one is extended by the same rule as a member holding none, and the two may sit in one
activation.

Property: the outcome of an extension is unchanged by whether any member has a declaration in force,
and no result reports a coverage decision.

## C4 — a declined extension changes nothing, anywhere

Declining is not a failure of the activation. When CBI18 refuses — an invalid intended set, a
malformed retained request, an addition the evaluator will not admit, or a result that would collide
on identity or receiving-domain Actor — every member stays released with the authority it already
had, and the result carries the unchanged activation as the one still in force.

Property: every declined result leaves every member released and reports an in-force activation
exactly equal to the one it was given.

## C5 — a malformed request decides nothing; evaluated loss decides everything, for all members

A retained request that does not re-identify its participant's relationship and grants is declined:
nothing was evaluated, so nothing was learned. A retained participant that is evaluated and no longer
reproduces its identical admission is positive evidence of loss, and retires **the whole activation**
as CBI14 does, whatever the extension was trying to add and whichever member the lapse was in.

Property: no result both retires the activation and reports zero evaluations, and after every
retirement either every member is released or none is.

## C6 — retained authority is revalidated before it is extended

Once every member's intended set is structurally valid, every request across the activation — retained
and added alike — is evaluated in a deterministic order, all or none. No set is extended on top of
authority that has itself lapsed, and a lapse outranks any problem with an addition, so a call that
would both retire and decline retires.

Property: a result carries either no CM5 observation at all or exactly one per intended participant
of every member, and no extended result exists where a retained participant failed to renew.

## C7 — an added participant is admitted on CBI13's terms

Each added request carries one `ComponentParticipant` relationship proposed by its own participant
and one or more non-unlimited authority requests dependent on it, with distinct tuples, and the
evaluator must admit it exactly. An addition the evaluator refuses declines the extension rather than
retiring the activation.

Property: an added participant that CBI13 would refuse admission is refused here too, and its refusal
leaves every member released.

## C8 — the extended activation obeys the activation-wide rules, against members already live

Admission, relationship, and authority request identities stay pairwise distinct across every
member's intended set, and the receiving-domain Actor mapping stays a function and injective across
the whole activation.

That second rule bites in both directions here, and the permitting direction is the one only a
multi-member activation can reach: a party that is already a participant of another member **may** be
added to a second member, and must then map onto the identical local Actor it already holds. Two
parties arriving at one local Actor is still the conflation CBI6 refuses; one party arriving at two
is refused as it is in CBI13.

Property: no extended activation contains a repeated identity, a participant holding two local
Actors, or two participants sharing one — and a party added to a second member under its established
local Actor is admitted.

## C9 — an extension produces an activation the other slices accept

A successful extension returns the activation in the form CBI13 produces: every member's current
observations and grants, and the same released members. CBI14 can revalidate that result, CBI15 can
revise it, and a further CBI18 call can extend it.

Property: the result of an extension is accepted by CBI14 revalidation, and revalidating it
immediately with the same requests continues it.

## C10 — both composition roots implement independently, and evidence remains bounded

Reference Studio and Minimal Host own separate activation extenders over their native CM5 and PB7
types. CBI18 is additive: CBI8's single-member extension is unchanged.

CBI18 proves fail-closed declaration-free growth of the participant sets of one protocol-free
multi-member activation. Removal and substitution in place remain CBI15's, under per-member
declarations; CBI18's growth-only rule stays the safe one wherever a member has no declaration. It
inherits CBI8's boundary, which a mixed activation makes visible per member: an undeclared member's
ordinary interaction cannot be verified, because CBI16 derives admission from a declaration and there
is none to derive from. It does not order participants by priority, let a participant declare itself
required, add or remove a member, exercise any granted Operation, notify a provider that a set
changed, or provide production identity, policy, distribution, or security.

Property: deleting either CBI18 extender leaves native CM5, CBI1-CBI17, and Portable Binding
behavior unchanged, and every CBI18 status statement preserves these limits.
