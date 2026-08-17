# Channel 0.2 design-foundation closure review 16 attestation

Reviewer identity: `agent:claude-opus-5-channel-0.2-closure-review-16-2026-08-17-95c62c1`

Date: 2026-08-17

Reviewed commit: `95c62c104ba191e52f651c161c63407513238a73`
(merge of `1cbdcfae954f1caa0f08be37c3c92f4d7272795a`, `fix(channel): close AK1-AK8 and audit every
C4-P2 operand`)

Reviewed tree: `1e83dbf0831c3fb7fc35c6e93a6f82c30a2157bd`

Overall verdict: **`does-not-conform`**

Blocking findings: **AL1**, **AL2**.
Nonblocking findings: **AL3**, **AL4**.

Every retained finding **B1** through **AK8** is verified closed in the artifacts its own evidence
sentences name, with one exception that is **AL2**: the `AK1` correction reached four of the five
surfaces `AK1`'s own evidence table enumerates, and the fifth still publishes the pre-correction
record. **AL1** is a capability-wide property that goes red on a conforming realization, which is the
class `AE3` made normative and `AE1` was blocking for, in a property the `AK` audit examined and
cleared.

## Isolation

Isolation is **complete**.

- A fresh isolated clone at the short path `C:/b036`, made before this session began, checked out at
  the pin above. **893 tracked paths**, `git status --porcelain` empty, `git diff HEAD` empty, all
  four verified in the clone rather than taken from the dispatch.
- The author's working repository `C:/Users/jakub/source/repos/Brontide` was **neither read, written
  to, nor executed against at any point in this session**. Every artifact assessed, every gate run,
  and every command issued was inside `C:/b036` or in this session's scratchpad. The clone's `origin`
  points at that repository, which is how the pin was obtained; that is the only relationship.
- The reviewer identity above differs from all fifteen retained reviewers, from every correction
  author, and from every retained iteration-review actor.
- No author private reasoning was available and none was supplied.
- The design was **not** repaired here. Temporary edits were made for mutation testing only in this
  session's scratchpad — never in the clone — so `git checkout -- .` was never needed and the clone
  was verified clean immediately before this file was written. At the end of the session the clone
  contains exactly one untracked path: this attestation.

## Disclosed dispatch provenance

The dispatching session disclosed the following, and it is recorded here as the policy requires.

- **The dispatching session has no prior involvement in this work.** It authored none of the
  corrections, no artifact in the design package, no check in the design verifier, no retained
  review, and no previous dispatch. It is a fresh session asked by the repository owner to dispatch
  this round. This is the first cycle since review 9 in which the dispatcher is not the author of the
  commit under review, and the next cycle should weigh this attestation's independence accordingly:
  the standing evidence that "the cold context did its own work" — that the blocking finding sat
  inside the dispatcher's own change — is unavailable here in either direction.
- **What it read before dispatching**: the git log, `docs/future/channel/reviews/README.md` in full,
  the commit messages of the two most recent correction commits, and parts of the review 15
  attestation (identity, isolation, dispatch provenance, closing sections). It states it formed no
  view of the design and conveyed none of what it read as a finding or a suspicion.
- **It made the clone and verified the pin before dispatching**, which is the origin of the numbers
  in the brief, and it told this reviewer to check them rather than accept them. They were checked.
- **The repository owner instructed the dispatch.** That is a party with an interest in the batch
  closing, and it was treated as a reason to probe harder rather than to defer.

**What the dispatch narrowed, stated so the next cycle can discount it.** The brief named no artifact
defect and no area of suspicion, but it gave five instructions, each a restatement of something the
policy already requires, and each did move effort:

1. *Verify the pin yourself* — done first, in subject, date, and tree-hash form. Negative result: the
   clause is correct.
2. *Falsify, do not read* — this is where the largest share of effort went. A `C4-P2` evaluator was
   written from the published prose and run over both named mutations, all seven declared
   required-green members, and two further conforming vectors. It produced **AL1** indirectly: the
   evaluator's session-scope machinery was reused on the session machine's own properties, which is
   not where the brief pointed.
3. *Check retained findings against their own evidence* — this produced **AL2** directly, and the
   credit belongs to the instruction. `AK1`'s evidence table in the review 15 attestation enumerates
   five publishing surfaces by line number; opening all five is what found the one the correction did
   not reach. No independent discovery is claimed for that class.
4. *Follow propagation* — the three frame references were traced across all six publishing surfaces.
   Negative result for `settling` and `terminal`; positive for `refused`, which is **AL2**.
5. *Audit the audit* — the operand enumeration was checked row by row against an independent
   clause-by-clause reading of both properties. Negative result: no row is missing and no row is
   wrong. That is recorded as probe **P7** rather than as an absence.

Effort went heavily to C4, C10, C12, the state/event grid, and — because of instruction 2 — the two
state machines' property lists. Less went to C5, C6, C7, and C11, which were read but not probed.

## Pin

The policy's pin clause was checked against the repository rather than against its own wording, as
`X6` requires and as `AI8` requires of its date.

| Clause states | Repository |
| --- | --- |
| target commit titled `fix(channel): close AK1-AK8 and audit every C4-P2 operand` | `1cbdcfa` carries exactly that subject |
| committed 2026-08-17 | `git log -1 --format=%ci 1cbdcfa` → `2026-08-17 21:30:52 +0200` |
| "or any later commit whose design artifacts hash identically" | `git diff --stat 1cbdcfa 95c62c1` is empty, so the reviewed merge's tree is identical to the named commit's |
| preceding pins `3892c23…` and `3b27e3a…` nonconforming | both present in history as the seventh and eighth review targets |

The clause is correct in all three forms. **No finding.**

## Blocking findings

### AL1 — `S3` counts a per-session fact across the vector and is red on a conforming two-session vector; the `AK` audit examined `S1`-`S6` and recorded the opposite

**Artifact/section evidence.**

- `Brontide-Channel-0.2-Session-State-Machine-0.1.md`, `## Capability-wide properties`, line 156:
  > **S3.** No new interaction is admitted after the first drain transition.
- The same artifact, `## Drain protocol`, item 3, line 103, states the underlying rule **with** the
  scope the property drops:
  > no new interaction may be admitted **locally** after the first drain transition
- The same artifact's status block, lines 9-11:
  > The AK pass audited `S1`-`S6` against C12's newly declared per-session facts along with every
  > other property in the package, and none of the six names one: the session machine's properties
  > are about one session by construction.
- `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md`, `## Vector format`, lines 233-245: a vector
  carries "the established profile and initial session/interaction state of **each session the vector
  carries**" and "**may carry more than one session**".
- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, C12, `**Facts a vector may hold more than one
  of.**` (lines 716-733) and the rule stated with it: "a property that names one names the session it
  means rather than counting or comparing it across the vector".
- `Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md`, per-capability property audit, line 209:
  the `S3` row restates the property without a scope and its required-green cell reads `**owed**`,
  with none of the "known conforming-realization exposure" note the `I5` row carries.

**The failure.** `S3` names no session. Evaluated over a vector that carries two sessions — which
`AH1` made legal and the brief's vector format states — "the first drain transition" is not a fact of
the vector, and any admission in a second session that happens after a first session's drain
transition violates the property literally. The evaluator run (probe **P5**) builds the vector from
the session machine's own legal transition table:

| step | session | transition or admission |
| --- | --- | --- |
| 1 | `s1` | `unestablished` + fixed profile validates → `established` |
| 2 | `s1` | admits interaction `i1` |
| 3 | `s1` | `established` + local drain begins → `draining` |
| 4 | `s2` | `unestablished` + sends/accepts one valid proposal → `establishing` |
| 5 | `s2` | `establishing` + exact acceptance → `established` |
| 6 | `s2` | admits interaction `i1` (identity reuse is legal in a new session, C4 Common terms) |

Every step is a row of the legal transition table and both endpoints conform in both sessions. `S3`
as published is **red** on it: the first drain transition is step 3 and an admission occurs at step 6.
With the session scope `AK7` added to `I5`, `C4-P1`, `C1-P1`, `C3-P1` and `C11-P1`, `S3` is green.
The same run confirms the corrected properties are green on the same vector, and that `C4-P1` without
its `AK7` scope is red on it — so the vector is the one `AK7` was raised for, and `S3` is a member of
that class the correction did not reach.

**Why this is blocking rather than a wording preference.** `AE3` made "a property must not fail
against a conforming realization" a normative rule in C12, and `AE1` — the only other finding of this
exact shape — was blocking. The programme's own statement of the class, in C12 beside the declared
fact list, is that "a property that counts a per-session fact per vector is AE1's defect reached
through the quantifier instead of through a clause".

**Why it survived.** Two reasons, and both are checkable. First, the `AK7` verifier check derives its
trigger set from C12's declared list of facts a vector may hold more than one of, and that list has
four members — `established profile`, `interaction identity`, `established finite bound`,
`nonterminal interactions`. A session's own state, and therefore its drain transition, is not among
them, so no pattern in the check can match `S3` (that omission is **AL3**). Second, the session
machine's status block records the audit as having been performed and having found nothing. The first
half of its sentence is literally true — `S3` names none of the four declared facts — and the second
half, "the session machine's properties are about one session by construction", is what is false: the
`AK7` correction rejected exactly that argument for `I5`, which lives in the interaction state
machine and is equally "about one interaction by construction", and required `I5` to say "within each
session the vector carries" all the same.

### AL2 — the state/event grid's two recipient `unseen` cells still publish the pre-`AK1` refusal record; they are one of the five surfaces `AK1`'s own evidence enumerates

**Artifact/section evidence.**

- `Brontide-Channel-0.2-State-Event-Coverage-0.1.md`, `## Recipient interaction coverage grid`, the
  `unseen` row, line 85. Cancellation-control cell:
  > no identity to correlate → state unchanged, recorded with `rejected-protocol` provenance, detailed
  > reason `unopened-interaction-identity`, frame kind `cancellation-control`, and effect certainty
  > `known-none`

  Other-peer-event cell:
  > state unchanged, recorded with `rejected-protocol` provenance, detailed reason
  > `unopened-interaction-identity`, the refused control's own frame kind, and effect certainty
  > `known-none`

  Both enumerate the record's contents and name **one** of the refused-frame reference's five fields.
  Neither names the session, the interaction identity, the committing endpoint, or the arrival ordinal.
- The same artifact's status block, lines 24-25, asserts the opposite:
  > Under **AK1** and **AK5** the recipient `unseen` route **publishes the refused-frame reference
  > rather than a reason and a frame kind**

  The route is those two cells, and they publish a reason and a frame kind.
- The same artifact's status block, line 8, still describes the cells correctly for `AC2`: "under AC2
  both `unseen` cells assert the detailed reason `unopened-interaction-identity` and the kind of frame
  refused". `AC2` put its field into the cells **and** the prose; `AK1`/`AK5` put four fields into the
  prose only.
- `channel-0.2-design-foundation-closure-review-15-attestation.md`, lines 195-201, is `AK1`'s own
  evidence, and it enumerates **five** publishing surfaces as separate rows, two of which are in this
  artifact:

  | row 3 | `Grid, line 82 (both `unseen` cells)` |
  | row 4 | `Grid prose, lines 117-124` |

  `git show 1cbdcfa -- …State-Event-Coverage…` changes the status block, the prose paragraph, and adds
  a new paragraph. **It does not touch line 85.** Of the five surfaces the finding's evidence names,
  four were corrected and one was not.

**Why the gate does not see it.** The frame-reference check in `build/verify-channel-0.2-design.ps1`
registers the grid surface as `$gridRecipientSection` — everything between `## Recipient interaction
coverage grid` and `## Late-traffic latch` (line 1744) — and requires the exact field list to appear
**once** anywhere in that span. The prose satisfies it. The package-wide class sweep at line 1835
triggers on the literal phrases `refused-frame reference|refused-frame position`, which the cells do
not use, so the abbreviated publication is invisible to both halves of the check written to catch
abbreviated publications. This is the `AI1` failure mode — a guard scoped to what it can already read
certifying its own completeness — one level finer than `AJ1` corrected it.

**Why it is blocking.** A vector or an implementation authored from the grid's recipient `unseen` row
records the refusal without a session, and probe **P3** shows that record takes `C4-P2` **red** on the
conforming two-session vector `AK1` was raised for — the identical false result, from the identical
cause, inside the commit written to close it. The mitigating fact is real and is stated: the same
artifact's prose 35 lines below does publish the whole reference and says "this cell asserts it like
any other". That makes this a contradiction between two passages of one artifact, which is `AF1`'s
class ("Two paragraphs of C4 contradicted each other, both gates stayed green"), and `AF1` was
blocking. It is also the ninth cycle of the correction-reaches-some-surfaces class, arriving in the
cycle immediately after review 15 recorded the first cycle in eight without one.

## Nonblocking findings

### AL3 — C12's declared list of facts a vector may hold more than one of omits the session's own state, which the neutral brief's vector format distributes per session

`Brontide-Channel-0.2-Capability-Contract-0.1.md`, C12, `**Facts a vector may hold more than one
of.**` declares four: `established profile`, `interaction identity`, `established finite bound`,
`nonterminal interactions`. `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` `## Vector format`
line 233 distributes a fifth over the same set — "the established profile **and initial
session/interaction state** of each session the vector carries" — and the session machine's six states
are per-session by definition.

The declaration is the trigger set of the `AK7`/`AK8` check
(`build/verify-channel-0.2-design.ps1`, lines 2049-2100), so the omission is what makes **AL1**
unreachable by the mechanism built to prevent it. `S2` ("No interaction dispatches outside
`established`") is the second property the omission leaves unexamined; it is not raised as a separate
finding because its vector-wide reading is undefined rather than red, but a scope would settle it.

The commit's own text says the declaration replaces "the members that happened to be visible" with a
class. The four declared members are exactly the four facts read by the five properties the pass found
red, which is the same derivation `AF6` was raised against — a class inferred from today's members —
applied one level up.

### AL4 — `S5` compares the established profile across the vector

`Brontide-Channel-0.2-Session-State-Machine-0.1.md` line 158:
> **S5.** Fixed and negotiated establishment produce equal normative profiles.

`established profile` is a declared per-session fact and a vector may carry two sessions with two
legitimately different profiles, one fixed and one negotiated. Read over the vector, `S5` is red on
that conforming input (probe **P5** runs it: `s1` fixed with `max-in-flight` 1, `s2` negotiated with
`max-in-flight` 4). What `S5` means is the session machine's own `## Fixed and negotiated equivalence`
section — "the fixed validator must produce the same immutable profile record a negotiated acceptance
would have produced" — which is a claim about **one** declared profile, and the property omits that
qualifier. `C1-P1` received the corresponding fix under `AK8`; `S5` did not.

This is rated nonblocking rather than blocking because the missing qualifier is arguably the declared
profile rather than the session, and because unlike `S3` no reading of the property is red on a vector
whose two sessions declare the same profile. The `AK7` recognizer also cannot see it for a second,
narrower reason worth recording: the check matches the declared fact `established profile` as the words
`established` and `profile` within a bounded gap, and `S5` writes `establishment … profiles`.

## Capability verdicts

| Item | Verdict | Rationale |
| --- | --- | --- |
| C1 | conforms | `C1-P1` carries the `AK8` per-session scope; profile establishment, downgrade refusal, and the fixed/negotiated equivalence rule are consistent with the session machine and the brief. |
| C2 | conforms | `C2-P1` is per-transition and per-session by construction; the legal, refused/illegal, and totality tables agree. Falsification attempt at `C2-P1` found nothing (P8). |
| C3 | conforms | `C3-P1` reads "the established profile **of its own session**" under `AK8`; class/direction/phase are exact admission inputs. |
| C4 | conforms | Both properties are correctly stated, correctly scoped, falsifiable on their named mutations, and green on all seven declared required-green members plus the `AK1` and `AK5` probes (P2, P3). **AL2** is against a surface `C4-P2` reads, not against C4's own text. |
| C5 | conforms | Positional, pre-effect, bounded; no per-session fact counted across the vector. |
| C6 | conforms | Authority local, exact, attributable; `known-none` on every denial. |
| C7 | conforms | The lifecycle declaration is `cm3-lifecycle-contract`-owned rather than profile-owned, so `C7-P1`'s "one and only one lifecycle declaration" is not a per-session fact and the `AK8` class does not reach it. Checked deliberately. |
| C8 | conforms | One terminal history per interaction; cancellation explicit and never semantic success. |
| C9 | conforms | Four provenance forms, exactly one selected; peer statement and local inference kept distinct. |
| C10 | conforms | C10's own text carries the refused-frame reference (`AK1`/`AK5`) and the terminal-frame reference (`AK6`), and the second enumeration's complete-list obligation is stated. |
| C11 | conforms | `C11-P1` carries the `AK8` per-session scope; facets cannot redefine core facts. |
| C12 | **conforms-with-nonblocking-findings** | Portability, boundedness, determinism, and independent testability hold, and the property-negative-probe scenario is real. Its declared per-session fact list is incomplete — **AL3** — and that incompleteness is the mechanism by which **AL1** survived. |

## Area verdicts

| Area | Verdict |
| --- | --- |
| Session state | **does-not-conform** — **AL1** (blocking) and **AL4**. |
| Interaction state | conforms — `I1`-`I7` were each read against the declared per-session facts; `I5` carries the `AK7` scope, `I1` was already per-session, and the remaining five name no per-session fact. |
| State/event totality | **does-not-conform** — **AL2**. The totality itself is sound: 108 published cells, 180 underlying state/event pairs, no empty cell, every catch-all and late-traffic latch present (P6). |
| Responsibility | conforms — 39 concerns, each with exactly one owner from the closed vocabulary; the local-observation row publishes all three frame references in full. |
| Completeness | conforms-with-nonblocking-findings — the `C4-P1`/`C4-P2` operand enumeration is sound row by row (P7); the `S3` audit row understates the property's exposure, which is part of **AL1**'s evidence. |
| Migration coverage | conforms — all 24 Channel 0.1 vectors dispositioned, `CH-R1`-`CH-R11` and `CH-K1`-`CH-K7` carried, `CH-R10` explicit, the new-evidence inventory carrying all three frame references. |
| Neutral brief | conforms — vector format, operator set, local-observation schema, parity profile, required adversarial groups, and Batch 2 entry gate are internally consistent and consistent with the artifacts the brief declares itself subordinate to. |

## Owner rulings

The four first-batch rulings recorded 2026-08-11 in the redesign plan's `## Resolved questions` are
represented consistently throughout the first-batch design; each was traced to the artifacts that have
to carry it.

| Ruling | Represented in |
| --- | --- |
| Core concurrency and cancellation (finite bounded unary; profiles select cancellation and the bound) | C4 `max-in-flight`; C8; interaction machine `## Concurrent interactions` and `## Cancellation`; responsibility matrix `Bounded unary concurrency` → `channel-profile` and `Class-specific cancellability` → `channel-profile`; migration ledger limits table; brief establishment rule |
| Session-state ownership (six Channel states only) | Session machine `## Boundary` and `## States`; responsibility matrix rows for Interconnection, Relational Initialisation, Ready, Release, withdrawal; C2; migration ledger state table |
| Relational initialization representation (ordinary interaction form, distinct class) | C7; C3; interaction machine; responsibility matrix `Relational interaction declaration` → `cm3-lifecycle-contract`; migration ledger feature migration |
| Extension invariants (facets add, never redefine) | C11 and `C11-P1`; C12; responsibility matrix; brief facet rules; migration ledger feature migration |

The correction rulings are recorded as issued and correctly excluded from that set of four: the
2026-08-13 R1 ruling, the 2026-08-13 S1 ruling with its `U2` normalisation note, the 2026-08-14 AE1
ruling with its two rejected options and the `AF8` narrowing, and the 2026-08-15 closure-standard
ruling with its timing disclosed. No proposed ruling remains in the plan.

## Retained findings

Every finding family `B` through `AK` was verified against its own evidence sentences in the artifacts
those sentences name, not against any index. All are closed except as **AL2** records.

- `B1`-`B4`, `N1`-`N3`, `F1`-`F3`, `D1`-`D5`, `T1`-`T4`, `R1`-`R3`, `S1`-`S3`, `U1`-`U8`, `V1`-`V3`,
  `W1`-`W6`, `X1`-`X7`, `Y1`-`Y4`, `Z1`-`Z4`, `AA1`-`AA3`, `AB1`-`AB2`, `AC1`-`AC4`, `AD1`-`AD3`,
  `AE1`-`AE5`, `AF1`-`AF8`, `AG1`-`AG5`, `AH1`-`AH6`: closed. Reviews 8-15 verified these individually
  and this review re-derived a sample by evidence sentence rather than by title — `S3`'s plan section
  7.8 (now reports fifteen attestations and names `AI9` as why it once reported seven), `AE3`'s
  required-green column, `AE5`'s sources inventory with `AF3`'s completion-check half, `AF5`'s seven
  named members, and `AF6`'s declared provenance table.
- `AI1`: **closed, and re-derived by evaluator.** Probe **P4** reproduces `AI1`'s exact false green on
  `C4-outcome-precedes-ack` from the pre-`AI1` field list and a correct red from the published one, on
  a two-session vector.
- `AI9`: closed. The plan's section 7.8 now reports fifteen retained attestations and records its own
  six-cycle staleness.
- `AJ1`-`AJ7`: closed. All six surfaces publish the settling-frame reference in the identical
  five-field form; `AJ5`'s `\bAI\b` defect is corrected by family-level claims; `AJ6`'s positional
  arguments are replaced by named fields in both latch sections.
- `AK1`: **closed in four of the five surfaces its own evidence enumerates.** The fifth is **AL2**.
- `AK2`: closed — the Channel index's Design reviews row names V, W, X, Y, Z, AA, AB, AC, AD
  individually and 15 attestations.
- `AK3`: closed — the package states 26 properties (13 capability, 6 session, 7 interaction), the audit
  has 25 rows, and 25 of 26 properties owe a required-green set with only `C4-P2` holding one. Counted
  independently.
- `AK4`: closed — the ledger status block says "four other artifacts … five other lists, because the
  brief publishes it twice".
- `AK5`: closed and **load-bearing**; probe **P3** shows reverting the arrival ordinal takes a
  conforming vector red.
- `AK6`: closed. Probe **P3** records that it is the one `AK` operand whose absence moves **no**
  verdict on its own in either the required-green group or the two mutations — see P3 for the detail.
- `AK7`, `AK8`: closed for `C4-P1`, `C4-P2`, `C1-P1`, `C3-P1`, `C11-P1` and `I5`, and **not** closed as
  a class. **AL1** is the same defect in `S3`, **AL4** in `S5`, and **AL3** is why the check cannot see
  either.

The programme's residual work is unchanged and is not a finding: 25 of the 26 properties owe the
required-green set `AE3` made normative, and `I1`-`I7` owe a named mutation as well.

## Probes performed

Falsification and verification work, including the attempts that found nothing.

### P1 — gates, in the isolated clone

| Gate | Result |
| --- | --- |
| `build/verify-channel-0.2-design.ps1` | **exit 0** — "11 required artifacts, C1-C12 with properties/scenarios/silence, total session/interaction event coverage, 6 session states, all 24 predecessor vectors dispositioned, 4 owner rulings resolved, and independent review still pending." |
| `build/verify-channel-0.2-design.ps1 -NegativeProbe` | **exit 1**, with exactly one failure: "Channel 0.2 capability contract properties is missing `**Property C12-P1.**`". Confirmed it fails **only** because `C12-P1` was removed in memory. |
| `build/verify-doc-links.ps1` | **exit 0** — 868 local links across 308 documents. |
| `build/verify-text.ps1` | **exit 0** — 887 UTF-8 files. |
| `build/verify-interchange.ps1` | **exit 0** — full build and test run; Reference and Minimal suites pass, 21 project references and 23 F# project boundaries checked, 25 dependency manifests resolved, 0 warnings. |

All five gates pass at the reviewed pin. **AL1** and **AL2** are both invisible to them, and the
reasons are stated in each finding.

### P2 — a `C4-P2` evaluator written from the published prose (positive: the property is correct)

The property was **not** read and agreed with. An evaluator was written from C4's `Property C4-P2`
statement, the paragraphs naming its subject (the committing endpoint), its membership scope, and its
two precedence operands, plus the brief's local-observation schema, parity profile, and property-
operator set. Frame references bind to declared stimulus steps by their five fields; precedence is over
declared steps only, never over the arrival ordinal (`Z1`).

Both named mutations and all seven declared required-green members were run, plus the `AK1`
two-session vector and the `AK5` two-controls vector:

| Vector | Required | Result |
| --- | --- | --- |
| `C4-control-precedes-request` | red | **red** (conjunct 1) |
| `C4-outcome-precedes-ack` | red | **red** (conjunct 2) |
| conforming commit-order delivery, initiator direction | green | green |
| conforming commit-order delivery, recipient direction | green | green |
| request lost, control delivered | green | green |
| acknowledgement lost | green | green |
| control for an identity the peer never opened | green | green |
| legal late control after a peer's terminal | green | green |
| duplicate terminal from a nonconformant peer | green | green |
| two sessions reusing one identity, conforming at both ends | green | green |
| two controls for one identity, delivery matching commit order | green | green |

**11 of 11 as the design requires.** `C4-P2` as published at this pin is falsifiable on both its named
mutations and stays green on every input the design says it must. This is a negative result for the
hunt and a positive result for the design.

### P3 — mutation-testing the published operands (positive: `AK1` and `AK5` are load-bearing; `AK6` is not, alone)

Each published field was reverted and the property re-evaluated, with ambiguous references resolved
existentially — a vector author facing a reference that does not single out one step still has to write
one expected observation, so "the fields do not decide" means some author reaches the wrong verdict.

| Reverted | On | Published | Reverted | Fires |
| --- | --- | --- | --- | --- |
| `AK1` session on the refused-frame reference | two-session conforming vector | green | **red** | yes |
| `AK1` interaction identity on the refused-frame reference | two-session conforming vector | green | green | no |
| `AK5` arrival ordinal on the refused-frame reference | two controls, commit-order delivery | green | **red** | yes |
| `AK5` committing endpoint on the refused-frame reference | two controls, commit-order delivery | green | green | no |
| `AK6` terminal-frame reference → terminal form only | duplicate terminal | green | green | no |
| `AK6` terminal-frame reference → terminal form only | legal late control after peer's terminal | green | green | no |
| `AK6` terminal-frame reference → terminal form only | `C4-outcome-precedes-ack` | red | red | no |
| `Y4` arrival ordinal on the settling-frame reference | duplicate terminal | green | green | no |
| **`Y4` and `AK6` together** | duplicate terminal | green | **red** | yes |

Three things follow, and the honest reading of each is given.

- **`AK1` and `AK5` reproduce their own findings independently.** Stripping the session from the
  refused-frame reference takes a vector conforming at both endpoints in both sessions red; stripping
  the arrival ordinal takes delivery that matched commit order red. Both corrections are real and
  necessary.
- **`AK6` alone moves no verdict.** Under the published settling-frame reference — which carries its
  own arrival ordinal since `Y4` — the terminal form is ambiguous but never ambiguous in a direction
  that changes the answer, on any of the seven required-green members or either mutation. The last row
  is why it is nonetheless defensible: with `Y4` reverted as well, the pair goes red on the duplicate
  terminal, so the terminal-frame reference removes the design's dependence on the settling frame's
  ordinal to carry both operands. **No finding is raised**: adding a reference is strictly more precise
  than inferring one, and a reviewer that raised over-precision would be manufacturing a finding. It is
  recorded so the next cycle knows the claim "`AK6` was a required-green failure" is not reproducible in
  the group as declared, and can decide whether that matters.
- **The interaction identity and committing endpoint fields of the refused-frame reference are
  redundant given the others in every vector tried.** Same disposition: recorded, not raised.

### P4 — `AI1` re-derived by evaluator on a two-session vector (positive: closed)

`AI1` claimed the settling-frame reference stops mapping to one declared step once two sessions may
hold one identity value, taking `C4-P2` green on `C4-outcome-precedes-ack`. Built directly: `s1`
carries the mutation, `s2` legitimately reuses identity `i1` and is fully conforming.

- Published form (session carried): **red**, correctly, on the `s1` mutation.
- Pre-`AI1` form (session stripped): **green** — the exact false green `AI1` and `AJ1` describe.

`AI1` and `AJ1` are closed, and the closure is reproduced rather than read.

### P5 — the same method turned on the session and interaction property lists (positive — **AL1**, **AL4**)

The session-scope machinery from P2 was reused on all 26 properties, evaluated over the conforming
two-session vector in **AL1**. `C1-P1`, `C3-P1`, `C4-P1`, `C4-P2`, `C11-P1` and `I5` are green with
their `AK7`/`AK8` scopes and `C4-P1` is red without its, confirming the vector is the one the class was
raised for. `S3` is **red** and `S5` is **red**. `S1`, `S2`, `S4`, `S6` and `I1`-`I4`, `I6`, `I7` were
each read against the declared per-session facts and against the brief's per-session vector fields;
only `S2` is unclear, and its vector-wide reading is undefined rather than red, so it is recorded
inside **AL3** rather than raised.

### P6 — independent enumeration of the state/event grid

Enumerated mechanically from the artifact rather than from any stated total: 18 data rows across three
grids × 6 content columns = **108 published cells, 0 empty**. Expanding the row groups against the
machines' own state lists — 6 session states, 12 initiator states, 12 recipient states — gives 36 + 72
+ 72 = **180 underlying state/event pairs**. This agrees with reviews 7 through 15. Every catch-all is
present (six numbered totality rules for the grid, plus each machine's own), the late-traffic latch is
asserted in every terminal row, and the `not-applicable` value is carried by the one route that reaches
no terminal interaction.

### P7 — auditing the operand enumeration (negative result)

The dispatch asked for the completeness review's `C4-P1`/`C4-P2` operand table to be checked as any
other claim. Both properties were re-read clause by clause and their operands listed independently
before the table was opened. The independent list contains: for `C4-P1`, the accepted terminal fact and
the admitted interaction it closes, dispatch of an interaction identity, the count of nonterminal
interactions, the established finite bound, and the established profile those are read against; for
`C4-P2`, the recorded `unseen` refusal provenance, its detailed reason, the refused-frame reference,
effect certainty on that route, the committed request as a declared step, the precedence between them,
the recipient's subsequent admission, the latch value, the settling-frame reference, the terminal-frame
reference, the precedence between those two, the `state-violation` category, and the preamble's own
per-identity selector.

**Every one has a row. No row is missing, no row names a surface that does not resolve, and no row
reads `insufficient`.** Two rows are more generous than the property's literal text — effect certainty
is attributed to conjunct 1 and the detailed reason likewise — and over-inclusion is not a defect. The
table's own statement of what it does not establish ("that the *reading* is complete") is accurate and
is the right disclaimer. **No finding.**

### P8 — falsification attempts at `C2-P1`, `C8-P1` and `C10-P1`, and the upstream pins (three negative results)

- `C2-P1` was run against the session machine's legal table, refused/illegal table, and totality rule
  for a state/event pair with two routes or none, and against the terminal rows for a path back to a
  nonterminal state. Nothing found; `closed` and `faulted` never transition and reconnect creates a new
  identity.
- `C8-P1` was run against the duplicate-terminal and cancellation-race paths for a second accepted
  terminal history or a cancellation acknowledgement recorded as semantic success. Nothing found; the
  latch preserves the first history and `outcome-cancelled` is reachable only through the handler's own
  completion.
- `C10-P1` was checked for a provenance form whose observation is incomplete after the three frame
  references were added. Nothing found; the `not-applicable` latch value and the "absent only where"
  conditions on each reference position cover every route.
- All **twelve** SHA-256 pins in `Brontide-Architecture-Status.json` were recomputed in the clone and
  all twelve match, including the Architecture 0.8 document, both stacks' 0.5 and 0.8 matrices, the
  shared 0.8 requirements, both READMEs, and both milestone ledgers. Architecture 0.8 is Complete
  Draft, not ratified; no architecture is ratified; both stacks state `Designed for: Brontide
  Architecture 0.8, Complete Draft, not ratified`. Consistent throughout.

## What this verdict means

The batch does not close. `AL1` and `AL2` are corrected in a later commit, test/contract-first, and
receive a fresh independent re-review from a reviewer identity distinct from all sixteen retained
reviewers and every correction author. This attestation is retained unmodified whatever that review
concludes.

Three things this cycle asks the next one to carry.

**The `AK` correction was substantially right and its own audit is sound.** `C4-P2` is correct,
falsifiable, and green on everything the design requires — verified by evaluator over the required
adversarial group, not by reading. The operand enumeration survived a row-by-row independent audit.
`AK1` and `AK5` reproduce their findings under mutation. This is the strongest state `C4-P2` has been
in across sixteen reviews, and neither blocking finding here is against it.

**Both blocking findings are one shape from opposite ends, and it is the shape this programme keeps
producing: a correction that generalises to a *class* and then defines the class from the members it
already found.** `AK7` declared the per-session facts and derived its check from the declaration — a
real improvement — but the declaration lists exactly the four facts the five red properties read, so
`S3` and `S5` were outside it before the check was written. `AK1`'s frame-reference guard is written
over the class "a frame a property reads is published as a frame reference" and enumerates its surfaces
from a search — also a real improvement — but the search is over the phrase `refused-frame reference`,
which the surface it missed does not use, because that surface is the one that predates the phrase.
Both mechanisms are better than what they replaced and both failed the same way. The next pass should
expect the third instance and should look for it where the *vocabulary* of the new class does not
reach, not where the class is stated.

**The one method that produced a finding here that no earlier cycle had is turning an evaluator built
for one property on the properties nobody had evaluated.** Fifteen cycles have evaluated `C4-P2`. The
session and interaction machines' thirteen properties entered the audit at `AF7` and, so far as the
retained records show, have never been run against a vector. Two of the six session properties are red
on the first conforming multi-session vector anyone wrote for them. `I1`-`I7` still owe a named
mutation and all thirteen owe a required-green set; that residual work is where the next finding of
this class most likely is.

## Note on the design gate

`build/verify-channel-0.2-design.ps1` is a strong gate and got stronger in this commit — the
frame-reference class assertion, the exact publication counts, and the declared-trigger session-scope
check are all genuine improvements over what they replace, and the negative probe is honest. Neither
blocking finding is a gate defect in the sense of a check that was written wrong: the session-scope
check does exactly what its comment says over the class C12 declares, and the frame-reference check
does exactly what its comment says over the surfaces it registers. In both cases the *declaration the
check reads* is what is short. That distinction matters for the correction: widening a regex would fix
neither.
