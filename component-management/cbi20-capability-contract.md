# CBI20 membership replacement capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI20 replaces the generation occupying one restart scope with a successor generation that resolves a
**different set of positions**: it may resolve a position the retained activation does not hold, and it
may stop resolving one it does. That is the last structural constant every slice from CBI12 onward has
held fixed, and CBI19 named it as out of scope rather than deferring it.

**Pointing this slice at CBI19 found a defect in CBI19 first.** CBI19's C1 says the input is "one entry
per successor member" and its C10 says it does not add or remove a position, and its implementation
checked neither: a caller could hand it a membership that is a strict subset of the positions the
successor generation resolves and get a cutover to a generation whose CM3 plan covers fewer members
than CM2 resolved. Nothing downstream would notice, because the caller's list was the only statement of
membership anything read. Its vectors could not have caught it — every one of them builds the member
list, the participant sets, and the CM3 plan from the same declaration — which is the shape Decision 10
describes and the way PB7 found the provider-identity defect. CBI19 now refuses a membership the
successor generation does not resolve, and refuses a changed one, so its stated limit is checked rather
than assumed. **CBI20 is the door through which a membership may legitimately differ**, and everything
below is what passing through it requires.

## C1 — the membership is read from the successor generation

The preconditions are CBI19's, unchanged: a released CBI13 activation, a completed successor
generation, one entry per successor member, and a runtime request whose restart scope is the one the
retained activation occupies and whose retained generation is the one it made active, with a successor
generation identity that differs.

The supplied entries must be **exactly** the positions the successor generation resolves, compared as
requirement and occurrence pairs. A position the generation resolves with no supplied entry is refused;
a supplied entry naming a position the generation does not resolve is refused. Neither is a drop and
neither is an addition: both are a caller disagreeing with the generation about what the activation is,
and the first is how a silent drop would enter. CBI20 differs from CBI19 in permitting that membership
to differ from the retained activation's, not in trusting the caller about it.

Property: every refusal of the membership itself leaves every retained member released, creates no
successor member, and reports no membership change, because none was computed.

## C2 — the added, dropped, and surviving sets are derived, never declared

The caller states no intent about membership. Added is the successor's occurrences the retained
activation does not hold; dropped is the retained activation's occurrences the successor does not
resolve; surviving is the intersection. All three come from the successor generation and from CM4's own
observation of what the retained activation made active, which is where C1 gets its comparison too, and
which is why a declared membership change would add nothing but a second opinion to disagree with.

Property: in every outcome in which the membership was accepted, added and surviving together are
exactly the successor's membership, dropped and surviving together are exactly the retained
activation's, and nothing is both added and dropped.

## C3 — a dropped position's authority is not re-established, and nothing carries it forward

A dropped occurrence has no successor member, so there is nothing to admit it against, and CBI19
already settles what that means: authority follows the occurrence and is re-established in the attempt
rather than inherited, so a grant not re-established is not in force in the successor. No withdrawal or
revocation is performed against the receiving domain, because the admission is the composition root's
own record and the successor's record is what this attempt produced. A withdrawal step here would imply
the grant had been carried across, which it never is.

The departing member itself is retired with the rest of the retained generation after cutover, by
CBI19's rule rather than a new one. Re-admitting a dropped position later is another membership
replacement: no member remains for CBI9 to revise or CBI18 to grow.

Property: for every dropped occurrence, in every outcome, the successor holds no admission and no grant
naming an authority request that occurrence was admitted for.

## C4 — an added position joins only across a cutover

There is no path by which a member joins an activation that is already released, and the absence is the
runtime's rather than a preference. A CM2 generation is one immutable object resolving every position at
once, so a membership holding a position the active generation does not is a different generation; and a
CM4 attempt carries one plan covering every member, requires a stage outcome for each of them, and makes
its target generation active in one atomic cutover. Neither model can represent an additional member
arriving into a generation already serving.

This is also the line between CBI18 and CBI20. CBI18 grows the **participant sets** of members that
already exist, which changes no generation and needs no cutover; adding a **member** changes the
generation and therefore needs one.

Property: no added member is released in any outcome in which cutover did not occur.

## C5 — an emptied membership is a withdrawal, not a replacement

A successor generation that resolves no position is refused before anything is established. Standing an
activation down entirely is CBI14's withdrawal, which retires the members and names why; routing it
through a replacement would cut a scope over to a generation with no member to release, and CBI12's
release barrier is a barrier over a membership rather than over nothing.

Property: the refusal leaves every retained member released and retires none.

## C6 — the successor stands up under CBI13's and CBI19's barriers, over its own membership

The successor activation is a CBI13 activation over the successor's membership: every member's set is
admitted before any successor provider is contacted, the activation-wide identity and receiving-domain
Actor rules hold across the successor's members, every member reaches Ready before the single Release,
and a surviving occurrence must be admitted with the authority that admitted it. A changed membership is
a fresh opportunity to violate the activation-wide rules against a member that survives, and they are
checked over the successor because that is the activation they are a property of.

One consequence only a changed membership can reach: a receiving-domain Actor that a **dropped**
member's participant held may be taken by a different party in an **added** member, because the retained
activation's mapping ends with the retained activation and the successor's mapping is a function and
injective within itself. The same reuse against a **surviving** member's participant is the conflation
CBI13 refuses.

Property: every admission, identity, or Actor-mapping refusal in the successor contacts no successor
provider and leaves the retained activation released.

## C7 — cutover is still the boundary, and the retained membership goes as a whole

Failure before cutover leaves the retained generation active with every retained member still released
and still able to interact, including the members whose positions the successor drops. After cutover
every retained member is retired, gate first, dropped and surviving alike, because CM4 requires a
pre-cutover failure to leave the retained generation serving and models no way to stand one member down
while its scope keeps running. A retained member whose peer refuses withdrawal after a successful
cutover is a named cleanup failure that restores nothing.

Property: no retained member is retired in any outcome in which cutover did not occur, and every
retained member is retired in every outcome in which it did.

## C8 — a membership replacement produces an activation the other slices accept

A successful replacement returns the successor in the form CBI13 produces, over the successor's
membership: every member's observations and grants, and the same released members. An added member is an
ordinary member of it — CBI14 can revalidate it, CBI15 can revise it, CBI18 can extend it, and CBI19 or a
further CBI20 call can replace it again — and a dropped member is absent from all of them.

Property: the result of a replacement is accepted by CBI14 revalidation, which names exactly the
successor's membership and continues it.

## C9 — both composition roots implement independently

Reference Studio and Minimal Host own separate membership replacers over their native CM2, CM4, CM5, and
PB7 types, each delegating the cutover to its own CBI19 replacer rather than restating it. CBI20 is
additive: CBI12 through CBI18 are unchanged, and CBI19 changes only by enforcing the limit it already
declared.

Property: deleting either CBI20 replacer leaves native CM2, CM4, CM5, CBI1-CBI19, and Portable Binding
behavior unchanged.

## C10 — evidence remains bounded

CBI20 proves fail-closed replacement of the generation occupying one restart scope by a successor
generation resolving a different set of positions, over protocol-free members. It does not add or retire
one member while its scope keeps running, because CM4 models no such operation; it does not migrate
state to an added member or away from a dropped one, attach a child Port, perform Relational
Initialisation, mediate, widen a Provider Set, or provide production identity, policy, distribution, or
security.

Property: every CBI20 status statement preserves these limits.
