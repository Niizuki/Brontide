# CBI22 child-Port activation capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI22 activates a Component position CM2 resolved **inside a child Port**, in its own restart scope,
attached to the scope and generation a released parent activation made active. Every slice from CBI2
onward has used one restart scope and never looked at Port containment at all.

**The integration did not refuse a Port-contained position; it ignored the containment.** A CM2
`ProviderSetObservation` carries the Region and Port a position was resolved into, and CBI1 reads
neither. A position resolved inside a child Port was therefore flattened into an ordinary one and
activated in whatever restart scope the caller's plan happened to name, with no child declaration —
so CM4 never saw an attachment, the parent generation was never named, and the Port CM2 placed the
Component in was silently dropped. The future-work index asserted the opposite, that such a position
was refused. It was not, and nothing tested it.

## C1 — a child activation needs a released parent and an attachment read from it

The input is a released CBI13 activation over the parent scope, a completed resolution whose selected
positions are Port-contained, one entry per child member, and a CM4 request whose plan uses a restart
scope of its own and whose child declaration names the parent.

The declared parent scope and generation must be the ones the parent activation made active, read
from CM4's own observation rather than from the caller's plan, as CBI19 reads a retained generation.
A child scope equal to the parent's is refused: CM4 requires them distinct, because the point of a
child Port is a restart boundary.

Property: every refusal before establishment leaves the parent active and every parent member
released, and creates no child member.

## C2 — the Port is the generation's, not the caller's

Every selected member must be contained in one and the same Port, and the attachment must name that
Port. Members drawn from two Ports, a member with no containment at all, and an attachment naming a
Port the generation did not resolve into are each refused. The lifecycle facts of the attachment are
read from the resolved envelope too: a caller may not declare a Port runtime-open that CM2 resolved
as activation-open.

CM2 refuses a sealed Port at resolution, so a sealed Port never reaches this seam; what reaches it is
a caller disagreeing with the generation about an open one, which is the reachable form of CM4's
closed-Port refusal and the only one this slice can produce.

Property: every admitted attachment names the Port every one of its members was resolved into, with
the lifecycle the resolved envelope declared.

## C3 — a Port-contained position outside a child activation is refused

An activation whose request declares no child attachment refuses a member CM2 contained in a Port,
rather than activating it as though it were top-level. This is the correction: the containment is a
statement the generation made about where the Component runs, and dropping it silently is how a
Component ends up outside the restart boundary its Port exists to give it.

The converse is refused too: a child attachment whose members are not Port-contained has nothing to
attach.

Property: no member is prepared for an activation whose Port containment and child declaration
disagree.

## C4 — an occupied Port needs an explicit replacement lifecycle declaration

Attaching to a Port that already holds a child is not initial attachment. CM4 refuses it unless the
request declares replacement lifecycles, and CBI22 reports that refusal as CM4 classifies it rather
than forming its own.

Property: an occupied-Port refusal reaches no provider and retires no parent member.

## C5 — a host-assisted export follows the child's internal Release

When the attachment is host-assisted, CM4 requires the exported outer boundary to be released after
the child's internal Release. CBI22 supplies the sequence and reports CM4's classification of a
disordered one.

Property: no admitted host-assisted attachment reports an export at or before its internal Release.

## C6 — the parent is untouched, in every outcome

Success or refusal, the parent scope stays active, its generation unchanged, and every parent member
released and able to interact. A child activation is a second activation, not a replacement of the
first, and nothing in it stands the parent down.

Property: after every CBI22 outcome the parent activation's members are all still released, and an
ordinary Operation on one of them is still served.

## C7 — the child's barriers are its own

The child's release barrier is CBI12's over the child's members, and the parent's is not re-armed:
they are separate CM4 attempts, with separate plans, separate Releases, and separate restart scopes.
A child that never reaches Ready releases no child member and changes nothing about the parent.

Property: after every outcome either every child member is released or none is, and the parent's
released members are the same ones either way.

## C8 — authority is the child's own

Every child member is admitted on CBI13's terms, per occurrence, before any child provider is
contacted. The parent activation's admissions and grants admit nothing for a child member: an
occurrence in a child Port is an occurrence like any other, and CBI13 admits against occurrences.

Property: no child member is released without its own admission in this attempt, and no grant of the
parent's appears in the child's.

## C9 — both composition roots implement independently

Reference Studio and Minimal Host own separate child-attachment paths over their native CM2, CM4,
CM5, and PB7 types, delegating the child's activation to their own CBI13 activation rather than
restating it. CBI22 is additive: CBI13 through CBI21 are unchanged, and the group and singleton
activations change only by refusing the Port-contained position they previously flattened.

Property: deleting either child path leaves native CM2, CM3, CM4, CM5, and Portable Binding behavior
unchanged.

## C10 — evidence remains bounded

CBI22 proves fail-closed attachment of one child activation to one runtime-open Port of one released
parent. It does not nest a child inside a child, migrate a Component between Ports, seal or open a
Port at runtime, perform Relational Initialisation, mediate, widen a Provider Set, or provide
production identity, policy, distribution, or security. It models no traffic between a parent member
and a child member, because the portable seam binds a host to a provider and models no
Component-to-Component binding.

Property: every CBI22 status statement preserves these limits.
