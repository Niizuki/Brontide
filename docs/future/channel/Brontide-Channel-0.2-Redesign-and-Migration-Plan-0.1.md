# BRONTIDE

## Channel 0.2 Redesign and Migration Plan 0.1

**Status:** First-batch design foundation drafted and its four owner rulings resolved. B1-B4, N1-N3,
F1-F3, D1-D5, T1-T4, and R1-R3 are closed as framed, the last three re-verified by the seventh review
in the artifacts they were raised against. That review's blocking finding S1 — that the R1 correction
kept `rejected-protocol` at recipient `unseen` under a delivery-ordering guarantee stated in the
state/event grid alone, which C4 and C11 disclaimed and the responsibility matrix assigned to
`delivery-facet` — is corrected under the 2026-08-13 S1 ruling: Channel 0.2 core owns intra-interaction
frame order, narrowly scoped, stated in C4 with `C4-P2` and a mutation vector, given an owner row in
the responsibility matrix, and declared by the realization profile. Nonblocking S2 and S3 are
dispositioned in the same pass. The eighth review then found S1 closed as to ownership and not as to
falsifiability and raised blocking **U1** with nonblocking **U2**-**U8**; those are corrected, as are
**V1**-**V3**, **W1**-**W6**, **X1**-**X7**, **Y1**-**Y4**, **Z1**-**Z4**, **AA1**-**AA3**,
**AB1**-**AB2**, and **AC1**-**AC4**, every one raised by an author-side iteration pass over the
previous corrections and none by an independent review. AB1 is this status block, which had stopped at
S3 while six passes ran. AC1-AC4 are the layer under the Y and V corrections — the arrival ordinal
stated only in the artifact that reads it, a closed detailed-reason set with no value for the refusal
`C4-P2` quantifies over, the property's own subject naming the wrong endpoint, and a class check blind
to two-letter finding families. **AD1**-**AD3** then turned the same method on the retained records
themselves: AD1 is the AC pass's residual denying that the AA and AB evidence existed and referring
the gap to the owner, AD3 the three disagreeing accounts of what the W iteration review contains, and
AD2 the half of the X7 class check still written over two ids, which is left open as an owner call.
A fresh independent closure re-review of that whole sequence precedes Batch 2. No Channel 0.2
implementation or ratification is claimed.
**Designed against:** Brontide Architecture 0.8, Complete Draft.
**Predecessor evidence:** [Channel Design Note 0.1](./Brontide-Design-Note-Channel-0.1.md),
[Draft Channel Contract 0.1](./Brontide-Draft-Channel-Contract-0.1.md), and the
[Architecture 0.8 Channel requirements and risk ledger](./architecture-0.8-channel-requirements-and-risk-ledger.md).
**First realization:** [Portable Component Binding 0.1](../binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md),
retained as experimental evidence rather than treated as the 0.2 design template.

## 1. Decision and purpose

Channel 0.1 has useful implementation evidence but remains provisional. Its naming question is
resolved by choosing an explicitly migrated revision rather than ratifying the 0.1 Shape and category
names. The successor is not limited to renaming those Shapes. Channel 0.2 will reconsider the
capability boundary, state models, message taxonomy, attribution model, and extension seams before
any replacement schema or public API is written.

This is a deliberate redesign, not a presumption that the existing contract is wrong. Channel 0.1
was extracted from two working interchange protocols, then exercised by two independent Portable
Binding implementations and an implementation-neutral peer. It therefore provides unusually strong
evidence about invariants and failure cases. It also carries the structure of synchronous,
single-request process protocols from which it was extracted. Channel 0.2 must preserve the former
without accepting the latter as architecture by default.

The intended outcome is one coherent semantic foundation that can support the next binding revision
and later Channel realizations without another foundational migration. This does not mean pulling
streaming, durable delivery, distributed trust, or long-running workflow into Channel. It means
settling Channel's ownership and extension points so those capabilities can compose with it without
silently changing its meanings.

## 2. Why a redesign is warranted

Decision 13 requires more than an additive message. Portable Binding 0.1 makes establishment imply
readiness, while Component Management requires exact relational lifecycle traffic after
interconnection and before Ready. Correcting that requires a separate readiness transition and a
new class of pre-Ready interaction.

That break exposes wider questions in the 0.1 model:

- one envelope taxonomy combines contract negotiation, request/Outcome traffic, session shutdown,
  and peer-reported protocol faults;
- the same surrounding taxonomy describes local process or transport observations that are not wire
  messages at all;
- `Lifecycle` currently means orderly session shutdown, while the missing relational lifecycle is
  an authorized Operation between group members;
- Channel, Portable Binding, Composition, and lifecycle gating do not yet have a complete ownership
  boundary;
- establishment may be negotiated or statically fixed, but the semantic equivalence those paths must
  establish is not one explicit artifact;
- optional `execution` and `occurrence` positions may be attribution context rather than Channel
  correlation identities;
- `unsupported-kind` also classifies an unknown protocol-error category, conflating two distinct
  protocol faults; and
- cancellation, concurrency, draining, streaming, retry, and delivery are non-promises, but 0.1 does
  not always distinguish “not provided by this version” from “owned by a compatible extension seam.”

None of those observations preselects a replacement. They define questions the first design batch
must answer.

## 3. Design stance

Channel 0.2 begins from observable capability and responsibility, not from CLR types, schemas, CBOR
tags, or the two existing host implementations.

The redesign follows these rules:

1. **Preserve proven semantics, not accidental structure.** A 0.1 rule survives when its meaning is
   still required, even if its representation moves or disappears.
2. **Keep peer statements separate from observer facts.** What a peer reports, what a transport
   proves, and what the local host infers must never share a form that permits one to masquerade as
   another.
3. **Make state and authority explicit before effects.** Every interaction class has a legal state,
   initiator, recipient, exact authority requirement, and failure behavior.
4. **Design extension seams without claiming extension semantics.** Channel may declare where Flow,
   Distributed, Realtime, Lifecycle, or a transport profile composes; it does not inherit their
   delivery or trust guarantees.
5. **Fail closed across versions.** Channel 0.1 and 0.2 are distinct contracts. No decoder or adapter
   silently treats one as the other.
6. **Keep the stacks independent.** The neutral contract is data-only; Reference and Minimal
   implement it natively and meet only through external artifacts and process seams.
7. **Treat silence as a design finding.** Each capability has a property over all of its vectors,
   and the first batch includes a completeness review that asks what the contract does not say.

## 4. Semantics expected to survive

The redesign starts with a rebuttable presumption that these 0.1 semantics remain necessary:

- contract compatibility is established before an interaction may cause provider effects;
- unknown required versions, features, Shapes, Operations, authority forms, and control values fail
  closed;
- payload positions and authority/control positions remain distinct, with projection allowed only
  where the declared plane permits it;
- a Capability never crosses a trust boundary as though possession or serialization conveyed
  authority;
- a local authority denial causes no far-side effect and is not fabricated as a peer observation;
- semantic failure, peer protocol fault, and local transport/process failure remain different facts;
- no foreign exception, runtime type, stack trace, or private identity crosses the seam;
- correlation is exact, bounded, and type-distinct from Actor, Execution, Occurrence, and other
  identity spaces;
- framing and parsing are bounded before allocation or effect;
- known zero provider effects and an unattributable effect count remain different observations; and
- interruption, retry, fallback, and delivery are never converted into fabricated success.

The first-batch migration ledger may retain, replace, move, or remove the representation of any of
these. Removing the semantic invariant itself requires an explicit owner decision and rationale.

## 5. Candidate conceptual decomposition

The following decomposition is the starting hypothesis, not a completed contract.

### 5.1 Contract profile

One immutable, inspectable profile states the exact contract identity and version, endpoint roles,
interaction classes, control features, authority-presentation mode, representations, limits,
correlation requirements, concurrency declaration, and extension points. Negotiated and statically
fixed realizations must establish the same semantic facts even when their mechanics differ.

### 5.2 Session control

Session control owns establishment, acceptance or refusal, readiness signaling, draining, orderly
closure, and terminal protocol fault. It has an explicit state machine and does not carry an
application Outcome. A session-control message cannot silently authorize an Operation.

The design must decide whether interconnection is a Channel state, a Portable Binding state mapped
onto Channel, or an external composition fact. It must not appear in two state machines with subtly
different transition rules.

### 5.3 Interaction

An interaction is one typed, correlated exchange admitted by the established profile and current
state. Ordinary invocation and relational initialization may share a general interaction form, but
their classes, authority, legal windows, and gates remain exact. The design must compare that model
with a dedicated relational envelope kind before selecting either.

Relational initialization carries the exact CM3 declaration: edge, direction, initiating and
receiving members, Operation, Capability, and input Shape. It occurs after interconnection and before
Ready. Undeclared or mismatched traffic is refused before delivery. Ordinary interaction remains
closed until Release.

### 5.4 Local observation

Local observation records what this endpoint can establish about framing, transport, process loss,
timeouts, peer termination, retry, fallback, boundary crossings, and provider effects. It is not a
wire message and cannot be treated as a peer admission. Unknown attribution is retained with a
reason instead of replaced by zero or a guessed failure domain.

A peer-reported protocol fault remains a peer statement inside the protocol. Its local receipt may
produce an observation, but the two forms and their provenance stay distinct.

## 6. Responsibility boundaries to settle

The first batch must produce an explicit responsibility matrix covering at least these areas:

| Concern | Candidate owner | Boundary question |
| --- | --- | --- |
| Contract/profile establishment | Channel | What exact facts must fixed and negotiated realizations establish equally? |
| Frame encoding and transport adapter | Realization/profile | Which bounds are Channel invariants and which are declared realization choices? |
| Payload and resource representation | Portable Binding | Which representation facts must Channel carry without owning resource semantics? |
| Authority evaluation and grants | Authority domain / Component Management | What presentation or attributable context may Channel carry without constituting authority? |
| Session establishment, readiness, drain, close | Channel or Portable Binding, selected once | Which transitions are protocol facts and which are composition facts? |
| Relational initialization | Lifecycle declaration + composition, transported by Channel | How is exact declared traffic represented and authorized without inventing a Component-to-Component binding? |
| Ordinary interaction gate | Portable Binding / composition | Which Channel state is necessary but not sufficient for Release? |
| Semantic Outcome | Operation contract | Which failures are application facts rather than protocol faults? |
| Protocol fault | Channel | Which faults may a peer report, and what provenance accompanies them? |
| Transport/process failure observation | Local host | How is uncertainty preserved without turning an observation into a wire claim? |
| Concurrency, cancellation, and flow control | Channel core or declared extension | What must 0.2 settle so a later extension does not reinterpret correlation or terminality? |
| Delivery, retry, ordering, persistence | Distributed/Flow/Realtime profiles | What are Channel's explicit non-promises and compatible attachment points? |

No implementation phase begins while one concern has two semantic owners or no owner.

## 7. Required first batch: design foundation

The first batch is mandatory and lands before a Channel 0.2 schema, public type, package, host, or
provider implementation. Its artifacts form one review unit because each constrains the others.

### 7.1 Fresh C1-Cn capability contract

Author-pass artifact: [Channel 0.2 C1-C12 capability contract](./Brontide-Channel-0.2-Capability-Contract-0.1.md).

Write a new behavioral contract from observable inputs, states, effects, outcomes, and failures. It
must not copy C1-C10 merely because Portable Binding 0.1 used them. Each capability item includes:

- the observable property being promised;
- its authority and effect boundary;
- legal and illegal state transitions;
- failure and uncertainty behavior;
- at least one named scenario and one property over all of that capability's vectors;
- the evidence required from each native implementation, a neutral peer, and process pairing; and
- explicit silence: nearby behavior the item intentionally does not specify.

The contract must cover contract establishment, session control, interaction admission and
terminality, relational initialization, correlation, authority-plane handling, failure provenance,
observation/effect attribution, bounds, compatibility, and closure. It may add or split items where
the completeness review shows one property has multiple owners.

### 7.2 Explicit session state machine

Author-pass artifact: [Channel 0.2 session state machine](./Brontide-Channel-0.2-Session-State-Machine-0.1.md).

Specify states, events, guards, effects, terminal states, and illegal transitions. At minimum, test
the distinctions among unestablished, established, interconnected where applicable, ready, released
where applicable, draining/withdrawing, closed, and faulted states. The model must say which of those
are Channel states and which are external facts projected into Channel guards.

Every transition states who may initiate it, what is transmitted, whether provider effects may have
occurred, and what late or duplicate traffic means. Readiness is never inferred from establishment.

### 7.3 Explicit interaction state machine

Author-pass artifact: [Channel 0.2 interaction state machine](./Brontide-Channel-0.2-Interaction-State-Machine-0.1.md).

Specify the lifecycle of one interaction independently from the session. Cover admission, dispatch,
peer acceptance where present, semantic Outcome, peer protocol fault, cancellation or its explicit
absence, timeout, interruption, duplicate/late terminal traffic, and effect uncertainty.

The model must define whether one session permits one, sequential, or concurrent interactions and
how correlation and draining behave for the selected rule. It must also define the pre-Ready window
for relational initialization and the post-Release window for ordinary invocation.

### 7.3a Closed state/event coverage

Correction-pass artifact: [Channel 0.2 state/event coverage](./Brontide-Channel-0.2-State-Event-Coverage-0.1.md).

The first-batch machines additionally carry a closed-world state/event grid. Every recognized event
family in every session, initiator, recipient, and terminal state maps to exactly one detailed row,
named catch-all, or finite late-traffic rule. Generated Batch 2 model vectors must enumerate this
grid. An unlisted event may not be ignored or assigned implementation-specific behavior.

### 7.4 Responsibility matrix

Author-pass artifact: [Channel 0.2 responsibility matrix](./Brontide-Channel-0.2-Responsibility-Matrix-0.1.md).

Complete the matrix begun in section 6 against Channel, Portable Binding, Component Management,
Composition, Lifecycle, Flow, Distributed, Realtime, authority domains, and concrete transports.
For every shared boundary, name the direction of dependency and the neutral artifact crossing it.

### 7.5 Contract-completeness and silence review

Author-pass artifact: [Channel 0.2 contract-completeness review](./Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md).

Conduct a review separate from conformance review. It asks, per capability, what the contract does
not say. At minimum it probes:

- simultaneous and sequential interactions;
- cancellation before dispatch, during possible effects, and after a terminal response;
- half-close, drain, late traffic, duplicate traffic, and peer loss in every nonterminal state;
- partial frames, oversized declarations, resource exhaustion, and allocation failure;
- replay and idempotency without accidentally promising exactly-once execution;
- version skew, optional features, unknown extensions, and downgrade behavior;
- authority revocation or replacement between establishment and dispatch;
- effect attribution when a response is missing, malformed, mismatched, or rejected locally;
- referenced-resource lifetime and cleanup when the transport is lost;
- how streaming, delivery, and long-running extensions attach without redefining terminality; and
- recovery or reconnection without equating a new session with the old one.

Each finding is corrected in the contract or recorded as an explicit non-goal with an owner and
extension seam. Agreement between the two existing stacks is not evidence that silence is safe.

### 7.6 Channel 0.1 to 0.2 migration ledger

Author-pass artifact: [Channel 0.1-to-0.2 migration ledger](./Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md).

Inventory every 0.1 logical Shape, field, message kind, state, category, failure domain, limit,
vector, and observation field. Give each one exactly one disposition:

- **retained** — same semantic meaning, with its 0.2 location;
- **replaced** — new meaning or structure, with the incompatibility explained;
- **moved** — same fact but a different semantic owner, including wire-to-local moves;
- **removed** — no 0.2 equivalent, with the safe migration behavior; or
- **legacy-only** — retained solely to execute or diagnose 0.1 evidence.

The ledger also maps every 0.1 vector to retained evidence, a revised 0.2 vector, or a documented
retirement. It identifies which golden encodings and parity digests must change and which historical
pins remain untouched.

### 7.7 Neutral contract and vector design brief

Author-pass artifact: [Channel 0.2 neutral contract/vector brief](./Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md).

Before authoring schemas, define the data-only artifact boundaries, identifier representations,
version-negotiation rule, vector grouping, capability-wide property format, expected observations,
and golden-encoding policy. The brief must be implementable without importing either stack and must
not derive expectations from one implementation's public API.

### 7.8 Fresh independent design review

Review policy, retained negative attestations, and the exact continuation instructions:
[`reviews/`](./reviews/README.md#exact-next-work). Seven independent negative attestations are
retained. Their findings through T1-T4 and R1-R3 have correction passes, the last three confirmed
closed by the seventh review at `3892c23a8dd4c7f298e877ba73710ee0ddc97bc4`. That review's blocking
S1 and nonblocking S2 and S3 are corrected under the 2026-08-13 S1 ruling, with a failing-first
design-verifier check written before the correction and mutation-tested after it. A fresh conforming
closure re-review attestation and a closure record are still required. The T1-T4
correction pass and the totality attestation that found them share one actor, which the review policy
records as a disclosed deviation. The seventh review is the first whose isolation is complete: it ran
from a fresh isolated clone with a reviewer identity distinct from every earlier reviewer and from
the correction author.

Obtain a fresh-context review of the complete first batch before implementation. Reviewers assess
Architecture 0.8, both local implementation targets, the retained 0.1 evidence, Decision 13, every
C-item, both state machines, the closed state/event grid, the responsibility matrix, the silence
review, and the migration ledger. A finding is corrected in the design package before public
surfaces are created.

### 7.9 First-batch exit gate

The batch is complete only when:

1. every C-item has a falsifiable property, named scenarios, and an evidence owner;
2. session and interaction state machines agree with the contract and each other;
3. every semantic concern has exactly one owner;
4. every 0.1 contract element and vector has a migration disposition;
5. version mismatch and downgrade behavior fail closed;
6. the silence review has no unowned finding;
7. the neutral artifact brief can be implemented without either stack; and
8. independent design review records no unresolved blocking finding.

## 8. Subsequent batches

The exact phase names may change after the first batch. The required order does not.

### Batch 2 — neutral Channel 0.2 contract

Author versioned, data-only schemas, shared vectors, capability-wide properties, and deterministic
golden encodings. Add a neutral verifier that loads neither stack. Prove every new test can fail by
running it against an intentionally incomplete or incorrect neutral endpoint before accepting it.

### Batch 3 — Reference native realization

Implement the contract natively outside Reference Core. Write each C-item's named tests with the
behavior and observe them fail before completing the path. Preserve strongly typed identities and
the existing dependency direction.

### Batch 4 — Minimal native realization

Implement independently outside Minimal Model/Kernel, using Minimal-owned algebraic types and
explicit results. Do not translate the Reference public surface or reuse its runtime.

### Batch 5 — process, neutral-peer, and cross-stack evidence

Run both hosts against their native providers, the implementation-neutral provider, and each
other's provider in both directions. Compare declared semantic profiles rather than private types.
Exercise every state transition and failure class across a real process seam, including the cases
where provider-effect attribution must remain unknown.

### Batch 6 — Portable Binding migration

Create an explicit Portable Binding 0.2 profile over Channel 0.2. Preserve the 0.1 profile as a
versioned experimental endpoint while required by retained evidence. Do not add an in-process
compatibility layer between the stacks. Any compatibility adapter is an external, bounded gateway
that declares both versions and cannot broaden authority or fabricate observations.

Integrate Decision 13's relational initialization through the exact CM3 declarations and CM4 stage
order. Establishment, relational interaction, readiness, Release, withdrawal, and cleanup evidence
must be derived from executed transitions rather than caller claims.

### Batch 7 — closure and ratification recommendation

Run the complete repository gate, refresh measurements and governed documentation, perform a second
contract-completeness review against implementation findings, and obtain fresh conformance reviews
of the neutral contract and both stacks. Only then may the project decide whether the Channel 0.2
logical contract is ready for ratification or needs another draft revision.

Ratification, stable package publication, and Architecture 0.8 ratification remain separate owner
decisions.

## 9. Compatibility and migration policy

- Channel 0.1 remains executable, historical experimental evidence. It is not retroactively renamed
  or described as a stable public contract.
- Channel 0.2 uses a distinct contract version. Unknown or mismatched versions fail before provider
  effects.
- There is no field-by-field “best effort” downgrade. An endpoint either establishes an exact
  supported profile or refuses.
- A dual-version host keeps each version behind its own decoder, state machine, limits, and
  observation mapping. Shared private helpers may implement mechanics but never decide semantics for
  both versions implicitly.
- A 0.1 observation may be projected into 0.2 only where the migration ledger proves the meaning is
  identical. Missing 0.2 facts remain unknown or unavailable; they are never synthesized.
- Retained evidence files and direct or transitive pins are not moved or rewritten merely to make
  the new plan tidy. Any required evidence relocation follows the repository's explicit repinning
  and independent-review policy.
- The migration ends only when every known consumer and fixture names its selected version and the
  remaining lifetime of the 0.1 endpoint is documented.

## 10. Required design questions

The first batch must answer or explicitly disposition these questions:

1. What is the smallest Channel capability independent of Portable Binding and process transport?
2. Which state belongs to a Channel session, and which belongs to binding or composition?
3. Is relational initialization a distinct message kind or a state-gated interaction class?
4. Which exact facts make negotiated and fixed establishment semantically equivalent?
5. What constitutes Channel correlation, and what is merely carried attribution context?
6. Which peer-reported faults are legal, and how are they distinguished from local observations?
7. What are the terminality rules when provider effects may have occurred but no valid Outcome is
   available?
8. What concurrency does core 0.2 support or declare, and how does drain interact with it?
9. Does core represent cancellation, or only declare that a selected extension/profile supplies it?
10. How do Flow, Distributed, Realtime, and Lifecycle attach without redefining Channel identities,
    terminality, or authority?
11. Which representation and resource facts are carried by Channel but owned by Portable Binding?
12. What is the exact compatibility and retirement policy for the 0.1 realization?

## 11. Non-goals

This plan does not itself:

- ratify Channel, Portable Binding, Architecture 0.8, or a standard vocabulary;
- choose a wire encoding, media type, package name, or numeric tag allocation;
- add cross-domain identity, attestation, key distribution, or authority federation;
- promise reliable delivery, ordering, exactly-once effects, automatic retry, persistence, or
  recovery;
- require streaming, backpressure, cancellation, or concurrent multiplexing in core 0.2 before the
  first batch decides their correct ownership;
- introduce a Component-to-Component binding merely to carry relational initialization;
- move Channel into Reference Core or Minimal Model/Kernel; or
- optimize the hot path before the semantic and observation model is evidenced.

## 12. Risks and controls

| Risk | Control |
| --- | --- |
| “Full redesign” becomes unbounded feature accumulation | Responsibility matrix and explicit non-goals; extension seams are designed without implementing extension semantics. |
| The 0.2 contract is 0.1 with renamed types | Migration ledger requires a reasoned disposition and owner for every retained structure. |
| Valuable 0.1 failure lessons are lost | Preserved-invariant list, vector-by-vector migration, and retained executable 0.1 profile. |
| Both stacks repeat the same silent defect | Capability-wide properties, separate silence review, neutral peer, and fresh design/conformance reviewers. |
| State is duplicated across Channel and Binding | Responsibility matrix and first-batch gate require one semantic owner per transition. |
| Compatibility broadens authority or fabricates facts | Exact profile establishment, external bounded adapters, and unknown rather than synthesized observation data. |
| The redesign freezes a wire accident | Semantics and state machines precede schemas, encodings, and public APIs. |
| Historical evidence is invalidated during cleanup | Stable-path discipline and explicit repinning authorization for any evidence-bearing move. |

## 13. Completion conditions

The redesign programme is complete when:

- the first-batch design package and its independent review are closed;
- the neutral Channel 0.2 contract is self-contained and fully property/vector verified;
- Reference and Minimal implement every capability independently;
- direct, process, neutral-peer, and both cross-stack directions agree on portable observations;
- Decision 13's protocol-bearing group reaches the exact CM3/CM4 stage order without caller-claimed
  readiness or authority;
- every 0.1 item and consumer has an explicit migration disposition;
- dependency guards, complete stack suites, and the repository gate pass without warnings;
- documentation distinguishes retained 0.1 evidence, implemented 0.2 behavior, and remaining
  non-goals; and
- fresh independent reviewers find no unresolved contract, migration, or implementation defect.

Completion establishes implementation evidence. It does not by itself ratify Channel or publish a
stable extension.

## Open questions (owners needed)

There are no unresolved owner decisions in the first-batch design foundation. Independent review may
still identify questions that require owners before closure.

## Resolved questions

- **2026-08-11 — Core concurrency and cancellation:** Channel 0.2 core defines finite bounded unary
  concurrency and optional cancellation terminal semantics. Profiles select whether cancellation is
  supported and choose their finite concurrency bound; they do not redefine correlation or
  terminality.
- **2026-08-11 — Session-state ownership:** Channel owns only `unestablished`, `establishing`,
  `established`, `draining`, `closed`, and `faulted`. Portable Binding owns Interconnection, Release, withdrawal, and cleanup; Composition owns the Relational Initialisation phase; Component Management owns Ready. Each consumes or supplies Channel observations across a neutral seam without sharing semantic ownership.
- **2026-08-11 — Relational initialization representation:** relational initialization is an exact,
  state-gated interaction class using the ordinary Channel interaction form, not a distinct envelope
  family. Lifecycle and Component Management retain ownership of the declaration and readiness
  semantics carried by that interaction.
- **2026-08-11 — Extension invariants:** exact profile facets may add interaction classes and
  evidence, but cannot redefine Channel identities, authority, terminal provenance, or effect
  certainty. A design that needs to change those invariants requires a new Channel contract version.
- **2026-08-11 — Ratify Channel 0.1 names or migrate:** publish an explicitly migrated revision.
  Channel 0.1 remains experimental evidence; its provisional logical names are not ratified as the
  lasting public contract.
- **2026-08-11 — Scope of the successor:** conduct a full Channel 0.2 redesign rather than limiting
  the revision to Decision 13's minimum lifecycle delta. Preserve proven invariants, but reconsider
  capability boundaries, state machines, taxonomy, observations, and extension seams before schemas
  or public surfaces.
- **2026-08-11 — Required first work:** the capability contract, session and interaction state
  machines, responsibility matrix, silence review, migration ledger, neutral-artifact brief, and
  fresh independent design review form one mandatory first batch before implementation.
- **2026-08-13 — R1 correction ruling, a cancellation that races recipient admission:** hold the
  control until admission resolves. This is a correction ruling raised by the sixth independent
  review; it does not join the four first-batch rulings above. Three options were weighed. **Option A,
  selected:** the recipient holds exactly one valid cancellation control while `validating` and
  applies it when admission resolves — no new wire traffic, no fault for conformant behaviour, and the
  hold is bounded at one because C8 already permits only one control. **Option B, rejected:** refuse
  the control framelessly; smallest change, but the initiator sits in `cancel-pending` never learning
  its cancellation did nothing, and a silent drop is the contract-silence class this programme keeps
  finding defects in. **Option C, rejected:** add a request-accepted acknowledgement so the race
  becomes avoidable and the existing fault becomes correct; principled, but it puts a new control
  frame on every interaction and enlarges a deliberately minimal bounded-unary protocol to settle a
  corner case. The ruling also splits `unseen` from `validating` in the recipient grid: at `unseen`
  there is no accepted identity to correlate and holding state would let a peer allocate unbounded
  local state, so that control stays `rejected-protocol`.
- **2026-08-13 — S1 correction ruling, who owns intra-interaction control ordering:** Channel 0.2
  core owns it, narrowly scoped. This is a correction ruling raised by the seventh independent
  review; like the R1 ruling it does not join the four first-batch rulings above. The R1 ruling above
  kept `rejected-protocol` at `unseen`, which is sound only if a conformant control cannot arrive
  there — and the sentence establishing that lived in the state/event grid alone, while C4's silence
  and C11 disclaimed ordering and the responsibility matrix assigned it to `delivery-facet` with
  Channel core named as explicitly not the owner.

  **Option A, selected:** core promises that within one session, for one interaction identity, frames
  sent by one endpoint are delivered in the order that endpoint committed them. C4 states it with
  `C4-P2` and the `C4-control-precedes-request` mutation vector, C4's silence and C11 are scoped to
  cross-interaction and cross-session ordering, the matrix gains an `Intra-interaction frame order`
  row owned by `channel-core`, and the realization profile declares per-interaction frame order so a
  profile can verify it. *(The identifier recorded in this ruling was later normalised to `channel`
  under U2, which found `channel-core` to be a second name for one contract family. The owner is
  unchanged; the ruling text is retained as issued.)* The `unseen` fault is then correct and provable. The obligation is small:
  one direction of one interaction carries at most a request and one cancellation control, so an
  unordered transport conforms by sequencing two frames rather than by building a reordering buffer.

  Two further arguments carried the decision. The design already half-believed the promise — the
  contract's boundary section says core does not provide "ordering across interactions" and the
  migration ledger's retained non-promise reads "no cross-interaction order", while C4's silence and
  C11 stated it unscoped, so those four artifacts disagreed about the *scope* of the non-promise
  before the R1 correction touched anything. And a substrate that cannot promise its own two frames
  for one interaction arrive in order is a weak substrate.

  **Option B, rejected:** core does not own it, and `unseen` holds the control as `validating` does.
  At `validating` the hold is bounded because admission is local and terminates. At `unseen` the
  recipient waits on a peer frame that may never arrive, and core has no timeout, deadline, or expiry
  concept anywhere — timing belongs to the Realtime facet. So B leaves the hold unbounded, imports
  timing into core, or refuses at a bound and reintroduces `rejected-protocol` for a conformant peer,
  which is R1 again. Bounding by `max-in-flight` does not rescue it, because that bounds admitted
  interactions and would let a peer consume the budget with identities it never opens. B needs a new
  unowned fact to work, which is the defect class S1 belongs to.

  **Option C, rejected:** leave C4 and C11 untouched and require a delivery facet of any profile that
  declares cancellation support, tightening the matrix's `may require facet` to `must`. This
  preserves the existing ownership assignment and is the smaller edit, but the matrix bundles
  delivery, persistence, and ordering under one owner, so it would drag persistence and retry into
  every cancelling profile unless that row were split anyway — and it makes cancellation unavailable
  on a bare core profile.

  Nonblocking S2 is dispositioned under the same pass rather than by ruling, because it needed a
  statement rather than a choice: loss and drain are the third and fourth exits from `validating`, a
  held control is discarded with no answering frame and does not fire the late-traffic latch, and an
  interaction whose admission has not resolved is outside the drain snapshot. The interaction
  machine's pre-dispatch loss rule is reconciled to "any nonterminal state", with certainty rather
  than applicability as what separates pre- from post-dispatch.
