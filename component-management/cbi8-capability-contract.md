# CBI8 in-place participant extension capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI8 changes an admitted CBI6 participant set while its member stays released. The caller declares
the complete intended set; CBI8 admits it only when the intended set retains every current
participant and adds at least one more.

**Removal and substitution in place are refused.** They are what "replacement" would need, and they
are exactly what cannot be decided here: nothing in an admitted set says which participants the
member's ordinary interaction depends on, so dropping one — even while adding a replacement holding
the identical tuples — would remove authority the member may rely on, invisibly. A grant's holder is
part of what makes it that grant, so a substitute holding the same Capability, target Actor,
Operation, and scope is not the same grant. Addition has no such problem: authority only grows, and
nothing the member already relies on is withdrawn. Removal and substitution therefore go through
CBI7 retirement and a fresh CBI6 admission, and stay future work until a member can declare which
grants it depends on.

## C1 — only an admitted, released set can be extended

The input is a complete successful CBI6 result — every participant admitted exactly, aggregate
grants present, one released Active member — and the complete intended participant set as one fresh
CM5 request each. Mappings are not resupplied: the member and its occurrence are already fixed, so
re-stating them could only introduce drift.

Property: every unavailable input produces no CM5 evaluation, no lifecycle effect, and no extended
set.

## C2 — the intended set retains everyone and adds someone

Every current participant appears in the intended set, at least one participant is new, and no
participant appears twice. An intended set that drops or substitutes a participant is declined, and
so is one identical to the current set, which would be a revalidation rather than an extension.

Property: no intended set that removes, substitutes, repeats, or merely repeats-in-full the current
participants changes anything about the member or its authority.

## C3 — a declined extension changes nothing

Declining is not a failure of the binding. When CBI8 refuses to extend — because the intended set is
not a valid extension, an added request is malformed, a retained request does not re-identify its
authority, an added participant is not admitted, or the resulting set would share identities or a
receiving-domain Actor — the member stays released with the authority it already had, and the result
carries the unchanged set as the one still in force.

Property: every declined result leaves the member released and reports an in-force set exactly equal
to the one CBI6 admitted.

## C4 — a malformed request decides nothing, and evaluated loss decides everything

The two ways CBI8 can meet a problem with a *retained* participant are not the same. A request that
does not re-identify that participant's relationship and grants is declined: nothing was evaluated,
so nothing was learned, and the member's release still rests on the last admission that did hold.
An evaluated outcome that no longer reproduces the identical relationship and grants is positive
evidence of loss, and retires the member exactly as CBI7 would.

Property: no result both retires the member and reports zero evaluations.

## C5 — retained authority is revalidated before it is extended

Once the intended set is structurally valid, every request in it — retained and added alike — is
evaluated by the native evaluator in a deterministic order, all or none. A set is never extended on
top of authority that has itself lapsed, and a lapse outranks any problem with an addition, so a
call that would both retire and decline retires.

Property: a result carries either no CM5 observation at all or exactly one per intended participant,
and no extended result exists where a retained participant failed to renew.

## C6 — an added participant is admitted on CBI6's terms

Each added request carries one `ComponentParticipant` relationship proposed by its own participant
and one or more non-unlimited authority requests dependent on it, with distinct tuples. The
evaluator must admit it exactly: one established relationship and one grant per authority request.

Property: an added participant that CBI6 would refuse admission is refused here too, and its refusal
declines the extension rather than retiring the member.

## C7 — the extended set obeys the whole-set rules, including against the participants already there

Admission, relationship, and authority request identities stay pairwise distinct across the complete
extended set, and the local Actor established for an added participant differs from every other
participant's, including the ones already admitted. An addition is a new opportunity for exactly the
collisions CBI6 refuses, now against a set that is already live.

Property: no extended set contains a repeated identity or two participants sharing one
receiving-domain Actor.

## C8 — an extension produces a set the other slices accept

A successful extension returns the complete admitted set in the same form CBI6 produces: every
participant's current observation, the aggregate grants, and the same released member. CBI7 can
revalidate that result, and a further CBI8 call can extend it.

Property: the result of an extension is accepted by CBI7 revalidation, and revalidating it
immediately with the same requests continues it.

## C9 — both composition roots implement independently

Reference Studio and Minimal Host own separate extenders over their native CM5 and PB7 types. CBI8
is additive: CBI6 admission and CBI7 revalidation are unchanged, and shared material is limited to
this contract and the data-only scenario inventory.

Property: deleting either CBI8 extender leaves native CM5, CBI1-CBI7, and Portable Binding behavior
unchanged.

## C10 — evidence remains bounded

CBI8 proves fail-closed in-place growth of one participant set over one released singleton binding.
Removal and substitution in place are covered separately by
[CBI9](./cbi9-capability-contract.md), under a dependency the resolved Component definition
declares; CBI8's growth-only rule remains the safe one wherever no such declaration exists. CBI8
itself does not order participants by priority, let a participant declare itself required, exercise
any granted Operation, notify the provider that the set changed, or provide production identity,
policy, distribution, or security.

Property: every CBI8 status statement preserves these limits.
