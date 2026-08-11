# Channel 0.2 design-foundation totality closure attestation

Date: 2026-08-11

Reviewer identity: `agent:claude-opus-5-channel-0.2-totality-closure-2026-08-11-5cf42c4`

Reviewed commit: `5cf42c4d97083324ffb8d6bd68491a145b8e611a`
(`docs(channel): close state event domain`, committed 2026-08-11T18:20:22+02:00)

Overall verdict: **does-not-conform**

## Independence and isolation

This is a fresh fifth review. The reviewer identity is distinct from the design actor and from the
four retained reviewers (`agent:channel-0.2-design-foundation-review-2026-08-11`,
`agent:channel-0.2-design-foundation-closure-review-2026-08-11-e863bf1`,
`agent:channel-0.2-design-foundation-final-closure-review-2026-08-11-1af7ba0-third`, and
`codex-channel-0.2-definitive-closure-2026-08-11-1b7c5fd`). No author-session private reasoning was
available or used; only repository evidence at the pin was read.

All reads and probes ran in a fresh non-local clone created with `git clone --no-local --no-checkout`
at
`C:\Users\jakub\AppData\Local\Temp\claude\C--Users-jakub-source-repos-Brontide\17b687e4-b8ae-4e84-bc54-6995e0e94177\scratchpad\review-clone`,
followed by a detached checkout of the full commit above. `git status --short --branch` reported
only `## HEAD (no branch)`. The shared worktree was used to write this attestation and to confirm
that the reviewed artifacts are the artifacts now on `main`.

`AGENTS.md` was read completely before `docs/future/channel/reviews/README.md`. All four retained
negative attestations were read completely before the corrected design was judged. Their SHA-256
values at the pin were:

- original: `a2f9f7cdc77b3f934a59fbe14c240e9c2f0bfe5864ca0dd702376ad455899070`;
- first closure: `ff50b7ff974eb60042dac0be186a2da21d2e8e382ecd48c2656566ca565046ec`;
- final closure: `31e479312813bbf9a1912d6eee2949bdc9e2739d3d9c18afdc0a99157dbb93f2`; and
- definitive closure: `b37d810d9755733825296baafa217842bd38a17bdc3192452d26a6e69e05c90f`.

The pin is not the branch head. `e3b90a49855ce4a1dcde6f7c986f90fca51a32c3` merged one later handoff
commit, `846d7c0adae63d23039af6e9e050ed0e83f6b621`. Every reviewed design artifact is byte-identical
across the two commits; only the redesign plan's section 7.8 and `reviews/README.md` differ, and both
differences are handoff prose. The nine Channel 0.2 design artifacts hash identically at both
commits, for example the migration ledger at
`92bd516eb1b58a83dc986f8d193a64ce6f743b9dd27f49b5e5e8f2054d050c7a` and the interaction state machine
at `59685705ae9e0eb7cddc56edb50e12e9842a8cafdf68197af386050fef0c0ecf`. This verdict therefore
applies unchanged to the current branch head.

## Overall decision

The corrected commit closes D1, D2, D3, and D4 in the capability contract and both state machines,
adds a genuinely closed state/event coverage grid, and gives the predecessor delivery-fallback fact a
migration disposition. Independent enumeration of all 102 session, initiator, and recipient grid
cells found no empty cell, no cell with two routes, and no recognized event without a named result.

It does not close the first batch. One blocking finding, T1 below, survives the D3 correction: the
migration ledger's retained `state-violation` row still tells an adapter author that an external
phase refusal may be a peer fault, which is the exact provenance the corrected contract and recipient
machine now forbid at both endpoints. That sentence was cited as the fifth evidence bullet of D3 and
was not corrected with the rest of the finding. Three nonblocking findings are recorded.

The ordinary structural gates pass. They establish headings, vocabulary, links, inventory subsets, and
the pinned D1-D5 correction strings; they do not test whether two artifacts assign the same fact the
same provenance, which is where T1 lives. Batch 2 therefore remains closed under the review policy
and the plan's first-batch exit gate.

## Architecture, targets, and predecessor evidence

### Current architecture and implementation targets — conforms as context

`Brontide-Architecture-Status.json` selects Architecture 0.8 at
`docs/current/architecture/Brontide-Architecture-0.8.md` with status **Complete Draft (document and
implementation evidence complete; not ratified)**. The recorded hash matches the file:
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579`. The registry records no ratified
Brontide architecture, and the first-batch package claims neither ratification nor implementation.

Both stack READMEs state `Designed for: Brontide Architecture 0.8`, Complete Draft, not ratified, and
`Partial implementation with explicitly labelled experiments`. Their registry hashes match their
files (`4FA7C85C…BE91` for Reference, `C59AFAB6…2A6B` for Minimal). Neither stack claims a Channel
0.2 implementation. This attestation is a design-foundation verdict, not runtime conformance.

### Decision 13 and CM3/CM4 — conforms

Decision 13 is recorded, retains the 0.1 refusal of protocol-bearing CM3 groups as a version
limitation, and selects option B for 0.2: readiness split from establishment, plus a declared
relational stage between Interconnection and Ready carrying an Operation, Capability, and input Shape
drawn from the group's CM3 protocols. Option B's own wording anticipated "a new envelope kind"; the
first batch instead selects a distinct interaction class under the ordinary interaction machine and
records that as one of the four owner rulings. That is a representation change with the semantic
ruling preserved, and C3, C7, the interaction machine, the responsibility matrix, and the plan all
carry it identically. The pre-Ready window, exact declaration match, separate relational authority,
and the rule that Channel cannot itself create Ready or Release are consistent across those
artifacts.

### Predecessor evidence — historical evidence valid; migration disposition complete, one rationale wrong

`conformance/channel-0.1-vectors.json` contains exactly 24 vectors, `CH-01-CORRELATION-ECHO` through
`CH-24-FAILURE-DOMAIN-RELATIVITY`, covering 11 requirements, 12 protocol categories, seven process
categories, and five failure domains under `build/verify-channel-vectors.ps1`. Every one has a
disposition row in the ledger, each occurring exactly once and using only the declared five-value
vocabulary. The retained Channel 0.1 design note, draft contract, requirements/risk ledger, Portable
Binding neutral schemas, and PB8 closure attestations remain untouched predecessor evidence at their
own pins and are not treated as proof of successor correctness.

D5 is closed: the delivery `fallback` observation now has a **moved** row naming the delivery/retry
facet as its owner and keeping `none` an attributable value rather than an inference. The remaining
predecessor defect is not an omission but a wrong rationale in a row that exists — T1.

## Capability verdicts

| Capability | Verdict | Rationale |
| --- | --- | --- |
| C1 | **conforms** | One immutable profile is established before any interaction is dispatchable; fixed and negotiated paths must produce the same normative record; unknown versions, required features, classes, and authority modes refuse with `known-none` and no implicit downgrade. |
| C2 | **conforms** | D1 is closed. Duplicate local or peer drain is now a legal-table row to `faulted` with one session-scoped `state-violation`, the original drain snapshot preserved and no interaction certainty rewritten. The session totality rule closes the remaining event domain without a permissive default. |
| C3 | **conforms as written** | D3 is closed in this item: a locally false or unknown guard is a frameless refusal with `known-none` "at either endpoint, including the recipient's independently derived external phase", and the recipient table now routes that case to `refused-local` rather than grouping it with structural failure. The surviving contradiction is in the migration ledger and is recorded as T1 against migration coverage. |
| C4 | **conforms** | Live replay commits one interaction-scoped `replay-detected` fault without redispatch, admission reserves an in-flight position atomically, bound refusals consume no lasting replay entry, and terminal facts close exactly one named interaction. See nonblocking T2 for the post-terminal replay category. |
| C5 | **conforms** | Declared finite bounds and positional Shape rules are evaluated before dispatch; authority/control positions never project; partial or oversized data cannot become a partial interaction. |
| C6 | **conforms** | Authority is evaluated per interaction after structural admission and before dispatch; a structurally valid presentation denied by local policy is frameless `refused-local` with `known-none`; no Capability or derivation chain crosses a trust boundary. |
| C7 | **conforms** | Relational initialization matches exactly one CM3 declaration inside the Interconnection/pre-Ready window, uses separate narrow authority, returns actual terminal provenance to CM4, and cannot produce Ready or Release. |
| C8 | **conforms** | D2 and D4 are closed. Acknowledgements select distinct `cancel-accepted`/`cancel-refused` states, unsolicited or further acknowledgements fault, and the duplicate terminal now has one finite `late-traffic-fault` latch with exactly one possible scoped fault emission and no answering-fault loop. |
| C9 | **conforms as written** | The four provenance forms remain exclusive in the contract, both machines, and the terminal-provenance table; unknown peer-fault categories fault locally without a reply. T1 is the one place in the package where a local inference is still described as a possible peer statement, and it is located in the ledger rather than in C9. |
| C10 | **conforms** | Effect certainty is `known-none` only with evidence that dispatch did not occur, `known` with profile-owned details, or `unknown` with a reason; every corrected transition row now carries its certainty, and the delivery-fallback fact has an owner. |
| C11 | **conforms** | Required facets must be supported exactly, optional facets are ignored only under a declared additive-absence rule, and no facet may redefine identity, authority, terminal provenance, or certainty; retry is a new identity with causal attribution. |
| C12 | **conforms as written** | The neutral design is data-only and stack-independent, each capability has one falsifiable property, and the negative probe demonstrates the structural property check can fail. T1 leaves one predecessor-category mapping non-deterministic for the vector map Batch 2 derives from the ledger. |

Every C1-C12 section carries named scenarios, one `Cn-P1` property, required evidence, and explicit
silence. These verdicts assess whether those statements cover the behavioral domain, not whether the
headings exist.

## Area verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state machine | **conforms** | Six states, an explicit legal table, an explicit refused/illegal table, and a closed-world totality rule that names its one nonfatal exception. Duplicate drain, premature close, terminal-state input, and loss all have exactly one result. |
| Interaction state machine | **conforms** | Initiator and recipient tables are separate and exclusive, cancellation acknowledgement multiplicity is total, the receiver-local phase route is frameless, the late-traffic latch is finite, and the admission order fixes the zero-effect boundary. See nonblocking T3 for one under-specified recipient cell. |
| State/event totality | **conforms** | Independent enumeration of the published grids yields 6×6 session, 6×6 initiator, and 5×6 recipient cells — 102 in total — with no empty cell, no second route, and no cell resolved only by an implementation default. The grid defers to the detailed tables and adds no permissive default. |
| Responsibility matrix | **conforms** | Every row carries one exact owner identifier matching `^`[a-z0-9-]+`$`, a nonblank neutral crossing, and separate consumer/carrier columns. Ready is `component-management`; Interconnection, Release, withdrawal, and cleanup are `portable-binding`; the Relational Initialisation phase is `composition`. |
| Contract-completeness review | **does-not-conform** | Its D-correction summary states that D3 "routes a receiver-local false/unknown external phase to frameless `refused-local`", which is true of the contract and machine but not of the ledger row the definitive review cited as part of D3. The review therefore records a correction as complete that is not complete in the package it reviews. |
| Migration coverage | **does-not-conform** | Inventory coverage is complete — 24 vectors, twelve protocol categories, seven process categories, five failure domains, ten limits, ten features, every observation field including the D5 delivery fallback, and every disposition inside the declared vocabulary — but the `state-violation` row assigns a forbidden provenance. This is T1. |
| Neutral contract/vector brief | **conforms as a brief; Batch 2 gate not satisfied** | It requires separated data-only schemas, typed identity spaces, closed enums, finite bounds, deterministic expected observations, one property plus one named mutation per capability, and independent native/neutral endpoints. Its own entry gate correctly refuses schema authoring while a blocking review finding is open. |

## Retained-finding closure decisions

| Finding | Decision | Evidence at `5cf42c4` |
| --- | --- | --- |
| B1 | **closed** | Recipient `refused-local` remains the frameless route for a structurally valid authority presentation denied by local policy; structural authority failure remains `rejected-protocol`. |
| B2 | **closed** | Recipient cancellation-authority denial produces `cancel-refused` and a nonterminal `refused` acknowledgement; the initiator records it as a distinct nonterminal state. |
| B3 | **closed** | Every ownership row has one exact owner identifier and one neutral crossing artifact; the verifier enforces the identifier form mechanically. |
| B4 | **closed** | CH-01 through CH-24 occur exactly once, in order, with declared dispositions. |
| N1 | **closed** | The exact Ready owner is carried identically by the plan, matrix, ledger, and completeness review. |
| N2 | **closed** | Invalid, unrecognized, unsupported, or wrongly scoped cancellation control is one interaction-scoped recipient fault with post-dispatch uncertainty preserved. |
| N3 | **closed** | The three retained non-promises use disposition `retained` and remain explicit non-promises in the treatment column. |
| F1 | **closed** | Live replay selects `peer-fault`, commits `replay-detected`, never redispatches, and ignores a later handler terminal. |
| F2 | **closed** | Recipient terminal states are exclusive `peer-fault` and `lost` with matching provenance rows. |
| F3 | **closed** | No bold disposition outside the declared five-value vocabulary remains anywhere in the ledger. |
| D1 | **closed** | `draining` plus duplicate local or peer drain is a legal-table transition to `faulted` with one session-scoped `state-violation`, the first drain snapshot preserved, and no interaction certainty rewritten. C2, the drain protocol, and the session grid agree. |
| D2 | **closed** | `dispatched` plus unsolicited acknowledgement faults; `cancel-pending` plus `accepted`/`refused` selects distinct recorded states; any further acknowledgement from either faults with effects preserved. |
| D3 | **closed in the contract and interaction machine; not closed in the migration ledger** | C3 now binds the frameless rule to either endpoint and the recipient table has a dedicated `refused-local` phase row with the structural row no longer listing phase. The ledger sentence from the finding's fifth evidence bullet is unchanged. Recorded as T1 rather than as a D3 relabel, because the surviving defect is in a different artifact and has its own failing check and correction. |
| D4 | **closed** | The `late-traffic-fault` latch has exactly three values, the first duplicate terminal or late non-fault control attempts exactly one interaction-scoped `state-violation`, and a late peer fault or post-settlement traffic emits no frame. |
| D5 | **closed** | The delivery `fallback` observation has a **moved** row naming the delivery/retry facet owner and keeping `none` attributable. |

## Four resolved owner-ruling verdicts

| Ruling | Verdict | Consistency assessment |
| --- | --- | --- |
| Core concurrency and cancellation | **conforms** | Finite bounded unary concurrency and optional cancellation with fixed terminal meaning are core in C4, C8, both machines, the grid, the matrix, and the ledger's `maxConcurrentRequests` and cancellation rows. D2 and D4 removed the last incomplete control outcomes. |
| Session-state ownership | **conforms** | The six Channel session states and the external ownership of Interconnection, Relational Initialisation, Ready, Release, withdrawal, and cleanup are stated identically in the plan, C2, the session machine, the matrix, and the ledger. |
| Relational-initialization representation | **conforms** | One exact pre-Ready interaction class, never a second envelope family, in the plan, C3, C7, the interaction machine, the matrix, the completeness review, the ledger, the brief, and against Decision 13/CM3/CM4. |
| Extension invariants | **conforms** | C11, the matrix extension-hook ruling, and the ledger's feature rows consistently permit additive facets and forbid redefining identity, authority, terminal provenance, or certainty. |

## Blocking findings

### T1 — the migration ledger still permits a peer fault for an external phase refusal

**Evidence:**

- `Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md`, **Protocol-fault category migration**, row
  ``| `state-violation` | **retained** | Scope identifies session versus interaction; external phase
  refusal may be local frameless or peer fault depending where detected. |``
- C3, **Failure and uncertainty**, now says: "A locally false or unknown guard is frameless refusal
  with `known-none` at either endpoint, including the recipient's independently derived external
  phase."
- The recipient transition table routes
  ``| `validating` | receiver-local external phase predicate is `false` or `unknown` | `refused-local` | no |``
  and its `refused-local` state definition emits no peer frame. The structural row that previously
  absorbed phase failure no longer lists it.
- The terminal-provenance table classifies recipient `refused-local` as a local observation with no
  peer statement, and recipient `rejected-protocol` as a peer Channel statement. There is no
  remaining route from an external phase refusal to either peer form at either endpoint.
- Cross-capability invariant 4 requires one provenance per fact, and C9-P1 forbids any field that
  permits a local inference to be accepted as a peer statement.
- The definitive attestation cited this exact sentence as D3's fifth evidence bullet. The correction
  commit `5cf42c4` touched the ledger only to add the D5 delivery-fallback row.

**Falsifying trace:** an initiator admits a structurally valid ordinary request under its own
`released` snapshot; the recipient independently derives `released` as false. The contract and the
recipient machine require frameless `refused-local` with `known-none` and no emitted frame. The
ledger's retained `state-violation` row tells an adapter or vector author that this same refusal may
instead be a peer fault "depending where detected". Both readings prevent handler dispatch, so
C3-P1 stays green and cannot separate them; the observable difference is whether a peer frame leaves
the recipient and whether the initiator's terminal history is `peer-fault` or `lost`.

**Impact:** the neutral brief lists "receiver-local phase refusal" among the required state-event
vector groups and requires each vector to declare an exact frame/provenance decision, and Batch 2
derives `migration/channel-0.1-vector-map.json` from this ledger. Migration coverage, the completeness
review's D3 claim, C9/C10 provenance data, C12 determinism, and first-batch exit-gate criteria 2 and 6
fail. This is blocking. It is a surviving component of D3, not a relabel: D3's core defect — the
recipient table's `rejected-protocol` route — is closed, and this defect is in a different artifact
and requires its own check and correction.

## Nonblocking findings

### T2 — the post-terminal replay category is named twice

The recipient grid routes a repeated request against a terminal interaction to the late-traffic
latch, which commits one `state-violation`. C4 scopes `replay-detected` to a repeat received "while
its original interaction is nonterminal", so the machine is determinate. The ledger's
``| `replay-detected` | **retained** | Same session/interaction identity already accepted; no
redispatch. |`` row nevertheless describes an applicability condition that also covers the terminal
case. A vector author reading only the ledger could label the post-terminal duplicate
`replay-detected`. Recommend narrowing the ledger row to the nonterminal window and naming the
late-traffic latch for the terminal case.

### T3 — recipient `cancel-refused` plus a handler-reported cancelled terminal has a route but no recorded result

The recipient grid's `cancel-refused` row says "success/failure accepted; cancelled is invalid". The
interaction totality rule then supplies the route — a wrong-state local action that emitted no frame
is refused locally and leaves the interaction unchanged — so no cell is uncovered. The consequence is
that a handler which has finished leaves its interaction nonterminal with no route to a terminal
except session or transport loss, and no observation is specified for the discarded handler result.
The initiator's mirror case is fully specified (`cancel-refused` plus a correlated cancelled Outcome
selects `peer-fault` with `unknown`). Recommend stating the recipient's result explicitly rather than
leaving it to the catch-all.

### T4 — three artifact status lines understate their own corrections and name a superseded review

- The capability contract's status names "N2, F1/F2, and D1-D4" and awaits a "definitive closure
  review", which has already occurred.
- The interaction state machine's status names only "B1/B2, N2, and F1/F2" although D2, D3, and D4
  were corrected inside it, and likewise awaits a "definitive closure review".
- The responsibility matrix's status names "B3 and cross-artifact N1" and awaits a "final closure
  review", two cycles behind.
- `docs/future/channel/README.md` repeats the matrix's stale "final closure review pending".

None changes a normative statement, but a status line is the first thing a later reader uses to
decide which review a document has survived.

## Probes and checks

All commands below ran from the detached isolated clone unless stated otherwise.

1. Pin and isolation: `git show -s --format="%H%n%cI%n%s" HEAD` returned the full requested pin,
   `2026-08-11T18:20:22+02:00`, and `docs(channel): close state event domain`;
   `git status --short --branch` returned only `## HEAD (no branch)`.
2. Pin-versus-head equivalence: SHA-256 of all thirteen `docs/future/channel/*.md` files and four
   retained attestations at the pin and at `e3b90a4` matched for every file except the redesign plan
   and `reviews/README.md`, whose diffs are handoff prose only.
3. `build/verify-channel-0.2-design.ps1` passed in the isolated clone and in the worktree, reporting
   11 required artifacts, C1-C12 headings/properties/scenarios/silence, total session/interaction
   event coverage, six session states, all 24 predecessor vectors, and four resolved rulings.
4. `build/verify-channel-0.2-design.ps1 -NegativeProbe` failed with exactly one message —
   `Channel 0.2 capability contract properties is missing '**Property C12-P1.**'` — and inner exit
   code 1, showing the structural property check can fail.
5. `build/verify-doc-links.ps1` passed 812 local links across 293 documents in the clone and 814
   across 293 in the worktree.
6. `build/verify-text.ps1` passed 876 UTF-8 files.
7. `build/verify-channel-vectors.ps1` passed 24 vectors covering 11 requirements, 12 protocol
   categories, seven process categories, and five failure domains.
8. Independent grid enumeration: a parser built from the published tables alone counted 6×6 session,
   6×6 initiator, and 5×6 recipient cells — 102 total — with zero empty cells and zero malformed
   rows, and confirmed the D1-D4 correction rows exist verbatim while the recipient structural row no
   longer lists `phase`.
9. Predecessor inventory: `conformance/channel-0.1-vectors.json` parsed to exactly 24 vectors,
   `CH-01-CORRELATION-ECHO` through `CH-24-FAILURE-DOMAIN-RELATIVITY`; the ledger's delivery
   `fallback` row and every declared disposition value were confirmed by direct scan.
10. Registry pins: SHA-256 of the selected Architecture 0.8 document, both stack READMEs, and the
    0.8 requirements file matched `Brontide-Architecture-Status.json` exactly.
11. Provenance probe: a targeted scan for external-phase provenance statements across all Channel 0.2
    artifacts found exactly one that permits a peer fault — the ledger `state-violation` row — against
    the contract's frameless rule and the recipient machine's `refused-local` row. This is T1.

### Genuine property-falsification attempt

An independent C4-P1 trace evaluator was written from the published C4 text, the admission/concurrency
rules, and the coverage grid, then run over a trace exercising a declared bound of two, a live replay
of an accepted identity, an out-of-order terminal, a bound refusal followed by later admission, and a
duplicate terminal. C4-P1 held on the control trace. Two named mutations were then applied: redispatch
of a replayed identity, and admission beyond the declared bound. The evaluator returned false for
both, failing on `no-identity-dispatched-twice` and `bounded-concurrency` respectively while the
`one-terminal-closes-one` conjunct stayed true — so each conjunct is separately load-bearing and the
property is capable of failing on the claims it makes.

That successful falsification does not cure T1. Every trace above keeps one terminal per interaction
and dispatches each identity once regardless of which provenance a phase refusal is given, so C4-P1
and C3-P1 both stay true while the frame/provenance decision remains contradicted between two
artifacts. This is the contract-silence class the separate completeness review exists to expose, and
it is the class that structural gates cannot reach.

## Closure consequence

This attestation closes every retained B1-B4, N1-N3, F1-F3, and D1-D5 finding, and records one new
blocking finding and three nonblocking findings. Under `docs/future/channel/reviews/README.md`, the
corrected design remains **does-not-conform**, Batch 2 must not begin, and no closure record may claim
a conforming first-batch foundation at `5cf42c4d97083324ffb8d6bd68491a145b8e611a`.
