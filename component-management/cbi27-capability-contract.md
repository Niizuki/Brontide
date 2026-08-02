# CBI27 wider Provider Set capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI27 carries a CM2 position whose cardinality is **not** `1..1` into portable preflight. CBI1 accepts
`1..1` and nothing else, and the portable seam refuses any other bound because *"a Provider Set needs
membership, exposure, and mediation semantics this seam does not own"*.

**CBI25's test decides it: does the refused thing have a representation the seam already holds?** A
Provider Set's *members* do — each is one provider answering one contract, which is the only thing the
seam binds. The *set* does not: nothing in the seam says that these n bindings answer one requirement
together. So a wide position is translated as **n ordinary members, and the set stays at the
composition root**, which already holds several members as one activation. The seam is not widened, no
refusal is relaxed, and a wide requirement offered to it is still refused.

**The finding is what the fan-out needs and CM2 does not supply: one binding scope per member.** The
portable scope is *"the composition's identity for this position"*, one member holds it, and the seam's
own `scope-uniqueness` silence says a composition that reuses one has *"two members claiming one
position, which its own resolver is the place to reject"*. A CM binding scope is not that. It is a
container: CM2 looks occupied bindings up by scope **and contract**, distinguishes them by `BindingId`,
and refuses several in one scope only when the position is `1..1`. CBI1 mapped one onto the other, and
that is a bijection only while a position is `1..1` **and** a scope holds one position. A wide set
breaks the first by construction, so CBI27 takes the portable scope explicitly, per member, exactly as
CBI1 takes every other portable identity. The second is already broken elsewhere, which C3 records.

## C1 — the position must be wider than `1..1`, and it must be distinct

The input names the requirement and supplies one member selection per resolved member. A `1..1`
position is refused, because CBI1 translates it and two paths for one shape would let a caller pick
which rules apply. A mediated position is refused too: CBI25 binds the Component its Mediation is
realized as, whatever the cardinality, so mediation is not this path's question. A **distinct**
position that carries a Mediation declaration anyway is also refused — CM2 records one and ignores it,
so exposure and the declaration are two facts a caller can disagree with, and checking one leaves the
other unchecked.

Property: every refusal produces no portable member at all.

## C2 — the membership is the generation's statement, not the caller's

Exactly one selection per resolved member, naming the position's own requirement and that member's
definition and occurrence. An omitted member, a supplied occurrence the position does not resolve, and
a member supplied twice are all refused. This is CBI20's rule at the scale of one position: the caller
maps identities and does not decide which providers answer.

Property: the occurrences of an admitted translation equal the position's resolved members exactly.

## C3 — each member carries its own binding scope, and CM2 names only the position

The caller supplies one portable binding scope per member, and they must be distinct across the set.
CM2 gives the position one scope, so nothing else can: minting one here would make the composition root
the author of which binding is which, and reusing the position's would produce exactly the state the
seam tells a composition to reject. The CM scope is carried as provenance instead.

CBI1's C4 — *"every successful member reports the same scope text the resolved Provider Set carried"* —
therefore describes `1..1` rather than a general rule, and it is unchanged where it was stated.

**The same collision is already reachable without a wide set, and this slice does not close it.** Two
distinct positions resolved in one CM binding scope produce two portable members that both report it,
which is the seam's `scope-uniqueness` case arriving through CBI1's mapping rather than through
cardinality. Both stacks do it identically, and no vector ever asked, because every fixture derives its
positions from one requirement template. A named test pins it rather than describing it. Correcting it
means changing what CBI1's mapping produces, which moves the `bindingScope` fact of every member and so
every CBI4 profile digest the shared fixture pins; that is a repin, and it is raised as **Decision 16**
rather than taken here.

Property: no two members of an admitted translation report the same portable binding scope.

## C4 — a refused member leaves no member at all

If any member's preparation is refused — a mapping that names another Component, an endpoint outside
the contract's text bound, a seam refusal — the whole position produces nothing. The rule comes from
the seam's own words rather than from a preference: it refuses a wide cardinality *"rather than
narrowed to a first member"*, and a composition root that kept the members which happened to work would
perform exactly that narrowing one level up, where the seam cannot see it.

Property: a declined translation produces zero portable members, whatever the position resolved.

## C5 — the set is not a portable fact

Each prepared member is an ordinary CBI1 member: cardinality `1..1`, distinct exposure, one provider,
one binding scope. Nothing tells the peer that it is one of several, because nothing in the seam can
say so and inventing a fact would be the erasure in the other direction. Every later slice therefore
accepts these members without knowing a wide position was involved.

Property: a wide `PortableResolvedRequirement` offered directly to the seam is still refused as
`cardinality-unsupported`, before and after this slice.

## C6 — a position that resolved nothing binds nothing, and says so

An optional position may complete with no members at all. There is nothing to bind and nothing wrong,
so this is neither a translation nor a refusal but its own outcome, and the caller must supply no
selections for it. Reporting it as an empty success would make "nothing was bound" indistinguishable
from "this position was never translated".

Property: an unfilled position produces no portable member and no refusal.

## C7 — what the set carries beyond its members is not carried

Two things a Provider Set states are not expressible as n members, and both are named rather than
approximated.

**How many members the requirement needs.** `Cardinality.Minimum` says when the position is satisfied,
so a `1..3` set that loses one member of three may still be satisfied. Nothing here owns that, and the
activation's existing answer is stricter: a lapse in any member retires the whole activation, as CBI14
requires. That is safe, and it is stated so that the strictness does not read as a decision about set
semantics that nobody made.

**Unfilled optional capacity.** A position may resolve fewer members than its maximum permits. The
translation reports how many positions are unfilled, and filling one later is a new generation's work
under CBI20's cutover-only rule rather than growth of a live set.

Property: the translation reports the position's declared cardinality and its unfilled optional
positions, and no portable member reports either.

## C8 — both composition roots implement independently, and evidence remains bounded

Reference Studio and Minimal Host own separate translations over their native CM2 and PB7 types,
delegating each member's preparation to their own CBI1 path. CBI27 is additive: CBI1 through CBI26 are
unchanged.

CBI27 proves fail-closed translation of a wide distinct position into one portable member per resolved
member, at preflight. It does not activate them: the group activation path prepares from selections
through CBI1, which refuses a wide position, so **a fanned-out set has no activation path yet** and
wiring one is the next slice rather than an omission from this one. It also does not decide set
satisfaction under member loss, fill unfilled capacity, express a Provider Set at the portable seam, or
provide production identity, policy, distribution, or security.

Property: deleting either translation leaves native CM2, CM4, CM5, and Portable Binding behavior
unchanged, and every CBI27 status statement preserves these limits.
