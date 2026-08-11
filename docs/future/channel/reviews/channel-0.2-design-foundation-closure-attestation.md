# Channel 0.2 design-foundation closure attestation

- **Reviewer identity:** `agent:channel-0.2-design-foundation-closure-review-2026-08-11-e863bf1`
- **Review date:** 2026-08-11
- **Reviewed commit:** `e863bf15fca30466d6e262b0ea66b3c05bc384eb`
- **Reviewed commit date:** 2026-08-11T15:48:23+02:00
- **Reviewed commit subject:** `docs(channel): correct design review findings`
- **Isolation:** every read and probe used a new `git clone --no-hardlinks` at
  `C:\Users\JakHoh\AppData\Local\Temp\brontide-channel02-review-183388e84f9e40af920bf8b33c42f39d\repo`,
  checked out detached at the full reviewed commit. `git status --short` was empty before and after
  review. The shared worktree was used only to write this attestation.
- **Independence:** this reviewer identity is distinct from the design and correction actors. The
  review used repository evidence only and had no access to the authors' private reasoning.
- **Scope:** fresh closure review under
  [`reviews/README.md`](./README.md), including the retained original negative attestation and a new
  search for blockers beyond B1-B4.

## Overall verdict

**does-not-conform**

The four specifically recorded B1-B4 defects are closed at the corrected pin: recipient invocation-
authority denial is now frameless local provenance, recipient cancellation refusal now has a
producer transition, all 37 responsibility rows use one exact owner identifier, and all 24
predecessor-vector rows use the five-value disposition vocabulary.

Closure nevertheless fails because this review found three new blocking defects:

1. the resolved Ready-ownership ruling, responsibility matrix, and migration ledger name
   incompatible semantic owners;
2. the interaction machine omits the contract-required `cancel-pending` plus peer-fault terminal
   path and does not state the recipient transition for structurally invalid cancellation control;
3. three Channel 0.1 feature rows still use `retained as non-promise`, which is outside the ledger's
   declared five-value disposition vocabulary.

The neutral-contract brief's Batch 2 entry gate therefore remains closed. This attestation does not
authorize schema authoring or a closure record.

## Architecture, targets, and predecessor evidence

### Current architecture and implementation targets — conforms as context

`Brontide-Architecture-Status.json` selects Architecture 0.8 as the current architecture with status
`Complete Draft (document and implementation evidence complete; not ratified)`. It records no latest
ratified architecture. The registry SHA-256 for
`docs/current/architecture/Brontide-Architecture-0.8.md` was recomputed as
`CAC9A02EA1221C3EE73C482D0624AE8DA45757B31A35C1EFD1061D4028B18579` and matched.

Both `Reference/README.md` and `Minimal/README.md` locally state `Designed for: Brontide Architecture
0.8`, Complete Draft and not ratified. Their registry hashes also matched. Both describe partial,
native implementation evidence and keep Composition and Portable Binding explicitly experimental;
neither claims that its implementation ratifies Architecture 0.8.

The public-boundary policy was assessed as a limitation, not silently enlarged into Channel 0.2:
Portable Binding 0.1 is a local-process experimental seam with finite frame/depth/collection/resource
bounds, a declared 10-second I/O timeout, one concurrent request, scoped replay protection, explicit
withdrawal/termination, and explicit non-promises for ordering, retry, cancellation, streaming, and
exactly-once execution. It assumes a locally selected executable and trusted launcher/account, and
does not claim cryptographic peer identity, multi-tenant isolation, or hostile-provider protection.

### Channel 0.1 and PB8 evidence — conforms as retained predecessor evidence

The retained Channel 0.1 design note, draft contract, requirements/risk ledger, and all 24 neutral
vectors were read. All nine Portable Binding schemas and their schema README were read. The retained
Reference, Minimal, and neutral PB8 closure attestations each record `conforms` within Portable
Binding 0.1's experimental boundary. This review does not convert those retained results into a
Channel 0.2 implementation claim.

The 24-vector inventory parses and the predecessor verifier reports coverage of all 11 Channel 0.1
requirements, 12 protocol categories, 7 process categories, and 5 failure domains. The Channel 0.1
artifacts remain unchanged and 0.2 is correctly planned as a distinct version rather than an implicit
decoder upgrade.

### Decision 13 and CM3/CM4 — conforms

Decision 13 retains fail-closed refusal for Portable Binding 0.1 and selects for 0.2 a separate Ready
signal plus exact relational lifecycle traffic between Interconnection and Ready. It requires the
declared edge, direction, initiating/receiving members, Operation, Capability, and input Shape;
undeclared traffic refuses before delivery, ordinary traffic remains closed until Release, authority
is exact, failure blocks Ready/Release, and actual effects survive into cleanup or rollback.

CM3 remains a plan-only contract and explicitly does not execute lifecycle Operations, report Ready,
Release, mutate the active generation, or roll back; CM4 owns those runtime effects. CM4 orders
Interconnection, optional Relational Initialisation, Ready, and logical Release, and its 20-vector
fixture includes the missing-Ready barrier and one-logical-Release cases. The CM3 18-vector and CM4
20-vector fixtures both parse. Channel C3/C7 and the interaction machine preserve Decision 13's
ordinary-machine, exact-declaration, pre-Ready interpretation.

## Capability verdicts

| Capability | Verdict | Rationale |
| --- | --- | --- |
| C1 | **conforms** | Exact Channel/profile/application versions, endpoint roles, required facets, finite bounds, and fixed/negotiated equivalence are established before dispatch; mismatch and downgrade fail with `known-none`. C1-P1 is substantive and falsifiable by a partial profile or any refused-path effect. |
| C2 | **conforms** | The session machine has exactly `unestablished`, `establishing`, `established`, `draining`, `closed`, and `faulted`; its legal, illegal, loss, drain, and terminal paths are monotone. External activation facts are predicates, not extra Channel states. The exact owner conflict in N1 is an ownership/ruling defect, not an extra session state. |
| C3 | **conforms** | Interaction class, direction, external phase predicate, Operation, and input contract are exact admission inputs; false/unknown phase refuses before dispatch. It does not infer phase from establishment. |
| C4 | **conforms** | Session-scoped interaction identities, atomic finite admission, replay reservation, sibling isolation, out-of-order completion, first-terminal authority, and drain snapshot are explicit without promising fairness or ordering. |
| C5 | **conforms** | Bounds and positional Shape rules precede effects; payload projection is distinct from exact authority/control positions; foreign runtime values and unbounded diagnostics are excluded. |
| C6 | **conforms** | B1 is corrected. Structural authority validation selects `rejected-protocol`; a structurally valid presentation denied by recipient local policy selects terminal `refused-local`, emits no peer frame, and records local `known-none`. Cross-trust Capability transfer remains forbidden. |
| C7 | **conforms** | Relational initialization is an exact ordinary-machine class in the Interconnection/pre-Ready window, matches exactly one lifecycle declaration, uses separate narrow authority, and cannot itself create Ready or Release. |
| C8 | **does-not-conform** | B2's cancellation-refusal producer is present, but N2 leaves other required cancellation paths undefined. C8 says a cancelled-pending interaction remains nonterminal until Outcome, peer fault, or local loss; the initiator table has no `cancel-pending` plus peer-fault transition. It also gives no recipient result for structurally invalid cancellation control. C8-P1 cannot be applied to every legal/illegal cancellation history without inventing behavior. |
| C9 | **conforms** | B1 is corrected and the four provenance forms remain exclusive: local refusal, semantic Outcome, peer protocol fault, and local loss. Unknown peer-fault categories do not create a reply loop, and local observations are not promoted to peer assertions. |
| C10 | **conforms** | Observations keep identity, dispatch, provenance, local detection, and `known-none`/`known`/`unknown` distinct. Possible post-dispatch effects are not rewritten to zero, and profile-owned details do not become Channel-verified facts. |
| C11 | **conforms** | Exact required/optional facets can add classes or evidence but cannot redefine Channel identity, authority, terminal provenance, or uncertainty. Retry remains a new interaction identity; Flow/Lifecycle/Delivery remain separate facets. |
| C12 | **conforms as a first-batch design requirement** | The neutral brief requires data-only canonical artifacts, deterministic expected observations, all C1-C12 groups and capability properties, independent native roots, a neutral endpoint without either stack runtime, process tests, dependency guards, and both cross-stack directions. Batch 2 has not begun, so this is not an implementation verdict. |

Each C1-C12 item has a named-scenario paragraph, one Cn-P1 property, an evidence paragraph, and an
explicit silence paragraph. The properties name observable failure triggers rather than restating
the examples.

## Area verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state machine | **conforms** | All six states, fixed and negotiated establishment, drain snapshot, orderly close, fatal fault/loss, representative illegal inputs, and terminal monotonicity are explicit. No Ready/Release/withdrawal/cleanup fact becomes session state. |
| Interaction state machine | **does-not-conform** | B1 and B2 are corrected, and dispatch/replay/concurrency/relational/terminal-race/sibling paths otherwise agree. N2 omits a contract-required initiator terminal transition after cancellation and the recipient classification/transition for invalid cancellation control. |
| Responsibility matrix | **does-not-conform** | Every one of 37 rows now has a syntactically exact owner identifier and a nonblank neutral crossing artifact, closing B3's matrix-cell defect. N1 still makes Ready's exact owner inconsistent across normative first-batch artifacts, so a schema cannot populate its required responsibility owner without choosing which artifact to contradict. |
| Contract-completeness review | **does-not-conform** | It correctly retains closure review as pending, but its claim that every non-goal has an owner is refuted by N1. Its residual cancellation risk produced N2, and its corrected-findings audit did not detect N3's feature dispositions. |
| Migration coverage | **does-not-conform** | Logical Shapes/fields, message kinds, states, taxonomies, limits, observations/resource subfields, 24 vectors, goldens/pins, and consumers are inventoried. B4's 24 vector dispositions are valid, but N1 gives Ready inconsistent target owners and N3 leaves three predecessor features outside the exact disposition vocabulary. |
| Neutral contract/vector brief | **conforms as a brief; Batch 2 gate not satisfied** | The brief separates schemas by responsibility, uses typed identities and finite closed data, requires deterministic C1-C12 vectors/properties and independent execution modes, and forbids a shared production runtime. Its gate correctly requires no state contradiction, one owner per concern, one disposition per predecessor item/vector, no unowned completeness finding, and no blocking review finding; N1-N3 keep that gate closed. |

## B1-B4 closure decisions

### B1 — closed

The recipient state set now includes `refused-local`. Structural/profile/state/class/phase/Shape/
authority-structure/bound/replay/concurrency failures select `rejected-protocol`; a structurally valid
authority presentation denied by local policy selects `refused-local` and emits no peer frame. The
terminal-provenance table classifies initiator and recipient `refused-local` as local observation,
not a peer Channel statement. This resolves the original C6/C9 contradiction.

### B2 — closed

The recipient table now contains `executing` plus a structurally valid cancellation control denied
by local cancellation authority, remains `executing`, and emits nonterminal `refused`. The initiator
consumes accepted or refused acknowledgement while remaining `cancel-pending`, and the cancellation
rules say refusal continues the ordinary terminal contract. This closes the original missing
recipient producer. N2 is a distinct remaining cancellation-path defect.

### B3 — closed as originally framed

A mechanical pass found 37 responsibility rows, 22 distinct owner identifiers, zero owner cells
outside the exact backticked `[a-z0-9-]+` form, and zero blank neutral-crossing artifacts. The original
compound/conditional owner-cell defect is corrected. N1 is a new cross-artifact consistency defect:
the exact matrix choice is not carried consistently into the resolved ruling and migration ledger.

### B4 — closed as originally framed

CH-01 through CH-24 occur exactly once and in order in the vector migration table. Every vector row
uses one of `retained`, `replaced`, `moved`, `removed`, or `legacy-only`; the prior `revised` and
`split` values are gone. N3 is a separate full-ledger defect in three predecessor feature rows.

## Four resolved ruling verdicts

| Ruling | Verdict | Consistency assessment |
| --- | --- | --- |
| Core concurrency and cancellation | **does-not-conform** | Finite bounded unary concurrency, profile-selected support, fixed identities, and non-redefinable terminality are consistent. N2 nevertheless leaves the fixed cancellation terminal machine incomplete after the cancellation request commits. |
| Session-state ownership | **does-not-conform** | The exact six Channel states and the exclusion of activation facts are consistent. N1 means the external Ready fact does not have one consistently represented semantic owner. |
| Relational-initialization representation | **conforms** | The plan, C3/C7, interaction machine, matrix, completeness review, ledger, neutral brief, Decision 13, and CM3/CM4 use the ordinary interaction form with a distinct exact pre-Ready class, not a second envelope family. |
| Extension invariants | **conforms** | C11, the matrix ruling, completeness review, migration rules, and brief consistently permit additive exact facets but forbid redefining identity, authority, terminal provenance, or effect certainty. |

## Blocking findings

### N1 — Ready has incompatible exact semantic owners

**Evidence:**

- `Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md`, **Resolved questions**, lines 450-453,
  says Portable Binding and Composition own Interconnection, Ready, Release, withdrawal, and cleanup.
- `Brontide-Channel-0.2-Responsibility-Matrix-0.1.md`, **Ownership matrix**, line 32, assigns Ready to
  exact owner `component-management`; **Session state versus activation phase**, lines 71-74, repeats
  that Component Management owns Ready and Composition is not an owner.
- `Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md` maps message-kind `ready` to
  Portable Binding/Composition at line 72, lifecycle state `ready` to Component/Portable Binding at
  line 88, and `readiness signal` to Portable Binding/Composition at line 164.
- `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md`, **Identity representation** and **Batch 2
  entry gate**, requires each schema to name its responsibility-matrix owner and every concern to
  have one owner.

**Impact:** `component-management`, `portable-binding`, and `composition` are distinct owner
identifiers in the corrected matrix. A neutral Ready schema or migration record cannot select one
without contradicting another normative first-batch artifact. This violates the required
cross-artifact consistency of the session-state ownership ruling and blocks Batch 2.

### N2 — cancellation histories remain incomplete after B2

**Evidence:**

- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, **C8**, lines 274-279, says an interaction whose
  cancellation request has been sent remains nonterminal until semantic Outcome, peer fault, or local
  loss arrives.
- `Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`, **Initiator transitions**, lines 64-67,
  accepts peer fault only from `dispatched`; from `cancel-pending` it specifies acknowledgement,
  semantic Outcome, and local loss but no peer-fault transition.
- The same file's **Admission order**, lines 92-101, validates every message/control kind and
  authority/control structure. Its **Recipient transitions**, lines 80-81, specify only valid
  cancellation control and structurally valid control denied by local authority; no transition says
  what a structurally invalid, unrecognized, unsupported, or wrongly scoped cancellation control
  does while the interaction remains `executing`.

**Impact:** a conforming initiator cannot consume one terminal form that C8 expressly permits after
cancellation, while recipient implementations can independently choose whether invalid cancellation
control faults the interaction/session, emits an interaction-scoped fault while execution continues,
or is ignored. The required all-legal/all-illegal state-machine review therefore fails, C8-P1 lacks a
complete domain, and neutral vectors cannot force one portable answer.

### N3 — three predecessor features use an undeclared migration disposition

**Evidence:**

- `Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md`, introduction, lines 17-19, declares exactly
  `retained`, `replaced`, `moved`, `removed`, and `legacy-only`.
- The same file's **Feature migration** table labels `streaming unsupported`, `ordering guarantee
  unsupported`, and `exactly-once unsupported` as `retained as non-promise` at lines 170-172.
- The ledger's **Ledger completion check**, lines 282-287, includes all ten lifecycle feature
  declarations and requires the independent review to challenge every semantic disposition.
- `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md`, **Batch 2 entry gate**, requires every 0.1
  item/vector to have a migration disposition.

**Impact:** `retained as non-promise` is not one of the five exact values. Treating it as `retained`
requires an undocumented normalization; treating it as a sixth value contradicts the plan and
ledger. The full migration inventory is not machine-actionable and the Batch 2 migration gate fails.

## Nonblocking findings

None.

Architecture 0.8's Complete Draft/non-ratified status, both stacks' partial experimental delivery,
and Portable Binding 0.1's declared threat limitations are accurately represented scope conditions,
not review findings.

## Checks and probes performed

1. Created a fresh no-hardlink clone, detached it at
   `e863bf15fca30466d6e262b0ea66b3c05bc384eb`, verified the subject/date, and confirmed a clean status
   before and after all reads/probes.
2. Read `AGENTS.md` and this review directory's README completely before evidence review.
3. Read the registry and all 5,647 lines of current Architecture 0.8, both stack target READMEs, and
   `docs/current/policies/public-boundaries.md`; recomputed and matched the registry hashes for the
   architecture and both stack plans.
4. Read the redesign plan; C1-C12 with all scenarios/properties/evidence/silence; both state machines;
   responsibility matrix; completeness review; migration ledger; neutral brief; and retained original
   negative attestation.
5. Read the Channel 0.1 design note, contract, requirements/risk ledger, all 24 vectors, all nine
   Portable Binding neutral schemas and schema README, the three PB8 closure attestations, Decision
   13, and CM3/CM4 contracts, completeness reviews, and vector fixtures.
6. Ran `build/verify-channel-0.2-design.ps1`: pass. Ran its `-NegativeProbe`: expected failure because
   C12-P1 was removed in memory, confirming the structural property check can fail. The normal pass
   does not check owner consistency, non-vector disposition rows, or complete cancellation edges and
   therefore does not rebut N1-N3.
7. Ran `build/verify-doc-links.ps1`: 798 local links across 289 documents passed.
8. Ran `build/verify-channel-vectors.ps1`: 24 vectors covered 11 requirements, 12 protocol categories,
   7 process categories, and 5 failure domains.
9. Parsed the 24-vector predecessor inventory, all nine Portable Binding schemas, and CM3/CM4 vector
   fixtures as JSON. Counts were 24, 18, and 20 where vectors are present.
10. Mechanically inspected the responsibility table: 37 rows, 22 distinct exact owner identifiers,
    no invalid owner-cell syntax, and no blank crossing artifact. A separate cross-artifact Ready
    probe returned `FAIL` because the plan/ledger owner sets differ from matrix owner
    `component-management`.
11. Mechanically inspected dispositions: all 24 vector rows use the declared vocabulary; the complete
    feature section returned three invalid values, all `retained as non-promise`.
12. Probed cancellation paths: C8's contract requirement for peer fault after cancellation was
    present, while the initiator table contained zero `cancel-pending` plus peer-fault transitions;
    the recipient table contained zero invalid-cancellation-control transitions.
13. **Capability-wide falsification attempt:** challenged C9-P1 with recipient local authority denial.
    The corrected actual path (`refused-local`; peer semantic `no`; peer Channel `no`; local
    observation `yes`) passed. An in-memory adversarial mutation routing the same denial to
    `rejected-protocol`/peer-Channel provenance failed as expected. This demonstrates the property is
    non-vacuous and independently confirms B1 closure without modifying the reviewed checkout.

No implementation repair was made and no implementation or full interchange build claim is made by
this design-only review.
