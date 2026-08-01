# CBI20 activation membership change capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI20 changes which positions an activation holds. CBI19 replaces the generation occupying one
restart scope with a successor that resolves the same positions; a successor that adds or drops one
is this slice, and it is the last structural constraint every slice from CBI12 onward held fixed.

**A membership change is a replacement.** Which positions exist is a property of a CM2 generation,
and a generation is one immutable object resolving every position at once, so a different set of
positions is a different generation. A generation becomes active in a restart scope only through
CM4's Release and cutover, and CM4 models one Release per attempt. There is therefore no operation
that adds a member to a released activation, and none that retires one while its scope keeps
running — which is what CBI19 established about the other half of the same question. CBI20 offers no
in-place entry point at all, and that absence is the answer rather than an omission.

**CBI19's stated limit was not an enforced one.** CBI19 says it "does not add or remove a position",
and its completeness review says a successor that does so "is not reachable through this slice".
Reading it to build CBI20 shows the limit described how CBI19 was called, not a rule it applied: its
replacer takes the caller's member list, admits an occurrence the retained activation does not hold
as a new member, and simply never visits a retained occurrence the list omits. Both changes went
through unannounced. CBI19 now refuses a member-set change and names this slice; the correction is
part of this change.

## C1 — a membership change needs a released activation, a successor for the same scope, and the change named

The input is a released CBI13 activation, a completed successor generation, one entry per successor
member, an explicit list of the occurrences the change drops, and a CM4 request whose restart scope
and retained generation are the ones the retained activation occupies and made active. CBI19's
scope and generation refusals apply unchanged. Beyond them: a repeated successor occurrence, a
declared drop the retained activation does not hold, a declared drop that is also a successor member,
and a retained occurrence the successor member list omits without declaring it are each refused. A
successor holding no member at all is refused, because a replacement stands a generation up in the
scope and standing nothing up is CBI14 withdrawal. A member set that neither adds nor drops is
declined and named as CBI19's.

Requiring the drop to be named is what makes an omission fail closed: a member left out of the
successor list would otherwise be dropped silently, which is the failure a caller cannot see.

Property: every refusal before establishment leaves every retained member released, retires nothing,
and creates no successor member.

## C2 — the successor generation decides what may be dropped; the caller only names it

A declared drop must be a position the successor generation does not resolve. A generation that
still resolves the position is the composition saying the position is still there, so dropping it
would be the caller narrowing the composition rather than the generation doing so, and it is refused.

Addition is not symmetrical, and deliberately so. CBI1 has always required the caller to supply an
explicit typed mapping for each resolved position it takes into portable preflight, so a position the
successor generation resolves and the caller does not map is simply one this activation does not
cover — nothing is taken away. A drop removes something that is live, so it must be the generation's
decision.

Property: no admitted membership change drops a position its successor generation still resolves,
and every successor member is a position that generation resolves.

## C3 — an added position joins only across a cutover

There is no path by which a member joins an activation that is already released. CBI18 grows a
member's *participant set* in place because that changes no position; growing the *member set*
changes the generation, and a generation reaches a scope through Release and cutover. Releasing one
newly added member into a live activation would be a Release for that member alone, which is the
operation CBI12 established CM4 does not model.

Property: every added member is released only in an outcome that cut over, and no outcome adds a
member to the retained activation.

## C4 — authority under a membership change

CBI19's rule holds for the occurrences it was written for, and the two new cases fall out of it
rather than needing rules of their own:

- an occurrence both generations hold must be admitted with a request that re-identifies the
  authority that admitted it;
- an occurrence only the successor holds is a new member, admitted exactly as CBI13 admits any
  participant set; and
- an occurrence only the retained generation holds is dropped: it has no successor member for its
  authority to follow to, and nothing needs to be revoked, because CBI19 already establishes that no
  authority survives an attempt. Every retained member is retired at cutover regardless of whether
  its position was dropped, and every successor member carries its own admission from this attempt.

The durability an occurrence has is therefore about what may be re-admitted, not about a grant that
outlives the attempt. A party admitted in a dropped member may be admitted in an added one, but only
by being admitted there afresh.

Property: the successor's admissions name exactly the successor's members, no grant admitted against
a dropped occurrence appears in the successor, and no successor member is released without its own
admission in this attempt.

## C5 — the receiving-domain Actor mapping is checked across both activations, not each alone

CBI13's rule — one participant holds one local Actor, one local Actor is held by one participant — is
checked over the retained and successor activations together. Between the successor reaching Ready
and the retained members retiring, both are established against the same binding scope, so a second
party arriving at a local Actor the retained generation still maps to someone else is a live
conflation rather than a hypothetical one, which is the reason CBI6 refuses it within a set.

Only a membership change can pose this: while the positions are the same, the successor maps the same
parties onto the same local Actors and the check is vacuous. Re-homing a party across a replacement
is refused for the same reason, and goes through CBI14 retirement and a fresh CBI13 activation.

Property: an admitted membership change leaves the union of the retained and successor mappings a
function and injective, and a conflation is refused before any successor provider is contacted.

## C6 — the release barrier covers the successor's members, whichever they are

Every successor member — survivors and additions alike — must reach Ready before the single Release.
The barrier is CBI12's, unchanged: ordinary interaction opens for every successor member at once or
for none. An addition that never reports Ready releases nobody.

Property: after every membership-change outcome, either every successor member is released or none
is.

## C7 — cutover is still the boundary, and before it the retained activation is untouched

Failure before cutover — admission, establishment, a member that never reports Ready, or a Release
that fails before cutover — discards the successor and leaves the retained generation active with
every retained member still released and still able to interact, including the members whose
positions the change would have dropped.

Property: no failure before cutover retires, closes the gate of, or withdraws any retained member.

## C8 — the retained members, dropped ones included, are retired after cutover and never before

Once the scope has cut over, every retained member is retired, gate first, as CBI19 retires. A member
whose position the change drops is retired then and not earlier: knowing a position is going is not
permission to stand it down while a pre-cutover failure must still leave it serving. A retained
member whose peer refuses withdrawal after cutover is a cleanup failure, named rather than swallowed,
and the successor stays released because the scope has already cut over.

Property: no retained member is retired in any outcome in which cutover did not occur, every retained
member is retired in every outcome in which it did, and no outcome reports both generations serving.

## C9 — a membership change produces an activation the other slices accept

A successful change returns the successor in the form CBI13 produces, over its own member set. CBI14
can revalidate it, CBI15 can revise it, CBI18 can extend it, CBI19 can replace it, and a further
CBI20 call can change its membership again.

Property: the result of a membership change is accepted by CBI14 revalidation, and revalidating it
immediately with the same requests continues it.

## C10 — both composition roots implement independently, and evidence remains bounded

Reference Studio and Minimal Host own separate implementations over their native CM2, CM4, CM5, and
PB7 types. CBI20 is additive: CBI12 through CBI18 are unchanged, and CBI19 changes only by refusing
the member-set change it never enforced.

CBI20 proves fail-closed addition and removal of positions across a scoped replacement, over
protocol-free members. It does not add or remove a member without a cutover, migrate state between a
dropped member and an added one, attach a child Port, perform Relational Initialisation, mediate,
widen a Provider Set, or provide production identity, policy, distribution, or security. It models no
grant that outlives the activation attempt it was admitted in, so a receiving domain that persists
grants beyond an attempt would need a withdrawal step this slice does not supply.

Property: deleting either CBI20 implementation leaves native CM2, CM4, CM5, CBI1-CBI19, and Portable
Binding behavior unchanged, and every CBI20 status statement preserves these limits.
