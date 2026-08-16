# Channel 0.2 design-foundation closure review 13 attestation

Reviewer identity: `agent:claude-opus-5-channel-0.2-closure-review-13-2026-08-15-e7bfeba`

Reviewed commit: `e7bfeba6ba58e2e4e9a48a5148e2461c187bf452`

Date: 2026-08-15

Overall verdict: **`does-not-conform`** — one blocking finding (**AI1**) and eight nonblocking
findings (**AI2**-**AI9**).

**AI1 is the U1 condition returning inside the correction written to close AH1.** The AH1 correction
settled a question no artifact had answered — a Channel 0.2 vector **may carry more than one session**
— and gave the declared stimulus step a session so the precedence operator has its operand. It did not
give a session to the *other* operand of the same conjunct. `C4-P2`'s second conjunct reads the
settling-frame reference, which the neutral brief's parity profile and the interaction state machine
both publish as exactly four fields — kind, interaction identity, committing endpoint, arrival ordinal
— and both assert maps to **one** declared stimulus step. Once two sessions in one vector may
legitimately hold the same interaction identity value, which the same commit declares legal, that
assertion is false, and an evaluator written from the published field list takes `C4-P2` **green** on
its own named mutation `C4-outcome-precedes-ack`. Probe **P3**, row `M2-two-session`, demonstrates it.

Every retained finding B1 through AG5 is closed in the artifact it was raised against **with one
exception, recorded as AI9: S3's evidence named the redesign plan's §7.8 by quotation, and that
section still reports "Seven independent negative attestations are retained" and stops at the seventh
review.** **AH1, AH2, AH4, and AH6 are closed. AH3 is closed in one of the three narrative surfaces
its evidence names and open in the other two. AH5 is closed in the `C4` row of the property audit and
open in the `I5` row, which its evidence names alongside it.**

## Isolation

Complete, with the dispatch provenance disclosed in its own section below.

```text
C:/b033  ->  e7bfeba6ba58e2e4e9a48a5148e2461c187bf452  (clean)
git status --porcelain   ->  (empty, 0 lines)
git ls-files | wc -l     ->  890
git diff HEAD            ->  (empty)
```

The clone materialised completely — 890 tracked paths, clean status, empty `git diff HEAD`, no
`Filename too long` failure, the clone target being a short path. Every artifact assessed here was
read from `C:/b033`; all four gates available to this review were run there. **The author's working
repository `C:/Users/jakub/source/repos/Brontide` was not read, written to, or executed against at
any point in this session.**

The reviewer identity above differs from all twelve retained reviewers, from every correction author,
and from every retained iteration-review actor. No author private reasoning was available. `AGENTS.md`
and `docs/future/channel/reviews/README.md` were both read from the clone at the pin and are the
source of this review's scope. The `C4-P2` evaluator used in probe **P3** imports no repository code;
it was written from the published prose of C4, the brief's operator set, the brief's vector format,
the brief's parity profile, and the interaction machine's latch section, and it lives outside the
clone.

**Independence caveat, stated plainly.** The dispatching brief named no artifact defect and no area of
suspicion. Three things in it narrowed where effort went, and I record them so the next cycle can
discount accordingly.

1. It told me to verify the pin myself rather than take it from the brief, and pointed at findings U6
   and X6 as the reason. I did (see **Pin**), it holds in the tree-hash form, and checking it is what
   surfaced **AI8**.
2. It restated the policy's requirement of at least one genuine attempt to falsify a capability-wide
   property. Roughly half the effort here went to C4, C12, the neutral brief, the completeness
   review's property audit and silence table, and the four entry-point narratives. C3, C5, C6, C7, C9,
   and C11 were assessed by reading and cross-tracing rather than by probe, with one falsification
   attempt at `C11-P1` and one at `C1-P1`.
3. It told me to read closure review 12's attestation for form. That attestation is a detailed account
   of AH1-AH6, so my verification that the AH corrections landed is verification of findings I had
   been told about. **AI1, AI4, AI5, AI6, and AI7 are in no retained record.** AI2, AI3, and AI9 are
   re-derivations of AH3's, AH5's, and S3's own evidence sentences against the artifacts they name —
   the method the policy's eleventh-review method note asks for — and AI8 is a fact review 12 saw and
   explicitly declined to raise.

I did **not** read any retained attestation before forming my own reading of C4, both machines, the
grid, and the brief, and before writing and running the `C4-P2` evaluator. Review 12's attestation was
read afterwards; reviews 8-11 were consulted only for specific findings' evidence sections when
re-deriving them.

## Disclosed process deviation in this dispatch

This review was dispatched by a session that, on its own account:

- **authored the correction commit under review**, `fix(channel): close AH1-AH6 and rule the closure
  standard`, including every artifact edit and every verifier check in it;
- **also authored the four commits before it** — the AG, AF, AE, and AD corrections — and the
  [AD correction iteration review](./channel-0.2-ad-correction-iteration-review.md) retained in this
  directory;
- **dispatched closure reviews 9, 10, 11, and 12**, whose findings those commits correct; and
- **recommended the 2026-08-15 closure-standard ruling**, which the repository owner made on that
  recommendation *after* the twelfth review's verdict was known.

This is the same relationship the directory discloses for closure reviews 10, 11, and 12 — the
dispatcher is the author of the very commit being judged — extended by one further commit, one
further dispatch, and now by authorship of the recommendation behind the standard my verdict is
measured against. It is recorded because an undisclosed relationship between a dispatcher and a
reviewer is the same class of defect as an undisclosed reviewer-repairs-own-finding, which this
directory already discloses twice.

**What the dispatch did and did not carry.** The brief conveyed none of the dispatching session's
findings, reasoning, or conclusions. It named no artifact defect, no area of suspicion, and nothing
about where it believed the work was weak or strong; my context contains nothing from that session
beyond the brief itself. It pointed me at `AGENTS.md` and this directory's policy, told me to take my
scope from them rather than from the brief, told me explicitly that I was reviewing work whose author
had arranged my review and that this was a reason to probe the corrections harder rather than defer to
them, told me that eleven reviews returned `does-not-conform` and the twelfth returned
`conforms-with-nonblocking-findings` and that this was context rather than a target in either
direction, and stated that neither manufacturing a finding to avoid committing to a verdict nor
suppressing one to reach a cleaner verdict was acceptable. It told me the 2026-08-15 ruling exists and
told me to read it myself rather than take its summary.

**Did anything in the dispatch narrow where I looked?** Yes, as recorded in the caveat above: the
instruction to verify the pin, the instruction to run an evaluator, and the instruction to read review
12 for form concentrated effort on the pin clause, on C4/C12/the brief, and on the AH findings.
**Nothing in the dispatch narrowed what I concluded**, and the blocking finding is not in the set the
brief pointed at: it is on the operand AH1 did *not* correct, in a conjunct the brief's instructions
gave me no reason to prefer over the first.

**One further note on the arrangement, because this cycle has a feature the previous four did not.**
The standard against which my verdict is scored was recommended by the dispatching session, after a
favourable verdict arrived, and it is a standard under which any finding at all withholds closure.
That cuts both ways and I record both. It removes any incentive for me to soften a finding to reach a
clean verdict, since the clean verdict is now unreachable with even one remark. It also means a
reviewer inclined to please the dispatcher has no cheap way to do so, which is a property of the
ruling worth having. What it does not do is bear on whether **AI1** is real; that rests on probe
**P3**, which anyone can re-run from the published prose.

## Pin

The policy's pin clause names the current target as the commit titled
`fix(channel): close AH1-AH6 and rule the closure standard`, "or any later commit whose design
artifacts hash identically to it — and check that claim rather than assuming it, because this clause
has now gone stale twice" (U6, then X6).

I checked it against the repository rather than against the brief, and it holds in the stronger
whole-tree form:

```text
git log -1 --format=%s a632d3f   ->  fix(channel): close AH1-AH6 and rule the closure standard
git rev-parse a632d3f^{tree}     ->  9703ab83f290cad8885c9d1c834ee29605b451b5
git rev-parse e7bfeba^{tree}     ->  9703ab83f290cad8885c9d1c834ee29605b451b5
git diff --stat a632d3f e7bfeba  ->  (empty)
```

The whole tree is identical, so every design artifact hashes identically by construction. `e7bfeba` is
the merge of PR #123 bringing `a632d3f` to `main`; `a632d3f` carries exactly the named subject and is
the head of the correction sequence beginning at `fix(channel): make C4-P2 falsifiable`. The X6
correction — checking this sentence against the most recent commit that changed a design artifact
rather than against its own wording — holds at this pin as to *subject*, and the design gate passes.

It does **not** hold as to date, which is **AI8**: the clause says "committed 2026-08-14" and
`a632d3f` is committed 2026-08-15 14:17:58 +0200. Its predecessor `6d0e43f` is 2026-08-15 10:59:03
+0200, so the date was already false when review 12 read it. Review 12 saw this and recorded "Noted,
not raised". I raise it, at the lowest weight in this attestation, for the reason given under AI8.

## Blocking findings

### AI1 — the settling-frame reference carries no session, the same commit declared multi-session vectors legal, and `C4-P2` then evaluates green on its own named mutation `C4-outcome-precedes-ack`

**Artifacts.**
`Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Vector format", the two bullets AH1 rewrote
(lines 198-214); the same document §"Local observation" (lines 173-177); the same document
§"Observation and parity profile", the settling-frame bullet (lines 336-350);
`Brontide-Channel-0.2-Interaction-State-Machine-0.1.md` §latch, "Settling the latch also **records the
frame that settled it**" (lines 215-225); `Brontide-Channel-0.2-Capability-Contract-0.1.md` §C4,
`C4-P2`'s second conjunct (lines 266-270) and §C10 (lines 528-535);
`build/verify-channel-0.2-design.ps1`, the AH1 block at `$stimulusStep`.

**What the correction did, and what it left.** AG2 scoped the precedence operator to one session. AH1
found the operand missing and supplied it, in two places:

> ordered stimulus steps, each naming its **committing endpoint**, its **session**, and, where it
> carries one, its interaction identity.

> profile, and the initial session/interaction state of **each session the vector carries**. A vector
> **may carry more than one session** …

The second sentence is the part that matters here. Before this commit the question was open, and
review 12 said so exactly: "if a vector may carry two sessions, precedence is not evaluable and AH1 is
live; if it may not, then AF8's membership scope, AG2's precedence scope, and three paragraphs of
normative justification all defend against a vector no author can write". The commit answered it in
the affirmative — correctly, in my judgement — and thereby made a second operand ambiguous that had
been safe only while the answer was undecided.

**The operand it left.** `C4-P2`'s second conjunct is:

> none records a late-traffic `state-violation` latched against a frame whose committing endpoint had
> committed it before that endpoint's own frame that made the interaction terminal

Evaluating it requires binding the settling frame to a declared stimulus step. Two artifacts state the
reference and both state it as four fields with **no session**:

> the **frame that settled the latch** wherever one is settled: its kind, its interaction identity,
> its committing endpoint, and its **arrival ordinal** within that interaction. The ordinal is what
> makes the reference unambiguous … With the ordinal the settling frame maps to one declared step —
> directly where no reordering is injected, and through the named injection where one is — and the
> precedence relation compares that step and no other. *(neutral brief, parity profile)*

> Settling the latch also **records the frame that settled it** — its kind, its interaction identity,
> the endpoint that committed it, and its **arrival ordinal** within that interaction … the ordinal is
> what maps the settling frame to one declared stimulus step. *(interaction state machine)*

Both assert the reference is *unambiguous* and maps to *one* declared step. The brief's own local
observation schema repeats the same four positions. A vector that carries two sessions holding the
same interaction identity value — which the parity profile, two paragraphs below, now explicitly
permits ("a vector carrying two sessions may hold the same identity value in both") — presents two
declared steps matching all four fields, because the ordinal is counted "within that interaction" and
the reference cannot say which of the two interactions it means. Both assertions are false at this
pin, and they were true at `f451f55`.

**Probed, not reasoned.** Probe **P3**, row `M2-two-session`. Vector: session B is wholly conforming
and reuses interaction identity `x1` — request, one cancellation control, one acknowledgement, still
nonterminal; session A carries the named mutation `C4-outcome-precedes-ack` for the same identity
value — the recipient commits the acknowledgement, then the Outcome, and the Outcome is delivered
first, so the late acknowledgement settles the initiator's latch to `fault-committed`. Design expects
**red**: this is a named mutation `C4-P2` must fail on.

| binding of the settling-frame reference | verdict |
| --- | --- |
| **A** — session taken from the enclosing local observation (a reading no artifact states) | **red** |
| **C** — ordinal indexed over the endpoint's frames of that kind for that identity, which is what the four published fields support | **green** |

Column C is the U1 condition. `C4-P2` is green on its own named mutation, produced by a vector any
author may write under the rule this commit added and an evaluator any author may write from the
fields these two artifacts publish. The mechanism is precise: the ordinal binds the reference to
session B's acknowledgement, session B has no terminal frame for that identity, and precedence — which
AG2 correctly restricted to one session — then returns no verdict at all, so the real violation in
session A goes unwitnessed.

**Why I rate this blocking, and the exact condition under which an owner should not.**

Blocking, on four grounds. It is a demonstrated false green on a named mutation, which is the one
defect class this programme has ruled blocking every time it has appeared — U1, AC3 through
unsatisfiability, AF1, and AG1 — and which C12 makes a finding against the property outright. It was
*created by the reviewed commit* rather than inherited: the ambiguity requires the multi-session
ruling, and that ruling is in this diff. It is the same defect as AH1 on the sibling operand of the
same conjunct, so the correction supplied the session to one operand of a two-operand conjunct and not
the other — W5's shape inside the correction written to close AH1's restatement of W5. And the trigger
is nameable and concrete, which is what separates it from AH1, AG2, and AF8: those were false *reds*
in a vector class no required group contains and no reviewer could name; I named this one and ran it.

The counter-reading, stated so the disagreement is locatable. C10 requires a local observation to
distinguish "session and interaction identities", the settling frame is a nested position inside one
such observation, and the frame that settled a terminal interaction's latch is necessarily a frame of
that interaction — so an evaluator author *could* take the session from the enclosing record and
reach column A. That inference is sound. It is also written down nowhere, and it is the inference the
programme has twice refused to leave implicit: Y4 added the arrival ordinal rather than let kind,
identity, and endpoint "obviously" identify the frame, and AC1 was raised because the resulting field
was stated in the subordinate artifact alone. **If the owner holds that a comparison field published
with an exhaustive field list may rely on an unstated containment inference, AI1 is nonblocking and my
verdict is wrong.** I do not hold that, because the two sentences quoted above make an affirmative
claim of unambiguity that is false at this pin, and because a property file authored from the parity
profile is exactly what Batch 2 is authorized to write.

Two things are **not** wrong with `C4-P2`, and I record them because a correction pass should not
widen the fix. The property is sound over its required vector group: probe **P3** finds it green on
all seven legal members and red on both named mutations in the single-session form, and green on
review 12's two-session conforming vector `P`, which is AH1's fix working. And AG2's session
restriction on precedence is correct and load-bearing — it is what prevents a *false red* on the same
vector class. AI1 is the missing session on the settling-frame reference alone.

## Nonblocking findings

### AI2 — AH3 is closed in one of the three narrative surfaces its evidence names; the other two still stop at the tenth review, which is what review 12's closing note predicted

**Artifacts.** `docs/future/README.md` §"Priority 1 — Channel 0.2 redesign and migration", the
design-package narrative (lines 33-70); `docs/future/channel/README.md` §"Channel 0.2 design
foundation", the opening narrative (lines 6-25) and the second narrative (lines 28-44);
`build/verify-channel-0.2-design.ps1`, the AH3 block.

AH3's evidence named three surfaces. The **redesign plan's status block** is corrected: it now reaches
the eleventh and twelfth reviews and the AG and AH families, and a new verifier check pins it. The
other two are not.

**`docs/future/README.md`.** The correction changed two tokens — `11 retained independent reviews` to
`12`, and `No independent review has yet seen the AF corrections` to `AH` — and updated the table row
at line 1645. AH3's first evidence sentence was that this document "carries the eleventh review's
existence nowhere in its Priority 1 prose … `AG` appears nowhere else in the file." That is still
exactly true, and is now true of the twelfth review as well. The narrative runs ninth review → AE,
tenth review → AF, "All eight are corrected", and then jumps to a sentence about "the AH corrections"
— a family the prose never introduces. `grep -n "AG\|AH"` over the whole file returns three hits: line
68 (`the AH corrections`), line 247 (`AGENTS.md`, not the family), and line 1645 (the table row). The
substitution made the false sentence true and left the "stopping short" half of the finding, which is
the half AH3 identified as AA2's defect.

**`docs/future/channel/README.md`.** The correction changed the range sentence to "S1 through
**AH6**" and updated all nine artifact rows. AH3's second evidence sentence was that the narrative
"narrates 'the ninth closure review raised AE1-AE5 …; the tenth raised AF1-AF8 … Both are corrected',
and stops. No eleventh review, no AG family, and the paragraph below it still ends 'and most recently
a fix stated only in the artifact that reads the fact rather than the ones that own it', which
describes AC1." All three clauses are still true, verbatim, at this pin. AH3 named this as "AF2's
defect in the same document, one paragraph over from where AF2 was corrected"; it is now that defect
one further paragraph over from where AH3 was corrected.

**Why no gate sees it.** The new AH3 check reads the plan's status block and the future index's "no
independent review has yet seen the … corrections" sentence. Neither narrative is read by anything:
the AA2 family check asks only that each family appear *somewhere* in `docs/future/README.md`, which
the table row satisfies, and the AF2 check reads the Channel index's range sentence alone.

Nonblocking, on the programme's unbroken precedent for entry-point staleness (S3, AA1/AA2, AE4, AF2,
AG4/AG5, AH3). Recorded because this is the seventh consecutive cycle in which it recurs, because two
of AH3's three named surfaces are unchanged in the commit that closes AH3, and because review 12's
closing note stated this outcome in advance: "**AH3** spans three documents and no gate reads any of
the three narratives; if the pass updates the counts and the status blocks without reading the prose
above them, the same finding survives the commit that closes it, for the sixth time."

### AI3 — AH5 is closed in the property audit's `C4` row and open in its `I5` row, which AH5's evidence names alongside it, and the pointer it added points the wrong way

**Artifacts.** `Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md` §"Per-capability property
audit", the `C4` row (line 176) and §"State-machine properties", the `I5` row (line 206); the same
document §"Required silence probes and dispositions", the `direction scope of the in-flight bound`
row (line 143).

AH5's evidence section named "the `C4` **and** `I5` rows of the property audit", and its substance is
that under AE3's converse rule the direction-scope disagreement is a known conforming-realization
exposure **for `C4-P1` and `I5`** rather than an undeclared scope, while "both required-green cells
read `owed`, so nothing records that `C4-P1` and `I5` have a known conforming-realization exposure".

The correction wrote the AE3 connection into the direction-scope row, correctly, and added a pointer
to one of the two cells:

> `C4-P1`: **owed**, and see the direction-scope disposition below before filling it

The `I5` row is untouched. Its required-green cell reads `**owed**` and nothing else — which is
precisely the state AH5 says "reads as 'not yet written' rather than 'known to have a red case'". The
direction-scope row's own new sentence names the consequence: "a pass filling **either** set in
without settling the direction scope first would reproduce the omission." The warning that would reach
such a pass exists only in the cell it will not read, because an author filling `I5`'s required-green
set reads the `I5` row.

This is the sixth instance of the closed-in-the-first-artifact pattern — AE4→AF2, AE5→AF3, AF1→AG1,
AF2→AG4, AF5→AH2, now AH5→here — and the first that is a single table with two rows rather than two
documents. The commit's own recorded lesson is the right one and was applied one row short: "a sweep
must enumerate what a correction *touches*, not what a finding's author cited." AH5's author cited
both rows.

**A second, smaller defect in the same sentence.** "see the direction-scope disposition **below**" —
the direction-scope row is at line 143 and the `C4` audit row at line 176, so it is above. A
navigation instruction added to steer a future author steers it the wrong way.

Nonblocking, on the same ground review 10 gave AF5 and review 12 gave AH2: the normative statement is
correct and only a record is incomplete, and no property changes verdict on it.

### AI4 — six of the eight design artifacts' own status blocks are stale by one to four finding families, while the Channel index rows claim those corrections

**Artifacts.** The `Status:` blocks of
`Brontide-Channel-0.2-Capability-Contract-0.1.md` (lines 5-25),
`Brontide-Channel-0.2-Interaction-State-Machine-0.1.md` (lines 5-18),
`Brontide-Channel-0.2-State-Event-Coverage-0.1.md` (lines 5-17),
`Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md` (lines 5-12),
`Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md` (lines 5-12), and
`Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` (lines 5-22);
`docs/future/channel/README.md`, the design-foundation artifact table;
`build/verify-channel-0.2-design.ps1`, the T4 check at line 908 and the AB1/AH3 checks at lines 1094
and 1527.

Enumerated mechanically from the first twenty-five lines of each artifact (probe **P4**), against the
corrections the Channel index's own per-artifact rows claim:

| Artifact | Newest family its status block names | Families the index row claims corrected in it |
| --- | --- | --- |
| Capability contract | `AC3` | AE1, AE3, AF1, AF5, AF8, AG2, **AH6** |
| Session state machine | `D1` | none since — index says unchanged by AE/AF/AG/AH ✔ |
| Interaction state machine | `AC2` | **AE2** |
| State/event coverage | `AC2` | **AE2** |
| Responsibility matrix | `AC1` | none since — index says unchanged by AE/AF/AG/AH ✔ |
| Contract-completeness review | `AC4` | AE3, AF7, AG1, **AH2**, **AH5** |
| Migration ledger | `AE5` | **AF3**, **AF4** |
| Neutral contract/vector brief | `AC2` | AE1, AE3, AF5, AG2, **AH1**, **AH6** |
| Redesign plan | `AH` ✔ (corrected by AH3) | — |

Six of the eight are behind. The brief is behind by four families in the artifact this commit edited
twice, and its status block still describes the operand AH1 changed in its pre-AH1 form: "stimulus
steps name their committing endpoint so that relation has an operand". The completeness review's block
still says "the disposition history now runs to the eighth cycle", and it runs to the twelfth — which
is U4's own defect restored as a self-description, and AD3's class ("descriptions that understate the
document they describe").

**Why no gate sees it.** T4's check constrains only that each status block carries the one stable
cycle phrase and names no superseded cycle; it says nothing about families. AB1's check, and the AH3
check written in this commit, cover the redesign plan alone — the plan was described as "the one status
block the T4 cycle-name check never covered", and the correction generalised the *finding* to the plan
rather than the *class* to the eight artifacts that carry the same kind of block.

This is AB1's defect on eight new surfaces. It has been true since the AE pass and was walked past by
the AE, AF, AG, and AH correction passes and by reviews 9, 10, 11, and 12, including by me until I
enumerated rather than read. Nonblocking on the entry-point precedent; recorded because the index rows
assert corrections the artifacts' own front matter contradicts, and because a reviewer who trusts a
status block over the document is the failure AD1 was raised for.

### AI5 — the AH1 correction justifies its multi-session ruling by citing "C2's reconnect and new-session cases", and C2's Silence disclaims reconnect

**Artifacts.** `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Vector format", the
profile/session bullet (lines 198-203); `Brontide-Channel-0.2-Capability-Contract-0.1.md` §C2, its
Named scenarios and its Silence (lines 121-132);
`Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md` §"C4 — reconnect cannot inherit identity
silently" (lines 69-72) and the extension-pressure row at line 167;
`Brontide-Channel-0.2-Session-State-Machine-0.1.md` line 40.

The new sentence reads:

> A vector **may carry more than one session**: **C2's reconnect and new-session cases require it**,
> and the identity rules make it observable …

C2 says:

> **Named scenarios.** `C2-drain-refuses-new`, `C2-drain-preserves-in-flight`,
> `C2-ready-is-not-session-state`, and `C2-late-control-after-close`.
>
> **Silence.** C2 does not define process launch, **reconnect**, session resumption, leader election,
> or Component activation ordering.

C2 has no reconnect case and no new-session case; it names reconnect once, to disclaim it. The
reconnect facts live in the session state machine ("Reconnect creates a new session identity and
begins at …") and in the completeness review, whose own extension-pressure row assigns "reconnect after
fault" to a **future resumption contract** rather than to C2, and whose §C4 heading is literally
"reconnect cannot inherit identity silently" — a silence probe, not a case.

This is AG2's class: a correction asserting that another artifact carries something, and it does not.
It matters more than the usual instance for two reasons. It is the stated *justification* for a
normative ruling — the ruling AI1 turns on — so the ruling's only cited support points at a disclaimer.
And the reviewed commit asserts, in the completeness review's own disposition history, that this class
is now structurally prevented: "cross-artifact claims are pinned against the artifact they describe so
AG2's class cannot be written again." The AH1 check pins that the phrase `more than one session`
appears in the brief; nothing pins the claim about C2 against C2.

Nonblocking, on review 11's rating of AG2 itself. The ruling is correct on other grounds — the session
machine and the completeness review both do supply reconnect, and C2's own reconnect silence is
compatible with a *vector* spanning two sessions — so no design fact is wrong; the citation is.

### AI6 — the AH6 insertion leaves `C4-P2`'s membership sentence attached to the wrong antecedent, which is AC3's class in the paragraph AC3 was raised against

**Artifacts.** `Brontide-Channel-0.2-Capability-Contract-0.1.md` §C4, lines 283-294;
`Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Observation and parity profile", the admission
bullet (lines 353-366).

Before the correction the contract read: "… a loss never delivers it and no admission ever occurs.
**The conjunct reads that**, through a membership test over the identities the recipient admits **in
the same session**." "That" pointed at the fact that separates a reordering from a loss, which is what
the conjunct reads.

The correction inserted a paragraph between them and did not re-anchor the pronoun. The text now ends
the AH6 paragraph and continues on the same line:

> … A reordering hidden behind an independent refusal is not witnessed by this conjunct, and no
> artifact claims otherwise. The conjunct reads that, through a membership test over the identities
> the recipient admits **in the same session**.

The nearest antecedent of "that" is now "a reordering hidden behind an independent refusal is not
witnessed by this conjunct" — which the conjunct does not read, and which the membership test is not
over. AC3 was raised for exactly this: "both conjuncts said 'no endpoint records … the same endpoint
had already committed', whose nearest antecedent is the recording endpoint … the literal reading was
unsatisfiable and therefore unfalsifiable, which is U1 through a pronoun." The consequence here is
milder — the sentence introducing AF8's membership operand is merely unreadable rather than
unsatisfiable — but it is the same defect in the same paragraph of the same artifact, introduced by
the pass that cites AC3 four paragraphs below.

The mechanical cause is visible in the diff: the inserted paragraph was appended to the line the
original sentence continued, producing a 140-character line in a document that otherwise wraps at
about 100 and merging two paragraphs into one. The brief's parallel edit has the opposite artefact —
"The conjunct\n tests membership of the identity in the\n set the recipient admits" — a sentence
broken across three short lines mid-clause. Both are cosmetic on their own; together they are the
signature of an insertion made without re-reading the paragraph it lands in, which is what produced
the pronoun.

Nonblocking: no property changes verdict, and the substantive AH6 correction is right. The coverage
limit is now stated in both artifacts, in the contract's own terms, and probe **P3** row `R` confirms
it — a reordering whose displaced request is refused on its own merits leaves the first conjunct
green, and the design now says so instead of claiming the retention rule requires the admission.

### AI7 — the vector format's `profile` and the parity profile's established-profile digest stay singular in the same lists AH1 made per-session

**Artifacts.** `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Vector format", the first
bullet (line 198); the same document §"Observation and parity profile", the first compared field (line
317); `Brontide-Channel-0.2-Capability-Contract-0.1.md` §C1.

The rewritten bullet is:

> profile, and the initial session/interaction state of **each session the vector carries**.

`profile` was not distributed over the sessions the same sentence just made plural. C1 establishes
"one immutable profile" **per session**, and a reconnect — the case AH1 cites — re-establishes, so a
two-session vector has two established profiles and may legitimately have two different ones. The
parity profile's first compared field, "exact established profile digest", is singular in the same
way, and `C1-P1` reads "exactly one profile is established".

Same class as AH1, in the same bullet, one field to the left; and the same class as AI1, in the
comparison list rather than the reference. Nonblocking and clearly the least severe of the seven: no
required vector group names a multi-session vector, no property is evaluated differently, and the fix
is a plural. Recorded because AH1's second half exists precisely to stop a per-session field being
written in the singular, and because the two remaining singulars are in the two lists AH1 edited.

### AI8 — the pin clause dates the target commit 2026-08-14 and it is 2026-08-15, in the one sentence this programme has twice raised staleness against

**Artifacts.** `docs/future/channel/reviews/README.md`, the pin clause (lines 586-595);
`build/verify-channel-0.2-design.ps1`, the X6 check.

> The current review target is the commit titled `fix(channel): close AH1-AH6 and rule the closure
> standard`, **committed 2026-08-14**, which is the head of the correction sequence …

`a632d3f` is committed `2026-08-15 14:17:58 +0200`. Its predecessor `6d0e43f`, which the clause named
before this commit, is `2026-08-15 10:59:03 +0200`, so the date was already false at `f451f55`. The AH
pass rewrote the subject in this sentence and carried the date through unexamined.

The X6 correction compares this sentence against the most recent commit that changed a design
artifact, and it compares the *subject* only — so the date is the one part of a self-checked pin clause
that no check reads, in the clause raised as U6 and then as X6 for going stale.

Nonblocking and the lowest-weight finding here: the pin is established by subject and tree, both of
which I verified, and no reviewer is misdirected by a date that identifies nothing. Recorded rather
than passed over for one reason. Review 12 saw it, wrote "Noted, not raised", and it survived the
correction pass that read review 12 line by line — which is how the U6 → X6 sequence began, and the
cheapest possible demonstration that "noted, not raised" is not a disposition this programme's
machinery can act on.

### AI9 — S3's own evidence named the redesign plan's §7.8, which still reports seven retained negative attestations and stops at the seventh review; the same false "negative" survives in the verifier message the AH pass edited

**Artifacts.** `Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md` §7.8 "Fresh independent
design review" (lines 319-330); `build/verify-channel-0.2-design.ps1`, the review-file-set failure
message at line 999 and the adjacent index-count check at lines 1063-1068;
`channel-0.2-design-foundation-closure-review-7-attestation.md`, S3's evidence list (lines 300-315).

**The retained finding.** S3's evidence quoted this exact section: "Same document, §7.8: '**Five**
independent negative attestations are retained. Their findings through T1-T4 have correction passes at
`11ba93bd…`.' Six are retained, and the current correction pin is `3892c23`." The correction updated
the number to seven and the narrative to the seventh review. At this pin the section reads:

> Seven independent negative attestations are retained. Their findings through T1-T4 and R1-R3 have
> correction passes, the last three confirmed closed by the seventh review at `3892c23a…`. That
> review's blocking S1 and nonblocking S2 and S3 are corrected under the 2026-08-13 S1 ruling …

Twelve attestations are retained; the U, V, W, X, Y, Z, AA, AB, AC, AD, AE, AF, AG, and AH families
all postdate this paragraph; and the "negative" is now false of the twelfth. **S3 is therefore not
closed in one of the two artifacts its own evidence quotes** — it was closed by incrementing the number
S3 quoted, which is exactly the failure mode the policy's eleventh-review method note warns about:
"take each retained finding's evidence sentences, not its title, and re-derive each one."

This is also the sharper reading of **AI2** and **AI4**. AB1 and AH3 both corrected this artifact's
**Status** block, twice and three times respectively, and the Channel index row declares both closed;
§7.8 is the section a reader following the plan's own review heading arrives at, and it has never been
covered by any check. The AB1/AH3 check reads `^\*\*Status:\*\*` to `^\*\*Designed against` and nothing
below it.

**The same false word in the verifier, in this commit's own diff.** The AH pass changed one count check
away from the phrase, with an explicit reason:

> "retained attestations" rather than "negative attestations": review 12 returned
> `conforms-with-nonblocking-findings`, so the count is no longer a count of negatives and a phrase
> saying otherwise is a false claim in the index that reports it.

Thirty lines above, in the same commit, the same pass changed `all eleven negative attestations` to
`all twelve negative attestations` — updating the count and preserving the word it had just diagnosed
as a false claim. I saw the message myself: retaining this attestation makes the gate emit "must retain
exactly the review README, all twelve **negative** attestations, and all four correction iteration
reviews".

Nonblocking, on the same precedent as AI2 and AI4: it is a count and an adjective, no design fact is
contradicted, and the review policy and both indexes describe the twelfth review's verdict correctly.
Recorded because a retained finding that is open in an artifact its own evidence quotes is the one
thing this attestation is required to report, and because the paired verifier instance shows the
diagnosis and the omission were thirty lines apart in one diff.

## Capability verdicts

| Item | Verdict | Rationale |
| --- | --- | --- |
| C1 | conforms | One immutable profile established before any interaction is dispatchable; negotiated and fixed paths yield the same inspectable facts; unknown Channel versions, required features, classes, authority modes, and incompatible application contracts refuse; no implicit downgrade and no in-place renegotiation. `C1-P1`'s disjunction is total over its vectors — either exactly one profile with every normative fact equal, or nothing dispatchable with `known-none`. A falsification attempt (probe **P6**) failed: no path lets a validation refusal, a peer establishment fault, or transport loss yield an established session. Cross-checked against the session machine's fixed/negotiated equivalence, which makes a field absent from the fixed path a contract defect rather than realization freedom. The established-profile image carries the realization's per-interaction frame order declaration, and W2's point — establishment verifies the declaration is *present*, never *true* — remains stated at the provider boundary. **AI7** touches C1's territory from the brief's side and is recorded there. |
| C2 | conforms | Six states, `closed`/`faulted` terminal and non-transitioning, drain refusing new interactions while admitted ones reach a terminal fact, D1's duplicate drain fatal with the first snapshot preserved and no interaction's effect certainty rewritten. `C2-P1` covers acceptance, the leave-unchanged-or-fault alternative, and terminal monotonicity. The session totality rule explicitly does not override the named nonfatal peer-interaction-during-drain row, so no event/state pair offers a choice. Interconnection, Ready, Release, withdrawal, and Component termination are each listed as explicitly not session states. C2's Silence disclaims reconnect and session resumption, which is correct and is what **AI5** is about — the citation, not the capability. |
| C3 | conforms | Class, direction, and external phase are three separate exact admission inputs evaluated before dispatch; `false` and `unknown` are treated identically; the receiver's independently derived phase gets D3's frameless `refused-local` with `known-none`; T1's rule that a phase refusal is never `state-violation` is carried in the ledger's `state-violation` row. Channel evaluates the declared predicate without creating or advancing the phase, and `C3-P1` binds all three inputs conjunctively. The Portable Binding 0.2 profile's two declared classes match C7 and Decision 13. |
| C4 | does-not-conform | **AI1** is against `C4-P2`'s second conjunct and is blocking; **AI3**, **AI6**, and **AI7** also bear on C4. What is sound is sound and I record it: probe **P3** finds `C4-P2` green on all seven legal members of its required vector group and red on both named mutations in single-session form, so AE1, AF1, AF5, AG1, and AH2 all hold; AH1's fix works, and review 12's two-session conforming vector is green under precedence as published. `C4-P1`'s three clauses, the finite positive `max-in-flight`, replay as a nonterminal-window fault with T2's post-terminal split, W4's retention rule with X5's recording-versus-retaining distinction, AC3's committing-endpoint subject, AF8's session-scoped membership operand, and both conjuncts' restriction to one endpoint's own frames all hold. What does not hold is the claim, made in two artifacts, that the settling-frame reference maps to one declared step — false at this pin, in a vector class this pin created. |
| C5 | conforms | Positional payload/authority classification with authority positions never projecting; parsing and structural validation before handler dispatch; no partial or oversized frame becoming a partial interaction; `known-none` on every pre-dispatch structural refusal. `C5-P1` binds dispatch to having passed every declared bound and positional rule. Environmental limits tighter than the profile's must be exposed and accepted at establishment, which is where the retained register's `CH-K6` hardening asymmetry is answered. Allocation failure is locally classified without transporting a runtime exception. |
| C6 | conforms | Authority evaluated per interaction after structural admission and before dispatch; delivery, correlation, establishment, provider availability, and Shape compatibility each explicitly disclaimed as grants; local denial emitting no frame and recording `known-none`; cross-trust carrying attributable context and exact designations and no Capability, Constraint expression, or derivation chain. `C6-P1` requires exactly one `permitted` local decision to reach dispatch and requires every denial or unevaluatable presentation to record decision point, initiator attribution, and `known-none`. |
| C7 | conforms | Traced clause by clause against Decision 13 as recorded in `binding/portable/open-decisions.md` §686: Option A retained for 0.1 and Option B selected for 0.2, C and D rejected, recorded 2026-08-11. C7 carries Option B's exact CM3-declared edge, direction, initiating member, receiving member, Operation, Capability, and input Shape; the post-Interconnection pre-Ready window; the composition root initiating on the Component's behalf; the refusal to introduce a Component-to-Component binding kind; and failure preventing Ready and Release while returning the actual observation to CM4 cleanup or rollback. `C7-P1` forbids the interaction producing Ready or Release by itself. Option B's wording says "a new envelope kind" and C7 uses the ordinary interaction form; that departure is explicit, reasoned in the completeness review, and recorded in the matrix's boundary ruling. |
| C8 | conforms | One accepted terminal history from five named forms; cancellation an optional core control with fixed meaning and exactly one request per nonterminal dispatched interaction; the acknowledgement explicitly nonterminal in both `accepted` and `refused` forms; R1's held control bounded at exactly one with R2's statement that the two preconditions are local and unsynchronised; S2's third and fourth exits from `validating`; T3's `cancelled`-with-no-request-in-force routed as a class at both endpoints. C8's statement that recipient admission is not observable from `dispatched` is what makes AE1's loss vector legal and is correctly unchanged. |
| C9 | conforms | Four provenance forms with an exclusivity property; an unknown peer-fault category faulting the local session with no answering fault and no loop; loss categories and detection points observer-relative and claiming no global topology. `C9-P1` forbids any field permitting a local inference to be accepted as a peer statement or a protocol fault as an Outcome. PB8's blocking finding — both stacks fabricating a known zero effect count on process loss — is answered by C10's certainty form rather than restated as a Channel 0.1 defect. |
| C10 | conforms-with-nonblocking-findings | AE2's `known-none` is present in the machine row and both grid cells; AC2's refused-frame kind and detailed reason, Y1/Y2's latch and settling frame, and Z3's `not-applicable` are all present and owned by the matrix's `Local observation content and provenance` row. `C10-P1` forbids an unsupported `known-none` after a possible post-dispatch path. C10 does require an observation to distinguish "session and interaction identities" — which is the containment inference **AI1**'s counter-reading rests on, and which C10 states about the observation and not about the settling-frame position nested in it. |
| C11 | conforms | Facets may add classes, payload forms, and stronger delivery evidence and may not reinterpret session/interaction identities, authority decisions, the four terminal provenance forms, or effect uncertainty; retry is a new interaction identity with optional attributable causation and never replay; the intra-interaction ordering fact is named as the one ordering fact core owns, which a facet may strengthen and may not weaken. `C11-P1` binds both halves. A falsification attempt (probe **P6**) failed: no facet route reaches a core identity, authority, terminal-provenance, or uncertainty result, and cross-capability invariant 7 and the matrix's `Extension hooks` list agree with C11 rather than restating it loosely. |
| C12 | does-not-conform | C12's own rule is what **AI1** violates: "Every property must be able to fail against a named incorrect implementation." At this pin `C4-P2` can be evaluated green on `C4-outcome-precedes-ack` from the published field list, so the rule is unsatisfied for the one property this programme has spent nine cycles on. AE3's converse rule is stated in C12 in the terms that make it a rule and the brief's format carries the required-green set as a normative field; AF7's audit extension holds — I enumerated it independently at 12 capability rows + 13 state-machine rows = 25 audited against 12 C-properties + `S1`-`S6` + `I1`-`I7` = 25 stated (probe **P4**). The audit's honesty about `I1`-`I7` satisfying neither half is the right disposition and is disclosed residual work; **AI3** is one cell of it left behind. |

## Area verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state | conforms | Ten legal transition rows, a refused/illegal table, a totality rule that explicitly does not override named nonfatal rows, and `S1`-`S6` carried in the property audit under AF7. No external phase appears as a session state and each is listed as explicitly not one. Reconnect creates a new session identity and begins at `unestablished`, inheriting no replay or in-flight state — which is the artifact that actually carries the fact **AI5** misattributes to C2. Drain is symmetric and occurs exactly once per endpoint history. Its status block is the one that has correctly stayed at `D1` (**AI4**). |
| Interaction state | conforms-with-nonblocking-findings | Twelve initiator and twelve recipient states with terminality marked; every transition row carries an effect-certainty or handler-effect column; `I1`-`I7` hold as statements and are audited. The `unseen` row is a detailed row (X3) routing to `unseen` rather than to the terminal state (Y3), carrying `known-none` (AE2), the refused frame's kind and detailed reason (AC2), and no history, latch, or reservation (W4). The terminal-provenance table gives the refusal a declared provenance. The latch section's settling-frame paragraph is where **AI1**'s second half sits: it asserts the four fields map the frame to one declared step, which the reviewed commit made false. Status block stale by one family (**AI4**). |
| State/event totality | conforms | Independently enumerated (probe **P2**): 6×6 session, 6×6 initiator, 6×6 recipient published rows = **108 published-row cells**, zero empty; expanding the published groups against the machine's own state tables (12 initiator states of which 6 terminal, 12 recipient states of which 7 terminal) gives **180 underlying state/event pairs**. Agrees with reviews 7-12. The six-rule closed-world ordering is well-founded, rule 1 genuinely claims the `unseen` event so rule 2 cannot produce the terminal `peer-fault` W4 refuses, and the `not-applicable` latch is asserted as a value rather than an absent field. Status block stale by one family (**AI4**). |
| Responsibility | conforms | Enumerated mechanically (probe **P4**): **39 ownership rows, 22 distinct owner identifiers used, every row carrying exactly one backticked owner, zero rows with two owners or none, and no `channel-core` in any owner cell** — it survives only in the prose recording that U2 abolished it. The `Intra-interaction frame order` row is owned by `channel` with the realization profile's declaration as its crossing artifact; the `Local observation content and provenance` row (AB2) names the latch with its `not-applicable` value, the settling frame with its arrival ordinal, and the kind and provenance of a refused frame that opens no interaction. That row is the owner of the fact **AI1** is about, and it inherits the same four-field reference. |
| Completeness | conforms-with-nonblocking-findings | AG1 remains closed in the silence-probe row, which now names the complete record set both endpoints produce. **AH2 is closed**: the property audit's `C4` required-green cell names all seven members. **AH5 is closed in that same cell and open in the `I5` row** (**AI3**). The disposition history is accurate and runs to the twelfth independent review; the residual risks are stated as challenges rather than resolutions; the AF7 audit extension is complete over all 25 properties and its `owed` cells are honest. Status block stale by three families and stale about its own history length (**AI4**). |
| Migration coverage | conforms | All 24 predecessor vectors dispositioned CH-01 through CH-24 in order, verified against `conformance/channel-0.1-vectors.json`, which holds exactly 24 (`CH-01-CORRELATION-ECHO` … `CH-24-FAILURE-DOMAIN-RELATIVITY`). Twelve protocol categories, seven process categories, five failure domains, ten limits, ten features, and every observation field and resource subfield carry a disposition from the declared five-value vocabulary. AE5's retained requirements register is in the sources inventory and the completion check (AF3); `CH-R10` is dispositioned **replaced** with `CH-K5` **retained**; AF4's admission is in the new-evidence inventory; Z4's intra-interaction frame order and both mutations are listed. Status block stale by two families (**AI4**). |
| Neutral brief | does-not-conform | **AI1** is against this document's parity profile and local-observation schema; **AI5** and **AI7** are against the vector-format bullets AH1 rewrote; **AI6** is against the admission bullet. **AH1's first half is closed** — the declared stimulus step names its session, the operator's qualifier now has an operand, and probe **P3** confirms the two-session conforming vector is green. Everything else holds: artifact boundaries, identity spaces, the three-version rule, the closed operator set with W1's precedence relation and Z1's identification-only restriction on the arrival ordinal, the required-green set as a normative format field, the golden policy, the reordering-injection provider boundary with W2's present-not-true point, and the Batch 2 entry gate. Status block stale by four families and describing the pre-AH1 operand (**AI4**). |

## Owner rulings

The four first-batch rulings recorded 2026-08-11 are each represented consistently throughout the
first-batch design. Verified by tracing each ruling to every artifact that must carry it, not by
reading the plan's resolved-questions section alone.

| Ruling | Represented consistently | Trace |
| --- | --- | --- |
| Core concurrency and cancellation | yes | C4's finite positive `max-in-flight` and C8's optional cancellation with fixed meaning; the interaction machine's `Concurrent interactions` and `Cancellation` sections; matrix rows `Bounded unary concurrency` → `channel-profile`, `Cancellation control and terminal meaning` → `channel`, `Class-specific cancellability` → `channel-profile`; ledger `maxConcurrentRequests` → **replaced** as `max-in-flight`, `single invocation` → **replaced**, `cancellation unsupported` → **replaced**; the grid's cancellation columns; the matrix's `Concurrency and cancellation` boundary ruling. The completeness review's direction-scope row records the session-wide-versus-per-direction disagreement rather than hiding it, and now records its relation to AE3; **AI3** is about which audit cells carry the pointer, not about the ruling. |
| Session-state ownership | yes | C2 and the session machine's explicit "not Channel session states" list; matrix rows assigning Interconnection and Release to `portable-binding`, the Relational Initialisation phase to `composition`, and Ready to `component-management`, with the same sentence in the plan's ruling and the matrix's boundary ruling; ledger `ready` → **moved** as state, message kind, and feature. No artifact lets a peer signal create a composition fact. |
| Relational initialization representation | yes | C3 and C7 as an interaction class under the ordinary machine; the interaction machine's `Relational initialization` section with the `interconnected && !ready` predicate; the matrix's boundary ruling of the same name; ledger `Lifecycle` → **removed** and split. Matches Decision 13's recorded Option B including its explicit rejection of C and D, its composition-root standing-in, and its refusal to introduce a Component-to-Component binding kind, with the envelope-kind departure disclosed and reasoned. |
| Extension invariants | yes | C11; cross-capability invariant 7; the matrix's `Extension hooks` list of the five things a facet cannot reinterpret; the brief's facet rules; ledger `retry unsupported` and `streaming unsupported` dispositions. C11's sentence that a facet may strengthen the intra-interaction ordering fact but not weaken it is the one place the S1 ruling touches this ruling, and the two are consistent. |

The plan's `## Open questions (owners needed)` section correctly reports no unresolved owner decision.
The R1 (2026-08-13), S1 (2026-08-13), and AE1 (2026-08-14) correction rulings are each recorded as
correction rulings that do **not** join the fixed set of four. AG3 remains closed: the AE1 ruling
states the membership operand "within one session" and carries the "Issued with a vector-scoped
operand, narrowed to the session under AF8 on 2026-08-15" note, retaining the original wording — the
same treatment the S1 ruling gives `channel-core`.

**The 2026-08-15 closure-standard ruling.** I read it in the plan rather than taking the dispatching
brief's summary of it. It is recorded as a first-batch ruling on the closure standard that does not
join the four design rulings; it selects "only `conforms` closes"; it records the rejected alternative
with reasons; and it states plainly that it was made after a verdict it excludes and why that timing is
disclosed rather than left unremarked. It is represented consistently in the plan, the review policy's
"Exact next work" step 3n, the completeness review's disposition history, and the Channel index's plan
row. It governs the consequence of my verdict and did not affect which verdict I reached: **AI1** is
blocking on the programme's own applied standard, not on the new one, and my verdict would be
`does-not-conform` under either.

## Retained findings

Every retained finding was verified in the artifact it was raised against rather than taken from a
disposition history or an index. Summary, with only the departures spelled out:

- **B1-B4, N1-N3, F1-F3, D1-D5, T1-T4, R1-R3, S1-S3** — closed. Recipient frameless `refused-local`;
  nonterminal cancellation-denial transition; one owner identifier per row; five-value disposition
  vocabulary; exact Ready ownership; peer fault from `cancel-pending`; `retained` disposition with
  treatment column; `replay-detected` live window; distinct recipient `peer-fault`/`lost`; `replaced`
  cancellation Outcome; duplicate drain fatal; distinct acknowledgement states; receiver-local phase to
  `refused-local`; the three-value latch; delivery-fallback moved to its facet; phase refusal never
  `state-violation`; T4's stable phrase present with no superseded cycle name in any status block; held
  control bounded at one; local unsynchronised preconditions; separate `unseen` and `validating` grid
  rows. **S3 is the exception: it is closed on three of the four surfaces its evidence quotes and open
  on the redesign plan's §7.8** (**AI9**), and its class recurs on the narrative surfaces as **AI2**.
- **U1-U8** — closed. U1 at the property, at C4's vector passage, and at the completeness review's
  account of the same vector. U2-U8 closed: the owner vocabulary is closed with the ordering row owned
  by `channel`; the brief carries the establishment declaration and the adversarial group; the audit
  registers `C4-P2` and both mutations; the direction-scope row records the disagreement; the initiator
  pre-dispatch Local loss cell names `lost`. **U4 is closed in the disposition history and its own
  status block still claims the eighth cycle** (**AI4**). **U6's pin clause is true as to subject and
  false as to date** (**AI8**).
- **V1-V3, W1-W6, X1-X7, Y1-Y4, Z1-Z4** — closed. Detailed reason compared; reordering injection
  declared and bounded to mutation vectors; the precedence relation restricted to one endpoint's own
  declared steps; the reordering provider's declaration with the present-not-true point; second
  mutation added and placed in a required group; retention rule in C4, the machine, and the grid;
  committing-endpoint operand supplied; latch compared; settling frame recorded and compared;
  `not-applicable` owned; `unseen` transition row present; recording-versus-retaining distinction;
  iteration reviews retained; C10 and the schema carrying the latch and settling frame; the refusal
  leaving state at `unseen`; the arrival ordinal restricted to identification; the grid naming a
  provenance as a provenance; the ordering requirement in the new-evidence inventory. **W5 is closed on
  the endpoint dimension and on the session dimension AH1 added; it is the settling-frame reference that
  now lacks the operand** (**AI1**). **Y4 is closed as framed and its class recurs on the session
  dimension** (**AI1**).
- **AA1-AA3, AB1-AB2** — closed as framed. Both indexes carry every disposition family somewhere, both
  computed counts read 12, `channel-core` appears in no status entry point, and the matrix owns local
  observation content and provenance. AB1's own surface is corrected for the third time and now has a
  check; **AB1's class on the eight design artifacts is AI4**, and **AA2's narrative half is AI2**.
- **AC1-AC4** — closed. The arrival ordinal is in the brief, the interaction machine, the grid, and the
  matrix; the closed detailed-reason set carries `unopened-interaction-identity` and C10 requires the
  refused frame's kind; `C4-P2`'s subject is the committing endpoint in both conjuncts and named
  explicitly; the class check matches two-letter families. **AC3's pronoun class recurs in the same
  paragraph** (**AI6**).
- **AD1-AD3** — closed, AD2 by the ruled correction which AF6 replaced with the declared provenance
  table. The table classifies every family the policy bolds, including `AH`, and every `iteration`
  family has its retained record.
- **AE1-AE5** — closed. AE1 at the property, the parity profile, the contract's vector passage, and the
  completeness review's silence-probe row; AE2 in both artifacts and both grid cells; AE3 as a rule in
  C12 with the format field and the audit column; AE4 and AE5 on both surfaces each.
- **AF1-AF8** — closed. AF1 on both surfaces, verified by evaluator (**P3**, rows M1 and M1b). AF2 on
  all three surfaces its evidence named, subject to **AI4** on the artifacts' own status blocks. AF3
  verified against the register's own highest identifiers (`CH-R` 11, `CH-K` 7). AF4, AF6, and AF7
  closed. **AF5 closed on all three surfaces** now that AH2 corrected the audit. AF8 closed at the
  membership operand in both normative artifacts and in the ruling of record.
- **AG1-AG5** — closed. AG1 verified by evaluator: a vector authored from the silence-probe row as it
  now reads takes `C4-P2` red on its own named mutation. AG2's qualifier present in the operator with
  the claim pinned; **its class recurs in the AH1 correction's citation of C2** (**AI5**). AG3's ruling
  note present in the S1 ruling's form. AG4's nine artifact rows each making a claim about `AH`, with
  the escape clause now bound (AH4). AG5's `| Channel |` row naming AH1-AH6.
- **AH1** — **closed at the precedence operand**, and the multi-session question it opened is answered.
  Its answer is what makes **AI1** live and what **AI5** and **AI7** are against.
- **AH2** — **closed.** The property audit's `C4` required-green cell names all seven legal members
  including both conforming-delivery members and the lost acknowledgement, and a new check reads that
  cell rather than the contract's window.
- **AH3** — **closed in one of three named surfaces** (**AI2**).
- **AH4** — **closed.** The escape clause is now `unchanged by[^|]*\b<family>\b`, and I confirmed by
  mutation (probe **P5**) that a row reading "unchanged by AF and AG" no longer satisfies the `AH`
  check while the five rows that now say "unchanged by AH" do.
- **AH5** — **closed in the `C4` audit row and the direction-scope row, open in the `I5` row**
  (**AI3**).
- **AH6** — **closed.** Both citing sentences state the retention rule as "not barred / admitted on its
  own merits" and both state the coverage limit; a verifier check fires on either old wording. Probe
  **P3** row `R` confirms the limit is real and correctly described. **AI6** is against the insertion,
  not the substance.

## Probes performed

### P1 — gates, in the isolated clone

| Gate | Result |
| --- | --- |
| `build/verify-channel-0.2-design.ps1` | pass — "11 required artifacts, C1-C12 with properties/scenarios/silence, total session/interaction event coverage, 6 session states, all 24 predecessor vectors dispositioned, 4 owner rulings resolved, and independent review still pending" |
| `build/verify-channel-0.2-design.ps1 -NegativeProbe` | fails with exactly one failure — "Channel 0.2 capability contract properties is missing '**Property C12-P1.**'" — which is the in-memory removal and nothing else |
| `build/verify-doc-links.ps1` | pass — 863 local links across 305 documents |
| `build/verify-text.ps1` | pass — 884 UTF-8 files |

`build/verify-interchange.ps1` was not run; it was outside the set the dispatching brief authorised for
this review.

**Green gates are not evidence of conformance**, and this review found that again: all nine findings
sit behind a fully green design gate, including the blocking one. **AI1** sits behind the AH1 check,
which verifies that a declared stimulus step names a session within a 110-character window and never
that the settling-frame reference can name one. **AI2** sits behind the AA2 family check, which asks
only that a family appear somewhere in `docs/future/README.md`, and behind the new AH3 check, which
reads one sentence of that file and the plan's status block. **AI3** sits behind the AH5 check, which
requires the string `AE3` in the direction-scope row and reads no audit cell. **AI4** has no check at
all, and neither does the plan section **AI9** is against — the AB1/AH3 check stops at the status
block's closing delimiter. **AI8** sits behind the X6 check, which compares the subject and not the
date.

### P2 — independent enumeration of the state/event grid

Parsed mechanically from the grid's three tables and cross-checked against the interaction machine's
own state tables rather than against the grid's prose counts.

- Session: 6 states × 6 event columns = **36** cells, 0 empty.
- Initiator: 6 published state groups × 6 columns = **36** published cells, 0 empty; the machine states
  12 initiator states (`candidate`, `admitting`, `refused-local`, `dispatched`, `cancel-pending`,
  `cancel-accepted`, `cancel-refused`, three Outcome terminals, `peer-fault`, `lost`; 6 terminal),
  giving 12 × 6 = **72** underlying pairs.
- Recipient: 6 published groups × 6 columns = **36** published cells, 0 empty; the machine states 12
  recipient states (7 terminal), giving 12 × 6 = **72** underlying pairs.

**108 published-row cells, 0 empty, 180 underlying state/event pairs** — agreeing with the seventh
through twelfth reviews. No cell offers a choice between two routes, and the closed-world rule ordering
is well-founded.

### P3 — falsification and soundness of a capability-wide property (`C4-P2`), and the positive result AI1

The policy requires at least one genuine attempt to falsify a capability-wide property. An evaluator
was written from the published prose — `C4-P2`'s two conjuncts with AC3's committing-endpoint subject,
the AE1 admission clause and AF8's session scope, the brief's closed operator set including AG2's
session qualifier on precedence, the brief's vector format as AH1 rewrote it, the parity profile's
compared fields, and the settling-frame reference as the brief, the interaction machine, and the grid
state it. It imports no repository code. Precedence is implemented exactly as the brief declares it and
returns *no verdict* outside its scope rather than a default; the arrival ordinal is used for equality
only and never as an ordering operand.

Each vector was run under two bindings of the settling-frame reference, so the design's claim about its
own operand could be tested rather than assumed. **A** takes the session from the enclosing local
observation, which no artifact states. **C** indexes the ordinal over the committing endpoint's frames
of that kind for that identity, which is what the four published fields support.

| Vector | Design expects | A | C |
| --- | --- | --- | --- |
| 1. conforming commit-order delivery, initiator direction | green | green | green |
| 2. conforming commit-order delivery, recipient direction | green | green | green |
| 3. request lost, control delivered (AE1's member) | green | green | green |
| 4. acknowledgement lost | green | green | green |
| 5. cancellation control for an identity the peer never opened | green | green | green |
| 6. legal late control after a peer's terminal | green | green | green |
| 7. duplicate terminal from a nonconformant peer | green | green | green |
| M1. `C4-control-precedes-request`, expected obs per the corrected C4 passage | red | red | red |
| M1b. same vector, expected obs per the completeness silence-probe row | red | red | red |
| M2. `C4-outcome-precedes-ack` | red | red | red |
| P. wholly conforming two-session identity reuse + required-green member 7 | green | green | green |
| Q. two-session, the conforming session declared first | green | green | green |
| R. reordering whose displaced request is refused on its own merits | green as stated | green | green |
| **M2-two-session. `C4-outcome-precedes-ack` with a conforming second session reusing the identity** | **red** | **red** | ***green*** |

Six results matter.

1. **`C4-P2` is sound in both directions in single-session form.** Green on all seven legal members of
   its required vector group, red on both named mutations. AE1, AF1, AF5, AG1, and AH2 all hold.
2. **AH1's fix works.** Vector `P` — review 12's own falsifying case — is green under precedence as
   published, because the operator's session qualifier now has an operand to read. Vector `Q`, which
   reorders the two sessions, is also green: the qualifier prevents a *false red* whichever way the
   reference binds, and that half of the operand chain is complete.
3. **`M2-two-session` is AI1**, and it is a false *green* rather than a false red, which is the half the
   session qualifier cannot protect. The ordinal binds the settling frame to the conforming session's
   acknowledgement; that session has no terminal frame for the identity; precedence correctly declines
   to compare across sessions; and the genuine violation in the other session is never reached.
4. **Restricting both conjuncts to one endpoint's own frames remains load-bearing**, confirmed rather
   than assumed: member 6 is green only because the settling frame and the terminal frame have different
   committing endpoints, and member 7 only because the ordinal binds the settling frame to the *later*
   of the two matching steps.
5. **Row R is AH6 working.** A reordering whose displaced request is refused on its own merits leaves
   the first conjunct green, and both the contract and the brief now say so instead of claiming the
   retention rule requires the admission. The correction is right; **AI6** is about how it was inserted.
6. **Row M1b confirms AG1 stays closed.** A vector authored from the silence-probe row as it now reads
   takes the property red on its own named mutation.

### P4 — mechanical enumeration of ownership, the property audit, the registry pins, and the status blocks

- **Responsibility matrix, enumerated from the source:** 39 ownership rows, 22 distinct owner
  identifiers used, every row carrying exactly one backticked owner, zero rows with two owners or none,
  and `channel-core` in no owner cell.
- **Property audit, enumerated:** 12 capability rows + 13 state-machine rows = 25 audited, against 12
  C-properties + 6 `S` + 7 `I` = 25 stated. Complete.
- **Registry pins, all twelve path/hash pairs in `Brontide-Architecture-Status.json` recomputed** — the
  Architecture 0.8 document, the 0.5 implementation baseline requirements, the 0.8 requirements, both
  stacks' 0.5 matrices, both stacks' 0.8 matrices, both stack READMEs, and both stack milestone-evidence
  ledgers — **all twelve match**.
- **Register ranges computed from the register itself:** `CH-R` highest = 11, `CH-K` highest = 7,
  matching the ledger's claimed range and its completion check.
- **`conformance/channel-0.1-vectors.json` holds exactly 24 vectors**, `CH-01-CORRELATION-ECHO` through
  `CH-24-FAILURE-DOMAIN-RELATIVITY`, matching the ledger's coverage claim row for row.
- **Status blocks, families extracted from the first 25 lines of each design artifact** — the table
  under **AI4**. This is the enumeration that produced that finding; reading the blocks did not.

### P5 — mutation-testing the AH4 check (negative result: the check is sound)

AH4's fix binds the AG4 escape clause to the newest family. I re-ran the row check against three
mutated rows: "unchanged by AF and AG" (the pre-AH4 wording) now **fails** the `AH` check, "unchanged
by AH" **passes**, and a row naming `AH2` passes through the first clause. The five rows that
previously depended on the unbound escape all now name `AH` explicitly. The check does what its comment
says. Recorded because a failed attempt to break a correction is evidence, and because four of the last
five correction passes have produced a check narrower than its own comment.

### P6 — attempts to falsify `C1-P1` and `C11-P1` (negative results)

`C1-P1` asserts that either exactly one profile is established with every normative fact equal to the
expected profile, or no interaction is dispatchable and effect certainty is `known-none`. The sharpest
available case is the negotiated path producing a profile the fixed path cannot express, which would
make the disjunction's first arm true of one realization and false of the other for the same vector.
**It does not fail**: the session machine's fixed/negotiated equivalence section makes a field absent
from the fixed path a contract defect rather than realization freedom, and C1's own "no implicit
downgrade, no in-place renegotiation" closes the remaining route. Transport loss during establishment
reaches the second arm with `known-none` rather than a partially established session.

`C11-P1` asserts every established profile has all required facets supported exactly and no facet
changes a core identity, authority, terminal-provenance, or uncertainty result. The sharpest case is a
Distributed facet declaring delivery ordering, since C11 explicitly lets a facet add ordering
guarantees and C4 owns intra-interaction frame order. **It does not fail**: C11 states the one ordering
fact core owns and says a facet "may add delivery and ordering guarantees beyond it but may not weaken
it", and retry is bound to a new interaction identity so a facet cannot reach interaction identity
through the retry route. Recorded because a failed falsification attempt is evidence and an unrecorded
one is not.

### P7 — upstream consistency and clone completeness

- `Brontide-Architecture-Status.json` selects Architecture 0.8 at
  `docs/current/architecture/Brontide-Architecture-0.8.md`, status "Complete Draft (document and
  implementation evidence complete; not ratified)", with `latestRatifiedArchitecture` null and the
  rationale "No Brontide architecture document currently has Ratified status." The document's own header
  carries the same Complete Draft status.
- Both stacks state `**Designed for:** Brontide Architecture 0.8, Complete Draft, not ratified` and
  `**Status:** Partial implementation with explicitly labelled experiments`. The Channel 0.2 contract
  states `Designed for: Brontide Architecture 0.8, Complete Draft` and the plan `Designed against:
  Brontide Architecture 0.8, Complete Draft`. No artifact treats 0.8 as ratified or claims Channel 0.2
  implementation conformance, and every first-batch status block carries T4's stable phrase.
- Decision 13's recorded ruling (Option A retained for 0.1, Option B selected for 0.2, C and D rejected,
  recorded 2026-08-11) matches C3, C7, and the plan's relational-initialization ruling, including the
  composition-root standing-in and the refusal to introduce a Component-to-Component binding kind.
- PB8's blocking finding in both stacks — process loss fabricating a known zero effect count — is
  answered by C10's certainty form and `C10-P1` rather than restated as a Channel 0.1 defect, and the
  ledger moves `providerEffectCount` to the Portable Binding/domain owner.
- The retained Channel 0.1 design note, draft contract, and requirements/risk ledger are present,
  inventoried in the migration ledger's sources list, and unmodified by this correction sequence.
- `channel/0.2` does not exist: no neutral schema, vector, property, or golden has been authored, and
  the Batch 2 entry gate in the brief is unchanged.
- 890 tracked paths, empty `git diff HEAD`, clean status, HEAD at
  `e7bfeba6ba58e2e4e9a48a5148e2461c187bf452`. No design artifact was read from outside the clone.

## What this verdict means

**Four of the six AH corrections land completely, and one of the two that do not is a table row.** AH1
answers a question that had been open under two families, AH2 closes AF5's last surface, AH4 fixes a
gate that would have silently expired at every future family, and AH6 corrects two sentences that
overstated what the design guarantees and states the coverage limit that follows. The 2026-08-15 ruling
is recorded with its rejected alternative and is candid about its own timing. This is the strongest
correction commit in the sequence, and I want that on the record alongside the verdict.

**The verdict turns on one escalation I did make and several I did not.**

- **AI1 is the escalation.** It is a demonstrated false green on a named mutation, produced by a vector
  the reviewed commit legalised and an evaluator written from two artifacts' exhaustive field lists,
  and those two artifacts affirmatively claim the reference is unambiguous. Every previous false green
  in this programme was blocking. The counter-argument — that the session is available by containment
  from the enclosing observation, which C10 requires to distinguish session identity — is sound and
  unwritten, and I have stated it in full under AI1 so an owner who accepts it can see exactly what
  they are accepting. **If it is accepted, my verdict should be read as
  `conforms-with-nonblocking-findings`**, which under the 2026-08-15 ruling still does not close the
  batch.
- **AI2, AI3, AI4, and AI9 are staleness and record findings**, all rated on the unbroken precedent
  that entry-point and record staleness is nonblocking (S3, AA1/AA2, AE4, AF2, AG4/AG5, AH3). AI4 is
  the largest by surface count and AI9 is the only one against a *retained finding still open*, and I
  escalated neither, because no design fact is contradicted and every affected artifact's *normative
  body* is current. **AI9 is the closest of the four**: an owner who holds that a retained finding open
  in an artifact its own evidence quotes is blocking by definition would escalate it, and the
  programme has never yet said either way.
- **AI5, AI6, AI7, and AI8** are a misattributed citation, a pronoun, two singulars, and a wrong date.
  None changes a property verdict. Each is recorded because each is a named class this programme has
  already paid for — AG2, AC3, AH1, and X6 respectively — recurring inside the commit that closes the
  previous instance.

**What I would tell the next pass to read first is the operand chain, end to end.** Five findings across
three cycles — AF8, AG2, AH1, AI1, AI7 — are one question asked five times: which of `C4-P2`'s operands
need a session, and does the artifact that publishes each of them give it one? The answer is now known
for the membership set and the declared step and unknown for the settling-frame reference and the
established profile. A pass that fixes AI1 by adding a session to the settling-frame position, and stops
there, will have done what the AE, AF, AG, and AH passes each did — closed the instance in the first
artifact its evidence names. The list to sweep is every position in the brief's local-observation schema
and every entry in its parity profile, against the question "is this per-session, and does a vector now
carry more than one?"

**The second thing worth carrying forward is what AI2, AI4, and AI9 say together.** Seven cycles have
now raised entry-point staleness and each correction has closed the surfaces the previous finding's
evidence enumerated — and AI9 shows that even that has been done incompletely, because S3 quoted §7.8
and the correction updated the quoted number without carrying the section forward again. AI4 is what
happens when nobody enumerates: eight artifacts carry a status block of the same kind, one of them was
fixed three times because it was the one a finding named, and the other seven have never been checked
for family currency at all. The cheapest fix is one check that iterates every artifact's status block
and the plan's §7.8 together, not a fourth check over the plan's status block alone.

**On the consequence.** This verdict does not satisfy the Closure section: a blocking finding is open, so
neither "every blocking review finding is corrected" nor "a fresh closure attestation conforms at the
corrected commit" holds. Batch 2 does not open. **I did not create
`channel-0.2-design-foundation-closure-record.md`**; that is a separate step, and my verdict would not
authorize it in any case. The named residual work also remains: the `owed` required-green cells across
the property audit, of which `I1`-`I7` satisfy neither half of C12's rule, and the completeness review's
own statement that **Batch 2 cannot author `capability-properties.json` until those are stated**.

The design was not repaired here: this attestation is the only file this reviewer wrote, nothing else in
the clone was modified, and nothing was committed.

## Note on the design gate

The gate results in **P1** are from before this attestation existed. Retaining it will make
`build/verify-channel-0.2-design.ps1` fail with the same class of failures the ninth through twelfth
reviews recorded: `$expectedReviewNames` names the README, exactly twelve attestations, and the four
iteration reviews, and the two computed counts in the Channel index and the future-work index read `12`.
That is the verifier working as designed, and because this verdict does not conform, the correction pass
does **not** additionally have to change what "independent review still pending" or the twelve
`awaits a fresh independent closure re-review` phrases assert.

Four notes for that pass.

First, **AI1**'s fix belongs in the parity profile's settling-frame bullet, the brief's local-observation
schema, and the interaction machine's latch section — three artifacts, because all three publish the
reference — and the check that guards it must read the *reference's* field list, not the vector format's
step bullet the AH1 check already reads. A check scoped to the artifact that was already corrected is
what let AF1 survive its own correction, twice.

Second, **AI3** is one table row and its check must read the `I5` row specifically; a check that greps
the document for `AE3` passes today.

Third, **AI2**, **AI4**, and **AI9** are the same shape at three scales and should be fixed with one
check that iterates artifacts and sections rather than three checks that name documents. If the pass
updates the counts and the plan's status block without reading the narratives above them, the front
matter of the other eight artifacts, and the plan's §7.8, all three findings survive the commit that
closes them — for the seventh, second, and second time respectively. The verifier's own
`all twelve negative attestations` message is in the same class and is a one-word change.

Fourth, **AI5** is the one that should change how the next check is written rather than what it asserts.
The AG2 machinery pins one cross-artifact claim against one artifact. The claim AH1 wrote about C2 was
not pinned because nobody thought to pin it, which is the same reason AG2's claim was unpinned before
AG2. A check that extracts every sentence of the form "<other artifact>'s X" and requires X to appear in
that artifact is the general form; naming them one at a time is the fifth consecutive round of the same
shortfall the commit message itself records.
