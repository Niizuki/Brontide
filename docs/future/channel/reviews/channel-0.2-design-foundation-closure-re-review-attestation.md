# Channel 0.2 design-foundation closure re-review attestation

Date: 2026-08-13

Reviewer identity: `agent:claude-opus-5-channel-0.2-closure-re-review-2026-08-13-11ba93b`

Reviewed commit: `11ba93bddbd38f03df59b4afc5166d7c6991c865`
(`docs(channel): widen the invalid cancelled terminal to its class`)

Overall verdict: **does-not-conform**

## Independence and isolation — declared deviation

This attestation does **not** satisfy the isolation clause of
[`reviews/README.md`](./README.md), and the deviation is recorded here rather than glossed, because an
attestation that overstates its own independence is the defect class this programme exists to catch.

What holds:

- The reviewer identity is distinct from the correction author and from all five retained reviewers
  (`agent:channel-0.2-design-foundation-review-2026-08-11`,
  `agent:channel-0.2-design-foundation-closure-review-2026-08-11-e863bf1`,
  `agent:channel-0.2-design-foundation-final-closure-review-2026-08-11-1af7ba0-third`,
  `codex-channel-0.2-definitive-closure-2026-08-11-1b7c5fd`, and
  `agent:claude-opus-5-channel-0.2-totality-closure-2026-08-11-5cf42c4`).
- No author-session private reasoning was available or used. Only repository evidence was read.
- The five retained attestations were read completely before the corrected design was judged. Their
  SHA-256 values are unchanged from the values the totality attestation recorded
  (`a2f9f7cd…9070`, `ff50b7ff…46ec`, `31e47931…b93f2`, `b37d810d…c90f`), and the totality attestation
  itself hashes `0eee8d277e344f8a7c988076ce31b127397c05bffb506b6a4de3d10b3c3994e9`.

What does not hold:

- **No fresh isolated clone was used.** All reads and probes ran in the shared worktree at branch head
  `57c25d7`, not in a detached checkout of the pin. Equivalence was established by diff rather than by
  isolation: `git diff --stat 11ba93b..HEAD -- docs/future/channel/` reports exactly two changed files,
  the redesign plan (one line, a status line) and `reviews/README.md`, both handoff prose. The nine
  Channel 0.2 design artifacts, the retained 0.1 predecessor artifacts, and the requirements/risk
  ledger are byte-identical at both commits. The verdict below therefore applies to the design content
  at the pin, and equally to branch head.
- **The reviewing context was not fresh.** Before the review began, this session had already cleaned up
  merged branches and read `docs/future/README.md`, `reviews/README.md`, and
  `binding/portable/open-decisions.md` while answering a question about what work came next. That
  reading is repository evidence, not private author reasoning, but it is not the cold start the policy
  requires.
- **The verifier in the worktree post-dates the pin.** `build/verify-channel-0.2-design.ps1` gained 75
  lines after `11ba93b`. Probe results below come from the *newer* verifier. This makes the structural
  checks stronger than those available at the pin, not weaker, but it is a difference worth naming.

Under the policy's own definition — reviewer identity differs, fresh isolated context, no access to
author reasoning — the second condition fails. **This attestation should be treated as a substantive
technical review whose independence is partial.** It is sufficient to establish the blocking finding
below, since a blocking finding needs only to be true. It is *not* sufficient to close the first batch
had the verdict been positive, and a future conforming attestation should come from a genuinely cold,
isolated reviewer.

## Overall decision

The T1-T4 correction pass is complete and correct. All four findings from the totality closure review
are closed in the artifacts they were raised against, verified individually below.

The first batch nevertheless does not close. One new blocking finding, **R1**, is recorded: the
capability contract and the recipient coverage grid disagree about a conformant initiator's
cancellation that races the recipient's admission, and the disagreement assigns peer-fault provenance
to an event no endpoint did wrong. It is the same class as T1 — two artifacts assigning one fact
different provenance — reached at a different point in the machine.

The ordinary structural gates pass, including the design verifier, its negative probe, links, text,
and the predecessor vector inventory. They establish headings, vocabulary, inventory subsets, and
pinned correction strings. They do not compare what two artifacts say about one event, which is where
R1 lives.

## Architecture, targets, and predecessor evidence

### Current architecture and implementation targets — conforms as context

`Brontide-Architecture-Status.json` selects Architecture 0.8 at
`docs/current/architecture/Brontide-Architecture-0.8.md`, status **Complete Draft (document and
implementation evidence complete; not ratified)**, hash
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579`. `latestRatifiedArchitecture` is
`null` with an explicit rationale. Both stacks record `designedFor: 0.8` with their own 0.8 matrices
and milestone ledgers. Neither stack claims a Channel 0.2 implementation, and the first-batch package
claims neither ratification nor runtime conformance. This is a design-foundation verdict only.

### Decision 13 and CM3/CM4 — conforms

Decision 13 is recorded 2026-08-11: Portable Binding 0.1 retains its fail-closed refusal of every CM3
group declaring a bounded lifecycle protocol, and a versioned 0.2 separates establishment from
readiness and adds exact declared relational lifecycle traffic before Ready. The first batch carries
the semantic ruling through a distinct interaction class rather than the new envelope kind Option B's
wording anticipated. C3, C7, the interaction machine's **Relational initialization** section, the
responsibility matrix, and the plan all carry that representation identically, with the pre-Ready
window (`interconnected && !ready`), exact declaration match, separate relational authority, and the
rule that Channel creates neither Ready nor Release.

### Predecessor evidence — conforms

`build/verify-channel-vectors.ps1` passes: 24 vectors covering 11 requirements, 12 protocol
categories, 7 process categories, and 5 failure domains. Every vector has exactly one disposition row
in the ledger within the declared five-value vocabulary. The retained Channel 0.1 design note, draft
contract, and requirements/risk ledger remain untouched predecessor evidence at their own pins and are
not treated as proof of successor correctness.

## Capability verdicts

| Capability | Verdict | Rationale |
| --- | --- | --- |
| C1 | **conforms** | One immutable profile precedes dispatchability; fixed and negotiated paths must produce equal normative records, with a field absent from the fixed path declared a contract defect rather than realization freedom; unknown versions, required features, classes, and authority modes refuse with no implicit downgrade and no in-place renegotiation. |
| C2 | **conforms** | Six states, only `established` admitting new interactions, external phases explicitly excluded. Duplicate local or peer drain moves `draining` to `faulted` with one session-scoped `state-violation`, preserving the first snapshot and rewriting no interaction certainty. The session totality rule closes the event domain without a permissive default and names its one nonfatal exception. |
| C3 | **conforms** | The D3 correction holds at both endpoints: a locally false or unknown guard is frameless refusal with `known-none` "at either endpoint, including the recipient's independently derived external phase", and the recipient table routes that case to `refused-local`. T1's contradicting ledger row is now corrected (see below). |
| C4 | **conforms** | Live replay commits one interaction-scoped `replay-detected` without redispatch; admission reserves an in-flight position atomically; bound refusals consume no lasting replay entry; a new session cannot inherit an old replay window. C4-P1's three conjuncts were exercised (see probes). |
| C5 | **conforms** | Positional payload/authority classification, finite declared bounds, and structural validation all precede dispatch; authority positions never project; a partial or oversized frame never becomes a partial interaction. |
| C6 | **conforms** | Authority is evaluated per interaction after structural admission and before dispatch; local denial is frameless with `known-none`; no Capability, Constraint expression, or derivation chain crosses a trust boundary; unknown authority structure never projects and never permits. |
| C7 | **conforms** | Relational initialization matches exactly one CM3 declaration inside the Interconnection/pre-Ready window, carries separate narrow authority, returns actual terminal provenance to CM4 cleanup or rollback, and cannot itself produce Ready or Release. |
| C8 | **does-not-conform** | This is R1. C8 enumerates exactly four cancellation-control conditions producing a peer protocol fault — structurally invalid, unrecognized, unsupported, or wrongly scoped — and a legal cancellation that arrives while the recipient is still `validating` is none of them, yet the recipient grid routes it to `rejected-protocol`. Everything else in C8 holds: the late-traffic latch is finite, acknowledgement multiplicity is total, and the unrequested-`cancelled` terminal is invalid at both endpoints. |
| C9 | **conforms as written** | The four provenance forms are exclusive in the contract, both machines, and the terminal-provenance table; unknown peer-fault categories fault locally as `unrecognized-peer-fault` with no answering frame. R1 does not sit inside C9's text, but it produces a violation of C9-P1 in the grid: a recipient-local timing artifact is accepted as a peer Channel statement. |
| C10 | **conforms as written** | Certainty is `known-none` only with evidence dispatch did not occur, `known` with profile-owned details, or `unknown` with a reason. R1 has a secondary C10 consequence recorded as a sub-point rather than a separate finding. |
| C11 | **conforms** | Required facets must be supported exactly; optional facets are ignored only under a declared additive-absence rule; no facet may redefine identity, authority, terminal provenance, or certainty; retry is a new identity with optional causal attribution, and reusing an identity is replay rather than retry. |
| C12 | **does-not-conform** | C12-P1 requires every neutral vector to have one deterministic expected portable observation, and C12 declares that a vector with an unspecified expectation is invalid evidence rather than per-stack freedom. Under R1 a cancellation vector's frame/provenance outcome depends on recipient-side admission timing that neither endpoint observes or controls, so it cannot state one deterministic expectation. |

Every C1-C12 section carries named scenarios, one `Cn-P1` property, required evidence, and explicit
silence. These verdicts assess whether the statements cover the behavioral domain, not whether the
headings exist.

## Area verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state machine | **conforms** | Six states, explicit legal and refused/illegal tables, a closed-world totality rule naming its one nonfatal exception, and a drain protocol that is symmetric but exactly-once per endpoint history. Duplicate drain, premature close, terminal-state input, and loss each have exactly one result. S1-S6 are stated as falsifiable properties. |
| Interaction state machine | **does-not-conform** | Initiator and recipient tables are separate and exclusive, the late-traffic latch is finite with exactly three values, the T3 correction is present and correctly widened to its class, and the admission order fixes the zero-effect boundary. The recipient table has **no row at all** for `validating` plus cancellation control; the only normative statement for that pair is the grid cell R1 records. |
| State/event totality | **does-not-conform** | Independent enumeration reproduces 6×6 session, 6×6 initiator, and 5×6 recipient cells — 102 total, zero empty. Totality is genuine. But R1's cell is *covered and wrong* rather than uncovered: the grid supplies a route that contradicts C8. Total coverage is not the same as correct coverage, and this grid's own purpose statement claims only the former. |
| Responsibility matrix | **conforms** | Every one of the 37 rows carries one exact owner identifier matching `^[a-z0-9-]+$`, a nonblank neutral crossing artifact, and separate consumer and carrier columns with no co-owners. Ready is `component-management`; Interconnection, Release, withdrawal, and cleanup are `portable-binding`; the Relational Initialisation phase is `composition`. |
| Contract-completeness review | **does-not-conform** | Its silence inventory records `cancel before dispatch` as "local refusal/no cancel frame; admission may itself be abandoned locally", which is the *initiator's* pre-dispatch race. The symmetric recipient-side race — cancel arriving before the recipient finishes admitting — is absent from the inventory. This is the review's own subject matter: a silence about a silence. |
| Migration coverage | **conforms** | 24 vectors, twelve protocol categories, seven process categories, five failure domains, ten limits, ten features, and every observation field carry a disposition inside the declared five-value vocabulary. T1 and T2 are corrected in place. |
| Neutral contract/vector brief | **conforms as a brief; Batch 2 gate not satisfied** | It requires separated data-only schemas, typed identity spaces, closed enums, finite bounds, deterministic expected observations, one property plus one named mutation per capability, and independent native/neutral endpoints. Its entry gate's first criterion — "no unresolved internal contradiction" across C1-C12, both machines, and the grid — is exactly what R1 fails, so the gate correctly holds schema authoring closed. |

## Retained-finding closure decisions

| Finding | Decision | Evidence at `11ba93b` |
| --- | --- | --- |
| B1 | **closed** | Recipient `refused-local` remains the frameless route for a structurally valid authority presentation denied by local policy; structural authority failure remains `rejected-protocol`. |
| B2 | **closed** | Recipient cancellation-authority denial produces `cancel-refused` with a nonterminal `refused` acknowledgement; the initiator records it as a distinct nonterminal state. |
| B3 | **closed** | Every matrix row has one exact owner identifier and one neutral crossing; the identifier form is enforced mechanically. |
| B4 | **closed** | CH-01 through CH-24 occur exactly once with declared dispositions; re-verified by `verify-channel-vectors.ps1`. |
| N1 | **closed** | The exact Ready owner is carried identically by the plan, matrix, ledger, and completeness review. |
| N2 | **closed** | Invalid, unrecognized, unsupported, or wrongly scoped cancellation control is one interaction-scoped recipient fault with post-dispatch uncertainty preserved. |
| N3 | **closed** | The three retained non-promises use disposition `retained` and remain explicit non-promises. |
| F1 | **closed** | Live replay selects `peer-fault`, commits `replay-detected`, never redispatches, and ignores a later handler terminal. |
| F2 | **closed** | Recipient terminal states are exclusive `peer-fault` and `lost` with matching provenance rows. |
| F3 | **closed** | No bold disposition outside the declared five-value vocabulary remains in the ledger. |
| D1 | **closed** | `draining` plus duplicate local or peer drain is a legal-table transition to `faulted` with one session-scoped `state-violation` and the first snapshot preserved. |
| D2 | **closed** | Unsolicited acknowledgement faults; `cancel-pending` plus `accepted`/`refused` selects distinct states; any further acknowledgement faults with effects preserved. |
| D3 | **closed** | Closed in the contract and interaction machine at the previous pin, and now closed in the migration ledger too — see T1. |
| D4 | **closed** | The `late-traffic-fault` latch has exactly three values with one possible scoped fault emission and no answering-fault loop. |
| D5 | **closed** | The delivery `fallback` observation has a **moved** row naming the delivery/retry facet owner and keeping `none` attributable. |
| **T1** | **closed** | The ledger's `state-violation` row now reads: "Scope identifies session versus interaction. An external phase refusal is never this fault: a false or unknown predicate is a frameless local refusal at either endpoint under C3." The forbidden provenance is gone and the row now points at C3 rather than contradicting it. |
| **T2** | **closed** | The `replay-detected` row now reads: "A repeated accepted identity received while its original interaction is nonterminal; no redispatch. A repeat arriving after that interaction is terminal follows the late-traffic latch as `state-violation` instead." The applicability condition is narrowed to the nonterminal window and the terminal case is named. |
| **T3** | **closed** | The interaction machine has an explicit row — `executing` or `cancel-refused` \| handler reports cancellation completed with no cancellation request in force \| `peer-fault` \| commit one interaction-scoped `internal-channel-failure` and record the discarded handler terminal. The pin commit widened it from `cancel-refused` alone to the class, and C8 and the recipient grid carry the same rule. The result is stated rather than left to the catch-all. |
| **T4** | **closed** | All six first-batch artifact status lines now say "a fresh independent closure re-review"; none names a superseded cycle. `docs/future/channel/README.md` matches. The design verifier rejects a status block naming a superseded cycle, so the correction is mechanically held. |

## Four resolved owner-ruling verdicts

| Ruling | Verdict | Consistency assessment |
| --- | --- | --- |
| Core concurrency and cancellation | **conforms as a ruling; R1 sits inside its subject matter** | Finite bounded unary concurrency and optional cancellation with fixed terminal meaning are core in C4, C8, both machines, the grid, the matrix, and the ledger. The ruling itself is represented consistently everywhere. R1 is not a departure from the ruling but an incomplete working-out of it: the ruling says profiles do not redefine terminality, and R1 is a case where the package has not fixed what terminality a legal cancellation reaches. |
| Session-state ownership | **conforms** | The six Channel session states and external ownership of Interconnection, Relational Initialisation, Ready, Release, withdrawal, and cleanup are stated identically in the plan, C2, the session machine, the matrix's boundary ruling, and the ledger. |
| Relational-initialization representation | **conforms** | One exact pre-Ready interaction class, never a second envelope family, in the plan, C3, C7, the interaction machine, the matrix, the completeness review, the ledger, and the brief, and consistent with Decision 13, CM3, and CM4. |
| Extension invariants | **conforms** | C11, the matrix extension-hook ruling, and the ledger's feature rows consistently permit additive facets and forbid redefining identity, authority, terminal provenance, or certainty. |

## Blocking findings

### R1 — a conformant cancellation that races recipient admission is classified as a peer protocol fault

**Statement.** The contract makes an initiator's cancellation legal on a purely local precondition,
and the recipient coverage grid makes that same cancellation a protocol violation whenever it arrives
before the recipient finishes admitting the request. Nothing observable lets the initiator avoid the
race, and the two artifacts assign the resulting fact different provenance.

**Evidence:**

- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, C8: "Exactly one cancellation request may be sent
  for a nonterminal dispatched interaction." The initiator's precondition is its own local state.
- `Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`, **Local initiator states**: `dispatched`
  means "A complete request was committed to the transport/direct seam."
- `Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`, **Cancellation**, item 4: "exactly one
  cancellation request is legal, from initiator `dispatched` and recipient `executing`." The two
  endpoint conditions are stated as if simultaneous; they are not.
- The recipient transition table has **no row** whose `From` is `validating` and whose event is a
  cancellation control. The transition `validating` → `executing` emits no frame, and no
  request-accepted acknowledgement exists anywhere in the design, so the initiator cannot observe when
  the recipient becomes `executing`.
- `Brontide-Channel-0.2-State-Event-Coverage-0.1.md`, **Recipient interaction coverage grid**, row
  ``| `unseen` / `validating` | … | wrong class/state → `rejected-protocol` | … |`` supplies the only
  normative route: a bounded peer protocol fault.
- C8's failure clause enumerates exactly four cancellation-control conditions producing a peer
  protocol fault — "structurally invalid, unrecognized, unsupported, or wrongly scoped". The raced
  cancellation is none of these: it is well-formed, recognized, supported by the profile, and names
  the correct interaction identity in the correct session.
- The **Terminal provenance** table classifies recipient `rejected-protocol` as a peer Channel
  statement. The initiator transition row for `cancel-pending` plus a valid correlated peer protocol
  fault selects `peer-fault`. Both endpoints therefore record that the peer erred.
- Cross-capability invariant 4 requires one fact to have one provenance and one semantic owner.
  C9-P1 forbids any field that permits a local inference to be accepted as a peer statement.

**Falsifying trace.** A profile supports cancellation. The initiator admits an ordinary interaction,
commits the request to the seam (`dispatched`), and — entirely within C8 — commits exactly one
cancellation request, reaching `cancel-pending`. Both frames are in flight. The recipient accepts the
request and enters `validating`. The cancellation arrives before validation completes. The grid routes
it to `rejected-protocol`, emitting a bounded peer protocol fault; the initiator accepts that fault
and terminates at `peer-fault`. Had the recipient's validation completed a moment earlier, the same
two frames would have produced `cancel-requested` at the recipient and an ordinary acknowledgement at
the initiator. One conformant initiator, one supported control, two contradictory terminal histories,
selected by recipient-internal timing.

**Why the properties stay green.** C8-P1 holds on this trace: the interaction has exactly one terminal
history (`peer-fault`) and nothing non-semantic is recorded as success — confirmed by executing the
trace against an independently written C8-P1 evaluator (probe 6). C3-P1 holds because the class,
direction, and phase predicate all matched. C4-P1 holds because one identity was dispatched once and
one terminal closed one interaction. The defect is a contract *silence* about a race, and the
capability properties are structurally blind to it, exactly as Decision 10 describes and as T1
demonstrated at a different point in the machine.

**Secondary consequence (C10).** The recipient in `validating` is inside admission-order steps 1-9,
which "cannot cause a provider/application handler effect", so the handler provably did not begin and
the true certainty is `known-none`. The initiator's rule records `known-none` "only when fault
explicitly proves handler did not begin". Recipient `rejected-protocol` is defined as "handler did not
begin", so a fault carrying that classification should narrow correctly — but nothing in the package
*requires* the emitted fault to carry it, so an implementation may record `unknown` where the evidence
supports `known-none`. This is a weaker point than the provenance defect and is recorded as part of R1
rather than as a separate finding.

**Impact.** C8 and C12 fail as recorded above; the interaction state machine, the state/event totality
area, and the completeness review fail; C9-P1 and cross-capability invariant 4 are violated by the
grid cell. The neutral brief's Batch 2 entry gate criterion 1 — "no unresolved internal contradiction
or uncovered recognized event" — is not satisfied. **This is blocking.**

**Correction direction (not a repair; the reviewer does not repair).** The design already contains the
principle it needs. The interaction machine's cancellation item 9 states that "a cancel request racing
a terminal Outcome accepts whichever terminal fact is valid first, while the late control is recorded
and does not replace it" — races are recorded, not faulted. The completeness review already handles the
initiator-side mirror ("cancel before dispatch → local refusal/no cancel frame"). What is missing is
the recipient-side admission race. Whichever way the owner resolves it — hold the control until
admission resolves, refuse it framelessly, or keep the fault and add a request-accepted
acknowledgement so the race becomes avoidable — the resolution must be stated in C8, given a recipient
transition row, and reflected in the grid cell and the completeness review's silence inventory.

## Nonblocking findings

### R2 — the two endpoint cancellation preconditions are written as if simultaneous

Interaction machine, **Cancellation**, item 4 reads "exactly one cancellation request is legal, from
initiator `dispatched` and recipient `executing`". These are two local states in two endpoints with no
synchronising event between them, and the sentence's grammar invites a reader to treat them as one
condition. This phrasing is how R1 stayed invisible across five reviews. Even after R1 is resolved,
recommend stating the two preconditions separately and naming what happens when they disagree.

### R3 — `unseen` and `validating` share a grid row but are not alike for cancellation control

The recipient grid groups `unseen` / `validating` into one row. For cancellation control the two are
materially different: at `unseen` no interaction with that identity has been accepted, so there is
nothing to correlate and `rejected-protocol` is defensible; at `validating` the identity is known and
the interaction exists. Recommend splitting the row for the cancellation-control column when R1 is
resolved, so the defensible case and the contested one stop sharing a verdict.

## Probes and checks

All commands ran in the shared worktree at `57c25d7`; see the isolation deviation above.

1. **Pin equivalence.** `git diff --stat 11ba93b..HEAD -- docs/future/channel/` returned exactly two
   files: the redesign plan (1 line) and `reviews/README.md` (9 lines), both handoff prose. All nine
   Channel 0.2 design artifacts, the retained predecessor artifacts, and the requirements/risk ledger
   are byte-identical at both commits.
2. **Artifact hashes at review time.** Capability contract
   `adec946372d07c4b3b848c5a1daed9d8b70f1de6a34a4499eab0b986cf8c4385`; interaction state machine
   `57abefa6aff66703b8439b07c84850bbf0da359f030856e8025132e21caabe67`; session state machine
   `a6c1513ba2a135014dacef5f22352f41be7f165fdbaba1e706cd288e78058f44`; state/event coverage
   `673cd810097e5bb11208ccb06c62178a2c6693f6a8b696c3baa1ffdcb2a7488b`; responsibility matrix
   `9cd77979465954929c710d06e72d8c5035f960c84bddaceabd3793fe0572a09c`; migration ledger
   `3c6089cd5fa128796de7aa52fb593f8f84bf2562d5f9da10545abdf165629c97`; completeness review
   `f44223290572439981e3a2e371d19df0e0a5e3020d4ace19147e41cd97650ffa`; neutral brief
   `050682d00bcde1f636a0b25a7a6207994cad04f15e93afa73d854fa4b4f4fd2d`.
3. **`build/verify-channel-0.2-design.ps1`** passed: 11 required artifacts, C1-C12 with
   properties/scenarios/silence, total session/interaction event coverage, 6 session states, all 24
   predecessor vectors dispositioned, 4 owner rulings resolved, independent review still pending.
4. **`build/verify-channel-0.2-design.ps1 -NegativeProbe`** failed with exactly one message —
   `FAIL: Channel 0.2 capability contract properties is missing '**Property C12-P1.**'` — and inner
   exit code 1, confirming the structural property check can fail.
5. **Independent grid enumeration.** A parser written from the published markdown tables alone (not
   from the design verifier) counted session 6 rows × 6 event columns = 36, initiator 6 × 6 = 36,
   recipient 5 × 6 = 30; **102 cells, 0 empty**. This reproduces the totality attestation's count from
   an independently written tool.
6. **Genuine property-falsification attempt — C8-P1.** An evaluator was written from the published C8
   text, the initiator/recipient transition tables, and the late-traffic latch rule, then run over
   eight traces. C8-P1 held on all controls: ordinary success, cancel-then-success, duplicate terminal
   (latch settles to `fault-committed`, one terminal), double cancel control, unrequested `cancelled`
   terminal, timeout after dispatch, and late traffic after terminal. Three named mutations were then
   applied. `reopen-terminal` (letting a late duplicate replace the first history) failed conjunct A
   only. `cancel-as-success` (recording a cancellation control as semantic success) failed conjunct B
   only. `timeout-as-success` failed both. Each conjunct is therefore separately load-bearing and
   C8-P1 is capable of failing on the claims it makes.
   **R1's trace was run through the same evaluator and C8-P1 returned true** (final
   `rejected-protocol`, one terminal, latch `clear`) — the property cannot separate the defect, which
   is why R1 is a contract-silence finding rather than a property violation.
7. **`build/verify-doc-links.ps1`** passed: 819 local links across 294 documents.
8. **`build/verify-text.ps1`** passed: 879 UTF-8 files.
9. **`build/verify-channel-vectors.ps1`** passed: 24 vectors covering 11 requirements, 12 protocol
   categories, 7 process categories, 5 failure domains.
10. **`build/verify-interchange.ps1`** was run as the complete repository gate. Every stage observed
    passed, including SDK policy, text, links, Channel 0.2 design, requirement evidence, adversarial
    vectors, the Architecture 0.8 handoff/audit/D1-D6/closure chain, Channel vectors, portable binding
    (9 schemas, 84 vectors, 6 golden encodings), neutral-provider independence, binding measurement,
    the 45-project graph, Architecture 0.7 comparison (15 vectors, no disagreements), and both stacks'
    suites (Reference 784 passed / 1 skipped across 8 assemblies; Minimal 740 passed / 1 skipped
    across 8 assemblies). The single skip in each stack is `Cbi51_C1_restart_policy_is_explicit_and_bounded`.
11. **Retained attestation integrity.** All five retained attestations hash to the values the
    preceding reviews recorded; none has been altered.
12. **Registry pins.** `Brontide-Architecture-Status.json` selects Architecture 0.8 as Complete Draft
    with a matching document hash and records no ratified architecture.
13. **Targeted race scan.** Every cancellation-related statement across the contract, both machines,
    the grid, the completeness review, and the ledger was collected and compared. The initiator-side
    pre-dispatch race is handled in the completeness review; the cancel-versus-terminal race is handled
    in cancellation item 9; the recipient-side admission race appears nowhere except the grid cell that
    faults it. This is R1.

## Closure consequence

This attestation closes T1, T2, T3, and T4, and records one new blocking finding, R1, with two
nonblocking findings, R2 and R3. Under [`reviews/README.md`](./README.md), the design remains
**does-not-conform**, Batch 2 must not begin, and no closure record may claim a conforming first-batch
foundation at `11ba93bddbd38f03df59b4afc5166d7c6991c865`.

Two things are required before the next attempt, not one. R1 must be corrected contract-first with a
failing check added for it, as the policy requires. Separately, the next closure attestation must come
from a reviewer in a genuinely fresh isolated context, because this one was not — and on the evidence
of R1, which survived five reviews inside a grid everyone had already enumerated, the isolation
requirement is earning its place rather than decorating the process.
