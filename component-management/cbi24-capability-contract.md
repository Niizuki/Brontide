# CBI24 replacing a generation that offers occupied Ports capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI24 replaces the generation occupying one restart scope when child activations are attached to
Ports that generation offers. CBI19 and CBI20 replace a generation; CBI22 attaches a Component to a
Port *of a generation*; nothing until now said what the first does to the second.

**CM4's answer is that it does nothing, by design.** Its C2 property is that every outcome preserves
the generation and activity state of every *unrelated* scope, and a child scope is unrelated: on
cutover the runtime rewrites the target scope's generation and carries every other scope through
untouched. So the child keeps running, at its own generation, in its own scope — while the
`ParentGeneration` its attachment recorded is no longer active anywhere. **A replacement silently
orphans every attachment beneath the generation it replaces**, and nothing ever looks again: the
attachment was validated once, at attach time.

**There is no migration operation, and that is the second finding.** Re-pointing an attachment at the
successor would need CM4 to hold the declaration as mutable state, and it holds it as an input to one
activation attempt. Moving a Component from a retired generation's Port to its successor's is
therefore not one operation but the child's own activation against the successor — which cannot run
before the successor exists. A Port does not migrate; a child is stood down and stood up again.

## C1 — the operation takes the generation and the attachments beneath it together

The input is CBI19's or CBI20's, plus the attached activations. The set is a **forest beneath the
retained generation, not a flat list of its direct children**: an attachment that must go takes
everything beneath it, and CBI23 already knows how to order the whole of it. So each supplied
activation must be released and attached either to the generation being replaced or to another scope
in the set. One that names a different parent generation, one whose own parent the caller left out,
and one that is not an attachment at all are each refused before anything is retired or established.

The forest rule is not a generalisation for its own sake — the first draft required every supplied
attachment to name the retained generation directly, and the two-level vector failed against it,
because a grandchild is beneath the generation being replaced without being attached to it.

Property: every refusal before the cascade leaves every attachment and every retained member
released, and creates no successor member.

## C2 — the attachments are stood down before the cutover, not after

The cascade runs first, deepest-first as CBI23 orders it, and only then is the generation replaced.
Doing it the other way would leave every child attached to a generation that no longer exists for the
width of the replacement, which is the state this slice exists to prevent — and CM4 would report
nothing wrong, because the child's scope is one it preserves.

This is the opposite order from CBI19's retained members, which are retired *after* cutover, and the
difference is which side of the boundary the thing being stood down lives on. A retained member is
inside the transaction and must keep serving until it succeeds; an attachment is outside it, in a
scope CM4 will not touch either way.

Property: no successor member is established while any supplied attachment is still released.

## C3 — a failed replacement does not restore the attachments

If the replacement fails after the cascade, the retained generation keeps serving, as CBI19
guarantees, but the attachments are already gone. They are not restored, because restoring one would
be a fresh activation against a generation this call did not establish, and reporting it as a
restoration would claim a continuity the runtime does not model.

Property: every outcome names every scope the cascade retired, whether the replacement that followed
succeeded or not.

## C4 — a child re-attaches to the successor as an ordinary attachment

After the replacement, standing the child up again is CBI22's attach naming the successor generation.
It is not part of this operation, because the successor's Ports are the successor's own statement and
which of them the caller wants occupied is a decision this slice has no input for.

Property: an attachment naming the successor generation is admitted on CBI22's terms once the
replacement has cut over, and one naming the retained generation is refused by CBI22's own
parent-generation check.

## C5 — a caller that does not present its attachments is not detected

CBI19 and CBI20 remain reachable with children still attached, and neither they nor CM4 can see it: a
replacement's inputs carry no record of what is attached beneath the retained generation, and the
runtime keeps none. This slice adds a path that does the right thing when told, and cannot add one
that notices when not told.

Property: every CBI24 outcome names exactly the attachments it was given, so an attachment the caller
omitted is visible by absence rather than assumed away.

## C6 — both composition roots implement independently, and evidence remains bounded

Reference Studio and Minimal Host own separate paths, delegating the cascade to their own CBI23
withdrawal and the replacement to their own CBI19 or CBI20 one. CBI24 is additive: CBI13 through
CBI23 are unchanged.

CBI24 proves fail-closed replacement of a generation with attachments beneath it, over protocol-free
members. It does not migrate a Component between Ports without standing it down, re-attach a child on
the caller's behalf, detect an attachment it was not given, perform Relational Initialisation,
mediate, widen a Provider Set, or provide production identity, policy, distribution, or security.

Property: deleting either path leaves native CM2, CM3, CM4, CM5, and Portable Binding behavior
unchanged, and every CBI24 status statement preserves these limits.
