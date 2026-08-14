# Channel 0.2 state/event coverage 0.1

Date: 2026-08-11

Status: proposed first-batch totality artifact; added after D1-D4, corrected for T3, R1, R3, S1, S2,
and U8, and subject to a fresh independent closure re-review. The intra-interaction ordering fact the
`unseen` verdict depends on is carried here and owned by C4. Under U8 the pre-dispatch Local loss cell
names `lost` like every other cell in that column, rather than leaving the state to be read out of the
interaction machine's totality rule.

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
| `unseen` | validation rows | no identity to correlate → `rejected-protocol` | impossible local action | structural/local-refusal split | local session route | `rejected-protocol` |
| `validating` | validation rows | valid control: hold exactly one, apply on admission; second control → `peer-fault` | impossible local action | structural/local-refusal split | local session route | `rejected-protocol` |
| `executing` | live replay → `peer-fault` | authorized → `cancel-requested`; denied → `cancel-refused`; invalid → `peer-fault` | success/failure accepted; cancelled → `internal-channel-failure` → `peer-fault` | committed fault → `peer-fault` | `lost` | `state-violation` → `peer-fault` |
| `cancel-requested` | live replay → `peer-fault` | any further control → `peer-fault` | success/failure/cancelled accepted | committed fault → `peer-fault` | `lost` | `state-violation` → `peer-fault` |
| `cancel-refused` | live replay → `peer-fault` | any further control → `peer-fault` | success/failure accepted; cancelled → `internal-channel-failure` → `peer-fault` | committed fault → `peer-fault` | `lost` | `state-violation` → `peer-fault` |
| any terminal | late-traffic latch | late-traffic latch | late-traffic latch | terminal preserved | local observation; terminal preserved | local record; no reply loop |

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

## Evidence required

- A generated session model enumerates all six states against every session event family.
- A generated initiator model enumerates every initiator state against every peer/local event family.
- A generated recipient model does the same independently.
- Each cell asserts next state, emitted frame or no-frame decision, provenance, effect certainty,
  dispatch delta, sibling delta, and late-traffic latch.
- Mutating any detailed row or catch-all to “ignore”, changing provenance, or adding a second route
  must fail at least one property.

This is design evidence only. Batch 2 will translate the grid into neutral vector groups and model
properties; it does not authorize implementation before independent closure.
