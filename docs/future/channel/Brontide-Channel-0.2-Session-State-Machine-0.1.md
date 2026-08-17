# Channel 0.2 session state machine 0.1

Date: 2026-08-11

Status: proposed first-batch design artifact; D1 corrected after independent review and subject to a
fresh independent closure re-review. Unchanged by the **AI1**-**AI9** and **AJ1**-**AJ7** families;
the claim is stated over each family rather than over its last finding, because "unchanged by AI9" is
a true statement about one finding and a false impression about the family, which is **AJ5**.
Corrected for **AL1** and **AL4**: all six of `S1`-`S6` now name the session they are about, and `S5`
names the one declared profile its two establishment paths are compared over.

This status block previously recorded that the AK pass had audited `S1`-`S6` against C12's newly
declared per-session facts and that none of the six named one, "because the session machine's
properties are about one session by construction". The first half was true and the second is the
defect: the same argument was available for `I5` — an interaction belongs to one session by
construction too — and **AK7** rejected it and required `I5` to name the session all the same. `S3`
read one session's own drain transition across the vector and was red on a conforming two-session
vector while this block reported the audit clean. That is **AL1**, and the audit that missed it could
not have found it, because its trigger set is C12's declared fact list and the session's own state was
absent from it (**AL3**).

Contract owner: [Channel 0.2 C2](./Brontide-Channel-0.2-Capability-Contract-0.1.md#c2--the-channel-session-has-one-small-explicit-state-machine).

## Boundary

The Channel session machine owns profile establishment, admission of new interactions, draining,
orderly close, and terminal Channel fault. It deliberately does not own Component or Binding phases.

The following are **not** Channel session states:

- Local Initialisation;
- Interconnection;
- Relational Initialisation;
- Ready;
- Release/Active;
- binding withdrawal; and
- Component termination or rollback.

Those are external facts owned by Component Management, Composition, or Portable Binding. A Channel
profile may require one as an interaction-admission predicate; Channel receives the fact explicitly
and never advances it.

## States

| State | Terminal | Admits a new interaction? | Meaning |
| --- | --- | --- | --- |
| `unestablished` | no | no | No profile is accepted. A negotiated proposal or fixed-profile validation may begin. |
| `establishing` | no | no | A negotiated proposal is outstanding. No application/provider interaction may dispatch. |
| `established` | no | yes, subject to profile and external guards | One immutable profile is accepted. |
| `draining` | no | no | No new interaction is admitted; admitted interactions may reach a terminal history. |
| `closed` | yes | no | Orderly session end. No message or interaction is legal. |
| `faulted` | yes | no | Channel processing cannot continue. No message or interaction is legal. |

`closed` and `faulted` never transition. Reconnect creates a new session identity and begins at
`unestablished`.

## Events

| Event | Initiator | Peer transmission | Application/provider effect possible? |
| --- | --- | --- | --- |
| `validate-fixed-profile` | local composition root | none | no |
| `send-establish-proposal` | initiating endpoint | bounded profile proposal | no |
| `receive-establish-proposal` | peer endpoint | proposal received | no |
| `accept-establishment` | receiving endpoint | bounded acceptance | no |
| `refuse-establishment` | receiving endpoint | bounded peer fault/refusal | no |
| `begin-drain` | either endpoint or local owner | drain control when negotiated transport is live | in existing interactions only |
| `receive-drain` | peer endpoint | drain control received | in existing interactions only |
| `close` | either endpoint after drain condition | close control when transport is live | in existing interactions only if close is premature, therefore premature close faults instead |
| `receive-close` | peer endpoint | close control received | no when legal |
| `fatal-protocol-fault` | either endpoint | at most one peer fault; none when the incoming fault is itself unrecognized | possibly in existing interactions |
| `local-loss` | local observer | none | possibly in existing interactions |

## Legal transition table

| From | Event and guard | To | Required observation |
| --- | --- | --- | --- |
| `unestablished` | fixed profile validates exactly under C1 | `established` | fixed establishment, no frame, `known-none` establishment effects |
| `unestablished` | local endpoint sends or accepts one valid proposal | `establishing` | proposal identity and exact offered profile |
| `establishing` | one exact acceptance matches the proposal | `established` | established profile equal to fixed-profile form |
| `establishing` | local validation or peer establishment refusal | `closed` | refusal provenance and `known-none` |
| `unestablished` | local fixed validation refuses | `closed` | frameless local refusal and `known-none` |
| `established` | local or peer drain begins | `draining` | drain initiator and current in-flight set |
| `draining` | duplicate local or peer drain control | `faulted` | session-scoped `state-violation`; preserve the original drain snapshot and all interaction effect evidence |
| `draining` | all admitted interactions terminal and close is sent/received | `closed` | orderly close and empty in-flight set |
| any nonterminal | fatal recognized Channel violation | `faulted` | peer fault or local violation provenance; each interaction records its own certainty |
| any nonterminal | transport/process loss prevents continuation | `faulted` | local loss category/detection point; each interaction records its own certainty |

Two endpoints may observe the crossing transitions at different instants. Conformance compares each
endpoint's legal local history and portable observations; it does not require a global simultaneous
state.

## Refused and illegal inputs

| State | Input | Result |
| --- | --- | --- |
| `unestablished` | interaction, drain, or close | local refusal if not emitted; session-scoped `state-violation` if received |
| `establishing` | second proposal, interaction, drain, or unmatched acceptance | session-scoped protocol fault; no application dispatch |
| `established` | second establishment or profile mutation | session-scoped protocol fault; profile remains immutable until the session faults |
| `draining` | new local interaction | frameless local refusal with `known-none` |
| `draining` | new peer interaction | interaction-scoped `state-violation`; session may continue draining unless the profile declares it fatal |
| `draining` | close while interactions remain nonterminal | fatal session `state-violation`; affected interactions preserve possible effects |
| `closed` or `faulted` | any local operation | local terminal-session refusal; no frame |
| `closed` or `faulted` | any received frame | ignored for semantics and recorded as late traffic; no answering fault loop |

## Drain protocol

Drain is symmetric but its control occurs exactly once per endpoint history:

1. the first accepted local or peer drain moves the local session to `draining`;
2. a subsequent local or peer drain control is a session-scoped `state-violation` and moves the
   session to `faulted`; the first drain snapshot and every interaction's effect evidence remain;
3. no new interaction may be admitted locally after the first drain transition;
4. interactions already admitted continue under the interaction state machine;
5. close is legal only when the local in-flight set is empty; and
6. a peer close with locally nonterminal interactions is a protocol fault, not proof those
   interactions produced no effects.

Channel does not promise that an unresponsive peer will cooperate with drain. Timeout or transport
loss faults the session and closes each nonterminal interaction through a local loss observation.

## Session event totality

The legal and refused/illegal tables are a closed-world dispatch table. A recognized event in a
nonterminal state that has no more specific legal row is a session-scoped `state-violation` and moves
the session to `faulted`. An unrecognized or structurally invalid received session control is a
session-scoped structural peer fault and moves the session to `faulted`. A wrong-state local action
that has not emitted a frame is a frameless local refusal and leaves the state unchanged only where
the refused/illegal table says so. Terminal-state input follows the terminal rows and never emits an
answering-fault loop.

This totality rule does not override a specific nonfatal row such as a new peer interaction during
drain. Every event/state pair therefore has one result rather than an implementation-selected
default.

## Fixed and negotiated equivalence

Fixed establishment skips the transmitted proposal/acceptance mechanics, not their semantics. The
fixed validator must produce the same immutable profile record a negotiated acceptance would have
produced, including endpoint roles, required features, interaction classes, limits, concurrency,
authority mode, and extension facets. A field absent from the fixed path is a contract defect rather
than realization freedom.

## External phase guards

An interaction class may declare an external predicate such as:

```text
relational-initialisation:
    interconnected = true
    ready = false

ordinary:
    released = true
```

The predicate is supplied explicitly with the local admission request and, where the peer must
validate it independently, derived from that peer's own profile-owned state. Channel treats `false`
and `unknown` identically for admission: refuse before dispatch. It records the predicate result but
does not mutate the external state.

## Capability-wide properties

Each of these is a statement about **one session**, and each says so. A vector may carry more than one
session under AH1, so a property of this machine that leaves the session unnamed is read across the
vector: that is **AL1**, and it made `S3` red on a vector whose two sessions both conform.

- **S1.** In each session the vector carries, every accepted transition of that session is in the
  legal table.
- **S2.** No interaction dispatches outside its own session's `established` state.
- **S3.** Within each session the vector carries, no new interaction is admitted after that session's
  first drain transition. The scope is the whole of this property: the drain transition belongs to one
  session, and a second session establishing and admitting afterwards is legal.
- **S4.** Within each session the vector carries, a terminal session never becomes nonterminal and is
  never resumed under the same session identity.
- **S5.** For each session the vector carries, fixed and negotiated establishment of that session's
  own declared profile produce equal normative profile records. The comparison is between the two
  paths to **one** declared profile, which is what the fixed and negotiated equivalence section above
  states; two sessions carrying two different declared profiles are conforming and this property says
  nothing about them. That qualifier is **AL4**, and it is the `AK8` correction `C1-P1` received.
- **S6.** In any session the vector carries, no session event creates Ready, Release, authority, or an
  application Outcome.

Each property receives a generated model test in both stacks and a named negative probe in the
neutral verifier before implementation closure.

## Deliberate limits

There is no half-open resumable session state. A transport half-close that prevents required duplex
control is a local loss and faults the session. A future simplex or resumable profile requires a
new declared Channel contract rather than reinterpretation of this table.

There is no idle timeout or keepalive in core. A transport/profile may supply liveness observations,
but expiry and retry remain outside this machine.
