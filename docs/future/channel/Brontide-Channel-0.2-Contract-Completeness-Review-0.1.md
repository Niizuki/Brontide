# Channel 0.2 contract-completeness and silence review 0.1

Date: 2026-08-11

Status: author pass plus B1-B4, N1-N3, F1-F3, D1-D5, T1-T4, R1-R3, S1-S3, U1/U4/U5/U7/W3, and X7
correction passes complete. The per-capability property audit now registers `C4-P2` and the mutation that must
fail it; its silence is why an unfalsifiable property survived the correction that introduced it. The
disposition history now runs to the eighth cycle rather than stopping at the fifth, and the in-flight
bound's direction scope is recorded as session-wide-as-written against per-direction-as-enforced
rather than as undeclared.
A fresh independent closure re-review remains required. This review asks what the proposed contract
does not say. It is separate from conformance review and does not claim the contract is correct.

Reviewed artifacts:

- [C1-C12 capability contract](./Brontide-Channel-0.2-Capability-Contract-0.1.md);
- [session state machine](./Brontide-Channel-0.2-Session-State-Machine-0.1.md);
- [interaction state machine](./Brontide-Channel-0.2-Interaction-State-Machine-0.1.md); and
- [responsibility matrix](./Brontide-Channel-0.2-Responsibility-Matrix-0.1.md).

## Method

For each capability the review looked for:

- an input, state, sequence, or perspective the contract leaves without an answer;
- two owners for one fact or no owner;
- a terminal path without effect certainty;
- a peer claim that could be manufactured locally;
- a future extension that would have to reinterpret a core identity or terminal fact; and
- a property that could pass without exercising the behavior it claims.

## Findings closed in the first-batch contract

### C1 — fixed establishment was an assertion, not an equivalence

Channel 0.1 allowed negotiation to be optional but did not define the complete record a fixed path
must establish. That permits fixed and negotiated realizations to agree on calls while disagreeing
on unsupported features, roles, limits, or authority mode. C1 now requires one canonical established
profile and byte/semantic equality between the two paths. A vector mutates one fixed-only fact.

### C2 — Ready cannot be both Channel and Composition state

The predecessor's global lifecycle treated Ready as a Channel state, while Architecture 0.8 makes it
the last Establishment stage of a Component group. Keeping both would allow a peer signal to create a
composition fact. The session machine removes Interconnection, Ready, Release, and binding withdrawal
from Channel state and consumes them only as explicit profile guard inputs.

### C2 — drain needed an answer for existing work

“No new requests after withdrawal” did not say whether admitted interactions finish, are cancelled,
or become unknown. Drain now freezes the admitted set, refuses new interactions, permits existing
ones to terminate, and faults if close arrives while they remain nonterminal.

### C3/C7 — relational initialization should not create a parallel invocation system

Decision 13 selected a new traffic capability, and its wording suggested a new envelope kind. A
dedicated kind would duplicate Operation, Capability, Shape, correlation, cancellation, terminality,
and observation. The contract instead makes it a distinct interaction class under the same machine,
with exact declaration and pre-Ready guard. This preserves the semantic ruling while changing its
representation.

### C4 — “single invocation” hid the future concurrency break

The predecessor fixed one active request and therefore never answered outcome ordering, drain with
siblings, replay reservation, or session failure across siblings. C4 supports a finite declared
bound and specifies independent terminal histories, out-of-order completion, atomic admission, and
drain/fault behavior. A profile may still select one.

### C4 — reconnect cannot inherit identity silently

Without an answer, a new transport could reuse a Channel identity and accidentally inherit replay or
in-flight state. Closed/faulted sessions never resume; reconnect creates a new session identity. A
future resumption contract must state durable identity and replay semantics explicitly.

### C5 — environmental limits could reject a negotiated profile invisibly

Channel 0.1 froze one limit set from two fixtures. A 0.2 implementation with a tighter environmental
limit might otherwise accept a profile and fail later. C5 requires every effective normative bound
to be exposed and accepted at establishment. Profile/resource limits stay with their owner.

### C6 — phase eligibility and delivery are not authority

A class admitted in the correct activation phase might be treated as authorized because the
composition root initiated it. C3 and C6 now require separate exact phase and local authority
decisions. Relational authority is narrow and does not flow into ordinary interaction.

### C7 — failed relational work cannot be projected as an incomplete “not Ready” alone

If an interaction may have performed effects and its Outcome is lost, merely withholding Ready loses
the cleanup evidence CM4 needs. C7 returns the actual terminal provenance and C10 certainty to the
composition owner; unknown remains unknown and rollback is not fabricated.

### C8 — cancellation acknowledgement is not terminal

Treating cancellation acceptance as terminal would claim the handler stopped and erase possible
effects. The interaction stays nonterminal until a semantic Outcome, peer fault, or local loss. The
contract also separates cancellation authority from invocation authority.

### C8 — terminal races require a first accepted history

Cancellation, timeout, peer fault, and Outcome can race. C8 now admits at most one terminal history;
late facts are recorded without replacing it. Endpoint perspectives may differ after transport loss,
so the contract does not invent a global winner neither endpoint can prove.

### C9 — unknown fault categories otherwise create fault loops

The 0.1 rule mapped an unknown error category to `unsupported-kind`, inviting the receiver to answer
an error with another error. C9 records local `unrecognized-peer-fault`, faults the session, and sends
no answering fault.

### C9 — process failure cannot be a peer message

The predecessor documented process categories beside envelope categories. The redesign gives local
loss a separate artifact and moves peer-unavailable to the launcher/binding owner when no Channel
session exists.

### C10 — a universal provider effect count is not a Channel fact

PB8 showed both stacks fabricated zero where the endpoint lacked evidence. Even a nullable count is
profile-specific: many Channel interactions have no meaningful universal “provider effect.” Channel
now owns effect certainty; Portable Binding or another profile owns counts/details. Known-none
requires proof before dispatch or explicit handler evidence.

### C11 — extension seams need invariants, not only non-promises

Saying streaming, retry, and delivery belong elsewhere did not prevent a later extension from
redefining correlation, terminality, or authority. C11 permits exact facets but forbids them to change
core identity, authority, provenance, and certainty. Retry is a new interaction identity with causal
attribution, never replay of the old one.

### C12 — capability properties need negative probes

A property can quantify all vectors and still assert something no implementation can violate. The
neutral brief requires one named mutation per property and retained failing output before the
property counts as evidence.

## Required silence probes and dispositions

| Probe | Contract answer | Owner if future behavior is added |
| --- | --- | --- |
| two interactions complete out of order | legal; independent terminal histories | Channel core |
| concurrency bound races at admission | reserve atomically; one refusal, no lasting replay entry | Channel core/runtime mechanism |
| direction scope of the in-flight bound | session-wide as written, per-direction as enforced: `C4-P1` bounds "the number of nonterminal interactions" and `I5` bounds "concurrency" with no direction restriction, which reads session-wide, while the only mechanism the design provides — the interaction machine's atomic one-position reservation at admission — is local and has no cross-endpoint coordination, so it can enforce only a per-direction count. The gap is unreachable in the only named profile, where one endpoint initiates both classes and the two readings coincide; a profile in which both endpoints initiate must state which it means before its vectors can be written | Channel profile + Batch 2 `established-profile` schema |
| cancel before dispatch | local refusal/no cancel frame; admission may itself be abandoned locally | Channel core |
| cancel during recipient admission | held, not faulted: exactly one control is retained while `validating` and applied when admission resolves; a refused admission discards it with no frame and does not fire the late-traffic latch | Channel core |
| loss or drain while a control is held | the third exit from `validating`: held control discarded with no answering frame, late-traffic latch does not fire, and the interaction reaches whatever terminal it would have reached with no control outstanding; an interaction still admitting is outside the drain snapshot | Channel core |
| control delivered before the request it names | impossible under C4 intra-interaction frame order, which core promises and a realization profile declares; `C4-control-precedes-request` exists as a mutation vector whose expected observation is the recipient's recorded `rejected-protocol` at `unseen`, which is the witness `C4-P2` fails on | Channel core + realization profile |
| cancel during possible effects | accepted/refused nonterminal ack; final Outcome/fault/loss required | Channel + Operation profile |
| cancel after terminal | late control; terminal history unchanged | Channel core |
| drain with in-flight work | no new admission; existing work terminates | Channel core |
| peer closes before drained work ends | session fault; each interaction local loss/unknown as applicable | Channel core/local observer |
| half-close | loss unless a future exact simplex profile exists | transport profile/future Channel version |
| partial frame | no interaction; bounded local/peer structural failure | realization + Channel provenance |
| oversized profile declaration | establishment refusal, known-none | Channel profile validator |
| allocation failure | sanitized local loss/fault at exact stage | local realization |
| replay after semantic failure | replay remains replay; no redispatch | Channel core |
| retry after local loss | new identity/attempt; old remains lost | Distributed/host facet |
| unknown optional extension | ignored only with declared additive absence rule | Channel profile |
| downgrade request | refused; no nearest-version selection | Channel core |
| authority revoked between profile establishment and dispatch | local authority re-evaluated per interaction; deny | authority domain |
| authority revoked after dispatch | Channel records resulting evidence; no implicit cancellation/rollback | authority/Lifecycle/Operation owner |
| malformed/mismatched terminal after dispatch | terminal not accepted; local loss/fault with unknown effects | Channel core |
| resource held when transport is lost | Channel reports loss; profile/resource owner cleans up or records inability | Portable Binding/Resource |
| stream wants several partial results | requires Flow/profile facet; cannot call partial values Outcomes | Flow |
| durable delivery repeats attempt | new interaction identity and causal link; no exactly-once claim | Distributed |
| long-running work outlives interaction | semantic Outcome may hand off an Activity reference under Lifecycle facet | Lifecycle |
| reconnect after fault | new session identity; no inherited replay/in-flight state | future resumption contract |

## Per-capability property audit

| Capability | Universal property present | Named mutation that must fail |
| --- | --- | --- |
| C1 | C1-P1 exact profile or known-none | remove one required facet from fixed profile only |
| C2 | C2-P1 legal transition/terminal monotonicity | accept new interaction while draining |
| C3 | C3-P1 exact class/direction/phase | mark unknown phase as true |
| C4 | C4-P1 one dispatch/terminal and bounded concurrency; C4-P2 intra-interaction frame order, one conjunct per direction | redispatch replayed identity or exceed bound; `C4-control-precedes-request` delivers one interaction's control before the request that opens it, and `C4-outcome-precedes-ack` delivers the recipient's Outcome before the acknowledgement it committed first — one named mutation per conjunct, because half a property with no mutation is half unfalsifiable |
| C5 | C5-P1 all positional/bound checks before dispatch | project an authority value or dispatch oversized payload |
| C6 | C6-P1 exact permitted local authority | treat compatibility/delivery as permission |
| C7 | C7-P1 exact declaration/pre-Ready/no phase creation | admit wrong edge or let success create Ready |
| C8 | C8-P1 one terminal; controls never success | make cancel acknowledgement terminal success |
| C9 | C9-P1 exactly one provenance form | map local loss to peer fault |
| C10 | C10-P1 no unsupported known-none after dispatch | replace unknown effect certainty with known-none |
| C11 | C11-P1 required facets exact/core invariants stable | extension changes interaction identity or authority result |
| C12 | C12-P1 deterministic data and independent runtimes | remove a property group or add stack dependency |

## Deliberate non-goals confirmed

- The contract does not choose a wire encoding or transport.
- It does not define streaming, backpressure, durable delivery, ordering, resumption, persistence, or
  exactly-once effects.
- It does not define cross-domain identity, attestation, or Capability transport.
- It does not define resource lifetime, release, fallback, or universal effect details.
- It does not define Component resolution, activation, Ready, Release, cleanup, or rollback.
- It does not standardize logging, metrics, tracing, or clocks.

Each non-goal has an owner or requires a future Channel version. None is represented as an
implementation-defined hole inside a core C-item.

## Residual review risks

These are not unowned contract holes, but the independent reviewer must challenge them:

1. Supporting optional cancellation in core may be more surface than the first profile can evidence;
   the reviewer should test whether reserving the semantics without implementing a positive path
   creates an unfalsifiable capability.
2. Bounded concurrency greater than one is intended to prevent another foundational break; the
   reviewer should test whether it accidentally imports scheduling or ordering promises. On the
   bound's direction scope the eighth review answered the question this risk used to ask: one count
   does already imply a scope. `C4-P1` and `I5` read session-wide and the reservation mechanism can
   enforce only per-direction, so the two disagree the moment a profile lets both endpoints initiate.
   No such profile exists, so no vector can falsify either reading today. The reviewer should test
   whether recording the disagreement is still the right disposition, or whether core must pick a
   scope before a second initiating direction is designed rather than after.
3. The external phase predicate may be too generic and permit profiles to smuggle arbitrary policy
   into Channel. Batch 2 must keep it a small, closed fact/predicate form.
4. Effect certainty `known` with profile-owned details needs a precise neutral representation that
   does not let a peer claim become locally verified evidence.
5. The peer/local structural-error boundary depends on whether a complete frame reached the peer;
   process vectors must force both perspectives.

## Review disposition

The first independent review at `66729b097b032febf498dd907dd2387e2aebc2c5` refuted the original
author-pass conclusion and recorded four blockers in the retained
[design-foundation attestation](./reviews/channel-0.2-design-foundation-attestation.md):

1. recipient local authority denial could become a peer protocol statement;
2. recipient cancellation refusal had no producer transition;
3. responsibility rows did not select exactly one semantic owner; and
4. 13 predecessor-vector rows used dispositions outside the declared vocabulary.

The correction pass separates authority structure from the local authority decision and adds a
frameless recipient `refused-local` path; adds the nonterminal cancellation-denial/`refused`
acknowledgement transition; assigns one exact owner identifier per responsibility row; and maps every
vector disposition to the declared five-value vocabulary. The strengthened design verifier pins all
four corrections and was observed failing on the pre-correction artifacts.

This correction does not close the first batch. A fresh reviewer must assess the corrected commit,
the original findings, current Architecture 0.8, predecessor evidence, Decision 13, every property
and state transition, the responsibility matrix, this silence review, the migration ledger, and the
neutral brief. No schema or public surface may be created until a closure attestation conforms.

The first closure review at `e863bf15fca30466d6e262b0ea66b3c05bc384eb` closed B1-B4 but recorded
three new blockers in the retained
[closure attestation](./reviews/channel-0.2-design-foundation-closure-attestation.md). N1 is corrected
by carrying the exact Ready owner consistently: Portable Binding owns Interconnection, Release,
withdrawal, and cleanup; Composition owns the Relational Initialisation phase; Component Management
owns Ready. N2 is corrected by accepting a peer fault from `cancel-pending` and making invalid
cancellation control an interaction-scoped recipient fault with post-dispatch uncertainty. N3 is
corrected by using the declared `retained` disposition while describing the three retained
non-promises in the treatment column. The verifier was extended first and observed all N1-N3 checks
failing against the pre-correction artifacts.

These corrections likewise require a fresh independent final closure review and do not authorize
Batch 2 by themselves.

The next closure review at `1af7ba0018c874750e346ee687f07ea1d302adef` closed B1-B4 and N1-N3 but
recorded F1-F3 in the retained
[final closure attestation](./reviews/channel-0.2-design-foundation-final-closure-attestation.md).
F1 is corrected by defining a repeated accepted identity during `executing` or `cancel-requested` as
one interaction-scoped `replay-detected` peer fault with no redispatch and a late-handler terminal
ignored. F2 is corrected by replacing recipient `faulted` with distinct `peer-fault` and `lost`
terminal states and assigning each an exclusive provenance row. F3 is corrected by mapping the
added cancellation Outcome through the declared `replaced` disposition. The verifier now checks
the live replay/provenance paths and every bold disposition row in the complete ledger; it was
observed failing on F1-F3 before correction.

These corrections require a new independent definitive closure review and do not authorize Batch 2
by themselves.

The definitive review at `1b7c5fdea0dc555a64152eea055fcebad053cf90` closed every retained finding
but recorded D1-D5 in the retained
[definitive closure attestation](./reviews/channel-0.2-design-foundation-definitive-closure-attestation.md).
D1 makes duplicate drain a fatal session-scoped `state-violation` with the original snapshot and
interaction evidence preserved. D2 records accepted/refused cancellation acknowledgements in
distinct states and faults unsolicited, duplicate, or contradictory acknowledgement/control. D3
routes a receiver-local false/unknown external phase to frameless `refused-local`. D4 adds one finite
late-traffic-fault latch and exactly one possible peer-fault emission without replacing the first
terminal. D5 moves the predecessor delivery-fallback observation to its owning delivery/retry facet.

The new [state/event coverage grid](./Brontide-Channel-0.2-State-Event-Coverage-0.1.md) closes the
event domain across every session, initiator, recipient, and terminal state. The verifier was
extended before correction and observed D1-D5 plus the missing grid fail.

The totality review at `5cf42c4d97083324ffb8d6bd68491a145b8e611a` closed every retained finding
through D1-D5 and confirmed the closed event domain by independent enumeration, but recorded one
blocking and three nonblocking findings in the retained
[totality closure attestation](./reviews/channel-0.2-design-foundation-totality-closure-attestation.md).
T1 is corrected by removing the migration ledger's permission for a peer fault on an external phase
refusal, which contradicted the D3 correction it was part of; the fault is now stated as never
applying to a phase predicate. T2 is corrected by binding `replay-detected` to the nonterminal window
and naming the late-traffic latch for a repeat after terminal. T3 is corrected by giving a `cancelled`
terminal with no cancellation request in force one explicit result — an interaction-scoped
`internal-channel-failure` at the recipient and a peer fault at the initiator — instead of leaving it
to the wrong-state catch-all, which would have left a finished handler's interaction nonterminal
until loss. The finding named the recipient's `cancel-refused` state; the correction covers the class,
because `executing` with no cancellation control at all, and the initiator receiving such an Outcome,
are the same contradiction and were equally unrouted. T4 is corrected by giving every first-batch status block one
stable phrase for the review it awaits.

The escalating cycle adjectives are themselves the cause of T4: four successive reviews were named
"closure", "final closure", "definitive closure", and "totality closure", and three status blocks
were left pointing at a cycle that had already run. The verifier now pins one stable phrase and
rejects the superseded names in a status block, so the next cycle cannot repeat the drift.

The closure re-review at `11ba93bddbd38f03df59b4afc5166d7c6991c865` closed T1-T4 but recorded R1-R3 in
the retained
[closure re-review attestation](./reviews/channel-0.2-design-foundation-closure-re-review-attestation.md).
R1 is corrected under the 2026-08-13 ruling that a cancellation control racing recipient admission is
held rather than faulted: the recipient retains exactly one control while `validating` and applies it
when admission resolves. R2 is corrected by stating that the two endpoint preconditions are local and
that no event synchronises them. R3 is corrected by giving `unseen` and `validating` separate recipient
grid rows, because a control correlates against a known identity in one and not the other. That
attestation's isolation is partial and it says so; it established R1 but could not have closed the
batch.

The seventh review at `3892c23a8dd4c7f298e877ba73710ee0ddc97bc4` re-verified R1-R3 individually and
recorded S1-S3 in the retained
[closure review 7 attestation](./reviews/channel-0.2-design-foundation-closure-review-7-attestation.md).
S1 — the R1 correction kept `rejected-protocol` at `unseen` while the fact making that sound was
asserted in the state/event grid alone, disclaimed by C4 and C11 and assigned to `delivery-facet` by
the responsibility matrix — is corrected under the 2026-08-13 S1 ruling giving intra-interaction frame
order an owner. S2 added the `validating` loss and drain rows and reconciled the pre-dispatch loss rule
to any nonterminal state. S3 was index and status staleness, closed in the commit that recorded the
review.

The eighth review at `3b27e3a85bf018bead6d226a13d075c7e6ed16fa` verified every retained finding through
S1-S3 closed individually and recorded U1-U8 in the retained
[closure review 8 attestation](./reviews/channel-0.2-design-foundation-closure-review-8-attestation.md).
It found S1 closed as to ownership but not as to falsifiability: `C4-P2` quantified over the frames a
recipient *accepts*, the design refuses every reordered frame, so the accepted sequence was empty and
the property stayed green on its own named mutation. U1 is corrected by restating `C4-P2` over the
refusal a reordering produces, with each conjunct restricted to one endpoint's own frames. U5 is
corrected in this table, U6 by rewriting the review policy's pin clause, and U2, U3, U7, and U8 in the
artifacts they were raised against. The subsequent
[U1 correction iteration review](./reviews/channel-0.2-u1-correction-iteration-review.md) then found V1
and V2, both of which would have left the corrected property unfalsifiable in practice: the parity
profile compared only the peer-fault category, and no endpoint was authorised to inject the reordering
the mutation needs. **V3** was raised in the same pass and deliberately not corrected: V1 and V2 were
U3 being paid down one forced instalment at a time rather than dispositioned, and how much of the
S1/U1 obligation the brief must carry before Batch 2 is an owner call about Batch 2's scope. U3 was
subsequently corrected in full, which is V3's disposition.

Two further author-side passes over those corrections raised **W1**-**W6**, and a third raised
**X1**-**X7**; the retained record of both is the
[W correction iteration review](./reviews/channel-0.2-w-correction-iteration-review.md). W1 gave the
closed property operator set the bounded precedence relation `C4-P2` needs, W2 stated what the
reordering provider declares at establishment, W3 added a second named mutation so each conjunct has
one, W4 stated that an identity refused at `unseen` retains no history and no latch, W5 gave the
precedence operator its operand in the vector format, and W6 added the late-traffic latch to the
normative parity comparison.

X1-X7 are the layer beneath those. **X1** — the parity profile compared the latch *value*, and the
conjunct that motivated W6 reads the frame the latch settled against; the mutation and the two cases
the property must leave green all record `state-violation` with `fault-committed`, and
`state-violation` declares no detailed-reason set for V1's clause to reach. The settling frame is now
recorded and compared. **X2** — W4 created a route with no latch while the grid requires every
generated cell to assert one; the absence is now an explicit `not-applicable` value rather than an
absent field. **X3** — the recipient transition table, which is the detailed authority, had no row for
a control at `unseen`, so the machine's own totality rule produced a terminal `peer-fault` with a
latch; the row now exists. **X4** — `C4-outcome-precedes-ack` was in no required adversarial vector
group, so half of W3 did not reach the suite. **X5** — `C4-P2`'s first conjunct quantifies over a
record W4 said the recipient does not keep; recording evidence is now distinguished from retaining
state, in C4, the provenance table, and the grid. **X6** — the pin clause closing U6 went stale one
commit later and is now checked against the repository rather than against its own wording. **X7** —
the W passes left no retained iteration review, and V3's disposition was unrecorded; both are closed
here and by a check written over the class.

A fourth pass over the X corrections raised **Y1**-**Y4**, recorded in the same iteration review.
**Y1** — W6 and X1 made the late-traffic latch and its settling frame normative comparisons, and
neither C10's enumeration nor the brief's local-observation schema carried either, so the parity
profile compared two fields no observation was required to hold. **Y2** — C10 requires an observation
for every attempted establishment and interaction, and the `unseen` refusal is neither, so the record
X5 depends on was mandated by the capability that reads it and by none that owns it. **Y3** — X3
routed the refusal to `rejected-protocol`, which the recipient state table marks terminal and the
`any terminal` rows therefore claim, reintroducing the latch W4 refuses; the recipient's per-identity
state remains `unseen` and `rejected-protocol` is the provenance. **Y4** — the settling-frame
reference named kind, identity, and committing endpoint, which do not separate two frames of the same
kind from one endpoint; a duplicate terminal is exactly that and must leave `C4-P2` green, so the
reference now carries the frame's arrival ordinal.

These changes still need a fresh independent closure re-review and do not authorize Batch 2
themselves.
