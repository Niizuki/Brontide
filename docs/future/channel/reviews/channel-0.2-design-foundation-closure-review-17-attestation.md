# Channel 0.2 design-foundation closure review 17 attestation

Reviewer identity: `agent:claude-channel-0.2-closure-review-17-2026-09-25-366bcd0`

Date: 2026-09-25

Reviewed commit: `366bcd0f1d9ad34c36a035bba785089a52be5e9b` (`Merge pull request #163 from
Niizuki/claude/lift-channel-closure-hold`), whose design artifacts hash identically to the pinned
target `c9c4ee9132866e43534f6c432d419e2492810ab6` (`docs(channel): lift the closure-review hold by owner
ruling of 2026-09-24`).

Reviewed tree: `506bb8bdfc2bb5146bc7956e30b551916fb679a4` (the pinned commit's tree is
`ffbfcf94032f1caacf349430bf380dfe664008f6`; the two differ only in `docs/future/channel/reviews/README.md`).

Overall verdict: **`does-not-conform`**

Blocking findings: **BL1**, **BL2**, **BL3**.
Nonblocking findings: **BL4**, **BL5**, **BL6**, **BL7**, **BL8**, **BL9**, **BL10**, **BL11**, **BL12**.

`C4-P2`, the property most of this programme's families have been about, is sound at this pin: an
evaluator written for this review from the published prose, deliberately differing from the gate's in
three mechanisms, agrees with every one of the eleven declared verdicts (probe **P4**). No blocking
finding here is against it. All three blocking findings are against the **session** machine, and all
three come from one question no retained record shows being asked: *what happens when the two endpoints
of one session each run that machine legally, at different instants, over a transport that orders only
what C4 says it orders?* The machine itself says the question exists — "Two endpoints may observe the
crossing transitions at different instants" — and nothing downstream of that sentence answers it.
**BL1** and **BL2** are legal races that the machine turns into fatal `state-violation` faults, the
class the 2026-08-13 R1 ruling was made to remove from the interaction machine. **BL3** is `AL1`'s own
evidence sentence re-derived: it bolded the qualifier "**locally**" as the scope `S3` dropped, and the
correction supplied a different one.

## Isolation

Isolation is **complete**, with one disclosure about session context.

- A fresh isolated clone at `C:/temp/cr17`, made by the dispatcher before this session began, detached
  at the reviewed commit. Verified in the clone rather than taken from the dispatch: `git log -1` is
  `366bcd0`, **942 tracked paths**, `git status --porcelain` empty, `origin` is
  `https://github.com/Niizuki/Brontide.git`.
- The author's working repository `C:/Users/jakub/source/repos/Brontide` was **neither read, written
  to, nor executed against** by any command in this session. **Disclosure:** the session harness placed
  that repository's `AGENTS.md` text and a three-line index of the owner's auto-memory titles into this
  session's initial context before any command ran. No memory entry was opened. `AGENTS.md` was read
  from the clone (blob `ed891d6cc4b7eaac0f58c50a5578287ae2cc36c6`), and no author reasoning about the
  design reached this session through either channel.
- Every mutation, probe vector, and heavy self-check ran in a **separate throwaway clone**,
  `C:/temp/cr17-scratch/probe`, cloned from the review clone and detached at `366bcd0`, restored with
  `git checkout -- .` after each probe. Evaluators and models live in `C:/temp/cr17-scratch`. The
  review clone was never edited: it was verified clean immediately before this file was written, and
  after it contains exactly one untracked path, this attestation. The repository gate's build outputs
  are ignored paths and do not appear in `git status`.
- The reviewer identity differs from all sixteen retained reviewers, every correction author, every
  retained iteration-review actor, and the dispatching session.
- No author private reasoning was available and none was supplied.
- The design was **not** repaired. Nothing was committed or pushed; retaining and committing this file is the next actor's work.

## Disclosed dispatch provenance

The dispatching brief disclosed that **the session dispatching this review authored part of the work
under review**: it ran the twenty-fifth condition-4 pass (the `BK` iteration review), wrote the
design-gate change in that pass, and recorded the owner ruling that lifted the closure-review hold. Git
agrees and adds one inference, labelled as such: `c9c4ee9` (the lift, which is the pinned target) and
`c1005099` (the pin move) carry the same `Co-Authored-By` trailer as the twenty-fifth pass's commits, so
both are treated here as the dispatcher's work. The brief told this reviewer to treat that as a reason to
probe that work harder. It was, and **two findings sit inside the dispatcher's own work**: **BL6** is in
the lift commit that is the pinned target, and **BL11** is in the generator shape the twenty-fifth pass
extended and the twenty-fourth introduced. Neither is blocking. None of the three blocking findings is
in the dispatcher's work; all three are in design artifacts that pass did not edit.

**What the brief narrowed, itemised so the next cycle can discount it.** It named no artifact defect and
no area of weakness. It gave six instructions: take scope from the repository; verify the pin; falsify
rather than read; re-derive retained findings from their evidence sentences; follow propagation; review
the architecture the registry selects. Each is a standing policy requirement, and each moved effort:

1. *Verify the pin* — done first (see **Pin**). Negative result: the clause holds.
2. *Falsify rather than read* — the largest share of effort. It produced **P4** (independent `C4-P2`
   evaluator, positive for the design), **BL3**'s executable reproduction, **BL8** (by mutating the
   owned-fact gate), and **BL10** (by writing the `C4-P2` evaluator with a broader latch selector).
3. *Re-derive retained findings from their own evidence* — this is where **BL3** came from: `AL1`'s
   evidence bolds "**locally**", and the correction carries "session". It also produced **BL4** (`B2`'s
   other half) and **BL7** (`S3`'s and `AI9`'s evidence surface). The credit for BL3's starting point
   belongs to the instruction.
4. *Follow propagation* — produced **BL5** (following the R1 ruling's own rejection rationale onto the
   branch of Option A it did not examine), **BL6**, **BL9**, and **BL11**.
5. *Review the registry's architecture* — negative result (**P11**).
6. The disclosure moved effort onto the lift commit and the `BJ`/`BK` generator, which is where BL6 and
   BL11 were found.

**BL1** and **BL2** came from no instruction: they came from building a two-endpoint model of the
session machine (**P2**), which the brief did not ask for and which no retained record shows. **BL12**
came from asking where the initiator's mirror of the recipient's `unseen` row is.

Effort went heavily to the session machine, C2, C4, C8, C12, the neutral brief, and the verification
gates. Less went to C5, C9 and C11, which were read and not probed.

## Pin

The policy's pin clause was checked against the repository rather than its own wording (`X6`, `AI8`).

| Clause states | Repository |
| --- | --- |
| target commit titled `docs(channel): lift the closure-review hold by owner ruling of 2026-09-24` | `c9c4ee9` carries exactly that subject; it is the only commit in history that does |
| committed 2026-09-24 | `git log -1 --format=%ci c9c4ee9` gives `2026-09-24 20:19:24 +0200` |
| "or any later commit whose design artifacts hash identically" | all fourteen files in `docs/future/channel/` have identical blob hashes at `c9c4ee9` and `366bcd0`; `git diff --stat c9c4ee9 366bcd0` touches only `reviews/README.md` |
| the move off the twenty-third pass's commit, because lifting the hold changed the redesign plan's status line | `git diff 8b6a9cd c9c4ee9` changes that status line and no other design-artifact line |
| preceding pins `3892c23…` and `3b27e3a…` nonconforming | both present as the seventh and eighth review targets |

The clause is correct as to target, date, and hash identity. **No finding on the pin itself.** Its
explanatory sentence — that the redesign plan's status line was the design artifact that "named the
hold" — is part of **BL6**: eight other design artifacts name it too and were not changed.

## Blocking findings

### BL1 — two conforming endpoints that each begin drain fault the session, and every in-flight interaction becomes `lost`

**Artifact/section evidence.**

- `Brontide-Channel-0.2-Session-State-Machine-0.1.md`, `## Events`, line 55: `begin-drain` is initiated
  by "either endpoint or local owner" and transmits a drain control.
- Same file, `## Legal transition table`, lines 71-72: `established` + "local or peer drain begins" →
  `draining`; `draining` + "duplicate local or peer drain control" → `faulted`, "session-scoped
  `state-violation`".
- Same file, `## Drain protocol`, items 1-2, lines 98-100: "the first accepted local or peer drain moves
  the local session to `draining`; a subsequent local or peer drain control is a session-scoped
  `state-violation` and moves the session to `faulted`".
- Same file, line 77: "Two endpoints may observe the crossing transitions at different instants."
- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, C2, line 90: "Drain refuses new interactions while
  allowing already admitted ones to reach a terminal fact"; line 101: "a second local or peer drain
  moves `draining` to `faulted`".
- `Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`, `## Concurrent interactions`, line 138: "A
  fatal session or transport loss maps every nonterminal local initiator and recipient interaction to
  `lost`."
- `Brontide-Channel-0.2-State-Event-Coverage-0.1.md`, line 48: `draining` / Drain: "duplicate drain →
  `faulted`"; and lines 82-83, the principle the R1 ruling installed: the initiator "cannot observe when
  the recipient reaches `executing`, so faulting it would condemn a conformant endpoint for losing an
  unobservable race".

**The failure (probe P2).** Endpoints A and B hold session `s1` at `established` with `i1` in flight. A
begins drain (legal: A is `established`) and sends a drain control. Before it arrives, B begins drain
(legal: B is still `established` and cannot know A's control is in flight) and sends its own. Each
endpoint then receives the other's drain while `draining`: under the legal row and drain-protocol item
2 — and under the session totality rule on the narrower reading of "duplicate" — each faults the session
with a session-scoped `state-violation`, commits at most one peer fault asserting that the other endpoint
violated the machine, and maps `i1` to `lost` with `unknown` certainty. Run a moment apart — B deciding
after A's drain arrives — the same two endpoints drain in order, `i1` completes, and both close.

**Why this is blocking.** Three consequences, each of which the programme has already ruled on:

- C2's own promise fails on conforming behaviour: admitted interactions do not "reach a terminal fact",
  they are destroyed, by the drain that exists to let them finish.
- Both endpoints record that the peer erred when neither did. That is the C9 argument the closure
  re-review made for **R1** — "Both endpoints therefore record that the peer erred" — and R1 was
  blocking.
- Two contradictory outcomes for identical conforming behaviour, selected by a timing neither endpoint
  observes. That is R1's falsifying trace almost word for word ("two contradictory terminal histories,
  selected by recipient-internal timing"), and the grid now states the principle R1 established.

If the owner rules that crossing drains are meant to fault, the design must say so as a deliberate
limit, qualify C2's drain promise, and stop calling a legal peer action a `state-violation`; either way
the current text cannot stand as written.

**Why it survived.** `D1` made a duplicate drain fatal, and its falsifying trace was a *repeated peer*
drain ("accept one peer drain, then receive the same peer drain again"). The generalisation to "local
or peer" caught the crossing case as a side effect. Every retained evaluator, and the repository's
executable session timeline, models one history per session with no endpoint field, so a race between
two endpoints' histories is not expressible in any instrument this programme has built.

### BL2 — session controls are unordered relative to interaction frames, so a legal close or drain overtaking the same endpoint's earlier interaction frame produces a fault

**Artifact/section evidence.**

- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, C4, lines 160-166: "Within one session, for one
  interaction identity, frames sent by one endpoint are delivered in the order that endpoint committed
  them. This is the whole of the ordering Channel 0.2 core promises", and "a realization over an
  unordered transport satisfies it by sequencing those frames"; C4 `**Silence.**`, line 367-370.
- `Brontide-Channel-0.2-Session-State-Machine-0.1.md`, `## Drain protocol`, item 5, line 103: "close is
  legal only when the **local** in-flight set is empty"; item 6, line 104: "a peer close with locally
  nonterminal interactions is a protocol fault"; `## Refused and illegal inputs`, line 90: `draining` +
  close while interactions remain nonterminal → "fatal session `state-violation`"; line 89: `draining` +
  new peer interaction → interaction-scoped `state-violation`.
- `Brontide-Channel-0.2-Responsibility-Matrix-0.1.md`, line 93: cross-interaction ordering is owned by
  `delivery-facet`, and no row owns the order of a session control relative to interaction frames.
- C2, line 90, as in BL1.

**The failure (probe P2), two traces, both conforming at both endpoints.**

1. *Close overtakes an Outcome.* A drains; B receives the drain; B's handler completes and B commits
   `i1`'s Outcome, making B's local in-flight set empty; B sends close (legal under item 5) and enters
   `closed`. Nothing orders B's close after B's Outcome — close carries no interaction identity — so a
   conforming unordered transport may deliver close first. A receives close with `i1` nonterminal: fatal
   session `state-violation`, `i1` → `lost` with `unknown`. B's Outcome then arrives at a faulted session
   and is discarded as late traffic. B recorded success; A recorded a fault accusing B and a loss.
2. *Drain overtakes a request committed before it.* A, `established`, admits and dispatches `i2`, then
   begins drain; `i2` is in A's drain snapshot. The drain control overtakes the request. B enters
   `draining`, then receives the request as a "new peer interaction" and commits an interaction-scoped
   `state-violation`. A's `i2`, which A admitted before draining and expects to finish, ends in
   `peer-fault` accusing A of a violation A did not commit.

**Why this is blocking.** Same three consequences as BL1: C2's drain promise fails, a peer is accused of
a violation it did not commit, and in-order and reordered delivery of the same committed frames produce
contradictory histories. Trace 1 also loses a committed semantic Outcome under a fatal fault, which is a
worse result than R1's. The defect is in the seam between C4's deliberately narrow ordering promise and
the session machine's close and drain rules, which silently assume the order C4 declines to promise.

**Why it survived.** The S1 ruling scoped the ordering promise to one interaction for good reasons and
checked what that scope meant for interaction frames; no record shows it checked what it meant for
session controls. Every executable vector and generated population carries one session timeline with
no transport model.

### BL3 — the session-state properties are scoped to the session, but the session machine runs once per local endpoint; `S2`, `S3`, `S4` and `C2-P1` are red in the repository's own evaluator on a conforming one-session vector, and `AL1`'s correction supplied the session qualifier rather than the "locally" qualifier its evidence named

**Artifact/section evidence.**

- `Brontide-Channel-0.2-Session-State-Machine-0.1.md`, line 77-79: "Two endpoints may observe the
  crossing transitions at different instants. Conformance compares each endpoint's legal local history
  … it does not require a global simultaneous state"; line 98: drain "moves the **local** session";
  item 3, line 101: "no new interaction may be admitted **locally** after the first drain transition".
- `Brontide-Channel-0.2-State-Event-Coverage-0.1.md`, line 25: the totality rule applies "For each local
  endpoint".
- The same session-machine file, `## Capability-wide properties`, lines 152-163: "Each of these is a
  statement about **one session**"; `S3`: "Within each session the vector carries, no new interaction is
  admitted after that session's first drain transition"; `S2`: "outside its own session's `established`
  state"; `S4`: "a terminal session never becomes nonterminal".
- `channel-0.2-design-foundation-closure-review-16-attestation.md`, `AL1` evidence, lines 110-112: drain
  item 3 "states the underlying rule **with** the scope the property drops: no new interaction may be
  admitted **locally** after the first drain transition".
- `Brontide-Channel-0.2-Capability-Contract-0.1.md`, C12, lines 700-702: "`session state` — each session
  the vector carries holds its own state and its own transitions through the session state machine,
  including … its **first drain transition**".
- `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md`, `## Vector format`, line 207: one "initial
  session/interaction state of **each session** the vector carries"; line 220: "endpoint perspective and
  role"; stimulus steps name a committing endpoint (line 222) and session transitions name none.
- C4, lines 195-196: a C4 vector's expected observations are "the complete set of records **both
  endpoints** produce under the vector"; the completeness review's residual risk 5, line 341: "process
  vectors must force both perspectives"; and its C8 finding, line 99: "Endpoint perspectives may differ
  after transport loss", which is the second vector below.
- `Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md`, lines 774-776, the AL disposition: every
  session-machine property is checked structurally "on the ground that the machine's properties are
  statements about one session by construction"; and `reviews/channel-0.2-disposition-index.md`, lines
  176-182, recording that exactly this "by construction" argument was the defect `AL1` found.

**The failure (probe P3, executed with the repository's own evaluator).** In the throwaway clone, one
vector was added to `conformance/channel-0.2-property-vectors.json`, declared additional-green for every
property its template carries, and `build/verify-channel-0.2-properties.ps1 -GeneratedCount 0` was run
unchanged. One session `s1`, both endpoints recorded, initial state `established`, every step a row of
that endpoint's own legal history:

| step | endpoint | event | endpoint's own authority |
| --- | --- | --- | --- |
| 1 | B | begin-drain, `established` → `draining` | legal table |
| 2 | A | admits `i1` (B's drain not yet received) | initiator `candidate` under an established, non-draining session |
| 3 | A | dispatches `i1` | initiator `admitting` → `dispatched` |
| 4 | A | receive-drain, `established` → `draining` | legal table |
| 5 | A | accepts B's interaction-scoped `state-violation` as `i1`'s terminal | refused/illegal `draining` + new peer interaction, at B |
| 6 | B | close, `draining` → `closed` | local in-flight empty |
| 7 | A | receive-close, `draining` → `closed` | local in-flight empty |

The gate reports, verbatim: `S2` red — "interaction i1 dispatched while its own session s1 was
draining"; `S3` red — "session s1 admitted interaction i1 after its own first drain transition"; `S4` red
— "session s1 reached terminal state closed and then transitioned to closed under the same session
identity"; `C2-P1` red through its third clause. A second vector — A faults on transport loss from
`established` while B, not yet aware, begins drain — takes `S4` and `C2-P1` red ("reached terminal state
faulted and then transitioned to draining"). The step table carries an endpoint column that the vector
could not: the executable session timeline has no endpoint field, and neither does the brief's vector
format for a session transition. The repository's own generator writes the initiating endpoint's
`send-establish-proposal` and the receiving endpoint's `accept-establishment` into one session history
(`build/verify-channel-0.2-properties.ps1`, lines 3803-3804), so the executable form already treats two
endpoints' histories as one.

Under the most generous reading — that a vector states no order across endpoints, as the brief says of
stimulus steps — the verdicts are not red but undefined: whether A's admission is "after that session's
first drain transition", or whether `s1` "is terminal" while A is `faulted` and B is `established`, has
no answer. That violates `C12-P1`'s one deterministic expected observation instead. With the qualifier
the drain protocol already carries — each endpoint's own local session — the verdicts flip: the gate's
`S2`, `S3` and `S4` logic re-implemented with its grouping key changed from session to (session,
endpoint), and nothing else changed, is green on both vectors, and `C2-P1`'s clauses follow.

**Why this is blocking.** It is `AL1`'s mechanism one level finer, and `AL1` was blocking: a property
of the session machine read over a scope wider than the machine it describes, red on conforming
behaviour, under `AE3`'s normative rule. It is also `AL1` closed as to its trace and not as to its own
evidence sentence, which is the "closed in the first artifact, open in the second" class the policy's
method notes describe — here closed in the first qualifier and open in the second. The refused/illegal
table's nonfatal "new peer interaction" row during drain exists precisely because a peer admits after
this endpoint's drain; read per session, `S3` forbids the scenario that row was written for.

**Why it survived.** The AL correction's structural check requires every session-machine property to
name *a session*, and it passes. C12's declaration says session state belongs to one session. Every
required-green member for `S1`-`S6` is a single-history timeline. And the AL disposition gives as its
ground the very argument `AL1` rejected — "about one session by construction" — one level up: the
machine is about one **endpoint's** session history by construction, which is what item 3's "locally"
said.

## Nonblocking findings

### BL4 — the recipient never emits the `accepted` cancellation acknowledgement the initiator's machine consumes

`Brontide-Channel-0.2-Interaction-State-Machine-0.1.md`, `## Recipient transitions`, line 100:
`executing` + valid cancellation control → `cancel-requested`, "possible/already occurred" — no emission;
line 101, the denied case, does say "emit nonterminal `refused` acknowledgement"; line 95, the held
control applied after admission, states no emission for either outcome. The initiator consumes an
`accepted` acknowledgement at line 73 (`cancel-pending` → `cancel-accepted`), C8 names
`C8-cancel-accepted-still-awaits-outcome` as a scenario (contract line 516), and C8 says a held control
is "evaluated under local cancellation authority, reaching the same accepted or refused acknowledgement
it would have reached" (lines 478-480). The grid's recipient `executing` cancellation cell (line 69)
reads "authorized → `cancel-requested`" with no frame decision, although every cell must assert one
(line 174-175). `B2` was this defect for `refused` — "no producer path for the promised `refused`
acknowledgement" — and was blocking; its correction added the `refused` emission and left `accepted`.

Rated nonblocking because C8's held-control sentence states the emission in the contract, so a
determinate answer exists by reading upward, where `B2` had none. **This is this review's closest
escalation call**: if the owner reads the machine as the detailed authority on frame decisions, as the
grid says it is, the answer is not determinate and BL4 is `B2`'s blocking class.

### BL5 — a recipient-side frameless refusal leaves the initiator with no terminal fact in a core that has no timer

Recipient authority denial, recipient phase refusal, and a drain that refuses a still-admitting
interaction each go to `refused-local` with no frame (interaction machine lines 90, 91, 97; C3 lines
135-136 "at either endpoint, including the recipient's"; C6 line 414). The initiator is `dispatched` and
receives nothing; its only exit is a local loss observation (`lost`, `unknown`), and "There is no idle
timeout or keepalive in core" (session machine lines 181-182). Until an out-of-core timer fires the
interaction holds its in-flight slot — with the first profile free to select a bound of one, the whole
session — and orderly close is impossible because close requires an empty local in-flight set. When a
held cancellation control is discarded on that branch, the initiator sits in `cancel-pending` never
learning anything, which is the consequence for which the R1 ruling **rejected** Option B ("the initiator
sits in `cancel-pending` never learning its cancellation did nothing, and a silent drop is the
contract-silence class this programme keeps finding defects in", redesign plan lines 499-502) —
reintroduced on the `refused-local` branch of the Option A it selected. The predecessor's frameless
denial (Design Note 0.1, lines 139-141 and 155) happened before the request crossed the wire; 0.2's
recipient-side one happens after it. The completeness review's silence-probe table does not list the
case. Rated nonblocking because the design is internally consistent — the initiator does eventually
record `lost` with honest uncertainty wherever a host timer exists — but the cost is unstated and
unowned.

### BL6 — lifting the hold reached some of the surfaces that state it (the pinned commit)

Eight of the nine design artifacts' status blocks still read "awaiting a fresh independent closure
re-review, **on hold under the owner decision of 2026-08-17**": the capability contract, both state
machines, the grid, the responsibility matrix, the completeness review, the migration ledger, and the
brief (line 6 of each). Only the redesign plan's was changed, and the review policy's pin clause
explains the pin move by saying the redesign plan's status line was the design artifact that "named the
hold". The Channel index (`docs/future/channel/README.md`, lines 41-48) had its lead sentence changed and
kept, in the same paragraph, "Three of its four conditions are met; the fourth asks for an author-side
pass … the thirteenth is the only one to find nothing, and the twelve before it and the nine after it
raised …", with the family list ending at `BI1`-`BI3`: all four conditions are met, the twenty-fourth
and twenty-fifth also found nothing (the review policy's own count is four), twelve passes follow the
thirteenth, and "nine" has been wrong since the twenty-third. This is `T4`'s class and the tenth
consecutive cycle in which an edit reached some of a fact's surfaces; the design gate's status check
forbids only the escalating cycle adjectives and requires one stable phrase, so "on hold" is invisible
to it.

### BL7 — the redesign plan's §7.8 and the repository README misstate the retained review count; `S3`'s and `AI9`'s evidence surface is stale a third time

`Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md` §7.8, lines 296-298: "Fifteen independent
attestations are retained — fourteen `does-not-conform` and one `conforms-with-nonblocking-findings`".
Sixteen are retained and fifteen are negative. The sentence entered with the AK correction
(`1cbdcfa`, 2026-08-17), review 16 was retained the same day (`adea3d7`), and the AL correction did not
update it. `S3`'s evidence quoted this passage saying "Five" when six were retained; `AI9` found it
saying "seven" six cycles later; the design gate's `AI9` check (`build/verify-channel-0.2-design.ps1`,
line 2750) matches the literal `Seven independent negative attestations`, which is the
defect-recognised-by-its-own-words shape `AL1` and `AL2` warned against. The repository `README.md`,
line 74, says "five independent reviews are retained", unchanged since `2b997fe` (2026-08-11); the gate
checks that file for stale phrases and not for counts.

### BL8 — the AL2 correction's record sweep does not fire on the pre-AK1 publication of the very record AK1 was raised against

`conformance/channel-0.2-facts.json`, `unseen-refusal-record.unfencedSweep`: trigger
`` `unopened-interaction-identity` `` with co-terms `provenance` **and** `effect certainty`. The review
policy describes this sweep as "keyed to **the record instead of the reference**". Probe **P7**: C10's
own pre-AK1 sentence (`git show 1cbdcfa^`, contract lines 549-551 — "The observation records the
refusal, **the kind of frame refused**, and its provenance with the detailed reason
`unopened-interaction-identity`") pasted unfenced into the brief passes both
`verify-channel-0.2-facts.ps1` and `verify-channel-0.2-design.ps1`; so does a fresh present-tense
sentence listing provenance, detailed reason, and frame kind. The pre-AK1 record never carried effect
certainty — C4, lines 239-240: "the record carried its provenance, its detailed reason, and the kind of
frame refused and nothing else" — so the sweep's co-term is a field the AL2 instance happened to carry,
which is a class inferred from its own member: `AL3`'s shape inside the `AL2` correction. The same
sweep is silent on the trigger written without backticks. Rated nonblocking because every current
publication is fenced and rendered.

### BL9 — the operand enumeration attributes to `C4-P1` a read of `session state` that neither named clause makes, because the design gate requires the row

`Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md`, line 267: "the `session state` an
interaction is admitted and dispatched in | `C4-P1` clauses 1 and 2"; line 262: "that fact is read by
`C4-P1`". Clause 1 is "each accepted terminal fact closes exactly one admitted interaction" and clause 2
"no interaction identity is dispatched twice"; neither reads a session state. The gate
(`build/verify-channel-0.2-design.ps1`, line 2994) fails unless every fact C12 declares has a row, with
the message "which C12 declares and `C4-P1` or `C4-P2` reads" — an assumption `AL3`'s addition made
false. The properties that do read session state (`S2`, `S3`, `S4`, `C2-P1`'s middle clause) have no
enumeration, which is where BL3 sat.

### BL10 — `C4-P2`'s executable declaration selects `latchValue = fault-committed`, a qualifier the contract's statement does not carry

`conformance/channel-0.2-properties.json`, `C4-P2-conjunct-2.selecting`, and
`build/verify-channel-0.2-properties.ps1` line 433. The contract states conjunct 2 as "none records a
late-traffic `state-violation` latched against a frame …" and says "The second witness is the settling
frame and **not the latch value**" (C4, line 214). A latch settles against a frame at either
`fault-committed` or `fault-unavailable` (interaction machine lines 214-218). Probe **P5**:
`C4-outcome-precedes-ack` with the initiator unable to commit its late fault — latch `fault-unavailable`,
same settling and terminal frames — is **green** in the gate and red in this review's evaluator. The
reading the gate takes is defensible (`fault-unavailable` records "only a local loss/late-traffic
observation"), and if it is the intended one the conjunct has a coverage limit — a reordering hidden
behind a failed fault commit is not witnessed — which, unlike conjunct 1's `AH6` limit, no artifact
states. No declared input carries `fault-unavailable`, so nothing distinguishes the two readings today.

### BL11 — the generated "conforming" population keeps an interaction nonterminal after a rejected terminal fact, which the interaction machine routes to `lost`, and no property can tell the two apart

`build/verify-channel-0.2-properties.ps1`, lines 3839-3876 (the `BJ` shape and its `BK` extension):
"an interaction whose claimed fact was rejected stays nonterminal, awaiting a valid one … the
interaction is as nonterminal after two rejected claims as after one"; guard probe `BJ-c` calls this "a
refused terminal fact the design permits". Neither cites an artifact, and the design says otherwise: the
initiator row at `dispatched`/`cancel-*` routes "unusable terminal frame" to `lost` with `unknown`
(interaction machine line 79); C4 says rejecting a malformed terminal fact after dispatch "leaves effect
certainty `unknown`, not zero" (line 174-175); and the completeness review's silence probe answers
"malformed/mismatched terminal after dispatch | terminal not accepted; local loss/fault with unknown
effects" (line 159). A later valid terminal for that interaction is late traffic, not an accepted
terminal. The population the twenty-fourth and twenty-fifth passes measured at "0 red" therefore
carries, in one dispatched interaction in four, a behaviour the machine forbids, and every property
stays green on it. The grid's evidence clause requires the opposite of the property set: "Mutating any
detailed row … to 'ignore' … must fail at least one property" (grid lines 177-178), and this is the
`unusable terminal frame → lost` row mutated to ignore. The "missing" shape (a claim naming no
identity) also cannot be attributed to the interaction it is filed under. Rated nonblocking because it
is in the verification rather than the design, and the declared corpus is unaffected.

### BL12 — the initiator has no route, observation, or retention rule for a frame naming an identity it never opened

The recipient's case has a dedicated row (`unseen`, interaction machine line 88), a detailed reason, a
C10 observation, and a retention rule argued from the R1 ruling. The initiator's has none: its states
begin at `candidate`, the grid's initiator rows begin at `candidate`/`admitting`, C10's "a recognized
frame that opens no interaction yields one too" names the recipient only (contract lines 581-586), grid
totality rule 2 needs a nonterminal state and rule 6 covers unknown or structurally invalid input rather
than a well-formed frame with a well-formed unopened identity. C4's "Missing, extra, wrong-session, or
mismatched identities reject the claimed terminal fact" (line 173) says reject and not with what fault,
observation, or retention. Two independent stacks can ignore the frame, commit an interaction-scoped
fault, or fault the session, and one that retains a terminal record for the identity has the unbounded
state R1 refused. Rated nonblocking because no dispatch or effect claim is at stake, only the frame and
observation decision the Batch 2 entry gate says must be covered.

## Capability verdicts

| Item | Verdict | Rationale |
| --- | --- | --- |
| C1 | conforms | `C1-P1` is per session; establishment, downgrade refusal and fixed/negotiated equivalence agree with the session machine and the brief. The event-token routing BH3 left open moves no outcome in the cases checked (P9). |
| C2 | **does-not-conform** | **BL1** and **BL2**: the drain promise fails on conforming behaviour and legal peers are faulted. **BL3**: `C2-P1` is red through its third clause on a conforming two-endpoint vector in the repository's own evaluator. |
| C3 | conforms | Class, direction and phase are exact admission inputs; `C3-P1` is per session. BL5 records the initiator-side cost of the recipient's frameless phase refusal. |
| C4 | conforms-with-nonblocking-findings | `C4-P2` agrees with all eleven declared verdicts under an independent evaluator (P4); `C4-P1` is correctly session-scoped. **BL10** is against its executable declaration and **BL12** against its failure clause. BL2's defect lives where C4's narrow ordering promise meets C2's close rule. |
| C5 | conforms | Read, not probed. Positional, pre-effect, bounded. |
| C6 | conforms-with-nonblocking-findings | Authority local and exact; **BL5**: a recipient-side denial strands the initiator. |
| C7 | conforms | Decision 13's exact declaration, pre-Ready window, separate authority, and no Ready/Release creation are all carried (P10). BL5 means a recipient phase refusal reaches CM4 as `lost`/`unknown` rather than as a refusal. |
| C8 | conforms-with-nonblocking-findings | One terminal history, explicit cancellation; **BL4** (the `accepted` acknowledgement has no producer) and BL5's discarded held control. |
| C9 | conforms | Read, not probed as a property. Its provenance separation is what BL1 and BL2 violate in effect, through C2, by producing peer faults for conforming behaviour. |
| C10 | conforms-with-nonblocking-findings | Observation content and certainty rules hold; **BL12** leaves the initiator's unopened-identity refusal with no observation rule. |
| C11 | conforms | Read, not probed. |
| C12 | **does-not-conform** | **BL3**: three session-machine properties and `C2-P1` fail against a conforming realization in the executable form, or are undecidable under the generous reading, and C12's own `session state` declaration carries the conflation. **BL9**: the gate's check over that declaration forced a false row. |

## Area verdicts

| Area | Verdict |
| --- | --- |
| Session state | **does-not-conform** — **BL1**, **BL2**, **BL3**. The six states, the legal and refused tables and the totality rule are otherwise sound and total over one endpoint. |
| Interaction state | conforms-with-nonblocking-findings — **BL4**, **BL5**, **BL12**. `I1`-`I7` read per session and are sound on everything probed. |
| State/event totality | conforms-with-nonblocking-findings — 108 published cells, 180 underlying pairs, no empty cell (P8); **BL12** is a recognized event with no cell. |
| Responsibility | conforms — 39 concerns, each with exactly one owner from the closed 22-identifier vocabulary, every identifier used (P8). BL2 notes that no row owns the order of a session control relative to interaction frames. |
| Completeness | conforms-with-nonblocking-findings — **BL9**; and BL1, BL2 and BL5 are silences its probe table does not list. |
| Migration coverage | conforms — all 24 predecessor vectors dispositioned by identifier (P8), `CH-R10` explicit, the register carried. |
| Neutral brief | **does-not-conform** — **BL3**'s vector-format half: one initial session state per session and no endpoint on a session transition cannot express two endpoints' histories, which C4's own expected observations require a vector to carry. |

## Owner rulings

The four first-batch rulings recorded 2026-08-11 are represented consistently: core concurrency and
cancellation (C4 `max-in-flight`, C8, the interaction machine, matrix rows `Bounded unary concurrency`
and `Class-specific cancellability` → `channel-profile`, the ledger's limit and feature tables);
session-state ownership (six Channel states; Portable Binding owns Interconnection, Release, withdrawal
and cleanup, Composition the Relational Initialisation phase, Component Management Ready — matrix rows
and `## Selected boundary rulings` agree); relational initialization representation (C3, C7, the
interaction machine, matrix `cm3-lifecycle-contract`); extension invariants (C11, `C11-P1`, matrix
`## Extension hooks`, brief). None of BL1-BL12 contradicts any of them. The correction rulings (R1, S1,
AE1 with its AF8 narrowing and AK1 note) and the 2026-08-15 closure-standard ruling are recorded as
issued. One observation, not a finding: the redesign plan says "There are no unresolved owner decisions
in the first-batch design foundation", while the verification foundation plan's open question 5 (BH3)
proposes a change to the session machine's legal table; it is correctly outside the design package's
own question list, and the routing it asks about moved no observable outcome in the cases checked (P9).

## Architecture selected by the status registry

`Brontide-Architecture-Status.json` selects Architecture 0.8, "Complete Draft (document and
implementation evidence complete; not ratified)", at `docs/current/architecture/Brontide-Architecture-0.8.md`;
no architecture is ratified. All twelve SHA-256 pins recompute and match (eleven distinct files; the 0.8
requirements file is pinned twice). Both stacks state "Designed for: Brontide Architecture 0.8, Complete
Draft, not ratified", and neither claims a Channel 0.2 implementation. The contract's cited sections
exist (§6.16, §13.6, §16.4, §18.1, §19, §24). §19's Channel direction says "Delivery, ordering, and retry
are promised by no one" of the 0.1 recorded frame; Channel 0.2's intra-interaction frame order narrows
that, and the migration ledger dispositions it through `CH-R10` and `CH-K5`. No ratified architecture
constrains the narrowing. **Negative result.**

## Retained findings

Every family `B` through `AL`, and the iteration families through `BK`, was checked against its own
evidence sentences in the artifacts those sentences name, individually for `AL1`-`AL4` and by sample for
the rest. Closed, except as follows.

- **`AL1`**: closed as to its two-session trace — reverting `S3` to its pre-correction text fails the
  design gate (P6) — and **not** as to its own evidence sentence, which bolded "locally". That is
  **BL3**.
- **`AL2`**: closed as to the two grid cells — they are rendered from `channel-0.2-facts.json`, and
  truncating one fails the owned-fact gate (P7). The correction's claim that its sweep is keyed to the
  record is **BL8**.
- **`AL3`**: closed — `session state` is declared, and removing it fails the gate against the brief's
  vector format (P6). Its consequences are **BL3** (the declaration's scope) and **BL9** (the row it
  forced).
- **`AL4`**: closed — `S5` names the one declared profile.
- **`S3`** and **`AI9`**: closed as to the instances their evidence quoted; the surface they named is
  stale again, **BL7**.
- **`B2`**: closed for `refused`; the `accepted` half is **BL4**.
- **`R1`**: closed in the interaction machine; its principle is what **BL1** and **BL2** violate in the
  session machine, and its rejection rationale recurs as **BL5**.
- **`T4`**: closed as to cycle adjectives; its class recurs as **BL6**.
- `AE1`, `AF5`/`AH2`, `AF8`, `AI1`, `AJ1`, `AK1`, `AK5`, `AK6`, `AK7`: re-derived by evaluator (P4) — the
  lost request, both conforming deliveries, the two-session vector, and the two-controls vector are
  green and both mutations red.
- `AK3`: 26 properties (13 capability, 6 session, 7 interaction), counted from the statements.
- `BK1`: closed; the gate passes with no exemption added for it.

## Probes performed

### P1 — gates in the review clone

| Gate | Result |
| --- | --- |
| `build/verify-channel-0.2-design.ps1` | exit 0 — "11 required artifacts, C1-C12 with properties/scenarios/silence, total session/interaction event coverage, 6 session states, all 24 predecessor vectors dispositioned, 4 owner rulings resolved, and independent review still pending." |
| `build/verify-channel-0.2-design.ps1 -NegativeProbe` | exit 1 with exactly one failure, "missing '**Property C12-P1.**'" — fails only because `C12-P1` was removed in memory. |
| `build/verify-channel-0.2-properties.ps1` | exit 0 — 26 of 26 executable, 139 evaluations over 61 declared inputs, 9 operand mutations, 2,600 generated evaluations at 0 red, every census at 0 findings. |
| `build/verify-channel-0.2-facts.ps1` | exit 0 — 4 facts, 21 fenced publications across 6 artifacts. |
| `build/verify-channel-0.2-return-channels.ps1`, `build/verify-channel-vectors.ps1` | exit 0. |
| `build/verify-doc-links.ps1` | exit 0 — 1,091 local links, 105 heading fragments, 338 documents. |
| `build/verify-text.ps1` | exit 0 — 936 UTF-8 files. |
| `build/verify-interchange.ps1` | exit 0 — full build and both stack suites, 0 warnings; self-checks skipped by design and run separately (P12). |

These were run before this file existed. Re-run with it in place: `verify-text`, `verify-doc-links`,
the owned-fact gate and the properties gate still pass (937 files; 339 documents). The design gate
fails with exactly six failures, each the bookkeeping a retained attestation owes and none about the
design — the review-file roster (line 1198), the retained-attestations list, the Channel index's and the
future-work index's review counts, the family provenance table having no row for review 17, and the
disposition index's cycle count. The correction that retains this file clears them.

### P2 — a two-endpoint model of the session machine (positive: **BL1**, **BL2**)

`C:/temp/cr17-scratch/race_model.py`, written from the session machine's legal, refused and drain
rules, the interaction machine's fatal-session mapping, and C4's ordering promise. Each endpoint runs its
own history; the transport delivers in a stated order, and a checker refuses any schedule that reorders
two frames one endpoint committed for one interaction identity — so every accepted schedule is one a
conforming realization may produce. The checker was shown non-vacuous by feeding it a genuine
intra-interaction reordering, which it refused. Results: crossing drain — both sessions `faulted`, `i1`
`lost(unknown)` at both; the same endpoints draining in sequence — both `closed`, `i1` succeeded at both;
close overtaking the Outcome — A `faulted` with `i1` `lost(unknown)`, B `closed` with `i1` succeeded;
drain overtaking a request — A's snapshot interaction `i2` ends `peer-fault`.

### P3 — the session properties over two endpoints' histories, in the repository's own evaluator (positive: **BL3**)

As described in BL3, in the throwaway clone: `S2`, `S3`, `S4` and `C2-P1` red on the drain-race vector;
`S4` and `C2-P1` red on the asymmetric-loss vector; every census then reports the declared/actual
disagreement, which is the gate working.

### P4 — an independent `C4-P2` evaluator (positive for the design)

`C:/temp/cr17-scratch/c4p2_eval.py`, from C4's statement and its paragraphs on subject, membership scope
and operands, and the brief's operator set. It differs from the gate on purpose: precedence by each
step's own `commitIndex` rather than declared order; a frame reference must resolve to exactly one step
or the record is unevaluable; conjunct 2 selects every settled latch by its category rather than by which of the two settled values it holds. **11 of 11** declared
verdicts agree — red on both named mutations, green on the seven required-green members and on the
two-session and two-controls vectors.

### P5 — the latch-value mutation (positive: **BL10**)

`C4-outcome-precedes-ack` with `latchValue` set to `fault-unavailable`, declared red, in the throwaway
clone: the gate reports "Property 'C4-P2' is green … and must be red"; P4's evaluator is red.

### P6 — mutation of the AL guards (negative: the guards fire)

Reverting `S3` to "No new interaction is admitted after the first drain transition." fails the design
gate ("Property 'S3' in the session state machine names no session"). Removing `session state` from
C12's list fails it against the brief's vector format ("distributes 'session/interaction state' per
session and C12 declares no matching fact").

### P7 — mutation of the owned-fact gate (one negative, one positive: **BL8**)

Truncating the grid's first `unseen` fence to three reference fields fails the facts gate. Pasting C10's
pre-AK1 sentence, and separately a present-tense three-field statement of the record, unfenced into the
brief passes the facts gate and the design gate.

### P8 — independent enumerations (negative)

Grid: 18 data rows × 6 content columns = **108 cells, 0 empty**; 6 × 6 + 12 × 6 + 12 × 6 = **180 pairs**,
agreeing with reviews 7-16. Matrix: **39 concerns**, one owner each, 22 identifiers, none unused.
Ledger: **24** predecessor vectors, each dispositioned `retained` or `replaced` by identifier.

### P9 — session event routing (negative)

`BH3`'s question tested for an observable consequence: `established` + close reaches `faulted` with a
session-scoped `state-violation` by the grid's "premature close" cell and by the totality rule alike, and
the negotiated path has a consistent reading for both endpoints (the receiver's validation of a proposal
takes the `establishing` row and its matching acceptance the `established` row). No outcome moved.

### P10 — Decision 13 and CM3/CM4 (negative)

`binding/portable/open-decisions.md` Decision 13 (Option B for 0.2): exact edge, direction, initiating
and receiving members, Operation, Capability and input Shape; refusal before delivery; ordinary traffic
closed until Release; composition root may initiate; explicit, exact lifecycle authority; failure
prevents Ready and Release and returns cleanup to CM4. C3, C7, `C7-P1`, `I6` and the interaction
machine's `## Relational initialization` carry every clause.

### P11 — registry pins and targets (negative)

All twelve SHA-256 pins recomputed in the clone; all match. Status and targets as stated in
**Architecture selected by the status registry**.

### P12 — gate self-checks in the throwaway clone

`build/verify-gate-self-checks.ps1` was run in `C:/temp/cr17-scratch/probe` only, never in the review
clone, at `366bcd0` with a clean tree, and exited 0: the probe corpus at **158 of 158** over 16
workers; the coverage measure over 4 gates with **49** declared condition and **5** operand exemptions
(the twenty-fifth pass recorded 47 and 6 before `c9c4ee9`, whose message declares the two added and the
one deleted); and the deep run at **52,000 evaluations over 2,000 generated vectors, 0 red**, with the
dropped-field sweep over 500 of them at 8,541 and 459 and all eight load-bearing droppings reporting.
The throwaway clone was clean afterwards. This is a negative result, and it is expected: none of BL1-BL3
is visible to any instrument whose inputs carry one session history, and BL11 is inside the generated
population this run measures.

## What this verdict means

The batch does not close. **BL1**, **BL2** and **BL3** are corrected test/contract-first in a later
commit and receive a fresh independent re-review from a reviewer identity distinct from all seventeen
retained reviewers, every correction author, every condition-4 pass author, and the session that
dispatched this one. This attestation is retained unmodified whatever that review concludes.

Three things this cycle asks the next one to carry.

**The interaction side is in its strongest state yet.** An evaluator built to disagree with the gate's
mechanisms agrees with every declared `C4-P2` verdict, the frame-reference publications are rendered
from one declaration and the gate catches a truncated one, and the AL guards fire when reverted. The
verification foundation did what the hold was for: it made this review cheap enough to spend its effort
somewhere new.

**The somewhere new was the session machine run twice.** Every instrument in this repository — the
declared corpus, the generator, sixteen reviewers' evaluators — models one session history. The machine
says there are two, at different instants, and three blocking findings live in the difference. A
correction for BL3 that adds the endpoint to the property statements and not to the vector format, or to
the format and not to the generator, will be the eleventh cycle of the class this programme keeps
recording. The next reviewer should build the second endpoint before reading anything else.

**The by-construction argument has now been wrong twice.** `AL1` rejected "about one session by
construction" for `I5` and the correction used it for `S1`-`S6`; the machine is about one endpoint's
history by construction. A property's scope is whatever the machine it restates is a machine *of*, and
that is a question to answer from the machine, not from the property.
