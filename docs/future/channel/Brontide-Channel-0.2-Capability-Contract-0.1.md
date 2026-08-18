# Channel 0.2 capability contract 0.1

Date: 2026-08-11

Status: proposed first-batch behavioral contract; awaiting a fresh independent
closure re-review, on hold under the owner decision of 2026-08-17 recorded in the
[verification foundation plan](./Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md).
Correction history is not carried here; it is owned by the
[disposition index](./reviews/channel-0.2-disposition-index.md#c1-c12-capability-contract).

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

**Property C1-P1.** For every C1 vector, either each session the vector carries has exactly one
established profile with every normative fact equal to that session's expected profile, or no
interaction is dispatchable and effect certainty is `known-none`.

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
phase predicate all exactly match the established profile of its own session.

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

Their expected observations are the complete set of records both endpoints produce under the vector,
and **not the refusal alone**. For `C4-control-precedes-request` that set begins with the recipient's
`rejected-protocol` for a control naming an identity it has never been asked to open, and continues
with what the displaced request produces when it arrives: the recipient admits it at `unseen` as any
first request bearing that identity does, dispatches it, and commits its terminal, while the initiator
records what its own machine rows give it for a terminal arriving against the control it committed.
For `C4-outcome-precedes-ack` the set contains one late-traffic `state-violation` whose latch records
the displaced acknowledgement as the frame that settled it.

**The subsequent admission is part of the expected observation, not a consequence left implicit.** It
is the second fact `C4-P2`'s first conjunct reads, so a vector authored to stop at the refusal leaves
the membership test an empty set and takes the property green on its own named mutation — which is U1
reached through the vector rather than through the property, and is how AF1 was raised against the
AE1 correction. What `C12` requires is that the vector state every one of these records as complete
data rather than an unspecified expectation; it does not require this paragraph to enumerate the
per-endpoint rows, which the two state machines already determine.

These recorded facts, and not the refusals alone, are the witnesses `C4-P2` fails on.

The second witness is the settling frame and not the latch value. `fault-committed` is one of three
enum values and names no frame, and the two cases the property must leave green — a legal late control
arriving after a peer's terminal, and a duplicate terminal from a nonconformant peer — record that
same value against the same `state-violation` category. What separates the mutation from them is which
frame settled the latch and which endpoint committed it, so the latch records that frame and the
parity profile compares it.

**The conjunct compares that frame against a second one, and the observation names it too.** The
clause is a precedence between two of one endpoint's own frames: the frame the latch settled against,
and that endpoint's own frame the interaction's terminal history was accepted on. The first has been
published as a five-field reference since Y4 and carried its session since AI1; the second was
published by nothing and left to be read off the terminal form, which identifies one frame only while
an endpoint commits at most one frame of that form for one identity. A nonconformant peer commits two,
which is exactly what a duplicate terminal is and is a required-green member of this property's own
group, so the observation records **the terminal-frame reference** as well — the same fields, for the
reason Y4 gave for the settling frame. That is **AK6**, and it is the shape W5, AH1, AI1, AJ1 and AK1
all have: an operator qualifier whose operand the record it reads does not publish.

**A control refused at `unseen` retains no interaction history and no latch.** The identity was never
accepted, so it never enters the replay set, and the recipient commits one interaction-scoped peer
fault and keeps nothing: no terminal history, no `late-traffic-fault` latch, and no reservation
against the in-flight bound.

It does **record** one C10 local observation of the refusal, carrying the **refused-frame reference**,
and recording is not retaining. The reference is the first conjunct's operand and it names the same
five facts the settling frame's does. Until **AK1** and **AK5** the record carried its provenance, its
detailed reason, and the kind of frame refused and nothing else, so three of the conjunct's own
qualifiers had no operand at all: AF8 scopes the membership test to **one session** and the record
named no session, the test is over **that identity** and the record named no identity, and the
precedence half is over **the committing endpoint's** own frames and the record named no endpoint.
The first of those is the sharpest, because a property is red rather than merely unevaluable when its
operand is missing: a two-session vector legitimately reusing one identity value, conforming at both
endpoints, has the refusal in one session and the admission in the other, and the membership test
finds the identity — which is AE1's own failure mode reached through the operand instead of through a
clause, and is the failure AF8's sentence below was written to prevent. The arrival ordinal is AK5's
other half and is Y4's argument on this operand: one endpoint may commit two controls naming one
identity in one session, and the record has to say which of them it refused, or a control committed
before the request binds to the one committed after it and the property goes red on delivery that
matched commit order. An
observation is written once as evidence and is **never consulted by a later admission, correlation,
replay, or bound decision**; the state the R1 ruling refused is state a later decision would have to
read, which is what made it accruable by a peer naming identities it never opens. Recording five
facts about one refused frame is not retention for the same reason recording three was not: nothing
reads them, and the bound the R1 ruling protects is on per-identity state a later decision consults,
not on the size of a write-once record. The distinction is
load-bearing in both directions and neither direction survives without it: a peer that names a
million unopened identities still costs the recipient no retained per-identity state, and `C4-P2`'s
first conjunct still has the recorded refusal it quantifies over. Abolishing the record instead of the
retention would leave the property with no witness at all.

Retaining nothing is what makes the `unseen` verdict bounded rather than merely
frameless — holding *any* per-identity state there, including a terminal record, would let a peer
accrue unbounded local state by naming identities it never opens, which is the exposure the 2026-08-13
R1 ruling refused. A later request bearing that identity therefore arrives at `unseen` as any other
first request does, and is admitted on its own merits; the earlier fault does not bar it, because a
refusal the recipient did not retain cannot bar anything. Under a conforming realization the sequence
never arises, and under a reordering one `C4-P2` has already gone red on the recorded refusal.

**Property C4-P1.** Across every C4 vector and within each session that vector carries, each accepted
terminal fact closes exactly one admitted interaction, no interaction identity is dispatched twice,
and the number of nonterminal interactions never exceeds that session's established finite bound.

**Property C4-P2.** Across every C4 vector, within one session and for each interaction identity,
nothing delivers two frames
one endpoint committed in an order that endpoint did not commit them in. Loss may still drop a frame.
Because the design refuses a reordered frame rather than accepting it, this is stated over the refusal
that reordering produces rather than over the accepted sequence, which no reordering can leave out of
order: no endpoint records a recipient `rejected-protocol` at `unseen` for a cancellation control
whose committing endpoint had already committed the request naming that identity **and whose
recipient afterwards admits an interaction for that identity in the same session**, and none records a
late-traffic `state-violation` latched against a frame whose committing endpoint had committed it
before that endpoint's own frame **the interaction's terminal history was accepted on**.

The subsequent admission in the first conjunct is what makes it decide reordering rather than loss,
and it is the 2026-08-14 owner ruling recorded in the redesign plan. Without it the conjunct is
satisfied by a fully conforming realization: the initiator commits the request, the transport **loses**
it, the initiator commits its one legal cancellation control — C8 states recipient admission is not
observable from `dispatched` — and the control lands at `unseen` and produces exactly this refusal.
That vector is a required member of the property's adversarial group, so the property would go red on
legal behaviour. Worse, a lost request and a reordered one present identical values in every other
field the property may read — same declared stimulus steps, same provenance, same detailed reason,
same refused frame kind, same `not-applicable` latch — so no carve-out written over those fields could
separate them, and declaring the loss vector green instead would leave the mutation green too. What
separates them is what happens next: a reordering delivers the request afterwards and the recipient
judges it on its own merits, as the retention passage below requires — the earlier refusal does not
bar it — so a conforming recipient admits it and the admission is recorded; a loss never delivers it
and no admission can occur at all.

**The two are not the same claim, and AH6 was reading one as the other.** The retention rule says the
request is *not barred*, not that it must be admitted, so a reordering whose displaced request is
refused on its own merits — an unknown Operation, a failed authority check, an exceeded bound — leaves
this conjunct green. That is a coverage limit rather than an unfalsifiable property: the named mutation
`C4-control-precedes-request` delivers a request that a conforming recipient admits, so the conjunct
still goes red on it, and the required-green set is unaffected. A reordering hidden behind an
independent refusal is not witnessed by this conjunct, and no artifact claims otherwise.

The conjunct reads **the recipient's subsequent admission of the refused identity** — named here rather
than left to the nearest antecedent, which the AH6 insertion moved and AI6 caught, and which is AC3's
defect in the paragraph AC3 was raised against — through a membership test
over the identities the recipient admits **in the same session**. The scope is the session and not
the vector: an interaction identity is unique within a session and a new session may legitimately
reuse the value, so a two-session vector could otherwise hold one identity refused at `unseen` in one
session and admitted in another, satisfy the test across them, and take the conjunct red on conforming
behaviour — AE1's own failure mode reached through the operand's scope instead of through a missing
clause. That is AF8, and the precedence relation W1 added carries the same qualifier for the same
reason.

**Both sides of that test publish the session, which is AK1 and is the half AF8 did not reach.** The
admitted side always did: C10 requires every attempted establishment and interaction to yield an
observation sufficient to distinguish session and interaction identities, and an admission is an
attempted interaction. The refused side did not, because C10's next paragraph places the `unseen`
refusal outside that class in terms — it is neither an attempted establishment nor an attempted
interaction — and the record it does require named only the refusal's provenance, its detailed reason,
and the kind of frame refused. A qualifier is worth nothing when only one of the two sets it scopes
carries the field, so the refusal record now carries the refused-frame reference and the paragraph
above says why. AF8, AH1, AI1 and AJ1 are four corrections that each added a session to something, and
AF8 is the one that added the *requirement* of a session without adding the session anywhere.

The conjunct itself now carries "in the same session" rather than deferring to this paragraph for it,
which is the second half of **AK7** and was found by the check written for it: a property whose scope
lives in the prose beside it is a property a vector author reads without the scope, and one artifact
describing another's qualifier is AG2's class.

**Required green.** `C4-P2` must not fail on a conforming realization. Its required vector group has
seven legal members and the set names all seven, because a member with no stated expectation is the
condition AE1 arose from: conforming commit-order delivery in the initiator direction; conforming
commit-order delivery in the recipient direction; a request **lost** while the cancellation control
naming its identity is delivered; a lost **acknowledgement**, the other half of "loss of either
frame"; a cancellation control for an identity the peer never opened; a legal late control arriving
after a peer's terminal; and a duplicate terminal from a nonconformant peer.

The lost request is the case the property was previously red on. The two conforming-delivery members
were the sharpest omission when AF5 was raised: a property that goes red on plain conforming delivery
is the worst failure available to it, and it was the one case the set did not name.

In both conjuncts the subject of "had already committed" and "had committed it" is the **committing
endpoint** — the endpoint that committed the frame the refusal names, which is never the endpoint that
records it. A recipient commits no requests and an initiator commits no acknowledgement its own latch
settles against, so reading the subject as the recording endpoint would make both conjuncts quantify
over an endpoint pair no vector can produce, which is a property that cannot fail. That is U1's defect
arriving through a pronoun, and it is why the subject is named here rather than left to the nearest
antecedent.

Restricting each conjunct to one endpoint's own frames is load-bearing: across
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
facts supplied by an owning extension, the terminal interaction's **late-traffic latch and the frame
that settled it**, the **terminal-frame reference** naming that interaction's own frame its terminal
history was accepted on, and effect certainty. Where a route reaches no terminal interaction the latch
is the explicit value `not-applicable`, which the observation carries like any other value; a route
with no latch and a latch that has not settled are different facts and an absent field would conflate
them.

The terminal-frame reference is required for the reason the settling frame's is, and is **AK6**:
`C4-P2`'s second conjunct compares those two frames against each other, and the terminal form alone
identifies one frame only while an endpoint commits at most one frame of that form for one identity —
which a duplicate terminal from a nonconformant peer, a required-green member of that property's own
group, is exactly the violation of. C10 states the fact and delegates the field list to the artifacts
that publish it, as it does for the settling frame.

**A recognized frame that opens no interaction yields one too.** A cancellation control or other
control naming an identity the recipient has never accepted is neither an attempted establishment nor
an attempted interaction — under C4 no interaction exists there — and it is refused as a peer
statement, so without this sentence the one record of that refusal would be required by C4 and by
nothing that owns observation. The observation records the refusal, the **refused-frame reference**,
and its provenance with the detailed reason `unopened-interaction-identity`; it retains no interaction
state, because there is none to retain. The reference names the kind of frame refused, because
provenance and detailed reason are identical for a cancellation control and for any other control
naming an unopened identity while `C4-P2`'s first conjunct quantifies over the cancellation control
alone — that much was AC2 — and it names the session, the interaction identity, the committing
endpoint and the arrival ordinal, which are **AK1** and **AK5**. Those four are the operands of the
conjunct's own qualifiers, and this paragraph is why they were absent: it places the record outside
the class the paragraph above requires session and interaction identities of, and then states what the
record does carry. An enumeration that excludes a record from the general rule owes that record its own
complete list.

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

**Property C11-P1.** In each session the vector carries, every established profile has all required
facets supported exactly, and no facet changes a core identity, authority, terminal-provenance, or
uncertainty result.

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
incorrect implementation, and every property **must not fail against a conforming realization**: it
carries a named set of legal inputs it must leave green, drawn from its own required vector group.

The second half is not the first half restated. A property that cannot fail and a property that
cannot stay green are the same defect measured from opposite ends — in both the verdict carries no
information about the behaviour the property names — but only falsifiability was ever written down as
a requirement, so ten review cycles audited for it and none audited the converse. `C4-P2`'s first
conjunct was red on a conforming realization through every one of them, and the vector it failed on
was already a required member of its own group with no stated expectation at all. A required-green set
that cannot be violated by any incorrect implementation is a finding against the set, the same way an
unfalsifiable property is a finding against the property.

**Facts a vector may hold more than one of.** AH1 settled that a vector **may carry more than one
session**. These facts belong to one session each, so a property that names one names the session it
means rather than counting or comparing it across the vector:

- `established profile` — each session the vector carries establishes its own;
- `interaction identity` — unique within one session, and a new session may legitimately reuse the
  value;
- `established finite bound` — declared by that session's own established profile;
- `nonterminal interactions` — counted against that session's own bound; and
- `session state` — each session the vector carries holds its own state and its own transitions
  through the session state machine, including its establishment, its **first drain transition**, and
  its terminal close or fault. This member is **AL3**, and the four above it were the four facts read
  by the five properties the AK pass found red — a class derived from the members that happened to be
  visible, which is AF6 one level up. It is not decorative: `S3` bounded admission by "the first drain
  transition" with no session named, and read across a vector that carries two sessions the property
  is red on a second session legally establishing and admitting after the first one drains. That is
  **AL1**, and no pattern built from the four members above could have matched it.

This is the same rule as the two above and it is stated for the same reason. AH1 gave the declared
stimulus step its session, AI1 and AJ1 gave the settling-frame reference its session across every
artifact that publishes it, and AK1 gave the recorded `unseen` refusal its session; none of those
reached the property *statements*, where `C4-P1` forbade an identity being dispatched twice, `C1-P1`
required exactly one established profile, and `I5` bounded concurrency against the established finite
bound, each without a session and each therefore red on a conforming two-session vector. A property
that counts a per-session fact per vector is AE1's defect reached through the quantifier instead of
through a clause, and the list above is declared rather than left to be inferred so that a fact added
to it is covered without a new check being written.

**This list is checked against the vector rather than against itself.** Every fact the neutral brief's
vector format distributes across "each session the vector carries" must appear above, so a fact the
vector holds per session cannot stay outside the audit's trigger set — which is exactly what happened
to the session's own state, and it is why AL1 survived a property audit that ran over all twenty-six
properties. What that check cannot do is prove the list total: a property may read a per-session fact
the vector format does not enumerate, and finding one is a finding rather than a typo. Two ways of
reading a property therefore both stay live — the declared facts it *names*, which the recognizer
audits, and the fact its subject *is*, which is why every property of the session state machine names
its session whether or not it mentions one of the facts above.

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
