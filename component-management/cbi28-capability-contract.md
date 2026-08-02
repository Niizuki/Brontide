# CBI28 fanned-out set activation capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI28 activates the members CBI27 fans out. CBI27 stopped at preflight because the multi-member
activation path prepares each member from a selection through CBI1, which refuses a wide position, so
the members it produced had nowhere to go.

**Nothing downstream needed teaching.** A wide position's members are distinct occurrences, and every
slice from CBI12 onward is per-occurrence: CM3 plans occurrences, CM4 stages them, CM5 admits against
them, and CBI16 attributes interaction per member. What the activation was missing is the one thing
CBI27 found CM2 does not supply — a binding scope per member — so that is what the entry point now
takes, and the rest follows.

## C1 — a member of a wide position carries the scope its caller named

An activation member may name a portable binding scope. A member of a wide position must, because CM2
gives the position one scope for all of them; a member of a `1..1` position must not, because there the
generation names it and a caller supplying one would be disagreeing with a resolution rather than
completing it. Both mismatches are refused before any provider is contacted.

Property: every member of an admitted activation reports either the scope its caller named or, for a
`1..1` position, the scope the generation recorded.

## C2 — a wide position joins an activation whole

Every member the generation resolves for a wide position must be in the activation. The check is
against the generation, and that is the point: CBI12 already refuses a member the plan does not carry
and a planned member the activation did not select, but **both compare the caller's member list with
the caller's CM3 plan**, so a caller who omits one member of a three-member position and builds the
plan from the two that remain satisfies both checks. The position would come up two-thirds bound with
no refusal anywhere.

This is CBI27's rule lifted, and the refusal is CBI27's own: the members of one position are the answer
to one requirement, and which providers answer is not the caller's to narrow.

Property: for every wide position an admitted activation touches, its members are exactly the members
the generation resolved for that position.

## C3 — the release barrier is the activation's, and the position's minimum is not a runtime concept

A member of a wide position that never reaches Ready retires the whole activation, including the
siblings that came up and including the rest of its own position. The answer comes from the runtime
rather than from a preference, as CBI12's barrier did: a CM4 attempt has one logical Release, and a
CM3 plan has no optional member.

**The tempting alternative has no representation.** `Cardinality.Minimum` says a `1..3` position is
satisfied by one provider, so a set that loses one of three looks like it could keep serving. CM2 uses
that number at resolution to decide how many members to select and then stops carrying it: the
resolved members are indistinguishable from one another, and the required-versus-optional split
survives only as **decision provenance** — a diagnostic keyed by requirement and definition — rather
than as a fact about a member. Nothing reaches CM3 or CM4 at all. So a runtime that wanted to run a
degraded set could not tell which members it is allowed to lose, and CBI27's C7 statement that the
activation is stricter than the set is now exercised rather than asserted.

Property: no activation releases a strict subset of its members.

## C4 — authority stays per member, and nothing about the position is admitted

CM5 admits against an occurrence, so two members of one position are two independent admissions,
exactly as two members of two positions are. CBI13's rules are unchanged and unrelaxed: identities stay
distinct across the activation, and the receiving-domain Actor mapping stays a function and injective
over the whole of it. No admission, relationship, or grant names the requirement, the Provider Set, or
the position's cardinality.

Property: an activation's grants are the union of its members', and no grant is held on behalf of a
position.

## C5 — a mixed activation is the ordinary case

A wide position activates beside `1..1` positions in one attempt, under one plan and one barrier. The
members are ordered and prepared identically; only where each one's binding scope comes from differs.

Property: an activation containing a wide position and an ordinary one reaches Active exactly when
each would on its own.

## C6 — scope distinctness is checked within the position, not across the activation

Two members of one position may not hold one binding scope, which is CBI27's rule and the seam's
`scope-uniqueness` silence. The same check is **not** applied across the activation, and the reason is
recorded rather than left as an omission: two ordinary positions resolved in one CM binding scope
already reach the seam as two members reporting one scope, so an activation-wide check would refuse
what CBI1 has produced since the first multi-member slice. That is **Decision 16**, and it stays open
here.

Property: an activation whose two ordinary positions share one CM binding scope is admitted, and its
members report one portable scope.

## C7 — both composition roots implement independently, and evidence remains bounded

Reference Studio and Minimal Host own separate paths, each delegating a wide position to its own CBI27
translation and an ordinary one to its own CBI1 preparation. CBI28 is additive in behaviour: CBI1
through CBI27 are unchanged for every input they already accepted.

It is not additive in surface. An activation member gains a binding scope, which is a breaking change
to the Minimal record and a source-compatible addition to the Reference one; the migration is to leave
it absent for every member of a `1..1` position, which is every member every earlier slice activates.

CBI28 proves fail-closed activation of a fanned-out position over the fake runtime. It does not decide
set satisfaction under member loss, fill unfilled optional capacity, express a Provider Set at the
portable seam, or provide production identity, policy, distribution, or security.

Property: deleting either path leaves native CM2, CM3, CM4, CM5, and Portable Binding behavior
unchanged, and every CBI28 status statement preserves these limits.
