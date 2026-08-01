# CBI21 strongly connected activation groups capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI21 takes the last stage neither the integration nor the portable seam has ever exercised:
Relational Initialisation, the bounded lifecycle handshake CM4 admits between two members of one
group before either reports Ready. Every slice from CBI12 onward has activated protocol-free
single-member groups, refusing anything else.

**The refusal covered two different things, and only one of them is Relational Initialisation.**
CBI12 refuses a group with more than one member and justifies it by saying that "a multi-member group
is a strongly connected component, which is what Relational Initialisation exists for". CM3 groups by
strongly connected component over **every** edge, not only relational ones, so two Components with
mutual ordinary-interaction edges are one cyclic group — with **no protocols, no Relational
Initialisation stage, and a stage plan CM4 activates**. That group needs nothing this seam does not
have, and CBI12 refused it for a property it does not possess. CBI21 delivers it.

**What remains refused is refused by the seam's own published contract, not by this slice.** The PB7
Composition handoff declares Relational Initialisation in its `outOfScope` list, its stages run local
initialisation → interconnection → ready → release with no slot between the last two, and a member
reports Ready *during* Interconnection, so there is no window in which a handshake could run before
the readiness CM4 requires it to precede. The seam also exposes exactly one traffic verb, and it is
gated on Release. A group carrying a declared protocol is therefore refused by name, and what a later
slice would need from Portable Binding is recorded as an owner decision rather than approximated
here.

## C1 — a group's protocols are the unit of refusal, not its members

A plan is supported when no group declares a bounded lifecycle protocol, however its members are
distributed across groups. A cyclic group of two members with no protocol is supported; a singleton
group is the same case with one member; a plan mixing both is supported.

The supplied members must be exactly the plan's members: a selected occurrence the plan does not
carry, a planned member the caller did not select, and a repeated selection are each refused with
their own code, because CBI12 reported one code for all of them and a caller could not tell which
condition fired.

Property: every refusal of the plan happens before any member is prepared, contacts no provider, and
leaves no portable member in existence.

## C2 — a declared bounded protocol is refused by name

A plan in which any group declares at least one lifecycle protocol is refused as
`relational-initialisation-unsupported`, whatever the group's member count. The refusal names the
stage rather than the shape, because the stage is what the seam cannot host: a group of one member
with a self-relational edge is refused for the same reason a cyclic pair is.

Property: no activation in which any group declares a protocol prepares a member, admits a
participant set, or reaches a provider.

## C3 — the refusal is the seam's, and this slice locates it rather than owning it

The same plan CBI21 refuses is a plan CM3 produced and CM4 accepts: given the stage observations the
plan declares, CM4 reaches Active for it. The refusal is therefore not CM3's, not CM4's, and not a
policy this slice invents — it is the portable seam declining a stage it declares out of scope.

Property: for every refused protocol-bearing plan, CM3 returned a plan and CM4 accepts that plan's
own declared stages, so the integration's refusal is the only one in the chain.

## C4 — the seam has no window for a relational stage

A portable member reports Ready as part of Interconnection: establishment and the readiness signal
are one step, and the member is Ready the moment Interconnection returns. CM4 requires Relational
Initialisation to complete *before* Ready. There is therefore no point in the portable lifecycle at
which a handshake could run and still precede the readiness it must precede.

Property: a member is Ready immediately after Interconnection and before anything else the seam
offers is called.

## C5 — the seam has no lifecycle-traffic verb

The seam carries exactly one kind of traffic a composition can initiate, and it is gated on Release.
An Operation attempted before Release is refused by the portable layer as a state violation — the
refusal is Portable Binding's own, not a check this slice adds. Establishment, readiness, withdrawal,
and termination are the seam's only lifecycle traffic, and none of them names an Operation, a
Capability, or an input Shape, which a declared protocol does.

Property: an Operation attempted on an interconnected, unreleased member is refused, and the refusal
reaches no provider.

## C6 — for the groups it delivers, nothing else changes

A multi-member protocol-free group activates on CBI12's terms exactly: every member is prepared, each
establishes independently, the release barrier is the activation's rather than the group's, and
authority is CBI13's per-member admission over occurrences. Grouping changes which members CM4 expects
stage observations for; it changes no barrier and no admission.

Property: in every accepted outcome either every member of the activation is released or none is,
whatever the grouping, and every released member carries its own admission from this attempt.

## C7 — the ordinary edges inside a delivered group are declarations, not traffic

A group's internal ordinary-interaction edges are what made it strongly connected, and this slice
performs none of them. It activates the members; whether they then interact is CBI16's verification
question over exercises a host supplies. Nothing here admits pre-Release peer traffic, which CM3
refuses at planning time in any case.

Property: an accepted activation produces no binding exercise and no provider effect of its own.

## C8 — both composition roots implement independently

Reference Studio and Minimal Host own separate plan classifiers over their native CM3 plans. CBI21 is
additive for CBI13 through CBI20, which neither gain nor lose behaviour; CBI12's plan refusal changes
only by naming which condition fired, and by admitting the multi-member protocol-free group it
previously refused.

Property: deleting either classifier leaves native CM2, CM3, CM4, CM5, and Portable Binding behavior
unchanged.

## C9 — evidence remains bounded, and the missing capability is recorded rather than approximated

CBI21 proves that a strongly connected protocol-free group activates across the portable seam and
that a protocol-bearing one is refused for a stated reason. It does not perform a lifecycle handshake,
decide whether lifecycle-traffic authority is CBI13's admission or a separate one, or decide what a
handshake failing midway leaves behind — all three are unreachable while the seam declares the stage
out of scope, and answering them here would decide a Portable Binding contract question invisibly.
What the seam would need is recorded as Decision 13.

Property: every CBI21 status statement preserves these limits, and no vector claims a lifecycle
interaction occurred.
