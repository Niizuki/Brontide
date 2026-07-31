# CBI19 scoped activation replacement capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI19 replaces the generation occupying one restart scope with a successor generation, standing up
the successor's members and cutting the scope over to them. CBI14, CBI15, and CBI18 each deferred to
scoped replacement by name; the first thing this slice establishes is that they were deferring to
something CM4 does not have.

**Scoped replacement swaps a whole generation, not a member.** CM4 targets one restart scope holding
one retained generation, and a successful Release makes the new generation active there **atomically**
— one Release per attempt, one cutover for the scope. There is no operation anywhere in CM4 that
retires one member while its scope keeps running. So "retire the whole activation" was never a
stand-in for this: it was already the correct answer, and the forward references named a capability
the runtime does not model.

**Authority follows the occurrence, not the activation attempt.** CBI13 admits against an occurrence
"because an occurrence is durable where an activation attempt is not", and a replacement is precisely
the event that ends an attempt while occurrences may survive it. That justification is finally
exercised here rather than merely asserted.

## C1 — replacement needs a released activation and a successor generation for the same scope

The input is a released CBI13 activation, a completed successor generation, one entry per successor
member, and a runtime request whose restart scope is the one the retained activation occupies and
whose retained generation is the one it made active. The successor's generation identity must differ
from the retained one; a request naming a different scope, the same generation identity, or a
retained generation the scope does not hold is refused before anything is established.

Property: every refusal before establishment leaves every retained member released and creates no
successor member.

## C2 — authority is re-established, never inherited, and it follows the occurrence

No grant carries across a replacement. Every successor member is admitted on CBI13's terms before any
successor provider is contacted, and which rule applies is decided by its occurrence:

- an occurrence the retained activation already holds must be admitted with a request that
  re-identifies the authority that admitted it, so a replacement cannot silently change what a
  surviving occurrence is authorised for; and
- an occurrence the retained activation does not hold is a new member, admitted exactly as CBI13
  admits any participant set.

Property: no successor member is released without its own admission in this attempt, and no surviving
occurrence's authority changes across a replacement.

## C3 — the successor stands up under CBI13's barriers, unchanged

The successor activation is a CBI13 activation: every member's set is admitted first, every member is
then established, and the activation-wide identity and receiving-domain Actor rules hold across the
successor's members. A refusal at any of those points is a refusal of the replacement.

Property: an admission refusal in the successor contacts no successor provider, and leaves the
retained activation released.

## C4 — the release barrier re-arms for the whole successor activation

Every successor member must reach Ready before the single Release. The barrier is CBI12's, unchanged:
ordinary interaction opens for every successor member at once or for none, because CM4 models one
Release for the attempt and one cutover for the scope. It does not arm for a replaced member alone,
and there is no partial cutover.

Property: after every replacement outcome, either every successor member is released or none is.

## C5 — cutover is the boundary, and before it the retained activation is untouched

Failure before cutover — preparation, admission, establishment, a member that never reports Ready, or
a Release that fails before cutover — discards the successor and leaves the retained generation
active with every retained member still released and still able to interact. The retained activation
is not a fallback that has to be restored; it was never stood down.

Property: no failure before cutover retires, closes the gate of, or withdraws any retained member.

## C6 — the retained members are retired after cutover, never before

Once the scope has cut over, the retained members are retired, gate first, as CBI14 retires. Doing it
earlier would stand down the activation that a pre-cutover failure is required to leave serving.

Property: no retained member is retired in any outcome in which cutover did not occur, and every
retained member is retired in every outcome in which it did.

## C7 — a failure after cutover is CM4's to classify, and cleanup failure stays visible

A Release that fails after cutover follows CM4's declared rollback availability and is reported as
CM4 classifies it. Separately, a retained member whose peer refuses withdrawal after a successful
cutover is a cleanup failure: the successor stays released, because the scope has already cut over,
and the failure is named rather than swallowed.

Property: no outcome reports both generations serving, and no cleanup failure silently restores a
retained member to released.

## C8 — a replacement produces an activation the other slices accept

A successful replacement returns the successor in the form CBI13 produces: every member's
observations and grants, and the same released members. CBI14 can revalidate it, CBI15 can revise it,
CBI18 can extend it, and a further CBI19 call can replace it again.

Property: the result of a replacement is accepted by CBI14 revalidation, and revalidating it
immediately with the same requests continues it.

## C9 — both composition roots implement independently

Reference Studio and Minimal Host own separate replacers over their native CM2, CM4, CM5, and PB7
types. CBI19 is additive: CBI12 through CBI18 are unchanged.

Property: deleting either CBI19 replacer leaves native CM2, CM4, CM5, CBI1-CBI18, and Portable
Binding behavior unchanged.

## C10 — evidence remains bounded

CBI19 proves fail-closed replacement of the generation occupying one restart scope by a successor
generation, over protocol-free members. It does not retire or replace one member while its scope keeps
running, because CM4 models no such operation; it does not add or remove a position, attach a child
Port, perform Relational Initialisation, mediate, widen a Provider Set, migrate state between the
retained and successor members, or provide production identity, policy, distribution, or security.

Property: every CBI19 status statement preserves these limits.
