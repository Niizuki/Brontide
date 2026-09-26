# Channel 0.2 design-foundation closure review 18 attestation

Reviewer identity: `agent:claude-channel-0.2-closure-review-18-2026-09-26-f4921fc`

Date: 2026-09-26

Reviewed commit: `f4921fcf7ac5cc2242991e3271fb1e95993851eb` (`Merge pull request #164 from
Niizuki/claude/retain-closure-review-17`), whose design artifacts hash identically to the pinned
target `168c730b98a150e9be3b94788393ff1d7034f96a` (`docs(channel): disposition the BL family in the
completeness review and the indexes`).

Reviewed tree: `8a4ec603d8a85fad4394664f6c5b09d4ec7811da` (the pinned commit's tree is
`63d6045d643a93c0bc10542cbdc7d4b714b7d7bd`; the two differ only in
`conformance/channel-0.2-guard-probes.json`,
`docs/future/channel/Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md` and
`docs/future/channel/reviews/README.md`, none of which is a design artifact).

Overall verdict: **`does-not-conform`**

Blocking findings: **BM1**, **BM2**, **BM3**.
Nonblocking findings: **BM4**, **BM5**, **BM6**, **BM7**.

The BL corrections close each of closure review 17's traces as that review wrote them. A two-endpoint
model (probe **P2**) shows that crossing drains now converge (BL1's trace), and that the transport
refuses a close delivered ahead of its sender's Outcome (BL2's trace 1). The repository's own evaluator
is green on BL3's two-endpoint vector. All three blocking findings here come from asking what each
correction **depends on**, and none of the three is a copy of a BL trace.

- **BM1.** Session-control order puts the request ahead of the drain. The recipient's drain rule
  still refuses a request whose admission has not yet resolved, and it sends no frame. The
  recipient's orderly close then faults the initiator's session with a `state-violation` against a
  peer that did nothing wrong. With no race at all, every frameless recipient refusal after dispatch
  that is followed by the recipient's orderly close does the same. That refutes the consequence the
  BL5 ruling states.
- **BM2.** The BL1 correction counts drain per endpoint and leaves no row for this endpoint's first
  local drain once the peer's drain has already moved it to `draining`. The session machine routes
  that pair to `faulted` and the grid routes it to an unchanged state.
- **BM3.** BL3's vector-format half is corrected for transitions and not for the initial state. A
  window that opens while the two endpoints disagree cannot be declared, and the repository's
  evaluator is red on it in one of the two ways the vector can state it.

## Isolation

Isolation is **complete**, with the disclosures below.

- **Review clone.** The review clone is
  `C:/Users/jakub/AppData/Local/Temp/claude/C--Users-jakub-source-repos-Brontide/5d03952a-17c9-4dee-89de-c2e0b14ccd4e/scratchpad/cr18`.
  The dispatcher made it from `https://github.com/Niizuki/Brontide.git` and detached it at the
  reviewed commit. I verified it in the clone rather than taking it from the dispatch:
  - HEAD is `f4921fc`;
  - there are **943 tracked paths**;
  - `git status --porcelain` was empty at the start; and
  - `origin` is the public remote.
- **Author's working repository.** Nothing in this session read, wrote, or executed anything in
  `C:/Users/jakub/source/repos/Brontide`. **Disclosure:**
  - The session harness put three things into this session's initial context before any command ran:
    that repository's `AGENTS.md` text, a snapshot of its `git status` and recent log, and a
    three-line index of the owner's auto-memory titles about pushback on policy conflicts, gate
    self-checks mutating a worktree, and a condition-4 pass workflow.
  - No memory entry was opened.
  - I read `AGENTS.md` from the clone (blob `ed891d6cc4b7`, identical at the pinned commit).
  - The harness's shell starts each command with its working directory set to the author's repository.
    Every command here first changed to a clone, and none read or wrote anything in the author's
    repository.
- **Throwaway clones.** Every gate run, probe vector, and self-check ran in separate throwaway clones
  made with `git clone --no-hardlinks` from the review clone and detached at `f4921fc`:
  - `C:/temp/cr18rv-probe` held the gates and probe vectors. It was restored with `git checkout -- .`
    after each probe and confirmed clean.
  - `C:/temp/cr18rv-self` held the self-modifying gate self-checks only.
  - An earlier attempt under the scratchpad failed on Windows' path-length limit and was deleted.
  - The models live in `C:/temp/cr18rv-scratch`.
  - `C:/temp` already held `cr18` and `cr18-scratch` directories this session did not create. None of
    them was opened or used.
- **Review clone left unedited.** It was never edited except by writing this file.
- **Independence.** The reviewer identity differs from all seventeen retained reviewers, every
  correction author, every condition-4 pass author, and the dispatching session. No author private
  reasoning was available and none was supplied.
- **No repair.** The design was **not** repaired. Nothing was committed or pushed.

## Disclosed dispatch provenance

The dispatching brief states that its session had no prior involvement in this work. Before
dispatching it read these, and nothing else:

- `AGENTS.md`;
- the Priority 1 section of the future-work index;
- parts of the review policy; and
- the review 17 attestation's header.

It also read memory notes from an earlier session about how the gates are run. It ran the design gate
and checked the stat of the diff from the pinned commit to HEAD. It named no defect and no area of
suspicion, and it told this reviewer to re-verify rather than accept any of that. I did: see **Pin**.

**What the brief narrowed, itemised so the next cycle can discount it.** The brief gave five
instructions, each a restatement of a standing policy requirement, and each moved effort:

1. *Verify the pin.* I did this first. The result was negative: the clause holds.
2. *Falsify rather than read.* This took the largest share of effort and produced:
   - **BM1** and **BM2**, from the two-endpoint model **P2**;
   - **BM3**, by the repository's own evaluator over a vector the format cannot state (**P3**); and
   - the confirmed limit recorded under **P4**.
3. *Re-derive retained findings from their own evidence sentences.* This produced **BM3**: BL3's
   evidence and review 17's neutral-brief verdict name the brief's per-session initial state, and the
   correction reached the transitions only. It also produced **BM7**, BL6's own evidence citing the
   policy's count of four clean passes, and part of **BM4**, BL12's evidence naming the grid.
4. *Follow propagation.* This produced **BM4** (the grid, the terminal-provenance table and the
   ledger's closed detailed-reason set), **BM5** (the ledger's ordering rows) and **BM6** (the session
   machine's claim about what its drain row relies on).
5. *Review the registry's architecture.* The result was negative (**P6**).

One further input shaped the effort. The review 17 attestation, which I read in full as retained
evidence, ends by asking the next reviewer to "build the second endpoint before reading anything else".
**P2** is that model. Without it BM1 and BM2 would not have been found. I credit the retained record
for the method, not for either finding: neither trace appears in it.

Effort went heavily to these areas:

- the session machine;
- C2, C4, C8 and C12;
- the interaction machine's refusal rows;
- the grid;
- the brief's vector format; and
- the BL disposition surfaces.

C5, C7 and C11 were read and not probed. C1, C3 and C6 were read against the BL diff and found
unchanged by it. I did not rebuild a `C4-P2` evaluator of my own. Reviews 16 and 17 each did, and the
only change to that property since is **BL10**, which I checked through the gate and through the
self-check corpus's `BL10-a` probe (see **Retained findings**).

## Pin

I checked the policy's pin clause against the repository rather than against its own wording (`X6`,
`AI8`).

| Clause states | Repository |
| --- | --- |
| target commit titled `docs(channel): disposition the BL family in the completeness review and the indexes` | `168c730` carries exactly that subject, and it is the only commit in history that does |
| committed 2026-09-25 | `git log -1 --format=%ci 168c730` gives `2026-09-25 08:33:09 +0200` |
| "or any later commit whose design artifacts hash identically" | See the list below the table |
| "the last of the correction to touch one" | `f62dff2`, `b2bb55c` and `9b3bf7f` touch only the plan, the probes and the policy |

On hash identity, these files have identical blob hashes at `168c730` and at `f4921fc`:

- all nine design artifacts the design verifier's pathspec derives from the required scope: the
  redesign plan `356c02a5a2a7`, the contract `51e45f91f1a7`, the session machine `5d7677ef4e9d`, the
  interaction machine `780f16290453`, the grid `0546eed1817e`, the matrix `f3c15f1c1ebe`, the
  completeness review `e4931ee2b816`, the ledger `e5b1f3237973` and the brief `e6be3d60b17e`;
- the retained 0.1 Design Note, the Draft Contract, the requirements ledger, `channel-0.1-vectors.json`,
  the Architecture 0.8 document, `binding/portable/open-decisions.md`, the status registry and
  `AGENTS.md`.

`git diff --name-only 168c730 HEAD` lists exactly the three non-design files the dispatcher named.

The clause is correct as to target, date, and hash identity. **I have no finding on the pin itself.**

## Blocking findings

### BM1 — a recipient-side frameless refusal after dispatch, followed by the recipient's orderly close, faults the initiator's session with a `state-violation` against a conforming peer; session-control order puts the request ahead of the drain and the recipient's drain rule still refuses it, so BL2's trace 2 recurs through `validating`, and the BL5 ruling's stated consequence is false

**Artifact/section evidence.**

- `Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`, `## Recipient transitions`:
  - line 97: `validating` + "drain refuses this still-admitting interaction" → `refused-local`, "no; an
    interaction whose admission has not resolved is outside the drain snapshot";
  - line 90: phase refusal → `refused-local`;
  - line 91: authority denial → `refused-local`, and `refused-local` emits no peer frame (line 46).
- The same file, lines 112-121, is the BL5 limit: "Its only exit is a local loss observation -- `lost`
  with `unknown` certainty -- … the host's timer or a transport loss observation closes it. Until then
  the interaction holds its in-flight slot, and orderly close waits because close requires an empty
  local in-flight set … the design is consistent, the initiator records its loss honestly".
- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, C8, lines 499-506: "If the session or transport is
  lost, or drain refuses the interaction, while a control is held …; An interaction still in
  `validating` when drain arrives is not in the drain snapshot".
- The same file, C2, lines 90-91: "Drain refuses new interactions while allowing already admitted ones
  to reach a terminal fact".
- `Brontide-Channel-0.2-Session-State-Machine-0.1.md`:
  - `## Drain protocol` item 5, line 109: "close is legal only when the **local** in-flight set is
    empty";
  - item 6, lines 110-111: "a peer close with locally nonterminal interactions is a protocol fault";
  - line 74: `draining` → `closed` needs "all admitted interactions terminal";
  - `## Refused and illegal inputs`, line 91: `draining` + "close while interactions remain
    nonterminal" → "fatal session `state-violation`";
  - lines 113-118, the BL2 correction: "a drain could overtake a request admitted before it, and the
    receiver would call that request a violation. Stating the order these rules assumed is **BL2**."
- `Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md`, `## Required silence probes`:
  - line 148: "peer closes before drained work ends | session fault";
  - line 160, the BL5 row: "the initiator stays `dispatched` with no terminal fact until an out-of-core
    timer or a transport loss observation closes it as `lost`".
- `Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md`, 2026-09-25 BL12/BL5 ruling, lines
  693-697: the same consequence, "a recipient-side authority, phase or drain refusal after the request
  crossed dispatch leaves the initiator `dispatched` with no terminal fact until an out-of-core timer
  or a transport loss observation closes it as `lost`"; and the BL2 ruling, lines 663-676.
- `Brontide-Channel-0.2-State-Event-Coverage-0.1.md`, lines 80-83: faulting a conformant endpoint
  "for losing an unobservable race" is what the R1 ruling refused.

**The failure (probe P2, `C:/temp/cr18rv-scratch/session_pair_model.py`).** Each endpoint runs its own
local history. The transport refuses any schedule that violates intra-interaction frame order or
session-control order, so every schedule below is one a conforming realization may produce.

1. *P2-c: the drain arrives while the request is still validating.* A is `established`. A admits and
   dispatches `i2`, then begins drain, so `i2` is in A's drain snapshot. The request and the drain are
   delivered in commit order, which is exactly what session-control order guarantees. B receives the
   request (`validating`) and then the drain before its admission of `i2` resolves. Line 97 refuses
   `i2` framelessly. B's local in-flight set is now empty, so B's close is legal under item 5. A
   receives the close with `i2` nonterminal and applies line 91: it records a fatal session
   `state-violation`, commits at most one peer fault asserting that B violated the machine, and maps
   `i2` to `lost` with `unknown`.

   *P2-c′* is the same frames with B's admission resolving an instant before it processes the drain.
   `i2` executes, its Outcome arrives, and both endpoints close orderly with `i2` succeeded.

   The same committed frames therefore produce two contradictory histories, and B's internal timing,
   which A cannot observe, selects between them. That is R1's falsifying trace almost word for word,
   and it is BL2's trace 2 with the reordering removed: session-control order made the request arrive
   first, and the recipient's drain rule refuses it anyway.
2. *P2-d: no race.* B's local authority denies `i3` (line 91, frameless). Later B begins drain, which
   is legal since B is `established`, and closes, which is legal since B's in-flight set is empty. A
   receives the drain and then the close with `i3` still `dispatched`, and faults the session with a
   `state-violation` against B. This is deterministic: it happens whenever the recipient shuts down
   before an out-of-core timer fires. A phase refusal (line 90) behaves identically.

**Why this is blocking.** It has the three consequences closure review 17 rated blocking for BL1 and
BL2, and each of them was ruled on before:

- **C2's drain promise fails on conforming behaviour.** `i2` was admitted at A before A drained, and it
  does not reach a terminal fact. It is destroyed.
- **A records that B erred when B did not.** That is the C9 argument the closure re-review made for
  **R1**, which was blocking.
- **P2-c and P2-c′ give contradictory outcomes for identical conforming behaviour**, selected by a
  timing neither endpoint observes. The 2026-09-25 BL1 ruling rejects exactly this shape, at lines
  657-661 of the plan.

It also falsifies a normative statement. The interaction machine, the silence-probe table and the BL5
ruling all say the initiator's exit is a timer or a transport-loss observation. Its actual exit, in the
common case of a recipient that shuts down, is a session fault that accuses a conforming peer. The
silence-probe row at line 148 answers "peer closes before drained work ends" with "session fault" on
the premise that such a close is premature. The frameless refusals make it legal.

**Why it survived.** Review 17 reported BL5 as a stranded *initiator*, and the ruling and the
correction examined the initiator's own close: "orderly close waits". Neither examined the recipient's
close, which does not wait, because its in-flight set is local and the refusal emptied it. The BL2
correction was checked against reordering, and P2-c needs none. No instrument in the repository
carries a transport or two endpoints' interaction states, so no gate could have seen it. The limit is
stated in the completeness review's BL disposition, lines 1009-1013.

### BM2 — the BL1 correction counts drain per endpoint and leaves this endpoint's first local drain, once the peer's drain has moved it to `draining`, with no legal row; the session machine routes the pair to `faulted`, the grid routes it to an unchanged state, and a timing neither endpoint controls selects between convergence and a fault

**Artifact/section evidence.**

- `Brontide-Channel-0.2-Session-State-Machine-0.1.md`:
  - `## Events`, line 55: `begin-drain` is initiated by "either endpoint or local owner" and transmits
    a drain control;
  - `## Legal transition table`, line 71: `established` + "local or peer drain begins" → `draining`;
  - line 72: `draining` + "the peer's first drain control, received after this endpoint's own drain
    began" → `draining`;
  - line 73: `draining` + "a second drain control from the same peer, or a second local drain" →
    `faulted`;
  - `## Drain protocol` item 2, lines 100-104: "each endpoint sends at most one drain control, so the
    peer's first drain control is legal in `draining` whichever endpoint drained first";
  - `## Session event totality`, lines 125-130: "A recognized event in a nonterminal state that has no
    more specific legal row is a session-scoped `state-violation` and moves the session to `faulted`
    … A wrong-state local action that has not emitted a frame is a frameless local refusal and leaves
    the state unchanged **only where the refused/illegal table says so**". The refused table, lines
    84-93, has no `draining` + local drain row.
- `Brontide-Channel-0.2-State-Event-Coverage-0.1.md`:
  - line 48, the `draining` / Drain cell: "peer's first drain → remains `draining`, the crossing case;
    a second drain from the same peer → `faulted`". No local drain is listed.
  - `## Closed-world totality rule`, lines 30-31, rule 3: "otherwise, a wrong-state local action that
    emitted no frame is a frameless local refusal and **preserves the current state**";
  - lines 37-39: "An implementation cannot ignore a recognized event, select between 'unchanged' and
    'faulted', or invent an extra state."
- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, C2, lines 100-105, and the redesign plan's
  2026-09-25 BL1 ruling, lines 651-661, which rejected keeping a fault for "identical conforming
  behaviour selected by a timing neither endpoint observes".

**The failure (probe P2-b).** A begins drain and sends its control. B is `established` and receives
A's drain, which moves it to `draining` under line 71. B's host then asks B to begin drain, because it
decided to shut down at the same moment and cannot observe that A's control has just been processed.
This is B's **first** local drain, and under item 2 B has sent no drain control, so it may send one.
Two readings follow, and each is a defect:

- **Literal reading.** Line 72 is keyed to a received peer control. Line 73 reads "a *second* local
  drain", and this is the first. No legal row matches:
  - the session machine's totality rule makes the pair a session-scoped `state-violation` →
    `faulted`, with every admitted interaction → `lost`;
  - the grid's rule 3 makes it a frameless refusal that preserves `draining`.

  The detailed authority and the grid disagree on one recognized pair. That is "select between
  'unchanged' and 'faulted'", which the grid says an implementation cannot do, so two stacks may
  legitimately diverge. It also violates `C12-P1`'s single deterministic expected observation.
- **Broader reading.** "A second local drain" means "any local drain once `draining`". Then line 73
  faults B, and BL1's consequence is reached at one endpoint instead of across the wire. Had B's host
  call been processed an instant earlier, B would have sent its drain control, A would have received
  it under line 72, and both would converge (P2-a). An instant later, B faults the session and
  destroys the admitted work drain exists to let finish.

  Both trace orders are legal, and the difference between them is a timing B's host cannot observe.
  That is what the BL1 ruling rejected, and the per-endpoint principle ("each endpoint sends at most
  one drain control") makes this B's legitimate first control.

Before the correction, line 73 read "duplicate local or peer drain control" and C2 read "a second
local or peer drain". Both routed the pair determinately to `faulted`. The correction narrowed the row
to "second local drain" and the grid cell to peer drains. That opened the literal-reading disagreement
and left the broader reading's race.

**Why this is blocking.** It is BL1 closed as to its trace and open as to its principle, one boundary
inward, which is the "closed in the first, open in the second" class the policy's method notes
describe. It also makes the grid's closure claim false for a pair the correction created. If the owner
rules that a host must not ask a `draining` endpoint to drain, the finding reduces to the machine/grid
disagreement, which still needs a row before Batch 2 enumerates the grid.

**Why it survived.** BL1 was raised, and corrected, as a race between two endpoints' *wire* decisions.
The event table names two initiators of `begin-drain` — "either endpoint or local owner" — and the
correction's rows are written over controls and "local drain" counts without asking what a first local
drain means once the peer has already moved the endpoint to `draining`. The properties gate cannot see
the pair: `S1` reads edges only and `draining>draining` is now a legal edge (**P4**).

### BM3 — BL3's vector-format half is corrected for session transitions and not for the initial session state; a conforming vector whose window opens while the two endpoints' local histories disagree cannot be declared, and `S2` and `C2-P1` are red on it in the repository's own evaluator

**Artifact/section evidence.**

- `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md`, `## Vector format`:
  - line 211: "the established profile and initial session/interaction state of **each session the
    vector carries**", which is one state per session;
  - lines 235-238, the BL3 correction: transitions now name "the **endpoint** whose local history of
    the session it belongs to".
- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, C12, lines 724-728: the state "is also **each
  endpoint's own** … the two endpoints of one session hold two local histories of it, which may
  legitimately disagree at any instant -- one already `draining` while the other still admits under
  `established`".
- `channel-0.2-design-foundation-closure-review-17-attestation.md`:
  - BL3 evidence (its line 231): the brief's "one 'initial session/interaction state of **each
    session**'";
  - its Neutral brief verdict (its line 466): "one initial session state per session and no endpoint
    on a session transition cannot express two endpoints' histories". Two halves are named there.
- `reviews/channel-0.2-disposition-index.md`, line 436, records the brief's BL3 correction as "the
  vector format has every session transition name the endpoint", which is the second half only.
- `build/verify-channel-0.2-properties.ps1`:
  - lines 621-628: `Get-InitialSessionState` reads one `initialSessionState` per session record;
  - lines 637-640: state is keyed by (session, endpoint), but an endpoint without a transition yet
    falls back to that per-session value;
  - lines 2839-2852: the consistency check requires **every** endpoint's first transition to depart
    from that single stated state.

**The failure (probe P3, executed with the repository's own evaluator).** I built the vector in the
throwaway clone. It is closure review 17's `S-two-endpoints-one-session` with the window opening one
instant later: the recipient has already begun drain before the first recorded step, and the initiator
is still `established` and has not yet received it. Every remaining step is unchanged, and each is
legal in the history of the endpoint that records it. The vector must state one initial state for
`s1`, and I declared it `additional-green` for `S1`-`S4` and `C2-P1`.

- Stated `established`: the gate fails the vector itself ("its timeline's first transition for that
  session at the recipient departs from 'draining'").
- Stated `draining`: the gate fails it the same way at the initiator, **and** `S2` is red ("interaction
  i1 dispatched while its own session s1 was draining") and `C2-P1` is red through its middle clause.
  The read-provenance, field-readership and operand censuses all report the declared/actual
  disagreement.

The brief's format therefore cannot express an input that C12 says is legal. The only statements
available are rejected by the harness, and one of them is red on conforming behaviour.

**Why this is blocking.** BL3 was blocking, and review 17 put the brief's per-session initial state in
its evidence and in its Neutral-brief verdict. The correction reached the transitions and left the
initial state, so BL3 is closed as to one of its two named surfaces. A mid-session window is not an
edge case the programme has disowned. **BF3** made `S2` and `C2-P1` start from the stated initial state
precisely because "a conforming realization whose window opens mid-session was red on both". Review 17
also warned that "a correction for BL3 that adds the endpoint to the property statements and not to the
vector format … will be the eleventh cycle of the class". The endpoint reached the statements and half
the format.

## Nonblocking findings

### BM4 — the BL12 correction reached the interaction machine's prose and C10, and not the grid, the terminal-provenance table, or the ledger's closed detailed-reason set that its route depends on

**Artifact/section evidence.**

- `Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`, lines 123-130: the initiator drops the
  frame, records "one local observation … under provenance `rejected-protocol` with the same detailed
  reason the recipient's `unseen` refusal carries", sends nothing and retains nothing.
- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, C10, lines 595-602, extends the fenced `unseen`
  record to it.
- **The grid**, `Brontide-Channel-0.2-State-Event-Coverage-0.1.md`:
  - the initiator grid, lines 52-61, still begins at `candidate` / `admitting` and has no row or cell
    for a frame naming an unopened identity;
  - lines 163-165 still say "A route that reaches no terminal interaction has no latch … **The `unseen`
    refusal is the only such route today**". That is false since BL12: the initiator's drop reaches no
    terminal interaction and owes the same `not-applicable` latch assertion;
  - lines 174-176 require "Each cell asserts … late-traffic latch — the last as `not-applicable` where
    the cell's route reaches no terminal interaction", and this route has no cell to assert it in.

  BL12's own evidence named "the grid's initiator rows begin at `candidate`/`admitting`" and grid
  totality rules 2 and 6. The disposition index's grid entry, line 287, records BL1, BL4 and BL6 and
  not BL12.
- **The terminal-provenance table**, same interaction-machine file, lines 288-304. Its only rows
  carrying `rejected-protocol` answer "Peer Channel statement? **yes**". The initiator's drop records
  that provenance and makes no peer statement. The machine's own argument for listing the recipient's
  row applies to it word for word: "Leaving it out of this table would leave the record … with no
  declared provenance, and a table of terminal histories is exactly where a reader looks for one"
  (lines 301-304). `C9-P1` requires every terminal vector to select exactly one of four provenance
  forms, and the design does not say which form this record is.
- **The migration ledger**, `Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md`, line 128. The closed
  detailed-reason set scopes `unopened-interaction-identity` to "a recognized control naming an
  identity **the recipient** has never accepted", and argues that the five identity reasons (missing,
  extra, wrong-session, reused, mismatched) do not cover it. At the initiator the frames BL12 names
  include "a well-formed terminal fact". C4's failure clause, line 186, says an "extra" identity
  rejects a claimed terminal fact, so a terminal fact naming an identity the initiator never opened now
  has two candidate reasons in a closed set that the parity profile compares.

Rated nonblocking because BL12 was nonblocking and C10's general rule gives the latch value by reading
upward. It is still AG2's and AJ1's shape: a correction that makes a claim about artifacts it did not
edit.

### BM5 — the migration ledger still says intra-interaction frame order is the one ordering fact 0.2 adds, and the one accepted instance of `CH-K5`'s over-scoping risk; BL2 added a second

**Artifact/section evidence.**

- `Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md`:
  - line 30, the `CH-R10` register row: "The ordering clause is narrowed by the 2026-08-13 S1 ruling …
    `CH-K5` … is **retained** and **this narrowing is its one accepted instance**";
  - line 191, the "ordering guarantee unsupported" row: "Replaced by a scoped non-promise plus **one
    narrow promise** … **This is the one ordering fact 0.2 adds over 0.1**".
- By contrast:
  - C4, lines 170-179 and 380-384, says "Core promises one ordering fact more, **session-control
    order**" and "The two are the whole of the ordering Channel 0.2 core promises";
  - C11, lines 657-658, says the same.
- `architecture-0.8-channel-requirements-and-risk-ledger.md`, line 73: `CH-K5` names over-scoping into
  ordering as a Medium risk.
- The disposition index's ledger entry, line 391, records only the new-evidence inventory.

The BL2 correction reached the ledger's new-evidence inventory (lines 276-279) and left the two rows
that disposition the retained register entry and risk every ordering finding turns on. The register
row is where Architecture 0.8's "Delivery, ordering, and retry are promised by no one" (§19, line 2655)
is traced to 0.2. It now traces one of the two narrowings. Rated nonblocking because C4 is the owning
statement and is correct.

### BM6 — the refused row for a new peer interaction during drain fires on the conforming cross-endpoint crossing that session-control order does not cover, calls the conforming initiator a `state-violation`, and lets a profile make it fatal through a declaration no artifact carries

**Artifact/section evidence.**

- `Brontide-Channel-0.2-Session-State-Machine-0.1.md`:
  - line 90: `draining` + "new peer interaction" → "interaction-scoped `state-violation`; session may
    continue draining **unless the profile declares it fatal**";
  - lines 113-118, the BL2 correction: "Items 5 and 6, **and the refused row for a new peer interaction
    during drain**, rely on session-control order".
- `conformance/channel-0.2-property-vectors.json`, `S-two-endpoints-one-session`, BL3's own
  required-green member. It is exactly this crossing: the recipient drains, the initiator
  (`established`, not yet aware) admits and dispatches `i1`, and `i1` ends in a `protocol-fault`
  terminal.
- The redesign plan's BL1 ruling, lines 646-648 and 657-661, rejected "a `state-violation` accusing a
  peer that erred in nothing" and rejected keeping such a fault even as a deliberate limit.
- The brief's `## Version and establishment rule`, lines 105-136, the responsibility matrix, and C12's
  declared facts carry no "new peer interaction during drain is fatal" profile declaration.

Session-control order covers the case where the request was committed before *its own sender's*
drain. It does not cover the case where the request was committed before the sender *received the
peer's* drain. Line 90 fires on that case, and the repository's own required-green vector records it
doing so. The sentence at lines 113-118 is therefore half true. In the nonfatal default only the new
interaction is refused, which is unavoidable, but it is refused with a category asserting that a
conforming initiator violated the machine. Under the "unless the profile declares it fatal" branch, the
same crossing faults the session and destroys admitted work, which is BL1's consequence permitted by a
profile field that no profile schema, owner row, or vector format has. Rated nonblocking because no
profile selects the fatal branch today and no schema could express it. **This is this review's closest
escalation call.** If that branch is a live profile option, it is BL1 reached through a profile and is
blocking.

### BM7 — BL6's and BL7's class in the review policy's own status block and step 4, and BL6's evidence sentence left disagreeing with the Channel index

- `docs/future/channel/reviews/README.md`, the status block, lines 3-8. It reads "sixteen retained
  independent reviews, fifteen negative and one conforming". Its family narrative runs from the
  sixteenth review back and never mentions the seventeenth or the BL family. Seventeen are retained
  and sixteen are negative (`ls reviews/*attestation.md` gives 17). The block was last edited
  2026-08-17. `b5588dd`, which retained review 17, added its roster entry and provenance section but
  not the status block.
- Step 4, line 876, tells the dispatcher that the reviewer must be "distinct from … all **sixteen**
  retained reviewers", while the review 17 provenance section, lines 1749-1751, says seventeen.
- `docs/future/channel/README.md`, lines 47-49: "the thirteenth, the twenty-fourth and the
  twenty-fifth found nothing in the package". The policy at lines 294-295 says four, counting the
  eleventh under the 2026-09-16 AW2 ruling. BL6's evidence sentence was "the review policy's own count
  is four". The correction rewrote the sentence and kept three.

BL7's correction recomputes two surfaces' counts (the plan's §7.8 and the repository README) and does
not read the policy's own status block, which is the first thing a closure reviewer reads. Rated
nonblocking: none of these is a design artifact, and the status block's own text tells a reviewer not
to trust it.

## Capability verdicts

| Item | Verdict | Rationale |
| --- | --- | --- |
| C1 | conforms | Unchanged by the BL corrections apart from the status line. `C1-P1` is per session, and fixed/negotiated equivalence agrees with the session machine and the brief. |
| C2 | **does-not-conform** | **BM1**: the drain promise fails for an interaction the initiator admitted before draining, and a legal close faults the session. **BM2**: the per-endpoint drain count leaves a recognized pair routed to `faulted` by the machine and preserved by the grid. **BM6** is nonblocking. BL1's crossing and BL3's `C2-P1` scope are closed as to their traces (P2-a; the gate). |
| C3 | conforms | Unchanged. A receiver-local phase refusal is one of BM1's triggers, but the defect lies in C2's close rule, not in C3's refusal. |
| C4 | conforms-with-nonblocking-findings | Session-control order is stated, owned, declared and scoped (C4, C11, matrix row, brief). The P2-e check confirms it forbids BL2's trace 1. `C4-P2`'s declared verdicts, now twelve with `C4-outcome-precedes-ack-fault-unavailable`, all hold in the gate. **BM5**: the ledger still counts one ordering fact. |
| C5 | conforms | Read, not probed. Unchanged. |
| C6 | conforms | Unchanged. The frameless local denial is correct as C6 states it, and BM1's deterministic trace starts from it, but the defect is C2's. |
| C7 | conforms | Unchanged since review 17's P10. Decision 13 was re-read, and C3, C7 and the interaction machine's `## Relational initialization` carry every clause. |
| C8 | **does-not-conform** | **BM1**: C8's rule that an interaction "still in `validating` when drain arrives is not in the drain snapshot" makes the recipient refuse, framelessly, an interaction that is in the initiator's snapshot, which is how P2-c destroys it. BL4 is closed. |
| C9 | conforms-with-nonblocking-findings | **BM4**: the initiator's drop is recorded under a provenance the terminal-provenance table maps to a peer statement that is never made. BM1 violates C9 in effect by producing a peer accusation against conforming behaviour, as BL1 and BL2 did. |
| C10 | conforms-with-nonblocking-findings | **BM4**: C10 extends the `unseen` record to the initiator, and the ledger's closed-set member it uses is scoped to the recipient. |
| C11 | conforms | Read, not probed. It names both core ordering facts. |
| C12 | **does-not-conform** | **BM3**: C12's own `session state` entry says the endpoints' local histories may disagree at any instant, the vector format cannot state that at a window's opening, and the executable form is red on one way of stating it. **BM2** also violates `C12-P1`'s deterministic expectation. |

## Area verdicts

| Area | Verdict |
| --- | --- |
| Session state | **does-not-conform**: **BM1**, **BM2**, **BM3**, with **BM6** nonblocking. The six states, the per-endpoint properties and BL1's crossing row are otherwise sound. |
| Interaction state | **does-not-conform**: **BM1**. Line 97 produces P2-c, and the BL5 paragraph at lines 112-121 states a consequence the model refutes. **BM4** is nonblocking. BL4 and BL12's routing prose are otherwise correct. |
| State/event totality | **does-not-conform**: **BM2** is a recognized pair with two routes. **BM4**'s initiator route has no cell, and the grid's "only such route" is false. 108 published cells, 0 empty (P5). |
| Responsibility | conforms. **40** concerns, each with exactly one owner from the closed **22**-identifier vocabulary, every identifier used (P5). The `Session-control order` row is BL2's. |
| Completeness | **does-not-conform**: **BM1**. The BL5 silence-probe row, line 160, and the "peer closes before drained work ends" row, line 148, give answers the model refutes. The operand enumeration's BL9 row is correct. |
| Migration coverage | conforms-with-nonblocking-findings: **BM5**, and **BM4**'s closed detailed-reason scope. All **24** predecessor vectors are dispositioned `retained` or `replaced` by identifier (P5), and `CH-R10` is explicit. |
| Neutral brief | **does-not-conform**: **BM3**. The establishment rule carries both ordering declarations. |

## Owner rulings

The four first-batch rulings recorded 2026-08-11 are represented consistently throughout the
first-batch design.

- **Core concurrency and cancellation.** C4 `max-in-flight`, C8, the interaction machine, the matrix
  rows `Bounded unary concurrency` and `Class-specific cancellability` → `channel-profile`, and the
  ledger's limit and feature tables all carry it.
- **Session-state ownership.** The six Channel states are unchanged: BL1 adds a self-loop, not a state.
  Portable Binding owns Interconnection, Release, withdrawal and cleanup, Composition owns the
  Relational Initialisation phase, and Component Management owns Ready. The matrix rows and
  `## Selected boundary rulings` agree.
- **Relational initialization representation.** C3, C7, the interaction machine and the matrix
  `cm3-lifecycle-contract` row are unchanged since review 17.
- **Extension invariants.** C11 now says a facet "may add delivery and ordering guarantees beyond them
  but may not weaken either", which is consistent with the ruling.

None of BM1-BM7 contradicts any of the four.

The correction rulings (R1, S1, AE1 with its AF8 narrowing and AK1 note, and the four of 2026-09-25)
and the 2026-08-15 closure-standard ruling are recorded as issued. Three observations are not
findings:

- **BM1** refutes the consequence the 2026-09-25 BL5 ruling states, and **BM2** shows the BL1 ruling's
  per-endpoint principle realized for wire controls and not for the local owner. Both are recorded
  against the artifacts, not against the rulings' choices.
- Like the 2026-08-13 rulings, the 2026-09-25 entries in the redesign plan do not record who issued
  them. Decision 13, for example, records "by user:JakHoh". The commits that record them are the
  owner's account with an agent co-author. I take them as issued, as review 17 took the earlier ones.
- The redesign plan's "There are no unresolved owner decisions" (line 468) remains correct as to the
  design package's own question list. BH3 is open in the verification foundation plan.

## Architecture selected by the status registry

`Brontide-Architecture-Status.json` selects Architecture 0.8, "Complete Draft (document and
implementation evidence complete; not ratified)", at `docs/current/architecture/Brontide-Architecture-0.8.md`
(line 9 of that document states the same status). No architecture is ratified. All **twelve** SHA-256
pins recompute and match in the clone; there are eleven distinct files, and the 0.8 requirements file
is pinned twice.

Both stacks' READMEs state "Designed for: Brontide Architecture 0.8, Complete Draft, not ratified".
Neither mentions Channel 0.2, so neither claims a Channel 0.2 realization, and no stack limitation is
affected.

§19's Channel direction (line 2655) says "Delivery, ordering, and retry are promised by no one" of the
0.1 recorded frame. Channel 0.2 now narrows that twice. C4 states both narrowings, and the ledger's
`CH-R10` disposition traces only the first (**BM5**). No ratified architecture constrains either
narrowing. **Negative result** apart from BM5's traceability gap.

## Retained findings

Each BL finding was re-derived from its own evidence sentences in the artifacts those sentences name.

- **BL1**: closed as to its trace. Crossing drains converge (P2-a); line 72, C2 and the grid cell agree;
  the `BL1-a` probe is in the corpus. **Open as to its principle** at the local-owner boundary: that is
  **BM2**.
- **BL2**: closed as to both traces. The model's transport refuses a close delivered ahead of its
  sender's Outcome (P2-e), and a request committed before its sender's drain is delivered first. C4,
  C11, the matrix row, the brief's establishment rule, the session machine and the ledger's
  new-evidence inventory carry it. Trace 2's consequence recurs through `validating` (**BM1**), and the
  ledger's register rows were not reached (**BM5**).
- **BL3**: closed for the properties, the evaluators, C12 and transitions. The repository's evaluator is
  green on `S-two-endpoints-one-session`, and `BL3-a` reverts the key and is red. **Open** for the
  initial state that its evidence and review 17's brief verdict named: that is **BM3**.
- **BL4**: closed. Line 95 (the held control) and line 100 (`executing`) emit `accepted`, and grid line
  69 asserts both acknowledgements.
- **BL5**: stated as a limit as ruled. The limit's stated consequence is false (**BM1**).
- **BL6**: closed in all nine design status blocks ("released on 2026-09-24"; `grep "on hold"` finds
  none), with the `BL6-a` probe in the corpus. The Channel index's condition sentence is corrected, and
  its count of clean passes still disagrees with the one BL6 cited (**BM7**).
- **BL7**: closed. The plan's §7.8 reads seventeen, sixteen and one, and the repository README reads
  seventeen; both are recomputed by the gate, and `BL7-a` is in the corpus. The class recurs in the
  policy's own status block (**BM7**).
- **BL8**: closed. The sweep keys on the bare trigger with `provenance` and any one of three field
  terms, and `BL8-a` and `BL8-b` are the two instances in the evidence.
- **BL9**: closed. The row names `S2`, `S3`, `S4` and `C2-P1`.
- **BL10**: closed. The declaration and the evaluator (`build/verify-channel-0.2-properties.ps1`, line
  436) read both settled values, `C4-outcome-precedes-ack-fault-unavailable` is a named mutation
  declared red and red in the gate, and the completeness review's C4 row names it.
- **BL11**: the generator is corrected. The refused claim routes the interaction to `lost` with
  `unknown`, and the gate reports 0 red. That no property distinguishes the two routings is kept as the
  self-retiring limit probe `BL11-a` and stated in the completeness review's disposition. I accept it as
  disclosed. It means the grid's evidence clause (lines 177-178) is not yet met for that row, which
  Batch 2 inherits.
- **BL12**: closed in the interaction machine's prose and in C10. Open in the grid, the
  terminal-provenance table and the ledger's closed set (**BM4**).

Earlier families, checked against their evidence at this pin:

- `AL1`: `S3` names the endpoint's local history of each session.
- `AL2`: the grid's `unseen` cells are fenced renderings, and the facts gate passes over 21 publications.
- `AL3`: C12 declares `session state`, now per endpoint.
- `AL4`: `S5` is per session.
- `AK1`, `AK5` and `AK6`: the three references and the record are rendered from
  `conformance/channel-0.2-facts.json`.
- `AE1`: `C4-P2` reads the subsequent admission in the same session.
- `R1`: the held-control row is at line 92.
- `B2`: the `refused` acknowledgement is at line 101.
- `T4`: status blocks use the stable phrase.
- `S3` and `AI9`: the plan's §7.8 count is correct.
- `U8`: the pre-dispatch Local-loss cell names `lost`.
- `AC2`: `unopened-interaction-identity` is in the closed set.

All are closed as their evidence states, except as recorded under BM4-BM7. Families before `AL` were
checked by sample, not individually.

## Probes performed

### P1 — gates in the throwaway clone at `f4921fc`

| Gate | Result |
| --- | --- |
| `build/verify-channel-0.2-design.ps1` | exit 0 — "11 required artifacts, C1-C12 with properties/scenarios/silence, total session/interaction event coverage, 6 session states, all 24 predecessor vectors dispositioned, 4 owner rulings resolved, and independent review still pending." |
| `build/verify-channel-0.2-properties.ps1` | exit 0 — 26 of 26 properties executable, 145 evaluations over 63 declared inputs, 9 operand mutations, 2,600 generated evaluations over 100 vectors at 0 red, and every census at 0 findings |
| `build/verify-channel-0.2-facts.ps1` | exit 0 — 4 declared facts rendered into 21 fenced publications across 6 artifacts |
| `build/verify-channel-0.2-return-channels.ps1` | exit 0 — 31 producers, 22 consumers across 5 gates |
| `build/verify-channel-vectors.ps1` | exit 0 — 24 vectors |
| `build/verify-text.ps1` | exit 0 — 937 UTF-8 files |
| `build/verify-doc-links.ps1` | exit 0 — 1,092 local links, 105 heading fragments, 339 documents |

These ran before this file existed. I then copied this file into the throwaway clone and re-ran:

- `verify-text` passes over 938 files.
- `verify-doc-links` passes over 340 documents.
- The owned-fact gate passes.
- The design gate fails with exactly **eight** failures. Each is bookkeeping a retained attestation
  owes: the review-file roster, the retained-attestations list, the Channel index's and the
  future-work index's counts, the provenance table having no row for review 18, the disposition
  index's cycle count, and the plan's §7.8 and the repository README reading "seventeen". The
  correction that retains this file clears them. **None of the eight reads the review policy's status
  block**, which is BM7's first surface.

I did not run `verify-interchange.ps1`. `git diff --name-only 366bcd0 f4921fc` lists no file other than
Markdown, JSON and PowerShell gate scripts, so no compiled file changed since review 17 ran it green.

### P2 — a two-endpoint model of the session machine with the interaction machine's refusal rows (positive: **BM1**, **BM2**; negative: BL1, BL2)

`C:/temp/cr18rv-scratch/session_pair_model.py` implements the rules and the checker:

- It transcribes the session machine's legal rows 71-74, refused rows 89-91, drain items 1-6 and
  totality rule, and the interaction machine's rows 90, 91 and 97. Each step logs the row that decided
  it.
- Its transport rejects any schedule that breaks intra-interaction frame order or session-control
  order.

The checker was shown non-vacuous: P2-e (a close delivered ahead of its sender's Outcome) is refused
with "schedule violates session-control order".

| Run | Result |
| --- | --- |
| P2-a | Crossing drains converge (both endpoints `closed`, `i1` succeeded) |
| P2-b | Session-machine routing gives B `faulted` with `i1` `lost`; grid routing gives B `draining` with `i1` still executing |
| P2-c | A is `faulted` with `i2` `lost(unknown)` and a peer fault accusing B; B is `closed` |
| P2-c′ | Both `closed`, `i2` succeeded |
| P2-d | A is `faulted` with `i3` `lost(unknown)` and a peer fault accusing B; B is `closed` |

### P3 — an unstateable conforming vector in the repository's own evaluator (positive: **BM3**)

As described in BM3, in the throwaway clone with `-GeneratedCount 0 -CensusPairs 1`, and then
restored. With the initial state stated `draining`, `S2` and `C2-P1` are red, the consistency check
fails, and the three censuses report the disagreement. With it stated `established`, the consistency
check fails at the recipient.

### P4 — the D1 mutation after BL1 (a disclosed limit, confirmed rather than raised)

The vector records an endpoint entering `draining` on the peer's drain and then accepting a **second**
drain control from the same peer as a `draining>draining` transition. Line 73 says that must fault. I
declared it a named mutation for `S1` and `C2-P1`, and both are **green** ("must be red"). Before
`717375d` the edge list had no `draining>draining`, so `S1` would have been red. The completeness
review's BL disposition, lines 1009-1013, states that the gate cannot tell a crossing drain from a
second drain until event tokens are routed. I record the limit as confirmed. It is not a finding,
because it is stated.

### P5 — independent enumerations (negative)

- **Grid:** 3 tables of 6 × 6, **108 cells, 0 empty**.
- **Matrix:** **40 concerns**, each with exactly one owner, 22 identifiers, none unused and none
  outside the vocabulary.
- **Ledger:** `CH-01`-`CH-24`, all 24 dispositioned `retained` or `replaced`.

### P6 — registry pins and targets (negative)

All twelve SHA-256 pins recompute and match, and the status and targets are as stated under
**Architecture selected by the status registry**.

### P7 — propagation reads (positive: **BM4**, **BM5**, **BM6**, **BM7**)

For every claim a BL correction makes about an artifact, I opened that artifact:

- the grid, the terminal-provenance table and the ledger's line 128 for BL12;
- the ledger's lines 30 and 191 and the risk ledger's `CH-K5` for BL2;
- the refused row at line 90 against BL3's required-green vector for BL2's line 113 claim; and
- the policy's status block, step 4 and the Channel index for BL6 and BL7.

Each disposition-index "Under BL1-BL12" line was compared with the artifact it describes.

### P8 — gate self-checks in the throwaway clone

`build/verify-gate-self-checks.ps1` ran in `C:/temp/cr18rv-self` only, never in the review clone, at
`f4921fc` with a clean tree. It ran from 11:07 to about 12:55 and exited 0:

- the probe corpus at **171 of 171** over 16 workers, including `BL1-a` through `BL12-a`, `BL8-b`
  and the self-retiring limit probe `BL11-a`;
- the coverage measure over 4 gates, with **49** declared condition exemptions and **5** operand
  exemptions; and
- the deep run at **52,000 evaluations over 2,000 generated vectors, 0 red**, with the dropped-field
  sweep over 500 of them at 8,541 and 459, and 8 of them load-bearing.

The throwaway clone was clean afterwards. This is a negative result, and it is the expected one:

- none of BM1-BM3 is visible to an instrument whose vectors carry one initial state per session and
  no transport or second interaction state;
- `S1` reads edges only, which is P4's limit.

## Observations not raised

- **Session-control order can be met by discarding.** The BL2 ruling rejected a manifest because it
  "waits without bound when one of those frames is lost". C4's stated realization, "holding the control
  until those frames are delivered", needs delivery knowledge a lossy transport does not give. A
  realization that instead discards an earlier frame arriving after the control satisfies the promise's
  letter, since a frame that is never delivered is not delivered after it. That reproduces BL2's trace 1
  as loss. The design accepts loss-driven peer faults elsewhere (`C4-P2`'s lost-request member), so I
  do not raise it. The owner may still want the realization strategy stated.
- **The generator's initiator history.** The generator still writes the receiving endpoint's
  `accept-establishment` token into the initiator's history
  (`build/verify-channel-0.2-properties.ps1`, line 3832). Event tokens are inert under BH3, so it moves
  no verdict.

## What this verdict means

The batch does not close. **BM1**, **BM2** and **BM3** are to be corrected test/contract-first in a
later commit and receive a fresh independent re-review. That reviewer must be distinct from all
eighteen retained reviewers, every correction author, every condition-4 pass author, and the session
that dispatched this review. This attestation is retained unmodified whatever that review concludes.

Two things this cycle asks the next one to carry.

**The second endpoint needs a transport and an interaction state, not only a session history.** Review
17 asked for the second endpoint and the BL corrections added it to the session timeline. BM1 lives
where one endpoint's interaction state meets the other endpoint's session control. It is invisible to
any instrument whose interactions are recorded from one side only, and no retained vector carries both
endpoints' interaction states.

**Each BL correction closed its trace and not what the trace depended on.** BL1's crossing is closed,
and its principle stops at the local owner (BM2). BL2's reordering is closed, and the recipient's drain
refusal reproduces its consequence without one (BM1). BL3's transitions carry the endpoint, and the
initial state beside them does not (BM3). BL12's route is in the machine, and not in the grid that
enumerates routes (BM4). A correction that pins its finding by the finding's own trace passes the pin
and still leaves these open. The next pass should ask of each correction what it depends on, as the
policy's U-through-AC paragraph says, and should not stop at checking that the trace now passes.
