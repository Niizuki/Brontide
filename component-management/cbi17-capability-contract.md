# CBI17 multi-member declaration succession capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI17 lifts CBI11's succession — narrowing a declaration to a successor resolution of the same
position, with observed use as a veto — to a multi-member activation, and answers the two questions
that lift raises.

**A succession is decided per member and applied to the activation as one transaction.** The
permission a declaration narrows against is a *generation*, and a CM2 generation is one immutable
object that resolves every position at once. Applying the members a successor generation narrows
while refusing the rest would leave the activation holding declarations drawn from two generations,
which is a state no generation records. So the answer comes from CM2's shape rather than from a
preference, as CBI12's release barrier came from CM4's.

**A member the successor does not narrow is unchanged, not refused.** CBI11 refuses an unchanged
declaration because a single-member succession that changes nothing has nothing to succeed. Over an
activation, a successor that narrows one Component and leaves another alone is the ordinary case, so
the two things CBI11's rule conflated — *nothing to succeed* and *this member is untouched* — come
apart here. The activation-level rule is the one that survives: at least one member must narrow.

## C1 — succession needs a released activation and one entry per member

The input is a released CBI13 activation, one completed successor generation, and one entry per
member it admitted, each naming that member's own selection, its declaration in force, the successor
declaration, and the attribution and observations CBI10 uses. A member set the activation did not
admit is refused.

Property: no succession proceeds on an unavailable activation or on a member set the activation did
not admit.

## C2 — every member's position must survive into the successor

Each member's successor position is checked as CBI11 checks it: the same requirement resolved to the
same definition and occurrence, as one direct `1..1` distinct position, under the binding scope that
member's live portable member itself records. A generation that fails this for **any** member is not
a successor of this activation, so it narrows none of them.

Property: a successor that does not resolve every member's position as the live activation holds it
changes no member's declaration.

## C3 — both declarations of every member must be the ones their generations record

Each member's declaration in force is checked against the current generation and its successor
declaration against the successor generation, by the rule CBI9 uses. A successor that declares
nothing for a member is refused rather than treated as depending on nothing, exactly as CBI11 refuses
it.

Property: no succession proceeds on a declaration either generation does not record for the member
that claims it.

## C4 — no member widens, and at least one narrows

Every member's successor names must be a subset of its names in force — equality permitted, because
that is an untouched member. Across the activation at least one member's names must be a strict
subset, because an activation-wide restatement succeeds nothing.

Property: no member's declaration ever gains a name, and every applied succession narrows at least
one member.

## C5 — a retained dependency keeps its exact tuple, per member

Every name a member's successor keeps must map to the identical Capability, target Actor, Operation,
and scope it mapped to before. Succession removes dependencies; it does not re-point them, and no
member's tuples are checked against another's.

Property: no retained declared authority of any member changes tuple across a succession.

## C6 — the veto is per member and refuses the whole succession

Each member's exercised authority is computed from that member's own attribution and observations, as
CBI16 attributes them. A dependency any member has already exercised cannot be narrowed away, and
because the succession is one transaction, one member's veto refuses all of it — including the
narrowings of members that had none.

Property: no authority attributed to a delivered interaction is ever dropped, and a vetoed succession
drops nothing anywhere.

## C7 — the result names which members narrowed and which vetoed

An applied succession reports, per member, the authorities it dropped and the declaration now in
force. A veto reports which members vetoed and which exercised authorities did it, so a member whose
narrowing was refused because a sibling vetoed is never reported as the cause — CBI14's separation of
cause from consequence.

Property: every applied succession names at least one member with a non-empty dropped set, and every
veto names at least one member with a non-empty vetoed set.

## C8 — nothing retires and nothing is performed

CBI17 has no retirement path at all, and no succession alters any member's participant set, grants,
portable stage, or any portable fact. Narrowing only changes what a later CBI15 revision will admit,
which is CBI11's C7 over an activation.

Property: every member is released after every CBI17 outcome, and the admissions and grants in force
are identical before and after every one of them.

## C9 — both composition roots implement independently

Reference Studio and Minimal Host own separate activation successors over their native CM2, CM5, and
PB7 types. CBI17 is additive: CBI11's single-member succession is unchanged.

Property: deleting either CBI17 implementation leaves native CM2, CM5, CBI1-CBI16, and Portable
Binding behavior unchanged.

## C10 — evidence remains bounded

CBI17 proves that the declarations of one protocol-free multi-member activation narrow only when the
Components' own successor generation narrows them, never against observed use, and only all at once.
It inherits CBI11's limits — it does not decide whether the successor generation is trustworthy,
verify that a narrower declaration is truthful, or replace a live member with a successor
generation's member — and adds nothing about member addition or removal, scoped replacement,
Relational Initialisation, mediation, real distribution, or production identity, policy, or security.

Property: every CBI17 status statement preserves these limits.
