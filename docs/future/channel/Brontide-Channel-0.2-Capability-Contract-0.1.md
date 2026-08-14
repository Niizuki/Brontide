# Channel 0.2 capability contract 0.1

Date: 2026-08-11

Status: proposed first-batch behavioral contract; N2, F1/F2, D1-D4, T3, R1, S1, S2, U1, W2, W3, and
W4 corrected after independent review. C4 now owns intra-interaction frame order with `C4-P2`, and
C4's silence and C11 are scoped to cross-interaction and cross-session ordering. Under the U1
correction `C4-P2` is stated over the refusal a reordering produces rather than over the accepted
sequence, because the design refuses a reordered frame and the accepted sequence can therefore never
be out of order. It carries one named mutation per conjunct under W3, and under W4 an identity
refused at `unseen` retains no interaction history and no latch. No Channel 0.2 schema, API,
implementation, or ratification is authorized until the complete design foundation receives a fresh
independent closure re-review.

Designed for: Brontide Architecture 0.8, Complete Draft, especially sections 6.16, 13.6, 16.4,
18.1, 19, and 24.

Companion artifacts:

- [session state machine](./Brontide-Channel-0.2-Session-State-Machine-0.1.md);
- [interaction state machine](./Brontide-Channel-0.2-Interaction-State-Machine-0.1.md);
- [responsibility matrix](./Brontide-Channel-0.2-Responsibility-Matrix-0.1.md);
- [contract-completeness review](./Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md);
- [Channel 0.1 migration ledger](./Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md); and
- [neutral contract and vector brief](./Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md).

## Boundary

Channel is the semantic agreement by which two endpoint roles establish one versioned communication
profile and conduct bounded, correlated interactions without sharing private runtime machinery.
Channel defines the facts an endpoint may send, accept, refuse, and observe. It does not select a
provider, constitute authority, define an Operation's domain semantics, own Component activation,
promise delivery, or identify a transport.

Channel has two independent state dimensions:

- one **session** establishes a profile and admits zero or more interactions until drain or failure;
- each **interaction** has its own admission, dispatch, optional cancellation, and terminal history.

Portable Binding and Composition may gate an interaction using their own phases. They do not add
states to the Channel session. In particular, Interconnection, Relational Initialisation, Ready, and
Release remain Component/Binding facts. Channel carries and enforces a profile's exact interaction
class and phase guard without claiming ownership of those phases.

## Common terms

- **Profile** — immutable data declaring the Channel contract version, application contract,
  endpoint roles, interaction classes, Shape positions, authority mode, limits, concurrency,
  optional controls, and extension use.
- **Session identity** — opaque identity of one established or attempted Channel session. It is not
  an Actor, Execution, Occurrence, binding, process, or transport identity.
- **Interaction identity** — opaque identity of one interaction within one session. It is not reused
  within that session and is not an Execution or Occurrence identity.
- **Interaction class** — exact profile-declared purpose and gate, such as
  `relational-initialisation` or `ordinary` in the Portable Binding 0.2 profile.
- **Peer protocol fault** — a bounded peer assertion that Channel processing rejected a session or
  interaction. It is a wire fact with peer provenance, not proof of transport or provider behavior.
- **Local loss observation** — what this endpoint can establish when no valid semantic Outcome or
  peer protocol fault closes an interaction. It never becomes a peer assertion.
- **Effect certainty** — `known-none`, `known`, or `unknown`, with a reason where unknown. Channel
  owns the certainty form; a profile owns any domain-specific effect count or details.

## C1 — exact profile establishment precedes interaction effects

A session establishes one immutable profile before any interaction is dispatchable. A negotiated
realization exchanges a proposal and acceptance; a fixed realization validates the same profile
locally. Both yield the same inspectable facts. Unknown Channel versions, required features,
interaction classes, authority modes, or incompatible application contracts refuse establishment.
There is no implicit downgrade and no in-place renegotiation.

**Authority and effect boundary.** Establishment recognizes communication meaning only. It grants no
authority and starts no application/provider effect.

**Failure and uncertainty.** A local validation refusal emits no peer statement. A peer may return a
bounded establishment fault. Transport loss produces a local observation. None produces an
established session.

**Named scenarios.** `C1-fixed-negotiated-equivalence`, `C1-required-feature-unknown`,
`C1-no-silent-downgrade`, and `C1-profile-change-needs-new-session`.

**Property C1-P1.** For every C1 vector, either exactly one profile is established with every
normative fact equal to the expected profile, or no interaction is dispatchable and effect certainty
is `known-none`.

**Evidence.** Neutral profile vectors; native fixed and negotiated tests in each stack; process
pairing against a neutral peer; direct/process profile parity.

**Silence.** C1 does not select a provider, attest a peer, establish cross-domain identity, or choose
a wire encoding.

## C2 — the Channel session has one small, explicit state machine

Core session states are `unestablished`, `establishing`, `established`, `draining`, `closed`, and
`faulted`. Only `established` admits new interactions. Drain refuses new interactions while allowing
already admitted ones to reach a terminal fact. `closed` and `faulted` are terminal. Fixed-profile
establishment may move directly from `unestablished` to `established` only after C1 validation.

Interconnection, Ready, Release, withdrawal of a binding, and Component termination are not Channel
session states. A profile may use those external facts as exact interaction guards.

**Authority and effect boundary.** A session transition never authorizes an Operation. Closing or
faulting a session does not imply that an in-flight provider effect was undone.

**Failure and uncertainty.** Illegal or duplicate control is a protocol fault. In particular, a
second local or peer drain moves `draining` to `faulted`, records one session-scoped
`state-violation`, preserves the first drain snapshot, and does not rewrite any interaction's effect
certainty. Peer loss faults the local session and leaves each nonterminal interaction to record its
own effect certainty.

**Named scenarios.** `C2-drain-refuses-new`, `C2-drain-preserves-in-flight`,
`C2-ready-is-not-session-state`, and `C2-late-control-after-close`.

**Property C2-P1.** Every accepted session transition belongs to the published transition table;
every other input leaves the prior state unchanged or enters `faulted`, and no terminal session
returns to a nonterminal state.

**Evidence.** Transition-table properties in both stacks; every legal edge and representative
illegal edge through a real process; fixed-profile transition evidence.

**Silence.** C2 does not define process launch, reconnect, session resumption, leader election, or
Component activation ordering.

## C3 — interaction class and external phase are exact admission inputs

Every interaction names one profile-declared class. The profile declares its initiator role,
recipient role, Operation/Shape positions, authority mode, allowed external phase predicate, and
terminal forms. An unknown class, wrong direction, or false/unknown phase predicate refuses before
dispatch. Channel evaluates the declared predicate but does not create or advance the external phase.

The Portable Binding 0.2 profile declares at least:

- `relational-initialisation`, admitted only for an exact CM3 lifecycle declaration after
  interconnection and before Ready; and
- `ordinary`, admitted only after the composition has recorded Release.

**Authority and effect boundary.** Phase eligibility is not authority. C6 must independently permit
the interaction.

**Failure and uncertainty.** A locally false or unknown guard is frameless refusal with
`known-none` at either endpoint, including the recipient's independently derived external phase.
A peer receiving a class or direction outside the established profile returns an interaction-scoped
protocol fault with no handler dispatch.

**Named scenarios.** `C3-relational-before-ready`, `C3-ordinary-before-release-refused`,
`C3-unknown-class`, and `C3-wrong-direction`.

**Property C3-P1.** No vector dispatches an interaction unless its class, direction, and external
phase predicate all exactly match the established profile.

**Evidence.** Neutral class/phase table and adversarial vectors; both native roots; CM3/CM4 composed
tests; both cross-stack directions.

**Silence.** C3 does not decide how a composition obtains its phase, which members form a group, or
whether a failed activation rolls back.

## C4 — correlation, concurrency, replay, and ordering are explicit

One session identity scopes distinct interaction identities. Profile declarations set a finite
positive `max-in-flight`. Channel 0.2 core supports that declared bounded concurrency; the first
Portable Binding 0.2 profile may select one or a larger tested bound. Outcomes, faults, cancellation
controls, and observations name the exact interaction. Reusing an accepted interaction identity in
the same session is replay and never dispatches the handler again.

No cross-interaction completion order is promised. Within one session, for one interaction identity,
frames sent by one endpoint are delivered in the order that endpoint committed them. This is the
whole of the ordering Channel 0.2 core promises, and it is what lets a recipient distinguish a
cancellation control that races its own admission from a control naming an identity it has never
been asked to open. The obligation binds one direction of one interaction, where an initiator commits
at most a request and one cancellation control, so a realization over an unordered transport
satisfies it by sequencing those frames rather than by building a general reordering buffer. Within
one interaction, accepted events follow the interaction state machine. A new session has a new
identity and cannot resume or inherit the replay window of an old session unless a later extension
defines a distinct resumption contract.

**Authority and effect boundary.** Correlation and possession of an identity grant no authority.

**Failure and uncertainty.** Missing, extra, wrong-session, or mismatched identities reject the
claimed terminal fact. If the request crossed dispatch, rejecting a malformed terminal fact leaves
effect certainty `unknown`, not zero.

A repeated accepted identity received while its original interaction is nonterminal commits one
interaction-scoped `replay-detected` peer fault for that identity. The original handler is not
redispatched, the recipient closes its interaction history as peer fault, any later handler terminal
is ignored, and effect certainty remains `unknown` unless explicit evidence narrows it. Replay after
an accepted terminal never replaces that first terminal history.

**Named scenarios.** `C4-two-complete-out-of-order`, `C4-bound-exceeded`,
`C4-replay-not-redispatched`, `C4-terminal-correlation-mismatch`, `C4-control-precedes-request`, and
`C4-outcome-precedes-ack`.

`C4-control-precedes-request` and `C4-outcome-precedes-ack` are mutation vectors rather than legal
paths, one per direction, because `C4-P2` binds both. In the first a realization delivers one
interaction's cancellation control before the request that opens it; in the second it delivers the
recipient's semantic Outcome before the cancellation acknowledgement the recipient committed first,
so the acknowledgement lands on an interaction the Outcome has already made terminal. A conforming
realization can produce neither, and they exist so that each conjunct of `C4-P2` has something to
fail on.

Their expected observations are exactly what the receiving endpoint records: one `rejected-protocol`
for a control naming an identity the recipient has never been asked to open, and one late-traffic
`state-violation` for the displaced acknowledgement. Those recorded refusals are the witnesses
`C4-P2` fails on. Each is complete data rather than an unspecified expectation, which is what `C12`
requires of every vector.

**A control refused at `unseen` retains no interaction history and no latch.** The identity was never
accepted, so it never enters the replay set, and the recipient commits one interaction-scoped peer
fault and keeps nothing: no terminal history, no `late-traffic-fault` latch, and no reservation
against the in-flight bound. This is what makes the `unseen` verdict bounded rather than merely
frameless — holding *any* per-identity state there, including a terminal record, would let a peer
accrue unbounded local state by naming identities it never opens, which is the exposure the 2026-08-13
R1 ruling refused. A later request bearing that identity therefore arrives at `unseen` as any other
first request does, and is admitted on its own merits; the earlier fault does not bar it, because a
refusal the recipient did not retain cannot bar anything. Under a conforming realization the sequence
never arises, and under a reordering one `C4-P2` has already gone red on the recorded refusal.

**Property C4-P1.** Across every C4 vector, each accepted terminal fact closes exactly one admitted
interaction, no interaction identity is dispatched twice, and the number of nonterminal interactions
never exceeds the established finite bound.

**Property C4-P2.** Across every C4 vector, for each interaction identity, nothing delivers two frames
one endpoint committed in an order that endpoint did not commit them in. Loss may still drop a frame.
Because the design refuses a reordered frame rather than accepting it, this is stated over the refusal
that reordering produces rather than over the accepted sequence, which no reordering can leave out of
order: no endpoint records a recipient `rejected-protocol` at `unseen` for a cancellation control
whose request the same endpoint had already committed, and none records a late-traffic
`state-violation` latched against a frame the same endpoint committed before the frame that made the
interaction terminal. Restricting each conjunct to one endpoint's own frames is load-bearing: across
endpoints Channel promises no order, so a legal late control that arrives after a peer's terminal, and
a duplicate terminal from a nonconformant peer, must both leave this property green.
`C4-control-precedes-request` is the mutation this property must go red on, and a run in which it
stays green is a finding against the property rather than evidence for the design.

**Evidence.** Model-based state tests and generated interleavings in both stacks; neutral peer with
out-of-order outcomes; replay and mismatch process vectors; and a realization-profile declaration of
per-interaction frame order that a profile checks at establishment.

**Silence.** C4 promises neither fairness nor relative scheduling, cross-interaction or cross-session
ordering, durable deduplication, or exactly-once effects. The intra-interaction frame order stated
above is the whole of what it promises about order: nothing here constrains how a realization
interleaves distinct interactions, and no ordering survives a session boundary.

## C5 — payload compatibility and bounds are positional and pre-effect

Every Shape-described position is declared payload or authority/control. Payload input and Outcome
positions follow Architecture 0.8 section 16.4 covariance and projection. Authority/control
positions never project. The profile declares finite frame, nesting, collection, scalar, and
in-flight bounds; a realization may declare tighter environmental limits only if profile
establishment exposes and accepts them.

Parsing and structural validation occur before handler dispatch. Unknown additional structure in an
open payload Shape may project canonically; missing required or incompatible structure refuses.

**Authority and effect boundary.** Shape compatibility never satisfies C6 authority. Limit and
payload refusal before dispatch has `known-none`.

**Failure and uncertainty.** Allocation/resource failure is locally classified without transporting
a runtime exception. A partial or oversized frame never becomes a partial interaction.

**Named scenarios.** `C5-open-payload-projection`, `C5-authority-position-no-projection`,
`C5-oversized-before-dispatch`, and `C5-partial-frame-no-interaction`.

**Property C5-P1.** Every dispatched vector has passed every declared bound and every positional
Shape rule; every pre-dispatch structural refusal records `known-none` and no semantic Outcome.

**Evidence.** Data-only Shape and bound vectors; bounded decoder properties in each stack; process
partial-frame and allocation probes; neutral peer.

**Silence.** C5 does not standardize a schema language, memory layout, encoding, compression,
resource-transfer protocol, or streaming chunks.

## C6 — authority stays local, exact, attributable, and independent of compatibility

The profile declares one boundary-relative authority presentation mode. Inside one authority domain,
a target may evaluate a recognized Capability presentation. Across a trust boundary, no Capability,
Constraint expression, or derivation chain crosses; the receiver evaluates attributable context and
exact designations under its own admission policy. Unknown authority structure is never projected and
never permits.

Authority is evaluated for every interaction after structural admission and before handler dispatch.
The effective authority is attributable to the request or to authority the responding Actor
deliberately presents as its own, recorded as such. Delivery, correlation, profile establishment,
provider availability, and Shape compatibility grant nothing.

**Authority and effect boundary.** A local denial is a local observation, emits no denial message,
and records `known-none`.

**Failure and uncertainty.** A peer receiving a forbidden authority form returns a protocol fault
before handler dispatch. A cross-trust peer claim is evidence for local policy, never a grant.

**Named scenarios.** `C6-local-unknown-denies`, `C6-cross-trust-capability-forbidden`,
`C6-shape-compatible-but-unauthorized`, and `C6-deputy-authority-attributed`.

**Property C6-P1.** No C6 vector reaches handler dispatch unless one exact local authority decision
is `permitted`; every denial or unevaluatable presentation records the decision point, initiator
attribution, and `known-none`.

**Evidence.** Neutral strong-Kleene and boundary-mode vectors; native authority adapters; process
proof that forbidden authority bytes do not leave the sender; cross-stack denial parity.

**Silence.** C6 does not define Capability serialization across domains, identity federation,
attestation, admission policy, Genesis, or revocation distribution.

## C7 — relational initialization is ordinary interaction machinery with a distinct class

Relational initialization uses the C3 interaction form rather than a second envelope family. Its
class requires exact CM3-declared edge, direction, initiating member, receiving member, Operation,
Capability, and input Shape. The composition root may initiate on the Component's behalf as recorded
by Decision 13. Its authority is exact and separate from participant admission and ordinary authority.

The interaction is admitted only after the owning composition records Interconnection and before it
records Ready. Success permits the composition to continue; semantic failure, local denial, peer
fault, or local loss prevents Ready and Release and returns the actual observation to CM4 cleanup or
rollback. Channel itself does not advance those phases.

**Authority and effect boundary.** Relational authority permits only the declared lifecycle
Operation on the declared edge and does not authorize ordinary interaction.

**Failure and uncertainty.** A failure preserves any effects that may have occurred. Missing terminal
evidence after dispatch is `unknown`; it is never projected as a completed lifecycle stage.

**Named scenarios.** `C7-exact-declared-relational-success`, `C7-undeclared-edge-refused`,
`C7-after-ready-refused`, and `C7-loss-prevents-ready`.

**Property C7-P1.** Every dispatched relational vector matches one and only one lifecycle declaration,
occurs in the pre-Ready window, and cannot produce a Ready or Release fact by itself.

**Evidence.** Neutral relational declaration vectors in both directions; CM3/CM4 integration tests;
native and cross-stack process evidence including semantic failure and peer loss.

**Silence.** C7 does not introduce Component-to-Component bindings, infer group topology, define
rollback, or authorize undeclared peer traffic.

## C8 — every interaction has one terminal history; cancellation is explicit but not magic

An interaction reaches exactly one terminal history: local refusal before dispatch, semantic
Outcome, peer protocol fault, locally observed loss, or cancellation completed by a valid terminal
Outcome. A semantic Outcome may succeed, fail with shaped details, or report `cancelled` when the
established profile supports cancellation.

Cancellation is an optional core control with fixed meaning. Exactly one cancellation request may be
sent for a nonterminal dispatched interaction. `dispatched` is the initiator's own local state, and
the recipient's admission is not observable from it: the recipient's admission transition emits no
frame and Channel declares no request-accepted acknowledgement, so an initiator cannot know when the
recipient has begun executing.

A cancellation control that arrives while the recipient is still admitting the interaction is held,
not faulted. The recipient retains exactly one held control and applies it when admission resolves.
If admission succeeds, dispatch crosses the boundary first and the held control is then evaluated
under local cancellation authority, reaching the same accepted or refused acknowledgement it would
have reached had it arrived a moment later. If admission refuses, the interaction is already terminal,
the held control is discarded with no answering frame, and the late-traffic latch does not fire — a
control that was legal when it was sent does not become late traffic because the request it named was
refused. A second control while one is held is an interaction-scoped `state-violation`.

Admission succeeding and admission refusing are not the only ways `validating` ends. If the session
or transport is lost, or drain refuses the interaction, while a control is held, the interaction
reaches `lost` or its drain refusal exactly as it would have with no control outstanding: the held
control is discarded with no answering frame, and the late-traffic latch does not fire, for the same
reason a refused admission does not fire it. A held control never changes which terminal an
interaction reaches; it only supplies a cancellation decision when the interaction survives long
enough to have one. An interaction still in `validating` when drain arrives is not in the drain
snapshot, because the snapshot is taken over admitted interactions and admission has not resolved.

The peer may acknowledge `accepted` or `refused`; the
initiator records those as distinct nonterminal states, and either proves nothing about effects. An
unsolicited, duplicate, or contradictory acknowledgement/control is an interaction-scoped
`state-violation`. The interaction remains nonterminal until a semantic Outcome, peer fault, or local
loss arrives. If cancellation is required but not supported, profile establishment refuses; if merely
optional and unsupported, no cancellation control is legal.

**Authority and effect boundary.** Cancellation authority is declared separately from invocation
authority. Accepting cancellation does not erase effects already performed.

**Failure and uncertainty.** Duplicate terminal facts are protocol faults and do not replace the
first accepted terminal history. The first duplicate terminal or late non-fault control attempts one
interaction-scoped `state-violation` peer fault and settles a finite late-traffic latch; a peer fault
or later late traffic receives no answering frame. A structurally invalid, unrecognized, unsupported, or wrongly
scoped cancellation control produces one interaction-scoped peer protocol fault; because invocation
may already be executing, effect certainty remains unknown unless stronger evidence exists. A
`cancelled` Outcome with no cancellation request in force contradicts the accepted history and is
invalid at both endpoints: the recipient commits an `internal-channel-failure` instead of the Outcome
and the initiator records a peer fault. Loss after cancellation acceptance retains unknown
effects unless stronger evidence exists.

**Named scenarios.** `C8-semantic-failure-is-not-protocol-fault`,
`C8-cancel-accepted-still-awaits-outcome`, `C8-unsolicited-cancel-ack-fault`,
`C8-contradictory-cancel-ack-fault`, `C8-cancel-unsupported-at-profile`,
`C8-invalid-cancel-control-peer-fault`, `C8-cancel-during-admission-held`,
`C8-cancel-held-then-admission-refused`, and `C8-duplicate-terminal-rejected`.

**Property C8-P1.** Every interaction has at most one accepted terminal history, and no cancellation
control, drain, timeout, or protocol rejection is recorded as semantic success.

**Evidence.** Interaction model properties; native terminal-race tests; process cancellation and
duplicate-terminal vectors; neutral peer.

**Silence.** C8 does not guarantee that a cancellable Operation stops immediately, rolls back, or
performs no effect.

## C9 — peer statements and local failures retain distinct provenance

Channel separates four forms:

1. local pre-dispatch refusal, which emits no peer frame;
2. semantic Outcome, which asserts the Operation's terminal result;
3. peer protocol fault, which asserts only that the peer endpoint rejected Channel processing; and
4. local loss observation, used when no valid peer terminal fact is available.

Peer faults are session- or interaction-scoped and carry one recognized category plus bounded
non-normative diagnostics. Unknown peer-fault categories are never mapped to `unsupported-kind` and
never provoke an infinite fault exchange: the receiver faults the local session as
`unrecognized-peer-fault` and sends no answering fault. Local loss categories and detection points
are observer-relative and do not claim global topology.

**Authority and effect boundary.** No failure form grants authority or fabricates a provider result.

**Failure and uncertainty.** Runtime exceptions, stack traces, private types, authority objects, and
unbounded diagnostics never cross. Internal failures map to a declared portable category locally.

**Named scenarios.** `C9-failed-outcome-distinct`, `C9-peer-fault-provenance`,
`C9-unknown-fault-no-loop`, and `C9-process-loss-is-local`.

**Property C9-P1.** Every terminal vector selects exactly one of the four provenance forms; no field
permits a local inference to be accepted as a peer statement or a protocol fault as an Outcome.

**Evidence.** Neutral provenance vectors; native exception-sanitization properties; peer/local
perspective process tests; neutral peer with unknown fault category.

**Silence.** C9 does not prove a peer honest, locate a failed process globally, or define domain
error details.

## C10 — observation records evidence and preserves effect uncertainty

Every attempted establishment and interaction yields a local observation sufficient to distinguish
profile, session and interaction identities, direction, class, admission and authority decisions,
dispatch boundary, terminal provenance, peer-reported facts, local detection point, retry/fallback
facts supplied by an owning extension, and effect certainty.

Channel's portable effect field is certainty, not a provider-specific count:

- `known-none` only where the observer proves dispatch did not occur or the declared handler did not
  begin;
- `known` with profile-owned details where attributable evidence exists; or
- `unknown` with a reason whenever dispatch may have crossed and evidence cannot decide.

Observations never drive protocol behavior. Non-normative timing and diagnostics are excluded from
semantic parity.

**Authority and effect boundary.** Observation does not constitute authority, success, or cleanup.

**Failure and uncertainty.** Missing evidence stays absent/unknown; adapters never synthesize zero,
success, a peer statement, or a narrower failure domain.

**Named scenarios.** `C10-pre-dispatch-known-none`, `C10-peer-loss-unknown`,
`C10-peer-reported-known-details`, and `C10-diagnostics-excluded-from-parity`.

**Property C10-P1.** Every observation is complete for its provenance form, and no vector with a
possible post-dispatch path records `known-none` without explicit evidence that the handler did not
begin.

**Evidence.** Neutral observation-shape properties; production-path tests in both stacks; direct,
process, neutral-peer, and cross-stack parity profiles.

**Silence.** C10 does not standardize logs, metrics, tracing, clocks, storage, or a universal domain
effect counter.

## C11 — extensions compose through declared profile facets without redefining core facts

A profile declares every required extension facet by exact identity and version. Unknown required
facets refuse establishment; unknown optional facets are ignored only when the profile says their
absence preserves the same core semantics. An extension may add interaction classes, payload forms,
or stronger delivery evidence, but it may not reinterpret session/interaction identities, authority
decisions, the four terminal provenance forms, or effect uncertainty.

Flow may define streaming and backpressure over a declared interaction class; Distributed may define
delivery attempts, ordering, persistence, and cross-domain trust evidence; Realtime may add timing
constraints; Lifecycle may define long-running activity. Each still terminates or observes through
the Channel core forms unless a future Channel version explicitly changes them.

Retries are new attempts with new interaction identities and optional attributable causation to the
prior attempt. Reusing one interaction identity is replay, not retry. Channel core promises no retry,
durable delivery, cross-interaction ordering, persistence, resumption, or exactly-once effect. The
single ordering fact core does own is C4's intra-interaction frame order; a facet may add delivery
and ordering guarantees beyond it but may not weaken it.

**Authority and effect boundary.** An extension declaration grants nothing and cannot broaden C6.

**Failure and uncertainty.** A required stronger guarantee that no mutually supported facet supplies
refuses at establishment.

**Named scenarios.** `C11-required-extension-unknown`, `C11-optional-additive-ignored`,
`C11-retry-new-identity`, and `C11-extension-cannot-redefine-authority`.

**Property C11-P1.** Every established profile has all required facets supported exactly, and no
facet changes a core identity, authority, terminal-provenance, or uncertainty result.

**Evidence.** Neutral feature-negotiation vectors; a deliberately unknown facet; a minimal additive
facet; native profile validation and neutral peer.

**Silence.** C11 does not define any named extension's full contract or require an implementation to
support one.

## C12 — the contract is portable, bounded, deterministic, and independently testable

The Channel 0.2 neutral contract is data-only and contains no executable semantics shared by the two
stacks. Every identity space is distinct in each public/native realization even when its neutral
scalar representation is the same. Semantic decisions receive profiles, external phase facts,
authority results, and local observations explicitly; they use no ambient clock, service lookup, or
unordered enumeration.

Reference and Minimal implement every C-item natively. A neutral peer imports neither. Direct and
process realizations may differ in framing, copies, timing, and local diagnostics only where the
parity profile declares the difference.

**Authority and effect boundary.** Shared fixtures and generated data confer no authority and contain
no private runtime objects.

**Failure and uncertainty.** A vector with an unspecified expectation is invalid evidence rather
than permission for each stack to choose. Every property must be able to fail against a named
incorrect implementation.

**Named scenarios.** `C12-neutral-provider-no-stack-dependency`,
`C12-direct-process-semantic-parity`, `C12-distinct-identity-spaces`, and
`C12-property-negative-probe`.

**Property C12-P1.** Every neutral vector has one deterministic expected portable observation, every
C1-C12 group has at least one capability-wide property, and neither stack nor neutral peer imports
the other's semantic runtime.

**Evidence.** Neutral verifier; dependency guards; native suites; both cross-stack directions;
negative probes proving each capability property can fail.

**Silence.** C12 does not require identical source structure, public APIs, allocation strategies,
or local diagnostic text.

## Cross-capability invariants

These hold across every Channel 0.2 vector:

1. Unknown control or authority never broadens behavior.
2. No application/provider effect is claimed before exact profile, class, phase, Shape, and authority
   admission.
3. A possible post-dispatch path never becomes known zero without evidence.
4. One fact has one provenance and one semantic owner.
5. Session closure, interaction terminality, and external composition phase remain distinct.
6. No private runtime or authority object crosses a neutral or process seam.
7. No extension silently changes a core identity, terminal form, or authority rule.

## Deliberate limits

Channel 0.2 core is a bounded unary-interaction substrate. It supports a finite declared number of
concurrent interactions and optional cancellation semantics, but does not itself provide streams,
backpressure, durable delivery, automatic retry, ordering across interactions, resumption, or
long-running activity. Those capabilities require declared facets or future contracts.

Resource representation, ownership, integrity, lifetime, release, and fallback belong to Portable
Binding or another profile. Channel carries the shaped positions and observes loss; it does not
invent a universal resource protocol.

Channel establishes communication compatibility, not peer identity or trust. Cross-domain mutual
identification and attestation remain Identity/Distributed work, with local admission under
Architecture 0.8 section 24.
