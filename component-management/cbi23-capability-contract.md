# CBI23 nested child-Port activation capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI23 nests child activations — a child may itself be the parent of another attachment — and then
answers what has to happen when a parent of such a chain is stood down.

**Nesting was already reachable, and CBI22 said so.** CBI22's completeness review records that a
child of a child "is not reached... nothing in this slice forbids it, but no vector exercises it and
the contract does not claim it". That was accurate: a child activation is an ordinary CBI13
activation, so passing it as the parent of a further attachment already worked. What this slice adds
is not the capability but the claim, its vectors, and the question the chain makes unavoidable.

**CM4 models no relationship between a parent and a child after attachment.** It requires the parent
scope to be active *at attach time* and preserves it through the child's activation, and that is all:
nothing in the runtime records that a scope has children, and nothing stands a child down when its
parent goes. So the ordering below is the composition root's to enforce, and the contract says which
part of it the root can and cannot see.

## C1 — a child activation is an ordinary parent

An attachment whose parent is itself a child is admitted on CBI22's terms, unchanged and applied at
each level: the declared parent scope and generation are the ones that parent made active, the
child's scope differs from its parent's, and the Port and its lifecycle come from the resolved
envelope. Nothing about a parent being a child relaxes or adds a rule.

Property: at every level, the same refusals apply and the parent at that level stays active,
released, and serving.

## C2 — depth is not bounded, and the reason is that no model bounds it

CM4 declares no depth, CM2 declares no nesting between Port envelopes, and a chain costs one CM4
attempt per level. A limit would therefore be a number this programme invented, which is what CBI11
refuses for elapsed time and interaction counts and for the same reason: a threshold nothing derives
is a threshold nobody can defend. What is bounded is the shape — the attachment relation is a finite
forest, and a cycle is refused.

Property: an attachment chain of any depth the caller builds is admitted on the same terms as the
first, and no outcome depends on how deep it is.

## C3 — the attachment relation is derived from CM4's own observations

Given a set of activations, the parent of each is read from its CM4 child declaration and its own
scope from its plan, so the relation is computed rather than declared. Two activations claiming one
restart scope are refused: which of them holds it is undecidable, and the depth of everything beneath
depends on the answer.

A cycle is reported rather than refused, because **no sequence of attachments can produce one**. Each
attachment requires a released activation as its parent and records that parent's scope, so a cycle
would need an activation to have existed before the one it is attached to. The ordering still has to
terminate on any input it is handed, and naming the cycle is how it says it cannot order the set —
not a refusal a caller can provoke. It is stated here rather than given a manufactured vector, as PB
states for `peer-unavailable`.

Property: every refusal of the relation itself retires nothing and leaves every activation released.

## C4 — a child is retired before the parent whose Port it occupies

Standing down a set of activations retires them deepest-first: an attachment occupies a Port of a
generation, so it cannot outlive the generation that offers the Port. CBI22's independence claim is
not contradicted, because it was one-directional — a child's activation does not disturb its parent;
this is the other direction, which only a chain makes askable.

Property: in every outcome, no activation is retired before an activation attached beneath it.

## C5 — the root can only order what it is given

A child the caller does not name is invisible: nothing in CM2 or CM4 records that a scope has
children, so a set that omits one retires its parent without knowing the child is there. The contract
states this rather than implying completeness, and the ordering guarantee of C4 is over the set
supplied, not over the world.

Property: every outcome names exactly the scopes it retired, so what was not ordered is visible by
absence rather than assumed.

## C6 — an attachment beneath a retired parent is refused

Once an activation is retired it is no longer a released CBI13 activation, so CBI22's own
precondition refuses an attachment naming it. A chain therefore cannot be re-extended beneath a level
that is gone, and no new rule is needed to say so.

Property: an attachment whose parent has been retired reaches no provider and creates no member.

## C7 — cleanup failure is named and restores nothing

A member whose peer refuses withdrawal during the cascade is a cleanup failure, reported against the
scope it happened in, and the cascade continues rather than stopping or rolling back. Restoring an
already-retired level would claim a state the runtime does not model, exactly as CBI19's post-cutover
cleanup failure does.

Property: no cleanup failure returns an activation to released, and every scope the cascade reached
is reported whether its cleanup succeeded or not.

## C8 — both composition roots implement independently, and evidence remains bounded

Reference Studio and Minimal Host own separate nesting and cascade paths over their native CM2, CM4,
CM5, and PB7 types, delegating each level's attachment to their own CBI22 path and each retirement to
their own CBI14 one. CBI23 is additive: CBI13 through CBI22 are unchanged.

CBI23 proves fail-closed nesting and ordered withdrawal of an attachment forest the caller supplies.
It does not discover children it was not given, migrate a Component between Ports, model traffic
between levels, perform Relational Initialisation, mediate, widen a Provider Set, or provide
production identity, policy, distribution, or security.

Property: deleting either path leaves native CM2, CM3, CM4, CM5, and Portable Binding behavior
unchanged, and every CBI23 status statement preserves these limits.
