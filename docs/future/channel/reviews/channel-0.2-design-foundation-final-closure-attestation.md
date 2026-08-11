# Channel 0.2 design-foundation final closure attestation

## Review identity and pin

- **Reviewer identity:** `agent:channel-0.2-design-foundation-final-closure-review-2026-08-11-1af7ba0-third`
- **Review date:** 2026-08-11
- **Reviewed commit:** `1af7ba0018c874750e346ee687f07ea1d302adef`
- **Reviewed commit date:** 2026-08-11T16:38:58+02:00
- **Reviewed commit subject:** `docs(channel): close remaining design gaps`
- **Isolation:** every repository read and probe used a fresh local clone created with
  `git clone --no-hardlinks --no-checkout` at
  `C:\Users\JakHoh\AppData\Local\Temp\brontide-channel02-final-23b80cb9ff5f4c96a2f063e75eee479f`,
  then checked out detached at the full reviewed commit. `HEAD` matched the full pin and the clone
  was clean before review. The shared worktree was used only to write this attestation.
- **Independence:** this reviewer identity is distinct from the design author, both correction
  actors, and both retained negative-review identities. The review ran in a new agent context, had
  no access to author private reasoning, and used repository evidence only.
- **Retained review input:** both negative attestations required by the review policy were read in
  full. The original review at `66729b097b032febf498dd907dd2387e2aebc2c5` supplied B1-B4; the
  first closure review at `e863bf15fca30466d6e262b0ea66b3c05bc384eb` supplied N1-N3. Neither
  prior verdict was adopted without independent reassessment.

## Overall verdict

**does-not-conform**

The corrected pin closes B1-B4 and N1-N3 as those findings were specifically framed. Final closure
still fails because this review found three new blocking defects:

1. a repeated accepted interaction identity received while the original interaction is still
   executing has no recipient transition or portable result;
2. recipient terminal state `faulted` combines a committed protocol fault and a local loss while the
   terminal-provenance table gives it no classification; and
3. the complete migration ledger still uses undeclared disposition `new` for `Outcome cancelled`.

The first-batch exit gate and the neutral brief's Batch 2 gate therefore remain closed. This
attestation does not authorize schema authoring, implementation, or a closure record.

No nonblocking finding is recorded.

## Architecture, targets, and predecessor evidence

### Current Architecture 0.8 and stack targets - conforms as context

`Brontide-Architecture-Status.json` selects Architecture 0.8 with status **Complete Draft (document
and implementation evidence complete; not ratified)** and records no latest ratified architecture.
The selected architecture hash recomputed as
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579` and matched the registry.

Architecture 0.8 keeps Channel provisional and outside Base. Its relevant requirements agree with
the first-batch direction: payload projection is covariant while authority/control positions fail
closed; Capabilities do not cross trust boundaries; admission belongs to the receiving domain;
semantic Outcome, protocol failure, and local loss remain distinct; interaction alone promises no
delivery, ordering, replay, cancellation, or lifecycle; and Component activation orders Local
Initialisation, Interconnection, optional Relational Initialisation, Ready, then one logical Release.

Both `Reference/README.md` and `Minimal/README.md` state `Designed for: Brontide Architecture 0.8`,
Complete Draft and not ratified, with partial implementations and explicitly labelled experiments.
Their registered hashes matched. The public-boundary policy keeps Portable Binding 0.1 experimental,
bounded, local-process, and non-hostile-peer: it assumes a trusted launcher/account and selected
executable, claims neither cryptographic peer identity nor multi-tenant isolation, and explicitly
does not promise retry, cancellation, streaming, ordering, or exactly-once execution.

### Channel 0.1 and PB8 predecessor evidence - conforms as retained evidence

The retained Channel 0.1 design note, draft contract, Architecture 0.8 requirements/risk ledger,
and all 24 predecessor vectors were reassessed as experimental evidence and migration input. The
vector file parsed and the repository verifier reported coverage of 11 requirements, 12 protocol
categories, 7 process categories, and 5 failure domains.

All nine Portable Binding neutral schemas parsed. The retained Reference, Minimal, and neutral PB8
closure attestations each record `conforms` at
`5150d6d774d683a6ce8e769f7472724d40f0baba` within Portable Binding 0.1's declared experimental
boundary, including closure of the effect-attribution defect class. That evidence supports the
predecessor semantics; it does not establish Channel 0.2 correctness or ratification.

### Decision 13 and CM3/CM4 - conforms

Decision 13 keeps fail-closed refusal in Portable Binding 0.1 and selects split readiness plus exact
relational lifecycle traffic for 0.2. The traffic must match the declared edge, direction,
initiating and receiving members, Operation, Capability, and input Shape; undeclared traffic is
refused before delivery, ordinary traffic remains closed until Release, and failure prevents Ready
and Release while preserving actual effects for cleanup or rollback.

CM3 remains an immutable effect-free planner. It declares the bounded lifecycle protocol and stage
order but does not execute lifecycle Operations, report Ready, Release, mutate an active generation,
or roll back. CM4 consumes the successful plan and executes the exact Local Initialisation,
Interconnection, optional Relational Initialisation, Ready, and one-logical-Release sequence. The
CM3 and CM4 fixtures parsed with 18 and 20 vectors respectively, including exact relational
admission, failed relational initialization, missing Ready, pre-Release ordinary refusal, and the
one-logical-Release barrier.

Channel C3/C7 and the interaction machine preserve this as one exact ordinary interaction class in
the pre-Ready window rather than a second envelope family. Channel consumes declaration and phase
facts and returns terminal/effect evidence; it creates neither Ready nor Release.

## Capability verdicts

| Capability | Verdict | Rationale |
| --- | --- | --- |
| C1 | **conforms** | Fixed and negotiated establishment produce one exact immutable profile before effects; mismatch, missing required facets, and downgrade refuse with `known-none`. |
| C2 | **conforms** | The session machine uses exactly six Channel-owned states, preserves drain and terminal monotonicity, and keeps activation phases external. |
| C3 | **conforms** | Class, direction, Operation/input contract, and explicit phase predicate are exact pre-dispatch inputs; false and unknown both refuse. |
| C4 | **does-not-conform** | Replay reservation prevents redispatch, but finding F1 leaves the result and existing interaction history undefined when the repeated identity arrives before the original terminal. The named replay behavior and deterministic vector requirement therefore lack one portable answer. |
| C5 | **conforms** | All finite profile bounds and positional Shape rules precede dispatch; projection remains payload-only and partial/oversized input never becomes a partial interaction. |
| C6 | **conforms** | B1 remains closed: structural authority failure is protocol rejection, while a structurally valid presentation denied by local policy is frameless `refused-local` with local `known-none`. |
| C7 | **conforms** | Relational initialization matches exactly one CM3 declaration in the Interconnection/pre-Ready window, uses separate narrow authority, and cannot create Ready or Release. |
| C8 | **conforms** | B2 and N2 remain closed: cancellation refusal has a producer, `cancel-pending` accepts peer fault, invalid cancellation control faults interaction scope with post-dispatch uncertainty, and acknowledgements remain nonterminal. |
| C9 | **does-not-conform** | Finding F2 leaves recipient `faulted` able to mean either a committed Channel fault or local loss while the provenance table classifies neither path under that state. C9-P1 cannot select exactly one provenance form for every terminal vector. |
| C10 | **does-not-conform** | Finding F2 prevents a complete deterministic observation for recipient `faulted`; an adapter must invent whether peer-Channel or local-loss provenance applies. Other certainty and profile-details boundaries conform. |
| C11 | **conforms** | Exact required/optional facets may add classes or stronger evidence but cannot redefine Channel identity, authority, terminal provenance, or certainty; retry remains a new identity. |
| C12 | **does-not-conform** | The neutral/dependency design is stack-independent and bounded, but F1 and F2 prevent one deterministic complete expected observation for named state-machine paths. A data-only vector cannot fill those fields without choosing unspecified behavior. |

Every C1-C12 section contains named scenarios, one Cn-P1 capability-wide property, an Evidence
paragraph, and explicit Silence. The verdicts above assess the complete package rather than heading
presence alone.

## State-machine and supporting-artifact verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state machine | **conforms** | The six states, fixed/negotiated paths, drain snapshot, close, loss/fault, representative illegal inputs, external phase guards, and terminal monotonicity agree with C1-C3. |
| Interaction state machine | **does-not-conform** | F1 leaves live replay without a recipient transition/result. F2 leaves one terminal recipient state without exclusive provenance. Other admission, authority, cancellation, concurrency, relational, terminal-race, loss, and sibling paths agree. |
| Responsibility matrix | **conforms** | Mechanical and semantic review found 37 concerns, 22 exact owner identifiers, no invalid owner cell, and no blank crossing artifact. Ready is consistently `component-management`; Interconnection/Release/withdrawal/cleanup are `portable-binding`; Relational Initialisation phase is `composition`; and the lifecycle declaration is `cm3-lifecycle-contract`. Carriers and consumers are not made co-owners. |
| Contract-completeness review | **does-not-conform** | Its author/correction conclusion is refuted by F1-F3. The required silence probes include replay only after semantic failure, not replay during execution; the terminal audit misses `faulted` provenance; and the migration correction audit stops before the invalid logical-item disposition. |
| Migration coverage | **does-not-conform** | B4's 24 vector rows and N3's three feature rows now use the declared vocabulary, and the inventories otherwise cover the predecessor Shapes/fields, kinds, states, taxonomies, limits, observations, resources, vectors, goldens, and consumers. F3 nevertheless leaves one table row outside the exact vocabulary. |
| Neutral contract/vector brief | **conforms as a brief; Batch 2 gate not satisfied** | The brief separates semantic schemas, distinct identities, closed controls, finite bounds, deterministic vector/property formats, parity fields, goldens, and an independent endpoint. Its own gate correctly requires contradiction-free machines, complete migration, and no blocking review finding; F1-F3 keep it closed. |

## B1-B4 closure decisions

### B1 - closed

Recipient `refused-local` now separates a structurally valid presentation denied by local authority
from structural/profile/state/Shape/authority-form protocol rejection. It emits no peer frame and
the terminal-provenance table classifies it as a local observation.

### B2 - closed

Recipient `executing` plus a structurally valid cancellation control denied by cancellation
authority remains `executing` and emits nonterminal `refused`; the initiator consumes accepted or
refused acknowledgement while remaining `cancel-pending`.

### B3 - closed as originally framed

Every responsibility row has one syntactically exact owner identifier and a nonblank crossing
artifact. No compound, conditional, slash, or semicolon owner cell remains.

### B4 - closed as originally framed

CH-01 through CH-24 occur exactly once and in order, and every vector row uses one of `retained`,
`replaced`, `moved`, `removed`, or `legacy-only`. F3 is a different full-ledger row outside the
vector section.

## N1-N3 closure decisions

### N1 - closed

Ready is consistently semantically owned by Component Management in the resolved plan ruling,
responsibility matrix, message-kind migration, state migration, feature migration, and readiness
wording. Portable Binding is only the carrier/gate where stated.

### N2 - closed as originally framed

The initiator now accepts a valid correlated peer fault from both `dispatched` and `cancel-pending`.
The recipient now maps invalid, unrecognized, unsupported, or wrongly scoped cancellation control
from `executing` or `cancel-requested` to interaction `faulted`, emits one interaction-scoped fault,
and ignores a later handler terminal. F2 is the distinct provenance omission created or exposed
after that transition.

### N3 - closed as originally framed

`streaming unsupported`, `ordering guarantee unsupported`, and `exactly-once unsupported` now use
exact disposition `retained`; their non-promise character remains in the treatment column. F3 is a
separate logical-item row.

## Four resolved owner-ruling verdicts

| Ruling | Verdict | Consistency assessment |
| --- | --- | --- |
| Core concurrency and cancellation | **does-not-conform** | Finite bounded unary concurrency, profile-selected bounds/cancellability, cancellation terminal meaning, and the N2 paths agree. F1 nevertheless leaves a replay arriving during an active concurrent interaction without a result or terminal-history rule. |
| Session-state ownership | **conforms** | Channel owns only `unestablished`, `establishing`, `established`, `draining`, `closed`, and `faulted`. The exact external owners and neutral crossings for Interconnection, Relational Initialisation, Ready, Release, withdrawal, and cleanup are carried consistently after N1. |
| Relational-initialization representation | **conforms** | The plan, C3/C7, interaction machine, matrix, completeness review, ledger, brief, Decision 13, and CM3/CM4 all use the ordinary interaction form with one exact pre-Ready class and no Component-to-Component binding kind. |
| Extension invariants | **conforms** | C11, the matrix, completeness review, migration rules, and brief permit additive exact facets while forbidding redefinition of identity, authority, terminal provenance, and effect certainty. |

## New blocking findings

### F1 - live replay has no recipient transition or portable interaction result

**Evidence:**

- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, **C4**, says reusing an accepted interaction
  identity in the same session is replay and never dispatches the handler again. C4-P1 quantifies
  every C4 vector and the named scenarios include `C4-replay-not-redispatched`.
- `Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`, **Concurrent interactions**, reserves the
  replay identity before dispatch. Its **Recipient transitions** defines a repeated request only
  from `any terminal`; it has no repeated-request transition from `executing` or
  `cancel-requested`.
- The same machine's **Admission order** detects replay, but does not say whether a live duplicate
  leaves the original executing, faults only the duplicate, faults the original interaction,
  faults the session, emits a replay fault, or is ignored.
- `Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md`, **Required silence probes**, answers
  replay after semantic failure but does not test replay before the original terminal.
- `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md`, **Vector format**, requires the exact
  transition, frame/provenance decision, terminal history, effect certainty, and sibling effects.

**Falsifying trace:** under an established session with spare `max-in-flight` capacity, interaction
identity `i1` passes admission and the recipient is `executing`. Before its handler returns, the peer
sends the same complete request with `i1`. Reservation proves the handler must not run twice, so the
no-redispatch part of C4-P1 survives. The published transition table supplies no next state, emitted
fact, effect certainty, or fate for the original handler. A neutral vector and two independent
implementations must invent one of several observably different histories.

**Impact:** this is a named replay/concurrency path, not speculative extension behavior. It fails
the interaction-machine agreement, completeness, deterministic-neutral-vector, core-concurrency
ruling, and first-batch exit gates. It is blocking.

### F2 - recipient `faulted` has two provenance meanings and no terminal-provenance row

**Evidence:**

- `Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`, **Local recipient states**, defines
  `faulted` as meaning either that one protocol fault was committed **or** local loss occurred.
- **Recipient transitions** reaches `faulted` for invalid cancellation control and internal Channel
  failure; **Concurrent interactions** also maps every nonterminal recipient interaction to
  `faulted` on a fatal session fault.
- **Terminal provenance** classifies `refused-local`, Outcome terminals,
  `peer-fault`/`rejected-protocol`, and `lost`, but contains no `faulted` row.
- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, **C9-P1**, requires every terminal vector to
  select exactly one of local refusal, semantic Outcome, peer protocol fault, or local loss. C10-P1
  requires a complete observation for that provenance form.

**Falsifying trace:** a recipient is `executing` and then either receives an invalid cancellation
control or loses the session before terminal commit. Both published paths enter `faulted`; the state
definition permits protocol-fault and local-loss provenance, while the provenance table selects
neither for `faulted`. The implementation must add an unrecorded discriminator or choose a
provenance from context that the terminal table does not define.

**Impact:** the same terminal label cannot yield one deterministic C9/C10 observation across native,
neutral, process, and cross-stack evidence. This fails the terminal-state, provenance,
completeness, and neutral-vector gates. It is blocking.

### F3 - one logical migration row still uses undeclared disposition `new`

**Evidence:**

- `Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md`, **7.6 Channel 0.1 to 0.2 migration
  ledger**, requires exactly one of `retained`, `replaced`, `moved`, `removed`, or `legacy-only` for
  every inventoried predecessor item.
- `Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md`, introduction, repeats exactly those five
  meanings.
- The ledger's **Logical Shapes and fields** table, whose first column is `Channel 0.1 item`, gives
  `Outcome cancelled` disposition `new`.
- The strengthened `verify-channel-0.2-design.ps1` validates vector and feature dispositions, but
  does not validate all disposition-bearing ledger tables; its normal pass therefore does not
  rebut this finding.

A mechanical scan of every Markdown table row with a `Disposition` cell found exactly one invalid
value: line 57, `Outcome cancelled`, `new`.

**Impact:** either `Outcome cancelled` is not a predecessor item and must be separated from the
migration inventory, or it needs one of the declared dispositions with an appropriate treatment.
As published, the supposedly exact ledger cannot be consumed under its own vocabulary and the
migration/Batch 2 gates fail. It is blocking.

## Nonblocking findings

None.

Architecture 0.8's Complete Draft/non-ratified status, the stacks' partial experimental delivery,
and Portable Binding 0.1's threat/resource limitations are scope conditions rather than findings.

## Checks and probes performed

1. Created the fresh no-hardlink clone, detached it at the full reviewed commit, verified subject,
   date, full `HEAD`, and clean status before evidence review.
2. Read `AGENTS.md` completely before reading the Channel review policy completely.
3. Read both retained negative attestations completely and independently reassessed B1-B4 and
   N1-N3.
4. Read the status registry and the current Architecture 0.8 status, authority/compatibility,
   Interaction/Execution/Outcome, Shape projection, Component activation, Channel direction,
   trust admission, conformance/silence, open-work, summary, and change sections. Recomputed and
   matched registry hashes for the architecture and both stack READMEs.
5. Read both stack targets and the complete public-boundary policy, including Portable Binding
   limits, trust assumptions, cleanup, replay, and non-promises.
6. Read the complete Channel 0.2 plan, C1-C12 contract, both state machines, all 37 responsibility
   rows, completeness review, every migration-ledger table, and neutral brief.
7. Read the retained Channel 0.1 design, draft contract, requirements/risk ledger, all 24 vectors,
   all nine Portable Binding schema declarations and schema README, and the three PB8 closure
   attestations.
8. Read Decision 13, the CM3/CM4 plan sections, both capability contracts, both completeness reviews,
   and relevant fixture entries. Parsed the CM3 and CM4 fixtures with counts 18 and 20.
9. Ran `build/verify-channel-0.2-design.ps1`: pass. Ran its `-NegativeProbe`: expected failure because
   `C12-P1` was removed in memory, proving the structural property-presence check can fail.
10. Ran `build/verify-doc-links.ps1`: 801 local links across 290 documents passed.
11. Ran `build/verify-channel-vectors.ps1`: 24 vectors covered 11 requirements, 12 protocol
    categories, 7 process categories, and 5 failure domains.
12. Parsed all nine Portable Binding schema JSON files, the 24 Channel vectors, and the CM3/CM4
    fixtures successfully.
13. Mechanically checked the responsibility matrix: 37 rows, 22 distinct exact owner identifiers,
    zero invalid owner cells, and zero blank crossing artifacts.
14. Mechanically scanned every migration table disposition against the declared five-value set:
    one failure, `Outcome cancelled` = `new`, recorded as F3.
15. **Capability-wide property-falsification attempt:** challenged C4-P1 with a repeated accepted
    identity while its recipient state was `executing`. The pre-dispatch replay reservation prevented
    a second dispatch, so the no-redispatch property itself was not falsified. The probe found zero
    live-replay transitions and one terminal-only replay transition, exposing F1's missing portable
    result. This was a genuine adversarial trace against the published property and state table, not
    an in-memory repair.
16. Mechanically enumerated recipient terminal states and the terminal-provenance section. The
    terminal set was `refused-local`, `rejected-protocol`, three Outcome terminals, and `faulted`;
    `faulted` was absent from the provenance table while its definition explicitly combined two
    forms, confirming F2.

No design repair, implementation edit, build, schema authoring, or closure record was made. No
implementation claim beyond the retained PB8 evidence is made by this design-only review.
