# Channel 0.2 state/event coverage 0.1

Date: 2026-08-11

Status: proposed first-batch design artifact; awaiting a fresh independent
closure re-review, on hold under the owner decision of 2026-08-17 recorded in the
[verification foundation plan](./Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md).
Correction history is not carried here; it is owned by the
[disposition index](./reviews/channel-0.2-disposition-index.md#stateevent-coverage-grid).

Normative companions:

- [capability contract](./Brontide-Channel-0.2-Capability-Contract-0.1.md);
- [session state machine](./Brontide-Channel-0.2-Session-State-Machine-0.1.md); and
- [interaction state machine](./Brontide-Channel-0.2-Interaction-State-Machine-0.1.md).

## Purpose

The state-machine transition tables remain the detailed authority. This grid proves their event
domain is closed: Every recognized event/state pair has exactly one route to a specific transition,
a named catch-all, or the terminal late-traffic rule. It does not add a permissive default.

## Closed-world totality rule

For each local endpoint:

1. a matching detailed transition row wins;
2. otherwise, a recognized peer event in a nonterminal state is a scope-appropriate
   `state-violation` and follows the session or interaction peer-fault route;
3. otherwise, a wrong-state local action that emitted no frame is a frameless local refusal and
   preserves the current state;
4. a local session/transport loss uses the named local-loss route;
5. terminal input uses the late-traffic rule and never reopens a terminal history; and
6. unknown or structurally invalid input uses the structural peer-fault row at the smallest scope
   whose identity and frame are valid.

An implementation cannot ignore a recognized event, select between “unchanged” and “faulted”, or
invent an extra state. The generated model suite enumerates every cell below and requires exactly
one route.

## Session coverage grid

| Session state | Establishment control | Interaction request | Drain | Close | Peer fault / invalid control | Local loss |
| --- | --- | --- | --- | --- | --- | --- |
| `unestablished` | fixed validation or proposal path | `state-violation` | `state-violation` | `state-violation` | `faulted` when a peer frame is attributable | `faulted` |
| `establishing` | exact acceptance/refusal; any second or mismatched control faults | `state-violation` | `state-violation` | `state-violation` | `faulted` | `faulted` |
| `established` | mutation/second establishment faults | interaction machine | first drain → `draining` | premature close → `faulted` | `faulted` | `faulted` |
| `draining` | `state-violation` | local refusal or named peer-interaction rule | duplicate drain → `faulted` | empty set → `closed`; otherwise `faulted` | `faulted` unless the named peer-interaction row is nonfatal | `faulted` |
| `closed` | terminal late input | terminal late input | terminal late input | terminal late input | terminal late input | remains `closed`; local observation only |
| `faulted` | terminal late input | terminal late input | terminal late input | terminal late input | terminal late input | remains `faulted`; local observation only |

## Initiator interaction coverage grid

| Initiator state group | Local request/cancel action | Cancellation acknowledgement | Semantic terminal | Peer fault | Local loss | Other peer control |
| --- | --- | --- | --- | --- | --- | --- |
| `candidate` / `admitting` | admission rows; wrong-state local cancel refuses | unsolicited peer event → `peer-fault` | unsolicited → `peer-fault` | `peer-fault` with `known-none` | `lost` with `known-none`; loss selects `lost` in any nonterminal state, pre-dispatch included | `peer-fault` |
| `dispatched` | exactly one cancel commit → `cancel-pending` | unsolicited → `peer-fault` | success/failure accepted; cancelled → `peer-fault` | `peer-fault` | `lost` | `state-violation` → `peer-fault` |
| `cancel-pending` | second local cancel refuses without a frame | first accepted/refused selects distinct state | declared race terminal accepted | `peer-fault` | `lost` | `state-violation` → `peer-fault` |
| `cancel-accepted` | further local cancel refuses | any later acknowledgement → `peer-fault` | success/failure/cancelled accepted | `peer-fault` | `lost` | `state-violation` → `peer-fault` |
| `cancel-refused` | further local cancel refuses | any later acknowledgement → `peer-fault` | success/failure accepted; cancelled → `peer-fault` | `peer-fault` | `lost` | `state-violation` → `peer-fault` |
| any terminal | local terminal-session refusal | late-traffic latch | late-traffic latch | local record; no reply loop | local observation; terminal preserved | late-traffic latch |

## Recipient interaction coverage grid

| Recipient state group | Request | Cancellation control | Handler terminal | Local protocol failure | Local loss | Other peer event |
| --- | --- | --- | --- | --- | --- | --- |
| `unseen` | validation rows | no identity to correlate → state unchanged, recorded with `rejected-protocol` provenance, detailed reason `unopened-interaction-identity`, the **refused-frame reference** — its kind, its **session**, its interaction identity, its **committing endpoint**, and its **arrival ordinal** for that interaction identity, the kind here being `cancellation-control` — and effect certainty `known-none` | impossible local action | structural/local-refusal split | local session route | state unchanged, recorded with `rejected-protocol` provenance, detailed reason `unopened-interaction-identity`, the **refused-frame reference** — its kind, its **session**, its interaction identity, its **committing endpoint**, and its **arrival ordinal** for that interaction identity, the kind here being the refused control's own — and effect certainty `known-none` |
| `validating` | validation rows | valid control: hold exactly one, apply on admission; second control → `peer-fault` | impossible local action | structural/local-refusal split | local session route | `rejected-protocol` |
| `executing` | live replay → `peer-fault` | authorized → `cancel-requested`; denied → `cancel-refused`; invalid → `peer-fault` | success/failure accepted; cancelled → `internal-channel-failure` → `peer-fault` | committed fault → `peer-fault` | `lost` | `state-violation` → `peer-fault` |
| `cancel-requested` | live replay → `peer-fault` | any further control → `peer-fault` | success/failure/cancelled accepted | committed fault → `peer-fault` | `lost` | `state-violation` → `peer-fault` |
| `cancel-refused` | live replay → `peer-fault` | any further control → `peer-fault` | success/failure accepted; cancelled → `internal-channel-failure` → `peer-fault` | committed fault → `peer-fault` | `lost` | `state-violation` → `peer-fault` |
| any terminal | late-traffic latch | late-traffic latch | late-traffic latch | terminal preserved | local observation; terminal preserved | local record; no reply loop |

The `unseen` row names a provenance where every other row names a next state, and says so in the cell
rather than leaving one token to mean two things in two artifacts. Nothing is retained there, so there
is no state to move to: `rejected-protocol` is what the refusal is recorded under, and the recipient's
per-identity state is still `unseen` when the next frame arrives. At `validating` the same token does
name a state, because an interaction exists to be in it.

`unseen` and `validating` are separate rows because a cancellation control means different things in
each. At `validating` the identity is known and the interaction exists, so the control correlates and
is held: the initiator sent it legally from `dispatched` and cannot observe when the recipient reaches
`executing`, so faulting it would condemn a conformant endpoint for losing an unobservable race. At
`unseen` there is no accepted identity to correlate against, and holding state for one would let a
peer allocate unbounded local state by naming identities it never opens, so the control is refused as
a peer statement. That verdict is sound only because a conformant control cannot arrive at `unseen`
at all: **C4 owns** the rule that within one session, for one interaction identity, frames sent by one
endpoint are delivered in the order that endpoint committed them, a realization profile declares
per-interaction frame order, and `C4-P2` is the property that fails when it does not hold. This grid
carries that fact and does not own it. Cross-interaction and cross-session ordering remain unpromised
under C4, and a delivery facet may add guarantees beyond the intra-interaction one but may not weaken
it.

That refusal **retains no history and no latch**. One interaction-scoped peer fault is committed and
nothing is kept, because a retained terminal record for an identity the peer never opened is the same
unbounded local state the refusal exists to avoid — the `any terminal` row of this grid therefore does
not reach it, and a later request bearing that identity re-enters at `unseen` like any other first
request. The interaction machine carries the event as a detailed recipient transition row, so totality
rule 1 selects it; without that row rule 2 would claim it and produce the terminal `peer-fault` this
paragraph refuses.

One local observation is recorded there all the same, and this cell asserts it like any other —
including the detailed reason `unopened-interaction-identity` and the **refused-frame reference** —
its kind, its **session**, its interaction identity, its **committing endpoint**, and its **arrival
ordinal** for that interaction identity — because
both `unseen` cells otherwise record one indistinguishable refusal while `C4-P2`'s first conjunct
quantifies over the cancellation control alone.
Recording evidence is not retaining state: nothing consults the observation, so it cannot accrue into
the per-identity state the refusal exists to avoid, and it is the record `C4-P2`'s first conjunct
quantifies over. The cell's late-traffic latch assertion is the explicit value `not-applicable`,
because the route reaches no terminal interaction and there is no latch to be `clear`.

The kind alone was AC2's correction, and the session, the interaction identity, the committing
endpoint and the arrival ordinal are **AK1** and **AK5**. That conjunct scopes
its membership test to one **session**, tests membership of one **interaction identity**, and takes
its precedence half over the **committing endpoint's** own frames, and the cell published none of the
three — so the property was red on a two-session vector conforming at both endpoints, which is AE1's
failure mode reached through the operand. The **arrival ordinal** is Y4's argument on this operand: one
endpoint may commit two controls naming one identity, and without it a control committed before the
request binds to one committed after it. This grid publishes the reference in the same form as the
interaction machine's `unseen` transition row, the brief's local-observation schema and parity profile,
the responsibility matrix row that owns the observation record, and the migration ledger's
new-evidence inventory.

A held control is covered by totality rule 1 — a matching detailed transition row wins — rather than
by the `state-violation` catch-all, and it never reaches the late-traffic latch: if admission refuses,
the held control is discarded with no answering frame, because a control that was legal when sent does
not become late traffic when the request it named is refused.

## Late-traffic latch

Each terminal interaction has one `late-traffic-fault` latch: `clear`, `fault-committed`, or
`fault-unavailable`. The first duplicate semantic terminal or late non-fault control at `clear`
preserves the first terminal and attempts exactly one interaction-scoped `state-violation` peer
fault. A late peer fault receives no answer. After the latch settles, every later input is recorded
locally without another frame. This makes the duplicate-terminal action finite and prevents a fault
loop.

Settling the latch records the frame that settled it — its kind, its **session**, its interaction
identity, its **committing endpoint**, and its **arrival ordinal** within the interaction — because the
three latch values name no frame and `C4-P2`'s second conjunct is about which frame a latch settled
against. The **session** is required because an interaction identity is unique only within one and a
vector may carry more than one: without it the reference matches a step in either session and stops
mapping to one declared stimulus step, which takes `C4-P2` green on `C4-outcome-precedes-ack`. The
ordinal is required for the reason kind, session, interaction identity, and committing endpoint are
together insufficient: one endpoint may commit two frames of the same kind for one identity, which is
what a duplicate terminal is, and that case must leave the property green. It identifies the frame and
is never an ordering operand. This grid publishes the reference in the same form as the interaction
machine that owns the latch, the brief's local-observation schema and parity profile, the
responsibility matrix row that owns the observation record, and the migration ledger's new-evidence
inventory. A field carried by some of those six surfaces and not the others makes the owned fact and
the compared fact different facts.

Accepting a terminal history records the **terminal-frame reference** — its kind, its **session**, its
interaction identity, its **committing endpoint**, and its **arrival ordinal** within the interaction.
That is the frame the settling frame is compared *against*, and until **AK6** no artifact published
it: the observation carried the terminal form, and a form names one frame only while an endpoint
commits at most one frame of that form for one identity. A duplicate terminal is an endpoint
committing two, and it is a case `C4-P2` must leave green. The reference is published across the same
six surfaces and for the same reason, and it identifies rather than orders.

A route that reaches no terminal interaction has no latch, and its cells assert the explicit value
`not-applicable` rather than leaving the field absent. The `unseen` refusal is the only such route
today. An absent required field is the silence two independent implementations agree on by accident:
one would write `clear` and the other nothing, both defensibly, and every cross-stack comparison would
pass.

## Evidence required

- A generated session model enumerates all six states against every session event family.
- A generated initiator model enumerates every initiator state against every peer/local event family.
- A generated recipient model does the same independently.
- Each cell asserts next state, emitted frame or no-frame decision, provenance, effect certainty,
  dispatch delta, sibling delta, recorded local observation, and late-traffic latch — the last as
  `not-applicable` where the cell's route reaches no terminal interaction, never as an absent field.
- Mutating any detailed row or catch-all to “ignore”, changing provenance, or adding a second route
  must fail at least one property.

This is design evidence only. Batch 2 will translate the grid into neutral vector groups and model
properties; it does not authorize implementation before independent closure.
