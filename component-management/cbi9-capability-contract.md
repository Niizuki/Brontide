# CBI9 declared grant dependency and participant revision capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI9 supplies the prerequisite CBI7 and CBI8 both stopped at — a statement of which grants the
member's ordinary interaction depends on — and then does what neither could: it removes and
substitutes participants of a live set without retiring the member.

The declaration is not the caller's opinion. Its names come from the resolved Component definition's
requested authority, which CM2 already carries into the generation, so the Component says what it
depends on and the caller only supplies the explicit typed mapping from each declared name to the
CM5 Capability, target Actor, Operation, and scope that satisfies it. A revision is admitted when
every declared dependency is still covered by some participant of the intended set.

This revises CBI8's reasoning rather than contradicting it. CBI8 refused substitution because a
substitute holding the identical tuple is a different grant — its holder differs — and nothing said
whether the member depended on that holder. A declaration answers that: it names tuples, not
holders, so a substitute that satisfies the same declared dependency is enough. Where there is no
declaration, CBI8's growth-only rule remains the safe one.

Participant precedence is still never decided. Coverage decides which participants may leave, so no
participant has to be ranked above another.

## C1 — the declaration comes from the resolution, not from the request

The declared names must equal, exactly, the requested authority the completed CM2 generation records
for the CBI1-selected definition. The caller adds one mapping entry per declared name, and the
mapped tuples are pairwise distinct. A declaration that renames, drops, or invents a dependency is
refused before anything is evaluated.

Property: no revision proceeds on a declaration whose names differ from the generation's record for
the selected definition.

## C2 — an empty declaration is not a licence to shrink

A definition that requests no authority states nothing about what its interaction depends on, which
is not the same as stating that it depends on nothing that was admitted. CBI9 refuses to revise
against an empty declaration; growth remains available through CBI8 and retirement through CBI7.

Property: no set is ever reduced under a declaration with no entries.

## C3 — the set in force must already satisfy the declaration

Before any revision is considered, every declared tuple must be held by some grant of the set
currently in force. A declaration cannot be introduced to bless a set that never covered it, and a
mapping the caller aimed at a tuple nobody holds is caught here rather than becoming a dependency
that silently constrains nothing.

Property: a declaration that the current set does not cover produces no evaluation and no change.

## C4 — the intended set is admitted only if every declared dependency stays covered

The intended set may drop participants, add participants, or both. It is admitted when each declared
tuple is held by at least one grant of the intended set. The holder may differ from the one that
satisfied it before; the declaration names tuples, so a substitute satisfies it.

Property: every revised set covers every declared tuple, and no revision leaves a declared tuple
uncovered.

## C5 — the intended set is still a set, and still non-empty

Participants are pairwise distinct, at least one remains, and an intended set identical to the
current one is refused because revalidating the current set is CBI7's decision. Admission,
relationship, and authority request identities stay pairwise distinct across the whole intended set,
and the local Actor established for each participant differs from every other's.

Property: no revised set is empty, repeats a participant, repeats an identity, or maps two
participants onto one receiving-domain Actor.

## C6 — retained participants are revalidated, added participants are admitted

Every request in the intended set is evaluated by the native evaluator in a deterministic order, all
or none. A participant the set already had must reproduce its established relationship and grants
exactly; a participant being added must be admitted exactly as CBI6 requires. A participant being
dropped is not evaluated, because after the revision it holds nothing.

Property: a result carries either no CM5 observation at all or exactly one per intended participant.

## C7 — a malformed request decides nothing, and evaluated loss decides everything

As in CBI8: a retained request that does not re-identify its authority is declined with the binding
untouched, because nothing was evaluated and nothing was learned. A retained participant whose fresh
outcome no longer reproduces the identical relationship and grants is positive evidence of loss and
retires the member, whatever the revision was trying to do.

Property: no result both retires the member and reports zero evaluations.

## C8 — a declined revision changes nothing

Refusing to revise is not a failure of the binding. Every declined outcome — declaration mismatch,
empty declaration, unsatisfied declaration, uncovered dependency, structural problem, malformed
retained request, refused addition, or Actor conflict — leaves the member released with the set it
already had, reported as the one still in force.

Property: a set is in force exactly while the member is released.

## C9 — both composition roots implement independently

Reference Studio and Minimal Host own separate revisers over their native CM2, CM5, and PB7 types.
CBI9 is additive: CBI6 admission, CBI7 revalidation, and CBI8 extension are unchanged, and shared
material is limited to this contract and the data-only scenario inventory.

Property: deleting either CBI9 reviser leaves native CM2, CM5, CBI1-CBI8, and Portable Binding
behavior unchanged.

## C10 — evidence remains bounded

CBI9 proves fail-closed revision of one participant set, under one declaration derived from one
resolved definition, over one released singleton binding. It does not verify that the Component's
declared authority is truthful or complete — [CBI10](./cbi10-capability-contract.md) separately
checks it against observed interaction, and only in the direction use can contradict. CBI9 itself
does not exercise any granted Operation, notify the provider that the set changed, transfer state
between a departing and an arriving participant, revoke a departing participant's authority anywhere
outside this set, order participants by priority, or provide production identity, policy,
distribution, or security.

Property: every CBI9 status statement preserves these limits.
