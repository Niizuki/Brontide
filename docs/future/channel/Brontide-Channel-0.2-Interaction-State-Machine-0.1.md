# Channel 0.2 interaction state machine 0.1

Date: 2026-08-11

Status: proposed first-batch design artifact; B1/B2, N2, F1/F2, D2/D3/D4, T3, R1, R2, S2, and W4
corrected after independent review and subject to a fresh independent closure re-review. `validating`
now carries loss and drain rows, the pre-dispatch loss rule is reconciled to any nonterminal state,
and under W4 an identity refused at `unseen` is not a terminal interaction and owns no latch.

Contract owners: [Channel 0.2 C3, C4, C7, C8, C9, and C10](./Brontide-Channel-0.2-Capability-Contract-0.1.md).

## Boundary

One interaction is a bounded unary exchange under one established session profile. It has one
interaction identity, one class, one initiator role, one recipient role, one Operation/input
contract, one authority decision, and at most one accepted terminal history.

“Unary” means one admitted input and one semantic terminal Outcome. Channel 0.2 may carry several
unary interactions concurrently up to the profile's finite bound. Streaming and long-running
activity require a declared extension facet and cannot reinterpret this state machine.

## Local initiator states

| State | Terminal | Meaning |
| --- | --- | --- |
| `candidate` | no | Local caller has proposed an interaction; no Channel admission is complete. |
| `admitting` | no | Class, phase, Shape, bounds, authority, concurrency, and replay checks are running. |
| `refused-local` | yes | Admission refused before dispatch; no request frame emitted and effects are `known-none`. |
| `dispatched` | no | A complete request was committed to the transport/direct seam; provider effects may be possible. |
| `cancel-pending` | no | A cancellation request was dispatched; the interaction still awaits a terminal fact. |
| `cancel-accepted` | no | Peer accepted the one cancellation request; the interaction still awaits a terminal fact. |
| `cancel-refused` | no | Peer refused the one cancellation request; ordinary execution still awaits success or failure. |
| `outcome-succeeded` | yes | One valid correlated semantic success was accepted. |
| `outcome-failed` | yes | One valid correlated semantic failure was accepted. |
| `outcome-cancelled` | yes | One valid correlated semantic cancelled Outcome was accepted. |
| `peer-fault` | yes | One valid correlated peer protocol fault was accepted. |
| `lost` | yes | No valid peer terminal fact is available and a local loss closed the interaction. |

## Local recipient states

| State | Terminal | Meaning |
| --- | --- | --- |
| `unseen` | no | No request with this identity has been accepted. |
| `validating` | no | Frame, profile, state, Shape, phase, authority, concurrency, and replay checks are running. |
| `refused-local` | yes | Local policy denied a structurally valid request before dispatch; no peer frame is emitted. |
| `rejected-protocol` | yes | Validation failed and a bounded peer protocol fault may be emitted; handler did not begin. |
| `executing` | no | Handler dispatch occurred. |
| `cancel-requested` | no | A valid cancellation request was received while execution remains nonterminal. |
| `cancel-refused` | no | Local cancellation authority refused the one request while ordinary execution remains nonterminal. |
| `outcome-succeeded` | yes | Handler produced semantic success and one Outcome was committed. |
| `outcome-failed` | yes | Handler produced shaped semantic failure and one Outcome was committed. |
| `outcome-cancelled` | yes | Handler completed through the supported cancellation contract and one Outcome was committed. |
| `peer-fault` | yes | One interaction-scoped peer protocol fault was committed; handler effects may already be possible. |
| `lost` | yes | Local session or transport loss prevented a valid terminal commit; no peer statement is claimed. |

The two endpoint histories need not end with the same local label when transport is lost after a peer
commits a terminal frame. Each reports only what it can establish. Portable parity compares facts
with the same provenance, not a fictional global state.

## Initiator transitions

| From | Event and guard | To | Effect certainty |
| --- | --- | --- | --- |
| `candidate` | begin admission under established, non-draining session | `admitting` | `known-none` |
| `admitting` | any class/phase/Shape/authority/bound/replay/concurrency refusal | `refused-local` | `known-none` |
| `admitting` | all checks pass and complete request commits to seam | `dispatched` | `unknown` until evidence narrows it |
| `dispatched` | valid correlated success | `outcome-succeeded` | profile-owned known details when supplied, otherwise `known` without fabricated count |
| `dispatched` | valid correlated semantic failure | `outcome-failed` | as reported by the profile; failure does not imply zero |
| `dispatched`, `cancel-pending`, `cancel-accepted`, or `cancel-refused` | valid correlated peer protocol fault | `peer-fault` | `known-none` only when fault explicitly proves handler did not begin; otherwise `unknown` |
| `dispatched` | valid cancellation request commits | `cancel-pending` | remains unchanged/unknown |
| `dispatched` | unsolicited cancellation acknowledgement | `peer-fault` | `unknown`; emit/record interaction-scoped `state-violation` |
| `cancel-pending` | cancellation `accepted` acknowledgement | `cancel-accepted` | acknowledgement is nonterminal and proves no effect fact |
| `cancel-pending` | cancellation `refused` acknowledgement | `cancel-refused` | acknowledgement is nonterminal and proves no effect fact |
| `cancel-accepted` or `cancel-refused` | any further cancellation acknowledgement | `peer-fault` | preserve possible effects; emit/record interaction-scoped `state-violation` |
| `cancel-pending` or `cancel-accepted` | valid correlated success/failure/cancelled Outcome | matching Outcome terminal | profile-owned evidence; acceptance is not retroactive rollback |
| `cancel-refused` | valid correlated success or failure Outcome | matching Outcome terminal | profile-owned evidence; cancellation refusal is not a terminal fact |
| `dispatched` or `cancel-refused` | correlated cancelled Outcome | `peer-fault` | `unknown`; cancelled contradicts a history with no cancellation request in force |
| `dispatched`, `cancel-pending`, `cancel-accepted`, or `cancel-refused` | timeout, interruption, peer close, or unusable terminal frame | `lost` | `unknown` unless explicit evidence proves otherwise |
| any terminal | first duplicate semantic terminal or late non-fault control while latch is `clear` | unchanged terminal | apply the `late-traffic-fault` latch; first accepted history remains authoritative |
| any terminal | peer fault, or any late traffic after the latch is settled | unchanged terminal | record locally; emit no answering frame |

## Recipient transitions

| From | Event and guard | To | Handler effect possible? |
| --- | --- | --- | --- |
| `unseen` | complete request for an established session arrives | `validating` | no |
| `validating` | structural/profile/state/class/direction/Shape/authority-structure/bound/replay/concurrency check fails | `rejected-protocol` | no |
| `validating` | receiver-local external phase predicate is `false` or `unknown` | `refused-local` | no |
| `validating` | structurally valid authority presentation is denied by local policy | `refused-local` | no |
| `validating` | valid cancellation control for this admitted identity arrives | `validating` | no; hold exactly one control and apply it when admission resolves |
| `validating` | any further cancellation control while one is held | `peer-fault` | no; emit one interaction-scoped `state-violation` |
| `validating` | all checks pass and dispatch boundary is crossed | `executing` | yes |
| `validating` | all checks pass, dispatch boundary is crossed, and one held cancellation control applies | `cancel-requested` or `cancel-refused` | yes; dispatch precedes the held control, which is then evaluated under local cancellation authority |
| `validating` | local session or transport loss, with or without a held cancellation control | `lost` | no; any held control is discarded with no answering frame and the late-traffic latch does not fire |
| `validating` | drain refuses this still-admitting interaction, with or without a held cancellation control | `refused-local` | no; an interaction whose admission has not resolved is outside the drain snapshot, and any held control is discarded with no answering frame |
| `executing`, `cancel-requested`, or `cancel-refused` | handler returns success | `outcome-succeeded` | yes/known by profile evidence |
| `executing`, `cancel-requested`, or `cancel-refused` | handler returns shaped failure | `outcome-failed` | possible; failure is not rollback |
| `executing` | valid cancellation control arrives | `cancel-requested` | possible/already occurred |
| `executing` | structurally valid cancellation control is denied by local cancellation authority | `cancel-refused` | possible/already occurred; emit nonterminal `refused` acknowledgement |
| `cancel-requested` or `cancel-refused` | any further cancellation control | `peer-fault` | possible/already occurred; no second handler signal, emit interaction-scoped `state-violation` |
| `executing`, `cancel-requested`, or `cancel-refused` | structurally invalid, unrecognized, unsupported, or wrongly scoped cancellation control | `peer-fault` | possible/already occurred; emit one interaction-scoped protocol fault and ignore a later handler terminal |
| `executing`, `cancel-requested`, or `cancel-refused` | repeated request with the same accepted identity | `peer-fault` | possible/already occurred; no redispatch, emit `replay-detected`, and ignore a later handler terminal |
| `cancel-requested` | handler reports cancellation completed | `outcome-cancelled` | possible; report exact evidence |
| `executing` or `cancel-refused` | handler reports cancellation completed with no cancellation request in force | `peer-fault` | possible; commit one interaction-scoped `internal-channel-failure` and record the discarded handler terminal |
| `executing`, `cancel-requested`, or `cancel-refused` | internal Channel failure and one scoped protocol fault commits | `peer-fault` | `unknown` unless handler boundary evidence narrows it |
| `executing`, `cancel-requested`, or `cancel-refused` | session/transport loss or internal failure prevents a valid terminal commit | `lost` | `unknown` unless handler boundary evidence narrows it |
| any terminal | first duplicate semantic terminal or late non-fault control while latch is `clear` | unchanged terminal | apply the `late-traffic-fault` latch; no redispatch or handler effect |
| any terminal | peer fault, or any late traffic after the latch is settled | unchanged terminal | record locally; emit no answering frame |

## Admission order

The local and peer admission pipelines use the same semantic order so a lower-level malformed value
cannot be reclassified by a later policy decision:

1. bounded frame completeness and structural decoding;
2. exact Channel version and established session identity;
3. recognized message/control kind;
4. interaction identity syntax, scope, replay, and concurrency bound;
5. exact profile and interaction class/direction;
6. external phase predicate;
7. payload Shape and declared bounds;
8. authority/control structure without projection;
9. local authority decision; and
10. handler dispatch.

Steps 1-9 cannot cause a provider/application handler effect. An implementation may combine passes
mechanically, but its observable classification and zero-effect boundary must match this order.

## Concurrent interactions

- Admission reserves one in-flight position atomically before dispatch.
- A refusal caused by the bound emits no local request and does not consume a lasting replay entry.
- Accepted interaction identities enter the replay set before handler dispatch.
- Outcomes may arrive in any order and close only their named interaction.
- Drain snapshots the admitted set and refuses new candidates; it does not reorder or cancel the set.
- A fatal session or transport loss maps every nonterminal local initiator and recipient interaction
  to `lost`. A recipient instead reaches `peer-fault` only when one scoped protocol fault actually
  commits. Each interaction retains its own effect evidence.

The contract promises no fairness. A finite bound is a safety/resource fact, not a scheduling policy.

## Cancellation

Cancellation is optional in the profile and exact when present:

1. the profile declares whether cancellation is unsupported, optional, or required for a class;
2. required cancellation unsupported by either endpoint refuses C1 establishment;
3. cancellation has a distinct authority requirement and may be denied independently;
4. exactly one cancellation request is legal. The initiator may send it from `dispatched`; the
   recipient applies it from `executing`, or holds exactly one while `validating` and applies it when
   admission resolves. The two preconditions are local to their own endpoints and no event
   synchronises them — the recipient's admission transition emits no frame and there is no
   request-accepted acknowledgement — so a control that arrives before admission completes has lost
   no race, is not a fault, and is held. If admission then refuses, the held control is discarded
   with no answering frame and the late-traffic latch does not fire;
5. `accepted` means the recipient has accepted responsibility to request cancellation from the
   handler, not that the handler has stopped and not that effects are zero;
6. `refused` means execution continues under the ordinary terminal contract, and a handler terminal
   of `cancelled` with no cancellation request in force — because none arrived, or because the
   recipient refused the one that did — is invalid at both endpoints: the recipient commits one
   interaction-scoped `internal-channel-failure` rather than an Outcome, and the initiator records
   the contradicting cancelled Outcome as `peer-fault`;
7. only a semantic Outcome, peer fault, or local loss is terminal; and
8. accepted/refused acknowledgement state is recorded explicitly; unsolicited, duplicate, or
   contradictory acknowledgement/control is an interaction-scoped `state-violation`; and
9. a cancel request racing a terminal Outcome accepts whichever terminal fact is valid first, while
   the late control is recorded and does not replace it.

## Late terminal and control disposition

An identity refused at `unseen` is not a terminal interaction and owns no latch. No request with that
identity was ever accepted, so no interaction exists to be terminal: the recipient commits one
interaction-scoped peer fault, retains no history, no latch, and no in-flight reservation, and a later
request bearing that identity arrives at `unseen` like any other first request. Holding per-identity
state for an identity a peer never opened is the unbounded-state exposure the 2026-08-13 R1 ruling
refused, and a retained terminal record would be exactly that state.

Every other terminal interaction owns a `late-traffic-fault` latch with exactly three values:

- `clear`: no post-terminal violation has been handled;
- `fault-committed`: one interaction-scoped `state-violation` peer fault was committed; and
- `fault-unavailable`: the fault could not be committed, so only a local loss/late-traffic
  observation exists.

The first duplicate semantic terminal or late non-fault control while the latch is `clear` preserves
the first accepted terminal history and attempts exactly one interaction-scoped `state-violation`
peer fault. Successful commit sets `fault-committed`; inability to commit sets `fault-unavailable`.
A late peer fault never receives an answering fault, and no late input after either settled value
emits another frame. Effect certainty and the semantic terminal remain those of the first history.

## Interaction event totality

The transition tables and the companion state/event coverage grid are closed-world. A recognized
peer event in a nonterminal interaction state without a more specific legal row becomes an
interaction-scoped `state-violation` and terminal `peer-fault`; certainty is `known-none` before
dispatch and otherwise `unknown` unless explicit evidence narrows it. A wrong-state local action
that has not emitted a frame is refused locally and leaves the interaction unchanged. Local loss in
any nonterminal state selects `lost`, pre-dispatch states included, and certainty is what separates
them rather than whether the rule applies: `known-none` before dispatch and `unknown` after it unless
explicit evidence narrows it. Terminal input follows the late-traffic latch.
No implementation may ignore an unlisted recognized event or invent another state.

## Relational initialization

`relational-initialisation` is an interaction class, not a session state and not a separate terminal
model. Its admission record additionally contains the exact lifecycle declaration identity, edge,
direction, initiating member, receiving member, Operation, Capability requirement, and input Shape.

The external predicate is `interconnected && !ready`. Success is evidence consumed by the
composition root; Channel does not mark the member Ready. Any other terminal form blocks that root
from claiming relational completion. Ordinary interaction uses the same machine with a different
class and `released` predicate.

## Terminal provenance

| Terminal history | Peer semantic statement? | Peer Channel statement? | Local observation? |
| --- | --- | --- | --- |
| initiator or recipient `refused-local` | no | no | yes |
| `outcome-succeeded/failed/cancelled` | yes | no | receipt/commit also observed locally |
| initiator `peer-fault` / recipient `peer-fault` / recipient `rejected-protocol` | no | yes | receipt/commit also observed locally |
| initiator or recipient `lost` | no | no | yes |

No adapter may translate horizontally between these columns merely because its local API has one
error union.

## Capability-wide properties

- **I1.** One interaction identity crosses the dispatch boundary at most once per session.
- **I2.** Every accepted interaction has at most one terminal history.
- **I3.** No cancellation acknowledgement, drain event, timeout, or protocol fault becomes semantic
  success.
- **I4.** Every pre-dispatch refusal is `known-none`; every possible post-dispatch loss is `unknown`
  unless explicit evidence narrows it.
- **I5.** Concurrency never exceeds the established finite bound under any generated interleaving.
- **I6.** A relational interaction matches exactly one declaration and never creates Ready/Release.
- **I7.** A terminal fact for one interaction changes no sibling interaction's terminal history.

## Deliberate limits

Core 0.2 does not define bidirectional streams, server-initiated unsolicited events, partial Outcomes,
or persistent activity. A profile needing them must declare a facet whose design preserves the core
session identity, authority regime, and terminal provenance or use a later Channel version.

Core cancellation is cooperative and observable. It is not preemption, transaction rollback, or an
exactly-once guarantee.
