# Channel 0.2 design-foundation closure review 8 attestation

Reviewer identity: `agent:claude-opus-5-channel-0.2-closure-review-8-2026-08-14-3b27e3a`

Reviewed commit: `3b27e3a85bf018bead6d226a13d075c7e6ed16fa`

Date: 2026-08-14

Overall verdict: **`does-not-conform`** — one blocking finding (**U1**) and seven nonblocking
findings (**U2**-**U8**). Every retained finding B1-B4, N1-N3, F1-F3, D1-D5, T1-T4, R1-R3, and S2-S3
is closed in the artifacts it was raised against, verified individually rather than taken from an
index. **S1 is closed as to ownership and not closed as to falsifiability**: the correction gives the
intra-interaction ordering fact an owner, but the property that fact was required to carry cannot
fail.

## Isolation

Complete, with two disclosed deviations recorded below.

```text
git clone --no-local --no-checkout C:/Users/jakub/source/repos/Brontide C:/temp/br-rev8
git -C C:/temp/br-rev8 checkout --detach 3b27e3a85bf018bead6d226a13d075c7e6ed16fa
git -C C:/temp/br-rev8 status --short --branch     ->  ## HEAD (no branch)
```

The clone materialised completely: 881 tracked paths, 881 files on disk, empty `git diff HEAD`. No
`Filename too long` failure occurred, because the clone target is a short path as the dispatching
brief required. Every design artifact assessed in this attestation was read from
`C:/temp/br-rev8`; all five gates were run there.

**Deviation 1.** The governing policy `docs/future/channel/reviews/README.md` was read from the
shared working tree before the clone existed, because the dispatching brief directs that it be read
first. Under the owner ruling recorded in
[`docs/future/README.md`](../../README.md#channel-02-first-batch-remaining-work) and applied to
cycle 6, navigational and policy reading does not by itself spend context freshness. No design
artifact was read from the shared tree.

**Deviation 2.** The shared working tree was queried once for file *metadata* only — a count of
files by extension — to explain a baseline discrepancy in the gate results. No content was read.

No author private reasoning was available. This reviewer identity differs from all seven retained
reviewers and from every correction author, including the session that produced PR #117. This
session did not author any part of what it reviews.

**Independence caveat, stated plainly.** The dispatching brief named the S1 correction as the primary
target, named the three sharpest questions the policy asks, and directed that the new completeness
row be treated as a possible S1-class instance. Blocking finding **U1** is the answer to the second
of those named questions — *can `C4-P2` actually fail, or is it unfalsifiable* — and is therefore not
an independently conceived finding, though the enumeration that settles it and its consequences are
this reviewer's own. **U2** through **U8** were reached independently and none was named by the
brief.

## Pin

The review policy names `ff80703` as the current target and permits "any later commit whose design
artifacts hash identically to it". That clause is false at this pin, and the divergence is itself
recorded as **U6**. Two artifacts in the required review scope changed after `ff80703` in
[PR #117](https://github.com/Niizuki/Brontide/pull/117):

| Artifact | `ff80703` blob | `3b27e3a` blob |
| --- | --- | --- |
| `Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md` | `19bdaee3` | `42dff298` |
| `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` | `0054dce1` | `9d9a8c15` |
| `Brontide-Channel-0.2-Capability-Contract-0.1.md` | unchanged | unchanged |

Reviewing `3b27e3a` assesses the S1 correction *and* the three unreviewed changes, so it satisfies
the policy's intent while covering a delta the policy's own pin clause would have caused a reviewer
to miss. Those three changes were authored, not reviewed; they are assessed here on their merits and
are the subject of **U3**, **U6**, and **U7**.

## Blocking finding

### U1 — `C4-P2`, the property the whole S1 correction rests on, cannot be made to fail

**Artifacts and sections.** `Brontide-Channel-0.2-Capability-Contract-0.1.md` §C4, "Property C4-P2"
and the `C4-control-precedes-request` paragraph; `Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`
"Local recipient states" and "Late terminal and control disposition";
`Brontide-Channel-0.2-State-Event-Coverage-0.1.md` "Recipient interaction coverage grid" and the
paragraph beginning "`unseen` and `validating` are separate rows";
`Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md` silence row "control delivered before the
request it names".

**The property.** C4-P2 quantifies over acceptance:

> Across every C4 vector, for each interaction identity the sequence of frames a recipient **accepts**
> from one endpoint is an order-preserving subsequence of the sequence that endpoint committed. Loss
> may drop a frame; nothing may deliver two frames of one interaction in an order the sender did not
> commit them in.

In this contract's vocabulary, acceptance is narrow and explicit. `rejected-protocol` is "Validation
failed and a bounded peer protocol fault may be emitted"; the late-traffic latch "preserves the first
accepted terminal history" and produces "no redispatch or handler effect"; "Accepted interaction
identities enter the replay set before handler dispatch". Neither a `rejected-protocol` route nor a
latched frame is an accepted frame.

**The consequence.** Under the property's own named mutation, the recipient accepts nothing:

- the control arrives at `unseen`; the recipient grid routes it to `rejected-protocol` — *rejected*;
- the request then arrives at a terminal interaction and takes the late-traffic latch — *rejected*;
- the accepted sequence is empty, and the empty sequence is an order-preserving subsequence of every
  sequence.

C4-P2 stays **green** on `C4-control-precedes-request`. C4-P2 itself declares the verdict for that
outcome: "`C4-control-precedes-request` is the mutation this property must go red on, and a run in
which it stays green is a finding against the property rather than evidence for the design."

**It is not only that mutation.** C4 bounds a sender to "at most a request and one cancellation
control" for one direction of one interaction, so the entire delivery space for one identity is five
cases. An evaluator written from the published prose alone (probe P3) enumerates all five and none
turns C4-P2 red. The recipient-to-initiator direction behaves identically: with committed
`[ack, outcome]` delivered as `[outcome, ack]`, the Outcome is accepted from `cancel-pending` and the
late acknowledgement is latched, so the accepted sequence is `[outcome]`, again a subsequence. The
design's own fault-and-latch routing guarantees that the out-of-order frame is never the accepted
one, which makes C4-P2 vacuously true for every realization, conforming or not.

**The alternative reading does not rescue it.** Read "accepts" as "receives" — which is what C4-P2's
own second sentence ("nothing may *deliver* two frames...") means — and the property goes red exactly
as intended; probe P3 confirms both readings side by side. But that reading quantifies over frame
arrival order, and frame arrival order appears in no observation this design defines. C10's
observation list records "profile, session and interaction identities, direction, class, admission
and authority decisions, dispatch boundary, terminal provenance, peer-reported facts, local detection
point, retry/fallback facts ... and effect certainty", and the neutral brief's "Observation and parity
profile" adds nothing of the kind. A vector therefore cannot express the expected observation, and
C12-P1's "one deterministic expected portable observation" cannot be met for it.

**A third statement conflicts with both.** C4 says of the mutation vector: "Its expected observation
is that the vector is rejected as nonconforming evidence, not that the recipient answers it." A
vector rejected as invalid evidence is never executed, so C4-P2 never evaluates it. That sentence and
C4-P2's "the mutation this property must go red on" cannot both be true.

**Why this blocks.** Three independent statements in the first-batch package assert this property is
falsifiable, and all three are refuted:

- C4-P2: "the mutation this property must go red on";
- the state/event grid: "`C4-P2` is the property that fails when it does not hold";
- the completeness review: the mutation exists "so that `C4-P2` has something to fail on".

And three normative rules are violated:

- C12 Failure and uncertainty: "Every property must be able to fail against a named incorrect
  implementation";
- the neutral brief: "A property that cannot be made to fail is a review finding";
- the design verifier's own recorded S1 rationale: "S1 survived seven review cycles because every
  `Cn-P1` stayed green across it, so a new promise without a falsifiable property repeats exactly
  that failure."

The S1 ruling supplied the missing *owner*. It did not supply the missing *falsifiability*, and the
`unseen` cancellation verdict — the cell the whole correction exists to make sound — still rests on a
promise no property can test. This is S1's defect class one level down: the correction asserted a new
fact, gave it an owner, and attached a property that cannot fail, so the fact is again true only
because the artifacts say so.

**Not repaired here.** Under the review policy a blocking finding is corrected test/contract-first in
a later commit by another actor and receives a fresh re-review; this attestation is retained
unchanged.

## Nonblocking findings

### U2 — the S1 ownership row introduces a second identifier for one owner family

`Brontide-Channel-0.2-Responsibility-Matrix-0.1.md`, ownership matrix. The new row reads

```text
| Intra-interaction frame order | `channel-core` | ... |
```

`channel-core` appears in exactly one of the 38 ownership rows. The seven other Channel-core semantic
facts — Channel contract version, fixed/negotiated profile equivalence, session
establishment/drain/close/fault, interaction identity and terminality, cancellation control and
terminal meaning, peer protocol fault, and effect certainty — all use `channel`. The matrix's own rule
says the column "uses one exact owner identifier per row. An identifier names the contract family that
defines the fact", and there is no glossary of identifiers. "Boundary verification required" then
demands "a machine-readable ownership inventory ... The neutral verifier must reject duplicate or
missing owners." Two strings for one contract family is a latent duplicate owner in that inventory.

Three other artifacts name the same owner three further ways: the grid says "**C4 owns**", the
migration ledger says "owned by C4 with `C4-P2`", and the ruling says "Channel 0.2 core owns it". The
design verifier's owner check tests only the shape `^`[a-z0-9-]+`$`, so it cannot see this.

### U3 — the neutral brief carries no trace of the S1 correction

`Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md`. A search across the Channel design package for
intra-interaction / per-interaction frame order returns hits in the capability contract, both the
grid and the matrix, the migration ledger, the redesign plan, and the Channel index — and **none** in
the neutral brief, whose status block was the only part PR #117 touched. Yet the brief is the artifact
that fixes every Batch 2 boundary the correction creates:

- "Version and establishment rule" states what a proposal declares and what acceptance confirms, and
  does not include the realization's per-interaction frame order declaration that the matrix makes
  the crossing artifact and that C4's Evidence requires "a profile checks at establishment";
- the `established-profile.json` schema boundary is unchanged;
- "Vector groups" lists no ordering-mutation group for `C4-control-precedes-request`; and
- "Observation and parity profile" adds no field a frame-order property could quantify over — which
  is precisely what makes U1's second horn unfixable in Batch 2 as currently briefed.

### U4 — the completeness review's disposition narrative stops four findings short of its own status block

`Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md`. The status block claims "author pass plus
B1-B4, N1-N3, F1-F3, D1-D5, T1-T4, R1-R3, and S1-S3 correction passes complete." The "Review
disposition" section narrates five cycles — `66729b0`, `e863bf1`, `1af7ba0`, `1b7c5fd`, `5cf42c4` —
and contains no occurrence of "R1" or "S1" at all. Its closing sentence, "These changes still need a
fresh independent closure re-review", is still attached to the T1-T4 pass. Every earlier cycle
received a disposition paragraph; the sixth and seventh did not. This is the artifact required-review-scope
item 10 names, and the omission is S3's class.

### U5 — the per-capability property audit was not updated for `C4-P2`

`Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md`, "Per-capability property audit". The C4
row still reads "C4-P1 one dispatch/terminal and bounded concurrency | redispatch replayed identity or
exceed bound". C4 now carries a second capability-wide property with its own named mutation, and this
table is the one artifact whose job is to pair each property with the mutation that must fail it.
Given **U1**, this omission is why the audit did not catch the unfalsifiable property.

### U6 — the review README's pin clause is false at the pin

`docs/future/channel/reviews/README.md`, closing paragraph of "Retained attestations": the S1
correction commit "is the current review target. Review that commit or any later commit whose design
artifacts hash identically to it." Two required-scope artifacts do not hash identically at
`3b27e3a` (table above). A reviewer following the README literally would review `ff80703` and assess
neither the new silence row nor the brief's new status claim. This recurs S3's defect — a correction
pass leaving the governing index at the pre-correction state — inside the pass that dispositioned S3.

### U7 — the new in-flight direction-scope row misattributes, and understates its own position

`Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md`, silence row "direction scope of the
in-flight bound", and residual risk 2.

(a) The row says "the atomic reservation C4 describes is local with no cross-endpoint coordination".
C4 describes no reservation. "Admission reserves one in-flight position atomically before dispatch"
is in `Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`, "Concurrent interactions". In a
correction whose whole class is about which artifact states which fact, the attribution should be
exact.

(b) The row says C4, `C4-P1`, and `I5` "state one count without saying whether it is per session or
per initiating direction." Answering residual risk 2's question directly — *does one count already
imply a scope the artifacts do not state?* — the answer is yes, in the weak direction. C4-P1's "the
number of nonterminal interactions never exceeds the established finite bound" and I5's "Concurrency
never exceeds the established finite bound" carry no direction restriction, so both read session-wide.
The only enforcement the design provides is a local atomic reservation which, as the row itself says,
has no cross-endpoint coordination and can therefore enforce only a per-direction count. The honest
statement is not that the scope is undeclared, but that two core properties state a session-wide count
that the design's only mechanism can enforce only while one endpoint initiates.

This is nonblocking because the row's reachability claim is correct: in the only named profile one
endpoint initiates both classes, the two readings coincide, and no vector can fail on the difference.
It does mean C4-P1 and I5 are core properties whose subject set a future both-initiate profile would
have to pick, which is a fact the artifacts should state rather than leave open.

### U8 — the initiator grid's pre-dispatch local-loss cell does not name the state S2 reconciled

`Brontide-Channel-0.2-State-Event-Coverage-0.1.md`, initiator grid, row `candidate` / `admitting`,
Local loss column: "local refusal/loss before dispatch". Every other Local loss cell in that grid
names `lost`. The interaction machine's totality rule is explicit — "Local loss in any nonterminal
state selects `lost`, pre-dispatch states included" — so the route is determinate and this is
presentational, not a hole. It is nonetheless the one cell S2's reconciliation was about, and it was
not updated with it.

## Capability verdicts

| Item | Verdict | Rationale |
| --- | --- | --- |
| C1 | `conforms` | One immutable profile before dispatchability, fixed/negotiated equivalence required as one canonical record, no implicit downgrade or in-place renegotiation. C1-P1 is falsifiable by its named mutation (remove one required facet from the fixed profile only), and the session machine's "Fixed and negotiated equivalence" makes a field absent from the fixed path a contract defect rather than realization freedom. |
| C2 | `conforms` | Six session states, drain semantics complete, D1's duplicate-drain fatal `state-violation` preserving the original snapshot verified in the legal transition table and the drain protocol. Ready/Interconnection/Release correctly excluded as session states and consumed only as guards. |
| C3 | `conforms` | Class, direction, and external phase are all exact admission inputs; `false` and `unknown` are treated identically and refuse framelessly at both endpoints (D3). Cross-checked against CM4's stage order: Local Initialisation, Interconnection, optional Relational Initialisation, Ready, with ordinary traffic only after the logical Release — which is exactly `interconnected && !ready` and `released`. |
| C4 | `does-not-conform` | Correlation, replay, and bounded concurrency are sound and C4-P1 is falsifiable. **U1**: `C4-P2`, the property the S1 correction requires the new ordering promise to carry, is vacuously true under the contract's own definition of acceptance and cannot be made to fail by any delivery order the design permits. **U7** additionally applies to `C4-P1`'s direction scope. |
| C5 | `conforms` | Positional payload/authority separation, pre-dispatch parsing and bounds, environmental limits exposed and accepted at establishment, `known-none` on every pre-dispatch structural refusal. |
| C6 | `conforms` | Authority is evaluated per interaction after structural admission and before dispatch, local denial is frameless with `known-none`, and no Capability or derivation chain crosses a trust boundary. C6-P1 is falsifiable by its named mutation (treat compatibility or delivery as permission). |
| C7 | `conforms` | Relational initialization is a class on the ordinary machine with the exact CM3 declaration, pre-Ready window, and separate narrow authority; success cannot create Ready or Release. Verified against CM3 C5 and CM4 C4/C5 in the clone. The CM3-declared timeout and retry bound do not cross into Channel, consistent with the matrix placing timing on `realtime-facet` and retry on `retry-profile`, and a fired timer surfaces as the ledger's retained `timeout` local-loss category with required timer provenance. |
| C8 | `conforms` | One terminal history; cancellation acknowledgement explicitly nonterminal; R1's held control, its application after dispatch, and S2's loss/drain exits all present and mutually consistent with the interaction machine and the grid. T3's contradictory `cancelled` terminal has one explicit result at both endpoints. |
| C9 | `conforms` | The four provenance forms are exclusive and the terminal-provenance table assigns each terminal state exactly one column; unknown peer-fault categories fault locally with no answering fault. |
| C10 | `conforms-with-nonblocking-findings` | Effect certainty is correct and is the right successor to the PB8 finding that both stacks fabricated zero where the endpoint lacked evidence (verified in `pb8-minimal-closure-attestation.md`, B3 and the retained Minimal R1/F1 rows). The observation record contains no field for frame arrival order, which is the second horn of **U1** and is why the falsifiable reading of `C4-P2` is not expressible. |
| C11 | `conforms` | Facets are exact, additive, and forbidden to reinterpret core identity, authority, provenance, or certainty. The S1 correction's amendment — "The single ordering fact core does own is C4's intra-interaction frame order; a facet may add delivery and ordering guarantees beyond it but may not weaken it" — scopes the non-promise without weakening the extension invariants ruling. |
| C12 | `does-not-conform` | C12's own text is sound and C12-P1 is present and falsifiable (the negative probe removes it and the gate fails on exactly that). But C12's Failure and uncertainty clause — "Every property must be able to fail against a named incorrect implementation" — is a normative requirement over this contract's properties, and `C4-P2` violates it (**U1**). C12 is recorded nonconforming because the package does not satisfy the rule C12 states, not because the rule is wrong. |

## Area verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state | `conforms` | Six states, closed-world event totality, D1 closed, no external phase admitted as a session state. |
| Interaction state | `conforms` | Twelve initiator and twelve recipient states, all producer transitions present, B1/B2/N2/F1/F2/D2-D4/T3/R1/R2/S2 verified closed against the tables themselves. |
| State/event totality | `conforms-with-nonblocking-findings` | Independently reproduced (probe P2); every cell routed, no empty cell, groupings a true partition. **U8**. |
| Responsibility | `conforms-with-nonblocking-findings` | All 38 rows carry exactly one owner identifier (B3 closed), N1's exact Ready/Interconnection/Relational owners consistent, and the S1 fact now has a row and a crossing artifact. **U2**. |
| Completeness | `conforms-with-nonblocking-findings` | Silence inventory, property audit, and residual risks are present and the S1/S2 rows are correct. **U4**, **U5**, **U7**. |
| Migration coverage | `conforms` | All 24 predecessor vectors dispositioned exactly once in order and present in `conformance/channel-0.1-vectors.json`; every bold disposition inside the declared five-value vocabulary; T1's peer-fault permission removed; T2's replay window bound to the nonterminal case; the ordering non-promise correctly re-dispositioned from **retained** to **replaced** because its scope changed. |
| Neutral brief | `conforms-with-nonblocking-findings` | Artifact boundaries, identity spaces, version rule, vector and property formats, golden policy, and the Batch 2 entry gate are implementable without either stack. **U3**. Its entry gate is in any case unmet while **U1** stands. |

## Owner rulings

The four 2026-08-11 first-batch rulings are represented consistently throughout the first-batch
design. Verified individually:

1. **Core concurrency and cancellation** — C4's finite positive `max-in-flight` and C8's optional
   cancellation; matrix `Bounded unary concurrency` = `channel-profile`, `Cancellation control and
   terminal meaning` = `channel`, `Class-specific cancellability` = `channel-profile`; interaction
   machine "Concurrent interactions" and "Cancellation" 1-2; ledger `single invocation` → **replaced**
   ("a profile may still choose 1") and `cancellation unsupported` → **replaced**. **U7** is a scope
   question inside this ruling and does not contradict it.
2. **Session-state ownership** — C2 and the session machine's explicit not-a-session-state list;
   matrix Interconnection = `portable-binding`, Relational Initialisation phase = `composition`,
   Ready = `component-management`, Release = `portable-binding`; the plan's exact sentence; ledger
   `ready` → **moved**. Consistent, and N1 remains closed.
3. **Relational initialization representation** — C3 and C7, the interaction machine's "Relational
   initialization" section, the matrix's boundary ruling and `cm3-lifecycle-contract` row, and the
   ledger's removal of the generic `Lifecycle` body. Cross-checked against CM3 C5 and CM4's stage
   order in the clone.
4. **Extension invariants** — C11, cross-capability invariant 7, the matrix's "Extension hooks"
   ruling, and the ledger's retry / streaming / exactly-once rows. The S1 correction amended C11's
   ordering sentence and left the ruling intact.

The **2026-08-13 R1 and S1 correction rulings** are separately recorded in the redesign plan's
resolved questions, each stating explicitly that it does not join the four first-batch rulings. This
attestation keeps them distinct and does not treat either as a fifth or sixth first-batch ruling.

## Retained findings

Each verified directly against the artifact text at this pin, not against an index, a summary, or a
previous attestation.

| Finding | Verdict | Evidence at this pin |
| --- | --- | --- |
| B1 | closed | Interaction machine: `validating` + denied structurally valid authority presentation → `refused-local`, frameless. |
| B2 | closed | Interaction machine: `executing` + denied cancellation control → `cancel-refused` with a nonterminal `refused` acknowledgement. |
| B3 | closed | All 38 ownership rows match `^`[a-z0-9-]+`$`; zero violations. See **U2** for the identifier-vocabulary residue. |
| B4 | closed | Every bold disposition in the ledger is one of the declared five; zero violations. |
| N1 | closed | Ready = `component-management`, Interconnection = `portable-binding`, Relational Initialisation phase = `composition`, all distinct and repeated identically in the plan and ledger. |
| N2 | closed | A valid correlated peer fault is accepted from `dispatched`, `cancel-pending`, `cancel-accepted`, and `cancel-refused`. |
| N3 | closed | No undeclared disposition word survives in any ledger table. |
| F1 | closed | Live replay during `executing`/`cancel-requested`/`cancel-refused` → `peer-fault`, no redispatch, `replay-detected`, later handler terminal ignored. |
| F2 | closed | Recipient `peer-fault` and `lost` are distinct terminal states, each with an exclusive terminal-provenance row. |
| F3 | closed | No `**new**` disposition remains. |
| D1 | closed | Duplicate drain → `faulted` with a session-scoped `state-violation`, original snapshot and interaction evidence preserved. |
| D2 | closed | Distinct `cancel-accepted` / `cancel-refused` states plus unsolicited, duplicate, and contradictory acknowledgement faults. |
| D3 | closed | Receiver-local `false`/`unknown` phase → frameless `refused-local`, and the ledger forbids reading it as a fault. |
| D4 | closed | Three-value `late-traffic-fault` latch, one possible emission, no fault loop. |
| D5 | closed | Delivery `fallback` → **moved** to the delivery/retry facet, with `none` retained as a valid attributable value. |
| T1 | closed | The permissive phrase is absent and the ledger states an external phase refusal is never that fault. |
| T2 | closed | `replay-detected` bound to the nonterminal window; a post-terminal repeat follows the latch as `state-violation`. |
| T3 | closed | A `cancelled` terminal with no request in force has one explicit result at each endpoint. |
| T4 | closed | Every first-batch status block carries the one stable phrase and names no superseded cycle; PR #117 extended the check to the neutral brief, which strengthens it. |
| R1 | closed | A control racing recipient admission is held, not faulted, with a matching detailed transition row rather than a catch-all. |
| R2 | closed | "The two preconditions are local to their own endpoints and no event synchronises them." |
| R3 | closed | `unseen` and `validating` occupy separate recipient grid rows with different verdicts. |
| S1 | **partially closed** | **Ownership: closed.** C4 promises the fact, C4's silence and C11 are scoped to cross-interaction and cross-session ordering, the matrix carries an owner row and a checkable crossing artifact, the realization profile declares it, the grid carries rather than owns it, and the ledger re-dispositions the non-promise. **Falsifiability: not closed** — see **U1**. |
| S2 | closed | `validating` carries loss and drain rows, a held control is discarded with no answering frame in both, the latch does not fire, and the pre-dispatch loss rule is reconciled to any nonterminal state. |
| S3 | closed | The plan, the Channel index, and the future-work index all name R1-R3 and S1-S3 and the awaited cycle, and the index's "108 cells" count is correct (probe P2). See **U6** for the same class recurring in the review README's pin clause. |

## Probes performed

### P1 — gates, in the required order, in the isolated clone

| Gate | Result |
| --- | --- |
| `build/verify-channel-0.2-design.ps1` | Pass. "Channel 0.2 design-foundation verification passed: 11 required artifacts, C1-C12 with properties/scenarios/silence, total session/interaction event coverage, 6 session states, all 24 predecessor vectors dispositioned, 4 owner rulings resolved, and independent review still pending." |
| `build/verify-channel-0.2-design.ps1 -NegativeProbe` | Exit 1 with **exactly one** message, on C12-P1, and nothing else: `FAIL: Channel 0.2 capability contract properties is missing '**Property C12-P1.**'.` Confirmed by reading the full output, not assumed. |
| `build/verify-doc-links.ps1` | Pass. "Documentation link verification passed for 830 local links across 296 documents." Matches the stated baseline. |
| `build/verify-text.ps1` | Pass. "Text integrity verification passed for **875** UTF-8 files." |
| `build/verify-interchange.ps1` | Exit 0. Two skips, both the pre-existing `Cbi51` restart-policy tests (`Cbi51_C1_restart_policy_is_explicit_and_bounded` in Reference Studio, `CBI51 C1 restart policy is explicit and bounded` in Minimal Host). |

Two baseline discrepancies, both explained and neither a repository defect:

- **875, not 879, text files.** The pinned tree contains exactly 875 tracked files with the scanned
  extensions. The 879 baseline was measured in the shared working tree, which additionally contains
  four git-ignored Visual Studio files — `.vs\ProjectSettings.json`, `.vs\VSWorkspaceState.json`,
  and two `.vs\Brontide.slnx\v18\DocumentLayout*.json`. 875 is the correct count for this pin.
- **Interchange totals.** The script prints no aggregate, so per-assembly figures are recorded
  instead. Base pass: Reference 784 passed + 1 skipped (785 total) across eight assemblies; Minimal
  740 passed + 1 skipped (741 total) across eight. The brief's "Reference 785 / Minimal 733" matches
  Reference's *total* but not Minimal's on any reading; exit code, the zero failures, and the two
  `Cbi51` skips — the substantive claims — all hold.

### P2 — independent enumeration of the state/event grid

States were enumerated from the two state machines' own state tables, and rows and columns from the
grid, without reading the grid's claimed totals.

| Dimension | States (from the machines) | Grid rows | Event columns | Row cells | State cells |
| --- | --- | --- | --- | --- | --- |
| Session | 6 | 6 | 6 | 36 | 36 |
| Initiator | 12 | 6 | 6 | 36 | 72 |
| Recipient | 12 | 6 | 6 | 36 | 72 |
| **Total** | | | | **108** | **180** |

**Cycle 7's 108 is reproduced**, and it counts published rows × event columns. The underlying
state × event count is 180; both are correct under their own definitions and the difference is
entirely the grouped rows.

The groupings were checked against the underlying states rather than accepted. They are an exact
partition: initiator `candidate`/`admitting` (2) + four singleton rows (4) + "any terminal" (the 6
initiator terminal states) = 12; recipient `unseen`, `validating`, `executing`, `cancel-requested`,
`cancel-refused` (5) + "any terminal" (the 7 recipient terminal states) = 12. No state is covered
twice and none is uncovered. Column-by-column the grouped rows hold, with the single presentational
exception recorded as **U8**. The `candidate`/`admitting` peer-fault cell relies on the interaction
machine's catch-all rather than a detailed row, and its `known-none` certainty matches I4.

### P3 — falsification of a capability-wide property (`C4-P2`)

An evaluator was written from the published prose of the capability contract, the interaction state
machine, and the state/event coverage grid. It imports no repository code. It was verified against
known-good observations before any negative result was trusted, then the property's own named
mutation was applied, then the whole reachable space was enumerated.

| Case | Committed | Delivered | Accepted | C4-P2 ("accepts") | C4-P2 ("delivered") |
| --- | --- | --- | --- | --- | --- |
| known-good, conforming | request, control | request, control | request, control | GREEN | GREEN |
| known-good, control lost | request, control | request | request | GREEN | GREEN |
| known-good, request only | request | request | request | GREEN | GREEN |
| known-good, total loss | request, control | — | — | GREEN | GREEN |
| **mutation `C4-control-precedes-request`** | request, control | control, request | — | **GREEN** | RED |
| known-good, recipient direction | ack, outcome | ack, outcome | ack, outcome | GREEN | GREEN |
| mutation, recipient direction | ack, outcome | outcome, ack | outcome | **GREEN** | RED |

Trace for the mutation: `control REJECTED at unseen -> rejected-protocol (terminal) ; request
REJECTED at terminal rejected-protocol (late-traffic latch)`.

Exhaustive enumeration over the complete space of deliveries for one identity in one direction — C4
bounds a sender to at most a request and one cancellation control, so the space is five — returns
green in every case. **No delivery order of the frames a sender may commit makes `C4-P2` go red under
the "accepts" reading.** This is the evidence for **U1**.

### P4 — direct verification of every retained finding

Twenty-two machine-checked assertions against the exact artifact text (transition rows, ownership
rows, disposition vocabulary, latch values, provenance rows), plus manual assessment of T4, S1, and
S3. Results in the retained-findings table above. No closure was taken from an index, a summary, or a
previous attestation.

### P5 — ownership-identifier enumeration

All 38 ownership-matrix rows parsed and grouped by owner identifier. 23 distinct identifiers;
`channel` × 7, `channel-profile` × 3, `channel-core` × 1. Evidence for **U2**.

### P6 — artifact hash comparison across the unreviewed delta

`git rev-parse` on each required-scope artifact at `ff80703` and at the pin, establishing which
artifacts moved in PR #117. Evidence for the Pin section and **U6**.

### P7 — cross-artifact and upstream consistency

`Brontide-Architecture-Status.json` names `docs/current/architecture/Brontide-Architecture-0.8.md`
with SHA-256 `CAC9A02E…B18579`; the file at that path hashes identically and carries "Complete Draft
(document and implementation evidence complete; not ratified)", with no ratified architecture
recorded. All six Architecture 0.8 sections the contract names resolve to matching topics: §6.16
compatibility and authority as separate regimes, §13.6 the invocation principle, §16.4 matching /
projection / unknown structure, §18.1 Composition and Components, §19 architectural extensions, §24
devices and trust admission. Both stacks declare "Designed for: Brontide Architecture 0.8, Complete
Draft, not ratified". Decision 13's relational lifecycle requirement was checked against CM3 C5 and
CM4 C4/C5 rather than against the Channel package's paraphrase of it. PB8's retained findings — B3
mandatory/fabricated effect count, Minimal R1 process loss fabricated zero, Minimal F1 post-request
correlation refusal fabricated zero — were read directly and are the correct antecedent of C10's
certainty form. The four retained Channel 0.1 neutral artifacts exist, and
`conformance/channel-0.1-vectors.json` contains exactly CH-01 through CH-24 matching the ledger's
dispositions. `channel/0.2` does not exist.

### P8 — clone completeness

881 tracked paths, 881 files on disk, empty `git diff HEAD`, `## HEAD (no branch)`. No path-length
truncation.

## What this verdict means

This attestation is negative, so it authorizes nothing. It does not close the first batch, does not
ratify Channel 0.2, claims no implementation conformance, and does not open Batch 2. No closure
record is created, because the policy permits one only when the verdict conforms.

The design was not repaired here. Under the review policy, **U1** is corrected test/contract-first in
a later commit by an actor other than this reviewer, with a failing verifier check written before the
correction, and receives a fresh independent re-review from an identity distinct from all eight
retained reviewers and from that correction's author. This attestation is retained unchanged
whatever that correction decides.

A note for whoever writes that correction. The `unseen` verdict, the R1 ruling, and the S1 ruling are
all sound as reasoning; nothing in this review reopens them. What is missing is a property that can
actually fail. Two routes are visible from here, and both have a cost the S1 ruling's option table did
not weigh: restate `C4-P2` over frames *delivered* rather than *accepted*, which requires the
observation model and the neutral brief's parity profile to carry a frame-order fact they currently
do not (**U3**); or keep the acceptance formulation and give the property a second conjunct that the
fault-and-latch routing cannot make vacuous — for instance, that a recipient never commits
`rejected-protocol` for an identity whose request the sender committed first. The second stays inside
the existing observation model. Neither is chosen here; choosing is an owner decision, and this
reviewer does not repair the design it reviews.

## Note on the design gate

`build/verify-channel-0.2-design.ps1` enumerates the reviews directory from the filesystem and
compares it against a hardcoded `$expectedReviewNames` list. The moment this attestation exists on
disk the gate fails with

```text
FAIL: The Channel 0.2 S1 correction pin must retain exactly the review README and all seven negative
attestations before the next closure review.
```

That is expected and is not a defect. The gate result recorded in P1 was obtained in the isolated
clone before this file was written. The commit that records this review updates that list in the same
change, which is how cycle 7 was recorded.
