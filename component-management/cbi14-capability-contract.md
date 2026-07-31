# CBI14 multi-member revalidation and withdrawal capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI13 admitted authority per member; CBI7 through CBI11 still governed one. CBI14 lifts the first of
them — revalidation and withdrawal — to the activation, and answers the question CBI13 left.

**When one member's authority lapses, the whole activation retires.** The answer comes from CM4's
structure rather than from preference, as CBI12's release barrier did. A CM4 activation has exactly
one restart scope, and every member of a CBI12 activation is inside it. CM4 models no way to retire
one member while its scope keeps running — doing that is a scoped replacement, which is a different
operation CM4 declares separately and which this slice does not perform. The members came up
together inside one scope, and they go down together.

That members are otherwise independent — separate positions, contracts, conversations, and plans —
is exactly why this had to be decided rather than assumed. Independence is about what they need from
each other, not about what scope they share.

## C1 — only a completely admitted, released activation can be revalidated

The input is a successful CBI13 result: every member admitted exactly, every member released. A
refused, partial, or already-retired activation is not silently treated as live authority.

Property: every unavailable input produces no CM5 evaluation, no retirement attempt, and no new
lifecycle effect.

## C2 — the member set must be identical

The fresh requests name exactly the members the activation named — no additions, no removals, no
substitutions. Membership is compared before anything is evaluated, because a different set of
members is not a revalidation of this activation.

Property: any added, removed, or substituted member retires the activation without evaluating a
single request.

## C3 — each member re-identifies its own set exactly

Within each member, CBI7's rules apply unchanged: the participant set is identical, and every
request preserves its admission-request and policy identities, participant, relationship request,
and every grant's Capability, target Actor, Operation, and scope. Evaluation time, evidence state,
validity interval, trusted issuers, and policy rules may change.

Property: identity drift in any member's request prevents every evaluation and cannot keep the
activation released.

## C4 — evaluation is all-or-none across the activation

Once the structure is valid, every member's fresh requests reach the native evaluator, in a
deterministic order, all or none. A result carries either no CM5 observation at all or one per
participant of every member.

Property: no result reports a prefix of the activation's members as evaluated.

## C5 — continuation requires every member to renew exactly

Every member stays released only when every one of its participants reproduces the established
relationship and grant list exactly, including receiving-domain Actor mapping, policy, and admitting
rules — and every other member does the same.

Property: every continued result reproduces, for every participant of every member, the authority
CBI13 admitted.

## C6 — a lapse in one member retires them all

When any member's authority lapses, every member is retired: gate closed first, then withdrawal and
termination, in a deterministic order. The activation shares a restart scope, so it shares a fate.

Property: after every result, either every member is released or none is.

## C7 — the result names which members lapsed and which participants within them

A withdrawal reports the members whose authority was not renewed and, inside each, the participants
that caused it. A member retired only because a sibling lapsed is reported as retired without being
reported as lapsed, so the cause stays distinguishable from the consequence.

Property: every withdrawal names at least one lapsed member, and no member is named lapsed whose
participants all renewed.

## C8 — cleanup failure is visible and stays fail closed

Retirement closes each member's gate before its withdrawal and termination traffic. A provider
cleanup failure produces a structured retirement failure that cannot restore any member's authority
or reopen any member, and no successful replacement record is fabricated for a member whose peer
state is unknown.

Property: after every non-continued result an ordinary interaction cannot reach any provider, even
when a peer cleanup subsequently fails.

## C9 — both composition roots implement independently

Reference Studio and Minimal Host own separate activation revalidators over their native CM5 and PB7
types. CBI14 is additive: CBI7's single-member revalidation is unchanged, and CBI8 through CBI11
remain single-member.

Property: deleting either CBI14 revalidator leaves native CM5, CBI1-CBI13, and Portable Binding
behavior unchanged.

## C10 — evidence remains bounded

CBI14 proves fail-closed revalidation and whole-activation withdrawal for one multi-member,
protocol-free activation. It does not perform a scoped replacement, retire one member while its
scope runs, extend, revise, verify, or narrow a multi-member declaration, cancel an in-flight
execution, or provide production identity, policy, distribution, or security.

Property: every CBI14 status statement preserves these limits.
