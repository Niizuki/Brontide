# Channel 0.2 contract-completeness and silence review 0.1

Date: 2026-08-11

Status: proposed first-batch completeness and silence review; awaiting a fresh independent
closure re-review, on hold under the owner decision of 2026-08-17 recorded in the
[verification foundation plan](./Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md).
Correction history is not carried here; it is owned by the
[disposition index](./reviews/channel-0.2-disposition-index.md#contract-completeness-review).

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
| direction scope of the in-flight bound | session-wide as written, per-direction as enforced: `C4-P1` bounds "the number of nonterminal interactions" and `I5` bounds "concurrency" with no direction restriction, which reads session-wide, while the only mechanism the design provides — the interaction machine's atomic one-position reservation at admission — is local and has no cross-endpoint coordination, so it can enforce only a per-direction count. The gap is unreachable in the only named profile, where one endpoint initiates both classes and the two readings coincide; a profile in which both endpoints initiate must state which it means before its vectors can be written. Under **AK7** both properties now say *session-wide* in terms — `C4-P1` bounds "the number of nonterminal interactions" against "that session's established finite bound", and `I5` bounds concurrency "within each session the vector carries" — which settles the vector-versus-session question AH1 opened and leaves this direction question exactly where it was. **Under AE3 this is a known conforming-realization exposure, not merely an undeclared scope.** The disposition was made when C12 required only that a property be able to fail; AE3 added the converse, and under it a realization enforcing exactly what the design provides may take `C4-P1` and `I5` red in a both-endpoints-initiating profile. Their required-green cells read `owed` until the sets were filled. Both are now stated and scoped to the one named profile, where the two readings coincide; they do not settle the direction scope for a both-endpoints-initiating profile, and a pass that filled them without saying so would have reproduced the omission. That connection was absent until AH5 | Channel profile + Batch 2 `established-profile` schema |
| cancel before dispatch | local refusal/no cancel frame; admission may itself be abandoned locally | Channel core |
| cancel during recipient admission | held, not faulted: exactly one control is retained while `validating` and applied when admission resolves; a refused admission discards it with no frame and does not fire the late-traffic latch | Channel core |
| loss or drain while a control is held | the third exit from `validating`: held control discarded with no answering frame, late-traffic latch does not fire, and the interaction reaches whatever terminal it would have reached with no control outstanding; an interaction still admitting is outside the drain snapshot | Channel core |
| control delivered before the request it names | impossible under C4 intra-interaction frame order, which core promises and a realization profile declares; `C4-control-precedes-request` exists as a mutation vector whose expected observation is the complete record set both endpoints produce — the recipient's recorded `rejected-protocol` at `unseen` **and its subsequent admission of that identity when the displaced request arrives**, which together are the witness `C4-P2` fails on. The refusal alone is not the expectation: the first conjunct reads the admission too, and a vector authored without it takes the property green on this very mutation, which is AG1 | Channel core + realization profile |
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

| Capability | Universal property present | Named mutation that must fail | Required-green inputs |
| --- | --- | --- | --- |
| C1 | C1-P1 exact profile or known-none | remove one required facet from fixed profile only, executable as `C1-required-facet-removed-from-fixed-profile` | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for |
| C2 | C2-P1 legal transition/terminal monotonicity -- three clauses, and this cell named the middle one only until **AU1** | accept new interaction while draining, executable as `C2-accept-interaction-while-draining`, which fires through the middle clause; an accepted transition the legal table does not contain, executable as `S1-illegal-transition-accepted`, which fires through the first; and a session that resumes after `closed` on an edge the table does contain, executable as `C2-terminal-session-resumed-legal-edge`, which fires through the third -- one named mutation per clause, the rule C4's row states. The first is the input `S1`'s own row names, because C2-P1 is evaluated through S1 and S4 rather than restating them. The third needed a vector of its own: no legal edge leaves a terminal state, so S4's own mutation also violates the legal table and the first clause returned before the third was reached. **AU1**: both were deletable outright with every gate green | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for |
| C3 | C3-P1 exact class/direction/phase -- two obligations, and this cell named the phase one only until **AU1** | mark unknown phase as true, executable as `C3-unknown-phase-marked-true`, which fires through the phase predicate; and dispatch an interaction whose class and direction do not match its session's established profile, executable as `C3-profile-mismatch-on-dispatch`, which fires through the obligation read before it. **AU1**: the profile-match obligation had no input that reached it and could be deleted outright with every gate green | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for |
| C4 | C4-P1 one dispatch/terminal and bounded concurrency; C4-P2 intra-interaction frame order, one conjunct per direction | redispatch replayed identity or exceed bound; `C4-control-precedes-request` delivers one interaction's control before the request that opens it, and `C4-outcome-precedes-ack` delivers the recipient's Outcome before the acknowledgement it committed first — one named mutation per conjunct, because half a property with no mutation is half unfalsifiable | `C4-P2`, all seven legal members of its required vector group: conforming commit-order delivery in the initiator direction; conforming commit-order delivery in the recipient direction; a request lost while the control naming its identity is delivered; a lost acknowledgement; a control for an identity the peer never opened; a legal late control after a peer's terminal; a duplicate terminal from a nonconformant peer. The lost request is AE1 — a required member carrying no expectation that the property was red on. This cell named four of the seven until AH2: AF5 corrected the set in the contract and the brief and not here, and this is the artifact Batch 2 authors the property file from. `C4-P1`: the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains, each with the expectation that evaluating the property over it returns green, and all three clauses executable with one named mutation apiece -- `C4-redispatch-replayed-identity`, `C4-bound-exceeded` and `C4-terminal-closes-two-interactions`, the third added because the contract names mutations for two clauses only. The set is scoped to the one named profile, and see the direction-scope disposition in the silence-probe table above before filling it — under AE3 it is a known conforming-realization exposure, not merely unwritten, and under **AK7** its second and third clauses are session-scoped, so a set filled in against the pre-AK7 wording would have named a two-session member the property was red on. This cell was the twelfth capability owing a required-green set, and the count of cells reading `owed` outright was eleven while twelve were owed, which is **AK3**. Every capability cell is now filled against an executable evaluator, so no capability owes one |
| C5 | C5-P1 all positional/bound checks before dispatch, and `known-none` on every pre-dispatch structural refusal -- this cell named the first clause only until **AR1**, in the artifact Batch 2 authors the property file from | project an authority value or dispatch oversized payload, executable as `C5-oversized-payload-dispatched`, which fires through the first clause; and a pre-dispatch structural refusal recording an effect certainty other than `known-none`, executable as `C5-pre-dispatch-refusal-possible-effect`, which fires through the second; and an interaction dispatched having passed every declared bound and not every positional Shape rule, executable as `C5-positional-shape-unchecked`, which fires through the second obligation of the first clause -- one named mutation per clause, and for the first clause one per obligation, because half a property with no mutation is half unfalsifiable, which is the rule C4's row above already states. **AR1**: no declared vector carried a pre-dispatch refusal at all, so the second clause could be deleted outright with both gates green. **AU1**: the first clause states two obligations and the AR1 mutation fails a declared bound and returns, so the positional Shape obligation beside it was still deletable with every gate green -- AR1's own finding one level below the clause it was raised against | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for |
| C6 | C6-P1 exact permitted local authority, and the decision point, initiator attribution and `known-none` on every denial or unevaluatable presentation -- this cell named the first clause only until **AR1** | treat compatibility/delivery as permission, executable as `C6-delivery-treated-as-permission`, which fires through the first clause; and a local denial that omits its decision point, executable as `C6-denial-without-decision-point`, which fires through the second; a denial that omits its initiator attribution, executable as `C6-denial-without-initiator-attribution`; and a denial recording a possible effect, executable as `C6-denial-with-possible-effect` -- one named mutation per clause, and for this clause one per obligation, because the clause requires three things and reads them as a disjunction of omissions. **AR1**: the only non-permitted decision in the corpus was also dispatched, so the first clause returned first and the second had no input that reached it. **AT2**: the AR1 mutation then omitted the first obligation, so it returned before the other two operands were evaluated and each of them could still be deleted with every gate green | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for |
| C7 | C7-P1 exact declaration/pre-Ready/no phase creation -- three obligations, and this cell named the third only until **AU1** | admit wrong edge or let success create Ready, executable as `C7-success-creates-ready`, which fires through the third; a dispatched relational interaction matching two lifecycle declarations, executable as `I6-relational-matches-two-declarations`, which fires through the first; and one that does not occur in the pre-Ready window, executable as `C7-relational-outside-pre-ready-window`, which fires through the second -- one named mutation per obligation. The first is I6's input, because the two obligations say the same thing about the same field, on the precedent I4's row states. **AU1**: the two obligations read before the declared mutation's had no input that reached them | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for |
| C8 | C8-P1 one terminal; controls never success -- two clauses, and this cell named the second only until **AU1** | make cancel acknowledgement terminal success, executable as `C8-acknowledgement-recorded-as-success`, which fires through the second clause; and an accepted interaction with two terminal histories, executable as `I2-two-terminal-histories`, which fires through the first -- one named mutation per clause. C8-P1 is evaluated through I2 and I3 rather than restating them, so the first clause's input is the one I2's own row names. **AU1**: the first clause had no input that reached it and could be deleted outright with every gate green | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for |
| C9 | C9-P1 exactly one provenance form -- two obligations, and this cell named the second only until **AU1** | map local loss to peer fault, executable as `C9-local-loss-mapped-to-peer-fault`, which fires through the second; and select a provenance form outside the four the design closes over, executable as `C9-provenance-form-outside-the-four`, which fires through the first. **AU1**: the closed-set obligation is read first and returns, and no input reached it, so it could be deleted outright with every gate green | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for |
| C10 | C10-P1 every observation complete for its provenance form, and no unsupported known-none after dispatch -- this cell named the second clause only until **AT3** | replace unknown effect certainty with known-none on the refusal, executable as `C10-known-none-after-dispatch`; and the same fabricated zero on a terminal history, executable as `C10-known-none-terminal-history`; and an observation that is not complete for its provenance form, executable as `C10-observation-incomplete`, which fires through the completeness obligation read before both of them. **AT3**: the refusal is read first and returns, so the terminal-history obligation had no input that reached it. **AU1**: both of those mutations fabricate a `known-none` certainty and the completeness obligation is read before either, so it too had no input that reached it -- AT3's finding one obligation further up the same evaluator | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for |
| C11 | C11-P1 required facets exact/core invariants stable -- two obligations, and this cell named the second only until **AU1** | extension changes interaction identity or authority result, executable as `C11-facet-changes-interaction-identity`, which fires through the core-invariant obligation; and a session requiring a facet its established profile does not support, executable as `C11-required-facet-unsupported`, which fires through the obligation read before it. **AU1**: the facet-support obligation had no input that reached it and could be deleted outright with every gate green | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for |
| C12 | C12-P1 deterministic data and independent runtimes | remove a property group or add stack dependency, executable as `C12-nondeterministic-expected-observation` | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for |

### State-machine properties

C12's soundness rule is written over **every** property, and the table above covers the thirteen
capability-wide ones in twelve rows, because the `C4` row carries `C4-P1` and `C4-P2` in one cell.
That difference is **AK3**: three surfaces reported the package as stating twenty-five properties, all
three were counting audit rows, and the property the count dropped is `C4-P2` — the one fifteen cycles
of this programme have been about. The two state machines state thirteen more under that same heading,
and AF7 was
that the rule was visible over less than half the properties the package states. They are audited here
under the same three obligations, in this section rather than a separate one, because a rule enforced
over the surfaces one audit happens to enumerate is the mechanism AF7 and AE4 share.

| Property | Statement | Named mutation that must fail | Required-green inputs |
| --- | --- | --- | --- |
| S1 | in each session the vector carries, every accepted transition of that session is in the legal table | named negative probe in the neutral verifier, now named and executable as `S1-illegal-transition-accepted` | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for, and a property reading one session's fact across the vector is green on the first and red on it |
| S2 | no interaction dispatches outside its own session's `established` state | named negative probe in the neutral verifier, now named and executable as `S2-dispatch-while-draining` | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for, and a property reading one session's fact across the vector is green on the first and red on it |
| S3 | within each session the vector carries, no new interaction is admitted after that session's first drain transition | named negative probe in the neutral verifier, now named and executable as `S3-admit-after-drain` | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for, and a property reading one session's fact across the vector is green on the first and red on it and the session scope is **AL1**: the property bounded admission by "the first drain transition" with no session named, which is not a fact of a vector that carries two, so a second session establishing and admitting after the first one drained took it red on conforming behaviour. The AK audit reported `S1`-`S6` clean because its trigger set was C12's declared fact list and the session's own state was absent from it (**AL3**) |
| S4 | within each session the vector carries, a terminal session never becomes nonterminal or resumes under the same session identity | named negative probe in the neutral verifier, now named and executable as `S4-terminal-session-resumed` | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for, and a property reading one session's fact across the vector is green on the first and red on it |
| S5 | for each session the vector carries, fixed and negotiated establishment of that session's own declared profile produce equal normative profile records | named negative probe in the neutral verifier, now named and executable as `S5-fixed-and-negotiated-differ` | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for, and a property reading one session's fact across the vector is green on the first and red on it and the qualifier is **AL4**: `established profile` is a declared per-session fact, so a property comparing "equal normative profiles" across the vector is red on two sessions that legitimately declare different ones. It is the correction `C1-P1` received under AK8, one artifact over |
| S6 | in any session the vector carries, no session event creates Ready, Release, authority, or an application Outcome | named negative probe in the neutral verifier, now named and executable as `S6-session-event-creates-outcome` | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for, and a property reading one session's fact across the vector is green on the first and red on it |
| I1 | one interaction identity crosses the dispatch boundary at most once per session | `I1-identity-dispatched-twice`, executable in `build/verify-channel-0.2-properties.ps1` | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for, and a property reading one session's fact across the vector is green on the first and red on it |
| I2 | every accepted interaction has at most one terminal history | `I2-two-terminal-histories`, executable in `build/verify-channel-0.2-properties.ps1` | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for, and a property reading one session's fact across the vector is green on the first and red on it |
| I3 | no cancellation acknowledgement, drain event, timeout, or protocol fault becomes a semantic Outcome | `I3-acknowledgement-as-success`, executable in `build/verify-channel-0.2-properties.ps1` | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for, and a property reading one session's fact across the vector is green on the first and red on it |
| I4 | every pre-dispatch refusal is `known-none`; every possible post-dispatch loss is `unknown` -- this cell named the second clause only until **AT1** | `I4-post-dispatch-loss-known-none`, which fires through the second clause, and `C5-pre-dispatch-refusal-possible-effect`, which fires through the first -- one named mutation per clause, both executable in `build/verify-channel-0.2-properties.ps1`. The first-clause mutation is the input `C5-P1`'s second clause already used, because the two clauses say the same thing about the same record. **AT1**: no vector in this property's group carried a pre-dispatch refusal at all, so the first clause could be deleted outright with both gates green | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for, and a property reading one session's fact across the vector is green on the first and red on it |
| I5 | concurrency within each session the vector carries never exceeds that session's established finite bound under any generated interleaving | `I5-concurrency-exceeds-bound`, executable in `build/verify-channel-0.2-properties.ps1` | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for, and a property reading one session's fact across the vector is green on the first and red on it and under AE3 a **known** conforming-realization exposure rather than merely unwritten: the direction-scope disposition in the silence-probe table above records that `I5` bounds concurrency with no direction restriction while the only mechanism the design provides enforces per-direction, so a both-endpoints-initiating profile may take it red on a conforming realization. AH5 recorded this against `C4-P1` and not against `I5`, which its own evidence named alongside it; that is AI3. The session scope is **AK7**: the bound belongs to one session's established profile and this property counted across the vector, so a conforming two-session vector took it red for a second and unrelated reason |
| I6 | a relational interaction matches exactly one declaration and never creates Ready/Release -- two obligations, and this cell named the second only until **AU1** | `I6-relational-creates-ready`, which fires through the Ready/Release obligation, and `I6-relational-matches-two-declarations`, which fires through the declaration count read before it, both executable in `build/verify-channel-0.2-properties.ps1` -- one named mutation per obligation. **AU1**: the declaration-count obligation had no input that reached it and could be deleted outright with every gate green | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for, and a property reading one session's fact across the vector is green on the first and red on it |
| I7 | a terminal fact for one interaction changes no sibling interaction's terminal history | `I7-sibling-terminal-history-changed`, executable in `build/verify-channel-0.2-properties.ps1` | the conforming single-session realization, and two sessions conforming, the second establishing and admitting after the first drains -- the second is the member AL1 and AK7 were raised for, and a property reading one session's fact across the vector is green on the first and red on it |

The two machines are not in the same state. The session machine commits each of `S1`-`S6` to a named
negative probe in the neutral verifier, so those six satisfy C12's falsifiability half and owe only
the required-green half. The interaction machine's `I1`-`I7` section carries no evidence sentence at
all, so those seven **satisfy neither half** — which was a larger gap than the eleven `owed` cells above, and was recorded here rather than left to be discovered a third time. Both halves are now closed: each of `I1`-`I7` carries a named mutation and a required-green set, and all seven execute.

`I1`-`I7` largely restate the C-properties at interaction scope and the C-properties carry the
normative weight, which is why AF7 was nonblocking. That is a reason to sequence this work, not to
leave it uncounted.

### Why so many cells read `owed`, and what filled them

The third column is the AE3 correction. Eleven of its twelve cells said `owed` rather than a plausible-looking set, and that was deliberate. C12 now requires every property to carry a required-green
set drawn from its own vector group, and no artifact currently states one for any capability but C4 —
so filling these in from this desk would be inventing the expectations, which is the failure mode
AGENTS.md names when it says a test written against a finished implementation asserts what the code
does rather than what it should do. `owed` is a checkable claim about the design's present state; a
guessed set is not, and would close AE3 in appearance while leaving exactly the gap AE1 fell through.

**Batch 2 cannot author `capability-properties.json` until these are stated**, because the property
format now lists the required-green set as a normative field. Deriving each from its capability's
required vector group is bounded work, and it is the first thing the next correction pass should take
if the closure reviewer does not raise it first.

## `C4-P1` and `C4-P2` operand enumeration

Four families in a row were one shape: **an operator qualifier whose operand is not published by the
record it reads.** W5 was the precedence relation with no committing endpoint on the declared stimulus
step; AH1 was the same relation with no session on it; AI1 and AJ1 were the settling-frame reference
with no session, in three publishing artifacts and then in two more; and **AK1** was AF8's membership
scope over a refusal record that named neither the session nor the identity. Each was found by
sampling one operand, and each cycle found one more.

This table ends the sampling. It lists **every fact the two properties read**, the scope each clause
claims over it, every artifact and section that publishes it, and whether what is published is enough
to evaluate the clause at that scope. It is written here so the next cycle can check it rather than
rediscover it, and the design verifier pins it: every declared frame reference and every fact C12
declares a vector may hold more than one of has a row, no row may read `insufficient`, and every row
names a publishing surface that resolves to an artifact.

Three things were `insufficient` when the enumeration was made, and all three were corrected in the
commit that made it — **AK1** and **AK5** on the first conjunct's operand, **AK6** on the second
conjunct's second operand. The enumeration also found the same shape one level up, in the properties'
own quantifiers, which is **AK7** and **AK8** and is recorded in the session-scope rows below.

The sixteenth review audited this table row by row against its own reading of the two properties and
found no row missing and none wrong, which is the strongest evidence the enumeration has. It also
found what the table's own construction could not reach. **AL2**: the refused-frame reference's row
named the state/event grid's recipient `unseen` route among its publishing surfaces and read
`sufficient`, while the two cells on that route still published the pre-AK1 record — the row was
verified against the artifact's prose, which does publish the reference, and a surface named at the
granularity of a route is satisfied by any one passage inside it. The `session state` row above is
**AL3** and is the other half: that fact is read by `C4-P1` and was in no row, because the enumeration
took its per-session facts from C12's declared list and the declaration omitted it.

| Operand | Read by | Scope the clause claims | Publishing surfaces | Sufficient |
| --- | --- | --- | --- | --- |
| the `session state` an interaction is admitted and dispatched in | `C4-P1` clauses 1 and 2 | that session's own state, and the vector may carry two | session state machine §States (the admits-a-new-interaction column) and §Drain protocol; state/event grid §Session coverage grid; C2 | sufficient |
| accepted terminal fact, and the admitted interaction it closes | `C4-P1` clause 1 | one interaction, in one session | C10 first enumeration; interaction machine terminal states and terminal-provenance table; neutral brief parity profile (terminal provenance) | sufficient |
| dispatch of an `interaction identity` | `C4-P1` clause 2 | not twice — **within one session**, which the property did not say until AK7 | C10 first enumeration (session and interaction identities, dispatch boundary); neutral brief parity profile (dispatch boundary crossed or not); interaction machine §Admission order and §Concurrent interactions | sufficient |
| count of `nonterminal interactions` | `C4-P1` clause 3 | **within one session**, which the property did not say until AK7 | C10 first enumeration (state); interaction machine §Concurrent interactions; state/event grid initiator and recipient rows | sufficient |
| the `established finite bound` | `C4-P1` clause 3 | that session's own established profile | C4 (`max-in-flight`); neutral brief §Version and establishment rule; responsibility matrix `Bounded unary concurrency` → `channel-profile`; migration ledger limits table | sufficient |
| the `established profile` a bound and a class are read against | `C4-P1` clause 3 and `C3-P1` | one per session, per AK8 | neutral brief §Version and establishment rule and parity profile (digest of each session the vector carries); C1; responsibility matrix `Fixed/negotiated profile equivalence` | sufficient |
| recorded recipient `rejected-protocol` provenance at `unseen` | `C4-P2` conjunct 1 | the recording endpoint's own observation | C10 second enumeration; interaction machine `unseen` transition row and terminal-provenance last row; state/event grid `unseen` cells and prose; neutral brief parity profile; responsibility matrix local-observation row | sufficient |
| its detailed reason `unopened-interaction-identity` | `C4-P2` conjunct 1 | the closed set of `invalid-interaction-correlation` | migration ledger closed detailed-reason set (AC2); C10; interaction machine; state/event grid; neutral brief parity profile | sufficient |
| the **refused-frame reference** (registered as `refused`; five fields, published in full by the surfaces beside it) | `C4-P2` conjunct 1 | one frame, one identity, **one session**, one committing endpoint | interaction machine `unseen` transition row; state/event grid recipient `unseen` route; neutral brief local-observation schema and parity profile; responsibility matrix local-observation row; migration ledger new-evidence inventory | sufficient |
| effect certainty `known-none` on that refusal | `C4-P2` conjunct 1 | the refusal route | C10; interaction machine `unseen` row (AE2); state/event grid `unseen` cells (AE2) | sufficient |
| the committed request naming that identity, as a declared stimulus step | `C4-P2` conjunct 1 | one endpoint, one identity, one session | neutral brief §Vector format (committing endpoint under W5, session under AH1) | sufficient |
| precedence between that request step and the refused control | `C4-P2` conjunct 1 | one endpoint, one identity, within one session | neutral brief property-operator set (W1, session under AG2) | sufficient |
| the recipient's subsequent admission of the refused identity | `C4-P2` conjunct 1 | membership over the identities admitted **in the same session** (AF8) | C10 first enumeration (every attempted interaction distinguishes session and interaction identities, admission decisions); neutral brief parity profile; migration ledger new-evidence inventory (AF4); C4 | sufficient |
| the late-traffic latch value, including `not-applicable` | `C4-P2` conjunct 2 | one terminal interaction | C10 first enumeration; interaction machine latch section; state/event grid latch section; neutral brief local-observation schema and parity profile | sufficient |
| the **settling-frame reference** (registered as `settling`; five fields, published in full by the surfaces beside it) | `C4-P2` conjunct 2 | one declared stimulus step, one endpoint, one session | interaction machine latch section; state/event grid latch section; neutral brief local-observation schema and parity profile; responsibility matrix local-observation row; migration ledger new-evidence inventory | sufficient |
| the **terminal-frame reference** (registered as `terminal`; five fields, published in full by the surfaces beside it) | `C4-P2` conjunct 2 | that endpoint's own frame, one interaction, one session | interaction machine latch section; state/event grid latch section; neutral brief local-observation schema and parity profile; responsibility matrix local-observation row; migration ledger new-evidence inventory | sufficient |
| precedence between the settling frame and the terminal frame | `C4-P2` conjunct 2 | one endpoint, one identity, within one session | neutral brief property-operator set | sufficient |
| the `state-violation` category the latch settles under | `C4-P2` conjunct 2 | interaction scope | interaction machine latch section; state/event grid; migration ledger category table | sufficient |
| the property's own selector "for each interaction identity" | `C4-P2` preamble | **within one session**, which the preamble did not say until AK7 | C4 common terms (identity unique within one session); C12 declared per-session facts | sufficient |

**Four of these rows are one owned fact.** The recipient `rejected-protocol` provenance, its detailed
reason, the effect certainty on that refusal, and the refused-frame reference are the four contents of
the `unseen` refusal record, which is declared in
[`conformance/channel-0.2-facts.json`](../../../conformance/channel-0.2-facts.json) and rendered into
every surface those rows name — C10, the interaction machine's `unseen` row, the grid's two `unseen`
cells, and the responsibility matrix row that owns the observation record. Their "publishing
surfaces" columns therefore say where the fact appears and no longer say where a copy of it is
maintained, which is what AL2's row was: the row was verified against the route's prose while two
cells inside that route published an older form of the same record. A row here that reads
`sufficient` for a rendered fact is checkable by the gate rather than by re-reading each surface; a
row for a fact stated in prose still is not, and the settling- and terminal-frame field lists in this
table are the ones still in that position.

**What the enumeration establishes, and what it does not.** Every operand above is now published at
the scope its clause claims, and the three that were not are the AK1 family. What it does not
establish is that the *reading* is complete: the rows were derived by reading the two properties
clause by clause, and a fact a property reads without naming — the way conjunct 2 read the terminal
frame under the words "that endpoint's own frame that made the interaction terminal" — is exactly what
this method has to catch by reading rather than by parsing. A later cycle that finds a row missing
should treat the omission as the finding and add the row, not repair the table quietly.

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

A fifth pass raised **Z1**-**Z4**. **Z1** — that ordinal is observed arrival order, which W1 removed
from the property language on purpose, so it is restricted to identification and may never be an
ordering operand. **Z2** — the grid's `unseen` cells still named `rejected-protocol` in the format
every other row uses for a next state, which Y3 had just settled is a provenance. **Z3** — C10 gained
the latch under Y1 and stopped at the terminal interaction's, leaving the `not-applicable` value X2
introduced compared and unowned. **Z4** — the migration ledger's inventory of 0.2 cases with no 0.1
predecessor did not list intra-interaction frame order or its two mutations, so the requirement every
finding since S1 turns on was absent from the list of what Batch 2 must build; unlike the rest of the
sequence this is not a defect a correction introduced but one they all went past.

A sixth pass left the design package and asked what the entry points say, raising **AA1**-**AA3**.
**AA1** and **AA2** — the Channel index and the future-work index had fallen behind every correction
family since V2, the second still naming S1 as the open blocking finding and both understating the
retained review count; the counts are now computed from the reviews directory and every family
recorded in this disposition history must appear in both. **AA3** — the future-work index still
attributed the ordering row to `channel-core`, the identifier U2 abolished, so the closed owner
vocabulary was closed in one artifact only.

A seventh pass raised **AB1** and **AB2**. **AB1** — the redesign plan is the fourth entry point and
the one status block the T4 cycle-name check never covered, and it had stopped at S3 while six
correction passes ran. **AB2** — X5, Y1, and Y2 made the local observation record what `C4-P2` reads,
and the responsibility matrix owned the peer fault, the loss classification, the effect certainty and
the observability system that *consumes* observations, while the observation record itself had no
owner row. A fact a property depends on with no owner is the S1 defect, in the artifact S1 was raised
against, six families after S1 was called closed.

An eighth pass raised **AC1**-**AC4**, retained as the
[AC correction iteration review](./reviews/channel-0.2-ac-correction-iteration-review.md). **AC1** —
Y4 added the settling frame's arrival ordinal to the neutral brief and to nothing else, while the
interaction machine owns the latch, the grid enumerates the cells that assert it, and the matrix owns
the observation record; because the brief is subordinate to all three, the hierarchy resolved the
contradiction against Y4 and left the parity profile comparing a field no observation carries, which
is Y1 restored by the fix for Y4. **AC2** — V1 made the peer-fault detailed reason normative wherever
its category declares a closed set and named the `C4-P2` case as one reason of
`invalid-interaction-correlation`; the ledger's closed set had five identity reasons and none of them
covers an identity that was never opened, so the compared field had no value for the refusal the first
conjunct reads. The same pass found that both `unseen` cells record one provenance while the conjunct
quantifies over a cancellation control alone, so C10 now requires the kind of frame refused. **AC3** —
both conjuncts opened with "no endpoint records" and continued "the same endpoint had already
committed", whose nearest antecedent is the recording endpoint, which is never the committing one; the
literal reading quantified over an endpoint pair no vector can produce, which is U1's unfalsifiable
property arriving through a pronoun. **AC4** — the check written over the X7 class matched one-letter
finding families only, so it could not see AA, AB, or the findings of the review retaining it.

A ninth pass raised **AD1**-**AD3**, retained as the
[AD correction iteration review](./reviews/channel-0.2-ad-correction-iteration-review.md), and is the
first whose findings are in the evidence *about* the design rather than in the design. **AD1** — the
AC review's residual stated that the AA and AB passes had left no retained record and referred the
resulting gap to the owner, while the W correction iteration review records AA1-AA3 and AB1-AB2 under
its fourth and fifth pass headings; acting on that residual would have meant authoring duplicate
records or rescoping a requirement that was never violated. It was made by reading the W review's
roster entry and scope line instead of the document, which is AC1 committed by the pass that raised
AC1. **AD2** — the X7 comment names two halves of its class check and AC4 widened only the first; the
second is written over two ids while the policy bolds thirty-six, so it cannot fail for AA, AB, AC, or
AD. There is no live gap and it is left as an owner call rather than corrected by a second actor in
the same week. **AD3** — the W review's scope line, the policy's roster entry, and the AC residual
gave three different accounts of what that review contains, none matching it; each is what some later
pass consulted instead of opening it, and AD1 is the proof that at least one did.

The ninth independent closure review, at `9408948`, returned `does-not-conform` with blocking **AE1**
and nonblocking **AE2**-**AE5**, and ruled the open **AD2** call a defect. **AE1** — `C4-P2`'s first
conjunct was red on a conforming realization. Loss of either frame is a required member of the
property's own adversarial group; when the transport loses the request the initiator legally commits
its one cancellation control, because C8 states recipient admission is not observable from
`dispatched`, and the control lands at `unseen` producing exactly the refusal the conjunct forbade of
an endpoint that had already committed the request. A lost request and a reordered one presented
identical values in every field the property may read, so the design had no third option: either the
property failed on legal behaviour, or declaring the loss vector green left the named mutation green
too, which is U1 by another route. The 2026-08-14 owner ruling resolves it by reading the fact that
already separates them — a reordering delivers the request afterwards and the recipient admits an
interaction for that identity, a loss never does — and the parity profile now compares that admission.
**AE2** — X3 and Y3 made the `unseen` refusal a detailed row so the machine's totality rule would not
claim it, and that rule was what supplied effect certainty; both artifacts now state `known-none`.
**AE3** is the structural half and the reason ten cycles missed AE1: C12 required every property to be
able to fail and nothing required one to stay green, so the loss vector sat in a required group with
no stated expectation. C12 now carries the converse rule, the property format carries a required-green
set, and the per-capability audit carries the column. **AE4** — the Channel index was a third surface
describing the retained iteration reviews and omitted AA and AB behind a range; every family is now
named and the AD3 check covers that surface. **AE5** — the retained requirements register instructs
item-by-item disposition and no `CH-R` identifier appeared in the package; `CH-R10`, the ordering
non-promise the S1 ruling narrowed, is now dispositioned explicitly. **AD2** — the X7 class check
asserted a class in its comment and tested two literals; the class is now derived from the policy's
own iteration-pass attributions.

Eleven of the twelve required-green cells read `owed` rather than a guessed set, which was named residual work and stated as such above; every cell is now filled against an executable evaluator.

The tenth independent closure review, at `c358464`, returned `does-not-conform` with blocking **AF1**
and nonblocking **AF2**-**AF8**. It confirmed the AE1 correction works — its evaluator returns green
on the lost-request vector and red on both named mutations — and then found the correction incomplete
one artifact below itself. **AF1** — C4's passage stating the mutation vectors' expected observations
said they are "exactly" the recorded refusals and called that complete data, while the corrected
conjunct reads a second fact. A vector authored from that passage leaves the membership test an empty
set and takes `C4-P2` green on `C4-control-precedes-request`, which is U1 reached through the vector
rather than through the property, and two paragraphs of C4 contradicted each other while every gate
stayed green. The passage now states the complete record set both endpoints produce and names the
subsequent admission as part of it. **AF2** and **AF3** are second halves of the AE4 and AE5
corrections: the Channel index's narrative still named a five-family-stale sequence and a closed
finding as open, and the ledger's completion check still did not claim the register its own sources
list had just gained, while the new disposition understated that register's `CH-K` range. **AF4** —
the new-evidence inventory enumerates the observation fields the ordering vectors compare and omitted
the admission AE1 added, which is Z4's class applied to the newest correction. **AF5** — the
required-green set named four of its group's seven legal members, and conforming commit-order delivery
in both directions was among the three missing; all seven are now named. **AF6** — the AD2 replacement
derived its class from one sentence shape and could not see `V`, `W5`, or `W6`, so the classification
is now declared in a totality-checked provenance table instead of inferred from prose. **AF7** — C12's
soundness rule is written over every property and the audit enforced it over thirteen of the
twenty-six the package states — AF7's own disposition said twelve of twenty-five, which is **AK3**;
`S1`-`S6` and `I1`-`I7` are now audited under the same three obligations, and the
record shows `I1`-`I7` satisfy neither half. **AF8** — the membership operand was scoped to the vector
while interaction identity is unique only per session, so a two-session vector could satisfy it across
sessions and take the conjunct red on conforming behaviour; the operand is now session-scoped.

The eleventh independent closure review, at `57bb1d8`, returned `does-not-conform` with blocking
**AG1** and nonblocking **AG2**-**AG5**, and closed AF3-AF7 completely. **AG1** — AF1's evidence named
two artifacts and quoted both; the correction closed C4 and stopped, and the check written for it
searched the contract alone, so this review's silence-probe row still gave the ordering mutation's
expected observation as the recorded refusal. A vector authored from that row takes `C4-P2` green on
its own named mutation: the U1 condition, surviving in the commit written to close it. **AG2** is a
sharper class than the omission — C4 asserted that the precedence relation carries AF8's session
qualifier, and the brief's operator set did not carry it. That is a correction making a claim about an
artifact it never opened, and a conforming two-session vector goes red under the operator as
published. The qualifier is now in the brief and the claim is pinned against it. **AG3** — the dated
AE1 ruling still stated the vector-scoped operand AF8 corrected, while C4 deferred to that ruling; the
ruling now carries its scope corrected and the original recorded as issued, as the S1 ruling records
`channel-core`. **AG4** and **AG5** are the third and fourth surfaces of the same index staleness AE4
and AF2 each closed one of.

**The pattern is the finding.** Four instances now share one shape — AE4→AF2, AE5→AF3, AF1→AG1,
AF2→AG4 — a correction closing the *first* artifact a finding's evidence names and stopping. A sweep
over all eleven retained attestations extracted, for every finding, the artifacts its own evidence
section cites: 47 findings cite artifacts and 23 cite more than one. Everything through AD was
verified individually by reviews 8 through 11 in the artifacts it was raised against; the live set was
AE, AF, and AG, and AG1 and AG4 are what the sweep confirms was left. The per-artifact index rows now
state their position against the newest family explicitly — naming it or declaring the artifact
unchanged by it — so a row cannot go stale by being left alone, and cross-artifact claims are pinned
against the artifact they describe so AG2's class cannot be written again.

The twelfth independent closure review, at `f451f55`, returned **`conforms-with-nonblocking-findings`**
— the first non-negative verdict in the programme — with **AH1**-**AH6** and no blocking finding, and
verified AG1-AG5 closed in the artifacts their evidence named, AG1 by evaluator rather than by reading.
Under the 2026-08-15 ruling recorded in the redesign plan, only an unqualified `conforms` closes the
batch, so that verdict stands as issued and did not close it. **AH1** — AG2 scoped the precedence
relation to a session and left the declared stimulus step unable to name one, which is W5's defect
inside the correction written to close AG2; underneath it sat a question no artifact answered, and the
vector format now states that a vector **may carry more than one session** and gives each step its
session. **AH2** — AF5's required-green correction was closed in the contract and the brief and left
here, in the audit Batch 2 authors the property file from, still naming four of seven members. It is
the fifth instance of the closed-in-the-first-artifact pattern and the one the AG sweep could not
reach, because that sweep enumerated the artifacts each finding's *evidence cites* and AF5's evidence
never cited this document. **AH3** — three narrative surfaces stopped one family short, one of them
stating affirmatively that no independent review had seen the AF corrections after the eleventh had.
**AH4** — the AG4 row check's escape clause was the bare phrase `unchanged by`, bound to no family, so
five of nine rows would have satisfied every future family's check without making a claim; the escape
now has to name the family it escapes. **AH5** — U7's direction-scope disposition predates AE3's
converse rule, and under that rule the disagreement it discloses is a known conforming-realization
exposure for `C4-P1` and `I5` rather than an undeclared scope; the row now says so, because `owed`
reads as "not yet written" and not as "known to have a red case". **AH6** — two sentences cited the
retention rule as *requiring* the later admission when it says the request is admitted on its own
merits and the earlier refusal does not bar it; both now state the coverage limit that follows.

**The sweep's own limit is worth carrying forward.** AH2 was unreachable by the AG sweep's method, and
the fix is to enumerate the artifacts a correction *touches* rather than the artifacts a finding's
author happened to cite.

The thirteenth independent closure review, at `e7bfeba`, returned `does-not-conform` with blocking
**AI1** and nonblocking **AI2**-**AI9**. **AI1** is the AH1 decision propagated to one operand of two.
AH1 declared multi-session vectors legal and gave the declared stimulus step a session so the
precedence relation had its operand; the settling-frame reference — the other operand of the same
property — stayed published in three places as four fields with no session, and both the brief and the
interaction machine asserted it maps to one declared step. That assertion stops being true the moment
two sessions may hold one identity value, and `C4-P2` then evaluates green on
`C4-outcome-precedes-ack`. All three field lists now carry the session. **AI2** is the sixth instance
of the first-artifact pattern and the one review 12's closing note predicted in writing. **AI3** — AH5
closed in the audit's `C4` row and not the `I5` row its evidence names alongside it. **AI4** — six of
eight artifacts' own status blocks were stale by one to four families while the Channel index claimed
those corrections, and nothing read them. **AI5** — the AH1 ruling justified itself by citing
reconnect cases C2 does not have, which is AG2's class inside the commit asserting that class closed;
the citation is withdrawn rather than repaired. **AI6**, **AI7**, and **AI8** are corrected as their
evidence describes. **AI9** is the sharpest: S3's own evidence named the redesign plan's section 7.8,
which still reported seven retained negative attestations, so **a retained finding had been open for
six cycles** while every entry point reported the programme's findings closed.

**The sweep axis changed, and this is the record of why.** The AG sweep enumerated the artifacts each
finding's *evidence cites*; AH2 and AI1 were both unreachable by it, because neither artifact was
cited by the finding whose correction invalidated it. The correct axis is the concept: when a
correction changes a fact, the impact set is every artifact asserting something about that fact. AI1
was found that way by the reviewer and reproduced that way here, and AI4's check is written over every
artifact's status block rather than over the ones a finding named. The AI4 and settling-frame checks
are the first two written from a concept sweep rather than from a finding's citation list.

The fourteenth independent closure review, at `6cddb99`, returned `does-not-conform` with blocking
**AJ1** and nonblocking **AJ2**-**AJ7**. **AJ1** is AI1 surviving the commit written to close it.
The settling-frame reference is published in **five** places, not three: the two the AI1 correction
did not reach are the state/event grid, which the neutral brief declares itself subordinate to, and
the responsibility matrix row that *owns* the observation record — and the migration ledger's
new-evidence inventory states the same reference a sixth time. The reviewer reproduced AI1's exact
false green on `C4-outcome-precedes-ack` from both of the two, with the field list as the only
variable. The check written for AI1 could not see it: it iterated two artifacts and asserted its own
completeness with `Count -lt 3`, a bound set to the number of lists in its own scope, while four AC1
checks thirty lines above it already enumerated the correct four artifacts for the same reference.
All six surfaces now publish the reference in one form, and the check is written over the reference —
every surface must publish the identical field list, any publication-shaped passage anywhere in the
package must publish the whole list, and the count is exact rather than a lower bound.

**AJ2**, **AJ3**, and **AJ4** are one shape at three scales: a finding closed in the surface a check
can reach and left open in the surface its evidence quoted. **AJ2** — AI2's two narrative surfaces
are unchanged and the redesign plan claimed the correction that neither of them received, which is
AG2's cross-artifact class applied to a finding's own disposition; the claim is withdrawn, both
narratives are rewritten rather than token-substituted, and the check now derives from the declared
provenance table that every numbered closure review is introduced by ordinal in each narrative and its
family named there. **AJ3** — AI7 named two entries and the correction reached one; `profile` sat
outside the per-session distribution the same sentence made plural. **AJ4** — the AI4 check reads
whether a status block reaches the newest family, and both sentences AI4 actually quoted were
untouched: the brief's block described the declared stimulus step in its pre-AH1 form, and this
document's block said its disposition history runs to the eighth cycle while it ran to the
thirteenth. **AJ5** — AH4's escape clause is defeated by naming the family's last finding, because
`\bAI\b` does not match `AI9`, and two of the four rows that used that wording were false for AJ1's
reason. **AJ6** — the AI1 insertion put the session second in a five-field list whose next sentence
counted from the front, so the machine's argument for the arrival ordinal became a claim about a set
that omits the committing endpoint it is about; both latch sections now name the fields instead of
counting them. **AJ7** — the retained-attestations list filed the thirteenth review in the eleventh's
place.

**What the sweep is, after this cycle.** The AG sweep read the artifacts each finding's evidence
cites; the AI sweep read the concept but computed the impact set from memory of the artifacts it had
been editing, and reproduced AI1's own two-artifact evidence list as though it were the concept. The
sweep is now executed as a search over the repository for the changed fact's own vocabulary, with the
result recorded, before any artifact is edited — `grep settl` over `docs/future/channel/` returns all
six settling-frame surfaces in one screen. Each of the last three cycles' blocking findings, AG1, AI1,
and AJ1, was one search away from the pass that missed it.

The fifteenth independent closure review, at `5cfa5ed`, returned `does-not-conform` with blocking
**AK1** and nonblocking **AK2**-**AK4**, and confirmed AJ1 closed by evaluator: all six settling-frame
surfaces publish the identical list, and its probe reproduces AI1's false green from the pre-AI1 form
and a correct red from the published one. It also recorded that this is the **first cycle in eight**
with no finding closed in the first artifact its evidence named and left open in the second.

**AK1** is the fourth instance of one shape on `C4-P2`, and the first on its **first** conjunct. Every
cycle from AH1 through AJ1 audited the settling-frame reference; the recorded `unseen` refusal is the
other conjunct's operand, five surfaces published what it contains, they agreed with each other
exactly, and not one named the session AF8's membership scope requires or the interaction identity the
test is over. The review's probe builds the two-session vector AF8's own text names as the failure it
exists to prevent and takes the property **red on behaviour conforming at both endpoints in both
sessions**. **AK2** is the Channel index's Design reviews row omitting the `W` family, which the
policy's provenance table classifies as an iteration family and which the retained record it names is
named after; the AE4 check derived its class from finding headings and the W findings are recorded in
a table, so the check could not ask for it. **AK3** is three surfaces counting audit rows and
reporting properties. **AK4** is the ledger's status block counting publishing surfaces as publishing
artifacts.

The sixteenth independent closure review, at `95c62c1`, returned `does-not-conform` with blocking
**AL1** and **AL2** and nonblocking **AL3** and **AL4**. It confirmed the AK corrections sound where it
could measure them: its `C4-P2` evaluator is red on both named mutations and green on all seven
required-green members and on the AK1 and AK5 vectors, its row-by-row audit of the operand enumeration
above found no row missing and none wrong, and it recorded that `AK6` moves no verdict on its own in
any member of that property's group — an over-precise operand rather than a defect, raised as a note
and not as a finding.

**AL1** is AK7's defect on a sixth property, in the artifact the AK audit reported clean. `S3` bounded
admission by "the first drain transition" and named no session; a vector may carry two, so a second
session that legally establishes and admits after the first one drains violates it as written. The
audit could not have found it, and that is **AL3**: its trigger set is C12's declared list of
per-session facts, `S3` names none of them because it reads a session's state *through a transition of
it*, and the session's own state was not declared — the four facts that were are exactly the four the
AK pass had found red, which is a class derived from its own members. **AL2** is the refused-frame
reference published in five surfaces and corrected in four; the state/event grid's two `unseen` cells
still carried the pre-AK1 record, and both halves of the AK1 check key on the reference's name, which
those cells never used. **AL4** is `S5` comparing an `established profile` — a declared per-session
fact — across the vector.

All four are corrected. Every property of the session state machine now names the session it means and
is checked structurally, on the ground that the machine's properties are statements about one session
by construction; C12's declared list carries the session's own state and is checked against the neutral
brief's vector format rather than against itself; the grid's two cells are registered as surfaces of
their own; and the package-wide sweep for that record is keyed to the record rather than to the
reference's name.

**The correction pass stopped sampling.** `C4-P1` and `C4-P2` were enumerated completely — every fact
they read, the scope each clause claims, every publishing surface, and whether the published fields
suffice — and the enumeration is retained above as the section this document now carries. It found
three further operands short at the scope their clauses claim. **AK5** is the rest of AK1's own
operand: the refusal record named no committing endpoint, which is the conjunct's literal subject, and
no arrival ordinal, so a control committed *before* the request binds to one committed after it and
the property goes red on delivery that matched commit order. **AK6** is the second conjunct's
*second* precedence operand — "that endpoint's own frame that made the interaction terminal" — which
no artifact published at all; it was read off the terminal form, and a form names one frame only while
an endpoint commits at most one frame of that form for one identity, which a duplicate terminal from a
nonconformant peer is exactly the violation of and is a required-green member of the property's own
group. Both are corrected as frame references in the same five-field form, and the check is written
over the class "a frame a property reads is published as a frame reference" rather than over any one
reference, so a fourth is registered or fails the sweep.

**AK7** and **AK8** are the same shape one level up, and are what the enumeration found by asking the
question of the properties' own quantifiers rather than of their clauses. AH1 settled that a vector
**may carry more than one session**, and the decision reached the declared stimulus step, the
settling-frame reference and now the refusal record — but never the property statements. `C4-P1`
forbade an identity being dispatched twice and bounded the number of nonterminal interactions with no
session named, `C4-P2`'s preamble quantified "for each interaction identity" across the vector, and
`I5` bounded concurrency against "the established finite bound" — each red on a conforming two-session
vector (**AK7**). Outside C4 the same class holds `C1-P1`, which required exactly one established
profile per vector, and `C3-P1`, which said "the established profile" where a vector may carry two
(**AK8**). C12 now declares which facts belong to one session each, so the rule is enforced over a
declared class rather than over the members that happened to be visible — which is AF6's correction
applied to a rule instead of to a family.

**AR1** was raised by the sixth author-side W1-W3 iteration pass, and it is the first finding in this
programme found by an instrument rather than by a reading. The pass ran each verification gate under a
line trace and required every conditional in it to be evaluated by a passing run. Two were not, and
both are in this document's subject rather than in the gates: **`C5-P1` and `C6-P1` each state two
clauses, each had one named mutation, and each mutation fires through the first clause.**

For `C5-P1` no declared vector carried a pre-dispatch structural refusal at all, so the second clause
— every such refusal records `known-none` — had no input that reached it. For `C6-P1` the corpus
carries exactly one non-permitted authority decision and that interaction is also dispatched, so the
first clause returns before the second is reached. Both second clauses were **deleted outright from
the evaluator and both gates stayed green**, across 113 evaluations over 41 declared inputs.

This is the rule C4's own audit row above already states — *one named mutation per conjunct, because
half a property with no mutation is half unfalsifiable* — enforced for the one property that declared
conjuncts and silent for the other twenty-five. No owner ruling was needed for the same reason U1
needed none: the design already claims what the correction restores. Each property now names its two
clauses, each clause carries its own named mutation, and the existing requirement that a mutation
declared against a conjunct must fire *through* that conjunct now reaches them. The two new mutations
are `C5-pre-dispatch-refusal-possible-effect` and `C6-denial-without-decision-point`, registered in
the per-capability audit above, which is the artifact Batch 2 authors property files from and which
had described both properties by their first clause alone.

The class is closed rather than the two instances: `build/verify-channel-0.2-coverage.ps1` fails when
any conditional in a covered gate is never evaluated, so a clause no input reaches is a gate failure
rather than a finding several cycles later. It ran on every push until **AT7** moved it behind
`build/verify-gate-self-checks.ps1`, where it runs on the schedule and on request.

**AT1**-**AT7** were raised by the eighth author-side pass, and **AT1 is AR1 again on a property AR1's
own correction could not reach.** That correction closed its class with a gate rule keyed on
properties which declare a `conjunct`; `C5-P1` and `C6-P1` do, and `I4` does not. So `I4` kept two
clauses and one mutation, the mutation fired through the second, and nothing in the property's group
carried a pre-dispatch refusal at all -- the first clause was deleteable outright with both gates
green. This is **AL1's lesson in a third guise**: a guard that recognises a defect by the words the
defect uses cannot see the instance that does not use them.

**AT2** and **AT3** are the same shape below the clause. `C6-P1`'s second clause requires three things
of a denial and reads them as a disjunction of omissions, so AR1's own mutation returned at the first
and the other two operands were evaluated by nothing; `C10-P1` reads the interaction's refusal before
its terminal histories and returns there, so the terminal-history obligation had no input. The audit
rows for `I4`, `C6` and `C10` above now name a mutation per clause, and for `C6-P1`'s second clause one
per obligation. `I4`'s first-clause mutation is the input `C5-P1`'s second clause already uses, because
the two clauses say the same thing about the same record.

**AT4** and **AT5** are verification rather than design and are recorded here because the family is
classified whole, on AR's precedent. AT4 widened the coverage instrument to the guard harness, to
itself, and to a second unit -- an operand no input reaches while the expression around it is
evaluated, which is what found AT1-AT3. AT5 is a probe mutation that survived an interrupted run:
the seventh pass hardened restoration against a transient failure, and a process that is killed never
reaches its restoration at all.

**AT6** is why the probes behind all of this were worth re-running. The coverage gate refused any
dirty repository while the reason it gave covers design artifacts alone, and the harness mutates a file
before running the gate a probe names -- so every probe aimed at that gate was answered by the refusal
instead of by its own rule, `AR2-a` included, since AR2. The refusal is now scoped to the directory the
reason names.

**AT7** is recorded with them because it is the same shape as what they are about. The AT4 measure was
timed in isolation and never in the repository gate that runs it, and covering two further gates took
that gate past its thirty-minute ceiling -- a number true about a part and never checked against the
whole. The two are no longer covered, the measure is faster than the one it extends, and what that gives
up is stated where the trade is made.

The class is closed rather than the three instances, and one level lower than AR1 closed it: the
coverage gate now fails when an operand of an evaluated expression is never evaluated, which is
structural over every property whatever its artifact calls the clauses.

**AU1**-**AU5** were raised by the ninth author-side pass, and **AU1 is that same class a third time,
including inside the clause AR1 was raised against.** AR1 stated the rule over declared conjuncts and
AT1 found the property that declares none; AT stated it over operands an expression never evaluates,
and these eleven obligations *are* evaluated, on every declared input, and never fire. Each was
deletable outright with the property, design and coverage gates all green, which the pass verified by
deleting each in turn. `C5-P1-clause-1` is the sharpest: AR1 gave that clause a mutation, the clause
states two obligations, the mutation fails a declared bound and returns, and the positional Shape rule
beside it was still unpinned. The other ten are both clauses of `C2-P1`, and one obligation each in
`C3-P1`, `C7-P1` twice, `C8-P1`, `C9-P1`, `C10-P1`, `C11-P1` and `I6`. The audit rows for all nine
properties above now name a mutation per obligation.

`C2-P1`'s third clause was unreachable by construction rather than for want of an input: no legal edge
leaves a terminal session state, so every input that takes S4 red also takes S1 red and the first clause
returns first. Its mutation closes the session and then records an accepted `established>draining`
transition, which the legal table does contain. Two of the eleven needed no new vector at all --
`C2-P1` and `C8-P1` are evaluated *through* S1, S4, I2 and I3 rather than restating them, so the inputs
those machine properties already name are the inputs these clauses need, which is the precedent `I4`'s
row states.

**AU2** is the same eleven audited for what would make them false pins, and it is **AE1's shape
latent**. Six properties were red on a conforming timeline whose interactions and sessions publish no
detail fields, so an obligation could not tell a realization that violates it from an input that does
not state the fact -- and each new mutation could have been satisfied by silence rather than by its
violation. `@($null)` is a one-element array in PowerShell, so an unpublished collection read as one
holding a null and `C11-P1` was red with a blank where the facet name belongs in its own witness; an
unpublished scalar is falsy, which is the other five. Collections are read through `Get-List` and
required scalars through `Read-Required`, which raises the absence against the vector rather than
returning a verdict, and the collection half is pinned by an additional-green member, on the precedent
of the one added for the conforming session fault. It is not live on today's inputs; the probe that
pins it removes one field from the
conforming single-session realization and reproduces `C3-P1` red on its own required-green member.

**AU3** and **AU4** are verification rather than design and are recorded here because the family is
classified whole, on AR's and AT's precedent. AU3 is the probe corpus stated in four places with three
values, where the one surface a gate recomputed was the one that was right -- the same split an earlier
pass recorded against the plan's own measures, one cycle later and in the same document, because that
correction made five measures computed and never asked where else the fact was stated. AU4 is the
review policy's exact-next-work paragraph naming the eighth
pass as the live path while listing the eighth as retained above it. The harness now sweeps every
narrative surface for a stated probe count. **AU5** is the disposition index's own section for the
redesign plan, which carried the previous family's clause twice: five freshness checks read those
sections and each asks whether the newest family is named, so a duplicated append is invariant under
all of them.

The class is closed rather than the eleven instances, and one level lower again: the property gate
fails when any `New-Red` call site is reached by no declared input. That unit is total over the file by
construction -- an obligation is a verdict constructor call whatever the contract calls its clauses --
which is what neither a declared conjunct list nor an operand measure could be.

These changes still need a fresh independent closure re-review and do not authorize Batch 2
themselves.
