# Channel 0.2 design-foundation closure review 7 attestation

Date: 2026-08-13

Reviewer identity: `agent:claude-opus-5-channel-0.2-closure-review-7-2026-08-13-3892c23`

Reviewed commit: `3892c23a8dd4c7f298e877ba73710ee0ddc97bc4`
(`fix(channel)!: hold a cancellation that races recipient admission`)

Overall verdict: **does-not-conform**

## Independence and isolation

This attestation satisfies the isolation clause of [`reviews/README.md`](./README.md), which is the
one condition the sixth review did not meet.

What holds:

- **A fresh isolated clone was used.** All reads and probes ran in a clone created for this review
  alone, checked out at the pin. `git rev-parse HEAD` returned
  `3892c23a8dd4c7f298e877ba73710ee0ddc97bc4`; `git status --short` returned empty; the working tree
  carried no local modification and no branch was checked out. Nothing in the origin working tree was
  read, written, or executed. This attestation file is the only change in the clone.
- The reviewer identity is distinct from the correction author and from all six retained reviewers
  (`agent:channel-0.2-design-foundation-review-2026-08-11`,
  `agent:channel-0.2-design-foundation-closure-review-2026-08-11-e863bf1`,
  `agent:channel-0.2-design-foundation-final-closure-review-2026-08-11-1af7ba0-third`,
  `codex-channel-0.2-definitive-closure-2026-08-11-1b7c5fd`,
  `agent:claude-opus-5-channel-0.2-totality-closure-2026-08-11-5cf42c4`, and
  `agent:claude-opus-5-channel-0.2-closure-re-review-2026-08-13-11ba93b`).
- No author-session private reasoning was available or used. Only repository evidence at the pin was
  read. The review brief that dispatched this session deliberately summarised none of the correction
  author's reasoning, and the option set behind the 2026-08-13 ruling was read from the redesign
  plan's `Resolved questions` section like any other artifact.
- The verifier executed is the one at the pin, not a later one.

What to weigh honestly:

- The reviewing context was cold with respect to the repository, but not blank: the dispatching brief
  named the artifact to start from (`reviews/README.md`), stated that the R1 correction is the
  primary target, and listed three specific things to check for new silence — what bounds the hold,
  whether a held control interacts with drain or session loss, and whether dispatch-before-held-control
  is the right zero-effect boundary. Finding S1 below is **not** one of those three and was reached
  independently; finding S2 is the second of them, and the attestation says so rather than presenting
  it as unprompted.
- One repository gate could not be executed in this clone for an environmental reason unrelated to the
  design; it is reported at probe 9 rather than counted as a pass.

## Overall decision

The R1 correction is real work and it fixes the cell it was aimed at. At recipient `validating` the
held-control rule is stated in C8, carried by two interaction-machine transition rows plus a
second-control fault row, split into its own grid row, and added to the completeness review's silence
inventory. The four artifacts agree with each other about that cell. R2 and R3 are closed. All
findings B1-B4, N1-N3, F1-F3, D1-D5, and T1-T4 remain closed in the artifacts they were raised
against, re-verified individually below rather than taken from an index.

The first batch nevertheless does not close. One new blocking finding, **S1**, is recorded.

R1 was raised against a grid row that covered `unseen` **and** `validating` together. The correction
split that row and fixed `validating`. At `unseen` the fault was deliberately kept, and the only thing
that stops a conformant initiator's legal cancellation from landing there is a sentence added to the
state/event coverage grid in the same commit: "A realization delivers controls for one interaction
identity in the order the peer committed them within one session." That is a delivery-ordering
guarantee. C4's explicit silence disclaims transport ordering, C11 says Channel core promises no
ordering, and the responsibility matrix assigns ordering to `delivery-facet` with Channel core named
in the `Explicitly not owned by` column. The correction is therefore sound only under a fact the
contract does not carry and the matrix gives away. Remove the guarantee and R1 reproduces verbatim
one row down — which is what probe 6 does.

This is the same shape as T1 and R1: two artifacts assigning one fact different provenance, each
internally consistent. It is reached at a third point in the machine.

The ordinary structural gates pass, including the design verifier and its negative probe. The
verifier's new R1 check is a genuine cross-artifact comparison and is the right kind of check; it
compares what four artifacts say about the `validating` cell. It does not ask who owns the ordering
fact the correction leans on, and no structural check could, because every artifact involved is well
formed and every `Cn-P1` property stays green across the defect.

## Architecture, targets, and predecessor evidence

### Current architecture and implementation targets — conforms as context

`Brontide-Architecture-Status.json` selects Architecture 0.8 at
`docs/current/architecture/Brontide-Architecture-0.8.md`, status **Complete Draft (document and
implementation evidence complete; not ratified)**, hash
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579`. The document was hashed in the
clone and matches the registry byte for byte. `latestRatifiedArchitecture` is `null` with an explicit
rationale. Both stack READMEs state `**Designed for:** Brontide Architecture 0.8, Complete Draft, not
ratified` and route the scope of that claim to their own limitations, matrices, and executable
evidence; both registry entries record `designedFor: 0.8`. Neither stack claims a Channel 0.2
implementation, and the first-batch package claims neither ratification nor runtime conformance. This
is a design-foundation verdict only.

### Decision 13 and CM3/CM4 — conforms

Decision 13 is recorded 2026-08-11 by `user:JakHoh` in `binding/portable/open-decisions.md` §686:
Option A retained for Portable Binding 0.1, **Option B selected for 0.2** — separate establishment
from readiness and add declared relational lifecycle traffic before Ready; Options C and D rejected.
Option B's own wording anticipates "a new envelope kind"; the first batch instead carries the
semantic ruling through a distinct interaction class on the ordinary interaction machine. That
substitution is deliberate, argued in the completeness review's `C3/C7` finding, and recorded as a
resolved owner ruling. C3, C7, the interaction machine's **Relational initialization** section, the
responsibility matrix, the migration ledger, and the plan carry it identically, with the pre-Ready
window `interconnected && !ready`, exact declaration match, separate relational authority, and the
rule that Channel creates neither Ready nor Release.

### Predecessor evidence — conforms

`build/verify-channel-vectors.ps1` passes: 24 vectors covering 11 requirements, 12 protocol
categories, 7 process categories, and 5 failure domains. Every CH-01..CH-24 vector has exactly one
disposition row in the ledger within the declared five-value vocabulary. The retained Channel 0.1
design note, draft contract, and Architecture 0.8 requirements/risk ledger remain untouched
predecessor evidence at their own pins; the requirements ledger states in its own header that it "is
not presumed to define the 0.2 structure". PB8's neutral closure attestation records **conforms** for
C1-C10 of the *neutral PB8 scope only*, explicitly not ratifying architecture or Channel; the
migration ledger treats it as predecessor conformance rather than successor correctness, which is the
correct use.

## Capability verdicts

| Capability | Verdict | Rationale |
| --- | --- | --- |
| C1 | **conforms** | One immutable profile precedes dispatchability; fixed and negotiated paths must produce equal normative records, with a field absent from the fixed path declared a contract defect rather than realization freedom; unknown versions, required features, classes, and authority modes refuse with no implicit downgrade and no in-place renegotiation. Untouched by the R1 correction. |
| C2 | **conforms** | Six states, only `established` admitting new interactions, external phases explicitly excluded. Duplicate local or peer drain moves `draining` to `faulted` with one session-scoped `state-violation`, preserving the first snapshot and rewriting no interaction certainty. The session totality rule closes the event domain and names its one nonfatal exception. |
| C3 | **conforms** | A locally false or unknown guard is frameless refusal with `known-none` "at either endpoint, including the recipient's independently derived external phase"; the recipient table routes that case to `refused-local`; the ledger's `state-violation` row now points at C3 instead of contradicting it. |
| C4 | **does-not-conform** | This is S1's home. C4 is titled "correlation, concurrency, replay, and **ordering** are explicit" and its **Silence** clause reads "C4 promises neither fairness nor relative scheduling, transport ordering, durable deduplication, or exactly-once effects." The R1 correction now depends on an intra-interaction delivery-ordering guarantee that C4 declares itself silent about and does not state. C4 owns this fact and does not carry it. Everything else in C4 holds: live replay commits one `replay-detected` without redispatch, admission reserves an in-flight position atomically, bound refusals consume no lasting replay entry, and a new session cannot inherit an old replay window. |
| C5 | **conforms** | Positional payload/authority classification, finite declared bounds, and structural validation all precede dispatch; authority positions never project; a partial or oversized frame never becomes a partial interaction. |
| C6 | **conforms** | Authority is evaluated per interaction after structural admission and before dispatch; local denial is frameless with `known-none`; no Capability, Constraint expression, or derivation chain crosses a trust boundary; unknown authority structure never projects and never permits. |
| C7 | **conforms** | Relational initialization matches exactly one CM3 declaration inside the Interconnection/pre-Ready window, carries separate narrow authority, returns actual terminal provenance to CM4 cleanup or rollback, and cannot itself produce Ready or Release. |
| C8 | **does-not-conform** | The held-control rule is correct, complete, and well argued *for `validating`*, and its zero-effect boundary is defensible (assessed below). Two gaps remain. C8 still makes the initiator's cancellation legal on a purely local precondition — "Exactly one cancellation request may be sent for a nonterminal dispatched interaction" — while the recipient-side guarantee that keeps that control out of `unseen` lives outside C8 and outside the contract entirely (S1). Separately, C8's held-control clause enumerates exactly two resolutions, "If admission succeeds" and "If admission refuses", and `validating` has a third exit (S2). |
| C9 | **conforms as written** | The four provenance forms are exclusive in the contract, both machines, and the terminal-provenance table; unknown peer-fault categories fault locally as `unrecognized-peer-fault` with no answering frame. S1 does not sit inside C9's text, but it produces the same C9-P1 consequence at `unseen` that R1 produced at `validating`: a recipient-local delivery-order artifact accepted as a peer Channel statement. |
| C10 | **conforms as written** | Certainty is `known-none` only with evidence dispatch did not occur, `known` with profile-owned details, or `unknown` with a reason. The held-control path preserves this correctly: a control held during admission-order steps 1-9 cannot cause an effect, and the second-control fault from `validating` is pre-dispatch and therefore `known-none` under the interaction totality rule. |
| C11 | **conforms as written** | Required facets must be supported exactly; optional facets are ignored only under a declared additive-absence rule; no facet may redefine identity, authority, terminal provenance, or certainty; retry is a new identity with optional causal attribution. C11's sentence "Channel core promises no retry, delivery, ordering, persistence, resumption, or exactly-once effect" is correct as written and is one of the two contract statements the grid's new sentence contradicts. |
| C12 | **does-not-conform** | C12-P1 requires every neutral vector to have one deterministic expected portable observation, and C12 declares that "a vector with an unspecified expectation is invalid evidence rather than permission for each stack to choose." Under S1 a cancellation vector aimed at the `unseen` row cannot state one deterministic expectation: its frame and provenance depend on a delivery-ordering property that Channel core does not promise, that no facet is required to supply, and that has no owner. Batch 2 cannot author that vector group as things stand. |

Every C1-C12 section carries named scenarios, one `Cn-P1` property, required evidence, and explicit
silence. C8 gained two named scenarios in the correction (`C8-cancel-during-admission-held`,
`C8-cancel-held-then-admission-refused`). These verdicts assess whether the statements cover the
behavioral domain, not whether the headings exist.

## Area verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state machine | **conforms** | Six states, explicit legal and refused/illegal tables, a closed-world totality rule naming its one nonfatal exception, and a drain protocol that is symmetric but exactly-once per endpoint history. Duplicate drain, premature close, terminal-state input, and loss each have exactly one result. S1-S6 are stated as falsifiable properties. Unchanged by the R1 correction and correct as it stands. |
| Interaction state machine | **does-not-conform** | The two new recipient rows are well formed and the `dispatch precedes the held control` boundary is stated explicitly. But **Concurrent interactions** says "A fatal session or transport loss maps every nonterminal local initiator and recipient interaction to `lost`", while the recipient transition table's `lost` row admits only `executing`, `cancel-requested`, and `cancel-refused`, and **Interaction event totality** narrows the same rule to "any *post-dispatch* nonterminal state". `validating` is nonterminal and pre-dispatch, so the three statements disagree about it, and with a control held there the disagreement acquires a consequence (S2). |
| State/event totality | **does-not-conform** | Independent enumeration reproduces 6×6 session, 6×6 initiator, and 6×6 recipient cells — **108 total, zero empty** (probe 5), confirming the correction's own count and the R3 split. Totality is genuine and the `validating` cell is now correct. But the `unseen` cell is *covered and contested* for exactly the reason the `validating` cell was, and the prose added beneath the grid introduces a delivery-ordering fact this artifact does not own (S1). Total coverage is not correct coverage; this grid's purpose statement claims only the former. |
| Responsibility matrix | **conforms as written** | All 37 rows carry one exact owner identifier matching `^[a-z0-9-]+$`, a nonblank neutral crossing artifact, and separate consumer and carrier columns with no co-owners. Ready is `component-management`; Interconnection, Release, withdrawal, and cleanup are `portable-binding`; the Relational Initialisation phase is `composition`. The matrix is also the artifact that *exposes* S1: its `Delivery, persistence, ordering` row names `delivery-facet` as owner and Channel core as explicitly not the owner, and no row exists for the obligation the grid places on "a realization". |
| Contract-completeness review | **does-not-conform** | The new `cancel during recipient admission` silence-inventory row is correct and sits exactly where the sixth review said it should. But this review's own **Residual review risk 2** instructs the reviewer to "test whether it accidentally imports scheduling or ordering promises" — the R1 correction did import one, and the inventory does not record it. The held-control-under-loss silence (S2) is likewise absent. This is the artifact whose subject matter is absence. |
| Migration coverage | **conforms** | 24 vectors, twelve protocol categories, seven process categories, five failure domains, ten limits, ten features, and every observation field carry a disposition inside the declared five-value vocabulary. T1 and T2 remain corrected in place. Its `ordering guarantee unsupported` row — "no cross-interaction order; extension facet required" — is the one artifact whose scoping is compatible with the grid's new sentence, and it is a **retained non-promise** row, not a grant of the intra-interaction guarantee the correction needs. |
| Neutral contract/vector brief | **conforms as a brief; Batch 2 gate not satisfied** | It requires separated data-only schemas, typed identity spaces, closed enums, finite bounds, deterministic expected observations, one property plus one named mutation per capability, and independent native/neutral endpoints. Its entry gate fails on criterion 1 ("no unresolved internal contradiction") and criterion 2 ("every responsibility-matrix concern has one owner"), so the gate correctly holds schema authoring closed. |

## Retained-finding closure decisions

Each was checked in the artifact it was raised against, at this pin, not taken from an index.

| Finding | Decision | Evidence at `3892c23` |
| --- | --- | --- |
| B1 | **closed** | Recipient `refused-local` remains the frameless route for a structurally valid authority presentation denied by local policy; structural authority failure remains `rejected-protocol`. |
| B2 | **closed** | Recipient cancellation-authority denial produces `cancel-refused` with a nonterminal `refused` acknowledgement; the initiator records it as a distinct nonterminal state. |
| B3 | **closed** | Every matrix row carries one exact owner identifier and one neutral crossing artifact. |
| B4 | **closed** | CH-01 through CH-24 occur exactly once with declared dispositions; re-verified by `verify-channel-vectors.ps1`. |
| N1 | **closed** | The exact Ready owner is carried identically by the plan, matrix, ledger, and completeness review. |
| N2 | **closed** | Invalid, unrecognized, unsupported, or wrongly scoped cancellation control is one interaction-scoped recipient fault with post-dispatch uncertainty preserved. |
| N3 | **closed** | The three retained non-promises use disposition `retained` and remain explicit non-promises. |
| F1 | **closed** | Live replay selects `peer-fault`, commits `replay-detected`, never redispatches, and ignores a later handler terminal. |
| F2 | **closed** | Recipient terminal states are exclusive `peer-fault` and `lost` with matching provenance rows. |
| F3 | **closed** | No bold disposition outside the declared five-value vocabulary remains in the ledger. |
| D1 | **closed** | `draining` plus duplicate local or peer drain is a legal-table transition to `faulted` with one session-scoped `state-violation` and the first snapshot preserved. |
| D2 | **closed** | Unsolicited acknowledgement faults; `cancel-pending` plus `accepted`/`refused` selects distinct states; any further acknowledgement faults with effects preserved. |
| D3 | **closed** | Frameless recipient `refused-local` for a false or unknown receiver-local phase predicate, in the contract, the interaction machine, and the ledger. |
| D4 | **closed** | The `late-traffic-fault` latch has exactly three values with one possible scoped fault emission and no answering-fault loop. |
| D5 | **closed** | The delivery `fallback` observation has a **moved** row naming the delivery/retry facet owner and keeping `none` attributable. |
| T1 | **closed** | The ledger's `state-violation` row still reads "An external phase refusal is never this fault: a false or unknown predicate is a frameless local refusal at either endpoint under C3." |
| T2 | **closed** | The `replay-detected` row still binds the fault to the nonterminal window and routes a post-terminal repeat to the late-traffic latch. |
| T3 | **closed** | The interaction machine's `executing` or `cancel-refused` handler-reports-cancelled row remains widened to its class, and C8 and the recipient grid carry the same rule. |
| T4 | **closed** | All first-batch status lines say "a fresh independent closure re-review"; none names a superseded cycle. The verifier holds this mechanically. |
| **R1** | **closed at `validating`; not closed at `unseen`** | The `validating` cell is corrected in C8, two interaction transition rows, its own grid row, and the completeness inventory, and the four artifacts agree. R1 was raised against the combined `` | `unseen` / `validating` | `` row, and at `unseen` a conformant initiator's legal control is still answered with `rejected-protocol` — a peer Channel statement — prevented from occurring only by the unowned ordering guarantee. See S1. |
| **R2** | **closed** | Interaction machine **Cancellation** item 4 now states the two preconditions separately and adds "The two preconditions are local to their own endpoints and no event synchronises them", with the absence of a request-accepted acknowledgement named as the reason. |
| **R3** | **closed** | `unseen` and `validating` are separate grid rows with different cancellation-control verdicts, and the paragraph beneath the grid gives each its own rationale. |

## Four resolved owner-ruling verdicts

The 2026-08-13 R1 correction ruling is correctly recorded in the redesign plan as a correction ruling
that "does not join the four first-batch rulings above", and the review README says the same. The four
remain the fixed set recorded on 2026-08-11.

| Ruling | Verdict | Consistency assessment |
| --- | --- | --- |
| Core concurrency and cancellation | **conforms as a ruling; S1 sits inside its subject matter** | Finite bounded unary concurrency and optional cancellation with fixed terminal meaning are core in C4, C8, both machines, the grid, the matrix, and the ledger, and the ruling is represented consistently in all of them. S1 is not a departure from the ruling but an incomplete working-out of it: the ruling puts cancellation terminality in core, and the package has not settled who owns the delivery property that decides which terminal a legal control reaches. |
| Session-state ownership | **conforms** | The six Channel session states and external ownership of Interconnection, Relational Initialisation, Ready, Release, withdrawal, and cleanup are stated identically in the plan, C2, the session machine, the matrix's boundary ruling, and the ledger. |
| Relational-initialization representation | **conforms** | One exact pre-Ready interaction class, never a second envelope family, in the plan, C3, C7, the interaction machine, the matrix, the completeness review, the ledger, and the brief, and consistent with Decision 13, CM3, and CM4. |
| Extension invariants | **conforms** | C11, the matrix extension-hook ruling, and the ledger's feature rows consistently permit additive facets and forbid redefining identity, authority, terminal provenance, or certainty. |

## Blocking findings

### S1 — the R1 correction rests on a delivery-ordering guarantee that C4 disclaims and the responsibility matrix gives away

**Statement.** The correction keeps `rejected-protocol` for a cancellation control arriving at
recipient `unseen`, and prevents a conformant initiator from ever landing there by asserting, in the
state/event coverage grid alone, that a realization delivers controls for one interaction identity in
commit order. That is an ordering promise. C4's explicit silence and C11 both disclaim it, and the
responsibility matrix assigns ordering to `delivery-facet` with Channel core named as explicitly not
the owner. One fact, three artifacts, three different owners — and the correction's soundness depends
on the one artifact that has no claim to it.

**Evidence:**

- `Brontide-Channel-0.2-State-Event-Coverage-0.1.md`, paragraph beneath the **Recipient interaction
  coverage grid** (added by this commit): "A realization delivers controls for one interaction
  identity in the order the peer committed them within one session; cross-interaction ordering
  remains unpromised under C4."
- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, C4 **Silence**: "C4 promises neither fairness nor
  relative scheduling, **transport ordering**, durable deduplication, or exactly-once effects." C4 is
  the capability titled "correlation, concurrency, replay, and ordering are explicit", so this is the
  owning item declaring itself silent. Its body separately says "No cross-interaction completion order
  is promised", which means the Silence clause's "transport ordering" is not merely restating that.
- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, C11: "Channel core promises no retry, delivery,
  **ordering**, persistence, resumption, or exactly-once effect."
- `Brontide-Channel-0.2-Responsibility-Matrix-0.1.md`, ownership matrix: `| Delivery, persistence,
  ordering | `delivery-facet` | Channel profile **may** require facet → extension | exact extension
  facet/version | **Channel core** |`, where the last column is `Explicitly not owned by`. The verb is
  *may*: a Channel 0.2 core profile is not obliged to require a delivery facet at all.
- The same matrix's realization row is `| Wire encoding and frame mechanics | `realization-profile` |
  Channel → realization declaration | **encoding id, framing id, finite bounds** | Channel logical
  contract |`. There is no ordering field in that crossing artifact, so a realization has no declared
  way to state the obligation the grid places on it and a profile has no way to verify it. Under the
  matrix's own **Rule** — "Every semantic fact has one owner" — this fact has none.
- `Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md`, **Residual review risks** 2: "the
  reviewer should test whether it accidentally imports scheduling or ordering promises." This finding
  is the result of doing exactly that.
- `git show 3892c23` confirms the sentence is new in this commit and that C4's Silence clause was not
  touched: the contract diff changes only the status line, the C8 cancellation paragraph, and C8's
  named-scenario list.

**Falsifying trace (executed; probe 6).** A profile supports cancellation. Channel "does not identify
a transport" (contract **Boundary**), no delivery facet is required, and C4 promises no transport
ordering — so a realization that reorders two frames of one interaction is permitted by the contract.
The initiator admits interaction `I`, commits the request to the seam (`dispatched`), then commits
exactly one cancellation control, entirely within C8, reaching `cancel-pending`. Both frames are in
flight. The control is delivered first. The recipient is at `unseen`, whose grid cell reads "no
identity to correlate → `rejected-protocol`" — a bounded peer protocol fault, classified by the
**Terminal provenance** table as a peer Channel statement. The initiator accepts that fault from
`cancel-pending` and terminates at `peer-fault`. The request then arrives at a terminal interaction
and goes to the late-traffic latch. One conformant initiator, one supported control, a terminal
history asserting the initiator erred, selected by a property no endpoint observes and the contract
does not promise. This is R1's falsifying trace with `unseen` substituted for `validating`.

**Why the properties stay green.** The evaluator reports `C8-P1` true on both orderings: the
interaction has exactly one terminal history and nothing non-semantic is recorded as success. `C4-P1`
holds — one identity, dispatched once, one terminal closing one interaction, bound respected. `C3-P1`
holds — class, direction, and phase all matched. Only cross-capability invariant 4, "One fact has one
provenance and one semantic owner", separates the two orderings, and it is not a `Cn-P1` property and
has no negative probe. This is Decision 10's structural blindness to silence, at the third location
this programme has found it.

**Impact.** C4 and C12 fail as recorded above; the interaction-machine, state/event-totality, and
completeness-review areas fail. C9-P1 and cross-capability invariant 4 are violated by the `unseen`
cell under any realization that does not volunteer the ordering guarantee. The responsibility
matrix's one-owner rule is violated by the new obligation. The neutral brief's Batch 2 entry gate
fails criteria 1 and 2, and the plan's first-batch exit gate fails conditions 2, 3, and 6. **This is
blocking.**

**Correction direction (not a repair; the reviewer does not repair).** The package already contains
every piece needed and the choice is an owner's, not a reviewer's. If intra-interaction control
ordering is genuinely a Channel 0.2 core requirement, it belongs in C4 as a stated promise with a
property and an evidence mode, with C11's sentence narrowed to match, a responsibility-matrix row
naming its owner and crossing artifact, and a realization-profile declaration a profile can check —
at which point the `unseen` fault becomes correct and provable. If it is not a core requirement, then
the `unseen` cell needs the same treatment `validating` received, which the ruling's own
unbounded-state objection would have to be answered for — bounding held state by the established
`max-in-flight` is the obvious candidate and is already a declared finite profile fact. Either way the
resolution must appear in C4, the responsibility matrix, the grid, and the completeness review's
silence inventory, not in the grid alone.

## Nonblocking findings

### S2 — a held control has no disposition when admission ends by loss rather than by resolving

C8's held-control clause enumerates exactly two resolutions: "If admission succeeds…" and "If
admission refuses…". `validating` has a third exit. The interaction machine's **Concurrent
interactions** section says "A fatal session or transport loss maps **every nonterminal** local
initiator and recipient interaction to `lost`", which includes `validating`; its **Interaction event
totality** section says "Local loss in any **post-dispatch** nonterminal state selects `lost`", which
excludes it; the recipient transition table has no `validating` row producing `lost` at all; and the
recipient grid's `validating` / `Local loss` cell says only "local session route", which is not one of
the named routes in the closed-world totality rule. With one control held, the question acquires a
consequence: whether the held control is discarded silently, as a refused admission discards it, or
fires the late-traffic latch. Practically it must be the former, and no reading is dangerous — which
is why this is nonblocking rather than blocking — but the artifact set does not say, and a grid whose
purpose is that every state/event pair has exactly one route should. The same gap exists symmetrically
for initiator `candidate`/`admitting`. Drain is a milder instance of the same shape: the design does
not state whether an interaction still in `validating` when drain arrives is inside the snapshotted
admitted set or is a candidate to be refused.

### S3 — the R1 correction pass left the plan and the Channel index at the pre-R1 state, including a count the correction invalidated

The correction updated the normative artifacts and the Channel index's prose, but not the plan's
status block, the plan's §7.8, or the index's artifact table. Concretely, at this pin:

- `Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md`, **Status**: "B1-B4, N1-N3, F1-F3, and
  D1-D5 are closed as framed; the totality review found blocking T1 and nonblocking T2-T4, which now
  have a contract-first correction and await a fresh independent closure re-review before Batch 2."
  R1-R3 are absent, although the same document's `Resolved questions` carries the 2026-08-13 R1
  ruling.
- Same document, §7.8: "**Five** independent negative attestations are retained. Their findings
  through T1-T4 have correction passes at `11ba93bddbd38f03df59b4afc5166d7c6991c865`." Six are
  retained, and the current correction pin is `3892c23`.
- `docs/future/channel/README.md`, artifact table: the state/event coverage row reads "Added for
  D1-D4; T3 corrected; **102 cells** enumerated independently" — the corrected grid has **108** cells,
  a number this commit's own message states; the capability-contract row omits R1; the interaction
  machine row omits R1/R2; the completeness-review row says "All findings through T1-T4 corrected";
  and the design-reviews row says "**Five** negative reviews retained".
- `Brontide-Channel-0.2-Responsibility-Matrix-0.1.md`, status: "confirmed unchanged by the fourth and
  fifth reviews" — the sixth also confirmed it unchanged.

This is the T4 class recurring — a correction landing in the normative artifacts while the summaries
that route readers to them keep describing the previous state — and it violates `AGENTS.md`'s
"Documentation cleanup is default completion work… update the relevant indexes in the same change".
The 102/108 cell count is a factual error about the corrected artifact rather than only staleness. T4
was graded nonblocking and this is graded the same way, but the verifier pins status *phrases* and
does not pin these *counts and finding lists*, so nothing currently stops the next recurrence.

## Observation for the author (not a finding against the design)

The design verifier's allowlist of files permitted in the reviews directory is an exact set of seven
names (`$expectedReviewNames`, `build/verify-channel-0.2-design.ps1`). Adding this attestation trips
it, as probe 4 records. Per the review brief this is reported rather than repaired; the verifier
script was not modified. The message it emits — "The Channel 0.2 R1 correction pin must retain exactly
the review README and all six negative attestations before the next closure review" — is accurate
about the pin and simply has not yet been advanced to admit a seventh attestation. The author's step 4
already covers updating the verifier; this note only confirms which check fires and why.

## Probes and checks

All commands ran in the fresh isolated clone at `3892c23a8dd4c7f298e877ba73710ee0ddc97bc4`.

1. **Isolation.** `git rev-parse HEAD` → `3892c23a8dd4c7f298e877ba73710ee0ddc97bc4`;
   `git status --short` → empty; no branch checked out; `origin` is the local repository, fetched
   once at clone time. No path outside the clone was read, written, or executed.
2. **Artifact hashes at review time.** Capability contract
   `705f24ca5f52efc05aeab3801143cec24bd29d28ddaa1a32e25df634b1fb56ec`; interaction state machine
   `ab13ebd9ae0f34cde41f39da849ec60ef06ad8e5b4d98c6912bcb83ccb3b968f`; session state machine
   `a6c1513ba2a135014dacef5f22352f41be7f165fdbaba1e706cd288e78058f44`; state/event coverage
   `24370b1fc276b94426ae4102ca3d29d1dda53ea985ad145fd3c22a700d0fb367`; responsibility matrix
   `9cd77979465954929c710d06e72d8c5035f960c84bddaceabd3793fe0572a09c`; migration ledger
   `3c6089cd5fa128796de7aa52fb593f8f84bf2562d5f9da10545abdf165629c97`; completeness review
   `8c1a5bc2a0a98f88f83efa6e3e3063424b47ec2a5b8d691e4de9a7da6ba63dd5`; neutral brief
   `050682d00bcde1f636a0b25a7a6207994cad04f15e93afa73d854fa4b4f4fd2d`. The session machine,
   responsibility matrix, migration ledger, and neutral brief hash identically to the values the sixth
   review recorded at `11ba93b`, confirming the R1 commit did not touch them.
3. **`build/verify-channel-0.2-design.ps1`** — **passed**, exit code 0. Output: "Channel 0.2
   design-foundation verification passed: 11 required artifacts, C1-C12 with
   properties/scenarios/silence, total session/interaction event coverage, 6 session states, all 24
   predecessor vectors dispositioned, 4 owner rulings resolved, and independent review still pending."
   Run before this attestation file existed.
4. **`build/verify-channel-0.2-design.ps1 -NegativeProbe`** — **failed as designed**, exit code 1,
   with exactly one message: `FAIL: Channel 0.2 capability contract properties is missing '**Property
   C12-P1.**'`. This confirms the structural property check can fail and fails only because `C12-P1`
   was removed in memory. Re-running the plain verifier after writing this attestation additionally
   emits the allowlist failure recorded above; that is the only difference.
5. **Independent grid enumeration.** A table parser written from the published markdown alone, not
   from the design verifier, gives session 6 rows × 6 event columns, initiator 6 × 6, recipient 6 × 6
   — **108 cells, 0 empty**. This confirms the correction's own count and the R3 row split (the sixth
   review counted 102 against the five-row recipient grid). The recipient `Cancellation control`
   column reads, in order: `unseen` "no identity to correlate → `rejected-protocol`"; `validating`
   "valid control: hold exactly one, apply on admission; second control → `peer-fault`"; then
   `executing`, `cancel-requested`, `cancel-refused`, and the terminal latch row.
6. **Genuine property-falsification attempt — capability-wide properties against the delivery-order
   race.** An evaluator was written from the published C4/C8/C9 text, the initiator and recipient
   transition tables, and the recipient grid's cancellation column, then run over the same conformant
   initiator under two delivery orders. **Request first:** recipient `unseen` → `validating`, control
   held, admission resolves, dispatch precedes the held control, recipient ends `cancel-requested`,
   initiator ends `cancel-accepted`, no fault. **Control first:** recipient ends `rejected-protocol`,
   initiator ends `peer-fault`, and the request that follows meets a terminal interaction. `C8-P1`
   returned **true on both**; `C9-P1` read literally returned **true on both**; only cross-capability
   invariant 4 separated them. The attempt to falsify a capability-wide property therefore **failed** —
   no `Cn-P1` could be made to fail on the defect — and that failure is the finding: the property set
   is blind to S1 exactly as it was blind to R1 and T1, so a green property is again not evidence of
   absence.
7. **Held-control disposition sweep.** Every occurrence of "held" across the contract, both machines,
   the grid, and the completeness review was collected. All of them resolve the hold through exactly
   two exits, "admission succeeds" and "admission refuses". None covers session or transport loss
   during `validating`, and none covers drain arriving while an interaction is mid-admission. This is
   S2.
8. **`build/verify-channel-vectors.ps1`** — **passed**: 24 vectors covering 11 requirements, 12
   protocol categories, 7 process categories, 5 failure domains.
9. **`build/verify-doc-links.ps1`** — **passed**: 825 local links across 295 documents.
   **`build/verify-text.ps1`** — **could not be executed in this clone**, exit code 1 with
   `DirectoryNotFoundException` on
   `Minimal/src/Brontide.Minimal.Experimental.ComponentManagement/Brontide.Minimal.Experimental.ComponentManagement.fsproj`.
   The file is present in `git ls-files`; its absolute path in this clone is 230 characters, exceeding
   the Windows `MAX_PATH` limit that the script's `File.ReadAllBytes` call is subject to. This is an
   artifact of the isolated clone's deep temporary path, **not** a repository defect, and it is
   reported rather than counted as a pass. `build/verify-interchange.ps1` was not run for the same
   reason; the sixth review executed the full gate at the immediately preceding pin and this commit
   changes only Markdown and the design verifier.
10. **Retained attestation integrity.** All six retained attestations were hashed. The five the sixth
    review recorded are unchanged: `a2f9f7cd…9070`, `ff50b7ff…46ec`, `31e47931…b93f2`, `b37d810d…c90f`,
    and totality `0eee8d277e344f8a7c988076ce31b127397c05bffb506b6a4de3d10b3c3994e9`. The closure
    re-review attestation hashes
    `7005bcd3060435913cd2dbe302ecc002a850719c3a256f864b3f9ca0e06f9f35`. None has been altered.
11. **Correction-scope diff.** `git show 3892c23` was read in full. Nine files changed. The contract
    diff touches only the status line, the C8 cancellation paragraph, and C8's named-scenario list;
    C4's Silence clause, C11, and the responsibility matrix are untouched, which is what makes S1 a
    contradiction rather than a coordinated change.
12. **Registry pins.** `Brontide-Architecture-Status.json` selects Architecture 0.8 as Complete Draft;
    the document was hashed in the clone and matches
    `CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579`. No ratified architecture is
    recorded. Both stacks state the 0.8 Complete Draft target locally.
13. **The three questions the handoff asked about the hold.** *What bounds the hold if admission never
    resolves?* Adequately, though implicitly: `validating` is admission-order steps 1-9, all local, and
    the external phase predicate is "supplied explicitly with the local admission request" rather than
    fetched, so admission terminates; the hold is bounded at one control because C8 permits only one.
    No finding, but the contract never states the bound argument, and the `unseen` rationale shows the
    author reasoned about unbounded state one row up. *Does a held control interact with drain or
    session loss?* No, and it should — this is S2. *Is dispatch-before-held-control the right
    zero-effect boundary or merely the conservative one?* It is defensible and correctly argued. The
    stated justification is observational equivalence: the held control reaches "the same accepted or
    refused acknowledgement it would have reached had it arrived a moment later." The alternative —
    applying the control before dispatch — would let recipient-internal timing decide whether an effect
    occurred at all, reintroducing the unobservable-race defect on the effect axis rather than the
    provenance axis, and would place a cancellation-authority evaluation inside admission-order steps
    1-9, which are defined as unable to cause an effect. Dispatch-first is the principled choice, not
    only the conservative one. No finding.

## What the verifier could and could not have caught

The design verifier passes, and that is evidence about what it checks. It checks that eleven artifacts
exist; that C1-C12 each carry properties, scenarios, and silence; that the session and interaction
event domains are declared total; that all 24 predecessor vectors are dispositioned inside the
five-value vocabulary; that the four owner rulings are resolved; that status blocks use one stable
phrase and name no superseded cycle; and — new in this commit — that C8, the interaction machine, the
grid, and the completeness review agree about a cancellation control arriving during `validating`.
That last check is the right kind, written failing-first and mutation-tested, and it is what makes R1
mechanically closed at `validating`.

It could not have caught S1. Every artifact involved is well formed, the `unseen` cell is populated,
and the check compares four artifacts about one cell without asking which artifact owns the delivery
property that decides whether a conformant control can reach that cell. Detecting S1 requires reading
C4's Silence clause, C11, and a responsibility-matrix row as claims about the same fact as a sentence
in a fourth document — a cross-artifact ownership question, not a presence question.

It could not have caught S2, which is an absence spread across three sections that each say something
slightly different, none of which is missing.

It could not have caught S3: the verifier pins status *phrases* but not the finding lists or cell
counts inside index tables and plan prose.

## Closure consequence

This attestation closes T1-T4, R2, and R3, confirms B1-B4, N1-N3, F1-F3, and D1-D5 remain closed in
the artifacts they were raised against, and records R1 as closed at `validating` but not at `unseen`.
It records one new blocking finding, **S1**, and two nonblocking findings, **S2** and **S3**.

Under [`reviews/README.md`](./README.md) the design remains **does-not-conform**, Batch 2 must not
begin, and no closure record may claim a conforming first-batch foundation at
`3892c23a8dd4c7f298e877ba73710ee0ddc97bc4`. S1 must be corrected contract-first with a failing check
added for it, and a fresh independent review must follow from a reviewer identity distinct from this
one and from all six earlier reviewers.

One thing is worth recording plainly for whoever reads this next. The isolation condition was met this
cycle and it did not by itself produce the finding; reading four artifacts as claims about one fact
did. S1 sits one table row away from R1, inside the very correction written to close R1, and it
survived a purpose-built cross-artifact check because that check asked whether four documents agree
about a cell rather than who owns the fact the cell depends on. The lesson each of the last three
cycles has paid for is the same one: a correction that resolves a contradiction by *asserting* a new
fact must also say who owns it.
