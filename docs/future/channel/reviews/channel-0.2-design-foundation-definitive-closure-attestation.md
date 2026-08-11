# Channel 0.2 design-foundation definitive closure attestation

Date: 2026-08-11

Reviewer identity: `codex-channel-0.2-definitive-closure-2026-08-11-1b7c5fd`

Reviewed commit: `1b7c5fdea0dc555a64152eea055fcebad053cf90`
(`docs(channel): complete recipient state model`, committed 2026-08-11T17:21:30+02:00)

Overall verdict: **does-not-conform**

## Independence and isolation

This is a fresh fourth review. The reviewer identity is distinct from the design actor and from the
three retained reviewers. No author-session private reasoning was available or used. The review used
only repository evidence at the pinned commit.

All reads and probes ran in a fresh non-local clone created with `git clone --no-local --no-checkout`
at
`C:\Users\JakHoh\AppData\Local\Temp\brontide-channel02-definitive-review-1b7c5fd`, followed by a
detached checkout of the full commit above. `git status --short --branch` reported only
`## HEAD (no branch)`. The shared worktree was used solely to write this attestation.

`AGENTS.md` was read completely before `docs/future/channel/reviews/README.md`. The three retained
negative attestations were then read completely before the corrected design was judged. Their
SHA-256 values at the pin were:

- original: `a2f9f7cdc77b3f934a59fbe14c240e9c2f0bfe5864ca0dd702376ad455899070`;
- first closure: `ff50b7ff974eb60042dac0be186a2da21d2e8e382ecd48c2656566ca565046ec`;
- final closure: `31e479312813bbf9a1912d6eee2949bdc9e2739d3d9c18afdc0a99157dbb93f2`.

## Overall decision

The corrected commit closes B1-B4, N1-N3, and F1-F3 as those findings were framed. It does not close
the first batch. Five new blocking findings, D1-D5 below, leave declared session and interaction
events without one portable result, contradict the required provenance of a receiver-local phase
refusal, and omit a predecessor delivery-fallback fact from the migration inventory.

The ordinary structural gates pass, but those gates establish headings, vocabulary, links, and
inventory subsets rather than semantic completeness. Batch 2 therefore remains closed under the
review policy and the plan's first-batch exit gate.

## Architecture, targets, and predecessor evidence

### Current architecture and implementation targets - conforms as context

`Brontide-Architecture-Status.json` selects Architecture 0.8 at
`docs/current/architecture/Brontide-Architecture-0.8.md` with status **Complete Draft (document and
implementation evidence complete; not ratified)**. Its recorded architecture hash matches the file:
`cac9a02ea1221c3ee73c482d0624ae8da45757b31a35c1efd1061d4028b18579`. The registry says there is no
ratified Brontide architecture.

Architecture 0.8's Operation/Interaction/Execution/Outcome distinctions, positional Shape rules,
static binding rule, Channel boundary, extension versioning, and trust-boundary admission rules are
the correct current context. The first-batch design does not claim ratification or implementation
and does not silently treat later architecture as a ratified release.

Both stack READMEs say `Designed for: Brontide Architecture 0.8`, Complete Draft, not ratified, and
`Partial implementation with explicitly labelled experiments`. Their registry hashes match their
files (`4fa7c85c...be91` for Reference and `c59afab6...a6b` for Minimal). Their retained 0.7 and
Portable Binding/Component Management evidence remains explicitly experimental or historical;
neither stack claims a Channel 0.2 implementation. That limitation is honest and this attestation
is a design-foundation verdict, not runtime conformance.

### Decision 13 and CM3/CM4 - conforms

Decision 13 retains the 0.1 refusal for protocol-bearing CM3 groups and selects, for 0.2, separate
readiness plus a declared relational-protocol interaction. CM3 remains an immutable, effect-free
planner that supplies the exact edge, direction, members, Operation, Capability, input Shape, and
stage plan. CM4 consumes that plan and orders Local Initialisation, Interconnection, Relational
Initialisation, Ready, and one logical Release without accepting caller-invented readiness.

The CM3 and CM4 contracts, completeness reviews, and 18- and 20-vector fixtures agree with C3/C7,
the responsibility matrix, and the ordinary-interaction representation. D3 concerns the failure
classification when a receiver's local phase predicate is false; it does not alter the chosen
relational representation or ownership.

### Predecessor evidence - historical evidence valid; migration use incomplete

The Channel 0.1 design note, draft contract, requirements/risk ledger, all 24 Channel vectors, nine
Portable Binding schemas, vector fixtures, and the retained PB8 closure attestations were
reassessed. The 24-vector inventory remains executable predecessor evidence, and the PB8
attestations remain historical decisions at their pins and within their explicitly narrowed 0.1
resource floor. They do not prove successor correctness.

They also do not prove that the 0.2 migration inventory is complete. D5 identifies a delivery-
fallback observation required by the Channel 0.1 contract and implemented by both legacy Cooling
hosts, but absent from the Portable Binding neutral observation schema and therefore absent from the
0.2 migration table. This is a contract-silence defect shared by predecessor artifacts, not grounds
to rewrite or move the pinned predecessor evidence.

## Capability verdicts

| Capability | Verdict | Rationale |
| --- | --- | --- |
| C1 | **conforms** | Fixed and negotiated establishment produce one exact immutable profile before interaction effects. Version/facet mismatch and downgrade refuse with `known-none`. |
| C2 | **does-not-conform** | D1 leaves a duplicate drain control with no exact protocol-fault, transmission, or session-result decision. C2-P1 permits either unchanged or `faulted`, so it does not resolve the contract/machine gap. |
| C3 | **does-not-conform** | D3 maps the same receiver-local false/unknown external phase to frameless `refused-local` in C3 but to `rejected-protocol`/peer-Channel provenance in the recipient machine. |
| C4 | **conforms** | The F1 correction gives live replay one interaction-scoped `replay-detected` terminal, prevents redispatch, preserves unknown effects, and ignores the late handler terminal. Finite concurrency and sibling isolation remain explicit. |
| C5 | **conforms** | Finite bounds and positional Shape validation precede dispatch; payload projection remains distinct from exact authority/control forms; partial or oversized data cannot create a partial interaction. |
| C6 | **conforms** | B1 remains closed. A structurally valid authority presentation denied by local policy selects frameless recipient `refused-local` with `known-none`; structural authority failure remains protocol rejection. |
| C7 | **conforms** | Relational initialization matches one exact CM3 declaration in the Interconnection/pre-Ready window, uses separate authority, and cannot create Ready or Release. |
| C8 | **does-not-conform** | D2 leaves wrong-state and contradictory cancellation acknowledgements without one portable result. D4 says a duplicate terminal is a protocol fault but does not define the fault's scope, frame/provenance decision, or observation. |
| C9 | **does-not-conform** | D2-D4 prevent exclusive provenance for named peer-control/fault paths. Most directly, D3 allows a local phase inference to be represented as a peer Channel statement. |
| C10 | **does-not-conform** | D1-D4 leave required observations indeterminate, and D5 omits the predecessor delivery-fallback fact that C10 says an owning extension may supply. Complete provenance-relative observations cannot yet be authored. |
| C11 | **conforms** | Exact facets may add interaction classes and stronger evidence without redefining core identities, authority, terminal provenance, or effect certainty; retry uses a new identity. |
| C12 | **does-not-conform** | The neutral design remains independent and data-only, but D1-D5 prevent deterministic, complete expected data for the affected traces and predecessor migration. |

Every C1-C12 section contains named scenarios, a Cn-P1 property, Evidence, and explicit Silence. The
verdicts above assess whether those statements cover the behavioral domain, not merely whether the
required headings exist.

## Area verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state machine | **does-not-conform** | The six-state ownership and ordinary establishment/drain/close/loss topology conform, but D1 leaves `draining + drain` outside the transition tables and without the exact protocol-fault behavior required by C2. |
| Interaction state machine | **does-not-conform** | F1/F2 are corrected, but D2-D4 leave cancellation acknowledgements, receiver-local phase refusal, and duplicate-terminal protocol-fault behavior without one complete transition/provenance result. |
| Responsibility matrix | **conforms** | A mechanical and semantic pass found 37 distinct concerns, 22 exact owner identifiers, no duplicate concern, no blank owner, and no blank crossing. Ready is `component-management`; its carriers are not co-owners. |
| Contract-completeness review | **does-not-conform** | D1, D2, and D4 survive the required duplicate/late-traffic and cancellation probes; D3 survives the phase/provenance audit; D5 survives the predecessor-field audit. The correction conclusion is therefore incomplete. |
| Migration coverage | **does-not-conform** | All 139 present disposition rows use the declared vocabulary, all CH-01-CH-24 rows are exact, and F3 is closed. D5 nevertheless shows that a known 0.1 delivery-fallback fact has no row or disposition. |
| Neutral contract/vector brief | **conforms as a brief; Batch 2 gate not satisfied** | It requires separated data-only schemas, typed identities, finite controls, deterministic observations, C1-C12 properties, independent native/neutral endpoints, and dependency guards. Its own entry gate correctly disallows implementation while the machines, completeness review, and migration ledger have blocking findings. |

## Retained-finding closure decisions

| Finding | Decision | Evidence at `1b7c5fd` |
| --- | --- | --- |
| B1 | **closed as framed** | Recipient `refused-local` now separates structurally valid local authority denial from `rejected-protocol`, emits no peer frame, and has local-observation provenance. D3 is the distinct external-phase path. |
| B2 | **closed as framed** | Recipient cancellation-authority denial from `executing` remains `executing` and emits nonterminal `refused`; initiator acknowledgement remains `cancel-pending`. D2 concerns wrong-state and multiple acknowledgements, not the missing producer B2 identified. |
| B3 | **closed as framed** | All 37 responsibility rows have one exact owner identifier and a nonblank neutral crossing; no compound or conditional owner cell remains. |
| B4 | **closed as framed** | CH-01 through CH-24 occur exactly once and use only `retained`, `replaced`, `moved`, `removed`, or `legacy-only`. D5 is an omitted predecessor fact, not an invalid vector disposition. |
| N1 | **closed** | Ready is consistently owned by `component-management`; Portable Binding is only a carrier/gate where named. |
| N2 | **closed as framed** | Initiator `cancel-pending` accepts a correlated peer fault. Recipient invalid/unrecognized/unsupported/wrongly scoped cancellation control from `executing` or `cancel-requested` selects `peer-fault`, emits one scoped fault, and ignores a late handler terminal. |
| N3 | **closed as framed** | `streaming unsupported`, `ordering guarantee unsupported`, and `exactly-once unsupported` use disposition `retained` while remaining explicit non-promises. |
| F1 | **closed** | A repeated accepted identity during `executing` or `cancel-requested` selects `peer-fault`, commits `replay-detected`, never redispatches, keeps effect certainty unknown unless narrowed, and ignores a later handler terminal. |
| F2 | **closed** | Recipient `faulted` has been replaced by exclusive terminal `peer-fault` and `lost` states with matching provenance rows and transitions. |
| F3 | **closed** | The logical `Outcome cancelled` row now uses declared disposition `replaced`. A full-table scan found no remaining invalid bold disposition. |

Closing these findings does not imply conformance when a fresh full-scope review finds a distinct
blocker. None of D1-D5 is a relabeling of a retained finding.

## Four resolved owner-ruling verdicts

| Ruling | Verdict | Consistency assessment |
| --- | --- | --- |
| Core concurrency and cancellation | **does-not-conform** | B2/N2/F1 are corrected and finite bounded concurrency remains core, but D2 and D4 leave core cancellation/terminal control outcomes incomplete. |
| Session-state ownership | **conforms** | Channel owns only `unestablished`, `establishing`, `established`, `draining`, `closed`, and `faulted`; Interconnection, Relational Initialisation, Ready, Release, withdrawal, and cleanup retain their exact external owners and crossings. |
| Relational-initialization representation | **conforms** | The plan, C3/C7, interaction machine, matrix, completeness review, ledger, brief, Decision 13, CM3, and CM4 use one exact pre-Ready ordinary interaction class rather than another envelope family. |
| Extension invariants | **conforms** | C11 and the supporting artifacts consistently permit exact additive facets but forbid redefining identity, authority, terminal provenance, or effect certainty. |

## New blocking findings

### D1 - duplicate drain has no exact protocol-fault result

**Evidence:**

- C2, **Failure and uncertainty**, says: `Illegal or duplicate control is a protocol fault.`
- The session machine's legal table contains the first `established` to `draining` transition but no
  `draining` plus drain transition. Its refused/illegal table also contains no such row.
- **Drain protocol** instead says subsequent drain controls keep the session in `draining` and are
  recorded as duplicates. It does not say whether a fault is emitted, whether the duplicate is only
  a local observation, or whether the session enters `faulted`.
- C2-P1 permits any unlisted input either to leave the state unchanged or enter `faulted`. Thus the
  property cannot choose between the observably different answers, while the neutral brief requires
  a deterministic transition and frame/provenance decision.

**Falsifying trace:** establish a session, accept one peer drain, then receive the same peer drain
again while `draining`. Keeping `draining` silently, keeping `draining` while emitting a fault, and
faulting the session all fit some part of the published package. A vector cannot choose one without
inventing behavior.

**Impact:** C2, the session machine, C9/C10 observations, C12 determinism, the completeness review,
and the first-batch state-machine gate fail. This is blocking.

### D2 - cancellation acknowledgements have no wrong-state or multiplicity semantics

**Evidence:**

- C8 permits `accepted` and `refused` cancellation acknowledgements and requires wrongly scoped
  cancellation control to produce one interaction-scoped peer protocol fault.
- The initiator transition table has one acknowledgement row only from `cancel-pending`, returning
  to the same undifferentiated `cancel-pending` state. It has no acknowledgement transition from
  `dispatched` and no state or fact recording which acknowledgement was already accepted.
- Consequently, a structurally valid unsolicited acknowledgement in `dispatched` has no result, and
  `accepted` followed by `refused` (or the reverse) repeatedly matches the same row without a rule
  saying whether the later contradictory peer statement is ignored, recorded, or faulted.
- The completeness review establishes only that an acknowledgement is nonterminal; it does not
  probe wrong-state, duplicate, or contradictory acknowledgements.

**Falsifying traces:** (1) while `dispatched`, before any cancel request, receive a correlated
`accepted` acknowledgement; (2) send a cancel request, receive `accepted`, then receive `refused`
while still `cancel-pending`. The contract/machine pair supplies no single next state,
frame/provenance action, or observation for either complete trace.

**Impact:** C8-C10, the interaction machine, the core-cancellation ruling, C12, and the completeness
gate fail. This is blocking.

### D3 - receiver-local false phase has incompatible provenance

**Evidence:**

- C3 says a **locally** false or unknown phase guard is a frameless refusal with `known-none`.
- The session machine says the peer independently validates the external predicate from its own
  profile-owned state and that false/unknown refuses before dispatch.
- The recipient transition table groups `phase` failure with structural/profile/state failures and
  selects terminal `rejected-protocol`.
- The recipient-state definition allows a bounded peer protocol fault to be emitted, and the
  terminal-provenance table classifies recipient `rejected-protocol` as a peer Channel statement.
- The migration ledger further says an external-phase refusal may be frameless local or peer fault
  depending on detection point, but C3 does not define that split.

**Falsifying trace:** a sender admits a structurally valid ordinary request under its `released`
snapshot; the receiver independently derives its local `released` fact as false or unknown. C3
requires receiver-local frameless `refused-local`/`known-none`, while the recipient table requires
`rejected-protocol` and permits a peer fault. Both prevent handler dispatch, so C3-P1 stays green and
cannot detect the provenance contradiction.

**Impact:** C3, C9, C10, C12, the interaction machine, the completeness review, and deterministic
phase vectors fail. This is blocking. It is distinct from B1, which concerned local authority
evaluation after a structurally valid authority presentation.

### D4 - duplicate terminal is named a protocol fault without a portable fault action

**Evidence:**

- C8 says duplicate terminal facts are protocol faults and do not replace the first accepted
  terminal history.
- The initiator transition table says `any terminal` plus `any further terminal/control` leaves the
  terminal unchanged and records duplicate/late traffic. It does not define fault scope, whether a
  peer-fault frame is emitted, whether only a local observation is made, or whether the session is
  affected.
- The neutral brief explicitly requires duplicate-terminal vectors with exact transition,
  frame/provenance decision, terminal history, and effect certainty.
- C8-P1 proves only that at most one terminal is accepted and controls do not become success; it
  cannot distinguish the missing fault actions.

**Falsifying trace:** accept `outcome-succeeded` for an interaction, then receive a second validly
correlated `outcome-failed`. The first history remains authoritative in every plausible
implementation, while the required protocol-fault observation and transmission remain
underdetermined.

**Impact:** C8-C10, the interaction machine, the core-cancellation/terminal ruling, C12, and the
duplicate-traffic completeness gate fail. This is blocking and distinct from F1's repeated request
while the original interaction was live.

### D5 - predecessor delivery-fallback fact has no migration disposition

**Evidence:**

- The Channel 0.1 design note says retry, interruption, and fallback are recorded as facts; its crash
  evidence expressly asserts `RetryCount = 0`, `Fallback = "none"`, and interruption.
- Draft Channel Contract 0.1 section 6 repeats that process failure records interruption, retry
  count, and fallback. PB-50 says retry and fallback facts are explicit.
- Both retained Cooling hosts have a normative `Fallback` observation field and their native tests
  assert `none`.
- `binding/portable/schemas/binding-observation.json` contains `retryCount` and `interrupted` but no
  delivery-fallback field; PB-50's expected object likewise omits it despite its prose.
- The 0.2 ledger's **Observation-field migration** table dispositions `retryCount` and `interrupted`
  but has no delivery-fallback row. Its Strong-Kleene `CH-09` fallback row is an authority-expression
  rule, not this delivery observation.
- The redesign plan explicitly preserves interruption/retry/fallback honesty and requires every 0.1
  contract element and observation field to receive a disposition. C10 also names retry/fallback
  facts supplied by an owning extension.

This finding concerns delivery-attempt fallback observation, not the separately declared non-goal
for resource ownership/lifetime/release/fallback.

**Impact:** the ledger's claim to cover every predecessor contract element and observation field is
false. An adapter cannot know whether the fact is retained, moved to `delivery-facet`/another owner,
replaced, removed, or legacy-only. Migration coverage, C10/C12, the completeness review, and the
first-batch exit gate fail. This is blocking and distinct from B4/F3, which concerned invalid
disposition values on rows that existed.

## Nonblocking findings

None.

## Probes and checks

All commands below ran from the detached isolated clone.

1. Pin and isolation: `git show -s --format="%H%n%cI%n%s" HEAD` returned the full requested pin,
   date, and subject; `git status --short --branch` returned only detached HEAD.
2. `build/verify-channel-0.2-design.ps1` passed and reported 10 required artifacts, C1-C12
   headings/properties/scenarios/silence, six session states, all 24 predecessor vectors, and four
   resolved rulings.
3. Its `-NegativeProbe` failed as intended with missing `**Property C12-P1.**` and inner exit code
   1, showing the structural property check can fail.
4. `build/verify-doc-links.ps1` passed 803 local links across 291 documents.
5. `build/verify-channel-vectors.ps1` passed 24 vectors covering 11 requirements, 12 protocol
   categories, seven process categories, and five failure domains.
6. JSON parsing succeeded for 22 relevant files with zero failures: all nine Portable Binding
   schemas, all Portable Binding vector files, the 24-vector Channel fixture, and the CM3/CM4
   fixtures. Counts were Channel 24, CM3 18, and CM4 20.
7. A complete Markdown disposition scan found 139 present rows, zero invalid bold dispositions, and
   CH-01-CH-24 exactly once. The observation-section comparison found `retryCount` and `interrupted`
   in both schema and ledger, no delivery `fallback`, while both legacy host types and the
   predecessor contract contain it.
8. A responsibility-matrix parser found 37 rows, 22 unique exact owners, zero blank owners, and zero
   duplicate concerns.
9. Targeted state/provenance probes confirmed: the C2 duplicate-control fault sentence and
   duplicate-drain idempotence sentence both exist; no `draining + drain` transition exists; the
   initiator has zero `dispatched + acknowledgement` rows and one undifferentiated
   `cancel-pending + accepted/refused acknowledgement` row; C3's frameless local phase rule and the
   recipient `phase -> rejected-protocol -> peer statement` path both exist; and C8's duplicate-
   terminal fault sentence coexists with the generic duplicate/late-recorded terminal row.
10. SHA-256 probes verified the status registry, selected Architecture 0.8, and both stack README
    pins. All relevant JSON status data parsed successfully.

### Genuine property-falsification attempt

A small independent C8-P1 trace evaluator accepted the published cancellation history
`cancellation-control(nonterminal, not success) -> outcome(success terminal)`. Two deliberate
mutations were then applied: treating the cancellation acknowledgement as terminal semantic success,
and accepting two terminal Outcomes. The evaluator returned `False` for both mutations after
returning `True` for the control trace. The property is therefore capable of failing on the claims it
makes.

That successful falsification does not cure D2 or D4. Both counter-traces preserve at most one
accepted terminal and never turn a control into success, so C8-P1 can remain true while the required
control/fault result is absent. This is precisely the contract-silence gap the separate completeness
review was required to expose.

## Closure consequence

This attestation closes all retained B1-B4/N1-N3/F1-F3 findings but records five new blocking
findings. Under `docs/future/channel/reviews/README.md`, the corrected design remains
**does-not-conform**, Batch 2 must not begin, and no closure record may claim a conforming first-batch
foundation at `1b7c5fdea0dc555a64152eea055fcebad053cf90`.
