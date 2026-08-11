# Channel 0.2 design-foundation attestation

## Review identity and pin

- **Reviewer identity:** `agent:channel-0.2-design-foundation-review-2026-08-11`
- **Review date:** 2026-08-11
- **Reviewed commit:** `66729b097b032febf498dd907dd2387e2aebc2c5`
- **Isolation:** a fresh local clone was created at
  `C:\Users\JakHoh\AppData\Local\Temp\brontide-channel02-review-66729b0-019fefaa-2`
  with `git clone --no-hardlinks`, then checked out detached at the full reviewed commit. The clone
  was clean before and after all inspection and probes. Every repository read and probe used that
  clone. This attestation is the only file written in the shared worktree.
- **Independence:** the reviewer identity is distinct from the design author, ran in a fresh agent
  context, had no access to the author's private reasoning, and assessed repository evidence only.
  Per the Channel review policy, the only earlier attestations inspected were the three retained PB8
  closure attestations explicitly required as predecessor evidence. No other review attestation was
  inspected.

## Overall verdict

**does-not-conform**

The first-batch foundation has four blocking findings. The interaction machine does not preserve
the contract's local-denial provenance, does not define how a recipient refuses cancellation, the
responsibility matrix assigns several concerns to more than one semantic owner, and 13 of the 24
Channel 0.1 vector rows use dispositions outside the ledger's declared disposition vocabulary.
These defects fail first-batch exit-gate items 2, 3, 4, 6, and 8. Batch 2 must not begin at this pin.

No nonblocking finding is recorded.

## Architecture, targets, and retained evidence

### Current architecture and implementation targets — conforms

`Brontide-Architecture-Status.json` selects Architecture 0.8 as **Complete Draft (document and
implementation evidence complete; not ratified)** and records no latest ratified architecture. The
registry SHA-256 for the Architecture 0.8 document matched. Both `Reference/README.md` and
`Minimal/README.md` matched their registry hashes, state `Designed for: Brontide Architecture 0.8`,
retain the Complete Draft/non-ratified qualification, and describe partial implementations with
explicit experiments.

The Channel design remains a provisional extension design rather than a Base or ratification claim.
Its authority/payload separation, trust-boundary prohibition on Capability transfer, Shape
position rules, relational lifecycle gates, and separation of semantic Outcome, protocol fault, and
local loss are directionally consistent with Architecture 0.8. The public-boundary policy correctly
retains Portable Binding 0.1's bounded-CBOR, trusted local launcher/account, non-hostile-peer, and
non-multi-tenant limitations, and its explicit non-promises for ordering, retry, cancellation,
streaming, and exactly-once execution.

### Channel 0.1 and PB8 predecessor evidence — conforms as retained evidence

The retained Channel 0.1 design note, draft contract, Architecture 0.8 requirements/risk ledger, and
all 24 `conformance/channel-0.1-vectors.json` vectors were reviewed as experimental predecessor
evidence, not as a limit on the Channel 0.2 review. The 24-vector JSON and all nine Portable Binding
neutral schema JSON files parsed successfully.

The retained Reference, Minimal, and neutral PB8 closure attestations each record `conforms` within
Portable Binding 0.1's declared experimental boundary and close their recorded PB8 findings,
including the effect-attribution defect class. This is credible retained evidence for preserved
0.1 behavior; it does not establish Channel 0.2 conformance or erase 0.1's documented limitations.

### Decision 13 and CM3/CM4 — conforms

Decision 13 selected split readiness plus declared relational protocol traffic. CM3 requires the
bounded lifecycle declaration and the order Local Initialisation, Interconnection, optional
Relational Initialisation, then Ready. CM4 admits only the exact declared edge, direction, members,
Operation, capability requirement, and input Shape during the relational stage; failure prevents
Ready and Release, while Release alone opens ordinary interaction.

The Channel 0.2 foundation preserves those semantics. It deliberately realizes relational traffic
as a state-gated class of the ordinary interaction machine rather than Decision 13's suggested new
envelope kind, a representation change explicitly recorded by the completeness review. Channel
consumes the lifecycle declaration and phase facts and returns terminal/effect evidence; it does not
create Ready, Release, rollback, or lifecycle authority.

## Capability verdicts

Every C1-C12 section contains named scenarios, one named capability-wide property, an evidence
section, and an explicit silence section. The verdicts below assess whether the complete first-batch
package represents each capability consistently, not whether the future implementations already
exist.

| Capability | Verdict | Rationale |
| --- | --- | --- |
| C1 | **conforms** | Channel, profile, application-contract, encoding, and facet versions are separated; fixed and negotiated establishment must yield one equal immutable profile; mismatch and downgrade refuse before interaction effects. |
| C2 | **conforms** | The contract and session machine use exactly `unestablished`, `establishing`, `established`, `draining`, `closed`, and `faulted`; legal, illegal, drain, loss, and terminal paths preserve monotonicity and do not create external activation facts. |
| C3 | **conforms** | Interaction class, role/direction, Operation/input contract, and external phase predicate are explicit. `false` and `unknown` refuse before dispatch, and successful session establishment supplies no phase or authority fact. |
| C4 | **conforms** | Session-scoped interaction identity, atomic finite in-flight reservation, replay admission before dispatch, out-of-order sibling completion, and first accepted terminal history are defined without importing fairness or ordering. |
| C5 | **conforms** | Payload projection is position-specific; authority/control positions require exact recognized forms; every normative bound is finite and established before dispatch; foreign runtime data and unbounded diagnostics are excluded. |
| C6 | **does-not-conform** | The contract requires a valid presentation denied by local policy to remain a frameless local observation with `known-none`, but the recipient transition table groups the local authority decision with protocol-validation failures and selects `rejected-protocol`, which may emit a peer fault. Finding B1 contradicts C6's authority/effect boundary and prevents one conforming recipient result. |
| C7 | **conforms** | Relational initialization is one exact ordinary-machine interaction class, matches one CM3 declaration in the Interconnection/pre-Ready window, uses separate narrow authority, and cannot itself create Ready or Release. |
| C8 | **does-not-conform** | Terminal uniqueness, cancellation races, and nonterminal acknowledgements are otherwise coherent, but the recipient machine has no transition for cancellation-authority denial or for emitting the specified `refused` acknowledgement while execution continues. Finding B2 leaves a declared legal path undefined. |
| C9 | **does-not-conform** | The four provenance forms are well defined in the capability text, but B1 permits a local authority inference to enter `rejected-protocol`/peer-statement provenance, contrary to C9-P1's exclusive classification. |
| C10 | **conforms** | Observations separate establishment/interaction identity, dispatch, provenance, and `known-none`/`known`/`unknown` certainty; profile-owned details remain nested and possible post-dispatch effects are not guessed away. |
| C11 | **conforms** | Exact required/optional facets may add classes or stronger evidence but cannot change core identity, authority, terminal provenance, or certainty; retry is a new interaction identity rather than replay. |
| C12 | **conforms** | The design requires data-only canonical artifacts, deterministic expectations, the same vector groups across independent Reference/Minimal/neutral endpoints, dependency guards, and native/process/cross-stack evidence without a shared production runtime. |

## State-machine and supporting-artifact verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state machine | **conforms** | All six ruled states, establishment alternatives, draining snapshot, close, fault/loss, representative illegal inputs, terminal monotonicity, and fixed/negotiated equivalence are explicit and agree with C1-C3. External lifecycle and binding facts are not promoted to session state. |
| Interaction state machine | **does-not-conform** | Finding B1 makes recipient authority-denial provenance contradict C6/C9. Finding B2 leaves the recipient side of cancellation refusal undefined. Other dispatch, replay, concurrency, race, loss, terminal, relational, and sibling-isolation paths are internally coherent. |
| Responsibility matrix | **does-not-conform** | Finding B3 violates the matrix rule and first-batch gate that every semantic concern has exactly one owner. The neutral crossing artifacts and dependency directions are generally named, but a downstream machine-readable owner inventory cannot select one owner for the affected rows without inventing a ruling. |
| Contract-completeness review | **does-not-conform** | Its author-pass conclusion that no semantic concern is unowned is refuted by B3, and its C6/C8 passes do not expose B1 or B2. Of its five residual risks, cancellation produces B2; concurrency imports no scheduling promise; the brief constrains phase predicates to closed facts; effect details remain profile-owned; and vector perspective/detection fields address the peer/local structural boundary. |
| Migration coverage | **does-not-conform** | The inventories otherwise cover the logical Shapes/fields, message kinds, states, categories, failure domains, limits/features, observations/resource subfields, all 24 vector prefixes, goldens/pins, and named consumers. Finding B4 nevertheless means 13 vector entries lack one disposition from the formally declared vocabulary. |
| Neutral contract/vector brief | **conforms; Batch 2 gate not satisfied** | The brief is stack-neutral, separates schemas by semantic role, requires closed control enums and finite bounds, defines perspective/provenance/effect expectations, enumerates C1-C12 vector groups and execution modes, and forbids either stack in the neutral endpoint. Its own Batch 2 gate correctly remains closed because B1-B4 leave contradictions, multi-owner concerns, invalid migration dispositions, and blocking findings. |

## Four resolved owner rulings

| Ruling | Verdict | Consistency assessment |
| --- | --- | --- |
| Core concurrency and cancellation | **does-not-conform** | Finite bounded unary concurrency and profile-selected cancellability are consistent across the plan, contract, machines, matrix, completeness review, migration ledger, and brief. Cancellation's fixed meaning is not completely represented, however, because B2 omits the recipient denial/refused-acknowledgement path. |
| Session-state ownership | **conforms** | The exact six Channel states appear consistently. Interconnection, Relational Initialisation, Ready, Release, withdrawal, cleanup, and rollback remain external facts consumed through explicit predicates/observations. B3 concerns which external system owns several of those facts, not whether Channel owns them. |
| Relational-initialization representation | **conforms** | The plan, C3/C7, interaction machine, selected matrix ruling, completeness review, migration ledger, and brief all use one ordinary interaction machine with a distinct exact pre-Ready class, not a second envelope family. Lifecycle/CM retain declaration and readiness semantics. |
| Extension invariants | **conforms** | C11, the matrix's selected ruling, the completeness review, migration rules, and neutral brief consistently allow exact additive facets while forbidding redefinition of identities, authority, terminal provenance, or effect certainty. |

## Blocking findings

### B1 — recipient local authority denial can become a peer protocol statement

**Evidence:**

- `docs/future/channel/Brontide-Channel-0.2-Capability-Contract-0.1.md`, **C6 — authority is local and
  boundary-relative**, says every interaction evaluates authority locally after structural admission
  and before dispatch; **Authority and effect boundary** says a local denial emits no denial message
  and records `known-none`; **Failure and uncertainty** reserves a protocol fault for a forbidden
  authority form; C6-P1 requires every denial to retain the local decision point.
- The same file, **C9 — peer statements and local failures retain distinct provenance**, defines a
  frameless local pre-dispatch refusal separately from a peer protocol fault, and C9-P1 forbids a
  local inference from being accepted as a peer statement.
- `docs/future/channel/Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`, **Admission order**, separates authority
  structure at step 8 from the local authority decision at step 9. Its **Recipient transitions** then
  combines `...authority... check fails` with every structural/profile/state failure and transitions
  to terminal `rejected-protocol`. The **Local recipient states** definition permits that state to
  emit a bounded peer protocol fault, and **Terminal provenance** classifies recipient
  `rejected-protocol` as a peer Channel statement.

**Falsifying trace:** a complete request for an established exact profile has an allowed class and
direction, true phase, valid payload Shape/bounds, and a structurally valid authority presentation;
the receiver's step-9 local policy returns `denied`. No handler runs. C6 requires
`local-observation/no-peer-frame/known-none`; the published recipient transition selects
`rejected-protocol/peer-fault-permitted`. The executable review probe therefore returned
`FALSIFIED_BY_PUBLISHED_RECIPIENT_TRANSITION` for C9-P1. The same trace contradicts C6's normative
authority/effect boundary.

**Impact:** neutral schemas, vectors, and two independent implementations cannot choose one
recipient terminal/provenance behavior without contradicting another normative first-batch
artifact. This is blocking.

### B2 — cancellation refusal has no recipient-side transition

**Evidence:**

- `docs/future/channel/Brontide-Channel-0.2-Capability-Contract-0.1.md`, **C8 — every interaction has one terminal
  history; cancellation is explicit but not magic**, permits the peer to acknowledge cancellation as
  `accepted` or `refused`, keeps either acknowledgement nonterminal, and gives cancellation a
  separate authority requirement.
- `docs/future/channel/Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`, **Cancellation**, says cancellation may be
  denied independently and that `refused` leaves execution under the ordinary terminal contract.
  **Initiator transitions** consumes an accepted or refused acknowledgement while remaining
  `cancel-pending`.
- The same file's **Recipient transitions** has only `executing` plus a *valid* cancellation control
  to `cancel-requested`; it has no event/guard/result for cancellation-authority denial, no
  transition that leaves execution in `executing`, and no producer path for the promised `refused`
  acknowledgement.

**Impact:** the two endpoints do not share a complete legal cancellation history, so a vector author
or implementation must invent recipient behavior. This fails the state-machine agreement and
completeness gates and is blocking.

### B3 — responsibility rows do not select exactly one semantic owner

**Evidence:**

- `docs/future/channel/Brontide-Channel-0.2-Responsibility-Matrix-0.1.md`, **Rule**, says every semantic fact has one
  owner. Its **Boundary verification required** requires a machine-readable inventory with one owner
  per normative field and rejection of duplicate/missing owners.
- `docs/future/channel/Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md`, **7.9 First-batch exit gate**, requires
  every semantic concern to have exactly one owner. The neutral brief repeats this as a Batch 2 gate.
- The matrix's **Ownership matrix** assigns direct compound owners to, among others,
  `Interconnection` (`Component Management / Portable Binding`), `Relational Initialisation phase`
  and `Ready` (`Component Management / Composition`), `Release / ordinary gate` and `Ordinary
  interaction eligibility` (`Composition / Portable Binding`), `Binding withdrawal and cleanup`
  (`Portable Binding / Composition`), `Cross-trust admission and local grants` (`receiving authority
  domain / Component Management`), and `Semantic failure` (`Operation contract / responding
  Actor`). Other cells use conditional `or`, semicolon, or unspecific slash syntax.

A mechanical review of all 35 rows found 21 owner cells with multi-owner, conditional-owner, or
compound slash/semicolon syntax. Some rows plainly combine separable facts, but the matrix does not
split those facts or map each one to a single owner; selecting one would require reviewer guesswork.

**Impact:** the Batch 2 schemas cannot name the required responsibility-matrix owner, and the neutral
verifier cannot reject duplicate ownership, until exact single-owner rows exist. This is blocking.

### B4 — 13 vector rows use dispositions outside the ledger's declared vocabulary

**Evidence:**

- `docs/future/channel/Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md`, **7.6 Migration ledger**, requires every
  0.1 Shape, field, kind, state, category, failure domain, limit, vector, and observation field to
  receive exactly one of `retained`, `replaced`, `moved`, `removed`, or `legacy-only`.
- `docs/future/channel/Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md`, introduction, repeats those five
  disposition meanings. Its **Channel 0.1 vector migration** table nevertheless labels CH-01,
  CH-02, CH-03, CH-05, CH-06, CH-15, CH-16, CH-17, CH-22, CH-23, and CH-24 as `revised`, and CH-04
  and CH-21 as `split`. Neither term is in the declared disposition set.
- The redesign plan separately permits mapping a vector to retained evidence, a revised vector, or
  retirement. That describes an evidence target, but the ledger puts `revised`/`split` in the
  `Disposition` column and provides no formal mapping from either term to one of the five required
  dispositions.

The mechanical disposition check parsed all 24 vector rows and found 13 outside the declared set.
All 24 predecessor vector prefixes were present with no missing or extra prefix, so this is a
disposition defect rather than an inventory omission.

**Impact:** the formal migration and first-batch gate cannot determine whether those vectors are
retained, replaced, moved, removed, or legacy-only. This is blocking.

## Nonblocking findings

None.

## Checks and probes performed

1. Read `AGENTS.md` completely before the review policy, then read
   `docs/future/channel/reviews/README.md` completely.
2. Verified the isolated clone's full `HEAD`, detached pin, and clean status before and after review.
3. Read the status registry and complete Architecture 0.8 document; recalculated and matched the
   registry SHA-256 for the architecture and both stack READMEs; confirmed current `0.8`, Complete
   Draft/non-ratified, and latest ratified `none`.
4. Read both stack READMEs and the complete public-boundary policy, including the local Architecture
   0.8 target, partial/experimental status, and trust, transport, limit, process, and non-promise
   boundaries.
5. Read the complete Channel 0.2 plan; retained Channel 0.1 design, contract, requirements ledger,
   and all 24 vectors; all nine Portable Binding neutral schemas and their schema README; the PB8
   review policy and only its three closure attestations; Decision 13; CM3/CM4 capability contracts
   and their completeness reviews; C1-C12; both state machines; responsibility matrix; completeness
   review; migration ledger; and neutral brief.
6. Parsed the 24 predecessor vectors and nine Portable Binding schemas as JSON successfully.
7. Mechanically checked C1-C12: all 12 have named scenarios, their matching `C<n>-P1` property,
   Evidence, and Silence sections.
8. Manually traversed session and interaction legal, refused/illegal, drain, cancellation, replay,
   concurrency, terminal-race, loss, late-frame, relational, and sibling-interaction paths against
   the C-item properties.
9. Attempted to falsify C9-P1 with a structurally valid but locally denied authority presentation;
   the published recipient transition did falsify the no-local-inference-as-peer-statement property,
   as recorded in B1. The same trace contradicts C6's authority/effect boundary.
10. Parsed all 35 responsibility rows and challenged every owner and crossing artifact; 21 owner
    cells use compound, conditional, slash, or semicolon syntax, with the blocking exact-owner cases
    recorded in B3.
11. Parsed all 24 vector migration rows against the declared five-value vocabulary; 13 failed. A
    separate predecessor-prefix comparison found all 24 present, with no missing or extra prefix.
12. Checked 30 local Markdown links across the first-batch artifacts; all resolved at the pin.
13. Compared the four owner rulings across the plan, capability contract, state machines, matrix,
    completeness review, migration ledger, and neutral brief, with the results recorded above.

This was a design-foundation review. No design repair, implementation edit, build, or conformance
gate was performed, and no implementation claim beyond the retained PB8 evidence is made.
